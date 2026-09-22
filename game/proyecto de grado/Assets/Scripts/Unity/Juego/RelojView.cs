using System.Linq;
using Nexus.Core.Jornada;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Juego {
    /// <summary>
    /// El reloj del dia continuo, siempre visible durante la Fase 2: dia y unidad (Sprint 1, Etapa: Diseño…),
    /// la hora, cuanto queda de jornada y cuantas alertas esperan. Se crea con RelojView.Crear y lee el
    /// estado cada fotograma: no guarda nada propio, asi que nunca se desincroniza de la sesion.
    ///
    /// La hora se pone en mostaza la ultima hora antes del cierre: es una de las pocas cosas que tienen
    /// derecho a gastar el 12 % de mostaza del encuadre.
    /// </summary>
    public sealed class RelojView : MonoBehaviour {
        private LevelRunner _runner;
        private NexusTheme _tema;
        private TMP_Text _dia;
        private TMP_Text _hora;
        private TMP_Text _estado;
        private TMP_Text _alertas;
        private BarraView _jornada;

        public static RelojView Crear(UiKit ui, Transform padre, LevelRunner runner) {
            var panel = ui.PanelColumna(padre, "Reloj");
            var columna = panel;

            var vista = panel.gameObject.AddComponent<RelojView>();
            vista._runner = runner;
            vista._tema = ui.Tema;
            vista._dia = ui.Texto(columna, "", EstiloTexto.Pequeno);
            vista._hora = ui.Texto(columna, "--:--", EstiloTexto.Titulo);
            vista._hora.fontSize = ui.Tema.tamTitulo * 1.3f;
            vista._jornada = ui.Barra(columna, 0);
            vista._estado = ui.Texto(columna, "", EstiloTexto.Pequeno);
            vista._alertas = ui.Texto(columna, "", EstiloTexto.Cuerpo);

            UiKit.Tamano(panel, ancho: 320);
            return vista;
        }

        private void Update() {
            var sesion = _runner == null ? null : _runner.Sesion;
            if (sesion == null) {
                _hora.text = "--:--";
                _dia.text = _estado.text = _alertas.text = "";
                return;
            }

            var brief = sesion.BriefDeHoy;
            _dia.text = sesion.R.DiaActual == 0
                ? "Antes del día 1"
                : $"DÍA {sesion.R.DiaActual} / {sesion.Perfil.DiasTotales}" + (brief == null ? "" : "  ·  " + brief.EtiquetaUnidad);

            _hora.text = sesion.HoraActual;

            var jornada = sesion.Perfil.Jornada;
            var inicio = jornada.HoraInicio * 60;
            var fin = (sesion.JornadaProrrogada ? jornada.HoraLimite : jornada.HoraCierre) * 60;
            _jornada.Valor = fin > inicio ? (sesion.MinutoDelDia - inicio) / (float)(fin - inicio) : 0;

            var ultimaHora = !sesion.JornadaProrrogada && jornada.HoraCierre * 60 - sesion.MinutoDelDia <= 60;
            _hora.color = ultimaHora ? _tema.mostaza : _tema.texto;
            _jornada.Color = sesion.JornadaProrrogada ? _tema.naranja : _tema.cian;

            _estado.text = Describir(_runner);

            var pendientes = sesion.AlertasDeHoy
                .Where(a => a.EstaPendiente && a.YaSono(sesion.MinutoDelDia))
                .OrderBy(a => a.MinutoDeExpiracion)
                .ToList();
            if (pendientes.Count == 0) {
                _alertas.text = "";
            } else {
                var urgente = pendientes[0];
                _alertas.text = $"<color=#{ColorUtility.ToHtmlStringRGB(_tema.mostaza)}>• {pendientes.Count} esperando</color>" +
                                $"  ·  la primera caduca a las {RelojDeJornada.Formatear(urgente.MinutoDeExpiracion)}";
            }
        }

        private static string Describir(LevelRunner runner) {
            switch (runner.Estado) {
                case EstadoDelDia.Corriendo:
                    if (runner.Pausado) return "EN PAUSA";
                    return runner.EnEscena ? "El reloj espera mientras decides" : "La jornada corre";
                case EstadoDelDia.EnElCierre: return "Cierre de la jornada: ¿te vas o te quedas?";
                case EstadoDelDia.Prorroga: return "Horas extra. Ya no llegan avisos.";
                case EstadoDelDia.DiaTerminado: return "Jornada terminada";
                case EstadoDelDia.DesarrolloTerminado: return "Se acabaron los días. Toca lanzar.";
                default: return "";
            }
        }
    }
}
