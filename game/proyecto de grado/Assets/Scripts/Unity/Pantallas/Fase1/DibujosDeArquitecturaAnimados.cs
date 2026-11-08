using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;
using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using A = Nexus.Unity.Pantallas.ActoresDeEscena;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las arquitecturas de la Fase 1, animadas (2026-11-08), en una lamina APAISADA de 1000×300 (el hueco de la
    /// animacion es ancho y bajo: asi el dibujo lo aprovecha y sus textos se leen). Con los modulos de ESTE proyecto:
    ///   · ResumenAnimado(a): 5 s por forma; una peticion que cruza el sistema (la explicacion general, ≤ 15 s).
    ///   · Detalle(a):  cómo es · una petición · al desplegar · el día a día (en bucle).
    ///   · Icono(p, a): la misma forma sin textos, para la carta (en 600×300).
    /// Como el plano de «Ver cómo quedaría» (DibujosDeArquitectura), la forma sale del id; uno desconocido se dibuja
    /// como un programa unico.
    /// </summary>
    public static class DibujosDeArquitecturaAnimados {
        public const float Ancho = 1000, Alto = 300;

        private enum Forma { Mono, Micro, Web, App, Parche }

        private static Forma FormaDe(string id) {
            id = (id ?? "").ToLowerInvariant();
            if (id.Contains("micro")) return Forma.Micro;
            if (id.Contains("parche")) return Forma.Parche;
            if (id.Contains("movil") || id.Contains("app")) return Forma.App;
            if (id.Contains("web")) return Forma.Web;
            return Forma.Mono;
        }

        /// <summary>Lo que se ve en el plano: donde estan los modulos, lo que se despliega y por donde va una peticion.</summary>
        private sealed class Geometria {
            public readonly List<Rect> Modulos = new List<Rect>();
            public readonly List<Rect> Despliegues = new List<Rect>();
            public readonly List<Vector2> Camino = new List<Vector2>();
            public readonly List<RectTransform> Nombres = new List<RectTransform>();
        }

        private static Vector2 V(float x, float y) { return new Vector2(x, y); }

        private static List<string> Modulos(FichaDelProyecto f) {
            var n = f != null && f.Modulos != null ? f.Modulos.Where(m => m != null && !string.IsNullOrEmpty(m.Nombre)).Select(m => m.Nombre).ToList() : new List<string>();
            if (n.Count == 0) n = new List<string> { "Módulo A", "Módulo B", "Módulo C" };
            return n.Take(6).ToList();
        }

        public static string Resumen(ArquitecturaOpcion a) {
            switch (FormaDe(a.Id)) {
                case Forma.Micro: return a.Nombre + ": cada módulo es un servicio aparte, con su base de datos; se hablan por la red.";
                case Forma.Parche: return a.Nombre + ": lo nuevo se construye encima del sistema viejo, sin tocarlo.";
                case Forma.App: return a.Nombre + ": cada persona instala una app que habla con el servidor.";
                case Forma.Web: return a.Nombre + ": se usa desde el navegador; todo vive en un servidor.";
                default: return a.Nombre + ": un solo programa, con los módulos separados por dentro.";
            }
        }

        private static string Peticion(Forma f) {
            switch (f) {
                case Forma.Micro: return "Una petición salta de servicio en servicio por la red: cada salto es algo más que puede fallar.";
                case Forma.Parche: return "Una petición entra por lo nuevo… y acaba en el sistema viejo, que nadie controla.";
                case Forma.App: return "La app del teléfono pide al servidor; el servidor consulta la base de datos y responde.";
                case Forma.Web: return "El navegador pide al servidor; el servidor consulta la base de datos y responde.";
                default: return "Una petición cruza el programa por dentro, sin red entre módulos, y llega a una sola base de datos.";
            }
        }

        private static float Carga(Forma f) {
            switch (f) {
                case Forma.Micro: return 0.85f;
                case Forma.Parche: return 0.7f;
                case Forma.App: return 0.55f;
                case Forma.Web: return 0.15f;
                default: return 0.2f;
            }
        }

        // ==================================================================== API

        public static FaseDeEscena ResumenAnimado(ArquitecturaOpcion a, FichaDelProyecto f) {
            var forma = FormaDe(a.Id);
            return new FaseDeEscena {
                Pestana = a.Nombre, Duracion = 5f, Pie = Resumen(a),
                Preparar = (p, act) => AnimarPeticion(act, Dibujar(p, act, forma, Modulos(f)), 0.9f, 3.6f)
            };
        }

        public static List<FaseDeEscena> Detalle(ArquitecturaOpcion a, FichaDelProyecto f) {
            var forma = FormaDe(a.Id);
            var mods = Modulos(f);
            return new List<FaseDeEscena> {
                new FaseDeEscena {
                    Pestana = "Cómo es", Duracion = 4.5f, Pie = Resumen(a),
                    Preparar = (p, act) => {
                        var g = Dibujar(p, act, forma, mods);
                        var luces = g.Modulos.Select(r => Luz(act, r, Tizador.Cian, 0.22f)).ToList();
                        return t => {
                            Nombres(g, t);
                            for (var i = 0; i < luces.Count; i++) {
                                var x = A.Tramo(t, 1.0f + 0.3f * i, 0.35f);
                                A.Alfa(luces[i], x); A.Escala(luces[i], A.Rebote(x));
                            }
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Una petición", Duracion = 5f, Pie = Peticion(forma),
                    Preparar = (p, act) => AnimarPeticion(act, Dibujar(p, act, forma, mods), 0.9f, 3.8f)
                },
                new FaseDeEscena {
                    Pestana = "Al desplegar", Duracion = 5f,
                    Pie = DibujosDeArquitectura.AlDesplegar(a.Id),
                    Preparar = (p, act) => {
                        var g = Dibujar(p, act, forma, mods);
                        var flashes = g.Despliegues.Select(r => Luz(act, r, Tizador.Mostaza, 0.35f)).ToList();
                        var cuenta = act.Rotulo(500, 288, 420, 24, "", Tizador.Mostaza, 18);
                        return t => {
                            Nombres(g, t);
                            var n = Mathf.Max(1, flashes.Count);
                            var hechos = 0;
                            for (var i = 0; i < flashes.Count; i++) {
                                var x = A.Tramo(t, 1.0f + i * (2.8f / n), 0.7f);
                                if (x > 0.5f) hechos++;
                                A.Alfa(flashes[i], Mathf.Sin(Mathf.PI * x));
                            }
                            cuenta.text = hechos == 0 ? "" : flashes.Count == 1 ? "1 despliegue" : hechos + " de " + flashes.Count + " despliegues";
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Día a día", Duracion = 4.5f,
                    Pie = DibujosDeArquitectura.EnElDia(a.Id),
                    Preparar = (p, act) => {
                        var g = Dibujar(p, act, forma, mods);
                        var velo = act.Ficha(Ancho, Alto, new Color(Tizador.Pizarra.r, Tizador.Pizarra.g, Tizador.Pizarra.b, 0.85f));
                        A.Poner(velo, Ancho / 2, Alto / 2);
                        var titulo = act.Rotulo(500, 100, 560, 40, "Trabajo extra que le cae al equipo", Tizador.Tiza, 26);
                        var pista = act.Ficha(520, 30, new Color(1, 1, 1, 0.12f));
                        A.Poner(pista, 500, 160);
                        var carga = Carga(forma);
                        var barra = act.Ficha(10, 30, carga > 0.5f ? Tizador.Roja : Tizador.Cian);
                        var nota = act.Rotulo(500, 212, 560, 30, carga >= 0.5f ? "mucho: queda menos tiempo para construir" : "poco: el equipo se dedica a construir", Tizador.Gris, 20);
                        return t => {
                            Nombres(g, t);
                            var x = A.Tramo(t, 1.0f, 0.4f);
                            A.Alfa(velo, x); A.Alfa(titulo, x); A.Alfa(pista, x); A.Alfa(nota, A.Tramo(t, 2.6f, 0.4f));
                            var ancho = Mathf.Max(6, 520 * carga * A.Salida(A.Tramo(t, 1.4f, 1.4f)));
                            barra.sizeDelta = new Vector2(ancho, 30);
                            A.Poner(barra, 240 + ancho / 2, 160);
                            A.Alfa(barra, x);
                        };
                    }
                }
            };
        }

        /// <summary>La forma sin textos, para la carta: la lamina de 1000×300 a 0,6 en la de 600×300.</summary>
        public static void Icono(Tizador p, ArquitecturaOpcion a, FichaDelProyecto f) {
            Dibujar(p.En(0, 60, 0.6f), null, FormaDe(a.Id), Modulos(f));
        }

        // ==================================================================== animaciones comunes

        private static RectTransform Luz(A act, Rect r, Color c, float alfa) {
            var l = act.Ficha(r.width, r.height, new Color(c.r, c.g, c.b, alfa));
            A.Poner(l, r.center);
            A.Alfa(l, 0);
            return l;
        }

        private static void Nombres(Geometria g, float t) {
            foreach (var n in g.Nombres) A.Alfa(n, A.Tramo(t, 0.7f, 0.3f));
        }

        /// <summary>Una ficha que recorre el camino de una peticion (ida y vuelta) entre 'desde' y 'desde + dura'.</summary>
        private static Action<float> AnimarPeticion(A act, Geometria g, float desde, float dura) {
            var ficha = act.Ficha(26, 26, Tizador.Mostaza, true);
            var largos = new List<float> { 0 };
            for (var i = 1; i < g.Camino.Count; i++) largos.Add(largos[i - 1] + Vector2.Distance(g.Camino[i - 1], g.Camino[i]));
            var total = Mathf.Max(1, largos[largos.Count - 1]);
            return t => {
                Nombres(g, t);
                var x = A.Suave(A.Tramo(t, desde, dura)) * total;
                var k = 1;
                while (k < largos.Count - 1 && largos[k] < x) k++;
                var tramo = Mathf.Max(0.001f, largos[k] - largos[k - 1]);
                var pos = g.Camino.Count > 1 ? Vector2.Lerp(g.Camino[k - 1], g.Camino[k], (x - largos[k - 1]) / tramo) : g.Camino[0];
                A.Poner(ficha, pos);
                A.Alfa(ficha, A.Tramo(t, desde - 0.3f, 0.3f) * (1 - A.Tramo(t, desde + dura, 0.3f)));
            };
        }

        // ==================================================================== el dibujo de cada forma

        /// <summary>Dibuja la forma (con 'act' null, sin nombres: el icono) y devuelve su geometria.</summary>
        private static Geometria Dibujar(Tizador p, A act, Forma forma, List<string> mods) {
            var g = new Geometria();
            switch (forma) {
                case Forma.Micro: Micro(p, act, mods, g); break;
                case Forma.Parche: Parche(p, act, mods, g); break;
                case Forma.Web: case Forma.App: Cliente(p, act, mods, g, forma == Forma.App); break;
                default: Mono(p, act, mods, g); break;
            }
            return g;
        }

        private static void Nombre(A act, Geometria g, Rect r, string texto) {
            if (act == null) return;
            var t = act.Rotulo(r.center.x, r.center.y, r.width - 14, r.height - 10, texto, Tizador.Tiza, 22);
            g.Nombres.Add(t.rectTransform);
        }

        /// <summary>Los modulos en rejilla dentro de un rectangulo, cada uno con su nombre (que cabe en su caja).</summary>
        private static void Rejilla(Tizador p, A act, List<string> mods, Rect zona, Color c, Geometria g) {
            var n = Math.Max(1, mods.Count);
            var cols = n <= 3 ? n : n <= 4 ? 2 : 3;
            var filas = (int)Math.Ceiling(n / (double)cols);
            float cw = zona.width / cols, ch = zona.height / filas;
            for (var i = 0; i < mods.Count; i++) {
                var r = new Rect(zona.x + (i % cols) * cw + 8, zona.y + (i / cols) * ch + 8, cw - 16, ch - 16);
                p.Caja(r.x, r.y, r.width, r.height, null, c);
                g.Modulos.Add(r);
                Nombre(act, g, r, mods[i]);
            }
        }

        private static void Mono(Tizador p, A act, List<string> mods, Geometria g) {
            p.Figura(60, 120, Tizador.Tiza);
            p.Flecha(95, 150, 160, 150, Tizador.Mostaza);
            var caja = new Rect(170, 25, 600, 250);
            p.Caja(caja.x, caja.y, caja.width, caja.height, null, Tizador.Cian);
            p.Caja(166, 120, 12, 60, null, Tizador.Mostaza);   // la unica puerta
            Rejilla(p, act, mods, new Rect(190, 40, 560, 220), Tizador.Cian, g);
            p.Flecha(780, 150, 840, 150, Tizador.Tiza);
            p.Cilindro(850, 90, 110, 110, Tizador.Tiza);
            if (act != null) {
                act.Rotulo(60, 200, 110, 28, "usuario", Tizador.Gris, 20);
                act.Rotulo(905, 232, 160, 28, "una base de datos", Tizador.Gris, 20);
            }
            g.Despliegues.Add(caja);
            g.Camino.AddRange(new[] { V(60, 150), V(172, 150), g.Modulos[0].center, g.Modulos[g.Modulos.Count - 1].center, V(905, 145), V(172, 150), V(60, 150) });
        }

        private static void Micro(Tizador p, A act, List<string> mods, Geometria g) {
            var n = Math.Max(1, mods.Count);
            var w = 900f / n; var x0 = 50f;
            p.Linea(x0 + 20, 32, x0 + w * n - 20, 32, Tizador.Gris, 2, true);
            for (var i = 0; i < mods.Count; i++) {
                var x = x0 + i * w;
                p.Linea(x + w / 2, 32, x + w / 2, 58, Tizador.Gris, 2);
                var r = new Rect(x + 10, 58, w - 20, 100);
                p.Caja(r.x, r.y, r.width, r.height, null, Tizador.Mostaza);
                g.Modulos.Add(r); g.Despliegues.Add(r);
                Nombre(act, g, r, mods[i]);
                p.Linea(x + w / 2, 158, x + w / 2, 180, Tizador.Gris, 2);
                p.Cilindro(x + w / 2 - 36, 180, 72, 72, Tizador.Tiza);
            }
            if (act != null) act.Rotulo(500, 14, 300, 24, "la red", Tizador.Gris, 18);
            g.Camino.Add(V(x0, 32));
            foreach (var r in g.Modulos) { g.Camino.Add(V(r.center.x, 32)); g.Camino.Add(r.center); g.Camino.Add(V(r.center.x, 32)); }
        }

        private static void Cliente(Tizador p, A act, List<string> mods, Geometria g, bool movil) {
            if (movil) {
                for (var i = 0; i < 3; i++) {
                    var r = new Rect(18 + i * 52, 80 + (i % 2) * 26, 44, 80);
                    p.Telefono(r.x, r.y, r.width, r.height, Tizador.Mostaza);
                    g.Despliegues.Add(r);   // cada telefono tiene que recibir la app
                }
            } else p.Navegador(20, 80, 150, 110, Tizador.Cian);
            if (act != null) act.Rotulo(95, 228, 180, 44, movil ? "una app en cada teléfono" : "un navegador", movil ? Tizador.Mostaza : Tizador.Cian, 20);
            p.Flecha(185, 130, 245, 130, Tizador.Tiza);
            p.Flecha(245, 160, 185, 160, Tizador.Gris);
            var servidor = new Rect(255, 25, 540, 250);
            p.Caja(servidor.x, servidor.y, servidor.width, servidor.height, null, Tizador.Cian);
            if (act != null) act.Rotulo(525, 46, 220, 26, "EL SERVIDOR", Tizador.Cian, 20);
            Rejilla(p, act, mods, new Rect(275, 66, 500, 196), Tizador.Cian, g);
            p.Flecha(805, 150, 850, 150, Tizador.Tiza);
            p.Cilindro(860, 90, 110, 110, Tizador.Tiza);
            if (act != null) act.Rotulo(915, 232, 160, 28, "la base de datos", Tizador.Gris, 20);
            g.Despliegues.Insert(0, servidor);
            g.Camino.AddRange(new[] { V(95, 130), V(250, 130), g.Modulos[0].center, V(915, 145), V(250, 160), V(95, 135) });
        }

        private static void Parche(Tizador p, A act, List<string> mods, Geometria g) {
            p.Figura(60, 70, Tizador.Tiza);
            p.Flecha(95, 100, 160, 100, Tizador.Mostaza);
            var nuevo = new Rect(170, 20, 620, 150);
            p.Caja(nuevo.x, nuevo.y, nuevo.width, nuevo.height, null, Tizador.Mostaza, true);
            Rejilla(p, act, mods, new Rect(185, 28, 590, 134), Tizador.Mostaza, g);
            var viejo = new Rect(170, 190, 620, 90);
            p.Caja(viejo.x, viejo.y, viejo.width, viejo.height, null, Tizador.Gris);
            p.Candado(480, 225, 22, Tizador.Gris);
            p.Bicho(640, 232, Tizador.Roja);
            if (act != null) act.Rotulo(895, 235, 190, 56, "el sistema viejo: nadie tiene su código", Tizador.Roja, 18);
            g.Despliegues.Add(nuevo); g.Despliegues.Add(viejo);
            g.Camino.AddRange(new[] { V(60, 100), V(172, 100), g.Modulos[0].center, V(480, 235), V(640, 240), g.Modulos[0].center, V(172, 100), V(60, 100) });
        }
    }
}
