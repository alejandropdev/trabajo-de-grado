using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Fase1;
using Nexus.Core.Tablero;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La Recoleccion de la Fase 1 rehecha (2026-11-08): casi sin texto, ilustrada y sin scroll.
    ///   · ANTES del recorrido: el mapa del edificio (el mismo del minimapa del 3D) ocupando casi todo, con una luz que lo
    ///     recorre; al lado, las teclas dibujadas (se pulsan solas) y lo que se puede encontrar, cada cosa con su imagen
    ///     y su tooltip; y el boton grande para empezar. Sin el 3D en la build, el mismo mapa sellado «SIMULADO» con las
    ///     zonas como chips, y los tres botones de la simulacion.
    ///   · DESPUES: el equipo como tarjetas con su retrato (se voltean para ver el perfil; si sobran hojas de vida, se
    ///     eligen pulsandolas) y una franja con lo demas que se trajo, cada cosa con su imagen y su tooltip.
    /// </summary>
    public sealed partial class PantallaDeFase1 {
        private TooltipDeCalidad Tooltip() { return TooltipDeCalidad.Obtener(Ui, Raiz); }

        /// <summary>Como acabo el recorrido recien hecho (null = no se sabe: se volvio a la pantalla mas tarde).</summary>
        private bool? _seAcaboElTiempo;

        private void Recoleccion() {
            var modulo = AppRoot.ModuloDeRecoleccion;
            if (!S.RecoleccionHecha) RecoleccionAntes(modulo);
            else RecoleccionDespues();
        }

        // ==================================================================== antes del recorrido

        private void RecoleccionAntes(IModuloDeRecoleccion modulo) {
            var cfg = S.Recoleccion;
            var hoja = LienzoSinScroll("Recoleccion · antes");
            Tooltip().OcultarYa();

            // ---- cabecera: el titulo y el reloj
            var cabecera = hoja.Fila();
            var titulos = Ui.Columna(cabecera, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            var etiqueta = Ui.Texto(titulos, modulo != null ? "Antes de planificar · recorre el edificio" : "Antes de planificar · recorre el plano",
                                    EstiloTexto.Leyenda, Tema.cyan);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Texto(titulos, "Recolección", EstiloTexto.Titulo);
            var reloj = Ui.Columna(cabecera, "Reloj", 0, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(reloj, ancho: Tema.Px(150));
            var cifra = Ui.Texto(reloj, modulo != null ? "5:00" : cfg.MinutosDisponibles.ToString(), EstiloTexto.Titulo, Tema.warning, TextAlignmentOptions.Center);
            cifra.fontSize = Tema.Px(38);
            cifra.gameObject.AddComponent<ReboteDeCalidad>().Lanzar();
            Ui.Texto(reloj, "MINUTOS", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);

            // ---- el cuerpo: el mapa (flexible) y la columna de teclas y objetos
            var cuerpo = Ui.Fila(hoja.Raiz, "Cuerpo", Tema.Espacio(6), 0, TextAnchor.UpperLeft);
            var leCuerpo = UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
            leCuerpo.preferredHeight = Tema.Px(400); leCuerpo.minHeight = Tema.Px(280);
            cuerpo.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            Mapa(cuerpo, cfg, modulo == null);
            Leyenda(cuerpo, modulo != null);

            // ---- el boton para salir
            var acciones = Ui.Fila(hoja.Raiz, "Empezar", Tema.Espacio(4), 0, TextAnchor.MiddleCenter);
            GuiaView.Registrar("fase1.simular", acciones);
            if (modulo != null) {
                var b = Ui.Boton(acciones, "▶  Empezar recorrido", () => EmpezarRecoleccion(modulo), VarianteBoton.Primario);
                UiKit.Tamano(b, ancho: Tema.Px(320));
            } else {
                Ui.Texto(acciones, "¿Cuánto lo recorres?", EstiloTexto.Pequeno, Tema.inkMuted);
                Ui.Boton(acciones, "Rápido", () => Simular(IntensidadesDeSimulacion.Rapida));
                Ui.Boton(acciones, "Normal", () => Simular(IntensidadesDeSimulacion.Normal), VarianteBoton.Primario);
                Ui.Boton(acciones, "A fondo", () => Simular(IntensidadesDeSimulacion.AFondo));
            }
        }

        /// <summary>El mapa del edificio, encajado en su hueco sin deformarse, con su luz de escaner.</summary>
        private void Mapa(RectTransform padre, RecoleccionConfig cfg, bool simulado) {
            var hueco = Ui.Nodo(padre, "Mapa");
            UiKit.Tamano(hueco, flexAncho: 1, flexAlto: 1);
            var fondo = hueco.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.6f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            Ui.Borde(hueco, NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg, 1.5f);
            var visor = Ui.Nodo(hueco, "Visor");
            UiKit.Rellenar(visor, Tema.Px(8));
            visor.gameObject.AddComponent<RectMask2D>();

            var textura = Resources.Load<Texture2D>("Mapas/Mapa3D");
            if (textura != null) {
                var img = Ui.Nodo(visor, "Edificio");
                img.anchorMin = img.anchorMax = new Vector2(0.5f, 0.5f);
                var ajuste = img.gameObject.AddComponent<AspectRatioFitter>();
                ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                ajuste.aspectRatio = (float)textura.width / textura.height;
                var ri = img.gameObject.AddComponent<RawImage>();
                ri.texture = textura;
                ri.raycastTarget = false;
                Aparecer.En(img, 0.05f, false);
            } else {
                TextoEnCaja.Crear(Ui, visor, "EL EDIFICIO", EstiloTexto.Titulo, Tema.inkFaint, TextAlignmentOptions.Center, Tema.Px(30),
                                  Vector2.zero, Vector2.one);
            }
            PiezasDeRecoleccion.Barrido(Ui, visor);

            if (!simulado) return;
            // Sin el 3D: el sello y las zonas del plano como chips (su descripcion, al pasar el raton).
            var sello = Ui.Nodo(hueco, "Simulado");
            sello.anchorMin = sello.anchorMax = new Vector2(1, 1);
            sello.pivot = new Vector2(1, 1);
            sello.anchoredPosition = new Vector2(-Tema.Px(14), -Tema.Px(14));
            sello.sizeDelta = new Vector2(Tema.Px(120), Tema.Px(28));
            sello.localEulerAngles = new Vector3(0, 0, -6);
            var fs = sello.gameObject.AddComponent<Image>();
            Ui.Fondo(fs, NexusTheme.Alfa(Tema.bg950, 0.9f), 4);
            Ui.Borde(sello, Tema.warning, 4, 2);
            TextoEnCaja.Crear(Ui, sello, "SIMULADO", EstiloTexto.Leyenda, Tema.warning, TextAlignmentOptions.Center, Tema.Px(13),
                              Vector2.zero, Vector2.one, 2).fontStyle = FontStyles.Bold;
            var zonas = Ui.Fila(hueco, "Zonas", Tema.Espacio(2), 0, TextAnchor.MiddleCenter);
            zonas.anchorMin = new Vector2(0, 0); zonas.anchorMax = new Vector2(1, 0);
            zonas.pivot = new Vector2(0.5f, 0);
            zonas.offsetMin = new Vector2(Tema.Px(12), Tema.Px(12)); zonas.offsetMax = new Vector2(-Tema.Px(12), Tema.Px(12) + Tema.Px(34));
            zonas.gameObject.AddComponent<ListaDeElementos>();
            foreach (var z in cfg.Zonas) {
                var zona = z;
                var chip = Ui.Nodo(zonas, "Zona " + z.Id);
                UiKit.Tamano(chip, alto: Tema.Px(34), flexAncho: 1);
                var fc = chip.gameObject.AddComponent<Image>();
                Ui.Fondo(fc, NexusTheme.Alfa(Tema.bg950, 0.88f), 17);
                Ui.Borde(chip, NexusTheme.Alfa(Tema.cyan, 0.7f), 17, 1.5f);
                TextoEnCaja.Crear(Ui, chip, z.Id + " · " + z.Nombre, EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.Center, Tema.Px(13),
                                  Vector2.zero, Vector2.one, 6);
                var r = chip.gameObject.AddComponent<RealceDeCalidad>();
                r.Fondo = fc; r.Normal = NexusTheme.Alfa(Tema.bg950, 0.88f); r.Resaltado = NexusTheme.Alfa(Tema.cyan, 0.25f); r.Escala = 1.05f;
                var hallazgos = cfg.Hallazgos.Count(h => h.Zona == z.Id);
                r.Entra = () => Tooltip().Mostrar(zona.Id + " · " + zona.Nombre,
                                                  zona.Descripcion + "\n" + zona.MinutosDeVisita + " min · " + (hallazgos == 1 ? "1 cosa por encontrar" : hallazgos + " cosas por encontrar"),
                                                  Tema.cyan);
                r.Sale = () => Tooltip().Ocultar();
            }
        }

        /// <summary>La columna de la derecha: como se mueve uno y que se puede encontrar.</summary>
        private void Leyenda(RectTransform padre, bool en3D) {
            var col = Ui.Columna(padre, "Leyenda", Tema.Espacio(3));
            UiKit.Tamano(col, ancho: Tema.Px(360));
            var fondo = col.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.5f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            col.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 14, 14);

            if (en3D) {
                Ui.Texto(col, "Muévete", EstiloTexto.Subtitulo);
                var secuencia = col.gameObject.AddComponent<SecuenciaDeTeclas>();
                var lado = Tema.Px(42);
                var fila1 = Ui.Fila(col, "W", 0, 0, TextAnchor.MiddleCenter);
                secuencia.Teclas.Add(PiezasDeRecoleccion.Tecla(Ui, fila1, "W", lado, lado));
                var fila2 = Ui.Fila(col, "ASD", Tema.Espacio(1), 0, TextAnchor.MiddleCenter);
                foreach (var k in new[] { "A", "S", "D" }) secuencia.Teclas.Add(PiezasDeRecoleccion.Tecla(Ui, fila2, k, lado, lado));
                var otras = Ui.Fila(col, "Otras", Tema.Espacio(3), 0, TextAnchor.UpperCenter);
                foreach (var par in new[] { new[] { "Ratón", "mirar" }, new[] { "E", "inventario" }, new[] { "M", "mapa" }, new[] { "Esc", "pausa" } }) {
                    var c = Ui.Columna(otras, par[0], 2, 0, TextAnchor.UpperCenter);
                    UiKit.Tamano(c, ancho: Tema.Px(68));   // «inventario» cabe debajo de su tecla
                    var fila = Ui.Fila(c, "Tecla", 0, 0, TextAnchor.MiddleCenter);
                    PiezasDeRecoleccion.Tecla(Ui, fila, par[0], par[0].Length > 1 ? Tema.Px(64) : lado, lado);
                    var que = Ui.Texto(c, par[1], EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);
                    TextoEnCaja.Ajustar(que, Tema.Px(11));
                    UiKit.Tamano(que, alto: Tema.Px(16));
                }
            } else {
                Ui.Texto(col, "Cómo funciona", EstiloTexto.Subtitulo);
                Ui.Texto(col, "Cuanto más a fondo, más encuentras… y más minutos gastas.", EstiloTexto.Pequeno, Tema.inkMuted);
            }

            Ui.Resorte(col);
            Ui.Texto(col, "Puedes encontrar", EstiloTexto.Subtitulo);
            // Dos filas de tres: las personas y lo que se guarda para el diario, arriba; lo que se gasta despues, abajo.
            var l = Tema.Px(60);
            var arriba = Ui.Fila(col, "Objetos 1", Tema.Espacio(2), 0, TextAnchor.UpperCenter);
            arriba.gameObject.AddComponent<ListaDeElementos>();
            PiezasDeRecoleccion.Ficha(Ui, arriba, null, "CV", "Hojas de vida", 0, l, "Hojas de vida",
                                      "Cada hoja de vida es alguien que puede entrar en tu equipo. Sin ninguna, trabajas solo.", Tooltip, Tema.cyan);
            PiezasDeRecoleccion.Ficha(Ui, arriba, null, "DIARIO", "Coleccionables", 0, l, "Coleccionables",
                                      "Cartas, documentos y otras piezas: van a tu Diario de campo y se quedan contigo en todas las partidas.", Tooltip, Tema.violet);
            PiezasDeRecoleccion.Ficha(Ui, arriba, null, "ID", "Acreditaciones", 0, l, "Acreditaciones",
                                      "Tarjetas de acceso: abren puertas cerradas del edificio mientras lo recorres.", Tooltip, Tema.success);
            var abajo = Ui.Fila(col, "Objetos 2", Tema.Espacio(2), 0, TextAnchor.UpperCenter);
            abajo.gameObject.AddComponent<ListaDeElementos>();
            PiezasDeRecoleccion.Ficha(Ui, abajo, "suministros/consumable_pizza", "P", "Pizza", 0, l, "Pizza",
                                      "Sube la moral del equipo durante el desarrollo.", Tooltip, Tema.warning);
            PiezasDeRecoleccion.Ficha(Ui, abajo, "suministros/consumable_coffee", "C", "Café", 0, l, "Café",
                                      "Ese día todo el equipo rinde más.", Tooltip, Tema.warning);
            PiezasDeRecoleccion.Ficha(Ui, abajo, "suministros/consumable_energy_drink", "G", "Guepardex", 0, l, "Guepardex",
                                      "Le quita el cansancio a una persona… si no abusa.", Tooltip, Tema.warning);
        }

        // ==================================================================== despues del recorrido

        private void RecoleccionDespues() {
            var hoja = LienzoSinScroll("Recoleccion · despues");
            Tooltip().OcultarYa();
            var elegir = S.HayQueElegirEquipo && !S.Fase1Cerrada;
            var max = S.MaxEquipo;

            // ---- quienes: las hojas de vida (o el equipo asignado)
            var personas = new List<KeyValuePair<MiembroDelEquipo, string>>();
            if (S.EquipoDesdeRecoleccion) {
                foreach (var o in elegir ? S.HojasRecogidas() : S.HojasDelEquipo())
                    if (o.Miembro != null) personas.Add(new KeyValuePair<MiembroDelEquipo, string>(o.Miembro, o.Texto));
            } else {
                foreach (var m in S.EquipoDelProyecto()) personas.Add(new KeyValuePair<MiembroDelEquipo, string>(m, null));
            }
            var solo = personas.Count == 0;

            // ---- cabecera: titulo, contador y el resumen
            var cabecera = hoja.Fila();
            var titulos = Ui.Columna(cabecera, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            // Lo que decia el resumen al volver (ya no se abre solo): como acabo y cuanto tiempo se uso.
            var acabo = _seAcaboElTiempo == true ? "Se acabó el tiempo · " : _seAcaboElTiempo == false ? "Dejaste el recorrido · " : "";
            var etiqueta = Ui.Texto(titulos, "Recolección · " + acabo + "usaste " + S.R.MinutosDeRecoleccion + " de " + S.Recoleccion.MinutosDisponibles + " minutos",
                                    EstiloTexto.Leyenda, Tema.cyan);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Texto(titulos, elegir ? "Elige tu equipo" : "Tu equipo", EstiloTexto.Titulo);
            const string ayudaElegir = "Pulsa una tarjeta para meterla o sacarla. Deja el ratón encima para ver su perfil.";
            var avisoTexto = Ui.Texto(titulos, elegir ? ayudaElegir : "Deja el ratón encima de una tarjeta para ver su perfil.", EstiloTexto.Pequeno, Tema.inkMuted);

            var cuenta = Ui.Columna(cabecera, "Cuenta", 0, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(cuenta, ancho: Tema.Px(170));
            var dentro = new HashSet<string>(S.EquipoDelProyecto().Select(m => m.Id));
            var contador = Ui.Texto(cuenta, "", EstiloTexto.Titulo, Tema.cyan, TextAlignmentOptions.Center);
            contador.fontSize = Tema.Px(34);
            contador.textWrappingMode = TextWrappingModes.NoWrap;
            var rebote = contador.gameObject.AddComponent<ReboteDeCalidad>();
            Ui.Texto(cuenta, elegir ? "PLAZAS" : solo ? "SIN EQUIPO" : "PERSONAS", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);
            System.Action contar = () => {
                contador.text = elegir ? dentro.Count + " de " + max : solo ? "0" : personas.Count.ToString();
                contador.color = elegir && dentro.Count == max ? Tema.success : Tema.cyan;
            };
            contar();
            if (S.ObjetosRecogidos().Count > 0)
                Ui.Boton(cabecera, "Ver el resumen", () => App.Router.Apilar<PantallaResumenDeRecoleccion>(), VarianteBoton.Fantasma);

            // ---- las tarjetas
            var fila = Ui.Fila(hoja.Raiz, "Equipo", Tema.Espacio(4), 0, TextAnchor.MiddleCenter);
            var le = UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1);
            le.preferredHeight = Tema.Px(300); le.minHeight = Tema.Px(240);
            fila.gameObject.AddComponent<ListaDeElementos>();
            GuiaView.Registrar("fase1.equipo", fila);
            var tarjetas = new Dictionary<string, TarjetaDeMiembro>();
            if (solo) {
                var t = TarjetaDeMiembro.Crear(Ui, fila, null, null, null, true, null, 0);
                UiKit.Tamano(t, ancho: Tema.Px(200), alto: Tema.Px(290));
            }
            for (var i = 0; i < personas.Count; i++) {
                var m = personas[i].Key;
                var id = m.Id;
                var tarjeta = TarjetaDeMiembro.Crear(Ui, fila, m, personas[i].Value, Nexus.Core.Minijuegos.AndamiajeDelReto.EnQueAyuda(m),
                                                     !elegir || dentro.Contains(id), () => {
                    var nuevo = new HashSet<string>(dentro);
                    if (!nuevo.Remove(id)) nuevo.Add(id);
                    var motivo = S.PorQueNoSePuedeElegirEquipo(nuevo);
                    if (motivo != null) {
                        avisoTexto.text = Nexus.Core.Narrativa.Glifos.Seguro(nuevo.Count > max ? "Ya tienes " + max + ": saca a alguien antes de meter a otra persona." : motivo);
                        avisoTexto.color = Tema.warning;
                        return false;
                    }
                    S.ElegirEquipo(nuevo.ToList());
                    dentro.Clear();
                    foreach (var x in nuevo) dentro.Add(x);
                    foreach (var kv in tarjetas) kv.Value.PonerDentro(dentro.Contains(kv.Key));
                    avisoTexto.text = Nexus.Core.Narrativa.Glifos.Seguro(ayudaElegir);
                    avisoTexto.color = Tema.inkMuted;
                    contar();
                    rebote.Lanzar();
                    GuardarBorrador();
                    PintarNavegacion();
                    return true;
                }, 0.07f * i);
                tarjeta.Seleccionable = elegir;
                UiKit.Tamano(tarjeta, ancho: Tema.Px(190), alto: Tema.Px(290));
                tarjetas[id] = tarjeta;
            }

            // ---- la franja: lo demas que se trajo
            Encontrado(hoja.Raiz);
        }

        /// <summary>Lo que se trajo aparte del equipo: suministros, piezas del diario, notas y hallazgos, como fichas.</summary>
        /// <summary>Un grupo de la franja: todo lo de un mismo tipo en una ficha («Diario ×5»), con la lista en el tooltip.</summary>
        private sealed class GrupoTraido {
            public string Imagen, Glifo, Etiqueta, Titulo;
            public Color Acento;
            public int Cuantos;
            public readonly List<string> Lineas = new List<string>();
        }

        private void Encontrado(RectTransform padre) {
            // Un panel a todo lo ancho, con su titulo arriba y las fichas grandes y centradas (antes era una tira estrecha).
            var panel = Ui.Columna(padre, "Tambien trajiste", Tema.Espacio(2), 0, TextAnchor.UpperCenter);
            UiKit.Tamano(panel, flexAncho: 1);
            var fondo = panel.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.55f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            Ui.Borde(panel, NexusTheme.Alfa(Tema.cyan, 0.35f), NexusTheme.RadioLg, 1.5f);
            panel.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(18, 18, 12, 14);
            var rotulo = Ui.Texto(panel, "También trajiste", EstiloTexto.Subtitulo, Tema.cyan, TextAlignmentOptions.Center);
            rotulo.fontSize = Tema.Px(20);

            // Lo que se repite va junto: los suministros, cada uno con su imagen; el diario, las notas, las pistas…, en un grupo.
            var grupos = new List<GrupoTraido>();
            System.Func<string, string, string, string, Color, GrupoTraido> grupo = (clave, imagen, glifo, etiqueta, acento) => {
                var g = grupos.FirstOrDefault(x => x.Titulo == clave);
                if (g == null) { g = new GrupoTraido { Titulo = clave, Imagen = imagen, Glifo = glifo, Etiqueta = etiqueta, Acento = acento }; grupos.Add(g); }
                return g;
            };
            foreach (var kv in S.ObjetosRecogidos()) {
                var o = kv.Key;
                if (o.Oculto || o.Tipo == TiposDeObjeto3D.Candidato) continue;
                GrupoTraido g;
                switch (o.Tipo) {
                    case TiposDeObjeto3D.Suministro: g = grupo(o.Id, "suministros/" + o.Id, "+", o.Nombre, Tema.warning); break;
                    case TiposDeObjeto3D.Coleccionable: g = grupo("diario", null, "DIARIO", "Para tu diario", Tema.violet); break;
                    case TiposDeObjeto3D.Efecto: g = grupo("efecto", null, "+", "Para el proyecto", Tema.success); break;
                    default: g = grupo("nota", null, "NOTA", "Notas", Tema.inkMuted); break;
                }
                g.Cuantos += kv.Value;
                g.Lineas.Add(o.Tipo == TiposDeObjeto3D.Suministro ? (o.Texto ?? o.Nombre) : "· " + o.Nombre);
            }
            foreach (var h in S.HallazgosRecogidos()) {
                if (h.Tipo == TiposDeHallazgo.Personaje) continue;   // las personas ya estan en las tarjetas
                GrupoTraido g;
                switch (h.Tipo) {
                    case TiposDeHallazgo.Pista: g = grupo("pista", null, "PISTA", "Pistas", Tema.cyan); break;
                    case TiposDeHallazgo.Coleccionable: g = grupo("diario", null, "DIARIO", "Para tu diario", Tema.violet); break;
                    case TiposDeHallazgo.Riesgo: g = grupo("riesgo", null, "!", "Tropiezos", Tema.warning); break;
                    default: g = grupo("recurso", null, "+", "Para el equipo", Tema.success); break;
                }
                g.Cuantos++;
                g.Lineas.Add(Nexus.Core.Narrativa.Glifos.Seguro(DescribirHallazgo(h)));
            }

            var fila = Ui.Fila(panel, "Fichas", Tema.Espacio(6), 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(fila, flexAncho: 1);
            fila.gameObject.AddComponent<ListaDeElementos>();
            if (grupos.Count == 0) {
                Ui.Texto(fila, "Nada más esta vez.", EstiloTexto.Cuerpo, Tema.inkMuted, TextAlignmentOptions.Center);
                return;
            }
            var l = Tema.Px(78);
            foreach (var g in grupos) {
                var texto = g.Lineas.Count > 6 ? string.Join("\n", g.Lineas.Take(6)) + "\n… y " + (g.Lineas.Count - 6) + " más" : string.Join("\n", g.Lineas);
                PiezasDeRecoleccion.Ficha(Ui, fila, g.Imagen, g.Glifo, g.Etiqueta, g.Cuantos, l, g.Etiqueta, texto, Tooltip, g.Acento);
            }
        }
    }
}
