using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Repartir {
    /// <summary>
    /// Un tipo de prueba explicado con la analogia del juego (2026-11-11): el sistema vive en el EDIFICIO DEL CLIENTE
    /// (la recepcion del edificio N1, los patios y ascensores de Facilities) y las incidencias entran a distinta altura,
    /// segun donde viva el fallo: en una pieza suelta (la base), en la conexion entre piezas (el medio) o en el recorrido
    /// completo de una persona (la cima). Cada tipo de prueba es la defensa de su piso.
    /// </summary>
    public sealed class TipoDePrueba {
        public string Id;
        /// <summary>Nombre corto para el piso («UNITARIAS»).</summary>
        public string Nombre;
        /// <summary>Altura en la piramide: de menor (la base) a mayor (la cima).</summary>
        public int Piso;
        /// <summary>Que parte del sistema revisa («cada pieza sola»).</summary>
        public string Revisa;
        /// <summary>La incidencia tipica que frena («un cortocircuito dentro de una pieza»).</summary>
        public string Incidencia;
        /// <summary>La defensa del edificio («el cuarto de maquinas…»).</summary>
        public string Defensa;
        /// <summary>Lo que NO ve este tipo de prueba.</summary>
        public string NoVe;
        /// <summary>La forma del bicho que lo representa: chinche | cable | bloqueo | fantasma | ganzua | enjambre.</summary>
        public string Bicho;
        /// <summary>El color, por nombre logico (lo resuelve la capa Unity): cyan | violet | warning | success | ink | naranja.</summary>
        public string Color;
    }

    public static class TiposDePrueba {
        private static readonly List<TipoDePrueba> Todos = new List<TipoDePrueba> {
            new TipoDePrueba {
                Id = "unitarias", Nombre = "UNITARIAS", Piso = 0, Bicho = "chinche", Color = "cyan",
                Revisa = "cada pieza sola", Incidencia = "un cortocircuito dentro de una pieza",
                Defensa = "el cuarto de máquinas: se prueba cada pieza por separado, y es barato",
                NoVe = "cómo se llevan las piezas entre sí"
            },
            new TipoDePrueba {
                Id = "integracion", Nombre = "INTEGRACIÓN", Piso = 1, Bicho = "cable", Color = "violet",
                Revisa = "las conexiones entre piezas", Incidencia = "un cable suelto entre dos piezas",
                Defensa = "el hueco de cables y puertas: comprueba que las piezas se hablen",
                NoVe = "un recorrido entero de principio a fin"
            },
            new TipoDePrueba {
                Id = "seguridad", Nombre = "SEGURIDAD", Piso = 2, Bicho = "ganzua", Color = "ink",
                Revisa = "quién puede entrar o ver qué", Incidencia = "un intruso con una credencial falsa",
                Defensa = "el control de acceso: la cerradura y las credenciales",
                NoVe = "los fallos del uso normal"
            },
            new TipoDePrueba {
                Id = "rendimiento", Nombre = "RENDIMIENTO", Piso = 2, Bicho = "enjambre", Color = "naranja",
                Revisa = "la hora punta", Incidencia = "una fila que se atasca por la carga",
                Defensa = "la prueba de la hora punta, con la fila simulada",
                NoVe = "los errores que no dependen de la carga"
            },
            new TipoDePrueba {
                Id = "regresion", Nombre = "REGRESIÓN", Piso = 3, Bicho = "fantasma", Color = "success",
                Revisa = "lo que ya funcionaba", Incidencia = "algo ya arreglado que vuelve a romperse",
                Defensa = "la ronda del conserje: repasa lo que ya estaba arreglado",
                NoVe = "lo nuevo que aún no se ha roto"
            },
            new TipoDePrueba {
                Id = "e2e", Nombre = "DE PUNTA A PUNTA", Piso = 4, Bicho = "bloqueo", Color = "warning",
                Revisa = "el recorrido completo de una persona", Incidencia = "un recorrido que se atasca a medio camino",
                Defensa = "la vigía de la azotea: ve todo el camino, pero son pocas y caras",
                NoVe = "los detalles finos de cada pieza"
            }
        };

        /// <summary>Los tipos conocidos, de la base a la cima.</summary>
        public static IReadOnlyList<TipoDePrueba> Conocidos { get { return Todos; } }

        public static bool EsConocido(string id) {
            return !string.IsNullOrEmpty(id) && Todos.Any(t => t.Id == id);
        }

        /// <summary>
        /// El tipo de una pila: por su id y, si no se reconoce, por lo que dice su nombre. Una pila desconocida se trata
        /// como una prueba de integracion (a medio camino) con su propio nombre, para que la pantalla nunca se rompa.
        /// </summary>
        public static TipoDePrueba Para(Deposito d) {
            if (d == null) throw new ArgumentNullException(nameof(d));
            var porId = Todos.FirstOrDefault(t => t.Id == d.Id);
            if (porId != null) return porId;
            var n = (d.Nombre ?? "").ToLowerInvariant();
            TipoDePrueba porNombre = null;
            if (n.Contains("unitari")) porNombre = Todos[0];
            else if (n.Contains("integra")) porNombre = Todos[1];
            else if (n.Contains("segur")) porNombre = Todos[2];
            else if (n.Contains("rendim")) porNombre = Todos[3];
            else if (n.Contains("regres")) porNombre = Todos[4];
            else if (n.Contains("extremo") || n.Contains("e2e") || n.Contains("punta")) porNombre = Todos[5];
            if (porNombre != null) return porNombre;
            var generico = Todos[1];
            return new TipoDePrueba {
                Id = d.Id, Nombre = (d.Nombre ?? d.Id ?? "").ToUpperInvariant(), Piso = generico.Piso, Bicho = generico.Bicho, Color = generico.Color,
                Revisa = generico.Revisa, Incidencia = generico.Incidencia, Defensa = generico.Defensa, NoVe = generico.NoVe
            };
        }

        /// <summary>
        /// Los pisos del edificio de una escena, de la BASE a la CIMA: por la altura del tipo y, a igual altura, por lo que
        /// cuesta encontrar un error (lo barato abajo). Es el orden de la piramide.
        /// </summary>
        public static List<Deposito> Pisos(RepartirCfg cfg) {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            return cfg.Depositos.Select((d, i) => new { d, i, t = Para(d) })
                      .OrderBy(x => x.t.Piso).ThenBy(x => x.d.CostePorDefecto).ThenBy(x => x.i)
                      .Select(x => x.d).ToList();
        }
    }
}
