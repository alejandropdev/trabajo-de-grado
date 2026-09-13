using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nexus.Core.Evaluacion {
    /// <summary>
    /// Las barras de competencia (§4.8.2): learning analytics embebido, local y determinista.
    /// Clave = objetivo de aprendizaje (OA-*). Se acumula por partida y se fusiona en el perfil permanente.
    /// </summary>
    public sealed class CompetenceProfile {
        public sealed class Conteo {
            public int Correctas;
            public int Aceptables;
            public int Incorrectas;

            [JsonIgnore]
            public int Total {
                get { return Correctas + Aceptables + Incorrectas; }
            }

            /// <summary>0-100. Sin decisiones vale 0, no NaN: el Dashboard no puede pintar NaN.</summary>
            [JsonIgnore]
            public double Puntuacion {
                get { return Total == 0 ? 0.0 : 100.0 * (Correctas + 0.5 * Aceptables) / Total; }
            }
        }

        public Dictionary<string, Conteo> PorObjetivo = new Dictionary<string, Conteo>();

        public void Acumular(string oa, string veredicto) {
            if (string.IsNullOrEmpty(oa))
                throw new ArgumentException("No se puede acumular una decision sin objetivo de aprendizaje.", nameof(oa));
            if (!Veredictos.EsValido(veredicto))
                throw new ArgumentException($"Veredicto '{veredicto}' desconocido para '{oa}'.", nameof(veredicto));

            var c = ConteoDe(oa);
            if (veredicto == Veredictos.Correcta) c.Correctas++;
            else if (veredicto == Veredictos.Aceptable) c.Aceptables++;
            else c.Incorrectas++;
        }

        /// <summary>Puntuacion 0-100 de un objetivo; 0 si nunca se evaluo.</summary>
        public double PuntuacionDe(string oa) {
            Conteo c;
            return oa != null && PorObjetivo.TryGetValue(oa, out c) ? c.Puntuacion : 0.0;
        }

        /// <summary>
        /// Suma los conteos de otra partida. Es lo que usara el perfil permanente:
        /// los proyectos se olvidan, las decisiones no.
        /// </summary>
        public void Fusionar(CompetenceProfile otra) {
            if (otra == null) return;
            foreach (var kv in otra.PorObjetivo) {
                var c = ConteoDe(kv.Key);
                c.Correctas += kv.Value.Correctas;
                c.Aceptables += kv.Value.Aceptables;
                c.Incorrectas += kv.Value.Incorrectas;
            }
        }

        private Conteo ConteoDe(string oa) {
            Conteo c;
            if (!PorObjetivo.TryGetValue(oa, out c)) {
                c = new Conteo();
                PorObjetivo[oa] = c;
            }
            return c;
        }
    }
}
