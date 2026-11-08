#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nexus.Core.Guardado;
using Nexus.Core.Modelo;
using Nexus.Core;
using Nexus.Core.Sesion;
using Nexus.Unity.Pantallas;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// SOLO EN EL EDITOR (2026-11-08). Lo pone en marcha «Nexus > Capturar Fase 1» (o el mismo metodo en batchmode):
    /// abre una partida de usar y tirar en los dos primeros niveles, recorre la Metodologia (sus tres partes, con cada
    /// metodologia) y la Calidad (tarjetas y tabla), y de cada pantalla guarda un PNG a 1920×1080 y un informe con
    /// los textos que se salen de su caja, los scrolls con contenido escondido y cuanto tuvo que encogerse cada lienzo.
    /// Todo va a Library/capturas-fase1/. Al terminar borra la partida y el perfil de prueba.
    /// </summary>
    public sealed class CapturadorDeFase1 : MonoBehaviour {
        public const string IdDePerfil = "capturas-fase1";
        public static int Fallos { get; private set; }
        /// <summary>En Library (ignorada por git): Temp la vacia el propio Unity al entrar en Play.</summary>
        public static string Carpeta { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "capturas-fase1")); } }

        private readonly StringBuilder _informe = new StringBuilder();
        private Camera _camara;
        private RenderTexture _rt;

        private IEnumerator Start() {
            Fallos = 0;
            Debug.Log("[Capturas] empiezo en " + Carpeta);
            Directory.CreateDirectory(Carpeta);
            foreach (var viejo in Directory.GetFiles(Carpeta, "*.png")) File.Delete(viejo);
            while (AppRoot.Instancia == null || AppRoot.Instancia.Router == null) yield return null;
            yield return null; yield return null;
            var app = AppRoot.Instancia;
            if (app.Catalogo == null) { Anotar("ERROR: el catalogo no cargo."); Fallos++; Terminar(); yield break; }

            PrepararCamara(app);
            if (app.CapaGuia != null) app.CapaGuia.gameObject.SetActive(false);
            app.SeleccionarPerfil(new PlayerProfile { idPerfil = IdDePerfil, nombreEstudiante = "Capturas", preTestHecho = true });

            var primero = ProgresionDeNiveles.Primero(app.Catalogo);
            var niveles = new List<string> { primero };
            var segundo = ProgresionDeNiveles.Siguiente(app.Catalogo, primero);
            if (segundo != null) niveles.Add(segundo);

            foreach (var nivel in niveles) {
                SaveGame partida = null;
                try {
                    partida = app.CrearPartida("capturas");
                    partida.Partida.NivelActualId = nivel;
                    partida.Partida.NivelAndamiaje = app.Catalogo.Niveles[nivel].NivelAndamiaje;
                    app.AbrirPartida(partida);
                    app.Sesion.R.IntroVista = true;
                } catch (System.Exception ex) {
                    Anotar("ERROR al abrir el nivel " + nivel + ": " + ex.Message); Fallos++;
                    continue;
                }
                // El proyecto: sus tres partes (la tercera, jugando y ya hecha).
                var perfilDelNivel = app.Sesion.Perfil;
                var preguntas = perfilDelNivel.Proyecto != null
                    ? Nexus.Core.Proyecto.PreguntasDelProyecto.Para(perfilDelNivel.Proyecto, perfilDelNivel.Id)
                    : new List<Nexus.Core.Proyecto.PreguntaDelProyecto>();
                var comprobadas = preguntas.ToDictionary(q => q.Id, q => q.Correcta);
                if (perfilDelNivel.Proyecto != null) {
                    yield return Capturar(app, nivel + "_p0_cliente", new Fase1Borrador { Paso = "Encargo", SubPaso = 0 }, 3.4f);
                    yield return Capturar(app, nivel + "_p1_sistema", new Fase1Borrador { Paso = "Encargo", SubPaso = 1 }, 3.4f);
                    yield return Capturar(app, nivel + "_p2_juego", new Fase1Borrador { Paso = "Encargo", SubPaso = 2 }, 2.6f);
                    yield return Capturar(app, nivel + "_p2_hecho", new Fase1Borrador { Paso = "Encargo", SubPaso = 2, Comprobacion = comprobadas }, 2.6f);
                }
                // La recoleccion: antes; y despues de un recorrido con todas las hojas de vida y algunos suministros.
                if (app.Sesion.Recoleccion != null) {
                    yield return Capturar(app, nivel + "_r0_antes", new Fase1Borrador { Paso = "Recoleccion" }, 2.8f);
                    var entrada = app.Sesion.EntradaDeRecoleccion(null);
                    var resultado = new Nexus.Core.Fase1.ResultadoDeRecoleccion { NivelId = nivel, MinutosUsados = 4 };
                    foreach (var o in entrada.Objetos) {
                        if (o.Tipo == Nexus.Core.Fase1.TiposDeObjeto3D.Candidato) resultado.Objetos[o.Id] = 1;
                        else if (o.Tipo == Nexus.Core.Fase1.TiposDeObjeto3D.Suministro) resultado.Objetos[o.Id] = o.Id.Contains("pizza") ? 2 : 1;
                        else if (o.Tipo == Nexus.Core.Fase1.TiposDeObjeto3D.Coleccionable && resultado.Objetos.Count < 12) resultado.Objetos[o.Id] = 1;
                    }
                    try { app.Sesion.AplicarRecoleccion(resultado); } catch (System.Exception ex) { Anotar("aviso al aplicar la recoleccion: " + ex.Message); }
                    yield return Capturar(app, nivel + "_r1_despues", new Fase1Borrador { Paso = "Recoleccion" }, 3.2f);
                }
                var mets = app.Sesion.MetodologiasDisponibles();
                yield return Capturar(app, nivel + "_m0_elegir", new Fase1Borrador { Paso = "Metodologia", SubPaso = 0 });
                foreach (var m in mets) {
                    var razon = m.TextosDeRazones.Keys.OrderBy(k => k).FirstOrDefault();
                    yield return Capturar(app, nivel + "_m0_" + m.Id + "_elegida", new Fase1Borrador { Paso = "Metodologia", SubPaso = 0, Metodologia = m.Id });
                    yield return Capturar(app, nivel + "_m1_" + m.Id, new Fase1Borrador { Paso = "Metodologia", SubPaso = 1, Metodologia = m.Id });
                    yield return Capturar(app, nivel + "_m1_" + m.Id + "_razon", new Fase1Borrador { Paso = "Metodologia", SubPaso = 1, Metodologia = m.Id, RazonMetodologia = razon }, 4.5f);
                    yield return Capturar(app, nivel + "_m2_" + m.Id, new Fase1Borrador { Paso = "Metodologia", SubPaso = 2, Metodologia = m.Id, RazonMetodologia = razon });
                }
                yield return Capturar(app, nivel + "_a0_elegir", new Fase1Borrador { Paso = "Arquitectura", SubPaso = 0 }, 3.2f);
                foreach (var a in app.Sesion.Perfil.Fase1.Arquitecturas) {
                    var razonA = app.Sesion.Perfil.Fase1.RazonesDe(a).Select(r => r.Id).FirstOrDefault();
                    yield return Capturar(app, nivel + "_a1_" + a.Id, new Fase1Borrador { Paso = "Arquitectura", SubPaso = 1, Arquitectura = a.Id });
                    yield return Capturar(app, nivel + "_a1_" + a.Id + "_razon_desplegar", new Fase1Borrador { Paso = "Arquitectura", SubPaso = 1, Arquitectura = a.Id, RazonArquitectura = razonA }, 11.5f);
                }
                var fichas = new Dictionary<string, int>();
                var atributos = app.Sesion.Perfil.Fase1.Calidad.Atributos;
                if (atributos.Count > 0) fichas[atributos[0].Id] = 2;
                yield return Capturar(app, nivel + "_calidad_tarjetas", new Fase1Borrador { Paso = "Calidad", Fichas = fichas });
                yield return Capturar(app, nivel + "_calidad_tabla", null, 2.5f, "Ver tabla completa");

                // El backlog: las dos partes (la segunda vacia y con dos elegidas) y, donde ya existe, la unica parte heredada.
                var proyecto = app.Sesion.Perfil.Proyecto;
                if (proyecto != null && !string.IsNullOrEmpty(proyecto.BacklogInicial)) {
                    yield return Capturar(app, nivel + "_backlog_p0", new Fase1Borrador { Paso = "Backlog", SubPaso = 0 }, 3.2f);
                    yield return Capturar(app, nivel + "_backlog_p1_vacio", new Fase1Borrador { Paso = "Backlog", SubPaso = 1 }, 2.2f);
                    if (proyecto.Historias.Count > 1)
                        yield return Capturar(app, nivel + "_backlog_p1_dos", new Fase1Borrador {
                            Paso = "Backlog", SubPaso = 1, Backlog = proyecto.Historias.Take(2).Select(h => h.Id).ToList() }, 2.2f);
                }
                // Los dialogos (cristal neon): se abren sobre la pantalla que hay.
                if (proyecto != null) {
                    DialogosDelExpediente.Documentos(app, proyecto);
                    yield return Capturar(app, nivel + "_dialogo_documentos", null, 1.2f);
                    DialogosDelExpediente.Vocabulario(app, proyecto);
                    yield return Capturar(app, nivel + "_dialogo_vocabulario", null, 1.2f);
                }

                // El resumen: a medias (sin decidir) y con todo decidido.
                yield return Capturar(app, nivel + "_resumen_a_medias", new Fase1Borrador { Paso = "Resumen", Fichas = fichas }, 3.2f);
                var fase1 = app.Sesion.Perfil.Fase1;
                var primeraMet = mets.FirstOrDefault();
                var primeraArq = fase1.Arquitecturas.FirstOrDefault();
                if (primeraMet != null && primeraArq != null) {
                    var completo = new Fase1Borrador {
                        Paso = "Resumen", Fichas = fichas, Comprobacion = comprobadas,
                        Metodologia = primeraMet.Id, RazonMetodologia = primeraMet.TextosDeRazones.Keys.OrderBy(k => k).FirstOrDefault(),
                        Arquitectura = primeraArq.Id, RazonArquitectura = fase1.RazonesDe(primeraArq).Select(r => r.Id).FirstOrDefault()
                    };
                    var proy = app.Sesion.Perfil.Proyecto;
                    if (proy != null && proy.Historias.Count > 0)
                        completo.Backlog = proy.Historias.Take(System.Math.Max(1, proy.Modulos.Count)).Select(h => h.Id).ToList();
                    yield return Capturar(app, nivel + "_resumen_completo", completo, 3.6f);
                }
                try { app.Partidas.Borrar(IdDePerfil, partida.Id); } catch (System.Exception ex) { Anotar("aviso: no se pudo borrar la partida: " + ex.Message); }
            }
            Terminar();
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

        /// <summary>
        /// Pone el borrador y abre la Fase 1 (si borrador es null, se queda en la pantalla que hay); con 'pulsar', pulsa
        /// el boton con ese texto. Espera a que se asiente y a que las animaciones avancen, audita y captura.
        /// </summary>
        private IEnumerator Capturar(AppRoot app, string nombre, Fase1Borrador borrador, float espera = 2.6f, string pulsar = null) {
            if (borrador != null) {
                app.Sesion.R.Fase1Borrador = borrador;
                app.Router.IrA<PantallaDeFase1>();
            }
            yield return new WaitForSecondsRealtime(0.6f);
            if (pulsar != null) {
                var boton = app.Router.Actual.GetComponentsInChildren<Button>()
                    .FirstOrDefault(b => b.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains(pulsar)));
                if (boton != null) boton.onClick.Invoke();
                else { Anotar(nombre + ": no encontre el boton «" + pulsar + "»"); Fallos++; }
            }
            yield return new WaitForSecondsRealtime(espera);
            // (sin WaitForEndOfFrame: en batchmode no se pinta ningun fotograma y nunca llega; la camara se pinta a mano)
            Canvas.ForceUpdateCanvases();
            Auditar(app, nombre);
            Guardar(nombre);
        }

        private void Auditar(AppRoot app, string nombre) {
            var pantalla = app.Router.Actual;
            if (pantalla == null) { Anotar(nombre + ": no hay pantalla"); Fallos++; return; }
            Anotar("== " + nombre + " (" + pantalla.GetType().Name + ")");
            foreach (var e in pantalla.GetComponentsInChildren<EscaladorAlCaber>())
                Anotar("   lienzo encogido a " + e.Escala.ToString("0.00"));
            foreach (var s in pantalla.GetComponentsInChildren<ScrollRect>()) {
                if (!s.vertical || s.viewport == null || s.content == null) continue;
                var sobra = s.content.rect.height - s.viewport.rect.height;
                if (sobra > 1) { Anotar("   SCROLL: " + Ruta(s.transform) + " esconde " + sobra.ToString("0") + " px"); Fallos++; }
            }
            foreach (var t in pantalla.GetComponentsInChildren<TMP_Text>()) {
                if (!t.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(t.text)) continue;
                var grupo = t.GetComponentInParent<CanvasGroup>();
                if (grupo != null && grupo.alpha < 0.05f) continue;   // invisible ahora mismo
                t.ForceMeshUpdate();
                var r = t.rectTransform.rect;
                if (t.isTextOverflowing) { Anotar("   TRUNCADO: " + Ruta(t.transform) + " «" + Corto(t.text) + "»"); Fallos++; continue; }
                if (t.overflowMode == TextOverflowModes.Overflow && r.height > 1 && t.preferredHeight > r.height + 3) {
                    Anotar("   SE SALE: " + Ruta(t.transform) + " pide " + t.preferredHeight.ToString("0") + " de alto y tiene " + r.height.ToString("0") + " «" + Corto(t.text) + "»");
                    Fallos++;
                }
            }
        }

        private void Guardar(string nombre) {
            _camara.Render();
            var antes = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = antes;
            File.WriteAllBytes(Path.Combine(Carpeta, nombre + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Debug.Log("[Capturas] " + nombre);
        }

        private void Terminar() {
            Anotar("FALLOS: " + Fallos);
            File.WriteAllText(Path.Combine(Carpeta, "informe.txt"), _informe.ToString());
            Debug.Log("[Capturas] " + Carpeta + "\n" + _informe);
            try {
                var dir = Path.Combine(RutasDeGuardado.Raiz, "perfiles", IdDePerfil);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            } catch (System.Exception ex) { Debug.LogWarning("[Capturas] no se pudo borrar el perfil de prueba: " + ex.Message); }
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
