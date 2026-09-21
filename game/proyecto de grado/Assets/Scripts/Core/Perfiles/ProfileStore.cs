using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Nexus.Core {
    public class ProfileStore {
        private const string PREFIJO = "perfiles/";
        private const int VERSION_ACTUAL = 1;
        private readonly IAlmacen _almacen;

        public ProfileStore(IAlmacen almacen) {
            _almacen = almacen;
        }

        public PlayerProfile CrearPerfil(string nombre) {
            var perfil = new PlayerProfile {
                idPerfil = Guid.NewGuid().ToString("N"),
                nombreEstudiante = nombre,
                fechaCreacion = DateTime.UtcNow.ToString("o"),
                version = VERSION_ACTUAL
            };
            Guardar(perfil);
            return perfil;
        }

        public List<PlayerProfile> ListarPerfiles() {
            var claves = _almacen.ListarClaves(PREFIJO);
            var resultado = new List<PlayerProfile>();

            foreach (var clave in claves) {
                var json = _almacen.Cargar(clave);
                var perfil = JsonConvert.DeserializeObject<PlayerProfile>(json);
                perfil = MigrarSiHaceFalta(perfil);
                resultado.Add(perfil);
            }

            // Más reciente primero
            return resultado.OrderByDescending(p => p.fechaCreacion).ToList();
        }

        public void Renombrar(string idPerfil, string nuevoNombre) {
            var perfil = CargarPorId(idPerfil);
            perfil.nombreEstudiante = nuevoNombre;
            Guardar(perfil);
        }

        public void Borrar(string idPerfil) {
            _almacen.Borrar(ClaveDe(idPerfil));
        }

        public PlayerProfile CargarPorId(string idPerfil) {
            var json = _almacen.Cargar(ClaveDe(idPerfil));
            var perfil = JsonConvert.DeserializeObject<PlayerProfile>(json);
            return MigrarSiHaceFalta(perfil);
        }

        /// <summary>Guarda un perfil modificado: el pre-test de la entrevista, los coleccionables globales…</summary>
        public void Guardar(PlayerProfile perfil) {
            var json = JsonConvert.SerializeObject(perfil, Formatting.Indented);
            _almacen.Guardar(ClaveDe(perfil.idPerfil), json);
        }

        private string ClaveDe(string idPerfil) => PREFIJO + idPerfil;

        /// <summary>
        /// Si algún día cambias la forma de PlayerProfile, este es el único lugar
        /// donde traduces un perfil viejo a la forma nueva sin perder datos.
        /// </summary>
        private PlayerProfile MigrarSiHaceFalta(PlayerProfile perfil) {
            if (perfil.version < VERSION_ACTUAL) {
                // Ejemplo de migración futura:
                // if (perfil.version == 0) { /* rellenar campo nuevo con valor por defecto */ }
                perfil.version = VERSION_ACTUAL;
            }
            return perfil;
        }
    }
}
