using System;
using System.Collections.Generic;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Metodologia {
    /// <summary>Un tramo del calendario: una iteracion, una etapa, o el nivel entero si el flujo es continuo.</summary>
    public sealed class Tramo {
        public int Indice;          // 0-based
        public string Id;
        public string Nombre;
        public int DiaInicio;       // 1-based, inclusive
        public int DiaFin;          // inclusive
        public string Texto;
        public Dictionary<string, double> MultiplicadorPesosPorTag;
        public Dictionary<string, object> EfectosPorDia;

        public bool Contiene(int dia) { return dia >= DiaInicio && dia <= DiaFin; }
    }

    /// <summary>
    /// C5 · La metodologia en funcionamiento (§4.5.2). Traduce el perfil de datos a respuestas concretas:
    /// que toca hoy, si hoy se puede cambiar el alcance, cuanto pesa un tag.
    ///
    /// **Ni un solo `if (id == "scrum")`.** Todo sale de MethodologyProfile. Si alguna vez hace falta
    /// escribir aqui el nombre de una metodologia, es que falta un campo en el JSON.
    ///
    /// El constructor VALIDA el perfil y lanza si no se sostiene. Esta puesto aqui a proposito y no solo
    /// en SchemaValidator (M6): construir las reglas es lo que de verdad ejercita el perfil, asi que el
    /// validador de catalogos podra limitarse a llamar a este constructor y traducir la excepcion.
    /// </summary>
    public sealed class MethodologyRules {
        private readonly List<Tramo> _tramos = new List<Tramo>();

        public MethodologyProfile Profile { get; private set; }
        public int DiasTotales { get; private set; }

        public IReadOnlyList<Tramo> Tramos { get { return _tramos; } }
        public int NumeroDeUnidades { get { return _tramos.Count; } }

        public double MultiplicadorDrama {
            get {
                return Profile.ModificadoresDirector == null ? 1.0 : Profile.ModificadoresDirector.MultiplicadorDrama;
            }
        }

        public MethodologyRules(MethodologyProfile profile, int diasTotales) {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (diasTotales < 1)
                throw new ArgumentOutOfRangeException(nameof(diasTotales), diasTotales,
                    "Un nivel necesita al menos un dia.");

            Profile = profile;
            DiasTotales = diasTotales;

            Validar();
            ConstruirTramos();
        }

        // ------------------------------------------------------------------ validacion

        private void Validar() {
            var id = string.IsNullOrEmpty(Profile.Id) ? "(sin id)" : Profile.Id;

            if (string.IsNullOrEmpty(Profile.Id))
                throw Invalido(id, "la metodologia no tiene 'id'.");

            if (!EsUnoDe(Profile.Familia, FamiliasDeMetodologia.Agil, FamiliasDeMetodologia.Tradicional))
                throw Invalido(id, $"'familia' vale '{Profile.Familia}' y solo admite 'agil' o 'tradicional'. " +
                                   "De ahi sale si la eleccion encajaba con la volatilidad real del nivel.");

            if (!EsUnoDe(Profile.TableroPrincipal,
                         TablerosPrincipales.Burndown, TablerosPrincipales.CurvaS, TablerosPrincipales.Cfd))
                throw Invalido(id, $"'tableroPrincipal' vale '{Profile.TableroPrincipal}' y solo admite " +
                                   "'burndown', 'curvaS' o 'cfd'.");

            ValidarCalendario(id);
            ValidarCeremonias(id);
            ValidarVentanas(id);

            if (Profile.RubricaCierre == null || Profile.RubricaCierre.Practicas == null ||
                Profile.RubricaCierre.Practicas.Count == 0)
                throw Invalido(id, "falta 'rubricaCierre.practicas': sin ella no hay con que juzgar al jugador " +
                                   "al cerrar el nivel, y la Fase 4 se quedaria muda.");
        }

        private void ValidarCalendario(string id) {
            var cal = Profile.Calendario;
            if (cal == null) throw Invalido(id, "falta el bloque 'calendario'.");

            switch (Normalizar(cal.Tipo)) {
                case "iterativo":
                    if (cal.LongitudIteracion <= 0 || cal.Iteraciones <= 0)
                        throw Invalido(id, "un calendario iterativo necesita 'longitudIteracion' e 'iteraciones' " +
                                           $"mayores que cero (hay {cal.LongitudIteracion} y {cal.Iteraciones}).");
                    break;

                case "secuencial":
                    if (cal.Etapas == null || cal.Etapas.Count == 0)
                        throw Invalido(id, "un calendario secuencial necesita al menos una etapa en 'calendario.etapas'.");
                    for (var i = 0; i < cal.Etapas.Count; i++) {
                        var etapa = cal.Etapas[i];
                        if (etapa == null) throw Invalido(id, $"la etapa {i} esta vacia.");
                        if (string.IsNullOrEmpty(etapa.Id)) throw Invalido(id, $"la etapa {i} no tiene 'id'.");
                        if (etapa.Dias <= 0) throw Invalido(id, $"la etapa '{etapa.Id}' dura {etapa.Dias} dias.");
                    }
                    break;

                case "continuo":
                    if (cal.LimiteWipInicial <= 0)
                        throw Invalido(id, $"un calendario continuo necesita 'limiteWipInicial' > 0 " +
                                           $"(hay {cal.LimiteWipInicial}); sin limite, Kanban no enseña nada.");
                    break;

                default:
                    throw Invalido(id, $"'calendario.tipo' vale '{cal.Tipo}' y solo admite " +
                                       "'iterativo', 'secuencial' o 'continuo'.");
            }
        }

        private void ValidarCeremonias(string id) {
            if (Profile.Ceremonias == null) return;
            var coeficientesDePrueba = new Coeficientes();

            foreach (var c in Profile.Ceremonias) {
                if (c == null) throw Invalido(id, "hay una ceremonia vacia.");
                if (string.IsNullOrEmpty(c.Id)) throw Invalido(id, "hay una ceremonia sin 'id'.");

                if (!EsUnoDe(c.Cuando, CuandoAplica.Diario, CuandoAplica.InicioIteracion, CuandoAplica.FinIteracion,
                             CuandoAplica.FinEtapa, CuandoAplica.Cada, CuandoAplica.CuandoSeLiberaWip))
                    throw Invalido(id, $"la ceremonia '{c.Id}' tiene 'cuando' = '{c.Cuando}', que no existe. " +
                                       "Validos: diario, inicioIteracion, finIteracion, finEtapa, cada, cuandoSeLiberaWip.");

                if (EsUnoDe(c.Cuando, CuandoAplica.Cada) && c.CadaNDias <= 0)
                    throw Invalido(id, $"la ceremonia '{c.Id}' es de tipo 'cada' pero 'cadaNDias' vale {c.CadaNDias}.");

                if (c.CosteDias < 0)
                    throw Invalido(id, $"la ceremonia '{c.Id}' tiene un coste negativo de dias.");

                if (c.AjustaCoeficiente && (c.Acciones == null || c.Acciones.Count == 0))
                    throw Invalido(id, $"la ceremonia '{c.Id}' dice ajustar un coeficiente pero no ofrece acciones. " +
                                       "Una retrospectiva sin opciones no es una retrospectiva.");

                if (c.Acciones == null) continue;
                foreach (var accion in c.Acciones) {
                    if (accion == null) throw Invalido(id, $"la ceremonia '{c.Id}' tiene una accion vacia.");
                    if (string.IsNullOrEmpty(accion.Id))
                        throw Invalido(id, $"una accion de '{c.Id}' no tiene 'id'.");
                    try {
                        coeficientesDePrueba.Clone().MultiplicarUno(accion.Coeficiente, accion.Multiplicador);
                    } catch (InvalidOperationException ex) {
                        throw Invalido(id, $"la accion '{accion.Id}' de '{c.Id}': {ex.Message}");
                    }
                }
            }
        }

        private void ValidarVentanas(string id) {
            var reglas = Profile.ReglasDeCambio;
            if (reglas == null || reglas.Ventanas == null) return;

            foreach (var ventana in reglas.Ventanas)
                if (!EsUnoDe(ventana, VentanasDeCambio.Siempre, VentanasDeCambio.EntreIteraciones, VentanasDeCambio.Hito))
                    throw Invalido(id, $"'reglasDeCambio.ventanas' contiene '{ventana}', que no existe. " +
                                       "Validos: siempre, entreIteraciones, hito.");
        }

        // ------------------------------------------------------------------ tramos

        /// <summary>
        /// Trocea el nivel. Si los tramos no llegan a cubrir DiasTotales por redondeo, el ULTIMO se estira;
        /// si se pasan, se recorta y los que empiezan despues del final no se crean. Asi ningun dia del
        /// nivel se queda sin unidad y ninguna unidad apunta a dias que no existen.
        /// </summary>
        private void ConstruirTramos() {
            var cal = Profile.Calendario;

            switch (Normalizar(cal.Tipo)) {
                case "iterativo":
                    for (var i = 0; i < cal.Iteraciones; i++) {
                        var inicio = i * cal.LongitudIteracion + 1;
                        if (inicio > DiasTotales) break;
                        _tramos.Add(new Tramo {
                            Indice = i,
                            Id = "it" + (i + 1),
                            Nombre = cal.EtiquetaUnidad + " " + (i + 1),
                            DiaInicio = inicio,
                            DiaFin = inicio + cal.LongitudIteracion - 1,
                            Texto = null
                        });
                    }
                    break;

                case "secuencial": {
                    var inicio = 1;
                    for (var i = 0; i < cal.Etapas.Count; i++) {
                        if (inicio > DiasTotales) break;
                        var etapa = cal.Etapas[i];
                        _tramos.Add(new Tramo {
                            Indice = i,
                            Id = etapa.Id,
                            Nombre = string.IsNullOrEmpty(etapa.Nombre) ? etapa.Id : etapa.Nombre,
                            DiaInicio = inicio,
                            DiaFin = inicio + etapa.Dias - 1,
                            Texto = etapa.Texto,
                            MultiplicadorPesosPorTag = etapa.MultiplicadorPesosPorTag,
                            EfectosPorDia = etapa.EfectosPorDia
                        });
                        inicio += etapa.Dias;
                    }
                    break;
                }

                default:   // continuo
                    _tramos.Add(new Tramo {
                        Indice = 0,
                        Id = "flujo",
                        Nombre = cal.EtiquetaUnidad,
                        DiaInicio = 1,
                        DiaFin = DiasTotales
                    });
                    break;
            }

            if (_tramos.Count == 0)
                _tramos.Add(new Tramo { Indice = 0, Id = "unico", Nombre = cal.EtiquetaUnidad, DiaInicio = 1, DiaFin = DiasTotales });

            _tramos[_tramos.Count - 1].DiaFin = DiasTotales;
        }

        // ------------------------------------------------------------------ API

        /// <summary>
        /// El plan de un dia. Los dias fuera del nivel se sujetan al primer o al ultimo tramo en vez de lanzar:
        /// decidir cuando se acaba el nivel es cosa de GameSession, no del calendario.
        /// </summary>
        public DayPlan PlanFor(int dia) {
            var tramo = TramoDe(dia);
            var diaSujeto = dia < tramo.DiaInicio ? tramo.DiaInicio : (dia > tramo.DiaFin ? tramo.DiaFin : dia);

            var plan = new DayPlan {
                Dia = dia,
                EtiquetaUnidad = Profile.Calendario.EtiquetaUnidad,
                UnidadId = tramo.Id,
                IndiceDeUnidad = tramo.Indice,
                DiaDentroDeUnidad = diaSujeto - tramo.DiaInicio + 1,
                EsPrimerDiaDeUnidad = diaSujeto == tramo.DiaInicio,
                EsUltimoDiaDeUnidad = diaSujeto == tramo.DiaFin,
                TextoDeUnidad = tramo.Texto
            };

            if (tramo.MultiplicadorPesosPorTag != null)
                foreach (var kv in tramo.MultiplicadorPesosPorTag) plan.MultiplicadorTagsDeEtapa[kv.Key] = kv.Value;

            if (tramo.EfectosPorDia != null)
                foreach (var kv in tramo.EfectosPorDia) plan.EfectosDeEtapa[kv.Key] = kv.Value;

            if (Profile.Ceremonias != null)
                foreach (var ceremonia in Profile.Ceremonias)
                    if (AplicaHoy(ceremonia, plan)) plan.Ceremonias.Add(ceremonia);

            return plan;
        }

        /// <summary>Que dice la metodologia si hoy el cliente quiere cambiar el alcance.</summary>
        public VeredictoCambio EvaluarCambioDeAlcance(int dia) {
            var plan = PlanFor(dia);
            var reglas = Profile.ReglasDeCambio ?? new ReglasDeCambio();

            if (EstaEnVentana(plan, reglas))
                return new VeredictoCambio {
                    Permitido = true,
                    EnVentana = true,
                    CosteMultiplicador = reglas.CosteMultiplicador,
                    ConsumeWip = reglas.ConsumeWip,
                    Texto = reglas.TextoEnVentana
                };

            var pena = reglas.PenalizacionFueraDeVentana ?? new PenalizacionFueraDeVentana();
            var veredicto = new VeredictoCambio {
                Permitido = pena.Permitido,
                EnVentana = false,
                CosteMultiplicador = reglas.CosteMultiplicador * pena.CosteMultiplicador,
                ConsumeWip = reglas.ConsumeWip,
                Texto = pena.Texto
            };

            if (pena.EfectosExtra != null)
                foreach (var kv in pena.EfectosExtra) veredicto.EfectosExtra[kv.Key] = kv.Value;

            return veredicto;
        }

        /// <summary>
        /// El multiplicador que la metodologia aplica a un tag, EN CASCADA: primero lo que la metodologia
        /// diga siempre, y encima lo que diga la etapa de hoy.
        /// </summary>
        public double MultiplicadorTag(string tag, DayPlan plan) {
            if (string.IsNullOrEmpty(tag)) return 1.0;
            var factor = 1.0;

            double delPerfil;
            if (Profile.ModificadoresDirector != null && Profile.ModificadoresDirector.MultiplicadorPesosPorTag != null &&
                Profile.ModificadoresDirector.MultiplicadorPesosPorTag.TryGetValue(tag, out delPerfil))
                factor *= delPerfil;

            double deLaEtapa;
            if (plan != null && plan.MultiplicadorTagsDeEtapa != null &&
                plan.MultiplicadorTagsDeEtapa.TryGetValue(tag, out deLaEtapa))
                factor *= deLaEtapa;

            return factor;
        }

        /// <summary>
        /// Dos motivos para bloquear un evento: que esta metodologia lo prohiba, o que el evento solo
        /// exista para otras. Una lista 'soloMetodologias' vacia significa "vale para todas".
        /// </summary>
        public bool EventoBloqueado(string eventoId, List<string> soloMetodologias) {
            var bloqueados = Profile.ModificadoresDirector == null ? null : Profile.ModificadoresDirector.EventosBloqueados;
            if (bloqueados != null)
                foreach (var b in bloqueados)
                    if (string.Equals(b, eventoId, StringComparison.OrdinalIgnoreCase)) return true;

            if (soloMetodologias == null || soloMetodologias.Count == 0) return false;

            foreach (var m in soloMetodologias)
                if (string.Equals(m, Profile.Id, StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }

        public Ceremonia CeremoniaPorId(string id) {
            if (Profile.Ceremonias == null || string.IsNullOrEmpty(id)) return null;
            foreach (var c in Profile.Ceremonias)
                if (c != null && string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase)) return c;
            return null;
        }

        // ------------------------------------------------------------------ interno

        private Tramo TramoDe(int dia) {
            foreach (var tramo in _tramos)
                if (tramo.Contiene(dia)) return tramo;
            return dia < _tramos[0].DiaInicio ? _tramos[0] : _tramos[_tramos.Count - 1];
        }

        private static bool AplicaHoy(Ceremonia c, DayPlan plan) {
            switch (Normalizar(c.Cuando)) {
                case "diario": return true;
                case "inicioiteracion": return plan.EsPrimerDiaDeUnidad;
                case "finiteracion":
                case "finetapa": return plan.EsUltimoDiaDeUnidad;
                case "cada": return c.CadaNDias > 0 && plan.Dia % c.CadaNDias == 0;
                // 'cuandoSeLiberaWip' no lo decide el calendario: lo dispara el flujo continuo de Kanban
                // cuando el throughput libera una tarjeta. Por eso aqui no aplica nunca.
                default: return false;
            }
        }

        private bool EstaEnVentana(DayPlan plan, ReglasDeCambio reglas) {
            // Una ceremonia que abre la ventana manda sobre el calendario: es lo que hace que
            // 'abreVentanaDeCambio' signifique algo en el JSON.
            foreach (var ceremonia in plan.Ceremonias)
                if (ceremonia.AbreVentanaDeCambio) return true;

            if (reglas.Ventanas == null) return false;

            foreach (var ventana in reglas.Ventanas) {
                switch (Normalizar(ventana)) {
                    case "siempre":
                        return true;
                    case "entreiteraciones":
                        if (plan.EsPrimerDiaDeUnidad || plan.EsUltimoDiaDeUnidad) return true;
                        break;
                    case "hito":
                        if (plan.EsUltimoDiaDeUnidad) return true;
                        break;
                }
            }
            return false;
        }

        private static string Normalizar(string s) {
            return s == null ? "" : s.Trim().ToLowerInvariant();
        }

        private static bool EsUnoDe(string valor, params string[] validos) {
            foreach (var v in validos)
                if (string.Equals(valor, v, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static InvalidOperationException Invalido(string id, string mensaje) {
            return new InvalidOperationException($"Metodologia '{id}': {mensaje}");
        }
    }
}
