using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Narrativa;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Aprender a fondo»: el indice del curso de un termino (Scrum, Kanban, Cascada). El pizarron corto del «?»
    /// sigue siendo el recordatorio; esto es para quien quiere entenderlo entero, capitulo a capitulo y sin tener
    /// que pasar treinta paginas seguidas.
    /// </summary>
    public sealed class PantallaDeCurso : Pantalla {
        public EntradaDeGlosario Termino;

        public override bool EsModal { get { return true; } }
        public override bool PuedeVolver { get { return true; } }

        public static void Abrir(AppRoot app, EntradaDeGlosario e) {
            if (e == null || e.Curso == null || e.Curso.Count == 0) return;
            app.Router.Apilar<PantallaDeCurso>(p => p.Termino = e);
        }

        protected override void Construir() {
            Lamina lamina;
            var marco = MarcoDePizarra.Construir(Ui, Raiz, "CURSO", out lamina, true);
            MarcoDePizarra.Boton(Ui, marco, "Ver todo seguido", () => AbrirPaginas(Termino.Curso, null), new Vector2(0, 0), new Vector2(52, 44), 280);
            MarcoDePizarra.Boton(Ui, marco, "Cerrar", () => App.Router.Volver(), new Vector2(1, 0), new Vector2(-52, 44), 220, VarianteBoton.Secundario);

            const float W = MarcoDePizarra.W;
            // Rajdhani, como el resto del pizarron de chapa (docs/08-arte): negrita para titulos, normal para el cuerpo.
            var t = new Tizador(lamina, Ui.Tema.FuenteInterfaz);
            var negrita = new Tizador(lamina, Ui.Tema.FuenteInterfazNegrita);
            if (!string.IsNullOrEmpty(Termino.Tema)) negrita.Texto(60, 50, Termino.Tema.ToUpperInvariant(), 17, PanelDelPizarron.Tema).characterSpacing = 8;
            negrita.Texto(60, 100, Termino.Termino + ", a fondo", 46, PanelDelPizarron.Titulo);
            t.Forma(LuzUI.Redondeado(60, 116, 72, 4, 2), PanelDelPizarron.Acento);
            t.Texto(60, 160, $"{Termino.Curso.Count} páginas en capítulos. Empieza por el primero o entra al que necesites.", 26, PanelDelPizarron.Contador);

            // Los capitulos, en el orden en que aparecen sus paginas: dos columnas de tarjetas.
            var capitulos = new List<string>();
            foreach (var p in Termino.Curso) if (!capitulos.Contains(p.Capitulo ?? "")) capitulos.Add(p.Capitulo ?? "");
            var filas = (capitulos.Count + 1) / 2;
            const float y0 = 200, hueco = 20;
            var ancho = (W - 120 - hueco) / 2;
            var alto = Mathf.Min(150, (800 - y0 - hueco * (filas - 1)) / Mathf.Max(1, filas));
            for (var i = 0; i < capitulos.Count; i++) {
                var capitulo = capitulos[i];
                var paginas = Termino.Curso.Where(p => (p.Capitulo ?? "") == capitulo).ToList();
                var x = 60 + (i % 2) * (ancho + hueco);
                var y = y0 + (i / 2) * (alto + hueco);
                Graphic cara, borde;
                PanelDelPizarron.Tarjeta(Ui, lamina.Raiz, x, y, ancho, alto, out cara, out borde);
                cara.transform.SetAsFirstSibling(); borde.transform.SetAsFirstSibling();
                t.Insignia(x + 46, y + alto / 2, 26, (i + 1).ToString(), PanelDelPizarron.Acento, H(0x1A1608));
                negrita.Texto(x + 92, y + alto / 2 - 10, capitulo, 30, PanelDelPizarron.Titulo);
                t.Texto(x + 92, y + alto / 2 + 24, Resumen(paginas, Mathf.FloorToInt((ancho - 110) / 10.2f)), 20, PanelDelPizarron.Contador);
                lamina.Zona(x, y, ancho, alto, () => AbrirPaginas(paginas, capitulo), "Capitulo " + (i + 1));
            }
        }

        /// <summary>«4 páginas: El Sprint, Sprint Planning…», recortado a lo que cabe en la tarjeta.</summary>
        private static Color H(int rgb) { return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1); }

        private static string Resumen(List<PaginaDePizarra> paginas, int max) {
            var texto = (paginas.Count == 1 ? "1 página: " : paginas.Count + " páginas: ") + string.Join(", ", paginas.Select(p => p.Titulo));
            return texto.Length <= max ? texto : texto.Substring(0, Mathf.Max(0, max - 1)).TrimEnd(' ', ',') + "…";
        }

        private void AbrirPaginas(List<PaginaDePizarra> paginas, string capitulo) {
            App.Router.Apilar<PantallaDePizarra>(p => {
                p.Etiqueta = "CURSO";
                p.Tema = Termino.Termino + ", a fondo" + (capitulo == null ? "" : " · " + capitulo);
                p.Paginas = paginas;
                p.Contador = capitulo;
                p.TextoDeCerrar = "Índice";
            });
        }
    }
}
