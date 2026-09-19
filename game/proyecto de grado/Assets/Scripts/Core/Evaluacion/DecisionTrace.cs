using System;
using System.Collections.Generic;

namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// Una decision del jugador con su rubrica (§4.8.1). La rubrica sale del JSON de contenido,
    /// no de un modelo: por eso la evaluacion es local, determinista y sin IA (SRS §3.4.5).
    /// </summary>
    public sealed class EntradaTraza {
        public int Dia;
        public string Origen;             // EV-xxx | FASE1 | RETRO | MJ-xxx | LANZAMIENTO
        public string Titulo;
        public string OpcionId;
        public string OpcionTexto;
        public string Veredicto;          // correcta | aceptable | incorrecta
        public string Oa;
        public string Razon;
        public string EstadoAntes;        // WorldState.ToString() antes: la traza es cierta gracias a INV-2
        public string EstadoDespues;      // y despues
        public string NotaDeMetodologia;
    }

    /// <summary>
    /// La traza de decisiones del nivel. Nunca se lee dentro del motor: es puramente de salida
    /// y alimenta el Dashboard de Lecciones y la cadena causal.
    /// </summary>
    public sealed class DecisionTrace {
        public List<EntradaTraza> Entradas = new List<EntradaTraza>();

        public void Registrar(EntradaTraza e) {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (string.IsNullOrEmpty(e.Origen))
                throw new ArgumentException("Una entrada de traza sin 'Origen' no se puede auditar.", nameof(e));
            if (!Veredictos.EsValido(e.Veredicto))
                throw new ArgumentException(
                    $"Veredicto '{e.Veredicto}' desconocido en '{e.Origen}'. Validos: correcta, aceptable, incorrecta.",
                    nameof(e));
            Entradas.Add(e);
        }
    }
}
