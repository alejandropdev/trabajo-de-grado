using System;
using System.Collections.Generic;
using Nexus.Core.Minijuegos;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// El modo guiado de un minijuego: un panel «Marisol te guía» encima del tablero que lleva al jugador paso a
    /// paso (RecorridoGuiado) hasta hacerlo bien, diciendo que hacer y que mirar, y señalando la pieza real.
    ///
    /// La pantalla del verbo pregunta Permite() antes de cada accion del jugador: lo que no toca no se hace, y el
    /// panel explica por que («Todavía no: …»). Cuando la accion es la esperada, la pantalla llama a Hecho() y
    /// el recorrido avanza. Los pasos de solo leer se pasan con «Entendido».
    /// </summary>
    public sealed class GuiaDelMinijuego {
        private readonly AppRoot _app;
        private readonly Func<PasoGuiado, RectTransform> _piezaDe;
        private readonly Action<PasoGuiado> _alCambiar;
        private List<PasoGuiado> _pasos = new List<PasoGuiado>();
        private int _indice;
        private bool _empezado;

        private readonly RectTransform _panel;
        private readonly TMP_Text _progreso, _texto, _porque, _aviso;
        private readonly Button _entendido;
        private readonly ScrollRect _scrollPorque;
        private readonly LayoutElement _altoPorque;
        private const float AltoMaximoDelPorque = 110;

        public PasoGuiado Actual { get { return _indice < _pasos.Count ? _pasos[_indice] : null; } }
        public IReadOnlyList<PasoGuiado> Pasos { get { return _pasos; } }
        public bool Terminado { get { return _indice >= _pasos.Count; } }

        public GuiaDelMinijuego(AppRoot app, Transform padre, int indiceEnPadre,
                                Func<PasoGuiado, RectTransform> piezaDe, Action<PasoGuiado> alCambiar) {
            _app = app;
            _piezaDe = piezaDe;
            _alCambiar = alCambiar;
            var ui = app.Ui;
            var tema = ui.Tema;

            _panel = ui.PanelColumna(padre, "Marisol te guia", tema.margen * 0.75f, 4, tema.fondoSecundario);
            _panel.SetSiblingIndex(indiceEnPadre);
            _panel.gameObject.AddComponent<Outline>().effectColor = tema.mostaza;
            var cabecera = ui.Fila(_panel);
            _progreso = ui.Texto(cabecera, "", EstiloTexto.Pequeno, tema.mostazaClara);
            UiKit.Tamano(_progreso, flexAncho: 1);
            _entendido = ui.Boton(cabecera, "Entendido", Entendido, VarianteBoton.Primario);
            _texto = ui.Texto(_panel, "", EstiloTexto.Subtitulo, tema.texto);
            // El porque puede ser largo: va en su propio scroll de alto acotado, para no aplastar el tablero.
            RectTransform contenido;
            _scrollPorque = ui.Desplazable(_panel, out contenido, "Porque");
            _altoPorque = UiKit.Tamano(_scrollPorque, flexAncho: 1, alto: 0);
            _porque = ui.Texto(contenido, "", EstiloTexto.Cuerpo, tema.cianClaro);
            _aviso = ui.Texto(_panel, "", EstiloTexto.Pequeno, tema.amarillo);
            UiKit.Tamano(_panel, flexAlto: 0);
            _panel.gameObject.SetActive(false);
        }

        public void Empezar(List<PasoGuiado> pasos) {
            _pasos = pasos ?? new List<PasoGuiado>();
            _indice = 0;
            _empezado = _pasos.Count > 0;
            _panel.gameObject.SetActive(true);
            GuiaView.Señalar(_app, () => Actual == null ? null : _piezaDe(Actual));
            Pintar();
        }

        /// <summary>¿Es esto lo que toca ahora? Objetivo null = cualquiera.</summary>
        public bool Permite(AccionGuiada accion, string objetivo = null) {
            if (!_empezado) return true;   // una guia que no llego a arrancar nunca impide jugar
            var p = Actual;
            if (p == null) return accion == AccionGuiada.Entregar;
            return p.Accion == accion && (string.IsNullOrEmpty(p.Objetivo) || objetivo == null || p.Objetivo == objetivo);
        }

        /// <summary>La pantalla hizo lo que tocaba: el recorrido avanza.</summary>
        public void Hecho(AccionGuiada accion, string objetivo = null) {
            if (!Permite(accion, objetivo)) return;
            Avanzar();
        }

        /// <summary>El jugador intento otra cosa: no se hace, y se le dice que toca.</summary>
        public void Rechazar() {
            var p = Actual;
            if (p == null) return;
            _aviso.text = "Todavía no: " + p.Texto + (p.Accion == AccionGuiada.Leer ? " (pulsa «Entendido» arriba)" : "");
        }

        public void Cerrar() {
            GuiaView.Señalar(_app, null);
            _panel.gameObject.SetActive(false);
        }

        private void Entendido() {
            if (Actual != null && Actual.Accion == AccionGuiada.Leer) Avanzar();
        }

        private void Avanzar() {
            _indice++;
            Pintar();
            _alCambiar?.Invoke(Actual);
        }

        private void Pintar() {
            _aviso.text = "";
            var p = Actual;
            if (p == null) {
                _progreso.text = "MARISOL TE GUÍA · LISTO";
                _texto.text = "¡Hecho!";
                PonerPorque("");
                _entendido.gameObject.SetActive(false);
                return;
            }
            _progreso.text = $"MARISOL TE GUÍA · PASO {_indice + 1} DE {_pasos.Count}";
            _texto.text = p.Texto;
            PonerPorque(p.Porque ?? "");
            _entendido.gameObject.SetActive(p.Accion == AccionGuiada.Leer);
        }

        /// <summary>El alto del porque se ajusta a su texto, hasta un maximo; lo que sobre, con scroll.</summary>
        private void PonerPorque(string texto) {
            _porque.text = texto;
            var ancho = _panel.rect.width > 100 ? _panel.rect.width - 40 : 1200;
            var alto = string.IsNullOrEmpty(texto) ? 0 : _porque.GetPreferredValues(texto, ancho, 0).y + 4;
            _altoPorque.minHeight = _altoPorque.preferredHeight = Mathf.Min(AltoMaximoDelPorque, alto);
            _scrollPorque.verticalNormalizedPosition = 1;
        }
    }
}
