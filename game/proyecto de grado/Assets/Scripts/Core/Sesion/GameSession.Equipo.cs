using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using Nexus.Core.Tablero;

namespace Nexus.Core.Sesion {
    /// <summary>Un compañero del equipo del proyecto, tal como esta hoy: que lleva, que termino y como va.</summary>
    public sealed class FichaDeMiembro {
        public MiembroDelEquipo Miembro;
        /// <summary>El tipo de trabajo que mejor se le da y el que peor («interfaz», «datos»…). null si le da igual.</summary>
        public string Fuerte, Flojo;
        public List<TarjetaDeTrabajo> Ahora = new List<TarjetaDeTrabajo>();
        public List<TarjetaDeTrabajo> Terminadas = new List<TarjetaDeTrabajo>();
        /// <summary>Su cansancio: el del equipo mas lo que el se desvia, de 0 a 100.</summary>
        public double Cansancio;
        /// <summary>Como va, en una frase: lo que diria si le preguntas.</summary>
        public string ComoVa;
    }

    /// <summary>Lo que te cuenta un compañero cuando te sientas con el un rato.</summary>
    public sealed class ResultadoDeUnoAUno {
        public string Quien;
        public string LoQueDice;
        public Dictionary<string, double> Cambios = new Dictionary<string, double>();
    }

    /// <summary>
    /// El equipo del proyecto: quienes son, que hace cada uno y lo que se puede hacer con cada uno durante la jornada
    /// (hablar uno a uno, darle un respiro, ponerlo en pareja; reasignarle trabajo es AsignarTarjeta).
    ///
    /// ★ Nada de esto mueve el motor si no se usa: sin acciones, el factor de flujo del dia sigue siendo el de
    /// siempre. Lo que se hace entra por el bono de flujo del dia (acotado en MotorDelTablero) y por efectos pequeños.
    /// </summary>
    public sealed partial class GameSession {
        public const int MinutosDeUnoAUno = 15, MinutosDeRespiro = 5, MinutosDePareja = 10;

        /// <summary>El equipo del proyecto. Antes de empezar el desarrollo, el del perfil del nivel.</summary>
        public List<MiembroDelEquipo> EquipoDelProyecto() {
            if (R.Tablero != null && R.Tablero.Miembros.Count > 0) return R.Tablero.Miembros;
            return Perfil.Equipo ?? new List<MiembroDelEquipo>();
        }

        /// <summary>Quien es (su descripcion y su frase vienen del nivel; el tablero guardado puede no traerlas).</summary>
        public MiembroDelEquipo DatosDeMiembro(string id) {
            return (Perfil.Equipo ?? new List<MiembroDelEquipo>()).FirstOrDefault(m => m.Id == id);
        }

        public List<FichaDeMiembro> FichasDelEquipo() {
            var fichas = new List<FichaDeMiembro>();
            var t = R.Tablero;
            foreach (var m in EquipoDelProyecto()) {
                var f = new FichaDeMiembro { Miembro = m, Cansancio = Math.Max(0, Math.Min(100, W.Cansancio + m.DesvioDeCansancio)) };
                if (m.Habilidades != null && m.Habilidades.Count > 0) {
                    var mejor = m.Habilidades.OrderByDescending(kv => kv.Value).First();
                    var peor = m.Habilidades.OrderBy(kv => kv.Value).First();
                    if (mejor.Value > 1.05) f.Fuerte = mejor.Key;
                    if (peor.Value < 0.95) f.Flojo = peor.Key;
                }
                if (t != null) {
                    f.Ahora = t.Tarjetas.Where(c => c.Asignado == m.Id && !c.Terminada && c.Empezada).OrderBy(c => c.Orden).ToList();
                    f.Terminadas = t.Tarjetas.Where(c => c.Asignado == m.Id && c.Terminada).OrderByDescending(c => c.DiaFin).ToList();
                }
                f.ComoVa = ComoVa(f);
                fichas.Add(f);
            }
            return fichas;
        }

        private static string ComoVa(FichaDeMiembro f) {
            var m = f.Miembro;
            if (m.DescansaHoy) return "Hoy tiene un respiro: no lleva tarjeta.";
            if (!string.IsNullOrEmpty(m.ParejaHoy)) return "Hoy trabaja en pareja.";
            if (f.Cansancio >= 70) return "Está quemado: lleva días tirando más de la cuenta.";
            var actual = f.Ahora.FirstOrDefault();
            if (actual == null) return "Ahora mismo no lleva ninguna tarjeta.";
            var habilidad = m.HabilidadPara(actual.Tipo);
            if (habilidad > 1.05) return $"Va cómodo con «{actual.Titulo}»: el trabajo de {actual.Tipo} es lo suyo.";
            if (habilidad < 0.95) return $"Le cuesta «{actual.Titulo}»: el trabajo de {actual.Tipo} no es lo suyo.";
            return $"Avanza con «{actual.Titulo}», sin problemas.";
        }

        private string PorQueNoConEl(string miembroId, int minutos) {
            var motivo = PorQueNoSePuedeActuar(minutos);
            if (motivo != null) return motivo;
            return R.Tablero.Miembro(miembroId) == null ? "Esa persona no está en el equipo." : null;
        }

        // ==================================================================== hablar uno a uno

        public string PorQueNoSePuedeHablarUnoAUno(string miembroId) {
            var motivo = PorQueNoConEl(miembroId, MinutosDeUnoAUno);
            if (motivo != null) return motivo;
            return R.Tablero.Miembro(miembroId).HabloHoy ? "Hoy ya hablaste con esa persona." : null;
        }

        /// <summary>
        /// Sentarse un rato con un compañero: cuenta como va de verdad (lo que el tablero no dice) y, por haberle
        /// preguntado, la moral sube un poco. Una vez al dia por persona.
        /// </summary>
        public ResultadoDeUnoAUno HablarUnoAUno(string miembroId, out ResultadoDeAvance avance) {
            Exigir(PorQueNoSePuedeHablarUnoAUno(miembroId));
            var m = R.Tablero.Miembro(miembroId);
            m.HabloHoy = true;
            var ficha = FichasDelEquipo().First(f => f.Miembro.Id == miembroId);
            var r = new ResultadoDeUnoAUno { Quien = m.Nombre, LoQueDice = ficha.ComoVa };
            var antes = W.MoralEquipo;
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { "MoralEquipo", 0.5 } });
            r.Cambios["MoralEquipo"] = Math.Round(W.MoralEquipo - antes, 2);
            // Si le cuesta lo que lleva, dice a quien le iria mejor: es la pista para reasignar.
            var actual = ficha.Ahora.FirstOrDefault();
            if (actual != null && m.HabilidadPara(actual.Tipo) < 0.95) {
                var mejor = R.Tablero.Miembros.Where(x => x.Id != m.Id).OrderByDescending(x => x.HabilidadPara(actual.Tipo)).FirstOrDefault();
                if (mejor != null && mejor.HabilidadPara(actual.Tipo) > 1.05)
                    r.LoQueDice += $" «A {mejor.Nombre} esto se le da mejor que a mí.»";
            }
            avance = AvanzarReloj(MinutosDeUnoAUno);
            return r;
        }

        // ==================================================================== un respiro

        public string PorQueNoSePuedeDarRespiro(string miembroId) {
            var motivo = PorQueNoConEl(miembroId, MinutosDeRespiro);
            if (motivo != null) return motivo;
            var m = R.Tablero.Miembro(miembroId);
            if (m.DescansaHoy) return "Hoy ya tiene un respiro.";
            if (!string.IsNullOrEmpty(m.ParejaHoy)) return "Hoy trabaja en pareja.";
            return R.Tablero.Miembros.Count(x => !x.DescansaHoy) <= 1 ? "Alguien tiene que seguir con el trabajo." : null;
        }

        /// <summary>
        /// Hoy no lleva tarjeta. Baja su cansancio (y algo el del equipo), pero el equipo avanza menos este dia: es
        /// una persona menos. Con dos compañeros se nota mas que con cuatro.
        /// </summary>
        public ResultadoDeAvance DarRespiro(string miembroId) {
            Exigir(PorQueNoSePuedeDarRespiro(miembroId));
            var t = R.Tablero;
            var m = t.Miembro(miembroId);
            m.DescansaHoy = true;
            m.DesvioDeCansancio = Math.Max(-30, m.DesvioDeCansancio - 8);
            t.BonoDeFlujoHoy -= 0.20 / Math.Max(1, t.Miembros.Count);
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { "Cansancio", -1.5 } });
            return AvanzarReloj(MinutosDeRespiro);
        }

        // ==================================================================== en pareja

        public string PorQueNoSePuedePonerEnPareja(string miembroId, string conQuienId) {
            var motivo = PorQueNoConEl(miembroId, MinutosDePareja);
            if (motivo != null) return motivo;
            if (miembroId == conQuienId) return "Una pareja son dos personas.";
            var a = R.Tablero.Miembro(miembroId);
            var b = R.Tablero.Miembro(conQuienId);
            if (b == null) return "Esa persona no está en el equipo.";
            if (a.DescansaHoy || b.DescansaHoy) return "Uno de los dos tiene hoy un respiro.";
            if (!string.IsNullOrEmpty(a.ParejaHoy) || !string.IsNullOrEmpty(b.ParejaHoy)) return "Uno de los dos ya trabaja hoy en pareja.";
            return null;
        }

        /// <summary>
        /// Dos personas en la misma tarjeta durante hoy. Sale menos trabajo (dos hacen el de algo mas que uno), pero lo
        /// que sale va revisado mientras se escribe: sube la cobertura y baja la deuda.
        /// </summary>
        public ResultadoDeAvance PonerEnPareja(string miembroId, string conQuienId) {
            Exigir(PorQueNoSePuedePonerEnPareja(miembroId, conQuienId));
            var t = R.Tablero;
            t.Miembro(miembroId).ParejaHoy = conQuienId;
            t.Miembro(conQuienId).ParejaHoy = miembroId;
            t.BonoDeFlujoHoy -= 0.16 / Math.Max(1, t.Miembros.Count);
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { "Cobertura", 2.0 }, { "DeudaTecnica", -1.0 } });
            return AvanzarReloj(MinutosDePareja);
        }
    }
}
