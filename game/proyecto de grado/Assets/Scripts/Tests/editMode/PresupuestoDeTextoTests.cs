using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Presupuesto de texto del Nivel 0. Los jugadores abandonaban hacia el dia 3 porque cada pantalla pedia
    /// leer antes de actuar; esto mide cuanto texto de lectura obligatoria hay en el contenido real y no deja
    /// que vuelva a crecer. Los techos solo bajan: cada hito de reduccion los baja a lo que consiguio.
    ///
    /// Se cuentan palabras de texto visible (enunciados, opciones, lineas de guion, burbujas de la guia).
    /// "mas" (Quiero saber mas) y los susurros no cuentan: son opcionales o se ven despues de actuar.
    /// Las pizarras, el glosario y los cursos quedan fuera: son el apoyo opcional y no se tocan.
    /// </summary>
    public class PresupuestoDeTextoTests {
        private static string Raiz() {
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            Assert.IsTrue(Directory.Exists(raiz), "No encuentro el contenido en " + raiz);
            return raiz;
        }

        private static JToken Leer(string relativo) {
            return JToken.Parse(File.ReadAllText(Path.Combine(Raiz(), relativo)));
        }

        internal static int Palabras(string texto) {
            return string.IsNullOrWhiteSpace(texto)
                ? 0
                : texto.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static int PalabrasEnTodo(JToken t) {
            if (t is JValue valor) return valor.Type == JTokenType.String ? Palabras((string)valor) : 0;
            return t is JContainer c
                ? c.Descendants().OfType<JValue>().Where(v => v.Type == JTokenType.String).Sum(v => Palabras((string)v))
                : 0;
        }

        // ------------------------------------------------------------ medidas

        /// <summary>Guiones del N0: se cuenta la variante mas larga de cada uno, que es lo peor que lee un jugador.</summary>
        internal static int PalabrasDeGuionesN0() {
            var total = 0;
            foreach (var g in Leer("narrativa/guiones.json")["guiones"]) {
                if ((string)g["nivel"] != "nivel-00") continue;
                var mayor = 0;
                foreach (var v in ((JObject)g["variantes"]).Properties()) {
                    var n = v.Value.OfType<JObject>().Sum(l => Palabras((string)l["texto"]));
                    if (n > mayor) mayor = n;
                }
                total += mayor;
            }
            return total;
        }

        /// <summary>Burbujas de la guia de Marisol en el N0: solo el texto, no el "mas".</summary>
        internal static int PalabrasDeGuiaN0() {
            return Leer("narrativa/guia-tutorial.json")["pasos"]
                .Where(p => (string)p["nivel"] == "nivel-00")
                .Sum(p => Palabras((string)p["texto"]));
        }

        /// <summary>
        /// Prueba de admision (sin susurros). Lo que se lee por defecto: la frase y las etiquetas de cada pregunta; el enunciado
        /// y las opciones completas (el instrumento original) quedan a un boton. Sin vista corta, el enunciado y las opciones.
        /// </summary>
        internal static int PalabrasDeAdmision() {
            var a = Leer("prueba-de-admision.json");
            return a["preguntas"].Sum(p => !string.IsNullOrEmpty((string)p["frase"]) && p["opciones"].All(o => !string.IsNullOrEmpty((string)o["etiqueta"]))
                ? Palabras((string)p["frase"]) + p["opciones"].Sum(o => Palabras((string)o["etiqueta"]))
                : Palabras((string)p["enunciado"]) + p["opciones"].Sum(o => Palabras((string)o["texto"])));
        }

        /// <summary>
        /// Eventos tutorial del N0 (EV-TUT-*). Con tarjeta se lee el aviso (el mensaje que llega por el canal), la frase y las
        /// etiquetas; la rubrica se lee despues, en el Dashboard. Sin tarjeta, todo el evento.
        /// </summary>
        internal static int PalabrasDeEventosTutorial() {
            return Leer("eventos/eventos.json")["eventos"]
                .Where(e => ((string)e["id"] ?? "").StartsWith("EV-TUT", StringComparison.Ordinal))
                .Sum(e => string.IsNullOrEmpty((string)e["frase"])
                    ? PalabrasEnTodo(e)
                    : Palabras((string)e["telegrafiado"]["texto"]) + Palabras((string)e["frase"]) +
                      e["opciones"].Sum(o => Palabras((string)o["etiqueta"])));
        }

        /// <summary>Retos de aviso del N0 (MJ-F0-*): lo que se ve antes y durante el reto.</summary>
        internal static int PalabrasDeRetosF0() {
            return new[] { "MJ-F0-01", "MJ-F0-02" }.Sum(id => PalabrasEnTodo(Leer("minijuegos/" + id + ".json")));
        }

        /// <summary>
        /// Lo que se lee de las ceremonias jugables de las tres metodologias: su frase y las etiquetas de sus opciones;
        /// si una no trae tarjeta, el parrafo y las frases largas de siempre. (El «?» y las actas se ven despues o a peticion.)
        /// </summary>
        internal static int PalabrasDeCeremonias() {
            var total = 0;
            foreach (var m in new[] { "scrum", "kanban", "cascada" })
                foreach (var c in Leer("metodologias/" + m + ".json")["ceremonias"]) {
                    var opciones = c["opciones"] as JArray;
                    if (opciones == null || opciones.Count == 0) continue;
                    var tarjeta = !string.IsNullOrEmpty((string)c["frase"]) && opciones.All(o => !string.IsNullOrEmpty((string)o["etiqueta"]));
                    total += tarjeta
                        ? Palabras((string)c["frase"]) + opciones.Sum(o => Palabras((string)o["etiqueta"]))
                        : Palabras((string)c["texto"]) + Palabras((string)c["paraQueSirve"]) + opciones.Sum(o => Palabras((string)o["texto"]));
                }
            return total;
        }

        // ------------------------------------------------------------ techos (solo bajan)

        [Test]
        public void La_linea_base_se_puede_medir_y_no_esta_vacia() {
            TestContext.WriteLine("guiones N0 (variante mas larga): " + PalabrasDeGuionesN0());
            TestContext.WriteLine("guia N0 (texto de burbujas):     " + PalabrasDeGuiaN0());
            TestContext.WriteLine("admision (enunciados+opciones):  " + PalabrasDeAdmision());
            TestContext.WriteLine("eventos EV-TUT:                  " + PalabrasDeEventosTutorial());
            TestContext.WriteLine("retos MJ-F0:                     " + PalabrasDeRetosF0());
            TestContext.WriteLine("ceremonias (3 metodologias):     " + PalabrasDeCeremonias());
            Assert.Greater(PalabrasDeGuionesN0(), 0);
            Assert.Greater(PalabrasDeGuiaN0(), 0);
            Assert.Greater(PalabrasDeAdmision(), 0);
            Assert.Greater(PalabrasDeEventosTutorial(), 0);
            Assert.Greater(PalabrasDeRetosF0(), 0);
            Assert.Greater(PalabrasDeCeremonias(), 0);
        }

        // Techos del 2026-10-29. Cada hito los baja; ninguno debe subir.
        //   H0 (antes de reducir): guiones 959 · guia 2199 · admision 750 · EV-TUT 244 · MJ-F0 791
        //   H3: guia 2199 → 612 (lo anterior pasa a «Quiero saber mas»); ceremonias → tarjetas (127 palabras visibles)
        private const int TechoGuiones = 474;   // H3/H4: 959 → 474 (cinematicas, entrevista y escenas del tutorial)
        private const int TechoGuia = 612;
        private const int TechoAdmision = 220;   // H4: 750 → 220 (frase + etiquetas; el instrumento original queda a un boton)
        private const int TechoEventosTutorial = 71;   // H3: 244 → 71 (aviso + frase + etiquetas)
        private const int TechoRetosF0 = 676;   // H3: 791 → 676 (comoSeJuega y enElProyecto); el resto se ve al jugar o en el «¿Por qué?»
        private const int TechoCeremonias = 127;

        [Test]
        public void El_texto_del_nivel_0_no_supera_el_techo_vigente() {
            Assert.LessOrEqual(PalabrasDeGuionesN0(), TechoGuiones, "guiones del N0");
            Assert.LessOrEqual(PalabrasDeGuiaN0(), TechoGuia, "burbujas de la guia del N0");
            Assert.LessOrEqual(PalabrasDeAdmision(), TechoAdmision, "prueba de admision");
            Assert.LessOrEqual(PalabrasDeEventosTutorial(), TechoEventosTutorial, "eventos EV-TUT");
            Assert.LessOrEqual(PalabrasDeRetosF0(), TechoRetosF0, "retos MJ-F0");
            Assert.LessOrEqual(PalabrasDeCeremonias(), TechoCeremonias, "ceremonias");
        }

        // La meta del plan era 1.500-2.000 palabras obligatorias en todo el N0. Lo medido aqui no incluye el expediente de
        // la Fase 1 ni el Dashboard, asi que el techo del total es lo conseguido hoy; ver C4-reduccion-de-texto.md.
        private const int TechoDelTotalMedido = 2200;

        [Test]
        public void El_total_medido_del_nivel_0_no_supera_su_techo() {
            var total = PalabrasDeGuionesN0() + PalabrasDeGuiaN0() + PalabrasDeAdmision() + PalabrasDeEventosTutorial() +
                        PalabrasDeRetosF0() + PalabrasDeCeremonias();
            TestContext.WriteLine("total medido del N0: " + total);
            Assert.LessOrEqual(total, TechoDelTotalMedido, "total medido del N0");
        }
    }
}
