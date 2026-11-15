using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Proyecto;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La pantallita de una parte del sistema dentro de su tarjeta de «El producto» (2026-11-14): se ve como es esa
    /// parte HOY, segun su etapa, en vez de solo leerlo.
    ///   · boceto     lineas grises sin rellenar: la idea, nada mas;
    ///   · mockup     cajas de color con los datos de ejemplo apagados: se ve como sera, pero nada responde;
    ///   · prototipo  ya tiene su boton y responde, pero no guarda: pulsarlo no deja rastro;
    ///   · MVP        funciona de verdad: su boton añade un registro que se queda, y las demas partes que ya funcionan
    ///                lo reciben (una fila «llegó…» en su pantallita).
    /// La forma sale de ModuloDelProyecto.Pantalla (formulario, lista, aviso, lector, calendario, grafica) y sus filas
    /// de ModuloDelProyecto.Datos. Todo con piezas de UI: sin texturas.
    /// </summary>
    public static class MaquetaDelModulo {
        private const int FilasVisibles = 3;

        /// <param name="propios">Registros que esta parte ya creo (solo cuenta en un MVP con accion).</param>
        /// <param name="recibidos">Registros creados en otras partes que esta, si funciona, ya refleja.</param>
        /// <param name="nuevo">Hay que animar la ultima fila (se acaba de crear o de recibir).</param>
        /// <param name="alUsar">Pulsar el boton de la pantallita (prototipo o MVP).</param>
        public static RectTransform Crear(UiKit ui, Transform padre, EstadoDelModulo e, int propios, int recibidos, bool nuevo, Action alUsar) {
            var tema = ui.Tema;
            var etapa = e.Etapa;
            var boceto = etapa == EtapasDelProducto.Boceto;
            var mockup = etapa == EtapasDelProducto.Mockup;
            var prototipo = etapa == EtapasDelProducto.Prototipo;
            var funciona = etapa == EtapasDelProducto.Mvp || etapa == EtapasDelProducto.Desplegado;
            var acento = boceto ? tema.inkMuted : mockup ? tema.violet : prototipo ? tema.warning : etapa == EtapasDelProducto.Desplegado ? tema.success : tema.cyan;

            // El marco: una ventana con su barra de titulo.
            var marco = ui.Columna(padre, "Maqueta", 4, 8, TextAnchor.UpperLeft);
            var fondo = marco.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, boceto ? NexusTheme.Alfa(tema.bg950, 0.35f) : NexusTheme.Alfa(tema.bg950, 0.9f), NexusTheme.RadioMd);
            fondo.raycastTarget = false;
            ui.Borde(marco, NexusTheme.Alfa(acento, boceto ? 0.45f : 0.7f), NexusTheme.RadioMd, boceto ? 1 : 1.5f);
            UiKit.Tamano(marco, flexAncho: 1, flexAlto: 1, alto: 104);   // crece si hay sitio; sin scroll con hasta 9 partes

            var barra = ui.Fila(marco, "Barra", 5, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(barra, flexAncho: 1);
            for (var i = 0; i < 3; i++) Punto(ui, barra, boceto ? NexusTheme.Alfa(tema.inkMuted, 0.4f) : NexusTheme.Alfa(i == 0 ? tema.danger : i == 1 ? tema.warning : tema.success, 0.8f), 7);
            var titulo = ui.Texto(barra, boceto ? "" : e.Modulo.Nombre, EstiloTexto.Leyenda, NexusTheme.Alfa(tema.ink, 0.7f));
            Una(titulo);
            UiKit.Tamano(titulo, flexAncho: 1);
            if (funciona && !string.IsNullOrEmpty(e.Modulo.Accion) && propios > 0)
                ui.Chip(barra, propios + (propios == 1 ? " guardado" : " guardados"), NexusTheme.Alfa(tema.success, 0.18f), tema.success);

            // El cuerpo: segun la forma de la pantalla.
            var cuerpo = ui.Columna(marco, "Cuerpo", 4, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
            var forma = string.IsNullOrEmpty(e.Modulo.Pantalla) ? "lista" : e.Modulo.Pantalla;
            var ejemplos = e.Modulo.Datos == null ? 0 : e.Modulo.Datos.Count;
            var filas = Filas(e, funciona ? propios : 0, funciona ? recibidos : 0);
            if (forma == "grafica") Grafica(ui, cuerpo, acento, boceto, mockup, funciona ? propios + recibidos : 0);
            else if (forma == "lector") Lector(ui, cuerpo, acento, boceto, mockup, filas);
            else {
                var visibles = filas.Skip(Math.Max(0, filas.Count - FilasVisibles)).ToList();
                for (var i = 0; i < visibles.Count; i++)
                    Fila(ui, cuerpo, visibles[i], acento, boceto, mockup, forma, nuevo && funciona && i == visibles.Count - 1 && filas.Count > ejemplos);
            }

            // El boton de la pantalla: en un boceto es una caja vacia; en un mockup, un boton que no hace nada.
            var accion = string.IsNullOrEmpty(e.Modulo.Accion) ? null : e.Modulo.Accion;
            if (accion != null) {
                if (boceto || mockup) {
                    var falso = ui.Nodo(marco, "Boton falso");
                    var img = falso.gameObject.AddComponent<Image>();
                    ui.Fondo(img, boceto ? Color.clear : NexusTheme.Alfa(tema.violet, 0.25f), NexusTheme.RadioSm);
                    img.raycastTarget = false;
                    ui.Borde(falso, NexusTheme.Alfa(acento, 0.5f), NexusTheme.RadioSm, 1);
                    UiKit.Tamano(falso, flexAncho: 1, alto: 30);
                    if (mockup) {
                        var t = ui.Texto(falso, accion, EstiloTexto.Etiqueta, NexusTheme.Alfa(tema.ink, 0.45f), TextAlignmentOptions.Center);
                        Una(t);
                        UiKit.Rellenar(t.rectTransform);
                    }
                } else {
                    var boton = ui.Boton(marco, (funciona ? "▶  " : "") + accion, () => alUsar?.Invoke(), funciona ? VarianteBoton.Primario : VarianteBoton.Secundario);
                    UiKit.Tamano(boton, flexAncho: 1, alto: 34);
                    if (funciona) boton.gameObject.AddComponent<LatidoSuave>();
                }
            }
            return marco;
        }

        /// <summary>Las filas que enseña: los datos de ejemplo y, si funciona, lo creado aqui y lo que llego de otras partes.</summary>
        private static List<string> Filas(EstadoDelModulo e, int propios, int recibidos) {
            var filas = new List<string>(e.Modulo.Datos ?? new List<string>());
            if (filas.Count == 0) filas.AddRange(new[] { "—", "—" });
            for (var i = 1; i <= propios; i++) filas.Add("✓ " + e.Modulo.Accion + " · nuevo #" + i);
            if (string.IsNullOrEmpty(e.Modulo.Accion))
                for (var i = 1; i <= recibidos; i++) filas.Add("← llegó el registro #" + i);
            return filas;
        }

        private static void Fila(UiKit ui, Transform cuerpo, string texto, Color acento, bool boceto, bool mockup, string forma, bool animar) {
            var tema = ui.Tema;
            var fila = ui.Fila(cuerpo, "Fila", 6, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(fila, flexAncho: 1, alto: 20);
            if (forma == "aviso" || forma == "calendario") Punto(ui, fila, NexusTheme.Alfa(acento, boceto ? 0.35f : 0.9f), 8);
            if (boceto) {
                // Un boceto: una raya donde ira el dato.
                var raya = ui.Nodo(fila, "Raya");
                var r = raya.gameObject.AddComponent<Image>();
                r.color = NexusTheme.Alfa(tema.inkMuted, 0.35f);
                r.raycastTarget = false;
                UiKit.Tamano(raya, flexAncho: 1, alto: 2);
                return;
            }
            var caja = ui.Nodo(fila, "Caja");
            var img = caja.gameObject.AddComponent<Image>();
            ui.Fondo(img, NexusTheme.Alfa(acento, animar ? 0.3f : 0.1f), NexusTheme.RadioXs);
            img.raycastTarget = false;
            UiKit.Tamano(caja, flexAncho: 1, alto: 20);
            var t = ui.Texto(caja, texto, EstiloTexto.Pequeno, mockup ? NexusTheme.Alfa(tema.ink, 0.35f) : tema.ink);
            Una(t);
            t.fontSize = 12;
            var rt = t.rectTransform;
            UiKit.Rellenar(rt);
            rt.offsetMin = new Vector2(6, 0);
            if (animar) caja.gameObject.AddComponent<EntradaDePieza>();
        }

        private static void Lector(UiKit ui, Transform cuerpo, Color acento, bool boceto, bool mockup, List<string> filas) {
            var tema = ui.Tema;
            var visor = ui.Nodo(cuerpo, "Visor");
            UiKit.Tamano(visor, flexAncho: 1, flexAlto: 1, alto: 46);
            ui.Borde(visor, NexusTheme.Alfa(acento, boceto ? 0.35f : 0.8f), NexusTheme.RadioSm, boceto ? 1 : 2);
            if (boceto) return;
            var placa = ui.Texto(visor, filas.Count > 0 ? filas[filas.Count - 1] : "", EstiloTexto.Mono, mockup ? NexusTheme.Alfa(tema.ink, 0.35f) : acento, TextAlignmentOptions.Center);
            Una(placa);
            placa.fontSize = 18;
            UiKit.Rellenar(placa.rectTransform);
        }

        private static void Grafica(UiKit ui, Transform cuerpo, Color acento, bool boceto, bool mockup, int extra) {
            var fila = ui.Fila(cuerpo, "Barras", 6, 0, TextAnchor.LowerLeft);
            UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1, alto: 50);
            var altos = new[] { 0.4f, 0.7f, 0.55f, 0.9f, Mathf.Min(1f, 0.3f + 0.15f * extra) };
            foreach (var a in altos) {
                var b = ui.Nodo(fila, "Barra");
                var img = b.gameObject.AddComponent<Image>();
                img.color = boceto ? Color.clear : NexusTheme.Alfa(acento, mockup ? 0.3f : 0.8f);
                img.raycastTarget = false;
                if (boceto) ui.Borde(b, NexusTheme.Alfa(ui.Tema.inkMuted, 0.4f), 2, 1);
                UiKit.Tamano(b, flexAncho: 1, alto: 50 * a);
            }
        }

        private static void Punto(UiKit ui, Transform padre, Color color, float lado) {
            var p = ui.Nodo(padre, "Punto");
            var img = p.gameObject.AddComponent<Image>();
            img.sprite = ui.SpriteCircular();
            img.color = color;
            img.raycastTarget = false;
            UiKit.Tamano(p, ancho: lado, alto: lado);
        }

        private static void Una(TMP_Text t) {
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) UnityEngine.Object.Destroy(alto);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    /// <summary>Un latido suave para el boton que funciona: invita a pulsarlo sin gritar.</summary>
    public sealed class LatidoSuave : MonoBehaviour {
        private void Update() {
            transform.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(Time.unscaledTime * 4f));
        }
    }
}
