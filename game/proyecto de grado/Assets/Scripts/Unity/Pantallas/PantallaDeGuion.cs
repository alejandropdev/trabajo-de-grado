using System;
using System.Collections.Generic;
using Nexus.Core.Narrativa;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Una escena: cinematicas, la entrevista, la guia de cada dia. Las lineas salen una a una, y las anteriores
    /// se quedan arriba, tenues, para poder releer. Si el guion termina en una eleccion, las opciones salen con la
    /// ultima linea y no se puede seguir sin elegir: una eleccion narrativa es una decision, y se registra.
    ///
    /// Tres voces: el narrador (sin nombre, en cursiva), los personajes (con su nombre) y «log» (texto de
    /// terminal, en monoespaciada: el Primer Glitch se lee ahi).
    /// </summary>
    public sealed class PantallaDeGuion : Pantalla {
        public Guion Guion;
        public List<LineaDeGuion> Lineas;
        public Action<OpcionDeGuion> AlElegir;
        public Action AlTerminar;

        private RectTransform _historial;
        private UnityEngine.UI.ScrollRect _scroll;
        private RectTransform _acciones;
        private TMP_Text _progreso;
        private int _linea;
        private bool _terminado;

        public override bool PuedeVolver { get { return false; } }

        private bool TieneOpciones { get { return Guion.Opciones != null && Guion.Opciones.Count > 0; } }
        private bool EnLaUltima { get { return _linea >= Lineas.Count - 1; } }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 1.5f));
            var cabecera = Ui.Fila(marco);
            Ui.Texto(cabecera, "ESCENA · " + (Guion.Titulo ?? Guion.Id).ToUpperInvariant(), EstiloTexto.Pequeno, Tema.cian);
            Ui.Resorte(cabecera);
            _progreso = Ui.Texto(cabecera, "", EstiloTexto.Pequeno);
            Ui.Boton(cabecera, "Saltar", Saltar, VarianteBoton.Fantasma);

            // El texto va en una columna estrecha y centrada: una escena se lee, no se escanea.
            var centro = Ui.Fila(marco, "Centro", alineacion: TextAnchor.UpperCenter);
            UiKit.Tamano(centro, flexAncho: 1, flexAlto: 1);
            Ui.Resorte(centro);
            var columna = Ui.Columna(centro, "Columna", Tema.margen);
            UiKit.Tamano(columna, ancho: 1100, flexAlto: 1);
            _scroll = Ui.Desplazable(columna, out _historial);
            _historial.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = Tema.margen;
            UiKit.Tamano(_scroll, flexAncho: 1, flexAlto: 1);
            _acciones = Ui.Columna(columna, "Acciones", Tema.espacio);
            Ui.Resorte(centro);

            _linea = 0;
        }

        public override void Repintar() {
            if (_historial == null || _terminado) return;
            UiKit.Vaciar(_historial);
            for (var i = 0; i <= _linea && i < Lineas.Count; i++) PintarLinea(Lineas[i], i == _linea);
            _progreso.text = $"{Mathf.Min(_linea + 1, Lineas.Count)} / {Lineas.Count}";

            UiKit.Vaciar(_acciones);
            if (!EnLaUltima) {
                Ui.Boton(Ui.Fila(_acciones), "Siguiente  ►", Avanzar, VarianteBoton.Primario);
            } else if (TieneOpciones) {
                foreach (var opcion in Guion.Opciones) {
                    var o = opcion;
                    Ui.BotonDeOpcion(_acciones, o.Texto, null, () => Elegir(o));
                }
            } else {
                Ui.Boton(Ui.Fila(_acciones), "Continuar  ►", Terminar, VarianteBoton.Primario);
            }
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0;   // la ultima linea, abajo, siempre a la vista
        }

        private void PintarLinea(LineaDeGuion linea, bool actual) {
            var bloque = Ui.Columna(_historial, "Linea", 4);
            var hablante = Textos.Hablante(linea.Quien);
            TMP_Text texto;
            if (linea.Quien == "log") {
                texto = Ui.Texto(bloque, linea.Texto, EstiloTexto.Mono);
            } else {
                if (hablante != null) Ui.Texto(bloque, hablante, EstiloTexto.Pequeno, actual ? Tema.cianClaro : Tema.textoTenue);
                texto = Ui.Texto(bloque, linea.Texto, EstiloTexto.Cuerpo);
                texto.fontSize = Tema.tamCuerpo * 1.15f;
                if (hablante == null) texto.fontStyle = FontStyles.Italic;
            }
            if (!actual) texto.color = Tema.textoTenue;
        }

        private void Update() {
            if (_terminado) return;
            var teclado = Keyboard.current;
            if (teclado == null) return;
            if (teclado.spaceKey.wasPressedThisFrame || teclado.enterKey.wasPressedThisFrame) {
                if (!EnLaUltima) Avanzar();
                else if (!TieneOpciones) Terminar();
            }
        }

        private void Avanzar() {
            if (EnLaUltima) return;
            _linea++;
            Repintar();
        }

        /// <summary>Salta a la ultima linea. Si hay eleccion, hay que hacerla igual.</summary>
        private void Saltar() {
            _linea = Lineas.Count - 1;
            Repintar();
        }

        private void Elegir(OpcionDeGuion opcion) {
            AlElegir?.Invoke(opcion);
            Terminar();
        }

        private void Terminar() {
            if (_terminado) return;
            _terminado = true;
            AlTerminar?.Invoke();
        }
    }
}
