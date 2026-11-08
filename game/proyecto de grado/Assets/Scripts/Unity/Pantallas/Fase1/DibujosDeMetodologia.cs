using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using A = Nexus.Unity.Pantallas.ActoresDeEscena;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las escenas animadas de las metodologias (2026-11-08), en la lamina de 600×300:
    ///   · Resumen(m): 5 s por metodologia, la idea en una imagen (las tres caben en 15 s).
    ///   · Detalle(m): la metodologia elegida, fase a fase (~20 s en bucle).
    ///   · Icono(p, m): la misma imagen sin textos, quieta, para la carta.
    /// Que dibujo toca se decide por el TIPO de calendario (iterativo / secuencial / continuo), nunca por el id: una
    /// metodologia nueva en datos (XP, RUP…) sale con el dibujo de su tipo. Los nombres de etapas, la unidad, la
    /// longitud del sprint y el limite WIP salen del perfil.
    /// </summary>
    public static class DibujosDeMetodologia {
        private static string Tipo(MethodologyProfile m) {
            return m != null && m.Calendario != null && !string.IsNullOrEmpty(m.Calendario.Tipo) ? m.Calendario.Tipo : TiposDeCalendario.Iterativo;
        }

        private static string Unidad(MethodologyProfile m) {
            var u = m != null && m.Calendario != null && !string.IsNullOrEmpty(m.Calendario.EtiquetaUnidad) ? m.Calendario.EtiquetaUnidad : "Sprint";
            return u;
        }

        private static Vector2 V(float x, float y) { return new Vector2(x, y); }

        // ==================================================================== API

        public static FaseDeEscena Resumen(MethodologyProfile m) {
            switch (Tipo(m)) {
                case TiposDeCalendario.Secuencial:
                    return new FaseDeEscena { Pestana = m.Nombre, Duracion = 5f, Preparar = ResumenSecuencial(m),
                        Pie = m.Nombre + ": una etapa detrás de otra, en orden. El cliente recibe el producto al final." };
                case TiposDeCalendario.Continuo:
                    return new FaseDeEscena { Pestana = m.Nombre, Duracion = 5f, Preparar = ResumenContinuo(m),
                        Pie = m.Nombre + ": el trabajo fluye por un tablero; cada tarea avanza cuando hay sitio." };
                default:
                    return new FaseDeEscena { Pestana = m.Nombre, Duracion = 5f, Preparar = ResumenIterativo(m),
                        Pie = m.Nombre + ": ciclos cortos (" + Unidad(m).ToLowerInvariant() + "s); cada uno entrega algo que funciona." };
            }
        }

        public static List<FaseDeEscena> Detalle(MethodologyProfile m) {
            switch (Tipo(m)) {
                case TiposDeCalendario.Secuencial: return DetalleSecuencial(m);
                case TiposDeCalendario.Continuo: return DetalleContinuo(m);
                default: return DetalleIterativo(m);
            }
        }

        /// <summary>La imagen de la metodologia, sin textos (para su carta).</summary>
        public static void Icono(Tizador p, MethodologyProfile m) {
            switch (Tipo(m)) {
                case TiposDeCalendario.Secuencial: {
                    var n = NombresDeEtapas(m).Count;
                    Escalera(p, NombresDeEtapas(m), false);
                    var h = Huella(0, n);
                    p.Punto(h.x, h.y - 14, 14, Tizador.Cian);
                    break;
                }
                case TiposDeCalendario.Continuo:
                    Tablero(p, false);
                    p.Tarjeta(35, 70, 110, 34, Tizador.Gris); p.Tarjeta(35, 114, 110, 34, Tizador.Gris);
                    p.Tarjeta(175, 70, 110, 34, Tizador.Mostaza); p.Tarjeta(315, 70, 110, 34, Tizador.Mostaza); p.Tarjeta(455, 70, 110, 34, Tizador.Cian);
                    break;
                default: Ciclo(p, null, false); break;
            }
        }

        // ==================================================================== iterativo (Scrum…)

        private static readonly Vector2 CentroDelCiclo = new Vector2(300, 150);
        private const float RadioDelCiclo = 82;

        private static Vector2 EnElCiclo(float grados) {
            var a = grados * Mathf.Deg2Rad;
            return CentroDelCiclo + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RadioDelCiclo;
        }

        /// <summary>El ciclo: el backlog a la izquierda, el circulo con sus cuatro momentos y la caja del incremento.</summary>
        private static void Ciclo(Tizador p, string unidad, bool conTextos) {
            p.Circulo(300, 150, RadioDelCiclo, Tizador.Gris, true);
            var nombres = new[] { "Planning", "Daily", "Review", "Retro" };
            for (var i = 0; i < 4; i++) {
                var n = EnElCiclo(-90 + 90 * i);
                var b = (-90 + 90 * i + 45) * Mathf.Deg2Rad;
                p.Curva(300 + Mathf.Cos(b - 0.3f) * RadioDelCiclo, 150 + Mathf.Sin(b - 0.3f) * RadioDelCiclo,
                        300 + Mathf.Cos(b) * (RadioDelCiclo + 10), 150 + Mathf.Sin(b) * (RadioDelCiclo + 10),
                        300 + Mathf.Cos(b + 0.3f) * RadioDelCiclo, 150 + Mathf.Sin(b + 0.3f) * RadioDelCiclo, Tizador.Tiza);
                p.Punto(n.x, n.y, 15, Tizador.Pizarra); p.Circulo(n.x, n.y, 15, Tizador.Cian);
            }
            p.Caja(28, 70, 92, 160, null, Tizador.Mostaza);
            for (var i = 0; i < 4; i++) p.Tarjeta(38, 82 + i * 36, 72, 28, i == 0 ? Tizador.Mostaza : Tizador.Gris);
            p.Curva(122, 92, 200, 40, 280, 64, Tizador.Mostaza);
            p.Caja(478, 90, 96, 120, null, Tizador.Tiza);
            if (!conTextos) return;
            p.Texto(300, 46, nombres[0], 19, Tizador.Cian, true);
            p.Texto(432, 156, nombres[1], 19, Tizador.Cian, true);
            p.Texto(300, 268, nombres[2], 19, Tizador.Cian, true);
            p.Texto(172, 156, nombres[3], 19, Tizador.Cian, true);
            p.Texto(74, 256, "Backlog", 19, Tizador.Mostaza, true);
            p.Texto(526, 232, "Incremento", 19, Tizador.Tiza, true);
            if (unidad != null) p.Texto(300, 156, unidad, 21, Tizador.Tiza, true);
        }

        private static Func<Tizador, A, Action<float>> ResumenIterativo(MethodologyProfile m) {
            return (p, a) => {
                Ciclo(p, Unidad(m), true);
                var tarea = a.Ficha(30, 20, Tizador.Mostaza);
                var hechas = new[] { a.Ficha(60, 16, Tizador.Cian), a.Ficha(60, 16, Tizador.Cian) };
                A.Poner(hechas[0], 526, 190); A.Poner(hechas[1], 526, 170);
                return t => {
                    A.Alfa(tarea, A.Tramo(t, 0.6f, 0.3f));
                    Vector2 pos;
                    if (t < 1.0f) pos = V(74, 96);
                    else if (t < 1.5f) pos = A.Entre(V(74, 96), EnElCiclo(-90), A.Tramo(t, 1.0f, 0.5f));
                    else if (t < 3.5f) pos = EnElCiclo(-90 + 360 * A.Suave(A.Tramo(t, 1.5f, 2.0f)));
                    else pos = A.Entre(EnElCiclo(-90), V(526, 150), A.Tramo(t, 3.5f, 0.6f));
                    A.Poner(tarea, pos);
                    A.Escala(tarea, t > 4.1f ? 1 + 0.25f * Mathf.Sin(Mathf.PI * A.Tramo(t, 4.1f, 0.35f)) : 1);
                    A.Alfa(hechas[0], A.Tramo(t, 0.9f, 0.3f));
                    A.Alfa(hechas[1], A.Tramo(t, 4.1f, 0.3f));
                    A.Escala(hechas[1], A.Rebote(A.Tramo(t, 4.1f, 0.4f)));
                };
            };
        }

        private static List<FaseDeEscena> DetalleIterativo(MethodologyProfile m) {
            var unidad = Unidad(m);
            var u = unidad.ToLowerInvariant();
            var largo = Mathf.Max(1, m.Calendario != null ? m.Calendario.LongitudIteracion : 10);
            var veces = Mathf.Max(1, m.Calendario != null ? m.Calendario.Iteraciones : 2);
            return new List<FaseDeEscena> {
                new FaseDeEscena {
                    Pestana = "Backlog", Duracion = 4.5f,
                    Pie = "Product Backlog: todo lo que hay que hacer, ordenado. Lo más importante va arriba.",
                    Preparar = (p, a) => {
                        p.Caja(190, 20, 220, 262, null, Tizador.Mostaza); p.Texto(300, 46, "Product Backlog", 21, Tizador.Mostaza, true);
                        for (var i = 0; i < 5; i++) p.Tarjeta(210, 60 + i * 42, 180, 32, i < 2 ? Tizador.Mostaza : Tizador.Gris);
                        p.Flecha(450, 250, 450, 66, Tizador.Mostaza); p.Texto(510, 150, "prioridad", 19, Tizador.Mostaza, true);
                        var nueva = a.Ficha(150, 28, Tizador.Cian, false, "nueva idea");
                        var foco = a.Ficha(190, 38, new Color(Tizador.Cian.r, Tizador.Cian.g, Tizador.Cian.b, 0.25f));
                        return t => {
                            var k = Mathf.Repeat((t - 1f) * 1.4f, 5f);
                            A.Poner(foco, 300, 76 + Mathf.Floor(k) * 42); A.Alfa(foco, t > 1f && t < 3.2f ? 1 : 0);
                            A.Poner(nueva, A.Entre(V(90, 280), V(300, 160), A.Tramo(t, 3.0f, 0.8f)));
                            A.Alfa(nueva, A.Tramo(t, 2.8f, 0.3f));
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Planning", Duracion = 4.5f,
                    Pie = "Sprint Planning: el equipo elige lo que cabe en el " + u + " y se compromete con ello.",
                    Preparar = (p, a) => {
                        p.Caja(30, 30, 130, 240, null, Tizador.Mostaza); p.Texto(95, 290, "Backlog", 19, Tizador.Mostaza, true);
                        p.Caja(330, 30, 240, 240, null, Tizador.Cian); p.Texto(450, 290, unidad + " 1", 20, Tizador.Cian, true);
                        p.Flecha(180, 150, 310, 150, Tizador.Tiza);
                        var tarjetas = new List<RectTransform>();
                        for (var i = 0; i < 5; i++) { var f = a.Ficha(104, 30, i < 3 ? Tizador.Mostaza : Tizador.Gris); A.Poner(f, 95, 60 + i * 44); tarjetas.Add(f); }
                        return t => {
                            for (var i = 0; i < 3; i++) {
                                var x = A.Tramo(t, 1.1f + i * 0.7f, 0.7f);
                                A.Poner(tarjetas[i], A.Entre(V(95, 60 + i * 44), V(450, 80 + i * 50), x));
                            }
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = unidad, Duracion = 5f,
                    Pie = "El " + u + " dura " + largo + " días. Cada día, una Daily de 15 minutos para coordinarse.",
                    Preparar = (p, a) => {
                        p.Circulo(300, 150, 110, Tizador.Gris, true);
                        var marcas = Mathf.Min(largo, 20);
                        for (var i = 0; i < marcas; i++) {
                            var ang = (-90 + 360f * i / marcas) * Mathf.Deg2Rad;
                            p.Linea(300 + Mathf.Cos(ang) * 100, 150 + Mathf.Sin(ang) * 100, 300 + Mathf.Cos(ang) * 120, 150 + Mathf.Sin(ang) * 120, Tizador.Tiza, 2);
                        }
                        p.Figura(120, 120, Tizador.Tiza); p.Figura(80, 150, Tizador.Tiza); p.Figura(120, 180, Tizador.Tiza);
                        var bola = a.Ficha(22, 22, Tizador.Mostaza, true);
                        var dia = a.Rotulo(300, 150, 160, 50, "Día 1", Tizador.Tiza, 22);
                        var daily = a.Ficha(120, 30, Tizador.Cian, false, "Daily");
                        A.Poner(daily, 480, 150);
                        return t => {
                            var x = A.Tramo(t, 0.8f, 3.8f);
                            var ang = (-90 + 360f * x) * Mathf.Deg2Rad;
                            A.Poner(bola, 300 + Mathf.Cos(ang) * 110, 150 + Mathf.Sin(ang) * 110);
                            var d = Mathf.Clamp(Mathf.FloorToInt(x * largo) + 1, 1, largo);
                            dia.text = "Día " + d + " de " + largo;
                            var enDia = Mathf.Repeat(x * largo, 1f);
                            A.Alfa(daily, t > 0.8f ? 1 - enDia : 0);
                            A.Escala(daily, 1 + 0.15f * (1 - enDia));
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Review", Duracion = 4.5f,
                    Pie = "Sprint Review: se enseña al cliente lo terminado y se recoge su opinión.",
                    Preparar = (p, a) => {
                        p.Caja(150, 100, 170, 130, null, Tizador.Cian); p.Texto(235, 258, "lo terminado", 19, Tizador.Cian, true);
                        p.Figura(470, 120, Tizador.Mostaza, "cliente");
                        p.Flecha(340, 165, 430, 150, Tizador.Tiza);
                        var piezas = new List<RectTransform>();
                        for (var i = 0; i < 3; i++) { var f = a.Ficha(120, 24, Tizador.Cian); A.Poner(f, 235, 200 - i * 32); piezas.Add(f); }
                        var opinion = a.Ficha(150, 34, Tizador.Mostaza, false, "¡me sirve!");
                        return t => {
                            for (var i = 0; i < 3; i++) { A.Alfa(piezas[i], A.Tramo(t, 0.9f + i * 0.35f, 0.25f)); A.Escala(piezas[i], A.Rebote(A.Tramo(t, 0.9f + i * 0.35f, 0.35f))); }
                            A.Poner(opinion, 470, 40); A.Alfa(opinion, A.Tramo(t, 2.6f, 0.3f)); A.Escala(opinion, A.Rebote(A.Tramo(t, 2.6f, 0.4f)));
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Retro", Duracion = 4.5f,
                    Pie = "Retrospectiva: el equipo mejora su forma de trabajar. El ciclo se repite " + veces + (veces == 1 ? " vez." : " veces."),
                    Preparar = (p, a) => {
                        p.Figura(190, 60, Tizador.Tiza); p.Figura(300, 44, Tizador.Tiza); p.Figura(410, 60, Tizador.Tiza);
                        p.Tablero(150, 130, 300, 150, Tizador.Tiza, "bien", "mejorar");
                        var notas = new List<RectTransform>();
                        for (var i = 0; i < 4; i++) { var f = a.Ficha(60, 40, i % 2 == 0 ? Tizador.Cian : Tizador.Mostaza); notas.Add(f); }
                        var vuelta = a.Rotulo(300, 290, 300, 24, "y vuelta a empezar", Tizador.Cian, 16);
                        return t => {
                            for (var i = 0; i < 4; i++) {
                                var col = i % 2; var fila = i / 2;
                                var destino = V(225 + col * 150, 195 + fila * 48);
                                var origen = V(190 + i * 70, 90);
                                A.Poner(notas[i], A.Entre(origen, destino, A.Tramo(t, 1.0f + i * 0.4f, 0.5f)));
                                A.Alfa(notas[i], A.Tramo(t, 0.9f + i * 0.4f, 0.2f));
                            }
                            A.Alfa(vuelta, A.Tramo(t, 3.3f, 0.4f));
                        };
                    }
                }
            };
        }

        // ==================================================================== secuencial (Cascada…)

        private static List<string> NombresDeEtapas(MethodologyProfile m) {
            var etapas = m != null && m.Calendario != null && m.Calendario.Etapas != null
                ? m.Calendario.Etapas.Where(e => e != null && !string.IsNullOrEmpty(e.Nombre)).Select(e => e.Nombre).ToList()
                : new List<string>();
            return etapas.Count >= 2 ? etapas : new List<string> { "Requisitos", "Diseño", "Construcción", "Pruebas" };
        }

        private const float X0 = 30, Y0 = 50, AnchoEsc = 540, AltoEsc = 200;

        /// <summary>El centro de la huella del escalon i (donde se apoya la gota).</summary>
        private static Vector2 Huella(int i, int n) { return V(X0 + AnchoEsc / n * (i + 0.5f), Y0 + AltoEsc / n * i); }

        /// <summary>La escalera: un escalon por etapa, con su nombre (y sus dias, si se dan).</summary>
        private static void Escalera(Tizador p, IList<string> nombres, bool conTextos, IList<int> dias = null) {
            var n = nombres.Count; var w = AnchoEsc / n; var h = AltoEsc / n;
            var xy = new float[n * 4 + 2];
            xy[0] = X0; xy[1] = Y0;
            for (var i = 0; i < n; i++) { xy[i * 4 + 2] = X0 + w * (i + 1); xy[i * 4 + 3] = Y0 + h * i; xy[i * 4 + 4] = X0 + w * (i + 1); xy[i * 4 + 5] = Y0 + h * (i + 1); }
            p.Trazo(Tizador.Tiza, xy);
            if (!conTextos) return;
            for (var i = 0; i < n; i++) {
                var c = Huella(i, n);
                var texto = dias != null && i < dias.Count && dias[i] > 0 ? nombres[i] + " · " + dias[i] + " d" : nombres[i];
                p.Texto(c.x, c.y + 22, texto, 18, Tizador.Mostaza, true, w - 6);
            }
        }

        private static Func<Tizador, A, Action<float>> ResumenSecuencial(MethodologyProfile m) {
            return (p, a) => {
                var nombres = NombresDeEtapas(m);
                var n = nombres.Count;
                Escalera(p, nombres, true);
                p.Bandera(X0 + AnchoEsc + 10, Y0 + AltoEsc + 30, 40, Tizador.Mostaza);
                var luces = new List<RectTransform>();
                for (var i = 0; i < n; i++) { var l = a.Ficha(AnchoEsc / n - 12, 6, Tizador.Cian); A.Poner(l, Huella(i, n) + V(0, 4)); luces.Add(l); }
                var gota = a.Ficha(24, 24, Tizador.Cian, true);
                var entrega = a.Ficha(110, 28, Tizador.Mostaza, false, "Entrega");
                A.Poner(entrega, 500, 290);
                return t => {
                    var porEscalon = 3.2f / n;
                    var x = (t - 1.0f) / porEscalon;
                    var i = Mathf.Clamp(Mathf.FloorToInt(x), 0, n - 1);
                    var f = Mathf.Clamp01(x - i);
                    Vector2 pos;
                    if (x < 0) pos = Huella(0, n) - V(0, 14);
                    else if (i < n - 1 && f > 0.6f) {
                        var g = (f - 0.6f) / 0.4f;
                        var a0 = Huella(i, n) - V(0, 14); var a1 = Huella(i + 1, n) - V(0, 14);
                        pos = Vector2.Lerp(a0, a1, g) - V(0, Mathf.Sin(Mathf.PI * g) * 18);
                    } else pos = Huella(i, n) - V(0, 14);
                    A.Poner(gota, pos);
                    for (var k = 0; k < n; k++) A.Alfa(luces[k], x >= k ? 1 : 0.0f);
                    A.Alfa(entrega, A.Tramo(t, 4.2f, 0.3f)); A.Escala(entrega, A.Rebote(A.Tramo(t, 4.2f, 0.4f)));
                };
            };
        }

        private static List<FaseDeEscena> DetalleSecuencial(MethodologyProfile m) {
            var nombres = NombresDeEtapas(m);
            var dias = m.Calendario != null && m.Calendario.Etapas != null ? m.Calendario.Etapas.Where(e => e != null && !string.IsNullOrEmpty(e.Nombre)).Select(e => e.Dias).ToList() : new List<int>();
            var n = nombres.Count;
            var total = dias.Count == n ? dias.Sum() : 0;
            return new List<FaseDeEscena> {
                new FaseDeEscena {
                    Pestana = nombres[0], Duracion = 4.5f,
                    Pie = "Primero se definen TODOS los requisitos y se firman antes de empezar a construir.",
                    Preparar = (p, a) => {
                        p.Figura(90, 110, Tizador.Mostaza, "cliente"); p.Figura(510, 110, Tizador.Cian, "equipo");
                        p.Hoja(220, 20, 160, 250, Tizador.Tiza, "Requisitos");
                        var sello = a.Ficha(150, 44, Tizador.Mostaza, false, "FIRMADO");
                        A.Poner(sello, 300, 210);
                        return t => {
                            var x = A.Tramo(t, 2.4f, 0.35f);
                            A.Alfa(sello, x); A.Escala(sello, Mathf.Lerp(2.2f, 1f, A.Salida(x)));
                            sello.localEulerAngles = new Vector3(0, 0, -10);
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Etapas", Duracion = 6f,
                    Pie = "Cada una de las " + n + " etapas termina antes de que empiece la siguiente" + (total > 0 ? " (" + total + " días en total)." : "."),
                    Preparar = (p, a) => {
                        Escalera(p, nombres, true, dias);
                        var gota = a.Ficha(24, 24, Tizador.Cian, true);
                        var reloj = a.Rotulo(430, 28, 330, 40, "", Tizador.Tiza, 22);
                        return t => {
                            var porEscalon = 4.2f / n;
                            var x = Mathf.Max(0, (t - 1.0f) / porEscalon);
                            var i = Mathf.Clamp(Mathf.FloorToInt(x), 0, n - 1);
                            var f = Mathf.Clamp01(x - i);
                            var pos = Huella(i, n) - V(0, 14);
                            if (i < n - 1 && f > 0.7f) {
                                var g = (f - 0.7f) / 0.3f;
                                pos = Vector2.Lerp(Huella(i, n), Huella(i + 1, n), g) - V(0, 14 + Mathf.Sin(Mathf.PI * g) * 18);
                            }
                            A.Poner(gota, pos);
                            reloj.text = "Etapa " + (i + 1) + " de " + n + ": " + nombres[i];
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Volver cuesta", Duracion = 4.5f,
                    Pie = "Si algo cambia tarde, volver a una etapa anterior cuesta mucho tiempo y dinero.",
                    Preparar = (p, a) => {
                        Escalera(p, nombres, true);
                        var gota = a.Ficha(24, 24, Tizador.Cian, true);
                        var costo = a.Ficha(170, 34, Tizador.Roja, false, "¡cambio caro!");
                        A.Poner(costo, 150, 270);
                        return t => {
                            var desde = Huella(n - 1, n) - V(0, 14); var hasta = Huella(0, n) - V(0, 14);
                            // sube despacio y a trompicones: cada escalon de vuelta cuesta
                            var x = A.Tramo(t, 1.0f, 2.6f);
                            var avance = x - Mathf.Sin(x * Mathf.PI * (n - 1) * 2) * 0.03f;
                            A.Poner(gota, Vector2.Lerp(desde, hasta, Mathf.Clamp01(avance)));
                            gota.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Tizador.Cian, Tizador.Roja, x);
                            A.Alfa(costo, A.Tramo(t, 2.4f, 0.3f)); A.Escala(costo, A.Rebote(A.Tramo(t, 2.4f, 0.4f)));
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Entrega", Duracion = 4.5f,
                    Pie = "El cliente ve el producto funcionando casi al final del proyecto.",
                    Preparar = (p, a) => {
                        p.Linea(40, 200, 480, 200, Tizador.Tiza);
                        for (var i = 0; i <= 10; i++) p.Linea(40 + i * 44, 192, 40 + i * 44, 208, Tizador.Gris, 2);
                        p.Texto(40, 236, "día 1", 18, Tizador.Gris, true); p.Texto(480, 236, "último día", 18, Tizador.Gris, true);
                        p.Figura(540, 150, Tizador.Mostaza, "cliente");
                        var caja = a.Ficha(70, 50, Tizador.Cian, false, "producto");
                        var duda = a.Rotulo(540, 90, 60, 40, "?", Tizador.Mostaza, 30);
                        return t => {
                            var x = A.Tramo(t, 0.8f, 2.8f);
                            A.Poner(caja, Mathf.Lerp(60, 470, x), 165);
                            duda.text = x < 1 ? "?" : "!";
                            A.Escala((RectTransform)duda.transform, x < 1 ? 1 + 0.1f * Mathf.Sin(t * 6) : A.Rebote(A.Tramo(t, 3.6f, 0.4f)) * 1.2f);
                        };
                    }
                }
            };
        }

        // ==================================================================== continuo (Kanban…)

        private static readonly string[] Columnas = { "Por hacer", "Haciendo", "Probando", "Hecho" };
        private static float XCol(int k) { return 90 + 140 * k; }
        private static float YFila(int i) { return 80 + i * 44; }

        private static void Tablero(Tizador p, bool conTextos, string haciendo = null) {
            // Los titulos de columna, mas grandes que los del Tablero de serie (13): escalada la lamina no se leian.
            p.Tablero(20, 20, 560, 260, Tizador.Tiza, "", "", "", "");
            if (!conTextos) return;
            var titulos = new[] { Columnas[0], haciendo ?? Columnas[1], Columnas[2], Columnas[3] };
            for (var k = 0; k < 4; k++) p.Texto(XCol(k), 42, titulos[k], 17, Tizador.Tiza, true, 134);
        }

        private static int Wip(MethodologyProfile m) { return Mathf.Clamp(m.Calendario != null ? m.Calendario.LimiteWipInicial : 3, 1, 5); }

        private static Func<Tizador, A, Action<float>> ResumenContinuo(MethodologyProfile m) {
            return (p, a) => {
                Tablero(p, true, "Haciendo (máx. " + Wip(m) + ")");
                var tarjetas = new List<RectTransform>();
                for (var j = 0; j < 3; j++) tarjetas.Add(a.Ficha(110, 34, j == 0 ? Tizador.Cian : j == 1 ? Tizador.Mostaza : Tizador.Tiza));
                return t => {
                    for (var j = 0; j < 3; j++) {
                        var x = Mathf.Max(0, (t - 1.0f - 0.55f * j) / 0.9f);
                        var k = Mathf.Min(3, Mathf.FloorToInt(x));
                        var f = k >= 3 ? 1 : A.Tramo(x - k, 0.55f, 0.45f);
                        var desde = V(XCol(k), YFila(j));
                        var hacia = V(XCol(Mathf.Min(3, k + 1)), YFila(j));
                        A.Poner(tarjetas[j], A.Entre(desde, hacia, f));
                        A.Alfa(tarjetas[j], A.Tramo(t, 0.6f + 0.15f * j, 0.3f));
                    }
                };
            };
        }

        private static List<FaseDeEscena> DetalleContinuo(MethodologyProfile m) {
            var wip = Wip(m);
            var cada = m.Calendario != null ? m.Calendario.CadaCuantosDias : 0;
            return new List<FaseDeEscena> {
                new FaseDeEscena {
                    Pestana = "Tablero", Duracion = 4.5f,
                    Pie = "Todo el trabajo está a la vista en un tablero: qué falta, qué se hace y qué está hecho.",
                    Preparar = (p, a) => {
                        Tablero(p, true);
                        var n = new[] { 3, 2, 1, 2 };
                        for (var k = 0; k < 4; k++) for (var i = 0; i < n[k]; i++) p.Tarjeta(XCol(k) - 55, YFila(i) - 17, 110, 34, k == 3 ? Tizador.Cian : k == 0 ? Tizador.Gris : Tizador.Mostaza);
                        var foco = a.Ficha(136, 252, new Color(Tizador.Cian.r, Tizador.Cian.g, Tizador.Cian.b, 0.14f));
                        return t => {
                            var x = A.Tramo(t, 1.0f, 3.2f) * 4f;
                            var k = Mathf.Clamp(Mathf.FloorToInt(x), 0, 3);
                            A.Poner(foco, XCol(k), 150); A.Alfa(foco, t > 1f ? 1 : 0);
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Límite WIP", Duracion = 5f,
                    Pie = "Límite WIP: en «Haciendo» caben " + wip + (wip == 1 ? " tarea" : " tareas") + "; no entra otra hasta que alguna salga.",
                    Preparar = (p, a) => {
                        Tablero(p, true, "Haciendo (máx. " + wip + ")");
                        for (var i = 0; i < wip; i++) p.Tarjeta(XCol(1) - 55, YFila(i) - 17, 110, 34, Tizador.Mostaza);
                        var nueva = a.Ficha(110, 34, Tizador.Cian);
                        var no = a.Rotulo(XCol(1), YFila(Mathf.Min(wip, 4)), 120, 34, "lleno", Tizador.Roja, 18);
                        return t => {
                            var ida = A.Tramo(t, 1.2f, 0.7f); var vuelta = A.Tramo(t, 1.9f, 0.5f);
                            var x = Mathf.Lerp(XCol(0), XCol(0) + 70, A.Suave(ida) - A.Suave(vuelta) * 0.9f);
                            A.Poner(nueva, x, YFila(0));
                            nueva.GetComponent<UnityEngine.UI.Image>().color = vuelta > 0 ? Color.Lerp(Tizador.Cian, Tizador.Roja, vuelta) : Tizador.Cian;
                            A.Alfa(no, A.Tramo(t, 1.8f, 0.2f)); A.Escala((RectTransform)no.transform, A.Rebote(A.Tramo(t, 1.8f, 0.35f)));
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Jalar", Duracion = 4.5f,
                    Pie = "Cuando se libera un sitio, el equipo «jala» la siguiente tarea: el trabajo fluye sin empujarlo.",
                    Preparar = (p, a) => {
                        Tablero(p, true, "Haciendo (máx. " + wip + ")");
                        for (var i = 1; i < wip; i++) p.Tarjeta(XCol(1) - 55, YFila(i) - 17, 110, 34, Tizador.Mostaza);
                        var sale = a.Ficha(110, 34, Tizador.Mostaza);
                        var entra = a.Ficha(110, 34, Tizador.Cian);
                        return t => {
                            A.Poner(sale, A.Entre(V(XCol(1), YFila(0)), V(XCol(2), YFila(0)), A.Tramo(t, 1.0f, 0.8f)));
                            A.Poner(entra, A.Entre(V(XCol(0), YFila(0)), V(XCol(1), YFila(0)), A.Tramo(t, 2.1f, 0.8f)));
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Entrega continua", Duracion = 5f,
                    Pie = cada > 0 ? "Se entrega de forma continua; cada " + cada + " días se revisa el flujo." : "Se entrega de forma continua, en cuanto algo está listo.",
                    Preparar = (p, a) => {
                        Tablero(p, true);
                        var tarjetas = new List<RectTransform>();
                        for (var j = 0; j < 4; j++) tarjetas.Add(a.Ficha(110, 34, Tizador.Cian));
                        var cuenta = a.Rotulo(XCol(3), 262, 130, 30, "", Tizador.Cian, 16);
                        return t => {
                            var hechas = 0;
                            for (var j = 0; j < 4; j++) {
                                var x = A.Tramo(t, 0.9f + 0.75f * j, 1.1f);
                                if (x >= 1) hechas++;
                                A.Poner(tarjetas[j], A.Entre(V(XCol(2), YFila(0)), V(XCol(3), YFila(j)), x));
                                A.Alfa(tarjetas[j], A.Tramo(t, 0.7f + 0.75f * j, 0.2f));
                            }
                            cuenta.text = hechas + (hechas == 1 ? " entregada" : " entregadas");
                        };
                    }
                }
            };
        }
    }
}
