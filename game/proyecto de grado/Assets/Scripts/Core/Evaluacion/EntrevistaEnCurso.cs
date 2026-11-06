using System;
using System.Collections.Generic;

namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// La entrevista de admision a medio hacer. Vive en el perfil (el pre-test es del estudiante, no de la partida) y
    /// se guarda tras cada respuesta: quien cierra el juego en la pregunta 9 vuelve a la 9, no a la 1.
    /// </summary>
    [Serializable]
    public sealed class EntrevistaEnCurso {
        /// <summary>Numero de pregunta → opcion elegida. null = «no lo sé», que tambien es una respuesta.</summary>
        public Dictionary<int, string> respuestas = new Dictionary<int, string>();
        /// <summary>La pregunta (posicion 0..14) que toca al volver.</summary>
        public int indice;

        /// <summary>Anota una respuesta y deja el indice en la siguiente pregunta.</summary>
        public static EntrevistaEnCurso Anotar(EntrevistaEnCurso actual, int numero, string opcion, int indiceSiguiente) {
            var e = actual ?? new EntrevistaEnCurso();
            if (e.respuestas == null) e.respuestas = new Dictionary<int, string>();
            e.respuestas[numero] = opcion;
            e.indice = Math.Max(0, indiceSiguiente);
            return e;
        }

        /// <summary>Donde retomar: la pregunta guardada, acotada a la prueba; 0 si no habia nada.</summary>
        public static int IndiceDeInicio(EntrevistaEnCurso guardada, int totalDePreguntas) {
            if (guardada == null || totalDePreguntas <= 0) return 0;
            return Math.Max(0, Math.Min(totalDePreguntas - 1, guardada.indice));
        }

        /// <summary>Si hay una entrevista empezada y sin terminar.</summary>
        public static bool HayUnaAMedias(EntrevistaEnCurso e) {
            return e != null && e.respuestas != null && e.respuestas.Count > 0;
        }
    }
}
