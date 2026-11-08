using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using A = Nexus.Unity.Pantallas.ActoresDeEscena;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El backlog contado en tres escenas animadas y GENERICAS (2026-11-08): no destapan las historias reales ni cuales sobran
    /// (eso es lo que se decide en la parte 2), solo la idea.
    ///   · Las historias:       cada una es algo que alguien necesita.
    ///   · El orden:            arriba, lo que mas vale.
    ///   · Lo que nadie pidio:  no entra en el backlog.
    /// </summary>
    internal static class EscenasDelBacklog {
        public static List<FaseDeEscena> Fases() {
            var colores = new[] { Tizador.Cian, Tizador.Mostaza, Tizador.Tiza, Tizador.Cian };
            return new List<FaseDeEscena> {
                new FaseDeEscena {
                    Pestana = "Las historias", Duracion = 5f, Pie = "Cada historia es algo que alguien necesita del sistema.",
                    Preparar = (p, a) => {
                        p.Figura(50, 130, Tizador.Mostaza);
                        var fichas = new List<RectTransform>();
                        for (var i = 0; i < 4; i++) {
                            var c = a.Ficha(120, 54, colores[i], false, "historia", Tizador.Gris);
                            A.Poner(c, 200 + (i % 2) * 150, 70 + (i / 2) * 90);
                            fichas.Add(c);
                        }
                        return t => {
                            for (var i = 0; i < fichas.Count; i++) {
                                var x = A.Tramo(t, 0.6f + i * 0.7f, 0.4f);
                                A.Alfa(fichas[i], x); A.Escala(fichas[i], A.Rebote(x));
                            }
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "El orden", Duracion = 5f, Pie = "El orden manda: arriba, lo que más le vale al cliente.",
                    Preparar = (p, a) => {
                        p.Flecha(150, 250, 150, 50, Tizador.Cian);
                        var arriba = a.Rotulo(10, 20, 300, 26, "lo que más vale", Tizador.Cian, 20);
                        var fichas = new List<RectTransform>();
                        for (var i = 0; i < 4; i++) {
                            var c = a.Ficha(140, 40, colores[i], false);
                            fichas.Add(c);
                        }
                        return t => {
                            A.Alfa(arriba, A.Tramo(t, 0.4f, 0.4f));
                            for (var i = 0; i < fichas.Count; i++) {
                                var x = A.Tramo(t, 0.8f + i * 0.2f, 1.8f);
                                var e = 1 - Mathf.Pow(1 - x, 3);
                                // del revoltijo a la fila ordenada
                                var desde = new Vector2(320 + (i * 53 % 140), 60 + (i * 71 % 180));
                                var hasta = new Vector2(230, 60 + i * 56);
                                A.Poner(fichas[i], Vector2.Lerp(desde, hasta, e));
                                A.Alfa(fichas[i], A.Tramo(t, 0.2f + i * 0.15f, 0.3f));
                            }
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Lo que nadie pidió", Duracion = 5f, Pie = "Si nadie lo pidió, no entra: sobra trabajo.",
                    Preparar = (p, a) => {
                        p.Caja(60, 120, 150, 110, null, Tizador.Cian);
                        p.Flecha(230, 175, 340, 175, Tizador.Gris);
                        var cola = a.Rotulo(40, 240, 200, 24, "el backlog", Tizador.Cian, 20);
                        var buena = a.Ficha(110, 40, Tizador.Cian);
                        var intrusa = a.Ficha(110, 40, Tizador.Roja);
                        var no = a.Rotulo(380, 140, 220, 30, "nadie la pidió", Tizador.Roja, 22);
                        return t => {
                            A.Alfa(cola, A.Tramo(t, 0.3f, 0.4f));
                            var x = A.Tramo(t, 0.6f, 1.2f);
                            A.Poner(buena, Vector2.Lerp(new Vector2(260, 40), new Vector2(100, 160), x));
                            var y = A.Tramo(t, 2.2f, 1.6f);
                            A.Poner(intrusa, Vector2.Lerp(new Vector2(260, 110), new Vector2(470, 175), y));
                            A.Escala(intrusa, 1 - 0.25f * y);
                            A.Alfa(no, A.Tramo(t, 3.6f, 0.4f));
                        };
                    }
                }
            };
        }
    }
}
