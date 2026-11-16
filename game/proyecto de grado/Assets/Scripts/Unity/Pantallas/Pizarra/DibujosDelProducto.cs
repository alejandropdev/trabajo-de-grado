using System.Collections.Generic;
using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «El producto»: cada modulo del sistema, dibujado en la etapa en que esta (boceto, mockup, prototipo, MVP y en
    /// produccion), el cliente, sus voces y los papeles que trae. Todo en un cuadro de 600×300, como las escenas, para
    /// poder ponerlo grande o en una rejilla. (2026-11-15) El dibujo vive en el lenguaje nuevo de las escenas
    /// (Pizarra/DibujosDelProducto.Luz.cs); esta clase conserva las firmas que usan las pantallas.
    /// </summary>
    public static class DibujosDelProducto {
        public const float Ancho = 600, Alto = 300;

        /// <summary>Un modulo en su etapa. 'filas' = cuantos datos de ejemplo se ven ya; 'pulsado' = acaba de usarse.</summary>
        public static void Modulo(Tizador p, ModuloDelProyecto m, string etapa, int filas, bool pulsado) { DibujosDeEscena.ProductoModulo(p, m, etapa, filas, pulsado); }

        /// <summary>«Quién es tu cliente», a pagina completa. 'y' = hasta donde llego el texto de arriba.</summary>
        public static void Cliente(Tizador t, FichaDelProyecto f, float y) { DibujosDeEscena.ProductoCliente(t, f, y); }

        /// <summary>«Lo que dicen hoy del producto»: por cada usuario del cliente, su parte del sistema y una frase corta.</summary>
        public static void Voces(Tizador t, List<VozDelCliente> voces, List<EstadoDelModulo> modulos, float y) { DibujosDeEscena.ProductoVoces(t, voces, modulos, y); }

        /// <summary>Un papel del cliente: una hoja de cuaderno, una queja, un correo, una hoja de datos o una nota.</summary>
        public static void Papel(Tizador p, PapelDelCliente papel) { DibujosDeEscena.ProductoPapel(p, papel); }
    }
}
