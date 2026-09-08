using UnityEngine;

public class MenuController : MonoBehaviour {
    [SerializeField] private GameObject panelMenuPrincipal;
    [SerializeField] private GameObject panelPartidasGuardadas;
    [SerializeField] private GameObject panelPerfiles;

    public void MostrarPartidasGuardadas() {
        panelMenuPrincipal.SetActive(false);
        panelPartidasGuardadas.SetActive(true);
    }

    public void MostrarPerfiles() {
        panelMenuPrincipal.SetActive(false);
        panelPerfiles.SetActive(true);
    }

    public void VolverAlMenuPrincipal() {
        panelPartidasGuardadas.SetActive(false);
        panelPerfiles.SetActive(false);
        panelMenuPrincipal.SetActive(true);
    }
}
