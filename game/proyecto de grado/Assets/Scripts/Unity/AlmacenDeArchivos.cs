using Nexus.Core;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nexus.Unity {
    public class AlmacenDeArchivos : IAlmacen {
        private readonly string _raiz;

        public AlmacenDeArchivos() {
            _raiz = Application.persistentDataPath;
        }

        public void Guardar(string clave, string contenidoJson) {
            var ruta = RutaCompleta(clave);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(ruta, contenidoJson);
        }

        public string Cargar(string clave) {
            return File.ReadAllText(RutaCompleta(clave));
        }

        public bool Existe(string clave) {
            return File.Exists(RutaCompleta(clave));
        }

        public void Borrar(string clave) {
            var ruta = RutaCompleta(clave);
            if (File.Exists(ruta)) File.Delete(ruta);
        }

        public List<string> ListarClaves(string prefijo) {
            var carpeta = Path.Combine(_raiz, prefijo);
            var resultado = new List<string>();

            if (!Directory.Exists(carpeta)) return resultado;

            foreach (var archivo in Directory.GetFiles(carpeta, "*.json")) {
                var nombreSinExtension = Path.GetFileNameWithoutExtension(archivo);
                resultado.Add(prefijo + nombreSinExtension);
            }
            return resultado;
        }

        private string RutaCompleta(string clave) {
            return Path.Combine(_raiz, clave + ".json");
        }
    }
}
