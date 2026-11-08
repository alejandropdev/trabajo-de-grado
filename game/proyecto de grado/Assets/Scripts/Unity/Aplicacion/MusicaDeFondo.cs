using Nexus.Core.Audio;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// La musica del juego: dos temas en bucle, y que suena en cada momento lo decide MezclaDeMusica (Core) por la
    /// pantalla de arriba del router (los dialogos no la cambian: son una pregunta sobre la pantalla de debajo).
    ///   · tema 1 «nexus»: menus, planificar y la jornada;
    ///   · tema 2 «accion»: el recorrido 3D y la Fase 3 (desplegar y lanzar), y sube un poco cuando al reloj le queda
    ///     poco (Apremio).
    /// Al cambiar de tema se funden uno en otro; el que se calla se PAUSA, asi que al volver sigue donde iba.
    /// Cada tema hace su bucle sin corte: dos fuentes con la misma pista, y la siguiente vuelta entra con un fundido
    /// antes de que acabe la actual, empezando justo donde empieza a sonar (sin el silencio del principio).
    /// Todo en 2D y sin pararse con la pausa del recorrido 3D.
    /// </summary>
    public sealed class MusicaDeFondo : MonoBehaviour {
        public const string Carpeta = "Audio/Musica/";
        private const float SegundosDeFundido = 1.5f, SegundosDeApremio = 2f, SegundosDeEmpalme = 2.5f;

        /// <summary>Un tema: su pista, sus dos fuentes para el bucle y su volumen de ahora y al que va.</summary>
        private sealed class Pista {
            public string Nombre;
            public AudioClip Clip;
            /// <summary>Donde empieza a sonar de verdad (el WAV trae un poco de silencio al principio).</summary>
            public float Inicio;
            public AudioSource[] Fuentes;
            public int Actual;
            public float Empalme;
            public float Nivel, Destino, Velocidad = 1f / SegundosDeFundido;
            public bool Empezada, Pausada;
        }

        private Pista[] _pistas;
        private float _volumenDeOpciones = 0.7f;
        private string _pantalla;
        private bool _apremio;

        public static MusicaDeFondo Crear(GameObject anfitrion, float volumenDeOpciones) {
            var m = anfitrion.AddComponent<MusicaDeFondo>();
            m._volumenDeOpciones = Mathf.Clamp01(volumenDeOpciones);
            return m;
        }

        private void Awake() {
            // Medido sobre los WAV: tema 1 suena desde 0,45 s; tema 2 desde 0,85 s.
            _pistas = new[] { NuevaPista(MezclaDeMusica.Nexus, "tema-nexus", 0.45f), NuevaPista(MezclaDeMusica.Accion, "tema-accion", 0.85f) };
            Recalcular(SegundosDeFundido);
        }

        private Pista NuevaPista(string nombre, string archivo, float inicio) {
            var p = new Pista { Nombre = nombre, Inicio = inicio, Clip = Resources.Load<AudioClip>(Carpeta + archivo) };
            if (p.Clip == null) {
                Debug.LogWarning($"No se encontró la música en Resources/{Carpeta}{archivo}: ese tema no sonará.");
                return p;
            }
            p.Inicio = Mathf.Min(inicio, Mathf.Max(0f, p.Clip.length - SegundosDeEmpalme * 2));
            p.Fuentes = new AudioSource[2];
            for (var i = 0; i < 2; i++) {
                var f = gameObject.AddComponent<AudioSource>();
                f.clip = p.Clip;
                f.loop = false;              // el bucle lo hace el empalme
                f.playOnAwake = false;
                f.spatialBlend = 0;
                f.ignoreListenerPause = true;
                f.priority = 0;
                f.volume = 0;
                p.Fuentes[i] = f;
            }
            return p;
        }

        /// <summary>El volumen de la musica elegido en Opciones (0..1).</summary>
        public void CambiarVolumen(float volumen) { _volumenDeOpciones = Mathf.Clamp01(volumen); }

        /// <summary>Lo llama el router en cada cambio de pantalla. El apremio es de la pantalla que lo pidio: se apaga.</summary>
        public void AlCambiarDePantalla(Pantalla pantalla) {
            if (pantalla == null || pantalla.EsModal) return;
            _pantalla = pantalla.GetType().Name;
            _apremio = false;
            Recalcular(SegundosDeFundido);
        }

        /// <summary>Al reloj le queda poco (el recorrido 3D, el despliegue): el tema sube un poco. Se puede llamar cada fotograma.</summary>
        public void Apremio(bool activo) {
            if (activo == _apremio) return;
            _apremio = activo;
            Recalcular(SegundosDeApremio);
        }

        private void Recalcular(float segundos) {
            if (_pistas == null) return;
            var m = MezclaDeMusica.Para(_pantalla, _apremio);
            foreach (var p in _pistas) {
                p.Destino = p.Nombre == m.Pista ? m.Nivel : 0f;
                p.Velocidad = Mathf.Max(0.05f, Mathf.Abs(p.Destino - p.Nivel)) / segundos;
            }
        }

        private void Update() {
            if (_pistas == null) return;
            var dt = Time.unscaledDeltaTime;
            foreach (var p in _pistas) Avanzar(p, dt);
        }

        private void Avanzar(Pista p, float dt) {
            if (p.Fuentes == null) return;
            p.Nivel = Mathf.MoveTowards(p.Nivel, p.Destino, p.Velocidad * dt);

            // Callada del todo: se pausa (y al volver sigue donde iba). Nunca empezada y sin nada que tocar: nada.
            if (p.Nivel <= 0.001f && p.Destino <= 0f) {
                if (p.Empezada && !p.Pausada) {
                    foreach (var f in p.Fuentes) f.Pause();
                    p.Pausada = true;
                }
                return;
            }
            if (!p.Empezada) {
                p.Fuentes[0].time = p.Inicio;
                p.Fuentes[0].Play();
                p.Empezada = true;
            } else if (p.Pausada) {
                foreach (var f in p.Fuentes) f.UnPause();
                p.Pausada = false;
            }

            var sonando = p.Fuentes[p.Actual];
            var siguiente = p.Fuentes[1 - p.Actual];
            // Antes de que acabe la vuelta, entra la siguiente desde donde empieza a sonar.
            if (!siguiente.isPlaying && sonando.isPlaying && sonando.time >= p.Clip.length - SegundosDeEmpalme) {
                siguiente.time = p.Inicio;
                siguiente.Play();
                p.Empalme = 0;
            }
            if (siguiente.isPlaying) {
                p.Empalme = Mathf.Min(1, p.Empalme + dt / SegundosDeEmpalme);
                if (!sonando.isPlaying || p.Empalme >= 1) {
                    sonando.Stop();
                    p.Actual = 1 - p.Actual;
                    p.Empalme = 0;
                }
            } else if (!sonando.isPlaying) {
                sonando.time = p.Inicio;   // por si algo la paro (un cambio de dispositivo de audio)
                sonando.Play();
            }

            var volumen = p.Nivel * _volumenDeOpciones;
            // Curva de igual potencia: en el empalme no hay bache de volumen.
            p.Fuentes[p.Actual].volume = volumen * Mathf.Cos(p.Empalme * Mathf.PI / 2);
            var otra = p.Fuentes[1 - p.Actual];
            otra.volume = otra.isPlaying ? volumen * Mathf.Sin(p.Empalme * Mathf.PI / 2) : 0;
        }
    }
}
