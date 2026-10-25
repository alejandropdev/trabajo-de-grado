using UnityEngine;
using UnityEngine.InputSystem;

namespace Mundo3D.Puertas {
    public sealed class DoorControllerTest : MonoBehaviour {
        [SerializeField]
        private DoorController door;

        private void Update() {
            if (door == null || Keyboard.current == null)
                return;

            if (Keyboard.current.oKey.wasPressedThisFrame)
                door.OpenDoor();

            if (Keyboard.current.cKey.wasPressedThisFrame)
                door.CloseDoor();

            if (Keyboard.current.tKey.wasPressedThisFrame)
                door.ToggleDoor();
        }
    }
}
