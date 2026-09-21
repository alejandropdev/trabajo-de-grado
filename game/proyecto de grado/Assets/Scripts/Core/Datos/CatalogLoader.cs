using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nexus.Core.Coleccion;
using Nexus.Core.Evaluacion;
using Nexus.Core.Eventos;
using Nexus.Core.Guardado;
using Nexus.Core.Metodologia;
using Nexus.Core.Minijuegos;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;

namespace Nexus.Core.Datos {
    /// <summary>Todo el contenido del juego, ya cargado y validado. Es lo que AppRoot tendra en memoria.</summary>
    public sealed class Catalogo {
        public List<EventDefinition> Eventos = new List<EventDefinition>();
        public Dictionary<string, LevelProfile> Niveles = new Dictionary<string, LevelProfile>(StringComparer.Ordinal);
        public Dictionary<string, MethodologyProfile> Metodologias =
            new Dictionary<string, MethodologyProfile>(StringComparer.Ordinal);

        /// <summary>El INDICE de minijuegos: lo que el motor necesita para elegir. Las escenas se cargan aparte.</summary>
        public List<MinigameDefinition> Minijuegos = new List<MinigameDefinition>();

        public List<NarrativeBeat> Beats = new List<NarrativeBeat>();

        /// <summary>El censo de flags. Existe aunque el codigo no lo necesitaria: el catalogo documenta.</summary>
        public List<DefinicionDeFlag> Flags = new List<DefinicionDeFlag>();

        /// <summary>Lo que se dice en cada escena. Los beats dicen cuando; esto dice que.</summary>
        public List<Guion> Guiones = new List<Guion>();

        /// <summary>La entrevista del N0. Null si el catalogo no la trae (un prototipo puede no tenerla).</summary>
        public PruebaDeAdmision Admision;

        public List<Coleccionable> Coleccionables = new List<Coleccionable>();

        public override string ToString() {
            return $"{Eventos.Count} eventos, {Niveles.Count} niveles, {Metodologias.Count} metodologias, " +
                   $"{Minijuegos.Count} minijuegos, {Beats.Count} beats, {Flags.Count} flags, " +
                   $"{Guiones.Count} guiones, {(Admision == null ? 0 : Admision.Preguntas.Count)} preguntas, " +
                   $"{Coleccionables.Count} coleccionables";
        }
    }

    /// <summary>
    /// C10b · Convierte JSON en objetos del motor y NO los deja pasar sin validar (§4.11, INV-5).
    ///
    /// Se lee UNA vez, al arrancar. Despues de esto el motor no vuelve a tocar JSON hasta el guardado,
    /// y por eso todo el coste de validar es gratis: se paga en el arranque, no en la jornada.
    ///
    /// Reutiliza la configuracion de Newtonsoft de JsonDeGuardado a proposito. Esas tres trampas
    /// (ObjectCreationHandling.Replace, ProcessDictionaryKeys = false, DateParseHandling.None) ya
    /// costaron una tarde cada una; tener una segunda configuracion seria volver a pagarlas.
    /// </summary>
    public static class CatalogLoader {
        public const string CarpetaEventos = "eventos";
        public const string CarpetaNiveles = "niveles";
        public const string CarpetaMetodologias = "metodologias";
        public const string CarpetaMinijuegos = "minijuegos";

        /// <summary>
        /// El indice va en un archivo con nombre fijo porque la carpeta 'minijuegos/' contiene tambien
        /// las escenas (MJ-*.json), que las carga la UI y no el motor.
        /// </summary>
        public const string ArchivoIndiceMinijuegos = "minijuegos/indice.json";
        public const string ArchivoNarrativa = "narrativa/narrativa.json";
        public const string ArchivoFlags = "flags.json";
        public const string ArchivoGuiones = "narrativa/guiones.json";
        public const string ArchivoAdmision = "prueba-de-admision.json";
        public const string ArchivoColeccionables = "coleccionables.json";

        public static JsonSerializerSettings Settings { get { return JsonDeGuardado.Settings; } }

        /// <summary>
        /// El sobre de eventos/*.json: { "version": 1, "eventos": [ … ] }.
        /// Propiedades y no campos para que el compilador no avise de que nadie las asigna:
        /// las rellena Newtonsoft por reflexion.
        /// </summary>
        private sealed class ArchivoDeEventos {
            public int Version { get; set; }
            public List<EventDefinition> Eventos { get; set; }
        }

        /// <summary>El sobre de minijuegos/indice.json: { "version": 1, "minijuegos": [ … ] }.</summary>
        private sealed class ArchivoDeMinijuegos {
            public int Version { get; set; }
            public List<MinigameDefinition> Minijuegos { get; set; }
        }

        private sealed class ArchivoDeNarrativa {
            public int Version { get; set; }
            public List<NarrativeBeat> Beats { get; set; }
        }

        private sealed class ArchivoDeFlags {
            public int Version { get; set; }
            public List<DefinicionDeFlag> Flags { get; set; }
        }

        private sealed class ArchivoDeGuiones {
            public int Version { get; set; }
            public List<Guion> Guiones { get; set; }
        }

        private sealed class ArchivoDeColeccionables {
            public int Version { get; set; }
            public List<Coleccionable> Coleccionables { get; set; }
        }

        // ------------------------------------------------------------------ uno a uno

        public static List<EventDefinition> CargarEventos(string json) {
            var eventos = ParsearEventos(json, "eventos");
            Exigir("El catalogo de eventos", SchemaValidator.ValidarEventos(eventos));
            return eventos;
        }

        public static LevelProfile CargarPerfil(string json) {
            var perfil = Parsear<LevelProfile>(json, "el perfil de nivel");
            Exigir("El perfil de nivel", SchemaValidator.ValidarNivel(perfil));
            return perfil;
        }

        public static MethodologyProfile CargarMetodologia(string json) {
            var perfil = Parsear<MethodologyProfile>(json, "la metodologia");
            Exigir("La metodologia", SchemaValidator.ValidarMetodologia(perfil));
            return perfil;
        }

        public static List<MinigameDefinition> CargarMinijuegos(string json, ICatalogSource fuente = null) {
            var minijuegos = ParsearMinijuegos(json, ArchivoIndiceMinijuegos);
            Exigir("El indice de minijuegos", SchemaValidator.ValidarMinijuegos(minijuegos, fuente));
            return minijuegos;
        }

        public static List<NarrativeBeat> CargarNarrativa(string json) {
            var archivo = Parsear<ArchivoDeNarrativa>(json, ArchivoNarrativa);
            var beats = archivo.Beats ?? new List<NarrativeBeat>();
            Exigir("El catalogo narrativo", SchemaValidator.ValidarNarrativa(beats));
            return beats;
        }

        public static List<DefinicionDeFlag> CargarFlags(string json) {
            var archivo = Parsear<ArchivoDeFlags>(json, ArchivoFlags);
            var flags = archivo.Flags ?? new List<DefinicionDeFlag>();
            Exigir("El censo de flags", SchemaValidator.ValidarFlags(flags));
            return flags;
        }

        public static List<Guion> CargarGuiones(string json) {
            var archivo = Parsear<ArchivoDeGuiones>(json, ArchivoGuiones);
            var guiones = archivo.Guiones ?? new List<Guion>();
            Exigir("Los guiones", SchemaValidator.ValidarGuiones(guiones));
            return guiones;
        }

        public static PruebaDeAdmision CargarAdmision(string json) {
            var prueba = Parsear<PruebaDeAdmision>(json, ArchivoAdmision);
            Exigir("La prueba de admision", SchemaValidator.ValidarAdmision(prueba));
            return prueba;
        }

        public static List<Coleccionable> CargarColeccionables(string json) {
            var archivo = Parsear<ArchivoDeColeccionables>(json, ArchivoColeccionables);
            var coleccionables = archivo.Coleccionables ?? new List<Coleccionable>();
            Exigir("Los coleccionables", SchemaValidator.ValidarColeccionables(coleccionables));
            return coleccionables;
        }

        public static string Serializar(object o) {
            return JsonConvert.SerializeObject(o, Settings);
        }

        // ------------------------------------------------------------------ todo de golpe

        /// <summary>
        /// Carga el catalogo entero de una fuente y lo valida DE UNA VEZ.
        ///
        /// Parsea primero todo y valida despues a proposito: si validara archivo a archivo, un autor de
        /// contenido con seis errores repartidos tendria que arrancar el juego seis veces para verlos.
        /// Asi los ve todos en el primer intento.
        /// </summary>
        public static Catalogo CargarTodo(ICatalogSource fuente) {
            if (fuente == null) throw new ArgumentNullException(nameof(fuente));

            var catalogo = new Catalogo();
            var errores = new List<string>();

            foreach (var ruta in fuente.ListarCatalogos(CarpetaEventos)) {
                try {
                    catalogo.Eventos.AddRange(ParsearEventos(fuente.LeerCatalogo(ruta), ruta));
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            foreach (var ruta in fuente.ListarCatalogos(CarpetaNiveles)) {
                try {
                    var perfil = Parsear<LevelProfile>(fuente.LeerCatalogo(ruta), ruta);
                    if (perfil == null || string.IsNullOrEmpty(perfil.Id)) { errores.Add($"{ruta}: falta 'id'."); continue; }
                    if (catalogo.Niveles.ContainsKey(perfil.Id)) { errores.Add($"{ruta}: el nivel '{perfil.Id}' esta repetido."); continue; }
                    catalogo.Niveles[perfil.Id] = perfil;
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            foreach (var ruta in fuente.ListarCatalogos(CarpetaMetodologias)) {
                try {
                    var perfil = Parsear<MethodologyProfile>(fuente.LeerCatalogo(ruta), ruta);
                    if (perfil == null || string.IsNullOrEmpty(perfil.Id)) { errores.Add($"{ruta}: falta 'id'."); continue; }
                    if (catalogo.Metodologias.ContainsKey(perfil.Id)) { errores.Add($"{ruta}: la metodologia '{perfil.Id}' esta repetida."); continue; }
                    catalogo.Metodologias[perfil.Id] = perfil;
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            // El indice de minijuegos es opcional: un nivel puede no tener ventana de verbos.
            if (fuente.Existe(ArchivoIndiceMinijuegos)) {
                try {
                    catalogo.Minijuegos = ParsearMinijuegos(fuente.LeerCatalogo(ArchivoIndiceMinijuegos),
                                                            ArchivoIndiceMinijuegos);
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            // Narrativa y flags tambien son opcionales: un prototipo de motor puede correr sin trama.
            if (fuente.Existe(ArchivoNarrativa)) {
                try {
                    var archivo = Parsear<ArchivoDeNarrativa>(fuente.LeerCatalogo(ArchivoNarrativa), ArchivoNarrativa);
                    catalogo.Beats = archivo.Beats ?? new List<NarrativeBeat>();
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            if (fuente.Existe(ArchivoFlags)) {
                try {
                    var archivo = Parsear<ArchivoDeFlags>(fuente.LeerCatalogo(ArchivoFlags), ArchivoFlags);
                    catalogo.Flags = archivo.Flags ?? new List<DefinicionDeFlag>();
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            // Guiones, entrevista y coleccionables: opcionales como la narrativa. Si estan, se validan enteros.
            if (fuente.Existe(ArchivoGuiones)) {
                try {
                    var archivo = Parsear<ArchivoDeGuiones>(fuente.LeerCatalogo(ArchivoGuiones), ArchivoGuiones);
                    catalogo.Guiones = archivo.Guiones ?? new List<Guion>();
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            if (fuente.Existe(ArchivoAdmision)) {
                try {
                    catalogo.Admision = Parsear<PruebaDeAdmision>(fuente.LeerCatalogo(ArchivoAdmision), ArchivoAdmision);
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            if (fuente.Existe(ArchivoColeccionables)) {
                try {
                    var archivo = Parsear<ArchivoDeColeccionables>(fuente.LeerCatalogo(ArchivoColeccionables),
                                                                   ArchivoColeccionables);
                    catalogo.Coleccionables = archivo.Coleccionables ?? new List<Coleccionable>();
                } catch (SchemaException ex) {
                    errores.Add(ex.Message);
                }
            }

            errores.AddRange(SchemaValidator.ValidarCatalogo(catalogo, fuente));
            Exigir("El catalogo", errores);
            return catalogo;
        }

        // ------------------------------------------------------------------ interno

        private static List<EventDefinition> ParsearEventos(string json, string donde) {
            // Se comprueba antes de deserializar porque el error de Newtonsoft para una lista suelta
            // habla de tipos de C#, y quien esta escribiendo el JSON necesita que se le diga la forma.
            if (json != null && json.TrimStart().StartsWith("[", StringComparison.Ordinal))
                throw new SchemaException(FormaEsperada(donde));

            var archivo = Parsear<ArchivoDeEventos>(json, donde);
            if (archivo == null || archivo.Eventos == null) throw new SchemaException(FormaEsperada(donde));
            return archivo.Eventos;
        }

        private static List<MinigameDefinition> ParsearMinijuegos(string json, string donde) {
            if (json != null && json.TrimStart().StartsWith("[", StringComparison.Ordinal))
                throw new SchemaException($"{donde}: se esperaba {{ \"version\": 1, \"minijuegos\": [ … ] }}.");

            var archivo = Parsear<ArchivoDeMinijuegos>(json, donde);
            if (archivo == null || archivo.Minijuegos == null)
                throw new SchemaException($"{donde}: se esperaba {{ \"version\": 1, \"minijuegos\": [ … ] }}.");
            return archivo.Minijuegos;
        }

        private static string FormaEsperada(string donde) {
            return $"{donde}: se esperaba un objeto con la forma {{ \"version\": 1, \"eventos\": [ … ] }}. " +
                   "Una lista suelta de eventos no vale: la version del esquema tiene que estar en el archivo.";
        }

        private static T Parsear<T>(string json, string donde) where T : class {
            if (string.IsNullOrEmpty(json)) throw new SchemaException($"{donde}: el archivo esta vacio.");

            T resultado;
            try {
                resultado = JsonConvert.DeserializeObject<T>(json, Settings);
            } catch (JsonException ex) {
                // El mensaje de Newtonsoft trae linea y posicion: es lo mas util que se le puede dar
                // a quien esta escribiendo el JSON, asi que se pasa tal cual.
                throw new SchemaException($"{donde}: no se pudo leer el JSON. {ex.Message}");
            }

            if (resultado == null) throw new SchemaException($"{donde}: el JSON no contiene un objeto.");
            return resultado;
        }

        private static void Exigir(string que, List<string> errores) {
            if (errores != null && errores.Count > 0) throw new SchemaException(que, errores);
        }
    }
}
