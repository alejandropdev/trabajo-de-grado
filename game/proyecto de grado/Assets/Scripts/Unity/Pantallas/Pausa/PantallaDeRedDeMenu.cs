using System;
using System.Collections.Generic;
using Nexus.Core.Tarjetas;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Un menu como RED DE NODOS (2026-11-08), con el lenguaje de la pausa: en el centro, un nucleo con el titulo; alrededor,
    /// un nodo por opcion unido a el por una linea (NodoDePausa, con su circulito al pasar el raton). Lo usa el «Más» del
    /// menu de un perfil. Elegir una opcion cierra la red y SOLO ENTONCES hace lo pedido (si abre otra pantalla, tiene que
    /// ir despues de volver, o se cerraria la equivocada). Escape y «Cerrar» vuelven.
    /// </summary>
    public sealed class PantallaDeRedDeMenu : Pantalla {
        public string Titulo = "Más";
        public List<OpcionDeMenu> Opciones = new List<OpcionDeMenu>();

        public override bool EsModal { get { return true; } }

        private static readonly Vector2 TamanoDeLaRed = new Vector2(1400f, 800f);
        private const float CentroY = 30f, RadioDelNucleo = 130f, RadioX = 520f, RadioY = 230f;
        private const float Entrada = 0.4f, Escalon = 0.06f, AlfaDelVelo = 0.95f, SubidaDelNodoDeArriba = 50f;

        private readonly List<NodoDePausa> _nodos = new List<NodoDePausa>();
        private Image _velo;
        private CanvasGroup _grupoRed, _grupoPie;
        private float _t;
        private bool _lista, _cerrando;
        private int _foco = -1;

        protected override void Construir() {
            _velo = Raiz.gameObject.AddComponent<Image>();
            _velo.color = NexusTheme.Alfa(Tema.bg950, 0f);

            var red = Ui.Nodo(Raiz, "Red");
            Centrar(red, TamanoDeLaRed, Vector2.zero);
            _grupoRed = red.gameObject.AddComponent<CanvasGroup>();
            _grupoRed.alpha = 0f; _grupoRed.blocksRaycasts = false;

            // el nucleo: un disco con el titulo, con su halo
            var halo = Ui.Nodo(red, "Halo del nucleo");
            Centrar(halo, new Vector2(360f, 360f), new Vector2(0f, CentroY));
            var hi = halo.gameObject.AddComponent<Image>();
            hi.sprite = Ui.SpriteCircular(); hi.color = NexusTheme.Alfa(Tema.cyan, 0.15f); hi.raycastTarget = false;
            var nucleo = Ui.Nodo(red, "Nucleo");
            Centrar(nucleo, new Vector2(RadioDelNucleo * 2f, RadioDelNucleo * 2f), new Vector2(0f, CentroY));
            var disco = nucleo.gameObject.AddComponent<Image>();
            disco.sprite = Ui.SpriteCircular(); disco.color = NexusTheme.Alfa(Tema.surface, 0.96f);
            Ui.Borde(nucleo, Tema.cyan, RadioDelNucleo / Tema.escala, 2.5f);
            var titulo = Ui.Texto(nucleo, Titulo.ToUpperInvariant(), EstiloTexto.Hero, Tema.ink, TextAlignmentOptions.Center);
            titulo.textWrappingMode = TextWrappingModes.NoWrap;
            Centrar(titulo.rectTransform, new Vector2(RadioDelNucleo * 1.7f, 80f), Vector2.zero);

            // los nodos
            var visibles = Opciones.FindAll(o => o != null);
            var puntos = DisposicionRadial.Calcular(visibles.Count, RadioX, RadioY);
            var centro = new Vector2(0f, CentroY);
            for (var i = 0; i < visibles.Count; i++) {
                var opcion = visibles[i];
                var destino = new Vector2(puntos[i].X, puntos[i].Y + CentroY);
                if (Mathf.Abs(puntos[i].X) < 1f && puntos[i].Y > 0f) destino.y += SubidaDelNodoDeArriba;
                var nodo = NodoDePausa.Crear(Ui, red, TipoDe(opcion.Texto), Rotulo(opcion.Texto), i, centro, destino, centro, RadioDelNucleo,
                                             TamanoDeLaRed, () => Elegir(opcion));
                nodo.AlEntrar = n => { if (!_cerrando) { _foco = n; Enfocar(); } };
                nodo.AlSalir = n => { if (!_cerrando && _foco == n) { _foco = -1; Enfocar(); } };
                _nodos.Add(nodo);
            }

            // «Cerrar»: abajo en el centro
            var pie = Ui.Nodo(Raiz, "Pie");
            UiKit.Rellenar(pie);
            _grupoPie = pie.gameObject.AddComponent<CanvasGroup>();
            _grupoPie.alpha = 0f; _grupoPie.blocksRaycasts = false;
            var cerrar = Ui.Boton(pie, "Cerrar", Cerrar, VarianteBoton.Secundario);
            Centrar((RectTransform)cerrar.transform, new Vector2(300f, 72f), new Vector2(0f, -400f));
        }

        private void Elegir(OpcionDeMenu opcion) {
            if (_cerrando || !_lista) return;
            _cerrando = true;
            App.Router.Volver();
            if (opcion.AlPulsar != null) opcion.AlPulsar();
        }

        private void Cerrar() {
            if (_cerrando) return;
            _cerrando = true;
            App.Router.Volver();
        }

        private void Enfocar() { for (var i = 0; i < _nodos.Count; i++) _nodos[i].Enfocar(i == _foco); }

        private void Update() {
            if (_lista) return;
            var total = Entrada + Escalon * Mathf.Max(0, _nodos.Count - 1);
            _t = Mathf.Min(total, _t + Time.unscaledDeltaTime);
            for (var i = 0; i < _nodos.Count; i++) _nodos[i].Entrar((_t - i * Escalon) / Entrada);
            _velo.color = NexusTheme.Alfa(Tema.bg950, AlfaDelVelo * Mathf.Clamp01(_t / 0.2f));
            _grupoRed.alpha = Mathf.Clamp01(_t / 0.15f);
            _grupoPie.alpha = Mathf.Clamp01((_t - 0.25f) / 0.2f);
            if (_t < total) return;
            _lista = true;
            _velo.color = NexusTheme.Alfa(Tema.bg950, AlfaDelVelo);
            _grupoRed.alpha = _grupoPie.alpha = 1f;
            _grupoRed.blocksRaycasts = _grupoPie.blocksRaycasts = true;
        }

        private static string Rotulo(string texto) { return string.IsNullOrEmpty(texto) ? "" : texto.TrimEnd('…', '.', ' '); }

        private static PictogramasDePausa.Tipo TipoDe(string texto) {
            var t = (texto ?? "").ToLowerInvariant();
            if (t.Contains("opciones")) return PictogramasDePausa.Tipo.Opciones;
            if (t.Contains("diario")) return PictogramasDePausa.Tipo.Diario;
            if (t.Contains("borrar")) return PictogramasDePausa.Tipo.Borrar;
            if (t.Contains("receta")) return PictogramasDePausa.Tipo.Apuntes;
            return PictogramasDePausa.Tipo.Ayuda;
        }

        private static void Centrar(RectTransform rt, Vector2 tamano, Vector2 posicion) {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tamano;
            rt.anchoredPosition = posicion;
        }
    }
}
