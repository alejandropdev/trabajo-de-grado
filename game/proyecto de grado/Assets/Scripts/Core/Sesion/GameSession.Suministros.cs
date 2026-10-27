using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Fase1;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;

namespace Nexus.Core.Sesion {
    /// <summary>Un suministro tal como esta hoy: cuantos quedan, si se puede usar ya y que hace.</summary>
    public sealed class FichaDeSuministro {
        public Objeto3D Objeto;
        public int Cantidad;
        /// <summary>Por que no se puede usar ahora (para todo el equipo). null = se puede.</summary>
        public string PorQueNo;
        /// <summary>Se le da a una persona, no al equipo entero.</summary>
        public bool ParaUnaPersona;
        /// <summary>Lo que cambia en el proyecto, ya calculado sobre el estado de hoy.</summary>
        public Dictionary<string, double> Cambios = new Dictionary<string, double>();
    }

    /// <summary>Lo que paso al gastar un suministro.</summary>
    public sealed class ResultadoDeSuministro {
        public string Texto;
        /// <summary>Se abuso de el y hizo lo contrario.</summary>
        public bool Reboto;
        public Dictionary<string, double> Cambios = new Dictionary<string, double>();
        public ResultadoDeAvance Avance;
    }

    /// <summary>
    /// Los suministros que se trajeron del recorrido 3D (pizza, cafe, Guepardex…) y como se gastan durante el
    /// desarrollo. Cada uno con su regla, que viene de objetos-3d.json: la pizza anima a todos pero solo cada pocos
    /// dias, el cafe da un empujon al dia, y la Guepardex quita el cansancio a una persona… hasta que se abusa de ella.
    /// </summary>
    public sealed partial class GameSession {
        public List<FichaDeSuministro> FichasDeSuministros() {
            var fichas = new List<FichaDeSuministro>();
            foreach (var o in _catalogo.Objetos3D.Objetos) {
                if (o == null || o.Tipo != TiposDeObjeto3D.Suministro || o.Suministro == null) continue;
                int n;
                R.Suministros.TryGetValue(o.Id, out n);
                if (n <= 0) continue;
                var f = new FichaDeSuministro {
                    Objeto = o, Cantidad = n,
                    ParaUnaPersona = o.Suministro.Objetivo == ObjetivosDeSuministro.Miembro,
                    PorQueNo = PorQueNoSePuedeUsarSuministro(o.Id, null, true)
                };
                foreach (var kv in o.Suministro.Efectos) f.Cambios[kv.Key] = kv.Value;
                fichas.Add(f);
            }
            return fichas;
        }

        public string PorQueNoSePuedeUsarSuministro(string id, string miembroId) {
            return PorQueNoSePuedeUsarSuministro(id, miembroId, false);
        }

        /// <param name="sinPersona">Para la ficha: lo que se puede decir antes de elegir a quien darselo.</param>
        private string PorQueNoSePuedeUsarSuministro(string id, string miembroId, bool sinPersona) {
            var o = _catalogo.Objetos3D.Buscar(id);
            if (o == null || o.Tipo != TiposDeObjeto3D.Suministro || o.Suministro == null) return "Eso no es un suministro.";
            var s = o.Suministro;
            var motivo = PorQueNoSePuedeActuar(s.MinutosDeUso);
            if (motivo != null) return motivo;
            int n;
            if (!R.Suministros.TryGetValue(id, out n) || n <= 0) return "No te queda ninguno.";

            if (s.EnfriamientoDias > 0) {
                var ultimo = R.UsosDeSuministros.Where(u => u.Suministro == id).Select(u => (int?)u.Dia).Max();
                if (ultimo.HasValue && R.DiaActual - ultimo.Value < s.EnfriamientoDias) {
                    var faltan = s.EnfriamientoDias - (R.DiaActual - ultimo.Value);
                    return faltan == 1 ? "Vuelve a estar disponible mañana." : $"Vuelve a estar disponible dentro de {faltan} días.";
                }
            }
            if (s.VecesPorDia > 0 && R.UsosDeSuministros.Count(u => u.Suministro == id && u.Dia == R.DiaActual) >= s.VecesPorDia)
                return s.VecesPorDia == 1 ? "Hoy ya lo usaste." : $"Hoy ya lo usaste {s.VecesPorDia} veces.";

            if (s.Objetivo != ObjetivosDeSuministro.Miembro || sinPersona) return null;
            if (string.IsNullOrEmpty(miembroId)) return "Elige a quién dárselo.";
            return R.Tablero.Miembro(miembroId) == null ? "Esa persona no está en el equipo." : null;
        }

        /// <summary>
        /// Si darle ahora este suministro a esa persona haria lo contrario: ya lo tomo demasiadas veces estos dias.
        /// La UI lo pregunta antes, para avisar; el motor lo vuelve a mirar al gastarlo.
        /// </summary>
        public bool Rebotaria(string id, string miembroId) {
            var o = _catalogo.Objetos3D.Buscar(id);
            var s = o == null ? null : o.Suministro;
            if (s == null || s.ReboteUsos <= 0 || string.IsNullOrEmpty(miembroId)) return false;
            var desde = R.DiaActual - s.ReboteVentanaDias;
            return R.UsosDeSuministros.Count(u => u.Suministro == id && u.Miembro == miembroId && u.Dia > desde) >= s.ReboteUsos;
        }

        /// <summary>
        /// Gasta uno. Al equipo: sus efectos (por EffectApplier, INV-2) y el bono de flujo de hoy. A una persona: su
        /// cansancio, o el rebote si se abuso. Cuesta los minutos que diga el catalogo.
        /// </summary>
        public ResultadoDeSuministro UsarSuministro(string id, string miembroId = null) {
            Exigir(PorQueNoSePuedeUsarSuministro(id, miembroId));
            var o = _catalogo.Objetos3D.Buscar(id);
            var s = o.Suministro;
            var r = new ResultadoDeSuministro();
            var paraUno = s.Objetivo == ObjetivosDeSuministro.Miembro;
            var reboto = paraUno && Rebotaria(id, miembroId);

            if (s.Efectos.Count > 0 && !reboto) {
                var d = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var kv in s.Efectos) d[kv.Key] = kv.Value;
                foreach (var kv in EffectApplier.Previsualizar(W, d)) r.Cambios[kv.Key] = Math.Round(kv.Value, 2);
                EffectApplier.Aplicar(W, d);
            }
            if (s.BonoDeFlujo != 0 && !reboto) R.Tablero.BonoDeFlujoHoy += s.BonoDeFlujo;

            if (paraUno) {
                var m = R.Tablero.Miembro(miembroId);
                var antes = Math.Max(0, Math.Min(100, W.Cansancio + m.DesvioDeCansancio));
                m.DesvioDeCansancio = Math.Max(-30, Math.Min(30, m.DesvioDeCansancio + (reboto ? s.ReboteDesvio : s.DesvioDeCansancio)));
                var despues = Math.Max(0, Math.Min(100, W.Cansancio + m.DesvioDeCansancio));
                r.Cambios["Cansancio de " + m.Nombre] = Math.Round(despues - antes, 2);
                r.Texto = reboto
                    ? $"{m.Nombre} lleva demasiadas {o.Nombre} estos días: el bajón llega de golpe y está más cansado que antes."
                    : $"{m.Nombre} se toma la {o.Nombre} y recupera el ritmo.";
            } else {
                r.Texto = string.IsNullOrEmpty(o.Texto) ? $"El equipo disfruta de: {o.Nombre}." : o.Texto;
            }

            R.Suministros[id] = R.Suministros[id] - 1;
            R.UsosDeSuministros.Add(new UsoDeSuministro { Dia = R.DiaActual, Suministro = id, Miembro = paraUno ? miembroId : null, Reboto = reboto });
            r.Reboto = reboto;
            r.Avance = AvanzarReloj(s.MinutosDeUso);
            return r;
        }
    }
}
