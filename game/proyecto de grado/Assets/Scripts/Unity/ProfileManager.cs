
using Nexus.Core;
using UnityEngine;

namespace Nexus.Unity {
    public class ProfileManager : MonoBehaviour {
        public static ProfileManager Instancia { get; private set; }

        public ProfileStore Store { get; private set; }
        public PlayerProfile PerfilActivo { get; private set; }

        private void Awake() {
            if (Instancia != null) {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            DontDestroyOnLoad(gameObject);
            Store = new ProfileStore(new AlmacenDeArchivos());
            Debug.Log("ProfileManager listo. Store es null? " + (Store == null)); // TEMPORAL
        }

        public void SeleccionarPerfil(PlayerProfile perfil) {
            PerfilActivo = perfil;
        }
    }
}
