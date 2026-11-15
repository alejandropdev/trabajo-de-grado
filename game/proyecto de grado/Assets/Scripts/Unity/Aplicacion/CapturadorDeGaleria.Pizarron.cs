#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Nexus.Core.Narrativa;
using Nexus.Unity.Pantallas;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// (2026-11-15, direccion de arte) El pizarron tal como se ve en el juego: abre la PantallaDePizarra de verdad con
    /// una sola pagina de un termino del glosario, la captura y la cierra. La capa de la galeria se queda transparente.
    /// </summary>
    public sealed partial class CapturadorDeGaleria {
        /// <summary>Lo que hay que deshacer despues de capturar la pagina (cerrar la pantalla apilada).</summary>
        private Action _despues;

        private void Pizarron(RectTransform capa, string termino, int pagina) {
            var fondo = capa.GetComponent<Image>();
            if (fondo != null) fondo.color = new Color(0, 0, 0, 0);
            var app = AppRoot.Instancia;
            var e = Glosario.Buscar(app.Catalogo.Glosario, termino);
            if (e == null) throw new InvalidOperationException("no hay termino " + termino);
            var paginas = Glosario.PaginasDe(e);
            var p = paginas[Mathf.Clamp(pagina, 0, paginas.Count - 1)];
            app.Router.Apilar<PantallaDePizarra>(s => {
                s.Etiqueta = "¿QUÉ ES?";
                s.Tema = e.Tema;
                s.Paginas = new List<PaginaDePizarra> { p };
                s.ConCurso = e.Curso != null && e.Curso.Count > 0 ? e : null;
            });
            _despues = () => app.Router.Volver();
        }
    }
}
#endif
