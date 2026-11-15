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
    ///   HUD fijo        arriba: «Mapa» y «Backlog» a la izquierda, el dia del proyecto en grande y el reloj en el
    ///                   centro, «Menú» a la derecha (pausar es abrir el menú: no hay un boton «Pausa» aparte);
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
        private TMP_Text _textoM, _subtituloHud, _desgloseM, _textoDia, _textoDiaDe;
        private bool _historialAbierto;
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
            ConstruirDiaGrande(fila);
            GuiaView.Registrar("dia.reloj", RelojView.CrearCompacto(Ui, fila, Runner));
            var controles = Ui.Fila(fila, "Controles", 6, alineacion: TextAnchor.MiddleCenter);
            controles.gameObject.AddComponent<ListaDeElementos>();
            var velocidad = Ui.Boton(controles, RotuloDeVelocidad(), () => {
                Runner.Velocidad = Runner.Velocidad < 1.5 ? 2 : Runner.Velocidad < 3 ? 4 : 1;
                _textoVelocidad.text = RotuloDeVelocidad();
            }, VarianteBoton.Fantasma);
            UiKit.Tamano(velocidad, ancho: 84);
            _textoVelocidad = velocidad.GetComponentInChildren<TMP_Text>();
            GuiaView.Registrar("dia.velocidad", velocidad);
            _subtituloHud = Ui.Texto(centro, "", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);
            Ui.Resorte(hud);

            // Solo «Menú»: abrirlo ya para el reloj (2026-11-14: el boton «Pausa» sobraba).
            var derecha = Ui.Fila(hud, "Derecha", Tema.Espacio(2));
            var menu = Ui.Boton(derecha, "Menú", () => App.Router.Apilar<PantallaDePausa>());
            UiKit.Tamano(menu, ancho: 130);
            GuiaView.Registrar("dia.diario", menu);   // el diario vive en «Menú» (la pausa)
        }

        /// <summary>
        /// El dia del proyecto, tan grande como la hora: «DÍA 3» con «de 10» debajo, y una raya que lo separa del reloj.
        /// Asi se ve de un vistazo cuanto queda de proyecto, no solo de jornada.
        /// </summary>
        private void ConstruirDiaGrande(RectTransform fila) {
            var bloque = Ui.Columna(fila, "Dia", 0, 0, TextAnchor.MiddleCenter);
            _textoDia = Ui.Texto(bloque, "", EstiloTexto.Titulo, Tema.cyan, TextAlignmentOptions.Center);
            QuitarAltoMinimo(_textoDia);
            _textoDia.font = Tema.FuenteDisplay;
            _textoDia.fontSize = 40;
            _textoDia.characterSpacing = 4;
            _textoDiaDe = Ui.Texto(bloque, "", EstiloTexto.Etiqueta, Tema.inkMuted, TextAlignmentOptions.Center);
            QuitarAltoMinimo(_textoDiaDe);
            _textoDiaDe.characterSpacing = 2;
            var raya = Ui.Nodo(fila, "Raya");
            raya.gameObject.AddComponent<Image>().color = NexusTheme.Alfa(Tema.cyan, 0.35f);
            UiKit.Tamano(raya, ancho: 2, alto: 54);
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
            var titulo = Ui.Texto(cabecera, "MENSAJES", EstiloTexto.Titulo, Tema.cyan);
            titulo.font = Tema.FuenteDisplay;
            titulo.characterSpacing = 4;
            UiKit.Tamano(titulo, flexAncho: 1);
            Ui.Boton(cabecera, "Cerrar (Esc)", AlternarBandeja, VarianteBoton.Salida);
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
        /// El buzon (rediseñado 2026-11-14): tarjetas cortas y con cara, no sobres llenos de texto.
        ///   · los avisos que suenan, arriba, en rojo y con «Atender»;
        ///   · lo del cliente: su cara, el titulo en una linea, lo que te dice en dos, y «Ver en la pizarra» bien grande;
        ///   · lo que paso sin ti: una linea por novedad (clic = vista) y «Marcar todo como visto»;
        ///   · lo que ha pasado (la bitacora), plegado: se abre si se quiere.
        /// Al abrir la bandeja las tarjetas entran escalonadas. El numero de la «M» baja con cada cosa vista.
        /// </summary>
        private void PintarBandeja() {
            var avisos = AlertasSonando();
            var papeles = S.Perfil.Proyecto != null ? S.PapelesDelCliente().Where(x => !S.R.PapelesLeidos.Contains(x.Id)).ToList()
                                                    : new List<Nexus.Core.Proyecto.PapelDelCliente>();
            var novedades = S.R.Novedades.ToList();
            var clave = string.Join(",", avisos.Select(a => a.Id)) + "|" + string.Join(",", papeles.Select(x => x.Id)) + "|" +
                        novedades.Count + "|" + _historialAbierto;
            if (clave == _claveBandeja) return;
            var animar = _claveBandeja == null;
            _claveBandeja = clave;
            UiKit.Vaciar(_cuerpoBandeja);
            var orden = 0;

            foreach (var a in avisos) {
                var alerta = a;
                var t = TarjetaDeBuzon(Tema.danger, animar, orden++);
                var fila = Ui.Fila(t, "Fila", Tema.Espacio(3), 0, TextAnchor.MiddleLeft);
                UiKit.Tamano(fila, flexAncho: 1);
                var textos = Ui.Columna(fila, "Textos", 2);
                UiKit.Tamano(textos, flexAncho: 1);
                Ui.Texto(textos, "AVISO · " + a.Canal.ToUpperInvariant(), EstiloTexto.Leyenda, Tema.danger);
                Recortar(Ui.Texto(textos, a.Texto, EstiloTexto.Cuerpo, Tema.ink), 2);
                var atender = Ui.Boton(fila, "Atender", () => { AlternarBandeja(); Atender(alerta); }, VarianteBoton.Primario);
                UiKit.Tamano(atender, ancho: 150, alto: 52);
            }

            foreach (var p in papeles.OrderByDescending(x => x.Dia)) {
                var papel = p;
                Action abrir = () => {
                    if (!S.R.PapelesLeidos.Contains(papel.Id)) S.R.PapelesLeidos.Add(papel.Id);
                    AlternarBandeja();
                    AbrirCarpetaDelCliente();
                };
                var t = TarjetaDeBuzon(Tema.cyan, animar, orden++);
                var fila = Ui.Fila(t, "Fila", Tema.Espacio(3), 0, TextAnchor.UpperLeft);
                UiKit.Tamano(fila, flexAncho: 1);
                if (!string.IsNullOrEmpty(p.De)) RostroDePersonaje.Crear(Ui, fila, MaterialesNexus.IdDePersonaje(p.De), p.De, 72, Tema.cyan);
                var textos = Ui.Columna(fila, "Textos", 4);
                UiKit.Tamano(textos, flexAncho: 1);
                var cima = Ui.Fila(textos, "Cima", Tema.Espacio(2), 0, TextAnchor.MiddleLeft);
                Ui.Chip(cima, "NUEVO", NexusTheme.Alfa(Tema.cyan, 0.2f), Tema.cyan);
                Ui.Texto(cima, "Día " + p.Dia + " · " + p.De, EstiloTexto.Leyenda, Tema.inkMuted);
                Recortar(Ui.Texto(textos, p.Titulo, EstiloTexto.Encabezado, Tema.ink), 1);
                if (!string.IsNullOrEmpty(p.QueTeDice)) Recortar(Ui.Texto(textos, p.QueTeDice, EstiloTexto.Pequeno, Tema.inkMuted), 2);
                var ver = Ui.Boton(t, "Ver en la pizarra  ►", abrir, VarianteBoton.Primario);
                UiKit.Tamano(ver, flexAncho: 1, alto: 56);
                var viva = t.gameObject.AddComponent<PiezaViva>();
                viva.Escala = 1.02f;
                viva.AlPulsar = abrir;
            }

            if (novedades.Count > 0) {
                var cabecera = Ui.Fila(_cuerpoBandeja, "Novedades", Tema.Espacio(2), 0, TextAnchor.MiddleLeft);
                UiKit.Tamano(Ui.Texto(cabecera, "PASÓ SIN TI · " + novedades.Count, EstiloTexto.Leyenda, Tema.inkMuted), flexAncho: 1);
                Ui.Boton(cabecera, "Marcar todo como visto", () => {
                    foreach (var l in S.R.Novedades.ToList()) S.R.VerNovedad(l);
                }, VarianteBoton.Fantasma);
                foreach (var linea in novedades.Take(5)) {
                    var l = linea;
                    var t = TarjetaDeBuzon(Tema.inkMuted, animar, orden++, true);
                    var fila = Ui.Fila(t, "Fila", Tema.Espacio(2), 0, TextAnchor.MiddleLeft);
                    UiKit.Tamano(fila, flexAncho: 1);
                    var punto = Ui.Nodo(fila, "Punto");
                    var imagen = punto.gameObject.AddComponent<Image>();
                    imagen.sprite = Ui.SpriteCircular();
                    imagen.color = Tema.warning;
                    UiKit.Tamano(punto, ancho: 10, alto: 10);
                    UiKit.Tamano(Recortar(Ui.Texto(fila, l, EstiloTexto.Pequeno, Tema.ink), 2), flexAncho: 1);
                    var viva = t.gameObject.AddComponent<PiezaViva>();
                    viva.Escala = 1.02f;
                    viva.AlPulsar = () => S.R.VerNovedad(l);
                }
                if (novedades.Count > 5) Ui.Texto(_cuerpoBandeja, "…y " + (novedades.Count - 5) + " más.", EstiloTexto.Leyenda, Tema.inkMuted);
            }

            if (avisos.Count == 0 && papeles.Count == 0 && novedades.Count == 0) {
                var vacio = Ui.Columna(_cuerpoBandeja, "Vacio", Tema.Espacio(2), Tema.Espacio(6), TextAnchor.MiddleCenter);
                var check = Ui.Nodo(vacio, "Check");
                var fondo = check.gameObject.AddComponent<Image>();
                fondo.sprite = Ui.SpriteCircular();
                fondo.color = NexusTheme.Alfa(Tema.success, 0.18f);
                UiKit.Tamano(check, ancho: 84, alto: 84);
                var marca = Ui.Texto(check, "✓", EstiloTexto.Titulo, Tema.success, TextAlignmentOptions.Center);
                QuitarAltoMinimo(marca);
                UiKit.Rellenar(marca.rectTransform);
                Ui.Texto(vacio, "Todo al día", EstiloTexto.Subtitulo, Tema.ink, TextAlignmentOptions.Center);
                Ui.Texto(vacio, "Cuando llegue algo, la «M» se enciende.", EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Center);
                if (animar) vacio.gameObject.AddComponent<EntradaDePieza>();
            }

            // La bitacora entera, plegada: el que quiera releerla la abre.
            if (S.R.Bitacora.Count > 0) {
                Ui.Separador(_cuerpoBandeja);
                Ui.Boton(_cuerpoBandeja, (_historialAbierto ? "▼  " : "►  ") + "Lo que ha pasado (" + Math.Min(12, S.R.Bitacora.Count) + ")",
                         () => _historialAbierto = !_historialAbierto, VarianteBoton.Fantasma);
                if (_historialAbierto)
                    foreach (var linea in S.R.Bitacora.Take(12)) Recortar(Ui.Texto(_cuerpoBandeja, "· " + linea, EstiloTexto.Pequeno, Tema.inkMuted), 2);
            }
        }

        /// <summary>Una tarjeta del buzon: cristal oscuro, una franja de color a la izquierda y entrada escalonada.</summary>
        private RectTransform TarjetaDeBuzon(Color acento, bool animar, int orden, bool compacta = false) {
            var t = Ui.PanelColumna(_cuerpoBandeja, "Mensaje", compacta ? Tema.Espacio(2) : Tema.Espacio(4), Tema.Espacio(2), Tema.surfaceRaised);
            Ui.Borde(t, NexusTheme.Alfa(acento, compacta ? 0.35f : 0.8f), NexusTheme.RadioMd, compacta ? 1 : 2);
            var franja = Ui.Nodo(t, "Franja");
            franja.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            franja.anchorMin = new Vector2(0, 0);
            franja.anchorMax = new Vector2(0, 1);
            franja.pivot = new Vector2(0, 0.5f);
            franja.sizeDelta = new Vector2(compacta ? 3 : 5, -12);
            franja.anchoredPosition = new Vector2(4, 0);
            var color = franja.gameObject.AddComponent<Image>();
            color.color = acento;
            color.raycastTarget = false;
            if (animar) t.gameObject.AddComponent<EntradaDePieza>().Retraso = Math.Min(8, orden) * 0.05f;
            return t;
        }

        /// <summary>Como mucho 'lineas' renglones, con puntos suspensivos: el buzon se lee de un vistazo.</summary>
        private static TMP_Text Recortar(TMP_Text t, int lineas) {
            t.maxVisibleLines = lineas;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
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

            // El HUD: el dia en grande; debajo, la unidad (sprint, etapa…) y donde estas.
            var brief = S.BriefDeHoy;
            _textoDia.text = S.R.DiaActual == 0 ? "DÍA 0" : "DÍA " + S.R.DiaActual;
            _textoDiaDe.text = S.R.DiaActual == 0 ? "antes de empezar" : "DE " + S.Perfil.DiasTotales + (S.R.DiaActual >= S.Perfil.DiasTotales ? " · EL ÚLTIMO" : "");
            _textoDia.color = S.R.DiaActual >= S.Perfil.DiasTotales ? Tema.warning : Tema.cyan;
            _subtituloHud.text = string.Join(" · ", new[] { brief != null ? brief.EtiquetaUnidad : null, zona != null ? zona.Nombre : null }.Where(x => !string.IsNullOrEmpty(x)));
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
