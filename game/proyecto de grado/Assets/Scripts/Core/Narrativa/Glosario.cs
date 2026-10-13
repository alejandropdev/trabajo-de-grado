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
        /// Las paginas del pizarron (estilo receta): que es, lo que haga falta para entenderlo (que conlleva, que
        /// pasa si sube o baja, como se cuida…) y, la ultima, como se ve en Nexus. Si faltan, se hacen dos automaticas
        /// con la definicion y el ejemplo.
        /// </summary>
        public List<PaginaDePizarra> Paginas = new List<PaginaDePizarra>();
        /// <summary>
        /// El curso a fondo («Aprender a fondo»): muchas paginas agrupadas por capitulo. Vive en su propio
        /// archivo (narrativa/cursos/&lt;id&gt;.json) y casi ningun termino lo tiene.
        /// </summary>
        public List<PaginaDePizarra> Curso = new List<PaginaDePizarra>();
    }

    /// <summary>
    /// Una pagina de pizarron: un titulo, una explicacion corta y su dibujo de tiza. El dibujo es UNA de tres
    /// cosas: una escena a pagina completa ('Dibujo'), de 2 a 4 columnas con su dibujo cada una, o de 0 a 4 viñetas.
    /// </summary>
    public sealed class PaginaDePizarra {
        public string Titulo;
        public string Texto;
        public List<VinetaDePizarra> Vinetas = new List<VinetaDePizarra>();
        /// <summary>La pagina que cuenta donde aparece esto en el juego (se marca «EN NEXUS»).</summary>
        public bool EnElJuego;
        /// <summary>La nota de la esquina para esta pagina (una de EtiquetasDePizarra.Todas). Vacia = la del pizarron.</summary>
        public string Etiqueta;
        /// <summary>Una escena de EscenasDePizarra.Todas, dibujada grande bajo el texto.</summary>
        public string Dibujo;
        public List<ColumnaDePizarra> Columnas = new List<ColumnaDePizarra>();
        /// <summary>El capitulo del curso al que pertenece (solo en las paginas de un curso).</summary>
        public string Capitulo;
    }

    /// <summary>Una columna de una pagina: su dibujo arriba, un titulo y una o dos frases.</summary>
    public sealed class ColumnaDePizarra {
        public string Titulo;
        public string Texto;
        /// <summary>Una escena (EscenasDePizarra) o un icono (IconosDePizarra).</summary>
        public string Dibujo;
        /// <summary>tiza | cian | mostaza | roja | gris. Vacio = tiza.</summary>
        public string Color;
    }

    /// <summary>Las notas de esquina que puede llevar una pagina: dicen de que va antes de leerla.</summary>
    public static class EtiquetasDePizarra {
        public static readonly string[] Todas = { "¿QUÉ ES?", "¿QUIÉN?", "¿CÓMO?", "¿CUÁNDO?", "DATO", "¡OJO!", "RESUMEN" };

        public static bool Existe(string etiqueta) { return Array.IndexOf(Todas, etiqueta) >= 0; }
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
