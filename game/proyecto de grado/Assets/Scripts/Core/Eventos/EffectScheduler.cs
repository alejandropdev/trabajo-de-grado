using System;
using System.Collections.Generic;

namespace Nexus.Core.Eventos {
    /// <summary>
    /// C4 · La agenda del tiempo (§4.4.4). Dos listas y ninguna regla de juego: guarda lo que vence
    /// mas adelante y los avisos que aun no han salido.
    ///
    /// La llenan GameSession y EventDirector; la vacia SOLO GameSession. El director agenda y avisa,
    /// nunca dispara (INV-3), y esta clase es lo que hace que esa separacion sea posible.
    ///
    /// Dos efectos laterales que hay que conocer porque la especificacion los exige:
    /// · Vencidos(dia) SACA los efectos de la cola: llamarlo dos veces el mismo dia no los aplica dos veces.
    /// · AvisosDeHoy(dia) marca Emitido = true: el aviso sale UNA vez y no se repite.
    /// </summary>
    public sealed class EffectScheduler {
        private readonly List<EfectoEnCola> _cola = new List<EfectoEnCola>();
        private readonly List<TelegrafiadoPendiente> _telegrafiados = new List<TelegrafiadoPendiente>();

        public IReadOnlyList<EfectoEnCola> Cola { get { return _cola; } }
        public IReadOnlyList<TelegrafiadoPendiente> Telegrafiados { get { return _telegrafiados; } }

        // ------------------------------------------------------------------ efectos diferidos

        /// <summary>
        /// Programa un efecto para dentro de diferido.EnDias. Los efectos se COPIAN: el catalogo se carga
        /// una sola vez al arrancar, asi que compartir el diccionario dejaria que una partida modificase
        /// el contenido que van a leer las siguientes.
        /// </summary>
        public void Encolar(int diaActual, EfectoDiferido diferido, string origen) {
            if (diferido == null) return;

            if (diferido.EnDias < 0)
                throw new ArgumentOutOfRangeException(nameof(diferido), diferido.EnDias,
                    $"Un efecto diferido de '{origen}' no puede vencer en el pasado ({diferido.EnDias} dias).");

            _cola.Add(new EfectoEnCola {
                DiaObjetivo = diaActual + diferido.EnDias,
                Origen = origen,
                Efectos = diferido.Efectos == null
                    ? new Dictionary<string, object>(StringComparer.Ordinal)
                    : new Dictionary<string, object>(diferido.Efectos, StringComparer.Ordinal),
                EventoForzado = diferido.EventoForzado
            });
        }

        /// <summary>
        /// Lo que vence hoy o antes, EN ORDEN DE ENCOLADO, y lo quita de la cola.
        ///
        /// Recoge tambien lo atrasado (DiaObjetivo &lt; dia). No deberia pasar, porque GameSession llama a
        /// esto cada mañana, pero si un dia se saltara, un efecto perdido seria una consecuencia que el
        /// jugador esquiva sin saberlo — y eso es justo lo que la cola existe para impedir.
        /// </summary>
        public List<EfectoEnCola> Vencidos(int dia) {
            var vencidos = new List<EfectoEnCola>();
            var quedan = new List<EfectoEnCola>();

            foreach (var efecto in _cola) {
                if (efecto.DiaObjetivo <= dia) vencidos.Add(efecto);
                else quedan.Add(efecto);
            }

            _cola.Clear();
            _cola.AddRange(quedan);
            return vencidos;
        }

        // ------------------------------------------------------------------ telegrafiados

        /// <summary>
        /// Agenda un evento y su aviso previo. El dia del aviso nunca baja de 1: si un evento del dia 2
        /// se telegrafiara con 3 dias de antelacion, el aviso caeria antes de empezar el nivel.
        /// </summary>
        public void AgendarTelegrafiado(string eventoId, int diaDelEvento, int diasAntes, string canal, string texto) {
            if (string.IsNullOrEmpty(eventoId))
                throw new ArgumentException("No se puede agendar un evento sin id.", nameof(eventoId));

            var antelacion = diasAntes < 0 ? 0 : diasAntes;

            _telegrafiados.Add(new TelegrafiadoPendiente {
                EventoId = eventoId,
                DiaDelEvento = diaDelEvento,
                DiaDelAviso = Math.Max(1, diaDelEvento - antelacion),
                Canal = canal,
                Texto = texto,
                Emitido = false
            });
        }

        /// <summary>
        /// Los avisos que toca emitir hoy, marcandolos como emitidos. Incluye los que se quedaron atras:
        /// si un aviso se perdiera, su evento se dispararia sin previo aviso y eso rompe INV-3.
        /// </summary>
        public List<TelegrafiadoPendiente> AvisosDeHoy(int dia) {
            var avisos = new List<TelegrafiadoPendiente>();

            foreach (var aviso in _telegrafiados) {
                if (aviso.Emitido || aviso.DiaDelAviso > dia) continue;
                aviso.Emitido = true;
                avisos.Add(aviso);
            }

            return avisos;
        }

        /// <summary>Los eventos agendados para hoy. No los retira: de eso se encarga OlvidarAgenda al resolverlos.</summary>
        public List<TelegrafiadoPendiente> EventosQueDisparanHoy(int dia) {
            var hoy = new List<TelegrafiadoPendiente>();
            foreach (var aviso in _telegrafiados)
                if (aviso.DiaDelEvento == dia) hoy.Add(aviso);
            return hoy;
        }

        /// <summary>Para que el director no agende dos veces el mismo evento.</summary>
        public bool HayEventoAgendado(string eventoId) {
            foreach (var aviso in _telegrafiados)
                if (string.Equals(aviso.EventoId, eventoId, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Cuantos hay ya en ese dia. Es lo que hace cumplir maxEventosPorDia del perfil de nivel.</summary>
        public int EventosAgendadosEn(int dia) {
            var n = 0;
            foreach (var aviso in _telegrafiados)
                if (aviso.DiaDelEvento == dia) n++;
            return n;
        }

        /// <summary>Retira un evento de la agenda, resuelto o cancelado.</summary>
        public void OlvidarAgenda(string eventoId) {
            _telegrafiados.RemoveAll(a => string.Equals(a.EventoId, eventoId, StringComparison.Ordinal));
        }

        // ------------------------------------------------------------------ guardado

        /// <summary>
        /// Rehidrata la agenda. Es el paso 2 del orden del §7.6, justo despues del azar y antes del estado:
        /// el director se construye con el scheduler ya lleno, no al reves.
        /// Las listas de entrada se copian, asi que quien las pasa puede tirarlas.
        /// </summary>
        public void Restaurar(List<EfectoEnCola> cola, List<TelegrafiadoPendiente> telegrafiados) {
            _cola.Clear();
            if (cola != null)
                foreach (var efecto in cola)
                    if (efecto != null) _cola.Add(efecto.Clone());

            _telegrafiados.Clear();
            if (telegrafiados != null)
                foreach (var aviso in telegrafiados)
                    if (aviso != null) _telegrafiados.Add(aviso.Clone());
        }

        /// <summary>Copia profunda para el guardado: lo que se serializa no puede seguir cambiando por debajo.</summary>
        public List<EfectoEnCola> CopiaDeCola() {
            var copia = new List<EfectoEnCola>(_cola.Count);
            foreach (var efecto in _cola) copia.Add(efecto.Clone());
            return copia;
        }

        /// <summary>Copia profunda para el guardado.</summary>
        public List<TelegrafiadoPendiente> CopiaDeTelegrafiados() {
            var copia = new List<TelegrafiadoPendiente>(_telegrafiados.Count);
            foreach (var aviso in _telegrafiados) copia.Add(aviso.Clone());
            return copia;
        }
    }
}
