using System;
using System.Collections.Generic;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Base de los dialogos: oscurece lo de debajo (que sigue viendose, y no se puede pulsar) y pone un panel
    /// centrado. La pantalla de debajo no se oculta: un modal es una pregunta sobre ella, no otro sitio.
    /// </summary>
    public abstract class PantallaModal : Pantalla {
        public override bool EsModal { get { return true; } }

        protected Hoja Hoja { get; private set; }

        protected virtual float Ancho { get { return 760; } }

        protected override void Construir() {
            var velo = Raiz.gameObject.AddComponent<Image>();
            velo.color = new Color(0, 0, 0, 0.62f);   // raycastTarget: bloquea los clics de la pantalla de debajo

            var panel = Ui.PanelColumna(Raiz, "Dialogo", Tema.margen * 1.25f, Tema.espacio);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            var ajuste = panel.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            panel.sizeDelta = new Vector2(Ancho, 0);

            Hoja = new Hoja(Ui, panel);
            Rellenar();
        }

        protected abstract void Rellenar();
    }

    /// <summary>«¿Seguro?» antes de lo que no se deshace: borrar un perfil o una partida.</summary>
    public sealed class PantallaDeConfirmacion : PantallaModal {
        public string Titulo = "¿Seguro?";
        public string Texto;
        public string TextoSi = "Sí";
        public Action AlConfirmar;

        protected override void Rellenar() {
            Hoja.Subtitulo(Titulo, Tema.texto);
            if (!string.IsNullOrEmpty(Texto)) Hoja.Parrafo(Texto);
            Hoja.Espacio();
            var fila = Hoja.Fila();
            Ui.Boton(fila, TextoSi, () => { App.Router.Volver(); AlConfirmar?.Invoke(); }, VarianteBoton.Peligro);
            Ui.Boton(fila, "Cancelar", () => App.Router.Volver(), VarianteBoton.Fantasma);
        }
    }

    /// <summary>Un mensaje a pantalla completa con una sola salida: el final de la version jugable, un error de partida.</summary>
    public sealed class PantallaDeMensaje : Pantalla {
        public string Etiqueta;
        public string Titulo;
        public List<string> Parrafos = new List<string>();
        public string TextoBoton = "Volver al menú";
        public Action AlPulsar;

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            var centro = Ui.Columna(Raiz, "Centro", Tema.espacio, Tema.margen);
            centro.anchorMin = new Vector2(0.2f, 0.15f);
            centro.anchorMax = new Vector2(0.8f, 0.85f);
            centro.offsetMin = centro.offsetMax = Vector2.zero;
            ((VerticalLayoutGroup)centro.GetComponent<VerticalLayoutGroup>()).childAlignment = TextAnchor.MiddleLeft;

            var hoja = new Hoja(Ui, centro);
            if (!string.IsNullOrEmpty(Etiqueta)) hoja.Etiqueta(Etiqueta);
            hoja.Titulo(Titulo);
            foreach (var p in Parrafos) hoja.Parrafo(p);
            hoja.Accion(TextoBoton, () => (AlPulsar ?? App.MostrarInicio)());
        }
    }

    /// <summary>
    /// La pausa, sobre el dia. Mientras esta abierta el reloj no corre (Pausado), y desde aqui se puede leer el
    /// diario o guardar y salir: la partida se guarda tal cual, a mitad de dia (INV-7 garantiza que se recupera igual).
    /// </summary>
    public sealed class PantallaDePausa : PantallaModal {
        protected override float Ancho { get { return 560; } }

        protected override void Rellenar() {
            App.Runner.Pausado = true;
            Hoja.Titulo("Pausa");
            Hoja.Nota($"Día {App.Sesion.R.DiaActual} · {App.Sesion.HoraActual}. El reloj está parado.");
            Hoja.Espacio();
            var col = Ui.Columna(Hoja.Raiz, espacio: Tema.espacio);
            Ui.Boton(col, "Seguir jugando", Seguir, VarianteBoton.Primario);
            Ui.Boton(col, "Diario de campo", () => App.Router.Apilar<PantallaDelDiario>());
            Ui.Boton(col, "Guardar y volver al menú", Salir);
        }

        private void Seguir() {
            App.Runner.Pausado = false;
            App.Router.Volver();
        }

        private void Salir() {
            App.CerrarPartida();
            App.MostrarInicio();
        }

        private void OnDestroy() {
            if (App != null && App.Runner != null) App.Runner.Pausado = false;
        }
    }
}
