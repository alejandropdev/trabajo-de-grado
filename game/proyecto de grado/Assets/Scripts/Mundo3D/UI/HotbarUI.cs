using UnityEngine;
using UnityEngine.UI;
using Mundo3D.Inventario;

namespace Mundo3D.UI {
    public sealed class HotbarUI : MonoBehaviour {
        private const int HotbarSize = 4;

        [Header("Inventario del jugador")]
        [SerializeField]
        private InventoryController inventory;

        [Header("Iconos de los cuatro slots")]
        [SerializeField]
        private Image[] slotIcons = new Image[HotbarSize];

        private void OnEnable() {
            if (inventory == null) {
                Debug.LogError(
                    "HotbarUI: asigna el InventoryController del jugador.",
                    this
                );

                return;
            }

            inventory.HotbarChanged += RefreshHotbar;
            RefreshHotbar();
        }

        private void OnDisable() {
            if (inventory != null) {
                inventory.HotbarChanged -= RefreshHotbar;
            }
        }

        private void RefreshHotbar() {
            if (inventory == null || slotIcons == null)
                return;

            var recentItems = inventory.RecentItems;

            for (int i = 0; i < slotIcons.Length; i++) {
                Image iconImage = slotIcons[i];

                if (iconImage == null)
                    continue;

                bool hasItem = i < recentItems.Count
                    && recentItems[i] != null;

                Sprite icon = hasItem
                    ? recentItems[i].Icon
                    : null;

                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
        }
    }
}
