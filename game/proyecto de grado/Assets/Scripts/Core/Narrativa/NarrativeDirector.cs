using System;
using System.Collections.Generic;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;

namespace Nexus.Core.Narrativa {
    /// <summary>
    /// C7 · El canal narrativo (§4.7.3). Decide QUE escena toca hoy y EN QUE TONO.
    ///
    /// ★ La invariante del subsistema: el director solo puede **RETRASAR, ADELANTAR y COLOREAR**.
    /// No puede CREAR un beat que no este en el catalogo ni CANCELAR uno que si esta. Por eso el arbol
    /// de los catorce finales es auditable: la trama esta escrita en narrativa.json y el motor solo
    /// decide cuando suena cada pieza, nunca cuales son.
    ///
    /// ★ Determinista al 100 %: aqui no entra el azar. Dos partidas con las mismas decisiones cuentan
    /// la misma historia aunque tengan semillas distintas — el azar decide que crisis te toca, no quien eres.
    /// </summary>
    public sealed class NarrativeDirector {
        /// <summary>Prefijo con el que los beats emitidos viajan en RuntimeState.Ocurrencias.</summary>
        public const string PrefijoOcurrencia = "CIN:";

        private readonly List<NarrativeBeat> _catalogo = new List<NarrativeBeat>();
        private readonly Dictionary<string, NarrativeBeat> _porId =
            new Dictionary<string, NarrativeBeat>(StringComparer.Ordinal);

        public List<DecisionNarrativa> Log = new List<DecisionNarrativa>();

        public NarrativeDirector(List<NarrativeBeat> catalogo) {
            if (catalogo == null) return;

            foreach (var beat in catalogo) {
                if (beat == null || string.IsNullOrEmpty(beat.Id)) continue;
                if (_porId.ContainsKey(beat.Id))
                    throw new InvalidOperationException($"El catalogo narrativo tiene dos beats con id '{beat.Id}'.");
                _porId[beat.Id] = beat;
                _catalogo.Add(beat);   // se conserva el orden del JSON: es el desempate
            }
        }

        public IReadOnlyList<NarrativeBeat> Catalogo { get { return _catalogo; } }

        public NarrativeBeat PorId(string id) {
            NarrativeBeat beat;
            return id != null && _porId.TryGetValue(id, out beat) ? beat : null;
        }

        public bool YaSalio(NarrativeBeat beat, RuntimeState r) {
            int veces;
            return r.Ocurrencias.TryGetValue(PrefijoOcurrencia + beat.Id, out veces) && veces > 0;
        }

        /// <summary>
        /// La escena de hoy, o null. **Como maximo una al dia**: un beat es una cinematica de 60-90 s,
        /// y encadenar dos el mismo dia convertiria la narrativa en una interrupcion del juego en vez
        /// de en parte de el. Los que se quedan fuera siguen disponibles mañana.
        ///
        /// Orden de preferencia: primero los obligatorios, luego los opcionales; dentro de cada grupo,
        /// el de ventana mas temprana, y a igualdad el orden del catalogo. Todo determinista.
        /// </summary>
        public BeatDeHoy BeatDeHoy(RuntimeState r, IStateContext ctx) {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            NarrativeBeat elegido = null;
            var elegidoRetrasado = false;

            foreach (var obligatorio in new[] { true, false }) {
                foreach (var beat in Ordenados(obligatorio)) {
                    bool retrasado;
                    if (!PuedeSalirHoy(beat, r, ctx, out retrasado)) continue;
                    elegido = beat;
                    elegidoRetrasado = retrasado;
                    break;
                }
                if (elegido != null) break;
            }

            if (elegido == null) return null;

            r.Ocurrencias[PrefijoOcurrencia + elegido.Id] = 1;

            var variante = Colorear(elegido, ctx);
            Log.Add(new DecisionNarrativa {
                Dia = r.DiaActual, BeatId = elegido.Id, Resultado = DecisionNarrativa.Emitido,
                Detalle = variante + (elegidoRetrasado ? " (retrasado)" : "")
            });

            return new BeatDeHoy {
                BeatId = elegido.Id, Nombre = elegido.Nombre,
                Variante = variante, Retrasado = elegidoRetrasado
            };
        }

        /// <summary>
        /// Los obligatorios que nunca llegaron a salir. Se llama al cerrar el nivel: no cambia nada,
        /// pero deja constancia de que la trama tenia un agujero. Un beat obligatorio inalcanzable es
        /// un bug de contenido, y sin esto no daria la cara nunca.
        /// </summary>
        public List<string> ObligatoriosQueNoSalieron(RuntimeState r) {
            var perdidos = new List<string>();

            foreach (var beat in _catalogo) {
                if (!EsObligatorio(beat) || YaSalio(beat, r)) continue;
                perdidos.Add(beat.Id);
                Log.Add(new DecisionNarrativa {
                    Dia = r.DiaActual, BeatId = beat.Id, Resultado = DecisionNarrativa.Perdido,
                    Detalle = "obligatorio que nunca cumplio sus precondiciones"
                });
            }

            return perdidos;
        }

        // ------------------------------------------------------------------ interno

        private IEnumerable<NarrativeBeat> Ordenados(bool obligatorios) {
            var lista = new List<NarrativeBeat>();
            foreach (var beat in _catalogo)
                if (EsObligatorio(beat) == obligatorios) lista.Add(beat);

            // Orden estable: por ventana y, a igualdad, por posicion en el catalogo.
            var indice = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < _catalogo.Count; i++) indice[_catalogo[i].Id] = i;

            lista.Sort((a, b) => {
                var porVentana = DiaMin(a).CompareTo(DiaMin(b));
                return porVentana != 0 ? porVentana : indice[a.Id].CompareTo(indice[b.Id]);
            });
            return lista;
        }

        private static bool EsObligatorio(NarrativeBeat beat) {
            return string.Equals(beat.Prioridad, PrioridadDeBeat.Obligatorio, StringComparison.OrdinalIgnoreCase);
        }

        private static int DiaMin(NarrativeBeat beat) {
            return beat.Ventana == null ? 1 : beat.Ventana.DiaMin;
        }

        /// <summary>
        /// La ventana y las precondiciones no significan lo mismo, y de ahi sale la semantica de
        /// 'obligatorio' que la especificacion dejaba abierta (§4.7.3, pendiente b):
        ///
        /// · La VENTANA es ritmo. Para un obligatorio es una preferencia: si el dia llega y las
        ///   precondiciones aun no se cumplen, el beat espera. Mejor tarde que no contar la escena.
        /// · Las PRECONDICIONES son coherencia. No se saltan jamas, ni para un obligatorio: disparar
        ///   "La Cafetera" a quien nunca piso la Zona C no seria narrativa, seria un fallo.
        /// </summary>
        private bool PuedeSalirHoy(NarrativeBeat beat, RuntimeState r, IStateContext ctx, out bool retrasado) {
            retrasado = false;
            if (YaSalio(beat, r)) return false;

            var ventana = beat.Ventana ?? new VentanaDeBeat();
            if (r.DiaActual < ventana.DiaMin) return false;

            if (r.DiaActual > ventana.DiaMax) {
                if (!EsObligatorio(beat)) {
                    Anotar(r, beat, DecisionNarrativa.FueraDeVentana, "se paso su ventana y era opcional");
                    return false;
                }
                retrasado = true;
            }

            if (ConditionEvaluator.EvaluarTodas(beat.Precondiciones, ctx)) return true;

            Anotar(r, beat, DecisionNarrativa.Precondiciones, "todavia no se cumplen");
            return false;
        }

        /// <summary>
        /// La primera expresion cierta manda; si ninguna lo es, la del "default". Nunca devuelve null:
        /// una escena sin variante no se podria pintar.
        /// </summary>
        private static string Colorear(NarrativeBeat beat, IStateContext ctx) {
            if (beat.Coloreo == null || beat.Coloreo.Count == 0) return NarrativeBeat.VarianteDefecto;

            foreach (var kv in beat.Coloreo) {
                if (string.Equals(kv.Key, NarrativeBeat.VarianteDefecto, StringComparison.OrdinalIgnoreCase)) continue;
                if (ConditionEvaluator.Evaluar(kv.Key, ctx)) return kv.Value;
            }

            string porDefecto;
            return beat.Coloreo.TryGetValue(NarrativeBeat.VarianteDefecto, out porDefecto)
                ? porDefecto
                : NarrativeBeat.VarianteDefecto;
        }

        /// <summary>Solo se anota una vez por beat y dia, para que el log no se convierta en ruido.</summary>
        private void Anotar(RuntimeState r, NarrativeBeat beat, string resultado, string detalle) {
            if (Log.Count > 0) {
                var ultima = Log[Log.Count - 1];
                if (ultima.Dia == r.DiaActual && ultima.BeatId == beat.Id && ultima.Resultado == resultado) return;
            }
            Log.Add(new DecisionNarrativa { Dia = r.DiaActual, BeatId = beat.Id, Resultado = resultado, Detalle = detalle });
        }
    }

    /// <summary>
    /// C7 · El puente entre los dos canales (§4.9.4). **INV-6: ocurre UNA vez, dentro de Cerrar(), y
    /// en ningun otro sitio.**
    ///
    /// Es el momento en que el proyecto se convierte en biografia: los stocks del nivel, que se van a
    /// tirar, dejan su huella en los flags de la partida, que no se tiran nunca.
    ///
    /// Vive aqui y no en GameSession para que la regla de que es snapshot y que es contador se lea de
    /// un vistazo, en el paquete que manda sobre los flags.
    /// </summary>
    public static class PuenteDeFlags {
        public const string DeudaTecnica = "FLG_DEUDA_TECNICA";
        public const string MoralEquipo = "FLG_MORAL_EQUIPO";
        public const string HorasExtra = "FLG_HORAS_EXTRA";
        public const string Salud = "FLG_SALUD";
        public const string VidaExterna = "FLG_VIDA_EXTERNA";
        public const string CalidadAcum = "FLG_CALIDAD_ACUM";
        public const string Reputacion = "FLG_REPUTACION";

        /// <summary>
        /// Cinco snapshots y dos contadores.
        ///
        /// La especificacion escribe los siete con '=', pero dos de ellos son contadores acumulativos:
        /// el Dashboard del §10.3 enseña "horas extra acumuladas: 412", y eso son ocho niveles sumando,
        /// no el ultimo sobreescribiendo a los siete anteriores. Por eso esos dos usan Sumar().
        /// </summary>
        public static void VolcarAlCerrar(WorldState w, RuntimeState r, FlagStore flags) {
            if (w == null) throw new ArgumentNullException(nameof(w));
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (flags == null) throw new ArgumentNullException(nameof(flags));

            // --- snapshots: como termino ESTE nivel ---
            flags.Set(DeudaTecnica, Math.Round(w.DeudaTecnica));
            flags.Set(MoralEquipo, Math.Round(w.MoralEquipo));
            flags.Set(Salud, Math.Round(w.SaludJugador));
            flags.Set(CalidadAcum, Math.Round((w.Cobertura + w.Documentacion) / 2.0));
            flags.Set(Reputacion, Math.Round(w.Reputacion));

            // --- contadores: lo que llevas acumulado de toda la partida ---
            flags.Sumar(HorasExtra, r.DiasConHorasExtra);
            flags.Sumar(VidaExterna, r.VecesQueSeFueACasa * 5);
        }
    }
}
