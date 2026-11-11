using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Despliegue;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public sealed partial class PantallaDeDespliegue {
        // ==================================================================== 3 · como salio

        /// <summary>
        /// El resultado, sin scroll ni parrafos (2026-11-09): tres tarjetas — la tuberia (una luz por etapa y los intentos),
        /// los modulos (una lampara cada uno) y lo que cambia hoy (barras que suben o bajan) — y, abajo, lo que noto el
        /// cliente en una frase y el boton de seguir. Pasar por una luz o una lampara dice en el dialogo que paso con ella.
        /// </summary>
        private void PintarResultado() {
            var r = _resultado;
            _paso = Paso.Resultado;
            PonerAntetitulo("resultado");
            PonerIntentos();
            Decir(r.Titulo + (r.SeAcaboElTiempo ? ". Se acabó el tiempo: se entregó lo que había puesto." : ". Pasa por las luces y las lámparas para ver qué pasó con cada una."));
            var bueno = r.Nivel == "bien";
            var tono = bueno ? Tema.success : r.Nivel == "mal" ? Tema.danger : Tema.warning;

            var fila = Ui.Fila(_escena, "Tarjetas", Tema.espacio * 2, 0, TextAnchor.UpperCenter);
            Anclar(fila, new Vector2(0.5f, 0.5f), new Vector2(0, 10));
            Crecer(fila, true);

            // ---- 1 · la tuberia: una luz por etapa
            var tub = Tarjeta(fila, "LA TUBERÍA", Tema.cyan);
            var renglonesTuberia = r.Renglones.Where(x => x.Que == "tuberia").ToList();
            var luces = Ui.Fila(tub, "Luces", 6, 0, TextAnchor.MiddleLeft);
            for (var i = 0; i < renglonesTuberia.Count; i++) {
                var x = renglonesTuberia[i];
                Lampara(luces, x.Estado, 30, 0.4f + i * 0.07f, x.Nombre, x.Texto);
            }
            Ui.Texto(tub, renglonesTuberia.Count == 0 ? "Quedó sin etapas." : renglonesTuberia.Count(x => x.Estado == EstadosDeDespliegue.Bien) + " de " + renglonesTuberia.Count + " etapas bien.",
                     EstiloTexto.Cuerpo, Tema.ink);
            Ui.Chip(tub, r.IntentosFallidos == 0 ? "Salió a la primera" : r.IntentosFallidos == 1 ? "1 prueba fallida" : r.IntentosFallidos + " pruebas fallidas",
                    r.IntentosFallidos == 0 ? Tema.successSoft : Tema.warningSoft, r.IntentosFallidos == 0 ? Tema.success : Tema.warning);
            if (r.SeAcaboElTiempo) Ui.Chip(tub, "Se acabó el tiempo", Tema.dangerSoft, Tema.danger);

            // ---- 2 · los modulos: una lampara cada uno
            var mods = Tarjeta(fila, "LOS MÓDULOS", Tema.violet);
            var renglonesModulos = r.Renglones.Where(x => x.Que == "modulo").ToList();
            if (renglonesModulos.Count == 0) Ui.Texto(mods, "Hoy no salía ningún módulo.", EstiloTexto.Cuerpo, Tema.inkMuted);
            for (var i = 0; i < renglonesModulos.Count; i += 3) {
                var fl = Ui.Fila(mods, "Lamparas", Tema.espacio, 0, TextAnchor.UpperLeft);
                foreach (var x in renglonesModulos.Skip(i).Take(3)) {
                    var col = Ui.Columna(fl, "Modulo " + x.Id, 4, 0, TextAnchor.UpperCenter);
                    UiKit.Tamano(col, ancho: 140);
                    Lampara(col, x.Estado, 48, 0.5f + (i / 3) * 0.1f, x.Nombre, x.Texto);
                    Ui.Texto(col, x.Nombre, EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.Center);
                }
            }
            if (renglonesModulos.Count > 0)
                Ui.Texto(mods, r.ModulosEncendidos.Count + " de " + renglonesModulos.Count + " encendidos.", EstiloTexto.Cuerpo, Tema.ink);

            // ---- 3 · lo que cambia hoy: barras que suben o bajan
            var hoy = Tarjeta(fila, "LO QUE CAMBIA HOY", tono);
            var cambios = new List<KeyValuePair<string, double>>();
            foreach (var kv in r.Efectos) cambios.Add(new KeyValuePair<string, double>(Textos.NombreDeStock(kv.Key), Convert.ToDouble(kv.Value)));
            cambios.Add(new KeyValuePair<string, double>("Riesgo del lanzamiento", r.Riesgo));
            if (r.Defectos > 0) cambios.Add(new KeyValuePair<string, double>("Errores que se escapan", r.Defectos));
            if (r.AvanceQueCostaraManana > 0 && !r.EsFinal) cambios.Add(new KeyValuePair<string, double>("Avance de mañana", -r.AvanceQueCostaraManana));
            var n = 0;
            foreach (var par in cambios.Take(6)) {
                // En estas barras, subir es malo: deuda, cansancio, riesgo y errores.
                var sube = par.Key == Textos.NombreDeStock("DeudaTecnica") || par.Key == Textos.NombreDeStock("Cansancio") ||
                           par.Key == "Riesgo del lanzamiento" || par.Key == "Errores que se escapan";
                BarraDeCambio(hoy, par.Key, par.Value, sube ? par.Value > 0 : par.Value < 0, 0.6f + n * 0.1f);
                n++;
            }
            if (cambios.Count == 0) Ui.Texto(hoy, "Nada: las barras se quedan como estaban.", EstiloTexto.Cuerpo, Tema.inkMuted);

            for (var i = 0; i < fila.childCount; i++) Aparecer((RectTransform)fila.GetChild(i), 0.12f + i * 0.16f);

            // ---- abajo a la izquierda: lo que noto el cliente
            var cliente = Globo(_escena, "Cliente", false, NexusTheme.Alfa(tono, 0.7f));
            Anclar(cliente, new Vector2(0, 0), new Vector2(16, 28));
            Crecer(cliente, true);
            Ui.Texto(cliente, "LO QUE NOTÓ EL CLIENTE", EstiloTexto.Leyenda, tono);
            var frase = Ui.Texto(cliente, r.ParaElCliente, EstiloTexto.Cuerpo, Tema.ink);
            UiKit.Tamano(frase, ancho: 640);
            var voz = App.Sesion.VocesDelProducto().FirstOrDefault(v => r.ModulosEncendidos.Contains(v.ModuloId));
            if (voz != null) Ui.Texto(cliente, voz.Quien + ": «" + voz.Frase + "»", EstiloTexto.Pequeno, Tema.cyan);
            Aparecer(cliente, 0.9f);

            // ---- abajo a la derecha: seguir
            var seguir = Globo(_escena, "Seguir", true);
            Anclar(seguir, new Vector2(1, 0), new Vector2(-16, 28));
            Crecer(seguir, true);
            var boton = Ui.Boton(seguir, r.EsFinal ? "Ir al lanzamiento  ►" : "Volver al desarrollo  ►", () => { if (AlTerminar != null) AlTerminar(); }, VarianteBoton.Primario);
            UiKit.Tamano(boton, alto: 64);
            Aparecer(seguir, 1.0f);
            GuiaView.Avisar(App, "despliegue.resultado");
        }

        /// <summary>
        /// Una luz de estado (verde = bien, ambar = mal puesto, roja = falto): salta con un rebote al aparecer y, al pasar por
        /// ella, el dialogo dice que paso.
        /// </summary>
        private void Lampara(RectTransform padre, string estado, float lado, float retraso, string nombre, string texto) {
            var bien = estado == EstadosDeDespliegue.Bien; var mal = estado == EstadosDeDespliegue.Mal;
            var color = bien ? Tema.success : mal ? Tema.warning : Tema.danger;
            var nodo = Ui.Nodo(padre, "Lampara " + nombre);
            UiKit.Tamano(nodo, lado, lado);
            var img = nodo.gameObject.AddComponent<Image>();
            img.sprite = Ui.SpriteCircular();
            img.color = color;
            img.raycastTarget = true;
            var marca = Ui.Texto(nodo, bien ? "√" : mal ? "~" : "×", EstiloTexto.Subtitulo, Tema.bg950, TextAlignmentOptions.Center);
            var alto = marca.GetComponent<AltoDeTexto>();
            if (alto != null) Destroy(alto);
            marca.fontSize = lado * 0.58f;
            marca.fontStyle = FontStyles.Bold;
            marca.raycastTarget = false;
            UiKit.Rellenar(marca.rectTransform);
            var pasar = nodo.gameObject.AddComponent<AlPasarElRaton>();
            var frase = nombre + ": " + (bien ? "bien. " : mal ? "mal puesto. " : "faltó. ") + texto;
            pasar.AlEntrar = () => Decir(frase);
            nodo.localScale = Vector3.zero;
            _anim.Lanzar(0.6f, t => { if (nodo != null) nodo.localScale = Vector3.one * Curvas.Elastica(t); }, () => { if (nodo != null) nodo.localScale = Vector3.one; }, retraso, nodo.gameObject);
        }

        /// <summary>Una barra que crece desde el centro: a la derecha si sube, a la izquierda si baja; verde si es bueno, roja si es malo.</summary>
        private void BarraDeCambio(RectTransform padre, string nombre, double valor, bool malo, float retraso) {
            var color = malo ? Tema.danger : Tema.success;
            var fila = Ui.Fila(padre, "Efecto " + nombre, Tema.espacio * 0.75f, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(Ui.Texto(fila, nombre, EstiloTexto.Pequeno, Tema.ink), ancho: 170);
            var pista = Ui.Nodo(fila, "Pista");
            UiKit.Tamano(pista, 190, 16);
            var fondo = pista.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, Tema.bg900, 8);
            fondo.raycastTarget = false;
            var marcaCentro = Ui.Nodo(pista, "Centro");
            marcaCentro.anchorMin = marcaCentro.anchorMax = new Vector2(0.5f, 0.5f);
            marcaCentro.sizeDelta = new Vector2(2, 22);
            var mc = marcaCentro.gameObject.AddComponent<Image>();
            mc.color = Tema.inkMuted;
            mc.raycastTarget = false;
            var relleno = Ui.Nodo(pista, "Relleno");
            relleno.anchorMin = new Vector2(0.5f, 0); relleno.anchorMax = new Vector2(0.5f, 1);
            relleno.pivot = new Vector2(valor >= 0 ? 0f : 1f, 0.5f);
            relleno.anchoredPosition = Vector2.zero;
            relleno.sizeDelta = new Vector2(0, -4);
            var ri = relleno.gameObject.AddComponent<Image>();
            ri.color = color;
            ri.raycastTarget = false;
            var etiqueta = Ui.Texto(fila, (valor >= 0 ? "▲ " : "▼ ") + Textos.Cambio(valor), EstiloTexto.Pequeno, color);
            UiKit.Tamano(etiqueta, ancho: 96);
            var meta = Mathf.Clamp((float)Math.Abs(valor) / 12f, 0.06f, 1f) * 92f;
            _anim.Lanzar(0.7f, t => { if (relleno != null) relleno.sizeDelta = new Vector2(meta * Curvas.SalidaCubica(t), -4); }, null, retraso, relleno.gameObject);
        }
    }
}
