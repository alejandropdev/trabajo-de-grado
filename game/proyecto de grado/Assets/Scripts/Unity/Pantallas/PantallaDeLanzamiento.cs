using System;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La Fase 3: el cliente ve lo que se hizo. Lo que se acumulo durante todos los dias (deuda, cobertura,
    /// cansancio) decide aqui si sale bien. No hay nada que elegir: es la cuenta.
    /// </summary>
    public sealed class PantallaDeLanzamiento : Pantalla {
        public Action AlSeguir;

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            var s = App.Sesion;
            var l = s.Lanzamiento;

            var centro = Ui.Columna(Raiz, "Centro", Tema.espacio, Tema.margen);
            centro.anchorMin = new Vector2(0.18f, 0.1f);
            centro.anchorMax = new Vector2(0.82f, 0.9f);
            centro.offsetMin = centro.offsetMax = Vector2.zero;
            var hoja = new Hoja(Ui, centro);

            hoja.Etiqueta("Fase 3 · el lanzamiento · " + s.Perfil.Nombre);
            hoja.Titulo(l.Exito ? "Salió bien" : "Salió mal");
            if (!string.IsNullOrEmpty(l.Texto)) hoja.Parrafo(l.Texto);
            if (!string.IsNullOrEmpty(l.TextoMetodologia)) hoja.Parrafo(l.TextoMetodologia, Tema.cianClaro);

            var datos = hoja.Tarjeta("Lo que llegó al cliente");
            Ui.Texto(datos, $"Alcance entregado: {l.AlcanceEntregado:0} de {l.AlcanceComprometido:0} puntos", EstiloTexto.Cuerpo);
            Ui.Barra(datos, l.AlcanceComprometido > 0 ? (float)(l.AlcanceEntregado / l.AlcanceComprometido) : 0);
            Ui.Texto(datos, $"Defectos que encontró el cliente: {l.DefectosEscapados}", EstiloTexto.Cuerpo,
                     l.DefectosEscapados == 0 ? Tema.cian : Tema.amarillo);
            Ui.Texto(datos, $"Riesgo con el que se lanzó: {l.RiesgoDeLanzamiento:0} / 100", EstiloTexto.Cuerpo);
            Ui.Barra(datos, (float)l.RiesgoDeLanzamiento / 100f, l.RiesgoDeLanzamiento > 50 ? Tema.rojo : Tema.mostaza);

            hoja.Accion("Cerrar el nivel", () => AlSeguir?.Invoke());
            GuiaView.Avisar(App, "lanzamiento");
        }
    }
}
