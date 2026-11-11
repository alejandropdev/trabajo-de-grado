using System.Linq;
using Nexus.Core.Proyecto;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «El producto» rediseñado (2026-11-11): la maqueta viva de lo que se esta construyendo, con el lenguaje de las
    /// pantallas de indicadores y las transiciones del fin de dia en lugar de un pizarron de tiza.
    ///
    ///   · arriba, una franja de tres indicadores: en que etapa esta el producto hoy, cuantas partes ya funcionan y el
    ///     avance medio (con cifras que cuentan y una barra que crece);
    ///   · una tarjeta por parte del sistema: su avance en grande, la etapa (boceto → mockup → prototipo → MVP) como una
    ///     barra por fases y, al pulsarla, lo que hace esa parte (un prototipo responde pero no guarda; en un MVP lo que
    ///     haces aparece en las partes que dependen de el);
    ///   · abajo, un globo con lo que acaba de pasar o con lo que dice de su parte quien la va a usar, con su cara.
    /// Las tarjetas entran escalonadas y no hay scroll. No cambia nada del proyecto; el reloj espera mientras esta abierta.
    /// </summary>
    public static class ProductoVivo {
        private sealed class Estado {
            public string Elegido, Mensaje;
            public int Registros;
            public bool PausadoAntes;
        }

        public static void Abrir(AppRoot app) {
            if (app.Sesion == null || app.Sesion.Perfil.Proyecto == null) return;
            var estado = new Estado();
            if (app.Runner != null) { estado.PausadoAntes = app.Runner.Pausado; app.Runner.Pausado = true; }
            app.Router.Apilar<PantallaDelMuro>(p => {
                p.Etiqueta = "El producto · " + app.Sesion.Perfil.Proyecto.Nombre;
                p.Titulo = "Lo que hay construido hoy";
                p.Pintar = m => Pintar(app, m, estado);
                p.AlCerrar = () => { if (app.Runner != null) app.Runner.Pausado = estado.PausadoAntes; };
                p.AlReabrir = pm => { if (app.Runner != null) { estado.PausadoAntes = app.Runner.Pausado; app.Runner.Pausado = true; } };
            });
        }

        private static Color ColorDe(NexusTheme tema, string etapa) {
            switch (etapa) {
                case EtapasDelProducto.Boceto: return tema.inkMuted;
                case EtapasDelProducto.Mockup: return tema.violet;
                case EtapasDelProducto.Prototipo: return tema.warning;
                case EtapasDelProducto.Desplegado: return tema.success;
                default: return tema.cyan;
            }
        }

        private static bool Funciona(EstadoDelModulo m) { return m.Etapa == EtapasDelProducto.Mvp || m.Etapa == EtapasDelProducto.Desplegado; }

        private static void Pintar(AppRoot app, PantallaDelMuro m, Estado est) {
            var ui = m.Kit; var tema = m.Colores;
            var s = app.Sesion;
            var modulos = s.Producto();
            var animar = m.PrimeraVez;
            var general = MaquetaDelProducto.EtapaGeneral(modulos);
            var funcionan = modulos.Count(Funciona);
            var medio = modulos.Count == 0 ? 0 : modulos.Average(x => x.Porcentaje);

            // ---- la franja de indicadores
            var franja = ui.Fila(m.Cuerpo, "Franja", tema.espacio, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(franja, flexAncho: 1, alto: 132);
            franja.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            var hoy = PiezasDelMuro.Panel(ui, franja, "Hoy es un…", ColorDe(tema, general));
            UiKit.Tamano(hoy, flexAncho: 1, flexAlto: 1);
            if (animar) hoy.gameObject.AddComponent<EntradaDePieza>().Retraso = 0f;
            var etapaTexto = ui.Texto(hoy, EtapasDelProducto.Nombre(general).ToUpperInvariant(), EstiloTexto.Hero, ColorDe(tema, general));
            etapaTexto.font = tema.FuenteDisplay;
            etapaTexto.textWrappingMode = TextWrappingModes.NoWrap;

            var parte = PiezasDelMuro.Panel(ui, franja, "Partes que ya funcionan", tema.success);
            UiKit.Tamano(parte, flexAncho: 1, flexAlto: 1);
            if (animar) parte.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.07f;
            PiezasDelMuro.Cifra(ui, parte, funcionan, "0", " de " + modulos.Count, funcionan == modulos.Count && modulos.Count > 0 ? tema.success : tema.ink, 44, animar ? 0.2f : -1f);

            var avance = PiezasDelMuro.Panel(ui, franja, "Avance medio", tema.cyan);
            UiKit.Tamano(avance, flexAncho: 1, flexAlto: 1);
            if (animar) avance.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.14f;
            PiezasDelMuro.Cifra(ui, avance, medio, "0", " %", tema.cyan, 44, animar ? 0.3f : -1f);
            PiezasDelMuro.Barra(ui, avance, (float)(medio / 100.0), tema.cyan, animar ? 0.3f : -1f, 8);

            // ---- una tarjeta por parte del sistema
            var n = modulos.Count;
            var cols = n <= 4 ? 2 : 3;
            for (var i = 0; i < n; i += cols) {
                var fila = ui.Fila(m.Cuerpo, "Fila " + (i / cols + 1), tema.espacio, 0, TextAnchor.UpperLeft);
                UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1);
                fila.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
                for (var k = i; k < System.Math.Min(n, i + cols); k++) Tarjeta(app, m, ui, tema, fila, modulos[k], est, animar, 3 + k);
            }

            // ---- abajo: lo que acaba de pasar, o lo que dice quien la va a usar
            var voz = s.VocesDelProducto().FirstOrDefault(v => v.ModuloId == est.Elegido) ?? s.VocesDelProducto().FirstOrDefault();
            if (!string.IsNullOrEmpty(est.Mensaje) || voz != null) {
                var globo = PiezasDelMuro.Panel(ui, m.Cuerpo, null, string.IsNullOrEmpty(est.Mensaje) ? tema.cyan : tema.warning, 14);
                UiKit.Tamano(globo, flexAncho: 1, alto: 96);
                if (animar) globo.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.5f;
                var cuerpo = ui.Fila(globo, "Voz", tema.espacio * 1.5f, 0, TextAnchor.MiddleLeft);
                UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
                if (string.IsNullOrEmpty(est.Mensaje)) RostroDePersonaje.Crear(ui, cuerpo, MaterialesNexus.IdDePersonaje(voz.Quien), voz.Quien, 64, tema.cyan);
                var texto = string.IsNullOrEmpty(est.Mensaje) ? voz.Quien + ": «" + voz.Frase + "»" : est.Mensaje;
                var t = ui.Texto(cuerpo, texto, EstiloTexto.Cuerpo, string.IsNullOrEmpty(est.Mensaje) ? tema.cyan : tema.warning, TextAlignmentOptions.MidlineLeft);
                var alto = t.GetComponent<AltoDeTexto>();
                if (alto != null) Object.Destroy(alto);
                t.enableAutoSizing = true; t.fontSizeMin = 13; t.fontSizeMax = 22;
                t.textWrappingMode = TextWrappingModes.Normal;
                UiKit.Tamano(t, flexAncho: 1, flexAlto: 1);
            }
        }

        private static void Tarjeta(AppRoot app, PantallaDelMuro m, UiKit ui, NexusTheme tema, RectTransform fila, EstadoDelModulo e, Estado est, bool animar, int indice) {
            var color = ColorDe(tema, e.Etapa);
            var elegido = est.Elegido == e.Modulo.Id;
            var tarjeta = PiezasDelMuro.Panel(ui, fila, e.Modulo.Nombre, elegido ? tema.cyan : color, 16);
            UiKit.Tamano(tarjeta, flexAncho: 1, flexAlto: 1);
            if (animar) tarjeta.gameObject.AddComponent<EntradaDePieza>().Retraso = System.Math.Min(8, indice) * 0.06f;
            if (elegido) ui.Borde(tarjeta, tema.cyan, NexusTheme.RadioLg, 2);

            // El avance en grande y la etapa.
            var cima = ui.Fila(tarjeta, "Cima", tema.espacio, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(cima, flexAncho: 1);
            PiezasDelMuro.Cifra(ui, cima, e.Porcentaje, "0", " %", color, 44, animar ? 0.2f + indice * 0.05f : -1f);
            var etapa = ui.Chip(cima, EtapasDelProducto.Nombre(e.Etapa), NexusTheme.Alfa(color, 0.18f), color);
            UiKit.Tamano(etapa, flexAncho: 0);

            // La barra por fases: boceto · mockup · prototipo · MVP.
            var orden = System.Math.Min(3, EtapasDelProducto.Orden(e.Etapa) - 0);
            PiezasDelMuro.Barra(ui, tarjeta, (orden + 1) / 4f, color, animar ? 0.3f + indice * 0.05f : -1f, 10);
            var nombres = ui.Fila(tarjeta, "Fases", 0, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(nombres, flexAncho: 1);
            var fases = new[] { "boceto", "mockup", "prototipo", "MVP" };
            for (var i = 0; i < fases.Length; i++) {
                var t = ui.Texto(nombres, fases[i], EstiloTexto.Leyenda, i == orden ? color : NexusTheme.Alfa(tema.inkMuted, i < orden ? 0.8f : 0.45f), i == 0 ? TextAlignmentOptions.Left : i == fases.Length - 1 ? TextAlignmentOptions.Right : TextAlignmentOptions.Center);
                UiKit.Tamano(t, flexAncho: 1);
            }

            // Pulsar la parte: hace lo suyo (y se dice abajo).
            var viva = tarjeta.gameObject.AddComponent<PiezaViva>();
            viva.Escala = 1.02f;
            viva.AlPulsar = () => { Usar(app, e, est); m.Repintar(); };
            tarjeta.gameObject.GetComponent<Image>().raycastTarget = true;
        }

        private static string Minuscula(string s) { return string.IsNullOrEmpty(s) ? "" : char.ToLowerInvariant(s[0]) + s.Substring(1); }

        /// <summary>Lo que hace cada etapa al tocarla: un boceto no responde, un prototipo responde sin guardar, un MVP guarda y se nota en los demas.</summary>
        private static void Usar(AppRoot app, EstadoDelModulo e, Estado est) {
            est.Elegido = e.Modulo.Id;
            var nombre = "«" + e.Modulo.Nombre + "»";
            var todos = app.Sesion.Producto();
            if (e.Etapa == EtapasDelProducto.Boceto) est.Mensaje = nombre + " es todavía un boceto: " + Minuscula(EtapasDelProducto.QueEs(e.Etapa));
            else if (e.Etapa == EtapasDelProducto.Mockup) est.Mensaje = nombre + " es un mockup: " + Minuscula(EtapasDelProducto.QueEs(e.Etapa));
            else if (e.Etapa == EtapasDelProducto.Prototipo) est.Mensaje = nombre + " es un prototipo: el botón responde, pero no guarda nada. Cierra y vuelve a abrir: no queda rastro.";
            else if (string.IsNullOrEmpty(e.Modulo.Accion)) {
                var conAccion = todos.FirstOrDefault(x => Funciona(x) && !string.IsNullOrEmpty(x.Modulo.Accion));
                est.Mensaje = conAccion == null
                    ? nombre + " ya funciona, pero muestra lo que entra por otro módulo que todavía no está listo. Las partes se necesitan unas a otras."
                    : nombre + " muestra lo que pasa en el sistema. Prueba «" + conAccion.Modulo.Accion + "» en «" + conAccion.Modulo.Nombre + "» y vuelve a mirar aquí.";
            } else {
                est.Registros++;
                var seEnteran = todos.Where(x => x.Modulo.Id != e.Modulo.Id && Funciona(x)).Select(x => "«" + x.Modulo.Nombre + "»").ToList();
                var sinEnterarse = todos.Where(x => x.Modulo.Id != e.Modulo.Id && !Funciona(x)).Select(x => "«" + x.Modulo.Nombre + "»").ToList();
                est.Mensaje = "«" + e.Modulo.Accion + "»: hecho, y guardado. " +
                              (seEnteran.Count > 0 ? "Mira " + string.Join(", ", seEnteran) + ": ya lo reflejan. " : "") +
                              (sinEnterarse.Count > 0 ? string.Join(", ", sinEnterarse) + " aún no, porque no están terminados." : "");
            }
        }
    }
}
