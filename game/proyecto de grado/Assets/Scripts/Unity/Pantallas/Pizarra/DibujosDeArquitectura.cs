using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Cómo está armado»: el plano de la arquitectura elegida, dibujado con los modulos de ESTE proyecto. La misma
    /// lista de modulos se ve como un solo programa con paredes, como servicios sueltos cada uno con su base, detras
    /// de un navegador o pegada encima de un sistema viejo. Es lo que hace visible la tercera decision de la Fase 1.
    /// Todo en un cuadro de 1400×560, para ponerlo a pagina completa o a escala.
    /// </summary>
    public static class DibujosDeArquitectura {
        public const float Ancho = 1400, Alto = 560;

        /// <summary>Lo que la arquitectura le hace al despliegue y al dia a dia, en una frase cada cosa.</summary>
        public static string AlDesplegar(string id) {
            switch (id) {
                case "microservicios": return "Cada módulo es un servicio aparte: cada uno se enciende por separado, con sus propios cables.";
                case "parche-sobre-la-barrera": return "Lo nuevo sale pegado a la barrera vieja: si lo viejo falla al encender, falla todo.";
                case "app-movil": return "Además del servidor, hay que publicar la app y esperar a que cada persona la instale.";
                case "web-sencilla": return "Se publica en un servidor y ya está: quien abre el navegador ve la versión nueva.";
                default: return "Sale todo junto, en un solo despliegue: una cosa que encender y una cosa que vigilar.";
            }
        }

        public static string EnElDia(string id) {
            switch (id) {
                case "microservicios": return "El equipo gasta parte de cada día en operar servicios: la deuda crece más deprisa.";
                case "parche-sobre-la-barrera": return "Se trabaja encima de código que nadie entiende: la deuda heredada pesa cada día.";
                case "app-movil": return "Hay dos cosas que construir (la app y el servidor): el equipo se cansa antes.";
                case "web-sencilla": return "Una sola cosa que construir y que mantener: no le añade trabajo al equipo.";
                default: return "Un solo programa bien separado por dentro: no le añade trabajo de operación al equipo.";
            }
        }

        /// <summary>El plano de la arquitectura con los modulos de este proyecto (2026-11-15: en el lenguaje nuevo, Pizarra/DibujosDeArquitectura.Luz.cs).</summary>
        public static void Plano(Tizador t, FichaDelProyecto f, string id, string nombre) { DibujosDeEscena.PlanoDeArquitectura(t, f, id, nombre); }
    }
}
