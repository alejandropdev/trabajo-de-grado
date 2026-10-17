
using UnityEngine;

namespace Mundo3D.Coleccionables {
    [RequireComponent(typeof(Collider))]
    public sealed class CollectiblePickup : MonoBehaviour {
        [Header("Configuración")]
        [SerializeField]
        private string playerTag = "Player";

        private bool isCollected;

        private void Reset() {
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other) {
            // Evita recoger el mismo objeto más de una vez.
            if (isCollected)
                return;

            // Solo permite recogerlo al jugador.
            if (!other.CompareTag(playerTag))
                return;

            // Marca el objeto antes de ejecutar la recolección.
            isCollected = true;

            Collect();
        }

        private void Collect() {
            Destroy(gameObject);
        }
    }
}
