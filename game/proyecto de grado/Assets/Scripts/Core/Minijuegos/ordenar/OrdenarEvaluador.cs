using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Ordenar {
    /// <summary>
    /// V3 · ordenar con restricciones (el backlog, MJ-F1-08). El jugador pone las tarjetas en orden; entran,
    /// de arriba abajo, las que caben en la capacidad. Despues responde al cliente: obedecer, rechazar o
    /// negociar.
    ///
    ///   falsoPositivo  una tarjeta entro sin lo que necesita (una dependencia detras, o fuera)
    ///   todos          orden valido y se capturo al menos el umbral del MEJOR valor posible
    ///   parcial        orden valido, pero se dejo fuera demasiado valor
    ///   omitido        no envio nada
    ///
    /// El "mejor valor posible" se calcula de verdad (hay pocas tarjetas: se prueban todas las combinaciones
    /// que respetan dependencias y capacidad), asi que la rubrica no depende de una solucion escrita a mano.
    /// </summary>
    public static class OrdenarEvaluador {
        public static ResultadoMinijuego Evaluar(MinijuegoDef def, IList<string> orden, string respuesta) {
            if (def == null) throw new ArgumentNullException(nameof(def));
            var cfg = def.Ordenar ?? throw new InvalidOperationException($"{def.Id} no es un V3: no tiene bloque 'ordenar'.");

            if (orden == null || orden.Count == 0 || string.IsNullOrEmpty(respuesta))
                return Construir(def, ResultadosDeMinijuego.Omitido, null, new List<string>());

            var porId = cfg.Tarjetas.ToDictionary(t => t.Id);
            foreach (var id in orden)
                if (!porId.ContainsKey(id)) throw new InvalidOperationException($"'{id}' no es una tarjeta de {def.Id}.");

            var entran = Entran(cfg, orden);
            var detalle = new List<string>();

            var violaciones = new List<string>();
            var posicion = orden.Select((id, i) => new { id, i }).ToDictionary(x => x.id, x => x.i);
            foreach (var id in entran)
                foreach (var dep in porId[id].DependeDe) {
                    if (!entran.Contains(dep))
                        violaciones.Add($"«{porId[id].Titulo}» entró sin «{porId[dep].Titulo}», que necesita.");
                    else if (posicion[dep] > posicion[id])
                        violaciones.Add($"«{porId[id].Titulo}» va antes que «{porId[dep].Titulo}», y depende de ella.");
                }
            detalle.AddRange(violaciones);

            var capturado = entran.Sum(id => porId[id].Valor);
            var mejor = MejorValorPosible(cfg);
            detalle.Add($"Entraron {entran.Count} de {cfg.Tarjetas.Count} tarjetas: {capturado} de valor, cuando el mejor orden posible daba {mejor}.");

            string clave;
            if (violaciones.Count > 0) clave = ResultadosDeMinijuego.FalsoPositivo;
            else if (mejor > 0 && capturado >= cfg.UmbralDeValor * mejor) clave = ResultadosDeMinijuego.Todos;
            else clave = ResultadosDeMinijuego.Parcial;

            Respuesta r;
            if (!cfg.Respuestas.TryGetValue(respuesta, out r))
                throw new InvalidOperationException($"'{respuesta}' no es una respuesta de {def.Id}. Validas: obedecer, rechazar, negociar.");
            return Construir(def, clave, r, detalle, respuesta);
        }

        /// <summary>Las tarjetas que caben, de arriba abajo, hasta la primera que ya no cabe.</summary>
        public static HashSet<string> Entran(OrdenarCfg cfg, IList<string> orden) {
            var porId = cfg.Tarjetas.ToDictionary(t => t.Id);
            var entran = new HashSet<string>();
            var usado = 0;
            foreach (var id in orden) {
                if (usado + porId[id].Esfuerzo > cfg.Capacidad) break;
                usado += porId[id].Esfuerzo;
                entran.Add(id);
            }
            return entran;
        }

        /// <summary>
        /// El maximo valor que cabe respetando dependencias. Fuerza bruta sobre subconjuntos: un backlog de
        /// minijuego tiene menos de 16 tarjetas, y asi el numero es exacto, no una heuristica.
        /// </summary>
        public static int MejorValorPosible(OrdenarCfg cfg) {
            var t = cfg.Tarjetas;
            var n = t.Count;
            if (n > 16) throw new InvalidOperationException("Un backlog de minijuego no puede tener mas de 16 tarjetas.");
            var indice = t.Select((x, i) => new { x.Id, i }).ToDictionary(x => x.Id, x => x.i);
            var mejor = 0;
            for (var mascara = 0; mascara < (1 << n); mascara++) {
                var esfuerzo = 0;
                var valor = 0;
                var valido = true;
                for (var i = 0; i < n && valido; i++) {
                    if ((mascara & (1 << i)) == 0) continue;
                    esfuerzo += t[i].Esfuerzo;
                    valor += t[i].Valor;
                    foreach (var dep in t[i].DependeDe)
                        if ((mascara & (1 << indice[dep])) == 0) valido = false;
                }
                if (valido && esfuerzo <= cfg.Capacidad && valor > mejor) mejor = valor;
            }
            return mejor;
        }

        public static bool HayCiclo(List<Tarjeta> tarjetas) {
            var porId = new Dictionary<string, Tarjeta>();
            foreach (var t in tarjetas) if (t != null && t.Id != null) porId[t.Id] = t;
            var estado = new Dictionary<string, int>();   // 0 sin visitar, 1 en curso, 2 terminado

            Func<string, bool> visitar = null;
            visitar = id => {
                int e;
                estado.TryGetValue(id, out e);
                if (e == 1) return true;
                if (e == 2) return false;
                estado[id] = 1;
                Tarjeta tarjeta;
                if (porId.TryGetValue(id, out tarjeta))
                    foreach (var dep in tarjeta.DependeDe)
                        if (porId.ContainsKey(dep) && visitar(dep)) return true;
                estado[id] = 2;
                return false;
            };
            return porId.Keys.Any(id => visitar(id));
        }

        private static ResultadoMinijuego Construir(MinijuegoDef def, string clave, Respuesta respuesta,
                                                    List<string> detalle, string idRespuesta = null) {
            Consecuencia c;
            if (!def.Consecuencias.TryGetValue(clave, out c)) c = new Consecuencia();
            var res = new ResultadoMinijuego {
                MinijuegoId = def.Id,
                Resultado = clave,
                Rubrica = c.Rubrica,
                EfectosInmediatos = new Dictionary<string, float>(c.EfectosInmediatos),
                EfectosDiferidos = c.EfectosDiferidos.ToList(),
                Hallazgos = c.Hallazgos.ToList(),
                Detalle = detalle,
                TextoCierre = def.Cierre?.Texto
            };

            // La respuesta al cliente suma sus propias consecuencias: negociar puede agendar un evento
            // (el cliente que entiende) que obedecer o rechazar no agendan nunca.
            if (respuesta != null) {
                var rc = respuesta.Consecuencia ?? new Consecuencia();
                foreach (var kv in rc.EfectosInmediatos) {
                    float previo;
                    res.EfectosInmediatos.TryGetValue(kv.Key, out previo);
                    res.EfectosInmediatos[kv.Key] = previo + kv.Value;
                }
                res.EfectosDiferidos.AddRange(rc.EfectosDiferidos);
                res.Hallazgos.AddRange(rc.Hallazgos);
                res.Hallazgos.Add("respuesta:" + idRespuesta);
                if (!string.IsNullOrEmpty(respuesta.Texto)) res.Detalle.Add("Al cliente: " + respuesta.Texto);
                if (rc.Rubrica != null && !string.IsNullOrEmpty(rc.Rubrica.Razon)) res.Detalle.Add(rc.Rubrica.Razon);
            }
            return res;
        }
    }
}
