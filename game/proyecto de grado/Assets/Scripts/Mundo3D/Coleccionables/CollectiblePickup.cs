using UnityEngine;
using Mundo3D.Inventario;

namespace Mundo3D.Coleccionables {
    [RequireComponent(typeof(Collider))]
    public sealed class CollectiblePickup : MonoBehaviour {
        [Header("Configuración")]
        [SerializeField]
        private string playerTag = "Player";

        [Header("Definición del objeto")]
        [SerializeField]
        private InventoryItemDefinition itemDefinition;

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

            InventoryController inventory =
                other.GetComponentInParent<InventoryController>();

            if (inventory == null) {
                Debug.LogWarning(
                    $"No se encontró InventoryController en el jugador " +
                    $"al intentar recoger '{gameObject.name}'.",
                    this
                );

                return;
            }

            Collect(inventory);
        }

        private void Collect(InventoryController inventory) {
            if (isCollected)
                return;

            if (itemDefinition == null) {
                Debug.LogWarning(
                    $"El coleccionable '{gameObject.name}' no tiene " +
                    "una definición de inventario asignada.",
                    this
                );

                return;
            }

            bool registered = inventory.RegisterItem(itemDefinition);

            if (!registered)
                return;

            isCollected = true;

            if (feedback != null)
                feedback.PlayPickupFeedback();

            Destroy(gameObject);
        }
    }
}
