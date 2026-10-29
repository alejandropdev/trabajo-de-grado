using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Sesion;

namespace Nexus.Core.Reportes {
    /// <summary>Lo que el informe necesita y el DebriefReport no trae: quien juega, cuando, y los textos ya resueltos del catalogo.</summary>
    public sealed class DatosDelReporte {
        public string Estudiante = "Estudiante sin nombre";
        public string Partida;
        public bool ModoAula;
        public DateTime Fecha;

        public string Institucion = "Pontificia Universidad Javeriana";
        public string Programa = "Ingeniería de Sistemas";
        public string Curso = "Trabajo de grado ISIST-2610-10";
        public string Producto = "Nexus Protocol";

        /// <summary>El motivo de elegir la metodologia, ya en palabras del jugador (el DebriefReport solo trae su id).</summary>
        public string MotivoDeMetodologia;
        public string ArquitecturaNombre;

        /// <summary>El riesgo latente del ultimo dia y de donde venia. null = no se tiene.</summary>
        public double? RiesgoLatente, RiesgoPorCansancio, RiesgoPorDeuda, RiesgoPorCobertura, RiesgoPorDocumentacion;
    }

    /// <summary>
    /// El informe de lecciones aprendidas en PDF: lo que el Dashboard de Lecciones enseña al cerrar un nivel, en un
    /// documento que se puede mandar a los profesores. Formato APA 7 sobre A4 (portada, resumen, secciones con
    /// titulos, tablas y figuras con leyenda, referencias). Es una funcion pura del DebriefReport: no lee nada
    /// mas, asi que la misma partida da el mismo documento y no revela veredictos que el juego aun no ha dado.
    /// </summary>
    public static class ReporteDeLecciones {
        private static readonly string[] Meses = {
            "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

        public static byte[] Generar(DebriefReport r, DatosDelReporte d) {
            if (r == null) throw new ArgumentNullException(nameof(r));
            d = d ?? new DatosDelReporte();
            var nivel = string.IsNullOrEmpty(r.NivelNombre) ? r.NivelId : r.NivelNombre;
            var estudiante = string.IsNullOrWhiteSpace(d.Estudiante) ? "Estudiante sin nombre" : d.Estudiante;

            var pdf = new PdfWriter("Informe de lecciones aprendidas · " + nivel, estudiante,
                                    d.Producto + " · lecciones aprendidas", d.Fecha);
            var doc = new Documento(pdf);

            Portada(doc, nivel, estudiante, d);
            doc.NuevaPagina();
            Resumen(doc, r, d, nivel, estudiante);
            Resultado(doc, r, d);
            SeccionDeDecisiones(doc, r);
            MetodologiaYArquitectura(doc, r, d);
            Competencias(doc, r);
            PuntosParaConversar(doc, r);
            NotaTecnica(doc, r, d);
            return pdf.Generar();
        }

        // ================================================================== PORTADA

        private static void Portada(Documento doc, string nivel, string estudiante, DatosDelReporte d) {
            doc.Espacio(150);
            doc.Parrafo("Informe de lecciones aprendidas", FuentePdf.Negrita, 16, alineacion: AlineacionPdf.Centro);
            doc.Parrafo("Nivel: " + nivel, FuentePdf.Negrita, Documento.Cuerpo, alineacion: AlineacionPdf.Centro, despues: 36);
            doc.Parrafo(estudiante, alineacion: AlineacionPdf.Centro);
            doc.Parrafo(d.Programa, alineacion: AlineacionPdf.Centro);
            doc.Parrafo(d.Institucion, alineacion: AlineacionPdf.Centro);
            doc.Parrafo(d.Curso, alineacion: AlineacionPdf.Centro);
            doc.Parrafo("Videojuego " + d.Producto, alineacion: AlineacionPdf.Centro);
            doc.Parrafo(FechaLarga(d.Fecha), alineacion: AlineacionPdf.Centro);
        }

        // ================================================================== RESUMEN

        private static void Resumen(Documento doc, DebriefReport r, DatosDelReporte d, string nivel, string estudiante) {
            doc.Titulo(1, "Resumen");
            var m = r.Metodologia;
            var l = r.Lanzamiento;
            var texto = $"{estudiante} jugó el nivel «{nivel}» de {d.Producto}" +
                        (m != null ? $" con la metodología {m.MetodologiaNombre}" : "") +
                        $". El nivel terminó {(r.CumpleUmbralesDeExito ? "superado" : "sin superar")}" +
                        (l != null ? $": el lanzamiento {Minuscula(NivelesDeLanzamiento.Titulo(l.Nivel ?? (l.Exito ? NivelesDeLanzamiento.Bien : NivelesDeLanzamiento.Mal)))}, con un puntaje de {Num(l.Puntaje)} sobre 100." : ".");
            doc.Parrafo(texto, sangriaPrimera: 36);

            var decisiones = Decisiones(r);
            if (decisiones.Count > 0) {
                var c = decisiones.Count(e => e.Veredicto == Veredictos.Correcta);
                var a = decisiones.Count(e => e.Veredicto == Veredictos.Aceptable);
                var i = decisiones.Count(e => e.Veredicto == Veredictos.Incorrecta);
                doc.Parrafo($"Se evaluaron {decisiones.Count} decisiones: {c} correctas, {a} aceptables y {i} incorrectas. " +
                            "Las secciones siguientes explican el resultado, cada decisión con su motivo, la metodología y la arquitectura elegidas, " +
                            "y las competencias que el nivel puso en juego.", sangriaPrimera: 36);
            }

            var filas = new List<string[]> {
                new[] { "Estudiante", estudiante },
                new[] { "Partida", string.IsNullOrEmpty(d.Partida) ? "—" : d.Partida },
                new[] { "Nivel", nivel + " (" + r.NivelId + ")" },
                new[] { "Metodología", m != null ? m.MetodologiaNombre : "—" },
                new[] { "Resultado del nivel", r.Final == "completado" ? "Completado" : "No superado" },
                new[] { "Modo aula", d.ModoAula ? "Sí" : "No" },
                new[] { "Semilla del escenario", r.Semilla.ToString(CultureInfo.InvariantCulture) },
                new[] { "Fecha del informe", FechaLarga(d.Fecha) }
            };
            doc.Tabla("Datos de la partida", new[] { "Dato", "Valor" }, filas, new[] { 1.4, 3.6 });
        }

        // ================================================================== EL RESULTADO

        private static void Resultado(Documento doc, DebriefReport r, DatosDelReporte d) {
            doc.Titulo(1, "El resultado");
            var l = r.Lanzamiento;
            if (l == null) {
                doc.Parrafo("El nivel no llegó a su lanzamiento, así que no hay resultado que medir.", sangriaPrimera: 36);
            } else {
                doc.Parrafo($"Se entregaron {Num(l.AlcanceEntregado)} de {Num(l.AlcanceComprometido)} puntos de alcance, " +
                            $"{l.DefectosEscapados} errores llegaron al cliente y la satisfacción final fue de {Num(l.SatisfaccionCliente)} sobre 100. " +
                            $"El riesgo con el que se lanzó fue de {Num(l.RiesgoDeLanzamiento)} sobre 100.", sangriaPrimera: 36);
                if (l.Factores.Count > 0) {
                    var filas = l.Factores.Select(f => new[] { f.Nombre, f.Valor, EstadoDeFactor(f.Estado), Num(f.Puntos) + " / " + Num(f.Maximo), f.Explicacion }).ToList();
                    doc.Tabla("Factores del lanzamiento", new[] { "Factor", "Resultado", "Estado", "Puntos", "Por qué importa" }, filas,
                              new[] { 2.2, 2.4, 1.0, 1.1, 3.6 }, "El puntaje total es la suma de los puntos de los cinco factores.");
                }
            }

            if (r.CumpleUmbralesDeExito) {
                doc.Parrafo("El nivel cumplió todos sus umbrales de éxito.", sangriaPrimera: 36);
            } else if (r.UmbralesFallados != null && r.UmbralesFallados.Count > 0) {
                doc.Parrafo("Umbrales del nivel que no se cumplieron:", sangriaPrimera: 36);
                foreach (var u in r.UmbralesFallados) doc.Vineta(u);
            }

            if (l != null && l.DecisionesQuePesaron != null && l.DecisionesQuePesaron.Count > 0) {
                doc.Parrafo("Las decisiones que más pesaron en contra del resultado fueron:", sangriaPrimera: 36);
                foreach (var p in l.DecisionesQuePesaron) doc.Vineta(p);
            }

            if (d.RiesgoLatente.HasValue) {
                var partes = new List<KeyValuePair<string, double>> {
                    new KeyValuePair<string, double>("Cansancio", d.RiesgoPorCansancio ?? 0),
                    new KeyValuePair<string, double>("Deuda técnica", d.RiesgoPorDeuda ?? 0),
                    new KeyValuePair<string, double>("Poca cobertura de pruebas", d.RiesgoPorCobertura ?? 0),
                    new KeyValuePair<string, double>("Poca documentación", d.RiesgoPorDocumentacion ?? 0)
                };
                doc.Parrafo($"Al empezar el último día el riesgo latente era de {Num(d.RiesgoLatente.Value)} sobre 100. La figura muestra de dónde venía.", sangriaPrimera: 36);
                doc.FiguraDeBarras("Origen del riesgo latente al empezar el último día", partes, Math.Max(1.0, partes.Max(p => p.Value)),
                                   "Las barras están en la misma escala: la mayor ocupa todo el ancho.");
            }
        }

        // ================================================================== LAS DECISIONES

        private static void SeccionDeDecisiones(Documento doc, DebriefReport r) {
            doc.Titulo(1, "Las decisiones y su justificación");
            var entradas = Decisiones(r);
            if (entradas.Count == 0) {
                doc.Parrafo("No hay decisiones registradas en este nivel.", sangriaPrimera: 36);
            } else {
                doc.Parrafo("Cada decisión se evaluó contra la rúbrica del nivel: el motor es local y determinista, y no usa inteligencia artificial. " +
                            "La tabla recoge lo que se eligió, el veredicto y el motivo.", sangriaPrimera: 36);
                var filas = entradas.Select(e => new[] {
                    e.Dia > 0 ? e.Dia.ToString(CultureInfo.InvariantCulture) : "—",
                    e.Titulo + (string.IsNullOrEmpty(e.OpcionTexto) || e.OpcionTexto == e.OpcionId ? "" : "\nElegida: " + e.OpcionTexto),
                    NombreDeVeredicto(e.Veredicto),
                    e.Oa ?? "—",
                    (e.Razon ?? "") + (string.IsNullOrEmpty(e.NotaDeMetodologia) ? "" : "\n" + e.NotaDeMetodologia)
                }).ToList();
                doc.Tabla("Decisiones del nivel", new[] { "Día", "Decisión", "Veredicto", "Objetivo", "Por qué" }, filas,
                          new[] { 0.55, 2.8, 1.2, 1.7, 3.4 });
            }

            if (r.CadenaCausal != null && r.CadenaCausal.Count > 0) {
                doc.Parrafo("La cadena causal reúne las últimas decisiones incorrectas, desde la más reciente hasta la raíz:", sangriaPrimera: 36);
                foreach (var eslabon in r.CadenaCausal) doc.Vineta(eslabon);
            } else {
                doc.Parrafo("Ninguna decisión de este nivel desencadenó otra.", sangriaPrimera: 36);
            }
            if (r.BeatsPerdidos != null && r.BeatsPerdidos.Count > 0)
                doc.Parrafo("Escenas obligatorias que no llegaron a verse: " + string.Join(", ", r.BeatsPerdidos) + ".", sangriaPrimera: 36);
        }

        // ================================================================== METODOLOGIA Y ARQUITECTURA

        private static void MetodologiaYArquitectura(Documento doc, DebriefReport r, DatosDelReporte d) {
            doc.Titulo(1, "Metodología y arquitectura");
            doc.Titulo(2, "La metodología");
            var m = r.Metodologia;
            if (m == null) {
                doc.Parrafo("No se registró la metodología del nivel.", sangriaPrimera: 36);
            } else {
                var motivo = string.IsNullOrEmpty(d.MotivoDeMetodologia) ? m.RazonElegida : d.MotivoDeMetodologia;
                doc.Parrafo($"La metodología elegida fue {m.MetodologiaNombre}. " +
                            (m.EraAdecuada ? "Era adecuada para este proyecto" : "No era la adecuada para este proyecto") +
                            $", cuya volatilidad real fue de {Num(m.VolatilidadReal)}. El motivo con que se eligió fue: «{motivo}». " +
                            (m.RazonValida ? "Es un buen motivo para elegirla."
                             : m.RazonTrampa ? "Es un motivo trampa: se puede acertar por la razón equivocada, y el juego lo distingue."
                             : "No es de los motivos que justifican esta metodología."), sangriaPrimera: 36);
                var practicas = m.Practicas.Where(p => p.Evaluable).ToList();
                if (practicas.Count > 0) {
                    var filas = practicas.Select(p => new[] { p.Descripcion, p.Cumple ? "Sí" : "No", p.Razon ?? "" }).ToList();
                    doc.Tabla("Prácticas de la metodología frente a lo ocurrido", new[] { "Práctica", "¿Se cumplió?", "Por qué" }, filas,
                              new[] { 3.0, 1.5, 3.4 });
                }
            }

            doc.Titulo(2, "La arquitectura");
            var nombre = string.IsNullOrEmpty(d.ArquitecturaNombre) ? r.ArquitecturaElegida : d.ArquitecturaNombre;
            if (string.IsNullOrEmpty(nombre)) {
                doc.Parrafo("No se registró la arquitectura del nivel.", sangriaPrimera: 36);
            } else {
                doc.Parrafo($"La arquitectura elegida fue {nombre}" +
                            (string.IsNullOrEmpty(r.ArquitecturaVeredicto) ? "." : $", con veredicto {Minuscula(NombreDeVeredicto(r.ArquitecturaVeredicto))}.") +
                            (string.IsNullOrEmpty(r.ArquitecturaRazon) ? "" : " " + r.ArquitecturaRazon), sangriaPrimera: 36);
            }
        }

        // ================================================================== COMPETENCIAS

        private static void Competencias(Documento doc, DebriefReport r) {
            doc.Titulo(1, "Competencias");
            var oas = r.Competencia == null ? new List<string>() : r.Competencia.PorObjetivo.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            if (oas.Count == 0) {
                doc.Parrafo("Todavía no hay decisiones evaluadas, así que no hay competencias que medir.", sangriaPrimera: 36);
                return;
            }
            doc.Parrafo("La puntuación de cada objetivo de aprendizaje va de 0 a 100: una decisión correcta vale una vez y una aceptable, media vez, " +
                        "sobre el total de decisiones de ese objetivo.", sangriaPrimera: 36);
            var filas = oas.Select(oa => {
                var c = r.Competencia.PorObjetivo[oa];
                return new[] { oa, c.Correctas.ToString(CultureInfo.InvariantCulture), c.Aceptables.ToString(CultureInfo.InvariantCulture),
                               c.Incorrectas.ToString(CultureInfo.InvariantCulture), Num(c.Puntuacion) };
            }).ToList();
            doc.Tabla("Decisiones por objetivo de aprendizaje", new[] { "Objetivo", "Correctas", "Aceptables", "Incorrectas", "Puntuación" }, filas,
                      new[] { 2.0, 1.1, 1.1, 1.1, 1.1 });
            doc.FiguraDeBarras("Puntuación por objetivo de aprendizaje",
                               oas.Select(oa => new KeyValuePair<string, double>(oa, r.Competencia.PuntuacionDe(oa))).ToList(), 100);
        }

        // ================================================================== PARA CONVERSAR

        private static void PuntosParaConversar(Documento doc, DebriefReport r) {
            doc.Titulo(1, "Puntos para conversar con el docente");
            var puntos = new List<string>();

            if (r.Competencia != null)
                foreach (var oa in r.Competencia.PorObjetivo.Keys.OrderBy(k => r.Competencia.PuntuacionDe(k)).ThenBy(k => k, StringComparer.Ordinal).Take(2))
                    if (r.Competencia.PuntuacionDe(oa) < 70)
                        puntos.Add($"Reforzar {oa}: su puntuación fue de {Num(r.Competencia.PuntuacionDe(oa))} sobre 100.");

            foreach (var e in Decisiones(r).Where(x => x.Veredicto == Veredictos.Incorrecta).Take(3))
                puntos.Add($"Revisar la decisión «{e.Titulo}»" + (e.Dia > 0 ? $" del día {e.Dia}" : "") + ": " + (string.IsNullOrEmpty(e.Razon) ? "fue incorrecta." : e.Razon));

            if (r.Metodologia != null && !r.Metodologia.EraAdecuada)
                puntos.Add($"Discutir por qué {r.Metodologia.MetodologiaNombre} no era la metodología adecuada para un proyecto con esa volatilidad.");

            if (puntos.Count == 0) {
                doc.Parrafo("El nivel no dejó competencias por reforzar ni decisiones incorrectas. Queda como pregunta abierta qué haría distinto " +
                            "el estudiante si el proyecto cambiara de alcance a mitad del camino.", sangriaPrimera: 36);
            } else {
                doc.Parrafo("A partir del resultado, estos son los puntos que vale la pena revisar:", sangriaPrimera: 36);
                foreach (var p in puntos) doc.Vineta(p);
            }
        }

        // ================================================================== NOTA TECNICA

        private static void NotaTecnica(Documento doc, DebriefReport r, DatosDelReporte d) {
            doc.Titulo(1, "Nota técnica");
            doc.Parrafo($"Este informe lo generó {d.Producto} al cerrar el nivel {r.NivelId}, con la semilla {r.Semilla}. " +
                        "Los veredictos salen de las rúbricas del contenido del juego, de forma local y determinista: la misma partida con la misma semilla " +
                        "produce el mismo informe. El formato sigue la séptima edición del Manual de publicaciones de la APA (American Psychological Association, 2020), " +
                        "con dos adaptaciones para leerlo en pantalla: interlineado de 1,5 y tablas compactas.", sangriaPrimera: 36);
            doc.Titulo(1, "Referencias");
            doc.Parrafo("American Psychological Association. (2020). Publication manual of the American Psychological Association (7.ª ed.). https://doi.org/10.1037/0000165-000",
                        sangria: 36, sangriaPrimera: -36);
        }

        // ================================================================== AYUDAS

        /// <summary>Las decisiones del jugador: el lanzamiento no se juzga a si mismo y la comprobacion del expediente no cuenta.</summary>
        private static List<EntradaTraza> Decisiones(DebriefReport r) {
            if (r.Traza == null) return new List<EntradaTraza>();
            return r.Traza.Entradas.Where(e => e != null && e.Origen != "LANZAMIENTO" && e.Origen != "COMPRENSION").ToList();
        }

        public static string NombreDeVeredicto(string v) {
            switch (v) {
                case Veredictos.Correcta: return "Correcta";
                case Veredictos.Aceptable: return "Aceptable";
                case Veredictos.Incorrecta: return "Incorrecta";
                default: return string.IsNullOrEmpty(v) ? "—" : v;
            }
        }

        private static string EstadoDeFactor(string estado) {
            switch (estado) {
                case "bien": return "Bien";
                case "regular": return "Regular";
                case "mal": return "Mal";
                default: return estado ?? "—";
            }
        }

        private static string Minuscula(string s) {
            return string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>Con coma decimal, como se escribe en espanol, y sin ceros de relleno: «62,5», «70».</summary>
        public static string Num(double v) {
            return v.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        public static string FechaLarga(DateTime f) {
            return f.Day + " de " + Meses[f.Month - 1] + " de " + f.Year;
        }
    }
}
