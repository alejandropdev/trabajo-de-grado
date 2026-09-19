using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nexus.Core {
    /// <summary>
    /// IAlmacen sobre disco con escritura ATOMICA (§4.10.1): se escribe un .tmp y se sustituye el
    /// archivo real de golpe. Los jugadores cierran los juegos a lo bruto; sin esto, un cierre en mitad
    /// de un guardado deja un JSON a medias y la partida se pierde entera.
    /// Vive en Core porque System.IO es legal aqui: la raiz la pasa quien lo crea
    /// (Unity: Application.persistentDataPath · arnes: una carpeta cualquiera).
    /// Misma semantica de claves que Nexus.Unity.AlmacenDeArchivos (clave + ".json").
    /// </summary>
    public sealed class AlmacenDeArchivosAtomico : IAlmacen {
        private const string Extension = ".json";
        private const string ExtensionTemporal = ".tmp";
        private static readonly Encoding Utf8SinBom = new UTF8Encoding(false);

        private readonly string _raiz;

        public AlmacenDeArchivosAtomico(string raiz) {
            if (string.IsNullOrEmpty(raiz)) throw new ArgumentException("La raiz del almacen no puede estar vacia.", nameof(raiz));
            _raiz = raiz;
        }

        public void Guardar(string clave, string contenidoJson) {
            var ruta = RutaCompleta(clave);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));

            var temporal = ruta + ExtensionTemporal;
            File.WriteAllText(temporal, contenidoJson ?? "", Utf8SinBom);

            if (File.Exists(ruta)) File.Replace(temporal, ruta, null);
            else File.Move(temporal, ruta);
        }

        public string Cargar(string clave) {
            return File.ReadAllText(RutaCompleta(clave), Utf8SinBom);
        }

        public bool Existe(string clave) {
            return File.Exists(RutaCompleta(clave));
        }

        public void Borrar(string clave) {
            var ruta = RutaCompleta(clave);
            if (File.Exists(ruta)) File.Delete(ruta);
        }

        public List<string> ListarClaves(string prefijo) {
            prefijo = prefijo ?? "";
            ValidarSinEscapar(prefijo);
            var carpeta = Path.Combine(_raiz, prefijo);
            if (!Directory.Exists(carpeta)) return new List<string>();

            return Directory.GetFiles(carpeta, "*" + Extension)
                .Select(a => prefijo + Path.GetFileNameWithoutExtension(a))
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();
        }

        private string RutaCompleta(string clave) {
            if (string.IsNullOrEmpty(clave)) throw new ArgumentException("La clave no puede estar vacia.", nameof(clave));
            ValidarSinEscapar(clave);
            return Path.Combine(_raiz, clave + Extension);
        }

        /// <summary>Una clave no puede salir de la raiz: un perfil no puede leer los datos de otro (SRS §3.6.4).</summary>
        private static void ValidarSinEscapar(string clave) {
            if (Path.IsPathRooted(clave) || clave.Split('/', '\\').Any(s => s == ".."))
                throw new ArgumentException($"La clave '{clave}' intenta salir de la carpeta del almacen.", nameof(clave));
        }
    }
}
