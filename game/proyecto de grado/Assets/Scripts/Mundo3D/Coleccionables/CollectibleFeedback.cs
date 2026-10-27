using Mundo3D.Inventario;
using UnityEngine;

namespace Mundo3D.Coleccionables {
    public sealed class CollectibleFeedback : MonoBehaviour {
        [Header("Efecto de partículas")]
        [SerializeField]
        private ParticleSystem pickupParticlesPrefab;

        [SerializeField, Min(0.1f)]
        private float particleEffectLifetime = 3f;

        [Header("Efecto de sonido")]
        [SerializeField]
        private AudioClip pickupSound;

        [SerializeField, Range(0f, 1f)]
        private float soundVolume = 1f;

        [Header("Brillo del coleccionable")]
        [SerializeField]
        private Light glowLight;

        // El brillo dice qué es antes de verlo de cerca: cada categoría tiene su color y su ritmo.
        private enum Destello { Fijo, Respira, Baliza, Latido, Centelleo, Pulso, Halo }

        private Destello destello = Destello.Fijo;
        private float baseIntensity = -1f;
        private float baseRange;
        private float phase;

        /// <summary>Colorea y anima la luz según la categoría del objeto. Se puede llamar más de una vez.</summary>
        public void ApplyCategory(InventoryItemCategory category) {
            if (glowLight == null)
                return;

            if (baseIntensity < 0f) {
                baseIntensity = glowLight.intensity;
                baseRange = glowLight.range;
                phase = Random.value * 10f;   // que no parpadeen todos a la vez
            }

            switch (category) {
                case InventoryItemCategory.CharacterCV: Set("#22D3EE", Destello.Respira); break;
                case InventoryItemCategory.AccessCard: Set("#FBBF24", Destello.Baliza); break;
                case InventoryItemCategory.Consumable: Set("#34D399", Destello.Latido); break;
                case InventoryItemCategory.DeveloperCard:
                case InventoryItemCategory.OfficeNote: Set("#C084FC", Destello.Centelleo); break;
                case InventoryItemCategory.Evidence: Set("#F87171", Destello.Pulso); break;
                case InventoryItemCategory.Certificate: Set("#E3C24F", Destello.Halo); break;
                default: destello = Destello.Fijo; break;
            }
        }

        private void Set(string hex, Destello ritmo) {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                glowLight.color = color;
            destello = ritmo;
        }

        private void Update() {
            if (glowLight == null || baseIntensity < 0f || destello == Destello.Fijo)
                return;

            // Tiempo escalado: con el juego en pausa el brillo también se queda quieto.
            float t = Time.time + phase;
            float factor;
            switch (destello) {
                case Destello.Respira:     // subir y bajar despacio, como respirando (2,5 s)
                    factor = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 2.5f));
                    break;
                case Destello.Baliza: {    // dos destellos cortos y una pausa, como una baliza (1,6 s)
                    float c = Mathf.Repeat(t, 1.6f);
                    factor = (c < 0.12f || (c > 0.3f && c < 0.42f)) ? 1.3f : 0.25f;
                    break;
                }
                case Destello.Latido: {    // latido suave: un golpe y descanso (1,2 s)
                    float c = Mathf.Repeat(t, 1.2f) / 1.2f;
                    factor = 0.6f + 0.5f * Mathf.Exp(-c * 6f);
                    break;
                }
                case Destello.Centelleo:   // irregular, como una chispa
                    factor = 0.45f + 0.8f * Mathf.PerlinNoise(t * 3.5f, phase);
                    break;
                case Destello.Pulso:       // rápido e insistente (0,6 s)
                    factor = 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 0.6f));
                    break;
                case Destello.Halo:        // brillo casi fijo con un halo que se abre lento (4 s)
                    float h = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 4f);
                    factor = 0.85f + 0.15f * h;
                    glowLight.range = baseRange * (0.9f + 0.4f * h);
                    break;
                default:
                    factor = 1f;
                    break;
            }
            glowLight.intensity = baseIntensity * factor;
        }

        public void PlayPickupFeedback() {
            PlayParticles();
            PlaySound();
        }

        private void PlayParticles() {
            if (pickupParticlesPrefab == null)
                return;

            ParticleSystem particles = Instantiate(
                pickupParticlesPrefab,
                transform.position,
                Quaternion.identity
            );

            particles.Play();

            // El efecto es independiente del coleccionable.
            // Por eso puede terminar aunque el objeto sea destruido.
            Destroy(particles.gameObject, particleEffectLifetime);
        }

        private void PlaySound() {
            if (pickupSound == null)
                return;

            // El sonido continúa aunque se destruya el coleccionable.
            AudioSource.PlayClipAtPoint(
                pickupSound,
                transform.position,
                soundVolume
            );
        }
    }
}
