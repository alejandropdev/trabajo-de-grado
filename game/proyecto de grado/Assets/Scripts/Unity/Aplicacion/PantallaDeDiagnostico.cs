using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Jornada;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Unity.Juego;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// La prueba del motor: un recorrido guiado por un nivel real (el primero del catalogo), del dia 1 al
    /// cierre, sin guardar nada. No es una pantalla del juego: es la forma de VER el flujo antes de que existan
    /// las pantallas de verdad.
    ///
    /// Se organiza alrededor de una sola pregunta: ¿que pasa ahora y que puedo hacer? La tarjeta central
    /// «Ahora» siempre la responde, con el boton para hacerlo. Arriba, la franja del dia marca en que momento
    /// del dia estas; a la izquierda, el reloj y el estado del proyecto; a la derecha, lo que ya ha pasado.
    /// </summary>
    public sealed class PantallaDeDiagnostico : Pantalla {
        private enum Paso {
            AntesDeEmpezar, Escena, Jornada, Aviso, Decision, Resultado, Cierre, Prorroga,
            ResumenDelDia, FinDelDesarrollo, NivelCerrado
        }

        private static readonly string[] Momentos = { "1 · Mañana", "2 · Jornada", "3 · Aviso", "4 · Decisión", "5 · Cierre", "6 · Resumen" };

        private LevelRunner Runner { get { return App.Runner; } }

        private GameSession _sesion;
        private string _nombreDelNivel;

        // lo que la pantalla sabe y la sesion no
        private Alerta _alerta;                  // sono y todavia no se ha atendido
        private List<LineaDeGuion> _escena;      // las lineas de la escena de la mañana
        private Guion _guionDeLaEscena;
        private int _lineaDeLaEscena;
        private Nexus.Core.Evaluacion.EntradaTraza _resultado;
        private Nexus.Core.Sesion.DebriefReport _cierre;
        private double[] _alEmpezarElDia = new double[4];
        private int _decisionesDeHoy;

        // interfaz
        private RectTransform _ahora;
        private readonly List<Image> _chips = new List<Image>();
        private readonly List<TMP_Text> _textosChip = new List<TMP_Text>();
        private RectTransform _diario;
        private readonly List<string> _entradas = new List<string>();
        private TMP_Text[] _valores = new TMP_Text[4];
        private BarraView[] _barras = new BarraView[4];
        private Button _pausa;
        private string _claveDeAhora;

        // ==================================================================== construir

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", relleno: Tema.margen, espacio: Tema.margen * 0.75f));

            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 2);
            Ui.Texto(titulos, "Prueba del motor", EstiloTexto.Titulo);
            Ui.Texto(titulos, "Un nivel real, jugado de principio a fin, sin guardar nada. Todavía no son las pantallas del juego: " +
                              "es la forma de ver el flujo mientras se construyen.", EstiloTexto.Pequeno);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Boton(cabecera, "Ver el tema", () => App.Router.Apilar<PantallaDelTema>(), VarianteBoton.Fantasma);
            Ui.Boton(cabecera, "Empezar de nuevo", EmpezarDeNuevo, VarianteBoton.Fantasma);

            ConstruirFranjaDelDia(marco);

            var cuerpo = Ui.Fila(marco, "Cuerpo", Tema.margen, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);

            ConstruirIzquierda(cuerpo);

            var centro = Ui.PanelColumna(cuerpo, "Ahora", Tema.margen, Tema.espacio);
            UiKit.Tamano(centro, flexAncho: 1, flexAlto: 1);
            var scroll = Ui.Desplazable(centro, out _ahora);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            var derecha = Ui.PanelColumna(cuerpo, "Diario", Tema.margen * 0.75f, Tema.espacio);
            UiKit.Tamano(derecha, ancho: 420, flexAlto: 1);
            Ui.Texto(derecha, "LO QUE HA PASADO", EstiloTexto.Pequeno, Tema.cian);
            var scrollDiario = Ui.Desplazable(derecha, out _diario);
            UiKit.Tamano(scrollDiario, flexAncho: 1, flexAlto: 1);

            Runner.AlSonarAlerta += AlSonar;
            Runner.AlExpirarAlerta += AlExpirar;
            Runner.AlLlegarElCierre += AlCierre;
            Runner.AlTerminarElDia += AlFinDelDia;

            EmpezarDeNuevo();
        }

        private void ConstruirFranjaDelDia(Transform padre) {
            var franja = Ui.Fila(padre, "Franja del dia", 6);
            Ui.Texto(franja, "UN DÍA ES ASÍ:", EstiloTexto.Pequeno);
            foreach (var momento in Momentos) {
                var chip = Ui.PanelColumna(franja, momento, 8, 0, Tema.pared);
                _chips.Add(chip.GetComponent<Image>());
                _textosChip.Add(Ui.Texto(chip, momento, EstiloTexto.Pequeno, Tema.texto, TextAlignmentOptions.Center));
                UiKit.Tamano(chip, flexAncho: 1);
            }
        }

        private void ConstruirIzquierda(Transform padre) {
            var izquierda = Ui.Columna(padre, "Izquierda", Tema.margen * 0.75f);
            UiKit.Tamano(izquierda, ancho: 360, flexAlto: 1);

            RelojView.Crear(Ui, izquierda, Runner);

            var ritmo = Ui.Tarjeta(izquierda, "Ritmo del reloj");
            var fila = Ui.Fila(ritmo, espacio: 6);
            _pausa = Ui.Boton(fila, "Pausa", () => Runner.Pausado = !Runner.Pausado);
            foreach (var v in new[] { 1, 2, 4 }) {
                var velocidad = v;
                UiKit.Tamano(Ui.Boton(fila, "×" + v, () => Runner.Velocidad = velocidad, VarianteBoton.Fantasma), ancho: 64);
            }

            var proyecto = Ui.Tarjeta(izquierda, "El proyecto");
            var nombres = new[] { "Avance", "Deuda técnica", "Moral del equipo", "Cobertura de pruebas" };
            var colores = new[] { Tema.cian, Tema.mostaza, Tema.cianClaro, Tema.cian };
            for (var i = 0; i < 4; i++) {
                var cabecera = Ui.Fila(proyecto);
                Ui.Texto(cabecera, nombres[i], EstiloTexto.Pequeno, Tema.texto);
                Ui.Resorte(cabecera);
                _valores[i] = Ui.Texto(cabecera, "", EstiloTexto.Pequeno, Tema.texto, TextAlignmentOptions.Right);
                _barras[i] = Ui.Barra(proyecto, 0, colores[i]);
            }
        }

        private void OnDestroy() {
            if (App == null || Runner == null) return;
            Runner.AlSonarAlerta -= AlSonar;
            Runner.AlExpirarAlerta -= AlExpirar;
            Runner.AlLlegarElCierre -= AlCierre;
            Runner.AlTerminarElDia -= AlFinDelDia;
            Runner.Detener();
        }

        // ==================================================================== la sesion de prueba

        /// <summary>
        /// Una sesion desechable del primer nivel. La Fase 1 se resuelve sola con la opcion adecuada: en el
        /// juego es una pantalla propia, aqui solo estorbaria para ver el dia.
        /// </summary>
        private void EmpezarDeNuevo() {
            var catalogo = App.Catalogo;
            var nivel = ProgresionDeNiveles.Primero(catalogo);
            var perfil = catalogo.Niveles[nivel];
            _nombreDelNivel = perfil.Nombre;

            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), catalogo.Flags);
            flags.Inicializar();
            _sesion = new GameSession(catalogo, nivel, flags, 4417);

            _sesion.ElegirMetodologia(perfil.MetodologiasPermitidas[0], "prueba");
            var calidad = perfil.Fase1.Calidad;
            _sesion.RepartirCalidad(new Dictionary<string, int> { { calidad.Atributos[0].Id, calidad.Fichas } });
            var arquitectura = perfil.Fase1.Arquitecturas.First(a => a.EsLaAdecuada);
            _sesion.ElegirArquitectura(arquitectura.Id, arquitectura.RazonesValidas.FirstOrDefault() ?? "prueba");
            _sesion.CerrarFase1();

            Runner.Usar(_sesion);
            Runner.EnEscena = false;
            Runner.Pausado = false;
            _alerta = null;
            _escena = null;
            _resultado = null;
            _cierre = null;
            _entradas.Clear();
            Anotar($"Empieza «{_nombreDelNivel}». Fase 1 resuelta: {_sesion.Metodologia.Nombre}, {arquitectura.Nombre}.");
            _claveDeAhora = null;
        }

        // ==================================================================== que pasa ahora

        private Paso PasoActual() {
            if (_cierre != null) return Paso.NivelCerrado;
            if (_escena != null) return Paso.Escena;
            if (_resultado != null) return Paso.Resultado;
            if (_sesion.Decision != null) return Paso.Decision;
            if (_alerta != null && _alerta.EstaPendiente) return Paso.Aviso;

            switch (Runner.Estado) {
                case EstadoDelDia.Parado: return Paso.AntesDeEmpezar;
                case EstadoDelDia.EnElCierre: return Paso.Cierre;
                case EstadoDelDia.Prorroga: return Paso.Prorroga;
                case EstadoDelDia.DiaTerminado: return Paso.ResumenDelDia;
                case EstadoDelDia.DesarrolloTerminado: return Paso.FinDelDesarrollo;
                default: return Paso.Jornada;
            }
        }

        private static int MomentoDe(Paso paso) {
            switch (paso) {
                case Paso.Escena: return 0;
                case Paso.Jornada: case Paso.Prorroga: return 1;
                case Paso.Aviso: return 2;
                case Paso.Decision: case Paso.Resultado: return 3;
                case Paso.Cierre: return 4;
                case Paso.ResumenDelDia: return 5;
                default: return -1;
            }
        }

        private void Update() {
            if (_sesion == null) return;

            var paso = PasoActual();
            var clave = $"{paso}|{_sesion.R.DiaActual}|{_lineaDeLaEscena}|{_alerta?.Id}|{_sesion.SePuedeCerrarLaJornada}|{QuedaAlgunAviso()}";
            if (clave != _claveDeAhora) {
                _claveDeAhora = clave;
                PintarAhora(paso);
                PintarFranja(paso);
            }
            PintarProyecto();
            _pausa.GetComponentInChildren<TMP_Text>().text = Runner.Pausado ? "Seguir" : "Pausa";
        }

        private void PintarFranja(Paso paso) {
            var actual = MomentoDe(paso);
            for (var i = 0; i < _chips.Count; i++) {
                _chips[i].color = i == actual ? Tema.cian : Tema.pared;
                _textosChip[i].color = i == actual ? Tema.textoSobreCian : Tema.textoTenue;
            }
        }

        private void PintarProyecto() {
            var w = _sesion.W;
            _valores[0].text = $"{w.Avance:0} / {w.Alcance:0} puntos";
            _barras[0].Valor = w.Alcance > 0 ? (float)(w.Avance / w.Alcance) : 0;
            _valores[1].text = $"{w.DeudaTecnica:0} / 100";
            _barras[1].Valor = (float)w.DeudaTecnica / 100f;
            _valores[2].text = $"{w.MoralEquipo:0} / 100";
            _barras[2].Valor = (float)w.MoralEquipo / 100f;
            _valores[3].text = $"{w.Cobertura:0} %";
            _barras[3].Valor = (float)w.Cobertura / 100f;
        }

        private void PintarAhora(Paso paso) {
            UiKit.Vaciar(_ahora);
            switch (paso) {
                case Paso.AntesDeEmpezar: AntesDeEmpezar(); break;
                case Paso.Escena: Escena(); break;
                case Paso.Jornada: Jornada(); break;
                case Paso.Aviso: Aviso(); break;
                case Paso.Decision: Decision(); break;
                case Paso.Resultado: Resultado(); break;
                case Paso.Cierre: Cierre(); break;
                case Paso.Prorroga: Prorroga(); break;
                case Paso.ResumenDelDia: ResumenDelDia(); break;
                case Paso.FinDelDesarrollo: FinDelDesarrollo(); break;
                case Paso.NivelCerrado: NivelCerrado(); break;
            }
        }

        // ---------------------------------------------------------------- cada paso

        private void AntesDeEmpezar() {
            Titulo("Antes del día 1");
            Parrafo($"En el juego, antes de este punto están la entrevista y la Fase 1: elegir metodología, repartir las fichas " +
                    $"de calidad y elegir la arquitectura. En esta prueba ya están resueltas ({_sesion.Metodologia.Nombre}), para ir al grano.");
            Parrafo($"«{_nombreDelNivel}» dura {_sesion.Perfil.DiasTotales} días. Todos tienen la misma forma, la de la franja de arriba: " +
                    "por la mañana puede haber una escena; después la jornada corre sola, y en cualquier momento puede llegar un " +
                    "aviso que hay que atender antes de que caduque. Al final de la jornada eliges irte a casa o quedarte.");
            Accion("Empezar el día 1", EmpezarDia);
        }

        private void Escena() {
            var linea = _escena[_lineaDeLaEscena];
            Etiqueta("1 · MAÑANA  —  ESCENA «" + _guionDeLaEscena.Titulo.ToUpperInvariant() + "»");
            if (linea.Quien != "narrador") Ui.Texto(_ahora, linea.Quien, EstiloTexto.Subtitulo);
            Parrafo(linea.Texto);
            Nota($"{_lineaDeLaEscena + 1} de {_escena.Count}  ·  el reloj espera mientras dura la escena");

            var ultima = _lineaDeLaEscena == _escena.Count - 1;
            if (!ultima) { Accion("Siguiente", () => _lineaDeLaEscena++); return; }

            if (_guionDeLaEscena.Opciones != null && _guionDeLaEscena.Opciones.Count > 0) {
                Nota("Esta escena termina con una elección. Queda anotada y se tendrá en cuenta al cerrar el nivel.");
                foreach (var opcion in _guionDeLaEscena.Opciones) {
                    var o = opcion;
                    Ui.BotonDeOpcion(_ahora, o.Texto, null, () => {
                        _sesion.RegistrarEleccion(_guionDeLaEscena.Id, o.Id);
                        Anotar($"Elegiste: «{o.Texto}»");
                        TerminarEscena();
                    });
                }
                return;
            }
            Accion("Empezar la jornada", TerminarEscena);
        }

        private void Jornada() {
            Titulo("La jornada corre");
            var porSonar = QuedaAlgunAviso();
            Parrafo(porSonar
                ? "Hoy va a llegar un aviso, y no sabes cuándo. Cuando llegue, tendrás unas horas para atenderlo desde tu puesto."
                : "Hoy ya no queda ningún aviso por llegar.");
            Parrafo("Mientras tanto, el reloj corre solo. Puedes esperar, acelerarlo con ×2 o ×4 a la izquierda, o usar un atajo:");

            var brief = _sesion.BriefDeHoy;
            if (brief != null && brief.Avisos.Count > 0) {
                Nota("Anuncios de hoy (avisan de lo que llegará en los próximos días):");
                foreach (var aviso in brief.Avisos) Nota("   " + aviso);
            }

            var botones = Ui.Fila(_ahora);
            if (porSonar) Ui.Boton(botones, "Adelantar hasta el aviso", () => Runner.AdelantarHastaElSiguienteAviso(), VarianteBoton.Primario);
            var cerrar = Ui.Boton(botones, "Cerrar la jornada", () => Runner.CerrarJornada(), porSonar ? VarianteBoton.Secundario : VarianteBoton.Primario);
            cerrar.interactable = _sesion.SePuedeCerrarLaJornada;
            if (!_sesion.SePuedeCerrarLaJornada)
                Nota("«Cerrar la jornada» salta al final del día, y solo se puede cuando no queda nada pendiente: saltar nunca te ahorra una consecuencia.");
        }

        private void Aviso() {
            Etiqueta("3 · AVISO", Tema.mostaza);
            Titulo("Ha llegado un aviso");
            Parrafo(_alerta.Texto);
            Parrafo($"Caduca a las {RelojDeJornada.Formatear(_alerta.MinutoDeExpiracion)}. Si no lo atiendes antes, alguien decidirá por ti, y casi nunca bien.");
            Nota("El reloj NO se para mientras decides si ir: por eso esperar también cuesta.");
            Accion("Atender el aviso", Atender);
        }

        private void Decision() {
            var d = _sesion.Decision;
            Etiqueta("4 · DECISIÓN");
            Titulo(d.Titulo);
            if (!string.IsNullOrEmpty(d.TextoAviso)) Parrafo(d.TextoAviso);
            Nota("El reloj está parado mientras decides. Debajo de cada opción, lo que cambia en el momento; las consecuencias que llegan días después no se ven.");
            foreach (var opcion in d.Opciones) {
                var o = opcion;
                var detalle = o.Bloqueada ? "No disponible: " + o.MotivoBloqueo : Previsualizar(o.Previsualizacion);
                if (!string.IsNullOrEmpty(o.NotaDeMetodologia) && !o.Bloqueada) detalle += "   ·   " + o.NotaDeMetodologia;
                var boton = Ui.BotonDeOpcion(_ahora, o.Texto, detalle, () => Resolver(o.Id));
                boton.interactable = !o.Bloqueada;
            }
        }

        private void Resultado() {
            Etiqueta("4 · DECISIÓN  —  LO QUE SIGNIFICA");
            Titulo(_resultado.Titulo);
            Parrafo("Elegiste: " + _resultado.OpcionTexto);
            var color = _resultado.Veredicto == "correcta" ? Tema.cian : _resultado.Veredicto == "aceptable" ? Tema.amarillo : Tema.rojo;
            Ui.Texto(_ahora, _resultado.Veredicto.ToUpperInvariant(), EstiloTexto.Subtitulo, color);
            Parrafo(_resultado.Razon);
            Nota("La rúbrica no se enseña así durante el juego: se lee en el Dashboard de Lecciones al cerrar el nivel. Aquí se muestra para ver el motor.");
            Accion("Seguir con la jornada", () => { _resultado = null; Runner.EnEscena = false; });
        }

        private void Cierre() {
            Etiqueta("5 · CIERRE");
            Titulo($"Son las {_sesion.HoraActual}: se acaba la jornada");
            Parrafo("Es la decisión más repetida del juego. Irte a casa: descansas, y el equipo también. Quedarte hasta la hora " +
                    "límite: hoy avanzas un 25 % más, pero lo pagas en cansancio, salud y deuda técnica, y se acumula.");
            var botones = Ui.Fila(_ahora);
            Ui.Boton(botones, "Irme a casa", () => { Runner.Irse(); Anotar("Te fuiste a casa."); }, VarianteBoton.Primario);
            Ui.Boton(botones, "Quedarme", () => { Runner.Quedarse(); Anotar("Te quedaste haciendo horas extra."); }, VarianteBoton.Peligro);
        }

        private void Prorroga() {
            Etiqueta("2 · JORNADA  —  HORAS EXTRA");
            Titulo("Te has quedado");
            Parrafo("El día ya contó con horas extra. Hasta la hora límite no llegan avisos: en el juego es tiempo libre para " +
                    "recorrer el mapa, hablar con la gente y encontrar coleccionables.");
            Accion("Terminar la jornada ya", () => Runner.TerminarLaProrroga());
        }

        private void ResumenDelDia() {
            var w = _sesion.W;
            Etiqueta("6 · RESUMEN");
            Titulo($"Fin del día {_sesion.R.DiaActual} de {_sesion.Perfil.DiasTotales}");
            Parrafo($"Hoy: avance {Cambio(w.Avance - _alEmpezarElDia[0])} · deuda {Cambio(w.DeudaTecnica - _alEmpezarElDia[1])} · " +
                    $"moral {Cambio(w.MoralEquipo - _alEmpezarElDia[2])} · cobertura {Cambio(w.Cobertura - _alEmpezarElDia[3])}.");
            Parrafo(_decisionesDeHoy == 0 ? "Hoy no tomaste ninguna decisión." :
                    _decisionesDeHoy == 1 ? "Hoy tomaste 1 decisión." : $"Hoy tomaste {_decisionesDeHoy} decisiones.");
            Accion($"Empezar el día {_sesion.R.DiaActual + 1}", EmpezarDia);
        }

        private void FinDelDesarrollo() {
            Titulo("Se acabaron los días");
            Parrafo("El desarrollo ha terminado. Ahora viene el lanzamiento: el cliente ve lo que has hecho, y el riesgo que " +
                    "acumulaste durante todos los días decide si sale bien. Después, el nivel se cierra y se evalúa.");
            Accion("Lanzar y cerrar el nivel", Lanzar);
        }

        private void NivelCerrado() {
            var l = _cierre.Lanzamiento;
            Titulo(l != null && l.Exito ? "El lanzamiento salió bien" : "El lanzamiento falló");
            if (l != null) {
                if (!string.IsNullOrEmpty(l.Texto)) Parrafo(l.Texto);
                Parrafo($"Entregado: {l.AlcanceEntregado:0} de {l.AlcanceComprometido:0} puntos · defectos que llegaron al cliente: {l.DefectosEscapados}.");
            }
            Parrafo(_cierre.CumpleUmbralesDeExito
                ? "El nivel cumple sus umbrales de éxito."
                : "No cumple sus umbrales: " + string.Join(" ", _cierre.UmbralesFallados));
            if (_cierre.Metodologia != null)
                Parrafo(_cierre.Metodologia.EraAdecuada
                    ? $"{_sesion.Metodologia.Nombre} era una metodología adecuada para este proyecto."
                    : $"{_sesion.Metodologia.Nombre} no era la metodología adecuada para este proyecto.");

            Nota("Tus decisiones, tal como las leerá el Dashboard de Lecciones:");
            foreach (var e in _cierre.Traza.Entradas.Where(x => x.Origen != null && x.Origen.StartsWith("EV-")))
                Nota($"   Día {e.Dia} · {e.Titulo}: {e.Veredicto}");
            Accion("Empezar de nuevo", EmpezarDeNuevo);
        }

        // ==================================================================== acciones

        private void EmpezarDia() {
            var w = _sesion.W;
            _alEmpezarElDia = new[] { w.Avance, w.DeudaTecnica, w.MoralEquipo, w.Cobertura };
            _decisionesDeHoy = 0;
            _alerta = null;

            var brief = Runner.EmpezarDia();
            if (brief == null) return;
            Anotar($"— Día {brief.Dia} · {brief.EtiquetaUnidad} —");
            if (brief.Ceremonias.Count > 0) Anotar("Ceremonias: " + string.Join(", ", brief.Ceremonias));

            if (_sesion.PendingPlanning != null) {
                var puntos = _sesion.PendingPlanning.CapacidadSugerida;
                _sesion.Comprometer(puntos);
                Anotar($"Planificación del sprint: el equipo se compromete a {puntos:0} puntos (en el juego lo decides tú).");
            }
            if (_sesion.PendingRetro != null && _sesion.PendingRetro.Acciones.Count > 0) {
                var accion = _sesion.PendingRetro.Acciones[0];
                _sesion.ElegirAccionRetro(accion.Id);
                Anotar($"Retrospectiva: «{accion.Texto}» (en el juego lo decides tú).");
            }

            if (_sesion.Beat != null) AbrirEscena(_sesion.Beat);
        }

        private void AbrirEscena(BeatDeHoy beat) {
            var guion = App.Catalogo.Guiones.FirstOrDefault(g => g.Id == beat.BeatId);
            List<LineaDeGuion> lineas;
            if (guion == null || !guion.Variantes.TryGetValue(beat.Variante, out lineas) || lineas.Count == 0) return;

            _guionDeLaEscena = guion;
            _escena = lineas;
            _lineaDeLaEscena = 0;
            Runner.EnEscena = true;
            Anotar($"Escena: «{guion.Titulo}»");
        }

        private void TerminarEscena() {
            _escena = null;
            _guionDeLaEscena = null;
            Runner.EnEscena = false;
        }

        private void AlSonar(Alerta alerta) {
            _alerta = alerta;
            Anotar($"{RelojDeJornada.Formatear(alerta.MinutoDeLaAlerta)} · Aviso: {alerta.Texto}");
        }

        private void Atender() {
            if (_alerta == null || !_alerta.EstaPendiente) return;
            var alerta = _alerta;
            _alerta = null;
            _sesion.AtenderAlerta(alerta.Id);
            if (_sesion.Decision != null) Runner.EnEscena = true;   // se para DENTRO de la escena, no mientras se decidia ir
        }

        private void Resolver(string opcionId) {
            _sesion.ResolverDecision(opcionId);
            _resultado = _sesion.Traza.Entradas.LastOrDefault();
            _decisionesDeHoy++;
            if (_resultado != null) Anotar($"{_sesion.HoraActual} · Decidiste en «{_resultado.Titulo}»: {_resultado.Veredicto}.");
        }

        private void AlExpirar(Alerta alerta) {
            if (_alerta != null && _alerta.Id == alerta.Id) _alerta = null;
            Anotar($"{RelojDeJornada.Formatear(alerta.MinutoDeExpiracion)} · El aviso caducó. Alguien decidió por ti.");
        }

        private void AlCierre() {
            Anotar($"{_sesion.HoraActual} · Cierre de la jornada.");
        }

        private void AlFinDelDia() {
            Anotar($"Fin del día {_sesion.R.DiaActual}.");
        }

        private void Lanzar() {
            _sesion.EjecutarLanzamiento();
            _cierre = _sesion.Cerrar();
            Anotar(_cierre.Lanzamiento != null && _cierre.Lanzamiento.Exito ? "Lanzamiento: salió bien." : "Lanzamiento: falló.");
            Anotar("Nivel cerrado.");
        }

        // ==================================================================== ayudas

        private bool QuedaAlgunAviso() {
            if (_sesion == null || Runner.Estado != EstadoDelDia.Corriendo) return false;
            var ahora = _sesion.MinutoDelDia;
            return _sesion.AlertasDeHoy.Any(a => a.EstaPendiente && a.MinutoDeLaAlerta > ahora);
        }

        private static readonly Dictionary<string, string> NombresDeStock = new Dictionary<string, string> {
            { "Avance", "Avance" }, { "DeudaTecnica", "Deuda" }, { "MoralEquipo", "Moral" }, { "Cobertura", "Cobertura" },
            { "Documentacion", "Documentación" }, { "SatisfaccionCliente", "Cliente" }, { "Alcance", "Alcance" },
            { "Dias", "Días gastados" }, { "Cansancio", "Cansancio" }, { "Reputacion", "Reputación" },
            { "Competencia", "Competencia" }, { "SaludJugador", "Salud" }, { "Dinero", "Dinero" }
        };

        private static string Previsualizar(Dictionary<string, double> cambios) {
            if (cambios == null || cambios.Count == 0) return "Sin cambios inmediatos.";
            return string.Join(" · ", cambios.Where(kv => Math.Abs(kv.Value) >= 0.05)
                .Select(kv => (NombresDeStock.TryGetValue(kv.Key, out var n) ? n : kv.Key) + " " + Cambio(kv.Value)));
        }

        private static string Cambio(double v) {
            return v >= 0 ? "+" + v.ToString("0.#") : "−" + (-v).ToString("0.#");
        }

        private void Etiqueta(string texto, Color? color = null) {
            Ui.Texto(_ahora, texto, EstiloTexto.Pequeno, color ?? Tema.cian);
        }

        private void Titulo(string texto) { Ui.Texto(_ahora, texto, EstiloTexto.Titulo); }
        private void Parrafo(string texto) { Ui.Texto(_ahora, texto, EstiloTexto.Cuerpo); }
        private void Nota(string texto) { Ui.Texto(_ahora, texto, EstiloTexto.Pequeno); }

        private void Accion(string texto, Action alPulsar) {
            Ui.Espaciador(_ahora, Tema.espacio);
            var fila = Ui.Fila(_ahora);
            Ui.Boton(fila, texto, alPulsar, VarianteBoton.Primario);
        }

        private void Anotar(string linea) {
            _entradas.Insert(0, linea);   // lo mas reciente arriba: es lo que se busca con la vista
            if (_entradas.Count > 40) _entradas.RemoveAt(_entradas.Count - 1);
            if (_diario == null) return;
            UiKit.Vaciar(_diario);
            foreach (var e in _entradas)
                Ui.Texto(_diario, e, EstiloTexto.Pequeno, e.StartsWith("—") ? Tema.cian : Tema.texto);
        }
    }
}
