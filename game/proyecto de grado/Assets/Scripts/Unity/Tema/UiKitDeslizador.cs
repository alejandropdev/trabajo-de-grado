using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// El deslizador de las opciones (volumen, musica, sensibilidad, velocidad). Sustituye a los botones sueltos por
    /// valor y a los pares «−/+»: un solo control que se arrastra, se pulsa en cualquier punto de la pista, y con el
    /// teclado se mueve con las flechas. El valor elegido se lee a la derecha, en mono.
    /// </summary>
    public sealed partial class UiKit {
        public const float AltoDePista = 8, LadoDelTirador = 22;

        /// <param name="paso">0 = continuo; si no, el valor salta de paso en paso.</param>
        /// <param name="formato">Como se escribe el valor a la derecha («70 %», «×1.2»).</param>
        public Slider Deslizador(Transform padre, string titulo, float valor, float min, float max, float paso,
                                 Action<float> alCambiar, Func<float, string> formato) {
            var bloque = Columna(padre, "Deslizador " + titulo, Tema.Espacio(1));
            Texto(bloque, titulo, EstiloTexto.Leyenda, Tema.inkMuted);
            var fila = Fila(bloque, "Fila", Tema.Espacio(3));

            var raiz = Nodo(fila, "Pista");
            Tamano(raiz, alto: LadoDelTirador + 6, flexAncho: 1);

            // La pista: una linea fina, y encima el tramo ya elegido en cian.
            var fondo = Nodo(raiz, "Fondo");
            fondo.anchorMin = new Vector2(0, 0.5f);
            fondo.anchorMax = new Vector2(1, 0.5f);
            fondo.sizeDelta = new Vector2(0, AltoDePista);
            Fondo(fondo.gameObject.AddComponent<Image>(), Tema.line, AltoDePista / 2);

            var zonaRelleno = Nodo(raiz, "Zona del relleno");
            zonaRelleno.anchorMin = new Vector2(0, 0.5f);
            zonaRelleno.anchorMax = new Vector2(1, 0.5f);
            zonaRelleno.sizeDelta = new Vector2(0, AltoDePista);
            var relleno = Nodo(zonaRelleno, "Relleno");
            relleno.sizeDelta = Vector2.zero;
            Fondo(relleno.gameObject.AddComponent<Image>(), Tema.cyan, AltoDePista / 2);

            // El tirador: un circulo claro con borde cian, un poco mas grande que la pista para que se vea que se agarra.
            var zonaTirador = Nodo(raiz, "Zona del tirador");
            zonaTirador.anchorMin = Vector2.zero;
            zonaTirador.anchorMax = Vector2.one;
            zonaTirador.offsetMin = new Vector2(LadoDelTirador / 2, 0);
            zonaTirador.offsetMax = new Vector2(-LadoDelTirador / 2, 0);
            var tirador = Nodo(zonaTirador, "Tirador");
            tirador.sizeDelta = new Vector2(LadoDelTirador, LadoDelTirador);
            var imgTirador = tirador.gameObject.AddComponent<Image>();
            imgTirador.sprite = SpriteCircular();
            imgTirador.color = Tema.ink;
            var anillo = Nodo(tirador, "Anillo");
            Rellenar(anillo, 3);
            var imgAnillo = anillo.gameObject.AddComponent<Image>();
            imgAnillo.sprite = SpriteCircular();
            imgAnillo.color = Tema.cyan;
            imgAnillo.raycastTarget = false;

            var slider = raiz.gameObject.AddComponent<Slider>();
            slider.fillRect = relleno;
            slider.handleRect = tirador;
            slider.targetGraphic = imgTirador;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            var colores = slider.colors;
            colores.highlightedColor = Color.white;
            colores.pressedColor = Tema.cyan;
            slider.colors = colores;

            var texto = Texto(fila, formato(valor), EstiloTexto.Mono, Tema.ink, TextAlignmentOptions.MidlineRight);
            Tamano(texto, ancho: 90);

            slider.SetValueWithoutNotify(Mathf.Clamp(valor, min, max));
            slider.onValueChanged.AddListener(v => {
                if (paso > 0) {
                    var redondeado = Mathf.Clamp(min + Mathf.Round((v - min) / paso) * paso, min, max);
                    if (Mathf.Abs(redondeado - v) > 0.0001f) { slider.SetValueWithoutNotify(redondeado); v = redondeado; }
                }
                texto.text = formato(v);
                alCambiar?.Invoke(v);
            });
            return slider;
        }
    }
}
