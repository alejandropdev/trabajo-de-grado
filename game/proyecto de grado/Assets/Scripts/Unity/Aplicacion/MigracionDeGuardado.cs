using System;
using System.IO;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// Trae las partidas guardadas con la identidad anterior del ejecutable.
    ///
    /// Unity arma persistentDataPath con la empresa y el producto (en Windows:
    /// %USERPROFILE%\AppData\LocalLow\&lt;empresa&gt;\&lt;producto&gt;). Hasta noviembre de 2026 el proyecto
    /// usaba los valores de la plantilla ("DefaultCompany" / "proyecto de grado"); al darle su nombre real los
    /// perfiles quedarian en la carpeta vieja y el juego arrancaria como si nunca se hubiera jugado.
    ///
    /// Se copia (no se mueve) la carpeta RutasDeGuardado.Carpeta, y solo si la nueva todavia no existe: una vez
    /// que se juega con el nombre nuevo no se vuelve a tocar nada, y la copia vieja queda como respaldo.
    /// </summary>
    public static class MigracionDeGuardado {
        public const string EmpresaAnterior = "DefaultCompany";
        public const string ProductoAnterior = "proyecto de grado";

        /// <summary>Devuelve cuantos archivos copio (0 si no habia nada que migrar o ya estaba hecho).</summary>
        public static int MigrarSiHaceFalta() {
            try {
                var actual = Application.persistentDataPath;
                var anterior = RutaAnterior(actual, Application.companyName, Application.productName);
                if (anterior == null) return 0;
                var copiados = Copiar(Path.Combine(anterior, RutasDeGuardado.Carpeta), Path.Combine(actual, RutasDeGuardado.Carpeta));
                if (copiados > 0) Debug.Log("[Nexus] Partidas migradas desde " + anterior + " (" + copiados + " archivos).");
                return copiados;
            } catch (Exception e) {
                // Nunca impedir el arranque por esto: en el peor caso el jugador ve la lista de perfiles vacia.
                Debug.LogWarning("[Nexus] No se pudieron migrar las partidas anteriores: " + e.Message);
                return 0;
            }
        }

        /// <summary>
        /// La carpeta de datos que tendria el juego con la identidad anterior, o null si no aplica (la ruta no
        /// sigue el patron &lt;base&gt;/&lt;empresa&gt;/&lt;producto&gt;, o la identidad no cambio).
        /// </summary>
        public static string RutaAnterior(string actual, string empresa, string producto) {
            if (string.IsNullOrEmpty(actual)) return null;
            if (empresa == EmpresaAnterior && producto == ProductoAnterior) return null;
            var dirProducto = new DirectoryInfo(actual.TrimEnd('/', '\\'));
            var dirEmpresa = dirProducto.Parent;
            if (dirEmpresa == null || dirEmpresa.Parent == null) return null;
            if (dirProducto.Name != producto || dirEmpresa.Name != empresa) return null;
            return Path.Combine(dirEmpresa.Parent.FullName, EmpresaAnterior, ProductoAnterior);
        }

        private static int Copiar(string origen, string destino) {
            if (!Directory.Exists(origen) || Directory.Exists(destino)) return 0;
            var temporal = destino + ".migrando";
            if (Directory.Exists(temporal)) Directory.Delete(temporal, true);
            var n = 0;
            foreach (var archivo in Directory.GetFiles(origen, "*", SearchOption.AllDirectories)) {
                var relativa = archivo.Substring(origen.Length).TrimStart('/', '\\');
                var copia = Path.Combine(temporal, relativa);
                Directory.CreateDirectory(Path.GetDirectoryName(copia));
                File.Copy(archivo, copia);
                n++;
            }
            // Se renombra al final: si el juego se cierra a mitad de la copia no queda una carpeta a medias que
            // bloquee el siguiente intento.
            if (n > 0) Directory.Move(temporal, destino);
            return n;
        }
    }
}
