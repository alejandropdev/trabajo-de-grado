using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nexus.Core {
    /// <summary>
    /// IAlmacen en memoria, para tests: instantaneo y sin dejar basura en disco.
    /// Imita la semantica de AlmacenDeArchivos: ListarClaves solo devuelve los hijos directos del prefijo.
    /// </summary>
    public sealed class AlmacenEnMemoria : IAlmacen {
        private readonly Dictionary<string, string> _datos = new Dictionary<string, string>(StringComparer.Ordinal);

        public int Cantidad {
            get { return _datos.Count; }
        }

        public void Guardar(string clave, string contenidoJson) {
            ValidarClave(clave);
            _datos[clave] = contenidoJson;
        }

        public string Cargar(string clave) {
            string json;
            if (!_datos.TryGetValue(clave ?? "", out json))
                throw new FileNotFoundException($"No existe la clave '{clave}'.", clave);
            return json;
        }

        public bool Existe(string clave) {
            return clave != null && _datos.ContainsKey(clave);
        }

        public void Borrar(string clave) {
            if (clave != null) _datos.Remove(clave);
        }

        public List<string> ListarClaves(string prefijo) {
            prefijo = prefijo ?? "";
            return _datos.Keys
                .Where(k => k.StartsWith(prefijo, StringComparison.Ordinal))
                .Where(k => k.Length > prefijo.Length && k.IndexOf('/', prefijo.Length) < 0)
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();
        }

        private static void ValidarClave(string clave) {
            if (string.IsNullOrEmpty(clave)) throw new ArgumentException("La clave no puede estar vacia.", nameof(clave));
        }
    }
}
