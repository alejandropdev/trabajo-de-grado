using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Despliegue;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La Fase 3 (desplegar) y lo que cambio alrededor de ella: cuando toca segun la metodologia, que se evalua en la
    /// tuberia y en los enchufes de cada modulo, y que su resultado pesa de verdad en el proyecto y en el lanzamiento.
    /// Tambien el cierre del dia con horas extra, que ahora se simula cuando el dia acaba, y el backlog guiado.
    /// </summary>
    public class DespliegueTests {
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

        private static GameSession Empezar(string nivel, string metodologia, int semilla = 3) {
            var s = new GameSession(Catalogo(), nivel, Flags(), semilla);
            s.ElegirMetodologia(metodologia, Catalogo().Metodologias[metodologia].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = Catalogo().Niveles[nivel].Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, Catalogo().Niveles[nivel].Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            return s;
        }

        private static void EmpezarDia(GameSession s) {
            s.ComenzarDia();
            if (s.PendingPlanning != null) s.Comprometer(s.PendingPlanning.CapacidadSugerida);
            if (s.PendingRetro != null && s.PendingRetro.Acciones.Count > 0) s.ElegirAccionRetro(s.PendingRetro.Acciones[0].Id);
        }

        private static void LlegarAlCierre(GameSession s) {
            for (var v = 0; v < 80 && !s.SePuedeCerrarLaJornada; v++) {
                foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                if (s.Minijuego != null) s.ResolverMinijuego(PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
            }
            if (!s.JornadaLlegoAlCierre) s.CerrarJornada();
        }

        /// <summary>Juega dias enteros (sin desplegar) hasta dejar cerrado el dia pedido.</summary>
        private static void JugarHasta(GameSession s, int dia, Func<GameSession, RespuestaDeDespliegue> alDesplegar = null) {
            while (s.R.DiaActual < dia) {
                EmpezarDia(s);
                LlegarAlCierre(s);
                s.TerminarDia(false);
                if (s.R.DiaActual < dia && s.DesplieguePendiente && alDesplegar != null) s.ResolverDespliegue(alDesplegar(s));
            }
        }

        private static RespuestaDeDespliegue Perfecta(GameSession s) {
            var def = s.Perfil.Despliegue;
            return new RespuestaDeDespliegue {
                Tuberia = EvaluadorDeDespliegue.TuberiaCorrecta(def),
                Enchufes = EvaluadorDeDespliegue.EnchufesCorrectos(def, s.PlanDelDespliegue())
            };
        }

        private static RespuestaDeDespliegue Desastrosa(GameSession s) {
            var def = s.Perfil.Despliegue;
            return new RespuestaDeDespliegue {
                Tuberia = def.Tuberia.Tramos.Where(t => t.Trampa).Select(t => t.Id).ToList(),
                Enchufes = new Dictionary<string, Dictionary<string, string>>()
            };
        }

        // ================================================================ cuando toca

        [Test]
        public void La_metodologia_decide_cada_cuanto_se_despliega_y_el_ultimo_dia_siempre() {
            CollectionAssert.AreEqual(new[] { 10, 20 }, Empezar("nivel-01", "scrum").DiasDeDespliegue(), "Scrum: al cerrar cada sprint");
            CollectionAssert.AreEqual(new[] { 5, 10, 15, 20 }, Empezar("nivel-01", "kanban").DiasDeDespliegue(), "Kanban: con cada revisión del flujo");
            CollectionAssert.AreEqual(new[] { 20 }, Empezar("nivel-01", "cascada").DiasDeDespliegue(), "Cascada: todo junto, al final");
            foreach (var m in Catalogo().Niveles["nivel-00"].MetodologiasPermitidas)
                CollectionAssert.AreEqual(new[] { 5 }, Empezar("nivel-00", m).DiasDeDespliegue(), "nivel-00/" + m + ": cinco días, un despliegue");
        }

        [Test]
        public void El_despliegue_queda_pendiente_al_cerrar_su_dia_y_solo_ese_dia() {
            var s = Empezar("nivel-01", "kanban");
            JugarHasta(s, 4);
            Assert.IsFalse(s.DesplieguePendiente, "el día 4 no despliega");
            EmpezarDia(s);
            Assert.IsFalse(s.DesplieguePendiente, "el día 5 aún no ha terminado");
            LlegarAlCierre(s);
            s.TerminarDia(false);
            Assert.IsTrue(s.DesplieguePendiente, "día 5 cerrado: toca desplegar");
            Assert.AreEqual(2, s.R.Fase, "después de un despliegue intermedio se vuelve al desarrollo");

            s.ResolverDespliegue(Perfecta(s));
            Assert.IsFalse(s.DesplieguePendiente, "ya se desplegó hoy");
            Assert.Throws<InvalidOperationException>(() => s.ResolverDespliegue(Perfecta(s)));
            EmpezarDia(s);
            Assert.AreEqual(6, s.R.DiaActual);
        }

        // ================================================================ la tuberia

        [Test]
        public void La_tuberia_bien_armada_y_todo_enchufado_es_un_despliegue_limpio() {
            foreach (var nivel in Catalogo().Niveles.Values.Where(n => n.Despliegue != null)) {
                var s = Empezar(nivel.Id, nivel.MetodologiasPermitidas[0]);
                JugarHasta(s, nivel.DiasTotales);
                var plan = s.PlanDelDespliegue();
                Assert.IsTrue(plan.EsFinal);
                Assert.IsNotEmpty(plan.Modulos, nivel.Id + ": el último día sale lo que haya");
                var r = EvaluadorDeDespliegue.Evaluar(nivel.Despliegue, nivel.Proyecto, plan, Perfecta(s));
                Assert.AreEqual("bien", r.Nivel, nivel.Id + ": " + string.Join(" | ", r.Renglones.Where(x => x.Estado != "bien").Select(x => x.Nombre + ": " + x.Texto)));
                Assert.Less(r.Riesgo, 0, "hacerlo bien baja el riesgo del lanzamiento");
                Assert.AreEqual(0, r.Defectos);
                CollectionAssert.AreEquivalent(plan.Modulos, r.ModulosEncendidos);
            }
        }

        [Test]
        public void Un_hueco_vacio_no_da_error_la_etapa_no_ocurre_y_lo_que_atrapaba_se_escapa() {
            var nivel = Catalogo().Niveles["nivel-01"];
            var s = Empezar("nivel-01", "cascada");
            JugarHasta(s, nivel.DiasTotales);
            var respuesta = Perfecta(s);
            respuesta.Tuberia.Remove("unitarias");
            var r = EvaluadorDeDespliegue.Evaluar(nivel.Despliegue, nivel.Proyecto, s.PlanDelDespliegue(), respuesta);
            Assert.AreEqual(EstadosDeDespliegue.Falto, r.Renglones.First(x => x.Id == "unitarias").Estado);
            Assert.Greater(r.Defectos, 0, "sin las pruebas, los errores que atrapaban llegan al cliente");
            Assert.Greater(r.Riesgo, 0);
            Assert.AreNotEqual("bien", r.Nivel);
        }

        [Test]
        public void Un_tramo_trampa_o_uno_antes_de_tiempo_cuentan_como_mal() {
            var nivel = Catalogo().Niveles["nivel-00"];
            var s = Empezar("nivel-00", nivel.MetodologiasPermitidas[0]);
            JugarHasta(s, nivel.DiasTotales);
            var plan = s.PlanDelDespliegue();

            var conTrampa = Perfecta(s);
            conTrampa.Tuberia.Insert(0, "directo");
            var r1 = EvaluadorDeDespliegue.Evaluar(nivel.Despliegue, nivel.Proyecto, plan, conTrampa);
            Assert.AreEqual(EstadosDeDespliegue.Mal, r1.Renglones.First(x => x.Id == "directo").Estado);

            var alReves = Perfecta(s);
            alReves.Tuberia.Remove("produccion");
            alReves.Tuberia.Insert(0, "produccion");   // producción antes del ensayo
            var r2 = EvaluadorDeDespliegue.Evaluar(nivel.Despliegue, nivel.Proyecto, plan, alReves);
            Assert.AreEqual(EstadosDeDespliegue.Mal, r2.Renglones.First(x => x.Id == "produccion").Estado);
            Assert.Greater(r2.Riesgo, 0);
        }

        // ================================================================ los modulos

        [Test]
        public void Una_toma_trampa_enciende_mal_un_cable_suelto_no_enciende_y_arrastra_a_quien_depende_de_el() {
            var nivel = Catalogo().Niveles["nivel-00"];
            var s = Empezar("nivel-00", nivel.MetodologiasPermitidas[0]);
            JugarHasta(s, nivel.DiasTotales);
            var plan = s.PlanDelDespliegue();
            CollectionAssert.Contains(plan.Modulos, "registro");
            CollectionAssert.Contains(plan.Modulos, "aviso");

            var trampa = Perfecta(s);
            trampa.Enchufes["registro"]["datos"] = "bd-pruebas";
            var r1 = EvaluadorDeDespliegue.Evaluar(nivel.Despliegue, nivel.Proyecto, plan, trampa);
            Assert.AreEqual(EstadosDeDespliegue.Mal, r1.Renglones.First(x => x.Que == "modulo" && x.Id == "registro").Estado);
            CollectionAssert.Contains(r1.ModulosEncendidos, "registro", "enciende, pero mal");
            Assert.Greater(r1.Defectos, 0);

            var suelto = Perfecta(s);
            suelto.Enchufes["registro"].Remove("clave");
            var r2 = EvaluadorDeDespliegue.Evaluar(nivel.Despliegue, nivel.Proyecto, plan, suelto);
            CollectionAssert.Contains(r2.ModulosCaidos, "registro");
            CollectionAssert.Contains(r2.ModulosCaidos, "aviso", "el aviso necesita el registro: sin él no enciende");
            StringAssert.Contains("Registro de visitas", r2.Renglones.First(x => x.Que == "modulo" && x.Id == "aviso").Texto);
        }

        [Test]
        public void En_un_despliegue_intermedio_solo_salen_los_modulos_que_ya_estan_listos() {
            var s = Empezar("nivel-01", "scrum");
            JugarHasta(s, 10);
            Assert.IsTrue(s.DesplieguePendiente);
            var plan = s.PlanDelDespliegue();
            Assert.IsFalse(plan.EsFinal);
            Assert.IsNotEmpty(plan.NoListos, "a mitad del nivel no puede estar todo listo");
            var umbral = s.Perfil.Despliegue.UmbralDeModulo;
            var avance = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(s.Perfil.Proyecto, s.W.Avance, s.W.Alcance).ToDictionary(p => p.Key.Id, p => p.Value);
            foreach (var m in plan.Modulos) Assert.GreaterOrEqual(avance[m], umbral, m);
            foreach (var m in plan.NoListos) Assert.Less(avance[m], umbral, m);
        }

        // ================================================================ lo que pesa

        [Test]
        public void Resolver_un_despliegue_mueve_las_barras_y_queda_en_el_historial_y_en_el_guardado() {
            var s = Empezar("nivel-01", "kanban");
            JugarHasta(s, 5);
            var satisfaccion = s.W.SatisfaccionCliente;
            var res = s.ResolverDespliegue(Desastrosa(s));
            Assert.AreEqual("mal", res.Nivel);
            Assert.Greater(s.R.RiesgoDeDespliegue, 0);
            Assert.AreEqual(1, s.R.Despliegues.Count);
            Assert.LessOrEqual(s.W.SatisfaccionCliente, satisfaccion);
            Assert.IsTrue(s.Traza.Entradas.Any(e => e.Origen == GameSession.OrigenDeDespliegue), "queda en la traza de decisiones");

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(s.Capturar(), CatalogLoader.Settings);
            var guardado = Newtonsoft.Json.JsonConvert.DeserializeObject<Nexus.Core.Guardado.NivelEnCurso>(json, CatalogLoader.Settings);
            var vuelta = GameSession.Restaurar(Catalogo(), "nivel-01", Flags(), 3, guardado);
            Assert.AreEqual(1, vuelta.R.Despliegues.Count);
            Assert.AreEqual(s.R.RiesgoDeDespliegue, vuelta.R.RiesgoDeDespliegue);
            Assert.IsFalse(vuelta.DesplieguePendiente, "recargar no deja repetir el despliegue");
        }

        [Test]
        public void Desplegar_bien_o_mal_cambia_de_verdad_el_lanzamiento() {
            Func<Func<GameSession, RespuestaDeDespliegue>, GameSession> jugar = como => {
                var s = Empezar("nivel-01", "scrum", 21);
                JugarHasta(s, s.Perfil.DiasTotales, como);
                s.ResolverDespliegue(como(s));
                return s;
            };
            var bien = jugar(Perfecta);
            var mal = jugar(Desastrosa);
            // El pronostico sigue siendo la misma cuenta que el lanzamiento, tambien con despliegues de por medio.
            var pronostico = mal.Pronostico();
            var lanzamientoMal = mal.EjecutarLanzamiento();
            Assert.AreEqual(pronostico.Puntaje, lanzamientoMal.Puntaje, 0.01);
            var lanzamientoBien = bien.EjecutarLanzamiento();

            Assert.Greater(lanzamientoMal.RiesgoDeLanzamiento, lanzamientoBien.RiesgoDeLanzamiento + 10, "el riesgo se nota");
            Assert.Greater(lanzamientoMal.DefectosEscapados, lanzamientoBien.DefectosEscapados, "y los errores");
            Assert.Greater(lanzamientoBien.Puntaje, lanzamientoMal.Puntaje + 8, "desplegar mal cuesta bastante en la nota final");
        }

        [Test]
        public void Un_despliegue_que_no_se_llega_a_jugar_no_cuenta_ni_a_favor_ni_en_contra() {
            var s = Empezar("nivel-00", Catalogo().Niveles["nivel-00"].MetodologiasPermitidas[0]);
            JugarHasta(s, s.Perfil.DiasTotales);
            Assert.IsTrue(s.DesplieguePendiente);
            Assert.AreEqual(0, s.RiesgoPorDespliegues());
            Assert.AreEqual(0, s.DefectosPorDespliegues());
            Assert.DoesNotThrow(() => s.EjecutarLanzamiento());
            Assert.IsFalse(s.DesplieguePendiente, "lanzado el nivel, ya no hay nada que desplegar");
        }

        // ================================================================ el cierre del dia con horas extra

        [Test]
        public void Quedarse_no_mueve_el_proyecto_hasta_que_el_dia_termina() {
            var s = Empezar("nivel-01", "scrum");
            EmpezarDia(s);
            LlegarAlCierre(s);
            var avance = s.W.Avance; var cansancio = s.W.Cansancio;

            s.Quedarse();
            Assert.IsTrue(s.JornadaProrrogada);
            Assert.IsFalse(s.JornadaSimulada, "quedarse solo alarga el reloj");
            Assert.AreEqual(avance, s.W.Avance, "las barras no se mueven a las 18:00");
            Assert.AreEqual(cansancio, s.W.Cansancio);

            s.AvanzarReloj(s.MinutosRestantesDeJornada);
            Assert.AreEqual(avance, s.W.Avance, "ni durante las horas extra");
            s.TerminarDia();
            Assert.IsTrue(s.JornadaSimulada);
            Assert.Greater(s.W.Avance, avance, "se mueven al terminar el día");
            Assert.Greater(s.W.Cansancio, cansancio, "con las horas extra ya contadas");
            Assert.Throws<InvalidOperationException>(() => s.TerminarDia(), "un día no se cierra dos veces");
        }

        [Test]
        public void Las_seis_de_la_tarde_sin_decidir_no_es_un_dia_cerrado_ni_despues_de_recargar() {
            var s = Empezar("nivel-01", "scrum");
            EmpezarDia(s);
            LlegarAlCierre(s);
            Assert.IsTrue(s.JornadaLlegoAlCierre);
            Assert.IsFalse(s.JornadaSimulada, "todavía no eligió irse o quedarse");

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(s.Capturar(), CatalogLoader.Settings);
            var guardado = Newtonsoft.Json.JsonConvert.DeserializeObject<Nexus.Core.Guardado.NivelEnCurso>(json, CatalogLoader.Settings);
            var vuelta = GameSession.Restaurar(Catalogo(), "nivel-01", Flags(), 3, guardado);
            Assert.IsFalse(vuelta.JornadaSimulada, "recargar ahí no puede dar el día por simulado: se saltaría");
            var avance = vuelta.W.Avance;
            vuelta.TerminarDia(false);
            Assert.Greater(vuelta.W.Avance, avance);
            Assert.IsTrue(vuelta.JornadaSimulada);
        }

        // ================================================================ el backlog guiado

        [Test]
        public void El_backlog_guiado_empieza_desordenado_y_seguir_a_Marisol_mueve_tarjetas_de_verdad() {
            var raiz = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            if (string.IsNullOrEmpty(raiz)) raiz = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            var escenas = Directory.GetFiles(Path.Combine(raiz, "minijuegos"), "MJ-*.json").Select(f => CatalogoMinijuegos.Parsear(File.ReadAllText(f)))
                                   .Where(d => Verbos.Normalizar(d.Verbo) == Verbos.Ordenar).ToList();
            Assert.IsNotEmpty(escenas);
            foreach (var def in escenas) {
                var orden = OrdenarEvaluador.OrdenInicial(def.Ordenar, 0);
                var movidas = 0;
                foreach (var paso in RecorridoGuiado.Para(def).Where(p => p.Accion == AccionGuiada.Mover)) {
                    if (orden.IndexOf(paso.Objetivo) != Math.Min(paso.Valor, orden.Count - 1)) movidas++;
                    orden.Remove(paso.Objetivo);
                    orden.Insert(Math.Min(paso.Valor, orden.Count), paso.Objetivo);
                }
                Assert.Greater(movidas, 0, def.Id + ": el recorrido guiado no mueve ninguna tarjeta");
                Assert.AreEqual(ResultadosDeMinijuego.Todos, RecorridoGuiado.Aplicar(def, RecorridoGuiado.Para(def)).Resultado, def.Id);
            }
        }
    }
}
