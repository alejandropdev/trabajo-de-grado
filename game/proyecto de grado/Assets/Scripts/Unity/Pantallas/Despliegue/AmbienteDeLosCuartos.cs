using System;
using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Lo que mueve un cuarto de la Fase 3 sin tocar su logica (2026-11-09, ronda 2): que la sala se sienta como un sitio
    /// donde estas de pie y no como un dibujo quieto.
    ///
    ///   · camara en primera persona: balanceo al respirar (px, giro y escala); mas fuerte al cruzar a otro cuarto;
    ///   · paralaje: el fondo y los props se desplazan en sentido contrario al haz, cada capa con distinta fuerza;
    ///   · polvo: motas que flotan despacio (estan bajo la oscuridad: solo se ven dentro del haz);
    ///   · LEDs que parpadean (racks, tomas, cuadros) y bombillas del techo que fallan de vez en cuando.
    /// Todo en tiempo sin escalar. Los LEDs se redibujan unas diez veces por segundo en un unico DibujoUI.
    /// </summary>
    public sealed class AmbienteDeLosCuartos : MonoBehaviour {
        public sealed class Led {
            public Vector2 Pos;        // en coordenadas de la lamina (origen arriba a la izquierda)
            public Color Color;
            public float Radio = 4f;
            public float Fase;
            public bool Fijo;          // siempre encendido
            public bool EnProps;       // dibujado en la capa de los props (la del fondo, si no)
        }

        public RectTransform Camara, Fondo, Props, Polvo;
        public DibujoUI Luces, LucesProps;
        public Func<Vector2> Haz;                // el haz de la linterna, con el origen en el centro y hacia arriba
        public float Ancho, Alto;
        public readonly List<Led> Leds = new List<Led>();
        public readonly List<Image> Bombillas = new List<Image>();
        public readonly List<float> BrilloDeBombillas = new List<float>();

        private readonly List<RectTransform> _motas = new List<RectTransform>();
        private readonly List<Vector3> _datosDeMotas = new List<Vector3>();   // x = velocidad, y = fase, z = amplitud
        private float _fuerza = 1f, _proximoLed;
        private Vector2 _pFondo, _pProps;

        /// <summary>Crea las motas de polvo (bajo la oscuridad: solo se ven donde llega el haz).</summary>
        public void PrepararPolvo(UiKit ui, Sprite circulo, int cuantas) {
            if (Polvo == null) return;
            for (var i = 0; i < cuantas; i++) {
                var m = ui.Nodo(Polvo, "Mota");
                m.anchorMin = m.anchorMax = m.pivot = new Vector2(0.5f, 0.5f);
                var tam = Random.Range(2.5f, 6.5f);
                m.sizeDelta = new Vector2(tam, tam);
                m.anchoredPosition = new Vector2(Random.Range(-Ancho / 2, Ancho / 2), Random.Range(-Alto / 2, Alto / 2));
                var img = m.gameObject.AddComponent<Image>();
                img.sprite = circulo;
                img.color = new Color(1f, 0.95f, 0.8f, Random.Range(0.22f, 0.6f));
                img.raycastTarget = false;
                _motas.Add(m);
                _datosDeMotas.Add(new Vector3(Random.Range(5f, 18f), Random.Range(0f, 6.28f), Random.Range(4f, 22f)));
            }
        }

        /// <summary>Al entrar en un cuarto la camara se balancea mas: se acaba de caminar.</summary>
        public void Caminar() { _fuerza = 3.4f; }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            var t = Time.unscaledTime;
            _fuerza = Mathf.MoveTowards(_fuerza, 1f, dt * 1.7f);

            // ---- la camara: respira (y camina)
            if (Camara != null) {
                var f = _fuerza;
                var camina = Mathf.Max(0f, f - 1f);
                var bx = Mathf.Sin(t * Mathf.PI * 2f * 0.9f) * 6f * f * 0.7f + Mathf.Sin(t * 1.3f) * 2f;
                var by = Mathf.Sin(t * Mathf.PI * 2f * 0.9f + 1.1f) * 4f * f + Mathf.Abs(Mathf.Sin(t * 7f)) * 9f * camina;
                Camara.anchoredPosition = new Vector2(bx, by);
                Camara.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 0.8f) * 0.4f * f);
                Camara.localScale = Vector3.one * (1.005f + 0.005f * Mathf.Sin(t * Mathf.PI * 2f * 0.45f));
            }

            // ---- el paralaje: en sentido contrario al haz
            var haz = Haz != null ? Haz() : Vector2.zero;
            var nx = Ancho > 1 ? Mathf.Clamp(haz.x / (Ancho / 2f), -1f, 1f) : 0f;
            var ny = Alto > 1 ? Mathf.Clamp(haz.y / (Alto / 2f), -1f, 1f) : 0f;
            var kk = 1f - Mathf.Exp(-6f * dt);
            _pFondo = Vector2.Lerp(_pFondo, new Vector2(-nx * 14f, -ny * 8f), kk);
            _pProps = Vector2.Lerp(_pProps, new Vector2(-nx * 28f, -ny * 15f), kk);
            if (Fondo != null) Fondo.anchoredPosition = _pFondo;
            if (Props != null) Props.anchoredPosition = _pProps;

            // ---- el polvo
            for (var i = 0; i < _motas.Count; i++) {
                var m = _motas[i];
                if (m == null) continue;
                var d = _datosDeMotas[i];
                var p = m.anchoredPosition;
                p.y += d.x * dt;
                p.x += Mathf.Sin(t * 0.7f + d.y) * d.z * dt * 0.6f;
                if (p.y > Alto / 2f + 10f) { p.y = -Alto / 2f - 10f; p.x = Random.Range(-Ancho / 2, Ancho / 2); }
                m.anchoredPosition = p;
            }

            // ---- las bombillas del techo: vibran y, a veces, se van un instante
            for (var i = 0; i < Bombillas.Count; i++) {
                var b = Bombillas[i];
                if (b == null) continue;
                var r = Mathf.PerlinNoise(t * 2.3f, i * 9.1f);
                var apagon = Mathf.PerlinNoise(t * 0.55f, 40f + i * 5f) < 0.13f ? 0.12f : 1f;
                b.color = new Color(1f, 0.9f, 0.62f, BrilloDeBombillas[i] * (0.75f + 0.25f * r) * apagon);
            }

            // ---- los LEDs
            _proximoLed -= dt;
            if (Luces != null && _proximoLed <= 0f) {
                _proximoLed = 0.1f;
                Luces.Limpiar();
                if (LucesProps != null) LucesProps.Limpiar();
                for (var i = 0; i < Leds.Count; i++) {
                    var l = Leds[i];
                    var dib = l.EnProps && LucesProps != null ? LucesProps : Luces;
                    var encendido = l.Fijo || Mathf.PerlinNoise(l.Fase * 7.3f, t * 1.7f) > 0.42f;
                    var apagado = new Color(l.Color.r, l.Color.g, l.Color.b, 0.18f);
                    if (!encendido) { dib.Circulo(l.Pos.x, l.Pos.y, l.Radio, apagado, 0, apagado); continue; }
                    var halo = new Color(l.Color.r, l.Color.g, l.Color.b, 0.16f);
                    dib.Circulo(l.Pos.x, l.Pos.y, l.Radio * 2.6f, halo, 0, halo);
                    dib.Circulo(l.Pos.x, l.Pos.y, l.Radio, l.Color, 0, l.Color);
                }
            }
        }
    }

    /// <summary>
    /// El grano de pelicula por encima de la oscuridad: una textura de ruido que salta de sitio unas doce veces por
    /// segundo. Con poca opacidad, da a todo el cuarto un temblor de imagen viva.
    /// </summary>
    public sealed class GranoDePelicula : MonoBehaviour {
        private RectTransform _rt;
        private float _t;
        private const float Lado = 128f;
        private static Sprite _ruido;

        public static Sprite Ruido() {
            if (_ruido != null) return _ruido;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            var rnd = new System.Random(7);
            var azar = new float[n * n];
            for (var i = 0; i < azar.Length; i++) azar[i] = (float)rnd.NextDouble();
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++) {
                    // Un poco de suavizado con los vecinos (con vuelta) para que no sea ruido blanco puro.
                    var v = azar[y * n + x] * 0.6f + azar[y * n + (x + 1) % n] * 0.2f + azar[((y + 1) % n) * n + x] * 0.2f;
                    var claro = v > 0.5f;
                    var a = Mathf.Abs(v - 0.5f) * 2f;
                    var c = (byte)(claro ? 255 : 0);
                    px[y * n + x] = new Color32(c, c, c, (byte)Mathf.RoundToInt(a * a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _ruido = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return _ruido;
        }

        /// <summary>Una capa de ruido que cubre el area (y sobra un poco por los lados, para poder moverla).</summary>
        public static RectTransform Crear(UiKit ui, RectTransform padre, float alfa, bool anima) {
            var rt = ui.Nodo(padre, "Grano");
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-Lado, -Lado); rt.offsetMax = new Vector2(Lado, Lado);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Ruido();
            img.type = Image.Type.Tiled;
            img.color = new Color(1f, 1f, 1f, alfa);
            img.raycastTarget = false;
            if (anima) rt.gameObject.AddComponent<GranoDePelicula>();
            return rt;
        }

        private void Awake() { _rt = (RectTransform)transform; }

        private void Update() {
            _t -= Time.unscaledDeltaTime;
            if (_t > 0f) return;
            _t = 0.08f;
            _rt.anchoredPosition = new Vector2(Random.Range(-Lado, Lado) * 0.5f, Random.Range(-Lado, Lado) * 0.5f);
        }
    }
}
