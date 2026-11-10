using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Despliegue;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public sealed partial class PantallaDeDespliegue {
        // ==================================================================== 1 · la tuberia

        private enum Luz { Neutra, Bien, Mal, Apagada }

        private Lamina _tub;
        private float _estLado, _pipeY, _x0Tubo, _x1Tubo, _paqueteX, _luzX, _grietaX = -1;
        private RectTransform[] _est;
        private Image[] _estBorde;
        private DibujoUI[] _estDibujo;
        private TMP_Text[] _estNombre, _estNumero;
        private Luz[] _estLuz;
        private AbanicoDeSoluciones _mano;
        private List<string> _ordenDeLaMano = new List<string>();
        private DibujoUI _luz;
        private RectTransform _paquete;
        private Image _paqueteImg, _nodoCliente, _nodoCodigo;
        private Button _btnProbar, _btnSiguiente;
        private int _sel;
        private bool _probando, _completa, _volando;

        private const float Hueco = 16f, AnchoDelNodo = 140f, AltoDelNodo = 170f, EscalaDeLaMano = 0.92f;

        private void LimpiarTuberia() {
            _tub = null; _est = null; _estBorde = null; _estDibujo = null; _estNombre = null; _estNumero = null; _estLuz = null;
            _mano = null; _luz = null; _paquete = null; _paqueteImg = null; _nodoCliente = null; _nodoCodigo = null;
            _btnProbar = null; _btnSiguiente = null;
            _probando = false; _volando = false;
        }

        private static uint HashDe(string s) {
            unchecked {
                var h = 2166136261u;
                foreach (var c in s ?? "") { h ^= c; h *= 16777619u; }
                return h;
            }
        }

        private void PintarTuberia() {
            _paso = Paso.Tuberia;
            PonerAntetitulo("1 de 2 · la tubería");
            PonerIntentos();
            Decir("Arma el camino del código al cliente, sin atajos. Cuando creas que está lista, pruébala.");
            _probando = false; _completa = false; _volando = false; _sel = 0; _luzX = 0; _grietaX = -1;

            var area = UiKit.Rellenar(Ui.Nodo(_escena, "Tuberia"));

            // ---- las etapas, abajo, como cartas pequeñas (desordenadas: el orden de la lista no es el del camino)
            var sitio = Ui.Nodo(area, "Sitio de la mano");
            sitio.anchorMin = new Vector2(0, 0); sitio.anchorMax = new Vector2(1, 0); sitio.pivot = new Vector2(0.5f, 0);
            sitio.anchoredPosition = Vector2.zero;
            sitio.sizeDelta = new Vector2(0, 280);
            _ordenDeLaMano = _def.Tuberia.Tramos.OrderBy(t => HashDe(t.Id)).Select(t => t.Id).ToList();
            var opciones = _ordenDeLaMano.Select(id => {
                var t = _def.Tramo(id);
                return new OpcionDeSolucion { Id = t.Id, Nombre = t.Nombre, Icono = t.Id };   // sin descripcion: sale en el dialogo al mirarla
            }).ToList();
            _mano = AbanicoDeSoluciones.Crear(Ui, sitio, opciones);
            _mano.Escala = EscalaDeLaMano;
            _mano.PermiteArrastrar = true;
            _mano.AlPulsar = JugarPieza;
            _mano.AlSoltar = SoltarPieza;
            _mano.AlEnfocar = id => { if (!string.IsNullOrEmpty(id)) { var t = _def.Tramo(id); Decir(t.Nombre + ": " + t.QueHace); } };
            _mano.enabled = false;   // reparte cuando la tuberia ya esta dibujada

            // ---- la accion de este paso, arriba a la derecha
            _btnProbar = Ui.Boton(_accion, "Probar la tubería", Probar, VarianteBoton.Primario);
            UiKit.Tamano(_btnProbar, alto: 64);
            GuiaView.Registrar("despliegue.probar", (RectTransform)_btnProbar.transform);

            // ---- la tuberia, cuando el hueco ya tiene su tamaño de verdad
            var medido = area.gameObject.AddComponent<HuecoMedido>();
            medido.AlMedirse = (w, h) => ConstruirTuberia(area, w, h);
            GuiaView.Avisar(App, "despliegue.tuberia");
        }

        private void ConstruirTuberia(RectTransform area, float w, float h) {
            _tub = new Lamina(Ui, area, w, h);
            var r = _tub.Raiz;
            r.GetComponent<LayoutElement>().ignoreLayout = true;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(w, h);
            r.SetAsFirstSibling();   // por debajo de la mano de cartas

            var n = _huecos.Length;
            _x0Tubo = 28 + AnchoDelNodo + 10;
            _x1Tubo = w - 28 - AnchoDelNodo - 10;
            _estLado = Mathf.Min(190f, ((_x1Tubo - _x0Tubo) - (n + 1) * Hueco) / n);
            var altoEst = _estLado + 36;
            _pipeY = Mathf.Clamp(h * 0.40f, 290f, Mathf.Max(290f, h - 430f));

            // El tubo: dos paredes de luz tenue de lado a lado, y un flujo discontinuo por el medio.
            var d = _tub.Dibujo;
            var tubo = NexusTheme.Alfa(Tema.cyan, 0.32f);
            d.Linea(_x0Tubo, _pipeY - 16, _x1Tubo, _pipeY - 16, tubo, 4);
            d.Linea(_x0Tubo, _pipeY + 16, _x1Tubo, _pipeY + 16, tubo, 4);
            d.Flecha(_x0Tubo + 6, _pipeY, _x1Tubo - 6, _pipeY, NexusTheme.Alfa(Tema.cyan, 0.16f), 3, true);

            // La luz que va dejando el paquete: una capa por encima del tubo y por debajo de las estaciones.
            var capaLuz = Ui.Nodo(r, "Luz");
            UiKit.Rellenar(capaLuz);
            capaLuz.gameObject.AddComponent<CanvasRenderer>();
            _luz = capaLuz.gameObject.AddComponent<DibujoUI>();
            _luz.raycastTarget = false;

            // Los dos extremos.
            _nodoCodigo = NodoDeTuberia(28, _pipeY - AltoDelNodo / 2, "TU CÓDIGO", true);
            _nodoCliente = NodoDeTuberia(w - 28 - AnchoDelNodo, _pipeY - AltoDelNodo / 2, "EL CLIENTE", false);

            // Las estaciones, una por hueco.
            _est = new RectTransform[n]; _estBorde = new Image[n]; _estDibujo = new DibujoUI[n];
            _estNombre = new TMP_Text[n]; _estNumero = new TMP_Text[n]; _estLuz = new Luz[n];
            for (var i = 0; i < n; i++) {
                var indice = i;
                var x = _x0Tubo + Hueco + i * (_estLado + Hueco);
                var rt = Ui.Nodo(r, "Estacion " + (i + 1));
                Lamina.Colocar(rt, x, _pipeY - altoEst / 2, _estLado, altoEst);
                var fondo = rt.gameObject.AddComponent<Image>();
                Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg900, 0.96f), NexusTheme.RadioLg);
                fondo.raycastTarget = true;
                _estBorde[i] = Ui.Borde(rt, NexusTheme.Alfa(Tema.cyan, 0.3f), NexusTheme.RadioLg, 2);
                var boton = rt.gameObject.AddComponent<Button>();
                boton.targetGraphic = fondo;
                boton.transition = Selectable.Transition.None;
                boton.onClick.AddListener(() => PincharEstacion(indice));
                var pasar = rt.gameObject.AddComponent<AlPasarElRaton>();
                pasar.AlEntrar = () => { var id = _huecos[indice]; if (id != null) { var t = _def.Tramo(id); Decir(t.Nombre + ": " + t.QueHace); } };

                var lienzo = Ui.Nodo(rt, "Dibujo");
                UiKit.Rellenar(lienzo);
                lienzo.gameObject.AddComponent<CanvasRenderer>();
                _estDibujo[i] = lienzo.gameObject.AddComponent<DibujoUI>();
                _estDibujo[i].raycastTarget = false;

                _estNumero[i] = TextoFijo(rt, (i + 1).ToString(), 15, Tema.inkMuted, TextAlignmentOptions.TopLeft, 9, 6, 40, 22);
                _estNombre[i] = TextoFijo(rt, "", 17, Tema.ink, TextAlignmentOptions.Center, 6, _estLado * 0.7f + 14, _estLado - 12, 44);
                _estNombre[i].enableAutoSizing = true; _estNombre[i].fontSizeMin = 11; _estNombre[i].fontSizeMax = 17;
                _est[i] = rt;
                RefrescarEstacion(i);
                // Entran de izquierda a derecha, con rebote.
                Aparecer(rt, 0.15f + i * 0.07f);
            }

            // El paquete de luz que recorre la tuberia al probarla.
            _paquete = Ui.Nodo(r, "Paquete de luz");
            _paquete.anchorMin = _paquete.anchorMax = new Vector2(0, 1);
            _paquete.pivot = new Vector2(0.5f, 0.5f);
            _paquete.sizeDelta = new Vector2(84, 84);
            _paqueteImg = _paquete.gameObject.AddComponent<Image>();
            _paqueteImg.sprite = PlanoIlustrado.Brillo();
            _paqueteImg.raycastTarget = false;
            _paqueteImg.color = new Color(1, 1, 1, 0.95f);
            _paquete.gameObject.SetActive(false);

            Aparecer(_nodoCodigo.rectTransform, 0.05f);
            Aparecer(_nodoCliente.rectTransform, 0.1f);
            var dueno = r.gameObject;
            _anim.Despues(0.15f + n * 0.07f + 0.2f, () => { if (_mano != null) _mano.enabled = true; }, dueno);   // se reparten las cartas
            _anim.Despues(0.15f + n * 0.07f + 1.1f, AbrirElTutorialDeLaTuberia, dueno);
        }

        /// <summary>«TU CÓDIGO» o «EL CLIENTE»: un cuadro de cristal con su dibujo, en un extremo del tubo.</summary>
        private Image NodoDeTuberia(float x, float y, string titulo, bool codigo) {
            var rt = Ui.Nodo(_tub.Raiz, titulo);
            Lamina.Colocar(rt, x, y, AnchoDelNodo, AltoDelNodo);
            var fondo = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg900, 0.96f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            var borde = Ui.Borde(rt, NexusTheme.Alfa(Tema.cyan, 0.45f), NexusTheme.RadioLg, 2.5f);
            var lienzo = Ui.Nodo(rt, "Dibujo");
            UiKit.Rellenar(lienzo);
            lienzo.offsetMin = new Vector2(11, -12); lienzo.offsetMax = new Vector2(11, -12);   // el dibujo se pensó para 118x140: se centra en el nodo más grande
            lienzo.gameObject.AddComponent<CanvasRenderer>();
            var d = lienzo.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            var c = Tema.ink; var a = Tema.cyan;
            if (codigo) {   // una terminal con su «>_»
                d.Rect(24, 22, 70, 52, c, 3, NexusTheme.Alfa(c, 0.12f), false, 6);
                d.Polilinea(new List<Vector2> { new Vector2(36, 38), new Vector2(48, 48), new Vector2(36, 58) }, a, 3.5f);
                d.Linea(54, 60, 74, 60, a, 3.5f);
            } else {   // una persona esperando
                d.Circulo(59, 34, 12, c, 3, NexusTheme.Alfa(c, 0.12f));
                d.Polilinea(new List<Vector2> { new Vector2(34, 74), new Vector2(34, 58), new Vector2(84, 58), new Vector2(84, 74) }, c, 3);
            }
            TextoFijo(rt, titulo, 15, Tema.inkMuted, TextAlignmentOptions.Center, 4, AltoDelNodo - 46, AnchoDelNodo - 8, 40).characterSpacing = 2;
            return borde;
        }

        /// <summary>Un texto en una caja fija de la estacion (sin «alto minimo»: aqui cada texto tiene su sitio).</summary>
        private TMP_Text TextoFijo(RectTransform padre, string texto, float tam, Color color, TextAlignmentOptions al, float x, float y, float w, float h) {
            var t = Ui.Texto(padre, texto, EstiloTexto.Cuerpo, color, al);
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);
            t.fontSize = tam;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            Lamina.Colocar(t.rectTransform, x, y, w, h);
            return t;
        }

        private float CentroX(int i) { return _x0Tubo + Hueco + i * (_estLado + Hueco) + _estLado / 2f; }

        private Color ColorDeLuz(Luz l) {
            switch (l) {
                case Luz.Bien: return Tema.success;
                case Luz.Mal: return Tema.danger;
                case Luz.Apagada: return NexusTheme.Alfa(Tema.lineStrong, 0.55f);
                default: return NexusTheme.Alfa(Tema.cyan, 0.45f);
            }
        }

        /// <summary>Repinta una estacion: vacia (borde discontinuo y su numero) o con el dibujo y el nombre de su etapa.</summary>
        private void RefrescarEstacion(int i) {
            if (_est == null || i < 0 || i >= _est.Length) return;
            var d = _estDibujo[i];
            d.Limpiar();
            var id = _huecos[i];
            var luz = _estLuz[i];
            var color = ColorDeLuz(luz);
            _estBorde[i].color = id == null ? NexusTheme.Alfa(color, 0.18f) : color;
            var alto = _estLado + 36;
            if (id == null) {
                var elegida = i == _sel && !_probando && !_completa;
                var borde = luz == Luz.Mal ? Tema.danger : elegida ? Tema.cyan : NexusTheme.Alfa(Tema.cyan, 0.3f);
                d.Rect(3, 3, _estLado - 6, alto - 6, borde, elegida ? 3.5f : 2.5f, elegida ? NexusTheme.Alfa(Tema.cyan, 0.07f) : (Color?)null, true, 14);
                _estNombre[i].text = "";
                _estNumero[i].color = luz == Luz.Mal ? Tema.danger : elegida ? Tema.cyan : Tema.inkMuted;
                _estNumero[i].fontSize = 26;
                Lamina.Colocar(_estNumero[i].rectTransform, 0, alto / 2 - 18, _estLado, 36);
                _estNumero[i].alignment = TextAlignmentOptions.Center;
            } else {
                var acento = luz == Luz.Bien ? Tema.success : luz == Luz.Mal ? Tema.danger : luz == Luz.Apagada ? Tema.inkMuted : Tema.cyan;
                DibujosDeEtapas.Dibujar(d, id, _estLado * 0.18f, 18, _estLado * 0.64f, luz == Luz.Apagada ? Tema.inkMuted : Tema.ink, acento);
                _estNombre[i].text = _def.Tramo(id).Nombre;
                _estNombre[i].color = luz == Luz.Apagada ? Tema.inkMuted : Tema.ink;
                _estNumero[i].color = Tema.inkMuted;
                _estNumero[i].fontSize = 15;
                Lamina.Colocar(_estNumero[i].rectTransform, 9, 6, 40, 22);
                _estNumero[i].alignment = TextAlignmentOptions.TopLeft;
            }
        }

        private void RefrescarTodas() { if (_est == null) return; for (var i = 0; i < _est.Length; i++) RefrescarEstacion(i); }

        // ==================================================================== poner y quitar etapas

        private bool PuedeTocar { get { return _tub != null && _mano != null && !_probando && !_completa && !_transicionando && !_volando && !_tutorialAbierto; } }

        private int HuecoLibre() {
            if (_sel >= 0 && _sel < _huecos.Length && _huecos[_sel] == null) return _sel;
            for (var i = 0; i < _huecos.Length; i++) if (_huecos[i] == null) return i;
            return -1;
        }

        private bool Puesta(string id) { return Array.IndexOf(_huecos, id) >= 0; }

        private void PincharEstacion(int i) {
            if (!PuedeTocar) return;
            if (_huecos[i] != null) { QuitarEtapa(i, true); return; }
            _sel = i;
            RefrescarTodas();
        }

        /// <summary>Pulsar una carta: vuela hasta el hueco elegido (o el primero libre).</summary>
        private void JugarPieza(string id) {
            if (!PuedeTocar || Puesta(id)) return;
            var hueco = HuecoLibre();
            if (hueco < 0) {
                _mano.Sacudir(id);
                Decir("No quedan huecos libres. Quita una etapa pulsando su cuadro, o prueba la tubería.");
                return;
            }
            var rt = _mano.Despegar(id, _capa);
            if (rt == null) { ColocarEtapa(id, hueco); return; }
            _volando = true;
            var desde = rt.position;
            var control = (desde + _est[hueco].position) / 2 + Vector3.up * 140f;
            var escala0 = rt.localScale.x;
            var giro0 = rt.localEulerAngles.z;
            _anim.Lanzar(0.5f, t => {
                if (rt == null) return;
                var e = Curvas.EntradaSalida(t);
                rt.position = Curvas.Bezier(desde, control, _est[hueco].position, e);   // la estacion no se mueve: se lee en cada fotograma
                rt.localScale = Vector3.one * Mathf.Lerp(escala0, 0.42f, Curvas.EntradaSalida(Mathf.Clamp01((t - 0.25f) / 0.75f)));
                rt.localEulerAngles = new Vector3(0, 0, giro0 + 360f * e);
            }, () => {
                _volando = false;
                _mano.Posar(id);
                ColocarEtapa(id, hueco);
            }, 0, rt.gameObject);
        }

        /// <summary>Soltar una carta arrastrada: si cae sobre un cuadro, se pone ahi (cambiando la que hubiera).</summary>
        private void SoltarPieza(string id, PointerEventData e) {
            if (!PuedeTocar || Puesta(id)) return;
            for (var i = 0; i < _est.Length; i++) {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_est[i], e.position, e.pressEventCamera)) continue;
                if (_huecos[i] != null) QuitarEtapa(i, false);
                _mano.Posar(id);
                ColocarEtapa(id, i);
                return;
            }
        }

        private void ColocarEtapa(string id, int hueco) {
            _huecos[hueco] = id;
            ApagarLuces();
            var libre = Enumerable.Range(0, _huecos.Length).FirstOrDefault(j => j > hueco && _huecos[j] == null);
            _sel = libre > hueco ? libre : Enumerable.Range(0, _huecos.Length).FirstOrDefault(j => _huecos[j] == null);
            if (_huecos.All(h => h != null)) _sel = -1;
            RefrescarTodas();
            Pulso(hueco, Tema.cyan);
            Chispas(_tub.Raiz, new Vector2(CentroX(hueco), _pipeY), Tema.cyan, 8, 46, true);
            var t = _def.Tramo(id);
            Decir(t.Nombre + ": " + t.QueHace);
        }

        private void QuitarEtapa(int i, bool avisar) {
            var id = _huecos[i];
            if (id == null) return;
            _huecos[i] = null;
            if (_mano != null) _mano.Mostrar(id);
            ApagarLuces();
            _sel = i;
            RefrescarTodas();
            if (avisar) Decir("«" + _def.Tramo(id).Nombre + "» vuelve a la mano.");
        }

        /// <summary>Un latido de la estacion y un aro de luz que se expande.</summary>
        private void Pulso(int i, Color color) {
            var rt = _est[i];
            _anim.Lanzar(0.42f, t => { if (rt != null) rt.localScale = Vector3.one * (1f + 0.14f * Curvas.Campana(t)); }, () => { if (rt != null) rt.localScale = Vector3.one; }, 0, rt.gameObject);
            var aro = Ui.Nodo(_tub.Raiz, "Aro");
            aro.anchorMin = aro.anchorMax = new Vector2(0, 1);
            aro.pivot = new Vector2(0.5f, 0.5f);
            aro.anchoredPosition = new Vector2(CentroX(i), -_pipeY);
            var img = aro.gameObject.AddComponent<Image>();
            img.sprite = PlanoIlustrado.Brillo();
            img.raycastTarget = false;
            _anim.Lanzar(0.6f, t => {
                if (aro == null) return;
                aro.sizeDelta = Vector2.one * Mathf.Lerp(_estLado * 0.6f, _estLado * 2.1f, Curvas.SalidaCubica(t));
                img.color = NexusTheme.Alfa(color, 0.6f * (1 - t));
            }, () => { if (aro != null) Destroy(aro.gameObject); }, 0, aro.gameObject);
        }

        private void Sacudir(RectTransform rt) {
            var origen = rt.anchoredPosition;
            _anim.Lanzar(0.45f, t => { if (rt != null) rt.anchoredPosition = origen + new Vector2(Mathf.Sin(t * 60f) * 9f * (1 - t), 0); },
                         () => { if (rt != null) rt.anchoredPosition = origen; }, 0, rt.gameObject);
        }

        // ==================================================================== probar: la luz recorre la tuberia

        /// <summary>Todo gris otra vez (y la luz, el cliente y las grietas apagados). No toca las etapas puestas.</summary>
        private void ApagarLuces() {
            if (_tub == null || _probando) return;
            for (var i = 0; i < _estLuz.Length; i++) _estLuz[i] = Luz.Neutra;
            _luzX = 0; _grietaX = -1;
            if (_luz != null) _luz.Limpiar();
            if (_nodoCliente != null) _nodoCliente.color = NexusTheme.Alfa(Tema.cyan, 0.45f);
            if (_nodoCodigo != null) _nodoCodigo.color = NexusTheme.Alfa(Tema.cyan, 0.45f);
            RefrescarTodas();
        }

        private void Probar() {
            if (!PuedeTocar) return;
            var flujo = RevisorDeTuberia.Revisar(_def, _huecos);
            _probando = true;
            for (var i = 0; i < _estLuz.Length; i++) _estLuz[i] = Luz.Neutra;
            _luzX = 0; _grietaX = -1; _luz.Limpiar();
            _nodoCliente.color = NexusTheme.Alfa(Tema.cyan, 0.45f);
            RefrescarTodas();
            _btnProbar.interactable = false;
            _paquete.gameObject.SetActive(true);
            _paqueteImg.color = new Color(1, 1, 1, 0.95f);
            _paqueteX = 40 + AnchoDelNodo;
            _luzX = _paqueteX;
            _nodoCodigo.color = Tema.cyan;
            PonerPaquete(_paqueteX);
            Decir("Probando… la luz recorre la tubería, etapa por etapa.");
            SiguienteSeccion(flujo, 0);
        }

        private void PonerPaquete(float x) {
            _paquete.anchoredPosition = new Vector2(x, -_pipeY);
            _paquete.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 18f));
        }

        private void MoverPaquete(float destino, Action alFin) {
            var x0 = _paqueteX;
            var dur = Mathf.Clamp(Mathf.Abs(destino - x0) / 1000f, 0.14f, 0.5f);
            _anim.Lanzar(dur, t => {
                if (_paquete == null) return;
                _paqueteX = Mathf.Lerp(x0, destino, Curvas.EntradaSalida(t));
                _luzX = _paqueteX;
                PonerPaquete(_paqueteX);
                RedibujarLuz();
            }, alFin, 0, _tub.Raiz.gameObject);
        }

        /// <summary>La luz que ya paso (verde, con halo) y, si la hay, la grieta roja donde se rompio el flujo.</summary>
        private void RedibujarLuz() {
            if (_luz == null) return;
            _luz.Limpiar();
            if (_luzX > _x0Tubo) {
                var halo = NexusTheme.Alfa(Tema.success, 0.22f);
                var luz = Tema.success;
                foreach (var dy in new[] { -16f, 16f }) {
                    _luz.Linea(_x0Tubo, _pipeY + dy, _luzX, _pipeY + dy, halo, 16);
                    _luz.Linea(_x0Tubo, _pipeY + dy, _luzX, _pipeY + dy, luz, 4);
                }
            }
            if (_grietaX >= 0) {
                var g = Tema.danger;
                _luz.Polilinea(new List<Vector2> {
                    new Vector2(_grietaX - 12, _pipeY - 30), new Vector2(_grietaX + 2, _pipeY - 8), new Vector2(_grietaX - 8, _pipeY + 4),
                    new Vector2(_grietaX + 8, _pipeY + 18), new Vector2(_grietaX - 2, _pipeY + 34) }, g, 5);
            }
        }

        private void SiguienteSeccion(FlujoRevisado flujo, int i) {
            if (_tub == null) return;
            if (i >= flujo.Secciones.Count) { MoverPaquete(_x1Tubo + 20, () => Final(flujo)); return; }
            var sec = flujo.Secciones[i];
            var cx = CentroX(i);
            var rompe = sec.Estado == EstadoDeSeccion.Mal;
            MoverPaquete(rompe ? cx - _estLado / 2 - 6 : cx, () => {
                if (rompe) { Romper(flujo, i); return; }
                if (sec.Estado == EstadoDeSeccion.Bien) {
                    _estLuz[i] = Luz.Bien;
                    RefrescarEstacion(i);
                    Pulso(i, Tema.success);
                    Chispas(_tub.Raiz, new Vector2(cx, _pipeY), Tema.success, 6, 36, true);
                }
                _anim.Despues(0.1f, () => SiguienteSeccion(flujo, i + 1), _tub.Raiz.gameObject);
            });
        }

        /// <summary>
        /// El flujo se rompe aqui: la estacion se pone roja y tiembla, la tuberia se agrieta con chispas, la luz se apaga y
        /// lo que haya detras se queda a oscuras (aunque este bien). Su carta salta de vuelta a la mano.
        /// </summary>
        private void Romper(FlujoRevisado flujo, int i) {
            var sec = flujo.Secciones[i];
            _estLuz[i] = Luz.Mal;
            for (var j = i + 1; j < _estLuz.Length; j++) _estLuz[j] = Luz.Apagada;
            _grietaX = _paqueteX;
            RefrescarTodas();
            RedibujarLuz();
            Sacudir(_est[i]);
            Chispas(_tub.Raiz, new Vector2(_paqueteX, _pipeY), Tema.danger, 16, 70, true);
            var img = _paqueteImg;
            _anim.Lanzar(0.6f, t => { if (img != null) img.color = new Color(1, 0.5f, 0.45f, (1 - t) * (0.5f + 0.5f * Mathf.Sin(t * 60f))); },
                         () => { if (_paquete != null) _paquete.gameObject.SetActive(false); }, 0, _tub.Raiz.gameObject);
            Decir(sec.Motivo);
            _intentos++;
            PonerIntentos();
            _anim.Despues(0.8f, () => {
                if (_tub == null) return;
                var id = _huecos[i];
                if (id != null) { _huecos[i] = null; _mano.Mostrar(id); }   // la etapa salta de vuelta a la mano
                _sel = i;
                _probando = false;
                _btnProbar.interactable = true;
                RefrescarTodas();
            }, _tub.Raiz.gameObject);
        }

        private void Final(FlujoRevisado flujo) {
            if (_tub == null) return;
            var img = _paqueteImg;
            _anim.Lanzar(0.4f, t => { if (img != null) img.color = new Color(1, 1, 1, 0.95f * (1 - t)); }, () => { if (_paquete != null) _paquete.gameObject.SetActive(false); }, 0, _tub.Raiz.gameObject);
            if (!flujo.Completa) {
                // La luz llega al final, pero faltan etapas: el cliente no se enciende.
                _nodoCliente.color = Tema.danger;
                Chispas(_tub.Raiz, new Vector2(_x1Tubo + 60, _pipeY), Tema.danger, 10, 50, true);
                Decir(flujo.MotivoFinal ?? "Faltan etapas.");
                _intentos++;
                PonerIntentos();
                _probando = false;
                _btnProbar.interactable = true;
                return;
            }
            _nodoCliente.color = Tema.success;
            _completa = true;
            _probando = false;
            _sel = -1;
            RefrescarTodas();
            Chispas(_tub.Raiz, new Vector2(_x1Tubo + 70, _pipeY), Tema.success, 26, 120, true);
            Chispas(_tub.Raiz, new Vector2(_x1Tubo + 70, _pipeY), Tema.cyan, 16, 160, true);
            Decir("¡La tubería está lista! Cada etapa en su sitio, y la luz llegó entera al cliente.");
            UiKit.Vaciar(_accion);
            _btnSiguiente = Ui.Boton(_accion, "Siguiente: los cuartos  ►", IrALosCuartos, VarianteBoton.Primario);
            UiKit.Tamano(_btnSiguiente, alto: 64);
            Aparecer((RectTransform)_btnSiguiente.transform, 0.1f);
            GuiaView.Registrar("despliegue.siguiente", (RectTransform)_btnSiguiente.transform);
        }

        /// <summary>«Siguiente» solo existe con la tuberia perfecta: antes no hay forma de pasar.</summary>
        private void IrALosCuartos() {
            if (!_completa || _transicionando || _paso != Paso.Tuberia) return;
            if (_plan.Modulos.Count == 0) { Entregar(false); return; }
            _paso = Paso.Cuartos;
            LimpiarPaso();
            Transicion(PintarCuartos);
        }

        private void TeclasDeLaTuberia() {
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado == null || _tutorialAbierto || _transicionando) return;
            if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame) {
                if (_completa) IrALosCuartos(); else Probar();
                return;
            }
            var digitos = new[] { teclado.digit1Key, teclado.digit2Key, teclado.digit3Key, teclado.digit4Key, teclado.digit5Key,
                                  teclado.digit6Key, teclado.digit7Key, teclado.digit8Key, teclado.digit9Key };
            for (var i = 0; i < digitos.Length && i < _ordenDeLaMano.Count; i++)
                if (digitos[i].wasPressedThisFrame) { JugarPieza(_ordenDeLaMano[i]); return; }
        }

        private void AbrirElTutorialDeLaTuberia() {
            if (_tub == null || _mano == null) return;
            TutorialDelPaso("despliegue-tuberia", new List<PasoDeTutorial> {
                new PasoDeTutorial { Texto = "Esta es la tubería: el código sale por la izquierda y llega al cliente por la derecha. Cada cuadro es una etapa del camino.",
                                     Objetivo = () => _est != null && _est.Length > 0 ? _est[0] : null },
                new PasoDeTutorial { Texto = "Las etapas están abajo, como cartas. Pulsa una (o arrástrala) para ponerla en el cuadro elegido. Hay más cartas que cuadros, y alguna es un atajo.",
                                     Objetivo = () => _mano != null ? _mano.Raiz : null },
                new PasoDeTutorial { Texto = "Cuando creas que está lista, pruébala: una luz recorre la tubería y se detiene donde falla. Lo que haya después no se mira.",
                                     Objetivo = () => _btnProbar != null ? (RectTransform)_btnProbar.transform : null },
                new PasoDeTutorial { Texto = "No pasarás a los cuartos hasta que la luz llegue entera al cliente. Puedes intentarlo cuantas veces haga falta, pero cada prueba fallida cuenta.", Objetivo = null }
            });
        }
    }
}
