using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Nexus.Core.Simulacion;

namespace Nexus.Core.Modelo {
    /// <summary>
    /// C1 · Los 13 stocks del proyecto mas VelocidadMod (§4.2.1). Es el unico dueño del estado del proyecto:
    /// nadie lo llama, lo leen todos, y solo EffectApplier (C10) y ForresterModel.AvanzarUnDia lo escriben (INV-2).
    ///
    /// El acceso es por NOMBRE y no por propiedad tipada a proposito: permite que un JSON de contenido diga
    /// "DeudaTecnica": 12 sin obligar a un switch en cada consumidor. El precio es que un nombre mal escrito
    /// no lo caza el compilador, asi que Set() lanza nombrando el stock invalido y listando los validos.
    ///
    /// Se reinicia CADA NIVEL (§8.1 B1). Lo que sobrevive entre niveles son los FLG_* (C7), no esto.
    /// </summary>
    public sealed class WorldState : IEstadoSimulable {
        // --- Recursos: lo que se consume ---
        public double Dias;
        public double Dinero;
        public double Alcance;

        // --- Producto: lo que se construye ---
        public double Avance;
        public double DeudaTecnica;
        public double Cobertura;
        public double Documentacion;

        // --- Equipo: las personas ---
        public double MoralEquipo = 60;
        public double Cansancio = 10;
        public double Competencia = 50;

        // --- Entorno: quien te mira ---
        public double SatisfaccionCliente = 50;
        public double Reputacion = 50;

        // --- Persona: tu ---
        public double SaludJugador = 80;

        /// <summary>Multiplicador tecnico de velocidad. Los efectos "+10%" / "-25%" lo MULTIPLICAN, no lo suman.</summary>
        public double VelocidadMod = 1.0;

        private static readonly string[] _nombres = {
            "Dias", "Dinero", "Alcance",
            "Avance", "DeudaTecnica", "Cobertura", "Documentacion",
            "MoralEquipo", "Cansancio", "Competencia",
            "SatisfaccionCliente", "Reputacion",
            "SaludJugador", "VelocidadMod"
        };

        /// <summary>Los nueve que viven entre 0 y 100. Los demas tienen cada uno su regla.</summary>
        private static readonly HashSet<string> _acotadosACien = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "DeudaTecnica", "Cobertura", "Documentacion", "MoralEquipo", "Cansancio",
            "Competencia", "SatisfaccionCliente", "Reputacion", "SaludJugador"
        };

        private static readonly HashSet<string> _todos = new HashSet<string>(_nombres, StringComparer.OrdinalIgnoreCase);

        /// <summary>Los 14 nombres escribibles, en el orden de la especificacion. Lo usa ToString() y los mensajes de error.</summary>
        public static IReadOnlyList<string> Nombres { get { return _nombres; } }

        public static bool EsStock(string nombre) {
            return nombre != null && _todos.Contains(nombre.Trim());
        }

        public static bool EsStockAcotado(string nombre) {
            return nombre != null && _acotadosACien.Contains(nombre.Trim());
        }

        /// <summary>
        /// Condiciones iniciales del nivel (§4.2.3 grupo 1). Lo que el perfil no dice se queda con el valor
        /// inicial de la especificacion: moral 60, cansancio 10, competencia 50, satisfaccion 50,
        /// reputacion 50, salud 80. Son los numeros que hacen que el jugador empiece "normal", no "perfecto".
        /// </summary>
        public static WorldState DesdeNivel(LevelProfile perfil) {
            if (perfil == null) throw new ArgumentNullException(nameof(perfil));
            var w = new WorldState();
            w.Set("Dinero", perfil.PresupuestoInicial);
            w.Set("Alcance", perfil.AlcanceInicial);
            w.Set("DeudaTecnica", perfil.DeudaHeredada);
            w.Set("Cobertura", perfil.CoberturaHeredada);
            w.Set("Documentacion", perfil.DocumentacionHeredada);
            return w;
        }

        public bool TryGet(string nombre, out double valor) {
            switch (Normalizar(nombre)) {
                case "dias": valor = Dias; return true;
                case "dinero": valor = Dinero; return true;
                case "alcance": valor = Alcance; return true;
                case "avance": valor = Avance; return true;
                case "deudatecnica": valor = DeudaTecnica; return true;
                case "cobertura": valor = Cobertura; return true;
                case "documentacion": valor = Documentacion; return true;
                case "moralequipo": valor = MoralEquipo; return true;
                case "cansancio": valor = Cansancio; return true;
                case "competencia": valor = Competencia; return true;
                case "satisfaccioncliente": valor = SatisfaccionCliente; return true;
                case "reputacion": valor = Reputacion; return true;
                case "saludjugador": valor = SaludJugador; return true;
                case "velocidadmod": valor = VelocidadMod; return true;
                default: valor = 0; return false;
            }
        }

        /// <summary>
        /// Escribe un stock aplicando ANTES su acotacion (§4.2.1). Es el paso 7 de AvanzarUnDia y el unico
        /// sitio donde se decide que significa "fuera de rango": ningun consumidor tiene que acordarse.
        /// </summary>
        public void Set(string nombre, double valor) {
            if (double.IsNaN(valor) || double.IsInfinity(valor))
                throw new InvalidOperationException(
                    $"Se intento escribir '{nombre}' con un valor no finito ({valor.ToString(CultureInfo.InvariantCulture)}). " +
                    "Casi siempre viene de una division por cero en un efecto de contenido.");

            switch (Normalizar(nombre)) {
                case "dias": Dias = NoNegativo(valor); return;
                case "dinero": Dinero = valor; return;                        // puede ser negativo: se puede estar en numeros rojos
                case "alcance": Alcance = NoNegativo(valor); return;
                case "avance": Avance = NoNegativo(valor); return;
                case "deudatecnica": DeudaTecnica = ACien(valor); return;
                case "cobertura": Cobertura = ACien(valor); return;
                case "documentacion": Documentacion = ACien(valor); return;
                case "moralequipo": MoralEquipo = ACien(valor); return;
                case "cansancio": Cansancio = ACien(valor); return;
                case "competencia": Competencia = ACien(valor); return;
                case "satisfaccioncliente": SatisfaccionCliente = ACien(valor); return;
                case "reputacion": Reputacion = ACien(valor); return;
                case "saludjugador": SaludJugador = ACien(valor); return;
                case "velocidadmod": VelocidadMod = Math.Max(0.1, valor); return;   // suelo 0.1: nunca 0, o el proyecto se congela
                default:
                    throw new InvalidOperationException(
                        $"'{nombre}' no es un stock del WorldState. Validos: {string.Join(", ", _nombres)}.");
            }
        }

        /// <summary>Copia independiente. Todos los campos son double, asi que MemberwiseClone ya es copia profunda.</summary>
        public WorldState Clone() {
            return (WorldState)MemberwiseClone();
        }

        /// <summary>
        /// Linea legible para EstadoAntes / EstadoDespues de la traza (§4.8.1). SIEMPRE en cultura invariante:
        /// sin esto, la misma partida se audita con coma decimal en una maquina y con punto en otra.
        /// Recorre Nombres, asi que no puede quedar desincronizada de la lista de stocks.
        /// </summary>
        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var nombre in _nombres) {
                double v;
                TryGet(nombre, out v);
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(nombre).Append('=')
                  .Append(v.ToString(nombre == "VelocidadMod" ? "F2" : "F1", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static string Normalizar(string nombre) {
            return nombre == null ? "" : nombre.Trim().ToLowerInvariant();
        }

        private static double ACien(double v) {
            return v < 0.0 ? 0.0 : (v > 100.0 ? 100.0 : v);
        }

        private static double NoNegativo(double v) {
            return v < 0.0 ? 0.0 : v;
        }
    }
}
