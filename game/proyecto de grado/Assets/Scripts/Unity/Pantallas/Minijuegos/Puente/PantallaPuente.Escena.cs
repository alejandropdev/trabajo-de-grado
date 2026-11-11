using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Puente;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    public sealed partial class PantallaPuente {
        // ==================================================================== la escena: el cañon, el puente y los pilares

        private sealed class CorazonVista {
            public RectTransform Rt;
            public DibujoUI Izq, Der;
            public bool Perdido;
            public float Fase;
        }

        private sealed class PilarVista {
            public int Indice;
            public PilarDef Def;
            public Color Color;
            public float Cx;
            public RectTransform Losa, Sello, Cartel;
            public Image LosaFondo, LosaBorde, LosaLuz, Lampara;
            public TMP_Text LosaTexto;
            public DibujoUI GrietasDeLaLosa;
            public readonly Dictionary<string, BloqueDeHistoria> Bloques = new Dictionary<string, BloqueDeHistoria>();
            /// <summary>El orden que se ve (de abajo arriba). Durante un arrastre va por delante del de Core.</summary>
            public List<string> Visual = new List<string>();
            public readonly List<CorazonVista> Corazones = new List<CorazonVista>();
            public bool Listo, Caido;
        }

        private readonly List<PilarVista> _pilares = new List<PilarVista>();
        private Lamina _lam;
        private RectTransform _visor, _camion;
        private DibujoUI _resalte;
        private Image _vineta, _onda;
        private float _w, _h, _baseY, _bh, _bw, _deckTop, _pillarTop, _cliffW, _slabW;
        private const float Hueco = 8f, AltoLosa = 54f, AltoCelosia = 22f;

        private Color ColorDelPilar(int i) {
            switch (i % 3) { case 0: return Tema.cyan; case 1: return Tema.violet; default: return Tema.warning; }
        }

        private float YDeSlot(int slot) { return _baseY - (slot + 1) * _bh - slot * Hueco; }

        private DibujoUI CapaDib(RectTransform padre, string nombre) {
            var rt = UiKit.Rellenar(Ui.Nodo(padre, nombre));
            rt.gameObject.AddComponent<CanvasRenderer>();
            var d = rt.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            return d;
        }

        /// <summary>
        /// El visor llena todo el cuerpo; la escena se dibuja cuando HuecoMedido sabe lo que mide, a escala 1:1. Encima,
        /// la capa donde flotan los globos. 'cierre': la escena ya resuelta (sin entrada animada).
        /// </summary>
        private void ConstruirEscena(RectTransform area, bool cierre, Action listo) {
            var fondo = area.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.55f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            Ui.Borde(area, NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);

            _visor = Ui.Nodo(area, "Visor");
            UiKit.Rellenar(_visor);
            _visor.gameObject.AddComponent<RectMask2D>();
            _capa = Ui.Nodo(area, "Globos");
            UiKit.Rellenar(_capa);

            var visor = _visor;
            var medido = visor.gameObject.AddComponent<HuecoMedido>();
            medido.AlMedirse = (w, h) => {
                if (_lam != null && _lam.Raiz != null) Destroy(_lam.Raiz.gameObject);
                _pilares.Clear();
                _lam = new Lamina(Ui, visor, w, h);
                var r = _lam.Raiz;
                r.GetComponent<LayoutElement>().ignoreLayout = true;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(w, h);
                r.gameObject.AddComponent<ListaDeElementos>();   // sus bloques son contenido, no botones de la pantalla
                PintarEscena(w, h);
                listo?.Invoke();
            };
        }

        private void PintarEscena(float w, float h) {
            _w = w; _h = h;
            var raiz = _lam.Raiz;
            var P = Def.Puente.Pilares.Count;
            _deckTop = Mathf.Clamp(h * 0.28f, 236f, 272f);
            _cliffW = Mathf.Clamp(w * 0.085f, 110f, 190f);
            _slabW = (w - 2 * _cliffW) / P;
            _pillarTop = _deckTop + AltoLosa + AltoCelosia + 4;
            _baseY = h - 128f;
            var maxN = Def.Puente.Pilares.Max(p => p.Historias.Count);
            _bh = Mathf.Clamp((_baseY - _pillarTop - 16f - (maxN - 1) * Hueco) / maxN, 58f, 104f);
            _bw = Mathf.Clamp(_slabW * 0.66f, 210f, 360f);

            var fondo = CapaDib(raiz, "Fondo");
            PintarElFondo(fondo, w, h);
            MotasFlotantes.Crear(Ui, raiz, w, h, 38, NexusTheme.Alfa(Tema.cyan, 0.9f));
            var puente = CapaDib(raiz, "Estructura");
            PintarLaEstructura(puente, w, h);
            _resalte = CapaDib(raiz, "Resalte");

            for (var i = 0; i < P; i++) _pilares.Add(CrearPilar(i));
            CrearElCamion();

            // La luz roja de emergencia (se pulsa al caer un pilar) y la onda de luz del puente completo.
            var onda = Ui.Nodo(raiz, "Onda");
            onda.anchorMin = onda.anchorMax = new Vector2(0, 1); onda.pivot = new Vector2(0.5f, 0.5f);
            onda.sizeDelta = new Vector2(260, 120);
            onda.anchoredPosition = new Vector2(-300, -(_deckTop + 20));
            _onda = onda.gameObject.AddComponent<Image>();
            _onda.sprite = PlanoIlustrado.Brillo();
            _onda.color = Color.clear;
            _onda.raycastTarget = false;
            var vineta = UiKit.Rellenar(Ui.Nodo(raiz, "Vineta"));
            _vineta = vineta.gameObject.AddComponent<Image>();
            _vineta.color = new Color(1f, 0.1f, 0.08f, 0f);
            _vineta.raycastTarget = false;
        }

        // ---------------------------------------------------------------- el fondo lejano y el cañon

        private void PintarElFondo(DibujoUI d, float w, float h) {
            var yRoad = _deckTop + AltoLosa;
            // El cielo, en bandas que van del azul oscuro a un verde de niebla (poca saturacion).
            const int bandas = 7;
            for (var i = 0; i < bandas; i++) {
                var f = i / (float)(bandas - 1);
                var c = Color.Lerp(new Color(0.035f, 0.07f, 0.1f), new Color(0.07f, 0.13f, 0.16f), f);
                var y0 = yRoad * i / bandas; var y1 = yRoad * (i + 1) / bandas + 1f;
                d.Poligono(new List<Vector2> { new Vector2(0, y0), new Vector2(w, y0), new Vector2(w, y1), new Vector2(0, y1) }, c);
            }
            // El cañon: oscuridad creciente hacia el fondo.
            for (var i = 0; i < bandas; i++) {
                var f = i / (float)(bandas - 1);
                var c = Color.Lerp(new Color(0.06f, 0.11f, 0.14f), new Color(0.015f, 0.03f, 0.045f), f);
                var y0 = yRoad + (h - yRoad) * i / bandas; var y1 = yRoad + (h - yRoad) * (i + 1) / bandas + 1f;
                d.Poligono(new List<Vector2> { new Vector2(0, y0), new Vector2(w, y0), new Vector2(w, y1), new Vector2(0, y1) }, c);
            }
            // La ciudad al otro lado: dos capas de edificios y pantallas holograficas tenues.
            DibujosDelPuente.Edificios(d, _cliffW + 10, w - _cliffW - 10, yRoad - 4, 130f, 11, new Color(0.045f, 0.085f, 0.115f), NexusTheme.Alfa(Tema.cyan, 0.32f));
            DibujosDelPuente.Edificios(d, _cliffW - 40, w - _cliffW + 40, yRoad - 4, 84f, 29, new Color(0.06f, 0.11f, 0.145f), NexusTheme.Alfa(Tema.cyan, 0.22f));
            DibujosDelPuente.PantallaHolografica(d, w * 0.40f, _deckTop - 196, 130, 84, Tema.cyan, 3);
            DibujosDelPuente.PantallaHolografica(d, w * 0.60f, _deckTop - 214, 160, 96, Tema.violet, 8);
            DibujosDelPuente.PantallaHolografica(d, w * 0.78f, _deckTop - 176, 116, 74, Tema.cyan, 13);
            // La niebla del fondo del cañon.
            d.Elipse(w * 0.3f, h - 40, w * 0.28f, 46, new Color(0, 0, 0, 0), 0, NexusTheme.Alfa(Tema.cyan, 0.035f));
            d.Elipse(w * 0.7f, h - 70, w * 0.3f, 54, new Color(0, 0, 0, 0), 0, NexusTheme.Alfa(Tema.cyan, 0.03f));
            // Los acantilados.
            var roca = new Color(0.1f, 0.15f, 0.19f);
            DibujosDelPuente.Risco(d, true, _cliffW, yRoad, h - yRoad, w, roca, Tema.cyan);
            DibujosDelPuente.Risco(d, false, _cliffW, yRoad, h - yRoad, w, roca, Tema.cyan);
            // La carretera: asfalto de lado a lado, con su borde y la linea discontinua.
            d.Poligono(new List<Vector2> { new Vector2(0, _deckTop), new Vector2(w, _deckTop), new Vector2(w, yRoad), new Vector2(0, yRoad) }, new Color(0.07f, 0.09f, 0.11f));
            d.Linea(0, _deckTop, w, _deckTop, NexusTheme.Alfa(Tema.cyan, 0.3f), 2.5f);
            d.Linea(0, yRoad, w, yRoad, NexusTheme.Alfa(Color.black, 0.6f), 3f);
            d.Linea(0, _deckTop + AltoLosa / 2f, _cliffW, _deckTop + AltoLosa / 2f, NexusTheme.Alfa(Color.white, 0.4f), 3f, true);
            d.Linea(w - _cliffW, _deckTop + AltoLosa / 2f, w, _deckTop + AltoLosa / 2f, NexusTheme.Alfa(Color.white, 0.4f), 3f, true);
            // La guia de lectura: de abajo (primero) hacia arriba (ultimo), sobre el acantilado izquierdo.
            var gx = _cliffW * 0.5f;
            d.Flecha(gx, _baseY, gx, _pillarTop + 40, NexusTheme.Alfa(Tema.ink, 0.55f), 3f);
        }

        private void PintarLaEstructura(DibujoUI d, float w, float h) {
            var yRoad = _deckTop + AltoLosa;
            var acero = NexusTheme.Alfa(Tema.ink, 0.38f);
            DibujosDelPuente.Barandilla(d, _cliffW, w - _cliffW, _deckTop - 18f, 18f, NexusTheme.Alfa(Tema.cyan, 0.45f));
            for (var i = 0; i < _slabCount(); i++) {
                var x0 = _cliffW + i * _slabW + 3; var x1 = x0 + _slabW - 6;
                DibujosDelPuente.Celosia(d, x0, x1, yRoad + 2, yRoad + AltoCelosia, acero);
                var cx = _cliffW + _slabW * (i + 0.5f);
                // El pilar: un hueco de cristal oscuro donde se apilan los bloques, con su remate y su cimiento.
                d.Poligono(new List<Vector2> {
                    new Vector2(cx - _slabW * 0.28f, yRoad + AltoCelosia), new Vector2(cx + _slabW * 0.28f, yRoad + AltoCelosia),
                    new Vector2(cx + _bw / 2 + 14, _pillarTop - 4), new Vector2(cx - _bw / 2 - 14, _pillarTop - 4) }, new Color(0.07f, 0.12f, 0.16f));
                d.Linea(cx - _slabW * 0.28f, yRoad + AltoCelosia, cx - _bw / 2 - 14, _pillarTop - 4, acero, 2.5f);
                d.Linea(cx + _slabW * 0.28f, yRoad + AltoCelosia, cx + _bw / 2 + 14, _pillarTop - 4, acero, 2.5f);
                d.Rect(cx - _bw / 2 - 14, _pillarTop - 4, _bw + 28, _baseY - _pillarTop + 14, NexusTheme.Alfa(ColorDelPilar(i), 0.3f), 2f, new Color(0, 0, 0, 0.32f), false, 10);
                d.Linea(cx, _pillarTop + 4, cx, _baseY, NexusTheme.Alfa(ColorDelPilar(i), 0.12f), 2f, true);
                // El cimiento, con franjas de obra.
                var py = _baseY + 8;
                d.Rect(cx - _bw / 2 - 36, py, _bw + 72, 14, NexusTheme.Alfa(Tema.ink, 0.35f), 2f, new Color(0.08f, 0.1f, 0.12f), false, 4);
                for (var k = 0; k < 12; k += 2) {
                    var fx0 = cx - _bw / 2 - 32 + (_bw + 64) * k / 12f; var fx1 = cx - _bw / 2 - 32 + (_bw + 64) * (k + 1) / 12f;
                    d.Poligono(new List<Vector2> { new Vector2(fx0, py + 3), new Vector2(fx1, py + 3), new Vector2(fx1 - 4, py + 11), new Vector2(fx0 - 4, py + 11) }, NexusTheme.Alfa(Tema.warning, 0.5f));
                }
                d.Elipse(cx, py + 20, _bw * 0.55f, 9, new Color(0, 0, 0, 0), 0, NexusTheme.Alfa(ColorDelPilar(i), 0.08f));
            }
        }

        private int _slabCount() { return Def.Puente.Pilares.Count; }

        // ---------------------------------------------------------------- un pilar: la losa, los bloques y los corazones

        private PilarVista CrearPilar(int i) {
            var def = Def.Puente.Pilares[i];
            var pv = new PilarVista { Indice = i, Def = def, Color = ColorDelPilar(i), Cx = _cliffW + _slabW * (i + 0.5f) };
            var raiz = _lam.Raiz;

            // ---- la losa del tablero (apagada hasta que su pilar se verifica)
            var losa = Ui.Nodo(raiz, "Losa " + def.Id);
            Lamina.Colocar(losa, _cliffW + i * _slabW + 3, _deckTop + 2, _slabW - 6, AltoLosa - 4);
            pv.Losa = losa;
            var luz = Ui.Nodo(losa, "Luz");
            luz.anchorMin = luz.anchorMax = luz.pivot = new Vector2(0.5f, 0.5f);
            luz.anchoredPosition = Vector2.zero;
            luz.sizeDelta = new Vector2(_slabW * 1.2f, AltoLosa * 3f);
            pv.LosaLuz = luz.gameObject.AddComponent<Image>();
            pv.LosaLuz.sprite = PlanoIlustrado.Brillo();
            pv.LosaLuz.color = Color.clear;
            pv.LosaLuz.raycastTarget = false;
            var cuerpo = Ui.Nodo(losa, "Cuerpo");
            UiKit.Rellenar(cuerpo);
            pv.LosaFondo = cuerpo.gameObject.AddComponent<Image>();
            Ui.Fondo(pv.LosaFondo, new Color(0.11f, 0.16f, 0.21f, 0.97f), 8);
            pv.LosaFondo.raycastTarget = false;
            pv.LosaBorde = Ui.Borde(cuerpo, NexusTheme.Alfa(pv.Color, 0.4f), 8, 2f);
            pv.LosaTexto = Ui.Texto(cuerpo, def.Nombre, EstiloTexto.Cuerpo, NexusTheme.Alfa(Tema.ink, 0.75f), TextAlignmentOptions.Center);
            var at = pv.LosaTexto.GetComponent<AltoDeTexto>();
            if (at != null) Destroy(at);
            pv.LosaTexto.enableAutoSizing = true; pv.LosaTexto.fontSizeMin = 14; pv.LosaTexto.fontSizeMax = 24;
            pv.LosaTexto.fontStyle = FontStyles.Bold;
            pv.LosaTexto.textWrappingMode = TextWrappingModes.Normal;
            pv.LosaTexto.raycastTarget = false;
            var tr = pv.LosaTexto.rectTransform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(46, 2); tr.offsetMax = new Vector2(-46, -2);
            var lamp = Ui.Nodo(cuerpo, "Lampara");
            lamp.anchorMin = lamp.anchorMax = new Vector2(0, 0.5f); lamp.pivot = new Vector2(0.5f, 0.5f);
            lamp.sizeDelta = new Vector2(18, 18); lamp.anchoredPosition = new Vector2(26, 0);
            pv.Lampara = lamp.gameObject.AddComponent<Image>();
            pv.Lampara.sprite = Ui.SpriteCircular();
            pv.Lampara.color = NexusTheme.Alfa(Tema.lineStrong, 0.7f);
            pv.Lampara.raycastTarget = false;
            var sello = TextoFijo(losa, "LISTO", 17, Tema.success, TextAlignmentOptions.Center, _slabW - 6 - 118, 8, 104, 30);
            sello.fontStyle = FontStyles.Bold; sello.characterSpacing = 6;
            pv.Sello = sello.rectTransform;
            pv.Sello.pivot = new Vector2(0.5f, 0.5f);
            pv.Sello.anchoredPosition += new Vector2(52f, -15f);   // el pivot pasa al centro sin mover el sello
            pv.Sello.localScale = Vector3.zero;
            var grietas = Ui.Nodo(losa, "Grietas");
            UiKit.Rellenar(grietas);
            grietas.gameObject.AddComponent<CanvasRenderer>();
            pv.GrietasDeLaLosa = grietas.gameObject.AddComponent<DibujoUI>();
            pv.GrietasDeLaLosa.raycastTarget = false;

            // ---- los bloques, apilados en el orden en que los encuentra el jugador
            pv.Visual = _st.Ordenes[i].ToList();
            var porId = def.Historias.ToDictionary(x => x.Id);
            foreach (var id in def.Historias.Select(x => x.Id)) {
                var b = BloqueDeHistoria.Crear(Ui, raiz, id, porId[id].Titulo, pv.Color, _bw, _bh);
                b.AlPulsar = bl => ElegirBloque(bl.Id);
                b.AlArrastrar = ArrastrandoBloque;
                b.AlSoltar = SoltandoBloque;
                b.AlPasarElRaton = (bl, entra) => { if (entra && !_verificando && Resultado == null) bl.Destello(NexusTheme.Alfa(pv.Color, 0.9f)); };
                b.PuedeArrastrar = false;   // hasta que termine la entrada (PintarEleccion lo habilita)
                pv.Bloques[id] = b;
            }
            Recolocar(pv, false, null);

            // ---- los corazones de vida de este pilar, en un globo bajo su cimiento
            var cartel = Ui.Nodo(raiz, "Vidas " + def.Id);
            Lamina.Colocar(cartel, pv.Cx - 118, _baseY + 36, 236, 54);
            pv.Cartel = cartel;
            var cFondo = cartel.gameObject.AddComponent<Image>();
            Ui.Fondo(cFondo, NexusTheme.Alfa(Tema.bg950, 0.86f), NexusTheme.RadioLg);
            cFondo.raycastTarget = false;
            Ui.Halo(cartel, NexusTheme.Alfa(pv.Color, 0.12f), NexusTheme.RadioLg);
            Ui.Borde(cartel, NexusTheme.Alfa(pv.Color, 0.5f), NexusTheme.RadioLg, 1.5f);
            var etiqueta = TextoFijo(cartel, "VIDAS", 14, Tema.inkMuted, TextAlignmentOptions.MidlineLeft, 16, 0, 80, 54);
            etiqueta.characterSpacing = 5;
            for (var k = 0; k < _st.VidasIniciales; k++) {
                var corazon = Ui.Nodo(cartel, "Corazon " + (k + 1));
                corazon.anchorMin = corazon.anchorMax = new Vector2(1, 0.5f);
                corazon.pivot = new Vector2(0.5f, 0.5f);
                corazon.sizeDelta = new Vector2(44, 40);
                corazon.anchoredPosition = new Vector2(-30 - (_st.VidasIniciales - 1 - k) * 48, 0);
                var cv = new CorazonVista { Rt = corazon, Fase = k * 1.7f + i };
                cv.Izq = CapaDe(corazon, "Mitad izquierda");
                cv.Der = CapaDe(corazon, "Mitad derecha");
                var rojo = new Color(0.97f, 0.27f, 0.36f); var filo = new Color(1f, 0.62f, 0.66f);
                DibujosDelPuente.MitadDeCorazon(cv.Izq, true, 22, 21, 1.05f, rojo, filo);
                DibujosDelPuente.MitadDeCorazon(cv.Der, false, 22, 21, 1.05f, rojo, filo);
                pv.Corazones.Add(cv);
            }
            return pv;
        }

        private DibujoUI CapaDe(RectTransform padre, string nombre) {
            var rt = UiKit.Rellenar(Ui.Nodo(padre, nombre));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var d = rt.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            return d;
        }

        // ---------------------------------------------------------------- el camion del cliente

        private void CrearElCamion() {
            var raiz = _lam.Raiz;
            _camion = Ui.Nodo(raiz, "Camion");
            Lamina.Colocar(_camion, 18, _deckTop - 71, 132, 72);
            var d = CapaDib(_camion, "Dibujo");
            DibujosDelPuente.Camion(d, 0, 0, new Color(0.27f, 0.5f, 0.82f), new Color(1f, 0.93f, 0.6f));
            var faro = Ui.Nodo(_camion, "Faro");
            faro.anchorMin = faro.anchorMax = new Vector2(0, 1); faro.pivot = new Vector2(0, 0.5f);
            faro.sizeDelta = new Vector2(150, 70); faro.anchoredPosition = new Vector2(122, -48);
            var fi = faro.gameObject.AddComponent<Image>();
            fi.sprite = PlanoIlustrado.Brillo();
            fi.color = new Color(1f, 0.92f, 0.6f, 0.3f);
            fi.raycastTarget = false;
            var chip = Ui.Nodo(_camion, "Cliente");
            Lamina.Colocar(chip, 6, -30, 120, 24);
            var ci = chip.gameObject.AddComponent<Image>();
            Ui.Fondo(ci, NexusTheme.Alfa(Tema.bg950, 0.88f), 6);
            ci.raycastTarget = false;
            Ui.Borde(chip, NexusTheme.Alfa(Tema.cyan, 0.5f), 6, 1.5f);
            _textoDelCamion = TextoFijo(chip, "CLIENTE", 13, Tema.ink, TextAlignmentOptions.Center, 0, 0, 120, 24);
            _textoDelCamion.characterSpacing = 3; _textoDelCamion.fontStyle = FontStyles.Bold;
        }

        private TMP_Text _textoDelCamion;

        // ---------------------------------------------------------------- la entrada

        /// <summary>
        /// La escena entra: el camion rueda hasta su sitio, las losas caen con rebote, los pilares se levantan bloque a
        /// bloque y los corazones aparecen de uno en uno. Devuelve cuanto dura todo.
        /// </summary>
        private float EntradaDeLaEscena() {
            var raiz = _lam.Raiz.gameObject;
            var grupo = _lam.Raiz.gameObject.GetComponent<CanvasGroup>();
            if (grupo == null) grupo = _lam.Raiz.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0;
            _anim.Lanzar(0.45f, t => { if (grupo != null) grupo.alpha = Curvas.SalidaCubica(t); }, () => { if (grupo != null) grupo.alpha = 1; }, 0, raiz);

            // El camion llega desde la izquierda.
            var destino = _camion.anchoredPosition;
            _camion.anchoredPosition = new Vector2(-190, destino.y);
            _anim.Lanzar(1.15f, t => { if (_camion != null) _camion.anchoredPosition = new Vector2(Mathf.Lerp(-190, destino.x, Curvas.EntradaSalida(t)), destino.y + (t < 1 ? Mathf.Abs(Mathf.Sin(t * 28f)) * 2.2f * (1 - t) : 0)); },
                         () => { if (_camion != null) _camion.anchoredPosition = destino; }, 0.3f, raiz);

            var fin = 1.0f;
            for (var i = 0; i < _pilares.Count; i++) {
                var pv = _pilares[i];
                // La losa cae desde arriba.
                var losa = pv.Losa;
                var gl = losa.gameObject.AddComponent<CanvasGroup>();
                gl.alpha = 0;
                var y0 = losa.anchoredPosition.y;
                var retraso = 0.2f + i * 0.14f;
                _anim.Lanzar(0.5f, t => {
                    if (losa == null) return;
                    gl.alpha = Mathf.Clamp01(t * 2.5f);
                    losa.anchoredPosition = new Vector2(losa.anchoredPosition.x, y0 + 70f * (1f - Curvas.SalidaAtras(t)));
                }, () => { if (losa != null) { gl.alpha = 1; losa.anchoredPosition = new Vector2(losa.anchoredPosition.x, y0); } }, retraso, losa.gameObject);
                // Los bloques caen sobre su pilar, el de abajo primero.
                for (var slot = 0; slot < pv.Visual.Count; slot++) {
                    var b = pv.Bloques[pv.Visual[slot]];
                    var g = b.gameObject.AddComponent<CanvasGroup>();
                    g.alpha = 0;
                    var meta = b.Rt.anchoredPosition;
                    var r2 = 0.55f + i * 0.12f + slot * 0.11f;
                    _anim.Lanzar(0.5f, t => {
                        if (b == null) return;
                        g.alpha = Mathf.Clamp01(t * 3f);
                        b.Rt.anchoredPosition = new Vector2(meta.x, meta.y + 130f * (1f - Curvas.SalidaAtras(t)));
                    }, () => { if (b != null) { g.alpha = 1; b.Rt.anchoredPosition = meta; } }, r2, b.gameObject);
                    fin = Mathf.Max(fin, r2 + 0.5f);
                }
                // Los corazones y su globo.
                var cartel = pv.Cartel;
                var gc = cartel.gameObject.AddComponent<CanvasGroup>();
                gc.alpha = 0;
                var r3 = 0.9f + i * 0.12f;
                _anim.Lanzar(0.4f, t => { if (gc != null) { gc.alpha = Mathf.Clamp01(t * 2f); cartel.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, Curvas.SalidaAtras(t)); } },
                             () => { if (gc != null) gc.alpha = 1; }, r3, cartel.gameObject);
                fin = Mathf.Max(fin, r3 + 0.4f);
            }
            return fin;
        }

        // ==================================================================== piezas y busquedas

        private BloqueDeHistoria BloqueDe(string id) {
            if (id == null) return null;
            foreach (var p in _pilares) { BloqueDeHistoria b; if (p.Bloques.TryGetValue(id, out b)) return b; }
            return null;
        }

        private PilarVista PilarDeBloque(string id) {
            return _pilares.FirstOrDefault(p => p.Bloques.ContainsKey(id));
        }

        private RectTransform BloqueEnSlot(PilarVista p, int slot) {
            return p != null && slot < p.Visual.Count ? p.Bloques[p.Visual[slot]].Rt : null;
        }

        private string Titulo(string id) {
            foreach (var p in Def.Puente.Pilares) foreach (var h in p.Historias) if (h.Id == id) return h.Titulo;
            return id ?? "";
        }

        // ==================================================================== colocar bloques

        private void MoverA(BloqueDeHistoria b, Vector2 objetivo, bool animar) {
            var v = ++b.Version;
            var desde = b.Rt.anchoredPosition;
            if (!animar || (desde - objetivo).sqrMagnitude < 0.25f) { b.Rt.anchoredPosition = objetivo; return; }
            _anim.Lanzar(0.26f, t => {
                if (b == null || b.Version != v || b.Arrastrando) return;
                b.Rt.anchoredPosition = Vector2.LerpUnclamped(desde, objetivo, Curvas.SalidaAtras(t));
            }, () => { if (b != null && b.Version == v && !b.Arrastrando) b.Rt.anchoredPosition = objetivo; }, 0, b.gameObject);
        }

        /// <summary>Cada bloque a su puesto segun el orden visual del pilar. 'excepto' = el que se arrastra.</summary>
        private void Recolocar(PilarVista p, bool animar, string excepto) {
            for (var slot = 0; slot < p.Visual.Count; slot++) {
                var id = p.Visual[slot];
                if (id == excepto) continue;
                MoverA(p.Bloques[id], new Vector2(p.Cx - _bw / 2f, -YDeSlot(slot)), animar);
            }
        }

        private int SlotDeY(PilarVista p, float centroY) {
            var slot = Mathf.RoundToInt((_baseY - _bh / 2f - centroY) / (_bh + Hueco));
            return Mathf.Clamp(slot, 0, p.Visual.Count - 1);
        }

        private bool PuedeJugar { get { return Resultado == null && !_bloqueado && !_verificando && !TutorialAbierto && _st != null && !_st.Perdido; } }

        private void ArrastrandoBloque(BloqueDeHistoria b, float centroY) {
            var pv = PilarDeBloque(b.Id);
            if (pv == null) return;
            var slot = SlotDeY(pv, centroY);
            var idx = pv.Visual.IndexOf(b.Id);
            if (slot == idx) return;
            pv.Visual.RemoveAt(idx);
            pv.Visual.Insert(slot, b.Id);
            Recolocar(pv, true, b.Id);
        }

        private void SoltandoBloque(BloqueDeHistoria b, float centroY) {
            var pv = PilarDeBloque(b.Id);
            if (pv == null) return;
            Colocar(b.Id, pv.Visual.IndexOf(b.Id), true);
        }

        /// <summary>Un bloque a un puesto de su pilar (arrastrando, con las flechas o por el modo guiado).</summary>
        private void Colocar(string id, int slot, bool desdeArrastre) {
            var pv = PilarDeBloque(id);
            if (pv == null) return;
            if (!PuedeJugar || !_st.SePuedeMover(pv.Indice)) { RestaurarVisual(pv); return; }
            if (Guiado && !Guia.Permite(Nexus.Core.Minijuegos.AccionGuiada.Mover, id)) {
                Guia.Rechazar();
                RestaurarVisual(pv);
                return;
            }
            RestablecerPilar(pv);
            _st.Mover(pv.Indice, id, slot);
            pv.Visual = _st.Ordenes[pv.Indice].ToList();
            Recolocar(pv, true, null);
            _elegido = id;
            if (Guiado) {
                var paso = Guia.Actual;
                if (paso != null && paso.Accion == Nexus.Core.Minijuegos.AccionGuiada.Mover && paso.Objetivo == id && paso.Valor == slot)
                    Guia.Hecho(Nexus.Core.Minijuegos.AccionGuiada.Mover, id);
            }
            PintarNecesidades();
            Repintar();
        }

        private void RestaurarVisual(PilarVista pv) {
            pv.Visual = _st.Ordenes[pv.Indice].ToList();
            Recolocar(pv, true, null);
        }

        /// <summary>Quien mueve un bloque de un pilar con luces rojas o apagadas las borra: vuelve a empezar a revisar.</summary>
        private void RestablecerPilar(PilarVista pv) {
            foreach (var b in pv.Bloques.Values)
                if (b.Vista == VistaDeBloque.Mal || b.Vista == VistaDeBloque.Apagado || b.Vista == VistaDeBloque.Bien) b.Poner(VistaDeBloque.Normal);
        }

        // ==================================================================== eleccion, teclado y guia

        private void ElegirBloque(string id) {
            if (!PuedeJugar) return;
            var pv = PilarDeBloque(id);
            if (pv == null) return;
            if (!_st.SePuedeMover(pv.Indice)) { Decir("«" + pv.Def.Nombre + "» ya está verificado: ahí no se toca."); return; }
            _elegido = _elegido == id ? null : id;
            if (_elegido != null) Decir("«" + Titulo(id) + "». Súbelo o bájalo con ▲ ▼, o arrástralo.");
            PintarEleccion();
        }

        private void MoverElegido(int delta) {
            if (_elegido == null) return;
            var pv = PilarDeBloque(_elegido);
            if (pv == null) return;
            var idx = pv.Visual.IndexOf(_elegido);
            var slot = idx + delta;
            if (slot < 0 || slot >= pv.Visual.Count) return;
            Colocar(_elegido, slot, false);
        }

        private void CambiarDeBloque(int paso) {
            var todos = _pilares.Where(p => _st.SePuedeMover(p.Indice)).SelectMany(p => Enumerable.Range(0, p.Visual.Count).Reverse().Select(s => p.Visual[s])).ToList();
            if (todos.Count == 0) return;
            var i = todos.IndexOf(_elegido);
            i = i < 0 ? (paso > 0 ? 0 : todos.Count - 1) : (i + paso + todos.Count) % todos.Count;
            _elegido = todos[i];
            PintarEleccion();
        }

        private void ColocarGuiado() {
            var paso = Guia != null ? Guia.Actual : null;
            if (paso == null || paso.Accion != Nexus.Core.Minijuegos.AccionGuiada.Mover) return;
            Colocar(paso.Objetivo, paso.Valor, false);
        }

        /// <summary>El bloque elegido se ilumina; los que aun esperan, normales. No toca los que ya tienen veredicto.</summary>
        private void PintarEleccion() {
            if (_st == null) return;
            foreach (var pv in _pilares) {
                var movible = _st.SePuedeMover(pv.Indice) && PuedeJugar;
                foreach (var b in pv.Bloques.Values) {
                    b.PuedeArrastrar = movible;
                    if (b.Vista == VistaDeBloque.Normal || b.Vista == VistaDeBloque.Elegido) {
                        var v = b.Id == _elegido && movible ? VistaDeBloque.Elegido : VistaDeBloque.Normal;
                        if (b.Vista != v) b.Poner(v);
                        else b.RefrescarIcono();
                    }
                }
            }
        }

        /// <summary>Lo que necesita cada bloque, en una linea pequeña (solo con ayuda, andamiaje o modo guiado).</summary>
        private void PintarNecesidades() {
            foreach (var pv in _pilares) {
                var porId = pv.Def.Historias.ToDictionary(h => h.Id);
                foreach (var h in pv.Def.Historias)
                    pv.Bloques[h.Id].MostrarNecesidades(!_necesidadesVisibles ? ""
                        : h.DependeDe.Count == 0 ? "no necesita nada"
                        : "necesita: " + string.Join(", ", h.DependeDe.Select(d => porId[d].Titulo)));
            }
        }

        /// <summary>Guiado: el hueco al que hay que llevar el bloque del paso, marcado, para que se vea a donde va.</summary>
        private void PintarResalteGuiado() {
            if (_resalte == null) return;
            _resalte.Limpiar();
            var paso = Guia != null ? Guia.Actual : null;
            if (paso == null || paso.Accion != Nexus.Core.Minijuegos.AccionGuiada.Mover) return;
            var pv = PilarDeBloque(paso.Objetivo);
            if (pv == null || pv.Visual.IndexOf(paso.Objetivo) == paso.Valor) return;
            var y = YDeSlot(paso.Valor);
            _resalte.Rect(pv.Cx - _bw / 2f - 8, y - 6, _bw + 16, _bh + 12, Tema.warning, 3f, NexusTheme.Alfa(Tema.warning, 0.06f), true, 14);
        }

        // ---------------------------------------------------------------- corazones

        private void LatirCorazones() {
            var t = Time.unscaledTime;
            foreach (var pv in _pilares)
                foreach (var c in pv.Corazones) {
                    if (c.Perdido || c.Rt == null || _verificando) continue;
                    c.Rt.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(t * 3.6f + c.Fase));
                }
        }
    }
}
