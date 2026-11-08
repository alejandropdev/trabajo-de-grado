using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Jornada;
using Nexus.Core.Modelo;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El rediseño de la Fase 2 (2026-11-06): las diapositivas del carrusel segun la metodologia, la bitacora que ahora
    /// se guarda, las series de los KPIs y los minijuegos de zona.
    /// </summary>
    public partial class GameSessionTests {
        // ================================================================ el carrusel

        [Test]
        public void Cada_metodologia_tiene_su_grafica_en_el_carrusel_y_cascada_ademas_sus_documentos() {
            CollectionAssert.AreEqual(new[] { "kpis", "burndown", "tablero", "equipo", "escritorio", "cliente" },
                DiapositivasDelDia.Para(new[] { "tablero", "backlog", "burndown", "pruebas" }, true));
            CollectionAssert.AreEqual(new[] { "kpis", "cfd", "tablero", "equipo", "escritorio", "cliente" },
                DiapositivasDelDia.Para(new[] { "tablero", "cfd", "pruebas" }, true));
            CollectionAssert.AreEqual(new[] { "kpis", "curvaS", "documentos", "tablero", "equipo", "escritorio" },
                DiapositivasDelDia.Para(new[] { "tablero", "curvaS", "documentos", "pruebas" }, false));
            CollectionAssert.AreEqual(new[] { "kpis", "evolucion", "tablero", "equipo", "escritorio" },
                DiapositivasDelDia.Para(null, false), "sin vistas propias, la evolucion del proyecto");
        }

        [Test]
        public void Las_metodologias_reales_dan_carruseles_distintos() {
            var vistos = new HashSet<string>();
            // El nivel de pruebas permite Scrum y Kanban (Cascada ya esta cubierta arriba con sus vistas reales).
            foreach (var m in new[] { "scrum", "kanban" }) {
                var s = Empezada(m);
                vistos.Add(string.Join(",", DiapositivasDelDia.Para(s.Politica.Vistas, false)));
            }
            Assert.AreEqual(2, vistos.Count, string.Join(" | ", vistos));
        }

        // ================================================================ la bitacora y las series

        [Test]
        public void La_bitacora_guarda_lo_mas_nuevo_primero_y_como_mucho_sesenta_lineas() {
            var r = new RuntimeState();
            for (var i = 0; i < 70; i++) r.Anotar("linea " + i);
            r.Anotar(null);
            Assert.AreEqual(RuntimeState.BitacoraMaxima, r.Bitacora.Count);
            Assert.AreEqual("linea 69", r.Bitacora[0]);
            Assert.AreEqual("linea 10", r.Bitacora.Last());
        }

        [Test]
        public void Cada_dia_deja_una_muestra_en_las_series_de_los_kpis() {
            var s = Empezada();
            s.ComenzarDia();
            Assert.Greater(s.R.SerieMoral.Count, 0);
            Assert.AreEqual(s.R.SerieAvance.Count, s.R.SerieMoral.Count);
            Assert.AreEqual(s.R.SerieAvance.Count, s.R.SerieCansancio.Count);
            Assert.AreEqual(s.R.SerieAvance.Count, s.R.SerieDeuda.Count);
            Assert.AreEqual(s.R.SerieAvance.Count, s.R.SerieCobertura.Count);
            Assert.AreEqual(s.R.SerieAvance.Count, s.R.SerieSatisfaccion.Count);
        }

        // ================================================================ los minijuegos de zona

        private static GameSession ConMinijuegoEnElBullpen(string tipo) {
            var c = CatalogoConMapa();
            c.Niveles["nivel-01"].Mapa.PorId("bullpen").MinijuegoDeZona = tipo;
            var s = Empezada(catalogo: c);
            s.ComenzarDia();
            return s;
        }

        [Test]
        public void En_el_escritorio_no_hay_minijuego_de_zona() {
            var s = ConMinijuegoEnElBullpen(TiposDeMinijuegoDeZona.Cafe);
            Assert.IsNull(s.MinijuegoDeZonaAqui());
            Assert.IsNotNull(s.PorQueNoSeJuegaElMinijuegoDeZona());
        }

        [Test]
        public void Un_buen_cafe_baja_el_cansancio_cobra_sus_minutos_y_solo_se_juega_una_vez_al_dia() {
            var s = ConMinijuegoEnElBullpen(TiposDeMinijuegoDeZona.Cafe);
            s.IrAZona("bullpen");
            Assert.AreEqual(TiposDeMinijuegoDeZona.Cafe, s.MinijuegoDeZonaAqui());
            var antes = s.MinutoDelDia;
            ResultadoDeMinijuegoDeZona r;
            s.JugarMinijuegoDeZona(true, out r);
            Assert.IsTrue(r.Exito);
            Assert.Less(r.Cambios["Cansancio"], 0, "el cafe descansa");
            Assert.AreEqual(antes + MinijuegosDeZona.Minutos(TiposDeMinijuegoDeZona.Cafe), s.MinutoDelDia);
            Assert.IsTrue(s.MinijuegoDeZonaHechoHoy("bullpen"));
            Assert.Throws<InvalidOperationException>(() => s.JugarMinijuegoDeZona(true, out r), "una vez al dia");
        }

        [Test]
        public void Fallar_la_caza_de_errores_no_cambia_nada() {
            var s = ConMinijuegoEnElBullpen(TiposDeMinijuegoDeZona.Logs);
            s.IrAZona("bullpen");
            ResultadoDeMinijuegoDeZona r;
            s.JugarMinijuegoDeZona(false, out r);
            Assert.IsFalse(r.Exito);
            Assert.IsEmpty(r.Cambios);
        }

        [Test]
        public void Los_rumores_siempre_dicen_algo() {
            var s = ConMinijuegoEnElBullpen(TiposDeMinijuegoDeZona.Rumores);
            s.IrAZona("bullpen");
            ResultadoDeMinijuegoDeZona r;
            s.JugarMinijuegoDeZona(true, out r);
            Assert.AreEqual(1, r.Revelaciones.Count);
            StringAssert.StartsWith("«", r.Revelaciones[0]);
        }

        [TestCase(0f, false)]
        [TestCase(2.36f, true)]
        [TestCase(2.1f, false)]
        [TestCase(10f, false)]
        public void La_taza_esta_bien_llena_solo_cerca_de_la_linea(float segundos, bool bien) {
            Assert.AreEqual(bien, MinijuegosDeZona.TazaBienLlena(MinijuegosDeZona.NivelDeLaTaza(segundos)));
        }

        [Test]
        public void La_linea_roja_de_los_logs_sale_siempre_en_la_segunda_mitad() {
            for (var semilla = 0; semilla < 50; semilla++)
                Assert.That(MinijuegosDeZona.IndiceDeLaRoja(semilla),
                            Is.InRange(MinijuegosDeZona.LineasDeLog / 2, MinijuegosDeZona.LineasDeLog - 1));
        }

        [Test]
        public void Un_minijuego_de_zona_que_no_existe_no_se_carga() {
            var p = CatalogoConMapa().Niveles["nivel-01"];
            p.Mapa.PorId("bullpen").MinijuegoDeZona = "karaoke";
            var e = SchemaValidator.ValidarNivel(p);
            Assert.IsTrue(e.Any(x => x.Contains("karaoke")), string.Join(" | ", e));
        }

        // ================================================================ ronda 2 (2026-11-06)

        [TestCase("Empezar ►", "Empezar »")]
        [TestCase("√  Hecho", "Hecho")]
        [TestCase("＋ Añadir", "+ Añadir")]
        [TestCase("Sin símbolos", "Sin símbolos")]
        [TestCase("Siguiente pregunta  ↵", "Siguiente pregunta")]
        public void Los_simbolos_que_la_letra_no_tiene_se_cambian_antes_de_pintar(string entrada, string esperado) {
            Assert.AreEqual(esperado, Nexus.Core.Narrativa.Glifos.Seguro(entrada));
        }

        [Test]
        public void El_orden_barajado_es_estable_y_no_siempre_el_mismo() {
            Assert.AreEqual(Nexus.Core.Narrativa.Barajar.Clave("CONV-1|3|a"), Nexus.Core.Narrativa.Barajar.Clave("CONV-1|3|a"));
            var primeros = new HashSet<string>();
            for (var dia = 1; dia <= 12; dia++)
                primeros.Add(new[] { "a", "b", "c" }.OrderBy(o => Nexus.Core.Narrativa.Barajar.Clave("CONV-1|" + dia + "|" + o)).First());
            Assert.Greater(primeros.Count, 1, "la primera opcion cambia segun el dia");
        }

        [Test]
        public void Las_novedades_de_la_bitacora_se_cuentan_hasta_que_se_abren() {
            var r = new RuntimeState();
            r.Anotar("uno"); r.Anotar("dos"); r.Anotar("tres");
            Assert.AreEqual(3, r.BitacoraSinVer);
            r.VerUnaNovedad();
            Assert.AreEqual(2, r.BitacoraSinVer);
            r.VerUnaNovedad(); r.VerUnaNovedad(); r.VerUnaNovedad();
            Assert.AreEqual(0, r.BitacoraSinVer);
        }

        [Test]
        public void En_las_charlas_ninguna_respuesta_delata_a_la_buena_por_lo_larga() {
            var raiz = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            if (string.IsNullOrEmpty(raiz))
                raiz = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(raiz, "narrativa", "conversaciones.json")));
            foreach (var c in json["conversaciones"]) {
                var p = c["pregunta"];
                if (p == null || p.Type == Newtonsoft.Json.Linq.JTokenType.Null) continue;
                var largos = p["opciones"].Select(o => ((string)o["texto"]).Split(' ').Length).ToList();
                Assert.LessOrEqual(largos.Max() / (double)largos.Min(), 1.6, $"{c["id"]}: {string.Join(",", largos)}");
            }
        }

        [Test]
        public void Los_niveles_reales_ponen_sus_minijuegos_en_sus_zonas() {
            var raiz = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            if (string.IsNullOrEmpty(raiz))
                raiz = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            var n1 = CatalogLoader.CargarPerfil(File.ReadAllText(Path.Combine(raiz, "niveles", "nivel-01.json")));
            var n0 = CatalogLoader.CargarPerfil(File.ReadAllText(Path.Combine(raiz, "niveles", "nivel-00.json")));
            Assert.AreEqual(TiposDeMinijuegoDeZona.Cafe, n1.Mapa.PorId("cafeteria").MinijuegoDeZona);
            Assert.AreEqual(TiposDeMinijuegoDeZona.Logs, n1.Mapa.PorId("data-hub").MinijuegoDeZona);
            Assert.AreEqual(TiposDeMinijuegoDeZona.Rumores, n1.Mapa.PorId("bullpen").MinijuegoDeZona);
            Assert.AreEqual(TiposDeMinijuegoDeZona.Tablon, n0.Mapa.PorId("pasillo").MinijuegoDeZona);
        }
    }
}
