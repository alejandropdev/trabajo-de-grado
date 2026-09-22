using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Unity.Aplicacion;
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
            var titulos = Ui.Columna(cabecera, espacio: 0);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "DIARIO DE CAMPO · " + (App.PerfilActivo != null ? App.PerfilActivo.nombreEstudiante.ToUpperInvariant() : ""),
                     EstiloTexto.Pequeno, Tema.cian);
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
                Ui.Texto(_lista, "Esta serie todavía no tiene piezas en esta versión.", EstiloTexto.Cuerpo, Tema.textoTenue);
                return;
            }
            foreach (var col in deLaSerie) {
                if (encontrados.Contains(col.Id)) Tarjeta(Ui, _lista, col);
                else Hueco(col);
            }
        }

        private void Hueco(Coleccionable col) {
            var t = Ui.PanelColumna(_lista, "Hueco", Tema.espacio, 4, Tema.fondo);
            Ui.Texto(t, "???", EstiloTexto.Subtitulo, Tema.textoTenue);
            Ui.Texto(t, "Todavía no lo has encontrado.", EstiloTexto.Pequeno);
        }

        /// <summary>La cara de un coleccionable encontrado. La usan el diario y la pantalla de recogida.</summary>
        public static RectTransform Tarjeta(UiKit ui, Transform padre, Coleccionable col) {
            var tema = ui.Tema;
            var t = ui.Tarjeta(padre, NombreDeSerie(col.Serie, false) + (string.IsNullOrEmpty(col.Palo) ? "" : " · " + Textos.Humanizar(col.Palo)));
            ui.Texto(t, col.Titulo, EstiloTexto.Subtitulo, tema.texto);
            if (!string.IsNullOrEmpty(col.Nicho)) ui.Texto(t, "Nicho: " + col.Nicho, EstiloTexto.Pequeno, tema.cianClaro);
            if (!string.IsNullOrEmpty(col.Texto)) ui.Texto(t, col.Texto, EstiloTexto.Cuerpo);
            if (!string.IsNullOrEmpty(col.Causa)) ui.Texto(t, "Por qué fracasó: " + col.Causa, EstiloTexto.Cuerpo, tema.amarillo);
            if (!string.IsNullOrEmpty(col.Comando)) ui.Texto(t, "> " + col.Comando, EstiloTexto.Mono);
            if (!string.IsNullOrEmpty(col.Revela)) ui.Texto(t, col.Revela, EstiloTexto.Cuerpo, tema.cianClaro);
            if (!string.IsNullOrEmpty(col.PreguntaDeAplicacion)) ui.Texto(t, "Para pensar: " + col.PreguntaDeAplicacion, EstiloTexto.Pequeno, tema.texto);
            if (!string.IsNullOrEmpty(col.Fuente)) ui.Texto(t, "Fuente: " + col.Fuente, EstiloTexto.Pequeno);
            return t;
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
                default: return singular ? "algo" : Textos.Humanizar(serie);
            }
        }
    }

    /// <summary>Lo que se acaba de encontrar, en grande, antes de guardarlo en el diario. El reloj espera.</summary>
    public sealed class PantallaDeColeccionable : PantallaModal {
        public Coleccionable Coleccionable;
        public Action AlCerrar;

        public override bool PuedeVolver { get { return false; } }
        protected override float Ancho { get { return 900; } }

        protected override void Rellenar() {
            Hoja.Etiqueta("Encontraste " + PantallaDelDiario.NombreDeSerie(Coleccionable.Serie, true), Tema.mostaza);
            PantallaDelDiario.Tarjeta(Ui, Hoja.Raiz, Coleccionable);
            Hoja.Accion("Guardarlo en el diario", () => {
                App.Router.Volver();
                AlCerrar?.Invoke();
            });
        }
    }
}
