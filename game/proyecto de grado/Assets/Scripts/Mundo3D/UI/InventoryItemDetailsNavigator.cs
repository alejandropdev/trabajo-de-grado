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
        [SerializeField] private TMP_Text itemDescriptionText1;
        [SerializeField] private TMP_Text itemDescriptionText2;
        [SerializeField] private TMP_Text itemCounterText;

        [Header("Botones de navegación")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;

        [Header("Botones del panel de inventario")]
        [SerializeField] private GameObject inventoryCloseButton;
        [SerializeField] private GameObject moreInfoButton;

        private readonly List<InventoryItemDefinition> visibleItems =
            new List<InventoryItemDefinition>();

        private InventoryItemCategory currentCategory;
        private int currentIndex;
        private bool categorySelected;

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

            SetInventoryButtonsVisible(false);
            RefreshDetails();
        }

        public void Close() {
            // Solo se cierra el panel detallado.
            // No se debe cerrar PanelInventario.
            if (detailsPanel != null)
                detailsPanel.SetActive(false);

            SetInventoryButtonsVisible(true);
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

        private void SetInventoryButtonsVisible(bool visible) {
            if (inventoryCloseButton != null)
                inventoryCloseButton.SetActive(visible);

            if (moreInfoButton != null)
                moreInfoButton.SetActive(visible);
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

            InventoryItemDefinition item = hasItem
                ? visibleItems[currentIndex]
                : null;

            if (itemImage != null) {
                itemImage.sprite = hasItem ? item.Icon : null;
                itemImage.enabled = hasItem && item.Icon != null;
                itemImage.preserveAspect = true;
            }

            if (itemNameText != null) {
                itemNameText.text = hasItem
                    ? item.DisplayName
                    : "Todavía no has encontrado este objeto";
            }

            if (itemDescriptionText1 != null) {
                itemDescriptionText1.text = hasItem
                    ? item.Description
                    : "Explora el escenario para encontrar nuevas piezas.";
            }

            if (itemDescriptionText2 != null) {
                itemDescriptionText2.text = hasItem
                    ? item.DetailedDescription
                    : string.Empty;
            }

            if (itemCounterText != null) {
                itemCounterText.text = hasItem
                    ? $"{currentIndex + 1}/{visibleItems.Count}"
                    : "0/0";
            }

            bool showArrows = visibleItems.Count > 1;

            if (previousButton != null) {
                previousButton.gameObject.SetActive(showArrows);
                previousButton.interactable = showArrows;
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(showArrows);
                nextButton.interactable = showArrows;
            }
        }
    }
}
