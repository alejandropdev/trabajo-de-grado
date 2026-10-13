using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Narrativa;
using Nexus.Core.Proyecto;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El product backlog inicial (ronda 6). En un proyecto que empieza de cero el jugador elige las historias y las
    /// ordena; las tarjetas salen de ahi y su suma sigue siendo el alcance. Quien no lo arma tiene el tablero de siempre.
    /// </summary>
    public class BacklogInicialTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static GameSession Abierta(string nivel) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), nivel, flags, 5);
            s.ElegirMetodologia("scrum", Catalogo().Metodologias["scrum"].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = Catalogo().Niveles[nivel].Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, Catalogo().Niveles[nivel].Fase1.RazonesDe(arq)[0].Id);
            return s;
        }

        private static List<string> Pedidas(GameSession s) {
            return s.Perfil.Proyecto.Historias.Where(h => h.Tipo == TiposDeHistoria.Pedida).OrderByDescending(h => h.Valor).Select(h => h.Id).ToList();
        }

        [Test]
        public void Solo_se_crea_el_backlog_en_el_proyecto_que_empieza_de_cero() {
            Assert.IsTrue(Abierta("nivel-00").SeCreaElBacklog);
            var heredado = Abierta("nivel-01");
            Assert.IsFalse(heredado.SeCreaElBacklog, "el nivel 1 ya trae trabajo hecho");
            Assert.Throws<InvalidOperationException>(() => heredado.CrearBacklog(new[] { "lo-que-sea" }));
        }

        [Test]
        public void Las_historias_del_nivel_estan_bien_escritas() {
            foreach (var nivel in Catalogo().Niveles.Values) {
                var f = nivel.Proyecto;
                if (f.BacklogInicial != BacklogsIniciales.Crear) { Assert.IsEmpty(f.Historias, nivel.Id); continue; }
                CollectionAssert.AllItemsAreUnique(f.Historias.Select(h => h.Id).ToList(), nivel.Id);
                foreach (var h in f.Historias) {
                    var donde = nivel.Id + "/" + h.Id;
                    Assert.IsNotNull(f.Modulo(h.Modulo), donde + ": módulo «" + h.Modulo + "»");
                    CollectionAssert.Contains(TiposDeHistoria.Todos, h.Tipo, donde);
                    Assert.That(h.Valor, Is.InRange(1, 3), donde);
                    foreach (var texto in new[] { h.Titulo, h.Como, h.Quiero, h.Para, h.Porque })
                        Assert.IsFalse(string.IsNullOrWhiteSpace(texto), donde + ": le falta un texto");
                    Assert.LessOrEqual(h.Titulo.Length, 48, donde + ": el título no cabe en la tarjeta");
                }
                foreach (var m in f.Modulos)
                    Assert.IsTrue(f.Historias.Any(h => h.Modulo == m.Id && h.Tipo == TiposDeHistoria.Pedida), $"{nivel.Id}/{m.Id}: ninguna historia pedida");
                Assert.GreaterOrEqual(f.Historias.Count(h => h.Tipo != TiposDeHistoria.Pedida), 3, nivel.Id + ": sin trampas no hay nada que decidir");
                foreach (var tipo in TiposDeHistoria.Todos)
                    Assert.IsTrue(f.Historias.Any(h => h.Tipo == tipo), $"{nivel.Id}: ninguna historia de tipo «{tipo}»");
            }
        }

        [Test]
        public void Sin_armar_el_backlog_el_tablero_es_el_de_siempre() {
            var s = Abierta("nivel-00");
            s.CerrarFase1();
            Assert.IsNull(s.BacklogCreado);
            Assert.IsTrue(s.Tablero.Tarjetas.All(c => c.Historia == null));
            Assert.AreEqual(s.W.Alcance, s.Tablero.PuntosDeAlcance, 1e-6);
            Assert.AreEqual(s.Perfil.AlcanceInicial, s.W.Alcance, 1e-9);
        }

        [Test]
        public void Las_tarjetas_son_las_historias_elegidas_en_el_orden_del_jugador_y_suman_el_alcance() {
            var s = Abierta("nivel-00");
            var orden = Pedidas(s);
            orden.Reverse();
            s.CrearBacklog(orden);
            s.CerrarFase1();

            var tarjetas = s.Tablero.Tarjetas.OrderBy(c => c.Orden).ToList();
            CollectionAssert.AreEqual(orden, tarjetas.Select(c => c.Historia).ToList(), "el orden del tablero es el del jugador");
            Assert.AreEqual(s.W.Alcance, s.Tablero.PuntosDeAlcance, 1e-6, "la suma sigue siendo el alcance");
            Assert.IsTrue(tarjetas.All(c => c.Puntos > 0 && !c.DescubiertaTarde));
            Assert.AreEqual(s.Perfil.AlcanceInicial, s.W.Alcance, 1e-9, "elegir bien no cambia el tamaño del trabajo");
            Assert.AreEqual(0, s.BacklogCreado.Dejadas);
            Assert.AreEqual(0, s.BacklogCreado.DeMas);
            Assert.AreEqual("bien", s.BacklogCreado.Renglones[0].Tipo);
        }

        [Test]
        public void Los_puntos_de_cada_modulo_no_cambian_al_repartirlos_entre_sus_historias() {
            var sin = Abierta("nivel-00");
            sin.CerrarFase1();
            var con = Abierta("nivel-00");
            con.CrearBacklog(Pedidas(con));
            con.CerrarFase1();
            foreach (var m in con.Perfil.Proyecto.Modulos)
                Assert.AreEqual(sin.Tablero.Tarjetas.Where(c => c.Modulo == m.Id).Sum(c => c.Puntos),
                                con.Tablero.Tarjetas.Where(c => c.Modulo == m.Id).Sum(c => c.Puntos), 1e-6, m.Id);
        }

        [Test]
        public void Lo_que_el_cliente_pidio_y_se_dejo_fuera_aparece_al_final_y_cuesta_satisfaccion() {
            var s = Abierta("nivel-00");
            var pedidas = Pedidas(s);
            var fuera = pedidas[0];
            var satisfaccion = s.W.SatisfaccionCliente;
            s.CrearBacklog(pedidas.Skip(1).ToList());
            s.CerrarFase1();

            Assert.AreEqual(1, s.BacklogCreado.Dejadas);
            Assert.Less(s.W.SatisfaccionCliente, satisfaccion);
            var tarde = s.Tablero.Tarjetas.Single(c => c.Historia == fuera);
            Assert.IsTrue(tarde.DescubiertaTarde);
            Assert.AreEqual(s.Tablero.Tarjetas.Max(c => c.Orden), tarde.Orden, "va la última");
            StringAssert.StartsWith("Se descubrió tarde", tarde.Titulo);
            Assert.AreEqual(s.W.Alcance, s.Tablero.PuntosDeAlcance, 1e-6);
            Assert.IsTrue(s.BacklogCreado.Renglones.Any(r => r.Tipo == "falta"));
        }

        [Test]
        public void Lo_que_nadie_pidio_es_trabajo_de_mas() {
            var s = Abierta("nivel-00");
            var deMas = s.Perfil.Proyecto.Historias.First(h => h.Tipo == TiposDeHistoria.NadieLaPidio).Id;
            var ids = Pedidas(s);
            ids.Add(deMas);
            s.CrearBacklog(ids);
            s.CerrarFase1();

            Assert.AreEqual(1, s.BacklogCreado.DeMas);
            Assert.Greater(s.W.Alcance, s.Perfil.AlcanceInicial);
            Assert.AreEqual(s.W.Alcance, s.Tablero.PuntosDeAlcance, 1e-6, "el tablero crece con el alcance");
            Assert.IsTrue(s.Tablero.Tarjetas.Any(c => c.Historia == deMas));
            Assert.IsTrue(s.BacklogCreado.Renglones.Any(r => r.Tipo == "sobra"));
        }

        [Test]
        public void El_backlog_armado_viaja_en_el_guardado_y_el_nivel_se_juega_entero() {
            var s = Abierta("nivel-00");
            s.CrearBacklog(Pedidas(s));
            s.CerrarFase1();
            s.ComenzarDia();
            var copia = GameSession.Restaurar(Catalogo(), "nivel-00", s.Flags, 5, s.Capturar());
            CollectionAssert.AreEqual(s.Tablero.Tarjetas.Select(c => c.Historia).ToList(), copia.Tablero.Tarjetas.Select(c => c.Historia).ToList());

            // Hasta el final, sin que el trabajo de las tarjetas deje de cuadrar con el avance.
            var primero = true;
            while (s.R.Fase == 2) {
                if (!primero) s.ComenzarDia();
                primero = false;
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
                for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                    foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                    if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                    if (s.Minijuego != null) s.ResolverMinijuego(Nexus.Core.Minijuegos.PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
                }
                s.CerrarJornada();
                s.TerminarDia(false);
                var t = s.R.Tablero;
                Assert.AreEqual(Math.Max(0, Math.Min(s.W.Avance, t.PuntosDeAlcance)), t.TrabajoDeAlcance - t.TrabajoAdeudado, 1e-4, "día " + s.R.DiaActual);
            }
        }
    }
}
