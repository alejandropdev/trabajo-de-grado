using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Jornada;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Juego;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La Fase 2 rediseñada (2026-11-06): poco texto y pocos botones, y lo que se mira, a pantalla completa.
    ///
    ///   HUD fijo        arriba: «Mapa» y «Backlog» a la izquierda, el reloj en el centro, «Pausa» y «Menú» a la derecha;
    ///                   abajo del todo: los puntos del carrusel (Indicadores, Burndown, Tablero, Equipo…); justo encima,
    ///                   «M» (mensajes: avisos, el cliente, la bitacora) a la izquierda y «Producto» a la derecha
    ///   el escenario    en el escritorio, un carrusel de diapositivas (indicadores, la grafica de la metodologia, el
    ///                   tablero, el equipo, el escritorio, el cliente); en otra zona, la escena viva de esa zona
    ///   un momento      lo que hay que decidir (decision, retro, planificacion, cierre, resumen…) sale como una carta
    ///                   encima, con los mismos pasos de siempre (PintarAhora); al terminar, se va y vuelve el carrusel
    ///   los avisos      no tapan nada: bajan como una tira arriba, con su cuenta atras y «Atender»
    /// </summary>
    public sealed partial class PantallaDelDia {
        private RectTransform _escenario, _capaMomento, _tarjetaMomento, _tiras, _bandeja, _cuerpoBandeja, _contenedorZona;
        private CanvasGroup _grupoMomento, _grupoBandeja;
        private ScrollRect _scrollMomento;
        private RectTransform _fijoMomento;
        private CarruselDeDiapositivas _carrusel;
        private TMP_Text _textoM, _subtituloHud, _desgloseM;
        private RectTransform _globoM;
        private float _momento01, _bandeja01;
        private bool _bandejaAbierta;
        private int _pestanaBandeja;
        private string _claveTiras, _claveBandeja, _zonaPintada;
        private static string _diapositivaRecordada;
        private readonly List<KeyValuePair<Alerta, TMP_Text>> _cuentasDeTiras = new List<KeyValuePair<Alerta, TMP_Text>>();

        // AltoPie = lo que ocupa el pie, de abajo hacia arriba: los puntos del carrusel (Indicadores, Burndown, Tablero…)
        // pegados al borde, y justo encima «M» y «Producto». El escenario empieza sobre todo eso.
        private const float AltoHud = 92f, AltoPie = 148f;

        /// <summary>Altura (desde el borde de abajo) de «M» y «Producto»: sobre los puntos, con aire.</summary>
        private const float BaseDeMYProducto = CarruselDeDiapositivas.AltoDeLosPuntos + 12f;

        /// <summary>Todo lo que se ve: HUD, escenario, la carta del momento, las tiras de avisos y la bandeja M.</summary>
        private void ConstruirEscenario() {
            // ---- el escenario: carrusel (escritorio) o escena de zona
            _escenario = Ui.Nodo(Raiz, "Escenario");
            _escenario.anchorMin = Vector2.zero;
            _escenario.anchorMax = Vector2.one;
            _escenario.offsetMin = new Vector2(16, AltoPie);
            _escenario.offsetMax = new Vector2(-16, -AltoHud - 4);
            ConstruirCarrusel();
            _contenedorZona = Ui.Nodo(_escenario, "Zona");
            UiKit.Rellenar(_contenedorZona);

            ConstruirHud();
            ConstruirPie();

            // ---- las tiras de avisos, arriba en el centro
            _tiras = Ui.Columna(Raiz, "Avisos", 8, 0, TextAnchor.UpperCenter);
            _tiras.anchorMin = _tiras.anchorMax = _tiras.pivot = new Vector2(0.5f, 1);
            _tiras.anchoredPosition = new Vector2(0, -AltoHud - 8);
            _tiras.sizeDelta = new Vector2(820, 0);
            _tiras.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _tiras.gameObject.AddComponent<ListaDeElementos>();

            ConstruirBandeja();
            ConstruirMomento();
        }

        // ==================================================================== HUD

        private void ConstruirHud() {
            var hud = Ui.Fila(Raiz, "HUD", Tema.espacio, Tema.Espacio(3), TextAnchor.MiddleCenter);
            hud.anchorMin = new Vector2(0, 1);
            hud.anchorMax = new Vector2(1, 1);
            hud.pivot = new Vector2(0.5f, 1);
            hud.sizeDelta = new Vector2(0, AltoHud);
            hud.anchoredPosition = Vector2.zero;

            var izquierda = Ui.Fila(hud, "Izquierda", Tema.Espacio(2));
            var mapa = Ui.Boton(izquierda, "Mapa", AbrirMapa);
            Ui.Boton(izquierda, "Backlog", AbrirBacklog);
            GuiaView.Registrar("dia.mapa", mapa);
            GuiaView.RegistrarRespaldo("dia.aqui", mapa);
            GuiaView.RegistrarRespaldo("mapa.", mapa);
            Ui.Resorte(hud);

            // El reloj y su velocidad (×1/×2/×4): una sola pieza. Ya no hay «adelantar» (2026-11-07): el dia pasa solo.
            var centro = Ui.Columna(hud, "Reloj", 2, 0, TextAnchor.MiddleCenter);
            var fila = Ui.Fila(centro, "Tiempo", Tema.Espacio(2), alineacion: TextAnchor.MiddleCenter);
            GuiaView.Registrar("dia.reloj", RelojView.CrearCompacto(Ui, fila, Runner));
            var controles = Ui.Fila(fila, "Controles", 6, alineacion: TextAnchor.MiddleCenter);
            controles.gameObject.AddComponent<ListaDeElementos>();
            var velocidad = Ui.Boton(controles, RotuloDeVelocidad(), () => {
                Runner.Velocidad = Runner.Velocidad < 1.5 ? 2 : Runner.Velocidad < 3 ? 4 : 1;
                _textoVelocidad.text = RotuloDeVelocidad();
            }, VarianteBoton.Fantasma);
            UiKit.Tamano(velocidad, ancho: 84);
            _textoVelocidad = velocidad.GetComponentInChildren<TMP_Text>();
            _subtituloHud = Ui.Texto(centro, "", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);
            Ui.Resorte(hud);

            var derecha = Ui.Fila(hud, "Derecha", Tema.Espacio(2));
            _pausa = Ui.Boton(derecha, "Pausa", () => Runner.Pausado = !Runner.Pausado);
            UiKit.Tamano(_pausa, ancho: 130);
            GuiaView.Registrar("dia.velocidad", _pausa);
            var menu = Ui.Boton(derecha, "Menú", () => App.Router.Apilar<PantallaDePausa>());
            GuiaView.Registrar("dia.diario", menu);   // el diario vive en «Menú» (la pausa)
        }

        /// <summary>Backlog: en Scrum, el product backlog; en las demas, el tablero (lo «por hacer» es su backlog).</summary>
        private void AbrirBacklog() {
            var tieneBacklog = Politica != null && Politica.Tiene(Nexus.Core.Metodologia.VistasDeMonitoreo.Backlog);
            AbrirTablero(tieneBacklog ? 2 : 0);
        }

        // ==================================================================== pie: M y Producto (sobre los puntos del carrusel)

        private void ConstruirPie() {
            // «M»: un circulo grande con su contador.
            var m = Ui.Nodo(Raiz, "Mensajes");
            m.anchorMin = m.anchorMax = m.pivot = new Vector2(0, 0);
            m.anchoredPosition = new Vector2(24, BaseDeMYProducto);
            m.sizeDelta = new Vector2(78, 78);
            var fondo = m.gameObject.AddComponent<Image>();
            fondo.sprite = Ui.SpriteCircular();
            fondo.color = Tema.surfaceRaised;
            Ui.Borde(m, Tema.cyan, 39, 2);
            var boton = m.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            boton.onClick.AddListener(AlternarBandeja);
            var letra = Ui.Texto(m, "M", EstiloTexto.Titulo, Tema.cyan, TextAlignmentOptions.Center);
            QuitarAltoMinimo(letra);
            UiKit.Rellenar(letra.rectTransform);
            // Al pasar el raton: de que es el numero (avisos, cliente, novedades).
            var desglose = Ui.PanelColumna(m, "Desglose", Tema.Espacio(3), 0, NexusTheme.Alfa(Tema.bg950, 0.95f));
            desglose.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            desglose.anchorMin = desglose.anchorMax = new Vector2(0, 1);
            desglose.pivot = new Vector2(0, 0);
            desglose.anchoredPosition = new Vector2(0, 10);
            desglose.sizeDelta = new Vector2(420, 48);
            Ui.Borde(desglose, Tema.cyan, NexusTheme.RadioMd, 1);
            _desgloseM = Ui.Texto(desglose, "", EstiloTexto.Pequeno, Tema.ink);
            desglose.gameObject.SetActive(false);
            var vivaM = m.gameObject.AddComponent<PiezaViva>();
            vivaM.Escala = 1.1f;
            vivaM.Revelar = desglose.gameObject;
            GuiaView.Registrar("dia.mensajes", m);

            _globoM = Ui.Nodo(m, "Contador");
            _globoM.anchorMin = _globoM.anchorMax = new Vector2(1, 1);
            _globoM.sizeDelta = new Vector2(32, 32);
            _globoM.anchoredPosition = new Vector2(-6, -6);
            var fondoGlobo = _globoM.gameObject.AddComponent<Image>();
            fondoGlobo.sprite = Ui.SpriteCircular();
            fondoGlobo.color = Tema.danger;
            fondoGlobo.raycastTarget = false;
            _textoM = Ui.Texto(_globoM, "", EstiloTexto.Etiqueta, Color.white, TextAlignmentOptions.Center);
            QuitarAltoMinimo(_textoM);
            UiKit.Rellenar(_textoM.rectTransform);
            _globoM.gameObject.SetActive(false);

            // «Producto»: lo construido, a pantalla completa.
            var producto = Ui.Boton(Raiz, "Producto", () => PantallaDelProducto.Abrir(App), VarianteBoton.Primario);
            var rp = (RectTransform)producto.transform;
            rp.anchorMin = rp.anchorMax = rp.pivot = new Vector2(1, 0);
            rp.sizeDelta = new Vector2(190, 62);
            rp.anchoredPosition = new Vector2(-24, BaseDeMYProducto + 8);   // centrado con el circulo de la «M» (78 contra 62)
            GuiaView.Registrar("dia.producto", producto);
        }

        private static void QuitarAltoMinimo(TMP_Text t) {
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // ==================================================================== el carrusel

        private void ConstruirCarrusel() {
            var hayCliente = S.Perfil.Proyecto != null && S.Perfil.Proyecto.Papeles.Count > 0;
            var ids = DiapositivasDelDia.Para(Politica != null ? Politica.Vistas : null, hayCliente);
            var lista = ids.Select(id => new CarruselDeDiapositivas.Diapositiva {
                Id = id,
                Nombre = DiapositivasDelDia.Nombre(id),
                Pintar = cuerpo => PintarDiapositiva(id, cuerpo),
                Clave = () => ClaveDeDiapositiva(id)
            }).ToList();
            // El carrusel baja hasta el borde de la pantalla: sus puntos van abajo del todo, con «M» y «Producto» encima.
            _carrusel = CarruselDeDiapositivas.Crear(Ui, _escenario, lista, _diapositivaRecordada, AltoPie);
            _carrusel.Bloqueado = () => _momento01 > 0.02f || _bandejaAbierta || _capaInteraccion != null;
            _carrusel.AlCambiar = id => _diapositivaRecordada = id;

            // La guia: lo que vive en una diapositiva que no se ve, se señala en su punto del carrusel.
            Respaldo("dia.proyecto", DiapositivasDelDia.Kpis);
            Respaldo("dia.pronostico", DiapositivasDelDia.Kpis);
            Respaldo("dia.tablero", DiapositivasDelDia.Tablero);
            Respaldo("dia.equipo", DiapositivasDelDia.Equipo);
            Respaldo("dia.oficina", DiapositivasDelDia.Escritorio);
            Respaldo("dia.ceremonias", DiapositivasDelDia.Escritorio);
            Respaldo("dia.cliente", hayCliente ? DiapositivasDelDia.Cliente : DiapositivasDelDia.Escritorio);
        }

        private void Respaldo(string clave, string diapositiva) {
            var punto = _carrusel.PuntoDe(diapositiva);
            if (punto != null) GuiaView.RegistrarRespaldo(clave, punto);
        }

        // ==================================================================== la carta del momento

        private void ConstruirMomento() {
            _capaMomento = Ui.Nodo(Raiz, "Momento");
            UiKit.Rellenar(_capaMomento);
            _grupoMomento = _capaMomento.gameObject.AddComponent<CanvasGroup>();
            var velo = _capaMomento.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(Tema.bg950, 0.62f);

            _tarjetaMomento = Ui.PanelColumna(_capaMomento, "Carta", Tema.margen, Tema.espacio, Tema.surface);
            _tarjetaMomento.anchorMin = new Vector2(0.5f, 0.05f);
            _tarjetaMomento.anchorMax = new Vector2(0.5f, 0.9f);
            _tarjetaMomento.sizeDelta = new Vector2(1180, 0);
            _tarjetaMomento.anchoredPosition = Vector2.zero;
            Ui.Halo(_tarjetaMomento, NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg);
            Ui.Borde(_tarjetaMomento, Tema.cyan, NexusTheme.RadioLg, 1.5f);
            RectTransform contenido;
            var scroll = Ui.Desplazable(_tarjetaMomento, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            _ahora = new Hoja(Ui, contenido);
            _scrollMomento = scroll;
            _fijoMomento = Ui.Columna(_tarjetaMomento, "Fijo", Tema.espacio);
            UiKit.Tamano(_fijoMomento, flexAncho: 1, flexAlto: 1);
            _fijoMomento.gameObject.SetActive(false);
            GuiaView.Registrar("dia.ahora", _tarjetaMomento);

            _capaMomento.gameObject.SetActive(false);
            _grupoMomento.alpha = 0;
        }

        /// <summary>Los pasos que salen como carta encima; en la jornada (y con avisos) se ve el escenario.</summary>
        private static bool EsMomento(Paso paso) {
            // Las horas extra no son una carta que tape: se juegan (recorrer, hablar…) y una tira arriba deja irse.
            return paso != Paso.Jornada && paso != Paso.Aviso && paso != Paso.Prorroga;
        }

        // ==================================================================== la bandeja «M»

        private void ConstruirBandeja() {
            _bandeja = Ui.PanelColumna(Raiz, "Bandeja", Tema.margen * 0.75f, Tema.espacio, Tema.surface);
            _bandeja.anchorMin = new Vector2(0, 0);
            _bandeja.anchorMax = new Vector2(0, 1);
            _bandeja.pivot = new Vector2(0, 0.5f);
            _bandeja.sizeDelta = new Vector2(600, -(AltoHud + AltoPie + 10));
            _bandeja.anchoredPosition = new Vector2(-640, (AltoPie - AltoHud) / 2f);
            Ui.Borde(_bandeja, Tema.cyan, NexusTheme.RadioLg, 1.5f);
            _grupoBandeja = _bandeja.gameObject.AddComponent<CanvasGroup>();
            _bandeja.gameObject.AddComponent<ListaDeElementos>();

            var cabecera = Ui.Fila(_bandeja, "Cabecera", Tema.Espacio(2));
            UiKit.Tamano(Ui.Texto(cabecera, "TU BUZÓN", EstiloTexto.Leyenda, Tema.cyan), flexAncho: 1);
            Ui.Boton(cabecera, "Cerrar", AlternarBandeja, VarianteBoton.Fantasma);
            RectTransform contenido;
            var scroll = Ui.Desplazable(_bandeja, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            _cuerpoBandeja = contenido;
            _bandeja.gameObject.SetActive(false);
        }

        /// <summary>
        /// Escape en cualquier sitio de la Fase 2 (2026-11-07): cierra lo que haya abierto encima del dia (la bandeja M, la
        /// escena de «Interactuar») y, si no hay nada, abre la pausa. Un solo sitio decide: si la bandeja o la capa se
        /// cerraran por su cuenta, el mismo Escape abriria ademas la pausa.
        /// </summary>
        public override bool AlPulsarEscape() {
            if (_bandejaAbierta) { AlternarBandeja(); return true; }
            if (_capaInteraccion != null) { CerrarInteraccion(); return true; }
            App.Router.Apilar<PantallaDePausa>();
            return true;
        }

        private void AlternarBandeja() {
            _bandejaAbierta = !_bandejaAbierta;
            _claveBandeja = null;
            if (_bandejaAbierta) {
                _bandeja.gameObject.SetActive(true);
                _bandeja.SetAsLastSibling();
                Runner.Mirando = true;   // mirar los mensajes es gratis: el reloj espera
            } else if (_panel == null) {
                Runner.Mirando = false;
            }
        }

        /// <summary>
        /// El buzon (2026-11-06): lo nuevo llega en SOBRES que se abren con un clic (giran y enseñan lo que traen), y el
        /// numero de la «M» baja con cada sobre abierto. Asi se ve que era ese numero y como se quita. Los avisos que
        /// suenan van arriba, abiertos, con «Atender». Debajo, lo que ha pasado.
        /// Solo se repinta al abrir la bandeja o si cambian los avisos: un sobre abierto se queda abierto mientras se mira.
        /// </summary>
        private void PintarBandeja() {
            var clave = string.Join(",", AlertasSonando().Select(a => a.Id));
            if (clave == _claveBandeja) return;
            _claveBandeja = clave;
            UiKit.Vaciar(_cuerpoBandeja);

            foreach (var a in AlertasSonando()) {
                var alerta = a;
                var t = Ui.Tarjeta(_cuerpoBandeja, "AVISO · " + a.Canal.ToUpperInvariant(), Tono.Aviso);
                Ui.Texto(t, a.Texto, EstiloTexto.Pequeno, Tema.ink);
                Ui.Boton(Ui.Fila(t), "Atender", () => { AlternarBandeja(); Atender(alerta); }, VarianteBoton.Primario);
            }

            var sobres = 0;
            if (S.Perfil.Proyecto != null)
                foreach (var p in S.PapelesDelCliente().Where(x => !S.R.PapelesLeidos.Contains(x.Id))) {
                    var papel = p;
                    Sobre(_cuerpoBandeja, "CLIENTE", Tema.cyan, $"Día {p.Dia} · {p.De}", p.Titulo, p.QueTeDice,
                          () => { if (!S.R.PapelesLeidos.Contains(papel.Id)) S.R.PapelesLeidos.Add(papel.Id); });
                    sobres++;
                }
            var novedades = S.R.BitacoraSinVer;
            // Solo lo que paso sin ti (RuntimeState.Novedades); cada sobre abierto da por vista la suya.
            foreach (var linea in S.R.Novedades.Take(6).ToList()) {
                var l = linea;
                Sobre(_cuerpoBandeja, "NOVEDAD", Tema.inkMuted, "Pasó sin ti", null, l, () => S.R.VerNovedad(l));
                sobres++;
            }
            if (novedades > 6) Ui.Texto(_cuerpoBandeja, $"…y {novedades - 6} novedades más en la bitácora.", EstiloTexto.Leyenda, Tema.inkMuted);
            if (sobres == 0 && AlertasSonando().Count == 0)
                Ui.Texto(_cuerpoBandeja, "Todo leído. Aquí llegarán los avisos, el cliente y lo que vaya pasando.", EstiloTexto.Cuerpo, Tema.inkMuted);

            Ui.Separador(_cuerpoBandeja);
            Ui.Texto(_cuerpoBandeja, "LO QUE HA PASADO", EstiloTexto.Leyenda, Tema.inkMuted);
            foreach (var linea in S.R.Bitacora.Take(12)) Ui.Texto(_cuerpoBandeja, linea, EstiloTexto.Pequeno, Tema.inkMuted);
        }

        /// <summary>Un sobre cerrado (de que seccion es y un titulo); al pulsarlo gira, enseña lo que trae y se da por leido.</summary>
        private void Sobre(RectTransform padre, string seccion, Color color, string de, string titulo, string contenido, Action alAbrir) {
            var t = Ui.PanelColumna(padre, "Sobre", Tema.Espacio(4), Tema.Espacio(1), Tema.surfaceRaised);
            Ui.Borde(t, color, NexusTheme.RadioMd, 2);
            var frente = Ui.Columna(t, "Cerrado", 2);
            Ui.Texto(frente, seccion + " · " + de, EstiloTexto.Leyenda, color);
            if (!string.IsNullOrEmpty(titulo)) Ui.Texto(frente, titulo, EstiloTexto.Encabezado, Tema.ink);
            Ui.Texto(frente, "Clic para abrir", EstiloTexto.Leyenda, Tema.inkFaint);
            var dentro = Ui.Columna(t, "Abierto", 2);
            Ui.Texto(dentro, seccion + " · " + de, EstiloTexto.Leyenda, color);
            if (!string.IsNullOrEmpty(titulo)) Ui.Texto(dentro, titulo, EstiloTexto.Encabezado, Tema.ink);
            if (!string.IsNullOrEmpty(contenido)) Ui.Texto(dentro, contenido, EstiloTexto.Pequeno, Tema.ink);
            if (seccion == "CLIENTE") Ui.Boton(Ui.Fila(dentro), "Ver en la pizarra", () => { AlternarBandeja(); AbrirCarpetaDelCliente(); }, VarianteBoton.Fantasma);
            dentro.gameObject.SetActive(false);
            var giro = t.gameObject.AddComponent<GiroDeCarta>();
            giro.Frente = frente.gameObject;
            giro.Reverso = dentro.gameObject;
            var abierto = false;
            var viva = t.gameObject.AddComponent<PiezaViva>();
            viva.Escala = 1.03f;
            viva.AlPulsar = () => {
                if (abierto) return;
                abierto = true;
                giro.Girar();
                alAbrir?.Invoke();
            };
        }

        private void PintarMensajesDelCliente(RectTransform hoja, List<Nexus.Core.Proyecto.PapelDelCliente> papeles) {
            if (papeles.Count == 0) {
                Ui.Texto(hoja, "El cliente no ha escrito todavía.", EstiloTexto.Cuerpo, Tema.inkMuted);
                return;
            }
            foreach (var p in papeles.OrderByDescending(x => x.Dia)) {
                var nuevo = !S.R.PapelesLeidos.Contains(p.Id);
                var t = Ui.Tarjeta(hoja, $"Día {p.Dia} · {p.De}", nuevo ? Tono.Cyan : Tono.Neutro);
                Ui.Texto(t, p.Titulo, EstiloTexto.Encabezado, nuevo ? Tema.ink : Tema.inkMuted);
                t.gameObject.AddComponent<PiezaViva>().AlPulsar = AbrirCarpetaDelCliente;
            }
            foreach (var v in S.VocesDelProducto().Take(4))
                Ui.Texto(hoja, $"«{v.Frase}» — {v.Quien}", EstiloTexto.Pequeno, Tema.violet);
        }

        // ==================================================================== cada fotograma

        /// <summary>Lo que el escenario hace en cada fotograma: momento, escena de zona, avisos, bandeja y contador.</summary>
        private void ActualizarEscenario(Paso paso) {
            var dt = Time.unscaledDeltaTime;

            // La carta del momento entra con un pequeño salto y se va igual.
            var quiero = EsMomento(paso) ? 1f : 0f;
            if (quiero > 0 && !_capaMomento.gameObject.activeSelf) { _capaMomento.gameObject.SetActive(true); _capaMomento.SetAsLastSibling(); }
            _momento01 = Mathf.MoveTowards(_momento01, quiero, dt / 0.25f);
            var e = 1 - Mathf.Pow(1 - _momento01, 3);
            _grupoMomento.alpha = e;
            _tarjetaMomento.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, e);
            _grupoMomento.blocksRaycasts = quiero > 0;
            if (_momento01 <= 0 && _capaMomento.gameObject.activeSelf) _capaMomento.gameObject.SetActive(false);

            // Escritorio: carrusel. Otra zona: su escena viva.
            var zona = ZonaDondeEstoy();
            var fuera = zona != null && !zona.EsAncla;
            if (_carrusel.gameObject.activeSelf == fuera) _carrusel.gameObject.SetActive(!fuera);
            var idZona = fuera ? ClaveDeEscenaDeZona(zona) : null;
            if (idZona != _zonaPintada) {
                _zonaPintada = idZona;
                UiKit.Vaciar(_contenedorZona);
                if (fuera) ConstruirEscenaDeZona(zona, _contenedorZona);
            }

            ActualizarTiras(paso);

            // La bandeja M entra desde la izquierda.
            _bandeja01 = Mathf.MoveTowards(_bandeja01, _bandejaAbierta ? 1 : 0, dt / 0.3f);
            var b = 1 - Mathf.Pow(1 - _bandeja01, 3);
            _bandeja.anchoredPosition = new Vector2(Mathf.Lerp(-640, 24, b), (AltoPie - AltoHud) / 2f);
            _grupoBandeja.alpha = b;
            if (_bandeja01 <= 0 && _bandeja.gameObject.activeSelf) _bandeja.gameObject.SetActive(false);
            if (_bandejaAbierta) PintarBandeja();   // Escape la cierra: ver AlPulsarEscape

            // El contador de la M = sobres sin abrir: avisos que sonaron, papeles del cliente sin leer y novedades.
            // Al pasar el raton se ve de que es cada parte.
            var avisos = AlertasSonando().Count;
            var delCliente = S.Perfil.Proyecto != null && S.Perfil.Proyecto.Papeles.Count > 0 ? S.PapelesSinLeer() : 0;
            var novedades = S.R.BitacoraSinVer;
            var hay = avisos + delCliente + novedades;
            if (_globoM.gameObject.activeSelf != hay > 0) _globoM.gameObject.SetActive(hay > 0);
            if (hay > 0) {
                _textoM.text = hay > 99 ? "99" : hay.ToString();
                _globoM.localScale = Vector3.one * (1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
            }
            if (_desgloseM != null)
                _desgloseM.text = hay == 0 ? "Nada nuevo" :
                    string.Join(" · ", new[] {
                        avisos > 0 ? $"{avisos} aviso{(avisos == 1 ? "" : "s")}" : null,
                        delCliente > 0 ? $"{delCliente} del cliente" : null,
                        novedades > 0 ? $"{novedades} novedad{(novedades == 1 ? "" : "es")}" : null
                    }.Where(x => x != null)) + " · clic para abrir";

            // El HUD: dia, unidad y donde estas, en una linea.
            var brief = S.BriefDeHoy;
            _subtituloHud.text = (S.R.DiaActual == 0 ? "Antes del día 1" : $"Día {S.R.DiaActual} de {S.Perfil.DiasTotales}" +
                                  (brief != null ? " · " + brief.EtiquetaUnidad : "")) + (zona != null ? " · " + zona.Nombre : "");
            _pausa.GetComponentInChildren<TMP_Text>().text = Runner.Pausado ? "Seguir" : "Pausa";
            if (_textoVelocidad != null) _textoVelocidad.text = RotuloDeVelocidad();
        }

        /// <summary>Los avisos que sonaron, como tiras arriba: canal, una linea, cuenta atras y «Atender».</summary>
        private void ActualizarTiras(Paso paso) {
            var alertas = Runner.Estado == EstadoDelDia.Corriendo ? AlertasSonando() : new List<Alerta>();
            var momento = EsMomento(paso);
            var prorroga = Runner.Estado == EstadoDelDia.Prorroga;
            var clave = string.Join(",", alertas.Select(a => a.Id)) + "|" + S.ZonaActual + "|" + momento + "|" + prorroga;
            if (clave != _claveTiras) {
                _claveTiras = clave;
                UiKit.Vaciar(_tiras);
                _cuentasDeTiras.Clear();
                if (prorroga && !momento) {
                    var tira = Ui.PanelColumna(_tiras, "Horas extra", Tema.Espacio(3), 4, NexusTheme.Alfa(Tema.bg950, 0.94f));
                    Ui.Borde(tira, Tema.violet, NexusTheme.RadioMd, 2);
                    var fila = Ui.Fila(tira, "Fila", Tema.Espacio(3));
                    var textos = Ui.Columna(fila, "Textos", 2);
                    UiKit.Tamano(textos, flexAncho: 1);
                    Ui.Texto(textos, "HORAS EXTRA", EstiloTexto.Leyenda, Tema.violet);
                    // Sin «Irme ya» (2026-11-07): las horas extra acaban solas a la hora limite; la velocidad las acorta.
                    Ui.Texto(textos, $"Ya no llegan avisos: recorre la oficina o habla con quien quede. El día acaba solo a las {S.Perfil.Jornada.HoraLimite:00}:00.",
                             EstiloTexto.Pequeno, Tema.ink);
                    tira.gameObject.AddComponent<EntradaDeTira>();
                }
                var ancla = Ancla();
                var enElEscritorio = ancla == null || S.ZonaActual == ancla.Id;
                if (!momento) {
                    foreach (var alerta in alertas.Take(3)) {
                        var a = alerta;
                        var tira = Ui.PanelColumna(_tiras, "Aviso", Tema.Espacio(3), 4, NexusTheme.Alfa(Tema.bg950, 0.94f));
                        Ui.Borde(tira, Tema.warning, NexusTheme.RadioMd, 2);
                        var fila = Ui.Fila(tira, "Fila", Tema.Espacio(3));
                        var textos = Ui.Columna(fila, "Textos", 2);
                        UiKit.Tamano(textos, flexAncho: 1);
                        Ui.Texto(textos, (a.Tipo == TiposDeAlerta.Minijuego ? "TICKET · " : "") + a.Canal.ToUpperInvariant(), EstiloTexto.Leyenda, Tema.warning);
                        var linea = Ui.Texto(textos, a.Texto, EstiloTexto.Pequeno, Tema.ink);
                        linea.maxVisibleLines = 2;
                        linea.overflowMode = TextOverflowModes.Ellipsis;
                        var cuenta = Ui.Texto(textos, "", EstiloTexto.Leyenda, Tema.inkMuted);
                        _cuentasDeTiras.Add(new KeyValuePair<Alerta, TMP_Text>(a, cuenta));
                        var texto = enElEscritorio ? "Atender" : $"Volver y atender ({S.Perfil.Mapa.CosteDeVisitar(S.ZonaActual, ancla.Id)} min)";
                        Ui.Boton(fila, texto, () => Atender(a), VarianteBoton.Primario);
                        tira.gameObject.AddComponent<EntradaDeTira>();
                    }
                }
            }
            foreach (var kv in _cuentasDeTiras)
                kv.Value.text = kv.Key.EstaPendiente ? $"caduca en {kv.Key.MinutosParaExpirar(S.MinutoDelDia)} min" : "";
        }

        /// <summary>La tira de un aviso baja desde arriba al aparecer.</summary>
        private sealed class EntradaDeTira : MonoBehaviour {
            private float _t;
            private CanvasGroup _grupo;
            private void Update() {
                if (_t >= 1) return;
                if (_grupo == null) _grupo = gameObject.AddComponent<CanvasGroup>();
                _t = Mathf.Min(1, _t + Time.unscaledDeltaTime / 0.3f);
                var e = 1 - Mathf.Pow(1 - _t, 3);
                transform.localScale = new Vector3(1, Mathf.Lerp(0.6f, 1f, e), 1);
                _grupo.alpha = e;
            }
        }
    }
}
