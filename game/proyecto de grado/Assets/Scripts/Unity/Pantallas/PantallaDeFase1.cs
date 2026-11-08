using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Fase1;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La Fase 1, antes del primer dia:
    ///   1 · El proyecto, POR PARTES y una a la vez: el cliente · el sistema · que lo hace especial · como se mide ·
    ///       comprueba que lo entendiste. Antes eran doce tarjetas apiladas en un solo scroll, y no se leian.
    ///   2 · La recoleccion 3D por el plano (zonas A–F). La construye otro subequipo; aqui se simula
    ///   3 · La metodologia y su motivo      4 · El reparto de fichas de calidad
    ///   5 · La arquitectura y su motivo     6 · El resumen, con «Confirmar y empezar»
    ///
    /// Se puede ir y volver entre pasos todas las veces que se quiera: las tres decisiones se guardan AQUI y
    /// solo se entregan al motor al confirmar el resumen (una llamada por decision, como siempre). La
    /// recoleccion es la excepcion: pasa en el mundo, y se aplica en cuanto termina.
    ///
    /// Ningun veredicto se enseña aqui: se leen en el Dashboard de Lecciones.
    /// </summary>
    public sealed class PantallaDeFase1 : Pantalla {
        private enum Paso { Encargo, Recoleccion, Metodologia, Calidad, Arquitectura, Backlog, Resumen }

        /// <summary>El product backlog que se va armando: ids de historias, en el orden en que se haran.</summary>
        private readonly List<string> _backlog = new List<string>();

        private GameSession S { get { return App.Sesion; } }
        private LevelProfile Perfil { get { return S.Perfil; } }
        private bool HayRecoleccion { get { return S.Recoleccion != null; } }

        private Paso _paso = Paso.Encargo;
        /// <summary>La parte del expediente que se esta leyendo, dentro del paso «El proyecto».</summary>
        private int _subPaso;
        private static readonly string[] PartesDelExpediente = { "El cliente", "El sistema", "Qué lo hace especial", "Cómo se mide", "Comprueba" };
        private static readonly string[] AvisoDeCadaParte = { "fase1.encargo", "fase1.encargo.sistema", "fase1.encargo.especial", "fase1.encargo.medidas", "fase1.encargo.comprueba" };
        private bool _guionInicialPendiente = true, _avisarAlEmpezar;
        private Hoja _hoja;
        private RectTransform _navegacion;
        private readonly Dictionary<Paso, Button> _chips = new Dictionary<Paso, Button>();
        private ScrollRect _scroll;

        private string _metodologia, _razonMetodologia, _arquitectura, _razonArquitectura;
        /// <summary>Cuanto dura cada sprint, etapa o revision (OpcionesDeCalendario). null = la de serie.</summary>
        private string _duracion;
        private readonly Dictionary<string, int> _fichas = new Dictionary<string, int>();

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 0.75f));

            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "FASE 1 · PLANIFICACIÓN", EstiloTexto.Leyenda, Tema.cyan);
            Ui.Texto(titulos, Perfil.Nombre, EstiloTexto.Titulo);
            GuiaView.BotonDeAyuda(App, cabecera);
            Ui.Boton(cabecera, "Menú", () => App.Router.Apilar<PantallaDePausa>(p => p.AntesDeSalir = GuardarBorrador),
                     VarianteBoton.Fantasma);

            var franja = Ui.Fila(marco, "Pasos", 6);
            GuiaView.Registrar("fase1.pasos", franja);
            foreach (var paso in Pasos()) {
                var p = paso;
                var chip = Ui.Boton(franja, NombreDe(p), () => IrA(p), VarianteBoton.Secundario);
                UiKit.Tamano(chip, flexAncho: 1);
                _chips[p] = chip;
            }

            // Como en las referencias de la Fase 1: el protagonista pensando a la izquierda, y la decision en una
            // ventana de metal a su lado.
            var escena = Ui.Fila(marco, "Escena", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(escena, flexAncho: 1, flexAlto: 1);
            var protagonista = Ui.Ilustracion(escena, "personajes/protagonista", "Tú", 300);
            UiKit.Tamano(protagonista, flexAlto: 1);
            var panel = Ui.Ventana(escena, "Contenido");
            UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            _scroll = Ui.Desplazable(panel, out contenido);
            UiKit.Tamano(_scroll, flexAncho: 1, flexAlto: 1);
            _hoja = new Hoja(Ui, contenido);
            GuiaView.Registrar("fase1.contenido", panel);
            _navegacion = Ui.Fila(panel, "Navegacion");
            GuiaView.Registrar("fase1.navegacion", _navegacion);

            foreach (var atributo in Perfil.Fase1.Calidad.Atributos) _fichas[atributo.Id] = 0;
            CargarBorrador();
        }

        // ==================================================================== salir y volver

        /// <summary>
        /// Lo elegido hasta ahora viaja en el guardado (sin entregarse al motor: eso solo pasa al confirmar). Asi
        /// «Guardar y salir» (la pausa) no obliga a planificar otra vez desde cero.
        /// </summary>
        private void GuardarBorrador() {
            if (S == null || S.Fase1Cerrada) return;
            S.R.Fase1Borrador = new Fase1Borrador {
                Paso = _paso.ToString(), SubPaso = _subPaso, Metodologia = _metodologia, RazonMetodologia = _razonMetodologia, DuracionDeUnidad = _duracion,
                Arquitectura = _arquitectura, RazonArquitectura = _razonArquitectura,
                Fichas = new Dictionary<string, int>(_fichas),
                Comprobacion = new Dictionary<string, int>(_comprobacion),
                FalladasAlguna = _falladas.ToList(),
                Backlog = _backlog.ToList()
            };
        }

        private void CargarBorrador() {
            var b = S.R.Fase1Borrador;
            if (b == null) return;
            _metodologia = b.Metodologia;
            _razonMetodologia = b.RazonMetodologia;
            _duracion = b.DuracionDeUnidad;
            _arquitectura = b.Arquitectura;
            _razonArquitectura = b.RazonArquitectura;
            if (b.Fichas != null)
                foreach (var kv in b.Fichas) if (_fichas.ContainsKey(kv.Key)) _fichas[kv.Key] = kv.Value;
            if (b.Comprobacion != null) foreach (var kv in b.Comprobacion) _comprobacion[kv.Key] = kv.Value;
            if (b.FalladasAlguna != null) foreach (var id in b.FalladasAlguna) _falladas.Add(id);
            if (b.Backlog != null) foreach (var id in b.Backlog) if (Perfil.Proyecto != null && Perfil.Proyecto.Historia(id) != null && !_backlog.Contains(id)) _backlog.Add(id);
            Paso paso;
            if (!string.IsNullOrEmpty(b.Paso) && System.Enum.TryParse(b.Paso, out paso) && Pasos().Contains(paso)) _paso = paso;
            _subPaso = Mathf.Clamp(b.SubPaso, 0, PartesDelPaso - 1);
            // Se vuelve con la intro ya vista: no hay guiones que esperar, y la guia arranca en el paso donde se dejo.
            _guionInicialPendiente = false;
            _avisarAlEmpezar = true;
        }

        private IEnumerable<Paso> Pasos() {
            yield return Paso.Encargo;
            if (HayRecoleccion) yield return Paso.Recoleccion;
            yield return Paso.Metodologia;
            yield return Paso.Calidad;
            yield return Paso.Arquitectura;
            if (Perfil.Proyecto != null && !string.IsNullOrEmpty(Perfil.Proyecto.BacklogInicial)) yield return Paso.Backlog;
            yield return Paso.Resumen;
        }

        private string NombreDe(Paso p) {
            switch (p) {
                case Paso.Encargo: return Perfil.Proyecto != null ? "El proyecto" : "El encargo";
                case Paso.Recoleccion: return "Recorrido";
                case Paso.Metodologia: return "Metodología";
                case Paso.Calidad: return "Calidad";
                case Paso.Arquitectura: return "Arquitectura";
                case Paso.Backlog: return "Backlog";
                default: return "Resumen";
            }
        }

        private void Update() {
            if (_avisarAlEmpezar) {
                _avisarAlEmpezar = false;
                Repintar();
                GuiaView.Avisar(App, AvisoDelPasoActual());
                return;
            }
            // Los guiones de la Fase 1 (la guia de Marisol, la llegada de Voss) se apilan encima en cuanto la
            // pantalla existe. No se puede hacer en Construir: el router todavia no la tiene en su pila.
            if (!_guionInicialPendiente) return;
            _guionInicialPendiente = false;
            ReproducirGuionesDeFase1(NarrativeBeat.VarianteDefecto, () => GuiaView.Avisar(App, "fase1.encargo"));
        }

        private void ReproducirGuionesDeFase1(string variante, System.Action alTerminar) {
            var guiones = FlujoDelNivel.Guiones(App, S.NivelId, MomentosDeGuion.Fase1)
                .Where(g => g.Variantes.ContainsKey(variante)).ToList();
            ReproducirEnOrden(guiones, 0, variante, alTerminar);
        }

        private void ReproducirEnOrden(List<Guion> guiones, int i, string variante, System.Action alTerminar) {
            if (i >= guiones.Count) { alTerminar?.Invoke(); return; }
            FlujoDelNivel.Reproducir(App, guiones[i], variante, true, () => ReproducirEnOrden(guiones, i + 1, variante, alTerminar));
        }

        // ==================================================================== pintar

        public override void Repintar() {
            if (_hoja == null) return;
            GuardarBorrador();
            foreach (var kv in _chips) {
                var hecho = Completo(kv.Key) && kv.Key != _paso;
                Ui.Resaltar(kv.Value, kv.Key == _paso, hecho ? Tema.successSoft : Tema.surfaceRaised);
                kv.Value.GetComponentInChildren<TMP_Text>().text = Nexus.Core.Narrativa.Glifos.Seguro((hecho ? "√ " : "") + NombreDe(kv.Key));
            }
            var posicion = _scroll.content.anchoredPosition;
            _hoja.Vaciar();
            switch (_paso) {
                case Paso.Encargo: Encargo(); break;
                case Paso.Recoleccion: Recoleccion(); break;
                case Paso.Metodologia: Metodologia(); break;
                case Paso.Calidad: Calidad(); break;
                case Paso.Arquitectura: Arquitectura(); break;
                case Paso.Backlog: Backlog(); break;
                case Paso.Resumen: Resumen(); break;
            }
            PintarNavegacion();
            // Al contestar una pregunta se repinta todo: se queda donde estaba, no salta arriba del todo.
            if (_mantenerScroll) _scroll.content.anchoredPosition = posicion;
            else _scroll.verticalNormalizedPosition = 1;
            _mantenerScroll = false;
        }

        private bool Completo(Paso p) {
            switch (p) {
                case Paso.Recoleccion: return S.RecoleccionHecha;
                case Paso.Metodologia: return _metodologia != null && _razonMetodologia != null;
                case Paso.Calidad: return true;
                case Paso.Arquitectura: return _arquitectura != null && _razonArquitectura != null;
                case Paso.Backlog: return !S.SeCreaElBacklog || _backlog.Count >= MinimoDeHistorias;
                case Paso.Resumen: return false;
                case Paso.Encargo: return ComprobacionHecha;
                default: return true;
            }
        }

        /// <summary>Cuantas partes tiene el paso en el que se esta: el expediente va por partes; lo demas, de una pieza.</summary>
        private int PartesDelPaso {
            get { return _paso == Paso.Encargo && Perfil.Proyecto != null ? PartesDelExpediente.Length : 1; }
        }

        private string AvisoDelPasoActual() {
            return _paso == Paso.Encargo && Perfil.Proyecto != null
                ? AvisoDeCadaParte[Mathf.Clamp(_subPaso, 0, AvisoDeCadaParte.Length - 1)]
                : "fase1." + _paso.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// «Atrás» y «Siguiente» recorren primero las partes del expediente y despues los pasos: el camino es uno solo,
        /// y no hay que entender que hay dos niveles de pestañas para avanzar.
        /// </summary>
        private void PintarNavegacion() {
            UiKit.Vaciar(_navegacion);
            var pasos = Pasos().ToList();
            var i = pasos.IndexOf(_paso);
            if (_subPaso > 0) Ui.Boton(_navegacion, "◄  Atrás", () => IrAParte(_subPaso - 1), VarianteBoton.Fantasma);
            else if (i > 0) Ui.Boton(_navegacion, "◄  Atrás", () => IrA(pasos[i - 1], true), VarianteBoton.Fantasma);
            Ui.Resorte(_navegacion);
            if (_subPaso < PartesDelPaso - 1) {
                Ui.Boton(_navegacion, "Siguiente  ►", () => IrAParte(_subPaso + 1), VarianteBoton.Primario);
            } else if (i < pasos.Count - 1) {
                var siguiente = Ui.Boton(_navegacion, "Siguiente  ►", () => IrA(pasos[i + 1]), VarianteBoton.Primario);
                siguiente.interactable = Completo(_paso);
            }
        }

        private void IrAParte(int parte) {
            _subPaso = Mathf.Clamp(parte, 0, PartesDelPaso - 1);
            Repintar();
            GuiaView.Avisar(App, AvisoDelPasoActual());
        }

        // ==================================================================== 1 · el encargo

        private void Encargo() {
            var f = Perfil.Proyecto;
            if (f == null) {
                // Un nivel sin expediente: el encargo cabe en una sola hoja.
                _hoja.Etiqueta("El encargo");
                _hoja.Titulo(Perfil.Nombre);
                if (Perfil.Briefing != null) foreach (var linea in Perfil.Briefing) _hoja.Parrafo(linea);
                PintarPistas(_hoja.Raiz);
                _hoja.Espacio();
                _hoja.Nota($"{Perfil.DiasTotales} días · {TextoDelEquipo()} · alcance de {Perfil.AlcanceInicial:0} puntos de trabajo");
                _hoja.Nota("Las tres decisiones se juzgan contra lo que dice aquí.");
                PintarMetricas();
                return;
            }

            _subPaso = Mathf.Clamp(_subPaso, 0, PartesDelExpediente.Length - 1);
            _hoja.Etiqueta("El proyecto · " + f.Nombre);

            // Las partes del expediente: se lee UNA a la vez. Se puede saltar a cualquiera, o ir con «Siguiente».
            var partes = _hoja.Fila(6);
            for (var i = 0; i < PartesDelExpediente.Length; i++) {
                var parte = i;
                var hecha = i == PartesDelExpediente.Length - 1 && ComprobacionHecha;
                var boton = Ui.Boton(partes, (hecha ? "√ " : (i + 1) + " · ") + PartesDelExpediente[i], () => IrAParte(parte), VarianteBoton.Fantasma);
                Ui.Resaltar(boton, i == _subPaso);
            }
            _hoja.Separador();

            switch (_subPaso) {
                case 0: ParteDelCliente(f); break;
                case 1: ParteDelSistema(f); break;
                case 2: ParteDeLoEspecial(f); break;
                case 3: PintarMetricas(); break;
                default: PintarComprobacion(); break;
            }
        }

        // ==================================================================== 1 · el expediente del proyecto, por partes

        /// <summary>Para quien se construye y que le duele; quien lo va a usar; y el encargo en cifras.</summary>
        private void ParteDelCliente(Nexus.Core.Proyecto.FichaDelProyecto f) {
            _hoja.Titulo(f.Nombre);
            if (!string.IsNullOrEmpty(f.EnUnaFrase)) _hoja.Parrafo(f.EnUnaFrase, Tema.cyan);

            var cliente = _hoja.Tarjeta("El cliente y su sector");
            GuiaView.Registrar("fase1.cliente", cliente);
            Ui.Texto(cliente, $"<b>{f.Cliente.Nombre}</b>: {f.Cliente.QueHace}", EstiloTexto.Cuerpo, Tema.ink);
            Ui.Texto(cliente, $"<b>Qué le duele hoy:</b> {f.Cliente.QueLeDuele}", EstiloTexto.Cuerpo, Tema.ink);
            Ui.Texto(cliente, $"<b>Su sector: {f.Sector.Nombre}.</b> {f.Sector.ComoTrabaja}", EstiloTexto.Pequeno, Tema.ink);
            if (f.Usuarios.Count > 0) {
                Ui.Texto(cliente, "QUIÉN LO VA A USAR", EstiloTexto.Leyenda, Tema.cyan);
                foreach (var u in f.Usuarios) Ui.Texto(cliente, $"· <b>{u.Nombre}</b>: {u.QueNecesita}", EstiloTexto.Pequeno, Tema.ink);
            }
            PantallaDeGlosario.Palabras(App, cliente, "Si alguna palabra no te suena:", "cliente", "interesados", "alcance");

            // Con quien se va a trabajar: se presenta desde el principio, no cuando ya hay confianza. Si el equipo sale
            // de las hojas de vida del recorrido y todavia no se ha recorrido, no hay equipo que presentar: se dice.
            if (S.EquipoDesdeRecoleccion && !S.RecoleccionHecha) {
                var porFormar = _hoja.Tarjeta("Tu equipo en este proyecto", Tono.Aviso);
                Ui.Texto(porFormar, "Todavía no tienes equipo. Lo formas tú en el recorrido por el edificio: cada hoja de vida que " +
                                    "encuentres es una persona que se une al proyecto. Si no encuentras ninguna, lo sacarás tú solo.",
                         EstiloTexto.Pequeno, Tema.ink);
            } else if (Perfil.Equipo != null && Perfil.Equipo.Count > 0) {
                var equipo = _hoja.Tarjeta("Tu equipo en este proyecto");
                foreach (var m in Perfil.Equipo) {
                    Ui.FilaDeEquipo(equipo, m.Nombre, m.Rol, EstadoDeEquipo.Activo);
                    if (!string.IsNullOrEmpty(m.Descripcion)) Ui.Texto(equipo, m.Descripcion, EstiloTexto.Pequeno, Tema.ink);
                    if (!string.IsNullOrEmpty(m.Frase)) Ui.Texto(equipo, "«" + m.Frase + "»", EstiloTexto.Pequeno, Tema.inkMuted);
                }
                Ui.Texto(equipo, "Durante el desarrollo los tienes en «Equipo»: qué lleva cada uno, qué terminó y cómo va.", EstiloTexto.Pequeno, Tema.cyan);
            }

            _hoja.Nota($"{Perfil.DiasTotales} días · {TextoDelEquipo()} · alcance de {Perfil.AlcanceInicial:0} puntos de trabajo");
            _hoja.Nota("Las tres decisiones se juzgan contra lo que dice aquí.");
            if (Perfil.Briefing != null && Perfil.Briefing.Length > 0)
            {
                var filaDelTicket = _hoja.Fila();
                Ui.Boton(filaDelTicket, "Leer el ticket original", () => DialogosDelExpediente.Ticket(App, Perfil), VarianteBoton.Fantasma);
                PantallaDeGlosario.Chip(App, filaDelTicket, "ticket");
            }
        }

        /// <summary>El sistema en una imagen y, al lado, sus partes: lo que se dibuja y lo que significa, juntos.</summary>
        private void ParteDelSistema(Nexus.Core.Proyecto.FichaDelProyecto f) {
            var fila = Ui.Fila(_hoja.Raiz, "Sistema", Tema.espacio, alineacion: TextAnchor.UpperLeft);
            var sistema = Ui.Tarjeta(fila, "El sistema en una imagen");
            UiKit.Tamano(sistema, ancho: 0, flexAncho: 3);
            GuiaView.Registrar("fase1.expediente", sistema);
            Ui.Texto(sistema, "Fuera, quién lo usa; dentro, sus partes. Cada reto es una de ellas.", EstiloTexto.Pequeno, Tema.ink);
            ExpedienteView.Pizarrita(App.UiClasico, sistema, f);   // la tiza no cambia de kit

            var modulos = Ui.Tarjeta(fila, "Sus partes");
            UiKit.Tamano(modulos, ancho: 0, flexAncho: 2);
            foreach (var m in f.Modulos) {
                Ui.Texto(modulos, $"<b>{m.Nombre}</b> · " + (m.AvanceInicial > 0 ? $"{m.AvanceInicial:0} % ya hecho" : "por hacer"),
                         EstiloTexto.Pequeno, m.AvanceInicial > 0 ? Tema.cyan : Tema.ink);
                Ui.Texto(modulos, m.QueHace, EstiloTexto.Pequeno, Tema.inkMuted);
            }
        }

        /// <summary>
        /// Lo que distingue a este proyecto (y lo que se averiguo en el recorrido). Las palabras del oficio y los
        /// documentos son de consulta: estan a un boton, no ocupando la pagina.
        /// </summary>
        private void ParteDeLoEspecial(Nexus.Core.Proyecto.FichaDelProyecto f) {
            var cambia = _hoja.Tarjeta(ExpedienteView.NivelAnterior(App, Perfil) != null ? "Qué tiene de distinto este proyecto"
                                                                                        : "Qué tiene de especial este proyecto");
            GuiaView.Registrar("fase1.distinto", cambia);
            foreach (var linea in ExpedienteView.QueCambia(App, Perfil)) Ui.Texto(cambia, "· " + linea, EstiloTexto.Cuerpo, Tema.ink);

            PintarPistas(_hoja.Raiz);

            var consulta = _hoja.Tarjeta("Para consultar");
            GuiaView.Registrar("fase1.vocabulario", consulta);
            Ui.Texto(consulta, "Las palabras de tu cliente y los documentos que importan.",
                     EstiloTexto.Pequeno, Tema.ink);
            var botones = Ui.Fila(consulta);
            Ui.Boton(botones, "Palabras del oficio", () => DialogosDelExpediente.Vocabulario(App, f));
            if (f.Artefactos.Count > 0) Ui.Boton(botones, "Los documentos que importan", () => DialogosDelExpediente.Documentos(App, f));
        }

        // ==================================================================== «comprueba que lo entendiste»

        private List<Nexus.Core.Proyecto.PreguntaDelProyecto> _preguntas;
        private readonly Dictionary<string, int> _comprobacion = new Dictionary<string, int>();
        private readonly HashSet<string> _falladas = new HashSet<string>();
        private string _ultimaFallada, _ultimaAcertada;
        private bool _mantenerScroll;

        private List<Nexus.Core.Proyecto.PreguntaDelProyecto> Preguntas {
            get {
                if (_preguntas == null)
                    _preguntas = Perfil.Proyecto == null ? new List<Nexus.Core.Proyecto.PreguntaDelProyecto>()
                                                         : Nexus.Core.Proyecto.PreguntasDelProyecto.Para(Perfil.Proyecto, Perfil.Id);
                return _preguntas;
            }
        }

        private bool ComprobacionHecha { get { return Preguntas.All(p => _comprobacion.ContainsKey(p.Id)); } }

        /// <summary>
        /// Una pregunta a la vez: la primera sin contestar, y encima lo que se acaba de acertar con su explicacion.
        /// Con todas hechas, el repaso de las tres.
        /// </summary>
        private void PintarComprobacion() {
            if (Preguntas.Count == 0) {
                _hoja.Nota("Este proyecto no trae preguntas: puedes seguir.");
                return;
            }
            var t = _hoja.Tarjeta("Comprueba que lo entendiste");
            GuiaView.Registrar("fase1.comprobacion", t);
            var hechas = Preguntas.Count(p => _comprobacion.ContainsKey(p.Id));
            Ui.Texto(t, $"{Preguntas.Count} preguntas rápidas. Si fallas, lo intentas otra vez. Llevas {hechas} de {Preguntas.Count}.", EstiloTexto.Pequeno, Tema.ink);

            if (ComprobacionHecha) {
                for (var i = 0; i < Preguntas.Count; i++) {
                    var p = Preguntas[i];
                    Ui.Texto(t, $"{i + 1}. {p.Texto}", EstiloTexto.Cuerpo, Tema.ink).fontStyle = FontStyles.Bold;
                    Ui.Texto(t, "√ " + p.Opciones[p.Correcta], EstiloTexto.Cuerpo, Tema.success);
                }
                Ui.Texto(t, "Hecho. Pulsa «Siguiente» para tomar las tres decisiones.", EstiloTexto.Pequeno, Tema.cyan);
                return;
            }

            var acertada = Preguntas.FirstOrDefault(p => p.Id == _ultimaAcertada);
            if (acertada != null) {
                Ui.Texto(t, "√ " + acertada.Opciones[acertada.Correcta], EstiloTexto.Cuerpo, Tema.success);
                Ui.Texto(t, acertada.Explicacion, EstiloTexto.Pequeno, Tema.inkMuted);
                Ui.Separador(t);
            }

            var indice = Preguntas.FindIndex(p => !_comprobacion.ContainsKey(p.Id));
            var actual = Preguntas[indice];
            Ui.Texto(t, $"{indice + 1}. {actual.Texto}", EstiloTexto.Cuerpo, Tema.ink).fontStyle = FontStyles.Bold;
            // Una mano de cartas: la buena se sella («¡CORRECTO!»); una mala se sacude y se queda apagada.
            var cartas = new List<OpcionDeCarta>();
            for (var j = 0; j < actual.Opciones.Count; j++) {
                var opcion = j;
                cartas.Add(new OpcionDeCarta {
                    Titulo = actual.Opciones[j],
                    Descartada = _opcionesFalladas.Contains(actual.Id + ":" + j),
                    EsCorrecta = () => opcion == actual.Correcta,
                    AlElegir = () => Contestar(actual, opcion),
                });
            }
            ManoDeCartas.Crear(Ui, Ui.Fila(t, "Mano", 0, alineacion: TextAnchor.MiddleCenter), cartas,
                               new ManoDeCartas.Ajustes { Sello = "¡CORRECTO!", ColorDelSello = Tema.success, AnchoMaximo = 980 });
            if (_ultimaFallada == actual.Id) Ui.Texto(t, "No es esa. Pista: " + actual.Explicacion, EstiloTexto.Pequeno, Tema.warning);
        }

        /// <summary>Las respuestas ya probadas y falladas («pregunta:opcion»): se pintan apagadas en la mano.</summary>
        private readonly HashSet<string> _opcionesFalladas = new HashSet<string>();

        private void Contestar(Nexus.Core.Proyecto.PreguntaDelProyecto p, int opcion) {
            if (opcion == p.Correcta) { _comprobacion[p.Id] = opcion; _ultimaFallada = null; _ultimaAcertada = p.Id; }
            else { _falladas.Add(p.Id); _ultimaFallada = p.Id; _opcionesFalladas.Add(p.Id + ":" + opcion); }
            _mantenerScroll = true;
            Repintar();
        }

        /// <summary>
        /// Lo que se va a medir y cuando sale bien (feedback beta #6: explicar las métricas antes del recorrido).
        /// Cada barra con su «?» y una línea de qué la mueve; abajo, lo que exige el lanzamiento de este nivel.
        /// </summary>
        private void PintarMetricas() {
            var t = _hoja.Tarjeta("Cómo se mide tu proyecto");
            GuiaView.Registrar("fase1.metricas", t);
            Ui.Texto(t, "Cada decisión mueve estas barras. Con ellas se decide el lanzamiento.",
                     EstiloTexto.Pequeno, Tema.ink);
            var metricas = new[] {
                new[] { "avance", "Avance", "sube con el trabajo de cada día; las horas extra lo suben más, pero cobran después." },
                new[] { "alcance", "Alcance", "lo prometido. Si aceptas pedir más, crece y hay más que entregar." },
                new[] { "deuda-tecnica", "Deuda técnica", "los atajos. Sube con prisas y horas extra; trae errores." },
                new[] { "cobertura", "Cobertura de pruebas", "cuánto está probado. Si es baja, el cliente encuentra errores." },
                new[] { "cansancio", "Cansancio", "sube al quedarte; baja al irte a casa. Alto = todo sale peor." },
                new[] { "moral", "Moral del equipo", "las ganas del equipo. Baja con promesas imposibles y noches largas." },
                new[] { "satisfaccion", "Satisfacción del cliente", "sube al escuchar, explicar y cumplir; baja con sorpresas." },
                new[] { "riesgo", "Riesgo latente", "junta cansancio, deuda y falta de pruebas: el peligro de entregar." }
            };
            // En dos columnas: ocho renglones seguidos no se leian, y asi la pagina cabe sin desplazarse.
            var rejilla = Ui.Fila(t, "Metricas", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);
            var columnas = new[] { Ui.Columna(rejilla, "Metricas A", Tema.Espacio(2)), Ui.Columna(rejilla, "Metricas B", Tema.Espacio(2)) };
            foreach (var c in columnas) UiKit.Tamano(c, ancho: 0, flexAncho: 1);
            for (var k = 0; k < metricas.Length; k++) {
                var m = metricas[k];
                var fila = Ui.Fila(columnas[k < (metricas.Length + 1) / 2 ? 0 : 1], espacio: 6);
                var texto = Ui.Texto(fila, $"<b>{m[1]}</b>: {m[2]}", EstiloTexto.Pequeno, Tema.ink);
                UiKit.Tamano(texto, flexAncho: 1);
                PantallaDeGlosario.Chip(App, fila, m[0]);
            }
            var u = Perfil.Lanzamiento;
            if (u != null)
                Ui.Texto(t, $"Para salir BIEN: {u.EntregaBien * 100:0} % entregado, hasta {u.DefectosBien} errores, riesgo bajo {u.RiesgoBien:0}, " +
                            $"cliente en {u.SatisfaccionBien:0} o más y decisiones justificadas.", EstiloTexto.Pequeno, Tema.cyan);
        }

        /// <summary>
        /// Las razones, en dos columnas y con su cuenta arriba, para que se vea que hay mas de cuatro (feedback #7).
        /// </summary>
        private void PintarRazones(Transform padre, IList<KeyValuePair<string, string>> razones, string elegida, System.Action<string> alElegir) {
            var titulo = Ui.Texto(padre, $"Elige 1 de estas {razones.Count} razones", EstiloTexto.Encabezado, Tema.ink);
            titulo.fontStyle = FontStyles.Bold;
            // Dos columnas que se reparten el ancho REAL (antes, celdas fijas de 760: en ventanas estrechas se salian
            // por la derecha, y el texto largo se salia de su celda de alto fijo).
            // Una mano de cartas que se puede cambiar (Seleccionable): con mas de cuatro razones, en rejilla.
            var rejilla = Ui.Fila(padre, "Razones", 0, alineacion: TextAnchor.MiddleCenter);
            GuiaView.Registrar("fase1.razones", rejilla);
            var cartas = new List<OpcionDeCarta>();
            var seleccionada = -1;
            foreach (var r in razones) {
                var id = r.Key;
                if (elegida == id) seleccionada = cartas.Count;
                cartas.Add(new OpcionDeCarta { Titulo = r.Value, AlElegir = () => alElegir(id) });
            }
            ManoDeCartas.Crear(Ui, rejilla, cartas, new ManoDeCartas.Ajustes {
                Seleccionable = true, Seleccionada = seleccionada, AnchoDeCarta = 300, AnchoMaximo = 1000
            });
        }

        /// <summary>«equipo de 4», o, si el equipo sale del recorrido y aun no se ha hecho, que todavia esta por formar.</summary>
        private string TextoDelEquipo() {
            if (S.EquipoDesdeRecoleccion && !S.RecoleccionHecha) return "equipo por formar en el recorrido";
            var equipo = S.EquipoDelProyecto();
            if (S.EquipoDesdeRecoleccion && S.TrabajasSolo) return "sin equipo: trabajas solo";
            return $"equipo de {Perfil.EquipoInicial}";
        }

        private void PintarPistas(Transform padre) {
            var pistas = S.PistasEncontradas();
            if (pistas.Count == 0) return;
            var t = Ui.Tarjeta(padre, "Lo que averiguaste en el recorrido");
            foreach (var p in pistas) Ui.Texto(t, "· " + p, EstiloTexto.Cuerpo);
        }

        // ==================================================================== 2 · la recoleccion 3D

        private void Recoleccion() {
            var cfg = S.Recoleccion;
            _hoja.Etiqueta("El recorrido por el plano");
            _hoja.Titulo("Recolección");
            if (!string.IsNullOrEmpty(cfg.Texto)) _hoja.Parrafo(cfg.Texto);

            var modulo = AppRoot.ModuloDeRecoleccion;
            if (!S.RecoleccionHecha) {
                var aviso = _hoja.Tarjeta("Esta parte es en 3D", Tono.Aviso);
                Ui.Texto(aviso, modulo != null
                             ? "Recorre el edificio en 3D durante 5 minutos: W A S D para moverte, el ratón para mirar, Esc para pausar. " +
                               "Las hojas de vida forman tu equipo; la pizza, el café y la Guepardex te ayudan después."
                             : "El recorrido en 3D lo está construyendo otro equipo. Mientras tanto, se simula: elige cómo de a fondo " +
                               "quieres recorrerlo. Cuanto más miras, más encuentras… y más tiempo gastas, y más cosas puedes tocar que no debías.",
                         EstiloTexto.Pequeno, Tema.ink);
            }

            var zonas = _hoja.Tarjeta($"El plano · {cfg.MinutosDisponibles} minutos");
            var visitadas = S.R.RecoleccionHecha ? new HashSet<string>(S.HallazgosRecogidos().Select(h => h.Zona)) : new HashSet<string>();
            foreach (var z in cfg.Zonas) {
                var cuantos = cfg.Hallazgos.Count(h => h.Zona == z.Id);
                Ui.Texto(zonas, $"<b>{z.Id} · {z.Nombre}</b>  ({z.MinutosDeVisita} min)  —  {z.Descripcion}  " +
                                (S.RecoleccionHecha ? "" : new string('?', cuantos)), EstiloTexto.Pequeno,
                         visitadas.Contains(z.Id) ? Tema.cyan : Tema.ink);
            }

            if (!S.RecoleccionHecha) {
                var fila = _hoja.Fila();
                GuiaView.Registrar("fase1.simular", fila);
                if (modulo != null) {
                    Ui.Boton(fila, "Empezar recolección", () => EmpezarRecoleccion(modulo), VarianteBoton.Primario);
                    return;
                }
                // Sin el mundo 3D en la build, el recorrido simulado de siempre.
                Ui.Boton(fila, "Simular recolección 3D: rápida", () => Simular(IntensidadesDeSimulacion.Rapida));
                Ui.Boton(fila, "Normal", () => Simular(IntensidadesDeSimulacion.Normal), VarianteBoton.Primario);
                Ui.Boton(fila, "A fondo", () => Simular(IntensidadesDeSimulacion.AFondo));
                return;
            }

            var resultado = _hoja.Tarjeta($"Lo que encontraste · {S.R.MinutosDeRecoleccion} minutos");
            var hallazgos = S.HallazgosRecogidos();
            var objetos = S.ObjetosRecogidos();
            if (hallazgos.Count == 0 && objetos.Count == 0) Ui.Texto(resultado, "Nada. A veces el recorrido no da nada.", EstiloTexto.Cuerpo);
            foreach (var h in hallazgos) Ui.Texto(resultado, DescribirHallazgo(h), EstiloTexto.Cuerpo,
                                                   h.Tipo == TiposDeHallazgo.Riesgo ? Tema.warning : Tema.ink);
            foreach (var kv in objetos)
                Ui.Texto(resultado, $"<b>{DescribirTipo(kv.Key.Tipo)}:</b> {kv.Key.Nombre}{(kv.Value > 1 ? "  ×" + kv.Value : "")}",
                         EstiloTexto.Cuerpo, Tema.ink);
            if (objetos.Count > 0)
                Ui.Boton(resultado, "Ver el resumen del recorrido", () => App.Router.Apilar<PantallaResumenDeRecoleccion>(), VarianteBoton.Fantasma);
            PintarEquipoFormado(_hoja.Raiz);
        }

        /// <summary>
        /// Quien va a trabajar contigo en el desarrollo, ya decidido: lo que se ve antes de cerrar la planificacion. Si
        /// salio de las hojas de vida, se dice; si no se encontro ninguna, se dice que vas solo.
        /// </summary>
        private string _avisoDeEquipo;

        /// <summary>
        /// Se recogieron mas hojas de vida de las que caben en el proyecto: el jugador elige quien entra. Cada boton
        /// mete o saca a esa persona; mientras no elija, entran las primeras.
        /// </summary>
        private void PintarEleccionDeEquipo(Transform padre) {
            var hojas = S.HojasRecogidas();
            var dentro = new HashSet<string>(S.EquipoDelProyecto().Select(m => m.Id));
            var max = S.MaxEquipo;
            var tarjeta = Ui.Tarjeta(padre, $"Elige tu equipo · {dentro.Count} de {max} plazas", Tono.Cyan);
            Ui.Texto(tarjeta, $"Encontraste {hojas.Count} hojas de vida; caben {max}. Elige quién entra.", EstiloTexto.Pequeno, Tema.ink);
            if (!string.IsNullOrEmpty(_avisoDeEquipo)) Ui.Texto(tarjeta, _avisoDeEquipo, EstiloTexto.Pequeno, Tema.warning);
            foreach (var o in hojas) {
                var id = o.Miembro.Id;
                var esta = dentro.Contains(id);
                var boton = Ui.BotonDeOpcion(tarjeta, (esta ? "✓ " : "+ ") + o.Miembro.Nombre + " · " + o.Miembro.Rol, o.Texto ?? o.Miembro.Descripcion, () => {
                    var nuevo = new HashSet<string>(dentro);
                    if (esta) nuevo.Remove(id); else nuevo.Add(id);
                    var motivo = S.PorQueNoSePuedeElegirEquipo(nuevo);
                    if (motivo == null) { S.ElegirEquipo(nuevo.ToList()); _avisoDeEquipo = null; }
                    else _avisoDeEquipo = nuevo.Count > max ? $"Ya tienes {max}: quita a alguien antes de añadir a otra persona." : motivo;
                    Repintar();
                });
                Ui.Resaltar(boton, esta);
            }
        }

        private void PintarEquipoFormado(Transform padre) {
            if (S.HayQueElegirEquipo && !S.Fase1Cerrada) { PintarEleccionDeEquipo(padre); return; }
            var equipo = S.EquipoDelProyecto();
            var solo = S.EquipoDesdeRecoleccion && S.TrabajasSolo;
            var tarjeta = Ui.Tarjeta(padre, solo ? "Tu equipo para el desarrollo · nadie" : $"Tu equipo para el desarrollo · {equipo.Count}",
                                     solo ? Tono.Aviso : Tono.Cyan);
            if (solo) {
                Ui.Texto(tarjeta, "Sin hojas de vida: sacarás el proyecto tú solo, y más despacio.",
                         EstiloTexto.Pequeno, Tema.warning);
                return;
            }
            foreach (var m in equipo) {
                Ui.FilaDeEquipo(tarjeta, m.Nombre, m.Rol, EstadoDeEquipo.Activo);
                // Lo que aporta, dicho en corto: asi se entiende por que importa a quien traes.
                var ayudaEn = Nexus.Core.Minijuegos.AndamiajeDelReto.EnQueAyuda(m);
                Ui.Texto(tarjeta, ayudaEn.Count > 0 ? "Te echa una mano al " + string.Join(" y al ", ayudaEn) + "."
                                                    : "Suma manos al tablero: con más gente, sale más trabajo al día.",
                         EstiloTexto.Pequeno, ayudaEn.Count > 0 ? Tema.success : Tema.inkMuted);
            }
            Ui.Texto(tarjeta, S.EquipoDesdeRecoleccion
                         ? "Las hojas de vida que encontraste. Las tendrás en «Equipo»."
                         : "Tu equipo asignado. Lo tendrás en «Equipo».",
                     EstiloTexto.Leyenda, Tema.cyan);
        }

        private static string DescribirTipo(string tipo) {
            switch (tipo) {
                case TiposDeObjeto3D.Candidato: return "Se une al equipo";
                case TiposDeObjeto3D.Suministro: return "Suministro";
                case TiposDeObjeto3D.Coleccionable: return "Para tu diario";
                case TiposDeObjeto3D.Efecto: return "Para el proyecto";
                default: return "Nota";
            }
        }

        private string DescribirHallazgo(Hallazgo h) {
            string que;
            switch (h.Tipo) {
                case TiposDeHallazgo.Pista: que = "Pista"; break;
                case TiposDeHallazgo.Personaje: que = "Se une"; break;
                case TiposDeHallazgo.Moral: que = "Para el equipo"; break;
                case TiposDeHallazgo.Recurso: que = "Recurso"; break;
                case TiposDeHallazgo.Coleccionable: que = "Para tu diario"; break;
                default: que = "Tropiezo"; break;
            }
            var texto = h.Tipo == TiposDeHallazgo.Coleccionable
                ? (App.Catalogo.Coleccionables.FirstOrDefault(c => c.Id == h.Id)?.Titulo ?? h.Id)
                : (h.Texto ?? h.Nombre);
            var efectos = h.Efectos != null && h.Efectos.Count > 0 ? "   (" + Textos.Previsualizar(h.Efectos) + ")" : "";
            return $"<b>{que}:</b> {texto}{efectos}";
        }

        private EntradaDeRecoleccion EntradaDelRecorrido() {
            return S.EntradaDeRecoleccion(App.PerfilActivo != null ? App.PerfilActivo.coleccionablesGlobales : null);
        }

        private void Simular(string intensidad) {
            AplicarRecoleccion(SimuladorDeRecoleccion.Simular(EntradaDelRecorrido(), intensidad));
        }

        /// <summary>
        /// Sale al mundo 3D. Lo elegido hasta ahora ya esta en el borrador (Repintar lo guarda), y esta pantalla
        /// sigue en la pila, apagada, hasta que el recorrido devuelve su resultado.
        /// </summary>
        private void EmpezarRecoleccion(IModuloDeRecoleccion modulo) {
            modulo.Empezar(App, EntradaDelRecorrido(), AplicarRecoleccion);
        }

        /// <summary>Lo que devuelve el recorrido (el 3D o su simulacion) se entrega al motor, una sola vez.</summary>
        private void AplicarRecoleccion(ResultadoDeRecoleccion resultado) {
            if (S.RecoleccionHecha) return;
            S.AplicarRecoleccion(resultado);
            foreach (var h in S.HallazgosRecogidos())
                if (h.Tipo == TiposDeHallazgo.Coleccionable) App.AnotarColeccionable(h.Id);
            // Las piezas del diario que se trajeron del 3D (cartas, el fragmento de USB).
            foreach (var kv in S.ObjetosRecogidos())
                if (kv.Key.Tipo == TiposDeObjeto3D.Coleccionable) App.AnotarColeccionable(kv.Key.Coleccionable);
            Repintar();
            // Lo que se trajo, dicho entero: quien forma el equipo, que suministros hay y que fue al diario.
            App.Router.Apilar<PantallaResumenDeRecoleccion>(p => p.SeAcaboElTiempo = !resultado.AbandonoAntes);
        }

        // ==================================================================== 3 · metodologia

        private void Metodologia() {
            _hoja.Etiqueta("Decisión 1 de 3");
            _hoja.Titulo("¿Cómo va a trabajar el equipo?");
            _hoja.Parrafo("Elige una metodología y el motivo. El motivo cuenta tanto como la elección.");

            // Las palabras del oficio, en un solo boton (antes, seis): quien ya las sabe no tiene que saltarlas.
            PantallaDeMenu.Boton(App, _hoja.Fila(), "Palabras que vas a ver ?", "Palabras del oficio", () => {
                var lista = new List<OpcionDeMenu>();
                foreach (var termino in new[] { "ciclo-de-vida", "sprint", "backlog", "ceremonia", "retrospectiva", "wip" }) {
                    var e = Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, termino);
                    if (e != null) lista.Add(new OpcionDeMenu(e.Termino, () => PantallaDePizarra.AbrirConcepto(App, e)));
                }
                return lista;
            });

            // Maestro y detalle: a la izquierda se elige, a la derecha se justifica. Antes las razones caian debajo de las
            // tarjetas y habia que desplazarse para verlas.
            // Las tres a la vista, una al lado de otra: antes iban en columna y la tercera (Kanban) quedaba por debajo
            // del borde, y habia que desplazarse para saber que existia. Los motivos y el calendario, debajo.
            var disponibles = S.MetodologiasDisponibles();
            var opcionesEnFila = Ui.Fila(_hoja.Raiz, "Metodologias", 0, alineacion: TextAnchor.MiddleCenter);
            GuiaView.Registrar("fase1.metodologias", opcionesEnFila);
            // Las metodologias, como una mano de cartas que se puede cambiar: la elegida lleva su sello «ELEGIDA».
            // Con frases cortas, el resumen largo se queda tras «¿Qué es …?» (la pizarra del concepto).
            var cartas = new List<OpcionDeCarta>();
            var seleccionada = -1;
            foreach (var m in disponibles) {
                var met = m;
                if (_metodologia == met.Id) seleccionada = cartas.Count;
                cartas.Add(new OpcionDeCarta {
                    Titulo = met.Nombre,
                    Detalle = App.Preferencias.modoTarjetas ? null : met.Resumen,
                    AlElegir = () => {
                        if (_metodologia != met.Id) { _razonMetodologia = null; _duracion = null; }   // otra metodologia, otros motivos y otro calendario
                        _metodologia = met.Id;
                        Repintar();
                    }
                });
            }
            ManoDeCartas.Crear(Ui, opcionesEnFila, cartas, new ManoDeCartas.Ajustes { Seleccionable = true, Seleccionada = seleccionada, AnchoMaximo = 1000 });
            // «¿Qué es …?» solo de la elegida.
            var queEs = _metodologia != null ? Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, _metodologia) : null;
            var elegidaParaAyuda = disponibles.FirstOrDefault(m => m.Id == _metodologia);
            if (queEs != null && elegidaParaAyuda != null)
                Ui.Boton(_hoja.Fila(), "¿Qué es " + elegidaParaAyuda.Nombre + "?", () => PantallaDePizarra.AbrirConcepto(App, queEs), VarianteBoton.Fantasma);

            var detalle = Ui.Columna(_hoja.Raiz, "Motivos", Tema.espacio);
            var elegida = disponibles.FirstOrDefault(m => m.Id == _metodologia);
            if (elegida == null) {
                Ui.Texto(detalle, "Elige una, arriba, para ver sus motivos.", EstiloTexto.Pequeno);
                return;
            }
            Ui.Texto(detalle, $"¿Por qué {elegida.Nombre}?", EstiloTexto.Subtitulo);
            var motivos = elegida.TextosDeRazones.OrderBy(x => x.Value, System.StringComparer.Ordinal).ToList();
            PintarRazones(detalle, motivos, _razonMetodologia, id => { _razonMetodologia = id; Repintar(); });
            PintarDuraciones(detalle, elegida);
        }

        /// <summary>
        /// Cuanto dura cada sprint, como se reparten las etapas o cada cuanto se entrega. Cada opcion enseña su
        /// calendario dibujado: las unidades a escala y los dias que cierran con un despliegue.
        /// </summary>
        private void PintarDuraciones(Transform padre, Nexus.Core.Metodologia.MethodologyProfile elegida) {
            var opciones = Nexus.Core.Metodologia.OpcionesDeCalendario.Para(elegida, Perfil.DiasTotales);
            if (opciones.Count < 2) return;
            var tipo = elegida.Calendario.Tipo;
            var pregunta = tipo == Nexus.Core.Metodologia.TiposDeCalendario.Iterativo ? $"¿Cuánto dura cada {elegida.Calendario.EtiquetaUnidad.ToLowerInvariant()}?"
                         : tipo == Nexus.Core.Metodologia.TiposDeCalendario.Continuo ? "¿Cada cuánto se revisa el flujo y se entrega?"
                         : "¿Cómo repartes los días entre las etapas?";
            var tarjeta = Ui.Tarjeta(padre, pregunta);
            GuiaView.Registrar("fase1.duracion", tarjeta);
            Ui.Texto(tarjeta, $"El nivel dura {Perfil.DiasTotales} días. No hay opción correcta: cada una cambia cuántas veces entregas.",
                     EstiloTexto.Pequeno, Tema.inkMuted);
            foreach (var o in opciones) {
                var opcion = o;
                var marcada = _duracion == null ? o.EsLaDeSerie : _duracion == o.Id;
                var boton = Ui.BotonDeOpcion(tarjeta, o.Titulo + (o.EsLaDeSerie ? "  ·  lo habitual" : ""), o.Detalle, () => {
                    _duracion = opcion.EsLaDeSerie ? null : opcion.Id;
                    Repintar();
                });
                Ui.Resaltar(boton, marcada);
                // El calendario, a escala: una caja por unidad y, debajo, los dias en que se despliega.
                var tira = Ui.Fila(tarjeta, "Calendario", 4);
                for (var i = 0; i < o.Unidades.Count; i++) {
                    var caja = Ui.PanelColumna(tira, "Unidad", Tema.Espacio(2), 0, marcada ? Tema.surfaceRaised : Tema.surfaceSunken);
                    UiKit.Tamano(caja, ancho: 0, flexAncho: o.Unidades[i]);
                    // El nombre de la etapa siempre (antes, los largos se cambiaban por su numero: «1», «3»), y los dias
                    // debajo: en dos lineas cabe aunque la etapa sea corta.
                    Ui.Texto(caja, $"{o.Nombres[i]}\n{o.Unidades[i]} d", EstiloTexto.Leyenda, marcada ? Tema.cyan : Tema.inkMuted, TextAlignmentOptions.Center);
                }
                Ui.Texto(tarjeta, o.Despliegues.Count == 1 ? $"Un solo despliegue, el día {o.Despliegues[0]}."
                                                           : $"{o.Despliegues.Count} despliegues: al cerrar los días {string.Join(", ", o.Despliegues)}.",
                         EstiloTexto.Leyenda, Tema.inkFaint);
            }
        }

        // ==================================================================== 4 · calidad

        private void Calidad() {
            var calidad = Perfil.Fase1.Calidad;
            var usadas = _fichas.Values.Sum();
            _hoja.Etiqueta("Decisión 2 de 3");
            _hoja.Titulo("¿A qué le dedicas tu cuidado?");
            if (!string.IsNullOrEmpty(calidad.TextoPresion)) _hoja.Parrafo(calidad.TextoPresion);
            _hoja.Subtitulo($"Fichas: {calidad.Fichas - usadas} de {calidad.Fichas} sin repartir",
                            usadas == calidad.Fichas ? Tema.success : Tema.warning);

            foreach (var atributo in calidad.Atributos) {
                var a = atributo;
                var fichas = _fichas[a.Id];
                var tarjeta = Ui.PanelColumna(_hoja.Raiz, a.Nombre, Tema.espacio, 4, Tema.surfaceRaised);
                var linea = Ui.Fila(tarjeta);
                var nombre = Ui.Texto(linea, a.Nombre, EstiloTexto.Cuerpo);
                nombre.fontStyle = FontStyles.Bold;
                UiKit.Tamano(nombre, flexAncho: 1);
                PantallaDeGlosario.Chip(App, linea, a.Id);
                var menos = Ui.Boton(linea, "−", () => { _fichas[a.Id]--; Repintar(); });
                UiKit.Tamano(menos, ancho: 64);
                menos.interactable = fichas > 0;
                UiKit.Tamano(Ui.Texto(linea, new string('●', fichas) + new string('○', Mathf.Max(0, calidad.Fichas / 2 - fichas)),
                                      EstiloTexto.Subtitulo, fichas > 0 ? Tema.cyan : Tema.inkMuted, TextAlignmentOptions.Center), ancho: 180);
                var mas = Ui.Boton(linea, "+", () => { _fichas[a.Id]++; Repintar(); });
                UiKit.Tamano(mas, ancho: 64);
                mas.interactable = usadas < calidad.Fichas;
                if (fichas == 0 && !string.IsNullOrEmpty(a.TextoSinInversion))
                    Ui.Texto(tarjeta, "Sin fichas: " + a.TextoSinInversion, EstiloTexto.Pequeno, Tema.warning);
            }
            if (usadas < calidad.Fichas) _hoja.Nota("Puedes dejar fichas sin usar, pero no se guardan para después.");
        }

        // ==================================================================== 5 · arquitectura

        private void Arquitectura() {
            var fase1 = Perfil.Fase1;
            _hoja.Etiqueta("Decisión 3 de 3");
            _hoja.Titulo("¿Qué forma va a tener lo que construyes?");
            var releer = _hoja.Fila();
            UiKit.Tamano(Ui.Texto(releer, "Vuelve a leer el encargo antes de elegir: la respuesta está ahí.", EstiloTexto.Cuerpo), flexAncho: 1);
            Ui.Boton(releer, "Releer el encargo", () => DialogosDelExpediente.Encargo(App, Perfil, S.PistasEncontradas()));

            // Las formas, como una mano de cartas que se puede cambiar; debajo, las ayudas de la elegida y sus razones.
            var fila = Ui.Fila(_hoja.Raiz, "Arquitecturas", 0, alineacion: TextAnchor.MiddleCenter);
            var cartas = new List<OpcionDeCarta>();
            var seleccionada = -1;
            foreach (var a in fase1.Arquitecturas) {
                var arq = a;
                if (_arquitectura == arq.Id) seleccionada = cartas.Count;
                cartas.Add(new OpcionDeCarta {
                    Titulo = arq.Nombre,
                    AlElegir = () => {
                        if (_arquitectura != arq.Id) _razonArquitectura = null;   // otra forma, otras razones
                        _arquitectura = arq.Id;
                        Repintar();
                    }
                });
            }
            ManoDeCartas.Crear(Ui, fila, cartas, new ManoDeCartas.Ajustes { Seleccionable = true, Seleccionada = seleccionada, AnchoMaximo = 1000 });
            if (_arquitectura != null) {
                // Las ayudas, solo de la elegida.
                var e = Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, _arquitectura);
                var ayudas = _hoja.Fila();
                if (e != null) Ui.Boton(ayudas, "¿Qué es?", () => PantallaDePizarra.AbrirConcepto(App, e), VarianteBoton.Fantasma);
                // Como quedaria ESTE sistema con esa forma: el plano con los modulos del proyecto.
                var id = _arquitectura;
                if (Perfil.Proyecto != null) Ui.Boton(ayudas, "Ver cómo quedaría", () => PantallaDelProducto.AbrirPlano(App, id, false), VarianteBoton.Fantasma);
            }

            var detalle = Ui.Columna(_hoja.Raiz, "Razones", Tema.espacio);
            var elegida = fase1.Arquitecturas.FirstOrDefault(a => a.Id == _arquitectura);
            if (elegida == null) {
                Ui.Texto(detalle, "Elige una forma, a la izquierda, para ver las razones que la justifican (o no).", EstiloTexto.Pequeno);
                return;
            }
            Ui.Texto(detalle, $"¿Por qué {elegida.Nombre}?", EstiloTexto.Subtitulo);
            // Cada forma trae SUS razones (feedback #8): elegir app o web no puede ofrecer los mismos argumentos.
            var razones = fase1.RazonesDe(elegida).Select(r => new KeyValuePair<string, string>(r.Id, r.Texto)).ToList();
            PintarRazones(detalle, razones, _razonArquitectura, id => { _razonArquitectura = id; Repintar(); });
        }

        // ==================================================================== 6 · el product backlog

        /// <summary>Con menos historias que modulos no hay sistema que construir.</summary>
        private int MinimoDeHistorias { get { return Perfil.Proyecto == null ? 1 : Mathf.Max(1, Perfil.Proyecto.Modulos.Count); } }

        /// <summary>
        /// El product backlog inicial. Si el proyecto empieza de cero, se arma: a la izquierda las historias candidatas
        /// de cada parte del sistema (unas las pidio el cliente; otras no deberian entrar), a la derecha el backlog en
        /// el orden en que se hara. Si el proyecto ya estaba en marcha, se enseña el que hay y por que ya existe.
        /// </summary>
        private void Backlog() {
            var f = Perfil.Proyecto;
            _hoja.Etiqueta("Antes de construir");
            if (!S.SeCreaElBacklog) {
                _hoja.Titulo("El product backlog ya existe");
                _hoja.Parrafo("Alguien ya empezó: esto es lo que falta, parte por parte. Durante el desarrollo lo ordenas en tu muro.");
                BotonDePalabras("backlog", "historia-de-usuario", "mvp");
                var partes = Ui.Fila(_hoja.Raiz, "Partes", Tema.espacio, 0, TextAnchor.UpperLeft);
                for (var i = 0; i < f.Modulos.Count; i++) {
                    var m = f.Modulos[i];
                    var color = ColorDeModulo(i);
                    var ficha = Ui.PanelColumna(partes, "Parte " + m.Nombre, Tema.Espacio(3), Tema.Espacio(2), Tema.surfaceRaised);
                    UiKit.Tamano(ficha, ancho: 0, flexAncho: 1);
                    ficha.localRotation = Quaternion.Euler(0, 0, (i % 3 - 1) * 0.6f);
                    PiezasDelMuro.Cinta(Ui, ficha, m.Nombre, color, i % 2 == 0 ? -1.4f : 1.1f);
                    UiKit.Tamano(Ui.Barra(ficha, (float)(m.AvanceInicial / 100.0), color), flexAncho: 1);
                    Ui.Texto(ficha, m.AvanceInicial > 0 ? $"{m.AvanceInicial:0} % hecho" : "sin empezar", EstiloTexto.Leyenda, color);
                    if (!App.Preferencias.modoTarjetas) Ui.Texto(ficha, m.QueHace, EstiloTexto.Pequeno, Tema.inkMuted);
                }
                return;
            }

            _hoja.Titulo("Arma el product backlog");
            _hoja.Parrafo("Arrastra a tu backlog lo que el cliente necesita, en el orden en que se hará. No todo debe entrar.");
            BotonDePalabras("backlog", "historia-de-usuario", "mvp", "alcance");
            var capa = CapaDeArrastre();

            if (string.IsNullOrEmpty(_moduloVisto) || f.Modulos.All(x => x.Id != _moduloVisto)) _moduloVisto = f.Modulos.Count > 0 ? f.Modulos[0].Id : null;
            var escena = Ui.Fila(_hoja.Raiz, "Backlog", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);

            // ---- a la izquierda: el monton de historias, una parte del sistema cada vez (pestañas con su punto)
            var candidatas = Ui.Columna(escena, "Candidatas", Tema.espacio);
            UiKit.Tamano(candidatas, ancho: 0, flexAncho: 3);
            GuiaView.Registrar("fase1.historias", candidatas);
            var pestanas = Ui.Fila(candidatas, "Partes", Tema.Espacio(2), 0, TextAnchor.MiddleLeft);
            for (var i = 0; i < f.Modulos.Count; i++) {
                var m = f.Modulos[i];
                if (f.Historias.All(x => x.Modulo != m.Id)) continue;
                var cubierta = _backlog.Any(id => { var h = f.Historia(id); return h != null && h.Modulo == m.Id; });
                var pestana = Ui.Fila(pestanas, "Pestaña", 6, 0, TextAnchor.MiddleLeft);
                Ui.Punto(pestana, cubierta ? ColorDeModulo(i) : NexusTheme.Alfa(Tema.inkFaint, 0.5f), 10);
                var id = m.Id;
                var boton = Ui.Boton(pestana, m.Nombre, () => { _moduloVisto = id; _mantenerScroll = true; Repintar(); }, VarianteBoton.Fantasma);
                Ui.Resaltar(boton, m.Id == _moduloVisto);
            }
            var indiceVisto = f.Modulos.FindIndex(x => x.Id == _moduloVisto);
            var modulo = indiceVisto < 0 ? null : f.Modulos[indiceVisto];
            if (modulo != null && !App.Preferencias.modoTarjetas) Ui.Texto(candidatas, modulo.QueHace, EstiloTexto.Pequeno, Tema.inkMuted);
            var monton = Ui.Columna(candidatas, "Monton", 12, 10);
            Ui.Fondo(monton.gameObject.AddComponent<Image>(), new Color(1, 1, 1, 0.03f), 16);
            UiKit.Tamano(monton, flexAncho: 1).minHeight = 240;
            var destinoMonton = monton.gameObject.AddComponent<DestinoDeFicha>();
            destinoMonton.Ui = Ui;
            destinoMonton.Acento = Tema.inkMuted;
            destinoMonton.Radio = 16;
            destinoMonton.Motivo = (ficha, _) => null;
            destinoMonton.Coste = ficha => ficha.Origen == "backlog" ? "sacarla" : null;
            destinoMonton.AlRecibir = (ficha, _) => {
                if (ficha.Origen != "backlog") return;
                _backlog.Remove(ficha.Id);
                _mantenerScroll = true;
                Repintar();
            };
            var quedan = modulo == null ? new List<Nexus.Core.Proyecto.HistoriaCandidata>()
                                        : f.Historias.Where(x => x.Modulo == modulo.Id && !_backlog.Contains(x.Id)).ToList();
            if (quedan.Count == 0) PiezasDelMuro.Vacio(Ui, monton, "De esta parte ya no queda nada en el montón.");
            foreach (var h in quedan) FichaDeHistoria(monton, capa, h, indiceVisto, 0, "monton");

            // ---- a la derecha: tu backlog, numerado, con lo de arriba primero
            var lado = Ui.Columna(escena, "Elegidas", Tema.espacio);
            UiKit.Tamano(lado, ancho: 0, flexAncho: 2);
            RectTransform lista;
            DestinoDeFicha destino;
            var carril = PiezasDelMuro.Carril(Ui, lado, "Tu product backlog", _backlog.Count.ToString(), Tema.cyan, out lista, out destino);
            UiKit.Tamano(carril, alto: 620, flexAncho: 1);
            GuiaView.Registrar("fase1.mibacklog", carril);
            var arriba = Ui.Fila(lista, "Arriba", 8, 0, TextAnchor.MiddleLeft);
            PiezasDelMuro.Flecha(Ui, arriba, 26, 34, true, Tema.cyan);
            Ui.Texto(arriba, "arriba, lo que más le vale al cliente", EstiloTexto.Leyenda, Tema.cyan);
            for (var i = 0; i < _backlog.Count; i++) {
                var h = f.Historia(_backlog[i]);
                if (h == null) continue;
                FichaDeHistoria(lista, capa, h, f.Modulos.FindIndex(x => x.Id == h.Modulo), i + 1, "backlog");
            }
            // Los huecos que faltan: uno por cada parte del sistema que aun no tiene ninguna historia.
            if (_backlog.Count < MinimoDeHistorias)
                foreach (var m in f.Modulos.Where(m => !_backlog.Any(id => { var h = f.Historia(id); return h != null && h.Modulo == m.Id; })))
                    PiezasDelMuro.Vacio(Ui, lista, "falta una de " + m.Nombre);
            destino.Motivo = (ficha, _) => null;
            destino.AlRecibir = (ficha, puesto) => {
                if (Nexus.Core.Tablero.OrdenDeLista.Poner(_backlog, ficha.Id, Mathf.Max(0, puesto))) {
                    _mantenerScroll = true;
                    Repintar();
                }
            };

            // ---- lo que pidio el cliente, como una nota pegada con cinta
            var nota = PiezasDelMuro.Panel(Ui, lado, "", Tema.violet, 14);
            var cabecera = Ui.Fila(nota, "Cabecera", 8, 0, TextAnchor.MiddleLeft);
            var titulo = Ui.Texto(cabecera, "¿QUÉ PIDIÓ EL CLIENTE?", EstiloTexto.Leyenda, Tema.violet);
            UiKit.Tamano(titulo, flexAncho: 1);
            Ui.Boton(cabecera, _notaDelClienteAbierta ? "Plegar" : "Desplegar", () => { _notaDelClienteAbierta = !_notaDelClienteAbierta; _mantenerScroll = true; Repintar(); },
                     VarianteBoton.Fantasma);
            var caras = Ui.Fila(nota, "Quienes", 6, 0, TextAnchor.MiddleLeft);
            foreach (var u in f.Usuarios) Ui.Retrato(caras, string.IsNullOrEmpty(u.Nombre) ? "?" : u.Nombre.Substring(0, 1), Tono.Violeta, 30);
            if (_notaDelClienteAbierta) {
                foreach (var u in f.Usuarios) Ui.Texto(nota, $"<b>{u.Nombre}</b>: {u.QueNecesita}", EstiloTexto.Pequeno, Tema.ink);
                Ui.Texto(nota, "Lo que nadie de aquí necesita, nadie lo pidió.", EstiloTexto.Pequeno, Tema.inkMuted).fontStyle = FontStyles.Italic;
            }
        }

        private string _moduloVisto;
        private bool _notaDelClienteAbierta = true, _palabrasDelBacklog;
        private RectTransform _capaDeArrastre;

        private RectTransform CapaDeArrastre() {
            if (_capaDeArrastre == null) _capaDeArrastre = PiezasDelMuro.CapaDeArrastre(Ui, Raiz);
            _capaDeArrastre.SetAsLastSibling();
            return _capaDeArrastre;
        }

        /// <summary>Las palabras del glosario, detras de «?»: el paso se lee sin ellas.</summary>
        private void BotonDePalabras(params string[] palabras) {
            var fila = _hoja.Fila();
            Ui.Boton(fila, _palabrasDelBacklog ? "Ocultar palabras" : "? Palabras", () => { _palabrasDelBacklog = !_palabrasDelBacklog; _mantenerScroll = true; Repintar(); },
                     VarianteBoton.Fantasma);
            if (_palabrasDelBacklog) PantallaDeGlosario.Palabras(App, fila, "", palabras);
        }

        private Color ColorDeModulo(int i) {
            var paleta = new[] { Tema.cyan, Tema.violet, Tizador.Mostaza, Tema.success, Tizador.Roja, Tizador.Cian };
            return paleta[Mathf.Abs(i) % paleta.Length];
        }

        /// <summary>
        /// Una historia como ficha: su titulo y «como / quiero / para» (sin el valor ni si la pidieron: eso es lo que se
        /// decide). Un clic la mete al final del backlog, o la saca; arrastrarla la pone en su puesto.
        /// </summary>
        private void FichaDeHistoria(RectTransform padre, RectTransform capa, Nexus.Core.Proyecto.HistoriaCandidata h, int indiceDeModulo, int numero, string origen) {
            var lineas = App.Preferencias.modoTarjetas || string.IsNullOrEmpty(h.Como) ? null : new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("como", h.Como),
                new KeyValuePair<string, string>("quiero", h.Quiero),
                new KeyValuePair<string, string>("para", h.Para)
            };
            var ficha = PiezasDelMuro.Ficha(Ui, padre, new DatosDeFicha {
                Id = h.Id, Titulo = h.Titulo, Numero = numero, Lineas = lineas, Cinta = ColorDeModulo(indiceDeModulo)
            }, capa);
            ficha.Origen = origen;
            var id = h.Id;
            ficha.AlPulsar = () => {
                if (!_backlog.Remove(id)) _backlog.Add(id);
                _mantenerScroll = true;
                Repintar();
            };
            ficha.AlFallar = motivo => PiezasDelMuro.Aviso(Ui, capa, motivo, Tema.warning);
        }

        // ==================================================================== 7 · resumen

        private void Resumen() {
            _hoja.Etiqueta("Antes de empezar");
            _hoja.Titulo("Tu plan");
            _hoja.Parrafo("Revísalo. Puedes cambiar cualquier cosa; cuando confirmes, ya no hay vuelta atrás: empieza el día 1.");

            var met = S.MetodologiasDisponibles().FirstOrDefault(m => m.Id == _metodologia);
            var duracion = met == null ? null : Nexus.Core.Metodologia.OpcionesDeCalendario.Para(met, Perfil.DiasTotales)
                               .FirstOrDefault(o => _duracion == null ? o.EsLaDeSerie : o.Id == _duracion);
            Linea("Metodología", met == null ? null : $"{met.Nombre} — porque «{met.TextoDe(_razonMetodologia)}»" +
                                                       (duracion != null && !string.IsNullOrEmpty(duracion.Titulo) ? $"\n{duracion.Titulo}" : ""),
                  Paso.Metodologia, _razonMetodologia != null);
            var reparto = string.Join(" · ", Perfil.Fase1.Calidad.Atributos.Where(a => _fichas[a.Id] > 0).Select(a => $"{a.Nombre} {_fichas[a.Id]}"));
            Linea("Calidad", string.IsNullOrEmpty(reparto) ? "sin fichas repartidas" : reparto, Paso.Calidad, true);
            var arq = Perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == _arquitectura);
            var razon = Perfil.Fase1.RazonesDisponibles.FirstOrDefault(r => r.Id == _razonArquitectura);
            Linea("Arquitectura", arq == null ? null : $"{arq.Nombre} — porque «{razon?.Texto}»", Paso.Arquitectura, razon != null);
            if (S.SeCreaElBacklog)
                Linea("Product backlog", _backlog.Count == 0 ? null : $"{_backlog.Count} historias; la primera, «{Perfil.Proyecto.Historia(_backlog[0]).Titulo}»",
                      Paso.Backlog, _backlog.Count >= MinimoDeHistorias);
            if (HayRecoleccion)
                Linea("Recorrido", S.RecoleccionHecha ? $"{S.HallazgosRecogidos().Count + S.ObjetosRecogidos().Count} hallazgos en {S.R.MinutosDeRecoleccion} min · {TextoDelEquipo()}" : null,
                      Paso.Recoleccion, S.RecoleccionHecha);
            // Antes de confirmar se ve con quien se va a trabajar (si el equipo sale del recorrido, solo cuando ya se hizo).
            if (!S.EquipoDesdeRecoleccion || S.RecoleccionHecha) PintarEquipoFormado(_hoja.Raiz);

            var listo = Pasos().Where(p => p != Paso.Resumen).All(Completo);
            var confirmar = _hoja.Accion("Confirmar y empezar el nivel", Confirmar);
            GuiaView.Registrar("fase1.confirmar", confirmar);
            confirmar.interactable = listo;
            if (!listo) _hoja.Nota("Falta algo por decidir: lo que no tiene √ arriba.", Tema.warning);
        }

        private void Linea(string que, string valor, Paso paso, bool completo) {
            var tarjeta = Ui.PanelColumna(_hoja.Raiz, que, Tema.espacio, 4, Tema.surfaceRaised);
            var fila = Ui.Fila(tarjeta);
            var textos = Ui.Columna(fila, espacio: 2);
            UiKit.Tamano(textos, flexAncho: 1);
            Ui.Texto(textos, que.ToUpperInvariant(), EstiloTexto.Leyenda, Tema.cyan);
            Ui.Texto(textos, valor ?? "Sin decidir", EstiloTexto.Cuerpo, completo ? Tema.ink : Tema.warning);
            Ui.Boton(fila, "Cambiar", () => IrA(paso), VarianteBoton.Fantasma);
        }

        private void Confirmar() {
            if (Preguntas.Count > 0)
                S.RegistrarComprensionDelProyecto(Preguntas.Count(p => _comprobacion.ContainsKey(p.Id) && !_falladas.Contains(p.Id)), Preguntas.Count);
            S.ElegirMetodologia(_metodologia, _razonMetodologia);
            if (!string.IsNullOrEmpty(_duracion) && S.OpcionesDeCalendarioDisponibles().Exists(o => o.Id == _duracion)) S.AjustarCalendario(_duracion);
            S.RepartirCalidad(_fichas.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value));
            S.ElegirArquitectura(_arquitectura, _razonArquitectura);
            if (S.SeCreaElBacklog && _backlog.Count > 0) S.CrearBacklog(_backlog);
            S.R.Fase1Borrador = null;
            // La historia reacciona a la arquitectura (Voss, en N1) con la variante «tras-<id>» del guion de la fase.
            ReproducirGuionesDeFase1("tras-" + _arquitectura, () => {
                S.CerrarFase1();
                FlujoDelNivel.TrasLaFase1(App);
            });
        }

        private void IrA(Paso paso, bool porElFinal = false) {
            _paso = paso;
            _subPaso = porElFinal ? PartesDelPaso - 1 : 0;
            Repintar();
            GuiaView.Avisar(App, AvisoDelPasoActual());
        }
    }
}
