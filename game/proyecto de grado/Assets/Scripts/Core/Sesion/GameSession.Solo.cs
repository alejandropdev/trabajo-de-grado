using System;
using System.Collections.Generic;
using Nexus.Core.Servicios;

namespace Nexus.Core.Sesion {
    /// <summary>
    /// Trabajar SOLO: el jugador no recogio ninguna hoja de vida en el recorrido y el equipo es «Tú». Lo que es de grupo
    /// (hablar uno a uno, dar un respiro a alguien, ponerlo en pareja) no tiene sentido; lo que si lo tiene es lo que
    /// hace quien trabaja solo para no hundirse: ordenar sus ideas, parar a tiempo y explicarle el codigo a un pato.
    /// Las tres se pueden una vez al dia, cuestan minutos de la jornada y van por EffectApplier (INV-2).
    /// </summary>
    public sealed partial class GameSession {
        public const int MinutosDeOrdenarIdeas = 15, MinutosDePausaActiva = 10, MinutosDePatoDeGoma = 10;

        public const string AccionOrdenarIdeas = "ordenar-ideas", AccionPausaActiva = "pausa-activa", AccionPatoDeGoma = "pato-de-goma";

        /// <summary>No hay nadie mas en el equipo del proyecto: solo «Tú».</summary>
        public bool TrabajasSolo {
            get {
                var equipo = EquipoDelProyecto();
                return equipo.Count == 1 && equipo[0].Id == Nexus.Core.Fase1.CatalogoDeObjetos3D.IdDeTuSolo;
            }
        }

        public string PorQueNoSePuedeHacerSolo(string accion) {
            if (!TrabajasSolo) return "Tienes equipo: esto es para quien trabaja solo.";
            int minutos;
            switch (accion) {
                case AccionOrdenarIdeas: minutos = MinutosDeOrdenarIdeas; break;
                case AccionPausaActiva: minutos = MinutosDePausaActiva; break;
                case AccionPatoDeGoma: minutos = MinutosDePatoDeGoma; break;
                default: return "Esa acción no existe.";
            }
            var motivo = PorQueNoSePuedeActuar(minutos);
            if (motivo != null) return motivo;
            return R.AccionesSoloHoy.Contains(accion) ? "Hoy ya lo hiciste." : null;
        }

        /// <summary>Escribir en tu bitacora que vas a hacer y por que: algo de documentacion y la cabeza mas clara.</summary>
        public ResultadoDeAvance OrdenarTusIdeas() {
            Hecho(AccionOrdenarIdeas);
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { "Documentacion", 1.0 }, { "MoralEquipo", 0.5 } });
            return AvanzarReloj(MinutosDeOrdenarIdeas);
        }

        /// <summary>Levantarse diez minutos: baja tu cansancio, pero hoy avanzas algo menos (no hay nadie que siga).</summary>
        public ResultadoDeAvance PausaActiva() {
            Hecho(AccionPausaActiva);
            var t = R.Tablero;
            var tu = t.Miembros[0];
            tu.DesvioDeCansancio = Math.Max(-30, tu.DesvioDeCansancio - 8);
            t.BonoDeFlujoHoy -= 0.10;
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { "Cansancio", -1.5 } });
            return AvanzarReloj(MinutosDePausaActiva);
        }

        /// <summary>
        /// Explicarle el codigo, linea a linea, a un pato de goma: al decirlo en voz alta aparecen los fallos. Lo mismo
        /// que daria una pareja, en pequeño: algo mas de cobertura, algo menos de deuda, y hoy se avanza algo menos.
        /// </summary>
        public ResultadoDeAvance PatoDeGoma() {
            Hecho(AccionPatoDeGoma);
            R.Tablero.BonoDeFlujoHoy -= 0.08;
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { "Cobertura", 1.5 }, { "DeudaTecnica", -1.0 } });
            return AvanzarReloj(MinutosDePatoDeGoma);
        }

        /// <summary>Comprueba que se puede y la apunta como hecha hoy (se vacia al empezar el dia).</summary>
        private void Hecho(string accion) {
            Exigir(PorQueNoSePuedeHacerSolo(accion));
            R.AccionesSoloHoy.Add(accion);
        }
    }
}
