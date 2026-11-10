using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Corregir
{
    /// <summary>
    /// El diagrama con cartas, evaluado. Funcion pura: (definicion, cartas puestas) -> resultado, con las mismas claves
    /// que Detectar (todos | parcial | falsoPositivo | omitido), asi el motor, el Dashboard y la telemetria no cambian.
    ///   · una zona se arregla si su carta esta entre sus soluciones;
    ///   · un señuelo se «toca» si le juegan una carta de verdad (cambiar lo que funciona tambien cuesta);
    ///   · «Está bien así» en una zona es dejar pasar el problema.
    /// </summary>
    public static class CorregirEvaluador
    {
        public static ResultadoMinijuego Evaluar(MinijuegoDef def, CorregirState st)
        {
            var puntos = PuntosDelDiagrama.De(def);
            var traza = new List<MarcaRegistrada>();
            var acertadas = new HashSet<string>();
            var tocados = new HashSet<string>();
            var detalle = new List<string>();

            foreach (var p in puntos)
            {
                var carta = st.CartaEn(p.Id);
                var nombre = PuntosDelDiagrama.NombreDeCarta(def, carta);
                var donde = $"Punto #{p.Numero} · {PuntosDelDiagrama.NombreDePieza(def, p.Pieza)}: ";
                if (p.EsReal)
                {
                    var bien = carta != null && p.Zona.Soluciones.Contains(carta);
                    if (bien) acertadas.Add(p.Zona.Id);
                    var buena = PuntosDelDiagrama.NombreDeCarta(def, p.Zona.Soluciones.FirstOrDefault());
                    if (bien) detalle.Add(donde + $"«{nombre}» lo arregla. " + p.Zona.Explicacion);
                    else if (carta == null) detalle.Add(donde + $"se quedó sin decidir. Lo arreglaba «{buena}». " + p.Zona.Explicacion);
                    else if (carta == PuntosDelDiagrama.EstaBien) detalle.Add(donde + $"lo diste por bueno, y no lo estaba. Lo arreglaba «{buena}». " + p.Zona.Explicacion);
                    else
                    {
                        var porQueNo = PuntosDelDiagrama.Carta(def, carta)?.PorQueNo;
                        detalle.Add(donde + $"pusiste «{nombre}», pero no lo arregla" + (string.IsNullOrEmpty(porQueNo) ? "." : ": " + porQueNo) +
                                    $" Lo arreglaba «{buena}». " + p.Zona.Explicacion);
                    }
                    if (carta != null)
                        traza.Add(new MarcaRegistrada
                        {
                            Commits = p.Zona.Commits.ToList(), Etiqueta = carta, ZonaAcertada = bien ? p.Zona.Id : null,
                            SegundoDeLaPartida = (int)Momento(st, p.Id)
                        });
                }
                else
                {
                    var tocado = carta != null && carta != PuntosDelDiagrama.EstaBien;
                    if (tocado) tocados.Add(p.Senuelo.Id);
                    if (carta == PuntosDelDiagrama.EstaBien) detalle.Add(donde + "bien visto: estaba bien. " + p.Senuelo.RazonNoEsDefecto);
                    else if (tocado) detalle.Add(donde + $"le pusiste «{nombre}» a algo que estaba bien. " + p.Senuelo.RazonNoEsDefecto);
                    else detalle.Add(donde + "se quedó sin decidir; estaba bien. " + p.Senuelo.RazonNoEsDefecto);
                    if (carta != null)
                        traza.Add(new MarcaRegistrada
                        {
                            Commits = p.Senuelo.Commits.ToList(), Etiqueta = carta, SenueloTocado = tocado ? p.Senuelo.Id : null,
                            SegundoDeLaPartida = (int)Momento(st, p.Id)
                        });
                }
            }

            string clave;
            if (st.Asignaciones.Count == 0) clave = ResultadosDeMinijuego.Omitido;
            else if (def.Zonas.Count == 0)
                clave = tocados.Count > 0 ? ResultadosDeMinijuego.FalsoPositivo
                      : st.TodoDecidido(puntos) ? ResultadosDeMinijuego.Todos : ResultadosDeMinijuego.Parcial;
            else if (tocados.Count > def.ToleranciaDeSenuelos) clave = ResultadosDeMinijuego.FalsoPositivo;
            else if (acertadas.Count == def.Zonas.Count && tocados.Count == 0) clave = ResultadosDeMinijuego.Todos;
            else clave = ResultadosDeMinijuego.Parcial;

            Consecuencia c;
            if (!def.Consecuencias.TryGetValue(clave, out c)) c = new Consecuencia();
            var res = new ResultadoMinijuego
            {
                MinijuegoId = def.Id,
                Resultado = clave,
                Rubrica = c.Rubrica,
                EfectosInmediatos = new Dictionary<string, float>(c.EfectosInmediatos),
                EfectosDiferidos = c.EfectosDiferidos.ToList(),
                Hallazgos = c.Hallazgos.ToList(),
                Traza = traza,
                TextoCierre = def.Cierre?.Texto
            };
            res.Detalle.AddRange(detalle);
            return res;
        }

        private static float Momento(CorregirState st, string punto)
        {
            float s;
            return st.Momentos.TryGetValue(punto, out s) ? s : 0;
        }

        /// <summary>Como sale cada punto, para pintar el cierre y la verificacion: acertado, fallado o señuelo bien/mal.</summary>
        public static bool Acierta(MinijuegoDef def, PuntoDelDiagrama p, string carta)
        {
            if (p.EsReal) return carta != null && p.Zona.Soluciones.Contains(carta);
            return carta == null || carta == PuntosDelDiagrama.EstaBien;
        }
    }
}
