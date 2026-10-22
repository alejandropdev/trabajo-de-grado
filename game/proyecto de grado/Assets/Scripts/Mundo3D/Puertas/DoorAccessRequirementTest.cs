using UnityEngine;

namespace Mundo3D.Puertas {
    public sealed class DoorAccessRequirementTest : MonoBehaviour {
        [SerializeField]
        private DoorAccessRequirement accessRequirement;

        [SerializeField]
        private bool testAuthorization;

        private void Start() {
            if (accessRequirement == null) {
                Debug.LogError(
                    "Asigna DoorAccessRequirement al script de prueba.",
                    this
                );

                return;
            }

            bool canAccess = accessRequirement.CanAccess();

            Debug.Log(
                $"Acceso inicial permitido: {canAccess}",
                this
            );

            if (testAuthorization) {
                accessRequirement.GrantAccess();

                Debug.Log(
                    $"Acceso después de autorizar: " +
                    $"{accessRequirement.CanAccess()}",
                    this
                );

                accessRequirement.RevokeAccess();

                Debug.Log(
                    $"Acceso después de revocar: " +
                    $"{accessRequirement.CanAccess()}",
                    this
                );
            }
        }
    }
}
