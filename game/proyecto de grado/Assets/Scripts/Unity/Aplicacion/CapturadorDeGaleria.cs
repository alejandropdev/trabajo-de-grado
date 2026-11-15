#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nexus.Core.Narrativa;
using Nexus.Unity.Pantallas;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// SOLO EN EL EDITOR (2026-11-14, auditoria de arte). Lo pone en marcha «Nexus > Capturar Galeria 2D»: pinta cada
    /// dibujo hecho por codigo fuera de su pantalla (escenas de pizarra, ventana del atardecer, despliegue, edificio,
    /// puente, suministros, iconos, hallazgos, pausa, entrevista y las piezas del kit) sobre una capa a pantalla completa
    /// y guarda un PNG a 1920×1080 de cada lamina. No toca partidas ni pantallas: la capa se crea y se destruye aqui.
    /// Todo va a Library/capturas-galeria/.
    /// </summary>
    public sealed partial class CapturadorDeGaleria : MonoBehaviour {
        public static int Fallos { get; private set; }
        public static string Carpeta { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "capturas-galeria")); } }

        private readonly StringBuilder _informe = new StringBuilder();
        private Camera _camara;
        private RenderTexture _rt;
        private Canvas _lienzo;
        private UiKit _ui;

        private IEnumerator Start() {
            Fallos = 0;
            Directory.CreateDirectory(Carpeta);
            // Con filtro (NEXUS_GALERIA) solo se borran las laminas que se van a repetir; sin filtro, todas.
            var filtroInicial = Environment.GetEnvironmentVariable("NEXUS_GALERIA");
            foreach (var viejo in Directory.GetFiles(Carpeta, "*.png"))
                if (string.IsNullOrEmpty(filtroInicial) || Path.GetFileName(viejo).Contains(filtroInicial)) File.Delete(viejo);
            while (AppRoot.Instancia == null || AppRoot.Instancia.Router == null) yield return null;
            yield return null; yield return null;
            var app = AppRoot.Instancia;
            _ui = app.Ui;
            PrepararCamara(app);
            if (app.CapaGuia != null) app.CapaGuia.gameObject.SetActive(false);

            var paginas = new List<KeyValuePair<string, Action<RectTransform>>>();
            // Primero las laminas de las propuestas de direccion de arte (CapturadorDeGaleria.Propuestas.cs).
            paginas.Add(Par("ronda2_1_bioluminiscencia", Ronda2_1));
            paginas.Add(Par("ronda2_2_streamline", Ronda2_2));
            paginas.Add(Par("ronda2_3_diorama", Ronda2_3));
            paginas.Add(Par("ronda2_4_linterna", Ronda2_4));
            paginas.Add(Par("ronda2_5_cartoon", Ronda2_5));
            paginas.Add(Par("ronda2_6_valvula", Ronda2_6));
            paginas.Add(Par("propuesta_A_senaletica", PropuestaA));
            paginas.Add(Par("propuesta_B_plano", PropuestaB));
            paginas.Add(Par("propuesta_C_serigrafia", PropuestaC));
            // Tanda piloto de las pizarras en el lenguaje nuevo, a tamano de pizarron (NEXUS_GALERIA=piloto).
            var piloto = EscenasDePizarra.Todas.Where(id => Prefijos().Any(id.StartsWith)).ToArray();
            for (var i = 0; i < piloto.Length; i += 4) {
                var trozo = piloto.Skip(i).Take(4).ToList();
                paginas.Add(Par("piloto_metricas_" + (i / 4 + 1), r => PizarrasGrandes(r, trozo)));
            }
            var escenas = EscenasDePizarra.Todas.ToList();
            for (var i = 0; i < escenas.Count; i += 9) {
                var trozo = escenas.Skip(i).Take(9).ToList();
                paginas.Add(Par("pizarra_" + (i / 9 + 1).ToString("00"), r => Pizarras(r, trozo)));
            }
            // El pizarron de verdad, con paginas del glosario (NEXUS_GALERIA=pizarron).
            foreach (var (termino, pagina) in new[] { ("avance", 0), ("cansancio", 1), ("avance", 4), ("avance", 5), ("cansancio", 4) }) {
                var t = termino; var n = pagina;
                paginas.Add(Par("pizarron_" + t + "_" + (n + 1), r => Pizarron(r, t, n)));
            }
            paginas.Add(Par("atardecer", Atardecer));
            paginas.Add(Par("despliegue", Despliegue));
            paginas.Add(Par("edificio", Edificio));
            paginas.Add(Par("puente", Puente));
            paginas.Add(Par("suministros", Suministros));
            paginas.Add(Par("iconos_soluciones_etapas", Iconos));
            paginas.Add(Par("hallazgos", Hallazgos));
            paginas.Add(Par("pausa", Pausa));
            paginas.Add(Par("entrevista", Entrevista));
            paginas.Add(Par("kit_botones", KitBotones));
            paginas.Add(Par("kit_tarjetas", KitTarjetas));

            // NEXUS_GALERIA=propuesta (o cualquier trozo de nombre) limita la galeria a esas paginas.
            var filtro = Environment.GetEnvironmentVariable("NEXUS_GALERIA");
            if (!string.IsNullOrEmpty(filtro)) paginas = paginas.Where(p => p.Key.Contains(filtro)).ToList();

            foreach (var p in paginas) {
                var capa = NuevaCapa(p.Key);
                var ok = true;
                try { p.Value(capa); }
                catch (Exception ex) { ok = false; Fallos++; Anotar("ERROR en " + p.Key + ": " + ex.GetType().Name + " " + ex.Message); }
                // Los dibujos que se trazan solos (Avance) y las entradas animadas necesitan unos segundos.
                yield return new WaitForSecondsRealtime(3.5f);
                yield return Capturar(p.Key);
                if (ok) Anotar("ok " + p.Key);
                if (_despues != null) { _despues(); _despues = null; yield return null; }
                capa.gameObject.SetActive(false);
                Destroy(capa.gameObject);
                yield return null;
            }
            Terminar();
        }

        private static KeyValuePair<string, Action<RectTransform>> Par(string n, Action<RectTransform> a) {
            return new KeyValuePair<string, Action<RectTransform>>(n, a);
        }

        // ================================================================ paginas

        private void Pizarras(RectTransform capa, List<string> ids) {
            for (var i = 0; i < ids.Count; i++) {
                float x = 30 + (i % 3) * 630, y = 20 + (i / 3) * 352;
                var fondo = Bloque(capa, x, y, 600, 300, Tizador.Pizarra);
                var lamina = Lamina(fondo, 0, 0, 600, 300);
                lamina.Dibujo.Tiza = true;
                DibujosDeEscena.Dibujar(new Tizador(lamina, _ui.FuenteTiza), ids[i]);
                Rotulo(capa, x, y + 302, 600, ids[i]);
            }
        }

        /// <summary>Que escenas entran en las paginas «piloto_…»: NEXUS_PREFIJOS (p. ej. «g-,d-»); por defecto, metricas y juego.</summary>
        private static string[] Prefijos() {
            var e = Environment.GetEnvironmentVariable("NEXUS_PREFIJOS");
            return string.IsNullOrEmpty(e) ? new[] { "m-", "j-" } : e.Split(',');
        }

        /// <summary>Hasta cuatro escenas a 1,45: lo que mide una escena a pagina completa en el pizarron.</summary>
        private void PizarrasGrandes(RectTransform capa, List<string> ids) {
            const float k = 1.45f, w = 600 * k, h = 300 * k;
            for (var i = 0; i < ids.Count; i++) {
                float x = 60 + (i % 2) * (w + 60), y = 40 + (i / 2) * (h + 70);
                var fondo = Bloque(capa, x, y, w, h, Tizador.Pizarra);
                var lamina = Lamina(fondo, 0, 0, w, h);
                lamina.Dibujo.Tiza = true;
                DibujosDeEscena.Dibujar(new Tizador(lamina, _ui.FuenteTiza, 0, 0, k), ids[i]);
                Rotulo(capa, x, y + h + 6, w, ids[i]);
            }
        }

        private void Atardecer(RectTransform capa) {
            var modos = new[] { VentanaDelAtardecer.Modo.Neutro, VentanaDelAtardecer.Modo.Casa, VentanaDelAtardecer.Modo.Quedarse };
            for (var i = 0; i < modos.Length; i++) {
                var col = Columna(capa, 160, 20 + i * 350, 1600, 300);
                var v = VentanaDelAtardecer.Crear(_ui, col, 17 * 60, 18 * 60, 20 * 60);
                v.Actual = modos[i];
                Rotulo(capa, 160, 322 + i * 350, 1600, "VentanaDelAtardecer · " + modos[i]);
            }
        }

        private void Despliegue(RectTransform capa) {
            var etapas = new[] { "compilar", "unitarias", "integracion", "seguridad", "empaquetar", "produccion" };
            var a = Columna(capa, 160, 40, 1600, 420);
            EscenaDelDespliegue.Crear(_ui, a, 2, false, etapas);
            Rotulo(capa, 160, 462, 1600, "EscenaDelDespliegue · 2 de 6");
            var b = Columna(capa, 160, 540, 1600, 420);
            EscenaDelDespliegue.Crear(_ui, b, etapas.Length, true, etapas);
            Rotulo(capa, 160, 962, 1600, "EscenaDelDespliegue · al cliente");
        }

        private void Edificio(RectTransform capa) {
            var l = Lamina(capa, 60, 40, 1800, 1000);
            var d = l.Dibujo;
            var t = _ui.Tema;
            var formas = new[] { "bloqueo", "cable", "enjambre", "fantasma", "ganzua", "integracion", "regresion", "rendimiento", "seguridad" };
            for (var i = 0; i < formas.Length; i++) {
                DibujosDelEdificio.Bicho(d, formas[i], 100 + i * 190, 120, 2.2f, t.danger);
                Rotulo(capa, 60 + i * 190, 260, 190, formas[i]);
            }
            DibujosDelEdificio.Red(d, 150, 450, 70, t.cyan, true);
            DibujosDelEdificio.Red(d, 350, 450, 70, t.cyan, false);
            DibujosDelEdificio.Moneda(d, 550, 450, 50);
            DibujosDelEdificio.Escudo(d, 750, 450, 2.5f, t.success);
            DibujosDelEdificio.Alerta(d, 950, 450, 2.5f, t.warning);
            DibujosDelEdificio.CajaDeVersion(d, 1150, 450, t.cyan);
            DibujosDelEdificio.GenteDelCliente(d, 1300, 520, t.ink, t.warning);
            DibujosDelEdificio.Antena(d, 1650, 560, t.inkMuted, t.danger);
            for (var i = 0; i < formas.Length; i++) DibujosDelEdificio.IconoDePiso(d, formas[i], 100 + i * 190, 750, t.cyan);
            Rotulo(capa, 60, 900, 1800, "DibujosDelEdificio · bichos, red, moneda, escudo, alerta, caja, gente, antena · iconos de piso");
        }

        private void Puente(RectTransform capa) {
            var l = Lamina(capa, 60, 40, 1800, 1000);
            var d = l.Dibujo;
            var t = _ui.Tema;
            DibujosDelPuente.Edificios(d, 0, 1800, 520, 300, 7, t.surfaceRaised, NexusTheme.Alfa(t.warning, 0.8f));
            DibujosDelPuente.Risco(d, true, 1800, 520, 420, 380, t.surface, t.lineStrong);
            DibujosDelPuente.Risco(d, false, 1800, 520, 420, 380, t.surface, t.lineStrong);
            DibujosDelPuente.Celosia(d, 380, 1420, 470, 520, t.cyan);
            DibujosDelPuente.Barandilla(d, 380, 1420, 470, 30, t.inkMuted);
            DibujosDelPuente.Camion(d, 420, 440, t.warning, t.cyan);
            DibujosDelPuente.PantallaHolografica(d, 800, 120, 260, 150, t.cyan, 3);
            DibujosDelPuente.Corazon(d, 200, 120, 3f, t.danger, t.ink);
            DibujosDelPuente.MitadDeCorazon(d, true, 340, 120, 3f, t.danger, t.ink);
            DibujosDelPuente.Candado(d, 520, 120, t.warning, 3f);
            Rotulo(capa, 60, 960, 1800, "DibujosDelPuente · edificios, riscos, celosia, barandilla, camion, holograma, corazones, candado");
        }

        private void Suministros(RectTransform capa) {
            // Como en el juego (PantallaDelDia.ImagenDeSuministro): la imagen de Resources/Ilustraciones/suministros y, solo
            // si falta, el dibujo de respaldo.
            var ids = new[] { "consumable_pizza", "consumable_coffee", "consumable_energy_drink", "otro" };
            for (var i = 0; i < ids.Length; i++) {
                for (var apagado = 0; apagado < 2; apagado++) {
                    var hueco = Bloque(capa, 120 + i * 440, 80 + apagado * 480, 360, 360, Color.clear);
                    var sprite = MaterialesNexus.Ilustracion("suministros/" + ids[i]);
                    RectTransform r;
                    if (sprite != null) {
                        r = _ui.Nodo(hueco, "Imagen");
                        var img = r.gameObject.AddComponent<Image>();
                        img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
                        img.color = apagado == 1 ? new Color(0.45f, 0.47f, 0.5f, 0.8f) : Color.white;
                    } else r = PiezasDelMuro.IlustracionDeSuministro(_ui, hueco, ids[i], 360, apagado == 1, false);
                    Colocar(r, 0, 0, 360, 360);
                    Rotulo(capa, 120 + i * 440, 445 + apagado * 480, 360, ids[i] + (apagado == 1 ? " · apagado" : ""));
                }
            }
        }

        private void Iconos(RectTransform capa) {
            var t = _ui.Tema;
            var soluciones = new[] { "acuerdo", "lista", "puerta", "adaptador", "candado", "escudo", "bitacora", "ojo", "ciclo", "antena",
                "plano", "rayo", "reintento", "uno", "cubos", "servidores", "llave", "comentario", "bifurcacion", "cola" };
            var l = Lamina(capa, 60, 30, 1800, 1020);
            for (var i = 0; i < soluciones.Length; i++) {
                float cx = 90 + (i % 10) * 180, cy = 90 + (i / 10) * 200;
                DibujosDeSoluciones.Dibujar(l.Dibujo, soluciones[i], cx, cy, 90, t.ink, t.cyan);
                Rotulo(capa, cx - 90 + 60, cy + 70 + 30, 180, soluciones[i]);
            }
            var etapas = new[] { "compilar", "unitarias", "pruebas", "integracion", "seguridad", "empaquetar", "entorno-de-pruebas",
                "visto-bueno", "produccion", "vigilancia", "directo", "probar-en-produccion", "a-mano" };
            for (var i = 0; i < etapas.Length; i++) {
                float cx = 90 + (i % 7) * 250, cy = 560 + (i / 7) * 220;
                DibujosDeEtapas.Dibujar(l.Dibujo, etapas[i], cx, cy, 90, t.ink, t.warning);
                Rotulo(capa, cx - 125 + 60, cy + 70 + 30, 250, etapas[i]);
            }
        }

        private void Hallazgos(RectTransform capa) {
            var t = _ui.Tema;
            // Los emblemas de las cartas se dibujan en 240×220 y los iconos de advertencia en 120×120: cada uno en su
            // cuadro de 200×200, a la escala que le toca.
            var todos = DibujosDeHallazgos.Emblemas.Select(e => new KeyValuePair<string, Action<Tizador, Color>>("emblema " + e.Key, e.Value))
                .Concat(DibujosDeHallazgos.IconosDeAdvertencia.Select(e => new KeyValuePair<string, Action<Tizador, Color>>("aviso " + e.Key, e.Value)))
                .Take(32).ToList();
            for (var i = 0; i < todos.Count; i++) {
                float x = 40 + (i % 8) * 232, y = 20 + (i / 8) * 262;
                var fondo = Bloque(capa, x, y, 200, 200, Tizador.Pizarra);
                var lamina = Lamina(fondo, 0, 0, 200, 200);
                lamina.Dibujo.Tiza = true;
                var esEmblema = todos[i].Key.StartsWith("emblema", StringComparison.Ordinal);
                var k = esEmblema ? 200f / 240f : 200f / 120f;
                todos[i].Value(new Tizador(lamina, _ui.FuenteTiza, 0, esEmblema ? 8 : 0, k), t.warning);
                Rotulo(capa, x - 16, y + 202, 232, todos[i].Key);
            }
        }

        private void Pausa(RectTransform capa) {
            var tipos = (PictogramasDePausa.Tipo[])Enum.GetValues(typeof(PictogramasDePausa.Tipo));
            for (var i = 0; i < tipos.Length; i++) {
                var l = Lamina(capa, 120 + i * 290, 380, PictogramasDePausa.Lado, PictogramasDePausa.Lado);
                l.Raiz.localScale = Vector3.one * 3.5f;
                PictogramasDePausa.Dibujar(l.Dibujo, tipos[i], _ui.Tema.cyan);
                Rotulo(capa, 80 + i * 290, 640, 290, tipos[i].ToString());
            }
        }

        private void Entrevista(RectTransform capa) {
            var col = Columna(capa, 760, 20, 400, 1040);
            EdificioDeLaEntrevista.Crear(_ui, col, 15, 3);
        }

        private void KitBotones(RectTransform capa) {
            var col = Columna(capa, 160, 40, 700, 1000);
            foreach (VarianteBoton v in Enum.GetValues(typeof(VarianteBoton))) {
                var b = _ui.Boton(col, "Boton " + v, () => { }, v);
                UiKit.Tamano(b, alto: 70);
            }
            var col2 = Columna(capa, 1000, 40, 760, 1000);
            foreach (Tono tono in Enum.GetValues(typeof(Tono))) _ui.OpcionDeEtiqueta(col2, "Opcion " + tono, tono, () => { }, "detalle de la opcion");
            var fila = _ui.Fila(col2, "Badges");
            foreach (Tono tono in Enum.GetValues(typeof(Tono))) _ui.Badge(fila, tono.ToString(), tono);
            var fila2 = _ui.Fila(col2, "Badges solidos");
            foreach (Tono tono in Enum.GetValues(typeof(Tono))) _ui.Badge(fila2, tono.ToString(), tono, true);
        }

        private void KitTarjetas(RectTransform capa) {
            var tonos = (Tono[])Enum.GetValues(typeof(Tono));
            for (var i = 0; i < tonos.Length; i++) {
                var col = Columna(capa, 60 + (i % 3) * 610, 40 + (i / 3) * 380, 580, 340);
                var tarjeta = _ui.Tarjeta(col, "Tarjeta " + tonos[i], tonos[i]);
                UiKit.Tamano(tarjeta, flexAncho: 1, flexAlto: 1);
                _ui.Texto(tarjeta, "Texto de cuerpo dentro de la tarjeta, para ver jerarquia y contraste.");
                _ui.Barra(tarjeta, 0.62f);
            }
            var fila = Columna(capa, 60, 820, 1800, 200);
            var f = _ui.Fila(fila, "Diales");
            _ui.Dial(f, 0.3f, "Tiempo");
            _ui.Dial(f, 0.7f, "Costo", _ui.Tema.warning);
            _ui.Dial(f, 0.9f, "Calidad", _ui.Tema.success);
            _ui.Chip(f, "chip", _ui.Tema.surfaceRaised);
        }

        // ================================================================ piezas de maquetacion

        private RectTransform NuevaCapa(string nombre) {
            var capa = _ui.Nodo(_lienzo.transform, "Galeria · " + nombre);
            UiKit.Rellenar(capa);
            capa.SetAsLastSibling();
            var img = capa.gameObject.AddComponent<Image>();
            img.color = _ui.Tema.bg950;
            return capa;
        }

        private static void Colocar(RectTransform r, float x, float y, float w, float h) {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }

        private RectTransform Bloque(RectTransform padre, float x, float y, float w, float h, Color color) {
            var r = _ui.Nodo(padre, "Bloque");
            Colocar(r, x, y, w, h);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return r;
        }

        private Lamina Lamina(RectTransform padre, float x, float y, float w, float h) {
            var l = new Lamina(_ui, padre, w, h);
            Colocar(l.Raiz, x, y, w, h);
            return l;
        }

        private RectTransform Columna(RectTransform padre, float x, float y, float w, float h) {
            var c = _ui.Columna(padre, "Columna");
            Colocar(c, x, y, w, h);
            return c;
        }

        private void Rotulo(RectTransform padre, float x, float y, float w, string texto) {
            var t = _ui.Texto(padre, texto, EstiloTexto.Leyenda, _ui.Tema.inkMuted, TMPro.TextAlignmentOptions.Top);
            Colocar(t.rectTransform, x, y, w, 30);
        }

        // ================================================================ captura

        private void PrepararCamara(AppRoot app) {
            _rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            _camara = new GameObject("Camara de capturas").AddComponent<Camera>();
            _camara.targetTexture = _rt;
            _camara.clearFlags = CameraClearFlags.SolidColor;
            _camara.backgroundColor = Color.black;
            _camara.orthographic = true;
            _camara.cullingMask = ~0;
            _lienzo = app.GetComponentInChildren<Canvas>();
            _lienzo.renderMode = RenderMode.ScreenSpaceCamera;
            _lienzo.worldCamera = _camara;
            _lienzo.planeDistance = 10;
        }

        private IEnumerator Capturar(string nombre) {
            yield return null;
            Canvas.ForceUpdateCanvases();
            _camara.Render();
            var antes = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = antes;
            File.WriteAllBytes(Path.Combine(Carpeta, nombre + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }

        private void Terminar() {
            Anotar("FALLOS: " + Fallos);
            File.WriteAllText(Path.Combine(Carpeta, "informe.txt"), _informe.ToString());
            Debug.Log("[Capturas galeria] " + Carpeta + "\n" + _informe);
            UnityEditor.EditorApplication.ExitPlaymode();
        }

        private void Anotar(string linea) { _informe.AppendLine(linea); }
    }
}
#endif
