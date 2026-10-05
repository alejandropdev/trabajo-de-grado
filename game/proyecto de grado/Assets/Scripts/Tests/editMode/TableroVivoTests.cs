using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Core.Simulacion;
using Nexus.Core.Tablero;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El tablero del equipo (Core/Tablero). Lo que tiene que cumplir para poder existir sin romper nada:
    /// es una proyeccion del avance del motor (su trabajo suma siempre lo que dice W.Avance), con el equipo en piloto
    /// automatico no cambia ni un decimal de la partida, y no gasta el azar de la sesion.
    /// </summary>
    public class TableroVivoTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static FlagStore Flags() {
            var store = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            store.Inicializar();
            return store;
        }

        private static GameSession Empezar(string nivel, string metodologia, int semilla) {
            var s = new GameSession(Catalogo(), nivel, Flags(), semilla);
            s.ElegirMetodologia(metodologia, Catalogo().Metodologias[metodologia].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            s.ElegirArquitectura(Catalogo().Niveles[nivel].Fase1.Arquitecturas[0].Id,
                                 Catalogo().Niveles[nivel].Fase1.RazonesDe(Catalogo().Niveles[nivel].Fase1.Arquitecturas[0])[0].Id);
            s.CerrarFase1();
            return s;
        }

        /// <summary>Un dia entero sin tocar el tablero: el robot mas simple, que atiende lo que suena con la primera opcion.</summary>
        private static void JugarUnDia(GameSession s, bool horasExtra = false) {
            s.ComenzarDia();
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
            for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                if (s.Minijuego != null) s.ResolverMinijuego(Nexus.Core.Minijuegos.PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
                Invariante(s);
            }
            s.CerrarJornada();
            Invariante(s);
            s.TerminarDia(horasExtra);
            Invariante(s);
        }

        private static void Invariante(GameSession s) {
            var t = s.R.Tablero;
            var esperado = Math.Max(0, Math.Min(s.W.Avance + t.ProvisionalHoy, t.PuntosDeAlcance));
            Assert.AreEqual(esperado, t.TrabajoDeAlcance - t.TrabajoAdeudado, 1e-4,
                            $"día {s.R.DiaActual}, {s.HoraActual}: el trabajo de las tarjetas no cuadra con el avance");
            foreach (var c in t.Tarjetas) Assert.That(c.Trabajo, Is.InRange(-1e-6, c.Puntos + 1e-6), c.Id);
        }

        // ================================================================ generar

        [TestCase("nivel-00")]
        [TestCase("nivel-01")]
        public void Las_tarjetas_suman_el_alcance_y_salen_de_los_modulos_del_proyecto(string nivel) {
            var s = Empezar(nivel, "scrum", 3);
            var t = s.R.Tablero;
            Assert.IsNotNull(t, "el tablero existe desde que se cierra la Fase 1");
            Assert.AreEqual(s.W.Alcance, t.PuntosDeAlcance, 0.01);
            Assert.That(t.Tarjetas.Count, Is.InRange(4, 30), "ni una sola tarjeta enorme ni un tablero ilegible");
            var modulos = new HashSet<string>(s.Perfil.Proyecto.Modulos.Select(m => m.Id));
            foreach (var c in t.Tarjetas) {
                Assert.IsTrue(modulos.Contains(c.Modulo), c.Id + " no es de ningún módulo: " + c.Modulo);
                Assert.Greater(c.Puntos, 0.49, c.Id);
            }
            CollectionAssert.AllItemsAreUnique(t.Tarjetas.Select(c => c.Id));
            Assert.GreaterOrEqual(t.Miembros.Count, 2, "hay equipo a quien asignar");
        }

        [Test]
        public void La_misma_semilla_genera_el_mismo_tablero_y_otra_semilla_otro() {
            Func<int, string> huella = semilla => string.Join("|", Empezar("nivel-01", "kanban", semilla).R.Tablero.Tarjetas.Select(c => c.Id + ":" + c.Puntos + ":" + c.Tipo));
            Assert.AreEqual(huella(11), huella(11));
            Assert.AreNotEqual(huella(11), huella(12));
        }

        [Test]
        public void El_tablero_no_gasta_el_azar_de_la_sesion() {
            // Si generar o mover el tablero tirara del generador de la sesion, todas las partidas calibradas cambiarian.
            var s = Empezar("nivel-01", "scrum", 9);
            var antes = s.Capturar().ConsumosDelRng;
            MotorDelTablero.SincronizarAlcance(s.R.Tablero, s.W.Alcance + 7, 9, 1);
            MotorDelTablero.Conciliar(s.R.Tablero, 5, 1);
            MotorDelTablero.AvanzarHasta(s.R.Tablero, 120, 3, 600);
            Assert.AreEqual(antes, s.Capturar().ConsumosDelRng);
        }

        // ================================================================ proyeccion del avance

        [TestCase("nivel-00", "scrum", 1)]
        [TestCase("nivel-00", "cascada", 7)]
        [TestCase("nivel-01", "kanban", 1)]
        [TestCase("nivel-01", "scrum", 4417)]
        public void El_invariante_de_trabajo_se_mantiene_toda_la_partida(string nivel, string metodologia, int semilla) {
            var s = Empezar(nivel, metodologia, semilla);
            Invariante(s);
            var dia = 0;
            while (s.R.Fase == 2) JugarUnDia(s, dia++ % 3 == 2);
            Assert.AreEqual(0, s.R.Tablero.ProvisionalHoy, 1e-9, "al cerrar el día no queda nada provisional");
        }

        [TestCase("nivel-00", 1)]
        [TestCase("nivel-01", 7)]
        public void En_piloto_automatico_el_factor_es_exactamente_uno_y_la_partida_es_la_de_siempre(string nivel, int semilla) {
            // Lo mismo, dia a dia, que el modelo puro sin tablero: es el criterio que deja intacta la calibracion.
            var s = Empezar(nivel, "scrum", semilla);
            while (s.R.Fase == 2) {
                s.ComenzarDia();
                if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
                if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
                for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                    foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                    if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                    if (s.Minijuego != null) s.ResolverMinijuego(Nexus.Core.Minijuegos.PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
                }
                s.CerrarJornada();
                Assert.AreEqual(1.0, MotorDelTablero.FactorDelDia(s.R.Tablero), "nadie tocó el tablero");

                var esperado = s.W.Clone();
                ForresterModel.AvanzarUnDia(esperado, s.R, s.Coef, s.Perfil.VelocidadBase, 1.0, false);
                var velocidadMult = (esperado.Avance - s.W.Avance);
                s.TerminarDia(false);
                Assert.AreEqual(esperado.DeudaTecnica, s.W.DeudaTecnica, 1e-9);
                Assert.AreEqual(esperado.Cansancio, s.W.Cansancio, 1e-9);
                Assert.AreEqual(esperado.MoralEquipo, s.W.MoralEquipo, 1e-9);
                Assert.GreaterOrEqual(velocidadMult, 0);
            }
        }

        [Test]
        public void Durante_el_dia_el_tablero_anticipa_el_avance_y_W_no_se_mueve() {
            var s = Empezar("nivel-01", "kanban", 5);
            s.ComenzarDia();
            var avance = s.W.Avance;
            s.AvanzarReloj(240);
            Assert.AreEqual(avance, s.W.Avance, 1e-9, "el avance de verdad solo se mueve al cerrar el día");
            Assert.Greater(s.R.Tablero.ProvisionalHoy, 0, "pero el equipo ya lleva media jornada trabajando");
            Assert.IsTrue(s.R.Tablero.Tarjetas.Any(c => c.Columna == ColumnasDeBase.Haciendo), "y se ve en las tarjetas");
            Assert.IsTrue(s.R.Tablero.Tarjetas.Where(c => c.Columna == ColumnasDeBase.Haciendo).All(c => c.Asignado != null), "cada una con quien la lleva");
        }

        // ================================================================ el motor, suelto

        private static TableroDelEquipo Tablero(params double[] puntos) {
            var t = new TableroDelEquipo();
            t.Miembros.Add(new MiembroDelEquipo { Id = "a", Nombre = "A", Habilidades = { { "datos", 1.3 }, { "interfaz", 0.7 } } });
            t.Miembros.Add(new MiembroDelEquipo { Id = "b", Nombre = "B" });
            for (var i = 0; i < puntos.Length; i++)
                t.Tarjetas.Add(new TarjetaDeTrabajo { Id = "T" + i, Puntos = puntos[i], Orden = i, Tipo = "datos" });
            return t;
        }

        [Test]
        public void El_trabajo_se_reparte_entre_tantas_tarjetas_como_companeros_y_en_orden() {
            var t = Tablero(2, 2, 3, 5);
            MotorDelTablero.Conciliar(t, 3, 1);
            Assert.AreEqual(1.5, t.Tarjetas[0].Trabajo, 1e-6);
            Assert.AreEqual(1.5, t.Tarjetas[1].Trabajo, 1e-6);
            Assert.AreEqual(0, t.Tarjetas[2].Trabajo, 1e-6, "la tercera espera: solo hay dos personas");
            MotorDelTablero.Conciliar(t, 5, 2);
            Assert.AreEqual(ColumnasDeBase.Hecho, t.Tarjetas[0].Columna);
            Assert.AreEqual(2, t.Tarjetas[0].DiaFin);
            Assert.AreEqual(1, t.Tarjetas[2].Trabajo + t.Tarjetas[3].Trabajo, 1e-6, "lo que sobra pasa a las siguientes");
        }

        [Test]
        public void Conciliar_dos_veces_con_lo_mismo_no_cambia_nada() {
            var t = Tablero(2, 3, 5);
            MotorDelTablero.Conciliar(t, 4.2, 1);
            var antes = string.Join("|", t.Tarjetas.Select(c => c.Trabajo + c.Columna + c.Asignado));
            MotorDelTablero.Conciliar(t, 4.2, 1);
            Assert.AreEqual(antes, string.Join("|", t.Tarjetas.Select(c => c.Trabajo + c.Columna + c.Asignado)));
        }

        [Test]
        public void Un_efecto_negativo_de_avance_quita_de_lo_que_esta_a_medias_y_no_de_lo_hecho() {
            var t = Tablero(2, 2, 4);
            MotorDelTablero.Conciliar(t, 5, 1);   // T0 y T1 hechas, T2 con 1
            MotorDelTablero.Conciliar(t, 3, 2);   // un evento obliga a rehacer 2 puntos
            Assert.AreEqual(2, t.Tarjetas[0].Trabajo, 1e-6, "lo terminado no se deshace");
            Assert.AreEqual(2, t.Tarjetas[1].Trabajo, 1e-6);
            Assert.AreEqual(0, t.Tarjetas[2].Trabajo, 1e-6, "se pierde lo que estaba a medias");
            Assert.AreEqual(1, t.TrabajoAdeudado, 1e-6, "y el resto se debe");

            MotorDelTablero.Conciliar(t, 4, 3);   // el siguiente punto de avance paga la deuda antes de avanzar
            Assert.AreEqual(0, t.TrabajoAdeudado, 1e-6);
            Assert.AreEqual(0, t.Tarjetas[2].Trabajo, 1e-6);
        }

        [Test]
        public void El_alcance_que_crece_trae_tarjetas_nuevas_y_el_que_baja_quita_las_que_no_se_empezaron() {
            var t = Tablero(2, 3, 5);
            MotorDelTablero.Conciliar(t, 2.5, 1);
            MotorDelTablero.SincronizarAlcance(t, 14, 1, 3);
            Assert.AreEqual(14, t.PuntosDeAlcance, 0.01);
            Assert.IsTrue(t.Tarjetas.Any(c => c.DiaEntrada == 3 && c.Modulo == null), "el cambio pedido entra como tarjeta suelta");

            MotorDelTablero.SincronizarAlcance(t, 9, 1, 4);
            Assert.AreEqual(9, t.PuntosDeAlcance, 0.01);
            Assert.AreEqual(2.5, t.TrabajoDeAlcance, 1e-6, "lo empezado no se toca");
        }

        [Test]
        public void Asignar_a_quien_se_le_da_bien_sube_el_flujo_y_a_quien_no_lo_baja_dentro_de_la_banda() {
            var t = Tablero(3, 3, 3);
            MotorDelTablero.Conciliar(t, 2, 1);
            Assert.AreEqual(1.0, MotorDelTablero.FactorInstantaneo(t), "piloto automático");

            foreach (var c in t.Tarjetas.Where(x => x.Columna == ColumnasDeBase.Haciendo)) { c.Asignado = "a"; c.AsignacionManual = true; }
            Assert.That(MotorDelTablero.FactorInstantaneo(t), Is.GreaterThan(1.0).And.LessThanOrEqualTo(MotorDelTablero.FactorMaximo));

            foreach (var c in t.Tarjetas) c.Tipo = "interfaz";
            Assert.That(MotorDelTablero.FactorInstantaneo(t), Is.LessThan(1.0).And.GreaterThanOrEqualTo(MotorDelTablero.FactorMinimo));

            MotorDelTablero.AvanzarHasta(t, 300, 3, 600);
            Assert.That(MotorDelTablero.FactorDelDia(t), Is.LessThan(1.0));
            MotorDelTablero.CerrarElDia(t, 2, 1);
            Assert.AreEqual(1.0, MotorDelTablero.FactorDelDia(t), "cada día se mide de nuevo");
        }

        // ================================================================ guardado

        [Test]
        public void El_tablero_viaja_en_el_guardado() {
            var s = Empezar("nivel-01", "scrum", 4);
            JugarUnDia(s);
            s.ComenzarDia();
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            s.AvanzarReloj(200);

            var json = Nexus.Core.Guardado.JsonDeGuardado.Serializar(s.Capturar());
            var nivel = Nexus.Core.Guardado.JsonDeGuardado.Deserializar<Nexus.Core.Guardado.NivelEnCurso>(json);
            var r = GameSession.Restaurar(Catalogo(), "nivel-01", Flags(), 4, nivel);

            Assert.AreEqual(s.R.Tablero.ProvisionalHoy, r.R.Tablero.ProvisionalHoy, 1e-9);
            CollectionAssert.AreEqual(s.R.Tablero.Tarjetas.Select(c => c.Id + c.Trabajo + c.Columna + c.Asignado),
                                      r.R.Tablero.Tarjetas.Select(c => c.Id + c.Trabajo + c.Columna + c.Asignado));
            Assert.AreEqual(s.R.Tablero.Miembros[0].HabilidadPara("interfaz"), r.R.Tablero.Miembros[0].HabilidadPara("INTERFAZ"));
        }

        [Test]
        public void Un_guardado_sin_tablero_lo_reconstruye_ya_avanzado() {
            var s = Empezar("nivel-01", "scrum", 4);
            JugarUnDia(s);
            JugarUnDia(s);
            var nivel = s.Capturar();
            nivel.R.Tablero = null;   // como un guardado de antes de que existiera

            var r = GameSession.Restaurar(Catalogo(), "nivel-01", Flags(), 4, nivel);
            Assert.IsNotNull(r.R.Tablero);
            Assert.AreEqual(r.W.Alcance, r.R.Tablero.PuntosDeAlcance, 0.01);
            Assert.AreEqual(Math.Min(r.W.Avance, r.W.Alcance), r.R.Tablero.TrabajoDeAlcance, 1e-4, "aparece con el trabajo que ya estaba hecho");
        }

        [Test]
        public void Cada_nivel_trae_su_equipo_con_habilidades_distintas() {
            foreach (var nivel in Catalogo().Niveles.Values) {
                Assert.GreaterOrEqual(nivel.Equipo.Count, 2, nivel.Id);
                foreach (var m in nivel.Equipo) Assert.IsNotEmpty(m.Habilidades, nivel.Id + "/" + m.Id);
                Assert.Greater(nivel.Equipo.Select(m => m.Habilidades.OrderByDescending(h => h.Value).First().Key).Distinct().Count(), 1,
                               nivel.Id + ": si a todos se les da bien lo mismo, asignar no es una decisión");
            }
        }
    }
}
