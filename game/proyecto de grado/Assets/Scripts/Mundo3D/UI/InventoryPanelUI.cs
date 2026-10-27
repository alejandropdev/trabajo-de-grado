using System;
using UnityEngine;

namespace Mundo3D.UI {
    public class InventoryPanelUI : MonoBehaviour {
        [Header("Panel de inventario")]
        [SerializeField] private GameObject inventoryPanel;

        [Header("Botón de inventario del HUD")]
        [SerializeField] private GameObject inventoryButton;

        /// <summary>El inventario se abrió: quien maneja el recorrido para el reloj y el mundo.</summary>
        public event Action Opened;

        /// <summary>El inventario se cerró.</summary>
        public event Action Closed;

        public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        public void OpenInventory() {
            if (inventoryPanel == null) {
                Debug.LogWarning(
                    "InventoryPanelUI: no se ha asignado el panel de inventario.",
                    this
                );
                return;
            }

            if (inventoryPanel.activeSelf)
                return;

            inventoryPanel.SetActive(true);

            if (inventoryButton != null) {
                inventoryButton.SetActive(false);
            }

            Opened?.Invoke();
        }

        public void CloseInventory() {
            if (inventoryPanel == null) {
                Debug.LogWarning(
                    "InventoryPanelUI: no se ha asignado el panel de inventario.",
                    this
                );
                return;
            }

            if (!inventoryPanel.activeSelf)
                return;

            inventoryPanel.SetActive(false);

            if (inventoryButton != null) {
                inventoryButton.SetActive(true);
            }

            Closed?.Invoke();
        }

        public void ToggleInventory() {
            if (IsOpen) CloseInventory();
            else OpenInventory();
        }
    }
}
