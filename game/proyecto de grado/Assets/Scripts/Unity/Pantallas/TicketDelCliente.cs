using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nexus.Core.Modelo;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El ticket del cliente (2026-11-11), con el lenguaje de las pantallas de indicadores en vez de un dialogo con
    /// parrafos apilados: el marco translucido de PantallaDelMuro, arriba una franja de cifras (el numero del ticket, la
    /// prioridad y quien lo reporta) y debajo una tarjeta por cada linea del encargo, con su rotulo y su color
    /// (el problema, lo que piden, los limites, quien es el cliente…). Si una linea es un comentario entrecomillado
    /// («…»), sale como una cita con la cara de quien la dijo y un sello. Las tarjetas entran escalonadas y no hay scroll.
    /// Sirve para «Leer el ticket» y para «El encargo, otra vez» (que añade lo averiguado en el recorrido).
    /// </summary>
    public static class TicketDelCliente {
        private sealed class Linea {
            public string Rotulo;
            public string Texto;
            public string Quien;       // solo en las citas
            public Color Color;
            public bool Cita;
        }

        private static readonly Regex Cabecera = new Regex(@"^Ticket\s+(\S+)\s*·\s*Prioridad:\s*(\S+)\s*·\s*Reportado por:\s*(.+?)\.?$");
        private static readonly Regex CitaDe = new Regex("^(?:Comentario de\\s+)?([^:«\"]{1,40}):\\s*«(.+)»\\.?$");

        public static void Abrir(AppRoot app, LevelProfile perfil, IEnumerable<string> pistas = null, string titulo = "Así llegó el encargo") {
            var etiqueta = pistas == null ? "El ticket original" : "El encargo, otra vez";
            var listaDePistas = pistas == null ? new List<string>() : pistas.Where(p => !string.IsNullOrEmpty(p)).ToList();
            app.Router.Apilar<PantallaDelMuro>(p => {
                p.Etiqueta = etiqueta;
                p.Titulo = titulo;
                p.Pintar = m => Pintar(m, perfil, listaDePistas);
            });
        }

        private static void Pintar(PantallaDelMuro m, LevelProfile perfil, List<string> pistas) {
            var ui = m.Kit; var tema = m.Colores;
            var lineas = new List<Linea>();
            string idTicket = null, prioridad = null, reporta = null;
            foreach (var cruda in perfil.Briefing ?? new string[0]) {
                var texto = (cruda ?? "").Trim();
                if (texto.Length == 0) continue;
                var c = Cabecera.Match(texto);
                if (c.Success) { idTicket = c.Groups[1].Value; prioridad = c.Groups[2].Value; reporta = c.Groups[3].Value; continue; }
                var q = CitaDe.Match(texto);
                if (q.Success) { lineas.Add(new Linea { Rotulo = "LO QUE DICE", Texto = "«" + q.Groups[2].Value + "»", Quien = q.Groups[1].Value.Trim(), Color = tema.danger, Cita = true }); continue; }
                lineas.Add(Clasificar(tema, texto));
            }
            foreach (var pista in pistas) lineas.Add(new Linea { Rotulo = "LO QUE AVERIGUASTE", Texto = pista, Color = tema.cyan });

            // ---- la franja de cifras, como los indicadores
            var tieneCabecera = idTicket != null;
            if (tieneCabecera) {
                var franja = ui.Fila(m.Cuerpo, "Franja", tema.espacio, 0, TextAnchor.UpperLeft);
                UiKit.Tamano(franja, flexAncho: 1, alto: 128);
                franja.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
                Indicador(ui, franja, "TICKET", idTicket, tema.cyan, 0);
                Indicador(ui, franja, "PRIORIDAD", prioridad, PrioridadColor(tema, prioridad), 1);
                Indicador(ui, franja, "LO REPORTA", reporta, tema.warning, 2);
            }

            // ---- una tarjeta por linea, en dos columnas
            var n = lineas.Count;
            for (var i = 0; i < n; i += 2) {
                var fila = ui.Fila(m.Cuerpo, "Fila " + (i / 2 + 1), tema.espacio, 0, TextAnchor.UpperLeft);
                UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1);
                fila.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
                for (var k = i; k < Mathf.Min(n, i + 2); k++) Tarjeta(ui, tema, fila, lineas[k], k + (tieneCabecera ? 3 : 0));
            }
        }

        private static Color PrioridadColor(NexusTheme tema, string prioridad) {
            switch ((prioridad ?? "").ToUpperInvariant()) {
                case "P1": return tema.danger;
                case "P2": return tema.warning;
                default: return tema.success;
            }
        }

        private static Linea Clasificar(NexusTheme tema, string texto) {
            var dos = texto.IndexOf(':');
            var tiene = dos > 0 && dos < 22;
            var clave = tiene ? texto.Substring(0, dos).Trim().ToLowerInvariant() : "";
            var resto = tiene ? texto.Substring(dos + 1).Trim() : texto;
            switch (clave) {
                case "resumen": return new Linea { Rotulo = "EL PROBLEMA", Texto = resto, Color = tema.cyan };
                case "descripción": case "descripcion": return new Linea { Rotulo = "LO QUE PIDEN", Texto = resto, Color = tema.violet };
                case "restricción": case "restriccion": return new Linea { Rotulo = "LOS LÍMITES", Texto = resto, Color = tema.warning };
                case "cliente": return new Linea { Rotulo = "QUIÉN ES EL CLIENTE", Texto = resto, Color = tema.success };
                default: return new Linea { Rotulo = "A TENER EN CUENTA", Texto = texto, Color = tema.inkMuted };
            }
        }

        /// <summary>Un indicador de la franja: rotulo pequeño y un valor grande en color, con una raya arriba.</summary>
        private static void Indicador(UiKit ui, RectTransform fila, string rotulo, string valor, Color color, int indice) {
            var panel = PiezasDelMuro.Panel(ui, fila, rotulo, color);
            UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            panel.gameObject.AddComponent<EntradaDePieza>().Retraso = indice * 0.07f;
            var v = ui.Texto(panel, valor ?? "", EstiloTexto.Hero, color);
            var alto = v.GetComponent<AltoDeTexto>();
            if (alto != null) Object.Destroy(alto);
            v.enableAutoSizing = true; v.fontSizeMin = 18; v.fontSizeMax = 44;
            v.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Tamano(v, flexAncho: 1, flexAlto: 1);
        }

        private static void Tarjeta(UiKit ui, NexusTheme tema, RectTransform fila, Linea l, int indice) {
            var tarjeta = PiezasDelMuro.Panel(ui, fila, l.Rotulo, l.Color, 18);
            UiKit.Tamano(tarjeta, flexAncho: 1, flexAlto: 1);
            tarjeta.gameObject.AddComponent<EntradaDePieza>().Retraso = Mathf.Min(8, indice) * 0.07f;
            if (l.Cita) {
                var cuerpo = ui.Fila(tarjeta, "Cita", tema.espacio * 1.5f, 0, TextAnchor.MiddleLeft);
                UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
                RostroDePersonaje.Crear(ui, cuerpo, MaterialesNexus.IdDePersonaje(l.Quien), l.Quien, 84, l.Color);
                var col = ui.Columna(cuerpo, "Texto", 4, 0, TextAnchor.MiddleLeft);
                UiKit.Tamano(col, flexAncho: 1, flexAlto: 1);
                ui.Texto(col, l.Quien.ToUpperInvariant(), EstiloTexto.Leyenda, l.Color);
                Cuerpo(ui, col, l.Texto, 26, FontStyles.Italic);
                PiezasDelMuro.Sello(ui, tarjeta, "CLIENTE", l.Color, true);
            } else {
                Cuerpo(ui, tarjeta, l.Texto, 24, FontStyles.Normal);
            }
        }

        /// <summary>El texto de una tarjeta: se ajusta a su caja (hasta 24 pt) y nunca se corta.</summary>
        private static void Cuerpo(UiKit ui, Transform padre, string texto, float maximo, FontStyles estilo) {
            var t = ui.Texto(padre, texto, EstiloTexto.Cuerpo, ui.Tema.ink, TextAlignmentOptions.TopLeft);
            var alto = t.GetComponent<AltoDeTexto>();
            if (alto != null) Object.Destroy(alto);
            t.enableAutoSizing = true; t.fontSizeMin = 13; t.fontSizeMax = maximo;
            t.fontStyle = estilo;
            t.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Tamano(t, flexAncho: 1, flexAlto: 1);
        }
    }
}
