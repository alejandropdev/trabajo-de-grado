using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;

namespace Nexus.Core.Relaciones {
    /// <summary>
    /// Alguien con quien se puede hablar en la Fase 2. La confianza que le ganes en el nivel desbloquea ayudas
    /// concretas (TiposDeAyuda), una vez cada una, al llegar a su umbral.
    /// </summary>
    public sealed class Personaje {
        public string Id;
        public string Nombre;
        /// <summary>«QA del concurso», «técnico del equipo»… Lo que se ve debajo del nombre.</summary>
        public string Rol;
        public List<AyudaDePersonaje> Ayudas = new List<AyudaDePersonaje>();
        /// <summary>Flag del censo al que se suma la confianza al cerrar el nivel (INV-6). Vacio = no deja huella.</summary>
        public string FlagAlCerrar;
        /// <summary>Cuantos puntos de confianza valen un punto del flag.</summary>
        public int PuntosPorFlag = 3;
        /// <summary>Sigue en los niveles siguientes: su confianza se hereda, y sus ayudas piden mas (7 o mas).</summary>
        public bool Persistente;
        /// <summary>Lo que comenta en una charla corta segun como va el proyecto (cuando no hay pregunta para hoy).</summary>
        public List<CharlaDelEstado> Charlas = new List<CharlaDelEstado>();
    }

    public sealed class AyudaDePersonaje {
        public int Umbral;
        public string Tipo;
        public int Usos = 1;
        /// <summary>Lo que dice el personaje al ofrecerla: «Si te toca probar algo, dime y te digo dónde mirar».</summary>
        public string Texto;
        /// <summary>Solo para bajar-cansancio: cuanto baja.</summary>
        public double Valor = 10;
    }

    /// <summary>
    /// Una conversacion de pasillo: unas lineas y, casi siempre, una pregunta de la asignatura en tono de charla.
    /// Contestar bien (lo correcto Y con buen trato) da confianza; contestar mal la quita un poco.
    /// </summary>
    public sealed class Conversacion {
        public string Id;
        public string Nivel;
        public string Personaje;
        public string Zona;
        public int DiaDesde = 1;
        public int DiaHasta = 999;
        public int Orden;
        public int Minutos = 20;
        /// <summary>Tema de la asignatura que toca (para el docente): «git», «estimacion», «testing»…</summary>
        public string Tema;
        public List<LineaDeConversacion> Lineas = new List<LineaDeConversacion>();
        public PreguntaDeConversacion Pregunta;
    }

    public sealed class LineaDeConversacion {
        public string Quien;
        public string Texto;
    }

    public sealed class PreguntaDeConversacion {
        public string Texto;
        public List<OpcionDeConversacion> Opciones = new List<OpcionDeConversacion>();
    }

    public sealed class OpcionDeConversacion {
        public string Id;
        public string Texto;
        public int Confianza;
        /// <summary>Lo que contesta el personaje, que es tambien la explicacion: por que eso estaba bien o mal.</summary>
        public string Respuesta;
    }

    /// <summary>
    /// Lo que un personaje comenta de «su» barra del proyecto en una charla corta: Javier de la cobertura, Óscar de la
    /// deuda… El texto depende de como este la barra (tramos) y lleva sus numeros reales: {valor}, {errores}.
    /// Es otra forma de que el jugador vea lo que cada estadistica provoca.
    /// </summary>
    public sealed class CharlaDelEstado {
        public string Estadistica;
        public List<TramoDeCharla> Tramos = new List<TramoDeCharla>();

        /// <summary>El texto del primer tramo cuyo 'Hasta' alcanza el valor actual, con los numeros puestos.</summary>
        public string Texto(WorldState w) {
            double valor;
            if (w == null || !w.TryGet(Estadistica, out valor)) return null;
            var tramo = Tramos.Where(t => t != null).OrderBy(t => t.Hasta).FirstOrDefault(t => valor <= t.Hasta)
                        ?? Tramos.Where(t => t != null).OrderBy(t => t.Hasta).LastOrDefault();
            if (tramo == null) return null;
            var errores = Nexus.Core.Evaluacion.PronosticoDeLanzamiento.Defectos(w.Cobertura, w.DeudaTecnica, true);
            return (tramo.Texto ?? "").Replace("{valor}", Math.Round(valor).ToString()).Replace("{errores}", errores.ToString());
        }
    }

    public sealed class TramoDeCharla {
        /// <summary>El tramo vale para valores hasta este (incluido). El ultimo tramo deberia llegar a 100.</summary>
        public double Hasta;
        public string Texto;
    }

    /// <summary>Lo que salio de hablar: cuanta confianza cambio, que contesto, y que ayudas se acaban de ganar.</summary>
    public sealed class ResultadoDeConversacion {
        public string ConversacionId;
        public string Personaje;
        public int CambioDeConfianza;
        public int ConfianzaTotal;
        public string Respuesta;
        public List<AyudaDePersonaje> AyudasNuevas = new List<AyudaDePersonaje>();
    }

    /// <summary>El archivo narrativa/conversaciones.json entero.</summary>
    public sealed class CatalogoDeRelaciones {
        public List<Personaje> Personajes = new List<Personaje>();
        public List<Conversacion> Conversaciones = new List<Conversacion>();

        public Personaje PersonajePorId(string id) { return Personajes.FirstOrDefault(p => p != null && p.Id == id); }
    }

    /// <summary>
    /// Las reglas de las relaciones, puras: reciben el estado del nivel (R) y el catalogo, y no saben de reloj ni
    /// de Unity. GameSession las llama y cobra los minutos.
    ///   · Una conversacion por personaje y dia, y cada conversacion una sola vez.
    ///   · Se habla en la zona de la conversacion, dentro de su ventana de dias, en orden.
    ///   · La confianza no baja de 0. Cada ayuda se concede una vez, al cruzar su umbral.
    /// </summary>
    public static class MotorDeRelaciones {
        /// <param name="w">El estado del proyecto. Si se da, quien no tiene pregunta para hoy ofrece una charla del estado.</param>
        public static List<Conversacion> Disponibles(CatalogoDeRelaciones c, RuntimeState r, string nivelId, string zona,
                                                     WorldState w = null) {
            var lista = new List<Conversacion>();
            if (c == null || r == null || string.IsNullOrEmpty(zona)) return lista;
            var delNivel = c.Conversaciones.Where(x => x != null && x.Nivel == nivelId).ToList();
            var porPersonaje = delNivel
                .Where(x => string.Equals(x.Zona, zona, StringComparison.OrdinalIgnoreCase))
                .Where(x => !r.ConversacionesHechas.Contains(x.Id))
                .Where(x => r.DiaActual >= x.DiaDesde && r.DiaActual <= x.DiaHasta)
                .GroupBy(x => x.Personaje);
            var conPregunta = new HashSet<string>(StringComparer.Ordinal);
            foreach (var g in porPersonaje) {
                if (HabloHoy(r, g.Key)) { conPregunta.Add(g.Key); continue; }
                lista.Add(g.OrderBy(x => x.Orden).ThenBy(x => x.Id, StringComparer.Ordinal).First());
                conPregunta.Add(g.Key);
            }
            if (w == null) return lista;

            // Charlas del estado: cada personaje de esta zona (su zona es la de su primera conversacion del nivel)
            // que hoy no tiene pregunta, comenta como va «su» barra. Asi casi cada dia hay con quien hablar.
            foreach (var p in c.Personajes.Where(x => x != null && x.Charlas.Count > 0 && !conPregunta.Contains(x.Id))) {
                var suyas = delNivel.Where(x => x.Personaje == p.Id).OrderBy(x => x.DiaDesde).ThenBy(x => x.Orden).ToList();
                if (suyas.Count == 0) continue;
                if (!string.Equals(suyas[0].Zona, zona, StringComparison.OrdinalIgnoreCase)) continue;
                if (r.DiaActual < suyas.Min(x => x.DiaDesde) || HabloHoy(r, p.Id)) continue;
                var charla = p.Charlas[Math.Abs(r.DiaActual) % p.Charlas.Count];
                var texto = charla.Texto(w);
                if (string.IsNullOrEmpty(texto)) continue;
                lista.Add(new Conversacion {
                    Id = IdDeCharla(p.Id, r.DiaActual), Nivel = nivelId, Personaje = p.Id, Zona = zona,
                    DiaDesde = r.DiaActual, DiaHasta = r.DiaActual, Minutos = 10, Tema = "estado",
                    Lineas = new List<LineaDeConversacion> { new LineaDeConversacion { Quien = p.Nombre, Texto = texto } }
                });
            }
            return lista;
        }

        public static string IdDeCharla(string personaje, int dia) { return "CHARLA-" + personaje + "-D" + dia; }

        private static bool HabloHoy(RuntimeState r, string personaje) {
            int ultimo;
            return r.UltimoDiaHablado.TryGetValue(personaje ?? "", out ultimo) && ultimo == r.DiaActual;
        }

        public static int ConfianzaDe(RuntimeState r, string personaje) {
            int v;
            return r != null && personaje != null && r.Confianza.TryGetValue(personaje, out v) ? v : 0;
        }

        /// <summary>Aplica la respuesta elegida. Lanza si la conversacion no esta disponible ahora mismo.</summary>
        public static ResultadoDeConversacion Hablar(CatalogoDeRelaciones c, RuntimeState r, string nivelId, string zona,
                                                    string conversacionId, string opcionId, WorldState w = null) {
            var conv = Disponibles(c, r, nivelId, zona, w).FirstOrDefault(x => x.Id == conversacionId);
            if (conv == null) throw new InvalidOperationException($"La conversacion '{conversacionId}' no esta disponible aqui y ahora.");

            OpcionDeConversacion opcion = null;
            if (conv.Pregunta != null && conv.Pregunta.Opciones.Count > 0) {
                opcion = conv.Pregunta.Opciones.FirstOrDefault(o => o.Id == opcionId);
                if (opcion == null) throw new InvalidOperationException($"'{opcionId}' no es una respuesta de '{conversacionId}'.");
            }

            var antes = ConfianzaDe(r, conv.Personaje);
            var cambio = opcion == null ? 1 : opcion.Confianza;   // charlar sin pregunta tambien acerca un poco
            var despues = Math.Max(0, antes + cambio);
            r.Confianza[conv.Personaje] = despues;
            r.ConversacionesHechas.Add(conv.Id);
            r.UltimoDiaHablado[conv.Personaje] = r.DiaActual;

            var res = new ResultadoDeConversacion {
                ConversacionId = conv.Id, Personaje = conv.Personaje,
                CambioDeConfianza = despues - antes, ConfianzaTotal = despues,
                Respuesta = opcion == null ? null : opcion.Respuesta
            };

            var personaje = c.PersonajePorId(conv.Personaje);
            if (personaje != null)
                foreach (var ayuda in personaje.Ayudas.Where(a => a != null && despues >= a.Umbral)) {
                    var clave = personaje.Id + "|" + ayuda.Umbral;
                    if (r.AyudasConcedidas.Contains(clave)) continue;
                    r.AyudasConcedidas.Add(clave);
                    int usos;
                    r.AyudasDisponibles.TryGetValue(ayuda.Tipo, out usos);
                    r.AyudasDisponibles[ayuda.Tipo] = usos + Math.Max(1, ayuda.Usos);
                    res.AyudasNuevas.Add(ayuda);
                }
            return res;
        }

        /// <summary>
        /// Al empezar un nivel: la confianza de los personajes que siguen viene del nivel anterior, y las ayudas
        /// cuyo umbral ya se alcanzo se vuelven a conceder para este nivel. Lo ganado antes, se nota despues.
        /// </summary>
        public static void Heredar(CatalogoDeRelaciones c, RuntimeState r, IDictionary<string, int> confianza) {
            if (c == null || r == null || confianza == null) return;
            foreach (var kv in confianza) {
                var p = c.PersonajePorId(kv.Key);
                if (p == null || !p.Persistente) continue;
                r.Confianza[p.Id] = Math.Max(0, kv.Value);
                foreach (var ayuda in p.Ayudas.Where(a => a != null && kv.Value >= a.Umbral)) {
                    var clave = p.Id + "|" + ayuda.Umbral;
                    if (r.AyudasConcedidas.Contains(clave)) continue;
                    r.AyudasConcedidas.Add(clave);
                    int usos;
                    r.AyudasDisponibles.TryGetValue(ayuda.Tipo, out usos);
                    r.AyudasDisponibles[ayuda.Tipo] = usos + Math.Max(1, ayuda.Usos);
                }
            }
        }

        /// <summary>La confianza de los personajes que siguen, para guardarla entre niveles.</summary>
        public static Dictionary<string, int> Persistentes(CatalogoDeRelaciones c, RuntimeState r) {
            var d = new Dictionary<string, int>(StringComparer.Ordinal);
            if (c == null || r == null) return d;
            foreach (var kv in r.Confianza) {
                var p = c.PersonajePorId(kv.Key);
                if (p != null && p.Persistente) d[kv.Key] = kv.Value;
            }
            return d;
        }

        public static int UsosDe(RuntimeState r, string tipo) {
            int v;
            return r != null && tipo != null && r.AyudasDisponibles.TryGetValue(tipo, out v) ? v : 0;
        }

        /// <summary>Gasta un uso de una ayuda. false si no quedaba.</summary>
        public static bool Gastar(RuntimeState r, string tipo) {
            var usos = UsosDe(r, tipo);
            if (usos <= 0) return false;
            r.AyudasDisponibles[tipo] = usos - 1;
            return true;
        }

        /// <summary>Lo que la confianza deja en los flags al cerrar el nivel: flag -> cuanto sumar.</summary>
        public static Dictionary<string, double> FlagsAlCerrar(CatalogoDeRelaciones c, RuntimeState r) {
            var flags = new Dictionary<string, double>(StringComparer.Ordinal);
            if (c == null || r == null) return flags;
            foreach (var kv in r.Confianza) {
                var p = c.PersonajePorId(kv.Key);
                if (p == null || string.IsNullOrEmpty(p.FlagAlCerrar) || kv.Value <= 0) continue;
                var suma = kv.Value / Math.Max(1, p.PuntosPorFlag);
                if (suma <= 0) continue;
                double previo;
                flags.TryGetValue(p.FlagAlCerrar, out previo);
                flags[p.FlagAlCerrar] = previo + suma;
            }
            return flags;
        }
    }
}
