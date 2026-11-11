using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
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
    /// V2 · estimar las pruebas como un edificio que defiende al cliente (2026-11-11, sustituye a las «pilas» de horas).
    ///
    ///   · el sistema vive en el EDIFICIO DEL CLIENTE (la recepcion del edificio N1, los patios y ascensores de
    ///     Facilities), en corte y con forma de piramide: cada PISO es un tipo de prueba, con lo barato abajo (las piezas
    ///     sueltas) y lo caro arriba (el recorrido completo). Los bichos son incidencias del proyecto y entran a la
    ///     altura de su tipo: cada piso solo frena los de su altura;
    ///   · se reparten las horas (monedas que se arrastran al piso, o + y −). Sobre cada piso, las siluetas son los
    ///     bichos que cabe esperar y las redes, lo que frenan las horas puestas: jugar es hacer que las redes alcancen;
    ///   · «¡Sacar la versión!» suelta UNA oleada animada: lo que frena un piso se atrapa; lo demas atraviesa el edificio
    ///     y llega al cliente (la gente de la garita), cuyo animo baja.
    ///
    /// Estructura (como el diagrama y el puente): NADA ocupa layout salvo la escena, que llena el cuerpo; el dialogo,
    /// las acciones y el tutorial flotan encima como globos de cristal. La logica es la de siempre
    /// (RepartirEvaluador, TiposDePrueba); aqui solo se dibuja y se anima.
    /// </summary>
    public sealed partial class PantallaEdificio : PantallaDeMinijuego {
        private const int SegundosPorPista = 15;

        protected override bool EstiloCristal { get { return true; } }

        private readonly Dictionary<string, int> _asignacion = new Dictionary<string, int>();
        private Animador _anim;
        private RectTransform _capa, _vuelos;
        private GloboDeTutorial _tutorial;
        private TMP_Text _globo, _contador;
        private Button _soltar, _botonDePista, _botonSaltar;
        private bool _bloqueado, _soltando, _cuentaPedida;
        private int _ola;

        private RepartirCfg Cfg { get { return Def.Repartir; } }
        private int Paso { get { return Math.Max(1, Cfg.Paso); } }
        private int Usado { get { return _asignacion.Values.Sum(); } }
        private int Libres { get { return Cfg.Presupuesto - Usado; } }

        /// <summary>Se ve cuantos bichos hay de verdad: ayuda de un compañero.</summary>
        private bool Revelado { get { return Pendiente != null && Pendiente.TieneAyuda(TiposDeAyuda.RevelarDefectos); } }

        /// <summary>La cuenta de cuantos se colarian (la hace sola con andamiaje 3, o se paga con reloj).</summary>
        private bool VerCuenta { get { return !Revelado && (Andamiaje >= 3 || _cuentaPedida) && Cfg.Depositos.Any(d => d.EstimadoMax > 0); } }

        // ==================================================================== construir

        protected override void ConstruirJuego(RectTransform cuerpo) {
            // Tambien al reintentar una practica: todo de nuevo en blanco.
            if (_vuelos != null) { Destroy(_vuelos.gameObject); _vuelos = null; }
            _ola++;
            _soltando = false; _cuentaPedida = false; _tutorial = null; _lam = null; _botonSaltar = null;
            TutorialAbierto = false;
            _asignacion.Clear();
            foreach (var d in Cfg.Depositos) _asignacion[d.Id] = 0;
            // Si alguien del equipo ya trae un reparto, se empieza con el puesto: lo que toca es revisarlo.
            if (Cfg.Propuesta != null && !Guiado)
                foreach (var kv in Cfg.Propuesta) if (_asignacion.ContainsKey(kv.Key)) _asignacion[kv.Key] = kv.Value;
            AsegurarAnimador();
            _bloqueado = true;   // hasta que termine la entrada

            var area = Ui.Nodo(cuerpo, "Edificio");
            UiKit.Tamano(area, flexAncho: 1, flexAlto: 1);
            ConstruirEscena(area, false, Entrada);
            GuiaView.Registrar("mj.repartir.pilas", area);

            ConstruirGlobosDeArriba(_capa);

            // La capa por la que vuelan monedas y avisos, encima de todo y sin bloquear clics.
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

        /// <summary>Un texto en una caja fija (sin «alto minimo»), medido desde arriba a la izquierda.</summary>
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

        /// <summary>Arriba a la izquierda, quien espera con su dialogo y el contador; arriba a la derecha, pista y «Sacar la version».</summary>
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
            // Sin andamiaje 3, la cuenta de cuantos se colarian se pide (y cuesta reloj); con el, ya sale sola.
            if (!Guiado && Andamiaje < 3 && !Revelado && Cfg.Depositos.Any(d => d.EstimadoMax > 0)) {
                _botonDePista = Ui.Boton(acciones, $"Hacer la cuenta (−{SegundosPorPista} s)", PedirCuenta, VarianteBoton.Secundario);
            }
            _botonSaltar = Ui.Boton(acciones, "Saltar  ▶▶", SaltarLaOleada, VarianteBoton.Fantasma);
            _botonSaltar.gameObject.SetActive(false);
            _soltar = Ui.Boton(acciones, "¡Sacar la versión!", Soltar, VarianteBoton.Primario);
            UiKit.Tamano(_soltar, alto: 64);
            GuiaView.Registrar("mj.entregar", (RectTransform)_soltar.transform);
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
            if (Cfg.Propuesta != null && !Guiado)
                return (string.IsNullOrEmpty(Cfg.QuienPropone) ? "El equipo" : Cfg.QuienPropone) + " ya trae este reparto. Revísalo piso a piso: si cubre el riesgo, entrégalo tal cual.";
            return "Cada piso del edificio frena un tipo de bicho. Reparte las horas: no alcanzan para todo.";
        }

        /// <summary>El tutorial como globos con un aro sobre la pieza que explican; sin pantalla aparte, sin tocar el layout.</summary>
        protected override void MostrarTutorial(bool primeraVez) {
            if (_capa == null || _anim == null || _pisos == null || _pisos.Count == 0 || (_tutorial != null && _tutorial.Abierto)) return;
            var @base = _vistas[_pisos[0].Id];
            var critico = _pisos.FirstOrDefault(d => d.Critico);
            var pasos = new List<PasoDeTutorial> {
                new PasoDeTutorial { Texto = "Es el edificio del cliente, con un piso por tipo de prueba. Abajo, las piezas sueltas; arriba, el recorrido completo. Cada piso frena los bichos de su altura.",
                                     Objetivo = () => @base.Rt },
                new PasoDeTutorial { Texto = "Las siluetas son los bichos que cabe esperar y las redes, lo que frenas con tus horas. Haz que las redes alcancen a las siluetas.",
                                     Objetivo = () => @base.Rt },
                new PasoDeTutorial { Texto = "Arrastra monedas de horas al piso, o usa + y −. No alcanza para todo: lo barato abajo, lo caro solo donde haga falta.",
                                     Objetivo = () => _bolsa }
            };
            if (critico != null)
                pasos.Add(new PasoDeTutorial { Texto = "El escudo marca el piso que no puede fallar: si se cuela un bicho ahí, pierdes.", Objetivo = () => _vistas[critico.Id].Rt });
            pasos.Add(new PasoDeTutorial { Texto = "Cuando estés listo, saca la versión: una oleada de bichos intentará llegar al cliente.", Objetivo = () => (RectTransform)_soltar.transform });
            TutorialAbierto = true;
            _tutorial = GloboDeTutorial.Abrir(Ui, _capa, _anim, pasos, () => TutorialAbierto = false);
        }

        // ==================================================================== la cuenta

        private void PedirCuenta() {
            if (_cuentaPedida || _soltando || _bloqueado || Resultado != null) return;
            _cuentaPedida = true;
            CobrarSegundos(SegundosPorPista);
            if (_botonDePista != null) _botonDePista.interactable = false;
            Decir("Hecha la cuenta: arriba verás cuántos bichos podrían colarse con este reparto.");
            Repintar();
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

        // ==================================================================== teclado y guia

        protected override void TeclasDelJuego(UnityEngine.InputSystem.Keyboard teclado) {
            if (Resultado != null || TutorialAbierto || _soltando || _bloqueado || _pisos == null) return;
            if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame) { Soltar(); return; }
            var digitos = new[] { teclado.digit1Key, teclado.digit2Key, teclado.digit3Key, teclado.digit4Key };
            var shift = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
            for (var i = 0; i < digitos.Length && i < _pisos.Count; i++)
                if (digitos[i].wasPressedThisFrame) { Sumar(_pisos[i].Id, shift ? -Paso : Paso); return; }
        }

        protected override RectTransform PiezaGuiada(PasoGuiado paso) {
            RectTransform rt;
            switch (paso.Accion) {
                case AccionGuiada.Asignar: return _mas.TryGetValue(paso.Objetivo ?? "", out rt) ? rt : null;
                case AccionGuiada.Entregar: return _soltar != null ? (RectTransform)_soltar.transform : null;
                default:
                    PisoVista pv;
                    if (!string.IsNullOrEmpty(paso.Objetivo) && _vistas.TryGetValue(paso.Objetivo, out pv)) return pv.Rt;
                    return _lam != null ? _lam.Raiz : null;
            }
        }

        // ==================================================================== evaluar

        protected override ResultadoMinijuego Evaluar() {
            return RepartirEvaluador.Evaluar(Def, _asignacion);
        }

        protected override IEnumerable<string> SolucionEnTexto() {
            // El que se podia razonar con lo que se veia, no el que sale de saber lo escondido.
            var ganador = RepartirEvaluador.RepartoPrudente(Cfg);
            yield return "Un reparto que ganaba seguro: " + string.Join(" · ", TiposDePrueba.Pisos(Cfg).Select(d => $"{TiposDePrueba.Para(d).Nombre.ToLowerInvariant()} {ganador[d.Id]} h")) +
                         ": cubre lo máximo que cabía esperar de cada tipo, renunciando solo a lo más caro de frenar.";
            yield return "Había " + string.Join(", ", TiposDePrueba.Pisos(Cfg).Select(d => $"{d.DefectosOcultos} en {TiposDePrueba.Para(d).Nombre.ToLowerInvariant()}")) + ".";
        }

        /// <summary>No se usa (el cierre es propio), pero la base lo exige.</summary>
        protected override void PintarResultado(RectTransform zona) { }
    }
}
