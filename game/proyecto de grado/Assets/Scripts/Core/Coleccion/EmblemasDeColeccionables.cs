using System.Collections.Generic;

namespace Nexus.Core.Coleccion {
    /// <summary>
    /// Lo propio de cada coleccionable en su escena de hallazgo (2026-11-06): el emblema dibujado de cada carta, la
    /// variante de cada advertencia (tipo de cartel + icono) y quien escribe cada nota. Aqui solo van las CLAVES; los
    /// dibujos estan en la capa de Unity (Pantallas/Hallazgos). Un test exige que todo el catalogo tenga lo suyo y que
    /// no se repita: «no quiero ver dos coleccionables con estilos parecidos».
    /// </summary>
    public static class EmblemasDeColeccionables {
        /// <summary>El emblema central de cada carta, compuesto a mano a partir de lo que cuenta la carta.</summary>
        public static readonly Dictionary<string, string> DeCartas = new Dictionary<string, string> {
            { "COL-DEV-02", "organigrama-espejo" },   // Ley de Conway: dos equipos, dos modulos con su misma frontera
            { "COL-DEV-03", "radiacion" },            // Therac-25: el trebol de radiacion y la dosis que se sale de la escala
            { "COL-DEV-04", "reloj-que-crece" },      // Ley de Parkinson: el reloj de arena y la tarea que llena el plazo
            { "COL-DEV-05", "desplome" },             // Knight Capital: siete servidores bien, uno no, y la grafica que cae
            { "COL-DEV-06", "pergamino" },            // ADR: el documento con su sello de decision
            { "COL-DEV-07", "autobus" },              // Bus factor: el autobus y la unica persona que lo sabe
            { "COL-DEV-08", "moneda-interes" },       // Deuda tecnica: la moneda que crece con intereses
            { "COL-DEV-09", "bateria" },              // Burnout: la bateria vaciandose
            { "COL-DEV-10", "escoba" },               // Boy Scout: la escoba y el codigo mas limpio
            { "COL-DEV-11", "ramas" },                // Git: las ramas que se separan y se vuelven a unir
            { "COL-DEV-12", "escudo" },               // Cobertura: el escudo de las pruebas
            { "COL-DEV-13", "criterios" },            // Historias de usuario: la tarjeta con sus criterios marcados
            { "COL-DEV-14", "plano" },                // Diagramas: el edificio dibujado antes de construirse
            { "COL-DEV-15", "taza-pausa" },           // Pausas activas: la taza y el reloj
            { "COL-DEV-16", "lupa-dialogo" },         // Code reviews: la lupa sobre el codigo y el bocadillo amable
        };

        /// <summary>Los cuatro tipos de cartel de una advertencia.</summary>
        public static class Carteles {
            public const string Esmalte = "esmalte";          // placa esmaltada roja y blanca, atornillada
            public const string AltoVoltaje = "alto-voltaje"; // triangulo amarillo con rayo, fondo negro
            public const string Laboratorio = "laboratorio";  // etiqueta naranja de laboratorio, con rombo
            public const string Obra = "obra";                // cinta de obra amarilla y negra
            public const string PostIt = "post-it";           // un aviso escrito a mano y pegado con cinta
        }

        /// <summary>Cada advertencia: su cartel y su icono. Ninguna pareja se repite.</summary>
        public static readonly Dictionary<string, KeyValuePair<string, string>> DeAdvertencias = new Dictionary<string, KeyValuePair<string, string>> {
            { "COL-ADV-01", Par(Carteles.Laboratorio, "commit") },      // UN COMMIT, UNA IDEA
            { "COL-ADV-02", Par(Carteles.Esmalte, "documento") },       // SI NO ESTA EN EL ADR, NO PASO
            { "COL-ADV-03", Par(Carteles.Laboratorio, "termometro") },  // COBERTURA ALTA NO ES PRUEBAS BUENAS
            { "COL-ADV-04", Par(Carteles.Obra, "embudo") },             // EL LIMITE DE WIP NO ES UNA SUGERENCIA
            { "COL-ADV-05", Par(Carteles.AltoVoltaje, "persona") },     // UNA PERSONA, UN MODULO, UN RIESGO
            { "COL-ADV-06", Par(Carteles.Esmalte, "reloj-de-arena") },  // EL ATAJO DE HOY LO PAGA OTRO
            { "COL-ADV-07", Par(Carteles.PostIt, "bocadillo") },        // «AH, Y TAMBIEN» ES UN CAMBIO DE ALCANCE
            { "COL-ADV-08", Par(Carteles.Esmalte, "balanza") },         // NEGOCIAR NO ES OBEDECER NI RECHAZAR
            { "COL-ADV-09", Par(Carteles.Obra, "calendario") },         // UN MERGE TARDE NO CUESTA MENOS
            { "COL-ADV-10", Par(Carteles.Laboratorio, "lupa") },        // LO QUE NO SE MIDE DE VERDAD, SE INVENTA
            { "COL-ADV-11", Par(Carteles.PostIt, "brujula") },          // UNA ESTIMACION SIN INCERTIDUMBRE
            { "COL-ADV-12", Par(Carteles.AltoVoltaje, "sirena") },      // EL SERVIDOR QUE NADIE MONITOREA
        };

        /// <summary>Quien escribe cada nota en la charla (alias inventados; nunca una persona real).</summary>
        public static readonly Dictionary<string, string> RemitentesDeNotas = new Dictionary<string, string> {
            { "COL-NOTA-01", "Anónimo" },
            { "COL-NOTA-02", "Número oculto" },
            { "COL-NOTA-03", "Alguien del sótano" },
            { "COL-NOTA-04", "Grupo · Cafetería" },
            { "COL-NOTA-05", "Vecino de escritorio" },
            { "COL-NOTA-06", "Usuario sin nombre" },
        };

        private static KeyValuePair<string, string> Par(string cartel, string icono) { return new KeyValuePair<string, string>(cartel, icono); }
    }
}
