using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Grafo;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// El grafo de commits y el diagrama de secuencia, dibujados para jugarse como los demas diagramas (2026-11-20):
    /// lienzo de cristal, puntos numerados sobre lo que hay que revisar, una carta por punto y «Verificar». Antes eran un
    /// «pincha y etiqueta» con reloj, en papel y con una columna de botones: mucho que hacer a la vez.
    ///
    /// Aqui solo se dibuja: cada pieza (un commit, un mensaje, una fila del log) deja su rectangulo en _cajas, su ancla
    /// en _anclas y, si es una flecha, su trazo en _flechas, que es lo que el resto de la pantalla necesita.
    /// </summary>
    public sealed partial class PantallaCorregirDiagrama {
        private const float AnchoDeLaRama = 190f, RadioDelCommit = 17f, AltoDelCommit = 94f;

        private static Color ColorDeLaRama(NexusTheme t, string nombre) {
            switch (nombre) {
                case "verde": return t.success;
                case "mostaza": return t.warning;
                case "gris": return t.inkMuted;
                default: return t.cyan;   // "canal" y lo que no se conozca
            }
        }

        /// <summary>«2026-04-17T09:30» → «17/04 09:30».</summary>
        private static string FechaCorta(string iso) {
            if (string.IsNullOrEmpty(iso) || iso.Length < 16) return iso ?? "";
            return iso.Substring(8, 2) + "/" + iso.Substring(5, 2) + " " + iso.Substring(11, 5);
        }

        private static string DescripcionDeCommit(Commit k) {
            return $"{k.Id} · «{k.Mensaje}», de {k.Autor} el {FechaCorta(k.Fecha)}, en la rama {k.Rama}.";
        }

        // ==================================================================== el grafo de commits

        /// <summary>
        /// El tiempo corre de izquierda a derecha y cada rama es un carril: lo que se hizo en el mismo instante cae en la
        /// misma columna (asi un tramo rehecho se ve justo debajo del original). Cada commit, un circulo con su id y su autor.
        /// </summary>
        private List<RectTransform> PintarGrafoDeCommits(Lamina l, float W, float H, Action<string> alPinchar) {
            var creados = new List<RectTransform>();
            var art = Def.Artefacto;
            if (art.Commits.Count == 0) return creados;

            var instantes = art.Commits.Select(c => c.Fecha ?? "").Distinct().OrderBy(f => f, StringComparer.Ordinal).ToList();
            var carriles = Math.Max(1, art.Ramas.Count);
            var xIni = MargenX + AnchoDeLaRama;
            var libreX = W - MargenX - xIni;
            var colW = Mathf.Clamp(libreX / instantes.Count, 112f, 230f);
            var xInicio = xIni + Mathf.Max(0f, libreX - colW * instantes.Count) / 2f;
            var libreY = H - MargenSup - MargenInf;
            var gapY = Mathf.Clamp(libreY / carriles, 104f, 150f);
            var y0 = MargenSup + 34f + Mathf.Max(0f, (libreY - 34f - gapY * carriles) / 2f) + gapY / 2f - 20f;

            Func<string, int> columna = f => instantes.IndexOf(f ?? "");
            Func<Commit, float> cx = k => xInicio + (columna(k.Fecha) + 0.5f) * colW;
            Func<Commit, float> cy = k => {
                var i = art.Ramas.FindIndex(r => r.Id == k.Rama);
                return y0 + Math.Max(0, i) * gapY;
            };
            Func<Commit, Color> color = k => {
                var r = art.Ramas.FirstOrDefault(x => x.Id == k.Rama);
                return ColorDeLaRama(Tema, r != null ? r.Color : null);
            };
            var porId = art.Commits.ToDictionary(k => k.Id);

            // Las ramas: su nombre a la izquierda y una guia tenue por todo el carril.
            for (var i = 0; i < art.Ramas.Count; i++) {
                var y = y0 + i * gapY;
                var col = ColorDeLaRama(Tema, art.Ramas[i].Color);
                l.Dibujo.Linea(xIni - 20, y, W - MargenX + 10, y, NexusTheme.Alfa(col, 0.14f), 2, true);
                var t = l.Texto(MargenX, y - 15, AnchoDeLaRama - 14, 30, art.Ramas[i].Id, 20, col, TextAlignmentOptions.MidlineLeft, null, true);
                t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 20;
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
                t.raycastTarget = false;
                creados.Add((RectTransform)t.transform);
            }

            // Cuando pasaron las cosas: una hora sobre cada columna.
            for (var c = 0; c < instantes.Count; c++) {
                var t = l.Texto(xInicio + c * colW, y0 - 58, colW, 22, FechaCorta(instantes[c]), 15, Tema.inkMuted, TextAlignmentOptions.Center);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false;
                creados.Add((RectTransform)t.transform);
            }

            // «Mismo instante y mismo mensaje»: no dice que sea un problema, dice donde mirar.
            foreach (var grupo in GrafoLayout.FechasDuplicadas(art)) {
                var ks = grupo.Where(porId.ContainsKey).Select(i => porId[i]).OrderBy(cy).ToList();
                for (var i = 0; i + 1 < ks.Count; i++) {
                    var x = cx(ks[i]);
                    var ya = cy(ks[i]) + RadioDelCommit + 4; var yb = cy(ks[i + 1]) - RadioDelCommit - 4;
                    if (yb <= ya) continue;
                    l.Dibujo.Linea(x, ya, x, yb, NexusTheme.Alfa(Tema.warning, 0.55f), 2, true);
                }
            }

            // Las aristas, de borde a borde del circulo (el relleno del circulo no tapa el trazo).
            foreach (var k in art.Commits)
                foreach (var padre in k.Padres) {
                    Commit p;
                    if (!porId.TryGetValue(padre, out p)) continue;
                    var x1 = cx(p) + RadioDelCommit; var y1 = cy(p);
                    var x2 = cx(k) - RadioDelCommit; var y2 = cy(k);
                    if (Mathf.Approximately(y1, y2)) l.Dibujo.Linea(x1, y1, x2, y2, NexusTheme.Alfa(color(k), 0.85f), 4);
                    else {
                        var mitad = (x2 - x1) / 2f;
                        l.Dibujo.CurvaCubica(new Vector2(x1, y1), new Vector2(x1 + mitad, y1), new Vector2(x2 - mitad, y2), new Vector2(x2, y2),
                                             NexusTheme.Alfa(color(k), 0.85f), 4);
                    }
                }

            // Los commits.
            foreach (var k in art.Commits) {
                var x = cx(k); var y = cy(k); var col = color(k);
                l.Dibujo.Circulo(x, y, RadioDelCommit, col, 4, Tema.bg900);
                if (k.Huerfano) {
                    var m = l.Texto(x - 12, y - 14, 24, 28, "!", 20, Tema.danger, TextAlignmentOptions.Center, null, true);
                    m.raycastTarget = false;
                    creados.Add((RectTransform)m.transform);
                }
                var linea1 = l.Texto(x - colW / 2f + 4, y + RadioDelCommit + 6, colW - 8, 24, k.Id + " · " + k.Autor, 18, Tema.ink, TextAlignmentOptions.Center);
                linea1.font = Tema.FuenteMono;
                linea1.textWrappingMode = TextWrappingModes.NoWrap; linea1.raycastTarget = false;
                creados.Add((RectTransform)linea1.transform);
                var linea2 = l.Texto(x - colW / 2f + 4, y + RadioDelCommit + 30, colW - 8, 24, k.Mensaje, 15, Tema.inkMuted, TextAlignmentOptions.Center);
                linea2.textWrappingMode = TextWrappingModes.NoWrap; linea2.overflowMode = TextOverflowModes.Ellipsis; linea2.raycastTarget = false;
                creados.Add((RectTransform)linea2.transform);

                var caja = new Rect(x - colW / 2f + 6, y - RadioDelCommit - 12, colW - 12, AltoDelCommit);
                _cajas[k.Id] = caja;
                _rectsDePieza[k.Id] = caja;
                _anclas[k.Id] = new Vector2(x + RadioDelCommit + 12, y - RadioDelCommit - 14);
                var id = k.Id;
                if (alPinchar != null) l.Zona(caja.x, caja.y, caja.width, caja.height, () => alPinchar(id), "Commit " + id);
            }
            return creados;
        }

        // ==================================================================== el diagrama de secuencia

        /// <summary>
        /// A la izquierda lo que se diseño (participantes con su linea de vida y los mensajes en orden) y a la derecha lo que
        /// paso de verdad (el log, una fila por mensaje). Los puntos caen sobre las filas del log y sobre los mensajes.
        /// </summary>
        private List<RectTransform> PintarSecuencia(Lamina l, float W, float H, Action<string> alPinchar) {
            var creados = new List<RectTransform>();
            var art = Def.Artefacto;
            var partes = art.Elementos.Where(e => e.Grupo == "diagrama").OrderBy(e => e.Columna).ToList();
            var mensajes = art.Conexiones.Where(c => c.Grupo == "diagrama").OrderBy(c => c.Orden).ToList();
            var traza = art.Elementos.Where(e => e.Grupo == "traza").OrderBy(e => e.Fila).ToList();
            if (partes.Count == 0 && traza.Count == 0) return creados;

            var top = MargenSup + 30f;
            var libre = Mathf.Max(300f, H - MargenSup - MargenInf - 30f);
            var anchoIzq = (W - 2 * MargenX - 70f) * 0.45f;
            var xIzq = MargenX;
            var xDer = MargenX + anchoIzq + 70f;
            var anchoDer = W - MargenX - xDer;

            Action<float, float, string, Color> titulo = (x, ancho, texto, col) => {
                var t = l.Texto(x, top - 44, ancho, 26, texto, 17, col, TextAlignmentOptions.MidlineLeft, null, true);
                t.characterSpacing = 3; t.raycastTarget = false;
                creados.Add((RectTransform)t.transform);
            };
            titulo(xIzq, anchoIzq, "LO QUE SE DISEÑÓ", Tema.cyan);
            titulo(xDer, anchoDer, "LO QUE PASÓ DE VERDAD", Tema.warning);

            // ---- izquierda: participantes, lineas de vida y mensajes
            var celda = anchoIzq / Math.Max(1, partes.Count);
            var cajaW = Mathf.Min(celda - 14f, 160f); const float cajaH = 50f;
            var xDe = new Dictionary<string, float>();
            for (var i = 0; i < partes.Count; i++) {
                var x = xIzq + celda * (i + 0.5f);
                xDe[partes[i].Id] = x;
                l.Dibujo.Linea(x, top + cajaH, x, top + libre, NexusTheme.Alfa(Tema.cyan, 0.3f), 2, true);
                l.Dibujo.Rect(x - cajaW / 2, top, cajaW, cajaH, Tema.cyan, 3, NexusTheme.Alfa(Tema.bg900, 0.95f), false, 12);
                var t = l.Texto(x - cajaW / 2 + 8, top, cajaW - 16, cajaH, partes[i].Texto, 20, Tema.ink, TextAlignmentOptions.Center, null, true);
                t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 20; t.raycastTarget = false;
                creados.Add((RectTransform)t.transform);
            }
            var paso = Mathf.Max(60f, (libre - cajaH - 16f) / Math.Max(1, mensajes.Count));
            var flecha = NexusTheme.Alfa(Tema.cyan, 0.75f);
            for (var k = 0; k < mensajes.Count; k++) {
                var m = mensajes[k];
                float xa, xb;
                if (!xDe.TryGetValue(m.Desde ?? "", out xa) || !xDe.TryGetValue(m.Hasta ?? "", out xb)) continue;
                var y = top + cajaH + 22f + paso * (k + 0.55f);
                var dir = Mathf.Sign(xb - xa);
                var p1 = new Vector2(xa + 5 * dir, y); var p2 = new Vector2(xb - 5 * dir, y);
                l.Dibujo.Flecha(p1.x, p1.y, p2.x, p2.y, flecha, 3);
                var texto = (k + 1) + " · " + m.Texto;
                var ancho = l.AnchoDePastilla(texto, 17);
                var medio = (p1.x + p2.x) / 2f;
                var pastilla = l.Pastilla(medio, y - 21, texto, 17, NexusTheme.Alfa(Tema.bg950, 0.92f), Tema.inkMuted);
                creados.Add(pastilla);
                _flechas[m.Id] = new[] { p1, p2 };
                var rect = new Rect(medio - ancho / 2f, y - 38, ancho, 40);
                _rectsDePieza[m.Id] = rect;
                _anclas[m.Id] = new Vector2(rect.xMax + MarcadorDePunto.Radio + 6, y - 21);
                var id = m.Id;
                var izq = Mathf.Min(rect.x, Mathf.Min(p1.x, p2.x)); var der = Mathf.Max(rect.xMax, Mathf.Max(p1.x, p2.x));
                if (alPinchar != null) l.Zona(izq, y - 38, der - izq, 46, () => alPinchar(id), "Mensaje " + id);
            }

            // ---- derecha: el log como tabla (numero, hora, de → a, con que paso del diseño se corresponde, que paso)
            var filas = FilasDeTraza(traza);
            var parejas = ParejasEnElDiseno(filas, mensajes);
            var alto = Mathf.Clamp(libre / Math.Max(1, traza.Count), 56f, 80f);
            for (var i = 0; i < traza.Count; i++) {
                var e = traza[i];
                var f = filas[i];
                var y = top + i * alto;
                var caja = new Rect(xDer, y, anchoDer, alto - 12f);
                l.Dibujo.Rect(caja.x, caja.y, caja.width, caja.height, Tema.lineStrong, 2.5f, NexusTheme.Alfa(Tema.bg900, 0.95f), false, 12);
                Func<float, float, string, float, Color, TextAlignmentOptions, TMP_Text> columna = (dx, ancho, texto, tam, col, al) => {
                    var t = l.Texto(caja.x + dx, caja.y, ancho, caja.height, texto, tam, col, al);
                    t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false;
                    creados.Add((RectTransform)t.transform);
                    return t;
                };
                columna(14, 34, f.N.ToString(), 19, Tema.inkMuted, TextAlignmentOptions.MidlineLeft);
                if (f.De != null) {
                    columna(52, 120, f.Hora, 19, Tema.cyan, TextAlignmentOptions.MidlineLeft).font = Tema.FuenteMono;
                    columna(176, 260, Textos.Humanizar(f.De) + " → " + Textos.Humanizar(f.A), 19, Tema.ink, TextAlignmentOptions.MidlineLeft);
                    int pasoDelDiseno;
                    if (parejas.TryGetValue(f.Id, out pasoDelDiseno)) columna(440, 100, "= paso " + pasoDelDiseno, 17, Tema.inkMuted, TextAlignmentOptions.MidlineLeft);
                    else columna(440, 100, "sin paso", 17, Tema.warning, TextAlignmentOptions.MidlineLeft);
                    var que = l.Texto(caja.x + 548, caja.y, Mathf.Max(120f, caja.width - 548 - 62), caja.height, f.Que, 18, Tema.ink, TextAlignmentOptions.MidlineLeft);
                    que.font = Tema.FuenteMono; que.enableAutoSizing = true; que.fontSizeMin = 12; que.fontSizeMax = 18; que.raycastTarget = false;
                    creados.Add((RectTransform)que.transform);
                } else {
                    var t = l.Texto(caja.x + 52, caja.y, caja.width - 120, caja.height, f.Que, 18, Tema.ink, TextAlignmentOptions.MidlineLeft);
                    t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = 18; t.raycastTarget = false;
                    creados.Add((RectTransform)t.transform);
                }
                _cajas[e.Id] = caja;
                _rectsDePieza[e.Id] = caja;
                _anclas[e.Id] = new Vector2(caja.xMax - 22, caja.y - 4);
                var id = e.Id;
                if (alPinchar != null) l.Zona(caja.x, caja.y, caja.width, caja.height, () => alPinchar(id), "Fila " + id);
            }
            return creados;
        }

        private sealed class FilaDeTraza { public string Id, Hora, De, A, Que; public int N; }

        /// <summary>Cada linea del log («07:58:02  garita  → acceso   registrarEntrada(ABC-123)») partida en sus columnas.</summary>
        private static List<FilaDeTraza> FilasDeTraza(List<Elemento> traza) {
            var re = new Regex(@"^(\S+)\s+(\S+)\s+→\s+(\S+)\s+(.*)$");
            return traza.Select(e => {
                var m = re.Match(e.Texto ?? "");
                return m.Success
                    ? new FilaDeTraza { Id = e.Id, N = e.Fila, Hora = m.Groups[1].Value, De = m.Groups[2].Value, A = m.Groups[3].Value, Que = m.Groups[4].Value }
                    : new FilaDeTraza { Id = e.Id, N = e.Fila, Que = e.Texto };
            }).ToList();
        }

        /// <summary>
        /// A que paso del diseño corresponde cada fila del log: el primero sin usar que va de la misma pieza a la misma
        /// pieza. Si el orden del log no es el del diseño se ve en la columna «= paso N»; las filas sin pareja
        /// (pasaron y no estaban en el diseño, o pasaron de mas) dicen «sin paso».
        /// </summary>
        private static Dictionary<string, int> ParejasEnElDiseno(List<FilaDeTraza> filas, List<Conexion> mensajes) {
            var usados = new HashSet<string>();
            var parejas = new Dictionary<string, int>();
            Func<string, string, bool> esLaMisma = (participante, nombre) =>
                participante != null && nombre != null && participante.EndsWith(nombre.Trim(), StringComparison.OrdinalIgnoreCase);
            foreach (var f in filas) {
                var m = mensajes.FirstOrDefault(x => !usados.Contains(x.Id) && esLaMisma(x.Desde, f.De) && esLaMisma(x.Hasta, f.A));
                if (m == null) continue;
                usados.Add(m.Id);
                parejas[f.Id] = m.Orden;
            }
            return parejas;
        }

        // ==================================================================== que los #N no queden pegados

        /// <summary>
        /// Separa los puntos (#1, #2…) para que ninguno quede pegado a otro: cada uno ocupa su disco y, encima, la chapa de
        /// la carta que se le juegue. Si dos se solapan, se apartan por el eje que menos obliga (hasta un tope, para que no se
        /// alejen de su pieza). Se llama una vez pintado el diagrama, antes de crear los marcadores.
        /// </summary>
        private void SepararAnclas(float W, float H) {
            const float anchoChapa = 250f, altoChapa = 44f, hueco = 14f, tope = 170f;
            var radio = MarcadorDePunto.Radio;
            var piezas = _puntos.Select(p => p.Pieza ?? "").Where(_anclas.ContainsKey).Distinct().ToList();
            if (piezas.Count < 2) return;
            var origen = piezas.ToDictionary(p => p, p => _anclas[p]);
            // La huella de un punto: la chapa (arriba) y el disco, con un hueco alrededor.
            Func<Vector2, Rect> huella = c => new Rect(c.x - anchoChapa / 2 - hueco / 2, c.y - radio - 8 - altoChapa - hueco / 2,
                                                       anchoChapa + hueco, radio + 8 + altoChapa + radio + hueco);
            for (var vuelta = 0; vuelta < 80; vuelta++) {
                var movido = false;
                for (var i = 0; i < piezas.Count; i++)
                    for (var j = i + 1; j < piezas.Count; j++) {
                        var a = _anclas[piezas[i]]; var b = _anclas[piezas[j]];
                        var ra = huella(a); var rb = huella(b);
                        if (!ra.Overlaps(rb)) continue;
                        var ox = Mathf.Min(ra.xMax, rb.xMax) - Mathf.Max(ra.xMin, rb.xMin);
                        var oy = Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin);
                        if (oy <= ox) {
                            var s = b.y >= a.y ? 1f : -1f;
                            a.y -= s * (oy / 2f + 0.5f); b.y += s * (oy / 2f + 0.5f);
                        } else {
                            var s = b.x >= a.x ? 1f : -1f;
                            a.x -= s * (ox / 2f + 0.5f); b.x += s * (ox / 2f + 0.5f);
                        }
                        _anclas[piezas[i]] = a; _anclas[piezas[j]] = b;
                        movido = true;
                    }
                // Dentro del lienzo, y sin irse lejos de donde empezaron.
                foreach (var p in piezas) {
                    var c = _anclas[p]; var o = origen[p];
                    c.x = Mathf.Clamp(Mathf.Clamp(c.x, o.x - tope, o.x + tope), anchoChapa / 2 + 6, Mathf.Max(anchoChapa / 2 + 6, W - anchoChapa / 2 - 6));
                    c.y = Mathf.Clamp(Mathf.Clamp(c.y, o.y - tope, o.y + tope), radio + 8 + altoChapa + 6, Mathf.Max(radio + 60, H - radio - 6));
                    _anclas[p] = c;
                }
                if (!movido) break;
            }
        }
    }
}
