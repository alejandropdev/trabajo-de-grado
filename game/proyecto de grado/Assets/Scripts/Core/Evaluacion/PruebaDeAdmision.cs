using System;
using System.Collections.Generic;

namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// El pre-test diegetico (§2.7): la entrevista de admision del N0. Quince preguntas que cubren los
    /// temas de Fundamentos de IS. No toca el WorldState ni ningun flag: es un instrumento de medida,
    /// no una mecanica. Lo que produce va a PlayerProfile.resultadoPreTest, y el mismo archivo sirve
    /// para el post-test de la entrevista de salida.
    /// </summary>
    public sealed class PruebaDeAdmision {
        public int Version;

        /// <summary>Quien hace las preguntas en voz alta. HH solo susurra.</summary>
        public PersonajeDeEscena Entrevistadora;

        public List<PreguntaDeAdmision> Preguntas = new List<PreguntaDeAdmision>();

        public PreguntaDeAdmision PorNumero(int numero) {
            if (Preguntas == null) return null;
            foreach (var p in Preguntas)
                if (p != null && p.Numero == numero) return p;
            return null;
        }
    }

    public sealed class PersonajeDeEscena {
        public string Id;
        public string Nombre;
        public int Edad;
        public string Rol;
        public string Descripcion;
    }

    public sealed class PreguntaDeAdmision {
        /// <summary>1..15. Es la posicion en resultadoPreTest: la pregunta 1 va al indice 0.</summary>
        public int Numero;

        /// <summary>El objetivo de aprendizaje que mide. Es lo que permite leer la ganancia por tema.</summary>
        public string Oa;

        public string Enunciado;
        public List<OpcionDePregunta> Opciones = new List<OpcionDePregunta>();

        /// <summary>El id de la opcion correcta.</summary>
        public string Correcta;

        public string SusurroSiAcierta;
        public string SusurroSiFalla;
    }

    public sealed class OpcionDePregunta {
        public string Id;
        public string Texto;
    }

    /// <summary>
    /// Corrige la entrevista y calcula la ganancia de Hake. Funciones puras: la pantalla de la entrevista
    /// solo tiene que ir juntando las respuestas y llamar aqui al final.
    /// </summary>
    public static class CorrectorDeAdmision {
        public const int NumeroDePreguntas = 15;

        /// <summary>
        /// Un 1 por acierto y un 0 por fallo, en el orden de las preguntas. Una pregunta sin responder
        /// cuenta como fallo: en una entrevista, callarse tambien es una respuesta.
        /// </summary>
        public static int[] Corregir(PruebaDeAdmision prueba, IDictionary<int, string> respuestas) {
            if (prueba == null) throw new ArgumentNullException(nameof(prueba));

            var resultado = new int[NumeroDePreguntas];
            for (var numero = 1; numero <= NumeroDePreguntas; numero++) {
                var pregunta = prueba.PorNumero(numero);
                if (pregunta == null)
                    throw new InvalidOperationException($"La prueba no tiene la pregunta {numero}.");

                string elegida;
                var acierta = respuestas != null && respuestas.TryGetValue(numero, out elegida) &&
                              string.Equals(elegida, pregunta.Correcta, StringComparison.Ordinal);
                resultado[numero - 1] = acierta ? 1 : 0;
            }
            return resultado;
        }

        /// <summary>Lo que HH le susurra a la secretaria despues de una respuesta.</summary>
        public static string Susurro(PreguntaDeAdmision pregunta, string opcionElegida) {
            if (pregunta == null) throw new ArgumentNullException(nameof(pregunta));
            return string.Equals(opcionElegida, pregunta.Correcta, StringComparison.Ordinal)
                ? pregunta.SusurroSiAcierta
                : pregunta.SusurroSiFalla;
        }

        /// <summary>De 0 a 100.</summary>
        public static double Porcentaje(int[] resultado) {
            if (resultado == null || resultado.Length == 0) return 0;
            var aciertos = 0;
            foreach (var r in resultado) if (r > 0) aciertos++;
            return 100.0 * aciertos / resultado.Length;
        }

        /// <summary>
        /// Ganancia normalizada de Hake: g = (post − pre) / (100 − pre), sobre porcentajes. Mide que parte
        /// de lo que le faltaba por aprender aprendio. Null si el pre-test ya era perfecto: no queda nada que
        /// ganar y la formula dividiria por cero.
        /// </summary>
        public static double? GananciaDeHake(int[] pre, int[] post) {
            var p = Porcentaje(pre);
            if (p >= 100.0) return null;
            return (Porcentaje(post) - p) / (100.0 - p);
        }

        /// <summary>Guarda el pre-test en el perfil. Es lo unico que hace la entrevista fuera de la escena.</summary>
        public static void GuardarPreTest(PlayerProfile perfil, int[] resultado) {
            if (perfil == null) throw new ArgumentNullException(nameof(perfil));
            if (resultado == null || resultado.Length != NumeroDePreguntas)
                throw new ArgumentException($"El pre-test tiene {NumeroDePreguntas} respuestas.", nameof(resultado));
            perfil.resultadoPreTest = (int[])resultado.Clone();
        }
    }
}
