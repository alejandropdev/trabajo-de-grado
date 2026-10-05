using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using Nexus.Core.Tablero;

namespace Nexus.Core.Sesion {
    /// <summary>Una ceremonia de hoy a la que se puede asistir, tal y como la ve la pantalla.</summary>
    public sealed class CeremoniaDeHoy {
        public string Id;
        public string Nombre;
        public string Texto;
        public string ParaQueSirve;
        public int DuracionMinutos;
        /// <summary>La vista de monitoreo que se pone delante en la reunion (o null).</summary>
        public string Muestra;
        public List<OpcionDeCeremonia> Opciones = new List<OpcionDeCeremonia>();
        public bool Hecha;
    }

    /// <summary>Un renglon de la agenda de ceremonias del nivel.</summary>
    public sealed class CitaDeCeremonia {
        public int Dia;
        public string Id;
        public string Nombre;
        public bool SePuedeAsistir;
        /// <summary>pasada | hoy | futura.</summary>
        public string Cuando;
    }

    /// <summary>
    /// Lo que el jugador hace con el tablero, con las ceremonias y con el monitoreo. La regla es una sola:
    /// MIRAR es gratis, ACTUAR cuesta minutos de la jornada (todo pasa por AvanzarReloj, asi que mientras actuas
    /// pueden sonar o caducar avisos, igual que con el trabajo de escritorio).
    ///
    /// Que acciones existen lo decide la metodologia (PoliticaDeTablero): en Kanban se mueve el limite de WIP y no
    /// hay sprint que comprometer; en Scrum es al reves; en Cascada no se prueba hasta la etapa de pruebas.
    /// </summary>
    public sealed partial class GameSession {
        public const int MinutosDeAsignar = 10, MinutosDePriorizar = 5, MinutosDeAjustarWip = 5, MinutosDeRevisarPr = 15,
                         MinutosDeProbar = 60, MinutosDeInspeccionar = 45, MinutosDeMeterAlSprint = 10;

        public TableroDelEquipo Tablero { get { return R.Tablero; } }
        public PoliticaDeTablero Politica { get { return Metodologia == null ? null : Metodologia.PoliticaDeTablero; } }

        /// <summary>El plan de hoy segun la metodologia (la etapa, si es el primer o el ultimo dia de la unidad…). null antes del dia 1.</summary>
        public DayPlan PlanDeHoy { get { return _planDeHoy; } }

        // ==================================================================== lo comun a todas las acciones

        /// <summary>Por que ahora no se puede actuar sobre el proyecto (null = se puede). 'minutos' es lo que costaria.</summary>
        private string PorQueNoSePuedeActuar(int minutos) {
            if (R.Fase != 2 || R.DiaActual == 0 || R.Tablero == null) return "Solo se puede durante los días de desarrollo.";
            if (_reloj.Terminada || (_reloj.LlegoElCierre && !R.JornadaProrrogada)) return "La jornada ya terminó.";
            if (Decision != null || Minijuego != null || TareaAbierta != null) return "Tienes algo abierto: termínalo primero.";
            var restantes = R.JornadaProrrogada ? _reloj.MinutosRestantes : Math.Max(0, _reloj.MinutoDeCierre - _reloj.Minuto);
            if (restantes < minutos) return "No queda tiempo hoy para esto.";
            return null;
        }

        private static void Exigir(string motivo) {
            if (motivo != null) throw new InvalidOperationException(motivo);
        }

        // ==================================================================== asignar y priorizar

        public string PorQueNoSePuedeAsignar(string tarjetaId, string miembroId) {
            var motivo = PorQueNoSePuedeActuar(MinutosDeAsignar);
            if (motivo != null) return motivo;
            var c = R.Tablero.Tarjeta(tarjetaId);
            if (c == null) return "Esa tarjeta no existe.";
            if (c.Terminada) return "Esa tarjeta ya está terminada.";
            if (R.Tablero.Miembro(miembroId) == null) return "Esa persona no está en el equipo.";
            if (c.Asignado == miembroId && c.AsignacionManual) return "Ya la lleva esa persona.";
            return null;
        }

        /// <summary>
        /// Pone a un compañero en una tarjeta. A quien se le da bien ese tipo de trabajo la hace rendir mas, y a quien
        /// no, menos: es lo que mueve el factor de flujo del dia. El piloto automatico reparte por turnos, sin mirar.
        /// </summary>
        public ResultadoDeAvance AsignarTarjeta(string tarjetaId, string miembroId) {
            Exigir(PorQueNoSePuedeAsignar(tarjetaId, miembroId));
            var c = R.Tablero.Tarjeta(tarjetaId);
            c.Asignado = miembroId;
            c.AsignacionManual = true;
            return AvanzarReloj(MinutosDeAsignar);
        }

        public string PorQueNoSePuedePriorizar(string tarjetaId, bool haciaArriba) {
            var motivo = PorQueNoSePuedeActuar(MinutosDePriorizar);
            if (motivo != null) return motivo;
            var c = R.Tablero.Tarjeta(tarjetaId);
            if (c == null) return "Esa tarjeta no existe.";
            if (c.Empezada) return "Ya está empezada: se prioriza lo que aún no se ha tocado.";
            return Vecina(c, haciaArriba) == null ? (haciaArriba ? "Ya es la primera." : "Ya es la última.") : null;
        }

        /// <summary>La tarjeta sin empezar que va justo antes (o despues) de esta, dentro de su mismo grupo (sprint o backlog).</summary>
        private TarjetaDeTrabajo Vecina(TarjetaDeTrabajo c, bool haciaArriba) {
            var grupo = R.Tablero.Tarjetas.Where(x => !x.EsBug && !x.Empezada && x.Sprint == c.Sprint).OrderBy(x => x.Orden).ToList();
            var i = grupo.IndexOf(c);
            var j = haciaArriba ? i - 1 : i + 1;
            return i < 0 || j < 0 || j >= grupo.Count ? null : grupo[j];
        }

        /// <summary>Sube o baja una tarjeta en el orden en que se va a hacer. Lo que va arriba se empieza antes.</summary>
        public ResultadoDeAvance PriorizarTarjeta(string tarjetaId, bool haciaArriba) {
            Exigir(PorQueNoSePuedePriorizar(tarjetaId, haciaArriba));
            var c = R.Tablero.Tarjeta(tarjetaId);
            var otra = Vecina(c, haciaArriba);
            var orden = c.Orden;
            c.Orden = otra.Orden;
            otra.Orden = orden;
            return AvanzarReloj(MinutosDePriorizar);
        }

        // ==================================================================== Scrum: el sprint

        /// <summary>
        /// Lo comprometido en la planificacion, hecho tarjetas: las primeras del backlog, en su orden, hasta llenar los
        /// puntos prometidos. Por eso priorizar ANTES de planificar decide que entra en el sprint.
        /// </summary>
        private void MarcarElSprint(double puntos) {
            var t = R.Tablero;
            if (t == null || Politica == null || !Politica.SprintCerrado) return;
            t.SprintActual = R.SprintActual;
            var suma = 0.0;
            foreach (var c in t.Tarjetas.Where(x => !x.EsBug && !x.Terminada).OrderBy(x => x.Empezada ? 0 : 1).ThenBy(x => x.Orden)) {
                if (suma >= puntos - 0.005) break;
                c.Sprint = R.SprintActual;
                suma += c.Restante;
            }
            R.BurndownDelSprint.Clear();
            R.BurndownDelSprint.Add(Math.Round(RestanteDelSprint(), 2));
            R.DiaDeInicioDelSprint = R.DiaActual;
        }

        /// <summary>
        /// Los dias que dura de verdad la unidad en curso (el sprint, la etapa) en ESTE nivel. No es la longitud que
        /// declara la metodologia: un sprint «de 10 dias» en un nivel de 5 dura 5, y el burndown tiene que acabar ahi.
        /// </summary>
        public int DiasDeLaUnidadActual() {
            if (Reglas == null || _planDeHoy == null) return Math.Max(1, Perfil.DiasTotales);
            foreach (var tramo in Reglas.Tramos)
                if (tramo.Id == _planDeHoy.UnidadId)
                    return Math.Max(1, Math.Min(tramo.DiaFin, Perfil.DiasTotales) - tramo.DiaInicio + 1);
            return Math.Max(1, Perfil.DiasTotales);
        }

        /// <summary>Los puntos del sprint en curso que aun no estan hechos.</summary>
        public double RestanteDelSprint() {
            var t = R.Tablero;
            if (t == null || t.SprintActual < 0) return 0;
            return t.Tarjetas.Where(c => !c.EsBug && c.Sprint == t.SprintActual).Sum(c => c.Restante);
        }

        public string PorQueNoSePuedeMeterAlSprint(string tarjetaId) {
            if (Politica == null || !Politica.SprintCerrado) return "Con esta metodología no hay sprint al que meter nada.";
            var motivo = PorQueNoSePuedeActuar(MinutosDeMeterAlSprint);
            if (motivo != null) return motivo;
            var c = R.Tablero.Tarjeta(tarjetaId);
            if (c == null) return "Esa tarjeta no existe.";
            if (R.Tablero.SprintActual < 0) return "Todavía no se ha planificado ningún sprint.";
            if (c.Sprint == R.Tablero.SprintActual) return "Ya está en el sprint.";
            if (c.Terminada) return "Esa tarjeta ya está terminada.";
            return null;
        }

        /// <summary>
        /// Meter una tarjeta en un sprint que ya esta en marcha. Se puede, y cuesta: el equipo se habia comprometido con
        /// otra cosa (moral), y lo que entra de mas es sobrecompromiso, que genera deuda cada dia hasta cerrar el sprint.
        /// Lo que Scrum pide es dejarlo en el product backlog para el siguiente.
        /// </summary>
        public ResultadoDeAvance MeterAlSprint(string tarjetaId) {
            Exigir(PorQueNoSePuedeMeterAlSprint(tarjetaId));
            var c = R.Tablero.Tarjeta(tarjetaId);
            c.Sprint = R.Tablero.SprintActual;
            R.SobreCompromiso += c.Restante;
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { "MoralEquipo", -2.0 } });
            return AvanzarReloj(MinutosDeMeterAlSprint);
        }

        // ==================================================================== Kanban: el limite de trabajo en curso

        public string PorQueNoSePuedeAjustarElWip(int delta) {
            if (Politica == null || !Politica.LimiteDeWip) return "Con esta metodología no hay límite de trabajo en curso.";
            var motivo = PorQueNoSePuedeActuar(MinutosDeAjustarWip);
            if (motivo != null) return motivo;
            if (R.LimiteWip + delta < 1) return "El límite no puede bajar de 1.";
            if (R.LimiteWip + delta > 9) return "Más de 9 cosas a la vez ya no es un límite.";
            return null;
        }

        /// <summary>
        /// Sube o baja el limite de tarjetas en curso. Bajarlo un paso enfoca al equipo; subirlo «para que nadie este
        /// parado» reparte la atencion y lo frena todo (MotorDelTablero.FactorDelWip). Se ve en el flujo acumulado.
        /// </summary>
        public ResultadoDeAvance AjustarLimiteWip(int delta) {
            Exigir(PorQueNoSePuedeAjustarElWip(delta));
            R.LimiteWip += delta;
            R.Tablero.LimiteEnCurso = R.LimiteWip;
            return AvanzarReloj(MinutosDeAjustarWip);
        }

        // ==================================================================== pruebas y documentos

        /// <summary>La etapa en la que estamos hoy (Cascada); null si la metodologia no va por etapas.</summary>
        public Etapa EtapaDeHoy() {
            if (Metodologia == null || _planDeHoy == null || Metodologia.Calendario.Etapas == null) return null;
            return Metodologia.Calendario.Etapas.FirstOrDefault(e => string.Equals(e.Id, _planDeHoy.UnidadId, StringComparison.Ordinal));
        }

        public string PorQueNoSePuedeProbar() {
            var p = Politica;
            if (p != null && p.PruebasSoloEnEtapas.Count > 0) {
                var etapa = EtapaDeHoy();
                if (etapa == null || !p.PruebasSoloEnEtapas.Contains(etapa.Id))
                    return "Con esta metodología las pruebas tienen su etapa, y aún no es esa" +
                           (etapa == null ? "." : $": estamos en «{etapa.Nombre}». Lo que se puede hacer ahora es inspeccionar el documento.");
            }
            var motivo = PorQueNoSePuedeActuar(MinutosDeProbar);
            if (motivo != null) return motivo;
            return R.PruebasHechasHoy ? "Hoy ya se hizo una ronda de pruebas." : null;
        }

        /// <summary>Lo que cambiaria una ronda de pruebas hoy.</summary>
        public Dictionary<string, double> EfectoDeProbar() {
            // Probar encuentra mas cuanto menos probado esta: la primera ronda rinde mucho, la decima poco.
            var cobertura = Math.Round(3 + 4 * (1 - W.Cobertura / 100.0), 1);
            return new Dictionary<string, double> { { "Cobertura", cobertura }, { "DeudaTecnica", -1 } };
        }

        /// <summary>Una ronda de pruebas: una hora de jornada que sube la cobertura y baja algo la deuda. Una al dia.</summary>
        public ResultadoDeAvance EjecutarPruebas() {
            Exigir(PorQueNoSePuedeProbar());
            EffectApplier.Aplicar(W, EfectoDeProbar().ToDictionary(kv => kv.Key, kv => (object)kv.Value));
            R.PruebasHechasHoy = true;
            R.VecesQueSeProbo++;
            return AvanzarReloj(MinutosDeProbar);
        }

        /// <summary>Los documentos que se estan escribiendo en la etapa de hoy (vacio si la metodologia no los pide).</summary>
        public List<string> DocumentosDeHoy() {
            var etapa = EtapaDeHoy();
            return etapa == null || etapa.Documentos == null ? new List<string>() : etapa.Documentos.ToList();
        }

        public string PorQueNoSePuedeInspeccionar(string documento) {
            if (Politica == null || !Politica.InspeccionDeDocumentos) return "Con esta metodología no hay documentos de etapa que inspeccionar.";
            if (!DocumentosDeHoy().Contains(documento)) return "Ese documento no es de la etapa en la que estamos.";
            var motivo = PorQueNoSePuedeActuar(MinutosDeInspeccionar);
            if (motivo != null) return motivo;
            return R.InspeccionHechaHoy ? "Hoy ya se inspeccionó un documento." : null;
        }

        /// <summary>
        /// Leer el documento de la etapa seccion por seccion con el equipo. Es la forma de encontrar errores cuando aun
        /// no hay nada que ejecutar: sube la calidad del documento y la documentacion del proyecto.
        /// </summary>
        public ResultadoDeAvance InspeccionarDocumento(string documento) {
            Exigir(PorQueNoSePuedeInspeccionar(documento));
            EstadoArtefacto estado;
            if (!R.Artefactos.TryGetValue(documento, out estado)) R.Artefactos[documento] = estado = new EstadoArtefacto();
            estado.Calidad = Math.Min(100, estado.Calidad + 25);
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { "Documentacion", 3.0 } });
            R.InspeccionHechaHoy = true;
            R.DocumentosInspeccionados++;
            return AvanzarReloj(MinutosDeInspeccionar);
        }

        /// <summary>Cada dia de una etapa, sus documentos avanzan lo que toca para estar completos el dia de la firma.</summary>
        private void AvanzarDocumentosDeLaEtapa() {
            var etapa = EtapaDeHoy();
            if (etapa == null || etapa.Documentos == null || etapa.Documentos.Count == 0 || Reglas == null) return;
            var tramo = Reglas.Tramos.FirstOrDefault(t => t.Id == etapa.Id);
            var dias = tramo == null ? 1 : Math.Max(1, tramo.DiaFin - tramo.DiaInicio + 1);
            foreach (var doc in etapa.Documentos) {
                EstadoArtefacto estado;
                if (!R.Artefactos.TryGetValue(doc, out estado)) R.Artefactos[doc] = estado = new EstadoArtefacto();
                estado.Completitud = Math.Min(100, Math.Round(estado.Completitud + 100.0 / dias, 1));
                // La calidad de base es la del proyecto: un equipo que documenta mal escribe mal tambien este.
                if (estado.Calidad <= 0) estado.Calidad = Math.Round(Math.Min(60, W.Documentacion), 1);
            }
        }

        // ==================================================================== revisar un cambio de codigo

        /// <summary>El cambio que hoy espera revision, o null (no hay, o ya se reviso).</summary>
        public MicroPr PrPendiente {
            get {
                if (R.PrRevisado || string.IsNullOrEmpty(R.PrDeHoy) || _catalogo.MicroPrs == null) return null;
                return _catalogo.MicroPrs.FirstOrDefault(p => p.Id == R.PrDeHoy);
            }
        }

        /// <summary>
        /// Un dia de cada dos, si hay trabajo en curso, alguien deja un cambio pequeño para revisar. Cual sale lo decide
        /// un hash de (semilla, dia): reproducible, y sin tocar el azar de la sesion.
        /// </summary>
        private void ElegirPrDeHoy() {
            R.PrDeHoy = null;
            R.TarjetaDelPrDeHoy = null;
            R.PrRevisado = false;
            var banco = _catalogo.MicroPrs;
            if (banco == null || banco.Count == 0 || R.Tablero == null || R.DiaActual % 2 != 0) return;
            var enCurso = R.Tablero.Tarjetas.Where(c => !c.EsBug && !c.Terminada).OrderBy(c => c.Empezada ? 0 : 1).ThenBy(c => c.Orden).FirstOrDefault();
            if (enCurso == null) return;
            R.PrDeHoy = banco[GeneradorDeTarjetas.Hash(_rng.Semilla, "pr:" + R.DiaActual) % banco.Count].Id;
            R.TarjetaDelPrDeHoy = enCurso.Id;
        }

        public string PorQueNoSePuedeRevisar() {
            if (PrPendiente == null) return "Hoy no hay ningún cambio esperando revisión.";
            return PorQueNoSePuedeActuar(MinutosDeRevisarPr);
        }

        /// <summary>
        /// Aprobar o pedir cambios. Aprobar un defecto mete deuda (lo que se cuela en la revision se paga despues);
        /// cazarlo la baja y sube la cobertura; pedir cambios a algo que estaba bien le cuesta al autor. No revisarlo no
        /// hace nada: el equipo se lo revisa entre ellos, como hasta ahora.
        /// </summary>
        public ResultadoDeRevision RevisarPr(string veredicto, out ResultadoDeAvance avance) {
            Exigir(PorQueNoSePuedeRevisar());
            if (veredicto != VeredictosDePr.Aprobar && veredicto != VeredictosDePr.PedirCambios)
                throw new InvalidOperationException($"'{veredicto}' no es un veredicto de revisión: aprobar o cambios.");
            var pr = PrPendiente;
            var aprobo = veredicto == VeredictosDePr.Aprobar;
            var efectos = new Dictionary<string, object>();
            string titulo;
            if (pr.TieneDefecto && aprobo) { efectos["DeudaTecnica"] = 2.0; titulo = "Se coló un defecto"; }
            else if (pr.TieneDefecto) { efectos["DeudaTecnica"] = -1.0; efectos["Cobertura"] = 1.0; titulo = "Lo cazaste antes de que entrara"; }
            else if (aprobo) { efectos["MoralEquipo"] = 0.5; titulo = "Estaba bien, y entró"; }
            else { efectos["MoralEquipo"] = -1.0; titulo = "Estaba bien: pediste cambios que no hacían falta"; }

            var cambios = EffectApplier.Previsualizar(W, efectos);
            EffectApplier.Aplicar(W, efectos);
            var acierto = pr.TieneDefecto != aprobo;
            if (acierto) R.PrsBienRevisados++; else R.PrsMalRevisados++;
            R.PrRevisado = true;
            avance = AvanzarReloj(MinutosDeRevisarPr);
            return new ResultadoDeRevision { Acierto = acierto, Titulo = titulo, Explicacion = pr.Explicacion, Cambios = cambios };
        }

        // ==================================================================== ceremonias

        /// <summary>Las ceremonias de hoy a las que se puede asistir (las que traen opciones), hechas o no.</summary>
        public List<CeremoniaDeHoy> CeremoniasDeHoy() {
            var lista = new List<CeremoniaDeHoy>();
            if (_planDeHoy == null || R.Fase != 2 || R.DiaActual == 0) return lista;
            foreach (var c in _planDeHoy.Ceremonias) {
                if (c.Opciones == null || c.Opciones.Count == 0) continue;
                lista.Add(new CeremoniaDeHoy {
                    Id = c.Id, Nombre = c.Nombre ?? c.Id, Texto = c.Texto, ParaQueSirve = c.ParaQueSirve,
                    DuracionMinutos = c.DuracionMinutos, Muestra = c.Muestra, Opciones = c.Opciones,
                    Hecha = R.CeremoniasHechasHoy.Contains(c.Id)
                });
            }
            return lista;
        }

        public string PorQueNoSePuedeAsistir(string ceremoniaId) {
            var c = CeremoniasDeHoy().FirstOrDefault(x => x.Id == ceremoniaId);
            if (c == null) return "Hoy no hay esa ceremonia.";
            if (c.Hecha) return "Ya asististe hoy.";
            return PorQueNoSePuedeActuar(c.DuracionMinutos);
        }

        /// <summary>
        /// Asistir a una ceremonia y llevarla de una forma. Cuesta sus minutos, aplica lo que esa forma provoca y deja
        /// acta. No asistir no castiga (el equipo la hace sin ti, y asi queda escrito): lo que se pierde es lo que se
        /// habria ganado llevandola bien.
        /// </summary>
        public ResultadoDeAvance AsistirACeremonia(string ceremoniaId, string opcionId) {
            Exigir(PorQueNoSePuedeAsistir(ceremoniaId));
            var c = _planDeHoy.Ceremonias.First(x => x.Id == ceremoniaId);
            var opcion = c.Opciones.FirstOrDefault(o => o.Id == opcionId);
            if (opcion == null) throw new InvalidOperationException($"'{opcionId}' no es una forma de llevar «{c.Nombre}».");

            EffectApplier.Aplicar(W, opcion.Efectos);
            if (R.Tablero != null) R.Tablero.BonoDeFlujoHoy += opcion.BonoDeFlujo;
            R.CeremoniasHechasHoy.Add(c.Id);
            R.ActasDeCeremonias.Add(new ActaDeCeremonia {
                Dia = R.DiaActual, CeremoniaId = c.Id, Nombre = c.Nombre ?? c.Id, Asistio = true,
                OpcionId = opcion.Id, Opcion = opcion.Texto, Texto = opcion.Acta
            });
            return AvanzarReloj(c.DuracionMinutos);
        }

        /// <summary>Al cerrar el dia: las ceremonias a las que no se fue tambien dejan acta.</summary>
        private void LevantarActasDeLasNoAsistidas() {
            foreach (var c in CeremoniasDeHoy().Where(x => !x.Hecha))
                R.ActasDeCeremonias.Add(new ActaDeCeremonia {
                    Dia = R.DiaActual, CeremoniaId = c.Id, Nombre = c.Nombre, Asistio = false,
                    Texto = "No asististe: el equipo la hizo sin ti."
                });
        }

        /// <summary>Todas las ceremonias del nivel, dia a dia: las que ya pasaron, las de hoy y las que vienen.</summary>
        public List<CitaDeCeremonia> AgendaDeCeremonias() {
            var agenda = new List<CitaDeCeremonia>();
            if (Reglas == null) return agenda;
            for (var dia = 1; dia <= Perfil.DiasTotales; dia++)
                foreach (var c in Reglas.PlanFor(dia).Ceremonias)
                    agenda.Add(new CitaDeCeremonia {
                        Dia = dia, Id = c.Id, Nombre = c.Nombre ?? c.Id,
                        SePuedeAsistir = c.Opciones != null && c.Opciones.Count > 0,
                        Cuando = dia < R.DiaActual ? "pasada" : dia == R.DiaActual ? "hoy" : "futura"
                    });
            return agenda;
        }

        // ==================================================================== el dia del tablero

        /// <summary>Al empezar el dia: lo de ayer se olvida, se elige el cambio a revisar y se apuntan las series.</summary>
        private void EmpezarElDiaDelTablero() {
            R.CeremoniasHechasHoy.Clear();
            R.PruebasHechasHoy = false;
            R.InspeccionHechaHoy = false;
            AsegurarTablero();
            var t = R.Tablero;
            if (t == null) return;
            MotorDelTablero.CerrarElDia(t, W.Avance, R.DiaActual);   // empieza un dia: nada provisional
            ElegirPrDeHoy();
            AvanzarDocumentosDeLaEtapa();

            R.SeriePorHacer.Add(Math.Round(t.Tarjetas.Where(c => !c.EsBug && !c.Empezada).Sum(c => c.Puntos), 2));
            R.SerieEnCurso.Add(Math.Round(t.Tarjetas.Where(c => !c.EsBug && c.Empezada && !c.Terminada).Sum(c => c.Puntos), 2));
            R.SerieTerminado.Add(Math.Round(t.Tarjetas.Where(c => !c.EsBug && c.Terminada).Sum(c => c.Puntos), 2));
        }

        /// <summary>Al cerrar el dia: el burndown del sprint, las actas, y si hoy se trabajo por encima del limite.</summary>
        private void CerrarElDiaDelTablero() {
            var t = R.Tablero;
            if (t == null) return;
            LevantarActasDeLasNoAsistidas();
            if (t.SprintActual >= 0 && R.BurndownDelSprint.Count > 0) R.BurndownDelSprint.Add(Math.Round(RestanteDelSprint(), 2));
            if (t.LimiteNeutro > 0 && t.LimiteEnCurso > t.LimiteNeutro) R.VecesExcedioWip++;
        }

        /// <summary>Al cerrar una iteracion: lo TERMINADO en ella (no lo avanzado): la velocidad que se puede prometer.</summary>
        private void CerrarLaUnidadDelTablero() {
            var t = R.Tablero;
            if (t == null) return;
            var inicio = _planDeHoy == null ? 1 : R.DiaActual - _planDeHoy.DiaDentroDeUnidad + 1;
            R.TerminadoPorIteracion.Add(Math.Round(t.Tarjetas.Where(c => !c.EsBug && c.Terminada && c.DiaFin >= inicio).Sum(c => c.Puntos), 2));
        }
    }
}
