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
        /// Luz de oficina por la tarde: ambiente en tres tonos (techo claro, paredes calidas, suelo oscuro), una niebla
        /// apenas perceptible que da profundidad a los pasillos, la luz principal calida y con sombras suaves, y un
        /// volumen de postproceso discreto. Nada de esto cambia lo que se puede ver o recoger.
        /// </summary>
        public static void Ambientar(Scene escena) {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.80f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.55f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.19f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.53f, 0.50f);
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 70f;

            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var luz in raiz.GetComponentsInChildren<Light>(true)) {
                    if (luz.type != LightType.Directional) continue;
                    luz.color = new Color(1f, 0.93f, 0.82f);
                    luz.intensity = Mathf.Max(luz.intensity, 1.1f);
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
            brillo.threshold.Override(1.05f);
            brillo.intensity.Override(0.35f);
            brillo.scatter.Override(0.6f);
            var color = perfil.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.25f);
            color.contrast.Override(8f);
            color.saturation.Override(6f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.93f));
            var vineta = perfil.Add<Vignette>(true);
            vineta.intensity.Override(0.22f);
            vineta.smoothness.Override(0.45f);
            volumen.sharedProfile = perfil;
        }
    }
}
