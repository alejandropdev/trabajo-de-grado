using System;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Un panel del dia: el mapa, el equipo, el trabajo de escritorio, los avisos, el monitoreo. Todo lo que antes
    /// estaba SIEMPRE a la vista en la pantalla del dia y la cargaba (feedback de la beta: «hay tantas cosas a la
    /// vez que no se le da a cada una la importancia que merece»). Ahora se abre desde el riel, encima del dia, y
    /// se cierra con su boton o con Escape.
    ///
    /// Es solo el marco. Lo que lleva dentro lo pinta quien lo abre (la pantalla del dia), que es quien tiene el
    /// estado; 'Clave' dice cuando hay que volver a pintarlo (cambio de zona, una ayuda gastada…).
    /// </summary>
    public sealed class PanelDelDia : PantallaModal {
        public string Etiqueta;
        public string Titulo;
        public float AnchoDelPanel = 1100;
        public Action<Hoja> Pintar;
        public Func<string> Clave;
        public Action AlCerrar;

        private Hoja _cuerpo;
        private string _clave;
        private bool _cerrado;

        protected override float Ancho { get { return AnchoDelPanel; } }

        protected override void Rellenar() {
            var cabecera = Hoja.Fila();
            var titulos = Ui.Columna(cabecera, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            if (!string.IsNullOrEmpty(Etiqueta)) Ui.Texto(titulos, Etiqueta.ToUpperInvariant(), EstiloTexto.Leyenda, Tema.cyan);
            Ui.Texto(titulos, Titulo ?? "", EstiloTexto.Subtitulo, Tema.ink);
            Ui.Boton(cabecera, "Cerrar", () => App.Router.Volver(), VarianteBoton.Fantasma);
            Hoja.Separador();

            _cuerpo = new Hoja(Ui, Ui.Columna(Hoja.Raiz, "Cuerpo", Tema.espacio));
            UiKit.Tamano(_cuerpo.Raiz, flexAncho: 1);
            Repintar();
        }

        public override void Repintar() {
            if (_cuerpo == null) return;
            _cuerpo.Vaciar();
            Pintar?.Invoke(_cuerpo);
            _clave = Clave?.Invoke();
        }

        private void Update() {
            if (Clave == null || _cuerpo == null) return;
            if (Clave() != _clave) Repintar();
        }

        /// <summary>Al cerrarse (o al destruirse la partida debajo): quien lo abrio suelta el reloj.</summary>
        public override void AlOcultar() {
            if (_cerrado) return;
            _cerrado = true;
            AlCerrar?.Invoke();
        }
    }
}
