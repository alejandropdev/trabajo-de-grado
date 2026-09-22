using System;
using System.Collections.Generic;

namespace Nexus.Core.Minijuegos {
    /// <summary>
    /// Los seis verbos (§6.1). El hallazgo que estructura todo el diseño de minijuegos:
    /// se dibujo uno por cada uno de los 17 temas y, al compararlos, eran SEIS juegos repetidos
    /// diecisiete veces. Seis escenas parametrizables por JSON, no diecisiete minijuegos.
    ///
    /// El contenido que ya existe escribe "V1_DETECTAR"; la especificacion §4.6 escribe "detectar".
    /// Aqui se admiten las dos formas y se normaliza a la del contenido, que es la que esta en disco.
    /// </summary>
    public static class Verbos {
        public const string Detectar = "V1_DETECTAR";   // señalar el defecto entre señuelos, contrarreloj
        public const string Repartir = "V2_REPARTIR";   // distribuir un presupuesto escaso
        public const string Ordenar = "V3_ORDENAR";     // secuenciar bajo precedencia
        public const string Elegir = "V4_ELEGIR";       // escoger Y registrar la razon
        public const string Predecir = "V5_PREDECIR";   // comprometerse con un numero
        public const string Trazar = "V6_TRAZAR";       // conectar causa y efecto en el tiempo

        private static readonly string[] _todos = { Detectar, Repartir, Ordenar, Elegir, Predecir, Trazar };

        public static IReadOnlyList<string> Todos { get { return _todos; } }

        /// <summary>"detectar", "V1_DETECTAR" o "v1_detectar" dan todos "V1_DETECTAR". null si no existe.</summary>
        public static string Normalizar(string verbo) {
            if (string.IsNullOrEmpty(verbo)) return null;
            var limpio = verbo.Trim();

            foreach (var canonico in _todos) {
                if (string.Equals(canonico, limpio, StringComparison.OrdinalIgnoreCase)) return canonico;
                // "V1_DETECTAR" -> "DETECTAR"
                var corto = canonico.Substring(canonico.IndexOf('_') + 1);
                if (string.Equals(corto, limpio, StringComparison.OrdinalIgnoreCase)) return canonico;
            }
            return null;
        }

        public static bool EsValido(string verbo) { return Normalizar(verbo) != null; }
    }

    /// <summary>
    /// Como termina un minijuego. Es el contrato COMUN de los seis verbos: son las cuatro claves del
    /// bloque 'consecuencias' de cualquier MJ-*.json.
    ///
    /// Nexus.Core.Minijuegos.Detectar.DetectarEvaluador declara las mismas cuatro para su propio uso.
    /// No se reutilizan desde aqui a proposito: el puente al motor vale para los seis verbos y no debe
    /// depender del evaluador de uno solo. Si alguna vez dejaran de coincidir, el validador de esquema
    /// lo cazaria, porque las consecuencias de cada escena se comprueban contra estas.
    /// </summary>
    public static class ResultadosDeMinijuego {
        public const string Todos = "todos";
        public const string Parcial = "parcial";
        public const string FalsoPositivo = "falsoPositivo";
        public const string Omitido = "omitido";

        private static readonly string[] _todos = { Todos, Parcial, FalsoPositivo, Omitido };
        public static IReadOnlyList<string> Vocabulario { get { return _todos; } }

        public static bool EsValido(string resultado) {
            foreach (var r in _todos)
                if (string.Equals(r, resultado, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>
    /// C6 · La entrada del INDICE de minijuegos (minijuegos/indice.json): lo que el motor necesita para
    /// decidir cual toca hoy, y nada mas.
    ///
    /// ★ La invariante del subsistema (§4.6): **la mecanica vive en la UI; el motor solo sabe cual toca.**
    /// Por eso aqui no hay zonas, ni señuelos, ni commits: eso vive en MinijuegoDef, que es la vista de
    /// la ESCENA y ya existia. Son dos vistas del mismo minijuego, y 'archivo' es el puente.
    ///
    /// La especificacion proponia un MinigameDefinition con un Dictionary&lt;string,object&gt; Parametros
    /// para el bloque de cada verbo, y ella misma marcaba el problema: renuncia al tipado. Separando las
    /// dos vistas el problema desaparece — cada verbo tiene su clase tipada en la capa que la juega —
    /// y el motor se queda con diez campos que valen igual para los seis verbos.
    /// </summary>
    public sealed class MinigameDefinition {
        public string Id;

        /// <summary>Del vocabulario de Verbos. Decide que escena abre la UI.</summary>
        public string Verbo;

        /// <summary>Ruta relativa del JSON de la escena: "minijuegos/MJ-F2-02.json".</summary>
        public string Archivo;

        /// <summary>Filtra contra objetivosActivos del nivel. Sin OA, un minijuego no evalua nada.</summary>
        public string ObjetivoAprendizaje;

        /// <summary>"Javier necesita el diagrama para empezar a las 10:00". La presion es diegetica, nunca un cronometro suelto.</summary>
        public string PresionDiegetica;

        /// <summary>Segundos de reloj. 60-120 segun §6.2.</summary>
        public int Reloj = 90;

        public double PesoBase = 10;

        /// <summary>Dias antes de que este minijuego concreto pueda repetirse.</summary>
        public int Enfriamiento = 6;

        public int MaxOcurrencias = 1;

        public List<string> Precondiciones = new List<string>();

        /// <summary>Fases en las que puede salir. Vacia = solo desarrollo, que es donde vive la ventana de las 15:00.</summary>
        public List<string> Fases = new List<string>();

        /// <summary>
        /// Lo que pasa si la alerta caduca sin que nadie la atienda: la consecuencia 'omitido' de SU escena.
        /// No se escribe en el indice; la copia el cargador desde el JSON de la escena. Sin esto, dejar caducar
        /// un minijuego aplicaria un omitido generico y se saltaria lo que la escena encadena (EV-ALC-01).
        /// </summary>
        public Consecuencia ConsecuenciaOmitido;

        /// <summary>Vacia = todos los niveles. Igual que en eventos y escenas.</summary>
        public List<string> SoloNiveles = new List<string>();

        public bool AplicaAlNivel(string nivelId) {
            if (SoloNiveles == null || SoloNiveles.Count == 0) return true;
            foreach (var n in SoloNiveles)
                if (string.Equals(n, nivelId, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>
    /// C6 · Lo que el motor le pasa a la UI cuando toca un minijuego. De ida: esto. De vuelta:
    /// el ResultadoMinijuego que ya existe.
    /// </summary>
    public sealed class PendingMinigame {
        public string MinijuegoId;
        public string Verbo;

        /// <summary>Donde esta el JSON de la escena, para que la UI lo cargue con CatalogoMinijuegos.</summary>
        public string Archivo;

        public string PresionDiegetica;
        public int Segundos;

        /// <summary>0..3, del LevelProfile. Con 3 se resaltan las zonas candidatas; con 0 hay etiquetas trampa.</summary>
        public int NivelAndamiaje;

        public string ObjetivoAprendizaje;
    }

    /// <summary>Por que el director de minijuegos hizo lo que hizo, un dia por entrada.</summary>
    public sealed class DecisionDeMinijuego {
        public const string Propuesto = "propuesto";
        public const string Ritmo = "ritmo";
        public const string SinCandidatos = "sinCandidatos";
        public const string DiaTranquilo = "diaTranquilo";

        public int Dia;
        public string Resultado;
        public string MinijuegoId;
        public int Candidatos;

        public override string ToString() {
            return $"dia {Dia}: {Resultado}" +
                   (string.IsNullOrEmpty(MinijuegoId) ? "" : " " + MinijuegoId) +
                   $" · {Candidatos} candidatos";
        }
    }
}
