using System;
using System.Collections.Generic;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;

namespace Nexus.Core.Eventos {
    /// <summary>
    /// C3 · El director de eventos (§4.4.3). Decide QUE crisis te toca y CUANDO.
    ///
    /// ★ INV-3, la invariante que define este subsistema: **el director elige y avisa; no dispara nunca.**
    /// Todo lo que hace TickSeleccion es poner un aviso en la agenda para dentro de unos dias.
    /// Quien presenta el evento es GameSession, el dia que toca, y solo despues de que el aviso haya salido.
    ///
    /// Por eso el jugador que lee los logs ve venir los problemas y el que no los lee, no. Esa diferencia
    /// es contenido pedagogico: aprender a leer las señales del proyecto ES el objetivo.
    ///
    /// No toca el WorldState. Lee el estado solo a traves de IStateContext, que es el puerto que existe
    /// exactamente para eso; por eso TickSeleccion no recibe un WorldState aunque la firma del §4.4.3
    /// lo mencione: darselo seria abrirle una puerta que no debe tener.
    /// </summary>
    public sealed class EventDirector {
        /// <summary>Clave del enfriamiento global: dias minimos entre dos eventos cualesquiera.</summary>
        public const string EnfriamientoGlobal = "EV:cualquiera";

        private readonly Dictionary<string, EventDefinition> _catalogo =
            new Dictionary<string, EventDefinition>(StringComparer.Ordinal);
        private readonly List<EventDefinition> _orden = new List<EventDefinition>();

        private readonly LevelProfile _perfil;
        private readonly MethodologyRules _metodologia;
        private readonly DeterministicRng _rng;
        private readonly EffectScheduler _scheduler;

        /// <summary>La traza de por que hizo lo que hizo, un dia por entrada.</summary>
        public List<DecisionDelDirector> Log = new List<DecisionDelDirector>();

        public EventDirector(List<EventDefinition> catalogo, LevelProfile perfil, MethodologyRules metodologia,
                             DeterministicRng rng, EffectScheduler scheduler) {
            if (perfil == null) throw new ArgumentNullException(nameof(perfil));
            if (metodologia == null) throw new ArgumentNullException(nameof(metodologia));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));

            _perfil = perfil;
            _metodologia = metodologia;
            _rng = rng;
            _scheduler = scheduler;

            if (catalogo == null) return;
            foreach (var ev in catalogo) {
                if (ev == null || string.IsNullOrEmpty(ev.Id)) continue;
                if (_catalogo.ContainsKey(ev.Id))
                    throw new InvalidOperationException($"El catalogo tiene dos eventos con id '{ev.Id}'.");
                _catalogo[ev.Id] = ev;
                _orden.Add(ev);   // el orden del catalogo es el orden de la ruleta: sin el, no hay determinismo
            }
        }

        public IReadOnlyList<EventDefinition> Catalogo { get { return _orden; } }

        public static string NombreDeFase(int fase) { return FasesDelNivel.De(fase); }

        /// <summary>
        /// Lo que queda de presupuesto de drama en la fase actual, ya escalado por la metodologia.
        /// Cascada con multiplicador 0.8 sobre un presupuesto de 3 tiene 2: un nivel mas plano, y mas aburrido.
        /// </summary>
        public int DramaRestante(RuntimeState r) {
            var bruto = r.DramaDeLaFase();
            if (bruto <= 0) return 0;
            var escalado = (int)Math.Floor(bruto * _metodologia.MultiplicadorDrama);
            return escalado < 0 ? 0 : escalado;
        }

        // ------------------------------------------------------------------ seleccion

        /// <summary>
        /// El tick diario: filtra, pesa, sortea y AGENDA. Nunca dispara.
        /// Se llama una vez por jornada, en la ventana de las 09:00.
        /// </summary>
        public void TickSeleccion(RuntimeState r, IStateContext ctx, DayPlan plan) {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var decision = new DecisionDelDirector { Dia = r.DiaActual, DramaAntes = DramaRestante(r) };
            Log.Add(decision);

            if (decision.DramaAntes <= 0) {
                decision.Resultado = DecisionDelDirector.SinDrama;
                return;
            }

            var candidatos = new List<EventDefinition>();
            var pesos = new List<double>();

            foreach (var ev in _orden) {
                string motivo;
                if (!EsCandidato(ev, r, ctx)) continue;
                if (!PasaPrecondiciones(ev, ctx, out motivo)) { decision.Descartados.Add($"{ev.Id}: {motivo}"); continue; }

                var peso = CalcularPeso(ev, ctx, plan);
                if (peso <= 0) { decision.Descartados.Add($"{ev.Id}: peso 0"); continue; }

                candidatos.Add(ev);
                pesos.Add(peso);
            }

            decision.Candidatos = candidatos.Count;
            if (candidatos.Count == 0) {
                decision.Resultado = DecisionDelDirector.SinCandidatos;
                return;
            }

            var elegido = _rng.RuletaPonderada(pesos);
            if (elegido < 0) {
                decision.Resultado = DecisionDelDirector.SinCandidatos;
                return;
            }

            var evento = candidatos[elegido];
            decision.EventoId = evento.Id;
            decision.Peso = pesos[elegido];

            var diasAntes = DiasDeAntelacion(evento);
            var diaDelEvento = r.DiaActual + diasAntes;
            decision.DiaDelEvento = diaDelEvento;

            // ★ El hueco se comprueba DESPUES de elegir, y no gastar drama aqui es deliberado (§6.3):
            // que el calendario este lleno no es una decision dramatica, es una limitacion de agenda.
            if (_scheduler.EventosAgendadosEn(diaDelEvento) >= Math.Max(1, _perfil.Director.MaxEventosPorDia)) {
                decision.Resultado = DecisionDelDirector.DiaLleno;
                return;
            }

            if (ChocaConElEnfriamientoGlobal(diaDelEvento)) {
                decision.Resultado = DecisionDelDirector.SinHueco;
                return;
            }

            Agendar(evento, diaDelEvento, diasAntes);
            GastarDrama(r);
            decision.Resultado = DecisionDelDirector.Agendado;
        }

        /// <summary>
        /// Encadena un evento (las 7 cadenas del §A). Sigue telegrafiado — una consecuencia encadenada
        /// tampoco cae del cielo — pero NO consume drama: las cadenas las escribe el contenido a proposito,
        /// no el presupuesto de caos del nivel.
        /// </summary>
        public void ForzarEvento(string eventoId, RuntimeState r) {
            EventDefinition evento;
            if (string.IsNullOrEmpty(eventoId) || !_catalogo.TryGetValue(eventoId, out evento))
                throw new InvalidOperationException(
                    $"Se intento encadenar '{eventoId}', que no esta en el catalogo de eventos.");

            if (_scheduler.HayEventoAgendado(eventoId)) return;

            var diasAntes = DiasDeAntelacion(evento);
            var diaDelEvento = r.DiaActual + diasAntes;

            Agendar(evento, diaDelEvento, diasAntes);
            Log.Add(new DecisionDelDirector {
                Dia = r.DiaActual, Resultado = DecisionDelDirector.Forzado,
                EventoId = eventoId, DiaDelEvento = diaDelEvento, DramaAntes = DramaRestante(r)
            });
        }

        // ------------------------------------------------------------------ disparo

        /// <summary>
        /// El evento que toca presentar hoy, o null. Lo llama GameSession en la ventana de las 09:00.
        ///
        /// Comprueba que su aviso ya salio y lanza si no: es la unica forma de que INV-3 sea una garantia
        /// y no una intencion. Solo puede romperse por un error de orden dentro del motor — el contenido
        /// no tiene forma de provocarlo — y por eso se prefiere que estalle en los tests a que pase inadvertido.
        /// </summary>
        public EventDefinition EventoDeHoy(RuntimeState r) {
            foreach (var agendado in _scheduler.EventosQueDisparanHoy(r.DiaActual)) {
                EventDefinition evento;
                if (!_catalogo.TryGetValue(agendado.EventoId, out evento)) continue;

                if (!agendado.Emitido)
                    throw new InvalidOperationException(
                        $"INV-3 rota: '{evento.Id}' iba a presentarse el dia {r.DiaActual} sin que su aviso " +
                        $"del dia {agendado.DiaDelAviso} hubiera salido. Ningun evento se dispara sin telegrafiar.");

                return evento;
            }
            return null;
        }

        /// <summary>Se llama al resolver el evento: cuenta la ocurrencia y arranca los dos enfriamientos.</summary>
        public void RegistrarDisparo(EventDefinition ev, RuntimeState r) {
            if (ev == null) throw new ArgumentNullException(nameof(ev));

            int veces;
            r.Ocurrencias.TryGetValue(ev.Id, out veces);
            r.Ocurrencias[ev.Id] = veces + 1;

            r.Enfriamientos[ev.Id] = r.DiaActual;
            r.Enfriamientos[EnfriamientoGlobal] = r.DiaActual;

            _scheduler.OlvidarAgenda(ev.Id);
        }

        // ------------------------------------------------------------------ pesos

        /// <summary>
        /// pesoEfectivo = pesoBase
        ///              × Π(1 + contribucion_i)        ← tus decisiones cambian tu perfil de riesgo
        ///              × pesosPorTag[tag]             ← el enfoque del nivel, que tu configuraste sin saberlo
        ///              × multiplicadorMetodologia     ← y la etapa de hoy, en cascada
        ///
        /// Los tags se MULTIPLICAN entre si. El motor lite tomaba el maximo; la especificacion deja la
        /// decision abierta (§5.6) y aqui se elige multiplicar, que es lo que dice el motor completo:
        /// un evento que es a la vez "tecnico" y "devops" en un nivel que descuido ambos debe ser
        /// mas probable que uno que solo toca uno de los dos.
        /// </summary>
        public double CalcularPeso(EventDefinition ev, IStateContext ctx, DayPlan plan) {
            if (ev == null) throw new ArgumentNullException(nameof(ev));

            var peso = ev.PesoBase;

            if (ev.ModificadoresPeso != null) {
                foreach (var m in ev.ModificadoresPeso) {
                    if (m == null || string.IsNullOrEmpty(m.Variable)) continue;

                    double valor;
                    if (!ctx.TryGetValue(m.Variable, out valor))
                        throw new InvalidOperationException(
                            $"El evento '{ev.Id}' pesa sobre '{m.Variable}', que el estado no conoce. " +
                            "Es un error de contenido, no de partida.");

                    peso *= 1.0 + Contribucion(ev.Id, m.Curva, m.Factor, valor);
                }
            }

            if (ev.Tags != null) {
                foreach (var tag in ev.Tags) {
                    if (string.IsNullOrEmpty(tag)) continue;

                    double delNivel;
                    if (_perfil.Director != null && _perfil.Director.PesosPorTag != null &&
                        _perfil.Director.PesosPorTag.TryGetValue(tag, out delNivel))
                        peso *= delNivel;

                    peso *= _metodologia.MultiplicadorTag(tag, plan);
                }
            }

            return peso < 0 ? 0 : peso;
        }

        private static double Contribucion(string eventoId, string curva, double factor, double valor) {
            switch (curva == null ? "" : curva.Trim().ToLowerInvariant()) {
                case "lineal": return factor * valor;
                case "inversa": return factor * (100.0 - valor);
                case "cuadratica": return factor * valor * valor / 100.0;
                default:
                    throw new InvalidOperationException(
                        $"El evento '{eventoId}' usa la curva de peso '{curva}', que no existe. " +
                        "Validas: lineal, inversa, cuadratica.");
            }
        }

        // ------------------------------------------------------------------ los cinco filtros

        private bool EsCandidato(EventDefinition ev, RuntimeState r, IStateContext ctx) {
            if (!ev.SaleEnFase(r.Fase)) return false;
            if (_scheduler.HayEventoAgendado(ev.Id)) return false;
            if (_metodologia.EventoBloqueado(ev.Id, ev.SoloMetodologias)) return false;

            int veces;
            if (ev.MaxOcurrencias > 0 && r.Ocurrencias.TryGetValue(ev.Id, out veces) && veces >= ev.MaxOcurrencias)
                return false;

            int ultimaVez;
            if (r.Enfriamientos.TryGetValue(ev.Id, out ultimaVez) && r.DiaActual - ultimaVez < ev.Enfriamiento)
                return false;

            int ultimoEvento;
            if (r.Enfriamientos.TryGetValue(EnfriamientoGlobal, out ultimoEvento) &&
                r.DiaActual - ultimoEvento < _perfil.Director.EnfriamientoGlobal)
                return false;

            return true;
        }

        private static bool PasaPrecondiciones(EventDefinition ev, IStateContext ctx, out string motivo) {
            motivo = null;
            if (ev.Precondiciones == null || ev.Precondiciones.Count == 0) return true;

            foreach (var condicion in ev.Precondiciones) {
                if (ConditionEvaluator.Evaluar(condicion, ctx)) continue;
                motivo = "no cumple \"" + condicion + "\"";
                return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ interno

        private void Agendar(EventDefinition evento, int diaDelEvento, int diasAntes) {
            var tele = evento.Telegrafiado;
            _scheduler.AgendarTelegrafiado(
                evento.Id, diaDelEvento, diasAntes,
                tele == null ? "log" : tele.Canal,
                tele == null ? null : tele.Texto);
        }

        /// <summary>Minimo 1: avisar el mismo dia no es avisar, y eso rompe INV-3.</summary>
        private int DiasDeAntelacion(EventDefinition evento) {
            var delEvento = evento.Telegrafiado == null ? 0 : evento.Telegrafiado.DiasAntes;
            var dias = delEvento > 0 ? delEvento : _perfil.Director.VentanaTelegrafiado;
            return dias < 1 ? 1 : dias;
        }

        private bool ChocaConElEnfriamientoGlobal(int diaDelEvento) {
            var minimo = _perfil.Director.EnfriamientoGlobal;
            if (minimo <= 0) return false;

            foreach (var agendado in _scheduler.Telegrafiados)
                if (Math.Abs(agendado.DiaDelEvento - diaDelEvento) < minimo) return true;

            return false;
        }

        private void GastarDrama(RuntimeState r) {
            var i = r.Fase - 1;
            if (r.DramaRestante != null && i >= 0 && i < r.DramaRestante.Length && r.DramaRestante[i] > 0)
                r.DramaRestante[i]--;
        }
    }
}
