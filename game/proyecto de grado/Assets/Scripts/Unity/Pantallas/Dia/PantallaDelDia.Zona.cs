using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Core.Jornada;
using Nexus.Core.Sesion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Fuera del escritorio, la Fase 2 enseña la ZONA (2026-11-06): su sala de la ilustracion del nivel a pantalla
    /// completa, con la gente que esta ahi (un bocadillo: «¿Tienes un minuto?» → hablar), los destellos de los
    /// coleccionables (se atrapan como en el mapa) y, en algunas zonas, su minijuego del dia:
    ///   cafeteria  llenar la taza hasta la linea      data-hub  cazar la linea roja de los logs
    ///   bullpen    escuchar rumores en las mesas      pasillo   voltear las notas del tablon
    /// </summary>
    public sealed partial class PantallaDelDia {
        private string _avisoDeZona;
        private float _avisoDeZonaHasta;

        /// <summary>Cuando cambia, la escena de la zona se rehace (alguien ya hablo, un destello atrapado, el minijuego hecho).</summary>
        private string ClaveDeEscenaDeZona(ZonaDeNivel zona) {
            return zona.Id + "|" + S.ConversacionesAqui().Count + "|" + S.ColeccionablesAqui().Count + "|" + S.MinijuegoDeZonaHechoHoy(zona.Id) +
                   "|" + Runner.PuedeMoverse + "|" + (_avisoDeZonaHasta > Time.unscaledTime);
        }

        private void ConstruirEscenaDeZona(ZonaDeNivel zona, RectTransform padre) {
            // ---- la sala, a sangre, con un vaiven lento de camara
            var marco = Ui.Nodo(padre, "Sala");
            UiKit.Rellenar(marco);
            marco.gameObject.AddComponent<RectMask2D>();
            var textura = TexturaDelPlano();
            var fondo = Ui.Nodo(marco, "Fondo");
            UiKit.Rellenar(fondo);
            if (textura != null && zona.TieneArea) {
                var img = fondo.gameObject.AddComponent<RawImage>();
                img.texture = textura;
                img.uvRect = new Rect(zona.Area[0], 1 - (zona.Area[1] + zona.Area[3]), zona.Area[2], zona.Area[3]);
                img.raycastTarget = false;
                fondo.gameObject.AddComponent<VaivenDeCamara>();
            } else {
                var img = fondo.gameObject.AddComponent<Image>();
                img.color = Tema.bg900;
            }
            var velo = Ui.Nodo(marco, "Velo");
            UiKit.Rellenar(velo);
            var imgVelo = velo.gameObject.AddComponent<Image>();
            imgVelo.color = NexusTheme.Alfa(Tema.bg950, 0.25f);
            imgVelo.raycastTarget = false;

            // ---- el rotulo de la sala y la vuelta al escritorio
            var rotulo = Ui.PanelColumna(marco, "Rotulo", Tema.Espacio(4), 2, NexusTheme.Alfa(Tema.bg950, 0.8f));
            rotulo.anchorMin = rotulo.anchorMax = rotulo.pivot = new Vector2(0, 1);
            rotulo.anchoredPosition = new Vector2(16, -16);
            var ajuste = rotulo.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.horizontalFit = ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            QuitarAltoMinimo(Ui.Texto(rotulo, zona.Nombre, EstiloTexto.Titulo, Tema.ink));
            if (!string.IsNullOrEmpty(zona.QueDa)) QuitarAltoMinimo(Ui.Texto(rotulo, zona.QueDa, EstiloTexto.Pequeno, Tema.cyan));

            var ancla = Ancla();
            if (ancla != null) {
                var volver = Ui.Boton(marco, $"Volver al escritorio · {S.Perfil.Mapa.CosteDeVisitar(S.ZonaActual, ancla.Id)} min", () => IrA(ancla.Id));
                var rv = (RectTransform)volver.transform;
                rv.anchorMin = rv.anchorMax = rv.pivot = new Vector2(1, 1);
                rv.sizeDelta = new Vector2(360, 56);
                rv.anchoredPosition = new Vector2(-16, -16);
                volver.interactable = Runner.PuedeMoverse;
            }

            // ---- la gente: retratos con bocadillo, repartidos abajo
            var gente = S.ConversacionesAqui();
            for (var i = 0; i < gente.Count && i < 4; i++) {
                var cv = gente[i];
                var p = App.Catalogo.Relaciones.PersonajePorId(cv.Personaje);
                var nombre = p != null ? p.Nombre : cv.Personaje;
                var persona = Ui.Columna(marco, "Persona " + nombre, 4, 0, TextAnchor.LowerCenter);
                persona.anchorMin = persona.anchorMax = persona.pivot = new Vector2((i + 1f) / (Mathf.Min(gente.Count, 4) + 1f), 0);
                persona.anchoredPosition = new Vector2(0, 16);
                persona.sizeDelta = new Vector2(220, 330);
                Ui.Ilustracion(persona, Nexus.Unity.Tema.MaterialesNexus.IdDePersonaje(nombre), null, ancho: 170, alto: 200);
                // El bocadillo, fuera del layout y DELANTE del retrato (ultimo hijo), justo encima de la cabeza.
                var globo = Ui.PanelColumna(persona, "Bocadillo", Tema.Espacio(3), 0, Color.white);
                globo.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                globo.anchorMin = globo.anchorMax = new Vector2(0.5f, 1);
                globo.pivot = new Vector2(0.5f, 0);
                globo.anchoredPosition = new Vector2(0, 6);
                globo.sizeDelta = new Vector2(250, 0);
                globo.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Ui.Borde(globo, PlanoIlustrado.ColorDelDestello, NexusTheme.RadioMd, 2);
                Ui.Texto(globo, cv.Pregunta == null ? "¿Tienes un minuto?" : "¡Te quiero preguntar algo!", EstiloTexto.Pequeno,
                         new Color32(0x26, 0x26, 0x2A, 0xFF), TextAlignmentOptions.Center);
                globo.gameObject.AddComponent<Flotar>();
                globo.SetAsLastSibling();
                var placa = Ui.Texto(persona, $"{nombre} · {cv.Minutos} min", EstiloTexto.Leyenda, Tema.ink, TextAlignmentOptions.Center);
                QuitarAltoMinimo(placa);
                var viva = persona.gameObject.AddComponent<PiezaViva>();
                viva.Escala = 1.08f;
                var conversacion = cv;
                viva.AlPulsar = () => { if (Runner.PuedeMoverse) Hablar(conversacion); };
                var hit = persona.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
            }

            // ---- los destellos de lo que hay aqui
            var rnd = new System.Random(zona.Id.GetHashCode() + S.R.DiaActual);
            foreach (var id in S.ColeccionablesAqui()) {
                var col = App.Catalogo.Coleccionables.FirstOrDefault(x => x.Id == id);
                var dificultad = col != null && col.Aparicion != null ? col.Aparicion.Dificultad : null;
                var c = id;
                DestelloLibre.Crear(Ui, marco, dificultad, rnd, () => Runner.PuedeMoverse, () => Recoger(c));
            }

            // ---- el minijuego de la zona: un punto de luz grande con su nombre
            var tipo = S.MinijuegoDeZonaAqui();
            if (tipo != null) {
                var hecho = S.MinijuegoDeZonaHechoHoy(zona.Id);
                var punto = Ui.PanelColumna(marco, "Minijuego", Tema.Espacio(4), 2, NexusTheme.Alfa(Tema.bg950, 0.85f));
                punto.anchorMin = punto.anchorMax = punto.pivot = new Vector2(0.5f, 1);
                punto.anchoredPosition = new Vector2(0, -24);
                var a2 = punto.gameObject.AddComponent<ContentSizeFitter>();
                a2.horizontalFit = a2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Ui.Borde(punto, hecho ? Tema.lineStrong : PlanoIlustrado.ColorDelDestello, NexusTheme.RadioLg, 2);
                if (!hecho) Ui.Halo(punto, NexusTheme.Alfa(PlanoIlustrado.ColorDelDestello, 0.6f), NexusTheme.RadioLg);
                QuitarAltoMinimo(Ui.Texto(punto, MinijuegosDeZona.Nombre(tipo), EstiloTexto.Subtitulo, hecho ? Tema.inkMuted : PlanoIlustrado.ColorDelDestello,
                                          TextAlignmentOptions.Center));
                var minutos = MinijuegosDeZona.Minutos(tipo);
                QuitarAltoMinimo(Ui.Texto(punto, hecho ? "Hecho hoy" : minutos > 0 ? $"{minutos} min · una vez al día" : "gratis · una vez al día",
                                          EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center));
                if (!hecho) {
                    var viva = punto.gameObject.AddComponent<PiezaViva>();
                    viva.Escala = 1.08f;
                    viva.AlPulsar = () => AbrirMinijuegoDeZona(tipo);
                    punto.gameObject.AddComponent<Flotar>();
                }
            }

            // ---- lo que acaba de pasar (resultado del minijuego), unos segundos
            if (!string.IsNullOrEmpty(_avisoDeZona) && _avisoDeZonaHasta > Time.unscaledTime) {
                var aviso = Ui.PanelColumna(marco, "Resultado", Tema.Espacio(4), 2, NexusTheme.Alfa(Tema.bg950, 0.9f));
                aviso.anchorMin = aviso.anchorMax = aviso.pivot = new Vector2(0.5f, 0.5f);
                aviso.sizeDelta = new Vector2(720, 0);
                aviso.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Ui.Borde(aviso, Tema.cyan, NexusTheme.RadioLg, 2);
                Ui.Texto(aviso, _avisoDeZona, EstiloTexto.Cuerpo, Tema.ink, TextAlignmentOptions.Center);
            }
        }

        // ==================================================================== minijuegos de zona

        private void AbrirMinijuegoDeZona(string tipo) {
            var motivo = S.PorQueNoSeJuegaElMinijuegoDeZona();
            if (motivo != null) { MostrarAvisoDeZona(motivo); return; }
            // La capa va sobre el escenario (no dentro de la escena), para que rehacer la escena no la borre.
            var capa = Ui.Nodo(_escenario, "Minijuego " + tipo);
            UiKit.Rellenar(capa);
            var velo = capa.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(Tema.bg950, 0.7f);
            Runner.Mirando = true;   // mientras se juega, el reloj espera; luego cobra sus minutos de golpe
            Action<bool> terminar = exito => {
                Destroy(capa.gameObject);
                if (_panel == null && !_bandejaAbierta) Runner.Mirando = false;
                JugarMinijuegoDeZona(exito);
            };
            switch (tipo) {
                case TiposDeMinijuegoDeZona.Cafe: TazaDeCafe.Crear(Ui, capa, terminar); break;
                case TiposDeMinijuegoDeZona.Logs: CazaDeLogs.Crear(Ui, capa, S.R.DiaActual * 31 + S.MinutoDelDia, terminar); break;
                default: {
                    // Rumores y tablon: se juega primero (lo que se descubre sale de la sesion) y luego se enseña.
                    var r = JugarMinijuegoDeZona(true, false);
                    if (r == null) { Destroy(capa.gameObject); if (_panel == null && !_bandejaAbierta) Runner.Mirando = false; return; }
                    Action cerrar = () => { Destroy(capa.gameObject); if (_panel == null && !_bandejaAbierta) Runner.Mirando = false; };
                    if (tipo == TiposDeMinijuegoDeZona.Rumores) MesasDeRumores.Crear(Ui, capa, r.Revelaciones, cerrar);
                    else TablonDeNotas.Crear(Ui, capa, r.Revelaciones, cerrar);
                    break;
                }
            }
        }

        /// <summary>Juega el minijuego en la sesion (cobra minutos, aplica el efecto) y dice lo que paso.</summary>
        private ResultadoDeMinijuegoDeZona JugarMinijuegoDeZona(bool exito, bool avisar = true) {
            ResultadoDeMinijuegoDeZona r = null;
            try {
                if (!Runner.Actuar(() => { ResultadoDeMinijuegoDeZona x; var a = S.JugarMinijuegoDeZona(exito, out x); r = x; return a; })) {
                    MostrarAvisoDeZona("Ahora no se puede: el reloj está parado o hay algo abierto.");
                    return null;
                }
            } catch (InvalidOperationException e) {
                MostrarAvisoDeZona(e.Message);
                return null;
            }
            var cambios = r.Cambios.Count > 0 ? " " + Textos.Previsualizar(r.Cambios) + "." : "";
            var texto = r.Tipo == TiposDeMinijuegoDeZona.Cafe ? (r.Exito ? "¡Café perfecto! El equipo respira." : "El café se quedó a medias.") + cambios
                      : r.Tipo == TiposDeMinijuegoDeZona.Logs ? (r.Exito ? "¡Cazado! Ese error ya no llegará al cliente." : "Se escapó entre las líneas.") + cambios
                      : string.Join("  ", r.Revelaciones);
            Anotar($"{S.HoraActual} · {MinijuegosDeZona.Nombre(r.Tipo)}: {texto}");
            if (avisar) MostrarAvisoDeZona(texto);
            return r;
        }

        private void MostrarAvisoDeZona(string texto) {
            _avisoDeZona = texto;
            _avisoDeZonaHasta = Time.unscaledTime + 4f;
            _zonaPintada = null;   // se rehace la escena con el aviso
        }

        // ---- la taza

        /// <summary>Mantener pulsado para servir; soltar con la taza en la linea. Se juega una vez.</summary>
        private sealed class TazaDeCafe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
            private RectTransform _liquido;
            private TMP_Text _texto;
            private float _segundos, _alto;
            private bool _sirviendo, _hecho;
            private Action<bool> _alTerminar;

            public static void Crear(UiKit ui, RectTransform capa, Action<bool> alTerminar) {
                var tema = ui.Tema;
                var panel = ui.PanelColumna(capa, "Cafe", tema.margen, tema.espacio, tema.surface);
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
                panel.sizeDelta = new Vector2(520, 560);
                ui.Texto(panel, "LLENAR LA TAZA", EstiloTexto.Leyenda, tema.cyan, TextAlignmentOptions.Center);
                var t = ui.Texto(panel, "Mantén pulsada la taza y suéltala en la línea.", EstiloTexto.Pequeno, tema.inkMuted, TextAlignmentOptions.Center);

                var taza = ui.Nodo(panel, "Taza");
                UiKit.Tamano(taza, 220, 300);
                var vaso = taza.gameObject.AddComponent<Image>();
                ui.Fondo(vaso, tema.bg950, 10);
                ui.Borde(taza, tema.ink, 10, 3);
                var liquido = ui.Nodo(taza, "Cafe");
                liquido.anchorMin = new Vector2(0, 0);
                liquido.anchorMax = new Vector2(1, 0);
                liquido.pivot = new Vector2(0.5f, 0);
                liquido.offsetMin = new Vector2(6, 6);
                liquido.offsetMax = new Vector2(-6, 6);
                var imgCafe = liquido.gameObject.AddComponent<Image>();
                imgCafe.color = new Color32(0x8B, 0x5A, 0x2B, 0xFF);
                imgCafe.raycastTarget = false;
                var linea = ui.Nodo(taza, "Linea");
                linea.anchorMin = new Vector2(-0.1f, MinijuegosDeZona.LineaDeLaTaza);
                linea.anchorMax = new Vector2(1.1f, MinijuegosDeZona.LineaDeLaTaza);
                linea.sizeDelta = new Vector2(0, 4);
                var imgLinea = linea.gameObject.AddComponent<Image>();
                imgLinea.color = tema.cyan;
                imgLinea.raycastTarget = false;

                var c = taza.gameObject.AddComponent<TazaDeCafe>();
                c._liquido = liquido;
                c._alto = 288;
                c._texto = t;
                c._alTerminar = alTerminar;
            }

            public void OnPointerDown(PointerEventData e) { if (!_hecho) _sirviendo = true; }

            public void OnPointerUp(PointerEventData e) {
                if (!_sirviendo || _hecho) return;
                _sirviendo = false;
                _hecho = true;
                var nivel = MinijuegosDeZona.NivelDeLaTaza(_segundos);
                var bien = MinijuegosDeZona.TazaBienLlena(nivel);
                _texto.text = bien ? "¡Justo en la línea!" : nivel < MinijuegosDeZona.LineaDeLaTaza ? "Corta…" : "¡Te pasaste!";
                StartCoroutine(Terminar(bien));
            }

            private System.Collections.IEnumerator Terminar(bool bien) {
                yield return new WaitForSecondsRealtime(0.9f);
                _alTerminar(bien);
            }

            private void Update() {
                if (_sirviendo) _segundos += Time.unscaledDeltaTime;
                var nivel = Mathf.Min(1f, MinijuegosDeZona.NivelDeLaTaza(_segundos));
                _liquido.sizeDelta = new Vector2(_liquido.sizeDelta.x, Mathf.Max(0, nivel * _alto - 6));
            }
        }

        // ---- los logs

        /// <summary>Las lineas de log suben; hay que hacer clic en la roja antes de que se vaya.</summary>
        private sealed class CazaDeLogs : MonoBehaviour {
            private static readonly string[] Normales = {
                "INFO  request ok 200 /api/pedidos", "DEBUG cache hit sesion=7f3a", "INFO  job sync terminado en 120ms",
                "INFO  usuario 104 inició sesión", "DEBUG pool conexiones 8/20", "INFO  health check ok", "WARN  latencia 410ms /buscar",
                "INFO  export csv listo", "DEBUG gc pausa 4ms", "INFO  cola vacía"
            };
            private RectTransform _lista;
            private float _t, _altoLinea = 34f;
            private bool _hecho;
            private Action<bool> _alTerminar;
            private TMP_Text _estado;

            public static void Crear(UiKit ui, RectTransform capa, int semilla, Action<bool> alTerminar) {
                var tema = ui.Tema;
                var panel = ui.PanelColumna(capa, "Logs", tema.margen, tema.espacio, tema.bg950);
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
                panel.sizeDelta = new Vector2(820, 560);
                ui.Borde(panel, tema.success, NexusTheme.RadioMd, 2);
                ui.Texto(panel, "CAZAR EL ERROR", EstiloTexto.Leyenda, tema.success, TextAlignmentOptions.Center);
                var estado = ui.Texto(panel, "Haz clic en la línea ERROR antes de que se vaya.", EstiloTexto.Pequeno, tema.inkMuted, TextAlignmentOptions.Center);
                var ventana = ui.Nodo(panel, "Ventana");
                UiKit.Tamano(ventana, flexAncho: 1, flexAlto: 1);
                ventana.gameObject.AddComponent<RectMask2D>();
                var lista = ui.Nodo(ventana, "Lineas");
                lista.anchorMin = new Vector2(0, 0);
                lista.anchorMax = new Vector2(1, 0);
                lista.pivot = new Vector2(0.5f, 1);
                // Exactamente el ancho de la ventana: con el sizeDelta por defecto sobresalia por los lados y la mascara
                // cortaba el principio de cada linea (se veia corrido a la izquierda).
                lista.sizeDelta = Vector2.zero;
                var fondoVentana = ventana.gameObject.AddComponent<Image>();
                fondoVentana.color = new Color32(0x05, 0x0A, 0x08, 0xFF);
                var c = ventana.gameObject.AddComponent<CazaDeLogs>();
                c._lista = lista;
                c._alTerminar = alTerminar;
                c._estado = estado;
                var roja = MinijuegosDeZona.IndiceDeLaRoja(semilla);
                var rnd = new System.Random(semilla);
                for (var i = 0; i < MinijuegosDeZona.LineasDeLog; i++) {
                    var esRoja = i == roja;
                    var fila = ui.Nodo(lista, "Linea");
                    fila.anchorMin = new Vector2(0, 1);
                    fila.anchorMax = new Vector2(1, 1);
                    fila.pivot = new Vector2(0.5f, 1);
                    fila.sizeDelta = new Vector2(0, c._altoLinea);
                    fila.anchoredPosition = new Vector2(0, -i * c._altoLinea);
                    var hit = fila.gameObject.AddComponent<Image>();
                    // Una linea si y otra no, un poco mas clara: se sigue mejor la lectura.
                    hit.color = i % 2 == 0 ? new Color(1, 1, 1, 0.03f) : new Color(0, 0, 0, 0);
                    var texto = esRoja ? "ERROR NullReference en Pago.Confirmar()" : Normales[rnd.Next(Normales.Length)];
                    var t = ui.Texto(fila, $"{8 + i / 6:00}:{(i * 7) % 60:00}:{(i * 13) % 60:00}  {texto}", EstiloTexto.Mono,
                                     esRoja ? tema.danger : tema.success, TextAlignmentOptions.MidlineLeft);
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                    t.overflowMode = TextOverflowModes.Ellipsis;
                    UiKit.Rellenar(t.rectTransform);
                    t.rectTransform.offsetMin = new Vector2(18, 0);
                    t.rectTransform.offsetMax = new Vector2(-18, 0);
                    var boton = fila.gameObject.AddComponent<Button>();
                    boton.transition = Selectable.Transition.None;
                    var r = esRoja;
                    boton.onClick.AddListener(() => c.Pulsar(r));
                }
            }

            private void Pulsar(bool roja) {
                if (_hecho) return;
                if (!roja) { _estado.text = "Esa no. ¡Busca la roja!"; return; }
                Acabar(true, "¡Cazado!");
            }

            private void Acabar(bool bien, string texto) {
                _hecho = true;
                _estado.text = texto;
                StartCoroutine(Terminar(bien));
            }

            private System.Collections.IEnumerator Terminar(bool bien) {
                yield return new WaitForSecondsRealtime(0.9f);
                _alTerminar(bien);
            }

            private void Update() {
                if (_hecho) return;
                _t += Time.unscaledDeltaTime;
                // Todas las lineas pasan de abajo a arriba en el tiempo de la caza.
                var alto = ((RectTransform)transform).rect.height;
                var recorrido = alto + MinijuegosDeZona.LineasDeLog * _altoLinea;
                _lista.anchoredPosition = new Vector2(0, -alto + recorrido * (_t / MinijuegosDeZona.SegundosDeLogs) + alto);
                if (_t >= MinijuegosDeZona.SegundosDeLogs) Acabar(false, "Se escapó…");
            }
        }

        // ---- los rumores

        /// <summary>
        /// Cinco mesas en la cafeteria (2026-11-06, rehecho: el texto apenas se leia). Cada mesa tiene su gente y unos
        /// puntos suspensivos que laten; al pasar el raton, el bocadillo salta y la frase se escribe letra a letra, en
        /// grande y sobre papel claro. Una de ellas dice lo que viene: brilla en dorado y se marca «PISTA».
        /// </summary>
        private static class MesasDeRumores {
            private static readonly string[] Charla = {
                "«…y entonces el build tardó cuarenta minutos…»", "«¿Alguien ha visto mi taza?»", "«El viernes hay pizza, dicen.»",
                "«Ese ticket lleva abierto desde marzo.»", "«No toques producción en viernes.»"
            };
            private static readonly Vector2[] Sitios = {
                new Vector2(0.16f, 0.56f), new Vector2(0.5f, 0.6f), new Vector2(0.84f, 0.56f), new Vector2(0.32f, 0.2f), new Vector2(0.68f, 0.2f)
            };

            public static void Crear(UiKit ui, RectTransform capa, List<string> revelaciones, Action cerrar) {
                var tema = ui.Tema;
                var sala = ui.Nodo(capa, "Cafeteria");
                // Ocupa toda la capa (con margen): la escena no tiene un alto fijo que no quepa entre el HUD y el pie.
                UiKit.Rellenar(sala, 28);
                ui.Fondo(sala.gameObject.AddComponent<Image>(), tema.bg900, NexusTheme.RadioLg);
                ui.Borde(sala, NexusTheme.Alfa(tema.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);
                sala.gameObject.AddComponent<EntradaDeTira>();

                var titulo = ui.Texto(sala, "ESCUCHAR RUMORES", EstiloTexto.Subtitulo, tema.cyan, TextAlignmentOptions.Center);
                Arriba(titulo.rectTransform, -22, 40);
                var guia = ui.Texto(sala, "Pasa el ratón por las mesas: alguien sabe lo que viene.", EstiloTexto.Cuerpo, tema.inkMuted,
                                    TextAlignmentOptions.Center);
                Arriba(guia.rectTransform, -62, 34);
                var contador = ui.Texto(sala, "", EstiloTexto.Leyenda, tema.ink, TextAlignmentOptions.Right);
                contador.rectTransform.anchorMin = contador.rectTransform.anchorMax = contador.rectTransform.pivot = new Vector2(1, 1);
                contador.rectTransform.sizeDelta = new Vector2(320, 30);
                contador.rectTransform.anchoredPosition = new Vector2(-28, -26);

                var buena = revelaciones.Count == 0 ? -1 : new System.Random(revelaciones.Count + DateTime.Now.Second).Next(Sitios.Length);
                var oidas = 0;
                Action contar = () => contador.text = $"MESAS ESCUCHADAS  {oidas} / {Sitios.Length}";
                contar();
                for (var i = 0; i < Sitios.Length; i++) {
                    var frase = i == buena ? revelaciones[0] : Charla[i];
                    MesaDeRumor.Crear(ui, sala, Sitios[i], frase, i == buena, i, () => { oidas++; contar(); });
                }

                var listo = ui.Boton(sala, "Volver", cerrar, VarianteBoton.Primario);
                var rl = (RectTransform)listo.transform;
                rl.anchorMin = rl.anchorMax = rl.pivot = new Vector2(0.5f, 0);
                rl.sizeDelta = new Vector2(240, 56);
                rl.anchoredPosition = new Vector2(0, 22);
            }

            private static void Arriba(RectTransform rt, float y, float alto) {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(0, alto);
                rt.anchoredPosition = new Vector2(0, y);
            }
        }

        /// <summary>Una mesa de la cafeteria: su gente alrededor, los «…» que laten, y el bocadillo que salta y se escribe.</summary>
        private sealed class MesaDeRumor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
            private static readonly Color Papel = new Color32(0xF7, 0xF4, 0xEC, 0xFF);
            private static readonly Color Tinta = new Color32(0x1E, 0x22, 0x2A, 0xFF);

            private RectTransform _bocadillo, _pista;
            private Image _brillo;
            private TMP_Text _texto;
            private Image[] _puntos;
            private RectTransform[] _gente;
            private Vector2[] _sitioGente;
            private Color _colorBase;
            private bool _buena, _oida, _encima;
            private float _tPop = -1, _letras, _semilla;
            private Action _alOir;

            public static void Crear(UiKit ui, RectTransform sala, Vector2 sitio, string frase, bool buena, int indice, Action alOir) {
                var tema = ui.Tema;
                var raiz = ui.Nodo(sala, "Mesa");
                raiz.anchorMin = raiz.anchorMax = sitio;
                raiz.pivot = new Vector2(0.5f, 0.5f);
                raiz.sizeDelta = new Vector2(240, 200);
                var hit = raiz.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                var m = raiz.gameObject.AddComponent<MesaDeRumor>();
                m._buena = buena;
                m._alOir = alOir;
                m._semilla = indice * 1.7f;
                m._colorBase = tema.cyan;

                // El brillo bajo la mesa: tenue siempre, dorado y latiendo en la que tiene la pista cuando ya se oyo.
                var brillo = ui.Nodo(raiz, "Brillo");
                brillo.sizeDelta = new Vector2(260, 260);
                m._brillo = brillo.gameObject.AddComponent<Image>();
                m._brillo.sprite = PlanoIlustrado.Brillo();
                m._brillo.color = NexusTheme.Alfa(tema.cyan, 0.12f);
                m._brillo.raycastTarget = false;

                // La gente: cuatro cabezas de colores alrededor de la mesa.
                var colores = new[] { tema.cyan, tema.violet, tema.warning, tema.success, tema.danger };
                var gente = ui.Nodo(raiz, "Gente");
                m._gente = new RectTransform[4];
                m._sitioGente = new Vector2[4];
                for (var k = 0; k < 4; k++) {
                    var ang = (k * 90 + 45 + indice * 20) * Mathf.Deg2Rad;
                    var cabeza = ui.Nodo(gente, "Cabeza");
                    cabeza.sizeDelta = new Vector2(34, 34);
                    var img = cabeza.gameObject.AddComponent<Image>();
                    img.sprite = ui.SpriteCircular();
                    img.color = colores[(indice + k) % colores.Length];
                    img.raycastTarget = false;
                    m._sitioGente[k] = new Vector2(Mathf.Cos(ang) * 74, Mathf.Sin(ang) * 54 - 6);
                    cabeza.anchoredPosition = m._sitioGente[k];
                    m._gente[k] = cabeza;
                }

                // La mesa, con dos tazas encima.
                var mesa = ui.Nodo(raiz, "Tablero");
                mesa.sizeDelta = new Vector2(140, 92);
                mesa.anchoredPosition = new Vector2(0, -6);
                var imgMesa = mesa.gameObject.AddComponent<Image>();
                imgMesa.sprite = ui.SpriteCircular();
                imgMesa.color = tema.surfaceRaised;
                imgMesa.raycastTarget = false;
                for (var k = 0; k < 2; k++) {
                    var taza = ui.Nodo(mesa, "Taza");
                    taza.sizeDelta = new Vector2(16, 16);
                    taza.anchoredPosition = new Vector2(k == 0 ? -26 : 24, k == 0 ? 8 : -10);
                    var imgTaza = taza.gameObject.AddComponent<Image>();
                    imgTaza.sprite = ui.SpriteCircular();
                    imgTaza.color = new Color32(0xE8, 0xD9, 0xC0, 0xFF);
                    imgTaza.raycastTarget = false;
                }

                // Los puntos suspensivos que laten mientras no se ha oido.
                var puntos = ui.Fila(raiz, "Puntos", 6, 0, TextAnchor.MiddleCenter);
                puntos.anchoredPosition = new Vector2(0, 84);
                puntos.sizeDelta = new Vector2(70, 26);
                ui.Fondo(puntos.gameObject.AddComponent<Image>(), tema.surfaceRaised, 13);
                m._puntos = new Image[3];
                for (var k = 0; k < 3; k++) m._puntos[k] = ui.Punto(puntos, tema.ink, 8);

                // El bocadillo: papel claro, texto grande y oscuro; sale encima de la mesa.
                var bocadillo = ui.PanelColumna(raiz, "Bocadillo", 16, 6, buena ? new Color32(0xFF, 0xF3, 0xC4, 0xFF) : Papel);
                bocadillo.anchorMin = bocadillo.anchorMax = new Vector2(0.5f, 1);
                bocadillo.pivot = new Vector2(0.5f, 0);
                bocadillo.anchoredPosition = new Vector2(0, -6);
                bocadillo.sizeDelta = new Vector2(360, 0);
                var ajuste = bocadillo.gameObject.AddComponent<ContentSizeFitter>();
                ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                ui.Borde(bocadillo, buena ? PlanoIlustrado.ColorDelDestello : tema.cyan, NexusTheme.RadioMd, 2.5f);
                if (buena) {
                    m._pista = ui.Chip(bocadillo, "PISTA", PlanoIlustrado.ColorDelDestello, Tinta);
                    m._pista.gameObject.SetActive(false);
                }
                m._texto = ui.Texto(bocadillo, frase, EstiloTexto.Cuerpo, Tinta, TextAlignmentOptions.Center);
                m._texto.fontSize *= 1.15f;
                m._texto.maxVisibleCharacters = 0;
                m._bocadillo = bocadillo;
                bocadillo.gameObject.SetActive(false);
            }

            public void OnPointerEnter(PointerEventData e) {
                _encima = true;
                // La mesa que se escucha se pinta encima de las demas, con su bocadillo.
                transform.SetAsLastSibling();
                if (_oida) return;
                _oida = true;
                _bocadillo.gameObject.SetActive(true);
                _tPop = 0;
                _letras = 0;
                _alOir?.Invoke();
            }

            public void OnPointerExit(PointerEventData e) { _encima = false; }

            private void Update() {
                var dt = Time.unscaledDeltaTime;
                var t = Time.unscaledTime + _semilla;

                // Sin oir: los «…» laten uno tras otro. Oida: se quitan.
                var fila = _puntos[0].transform.parent.gameObject;
                if (!_oida) {
                    for (var k = 0; k < _puntos.Length; k++) {
                        var a = 0.35f + 0.65f * Mathf.Max(0, Mathf.Sin(t * 5f - k * 0.9f));
                        var c = _puntos[k].color;
                        _puntos[k].color = new Color(c.r, c.g, c.b, a);
                        _puntos[k].transform.localScale = Vector3.one * (0.8f + 0.35f * a);
                    }
                } else if (fila.activeSelf) {
                    fila.SetActive(false);
                }

                // La gente charla: cuando escuchas, se mueven mas.
                var fuerza = _encima ? 5f : 1.5f;
                for (var k = 0; k < _gente.Length; k++)
                    _gente[k].anchoredPosition = _sitioGente[k] + Vector2.up * Mathf.Sin(t * (_encima ? 9f : 2.2f) + k * 1.3f) * fuerza;

                // La mesa crece un poco bajo el raton.
                var s = Mathf.MoveTowards(transform.localScale.x, _encima ? 1.08f : 1f, dt * 1.2f);
                transform.localScale = new Vector3(s, s, 1);

                if (_tPop < 0) return;
                // El bocadillo salta (con un rebote) y la frase se escribe letra a letra.
                _tPop += dt;
                var p = Mathf.Clamp01(_tPop / 0.32f);
                const float c1 = 1.70158f, c3 = c1 + 1;
                var rebote = 1 + c3 * Mathf.Pow(p - 1, 3) + c1 * Mathf.Pow(p - 1, 2);
                _bocadillo.localScale = Vector3.one * Mathf.Max(0.01f, rebote);
                var total = _texto.textInfo == null ? 0 : _texto.textInfo.characterCount;
                if (_tPop > 0.15f) {
                    _letras += dt * 48f;
                    _texto.maxVisibleCharacters = total > 0 ? Mathf.Min(total, (int)_letras) : (int)_letras;
                }
                var escrito = total > 0 && _letras >= total;

                // La buena: el brillo se vuelve dorado y late, y aparece la marca «PISTA».
                if (_buena && escrito) {
                    if (_pista != null && !_pista.gameObject.activeSelf) _pista.gameObject.SetActive(true);
                    var latido = 0.35f + 0.2f * Mathf.Sin(t * 4f);
                    _brillo.color = NexusTheme.Alfa(PlanoIlustrado.ColorDelDestello, latido);
                    _brillo.transform.localScale = Vector3.one * (1.1f + 0.08f * Mathf.Sin(t * 4f));
                } else {
                    _brillo.color = NexusTheme.Alfa(_colorBase, _encima ? 0.24f : 0.1f);
                }
            }
        }

        // ---- el tablon

        /// <summary>Tres notas boca abajo en un corcho; al voltearlas, las pistas de lo que podria aparecer.</summary>
        private static class TablonDeNotas {
            public static void Crear(UiKit ui, RectTransform capa, List<string> pistas, Action cerrar) {
                var tema = ui.Tema;
                var corcho = ui.PanelColumna(capa, "Corcho", tema.margen, tema.espacio, new Color32(0xB0, 0x83, 0x55, 0xFF));
                corcho.anchorMin = corcho.anchorMax = corcho.pivot = new Vector2(0.5f, 0.5f);
                corcho.sizeDelta = new Vector2(1000, 520);
                ui.Texto(corcho, "EL TABLÓN · voltea las notas", EstiloTexto.Leyenda, new Color32(0x3A, 0x26, 0x10, 0xFF), TextAlignmentOptions.Center);
                var fila = ui.Fila(corcho, "Notas", tema.espacio, alineacion: TextAnchor.MiddleCenter);
                UiKit.Tamano(fila, flexAlto: 1);
                for (var i = 0; i < 3; i++) {
                    var nota = ui.PanelColumna(fila, "Nota", tema.Espacio(4), 0, new Color32(0xF6, 0xE2, 0x7F, 0xFF));
                    UiKit.Tamano(nota, 280, 300);
                    nota.localEulerAngles = new Vector3(0, 0, (i - 1) * 4);
                    var dorso = ui.Texto(nota, "?", EstiloTexto.Hero, new Color32(0x6A, 0x5A, 0x3A, 0xFF), TextAlignmentOptions.Center);
                    var texto = ui.Texto(nota, i < pistas.Count ? pistas[i] : "(una nota en blanco)", EstiloTexto.Pequeno,
                                         new Color32(0x26, 0x26, 0x2A, 0xFF), TextAlignmentOptions.Center);
                    texto.font = ui.FuenteTiza;
                    texto.gameObject.SetActive(false);
                    var viva = nota.gameObject.AddComponent<PiezaViva>();
                    var d = dorso.gameObject;
                    var t = texto.gameObject;
                    viva.AlPulsar = () => { d.SetActive(false); t.SetActive(true); };
                }
                ui.Boton(corcho, "Volver", cerrar, VarianteBoton.Primario);
            }
        }

        // ==================================================================== piezas de la escena

        /// <summary>Un vaiven lento de camara (Ken Burns): la sala respira.</summary>
        private sealed class VaivenDeCamara : MonoBehaviour {
            private void Update() {
                var t = Time.unscaledTime * 0.08f;
                transform.localScale = Vector3.one * (1.04f + 0.03f * Mathf.Sin(t));
                ((RectTransform)transform).anchoredPosition = new Vector2(Mathf.Sin(t * 0.7f) * 18f, Mathf.Cos(t * 0.5f) * 10f);
            }
        }

        /// <summary>Sube y baja un poco, para que se vea vivo (bocadillos, el punto del minijuego).</summary>
        private sealed class Flotar : MonoBehaviour {
            private float _semilla;
            private Vector3 _base;
            private void Start() { _semilla = UnityEngine.Random.value * 10f; _base = transform.localPosition; }
            private void Update() { transform.localPosition = _base + Vector3.up * Mathf.Sin(Time.unscaledTime * 2f + _semilla) * 5f; }
        }

        /// <summary>
        /// Un coleccionable en la escena de la zona: flota, huye del cursor segun su dificultad (HuidaDelDestello) y se
        /// atrapa con un clic, como en el mapa.
        /// </summary>
        private sealed class DestelloLibre : MonoBehaviour, IPointerClickHandler {
            private HuidaDelDestello _huida;
            private RectTransform _rt, _padre;
            private Image _luz;
            private Vector2 _ancla, _pos, _azar;
            private float _semilla, _rapidez = 6f;
            private bool _colocado, _atrapado;
            private Func<bool> _activo;
            private Action _alAtrapar;

            public static void Crear(UiKit ui, RectTransform padre, string dificultad, System.Random rnd, Func<bool> activo, Action alAtrapar) {
                var rt = ui.Nodo(padre, "Destello");
                rt.sizeDelta = new Vector2(70, 70);
                var hit = rt.gameObject.AddComponent<Image>();
                hit.color = new Color(0, 0, 0, 0);
                var luz = ui.Nodo(rt, "Luz");
                luz.sizeDelta = new Vector2(70, 70);
                var img = luz.gameObject.AddComponent<Image>();
                img.sprite = PlanoIlustrado.Brillo();
                img.color = PlanoIlustrado.ColorDelDestello;
                img.raycastTarget = false;
                var d = rt.gameObject.AddComponent<DestelloLibre>();
                d._huida = new HuidaDelDestello(dificultad);
                d._rt = rt;
                d._padre = padre;
                d._luz = img;
                d._semilla = (float)rnd.NextDouble() * 100f;
                d._azar = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
                d._activo = activo;
                d._alAtrapar = alAtrapar;
            }

            public void OnPointerClick(PointerEventData e) {
                if (_atrapado || !_activo()) return;
                _atrapado = true;
                StartCoroutine(Atrapar());
            }

            private System.Collections.IEnumerator Atrapar() {
                for (var t = 0f; t < 0.35f; t += Time.unscaledDeltaTime) {
                    var f = t / 0.35f;
                    _luz.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.2f, 3.2f, f);
                    _luz.color = NexusTheme.Alfa(PlanoIlustrado.ColorDelDestello, 1 - f);
                    yield return null;
                }
                _alAtrapar();
            }

            private void Update() {
                if (_atrapado) return;
                var r = _padre.rect;
                if (r.width < 10) return;
                var interior = new Rect(r.xMin + 60, r.yMin + 60, r.width - 120, r.height - 200);
                if (!_colocado) {
                    _colocado = true;
                    _ancla = _pos = new Vector2(Mathf.Lerp(interior.xMin, interior.xMax, _azar.x), Mathf.Lerp(interior.yMin, interior.yMax, _azar.y));
                }
                Vector2 raton;
                if (_activo() && PlanoIlustrado.Raton(_padre, out raton)) {
                    float nx, ny;
                    if (_huida.Asustar(_pos.x, _pos.y, raton.x, raton.y, interior.xMin, interior.yMin, interior.xMax, interior.yMax, out nx, out ny)) {
                        _ancla = new Vector2(nx, ny);
                        _rapidez = 16f;
                    }
                }
                var t = Time.unscaledTime * 0.45f * _huida.Ritmo + _semilla;
                var vaiven = new Vector2(Mathf.PerlinNoise(t, _semilla) - 0.5f, Mathf.PerlinNoise(_semilla, t) - 0.5f) * 40f;
                _pos = Vector2.Lerp(_pos, _ancla + vaiven, 1 - Mathf.Exp(-_rapidez * Time.unscaledDeltaTime));
                _rapidez = Mathf.MoveTowards(_rapidez, 6f, Time.unscaledDeltaTime * 20f);
                _rt.anchoredPosition = _pos;
                var latido = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (_huida.Cansado ? 5f : 2.6f) + _semilla);
                _luz.rectTransform.localScale = Vector3.one * (_huida.Cansado ? 1.25f : 1f) * Mathf.Lerp(0.85f, 1.1f, latido);
                _luz.color = NexusTheme.Alfa(PlanoIlustrado.ColorDelDestello, (_activo() ? 0.9f : 0.35f) * Mathf.Lerp(0.75f, 1f, latido));
            }
        }
    }
}
