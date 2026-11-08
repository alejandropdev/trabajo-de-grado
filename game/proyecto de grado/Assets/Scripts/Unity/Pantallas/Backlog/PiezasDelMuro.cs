using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>Lo que dice una ficha del muro. Lo mismo vale para una tarjeta del tablero que para una historia.</summary>
    public sealed class DatosDeFicha {
        public string Id;
        public string Titulo;
        /// <summary>«interfaz», «datos», «logica», «pruebas» (o vacio en una historia).</summary>
        public string Tipo;
        public double Puntos;
        /// <summary>0..1 si esta empezada; -1 si no.</summary>
        public float Avance = -1;
        public string Quien, Pareja;
        public bool EsBug, Fija, Nueva, Tarde, NoCabe, Siguiente;
        public string PorQueFija;
        /// <summary>Una linea de aviso bajo el titulo (en ambar).</summary>
        public string Nota;
        /// <summary>Lineas cortas con su rotulo («como», «quiero», «para»): las historias.</summary>
        public List<KeyValuePair<string, string>> Lineas;
        /// <summary>El puesto en la fila (1, 2…), o 0 para no ponerlo.</summary>
        public int Numero;
        /// <summary>Un color propio para la franja (las historias: el de su modulo). null = el de su tipo.</summary>
        public Color? Cinta;
        /// <summary>Segundos de espera para entrar con su animacion; negativo = sin animacion de entrada.</summary>
        public float Entrada = -1;
    }

    /// <summary>
    /// Las piezas del muro del backlog, con el lenguaje de la pantalla de indicadores (2026-11-07): paneles oscuros
    /// translucidos sobre la escena, borde fino, rotulo en mayusculas pequeñas, cifras grandes en color, barras y
    /// mini-graficas. Las fichas llevan una franja del color de su tipo de trabajo y su icono de linea; los puntos son
    /// bolitas. Todo entra animado (escalonado), las barras crecen, los numeros cuentan y lo que esta en curso brilla.
    /// Las usan el muro de la Fase 2, la planificacion del sprint y el product backlog de la Fase 1.
    /// </summary>
    public static class PiezasDelMuro {
        /// <summary>El papel de la nota del cliente (Fase 1).</summary>
        public static readonly Color ColorCinta = new Color32(0xE6, 0xDB, 0xB4, 0xFF);
        public static readonly Color TintaCinta = new Color32(0x2B, 0x25, 0x18, 0xFF);

        public static Color ColorDeTipo(NexusTheme tema, string tipo, bool esBug) {
            if (esBug) return tema.danger;
            switch (tipo) {
                case "interfaz": return tema.cyan;
                case "datos": return tema.violet;
                case "logica": return tema.warning;
                case "pruebas": return tema.success;
                default: return tema.inkMuted;
            }
        }

        public static string NombreDeTipo(string tipo) {
            switch (tipo) {
                case "logica": return "lógica";
                default: return tipo ?? "";
            }
        }

        // ==================================================================== la capa de arrastre

        /// <summary>Un nodo a toda pantalla por encima de lo demas, donde vuela la ficha que se arrastra.</summary>
        public static RectTransform CapaDeArrastre(UiKit ui, Transform padre) {
            var capa = ui.Nodo(padre, "Capa de arrastre");
            UiKit.Rellenar(capa);
            UiKit.Tamano(capa).ignoreLayout = true;
            capa.SetAsLastSibling();
            return capa;
        }

        // ==================================================================== un panel como los de indicadores

        /// <summary>Un panel de indicadores: fondo oscuro, borde fino, el rotulo en mayusculas. Devuelve donde meter las cosas.</summary>
        public static RectTransform Panel(UiKit ui, Transform padre, string rotulo, Color? raya = null, float relleno = 16) {
            var tema = ui.Tema;
            var p = ui.Columna(padre, "Panel " + rotulo, 8, relleno, TextAnchor.UpperLeft);
            var fondo = p.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(tema.surfaceRaised, 0.94f), NexusTheme.RadioLg);
            ui.Borde(p, tema.lineStrong, NexusTheme.RadioLg, 1);
            if (raya.HasValue) {
                // Una raya de color arriba, por dentro: de un vistazo, de que es el panel.
                var r = ui.Nodo(p, "Raya");
                UiKit.Tamano(r).ignoreLayout = true;
                r.anchorMin = new Vector2(0, 1);
                r.anchorMax = new Vector2(1, 1);
                r.pivot = new Vector2(0.5f, 1);
                r.sizeDelta = new Vector2(-28, 3);
                r.anchoredPosition = new Vector2(0, -1);
                var img = r.gameObject.AddComponent<Image>();
                img.color = raya.Value;
                img.raycastTarget = false;
            }
            if (!string.IsNullOrEmpty(rotulo)) ui.Texto(p, rotulo.ToUpperInvariant(), EstiloTexto.Leyenda, tema.inkMuted);
            return p;
        }

        /// <summary>Un numero grande en color, como el de los indicadores. Si 'contar', cuenta desde 0.</summary>
        public static TMP_Text Cifra(UiKit ui, Transform padre, double valor, string formato, string sufijo, Color color, float tamano, float retraso = -1) {
            var t = ui.Texto(padre, valor.ToString(formato) + sufijo, EstiloTexto.Hero, color);
            t.fontSize = tamano;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            if (retraso >= 0) {
                var c = t.gameObject.AddComponent<ContarCifra>();
                c.Hasta = (float)valor;
                c.Formato = formato;
                c.Sufijo = sufijo;
                c.Retraso = retraso;
            }
            return t;
        }

        /// <summary>Una barra que crece desde 0 (si 'retraso' &gt;= 0).</summary>
        public static BarraView Barra(UiKit ui, Transform padre, float valor, Color color, float retraso = -1, float alto = 8) {
            var b = ui.Barra(padre, retraso >= 0 ? 0 : valor, color);
            UiKit.Tamano(b, alto: alto, flexAncho: 1);
            if (retraso >= 0) {
                var c = b.gameObject.AddComponent<CrecerBarra>();
                c.Hasta = Mathf.Clamp01(valor);
                c.Retraso = retraso;
            }
            return b;
        }

        /// <summary>Barritas de una serie (una por dia), la ultima en color pleno.</summary>
        public static RectTransform MiniGrafica(UiKit ui, Transform padre, IList<double> serie, Color color, float alto = 30, int maximo = 12) {
            var fila = ui.Fila(padre, "Mini", 3, 0, TextAnchor.LowerLeft);
            UiKit.Tamano(fila, alto: alto);
            if (serie == null || serie.Count == 0) {
                ui.Texto(fila, "sale al cerrar el día", EstiloTexto.Leyenda, ui.Tema.inkFaint);
                return fila;
            }
            var desde = Math.Max(0, serie.Count - maximo);
            var max = 0.0;
            for (var i = desde; i < serie.Count; i++) max = Math.Max(max, serie[i]);
            max = Math.Max(1e-3, max);
            for (var i = desde; i < serie.Count; i++) {
                var b = ui.Nodo(fila, "Barra");
                var img = b.gameObject.AddComponent<Image>();
                img.color = NexusTheme.Alfa(color, i == serie.Count - 1 ? 1f : 0.45f);
                img.raycastTarget = false;
                UiKit.Tamano(b, 10, Mathf.Max(3, (float)(serie[i] / max * alto)));
            }
            return fila;
        }

        /// <summary>Una barra partida en tramos de color (lo que hay de cada tipo), con su leyenda de puntos.</summary>
        public static void Reparto(UiKit ui, Transform padre, IList<KeyValuePair<Color, double>> tramos, float alto = 10) {
            var total = 0.0;
            foreach (var kv in tramos) total += kv.Value;
            var barra = ui.Fila(padre, "Reparto", 2, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(barra, alto: alto, flexAncho: 1);
            if (total <= 0) {
                var vacio = ui.Nodo(barra, "Vacio");
                vacio.gameObject.AddComponent<Image>().color = NexusTheme.Alfa(ui.Tema.inkFaint, 0.25f);
                UiKit.Tamano(vacio, alto: alto, flexAncho: 1);
                return;
            }
            foreach (var kv in tramos) {
                if (kv.Value <= 0) continue;
                var t = ui.Nodo(barra, "Tramo");
                var img = t.gameObject.AddComponent<Image>();
                img.color = kv.Key;
                img.raycastTarget = false;
                UiKit.Tamano(t, ancho: 0, alto: alto, flexAncho: (float)(kv.Value / total));
            }
        }

        // ==================================================================== la ficha

        public static FichaArrastrable Ficha(UiKit ui, Transform padre, DatosDeFicha d, RectTransform capa) {
            var tema = ui.Tema;
            var color = d.Cinta ?? ColorDeTipo(tema, d.Tipo, d.EsBug);
            var raiz = ui.Fila(padre, "Ficha " + d.Id, 10, 0, TextAnchor.UpperLeft);
            var grupoFila = raiz.GetComponent<HorizontalLayoutGroup>();
            grupoFila.padding = new RectOffset(0, 12, 10, 10);
            var fondo = raiz.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, d.Fija ? NexusTheme.Alfa(tema.bg900, 0.9f) : NexusTheme.Alfa(tema.bg950, 0.82f), NexusTheme.RadioMd);
            var borde = ui.Borde(raiz, d.Avance >= 0 ? NexusTheme.Alfa(color, 0.55f) : tema.line, NexusTheme.RadioMd, 1);

            // La franja del color de su tipo, a la izquierda (como las tarjetas del carrusel).
            var franja = ui.Nodo(raiz, "Franja");
            var imgFranja = franja.gameObject.AddComponent<Image>();
            ui.Fondo(imgFranja, color, 3);
            imgFranja.raycastTarget = false;
            UiKit.Tamano(franja, ancho: 5);

            if (d.Numero > 0) {
                var n = ui.Texto(raiz, d.Numero.ToString(), EstiloTexto.Encabezado, NexusTheme.Alfa(color, 0.95f), TextAlignmentOptions.Center);
                n.font = tema.FuenteDisplay;
                UiKit.Tamano(n, 30, 34);
            }
            if (!string.IsNullOrEmpty(d.Tipo) || d.EsBug) IconoDeTipo(ui, raiz, d.Tipo, d.EsBug, 34, color);

            var cuerpo = ui.Columna(raiz, "Cuerpo", 5);
            UiKit.Tamano(cuerpo, flexAncho: 1);
            if (d.Siguiente) {
                var sig = ui.Chip(cuerpo, "LA SIGUIENTE", NexusTheme.Alfa(tema.cyan, 0.18f), tema.cyan);
                sig.gameObject.AddComponent<Latido>().Fuerza = 0.05f;
            }
            var titulo = ui.Texto(cuerpo, d.Titulo ?? "", EstiloTexto.Cuerpo, tema.ink);
            titulo.fontStyle = FontStyles.Bold;
            titulo.maxVisibleLines = 2;
            titulo.overflowMode = TextOverflowModes.Ellipsis;
            if (d.Lineas != null)
                foreach (var kv in d.Lineas) {
                    if (string.IsNullOrEmpty(kv.Value)) continue;
                    var l = ui.Texto(cuerpo, $"<color={NexusTheme.Html(NexusTheme.Alfa(color, 0.95f))}>{kv.Key.ToUpperInvariant()}</color>  {kv.Value}", EstiloTexto.Pequeno, tema.inkMuted);
                    l.maxVisibleLines = 2;
                    l.overflowMode = TextOverflowModes.Ellipsis;
                }
            if (!string.IsNullOrEmpty(d.Nota)) ui.Texto(cuerpo, d.Nota, EstiloTexto.Leyenda, tema.warning);

            var pie = ui.Fila(cuerpo, "Pie", 6, 0, TextAnchor.MiddleLeft);
            if (d.Puntos > 0) Puntos(ui, pie, d.Puntos, color);
            if (d.Nueva) ui.Chip(pie, "nueva", NexusTheme.Alfa(tema.cyan, 0.2f), tema.cyan);
            if (d.Tarde) ui.Chip(pie, "pedida tarde", NexusTheme.Alfa(tema.warning, 0.2f), tema.warning);
            if (d.NoCabe) ui.Chip(pie, "no cabe", NexusTheme.Alfa(tema.warning, 0.25f), tema.warning);
            UiKit.Tamano(ui.Nodo(pie, "Hueco"), flexAncho: 1);
            if (d.Fija) Candado(ui, pie, tema.inkMuted);
            if (!string.IsNullOrEmpty(d.Quien)) ui.Retrato(pie, d.Quien.Substring(0, 1), Tono.Cyan, 24);
            if (!string.IsNullOrEmpty(d.Pareja)) ui.Retrato(pie, d.Pareja.Substring(0, 1), Tono.Violeta, 24);
            if (d.Avance >= 0) {
                var barra = Barra(ui, cuerpo, Mathf.Clamp01(d.Avance), color, d.Entrada >= 0 ? d.Entrada + 0.15f : -1, 6);
                // Lo que esta en curso «trabaja»: un brillo que recorre su barra.
                barra.gameObject.AddComponent<BrilloQueRecorre>();
            }

            if (d.NoCabe) {
                var velo = ui.Nodo(raiz, "No cabe");
                UiKit.Tamano(velo).ignoreLayout = true;
                UiKit.Rellenar(velo);
                var img = velo.gameObject.AddComponent<Image>();
                ui.Fondo(img, NexusTheme.Alfa(tema.warning, 0.08f), NexusTheme.RadioMd);
                img.raycastTarget = false;
            }

            var ficha = raiz.gameObject.AddComponent<FichaArrastrable>();
            ficha.Id = d.Id;
            ficha.Tipo = d.Tipo;
            ficha.Fija = d.Fija;
            ficha.PorQueFija = d.PorQueFija;
            ficha.Capa = capa;
            var viva = raiz.gameObject.AddComponent<PiezaViva>();
            viva.Escala = 1.025f;
            // Al pasar el raton, el borde se enciende del color de su tipo.
            var pasar = raiz.gameObject.AddComponent<AlPasar>();
            var bordeNormal = borde.color;
            pasar.Entra += () => borde.color = NexusTheme.Alfa(color, 0.95f);
            pasar.Sale += () => borde.color = bordeNormal;
            if (FichaArrastrable.UltimaSoltada == d.Id) {
                raiz.gameObject.AddComponent<AsientoDeFicha>();
                FichaArrastrable.UltimaSoltada = null;
            } else if (d.Entrada >= 0) {
                raiz.gameObject.AddComponent<EntradaDePieza>().Retraso = d.Entrada;
            }
            return ficha;
        }

        /// <summary>Los puntos como bolitas (hasta 8); mas, como cifra.</summary>
        public static void Puntos(UiKit ui, Transform padre, double puntos, Color color) {
            var n = Mathf.CeilToInt((float)puntos - 0.01f);
            if (n > 8) { ui.Chip(padre, $"{puntos:0.#} pts", NexusTheme.Alfa(color, 0.18f), color); return; }
            var fila = ui.Fila(padre, "Puntos", 3, 0, TextAnchor.MiddleLeft);
            for (var i = 0; i < n; i++) {
                var lleno = i < Mathf.FloorToInt((float)puntos + 0.01f);
                ui.Punto(fila, lleno ? color : NexusTheme.Alfa(color, 0.4f), 9);
            }
        }

        // ==================================================================== iconos de linea

        /// <summary>El icono del tipo de trabajo, en linea fina, en 'lado'×'lado'.</summary>
        public static void IconoDeTipo(UiKit ui, Transform padre, string tipo, bool esBug, float lado, Color color) {
            var lamina = new Lamina(ui, padre, lado, lado, "Icono");
            var t = new Tizador(lamina, ui.FuenteTiza, 0, 0, lado / 40f);
            if (esBug) {
                t.Elipse(20, 22, 8, 11, color);
                t.Linea(20, 12, 20, 33, color, 2);
                for (var i = 0; i < 3; i++) {
                    var y = 16 + i * 7;
                    t.Linea(12, y, 5, y - 3 + i * 3, color, 2);
                    t.Linea(28, y, 35, y - 3 + i * 3, color, 2);
                }
                t.Linea(17, 11, 13, 5, color, 2);
                t.Linea(23, 11, 27, 5, color, 2);
                return;
            }
            switch (tipo) {
                case "interfaz":
                    t.Caja(4, 7, 32, 26, null, color);
                    t.Linea(4, 14, 36, 14, color, 2);
                    t.Punto(9, 10.5f, 1.4f, color);
                    t.Punto(14, 10.5f, 1.4f, color);
                    t.Linea(10, 21, 24, 21, color, 2);
                    t.Linea(10, 27, 19, 27, color, 2);
                    break;
                case "datos":
                    t.Cilindro(9, 5, 22, 30, color);
                    break;
                case "logica":
                    t.Engranaje(20, 20, 9, color);
                    break;
                case "pruebas":
                    t.Trazo(color, 15, 5, 15, 16, 6, 34, 34, 34, 25, 16, 25, 5);
                    t.Linea(12, 5, 28, 5, color, 3);
                    t.Si(20, 27, color, 5);
                    break;
                default:
                    t.Caja(8, 6, 24, 28, null, color);
                    t.Linea(12, 14, 28, 14, color, 2);
                    t.Linea(12, 20, 26, 20, color, 2);
                    t.Linea(12, 26, 22, 26, color, 2);
                    break;
            }
        }

        /// <summary>
        /// El icono de una ceremonia, por lo que es (se deduce de su id): la diaria, gente en corro; planificar, un
        /// calendario; revisar, una lupa; la retro, un bocadillo; firmar, un sello; lo demas, un engranaje.
        /// </summary>
        public static void IconoDeCeremonia(UiKit ui, Transform padre, string id, float lado, Color color) {
            var lamina = new Lamina(ui, padre, lado, lado, "Icono");
            var t = new Tizador(lamina, ui.FuenteTiza, 0, 0, lado / 40f);
            var i = (id ?? "").ToLowerInvariant();
            if (i.Contains("daily") || i.Contains("diaria")) {
                t.Circulo(20, 21, 13, color, true, 2);
                for (var k = 0; k < 4; k++) {
                    var a = Mathf.PI / 2 * k + 0.4f;
                    t.Punto(20 + Mathf.Cos(a) * 13, 21 + Mathf.Sin(a) * 13, 4f, color);
                }
                t.Punto(20, 21, 2.5f, color);
            } else if (i.Contains("planning") || i.Contains("plan") || i.Contains("kickoff") || i.Contains("reposicion")) {
                t.Caja(6, 9, 28, 25, null, color);
                t.Linea(6, 16, 34, 16, color, 2);
                t.Linea(13, 5, 13, 11, color, 3);
                t.Linea(27, 5, 27, 11, color, 3);
                t.Si(20, 25, color, 5);
            } else if (i.Contains("review") || i.Contains("revision") || i.Contains("hito") || i.Contains("comite")) {
                t.Circulo(17, 17, 9, color);
                t.Linea(24, 24, 33, 33, color, 4);
            } else if (i.Contains("retro") || i.Contains("lecciones") || i.Contains("mejora")) {
                t.Caja(5, 7, 30, 20, null, color);
                t.Trazo(color, 12, 27, 10, 34, 19, 27);
                t.Punto(13, 17, 1.8f, color);
                t.Punto(20, 17, 1.8f, color);
                t.Punto(27, 17, 1.8f, color);
            } else if (i.Contains("firma")) {
                t.Circulo(20, 18, 10, color);
                t.Linea(20, 28, 20, 33, color, 3);
                t.Linea(10, 35, 30, 35, color, 3);
            } else {
                t.Engranaje(20, 20, 9, color);
            }
        }

        /// <summary>Un color fijo para cada ceremonia (el mismo en la agenda, las actas y su tarjeta).</summary>
        public static Color ColorDeCeremonia(NexusTheme tema, string id) {
            var paleta = new[] { tema.cyan, tema.violet, tema.warning, tema.success, Tizador.Roja, Tizador.Cian };
            var h = 0;
            foreach (var ch in id ?? "") h = (h * 31 + ch) & 0x7fffffff;
            return paleta[h % paleta.Length];
        }

        /// <summary>La primera frase de un texto (hasta el primer punto), para no poner parrafos.</summary>
        public static string PrimeraFrase(string texto) {
            if (string.IsNullOrEmpty(texto)) return "";
            var i = texto.IndexOf(". ", StringComparison.Ordinal);
            return i < 0 ? texto : texto.Substring(0, i + 1);
        }

        /// <summary>
        /// Un sello de goma («HECHA», «ACORDADO») arriba a la derecha de una pieza, algo torcido. Si 'caer', cae como
        /// un golpe: de grande a su tamaño en ~250 ms (solo la primera vez que se ve).
        /// </summary>
        public static RectTransform Sello(UiKit ui, Transform padre, string texto, Color color, bool caer) {
            var tema = ui.Tema;
            var sello = ui.Fila(padre, "Sello", 0, 6, TextAnchor.MiddleCenter);
            UiKit.Tamano(sello).ignoreLayout = true;
            sello.anchorMin = sello.anchorMax = sello.pivot = new Vector2(1, 1);
            sello.anchoredPosition = new Vector2(-14, -14);
            var ajuste = sello.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.horizontalFit = ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sello.localRotation = Quaternion.Euler(0, 0, -11);
            var img = sello.gameObject.AddComponent<Image>();
            ui.Fondo(img, NexusTheme.Alfa(color, 0.12f), 6);
            img.raycastTarget = false;
            ui.Borde(sello, color, 6, 2);
            var t = ui.Texto(sello, texto, EstiloTexto.Encabezado, color, TextAlignmentOptions.Center);
            t.font = tema.FuenteDisplay;
            t.characterSpacing = 4;
            t.margin = new Vector4(8, 0, 8, 0);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            if (caer) sello.gameObject.AddComponent<SelloQueCae>();
            return sello;
        }

        private sealed class SelloQueCae : MonoBehaviour {
            private float _t;
            private CanvasGroup _g;

            private void Awake() {
                _g = gameObject.AddComponent<CanvasGroup>();
                _g.alpha = 0;
                transform.localScale = Vector3.one * 1.25f;
            }

            private void Update() {
                _t += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01((_t - 0.15f) / 0.25f);
                _g.alpha = Mathf.Clamp01(p * 2);
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(1.25f, 1f, ContextoDeHallazgo.Rebote(p));
                if (p >= 1) { transform.localScale = Vector3.one; Destroy(this); }
            }
        }

        // ==================================================================== los suministros, dibujados

        /// <summary>El color de cada suministro: la pizza naranja, el cafe tostado, la Guepardex lima.</summary>
        public static Color ColorDeSuministro(NexusTheme tema, string id) {
            var i = (id ?? "").ToLowerInvariant();
            if (i.Contains("pizza")) return new Color32(0xF5, 0x9E, 0x0B, 0xFF);
            if (i.Contains("coffee") || i.Contains("cafe")) return new Color32(0xC0, 0x84, 0x57, 0xFF);
            if (i.Contains("energy") || i.Contains("guepard")) return new Color32(0xA3, 0xE6, 0x35, 0xFF);
            return tema.cyan;
        }

        /// <summary>
        /// El dibujo del suministro, relleno y con su contorno, en 'lado'×'lado' (2026-11-07): una porcion de pizza con
        /// pepperoni, una taza de cafe con su vapor que sube, una lata de Guepardex con su rayo y burbujas; lo demas, una
        /// caja con lazo. Se dibuja solo al abrir (Avance) y queda con su pequeño movimiento. 'apagado' lo pinta en gris.
        /// </summary>
        public static RectTransform IlustracionDeSuministro(UiKit ui, Transform padre, string id, float lado, bool apagado, bool dibujarse) {
            var lamina = new Lamina(ui, padre, lado, lado, "Ilustracion");
            var d = lamina.Dibujo;
            var k = lado / 100f;
            Func<float, float, Vector2> P = (x, y) => new Vector2(x * k, y * k);
            Func<Color, Color> C = c => apagado ? Color.Lerp(c, new Color(0.45f, 0.48f, 0.52f, c.a), 0.8f) : c;
            var tinta = C(new Color32(0x2A, 0x1C, 0x10, 0xFF));
            var g = 2.5f * k;
            var i = (id ?? "").ToLowerInvariant();
            if (i.Contains("pizza")) {
                d.Poligono(new List<Vector2> { P(18, 24), P(82, 24), P(50, 90) }, C(new Color32(0xF8, 0xCD, 0x5A, 0xFF)));
                d.Poligono(new List<Vector2> { P(14, 12), P(86, 12), P(82, 25), P(18, 25) }, C(new Color32(0xC7, 0x7D, 0x3A, 0xFF)));
                d.Polilinea(new List<Vector2> { P(14, 12), P(86, 12), P(82, 25), P(50, 90), P(18, 25), P(14, 12) }, tinta, g);
                d.Linea(18 * k, 25 * k, 82 * k, 25 * k, tinta, g);
                var pepperoni = C(new Color32(0xD9, 0x48, 0x3B, 0xFF));
                d.Circulo(40 * k, 38 * k, 7 * k, tinta, 1.5f * k, pepperoni);
                d.Circulo(61 * k, 41 * k, 6 * k, tinta, 1.5f * k, pepperoni);
                d.Circulo(50 * k, 61 * k, 5.5f * k, tinta, 1.5f * k, pepperoni);
                d.Elipse(67 * k, 30 * k, 3 * k, 6 * k, C(new Color32(0xFB, 0xE2, 0x8F, 0xFF)), 1, C(new Color32(0xFB, 0xE2, 0x8F, 0xFF)));
                lamina.Raiz.gameObject.AddComponent<Bamboleo>();
            } else if (i.Contains("coffee") || i.Contains("cafe")) {
                d.Elipse(50 * k, 86 * k, 36 * k, 7 * k, tinta, g, C(new Color32(0xD9, 0xD2, 0xC5, 0xFF)));
                d.Rect(26 * k, 42 * k, 44 * k, 40 * k, tinta, g, C(new Color32(0xEE, 0xE7, 0xDB, 0xFF)), false, 8 * k);
                d.Circulo(74 * k, 58 * k, 10 * k, tinta, g);
                d.Elipse(48 * k, 44 * k, 20 * k, 4.5f * k, tinta, 1.5f * k, C(new Color32(0x6B, 0x3F, 0x21, 0xFF)));
                d.Linea(34 * k, 62 * k, 60 * k, 62 * k, C(new Color32(0xC0, 0x84, 0x57, 0xFF)), 3 * k);
                // El vapor: tres volutas aparte, que suben y se apagan en bucle.
                if (!apagado)
                    for (var v = 0; v < 3; v++) {
                        var vapor = new Lamina(ui, lamina.Raiz, 16 * k, 30 * k, "Vapor");
                        var rt = vapor.Raiz;
                        UiKit.Tamano(rt).ignoreLayout = true;
                        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
                        rt.anchoredPosition = new Vector2((32 + v * 11) * k, -8 * k);
                        vapor.Dibujo.Curva(new Vector2(8 * k, 28 * k), new Vector2(0, 14 * k), new Vector2(8 * k, 2 * k), new Color(1, 1, 1, 0.75f), 2.2f * k, false);
                        rt.gameObject.AddComponent<SubeEnBucle>().Fase = v * 0.33f;
                    }
            } else if (i.Contains("energy") || i.Contains("guepard")) {
                var lata = C(new Color32(0xA3, 0xE6, 0x35, 0xFF));
                d.Rect(32 * k, 20 * k, 36 * k, 66 * k, tinta, g, lata, false, 7 * k);
                d.Elipse(50 * k, 20 * k, 18 * k, 4 * k, tinta, g, C(new Color32(0xC9, 0xCF, 0xD6, 0xFF)));
                d.Linea(36 * k, 32 * k, 64 * k, 32 * k, tinta, 1.5f * k);
                d.Linea(36 * k, 76 * k, 64 * k, 76 * k, tinta, 1.5f * k);
                // El rayo y las manchas de guepardo.
                d.Polilinea(new List<Vector2> { P(53, 36), P(44, 55), P(54, 55), P(46, 72) }, tinta, 4 * k);
                foreach (var m in new[] { P(39, 42), P(61, 46), P(40, 64), P(60, 66) }) d.Circulo(m.x, m.y, 2.6f * k, tinta, 1, tinta);
                if (!apagado)
                    for (var b = 0; b < 4; b++) {
                        var burbuja = ui.Nodo(lamina.Raiz, "Burbuja");
                        UiKit.Tamano(burbuja).ignoreLayout = true;
                        burbuja.anchorMin = burbuja.anchorMax = burbuja.pivot = new Vector2(0, 1);
                        burbuja.sizeDelta = Vector2.one * (3 + b % 2 * 2) * k;
                        burbuja.anchoredPosition = new Vector2((40 + b * 6) * k, -16 * k);
                        var img = burbuja.gameObject.AddComponent<Image>();
                        img.sprite = ui.SpriteCircular();
                        img.color = new Color(1, 1, 1, 0.8f);
                        img.raycastTarget = false;
                        burbuja.gameObject.AddComponent<SubeEnBucle>().Fase = b * 0.25f;
                    }
            } else {
                d.Rect(22 * k, 34 * k, 56 * k, 50 * k, tinta, g, C(new Color32(0x67, 0xE8, 0xF9, 0xFF)), false, 6 * k);
                d.Linea(50 * k, 34 * k, 50 * k, 84 * k, tinta, g);
                d.Linea(22 * k, 56 * k, 78 * k, 56 * k, tinta, g);
                d.Circulo(44 * k, 28 * k, 6 * k, tinta, g);
                d.Circulo(56 * k, 28 * k, 6 * k, tinta, g);
            }
            if (dibujarse) lamina.Raiz.gameObject.AddComponent<DibujarseAlAbrir>();
            return lamina.Raiz;
        }

        public static void Candado(UiKit ui, Transform padre, Color color) {
            var lamina = new Lamina(ui, padre, 22, 24, "Candado");
            new Tizador(lamina, ui.FuenteTiza, 0, 0, 0.55f).Candado(20, 18, 9, color);
        }

        /// <summary>Una flecha de linea (para «arriba, lo que mas vale» y para pasar de un carril a otro).</summary>
        public static void Flecha(UiKit ui, Transform padre, float w, float h, bool haciaArriba, Color color) {
            var lamina = new Lamina(ui, padre, w, h, "Flecha");
            var t = new Tizador(lamina, ui.FuenteTiza);
            if (haciaArriba) t.Flecha(w / 2, h - 4, w / 2, 6, color, 3);
            else t.Flecha(4, h / 2, w - 6, h / 2, color, 3);
        }

        // ==================================================================== el carril

        /// <summary>
        /// Una columna del muro, como un panel de indicadores: el rotulo, la cifra grande de cuantas hay (cuenta al
        /// abrir), los puntos y una barra con su parte del total; debajo, la lista con su desplazamiento. El carril es
        /// tambien donde se sueltan fichas: devuelve su destino, ya con su lista.
        /// </summary>
        public static RectTransform Carril(UiKit ui, Transform padre, string titulo, string cuenta, Color acento,
                                           out RectTransform lista, out DestinoDeFicha destino,
                                           double puntos = -1, float parte = -1, float retraso = -1) {
            var tema = ui.Tema;
            var carril = Panel(ui, padre, titulo, acento, 14);
            var cabecera = ui.Fila(carril, "Cabecera", 10, 0, TextAnchor.LowerLeft);
            int n;
            if (!string.IsNullOrEmpty(cuenta)) {
                if (int.TryParse(cuenta, out n)) Cifra(ui, cabecera, n, "0", "", acento, 40, retraso);
                else Cifra(ui, cabecera, 0, "0", "", acento, 40).text = cuenta;
            }
            if (puntos >= 0) ui.Texto(cabecera, $"{puntos:0.#} pts", EstiloTexto.Pequeno, tema.inkMuted);
            if (parte >= 0) Barra(ui, carril, parte, acento, retraso, 6);

            var scroll = ui.Desplazable(carril, out lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var grupo = lista.GetComponent<VerticalLayoutGroup>();
            if (grupo != null) {
                grupo.spacing = 10;
                grupo.padding = new RectOffset(4, 4, 6, 6);
            }

            destino = carril.gameObject.AddComponent<DestinoDeFicha>();
            destino.Ui = ui;
            destino.Lista = lista;
            destino.Acento = acento;
            destino.Radio = NexusTheme.RadioLg;
            if (retraso >= 0) carril.gameObject.AddComponent<EntradaDePieza>().Retraso = retraso;
            return carril;
        }

        /// <summary>Un rotulo de seccion: mayusculas pequeñas y una raya de color debajo.</summary>
        public static RectTransform Cinta(UiKit ui, Transform padre, string texto, Color raya, float giro = 0) {
            var col = ui.Columna(padre, "Rotulo", 4);
            ui.Texto(col, (texto ?? "").ToUpperInvariant(), EstiloTexto.Leyenda, ui.Tema.ink);
            var r = ui.Nodo(col, "Raya");
            var img = r.gameObject.AddComponent<Image>();
            img.color = raya;
            img.raycastTarget = false;
            UiKit.Tamano(r, 36, 3);
            return col;
        }

        /// <summary>Un hueco a trazos con una linea: un carril vacio, o un sitio libre.</summary>
        public static RectTransform Vacio(UiKit ui, Transform padre, string texto, float alto = 70) {
            var tema = ui.Tema;
            var hueco = ui.Fila(padre, "Hueco", 10, 12, TextAnchor.MiddleCenter);
            UiKit.Tamano(hueco, alto: alto, flexAncho: 1);
            var img = hueco.gameObject.AddComponent<Image>();
            img.sprite = Formas.Borde(tema.Px(NexusTheme.RadioMd), 1.5f);
            img.type = Image.Type.Sliced;
            img.color = NexusTheme.Alfa(tema.inkFaint, 0.45f);
            img.raycastTarget = false;
            var t = ui.Texto(hueco, texto, EstiloTexto.Pequeno, tema.inkFaint, TextAlignmentOptions.Center);
            UiKit.Tamano(t, flexAncho: 1);
            hueco.gameObject.AddComponent<Latido>().Fuerza = 0.012f;
            return hueco;
        }

        /// <summary>Una raya punteada con un letrero en medio, dentro de una lista (p. ej. «hasta aquí cabe en el sprint»).</summary>
        public static void Corte(UiKit ui, Transform padre, string texto, Color color) {
            var fila = ui.Fila(padre, "Corte", 8, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(fila, alto: 24, flexAncho: 1);
            for (var lado = 0; lado < 2; lado++) {
                var trazos = ui.Fila(fila, "Trazos", 5, 0, TextAnchor.MiddleCenter);
                UiKit.Tamano(trazos, ancho: 0, flexAncho: 1);
                for (var i = 0; i < 6; i++) {
                    var p = ui.Nodo(trazos, "Trazo");
                    var img = p.gameObject.AddComponent<Image>();
                    img.color = NexusTheme.Alfa(color, 0.8f);
                    img.raycastTarget = false;
                    UiKit.Tamano(p, 8, 2);
                }
                if (lado == 0) {
                    var t = ui.Texto(fila, texto, EstiloTexto.Leyenda, color, TextAlignmentOptions.Center);
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
        }

        // ==================================================================== lo que pasa al actuar

        /// <summary>«-5 min» que sale de donde se solto la ficha, sube y se apaga.</summary>
        public static void Marca(UiKit ui, RectTransform capa, Vector3 mundo, string texto, Color color) {
            if (capa == null || string.IsNullOrEmpty(texto)) return;
            var t = ui.Texto(capa, texto, EstiloTexto.Encabezado, color, TextAlignmentOptions.Center);
            t.font = ui.Tema.FuenteDisplay;
            UiKit.Tamano(t).ignoreLayout = true;
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(360, 44);
            t.rectTransform.position = mundo;
            t.raycastTarget = false;
            t.gameObject.AddComponent<MarcaQueSube>();
        }

        /// <summary>Una linea abajo en el centro que se va sola: lo que acaba de pasar, o por que no se pudo.</summary>
        public static void Aviso(UiKit ui, RectTransform capa, string texto, Color color) {
            if (capa == null || string.IsNullOrEmpty(texto)) return;
            var viejo = capa.Find("Aviso");
            if (viejo != null) UnityEngine.Object.Destroy(viejo.gameObject);
            var tema = ui.Tema;
            var aviso = ui.Fila(capa, "Aviso", 10, 14, TextAnchor.MiddleCenter);
            aviso.name = "Aviso";
            UiKit.Tamano(aviso).ignoreLayout = true;
            aviso.anchorMin = aviso.anchorMax = aviso.pivot = new Vector2(0.5f, 0);
            aviso.anchoredPosition = new Vector2(0, 170);
            var ajuste = aviso.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var fondo = aviso.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(tema.bg950, 0.96f), 22);
            ui.Borde(aviso, color, 22, 1.5f);
            ui.Punto(aviso, color, 10);
            var t = ui.Texto(aviso, texto, EstiloTexto.Cuerpo, tema.ink);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            aviso.gameObject.AddComponent<AvisoQueSeVa>();
        }

        private sealed class MarcaQueSube : MonoBehaviour {
            private float _t;
            private Vector3 _inicio;
            private TMP_Text _texto;

            private void Start() { _inicio = transform.localPosition; _texto = GetComponent<TMP_Text>(); }

            private void Update() {
                _t += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(_t / 1.3f);
                transform.localPosition = _inicio + new Vector3(0, ContextoDeHallazgo.Suave(p) * 70, 0);
                transform.localScale = Vector3.one * (p < 0.15f ? Mathf.Lerp(0.6f, 1.15f, p / 0.15f) : Mathf.Lerp(1.15f, 1f, (p - 0.15f) / 0.3f));
                if (_texto != null) _texto.alpha = p < 0.6f ? 1 : 1 - (p - 0.6f) / 0.4f;
                if (p >= 1) Destroy(gameObject);
            }
        }

        private sealed class AvisoQueSeVa : MonoBehaviour {
            private float _t;
            private CanvasGroup _g;

            private void Start() { _g = gameObject.AddComponent<CanvasGroup>(); _g.blocksRaycasts = false; }

            private void Update() {
                _t += Time.unscaledDeltaTime;
                _g.alpha = _t < 0.2f ? _t / 0.2f : _t > 3.4f ? Mathf.Max(0, 1 - (_t - 3.4f) / 0.4f) : 1;
                transform.localScale = Vector3.one * (_t < 0.2f ? Mathf.Lerp(0.9f, 1f, ContextoDeHallazgo.Rebote(_t / 0.2f)) : 1f);
                if (_t > 3.8f) Destroy(gameObject);
            }
        }
    }

    // ======================================================================== animaciones e interaccion

    /// <summary>Entra con un retraso: aparece y crece un poco (sin tocar la posicion, que es del layout).</summary>
    public sealed class EntradaDePieza : MonoBehaviour {
        public float Retraso;
        /// <summary>Hasta donde se ve al terminar (una ficha apagada por el filtro se queda a medias).</summary>
        public float AlfaFinal = 1;
        private float _t;
        private CanvasGroup _g;

        private void Awake() {
            _g = GetComponent<CanvasGroup>();
            if (_g == null) _g = gameObject.AddComponent<CanvasGroup>();
            _g.alpha = 0;
            transform.localScale = Vector3.one * 0.95f;
        }

        private void Update() {
            _t += Time.unscaledDeltaTime;
            // emil-design: entra rapido y aterriza suave (ease-out), en ~220 ms, desde 0,95; sin rebote.
            var p = Mathf.Clamp01((_t - Retraso) / 0.22f);
            _g.alpha = p * AlfaFinal;
            transform.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, ContextoDeHallazgo.Suave(p));
            if (p >= 1) { _g.alpha = AlfaFinal; transform.localScale = Vector3.one; Destroy(this); }
        }
    }

    /// <summary>Un numero que cuenta desde 0.</summary>
    public sealed class ContarCifra : MonoBehaviour {
        public float Hasta, Retraso;
        public string Formato = "0", Sufijo = "";
        private float _t;
        private TMP_Text _texto;

        private void Awake() { _texto = GetComponent<TMP_Text>(); }

        private void Update() {
            _t += Time.unscaledDeltaTime;
            var p = Mathf.Clamp01((_t - Retraso) / 0.8f);
            _texto.text = (Hasta * ContextoDeHallazgo.Suave(p)).ToString(Formato) + Sufijo;
            if (p >= 1) Destroy(this);
        }
    }

    /// <summary>Una barra que crece desde 0.</summary>
    public sealed class CrecerBarra : MonoBehaviour {
        public float Hasta, Retraso;
        private float _t;
        private BarraView _barra;

        private void Awake() { _barra = GetComponent<BarraView>(); }

        private void Update() {
            _t += Time.unscaledDeltaTime;
            var p = Mathf.Clamp01((_t - Retraso) / 0.8f);
            if (_barra != null) _barra.Valor = Hasta * ContextoDeHallazgo.Suave(p);
            if (p >= 1) Destroy(this);
        }
    }

    /// <summary>Un latido suave de escala: lo que llama la atencion sin gritar.</summary>
    public sealed class Latido : MonoBehaviour {
        public float Fuerza = 0.03f, Velocidad = 2.4f;
        private float _fase;

        private void Start() { _fase = UnityEngine.Random.value * 6f; }

        private void Update() {
            transform.localScale = Vector3.one * (1 + Mathf.Sin(Time.unscaledTime * Velocidad + _fase) * Fuerza);
        }
    }

    /// <summary>Un brillo que recorre una barra de vez en cuando: el trabajo que se esta haciendo.</summary>
    public sealed class BrilloQueRecorre : MonoBehaviour {
        private RectTransform _brillo;
        private float _fase;

        private void Start() {
            var rt = (RectTransform)transform;
            if (GetComponent<RectMask2D>() == null) gameObject.AddComponent<RectMask2D>();
            _brillo = new GameObject("Brillo", typeof(RectTransform)).GetComponent<RectTransform>();
            _brillo.SetParent(rt, false);
            var le = _brillo.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            _brillo.anchorMin = new Vector2(0, 0);
            _brillo.anchorMax = new Vector2(0, 1);
            _brillo.pivot = new Vector2(0.5f, 0.5f);
            _brillo.sizeDelta = new Vector2(40, 0);
            var img = _brillo.gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.35f);
            img.raycastTarget = false;
            _fase = UnityEngine.Random.value * 2.6f;
        }

        private void Update() {
            if (_brillo == null) return;
            var ancho = ((RectTransform)transform).rect.width;
            var p = Mathf.Repeat(Time.unscaledTime + _fase, 2.6f) / 1.1f;
            _brillo.gameObject.SetActive(p <= 1);
            _brillo.anchoredPosition = new Vector2(Mathf.Lerp(-40, ancho + 40, p), 0);
        }
    }

    /// <summary>Algo que sube y se apaga en bucle (el vapor del cafe, las burbujas de la lata).</summary>
    public sealed class SubeEnBucle : MonoBehaviour {
        public float Fase, Altura = 14, Periodo = 1.6f;
        private Vector2 _base;
        private CanvasGroup _g;

        private void Start() {
            _base = ((RectTransform)transform).anchoredPosition;
            _g = gameObject.AddComponent<CanvasGroup>();
            _g.blocksRaycasts = false;
        }

        private void Update() {
            var p = Mathf.Repeat(Time.unscaledTime / Periodo + Fase, 1f);
            ((RectTransform)transform).anchoredPosition = _base + new Vector2(Mathf.Sin(p * Mathf.PI * 2) * 1.5f, p * Altura);
            _g.alpha = Mathf.Sin(p * Mathf.PI);
        }
    }

    /// <summary>Flota arriba y abajo unos pixeles, despacio (la imagen de un suministro).</summary>
    public sealed class FlotaEnBucle : MonoBehaviour {
        public float Altura = 3f, Periodo = 3f;
        private Vector2 _base;
        private float _fase;
        private bool _listo;

        private void Update() {
            var rt = (RectTransform)transform;
            if (!_listo) { _base = rt.anchoredPosition; _fase = UnityEngine.Random.value * 6f; _listo = true; }
            rt.anchoredPosition = _base + new Vector2(0, Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / Periodo + _fase) * Altura);
        }
    }

    /// <summary>Un bamboleo lento de unos grados (la porcion de pizza).</summary>
    public sealed class Bamboleo : MonoBehaviour {
        private void Update() { transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.6f) * 3f); }
    }

    /// <summary>Un dibujo que se traza al aparecer (DibujoUI.Avance de 0 a 1 en ~0,6 s, ease-out).</summary>
    public sealed class DibujarseAlAbrir : MonoBehaviour {
        public float Retraso = 0.15f;
        private float _t;
        private DibujoUI _d;

        private void Awake() {
            _d = GetComponentInChildren<DibujoUI>();
            if (_d != null) _d.Avance = 0;
        }

        private void Update() {
            _t += Time.unscaledDeltaTime;
            var p = Mathf.Clamp01((_t - Retraso) / 0.6f);
            if (_d != null) _d.Avance = ContextoDeHallazgo.Suave(p);
            if (p >= 1) Destroy(this);
        }
    }

    /// <summary>Avisa al entrar y salir el raton (para encender lo relacionado: la cara de quien la lleva, sus tarjetas).</summary>
    public sealed class AlPasar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public event Action Entra, Sale;
        public void OnPointerEnter(PointerEventData e) { if (FichaArrastrable.EnVuelo == null) Entra?.Invoke(); }
        public void OnPointerExit(PointerEventData e) { Sale?.Invoke(); }
        private void OnDisable() { Sale?.Invoke(); }
    }

    /// <summary>
    /// Un tubo que se llena con lo que se mete (el sprint, lo comprometido), con la raya de lo que cabe. Pasada la raya
    /// se vuelve ambar y, al cruzarla, da una sacudida. Recuerda por donde iba (por su nombre) para crecer desde ahi
    /// al repintar, y no desde cero.
    /// </summary>
    public sealed class MedidorDeCapacidad : MonoBehaviour {
        private static readonly Dictionary<string, float> Ultimos = new Dictionary<string, float>();

        private string _clave;
        private bool _vertical;
        private RectTransform _relleno, _raya;
        private Image _imgRelleno;
        private TMP_Text _cifra;
        private NexusTheme _tema;
        private float _actual, _objetivo, _capacidad01, _sacudida;
        private string _unidad;

        public static MedidorDeCapacidad Crear(UiKit ui, Transform padre, string clave, bool vertical, float largo, float grueso, string unidad = "pts") {
            var tema = ui.Tema;
            var raiz = ui.Columna(padre, "Medidor", 6, 0, TextAnchor.MiddleCenter);
            var tubo = ui.Nodo(raiz, "Tubo");
            UiKit.Tamano(tubo, vertical ? grueso : largo, vertical ? largo : grueso);
            var fondo = tubo.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, tema.surfaceSunken, grueso / 2f / tema.escala);
            ui.Borde(tubo, tema.lineStrong, grueso / 2f / tema.escala, 1.5f);

            var m = raiz.gameObject.AddComponent<MedidorDeCapacidad>();
            m._clave = clave;
            m._vertical = vertical;
            m._tema = tema;
            m._unidad = unidad;
            m._relleno = ui.Nodo(tubo, "Relleno");
            m._relleno.anchorMin = Vector2.zero;
            m._relleno.anchorMax = vertical ? new Vector2(1, 0) : new Vector2(0, 1);
            m._relleno.pivot = Vector2.zero;
            m._relleno.offsetMin = new Vector2(3, 3);
            m._relleno.offsetMax = new Vector2(-3, -3);
            m._imgRelleno = m._relleno.gameObject.AddComponent<Image>();
            ui.Fondo(m._imgRelleno, tema.success, (grueso - 6) / 2f / tema.escala);
            m._imgRelleno.raycastTarget = false;

            m._raya = ui.Nodo(tubo, "Cabe");
            m._raya.anchorMin = new Vector2(0, 0);
            m._raya.anchorMax = vertical ? new Vector2(1, 0) : new Vector2(0, 1);
            m._raya.pivot = new Vector2(0.5f, 0.5f);
            m._raya.sizeDelta = vertical ? new Vector2(14, 3) : new Vector2(3, 14);
            var ir = m._raya.gameObject.AddComponent<Image>();
            ir.color = tema.ink;
            ir.raycastTarget = false;

            m._cifra = ui.Texto(raiz, "", EstiloTexto.Encabezado, tema.ink, TextAlignmentOptions.Center);
            m._cifra.font = tema.FuenteDisplay;
            m._cifra.textWrappingMode = TextWrappingModes.NoWrap;
            float previo;
            m._actual = clave != null && Ultimos.TryGetValue(clave, out previo) ? previo : 0;
            return m;
        }

        /// <summary>Lo que hay y lo que cabe.</summary>
        public void Fijar(double valor, double capacidad) {
            var tope = Math.Max(1e-3, Math.Max(capacidad * 1.25, valor * 1.05));
            _objetivo = (float)(valor / tope);
            _capacidad01 = (float)(capacidad / tope);
            var pasado = valor > capacidad + 0.5;
            _cifra.text = $"{valor:0.#} / {capacidad:0.#} {_unidad}";
            _cifra.color = pasado ? _tema.warning : _tema.ink;
            Colocar();
        }

        private void Colocar() {
            var largo = _vertical ? ((RectTransform)_relleno.parent).rect.height : ((RectTransform)_relleno.parent).rect.width;
            if (_vertical) {
                _relleno.anchorMax = new Vector2(1, Mathf.Clamp01(_actual));
                _raya.anchoredPosition = new Vector2(0, largo * _capacidad01);
            } else {
                _relleno.anchorMax = new Vector2(Mathf.Clamp01(_actual), 1);
                _raya.anchoredPosition = new Vector2(largo * _capacidad01, 0);
            }
            _imgRelleno.color = _actual > _capacidad01 + 0.001f ? _tema.warning : _tema.success;
        }

        private void Update() {
            if (_tema == null) return;
            var antes = _actual > _capacidad01 + 0.001f;
            _actual = Mathf.MoveTowards(_actual, _objetivo, Time.unscaledDeltaTime * 0.9f);
            var ahora = _actual > _capacidad01 + 0.001f;
            if (ahora && !antes) _sacudida = 1;
            if (_clave != null) Ultimos[_clave] = _actual;
            _sacudida = Mathf.MoveTowards(_sacudida, 0, Time.unscaledDeltaTime * 2.2f);
            transform.localRotation = Quaternion.Euler(0, 0, _sacudida > 0 ? Mathf.Sin(Time.unscaledTime * 50) * 4 * _sacudida : 0);
            Colocar();
        }
    }
}
