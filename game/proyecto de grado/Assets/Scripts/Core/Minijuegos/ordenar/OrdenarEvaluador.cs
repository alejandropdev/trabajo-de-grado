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

            Respuesta r;
            if (!cfg.Respuestas.TryGetValue(respuesta, out r))
                throw new InvalidOperationException($"'{respuesta}' no es una respuesta de {def.Id}. Validas: obedecer, rechazar, negociar.");

            // Lo que se evalua es el tablero DESPUES de contestar: si prometiste la tarjeta del cliente, entra.
            orden = AplicarRespuesta(cfg, orden, r);
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

            // Un buen orden con una mala respuesta al cliente no es un buen trabajo: priorizar incluye decir
            // que no (o negociar) a tiempo.
            var veredictoRespuesta = r.Consecuencia?.Rubrica?.Veredicto;
            if (clave == ResultadosDeMinijuego.Todos && veredictoRespuesta == "incorrecta") {
                clave = ResultadosDeMinijuego.Parcial;
                detalle.Add("El orden era bueno, pero la respuesta al cliente lo estropeó.");
            }
            return Construir(def, clave, r, detalle, respuesta);
        }

        /// <summary>
        /// El tablero tal y como queda tras contestar al cliente. No toca la lista original.
        ///   forzar-arriba  la tarjeta pedida sube a lo mas alto (aunque le falte lo que necesita)
        ///   intercambiar   la tarjeta pedida ocupa el sitio de la de menos valor que entraba y nadie necesita
        ///   ninguno        todo sigue igual
        /// </summary>
        public static List<string> AplicarRespuesta(OrdenarCfg cfg, IList<string> orden, Respuesta respuesta) {
            var final = orden == null ? new List<string>() : orden.ToList();
            var pedida = cfg?.TarjetaPedida;
            var efecto = respuesta?.EfectoEnBacklog;
            if (string.IsNullOrEmpty(pedida) || !final.Contains(pedida) || string.IsNullOrEmpty(efecto)) return final;

            if (efecto == EfectosEnBacklog.ForzarArriba) {
                final.Remove(pedida);
                final.Insert(0, pedida);
                return final;
            }
            if (efecto == EfectosEnBacklog.Intercambiar) {
                var entran = Entran(cfg, final);
                if (entran.Contains(pedida)) return final;
                var porId = cfg.Tarjetas.ToDictionary(t => t.Id);
                var necesitadas = new HashSet<string>(entran.SelectMany(id => porId[id].DependeDe));
                necesitadas.UnionWith(porId[pedida].DependeDe);
                var sale = final.Where(id => entran.Contains(id) && !necesitadas.Contains(id))
                                .OrderBy(id => porId[id].Valor).ThenByDescending(id => final.IndexOf(id))
                                .FirstOrDefault();
                if (sale == null) return final;
                var hueco = final.IndexOf(sale);   // la pedida estaba fuera, o sea mas abajo: quitarla no mueve el hueco
                final.Remove(pedida);
                final.Remove(sale);
                // La pedida va al hueco, pero nunca antes de lo que necesita.
                var trasDeps = porId[pedida].DependeDe.Where(final.Contains).Select(d => final.IndexOf(d) + 1).DefaultIfEmpty(0).Max();
                final.Insert(Math.Min(Math.Max(hueco, trasDeps), final.Count), pedida);
                // La que sale queda justo debajo de la ultima que entra: el jugador la ve caer bajo la linea.
                var ultimaDentro = Entran(cfg, final).Select(id => final.IndexOf(id)).DefaultIfEmpty(-1).Max();
                final.Insert(Math.Min(ultimaDentro + 1, final.Count), sale);
            }
            return final;
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
        /// Las dependencias rotas entre las tarjetas que entran: (la que va antes de tiempo, la que necesitaba).
        /// Es lo que la pantalla pinta en rojo mientras se ordena, y lo que el evaluador castiga al entregar.
        /// </summary>
        public static List<KeyValuePair<string, string>> Rotas(OrdenarCfg cfg, IList<string> orden) {
            var lista = new List<KeyValuePair<string, string>>();
            var entran = Entran(cfg, orden);
            foreach (var t in cfg.Tarjetas) {
                if (!entran.Contains(t.Id)) continue;
                foreach (var dep in t.DependeDe)
                    if (!entran.Contains(dep) || orden.IndexOf(dep) > orden.IndexOf(t.Id))
                        lista.Add(new KeyValuePair<string, string>(t.Id, dep));
            }
            return lista;
        }

        /// <summary>
        /// El orden en que aparece el backlog al empezar: barajado, y nunca uno que ya gane tal cual. El JSON se
        /// escribe en el orden que se le ocurre a quien lo redacta (casi siempre el bueno), y un tablero que ya
        /// esta resuelto enseña a no tocar nada. Mismo backlog y misma semilla, mismo orden.
        /// </summary>
        public static List<string> OrdenInicial(OrdenarCfg cfg, int semilla) {
            var ids = cfg.Tarjetas.Select(t => t.Id).ToList();
            if (ids.Count < 2) return ids;
            var mejor = MejorValorPosible(cfg);
            var porId = cfg.Tarjetas.ToDictionary(t => t.Id);
            for (var intento = 0; intento < 32; intento++) {
                var orden = Barajar(ids, semilla + intento * 7919);
                var capturado = Entran(cfg, orden).Sum(id => porId[id].Valor);
                var yaGana = Rotas(cfg, orden).Count == 0 && mejor > 0 && capturado >= cfg.UmbralDeValor * mejor;
                if (!yaGana) return orden;
            }
            ids.Reverse();
            return ids;
        }

        /// <summary>Fisher-Yates con un generador propio: ni System.Random ni GetHashCode dan lo mismo en todas las plataformas.</summary>
        private static List<string> Barajar(List<string> ids, int semilla) {
            uint estado = 2166136261u;
            unchecked {
                foreach (var id in ids)
                    foreach (var c in id) estado = (estado ^ (uint)c) * 16777619u;
                estado ^= (uint)semilla * 2654435761u;
            }
            if (estado == 0) estado = 1;
            var orden = ids.ToList();
            for (var i = orden.Count - 1; i > 0; i--) {
                estado ^= estado << 13; estado ^= estado >> 17; estado ^= estado << 5;
                var j = (int)(estado % (uint)(i + 1));
                var tmp = orden[i]; orden[i] = orden[j]; orden[j] = tmp;
            }
            return orden;
        }

        /// <summary>
        /// El maximo valor que cabe respetando dependencias. Fuerza bruta sobre subconjuntos: un backlog de
        /// minijuego tiene menos de 16 tarjetas, y asi el numero es exacto, no una heuristica.
        /// </summary>
        public static int MejorValorPosible(OrdenarCfg cfg) {
            int mejor;
            MejorMascara(cfg, out mejor);
            return mejor;
        }

        /// <summary>
        /// Un orden que alcanza el mejor valor: las tarjetas del mejor subconjunto, cada una despues de lo que
        /// necesita (y, entre las libres, la de mas valor primero). Es lo que se enseña al cerrar.
        /// </summary>
        public static List<string> MejorOrden(OrdenarCfg cfg) {
            int valor;
            var mascara = MejorMascara(cfg, out valor);
            var t = cfg.Tarjetas;
            var pendientes = t.Where((x, i) => (mascara & (1 << i)) != 0).ToList();
            var orden = new List<string>();
            while (pendientes.Count > 0) {
                var lista = pendientes.Where(x => x.DependeDe.All(orden.Contains)).OrderByDescending(x => x.Valor).ToList();
                if (lista.Count == 0) break;   // un ciclo; el validador no deja que llegue aqui
                orden.Add(lista[0].Id);
                pendientes.Remove(lista[0]);
            }
            return orden;
        }

        private static int MejorMascara(OrdenarCfg cfg, out int mejor) {
            var t = cfg.Tarjetas;
            var n = t.Count;
            if (n > 16) throw new InvalidOperationException("Un backlog de minijuego no puede tener mas de 16 tarjetas.");
            var indice = t.Select((x, i) => new { x.Id, i }).ToDictionary(x => x.Id, x => x.i);
            mejor = 0;
            var mejorMascara = 0;
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
                if (valido && esfuerzo <= cfg.Capacidad && valor > mejor) { mejor = valor; mejorMascara = mascara; }
            }
            return mejorMascara;
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
