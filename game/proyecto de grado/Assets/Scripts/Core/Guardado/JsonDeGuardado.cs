using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Nexus.Core.Guardado {
    /// <summary>
    /// La configuracion de Newtonsoft del guardado. Tres trampas que cuestan una tarde cada una:
    /// 1 · ObjectCreationHandling.Replace: sin el, una lista ya inicializada en C# AÑADE los valores del JSON.
    /// 2 · ProcessDictionaryKeys = false: sin el, el camelCase convierte "FLG_DEUDA_TECNICA" en "fLG_DEUDA_TECNICA"
    ///     y "OA-DIS-01" en "oA-DIS-01", y los flags y la competencia dejan de encontrarse al cargar.
    /// 3 · DateParseHandling.None: sin el, "2026-09-06T14:22:09.1234567Z" se lee como DateTime y vuelve
    ///     como texto con el formato del sistema operativo.
    /// </summary>
    public static class JsonDeGuardado {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Double,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            DateParseHandling = DateParseHandling.None,
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver {
                NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false, OverrideSpecifiedNames = true }
            }
        };

        private static readonly JsonSerializer Serializador = JsonSerializer.Create(Settings);

        public static string Serializar(object o) {
            return JsonConvert.SerializeObject(o, Settings);
        }

        public static T Deserializar<T>(string json) {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        /// <summary>Lee un JSON a arbol sin convertir fechas. Lanza JsonException si no es JSON valido.</summary>
        public static JToken LeerArbol(string json) {
            using (var lector = new JsonTextReader(new StringReader(json ?? "")) {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double
            }) {
                var arbol = JToken.ReadFrom(lector);
                if (lector.Read() && lector.TokenType != JsonToken.Comment)
                    throw new JsonReaderException("Hay contenido despues del final del JSON.");
                return arbol;
            }
        }

        /// <summary>Convierte un objeto del motor (WorldState, RuntimeState, EfectoEnCola…) en un bloque del guardado.</summary>
        public static JToken ABloque(object o) {
            return o == null ? null : JToken.FromObject(o, Serializador);
        }

        /// <summary>El camino de vuelta de ABloque.</summary>
        public static T DeBloque<T>(JToken bloque) {
            if (bloque == null) throw new ArgumentNullException(nameof(bloque));
            return bloque.ToObject<T>(Serializador);
        }
    }
}
