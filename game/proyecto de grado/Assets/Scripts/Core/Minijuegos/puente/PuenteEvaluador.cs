using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Puente {
    /// <summary>Como sale una historia al verificar su pilar.</summary>
    public enum EstadoDeBloque {
        /// <summary>Todo lo que necesita esta mas abajo: se ilumina en verde.</summary>
        Bien,
        /// <summary>Le falta algo que necesita por debajo: es la primera que falla y se pone roja.</summary>
        Mal,
        /// <summary>Esta por encima del fallo: la luz no llego hasta ella, este bien o mal.</summary>
        SinRevisar
    }

    /// <summary>Lo que dice la luz al subir por un pilar. Los puestos cuentan desde ABAJO (0 = el primero).</summary>
    public sealed class RevisionDePilar {
        public string Pilar;
        public List<string> Orden = new List<string>();
        public EstadoDeBloque[] Estados = new EstadoDeBloque[0];
        /// <summary>Puesto de la primera historia mal puesta; -1 si el pilar esta bien.</summary>
        public int PrimerFallo = -1;
        public string HistoriaQueFalla;
        /// <summary>La historia que necesita y que no esta mas abajo.</summary>
        public string LaQueFalta;
        /// <summary>Una frase para el jugador: «“X” necesita “Y” antes…».</summary>
        public string Motivo;
        public bool Correcto { get { return PrimerFallo < 0; } }
    }

    /// <summary>Lo que paso en una pulsacion de «Verificar»: una revision por cada pilar que se reviso.</summary>
    public sealed class RondaDeRevision {
        public List<int> Pilares = new List<int>();
        public List<RevisionDePilar> Revisiones = new List<RevisionDePilar>();
        /// <summary>Los pilares que acaban de quedarse sin vidas (el puente se pierde).</summary>
        public List<int> Caidos = new List<int>();
        public bool SePerdio { get { return Caidos.Count > 0; } }
        public bool Completo;
    }

    /// <summary>
    /// El puente del backlog (2026-11-10). Reglas puras, sin Unity:
    ///   · una historia esta bien si TODAS las que necesita estan mas abajo en su pilar;
    ///   · verificar ilumina cada pilar de abajo arriba y se detiene en la primera que falla (las de arriba quedan sin
    ///     revisar aunque esten bien);
    ///   · un pilar bien se da por hecho y se bloquea; un pilar mal pierde una vida; sin vidas, el puente se pierde.
    ///
    ///   todos          puente completo sin perder ninguna vida
    ///   parcial        puente completo habiendo perdido vidas, o se acabo el tiempo con algun pilar hecho
    ///   falsoPositivo  un pilar se quedo sin vidas
    ///   omitido        se acabo el tiempo sin ningun pilar hecho
    /// </summary>
    public static class PuenteEvaluador {
        // ------------------------------------------------------------ un pilar

        public static RevisionDePilar RevisarPilar(PilarDef pilar, IList<string> ordenDeAbajoArriba) {
            if (pilar == null) throw new ArgumentNullException(nameof(pilar));
            if (ordenDeAbajoArriba == null) throw new ArgumentNullException(nameof(ordenDeAbajoArriba));
            var porId = pilar.Historias.ToDictionary(h => h.Id);
            if (ordenDeAbajoArriba.Count != porId.Count || ordenDeAbajoArriba.Any(id => !porId.ContainsKey(id)) || ordenDeAbajoArriba.Distinct().Count() != porId.Count)
                throw new InvalidOperationException("El orden del pilar '" + pilar.Id + "' no es una permutacion de sus historias.");

            var r = new RevisionDePilar { Pilar = pilar.Id, Orden = ordenDeAbajoArriba.ToList(), Estados = new EstadoDeBloque[ordenDeAbajoArriba.Count] };
            var puesto = new Dictionary<string, int>();
            for (var i = 0; i < ordenDeAbajoArriba.Count; i++) puesto[ordenDeAbajoArriba[i]] = i;

            for (var i = 0; i < ordenDeAbajoArriba.Count; i++) {
                if (r.PrimerFallo >= 0) { r.Estados[i] = EstadoDeBloque.SinRevisar; continue; }
                var h = porId[ordenDeAbajoArriba[i]];
                var falta = h.DependeDe.FirstOrDefault(d => puesto.ContainsKey(d) && puesto[d] > i);
                if (falta == null) { r.Estados[i] = EstadoDeBloque.Bien; continue; }
                r.Estados[i] = EstadoDeBloque.Mal;
                r.PrimerFallo = i;
                r.HistoriaQueFalla = h.Id;
                r.LaQueFalta = falta;
                var necesita = porId[falta];
                r.Motivo = "«" + h.Titulo + "» necesita «" + necesita.Titulo + "» antes: esa tiene que estar más abajo." +
                           (string.IsNullOrEmpty(h.Porque) ? "" : " " + h.Porque);
            }
            return r;
        }

        /// <summary>Un orden valido (de abajo arriba): primero las que no necesitan nada. Estable respecto al orden del JSON.</summary>
        public static List<string> Solucion(PilarDef pilar) {
            var resultado = new List<string>();
            var pendientes = pilar.Historias.ToList();
            var puestas = new HashSet<string>();
            while (pendientes.Count > 0) {
                var siguiente = pendientes.FirstOrDefault(h => h.DependeDe.All(puestas.Contains));
                if (siguiente == null) throw new InvalidOperationException("El pilar '" + pilar.Id + "' tiene un ciclo de dependencias.");
                resultado.Add(siguiente.Id);
                puestas.Add(siguiente.Id);
                pendientes.Remove(siguiente);
            }
            return resultado;
        }

        public static bool HayCiclo(PilarDef pilar) {
            try { Solucion(pilar); return false; } catch (InvalidOperationException) { return true; }
        }

        /// <summary>El tablero tal como lo encuentra el jugador: barajado de forma estable (no depende del azar) y nunca resuelto.</summary>
        public static List<string> OrdenInicial(PilarDef pilar, int semilla) {
            var orden = pilar.Historias.Select(h => h.Id).OrderBy(id => Hash(id + "|" + semilla)).ThenBy(id => id, StringComparer.Ordinal).ToList();
            if (RevisarPilar(pilar, orden).Correcto) {
                // Si la baraja cayo resuelta, se le da la vuelta a la solucion: con una dependencia, no vale a la vez.
                orden = Solucion(pilar);
                orden.Reverse();
            }
            return orden;
        }

        private static uint Hash(string s) {
            unchecked {
                var h = 2166136261u;
                foreach (var c in s) { h ^= c; h *= 16777619u; }
                return h;
            }
        }

        // ------------------------------------------------------------ resultado

        public static ResultadoMinijuego Evaluar(MinijuegoDef def, EstadoDelPuente estado) {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (estado == null) throw new ArgumentNullException(nameof(estado));
            string clave;
            if (estado.Perdido) clave = ResultadosDeMinijuego.FalsoPositivo;
            else if (estado.Completo) clave = estado.VidasPerdidas == 0 ? ResultadosDeMinijuego.Todos : ResultadosDeMinijuego.Parcial;
            else clave = estado.Hecho.Any(h => h) ? ResultadosDeMinijuego.Parcial : ResultadosDeMinijuego.Omitido;

            Consecuencia c;
            if (!def.Consecuencias.TryGetValue(clave, out c)) c = new Consecuencia();
            return new ResultadoMinijuego {
                MinijuegoId = def.Id,
                Resultado = clave,
                Rubrica = c.Rubrica,
                EfectosInmediatos = new Dictionary<string, float>(c.EfectosInmediatos),
                EfectosDiferidos = c.EfectosDiferidos.ToList(),
                Hallazgos = c.Hallazgos.ToList(),
                Detalle = estado.Detalle(),
                TextoCierre = def.Cierre == null ? null : def.Cierre.Texto
            };
        }
    }

    /// <summary>
    /// El estado de una partida del puente: el orden de cada pilar (de abajo arriba), sus vidas y cuales estan hechos.
    /// </summary>
    public sealed class EstadoDelPuente {
        public readonly PuenteCfg Cfg;
        public readonly List<List<string>> Ordenes = new List<List<string>>();
        public readonly int[] Vidas;
        public readonly bool[] Hecho;
        public readonly bool[] Caido;
        public readonly RevisionDePilar[] UltimaRevision;
        public int Verificaciones { get; private set; }

        public EstadoDelPuente(PuenteCfg cfg, int semilla = 0) {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            Cfg = cfg;
            Vidas = cfg.Pilares.Select(_ => Math.Max(1, cfg.Vidas)).ToArray();
            Hecho = new bool[cfg.Pilares.Count];
            Caido = new bool[cfg.Pilares.Count];
            UltimaRevision = new RevisionDePilar[cfg.Pilares.Count];
            foreach (var p in cfg.Pilares) Ordenes.Add(PuenteEvaluador.OrdenInicial(p, semilla));
        }

        public int VidasIniciales { get { return Math.Max(1, Cfg.Vidas); } }
        public bool Perdido { get { return Caido.Any(c => c); } }
        public bool Completo { get { return Hecho.Length > 0 && Hecho.All(h => h); } }
        public int VidasPerdidas { get { return Vidas.Sum(v => VidasIniciales - v); } }
        public bool SePuedeMover(int pilar) { return !Perdido && !Hecho[pilar] && !Caido[pilar]; }

        /// <summary>Lleva la historia al puesto 'puesto' de su pilar (0 = abajo del todo). false si no se puede.</summary>
        public bool Mover(int pilar, string historia, int puesto) {
            if (!SePuedeMover(pilar)) return false;
            var orden = Ordenes[pilar];
            if (!orden.Contains(historia)) return false;
            puesto = Math.Max(0, Math.Min(orden.Count - 1, puesto));
            orden.Remove(historia);
            orden.Insert(puesto, historia);
            return true;
        }

        public int PilarDe(string historia) {
            for (var i = 0; i < Cfg.Pilares.Count; i++) if (Cfg.Pilares[i].Historias.Any(h => h.Id == historia)) return i;
            return -1;
        }

        /// <summary>
        /// «Verificar»: revisa todos los pilares que aun no estan hechos. Los bien puestos se dan por hechos y se
        /// bloquean; los mal puestos pierden una vida y, sin vidas, el puente se pierde.
        /// </summary>
        public RondaDeRevision Verificar() {
            var ronda = new RondaDeRevision();
            if (Perdido || Completo) { ronda.Completo = Completo; return ronda; }
            Verificaciones++;
            for (var i = 0; i < Cfg.Pilares.Count; i++) {
                if (Hecho[i]) continue;
                var rev = PuenteEvaluador.RevisarPilar(Cfg.Pilares[i], Ordenes[i]);
                UltimaRevision[i] = rev;
                ronda.Pilares.Add(i);
                ronda.Revisiones.Add(rev);
                if (rev.Correcto) { Hecho[i] = true; continue; }
                Vidas[i]--;
                if (Vidas[i] <= 0) { Caido[i] = true; ronda.Caidos.Add(i); }
            }
            ronda.Completo = Completo;
            return ronda;
        }

        /// <summary>Una linea por pilar para el cierre: como le fue y, si fallo, por que.</summary>
        public List<string> Detalle() {
            var lineas = new List<string>();
            for (var i = 0; i < Cfg.Pilares.Count; i++) {
                var nombre = "«" + Cfg.Pilares[i].Nombre + "»";
                var errores = VidasIniciales - Vidas[i];
                var ult = UltimaRevision[i];
                if (Caido[i]) lineas.Add(nombre + ": se cayó. " + (ult != null ? ult.Motivo : ""));
                else if (Hecho[i]) lineas.Add(nombre + (errores == 0 ? ": bien a la primera." : ": bien, tras " + errores + (errores == 1 ? " error." : " errores.")));
                else lineas.Add(nombre + ": sin terminar." + (ult != null && !ult.Correcto ? " " + ult.Motivo : ""));
            }
            return lineas;
        }
    }

    /// <summary>Cuando un reto es un puente (el lienzo "puente" con su bloque).</summary>
    public static class PuenteDelBacklog {
        public const string Lienzo = "puente";
        public static bool Aplica(MinijuegoDef def) {
            return def != null && def.Lienzo == Lienzo && def.Puente != null;
        }
    }
}
