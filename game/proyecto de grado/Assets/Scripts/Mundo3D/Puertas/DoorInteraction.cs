using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Mundo3D.Puertas {
    public sealed class DoorInteraction : MonoBehaviour {
        [Header("Referencias")]
        [SerializeField]
        private DoorController doorController;

        [SerializeField]
        private GameObject interactionPrompt;

        [SerializeField]
        private TMP_Text promptText;

        [Header("Configuración")]
        [SerializeField]
        private string playerTag = "Player";

        private Transform playerInRange;

        private void Awake() {
            if (doorController == null) {
                doorController =
                    GetComponentInParent<DoorController>();
            }

            if (doorController == null) {
                Debug.LogError(
                    $"[{nameof(DoorInteraction)}] No se encontró " +
                    $"DoorController en {gameObject.name}.",
                    this
                );

                enabled = false;
                return;
            }

            SetPromptVisible(false);
        }

        private void Update() {
            if (playerInRange == null) {
                SetPromptVisible(false);
                return;
            }

            if (doorController.IsMoving) {
                SetPromptVisible(false);
                return;
            }

            UpdatePromptText();
            SetPromptVisible(true);

            if (Keyboard.current == null)
                return;

            if (Keyboard.current.eKey.wasPressedThisFrame) {
                doorController.ToggleDoor();
            }
        }

        private void OnTriggerEnter(Collider other) {
            if (!other.CompareTag(playerTag))
                return;

            playerInRange = other.transform;
        }

        private void OnTriggerExit(Collider other) {
            if (playerInRange == null)
                return;

            if (other.transform == playerInRange) {
                playerInRange = null;
                SetPromptVisible(false);
            }
        }

        private void UpdatePromptText() {
            if (promptText == null)
                return;

            promptText.text = doorController.IsOpen
                ? "CERRAR PUERTA"
                : "ABRIR PUERTA";
        }

        private void SetPromptVisible(bool visible) {
            if (interactionPrompt != null &&
                interactionPrompt.activeSelf != visible) {
                interactionPrompt.SetActive(visible);
            }
        }
    }
}
