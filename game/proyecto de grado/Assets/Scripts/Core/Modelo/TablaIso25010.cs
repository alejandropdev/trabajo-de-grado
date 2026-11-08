using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Modelo {
    /// <summary>Una casilla de la tabla ISO/IEC 25010: la subcaracteristica y lo que significa, en una frase.</summary>
    public sealed class SubcaracteristicaIso {
        public string Nombre { get; private set; }
        public string Significado { get; private set; }
        public SubcaracteristicaIso(string nombre, string significado) { Nombre = nombre; Significado = significado; }
    }

    /// <summary>Una columna de la tabla: una caracteristica de calidad del producto software y sus subcaracteristicas.</summary>
    public sealed class CaracteristicaIso {
        public string Id { get; private set; }
        public string Nombre { get; private set; }
        public string Resumen { get; private set; }
        public IList<SubcaracteristicaIso> Subcaracteristicas { get; private set; }
        public CaracteristicaIso(string id, string nombre, string resumen, params SubcaracteristicaIso[] subs) {
            Id = id; Nombre = nombre; Resumen = resumen; Subcaracteristicas = subs;
        }
    }

    /// <summary>
    /// La tabla «Calidad del producto software» de la ISO/IEC 25010 (edicion 2023): nueve caracteristicas y sus
    /// subcaracteristicas. Los atributos de calidad de cada nivel (fase 1) son columnas de esta tabla.
    /// </summary>
    public static class TablaIso25010 {
        private static SubcaracteristicaIso S(string n, string s) { return new SubcaracteristicaIso(n, s); }

        public static readonly IList<CaracteristicaIso> Columnas = new List<CaracteristicaIso> {
            new CaracteristicaIso("adecuacion-funcional", "Adecuación funcional", "Que el sistema haga lo que se necesita, y lo haga bien.",
                S("Completitud funcional", "Las funciones cubren todas las tareas y objetivos del usuario: no falta nada de lo pedido."),
                S("Corrección funcional", "Da resultados correctos y con la precisión que hace falta."),
                S("Pertinencia funcional", "Las funciones ayudan de verdad a cumplir las tareas, sin pasos ni funciones que sobren.")),
            new CaracteristicaIso("eficiencia-de-desempeno", "Eficiencia de desempeño", "Rapidez y buen uso de los recursos con la carga prevista.",
                S("Comportamiento temporal", "Los tiempos de respuesta y de proceso son adecuados cuando el sistema trabaja a la carga esperada."),
                S("Utilización de recursos", "Gasta una cantidad apropiada de recursos (procesador, memoria, red, almacenamiento)."),
                S("Capacidad", "Los límites máximos (usuarios, datos, operaciones a la vez) cubren lo que se necesita.")),
            new CaracteristicaIso("compatibilidad", "Compatibilidad", "Convivir e intercambiar información con otros sistemas.",
                S("Coexistencia", "Funciona junto a otros sistemas en el mismo entorno sin estorbarlos ni ser estorbado."),
                S("Interoperabilidad", "Dos o más sistemas pueden intercambiar información y usar lo que se han intercambiado.")),
            new CaracteristicaIso("capacidad-de-interaccion", "Capacidad de interacción", "Lo fácil, claro y agradable que es usarlo (antes «usabilidad»).",
                S("Reconocibilidad de adecuación", "El usuario puede darse cuenta de si el sistema le sirve para lo que quiere hacer."),
                S("Aprendizabilidad", "Se aprende a usar con poco esfuerzo y en poco tiempo."),
                S("Operabilidad", "Tiene los controles y la forma de manejo que hacen cómodo usarlo y controlarlo."),
                S("Protección frente a errores de usuario", "Evita que el usuario se equivoque y le ayuda a corregirlo si lo hace."),
                S("Involucración del usuario", "La interacción es motivadora y mantiene el interés del usuario."),
                S("Inclusividad", "Lo pueden usar personas con características y capacidades muy distintas."),
                S("Asistencia al usuario", "Ofrece ayuda y apoyo cuando el usuario lo necesita."),
                S("Autodescriptividad", "Se explica solo: el usuario entiende qué hace y cómo, sin manuales.")),
            new CaracteristicaIso("fiabilidad", "Fiabilidad", "Que funcione cuando se le necesita, sin fallar.",
                S("Ausencia de fallos", "Funciona sin errores durante el uso normal."),
                S("Disponibilidad", "Está operativo y accesible cuando se necesita usarlo."),
                S("Tolerancia a fallos", "Sigue funcionando aunque haya fallos de software o de hardware."),
                S("Recuperabilidad", "Tras una caída recupera los datos y vuelve al estado deseado rápido.")),
            new CaracteristicaIso("seguridad", "Seguridad", "Proteger la información y quién puede usarla.",
                S("Confidencialidad", "Solo accede a los datos quien está autorizado."),
                S("Integridad", "Impide que se modifiquen datos o programas sin permiso."),
                S("No-repudio", "Se puede probar que una acción o evento ocurrió, y nadie puede negarlo después."),
                S("Responsabilidad", "Cada acción se puede atribuir de forma única a quien la hizo."),
                S("Autenticidad", "Se puede comprobar que una persona o un recurso es quien dice ser."),
                S("Resistencia", "Sigue funcionando y se defiende mientras sufre un ataque.")),
            new CaracteristicaIso("mantenibilidad", "Mantenibilidad", "Lo fácil que es entenderlo, corregirlo y mejorarlo.",
                S("Modularidad", "Está hecho de partes independientes: cambiar una apenas afecta a las demás."),
                S("Reusabilidad", "Sus partes sirven para construir otros sistemas."),
                S("Analizabilidad", "Es fácil descubrir qué cambiar o por qué falla algo."),
                S("Capacidad de ser modificado", "Se puede cambiar sin meter defectos ni perder calidad."),
                S("Capacidad de ser probado", "Es fácil definir pruebas y comprobar que se cumplen.")),
            new CaracteristicaIso("flexibilidad", "Flexibilidad", "Adaptarse a otros entornos y a un uso que crece.",
                S("Adaptabilidad", "Se adapta a hardware, software o entornos nuevos o distintos."),
                S("Escalabilidad", "Aguanta que crezca la carga de trabajo, los datos o los usuarios."),
                S("Instalabilidad", "Se puede instalar y desinstalar con éxito en un entorno dado."),
                S("Reemplazabilidad", "Puede sustituir a otro producto con el mismo propósito en el mismo entorno.")),
            new CaracteristicaIso("proteccion", "Protección", "Que no ponga en peligro a las personas ni al entorno.",
                S("Restricción operativa", "Limita su funcionamiento a parámetros seguros y evita estados peligrosos."),
                S("Identificación de riesgos", "Detecta los riesgos que puedan dañar a las personas, los bienes o el entorno."),
                S("Protección ante fallos", "Mantiene un estado seguro aunque algo falle."),
                S("Advertencia de peligro", "Avisa de los riesgos a tiempo para que se pueda reaccionar."),
                S("Integración segura", "Mantiene la seguridad al combinarse con otros elementos o al estar en uso."))
        };

        private static readonly Dictionary<string, string> ColumnaDelAtributo = new Dictionary<string, string> {
            { "usabilidad", "capacidad-de-interaccion" },
            { "rendimiento", "eficiencia-de-desempeno" }
        };

        /// <summary>El id de la columna de la tabla a la que corresponde un atributo de calidad de un nivel, o null.</summary>
        public static string IdDeColumna(string atributoId) {
            if (string.IsNullOrEmpty(atributoId)) return null;
            string c;
            if (ColumnaDelAtributo.TryGetValue(atributoId, out c)) return c;
            return Columnas.Any(x => x.Id == atributoId) ? atributoId : null;
        }

        public static CaracteristicaIso DeAtributo(string atributoId) {
            var id = IdDeColumna(atributoId);
            return id == null ? null : Columnas.First(x => x.Id == id);
        }
    }
}
