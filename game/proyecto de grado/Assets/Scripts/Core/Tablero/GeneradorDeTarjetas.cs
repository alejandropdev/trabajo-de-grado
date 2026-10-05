using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;

namespace Nexus.Core.Tablero {
    /// <summary>
    /// Convierte el alcance del nivel en tarjetas. Reparte los puntos por modulo igual que AvanceDeModulos (por
    /// peso y por lo que les falta) y va cortando cada modulo en tarjetas de 1, 2, 3 o 5 puntos. Todo sale de una
    /// funcion pura de (perfil, semilla): la misma partida genera siempre el mismo tablero, y no gasta ni una tirada
    /// del azar de la sesion (desplazaria todas las partidas calibradas).
    /// </summary>
    public static class GeneradorDeTarjetas {
        private static readonly double[] Tamanos = { 1, 2, 3, 5 };
        private static readonly string[] Tipos = { "interfaz", "datos", "logica" };

        public static TableroDelEquipo Crear(LevelProfile perfil, double alcance, int semilla) {
            if (perfil == null) throw new ArgumentNullException(nameof(perfil));
            var t = new TableroDelEquipo();
            t.Miembros = MiembrosDe(perfil);

            var modulos = perfil.Proyecto != null && perfil.Proyecto.Modulos.Count > 0
                ? perfil.Proyecto.Modulos.Select(m => new { m.Id, m.Nombre, Faltan = Math.Max(0, m.Peso) * (100 - Limitar(m.AvanceInicial)) / 100 }).ToList()
                : new[] { new { Id = "trabajo", Nombre = "El trabajo", Faltan = 1.0 } }.ToList();
            var total = modulos.Sum(m => m.Faltan);
            if (total <= 0) { total = modulos.Count; modulos = modulos.Select(m => new { m.Id, m.Nombre, Faltan = 1.0 }).ToList(); }

            var acumulado = 0.0;
            for (var i = 0; i < modulos.Count; i++) {
                var m = modulos[i];
                // El ultimo modulo se lleva lo que falte, para que la suma sea EXACTAMENTE el alcance.
                var puntos = i == modulos.Count - 1 ? alcance - acumulado : Math.Round(alcance * m.Faltan / total, 2);
                acumulado += puntos;
                Cortar(t, m.Id, m.Nombre, puntos, semilla);
            }
            return t;
        }

        /// <summary>Añade tarjetas hasta sumar 'puntos' (un cambio pedido, un bug).</summary>
        public static void Cortar(TableroDelEquipo t, string moduloId, string nombre, double puntos, int semilla,
                                  bool bug = false, int dia = 0, string tipo = null) {
            var restante = Math.Round(puntos, 2);
            var n = 0;
            while (restante > 0.005) {
                n++;
                var h = Hash(semilla, moduloId + ":" + n + ":" + t.Generadas);
                var tam = Tamanos[h % Tamanos.Length];
                // Lo que quede por debajo de medio punto se absorbe en esta tarjeta: no hay tarjetas de 0,1 puntos.
                if (tam > restante || restante - tam < 0.5) tam = restante;
                tam = Math.Round(tam, 2);
                t.Tarjetas.Add(new TarjetaDeTrabajo {
                    Id = "T" + (++t.Generadas).ToString("00"),
                    Titulo = bug ? "Arreglar: " + nombre : nombre + " · parte " + n,
                    Modulo = bug ? null : moduloId,
                    Tipo = tipo ?? (bug ? "pruebas" : Tipos[(h / 7) % Tipos.Length]),
                    Puntos = tam, EsBug = bug, DiaEntrada = dia,
                    Orden = t.Tarjetas.Count == 0 ? 0 : t.Tarjetas.Max(x => x.Orden) + 1
                });
                restante = Math.Round(restante - tam, 2);
            }
        }

        /// <summary>El equipo del nivel; sin bloque «equipo», dos compañeros genericos (el tablero nunca esta vacio).</summary>
        private static List<MiembroDelEquipo> MiembrosDe(LevelProfile perfil) {
            if (perfil.Equipo != null && perfil.Equipo.Count > 0)
                return perfil.Equipo.Select(m => new MiembroDelEquipo {
                    Id = m.Id, Nombre = m.Nombre, Rol = m.Rol, Personaje = m.Personaje,
                    Habilidades = new Dictionary<string, double>(m.Habilidades ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase)
                }).ToList();
            return new List<MiembroDelEquipo> {
                new MiembroDelEquipo { Id = "equipo-1", Nombre = "Compañero 1", Rol = "Desarrollo" },
                new MiembroDelEquipo { Id = "equipo-2", Nombre = "Compañero 2", Rol = "Desarrollo" }
            };
        }

        private static double Limitar(double v) { return Math.Max(0, Math.Min(100, v)); }

        /// <summary>Un hash estable (string.GetHashCode cambia entre ejecuciones y entre plataformas).</summary>
        public static int Hash(int semilla, string s) {
            unchecked {
                var h = 17 + semilla * 31;
                foreach (var ch in s ?? "") h = h * 31 + ch;
                h ^= h >> 15; h *= 0x2c1b3c6d; h ^= h >> 12;
                return h & 0x7fffffff;
            }
        }
    }
}
