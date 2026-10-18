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
