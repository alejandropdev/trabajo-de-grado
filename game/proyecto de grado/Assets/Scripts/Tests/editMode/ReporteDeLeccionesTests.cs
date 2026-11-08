using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nexus.Core.Evaluacion;
using Nexus.Core.Reportes;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El informe PDF de las lecciones aprendidas: el escritor produce un PDF bien formado, el maquetador no se sale
    /// de los margenes y el contenido sale del DebriefReport sin inventar nada.
    /// </summary>
    public class ReporteDeLeccionesTests {
        private static readonly DateTime Fecha = new DateTime(2026, 10, 28, 10, 30, 0);

        // ------------------------------------------------------------------ AYUDAS

        private static string Latin1(byte[] bytes) {
            var sb = new StringBuilder(bytes.Length);
            foreach (var b in bytes) sb.Append((char)b);
            return sb.ToString();
        }

        /// <summary>El texto del PDF tal como se leeria: los escapes octales (\363) vuelven a ser su letra.</summary>
        private static string Legible(byte[] pdf) {
            return Regex.Replace(Latin1(pdf), @"\\([0-7]{3})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 8)).ToString());
        }

        private static DebriefReport Ejemplo(bool superado = true, int decisiones = 4) {
            var traza = new DecisionTrace();
            for (var i = 0; i < decisiones; i++)
                traza.Registrar(new EntradaTraza {
                    Dia = i + 1, Origen = "EV-" + i, Titulo = "Decisión número " + (i + 1), OpcionId = "o" + i,
                    OpcionTexto = "Opción elegida " + (i + 1),
                    Veredicto = i % 3 == 0 ? Veredictos.Correcta : i % 3 == 1 ? Veredictos.Aceptable : Veredictos.Incorrecta,
                    Oa = i % 2 == 0 ? "OA-SCRUM" : "OA-DOC",
                    Razon = "Motivo " + (i + 1) + " según la rúbrica.",
                    NotaDeMetodologia = i == 0 ? "En Scrum esto se resuelve en la retrospectiva." : null
                });
            var comp = new CompetenceProfile();
            foreach (var e in traza.Entradas) comp.Acumular(e.Oa, e.Veredicto);

            return new DebriefReport {
                NivelId = "N1", NivelNombre = "Cradle Lifts", Semilla = 1234,
                Final = superado ? "completado" : "fallado",
                CumpleUmbralesDeExito = superado,
                UmbralesFallados = superado ? new List<string>() : new List<string> { "La cobertura quedó por debajo del 60 %." },
                Lanzamiento = new LaunchResult {
                    Exito = superado, Nivel = superado ? NivelesDeLanzamiento.Bien : NivelesDeLanzamiento.Mal,
                    Puntaje = superado ? 78.5 : 31, RiesgoDeLanzamiento = 40, AlcanceEntregado = 30, AlcanceComprometido = 36,
                    DefectosEscapados = 3, SatisfaccionCliente = 62,
                    Factores = new List<FactorDeLanzamiento> {
                        new FactorDeLanzamiento { Nombre = "Alcance entregado", Valor = "30 de 36 puntos (83 %)", Estado = "regular", Explicacion = "Lo prometido y no entregado, el cliente lo nota primero.", Puntos = 25, Maximo = 30 }
                    },
                    DecisionesQuePesaron = new List<string> { "Día 3 · Decisión número 3: «Opción elegida 3»" }
                },
                Metodologia = new MethodologyReport {
                    MetodologiaId = "scrum", MetodologiaNombre = "Scrum", Familia = "agil", RazonElegida = "r1",
                    RazonValida = true, VolatilidadReal = 62, EraAdecuada = true,
                    Practicas = new List<PracticaEvaluada> {
                        new PracticaEvaluada { Descripcion = "Hacer retrospectivas", Evaluable = true, Cumple = true, Razon = "Se eligieron acciones de mejora." },
                        new PracticaEvaluada { Descripcion = "Sin horas extra", Evaluable = true, Cumple = false, Razon = "Hubo días con horas extra." }
                    }
                },
                Competencia = comp,
                Traza = traza,
                ArquitecturaElegida = "A2", ArquitecturaVeredicto = "aceptable", ArquitecturaRazon = "Funciona, pero acopla la interfaz a los datos.",
                CadenaCausal = new List<string> { "Decisión número 3 trajo la deuda técnica." },
                BeatsPerdidos = new List<string>()
            };
        }

        private static DatosDelReporte Datos() {
            return new DatosDelReporte {
                Estudiante = "Ana María Pérez", Partida = "Partida de prueba", Fecha = Fecha, MotivoDeMetodologia = "Los requisitos cambian mucho",
                ArquitecturaNombre = "Capas", RiesgoLatente = 48, RiesgoPorCansancio = 20, RiesgoPorDeuda = 15, RiesgoPorCobertura = 8, RiesgoPorDocumentacion = 5
            };
        }

        // ------------------------------------------------------------------ EL PDF

        [Test]
        public void Un_pdf_empieza_con_su_cabecera_y_acaba_en_EOF() {
            var pdf = Latin1(ReporteDeLecciones.Generar(Ejemplo(), Datos()));
            StringAssert.StartsWith("%PDF-1.4", pdf);
            StringAssert.EndsWith("%%EOF\n", pdf);
        }

        [Test]
        public void Los_offsets_de_la_tabla_xref_apuntan_a_cada_objeto() {
            var pdf = Latin1(ReporteDeLecciones.Generar(Ejemplo(), Datos()));
            var inicio = int.Parse(Regex.Match(pdf, @"startxref\n(\d+)\n").Groups[1].Value, CultureInfo.InvariantCulture);
            Assert.AreEqual("xref", pdf.Substring(inicio, 4));

            var total = int.Parse(Regex.Match(pdf.Substring(inicio), @"xref\n0 (\d+)\n").Groups[1].Value, CultureInfo.InvariantCulture);
            var entradas = Regex.Matches(pdf.Substring(inicio), @"(\d{10}) 00000 n \n");
            Assert.AreEqual(total - 1, entradas.Count);
            for (var i = 0; i < entradas.Count; i++) {
                var offset = int.Parse(entradas[i].Groups[1].Value, CultureInfo.InvariantCulture);
                StringAssert.StartsWith((i + 1) + " 0 obj", pdf.Substring(offset), "El objeto " + (i + 1) + " no esta donde dice la tabla.");
            }
        }

        [Test]
        public void El_numero_de_paginas_del_catalogo_coincide_con_las_paginas_dibujadas() {
            var pdf = Latin1(ReporteDeLecciones.Generar(Ejemplo(decisiones: 40), Datos()));
            var cuenta = int.Parse(Regex.Match(pdf, @"/Count (\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
            Assert.AreEqual(cuenta, Regex.Matches(pdf, @"/Type /Page /Parent").Count);
            Assert.Greater(cuenta, 3, "Con 40 decisiones el informe tiene que pasar de la portada, el resumen y poco mas.");
        }

        [Test]
        public void El_largo_declarado_de_cada_flujo_es_el_real() {
            var pdf = Latin1(ReporteDeLecciones.Generar(Ejemplo(decisiones: 12), Datos()));
            foreach (Match m in Regex.Matches(pdf, @"/Length (\d+) >>\nstream\n"))
                Assert.AreEqual("endstream", pdf.Substring(m.Index + m.Length + int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), 9));
        }

        [Test]
        public void Las_tildes_y_la_enie_salen_en_WinAnsi() {
            var w = new PdfWriter("t", "a", "s", Fecha);
            w.NuevaPagina();
            w.Texto(72, 100, "canción (año) \\ ¿qué?", FuentePdf.Normal, 12);
            var bytes = Latin1(w.Generar());
            StringAssert.Contains(@"canci\363n \(a\361o\) \\ \277qu\351?", bytes);
        }

        [Test]
        public void Lo_que_WinAnsi_no_tiene_sale_como_interrogante() {
            Assert.AreEqual('?', PdfWriter.Codificar('√'));
            Assert.AreEqual(0x97, PdfWriter.Codificar('—'));
        }

        [Test]
        public void La_misma_entrada_da_los_mismos_bytes() {
            CollectionAssert.AreEqual(ReporteDeLecciones.Generar(Ejemplo(), Datos()), ReporteDeLecciones.Generar(Ejemplo(), Datos()));
        }

        [Test]
        public void Las_tablas_de_anchos_cubren_los_95_caracteres_imprimibles() {
            Assert.AreEqual(95, PdfWriter.AnchoRoman.Length);
            Assert.AreEqual(95, PdfWriter.AnchoNegrita.Length);
            Assert.AreEqual(95, PdfWriter.AnchoCursiva.Length);
            // Valores conocidos de las AFM de Times: «A» 722, «i» 278, espacio 250.
            Assert.AreEqual(722 * 12 / 1000.0, PdfWriter.Ancho("A", FuentePdf.Normal, 12), 1e-9);
            Assert.AreEqual(278 * 12 / 1000.0, PdfWriter.Ancho("i", FuentePdf.Normal, 12), 1e-9);
            Assert.AreEqual(250 * 12 / 1000.0, PdfWriter.Ancho(" ", FuentePdf.Normal, 12), 1e-9);
            Assert.AreEqual(PdfWriter.Ancho("n", FuentePdf.Normal, 12), PdfWriter.Ancho("ñ", FuentePdf.Normal, 12), 1e-9);
        }

        // ------------------------------------------------------------------ EL MAQUETADOR

        [Test]
        public void Ninguna_linea_ajustada_pasa_del_ancho() {
            var texto = "Un párrafo largo con palabras de todos los tamaños, incluido un identificador larguísimo_sin_espacios_que_no_cabe_en_una_sola_linea_del_documento y un cierre.";
            foreach (var fuente in new[] { FuentePdf.Normal, FuentePdf.Negrita, FuentePdf.Cursiva, FuentePdf.Mono }) {
                var lineas = Documento.Ajustar(texto, fuente, 12, 200, 164);
                Assert.Greater(lineas.Count, 3);
                for (var i = 0; i < lineas.Count; i++)
                    Assert.LessOrEqual(PdfWriter.Ancho(lineas[i], fuente, 12), i == 0 ? 164 : 200, "Linea " + i + " con " + fuente + ": " + lineas[i]);
            }
        }

        [Test]
        public void Los_saltos_de_linea_del_texto_se_respetan() {
            var lineas = Documento.Ajustar("uno\ndos\n\ntres", FuentePdf.Normal, 12, 400, 400);
            CollectionAssert.AreEqual(new[] { "uno", "dos", "", "tres" }, lineas);
        }

        // ------------------------------------------------------------------ EL CONTENIDO

        [Test]
        public void El_informe_tiene_todas_sus_secciones() {
            var texto = Legible(ReporteDeLecciones.Generar(Ejemplo(), Datos()));
            foreach (var parte in new[] {
                "Informe de lecciones aprendidas", "Resumen", "El resultado", "Las decisiones y su justificación",
                "Metodología y arquitectura", "La metodología", "La arquitectura", "Competencias",
                "Puntos para conversar con el docente", "Nota técnica", "Referencias" })
                StringAssert.Contains(parte, texto, "Falta la seccion «" + parte + "».");
        }

        [Test]
        public void Salen_los_datos_del_estudiante_y_del_nivel() {
            var texto = Legible(ReporteDeLecciones.Generar(Ejemplo(), Datos()));
            StringAssert.Contains("Ana María Pérez", texto);
            StringAssert.Contains("Cradle Lifts", texto);
            StringAssert.Contains("Scrum", texto);
            StringAssert.Contains("28 de octubre de 2026", texto);
            StringAssert.Contains("78,5", texto, "Los decimales van con coma.");
        }

        [Test]
        public void Un_nivel_fallado_y_uno_superado_no_dicen_lo_mismo() {
            var bien = Legible(ReporteDeLecciones.Generar(Ejemplo(superado: true), Datos()));
            var mal = Legible(ReporteDeLecciones.Generar(Ejemplo(superado: false), Datos()));
            StringAssert.Contains("superado", bien);
            StringAssert.Contains("sin superar", mal);
            StringAssert.Contains("La cobertura quedó por debajo del 60 %.", mal);
            StringAssert.DoesNotContain("La cobertura quedó por debajo", bien);
            StringAssert.Contains("cumplió todos sus umbrales", bien);
        }

        [Test]
        public void Cada_decision_aparece_con_su_veredicto_y_su_motivo() {
            var texto = Legible(ReporteDeLecciones.Generar(Ejemplo(decisiones: 3), Datos()));
            StringAssert.Contains("Decisión número 1", texto);
            StringAssert.Contains("Correcta", texto);
            StringAssert.Contains("Incorrecta", texto);
            StringAssert.Contains("Motivo 2", texto);
        }

        [Test]
        public void Con_muchas_decisiones_la_tabla_sigue_en_la_pagina_siguiente_con_su_cabecera() {
            var pdf = ReporteDeLecciones.Generar(Ejemplo(decisiones: 60), Datos());
            var texto = Legible(pdf);
            // La cabecera de «Decisiones del nivel» se repite en cada pagina que ocupa la tabla.
            Assert.GreaterOrEqual(Regex.Matches(texto, @"\(Veredicto\)").Count, 2);
            StringAssert.Contains("Decisión número 60", texto);
        }

        [Test]
        public void Sin_lanzamiento_ni_metodologia_ni_competencias_el_informe_se_genera_igual() {
            var vacio = new DebriefReport { NivelId = "N0", Final = "fallado" };
            byte[] pdf = null;
            Assert.DoesNotThrow(() => pdf = ReporteDeLecciones.Generar(vacio, null));
            var texto = Legible(pdf);
            StringAssert.Contains("No hay decisiones registradas", texto);
            StringAssert.Contains("no llegó a su lanzamiento", texto);
            StringAssert.Contains("Estudiante sin nombre", texto);
        }

        [Test]
        public void Un_reporte_nulo_es_un_error_de_programacion() {
            Assert.Throws<ArgumentNullException>(() => ReporteDeLecciones.Generar(null, Datos()));
        }

        [Test]
        public void Las_tablas_y_figuras_llevan_su_leyenda_numerada() {
            var texto = Legible(ReporteDeLecciones.Generar(Ejemplo(), Datos()));
            foreach (var leyenda in new[] { "Tabla 1", "Tabla 2", "Tabla 3", "Figura 1", "Figura 2" })
                StringAssert.Contains("(" + leyenda + ")", texto);
        }

        // ------------------------------------------------------------------ EL NOMBRE DEL ARCHIVO

        [Test]
        public void El_nombre_del_archivo_no_lleva_tildes_ni_espacios() {
            var nombre = NombreDeReporte.Para("N1", "Ana María Pérez Núñez", Fecha);
            Assert.AreEqual("NexusProtocol_Lecciones_N1_Ana_Maria_Perez_Nunez_2026-10-28_1030.pdf", nombre);
            StringAssert.IsMatch(@"^[A-Za-z0-9_\-\.]+$", nombre);
        }

        [Test]
        public void Los_caracteres_prohibidos_en_Windows_y_los_nombres_vacios_se_sanean() {
            Assert.AreEqual("NexusProtocol_Lecciones_nivel_estudiante_2026-10-28_1030.pdf", NombreDeReporte.Para(null, "  ", Fecha));
            var nombre = NombreDeReporte.Para("N/1", "a:b*c?d\"e<f>g|h\\i", Fecha);
            foreach (var c in "/:*?\"<>|\\ ") Assert.AreEqual(-1, nombre.IndexOf(c), "Quedo «" + c + "» en " + nombre);
        }

        [Test]
        public void Un_nombre_larguisimo_no_hace_un_archivo_ilegible() {
            var nombre = NombreDeReporte.Para("N1", new string('x', 500), Fecha);
            Assert.Less(nombre.Length, 100);
        }

        [Test]
        public void Repetir_el_nivel_a_otra_hora_da_otro_archivo() {
            Assert.AreNotEqual(NombreDeReporte.Para("N1", "Ana", Fecha), NombreDeReporte.Para("N1", "Ana", Fecha.AddMinutes(7)));
        }
    }
}
