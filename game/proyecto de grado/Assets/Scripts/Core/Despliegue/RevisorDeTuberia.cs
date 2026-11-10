using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Despliegue {
    /// <summary>Como queda una seccion (un hueco) de la tuberia al probarla.</summary>
    public enum EstadoDeSeccion {
        /// <summary>La etapa esta y esta bien puesta: la luz pasa.</summary>
        Bien,
        /// <summary>Aqui se rompe el flujo (un atajo, un orden que no vale, una etapa que falta): la luz se detiene.</summary>
        Mal,
        /// <summary>Hueco sin etapa que se puede dejar asi (ya estan todas las obligatorias): la luz lo atraviesa apagada.</summary>
        Vacia,
        /// <summary>Despues del primer fallo: no se mira. Se queda apagada aunque la pieza estuviera bien.</summary>
        SinRevisar
    }

    public sealed class SeccionRevisada {
        public int Hueco;
        /// <summary>La etapa puesta ahi (null = vacio).</summary>
        public string Tramo;
        public EstadoDeSeccion Estado;
        /// <summary>Solo en Mal: por que, en una frase corta.</summary>
        public string Motivo;
    }

    /// <summary>La tuberia recorrida de izquierda a derecha, hasta donde llega la luz.</summary>
    public sealed class FlujoRevisado {
        public List<SeccionRevisada> Secciones = new List<SeccionRevisada>();
        /// <summary>El primer hueco que falla (0-based); -1 si ninguno.</summary>
        public int PrimerFallo = -1;
        /// <summary>Las etapas obligatorias que no estan puestas en ningun hueco.</summary>
        public List<string> Faltan = new List<string>();
        /// <summary>Todo bien y no falta nada: la luz llega al cliente y se puede pasar a los modulos.</summary>
        public bool Completa;
        /// <summary>Si la luz llega al final pero faltan etapas: lo que hay que decir en «El cliente».</summary>
        public string MotivoFinal;
    }

    /// <summary>
    /// La tuberia de la Fase 3 al PROBARLA (2026-11-09): un paquete de luz recorre los huecos de izquierda a derecha y se
    /// detiene en el primero que falla; lo que haya despues no se mira, aunque este bien, porque el flujo ya se rompio.
    /// Es lo que se anima en pantalla, y lo que decide si se puede pasar a los modulos (solo con Completa). Funcion pura;
    /// las reglas son las de EvaluadorDeDespliegue (una tuberia Completa da «bien» en la tuberia).
    ///
    /// Falla un hueco si:
    ///   · tiene una trampa (un atajo);
    ///   · tiene una etapa antes de otra de la que depende (esta puesta mas adelante);
    ///   · tiene una etapa cuya dependencia OBLIGATORIA no esta en la tuberia;
    ///   · esta vacio y aun quedan etapas obligatorias sin poner. Con todas ya puestas, un hueco vacio se salta.
    /// </summary>
    public static class RevisorDeTuberia {
        public static FlujoRevisado Revisar(DespliegueDef def, IList<string> huecos) {
            if (def == null) throw new ArgumentNullException(nameof(def));
            var n = Math.Max(0, def.Tuberia.Huecos);
            var puestos = new List<string>(n);
            for (var i = 0; i < n; i++) {
                var id = huecos != null && i < huecos.Count ? huecos[i] : null;
                puestos.Add(!string.IsNullOrEmpty(id) && def.Tramo(id) != null ? id : null);
            }
            var colocados = new HashSet<string>(puestos.Where(x => x != null), StringComparer.Ordinal);
            var faltan = def.Tuberia.Tramos.Where(t => t.Obligatorio && !t.Trampa && !colocados.Contains(t.Id)).Select(t => t.Id).ToList();

            var flujo = new FlujoRevisado { Faltan = faltan };
            var yaBien = new HashSet<string>(StringComparer.Ordinal);
            var vistos = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < n; i++) {
                var s = new SeccionRevisada { Hueco = i, Tramo = puestos[i] };
                flujo.Secciones.Add(s);
                if (flujo.PrimerFallo >= 0) { s.Estado = EstadoDeSeccion.SinRevisar; continue; }

                if (s.Tramo == null) {
                    if (faltan.Count > 0) { Romper(flujo, s, "Aquí falta una etapa: todavía hay etapas obligatorias sin poner."); continue; }
                    s.Estado = EstadoDeSeccion.Vacia;
                    continue;
                }
                var tramo = def.Tramo(s.Tramo);
                if (!vistos.Add(s.Tramo)) { Romper(flujo, s, "Esta etapa ya está en la tubería."); continue; }
                if (tramo.Trampa) { Romper(flujo, s, "Es un atajo: " + Minuscula(tramo.Porque)); continue; }

                string motivo = null;
                foreach (var d in tramo.DespuesDe ?? new List<string>()) {
                    if (yaBien.Contains(d)) continue;
                    var antes = def.Tramo(d);
                    var nombre = antes == null ? d : antes.Nombre;
                    if (colocados.Contains(d)) { motivo = $"Va antes de «{nombre}», y la necesita primero."; break; }
                    if (antes != null && antes.Obligatorio) { motivo = $"Necesita «{nombre}» antes, y no está en la tubería."; break; }
                }
                if (motivo != null) { Romper(flujo, s, motivo); continue; }
                s.Estado = EstadoDeSeccion.Bien;
                yaBien.Add(s.Tramo);
            }

            flujo.Completa = flujo.PrimerFallo < 0 && faltan.Count == 0;
            if (flujo.PrimerFallo < 0 && faltan.Count > 0)
                flujo.MotivoFinal = "Falta: " + string.Join(", ", faltan.Select(id => "«" + def.Tramo(id).Nombre + "»")) + ".";
            return flujo;
        }

        private static void Romper(FlujoRevisado flujo, SeccionRevisada s, string motivo) {
            s.Estado = EstadoDeSeccion.Mal;
            s.Motivo = motivo;
            flujo.PrimerFallo = s.Hueco;
        }

        private static string Minuscula(string s) {
            return string.IsNullOrEmpty(s) ? "" : char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
