using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// La moneda de horas de la bolsa (2026-11-11): se arrastra hasta un piso del edificio. Mientras se arrastra, una
    /// copia sigue al puntero (la moneda de la bolsa se queda donde esta); al soltarla, dice donde cayo (la pantalla
    /// decide a que piso corresponde). Un clic sin arrastrar no hace nada.
    /// </summary>
    public sealed class FichaDeHoras : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        public bool Activa = true;
        /// <summary>Empieza un arrastre: devuelve el nodo que seguira al puntero (lo crea la pantalla).</summary>
        public Func<RectTransform> CrearCopia;
        /// <summary>Soltada en el punto de pantalla (y la camara del lienzo).</summary>
        public Action<Vector2, Camera> AlSoltar;

        private RectTransform _copia;

        public void OnBeginDrag(PointerEventData e) {
            if (!Activa || CrearCopia == null) return;
            _copia = CrearCopia();
            Mover(e);
        }

        public void OnDrag(PointerEventData e) { Mover(e); }

        public void OnEndDrag(PointerEventData e) {
            if (_copia == null) return;
            var copia = _copia;
            _copia = null;
            if (copia != null) Destroy(copia.gameObject);
            AlSoltar?.Invoke(e.position, e.pressEventCamera);
        }

        private void Mover(PointerEventData e) {
            if (_copia == null) return;
            var padre = (RectTransform)_copia.parent;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position, e.pressEventCamera, out local)) return;
            // El padre tiene el pivot en el centro y el nodo esta anclado arriba a la izquierda.
            _copia.anchoredPosition = new Vector2(local.x + padre.rect.width / 2f, local.y - padre.rect.height / 2f);
        }
    }

    /// <summary>Avisa de cuando el puntero entra, sale o pulsa un nodo (un globo con lo que hace una pieza, sin tener que pulsarla).</summary>
    public sealed class EscuchaDeRaton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        public Action AlEntrar, AlSalir, AlPulsar;
        public void OnPointerEnter(PointerEventData e) { AlEntrar?.Invoke(); }
        public void OnPointerExit(PointerEventData e) { AlSalir?.Invoke(); }
        public void OnPointerClick(PointerEventData e) { AlPulsar?.Invoke(); }
    }
}
