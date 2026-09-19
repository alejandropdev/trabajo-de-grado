using System;

namespace Nexus.Core.Simulacion {
    /// <summary>
    /// La simulacion continua (§4.3.2). Dos operaciones que no hay que mezclar:
    /// Calcular() es una funcion pura, y AvanzarUnDia() es el UNICO punto del motor donde avanza el tiempo
    /// (la unica excepcion deliberada a INV-2: el motor del tiempo escribe el WorldState).
    /// Las funciones de forma son contenido pedagogico: para balancear se tocan los coeficientes, nunca estas curvas.
    /// </summary>
    public static class ForresterModel {
        public const string Dias = "Dias";
        public const string Alcance = "Alcance";
        public const string Avance = "Avance";
        public const string DeudaTecnica = "DeudaTecnica";
        public const string Cobertura = "Cobertura";
        public const string Documentacion = "Documentacion";
        public const string MoralEquipo = "MoralEquipo";
        public const string Cansancio = "Cansancio";
        public const string Competencia = "Competencia";
        public const string SaludJugador = "SaludJugador";
        public const string VelocidadMod = "VelocidadMod";

        /// <summary>Lineal y suave.</summary>
        public static double FMoral(double moral) {
            return 0.5 + 0.5 * (moral / 100.0);
        }

        /// <summary>NO LINEAL: castiga tarde y fuerte.</summary>
        public static double FFatiga(double cansancio) {
            var x = cansancio / 100.0;
            return 1.0 - 0.6 * x * x;
        }

        /// <summary>
        /// NO LINEAL: la deuda es un prestamo, no un impuesto. Casi no se nota hasta pasado el 50
        /// y a partir de ahi se hunde el suelo. Si alguien la cambia por una recta, el test
        /// La_deuda_castiga_de_forma_no_lineal lo caza.
        /// </summary>
        public static double FDeuda(double deuda) {
            var x = deuda / 100.0;
            return 1.0 / (1.0 + 2.0 * x * x);
        }

        /// <summary>Con competencia 50 (el valor inicial) da exactamente 1.0.</summary>
        public static double FComp(double competencia) {
            return 0.6 + 0.8 * (competencia / 100.0);
        }

        /// <summary>Funcion pura: lee el estado y devuelve las derivadas. No muta nada.</summary>
        public static Derived Calcular(IEstadoSimulable w, IContadoresDeSimulacion r, Coeficientes c,
                                       double velocidadBase, double mult) {
            Comprobar(w, r, c);

            var alcance = Leer(w, Alcance);
            var avance = Leer(w, Avance);
            var deuda = Leer(w, DeudaTecnica);
            var cobertura = Leer(w, Cobertura);
            var documentacion = Leer(w, Documentacion);
            var moral = Leer(w, MoralEquipo);
            var cansancio = Leer(w, Cansancio);
            var competencia = Leer(w, Competencia);
            var velocidadMod = Leer(w, VelocidadMod);

            var d = new Derived();
            d.Velocidad = velocidadBase * mult * velocidadMod
                          * FComp(competencia) * FMoral(moral) * FFatiga(cansancio) * FDeuda(deuda);

            d.RiesgoPorCansancio = c.W[0] * cansancio;
            d.RiesgoPorDeuda = c.W[1] * deuda;
            d.RiesgoPorCobertura = c.W[2] * (100.0 - cobertura);
            d.RiesgoPorDocumentacion = c.W[3] * (100.0 - documentacion);
            d.RiesgoLatente = Acotar(d.RiesgoPorCansancio + d.RiesgoPorDeuda
                                     + d.RiesgoPorCobertura + d.RiesgoPorDocumentacion, 0.0, 100.0);

            d.CalidadEntregada = (c.K1 * cobertura + c.K2 * documentacion) * FDeuda(deuda) * FMoral(moral);
            d.LeadTime = r.WipActual / Math.Max(0.1, d.Velocidad / 6.0);
            d.Burndown = alcance - avance;
            return d;
        }

        /// <summary>
        /// Avanza un dia, en el orden exacto de §4.3.2 / §7.2. Los pasos 1-6 se calculan sobre
        /// variables locales y el paso 7 (acotar) ocurre al escribir con Set(): asi ningun paso
        /// intermedio ve un valor ya acotado, igual que en la especificacion.
        /// </summary>
        /// <returns>El avance del dia (incluye el +25 % del overtime).</returns>
        public static double AvanzarUnDia(IEstadoSimulable w, IContadoresDeSimulacion r, Coeficientes c,
                                          double velocidadBase, double mult, bool horasExtra) {
            var d = Calcular(w, r, c, velocidadBase, mult);

            var avance = Leer(w, Avance);
            var deuda = Leer(w, DeudaTecnica);
            var cobertura = Leer(w, Cobertura);
            var documentacion = Leer(w, Documentacion);
            var moral = Leer(w, MoralEquipo);
            var cansancio = Leer(w, Cansancio);
            var competencia = Leer(w, Competencia);
            var salud = Leer(w, SaludJugador);
            var dias = Leer(w, Dias);

            // 1 · el +25 % del overtime es el señuelo
            var avanceDia = d.Velocidad * (horasExtra ? 1.25 : 1.0);
            var avanceAntes = avance;
            avance += avanceDia;

            // 2 · la presion genera deuda
            var presion = (horasExtra ? 3.0 : 0.8) + (r.SobreCompromiso > 0 ? 1.2 : 0.0);
            deuda += c.Alpha * presion;

            // 3 y 5c · cansancio y salud. La salud NO es lineal: tres noches seguidas cuestan mucho mas que el triple
            if (horasExtra) {
                cansancio += c.Gamma * 11.0;
                salud -= 1.5 + 0.8 * Math.Max(0, r.DiasSeguidosTrabajando - 1);
            } else {
                cansancio -= c.Delta * 4.0;
                salud += 2.0;
            }

            // 4 · la moral la paga el cansancio ACUMULADO, no el esfuerzo puntual
            moral += (horasExtra ? -2.5 : 0.6) - c.SensibilidadMoral * 0.02 * cansancio;

            // 5 y 5b · la calidad se diluye al crecer el producto
            var deltaAvance = avance - avanceAntes;
            cobertura -= c.Iota * (deltaAvance / 3.0);
            documentacion -= c.Kappa * (deltaAvance / 3.0);

            // 5d · se aprende documentando
            competencia += 0.15 * (documentacion / 100.0);

            // 6 · el tiempo avanza
            dias += 1.0;

            // 7 · acotar: Set() aplica las reglas de WorldState
            w.Set(Avance, avance);
            w.Set(DeudaTecnica, deuda);
            w.Set(Cansancio, cansancio);
            w.Set(SaludJugador, salud);
            w.Set(MoralEquipo, moral);
            w.Set(Cobertura, cobertura);
            w.Set(Documentacion, documentacion);
            w.Set(Competencia, competencia);
            w.Set(Dias, dias);

            return avanceDia;
        }

        private static void Comprobar(IEstadoSimulable w, IContadoresDeSimulacion r, Coeficientes c) {
            if (w == null) throw new ArgumentNullException(nameof(w));
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (c.W == null || c.W.Length != 4)
                throw new InvalidOperationException("Coeficientes.W debe tener exactamente 4 pesos de riesgo.");
        }

        private static double Leer(IEstadoSimulable w, string nombre) {
            double v;
            if (!w.TryGet(nombre, out v))
                throw new InvalidOperationException($"El estado no expone el stock '{nombre}', que ForresterModel necesita.");
            return v;
        }

        private static double Acotar(double v, double min, double max) {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
