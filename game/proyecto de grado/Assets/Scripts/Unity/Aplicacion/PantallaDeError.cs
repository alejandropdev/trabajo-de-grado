using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// INV-5 hecho pantalla: si el contenido no valida, el juego no arranca a medias, y dice exactamente por
    /// que. Cada linea es un mensaje del SchemaValidator, que nombra el archivo y el campo culpable.
    /// </summary>
    public sealed class PantallaDeError : Pantalla {
        public IReadOnlyList<string> Errores = new List<string>();

        public override bool PuedeVolver { get { return false; } }

        private RectTransform _lista;
        private TMP_Text _resumen;

        protected override void Construir() {
            var columna = UiKit.Rellenar(Ui.Columna(Raiz, "Error", relleno: Tema.margen * 2), 0);

            Ui.Texto(columna, "El contenido del juego tiene errores", EstiloTexto.Titulo, Tema.mostaza);
            _resumen = Ui.Texto(columna, "", EstiloTexto.Cuerpo);
            Ui.Texto(columna, "Contenido: " + RutasDeGuardado.Contenido, EstiloTexto.Pequeno);
            Ui.Separador(columna);

            var scroll = Ui.Desplazable(columna, out _lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            var botones = Ui.Fila(columna);
            Ui.Boton(botones, "Recargar el contenido", Reintentar, VarianteBoton.Primario);
            Ui.Boton(botones, "Salir", Application.Quit, VarianteBoton.Fantasma);
        }

        public override void Repintar() {
            _resumen.text = Errores.Count == 1
                ? "Hay 1 problema. Corrígelo en el archivo y pulsa «Recargar»: el juego no arranca a medias."
                : $"Hay {Errores.Count} problemas. Corrígelos en los archivos y pulsa «Recargar»: el juego no arranca a medias.";

            UiKit.Vaciar(_lista);
            foreach (var error in Errores) Ui.Texto(_lista, "• " + error, EstiloTexto.Mono);
        }

        private void Reintentar() {
            if (App.RecargarCatalogo()) {
                App.MostrarInicio();
                return;
            }
            Errores = App.ErroresDeCarga;
            Repintar();
        }
    }
}
