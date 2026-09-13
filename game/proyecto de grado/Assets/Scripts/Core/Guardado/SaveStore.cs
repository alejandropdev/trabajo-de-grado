using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nexus.Core.Evaluacion;

namespace Nexus.Core.Guardado {
    /// <summary>
    /// El repositorio de PARTIDAS (§4.10.5). Habla solo con el puerto IAlmacen.
    /// Politica de carga (INV-5 · §6.5): si algo no cuadra, SaveException con un mensaje honesto
    /// y NUNCA una partida cargada a medias.
    /// </summary>
    public sealed class SaveStore {
        public const int VersionActual = 1;
        public const string PerfilPorDefecto = "default";
        public const string PrefijoFlag = "FLG_";

        private readonly IAlmacen _almacen;
        private readonly Func<DateTime> _reloj;

        public SaveStore(IAlmacen almacen, Func<DateTime> reloj = null) {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
            _reloj = reloj ?? (() => DateTime.UtcNow);
        }

        public static string ClaveDePartida(string perfilId, string partidaId) {
            return PrefijoDePartidas(perfilId) + ValidarId(partidaId, "partidaId");
        }

        public static string PrefijoDePartidas(string perfilId) {
            return "perfiles/" + ValidarId(perfilId, "perfilId") + "/partidas/";
        }

        public SaveGame Crear(string perfilId, string nombre, string nivelId, int semilla, bool modoAula, int nivelAndamiaje) {
            var ahora = Ahora();
            var save = new SaveGame {
                Id = Guid.NewGuid().ToString("N"),
                PerfilId = string.IsNullOrEmpty(perfilId) ? PerfilPorDefecto : perfilId,
                Partida = new DatosDePartida {
                    Nombre = nombre,
                    Semilla = semilla,
                    ModoAula = modoAula,
                    FechaCreacion = ahora,
                    FechaUltimoGuardado = ahora,
                    NivelActualId = nivelId,
                    NivelAndamiaje = nivelAndamiaje
                },
                Nivel = null
            };
            Escribir(save);
            return save;
        }

        public SaveGame Cargar(string perfilId, string partidaId) {
            var clave = ClaveDePartida(perfilId, partidaId);
            if (!_almacen.Existe(clave))
                throw new SaveException($"No existe la partida '{partidaId}' del perfil '{perfilId}'.");

            string json;
            try {
                json = _almacen.Cargar(clave);
            } catch (Exception ex) {
                throw new SaveException($"(partida dañada) No se pudo leer la partida '{partidaId}': {ex.Message}", ex);
            }

            var save = Parsear(json, partidaId);
            if (save.PerfilId != perfilId || save.Id != partidaId)
                throw new SaveException(
                    $"(partida dañada) El archivo '{partidaId}' dice pertenecer al perfil '{save.PerfilId}' " +
                    $"con id '{save.Id}'. Un perfil no puede abrir las partidas de otro.");
            return save;
        }

        /// <summary>Guarda la partida tal cual. Nunca escribe una partida invalida.</summary>
        public void Guardar(SaveGame save) {
            if (save == null) throw new ArgumentNullException(nameof(save));
            var errores = Validar(save);
            if (errores.Count > 0)
                throw new SaveException("No se guarda una partida inválida:\n - " + string.Join("\n - ", errores));
            save.Partida.FechaUltimoGuardado = Ahora();
            Escribir(save);
        }

        public void Borrar(string perfilId, string partidaId) {
            _almacen.Borrar(ClaveDePartida(perfilId, partidaId));
        }

        /// <summary>Para cuando se borra un perfil: sus partidas no pueden quedar huerfanas (§8.4).</summary>
        public int BorrarTodasDelPerfil(string perfilId) {
            var claves = _almacen.ListarClaves(PrefijoDePartidas(perfilId));
            foreach (var clave in claves) _almacen.Borrar(clave);
            return claves.Count;
        }

        /// <summary>Las partidas de un perfil, la mas reciente primero. Las dañadas se listan al final, sin tumbar la lista.</summary>
        public List<ResumenDePartida> Listar(string perfilId) {
            var prefijo = PrefijoDePartidas(perfilId);
            var resultado = new List<ResumenDePartida>();

            foreach (var clave in _almacen.ListarClaves(prefijo)) {
                var partidaId = clave.Substring(prefijo.Length);
                try {
                    var s = Cargar(perfilId, partidaId);
                    resultado.Add(new ResumenDePartida {
                        Id = s.Id,
                        Nombre = s.Partida.Nombre,
                        NivelActualId = s.Partida.NivelActualId,
                        FechaUltimoGuardado = s.Partida.FechaUltimoGuardado,
                        EntreNiveles = s.Nivel == null
                    });
                } catch (Exception ex) when (ex is SaveException || ex is ArgumentException) {
                    resultado.Add(new ResumenDePartida { Id = partidaId, Nombre = "(partida dañada)", Danada = true, Error = ex.Message });
                }
            }

            return resultado
                .OrderBy(r => r.Danada)
                .ThenByDescending(r => r.FechaUltimoGuardado ?? "", StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Abre la sesion de una partida (§5.7). Sin nivel en curso se empieza el nivel (Fase 1);
        /// con nivel a medias se rehidrata. Nunca se vuelve a llamar a ElegirMetodologia, RepartirCalidad
        /// ni ElegirArquitectura: sus efectos YA estan dentro de W y Coef.
        /// </summary>
        public TSesion AbrirSesion<TSesion>(SaveGame save, string nivelIdEsperado, IFabricaDeSesion<TSesion> fabrica) {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (fabrica == null) throw new ArgumentNullException(nameof(fabrica));

            if (save.Nivel == null) return fabrica.Nueva(save);

            if (!string.IsNullOrEmpty(nivelIdEsperado) && save.Nivel.PerfilDeNivelId != nivelIdEsperado)
                throw new SaveException(
                    $"La partida es de otro nivel: se guardó en '{save.Nivel.PerfilDeNivelId}' y se intentó abrir con '{nivelIdEsperado}'.");

            return fabrica.Restaurar(save, save.Nivel);
        }

        /// <summary>Vuelca la sesion en la partida y la escribe. Es lo que llama AutoGuardado en los cinco puntos.</summary>
        public void Sincronizar(SaveGame save, ISesionPersistible sesion) {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (sesion == null) throw new ArgumentNullException(nameof(sesion));

            if (sesion.NivelTerminado) {
                // El WorldState se tira. Los flags y la competencia se quedan.
                save.Nivel = null;
                if (!string.IsNullOrEmpty(sesion.NivelId) && !save.Partida.NivelesCompletados.Contains(sesion.NivelId))
                    save.Partida.NivelesCompletados.Add(sesion.NivelId);
            } else {
                var nivel = sesion.Capturar();
                if (nivel == null) throw new SaveException("La sesión no está terminada pero Capturar() devolvió null.");
                save.Nivel = nivel;
                save.Partida.NivelActualId = sesion.NivelId;
            }

            var flags = sesion.CapturarFlags();
            save.Flags = flags == null ? new Dictionary<string, double>() : new Dictionary<string, double>(flags);
            Guardar(save);
        }

        /// <summary>Validacion de esquema. Lista vacia = valida.</summary>
        public static List<string> Validar(SaveGame s) {
            var e = new List<string>();
            if (s == null) { e.Add("La partida está vacía."); return e; }

            if (s.VersionEsquema < 1 || s.VersionEsquema > VersionActual)
                e.Add($"Versión de esquema {s.VersionEsquema} no soportada (actual: {VersionActual}).");
            if (string.IsNullOrEmpty(s.Id)) e.Add("Falta 'id'.");
            if (string.IsNullOrEmpty(s.PerfilId)) e.Add("Falta 'perfilId'.");
            if (s.Partida == null) e.Add("Falta el bloque 'partida'.");
            else if (string.IsNullOrEmpty(s.Partida.NivelActualId)) e.Add("Falta 'partida.nivelActualId'.");

            if (s.Flags != null)
                foreach (var clave in s.Flags.Keys)
                    if (clave == null || !clave.StartsWith(PrefijoFlag, StringComparison.Ordinal))
                        e.Add($"La clave de flag '{clave}' no empieza por {PrefijoFlag}.");

            if (s.Nivel != null) ValidarNivel(s.Nivel, e);
            return e;
        }

        private static void ValidarNivel(NivelEnCurso n, List<string> e) {
            if (string.IsNullOrEmpty(n.PerfilDeNivelId)) e.Add("Falta 'nivel.perfilDeNivelId'.");
            if (n.W == null || n.W.Type != JTokenType.Object) e.Add("Falta el bloque 'nivel.w' (los 13 stocks).");
            if (n.R == null || n.R.Type != JTokenType.Object) e.Add("Falta el bloque 'nivel.r' (el estado de ejecución).");
            if (n.Coef == null) e.Add("Faltan los coeficientes 'nivel.coef'.");
            else if (n.Coef.W == null || n.Coef.W.Length != 4) e.Add("'nivel.coef.w' debe tener exactamente 4 pesos.");
            if (n.Traza == null) e.Add("Falta la traza 'nivel.traza'.");
            else if (n.Traza.Entradas == null) e.Add("Falta 'nivel.traza.entradas'.");
            else
                for (var i = 0; i < n.Traza.Entradas.Count; i++)
                    if (n.Traza.Entradas[i] == null || !Veredictos.EsValido(n.Traza.Entradas[i].Veredicto))
                        e.Add($"La entrada {i} de la traza no tiene un veredicto válido.");
            if (n.Competencia == null || n.Competencia.PorObjetivo == null) e.Add("Falta la competencia 'nivel.competencia'.");
            if (n.Fase1Cerrada && string.IsNullOrEmpty(n.MetodologiaId))
                e.Add("La Fase 1 está cerrada pero falta 'nivel.metodologiaId'.");
            if (n.ColaDeEfectos == null) e.Add("Falta 'nivel.colaDeEfectos': sin ella se esquivan consecuencias recargando.");
            if (n.Telegrafiados == null) e.Add("Falta 'nivel.telegrafiados'.");
            if (n.ConsumosDelRng < 0) e.Add("'nivel.consumosDelRng' no puede ser negativo.");
        }

        private static SaveGame Parsear(string json, string partidaId) {
            // Primero la version, con el JSON en arbol: una version futura puede tener otra forma
            // y debe rechazarse como "version futura", no como "dañada".
            JToken arbol;
            try {
                arbol = JsonDeGuardado.LeerArbol(json);
            } catch (JsonException ex) {
                throw new SaveException($"(partida dañada) '{partidaId}' no es un JSON válido: {ex.Message}", ex);
            }

            var raiz = arbol as JObject;
            if (raiz == null) throw new SaveException($"(partida dañada) '{partidaId}' no contiene un objeto JSON.");

            var version = raiz["versionEsquema"];
            if (version == null || version.Type != JTokenType.Integer)
                throw new SaveException($"(partida dañada) '{partidaId}' no declara 'versionEsquema'.");

            var v = version.Value<long>();
            if (v > VersionActual)
                throw new SaveException(
                    $"La partida '{partidaId}' se guardó con la versión de esquema {v}, más nueva que la que entiende " +
                    $"este juego ({VersionActual}). Actualiza el juego: no se carga a medias.");

            SaveGame save;
            try {
                save = JsonDeGuardado.Deserializar<SaveGame>(json);
            } catch (JsonException ex) {
                throw new SaveException($"(partida dañada) '{partidaId}' no tiene la forma de una partida: {ex.Message}", ex);
            }

            var errores = Validar(save);
            if (errores.Count > 0)
                throw new SaveException($"(partida dañada) '{partidaId}':\n - " + string.Join("\n - ", errores));
            return save;
        }

        private void Escribir(SaveGame save) {
            _almacen.Guardar(ClaveDePartida(save.PerfilId, save.Id), JsonDeGuardado.Serializar(save));
        }

        private string Ahora() {
            return _reloj().ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }

        private static string ValidarId(string id, string nombre) {
            if (string.IsNullOrEmpty(id) || id.IndexOfAny(new[] { '/', '\\' }) >= 0 || id.Contains(".."))
                throw new ArgumentException($"'{id}' no es un {nombre} válido.", nombre);
            return id;
        }
    }
}
