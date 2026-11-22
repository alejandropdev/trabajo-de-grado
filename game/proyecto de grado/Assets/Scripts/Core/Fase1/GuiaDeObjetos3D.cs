using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Fase1 {
    /// <summary>
    /// Un tipo de cosa que se puede recoger en el recorrido 3D, para la lista de «Objetos» (Pausa y pantalla previa).
    /// </summary>
    public sealed class GrupoDeObjeto3D {
        public string Id, Nombre, ParaQue, Glifo;
        /// <summary>Cuantas piezas distintas hay de este tipo.</summary>
        public int Cantidad;
        /// <summary>Prefijo de los ids del Diario que cuentan como «encontradas» (null = no van al Diario).</summary>
        public string PrefijoDelDiario;
        /// <summary>true = no se cuenta que es hasta encontrar la primera pieza (aparece como «?»).</summary>
        public bool Secreto;
        /// <summary>Texto de apoyo cuando se muestra como «?».</summary>
        public string PistaSecreta;
    }

    /// <summary>
    /// La lista de lo que se puede recoger en el 3D. Es texto de ayuda para el jugador: dice QUE es cada cosa y PARA QUE
    /// sirve, y nunca DONDE aparece (decision 2026-11-20: la exploracion es del jugador). Los ids del Diario son los de
    /// objetos-3d.json (COL-DEV-10..16, COL-NOTA-01..06, COL-USB-02).
    /// </summary>
    public static class GuiaDeObjetos3D {
        public static readonly IReadOnlyList<GrupoDeObjeto3D> Grupos = new List<GrupoDeObjeto3D> {
            new GrupoDeObjeto3D { Id = "cv", Nombre = "Hojas de vida", Glifo = "CV", Cantidad = 7,
                ParaQue = "Personas para tu equipo." },
            new GrupoDeObjeto3D { Id = "pizza", Nombre = "Pizza", Glifo = "P", Cantidad = 1,
                ParaQue = "Sube la moral del equipo." },
            new GrupoDeObjeto3D { Id = "cafe", Nombre = "Café", Glifo = "C", Cantidad = 1,
                ParaQue = "Todo el equipo rinde más ese día." },
            new GrupoDeObjeto3D { Id = "guepardex", Nombre = "Guepardex", Glifo = "G", Cantidad = 1,
                ParaQue = "Quita el cansancio de una persona." },
            new GrupoDeObjeto3D { Id = "certificado", Nombre = "Certificado", Glifo = "CERT", Cantidad = 1,
                ParaQue = "Mejora al cliente y las pruebas." },
            new GrupoDeObjeto3D { Id = "acceso", Nombre = "Tarjetas de acceso", Glifo = "ID", Cantidad = 4,
                ParaQue = "Abren puertas (tecla F)." },
            new GrupoDeObjeto3D { Id = "cartas", Nombre = "Cartas de desarrollador", Glifo = "CARTA", Cantidad = 7, PrefijoDelDiario = "COL-DEV-",
                ParaQue = "Buenas prácticas, para tu Diario." },
            new GrupoDeObjeto3D { Id = "notas", Nombre = "Notas post-it", Glifo = "NOTA", Cantidad = 6, PrefijoDelDiario = "COL-NOTA-",
                ParaQue = "Notas del equipo, para tu Diario." },
            new GrupoDeObjeto3D { Id = "usb", Nombre = "Fragmento de USB", Glifo = "USB", Cantidad = 1, PrefijoDelDiario = "COL-USB-",
                Secreto = true, PistaSecreta = "Algo más por descubrir." },
        };

        /// <summary>Cuantas piezas del grupo hay ya en el Diario del jugador (0 si el grupo no va al Diario).</summary>
        public static int Encontradas(GrupoDeObjeto3D grupo, IEnumerable<string> coleccionablesGlobales) {
            if (grupo == null || grupo.PrefijoDelDiario == null || coleccionablesGlobales == null) return 0;
            return coleccionablesGlobales.Where(id => id != null && id.StartsWith(grupo.PrefijoDelDiario)).Distinct().Count();
        }

        /// <summary>Si el grupo se enseña como «?»: es secreto y todavia no hay ninguna pieza en el Diario.</summary>
        public static bool EstaOculto(GrupoDeObjeto3D grupo, IEnumerable<string> coleccionablesGlobales) {
            return grupo != null && grupo.Secreto && Encontradas(grupo, coleccionablesGlobales) == 0;
        }
    }
}
