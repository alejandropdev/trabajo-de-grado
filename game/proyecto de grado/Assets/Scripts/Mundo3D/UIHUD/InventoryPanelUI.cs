using UnityEngine;

namespace Mundo3D.UI {
    public class InventoryPanelUI : MonoBehaviour {
        [Header("Panel de inventario")]
        [SerializeField] private GameObject inventoryPanel;

        [Header("Botón de inventario del HUD")]
        [SerializeField] private GameObject inventoryButton;

        public void OpenInventory() {
            if (inventoryPanel == null) {
                Debug.LogWarning(
                    "InventoryPanelUI: no se ha asignado el panel de inventario.",
                    this
                );
                return;
            }

            inventoryPanel.SetActive(true);

            if (inventoryButton != null) {
                inventoryButton.SetActive(false);
            }
        }

        public void CloseInventory() {
            if (inventoryPanel == null) {
                Debug.LogWarning(
                    "InventoryPanelUI: no se ha asignado el panel de inventario.",
                    this
                );
                return;
            }

            inventoryPanel.SetActive(false);

            if (inventoryButton != null) {
                inventoryButton.SetActive(true);
            }
        }
    }
}
