using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Repartir {
    /// <summary>
    /// V2 · repartir un presupuesto escaso (las horas de pruebas, MJ-F2-08). Cada deposito encuentra un
    /// defecto por cada 'costePorDefecto' horas, hasta agotar los que hay escondidos de ese tipo. Lo que no
    /// se encuentra, escapa — y escapa exactamente del tipo en el que no se invirtio.
    ///
    ///   todos          escapan como mucho 'toleranciaDeEscapes'
    ///   falsoPositivo  se sobrecompro un tipo (mas horas de las que tenia sentido) mientras otro con
    ///                  defectos se quedo a cero: el error clasico de «cobertura alta» en un solo sitio
    ///   parcial        escapan demasiados
    ///   omitido        no se asigno ni una hora
    /// </summary>
    public static class RepartirEvaluador {
        public sealed class Reparto {
            public string DepositoId;
            public int Horas;
            public int Encontrados;
            public int Escapan;
        }

        public static List<Reparto> Calcular(RepartirCfg cfg, IDictionary<string, int> asignacion) {
            var lista = new List<Reparto>();
            foreach (var d in cfg.Depositos) {
                int horas;
                if (asignacion == null || !asignacion.TryGetValue(d.Id, out horas)) horas = 0;
                if (horas < 0) throw new InvalidOperationException($"'{d.Id}' tiene horas negativas.");
                var encontrados = Math.Min(d.DefectosOcultos, horas / d.CostePorDefecto);
                lista.Add(new Reparto { DepositoId = d.Id, Horas = horas, Encontrados = encontrados, Escapan = d.DefectosOcultos - encontrados });
            }
            return lista;
        }

        public static ResultadoMinijuego Evaluar(MinijuegoDef def, IDictionary<string, int> asignacion) {
            if (def == null) throw new ArgumentNullException(nameof(def));
            var cfg = def.Repartir ?? throw new InvalidOperationException($"{def.Id} no es un V2: no tiene bloque 'repartir'.");

            var total = asignacion == null ? 0 : asignacion.Values.Sum();
            if (total > cfg.Presupuesto)
                throw new InvalidOperationException($"Se repartieron {total} {cfg.Unidad} y solo hay {cfg.Presupuesto}.");
            if (total == 0) return Construir(def, ResultadosDeMinijuego.Omitido, new List<string>());

            var reparto = Calcular(cfg, asignacion);
            var porId = cfg.Depositos.ToDictionary(d => d.Id);
            var detalle = new List<string>();
            foreach (var r in reparto) {
                var d = porId[r.DepositoId];
                detalle.Add(r.Escapan == 0
                    ? $"{d.Nombre}: {r.Horas} {cfg.Unidad}, encontraste los {r.Encontrados} defectos que había."
                    : $"{d.Nombre}: {r.Horas} {cfg.Unidad}, encontraste {r.Encontrados} de {d.DefectosOcultos}. {r.Escapan} llegaron al cliente.");
            }

            var escapan = reparto.Sum(r => r.Escapan);
            var sobrecompra = reparto.Any(r => r.Horas > porId[r.DepositoId].CostePorDefecto * porId[r.DepositoId].DefectosOcultos);
            var desatendido = reparto.Any(r => r.Horas == 0 && porId[r.DepositoId].DefectosOcultos > 0);

            string clave;
            if (escapan <= cfg.ToleranciaDeEscapes) clave = ResultadosDeMinijuego.Todos;
            else if (sobrecompra && desatendido) clave = ResultadosDeMinijuego.FalsoPositivo;
            else clave = ResultadosDeMinijuego.Parcial;

            if (escapan > 0) {
                var sinNada = reparto.Where(r => r.Escapan > 0 && r.Horas == 0).Select(r => porId[r.DepositoId].Nombre.ToLowerInvariant()).ToList();
                var corto = reparto.Where(r => r.Escapan > 0 && r.Horas > 0).Select(r => porId[r.DepositoId].Nombre.ToLowerInvariant()).ToList();
                if (sinNada.Count > 0)
                    detalle.Add("Cada tipo de prueba solo encuentra sus propios errores. No pusiste horas en: " + string.Join(", ", sinNada) + ", y por ahí se escaparon.");
                if (corto.Count > 0)
                    detalle.Add("Pusiste horas, pero no las suficientes, en: " + string.Join(", ", corto) + ".");
            }
            return Construir(def, clave, detalle);
        }

        /// <summary>
        /// Los repartos que ganan ("todos"), contando solo los que se pueden hacer con los botones (multiplos de
        /// 'paso') y sin desperdiciar horas en un tipo que ya lo encontro todo. Sirve para que el validador
        /// rechace una escena imposible y para que un test la pruebe entera.
        /// </summary>
        public static List<Dictionary<string, int>> RepartosGanadores(RepartirCfg cfg) {
            var ganadores = new List<Dictionary<string, int>>();
            if (cfg == null || cfg.Depositos.Count == 0) return ganadores;
            var paso = Math.Max(1, cfg.Paso);
            var actual = new Dictionary<string, int>();

            Action<int, int> probar = null;
            probar = (i, restante) => {
                if (i == cfg.Depositos.Count) {
                    var escapan = Calcular(cfg, actual).Sum(r => r.Escapan);
                    if (actual.Values.Sum() > 0 && escapan <= cfg.ToleranciaDeEscapes)
                        ganadores.Add(new Dictionary<string, int>(actual));
                    return;
                }
                var d = cfg.Depositos[i];
                var utiles = d.CostePorDefecto * d.DefectosOcultos;
                var tope = Math.Min(restante, ((utiles + paso - 1) / paso) * paso);
                for (var h = 0; h <= tope; h += paso) {
                    actual[d.Id] = h;
                    probar(i + 1, restante - h);
                }
                actual.Remove(d.Id);
            };
            probar(0, cfg.Presupuesto);
            return ganadores;
        }

        private static ResultadoMinijuego Construir(MinijuegoDef def, string clave, List<string> detalle) {
            Consecuencia c;
            if (!def.Consecuencias.TryGetValue(clave, out c)) c = new Consecuencia();
            return new ResultadoMinijuego {
                MinijuegoId = def.Id,
                Resultado = clave,
                Rubrica = c.Rubrica,
                EfectosInmediatos = new Dictionary<string, float>(c.EfectosInmediatos),
                EfectosDiferidos = c.EfectosDiferidos.ToList(),
                Hallazgos = c.Hallazgos.ToList(),
                Detalle = detalle,
                TextoCierre = def.Cierre?.Texto
            };
        }
    }
}
