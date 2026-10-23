using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mundo3D.Inventario;

namespace Mundo3D.UI {
    public sealed class InventoryPanelContentUI : MonoBehaviour {
        [Header("Inventario del jugador")]
        [SerializeField] private InventoryController inventory;

        [Header("Slots de cada categoría")]
        [SerializeField] private InventorySlotUI[] collectibleSlots;
        [SerializeField] private InventorySlotUI[] accessCardSlots;
        [SerializeField] private InventorySlotUI[] cvSlots;
        [SerializeField] private InventorySlotUI[] consumableSlots;

        [Header("Panel de detalles")]
        [SerializeField] private Image selectedItemImage;
        [SerializeField] private TMP_Text selectedItemName;
        [SerializeField] private TMP_Text selectedItemDescription;

        private void Awake() {
            ConfigureSlots(collectibleSlots);
            ConfigureSlots(accessCardSlots);
            ConfigureSlots(cvSlots);
            ConfigureSlots(consumableSlots);

            ClearDetails();
        }

        private void OnEnable() {
            if (inventory == null) {
                Debug.LogError(
                    "InventoryPanelContentUI: asigna el InventoryController.",
                    this
                );
                return;
            }

            inventory.InventoryChanged += RefreshInventory;
            RefreshInventory();
        }

        private void OnDisable() {
            if (inventory != null)
                inventory.InventoryChanged -= RefreshInventory;
        }

        private void ConfigureSlots(InventorySlotUI[] slots) {
            if (slots == null)
                return;

            foreach (InventorySlotUI slot in slots) {
                if (slot == null)
                    continue;

                slot.SetSelectionCallback(ShowItemDetails);
                slot.Clear();
            }
        }

        private void RefreshInventory() {
            ClearSlots(collectibleSlots);
            ClearSlots(accessCardSlots);
            ClearSlots(cvSlots);
            ClearSlots(consumableSlots);

            if (inventory == null)
                return;

            int collectibleIndex = 0;
            int accessCardIndex = 0;
            int cvIndex = 0;
            int consumableIndex = 0;

            foreach (InventoryItemDefinition item in inventory.CollectedItems) {
                if (item == null)
                    continue;

                switch (item.Category) {
                    case InventoryItemCategory.DeveloperCard:
                    case InventoryItemCategory.OfficeNote:
                    case InventoryItemCategory.Evidence:
                        AddToSlots(
                            collectibleSlots,
                            ref collectibleIndex,
                            item
                        );
                        break;

                    case InventoryItemCategory.AccessCard:
                        AddToSlots(
                            accessCardSlots,
                            ref accessCardIndex,
                            item
                        );
                        break;

                    case InventoryItemCategory.CharacterCV:
                        AddToSlots(
                            cvSlots,
                            ref cvIndex,
                            item
                        );
                        break;

                    case InventoryItemCategory.Consumable:
                    case InventoryItemCategory.Certificate:
                        AddToSlots(
                            consumableSlots,
                            ref consumableIndex,
                            item
                        );
                        break;

                    case InventoryItemCategory.Other:
                        Debug.LogWarning(
                            $"El objeto '{item.DisplayName}' tiene la " +
                            "categoría Other y no está asignado a una sección.",
                            this
                        );
                        break;
                }
            }
        }

        private void AddToSlots(
            InventorySlotUI[] slots,
            ref int index,
            InventoryItemDefinition item
        ) {
            if (slots == null || index >= slots.Length) {
                Debug.LogWarning(
                    $"No hay slots disponibles para '{item.DisplayName}'.",
                    this
                );
                return;
            }

            InventorySlotUI slot = slots[index];
            index++;

            if (slot != null)
                slot.SetItem(item);
        }

        private void ClearSlots(InventorySlotUI[] slots) {
            if (slots == null)
                return;

            foreach (InventorySlotUI slot in slots) {
                if (slot != null)
                    slot.Clear();
            }
        }

        private void ShowItemDetails(InventoryItemDefinition item) {
            if (item == null)
                return;

            if (selectedItemImage != null) {
                selectedItemImage.sprite = item.Icon;
                selectedItemImage.enabled = item.Icon != null;
                selectedItemImage.preserveAspect = true;
            }

            if (selectedItemName != null)
                selectedItemName.text = item.DisplayName;

            if (selectedItemDescription != null)
                selectedItemDescription.text = item.Description;
        }

        private void ClearDetails() {
            if (selectedItemImage != null) {
                selectedItemImage.sprite = null;
                selectedItemImage.enabled = false;
            }

            if (selectedItemName != null)
                selectedItemName.text = string.Empty;

            if (selectedItemDescription != null)
                selectedItemDescription.text = string.Empty;
        }
    }
}
