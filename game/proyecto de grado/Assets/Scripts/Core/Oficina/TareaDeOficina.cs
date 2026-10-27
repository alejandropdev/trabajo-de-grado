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
        /// <summary>
        /// Otros casos de la misma practica (otro diagrama, otro backlog, otro plan de pruebas): cada dia sale uno
        /// distinto, y no se repite ninguno hasta que han salido todos. El de Minijuego/Archivo es el primero.
        /// </summary>
        public List<VarianteDeTarea> Variantes = new List<VarianteDeTarea>();
        /// <summary>Lo que cuesta del reloj del dia, al empezarla. En ese rato pueden sonar (y caducar) avisos.</summary>
        public int Minutos = 45;
        public int VecesPorDia = 1;
        /// <summary>resultado (todos | parcial | falsoPositivo | omitido) → cambios en el WorldState.</summary>
        public Dictionary<string, Dictionary<string, double>> Efectos =
            new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);

        /// <summary>Todos los casos: el de siempre y sus variantes.</summary>
        public List<VarianteDeTarea> Casos() {
            var lista = new List<VarianteDeTarea> { new VarianteDeTarea { Minijuego = Minijuego, Archivo = Archivo } };
            if (Variantes != null) foreach (var v in Variantes) if (v != null) lista.Add(v);
            return lista;
        }

        public TareaDeOficina Clone() {
            var c = (TareaDeOficina)MemberwiseClone();
            c.Variantes = Variantes == null ? new List<VarianteDeTarea>() : Variantes.ConvertAll(v => v == null ? null : v.Clone());
            c.Efectos = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
            if (Efectos != null)
                foreach (var kv in Efectos)
                    c.Efectos[kv.Key] = kv.Value == null ? null : new Dictionary<string, double>(kv.Value, StringComparer.Ordinal);
            return c;
        }
    }

    /// <summary>Un caso de una tarea de oficina: la escena de practica y su archivo.</summary>
    public sealed class VarianteDeTarea {
        public string Minijuego;
        public string Archivo;

        public VarianteDeTarea Clone() { return (VarianteDeTarea)MemberwiseClone(); }
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
