using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;
using Nexus.Core.Proyecto;
using Nexus.Core.Servicios;

namespace Nexus.Core.Sesion {
    /// <summary>Lo que salio de armar el product backlog: que estuvo bien, que sobro y que falto.</summary>
    public sealed class ResultadoDeBacklog {
        public sealed class Renglon {
            /// <summary>"bien" | "sobra" | "falta".</summary>
            public string Tipo;
            public string Texto;
        }
        public List<Renglon> Renglones = new List<Renglon>();
        public Dictionary<string, double> Cambios = new Dictionary<string, double>();
        public int Pedidas, Dejadas, DeMas;
    }

    /// <summary>
    /// El product backlog inicial (ronda 6: «al iniciar todo, el jugador deberia poder semi-crear el product backlog»).
    /// En un proyecto que empieza de cero, el jugador elige que historias entran, de entre las candidatas del nivel,
    /// y las ordena. Las tarjetas del tablero salen de ahi.
    ///
    /// ★ No cambia el tamaño del trabajo por si solo: los puntos de cada modulo se reparten entre sus historias. Lo
    /// unico que mueve el alcance es meter cosas que nadie pidio. Quien no arma el backlog (los robots, un nivel
    /// con el proyecto ya empezado) recibe el tablero de siempre.
    /// </summary>
    public sealed partial class GameSession {
        /// <summary>Si en este nivel el backlog lo arma el jugador: el proyecto empieza de cero y hay historias candidatas.</summary>
        public bool SeCreaElBacklog {
            get {
                var f = Perfil.Proyecto;
                return f != null && f.BacklogInicial == BacklogsIniciales.Crear && f.Historias.Count > 0;
            }
        }

        /// <summary>Lo que paso con el backlog que armo el jugador (null si no lo armo).</summary>
        public ResultadoDeBacklog BacklogCreado {
            get { return Fase1Cerrada && R.BacklogElegido != null && R.BacklogElegido.Count > 0 ? ValorarBacklog(R.BacklogElegido) : null; }
        }

        /// <summary>Que saldria de armar el backlog con estas historias, sin aplicarlo: para enseñarlo antes de confirmar.</summary>
        public ResultadoDeBacklog ValorarBacklog(IList<string> idsEnOrden) {
            var f = Perfil.Proyecto;
            var r = new ResultadoDeBacklog();
            if (f == null) return r;
            var elegidas = new HashSet<string>(idsEnOrden ?? new string[0], StringComparer.Ordinal);
            foreach (var h in f.Historias) {
                var dentro = elegidas.Contains(h.Id);
                if (h.Tipo == TiposDeHistoria.Pedida) {
                    if (dentro) r.Pedidas++;
                    else {
                        r.Dejadas++;
                        r.Renglones.Add(new ResultadoDeBacklog.Renglon { Tipo = "falta", Texto = $"Faltó «{h.Titulo}»: {h.Porque}" });
                    }
                } else if (dentro) {
                    r.DeMas++;
                    r.Renglones.Add(new ResultadoDeBacklog.Renglon { Tipo = "sobra", Texto = $"Sobraba «{h.Titulo}»: {h.Porque}" });
                }
            }
            if (r.Dejadas == 0 && r.DeMas == 0)
                r.Renglones.Insert(0, new ResultadoDeBacklog.Renglon { Tipo = "bien", Texto = "Están todas las historias que el cliente pidió, y ninguna que no pidiera." });
            else if (r.Pedidas > 0)
                r.Renglones.Insert(0, new ResultadoDeBacklog.Renglon { Tipo = "bien", Texto = $"Entraron {r.Pedidas} de las {r.Pedidas + r.Dejadas} historias que el cliente pidió." });

            // Lo que cuesta: lo que se dejo fuera se descubre tarde (y el cliente lo nota); lo que nadie pidio es trabajo de mas.
            var deMasQueCrece = f.Historias.Count(h => elegidas.Contains(h.Id) && h.Tipo == TiposDeHistoria.NadieLaPidio);
            if (r.Dejadas > 0) r.Cambios["SatisfaccionCliente"] = -Math.Min(6, 2 * r.Dejadas);
            if (deMasQueCrece > 0) r.Cambios["Alcance"] = Math.Min(3, deMasQueCrece);
            if (r.Dejadas == 0 && r.DeMas == 0) { r.Cambios["SatisfaccionCliente"] = 2; r.Cambios["Documentacion"] = 2; }
            return r;
        }

        /// <summary>
        /// Arma el product backlog: estas historias, en este orden. Se hace con la Fase 1 abierta; las tarjetas se
        /// crean al cerrarla. Llamarlo otra vez sustituye lo anterior (todavia no se aplico nada).
        /// </summary>
        public void CrearBacklog(IList<string> idsEnOrden) {
            ExigirFase1Abierta();
            if (!SeCreaElBacklog) throw new InvalidOperationException("En este nivel el backlog ya viene hecho.");
            var f = Perfil.Proyecto;
            var ids = (idsEnOrden ?? new string[0]).Distinct().ToList();
            foreach (var id in ids)
                if (f.Historia(id) == null) throw new InvalidOperationException($"'{id}' no es una historia de este proyecto.");
            if (ids.Count == 0) throw new InvalidOperationException("Un backlog sin ninguna historia no es un backlog.");
            R.BacklogElegido = ids;
        }

        /// <summary>Al cerrar la Fase 1: lo que cuesta (o lo que da) el backlog que se armo. Antes de crear el tablero.</summary>
        private void AplicarElBacklogCreado() {
            if (R.BacklogElegido == null || R.BacklogElegido.Count == 0) return;
            var efectos = new Dictionary<string, object>();
            foreach (var kv in ValorarBacklog(R.BacklogElegido).Cambios) efectos[kv.Key] = kv.Value;
            EffectApplier.Aplicar(W, efectos);
        }
    }
}
