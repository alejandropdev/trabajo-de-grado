using System.Collections;
using UnityEngine;

namespace Mundo3D.Puertas {
    public sealed class DoorController : MonoBehaviour {
        [Header("Referencia de la puerta")]
        [SerializeField]
        private Transform movingPart;

        [Header("Configuración de apertura")]
        [SerializeField]
        private Vector3 openOffset = new Vector3(0f, 0f, 2f);

        [SerializeField, Min(0.01f)]
        private float movementDuration = 1f;

        [Header("Estado inicial")]
        [SerializeField]
        private bool startOpen = false;

        private Vector3 closedPosition;
        private Vector3 openPosition;

        private Coroutine movementCoroutine;

        public bool IsOpen { get; private set; }
        public bool IsMoving { get; private set; }

        private void Awake() {
            if (movingPart == null) {
                Debug.LogError(
                    $"[{nameof(DoorController)}] No se ha asignado " +
                    $"Moving Part en {gameObject.name}.",
                    this
                );

                enabled = false;
                return;
            }

            closedPosition = movingPart.localPosition;
            openPosition = closedPosition + openOffset;

            IsOpen = startOpen;

            movingPart.localPosition = IsOpen
                ? openPosition
                : closedPosition;
        }

        public void OpenDoor() {
            if (IsOpen && !IsMoving)
                return;

            IsOpen = true;
            StartMovement(openPosition);
        }

        public void CloseDoor() {
            if (!IsOpen && !IsMoving)
                return;

            IsOpen = false;
            StartMovement(closedPosition);
        }

        public void ToggleDoor() {
            if (IsOpen)
                CloseDoor();
            else
                OpenDoor();
        }

        private void StartMovement(Vector3 targetPosition) {
            if (movementCoroutine != null) {
                StopCoroutine(movementCoroutine);
            }

            movementCoroutine = StartCoroutine(
                MoveDoor(targetPosition)
            );
        }

        private IEnumerator MoveDoor(Vector3 targetPosition) {
            IsMoving = true;

            Vector3 initialPosition = movingPart.localPosition;
            float elapsedTime = 0f;
            float duration = Mathf.Max(0.01f, movementDuration);

            while (elapsedTime < duration) {
                elapsedTime += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / duration
                );

                float smoothProgress = Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

                movingPart.localPosition = Vector3.Lerp(
                    initialPosition,
                    targetPosition,
                    smoothProgress
                );

                yield return null;
            }

            movingPart.localPosition = targetPosition;

            IsMoving = false;
            movementCoroutine = null;
        }
    }
}
