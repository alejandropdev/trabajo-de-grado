using System;
using Nexus.Core.Jornada;
using Nexus.Core.Sesion;
using UnityEngine;

namespace Nexus.Unity.Juego {
    /// <summary>En que momento del dia continuo esta el runner.</summary>
    public enum EstadoDelDia {
        /// <summary>No hay dia en marcha: antes de empezar, o entre dos dias.</summary>
        Parado,
        /// <summary>De la hora de entrada al cierre: el reloj corre y suenan las alertas.</summary>
        Corriendo,
        /// <summary>Las 18:00. El reloj se detiene hasta que el jugador elija irse o quedarse.</summary>
        EnElCierre,
        /// <summary>Se quedo: el reloj corre hasta la hora limite. Ya no suenan alertas; es tiempo para explorar.</summary>
        Prorroga,
        /// <summary>El dia ha acabado. Falta llamar a EmpezarDia.</summary>
        DiaTerminado,
        /// <summary>No quedan dias: el nivel ha pasado a la Fase 3 (lanzamiento).</summary>
        DesarrolloTerminado
    }

    /// <summary>
    /// Hace correr el dia continuo en tiempo real. Cada fotograma pasa Time.deltaTime al RelojEnTiempoReal
    /// (Core), que le dice cuantos minutos de juego avanzan, y se los pide a la GameSession. Lo que ocurre en
    /// ese tramo lo cuenta con eventos; las pantallas se suscriben y pintan.
    ///
    /// Tres formas de parar el reloj, y no son lo mismo:
    ///   · EnEscena  — hay una decision, un minijuego o una cinematica abiertos. La alerta ya se atendio,
    ///                 asi que congelar el mundo aqui no le regala nada al jugador.
    ///   · Pausado   — el menu de pausa. Tambien congela.
    ///   · nunca     — mientras el jugador DECIDE si ir a atender una alerta. Si el mundo se parara, esa
    ///                 decision no costaria nada (§M8): por eso sonar una alerta no pausa.
    /// </summary>
    public sealed class LevelRunner : MonoBehaviour {
        private GameSession _sesion;
        private RelojEnTiempoReal _reloj;
        private double _segundosJugados;

        public EstadoDelDia Estado { get; private set; } = EstadoDelDia.Parado;
        public GameSession Sesion { get { return _sesion; } }

        public bool Pausado { get; set; }

        /// <summary>Lo activa quien abre una decision, un minijuego o una cinematica; lo desactiva al cerrarla.</summary>
        public bool EnEscena { get; set; }

        /// <summary>El multiplicador de la simulacion, de 0,25 a 4. El docente puede comprimir una sesion.</summary>
        public double Velocidad {
            get { return _reloj == null ? 1.0 : _reloj.Velocidad; }
            set { if (_reloj != null) _reloj.Velocidad = value; }
        }

        /// <summary>Cada tramo en que el reloj avanzo al menos un minuto.</summary>
        public event Action<ResultadoDeAvance> AlAvanzar;
        public event Action<Alerta> AlSonarAlerta;
        public event Action<Alerta> AlExpirarAlerta;
        /// <summary>Son las 18:00 (o la hora de cierre del nivel). Toca elegir irse o quedarse.</summary>
        public event Action AlLlegarElCierre;
        public event Action AlTerminarElDia;
        /// <summary>Ya no quedan dias. Toca el lanzamiento.</summary>
        public event Action AlTerminarElDesarrollo;

        // ==================================================================== control

        /// <summary>Engancha una sesion ya con la Fase 1 cerrada. No empieza el dia: eso es EmpezarDia.</summary>
        public void Usar(GameSession sesion) {
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
            var velocidad = _reloj == null ? 1.0 : _reloj.Velocidad;
            _reloj = new RelojEnTiempoReal(sesion.Perfil.Jornada) { Velocidad = velocidad };
            Estado = sesion.R.Fase == 2 ? EstadoDelDia.Parado : EstadoDelDia.DesarrolloTerminado;
        }

        /// <summary>Empieza el dia siguiente y pone el reloj en marcha. Devuelve lo que el dia trae.</summary>
        public DayBrief EmpezarDia() {
            ExigirSesion();
            if (_sesion.R.Fase != 2) {
                Estado = EstadoDelDia.DesarrolloTerminado;
                return null;
            }
            var brief = _sesion.ComenzarDia();
            _reloj.Reiniciar();
            Estado = EstadoDelDia.Corriendo;
            return brief;
        }

        /// <summary>
        /// Continua un dia que se cargo a medias (la partida se guardo a las 14:30). No llama a ComenzarDia:
        /// el dia ya empezo, y volverlo a empezar lo sumaria dos veces.
        /// </summary>
        public void ReanudarDia() {
            ExigirSesion();
            if (_sesion.R.Fase != 2) { Estado = EstadoDelDia.DesarrolloTerminado; return; }
            if (_sesion.R.DiaActual == 0) { Estado = EstadoDelDia.Parado; return; }

            if (_sesion.JornadaTerminada) Estado = EstadoDelDia.DiaTerminado;
            else if (_sesion.JornadaProrrogada) Estado = EstadoDelDia.Prorroga;
            else if (_sesion.JornadaLlegoAlCierre) Estado = EstadoDelDia.EnElCierre;
            else Estado = EstadoDelDia.Corriendo;
        }

        /// <summary>Salta a las 18:00. Solo si no queda nada pendiente hoy (lo decide la sesion).</summary>
        public void CerrarJornada() {
            ExigirSesion();
            if (Estado != EstadoDelDia.Corriendo || !_sesion.SePuedeCerrarLaJornada) return;
            Notificar(_sesion.CerrarJornada());
            ComprobarCierre();
        }

        /// <summary>
        /// Avanza el reloj de golpe hasta el siguiente aviso de hoy, si queda alguno por sonar. Devuelve false si
        /// no queda ninguno. Es lo mismo que esperar, pero sin esperar: el aviso suena igual y su ventana de
        /// atencion empieza igual.
        /// </summary>
        public bool AdelantarHastaElSiguienteAviso() {
            ExigirSesion();
            if (Estado != EstadoDelDia.Corriendo) return false;

            var ahora = _sesion.MinutoDelDia;
            var siguiente = int.MaxValue;
            foreach (var alerta in _sesion.AlertasDeHoy)
                if (alerta.EstaPendiente && alerta.MinutoDeLaAlerta > ahora && alerta.MinutoDeLaAlerta < siguiente)
                    siguiente = alerta.MinutoDeLaAlerta;
            if (siguiente == int.MaxValue) return false;

            Notificar(_sesion.AvanzarReloj(siguiente - ahora));
            ComprobarCierre();
            return true;
        }

        /// <summary>Durante las horas extra: dar la jornada por terminada ya, sin esperar a la hora limite.</summary>
        public void TerminarLaProrroga() {
            ExigirEstado(EstadoDelDia.Prorroga);
            Notificar(_sesion.AvanzarReloj(_sesion.MinutosRestantesDeJornada));
            ComprobarCierre();
        }

        /// <summary>Las 18:00: irse a casa. El dia se simula y termina.</summary>
        public void Irse() {
            ExigirEstado(EstadoDelDia.EnElCierre);
            _sesion.TerminarDia(false);
            TerminarElDia();
        }

        /// <summary>
        /// Las 18:00: quedarse. El dia se simula YA con horas extra (+25 % de avance, y lo que cuesta). Si
        /// quedan dias, el reloj sigue hasta la hora limite para explorar; si era el ultimo, el nivel pasa
        /// directamente al lanzamiento.
        /// </summary>
        public void Quedarse() {
            ExigirEstado(EstadoDelDia.EnElCierre);
            _sesion.TerminarDia(true);
            if (_sesion.R.Fase != 2) { TerminarElDia(); return; }
            Estado = EstadoDelDia.Prorroga;
        }

        /// <summary>Suelta la sesion. El reloj deja de correr.</summary>
        public void Detener() {
            _sesion = null;
            Estado = EstadoDelDia.Parado;
            EnEscena = false;
            Pausado = false;
        }

        /// <summary>Los segundos reales jugados desde la ultima vez que se pidieron. Para DatosDePartida.SegundosJugados.</summary>
        public double ConsumirSegundosJugados() {
            var s = _segundosJugados;
            _segundosJugados = 0;
            return s;
        }

        // ==================================================================== el bucle

        private void Update() {
            if (_sesion == null) return;
            if (Estado != EstadoDelDia.Corriendo && Estado != EstadoDelDia.Prorroga) return;

            _segundosJugados += Time.unscaledDeltaTime;
            if (Pausado || EnEscena) return;

            var minutos = _reloj.Tick(Time.unscaledDeltaTime);
            if (minutos <= 0) return;

            Notificar(_sesion.AvanzarReloj(minutos));
            ComprobarCierre();
        }

        private void ComprobarCierre() {
            if (Estado == EstadoDelDia.Corriendo && _sesion.JornadaLlegoAlCierre) {
                Estado = EstadoDelDia.EnElCierre;
                AlLlegarElCierre?.Invoke();
            } else if (Estado == EstadoDelDia.Prorroga && _sesion.JornadaTerminada) {
                TerminarElDia();
            }
        }

        private void TerminarElDia() {
            if (_sesion.R.Fase != 2) {
                Estado = EstadoDelDia.DesarrolloTerminado;
                AlTerminarElDia?.Invoke();
                AlTerminarElDesarrollo?.Invoke();
                return;
            }
            Estado = EstadoDelDia.DiaTerminado;
            AlTerminarElDia?.Invoke();
        }

        private void Notificar(ResultadoDeAvance resultado) {
            if (resultado == null) return;
            if (resultado.MinutosAvanzados > 0) AlAvanzar?.Invoke(resultado);
            foreach (var alerta in resultado.AlertasQueSuenan) AlSonarAlerta?.Invoke(alerta);
            foreach (var alerta in resultado.AlertasQueExpiraron) AlExpirarAlerta?.Invoke(alerta);
        }

        private void ExigirSesion() {
            if (_sesion == null) throw new InvalidOperationException("El runner no tiene ninguna sesion enganchada (Usar).");
        }

        private void ExigirEstado(EstadoDelDia esperado) {
            ExigirSesion();
            if (Estado != esperado)
                throw new InvalidOperationException($"Esto solo se puede hacer en {esperado}, y el dia esta en {Estado}.");
        }
    }
}
