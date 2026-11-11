#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Corregir;
using Nexus.Core;
using Nexus.Unity.Pantallas.Minijuegos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// SOLO EN EL EDITOR (2026-11-09). Lo pone en marcha «Nexus > Capturar minijuego de diagrama»: abre el diagrama con
    /// cartas como practica (MJ-F1-07, con tres problemas y dos señuelos, y MJ-F0-02, sin defectos), y guarda un PNG a
    /// 1920×1080 de cada momento: la receta, la entrada, un punto elegido, una carta volando, una puesta, todo decidido,
    /// el revelado y el cierre. En el informe, los textos que se salen de su caja. Todo va a Library/capturas-diagrama/.
    /// </summary>
    public sealed class CapturadorDeDiagrama : MonoBehaviour {
        public const string IdDePerfil = "capturas-diagrama";
        public static int Fallos { get; private set; }
        public static string Carpeta { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "capturas-diagrama")); } }

        private readonly StringBuilder _informe = new StringBuilder();
        private Camera _camara;
        private RenderTexture _rt;

        private IEnumerator Start() {
            Fallos = 0;
            Directory.CreateDirectory(Carpeta);
            foreach (var viejo in Directory.GetFiles(Carpeta, "*.png")) File.Delete(viejo);
            while (AppRoot.Instancia == null || AppRoot.Instancia.Router == null) yield return null;
            yield return null; yield return null;
            var app = AppRoot.Instancia;
            PrepararCamara(app);
            if (app.CapaGuia != null) app.CapaGuia.gameObject.SetActive(false);
            app.SeleccionarPerfil(new PlayerProfile { idPerfil = IdDePerfil, nombreEstudiante = "Capturas", preTestHecho = true });

            foreach (var id in new[] { "MJ-F1-07", "MJ-F0-02" }) {
                MinijuegoDef def = null;
                try { def = CatalogoMinijuegos.Parsear(new CatalogoDeArchivos(RutasDeGuardado.Contenido).LeerCatalogo("minijuegos/" + id + ".json")); }
                catch (System.Exception ex) { Anotar("ERROR al cargar " + id + ": " + ex.Message); Fallos++; }
                if (def == null) continue;
                var terminado = false;
                var p = app.Router.Apilar<PantallaCorregirDiagrama>(x => {
                    x.Def = def; x.Practica = true; x.AlTerminar = r => { terminado = true; app.Router.Volver(); };
                });
                yield return Esperar(1.2f);
                yield return Capturar(app, id + "_0_receta");
                // «¡A jugar!» (la receta puede tener dos paginas, y detras esta el boton de la propia pantalla).
                for (var intento = 0; intento < 4 && p.Puntos == null; intento++) {
                    if (!Pulsar(app, "jugar") && !Pulsar(app, "Cómo se juega")) break;
                    yield return Esperar(0.6f);
                }
                yield return Esperar(0.3f);
                yield return Capturar(app, id + "_1_entrada");
                yield return Esperar(3.2f);
                // La primera vez se abre el tutorial en globos: se fotografia, se avanza un paso y se salta.
                yield return Capturar(app, id + "_2_tutorial_1");
                Pulsar(app, "Siguiente");
                yield return Esperar(0.5f);
                yield return Capturar(app, id + "_2_tutorial_2");
                Pulsar(app, "Saltar");
                yield return Esperar(0.5f);
                yield return Capturar(app, id + "_2_listo");
                if (p.Puntos == null || p.Puntos.Count == 0) { Anotar(id + ": el reto no arranco"); Fallos++; app.Router.Volver(); continue; }

                var ganadora = PuntosDelDiagrama.SolucionGanadora(def);
                var puntos = p.Puntos.ToList();
                p.ElegirPunto(puntos[0].Id);
                yield return Esperar(0.8f);
                yield return Capturar(app, id + "_3_punto");
                p.JugarCarta(ganadora[puntos[0].Id]);
                yield return Esperar(0.25f);
                yield return Capturar(app, id + "_4_vuelo");
                yield return Esperar(0.9f);
                yield return Capturar(app, id + "_5_puesta");
                // El resto: el segundo punto, a proposito con una carta que no toca (para ver la × al revelar).
                var arreglan = new HashSet<string>(def.Zonas.SelectMany(z => z.Soluciones));
                var mala = def.Cartas.Select(c => c.Id).FirstOrDefault(c => !arreglan.Contains(c));
                for (var i = 1; i < puntos.Count; i++) {
                    p.ElegirPunto(puntos[i].Id);
                    yield return Esperar(0.4f);
                    p.JugarCarta(i == 1 && mala != null ? mala : ganadora[puntos[i].Id]);
                    yield return Esperar(1.3f);
                }
                yield return Capturar(app, id + "_6_todo");
                p.Verificar();
                yield return Esperar(1.5f);
                yield return Capturar(app, id + "_7_revelando");
                yield return Esperar(puntos.Count * 0.75f + 1.6f);
                yield return Capturar(app, id + "_8_cierre");
                Pulsar(app, "Volver a la jornada");
                yield return Esperar(0.6f);
                if (!terminado && app.Router.Actual == p) app.Router.Volver();
                yield return Esperar(0.4f);
            }
            Terminar();
        }

        private static IEnumerator Esperar(float s) { yield return new WaitForSecondsRealtime(s); }

        private static bool Pulsar(AppRoot app, string texto) {
            var actual = app.Router.Actual;
            if (actual == null) return false;
            var boton = actual.GetComponentsInChildren<Button>()
                .FirstOrDefault(b => b.interactable && b.GetComponentsInChildren<TMP_Text>().Any(t => t.text.IndexOf(texto, System.StringComparison.OrdinalIgnoreCase) >= 0));
            if (boton == null) return false;
            boton.onClick.Invoke();
            return true;
        }

        private void PrepararCamara(AppRoot app) {
            _rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            _camara = new GameObject("Camara de capturas").AddComponent<Camera>();
            _camara.targetTexture = _rt;
            _camara.clearFlags = CameraClearFlags.SolidColor;
            _camara.backgroundColor = Color.black;
            _camara.orthographic = true;
            _camara.cullingMask = ~0;
            var lienzo = app.GetComponentInChildren<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceCamera;
            lienzo.worldCamera = _camara;
            lienzo.planeDistance = 10;
        }

        private IEnumerator Capturar(AppRoot app, string nombre) {
            yield return null;
            Canvas.ForceUpdateCanvases();
            Auditar(app, nombre);
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

        private void Auditar(AppRoot app, string nombre) {
            var pantalla = app.Router.Actual;
            if (pantalla == null) { Anotar(nombre + ": no hay pantalla"); Fallos++; return; }
            Anotar("== " + nombre + " (" + pantalla.GetType().Name + ")");
            foreach (var t in pantalla.GetComponentsInChildren<TMP_Text>()) {
                if (!t.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(t.text)) continue;
                var grupo = t.GetComponentInParent<CanvasGroup>();
                if (grupo != null && grupo.alpha < 0.05f) continue;
                t.ForceMeshUpdate();
                var r = t.rectTransform.rect;
                if (t.isTextOverflowing) { Anotar("   TRUNCADO: " + Ruta(t.transform) + " «" + Corto(t.text) + "»"); Fallos++; continue; }
                if (t.overflowMode == TextOverflowModes.Overflow && r.height > 1 && t.preferredHeight > r.height + 3) {
                    Anotar("   SE SALE: " + Ruta(t.transform) + " pide " + t.preferredHeight.ToString("0") + " y tiene " + r.height.ToString("0") + " «" + Corto(t.text) + "»");
                    Fallos++;
                }
            }
        }

        private void Terminar() {
            Anotar("FALLOS: " + Fallos);
            File.WriteAllText(Path.Combine(Carpeta, "informe.txt"), _informe.ToString());
            Debug.Log("[Capturas diagrama] " + Carpeta + "\n" + _informe);
            try {
                var dir = Path.Combine(RutasDeGuardado.Raiz, "perfiles", IdDePerfil);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            } catch (System.Exception ex) { Debug.LogWarning("[Capturas diagrama] no se pudo borrar el perfil de prueba: " + ex.Message); }
            UnityEditor.EditorApplication.ExitPlaymode();
        }

        private void Anotar(string linea) { _informe.AppendLine(linea); }

        private static string Corto(string s) { s = s.Replace("\n", " "); return s.Length > 60 ? s.Substring(0, 60) + "…" : s; }

        private static string Ruta(Transform t) {
            var partes = new List<string>();
            for (var x = t; x != null && partes.Count < 6; x = x.parent) partes.Add(x.name);
            partes.Reverse();
            return string.Join("/", partes);
        }
    }
}
#endif
