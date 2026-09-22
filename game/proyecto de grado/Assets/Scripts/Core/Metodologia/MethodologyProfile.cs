using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Nexus.Core.Modelo;

namespace Nexus.Core.Metodologia {
    /// <summary>Vocabulario cerrado de tipos de calendario.</summary>
    public static class TiposDeCalendario {
        public const string Iterativo = "iterativo";     // Scrum, XP, Espiral
        public const string Secuencial = "secuencial";   // Cascada, RUP
        public const string Continuo = "continuo";       // Kanban
    }

    public static class FamiliasDeMetodologia {
        public const string Agil = "agil";
        public const string Tradicional = "tradicional";
    }

    /// <summary>Cuando aplica una ceremonia.</summary>
    public static class CuandoAplica {
        public const string Diario = "diario";
        public const string InicioIteracion = "inicioIteracion";
        public const string FinIteracion = "finIteracion";
        public const string FinEtapa = "finEtapa";
        public const string Cada = "cada";
        /// <summary>No lo dispara el calendario, lo dispara el flujo continuo de Kanban.</summary>
        public const string CuandoSeLiberaWip = "cuandoSeLiberaWip";
    }

    /// <summary>Cuando se admite un cambio de alcance.</summary>
    public static class VentanasDeCambio {
        public const string Siempre = "siempre";                   // Kanban
        public const string EntreIteraciones = "entreIteraciones"; // Scrum
        public const string Hito = "hito";                         // Cascada, RUP
    }

    public static class TablerosPrincipales {
        public const string Burndown = "burndown";
        public const string CurvaS = "curvaS";
        public const string Cfd = "cfd";
    }

    /// <summary>
    /// C5 · Una metodologia entera en datos (§4.5.1). Espejo en C# de metodologias/*.json.
    ///
    /// La regla de revision de este paquete: **ni un solo `if (id == "scrum")` en ningun sitio**.
    /// Todo lo que diferencia a Scrum de Cascada esta en estos ocho bloques, y por eso añadir XP,
    /// Espiral o RUP es escribir un JSON, no tocar el motor. Hay un test que lo comprueba metiendo
    /// una metodologia inventada.
    ///
    /// Los ocho bloques reescriben, en este orden: el calendario, las ceremonias, las reglas de cambio
    /// de alcance, los coeficientes del modelo, el director de eventos, el tablero que se enseña,
    /// el lanzamiento y la rubrica con la que se te juzga al cerrar.
    /// </summary>
    public sealed class MethodologyProfile {
        public string Id;
        public string Nombre;

        /// <summary>agil | tradicional. Decide si acertaste: agil encaja si volatilidadReal &gt;= 50.</summary>
        public string Familia = FamiliasDeMetodologia.Agil;

        public string Resumen;

        // --- verbo V4: elegir Y justificar ---
        public List<string> RazonesValidas = new List<string>();

        /// <summary>Razones que suenan bien y no lo son. Permiten acertar por el motivo equivocado, y detectarlo.</summary>
        public List<string> RazonesTrampa = new List<string>();

        /// <summary>id de razon -> como se lee en pantalla. Los ids no llevan tildes; los textos si.</summary>
        public Dictionary<string, string> TextosDeRazones = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>El texto de una razon; si no lo hay, el id legible.</summary>
        public string TextoDe(string razonId) {
            string texto;
            if (razonId != null && TextosDeRazones != null && TextosDeRazones.TryGetValue(razonId, out texto)) return texto;
            if (string.IsNullOrEmpty(razonId)) return "";
            var legible = razonId.Replace('_', ' ');
            return char.ToUpperInvariant(legible[0]) + legible.Substring(1) + ".";
        }

        // --- los ocho bloques ---
        public Calendario Calendario = new Calendario();                                   // 1
        public List<Ceremonia> Ceremonias = new List<Ceremonia>();                         // 2
        public ReglasDeCambio ReglasDeCambio = new ReglasDeCambio();                       // 3

        /// <summary>4 · Multiplica alpha, beta, gamma, delta, iota, kappa... y velocidadBase.</summary>
        public Dictionary<string, double> ModificadoresModelo =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public ModificadoresDirector ModificadoresDirector = new ModificadoresDirector();  // 5
        public string TableroPrincipal = TablerosPrincipales.Burndown;                     // 6
        public LanzamientoConfig Lanzamiento = new LanzamientoConfig();                    // 7
        public RubricaCierre RubricaCierre = new RubricaCierre();                          // 8

        public bool EsAgil {
            get { return string.Equals(Familia, FamiliasDeMetodologia.Agil, StringComparison.OrdinalIgnoreCase); }
        }

        public MethodologyProfile Clone() {
            var c = (MethodologyProfile)MemberwiseClone();
            c.RazonesValidas = RazonesValidas == null ? null : new List<string>(RazonesValidas);
            c.RazonesTrampa = RazonesTrampa == null ? null : new List<string>(RazonesTrampa);
            c.Calendario = Calendario == null ? null : Calendario.Clone();
            c.ModificadoresModelo = ModificadoresModelo == null
                ? null
                : new Dictionary<string, double>(ModificadoresModelo, StringComparer.OrdinalIgnoreCase);
            c.ModificadoresDirector = ModificadoresDirector == null ? null : ModificadoresDirector.Clone();
            c.ReglasDeCambio = ReglasDeCambio == null ? null : ReglasDeCambio.Clone();
            c.Lanzamiento = Lanzamiento == null ? null : Lanzamiento.Clone();
            c.RubricaCierre = RubricaCierre == null ? null : RubricaCierre.Clone();

            if (Ceremonias != null) {
                c.Ceremonias = new List<Ceremonia>(Ceremonias.Count);
                foreach (var ceremonia in Ceremonias) c.Ceremonias.Add(ceremonia == null ? null : ceremonia.Clone());
            }
            return c;
        }

        [OnDeserialized]
        internal void TrasCargar(StreamingContext _) {
            ModificadoresModelo = Diccionarios.SinMayusculas(ModificadoresModelo);
        }
    }

    // ================================================================= 1 · calendario

    /// <summary>
    /// Como se trocea el nivel. Los tres tipos no son cosmeticos: cambian cuando se abre la ventana
    /// de cambio de alcance, cuando caen las ceremonias y que tablero se enseña.
    /// </summary>
    public sealed class Calendario {
        public string Tipo = TiposDeCalendario.Iterativo;

        /// <summary>Como se llama una unidad en la pantalla: "Sprint", "Etapa", "Semana".</summary>
        public string EtiquetaUnidad = "Sprint";

        // --- iterativo ---
        public int LongitudIteracion = 10;
        public int Iteraciones = 2;

        // --- secuencial ---
        public List<Etapa> Etapas = new List<Etapa>();

        // --- continuo ---
        public int LimiteWipInicial = 4;

        public Calendario Clone() {
            var c = (Calendario)MemberwiseClone();
            if (Etapas != null) {
                c.Etapas = new List<Etapa>(Etapas.Count);
                foreach (var e in Etapas) c.Etapas.Add(e == null ? null : e.Clone());
            }
            return c;
        }
    }

    /// <summary>
    /// Una etapa de un calendario secuencial. Lo interesante es MultiplicadorPesosPorTag:
    /// en Cascada, durante "Analisis" pesan los eventos de alcance y durante "Pruebas" los de calidad.
    /// El enfoque del nivel cambia DENTRO del nivel, sin que nadie lo anuncie.
    /// </summary>
    public sealed class Etapa {
        public string Id;
        public string Nombre;
        public int Dias;

        public Dictionary<string, double> MultiplicadorPesosPorTag =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Se aplican cada dia de la etapa. Valores double o "+10%" / "-25%".</summary>
        public Dictionary<string, object> EfectosPorDia =
            new Dictionary<string, object>(StringComparer.Ordinal);

        public string Texto;

        public Etapa Clone() {
            var c = (Etapa)MemberwiseClone();
            c.MultiplicadorPesosPorTag = MultiplicadorPesosPorTag == null
                ? null
                : new Dictionary<string, double>(MultiplicadorPesosPorTag, StringComparer.OrdinalIgnoreCase);
            c.EfectosPorDia = EfectosPorDia == null
                ? null
                : new Dictionary<string, object>(EfectosPorDia, StringComparer.Ordinal);
            return c;
        }

        [OnDeserialized]
        internal void TrasCargar(StreamingContext _) {
            MultiplicadorPesosPorTag = Diccionarios.SinMayusculas(MultiplicadorPesosPorTag);
        }
    }

    // ================================================================= 2 · ceremonias

    public sealed class Ceremonia {
        public string Id;
        public string Nombre;

        /// <summary>Del vocabulario de CuandoAplica.</summary>
        public string Cuando = CuandoAplica.Diario;

        /// <summary>Solo para Cuando = "cada".</summary>
        public int CadaNDias = 1;

        /// <summary>Lo que cuesta en dias del proyecto. Una retro de medio dia es medio dia que no se programa.</summary>
        public double CosteDias;

        /// <summary>El verbo de minijuego que abre, si abre alguno (V4, V5...).</summary>
        public string Verbo;

        public string Texto;

        public Dictionary<string, object> Efectos = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>Si es true, Acciones no puede estar vacia: es la retrospectiva.</summary>
        public bool AjustaCoeficiente;

        public List<AccionRetro> Acciones = new List<AccionRetro>();

        public bool AbreVentanaDeCambio;
        public bool RevelaProductoAlCliente;
        public bool MuestraLeadTime;

        public Ceremonia Clone() {
            var c = (Ceremonia)MemberwiseClone();
            c.Efectos = Efectos == null ? null : new Dictionary<string, object>(Efectos, StringComparer.Ordinal);
            if (Acciones != null) {
                c.Acciones = new List<AccionRetro>(Acciones.Count);
                foreach (var a in Acciones) c.Acciones.Add(a == null ? null : a.Clone());
            }
            return c;
        }
    }

    /// <summary>
    /// Lo que el jugador elige en la retrospectiva. Es la UNICA mecanica del juego en la que el jugador
    /// modifica literalmente una constante de su propio proceso: {coeficiente:"kappa", multiplicador:0.85}
    /// se traduce en Coeficientes.MultiplicarUno("kappa", 0.85), y a partir de ese dia la documentacion
    /// se diluye mas despacio. Para siempre, y sin que la pantalla lo presuma.
    /// </summary>
    public sealed class AccionRetro {
        public string Id;
        public string Texto;

        /// <summary>alpha | beta | gamma | delta | sensibilidadMoral | theta | iota | kappa | lambda.</summary>
        public string Coeficiente;

        public double Multiplicador = 1.0;

        /// <summary>Lo que se le explica al jugador. Sin esto la accion es magia.</summary>
        public string Explicacion;

        public AccionRetro Clone() { return (AccionRetro)MemberwiseClone(); }
    }

    // ================================================================= 3 · cambios de alcance

    public sealed class ReglasDeCambio {
        /// <summary>Cuanto multiplica el coste de aceptar un cambio con esta metodologia.</summary>
        public double CosteMultiplicador = 1.0;

        /// <summary>Del vocabulario de VentanasDeCambio. Vacia = nunca hay ventana.</summary>
        public List<string> Ventanas = new List<string> { VentanasDeCambio.Siempre };

        public bool ConsumeWip;
        public string TextoEnVentana;

        public PenalizacionFueraDeVentana PenalizacionFueraDeVentana = new PenalizacionFueraDeVentana();

        public ReglasDeCambio Clone() {
            var c = (ReglasDeCambio)MemberwiseClone();
            c.Ventanas = Ventanas == null ? null : new List<string>(Ventanas);
            c.PenalizacionFueraDeVentana = PenalizacionFueraDeVentana == null
                ? null
                : PenalizacionFueraDeVentana.Clone();
            return c;
        }
    }

    /// <summary>
    /// Que pasa si el cliente cambia de idea a mitad de sprint. En Kanban no pasa nada; en Cascada,
    /// fuera de un hito, puede ser directamente imposible.
    /// </summary>
    public sealed class PenalizacionFueraDeVentana {
        public bool Permitido = true;
        public double CosteMultiplicador = 1.0;
        public Dictionary<string, object> EfectosExtra = new Dictionary<string, object>(StringComparer.Ordinal);
        public string Texto;

        public PenalizacionFueraDeVentana Clone() {
            var c = (PenalizacionFueraDeVentana)MemberwiseClone();
            c.EfectosExtra = EfectosExtra == null
                ? null
                : new Dictionary<string, object>(EfectosExtra, StringComparer.Ordinal);
            return c;
        }
    }

    // ================================================================= 5 · director

    public sealed class ModificadoresDirector {
        /// <summary>Se multiplica EN CASCADA sobre los pesosPorTag del nivel.</summary>
        public Dictionary<string, double> MultiplicadorPesosPorTag =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Escala el presupuesto de drama. Una metodologia puede hacer un nivel mas o menos convulso.</summary>
        public double MultiplicadorDrama = 1.0;

        /// <summary>Eventos que con esta metodologia no tienen sentido y no deben salir.</summary>
        public List<string> EventosBloqueados = new List<string>();

        public ModificadoresDirector Clone() {
            var c = (ModificadoresDirector)MemberwiseClone();
            c.MultiplicadorPesosPorTag = MultiplicadorPesosPorTag == null
                ? null
                : new Dictionary<string, double>(MultiplicadorPesosPorTag, StringComparer.OrdinalIgnoreCase);
            c.EventosBloqueados = EventosBloqueados == null ? null : new List<string>(EventosBloqueados);
            return c;
        }

        [OnDeserialized]
        internal void TrasCargar(StreamingContext _) {
            MultiplicadorPesosPorTag = Diccionarios.SinMayusculas(MultiplicadorPesosPorTag);
        }
    }

    // ================================================================= 7 · lanzamiento

    public sealed class LanzamientoConfig {
        /// <summary>Multiplica el riesgo latente. Entregar de golpe tras seis meses no es igual de arriesgado.</summary>
        public double FactorRiesgo = 1.0;

        /// <summary>Si se entrego por partes, escapan menos defectos.</summary>
        public bool EntregaIncremental;

        /// <summary>Si el cliente ya vio el producto, el rechazo por sorpresa deja de ser posible.</summary>
        public bool ClienteYaVioElProducto;

        public string Texto;
        public string TextoSiSale;
        public string TextoSiFalla;

        public LanzamientoConfig Clone() { return (LanzamientoConfig)MemberwiseClone(); }
    }

    // ================================================================= 8 · rubrica de cierre

    /// <summary>Sin esto no hay con que juzgar al jugador, y el validador lo exige.</summary>
    public sealed class RubricaCierre {
        public List<PracticaEsperada> Practicas = new List<PracticaEsperada>();

        public RubricaCierre Clone() {
            var c = new RubricaCierre();
            if (Practicas != null) {
                c.Practicas = new List<PracticaEsperada>(Practicas.Count);
                foreach (var p in Practicas) c.Practicas.Add(p == null ? null : p.Clone());
            }
            return c;
        }
    }

    /// <summary>
    /// Una practica de ESTA metodologia, contrastada contra una metrica real del nivel.
    /// Tiene un espejo deliberado en Nexus.Core.Evaluacion.PracticaAEvaluar: GameSession copia de aqui
    /// a alla al cerrar, y asi Evaluacion no necesita conocer este paquete.
    /// </summary>
    public sealed class PracticaEsperada {
        public string Id;
        public string Descripcion;

        /// <summary>Nombre de una de las 9 de MethodologyReport.Metricas.</summary>
        public string Metrica;

        /// <summary>&gt;= | &lt;= | &gt; | &lt; | ==</summary>
        public string Comparador;

        public double Objetivo;
        public string RazonSiCumple;
        public string RazonSiFalla;

        public PracticaEsperada Clone() { return (PracticaEsperada)MemberwiseClone(); }
    }
}
