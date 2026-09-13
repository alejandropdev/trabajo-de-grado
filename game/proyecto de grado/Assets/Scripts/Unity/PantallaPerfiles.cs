using System.Collections.Generic;
using Nexus.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity {
    public class PantallaPerfiles : MonoBehaviour {
        [SerializeField] private Transform contenedorLista;
        [SerializeField] private GameObject prefabFila;
        [SerializeField] private TMP_InputField inputNombreNuevo;
        [SerializeField] private Button botonCrearPerfil;

        private const int MAX_PERFILES = 20;

        private void Awake() {
            botonCrearPerfil.onClick.AddListener(CrearPerfil);
        }

        private void OnEnable() {
            RefrescarLista();
        }

        private void RefrescarLista() {
            Debug.Log("contenedorLista null? " + (contenedorLista == null)
                + " | Instancia null? " + (ProfileManager.Instancia == null)
                + " | Store null? " + (ProfileManager.Instancia?.Store == null)); // TEMPORAL

            foreach (Transform hijo in contenedorLista)
                Destroy(hijo.gameObject);


            List<PlayerProfile> perfiles = ProfileManager.Instancia.Store.ListarPerfiles();

            foreach (var perfil in perfiles) {
                var fila = Instantiate(prefabFila, contenedorLista);
                fila.GetComponent<FilaPerfilUI>().Configurar(perfil, this);
            }

            botonCrearPerfil.interactable = perfiles.Count < MAX_PERFILES;
        }

        private void CrearPerfil() {
            string nombre = inputNombreNuevo.text.Trim();
            if (string.IsNullOrEmpty(nombre)) return;

            ProfileManager.Instancia.Store.CrearPerfil(nombre);
            inputNombreNuevo.text = "";
            RefrescarLista();
        }

        public void SeleccionarPerfil(PlayerProfile perfil) {
            ProfileManager.Instancia.SeleccionarPerfil(perfil);
            // aquí luego navegas a la pantalla de partidas, si aplica
        }

        public void PedirRenombrar(PlayerProfile perfil) {
            inputNombreNuevo.text = perfil.nombreEstudiante;
            botonCrearPerfil.onClick.RemoveAllListeners();
            botonCrearPerfil.onClick.AddListener(() => {
                ProfileManager.Instancia.Store.Renombrar(perfil.idPerfil, inputNombreNuevo.text.Trim());
                inputNombreNuevo.text = "";
                botonCrearPerfil.onClick.RemoveAllListeners();
                botonCrearPerfil.onClick.AddListener(CrearPerfil);
                RefrescarLista();
            });
        }

        public void BorrarPerfil(PlayerProfile perfil) {
            ProfileManager.Instancia.Store.Borrar(perfil.idPerfil);
            RefrescarLista();
        }
    }
}
