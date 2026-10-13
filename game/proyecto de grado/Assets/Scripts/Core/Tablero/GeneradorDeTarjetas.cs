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

        /// <summary>
        /// El product backlog que armo el jugador, hecho tarjetas. Por cada modulo, sus tarjetas genericas («parte 1»,
        /// «parte 2») se cambian por las historias elegidas, y los puntos del modulo se reparten entre ellas segun lo
        /// que valen: la suma no cambia. Las historias que el cliente pidio y se dejaron fuera entran igual, las
        /// ultimas y marcadas: ese trabajo existia aunque nadie lo apuntara. Sin backlog elegido no hace nada.
        /// </summary>
        public static void AplicarHistorias(TableroDelEquipo t, Nexus.Core.Proyecto.FichaDelProyecto f, IList<string> idsEnOrden) {
            if (t == null || f == null || idsEnOrden == null || idsEnOrden.Count == 0) return;
            var orden = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < idsEnOrden.Count; i++) if (!orden.ContainsKey(idsEnOrden[i])) orden[idsEnOrden[i]] = i;

            var nuevas = new List<TarjetaDeTrabajo>();
            foreach (var modulo in f.Modulos) {
                var viejas = t.Tarjetas.Where(c => !c.EsBug && c.Modulo == modulo.Id).ToList();
                if (viejas.Count == 0) continue;
                var historias = f.Historias.Where(h => h != null && h.Modulo == modulo.Id &&
                                                       (orden.ContainsKey(h.Id) || h.Tipo == Nexus.Core.Proyecto.TiposDeHistoria.Pedida)).ToList();
                if (historias.Count == 0) { nuevas.AddRange(viejas); continue; }   // un modulo sin historias se queda como estaba

                var puntos = viejas.Sum(c => c.Puntos);
                var pesos = historias.Sum(h => (double)Math.Max(1, h.Valor));
                var repartido = 0.0;
                for (var i = 0; i < historias.Count; i++) {
                    var h = historias[i];
                    // La ultima se lleva lo que falte: la suma del modulo es EXACTAMENTE la de antes.
                    var p = i == historias.Count - 1 ? Math.Round(puntos - repartido, 2) : Math.Round(puntos * Math.Max(1, h.Valor) / pesos, 2);
                    repartido += p;
                    var tarde = !orden.ContainsKey(h.Id);
                    nuevas.Add(new TarjetaDeTrabajo {
                        Id = "T" + (++t.Generadas).ToString("00"),
                        Titulo = (tarde ? "Se descubrió tarde: " : "") + h.Titulo,
                        Modulo = modulo.Id, Historia = h.Id, DescubiertaTarde = tarde,
                        Tipo = viejas[i % viejas.Count].Tipo, Puntos = p,
                        // Lo elegido, en el orden del jugador; lo descubierto tarde, detras de todo.
                        Orden = tarde ? 10000 + nuevas.Count : orden[h.Id]
                    });
                }
            }
            // Lo que no cuelga de un modulo (no deberia haber nada todavia) se conserva.
            nuevas.AddRange(t.Tarjetas.Where(c => c.EsBug || f.Modulos.All(m => m.Id != c.Modulo)));
            // El orden queda compacto y sin empates: 0, 1, 2…
            var n = 0;
            foreach (var c in nuevas.OrderBy(c => c.Orden).ToList()) c.Orden = n++;
            t.Tarjetas = nuevas.OrderBy(c => c.Orden).ToList();
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
                    Id = m.Id, Nombre = m.Nombre, Rol = m.Rol, Personaje = m.Personaje, Descripcion = m.Descripcion, Frase = m.Frase,
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
