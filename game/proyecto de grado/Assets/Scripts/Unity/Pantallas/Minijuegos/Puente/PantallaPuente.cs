using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Puente;
using Nexus.Core.Relaciones;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// V3 · el puente del backlog (2026-11-10, sustituye a «ordenar la lista» en los retos de backlog).
    ///
    ///   · cada MODULO es un pilar de un puente y sus historias son bloques apilados; se ordenan de ABAJO hacia ARRIBA:
    ///     abajo, lo que no necesita nada; arriba, lo que necesita lo de debajo;
    ///   · «Verificar» ilumina cada pilar de abajo arriba y se detiene en la primera historia que falla: se pone roja,
    ///     el pilar pierde una vida (cada pilar tiene dos) y lo de arriba queda sin revisar. Los pilares bien puestos se
    ///     dan por hechos, se bloquean y encienden su losa del tablero;
    ///   · dos errores en el mismo pilar y el pilar se cae: el puente se pierde. Con los tres pilares verificados, el
    ///     camion del cliente cruza.
    ///
    /// Estructura (como el diagrama): NADA ocupa layout salvo la escena, que llena todo el cuerpo; el dialogo, las
    /// acciones, los corazones y el tutorial flotan encima como globos de cristal. La logica vive en Core
    /// (PuenteEvaluador, EstadoDelPuente); aqui solo se dibuja y se anima.
    /// </summary>
    public sealed partial class PantallaPuente : PantallaDeMinijuego {
        private const int SegundosPorPista = 15;

        protected override bool EstiloCristal { get { return true; } }

        private EstadoDelPuente _st;
        private Animador _anim;
        private RectTransform _capa, _vuelos, _panelVerificar;
        private GloboDeTutorial _tutorial;
        private TMP_Text _globo, _contador;
        private Button _verificar, _botonDePista, _botonColocar;
        private bool _bloqueado, _verificando, _necesidadesVisibles;
        private int _pistasPedidas;
        private string _elegido;
        private float _latidoVerificar;

        // ==================================================================== construir

        protected override void ConstruirJuego(RectTransform cuerpo) {
            // Tambien al reintentar una practica: todo de nuevo en blanco.
            if (_vuelos != null) { Destroy(_vuelos.gameObject); _vuelos = null; }
            _pistasPedidas = 0; _verificando = false; _latidoVerificar = 0; _elegido = null; _tutorial = null;
            TutorialAbierto = false;
            _st = new EstadoDelPuente(Def.Puente);
            _necesidadesVisibles = Guiado || Andamiaje >= 3 ||
                                   (Pendiente != null && Pendiente.TieneAyuda(TiposDeAyuda.MostrarDependencias));
            AsegurarAnimador();
            _bloqueado = true;   // hasta que termine la entrada

            var area = Ui.Nodo(cuerpo, "Puente");
            UiKit.Tamano(area, flexAncho: 1, flexAlto: 1);
            ConstruirEscena(area, false, Entrada);
            GuiaView.Registrar("mj.puente.pilares", area);

            // ---- los globos que flotan encima
            ConstruirGlobosDeArriba(_capa);

            // La capa por la que vuelan chispas y avisos, encima de todo y sin bloquear clics.
            _vuelos = Ui.Nodo(Raiz, "Vuelos");
            UiKit.Rellenar(_vuelos);
            _vuelos.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var grupo = _vuelos.gameObject.AddComponent<CanvasGroup>();
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
            _vuelos.SetAsLastSibling();
        }

        private void AsegurarAnimador() {
            if (_anim != null) return;
            _anim = Raiz.GetComponent<Animador>();
            if (_anim == null) _anim = Raiz.gameObject.AddComponent<Animador>();
        }

        // ---- pequeñas ayudas de layout para los globos (nada de esto entra en un layout: se ancla y ya)

        private static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos) {
            rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
            rt.anchoredPosition = pos;
        }

        private static void CrecerConElContenido(RectTransform rt, bool aLoAncho) {
            var f = rt.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            f.horizontalFit = aLoAncho ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>Un globo de cristal: vidrio oscuro con un filo de luz, como los de la Fase 1.</summary>
        private RectTransform Globo(Transform padre, string nombre, bool fila, Color? filo = null, float? relleno = null) {
            var rt = fila ? Ui.Fila(padre, nombre, Tema.espacio * 0.75f, relleno ?? Tema.espacio, TextAnchor.MiddleLeft)
                          : Ui.Columna(padre, nombre, Tema.espacio * 0.75f, relleno ?? Tema.espacio);
            var fondo = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.86f), NexusTheme.RadioLg);
            fondo.raycastTarget = true;
            Ui.Halo(rt, NexusTheme.Alfa(Tema.cyan, 0.16f), NexusTheme.RadioLg);
            Ui.Borde(rt, filo ?? NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);
            return rt;
        }

        /// <summary>Como en la Fase 1: una linea pequeña en cian y el titulo grande debajo.</summary>
        private RectTransform Titular(Transform padre, string antetitulo, string titulo, Color color) {
            var t = Ui.Columna(padre, "Titular", 2);
            Ui.Texto(t, antetitulo, EstiloTexto.Leyenda, Tema.cyan);
            var tt = Ui.Texto(t, titulo, EstiloTexto.Titulo, color);
            tt.textWrappingMode = TextWrappingModes.NoWrap;
            tt.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        /// <summary>Entra con un fundido y una escala de 0,96 a 1.</summary>
        private void Entrar(RectTransform rt, float retraso) {
            var g = rt.gameObject.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            _anim.Lanzar(0.32f, t => {
                if (g == null) return;
                var e = Curvas.SalidaCubica(t);
                g.alpha = e;
                rt.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, e);
            }, null, retraso, rt.gameObject);
        }

        /// <summary>Un texto en una caja fija (sin «alto minimo»: aqui cada texto tiene su sitio), medido desde arriba a la izquierda.</summary>
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

        // ==================================================================== los globos de arriba

        /// <summary>Arriba a la izquierda, quien espera con su dialogo y el contador; arriba a la derecha, pista y Verificar.</summary>
        private void ConstruirGlobosDeArriba(RectTransform capa) {
            var dialogo = Globo(capa, "Dialogo", false);
            Anclar(dialogo, new Vector2(0, 1), new Vector2(16, -14));
            dialogo.sizeDelta = new Vector2(640, 10);
            CrecerConElContenido(dialogo, false);
            var quien = Def.Presentacion.QuienEspera;
            var fila = Ui.Fila(dialogo, "Retrato", Tema.espacio, alineacion: TextAnchor.MiddleLeft);
            if (!string.IsNullOrEmpty(quien) && quien != "Tú mismo")
                Ui.Ilustracion(fila, MaterialesNexus.IdDePersonaje(quien), null, 72, 84);
            var textos = Ui.Columna(fila, "Texto", 2);
            UiKit.Tamano(textos, flexAncho: 1);
            var quienEs = !string.IsNullOrEmpty(quien) && quien != "Tú mismo" ? quien.ToUpperInvariant() : "TUS NOTAS";
            Ui.Texto(textos, quienEs + " · " + (Def.Presentacion.Titulo ?? Def.Id).ToUpperInvariant(), EstiloTexto.Leyenda, Tema.cyan);
            _globo = Ui.Texto(textos, "", EstiloTexto.Dialogo, Tema.ink);
            var alto = _globo.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);   // caja fija con letra que se ajusta: que no crezca el globo
            _globo.enableAutoSizing = true; _globo.fontSizeMin = 14; _globo.fontSizeMax = 20;
            UiKit.Tamano(_globo, alto: 78);
            _contador = Ui.Texto(textos, "", EstiloTexto.Pequeno, Tema.inkMuted);

            var acciones = Ui.Fila(capa, "Acciones", Tema.espacio * 0.75f, 0, TextAnchor.MiddleRight);
            Anclar(acciones, new Vector2(1, 1), new Vector2(-16, -14));
            CrecerConElContenido(acciones, true);
            acciones.gameObject.AddComponent<ListaDeElementos>();
            if (!Guiado) {
                _botonDePista = Ui.Boton(acciones, "", PedirPista, VarianteBoton.Secundario);
                PintarBotonDePista();
            } else {
                _botonColocar = Ui.Boton(acciones, "Llévalo a su puesto", ColocarGuiado, VarianteBoton.Secundario);
            }
            _verificar = Ui.Boton(acciones, "Verificar el puente", Verificar, VarianteBoton.Primario);
            UiKit.Tamano(_verificar, alto: 64);
            _panelVerificar = (RectTransform)_verificar.transform;
            GuiaView.Registrar("mj.entregar", _panelVerificar);
        }

        // ==================================================================== la entrada y el tutorial

        /// <summary>Cuando la escena ya esta dibujada y entra: se dice que hay que hacer y, al terminar, se puede jugar.</summary>
        private void Entrada() {
            Decir(Saludo());
            // Si algo de la entrada animada fallara, la partida no puede quedarse bloqueada: se juega igual.
            var fin = 1f;
            try { fin = EntradaDeLaEscena(); } catch (Exception e) { Debug.LogException(e); }
            var dueno = _lam.Raiz.gameObject;
            _anim.Despues(fin + 0.3f, () => {
                _bloqueado = false;
                Repintar();
                // La primera vez que se ve esta mecanica, el tutorial en globos (despues, tras el «?»).
                // (Con Marisol guiando ya hay un globo que explica cada paso: no se apilan dos.)
                if (TutorialPendiente) { TutorialPendiente = false; if (!Guiado) MostrarTutorial(true); }
            }, dueno);
            Repintar();
        }

        private string Saludo() {
            var n = _st.Cfg.Pilares.Count;
            return "Hay " + n + " pilares y cada uno tiene dos vidas. Ordena sus historias de abajo arriba: lo que no necesita nada, abajo. Luego, verifica.";
        }

        /// <summary>El tutorial como globos con un aro sobre la pieza que explican; sin pantalla aparte, sin tocar el layout.</summary>
        protected override void MostrarTutorial(bool primeraVez) {
            if (_capa == null || _anim == null || _pilares.Count == 0 || (_tutorial != null && _tutorial.Abierto)) return;
            var primero = _pilares[0];
            var pasos = new List<PasoDeTutorial> {
                new PasoDeTutorial { Texto = "Cada pilar es un módulo y cada bloque, una historia de usuario. Se construyen de abajo arriba.",
                                     Objetivo = () => primero.Losa },
                new PasoDeTutorial { Texto = "Abajo va lo que no necesita nada; arriba, lo que necesita lo de debajo. Arrastra los bloques para ordenarlos, o pulsa uno y usa ▲ ▼.",
                                     Objetivo = () => BloqueEnSlot(primero, 0) },
                new PasoDeTutorial { Texto = "Cada pilar tiene dos vidas. Si una historia está antes de lo que necesita, se pone roja y el pilar pierde una.",
                                     Objetivo = () => primero.Cartel },
                new PasoDeTutorial { Texto = "Pulsa Verificar: la luz sube por cada pilar y se detiene en el primer fallo. Los pilares bien puestos se quedan hechos.",
                                     Objetivo = () => _panelVerificar },
                new PasoDeTutorial { Texto = "Con los tres pilares verificados el camión del cliente cruza. Con dos errores en un mismo pilar, se cae.",
                                     Objetivo = () => _camion }
            };
            TutorialAbierto = true;
            _tutorial = GloboDeTutorial.Abrir(Ui, _capa, _anim, pasos, () => TutorialAbierto = false);
        }

        // ==================================================================== pistas

        /// <summary>
        /// Primero, la historia que primero falla en el primer pilar sin terminar; despues, las necesidades de todos los
        /// bloques en una linea pequeña. Cada una cuesta segundos del reloj, nunca vidas.
        /// </summary>
        private void PedirPista() {
            if (Resultado != null || _verificando || _bloqueado || _pistasPedidas >= 2) return;
            CobrarSegundos(SegundosPorPista);
            if (_pistasPedidas == 0) {
                _pistasPedidas++;
                var i = Enumerable.Range(0, _st.Cfg.Pilares.Count).FirstOrDefault(k => !_st.Hecho[k]);
                var rev = PuenteEvaluador.RevisarPilar(_st.Cfg.Pilares[i], _st.Ordenes[i]);
                Decir("Pista · «" + _st.Cfg.Pilares[i].Nombre + "»: " + (rev.Correcto
                    ? "este pilar ya está bien. Revisa los otros."
                    : "mira «" + Titulo(rev.HistoriaQueFalla) + "»: necesita «" + Titulo(rev.LaQueFalta) + "» más abajo."));
                var bloque = BloqueDe(rev.HistoriaQueFalla);
                if (bloque != null) bloque.Destello(Tema.warning);
            } else {
                _pistasPedidas++;
                _necesidadesVisibles = true;
                PintarNecesidades();
                Decir("Ahora cada bloque dice, en una línea, lo que necesita que esté más abajo.");
            }
            PintarBotonDePista();
        }

        private void PintarBotonDePista() {
            if (_botonDePista == null) return;
            _botonDePista.interactable = _pistasPedidas < 2 && !_necesidadesVisibles;
            _botonDePista.GetComponentInChildren<TMP_Text>().text =
                _pistasPedidas == 0 ? $"Pista (−{SegundosPorPista} s)"
                : _pistasPedidas == 1 ? $"Ver lo que necesita cada uno (−{SegundosPorPista} s)" : "Sin más pistas";
        }

        // ==================================================================== el dialogo

        /// <summary>El globo escribe la frase letra a letra (con un tope de duracion: nunca hace esperar).</summary>
        private void Decir(string frase) {
            if (_globo == null) return;
            AsegurarAnimador();
            var texto = Nexus.Core.Narrativa.Glifos.Seguro(frase ?? "");
            _globo.text = texto;
            _globo.maxVisibleCharacters = 0;
            var total = texto.Length;
            var globo = _globo;
            _anim.Lanzar(Mathf.Clamp(total * 0.016f, 0.25f, 1.1f), t => {
                if (globo == null || globo.text != texto) return;
                globo.maxVisibleCharacters = Mathf.CeilToInt(total * t);
            }, () => { if (globo != null && globo.text == texto) globo.maxVisibleCharacters = 99999; }, 0, globo.gameObject);
        }

        // ==================================================================== repintar

        public override void Repintar() {
            if (_st == null || Resultado != null || _verificar == null) return;
            var hechos = _st.Hecho.Count(h => h);
            var vidas = _st.Vidas.Sum();
            _contador.text = $"{hechos} de {_st.Cfg.Pilares.Count} pilares listos · {vidas} " + (vidas == 1 ? "vida" : "vidas");
            _verificar.interactable = !_verificando && !_bloqueado;
            _latidoVerificar = !_verificando && !_bloqueado ? 1f : 0f;
            if (_botonColocar != null) {
                var paso = Guia != null ? Guia.Actual : null;
                _botonColocar.gameObject.SetActive(paso != null && paso.Accion == AccionGuiada.Mover);
            }
            PintarEleccion();
            PintarResalteGuiado();
        }

        private void LateUpdate() {
            if (_panelVerificar == null) return;
            // «Verificar» late: es lo siguiente que hay que hacer cuando todo esta colocado.
            var s = _latidoVerificar > 0 && !_verificando && Resultado == null && TodoColocadoParaVerificar() ? 1f + 0.03f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            _panelVerificar.localScale = Vector3.one * s;
            LatirCorazones();
        }

        /// <summary>Hay algo que verificar (al menos un pilar sin hacer) y no se esta guiando un movimiento.</summary>
        private bool TodoColocadoParaVerificar() {
            if (_st == null || _st.Perdido || _st.Completo) return false;
            var paso = Guia != null ? Guia.Actual : null;
            return paso == null || paso.Accion != AccionGuiada.Mover;
        }

        // ==================================================================== teclado y guia

        protected override void TeclasDelJuego(UnityEngine.InputSystem.Keyboard teclado) {
            if (_st == null || Resultado != null || TutorialAbierto || _verificando || _bloqueado) return;
            if (teclado.upArrowKey.wasPressedThisFrame) { MoverElegido(+1); return; }
            if (teclado.downArrowKey.wasPressedThisFrame) { MoverElegido(-1); return; }
            if (teclado.tabKey.wasPressedThisFrame || teclado.rightArrowKey.wasPressedThisFrame) { CambiarDeBloque(+1); return; }
            if (teclado.leftArrowKey.wasPressedThisFrame) { CambiarDeBloque(-1); return; }
            if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame) Verificar();
        }

        protected override RectTransform PiezaGuiada(PasoGuiado paso) {
            switch (paso.Accion) {
                case AccionGuiada.Mover: { var b = BloqueDe(paso.Objetivo); return b != null ? b.Rt : null; }
                case AccionGuiada.Listo:
                case AccionGuiada.Entregar: return _verificar != null ? (RectTransform)_verificar.transform : null;
                default: return null;
            }
        }

        // ==================================================================== evaluar

        protected override ResultadoMinijuego Evaluar() {
            return PuenteEvaluador.Evaluar(Def, _st ?? new EstadoDelPuente(Def.Puente));
        }

        protected override IEnumerable<string> SolucionEnTexto() {
            foreach (var p in Def.Puente.Pilares) {
                var porId = p.Historias.ToDictionary(h => h.Id);
                yield return "«" + p.Nombre + "», de abajo arriba: " + string.Join(" → ", PuenteEvaluador.Solucion(p).Select(id => porId[id].Titulo));
            }
        }

        /// <summary>No se usa (el cierre es propio), pero la base lo exige.</summary>
        protected override void PintarResultado(RectTransform zona) { }
    }
}
