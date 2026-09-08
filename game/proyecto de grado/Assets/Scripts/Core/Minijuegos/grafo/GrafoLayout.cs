using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Nexus.Core.Minijuegos.Grafo
{
    /// <summary>
    /// Convierte (commits + padres + ramas) en coordenadas logicas.
    /// Vive en Core y no sabe nada de pixeles: la capa Unity multiplica
    /// carril y fila por el tamano de celda. Asi se puede testear sin abrir el editor.
    /// Determinista: mismo JSON, mismo layout, siempre.
    /// </summary>
    public static class GrafoLayout
    {
        public sealed class Nodo
        {
            public string CommitId;
            public int Carril;      // columna: 0 = rama declarada primero
            public int Fila;        // 0 = el mas reciente, arriba
            public DateTime Fecha;
        }

        public sealed class Arista
        {
            public string DesdeId;  // padre
            public string HastaId;  // hijo
            public bool Cruzada;    // el padre esta en otro carril: hay que dibujar diagonal
        }

        public sealed class Resultado
        {
            public List<Nodo> Nodos = new List<Nodo>();
            public List<Arista> Aristas = new List<Arista>();
            public int Carriles;
            public int Filas;
            public Nodo Por(string id) { return Nodos.FirstOrDefault(n => n.CommitId == id); }
        }

        public static Resultado Calcular(Artefacto art)
        {
            var res = new Resultado();

            // Carril = orden de declaracion de la rama. Estable y legible.
            var carrilDeRama = new Dictionary<string, int>();
            for (int i = 0; i < art.Ramas.Count; i++) carrilDeRama[art.Ramas[i].Id] = i;

            // Fila = fecha descendente. Desempate por id para que sea determinista.
            var ordenados = art.Commits
                .OrderByDescending(ParsearFecha)
                .ThenBy(c => c.Id, StringComparer.Ordinal)
                .ToList();

            for (int fila = 0; fila < ordenados.Count; fila++)
            {
                var c = ordenados[fila];
                int carril;
                if (!carrilDeRama.TryGetValue(c.Rama ?? "", out carril)) carril = art.Ramas.Count;

                res.Nodos.Add(new Nodo
                {
                    CommitId = c.Id,
                    Carril = carril,
                    Fila = fila,
                    Fecha = ParsearFecha(c)
                });
            }

            foreach (var c in art.Commits)
                foreach (var padre in c.Padres)
                {
                    var nHijo = res.Por(c.Id);
                    var nPadre = res.Por(padre);
                    if (nHijo == null || nPadre == null) continue;   // el validador ya avisa
                    res.Aristas.Add(new Arista
                    {
                        DesdeId = padre,
                        HastaId = c.Id,
                        Cruzada = nPadre.Carril != nHijo.Carril
                    });
                }

            res.Carriles = res.Nodos.Count == 0 ? 0 : res.Nodos.Max(n => n.Carril) + 1;
            res.Filas = res.Nodos.Count;
            return res;
        }

        /// <summary>
        /// Commits distintos con la misma fecha y el mismo mensaje: la pista visual
        /// del tramo reescrito. El lienzo la usa para dibujar el corte de la linea.
        /// No revela cual es el defecto, solo donde mirar.
        /// </summary>
        public static List<List<string>> FechasDuplicadas(Artefacto art)
        {
            return art.Commits
                .GroupBy(c => c.Fecha + "|" + c.Mensaje)
                .Where(g => g.Count() > 1)
                .Select(g => g.Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).ToList())
                .ToList();
        }

        private static DateTime ParsearFecha(Commit c)
        {
            DateTime d;
            if (DateTime.TryParse(c.Fecha, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out d)) return d;
            return DateTime.MinValue;
        }
    }
}
