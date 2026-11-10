using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Proyecto;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El sistema como un MAPA VIVO (2026-11-08): a la izquierda, las personas que lo rodean (con su cara); en el centro,
    /// las partes del sistema (con cuanto llevan hecho); a la derecha, los sistemas de fuera. Entre ellos, flechas con
    /// puntos que viajan: asi se ve por donde va la informacion. Pasar el raton por un nodo dice quien es o que hace;
    /// por el circulo de una flecha, que se dice por ella. Casi sin texto: es el diagrama de contexto, pero jugable.
    /// </summary>
    internal static class MapaVivoDelProyecto {
        private const float W = 1280, H = 420;

        public static void Crear(UiKit ui, RectTransform hueco, FichaDelProyecto f, Func<TooltipDeCalidad> tip) {
            var t = ui.Tema;
            var fondo = hueco.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(t.bg950, 0.6f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            ui.Borde(hueco, NexusTheme.Alfa(t.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);
            var visor = ui.Nodo(hueco, "Visor");
            UiKit.Rellenar(visor, t.Px(8));
            visor.gameObject.AddComponent<RectMask2D>();
            var lamina = PiezasDeProyecto.NuevaLamina(ui, visor, W, H);

            var personas = f.Contexto.Actores.Where(a => a.Tipo == "persona").ToList();
            var sistemas = f.Contexto.Actores.Where(a => a.Tipo != "persona").ToList();
            var rects = new Dictionary<string, Rect>();

            // ---- donde va cada cosa (coordenadas de la lamina, desde arriba a la izquierda)
            // Con mas personas, caras mas pequeñas: asi el nombre de cada una cabe debajo sin pisar a la siguiente.
            var lado = personas.Count <= 3 ? 78f : personas.Count == 4 ? 52f : 44f;
            for (var i = 0; i < personas.Count; i++) {
                var cy = H * (i + 1) / (personas.Count + 1);
                rects[personas[i].Id] = new Rect(110 - lado / 2, cy - lado / 2 - 8, lado, lado);
            }
            for (var i = 0; i < sistemas.Count; i++) {
                var cy = H * (i + 1) / (sistemas.Count + 1);
                rects[sistemas[i].Id] = new Rect(1130 - 80, cy - 38, 160, 76);
            }
            var n = Math.Max(1, f.Modulos.Count);
            var cols = n <= 3 ? n : n <= 4 ? 2 : 3;
            var filas = (int)Math.Ceiling(n / (double)cols);
            float zonaX = 330, zonaW = 620, zonaY = 40, zonaH = H - 80;
            float cw = zonaW / cols, ch = zonaH / filas;
            for (var i = 0; i < f.Modulos.Count; i++) {
                var cx = zonaX + (i % cols + 0.5f) * cw;
                var cy = zonaY + (i / cols + 0.5f) * ch;
                var w = Math.Min(190, cw - 28); var h = Math.Min(96, ch - 26);
                rects[f.Modulos[i].Id] = new Rect(cx - w / 2, cy - h / 2, w, h);
            }

            // ---- las flechas (debajo de los nodos)
            // Rectas con codos que no pisan ninguna caja y se cruzan lo menos posible (RutaDeFlechas).
            var d = lamina.Dibujo;
            var flujos = new List<Flujo>();
            var ids = new List<string>(); var partes = new List<List<Rect>>(); var sinAbajo = new List<bool>();
            foreach (var kv in rects) {
                ids.Add(kv.Key);
                var esPersona = personas.Any(p => p.Id == kv.Key);
                var partesDeCaja = new List<Rect> { kv.Value };
                if (esPersona) partesDeCaja.Add(new Rect(kv.Value.center.x - 70, kv.Value.yMax, 140, 38));   // su nombre
                partes.Add(partesDeCaja); sinAbajo.Add(esPersona);
            }
            foreach (var cx in new[] { 110f, 640f, 1130f }) {   // las leyendas de arriba tambien estorban
                partes.Add(new List<Rect> { new Rect(cx - 130, 8, 260, 28) }); sinAbajo.Add(false);
            }
            var pares = new List<int[]>(); var validos = new List<Flujo>();
            foreach (var fl in f.Contexto.Flujos) {
                var i = ids.IndexOf(fl.Desde); var j = ids.IndexOf(fl.Hasta);
                if (i < 0 || j < 0) continue;
                pares.Add(new[] { i, j }); validos.Add(new Flujo { Texto = fl.Texto });
            }
            var rutas = RutaDeFlechas.Calcular(partes, sinAbajo, pares, W, H);
            for (var k = 0; k < validos.Count; k++) {
                var ruta = rutas[k];
                if (ruta == null) {   // sin camino limpio: la recta de siempre
                    var a = rects[ids[pares[k][0]]]; var b = rects[ids[pares[k][1]]];
                    ruta = new List<Vector2> { BordeHacia(a, b.center), BordeHacia(b, a.center) };
                }
                // la punta se queda a 4 px de la caja de destino
                var u = ruta[ruta.Count - 1]; var v = ruta[ruta.Count - 2];
                ruta[ruta.Count - 1] = u + (v - u).normalized * 4;
                for (var i = 0; i + 1 < ruta.Count; i++) d.Linea(ruta[i].x, ruta[i].y, ruta[i + 1].x, ruta[i + 1].y, NexusTheme.Alfa(t.cyan, 0.5f), 3);
                d.Punta(ruta[ruta.Count - 2], ruta[ruta.Count - 1], NexusTheme.Alfa(t.cyan, 0.8f), 3, 14);
                validos[k].Ruta = ruta;
                flujos.Add(validos[k]);
            }

            // ---- las leyendas de las tres columnas
            Leyenda(ui, lamina, 110, 14, "QUIÉN LO USA", t.cyan);
            Leyenda(ui, lamina, 640, 14, "LAS PARTES DEL SISTEMA", t.warning);
            Leyenda(ui, lamina, 1130, 14, "SISTEMAS DE FUERA", t.violet);

            // ---- los nodos
            for (var i = 0; i < personas.Count; i++) Persona(ui, lamina, personas[i], rects[personas[i].Id], f, tip, 0.06f * i);
            for (var i = 0; i < f.Modulos.Count; i++) Modulo(ui, lamina, f.Modulos[i], rects[f.Modulos[i].Id], tip, 0.3f + 0.08f * i);
            for (var i = 0; i < sistemas.Count; i++) Sistema(ui, lamina, sistemas[i], rects[sistemas[i].Id], tip, 0.5f + 0.06f * i);

            // ---- el circulo de cada flecha (su texto, al pasar el raton) y el punto que viaja
            var puntos = new List<RectTransform>();
            for (var i = 0; i < flujos.Count; i++) {
                var fl = flujos[i];
                var medio = fl.Medio();
                var marca = ui.Nodo(lamina.Raiz, "Flujo " + i);
                PonerCentro(marca, medio, 22, 22);
                var im = marca.gameObject.AddComponent<Image>();
                im.sprite = ui.SpriteCircular(); im.color = NexusTheme.Alfa(t.bg950, 0.95f);
                ui.Borde(marca, NexusTheme.Alfa(t.cyan, 0.9f), 11, 1.5f);
                TextoEnCaja.Crear(ui, marca, "i", EstiloTexto.Leyenda, t.cyan, TextAlignmentOptions.Center, 16, Vector2.zero, Vector2.one);
                var r = marca.gameObject.AddComponent<RealceDeCalidad>();
                im.raycastTarget = true;
                r.Fondo = im; r.Normal = NexusTheme.Alfa(t.bg950, 0.95f); r.Resaltado = NexusTheme.Alfa(t.cyan, 0.4f); r.Escala = 1.25f;
                var texto = fl.Texto;
                r.Entra = () => tip().Mostrar("Qué pasa aquí", Mayuscula(texto), t.cyan);
                r.Sale = () => tip().Ocultar();

                var punto = ui.Nodo(lamina.Raiz, "Punto " + i);
                punto.anchorMin = punto.anchorMax = new Vector2(0, 1);
                punto.pivot = new Vector2(0.5f, 0.5f);
                punto.sizeDelta = new Vector2(13, 13);
                var pi = punto.gameObject.AddComponent<Image>();
                pi.sprite = ui.SpriteCircular(); pi.color = t.warning; pi.raycastTarget = false;
                puntos.Add(punto);
            }
            var viajero = lamina.Raiz.gameObject.AddComponent<PuntosViajeros>();
            viajero.Puntos = puntos; viajero.Flujos = flujos;
        }

        private sealed class Flujo {
            public List<Vector2> Ruta; public string Texto;
            public float Largo() { var l = 0f; for (var i = 0; i + 1 < Ruta.Count; i++) l += Vector2.Distance(Ruta[i], Ruta[i + 1]); return l; }
            /// <summary>El punto a lo largo de la ruta (0 = salida, 1 = llegada).</summary>
            public Vector2 En(float x) {
                var objetivo = Mathf.Clamp01(x) * Largo();
                for (var i = 0; i + 1 < Ruta.Count; i++) {
                    var tramo = Vector2.Distance(Ruta[i], Ruta[i + 1]);
                    if (objetivo <= tramo || i + 2 == Ruta.Count) return Vector2.Lerp(Ruta[i], Ruta[i + 1], tramo < 0.01f ? 0 : Mathf.Clamp01(objetivo / tramo));
                    objetivo -= tramo;
                }
                return Ruta[0];
            }
            /// <summary>El centro del tramo mas largo: ahi cabe el circulo «i» sin pisar un codo.</summary>
            public Vector2 Medio() {
                var mejor = 0; var largo = -1f;
                for (var i = 0; i + 1 < Ruta.Count; i++) { var l = Vector2.Distance(Ruta[i], Ruta[i + 1]); if (l > largo) { largo = l; mejor = i; } }
                return (Ruta[mejor] + Ruta[mejor + 1]) / 2;
            }
        }

        private static string Mayuscula(string s) { return string.IsNullOrEmpty(s) ? "" : char.ToUpperInvariant(s[0]) + s.Substring(1) + "."; }

        private static Vector2 BordeHacia(Rect r, Vector2 hacia) {
            var c = r.center; var v = hacia - c;
            var sx = Mathf.Abs(v.x) < 0.001f ? float.MaxValue : (r.width / 2) / Mathf.Abs(v.x);
            var sy = Mathf.Abs(v.y) < 0.001f ? float.MaxValue : (r.height / 2) / Mathf.Abs(v.y);
            return c + v * Mathf.Min(sx, sy);
        }

        private static void PonerCentro(RectTransform rt, Vector2 c, float w, float h) {
            Lamina.Colocar(rt, c.x - w / 2, c.y - h / 2, w, h);
        }

        private static void Leyenda(UiKit ui, Lamina l, float cx, float y, string texto, Color color) {
            var rt = ui.Nodo(l.Raiz, "Leyenda " + texto);
            Lamina.Colocar(rt, cx - 130, y - 4, 260, 24);
            TextoEnCaja.Crear(ui, rt, texto, EstiloTexto.Leyenda, color, TextAlignmentOptions.Center, 15, Vector2.zero, Vector2.one);
        }

        private static string Coincide(FichaDelProyecto f, string nombre) {
            foreach (var u in f.Usuarios) {
                if (string.IsNullOrEmpty(u.Nombre) || string.IsNullOrEmpty(nombre)) continue;
                var a = u.Nombre.ToLowerInvariant(); var b = nombre.ToLowerInvariant();
                if (a.Contains(b) || b.Contains(a)) return u.QueNecesita;
            }
            return null;
        }

        private static void Persona(UiKit ui, Lamina l, ActorDelProyecto a, Rect r, FichaDelProyecto f, Func<TooltipDeCalidad> tip, float retraso) {
            var t = ui.Tema;
            var caja = ui.Nodo(l.Raiz, "Persona " + a.Nombre);
            Lamina.Colocar(caja, r.center.x - 80, r.y - 2, 160, r.height + 36);
            var zona = caja.gameObject.AddComponent<Image>();
            zona.color = Color.clear;
            var rostro = RostroDePersonaje.Crear(ui, caja, MaterialesNexus.IdDePersonaje(a.Nombre), a.Nombre, r.width);
            rostro.anchorMin = rostro.anchorMax = new Vector2(0.5f, 1);
            rostro.pivot = new Vector2(0.5f, 1);
            rostro.anchoredPosition = new Vector2(0, -2);
            rostro.GetComponent<LayoutElement>().ignoreLayout = true;
            TextoEnCaja.Crear(ui, caja, a.Nombre, EstiloTexto.Pequeno, t.ink, TextAlignmentOptions.Top, 18,
                              Vector2.zero, new Vector2(1, 0.28f), 0);
            var necesita = Coincide(f, a.Nombre);
            var r2 = caja.gameObject.AddComponent<RealceDeCalidad>();
            r2.Fondo = zona; r2.Normal = Color.clear; r2.Resaltado = NexusTheme.Alfa(t.cyan, 0.12f); r2.Escala = 1.06f;
            r2.Entra = () => tip().Mostrar(a.Nombre, necesita ?? "Una persona que rodea al sistema.", t.cyan);
            r2.Sale = () => tip().Ocultar();
            Aparecer.En(caja, retraso, false);
        }

        private static void Modulo(UiKit ui, Lamina l, ModuloDelProyecto m, Rect r, Func<TooltipDeCalidad> tip, float retraso) {
            var t = ui.Tema;
            var caja = ui.Nodo(l.Raiz, "Parte " + m.Nombre);
            Lamina.Colocar(caja, r.x, r.y, r.width, r.height);
            var fondo = caja.gameObject.AddComponent<Image>();
            var hecho = m.AvanceInicial > 0;
            var acento = hecho ? t.cyan : t.warning;
            ui.Fondo(fondo, NexusTheme.Alfa(t.bg950, 0.85f), NexusTheme.RadioMd);
            ui.Borde(caja, NexusTheme.Alfa(acento, 0.85f), NexusTheme.RadioMd, 2);
            TextoEnCaja.Crear(ui, caja, m.Nombre, EstiloTexto.Encabezado, t.ink, TextAlignmentOptions.Center, 24,
                              new Vector2(0, 0.34f), Vector2.one, 8).fontStyle = FontStyles.Bold;
            // cuanto lleva hecho: una barra y, si ya hay algo, su cifra
            var pista = ui.Nodo(caja, "Pista");
            pista.anchorMin = new Vector2(0.1f, 0.14f); pista.anchorMax = new Vector2(0.9f, 0.24f);
            pista.offsetMin = pista.offsetMax = Vector2.zero;
            var fp = pista.gameObject.AddComponent<Image>();
            ui.Fondo(fp, NexusTheme.Alfa(t.ink, 0.12f), 3);
            fp.raycastTarget = false;
            if (hecho) {
                var lleno = ui.Nodo(pista, "Lleno");
                lleno.anchorMin = Vector2.zero; lleno.anchorMax = new Vector2(Mathf.Clamp01((float)m.AvanceInicial / 100f), 1);
                lleno.offsetMin = lleno.offsetMax = Vector2.zero;
                var fl = lleno.gameObject.AddComponent<Image>();
                ui.Fondo(fl, t.cyan, 3);
                fl.raycastTarget = false;
            }
            fondo.raycastTarget = true;
            var re = caja.gameObject.AddComponent<RealceDeCalidad>();
            re.Fondo = fondo; re.Normal = NexusTheme.Alfa(t.bg950, 0.85f); re.Resaltado = NexusTheme.Alfa(acento, 0.25f); re.Escala = 1.05f;
            var estado = hecho ? m.AvanceInicial.ToString("0") + " % ya hecho" : "Por hacer";
            re.Entra = () => tip().Mostrar(m.Nombre + " · " + estado, m.QueHace, acento);
            re.Sale = () => tip().Ocultar();
            Aparecer.En(caja, retraso, false);
        }

        private static void Sistema(UiKit ui, Lamina l, ActorDelProyecto a, Rect r, Func<TooltipDeCalidad> tip, float retraso) {
            var t = ui.Tema;
            var caja = ui.Nodo(l.Raiz, "Sistema " + a.Nombre);
            Lamina.Colocar(caja, r.x, r.y, r.width, r.height);
            var fondo = caja.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(t.bg950, 0.85f), NexusTheme.RadioMd);
            ui.Borde(caja, NexusTheme.Alfa(t.violet, 0.85f), NexusTheme.RadioMd, 2);
            TextoEnCaja.Crear(ui, caja, a.Nombre, EstiloTexto.Pequeno, t.ink, TextAlignmentOptions.Center, 19, Vector2.zero, Vector2.one, 8);
            fondo.raycastTarget = true;
            var re = caja.gameObject.AddComponent<RealceDeCalidad>();
            re.Fondo = fondo; re.Normal = NexusTheme.Alfa(t.bg950, 0.85f); re.Resaltado = NexusTheme.Alfa(t.violet, 0.25f); re.Escala = 1.05f;
            re.Entra = () => tip().Mostrar(a.Nombre, "Un sistema que ya existe fuera del tuyo: se habla con él, no se construye.", t.violet);
            re.Sale = () => tip().Ocultar();
            Aparecer.En(caja, retraso, false);
        }

        /// <summary>Hace viajar un punto por cada flecha, escalonados, en bucle.</summary>
        private sealed class PuntosViajeros : MonoBehaviour {
            public List<RectTransform> Puntos;
            public List<Flujo> Flujos;
            private void Update() {
                if (Puntos == null) return;
                for (var i = 0; i < Puntos.Count; i++) {
                    var x = Mathf.Repeat(Time.unscaledTime * 0.32f + i * 0.19f, 1f);
                    var p = Flujos[i].En(x);
                    Puntos[i].anchoredPosition = new Vector2(p.x, -p.y);
                    var s = Mathf.Sin(Mathf.PI * x);
                    Puntos[i].localScale = Vector3.one * (0.4f + 0.8f * s);
                }
            }
        }
    }
}
