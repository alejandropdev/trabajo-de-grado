using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using A = Nexus.Unity.Pantallas.ActoresDeEscena;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El cliente contado en tres escenas animadas de 5 s (2026-11-08), con la pizarra de las metodologias:
    ///   · Quien es:     su edificio y las personas, con su nombre.
    ///   · Que le duele: la fila que crece, el reloj y la cara de disgusto.
    ///   · Como trabaja: el papel que pasa de mano en mano por el mostrador.
    /// El pie de cada una es la frase de la ficha, entera (la pizarra le da hasta tres lineas).
    /// </summary>
    internal static class EscenasDelCliente {
        public static List<FaseDeEscena> Fases(FichaDelProyecto f) {
            var usuarios = Mathf.Clamp(f.Usuarios.Count, 2, 4);
            return new List<FaseDeEscena> {
                new FaseDeEscena {
                    Pestana = "Quién es", Duracion = 5f, Pie = PiezasDeProyecto.PrimeraFrase(f.Cliente.QueHace, 260),
                    Preparar = (p, a) => {
                        p.Edificio(70, 50, 190, 200, Tizador.Cian);
                        for (var i = 0; i < usuarios; i++) p.Figura(340 + i * 64, 175, i == 0 ? Tizador.Mostaza : Tizador.Tiza);
                        p.Flecha(280, 180, 320, 180, Tizador.Gris);
                        var nombre = a.Rotulo(430, 70, 300, 70, f.Cliente.Nombre, Tizador.Mostaza, 30);
                        var sector = a.Rotulo(430, 270, 320, 30, f.Sector.Nombre, Tizador.Gris, 20);
                        return t => {
                            var x = A.Tramo(t, 0.9f, 0.4f);
                            A.Alfa(nombre, x); A.Escala((RectTransform)nombre.transform, A.Rebote(x));
                            A.Alfa(sector, A.Tramo(t, 1.6f, 0.4f));
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Qué le duele", Duracion = 5f, Pie = PiezasDeProyecto.PrimeraFrase(f.Cliente.QueLeDuele, 260),
                    Preparar = (p, a) => {
                        p.Caja(40, 120, 110, 80, null, Tizador.Gris);
                        p.Reloj(520, 70);
                        p.Carita(500, 205, 38, -1, Tizador.Roja);
                        var fila = new List<RectTransform>();
                        for (var i = 0; i < 7; i++) {
                            var c = a.Ficha(34, 34, Tizador.Tiza, true);
                            A.Poner(c, 190 + i * 40, 210);
                            fila.Add(c);
                        }
                        var enfado = a.Rotulo(500, 262, 150, 28, "¡qué espera!", Tizador.Roja, 20);
                        return t => {
                            for (var i = 0; i < fila.Count; i++) {
                                var x = A.Tramo(t, 0.7f + i * 0.4f, 0.35f);
                                A.Alfa(fila[i], x); A.Escala(fila[i], A.Rebote(x));
                            }
                            A.Alfa(enfado, A.Tramo(t, 3.6f, 0.4f));
                        };
                    }
                },
                new FaseDeEscena {
                    Pestana = "Cómo trabaja", Duracion = 5f, Pie = PiezasDeProyecto.PrimeraFrase(f.Sector.ComoTrabaja, 260),
                    Preparar = (p, a) => {
                        p.Figura(70, 140, Tizador.Tiza);
                        p.Caja(220, 100, 160, 110, null, Tizador.Cian);
                        p.Figura(530, 140, Tizador.Mostaza);
                        p.Flecha(110, 175, 210, 175, Tizador.Gris);
                        p.Flecha(390, 175, 480, 175, Tizador.Gris);
                        var papel = a.Ficha(46, 32, Tizador.Cian);
                        return t => {
                            var x = A.Tramo(t, 0.8f, 3.6f);
                            // el papel va, se queda un momento en el mostrador y sigue
                            var pos = x < 0.4f ? Mathf.Lerp(90, 300, x / 0.4f)
                                    : x < 0.6f ? 300
                                    : Mathf.Lerp(300, 510, (x - 0.6f) / 0.4f);
                            A.Poner(papel, pos, 160);
                            A.Escala(papel, 1 + 0.18f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((x - 0.4f) / 0.2f)));
                        };
                    }
                }
            };
        }
    }
}
