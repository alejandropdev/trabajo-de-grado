using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;

namespace Nexus.Core.Simulacion {
    /// <summary>
    /// Una consecuencia del estado del proyecto que se nota HOY: con la deuda por las nubes «algo que
    /// funcionaba se rompe»; con la cobertura baja «el cliente encuentra un error». Es lo que hace que las
    /// barras del proyecto, ademas del avance, pesen durante la Fase 2 (feedback beta, ronda 2). Vive en el
    /// JSON del nivel (bloque "incidencias").
    /// </summary>
    public sealed class Incidencia {
        public string Id;
        /// <summary>El stock del WorldState que la provoca: DeudaTecnica, Cobertura, Cansancio…</summary>
        public string Estadistica;
        /// <summary>"&gt;=" o "&lt;=".</summary>
        public string Comparador = ">=";
        public double Umbral;
        public int DesdeDia = 1;
        public string Titulo;
        /// <summary>Lo que pasa, en palabras. Puede llevar {valor}.</summary>
        public string Texto;
        public Dictionary<string, object> Efectos = new Dictionary<string, object>(StringComparer.Ordinal);

        public Incidencia Clone() {
            var c = (Incidencia)MemberwiseClone();
            c.Efectos = Efectos == null ? null : new Dictionary<string, object>(Efectos, StringComparer.Ordinal);
            return c;
        }
    }

    /// <summary>La incidencia de hoy, ya con su valor: lo que la UI enseña.</summary>
    public sealed class IncidenciaDeHoy {
        public string Id;
        public string Estadistica;
        public double Valor;
        public string Titulo;
        public string Texto;
        public Dictionary<string, object> Efectos;
    }

    /// <summary>
    /// Que incidencia toca hoy. Determinista y sin azar (no gasta tiradas del RNG, asi no cambia nada de lo
    /// demas): de las que se cumplen hoy, la que menos veces ha salido; a igualdad, la primera del JSON. Como
    /// mucho una por dia, para que sean noticia y no ruido.
    /// </summary>
    public static class IncidenciasDelEstado {
        public static IncidenciaDeHoy Elegir(IList<Incidencia> incidencias, WorldState w, RuntimeState r) {
            if (incidencias == null || w == null || r == null) return null;
            Incidencia mejor = null;
            var vecesMejor = int.MaxValue;
            foreach (var inc in incidencias) {
                if (inc == null || r.DiaActual < inc.DesdeDia) continue;
                double valor;
                if (!w.TryGet(inc.Estadistica, out valor)) continue;
                var cumple = inc.Comparador == "<=" ? valor <= inc.Umbral : valor >= inc.Umbral;
                if (!cumple) continue;
                int veces;
                r.IncidenciasVistas.TryGetValue(inc.Id ?? "", out veces);
                if (veces < vecesMejor) { mejor = inc; vecesMejor = veces; }
            }
            if (mejor == null) return null;
            double v;
            w.TryGet(mejor.Estadistica, out v);
            return new IncidenciaDeHoy {
                Id = mejor.Id, Estadistica = mejor.Estadistica, Valor = Math.Round(v),
                Titulo = mejor.Titulo, Texto = (mejor.Texto ?? "").Replace("{valor}", Math.Round(v).ToString()),
                Efectos = mejor.Efectos == null ? new Dictionary<string, object>() : new Dictionary<string, object>(mejor.Efectos)
            };
        }
    }
}
