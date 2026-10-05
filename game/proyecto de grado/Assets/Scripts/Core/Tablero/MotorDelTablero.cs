using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Tablero {
    /// <summary>
    /// Lo que hace el tablero con el avance. Todo determinista y sin azar: son sumas, repartos y un orden.
    ///
    ///   Conciliar          lleva el tablero a un avance dado (la unica forma de mover el trabajo de las tarjetas)
    ///   AvanzarHasta       el equipo trabaja unos minutos: crece lo provisional del dia
    ///   CerrarElDia        el dia termina y lo provisional se sustituye por el avance de verdad
    ///   FactorDelDia       lo que el jugador le saca (o le quita) al ritmo del equipo con como asigna
    ///
    /// Con el equipo en piloto automatico el factor del dia vale EXACTAMENTE 1: el tablero no cambia el avance, solo
    /// lo muestra. Es lo que deja intactas las partidas ya calibradas.
    /// </summary>
    public static class MotorDelTablero {
        public const double Epsilon = 1e-6;
        public const double FactorMinimo = 0.80, FactorMaximo = 1.15;

        /// <summary>Cuantas tarjetas se llevan a la vez: el limite de WIP si lo hay; si no, tantas como compañeros.</summary>
        public static int EnCurso(TableroDelEquipo t) { return Math.Max(1, t.LimiteEnCurso > 0 ? t.LimiteEnCurso : t.Miembros.Count); }

        /// <summary>
        /// Lo que le hace al ritmo mover el limite de trabajo en curso desde donde estaba. Bajarlo UN paso enfoca al
        /// equipo (+2 %): menos cosas a medias, menos cambios de contexto. Bajarlo mas deja gente parada, y subirlo
        /// reparte la atencion: −4 % por cada paso de mas. Sin tocarlo, 1: el piloto automatico sigue siendo neutro.
        /// </summary>
        public static double FactorDelWip(TableroDelEquipo t) {
            if (t.LimiteEnCurso <= 0 || t.LimiteNeutro <= 0) return 1.0;
            var pasos = t.LimiteEnCurso - t.LimiteNeutro;
            if (pasos == 0) return 1.0;
            if (pasos > 0) return 1.0 - 0.04 * pasos;
            return pasos == -1 ? 1.02 : 1.02 - 0.04 * (-pasos - 1);
        }

        /// <summary>El orden en que se trabaja: lo del sprint en curso primero; despues, el orden de construccion.</summary>
        private static IEnumerable<TarjetaDeTrabajo> EnOrdenDeTrabajo(TableroDelEquipo t) {
            return t.Tarjetas.Where(x => !x.EsBug && !x.Terminada)
                    .OrderBy(x => t.SprintActual >= 0 && x.Sprint == t.SprintActual ? 0 : 1).ThenBy(x => x.Orden);
        }

        // ==================================================================== el alcance

        /// <summary>
        /// El alcance puede cambiar a mitad de nivel (un cambio pedido que se acepta): sobran tarjetas nuevas, o se
        /// quitan las que aun no se han empezado. Solo toca lo que no se empezo.
        /// </summary>
        public static void SincronizarAlcance(TableroDelEquipo t, double alcance, int semilla, int dia) {
            var actual = t.PuntosDeAlcance;
            if (alcance > actual + 0.005) {
                GeneradorDeTarjetas.Cortar(t, "cambio", "Cambio pedido", alcance - actual, semilla, false, dia);
                // «Cambio pedido» no es un modulo: la tarjeta lo dice y no cuelga de ninguno.
                foreach (var c in t.Tarjetas.Where(x => x.Modulo == "cambio")) c.Modulo = null;
            } else if (alcance < actual - 0.005) {
                var sobra = actual - alcance;
                foreach (var c in t.Tarjetas.Where(x => !x.EsBug && !x.Empezada).OrderByDescending(x => x.Orden).ToList()) {
                    if (sobra <= 0.005) break;
                    if (c.Puntos <= sobra + 0.005) { sobra -= c.Puntos; t.Tarjetas.Remove(c); }
                    else { c.Puntos = Math.Round(c.Puntos - sobra, 2); sobra = 0; }
                }
            }
        }

        // ==================================================================== el avance

        /// <summary>
        /// Lleva el trabajo de las tarjetas a 'objetivo' (W.Avance + lo provisional de hoy). Idempotente: llamarlo dos
        /// veces seguidas con lo mismo no cambia nada. Sube repartiendo entre las tarjetas en curso y baja quitando de
        /// lo que esta a medias; lo terminado no se deshace, se queda como TrabajoAdeudado.
        /// </summary>
        public static void Conciliar(TableroDelEquipo t, double objetivo, int dia) {
            var meta = Math.Max(0, Math.Min(objetivo, t.PuntosDeAlcance));
            var delta = meta - (t.TrabajoDeAlcance - t.TrabajoAdeudado);
            if (delta > Epsilon) Subir(t, delta);
            else if (delta < -Epsilon) Bajar(t, -delta);
            ActualizarColumnas(t, dia);
        }

        private static void Subir(TableroDelEquipo t, double delta) {
            // Lo que se debia se paga primero: es trabajo que hay que volver a hacer antes de avanzar de verdad.
            var paga = Math.Min(delta, t.TrabajoAdeudado);
            t.TrabajoAdeudado -= paga;
            delta -= paga;

            var guarda = 0;
            while (delta > Epsilon && guarda++ < 1000) {
                var activas = EnOrdenDeTrabajo(t).Take(EnCurso(t)).ToList();
                if (activas.Count == 0) break;
                var parte = delta / activas.Count;
                var dado = 0.0;
                foreach (var c in activas) {
                    var d = Math.Min(parte, c.Restante);
                    c.Trabajo = Math.Round(c.Trabajo + d, 6);
                    dado += d;
                }
                if (dado <= Epsilon) break;
                delta -= dado;
            }
        }

        private static void Bajar(TableroDelEquipo t, double quitar) {
            // De lo que esta a medias, empezando por lo ultimo que se empezo. Lo terminado no se toca.
            foreach (var c in t.Tarjetas.Where(x => !x.EsBug && x.Empezada && !x.Terminada).OrderByDescending(x => x.Orden)) {
                if (quitar <= Epsilon) break;
                var d = Math.Min(c.Trabajo, quitar);
                c.Trabajo = Math.Round(c.Trabajo - d, 6);
                quitar -= d;
            }
            if (quitar > Epsilon) t.TrabajoAdeudado += quitar;
        }

        /// <summary>
        /// El equipo trabaja 'minutos' a la velocidad de hoy: crece lo provisional. 'minutosDeJornada' es lo que dura el
        /// dia entero, de modo que a lo largo de un dia completo se suma la velocidad de un dia.
        /// </summary>
        public static void AvanzarHasta(TableroDelEquipo t, int minutos, double velocidadPorDia, int minutosDeJornada) {
            if (minutos <= 0 || minutosDeJornada <= 0) return;
            t.ProvisionalHoy += velocidadPorDia * minutos / minutosDeJornada;
            t.SumaFactorMinutos += FactorInstantaneo(t) * minutos;
            t.MinutosDeHoy += minutos;
        }

        /// <summary>El dia termino: lo provisional deja de existir y el tablero pasa a ser lo que dice el motor.</summary>
        public static void CerrarElDia(TableroDelEquipo t, double avanceOficial, int dia) {
            t.ProvisionalHoy = 0;
            t.SumaFactorMinutos = 0;
            t.MinutosDeHoy = 0;
            t.BonoDeFlujoHoy = 0;
            Conciliar(t, avanceOficial, dia);
        }

        // ==================================================================== el factor del jugador

        /// <summary>
        /// Lo que vale ahora asignar como esta asignado. Piloto automatico = 1. Si el jugador puso a alguien en una
        /// tarjeta, cuenta su habilidad para ese tipo de trabajo (0,7 flojo ... 1,3 su fuerte), solo en esas tarjetas.
        /// </summary>
        public static double FactorInstantaneo(TableroDelEquipo t) {
            var manuales = t.Tarjetas.Where(x => !x.EsBug && x.AsignacionManual && x.Empezada && !x.Terminada).ToList();
            if (manuales.Count == 0) return Math.Max(FactorMinimo, Math.Min(FactorMaximo, FactorDelWip(t)));
            var enCurso = t.Tarjetas.Count(x => !x.EsBug && x.Empezada && !x.Terminada);
            var suma = 0.0;
            foreach (var c in manuales) {
                var m = t.Miembro(c.Asignado);
                suma += m == null ? 1.0 : m.HabilidadPara(c.Tipo);
            }
            // Las que reparte el piloto cuentan como 1.
            var media = (suma + Math.Max(0, enCurso - manuales.Count)) / Math.Max(1, enCurso);
            return Math.Max(FactorMinimo, Math.Min(FactorMaximo, media * FactorDelWip(t)));
        }

        /// <summary>La media del dia del factor instantaneo, por minutos. 1 si no se llego a medir nada.</summary>
        public static double FactorDelDia(TableroDelEquipo t) {
            if (t == null) return 1.0;
            var medio = t.MinutosDeHoy <= 0 ? 1.0 : t.SumaFactorMinutos / t.MinutosDeHoy;
            return Math.Max(FactorMinimo, Math.Min(FactorMaximo, medio + t.BonoDeFlujoHoy));
        }

        // ==================================================================== lo que se ve

        /// <summary>
        /// Pone cada tarjeta en su columna segun lo hecho, anota cuando se empezo y se termino, y reparte entre los
        /// compañeros las que nadie lleva (piloto automatico, por turnos y en orden: determinista).
        /// </summary>
        public static void ActualizarColumnas(TableroDelEquipo t, int dia) {
            foreach (var c in t.Tarjetas) {
                if (c.EsBug && c.Columna != ColumnasDeBase.PorHacer && c.Columna != ColumnasDeBase.Haciendo && c.Columna != ColumnasDeBase.Hecho) continue;
                string columna;
                if (c.Terminada) columna = ColumnasDeBase.Hecho;
                else if (c.Empezada) columna = ColumnasDeBase.Haciendo;
                else columna = ColumnasDeBase.PorHacer;
                if (c.Columna == ColumnasDeBase.Revision && !c.Terminada && c.Empezada) columna = ColumnasDeBase.Revision;
                c.Columna = columna;
                if (c.Empezada && c.DiaInicio < 0) c.DiaInicio = dia;
                if (!c.Empezada) c.DiaInicio = -1;
                if (c.Terminada) { if (c.DiaFin < 0) c.DiaFin = dia; } else c.DiaFin = -1;
            }
            AutoAsignar(t);
        }

        /// <summary>A cada tarjeta a medias sin dueño le toca el compañero con menos tarjetas ahora (a igualdad, el primero).</summary>
        public static void AutoAsignar(TableroDelEquipo t) {
            if (t.Miembros.Count == 0) return;
            foreach (var c in t.Tarjetas.Where(x => x.Columna == ColumnasDeBase.Haciendo || x.Columna == ColumnasDeBase.Revision).OrderBy(x => x.Orden)) {
                if (c.AsignacionManual && t.Miembro(c.Asignado) != null) continue;
                if (c.Asignado != null && t.Miembro(c.Asignado) != null) continue;
                var carga = t.Miembros.Select(m => new {
                    m, n = t.Tarjetas.Count(x => x.Asignado == m.Id && x != c && x.Columna == ColumnasDeBase.Haciendo)
                }).OrderBy(x => x.n).ThenBy(x => t.Miembros.IndexOf(x.m)).First();
                c.Asignado = carga.m.Id;
                c.AsignacionManual = false;
            }
            // Las que ya no estan en curso se quedan sin dueño.
            foreach (var c in t.Tarjetas.Where(x => x.Columna == ColumnasDeBase.PorHacer && !x.AsignacionManual)) c.Asignado = null;
        }
    }
}
