using System;
using System.Collections.Generic;

namespace Nexus.Core.Narrativa {
    public static class PrioridadDeBeat {
        /// <summary>La trama lo necesita. Su ventana de dias es una preferencia, no un limite.</summary>
        public const string Obligatorio = "obligatorio";

        /// <summary>Si su ventana pasa sin que se cumplan las precondiciones, se pierde. Y esta bien que se pierda.</summary>
        public const string Opcional = "opcional";

        public static bool EsValida(string prioridad) {
            return string.Equals(prioridad, Obligatorio, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(prioridad, Opcional, StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class VentanaDeBeat {
        public int DiaMin = 1;
        public int DiaMax = int.MaxValue;
    }

    /// <summary>
    /// C7 · Un beat narrativo del catalogo (§8.3.5). Espejo en C# de narrativa.json.
    ///
    /// Es la unidad del principio P3 — "cada beat narrativo es un beat pedagogico" — y de P2:
    /// la escena llega por un canal diegetico, nunca por un HUD que anuncie "atencion, narrativa".
    /// </summary>
    public sealed class NarrativeBeat {
        public string Id;          // CIN-1.2, INT-1…
        public string Nombre;

        public VentanaDeBeat Ventana = new VentanaDeBeat();

        /// <summary>Lo que tiene que ser cierto para que la escena tenga sentido.</summary>
        public List<string> Precondiciones = new List<string>();

        public string Prioridad = PrioridadDeBeat.Opcional;

        /// <summary>
        /// expresion -> variante, mas una clave "default". La PRIMERA cuya expresion sea cierta manda,
        /// asi que el orden del JSON importa y por eso se conserva.
        ///
        /// El coloreo es lo unico que deja que la misma escena suene distinta segun como vaya el proyecto:
        /// "MoralEquipo &lt; 40" -> variante_fria. No cambia lo que pasa, cambia como se cuenta.
        /// </summary>
        public Dictionary<string, string> Coloreo = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Vacia = cualquier nivel. Una cinematica casi siempre pertenece a uno solo.</summary>
        public List<string> SoloNiveles = new List<string>();

        public bool AplicaAlNivel(string nivelId) {
            if (SoloNiveles == null || SoloNiveles.Count == 0) return true;
            foreach (var n in SoloNiveles)
                if (string.Equals(n, nivelId, StringComparison.Ordinal)) return true;
            return false;
        }

        public const string VarianteDefecto = "default";
    }

    /// <summary>C7 · Lo que el motor le pasa a la UI: que escena y en que tono.</summary>
    public sealed class BeatDeHoy {
        public string BeatId;
        public string Nombre;

        /// <summary>La variante de coloreo elegida. Nunca null: si nada aplica, sale la del "default".</summary>
        public string Variante;

        /// <summary>True si es un obligatorio que salio fuera de su ventana preferida.</summary>
        public bool Retrasado;
    }

    /// <summary>Por que el canal narrativo hizo lo que hizo. Es lo que permite depurar una trama que no cuadra.</summary>
    public sealed class DecisionNarrativa {
        public const string Emitido = "emitido";
        public const string FueraDeVentana = "fueraDeVentana";
        public const string Precondiciones = "precondiciones";
        public const string YaSalio = "yaSalio";
        public const string Perdido = "perdido";

        public int Dia;
        public string BeatId;
        public string Resultado;
        public string Detalle;

        public override string ToString() {
            return $"dia {Dia}: {BeatId} · {Resultado}" + (string.IsNullOrEmpty(Detalle) ? "" : " — " + Detalle);
        }
    }
}
