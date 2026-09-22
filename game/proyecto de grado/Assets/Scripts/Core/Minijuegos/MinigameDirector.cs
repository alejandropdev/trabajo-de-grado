using System;
using System.Collections.Generic;
using Nexus.Core.Eventos;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;

namespace Nexus.Core.Minijuegos {
    /// <summary>
    /// C6 · La ventana de las 15:00 (§4.6, §7.5). Decide si hoy toca un minijuego y cual.
    ///
    /// Es deliberadamente mas simple que el director de eventos: no telegrafia, no consume drama y no
    /// agenda nada para el futuro. Un minijuego es una interrupcion del dia, no una consecuencia.
    ///
    /// La regla de ritmo es contenido pedagogico, no una limitacion tecnica: **maximo uno al dia,
    /// nunca dos dias seguidos, y el 45 % de los dias elegibles no sale ninguno**. En doce dias deben
    /// salir tres o cuatro. Si saliera uno cada dia dejaria de ser una interrupcion y pasaria a ser
    /// la mecanica principal, y el juego no va de eso: va de gestionar.
    /// </summary>
    public sealed class MinigameDirector {
        /// <summary>Marca que HOY ya hubo minijuego, se jugara o se omita.</summary>
        public const string EnfriamientoGlobal = "MJ:cualquiera";

        public const string PrefijoEnfriamiento = "MJ:";

        /// <summary>El 45 % de los dias elegibles no sale ninguno (§7.5, paso 4).</summary>
        public const double ProbabilidadDeDiaTranquilo = 0.45;

        /// <summary>Nunca dos dias seguidos.</summary>
        public const int DiasEntreMinijuegos = 2;

        private readonly Dictionary<string, MinigameDefinition> _catalogo =
            new Dictionary<string, MinigameDefinition>(StringComparer.Ordinal);
        private readonly List<MinigameDefinition> _orden = new List<MinigameDefinition>();

        private readonly LevelProfile _perfil;
        private readonly DeterministicRng _rng;

        public List<DecisionDeMinijuego> Log = new List<DecisionDeMinijuego>();

        public MinigameDirector(List<MinigameDefinition> catalogo, LevelProfile perfil, DeterministicRng rng) {
            if (perfil == null) throw new ArgumentNullException(nameof(perfil));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            _perfil = perfil;
            _rng = rng;

            if (catalogo == null) return;
            foreach (var def in catalogo) {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                if (_catalogo.ContainsKey(def.Id))
                    throw new InvalidOperationException($"El indice de minijuegos tiene dos entradas con id '{def.Id}'.");
                _catalogo[def.Id] = def;
                _orden.Add(def);   // el orden del indice es el orden de la ruleta: de el depende el determinismo
            }
        }

        public IReadOnlyList<MinigameDefinition> Catalogo { get { return _orden; } }

        public MinigameDefinition PorId(string id) {
            MinigameDefinition def;
            return id != null && _catalogo.TryGetValue(id, out def) ? def : null;
        }

        /// <summary>
        /// El minijuego de hoy, o null. Sigue el algoritmo del §7.5 en su orden exacto, y el orden importa
        /// porque decide CUANDO se gasta una tirada del azar: la regla de ritmo y el filtrado no gastan
        /// ninguna, asi que añadir un minijuego al indice no descoloca las partidas ya guardadas.
        /// </summary>
        public PendingMinigame MinijuegoDeHoy(RuntimeState r, IStateContext ctx) {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var decision = new DecisionDeMinijuego { Dia = r.DiaActual };
            Log.Add(decision);

            // 1 · ritmo: nunca dos dias seguidos
            int ultimoDia;
            if (r.Enfriamientos.TryGetValue(EnfriamientoGlobal, out ultimoDia) &&
                r.DiaActual - ultimoDia < DiasEntreMinijuegos) {
                decision.Resultado = DecisionDeMinijuego.Ritmo;
                return null;
            }

            // 2 y 3 · filtrar y pesar
            var candidatos = new List<MinigameDefinition>();
            var pesos = new List<double>();

            foreach (var def in _orden) {
                if (!EsCandidato(def, r, ctx)) continue;
                if (def.PesoBase <= 0) continue;
                candidatos.Add(def);
                pesos.Add(def.PesoBase);
            }

            decision.Candidatos = candidatos.Count;
            if (candidatos.Count == 0) {
                decision.Resultado = DecisionDeMinijuego.SinCandidatos;
                return null;
            }

            // 4 · el 45 % de los dias elegibles no sale ninguno.
            // La tirada se gasta AQUI, despues de saber que habia candidatos: si se tirara antes,
            // un nivel sin minijuegos elegibles consumiria azar y las partidas dejarian de cuadrar.
            // La tirada se gasta siempre, aunque la probabilidad sea 0: si no, cambiar este numero en un
            // nivel desalinearia todo lo que el azar sortea despues (INV-7 con la misma semilla).
            var tranquilo = _perfil.Director == null ? ProbabilidadDeDiaTranquilo : _perfil.Director.ProbabilidadDeDiaTranquiloMinijuegos;
            if (_rng.NextDouble() < tranquilo) {
                decision.Resultado = DecisionDeMinijuego.DiaTranquilo;
                return null;
            }

            // 5 · ruleta
            var elegido = _rng.RuletaPonderada(pesos);
            if (elegido < 0) {
                decision.Resultado = DecisionDeMinijuego.SinCandidatos;
                return null;
            }

            var minijuego = candidatos[elegido];

            // 6 · hoy ya hubo minijuego, se juegue o se omita
            r.Enfriamientos[EnfriamientoGlobal] = r.DiaActual;
            r.MinijuegosJugados++;

            decision.Resultado = DecisionDeMinijuego.Propuesto;
            decision.MinijuegoId = minijuego.Id;

            return new PendingMinigame {
                MinijuegoId = minijuego.Id,
                Verbo = minijuego.Verbo,
                Archivo = minijuego.Archivo,
                PresionDiegetica = minijuego.PresionDiegetica,
                Segundos = minijuego.Reloj,
                NivelAndamiaje = _perfil.NivelAndamiaje,
                ObjetivoAprendizaje = minijuego.ObjetivoAprendizaje
            };
        }

        /// <summary>
        /// Se llama al resolver el minijuego, incluso si se omitio: omitir es una decision y cuenta
        /// como ocurrencia. Arranca el enfriamiento propio.
        /// </summary>
        public void RegistrarJugado(string minijuegoId, RuntimeState r) {
            if (string.IsNullOrEmpty(minijuegoId)) throw new ArgumentException("Falta el id.", nameof(minijuegoId));

            int veces;
            r.Ocurrencias.TryGetValue(minijuegoId, out veces);
            r.Ocurrencias[minijuegoId] = veces + 1;

            r.Enfriamientos[PrefijoEnfriamiento + minijuegoId] = r.DiaActual;
        }

        // ------------------------------------------------------------------ filtros

        private bool EsCandidato(MinigameDefinition def, RuntimeState r, IStateContext ctx) {
            if (!SaleEnFase(def, r.Fase)) return false;
            if (!EstaEntreLosObjetivosDelNivel(def)) return false;

            int veces;
            if (def.MaxOcurrencias > 0 && r.Ocurrencias.TryGetValue(def.Id, out veces) && veces >= def.MaxOcurrencias)
                return false;

            int ultimaVez;
            if (r.Enfriamientos.TryGetValue(PrefijoEnfriamiento + def.Id, out ultimaVez) &&
                r.DiaActual - ultimaVez < def.Enfriamiento)
                return false;

            return ConditionEvaluator.EvaluarTodas(def.Precondiciones, ctx);
        }

        /// <summary>Un indice sin 'fases' solo sale en desarrollo, que es donde vive la ventana de las 15:00.</summary>
        private static bool SaleEnFase(MinigameDefinition def, int fase) {
            if (def.Fases == null || def.Fases.Count == 0) return fase == 2;

            var nombre = FasesDelNivel.De(fase);
            foreach (var f in def.Fases)
                if (string.Equals(f, nombre, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Un nivel evalua unos objetivos concretos. Un minijuego de Git en un nivel que no enseña Git
        /// seria ruido, por bien hecho que este.
        /// </summary>
        private bool EstaEntreLosObjetivosDelNivel(MinigameDefinition def) {
            if (_perfil.ObjetivosActivos == null || _perfil.ObjetivosActivos.Length == 0) return true;
            if (string.IsNullOrEmpty(def.ObjetivoAprendizaje)) return false;

            foreach (var oa in _perfil.ObjetivosActivos)
                if (string.Equals(oa, def.ObjetivoAprendizaje, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
