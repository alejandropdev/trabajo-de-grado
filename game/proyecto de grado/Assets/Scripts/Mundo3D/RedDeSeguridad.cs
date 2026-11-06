using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Nexus.Mundo3D {
    /// <summary>
    /// Lo que evita salirse del mapa o quedarse atascado, sin poner ni un solo muro invisible. Recuerda los ultimos sitios
    /// firmes por donde paso el jugador (separados al menos un metro):
    ///   · si cae por debajo de todo el nivel, o se va mas alla de todo lo que se ve, vuelve al ultimo;
    ///   · si lleva tres segundos intentando andar (W A S D pulsadas) y no avanza, vuelve a uno que este a metro y
    ///     medio o mas de donde se atasco: volver justo al sitio del atasco no serviria de nada.
    /// Mientras se juega normal no hace nada: no choca, no frena, no se nota.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class RedDeSeguridad : MonoBehaviour {
        public const float Margen = 4f, BajoElSuelo = 3f;
        /// <summary>
        /// Atascado = tres segundos con W A S D pulsadas y menos de medio metro recorrido (andando se hacen 9, corriendo
        /// 15). Se mide en ventanas de tres segundos: avanzar a rastras, unos centimetros cada vez, tambien es atascarse.
        /// </summary>
        public const float SeparacionEntreSitios = 1f, LejosDelAtasco = 1.5f, SegundosAtascado = 3f, MinimoEnLaVentana = 0.5f;
        private const int SitiosQueRecuerda = 12;

        private CharacterController _cc;
        private Bounds _visible;
        private float _suelo;
        private readonly List<Vector3> _firmes = new List<Vector3>();
        private Vector3 _dondeEmpezoAIntentar;
        private float _intentando;

        /// <summary>Mide una vez lo que se ve del nivel (sin el propio jugador): su caja y su punto mas bajo.</summary>
        public void Medir(Scene escena) {
            _cc = GetComponent<CharacterController>();
            var hay = false;
            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var r in raiz.GetComponentsInChildren<MeshRenderer>(false)) {
                    if (!r.enabled || r.transform.IsChildOf(transform)) continue;
                    if (!hay) { _visible = r.bounds; hay = true; } else _visible.Encapsulate(r.bounds);
                }
            if (!hay) _visible = new Bounds(transform.position, Vector3.one * 1000f);
            _suelo = _visible.min.y - BajoElSuelo;
            _visible.Expand(new Vector3(Margen * 2, 1000f, Margen * 2));
            _firmes.Clear();
            _firmes.Add(transform.position);
        }

        private void LateUpdate() {
            if (_cc == null || !_cc.enabled || Time.timeScale <= 0f) return;
            var p = transform.position;
            if (p.y < _suelo || !_visible.Contains(p)) { Devolver(0f); return; }

            if (_cc.isGrounded && Vector3.Distance(p, _firmes[_firmes.Count - 1]) >= SeparacionEntreSitios) {
                _firmes.Add(p);
                if (_firmes.Count > SitiosQueRecuerda) _firmes.RemoveAt(0);
            }

            // ¿Atascado? Quiere andar, y en una ventana de tres segundos no recorre ni medio metro (en horizontal).
            if (!QuiereAndar()) { _intentando = 0f; return; }
            if (_intentando <= 0f) {
                _dondeEmpezoAIntentar = p;
                _intentando = Time.deltaTime;
                return;
            }
            _intentando += Time.deltaTime;
            if (_intentando < SegundosAtascado) return;
            var recorrido = p - _dondeEmpezoAIntentar;
            recorrido.y = 0;
            _intentando = 0f;   // la siguiente ventana empieza de cero
            if (recorrido.magnitude < MinimoEnLaVentana) Devolver(LejosDelAtasco);
        }

        private static bool QuiereAndar() {
            var k = Keyboard.current;
            return k != null && (k.wKey.isPressed || k.aKey.isPressed || k.sKey.isPressed || k.dKey.isPressed ||
                                 k.upArrowKey.isPressed || k.downArrowKey.isPressed || k.leftArrowKey.isPressed || k.rightArrowKey.isPressed);
        }

        /// <summary>Al sitio firme mas reciente que este al menos a 'lejos' metros de aqui (o al primero que se guardo).</summary>
        private void Devolver(float lejos) {
            var aqui = transform.position;
            var destino = _firmes[0];
            for (var i = _firmes.Count - 1; i >= 0; i--)
                if (Vector3.Distance(_firmes[i], aqui) >= lejos) { destino = _firmes[i]; break; }
            // Un CharacterController encendido ignora que le cambien la posicion: se apaga un instante.
            _cc.enabled = false;
            transform.position = destino + Vector3.up * 0.1f;
            _cc.enabled = true;
            Debug.Log(lejos > 0 ? "[Nexus] El jugador se atascó: vuelve a un sitio libre." : "[Nexus] El jugador salió del mapa: vuelve al último sitio firme.");
        }
    }
}
