using UnityEngine;
using Mundo3D.Inventario;

namespace Mundo3D.Puertas {
    public sealed class DoorAccessRequirementTest : MonoBehaviour {
        [Header("Referencias")]
        [SerializeField]
        private DoorAccessRequirement accessRequirement;

        [SerializeField]
        private InventoryController inventory;

        [Header("Prueba")]
        [SerializeField]
        private bool registerRequiredCard;

        private void Start() {
            if (accessRequirement == null) {
                Debug.LogError(
                    "Asigna DoorAccessRequirement al script de prueba.",
                    this
                );

                return;
            }

            if (inventory == null) {
                Debug.LogError(
                    "Asigna InventoryController al script de prueba.",
                    this
                );

                return;
            }

            Debug.Log(
                $"Acceso inicial permitido: " +
                $"{accessRequirement.CanAccess(inventory)}",
                this
            );

            if (registerRequiredCard) {
                InventoryItemDefinition requiredCard =
                    accessRequirement.RequiredAccessCard;

                if (requiredCard == null) {
                    Debug.LogWarning(
                        "La puerta no tiene una tarjeta requerida asignada.",
                        this
                    );

                    return;
                }

                bool registered = inventory.RegisterItem(requiredCard);

                Debug.Log(
                    $"Tarjeta registrada: {registered}. " +
                    $"Acceso después de registrar la tarjeta: " +
                    $"{accessRequirement.CanAccess(inventory)}",
                    this
                );
            }
        }
    }
}
