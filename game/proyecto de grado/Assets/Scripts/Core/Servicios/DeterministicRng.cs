using System;
using System.Collections.Generic;

namespace Nexus.Core.Servicios {
    /// <summary>
    /// C10 · El azar reproducible (§4.10.4). INV-4: nadie sortea sin pasar por aqui.
    ///
    /// El problema que resuelve: un generador no se puede serializar, pero la partida se guarda a mitad
    /// de nivel y al recargar TIENE que seguir la misma secuencia (INV-7). La solucion es contar las tiradas
    /// y quemarlas al restaurar: se guarda (semilla 4417, consumos 37), y al volver se crea el generador
    /// con la semilla 4417 y se tiran 37 numeros a la basura. El numero 38 es el que tocaba.
    ///
    /// Condicion estricta que hace que eso funcione: TODO sale por SiguienteDouble(). Si Next(int) tuviera
    /// su propio camino, contar tiradas dejaria de reconstruir la secuencia y el bug solo aparecería
    /// despues de guardar y recargar, que es cuando nadie esta mirando.
    ///
    /// ---
    /// Por que NO se usa System.Random, aunque la especificacion lo nombre:
    /// System.Random no garantiza la misma secuencia entre runtimes (Microsoft ya cambio el algoritmo una vez),
    /// y este juego apoya en el azar un instrumento de la tesis: "dos personas con la misma semilla juegan
    /// exactamente el mismo escenario, y ahi se puede comparar quien decidio mejor". Un generador propio de
    /// quince lineas da esa garantia en Mono, en IL2CPP y en el arnes de consola, con la misma semilla.
    /// El algoritmo es SplitMix64 (Steele, Lea y Flood, 2014): periodo 2^64 y estado de un solo ulong.
    /// </summary>
    public sealed class DeterministicRng {
        private const ulong Incremento = 0x9E3779B97F4A7C15UL;   // proporcion aurea en 64 bits
        private const ulong Mezcla1 = 0xBF58476D1CE4E5B9UL;
        private const ulong Mezcla2 = 0x94D049BB133111EBUL;

        /// <summary>2^53: el numero de dobles distintos que caben en [0,1) sin perder precision.</summary>
        private const double Escala = 1.0 / 9007199254740992.0;

        private ulong _estado;

        public int Semilla { get; private set; }

        /// <summary>Tiradas gastadas hasta ahora. Viaja en el guardado y es lo unico que hace falta para volver.</summary>
        public int Consumos { get; private set; }

        public DeterministicRng(int semilla) {
            Semilla = semilla;
            _estado = unchecked((ulong)semilla);
            Consumos = 0;
        }

        /// <summary>
        /// Rehidrata el generador quemando las tiradas ya gastadas. Es el paso 1 del orden de
        /// rehidratacion del §7.6, y no es negociable: el scheduler y el director se construyen despues
        /// porque dependen de que el azar ya este en su sitio.
        /// </summary>
        public static DeterministicRng Restaurar(int semilla, int consumos) {
            if (consumos < 0)
                throw new ArgumentOutOfRangeException(nameof(consumos), consumos,
                    "Los consumos del azar no pueden ser negativos: el guardado esta corrupto.");

            var rng = new DeterministicRng(semilla);
            for (var i = 0; i < consumos; i++) rng.SiguienteDouble();
            return rng;
        }

        /// <summary>El UNICO punto de salida del generador. Todo lo demas se construye sobre esto.</summary>
        private double SiguienteDouble() {
            Consumos++;
            unchecked {
                _estado += Incremento;
                var z = _estado;
                z = (z ^ (z >> 30)) * Mezcla1;
                z = (z ^ (z >> 27)) * Mezcla2;
                z = z ^ (z >> 31);
                return (z >> 11) * Escala;   // 53 bits utiles -> [0, 1)
            }
        }

        /// <summary>Un doble en [0, 1).</summary>
        public double NextDouble() {
            return SiguienteDouble();
        }

        /// <summary>
        /// Un entero en [0, maxExclusive). Pasa por SiguienteDouble() a proposito: mezclar dos caminos
        /// de salida romperia la reconstruccion por contador.
        /// </summary>
        public int Next(int maxExclusive) {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive,
                    "El maximo exclusivo debe ser positivo.");

            var v = (int)(SiguienteDouble() * maxExclusive);
            return v >= maxExclusive ? maxExclusive - 1 : v;   // blindaje ante el 0.9999999999999999
        }

        /// <summary>
        /// Elige un indice con probabilidad proporcional a su peso. Los pesos &lt;= 0, NaN o infinitos
        /// no participan. Devuelve -1 si no hay ni un peso positivo, que es como el director dice
        /// "hoy no toca ningun evento" sin inventarse uno.
        ///
        /// Gasta EXACTAMENTE una tirada, tenga la lista dos elementos o doscientos: si el coste en tiradas
        /// dependiera del tamaño del catalogo, añadir un evento cambiaria todas las partidas guardadas.
        /// </summary>
        public int RuletaPonderada(IList<double> pesos) {
            if (pesos == null || pesos.Count == 0) return -1;

            var total = 0.0;
            for (var i = 0; i < pesos.Count; i++)
                if (EsPesoValido(pesos[i])) total += pesos[i];

            if (total <= 0.0) return -1;

            var tirada = SiguienteDouble() * total;
            var acumulado = 0.0;
            var ultimoValido = -1;

            for (var i = 0; i < pesos.Count; i++) {
                if (!EsPesoValido(pesos[i])) continue;
                ultimoValido = i;
                acumulado += pesos[i];
                if (tirada < acumulado) return i;
            }

            return ultimoValido;   // solo se llega aqui por redondeo en coma flotante
        }

        private static bool EsPesoValido(double p) {
            return p > 0.0 && !double.IsNaN(p) && !double.IsInfinity(p);
        }
    }
}
