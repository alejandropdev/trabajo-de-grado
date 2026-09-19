using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// El post-mortem lite (§4.9.5): las 5 decisiones incorrectas mas recientes, de la mas nueva
    /// a la mas vieja, señalando la raiz. GameSession.ConstruirCadenaCausal() delega aqui.
    /// La leccion es la distancia: lo que decidiste el dia 2 importa mas que lo que hiciste hoy.
    /// </summary>
    public static class CadenaCausal {
        public const int Maximo = 5;
        public const string SinDecisionesIncorrectas = "Lo que salió mal, salió mal por el sistema, no por ti.";

        public static List<string> Construir(DecisionTrace traza) {
            var incorrectas = traza == null
                ? new List<EntradaTraza>()
                : traza.Entradas.Where(e => e != null && e.Veredicto == Veredictos.Incorrecta).ToList();

            if (incorrectas.Count == 0) return new List<string> { SinDecisionesIncorrectas };

            // La traza es cronologica: las ultimas entradas son las mas recientes.
            var cadena = incorrectas.Skip(Math.Max(0, incorrectas.Count - Maximo)).Reverse().ToList();

            var lineas = cadena.Select(e => $"Día {e.Dia,2} · {e.Titulo} → «{e.OpcionTexto}»").ToList();

            var ultima = cadena[0];
            var raiz = cadena[cadena.Count - 1];
            var distancia = ultima.Dia - raiz.Dia;

            if (cadena.Count == 1 || distancia <= 0) {
                lineas.Add($"  ↑ La raíz está aquí, en el día {raiz.Dia}.");
            } else {
                lineas.Add($"  ↑ La raíz está aquí, en el día {raiz.Dia}, {distancia} días antes de «{ultima.Titulo}».");
                lineas.Add("    Entre ambas hubo tiempo de sobra para corregirlo, y ninguna alarma sonó.");
            }
            return lineas;
        }
    }
}
