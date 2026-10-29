using System;
using System.IO;
using System.Linq;
using Nexus.Core.Reportes;
using Nexus.Core.Sesion;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// Saca el informe de lecciones al disco: lo que el juego sabe (perfil, partida, catalogo) lo convierte en los
    /// DatosDelReporte, ReporteDeLecciones lo maqueta en PDF y esto lo guarda en Documentos\NexusProtocol\Reportes,
    /// una carpeta que el estudiante encuentra sin buscar en AppData para adjuntar el archivo a un correo.
    /// </summary>
    public static class ExportadorDeReportes {
        public const string NombreDeCarpeta = "NexusProtocol";
        public const string SubcarpetaDeReportes = "Reportes";

        /// <summary>Documentos del usuario; si el sistema no la da (o esta bloqueada), la carpeta de datos del juego.</summary>
        public static string Carpeta() {
            string documentos = null;
            try { documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); } catch (Exception) { /* sin carpeta de documentos */ }
            var raiz = string.IsNullOrEmpty(documentos) ? Application.persistentDataPath : documentos;
            return Path.Combine(raiz, NombreDeCarpeta, SubcarpetaDeReportes);
        }

        /// <summary>Maqueta el informe del cierre y lo guarda. Devuelve la ruta del PDF. Lanza si no se puede escribir.</summary>
        public static string Exportar(AppRoot app, DebriefReport reporte) {
            var datos = DatosDe(app, reporte);
            var pdf = ReporteDeLecciones.Generar(reporte, datos);
            return Guardar(pdf, NombreDeReporte.Para(reporte.NivelId, datos.Estudiante, datos.Fecha));
        }

        /// <summary>Escribe a un temporal y lo mueve: un corte a medias no deja un PDF roto con el nombre bueno.</summary>
        public static string Guardar(byte[] pdf, string nombre) {
            var carpeta = Carpeta();
            Directory.CreateDirectory(carpeta);
            var ruta = Path.Combine(carpeta, nombre);
            var temporal = ruta + ".tmp";
            File.WriteAllBytes(temporal, pdf);
            if (File.Exists(ruta)) File.Delete(ruta);
            File.Move(temporal, ruta);
            return ruta;
        }

        /// <summary>Abre el Explorador con el archivo seleccionado. Un fallo aqui no tumba nada: el archivo ya esta guardado.</summary>
        public static void MostrarEnCarpeta(string ruta) {
            try {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", "/select,\"" + ruta + "\"") { UseShellExecute = true });
#else
                Application.OpenURL("file://" + Path.GetDirectoryName(ruta));
#endif
            } catch (Exception e) {
                UnityEngine.Debug.LogWarning("No se pudo abrir la carpeta del reporte: " + e.Message);
            }
        }

        /// <summary>Lo que el DebriefReport no trae: quien juega, la partida, el catalogo ya resuelto y el riesgo del ultimo dia.</summary>
        public static DatosDelReporte DatosDe(AppRoot app, DebriefReport r) {
            var d = new DatosDelReporte { Fecha = DateTime.Now };
            var perfil = app.PerfilActivo;
            if (perfil != null && !string.IsNullOrWhiteSpace(perfil.nombreEstudiante)) d.Estudiante = perfil.nombreEstudiante;

            var partida = app.PartidaActiva != null ? app.PartidaActiva.Partida : null;
            if (partida != null) { d.Partida = partida.Nombre; d.ModoAula = partida.ModoAula; }

            var m = r.Metodologia;
            if (m != null && app.Catalogo.Metodologias.ContainsKey(m.MetodologiaId ?? ""))
                d.MotivoDeMetodologia = app.Catalogo.Metodologias[m.MetodologiaId].TextoDe(m.RazonElegida);

            var nivel = app.Catalogo.Niveles.ContainsKey(r.NivelId ?? "") ? app.Catalogo.Niveles[r.NivelId] : null;
            var arquitectura = nivel == null ? null : nivel.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == r.ArquitecturaElegida);
            if (arquitectura != null) d.ArquitecturaNombre = arquitectura.Nombre;

            var brief = app.Sesion != null ? app.Sesion.BriefDeHoy : null;
            var riesgo = brief != null ? brief.Derivadas : null;
            if (riesgo != null) {
                d.RiesgoLatente = riesgo.RiesgoLatente;
                d.RiesgoPorCansancio = riesgo.RiesgoPorCansancio;
                d.RiesgoPorDeuda = riesgo.RiesgoPorDeuda;
                d.RiesgoPorCobertura = riesgo.RiesgoPorCobertura;
                d.RiesgoPorDocumentacion = riesgo.RiesgoPorDocumentacion;
            }
            return d;
        }
    }
}
