using System;
using System.Collections.Generic;
using Nexus.Core.Servicios;

namespace Nexus.Core.Fase1 {
    /// <summary>Lo que se puede encontrar en la recoleccion 3D de la Fase 1.</summary>
    public static class TiposDeHallazgo {
        /// <summary>Una linea extra del encargo: algo que el briefing no decia y ayuda a decidir.</summary>
        public const string Pista = "pista";
        /// <summary>Dinero, documentacion: stocks del proyecto.</summary>
        public const string Recurso = "recurso";
        /// <summary>Alguien que se une o que ayuda: velocidad, moral. Puede dejar un flag al cerrar el nivel.</summary>
        public const string Personaje = "personaje";
        /// <summary>Lo que cuida al equipo: un cafe, un sitio para descansar.</summary>
        public const string Moral = "moral";
        /// <summary>Una pieza del Diario de Campo (del catalogo de coleccionables).</summary>
        public const string Coleccionable = "coleccionable";
        /// <summary>Algo que resta si se toca: un cable suelto, una reunion que se alarga.</summary>
        public const string Riesgo = "riesgo";

        private static readonly string[] _todos = { Pista, Recurso, Personaje, Moral, Coleccionable, Riesgo };
        public static IReadOnlyList<string> Todos { get { return _todos; } }

        public static bool EsValido(string tipo) {
            foreach (var t in _todos) if (string.Equals(t, tipo, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    public sealed class ZonaDeRecoleccion {
        public string Id;            // "A".."F"
        public string Nombre;
        public string Descripcion;
        public int MinutosDeVisita = 20;

        public ZonaDeRecoleccion Clone() { return (ZonaDeRecoleccion)MemberwiseClone(); }
    }

    public sealed class Hallazgo {
        public string Id;
        public string Tipo;
        public string Zona;
        /// <summary>Para personajes y recursos: como se llama lo encontrado.</summary>
        public string Nombre;
        /// <summary>Para pistas: la linea que se añade al encargo. Para lo demas, una descripcion corta.</summary>
        public string Texto;
        /// <summary>Stocks del WorldState que cambia al recogerlo (Dinero, MoralEquipo, VelocidadMod…).</summary>
        public Dictionary<string, double> Efectos = new Dictionary<string, double>(StringComparer.Ordinal);
        /// <summary>Solo personajes: el flag que se suma en Cerrar() (INV-6), p. ej. conocer a alguien.</summary>
        public string FlagAlCerrar;
        public double ValorDelFlag = 1;

        public Hallazgo Clone() {
            var c = (Hallazgo)MemberwiseClone();
            c.Efectos = new Dictionary<string, double>(Efectos ?? new Dictionary<string, double>(), StringComparer.Ordinal);
            return c;
        }
    }

    /// <summary>La seccion fase1.recoleccion del JSON del nivel: el recorrido por el plano (zonas A–F, §4.2).</summary>
    public sealed class RecoleccionConfig {
        public int MinutosDisponibles = 120;
        public string Texto;
        public List<ZonaDeRecoleccion> Zonas = new List<ZonaDeRecoleccion>();
        public List<Hallazgo> Hallazgos = new List<Hallazgo>();

        public ZonaDeRecoleccion Zona(string id) {
            foreach (var z in Zonas) if (z != null && z.Id == id) return z;
            return null;
        }

        public Hallazgo Hallazgo(string id) {
            foreach (var h in Hallazgos) if (h != null && h.Id == id) return h;
            return null;
        }

        public RecoleccionConfig Clone() {
            var c = (RecoleccionConfig)MemberwiseClone();
            c.Zonas = new List<ZonaDeRecoleccion>();
            if (Zonas != null) foreach (var z in Zonas) c.Zonas.Add(z == null ? null : z.Clone());
            c.Hallazgos = new List<Hallazgo>();
            if (Hallazgos != null) foreach (var h in Hallazgos) c.Hallazgos.Add(h == null ? null : h.Clone());
            return c;
        }
    }

    /// <summary>
    /// ENTRADA del modulo 3D: todo lo que necesita para montar el recorrido. Es un documento autocontenido,
    /// se puede serializar a JSON tal cual y pasarlo al subequipo; no depende de ninguna clase del motor.
    /// </summary>
    public sealed class EntradaDeRecoleccion {
        public string NivelId;
        public int Semilla;
        public int NivelAndamiaje;
        public int MinutosDisponibles;
        public string Texto;
        public List<ZonaDeRecoleccion> Zonas = new List<ZonaDeRecoleccion>();
        public List<Hallazgo> Hallazgos = new List<Hallazgo>();
        /// <summary>Lo que el perfil ya tiene: el 3D puede no volver a enseñarlo, o enseñarlo como ya visto.</summary>
        public List<string> ColeccionablesYaEnElPerfil = new List<string>();
        /// <summary>Los stocks al empezar, por si el 3D quiere enseñarlos (el dinero en el HUD, por ejemplo).</summary>
        public Dictionary<string, double> EstadoInicial = new Dictionary<string, double>(StringComparer.Ordinal);
    }

    /// <summary>
    /// SALIDA del modulo 3D: SOLO que se recogio. Los efectos no los calcula el 3D: los aplica el motor
    /// (GameSession.AplicarRecoleccion), que valida que todo lo que llega estuviera en la entrada.
    /// </summary>
    public sealed class ResultadoDeRecoleccion {
        public string NivelId;
        public int MinutosUsados;
        public List<string> ZonasVisitadas = new List<string>();
        public List<string> Hallazgos = new List<string>();
        /// <summary>El jugador salio antes de agotar el tiempo. No cambia nada; queda anotado.</summary>
        public bool AbandonoAntes;
    }

    public static class IntensidadesDeSimulacion {
        public const string Rapida = "rapida";
        public const string Normal = "normal";
        public const string AFondo = "a-fondo";
    }

    /// <summary>
    /// Mientras el 3D no existe, esto lo sustituye: un recorrido simulado y determinista (misma semilla y misma
    /// intensidad, mismo resultado). Rapida usa poco tiempo y deja cosas; a fondo lo mira todo, riesgos incluidos.
    /// Devuelve exactamente lo que devolveria el 3D, asi que cambiar uno por otro no toca nada mas.
    /// </summary>
    public static class SimuladorDeRecoleccion {
        public static ResultadoDeRecoleccion Simular(EntradaDeRecoleccion entrada, string intensidad) {
            if (entrada == null) throw new ArgumentNullException(nameof(entrada));
            double fraccionDeTiempo, probabilidad, probabilidadDeRiesgo;
            switch (intensidad) {
                case IntensidadesDeSimulacion.Rapida: fraccionDeTiempo = 0.4; probabilidad = 0.5; probabilidadDeRiesgo = 0.2; break;
                case IntensidadesDeSimulacion.AFondo: fraccionDeTiempo = 1.0; probabilidad = 1.0; probabilidadDeRiesgo = 0.6; break;
                default: fraccionDeTiempo = 0.7; probabilidad = 0.8; probabilidadDeRiesgo = 0.35; break;
            }

            var rng = new DeterministicRng(entrada.Semilla ^ intensidad.GetHashCodeEstable());
            var presupuesto = (int)Math.Floor(entrada.MinutosDisponibles * fraccionDeTiempo);
            var resultado = new ResultadoDeRecoleccion { NivelId = entrada.NivelId };

            // Orden de visita: el de la entrada, rotado por la semilla (cada partida empieza por otro sitio).
            var zonas = new List<ZonaDeRecoleccion>(entrada.Zonas);
            if (zonas.Count > 0) {
                var inicio = rng.Next(zonas.Count);
                zonas = Rotar(zonas, inicio);
            }

            foreach (var zona in zonas) {
                if (zona == null || resultado.MinutosUsados + zona.MinutosDeVisita > presupuesto) continue;
                resultado.MinutosUsados += zona.MinutosDeVisita;
                resultado.ZonasVisitadas.Add(zona.Id);
                foreach (var h in entrada.Hallazgos) {
                    if (h == null || h.Zona != zona.Id) continue;
                    var p = h.Tipo == TiposDeHallazgo.Riesgo ? probabilidadDeRiesgo : probabilidad;
                    if (rng.NextDouble() < p) resultado.Hallazgos.Add(h.Id);
                }
            }
            resultado.AbandonoAntes = resultado.MinutosUsados < entrada.MinutosDisponibles && intensidad == IntensidadesDeSimulacion.Rapida;
            return resultado;
        }

        private static List<T> Rotar<T>(List<T> lista, int desde) {
            var r = new List<T>(lista.Count);
            for (var i = 0; i < lista.Count; i++) r.Add(lista[(desde + i) % lista.Count]);
            return r;
        }

        /// <summary>string.GetHashCode cambia entre ejecuciones en .NET moderno: este no.</summary>
        private static int GetHashCodeEstable(this string s) {
            unchecked {
                var h = 23;
                foreach (var c in s ?? "") h = h * 31 + c;
                return h;
            }
        }
    }
}
