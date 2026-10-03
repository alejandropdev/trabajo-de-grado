using System;
using System.Collections.Generic;

namespace Nexus.Core.Narrativa {
    /// <summary>
    /// Una palabra de la asignatura explicada para quien nunca la ha oido: sprint, WIP, deuda técnica, SRS…
    /// La UI la enseña en un «?» junto a donde aparece. Es contenido (narrativa/glosario.json), no código.
    /// </summary>
    public sealed class EntradaDeGlosario {
        public string Id;
        public string Termino;
        /// <summary>Una o dos frases, sin jerga.</summary>
        public string Definicion;
        /// <summary>Un ejemplo de la vida diaria o del propio juego.</summary>
        public string Ejemplo;
        /// <summary>El tema de la asignatura: «Scrum», «Calidad», «Testing»…</summary>
        public string Tema;
        /// <summary>
        /// Las paginas del pizarron (estilo receta): que es, que conlleva y como se ve en Nexus. Si faltan, se
        /// hacen dos automaticas con la definicion y el ejemplo.
        /// </summary>
        public List<PaginaDePizarra> Paginas = new List<PaginaDePizarra>();
    }

    /// <summary>Una pagina de pizarron: un titulo, una explicacion corta y de 0 a 4 viñetas dibujadas con tiza.</summary>
    public sealed class PaginaDePizarra {
        public string Titulo;
        public string Texto;
        public List<VinetaDePizarra> Vinetas = new List<VinetaDePizarra>();
        /// <summary>La pagina que cuenta donde aparece esto en el juego (se marca «EN NEXUS»).</summary>
        public bool EnElJuego;
    }

    public sealed class VinetaDePizarra {
        /// <summary>Uno de IconosDePizarra.Todos: persona, equipo, caja, base-de-datos, documento, bicho…</summary>
        public string Icono;
        public string Texto;
        /// <summary>tiza | cian | mostaza | roja | gris. Vacio = tiza.</summary>
        public string Color;
    }

    /// <summary>Los iconos que sabe dibujar el pizarron. El validador rechaza cualquier otro.</summary>
    public static class IconosDePizarra {
        public static readonly string[] Todos = {
            "persona", "equipo", "caja", "base-de-datos", "servidor", "documento", "tablero", "tarjeta", "pila",
            "commit", "rama", "bicho", "check", "cruz", "reloj", "barra-subiendo", "barra-bajando", "mano", "candado", "flecha"
        };

        public static bool Existe(string icono) { return Array.IndexOf(Todos, icono) >= 0; }
    }

    public static class Glosario {
        /// <summary>Las paginas del concepto: las suyas, o dos automaticas si no tiene.</summary>
        public static List<PaginaDePizarra> PaginasDe(EntradaDeGlosario e) {
            if (e == null) return new List<PaginaDePizarra>();
            if (e.Paginas != null && e.Paginas.Count > 0) return e.Paginas;
            var lista = new List<PaginaDePizarra> { new PaginaDePizarra { Titulo = "¿Qué es " + e.Termino + "?", Texto = e.Definicion } };
            if (!string.IsNullOrEmpty(e.Ejemplo)) lista.Add(new PaginaDePizarra { Titulo = "Un ejemplo", Texto = e.Ejemplo, EnElJuego = true });
            return lista;
        }

        public static EntradaDeGlosario Buscar(IEnumerable<EntradaDeGlosario> glosario, string id) {
            if (glosario == null || string.IsNullOrEmpty(id)) return null;
            foreach (var e in glosario)
                if (e != null && string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)) return e;
            return null;
        }
    }
}
