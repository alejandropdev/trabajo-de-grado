using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Metodologia {
    /// <summary>
    /// Una forma de trocear el nivel dentro de una metodologia: sprints mas cortos o mas largos, etapas repartidas de
    /// otra manera, o cada cuanto se revisa y se entrega en un flujo continuo.
    /// </summary>
    public sealed class OpcionDeCalendario {
        public string Id;
        public string Titulo;
        /// <summary>Que se gana y que cuesta, en una frase.</summary>
        public string Detalle;
        /// <summary>Los dias de cada unidad (sprint o etapa), en orden. En un flujo continuo, una sola con todos los dias.</summary>
        public List<int> Unidades = new List<int>();
        /// <summary>Los nombres de las unidades («Sprint 1», «Requisitos»…).</summary>
        public List<string> Nombres = new List<string>();
        /// <summary>Los dias que cierran con un despliegue, contando el ultimo del nivel.</summary>
        public List<int> Despliegues = new List<int>();
        /// <summary>Es la que trae la metodologia si no se toca nada.</summary>
        public bool EsLaDeSerie;
    }

    /// <summary>
    /// Las duraciones entre las que se puede elegir (ronda 6: «dependiendo de la metodologia, poder escoger el tiempo
    /// de cada sprint, etapa o iteracion»). No inventa mecanicas: cambia el calendario, y de el salen solas mas o
    /// menos ceremonias, planificaciones y despliegues.
    ///
    /// ★ La opcion «de serie» deja el calendario exactamente como lo trae el JSON: quien no elige juega lo de siempre.
    /// </summary>
    public static class OpcionesDeCalendario {
        public const string DeSerie = "serie";

        public static List<OpcionDeCalendario> Para(MethodologyProfile perfil, int diasTotales) {
            var opciones = new List<OpcionDeCalendario>();
            if (perfil == null || perfil.Calendario == null) return opciones;
            var serie = Describir(perfil, DeSerie, diasTotales);
            serie.EsLaDeSerie = true;

            switch (perfil.Calendario.Tipo) {
                case TiposDeCalendario.Iterativo:
                    foreach (var n in new[] { 4, 3, 2, 1 }) {
                        var largo = (int)Math.Ceiling(diasTotales / (double)n);
                        var ultimo = diasTotales - largo * (n - 1);
                        if (largo < 2 || ultimo < 2) continue;
                        var o = Describir(perfil, "sprints-" + n, diasTotales);
                        if (o.Unidades.Count != n || opciones.Any(x => x.Unidades.SequenceEqual(o.Unidades))) continue;
                        o.EsLaDeSerie = o.Unidades.SequenceEqual(serie.Unidades);
                        var u = perfil.Calendario.EtiquetaUnidad.ToLowerInvariant();
                        o.Titulo = n == 1 ? $"Un solo {u} de {diasTotales} días" : $"{n} {u}s de {string.Join(", ", o.Unidades.Distinct())} días";
                        o.Detalle = n == 1
                            ? "Una sola planificación y una sola entrega, al final. Menos reuniones, pero el cliente no ve nada hasta el último día."
                            : n >= 4 ? "Planificas, enseñas y entregas muy seguido: te enteras pronto de lo que falla, pero cada cierre gasta reuniones y un despliegue."
                            : n == 3 ? "Un punto medio: tres entregas para corregir el rumbo, sin vivir de reunión en reunión."
                            : "Dos entregas: tiempo para construir tranquilo, pero solo una ocasión de corregir antes del final.";
                        opciones.Add(o);
                    }
                    break;

                case TiposDeCalendario.Continuo:
                    foreach (var cada in new[] { 3, 5, 7 }) {
                        if (cada > diasTotales) continue;
                        var o = Describir(perfil, "cada-" + cada, diasTotales);
                        o.EsLaDeSerie = o.Despliegues.SequenceEqual(serie.Despliegues);
                        o.Titulo = $"Revisar el flujo y entregar cada {cada} días";
                        o.Detalle = cada <= 3 ? "El cliente recibe cosas muy seguido y los atascos se ven pronto, pero paras a revisar y desplegar más veces."
                                  : cada >= 7 ? "Pocas paradas: más tiempo seguido de trabajo, y los problemas del flujo tardan más en verse."
                                  : "Lo habitual: una revisión y una entrega por semana de trabajo.";
                        opciones.Add(o);
                    }
                    break;

                case TiposDeCalendario.Secuencial:
                    foreach (var id in new[] { "equilibrado", "mas-analisis", "mas-construccion" }) {
                        var o = Describir(perfil, id == "equilibrado" ? DeSerie : id, diasTotales);
                        o.Id = id == "equilibrado" ? DeSerie : id;
                        if (opciones.Any(x => x.Unidades.SequenceEqual(o.Unidades))) continue;
                        o.EsLaDeSerie = id == "equilibrado";
                        o.Titulo = id == "equilibrado" ? "Etapas equilibradas"
                                 : id == "mas-analisis" ? "Más días para requisitos y diseño" : "Más días para construir y probar";
                        o.Detalle = id == "equilibrado" ? "El reparto habitual entre pensar, construir y probar."
                                  : id == "mas-analisis" ? "Se pregunta y se diseña con calma: menos sorpresas después, pero menos días para construir y probar."
                                  : "Se construye y se prueba con holgura, pero lo que no se preguntó a tiempo se va a suponer.";
                        opciones.Add(o);
                    }
                    break;
            }

            if (opciones.Count == 0 || !opciones.Any(o => o.EsLaDeSerie)) {
                serie.Titulo = "Como viene de serie";
                serie.Detalle = "El calendario habitual de esta forma de trabajar.";
                opciones.Insert(0, serie);
            }
            return opciones;
        }

        /// <summary>El calendario del perfil con la opcion aplicada, descrito: sus unidades y sus dias de despliegue.</summary>
        private static OpcionDeCalendario Describir(MethodologyProfile perfil, string opcionId, int diasTotales) {
            var copia = perfil.Clone();
            Aplicar(copia, opcionId, diasTotales);
            var reglas = new MethodologyRules(copia, diasTotales);
            var o = new OpcionDeCalendario { Id = opcionId };
            foreach (var t in reglas.Tramos) {
                o.Unidades.Add(Math.Min(t.DiaFin, diasTotales) - t.DiaInicio + 1);
                o.Nombres.Add(t.Nombre);
            }
            for (var d = 1; d <= diasTotales; d++)
                if (d == diasTotales || reglas.EsDiaDeDespliegue(d)) o.Despliegues.Add(d);
            return o;
        }

        /// <summary>Cambia el calendario del perfil (que tiene que ser una copia) segun la opcion. «serie» no toca nada.</summary>
        public static void Aplicar(MethodologyProfile perfil, string opcionId, int diasTotales) {
            if (perfil == null || perfil.Calendario == null || string.IsNullOrEmpty(opcionId) || opcionId == DeSerie) return;
            var cal = perfil.Calendario;

            if (opcionId.StartsWith("sprints-", StringComparison.Ordinal) && cal.Tipo == TiposDeCalendario.Iterativo) {
                int n;
                if (!int.TryParse(opcionId.Substring("sprints-".Length), out n) || n < 1) throw NoExiste(opcionId, perfil);
                cal.Iteraciones = n;
                cal.LongitudIteracion = (int)Math.Ceiling(diasTotales / (double)n);
                return;
            }
            if (opcionId.StartsWith("cada-", StringComparison.Ordinal) && cal.Tipo == TiposDeCalendario.Continuo) {
                int cada;
                if (!int.TryParse(opcionId.Substring("cada-".Length), out cada) || cada < 1) throw NoExiste(opcionId, perfil);
                // Las ceremonias que iban con la cadencia de serie (la revision del flujo) se mueven con ella.
                var antes = cal.CadaCuantosDias > 0 ? cal.CadaCuantosDias : MethodologyRules.DiasEntreDesplieguesContinuos;
                if (perfil.Ceremonias != null)
                    foreach (var c in perfil.Ceremonias)
                        if (c != null && string.Equals(c.Cuando, "cada", StringComparison.OrdinalIgnoreCase) && c.CadaNDias == antes) c.CadaNDias = cada;
                cal.CadaCuantosDias = cada;
                return;
            }
            if ((opcionId == "mas-analisis" || opcionId == "mas-construccion") && cal.Tipo == TiposDeCalendario.Secuencial) {
                // Las dos primeras etapas son pensar (requisitos y diseño); las dos siguientes, construir y probar.
                for (var i = 0; i < cal.Etapas.Count; i++) {
                    var pensar = i < 2;
                    var hacer = i >= 2 && i < 4;
                    var peso = opcionId == "mas-analisis" ? (pensar ? 1.5 : hacer ? 0.75 : 1.0) : (hacer ? 1.4 : pensar ? 0.65 : 1.0);
                    cal.Etapas[i].Dias = Math.Max(1, (int)Math.Round(cal.Etapas[i].Dias * peso * 10));
                }
                cal.AjustarEtapasAlNivel = true;
                return;
            }
            throw NoExiste(opcionId, perfil);
        }

        private static InvalidOperationException NoExiste(string opcionId, MethodologyProfile perfil) {
            return new InvalidOperationException($"'{opcionId}' no es una duración válida para {perfil.Id}.");
        }
    }
}
