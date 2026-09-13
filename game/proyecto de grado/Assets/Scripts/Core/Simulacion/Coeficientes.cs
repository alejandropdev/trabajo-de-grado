using System;
using System.Collections.Generic;

namespace Nexus.Core.Simulacion {
    /// <summary>
    /// Las constantes griegas del modelo (§4.2.2). Viven en el LevelProfile, no en el codigo:
    /// cambiar estos numeros cambia el caracter de un nivel sin recompilar.
    /// Viaja en el guardado YA multiplicado por metodologia, calidad y retrospectivas.
    /// </summary>
    public sealed class Coeficientes {
        public double Alpha = 0.8;              // tasaDeudaPorPresion
        public double Beta = 1.2;               // eficaciaRefactor
        public double Gamma = 0.6;              // tasaFatiga
        public double Delta = 1.0;              // tasaRecuperacion
        public double SensibilidadMoral = 1.0;  // epsilon zeta eta
        public double Theta = 0.5;              // subida de cobertura por QA
        public double Iota = 0.5;               // dilucion de cobertura
        public double Kappa = 0.4;              // dilucion de documentacion
        public double Lambda = 0.4;             // subida de documentacion por esfuerzo

        /// <summary>Pesos de las 4 fuentes del riesgo: cansancio, deuda, cobertura, documentacion.</summary>
        public double[] W = { 0.30, 0.35, 0.20, 0.15 };

        public double K1 = 0.5;                 // peso de la cobertura en la calidad entregada
        public double K2 = 0.5;                 // peso de la documentacion en la calidad entregada

        /// <summary>
        /// Copia profunda. MemberwiseClone solo no basta: W es un array y quedaria compartido,
        /// asi que una retro en la sesion restaurada modificaria tambien el perfil original.
        /// </summary>
        public Coeficientes Clone() {
            var c = (Coeficientes)MemberwiseClone();
            c.W = W == null ? null : (double[])W.Clone();
            return c;
        }

        /// <summary>La metodologia reescribe el bloque entero (ModificadoresModelo).</summary>
        public void MultiplicarPor(Dictionary<string, double> mods) {
            if (mods == null) return;
            foreach (var kv in mods) MultiplicarUno(kv.Key, kv.Value);
        }

        /// <summary>
        /// El punto exacto que toca la retrospectiva: MultiplicarUno("kappa", 0.85).
        /// Es la unica mecanica en la que el jugador modifica una constante de su propio proceso.
        /// "velocidadBase" se ignora a proposito: no es un coeficiente, lo acumula GameSession.
        /// </summary>
        public void MultiplicarUno(string nombre, double factor) {
            switch (nombre == null ? "" : nombre.Trim().ToLowerInvariant()) {
                case "alpha": Alpha *= factor; break;
                case "beta": Beta *= factor; break;
                case "gamma": Gamma *= factor; break;
                case "delta": Delta *= factor; break;
                case "sensibilidadmoral": SensibilidadMoral *= factor; break;
                case "theta": Theta *= factor; break;
                case "iota": Iota *= factor; break;
                case "kappa": Kappa *= factor; break;
                case "lambda": Lambda *= factor; break;
                case "velocidadbase": break;
                default:
                    throw new InvalidOperationException(
                        $"'{nombre}' no es un coeficiente del modelo. Validos: alpha, beta, gamma, delta, " +
                        "sensibilidadMoral, theta, iota, kappa, lambda (y velocidadBase, que aplica GameSession).");
            }
        }
    }
}
