using System.Collections.Generic;
using Nexus.Core.Jornada;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La ventana de la oficina a las cinco (2026-11-07), arriba del cierre de la jornada: el cielo del atardecer, el sol
    /// medio hundido tras la ciudad, las ventanas de los edificios que se van encendiendo (la ciudad se va a casa), la
    /// lampara y la taza en el alfeizar, y en la pared el reloj con su segundero y el abrigo colgado.
    ///
    /// La decision la cambia (Modo, al mirar una carta):
    ///   Casa      el sol termina de ponerse, se hace de noche (luna, estrellas), la lampara se apaga y el abrigo se mece
    ///   Quedarse  sigue la tarde, la lampara se enciende, la taza humea y en el reloj se marca el arco hasta la hora limite
    /// Los cambios entran en ~220 ms con ease-out y vuelven a la tarde un poco mas rapido (emil-design). Todo esta dibujado
    /// en codigo, en un lienzo de 760×250 que se escala para caber en el hueco que le deje el layout.
    /// </summary>
    public sealed class VentanaDelAtardecer : MonoBehaviour {
        public enum Modo { Neutro, Casa, Quedarse }

        private const float Ancho = 760, Alto = 250;
        // El hueco de la ventana, en coordenadas del lienzo (desde arriba a la izquierda).
        private const float VX = 30, VY = 12, VW = 520, VH = 194;
        private const float RelojX = 662, RelojY = 74, RelojR = 46;
        private const float SolX = 330;

        public Modo Actual = Modo.Neutro;

        private RectTransform _lienzo, _sol, _segundero, _abrigo;
        private CanvasGroup _noche, _lunaYEstrellas, _arco, _vapor;
        private Image _brilloDelSol, _brilloDeLampara, _bombilla;
        private readonly List<Image> _ventanas = new List<Image>();
        private int[] _orden;
        private float _casa, _quedarse, _t;
        private float _solTarde, _solNoche;
        private Color _luzEncendida, _luzApagada;

        /// <summary>
        /// Crea la ventana dentro de 'padre' (que la maqueta: flexible a lo ancho y a lo alto). minutoDelDia es la hora que
        /// marca el reloj; limite, en minutos del dia, cierra el arco de las horas extra y hunde el sol.
        /// </summary>
        public static VentanaDelAtardecer Crear(UiKit ui, Transform padre, int minutoDelDia, int cierre, int limite) {
            var hueco = ui.Nodo(padre, "Ventana del atardecer");
            var le = UiKit.Tamano(hueco, alto: 150, flexAncho: 1, flexAlto: 1);
            le.preferredHeight = Alto;
            var v = hueco.gameObject.AddComponent<VentanaDelAtardecer>();
            v.Construir(ui, minutoDelDia, cierre, limite);
            return v;
        }

        private void Construir(UiKit ui, int minuto, int cierre, int limite) {
            var tema = ui.Tema;
            _lienzo = ui.Nodo(transform, "Lienzo");
            _lienzo.anchorMin = _lienzo.anchorMax = _lienzo.pivot = new Vector2(0.5f, 0.5f);
            _lienzo.sizeDelta = new Vector2(Ancho, Alto);
            _luzEncendida = NexusTheme.Alfa(Hex(0xF5C46B), 0.9f);
            _luzApagada = NexusTheme.Alfa(Hex(0x3A4356), 0.55f);

            // ---- el cristal: todo lo de fuera se recorta al hueco de la ventana
            var cristal = ui.Nodo(_lienzo, "Cristal");
            Lamina.Colocar(cristal, VX, VY, VW, VH);
            cristal.gameObject.AddComponent<RectMask2D>();

            var tarde = Capa(ui, cristal, "Cielo de tarde");
            Bandas(tarde, new[] { Hex(0x1C2442), Hex(0x2E2A52), Hex(0x55344E), Hex(0x86503F) });

            var noche = Capa(ui, cristal, "Cielo de noche");
            Bandas(noche, new[] { Hex(0x070A16), Hex(0x0B1024), Hex(0x111733), Hex(0x18203F) });
            _noche = Grupo(noche.rectTransform, 0);

            // Luna y estrellas, solo de noche.
            var astros = Capa(ui, cristal, "Luna y estrellas");
            astros.Circulo(92, 40, 14, Color.clear, 0, Hex(0xE8E6D8));
            astros.Circulo(99, 35, 12, Color.clear, 0, Hex(0x070A16));
            var estrellas = new[] { P(160, 22), P(240, 48), P(330, 18), P(410, 40), P(470, 14), P(40, 70), P(205, 80), P(370, 76) };
            foreach (var e in estrellas) astros.Circulo(e.x, e.y, 1.6f, Color.clear, 0, NexusTheme.Alfa(tema.ink, 0.8f));
            _lunaYEstrellas = Grupo(astros.rectTransform, 0);

            // El sol: un halo difuso y el disco, detras de la ciudad. Mas tarde, mas hundido.
            _sol = ui.Nodo(cristal, "Sol");
            _sol.anchorMin = _sol.anchorMax = new Vector2(0, 1);
            _sol.pivot = new Vector2(0.5f, 0.5f);
            _sol.sizeDelta = new Vector2(60, 60);
            var halo = ui.Nodo(_sol, "Halo");
            halo.anchorMin = halo.anchorMax = halo.pivot = new Vector2(0.5f, 0.5f);
            halo.sizeDelta = new Vector2(230, 230);
            _brilloDelSol = Imagen(halo, PlanoIlustrado.Brillo(), NexusTheme.Alfa(Hex(0xF6B45E), 0.5f));
            var disco = Capa(ui, _sol, "Disco");
            disco.Circulo(30, 30, 24, Color.clear, 0, Hex(0xF2B05E));
            _solTarde = Mathf.Lerp(150, 168, EscenaDelCierre.FraccionDeTarde(minuto, cierre, limite));
            _solNoche = 228;
            _sol.anchoredPosition = new Vector2(SolX, -_solTarde);

            // La ciudad: una fila lejana y clara, otra cercana y oscura, con sus ventanas.
            var lejos = Capa(ui, cristal, "Ciudad lejana");
            var cerca = Capa(ui, cristal, "Ciudad cercana");
            var luces = ui.Nodo(cristal, "Luces");
            UiKit.Rellenar(luces);
            Edificios(ui, lejos, luces, Hex(0x1A2130), new[] { 0f, 52, 96, 150, 214, 262, 330, 388, 446 },
                      new[] { 50f, 40, 52, 60, 44, 66, 54, 56, 74 }, new[] { 88f, 114, 76, 102, 122, 82, 110, 94, 80 }, false);
            Edificios(ui, cerca, luces, Hex(0x0E131D), new[] { 0f, 74, 128, 206, 276, 352, 420, 470 },
                      new[] { 70f, 50, 74, 66, 72, 64, 46, 50 }, new[] { 34f, 56, 26, 44, 30, 50, 38, 28 }, true);
            _orden = EscenaDelCierre.OrdenDeEncendido(_ventanas.Count);

            // Reflejos en el cristal.
            var reflejos = Capa(ui, cristal, "Reflejos");
            reflejos.Linea(60, 180, 150, 20, NexusTheme.Alfa(tema.ink, 0.05f), 10);
            reflejos.Linea(90, 190, 170, 50, NexusTheme.Alfa(tema.ink, 0.04f), 4);

            // ---- el marco, el parteluz y el alfeizar
            var marco = Capa(ui, _lienzo, "Marco");
            marco.Rect(VX, VY, VW, VH, tema.lineStrong, 6, null, false, 6);
            marco.Linea(VX + VW / 2, VY, VX + VW / 2, VY + VH, tema.lineStrong, 5);
            marco.Rect(VX - 14, VY + VH, VW + 28, 14, tema.lineStrong, 2, tema.surfaceRaised, false, 3);

            // ---- en el alfeizar: la lampara (izquierda) y la taza (derecha)
            _brilloDeLampara = Imagen(Colocado(ui, _lienzo, "Luz de la lampara", 26, 80, 180, 140), PlanoIlustrado.Brillo(),
                                      NexusTheme.Alfa(Hex(0xFFD08A), 0.3f));
            var lampara = Capa(ui, _lienzo, "Lampara");
            const float base_ = VY + VH;
            lampara.Elipse(78, base_ - 3, 22, 5, tema.inkFaint, 2, tema.surfaceRaised);
            lampara.Linea(78, base_ - 6, 92, base_ - 50, tema.inkMuted, 4);
            lampara.Linea(92, base_ - 50, 112, base_ - 66, tema.inkMuted, 4);
            lampara.Poligono(new List<Vector2> { P(100, base_ - 84), P(132, base_ - 80), P(140, base_ - 58), P(96, base_ - 64) }, tema.inkMuted);
            _bombilla = Imagen(Colocado(ui, _lienzo, "Bombilla", 106, base_ - 70, 28, 28), PlanoIlustrado.Brillo(), NexusTheme.Alfa(Hex(0xFFE2A8), 0.4f));

            var taza = Capa(ui, _lienzo, "Taza");
            taza.Rect(462, base_ - 30, 30, 30, tema.inkFaint, 2, Hex(0xC9D2DC), false, 4);
            taza.Circulo(497, base_ - 16, 8, Hex(0xC9D2DC), 3);
            var vapor = Colocado(ui, _lienzo, "Vapor", 462, base_ - 62, 30, 30);
            for (var i = 0; i < 3; i++) {
                var voluta = Colocado(ui, vapor, "Voluta", 2 + i * 8, 18, 12, 12);
                Imagen(voluta, PlanoIlustrado.Brillo(), NexusTheme.Alfa(tema.ink, 0.7f));
                var sube = voluta.gameObject.AddComponent<SubeEnBucle>();
                sube.Fase = i / 3f; sube.Altura = 22; sube.Periodo = 2.2f;
            }
            _vapor = Grupo(vapor, 0.3f);

            // ---- en la pared: el reloj
            var reloj = Capa(ui, _lienzo, "Reloj");
            reloj.Circulo(RelojX, RelojY, RelojR, tema.lineStrong, 4, tema.surfaceSunken);
            for (var h = 0; h < 12; h++) {
                var a = h * 30 * Mathf.Deg2Rad;
                var grande = h % 3 == 0;
                var largo = grande ? 9f : 5f;
                reloj.Linea(RelojX + Mathf.Sin(a) * (RelojR - 6), RelojY - Mathf.Cos(a) * (RelojR - 6),
                            RelojX + Mathf.Sin(a) * (RelojR - 6 - largo), RelojY - Mathf.Cos(a) * (RelojR - 6 - largo),
                            grande ? tema.ink : tema.inkFaint, grande ? 3 : 2);
            }
            // El arco de las horas extra, de ahora a la hora limite (en la esfera de las horas).
            var arco = Capa(ui, _lienzo, "Arco de horas extra");
            var desde = EscenaDelCierre.AnguloDeHora(minuto);
            var hasta = desde + Mathf.Max(0, limite - minuto) / 720f * 360f;
            if (hasta > desde) {
                var puntos = new List<Vector2>();
                for (var k = 0; k <= 16; k++) {
                    var a = Mathf.Lerp(desde, hasta, k / 16f) * Mathf.Deg2Rad;
                    puntos.Add(P(RelojX + Mathf.Sin(a) * (RelojR + 8), RelojY - Mathf.Cos(a) * (RelojR + 8)));
                }
                arco.Polilinea(puntos, tema.warning, 4);
                var fin = puntos[puntos.Count - 1];
                arco.Circulo(fin.x, fin.y, 4, Color.clear, 0, tema.warning);
            }
            _arco = Grupo(arco.rectTransform, 0);

            Aguja(ui, EscenaDelCierre.AnguloDeHora(minuto), 26, 5, tema.ink);
            Aguja(ui, EscenaDelCierre.AnguloDeMinuto(minuto), 36, 3, tema.ink);
            _segundero = Aguja(ui, 0, 38, 1.5f, tema.cyan);
            var eje = Capa(ui, _lienzo, "Eje");
            eje.Circulo(RelojX, RelojY, 4, Color.clear, 0, tema.cyan);

            // ---- el abrigo, colgado bajo el reloj
            var percha = Capa(ui, _lienzo, "Percha");
            percha.Linea(RelojX - 12, 146, RelojX + 12, 146, tema.lineStrong, 4);
            percha.Circulo(RelojX, 150, 3, Color.clear, 0, tema.inkMuted);
            _abrigo = ui.Nodo(_lienzo, "Abrigo");
            _abrigo.anchorMin = _abrigo.anchorMax = new Vector2(0, 1);
            _abrigo.pivot = new Vector2(0.5f, 1f);
            _abrigo.sizeDelta = new Vector2(70, 80);
            _abrigo.anchoredPosition = new Vector2(RelojX, -150);
            var tela = Capa(ui, _abrigo, "Tela");
            tela.Poligono(new List<Vector2> { P(35, 0), P(56, 12), P(64, 78), P(6, 78), P(14, 12) }, Hex(0x3A2F4F));
            tela.Linea(35, 4, 35, 78, NexusTheme.Alfa(tema.ink, 0.15f), 2);
            tela.Linea(35, 4, 24, 26, NexusTheme.Alfa(tema.ink, 0.2f), 2);
            tela.Linea(35, 4, 46, 26, NexusTheme.Alfa(tema.ink, 0.2f), 2);

            Ajustar();
        }

        // ==================================================================== cada fotograma

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            _t += dt;
            Ajustar();

            // Hacia el modo que se mira: entra con ease-out (~220 ms); volver a la tarde va algo mas rapido.
            _casa = Acercar(_casa, Actual == Modo.Casa ? 1 : 0, dt);
            _quedarse = Acercar(_quedarse, Actual == Modo.Quedarse ? 1 : 0, dt);

            _noche.alpha = _casa;
            _lunaYEstrellas.alpha = _casa;
            _arco.alpha = _quedarse;
            _vapor.alpha = (0.3f + 0.7f * _quedarse) * (1 - _casa);
            _sol.anchoredPosition = new Vector2(SolX, -(Mathf.Lerp(_solTarde, _solNoche, _casa) - 10 * _quedarse));
            _brilloDelSol.color = NexusTheme.Alfa(_brilloDelSol.color, 0.5f * (1 - _casa) + 0.15f * _quedarse);
            var lampara = 0.3f * (1 - _casa) + 0.6f * _quedarse;
            _brilloDeLampara.color = NexusTheme.Alfa(_brilloDeLampara.color, lampara);
            _bombilla.color = NexusTheme.Alfa(_bombilla.color, Mathf.Clamp01(lampara * 1.4f));

            // El abrigo se mece cuando te vas (alguien lo va a coger).
            _abrigo.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_t * 3.2f) * 6f * _casa);

            // El segundero salta cada segundo (un tic, no un barrido).
            _segundero.localRotation = Quaternion.Euler(0, 0, -Mathf.Floor(_t) * 6f);

            // La ciudad se va encendiendo en su orden salteado, en bucle.
            var encendidas = EscenaDelCierre.VentanasEncendidas(_t, _ventanas.Count);
            for (var i = 0; i < _orden.Length; i++)
                _ventanas[_orden[i]].color = i < encendidas ? _luzEncendida : _luzApagada;
        }

        private static float Acercar(float v, float objetivo, float dt) {
            // Exponencial: rapida al principio y aterriza suave. k=14 llega al 95 % en ~215 ms; k=20, en ~150 ms.
            var k = objetivo > v ? 14f : 20f;
            return Mathf.Lerp(v, objetivo, 1 - Mathf.Exp(-k * dt));
        }

        /// <summary>El lienzo se escala para caber en el hueco, sin deformarse (nunca mas de un 15 % de su tamaño).</summary>
        private void Ajustar() {
            var r = ((RectTransform)transform).rect;
            if (r.width < 1 || r.height < 1) return;
            var s = Mathf.Min(r.width / Ancho, r.height / Alto, 1.15f);
            _lienzo.localScale = new Vector3(s, s, 1);
        }

        // ==================================================================== piezas

        private void Edificios(UiKit ui, DibujoUI d, RectTransform luces, Color color, float[] xs, float[] anchos, float[] altos, bool cerca) {
            for (var i = 0; i < xs.Length; i++) {
                float x = xs[i], w = anchos[i], y = VH - altos[i];
                d.Poligono(new List<Vector2> { P(x, y), P(x + w, y), P(x + w, VH), P(x, VH) }, color);
                // Las ventanas: una rejilla con aire, solo arriba del edificio; en la fila cercana, mas grandes.
                float lw = cerca ? 8 : 5, lh = cerca ? 10 : 6, paso = cerca ? 16 : 11, hondo = cerca ? 36 : 60;
                for (var fy = y + 8; fy + lh < VH - 6 && fy < y + hondo; fy += paso)
                    for (var fx = x + 6; fx + lw < x + w - 4; fx += paso)
                        _ventanas.Add(Imagen(Colocado(ui, luces, "Ventana", fx, fy, lw, lh), null, _luzApagada));
            }
        }

        private RectTransform Aguja(UiKit ui, float angulo, float largo, float grosor, Color color) {
            var rt = ui.Nodo(_lienzo, "Aguja");
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(grosor, largo);
            rt.anchoredPosition = new Vector2(RelojX, -RelojY);
            rt.localRotation = Quaternion.Euler(0, 0, -angulo);
            Imagen(rt, null, color);
            return rt;
        }

        /// <summary>Cuatro bandas de cielo, de arriba abajo: un degradado escalonado, a proposito.</summary>
        private static void Bandas(DibujoUI d, Color[] colores) {
            var alto = VH / colores.Length;
            for (var i = 0; i < colores.Length; i++)
                d.Poligono(new List<Vector2> { P(0, i * alto - 1), P(VW, i * alto - 1), P(VW, (i + 1) * alto + 1), P(0, (i + 1) * alto + 1) }, colores[i]);
        }

        internal static DibujoUI Capa(UiKit ui, Transform padre, string nombre) {
            var rt = ui.Nodo(padre, nombre);
            UiKit.Rellenar(rt);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var d = rt.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            return d;
        }

        internal static CanvasGroup Grupo(RectTransform rt, float alfa) {
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = alfa;
            g.blocksRaycasts = false;
            g.interactable = false;
            return g;
        }

        internal static Image Imagen(RectTransform rt, Sprite sprite, Color color) {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        internal static RectTransform Colocado(UiKit ui, Transform padre, string nombre, float x, float y, float w, float h) {
            var rt = ui.Nodo(padre, nombre);
            Lamina.Colocar(rt, x, y, w, h);
            return rt;
        }

        private static Vector2 P(float x, float y) { return new Vector2(x, y); }
        internal static Color Hex(int rgb) { return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1); }
    }
}
