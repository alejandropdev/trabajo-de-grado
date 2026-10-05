using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Detectar;
using Nexus.Core.Minijuegos.Grafo;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>Como se pinta cada pieza del artefacto, en juego y en el cierre.</summary>
    public enum EstadoPieza { Normal, Candidata, Seleccionada, Marcada, Acertada, Fallada, Senuelo }

    /// <summary>
    /// V1 · Detectar. Un artefacto con defectos entre señuelos: se pinchan piezas y se les pone nombre; marcar lo que
    /// esta bien tambien cuesta. Cada lienzo se dibuja como lo que es:
    ///   · grafo_commits — el grafo git, un carril por rama, las aristas padre → hijo, y el diff al lado
    ///   · diagrama      — cajas (externo, componente, datos) y flechas con su texto, las dos pulsables
    ///   · secuencia     — el diagrama de secuencia diseñado, y al lado lo que paso de verdad como TABLA
    /// En el cierre se vuelven a pintar enseñando la solucion: lo que acertaste en cian, lo que se te escapo en rojo,
    /// y los señuelos en gris.
    /// </summary>
    public sealed class PantallaDetectar : PantallaDeMinijuego {
        private DetectarState _estado;
        private AndamiajeCfg _andamiaje;
        private readonly Dictionary<string, string> _nombres = new Dictionary<string, string>();
        private HashSet<string> _candidatas = new HashSet<string>();
        private Action _repintarLienzo;
        private RectTransform _marcas, _detalle;
        private TMP_Text _seleccion, _aviso;
        private Func<string, EstadoPieza> _estadoDe;
        private string _pista;
        private string _dudaAvisada;
        private readonly Dictionary<string, RectTransform> _piezas = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, Button> _botonesEtiqueta = new Dictionary<string, Button>();
        private Button _entregar, _limpiar;

        protected override void ConstruirJuego(RectTransform cuerpo) {
            _estado = new DetectarState(Def.Presentacion.SegundosReloj);
            AndamiajeCfg cfg;
            _andamiaje = Def.Andamiaje != null && Def.Andamiaje.TryGetValue(Pendiente.NivelAndamiaje.ToString(), out cfg) ? cfg : null;
            if (_andamiaje != null && _andamiaje.ResaltarZonasCandidatas)
                _candidatas = new HashSet<string>(Def.Zonas.SelectMany(z => z.Commits));
            // La ayuda de un compañero: una pieza con un problema de verdad, señalada.
            if (Pendiente != null && Pendiente.TieneAyuda(Nexus.Core.Relaciones.TiposDeAyuda.PistaDetectar) && Def.Zonas.Count > 0)
                _pista = Def.Zonas[0].Commits.FirstOrDefault();
            foreach (var e in Def.Artefacto.Elementos) _nombres[e.Id] = e.Texto;
            foreach (var c in Def.Artefacto.Commits) _nombres[c.Id] = c.Id + " · " + c.Mensaje;
            foreach (var c in Def.Artefacto.Conexiones) _nombres[c.Id] = "flecha: " + c.Texto;

            _estadoDe = EstadoEnJuego;
            ConstruirLienzos(cuerpo);
            ConstruirPanelDeMarcas(cuerpo);
            Repintar();
        }

        private void ConstruirLienzos(RectTransform cuerpo) {
            GuiaView.Registrar("mj.detectar.tablero", cuerpo);
            switch (Def.Lienzo) {
                case "grafo_commits": {
                    var l = NuevoLienzo(cuerpo, "El histórico · cada columna es una rama, cada punto un commit", 1335, 90 + Def.Artefacto.Commits.Count * 62);
                    _repintarLienzo = () => PintarGrafo(l);
                    break;
                }
                case "secuencia": {
                    var a = NuevoLienzo(cuerpo, "Lo que se diseñó", 620, 680, 640);
                    var b = NuevoLienzo(cuerpo, "Lo que pasó de verdad · momento a momento", 720, 90 + FilasDeTraza().Count * 78);
                    _repintarLienzo = () => { PintarSecuencia(a); PintarTraza(b); };
                    break;
                }
                default: {
                    var filas = Def.Artefacto.Elementos.Count == 0 ? 1 : Def.Artefacto.Elementos.Max(e => e.Fila) + 1;
                    var l = NuevoLienzo(cuerpo, "El diagrama · pincha una caja o una flecha", 1330, 120 + filas * 250);
                    _repintarLienzo = () => PintarDiagrama(l);
                    break;
                }
            }
        }

        // ==================================================================== estado de cada pieza

        private EstadoPieza EstadoEnJuego(string id) {
            if (_estado.Seleccion.Contains(id)) return EstadoPieza.Seleccionada;
            if (_estado.Marcas.Any(m => m.Commits.Contains(id))) return EstadoPieza.Marcada;
            if (_candidatas.Contains(id) || id == _pista) return EstadoPieza.Candidata;
            return EstadoPieza.Normal;
        }

        private EstadoPieza EstadoEnElCierre(string id) {
            var zona = Def.Zonas.FirstOrDefault(z => z.Commits.Contains(id));
            if (zona != null)
                return Resultado.Traza.Any(t => t.ZonaAcertada == zona.Id) ? EstadoPieza.Acertada : EstadoPieza.Fallada;
            if (Def.Senuelos.Any(s => s.Commits.Contains(id))) return EstadoPieza.Senuelo;
            return EstadoPieza.Normal;
        }

        private string EtiquetaDe(string id) {
            var m = _estado.Marcas.LastOrDefault(x => x.Commits.Contains(id));
            return m == null ? null : Textos.Humanizar(m.Etiqueta);
        }

        private Color ColorDe(EstadoPieza e, Color normal) {
            switch (e) {
                case EstadoPieza.Seleccionada: return Tema.papelCyan;
                case EstadoPieza.Marcada: return Tema.papelAviso;
                case EstadoPieza.Candidata: return Color.Lerp(normal, Tema.papelAviso, 0.45f);
                case EstadoPieza.Acertada: return Tema.papelExito;
                case EstadoPieza.Fallada: return Tema.papelPeligro;
                case EstadoPieza.Senuelo: return Tema.paperMuted;
                default: return normal;
            }
        }

        private Color? FondoDeFila(EstadoPieza e) {
            switch (e) {
                case EstadoPieza.Seleccionada: return NexusTheme.Alfa(Tema.papelCyan, 0.16f);
                case EstadoPieza.Marcada: return NexusTheme.Alfa(Tema.papelAviso, 0.18f);
                case EstadoPieza.Acertada: return NexusTheme.Alfa(Tema.papelExito, 0.16f);
                case EstadoPieza.Fallada: return NexusTheme.Alfa(Tema.papelPeligro, 0.16f);
                case EstadoPieza.Senuelo: return NexusTheme.Alfa(Tema.paperInk, 0.05f);
                default: return null;
            }
        }

        /// <summary>La chapita junto a una pieza: «!» si esta marcada; en el cierre, √ o ×.</summary>
        private void Chapita(Lamina l, float x, float y, EstadoPieza e) {
            string s; Color c;
            switch (e) {
                case EstadoPieza.Marcada: s = "!"; c = Tema.papelAviso; break;
                case EstadoPieza.Acertada: s = "√"; c = Tema.papelExito; break;
                case EstadoPieza.Fallada: s = "×"; c = Tema.papelPeligro; break;
                default: return;
            }
            l.Dibujo.Circulo(x, y, 15, c, 0, c);
            l.Texto(x - 15, y - 15, 30, 30, s, 20, Color.white, TextAlignmentOptions.Center, null, true);
        }

        /// <summary>
        /// Pinchar una pieza la mira y la selecciona. En un diagrama o una secuencia, pinchar OTRA cambia la selección
        /// a esa sola: antes se acumulaban, y una marca podía llevar a la vez el problema y un señuelo (y salir mal
        /// aunque el jugador creyera haberlo hecho bien). Solo el grafo de git selecciona varias, porque un problema
        /// puede ser un tramo de commits.
        /// </summary>
        private void Pinchar(string id, Action alMirar) {
            if (Resultado != null) return;
            alMirar?.Invoke();
            if (Guiado) {
                if (!Guia.Permite(AccionGuiada.Seleccionar, id)) { Guia.Rechazar(); Repintar(); return; }
                if (!_estado.Seleccion.Contains(id)) _estado.Alternar(id);
                Guia.Hecho(AccionGuiada.Seleccionar, id);
                Repintar();
                return;
            }
            if (Def.Lienzo == "grafo_commits" || _estado.Seleccion.Contains(id)) _estado.Alternar(id);
            else { _estado.Seleccion.Clear(); _estado.Alternar(id); }
            Repintar();
        }

        // ==================================================================== el grafo de commits

        private void PintarGrafo(Lamina l) {
            Limpiar(l);
            var art = Def.Artefacto;
            var layout = GrafoLayout.Calcular(art);
            const float W = 1330;
            Func<int, float> X = i => 70 + i * 110;
            Func<int, float> Y = i => 70 + i * 62;
            var colorDeRama = new Dictionary<string, Color>();
            for (var i = 0; i < art.Ramas.Count; i++) {
                var r = art.Ramas[i];
                colorDeRama[r.Id] = ColorDeRama(r.Color);
                l.Texto(X(i) - 8, i % 2 == 1 ? 18 : 0, 260, 22, r.Id, 15, colorDeRama[r.Id]);
            }
            Func<string, Color> colorDe = id => {
                var c = art.Commits.First(x => x.Id == id);
                Color col; return colorDeRama.TryGetValue(c.Rama ?? "", out col) ? col : Tema.papelCyan;
            };

            foreach (var n in layout.Nodos) {
                var fondo = FondoDeFila(_estadoDe(n.CommitId));
                if (fondo.HasValue) l.Dibujo.Rect(20, Y(n.Fila) - 26, W - 40, 52, fondo.Value, 0, fondo.Value);
            }
            foreach (var a in layout.Aristas) {
                var p = layout.Por(a.DesdeId); var h = layout.Por(a.HastaId);
                var x1 = X(p.Carril); var y1 = Y(p.Fila); var x2 = X(h.Carril); var y2 = Y(h.Fila);
                var col = colorDe(a.HastaId);
                if (Mathf.Approximately(x1, x2)) l.Dibujo.Linea(x1, y1, x2, y2, col, 4);
                else l.Dibujo.CurvaCubica(new Vector2(x1, y1), new Vector2(x1, (y1 + y2) / 2), new Vector2(x2, (y1 + y2) / 2), new Vector2(x2, y2), col, 4);
            }
            foreach (var n in layout.Nodos) {
                var c = art.Commits.First(x => x.Id == n.CommitId);
                var e = _estadoDe(c.Id);
                var x = X(n.Carril); var y = Y(n.Fila);
                l.Dibujo.Circulo(x, y, 13, e == EstadoPieza.Seleccionada ? Tema.papelCyan : colorDe(c.Id), e == EstadoPieza.Seleccionada ? 6 : 4, Tema.surface);
                if (c.Huerfano) l.Texto(x - 10, y - 12, 20, 24, "!", 18, Tema.papelPeligro, TextAlignmentOptions.Center, null, true);
                Chapita(l, 460, y, e);
                var mono = l.Texto(520, y - 14, 70, 28, c.Id, 18, Tema.papelCyan);
                mono.font = Tema.FuenteMono;
                l.Texto(590, y - 14, 520, 28, c.Mensaje, 19, Tema.paperInk);
                l.Texto(W - 260, y - 13, 200, 26, $"{c.Autor} · {(c.Fecha ?? "").Replace('T', ' ').Substring(Math.Min(5, (c.Fecha ?? "").Length))}", 16,
                        Tema.paperMuted, TextAlignmentOptions.MidlineRight);
                var id = c.Id;
                _piezas[id] = (RectTransform)l.Zona(20, y - 26, W - 40, 52, () => Pinchar(id, () => MostrarCommit(c))).transform;
            }
        }

        private Color ColorDeRama(string nombre) {
            switch (nombre) {
                case "canal": return Tema.papelCyan;
                case "verde": return Tema.papelExito;
                case "mostaza": return Tema.papelAviso;
                case "gris": return Tema.paperMuted;
                default: return Tema.papelCyan;
            }
        }

        // ==================================================================== el diagrama de componentes

        private void PintarDiagrama(Lamina l) {
            Limpiar(l);
            var els = Def.Artefacto.Elementos;
            if (els.Count == 0) return;
            const float W = 1330, BW = 250, BH = 96;
            var cols = els.Max(e => e.Columna) + 1;
            var gx = (W - cols * BW) / (cols + 1);
            var pos = els.ToDictionary(e => e.Id, e => new Vector2(gx + e.Columna * (BW + gx), 70 + e.Fila * 250));
            // Cada caja lleva debajo lo que hace (su «detalle»), a la vista: antes solo se leia pinchandola, y las
            // cajas eran nombres sueltos (ronda 4). Las etiquetas de las flechas esquivan tambien ese texto.
            const float AltoDetalle = 64;
            var ocupados = els.Select(e => new Rect(pos[e.Id].x, pos[e.Id].y, BW, BH + (string.IsNullOrEmpty(e.Detalle) ? 0 : AltoDetalle))).ToList();

            var pastillas = new List<Action>();
            foreach (var c in Def.Artefacto.Conexiones) {
                Vector2 pa, pb;
                if (!pos.TryGetValue(c.Desde ?? "", out pa) || !pos.TryGetValue(c.Hasta ?? "", out pb)) continue;
                var a = pa + new Vector2(BW / 2, BH / 2); var b = pb + new Vector2(BW / 2, BH / 2);
                var p1 = Corte(a, b); var p2 = Corte(b, a);
                var e = _estadoDe(c.Id);
                var col = e == EstadoPieza.Normal ? Tema.paperMuted : ColorDe(e, Tema.paperMuted);
                l.Dibujo.Flecha(p1.x, p1.y, p2.x, p2.y, col, e == EstadoPieza.Normal ? 3 : 4);
                var ancho = c.Texto.Length * 16 * 0.55f + 26;
                var hueco = Hueco(p1, p2, ancho, ocupados);
                var con = c;
                pastillas.Add(() => {
                    Color fondo = Tema.papelCaja, texto = Tema.paperInk;
                    if (e == EstadoPieza.Seleccionada) { fondo = Tema.papelCyan; texto = Color.white; }
                    else if (e == EstadoPieza.Marcada) { fondo = Tema.papelAviso; texto = Color.white; }
                    else if (e != EstadoPieza.Normal && e != EstadoPieza.Candidata) { fondo = ColorDe(e, Tema.papelCaja); texto = Color.white; }
                    l.Pastilla(hueco.x, hueco.y, con.Texto, 16, fondo, texto);
                    _piezas[con.Id] = (RectTransform)l.Zona(hueco.x - ancho / 2, hueco.y - 15, ancho, 30, () => Pinchar(con.Id, () => MostrarTexto("Flecha", con.Texto))).transform;
                });
            }
            foreach (var p in pastillas) p();

            foreach (var el in els) {
                var p = pos[el.Id];
                var e = _estadoDe(el.Id);
                var normal = el.Tipo == "externo" ? Tema.paperMuted : Tema.papelCyan;
                var borde = ColorDe(e, normal);
                var grosor = e == EstadoPieza.Seleccionada ? 5 : 3;
                var fondo = e == EstadoPieza.Seleccionada ? NexusTheme.Alfa(Tema.papelCyan, 0.16f) : Tema.papelCaja;
                if (el.Tipo == "baseDeDatos") l.Dibujo.Cilindro(p.x, p.y, BW, BH, borde, grosor, Tema.papelCaja);
                else l.Dibujo.Rect(p.x, p.y, BW, BH, borde, grosor, fondo, el.Tipo == "externo", 10);
                var db = el.Tipo == "baseDeDatos";
                l.Texto(p.x + 16, p.y + (db ? 30 : 12), BW - 30, 20, TipoDe(el.Tipo), 13, Tema.paperMuted);
                l.Texto(p.x + 16, p.y + (db ? 50 : 38), BW - 30, 50, el.Texto, 20, Tema.paperInk, TextAlignmentOptions.TopLeft, null, true);
                if (!string.IsNullOrEmpty(el.Detalle))
                    l.Texto(p.x + 4, p.y + BH + 6, BW - 8, AltoDetalle, el.Detalle, 14, Tema.paperMuted, TextAlignmentOptions.TopLeft);
                Chapita(l, p.x + BW - 6, p.y, e);
                var elem = el;
                _piezas[elem.Id] = (RectTransform)l.Zona(p.x, p.y, BW, BH, () => Pinchar(elem.Id, () => MostrarTexto(elem.Texto, elem.Detalle))).transform;
            }
        }

        private static string TipoDe(string tipo) {
            switch (tipo) {
                case "externo": return "↔ EXTERNO";
                case "baseDeDatos": return "DATOS";
                case "componente": return "■ COMPONENTE";
                default: return Textos.Humanizar(tipo).ToUpperInvariant();
            }
        }

        private static Vector2 Corte(Vector2 desde, Vector2 hacia) {
            const float BW = 250, BH = 96;
            var d = hacia - desde;
            var tx = Mathf.Abs(d.x) > 0.01f ? (BW / 2 + 8) / Mathf.Abs(d.x) : 1e9f;
            var ty = Mathf.Abs(d.y) > 0.01f ? (BH / 2 + 8) / Mathf.Abs(d.y) : 1e9f;
            return desde + d * Mathf.Min(tx, ty);
        }

        /// <summary>Donde poner la etiqueta de una flecha para que no pise ni cajas ni otras etiquetas.</summary>
        private static Vector2 Hueco(Vector2 a, Vector2 b, float ancho, List<Rect> ocupados) {
            foreach (var t in new[] { .5f, .35f, .65f, .25f, .75f, .18f, .82f }) {
                var p = Vector2.Lerp(a, b, t);
                var r = new Rect(p.x - ancho / 2 - 6, p.y - 19, ancho + 12, 38);
                if (!ocupados.Any(o => o.Overlaps(r))) { ocupados.Add(r); return p; }
            }
            var m = (a + b) / 2;
            ocupados.Add(new Rect(m.x - ancho / 2, m.y - 15, ancho, 30));
            return m;
        }

        // ==================================================================== la secuencia contra la traza

        private sealed class FilaDeTraza { public string Id, Hora, De, A, Que; public int N; }

        private List<FilaDeTraza> FilasDeTraza() {
            var re = new Regex(@"^(\S+)\s+(\S+)\s+→\s+(\S+)\s+(.*)$");
            return Def.Artefacto.Elementos.Where(e => e.Grupo == "traza").OrderBy(e => e.Fila).Select(e => {
                var m = re.Match(e.Texto ?? "");
                return m.Success
                    ? new FilaDeTraza { Id = e.Id, N = e.Fila, Hora = m.Groups[1].Value, De = m.Groups[2].Value, A = m.Groups[3].Value, Que = m.Groups[4].Value }
                    : new FilaDeTraza { Id = e.Id, N = e.Fila, Que = e.Texto };
            }).ToList();
        }

        private void PintarSecuencia(Lamina l) {
            Limpiar(l);
            var parts = Def.Artefacto.Elementos.Where(e => e.Grupo == "diagrama").OrderBy(e => e.Columna).ToList();
            const float alto = 660;
            Func<int, float> X = i => 70 + i * 160;
            var col = new Dictionary<string, int>();
            for (var i = 0; i < parts.Count; i++) {
                col[parts[i].Id] = i;
                l.Dibujo.Rect(X(i) - 62, 10, 124, 46, Tema.papelCyan, 2, Tema.papelCaja);
                l.Texto(X(i) - 62, 10, 124, 46, NombreCorto(parts[i].Texto), 17, Tema.paperInk, TextAlignmentOptions.Center, null, true);
                l.Dibujo.Linea(X(i), 56, X(i), alto - 10, Tema.paperLine, 2, true);
            }
            var mensajes = Def.Artefacto.Conexiones.Where(c => c.Grupo == "diagrama").OrderBy(c => c.Orden).ToList();
            for (var k = 0; k < mensajes.Count; k++) {
                var m = mensajes[k];
                int a, b;
                if (!col.TryGetValue(m.Desde ?? "", out a) || !col.TryGetValue(m.Hasta ?? "", out b)) continue;
                var y = 110 + k * 96;
                var e = _estadoDe(m.Id);
                var fondo = FondoDeFila(e);
                if (fondo.HasValue) l.Dibujo.Rect(10, y - 34, 600, 52, fondo.Value, 0, fondo.Value);
                var retorno = b < a;
                l.Dibujo.Flecha(X(a), y, X(b), y, e == EstadoPieza.Normal ? (retorno ? Tema.paperMuted : Tema.papelCyan) : ColorDe(e, Tema.papelCyan), 3, retorno);
                l.Texto(Mathf.Min(X(a), X(b)) - 40, y - 32, Mathf.Abs(X(b) - X(a)) + 80, 22, m.Orden + ". " + m.Texto, 15, Tema.paperInk, TextAlignmentOptions.Center);
                Chapita(l, 596, y - 8, e);
                var men = m;
                _piezas[men.Id] = (RectTransform)l.Zona(10, y - 34, 600, 52, () => Pinchar(men.Id, () => MostrarTexto($"Paso {men.Orden} del diseño", men.Texto))).transform;
            }
        }

        private void PintarTraza(Lamina l) {
            Limpiar(l);
            var mono = Tema.FuenteMono;
            var cab = new[] { new { t = "#", x = 10f }, new { t = "HORA", x = 44f }, new { t = "QUIÉN", x = 150f }, new { t = "A QUIÉN", x = 290f }, new { t = "QUÉ PASÓ", x = 410f } };
            foreach (var c in cab) l.Texto(c.x, 8, 140, 22, c.t, 15, Tema.papelCyan);
            var filas = FilasDeTraza();
            for (var i = 0; i < filas.Count; i++) {
                var f = filas[i];
                var y = 44 + i * 78;
                var e = _estadoDe(f.Id);
                var fondo = FondoDeFila(e) ?? (i % 2 == 0 ? Tema.papelCaja : new Color(0, 0, 0, 0));
                l.Dibujo.Rect(0, y, 710, 66, e == EstadoPieza.Normal ? new Color(0, 0, 0, 0) : ColorDe(e, Tema.papelCyan), e == EstadoPieza.Normal ? 0 : 2, fondo, false, 6);
                l.Texto(10, y + 20, 30, 26, f.N.ToString(), 18, Tema.paperMuted);
                l.Texto(44, y + 20, 100, 26, f.Hora ?? "", 17, Tema.papelCyan).font = mono;
                if (f.De != null) {
                    l.Pastilla(150 + 50, y + 33, Textos.Humanizar(f.De), 16, Tema.papelCaja, Tema.paperInk);
                    l.Texto(252, y + 18, 30, 30, "→", 22, Tema.paperMuted, TextAlignmentOptions.Center);
                    l.Pastilla(290 + 50, y + 33, Textos.Humanizar(f.A), 16, Tema.papelCaja, Tema.paperInk);
                }
                var que = l.Texto(410, y + 8, 290, 52, f.Que ?? "", 16, Tema.paperInk, TextAlignmentOptions.MidlineLeft);
                que.font = mono;
                Chapita(l, 676, y + 14, e);
                var etiqueta = EtiquetaDe(f.Id);
                if (etiqueta != null && Resultado == null) l.Pastilla(600, y + 52, etiqueta, 13, Tema.papelAviso, Color.white);
                var fila = f;
                _piezas[fila.Id] = (RectTransform)l.Zona(0, y, 710, 66, () => Pinchar(fila.Id, () => MostrarTexto($"Fila {fila.N} · {fila.Hora}", fila.Que))).transform;
            }
        }

        private static string NombreCorto(string texto) {
            if (string.IsNullOrEmpty(texto)) return "";
            if (texto.StartsWith("Módulo de ")) return Textos.Humanizar(texto.Substring(10));
            if (texto == "Base de datos") return "BD";
            return texto;
        }

        private static void Limpiar(Lamina l) {
            for (var i = l.Raiz.childCount - 1; i >= 0; i--) {
                var hijo = l.Raiz.GetChild(i);
                if (hijo.gameObject != l.Dibujo.gameObject) UnityEngine.Object.Destroy(hijo.gameObject);
            }
            l.Dibujo.Limpiar();
        }

        // ==================================================================== las marcas

        private void ConstruirPanelDeMarcas(Transform padre) {
            var panel = Ui.Columna(padre, "Marcas", Tema.espacio);
            UiKit.Tamano(panel, ancho: 420, flexAlto: 1);

            var marcar = Ui.Tarjeta(panel, "2 · ¿Qué problema tiene? Elige el nombre");
            GuiaView.Registrar("mj.detectar.etiquetas", marcar);
            _seleccion = Ui.Texto(marcar, "", EstiloTexto.Pequeno, Tema.ink);
            var etiquetas = _andamiaje != null && _andamiaje.Etiquetas != null && _andamiaje.Etiquetas.Count > 0
                ? _andamiaje.Etiquetas : Def.PaletaEtiquetas;
            foreach (var etiqueta in etiquetas) {
                var e = etiqueta;
                // TagOption. Guiado: cada nombre dice que significa: no se puede elegir bien lo que no se conoce.
                _botonesEtiqueta[e] = Ui.OpcionDeEtiqueta(marcar, Textos.Humanizar(e), TonoDeEtiqueta(e), () => Marcar(e),
                                                          Guiado ? Recetas.Significado(e) : null);
            }
            _limpiar = Ui.Boton(marcar, "Limpiar la selección", () => { _estado.Seleccion.Clear(); _aviso.text = ""; Repintar(); }, VarianteBoton.Fantasma);
            _aviso = Ui.Texto(marcar, "", EstiloTexto.Pequeno, Tema.warning);

            var lista = Ui.PanelColumna(panel, "Lista", Tema.margen * 0.75f, Tema.espacio);
            UiKit.Tamano(lista, flexAlto: 1);
            var titulo = "LO QUE HAS MARCADO" + (_andamiaje != null && _andamiaje.ContadorRestantes ? $" · hay {Def.Zonas.Count} problema(s) en total" : "");
            Ui.Texto(lista, titulo, EstiloTexto.Leyenda, Tema.cyan);
            var scroll = Ui.Desplazable(lista, out _marcas);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            var detalle = Ui.Tarjeta(panel, "Detalle");
            _detalle = Ui.Columna(detalle, "Contenido", 4);
            Ui.Texto(_detalle, "Pincha una pieza para verla de cerca.", EstiloTexto.Pequeno);

            _entregar = Ui.Boton(panel, "Entregar", Entregar, VarianteBoton.Primario);
            GuiaView.Registrar("mj.entregar", _entregar);
        }

        /// <summary>
        /// El color de papel de una etiqueta. Los datos no dicen su gravedad, asi que se reparte de forma estable por su
        /// id (siempre el mismo color para la misma etiqueta): basta para distinguirlas de un vistazo, como en las
        /// referencias, sin insinuar que una es peor que otra.
        /// </summary>
        private static Tono TonoDeEtiqueta(string id) {
            var h = 0;
            foreach (var c in id ?? "") h = (h * 31 + c) & 0x7fffffff;
            switch (h % 3) { case 0: return Tono.Cyan; case 1: return Tono.Aviso; default: return Tono.Peligro; }
        }

        private void Marcar(string etiqueta) {
            if (_estado.Seleccion.Count == 0) {
                _aviso.text = "Primero pincha en el tablero (paso 1) lo que tiene el problema.";
                return;
            }
            if (Guiado && !Guia.Permite(AccionGuiada.Etiquetar, etiqueta)) { Guia.Rechazar(); return; }
            // Con andamiaje de tutorial, acusar una pieza sana no pasa en silencio: Marisol duda una vez, y es el
            // jugador quien decide si la marca igual (pulsando otra vez) o la suelta. Antes solo se sabia al entregar.
            if (!Guiado && Andamiaje >= 3) {
                var dudosa = string.Join("|", _estado.Seleccion);
                if (dudosa != _dudaAvisada && Def.Senuelos.Any(s => s.Commits.Any(_estado.Seleccion.Contains))) {
                    _dudaAvisada = dudosa;
                    _aviso.text = "Marisol duda: «¿Seguro que eso es un problema? Mira su detalle otra vez». " +
                                  "Marcar lo que está bien cuenta en contra. Pulsa el nombre de nuevo para marcarlo igual, o «Limpiar la selección».";
                    return;
                }
            }
            _dudaAvisada = null;
            _estado.Marcar(etiqueta, Segundo);
            _aviso.text = "";
            Guia?.Hecho(AccionGuiada.Etiquetar, etiqueta);
            Repintar();
        }

        protected override RectTransform PiezaGuiada(PasoGuiado paso) {
            RectTransform rt;
            switch (paso.Accion) {
                case AccionGuiada.Etiquetar:
                    Button b;
                    return _botonesEtiqueta.TryGetValue(paso.Objetivo ?? "", out b) ? (RectTransform)b.transform : null;
                case AccionGuiada.Entregar: return _entregar != null ? (RectTransform)_entregar.transform : null;
                default: return _piezas.TryGetValue(paso.Objetivo ?? "", out rt) ? rt : null;
            }
        }

        public override void Repintar() {
            if (_estado == null || Resultado != null) return;
            _repintarLienzo?.Invoke();
            _seleccion.text = _estado.Seleccion.Count == 0
                ? "Paso 1: pincha en el tablero una caja o una flecha que creas que está mal. Paso 2: pulsa aquí el nombre de su problema."
                : "Seleccionado: " + string.Join(", ", _estado.Seleccion.Select(Nombre)) + ". Ahora elige qué problema tiene.";
            _limpiar.gameObject.SetActive(!Guiado && _estado.Seleccion.Count > 0);

            UiKit.Vaciar(_marcas);
            for (var i = 0; i < _estado.Marcas.Count; i++) {
                var indice = i;
                var m = _estado.Marcas[i];
                var fila = Ui.PanelColumna(_marcas, "Marca", 8, 4, Tema.surfaceRaised);
                Ui.Texto(fila, Textos.Humanizar(m.Etiqueta), EstiloTexto.Encabezado, Tema.warning);
                Ui.Texto(fila, string.Join(", ", m.Commits.Select(Nombre)), EstiloTexto.Pequeno);
                Ui.Boton(Ui.Fila(fila), "Quitar", () => { _estado.Desmarcar(indice); Repintar(); }, VarianteBoton.Fantasma);
            }
        }

        private string Nombre(string id) {
            string n;
            return _nombres.TryGetValue(id ?? "", out n) ? n : id;
        }

        private void MostrarTexto(string titulo, string texto) {
            UiKit.Vaciar(_detalle);
            Ui.Texto(_detalle, titulo, EstiloTexto.Cuerpo).fontStyle = FontStyles.Bold;
            if (!string.IsNullOrEmpty(texto)) Ui.Texto(_detalle, texto, EstiloTexto.Pequeno, Tema.ink);
        }

        private void MostrarCommit(Commit c) {
            UiKit.Vaciar(_detalle);
            Ui.Texto(_detalle, c.Id + " · " + c.Mensaje, EstiloTexto.Cuerpo).fontStyle = FontStyles.Bold;
            Diff diff;
            if (string.IsNullOrEmpty(c.Diff) || !Def.Artefacto.Diffs.TryGetValue(c.Diff, out diff)) return;
            foreach (var linea in diff.Lineas.Take(10)) {
                var t = Ui.Texto(_detalle, (linea.Tipo == "add" ? "+ " : linea.Tipo == "del" ? "− " : "  ") + linea.Texto, EstiloTexto.Mono);
                t.color = linea.Tipo == "add" ? Tema.success : linea.Tipo == "del" ? Tema.danger : Tema.inkMuted;
            }
        }

        protected override ResultadoMinijuego Evaluar() {
            return DetectarEvaluador.Evaluar(Def, _estado);
        }

        // ==================================================================== el cierre, sobre el propio lienzo

        protected override void PintarResultado(RectTransform zona) {
            _estadoDe = EstadoEnElCierre;
            var fila = Ui.Fila(zona, "Tableros", Tema.espacio, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1);
            ConstruirLienzos(fila);
            _repintarLienzo();

            var leyenda = Ui.Tarjeta(zona, "Qué era cada cosa");
            var ley = Ui.Fila(leyenda, espacio: 8);
            Ui.Badge(ley, "√ lo encontraste", Tono.Exito);
            Ui.Badge(ley, "× se te escapó", Tono.Peligro);
            Ui.Badge(ley, "estaba bien");
            Ui.Texto(leyenda, "La explicación de cada uno está a la derecha.", EstiloTexto.Pequeno);
        }
    }
}
