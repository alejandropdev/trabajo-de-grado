using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Relaciones;
using Nexus.Core.Tablero;

namespace Nexus.Core.Minijuegos {
    /// <summary>
    /// Cuanta ayuda trae un reto. El nivel fija la base (2: sin ayudas automaticas); el equipo y las relaciones
    /// pueden subirla un punto, y el jugador ve quien se la dio. Dos caminos:
    ///   · un compañero del equipo que es fuerte en lo que pide el reto (habilidad ≥ 1,2);
    ///   · alguien de la oficina que confia en ti lo bastante como para ofrecerte la ayuda propia de ese reto.
    /// Es la forma de que contratar bien y tratar bien a la gente se note dentro de los minijuegos.
    /// </summary>
    public static class AndamiajeDelReto {
        public const double HabilidadFuerte = 1.2;

        /// <summary>La habilidad del CV que cuenta para cada mecanica.</summary>
        private static readonly Dictionary<string, string> _habilidadPorVerbo = new Dictionary<string, string> {
            { Verbos.Detectar, "pruebas" },
            { Verbos.Ordenar, "logica" },
            { Verbos.Repartir, "datos" },
        };

        public static string HabilidadPara(string verbo) {
            var v = Verbos.Normalizar(verbo);
            string h;
            return v != null && _habilidadPorVerbo.TryGetValue(v, out h) ? h : null;
        }

        /// <summary>
        /// En que retos echa una mano esta persona, dicho como lo diria el equipo: «revisar diagramas», «ordenar el
        /// backlog»… Vacio si no destaca en nada. Es lo que se le enseña al jugador al formar el equipo.
        /// </summary>
        public static List<string> EnQueAyuda(MiembroDelEquipo m) {
            var lista = new List<string>();
            if (m == null || EsElJugador(m)) return lista;
            foreach (var verbo in new[] { Verbos.Detectar, Verbos.Ordenar, Verbos.Repartir })
                if (m.HabilidadPara(_habilidadPorVerbo[verbo]) >= HabilidadFuerte) lista.Add(NombreDelReto(verbo));
            return lista;
        }

        public static string NombreDelReto(string verbo) {
            switch (Verbos.Normalizar(verbo)) {
                case Verbos.Detectar: return "revisar diagramas y cambios";
                case Verbos.Ordenar: return "ordenar el backlog";
                case Verbos.Repartir: return "repartir las pruebas";
                default: return "los retos";
            }
        }

        public sealed class Resultado {
            public int Nivel;
            /// <summary>Quien sube el andamiaje (nombre visible), o null si se queda en la base.</summary>
            public string Quien;
            /// <summary>«equipo» o «confianza»: por donde llego la ayuda.</summary>
            public string Via;
            public bool Subio { get { return Quien != null; } }
        }

        /// <param name="confianzaDe">Confianza actual con un personaje (por id).</param>
        public static Resultado Efectivo(int nivelBase, string verbo, IEnumerable<MiembroDelEquipo> equipo,
                                         IEnumerable<Personaje> personajes, Func<string, int> confianzaDe) {
            var res = new Resultado { Nivel = Math.Max(0, Math.Min(3, nivelBase)) };
            if (res.Nivel >= 3 || Verbos.Normalizar(verbo) == null) return res;

            var habilidad = HabilidadPara(verbo);
            var compa = habilidad == null || equipo == null ? null
                : equipo.Where(m => m != null && !EsElJugador(m) && m.HabilidadPara(habilidad) >= HabilidadFuerte)
                        .OrderByDescending(m => m.HabilidadPara(habilidad))
                        .FirstOrDefault();
            if (compa != null) return Subir(res, compa.Nombre, "equipo");

            if (personajes != null && confianzaDe != null) {
                foreach (var p in personajes) {
                    if (p == null || p.Ayudas == null) continue;
                    // La ayuda propia del reto (no los 45 s, que valen para todo): quien la ofrece sabe de esto.
                    var suya = p.Ayudas.Where(a => a != null && a.Tipo != TiposDeAyuda.MinutosExtra &&
                                                   TiposDeAyuda.SirvePara(a.Tipo, verbo))
                                       .OrderBy(a => a.Umbral).FirstOrDefault();
                    if (suya != null && confianzaDe(p.Id) >= suya.Umbral) return Subir(res, p.Nombre, "confianza");
                }
            }
            return res;
        }

        private static Resultado Subir(Resultado r, string quien, string via) {
            r.Nivel = Math.Min(3, r.Nivel + 1);
            r.Quien = quien;
            r.Via = via;
            return r;
        }

        private static bool EsElJugador(MiembroDelEquipo m) {
            return string.Equals(m.Id, Nexus.Core.Fase1.CatalogoDeObjetos3D.IdDeTuSolo, StringComparison.OrdinalIgnoreCase);
        }
    }
}
