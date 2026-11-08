using System;
using System.IO;
using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Core.Datos;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Las escenas de hallazgo (2026-11-06): lo que de ellas vive en Core. Cada carta y cada advertencia del contenido
    /// real tiene lo suyo y no se repite; las startups traen su historia en hitos; las notas se parten en charla; el
    /// codigo se colorea sin romper las etiquetas de TextMeshPro.
    /// </summary>
    public class HallazgosTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        [Test]
        public void Cada_carta_tiene_su_emblema_y_ninguno_se_repite() {
            var cartas = Catalogo().Coleccionables.Where(c => c.Serie == SeriesDeColeccionables.Carta).ToList();
            Assert.That(cartas.Count, Is.GreaterThan(0));
            foreach (var c in cartas)
                Assert.IsTrue(EmblemasDeColeccionables.DeCartas.ContainsKey(c.Id), $"{c.Id} («{c.Titulo}») no tiene emblema.");
            var claves = cartas.Select(c => EmblemasDeColeccionables.DeCartas[c.Id]).ToList();
            Assert.AreEqual(claves.Count, claves.Distinct().Count(), "Dos cartas comparten emblema.");
        }

        [Test]
        public void Cada_advertencia_tiene_su_cartel_y_su_icono_sin_repetir_la_pareja() {
            var advertencias = Catalogo().Coleccionables.Where(c => c.Serie == SeriesDeColeccionables.Advertencia).ToList();
            foreach (var c in advertencias)
                Assert.IsTrue(EmblemasDeColeccionables.DeAdvertencias.ContainsKey(c.Id), $"{c.Id} no tiene cartel.");
            var iconos = advertencias.Select(c => EmblemasDeColeccionables.DeAdvertencias[c.Id].Value).ToList();
            Assert.AreEqual(iconos.Count, iconos.Distinct().Count(), "Dos advertencias comparten icono.");
        }

        [Test]
        public void Cada_nota_tiene_un_remitente_distinto() {
            var notas = Catalogo().Coleccionables.Where(c => c.Serie == SeriesDeColeccionables.Nota).ToList();
            foreach (var c in notas)
                Assert.IsTrue(EmblemasDeColeccionables.RemitentesDeNotas.ContainsKey(c.Id), $"{c.Id} no tiene remitente.");
            var quienes = notas.Select(c => EmblemasDeColeccionables.RemitentesDeNotas[c.Id]).ToList();
            Assert.AreEqual(quienes.Count, quienes.Distinct().Count());
        }

        [Test]
        public void Cada_startup_cuenta_su_historia_en_al_menos_tres_hitos_y_el_ultimo_esta_en_su_texto() {
            var startups = Catalogo().Coleccionables.Where(c => c.Serie == SeriesDeColeccionables.Startup).ToList();
            Assert.That(startups.Count, Is.GreaterThan(0));
            foreach (var s in startups) {
                Assert.That(s.Hitos.Count, Is.GreaterThanOrEqualTo(3), s.Id);
                Assert.IsTrue(s.Hitos.All(h => !string.IsNullOrEmpty(h.Fecha) && !string.IsNullOrEmpty(h.Texto)), s.Id);
                // Sin hechos nuevos: el año del cierre sale del texto o de la fuente del propio coleccionable.
                var ultimo = s.Hitos.Last().Fecha;
                var ano = new string(ultimo.Where(char.IsDigit).ToArray());
                Assert.IsTrue((s.Texto + " " + s.Fuente).Contains(ano), $"{s.Id}: el año «{ano}» del cierre no está en su texto ni en su fuente.");
            }
        }

        [Test]
        public void Una_nota_firmada_se_parte_en_autor_y_frases() {
            var l = LecturaDeNota.Partir("«Cuidado con esa chica, en la personalidad se ve que es peligrosa y lo es, mantén distancia. – Anónimo.»");
            Assert.AreEqual("Anónimo", l.Autor);
            Assert.AreEqual(1, l.Frases.Count);
            StringAssert.EndsWith("distancia.", l.Frases[0]);
        }

        [Test]
        public void Una_nota_sin_firma_se_parte_en_sus_frases() {
            var l = LecturaDeNota.Partir("«Alguien se volvió a comer la pizza de la nevera sin anotar su nombre. Óscar dice que no fue él, pero tenía salsa en la camisa.»");
            Assert.IsNull(l.Autor);
            Assert.AreEqual(2, l.Frases.Count);
            StringAssert.StartsWith("Óscar", l.Frases[1]);
        }

        [Test]
        public void Todas_las_notas_reales_dan_al_menos_una_frase() {
            foreach (var n in Catalogo().Coleccionables.Where(c => c.Serie == SeriesDeColeccionables.Nota))
                Assert.That(LecturaDeNota.Partir(n.Texto).Frases.Count, Is.GreaterThan(0), n.Id);
        }

        [Test]
        public void En_bash_el_comando_las_opciones_y_las_rutas_llevan_su_color() {
            var r = ResaltadoDeCodigo.Colorear("ls -la /var/log/legacy", "bash");
            StringAssert.Contains("<color=" + ResaltadoDeCodigo.ColorComando + ">ls</color>", r);
            StringAssert.Contains("<color=" + ResaltadoDeCodigo.ColorOpcion + ">-la</color>", r);
            StringAssert.Contains("<color=" + ResaltadoDeCodigo.ColorRuta + ">/var/log/legacy</color>", r);
        }

        [Test]
        public void En_un_lenguaje_se_colorean_claves_cadenas_comentarios_y_numeros() {
            var r = ResaltadoDeCodigo.Colorear("if (x > 3) return \"hola\"; // fin", "csharp");
            StringAssert.Contains("<color=" + ResaltadoDeCodigo.ColorClave + ">if</color>", r);
            StringAssert.Contains("<color=" + ResaltadoDeCodigo.ColorClave + ">return</color>", r);
            StringAssert.Contains("<color=" + ResaltadoDeCodigo.ColorNumero + ">3</color>", r);
            StringAssert.Contains("<color=" + ResaltadoDeCodigo.ColorCadena + ">\"hola\"</color>", r);
            StringAssert.Contains("<color=" + ResaltadoDeCodigo.ColorComentario + ">// fin</color>", r);
            // El «>» del codigo no es una etiqueta.
            StringAssert.Contains("<noparse>></noparse>", r);
        }

        [Test]
        public void Un_codigo_es_de_terminal_si_no_dice_lenguaje_o_dice_bash() {
            Assert.IsTrue(new Coleccionable().EsDeTerminal);
            Assert.IsTrue(new Coleccionable { Lenguaje = "bash" }.EsDeTerminal);
            Assert.IsFalse(new Coleccionable { Lenguaje = "python" }.EsDeTerminal);
        }

        [Test]
        public void Un_hito_sin_fecha_no_pasa_el_validador() {
            var c = new Coleccionable {
                Id = "COL-X", Serie = SeriesDeColeccionables.Startup, Titulo = "X", Texto = "t", Ubicacion = "u",
                Nicho = "n", Causa = "c", Fuente = "f"
            };
            c.Hitos.Add(new HitoDeStartup { Fecha = "", Texto = "algo" });
            var errores = SchemaValidator.ValidarColeccionables(new System.Collections.Generic.List<Coleccionable> { c });
            Assert.IsTrue(errores.Any(e => e.Contains("hito 1")), string.Join("\n", errores));
        }
    }
}
