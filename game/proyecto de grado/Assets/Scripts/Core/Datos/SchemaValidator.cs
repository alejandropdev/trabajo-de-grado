using System;
using System.Collections.Generic;
using Nexus.Core.Evaluacion;
using Nexus.Core.Eventos;
using Nexus.Core.Jornada;
using Nexus.Core.Metodologia;
using Nexus.Core.Minijuegos;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Servicios;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Datos {
    /// <summary>Un catalogo que no se sostiene. INV-5: el juego no arranca, y dice cual es el campo.</summary>
    public sealed class SchemaException : Exception {
        public List<string> Errores { get; private set; }

        public SchemaException(string que, List<string> errores)
            : base($"{que} no es valido:\n - " + string.Join("\n - ", (errores ?? new List<string>()).ToArray())) {
            Errores = errores ?? new List<string>();
        }

        public SchemaException(string mensaje) : base(mensaje) {
            Errores = new List<string> { mensaje };
        }
    }

    /// <summary>
    /// C10b · El portero del contenido (§4.11). INV-5: **validacion al cargar, o no se carga**.
    ///
    /// La regla de oro de este archivo: un error de contenido tiene que doler AQUI, al arrancar, con el
    /// nombre del campo y el id del evento. Si no duele aqui, duele el dia 14 de una sesion de laboratorio
    /// de 90 minutos, delante de veinte estudiantes, y entonces ya no hay nada que hacer.
    ///
    /// Valida hasta las expresiones: una precondicion que dice "DeudaMoral &gt; 3" se caza al cargar,
    /// porque el evaluador se ejecuta contra un contexto que conoce todos los nombres legitimos y
    /// ninguno mas. Por eso ConditionEvaluator puede permitirse lanzar ante una variable desconocida:
    /// cuando el juego esta corriendo, ya se sabe que no las hay.
    /// </summary>
    public static class SchemaValidator {
        private static readonly string[] _metricasValidas = {
            MethodologyReport.Metricas.DiasConHorasExtra, MethodologyReport.Metricas.CambiosAceptados,
            MethodologyReport.Metricas.AccionesRetroElegidas, MethodologyReport.Metricas.VolatilidadReal,
            MethodologyReport.Metricas.CoberturaAlCerrarDiseno, MethodologyReport.Metricas.VecesExcedioWip,
            MethodologyReport.Metricas.WipMedio, MethodologyReport.Metricas.LeadTimeMedio,
            MethodologyReport.Metricas.DesviacionCompromisoPct
        };

        /// <summary>
        /// Un estado que conoce TODOS los nombres legitimos y ningun otro, y devuelve 0 para todos.
        /// No sirve para jugar: sirve para que evaluar una expresion revele si sus nombres existen.
        /// </summary>
        private sealed class ContextoDeValidacion : IStateContext {
            public bool TryGetValue(string nombre, out double valor) {
                valor = 0;
                if (WorldState.EsStock(nombre)) return true;
                if (VariablesDeSesion.EsVariable(nombre)) return true;
                foreach (var consultable in RuntimeState.Consultables)
                    if (string.Equals(consultable, nombre, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }

            public double CallFunction(string nombre, string argumento) {
                if (VariablesDeSesion.EsFuncion(nombre)) return 0;
                throw new InvalidOperationException(
                    $"'{nombre}(...)' no es una funcion del estado. Validas: " +
                    string.Join(", ", new List<string>(VariablesDeSesion.Funciones).ToArray()) + ".");
            }
        }

        private static readonly ContextoDeValidacion _contexto = new ContextoDeValidacion();

        // ============================================================ eventos

        public static List<string> ValidarEventos(List<EventDefinition> eventos) {
            var e = new List<string>();
            if (eventos == null) { e.Add("No hay bloque 'eventos'."); return e; }
            if (eventos.Count == 0) e.Add("El catalogo de eventos esta vacio.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ev in eventos) {
                if (ev == null) { e.Add("Hay un evento vacio en la lista."); continue; }
                if (string.IsNullOrEmpty(ev.Id)) { e.Add("Hay un evento sin 'id'."); continue; }
                if (!ids.Add(ev.Id)) e.Add($"El id de evento '{ev.Id}' esta repetido.");
                ValidarEvento(ev, e);
            }

            // Las cadenas tienen que apuntar a un evento que exista, o la consecuencia se pierde en silencio.
            foreach (var ev in eventos) {
                if (ev == null || ev.Opciones == null) continue;
                foreach (var opcion in ev.Opciones) {
                    if (opcion == null || opcion.EfectosDiferidos == null) continue;
                    foreach (var diferido in opcion.EfectosDiferidos) {
                        if (diferido == null || string.IsNullOrEmpty(diferido.EventoForzado)) continue;
                        if (!ids.Contains(diferido.EventoForzado))
                            e.Add($"{Donde(ev, opcion)}encadena '{diferido.EventoForzado}', que no esta en el catalogo.");
                    }
                }
            }

            return e;
        }

        private static void ValidarEvento(EventDefinition ev, List<string> e) {
            if (string.IsNullOrEmpty(ev.Nombre)) e.Add($"{ev.Id}: falta 'nombre'.");
            if (string.IsNullOrEmpty(ev.ObjetivoAprendizaje))
                e.Add($"{ev.Id}: falta 'objetivoAprendizaje'. Un evento que no evalua nada no deberia existir.");

            // 0 se admite a proposito: las buenas noticias (familia BUE) no consumen presupuesto de drama.
            if (ev.Severidad < 0 || ev.Severidad > 5)
                e.Add($"{ev.Id}: 'severidad' vale {ev.Severidad} y tiene que estar entre 0 y 5.");

            if (ev.PesoBase <= 0) e.Add($"{ev.Id}: 'pesoBase' vale {ev.PesoBase}; un evento con peso 0 no sale nunca.");
            if (ev.Enfriamiento < 0) e.Add($"{ev.Id}: 'enfriamiento' no puede ser negativo.");
            if (ev.MaxOcurrencias < 1) e.Add($"{ev.Id}: 'maxOcurrencias' vale {ev.MaxOcurrencias}; minimo 1.");

            if (ev.Fases == null || ev.Fases.Count == 0) {
                e.Add($"{ev.Id}: falta 'fases'; un evento que no sale en ninguna fase no sale nunca.");
            } else {
                foreach (var fase in ev.Fases)
                    if (!EsUnaDe(fase, FasesDelNivel.Planificacion, FasesDelNivel.Desarrollo,
                                 FasesDelNivel.Lanzamiento, FasesDelNivel.Evaluacion))
                        e.Add($"{ev.Id}: la fase '{fase}' no existe. Validas: planificacion, desarrollo, lanzamiento, evaluacion.");
            }

            // ★ INV-3 en el catalogo: sin aviso previo no hay evento.
            if (ev.Telegrafiado == null) {
                e.Add($"{ev.Id}: falta 'telegrafiado'. Ningun evento puede caer sin aviso previo (INV-3).");
            } else {
                if (string.IsNullOrEmpty(ev.Telegrafiado.Texto))
                    e.Add($"{ev.Id}: el telegrafiado no dice nada. El aviso ES la leccion: sin texto no hay señal que leer.");
                if (string.IsNullOrEmpty(ev.Telegrafiado.Canal))
                    e.Add($"{ev.Id}: el telegrafiado no tiene canal (log, correo, chat, ticket, standup, postit).");
                if (ev.Telegrafiado.DiasAntes < 1)
                    e.Add($"{ev.Id}: 'telegrafiado.diasAntes' vale {ev.Telegrafiado.DiasAntes}; avisar el mismo dia no es avisar.");
            }

            ValidarExpresiones(ev.Precondiciones, $"{ev.Id}: precondicion", e);

            if (ev.ModificadoresPeso != null)
                foreach (var m in ev.ModificadoresPeso) {
                    if (m == null) { e.Add($"{ev.Id}: hay un modificador de peso vacio."); continue; }
                    double ignorado;
                    if (string.IsNullOrEmpty(m.Variable) || !_contexto.TryGetValue(m.Variable, out ignorado))
                        e.Add($"{ev.Id}: el modificador de peso usa '{m.Variable}', que el estado no conoce.");
                    if (!EsUnaDe(m.Curva, CurvasDePeso.Lineal, CurvasDePeso.Inversa, CurvasDePeso.Cuadratica))
                        e.Add($"{ev.Id}: la curva '{m.Curva}' no existe. Validas: lineal, inversa, cuadratica.");
                }

            // ★ Si solo hay una opcion, no es un dilema.
            if (ev.Opciones == null || ev.Opciones.Count < 2) {
                e.Add($"{ev.Id}: hacen falta al menos dos opciones. Con una sola no es un dilema, es un aviso.");
                return;
            }

            var idsDeOpcion = new HashSet<string>(StringComparer.Ordinal);
            var algunaDiferida = false;

            foreach (var opcion in ev.Opciones) {
                if (opcion == null) { e.Add($"{ev.Id}: hay una opcion vacia."); continue; }
                if (string.IsNullOrEmpty(opcion.Id)) { e.Add($"{ev.Id}: hay una opcion sin 'id'."); continue; }
                if (!idsDeOpcion.Add(opcion.Id)) e.Add($"{ev.Id}: la opcion '{opcion.Id}' esta repetida.");
                if (string.IsNullOrEmpty(opcion.Texto)) e.Add($"{Donde(ev, opcion)}falta 'texto'.");

                // ★ Sin rubrica la decision no se puede evaluar y la Fase 4 se queda muda.
                if (opcion.Rubrica == null) {
                    e.Add($"{Donde(ev, opcion)}falta 'rubrica'. Sin ella la decision no se puede evaluar.");
                } else {
                    if (!Veredictos.EsValido(opcion.Rubrica.Veredicto))
                        e.Add($"{Donde(ev, opcion)}veredicto '{opcion.Rubrica.Veredicto}' desconocido. " +
                              "Validos: correcta, aceptable, incorrecta.");
                    if (string.IsNullOrEmpty(opcion.Rubrica.Razon))
                        e.Add($"{Donde(ev, opcion)}la rubrica no explica el porque. Es lo unico que el jugador lee al cerrar.");
                }

                ValidarEfectos(opcion.EfectosInmediatos, Donde(ev, opcion) + "efectosInmediatos", e);
                ValidarExpresiones(opcion.Requisitos, Donde(ev, opcion) + "requisito", e);

                if (opcion.EfectosDiferidos == null) continue;
                foreach (var diferido in opcion.EfectosDiferidos) {
                    if (diferido == null) { e.Add($"{Donde(ev, opcion)}hay un efecto diferido vacio."); continue; }
                    algunaDiferida = true;
                    if (diferido.EnDias < 0)
                        e.Add($"{Donde(ev, opcion)}un efecto diferido no puede vencer en el pasado ({diferido.EnDias}).");
                    ValidarEfectos(diferido.Efectos, Donde(ev, opcion) + "efectoDiferido", e);
                }
            }

            // Regla 5 del §A: al menos una opcion tiene coste diferido. Sin eso el evento no enseña P4.
            if (!algunaDiferida)
                e.Add($"{ev.Id}: ninguna opcion tiene coste diferido. Un evento sin consecuencia futura " +
                      "no enseña que toda velocidad es un prestamo.");
        }

        // ============================================================ nivel

        public static List<string> ValidarNivel(LevelProfile p) {
            var e = new List<string>();
            if (p == null) { e.Add("El perfil de nivel esta vacio."); return e; }

            var id = string.IsNullOrEmpty(p.Id) ? "(sin id)" : p.Id;
            if (string.IsNullOrEmpty(p.Id)) e.Add("Falta 'id'.");
            if (string.IsNullOrEmpty(p.Nombre)) e.Add($"{id}: falta 'nombre'.");

            if (p.DiasTotales < 2) e.Add($"{id}: 'diasTotales' vale {p.DiasTotales}; un nivel necesita al menos 2.");
            if (p.AlcanceInicial <= 0) e.Add($"{id}: 'alcanceInicial' vale {p.AlcanceInicial}; sin alcance no hay proyecto.");
            if (p.VelocidadBase <= 0) e.Add($"{id}: 'velocidadBase' vale {p.VelocidadBase}; el proyecto no avanzaria nunca.");
            if (p.VolatilidadReal < 0 || p.VolatilidadReal > 100)
                e.Add($"{id}: 'volatilidadReal' vale {p.VolatilidadReal} y tiene que estar entre 0 y 100.");
            if (p.NivelAndamiaje < 0 || p.NivelAndamiaje > 3)
                e.Add($"{id}: 'nivelAndamiaje' vale {p.NivelAndamiaje}; va de 0 (sin ayudas) a 3 (tutorial).");
            if (p.Briefing == null || p.Briefing.Length == 0)
                e.Add($"{id}: falta 'briefing'. La respuesta correcta de la arquitectura esta ahi, no en el catalogo de patrones.");

            foreach (var heredado in new[] { p.DeudaHeredada, p.CoberturaHeredada, p.DocumentacionHeredada })
                if (heredado < 0 || heredado > 100)
                    e.Add($"{id}: los valores heredados van de 0 a 100 (hay un {heredado}).");

            if (p.Coef == null) e.Add($"{id}: faltan los coeficientes.");
            else if (p.Coef.W == null || p.Coef.W.Length != 4)
                e.Add($"{id}: 'coef.w' debe tener exactamente 4 pesos de riesgo.");

            if (p.MetodologiasPermitidas == null || p.MetodologiasPermitidas.Length == 0)
                e.Add($"{id}: falta 'metodologiasPermitidas'; el jugador tiene que poder elegir algo.");

            ValidarDirector(id, p.Director, e);
            ValidarJornada(id, p.Jornada, e);
            ValidarFase1(id, p.Fase1, e);
            return e;
        }

        /// <summary>
        /// La jornada del dia continuo (§3.3). Se delega en el constructor de RelojDeJornada, que es
        /// quien de verdad ejercita la configuracion: la misma politica que con MethodologyRules.
        /// </summary>
        private static void ValidarJornada(string id, JornadaConfig j, List<string> e) {
            if (j == null) { e.Add($"{id}: falta el bloque 'jornada'."); return; }

            try {
                var _ = new RelojDeJornada(j);
            } catch (InvalidOperationException ex) {
                e.Add($"{id}: {ex.Message}");
            }
        }

        private static void ValidarDirector(string id, DirectorConfig d, List<string> e) {
            if (d == null) { e.Add($"{id}: falta el bloque 'director'."); return; }

            if (d.PresupuestoDrama == null || d.PresupuestoDrama.Length != 4)
                e.Add($"{id}: 'presupuestoDrama' debe tener exactamente cuatro valores, uno por fase " +
                      $"(hay {(d.PresupuestoDrama == null ? 0 : d.PresupuestoDrama.Length)}).");
            else
                foreach (var drama in d.PresupuestoDrama)
                    if (drama < 0) e.Add($"{id}: el presupuesto de drama no puede ser negativo.");

            if (d.MaxEventosPorDia < 1) e.Add($"{id}: 'maxEventosPorDia' vale {d.MaxEventosPorDia}; minimo 1.");
            if (d.EnfriamientoGlobal < 0) e.Add($"{id}: 'enfriamientoGlobal' no puede ser negativo.");
            if (d.VentanaTelegrafiado < 1)
                e.Add($"{id}: 'ventanaTelegrafiado' vale {d.VentanaTelegrafiado}; avisar el mismo dia no es avisar.");

            if (d.PesosPorTag == null) return;
            foreach (var kv in d.PesosPorTag)
                if (kv.Value < 0) e.Add($"{id}: el peso del tag '{kv.Key}' es negativo.");
        }

        private static void ValidarFase1(string id, Fase1Config f, List<string> e) {
            if (f == null) { e.Add($"{id}: falta el bloque 'fase1'."); return; }

            // --- calidad ---
            if (f.Calidad == null) {
                e.Add($"{id}: falta 'fase1.calidad'.");
            } else {
                if (f.Calidad.Fichas < 1) e.Add($"{id}: 'fase1.calidad.fichas' vale {f.Calidad.Fichas}; minimo 1.");
                if (f.Calidad.Atributos == null || f.Calidad.Atributos.Count == 0) {
                    e.Add($"{id}: 'fase1.calidad.atributos' esta vacio; no habria nada que repartir.");
                } else {
                    var idsAtributo = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var a in f.Calidad.Atributos) {
                        if (a == null || string.IsNullOrEmpty(a.Id)) { e.Add($"{id}: hay un atributo de calidad sin 'id'."); continue; }
                        if (!idsAtributo.Add(a.Id)) e.Add($"{id}: el atributo de calidad '{a.Id}' esta repetido.");
                        if (!string.IsNullOrEmpty(a.CoeficienteAfectado)) ValidarCoeficiente(id, a.CoeficienteAfectado, e);
                    }
                }
            }

            // --- razones ---
            var razones = new HashSet<string>(StringComparer.Ordinal);
            if (f.RazonesDisponibles != null)
                foreach (var r in f.RazonesDisponibles) {
                    if (r == null || string.IsNullOrEmpty(r.Id)) { e.Add($"{id}: hay una razon sin 'id'."); continue; }
                    if (!razones.Add(r.Id)) e.Add($"{id}: la razon '{r.Id}' esta repetida.");
                    if (string.IsNullOrEmpty(r.Texto)) e.Add($"{id}: la razon '{r.Id}' no tiene texto.");
                }

            // --- arquitecturas ---
            if (f.Arquitecturas == null || f.Arquitecturas.Count < 2) {
                e.Add($"{id}: hacen falta al menos dos arquitecturas; con una sola no hay decision.");
                return;
            }

            var adecuadas = 0;
            var idsArquitectura = new HashSet<string>(StringComparer.Ordinal);
            foreach (var a in f.Arquitecturas) {
                if (a == null || string.IsNullOrEmpty(a.Id)) { e.Add($"{id}: hay una arquitectura sin 'id'."); continue; }
                if (!idsArquitectura.Add(a.Id)) e.Add($"{id}: la arquitectura '{a.Id}' esta repetida.");
                if (a.EsLaAdecuada) adecuadas++;

                if (!Veredictos.EsValido(a.Veredicto))
                    e.Add($"{id}: la arquitectura '{a.Id}' tiene el veredicto '{a.Veredicto}', que no existe.");
                if (string.IsNullOrEmpty(a.Razon))
                    e.Add($"{id}: la arquitectura '{a.Id}' no explica su veredicto.");

                ValidarEfectos(a.Efectos, $"{id}: arquitectura '{a.Id}'", e);

                if (a.ModificadoresModelo != null)
                    foreach (var kv in a.ModificadoresModelo) ValidarCoeficiente($"{id}: arquitectura '{a.Id}'", kv.Key, e);

                foreach (var lista in new[] { a.RazonesValidas, a.RazonesTrampa }) {
                    if (lista == null) continue;
                    foreach (var razon in lista)
                        if (razones.Count > 0 && !razones.Contains(razon))
                            e.Add($"{id}: la arquitectura '{a.Id}' cita la razon '{razon}', que no esta en 'razonesDisponibles'.");
                }
            }

            // ★ Exactamente una: la que el briefing sostiene. Con ninguna no hay respuesta correcta que
            // enseñar; con dos, la Fase 4 no podria decir si acertaste.
            if (adecuadas == 0)
                e.Add($"{id}: ninguna arquitectura tiene 'esLaAdecuada'. El briefing tiene que sostener una.");
            else if (adecuadas > 1)
                e.Add($"{id}: hay {adecuadas} arquitecturas marcadas como adecuadas; solo puede haber una.");
        }

        // ============================================================ metodologia

        /// <summary>
        /// Delega el grueso en el constructor de MethodologyRules, que es lo que de verdad ejercita el
        /// perfil: al trocear el calendario se descubre que una etapa no tiene dias o que 'kappa' esta
        /// escrito 'kapa'. Duplicar aqui esas reglas seria tener dos fuentes de verdad.
        /// </summary>
        public static List<string> ValidarMetodologia(MethodologyProfile p, int diasTotales = 20) {
            var e = new List<string>();
            if (p == null) { e.Add("El perfil de metodologia esta vacio."); return e; }

            var id = string.IsNullOrEmpty(p.Id) ? "(sin id)" : p.Id;
            if (string.IsNullOrEmpty(p.Nombre)) e.Add($"{id}: falta 'nombre'.");

            try {
                var _ = new MethodologyRules(p, diasTotales);
            } catch (Exception ex) {
                e.Add(ex.Message);
                return e;   // sin reglas construidas, el resto de comprobaciones no tienen sentido
            }

            if (p.ModificadoresModelo != null)
                foreach (var kv in p.ModificadoresModelo) ValidarCoeficiente(id, kv.Key, e);

            if (p.Calendario != null && p.Calendario.Etapas != null)
                foreach (var etapa in p.Calendario.Etapas) {
                    if (etapa == null) continue;
                    ValidarEfectos(etapa.EfectosPorDia, $"{id}: etapa '{etapa.Id}' efectosPorDia", e);
                }

            if (p.Ceremonias != null)
                foreach (var c in p.Ceremonias) {
                    if (c == null) continue;
                    ValidarEfectos(c.Efectos, $"{id}: ceremonia '{c.Id}'", e);
                    if (c.Acciones == null) continue;
                    foreach (var a in c.Acciones)
                        if (a != null && string.IsNullOrEmpty(a.Explicacion))
                            e.Add($"{id}: la accion '{a.Id}' de '{c.Id}' no explica que cambia. Sin explicacion es magia.");
                }

            if (p.ReglasDeCambio != null && p.ReglasDeCambio.PenalizacionFueraDeVentana != null)
                ValidarEfectos(p.ReglasDeCambio.PenalizacionFueraDeVentana.EfectosExtra,
                               $"{id}: penalizacion fuera de ventana", e);

            if (p.Lanzamiento != null && p.Lanzamiento.FactorRiesgo <= 0)
                e.Add($"{id}: 'lanzamiento.factorRiesgo' vale {p.Lanzamiento.FactorRiesgo}; tiene que ser positivo.");

            // Una razon no puede ser valida y trampa a la vez: el jugador no podria acertar nunca.
            if (p.RazonesValidas != null && p.RazonesTrampa != null)
                foreach (var razon in p.RazonesValidas)
                    if (p.RazonesTrampa.Contains(razon))
                        e.Add($"{id}: la razon '{razon}' esta a la vez en 'razonesValidas' y en 'razonesTrampa'.");

            if (p.RubricaCierre == null || p.RubricaCierre.Practicas == null) return e;
            foreach (var practica in p.RubricaCierre.Practicas) {
                if (practica == null) { e.Add($"{id}: hay una practica de cierre vacia."); continue; }
                if (string.IsNullOrEmpty(practica.Id)) e.Add($"{id}: hay una practica de cierre sin 'id'.");

                if (!EsUnaDe(practica.Metrica, _metricasValidas))
                    e.Add($"{id}: la practica '{practica.Id}' mide '{practica.Metrica}', que no es una de las 9 metricas. " +
                          "Validas: " + string.Join(", ", _metricasValidas) + ".");

                if (MethodologyReport.Comparar(0, practica.Comparador, 0) == null)
                    e.Add($"{id}: la practica '{practica.Id}' usa el comparador '{practica.Comparador}'. " +
                          "Validos: >=, <=, >, <, ==.");

                if (string.IsNullOrEmpty(practica.RazonSiCumple) || string.IsNullOrEmpty(practica.RazonSiFalla))
                    e.Add($"{id}: la practica '{practica.Id}' no dice que significa cumplirla ni fallarla.");
            }

            return e;
        }

        // ============================================================ minijuegos

        /// <summary>
        /// El INDICE de minijuegos, que es la vista del motor. Si se le pasa la fuente, comprueba ademas
        /// que cada 'archivo' exista, parsee como escena y **coincida** con lo que el indice dice de el:
        /// dos vistas del mismo minijuego que se contradigan son un bug que no daria la cara hasta que
        /// alguien abriera esa escena concreta.
        /// </summary>
        public static List<string> ValidarMinijuegos(List<MinigameDefinition> minijuegos, ICatalogSource fuente = null) {
            var e = new List<string>();
            if (minijuegos == null) return e;   // un nivel puede no tener ventana de verbos

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var def in minijuegos) {
                if (def == null) { e.Add("Hay una entrada de minijuego vacia."); continue; }
                if (string.IsNullOrEmpty(def.Id)) { e.Add("Hay un minijuego sin 'id'."); continue; }
                if (!ids.Add(def.Id)) e.Add($"El id de minijuego '{def.Id}' esta repetido.");

                if (!Verbos.EsValido(def.Verbo))
                    e.Add($"{def.Id}: el verbo '{def.Verbo}' no existe. Validos: " +
                          string.Join(", ", new List<string>(Verbos.Todos).ToArray()) + ".");

                if (string.IsNullOrEmpty(def.ObjetivoAprendizaje))
                    e.Add($"{def.Id}: falta 'objetivoAprendizaje'; sin el, el nivel no puede filtrarlo.");

                if (string.IsNullOrEmpty(def.PresionDiegetica))
                    e.Add($"{def.Id}: falta 'presionDiegetica'. El reloj existe porque alguien espera, " +
                          "no porque si: sin ese texto solo queda un cronometro.");

                if (def.Reloj < 10 || def.Reloj > 600)
                    e.Add($"{def.Id}: 'reloj' vale {def.Reloj} segundos; el diseño pide entre 60 y 120.");

                if (def.PesoBase <= 0) e.Add($"{def.Id}: 'pesoBase' vale {def.PesoBase}; no saldria nunca.");
                if (def.Enfriamiento < 0) e.Add($"{def.Id}: 'enfriamiento' no puede ser negativo.");
                if (def.MaxOcurrencias < 1) e.Add($"{def.Id}: 'maxOcurrencias' vale {def.MaxOcurrencias}; minimo 1.");

                if (def.Fases != null)
                    foreach (var fase in def.Fases)
                        if (!EsUnaDe(fase, FasesDelNivel.Planificacion, FasesDelNivel.Desarrollo,
                                     FasesDelNivel.Lanzamiento, FasesDelNivel.Evaluacion))
                            e.Add($"{def.Id}: la fase '{fase}' no existe.");

                ValidarExpresiones(def.Precondiciones, $"{def.Id}: precondicion", e);

                if (string.IsNullOrEmpty(def.Archivo)) {
                    e.Add($"{def.Id}: falta 'archivo'; el motor no sabria que escena abrir.");
                    continue;
                }
                if (fuente != null) ValidarEscena(def, fuente, e);
            }

            return e;
        }

        private static void ValidarEscena(MinigameDefinition def, ICatalogSource fuente, List<string> e) {
            if (!fuente.Existe(def.Archivo)) {
                e.Add($"{def.Id}: apunta a '{def.Archivo}', que no existe.");
                return;
            }

            MinijuegoDef escena;
            try {
                escena = CatalogoMinijuegos.Parsear(fuente.LeerCatalogo(def.Archivo));
            } catch (Exception ex) {
                e.Add($"{def.Id}: '{def.Archivo}' no se pudo leer. {ex.Message}");
                return;
            }

            if (!string.Equals(escena.Id, def.Id, StringComparison.Ordinal))
                e.Add($"{def.Id}: el archivo '{def.Archivo}' dice llamarse '{escena.Id}'.");

            if (Verbos.Normalizar(escena.Verbo) != Verbos.Normalizar(def.Verbo))
                e.Add($"{def.Id}: el indice dice verbo '{def.Verbo}' y la escena dice '{escena.Verbo}'.");

            if (!string.IsNullOrEmpty(escena.ObjetivoAprendizaje) &&
                !string.Equals(escena.ObjetivoAprendizaje, def.ObjetivoAprendizaje, StringComparison.Ordinal))
                e.Add($"{def.Id}: el indice dice OA '{def.ObjetivoAprendizaje}' y la escena dice " +
                      $"'{escena.ObjetivoAprendizaje}'.");
        }

        // ============================================================ narrativa y flags

        /// <summary>
        /// El catalogo narrativo. Lo mas util que valida son las expresiones de COLOREO: una condicion
        /// mal escrita ahi no daria error, daria una escena que siempre suena igual — y eso no se nota
        /// jugando, solo se nota leyendo el JSON con lupa.
        /// </summary>
        public static List<string> ValidarNarrativa(List<NarrativeBeat> beats) {
            var e = new List<string>();
            if (beats == null) return e;   // una partida puede correr sin trama

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var beat in beats) {
                if (beat == null) { e.Add("Hay un beat narrativo vacio."); continue; }
                if (string.IsNullOrEmpty(beat.Id)) { e.Add("Hay un beat sin 'id'."); continue; }
                if (!ids.Add(beat.Id)) e.Add($"El id de beat '{beat.Id}' esta repetido.");

                if (string.IsNullOrEmpty(beat.Nombre)) e.Add($"{beat.Id}: falta 'nombre'.");

                if (!PrioridadDeBeat.EsValida(beat.Prioridad))
                    e.Add($"{beat.Id}: 'prioridad' vale '{beat.Prioridad}'. Validas: obligatorio, opcional.");

                var ventana = beat.Ventana;
                if (ventana != null) {
                    if (ventana.DiaMin < 1) e.Add($"{beat.Id}: 'ventana.diaMin' vale {ventana.DiaMin}; minimo 1.");
                    if (ventana.DiaMax < ventana.DiaMin)
                        e.Add($"{beat.Id}: la ventana va del dia {ventana.DiaMin} al {ventana.DiaMax}, " +
                              "que es antes de empezar.");
                }

                ValidarExpresiones(beat.Precondiciones, $"{beat.Id}: precondicion", e);

                if (beat.Coloreo == null) continue;
                foreach (var kv in beat.Coloreo) {
                    if (string.IsNullOrEmpty(kv.Value))
                        e.Add($"{beat.Id}: la variante de coloreo de '{kv.Key}' esta vacia.");
                    if (string.Equals(kv.Key, NarrativeBeat.VarianteDefecto, StringComparison.OrdinalIgnoreCase))
                        continue;
                    ValidarExpresiones(new List<string> { kv.Key }, $"{beat.Id}: coloreo", e);
                }
            }

            return e;
        }

        /// <summary>El censo de flags: la lista completa en una pagina, para que se pueda auditar de un vistazo.</summary>
        public static List<string> ValidarFlags(List<DefinicionDeFlag> flags) {
            var e = new List<string>();
            if (flags == null) return e;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var hayDeudaMoral = false;

            foreach (var def in flags) {
                if (def == null) { e.Add("Hay una definicion de flag vacia."); continue; }
                if (string.IsNullOrEmpty(def.Id)) { e.Add("Hay un flag sin 'id'."); continue; }

                if (!def.Id.StartsWith(FlagStore.Prefijo, StringComparison.Ordinal))
                    e.Add($"'{def.Id}' no empieza por {FlagStore.Prefijo}.");
                if (!ids.Add(def.Id)) e.Add($"El flag '{def.Id}' esta repetido en el censo.");

                if (!EjesDeFlag.EsValido(def.Eje))
                    e.Add($"{def.Id}: el eje '{def.Eje}' no existe. Validos: " +
                          string.Join(", ", new List<string>(EjesDeFlag.Todos).ToArray()) + ".");

                if (string.IsNullOrEmpty(def.Descripcion))
                    e.Add($"{def.Id}: falta 'descripcion'. El censo existe para documentar; sin texto no documenta nada.");

                if (def.SinTecho && def.Max.HasValue)
                    e.Add($"{def.Id}: dice 'sinTecho' y a la vez declara un maximo de {def.Max.Value}.");

                if (def.Min.HasValue && def.Max.HasValue && def.Min.Value > def.Max.Value)
                    e.Add($"{def.Id}: el minimo ({def.Min.Value}) es mayor que el maximo ({def.Max.Value}).");

                if (def.Min.HasValue && def.Inicial < def.Min.Value)
                    e.Add($"{def.Id}: el valor inicial ({def.Inicial}) esta por debajo de su minimo.");
                if (def.Max.HasValue && def.Inicial > def.Max.Value)
                    e.Add($"{def.Id}: el valor inicial ({def.Inicial}) esta por encima de su maximo.");

                if (string.Equals(def.Id, FlagStore.DeudaMoral, StringComparison.Ordinal)) {
                    hayDeudaMoral = true;
                    if (!def.NoBaja)
                        e.Add($"{FlagStore.DeudaMoral} tiene que declarar 'noBaja': la deuda moral no se paga. " +
                              "Es la tesis del juego, no un parametro de balanceo.");
                }
            }

            if (ids.Count > 0 && !hayDeudaMoral)
                e.Add($"El censo no incluye {FlagStore.DeudaMoral}, que el motor necesita.");

            return e;
        }

        // ============================================================ el catalogo entero

        /// <summary>Las comprobaciones que solo se pueden hacer con todo cargado a la vez.</summary>
        public static List<string> ValidarCatalogo(Catalogo catalogo, ICatalogSource fuente = null) {
            var e = new List<string>();
            if (catalogo == null) { e.Add("No hay catalogo."); return e; }

            e.AddRange(ValidarEventos(catalogo.Eventos));
            e.AddRange(ValidarMinijuegos(catalogo.Minijuegos, fuente));
            e.AddRange(ValidarNarrativa(catalogo.Beats));
            e.AddRange(ValidarFlags(catalogo.Flags));

            if (catalogo.Niveles == null || catalogo.Niveles.Count == 0) e.Add("No hay ningun perfil de nivel.");
            if (catalogo.Metodologias == null || catalogo.Metodologias.Count == 0) e.Add("No hay ninguna metodologia.");
            if (catalogo.Niveles == null || catalogo.Metodologias == null) return e;

            foreach (var kv in catalogo.Metodologias) e.AddRange(ValidarMetodologia(kv.Value));

            foreach (var kv in catalogo.Niveles) {
                var nivel = kv.Value;
                e.AddRange(ValidarNivel(nivel));
                if (nivel == null || nivel.MetodologiasPermitidas == null) continue;

                foreach (var metodologia in nivel.MetodologiasPermitidas)
                    if (!catalogo.Metodologias.ContainsKey(metodologia))
                        e.Add($"{nivel.Id}: permite la metodologia '{metodologia}', que no esta en metodologias/.");

                // Cada metodologia tiene que sostener ESTE nivel, con SUS dias.
                foreach (var metodologia in nivel.MetodologiasPermitidas) {
                    MethodologyProfile perfil;
                    if (!catalogo.Metodologias.TryGetValue(metodologia, out perfil)) continue;
                    foreach (var error in ValidarMetodologia(perfil, nivel.DiasTotales))
                        e.Add($"{nivel.Id} con '{metodologia}': {error}");
                }
            }

            return e;
        }

        // ============================================================ utilidades

        /// <summary>
        /// Evalua la expresion contra un estado que conoce todos los nombres legitimos. No importa el
        /// resultado: importa que no lance. Traduce las excepciones del evaluador y del aplicador de
        /// efectos, que no conocen la capa de datos, a errores de esquema con su contexto.
        /// </summary>
        private static void ValidarExpresiones(List<string> expresiones, string contexto, List<string> e) {
            if (expresiones == null) return;
            foreach (var expresion in expresiones) {
                if (string.IsNullOrEmpty(expresion)) { e.Add($"{contexto}: hay una expresion vacia."); continue; }
                try {
                    ConditionEvaluator.Evaluar(expresion, _contexto);
                } catch (ExpresionInvalidaException ex) {
                    e.Add($"{contexto}: {ex.Message}");
                } catch (InvalidOperationException ex) {
                    e.Add($"{contexto} \"{expresion}\": {ex.Message}");
                }
            }
        }

        private static void ValidarEfectos(Dictionary<string, object> efectos, string contexto, List<string> e) {
            try {
                EffectApplier.Validar(efectos, contexto);
            } catch (InvalidOperationException ex) {
                e.Add(ex.Message);
            }
        }

        private static void ValidarCoeficiente(string contexto, string nombre, List<string> e) {
            try {
                new Coeficientes().MultiplicarUno(nombre, 1.0);
            } catch (InvalidOperationException ex) {
                e.Add($"{contexto}: {ex.Message}");
            }
        }

        private static string Donde(EventDefinition ev, OpcionEvento opcion) {
            return $"{ev.Id} opcion '{(opcion == null ? "?" : opcion.Id)}': ";
        }

        private static bool EsUnaDe(string valor, params string[] validos) {
            foreach (var v in validos)
                if (string.Equals(valor, v, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
