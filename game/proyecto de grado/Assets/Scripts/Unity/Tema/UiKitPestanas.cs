using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Pestañas: una franja de botones y, debajo, UNA sola de sus hojas a la vez. Es la forma de que una pantalla
    /// tenga muchas cosas sin enseñarlas todas juntas: antes cada pantalla se las arreglaba con una fila de botones
    /// y un «Resaltar», y lo que no cabia se apilaba en un scroll largo.
    ///
    /// La hoja se vuelve a pintar entera al cambiar de pestaña (como hace el resto del juego al repintar).
    /// </summary>
    public sealed class PestanasView {
        private readonly UiKit _ui;
        private readonly List<Button> _botones = new List<Button>();
        private readonly Action<int, RectTransform> _pintar;

        public RectTransform Raiz { get; private set; }
        public RectTransform Franja { get; private set; }
        public RectTransform Contenido { get; private set; }
        public int Indice { get; private set; }

        /// <summary>Se llama al cambiar de pestaña (para que quien la abrio recuerde en cual se quedo).</summary>
        public Action<int> AlCambiar;

        internal PestanasView(UiKit ui, RectTransform raiz, RectTransform franja, RectTransform contenido, Action<int, RectTransform> pintar) {
            _ui = ui;
            Raiz = raiz;
            Franja = franja;
            Contenido = contenido;
            _pintar = pintar;
        }

        internal void Anadir(Button boton) { _botones.Add(boton); }

        public Button Boton(int indice) { return indice >= 0 && indice < _botones.Count ? _botones[indice] : null; }

        public void Seleccionar(int indice) {
            if (_botones.Count == 0) return;
            Indice = Mathf.Clamp(indice, 0, _botones.Count - 1);
            for (var i = 0; i < _botones.Count; i++) _ui.Resaltar(_botones[i], i == Indice);
            Repintar();
            AlCambiar?.Invoke(Indice);
        }

        /// <summary>Vuelve a pintar la pestaña abierta, sin cambiar de pestaña.</summary>
        public void Repintar() {
            UiKit.Vaciar(Contenido);
            _pintar?.Invoke(Indice, Contenido);
        }
    }

    public partial class UiKit {
        /// <summary>
        /// Una franja de pestañas y su hoja. 'pintar' recibe el indice de la pestaña y la columna donde escribirla.
        /// </summary>
        public PestanasView Pestanas(Transform padre, IList<string> titulos, Action<int, RectTransform> pintar, int inicial = 0) {
            var raiz = Columna(padre, "Pestanas", Tema.espacio);
            var franja = Fila(raiz, "Franja", Tema.Espacio(2));
            Separador(raiz);
            var contenido = Columna(raiz, "Hoja de la pestana", Tema.espacio);
            Tamano(contenido, flexAncho: 1);

            var vista = new PestanasView(this, raiz, franja, contenido, pintar);
            for (var i = 0; i < titulos.Count; i++) {
                var indice = i;
                vista.Anadir(Boton(franja, titulos[i], () => vista.Seleccionar(indice), VarianteBoton.Fantasma));
            }
            vista.Seleccionar(inicial);
            return vista;
        }
    }
}
