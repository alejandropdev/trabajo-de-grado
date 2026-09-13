using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nexus.Core.Minijuegos;

namespace Nexus.Core.Datos
{
    /// <summary>
    /// Deserializa y valida un MJ-*.json. La validacion no es opcional:
    /// con 37 archivos, un commit fantasma en zonas[] es un bug de media tarde
    /// si falla en silencio, y de treinta segundos si falla aqui con nombre y linea.
    /// </summary>
    public static class CatalogoMinijuegos
    {
        public static MinijuegoDef Parsear(string json)
        {
            var def = JsonConvert.DeserializeObject<MinijuegoDef>(json);
            var errores = Validar(def);
            if (errores.Count > 0)
                throw new MinijuegoInvalidoException(def?.Id ?? "(sin id)", errores);
            return def;
        }

        public static List<string> Validar(MinijuegoDef def)
        {
            var e = new List<string>();
            if (def == null) { e.Add("El JSON no se pudo deserializar."); return e; }
            if (string.IsNullOrEmpty(def.Id)) e.Add("Falta 'id'.");
            if (string.IsNullOrEmpty(def.ObjetivoAprendizaje)) e.Add("Falta 'objetivoAprendizaje'.");
            if (def.Zonas.Count == 0) e.Add("Un V1 sin zonas no tiene nada que detectar.");

            var ids = new HashSet<string>(def.Artefacto.Commits.Select(c => c.Id));
            if (ids.Count != def.Artefacto.Commits.Count) e.Add("Hay ids de commit repetidos.");

            var ramas = new HashSet<string>(def.Artefacto.Ramas.Select(r => r.Id));
            foreach (var c in def.Artefacto.Commits)
            {
                if (!ramas.Contains(c.Rama)) e.Add($"El commit '{c.Id}' apunta a la rama inexistente '{c.Rama}'.");
                foreach (var p in c.Padres)
                    if (!ids.Contains(p)) e.Add($"El commit '{c.Id}' tiene el padre inexistente '{p}'.");
                if (!string.IsNullOrEmpty(c.Diff) && !def.Artefacto.Diffs.ContainsKey(c.Diff))
                    e.Add($"El commit '{c.Id}' apunta al diff inexistente '{c.Diff}'.");
            }

            foreach (var z in def.Zonas)
            {
                if (z.Commits.Count == 0) e.Add($"La zona '{z.Id}' no cubre ningun commit.");
                foreach (var cid in z.Commits)
                    if (!ids.Contains(cid)) e.Add($"La zona '{z.Id}' apunta al commit inexistente '{cid}'.");
                if (!def.PaletaEtiquetas.Contains(z.Defecto))
                    e.Add($"La zona '{z.Id}' usa la etiqueta '{z.Defecto}', que no esta en paletaEtiquetas.");
                if (string.IsNullOrEmpty(z.Explicacion))
                    e.Add($"La zona '{z.Id}' no explica nada. Sin explicacion no hay leccion.");
            }

            foreach (var s in def.Senuelos)
            {
                foreach (var cid in s.Commits)
                    if (!ids.Contains(cid)) e.Add($"El senuelo '{s.Id}' apunta al commit inexistente '{cid}'.");
                if (string.IsNullOrEmpty(s.RazonNoEsDefecto))
                    e.Add($"El senuelo '{s.Id}' no dice por que no es un defecto.");
                if (def.Zonas.Any(z => z.Commits.Intersect(s.Commits).Any()))
                    e.Add($"El senuelo '{s.Id}' se solapa con una zona real. Seria injusto.");
            }

            foreach (var clave in new[] { "todos", "parcial", "falsoPositivo", "omitido" })
                if (!def.Consecuencias.ContainsKey(clave))
                    e.Add($"Falta la consecuencia '{clave}'.");

            foreach (var kv in def.Consecuencias)
                if (string.IsNullOrEmpty(kv.Value.Rubrica?.Veredicto))
                    e.Add($"La consecuencia '{kv.Key}' no trae rubrica. Sin rubrica no hay Dashboard.");

            return e;
        }
    }

    public sealed class MinijuegoInvalidoException : System.Exception
    {
        public MinijuegoInvalidoException(string id, List<string> errores)
            : base($"Minijuego '{id}' invalido:\n - " + string.Join("\n - ", errores)) { }
    }
}
