using System;
using System.Collections.Generic;
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

        [Header("Aviso de tiempo")]
        [SerializeField, Min(0)]
        private float warningSeconds = 30f;

        [SerializeField]
        private Color warningColor = new Color(0.98f, 0.75f, 0.14f);

        private float remainingTime;
        private bool isRunning;
        private bool exhaustedRaised;
        private Color normalColor = Color.white;

        // Quien tiene el reloj parado (la pausa, el inventario…). Mientras haya alguno, no corre.
        private readonly HashSet<object> pauseReasons = new HashSet<object>();

        /// <summary>Se dispara una sola vez, cuando el tiempo llega a cero.</summary>
        public event Action Exhausted;

        public float RemainingTime => remainingTime;
        public bool IsRunning => isRunning;
        public bool IsPaused => pauseReasons.Count > 0;
        public float TotalTime => initialMinutes * 60f + initialSeconds;

        /// <summary>La parte del tiempo que ya se gastó, de 0 a 1.</summary>
        public float UsedFraction =>
            TotalTime <= 0f ? 1f : Mathf.Clamp01(1f - remainingTime / TotalTime);

        private void Awake() {
            remainingTime = TotalTime;
            isRunning = remainingTime > 0f;

            if (timerText != null)
                normalColor = timerText.color;

            UpdateTimerDisplay();
        }

        private void Update() {
            if (!isRunning || IsPaused)
                return;

            // Tiempo real (no escalado): la pausa lo para con Pause(), no con Time.timeScale.
            remainingTime -= Time.unscaledDeltaTime;

            if (remainingTime <= 0f) {
                remainingTime = 0f;
                isRunning = false;
            }

            UpdateTimerDisplay();

            if (!isRunning && !exhaustedRaised) {
                exhaustedRaised = true;
                Exhausted?.Invoke();
            }
        }

        private void UpdateTimerDisplay() {
            if (timerText == null)
                return;

            int totalSeconds = Mathf.CeilToInt(remainingTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            timerText.text = $"{minutes:00}:{seconds:00}";
            timerText.color = remainingTime <= warningSeconds ? warningColor : normalColor;
        }

        /// <summary>Para el reloj mientras 'reason' lo tenga parado. Se puede llamar varias veces.</summary>
        public void Pause(object reason) {
            if (reason != null)
                pauseReasons.Add(reason);
        }

        /// <summary>Quita el motivo; el reloj vuelve a correr cuando no queda ninguno.</summary>
        public void Resume(object reason) {
            if (reason != null)
                pauseReasons.Remove(reason);
        }

        public void StartTimer() {
            if (remainingTime > 0f)
                isRunning = true;
        }

        public void StopTimer() {
            isRunning = false;
        }

        public void ResetTimer() {
            remainingTime = TotalTime;
            isRunning = remainingTime > 0f;
            exhaustedRaised = false;
            pauseReasons.Clear();
            UpdateTimerDisplay();
        }
    }
}
