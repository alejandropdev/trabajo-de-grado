using TMPro;
using UnityEngine;

namespace Mundo3D.UI {
    public sealed class GameTimerUI : MonoBehaviour {
        [Header("Configuración del temporizador")]
        [SerializeField, Min(0)]
        private int initialMinutes = 5;

        [SerializeField, Range(0, 59)]
        private int initialSeconds = 0;

        [Header("Interfaz")]
        [SerializeField]
        private TMP_Text timerText;

        private float remainingTime;
        private bool isRunning;

        public float RemainingTime => remainingTime;
        public bool IsRunning => isRunning;

        private void Awake() {
            remainingTime =
                initialMinutes * 60f + initialSeconds;

            isRunning = remainingTime > 0f;

            UpdateTimerDisplay();
        }

        private void Update() {
            if (!isRunning)
                return;

            // El temporizador avanza aunque el juego use
            // Time.timeScale = 0, por ejemplo al abrir una pausa.
            remainingTime -= Time.unscaledDeltaTime;

            if (remainingTime <= 0f) {
                remainingTime = 0f;
                isRunning = false;
            }

            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay() {
            if (timerText == null)
                return;

            int totalSeconds = Mathf.CeilToInt(remainingTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        public void StartTimer() {
            if (remainingTime > 0f)
                isRunning = true;
        }

        public void StopTimer() {
            isRunning = false;
        }

        public void ResetTimer() {
            remainingTime =
                initialMinutes * 60f + initialSeconds;

            isRunning = remainingTime > 0f;
            UpdateTimerDisplay();
        }
    }
}
