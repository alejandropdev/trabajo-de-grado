using System;
using Nexus.Core.Minijuegos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Minijuegos {
    /// <summary>
    /// Prefab "FilaCommit". Toda la maquetacion (posiciones, fuentes, tamanos)
    /// se hace en el editor; este script solo rellena texto y cambia color.
    /// </summary>
    public sealed class FilaCommitView : MonoBehaviour {
        [SerializeField] private Image _fondo;
        [SerializeField] private TMP_Text _fecha;
        [SerializeField] private TMP_Text _mensaje;
        [SerializeField] private TMP_Text _autor;
        [SerializeField] private Button _boton;
        [SerializeField] private Image _chipAutor;   // opcional: puede quedar vacio

        public string CommitId { get; private set; }

        public void Rellenar(Commit c, string fechaFormateada, Action<string> alPinchar) {
            CommitId = c.Id;
            _fecha.text = fechaFormateada;
            _mensaje.text = c.Mensaje;
            _autor.text = c.Autor;
            _boton.onClick.RemoveAllListeners();
            _boton.onClick.AddListener(() => alPinchar(CommitId));
        }

        /// <summary>Solo se llama con andamiaje 3. Ver LienzoGrafoView.AsignarColoresAutor.</summary>
        public void PintarAutor(Color c) {
            if (_autor != null) _autor.color = c;
            if (_chipAutor != null) _chipAutor.color = c;
        }

        public void PintarFondo(Color color) { _fondo.color = color; }
        public void Bloquear() { _boton.interactable = false; }
    }
}
