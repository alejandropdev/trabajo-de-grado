using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Core.Relaciones;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>Hace arrastrable una tarjeta del backlog: la sigue al puntero y, al soltarla, dice a que altura cayo.</summary>
    public sealed class TarjetaArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        public Action<float> AlSoltar;     // la y (hacia abajo, en coordenadas de la lamina) donde quedo su centro
        private RectTransform _rt;

        public void OnBeginDrag(PointerEventData e) {
            _rt = (RectTransform)transform;
            _rt.SetAsLastSibling();
            _rt.localRotation = Quaternion.Euler(0, 0, 1.2f);
        }

        public void OnDrag(PointerEventData e) {
            var padre = (RectTransform)_rt.parent;
            Vector2 local, anterior;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position, e.pressEventCamera, out local);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position - e.delta, e.pressEventCamera, out anterior);
            _rt.anchoredPosition += new Vector2(0, (local - anterior).y);
        }

        public void OnEndDrag(PointerEventData e) {
            _rt.localRotation = Quaternion.identity;
            var y = -_rt.anchoredPosition.y + _rt.sizeDelta.y / 2;
            AlSoltar?.Invoke(y);
        }
    }

    /// <summary>
    /// V3 · Ordenar. El backlog como un tablero, en DOS FASES para que nunca se mezcle ordenar con contestar:
    ///   1 · Ordena   — tarjetas que se arrastran (o se suben y bajan con ▲▼), un medidor de esfuerzo y la linea de
    ///                  capacidad: lo de abajo se queda fuera. «Listo: enseñárselo al cliente» pasa a la fase 2.
    ///   2 · Contesta — el tablero queda quieto; el cliente pide algo y cada respuesta lo cambia AL MOMENTO (la
    ///                  tarjeta pedida sube, o entra a cambio de otra), con la replica del cliente. Se puede cambiar
    ///                  de respuesta cuantas veces se quiera (cada una parte del orden de la fase 1), y «Volver a
    ///                  ordenar» deshace la respuesta.
    ///
    /// Las dependencias rotas se dicen DENTRO de la tarjeta que va antes de tiempo («Necesita X ANTES · está en
    /// el puesto 3»), y la tarjeta necesitada dice quien la necesita: nada de flechas debajo que se confundan con
    /// otra tarjeta. Se ven en el modo guiado, con la ayuda de un compañero y al cerrar.
    /// </summary>
    public sealed class PantallaOrdenar : PantallaDeMinijuego {
        private List<string> _orden;
        private List<string> _ordenDeLaFase1;
        private bool _contestando;
        private string _respuesta;
        private Lamina _lamina;
        private RectTransform _respuestas, _globo, _peticion;
        private TMP_Text _resumen, _ayudaDeps, _faseTexto;
        private Button _listo, _volver, _entregar;
        private readonly Dictionary<string, RectTransform> _tarjetas = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, Button> _botonesRespuesta = new Dictionary<string, Button>();

        private OrdenarCfg Cfg { get { return Def.Ordenar; } }
        private const float Fila = 78, Y0 = 20, Izq = 130, Ancho = 1180;

        /// <summary>
        /// Las dependencias ROTAS se marcan en rojo mientras se ordena: modo guiado, ayuda de un compañero o
        /// andamiaje de tutorial (3).
        /// </summary>
        private bool VerDependencias {
            get { return Guiado || Andamiaje >= 3 || (Pendiente != null && Pendiente.TieneAyuda(TiposDeAyuda.MostrarDependencias)); }
        }

        /// <summary>
        /// Cada tarjeta dice que necesita antes (sin decir si esta bien o mal colocada): con andamiaje normal (2) o
        /// mas. Ordenar sin saber que depende de que no era priorizar, era adivinar.
        /// </summary>
        private bool VerLoQueNecesita { get { return VerDependencias || Andamiaje >= 2; } }

        private bool _avisadoDeRotas;

        private string QuienPide { get { return string.IsNullOrEmpty(Def.Presentacion.QuienEspera) || Def.Presentacion.QuienEspera == "Tú mismo" ? "el cliente" : Def.Presentacion.QuienEspera; } }

        protected override void ConstruirJuego(RectTransform cuerpo) {
            // Barajado y nunca ya resuelto: el JSON trae las tarjetas en el orden bueno. Guiado no hace falta (Marisol
            // dice el puesto de cada una, y un tablero lleno de avisos rojos distrae del recorrido). El dia entra en
            // la semilla para que repetir la practica no sea calcarla.
            _orden = Guiado ? Cfg.Tarjetas.Select(t => t.Id).ToList()
                            : OrdenarEvaluador.OrdenInicial(Cfg, App.Sesion != null ? App.Sesion.R.DiaActual : 0);
            _ordenDeLaFase1 = null;
            _contestando = false;
            _respuesta = null;
            _avisadoDeRotas = false;
            _lamina = NuevoLienzo(cuerpo, "El backlog · arrastra las tarjetas o usa ▲▼ · arriba lo primero", Izq + Ancho + 20, Y0 + (Cfg.Tarjetas.Count + 1) * Fila + 40);

            RectTransform pie;
            var lado = ColumnaLateral(cuerpo, "Cliente", 440, out pie);

            var sprint = Ui.Tarjeta(lado, "1 · Ordena el sprint");
            _faseTexto = Ui.Texto(sprint, "", EstiloTexto.Leyenda, Tema.cyan);
            _resumen = Ui.Texto(sprint, "", EstiloTexto.Cuerpo);
            _ayudaDeps = Ui.Texto(sprint, "", EstiloTexto.Pequeno);
            _listo = Ui.Boton(sprint, "Listo: enseñárselo al cliente  ►", PasarAContestar, VarianteBoton.Primario);
            GuiaView.Registrar("mj.ordenar.resumen", sprint);

            _peticion = Ui.PanelColumna(lado, "Peticion", Tema.margen * 0.75f, Tema.espacio);
            UiKit.ColorDeBorde(_peticion, Tema.warning);
            _peticion.gameObject.AddComponent<CanvasGroup>();
            Ui.Texto(_peticion, ("2 · Lo que pide " + QuienPide).ToUpperInvariant(), EstiloTexto.Leyenda, Tema.warning);
            Ui.Texto(_peticion, "«" + (Cfg.Peticion ?? "") + "»", EstiloTexto.Dialogo);
            if (!string.IsNullOrEmpty(Cfg.TarjetaPedida))
                Ui.Texto(_peticion, "Es la tarjeta con borde amarillo. Cada respuesta la mueve en el tablero: pruébalas.", EstiloTexto.Pequeno, Tema.warning);
            _respuestas = Ui.Columna(_peticion, "Respuestas", 8);
            _volver = Ui.Boton(_peticion, "◄ Volver a ordenar", VolverAOrdenar, VarianteBoton.Fantasma);
            GuiaView.Registrar("mj.ordenar.respuestas", _peticion);

            _globo = Ui.PanelColumna(lado, "Replica", Tema.margen * 0.75f, 4, Tema.surfaceRaised);
            UiKit.ColorDeBorde(_globo, Tema.cyan);

            _entregar = Ui.Boton(pie, "Entregar el orden y la respuesta", Entregar, VarianteBoton.Primario);
            GuiaView.Registrar("mj.entregar", _entregar);
            Repintar();
        }

        public override void Repintar() {
            if (_orden == null || Resultado != null) return;
            PintarBacklog(_lamina, false);

            var entran = OrdenarEvaluador.Entran(Cfg, _orden);
            var porId = Cfg.Tarjetas.ToDictionary(t => t.Id);
            _resumen.text = $"Ocupas {entran.Sum(id => porId[id].Esfuerzo)} de {Cfg.Capacidad} de esfuerzo · valor dentro: {entran.Sum(id => porId[id].Valor)}";
            _faseTexto.text = _contestando ? "Orden cerrado. Ahora contesta al cliente →" : "Ordena el tablero. Cuando lo tengas, pulsa «Listo».";
            var rotas = Rotas();
            if (VerDependencias)
                _ayudaDeps.text = rotas.Count == 0
                    ? $"<color={NexusTheme.Html(Tema.success)}>Ninguna tarjeta va antes de lo que necesita.</color>"
                    : $"<color={NexusTheme.Html(Tema.danger)}>{rotas.Count} tarjeta(s) van antes de lo que necesitan: lo dice dentro de cada una.</color>";
            else if (_avisadoDeRotas && rotas.Count > 0)
                _ayudaDeps.text = $"<color={NexusTheme.Html(Tema.danger)}>Ojo: {rotas.Count} tarjeta(s) entran antes de lo que necesitan. " +
                                  "Revisa el orden, o pulsa «Listo» otra vez para enseñarlo así.</color>";
            else
                _ayudaDeps.text = VerLoQueNecesita
                    ? "Cada tarjeta dice qué necesita antes: lo que necesita tiene que quedar por encima, y dentro de la línea."
                    : "Algunas tarjetas necesitan otra antes. Si las pones al revés, lo verás al entregar.";
            _listo.gameObject.SetActive(!_contestando);

            // Fase 2: el panel del cliente; en la fase 1 se ve apagado, para que se sepa lo que viene.
            UiKit.Vaciar(_respuestas);
            _botonesRespuesta.Clear();
            foreach (var clave in new[] { "obedecer", "rechazar", "negociar" }) {
                Respuesta resp;
                if (!Cfg.Respuestas.TryGetValue(clave, out resp)) continue;
                var c = clave;
                var boton = Ui.BotonDeOpcion(_respuestas, resp.Texto, Textos.Humanizar(c), () => Responder(c));
                boton.interactable = _contestando;
                Ui.Resaltar(boton, _respuesta == c);
                _botonesRespuesta[c] = boton;
            }
            _volver.gameObject.SetActive(_contestando);
            SetAlfa(_peticion, _contestando ? 1f : 0.45f, _contestando);

            UiKit.Vaciar(_globo);
            Respuesta elegida = null;
            if (_respuesta != null) Cfg.Respuestas.TryGetValue(_respuesta, out elegida);
            _globo.gameObject.SetActive(elegida != null);
            if (elegida != null) {
                Ui.Texto(_globo, QuienPide.ToUpperInvariant() + " RESPONDE", EstiloTexto.Leyenda, Tema.cyan);
                if (!string.IsNullOrEmpty(elegida.Replica)) Ui.Texto(_globo, "«" + elegida.Replica + "»", EstiloTexto.Dialogo);
                var efecto = QueCambio();
                if (!string.IsNullOrEmpty(efecto)) Ui.Texto(_globo, "En el tablero: " + efecto, EstiloTexto.Pequeno, Tema.warning);
            }
            _entregar.interactable = _contestando && _respuesta != null;
        }

        /// <summary>Nada de '??' con componentes de Unity: el «null falso» del editor no lo detecta.</summary>
        private static void SetAlfa(Component c, float alfa, bool interactuable) {
            var g = c.GetComponent<CanvasGroup>();
            if (g == null) g = c.gameObject.AddComponent<CanvasGroup>();
            g.alpha = alfa;
            g.interactable = interactuable;
        }

        /// <summary>Lo que la respuesta elegida le hizo al tablero, en una frase.</summary>
        private string QueCambio() {
            if (_ordenDeLaFase1 == null || string.IsNullOrEmpty(Cfg.TarjetaPedida)) return null;
            var porId = Cfg.Tarjetas.ToDictionary(t => t.Id);
            var antes = OrdenarEvaluador.Entran(Cfg, _ordenDeLaFase1);
            var ahora = OrdenarEvaluador.Entran(Cfg, _orden);
            var entraron = ahora.Except(antes).Select(id => "«" + porId[id].Titulo + "»").ToList();
            var salieron = antes.Except(ahora).Select(id => "«" + porId[id].Titulo + "»").ToList();
            if (entraron.Count == 0 && salieron.Count == 0) {
                var subio = _orden.IndexOf(Cfg.TarjetaPedida) < _ordenDeLaFase1.IndexOf(Cfg.TarjetaPedida);
                return subio ? $"«{porId[Cfg.TarjetaPedida].Titulo}» subió al puesto {_orden.IndexOf(Cfg.TarjetaPedida) + 1}." : "no cambia nada: el orden se queda como lo dejaste.";
            }
            var partes = new List<string>();
            if (entraron.Count > 0) partes.Add("entra " + string.Join(", ", entraron));
            if (salieron.Count > 0) partes.Add("se queda fuera " + string.Join(", ", salieron));
            return string.Join(" y ", partes) + ".";
        }

        // ==================================================================== las dos fases

        private void PasarAContestar() {
            if (_contestando) return;
            if (Guiado && !Guia.Permite(AccionGuiada.Listo)) { Guia.Rechazar(); return; }
            // Antes de enseñarselo al cliente, un aviso (una vez) si algo entra sin lo que necesita: es el fallo
            // que mas se castiga y antes solo se descubria al entregar. Pulsar «Listo» de nuevo sigue adelante.
            if (!Guiado && !_avisadoDeRotas && Rotas().Count > 0) {
                _avisadoDeRotas = true;
                Repintar();
                return;
            }
            _ordenDeLaFase1 = _orden.ToList();
            _contestando = true;
            Guia?.Hecho(AccionGuiada.Listo);
            Repintar();
        }

        /// <summary>Contestar: siempre a partir del orden de la fase 1, asi cambiar de respuesta no acumula efectos.</summary>
        private void Responder(string clave) {
            if (!_contestando) return;
            if (Guiado && !Guia.Permite(AccionGuiada.Responder, clave)) { Guia.Rechazar(); return; }
            _respuesta = clave;
            Respuesta r;
            _orden = Cfg.Respuestas.TryGetValue(clave, out r)
                ? OrdenarEvaluador.AplicarRespuesta(Cfg, _ordenDeLaFase1, r)
                : _ordenDeLaFase1.ToList();
            Guia?.Hecho(AccionGuiada.Responder, clave);
            Repintar();
        }

        private void VolverAOrdenar() {
            if (Guiado) { Guia.Rechazar(); return; }
            if (_ordenDeLaFase1 != null) _orden = _ordenDeLaFase1.ToList();
            _respuesta = null;
            _contestando = false;
            Repintar();
        }

        /// <summary>La etiqueta de la tarjeta pedida (y de la que sale por ella), segun lo contestado.</summary>
        private string EtiquetaDeNegociacion(string id, HashSet<string> entran) {
            if (string.IsNullOrEmpty(Cfg.TarjetaPedida)) return null;
            if (id == Cfg.TarjetaPedida) {
                if (_respuesta == null) return _contestando ? "la pide " + QuienPide : null;
                switch (_respuesta) {
                    case "obedecer": return "prometida";
                    case "rechazar": return "rechazada";
                    default: return entran.Contains(id) ? "entra (acordado)" : "para después (acordado)";
                }
            }
            if (_respuesta != null && _ordenDeLaFase1 != null && !entran.Contains(id) &&
                OrdenarEvaluador.Entran(Cfg, _ordenDeLaFase1).Contains(id))
                return "sale por la pedida";
            return null;
        }

        // ==================================================================== el tablero

        private float YDe(int indice, int linea) { return Y0 + indice * Fila + (indice >= linea ? 44 : 0); }

        private void PintarBacklog(Lamina l, bool resultado) {
            for (var i = l.Raiz.childCount - 1; i >= 0; i--)
                if (l.Raiz.GetChild(i).gameObject != l.Dibujo.gameObject) Destroy(l.Raiz.GetChild(i).gameObject);
            l.Dibujo.Limpiar();
            if (!resultado) _tarjetas.Clear();

            var porId = Cfg.Tarjetas.ToDictionary(t => t.Id);
            var entran = OrdenarEvaluador.Entran(Cfg, _orden);
            var linea = _orden.FindIndex(id => !entran.Contains(id));
            if (linea < 0) linea = _orden.Count;

            // el medidor de esfuerzo: la columna entera es TODO el backlog; se llena con lo que entra
            var total = Cfg.Tarjetas.Sum(t => t.Esfuerzo);
            var alto = _orden.Count * Fila + 44;
            var escala = alto / total;
            l.Dibujo.Rect(40, Y0, 44, alto, Tema.paperLine, 0, Tema.paperLine, false, 8);
            var yb = Y0;
            foreach (var id in _orden.Where(entran.Contains)) {
                var h = porId[id].Esfuerzo * escala;
                l.Dibujo.Rect(40, yb, 44, h - 3, Tema.papelCyan, 0, Tema.papelCyan, false, 6);
                yb += h;
            }
            var yCap = Y0 + Cfg.Capacidad * escala;
            l.Dibujo.Linea(24, yCap, 100, yCap, Tema.papelAviso, 4, true);
            l.Texto(20, yCap + 6, 90, 24, $"{entran.Sum(id => porId[id].Esfuerzo)}/{Cfg.Capacidad}", 16, Tema.papelAviso, TextAlignmentOptions.Center);

            // la linea de capacidad entre las tarjetas
            var yLinea = YDe(linea, linea) - 30;
            l.Dibujo.Linea(Izq, yLinea, Izq + Ancho, yLinea, Tema.papelAviso, 4, true);
            l.Texto(Izq, yLinea + 4, 700, 22, "CAPACIDAD DEL SPRINT · lo de abajo se queda fuera", 16, Tema.papelAviso);

            var ver = resultado || VerDependencias;
            var rotas = ver ? Rotas() : new List<KeyValuePair<string, string>>();
            var pasoGuiado = Guiado ? Guia.Actual : null;
            for (var i = 0; i < _orden.Count; i++) {
                var t = porId[_orden[i]];
                var dentro = i < linea;
                var y = YDe(i, linea);
                var rota = rotas.Where(p => p.Key == t.Id).Select(p => p.Value).ToList();
                var laNecesitan = rotas.Where(p => p.Value == t.Id).Select(p => p.Key).ToList();

                var tarjeta = Ui.Nodo(l.Raiz, "Tarjeta " + t.Id);
                Lamina.Colocar(tarjeta, Izq, y, Ancho, Fila - 10);
                if (!resultado) _tarjetas[t.Id] = tarjeta;
                // Como TaskCard/BacklogRow: el color vive en el borde izquierdo; bloqueada = danger con glow-danger, la
                // pedida por el cliente = borde amarillo (warning), dentro del sprint = cyan.
                var pedida = t.Id == Cfg.TarjetaPedida && (_contestando || resultado);
                // Una ficha de papel sobre la hoja: crema dentro del sprint, gris fuera, rosada si esta mal colocada.
                var ficha = tarjeta.gameObject.AddComponent<Image>();
                var tinte = rota.Count > 0 ? Color.Lerp(Tema.etiquetaRoja, Color.white, 0.45f) : dentro ? Color.white : new Color(0.82f, 0.82f, 0.82f, 1);
                if (!Ui.Material(ficha, MaterialesNexus.Etiqueta, 6, tinte))
                    Ui.Fondo(ficha, rota.Count > 0 ? NexusTheme.Alfa(Tema.papelPeligro, 0.16f) : Tema.papelCaja, NexusTheme.RadioSm);
                if (rota.Count > 0) Ui.Halo(tarjeta, Tema.papelPeligro, NexusTheme.RadioSm);
                Ui.Borde(tarjeta, rota.Count > 0 ? Tema.papelPeligro : pedida ? Tema.papelAviso : Tema.paperLine, NexusTheme.RadioSm, pedida ? 2 : 1);
                var franja = Ui.Nodo(tarjeta, "Franja");
                franja.anchorMin = Vector2.zero; franja.anchorMax = new Vector2(0, 1); franja.pivot = new Vector2(0, 0.5f);
                franja.sizeDelta = new Vector2(Tema.Px(3), 0);
                franja.gameObject.AddComponent<Image>().color = rota.Count > 0 ? Tema.papelPeligro : pedida ? Tema.papelAviso : dentro ? Tema.papelCyan : Tema.papelTrazo;
                if (i == 0) GuiaView.Registrar("mj.ordenar.tarjeta", tarjeta);

                var tenue = dentro ? 1f : 0.55f;
                var fila = new Lamina(Ui, tarjeta, Ancho, Fila - 10, "Contenido");
                UiKit.Rellenar(fila.Raiz);
                // el puesto, grande: es lo que se ordena
                fila.Texto(10, 0, 60, Fila - 10, (i + 1).ToString(), 26, dentro ? Tema.papelCyan : Tema.paperMuted, TextAlignmentOptions.Center, Tema.FuenteInterfazNegrita);
                fila.Texto(66, 0, 26, Fila - 10, "≡", 20, Tema.paperMuted, TextAlignmentOptions.Center);

                string aviso = null;
                Color colorAviso = Tema.paperMuted;
                if (rota.Count > 0) {
                    aviso = "¡Ojo! Necesita " + string.Join(" y ", rota.Select(d =>
                                $"«{porId[d].Titulo}» ANTES · " + (entran.Contains(d) ? $"está en el puesto {_orden.IndexOf(d) + 1}" : "y no entró"))) + ": súbela por encima.";
                    colorAviso = Tema.papelPeligro;
                } else if (laNecesitan.Count > 0) {
                    aviso = "la necesita " + string.Join(", ", laNecesitan.Select(k => $"«{porId[k].Titulo}» (puesto {_orden.IndexOf(k) + 1})")) + ": tiene que ir encima";
                    colorAviso = Tema.papelAviso;
                } else if (ver && t.DependeDe.Count > 0) {
                    aviso = "necesita: " + string.Join(", ", t.DependeDe.Select(d => "«" + porId[d].Titulo + "»")) + " · bien colocada";
                    colorAviso = Tema.papelCyan;
                } else if (VerLoQueNecesita && t.DependeDe.Count > 0) {
                    // Sin decir si esta bien o mal: solo el dato con el que se ordena.
                    aviso = "necesita antes: " + string.Join(", ", t.DependeDe.Select(d => "«" + porId[d].Titulo + "»"));
                }
                if (aviso != null) {
                    fila.Texto(104, 4, 600, 32, t.Titulo, 19, WithAlpha(Tema.paperInk, tenue), TextAlignmentOptions.MidlineLeft, null, true);
                    fila.Texto(104, 36, 690, 28, aviso, 14, colorAviso, TextAlignmentOptions.MidlineLeft);
                } else {
                    fila.Texto(104, 0, 600, Fila - 10, t.Titulo, 20, WithAlpha(Tema.paperInk, tenue), TextAlignmentOptions.MidlineLeft, null, true);
                }
                var etiqueta = EtiquetaDeNegociacion(t.Id, entran);
                if (etiqueta != null) fila.Pastilla(720, 20, etiqueta, 14, Tema.papelAviso, Color.white);
                fila.Pastilla(830, (Fila - 10) / 2, "valor " + t.Valor, 16, WithAlpha(Tema.papelCyan, tenue), Color.white);
                fila.Pastilla(950, (Fila - 10) / 2, "esfuerzo " + t.Esfuerzo, 16, Tema.paperLine, Tema.paperInk);

                if (resultado) {
                    if (!dentro) fila.Texto(Ancho - 200, 0, 180, Fila - 10, "no entró", 16, Tema.paperMuted, TextAlignmentOptions.MidlineRight);
                    continue;
                }
                if (_contestando) continue;   // en la fase 2 el tablero no se toca
                var indice = i;
                if (Guiado) {
                    // Guiado: solo se mueve la tarjeta del paso, con un boton que dice a donde.
                    if (pasoGuiado != null && pasoGuiado.Accion == AccionGuiada.Mover && pasoGuiado.Objetivo == t.Id) {
                        var destino = pasoGuiado.Valor;
                        var poner = Ui.Boton(tarjeta, $"Ponla en el puesto {destino + 1}", () => MoverGuiado(t.Id, destino), VarianteBoton.Primario);
                        Lamina.Colocar((RectTransform)poner.transform, Ancho - 250, 8, 238, Fila - 26);
                    }
                    continue;
                }
                var sube = Ui.Boton(tarjeta, "▲", () => Mover(indice, indice - 1));
                Lamina.Colocar((RectTransform)sube.transform, Ancho - 124, 8, 52, Fila - 26);
                sube.interactable = i > 0;
                var baja = Ui.Boton(tarjeta, "▼", () => Mover(indice, indice + 1));
                Lamina.Colocar((RectTransform)baja.transform, Ancho - 64, 8, 52, Fila - 26);
                baja.interactable = i < _orden.Count - 1;
                tarjeta.gameObject.AddComponent<TarjetaArrastrable>().AlSoltar = yCentro => Soltar(indice, yCentro, linea);
            }
        }

        private static Color WithAlpha(Color c, float a) { c.a *= a; return c; }

        /// <summary>Las dependencias rotas entre las tarjetas que entran: (la que va antes de tiempo, la que necesitaba).</summary>
        private List<KeyValuePair<string, string>> Rotas() {
            return OrdenarEvaluador.Rotas(Cfg, _orden);
        }

        private void Soltar(int desde, float yCentro, int linea) {
            var hasta = 0;
            for (var i = 0; i < _orden.Count; i++)
                if (i != desde && yCentro > YDe(i, linea) + Fila / 2) hasta++;
            Mover(desde, Mathf.Clamp(hasta, 0, _orden.Count - 1));
        }

        private void Mover(int desde, int hasta) {
            if (_contestando) return;
            if (hasta < 0 || hasta >= _orden.Count) { Repintar(); return; }
            var id = _orden[desde];
            _orden.RemoveAt(desde);
            _orden.Insert(hasta, id);
            _avisadoDeRotas = false;   // el orden cambio: el aviso de «Listo» vuelve a valer
            Repintar();
        }

        private void MoverGuiado(string id, int destino) {
            if (!Guia.Permite(AccionGuiada.Mover, id)) { Guia.Rechazar(); return; }
            _orden.Remove(id);
            _orden.Insert(Math.Min(destino, _orden.Count), id);
            Guia.Hecho(AccionGuiada.Mover, id);
            Repintar();
        }

        protected override RectTransform PiezaGuiada(PasoGuiado paso) {
            RectTransform rt;
            switch (paso.Accion) {
                case AccionGuiada.Mover: return _tarjetas.TryGetValue(paso.Objetivo ?? "", out rt) ? rt : null;
                case AccionGuiada.Listo: return _listo != null ? (RectTransform)_listo.transform : null;
                case AccionGuiada.Responder:
                    Button b;
                    return _botonesRespuesta.TryGetValue(paso.Objetivo ?? "", out b) ? (RectTransform)b.transform : _peticion;
                case AccionGuiada.Entregar: return _entregar != null ? (RectTransform)_entregar.transform : null;
                default: return _lamina != null ? _lamina.Raiz : null;
            }
        }

        protected override ResultadoMinijuego Evaluar() {
            // Sin respuesta al cliente, el evaluador lo da por omitido: no contestar tambien es contestar.
            // El tablero ya lleva la respuesta aplicada; aplicarla otra vez no cambia nada.
            return OrdenarEvaluador.Evaluar(Def, _orden, _respuesta);
        }

        protected override void PintarResultado(RectTransform zona) {
            var l = NuevoLienzo(zona, "Tu orden · en rojo, lo que iba antes de lo que necesita", Izq + Ancho + 20, Y0 + (Cfg.Tarjetas.Count + 1) * Fila + 40);
            PintarBacklog(l, true);
        }

        /// <summary>Un orden que da el mejor valor respetando dependencias: lo que se enseña al cerrar.</summary>
        protected override IEnumerable<string> SolucionEnTexto() {
            var porId = Cfg.Tarjetas.ToDictionary(t => t.Id);
            var mejor = OrdenarEvaluador.MejorOrden(Cfg);
            if (mejor.Count == 0) yield break;
            yield return "Un orden que habría dado el máximo valor (" + mejor.Sum(id => porId[id].Valor) + "): " +
                         string.Join(" → ", mejor.Select(id => porId[id].Titulo)) + ".";
        }
    }
}
