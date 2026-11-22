using System;
using System.Collections.Generic;
using Nexus.Core.Fase1;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La lista de TODO lo que se puede recoger en el recorrido 3D (2026-11-20): una cuadricula de tarjetas, una por tipo
    /// de objeto, con que es y para que sirve. Nunca dice DONDE esta cada cosa. Lo que es parte de la historia (el
    /// fragmento de USB) sale como «?» hasta encontrarlo; las cartas y las notas cuentan «x de y» segun el Diario.
    /// Se usa dentro de la Pausa del 3D (Pintar) y antes del recorrido, como dialogo (Abrir).
    /// </summary>
    public static class PanelDeObjetos3D {
        private const int Columnas = 3;

        /// <summary>Pinta la cuadricula dentro de «padre» (una columna). «diario» = los ids del Diario del jugador.</summary>
        public static void Pintar(UiKit ui, Transform padre, IEnumerable<string> diario) {
            var tema = ui.Tema;
            var grupos = GuiaDeObjetos3D.Grupos;
            var lista = new List<string>(diario ?? new string[0]);
            RectTransform fila = null;
            for (var i = 0; i < grupos.Count; i++) {
                if (i % Columnas == 0) {
                    fila = ui.Fila(padre, "Fila " + (i / Columnas + 1), tema.Espacio(2), 0, TextAnchor.UpperLeft);
                    fila.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
                    fila.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
                }
                Tarjeta(ui, fila, grupos[i], lista);
            }
        }

        private static void Tarjeta(UiKit ui, Transform fila, GrupoDeObjeto3D g, List<string> diario) {
            var tema = ui.Tema;
            var oculto = GuiaDeObjetos3D.EstaOculto(g, diario);
            var acento = ColorDe(tema, g);
            // Una ficha limpia: la imagen del objeto tal como se ve en el 3D, su nombre y UNA linea. Nada mas.
            var tarjeta = ui.PanelColumna(fila, "Objeto " + g.Id, tema.Espacio(2), tema.Espacio(1),
                                          NexusTheme.Alfa(tema.bg950, 0.55f));
            tarjeta.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            UiKit.Tamano(tarjeta, flexAncho: 1, flexAlto: 1);
            ui.Borde(tarjeta, NexusTheme.Alfa(oculto ? tema.inkFaint : acento, 0.45f), NexusTheme.RadioMd, 1.5f);

            var caja = ui.Nodo(tarjeta, "Imagen");
            UiKit.Tamano(caja, alto: tema.Px(112), flexAncho: 1);
            var sprite = MaterialesNexus.Ilustracion("objetos3d/" + g.Id);
            if (sprite != null) {
                var img = caja.gameObject.AddComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                // Lo secreto se ve como una silueta: se adivina que hay algo, no que es.
                img.color = oculto ? new Color(0.05f, 0.07f, 0.1f, 0.85f) : Color.white;
            } else {
                TextoEnCaja.Crear(ui, caja, oculto ? "?" : g.Glifo, EstiloTexto.Titulo, acento, TMPro.TextAlignmentOptions.Center,
                                  tema.Px(44), Vector2.zero, Vector2.one, 4);
            }
            if (oculto && sprite != null) {
                TextoEnCaja.Crear(ui, caja, "?", EstiloTexto.Titulo, tema.inkMuted, TMPro.TextAlignmentOptions.Center,
                                  tema.Px(52), Vector2.zero, Vector2.one, 4);
            }

            var nombre = oculto ? "???" : g.Nombre;
            if (!oculto && g.Cantidad > 1 && g.PrefijoDelDiario == null) nombre += "  ×" + g.Cantidad;
            var titulo = ui.Texto(tarjeta, nombre, EstiloTexto.Cuerpo, oculto ? tema.inkMuted : tema.ink, TMPro.TextAlignmentOptions.Center);
            titulo.fontStyle = TMPro.FontStyles.Bold;
            ui.Texto(tarjeta, oculto ? g.PistaSecreta : g.ParaQue, EstiloTexto.Pequeno, tema.inkMuted, TMPro.TextAlignmentOptions.Center);
            if (!oculto && g.PrefijoDelDiario != null)
                ui.Texto(tarjeta, GuiaDeObjetos3D.Encontradas(g, diario) + " DE " + g.Cantidad, EstiloTexto.Leyenda, acento,
                         TMPro.TextAlignmentOptions.Center);
        }

        private static Color ColorDe(NexusTheme tema, GrupoDeObjeto3D g) {
            switch (g.Id) {
                case "cv": return tema.cyan;
                case "pizza": case "cafe": case "guepardex": return tema.success;
                case "acceso": return tema.warning;
                case "cartas": case "notas": return tema.violet;
                default: return tema.warning;
            }
        }

        /// <summary>
        /// La cuadricula como dialogo, encima de «padre» (con un velo que bloquea los clics) y un boton para cerrar.
        /// Devuelve el nodo raiz: destruirlo la cierra.
        /// </summary>
        public static GameObject Abrir(UiKit ui, Transform padre, IEnumerable<string> diario, Action alCerrar = null) {
            var tema = ui.Tema;
            var raiz = ui.Nodo(padre, "Objetos del recorrido");
            UiKit.Rellenar(raiz);
            var velo = raiz.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(tema.bg950, 0.8f);
            var ventana = ui.Dialogo(raiz, "Ventana");
            ventana.anchorMin = ventana.anchorMax = ventana.pivot = new Vector2(0.5f, 0.5f);
            ventana.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ventana.sizeDelta = new Vector2(tema.Px(960), 0);

            ui.Texto(ventana, "RECORRIDO · OBJETOS", EstiloTexto.Leyenda, tema.cyan);
            ui.Texto(ventana, "Lo que puedes recoger", EstiloTexto.Titulo, tema.ink);
            Pintar(ui, ventana, diario);
            var cerrar = ui.Boton(ventana, "Cerrar", () => {
                UnityEngine.Object.Destroy(raiz.gameObject);
                if (alCerrar != null) alCerrar();
            }, VarianteBoton.Primario);
            UiKit.Tamano(cerrar, flexAncho: 1);
            return raiz.gameObject;
        }
    }
}
