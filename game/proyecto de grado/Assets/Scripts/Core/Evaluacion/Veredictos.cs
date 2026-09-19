namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// Vocabulario cerrado de la rubrica (§11.2). Escala: correcta = 1 · aceptable = 0.5 · incorrecta = 0.
    /// </summary>
    public static class Veredictos {
        public const string Correcta = "correcta";
        public const string Aceptable = "aceptable";
        public const string Incorrecta = "incorrecta";

        public static bool EsValido(string veredicto) {
            return veredicto == Correcta || veredicto == Aceptable || veredicto == Incorrecta;
        }
    }
}
