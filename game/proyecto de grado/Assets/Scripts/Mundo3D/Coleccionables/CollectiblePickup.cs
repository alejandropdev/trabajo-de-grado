using UnityEngine;

namespace Mundo3D.Coleccionables {
    [RequireComponent(typeof(Collider))]
    public sealed class CollectiblePickup : MonoBehaviour {
        [Header("Configuración")]
        [SerializeField]
        private string playerTag = "Player";

        [Header("Efectos de recogida")]
        [SerializeField]
        private CollectibleFeedback feedback;

        private bool isCollected;

        private void Reset() {
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;
        }

        private void Awake() {
            if (feedback == null)
                feedback = GetComponent<CollectibleFeedback>();
        }

        private void OnTriggerEnter(Collider other) {
            if (isCollected)
                return;

            if (!other.CompareTag(playerTag))
                return;

            Collect();
        }

        private void Collect() {
            if (isCollected)
                return;

            isCollected = true;

            if (feedback != null)
                feedback.PlayPickupFeedback();

            Destroy(gameObject);
        }
    }
}
