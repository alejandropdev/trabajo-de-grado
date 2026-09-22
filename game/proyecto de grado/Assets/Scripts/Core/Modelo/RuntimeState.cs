using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Modelo {
    /// <summary>
    /// C1 · El estado de EJECUCION del nivel (§4.3.3): reloj, director, comportamiento del jugador,
    /// registro pedagogico y las series de los tableros. Se descarta al cerrar el nivel.
    ///
    /// Diferencia con WorldState: WorldState es "como va el proyecto", RuntimeState es "como vas tu jugandolo".
    /// El modelo de Forrester solo lee tres contadores de aqui, y los lee a traves de IContadoresDeSimulacion
    /// (implementado de forma EXPLICITA mas abajo) para que el contrato "ForresterModel nunca escribe
    /// contadores" sea literal y no solo un comentario.
    /// </summary>
    public sealed class RuntimeState : IContadoresDeSimulacion {
        /// <summary>Los cinco artefactos vivos del §A5. El SRS con completitud &lt; 60 duplica el peso del tag 'alcance'.</summary>
        public static readonly string[] ArtefactosDelProyecto = { "SRS", "SAD", "SDD", "PTP", "PMP" };

        // --- Reloj y progresion ---
        public int DiaActual;
        public int SprintActual;

        /// <summary>
        /// Minutos desde medianoche. 480 = 08:00. Es el reloj del dia continuo (§3.3) y viaja en el
        /// guardado: sin el, recargar a mitad de jornada devolveria al jugador al principio del dia.
        /// </summary>
        public int MinutoDelDia;

        /// <summary>Donde esta el jugador ahora. Vacio = en su escritorio, que es siempre el ancla.</summary>
        public string ZonaActual;

        /// <summary>True si eligio quedarse en el cierre y el dia sigue hasta la hora limite.</summary>
        public bool JornadaProrrogada;

        /// <summary>1 = planificacion · 2 = desarrollo · 3 = lanzamiento · 4 = evaluacion.</summary>
        public int Fase = 1;

        // --- Estado del director ---
        // La cola de diferidos y los telegrafiados pendientes NO viven aqui: los posee EffectScheduler (C4, §4.4.4)
        // y viajan en NivelEnCurso por separado. Duplicarlos seria tener dos fuentes de verdad de la agenda.

        /// <summary>Presupuesto de drama restante, una casilla por fase. Se copia del perfil, no se comparte.</summary>
        public int[] DramaRestante = { 0, 0, 0, 0 };

        /// <summary>id de evento o minijuego -> ultimo dia en que ocurrio. Lo lee diasDesde('EV-XXX').</summary>
        public Dictionary<string, int> Enfriamientos = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>id -> cuantas veces ha salido. Lo lee ocurrencias('EV-XXX') y lo limita maxOcurrencias.</summary>
        public Dictionary<string, int> Ocurrencias = new Dictionary<string, int>(StringComparer.Ordinal);

        // --- Comportamiento del jugador ---
        /// <summary>Zona A..F -> veces visitada en la Fase 1.</summary>
        public Dictionary<string, int> ZonasVisitadas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Tres seguidos disparan el evento "Manos temblando". Lo usa la penalizacion no lineal de salud.</summary>
        public int DiasSeguidosTrabajando;

        /// <summary>Cada vez abre el interludio de esa noche. Al cerrar el nivel se vuelca a FLG_VIDA_EXTERNA.</summary>
        public int VecesQueSeFueACasa;

        public int WipActual;
        public int VisitasZonaC;
        public int InteraccionesDerek;

        /// <summary>
        /// guion -> opcion elegida al final de una escena (Marta, la pared dorada, el log de build…).
        /// No escribe ningun flag al momento: INV-6 solo deja hacerlo dentro de Cerrar(), que es quien
        /// lee esto. Viaja en el guardado para que recargar no borre lo que el jugador ya decidio.
        /// </summary>
        public Dictionary<string, string> EleccionesNarrativas = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Los coleccionables recogidos en este nivel. Al cerrar se cuentan en FLG_CARTAS y FLG_CODIGOS.</summary>
        public List<string> ColeccionablesRecogidos = new List<string>();

        // --- Registro pedagogico ---
        /// <summary>Estimado vs real: alimenta el cono de incertidumbre y el sesgo de optimismo del verbo V5.</summary>
        public List<Estimacion> HistorialEstimaciones = new List<Estimacion>();

        /// <summary>Los ADRs escritos. Con 8 o mas se abre la Prueba G del juicio del N8.</summary>
        public List<string> AdrsEscritos = new List<string>();

        // --- Artefactos y defectos ---
        public Dictionary<string, EstadoArtefacto> Artefactos =
            new Dictionary<string, EstadoArtefacto>(StringComparer.OrdinalIgnoreCase);

        /// <summary>logica_local, integracion, flujo, reincidencia. NO es fija: depende de lo que hiciste antes.</summary>
        public Dictionary<string, int> PoblacionDefectos = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // --- Auxiliares de metodologia ---
        public int DiasConHorasExtra;
        public int CambiosAceptados;
        public int CambiosNegociados;
        public int CambiosRechazados;
        public double CompromisoActual;

        /// <summary>Puntos comprometidos por encima de la velocidad empirica. &gt; 0 genera deuda extra cada dia.</summary>
        public double SobreCompromiso;

        public List<double> CompromisosPorIteracion = new List<double>();
        public List<double> EntregadoPorIteracion = new List<double>();
        public int AccionesRetroElegidas;

        /// <summary>-1 mientras no se haya cerrado el diseño. Se compara contra la practica esperada de la metodologia.</summary>
        public double CoberturaAlCerrarDiseno = -1;

        public int LimiteWip;
        public int VecesExcedioWip;
        public int MinijuegosJugados;

        // --- Series para los tableros (burndown, curva S, CFD) ---
        public List<double> SerieAvance = new List<double>();
        public List<double> SerieAlcance = new List<double>();
        public List<double> SerieRiesgo = new List<double>();
        public List<double> SerieWip = new List<double>();
        public List<double> SerieLeadTime = new List<double>();

        private static readonly string[] _consultables = {
            "diaActual", "fase", "wipActual", "limiteWip", "sobreCompromiso",
            "diasConHorasExtra", "diasSeguidosTrabajando", "cambiosAceptados",
            "vecesQueSeFueACasa", "accionesRetroElegidas",
            // del dia continuo: permite precondiciones del tipo "minutoDelDia / 60 >= 15"
            "minutoDelDia"
        };

        /// <summary>Lo unico que una precondicion de un JSON puede consultar de aqui. El resto es interno a proposito.</summary>
        public static IReadOnlyList<string> Consultables { get { return _consultables; } }

        /// <summary>
        /// Estado de ejecucion al empezar el nivel. El presupuesto de drama se COPIA: si se compartiera el array
        /// con el LevelProfile, jugar el mismo nivel dos veces en la misma sesion empezaria con el drama ya gastado.
        /// </summary>
        public static RuntimeState DesdeNivel(LevelProfile perfil) {
            if (perfil == null) throw new ArgumentNullException(nameof(perfil));

            var r = new RuntimeState();
            r.Fase = 1;

            var drama = perfil.Director == null ? null : perfil.Director.PresupuestoDrama;
            r.DramaRestante = drama == null ? new[] { 0, 0, 0, 0 } : (int[])drama.Clone();

            foreach (var artefacto in ArtefactosDelProyecto)
                r.Artefactos[artefacto] = new EstadoArtefacto();

            return r;
        }

        /// <summary>
        /// Expone un SUBCONJUNTO a proposito (§4.3.3): no todo lo interno debe poder consultarse desde el JSON
        /// de un evento. Abrir esto entero convertiria cualquier detalle de implementacion en contrato de contenido.
        /// </summary>
        public bool TryGet(string nombre, out double valor) {
            switch (nombre == null ? "" : nombre.Trim().ToLowerInvariant()) {
                case "diaactual": valor = DiaActual; return true;
                case "fase": valor = Fase; return true;
                case "wipactual": valor = WipActual; return true;
                case "limitewip": valor = LimiteWip; return true;
                case "sobrecompromiso": valor = SobreCompromiso; return true;
                case "diasconhorasextra": valor = DiasConHorasExtra; return true;
                case "diasseguidostrabajando": valor = DiasSeguidosTrabajando; return true;
                case "cambiosaceptados": valor = CambiosAceptados; return true;
                case "vecesquesefueacasa": valor = VecesQueSeFueACasa; return true;
                case "accionesretroelegidas": valor = AccionesRetroElegidas; return true;
                case "minutodeldia": valor = MinutoDelDia; return true;
                default: valor = 0; return false;
            }
        }

        /// <summary>Drama que queda en la fase actual. Fuera de las 4 fases devuelve 0: no se agenda nada.</summary>
        public int DramaDeLaFase() {
            var i = Fase - 1;
            return i >= 0 && DramaRestante != null && i < DramaRestante.Length ? DramaRestante[i] : 0;
        }

        [OnDeserialized]
        internal void TrasCargar(StreamingContext _) {
            ZonasVisitadas = Diccionarios.SinMayusculas(ZonasVisitadas);
            Artefactos = Diccionarios.SinMayusculas(Artefactos);
            PoblacionDefectos = Diccionarios.SinMayusculas(PoblacionDefectos);
            // Un guardado anterior a las elecciones narrativas no trae el campo.
            if (EleccionesNarrativas == null)
                EleccionesNarrativas = new Dictionary<string, string>(StringComparer.Ordinal);
            if (ColeccionablesRecogidos == null) ColeccionablesRecogidos = new List<string>();
        }

        // IContadoresDeSimulacion, implementado de forma explicita: para leerlos hay que pedir el puerto,
        // y el puerto es de solo lectura. GameSession los actualiza por los campos publicos antes de llamar
        // a ForresterModel (§5.3, TerminarDia).
        int IContadoresDeSimulacion.DiasSeguidosTrabajando { get { return DiasSeguidosTrabajando; } }
        double IContadoresDeSimulacion.SobreCompromiso { get { return SobreCompromiso; } }
        int IContadoresDeSimulacion.WipActual { get { return WipActual; } }
    }

    /// <summary>Una estimacion del jugador frente a lo que costo de verdad (verbo V5).</summary>
    public sealed class Estimacion {
        public int Dia;
        public string ItemId;
        public double Estimado;
        public double Real;
    }

    /// <summary>Un artefacto vivo: no es un documento entregado, es un documento que se degrada (§A5).</summary>
    public sealed class EstadoArtefacto {
        public double Completitud;   // 0-100
        public double Calidad;       // 0-100
    }
}
