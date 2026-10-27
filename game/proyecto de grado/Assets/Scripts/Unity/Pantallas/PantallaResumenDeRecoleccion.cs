using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Fase1;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Al volver del recorrido 3D: todo lo que se trajo, por lo que significa para el proyecto. Primero quien forma
    /// el equipo (o que vas solo), despues los suministros para la Fase 2, lo que fue al diario y lo demas.
    /// </summary>
    public sealed class PantallaResumenDeRecoleccion : PantallaModal {
        /// <summary>El recorrido acabo porque se agoto el tiempo (y no porque el jugador lo dejara antes).</summary>
        public bool SeAcaboElTiempo = true;

        protected override float Ancho { get { return 860; } }

        protected override void Rellenar() {
            var s = App.Sesion;
            Hoja.Etiqueta("FASE 1 · FIN DEL RECORRIDO");
            Hoja.Titulo(SeAcaboElTiempo ? "Se acabó el tiempo" : "Dejaste el recorrido");
            Hoja.Nota($"Usaste {s.R.MinutosDeRecoleccion} de {s.Recoleccion.MinutosDisponibles} minutos. Esto es lo que trajiste:", Tema.inkMuted);

            var objetos = s.ObjetosRecogidos();
            // El equipo, siempre lo primero: es lo que mas cambia lo que viene.
            {
                var miembros = s.EquipoDelProyecto();
                var equipo = Hoja.Tarjeta($"Tu equipo para el desarrollo · {miembros.Count}", Tono.Cyan);
                if (s.HayQueElegirEquipo)
                    Ui.Texto(equipo, $"Recogiste {s.HojasRecogidas().Count} hojas de vida, pero el proyecto da para {s.MaxEquipo}. " +
                                     "De momento entran las primeras: elige quién entra en el paso «Recorrido».", EstiloTexto.Pequeno, Tema.warning);
                if (!s.EquipoDesdeRecoleccion) {
                    foreach (var m in miembros) Ui.FilaDeEquipo(equipo, m.Nombre, m.Rol, EstadoDeEquipo.Activo);
                    Ui.Texto(equipo, "En este proyecto el equipo ya estaba asignado: las hojas de vida no lo cambian.", EstiloTexto.Pequeno, Tema.inkMuted);
                } else if (s.TrabajasSolo) {
                    Ui.Texto(equipo, "No encontraste ninguna hoja de vida: vas a sacar el proyecto tú solo, y más despacio.",
                             EstiloTexto.Cuerpo, Tema.warning);
                } else {
                    foreach (var m in miembros) {
                        Ui.FilaDeEquipo(equipo, m.Nombre, m.Rol, EstadoDeEquipo.Activo);
                        var cv = objetos.FirstOrDefault(kv => kv.Key.Miembro != null && kv.Key.Miembro.Id == m.Id).Key;
                        if (cv != null && !string.IsNullOrEmpty(cv.Texto)) Ui.Texto(equipo, cv.Texto, EstiloTexto.Pequeno, Tema.inkMuted);
                    }
                }
            }

            Seccion(objetos, TiposDeObjeto3D.Suministro, "Suministros para el desarrollo",
                    "Los usas en la Fase 2 desde «Suministros», en el riel del día.");
            Seccion(objetos, TiposDeObjeto3D.Efecto, "Lo que ya cambia el proyecto", null);
            Seccion(objetos, TiposDeObjeto3D.Coleccionable, "Para tu diario de campo", null);
            Seccion(objetos, TiposDeObjeto3D.Informativo, "Notas y otros hallazgos", null);

            var hallazgos = s.HallazgosRecogidos();
            if (hallazgos.Count > 0) {
                var t = Hoja.Tarjeta("En el plano");
                foreach (var h in hallazgos) Ui.Texto(t, "· " + (h.Texto ?? h.Nombre ?? h.Id), EstiloTexto.Pequeno, Tema.ink);
            }
            if (objetos.Count == 0 && hallazgos.Count == 0)
                Hoja.Parrafo("Nada. Volviste con las manos vacías.", Tema.warning);

            Hoja.Espacio();
            Hoja.Accion("Seguir con la planificación", () => App.Router.Volver());
        }

        private void Seccion(List<KeyValuePair<Objeto3D, int>> objetos, string tipo, string titulo, string pie) {
            var deEste = objetos.Where(kv => kv.Key.Tipo == tipo).ToList();
            if (deEste.Count == 0) return;
            var t = Hoja.Tarjeta(titulo);
            foreach (var kv in deEste) {
                var o = kv.Key;
                Ui.Texto(t, $"<b>{o.Nombre}</b>{(kv.Value > 1 ? "  ×" + kv.Value : "")}", EstiloTexto.Cuerpo, Tema.ink);
                if (!string.IsNullOrEmpty(o.Texto)) Ui.Texto(t, o.Texto, EstiloTexto.Pequeno, Tema.inkMuted);
            }
            if (!string.IsNullOrEmpty(pie)) Ui.Texto(t, pie, EstiloTexto.Leyenda, Tema.cyan);
        }
    }
}
