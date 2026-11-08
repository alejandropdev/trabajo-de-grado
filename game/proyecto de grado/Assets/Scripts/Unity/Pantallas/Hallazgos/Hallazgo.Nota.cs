using System.Collections.Generic;
using Nexus.Core.Coleccion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public static partial class Hallazgos {
        private static readonly Color PapelPostIt = new Color32(0xF6, 0xE2, 0x7F, 0xFF);
        private static readonly Color TintaPostIt = new Color32(0x3A, 0x2E, 0x12, 0xFF);

        /// <summary>
        /// Una nota adhesiva, leida como una charla: una ventana de mensajeria con quien la escribe (cada nota, alguien
        /// distinto: «Anónimo», «Número oculto», «Grupo · Cafetería»…), «escribiendo…» con tres puntos que laten, y cada
        /// frase llega en su burbuja con su hora y su doble marca de leido. Al lado, el post-it de verdad, pegado con
        /// cinta, dice donde estaba.
        /// </summary>
        public static void Nota(ContextoDeHallazgo x) {
            var col = x.Col;
            var tema = x.Tema;
            var e = x.Escenario;
            var lectura = LecturaDeNota.Partir(col.Texto);
            string remitente;
            if (!EmblemasDeColeccionables.RemitentesDeNotas.TryGetValue(col.Id, out remitente)) remitente = "Alguien";
            if (!string.IsNullOrEmpty(lectura.Autor)) remitente = lectura.Autor;
            var semilla = Mathf.Abs(col.Id.GetHashCode());
            var paleta = new[] { tema.violet, tema.cyan, tema.warning, tema.success, tema.danger, PlanoIlustrado.ColorDelDestello };
            var colorAvatar = paleta[semilla % paleta.Length];
            var anonimo = remitente.StartsWith("Anónimo") || remitente.StartsWith("Número") || remitente.StartsWith("Usuario");

            // ---- la ventana de la charla
            var ventana = x.Caja(e, "Charla", -150, 0, 600, 680, new Color32(0x12, 0x18, 0x21, 0xFF), 22);
            x.Ui.Borde((RectTransform)ventana.transform, new Color32(0x2A, 0x35, 0x42, 0xFF), 22, 1.5f);
            ventana.gameObject.AddComponent<RectMask2D>();
            var v = ventana.transform;
            x.Caja(v, "Cabecera", 0, 300, 600, 80, new Color32(0x18, 0x21, 0x2C, 0xFF));
            var avatar = x.Circulo(v, "Avatar", -240, 300, 52, NexusTheme.Alfa(colorAvatar, anonimo ? 0.25f : 0.9f));
            x.Texto(avatar.transform, anonimo ? "?" : remitente.Substring(0, 1).ToUpperInvariant(), EstiloTexto.Subtitulo, anonimo ? colorAvatar : tema.bg950, 0, 0, 52, 52);
            x.Texto(v, remitente, EstiloTexto.Subtitulo, tema.ink, -40, 312, 340, 30, TextAlignmentOptions.Left);
            var estado = x.Texto(v, "en línea", EstiloTexto.Pequeno, tema.success, -40, 284, 340, 24, TextAlignmentOptions.Left);

            // ---- las burbujas
            var hora = 21 + semilla % 3;
            var minuto = 10 + semilla % 40;
            var y = 236f;
            var burbujas = new List<RectTransform>();
            var grupos = new List<CanvasGroup>();
            foreach (var frase in lectura.Frases) {
                var texto = x.Texto(v, frase, EstiloTexto.Cuerpo, tema.ink, 0, 0, 420, 40, TextAlignmentOptions.TopLeft);
                var alto = Mathf.Max(30, texto.GetPreferredValues(frase, 400, 0).y);
                var burbuja = x.Caja(v, "Burbuja", -270 + 230, y - (alto + 44) / 2, 460, alto + 44, new Color32(0x22, 0x2D, 0x3A, 0xFF), 16);
                var rb = (RectTransform)burbuja.transform;
                rb.pivot = new Vector2(0, 1);
                rb.anchoredPosition = new Vector2(-270, y);
                texto.rectTransform.SetParent(rb, false);
                texto.rectTransform.anchorMin = texto.rectTransform.anchorMax = texto.rectTransform.pivot = new Vector2(0, 1);
                texto.rectTransform.anchoredPosition = new Vector2(18, -12);
                texto.rectTransform.sizeDelta = new Vector2(420, alto + 4);
                var pie = x.Texto(rb, $"{hora:00}:{minuto + burbujas.Count:00}", EstiloTexto.Pequeno, tema.inkMuted, 0, 0, 90, 20, TextAlignmentOptions.Right);
                pie.fontSize *= 0.8f;
                pie.rectTransform.anchorMin = pie.rectTransform.anchorMax = pie.rectTransform.pivot = new Vector2(1, 0);
                pie.rectTransform.anchoredPosition = new Vector2(-40, 8);
                // La doble marca de leido, dibujada.
                DibujoUI dLeido;
                var leido = x.Lamina(rb, 0, 0, 30, 16, out dLeido);
                var rl = (RectTransform)dLeido.transform.parent;
                rl.anchorMin = rl.anchorMax = rl.pivot = new Vector2(1, 0);
                rl.anchoredPosition = new Vector2(-8, 10);
                leido.Trazo(tema.cyan, 2, 8, 7, 13, 15, 3);
                leido.Trazo(tema.cyan, 11, 10, 14, 13, 22, 3);
                burbujas.Add(rb);
                grupos.Add(x.Grupo(burbuja));
                y -= alto + 44 + 14;
            }

            // Los tres puntos de «escribiendo».
            var escribiendo = x.Caja(v, "Escribiendo", -220, 0, 90, 40, new Color32(0x22, 0x2D, 0x3A, 0xFF), 16);
            var puntos = new Image[3];
            for (var i = 0; i < 3; i++) puntos[i] = x.Circulo(escribiendo.transform, "Punto", -22 + i * 22, 0, 10, tema.inkMuted);
            var gEscribiendo = x.Grupo(escribiendo);

            // ---- el post-it de verdad, pegado con cinta
            var postit = x.Caja(e, "Post-it", 430, 110, 280, 260, PapelPostIt);
            postit.transform.localEulerAngles = new Vector3(0, 0, -4);
            x.Caja(postit.transform, "Sombra", 6, -134, 270, 10, new Color(0, 0, 0, 0.18f));
            var cinta = x.Caja(postit.transform, "Cinta", 0, 128, 120, 30, new Color(1, 1, 1, 0.45f));
            cinta.transform.localEulerAngles = new Vector3(0, 0, 3);
            var encontrado = x.Texto(postit.transform, "Estaba aquí:", EstiloTexto.Cuerpo, TintaPostIt, 0, 76, 240, 34);
            encontrado.font = x.Ui.FuenteTiza;
            var donde = x.Texto(postit.transform, col.Ubicacion ?? "", EstiloTexto.Cuerpo, TintaPostIt, 0, -20, 240, 150);
            donde.font = x.Ui.FuenteTiza;
            donde.enableAutoSizing = true;
            donde.fontSizeMin = 14;
            donde.fontSizeMax = donde.fontSize * 1.1f;
            var gPostit = x.Grupo(postit);
            x.Texto(e, col.Titulo, EstiloTexto.Subtitulo, PlanoIlustrado.ColorDelDestello, 430, -110, 320, 60).fontStyle = FontStyles.Italic;

            // ---- el ritmo: el post-it cae, la ventana entra, y por cada frase «escribiendo…» y su burbuja
            var s = x.Sec;
            var gVentana = x.Grupo(ventana);
            s.En(0f, 0.35f, p => { gVentana.alpha = p; ((RectTransform)ventana.transform).anchoredPosition = new Vector2(-150, Mathf.Lerp(-30, 0, ContextoDeHallazgo.Suave(p))); });
            s.En(0.2f, 0.45f, p => {
                gPostit.alpha = p;
                postit.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, ContextoDeHallazgo.Suave(p));
                postit.transform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-14, -4, ContextoDeHallazgo.Suave(p)));
            });
            var t0 = 0.8f;
            for (var i = 0; i < burbujas.Count; i++) {
                var k = i;
                var rb = burbujas[k];
                var espera = 0.6f + Mathf.Min(1.0f, lectura.Frases[k].Length / 90f);
                s.En(t0, espera, p => {
                    var activo = p > 0 && p < 1;
                    if (activo) {
                        gEscribiendo.alpha = 1;
                        ((RectTransform)escribiendo.transform).anchoredPosition = new Vector2(-220, rb.anchoredPosition.y - 20);
                        estado.text = "escribiendo...";
                    } else if (p >= 1) {
                        gEscribiendo.alpha = 0;
                        estado.text = "en línea";
                    }
                });
                s.En(t0 + espera, 0.3f, p => {
                    grupos[k].alpha = p;
                    rb.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, ContextoDeHallazgo.Rebote(p));
                });
                t0 += espera + 0.35f;
            }
            s.Siempre(t => {
                for (var i = 0; i < 3; i++) {
                    var a = 0.4f + 0.6f * Mathf.Max(0, Mathf.Sin(t * 6f - i * 0.9f));
                    puntos[i].color = NexusTheme.Alfa(tema.inkMuted, a);
                }
            });
        }
    }
}
