using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Narrativa;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;

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
            var marco = MarcoDePizarra.Construir(Ui, Raiz, "CURSO", out lamina);
            MarcoDePizarra.Boton(Ui, marco, "Ver todo seguido", () => AbrirPaginas(Termino.Curso, null), new Vector2(0, 0), new Vector2(52, 44), 280);
            MarcoDePizarra.Boton(Ui, marco, "Cerrar", () => App.Router.Volver(), new Vector2(1, 0), new Vector2(-52, 44), 220, VarianteBoton.Secundario);

            const float W = MarcoDePizarra.W;
            var t = new Tizador(lamina, Ui.FuenteTiza);
            if (!string.IsNullOrEmpty(Termino.Tema)) t.Texto(60, 44, Termino.Tema.ToUpperInvariant(), 22, Tizador.Gris);
            t.Texto(W / 2, 96, Termino.Termino + ", a fondo", 44, Tizador.Tiza, true);
            t.Linea(360, 116, W - 360, 116, null, 2);
            t.Texto(W / 2, 160, $"{Termino.Curso.Count} páginas en capítulos. Empieza por el primero o entra al que necesites.", 26, Tizador.Gris, true);

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
                t.Caja(x, y, ancho, alto, null, Tizador.Cian);
                t.Insignia(x + 46, y + alto / 2, 26, (i + 1).ToString(), Tizador.Cian, Tizador.Pizarra);
                t.Texto(x + 92, y + alto / 2 - 14, capitulo, 30, Tizador.Tiza);
                t.Texto(x + 92, y + alto / 2 + 22, Resumen(paginas, Mathf.FloorToInt((ancho - 110) / 11.5f)), 20, Tizador.Gris);
                lamina.Zona(x, y, ancho, alto, () => AbrirPaginas(paginas, capitulo), "Capitulo " + (i + 1));
            }
        }

        /// <summary>«4 páginas: El Sprint, Sprint Planning…», recortado a lo que cabe en la tarjeta.</summary>
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
