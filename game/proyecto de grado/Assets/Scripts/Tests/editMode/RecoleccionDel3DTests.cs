using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Fase1;
using Nexus.Core.Guardado;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// Lo que se recoge en el mundo 3D llega al motor: las hojas de vida son el equipo (en N1, solo ellas: sin
    /// ninguna, trabajas solo), los suministros se guardan para la Fase 2, las cartas van al diario y el certificado
    /// cambia el proyecto. Y los suministros se gastan con sus reglas: la pizza cada tres dias, el cafe una vez al dia
    /// y la Guepardex rebota si se abusa de ella.
    /// </summary>
    public class RecoleccionDel3DTests {
        private const string Pizza = "consumable_pizza", Cafe = "consumable_coffee", Guepardex = "consumable_energy_drink";
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static GameSession Nueva(string nivel) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            return new GameSession(Catalogo(), nivel, flags, 7);
        }

        /// <summary>Lo que devolveria el 3D: el inventario traducido, con la mitad del tiempo usada.</summary>
        private static ResultadoDeRecoleccion Del3D(GameSession s, Dictionary<string, int> inventario) {
            return TraductorDeRecoleccion3D.Traducir(s.EntradaDeRecoleccion(), Catalogo().Objetos3D, inventario, 0.5, false);
        }

        private static void HastaElDia(GameSession s) {
            s.ElegirMetodologia("kanban", Catalogo().Metodologias["kanban"].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = s.Perfil.Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, s.Perfil.Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            s.ComenzarDia();
        }

        private static GameSession EnElDiaCon(Dictionary<string, int> inventario) {
            var s = Nueva("nivel-01");
            s.AplicarRecoleccion(Del3D(s, inventario));
            HastaElDia(s);
            return s;
        }

        private static void PasarDias(GameSession s, int dias) {
            for (var i = 0; i < dias; i++) {
                for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                    foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                    if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                    if (s.Minijuego != null) s.ResolverMinijuego(Nexus.Core.Minijuegos.PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
                }
                s.CerrarJornada();
                s.TerminarDia(false);
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
            }
        }

        // ---------------------------------------------------------------- el catalogo y la traduccion

        [Test]
        public void Cada_hoja_de_vida_trae_a_alguien_con_rol_descripcion_y_habilidades() {
            var cvs = Catalogo().Objetos3D.Objetos.Where(o => o.Tipo == TiposDeObjeto3D.Candidato).ToList();
            Assert.AreEqual(7, cvs.Count);
            foreach (var o in cvs) {
                Assert.IsFalse(string.IsNullOrWhiteSpace(o.Miembro.Rol), o.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(o.Miembro.Descripcion), o.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(o.Miembro.Frase), o.Id);
                Assert.IsTrue(o.Miembro.Habilidades.Values.Any(v => v > 1.05), o.Id + ": algo se le da mejor");
            }
        }

        [Test]
        public void El_traductor_cuenta_el_tiempo_y_deja_fuera_lo_que_no_esta_en_el_catalogo() {
            var s = Nueva("nivel-01");
            var descartados = new List<string>();
            var r = TraductorDeRecoleccion3D.Traducir(s.EntradaDeRecoleccion(), Catalogo().Objetos3D,
                new Dictionary<string, int> { { "cv_marta", 1 }, { Pizza, 2 }, { "objeto_inventado", 3 }, { Cafe, 0 } },
                0.25, true, descartados);
            Assert.AreEqual(30, r.MinutosUsados, "un cuarto de 120 minutos");
            Assert.IsTrue(r.AbandonoAntes);
            CollectionAssert.AreEquivalent(new[] { "cv_marta", Pizza }, r.Objetos.Keys);
            CollectionAssert.AreEqual(new[] { "objeto_inventado" }, descartados);
        }

        [Test]
        public void El_motor_rechaza_un_objeto_que_el_3D_no_podia_dar() {
            var s = Nueva("nivel-01");
            var r = new ResultadoDeRecoleccion { NivelId = "nivel-01" };
            r.Objetos["objeto_inventado"] = 1;
            Assert.Throws<InvalidOperationException>(() => s.AplicarRecoleccion(r));
            Assert.IsFalse(s.RecoleccionHecha);
        }

        [Test]
        public void La_simulacion_tambien_trae_objetos_y_siempre_los_mismos() {
            var a = SimuladorDeRecoleccion.Simular(Nueva("nivel-01").EntradaDeRecoleccion(), IntensidadesDeSimulacion.AFondo);
            var b = SimuladorDeRecoleccion.Simular(Nueva("nivel-01").EntradaDeRecoleccion(), IntensidadesDeSimulacion.AFondo);
            Assert.IsNotEmpty(a.Objetos);
            CollectionAssert.AreEquivalent(a.Objetos, b.Objetos);
        }

        // ---------------------------------------------------------------- el equipo sale de las hojas de vida

        [Test]
        public void El_equipo_son_las_hojas_de_vida_recogidas_y_nada_mas() {
            var s = EnElDiaCon(new Dictionary<string, int> { { "cv_kenji", 1 }, { "cv_marta", 2 }, { "cv_elena", 1 } });
            // En el orden del catalogo, y una hoja de vida repetida no duplica a nadie.
            CollectionAssert.AreEqual(new[] { "marta", "elena", "kenji" }, s.EquipoDelProyecto().Select(m => m.Id).ToArray());
            Assert.AreEqual(3, s.Perfil.EquipoInicial);
            var kenji = s.EquipoDelProyecto().First(m => m.Id == "kenji");
            Assert.Greater(kenji.HabilidadPara("logica"), 1.3);
            Assert.IsNotNull(s.DatosDeMiembro("elena"));
            Assert.IsFalse(s.Tablero.Tarjetas.Any(c => c.Asignado != null && !new[] { "marta", "elena", "kenji" }.Contains(c.Asignado)));
            // El catalogo no se toca: el nivel sigue teniendo su equipo para la siguiente partida.
            Assert.AreEqual("valeria", Catalogo().Niveles["nivel-01"].Equipo[0].Id);
        }

        [Test]
        public void Sin_ninguna_hoja_de_vida_trabajas_tu_solo() {
            var s = EnElDiaCon(new Dictionary<string, int> { { Pizza, 1 } });
            var equipo = s.EquipoDelProyecto();
            Assert.AreEqual(1, equipo.Count);
            Assert.AreEqual("tu", equipo[0].Id);
            Assert.Less(equipo[0].HabilidadPara("logica"), 1.0);
        }

        [Test]
        public void Lo_que_trae_cada_persona_al_llegar_se_nota_al_cerrar_la_fase_1() {
            Func<Dictionary<string, int>, double> moralAlCerrar = inventario => {
                var s = Nueva("nivel-01");
                s.AplicarRecoleccion(Del3D(s, inventario));
                s.ElegirMetodologia("kanban", Catalogo().Metodologias["kanban"].TextosDeRazones.Keys.First());
                s.RepartirCalidad(new Dictionary<string, int>());
                var arq = s.Perfil.Fase1.Arquitecturas[0];
                s.ElegirArquitectura(arq.Id, s.Perfil.Fase1.RazonesDe(arq)[0].Id);
                s.CerrarFase1();
                return s.W.MoralEquipo;
            };
            var conOscar = moralAlCerrar(new Dictionary<string, int> { { "cv_oscar", 1 }, { "cv_marta", 1 } });
            var sinOscar = moralAlCerrar(new Dictionary<string, int> { { "cv_marta", 1 } });
            Assert.AreEqual(sinOscar + 6, conOscar, 0.001, "Óscar trae +6 de moral al llegar, una vez");
        }

        // ---------------------------------------------------------------- el tamaño del equipo

        private static readonly Dictionary<string, int> SeisHojas = new Dictionary<string, int> {
            { "cv_marta", 1 }, { "cv_dereck", 1 }, { "cv_elena", 1 }, { "cv_javier", 1 }, { "cv_kenji", 1 }, { "cv_oscar", 1 }
        };

        [Test]
        public void Cada_proyecto_tiene_un_limite_de_personas_y_sin_elegir_entran_las_primeras() {
            var s = Nueva("nivel-01");
            Assert.AreEqual(4, s.MaxEquipo);
            s.AplicarRecoleccion(Del3D(s, SeisHojas));
            Assert.IsTrue(s.HayQueElegirEquipo);
            Assert.AreEqual(6, s.HojasRecogidas().Count);
            CollectionAssert.AreEqual(new[] { "marta", "derek", "elena", "javier" }, s.EquipoDelProyecto().Select(m => m.Id).ToArray());

            var tutorial = Nueva("nivel-00");
            Assert.AreEqual(2, tutorial.MaxEquipo, "el tutorial es un proyecto pequeño");
        }

        [Test]
        public void El_jugador_elige_quien_entra_hasta_el_limite_y_solo_entre_lo_recogido() {
            var s = Nueva("nivel-01");
            s.AplicarRecoleccion(Del3D(s, SeisHojas));
            Assert.IsNotNull(s.PorQueNoSePuedeElegirEquipo(new[] { "marta", "derek", "elena", "javier", "kenji" }), "no caben cinco");
            Assert.IsNotNull(s.PorQueNoSePuedeElegirEquipo(new[] { "sara" }), "Sara no se recogio");
            s.ElegirEquipo(new[] { "kenji", "oscar" });
            CollectionAssert.AreEqual(new[] { "kenji", "oscar" }, s.EquipoDelProyecto().Select(m => m.Id).ToArray());

            var copia = GameSession.Restaurar(Catalogo(), "nivel-01", s.Flags, 7,
                                              JsonDeGuardado.Deserializar<NivelEnCurso>(JsonDeGuardado.Serializar(s.Capturar())));
            CollectionAssert.AreEqual(new[] { "kenji", "oscar" }, copia.EquipoDelProyecto().Select(m => m.Id).ToArray(), "viaja en el guardado");
        }

        [Test]
        public void Si_caben_todas_no_hay_nada_que_elegir() {
            var s = Nueva("nivel-01");
            s.AplicarRecoleccion(Del3D(s, new Dictionary<string, int> { { "cv_marta", 1 }, { "cv_kenji", 1 } }));
            Assert.IsFalse(s.HayQueElegirEquipo);
            Assert.IsNotNull(s.PorQueNoSePuedeElegirEquipo(new[] { "marta" }));
        }

        [Test]
        public void En_el_tutorial_el_equipo_tambien_sale_de_las_hojas_de_vida() {
            var s = Nueva("nivel-00");
            Assert.IsTrue(s.EquipoDesdeRecoleccion);
            s.AplicarRecoleccion(Del3D(s, new Dictionary<string, int> { { "cv_kenji", 1 }, { "cv_oscar", 1 } }));
            CollectionAssert.AreEqual(new[] { "kenji", "oscar" }, s.EquipoDelProyecto().Select(m => m.Id).ToArray());
        }

        [Test]
        public void Las_cartas_van_al_diario_y_el_certificado_cambia_el_proyecto() {
            var s = Nueva("nivel-01");
            var satisfaccion = s.W.SatisfaccionCliente;
            s.AplicarRecoleccion(Del3D(s, new Dictionary<string, int> { { "DEV_CARD_DIAGRAMS", 1 }, { "usb_fragment", 1 }, { "cert_acreditation", 1 } }));
            // Entradas propias del diario, con el nombre del 3D: no las del mapa 2D.
            CollectionAssert.IsSupersetOf(s.R.ColeccionablesRecogidos, new[] { "COL-DEV-14", "COL-USB-02" });
            Assert.AreEqual(satisfaccion + 3, s.W.SatisfaccionCliente, 0.001);
            Assert.AreEqual(3, s.ObjetosRecogidos().Count);
        }

        [Test]
        public void El_equipo_y_los_suministros_viajan_en_el_guardado() {
            var s = EnElDiaCon(new Dictionary<string, int> { { "cv_zara", 1 }, { "cv_javier", 1 }, { Guepardex, 3 } });
            var json = JsonDeGuardado.Serializar(s.Capturar());
            var copia = GameSession.Restaurar(Catalogo(), "nivel-01", s.Flags, 7, JsonDeGuardado.Deserializar<NivelEnCurso>(json));
            CollectionAssert.AreEqual(s.EquipoDelProyecto().Select(m => m.Id).ToArray(), copia.EquipoDelProyecto().Select(m => m.Id).ToArray());
            Assert.AreEqual(3, copia.R.Suministros[Guepardex]);
            Assert.IsNotNull(copia.DatosDeMiembro("sara"));
        }

        [Test]
        public void El_borrador_de_la_fase_1_no_pierde_el_equipo_recogido() {
            var s = Nueva("nivel-01");
            s.AplicarRecoleccion(Del3D(s, new Dictionary<string, int> { { "cv_dereck", 1 } }));
            var copia = GameSession.Restaurar(Catalogo(), "nivel-01", s.Flags, 7,
                                              JsonDeGuardado.Deserializar<NivelEnCurso>(JsonDeGuardado.Serializar(s.Capturar())));
            CollectionAssert.AreEqual(new[] { "derek" }, copia.EquipoDelProyecto().Select(m => m.Id).ToArray());
        }

        // ---------------------------------------------------------------- los suministros en la Fase 2

        [Test]
        public void La_pizza_anima_a_todos_y_luego_hay_que_esperar_tres_dias() {
            var s = EnElDiaCon(new Dictionary<string, int> { { "cv_marta", 1 }, { "cv_oscar", 1 }, { Pizza, 2 } });
            var moral = s.W.MoralEquipo;
            s.UsarSuministro(Pizza);
            Assert.Greater(s.W.MoralEquipo, moral);
            Assert.AreEqual(1, s.R.Suministros[Pizza]);
            Assert.IsNotNull(s.PorQueNoSePuedeUsarSuministro(Pizza, null), "hoy ya no");
            PasarDias(s, 2);
            Assert.IsNotNull(s.PorQueNoSePuedeUsarSuministro(Pizza, null), "dos dias despues, todavia no");
            PasarDias(s, 1);
            Assert.IsNull(s.PorQueNoSePuedeUsarSuministro(Pizza, null), "a los tres dias, si");
        }

        [Test]
        public void El_cafe_empuja_el_flujo_de_hoy_una_sola_vez() {
            var s = EnElDiaCon(new Dictionary<string, int> { { "cv_marta", 1 }, { Cafe, 2 } });
            var bono = s.Tablero.BonoDeFlujoHoy;
            s.UsarSuministro(Cafe);
            Assert.AreEqual(bono + 0.15, s.Tablero.BonoDeFlujoHoy, 0.0001);
            Assert.AreEqual("Hoy ya lo usaste.", s.PorQueNoSePuedeUsarSuministro(Cafe, null));
            PasarDias(s, 1);
            Assert.IsNull(s.PorQueNoSePuedeUsarSuministro(Cafe, null));
        }

        [Test]
        public void La_guepardex_descansa_a_una_persona_y_si_se_abusa_rebota() {
            var s = EnElDiaCon(new Dictionary<string, int> { { "cv_marta", 1 }, { "cv_kenji", 1 }, { Guepardex, 4 } });
            Assert.AreEqual("Elige a quién dárselo.", s.PorQueNoSePuedeUsarSuministro(Guepardex, null));
            var marta = s.Tablero.Miembro("marta");
            var desvio = marta.DesvioDeCansancio;

            var r1 = s.UsarSuministro(Guepardex, "marta");
            Assert.IsFalse(r1.Reboto);
            Assert.AreEqual(desvio - 12, marta.DesvioDeCansancio, 0.001);
            PasarDias(s, 1);
            Assert.IsFalse(s.UsarSuministro(Guepardex, "marta").Reboto);

            // La tercera en tres dias: hace lo contrario.
            Assert.IsTrue(s.Rebotaria(Guepardex, "marta"));
            Assert.IsFalse(s.Rebotaria(Guepardex, "kenji"), "a otra persona no le pasa nada");
            var antes = marta.DesvioDeCansancio;
            var r3 = s.UsarSuministro(Guepardex, "marta");
            Assert.IsTrue(r3.Reboto);
            Assert.AreEqual(antes + 10, marta.DesvioDeCansancio, 0.001);

            // Pasados los dias de abuso, vuelve a funcionar.
            PasarDias(s, 3);
            Assert.IsFalse(s.Rebotaria(Guepardex, "marta"));
        }

        [Test]
        public void Los_suministros_solo_se_usan_en_el_desarrollo_y_si_quedan() {
            var s = Nueva("nivel-01");
            s.AplicarRecoleccion(Del3D(s, new Dictionary<string, int> { { "cv_marta", 1 }, { Cafe, 1 } }));
            Assert.IsNotNull(s.PorQueNoSePuedeUsarSuministro(Cafe, null), "en la Fase 1 no");
            HastaElDia(s);
            s.UsarSuministro(Cafe);
            PasarDias(s, 1);
            Assert.AreEqual("No te queda ninguno.", s.PorQueNoSePuedeUsarSuministro(Cafe, null));
            Assert.IsEmpty(s.FichasDeSuministros(), "lo que se acabo no sale en el panel");
            Assert.Throws<InvalidOperationException>(() => s.UsarSuministro(Pizza));
        }

        [Test]
        public void Usar_un_suministro_cuesta_tiempo() {
            var s = EnElDiaCon(new Dictionary<string, int> { { "cv_marta", 1 }, { Pizza, 1 } });
            var minuto = s.MinutoDelDia;
            s.UsarSuministro(Pizza);
            Assert.AreEqual(minuto + 10, s.MinutoDelDia);
        }
    }
}
