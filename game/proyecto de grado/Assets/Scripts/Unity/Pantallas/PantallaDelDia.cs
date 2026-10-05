using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Guardado;
using Nexus.Core.Jornada;
using Nexus.Core.Minijuegos;
using Nexus.Core.Sesion;
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
        private TMP_Text _titulo, _etiqueta, _riesgo, _satisfaccion, _textoAvisos;
        private Button _pausa;
        private readonly TMP_Text[] _valores = new TMP_Text[5];
        private readonly TMP_Text[] _efectos = new TMP_Text[5];
        private TMP_Text _pronosticoTitulo, _pronosticoDetalle;
        private BarraView _pronosticoBarra;
        /// <summary>Por modulo del proyecto: el texto del porcentaje y su barra.</summary>
        private readonly List<KeyValuePair<TMP_Text, BarraView>> _modulos = new List<KeyValuePair<TMP_Text, BarraView>>();
        private float _siguientePronostico;
        private readonly BarraView[] _barras = new BarraView[5];
        private readonly List<KeyValuePair<Alerta, TMP_Text>> _cuentasAtras = new List<KeyValuePair<Alerta, TMP_Text>>();
        private string _claveAhora, _claveRiel;
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
        private readonly List<string> _entradas = new List<string>();

        /// <summary>Los cinco medidores del proyecto: nombre corto (el panel), nombre largo (el detalle) y su entrada del glosario.</summary>
        private static readonly string[] MedidoresCortos = { "Avance", "Deuda", "Moral", "Cobertura", "Cansancio" };
        private static readonly string[] MedidoresLargos = { "Avance", "Deuda técnica", "Moral del equipo", "Cobertura de pruebas", "Cansancio" };
        private static readonly string[] MedidoresEnGlosario = { "avance", "deuda-tecnica", "moral", "cobertura", "cansancio" };

        public override bool PuedeVolver { get { return false; } }

        // ==================================================================== construir

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.espacio, Tema.margen * 0.75f));
            ConstruirHud(marco);
            ConstruirRiel(marco);

            var cuerpo = Ui.Fila(marco, "Cuerpo", Tema.margen * 0.75f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);

            var centro = Ui.PanelColumna(cuerpo, "Ahora", Tema.margen, Tema.espacio);
            UiKit.Tamano(centro, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(centro, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            _ahora = new Hoja(Ui, contenido);
            GuiaView.Registrar("dia.ahora", centro);

            ConstruirProyecto(cuerpo);

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

        /// <summary>El HUD: que dia es y donde estas, el reloj, y el control del tiempo. Una sola franja.</summary>
        private void ConstruirHud(Transform padre) {
            var hud = Ui.Fila(padre, "HUD");
            var titulos = Ui.Columna(hud, espacio: Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            _etiqueta = Ui.Texto(titulos, "", EstiloTexto.Leyenda, Tema.cyan);
            _titulo = Ui.Texto(titulos, "", EstiloTexto.Subtitulo, Tema.ink);

            GuiaView.Registrar("dia.reloj", RelojView.CrearCompacto(Ui, hud, Runner));

            _pausa = Ui.Boton(hud, "Pausa", () => Runner.Pausado = !Runner.Pausado);
            UiKit.Tamano(_pausa, ancho: 130);
            foreach (var v in new[] { 1, 2, 4 }) {
                var velocidad = v;
                UiKit.Tamano(Ui.Boton(hud, "×" + v, () => Runner.Velocidad = velocidad, VarianteBoton.Fantasma), ancho: 64);
            }
            GuiaView.Registrar("dia.velocidad", _pausa);
            Ui.Boton(hud, "Menú", () => App.Router.Apilar<PantallaDePausa>());
        }

        /// <summary>
        /// El riel: un boton por cada cosa que ya no esta siempre en pantalla. Tambien es a donde señala la guia
        /// cuando habla de algo que vive dentro de un panel cerrado (RegistrarRespaldo).
        /// </summary>
        private void ConstruirRiel(Transform padre) {
            var riel = Ui.Fila(padre, "Riel", Tema.Espacio(2));
            var mapa = Ui.Boton(riel, "Mapa", AbrirMapa);
            var equipo = Ui.Boton(riel, "Equipo", AbrirEquipo);
            var escritorio = Ui.Boton(riel, "Escritorio", AbrirEscritorio);
            var tablero = Ui.Boton(riel, "Tablero", () => AbrirTablero(0));
            Ui.Boton(riel, "Monitoreo", () => AbrirMonitoreo(0));
            var ceremonias = Ui.Boton(riel, "Ceremonias", () => AbrirCeremonias(0));
            GuiaView.Registrar("dia.ceremonias", ceremonias);
            GuiaView.RegistrarRespaldo("dia.tablero", tablero);
            var avisos = Ui.Boton(riel, "Avisos", () => AbrirAvisos(0));
            UiKit.Tamano(avisos, ancho: 260);   // su rotulo cambia («Avisos · 2 esperando»): ancho fijo para que quepa
            _textoAvisos = avisos.GetComponentInChildren<TMP_Text>();
            var diario = Ui.Boton(riel, "Diario", AbrirDiario);
            Ui.Resorte(riel);
            GuiaView.BotonDeAyuda(App, riel);

            GuiaView.Registrar("dia.diario", diario);
            GuiaView.RegistrarRespaldo("dia.mapa", mapa);
            GuiaView.RegistrarRespaldo("dia.aqui", mapa);
            GuiaView.RegistrarRespaldo("mapa.", mapa);
            GuiaView.RegistrarRespaldo("dia.equipo", equipo);
            GuiaView.RegistrarRespaldo("dia.oficina", escritorio);
        }

        /// <summary>
        /// «El proyecto»: lo que se esta construyendo y como va, de un vistazo. Una linea por medidor (el efecto de
        /// cada uno solo aparece cuando ya esta haciendo daño); el detalle completo, con sus «?», esta en Monitoreo.
        /// </summary>
        private void ConstruirProyecto(Transform padre) {
            var panel = Ui.PanelColumna(padre, "Proyecto", Tema.margen * 0.75f, Tema.espacio);
            UiKit.Tamano(panel, ancho: 640, flexAlto: 1);
            GuiaView.Registrar("dia.proyecto", panel);

            var ficha = S.Perfil.Proyecto;
            var cabecera = Ui.Fila(panel, "Cabecera");
            UiKit.Tamano(Ui.Texto(cabecera, "EL PROYECTO" + (ficha != null ? " · " + ficha.Nombre.ToUpperInvariant() : ""),
                                  EstiloTexto.Leyenda, Tema.cyan), flexAncho: 1);
            Ui.Boton(cabecera, "Ver el detalle", () => AbrirMonitoreo(0), VarianteBoton.Fantasma);

            RectTransform contenido;
            var scroll = Ui.Desplazable(panel, out contenido, "Proyecto (scroll)");
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            // Lo primero: como saldria el lanzamiento si se entregara al ritmo de hoy. Es lo que da peso a cada barra.
            var pronostico = Ui.Columna(contenido, "Pronostico", Tema.Espacio(1));
            GuiaView.Registrar("dia.pronostico", pronostico);
            _pronosticoTitulo = Ui.Texto(pronostico, "", EstiloTexto.Cuerpo, Tema.ink);
            _pronosticoBarra = Ui.Barra(pronostico, 0, Tema.cyan);
            _pronosticoDetalle = Ui.Texto(pronostico, "", EstiloTexto.Pequeno, Tema.inkMuted);
            Ui.Separador(contenido);

            var colores = new[] { Tema.cyan, Tema.warning, Tema.cyan, Tema.cyan, Tema.warning };
            for (var i = 0; i < MedidoresCortos.Length; i++) {
                var fila = Ui.Fila(contenido, "Medidor", Tema.Espacio(3));
                UiKit.Tamano(Ui.Texto(fila, MedidoresCortos[i], EstiloTexto.Pequeno, Tema.ink), ancho: 120);
                _barras[i] = Ui.Barra(fila, 0, colores[i]);
                _valores[i] = Ui.Texto(fila, "", EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.Right);
                UiKit.Tamano(_valores[i], ancho: 130);
                _efectos[i] = Ui.Texto(contenido, "", EstiloTexto.Leyenda, Tema.danger);
                _efectos[i].gameObject.SetActive(false);
            }
            _riesgo = Ui.Texto(contenido, "", EstiloTexto.Pequeno);
            _satisfaccion = Ui.Texto(contenido, "", EstiloTexto.Pequeno);

            // Lo que el equipo tiene entre manos ahora mismo: el tablero, en pequeño.
            Ui.Separador(contenido);
            ConstruirTableroDelDia(contenido);

            // El avance, parte por parte del sistema: que se construye de verdad cada dia (ronda 4).
            if (ficha != null && ficha.Modulos.Count > 0) {
                Ui.Separador(contenido);
                var modulos = Ui.Columna(contenido, "Modulos", Tema.Espacio(2));
                GuiaView.Registrar("dia.modulos", modulos);
                Ui.Texto(modulos, "LAS PARTES DEL SISTEMA", EstiloTexto.Leyenda, Tema.cyan);
                foreach (var m in ficha.Modulos) {
                    var fila = Ui.Fila(modulos, "Modulo", Tema.Espacio(3));
                    UiKit.Tamano(Ui.Texto(fila, m.Nombre, EstiloTexto.Pequeno, Tema.ink), ancho: 220);
                    var barra = Ui.Barra(fila, 0, Tema.cyan);
                    var pct = Ui.Texto(fila, "", EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.Right);
                    UiKit.Tamano(pct, ancho: 70);
                    _modulos.Add(new KeyValuePair<TMP_Text, BarraView>(pct, barra));
                }
            }
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
                        $"{S.SePuedeCerrarLaJornada}|{QuedaAlgunAviso()}|{_compromiso}|{Runner.PuedeMoverse}|" +
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

            PintarCabecera();
            PintarRiel();
            PintarProyecto();
            PintarTableroDelDia();
        }

        private void PintarCabecera() {
            var brief = S.BriefDeHoy;
            _etiqueta.text = S.Perfil.Nombre.ToUpperInvariant() + " · " + S.Metodologia.Nombre.ToUpperInvariant();
            var zona = ZonaDondeEstoy();
            _titulo.text = (S.R.DiaActual == 0
                ? "Antes del día 1"
                : $"Día {S.R.DiaActual} de {S.Perfil.DiasTotales}" + (brief != null ? " · " + brief.EtiquetaUnidad : "")) +
                (zona != null ? "  ·  estás en: " + zona.Nombre : "");
            _pausa.GetComponentInChildren<TMP_Text>().text = Runner.Pausado ? "Seguir" : "Pausa";
        }

        /// <summary>El boton «Avisos» dice cuantos esperan: es lo unico del riel que cambia solo.</summary>
        private void PintarRiel() {
            var sonando = AlertasSonando().Count;
            var deHoy = AvisosDeHoy();
            var clave = sonando + "|" + deHoy;
            if (clave == _claveRiel) return;
            _claveRiel = clave;
            _textoAvisos.text = sonando > 0 ? $"Avisos · {sonando} esperando" : deHoy > 0 ? $"Avisos · {deHoy} de hoy" : "Avisos";
        }

        /// <summary>Lo que hoy trae de serie para leer: la consecuencia del estado y los anuncios de lo que viene.</summary>
        private int AvisosDeHoy() {
            var brief = S.BriefDeHoy;
            if (brief == null) return 0;
            return brief.Avisos.Count + (brief.Incidencia != null ? 1 : 0);
        }

        private ZonaDeNivel ZonaDondeEstoy() {
            var mapa = S.Perfil.Mapa;
            return mapa == null || mapa.Vacio ? null : mapa.PorId(S.ZonaActual);
        }

        private void PintarProyecto() {
            var w = S.W;
            _valores[0].text = $"{w.Avance:0} / {w.Alcance:0} pts";
            _barras[0].Valor = w.Alcance > 0 ? (float)(w.Avance / w.Alcance) : 0;
            _valores[1].text = $"{w.DeudaTecnica:0} / 100";
            _barras[1].Valor = (float)w.DeudaTecnica / 100f;
            _valores[2].text = $"{w.MoralEquipo:0} / 100";
            _barras[2].Valor = (float)w.MoralEquipo / 100f;
            _valores[3].text = $"{w.Cobertura:0} %";
            _barras[3].Valor = (float)w.Cobertura / 100f;
            _valores[4].text = $"{w.Cansancio:0} / 100";
            _barras[4].Valor = (float)w.Cansancio / 100f;
            var d = S.BriefDeHoy != null ? S.BriefDeHoy.Derivadas : null;
            _riesgo.text = d == null ? "" : $"Riesgo latente al empezar el día: {d.RiesgoLatente:0} / 100";
            _satisfaccion.text = $"Satisfacción del cliente: {w.SatisfaccionCliente:0} / 100" +
                                 (w.SatisfaccionCliente <= 35 ? $" · <color={NexusTheme.Html(Tema.danger)}>tan baja que pedirán explicaciones</color>" : "");

            // Debajo de un medidor solo se escribe cuando ya esta haciendo daño: asi el rojo significa algo.
            bool[] malos;
            var efectos = EfectosDeLosMedidores(out malos);
            for (var i = 0; i < _efectos.Length; i++) {
                var go = _efectos[i].gameObject;
                if (go.activeSelf != malos[i]) go.SetActive(malos[i]);
                if (malos[i]) _efectos[i].text = efectos[i];
            }

            if (_modulos.Count > 0) {
                var porModulo = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(S.Perfil.Proyecto, w.Avance, w.Alcance);
                for (var i = 0; i < _modulos.Count && i < porModulo.Count; i++) {
                    _modulos[i].Key.text = $"{porModulo[i].Value:0} %";
                    _modulos[i].Value.Valor = (float)(porModulo[i].Value / 100.0);
                }
            }
            if (Time.unscaledTime >= _siguientePronostico) {
                _siguientePronostico = Time.unscaledTime + 1f;
                PintarPronostico();
            }
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

        private void PintarPronostico() {
            if (S.R.Fase != 2) return;
            var c = S.Pronostico();
            var color = NexusTheme.Html(ColorDelNivel(c.Nivel));
            _pronosticoTitulo.text = $"Si sigues así: <color={color}><b>{Nexus.Core.Evaluacion.NivelesDeLanzamiento.Titulo(c.Nivel).ToLowerInvariant()}</b></color> ({c.Puntaje:0}/100)";
            _pronosticoBarra.Valor = (float)(c.Puntaje / 100.0);
            var peor = c.Factores.OrderBy(f => f.Maximo > 0 ? f.Puntos / f.Maximo : 1).First();
            _pronosticoDetalle.text = $"Lo que más te resta: {peor.Nombre.ToLowerInvariant()} ({peor.Valor}).";
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
            _ahora.Parrafo($"Cada día empieza a las {j.HoraInicio:00}:00 y la jornada acaba a las {j.HoraCierre:00}:00. El reloj corre solo: " +
                           "arriba puedes pausarlo o acelerarlo.");
            _ahora.Parrafo("En cualquier momento puede llegar un aviso. Tienes unas horas para atenderlo, y solo se atiende desde tu " +
                           "escritorio. Si lo dejas caducar, alguien decide por ti, y casi nunca bien.");
            _ahora.Parrafo("Mientras tanto, el edificio es tuyo: con «Mapa», arriba, vas a otras zonas, y cada una tiene algo que no " +
                           "está en ninguna otra. Pero ir cuesta tiempo, y estar lejos cuando suena un aviso también.");
            _ahora.Accion("Empezar el día 1", EmpezarDia);
        }

        private void Retro() {
            var retro = S.PendingRetro;
            var ceremonia = S.Reglas == null ? null : S.Reglas.CeremoniaPorId(retro.CeremoniaId);
            _ahora.Etiqueta("Ceremonia · " + (ceremonia != null && !string.IsNullOrEmpty(ceremonia.Nombre) ? ceremonia.Nombre : "retrospectiva"));
            _ahora.Titulo("¿Qué cambiamos del proceso?");
            if (!string.IsNullOrEmpty(retro.Texto)) _ahora.Parrafo(retro.Texto);
            _ahora.Nota("Lo que elijas cambia cómo trabaja el equipo desde hoy. El reloj espera.");
            foreach (var accion in retro.Acciones) {
                var a = accion;
                _ahora.Opcion(a.Texto, null, () => {
                    S.ElegirAccionRetro(a.Id);
                    Anotar($"Retrospectiva: «{a.Texto}»");
                });
            }
        }

        private void Planificacion() {
            var plan = S.PendingPlanning;
            if (_diaDelCompromiso != S.R.DiaActual) {
                _diaDelCompromiso = S.R.DiaActual;
                _compromiso = Math.Round(plan.CapacidadSugerida);
            }
            _ahora.Etiqueta("Ceremonia · planificación de " + plan.Unidad);
            _ahora.Titulo("¿Con cuánto te comprometes?");
            _ahora.Parrafo($"El equipo calcula que puede con unos {plan.CapacidadSugerida:0.#} puntos. Quedan {plan.PuntosPendientes:0} puntos por hacer.");
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
                Ui.Texto(entra, "Entran en el orden del backlog: lo de arriba, primero. El resto espera en el product backlog.", EstiloTexto.Pequeno);
            }
            _ahora.Nota("Prometer más de lo que cabe no da error: genera sobrecompromiso, y el sobrecompromiso genera deuda técnica cada día hasta que se cierra.");

            var fila = _ahora.Fila();
            UiKit.Tamano(Ui.Boton(fila, "−", () => _compromiso = Math.Max(0, _compromiso - 1)), ancho: 64);
            Ui.Texto(fila, $"{_compromiso:0} puntos", EstiloTexto.Subtitulo,
                     _compromiso > plan.CapacidadSugerida + 0.5 ? Tema.warning : Tema.success, TextAlignmentOptions.Center);
            UiKit.Tamano(Ui.Boton(fila, "+", () => _compromiso += 1), ancho: 64);

            _ahora.Accion($"Comprometerse a {_compromiso:0} puntos", () => {
                S.Comprometer(_compromiso);
                Anotar($"Planificación: el equipo se compromete a {_compromiso:0} puntos.");
            });
        }

        private void Jornada() {
            var porSonar = QuedaAlgunAviso();
            _ahora.Etiqueta("La jornada corre");
            _ahora.Titulo(S.HoraActual.StartsWith("0") ? "Buenos días" : "Trabajando");
            _ahora.Parrafo(porSonar
                ? "Hoy va a llegar algún aviso, y no sabes cuándo. Mientras tanto puedes recorrer el mapa, hablar con la gente o adelantar trabajo en tu escritorio."
                : "Hoy ya no queda ningún aviso por llegar. Puedes explorar, adelantar trabajo, o dar la jornada por terminada.");

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
                var n = Ui.Notificacion(_ahora.Raiz, "Un compañero ha dejado un cambio de código para que lo revises.", Tono.Cyan);
                Ui.Boton(n, "Revisar", AbrirRevision, VarianteBoton.Fantasma);
            }
            if (S.TareasDeOficina.Count > 0) GuiaView.Avisar(App, "oficina.disponible");

            _ahora.Espacio();
            var botones = _ahora.Fila();
            if (S.TareasDeOficina.Count > 0) Ui.Boton(botones, "Trabajar en tu escritorio", AbrirEscritorio);
            if (porSonar) Ui.Boton(botones, "Esperar al siguiente aviso", () => Runner.AdelantarHastaElSiguienteAviso());
            var cerrar = Ui.Boton(botones, "Cerrar la jornada", () => { Runner.CerrarJornada(); GuiaView.Hecho(App, "cerrar-jornada"); },
                                  porSonar ? VarianteBoton.Secundario : VarianteBoton.Primario);
            cerrar.interactable = S.SePuedeCerrarLaJornada;
            if (!S.SePuedeCerrarLaJornada)
                _ahora.Nota("«Cerrar la jornada» salta al final del día, y solo se puede cuando no queda nada pendiente: saltar nunca te ahorra una consecuencia.");
        }

        private void EmpezarTarea(Nexus.Core.Oficina.TareaDeOficina tarea) {
            CerrarPanel();   // el reto se abre sobre el dia, no sobre el panel del escritorio
            if (!Runner.EmpezarTarea(tarea.Id)) return;
            Anotar($"{S.HoraActual} · Trabajo: «{tarea.Titulo}».");
            var pendiente = new PendingMinigame {
                MinijuegoId = tarea.Minijuego, Archivo = tarea.Archivo, NivelAndamiaje = S.Perfil.NivelAndamiaje
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
            if (!string.IsNullOrEmpty(d.TextoAviso)) _ahora.Parrafo(d.TextoAviso);
            _ahora.Nota("El reloj está parado mientras decides. Debajo de cada opción, lo que cambia en el momento; lo que llega días después no se ve.");
            foreach (var opcion in d.Opciones) {
                var o = opcion;
                var detalle = o.Bloqueada ? "No disponible: " + o.MotivoBloqueo : Textos.Previsualizar(o.Previsualizacion);
                if (!string.IsNullOrEmpty(o.NotaDeMetodologia) && !o.Bloqueada) detalle += "   ·   " + o.NotaDeMetodologia;
                var boton = _ahora.Opcion(o.Texto, detalle, () => Resolver(o.Id));
                boton.interactable = !o.Bloqueada;
            }
        }

        private void Decidido() {
            _ahora.Etiqueta("Decidido");
            _ahora.Titulo(_decidido.Titulo);
            _ahora.Parrafo("Elegiste: " + _decidido.OpcionTexto);
            _ahora.Parrafo("En el momento: " + _cambiosDeLaDecision);
            _ahora.Nota("Si la decisión tiene consecuencias, llegarán en los próximos días. Si estuvo bien o mal, y por qué, lo leerás en el Dashboard de Lecciones al cerrar el nivel.");
            _ahora.Accion("Volver a la jornada", () => { _decidido = null; });
        }

        private void Cierre() {
            var j = S.Perfil.Jornada;
            _ahora.Etiqueta("Cierre de la jornada");
            _ahora.Titulo($"Son las {S.HoraActual}");
            _ahora.Parrafo("Irte a casa: descansas, y el equipo también. Quedarte hasta las " + $"{j.HoraLimite:00}:00" +
                           ": hoy avanzas un 25 % más, pero lo pagas en cansancio, salud y deuda técnica, y se acumula.");
            _ahora.Parrafo("Si te quedas, hasta esa hora no llegan avisos: es tiempo libre para recorrer el mapa.");
            var botones = _ahora.Fila();
            Ui.Boton(botones, "Irme a casa", () => { Runner.Irse(); Anotar("Te fuiste a casa."); GuiaView.Hecho(App, "irse"); }, VarianteBoton.Primario);
            // Quedarse hace imposible el «Irme a casa» que pudiera estar pidiendo la guia: se cancela, no se cuelga.
            Ui.Boton(botones, "Quedarme", () => { Runner.Quedarse(); Anotar("Te quedaste haciendo horas extra."); GuiaView.Cancelar("irse"); }, VarianteBoton.Peligro);
        }

        private void Prorroga() {
            _ahora.Etiqueta("Horas extra");
            _ahora.Titulo("Te has quedado");
            _ahora.Parrafo("El día ya contó con las horas extra. Hasta la hora límite no llegan avisos: abre el «Mapa», habla con quien quede y busca lo que no has encontrado.");
            _ahora.Accion("Irme ya", () => { Runner.TerminarLaProrroga(); GuiaView.Hecho(App, "irse"); });
        }

        private void Resumen() {
            var w = S.W;
            _ahora.Etiqueta("Fin del día");
            _ahora.Titulo($"Día {S.R.DiaActual} de {S.Perfil.DiasTotales}, terminado");
            if (_tengoElInicioDelDia) _ahora.Parrafo($"Hoy: avance {Textos.Cambio(w.Avance - _alEmpezarElDia[0])} · deuda {Textos.Cambio(w.DeudaTecnica - _alEmpezarElDia[1])} · " +
                           $"moral {Textos.Cambio(w.MoralEquipo - _alEmpezarElDia[2])} · cobertura {Textos.Cambio(w.Cobertura - _alEmpezarElDia[3])} · " +
                           $"cansancio {Textos.Cambio(w.Cansancio - _alEmpezarElDia[4])}.");
            if (_tengoElInicioDelDia) PintarPorQueAvanzaste(w.Avance - _alEmpezarElDia[0]);
            if (_tengoElInicioDelDia) PintarQueSeConstruyo(_alEmpezarElDia[0], w.Avance, w.Alcance);
            if (_tengoElInicioDelDia) _ahora.Parrafo(_decisionesDeHoy == 0 ? "Hoy no tomaste ninguna decisión." :
                           _decisionesDeHoy == 1 ? "Hoy tomaste 1 decisión." : $"Hoy tomaste {_decisionesDeHoy} decisiones.");
            _ahora.Nota("La partida se ha guardado.");
            _ahora.Accion($"Empezar el día {S.R.DiaActual + 1}", EmpezarDia);
        }

        /// <summary>Que partes del sistema avanzaron hoy, y cuanto: el avance en puntos traducido a lo que se construye.</summary>
        private void PintarQueSeConstruyo(double avanceAntes, double avanceAhora, double alcance) {
            var f = S.Perfil.Proyecto;
            if (f == null || f.Modulos.Count == 0) return;
            var antes = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(f, avanceAntes, alcance);
            var ahora = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(f, avanceAhora, alcance);
            var cambios = Enumerable.Range(0, ahora.Count).Where(i => ahora[i].Value - antes[i].Value >= 0.5)
                .Select(i => $"{ahora[i].Key.Nombre} ({antes[i].Value:0} → {ahora[i].Value:0} %)").ToList();
            _ahora.Parrafo(cambios.Count == 0 ? "Hoy no avanzó ninguna parte del sistema."
                                              : "Hoy se construyó: " + string.Join(" · ", cambios) + ".", Tema.cyan);
        }

        /// <summary>
        /// Por que el equipo avanzo lo que avanzo hoy: la velocidad base por cada barra que la frena o la empuja.
        /// Es ForresterModel en palabras: deja ver que la deuda, el cansancio y la moral SI cuentan cada dia.
        /// </summary>
        private void PintarPorQueAvanzaste(double avanzado) {
            var t = _ahora.Tarjeta("Por qué avanzaste " + avanzado.ToString("0.#") + " puntos");
            var m = Nexus.Core.Simulacion.ForresterModel.FMoral(_alEmpezarElDia[2]);
            var f = Nexus.Core.Simulacion.ForresterModel.FFatiga(_alEmpezarElDia[4]);
            var d = Nexus.Core.Simulacion.ForresterModel.FDeuda(_alEmpezarElDia[1]);
            var c = Nexus.Core.Simulacion.ForresterModel.FComp(S.W.Competencia);
            Ui.Texto(t, $"Un equipo en perfectas condiciones haría unos {S.Perfil.VelocidadBase:0.#} puntos al día. Hoy:", EstiloTexto.Pequeno, Tema.ink);
            Factor(t, "Moral del equipo", _alEmpezarElDia[2], m);
            Factor(t, "Cansancio", _alEmpezarElDia[4], f);
            Factor(t, "Deuda técnica", _alEmpezarElDia[1], d);
            Factor(t, "Competencia del equipo", S.W.Competencia, c);
            Ui.Texto(t, "Si te quedaste, las horas extra sumaron un 25 %, pero subieron el cansancio para mañana.", EstiloTexto.Pequeno, Tema.inkMuted);
        }

        private void Factor(Transform padre, string nombre, double valor, double mult) {
            var pct = (mult - 1) * 100;
            var color = pct < -5 ? Tema.danger : pct > 0.5 ? Tema.success : Tema.ink;
            Ui.Texto(padre, $"· {nombre} ({valor:0}): {(pct >= 0 ? "+" : "")}{pct:0} %", EstiloTexto.Pequeno, color);
        }

        private void FinDelDesarrollo() {
            _ahora.Etiqueta("Fase 3 · el lanzamiento");
            _ahora.Titulo("Se acabaron los días");
            _ahora.Parrafo("El desarrollo ha terminado. Ahora el cliente ve lo que has hecho, y el riesgo que acumulaste durante todos " +
                           "estos días decide si sale bien. Después, el nivel se cierra y se evalúa.");
            _ahora.Accion("Ir al lanzamiento", () => FlujoDelNivel.Lanzar(App));
        }

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
            var dia = brief.Dia;
            Anotar($"— Día {brief.Dia} · {brief.EtiquetaUnidad} —");
            if (brief.Ceremonias.Count > 0) Anotar("Ceremonias: " + string.Join(", ", brief.Ceremonias));
            if (brief.Incidencia != null)
            {
                Anotar($"Consecuencia de tu {NombreDeEstadistica(brief.Incidencia.Estadistica)} ({brief.Incidencia.Valor:0}): {brief.Incidencia.Titulo}.");
                GuiaView.Avisar(App, "incidencia");
            }
            foreach (var origen in brief.EfectosQueVencieronHoy.Distinct())
                Anotar("Hoy se cobra algo de antes: " + NombreDeOrigen(origen) + ".");

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

            _escenaApilada = true;
            Runner.EnEscena = true;
            Action<ResultadoMinijuego> alTerminar = resultado => {
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
                    App.Router.Apilar<PantallaDetectar>(p => { p.Def = def; p.Pendiente = pendiente; p.Practica = practica; p.AlTerminar = alTerminar; });
                    break;
            }
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
            App.Router.Apilar<PantallaDeColeccionable>(p => {
                p.Coleccionable = col;
                p.AlCerrar = () => _escenaApilada = false;
            });
            GuiaView.Hecho(App, "recoger");
            GuiaView.Avisar(App, "coleccionable");
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
            Anotar($"{RelojDeJornada.Formatear(alerta.MinutoDeExpiracion)} · Un aviso caducó sin que nadie lo atendiera. Alguien decidió por ti.");
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

        private bool QuedaAlgunAviso() {
            if (Runner.Estado != EstadoDelDia.Corriendo) return false;
            var ahora = S.MinutoDelDia;
            return S.AlertasDeHoy.Any(a => a.EstaPendiente && a.MinutoDeLaAlerta > ahora);
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
            return origen;
        }

        private void Anotar(string linea) {
            _entradas.Insert(0, linea);
            if (_entradas.Count > 60) _entradas.RemoveAt(_entradas.Count - 1);
        }
    }
}
