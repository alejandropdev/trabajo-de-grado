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

            switch (Verbos.Normalizar(def.Verbo))
            {
                case Verbos.Ordenar: ValidarOrdenar(def, e); break;
                case Verbos.Repartir: ValidarRepartir(def, e); break;
                default: ValidarDetectar(def, e); break;
            }

            foreach (var clave in new[] { "todos", "parcial", "falsoPositivo", "omitido" })
                if (!def.Consecuencias.ContainsKey(clave))
                    e.Add($"Falta la consecuencia '{clave}'.");
            foreach (var kv in def.Consecuencias)
                if (string.IsNullOrEmpty(kv.Value.Rubrica?.Veredicto))
                    e.Add($"La consecuencia '{kv.Key}' no trae rubrica. Sin rubrica no hay Dashboard.");
            return e;
        }

        /// <summary>
        /// V1 · marcar sobre un lienzo. Lo marcable son los commits del grafo, los elementos de un diagrama y
        /// las conexiones entre ellos: las zonas y los senuelos los nombran a todos por id.
        /// </summary>
        private static void ValidarDetectar(MinijuegoDef def, List<string> e)
        {
            if (def.Zonas.Count == 0) e.Add("Un V1 sin zonas no tiene nada que detectar.");

            var commits = new HashSet<string>(def.Artefacto.Commits.Select(c => c.Id));
            if (commits.Count != def.Artefacto.Commits.Count) e.Add("Hay ids de commit repetidos.");
            var ramas = new HashSet<string>(def.Artefacto.Ramas.Select(r => r.Id));
            foreach (var c in def.Artefacto.Commits)
            {
                if (!ramas.Contains(c.Rama)) e.Add($"El commit '{c.Id}' apunta a la rama inexistente '{c.Rama}'.");
                foreach (var p in c.Padres)
                    if (!commits.Contains(p)) e.Add($"El commit '{c.Id}' tiene el padre inexistente '{p}'.");
                if (!string.IsNullOrEmpty(c.Diff) && !def.Artefacto.Diffs.ContainsKey(c.Diff))
                    e.Add($"El commit '{c.Id}' apunta al diff inexistente '{c.Diff}'.");
            }

            var elementos = new HashSet<string>();
            foreach (var el in def.Artefacto.Elementos)
                if (string.IsNullOrEmpty(el.Id) || !elementos.Add(el.Id)) e.Add($"Hay un elemento sin id o repetido ('{el.Id}').");
            foreach (var cx in def.Artefacto.Conexiones)
            {
                if (string.IsNullOrEmpty(cx.Id) || elementos.Contains(cx.Id) || commits.Contains(cx.Id))
                    e.Add($"La conexion '{cx.Id}' no tiene id o lo comparte con otra pieza.");
                if (!elementos.Contains(cx.Desde) || !elementos.Contains(cx.Hasta))
                    e.Add($"La conexion '{cx.Id}' une piezas que no existen ('{cx.Desde}' a '{cx.Hasta}').");
            }

            var marcables = new HashSet<string>(commits);
            marcables.UnionWith(elementos);
            marcables.UnionWith(def.Artefacto.Conexiones.Select(c => c.Id));
            if (marcables.Count == 0) e.Add("El lienzo no tiene nada que marcar: ni commits, ni elementos, ni conexiones.");

            foreach (var z in def.Zonas)
            {
                if (z.Commits.Count == 0) e.Add($"La zona '{z.Id}' no cubre ninguna pieza.");
                foreach (var cid in z.Commits)
                    if (!marcables.Contains(cid)) e.Add($"La zona '{z.Id}' apunta a la pieza inexistente '{cid}'.");
                if (!def.PaletaEtiquetas.Contains(z.Defecto))
                    e.Add($"La zona '{z.Id}' usa la etiqueta '{z.Defecto}', que no esta en paletaEtiquetas.");
                if (string.IsNullOrEmpty(z.Explicacion))
                    e.Add($"La zona '{z.Id}' no explica nada. Sin explicacion no hay leccion.");
            }
            foreach (var s in def.Senuelos)
            {
                foreach (var cid in s.Commits)
                    if (!marcables.Contains(cid)) e.Add($"El senuelo '{s.Id}' apunta a la pieza inexistente '{cid}'.");
                if (string.IsNullOrEmpty(s.RazonNoEsDefecto))
                    e.Add($"El senuelo '{s.Id}' no dice por que no es un defecto.");
                if (def.Zonas.Any(z => z.Commits.Intersect(s.Commits).Any()))
                    e.Add($"El senuelo '{s.Id}' se solapa con una zona real. Seria injusto.");
            }
        }

        /// <summary>V3 · ordenar con restricciones. Las dependencias tienen que existir y no pueden formar un ciclo.</summary>
        private static void ValidarOrdenar(MinijuegoDef def, List<string> e)
        {
            var o = def.Ordenar;
            if (o == null) { e.Add("Un V3 necesita el bloque 'ordenar'."); return; }
            if (o.Tarjetas.Count < 3) e.Add("Un backlog de menos de tres tarjetas no obliga a ordenar nada.");
            if (o.Capacidad <= 0) e.Add("'ordenar.capacidad' tiene que ser positiva.");
            if (o.Capacidad >= o.Tarjetas.Sum(t => t.Esfuerzo))
                e.Add("Todo el backlog cabe en la capacidad: no hay nada que dejar fuera, y ordenar no ensena nada.");

            var ids = new HashSet<string>();
            foreach (var t in o.Tarjetas)
            {
                if (string.IsNullOrEmpty(t.Id) || !ids.Add(t.Id)) e.Add($"Hay una tarjeta sin id o repetida ('{t.Id}').");
                if (string.IsNullOrEmpty(t.Titulo)) e.Add($"La tarjeta '{t.Id}' no tiene titulo.");
                if (t.Valor <= 0 || t.Esfuerzo <= 0) e.Add($"La tarjeta '{t.Id}' necesita valor y esfuerzo positivos.");
            }
            foreach (var t in o.Tarjetas)
                foreach (var d in t.DependeDe)
                    if (!ids.Contains(d)) e.Add($"La tarjeta '{t.Id}' depende de '{d}', que no existe.");
            if (Nexus.Core.Minijuegos.Ordenar.OrdenarEvaluador.HayCiclo(o.Tarjetas))
                e.Add("Las dependencias forman un ciclo: no existe ningun orden valido.");

            foreach (var clave in new[] { "obedecer", "rechazar", "negociar" })
                if (!o.Respuestas.ContainsKey(clave))
                    e.Add($"Falta la respuesta '{clave}' al cliente. Las tres opciones tienen que existir desde el principio.");
        }

        /// <summary>V2 · repartir un presupuesto escaso. Tiene que ser escaso de verdad.</summary>
        private static void ValidarRepartir(MinijuegoDef def, List<string> e)
        {
            var r = def.Repartir;
            if (r == null) { e.Add("Un V2 necesita el bloque 'repartir'."); return; }
            if (r.Depositos.Count < 2) e.Add("Con menos de dos depositos no hay nada que repartir.");
            if (r.Presupuesto <= 0) e.Add("'repartir.presupuesto' tiene que ser positivo.");

            var ids = new HashSet<string>();
            foreach (var d in r.Depositos)
            {
                if (string.IsNullOrEmpty(d.Id) || !ids.Add(d.Id)) e.Add($"Hay un deposito sin id o repetido ('{d.Id}').");
                if (d.CostePorDefecto <= 0) e.Add($"El deposito '{d.Id}' necesita un coste por defecto positivo.");
                if (d.DefectosOcultos < 0) e.Add($"El deposito '{d.Id}' no puede tener defectos negativos.");
            }
            var necesario = r.Depositos.Sum(d => d.CostePorDefecto * d.DefectosOcultos);
            if (r.Presupuesto >= necesario)
                e.Add("El presupuesto alcanza para encontrarlo todo: repartir no obligaria a renunciar a nada.");
        }
    }

    public sealed class MinijuegoInvalidoException : System.Exception
    {
        public MinijuegoInvalidoException(string id, List<string> errores)
            : base($"Minijuego '{id}' invalido:\n - " + string.Join("\n - ", errores)) { }
    }
}
