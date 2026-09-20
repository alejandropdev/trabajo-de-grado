using System;
using System.Collections.Generic;
using System.IO;

namespace Nexus.Core.Datos {
    /// <summary>
    /// C10 · De donde salen los catalogos (§S.5). Era la unica asimetria que quedaba en el diseño:
    /// el guardado tenia su puerto (IAlmacen) desde el principio, pero el contenido se leia con una
    /// llamada directa a Application.streamingAssetsPath, y eso ataba el motor a Unity.
    ///
    /// Con este puerto, los tests cargan catalogos de memoria y el arnes de consola los carga de disco,
    /// sin que Nexus.Core sepa que existe un StreamingAssets.
    ///
    /// Adaptadores: CatalogoEnMemoria (tests) · CatalogoDeArchivos (arnes) ·
    /// CatalogoDeStreamingAssets (capa Unity, llega en la Fase B).
    /// </summary>
    public interface ICatalogSource {
        /// <summary>Lee un archivo del catalogo. Lanza si no existe: un catalogo incompleto no se carga a medias.</summary>
        string LeerCatalogo(string rutaRelativa);

        bool Existe(string rutaRelativa);

        /// <summary>
        /// Los .json que hay en una carpeta, ORDENADOS. El orden importa: el catalogo de eventos
        /// define el orden de la ruleta, y sin un orden estable la misma semilla dejaria de dar
        /// la misma partida segun como el sistema de archivos devuelva la lista.
        /// </summary>
        List<string> ListarCatalogos(string carpetaRelativa);
    }

    /// <summary>Catalogos en memoria, para tests y para contenido embebido.</summary>
    public sealed class CatalogoEnMemoria : ICatalogSource {
        private readonly Dictionary<string, string> _archivos = new Dictionary<string, string>(StringComparer.Ordinal);

        public CatalogoEnMemoria Con(string rutaRelativa, string contenido) {
            _archivos[Normalizar(rutaRelativa)] = contenido;
            return this;
        }

        public string LeerCatalogo(string rutaRelativa) {
            string contenido;
            if (!_archivos.TryGetValue(Normalizar(rutaRelativa), out contenido))
                throw new FileNotFoundException($"El catalogo no contiene '{rutaRelativa}'.");
            return contenido;
        }

        public bool Existe(string rutaRelativa) {
            return _archivos.ContainsKey(Normalizar(rutaRelativa));
        }

        public List<string> ListarCatalogos(string carpetaRelativa) {
            var prefijo = Normalizar(carpetaRelativa);
            if (prefijo.Length > 0 && !prefijo.EndsWith("/", StringComparison.Ordinal)) prefijo += "/";

            var resultado = new List<string>();
            foreach (var clave in _archivos.Keys) {
                if (!clave.StartsWith(prefijo, StringComparison.Ordinal)) continue;
                if (clave.IndexOf('/', prefijo.Length) >= 0) continue;   // solo hijos directos
                if (clave.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) resultado.Add(clave);
            }
            resultado.Sort(StringComparer.Ordinal);
            return resultado;
        }

        private static string Normalizar(string ruta) {
            return (ruta ?? "").Replace('\\', '/').TrimStart('/');
        }
    }

    /// <summary>
    /// Catalogos en disco bajo una raiz. Vive en Nexus.Core y no en la capa Unity, y eso es correcto:
    /// noEngineReferences prohibe UnityEngine, no System.IO. Lo unico que Unity aporta es la ruta base.
    /// </summary>
    public sealed class CatalogoDeArchivos : ICatalogSource {
        private readonly string _raiz;

        public CatalogoDeArchivos(string raiz) {
            if (string.IsNullOrEmpty(raiz)) throw new ArgumentException("Hace falta una carpeta raiz.", nameof(raiz));
            _raiz = raiz;
        }

        public string LeerCatalogo(string rutaRelativa) {
            var ruta = Resolver(rutaRelativa);
            if (!File.Exists(ruta))
                throw new FileNotFoundException($"Falta el catalogo '{rutaRelativa}'. Se buscaba en: {ruta}", ruta);
            return File.ReadAllText(ruta);
        }

        public bool Existe(string rutaRelativa) {
            return File.Exists(Resolver(rutaRelativa));
        }

        public List<string> ListarCatalogos(string carpetaRelativa) {
            var carpeta = Resolver(carpetaRelativa);
            var resultado = new List<string>();
            if (!Directory.Exists(carpeta)) return resultado;

            var prefijo = (carpetaRelativa ?? "").Replace('\\', '/').TrimStart('/').TrimEnd('/');
            foreach (var archivo in Directory.GetFiles(carpeta, "*.json"))
                resultado.Add((prefijo.Length == 0 ? "" : prefijo + "/") + Path.GetFileName(archivo));

            resultado.Sort(StringComparer.Ordinal);   // orden estable: de el depende el determinismo
            return resultado;
        }

        /// <summary>Un catalogo no puede salirse de su carpeta, igual que un guardado no sale de su perfil.</summary>
        private string Resolver(string rutaRelativa) {
            var limpia = (rutaRelativa ?? "").Replace('\\', '/').TrimStart('/');
            if (limpia.Contains("..") || Path.IsPathRooted(limpia))
                throw new ArgumentException($"'{rutaRelativa}' no es una ruta de catalogo valida.", nameof(rutaRelativa));
            return Path.Combine(_raiz, limpia.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
