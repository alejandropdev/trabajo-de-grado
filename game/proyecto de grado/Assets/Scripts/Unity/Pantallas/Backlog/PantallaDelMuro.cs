using System;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El muro del backlog a pantalla completa (2026-11-06): el marco. Sustituye al panel de tres pestañas (tablero,
    /// equipo, product backlog) que se abria con «Backlog». Arriba, «Volver (Esc)», el nombre a mano y las
    /// herramientas de la metodologia; en medio, lo que pinta quien lo abre (la pantalla del dia, que tiene el estado);
    /// por encima de todo, la capa donde vuelan las fichas. Como PanelDelDia: 'Clave' dice cuando repintar (nunca con
    /// una ficha en el aire), y AlCerrar / AlReabrir sueltan y vuelven a parar el reloj.
    /// </summary>
    public sealed class PantallaDelMuro : Pantalla {
        public string Etiqueta;
        public string Titulo;
        public Action<PantallaDelMuro> Pintar;
        public Func<string> Clave;
        public Action AlCerrar;
        public Action<PantallaDelMuro> AlReabrir;

        public override bool EsModal { get { return true; } }

        public RectTransform Cuerpo { get; private set; }
        public RectTransform Herramientas { get; private set; }
        public RectTransform Capa { get; private set; }
        public UiKit Kit { get { return Ui; } }
        /// <summary>Cuantas veces se ha pintado. La primera (0 mientras se pinta) todo entra animado; al repintar tras actuar, no.</summary>
        public int Pintadas { get; private set; }
        public bool PrimeraVez { get { return Pintadas == 0; } }
        public NexusTheme Colores { get { return Tema; } }

        private string _clave;
        private bool _cerrado;
        private TMP_Text _titulo;

        protected override void Construir() {
            var fondo = Raiz.gameObject.AddComponent<Image>();
            // Como los indicadores: la escena se ve detras, oscurecida, y los paneles flotan encima.
            fondo.color = NexusTheme.Alfa(Tema.bg950, 0.7f);
            // Un panel grande semitransparente detras de todo (2026-11-07): la escena se intuye, pero las fichas, cifras
            // y textos se leen sobre un fondo parejo, no sobre los detalles del dibujo.
            var lamina = Ui.Nodo(Raiz, "Fondo del backlog");
            lamina.anchorMin = Vector2.zero;
            lamina.anchorMax = Vector2.one;
            lamina.offsetMin = new Vector2(12, 8);
            lamina.offsetMax = new Vector2(-12, -6);
            var imgLamina = lamina.gameObject.AddComponent<Image>();
            Ui.Fondo(imgLamina, NexusTheme.Alfa(Tema.bg900, 0.86f), NexusTheme.RadioLg);
            imgLamina.raycastTarget = false;
            Ui.Borde(lamina, NexusTheme.Alfa(Tema.cyan, 0.35f), NexusTheme.RadioLg, 1.5f);
            var halo = Ui.Nodo(lamina, "Halo");
            halo.sizeDelta = new Vector2(1800, 1100);
            var imgHalo = halo.gameObject.AddComponent<Image>();
            imgHalo.sprite = PlanoIlustrado.Brillo();
            imgHalo.color = NexusTheme.Alfa(Tema.cyan, 0.05f);
            imgHalo.raycastTarget = false;
            // Una raya bajo la cabecera, como separador.
            var raya = Ui.Nodo(lamina, "Raya");
            raya.anchorMin = new Vector2(0, 1);
            raya.anchorMax = new Vector2(1, 1);
            raya.pivot = new Vector2(0.5f, 1);
            raya.sizeDelta = new Vector2(-40, 1);
            raya.anchoredPosition = new Vector2(0, -90);
            var imgRaya = raya.gameObject.AddComponent<Image>();
            imgRaya.color = Tema.lineStrong;
            imgRaya.raycastTarget = false;

            var cabecera = Ui.Fila(Raiz, "Cabecera", Tema.espacio * 1.5f, 0, TextAnchor.MiddleLeft);
            cabecera.anchorMin = new Vector2(0, 1);
            cabecera.anchorMax = new Vector2(1, 1);
            cabecera.pivot = new Vector2(0.5f, 1);
            cabecera.offsetMin = new Vector2(28, -92);
            cabecera.offsetMax = new Vector2(-28, -14);
            Ui.Boton(cabecera, "Volver (Esc)", () => App.Router.Volver(), VarianteBoton.Fantasma);
            var titulos = Ui.Columna(cabecera, "Titulos", 0);
            if (!string.IsNullOrEmpty(Etiqueta)) {
                var e = Ui.Texto(titulos, Etiqueta.ToUpperInvariant(), EstiloTexto.Leyenda, Tema.cyan);
                e.characterSpacing = 4;
            }
            _titulo = Ui.Texto(titulos, (Titulo ?? "").ToUpperInvariant(), EstiloTexto.Encabezado, Tema.cyan);
            _titulo.font = Tema.FuenteDisplay;
            Herramientas = Ui.Fila(cabecera, "Herramientas", Tema.espacio, 0, TextAnchor.MiddleRight);
            UiKit.Tamano(Herramientas, flexAncho: 1);

            Cuerpo = Ui.Columna(Raiz, "Cuerpo", Tema.espacio);
            Cuerpo.anchorMin = Vector2.zero;
            Cuerpo.anchorMax = Vector2.one;
            Cuerpo.offsetMin = new Vector2(28, 18);
            Cuerpo.offsetMax = new Vector2(-28, -104);

            Capa = PiezasDelMuro.CapaDeArrastre(Ui, Raiz);
        }

        public override void Repintar() {
            if (Cuerpo == null) return;
            UiKit.Vaciar(Cuerpo);
            UiKit.Vaciar(Herramientas);
            Pintar?.Invoke(this);
            Pintadas++;
            _clave = Clave?.Invoke();
            Capa.SetAsLastSibling();
        }

        private void Update() {
            if (Clave == null || Cuerpo == null || FichaArrastrable.EnVuelo != null) return;
            if (Clave() != _clave) Repintar();
        }

        /// <summary>Lo que acaba de pasar, o por que no se pudo: una linea abajo que se va sola.</summary>
        public void Avisar(string texto, Color color) { PiezasDelMuro.Aviso(Ui, Capa, texto, color); }

        /// <summary>«−5 min» saliendo de donde se solto la ficha.</summary>
        public void Marcar(Vector3 mundo, string texto, Color color) { PiezasDelMuro.Marca(Ui, Capa, mundo, texto, color); }

        public override void AlOcultar() {
            if (_cerrado) return;
            _cerrado = true;
            AlCerrar?.Invoke();
        }

        /// <summary>Otra pantalla lo tapo (el glosario, el diario) y vuelve: quien lo abrio vuelve a parar el reloj.</summary>
        public override void AlMostrar() {
            if (_cerrado) {
                _cerrado = false;
                AlReabrir?.Invoke(this);
            }
            base.AlMostrar();
        }
    }
}
