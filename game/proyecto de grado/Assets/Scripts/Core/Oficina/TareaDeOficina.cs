using System;
using System.Collections.Generic;

namespace Nexus.Core.Oficina {
    /// <summary>
    /// El trabajo de oficina: lo que se puede hacer en el escritorio cuando no suena nada. Cada tarea es un
    /// minijuego de practica (una escena que NO esta en el indice del director, asi que nunca llega como
    /// alerta) y da recursos segun como salga. No cuenta para la evaluacion: sin traza, sin competencia, sin
    /// consecuencias diferidas. Es trabajo, no examen.
    /// </summary>
    public sealed class TareaDeOficina {
        public string Id;
        public string Titulo;
        public string Descripcion;
        /// <summary>Lo que mejora, en palabras: «Documentación y deuda técnica».</summary>
        public string Mejora;
        /// <summary>El id de la escena de practica y su archivo (como en el indice de minijuegos).</summary>
        public string Minijuego;
        public string Archivo;
        /// <summary>Lo que cuesta del reloj del dia, al empezarla. En ese rato pueden sonar (y caducar) avisos.</summary>
        public int Minutos = 45;
        public int VecesPorDia = 1;
        /// <summary>resultado (todos | parcial | falsoPositivo | omitido) → cambios en el WorldState.</summary>
        public Dictionary<string, Dictionary<string, double>> Efectos =
            new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);

        public TareaDeOficina Clone() {
            var c = (TareaDeOficina)MemberwiseClone();
            c.Efectos = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
            if (Efectos != null)
                foreach (var kv in Efectos)
                    c.Efectos[kv.Key] = kv.Value == null ? null : new Dictionary<string, double>(kv.Value, StringComparer.Ordinal);
            return c;
        }
    }

    public sealed class OficinaConfig {
        public List<TareaDeOficina> Tareas = new List<TareaDeOficina>();

        public TareaDeOficina PorId(string id) {
            if (Tareas == null) return null;
            foreach (var t in Tareas) if (t != null && t.Id == id) return t;
            return null;
        }

        public OficinaConfig Clone() {
            var c = new OficinaConfig();
            if (Tareas != null) foreach (var t in Tareas) c.Tareas.Add(t == null ? null : t.Clone());
            return c;
        }
    }
}
