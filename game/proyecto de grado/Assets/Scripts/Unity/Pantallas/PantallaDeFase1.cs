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
    public sealed partial class PantallaDeFase1 : Pantalla {
        private enum Paso { Encargo, Recoleccion, Metodologia, Calidad, Arquitectura, Backlog, Resumen }

        /// <summary>El product backlog que se va armando: ids de historias, en el orden en que se haran.</summary>
        private readonly List<string> _backlog = new List<string>();

        private GameSession S { get { return App.Sesion; } }
        private LevelProfile Perfil { get { return S.Perfil; } }
        private bool HayRecoleccion { get { return S.Recoleccion != null; } }

        private Paso _paso = Paso.Encargo;
        /// <summary>La parte del expediente que se esta leyendo, dentro del paso «El proyecto».</summary>
        private int _subPaso;
        // El proyecto va en tres partes (cliente · sistema y como se mide · comprueba): ver PantallaDeFase1.Proyecto.cs.
        private static readonly string[] AvisoDeCadaParte = { "fase1.encargo", "fase1.encargo.sistema", "fase1.encargo.comprueba" };
        private bool _guionInicialPendiente = true, _avisarAlEmpezar;
        private Hoja _hoja;
        private RectTransform _navegacion;
        private readonly Dictionary<Paso, Button> _chips = new Dictionary<Paso, Button>();
        private ScrollRect _scroll;

        private string _metodologia, _razonMetodologia, _arquitectura, _razonArquitectura;
        /// <summary>Cuanto dura cada sprint, etapa o revision (OpcionesDeCalendario). null = la de serie.</summary>
        private string _duracion;
        private readonly Dictionary<string, int> _fichas = new Dictionary<string, int>();
        /// <summary>La vista de Calidad en la que se esta: false = tarjetas, true = tabla ISO 25010.</summary>
        private bool _calidadEnTabla;

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

            // El protagonista pensando a la izquierda y la decision a su lado, SIN ventana de metal ni paneles
            // grises (2026-11-07): el contenido va directamente sobre la escena.
            var escena = Ui.Fila(marco, "Escena", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(escena, flexAncho: 1, flexAlto: 1);
            var protagonista = Ui.Ilustracion(escena, "personajes/protagonista", "Tú", 300);
            UiKit.Tamano(protagonista, flexAlto: 1);
            var panel = Ui.Columna(escena, "Contenido", Tema.espacio);
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
            _avanceProgramado++;   // un «pasa sola a la parte siguiente» pendiente ya no vale: se esta repintando
            GuardarBorrador();
            foreach (var kv in _chips) {
                var hecho = Completo(kv.Key) && kv.Key != _paso;
                Ui.Resaltar(kv.Value, kv.Key == _paso, hecho ? Tema.successSoft : Tema.surfaceRaised);
                kv.Value.GetComponentInChildren<TMP_Text>().text = Nexus.Core.Narrativa.Glifos.Seguro((hecho ? "√ " : "") + NombreDe(kv.Key));
            }
            var posicion = _scroll.content.anchoredPosition;
            _hoja.Vaciar();
            // Sin marcos ni paneles grises en toda la Fase 1: mientras se pinta, UiKit crea paneles y tarjetas
            // transparentes. Se apaga siempre al terminar, que el kit es compartido con el resto del juego.
            Ui.SinMarcos = true;
            try {
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
            } finally {
                Ui.SinMarcos = false;
            }
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

        /// <summary>
        /// Cuantas partes tiene el paso en el que se esta: el expediente y la metodologia (elegir · por que · duracion,
        /// 2026-11-08) van por partes; lo demas, de una pieza.
        /// </summary>
        private int PartesDelPaso {
            get {
                if (_paso == Paso.Encargo && Perfil.Proyecto != null) return PartesDeProyecto;
                if (_paso == Paso.Metodologia) return PartesDeMetodologia;
                if (_paso == Paso.Arquitectura) return PartesDeArquitectura;
                if (_paso == Paso.Backlog) return PartesDelBacklog;
                return 1;
            }
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
                // En la metodologia, no se pasa a la parte siguiente sin haber elegido en esta.
                var siguienteParte = Ui.Boton(_navegacion, "Siguiente  ►", () => IrAParte(_subPaso + 1), VarianteBoton.Primario);
                siguienteParte.interactable = ParteCompleta();
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

            EncargoEnPartes(f);   // ilustrado y sin scroll: ver PantallaDeFase1.Proyecto.cs
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


        /// <summary>Las respuestas ya probadas y falladas («pregunta:opcion»): se pintan apagadas en la mano.</summary>
        private readonly HashSet<string> _opcionesFalladas = new HashSet<string>();


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

        // Recoleccion(), ilustrada y sin scroll (antes y despues del recorrido): ver PantallaDeFase1.Recoleccion.cs.

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
            // Ya no se abre el resumen encima (2026-11-08): la propia pantalla lo enseña (equipo en tarjetas, la franja
            // de lo que se trajo y como acabo el recorrido); el resumen entero queda tras «Ver el resumen».
            _seAcaboElTiempo = !resultado.AbandonoAntes;
            Repintar();
        }

        // ==================================================================== 3 · metodologia
        // Metodologia(), en tres partes (elegir · por que · duracion): ver PantallaDeFase1.Metodologia.cs.

        // ==================================================================== 4 · calidad

        private void Calidad() {
            var calidad = Perfil.Fase1.Calidad;

            // Esta seccion NUNCA hace scroll: todo va en un lienzo que mide exactamente el visor, y si el contenido
            // pide mas, se encoge (EscaladorAlCaber). Ver CalidadView.
            var hoja = LienzoSinScroll("AjusteDeCalidad");
            var interior = hoja.Raiz;

            hoja.Etiqueta("Decisión 2 de 3");
            hoja.Titulo("¿A qué le dedicas tu cuidado?");
            if (!string.IsNullOrEmpty(calidad.TextoPresion)) hoja.Parrafo(calidad.TextoPresion);

            // El reparto vive en dos vistas (tarjetas y tabla ISO 25010) que se cambian con movimiento. Sus +/− no
            // repintan la pantalla: guardan el borrador y se actualizan por dentro.
            new CalidadView(Ui, interior, Raiz, calidad, _fichas, GuardarBorrador,
                            (padre, id) => PantallaDeGlosario.Chip(App, padre, id),
                            _calidadEnTabla, enTabla => _calidadEnTabla = enTabla).Construir();
        }

        // ==================================================================== 5 · arquitectura

        // Arquitectura(), en dos partes (elegir · por que): ver PantallaDeFase1.Arquitectura.cs.

        // ==================================================================== 6 · el product backlog

        /// <summary>Con menos historias que modulos no hay sistema que construir.</summary>
        private int MinimoDeHistorias { get { return Perfil.Proyecto == null ? 1 : Mathf.Max(1, Perfil.Proyecto.Modulos.Count); } }

        // Backlog(), en dos partes (lo que pide el cliente · arma tu backlog): ver PantallaDeFase1.Backlog.cs.

        // ==================================================================== 7 · resumen

        // Resumen(), como cabina de lanzamiento: ver PantallaDeFase1.Resumen.cs.

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
            // A la metodologia se entra por la primera parte que falte (elegir, o el porque).
            if (paso == Paso.Metodologia && !porElFinal && _metodologia != null && _razonMetodologia == null) _subPaso = 1;
            if (paso == Paso.Arquitectura && !porElFinal && _arquitectura != null) _subPaso = 1;
            Repintar();
            GuiaView.Avisar(App, AvisoDelPasoActual());
        }
    }
}
