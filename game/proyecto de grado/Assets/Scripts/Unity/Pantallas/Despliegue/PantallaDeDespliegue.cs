using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Despliegue;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Una pieza que se arrastra por la lamina en las dos direcciones (un tramo de tuberia, un cable). Al soltarla
    /// dice donde quedo su centro, en coordenadas de la lamina; quien la creo decide que pasa y vuelve a pintar.
    /// Un clic sin arrastre tambien cuenta: todo lo que se hace arrastrando se puede hacer pulsando.
    /// </summary>
    public sealed class PiezaArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {
        public Action<Vector2> AlSoltar;
        public Action AlPulsar;
        private RectTransform _rt;

        public void OnBeginDrag(PointerEventData e) {
            _rt = (RectTransform)transform;
            _rt.SetAsLastSibling();
            _rt.localRotation = Quaternion.Euler(0, 0, 1.5f);
        }

        public void OnDrag(PointerEventData e) {
            if (_rt == null) return;
            var padre = (RectTransform)_rt.parent;
            Vector2 local, anterior;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position, e.pressEventCamera, out local);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position - e.delta, e.pressEventCamera, out anterior);
            _rt.anchoredPosition += local - anterior;
        }

        public void OnEndDrag(PointerEventData e) {
            if (_rt == null) return;
            _rt.localRotation = Quaternion.identity;
            var centro = new Vector2(_rt.anchoredPosition.x + _rt.sizeDelta.x / 2, -_rt.anchoredPosition.y + _rt.sizeDelta.y / 2);
            AlSoltar?.Invoke(centro);
        }

        public void OnPointerClick(PointerEventData e) {
            if (!e.dragging) AlPulsar?.Invoke();
        }
    }

    /// <summary>
    /// La Fase 3: desplegar. Con una sola cuenta atras para todo:
    ///   1 · La tuberia — se arrastran los tramos (compilar, probar, ensayar, produccion, vigilar…) a los huecos del
    ///       camino entre el codigo y el cliente. Hay mas tramos que huecos, y algunos son atajos.
    ///   2 · Los modulos — uno por cada modulo que sale hoy: cada cable a su toma del entorno. Hay tomas de pruebas
    ///       y claves a la vista; al encender, el modulo prende o salta.
    /// Al acabar (o al agotarse el tiempo) se enseña renglon a renglon que estuvo bien y que falto, y lo que le hace
    /// al proyecto. Las reglas estan en EvaluadorDeDespliegue; esto solo pinta y recoge la respuesta.
    /// </summary>
    public sealed class PantallaDeDespliegue : Pantalla {
        public Action AlTerminar;

        public override bool PuedeVolver { get { return false; } }

        private enum Paso { Intro, Tuberia, Modulo, Resultado }

        private const float W = 1300, H = 640;

        private DespliegueDef _def;
        private PlanDeDespliegue _plan;
        private Nexus.Core.Proyecto.FichaDelProyecto _ficha;
        private Paso _paso = Paso.Intro;
        private string[] _huecos;
        private readonly Dictionary<string, Dictionary<string, string>> _enchufes = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private int _modulo;
        private bool _encendido;            // el modulo actual ya se encendio: se ve como quedo y se pasa al siguiente
        private string _cableElegido, _tramoMirado;
        private float _restante, _total;
        private ResultadoDeDespliegue _resultado;

        private RectTransform _cuerpo;
        private TemporizadorView _reloj;
        private Lamina _lamina;
        private TMP_Text _info, _titulo;
        private Button _principal;

        protected override void Construir() {
            var s = App.Sesion;
            _def = s.Perfil.Despliegue;
            _ficha = s.Perfil.Proyecto;
            _plan = s.PlanDelDespliegue();
            _huecos = new string[Math.Max(1, _def.Tuberia.Huecos)];
            foreach (var m in _plan.Modulos) _enchufes[m] = new Dictionary<string, string>(StringComparer.Ordinal);
            _total = _restante = _plan.Segundos;

            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.espacio, Tema.margen * 0.75f));
            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, ("Fase 3 · despliegue · día " + _plan.Dia + (_plan.EsFinal ? " · el último" : "")).ToUpperInvariant(), EstiloTexto.Leyenda, Tema.warning);
            _titulo = Ui.Texto(titulos, "", EstiloTexto.Subtitulo, Tema.ink);
            GuiaView.BotonDeAyuda(App, cabecera);
            _reloj = Ui.Temporizador(cabecera, Reloj(_total), Tono.Cyan);

            _cuerpo = Ui.Fila(marco, "Cuerpo", Tema.espacio * 1.5f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(_cuerpo, flexAncho: 1, flexAlto: 1);
            PintarIntro();
        }

        private static string Reloj(float segundos) {
            var s = Mathf.CeilToInt(Mathf.Max(0, segundos));
            return $"{s / 60}:{s % 60:00}";
        }

        private string NombreDe(string modulo) {
            var m = _ficha == null ? null : _ficha.Modulo(modulo);
            return m == null ? modulo : m.Nombre;
        }

        private void Update() {
            if (_paso != Paso.Tuberia && _paso != Paso.Modulo) return;
            if (GuiaView.PausaActiva || PantallaDePausa.Abierta) return;
            _restante -= Time.unscaledDeltaTime;
            var f = Mathf.Max(0, _restante) / Mathf.Max(10f, _total);
            _reloj.Mostrar(Reloj(_restante), f < 0.15f ? Tono.Peligro : f < 0.3f ? Tono.Aviso : Tono.Cyan);
            if (_restante <= 0) Entregar(true);
        }

        // ==================================================================== 0 · que se despliega hoy

        private void PintarIntro() {
            _paso = Paso.Intro;
            _titulo.text = "Hoy sale a producción lo que está listo";
            UiKit.Vaciar(_cuerpo);
            var panel = Ui.PanelColumna(_cuerpo, "Intro", Tema.margen, Tema.espacio);
            UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(panel, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var hoja = new Hoja(Ui, contenido);

            hoja.Parrafo(_plan.EsFinal
                ? "Se acabaron los días de desarrollo. Antes de la evaluación, lo construido tiene que llegar al cliente: eso es desplegar."
                : "Cerraste una etapa de trabajo. Lo que ya está listo puede llegar hoy al cliente: eso es desplegar. Mañana se sigue construyendo.");

            var salen = hoja.Tarjeta(_plan.Modulos.Count == 0 ? "Hoy no sale ningún módulo" : "Lo que sale hoy", Tono.Cyan);
            if (_plan.Modulos.Count == 0) Ui.Texto(salen, "Ninguno llega todavía al mínimo para entregarse. Aun así hay que dejar armado el camino a producción.", EstiloTexto.Cuerpo, Tema.ink);
            foreach (var m in _plan.Modulos) {
                var modulo = _ficha == null ? null : _ficha.Modulo(m);
                Ui.Texto(salen, $"· <b>{NombreDe(m)}</b>" + (modulo == null ? "" : ": " + modulo.QueHace), EstiloTexto.Cuerpo, Tema.ink);
            }
            var arq = App.Sesion.Perfil.Fase1 == null ? null : App.Sesion.Perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == App.Sesion.ArquitecturaElegida);
            if (arq != null) {
                var comoSale = hoja.Tarjeta("Cómo sale con tu arquitectura · " + arq.Nombre);
                Ui.Texto(comoSale, DibujosDeArquitectura.AlDesplegar(arq.Id), EstiloTexto.Cuerpo, Tema.ink);
                Ui.Boton(Ui.Fila(comoSale), "Ver el plano", () => PantallaDelProducto.AbrirPlano(App, arq.Id, true), VarianteBoton.Fantasma);
            }
            if (_plan.YaDesplegados.Count > 0)
                hoja.Nota("Ya estaban en producción: " + string.Join(", ", _plan.YaDesplegados.Select(NombreDe)) + ".", Tema.success);
            if (_plan.NoListos.Count > 0)
                hoja.Nota("Todavía no salen (les falta trabajo): " + string.Join(", ", _plan.NoListos.Select(NombreDe)) + ".", Tema.inkMuted);

            var como = hoja.Tarjeta("Cómo se juega");
            Ui.Texto(como, "1 · <b>La tubería.</b> Arrastra a cada hueco un tramo del camino entre el código y el cliente. Hay más tramos que huecos, y alguno es un atajo que sale caro. Un hueco vacío no da error: esa etapa, simplemente, no ocurre.", EstiloTexto.Cuerpo, Tema.ink);
            Ui.Texto(como, "2 · <b>Los módulos.</b> Uno por uno: arrastra cada cable a la toma que le corresponde. Fíjate en cuáles son de producción y cuáles de pruebas. Después, enciéndelo.", EstiloTexto.Cuerpo, Tema.ink);
            Ui.Texto(como, $"Tienes <b>{Reloj(_total)}</b> para todo. Cuando el reloj llega a cero se entrega lo que haya. Lo que salga de aquí cambia las barras del proyecto y pesa en la evaluación final.", EstiloTexto.Cuerpo, Tema.warning);
            PantallaDeGlosario.Palabras(App, hoja.Raiz, "Si alguna palabra no te suena:", "despliegue", "pipeline", "entorno", "secretos");

            hoja.Accion("Empezar: armar la tubería  ►", () => { _paso = Paso.Tuberia; PintarJuego(); });
            GuiaView.Avisar(App, "despliegue.intro");
        }

        // ==================================================================== el tablero de juego

        private void PintarJuego() {
            UiKit.Vaciar(_cuerpo);
            _lamina = PantallaDeMinijuego.CrearLienzo(Ui, _cuerpo, null, W, H);

            var lado = Ui.Columna(_cuerpo, "Lado", Tema.espacio);
            UiKit.Tamano(lado, ancho: 440, flexAlto: 1);
            var tarjeta = Ui.PanelColumna(lado, "Info", Tema.margen * 0.75f, Tema.espacio);
            UiKit.Tamano(tarjeta, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(tarjeta, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            _info = Ui.Texto(contenido, "", EstiloTexto.Cuerpo, Tema.ink);
            PantallaDeGlosario.Palabras(App, lado, "", "pipeline", "entorno", "secretos");
            _principal = Ui.Boton(lado, "", Avanzar, VarianteBoton.Primario);
            Repintar();
            GuiaView.Avisar(App, _paso == Paso.Tuberia ? "despliegue.tuberia" : "despliegue.modulos");
        }

        public override void Repintar() {
            if (_lamina == null || (_paso != Paso.Tuberia && _paso != Paso.Modulo)) return;
            for (var i = _lamina.Raiz.childCount - 1; i >= 0; i--) {
                var hijo = _lamina.Raiz.GetChild(i);
                if (hijo.gameObject != _lamina.Dibujo.gameObject) Destroy(hijo.gameObject);
            }
            _lamina.Dibujo.Limpiar();
            if (_paso == Paso.Tuberia) PintarTuberia(); else PintarModulo();
        }

        private void Avanzar() {
            if (_paso == Paso.Tuberia) {
                if (_plan.Modulos.Count == 0) { Entregar(false); return; }
                _paso = Paso.Modulo; _modulo = 0; _encendido = false; _cableElegido = null;
                Repintar();
                GuiaView.Avisar(App, "despliegue.modulos");
                return;
            }
            if (!_encendido) { _encendido = true; _cableElegido = null; Repintar(); return; }
            if (_modulo + 1 >= _plan.Modulos.Count) { Entregar(false); return; }
            _modulo++; _encendido = false; _cableElegido = null;
            Repintar();
        }

        // ==================================================================== 1 · la tuberia

        private const float TuboY = 150, HuecoAlto = 110, BandejaY = 300, PiezaW = 300, PiezaH = 96;

        private Rect RectDelHueco(int i) {
            var n = _huecos.Length;
            const float x0 = 150, x1 = 1150, gap = 12;
            var w = (x1 - x0 - gap * (n - 1)) / n;
            return new Rect(x0 + i * (w + gap), TuboY - HuecoAlto / 2, w, HuecoAlto);
        }

        private void PintarTuberia() {
            _titulo.text = "1 · La tubería: del código al cliente";
            var d = _lamina.Dibujo;
            // los dos extremos y el tubo
            d.Rect(20, TuboY - 60, 110, 120, Tema.papelTrazo, 3, Tema.papelCaja, false, 12);
            _lamina.Texto(20, TuboY - 60, 110, 120, "EL\nCÓDIGO", 18, Tema.paperInk, TextAlignmentOptions.Center, null, true);
            d.Rect(1170, TuboY - 60, 110, 120, Tema.papelTrazo, 3, Tema.papelCaja, false, 12);
            _lamina.Texto(1170, TuboY - 60, 110, 120, "EL\nCLIENTE", 18, Tema.paperInk, TextAlignmentOptions.Center, null, true);
            d.Linea(130, TuboY - 22, 1170, TuboY - 22, Tema.papelCyan, 5);
            d.Linea(130, TuboY + 22, 1170, TuboY + 22, Tema.papelCyan, 5);
            d.Flecha(134, TuboY, 1164, TuboY, NexusTheme.Alfa(Tema.papelCyan, 0.35f), 3, true);
            _lamina.Texto(150, TuboY - 100, 1000, 30, "Arrastra un tramo a cada hueco, en el orden en que debe pasar. Pulsa un tramo puesto para quitarlo.", 16, Tema.paperMuted);

            var puestos = new HashSet<string>(_huecos.Where(h => h != null));
            for (var i = 0; i < _huecos.Length; i++) {
                var r = RectDelHueco(i);
                var indice = i;
                if (_huecos[i] == null) {
                    d.Rect(r.x, r.y, r.width, r.height, Tema.papelTrazo, 3, Tema.paperBg, true, 10);
                    _lamina.Texto(r.x, r.y, r.width, r.height, (i + 1).ToString(), 26, Tema.paperLine, TextAlignmentOptions.Center, null, true);
                    continue;
                }
                var tramo = _def.Tramo(_huecos[i]);
                var pieza = Pieza(r.x, r.y, r.width, r.height, tramo.Nombre, null, Tema.papelCyan, _tramoMirado == tramo.Id);
                pieza.AlPulsar = () => { _huecos[indice] = null; _tramoMirado = tramo.Id; Repintar(); };
                pieza.AlSoltar = centro => SoltarTramo(tramo.Id, centro, indice);
            }

            _lamina.Texto(20, BandejaY - 40, 900, 30, "TRAMOS DISPONIBLES", 16, Tema.papelCyan, TextAlignmentOptions.MidlineLeft, null, true);
            var libres = _def.Tuberia.Tramos.Where(t => !puestos.Contains(t.Id)).ToList();
            for (var k = 0; k < libres.Count; k++) {
                var tramo = libres[k];
                var x = 20 + (k % 4) * (PiezaW + 20); var y = BandejaY + (k / 4) * (PiezaH + 14);
                var pieza = Pieza(x, y, PiezaW, PiezaH, tramo.Nombre, tramo.QueHace, Tema.papelTrazo, _tramoMirado == tramo.Id);
                pieza.AlPulsar = () => { _tramoMirado = tramo.Id; var libre = Array.IndexOf(_huecos, null); if (libre >= 0) _huecos[libre] = tramo.Id; Repintar(); };
                pieza.AlSoltar = centro => SoltarTramo(tramo.Id, centro, -1);
            }

            var mirado = _tramoMirado == null ? null : _def.Tramo(_tramoMirado);
            var puestosCuenta = _huecos.Count(h => h != null);
            _info.text = $"<b>{puestosCuenta} de {_huecos.Length} huecos ocupados.</b>\n" +
                         "El orden importa: lo que va a la izquierda pasa primero. No hace falta llenarlos todos, pero lo que no pongas no ocurre.\n\n" +
                         (mirado == null ? "Pulsa un tramo para ver qué hace."
                                         : $"<b>{mirado.Nombre}</b>\n{mirado.QueHace}");
            _principal.GetComponentInChildren<TMP_Text>().text = _plan.Modulos.Count == 0 ? "Tubería lista: entregar" : "Tubería lista: a los módulos  ►";
        }

        private void SoltarTramo(string id, Vector2 centro, int desde) {
            _tramoMirado = id;
            var destino = -1;
            for (var i = 0; i < _huecos.Length; i++) {
                var r = RectDelHueco(i);
                if (centro.x >= r.xMin - 6 && centro.x <= r.xMax + 6 && centro.y >= r.yMin - 40 && centro.y <= r.yMax + 40) destino = i;
            }
            if (desde >= 0) _huecos[desde] = null;
            if (destino >= 0) {
                // Si el hueco estaba ocupado, los dos tramos se cambian de sitio (o el otro vuelve a la bandeja).
                var otro = _huecos[destino];
                _huecos[destino] = id;
                if (otro != null && desde >= 0) _huecos[desde] = otro;
            }
            Repintar();
        }

        /// <summary>Una ficha de papel con su nombre (y, si cabe, lo que hace) que se puede arrastrar o pulsar.</summary>
        private PiezaArrastrable Pieza(float x, float y, float w, float h, string nombre, string detalle, Color borde, bool resaltada) {
            var rt = Ui.Nodo(_lamina.Raiz, "Pieza " + nombre);
            Lamina.Colocar(rt, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(img, Color.white, NexusTheme.RadioSm);
            Ui.Borde(rt, resaltada ? Tema.papelAviso : borde, NexusTheme.RadioSm, resaltada ? 3 : 2);
            var texto = new Lamina(Ui, rt, w, h, "Contenido");
            UiKit.Rellenar(texto.Raiz);
            texto.Dibujo.raycastTarget = false;
            if (string.IsNullOrEmpty(detalle)) {
                var t = texto.Texto(6, 0, w - 12, h, nombre, w < 140 ? 14 : 17, Tema.paperInk, TextAlignmentOptions.Center, null, true);
                t.raycastTarget = false;
            } else {
                texto.Texto(12, 6, w - 24, 28, nombre, 18, Tema.paperInk, TextAlignmentOptions.MidlineLeft, null, true).raycastTarget = false;
                texto.Texto(12, 34, w - 24, h - 40, detalle, 14, Tema.paperMuted, TextAlignmentOptions.TopLeft).raycastTarget = false;
            }
            return rt.gameObject.AddComponent<PiezaArrastrable>();
        }

        // ==================================================================== 2 · enchufar cada modulo

        private const float CableX = 40, CableW = 360, CableH = 62, TomaW = 300, TomaH = 70;

        private Rect RectDeLaToma(int i) {
            return new Rect(640 + (i % 2) * (TomaW + 30), 90 + (i / 2) * (TomaH + 22), TomaW, TomaH);
        }

        private void PintarModulo() {
            var id = _plan.Modulos[_modulo];
            var modulo = _ficha == null ? null : _ficha.Modulo(id);
            var cables = _def.CablesDe(id);
            var puestos = _enchufes[id];
            _titulo.text = $"2 · Módulo {_modulo + 1} de {_plan.Modulos.Count}: {NombreDe(id)}";
            var d = _lamina.Dibujo;

            // Como quedaria (o quedo) este modulo con lo enchufado hasta ahora.
            RenglonDeDespliegue renglon = null;
            if (_encendido) renglon = EvaluadorDeDespliegue.Evaluar(_def, _ficha, _plan, Respuesta(false)).Renglones.FirstOrDefault(x => x.Que == "modulo" && x.Id == id);
            var color = renglon == null ? Tema.papelTrazo : renglon.Estado == EstadosDeDespliegue.Bien ? Tema.papelExito : renglon.Estado == EstadosDeDespliegue.Mal ? Tema.papelAviso : Tema.papelPeligro;

            d.Rect(20, 20, 400, H - 40, color, renglon == null ? 3 : 5, Tema.papelCaja, false, 14);
            _lamina.Texto(40, 30, 360, 34, NombreDe(id).ToUpperInvariant(), 20, Tema.paperInk, TextAlignmentOptions.MidlineLeft, null, true);
            if (modulo != null) _lamina.Texto(40, 64, 360, 64, modulo.QueHace, 14, Tema.paperMuted, TextAlignmentOptions.TopLeft);
            _lamina.Texto(40, 132, 360, 24, "SUS CABLES", 14, Tema.papelCyan, TextAlignmentOptions.MidlineLeft, null, true);

            _lamina.Texto(640, 30, 640, 30, "EL ENTORNO · arrastra cada cable a su toma (o pulsa el cable y después la toma)", 15, Tema.paperMuted);
            for (var i = 0; i < _def.Tomas.Count; i++) {
                var toma = _def.Tomas[i];
                var r = RectDeLaToma(i);
                var dePruebas = toma.Entorno == "pruebas";
                var ocupada = puestos.Values.Contains(toma.Id);
                d.Rect(r.x, r.y, r.width, r.height, ocupada ? Tema.papelCyan : dePruebas ? Tema.paperMuted : Tema.papelTrazo, ocupada ? 4 : 2, Color.white, dePruebas, 10);
                d.Circulo(r.x + 22, r.center.y, 9, Tema.papelTrazo, 3, ocupada ? Tema.papelCyan : (Color?)null);
                _lamina.Texto(r.x + 42, r.y + 6, r.width - 50, 34, toma.Nombre, 16, Tema.paperInk, TextAlignmentOptions.MidlineLeft, null, true);
                _lamina.Texto(r.x + 42, r.y + 38, r.width - 50, 24, dePruebas ? "PRUEBAS" : "PRODUCCIÓN", 12, dePruebas ? Tema.paperMuted : Tema.papelCyan);
                if (_encendido) continue;
                var destino = toma.Id;
                _lamina.Zona(r.x, r.y, r.width, r.height, () => {
                    if (_cableElegido == null) return;
                    puestos[_cableElegido] = destino; _cableElegido = null; Repintar();
                }, "Toma " + toma.Id);
            }

            for (var i = 0; i < cables.Count; i++) {
                var cable = cables[i];
                var y = 164 + i * (CableH + 14);
                if (cable.EsDependencia) {
                    var listo = _plan.YaDesplegados.Contains(cable.ModuloNecesario) || _plan.Modulos.Contains(cable.ModuloNecesario);
                    d.Rect(CableX, y, CableW, CableH, listo ? Tema.papelExito : Tema.papelPeligro, 2, Tema.paperBg, true, 10);
                    _lamina.Texto(CableX + 12, y, CableW - 24, CableH, cable.Nombre + (listo
                        ? (_plan.YaDesplegados.Contains(cable.ModuloNecesario) ? "\n<size=80%>ya está en producción</size>" : "\n<size=80%>sale hoy: tiene que encender</size>")
                        : "\n<size=80%>no sale hoy: este módulo no podrá encender</size>"), 15, Tema.paperInk, TextAlignmentOptions.MidlineLeft);
                    continue;
                }
                string enchufadoEn;
                puestos.TryGetValue(cable.Id, out enchufadoEn);
                if (enchufadoEn != null) {
                    var indice = _def.Tomas.FindIndex(t => t.Id == enchufadoEn);
                    if (indice >= 0) {
                        var r = RectDeLaToma(indice);
                        d.CurvaCubica(new Vector2(CableX + CableW, y + CableH / 2), new Vector2(520, y + CableH / 2),
                                      new Vector2(540, r.center.y), new Vector2(r.x + 22, r.center.y), Tema.papelCyan, 4);
                    }
                }
                if (_encendido) {
                    d.Rect(CableX, y, CableW, CableH, Tema.papelTrazo, 2, Color.white, false, 10);
                    _lamina.Texto(CableX + 12, y, CableW - 24, CableH, cable.Nombre, 16, Tema.paperInk, TextAlignmentOptions.MidlineLeft, null, true);
                    continue;
                }
                var cableId = cable.Id;
                var pieza = Pieza(CableX, y, CableW, CableH, cable.Nombre, enchufadoEn == null ? "sin enchufar" : "en «" + _def.Toma(enchufadoEn).Nombre + "» · pulsa para soltarlo",
                                  enchufadoEn == null ? Tema.papelAviso : Tema.papelCyan, _cableElegido == cable.Id);
                pieza.AlPulsar = () => {
                    if (puestos.ContainsKey(cableId)) { puestos.Remove(cableId); _cableElegido = null; }
                    else _cableElegido = _cableElegido == cableId ? null : cableId;
                    Repintar();
                };
                pieza.AlSoltar = centro => {
                    for (var k = 0; k < _def.Tomas.Count; k++) {
                        var r = RectDeLaToma(k);
                        if (centro.x >= r.xMin - 30 && centro.x <= r.xMax + 30 && centro.y >= r.yMin - 12 && centro.y <= r.yMax + 12) puestos[cableId] = _def.Tomas[k].Id;
                    }
                    _cableElegido = null;
                    Repintar();
                };
            }

            if (renglon != null) {
                var titulo = renglon.Estado == EstadosDeDespliegue.Bien ? "ENCENDIÓ" : renglon.Estado == EstadosDeDespliegue.Mal ? "ENCENDIÓ, PERO MAL" : "NO ENCENDIÓ";
                _lamina.Texto(40, H - 150, 360, 34, titulo, 22, color, TextAlignmentOptions.MidlineLeft, null, true);
                _info.text = $"<b>{NombreDe(id)}: {titulo.ToLowerInvariant()}.</b>\n{renglon.Texto}";
                _principal.GetComponentInChildren<TMP_Text>().text = _modulo + 1 >= _plan.Modulos.Count ? "Terminar el despliegue  ►" : "Siguiente módulo  ►";
                return;
            }
            var sueltos = cables.Count(c => !c.EsDependencia && !puestos.ContainsKey(c.Id));
            var elegido = cables.FirstOrDefault(c => c.Id == _cableElegido);
            _info.text = (sueltos == 0 ? "<b>Todo enchufado.</b> Revisa que cada cable esté en una toma de producción y enciéndelo."
                                       : $"<b>Quedan {sueltos} cable(s) sin enchufar.</b>") +
                         "\n\nCada cable es algo que el módulo necesita para funcionar. En producción tiene que apuntar a lo de verdad, y las claves, a un sitio donde nadie las lea." +
                         (elegido == null ? "" : $"\n\n<b>Elegido: {elegido.Nombre}.</b> Ahora pulsa su toma.");
            _principal.GetComponentInChildren<TMP_Text>().text = "Encender el módulo";
        }

        // ==================================================================== entregar y ver como salio

        private RespuestaDeDespliegue Respuesta(bool sinTiempo) {
            return new RespuestaDeDespliegue {
                Tuberia = _huecos.ToList(),
                Enchufes = _enchufes.ToDictionary(kv => kv.Key, kv => new Dictionary<string, string>(kv.Value, StringComparer.Ordinal), StringComparer.Ordinal),
                SegundosUsados = _total - Mathf.Max(0, _restante),
                SeAcaboElTiempo = sinTiempo
            };
        }

        private void Entregar(bool sinTiempo) {
            if (_paso == Paso.Resultado) return;
            _paso = Paso.Resultado;
            _lamina = null;
            _reloj.Mostrar(Reloj(_restante), sinTiempo ? Tono.Peligro : Tono.Cyan);
            _resultado = App.Sesion.ResolverDespliegue(Respuesta(sinTiempo));
            PintarResultado();
        }

        private void PintarResultado() {
            var r = _resultado;
            _titulo.text = r.Titulo;
            UiKit.Vaciar(_cuerpo);

            var izquierda = Ui.PanelColumna(_cuerpo, "Detalle", Tema.margen, Tema.espacio);
            UiKit.Tamano(izquierda, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(izquierda, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var hoja = new Hoja(Ui, contenido);
            hoja.Etiqueta("Qué estuvo bien y qué faltó");
            if (r.SeAcaboElTiempo) hoja.Nota("Se acabó el tiempo: se entregó lo que había puesto.", Tema.warning);

            foreach (var grupo in new[] { new { que = "tuberia", titulo = "La tubería" }, new { que = "modulo", titulo = "Los módulos" } }) {
                var renglones = r.Renglones.Where(x => x.Que == grupo.que).ToList();
                if (renglones.Count == 0) continue;
                var t = hoja.Tarjeta(grupo.titulo);
                foreach (var x in renglones) {
                    var bien = x.Estado == EstadosDeDespliegue.Bien; var mal = x.Estado == EstadosDeDespliegue.Mal;
                    var color = bien ? Tema.success : mal ? Tema.warning : Tema.danger;
                    Ui.Texto(t, $"<b>{(bien ? "√" : mal ? "~" : "×")} {x.Nombre}</b>  <size=85%>{(bien ? "bien" : mal ? "mal puesto" : "faltó")}</size>", EstiloTexto.Cuerpo, color);
                    if (!string.IsNullOrEmpty(x.Texto)) Ui.Texto(t, x.Texto, EstiloTexto.Pequeno, Tema.inkMuted);
                }
            }

            // Las consecuencias van en un scroll y el boton de seguir, fijo debajo: con varios efectos y lo que dice
            // el cliente no siempre caben de alto, y el boton no puede quedar fuera de la pantalla.
            var lado = Ui.Columna(_cuerpo, "Consecuencias", Tema.espacio);
            UiKit.Tamano(lado, ancho: 520, flexAlto: 1);
            RectTransform derecha;
            var scrollDeLado = Ui.Desplazable(lado, out derecha);
            UiKit.Tamano(scrollDeLado, flexAncho: 1, flexAlto: 1);
            derecha.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(4, 18, 4, 4);
            var tono = r.Nivel == "bien" ? Tono.Exito : r.Nivel == "mal" ? Tono.Peligro : Tono.Aviso;
            var cliente = Ui.Tarjeta(derecha, "Lo que notó el cliente", tono);
            Ui.Texto(cliente, r.ParaElCliente, EstiloTexto.Cuerpo, Tema.ink);
            // Quien usa lo que acaba de salir, en sus palabras.
            foreach (var voz in App.Sesion.VocesDelProducto().Where(v => r.ModulosEncendidos.Contains(v.ModuloId)).Take(2))
                Ui.Texto(cliente, $"{voz.Quien}: «{voz.Frase}»", EstiloTexto.Pequeno, Tema.cyan);

            var hoy = Ui.Tarjeta(derecha, "Lo que cambia hoy en el proyecto");
            if (r.Efectos.Count == 0) Ui.Texto(hoy, "Nada: las barras se quedan como estaban.", EstiloTexto.Pequeno, Tema.inkMuted);
            foreach (var kv in r.Efectos) {
                var v = Convert.ToDouble(kv.Value);
                // En estas barras, subir es malo: deuda y cansancio.
                var malo = (kv.Key == "DeudaTecnica" || kv.Key == "Cansancio") ? v > 0 : v < 0;
                Ui.Texto(hoy, $"· {Textos.NombreDeStock(kv.Key)}: {Textos.Cambio(v)}", EstiloTexto.Cuerpo, malo ? Tema.danger : Tema.success);
            }
            if (r.AvanceQueCostaraManana > 0 && !r.EsFinal)
                Ui.Texto(hoy, $"· Mañana, arreglar lo que salió mal costará {r.AvanceQueCostaraManana:0.#} puntos de avance.", EstiloTexto.Cuerpo, Tema.danger);

            var final = Ui.Tarjeta(derecha, r.EsFinal ? "Lo que se lleva a la evaluación" : "Lo que se lleva al lanzamiento final");
            Ui.Texto(final, $"· Riesgo del lanzamiento: {Textos.Cambio(r.Riesgo)}", EstiloTexto.Cuerpo, r.Riesgo > 0 ? Tema.danger : r.Riesgo < 0 ? Tema.success : Tema.ink);
            Ui.Texto(final, r.Defectos > 0 ? $"· Errores que se escaparon por desplegar así: +{r.Defectos}" : "· No se escapó ningún error por el despliegue.",
                     EstiloTexto.Cuerpo, r.Defectos > 0 ? Tema.danger : Tema.success);
            var s = App.Sesion;
            Ui.Texto(final, $"Acumulado del nivel: riesgo {Textos.Cambio(s.RiesgoPorDespliegues())}, errores +{s.DefectosPorDespliegues()}.", EstiloTexto.Pequeno, Tema.inkMuted);

            Ui.Boton(lado, r.EsFinal ? "Ir al lanzamiento  ►" : "Volver al desarrollo  ►", () => AlTerminar?.Invoke(), VarianteBoton.Primario);
            GuiaView.Avisar(App, "despliegue.resultado");
        }
    }
}
