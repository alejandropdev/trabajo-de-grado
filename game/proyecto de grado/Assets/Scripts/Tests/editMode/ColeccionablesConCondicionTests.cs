using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Core.Datos;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La mayoria de los coleccionables del mapa no estan ahi desde que se entra: aparecen a cierta hora, cierto dia
    /// o despues de hacer algo (ronda 8: «apenas entrar al cuarto ya puedo conseguirlos»). Mientras no aparecen, la
    /// zona enseña una pista. Y lo que se trae del 3D tiene sus propias entradas del diario, sin pisar las del mapa.
    /// </summary>
    public class ColeccionablesConCondicionTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static GameSession EnElDia(string nivel) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), nivel, flags, 7);
            s.ElegirMetodologia("kanban", Catalogo().Metodologias["kanban"].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = s.Perfil.Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, s.Perfil.Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            s.ComenzarDia();
            return s;
        }

        /// <summary>Hace correr el reloj hasta 'minuto', atendiendo lo que suene por el camino.</summary>
        private static void HastaLas(GameSession s, int minuto) {
            for (var v = 0; v < 40 && s.MinutoDelDia < minuto; v++) {
                foreach (var a in s.AvanzarReloj(Math.Min(30, minuto - s.MinutoDelDia)).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                if (s.Minijuego != null) s.ResolverMinijuego(Nexus.Core.Minijuegos.PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
            }
        }

        private static IEnumerable<string> PiezasDelMapa() {
            return Catalogo().Niveles.Values.Where(n => n.Mapa != null)
                .SelectMany(n => n.Mapa.Zonas).Where(z => z.Coleccionables != null).SelectMany(z => z.Coleccionables);
        }

        private static Coleccionable Col(string id) { return Catalogo().Coleccionables.First(c => c.Id == id); }

        [Test]
        public void Al_menos_el_80_por_ciento_de_las_piezas_del_mapa_tienen_condicion() {
            var piezas = PiezasDelMapa().ToList();
            var conCondicion = piezas.Count(id => Col(id).TieneCondicion);
            Assert.GreaterOrEqual(conCondicion, (int)Math.Ceiling(piezas.Count * 0.8),
                                  $"{conCondicion} de {piezas.Count} tienen condición");
        }

        [Test]
        public void Hay_piezas_faciles_medias_y_dificiles_y_cada_una_con_pista() {
            var conCondicion = PiezasDelMapa().Select(Col).Where(c => c.TieneCondicion).ToList();
            foreach (var d in DificultadesDeColeccionable.Todas)
                Assert.IsTrue(conCondicion.Any(c => c.Aparicion.Dificultad == d), "falta alguna de dificultad " + d);
            foreach (var c in conCondicion) Assert.IsFalse(string.IsNullOrWhiteSpace(c.Aparicion.Pista), c.Id);
        }

        [Test]
        public void Una_pieza_con_hora_no_esta_antes_ni_se_puede_recoger_y_si_despues() {
            var s = EnElDia("nivel-01");   // el dia 1 empieza a las 08:00 en el escritorio
            Assert.AreEqual("escritorio", s.ZonaActual);
            CollectionAssert.Contains(s.ColeccionablesAqui(), "COL-DEV-08", "la del escritorio esta libre");
            CollectionAssert.DoesNotContain(s.ColeccionablesAqui(), "COL-ADV-02", "la del corcho es por la tarde");
            Assert.IsTrue(s.ColeccionablesPorAparecerAqui().Any(c => c.Id == "COL-ADV-02"), "pero se ve su pista");
            Assert.Throws<InvalidOperationException>(() => s.RecogerColeccionable("COL-ADV-02"));

            HastaLas(s, 840);
            CollectionAssert.Contains(s.ColeccionablesAqui(), "COL-ADV-02", "a las 14:00 ya esta");
            s.RecogerColeccionable("COL-ADV-02");
            Assert.IsFalse(s.ColeccionablesPorAparecerAqui().Any(c => c.Id == "COL-ADV-02"));
        }

        [Test]
        public void Una_pieza_por_accion_no_sale_sin_haberla_hecho() {
            var s = EnElDia("nivel-01");
            Assert.IsFalse(s.HaAparecido("COL-ADV-07"), "sin aceptar ningun cambio del cliente");
            s.R.CambiosAceptados = 1;
            Assert.IsTrue(s.HaAparecido("COL-ADV-07"));
        }

        [Test]
        public void Lo_del_3D_va_a_entradas_propias_del_diario_que_no_estan_en_el_mapa() {
            var enElMapa = new HashSet<string>(PiezasDelMapa());
            var del3D = Catalogo().Objetos3D.Objetos.Where(o => o.Tipo == Nexus.Core.Fase1.TiposDeObjeto3D.Coleccionable).ToList();
            Assert.IsNotEmpty(del3D);
            foreach (var o in del3D) {
                Assert.IsNotNull(Catalogo().Coleccionables.FirstOrDefault(c => c.Id == o.Coleccionable), o.Id + ": sin entrada en el diario");
                Assert.IsFalse(enElMapa.Contains(o.Coleccionable), o.Id + ": su entrada tambien esta en el mapa 2D");
            }
            // Las siete cartas y las seis notas del recorrido.
            Assert.AreEqual(7, Enumerable.Range(10, 7).Count(n => Catalogo().Coleccionables.Any(c => c.Id == "COL-DEV-" + n)));
            Assert.AreEqual(6, Catalogo().Coleccionables.Count(c => c.Serie == SeriesDeColeccionables.Nota));
            Assert.AreEqual("Diagramas que salvan vidas", Col("COL-DEV-14").Titulo, "con el mismo nombre que en el 3D");
        }

        [Test]
        public void Una_condicion_mal_escrita_no_pasa_el_validador() {
            var mala = new Coleccionable {
                Id = "COL-ADV-99", Serie = SeriesDeColeccionables.Advertencia, Titulo = "x", Texto = "x",
                Aparicion = new AparicionDeColeccionable { Dificultad = "imposible", Precondiciones = { "variableQueNoExiste >= 1" } }
            };
            var errores = SchemaValidator.ValidarColeccionables(new List<Coleccionable> { mala });
            Assert.IsTrue(errores.Any(e => e.Contains("dificultad")), "dificultad invalida");
            Assert.IsTrue(errores.Any(e => e.Contains("pista")), "sin pista");
            Assert.IsTrue(errores.Any(e => e.Contains("variableQueNoExiste")), "variable que no existe");
        }
    }
}
