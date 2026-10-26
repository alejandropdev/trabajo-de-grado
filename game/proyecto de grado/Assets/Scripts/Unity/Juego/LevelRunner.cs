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
    ///   · Mirando   — un panel del dia abierto (mapa, equipo, monitoreo). Ver es gratis; actuar desde el, no.
    ///   · nunca     — mientras el jugador DECIDE si ir a atender una alerta. Si el mundo se parara, esa
    ///                 decision no costaria nada (§M8): por eso sonar una alerta no pausa.
    /// </summary>
    public sealed class LevelRunner : MonoBehaviour {
        private GameSession _sesion;
        private RelojEnTiempoReal _reloj;
        /// <summary>La velocidad elegida en las opciones: la toma el reloj de cada nivel al crearse.</summary>
        private double _velocidadPreferida = 1.0;
        private double _segundosJugados;

        public EstadoDelDia Estado { get; private set; } = EstadoDelDia.Parado;
        public GameSession Sesion { get { return _sesion; } }

        public bool Pausado { get; set; }

        /// <summary>Lo activa quien abre una decision, un minijuego o una cinematica; lo desactiva al cerrarla.</summary>
        public bool EnEscena { get; set; }

        /// <summary>
        /// El jugador tiene abierto un panel del dia (el mapa, el equipo, el monitoreo…). MIRAR es gratis: el reloj
        /// espera. Pero no es una escena: desde el panel se puede ACTUAR (ir a una zona, empezar una tarea), y cada
        /// accion cobra sus minutos como siempre. Por eso no entra en PuedeMoverse.
        /// </summary>
        public bool Mirando { get; set; }

        /// <summary>
        /// Si el jugador puede ir a otra zona ahora. ★ Con el reloj parado por una escena o por la pausa NO: si
        /// estas dentro de una decision, un minijuego o una carta, estas ocupado en un sitio. Poder viajar con el
        /// tiempo congelado seria moverse gratis. Mirar un panel no ocupa: el viaje se sigue pagando en minutos.
        /// </summary>
        public bool PuedeMoverse {
            get {
                return _sesion != null && !EnEscena && !Pausado &&
                       (Estado == EstadoDelDia.Corriendo || Estado == EstadoDelDia.Prorroga);
            }
        }

        /// <summary>El multiplicador de la simulacion, de 0,25 a 4. El docente puede comprimir una sesion.</summary>
        public double Velocidad {
            get { return _reloj == null ? _velocidadPreferida : _reloj.Velocidad; }
            set {
                _velocidadPreferida = value;
                if (_reloj != null) _reloj.Velocidad = value;
            }
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
            var velocidad = _reloj == null ? _velocidadPreferida : _reloj.Velocidad;
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

            // ★ «Dia cerrado» lo dice la sesion, no el reloj: a las 18:00 sin prorrogar el reloj ya marca el final, y
            // una partida guardada ahi, antes de elegir, se recargaba como dia terminado y ese dia no se simulaba nunca.
            if (_sesion.JornadaSimulada) Estado = EstadoDelDia.DiaTerminado;
            else if (_sesion.JornadaProrrogada) { Estado = EstadoDelDia.Prorroga; ComprobarCierre(); }
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

        /// <summary>
        /// «Adelantar el reloj»: salta hasta lo siguiente que pase hoy. Si queda un aviso por sonar, hasta el; si no
        /// queda ninguno, hasta el cierre de la jornada. El jugador no sabe de antemano cual de las dos cosas sera:
        /// el dia no anuncia sus avisos. Devuelve true si lo que llego fue el cierre.
        /// </summary>
        public bool AdelantarHastaQuePaseAlgo() {
            ExigirSesion();
            if (Estado != EstadoDelDia.Corriendo) return false;
            if (AdelantarHastaElSiguienteAviso()) return false;
            if (!_sesion.SePuedeCerrarLaJornada) return false;
            CerrarJornada();
            return true;
        }

        /// <summary>
        /// Atiende una alerta. Atender tambien mueve el reloj (MinutosPorAtender), y en ese rato puede sonar o
        /// caducar OTRA alerta: por eso pasa por aqui y no directo a la sesion, para que las pantallas se enteren.
        /// Devuelve false si la alerta ya no estaba pendiente.
        /// </summary>
        public bool Atender(string alertaId) {
            ExigirSesion();
            if (Estado != EstadoDelDia.Corriendo || EnEscena || Pausado) return false;
            Alerta alerta = null;
            foreach (var a in _sesion.AlertasDeHoy) if (a.Id == alertaId) alerta = a;
            if (alerta == null || !alerta.EstaPendiente) return false;

            Notificar(_sesion.AtenderAlerta(alertaId));
            ComprobarCierre();
            return true;
        }

        /// <summary>
        /// Empieza una tarea de oficina: su tiempo se cobra del reloj ya, y en ese rato pueden sonar o caducar
        /// avisos (por eso pasa por aqui). Devuelve false si ahora no se puede.
        /// </summary>
        public bool EmpezarTarea(string tareaId) {
            ExigirSesion();
            if (!PuedeMoverse || _sesion.PorQueNoSePuedeHacer(tareaId) != null) return false;
            Notificar(_sesion.EmpezarTarea(tareaId));
            ComprobarCierre();
            return true;
        }

        /// <summary>
        /// Contestar en una conversacion. La charla cuesta minutos y en ellos pueden sonar o caducar avisos: por eso
        /// pasa por aqui. Se llama con la escena de la conversacion abierta, asi que no exige poder moverse, solo que
        /// la jornada este corriendo. null si ahora no se puede.
        /// </summary>
        public Nexus.Core.Relaciones.ResultadoDeConversacion Hablar(string conversacionId, string opcionId) {
            ExigirSesion();
            if (Estado != EstadoDelDia.Corriendo && Estado != EstadoDelDia.Prorroga) return null;
            ResultadoDeAvance avance;
            var resultado = _sesion.Hablar(conversacionId, opcionId, out avance);
            Notificar(avance);
            ComprobarCierre();
            return resultado;
        }

        /// <summary>
        /// Una accion del jugador sobre el proyecto que cuesta minutos (asignar una tarjeta, hacer pruebas, asistir a
        /// una ceremonia…). Pasa por aqui, y no directa a la sesion, por lo mismo que las demas: en esos minutos puede
        /// sonar o caducar un aviso, o llegar el cierre. Devuelve false si ahora no se puede actuar.
        /// </summary>
        public bool Actuar(Func<ResultadoDeAvance> accion) {
            ExigirSesion();
            if (accion == null || !PuedeMoverse) return false;
            Notificar(accion());
            return true;
        }

        /// <summary>Para lo que ya movio el reloj por su cuenta dentro de una escena (revisar un cambio de codigo).</summary>
        public void Notificar(ResultadoDeAvance resultado) {
            Avisar(resultado);
            ComprobarCierre();
        }

        /// <summary>Ir a otra zona del mapa. El viaje cuesta minutos, y en ellos el dia sigue pasando.</summary>
        public void IrAZona(string zonaId) {
            ExigirSesion();
            if (!PuedeMoverse) return;
            if (string.Equals(_sesion.ZonaActual, zonaId, StringComparison.OrdinalIgnoreCase)) return;
            Notificar(_sesion.IrAZona(zonaId));
            ComprobarCierre();
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
            _sesion.TerminarDia();
            TerminarElDia();
        }

        /// <summary>
        /// Las 18:00: quedarse. El reloj sigue hasta la hora limite. ★ El dia se simula al ACABAR las horas extra
        /// (con su +25 % de avance y lo que cuesta), no ahora: asi las barras se mueven cuando el dia termina a la
        /// vista, y no dos horas antes.
        /// </summary>
        public void Quedarse() {
            ExigirEstado(EstadoDelDia.EnElCierre);
            _sesion.Quedarse();
            Estado = EstadoDelDia.Prorroga;
            // Un nivel sin horas extra configuradas no tiene prorroga que jugar: el dia acaba ya.
            ComprobarCierre();
        }

        /// <summary>Suelta la sesion. El reloj deja de correr.</summary>
        public void Detener() {
            _sesion = null;
            Estado = EstadoDelDia.Parado;
            EnEscena = false;
            Pausado = false;
            Mirando = false;
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
            if (Pausado || EnEscena || Mirando) return;

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
                if (!_sesion.JornadaSimulada) _sesion.TerminarDia();
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

        private void Avisar(ResultadoDeAvance resultado) {
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
