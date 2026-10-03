using System;
using Nexus.Core.Evaluacion;
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

            var marco = Ui.Columna(Raiz, "Centro", Tema.espacio, Tema.margen);
            marco.anchorMin = new Vector2(0.14f, 0.06f);
            marco.anchorMax = new Vector2(0.86f, 0.94f);
            marco.offsetMin = marco.offsetMax = Vector2.zero;
            RectTransform centro;
            var scroll = Ui.Desplazable(marco, out centro);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var hoja = new Hoja(Ui, centro);

            var nivel = l.Nivel ?? (l.Exito ? NivelesDeLanzamiento.Bien : NivelesDeLanzamiento.Mal);
            var color = nivel == NivelesDeLanzamiento.Bien ? Tema.cian : nivel == NivelesDeLanzamiento.Mal ? Tema.rojo : Tema.amarillo;
            hoja.Etiqueta("Fase 3 · el lanzamiento · " + s.Perfil.Nombre);
            hoja.Titulo(NivelesDeLanzamiento.Titulo(nivel));
            hoja.Subtitulo($"Puntaje del lanzamiento: {l.Puntaje:0} / 100", color);
            hoja.Parrafo(nivel == NivelesDeLanzamiento.Bien
                ? "El cliente recibió lo que esperaba y casi sin sorpresas. Así se ve un proyecto bien llevado."
                : nivel == NivelesDeLanzamiento.ConProblemas
                    ? "Se entregó, pero el cliente notó cosas. No es un desastre, pero tampoco es lo que se buscaba: mira abajo qué falló."
                    : "El cliente no quedó contento. Abajo está qué falló y qué decisiones lo provocaron: es justo lo que hay que aprender.");
            if (!string.IsNullOrEmpty(l.Texto)) hoja.Parrafo(l.Texto);
            if (!string.IsNullOrEmpty(l.TextoMetodologia)) hoja.Parrafo(l.TextoMetodologia, Tema.cianClaro);

            var datos = hoja.Tarjeta("Lo que se midió");
            foreach (var f in l.Factores) {
                var c = f.Estado == "bien" ? Tema.cian : f.Estado == "mal" ? Tema.rojo : Tema.amarillo;
                var marca = f.Estado == "bien" ? "√" : f.Estado == "mal" ? "×" : "~";
                Ui.Texto(datos, $"<b>{marca} {f.Nombre}:</b> {f.Valor}   <size=80%>({f.Puntos:0}/{f.Maximo:0} pts)</size>", EstiloTexto.Cuerpo, c);
                Ui.Barra(datos, f.Maximo > 0 ? (float)(f.Puntos / f.Maximo) : 0, c);
                Ui.Texto(datos, f.Explicacion, EstiloTexto.Pequeno, Tema.textoTenue);
            }

            if (l.DecisionesQuePesaron.Count > 0) {
                var pesaron = hoja.Tarjeta("Las decisiones que más pesaron en contra", Tema.mostaza);
                foreach (var d in l.DecisionesQuePesaron) Ui.Texto(pesaron, "· " + d, EstiloTexto.Pequeno, Tema.texto);
            }

            hoja.Nota(nivel == NivelesDeLanzamiento.Bien
                ? "Al cerrar el nivel verás cada decisión con su explicación."
                : "Al cerrar el nivel podrás repetirlo para intentar que salga bien, o seguir adelante con estas consecuencias.");
            hoja.Accion("Cerrar el nivel", () => AlSeguir?.Invoke());
            GuiaView.Avisar(App, "lanzamiento");
        }
    }
}
