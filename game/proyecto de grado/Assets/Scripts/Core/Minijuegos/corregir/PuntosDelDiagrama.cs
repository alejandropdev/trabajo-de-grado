using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Corregir
{
    /// <summary>
    /// Un punto marcado en el diagrama (#1, #2…): una zona con un problema de verdad, o un señuelo que parece raro y
    /// esta bien. El jugador no sabe cual es cual: a cada uno le juega una carta de solucion o «Está bien así».
    /// </summary>
    public sealed class PuntoDelDiagrama
    {
        public string Id;
        public int Numero;
        /// <summary>La pieza (caja o flecha) sobre la que se dibuja el marcador.</summary>
        public string Pieza;
        public Zona Zona;
        public Senuelo Senuelo;
        public bool EsReal { get { return Zona != null; } }
    }

    /// <summary>
    /// Los puntos de un diagrama, en un orden estable que mezcla zonas y señuelos (por un hash del id: el mismo reto
    /// sale siempre igual, y el #1 no es siempre el problema).
    /// </summary>
    public static class PuntosDelDiagrama
    {
        /// <summary>Lo que se juega en vez de una carta cuando el punto esta bien. No se gasta.</summary>
        public const string EstaBien = "esta_bien";

        /// <summary>El reto se juega con cartas (lienzo diagrama con cartas): si no, es el Detectar de siempre.</summary>
        public static bool Aplica(MinijuegoDef def)
        {
            return def != null && def.Lienzo == "diagrama" && def.Cartas != null && def.Cartas.Count > 0;
        }

        public static List<PuntoDelDiagrama> De(MinijuegoDef def)
        {
            var puntos = new List<PuntoDelDiagrama>();
            foreach (var z in def.Zonas)
                puntos.Add(new PuntoDelDiagrama { Id = "Z:" + z.Id, Pieza = z.Commits.FirstOrDefault(), Zona = z });
            foreach (var s in def.Senuelos)
                puntos.Add(new PuntoDelDiagrama { Id = "S:" + s.Id, Pieza = s.Commits.FirstOrDefault(), Senuelo = s });
            var ordenados = puntos.OrderBy(p => Hash(def.Id + "|" + p.Id)).ThenBy(p => p.Id, System.StringComparer.Ordinal).ToList();
            for (var i = 0; i < ordenados.Count; i++) ordenados[i].Numero = i + 1;
            return ordenados;
        }

        /// <summary>FNV-1a: estable entre ejecuciones y plataformas (string.GetHashCode no lo es).</summary>
        private static uint Hash(string s)
        {
            unchecked
            {
                var h = 2166136261u;
                foreach (var c in s ?? "") { h ^= c; h *= 16777619u; }
                return h;
            }
        }

        public static CartaDeSolucion Carta(MinijuegoDef def, string id)
        {
            return def.Cartas.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>«Está bien así» o el nombre de la carta.</summary>
        public static string NombreDeCarta(MinijuegoDef def, string id)
        {
            if (id == EstaBien) return "Está bien así";
            var c = Carta(def, id);
            return c != null ? c.Nombre : id;
        }

        /// <summary>«Módulo de reservas» o «la flecha «lee las tablas…»».</summary>
        public static string NombreDePieza(MinijuegoDef def, string pieza)
        {
            var e = def.Artefacto.Elementos.FirstOrDefault(x => x.Id == pieza);
            if (e != null) return "«" + e.Texto + "»";
            var c = def.Artefacto.Conexiones.FirstOrDefault(x => x.Id == pieza);
            if (c != null) return "la flecha «" + c.Texto + "»";
            return "«" + pieza + "»";
        }

        /// <summary>
        /// Una manera de ganar: a cada zona una carta que la arregla, sin repetir carta, y «Está bien así» en los
        /// señuelos. null si no existe (el reto seria imposible: lo rechaza el validador). Backtracking: hay pocas.
        /// </summary>
        public static Dictionary<string, string> SolucionGanadora(MinijuegoDef def)
        {
            var puntos = De(def);
            var zonas = puntos.Where(p => p.EsReal).ToList();
            var asignacion = new Dictionary<string, string>();
            var usadas = new HashSet<string>();
            var cartas = new HashSet<string>(def.Cartas.Select(c => c.Id));
            if (!Asignar(zonas, 0, cartas, usadas, asignacion)) return null;
            foreach (var p in puntos.Where(p => !p.EsReal)) asignacion[p.Id] = EstaBien;
            return asignacion;
        }

        private static bool Asignar(List<PuntoDelDiagrama> zonas, int i, HashSet<string> cartas, HashSet<string> usadas, Dictionary<string, string> asignacion)
        {
            if (i == zonas.Count) return true;
            foreach (var c in zonas[i].Zona.Soluciones)
            {
                if (!cartas.Contains(c) || usadas.Contains(c)) continue;
                usadas.Add(c);
                asignacion[zonas[i].Id] = c;
                if (Asignar(zonas, i + 1, cartas, usadas, asignacion)) return true;
                usadas.Remove(c);
                asignacion.Remove(zonas[i].Id);
            }
            return false;
        }
    }
}
