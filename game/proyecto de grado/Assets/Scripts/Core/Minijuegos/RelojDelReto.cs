using System;
using Nexus.Core;

namespace Nexus.Core.Minijuegos {
    /// <summary>
    /// El reloj de un reto segun la dificultad elegida en Opciones. El JSON del reto trae el tiempo «normal»; fácil da
    /// la mitad mas, difícil quita un 30 %. Nunca baja de 30 s: con menos no da tiempo ni a leer el tablero.
    /// </summary>
    public static class RelojDelReto {
        public const float Minimo = 30f;

        public static float Factor(string dificultad) {
            switch (dificultad) {
                case Preferencias.Facil: return 1.5f;
                case Preferencias.Dificil: return 0.7f;
                default: return 1f;
            }
        }

        public static float Segundos(float segundosDelReto, string dificultad) {
            return Math.Max(Minimo, (float)Math.Round(segundosDelReto * Factor(dificultad)));
        }
    }
}
