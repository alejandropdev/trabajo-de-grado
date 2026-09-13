using System;
using Nexus.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity {
    public class FilaPerfilUI : MonoBehaviour {
        [SerializeField] private TMP_Text textNombre;
        [SerializeField] private TMP_Text textFecha;
        [SerializeField] private Button btn_Seleccionar;
        [SerializeField] private Button btn_Renombrar;
        [SerializeField] private Button btn_Borrar;

        private PlayerProfile _perfil;
        private PantallaPerfiles _pantalla;

        public void Configurar(PlayerProfile perfil, PantallaPerfiles pantalla) {
            _perfil = perfil;
            _pantalla = pantalla;

            textNombre.text = perfil.nombreEstudiante;
            textFecha.text = DateTime.Parse(perfil.fechaCreacion).ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            btn_Seleccionar.onClick.RemoveAllListeners();
            btn_Seleccionar.onClick.AddListener(() => _pantalla.SeleccionarPerfil(_perfil));

            btn_Renombrar.onClick.RemoveAllListeners();
            btn_Renombrar.onClick.AddListener(() => _pantalla.PedirRenombrar(_perfil));

            btn_Borrar.onClick.RemoveAllListeners();
            btn_Borrar.onClick.AddListener(() => _pantalla.BorrarPerfil(_perfil));
        }
    }
}
