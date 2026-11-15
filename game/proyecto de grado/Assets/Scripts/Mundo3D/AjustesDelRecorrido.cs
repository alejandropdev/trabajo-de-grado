using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Nexus.Mundo3D {
    /// <summary>
    /// Lo que se le ajusta al nivel del equipo 3D al cargarlo, sin tocar la escena (se regenera con herramientas):
    ///   · el salto, que con 4,35 subia casi un metro y dejaba pasar por encima de tabiques y mamparas;
    ///   · los limites: sin muros invisibles; una red de seguridad devuelve al jugador si se cae o se sale;
    ///   · la camara: no atraviesa paredes (CinemachineCollider), arranca y frena suave, y respeta «invertir eje»;
    ///   · la ambientacion: luz calida de oficina y un poco de postproceso (tonos, brillo, viñeta).
    /// Todo se hace una sola vez por carga.
    /// </summary>
    public static class AjustesDelRecorrido {
        // ================================================================== jugador

        /// <summary>
        /// Sin salto. El recorrido no lo necesita (los escalones se suben solos: stepOffset 0,3 m), y saltando el
        /// jugador caia encajado entre taburetes, reposapies, voladizos de barras y estanterias, donde andando no se
        /// llega y de donde andando no se sale (2026-11-05: 622 sitios así, medidos con AtascosDelRecorrido3D).
        /// NThirdPersonPlayer es de otro ensamblado: se toca por reflexion, como el resto.
        /// </summary>
        public static void AjustarSalto(GameObject jugador) {
            if (jugador == null) return;
            foreach (var c in jugador.GetComponentsInChildren<MonoBehaviour>(true)) {
                if (c == null || c.GetType().Name != "NThirdPersonPlayer") continue;
                var saltos = c.GetType().GetField("jumpsMax");
                if (saltos != null && saltos.FieldType == typeof(int)) saltos.SetValue(c, 0);
            }
        }

        // ================================================================== limites

        /// <summary>
        /// Salir del mapa ya no se impide con muros invisibles (cortaban paso donde no se veia nada): el jugador lleva
        /// una RedDeSeguridad que lo devuelve al ultimo sitio donde piso suelo si se cae o se sale de lo que se ve.
        /// Las paredes que dejaban pasar se arreglan en la escena (ParedesInvisibles3D, en el editor), no aqui.
        /// </summary>
        public static void PonerRedDeSeguridad(Scene escena) {
            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var cc in raiz.GetComponentsInChildren<CharacterController>(true))
                    if (cc.GetComponent<RedDeSeguridad>() == null) cc.gameObject.AddComponent<RedDeSeguridad>().Medir(escena);
        }

        // ================================================================== camara

        /// <summary>
        /// La camara libre: que no se meta dentro de las paredes (se acerca al jugador cuando algo se interpone) y con
        /// aceleracion y frenada suaves, para que mirar no maree.
        /// </summary>
        public const float AltoDelAroDeArriba = 2.6f, RadioDelAroDeArriba = 2.4f;

        public static void AjustarCamara(CinemachineFreeLook camara, string etiquetaDelJugador) {
            if (camara == null) return;
            camara.m_XAxis.m_AccelTime = 0.12f;
            camara.m_XAxis.m_DecelTime = 0.18f;
            camara.m_YAxis.m_AccelTime = 0.15f;
            camara.m_YAxis.m_DecelTime = 0.2f;
            camara.m_YAxisRecentering.m_enabled = false;   // que no tire de la vista mientras se mira

            // El aro de arriba venia a 3,55 m de alto y 1,3 m de distancia: 70° hacia abajo, y al acercarse por una pared
            // la camara quedaba encima del personaje mirando el suelo. Ahora como mucho ~47°: se ve hacia donde se anda.
            var arriba = camara.m_Orbits[0];
            arriba.m_Height = Mathf.Min(arriba.m_Height, AltoDelAroDeArriba);
            arriba.m_Radius = Mathf.Max(arriba.m_Radius, RadioDelAroDeArriba);
            camara.m_Orbits[0] = arriba;

            var colision = camara.GetComponent<CinemachineCollider>();
            if (colision == null) colision = camara.gameObject.AddComponent<CinemachineCollider>();
            colision.m_CollideAgainst = ~LayerMask.GetMask("Ignore Raycast", "UI", "TransparentFX");
            if (!string.IsNullOrEmpty(etiquetaDelJugador) && etiquetaDelJugador != "Untagged") colision.m_IgnoreTag = etiquetaDelJugador;
            colision.m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
            colision.m_MinimumDistanceFromTarget = 0.3f;
            colision.m_CameraRadius = 0.2f;
            colision.m_Damping = 0.25f;
            colision.m_DampingWhenOccluded = 0.1f;
            colision.m_SmoothingTime = 0.15f;
            colision.m_AvoidObstacles = true;
        }

        /// <summary>'invertidoDeFabrica' es como venia la camara: invertir es respecto de eso.</summary>
        public static void InvertirEjeY(CinemachineFreeLook camara, bool invertidoDeFabrica, bool invertir) {
            if (camara != null) camara.m_YAxis.m_InvertInput = invertidoDeFabrica ^ invertir;
        }

        // ================================================================== ambientacion

        /// <summary>
        /// El color de la luz principal de la escena de recoleccion (#160059: R 22, G 0, B 89). Se escribe en sRGB, tal como
        /// sale del selector de color; Unity lo convierte solo en el espacio lineal.
        /// </summary>
        public static readonly Color ColorDeLaLuz = new Color32(0x5F, 0xB8, 0xC8, 0xFF);   // luz principal fria y tenue (antes, morada #160059)

        /// <summary>
        /// Luz de oficina por la tarde: ambiente en tres tonos (techo claro, paredes calidas, suelo oscuro), una niebla
        /// apenas perceptible que da profundidad a los pasillos, la luz principal con el color de la escena (#160059; antes
        /// se le ponia uno calido y se sobreponia) y sombras suaves, y un volumen de postproceso discreto. Solo toca la
        /// escena que se le pasa. Nada de esto cambia lo que se puede ver o recoger.
        /// </summary>
        public static void Ambientar(Scene escena) {
            // 2026-11-15, direccion de arte (docs/08-arte): el recorrido es el INTERIOR del bunker, en bioluminiscencia.
            // Ambiente y niebla verdeazulados (antes: niebla parda y filtro calido sobre luz morada, dos temperaturas que
            // se anulaban), un techo que cierra el bunker (antes se veian las estrellas dentro de la oficina) y los paneles
            // de luz en el cian del neon.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color32(0x24, 0x4C, 0x54, 0xFF);
            RenderSettings.ambientEquatorColor = new Color32(0x16, 0x34, 0x3A, 0xFF);
            RenderSettings.ambientGroundColor = new Color32(0x08, 0x14, 0x18, 0xFF);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color32(0x16, 0x46, 0x50, 0xFF);
            RenderSettings.fogStartDistance = 10f;
            RenderSettings.fogEndDistance = 55f;
            PonerTecho(escena);
            TemplarMateriales(escena);
            var n = 0;
            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var luz in raiz.GetComponentsInChildren<Light>(true)) {
                    if (luz.type != LightType.Point && luz.type != LightType.Spot) continue;
                    // Los paneles del equipo eran azul electrico (0.11, 0.40, 0.83): pasan al cian del neon; uno de cada tres,
                    // al ambar de las bombillas, para que el interior tenga su contrapunto calido.
                    if (luz.color.b > luz.color.r * 2f) {
                        luz.color = n++ % 3 == 0 ? new Color32(0xF2, 0xB0, 0x5A, 0xFF) : new Color32(0x6F, 0xCF, 0xE6, 0xFF);
                        luz.intensity *= 0.65f;
                    }
                }

            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var luz in raiz.GetComponentsInChildren<Light>(true)) {
                    if (luz.type != LightType.Directional) continue;
                    luz.color = ColorDeLaLuz;
                    luz.intensity = 0.45f;
                    luz.shadows = LightShadows.Soft;
                    luz.shadowStrength = 0.75f;
                }

            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var cam in raiz.GetComponentsInChildren<Camera>(true)) {
                    var datos = cam.GetUniversalAdditionalCameraData();
                    if (datos != null) datos.renderPostProcessing = true;
                }

            var go = new GameObject("Ambiente del recorrido (Nexus)");
            SceneManager.MoveGameObjectToScene(go, escena);
            var volumen = go.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.priority = 10;
            var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            perfil.name = "Ambiente del recorrido";
            var tonos = perfil.Add<Tonemapping>(true);
            tonos.mode.Override(TonemappingMode.ACES);
            var brillo = perfil.Add<Bloom>(true);
            brillo.threshold.Override(0.85f);
            brillo.intensity.Override(0.6f);
            brillo.scatter.Override(0.6f);
            var color = perfil.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.1f);
            color.contrast.Override(8f);
            color.saturation.Override(6f);
            color.colorFilter.Override(new Color(0.94f, 1f, 1f));
            var vineta = perfil.Add<Vignette>(true);
            vineta.intensity.Override(0.3f);
            vineta.smoothness.Override(0.45f);
            volumen.sharedProfile = perfil;
        }

        /// <summary>
        /// Materiales de estilo (2026-11-15): el marmol casi blanco del nivel quemaba la imagen en la penumbra del bunker.
        /// Se pinta con una COPIA de cada material (el original no se toca) entintada hacia el hormigon verdeazulado y con
        /// menos brillo especular. Una copia por material, compartida por todos los que lo usan.
        /// </summary>
        private static void TemplarMateriales(Scene escena) {
            var copias = new Dictionary<Material, Material>();
            var tinte = new Color(0.42f, 0.52f, 0.54f, 1f);
            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var r in raiz.GetComponentsInChildren<Renderer>(true)) {
                    var mats = r.sharedMaterials;
                    var cambiado = false;
                    for (var i = 0; i < mats.Length; i++) {
                        var m = mats[i];
                        if (m == null || !m.name.StartsWith("Marmol", StringComparison.OrdinalIgnoreCase)) continue;
                        Material copia;
                        if (!copias.TryGetValue(m, out copia)) {
                            copia = new Material(m) { name = m.name + " (bunker)" };
                            if (copia.HasProperty("_BaseColor")) copia.SetColor("_BaseColor", copia.GetColor("_BaseColor") * tinte);
                            if (copia.HasProperty("_Smoothness")) copia.SetFloat("_Smoothness", Mathf.Min(copia.GetFloat("_Smoothness"), 0.25f));
                            copias[m] = copia;
                        }
                        mats[i] = copia;
                        cambiado = true;
                    }
                    if (cambiado) r.sharedMaterials = mats;
                }
            Debug.Log("[Recorrido] materiales templados: " + copias.Count);
        }

        /// <summary>
        /// El techo del bunker (2026-11-15): el nivel del equipo no tenia, y dentro de la oficina se veia el cielo con
        /// estrellas. La altura sale de los muros (piezas altas y delgadas): la mediana de su borde superior. Una losa
        /// oscura, iluminada por los paneles, que no colisiona ni cambia nada de lo que se puede ver o recoger a ras de suelo.
        /// </summary>
        private static void PonerTecho(Scene escena) {
            var alturas = new List<float>();
            var todo = new Bounds();
            var hay = false;
            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var r in raiz.GetComponentsInChildren<MeshRenderer>(false)) {
                    var b = r.bounds;
                    if (!hay) { todo = b; hay = true; } else todo.Encapsulate(b);
                    var largo = Mathf.Max(b.size.x, b.size.z);
                    var grueso = Mathf.Min(b.size.x, b.size.z);
                    if (b.size.y > 2.2f && b.size.y < 9f && largo > 3f && grueso < 1.2f) alturas.Add(b.max.y);
                }
            if (!hay || alturas.Count < 5) { Debug.LogWarning("[Recorrido] sin muros para medir la altura del techo: no se pone"); return; }
            alturas.Sort();
            var altura = alturas[alturas.Count / 2];

            var techo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            techo.name = "Techo del bunker (Nexus)";
            UnityEngine.Object.Destroy(techo.GetComponent<Collider>());
            SceneManager.MoveGameObjectToScene(techo, escena);
            techo.transform.position = new Vector3(todo.center.x, altura, todo.center.z);
            techo.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // la cara visible mira hacia abajo
            techo.transform.localScale = new Vector3(todo.size.x + 20f, todo.size.z + 20f, 1f);
            var sombreador = Shader.Find("Universal Render Pipeline/Lit");
            if (sombreador != null) {
                var m = new Material(sombreador) { name = "Techo del bunker" };
                m.SetColor("_BaseColor", new Color32(0x0A, 0x2A, 0x33, 0xFF));
                m.SetFloat("_Smoothness", 0.05f);
                techo.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            // Por las dos caras: la losa tapa la luz principal y el interior queda en la penumbra de sus propias lamparas.
            techo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;
            Debug.Log("[Recorrido] techo a " + altura.ToString("0.00") + " m (" + alturas.Count + " muros medidos)");
        }
    }
}
