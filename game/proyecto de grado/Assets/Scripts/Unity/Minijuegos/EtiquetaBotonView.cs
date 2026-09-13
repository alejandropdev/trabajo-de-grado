using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Minijuegos
{
    /// <summary>Prefab "BotonEtiqueta" de la paleta de auditoria.</summary>
    public sealed class EtiquetaBotonView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _texto;
        [SerializeField] private Button _boton;
        [SerializeField] private Image _icono;

        public void Rellenar(string etiqueta, Sprite icono, Action<string> alPulsar)
        {
            _texto.text = etiqueta.Replace('_', ' ').ToUpperInvariant();
            if (_icono != null) { _icono.sprite = icono; _icono.enabled = icono != null; }
            _boton.onClick.RemoveAllListeners();
            _boton.onClick.AddListener(() => alPulsar(etiqueta));
        }

        public void Bloquear() { _boton.interactable = false; }
    }
}
