using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mundo3D.Inventario;

namespace Mundo3D.UI {
    public sealed class InventoryItemDetailsNavigator : MonoBehaviour {
        [Header("Inventario")]
        [SerializeField] private InventoryController inventory;

        [Header("Panel de información")]
        [SerializeField] private GameObject detailsPanel;
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;
        [SerializeField] private TMP_Text itemCounterText;

        [Header("Botones de navegación")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;

        private readonly List<InventoryItemDefinition> visibleItems =
            new List<InventoryItemDefinition>();

        private InventoryItemCategory currentCategory;
        private int currentIndex = 0;
        private bool categorySelected = false;

        private void Awake() {
            if (detailsPanel != null)
                detailsPanel.SetActive(false);

            if (previousButton != null)
                previousButton.onClick.AddListener(ShowPrevious);

            if (nextButton != null)
                nextButton.onClick.AddListener(ShowNext);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnEnable() {
            if (inventory == null) {
                Debug.LogError(
                    "InventoryItemDetailsNavigator: asigna el InventoryController.",
                    this
                );
                return;
            }

            inventory.InventoryChanged += HandleInventoryChanged;
        }

        private void OnDisable() {
            if (inventory != null)
                inventory.InventoryChanged -= HandleInventoryChanged;
        }

        private void OnDestroy() {
            if (previousButton != null)
                previousButton.onClick.RemoveListener(ShowPrevious);

            if (nextButton != null)
                nextButton.onClick.RemoveListener(ShowNext);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void OpenDeveloperCards() {
            OpenCategory(InventoryItemCategory.DeveloperCard);
        }

        public void OpenOfficeNotes() {
            OpenCategory(InventoryItemCategory.OfficeNote);
        }

        public void OpenUsbEvidence() {
            OpenCategory(InventoryItemCategory.Evidence);
        }

        public void OpenCategory(InventoryItemCategory category) {
            if (inventory == null) {
                Debug.LogError(
                    "No se puede abrir el detalle: falta InventoryController.",
                    this
                );
                return;
            }

            currentCategory = category;
            categorySelected = true;
            currentIndex = 0;

            RefreshVisibleItems();

            if (detailsPanel != null)
                detailsPanel.SetActive(true);

            RefreshDetails();
        }

        public void Close() {
            if (detailsPanel != null)
                detailsPanel.SetActive(false);
        }

        public void ShowPrevious() {
            if (visibleItems.Count <= 1)
                return;

            currentIndex--;

            if (currentIndex < 0)
                currentIndex = visibleItems.Count - 1;

            RefreshDetails();
        }

        public void ShowNext() {
            if (visibleItems.Count <= 1)
                return;

            currentIndex++;

            if (currentIndex >= visibleItems.Count)
                currentIndex = 0;

            RefreshDetails();
        }

        private void HandleInventoryChanged() {
            if (!categorySelected)
                return;

            string currentItemId = null;

            if (currentIndex >= 0 &&
                currentIndex < visibleItems.Count &&
                visibleItems[currentIndex] != null) {
                currentItemId = visibleItems[currentIndex].Id;
            }

            RefreshVisibleItems();

            // Conserva la pieza seleccionada si sigue en la colección.
            if (!string.IsNullOrWhiteSpace(currentItemId)) {
                int newIndex = visibleItems.FindIndex(
                    item => item != null && item.Id == currentItemId
                );

                if (newIndex >= 0)
                    currentIndex = newIndex;
            }

            if (currentIndex >= visibleItems.Count)
                currentIndex = visibleItems.Count - 1;

            if (currentIndex < 0)
                currentIndex = 0;

            RefreshDetails();
        }

        private void RefreshVisibleItems() {
            visibleItems.Clear();

            if (inventory == null)
                return;

            HashSet<string> addedIds = new HashSet<string>();

            foreach (InventoryItemDefinition item
                in inventory.UniqueCollectedItems) {
                if (item == null || item.Category != currentCategory)
                    continue;

                if (string.IsNullOrWhiteSpace(item.Id))
                    continue;

                if (addedIds.Add(item.Id))
                    visibleItems.Add(item);
            }

            if (currentIndex >= visibleItems.Count)
                currentIndex = visibleItems.Count - 1;

            if (currentIndex < 0)
                currentIndex = 0;
        }

        private void RefreshDetails() {
            bool hasItem = visibleItems.Count > 0;

            if (itemImage != null) {
                itemImage.sprite = hasItem
                    ? visibleItems[currentIndex].Icon
                    : null;

                itemImage.enabled =
                    hasItem && visibleItems[currentIndex].Icon != null;

                itemImage.preserveAspect = true;
            }

            if (itemNameText != null) {
                itemNameText.text = hasItem
                    ? visibleItems[currentIndex].DisplayName
                    : "Todavía no has encontrado este objeto";
            }

            if (itemDescriptionText != null) {
                if (!hasItem) {
                    itemDescriptionText.text =
                        "Explora el escenario para encontrar nuevas piezas.";
                }
                else {
                    InventoryItemDefinition item =
                        visibleItems[currentIndex];

                    itemDescriptionText.text =
                        !string.IsNullOrWhiteSpace(item.DetailedDescription)
                            ? item.DetailedDescription
                            : item.Description;
                }
            }

            if (itemCounterText != null) {
                itemCounterText.text = hasItem
                    ? $"{currentIndex + 1}/{visibleItems.Count}"
                    : "0/0";
            }

            if (previousButton != null)
                previousButton.interactable = visibleItems.Count > 1;

            if (nextButton != null)
                nextButton.interactable = visibleItems.Count > 1;
        }
    }
}
