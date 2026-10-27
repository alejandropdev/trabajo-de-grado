using System;
using System.Collections.Generic;
using Nexus.Core.Tablero;

namespace Nexus.Core.Fase1 {
    /// <summary>Lo que puede ser un objeto que se recoge en el mundo 3D.</summary>
    public static class TiposDeObjeto3D {
        /// <summary>Una hoja de vida: quien la recoge puede contar con esa persona en el equipo.</summary>
        public const string Candidato = "candidato";
        /// <summary>Pizza, cafe, Guepardex: se guardan y se gastan durante el desarrollo (Fase 2).</summary>
        public const string Suministro = "suministro";
        /// <summary>Una pieza del Diario de Campo (del catalogo de coleccionables).</summary>
        public const string Coleccionable = "coleccionable";
        /// <summary>Algo que cambia el proyecto en cuanto se trae (un certificado, por ejemplo).</summary>
        public const string Efecto = "efecto";
        /// <summary>Se enseña en el resumen y no hace nada mas: notas, tarjetas de acceso.</summary>
        public const string Informativo = "informativo";

        private static readonly string[] _todos = { Candidato, Suministro, Coleccionable, Efecto, Informativo };
        public static IReadOnlyList<string> Todos { get { return _todos; } }

        public static bool EsValido(string tipo) {
            foreach (var t in _todos) if (string.Equals(t, tipo, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>A quien se le da un suministro.</summary>
    public static class ObjetivosDeSuministro {
        public const string Equipo = "equipo";
        public const string Miembro = "miembro";
    }

    /// <summary>
    /// Como se gasta un suministro en la Fase 2. Los numeros viven en objetos-3d.json para poder ajustarlos sin tocar
    /// codigo. Lo que el equipo nota va por EffectApplier (INV-2); el bono de flujo y el cansancio de una persona van
    /// al tablero, como las acciones del panel «Equipo».
    /// </summary>
    public sealed class SuministroDef {
        public string Objetivo = ObjetivosDeSuministro.Equipo;
        /// <summary>Stocks del WorldState que cambia (MoralEquipo, Cansancio…).</summary>
        public Dictionary<string, double> Efectos = new Dictionary<string, double>(StringComparer.Ordinal);
        /// <summary>Lo que se suma al bono de flujo de hoy (0,15 = el equipo rinde hasta un 15 % mas hoy).</summary>
        public double BonoDeFlujo;
        /// <summary>Solo para un miembro: lo que cambia su desvio de cansancio (negativo = descansa).</summary>
        public double DesvioDeCansancio;
        /// <summary>Dias que hay que esperar entre un uso y el siguiente. 0 = sin espera.</summary>
        public int EnfriamientoDias;
        /// <summary>Usos por dia. 0 = sin limite.</summary>
        public int VecesPorDia;
        public int MinutosDeUso = 5;
        /// <summary>
        /// El abuso: si la misma persona ya lo tomo ReboteUsos veces en los ultimos ReboteVentanaDias dias, el
        /// siguiente hace lo contrario y su desvio de cansancio cambia en ReboteDesvio. 0 usos = no rebota nunca.
        /// </summary>
        public int ReboteUsos;
        public int ReboteVentanaDias;
        public double ReboteDesvio;

        public SuministroDef Clone() {
            var c = (SuministroDef)MemberwiseClone();
            c.Efectos = new Dictionary<string, double>(Efectos ?? new Dictionary<string, double>(), StringComparer.Ordinal);
            return c;
        }
    }

    /// <summary>
    /// Un objeto del mundo 3D, por el id de su InventoryItemDefinition («cv_marta», «consumable_pizza»…). El 3D solo
    /// cuenta cuantos recogio de cada uno; que significa cada uno para el motor se decide aqui.
    /// </summary>
    public sealed class Objeto3D {
        public string Id;
        public string Tipo;
        public string Nombre;
        /// <summary>Lo que hace, en una frase, para el resumen y el panel de suministros.</summary>
        public string Texto;
        /// <summary>Solo candidatos: la persona que se une al equipo.</summary>
        public MiembroDelEquipo Miembro;
        /// <summary>Candidatos y objetos de efecto: lo que cambia el proyecto al traerlo (una vez).</summary>
        public Dictionary<string, double> Efectos = new Dictionary<string, double>(StringComparer.Ordinal);
        /// <summary>Solo coleccionables: el id del coleccionable del Diario.</summary>
        public string Coleccionable;
        /// <summary>Solo suministros.</summary>
        public SuministroDef Suministro;
        /// <summary>No sale en el resumen (las tarjetas de acceso solo sirven dentro del 3D).</summary>
        public bool Oculto;

        public Objeto3D Clone() {
            var c = (Objeto3D)MemberwiseClone();
            c.Efectos = new Dictionary<string, double>(Efectos ?? new Dictionary<string, double>(), StringComparer.Ordinal);
            c.Miembro = Miembro == null ? null : CopiarMiembro(Miembro);
            c.Suministro = Suministro == null ? null : Suministro.Clone();
            return c;
        }

        /// <summary>Una persona nueva, sin nada de lo del dia: la del catalogo no se toca nunca.</summary>
        public static MiembroDelEquipo CopiarMiembro(MiembroDelEquipo m) {
            return new MiembroDelEquipo {
                Id = m.Id, Nombre = m.Nombre, Rol = m.Rol, Personaje = m.Personaje, Descripcion = m.Descripcion, Frase = m.Frase,
                Habilidades = new Dictionary<string, double>(m.Habilidades ?? new Dictionary<string, double>(), StringComparer.OrdinalIgnoreCase)
            };
        }
    }

    /// <summary>objetos-3d.json: que es para el juego cada objeto que se puede recoger en el recorrido 3D.</summary>
    public sealed class CatalogoDeObjetos3D {
        /// <summary>El id del unico miembro cuando no se recogio ninguna hoja de vida.</summary>
        public const string IdDeTuSolo = "tu";

        public List<Objeto3D> Objetos = new List<Objeto3D>();

        public Objeto3D Buscar(string id) {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var o in Objetos) if (o != null && string.Equals(o.Id, id, StringComparison.Ordinal)) return o;
            return null;
        }

        /// <summary>
        /// Quien trabaja cuando no se recogio ninguna hoja de vida: tu, solo, y despacio. El tablero necesita a alguien
        /// (reparte tarjetas y divide entre el numero de personas), y asi el precio de no buscar equipo se nota.
        /// </summary>
        public static MiembroDelEquipo TuSolo() {
            return new MiembroDelEquipo {
                Id = IdDeTuSolo, Nombre = "Tú", Rol = "Todo a la vez",
                Descripcion = "No encontraste ninguna hoja de vida en el recorrido: el proyecto lo sacas tú solo.",
                Frase = "Si no lo hago yo, no lo hace nadie.",
                Habilidades = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) {
                    { "interfaz", 0.7 }, { "datos", 0.7 }, { "logica", 0.7 }, { "pruebas", 0.7 }
                }
            };
        }
    }

    /// <summary>
    /// Convierte lo que el jugador lleva en el inventario del 3D en el ResultadoDeRecoleccion que entiende el motor.
    /// Puro: no sabe nada de Unity, y lo que no esta en el catalogo se queda fuera (y se dice en 'descartados').
    /// </summary>
    public static class TraductorDeRecoleccion3D {
        public static ResultadoDeRecoleccion Traducir(EntradaDeRecoleccion entrada, CatalogoDeObjetos3D catalogo,
                                                      IDictionary<string, int> objetos, double fraccionDeTiempoUsada,
                                                      bool terminoAntes, List<string> descartados = null) {
            if (entrada == null) throw new ArgumentNullException(nameof(entrada));
            var fraccion = double.IsNaN(fraccionDeTiempoUsada) ? 1.0 : Math.Max(0, Math.Min(1, fraccionDeTiempoUsada));
            var r = new ResultadoDeRecoleccion {
                NivelId = entrada.NivelId,
                MinutosUsados = (int)Math.Round(entrada.MinutosDisponibles * fraccion, MidpointRounding.AwayFromZero),
                AbandonoAntes = terminoAntes
            };
            if (objetos == null) return r;
            foreach (var kv in objetos) {
                if (kv.Value <= 0) continue;
                if (catalogo == null || catalogo.Buscar(kv.Key) == null) {
                    if (descartados != null) descartados.Add(kv.Key);
                    continue;
                }
                r.Objetos[kv.Key] = kv.Value;
            }
            return r;
        }
    }
}
