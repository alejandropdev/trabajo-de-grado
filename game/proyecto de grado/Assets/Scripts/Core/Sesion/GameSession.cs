using System;
using System.Collections.Generic;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Eventos;
using Nexus.Core.Guardado;
using Nexus.Core.Metodologia;
using Nexus.Core.Minijuegos;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Servicios;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Sesion {
    /// <summary>
    /// El hub (§4.9). Un nivel entero, de la Fase 1 al Dashboard de Lecciones.
    ///
    /// ★ La topologia es ESTRELLA, no malla: si aparece una flecha entre dos subsistemas que no pasa
    /// por aqui, esta mal. La unica excepcion es EventDirector, que si habla con el evaluador de
    /// condiciones, el azar, la agenda y las reglas de metodologia.
    ///
    /// Implementa cuatro puertos que otros paquetes declararon antes de que esta clase existiera:
    /// IStateContext (lo que ve una precondicion), ISesionPersistible (lo que guarda C9),
    /// IMetricasDelNivel (lo que mide C8) y, a traves de WorldState/RuntimeState, los de C2.
    /// Ninguno de esos paquetes conoce GameSession: por eso se pudieron escribir y probar antes.
    /// </summary>
    public sealed class GameSession : IStateContext, ISesionPersistible, IMetricasDelNivel {
        // --- contenido y entradas ---
        private readonly Catalogo _catalogo;

        public LevelProfile Perfil { get; private set; }
        public MethodologyProfile Metodologia { get; private set; }
        public MethodologyRules Reglas { get; private set; }
        public int Semilla { get; private set; }

        // --- estado ---
        public WorldState W { get; private set; }
        public RuntimeState R { get; private set; }
        public Coeficientes Coef { get; private set; }
        public DecisionTrace Traza { get; private set; }
        public CompetenceProfile Competencia { get; private set; }
        public FlagStore Flags { get; private set; }

        // --- subsistemas ---
        private DeterministicRng _rng;
        private EffectScheduler _scheduler;
        private EventDirector _eventos;
        private MinigameDirector _minijuegos;
        private NarrativeDirector _narrativa;

        // --- salidas para la UI ---
        public DayBrief BriefDeHoy { get; private set; }
        public PendingDecision Decision { get; private set; }
        public PendingMinigame Minijuego { get; private set; }
        public BeatDeHoy Beat { get; private set; }
        public PlanningRequest PendingPlanning { get; private set; }
        public RetroRequest PendingRetro { get; private set; }
        public LaunchResult Lanzamiento { get; private set; }

        public bool Fase1Cerrada { get; private set; }
        public bool NivelTerminado { get; private set; }

        // --- lo privado que NO se puede recalcular, y por eso viaja en el guardado (§4.9.1) ---
        private double _velocidadBaseMult = 1.0;
        private double _throughputAcumulado;
        private double _avanceAlEmpezarUnidad;
        private string _unidadAnterior;
        private string _arquitectura;
        private string _razonMetodologia;
        private string _razonArquitectura;
        private Dictionary<string, int> _fichasDeCalidad = new Dictionary<string, int>(StringComparer.Ordinal);

        private DayPlan _planDeHoy;
        private EventDefinition _eventoDeHoy;

        public string NivelId { get { return Perfil.Id; } }
        public IReadOnlyList<DecisionDelDirector> LogDeEventos { get { return _eventos.Log; } }

        // ==================================================================== construccion

        /// <summary>
        /// Empieza un nivel. El perfil se CLONA: el reparto de fichas de la Fase 1 reescribe
        /// Director.PesosPorTag, y el catalogo se carga una sola vez para toda la aplicacion.
        /// </summary>
        public GameSession(Catalogo catalogo, string nivelId, FlagStore flags, int semilla) {
            if (catalogo == null) throw new ArgumentNullException(nameof(catalogo));

            LevelProfile perfil;
            if (!catalogo.Niveles.TryGetValue(nivelId ?? "", out perfil))
                throw new InvalidOperationException($"No existe el perfil de nivel '{nivelId}'.");

            _catalogo = catalogo;
            Perfil = perfil.Clone();
            Semilla = semilla;
            Flags = flags ?? new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), catalogo.Flags);

            W = WorldState.DesdeNivel(Perfil);
            R = RuntimeState.DesdeNivel(Perfil);
            Coef = Perfil.Coef.Clone();
            Traza = new DecisionTrace();
            Competencia = new CompetenceProfile();

            _rng = new DeterministicRng(Perfil.Director.Semilla != 0 ? Perfil.Director.Semilla : semilla);
            _scheduler = new EffectScheduler();
            _narrativa = new NarrativeDirector(catalogo.Beats);
        }

        /// <summary>Las que este nivel permite, en el orden del perfil.</summary>
        public List<MethodologyProfile> MetodologiasDisponibles() {
            var lista = new List<MethodologyProfile>();
            if (Perfil.MetodologiasPermitidas == null) return lista;

            foreach (var id in Perfil.MetodologiasPermitidas) {
                MethodologyProfile perfil;
                if (_catalogo.Metodologias.TryGetValue(id, out perfil)) lista.Add(perfil);
            }
            return lista;
        }

        // ==================================================================== FASE 1

        /// <summary>
        /// La primera de las tres decisiones fundacionales. Reescribe ocho cosas del motor de golpe:
        /// el calendario, las ceremonias, las reglas de cambio, los coeficientes, el director,
        /// el tablero, el lanzamiento y la rubrica con la que se te va a juzgar.
        /// </summary>
        public void ElegirMetodologia(string id, string razonId) {
            ExigirFase1Abierta();

            MethodologyProfile perfil = null;
            foreach (var candidata in MetodologiasDisponibles())
                if (string.Equals(candidata.Id, id, StringComparison.OrdinalIgnoreCase)) perfil = candidata;

            if (perfil == null)
                throw new InvalidOperationException($"'{id}' no es una metodologia permitida en {Perfil.Id}.");

            Metodologia = perfil.Clone();
            Reglas = new MethodologyRules(Metodologia, Perfil.DiasTotales);
            _razonMetodologia = razonId;

            Coef.MultiplicarPor(Metodologia.ModificadoresModelo);

            double velocidad;
            if (Metodologia.ModificadoresModelo != null &&
                Metodologia.ModificadoresModelo.TryGetValue("velocidadBase", out velocidad))
                _velocidadBaseMult *= velocidad;

            // El drama del nivel lo escala la metodologia: Cascada hace el nivel mas plano, y mas aburrido.
            if (Metodologia.Calendario.Tipo == TiposDeCalendario.Continuo)
                R.LimiteWip = Metodologia.Calendario.LimiteWipInicial;

            Registrar("FASE1", "Metodologia: " + Metodologia.Nombre, id, razonId,
                      VeredictoDeRazon(Metodologia.RazonesValidas, Metodologia.RazonesTrampa, razonId),
                      "OA-PROC-01", RazonDeMetodologia(razonId), null);
        }

        /// <summary>
        /// El reparto de fichas de calidad. ★ El giro del §4.2.4: **no invertir en un atributo SUBE el
        /// peso de su tag en el director**. El jugador acaba de configurar que crisis va a sufrir el
        /// resto del nivel, y la pantalla no se lo dice.
        /// </summary>
        public void RepartirCalidad(Dictionary<string, int> fichasPorAtributo) {
            ExigirFase1Abierta();
            if (fichasPorAtributo == null) throw new ArgumentNullException(nameof(fichasPorAtributo));

            var calidad = Perfil.Fase1.Calidad;
            var total = 0;
            foreach (var kv in fichasPorAtributo) {
                if (kv.Value < 0) throw new InvalidOperationException($"'{kv.Key}' tiene un reparto negativo.");
                total += kv.Value;
            }
            if (total > calidad.Fichas)
                throw new InvalidOperationException($"Se repartieron {total} fichas y solo hay {calidad.Fichas}.");

            _fichasDeCalidad = new Dictionary<string, int>(fichasPorAtributo, StringComparer.Ordinal);
            ReaplicarPesosDeCalidad();

            // Invertir en un atributo tambien mejora su coeficiente del modelo. Esto NO va en
            // ReaplicarPesosDeCalidad: se aplica una sola vez, aqui, porque su efecto queda dentro
            // de Coef y Coef viaja en el guardado. Reaplicarlo al restaurar regalaria el descuento
            // en cada recarga, y sin dar error — que es exactamente lo que INV-7 prohibe.
            foreach (var atributo in calidad.Atributos) {
                if (atributo == null || string.IsNullOrEmpty(atributo.CoeficienteAfectado)) continue;

                int fichas;
                if (!_fichasDeCalidad.TryGetValue(atributo.Id, out fichas) || fichas <= 0) continue;
                Coef.MultiplicarUno(atributo.CoeficienteAfectado, Math.Pow(0.93, fichas));
            }

            Registrar("FASE1", "Reparto de calidad", "calidad", null,
                      total == calidad.Fichas ? Veredictos.Correcta : Veredictos.Aceptable, "OA-CAL-01",
                      total == calidad.Fichas
                          ? "Repartiste todas las fichas."
                          : $"Dejaste {calidad.Fichas - total} fichas sin usar; no se guardan para despues.",
                      null);
        }

        /// <summary>
        /// Aplica el reparto SOLO sobre los pesos por tag. Se llama tambien al restaurar, y es lo unico
        /// de la Fase 1 que hay que reaplicar: estos pesos viven en el LevelProfile, que se recarga
        /// limpio del catalogo en cada arranque.
        ///
        /// Todo lo demas de la Fase 1 (los efectos de la arquitectura, los modificadores de la
        /// metodologia, el descuento de coeficiente por invertir en un atributo) ya esta dentro de
        /// W y de Coef, que si viajan en el guardado. Tocarlos aqui seria regalarlos en cada recarga.
        /// </summary>
        private void ReaplicarPesosDeCalidad() {
            var calidad = Perfil.Fase1.Calidad;
            if (calidad == null || calidad.Atributos == null) return;

            foreach (var atributo in calidad.Atributos) {
                if (atributo == null || string.IsNullOrEmpty(atributo.TagAfectado)) continue;

                int fichas;
                _fichasDeCalidad.TryGetValue(atributo.Id, out fichas);

                double actual;
                if (!Perfil.Director.PesosPorTag.TryGetValue(atributo.TagAfectado, out actual)) actual = 1.0;
                Perfil.Director.PesosPorTag[atributo.TagAfectado] = actual * CalidadConfig.FactorDeTag(fichas);
            }
        }

        public void ElegirArquitectura(string id, string razonId) {
            ExigirFase1Abierta();

            ArquitecturaOpcion opcion = null;
            foreach (var candidata in Perfil.Fase1.Arquitecturas)
                if (string.Equals(candidata.Id, id, StringComparison.OrdinalIgnoreCase)) opcion = candidata;

            if (opcion == null)
                throw new InvalidOperationException($"'{id}' no es una arquitectura de {Perfil.Id}.");

            var antes = W.ToString();
            EffectApplier.Aplicar(W, opcion.Efectos);
            Coef.MultiplicarPor(opcion.ModificadoresModelo);

            _arquitectura = opcion.Id;
            _razonArquitectura = razonId;

            Registrar("FASE1", "Arquitectura: " + opcion.Nombre, id, razonId,
                      VeredictoDeRazon(opcion.RazonesValidas, opcion.RazonesTrampa, razonId, opcion.Veredicto),
                      "OA-ARQ-01", opcion.Razon, antes);
        }

        /// <summary>Cierra la planificacion y arranca el bucle diario. El director ya puede agendar.</summary>
        public void CerrarFase1() {
            ExigirFase1Abierta();
            if (Metodologia == null) throw new InvalidOperationException("Falta elegir metodologia.");
            if (_arquitectura == null) throw new InvalidOperationException("Falta elegir arquitectura.");

            Fase1Cerrada = true;
            R.Fase = 2;
            R.CoberturaAlCerrarDiseno = W.Cobertura;

            _eventos = new EventDirector(_catalogo.Eventos, Perfil, Reglas, _rng, _scheduler);
            _minijuegos = new MinigameDirector(_catalogo.Minijuegos, Perfil, _rng);
        }

        // ==================================================================== FASE 2

        /// <summary>
        /// La ventana de las 09:00, en el orden exacto del §5.3. El orden no es negociable: los efectos
        /// diferidos vencen ANTES de que el director elija, porque lo que se cobra hoy cambia lo que
        /// hoy es probable.
        /// </summary>
        public DayBrief ComenzarDia() {
            ExigirFase2();

            R.DiaActual++;
            R.Ventana = 0;
            Decision = null;
            Minijuego = null;
            Beat = null;
            PendingPlanning = null;
            PendingRetro = null;

            _planDeHoy = Reglas.PlanFor(R.DiaActual);
            R.SprintActual = _planDeHoy.IndiceDeUnidad;

            var brief = new DayBrief {
                Dia = R.DiaActual, DiasTotales = Perfil.DiasTotales,
                EtiquetaUnidad = _planDeHoy.EtiquetaUnidad + " " + (_planDeHoy.IndiceDeUnidad + 1),
                UnidadId = _planDeHoy.UnidadId, TextoDeUnidad = _planDeHoy.TextoDeUnidad,
                Tablero = Metodologia.TableroPrincipal
            };

            // 1 · lo que se cobra hoy de decisiones viejas
            foreach (var vencido in _scheduler.Vencidos(R.DiaActual)) {
                EffectApplier.Aplicar(W, vencido.Efectos);
                brief.EfectosQueVencieronHoy.Add(vencido.Origen);
                if (!string.IsNullOrEmpty(vencido.EventoForzado)) _eventos.ForzarEvento(vencido.EventoForzado, R);
            }

            // 2 · lo que la etapa cobra cada dia, pase lo que pase
            EffectApplier.Aplicar(W, _planDeHoy.EfectosDeEtapa);

            // 3 · ceremonias: cuestan dias de proyecto y pueden abrir planning o retro
            foreach (var ceremonia in _planDeHoy.Ceremonias) {
                brief.Ceremonias.Add(ceremonia.Nombre ?? ceremonia.Id);
                if (ceremonia.CosteDias > 0) W.Set("Dias", W.Dias + ceremonia.CosteDias);
                EffectApplier.Aplicar(W, ceremonia.Efectos);

                if (ceremonia.AjustaCoeficiente)
                    PendingRetro = new RetroRequest {
                        CeremoniaId = ceremonia.Id, Texto = ceremonia.Texto,
                        Acciones = ceremonia.Acciones ?? new List<AccionRetro>()
                    };

                if (_planDeHoy.EsPrimerDiaDeUnidad && string.Equals(ceremonia.Verbo, "V5", StringComparison.OrdinalIgnoreCase))
                    AbrirPlanning();
            }

            // 4 · el evento de hoy, o el tick que agenda uno para dentro de unos dias
            _eventoDeHoy = _eventos.EventoDeHoy(R);
            if (_eventoDeHoy == null) _eventos.TickSeleccion(R, this, _planDeHoy);
            else Decision = Presentar(_eventoDeHoy);

            // 5 · la ventana de los verbos
            Minijuego = _minijuegos.MinijuegoDeHoy(R, this);

            // 6 · los avisos, que salen DESPUES de que el evento de hoy se haya presentado:
            //     el telegrafiado es de un evento futuro, no del de hoy
            foreach (var aviso in _scheduler.AvisosDeHoy(R.DiaActual))
                brief.Avisos.Add($"[{aviso.Canal}] {aviso.Texto}");

            // 7 · la escena de hoy
            Beat = _narrativa.BeatDeHoy(R, this);

            brief.Derivadas = Derivadas();
            brief.WipActual = R.WipActual;
            brief.LimiteWip = R.LimiteWip;
            brief.LeadTime = brief.Derivadas.LeadTime;

            RegistrarSeries(brief.Derivadas);
            BriefDeHoy = brief;
            return brief;
        }

        private void AbrirPlanning() {
            var diasDeUnidad = Math.Max(1, Metodologia.Calendario.LongitudIteracion);
            PendingPlanning = new PlanningRequest {
                Unidad = _planDeHoy.EtiquetaUnidad + " " + (_planDeHoy.IndiceDeUnidad + 1),
                CapacidadSugerida = Math.Round(VelocidadActual() * diasDeUnidad, 1),
                PuntosPendientes = Math.Max(0, W.Alcance - W.Avance),
                Texto = "¿Con cuanto te comprometes?"
            };
            _avanceAlEmpezarUnidad = W.Avance;
        }

        /// <summary>
        /// El compromiso del sprint. Prometer por encima de la capacidad no da error: genera
        /// sobrecompromiso, y el sobrecompromiso genera deuda todos los dias hasta que se cierre.
        /// </summary>
        public void Comprometer(double puntos) {
            if (PendingPlanning == null) throw new InvalidOperationException("Hoy no hay planificacion abierta.");

            R.CompromisoActual = puntos;
            R.SobreCompromiso = Math.Max(0, puntos - PendingPlanning.CapacidadSugerida);
            R.CompromisosPorIteracion.Add(puntos);
            R.HistorialEstimaciones.Add(new Estimacion {
                Dia = R.DiaActual, ItemId = _planDeHoy.UnidadId, Estimado = puntos
            });
            PendingPlanning = null;
        }

        /// <summary>La decision de las 12:00. Maximo una con consecuencia permanente al dia.</summary>
        public void ResolverDecision(string opcionId) {
            if (Decision == null || _eventoDeHoy == null) throw new InvalidOperationException("Hoy no hay decision.");

            OpcionEvento opcion = null;
            foreach (var candidata in _eventoDeHoy.Opciones)
                if (string.Equals(candidata.Id, opcionId, StringComparison.Ordinal)) opcion = candidata;

            if (opcion == null) throw new InvalidOperationException($"'{opcionId}' no es una opcion de {_eventoDeHoy.Id}.");

            foreach (var presentada in Decision.Opciones)
                if (presentada.Id == opcionId && presentada.Bloqueada)
                    throw new InvalidOperationException($"La opcion '{opcionId}' esta bloqueada: {presentada.MotivoBloqueo}");

            var multiplicador = 1.0;
            var veredicto = Decision.Veredicto;
            if (veredicto != null && opcion.AmpliaAlcance) {
                multiplicador = veredicto.CosteMultiplicador;
                EffectApplier.Aplicar(W, veredicto.EfectosExtra);
                if (veredicto.ConsumeWip && R.WipActual > 0) R.WipActual--;
                if (veredicto.EnVentana) R.CambiosAceptados++; else R.CambiosNegociados++;
            } else if (Decision.EsCambioDeAlcance) {
                R.CambiosRechazados++;
            }

            var antes = W.ToString();
            EffectApplier.Aplicar(W, opcion.EfectosInmediatos, multiplicador);

            if (opcion.EfectosDiferidos != null)
                foreach (var diferido in opcion.EfectosDiferidos)
                    _scheduler.Encolar(R.DiaActual, diferido, _eventoDeHoy.Id);

            var rubrica = opcion.Rubrica ?? new RubricaOpcion();
            Registrar(_eventoDeHoy.Id, _eventoDeHoy.Nombre, opcion.Id, opcion.Texto,
                      rubrica.Veredicto, rubrica.Oa, rubrica.Razon, antes, opcion.NotaDeMetodologia);

            _eventos.RegistrarDisparo(_eventoDeHoy, R);
            _eventoDeHoy = null;
            Decision = null;
            R.Ventana = 1;
        }

        /// <summary>La vuelta de la escena. La UI no aplica nada: devuelve el resultado y el motor lo aplica.</summary>
        public void ResolverMinijuego(ResultadoMinijuego resultado) {
            if (Minijuego == null) throw new InvalidOperationException("Hoy no hay minijuego.");
            if (resultado == null) throw new ArgumentNullException(nameof(resultado));

            var antes = W.ToString();
            PuenteDelMotor.Aplicar(resultado, W, _scheduler, R.DiaActual);

            var entrada = PuenteDelMotor.ATraza(resultado, R.DiaActual, antes, W.ToString());
            Traza.Registrar(entrada);
            if (!string.IsNullOrEmpty(entrada.Oa)) Competencia.Acumular(entrada.Oa, entrada.Veredicto);

            _minijuegos.RegistrarJugado(Minijuego.MinijuegoId, R);
            Minijuego = null;
            R.Ventana = 2;
        }

        /// <summary>
        /// La retrospectiva. La unica mecanica del juego en la que el jugador modifica literalmente
        /// una constante de su propio proceso, y a partir de ese dia el modelo se comporta distinto.
        /// </summary>
        public void ElegirAccionRetro(string accionId) {
            if (PendingRetro == null) throw new InvalidOperationException("Hoy no hay retrospectiva.");

            AccionRetro accion = null;
            foreach (var candidata in PendingRetro.Acciones)
                if (string.Equals(candidata.Id, accionId, StringComparison.Ordinal)) accion = candidata;

            if (accion == null) throw new InvalidOperationException($"'{accionId}' no es una accion de la retro.");

            Coef.MultiplicarUno(accion.Coeficiente, accion.Multiplicador);
            R.AccionesRetroElegidas++;

            Registrar("RETRO", "Retrospectiva", accion.Id, accion.Texto,
                      Veredictos.Correcta, "OA-PROC-02", accion.Explicacion, null);

            PendingRetro = null;
        }

        /// <summary>
        /// ★ Las 18:00: el unico punto del motor donde el tiempo avanza, y la mecanica mas importante
        /// del juego. Quedarse da un 25 % mas de avance hoy y lo cobra durante el resto de la partida;
        /// irse a casa desbloquea el interludio de esa noche.
        /// </summary>
        public void TerminarDia(bool horasExtra) {
            ExigirFase2();

            if (horasExtra) {
                R.DiasConHorasExtra++;
                R.DiasSeguidosTrabajando++;
            } else {
                R.DiasSeguidosTrabajando = 0;
                R.VecesQueSeFueACasa++;
            }

            ForresterModel.AvanzarUnDia(W, R, Coef, Perfil.VelocidadBase, _velocidadBaseMult, horasExtra);

            if (Metodologia.Calendario.Tipo == TiposDeCalendario.Continuo) AvanzarFlujoContinuo();
            if (_planDeHoy != null && _planDeHoy.EsUltimoDiaDeUnidad) CerrarUnidad();

            R.Ventana = 3;
            if (R.DiaActual >= Perfil.DiasTotales) R.Fase = 3;
        }

        /// <summary>Kanban (§7.4): el throughput libera tarjetas y la reposicion vuelve a llenar el tablero.</summary>
        private void AvanzarFlujoContinuo() {
            const double PuntosPorTarjeta = 12.0;
            _throughputAcumulado += VelocidadActual();

            while (_throughputAcumulado >= PuntosPorTarjeta && R.WipActual > 0) {
                R.WipActual--;
                _throughputAcumulado -= PuntosPorTarjeta;
            }

            if (W.Avance < W.Alcance && R.WipActual < R.LimiteWip) R.WipActual++;
            if (R.WipActual > R.LimiteWip) R.VecesExcedioWip++;
        }

        private void CerrarUnidad() {
            R.EntregadoPorIteracion.Add(Math.Round(W.Avance - _avanceAlEmpezarUnidad, 2));
            R.SobreCompromiso = 0;
            _avanceAlEmpezarUnidad = W.Avance;
            _unidadAnterior = _planDeHoy.UnidadId;
        }

        public double VelocidadActual() { return Derivadas().Velocidad; }

        private Derived Derivadas() {
            return ForresterModel.Calcular(W, R, Coef, Perfil.VelocidadBase, _velocidadBaseMult);
        }

        private void RegistrarSeries(Derived d) {
            R.SerieAvance.Add(Math.Round(W.Avance, 2));
            R.SerieAlcance.Add(Math.Round(W.Alcance, 2));
            R.SerieRiesgo.Add(Math.Round(d.RiesgoLatente, 2));
            R.SerieWip.Add(R.WipActual);
            R.SerieLeadTime.Add(Math.Round(d.LeadTime, 2));
        }

        // ==================================================================== FASE 3

        /// <summary>El deploy (§4.9.3). La metodologia decide cuanto riesgo trae y cuantos defectos escapan.</summary>
        public LaunchResult EjecutarLanzamiento() {
            if (!Fase1Cerrada) throw new InvalidOperationException("El nivel no ha empezado.");
            if (NivelTerminado) throw new InvalidOperationException("El nivel ya se cerro.");

            R.Fase = 3;
            var d = Derivadas();
            var config = Metodologia.Lanzamiento ?? new LanzamientoConfig();

            var riesgo = Math.Min(100.0, d.RiesgoLatente * config.FactorRiesgo);
            var defectos = (int)Math.Round(((100.0 - W.Cobertura) / 12.0 + W.DeudaTecnica / 15.0) *
                                           (config.EntregaIncremental ? 1.0 : 1.3));
            var entregado = Math.Min(W.Avance, W.Alcance);
            var exito = riesgo < 62.0 && entregado >= Perfil.Umbrales.Exito.AvanceMinimo;

            if (!exito) W.Set("MoralEquipo", W.MoralEquipo - 8);

            Lanzamiento = new LaunchResult {
                Exito = exito,
                RiesgoDeLanzamiento = Math.Round(riesgo, 1),
                AlcanceEntregado = Math.Round(entregado, 1),
                AlcanceComprometido = Math.Round(W.Alcance, 1),
                DefectosEscapados = Math.Max(0, defectos),
                Texto = config.Texto,
                TextoMetodologia = exito ? config.TextoSiSale : config.TextoSiFalla
            };

            Registrar("LANZAMIENTO", "Lanzamiento", exito ? "exito" : "fallo",
                      $"{Lanzamiento.AlcanceEntregado} de {Lanzamiento.AlcanceComprometido} puntos",
                      exito ? Veredictos.Correcta : Veredictos.Incorrecta, "OA-PROC-03",
                      $"Riesgo de lanzamiento {Lanzamiento.RiesgoDeLanzamiento:F1}; " +
                      $"escaparon {Lanzamiento.DefectosEscapados} defectos.", null);

            R.Fase = 4;
            return Lanzamiento;
        }

        // ==================================================================== FASE 4

        /// <summary>
        /// El cierre. ★ INV-6: el puente entre los dos canales — volcar los stocks a los FLG_* — ocurre
        /// AQUI y en ningun otro sitio. Es el momento en que el proyecto se convierte en biografia.
        /// </summary>
        public DebriefReport Cerrar() {
            if (NivelTerminado) throw new InvalidOperationException("El nivel ya se cerro.");
            if (Lanzamiento == null) EjecutarLanzamiento();

            var metricas = MethodologyReport.CalcularMetricas(this);
            var umbralesFallados = ComprobarUmbrales();

            var reporte = new DebriefReport {
                NivelId = Perfil.Id, NivelNombre = Perfil.Nombre, Semilla = Semilla,
                Lanzamiento = Lanzamiento,
                CumpleUmbralesDeExito = umbralesFallados.Count == 0 && Lanzamiento.Exito,
                UmbralesFallados = umbralesFallados,
                Metodologia = MethodologyReport.Construir(DatosDeLaMetodologia(), _razonMetodologia,
                                                          Perfil.VolatilidadReal, metricas),
                Competencia = Competencia,
                Traza = Traza,
                ArquitecturaElegida = _arquitectura,
                CadenaCausal = CadenaCausal.Construir(Traza),
                BeatsPerdidos = _narrativa.ObligatoriosQueNoSalieron(R)
            };

            foreach (var kv in metricas) reporte.Metricas[kv.Key] = kv.Value;

            foreach (var arquitectura in Perfil.Fase1.Arquitecturas)
                if (string.Equals(arquitectura.Id, _arquitectura, StringComparison.Ordinal)) {
                    reporte.ArquitecturaVeredicto = arquitectura.Veredicto;
                    reporte.ArquitecturaRazon = arquitectura.Razon;
                }

            reporte.Final = reporte.CumpleUmbralesDeExito ? "completado" : "fallado";

            // ★ EL PUENTE. Una vez, aqui.
            PuenteDeFlags.VolcarAlCerrar(W, R, Flags);
            reporte.Flags = Flags.Copia();

            NivelTerminado = true;
            return reporte;
        }

        private List<string> ComprobarUmbrales() {
            var fallados = new List<string>();
            var exito = Perfil.Umbrales.Exito;
            var fallo = Perfil.Umbrales.Fallo;

            if (W.Avance < exito.AvanceMinimo)
                fallados.Add($"Avance {W.Avance:F1} por debajo del minimo ({exito.AvanceMinimo:F0}).");
            if (W.Cobertura < exito.CoberturaMinima)
                fallados.Add($"Cobertura {W.Cobertura:F1} por debajo del minimo ({exito.CoberturaMinima:F0}).");
            if (W.DeudaTecnica > exito.DeudaMaxima)
                fallados.Add($"Deuda tecnica {W.DeudaTecnica:F1} por encima del maximo ({exito.DeudaMaxima:F0}).");

            if (fallo.Deuda.HasValue && W.DeudaTecnica >= fallo.Deuda.Value)
                fallados.Add($"La deuda tecnica llego a {W.DeudaTecnica:F1}.");
            if (fallo.Moral.HasValue && W.MoralEquipo <= fallo.Moral.Value)
                fallados.Add($"La moral del equipo cayo a {W.MoralEquipo:F1}.");
            if (fallo.Dinero.HasValue && W.Dinero <= fallo.Dinero.Value)
                fallados.Add($"El presupuesto llego a {W.Dinero:F0}.");

            return fallados;
        }

        /// <summary>
        /// Copia el arbol de la metodologia al espejo que Evaluacion entiende. Es la traduccion que el
        /// comentario de DatosDeMetodologia anticipaba: asi Core.Evaluacion nunca conoce Core.Metodologia.
        /// </summary>
        private DatosDeMetodologia DatosDeLaMetodologia() {
            var datos = new DatosDeMetodologia {
                Id = Metodologia.Id, Nombre = Metodologia.Nombre, Familia = Metodologia.Familia,
                RazonesValidas = new List<string>(Metodologia.RazonesValidas ?? new List<string>()),
                RazonesTrampa = new List<string>(Metodologia.RazonesTrampa ?? new List<string>())
            };

            if (Metodologia.RubricaCierre == null || Metodologia.RubricaCierre.Practicas == null) return datos;

            foreach (var practica in Metodologia.RubricaCierre.Practicas)
                datos.Practicas.Add(new PracticaAEvaluar {
                    Id = practica.Id, Descripcion = practica.Descripcion, Metrica = practica.Metrica,
                    Comparador = practica.Comparador, Objetivo = practica.Objetivo,
                    RazonSiCumple = practica.RazonSiCumple, RazonSiFalla = practica.RazonSiFalla
                });

            return datos;
        }

        // ==================================================================== IStateContext

        public bool TryGetValue(string nombre, out double valor) {
            if (W.TryGet(nombre, out valor)) return true;
            if (R.TryGet(nombre, out valor)) return true;

            switch (nombre == null ? "" : nombre.Trim().ToLowerInvariant()) {
                case "diastotales": valor = Perfil.DiasTotales; return true;
                case "volatilidadreal": valor = Perfil.VolatilidadReal; return true;
                case "riesgolatente": valor = Derivadas().RiesgoLatente; return true;
                case "retrasorelativo": valor = RetrasoRelativo(); return true;
                default: valor = 0; return false;
            }
        }

        /// <summary>(esperado − avance) / esperado. Negativo si se va por delante del plan.</summary>
        private double RetrasoRelativo() {
            if (Perfil.DiasTotales <= 0 || W.Alcance <= 0) return 0;
            var esperado = W.Alcance * (R.DiaActual / (double)Perfil.DiasTotales);
            return esperado <= 0 ? 0 : (esperado - W.Avance) / esperado;
        }

        public double CallFunction(string nombre, string argumento) {
            if (string.Equals(nombre, VariablesDeSesion.FuncionDiasDesde, StringComparison.OrdinalIgnoreCase)) {
                int dia;
                // 999 y no 0: un evento que nunca ocurrio debe hacer cierto "diasDesde(x) > N"
                return R.Enfriamientos.TryGetValue(argumento ?? "", out dia) ? R.DiaActual - dia : 999;
            }

            if (string.Equals(nombre, VariablesDeSesion.FuncionOcurrencias, StringComparison.OrdinalIgnoreCase)) {
                int veces;
                return R.Ocurrencias.TryGetValue(argumento ?? "", out veces) ? veces : 0;
            }

            throw new InvalidOperationException(
                $"'{nombre}(...)' no es una funcion del estado. Validas: diasDesde, ocurrencias.");
        }

        // ==================================================================== IMetricasDelNivel

        int IMetricasDelNivel.DiasConHorasExtra { get { return R.DiasConHorasExtra; } }
        int IMetricasDelNivel.CambiosAceptados { get { return R.CambiosAceptados; } }
        int IMetricasDelNivel.AccionesRetroElegidas { get { return R.AccionesRetroElegidas; } }
        double IMetricasDelNivel.VolatilidadReal { get { return Perfil.VolatilidadReal; } }
        double IMetricasDelNivel.CoberturaAlCerrarDiseno { get { return R.CoberturaAlCerrarDiseno; } }
        int IMetricasDelNivel.VecesExcedioWip { get { return R.VecesExcedioWip; } }
        IReadOnlyList<double> IMetricasDelNivel.SerieWip { get { return R.SerieWip; } }
        IReadOnlyList<double> IMetricasDelNivel.SerieLeadTime { get { return R.SerieLeadTime; } }
        IReadOnlyList<double> IMetricasDelNivel.CompromisosPorIteracion { get { return R.CompromisosPorIteracion; } }
        IReadOnlyList<double> IMetricasDelNivel.EntregadoPorIteracion { get { return R.EntregadoPorIteracion; } }

        // ==================================================================== guardado

        public NivelEnCurso Capturar() {
            return new NivelEnCurso {
                PerfilDeNivelId = Perfil.Id,
                W = W.Clone(), R = R, Coef = Coef.Clone(),
                Traza = Traza, Competencia = Competencia, Fase1Cerrada = Fase1Cerrada,
                MetodologiaId = Metodologia == null ? null : Metodologia.Id,
                RazonMetodologia = _razonMetodologia,
                ArquitecturaId = _arquitectura, RazonArquitectura = _razonArquitectura,
                FichasDeCalidad = new Dictionary<string, int>(_fichasDeCalidad, StringComparer.Ordinal),
                VelocidadBaseMult = _velocidadBaseMult,
                ThroughputAcumulado = _throughputAcumulado,
                AvanceAlEmpezarUnidad = _avanceAlEmpezarUnidad,
                UnidadAnterior = _unidadAnterior,
                ColaDeEfectos = _scheduler.CopiaDeCola(),
                Telegrafiados = _scheduler.CopiaDeTelegrafiados(),
                ConsumosDelRng = _rng.Consumos
            };
        }

        public Dictionary<string, double> CapturarFlags() { return Flags.Copia(); }

        /// <summary>
        /// Rehidrata una sesion, en el orden NO NEGOCIABLE del §7.6.
        ///
        /// ★ Lo que NO se hace: volver a llamar a ElegirMetodologia, RepartirCalidad ni ElegirArquitectura.
        /// Sus efectos ya estan dentro de W y de Coef, y reaplicarlos daria cobertura gratis en cada
        /// recarga — sin dar error, que es lo peor. La unica excepcion son los pesos por tag del reparto
        /// de calidad, porque viven en el LevelProfile, que se recarga limpio del catalogo.
        /// </summary>
        public static GameSession Restaurar(Catalogo catalogo, string nivelId, FlagStore flags, int semilla,
                                            NivelEnCurso nivel) {
            if (nivel == null) throw new ArgumentNullException(nameof(nivel));

            var s = new GameSession(catalogo, nivelId, flags, semilla);

            // 1 · el azar, quemando las tiradas ya gastadas
            s._rng = DeterministicRng.Restaurar(s._rng.Semilla, nivel.ConsumosDelRng);

            // 2 · la agenda del tiempo
            s._scheduler = new EffectScheduler();
            s._scheduler.Restaurar(nivel.ColaDeEfectos, nivel.Telegrafiados);

            // 3 · el estado y lo privado que no se puede recalcular
            s.W = nivel.W; s.R = nivel.R; s.Coef = nivel.Coef;
            s.Traza = nivel.Traza; s.Competencia = nivel.Competencia;
            s.Fase1Cerrada = nivel.Fase1Cerrada;
            s._razonMetodologia = nivel.RazonMetodologia;
            s._arquitectura = nivel.ArquitecturaId;
            s._razonArquitectura = nivel.RazonArquitectura;
            s._velocidadBaseMult = nivel.VelocidadBaseMult;
            s._throughputAcumulado = nivel.ThroughputAcumulado;
            s._avanceAlEmpezarUnidad = nivel.AvanceAlEmpezarUnidad;
            s._unidadAnterior = nivel.UnidadAnterior;
            s._fichasDeCalidad = nivel.FichasDeCalidad ?? new Dictionary<string, int>(StringComparer.Ordinal);
            s.ReaplicarPesosDeCalidad();   // ← lo unico de la Fase 1 que SI hay que reaplicar

            // 4 · metodologia, reglas y directores, que dependen del azar y de la agenda ya restaurados
            if (!string.IsNullOrEmpty(nivel.MetodologiaId)) {
                MethodologyProfile metodologia;
                if (!catalogo.Metodologias.TryGetValue(nivel.MetodologiaId, out metodologia))
                    throw new SaveException($"La partida usa la metodologia '{nivel.MetodologiaId}', que no esta en el catalogo.");

                s.Metodologia = metodologia.Clone();
                s.Reglas = new MethodologyRules(s.Metodologia, s.Perfil.DiasTotales);
            }

            if (s.Fase1Cerrada) {
                s._eventos = new EventDirector(catalogo.Eventos, s.Perfil, s.Reglas, s._rng, s._scheduler);
                s._minijuegos = new MinigameDirector(catalogo.Minijuegos, s.Perfil, s._rng);
            }

            // 5 · el plan de hoy
            if (s.Reglas != null && s.R.DiaActual > 0) s._planDeHoy = s.Reglas.PlanFor(s.R.DiaActual);

            return s;
        }

        // ==================================================================== interno

        private void ExigirFase1Abierta() {
            if (Fase1Cerrada) throw new InvalidOperationException("La Fase 1 ya esta cerrada.");
        }

        private void ExigirFase2() {
            if (!Fase1Cerrada) throw new InvalidOperationException("Hay que cerrar la Fase 1 antes de empezar el bucle diario.");
            if (NivelTerminado) throw new InvalidOperationException("El nivel ya se cerro.");
        }

        /// <summary>Convierte un evento en algo que la pantalla puede pintar, con bloqueos y previsualizaciones.</summary>
        private PendingDecision Presentar(EventDefinition ev) {
            var telegrafiado = ev.Telegrafiado ?? new Telegrafiado();
            var decision = new PendingDecision {
                EventoId = ev.Id, Titulo = ev.Nombre,
                Canal = telegrafiado.Canal, TextoAviso = telegrafiado.Texto,
                Severidad = ev.Severidad, ObjetivoAprendizaje = ev.ObjetivoAprendizaje,
                EsCambioDeAlcance = ev.EsCambioDeAlcance
            };

            if (ev.EsCambioDeAlcance) decision.Veredicto = Reglas.EvaluarCambioDeAlcance(R.DiaActual);

            foreach (var opcion in ev.Opciones) {
                var multiplicador = decision.Veredicto != null && opcion.AmpliaAlcance
                    ? decision.Veredicto.CosteMultiplicador
                    : 1.0;

                var presentada = new OpcionPresentada {
                    Id = opcion.Id, Texto = opcion.Texto,
                    NotaDeMetodologia = opcion.NotaDeMetodologia,
                    Previsualizacion = EffectApplier.Previsualizar(W, opcion.EfectosInmediatos, multiplicador)
                };

                if (decision.Veredicto != null && opcion.AmpliaAlcance) {
                    if (!decision.Veredicto.Permitido) {
                        presentada.Bloqueada = true;
                        presentada.MotivoBloqueo = decision.Veredicto.Texto;
                    } else if (string.IsNullOrEmpty(presentada.NotaDeMetodologia)) {
                        presentada.NotaDeMetodologia = decision.Veredicto.Texto;
                    }
                }

                if (!presentada.Bloqueada && !ConditionEvaluator.EvaluarTodas(opcion.Requisitos, this)) {
                    presentada.Bloqueada = true;
                    presentada.MotivoBloqueo = "Ahora mismo no se dan las condiciones para esto.";
                }

                decision.Opciones.Add(presentada);
            }

            return decision;
        }

        private static string VeredictoDeRazon(List<string> validas, List<string> trampa, string razonId,
                                               string porDefecto = null) {
            if (razonId != null && validas != null && validas.Contains(razonId)) return Veredictos.Correcta;
            if (razonId != null && trampa != null && trampa.Contains(razonId)) return Veredictos.Incorrecta;
            return porDefecto != null && Veredictos.EsValido(porDefecto) ? porDefecto : Veredictos.Aceptable;
        }

        private string RazonDeMetodologia(string razonId) {
            if (razonId != null && Metodologia.RazonesTrampa != null && Metodologia.RazonesTrampa.Contains(razonId))
                return "Elegiste bien o mal, pero por un motivo que no sostiene la decision.";
            return Metodologia.Resumen;
        }

        /// <summary>Los seis sitios que escriben en la traza pasan todos por aqui.</summary>
        private void Registrar(string origen, string titulo, string opcionId, string opcionTexto,
                               string veredicto, string oa, string razon, string estadoAntes,
                               string notaDeMetodologia = null) {
            Traza.Registrar(new EntradaTraza {
                Dia = R.DiaActual, Origen = origen, Titulo = titulo,
                OpcionId = opcionId, OpcionTexto = opcionTexto,
                Veredicto = Veredictos.EsValido(veredicto) ? veredicto : Veredictos.Aceptable,
                Oa = oa, Razon = razon,
                EstadoAntes = estadoAntes ?? W.ToString(), EstadoDespues = W.ToString(),
                NotaDeMetodologia = notaDeMetodologia
            });

            if (!string.IsNullOrEmpty(oa))
                Competencia.Acumular(oa, Veredictos.EsValido(veredicto) ? veredicto : Veredictos.Aceptable);
        }
    }
}
