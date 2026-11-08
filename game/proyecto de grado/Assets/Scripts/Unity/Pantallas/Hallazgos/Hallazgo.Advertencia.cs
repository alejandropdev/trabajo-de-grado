using System.Collections.Generic;
using Nexus.Core.Coleccion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Carteles = Nexus.Core.Coleccion.EmblemasDeColeccionables.Carteles;

namespace Nexus.Unity.Pantallas {
    public static partial class Hallazgos {
        private static readonly Color AmarilloObra = new Color32(0xF2, 0xC2, 0x30, 0xFF);
        private static readonly Color NegroObra = new Color32(0x14, 0x14, 0x14, 0xFF);
        private static readonly Color RojoEsmalte = new Color32(0xC6, 0x28, 0x28, 0xFF);
        private static readonly Color BlancoEsmalte = new Color32(0xF4, 0xF1, 0xEA, 0xFF);
        private static readonly Color NaranjaLab = new Color32(0xF5, 0x7C, 0x00, 0xFF);

        /// <summary>
        /// Una advertencia, con la sensacion de una alarma: los bordes laten en rojo, dos cintas de obra cruzan la escena,
        /// y el cartel CAE con un golpe (la pantalla tiembla y destella). Cada advertencia tiene su cartel y su icono
        /// (EmblemasDeColeccionables.DeAdvertencias): placa esmaltada, alto voltaje, etiqueta de laboratorio, cinta de
        /// obra o un aviso a mano pegado con cinta. Despues, el cartel queda colgando y se balancea.
        /// </summary>
        public static void Advertencia(ContextoDeHallazgo x) {
            var col = x.Col;
            var tema = x.Tema;
            var e = x.Escenario;
            KeyValuePair<string, string> variante;
            if (!EmblemasDeColeccionables.DeAdvertencias.TryGetValue(col.Id, out variante))
                variante = new KeyValuePair<string, string>(Carteles.Esmalte, "sirena");
            var cartel = variante.Key;
            var colorAlarma = cartel == Carteles.AltoVoltaje || cartel == Carteles.Obra ? AmarilloObra : tema.danger;

            // ---- los bordes que laten
            var bordes = new List<Image> {
                x.Caja(e, "Borde", 0, 470, 2200, 140, colorAlarma), x.Caja(e, "Borde", 0, -470, 2200, 140, colorAlarma),
                x.Caja(e, "Borde", -900, 0, 140, 1400, colorAlarma), x.Caja(e, "Borde", 900, 0, 140, 1400, colorAlarma)
            };
            foreach (var b in bordes) { b.sprite = PlanoIlustrado.Brillo(); b.type = Image.Type.Simple; }

            // ---- las cintas de obra
            var cintaArriba = Cinta(x, e, 300, 3);
            var cintaAbajo = Cinta(x, e, -300, -2);

            // ---- el cartel, segun su tipo
            var sostiene = x.Nodo(e, "Cartel", 0, 10, 780, 440);
            var colorIcono = NegroObra;
            Transform zonaIcono;
            TMP_Text titulo, texto;
            switch (cartel) {
                case Carteles.AltoVoltaje: {
                    var placa = x.Caja(sostiene, "Placa", 0, 0, 780, 420, NegroObra, 18);
                    x.Ui.Borde((RectTransform)placa.transform, AmarilloObra, 18, 6);
                    DibujoUI dTri;
                    var tri = x.Lamina(placa.transform, -250, 20, 220, 200, out dTri);
                    dTri.Poligono(new List<Vector2> { new Vector2(110, 6), new Vector2(214, 194), new Vector2(6, 194) }, AmarilloObra);
                    tri.Trazo(NegroObra, 110, 20, 200, 186, 20, 186, 110, 20);
                    zonaIcono = x.Nodo(placa.transform, "Icono", -250, 0, 120, 120);
                    colorIcono = NegroObra;
                    titulo = x.Texto(placa.transform, col.Titulo, EstiloTexto.Titulo, AmarilloObra, 120, 60, 470, 170);
                    texto = x.Texto(placa.transform, col.Texto, EstiloTexto.Cuerpo, new Color32(0xE8, 0xE2, 0xC8, 0xFF), 120, -110, 470, 120);
                    break;
                }
                case Carteles.Laboratorio: {
                    var placa = x.Caja(sostiene, "Etiqueta", 0, 0, 780, 420, BlancoEsmalte, 10);
                    var banda = x.Caja(placa.transform, "Banda", 0, 172, 780, 76, NaranjaLab);
                    x.Texto(banda.transform, "PRECAUCIÓN", EstiloTexto.Subtitulo, Color.white, 0, 0, 700, 60).characterSpacing = 14;
                    var rombo = x.Caja(placa.transform, "Rombo", -260, -50, 170, 170, NaranjaLab);
                    rombo.transform.localEulerAngles = new Vector3(0, 0, 45);
                    x.Ui.Borde((RectTransform)rombo.transform, NegroObra, 6, 5);
                    zonaIcono = x.Nodo(placa.transform, "Icono", -260, -50, 120, 120);
                    colorIcono = NegroObra;
                    titulo = x.Texto(placa.transform, col.Titulo, EstiloTexto.Titulo, NegroObra, 110, 10, 480, 160);
                    texto = x.Texto(placa.transform, col.Texto, EstiloTexto.Cuerpo, new Color32(0x3A, 0x3A, 0x3A, 0xFF), 110, -140, 480, 100);
                    break;
                }
                case Carteles.Obra: {
                    var placa = x.Caja(sostiene, "Valla", 0, 0, 780, 420, AmarilloObra, 6);
                    DibujoUI dRayas;
                    var rayas = x.Lamina(placa.transform, 0, 0, 780, 420, out dRayas);
                    for (var k = -20; k < 60; k++) {
                        var x0 = k * 30f;
                        // Rayas negras solo en el marco: arriba y abajo.
                        rayas.Linea(x0, 0, x0 + 30, 34, NegroObra, 12);
                        rayas.Linea(x0, 386, x0 + 30, 420, NegroObra, 12);
                    }
                    zonaIcono = x.Nodo(placa.transform, "Icono", -280, 0, 120, 120);
                    colorIcono = NegroObra;
                    titulo = x.Texto(placa.transform, col.Titulo, EstiloTexto.Titulo, NegroObra, 90, 40, 540, 170);
                    texto = x.Texto(placa.transform, col.Texto, EstiloTexto.Cuerpo, NegroObra, 90, -110, 540, 100);
                    break;
                }
                case Carteles.PostIt: {
                    var placa = x.Caja(sostiene, "Aviso", 0, 0, 600, 420, PapelPostIt);
                    placa.transform.localEulerAngles = new Vector3(0, 0, -3);
                    var c1 = x.Caja(placa.transform, "Cinta", -250, 200, 130, 34, new Color(1, 1, 1, 0.5f));
                    c1.transform.localEulerAngles = new Vector3(0, 0, 32);
                    var c2 = x.Caja(placa.transform, "Cinta", 250, 200, 130, 34, new Color(1, 1, 1, 0.5f));
                    c2.transform.localEulerAngles = new Vector3(0, 0, -28);
                    zonaIcono = x.Nodo(placa.transform, "Icono", 0, 120, 120, 120);
                    colorIcono = RojoEsmalte;
                    titulo = x.Texto(placa.transform, col.Titulo, EstiloTexto.Titulo, RojoEsmalte, 0, -10, 540, 130);
                    titulo.font = x.Ui.FuenteTiza;
                    DibujoUI dSubrayado;
                    var subrayado = x.Lamina(placa.transform, 0, -80, 460, 20, out dSubrayado);
                    var ola = new List<float>();
                    for (var k = 0; k <= 23; k++) { ola.Add(k * 20); ola.Add(10 + Mathf.Sin(k * 1.3f) * 5); }
                    subrayado.Trazo(RojoEsmalte, ola.ToArray());
                    texto = x.Texto(placa.transform, col.Texto, EstiloTexto.Cuerpo, TintaPostIt, 0, -140, 520, 90);
                    texto.font = x.Ui.FuenteTiza;
                    break;
                }
                default: {   // la placa esmaltada, atornillada
                    var placa = x.Caja(sostiene, "Placa", 0, 0, 780, 420, BlancoEsmalte, 26);
                    x.Ui.Borde((RectTransform)placa.transform, RojoEsmalte, 26, 10);
                    var banda = x.Caja(placa.transform, "Banda", 0, 150, 700, 70, RojoEsmalte, 10);
                    x.Texto(banda.transform, "ATENCIÓN", EstiloTexto.Subtitulo, Color.white, 50, 0, 500, 56).characterSpacing = 12;
                    zonaIcono = x.Nodo(banda.transform, "Icono", -290, 0, 120, 120);
                    zonaIcono.localScale = Vector3.one * 0.5f;
                    colorIcono = Color.white;
                    foreach (var p in new[] { new Vector2(-360, 180), new Vector2(360, 180), new Vector2(-360, -180), new Vector2(360, -180) }) {
                        var tornillo = x.Circulo(placa.transform, "Tornillo", p.x, p.y, 16, new Color32(0x9A, 0x9A, 0x9A, 0xFF));
                        x.Caja(tornillo.transform, "Ranura", 0, 0, 12, 2, new Color32(0x55, 0x55, 0x55, 0xFF)).transform.localEulerAngles = new Vector3(0, 0, 35);
                    }
                    titulo = x.Texto(placa.transform, col.Titulo, EstiloTexto.Titulo, RojoEsmalte, 0, 20, 680, 150);
                    texto = x.Texto(placa.transform, col.Texto, EstiloTexto.Cuerpo, new Color32(0x2A, 0x2A, 0x2A, 0xFF), 0, -120, 660, 90);
                    break;
                }
            }
            titulo.fontStyle = FontStyles.Bold;
            titulo.characterSpacing = 2;
            titulo.enableAutoSizing = true;
            titulo.fontSizeMin = 24;
            titulo.fontSizeMax = titulo.fontSize * 1.25f;
            texto.enableAutoSizing = true;
            texto.fontSizeMin = 14;
            texto.fontSizeMax = texto.fontSize;
            var gTexto = x.Grupo(texto);

            // El icono propio de esta advertencia, dibujado.
            DibujoUI dIcono;
            var icono = x.Lamina(zonaIcono, 0, 0, 120, 120, out dIcono);
            System.Action<Tizador, Color> dibujar;
            if (!DibujosDeHallazgos.IconosDeAdvertencia.TryGetValue(variante.Value, out dibujar)) dibujar = DibujosDeHallazgos.IconosDeAdvertencia["sirena"];
            dibujar(icono, colorIcono);

            // El destello del golpe.
            var destello = x.Caja(e, "Destello", 0, 0, 2400, 1400, Color.white);

            // ---- el ritmo
            var s = x.Sec;
            var rc1 = (RectTransform)cintaArriba.transform;
            var rc2 = (RectTransform)cintaAbajo.transform;
            s.En(0f, 0.5f, p => {
                rc1.anchoredPosition = new Vector2(Mathf.Lerp(-1800, 0, ContextoDeHallazgo.Suave(p)), 300);
                rc2.anchoredPosition = new Vector2(Mathf.Lerp(1800, 0, ContextoDeHallazgo.Suave(p)), -300);
            });
            s.En(0.4f, 0.35f, p => sostiene.anchoredPosition = new Vector2(0, Mathf.Lerp(820, 10, ContextoDeHallazgo.Caida(p))));
            s.En(0.75f, 0.35f, p => {
                if (p > 0 && p < 0.2f) x.Sacudir(18);
                destello.color = new Color(1, 1, 1, p <= 0 ? 0 : 0.55f * (1 - p));
            });
            s.En(0.9f, 0.9f, p => dIcono.Avance = p);
            s.En(1.3f, 0.4f, p => gTexto.alpha = p);
            s.Siempre(t => {
                // Los bordes laten como una alarma, y las cintas se deslizan.
                var latido = 0.12f + 0.2f * Mathf.Max(0, Mathf.Sin(t * 5.5f));
                foreach (var b in bordes) b.color = NexusTheme.Alfa(colorAlarma, latido);
                // Colgado de dos puntos, el cartel se balancea cada vez menos tras el golpe.
                if (t > 1.1f) sostiene.localEulerAngles = new Vector3(0, 0, Mathf.Sin((t - 1.1f) * 3.2f) * 2.2f * Mathf.Exp(-(t - 1.1f) * 0.5f));
            });
        }

        /// <summary>Una cinta de obra amarilla y negra con «PRECAUCIÓN», ligeramente inclinada, que se desliza.</summary>
        private static Image Cinta(ContextoDeHallazgo x, RectTransform padre, float y, float angulo) {
            var cinta = x.Caja(padre, "Cinta", 0, y, 1900, 54, AmarilloObra);
            cinta.transform.localEulerAngles = new Vector3(0, 0, angulo);
            cinta.gameObject.AddComponent<RectMask2D>();
            var tira = x.Nodo(cinta.transform, "Tira", 0, 0, 3800, 54);
            for (var k = 0; k < 120; k++) {
                var raya = x.Caja(tira, "Raya", -1900 + k * 34, 0, 14, 90, NegroObra);
                raya.transform.localEulerAngles = new Vector3(0, 0, 40);
            }
            for (var k = 0; k < 10; k++) {
                var cartela = x.Caja(tira, "Rotulo", -1700 + k * 380, 0, 210, 36, AmarilloObra);
                x.Texto(cartela.transform, "PRECAUCIÓN", EstiloTexto.Leyenda, NegroObra, 0, 0, 210, 36).characterSpacing = 6;
            }
            x.Sec.Siempre(t => tira.anchoredPosition = new Vector2(-Mathf.Repeat(t * 60f * Mathf.Sign(angulo + 0.01f), 380f), 0));
            return cinta;
        }
    }
}
