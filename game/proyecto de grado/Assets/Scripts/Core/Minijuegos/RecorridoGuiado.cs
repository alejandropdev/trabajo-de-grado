using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos.Detectar;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Core.Minijuegos.Repartir;

namespace Nexus.Core.Minijuegos {
    /// <summary>Lo que el modo guiado le pide al jugador en cada paso.</summary>
    public enum AccionGuiada {
        /// <summary>Solo leer y pulsar «Entendido».</summary>
        Leer,
        /// <summary>Detectar: pinchar la pieza 'Objetivo'.</summary>
        Seleccionar,
        /// <summary>Detectar: poner la etiqueta 'Objetivo' a lo seleccionado.</summary>
        Etiquetar,
        /// <summary>Ordenar: llevar la tarjeta 'Objetivo' al puesto 'Valor' (0 = arriba).</summary>
        Mover,
        /// <summary>Ordenar: dar el orden por bueno y pasar a contestar al cliente.</summary>
        Listo,
        /// <summary>Ordenar: contestar 'Objetivo' (obedecer | rechazar | negociar).</summary>
        Responder,
        /// <summary>Repartir: dejar el deposito 'Objetivo' con 'Valor' horas.</summary>
        Asignar,
        /// <summary>Pulsar «Entregar».</summary>
        Entregar
    }

    public sealed class PasoGuiado {
        public AccionGuiada Accion;
        /// <summary>La pieza, tarjeta, deposito, etiqueta o respuesta a la que se refiere (y la que se señala).</summary>
        public string Objetivo;
        public int Valor;
        /// <summary>Lo que hay que hacer, en una frase.</summary>
        public string Texto;
        /// <summary>Por que, y que hay que mirar para saberlo la proxima vez sin ayuda.</summary>
        public string Porque;
        /// <summary>Si su porque entra en el resumen final de «lo que tienes que mirar la próxima vez».</summary>
        public bool EsLeccion;
    }

    /// <summary>
    /// El modo guiado: Marisol lleva al jugador de la mano por TODO el minijuego hasta hacerlo bien, diciendo en
    /// cada paso que hacer y, sobre todo, que mirar para saberlo solo la proxima vez. Se genera a partir de los
    /// datos de la escena (las zonas y sus explicaciones, el mejor orden del backlog, el reparto ganador), asi que
    /// cualquier escena nueva tiene su recorrido sin escribirlo a mano. Seguirlo al pie de la letra da siempre
    /// «todos» (lo comprueba RecorridoGuiadoTests con Aplicar).
    /// </summary>
    public static class RecorridoGuiado {
        public static List<PasoGuiado> Para(MinijuegoDef def) {
            if (def == null) throw new ArgumentNullException(nameof(def));
            List<PasoGuiado> pasos;
            switch (Verbos.Normalizar(def.Verbo)) {
                case Verbos.Ordenar: pasos = Ordenar(def); break;
                case Verbos.Repartir: pasos = Repartir(def); break;
                default: pasos = Detectar(def); break;
            }
            // Lo primero: en que parte del proyecto estamos. Sin eso, las piezas son nombres sueltos.
            if (!string.IsNullOrEmpty(def.EnElProyecto))
                pasos.Insert(0, new PasoGuiado {
                    Accion = AccionGuiada.Leer, Texto = "Primero, dónde estamos en el proyecto.",
                    Porque = def.EnElProyecto + " Antes de tocar nada, lee qué es cada pieza o tarjeta: todas son partes de ese sistema.",
                    EsLeccion = false
                });
            return pasos;
        }

        /// <summary>Lo esencial del recorrido, para el cierre: «la próxima vez, mira esto».</summary>
        public static List<string> Lecciones(IEnumerable<PasoGuiado> pasos) {
            return pasos.Where(p => p.EsLeccion && !string.IsNullOrEmpty(p.Porque)).Select(p => p.Porque).Distinct().ToList();
        }

        // ==================================================================== V1 · detectar

        private static List<PasoGuiado> Detectar(MinijuegoDef def) {
            var pasos = new List<PasoGuiado>();
            string queSeBusca;
            switch (def.Lienzo) {
                case "grafo_commits":
                    queSeBusca = "Esto es el historial de git: cada punto es un commit (una versión guardada) y cada columna, una rama. " +
                                 "Al revisarlo te preguntas: ¿hay commits copiados de otros, con la misma fecha? ¿Quién firma cada trabajo? " +
                                 "¿Se reescribió algo que ya era de todos?";
                    break;
                case "secuencia":
                    queSeBusca = "A la izquierda, lo diseñado; a la derecha, lo que pasó de verdad. Se compara fila a fila: " +
                                 "¿pasó en el mismo orden? ¿algo pasó dos veces? ¿pasó algo que el diseño no tenía?";
                    break;
                default:
                    queSeBusca = "En un diagrama de componentes, cada caja es una pieza y cada flecha dice quién le habla a quién. " +
                                 "Al revisarlo te preguntas tres cosas: ¿hace TODO lo que pide el encargo? ¿cada pieza habla con otra " +
                                 "solo por su puerta de entrada? ¿hay alguna pieza que decide algo sin que nadie haya dicho cómo?";
                    break;
            }
            pasos.Add(new PasoGuiado { Accion = AccionGuiada.Leer, Texto = "Antes de pinchar nada: qué se busca aquí.", Porque = queSeBusca, EsLeccion = true });

            foreach (var z in def.Zonas) {
                for (var i = 0; i < z.Commits.Count; i++) {
                    var pieza = z.Commits[i];
                    pasos.Add(new PasoGuiado {
                        Accion = AccionGuiada.Seleccionar, Objetivo = pieza,
                        Texto = (i == 0 ? "Pincha " : "Pincha también ") + Nombre(def, pieza) + ".",
                        Porque = i == 0 ? (z.Pista ?? "Fíjate en esta pieza y compárala con lo que pide el encargo: algo no cuadra.") : "Es parte del mismo problema: van juntas.",
                        EsLeccion = i == 0 && !string.IsNullOrEmpty(z.Pista)
                    });
                }
                string que;
                EtiquetasDeDefecto.QueEs.TryGetValue(z.Defecto ?? "", out que);
                pasos.Add(new PasoGuiado {
                    Accion = AccionGuiada.Etiquetar, Objetivo = z.Defecto,
                    Texto = $"Ahora pulsa, a la derecha, «{EtiquetasDeDefecto.Humanizar(z.Defecto)}».",
                    Porque = $"«{EtiquetasDeDefecto.Humanizar(z.Defecto)}» quiere decir: {que ?? "es el tipo de problema que tiene esta pieza"}."
                });
                pasos.Add(new PasoGuiado {
                    Accion = AccionGuiada.Leer, Objetivo = z.Commits.FirstOrDefault(),
                    Texto = "¿Por qué es un problema?", Porque = z.Explicacion, EsLeccion = true
                });
            }

            foreach (var s in def.Senuelos) {
                var pieza = s.Commits.FirstOrDefault();
                pasos.Add(new PasoGuiado {
                    Accion = AccionGuiada.Leer, Objetivo = pieza,
                    Texto = $"Mira {Nombre(def, pieza)}: parece raro, pero NO se marca.",
                    Porque = s.RazonNoEsDefecto + " Marcar lo que está bien también cuenta en contra.", EsLeccion = true
                });
            }

            pasos.Add(new PasoGuiado {
                Accion = AccionGuiada.Entregar, Texto = "Ya está todo marcado. Pulsa «Entregar».",
                Porque = "Revisar bien es encontrar lo que falla sin acusar a lo que funciona."
            });
            return pasos;
        }

        private static string Nombre(MinijuegoDef def, string pieza) {
            var e = def.Artefacto.Elementos.FirstOrDefault(x => x.Id == pieza);
            if (e != null) return "«" + e.Texto + "»";
            var c = def.Artefacto.Conexiones.FirstOrDefault(x => x.Id == pieza);
            if (c != null) return "la flecha «" + c.Texto + "»";
            var k = def.Artefacto.Commits.FirstOrDefault(x => x.Id == pieza);
            if (k != null) return "el commit " + k.Id + " («" + k.Mensaje + "»)";
            return "«" + pieza + "»";
        }

        // ==================================================================== V3 · ordenar

        private static List<PasoGuiado> Ordenar(MinijuegoDef def) {
            var cfg = def.Ordenar;
            var porId = cfg.Tarjetas.ToDictionary(t => t.Id);
            var pasos = new List<PasoGuiado> {
                new PasoGuiado {
                    Accion = AccionGuiada.Leer, Texto = "Antes de mover nada: cómo se ordena un backlog.",
                    Porque = "Tres reglas, en este orden: 1) lo que otras tarjetas necesitan va ANTES que ellas; 2) entre lo que " +
                             "queda libre, primero lo que más vale para el esfuerzo que cuesta; 3) lo que no cabe sobre la línea " +
                             "(la capacidad del equipo) espera al siguiente sprint.",
                    EsLeccion = true
                }
            };

            var mejor = OrdenarEvaluador.MejorOrden(cfg);
            var necesitadaPor = cfg.Tarjetas.SelectMany(t => t.DependeDe.Select(d => new { d, t.Id }))
                                   .GroupBy(x => x.d).ToDictionary(g => g.Key, g => g.Count());
            for (var i = 0; i < mejor.Count; i++) {
                var t = porId[mejor[i]];
                string porque;
                if (!string.IsNullOrEmpty(t.Porque)) porque = t.Porque;
                else if (t.DependeDe.Count > 0)
                    porque = $"Necesita {string.Join(" y ", t.DependeDe.Select(d => "«" + porId[d].Titulo + "»"))}, que ya está arriba. " +
                             $"Vale {t.Valor} y cuesta {t.Esfuerzo}.";
                else porque = $"No necesita ninguna otra tarjeta y vale {t.Valor} con un esfuerzo de {t.Esfuerzo}.";
                int cuantas;
                if (necesitadaPor.TryGetValue(t.Id, out cuantas))
                    porque += $" Además, {cuantas} tarjeta(s) la necesitan: por eso va pronto.";
                pasos.Add(new PasoGuiado {
                    Accion = AccionGuiada.Mover, Objetivo = t.Id, Valor = i,
                    Texto = $"Pon «{t.Titulo}» en el puesto {i + 1}.", Porque = porque, EsLeccion = i == 0
                });
            }

            var fuera = cfg.Tarjetas.Where(t => !mejor.Contains(t.Id)).ToList();
            if (fuera.Count > 0)
                pasos.Add(new PasoGuiado {
                    Accion = AccionGuiada.Leer, Texto = "Mira la línea amarilla: lo de abajo se queda fuera.",
                    Porque = $"Con una capacidad de {cfg.Capacidad} ya no caben " +
                             string.Join(", ", fuera.Select(t => $"«{t.Titulo}» (valor {t.Valor})")) +
                             ". No se pierden: esperan al siguiente sprint. Priorizar es decidir qué se queda fuera.",
                    EsLeccion = true
                });

            pasos.Add(new PasoGuiado {
                Accion = AccionGuiada.Listo, Texto = "El orden está listo. Pulsa «Listo: enseñárselo al cliente».",
                Porque = "Primero se ordena con criterio; después se habla con el cliente sobre ESE orden."
            });

            Respuesta negociar;
            cfg.Respuestas.TryGetValue("negociar", out negociar);
            pasos.Add(new PasoGuiado {
                Accion = AccionGuiada.Responder, Objetivo = "negociar",
                Texto = $"Contesta: «{negociar?.Texto ?? "negociar"}».",
                Porque = "«Sí a todo» promete lo que no cabe y lo paga el equipo; «No» lo protege, pero el cliente no elige nada. " +
                         "Negociar es enseñarle lo que cuesta su petición y dejarle elegir qué sale.",
                EsLeccion = true
            });
            pasos.Add(new PasoGuiado {
                Accion = AccionGuiada.Entregar, Texto = "Pulsa «Entregar el orden y la respuesta».",
                Porque = "Un buen backlog se puede construir de arriba abajo sin bloquearse, y el cliente sabe qué se queda fuera y por qué."
            });
            return pasos;
        }

        // ==================================================================== V2 · repartir

        private static List<PasoGuiado> Repartir(MinijuegoDef def) {
            var cfg = def.Repartir;
            var pasos = new List<PasoGuiado> {
                new PasoGuiado {
                    Accion = AccionGuiada.Leer, Texto = "Antes de repartir: cómo se planifican las pruebas.",
                    Porque = "Las horas nunca alcanzan para todo. Cada tarjeta dice cuántos errores suele haber de ese tipo: " +
                             "multiplica el número MÁS ALTO por lo que cuesta atrapar uno, y eso es lo que cuesta ir sobre seguro. " +
                             "Cubre primero lo barato; a lo que renuncies, que sea el error más caro de atrapar.",
                    EsLeccion = true
                }
            };
            // El reparto de quien razona con lo que se ve (nada de numeros escondidos): es el que se puede repetir solo.
            var prudente = RepartirEvaluador.RepartoPrudente(cfg);

            foreach (var d in cfg.Depositos.OrderBy(x => x.CostePorDefecto)) {
                int h;
                prudente.TryGetValue(d.Id, out h);
                var maximo = RepartirEvaluador.MaximoEsperado(d);
                var cubre = h / Math.Max(1, d.CostePorDefecto);
                var seEspera = $"suele haber {RepartirEvaluador.RangoEsperado(d)}";
                if (h > 0)
                    pasos.Add(new PasoGuiado {
                        Accion = AccionGuiada.Asignar, Objetivo = d.Id, Valor = h,
                        Texto = $"Sube {d.Nombre} hasta {h} {cfg.Unidad}.",
                        Porque = $"Cada error de {d.Nombre.ToLowerInvariant()} cuesta {d.CostePorDefecto} {cfg.Unidad}, y {seEspera}: " +
                                 $"{h} {cfg.Unidad} alcanzan para atrapar {cubre}." +
                                 (cubre < maximo ? " No llega al máximo: es lo más caro de atrapar, y a algo hay que renunciar." : "")
                    });
                else
                    pasos.Add(new PasoGuiado {
                        Accion = AccionGuiada.Leer, Objetivo = d.Id,
                        Texto = $"Deja {d.Nombre} en 0.",
                        Porque = $"Es la más cara ({d.CostePorDefecto} {cfg.Unidad} por error) y {seEspera}: con lo que queda no " +
                                 "da. Dejar algo fuera A PROPÓSITO también es planificar.",
                        EsLeccion = true
                    });
            }
            pasos.Add(new PasoGuiado {
                Accion = AccionGuiada.Entregar, Texto = "Pulsa «Entregar el reparto».",
                Porque = "Un plan de pruebas no prueba todo: decide qué se prueba primero y qué se deja fuera, y por qué."
            });
            return pasos;
        }

        // ==================================================================== seguirlo (para los tests)

        /// <summary>Sigue el recorrido al pie de la letra desde el estado inicial de la escena y lo evalua.</summary>
        public static ResultadoMinijuego Aplicar(MinijuegoDef def, IList<PasoGuiado> pasos) {
            switch (Verbos.Normalizar(def.Verbo)) {
                case Verbos.Ordenar: {
                    var orden = def.Ordenar.Tarjetas.Select(t => t.Id).ToList();
                    string respuesta = null;
                    foreach (var p in pasos) {
                        if (p.Accion == AccionGuiada.Mover) { orden.Remove(p.Objetivo); orden.Insert(Math.Min(p.Valor, orden.Count), p.Objetivo); }
                        if (p.Accion == AccionGuiada.Responder) respuesta = p.Objetivo;
                    }
                    Respuesta r;
                    if (respuesta != null && def.Ordenar.Respuestas.TryGetValue(respuesta, out r))
                        orden = OrdenarEvaluador.AplicarRespuesta(def.Ordenar, orden, r);
                    return OrdenarEvaluador.Evaluar(def, orden, respuesta);
                }
                case Verbos.Repartir: {
                    var horas = def.Repartir.Depositos.ToDictionary(d => d.Id, d => 0);
                    foreach (var p in pasos.Where(x => x.Accion == AccionGuiada.Asignar)) horas[p.Objetivo] = p.Valor;
                    return RepartirEvaluador.Evaluar(def, horas);
                }
                default: {
                    var estado = new DetectarState(def.Presentacion.SegundosReloj);
                    var t = 0;
                    foreach (var p in pasos) {
                        if (p.Accion == AccionGuiada.Seleccionar && !estado.Seleccion.Contains(p.Objetivo)) estado.Alternar(p.Objetivo);
                        if (p.Accion == AccionGuiada.Etiquetar) estado.Marcar(p.Objetivo, t++);
                    }
                    return DetectarEvaluador.Evaluar(def, estado);
                }
            }
        }
    }

    /// <summary>Las etiquetas de defecto del verbo Detectar, en palabras para el jugador.</summary>
    public static class EtiquetasDeDefecto {
        public static readonly Dictionary<string, string> QueEs = new Dictionary<string, string> {
            { "requisito_ambiguo", "dice QUÉ hacer, pero no cómo decidir" }, { "requisito_incompleto", "se olvidó de un caso" },
            { "acoplamiento_indebido", "se mete en las tripas de otra pieza" }, { "falla_de_seguridad", "deja pasar a cualquiera" },
            { "dependencia_circular", "A necesita a B y B necesita a A" }, { "elemento_sin_especificar", "nadie dice qué hace ni cómo" },
            { "orden_incorrecto", "pasó en otro orden que el diseñado" }, { "mensaje_duplicado", "pasó dos veces" },
            { "mensaje_no_documentado", "pasó y no estaba en el diseño" }, { "participante_equivocado", "habló con quien no era" },
            { "historia_reescrita", "commits copiados y cambiados de sitio" }, { "autoria_perdida", "el trabajo cambió de dueño" },
            { "rama_huerfana", "un commit que no sale de nada" }, { "fusion_sin_revisar", "se unió sin que nadie lo revisara" },
        };

        public static string Humanizar(string etiqueta) {
            return string.IsNullOrEmpty(etiqueta) ? "" : etiqueta.Replace('_', ' ');
        }
    }
}
