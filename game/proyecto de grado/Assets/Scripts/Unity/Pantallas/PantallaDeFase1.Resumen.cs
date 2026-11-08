using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Fase1;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El Resumen como CABINA DE LANZAMIENTO (2026-11-08). Cada decision es un instrumento, no un parrafo:
    ///   · El proyecto     cuantas comprobaciones acertaste        · Equipo       las caras de quien va contigo
    ///   · Metodologia     su dibujo y el calendario en una tira   · Recorrido    minutos, suministros y hallazgos
    ///   · Calidad         un radar con las fichas repartidas      · Backlog      la pila de historias
    ///   · Arquitectura    su dibujo
    /// Lo que falta late en ambar; lo hecho brilla en su color. Pulsar un instrumento lleva a ese paso, y al pasar el raton
    /// sale el «porque» que antes era texto. Arriba, un anillo cuenta cuantos estan listos; abajo, la palanca de lanzar,
    /// que hace una cuenta atras antes de empezar el dia 1. Todo en una pantalla, sin scroll.
    /// </summary>
    public sealed partial class PantallaDeFase1 {
        private bool _lanzando;

        private void Resumen() {
            var hoja = LienzoSinScroll("Resumen · cabina");
            Tooltip().OcultarYa();
            var pasos = Pasos().Where(p => p != Paso.Resumen).ToList();
            var hechos = pasos.Count(Completo);
            var listo = hechos == pasos.Count;

            // ---- cabecera: el titulo y el anillo de «listo»
            var cabecera = hoja.Fila();
            var titulos = Ui.Columna(cabecera, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            var etiqueta = Ui.Texto(titulos, "Antes de empezar · ya no hay vuelta atrás", EstiloTexto.Leyenda, Tema.cyan);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Texto(titulos, "Tu plan", EstiloTexto.Titulo);
            var anillo = Ui.Columna(cabecera, "Listo", 0, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(anillo, ancho: Tema.Px(104));
            PiezasDeProyecto.Anillo(Ui, anillo, Tema.Px(72), hechos / (float)Mathf.Max(1, pasos.Count), listo ? Tema.success : Tema.warning,
                                    hechos + "/" + pasos.Count);
            var leyendaAnillo = Ui.Texto(anillo, listo ? "LISTO" : "DECIDIDO", EstiloTexto.Leyenda, listo ? Tema.success : Tema.warning, TextAlignmentOptions.Center);
            UiKit.Tamano(leyendaAnillo, alto: Tema.Px(16));

            // ---- los instrumentos, en dos filas
            var filas = new[] { Ui.Fila(hoja.Raiz, "Instrumentos 1", Tema.Espacio(4), 0, TextAnchor.MiddleCenter),
                                Ui.Fila(hoja.Raiz, "Instrumentos 2", Tema.Espacio(4), 0, TextAnchor.MiddleCenter) };
            foreach (var f in filas) {
                var le = UiKit.Tamano(f, flexAncho: 1, flexAlto: 1);
                le.preferredHeight = Tema.Px(190); le.minHeight = Tema.Px(130);
                f.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
                f.gameObject.AddComponent<ListaDeElementos>();
            }
            var instrumentos = new List<Action<RectTransform, float>>();

            // El proyecto: cuanto se comprobo
            instrumentos.Add((fila, retraso) => {
                var ok = Preguntas.Count(p => _comprobacion.ContainsKey(p.Id));
                var limpias = Preguntas.Count(p => _comprobacion.ContainsKey(p.Id) && !_falladas.Contains(p.Id));
                RectTransform c;
                Instrumento(fila, "El proyecto", Paso.Encargo, ComprobacionHecha, Tema.cyan, "El proyecto: " + Perfil.Proyecto?.Nombre,
                            "Respondiste bien " + ok + " de " + Preguntas.Count + " comprobaciones" + (ok > 0 ? " (" + limpias + " a la primera)." : "."), retraso, out c);
                TextoEnCaja.Crear(Ui, c, ok + "/" + Preguntas.Count, EstiloTexto.Titulo, Tema.cyan, TextAlignmentOptions.Center, Tema.Px(46),
                                  new Vector2(0, 0.30f), Vector2.one, 4).fontStyle = FontStyles.Bold;
                TextoEnCaja.Crear(Ui, c, "COMPROBADO", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center, Tema.Px(12),
                                  Vector2.zero, new Vector2(1, 0.28f), 2);
            });

            // Metodologia: su dibujo, su nombre y el calendario
            var met = S.MetodologiasDisponibles().FirstOrDefault(m => m.Id == _metodologia);
            instrumentos.Add((fila, retraso) => {
                RectTransform c;
                var duracion = met == null ? null : Nexus.Core.Metodologia.OpcionesDeCalendario.Para(met, Perfil.DiasTotales)
                                   .FirstOrDefault(o => _duracion == null ? o.EsLaDeSerie : o.Id == _duracion);
                var porque = met == null ? "Falta elegir la metodología." : "Porque: «" + met.TextoDe(_razonMetodologia) + "»" +
                             (duracion != null && !string.IsNullOrEmpty(duracion.Titulo) ? "\n" + duracion.Titulo : "");
                Instrumento(fila, "Metodología", Paso.Metodologia, _razonMetodologia != null, Tema.cyan, met == null ? "Metodología" : met.Nombre, porque, retraso, out c);
                if (met == null) { SinDecidir(c); return; }
                var dibujo = Ui.Nodo(c, "Dibujo");
                dibujo.anchorMin = new Vector2(0, 0.42f); dibujo.anchorMax = Vector2.one; dibujo.offsetMin = dibujo.offsetMax = Vector2.zero;
                var lamina = PiezasDeProyecto.NuevaLamina(Ui, dibujo, 600, 300);
                lamina.Dibujo.Tiza = true;
                DibujosDeMetodologia.Icono(new Tizador(lamina, Ui.FuenteTiza), met);
                TextoEnCaja.Crear(Ui, c, met.Nombre, EstiloTexto.Encabezado, Tema.ink, TextAlignmentOptions.Center, Tema.Px(18),
                                  new Vector2(0, 0.24f), new Vector2(1, 0.42f), 2).fontStyle = FontStyles.Bold;
                var tira = Ui.Nodo(c, "Calendario");
                tira.anchorMin = new Vector2(0.02f, 0.02f); tira.anchorMax = new Vector2(0.98f, 0.22f); tira.offsetMin = tira.offsetMax = Vector2.zero;
                MiniTira.Crear(Ui, tira, duracion, Perfil.DiasTotales);
            });

            // Calidad: el radar
            instrumentos.Add((fila, retraso) => {
                RectTransform c;
                var calidad = Perfil.Fase1.Calidad;
                var reparto = string.Join("\n", calidad.Atributos.Select(a => a.Nombre + ": " + _fichas[a.Id] + (_fichas[a.Id] == 1 ? " ficha" : " fichas")));
                Instrumento(fila, "Calidad", Paso.Calidad, true, Tema.cyan, "Tus fichas de calidad", reparto, retraso, out c);
                RadarDeCalidad.Crear(Ui, c, calidad.Atributos, id => _fichas[id]);
            });

            // Arquitectura: su dibujo
            var arq = Perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == _arquitectura);
            var razon = Perfil.Fase1.RazonesDisponibles.FirstOrDefault(r => r.Id == _razonArquitectura);
            instrumentos.Add((fila, retraso) => {
                RectTransform c;
                Instrumento(fila, "Arquitectura", Paso.Arquitectura, razon != null, Tema.cyan, arq == null ? "Arquitectura" : arq.Nombre,
                            arq == null ? "Falta elegir la arquitectura." : "Porque: «" + (razon != null ? razon.Texto : "…") + "»", retraso, out c);
                if (arq == null) { SinDecidir(c); return; }
                var dibujo = Ui.Nodo(c, "Dibujo");
                dibujo.anchorMin = new Vector2(0, 0.24f); dibujo.anchorMax = Vector2.one; dibujo.offsetMin = dibujo.offsetMax = Vector2.zero;
                var lamina = PiezasDeProyecto.NuevaLamina(Ui, dibujo, 600, 300);
                lamina.Dibujo.Tiza = true;
                DibujosDeArquitecturaAnimados.Icono(new Tizador(lamina, Ui.FuenteTiza), arq, Perfil.Proyecto);
                TextoEnCaja.Crear(Ui, c, arq.Nombre, EstiloTexto.Encabezado, Tema.ink, TextAlignmentOptions.Center, Tema.Px(18),
                                  new Vector2(0, 0.0f), new Vector2(1, 0.24f), 2).fontStyle = FontStyles.Bold;
            });

            // Equipo: las caras
            instrumentos.Add((fila, retraso) => {
                RectTransform c;
                var equipo = S.EquipoDelProyecto();
                var solo = S.EquipoDesdeRecoleccion && S.TrabajasSolo;
                var porFormar = S.EquipoDesdeRecoleccion && !S.RecoleccionHecha;
                var completo = !HayRecoleccion || S.RecoleccionHecha;
                Instrumento(fila, "Equipo", HayRecoleccion ? Paso.Recoleccion : Paso.Encargo, completo, Tema.cyan, "Tu equipo",
                            porFormar ? "Todavía no lo formaste: lo haces en el recorrido." : solo ? "Trabajas tú solo, y más despacio."
                                        : string.Join("\n", equipo.Select(m => m.Nombre + " · " + m.Rol)), retraso, out c);
                if (porFormar) { SinDecidir(c); return; }
                var caras = Ui.Fila(c, "Caras", Tema.Espacio(1), 0, TextAnchor.MiddleCenter);
                caras.anchorMin = new Vector2(0, 0.28f); caras.anchorMax = Vector2.one; caras.offsetMin = caras.offsetMax = Vector2.zero;
                var lado = equipo.Count > 4 ? Tema.Px(48) : Tema.Px(66);
                if (solo || equipo.Count == 0) RostroDePersonaje.Crear(Ui, caras, "personajes/protagonista", "Tú", Tema.Px(66), Tema.warning);
                else foreach (var m in equipo.Take(7)) RostroDePersonaje.Crear(Ui, caras, RostroDePersonaje.FotoDe(m), m.Nombre, lado);
                TextoEnCaja.Crear(Ui, c, solo || equipo.Count == 0 ? "SOLO" : equipo.Count + (equipo.Count == 1 ? " PERSONA" : " PERSONAS"), EstiloTexto.Leyenda,
                                  Tema.inkMuted, TextAlignmentOptions.Center, Tema.Px(12), Vector2.zero, new Vector2(1, 0.26f), 2);
            });

            // Recorrido: minutos y suministros
            if (HayRecoleccion) instrumentos.Add((fila, retraso) => {
                RectTransform c;
                var suministros = S.ObjetosRecogidos().Where(kv => kv.Key.Tipo == TiposDeObjeto3D.Suministro).ToList();
                var cosas = S.HallazgosRecogidos().Count + S.ObjetosRecogidos().Count(kv => !kv.Key.Oculto);
                Instrumento(fila, "Recorrido", Paso.Recoleccion, S.RecoleccionHecha, Tema.cyan, "El recorrido",
                            S.RecoleccionHecha ? "Usaste " + S.R.MinutosDeRecoleccion + " minutos y trajiste " + cosas + " cosas." : "Todavía no hiciste el recorrido.", retraso, out c);
                if (!S.RecoleccionHecha) { SinDecidir(c); return; }
                TextoEnCaja.Crear(Ui, c, S.R.MinutosDeRecoleccion + " min", EstiloTexto.Titulo, Tema.warning, TextAlignmentOptions.Center, Tema.Px(30),
                                  new Vector2(0, 0.66f), Vector2.one, 2).fontStyle = FontStyles.Bold;
                var fichas = Ui.Fila(c, "Suministros", Tema.Espacio(1), 0, TextAnchor.MiddleCenter);
                fichas.anchorMin = new Vector2(0, 0); fichas.anchorMax = new Vector2(1, 0.66f); fichas.offsetMin = fichas.offsetMax = Vector2.zero;
                foreach (var kv in suministros.Take(3))
                    PiezasDeRecoleccion.Ficha(Ui, fichas, "suministros/" + kv.Key.Id, "+", "", kv.Value, Tema.Px(48), kv.Key.Nombre,
                                              kv.Key.Texto ?? "", Tooltip, Tema.warning);
                if (suministros.Count == 0) TextoEnCaja.Crear(Ui, fichas, cosas + " cosas", EstiloTexto.Cuerpo, Tema.inkMuted, TextAlignmentOptions.Center,
                                                              Tema.Px(16), Vector2.zero, Vector2.one);
            });

            // Backlog: la pila de historias
            if (S.SeCreaElBacklog) instrumentos.Add((fila, retraso) => {
                RectTransform c;
                var primera = _backlog.Count == 0 ? null : Perfil.Proyecto.Historia(_backlog[0]);
                Instrumento(fila, "Backlog", Paso.Backlog, _backlog.Count >= MinimoDeHistorias, Tema.cyan, "Tu product backlog",
                            _backlog.Count == 0 ? "Falta armar el backlog." : _backlog.Count + " historias; la primera, «" + (primera != null ? primera.Titulo : "…") + "»", retraso, out c);
                if (_backlog.Count == 0) { SinDecidir(c); return; }
                for (var i = 2; i >= 0; i--) {
                    var hoja2 = Ui.Nodo(c, "Historia " + i);
                    hoja2.anchorMin = new Vector2(0.18f + i * 0.04f, 0.34f - i * 0.0f); hoja2.anchorMax = new Vector2(0.82f + i * 0.04f, 0.98f - i * 0.0f);
                    hoja2.offsetMin = new Vector2(0, -i * Tema.Px(5)); hoja2.offsetMax = new Vector2(0, -i * Tema.Px(5));
                    hoja2.anchorMin = new Vector2(0.15f + i * 0.03f, 0.34f); hoja2.anchorMax = new Vector2(0.75f + i * 0.03f, 0.96f);
                    var im = hoja2.gameObject.AddComponent<Image>();
                    Ui.Fondo(im, NexusTheme.Alfa(i == 0 ? Tema.cyan : Tema.inkMuted, i == 0 ? 0.35f : 0.18f), NexusTheme.RadioSm);
                    im.raycastTarget = false;
                    Ui.Borde(hoja2, NexusTheme.Alfa(Tema.cyan, i == 0 ? 0.9f : 0.4f), NexusTheme.RadioSm, 1.5f);
                }
                TextoEnCaja.Crear(Ui, c, _backlog.Count.ToString(), EstiloTexto.Titulo, Tema.ink, TextAlignmentOptions.Center, Tema.Px(36),
                                  new Vector2(0.15f, 0.40f), new Vector2(0.75f, 0.92f), 2).fontStyle = FontStyles.Bold;
                TextoEnCaja.Crear(Ui, c, _backlog.Count == 1 ? "HISTORIA" : "HISTORIAS", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center,
                                  Tema.Px(12), Vector2.zero, new Vector2(1, 0.30f), 2);
            });

            // reparto: las cuatro primeras arriba, el resto debajo
            for (var i = 0; i < instrumentos.Count; i++) instrumentos[i](filas[i < 4 ? 0 : 1], 0.07f * i);
            if (instrumentos.Count <= 4) UnityEngine.Object.Destroy(filas[1].gameObject);

            // ---- la palanca
            var acciones = Ui.Fila(hoja.Raiz, "Lanzar", Tema.Espacio(4), 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(acciones, flexAncho: 1);
            if (!listo) {
                var falta = Ui.Texto(acciones, "Faltan " + (pasos.Count - hechos) + (pasos.Count - hechos == 1 ? " decisión" : " decisiones") + ": las que laten en ámbar.",
                                     EstiloTexto.Cuerpo, Tema.warning);
                TextoEnCaja.Ajustar(falta, Tema.Px(16));
                UiKit.Tamano(falta, ancho: Tema.Px(420), alto: Tema.Px(30));
            }
            var lanzar = Ui.Boton(acciones, "▶  Lanzar el nivel", Lanzar, VarianteBoton.Primario);
            UiKit.Tamano(lanzar, ancho: Tema.Px(340));
            GuiaView.Registrar("fase1.confirmar", lanzar);
            lanzar.interactable = listo;
        }

        /// <summary>Un instrumento: tarjeta de cristal con su titulo, su sello de hecho/falta y su «porque» en el tooltip.</summary>
        private void Instrumento(RectTransform fila, string titulo, Paso paso, bool completo, Color acento, string tipTitulo, string tipTexto,
                                 float retraso, out RectTransform contenido) {
            var color = completo ? acento : Tema.warning;
            var rt = Ui.Nodo(fila, "Instrumento " + titulo);
            var le = UiKit.Tamano(rt, flexAncho: 1, flexAlto: 1);
            le.minWidth = Tema.Px(140);
            var fondo = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.7f), NexusTheme.RadioLg);
            fondo.raycastTarget = true;
            var halo = Ui.Halo(rt, NexusTheme.Alfa(color, 0), NexusTheme.RadioLg);
            var borde = Ui.Borde(rt, completo ? NexusTheme.Alfa(acento, 0.75f) : color, NexusTheme.RadioLg, 1.5f);
            if (!completo) {
                var latido = rt.gameObject.AddComponent<LatidoDeBorde>();
                latido.Borde = borde; latido.Halo = halo; latido.Color = Tema.warning;
            }
            TextoEnCaja.Crear(Ui, rt, titulo.ToUpperInvariant(), EstiloTexto.Leyenda, color, TextAlignmentOptions.MidlineLeft, Tema.Px(12),
                              new Vector2(0.06f, 0.88f), new Vector2(0.74f, 0.99f), 0);
            // el sello: √ si esta hecho, ? si falta
            var sello = Ui.Nodo(rt, "Sello");
            sello.anchorMin = sello.anchorMax = new Vector2(1, 1);
            sello.pivot = new Vector2(1, 1);
            sello.sizeDelta = new Vector2(Tema.Px(24), Tema.Px(24));
            sello.anchoredPosition = new Vector2(-Tema.Px(8), -Tema.Px(8));
            var si = sello.gameObject.AddComponent<Image>();
            si.sprite = Ui.SpriteCircular(); si.color = NexusTheme.Alfa(color, 0.2f); si.raycastTarget = false;
            Ui.Borde(sello, color, Tema.Px(12) / Tema.escala, 1.5f);
            // «OK»: la fuente de los titulos no dibuja el simbolo de «hecho»
            TextoEnCaja.Crear(Ui, sello, completo ? "OK" : "?", EstiloTexto.Encabezado, color, TextAlignmentOptions.Center, Tema.Px(completo ? 11 : 16),
                              Vector2.zero, Vector2.one, 1).fontStyle = FontStyles.Bold;

            contenido = Ui.Nodo(rt, "Contenido");
            contenido.anchorMin = new Vector2(0.04f, 0.04f); contenido.anchorMax = new Vector2(0.96f, 0.86f);
            contenido.offsetMin = contenido.offsetMax = Vector2.zero;

            var boton = rt.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            boton.transition = Selectable.Transition.None;
            boton.navigation = new Navigation { mode = Navigation.Mode.None };
            boton.onClick.AddListener(() => IrA(paso));
            var r = rt.gameObject.AddComponent<RealceDeCalidad>();
            r.Fondo = fondo; r.Normal = NexusTheme.Alfa(Tema.bg950, 0.7f); r.Resaltado = NexusTheme.Alfa(color, 0.18f); r.Escala = 1.03f;
            var texto = (tipTexto ?? "") + "\n\nPulsa para cambiarlo.";
            r.Entra = () => Tooltip().Mostrar(tipTitulo, texto, color);
            r.Sale = () => Tooltip().Ocultar();
            Aparecer.En(rt, retraso, false);
        }

        /// <summary>Lo que aun no se decidio: una interrogacion grande.</summary>
        private void SinDecidir(RectTransform c) {
            TextoEnCaja.Crear(Ui, c, "?", EstiloTexto.Titulo, NexusTheme.Alfa(Tema.warning, 0.8f), TextAlignmentOptions.Center, Tema.Px(64),
                              Vector2.zero, Vector2.one, 2).fontStyle = FontStyles.Bold;
        }

        // ==================================================================== lanzar, con cuenta atras

        private void Lanzar() {
            if (_lanzando) return;
            _lanzando = true;
            StartCoroutine(CuentaAtras());
        }

        private IEnumerator CuentaAtras() {
            var capa = Ui.Nodo(Raiz, "Cuenta atras");
            UiKit.Rellenar(capa);
            capa.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var velo = capa.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(Tema.bg950, 0.8f);   // tambien tapa los clics: ya no hay vuelta atras
            var texto = TextoEnCaja.Crear(Ui, capa, "3", EstiloTexto.Titulo, Tema.cyan, TextAlignmentOptions.Center, Tema.Px(200),
                                          Vector2.zero, Vector2.one);
            texto.fontStyle = FontStyles.Bold;
            capa.SetAsLastSibling();
            foreach (var paso in new[] { "3", "2", "1", "¡DESPEGUE!" }) {
                texto.text = paso;
                texto.color = paso == "¡DESPEGUE!" ? Tema.success : Tema.cyan;
                texto.enableAutoSizing = true;
                var duracion = paso == "¡DESPEGUE!" ? 0.6f : 0.7f;
                for (var t = 0f; t < duracion; t += Mathf.Min(Time.unscaledDeltaTime, 0.1f)) {
                    var x = t / duracion;
                    texto.rectTransform.localScale = Vector3.one * (1f + 0.5f * (1 - x) * (1 - x));
                    texto.alpha = 1 - Mathf.Clamp01((x - 0.7f) / 0.3f);
                    yield return null;
                }
            }
            UnityEngine.Object.Destroy(capa.gameObject);
            _lanzando = false;
            Confirmar();
        }
    }
}
