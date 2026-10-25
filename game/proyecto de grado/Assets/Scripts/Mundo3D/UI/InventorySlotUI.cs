using System;
using UnityEngine;
using UnityEngine.UI;
using Mundo3D.Inventario;

namespace Mundo3D.UI {
    public sealed class InventorySlotUI : MonoBehaviour {
        [Header("Componentes del slot")]
        [SerializeField] private Button button;
        [SerializeField] private Image itemIcon;

        private InventoryItemDefinition item;
        private Action<InventoryItemDefinition> selectionCallback;

        private void Awake() {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(HandleClick);
            else
                Debug.LogError(
                    $"InventorySlotUI: falta Button en {name}.",
                    this
                );
        }

        private void OnDestroy() {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void SetSelectionCallback(
            Action<InventoryItemDefinition> callback
        ) {
            selectionCallback = callback;
        }

        public void SetItem(InventoryItemDefinition definition) {
            item = definition;

            if (itemIcon != null) {
                Sprite sprite = item != null ? item.Icon : null;

                itemIcon.sprite = sprite;
                itemIcon.enabled = sprite != null;
                itemIcon.preserveAspect = true;
                itemIcon.raycastTarget = false;
            }

            if (button != null)
                button.interactable = item != null;
        }

        public void Clear() {
            SetItem(null);
        }

        private void HandleClick() {
            if (item == null)
                return;

            selectionCallback?.Invoke(item);
        }
    }
}
