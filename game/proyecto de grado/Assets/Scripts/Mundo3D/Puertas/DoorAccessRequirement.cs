using UnityEngine;
using Mundo3D.Inventario;

namespace Mundo3D.Puertas {
    public sealed class DoorAccessRequirement : MonoBehaviour {
        [Header("Requisito de acceso")]
        [SerializeField]
        private bool requiresAuthorization = false;

        [Header("Tarjeta requerida")]
        [SerializeField]
        private InventoryItemDefinition requiredAccessCard;

        public bool RequiresAuthorization => requiresAuthorization;

        public InventoryItemDefinition RequiredAccessCard =>
            requiredAccessCard;

        /// Indica si el jugador puede acceder según la configuración de la puerta y su inventario.
        public bool CanAccess(InventoryController inventory) {
            if (!requiresAuthorization)
                return true;

            if (requiredAccessCard == null)
                return false;

            if (inventory == null)
                return false;

            return inventory.HasItem(requiredAccessCard.Id);
        }
    }
}
