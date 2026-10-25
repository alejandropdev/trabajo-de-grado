using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Mundo3D.Inventario;

namespace Mundo3D.Puertas {
    public sealed class DoorInteraction : MonoBehaviour {
        [Header("Referencias")]
        [SerializeField]
        private DoorController doorController;

        [SerializeField]
        private DoorAccessRequirement accessRequirement;

        [SerializeField]
        private GameObject interactionPrompt;

        [SerializeField]
        private TMP_Text promptText;

        [Header("Configuración")]
        [SerializeField]
        private string playerTag = "Player";

        private bool playerInRange;
        private InventoryController playerInventory;

        private void Awake() {
            if (doorController == null) {
                Debug.LogError(
                    "Asigna DoorController al script DoorInteraction.",
                    this
                );
            }

            if (accessRequirement == null) {
                accessRequirement =
                    GetComponent<DoorAccessRequirement>();
            }

            SetPromptVisible(false);
        }

        private void Update() {
            if (!playerInRange || doorController == null)
                return;

            if (doorController.IsMoving) {
                SetPromptVisible(false);
                return;
            }

            UpdatePromptText();
            SetPromptVisible(true);

            if (Keyboard.current == null)
                return;

            if (!Keyboard.current.eKey.wasPressedThisFrame)
                return;

            InteractWithDoor();
        }

        private void OnTriggerEnter(Collider other) {
            if (!other.CompareTag(playerTag))
                return;

            InventoryController inventory =
                other.GetComponentInParent<InventoryController>();

            if (inventory == null) {
                Debug.LogWarning(
                    "No se encontró InventoryController en el jugador.",
                    this
                );

                return;
            }

            playerInventory = inventory;
            playerInRange = true;

            UpdatePromptText();

            if (doorController != null && !doorController.IsMoving)
                SetPromptVisible(true);
        }

        private void OnTriggerExit(Collider other) {
            if (!other.CompareTag(playerTag))
                return;

            playerInRange = false;
            playerInventory = null;

            SetPromptVisible(false);
        }

        private void InteractWithDoor() {
            if (doorController == null)
                return;

            // Una puerta abierta siempre se puede cerrar.
            if (doorController.IsOpen) {
                doorController.CloseDoor();
                SetPromptVisible(false);
                return;
            }

            // Si existe un requisito, se valida antes de abrir.
            if (accessRequirement != null) {
                if (!accessRequirement.CanAccess(playerInventory)) {
                    Debug.Log(
                        "Acceso denegado: necesitas la tarjeta requerida.",
                        this
                    );

                    UpdatePromptText();
                    return;
                }
            }

            doorController.OpenDoor();
            SetPromptVisible(false);
        }

        private void UpdatePromptText() {
            if (promptText == null || doorController == null)
                return;

            promptText.text = doorController.IsOpen
                ? "CERRAR PUERTA"
                : "ABRIR PUERTA";
        }

        private void SetPromptVisible(bool visible) {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(visible);
        }
    }
}
