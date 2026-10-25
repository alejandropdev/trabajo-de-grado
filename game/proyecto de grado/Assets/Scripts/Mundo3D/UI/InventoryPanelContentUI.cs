using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mundo3D.Inventario;

namespace Mundo3D.UI {
    public sealed class InventoryPanelContentUI : MonoBehaviour {
        [Header("Inventario del jugador")]
        [SerializeField] private InventoryController inventory;

        [Header("Definiciones generales de las colecciones")]
        [SerializeField]
        private InventoryItemDefinition developerCardsCollectionDefinition;

        [SerializeField]
        private InventoryItemDefinition officeNotesCollectionDefinition;

        [SerializeField]
        private InventoryItemDefinition usbEvidenceCollectionDefinition;

        [Header("Slots de cada categoría")]
        [SerializeField] private InventorySlotUI[] collectibleSlots;
        [SerializeField] private InventorySlotUI[] accessCardSlots;
        [SerializeField] private InventorySlotUI[] cvSlots;
        [SerializeField] private InventorySlotUI[] consumableSlots;

        [Header("Contadores de coleccionables")]
        [SerializeField] private TMP_Text developerCardsCountText;
        [SerializeField] private TMP_Text officeNotesCountText;
        [SerializeField] private TMP_Text usbFragmentCountText;

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
            UpdateCollectibleCounters(0, 0, 0);
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

            if (inventory == null) {
                UpdateCollectibleCounters(0, 0, 0);
                return;
            }

            bool hasDeveloperCards = false;
            bool hasOfficeNotes = false;
            bool hasUsbEvidence = false;

            int developerCardsCount = 0;
            int officeNotesCount = 0;
            int usbFragmentCount = 0;

            // Cada colección cuenta IDs individuales diferentes.
            HashSet<string> countedDeveloperCards =
                new HashSet<string>();

            HashSet<string> countedOfficeNotes =
                new HashSet<string>();

            HashSet<string> countedUsbFragments =
                new HashSet<string>();

            int accessCardIndex = 0;
            int cvIndex = 0;
            int consumableIndex = 0;

            foreach (InventoryItemDefinition item in inventory.CollectedItems) {
                if (item == null)
                    continue;

                switch (item.Category) {
                    case InventoryItemCategory.DeveloperCard: {
                            hasDeveloperCards = true;

                            if (countedDeveloperCards.Add(GetItemKey(item)))
                                developerCardsCount++;

                            break;
                        }

                    case InventoryItemCategory.OfficeNote: {
                            hasOfficeNotes = true;

                            if (countedOfficeNotes.Add(GetItemKey(item)))
                                officeNotesCount++;

                            break;
                        }

                    case InventoryItemCategory.Evidence: {
                            hasUsbEvidence = true;

                            if (countedUsbFragments.Add(GetItemKey(item)))
                                usbFragmentCount++;

                            break;
                        }

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

            // Los slots de estas colecciones muestran siempre
            // su definición general, no la primera pieza recogida.
            SetGeneralCollectionSlot(
                0,
                developerCardsCollectionDefinition,
                hasDeveloperCards
            );

            SetGeneralCollectionSlot(
                1,
                officeNotesCollectionDefinition,
                hasOfficeNotes
            );

            SetGeneralCollectionSlot(
                2,
                usbEvidenceCollectionDefinition,
                hasUsbEvidence
            );

            UpdateCollectibleCounters(
                developerCardsCount,
                officeNotesCount,
                usbFragmentCount
            );
        }

        private string GetItemKey(InventoryItemDefinition item) {
            if (!string.IsNullOrWhiteSpace(item.Id))
                return item.Id;

            return item.name;
        }

        private void SetGeneralCollectionSlot(
            int slotIndex,
            InventoryItemDefinition generalDefinition,
            bool hasCollectedItems) {
            if (collectibleSlots == null ||
                slotIndex >= collectibleSlots.Length) {
                Debug.LogError(
                    $"Falta configurar el slot {slotIndex + 1} " +
                    "de la sección de coleccionables.",
                    this
                );
                return;
            }

            InventorySlotUI slot = collectibleSlots[slotIndex];

            if (slot == null) {
                Debug.LogError(
                    $"El slot {slotIndex + 1} de coleccionables " +
                    "no está asignado.",
                    this
                );
                return;
            }

            if (!hasCollectedItems) {
                slot.Clear();
                return;
            }

            if (generalDefinition == null) {
                Debug.LogError(
                    $"Asigna la definición general del slot " +
                    $"{slotIndex + 1} en InventoryPanelContentUI.",
                    this
                );
                slot.Clear();
                return;
            }

            slot.SetItem(generalDefinition);
        }

        private void UpdateCollectibleCounters(
            int developerCards,
            int officeNotes,
            int usbFragments) {
            if (developerCardsCountText != null) {
                developerCardsCountText.text =
                    $"{Mathf.Min(developerCards, 7)}/7";
            }

            if (officeNotesCountText != null) {
                officeNotesCountText.text =
                    $"{Mathf.Min(officeNotes, 6)}/6";
            }

            if (usbFragmentCountText != null) {
                usbFragmentCountText.text =
                    $"{Mathf.Min(usbFragments, 1)}/1";
            }
        }

        private void AddToSlots(
            InventorySlotUI[] slots,
            ref int index,
            InventoryItemDefinition item) {
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
