using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La pizarra de la entrevista (2026-11-07), debajo de cada pregunta: una pizarra de verdad (marco de madera, la
    /// repisa con su tiza) donde se dibuja sola, trazo a trazo, la escena de tiza del TEMA de la pregunta (la misma
    /// libreria que los pizarrones del curso, DibujosDeEscena). Cada pregunta es un dibujo distinto: es lo que hace que
    /// las quince no se vean iguales.
    ///
    /// Al responder, la pizarra se atenua y por abajo sube el susurro del hombre del traje verde (HH): su silueta y el
    /// bocadillo, escrito letra a letra por la pantalla, con el boton para seguir.
    /// </summary>
    public sealed class PizarraDeLaPregunta : MonoBehaviour {
        private const float DuracionDelTrazo = 1.6f;

        private UiKit _ui;
        private RectTransform _tabla, _tiza, _susurro;
        private Lamina _lamina;
        private CanvasGroup _grupoDibujo, _grupoSusurro;
        private readonly List<CanvasGroup> _textos = new List<CanvasGroup>();
        private float _t = 99, _tSusurro = 99;
        private bool _conSusurro;

        public static PizarraDeLaPregunta Crear(UiKit ui, Transform padre) {
            var marco = ui.Nodo(padre, "Pizarra");
            UiKit.Tamano(marco, alto: 220, flexAncho: 1, flexAlto: 1);
            var p = marco.gameObject.AddComponent<PizarraDeLaPregunta>();
            p.Construir(ui, marco);
            return p;
        }

        private void Construir(UiKit ui, RectTransform marco) {
            _ui = ui;
            var madera = marco.gameObject.AddComponent<Image>();
            madera.sprite = ui.SpriteRedondeado(); madera.type = Image.Type.Sliced;
            madera.color = new Color32(0x3A, 0x2E, 0x22, 0xFF);
            madera.raycastTarget = false;

            _tabla = ui.Nodo(marco, "Tabla");
            UiKit.Rellenar(_tabla, 12);
            _tabla.offsetMin = new Vector2(12, 26);   // abajo, la repisa
            _tabla.gameObject.AddComponent<Image>().color = Tizador.Pizarra;
            _tabla.gameObject.AddComponent<RectMask2D>();

            // La repisa con su tiza (que se mueve mientras se dibuja).
            _tiza = ui.Nodo(marco, "Tiza");
            _tiza.anchorMin = _tiza.anchorMax = new Vector2(0.82f, 0);
            _tiza.pivot = new Vector2(0.5f, 0.5f);
            _tiza.sizeDelta = new Vector2(38, 9);
            _tiza.anchoredPosition = new Vector2(0, 14);
            var imgTiza = _tiza.gameObject.AddComponent<Image>();
            imgTiza.color = Tizador.Tiza;
            imgTiza.raycastTarget = false;

            var capa = ui.Nodo(_tabla, "Dibujo");
            UiKit.Rellenar(capa);
            _grupoDibujo = capa.gameObject.AddComponent<CanvasGroup>();
            _lamina = new Lamina(ui, capa, DibujosDeEscena.Ancho, DibujosDeEscena.Alto);
            _lamina.Raiz.anchorMin = _lamina.Raiz.anchorMax = _lamina.Raiz.pivot = new Vector2(0.5f, 0.5f);
            _lamina.Raiz.sizeDelta = new Vector2(DibujosDeEscena.Ancho, DibujosDeEscena.Alto);
            _lamina.Dibujo.Tiza = true;
        }

        /// <summary>Borra la pizarra y dibuja la escena 'id' trazo a trazo. Sin escena, el numero de la pregunta.</summary>
        public void Mostrar(string id, int numero) {
            QuitarSusurro(true);
            MarcoDePizarra.Limpiar(_lamina);
            _textos.Clear();
            var t = new Tizador(_lamina, _ui.FuenteTiza);
            if (DibujosDeEscena.Existe(id)) DibujosDeEscena.Dibujar(t, id);
            else t.Texto(DibujosDeEscena.Ancho / 2, DibujosDeEscena.Alto / 2 + 20, numero.ToString(), 120, Tizador.Gris, true);
            // Los textos de tiza aparecen al final del trazo.
            for (var i = 0; i < _lamina.Raiz.childCount; i++) {
                var hijo = _lamina.Raiz.GetChild(i);
                if (hijo.gameObject == _lamina.Dibujo.gameObject) continue;
                var g = hijo.gameObject.AddComponent<CanvasGroup>();
                g.alpha = 0;
                _textos.Add(g);
            }
            _lamina.Dibujo.Avance = 0;
            _t = 0;
        }

        /// <summary>
        /// El susurro de HH sobre la pizarra: la silueta, el bocadillo (devuelve su texto, para escribirlo letra a letra)
        /// y, a su derecha, el boton para seguir.
        /// </summary>
        public TMP_Text Susurro(string texto, string textoDelBoton, System.Action alSeguir) {
            QuitarSusurro(true);
            var tema = _ui.Tema;
            _susurro = _ui.Fila(_tabla, "Susurro", 14, 14, TextAnchor.MiddleLeft);
            _susurro.anchorMin = new Vector2(0, 0);
            _susurro.anchorMax = new Vector2(1, 0);
            _susurro.pivot = new Vector2(0.5f, 0);
            _susurro.sizeDelta = new Vector2(-24, 120);
            _susurro.anchoredPosition = new Vector2(0, 12);
            var fondo = _susurro.gameObject.AddComponent<Image>();
            _ui.Fondo(fondo, NexusTheme.Alfa(tema.bg950, 0.92f), NexusTheme.RadioLg);
            _ui.Borde(_susurro, NexusTheme.Alfa(tema.violet, 0.6f), NexusTheme.RadioLg, 1);
            _grupoSusurro = _susurro.gameObject.AddComponent<CanvasGroup>();
            _grupoSusurro.alpha = 0;

            // La silueta de HH: cabeza y hombros, traje verde.
            var silueta = _ui.Nodo(_susurro, "HH");
            UiKit.Tamano(silueta, 70, 80);
            silueta.gameObject.AddComponent<CanvasRenderer>();
            var d = silueta.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            var verde = new Color32(0x2F, 0x5D, 0x46, 0xFF);
            d.Circulo(35, 24, 15, Color.clear, 0, new Color32(0x3B, 0x47, 0x52, 0xFF));
            d.Poligono(new List<Vector2> { new Vector2(10, 80), new Vector2(18, 48), new Vector2(52, 48), new Vector2(60, 80) }, verde);
            d.Poligono(new List<Vector2> { new Vector2(30, 48), new Vector2(40, 48), new Vector2(35, 66) }, new Color32(0xEE, 0xF3, 0xF7, 0xFF));

            var textos = _ui.Columna(_susurro, "Textos", 4);
            UiKit.Tamano(textos, flexAncho: 1);
            _ui.Texto(textos, "El hombre del traje verde se inclina hacia Marisol:", EstiloTexto.Pequeno, tema.inkMuted);
            var linea = _ui.Texto(textos, "«" + texto + "»", EstiloTexto.Cuerpo, tema.violet);
            linea.fontStyle = FontStyles.Italic;
            var boton = _ui.Boton(_susurro, textoDelBoton, alSeguir, VarianteBoton.Primario);
            UiKit.Tamano(boton, ancho: 300);

            _conSusurro = true;
            _tSusurro = 0;
            return linea;
        }

        private void QuitarSusurro(bool deGolpe) {
            _conSusurro = false;
            if (_susurro != null) Destroy(_susurro.gameObject);
            _susurro = null;
            if (deGolpe && _grupoDibujo != null) _grupoDibujo.alpha = 1;
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;

            // La lamina (600×300) se escala para caber en la tabla, sin deformarse.
            var r = _tabla.rect;
            if (r.width > 1 && r.height > 1) {
                var k = Mathf.Min((r.width - 24) / DibujosDeEscena.Ancho, (r.height - 16) / DibujosDeEscena.Alto);
                _lamina.Raiz.localScale = new Vector3(k, k, 1);
            }

            // El trazo: de 0 a 1 en ~1,6 s, ease-out; los textos, al final. La tiza se mueve mientras dibuja.
            if (_t < DuracionDelTrazo + 0.4f) {
                _t += dt;
                var p = Mathf.Clamp01(_t / DuracionDelTrazo);
                _lamina.Dibujo.Avance = 1 - Mathf.Pow(1 - p, 3);
                var alfaTextos = Mathf.Clamp01((_t - DuracionDelTrazo * 0.7f) / 0.35f);
                foreach (var g in _textos) if (g != null) g.alpha = alfaTextos;
                var dibujando = p < 1;
                _tiza.localRotation = Quaternion.Euler(0, 0, dibujando ? Mathf.Sin(_t * 22f) * 8f : 0);
                _tiza.anchoredPosition = new Vector2(dibujando ? Mathf.Sin(_t * 9f) * 6f : 0, 14 + (dibujando ? 3 : 0));
            }

            // El susurro sube y se ve (220 ms, ease-out); la pizarra se atenua detras para que se lea.
            if (_conSusurro && _grupoSusurro != null) {
                _tSusurro += dt;
                var s = 1 - Mathf.Pow(1 - Mathf.Clamp01(_tSusurro / 0.22f), 3);
                _grupoSusurro.alpha = s;
                _susurro.anchoredPosition = new Vector2(0, Mathf.Lerp(-20, 12, s));
                _grupoDibujo.alpha = Mathf.Lerp(1, 0.4f, s);
            }
        }
    }
}
