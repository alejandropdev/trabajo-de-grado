using System.Collections.Generic;
using UnityEngine;
using Mundo3D.Inventario;

namespace Mundo3D.Puertas {
    public sealed class DoorAccessRequirement : MonoBehaviour {
        [Header("Requisito de acceso")]
        [SerializeField]
        private bool requiresAuthorization = false;

        [Header("Tarjeta principal requerida")]
        [SerializeField]
        private InventoryItemDefinition requiredAccessCard;

        [Header("Tarjetas adicionales requeridas")]
        [SerializeField]
        private List<InventoryItemDefinition> additionalRequiredAccessCards =
            new List<InventoryItemDefinition>();

        public bool RequiresAuthorization => requiresAuthorization;

        public InventoryItemDefinition RequiredAccessCard =>
            requiredAccessCard;

        /// <summary>
        /// Comprueba si el inventario contiene todas las tarjetas
        /// necesarias para acceder a la puerta.
        /// </summary>
        public bool CanAccess(InventoryController inventory) {
            if (!requiresAuthorization)
                return true;

            if (inventory == null)
                return false;

            // Una puerta restringida debe tener una tarjeta principal.
            if (requiredAccessCard == null)
                return false;

            if (string.IsNullOrWhiteSpace(requiredAccessCard.Id))
                return false;

            if (!inventory.HasItem(requiredAccessCard.Id))
                return false;

            // Todas las tarjetas adicionales son obligatorias.
            foreach (InventoryItemDefinition card
                in additionalRequiredAccessCards) {
                if (card == null)
                    return false;

                if (string.IsNullOrWhiteSpace(card.Id))
                    return false;

                if (!inventory.HasItem(card.Id))
                    return false;
            }

            return true;
        }
    }
}
