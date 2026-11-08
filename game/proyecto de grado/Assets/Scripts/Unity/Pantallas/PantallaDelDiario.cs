using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El Diario de Campo: todo lo que el estudiante ha encontrado, en todas sus partidas. Cinco series:
    /// cartas de desarrollador (leyes, desastres, practicas, oficio), fragmentos del pendrive, codigos de
    /// terminal, startups que fracasaron y advertencias. Lo no encontrado se ve como hueco: saber que falta
    /// algo es parte de lo que empuja a explorar.
    ///
    /// Las cartas hablan de hechos y personas reales: llevan su fuente al reverso, y nada de lo que dicen se
    /// pone en boca de nadie.
    /// </summary>
    public sealed class PantallaDelDiario : Pantalla {
        public Action AlCerrar;

        private string _serie = SeriesDeColeccionables.Carta;
        private RectTransform _lista;
        private readonly Dictionary<string, Button> _pestanas = new Dictionary<string, Button>();

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 0.75f));
            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "DIARIO DE CAMPO · " + (App.PerfilActivo != null ? App.PerfilActivo.nombreEstudiante.ToUpperInvariant() : ""),
                     EstiloTexto.Leyenda, Tema.cyan);
            Ui.Texto(titulos, "Lo que has encontrado", EstiloTexto.Titulo);
            Ui.Boton(cabecera, "Cerrar", Cerrar, VarianteBoton.Primario);

            var pestanas = Ui.Fila(marco, "Series", 6);
            foreach (var serie in SeriesDeColeccionables.Todas) {
                var s = serie;
                _pestanas[s] = Ui.Boton(pestanas, NombreDeSerie(s, false), () => { _serie = s; Repintar(); });
            }

            var panel = Ui.PanelColumna(marco, "Coleccion", Tema.margen, Tema.espacio);
            UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            var scroll = Ui.Desplazable(panel, out _lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            GuiaView.Avisar(App, "diario");
        }

        private void Cerrar() { App.Router.Volver(); }

        // Tambien al cerrarlo con Escape: quien lo abrio (el dia, que lo pauso) tiene que enterarse.
        private void OnDestroy() { AlCerrar?.Invoke(); }

        public override void Repintar() {
            if (_lista == null) return;
            var encontrados = Encontrados(App);
            foreach (var kv in _pestanas) {
                var total = App.Catalogo.Coleccionables.Count(c => c.Serie == kv.Key);
                var tiene = App.Catalogo.Coleccionables.Count(c => c.Serie == kv.Key && encontrados.Contains(c.Id));
                kv.Value.GetComponentInChildren<TMP_Text>().text = $"{NombreDeSerie(kv.Key, false)}  {tiene}/{total}";
                Ui.Resaltar(kv.Value, kv.Key == _serie);
            }

            UiKit.Vaciar(_lista);
            var deLaSerie = App.Catalogo.Coleccionables.Where(c => c.Serie == _serie).OrderBy(c => c.Id).ToList();
            if (deLaSerie.Count == 0) {
                Ui.Texto(_lista, "Esta serie todavía no tiene piezas en esta versión.", EstiloTexto.Cuerpo, Tema.inkMuted);
                return;
            }
            // Una rejilla de miniaturas (2026-11-06): lo encontrado con su emblema o pictograma, y al pulsarlo se vuelve
            // a ver su escena; lo que falta, en silueta con su pista.
            var rejilla = Ui.Nodo(_lista, "Rejilla");
            var grid = rejilla.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(230, 230);
            grid.spacing = new Vector2(Tema.espacio, Tema.espacio);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            var ajuste = rejilla.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UiKit.Tamano(rejilla, flexAncho: 1);
            foreach (var col in deLaSerie) {
                if (encontrados.Contains(col.Id)) Miniatura(rejilla, col);
                else Hueco(rejilla, col);
            }
        }

        /// <summary>Lo encontrado: su dibujo, su titulo; al pulsarlo, su escena otra vez.</summary>
        private void Miniatura(RectTransform padre, Coleccionable col) {
            var color = col.Serie == SeriesDeColeccionables.Carta ? Hallazgos.ColorDePalo(Tema, col.Palo) : EscenaDeHallazgo.ColorDeSerie(Tema, col.Serie);
            var t = Ui.PanelColumna(padre, "Miniatura", Tema.Espacio(3), 6, Tema.surfaceRaised);
            Ui.Borde(t, NexusTheme.Alfa(color, 0.6f), NexusTheme.RadioMd, 1.5f);
            var lamina = new Lamina(Ui, t, 200, 130, "Dibujo");
            var tiza = new Tizador(lamina, Ui.FuenteTiza);
            string clave;
            Action<Tizador, Color> emblema;
            if (col.Serie == SeriesDeColeccionables.Carta && EmblemasDeColeccionables.DeCartas.TryGetValue(col.Id, out clave) &&
                DibujosDeHallazgos.Emblemas.TryGetValue(clave, out emblema))
                emblema(tiza.En(40, 4, 0.55f), color);
            else if (col.Serie == SeriesDeColeccionables.Advertencia) {
                KeyValuePair<string, string> v;
                Action<Tizador, Color> icono;
                if (EmblemasDeColeccionables.DeAdvertencias.TryGetValue(col.Id, out v) && DibujosDeHallazgos.IconosDeAdvertencia.TryGetValue(v.Value, out icono))
                    icono(tiza.En(50, 2, 1.05f), color);
            } else DibujosDeHallazgos.Pictograma(tiza.En(30, 10, 1.15f), col.Serie, color);
            var titulo = Ui.Texto(t, col.Titulo, EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.Center);
            titulo.maxVisibleLines = 2;
            titulo.overflowMode = TextOverflowModes.Ellipsis;
            var boton = t.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            var elegido = col;
            boton.onClick.AddListener(() => App.Router.Apilar<EscenaDeHallazgo>(p => { p.Coleccionable = elegido; p.Repeticion = true; }));
            t.gameObject.AddComponent<PiezaViva>().Escala = 1.05f;
        }

        private void Hueco(RectTransform padre, Coleccionable col) {
            var t = Ui.PanelColumna(padre, "Hueco", Tema.Espacio(3), 6, Tema.surfaceSunken);
            var lamina = new Lamina(Ui, t, 200, 110, "Silueta");
            DibujosDeHallazgos.Pictograma(new Tizador(lamina, Ui.FuenteTiza).En(40, 6, 1f), col.Serie, NexusTheme.Alfa(Tema.inkFaint, 0.5f));
            Ui.Texto(t, "???", EstiloTexto.Subtitulo, Tema.inkFaint, TextAlignmentOptions.Center);
            var pista = col.Aparicion != null && !string.IsNullOrEmpty(col.Aparicion.Pista) ? col.Aparicion.Pista : "Todavía no lo has encontrado.";
            var tp = Ui.Texto(t, pista, EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Center);
            tp.maxVisibleLines = 3;
            tp.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static HashSet<string> Encontrados(AppRoot app) {
            var set = new HashSet<string>();
            if (app.PerfilActivo != null && app.PerfilActivo.coleccionablesGlobales != null)
                foreach (var id in app.PerfilActivo.coleccionablesGlobales) set.Add(id);
            if (app.Sesion != null) foreach (var id in app.Sesion.R.ColeccionablesRecogidos) set.Add(id);
            return set;
        }

        public static string NombreDeSerie(string serie, bool singular) {
            switch (serie) {
                case SeriesDeColeccionables.Carta: return singular ? "una carta de desarrollador" : "Cartas";
                case SeriesDeColeccionables.Usb: return singular ? "un fragmento del pendrive" : "Pendrive";
                case SeriesDeColeccionables.Codigo: return singular ? "un código de terminal" : "Códigos";
                case SeriesDeColeccionables.Startup: return singular ? "una lápida de startup" : "Startups";
                case SeriesDeColeccionables.Advertencia: return singular ? "una advertencia" : "Advertencias";
                case SeriesDeColeccionables.Nota: return singular ? "una nota adhesiva" : "Notas";
                default: return singular ? "algo" : Textos.Humanizar(serie);
            }
        }
    }
}
