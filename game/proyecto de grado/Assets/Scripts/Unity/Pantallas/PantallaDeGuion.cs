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

        private const int PorFila = 3;
        private const float AltoViñeta = 330;

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.espacio, Tema.margen));
            var cabecera = Ui.Fila(marco);
            // Una escena es un momento narrativo: su etiqueta va en violet, el acento reservado a la historia.
            var etiqueta = Ui.Texto(cabecera, "Escena · " + (Guion.Titulo ?? Guion.Id), EstiloTexto.Leyenda, Tema.violet);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Resorte(cabecera);
            _progreso = Ui.Texto(cabecera, "", EstiloTexto.Leyenda);
            Ui.Boton(cabecera, "Saltar", Saltar, VarianteBoton.Fantasma);

            // La escena se cuenta como una tira de comic: viñetas numeradas, tres por fila, que se van destapando.
            _scroll = Ui.Desplazable(marco, out _historial);
            _historial.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = Tema.espacio;
            UiKit.Tamano(_scroll, flexAncho: 1, flexAlto: 1);
            _acciones = Ui.Columna(marco, "Acciones", Tema.espacio);

            _linea = 0;
        }

        public override void Repintar() {
            if (_historial == null || _terminado) return;
            UiKit.Vaciar(_historial);
            RectTransform fila = null;
            for (var i = 0; i <= _linea && i < Lineas.Count; i++) {
                if (i % PorFila == 0) fila = Ui.Fila(_historial, "Tira", Tema.espacio, alineacion: TextAnchor.UpperLeft);
                PintarViñeta(fila, Lineas[i], i, i == _linea);
            }
            // La ultima fila, completada con huecos: si no, sus viñetas se estirarian a todo el ancho.
            var enLaUltimaFila = Mathf.Min(_linea, Lineas.Count - 1) % PorFila + 1;
            for (var k = enLaUltimaFila; fila != null && k < PorFila; k++)
                UiKit.Tamano(Ui.Nodo(fila, "Hueco"), ancho: 0, flexAncho: 1);
            _progreso.text = $"{Mathf.Min(_linea + 1, Lineas.Count)} / {Lineas.Count}";

            UiKit.Vaciar(_acciones);
            if (!EnLaUltima) {
                Ui.Boton(Ui.Fila(_acciones), "Siguiente  ►", Avanzar, VarianteBoton.Primario);
            } else if (TieneOpciones) {
                var opciones = Ui.Fila(_acciones, "Opciones", Tema.espacio, alineacion: TextAnchor.UpperLeft);
                foreach (var opcion in Guion.Opciones) {
                    var o = opcion;
                    UiKit.Tamano(Ui.BotonDeOpcion(opciones, o.Texto, null, () => Elegir(o)), ancho: 0, flexAncho: 1);
                }
            } else {
                Ui.Boton(Ui.Fila(_acciones), "Continuar  ►", Terminar, VarianteBoton.Primario);
            }
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0;   // la ultima viñeta, abajo, siempre a la vista
        }

        /// <summary>
        /// Una viñeta: la ilustracion de ese momento (escenas/&lt;guion&gt;-&lt;n&gt;, o la del personaje que habla, o su silueta
        /// mientras el arte no exista), su numero en la esquina y el texto en una cartela debajo. El narrador no lleva
        /// nombre; un «log» se ve como una terminal dentro de la viñeta.
        /// </summary>
        private void PintarViñeta(Transform fila, LineaDeGuion linea, int indice, bool actual) {
            var hablante = Textos.Hablante(linea.Quien);
            var esLog = linea.Quien == "log";
            var viñeta = Ui.PanelColumna(fila, "Viñeta " + (indice + 1), 0, 0, Tema.bg950);
            UiKit.Tamano(viñeta, ancho: 0, flexAncho: 1);
            UiKit.ColorDeBorde(viñeta, actual ? Tema.violet : Tema.lineStrong);

            var escena = Ui.Nodo(viñeta, "Escena");
            UiKit.Tamano(escena, alto: AltoViñeta, flexAncho: 1);
            if (esLog) {
                var consola = Ui.Registro(escena, new[] { new KeyValuePair<NivelDeLog, string>(NivelDeLog.Info, linea.Texto) });
                UiKit.Rellenar(consola, Tema.Espacio(4));
            } else {
                var id = $"escenas/{Guion.Id}-{indice + 1}";
                if (MaterialesNexus.Ilustracion(id) == null && hablante != null) id = MaterialesNexus.IdDePersonaje(hablante);
                var arte = Ui.Ilustracion(escena, id, hablante == null ? "Escena " + (indice + 1) : hablante, silueta: hablante != null);
                UiKit.Rellenar(arte);
                arte.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            }
            // el numero de la viñeta, en su cuadrito
            var numero = Ui.Nodo(escena, "Numero");
            numero.anchorMin = numero.anchorMax = numero.pivot = new Vector2(0, 1);
            numero.anchoredPosition = new Vector2(Tema.Espacio(2), -Tema.Espacio(2));
            numero.sizeDelta = new Vector2(Tema.Px(26), Tema.Px(26));
            Ui.Fondo(numero.gameObject.AddComponent<UnityEngine.UI.Image>(), Tema.bg950, NexusTheme.RadioXs);
            Ui.Borde(numero, Tema.lineStrong, NexusTheme.RadioXs);
            var n = Ui.Texto(numero, (indice + 1).ToString(), EstiloTexto.Leyenda, Tema.ink, TextAlignmentOptions.Center);
            UiKit.Rellenar((RectTransform)n.transform);

            // la cartela: el texto de la viñeta sobre negro, como en un comic
            var cartela = Ui.PanelColumna(viñeta, "Cartela", Tema.Espacio(4), 4, NexusTheme.Alfa(Tema.surface, 0.96f));
            if (hablante != null && !esLog) Ui.Texto(cartela, hablante, EstiloTexto.Etiqueta, Tema.cyan);
            var texto = Ui.Texto(cartela, esLog ? "" : linea.Texto, EstiloTexto.Dialogo);
            if (hablante == null && !esLog) texto.fontStyle = FontStyles.Italic;
            if (esLog) cartela.gameObject.SetActive(false);
            if (!actual) texto.color = Tema.inkMuted;
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
