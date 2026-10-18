using UnityEngine;

namespace Mundo3D.Coleccionables {
    public sealed class CollectibleAnimator : MonoBehaviour {
        [Header("Flotación")]
        [SerializeField, Min(0f)]
        private float floatAmplitude = 0.2f;

        [SerializeField, Min(0f)]
        private float floatFrequency = 1.5f;

        [Header("Rotación")]
        [SerializeField]
        private Vector3 rotationSpeed = new Vector3(0f, 60f, 0f);

        private Vector3 initialLocalPosition;
        private float phaseOffset;

        private void Awake() {
            initialLocalPosition = transform.localPosition;

            // Desfase para que los objetos no floten sincronizados
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update() {
            AnimateFloating();
            AnimateRotation();
        }

        private void AnimateFloating() {
            float verticalOffset =
                Mathf.Sin(
                    Time.time * floatFrequency + phaseOffset
                ) * floatAmplitude;

            transform.localPosition =
                initialLocalPosition +
                Vector3.up * verticalOffset;
        }

        private void AnimateRotation() {
            transform.Rotate(
                rotationSpeed * Time.deltaTime,
                Space.Self
            );
        }
    }
}
