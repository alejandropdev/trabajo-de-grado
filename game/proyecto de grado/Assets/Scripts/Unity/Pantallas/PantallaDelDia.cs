using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Guardado;
using Nexus.Core.Jornada;
using Nexus.Core.Minijuegos;
using Nexus.Core.Sesion;
using Nexus.Core.Tarjetas;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Juego;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El dia continuo (§3.3). Lo que se ve SIEMPRE son solo dos cosas, las dos que importan:
    ///   «La jornada» — lo unico que hay que mirar para saber que hacer ahora (un aviso, una decision, el cierre…)
    ///   «El proyecto» — como va: el pronostico, sus cinco medidores y las partes del sistema
    /// Arriba, el HUD (dia, donde estas, reloj, pausa) y debajo el riel de accesos. Todo lo demas —el mapa, el
    /// equipo, el trabajo de escritorio, los avisos y la bitacora, el detalle del proyecto— vive en paneles que se
    /// abren desde el riel (Dia/PantallaDelDia.Paneles.cs). Antes estaba todo a la vez, en tres columnas y cinco
    /// scrolls, y nada destacaba (feedback de la beta).
    ///
    /// El reloj corre solo. Se para DENTRO de una escena (decision, minijuego, planificacion, retro, una carta
    /// que lees), en la pausa y mientras se mira un panel; nunca mientras decides si ir a atender un aviso — si el
    /// mundo se congelara ahi, esa decision no costaria nada. Los avisos solo se atienden desde tu escritorio: si
    /// estas lejos, volver cuesta minutos, y por el camino el aviso puede caducar.
    /// </summary>
    public sealed partial class PantallaDelDia : Pantalla {
        private enum Paso {
            AntesDeEmpezar, Retro, Planificacion, Jornada, Aviso, Decision, Decidido,
            Cierre, Prorroga, Resumen, FinDelDesarrollo
        }

        private GameSession S { get { return App.Sesion; } }
        private LevelRunner Runner { get { return App.Runner; } }

        // interfaz
        private Hoja _ahora;
        private Button _pausa;
        private readonly List<KeyValuePair<Alerta, TMP_Text>> _cuentasAtras = new List<KeyValuePair<Alerta, TMP_Text>>();
        private string _claveAhora;
        private Paso _pasoAnterior;

        // lo que la pantalla sabe y la sesion no
        private bool _escenaApilada;
        private EntradaTraza _decidido;
        private string _cambiosDeLaDecision;
        private double _compromiso;
        private int _diaDelCompromiso = -1;
        private double[] _alEmpezarElDia = new double[5];
        private int _decisionesDeHoy;
        private bool _tengoElInicioDelDia;   // false al recuperar una partida: no se sabe como empezo ese dia

        /// <summary>Los cinco medidores del proyecto: nombre corto (el panel), nombre largo (el detalle) y su entrada del glosario.</summary>
        private static readonly string[] MedidoresCortos = { "Avance", "Deuda", "Moral", "Cobertura", "Cansancio" };
        private static readonly string[] MedidoresLargos = { "Avance", "Deuda técnica", "Moral del equipo", "Cobertura de pruebas", "Cansancio" };
        private static readonly string[] MedidoresEnGlosario = { "avance", "deuda-tecnica", "moral", "cobertura", "cansancio" };

        public override bool PuedeVolver { get { return false; } }

        // ==================================================================== construir

        protected override void Construir() {
            // Fase 2 rediseñada (2026-11-06): HUD fijo, carrusel o escena de zona, carta del momento, tiras de avisos y
            // bandeja M (Dia/PantallaDelDia.Escenario.cs).
            ConstruirEscenario();

            Runner.Usar(S);
            if (S.R.DiaActual > 0) Runner.ReanudarDia();
            Runner.EnEscena = false;
            Runner.Pausado = false;
            Runner.Mirando = false;
            Runner.AlSonarAlerta += AlSonar;
            Runner.AlExpirarAlerta += AlExpirar;
            Runner.AlLlegarElCierre += AlCierre;
            Runner.AlTerminarElDia += AlFinDelDia;

            Anotar(S.R.DiaActual == 0
                ? $"Empieza «{S.Perfil.Nombre}» con {S.Metodologia.Nombre}."
                : $"Partida recuperada: día {S.R.DiaActual}, {S.HoraActual}.");
            GuiaView.RegistrarComprobador(AccionYaHecha);
            if (S.R.DiaActual == 0) GuiaView.Avisar(App, "dia.antes");
        }

        /// <summary>
        /// Para la guia: lo que una burbuja pide, ¿ya esta hecho (o ya no se puede hacer)? Si lo esta, la burbuja
        /// no sale; si saliera, esperaria para siempre algo que no va a pasar.
        /// </summary>
        private bool AccionYaHecha(string accion) {
            if (S == null || string.IsNullOrEmpty(accion)) return false;
            if (accion.StartsWith("ir-a-zona:", StringComparison.Ordinal))
                return S.ZonaActual == accion.Substring("ir-a-zona:".Length);
            switch (accion) {
                case "empezar-dia": return S.R.DiaActual > 0 && Runner.Estado != EstadoDelDia.Parado && Runner.Estado != EstadoDelDia.DiaTerminado;
                case "irse": return Runner.Estado == EstadoDelDia.Prorroga || Runner.Estado == EstadoDelDia.DiaTerminado;
                case "atender": return AlertasSonando().Count == 0;
                default: return false;
            }
        }

        private string RotuloDeVelocidad() {
            var v = Runner.Velocidad;
            // Sin símbolo detras: «▸» no existe en Rajdhani y, como este texto se reasigna directo (sin pasar por
            // Glifos.Seguro), se pintaba como un cuadro junto al ×1.
            return "×" + v.ToString("0.#");
        }

        private void OnDestroy() {
            if (App == null || Runner == null) return;
            Runner.AlSonarAlerta -= AlSonar;
            Runner.AlExpirarAlerta -= AlExpirar;
            Runner.AlLlegarElCierre -= AlCierre;
            Runner.AlTerminarElDia -= AlFinDelDia;
            Runner.Mirando = false;
        }

        // ==================================================================== que pasa ahora

        private Paso PasoActual() {
            if (_decidido != null) return Paso.Decidido;
            if (S.Decision != null) return Paso.Decision;
            if (S.PendingRetro != null && S.PendingRetro.Acciones.Count > 0) return Paso.Retro;
            if (S.PendingPlanning != null) return Paso.Planificacion;
            if (Runner.Estado == EstadoDelDia.Corriendo && AlertasSonando().Count > 0) return Paso.Aviso;

            switch (Runner.Estado) {
                case EstadoDelDia.Parado: return Paso.AntesDeEmpezar;
                case EstadoDelDia.EnElCierre: return Paso.Cierre;
                case EstadoDelDia.Prorroga: return Paso.Prorroga;
                case EstadoDelDia.DiaTerminado: return Paso.Resumen;
                case EstadoDelDia.DesarrolloTerminado: return Paso.FinDelDesarrollo;
                default: return Paso.Jornada;
            }
        }

        private void Update() {
            if (S == null || S.NivelTerminado) return;

            // ★ El reloj se para dentro de una escena, y solo ahi.
            Runner.EnEscena = _escenaApilada || _decidido != null || S.Decision != null || S.PendingPlanning != null ||
                              (S.PendingRetro != null && S.PendingRetro.Acciones.Count > 0) || GuiaView.PausaActiva;

            var paso = PasoActual();
            var clave = $"{paso}|{S.R.DiaActual}|{S.ZonaActual}|{string.Join(",", AlertasSonando().Select(a => a.Id))}|" +
                        $"{_compromiso}|{string.Join(",", _alSprint)}|{Runner.PuedeMoverse}|" +
                        string.Join(",", S.TareasDeOficina.Select(t => S.PorQueNoSePuedeHacer(t.Id) == null ? "1" : "0")) +
                        $"|{S.R.CeremoniasHechasHoy.Count}|{S.PrPendiente != null}";
            if (clave != _claveAhora) {
                _claveAhora = clave;
                PintarAhora(paso);
            }

            // Un panel abierto se quita de en medio cuando la jornada pasa a pedir otra cosa (llego el cierre, se
            // acabo el dia): lo que hay que mirar entonces esta debajo.
            if (paso != _pasoAnterior) {
                if (paso == Paso.Cierre || paso == Paso.Resumen || paso == Paso.FinDelDesarrollo) CerrarPanel();
                _pasoAnterior = paso;
            }

            foreach (var kv in _cuentasAtras)
                kv.Value.text = kv.Key.EstaPendiente
                    ? $"Caduca a las {RelojDeJornada.Formatear(kv.Key.MinutoDeExpiracion)} · quedan {kv.Key.MinutosParaExpirar(S.MinutoDelDia)} min"
                    : "";

            ActualizarEscenario(paso);
        }

        private ZonaDeNivel ZonaDondeEstoy() {
            var mapa = S.Perfil.Mapa;
            return mapa == null || mapa.Vacio ? null : mapa.PorId(S.ZonaActual);
        }

        /// <summary>
        /// Lo que cada medidor esta provocando AHORA, con las mismas formulas que usa el motor: sin esto, solo el
        /// avance parecia importar (feedback beta, ronda 2). 'malos' dice cuales ya estan en zona de daño.
        /// </summary>
        private string[] EfectosDeLosMedidores(out bool[] malos) {
            var w = S.W;
            var inc = S.EntregaIncremental;
            var faltan = Math.Max(0, w.Alcance - w.Avance);
            var lentoDeuda = (1 - Nexus.Core.Simulacion.ForresterModel.FDeuda(w.DeudaTecnica)) * 100;
            var errDeuda = Nexus.Core.Evaluacion.PronosticoDeLanzamiento.DefectosPorDeuda(w.DeudaTecnica, inc);
            var rinde = Nexus.Core.Simulacion.ForresterModel.FMoral(w.MoralEquipo) * 100;
            var errCob = Nexus.Core.Evaluacion.PronosticoDeLanzamiento.DefectosPorCobertura(w.Cobertura, inc);
            var lentoFatiga = (1 - Nexus.Core.Simulacion.ForresterModel.FFatiga(w.Cansancio)) * 100;

            malos = new[] { false, w.DeudaTecnica >= 50, w.MoralEquipo <= 35, w.Cobertura <= 30, w.Cansancio >= 60 };
            return new[] {
                faltan <= 0.05 ? "Todo lo prometido está hecho." : $"Faltan {faltan:0.#} puntos por hacer.",
                $"El equipo va un {lentoDeuda:0} % más lento · +{errDeuda:0.#} errores llegarán al cliente" + (malos[1] ? " · ya rompe cosas" : ""),
                $"El equipo rinde al {rinde:0} % de lo que podría" + (malos[2] ? " · hay roces" : ""),
                $"~{errCob:0} errores sin probar llegarían al cliente",
                $"El equipo va un {lentoFatiga:0} % más lento por cansancio" + (malos[4] ? " · se equivoca más" : "")
            };
        }

        private Color ColorDelNivel(string nivel) {
            return nivel == Nexus.Core.Evaluacion.NivelesDeLanzamiento.Bien ? Tema.success
                 : nivel == Nexus.Core.Evaluacion.NivelesDeLanzamiento.Mal ? Tema.danger : Tema.warning;
        }

        /// <summary>Hablar con alguien de la sala: la escena para el reloj, y la charla lo cobra al contestar.</summary>
        private void Hablar(Nexus.Core.Relaciones.Conversacion c) {
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<PantallaDeConversacion>(p => {
                p.Conversacion = c;
                p.AlContestar = opcion => Runner.Hablar(c.Id, opcion);
                p.AlCerrar = r => {
                    _escenaApilada = false;
                    if (r == null) return;
                    var nombre = App.Catalogo.Relaciones.PersonajePorId(r.Personaje)?.Nombre ?? r.Personaje;
                    Anotar($"{S.HoraActual} · Hablaste con {nombre}: confianza {Textos.Cambio(r.CambioDeConfianza)}.");
                    GuiaView.Hecho(App, "hablar");
                    GuiaView.Avisar(App, "conversacion");
                    if (r.AyudasNuevas.Count > 0) GuiaView.Avisar(App, "ayuda.disponible");
                };
            });
        }

        private void PintarAhora(Paso paso) {
            _cuentasAtras.Clear();
            _ahora.Vaciar();
            // El resumen del dia y el cierre de la jornada no se desplazan: tienen su carta fija (Dia/PantallaDelDia.Resumen.cs, .Cierre.cs).
            var fijo = paso == Paso.Resumen || paso == Paso.Cierre || paso == Paso.FinDelDesarrollo || (paso == Paso.Planificacion && S.SeArmaElSprintBacklog);
            _scrollMomento.gameObject.SetActive(!fijo);
            _fijoMomento.gameObject.SetActive(fijo);
            UiKit.Vaciar(_fijoMomento);
            switch (paso) {
                case Paso.AntesDeEmpezar: AntesDeEmpezar(); break;
                case Paso.Retro: Retro(); break;
                case Paso.Planificacion: Planificacion(); break;
                case Paso.Jornada: Jornada(); break;
                case Paso.Aviso: Aviso(); break;
                case Paso.Decision: Decision(); break;
                case Paso.Decidido: Decidido(); break;
                case Paso.Cierre: Cierre(); break;
                case Paso.Prorroga: Prorroga(); break;
                case Paso.Resumen: Resumen(); break;
                case Paso.FinDelDesarrollo: FinDelDesarrollo(); break;
            }
            switch (paso) {
                case Paso.Retro: GuiaView.Avisar(App, "retro"); break;
                case Paso.Planificacion: GuiaView.Avisar(App, "planificacion"); break;
                case Paso.Decision: GuiaView.Avisar(App, "decision.abierta"); break;
                case Paso.Cierre: GuiaView.Avisar(App, "cierre"); break;
                case Paso.Prorroga: GuiaView.Avisar(App, "prorroga"); break;
                case Paso.Resumen: GuiaView.Avisar(App, "resumen"); break;
            }
        }

        // ---------------------------------------------------------------- cada paso

        private void AntesDeEmpezar() {
            var j = S.Perfil.Jornada;
            _ahora.Etiqueta("Fase 2 · el desarrollo");
            _ahora.Titulo($"{S.Perfil.DiasTotales} días por delante");
            Func<TooltipDeCalidad> tip = () => TooltipDeCalidad.Obtener(Ui, Raiz);
            var ancho = Tema.Px(200); var alto = Tema.Px(72);

            // Lo esencial en tres tarjetas; el detalle, al pasar el ratón.
            var datos = Ui.Fila(_ahora.Raiz, "Datos", Tema.Espacio(3), 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(datos, flexAncho: 1);
            PiezasDeProyecto.Dato(Ui, datos, $"{j.HoraInicio:00} – {j.HoraCierre:00}", "JORNADA", Tema.cyan, "La jornada",
                                  $"De {j.HoraInicio:00}:00 a {j.HoraCierre:00}:00, con el reloj corriendo solo.", tip, ancho, alto);
            PiezasDeProyecto.Dato(Ui, datos, "ESCRITORIO", "AVISOS", Tema.warning, "Los avisos",
                                  "Se atienden desde tu escritorio. Si uno caduca, alguien decide por ti.", tip, ancho, alto);
            PiezasDeProyecto.Dato(Ui, datos, "MÁS › MAPA", "EL EDIFICIO", Tema.violet, "Lo demás del edificio",
                                  "Está en «Más» › «Mapa». Moverse cuesta tiempo.", tip, ancho, alto);

            // El product backlog que armo el jugador: cuantas de las pedidas entraron, y lo que sobro y falto (detalle al pasar el raton).
            var backlog = S.BacklogCreado;
            if (backlog != null) {
                var limpio = backlog.Dejadas == 0 && backlog.DeMas == 0;
                var color = limpio ? Tema.success : Tema.warning;
                var panel = Ui.Columna(_ahora.Raiz, "Backlog revisado", Tema.Espacio(2), 0, TextAnchor.UpperCenter);
                UiKit.Tamano(panel, flexAncho: 1);
                PiezasDeProyecto.Cristal(Ui, panel, NexusTheme.Alfa(color, 0.6f));
                panel.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 12, 14);
                var rotulo = Ui.Texto(panel, "TU PRODUCT BACKLOG, REVISADO CON EL CLIENTE", EstiloTexto.Leyenda, color, TextAlignmentOptions.Center);
                UiKit.Tamano(rotulo, alto: Tema.Px(18));
                var fila = Ui.Fila(panel, "Resultado", Tema.Espacio(3), 0, TextAnchor.MiddleCenter);
                var total = backlog.Pedidas + backlog.Dejadas;
                var anillo = PiezasDeProyecto.Anillo(Ui, fila, Tema.Px(96), total == 0 ? 0 : (float)backlog.Pedidas / total, color, backlog.Pedidas + "/" + total);
                UiKit.Tamano((RectTransform)anillo, Tema.Px(96), Tema.Px(96));
                var cifras = Ui.Fila(fila, "Cifras", Tema.Espacio(2), 0, TextAnchor.MiddleCenter);
                UiKit.Tamano(cifras, flexAncho: 1);
                var w = Tema.Px(130); var h = Tema.Px(64);
                string Detalle(string tipo) => string.Join("\n", backlog.Renglones.Where(r => r.Tipo == tipo).Select(r => "· " + r.Texto));
                if (limpio)
                    PiezasDeProyecto.Dato(Ui, cifras, "TODAS", "LAS PEDIDAS", Tema.success, "Backlog completo",
                                          Detalle("bien"), tip, w * 1.4f, h);
                else {
                    PiezasDeProyecto.Dato(Ui, cifras, backlog.Pedidas.ToString(), "ENTRARON", Tema.success, "Lo que entró",
                                          Detalle("bien"), tip, w, h);
                    if (backlog.Dejadas > 0)
                        PiezasDeProyecto.Dato(Ui, cifras, backlog.Dejadas.ToString(), "FALTARON", Tema.danger, "Lo que faltó",
                                              Detalle("falta") + "\n\nEstá en el tablero, al final de la fila, como «Se descubrió tarde».", tip, w, h);
                    if (backlog.DeMas > 0)
                        PiezasDeProyecto.Dato(Ui, cifras, backlog.DeMas.ToString(), "SOBRARON", Tema.warning, "Lo que sobraba",
                                              Detalle("sobra"), tip, w, h);
                }
                foreach (var kv in backlog.Cambios)
                    PiezasDeProyecto.Dato(Ui, cifras, Textos.Cambio(kv.Value), Textos.NombreDeStock(kv.Key).ToUpperInvariant(), Tema.ink,
                                          "Lo que eso le hizo al proyecto", $"{Textos.NombreDeStock(kv.Key)} {Textos.Cambio(kv.Value)}.", tip, w, h);
            }
            _ahora.Accion("Empezar el día 1", EmpezarDia);
        }

        private void Retro() {
            var retro = S.PendingRetro;
            var ceremonia = S.Reglas == null ? null : S.Reglas.CeremoniaPorId(retro.CeremoniaId);
            _ahora.Etiqueta("Ceremonia · " + (ceremonia != null && !string.IsNullOrEmpty(ceremonia.Nombre) ? ceremonia.Nombre : "retrospectiva"));
            _ahora.Titulo("¿Qué cambiamos del proceso?");
            // Con tarjeta, la retro es una pregunta y una mejora; el texto largo se queda fuera de la vista.
            var tarjeta = App.Preferencias.modoTarjetas ? TarjetaDeProceso.DeRetro(retro, ceremonia == null ? null : ceremonia.Nombre) : null;
            if (tarjeta != null) {
                _ahora.Nota("El reloj espera. Lo que elijas cambia cómo trabajas desde hoy.");
                Action abrirRetro = () => ProcesoConTarjeta(tarjeta, "Retrospectiva", opcion => {
                    var accion = retro.Acciones.FirstOrDefault(x => x.Id == opcion.Id);
                    S.ElegirAccionRetro(opcion.Id);
                    Anotar($"Retrospectiva: «{(accion != null ? accion.Texto : opcion.Etiqueta)}»");
                });
                _ahora.Accion("Elegir una mejora  ►", abrirRetro);
                AbrirSola(() => S.PendingRetro != null, abrirRetro);
                return;
            }
            if (!string.IsNullOrEmpty(retro.Texto)) _ahora.Parrafo(retro.Texto);
            _ahora.Nota(Solo("Lo que elijas cambia cómo trabaja el equipo desde hoy. El reloj espera.", "Lo que elijas cambia cómo trabajas desde hoy. El reloj espera."));
            var cartas = retro.Acciones.Select(accion => {
                var a = accion;
                return new OpcionDeCarta {
                    Titulo = a.Texto,
                    AlElegir = () => {
                        S.ElegirAccionRetro(a.Id);
                        Anotar($"Retrospectiva: «{a.Texto}»");
                    }
                };
            }).ToList();
            _ahora.Mano(cartas, new ManoDeCartas.Ajustes { Sello = "ACORDADO" });
        }

        private readonly HashSet<string> _alSprint = new HashSet<string>();
        private int _diaDelSprintBacklog = -1;

        private RectTransform _capaDelDia;
        private bool _ayudaDelSprint;

        /// <summary>La capa por encima de todo el dia donde vuelan las fichas (y los sellos) fuera del muro.</summary>
        private RectTransform CapaDelDia() {
            if (_capaDelDia == null) _capaDelDia = PiezasDelMuro.CapaDeArrastre(Ui, Raiz);
            _capaDelDia.SetAsLastSibling();
            return _capaDelDia;
        }

        /// <summary>
        /// La planificacion cuando hay mas de un sprint (2026-11-06, sin scroll): dos carriles, el product backlog y
        /// «Este sprint», con el tubo de lo que cabe en medio. Las fichas se arrastran de uno a otro (o un clic las pasa);
        /// lo ya empezado entra si o si, con candado. Pasada la raya, el tubo se vuelve ambar y lo que no cabe se marca.
        /// </summary>
        private void ArmarElSprintBacklog() {
            var plan = S.PendingPlanning;
            var candidatas = S.CandidatasAlSprint();
            if (_diaDelSprintBacklog != S.R.DiaActual) {
                // Se parte de lo que cabria: las primeras del backlog hasta la capacidad. El jugador quita y pone.
                _diaDelSprintBacklog = S.R.DiaActual;
                _alSprint.Clear();
                var cabe = 0.0;
                foreach (var c in candidatas) {
                    if (!c.Empezada && cabe + c.Restante > plan.CapacidadSugerida + 0.005) continue;
                    _alSprint.Add(c.Id); cabe += c.Restante;
                }
            }
            var dentro = candidatas.Where(c => _alSprint.Contains(c.Id) || c.Empezada).ToList();
            var fuera = candidatas.Where(c => !_alSprint.Contains(c.Id) && !c.Empezada).ToList();
            var suma = dentro.Sum(c => c.Restante);
            var pasado = suma > plan.CapacidadSugerida + 0.5;
            var raiz = _fijoMomento;
            var capa = CapaDelDia();

            // ---- cabecera: que es, cuanto cabe, y las palabras detras de «?»
            var cabecera = Ui.Fila(raiz, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var titulos = Ui.Columna(cabecera, "Titulos", 2);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, ("Planificación de " + plan.Unidad).ToUpperInvariant(), EstiloTexto.Leyenda, Tema.cyan).characterSpacing = 3;
            var titulo = Ui.Texto(titulos, "ARMA EL SPRINT", EstiloTexto.Titulo, Tema.ink);
            titulo.font = Tema.FuenteDisplay;
            Ui.Texto(titulos, $"Caben unos {plan.CapacidadSugerida:0.#} puntos. Pasa al sprint lo que {Solo("el equipo se compromete", "te comprometes")} a terminar.",
                     EstiloTexto.Pequeno, Tema.inkMuted);
            Ui.Boton(cabecera, _ayudaDelSprint ? "Ocultar palabras" : "?", () => { _ayudaDelSprint = !_ayudaDelSprint; _claveAhora = null; }, VarianteBoton.Fantasma);
            if (_ayudaDelSprint) PantallaDeGlosario.Palabras(App, raiz, "Si alguna palabra no te suena:", "backlog", "sprint", "planificacion-de-sprint");

            // ---- los dos carriles y el tubo en medio
            var mesa = Ui.Fila(raiz, "Mesa", Tema.espacio * 1.5f, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(mesa, flexAncho: 1, flexAlto: 1);
            Action<string> alternar = id => {
                var c = candidatas.FirstOrDefault(x => x.Id == id);
                if (c == null || c.Empezada) return;
                if (!_alSprint.Remove(id)) _alSprint.Add(id);
            };

            RectTransform listaFuera, listaDentro;
            DestinoDeFicha destinoFuera, destinoDentro;
            var carrilFuera = PiezasDelMuro.Carril(Ui, mesa, "Product backlog", fuera.Count.ToString(), Tema.inkMuted, out listaFuera, out destinoFuera, fuera.Sum(c => c.Restante));
            UiKit.Tamano(carrilFuera, ancho: 0, flexAncho: 1, flexAlto: 1);
            if (fuera.Count == 0) PiezasDelMuro.Vacio(Ui, listaFuera, "Todo entra en el sprint.");
            foreach (var c in fuera) FichaDePlanificacion(listaFuera, capa, c, "fuera", false, alternar);

            var medio = Ui.Columna(mesa, "Medio", Tema.espacio, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(medio, 150, flexAlto: 1);
            PiezasDelMuro.Flecha(Ui, medio, 110, 40, false, Tema.cyan);
            var medidor = MedidorDeCapacidad.Crear(Ui, medio, "plan." + S.R.DiaActual, true, 320, 46);
            medidor.Fijar(suma, plan.CapacidadSugerida);
            var nota = Ui.Texto(medio, pasado ? "Te pasas: cada día suma deuda." : "Lo que no entre espera al siguiente sprint.", EstiloTexto.Leyenda,
                                pasado ? Tema.warning : Tema.inkMuted, TextAlignmentOptions.Center);
            UiKit.Tamano(nota, 140);

            var carrilDentro = PiezasDelMuro.Carril(Ui, mesa, "Este sprint", dentro.Count.ToString(), Tema.cyan, out listaDentro, out destinoDentro, suma);
            UiKit.Tamano(carrilDentro, ancho: 0, flexAncho: 1, flexAlto: 1);
            if (dentro.Count == 0) PiezasDelMuro.Vacio(Ui, listaDentro, "Arrastra aquí lo que entra.");
            var acumulado = 0.0;
            foreach (var c in dentro) {
                acumulado += c.Restante;
                FichaDePlanificacion(listaDentro, capa, c, "dentro", acumulado > plan.CapacidadSugerida + 0.5, alternar);
            }

            destinoDentro.Motivo = (f, _) => null;
            destinoDentro.Coste = f => {
                if (f.Origen != "fuera") return null;
                var c = candidatas.FirstOrDefault(x => x.Id == f.Id);
                return c == null ? null : $"+{c.Restante:0.#} pts";
            };
            destinoDentro.AlRecibir = (f, _) => { if (f.Origen == "fuera") alternar(f.Id); };
            destinoFuera.Motivo = (f, _) => null;
            destinoFuera.AlRecibir = (f, _) => { if (f.Origen == "dentro") alternar(f.Id); };

            // ---- abajo: comprometerse
            var pie = Ui.Fila(raiz, "Pie", Tema.espacio, 0, TextAnchor.MiddleRight);
            UiKit.Tamano(Ui.Nodo(pie, "Hueco"), flexAncho: 1);
            var comprometer = Ui.Boton(pie, $"Comprometerse con {suma:0.#} puntos", () => {
                var n = S.CandidatasAlSprint().Count(c => _alSprint.Contains(c.Id) || c.Empezada);
                PiezasDelMuro.Marca(Ui, capa, capa.TransformPoint(Vector3.zero), "COMPROMETIDO", pasado ? Tema.warning : Tema.success);
                S.ComprometerTarjetas(_alSprint);
                _diaDelSprintBacklog = -1;
                Anotar($"Planificación: {Solo("el equipo se compromete", "te comprometes")} a {n} tarjetas ({suma:0.#} puntos).");
            }, VarianteBoton.Primario);
            comprometer.interactable = suma > 0.005;
        }

        /// <summary>Una ficha de la planificacion: un clic (o arrastrarla al otro carril) la pasa de uno a otro.</summary>
        private void FichaDePlanificacion(RectTransform lista, RectTransform capa, Nexus.Core.Tablero.TarjetaDeTrabajo c, string origen, bool noCabe, Action<string> alternar) {
            var modulo = S.Perfil.Proyecto == null ? null : S.Perfil.Proyecto.Modulo(c.Modulo);
            var ficha = PiezasDelMuro.Ficha(Ui, lista, new DatosDeFicha {
                Id = c.Id, Titulo = c.Titulo, Tipo = c.Tipo, Puntos = c.Restante, EsBug = c.EsBug,
                Avance = c.Empezada ? (float)(c.Trabajo / Math.Max(0.01, c.Puntos)) : -1,
                Fija = c.Empezada, PorQueFija = "Ya está empezada: entra sí o sí.", NoCabe = noCabe, Tarde = c.DescubiertaTarde,
                Lineas = modulo == null ? null : new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("módulo", modulo.Nombre) }
            }, capa);
            ficha.Origen = origen;
            var id = c.Id;
            ficha.AlPulsar = () => alternar(id);
            ficha.AlFallar = motivo => PiezasDelMuro.Aviso(Ui, capa, motivo, Tema.warning);
        }

        private void Planificacion() {
            if (S.SeArmaElSprintBacklog) { ArmarElSprintBacklog(); return; }
            var plan = S.PendingPlanning;
            if (_diaDelCompromiso != S.R.DiaActual) {
                _diaDelCompromiso = S.R.DiaActual;
                _compromiso = Math.Round(plan.CapacidadSugerida);
            }
            _ahora.Etiqueta("Ceremonia · planificación de " + plan.Unidad);
            _ahora.Titulo("¿Con cuánto te comprometes?");
            var tarjeta = App.Preferencias.modoTarjetas ? TarjetaDeProceso.DePlanificacion(plan, S.TrabajasSolo) : null;
            if (tarjeta != null) {
                _ahora.Nota("El reloj espera. Prometer de más no da error, pero se paga después.");
                Action abrirPlan = () => ProcesoConTarjeta(tarjeta, "Planificación", opcion => {
                    var puntos = TarjetaDeProceso.PuntosDe(opcion);
                    S.Comprometer(puntos);
                    Anotar($"Planificación: {Solo("el equipo se compromete", "te comprometes")} a {puntos:0} puntos.");
                });
                _ahora.Accion("Comprometerse  ►", abrirPlan);
                AbrirSola(() => S.PendingPlanning != null, abrirPlan);
                return;
            }
            _ahora.Parrafo($"{Solo("El equipo calcula que puede", "Calculas que puedes")} con unos {plan.CapacidadSugerida:0.#} puntos. Quedan {plan.PuntosPendientes:0} puntos por hacer.");
            // Lo que se promete son tarjetas de verdad: las primeras del backlog, hasta llenar los puntos.
            if (S.Tablero != null) {
                var entran = new List<string>();
                var suma = 0.0;
                foreach (var c in S.Tablero.Tarjetas.Where(x => !x.EsBug && !x.Terminada).OrderBy(x => x.Empezada ? 0 : 1).ThenBy(x => x.Orden)) {
                    if (suma >= _compromiso - 0.005) break;
                    entran.Add($"{c.Titulo} ({c.Restante:0.#})");
                    suma += c.Restante;
                }
                var entra = _ahora.Tarjeta($"Con {_compromiso:0} puntos entran en el sprint {entran.Count} tarjetas");
                Ui.Texto(entra, entran.Count == 0 ? "Ninguna." : string.Join(" · ", entran), EstiloTexto.Pequeno, Tema.ink);
                Ui.Texto(entra, "Entran en orden, de arriba abajo. El resto espera.", EstiloTexto.Pequeno);
            }
            _ahora.Nota("Prometer más de lo que cabe suma deuda técnica cada día.");

            var fila = _ahora.Fila();
            UiKit.Tamano(Ui.Boton(fila, "−", () => _compromiso = Math.Max(0, _compromiso - 1)), ancho: 64);
            Ui.Texto(fila, $"{_compromiso:0} puntos", EstiloTexto.Subtitulo,
                     _compromiso > plan.CapacidadSugerida + 0.5 ? Tema.warning : Tema.success, TextAlignmentOptions.Center);
            UiKit.Tamano(Ui.Boton(fila, "+", () => _compromiso += 1), ancho: 64);

            _ahora.Accion($"Comprometerse a {_compromiso:0} puntos", () => {
                S.Comprometer(_compromiso);
                Anotar($"Planificación: {Solo("el equipo se compromete", "te comprometes")} a {_compromiso:0} puntos.");
            });
        }

        private void Jornada() {
            _ahora.Etiqueta("La jornada corre");
            _ahora.Titulo(S.HoraActual.StartsWith("0") ? "Buenos días" : "Trabajando");
            // El dia no dice si hoy va a sonar algo ni cuando: los avisos llegan, no se anuncian.
            _ahora.Parrafo("Recorre la oficina, habla con la gente o adelanta trabajo. Si surge algo, te llegará un aviso.");

            // Lo que hoy trae para leer cabe en una linea cada cosa; entero, en «Avisos».
            var brief = S.BriefDeHoy;
            if (brief != null && brief.Incidencia != null) {
                var n = Ui.Notificacion(_ahora.Raiz, $"Hoy, por tu {NombreDeEstadistica(brief.Incidencia.Estadistica)} ({brief.Incidencia.Valor:0}): {brief.Incidencia.Titulo}", Tono.Aviso);
                Ui.Boton(n, "Ver", () => AbrirAvisos(0), VarianteBoton.Fantasma);
            }
            if (brief != null && brief.Avisos.Count > 0) {
                var n = Ui.Notificacion(_ahora.Raiz, brief.Avisos.Count == 1 ? "Hay 1 anuncio de lo que llegará en los próximos días."
                                                                              : $"Hay {brief.Avisos.Count} anuncios de lo que llegará en los próximos días.", Tono.Cyan);
                Ui.Boton(n, "Leer", () => AbrirAvisos(0), VarianteBoton.Fantasma);
            }
            var pendientes = S.CeremoniasDeHoy().Where(c => !c.Hecha).ToList();
            if (pendientes.Count > 0) {
                var n = Ui.Notificacion(_ahora.Raiz, "Hoy puedes asistir a: " + string.Join(" · ", pendientes.Select(c => $"{c.Nombre} ({c.DuracionMinutos} min)")), Tono.Cyan);
                Ui.Boton(n, "Ir", () => AbrirCeremonias(0), VarianteBoton.Fantasma);
            }
            if (S.PrPendiente != null) {
                var n = Ui.Notificacion(_ahora.Raiz, Solo("Un compañero ha dejado un cambio de código para que lo revises.", "Tienes un cambio tuyo sin revisar: míralo con ojos frescos antes de integrarlo."), Tono.Cyan);
                Ui.Boton(n, "Revisar", AbrirRevision, VarianteBoton.Fantasma);
            }
            if (S.TareasDeOficina.Count > 0) GuiaView.Avisar(App, "oficina.disponible");

            _ahora.Espacio();
            // Sin «Adelantar el reloj» (2026-11-07): el tiempo corre solo y se acelera con la velocidad (×1/×2/×4).
            if (S.TareasDeOficina.Count > 0) Ui.Boton(_ahora.Fila(), "Trabajar en tu escritorio", AbrirEscritorio);
        }

        private void EmpezarTarea(Nexus.Core.Oficina.TareaDeOficina tarea) {
            CerrarPanel();   // el reto se abre sobre el dia, no sobre el panel del escritorio
            if (!Runner.EmpezarTarea(tarea.Id)) return;
            Anotar($"{S.HoraActual} · Trabajo: «{tarea.Titulo}».");
            // El caso de hoy: cada dia otro (otro diagrama, otro backlog, otro plan de pruebas), sin repetir.
            var caso = S.CasoDeHoy(tarea.Id);
            var pendiente = new PendingMinigame {
                MinijuegoId = caso.Minijuego, Archivo = caso.Archivo, NivelAndamiaje = S.Perfil.NivelAndamiaje
            };
            AbrirEscenaDeMinijuego(pendiente, true, resultado => {
                var cambios = S.ResolverTarea(resultado);
                Anotar($"Trabajo terminado: {Textos.Previsualizar(cambios)}.");
                GuiaView.Hecho(App, "oficina");
                GuiaView.Avisar(App, "oficina.hecha");
            });
        }

        private void Aviso() {
            _ahora.Etiqueta("Aviso", Tema.warning);
            var alertas = AlertasSonando();
            _ahora.Titulo(alertas.Count == 1 ? "Ha llegado un aviso" : $"Tienes {alertas.Count} avisos");
            _ahora.Nota("El reloj NO se para mientras decides si ir: esperar también cuesta.");

            var ancla = Ancla();
            var enElEscritorio = ancla == null || S.ZonaActual == ancla.Id;
            foreach (var alerta in alertas) {
                var a = alerta;
                var tarjeta = _ahora.Tarjeta(a.Tipo == TiposDeAlerta.Minijuego ? "Ticket · " + a.Canal : a.Canal, Tono.Aviso);
                Ui.Texto(tarjeta, a.Texto, EstiloTexto.Cuerpo);
                _cuentasAtras.Add(new KeyValuePair<Alerta, TMP_Text>(a, Ui.Texto(tarjeta, "", EstiloTexto.Pequeno, Tema.warning)));
                var texto = enElEscritorio
                    ? "Atender"
                    : $"Volver al escritorio y atender ({S.Perfil.Mapa.CosteDeVisitar(S.ZonaActual, ancla.Id)} min)";
                Ui.Boton(Ui.Fila(tarjeta), texto, () => Atender(a), VarianteBoton.Primario);
            }
        }

        private void Decision() {
            var d = S.Decision;
            _ahora.Etiqueta("Decisión");
            _ahora.Titulo(d.Titulo);
            // Todas las decisiones son una mano de cartas (2026-11-05). Con textos cortos, la frase y la etiqueta de cada
            // opcion; si no, el aviso y el texto entero. Al pie de cada carta, lo que cambia ya; lo de despues no se ve.
            var corta = App.Preferencias.modoTarjetas && !string.IsNullOrEmpty(d.Frase);
            if (corta) _ahora.Parrafo(d.Frase);
            else if (!string.IsNullOrEmpty(d.TextoAviso)) _ahora.Parrafo(d.TextoAviso);
            _ahora.Nota("El reloj espera. Al pie de cada carta, lo que cambia ya; lo de después no se ve.");
            var cartas = new List<OpcionDeCarta>();
            foreach (var opcion in d.Opciones) {
                var o = opcion;
                var pie = Textos.Previsualizar(o.Previsualizacion);
                if (!string.IsNullOrEmpty(o.NotaDeMetodologia)) pie += (string.IsNullOrEmpty(pie) ? "" : " · ") + o.NotaDeMetodologia;
                cartas.Add(new OpcionDeCarta {
                    Titulo = corta && !string.IsNullOrEmpty(o.Etiqueta) ? o.Etiqueta : o.Texto,
                    Pie = pie,
                    Bloqueo = o.Bloqueada ? (string.IsNullOrEmpty(o.MotivoBloqueo) ? "no disponible" : o.MotivoBloqueo) : null,
                    AlElegir = () => Resolver(o.Id),
                });
            }
            _ahora.Mano(cartas, new ManoDeCartas.Ajustes { Sello = "DECIDIDO" });
        }

        private void Decidido() {
            _ahora.Etiqueta("Decidido");
            _ahora.Titulo(_decidido.Titulo);
            _ahora.Parrafo("Elegiste: " + _decidido.OpcionTexto);
            _ahora.Parrafo("En el momento: " + _cambiosDeLaDecision);
            _ahora.Nota("Si trae consecuencias, llegarán estos días. Si estuvo bien, lo verás en las Lecciones.");
            _ahora.Accion("Volver a la jornada", () => { _decidido = null; });
        }

        // Cierre(): Dia/PantallaDelDia.Cierre.cs (la ventana del atardecer, 2026-11-07).

        private void Prorroga() {
            _ahora.Etiqueta("Horas extra");
            _ahora.Titulo("Te has quedado");
            _ahora.Parrafo("Ya no llegan avisos. Habla con quien quede o busca lo que te falta.");
            _ahora.Nota("Las barras se mueven al cerrar el día, con las horas extra. El día acaba solo a la hora límite.");
        }

        // FinDelDesarrollo(): Dia/PantallaDelDia.Cierre.cs (la tuberia hacia produccion, 2026-11-07).

        // ==================================================================== acciones

        private void IrA(string zonaId) {
            Runner.IrAZona(zonaId);
            if (S.ZonaActual != zonaId) return;
            GuiaView.Hecho(App, "ir-a-zona:" + zonaId);
            GuiaView.Avisar(App, "mapa.zona");
        }

        private void EmpezarDia() {
            var w = S.W;
            _alEmpezarElDia = new[] { w.Avance, w.DeudaTecnica, w.MoralEquipo, w.Cobertura, w.Cansancio };
            _decisionesDeHoy = 0;
            _tengoElInicioDelDia = true;

            var brief = Runner.EmpezarDia();
            if (brief == null) return;
            GuiaView.Hecho(App, "empezar-dia");
            // Cada dia empieza mirando los indicadores: como esta el proyecto antes de hacer nada.
            _diapositivaRecordada = Nexus.Core.Jornada.DiapositivasDelDia.Kpis;
            if (_carrusel != null) _carrusel.IrA(Nexus.Core.Jornada.DiapositivasDelDia.Kpis);
            var dia = brief.Dia;
            AnotarNovedad($"— Día {brief.Dia} · {brief.EtiquetaUnidad} —");
            if (brief.Ceremonias.Count > 0) AnotarNovedad("Ceremonias: " + string.Join(", ", brief.Ceremonias));
            if (brief.Incidencia != null)
            {
                AnotarNovedad($"Consecuencia de tu {NombreDeEstadistica(brief.Incidencia.Estadistica)} ({brief.Incidencia.Valor:0}): {brief.Incidencia.Titulo}.");
                GuiaView.Avisar(App, "incidencia");
            }
            foreach (var origen in brief.EfectosQueVencieronHoy.Distinct())
                AnotarNovedad("Hoy se cobra algo de antes: " + NombreDeOrigen(origen) + ".");
            foreach (var papel in S.PapelesDelCliente().Where(p => p.Dia == brief.Dia))
                AnotarNovedad($"Llegó un papel del cliente: «{papel.Titulo}». Está en «Cliente».");

            if (S.Beat != null) {
                var guion = App.Catalogo.Guiones.FirstOrDefault(g => g.Id == S.Beat.BeatId);
                if (guion != null) {
                    Anotar($"Escena: «{guion.Titulo}»");
                    _escenaApilada = true;
                    Runner.EnEscena = true;
                    FlujoDelNivel.Reproducir(App, guion, S.Beat.Variante, true, () => {
                        _escenaApilada = false;
                        GuiaView.Avisar(App, "dia." + dia);
                    });
                    return;
                }
            }
            GuiaView.Avisar(App, "dia." + dia);
        }

        private void Atender(Alerta alerta) {
            CerrarPanel();   // la decision o el reto se abren sobre el dia
            var ancla = Ancla();
            if (ancla != null && S.ZonaActual != ancla.Id) Runner.IrAZona(ancla.Id);
            if (!alerta.EstaPendiente) {
                Anotar("Llegaste tarde: el aviso caducó por el camino.");
                if (AlertasSonando().Count == 0) GuiaView.Cancelar("atender");
                return;
            }
            if (!Runner.Atender(alerta.Id)) return;
            GuiaView.Hecho(App, "atender");
            if (S.Minijuego != null) AbrirMinijuego(S.Minijuego);
        }

        private void AbrirMinijuego(PendingMinigame pendiente) {
            AbrirEscenaDeMinijuego(pendiente, false, resultado => {
                S.ResolverMinijuego(resultado);
                Anotar($"{S.HoraActual} · {PantallaDeMinijuego.TituloDelResultado(resultado.Resultado, pendiente.Verbo)}.");
            });
        }

        /// <summary>Abre la escena de un minijuego encima del dia, con el reloj parado. 'practica' = trabajo de oficina.</summary>
        private void AbrirEscenaDeMinijuego(PendingMinigame pendiente, bool practica, Action<ResultadoMinijuego> alResolver) {
            MinijuegoDef def;
            try {
                def = Nexus.Core.Datos.CatalogoMinijuegos.Parsear(new Nexus.Core.Datos.CatalogoDeArchivos(RutasDeGuardado.Contenido).LeerCatalogo(pendiente.Archivo));
            } catch (Exception ex) {
                // El catalogo se valido al arrancar (INV-5), asi que esto no deberia pasar; si pasa, la partida sigue.
                Debug.LogException(ex);
                alResolver(PuenteDelMotor.Omitido(pendiente.MinijuegoId, pendiente.ObjetivoAprendizaje, null));
                Anotar("No se pudo abrir el minijuego " + pendiente.MinijuegoId + ".");
                return;
            }

            // Guiado: la primera vez que este perfil se enfrenta a la MECANICA (detectar, ordenar, repartir), sea un
            // reto o una practica, y tambien en el tutorial. Las siguientes, libre: es donde se demuestra lo aprendido.
            // La escena anota la mecanica como guiada cuando el recorrido se termina (PantallaDeMinijuego).
            pendiente.Guiado = pendiente.Guiado || S.Perfil.MinijuegosGuiados || !App.YaSeGuioLaMecanica(def.Verbo);

            // La base del nivel deja los retos sin ayudas automaticas; un buen compañero o una buena relacion las traen.
            var andamiaje = S.AndamiajePara(def.Verbo);
            pendiente.NivelAndamiaje = andamiaje.Nivel;
            pendiente.AyudaDe = andamiaje.Quien;
            pendiente.AyudaPor = andamiaje.Via;

            _escenaApilada = true;
            Runner.EnEscena = true;
            Action<ResultadoMinijuego> alTerminar = resultado => {
                if (pendiente.AyudaDe != null) S.AnotarRetoConAyuda(pendiente.AyudaDe);
                alResolver(resultado);
                App.AnotarMinijuegoJugado(pendiente.MinijuegoId);
                App.Router.Volver();
                _escenaApilada = false;
            };
            switch (Verbos.Normalizar(def.Verbo)) {
                case Verbos.Ordenar:
                    App.Router.Apilar<PantallaOrdenar>(p => { p.Def = def; p.Pendiente = pendiente; p.Practica = practica; p.AlTerminar = alTerminar; });
                    break;
                case Verbos.Repartir:
                    App.Router.Apilar<PantallaRepartir>(p => { p.Def = def; p.Pendiente = pendiente; p.Practica = practica; p.AlTerminar = alTerminar; });
                    break;
                default:
                    // Los diagramas se juegan con cartas de solucion (2026-11-09); el grafo y la secuencia, marcando.
                    if (Nexus.Core.Minijuegos.Corregir.PuntosDelDiagrama.Aplica(def))
                        App.Router.Apilar<PantallaCorregirDiagrama>(p => { p.Def = def; p.Pendiente = pendiente; p.Practica = practica; p.AlTerminar = alTerminar; });
                    else
                        App.Router.Apilar<PantallaDetectar>(p => { p.Def = def; p.Pendiente = pendiente; p.Practica = practica; p.AlTerminar = alTerminar; });
                    break;
            }
        }

        /// <summary>
        /// La retrospectiva y la planificacion como tarjeta: se elige deslizando o con un boton, y al cerrar se llama al
        /// mismo camino de siempre (ElegirAccionRetro, Comprometer) con lo elegido.
        /// </summary>
        /// <summary>
        /// Abre la tarjeta sin esperar a que se pulse el boton: la decision, la retro y la planificacion salen sobre el
        /// fondo del juego, como las ceremonias, y no sobre el panel del dia. Se difiere un fotograma para no apilar una
        /// pantalla en mitad de pintar esta; si para entonces el paso ya paso, no se abre.
        /// </summary>
        private void AbrirSola(Func<bool> sigueEnPie, Action abrir) {
            if (_escenaApilada || !isActiveAndEnabled) return;
            StartCoroutine(AbrirEnElSiguienteFotograma(sigueEnPie, abrir));
        }

        private System.Collections.IEnumerator AbrirEnElSiguienteFotograma(Func<bool> sigueEnPie, Action abrir) {
            yield return null;
            if (this != null && isActiveAndEnabled && !_escenaApilada && sigueEnPie()) abrir();
        }

        private void ProcesoConTarjeta(TarjetaDef tarjeta, string etiqueta, Action<OpcionTarjeta> alCerrar) {
            OpcionTarjeta elegida = null;
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<PantallaDeTarjetas>(p => {
                p.Sesion = S;
                p.Pizarra = true;   // las ceremonias, en la pizarra de post-its (2026-11-05)
                p.Etiqueta = etiqueta;
                p.Tarjetas = new List<TarjetaDef> { tarjeta };
                p.ResuelveOtro = true;
                p.AlElegir = (_, opcion) => elegida = opcion;
                p.AlTerminar = () => {
                    App.Router.Volver();
                    _escenaApilada = false;
                    if (elegida != null) alCerrar(elegida);
                };
            });
        }

        private void Resolver(string opcionId) {
            var antes = S.W.Clone();
            S.ResolverDecision(opcionId);
            _decidido = S.Traza.Entradas.LastOrDefault();
            _decisionesDeHoy++;

            var cambios = new Dictionary<string, double>();
            foreach (var nombre in Nexus.Core.Modelo.WorldState.Nombres) {
                double a, d;
                if (antes.TryGet(nombre, out a) && S.W.TryGet(nombre, out d) && Math.Abs(d - a) >= 0.05 && nombre != "VelocidadMod")
                    cambios[nombre] = d - a;
            }
            _cambiosDeLaDecision = Textos.Previsualizar(cambios);
            if (_decidido != null) Anotar($"{S.HoraActual} · Decidiste en «{_decidido.Titulo}».");
            GuiaView.Hecho(App, "decidir");
            GuiaView.Avisar(App, "decision.hecha");
        }

        private void Recoger(string id) {
            S.RecogerColeccionable(id);
            App.AnotarColeccionable(id);
            var col = App.Catalogo.Coleccionables.FirstOrDefault(x => x.Id == id);
            if (col == null) return;
            Anotar($"Encontraste {PantallaDelDiario.NombreDeSerie(col.Serie, true)}: «{col.Titulo}».");
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<EscenaDeHallazgo>(p => {
                p.Coleccionable = col;
                p.AlCerrar = () => _escenaApilada = false;
            });
            GuiaView.Hecho(App, "recoger");
            GuiaView.Avisar(App, "coleccionable");
        }

        private TMP_Text _textoMas, _textoVelocidad;

        /// <summary>La frase de siempre o, si se trabaja solo (sin hojas de vida), la contada en primera persona.</summary>
        private string Solo(string conEquipo, string trabajandoSolo) { return S.TrabajasSolo ? trabajandoSolo : conEquipo; }

        /// <summary>
        /// La carpeta del cliente, en un pizarron: quien es y que le duele, los papeles que han ido llegando (cada uno
        /// dibujado como lo que es) y lo que dicen hoy del producto quienes lo van a usar.
        /// </summary>
        private void AbrirCarpetaDelCliente() {
            var ficha = S.Perfil.Proyecto;
            if (ficha == null) return;
            var paginas = new List<Nexus.Core.Narrativa.PaginaDePizarra>();
            var dibujos = new Dictionary<Nexus.Core.Narrativa.PaginaDePizarra, Action<Nexus.Unity.Pantallas.Minijuegos.Tizador, float>>();

            foreach (var papel in S.PapelesDelCliente()) {
                var p = papel;
                var nuevo = !S.R.PapelesLeidos.Contains(p.Id);
                var pagina = new Nexus.Core.Narrativa.PaginaDePizarra {
                    Titulo = p.Titulo,
                    Texto = $"{(nuevo ? "NUEVO · " : "")}De: {p.De} · llegó el día {p.Dia}\n{p.QueTeDice}"
                };
                // El papel, dibujado debajo del texto, tan grande como quepa.
                dibujos[pagina] = (t, y) => {
                    var k = Mathf.Min(1.5f, (800 - (y + 30)) / DibujosDelProducto.Alto);
                    DibujosDelProducto.Papel(t.En((MarcoDePizarra.W - DibujosDelProducto.Ancho * k) / 2, y + 30, k), p);
                };
                paginas.Add(pagina);
            }
            // Quien es el cliente y lo que dicen sus usuarios, dibujado: tres cuadros y una fila de caras, no parrafos.
            var cliente = new Nexus.Core.Narrativa.PaginaDePizarra { Titulo = "Quién es tu cliente" };
            dibujos[cliente] = (t, y) => DibujosDelProducto.Cliente(t, ficha, y);
            paginas.Add(cliente);
            var voces = S.VocesDelProducto();
            if (voces.Count > 0) {
                var modulos = S.Producto();
                var dicen = new Nexus.Core.Narrativa.PaginaDePizarra {
                    Titulo = "Lo que dicen hoy del producto",
                    Texto = "Cada uno mira su parte del sistema, tal como está hoy."
                };
                dibujos[dicen] = (t, y) => DibujosDelProducto.Voces(t, voces, modulos, y);
                paginas.Add(dicen);
            }

            S.MarcarPapelesLeidos();
            App.Router.Apilar<PantallaDePizarra>(p => {
                p.Etiqueta = "EL CLIENTE";
                p.Tema = "La carpeta del cliente · " + ficha.Cliente.Nombre;
                p.Paginas = paginas;
                p.Dibujos = dibujos;
            });
        }

        private void AbrirDiario() {
            Runner.Pausado = true;
            App.Router.Apilar<PantallaDelDiario>(p => p.AlCerrar = () => Runner.Pausado = false);
        }

        // ==================================================================== lo que avisa el runner

        private void AlSonar(Alerta alerta) {
            Anotar($"{RelojDeJornada.Formatear(alerta.MinutoDeLaAlerta)} · Aviso: {alerta.Texto}");
            // Primero «estás lejos» (que para el reloj), y despues «pulsa Atender», que espera a que se haga.
            var ancla = Ancla();
            if (ancla != null && S.ZonaActual != ancla.Id) GuiaView.Avisar(App, "alerta.lejos");
            GuiaView.Avisar(App, "alerta.suena");
        }

        private void AlExpirar(Alerta alerta) {
            AnotarNovedad($"{RelojDeJornada.Formatear(alerta.MinutoDeExpiracion)} · Un aviso caducó sin que nadie lo atendiera. Alguien decidió por ti.");
            // La guia podia estar esperando «Atender»: ya no hay nada que atender, asi que no se queda colgada.
            if (AlertasSonando().Count == 0) GuiaView.Cancelar("atender");
        }

        private void AlCierre() {
            Anotar($"{S.HoraActual} · Cierre de la jornada.");
        }

        private void AlFinDelDia() {
            Anotar($"Fin del día {S.R.DiaActual}.");
            App.Guardar(AutoGuardado.Motivos.FinDeJornada);
        }

        // ==================================================================== ayudas

        private List<Alerta> AlertasSonando() {
            var ahora = S.MinutoDelDia;
            return S.AlertasDeHoy.Where(a => a.EstaPendiente && a.YaSono(ahora)).ToList();
        }

        private static string NombreDeEstadistica(string stock) {
            switch (stock) {
                case "DeudaTecnica": return "deuda técnica";
                case "Cobertura": return "cobertura de pruebas";
                case "Cansancio": return "cansancio";
                case "MoralEquipo": return "moral del equipo";
                case "SatisfaccionCliente": return "satisfacción del cliente";
                default: return stock;
            }
        }

        private ZonaDeNivel Ancla() {
            var mapa = S.Perfil.Mapa;
            return mapa != null && !mapa.Vacio ? mapa.Ancla : null;
        }

        private string NombreDeOrigen(string origen) {
            var ev = App.Catalogo.Eventos.FirstOrDefault(e => e.Id == origen);
            if (ev != null) return "«" + ev.Nombre + "»";
            if (origen == Nexus.Core.Sesion.GameSession.OrigenDeDespliegue) return "arreglar lo que salió mal en el último despliegue";
            return origen;
        }

        private void Anotar(string linea) {
            S.R.Anotar(linea, false);   // lo que hizo el jugador: se guarda, pero no cuenta como novedad en la M
        }

        /// <summary>Algo que paso sin que el jugador lo hiciera (lo que trae el dia, un aviso que caduco): novedad en la M.</summary>
        private void AnotarNovedad(string linea) {
            S.R.Anotar(linea, true);
        }
    }
}
