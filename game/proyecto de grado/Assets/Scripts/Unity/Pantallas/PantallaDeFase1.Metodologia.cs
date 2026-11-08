using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La Metodologia de la Fase 1 en TRES partes (2026-11-08), cada una de una pantalla, sin scroll:
    ///   0 · Elegir     las cartas (con su imagen) y, debajo, una animacion de ≤ 15 s con las tres metodologias.
    ///                  Al elegir, pasa sola a la parte 1.
    ///   1 · ¿Por qué?  la carta elegida (al pasar el raton se voltea: «Cambiar metodología»), las razones en dos
    ///                  columnas y, debajo, la animacion detallada de la elegida. Al elegir razon, pasa sola a la 2.
    ///   2 · Duración   cuanto dura cada sprint / como se reparten las etapas / cada cuanto se revisa el flujo, con la
    ///                  vista previa del calendario a escala.
    /// Las partes se recorren con «Atrás» y «Siguiente» como las del expediente (_subPaso).
    /// </summary>
    public sealed partial class PantallaDeFase1 {
        private const int PartesDeMetodologia = 3;
        private int _avanceProgramado;

        private bool ParteCompleta() {
            if (_paso == Paso.Arquitectura) return _subPaso == 0 ? _arquitectura != null : true;
            if (_paso != Paso.Metodologia) return true;
            if (_subPaso == 0) return _metodologia != null;
            if (_subPaso == 1) return _razonMetodologia != null;
            return true;
        }

        /// <summary>
        /// Una hoja que mide EXACTAMENTE lo que el visor: si lo de dentro pide mas, se encoge (EscaladorAlCaber). Asi
        /// ninguna de estas pantallas hace scroll.
        /// </summary>
        private Hoja LienzoSinScroll(string nombre) {
            var ajuste = Ui.Nodo(_hoja.Raiz, nombre);
            UiKit.Tamano(ajuste, alto: 100, flexAncho: 1);   // el alto real lo fija el escalador: el del visor
            var interior = Ui.Columna(ajuste, "Interior", Tema.espacio);
            var escalador = ajuste.gameObject.AddComponent<EscaladorAlCaber>();
            escalador.Interior = interior;
            escalador.AltoFijo = () => _scroll.viewport.rect.height - 24;   // 24 = el relleno del contenido (10+10) y un margen
            return new Hoja(Ui, interior);
        }

        /// <summary>Pasa sola a otra parte tras un momento (lo que dura el sello). Si mientras tanto se repinta, se anula.</summary>
        private void AvanzarDespues(float segundos, int parte) {
            var turno = _avanceProgramado;
            var paso = _paso;
            StartCoroutine(Esperar(segundos, () => {
                if (turno == _avanceProgramado && _paso == paso) IrAParte(parte);
            }));
        }

        private static IEnumerator Esperar(float segundos, Action luego) {
            for (var t = 0f; t < segundos; t += Time.unscaledDeltaTime) yield return null;
            luego();
        }

        private void Metodologia() {
            var disponibles = S.MetodologiasDisponibles();
            var elegida = disponibles.FirstOrDefault(m => m.Id == _metodologia);
            if (elegida == null) _subPaso = 0;
            else if (_subPaso == 2 && _razonMetodologia == null) _subPaso = 1;
            switch (_subPaso) {
                case 0: ParteElegir(disponibles); break;
                case 1: ParteRazon(elegida); break;
                default: ParteDuracion(elegida); break;
            }
        }

        private void Cabecera(Hoja hoja, string titulo, Action<RectTransform> derecha = null) {
            var fila = hoja.Fila();
            var textos = Ui.Columna(fila, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(textos, flexAncho: 1);
            var etiqueta = Ui.Texto(textos, "Decisión 1 de 3 · Metodología · " + (_subPaso + 1) + " de " + PartesDeMetodologia,
                                    EstiloTexto.Leyenda, Tema.cyan);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Texto(textos, titulo, EstiloTexto.Titulo);
            if (derecha != null) derecha(fila);
        }

        // ==================================================================== 0 · elegir

        private void ParteElegir(IList<MethodologyProfile> disponibles) {
            var hoja = LienzoSinScroll("Metodologia · elegir");
            Cabecera(hoja, "¿Cómo va a trabajar el equipo?", fila => {
                // Las palabras del oficio, en un solo boton: quien ya las sabe no tiene que saltarlas.
                PantallaDeMenu.Boton(App, fila, "Palabras que vas a ver ?", "Palabras del oficio", () => {
                    var lista = new List<OpcionDeMenu>();
                    foreach (var termino in new[] { "ciclo-de-vida", "sprint", "backlog", "ceremonia", "retrospectiva", "wip" }) {
                        var e = Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, termino);
                        if (e != null) lista.Add(new OpcionDeMenu(e.Termino, () => PantallaDePizarra.AbrirConcepto(App, e)));
                    }
                    return lista;
                });
            });

            var rotulo = hoja.Fila();
            Ui.Texto(rotulo, "Metodologías", EstiloTexto.Subtitulo);
            UiKit.Tamano(Ui.Texto(rotulo, "Pasa el ratón por una para verla abajo; púlsala para elegirla.", EstiloTexto.Pequeno), flexAncho: 1);

            var filaCartas = Ui.Fila(hoja.Raiz, "Metodologias", Tema.Espacio(6), 0, TextAnchor.MiddleCenter);
            GuiaView.Registrar("fase1.metodologias", filaCartas);
            filaCartas.gameObject.AddComponent<Nexus.Unity.Aplicacion.ListaDeElementos>();

            var escena = Ui.Nodo(hoja.Raiz, "Explicacion general");
            var le = UiKit.Tamano(escena, flexAncho: 1, flexAlto: 1);
            le.minHeight = Tema.Px(150); le.preferredHeight = Tema.Px(200);
            var animacion = EscenaDeMetodologia.Crear(Ui, escena, disponibles.Select(DibujosDeMetodologia.Resumen).ToList(), "Las metodologias");
            UiKit.Rellenar((RectTransform)animacion.transform);

            var cartas = new List<CartaDeMetodologia>();
            for (var i = 0; i < disponibles.Count; i++) {
                var met = disponibles[i];
                var indice = i;
                CartaDeMetodologia carta = null;
                carta = CartaDeMetodologia.Crear(Ui, filaCartas, met, false, () => {
                    if (_metodologia != met.Id) { _razonMetodologia = null; _duracion = null; }   // otra metodologia, otros motivos y otro calendario
                    _metodologia = met.Id;
                    foreach (var c in cartas) { c.Elegida = false; c.Apagada = c != carta; }
                    carta.Sellar();
                    GuardarBorrador();
                    PintarNavegacion();
                    AvanzarDespues(0.8f, 1);
                }, 0.07f * i);
                UiKit.Tamano(carta, ancho: Tema.Px(220), alto: Tema.Px(150));
                carta.AlEnfocar = encima => { if (encima) animacion.Saltar(indice); };
                if (_metodologia == met.Id) carta.Elegida = true;
                else if (_metodologia != null) carta.Apagada = true;
                cartas.Add(carta);
            }
        }

        // ==================================================================== 1 · ¿por qué?

        private sealed class FichaDeRazon {
            public string Id;
            public Image Borde, Halo, Marca;
        }

        private void ParteRazon(MethodologyProfile elegida) {
            var hoja = LienzoSinScroll("Metodologia · por que");
            var queEs = Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, elegida.Id);
            Cabecera(hoja, "¿Por qué " + elegida.Nombre + "?", fila => {
                if (queEs != null) Ui.Boton(fila, "¿Qué es " + elegida.Nombre + "?", () => PantallaDePizarra.AbrirConcepto(App, queEs), VarianteBoton.Fantasma);
            });

            var medio = Ui.Fila(hoja.Raiz, "Eleccion y razones", Tema.Espacio(6), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(medio, flexAncho: 1);

            var carta = CartaDeMetodologia.Crear(Ui, medio, elegida, true, () => {
                _metodologia = null; _razonMetodologia = null; _duracion = null;
                IrAParte(0);
            });
            carta.Elegida = true;
            UiKit.Tamano(carta, ancho: Tema.Px(210), alto: Tema.Px(205));

            var derecha = Ui.Columna(medio, "Razones", Tema.Espacio(2));
            UiKit.Tamano(derecha, flexAncho: 1);
            var motivos = elegida.TextosDeRazones.OrderBy(x => x.Value, StringComparer.Ordinal).ToList();
            RejillaDeRazones(derecha, motivos, () => _razonMetodologia, id => {
                _razonMetodologia = id;
                GuardarBorrador();
                PintarNavegacion();
                AvanzarDespues(0.6f, 2);
            });

            var escena = Ui.Nodo(hoja.Raiz, "Explicacion detallada");
            var le = UiKit.Tamano(escena, flexAncho: 1, flexAlto: 1);
            le.minHeight = Tema.Px(150); le.preferredHeight = Tema.Px(190);
            var animacion = EscenaDeMetodologia.Crear(Ui, escena, DibujosDeMetodologia.Detalle(elegida), "La metodologia en detalle");
            UiKit.Rellenar((RectTransform)animacion.transform);
        }

        /// <summary>
        /// Las razones en dos columnas de fichas compactas (la metodologia y la arquitectura): la elegida, con su filo
        /// cian. 'elegida' dice cual lo esta; 'alElegir' guarda la nueva (y decide si se pasa sola a otra parte).
        /// </summary>
        private void RejillaDeRazones(Transform padre, IList<KeyValuePair<string, string>> motivos, Func<string> elegida, Action<string> alElegir) {
            Ui.Texto(padre, "Elige 1 de estas " + motivos.Count + " razones. El motivo cuenta tanto como la elección.", EstiloTexto.Pequeno);
            var rejilla = Ui.Fila(padre, "Rejilla", Tema.Espacio(3), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(rejilla, flexAncho: 1);
            rejilla.gameObject.AddComponent<Nexus.Unity.Aplicacion.ListaDeElementos>();
            GuiaView.Registrar("fase1.razones", rejilla);
            var columnas = new[] { Ui.Columna(rejilla, "Columna 1", Tema.Espacio(2)), Ui.Columna(rejilla, "Columna 2", Tema.Espacio(2)) };
            foreach (var c in columnas) UiKit.Tamano(c, flexAncho: 1);
            var porColumna = (motivos.Count + 1) / 2;
            var fichas = new List<FichaDeRazon>();
            for (var i = 0; i < motivos.Count; i++)
                fichas.Add(Razon(columnas[i < porColumna ? 0 : 1], motivos[i].Key, motivos[i].Value, fichas, elegida, alElegir));
            PintarRazonElegida(fichas, elegida());
        }

        private FichaDeRazon Razon(Transform padre, string id, string texto, List<FichaDeRazon> todas, Func<string> elegida, Action<string> alElegir) {
            var f = new FichaDeRazon { Id = id };
            var rt = Ui.Nodo(padre, "Razon " + id);
            UiKit.Tamano(rt, alto: Tema.Px(40), flexAncho: 1);
            var img = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(img, NexusTheme.Alfa(Tema.bg950, 0.55f), NexusTheme.RadioMd);
            f.Halo = Ui.Halo(rt, NexusTheme.Alfa(Tema.cyan, 0), NexusTheme.RadioMd);
            f.Borde = Ui.Borde(rt, Tema.lineStrong, NexusTheme.RadioMd, 1.5f);
            var marca = Ui.Nodo(rt, "Marca");
            marca.anchorMin = marca.anchorMax = new Vector2(0, 0.5f);
            marca.pivot = new Vector2(0.5f, 0.5f);
            marca.sizeDelta = new Vector2(Tema.Px(12), Tema.Px(12));
            marca.anchoredPosition = new Vector2(Tema.Px(16), 0);
            f.Marca = marca.gameObject.AddComponent<Image>();
            f.Marca.sprite = Ui.SpriteCircular();
            f.Marca.raycastTarget = false;
            var t = TextoEnCaja.Crear(Ui, rt, texto, EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.MidlineLeft, Tema.Px(13.5f),
                                      Vector2.zero, Vector2.one);
            t.rectTransform.offsetMin = new Vector2(Tema.Px(30), 3);
            t.rectTransform.offsetMax = new Vector2(-Tema.Px(8), -3);
            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = img;
            boton.transition = Selectable.Transition.None;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            var rebote = rt.gameObject.AddComponent<ReboteDeCalidad>();
            boton.onClick.AddListener(() => {
                alElegir(id);
                PintarRazonElegida(todas, elegida());
                rebote.Lanzar();
            });
            var r = rt.gameObject.AddComponent<RealceDeCalidad>();
            r.Fondo = img; r.Normal = NexusTheme.Alfa(Tema.bg950, 0.55f); r.Resaltado = NexusTheme.Alfa(Tema.cyan, 0.16f);
            return f;
        }

        private void PintarRazonElegida(List<FichaDeRazon> fichas, string elegida) {
            foreach (var f in fichas) {
                var si = f.Id == elegida;
                f.Borde.color = si ? Tema.cyan : Tema.lineStrong;
                f.Halo.color = NexusTheme.Alfa(Tema.cyan, si ? 0.3f : 0);
                f.Marca.color = si ? Tema.cyan : NexusTheme.Alfa(Tema.inkMuted, 0.4f);
            }
        }

        // ==================================================================== 2 · duracion

        private sealed class FilaDeDuracion {
            public OpcionDeCalendario Opcion;
            public Image Borde, Halo, Marca;
        }

        private void ParteDuracion(MethodologyProfile elegida) {
            var hoja = LienzoSinScroll("Metodologia · duracion");
            var cal = elegida.Calendario ?? new Calendario();
            var unidad = string.IsNullOrEmpty(cal.EtiquetaUnidad) ? "Sprint" : cal.EtiquetaUnidad;
            var pregunta = cal.Tipo == TiposDeCalendario.Iterativo ? "¿Cuánto debe durar cada " + unidad.ToLowerInvariant() + "?"
                         : cal.Tipo == TiposDeCalendario.Continuo ? "¿Cada cuánto se revisa el flujo y se entrega?"
                         : "¿Cómo repartes los días entre las etapas?";
            Cabecera(hoja, pregunta);
            hoja.Nota("El nivel dura " + Perfil.DiasTotales + " días. No hay opción correcta: cada una cambia cuántas veces entregas.");

            var opciones = OpcionesDeCalendario.Para(elegida, Perfil.DiasTotales);
            var marcada = DuracionMarcada(opciones) ?? opciones.FirstOrDefault();

            var cuerpo = Ui.Fila(hoja.Raiz, "Duracion", Tema.Espacio(6), 0, TextAnchor.UpperLeft);
            var le = UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
            le.preferredHeight = Tema.Px(330); le.minHeight = Tema.Px(240);
            GuiaView.Registrar("fase1.duracion", cuerpo);
            cuerpo.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            CalendarioPrevio previo;
            if (opciones.Count >= 2) {
                var lista = Ui.Columna(cuerpo, "Opciones", Tema.Espacio(2));
                UiKit.Tamano(lista, flexAncho: 0.85f);
                lista.gameObject.AddComponent<Nexus.Unity.Aplicacion.ListaDeElementos>();
                var zonaPrevio = Ui.Nodo(cuerpo, "Vista previa");
                UiKit.Tamano(zonaPrevio, flexAncho: 1.15f, flexAlto: 1);
                previo = CalendarioPrevio.Crear(Ui, zonaPrevio);
                UiKit.Rellenar((RectTransform)previo.transform);
                var filas = new List<FilaDeDuracion>();
                foreach (var o in opciones) filas.Add(Duracion(lista, o, filas, previo, unidad));
                PintarDuracionElegida(filas);
            } else {
                var zonaPrevio = Ui.Nodo(cuerpo, "Vista previa");
                UiKit.Tamano(zonaPrevio, flexAncho: 1, flexAlto: 1);
                previo = CalendarioPrevio.Crear(Ui, zonaPrevio);
                UiKit.Rellenar((RectTransform)previo.transform);
            }
            if (marcada != null) previo.Mostrar(marcada, Perfil.DiasTotales, unidad);
        }

        private OpcionDeCalendario DuracionMarcada(IEnumerable<OpcionDeCalendario> opciones) {
            return opciones.FirstOrDefault(o => _duracion == null ? o.EsLaDeSerie : o.Id == _duracion);
        }

        private FilaDeDuracion Duracion(Transform padre, OpcionDeCalendario o, List<FilaDeDuracion> todas, CalendarioPrevio previo, string unidad) {
            var f = new FilaDeDuracion { Opcion = o };
            var rt = Ui.Nodo(padre, "Opcion " + o.Id);
            UiKit.Tamano(rt, alto: Tema.Px(58), flexAncho: 1);
            var img = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(img, NexusTheme.Alfa(Tema.bg950, 0.55f), NexusTheme.RadioMd);
            f.Halo = Ui.Halo(rt, NexusTheme.Alfa(Tema.cyan, 0), NexusTheme.RadioMd);
            f.Borde = Ui.Borde(rt, Tema.lineStrong, NexusTheme.RadioMd, 1.5f);
            var marca = Ui.Nodo(rt, "Marca");
            marca.anchorMin = marca.anchorMax = new Vector2(0, 0.5f);
            marca.sizeDelta = new Vector2(Tema.Px(14), Tema.Px(14));
            marca.anchoredPosition = new Vector2(Tema.Px(18), 0);
            f.Marca = marca.gameObject.AddComponent<Image>();
            f.Marca.sprite = Ui.SpriteCircular();
            f.Marca.raycastTarget = false;
            var titulo = TextoEnCaja.Crear(Ui, rt, o.Titulo + (o.EsLaDeSerie ? "  ·  lo habitual" : ""), EstiloTexto.Encabezado, Tema.ink,
                                           TextAlignmentOptions.BottomLeft, Tema.Px(16), new Vector2(0, 0.48f), Vector2.one);
            titulo.fontStyle = FontStyles.Bold;
            titulo.rectTransform.offsetMin = new Vector2(Tema.Px(34), 0); titulo.rectTransform.offsetMax = new Vector2(-Tema.Px(8), -3);
            var detalle = TextoEnCaja.Crear(Ui, rt, o.Detalle ?? "", EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.TopLeft,
                                            Tema.Px(12.5f), Vector2.zero, new Vector2(1, 0.48f));
            detalle.rectTransform.offsetMin = new Vector2(Tema.Px(34), 3); detalle.rectTransform.offsetMax = new Vector2(-Tema.Px(8), 0);

            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = img;
            boton.transition = Selectable.Transition.None;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            var rebote = rt.gameObject.AddComponent<ReboteDeCalidad>();
            boton.onClick.AddListener(() => {
                _duracion = o.EsLaDeSerie ? null : o.Id;
                PintarDuracionElegida(todas);
                rebote.Lanzar();
                previo.Mostrar(o, Perfil.DiasTotales, unidad);
                GuardarBorrador();
            });
            var r = rt.gameObject.AddComponent<RealceDeCalidad>();
            r.Fondo = img; r.Normal = NexusTheme.Alfa(Tema.bg950, 0.55f); r.Resaltado = NexusTheme.Alfa(Tema.cyan, 0.16f);
            // Mirar una opcion la enseña en la vista previa; al salir, vuelve la elegida.
            r.Entra = () => previo.Mostrar(o, Perfil.DiasTotales, unidad);
            r.Sale = () => { var m = DuracionMarcada(todas.Select(x => x.Opcion)); if (m != null) previo.Mostrar(m, Perfil.DiasTotales, unidad); };
            return f;
        }

        private void PintarDuracionElegida(List<FilaDeDuracion> filas) {
            var marcada = DuracionMarcada(filas.Select(x => x.Opcion));
            foreach (var f in filas) {
                var si = f.Opcion == marcada;
                f.Borde.color = si ? Tema.cyan : Tema.lineStrong;
                f.Halo.color = NexusTheme.Alfa(Tema.cyan, si ? 0.3f : 0);
                f.Marca.color = si ? Tema.cyan : NexusTheme.Alfa(Tema.inkMuted, 0.4f);
            }
        }
    }
}
