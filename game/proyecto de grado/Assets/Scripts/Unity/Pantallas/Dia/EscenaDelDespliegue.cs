using System.Collections.Generic;
using Nexus.Core.Jornada;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Se acabaron los dias» (2026-11-07): lo construido sale hacia produccion. Sobre un fondo de plano, a la izquierda
    /// la pila de cajas (las tarjetas hechas); en medio la tuberia con sus etapas, por la que un paquete viaja en bucle y
    /// va encendiendo cada etapa al pasar; a la derecha el destino: el rack de produccion con sus luces (Fase 3) o la
    /// pantalla del cliente con sus barras (Fase 4). Al llegar, un destello.
    ///
    /// Al mirar la carta («Listo»), todo se acelera: el paquete va mas deprisa, las luces pasan a verde y el destino
    /// brilla. Entra en ~220 ms con ease-out (emil-design). Lienzo de 760×230, escalado para caber.
    /// </summary>
    public sealed class EscenaDelDespliegue : MonoBehaviour {
        private const float Ancho = 760, Alto = 230;
        private const float LineaY = 108, DesdeX = 210, DestinoX = 650;

        public bool Listo;

        private RectTransform _lienzo, _paquete, _avance, _destello;
        private Image _brilloDelPaquete, _brilloDelDestino, _imgDestello;
        private readonly List<Image> _etapas = new List<Image>();
        private readonly List<float> _xs = new List<float>();
        private readonly List<Image> _luces = new List<Image>();
        private NexusTheme _tema;
        private bool _alCliente;
        private float _t, _reloj, _listo, _destelloT = 9;
        private float _recorridoAnterior;

        public static EscenaDelDespliegue Crear(UiKit ui, Transform padre, int hechas, bool alCliente, string[] etapas) {
            var hueco = ui.Nodo(padre, "Escena del despliegue");
            var le = UiKit.Tamano(hueco, alto: 140, flexAncho: 1, flexAlto: 1);
            le.preferredHeight = Alto;
            var e = hueco.gameObject.AddComponent<EscenaDelDespliegue>();
            e.Construir(ui, hechas, alCliente, etapas);
            return e;
        }

        private void Construir(UiKit ui, int hechas, bool alCliente, string[] etapas) {
            var tema = _tema = ui.Tema;
            _alCliente = alCliente;
            _lienzo = ui.Nodo(transform, "Lienzo");
            _lienzo.anchorMin = _lienzo.anchorMax = _lienzo.pivot = new Vector2(0.5f, 0.5f);
            _lienzo.sizeDelta = new Vector2(Ancho, Alto);

            // ---- el plano: una cuadricula muy tenue dentro de un marco
            var plano = VentanaDelAtardecer.Capa(ui, _lienzo, "Plano");
            plano.Rect(4, 4, Ancho - 8, Alto - 8, tema.line, 2, NexusTheme.Alfa(tema.surfaceSunken, 0.6f), false, 12);
            for (var x = 42f; x < Ancho - 10; x += 38) plano.Linea(x, 8, x, Alto - 8, NexusTheme.Alfa(tema.cyan, 0.04f), 1);
            for (var y = 42f; y < Alto - 10; y += 38) plano.Linea(8, y, Ancho - 8, y, NexusTheme.Alfa(tema.cyan, 0.04f), 1);

            // ---- la pila de lo construido
            var pila = VentanaDelAtardecer.Capa(ui, _lienzo, "Pila");
            var filas = EscenaDelCierre.FilasDeLaPila(hechas, 5);
            const float caja = 26, separacion = 4, suelo = 176;
            for (var f = 0; f < filas.Length; f++) {
                var ancho = filas[f] * caja + (filas[f] - 1) * separacion;
                var x0 = 30 + (5 * caja + 4 * separacion - ancho) / 2;
                var y = suelo - (f + 1) * (caja - 4);
                for (var c = 0; c < filas[f]; c++) {
                    var x = x0 + c * (caja + separacion);
                    pila.Rect(x, y, caja, caja - 6, NexusTheme.Alfa(tema.cyan, 0.55f), 2, tema.surfaceRaised, false, 3);
                    pila.Linea(x + caja / 2, y + 1, x + caja / 2, y + caja - 7, NexusTheme.Alfa(tema.cyan, 0.3f), 2);
                }
            }
            pila.Linea(22, suelo, 178, suelo, tema.lineStrong, 3);
            Texto(ui, 20, suelo + 8, 170, 20, hechas == 1 ? "1 tarjeta hecha" : $"{hechas} tarjetas hechas",
                  14, tema.inkMuted, TextAlignmentOptions.Center);

            // ---- la tuberia: la linea de fondo, lo recorrido (cian) y las etapas
            var tubo = VentanaDelAtardecer.Capa(ui, _lienzo, "Tubo");
            tubo.Linea(DesdeX, LineaY, DestinoX - 40, LineaY, tema.lineStrong, 3, true);
            _avance = VentanaDelAtardecer.Colocado(ui, _lienzo, "Recorrido", DesdeX, LineaY - 2, 0, 4);
            VentanaDelAtardecer.Imagen(_avance, null, NexusTheme.Alfa(tema.cyan, 0.8f));

            var n = etapas.Length;
            for (var i = 0; i < n - 1; i++) {
                var x = Mathf.Lerp(DesdeX + 20, DestinoX - 70, n <= 2 ? 0.5f : i / (float)(n - 2));
                _xs.Add(x);
                var anillo = VentanaDelAtardecer.Capa(ui, _lienzo, "Etapa");
                anillo.Circulo(x, LineaY, 13, tema.lineStrong, 3, tema.surfaceSunken);
                var luz = VentanaDelAtardecer.Colocado(ui, _lienzo, "Luz", x - 7, LineaY - 7, 14, 14);
                _etapas.Add(VentanaDelAtardecer.Imagen(luz, ui.SpriteCircular(), NexusTheme.Alfa(tema.inkFaint, 0.5f)));
                Texto(ui, x - 60, LineaY + 22, 120, 20, etapas[i], 14, tema.inkMuted, TextAlignmentOptions.Center);
            }

            // ---- el destino
            _brilloDelDestino = VentanaDelAtardecer.Imagen(
                VentanaDelAtardecer.Colocado(ui, _lienzo, "Brillo del destino", DestinoX - 110, LineaY - 110, 220, 220),
                PlanoIlustrado.Brillo(), NexusTheme.Alfa(tema.cyan, 0.1f));
            var destino = VentanaDelAtardecer.Capa(ui, _lienzo, "Destino");
            if (!alCliente) {
                // El rack de produccion: cinco bandejas con tres luces cada una.
                destino.Rect(DestinoX - 36, 36, 72, 150, tema.lineStrong, 3, tema.surfaceSunken, false, 6);
                for (var b = 0; b < 5; b++) {
                    var y = 46 + b * 27;
                    destino.Rect(DestinoX - 28, y, 56, 20, tema.line, 2, tema.surfaceRaised, false, 3);
                    for (var k = 0; k < 3; k++) {
                        var led = VentanaDelAtardecer.Colocado(ui, _lienzo, "Led", DestinoX - 20 + k * 9, y + 7, 6, 6);
                        _luces.Add(VentanaDelAtardecer.Imagen(led, ui.SpriteCircular(), tema.cyan));
                    }
                }
            } else {
                // La pantalla del cliente: un monitor con cinco barras que crecen cuando llega lo entregado.
                destino.Rect(DestinoX - 62, 48, 124, 86, tema.lineStrong, 3, tema.surfaceSunken, false, 8);
                destino.Linea(DestinoX, 134, DestinoX, 158, tema.lineStrong, 5);
                destino.Linea(DestinoX - 26, 160, DestinoX + 26, 160, tema.lineStrong, 5);
                for (var k = 0; k < 5; k++) {
                    var barra = VentanaDelAtardecer.Colocado(ui, _lienzo, "Barra", 0, 0, 12, 40);
                    barra.pivot = new Vector2(0.5f, 0);
                    barra.anchoredPosition = new Vector2(DestinoX - 40 + k * 19, -122);
                    _luces.Add(VentanaDelAtardecer.Imagen(barra, null, tema.cyan));
                }
            }
            Texto(ui, DestinoX - 80, alCliente ? 168 : 192, 160, 22, etapas[n - 1].ToUpperInvariant(), 13, tema.cyan,
                  TextAlignmentOptions.Center).characterSpacing = 3;

            // ---- el destello de llegada y el paquete
            _destello = VentanaDelAtardecer.Colocado(ui, _lienzo, "Destello", 0, 0, 80, 80);
            _destello.pivot = new Vector2(0.5f, 0.5f);
            _destello.anchoredPosition = new Vector2(DestinoX, -LineaY);
            _imgDestello = VentanaDelAtardecer.Imagen(_destello, PlanoIlustrado.Brillo(), NexusTheme.Alfa(tema.ink, 0));

            _paquete = ui.Nodo(_lienzo, "Paquete");
            _paquete.anchorMin = _paquete.anchorMax = new Vector2(0, 1);
            _paquete.pivot = new Vector2(0.5f, 0.5f);
            _paquete.sizeDelta = new Vector2(18, 14);
            var halo = ui.Nodo(_paquete, "Halo");
            halo.anchorMin = halo.anchorMax = halo.pivot = new Vector2(0.5f, 0.5f);
            halo.sizeDelta = new Vector2(70, 70);
            _brilloDelPaquete = VentanaDelAtardecer.Imagen(halo, PlanoIlustrado.Brillo(), NexusTheme.Alfa(tema.cyan, 0.5f));
            var cuerpo = ui.Nodo(_paquete, "Cuerpo");
            UiKit.Rellenar(cuerpo);
            ui.Fondo(VentanaDelAtardecer.Imagen(cuerpo, null, tema.cyan), tema.cyan, NexusTheme.RadioXs);

            Ajustar();
        }

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            _t += dt;
            _listo = Mathf.Lerp(_listo, Listo ? 1 : 0, 1 - Mathf.Exp(-(Listo ? 14f : 20f) * dt));
            // El reloj de la tuberia corre mas deprisa cuando se mira la carta.
            _reloj += dt * (1 + 1.4f * _listo);
            Ajustar();

            var r = EscenaDelCierre.Recorrido(_reloj);
            var x = Mathf.Lerp(DesdeX, DestinoX - 40, r);
            _paquete.anchoredPosition = new Vector2(x, -LineaY);
            _paquete.gameObject.SetActive(r < 1);
            _avance.sizeDelta = new Vector2(x - DesdeX, 4);
            _brilloDelPaquete.color = NexusTheme.Alfa(_tema.cyan, 0.45f + 0.35f * _listo);

            // Cada etapa se enciende cuando el paquete la pasa.
            for (var i = 0; i < _etapas.Count; i++)
                _etapas[i].color = x >= _xs[i] ? _tema.cyan : NexusTheme.Alfa(_tema.inkFaint, 0.5f);

            // Al llegar: un destello que crece y se apaga (~450 ms, ease-out).
            if (r >= 1 && _recorridoAnterior < 1) _destelloT = 0;
            _recorridoAnterior = r;
            _destelloT += dt;
            var p = Mathf.Clamp01(_destelloT / 0.45f);
            _destello.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.2f, 1 - Mathf.Pow(1 - p, 3));
            _imgDestello.color = NexusTheme.Alfa(_tema.ink, 0.5f * (1 - p));
            _brilloDelDestino.color = NexusTheme.Alfa(Color.Lerp(_tema.cyan, _tema.success, _listo), 0.1f + 0.25f * _listo + 0.2f * (1 - p));

            // El destino: el rack parpadea (verde si esta listo); en el monitor, las barras respiran y crecen al llegar.
            for (var i = 0; i < _luces.Count; i++) {
                if (!_alCliente) {
                    var encendida = Mathf.Repeat(_t * (0.9f + (i * 37 % 11) * 0.13f) + i * 0.31f, 1f) < 0.65f;
                    var c = Color.Lerp(i % 4 == 3 ? _tema.warning : _tema.cyan, _tema.success, _listo);
                    _luces[i].color = NexusTheme.Alfa(c, encendida ? 0.95f : 0.2f);
                } else {
                    var alto = 18 + 40 * Mathf.Clamp01(0.35f + 0.25f * Mathf.Sin(_t * 1.3f + i * 1.1f) + 0.4f * (1 - p) + 0.2f * _listo);
                    _luces[i].rectTransform.sizeDelta = new Vector2(12, alto);
                    _luces[i].color = Color.Lerp(_tema.cyan, _tema.success, _listo);
                }
            }
        }

        private void Ajustar() {
            var rect = ((RectTransform)transform).rect;
            if (rect.width < 1 || rect.height < 1) return;
            var s = Mathf.Min(rect.width / Ancho, rect.height / Alto, 1.15f);
            _lienzo.localScale = new Vector3(s, s, 1);
        }

        private TMP_Text Texto(UiKit ui, float x, float y, float w, float h, string texto, float tam, Color color, TextAlignmentOptions alineacion) {
            var t = ui.Texto(_lienzo, texto, EstiloTexto.Pequeno, color, alineacion);
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            Lamina.Colocar(t.rectTransform, x, y, w, h);
            return t;
        }
    }
}
