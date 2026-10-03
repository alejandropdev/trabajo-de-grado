using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Narrativa;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El marco de pizarra que comparten la receta de los minijuegos, los conceptos («?») y los apuntes: velo,
    /// madera, pizarra y la nota pegada en la esquina. Devuelve la lamina de 1612×932 donde se dibuja con tiza.
    /// </summary>
    public static class MarcoDePizarra {
        public const float W = 1612, H = 932;

        public static RectTransform Construir(UiKit ui, RectTransform raiz, string nota, out Lamina lamina) {
            var velo = raiz.gameObject.AddComponent<Image>();
            velo.color = new Color(0, 0, 0, 0.66f);

            var marco = ui.Nodo(raiz, "Marco");
            marco.anchorMin = marco.anchorMax = marco.pivot = new Vector2(0.5f, 0.5f);
            marco.sizeDelta = new Vector2(W + 28, H + 28);
            var madera = marco.gameObject.AddComponent<Image>();
            madera.sprite = ui.SpriteRedondeado(); madera.type = Image.Type.Sliced;
            madera.color = new Color32(0x3A, 0x2E, 0x22, 0xFF);

            var tabla = ui.Nodo(marco, "Pizarra");
            UiKit.Rellenar(tabla, 14);
            tabla.gameObject.AddComponent<Image>().color = Tizador.Pizarra;

            lamina = new Lamina(ui, tabla, W, H);
            UiKit.Rellenar(lamina.Raiz);
            lamina.Dibujo.Tiza = true;

            var notaRt = ui.Nodo(marco, "Nota");
            notaRt.anchorMin = notaRt.anchorMax = new Vector2(1, 1);
            notaRt.pivot = new Vector2(0.5f, 0.5f);
            notaRt.anchoredPosition = new Vector2(-70, -50);
            notaRt.sizeDelta = new Vector2(230, 56);
            notaRt.localRotation = Quaternion.Euler(0, 0, -9);
            notaRt.gameObject.AddComponent<Image>().color = new Color32(0xE7, 0xEE, 0xF0, 0xFF);
            var tn = ui.Texto(notaRt, nota, EstiloTexto.Subtitulo, new Color32(0x2E, 0x4A, 0x52, 0xFF), TextAlignmentOptions.Center);
            tn.font = ui.FuenteTiza;
            UiKit.Rellenar((RectTransform)tn.transform);
            return marco;
        }

        /// <summary>Un boton pegado a una esquina del marco.</summary>
        public static Button Boton(UiKit ui, RectTransform marco, string texto, Action alPulsar, Vector2 ancla, Vector2 pos,
                                   float ancho = 220, VarianteBoton variante = VarianteBoton.Primario) {
            var b = ui.Boton(marco, texto, alPulsar, variante);
            var rb = (RectTransform)b.transform;
            rb.anchorMin = rb.anchorMax = rb.pivot = ancla;
            rb.anchoredPosition = pos;
            rb.sizeDelta = new Vector2(ancho, 60);
            return b;
        }

        /// <summary>Borra lo dibujado en la lamina para pintar otra pagina.</summary>
        public static void Limpiar(Lamina l) {
            for (var i = l.Raiz.childCount - 1; i >= 0; i--) {
                var hijo = l.Raiz.GetChild(i);
                if (hijo.gameObject != l.Dibujo.gameObject) UnityEngine.Object.Destroy(hijo.gameObject);
            }
            l.Dibujo.Limpiar();
        }
    }

    /// <summary>Los dibujos de tiza de las viñetas, cada uno en un cuadro de 160×130.</summary>
    public static class DibujosDePizarra {
        public static Color ColorDe(string nombre) {
            switch (nombre) {
                case "cian": return Tizador.Cian;
                case "mostaza": return Tizador.Mostaza;
                case "roja": return Tizador.Roja;
                case "gris": return Tizador.Gris;
                default: return Tizador.Tiza;
            }
        }

        public static void Dibujar(Tizador t, string icono, Color c) {
            switch (icono) {
                case "persona": t.Persona(80, 25, c); break;
                case "equipo": t.Persona(42, 35, c); t.Persona(80, 22, c); t.Persona(118, 35, c); break;
                case "caja": t.Caja(20, 35, 120, 60, null, c); break;
                case "base-de-datos": t.Cilindro(40, 20, 80, 90, c); break;
                case "servidor":
                    t.Caja(45, 10, 70, 110, null, c);
                    for (var i = 0; i < 3; i++) t.Linea(55, 38 + i * 28, 105, 38 + i * 28, c, 2);
                    break;
                case "documento":
                    t.Caja(45, 8, 72, 112, null, c);
                    for (var i = 0; i < 4; i++) t.Linea(56, 32 + i * 20, 106, 32 + i * 20, c, 2);
                    break;
                case "tablero":
                    t.Caja(8, 12, 144, 106, null, c);
                    t.Linea(56, 12, 56, 118, c, 2); t.Linea(104, 12, 104, 118, c, 2);
                    t.Caja(16, 30, 32, 20, null, Tizador.Cian); t.Caja(64, 30, 32, 20, null, Tizador.Mostaza); t.Caja(112, 30, 32, 20, null, Tizador.Cian);
                    t.Caja(16, 60, 32, 20, null, Tizador.Cian);
                    break;
                case "tarjeta": t.Caja(28, 38, 104, 54, "HU", c); break;
                case "pila": t.Pila(55, 12, 50, 110, 0.6f); break;
                case "commit":
                    t.Linea(80, 8, 80, 122, c); t.Nodo(80, 38, c); t.Nodo(80, 92, c); break;
                case "rama":
                    t.Linea(45, 8, 45, 122, c); t.Nodo(45, 30, c);
                    t.Curva(45, 45, 110, 45, 110, 90, Tizador.Mostaza, false); t.Nodo(110, 100, Tizador.Mostaza); break;
                case "bicho": t.Bicho(80, 68, Tizador.Roja); break;
                case "check": t.Check(80, 65, c == Tizador.Tiza ? Tizador.Cian : c); break;
                case "cruz": t.Cruz(80, 65, Tizador.Roja, 24); break;
                case "reloj": t.Reloj(80, 65); break;
                case "barra-subiendo":
                    for (var i = 0; i < 4; i++) t.Pila(20 + i * 32, 110 - 25 * (i + 1), 24, 25 * (i + 1), 1f);
                    break;
                case "barra-bajando":
                    for (var i = 0; i < 4; i++) t.Pila(20 + i * 32, 110 - 25 * (4 - i), 24, 25 * (4 - i), 1f);
                    break;
                case "mano": t.Mano(70, 55); break;
                case "candado":
                    t.Caja(50, 60, 60, 50, null, c); t.Curva(58, 60, 80, 10, 102, 60, c, false); break;
                case "flecha": t.Flecha(20, 65, 140, 65, c); break;
                default: t.Texto(80, 70, "?", 40, c, true); break;
            }
        }
    }

    /// <summary>
    /// Un concepto (o unos apuntes) explicado en un pizarron de varias paginas, al estilo de la receta de los
    /// minijuegos: dibujos de tiza y pocas palabras por pagina, para no cargarlo todo en una imagen. La ultima
    /// pagina suele contar donde se ve en Nexus. El reloj del dia espera mientras esta abierto.
    /// </summary>
    public sealed class PantallaDePizarra : Pantalla {
        public string Etiqueta = "CONCEPTO";
        public string Tema;
        public List<PaginaDePizarra> Paginas = new List<PaginaDePizarra>();

        public override bool EsModal { get { return true; } }
        public override bool PuedeVolver { get { return true; } }

        private Lamina _lamina;
        private int _pagina;
        private Button _anterior, _siguiente;
        private TMP_Text _contador;
        private bool _pausadoAntes;

        /// <summary>Abre el pizarron de un termino del glosario.</summary>
        public static void AbrirConcepto(AppRoot app, EntradaDeGlosario e) {
            if (e == null) return;
            app.Router.Apilar<PantallaDePizarra>(p => {
                p.Etiqueta = "¿QUÉ ES?";
                p.Tema = e.Tema;
                p.Paginas = Glosario.PaginasDe(e);
            });
        }

        protected override void Construir() {
            if (App.Runner != null) { _pausadoAntes = App.Runner.Pausado; App.Runner.Pausado = true; }
            var marco = MarcoDePizarra.Construir(Ui, Raiz, Etiqueta, out _lamina);
            _anterior = MarcoDePizarra.Boton(Ui, marco, "◄ Anterior", () => Ir(_pagina - 1), new Vector2(0, 0), new Vector2(52, 44), 220, VarianteBoton.Secundario);
            _siguiente = MarcoDePizarra.Boton(Ui, marco, "Siguiente ►", () => Ir(_pagina + 1), new Vector2(1, 0), new Vector2(-300, 44), 220);
            MarcoDePizarra.Boton(Ui, marco, "Cerrar", () => App.Router.Volver(), new Vector2(1, 0), new Vector2(-52, 44), 220, VarianteBoton.Secundario);
            var contador = Ui.Nodo(marco, "Contador");
            contador.anchorMin = contador.anchorMax = contador.pivot = new Vector2(0.5f, 0);
            contador.anchoredPosition = new Vector2(-120, 52);
            contador.sizeDelta = new Vector2(300, 44);
            _contador = Ui.Texto(contador, "", EstiloTexto.Cuerpo, Tizador.Gris, TextAlignmentOptions.Center);
            _contador.font = Ui.FuenteTiza;
            UiKit.Rellenar((RectTransform)_contador.transform);
            Ir(0);
        }

        private void Ir(int pagina) {
            if (Paginas == null || Paginas.Count == 0) return;
            _pagina = Mathf.Clamp(pagina, 0, Paginas.Count - 1);
            _anterior.interactable = _pagina > 0;
            _siguiente.interactable = _pagina < Paginas.Count - 1;
            _contador.text = $"Página {_pagina + 1} de {Paginas.Count}";
            MarcoDePizarra.Limpiar(_lamina);
            Pintar(new Tizador(_lamina, Ui.FuenteTiza), Paginas[_pagina]);
        }

        private void Pintar(Tizador t, PaginaDePizarra p) {
            const float W = MarcoDePizarra.W;
            if (!string.IsNullOrEmpty(Tema)) t.Texto(60, 44, Tema.ToUpperInvariant(), 22, Tizador.Gris);
            t.Texto(W / 2, 96, p.Titulo ?? "", 44, p.EnElJuego ? Tizador.Mostaza : Tizador.Tiza, true);
            t.Linea(360, 116, W - 360, 116, null, 2);

            var vinetas = p.Vinetas ?? new List<VinetaDePizarra>();
            var conDibujos = vinetas.Count > 0;
            float y = 170;
            if (p.EnElJuego) {
                t.Texto(60, y, "EN NEXUS", 24, Tizador.Mostaza);
                y += 16;
            }
            if (!string.IsNullOrEmpty(p.Texto)) {
                var lineas = Partir(p.Texto, conDibujos ? 92 : 78);
                foreach (var linea in lineas) {
                    y += conDibujos ? 40 : 46;
                    t.Texto(60, y, linea, conDibujos ? 28 : 32, Tizador.Tiza, false, W - 120);
                }
            }
            if (!conDibujos) return;

            // Las viñetas, en fila y unidas con curvas de tiza, como los pasos de la receta.
            var n = Math.Min(4, vinetas.Count);
            var ancho = (W - 120) / n;
            var y0 = Math.Max(y + 60, 430);
            for (var i = 0; i < n; i++) {
                var v = vinetas[i];
                var cx = 60 + ancho * i + ancho / 2;
                var color = DibujosDePizarra.ColorDe(v.Color);
                DibujosDePizarra.Dibujar(t.En(cx - 120, y0, 1.5f), v.Icono, color);
                foreach (var (linea, k) in Partir(v.Texto ?? "", 26).Select((l, k) => (l, k)))
                    t.Texto(cx, y0 + 250 + k * 34, linea, 26, color, true);
                if (i < n - 1) t.Curva(cx + 130, y0 + 95, cx + ancho / 2, y0 + 40, cx + ancho - 130, y0 + 95);
            }
        }

        /// <summary>Parte un texto en lineas de como mucho 'max' caracteres, sin cortar palabras.</summary>
        private static List<string> Partir(string texto, int max) {
            var lineas = new List<string>();
            foreach (var parrafo in texto.Split('\n')) {
                var actual = "";
                foreach (var palabra in parrafo.Split(' ')) {
                    if (actual.Length + palabra.Length + 1 > max && actual.Length > 0) { lineas.Add(actual); actual = palabra; }
                    else actual = actual.Length == 0 ? palabra : actual + " " + palabra;
                }
                lineas.Add(actual);
            }
            return lineas;
        }

        private void OnDestroy() {
            if (App != null && App.Runner != null) App.Runner.Pausado = _pausadoAntes;
        }
    }
}
