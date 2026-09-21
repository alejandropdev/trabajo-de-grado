using System;
using System.Collections.Generic;

namespace Nexus.Core.Jornada {
    /// <summary>
    /// C11 · Las alertas del día de hoy y en qué estado está cada una (§3.3).
    ///
    /// Se vacía cada mañana: una alerta pertenece a su día y no se arrastra. Lo que sí sobrevive al
    /// guardado es esta cola tal cual, porque guardar a las 14:00 con una alerta viva hasta las 16:00
    /// y recargar tiene que devolver exactamente esa situación — si no, guardar sería una forma de
    /// esquivarla, y eso es justo lo que INV-7 prohíbe.
    ///
    /// No sabe qué es un evento ni qué es un minijuego. Solo lleva alertas.
    /// </summary>
    public sealed class ColaDeAlertas {
        private readonly List<Alerta> _alertas = new List<Alerta>();

        public IReadOnlyList<Alerta> Alertas { get { return _alertas; } }

        /// <summary>Al empezar el día. Las alertas de ayer no se heredan.</summary>
        public void VaciarDelDia() { _alertas.Clear(); }

        public void Encolar(Alerta alerta) {
            if (alerta == null) throw new ArgumentNullException(nameof(alerta));
            if (string.IsNullOrEmpty(alerta.Id))
                throw new ArgumentException("Una alerta sin id no se puede atender.", nameof(alerta));

            if (alerta.MinutoDeExpiracion <= alerta.MinutoDeLaAlerta)
                throw new ArgumentException(
                    $"La alerta '{alerta.Id}' expira antes de sonar " +
                    $"({RelojDeJornada.Formatear(alerta.MinutoDeLaAlerta)} → " +
                    $"{RelojDeJornada.Formatear(alerta.MinutoDeExpiracion)}).", nameof(alerta));

            _alertas.Add(alerta);
        }

        public Alerta PorId(string id) {
            foreach (var a in _alertas)
                if (string.Equals(a.Id, id, StringComparison.Ordinal)) return a;
            return null;
        }

        // ------------------------------------------------------------------ el paso del tiempo

        /// <summary>
        /// Las que suenan al avanzar el reloj de 'desde' a 'hasta'. El intervalo es (desde, hasta]
        /// a propósito: así avanzar el reloj dos veces seguidas no anuncia dos veces la misma alerta.
        /// </summary>
        public List<Alerta> SuenanEntre(int desde, int hasta) {
            var sonando = new List<Alerta>();
            foreach (var a in _alertas)
                if (a.MinutoDeLaAlerta > desde && a.MinutoDeLaAlerta <= hasta) sonando.Add(a);
            return sonando;
        }

        /// <summary>
        /// Marca como expiradas las que se pasaron de plazo y las devuelve. Anota dónde estaba el
        /// jugador: el §8 quiere esa evidencia en la traza.
        ///
        /// ★ Expirar NO es lo mismo que no haber pasado nada. El resultado es 'omitido' y la rúbrica
        /// lo lee como 'incorrecta': el mundo queda igual que si la hubieras resuelto mal. Quien
        /// aplica esa consecuencia es GameSession; aquí solo se marca.
        /// </summary>
        public List<Alerta> Expirar(int minutoActual, string zonaDelJugador) {
            var expiradas = new List<Alerta>();

            foreach (var a in _alertas) {
                if (!a.EstaPendiente || minutoActual < a.MinutoDeExpiracion) continue;
                a.Estado = EstadosDeAlerta.Expirada;
                a.MinutoDeResolucion = minutoActual;
                a.ZonaDelJugador = zonaDelJugador;
                expiradas.Add(a);
            }

            return expiradas;
        }

        /// <summary>Las que ya sonaron, siguen vivas y se pueden atender ahora mismo.</summary>
        public List<Alerta> Pendientes(int minutoActual) {
            var vivas = new List<Alerta>();
            foreach (var a in _alertas)
                if (a.EstaPendiente && a.YaSono(minutoActual)) vivas.Add(a);
            return vivas;
        }

        /// <summary>
        /// Atiende una alerta. Lanza si ya se resolvió o si todavía no ha sonado: las dos cosas serían
        /// un error del motor, no del jugador.
        /// </summary>
        public Alerta Atender(string id, int minutoActual, string zonaDelJugador) {
            var alerta = PorId(id);
            if (alerta == null)
                throw new InvalidOperationException($"Hoy no hay ninguna alerta '{id}'.");

            if (!alerta.EstaPendiente)
                throw new InvalidOperationException(
                    $"La alerta '{id}' ya estaba {alerta.Estado} desde las " +
                    RelojDeJornada.Formatear(alerta.MinutoDeResolucion) + ".");

            if (!alerta.YaSono(minutoActual))
                throw new InvalidOperationException(
                    $"La alerta '{id}' no suena hasta las {RelojDeJornada.Formatear(alerta.MinutoDeLaAlerta)}.");

            alerta.Estado = EstadosDeAlerta.Atendida;
            alerta.MinutoDeResolucion = minutoActual;
            alerta.ZonaDelJugador = zonaDelJugador;
            return alerta;
        }

        // ------------------------------------------------------------------ cerrar la jornada

        /// <summary>Hay algo esperando respuesta ahora mismo.</summary>
        public bool HayPendientes(int minutoActual) { return Pendientes(minutoActual).Count > 0; }

        /// <summary>Queda alguna por sonar más tarde.</summary>
        public bool QuedaAlgoPorSonar(int minutoActual) {
            foreach (var a in _alertas)
                if (a.EstaPendiente && !a.YaSono(minutoActual)) return true;
            return false;
        }

        /// <summary>
        /// Si se puede ofrecer el botón de «cerrar jornada».
        ///
        /// Solo cuando no queda NADA: ni pendiente ahora ni por sonar después. Así saltar al cierre
        /// nunca se come una consecuencia, y lo único que cuesta es perderse la exploración y las
        /// conversaciones de lo que quedaba de tarde.
        /// </summary>
        public bool SePuedeCerrarLaJornada(int minutoActual) {
            return !HayPendientes(minutoActual) && !QuedaAlgoPorSonar(minutoActual);
        }

        // ------------------------------------------------------------------ guardado

        public List<Alerta> Copia() {
            var copia = new List<Alerta>(_alertas.Count);
            foreach (var a in _alertas) copia.Add(a.Clone());
            return copia;
        }

        public void Restaurar(List<Alerta> alertas) {
            _alertas.Clear();
            if (alertas == null) return;
            foreach (var a in alertas)
                if (a != null) _alertas.Add(a.Clone());
        }
    }
}
