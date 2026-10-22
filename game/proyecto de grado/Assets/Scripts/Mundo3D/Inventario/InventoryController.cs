using System.Collections.Generic;
using UnityEngine;

namespace Mundo3D.Inventario {
    public sealed class InventoryController : MonoBehaviour {
        private readonly Dictionary<string, InventoryItemDefinition> items =
            new Dictionary<string, InventoryItemDefinition>();

        private readonly Dictionary<string, int> quantities =
            new Dictionary<string, int>();

        public bool RegisterItem(InventoryItemDefinition definition) {
            if (definition == null) {
                Debug.LogWarning(
                    "No se puede registrar un objeto sin una definición."
                );

                return false;
            }

            string itemId = definition.Id;

            if (string.IsNullOrWhiteSpace(itemId)) {
                Debug.LogWarning(
                    $"El objeto '{definition.name}' no tiene un ID válido."
                );

                return false;
            }

            if (items.ContainsKey(itemId)) {
                quantities[itemId]++;
            }
            else {
                items.Add(itemId, definition);
                quantities.Add(itemId, 1);
            }

            return true;
        }

        public bool HasItem(string itemId) {
            return !string.IsNullOrWhiteSpace(itemId)
                && quantities.TryGetValue(itemId, out int quantity)
                && quantity > 0;
        }

        public int GetQuantity(string itemId) {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0;

            return quantities.TryGetValue(itemId, out int quantity)
                ? quantity
                : 0;
        }

        public bool TryGetItem(
            string itemId,
            out InventoryItemDefinition definition
        ) {
            if (!string.IsNullOrWhiteSpace(itemId)
                && items.TryGetValue(itemId, out definition)) {
                return true;
            }

            definition = null;
            return false;
        }
    }
}
