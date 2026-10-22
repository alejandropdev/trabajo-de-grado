using UnityEngine;

namespace Mundo3D.Puertas {
    public sealed class DoorAccessRequirement : MonoBehaviour {
        [Header("Requisito de acceso")]
        [SerializeField]
        private bool requiresAuthorization = false;

        [Header("Estado de autorización")]
        [SerializeField]
        private bool authorizationGranted = false;

        public bool RequiresAuthorization => requiresAuthorization;

        public bool AuthorizationGranted => authorizationGranted;

        /// Indica si la puerta permite el acceso según su configuración actual.
        public bool CanAccess() {
            if (!requiresAuthorization)
                return true;

            return authorizationGranted;
        }

        /// Concede autorización a través de otro componente.
        public void GrantAccess() {
            authorizationGranted = true;
        }

        /// Revoca la autorización concedida.
        public void RevokeAccess() {
            authorizationGranted = false;
        }
    }
}
