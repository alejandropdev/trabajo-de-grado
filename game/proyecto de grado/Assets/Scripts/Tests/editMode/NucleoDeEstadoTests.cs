using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Nexus.Core.Guardado;
using Nexus.Core.Modelo;
using Nexus.Core.Simulacion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C1 · Nucleo de Estado. Lo que se blinda aqui es la ACOTACION (§4.2.1) y el aislamiento entre
    /// partidas: un stock que se escapa de su rango o un perfil que se comparte por referencia no dan
    /// error, dan una partida sutilmente distinta, que es mucho peor de depurar.
    /// </summary>
    public class NucleoDeEstadoTests {
        private const double Tol = 1e-9;

        private static LevelProfile PerfilDePrueba() {
            var p = new LevelProfile {
                Id = "nivel-01",
                Nombre = "Cradle Lifts",
                Briefing = new[] { "Veinte dias.", "No es un proyecto importante." },
                DiasTotales = 20,
                PresupuestoInicial = 18000,
                AlcanceInicial = 34,
                EquipoInicial = 3,
                DeudaHeredada = 0,
                CoberturaHeredada = 40,
                DocumentacionHeredada = 40,
                VelocidadBase = 3.0,
                VolatilidadReal = 25,
                ObjetivosActivos = new[] { "OA-DOC-01" },
                MetodologiasPermitidas = new[] { "cascada", "scrum", "kanban" },
                NivelAndamiaje = 3
            };
            p.Director.PresupuestoDrama = new[] { 1, 3, 2, 1 };
            p.Director.PesosPorTag["equipo"] = 1.5;
            p.Director.PesosPorTag["tecnico"] = 0.8;
            p.Fase1.Arquitecturas.Add(new ArquitecturaOpcion { Id = "monolito", Nombre = "Monolito", EsLaAdecuada = true });
            p.Fase1.Calidad.Atributos.Add(new AtributoCalidad { Id = "seguridad", TagAfectado = "seguridad" });
            return p;
        }

        // ---------------------------------------------------------------- WorldState

        [Test]
        public void Los_valores_iniciales_son_los_de_la_especificacion() {
            var w = new WorldState();
            Assert.AreEqual(0.0, w.Dias, Tol);
            Assert.AreEqual(0.0, w.Avance, Tol);
            Assert.AreEqual(60.0, w.MoralEquipo, Tol);
            Assert.AreEqual(10.0, w.Cansancio, Tol);
            Assert.AreEqual(50.0, w.Competencia, Tol);
            Assert.AreEqual(50.0, w.SatisfaccionCliente, Tol);
            Assert.AreEqual(50.0, w.Reputacion, Tol);
            Assert.AreEqual(80.0, w.SaludJugador, Tol);
            Assert.AreEqual(1.0, w.VelocidadMod, Tol);
            Assert.AreEqual(14, WorldState.Nombres.Count, "13 stocks mas VelocidadMod");
        }

        [Test]
        public void Los_nueve_stocks_acotados_se_quedan_entre_0_y_100() {
            var w = new WorldState();
            foreach (var nombre in WorldState.Nombres) {
                if (!WorldState.EsStockAcotado(nombre)) continue;
                double v;

                w.Set(nombre, 999);
                w.TryGet(nombre, out v);
                Assert.AreEqual(100.0, v, Tol, nombre + " deberia topar en 100");

                w.Set(nombre, -999);
                w.TryGet(nombre, out v);
                Assert.AreEqual(0.0, v, Tol, nombre + " deberia topar en 0");
            }
            Assert.AreEqual(9, ContarAcotados(), "son exactamente nueve");
        }

        private static int ContarAcotados() {
            var n = 0;
            foreach (var nombre in WorldState.Nombres)
                if (WorldState.EsStockAcotado(nombre)) n++;
            return n;
        }

        [Test]
        public void Dias_alcance_y_avance_no_bajan_de_cero_pero_no_tienen_techo() {
            var w = new WorldState();
            w.Set("Dias", -5);
            w.Set("Alcance", -5);
            w.Set("Avance", 500);
            Assert.AreEqual(0.0, w.Dias, Tol);
            Assert.AreEqual(0.0, w.Alcance, Tol);
            Assert.AreEqual(500.0, w.Avance, Tol, "el avance puede pasar de 100: son puntos, no un porcentaje");
        }

        [Test]
        public void El_dinero_puede_ser_negativo() {
            var w = new WorldState();
            w.Set("Dinero", -1200.5);
            Assert.AreEqual(-1200.5, w.Dinero, Tol, "estar en numeros rojos es un estado valido del juego");
        }

        [Test]
        public void VelocidadMod_tiene_suelo_de_0_1_y_nunca_llega_a_cero() {
            var w = new WorldState();
            w.Set("VelocidadMod", 0);
            Assert.AreEqual(0.1, w.VelocidadMod, Tol, "con 0 el proyecto se congelaria para siempre");
            w.Set("VelocidadMod", -3);
            Assert.AreEqual(0.1, w.VelocidadMod, Tol);
        }

        [Test]
        public void Un_stock_desconocido_lanza_nombrandolo_y_listando_los_validos() {
            var w = new WorldState();
            var ex = Assert.Throws<InvalidOperationException>(() => w.Set("DeudaMoral", 5));
            StringAssert.Contains("DeudaMoral", ex.Message);
            StringAssert.Contains("DeudaTecnica", ex.Message, "el mensaje debe listar los validos");

            double v;
            Assert.IsFalse(w.TryGet("DeudaMoral", out v));
            Assert.AreEqual(0.0, v, Tol);
        }

        [Test]
        public void Un_valor_no_finito_se_rechaza_en_el_acto() {
            var w = new WorldState();
            Assert.Throws<InvalidOperationException>(() => w.Set("DeudaTecnica", double.NaN));
            Assert.Throws<InvalidOperationException>(() => w.Set("Dinero", double.PositiveInfinity));
            Assert.AreEqual(0.0, w.DeudaTecnica, Tol, "un rechazo no deja el estado a medias");
        }

        [Test]
        public void El_acceso_por_nombre_no_distingue_mayusculas() {
            var w = new WorldState();
            w.Set("deudatecnica", 42);
            double v;
            Assert.IsTrue(w.TryGet("DEUDATECNICA", out v));
            Assert.AreEqual(42.0, v, Tol);
            Assert.IsTrue(w.TryGet("  DeudaTecnica  ", out v), "se recortan los espacios del JSON");
            Assert.IsTrue(WorldState.EsStock("velocidadMod"));
            Assert.IsFalse(WorldState.EsStock("FLG_SALUD"));
        }

        [Test]
        public void Clone_produce_un_estado_independiente() {
            var w = new WorldState();
            w.Set("DeudaTecnica", 30);
            var copia = w.Clone();
            copia.Set("DeudaTecnica", 90);
            Assert.AreEqual(30.0, w.DeudaTecnica, Tol);
            Assert.AreEqual(90.0, copia.DeudaTecnica, Tol);
        }

        [Test]
        public void ToString_usa_punto_decimal_en_cualquier_cultura() {
            CultureInfo espanola;
            try {
                espanola = new CultureInfo("es-ES");
            } catch (CultureNotFoundException) {
                Assert.Ignore("Esta maquina no tiene la cultura es-ES instalada.");
                return;
            }

            var previa = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = espanola;
                var texto = new WorldState().ToString();
                StringAssert.Contains("SaludJugador=80.0", texto);
                StringAssert.Contains("VelocidadMod=1.00", texto);
                Assert.IsFalse(texto.Contains(","), "una coma decimal rompe la auditoria de la traza");
            } finally {
                CultureInfo.CurrentCulture = previa;
            }
        }

        [Test]
        public void DesdeNivel_aplica_las_condiciones_iniciales_del_perfil() {
            var w = WorldState.DesdeNivel(PerfilDePrueba());
            Assert.AreEqual(18000.0, w.Dinero, Tol);
            Assert.AreEqual(34.0, w.Alcance, Tol);
            Assert.AreEqual(40.0, w.Cobertura, Tol);
            Assert.AreEqual(40.0, w.Documentacion, Tol);
            Assert.AreEqual(0.0, w.Dias, Tol);
            Assert.AreEqual(80.0, w.SaludJugador, Tol, "lo que el perfil no dice se queda con el inicial de la spec");
        }

        [Test]
        public void El_WorldState_sobrevive_a_un_viaje_por_el_json_del_guardado() {
            var w = WorldState.DesdeNivel(PerfilDePrueba());
            w.Set("DeudaTecnica", 47.25);
            w.Set("Dinero", -1200.5);

            var json = JsonDeGuardado.Serializar(w);
            var vuelta = JsonDeGuardado.Deserializar<WorldState>(json);

            StringAssert.Contains("\"deudaTecnica\": 47.25", json, "camelCase, como el resto del guardado");
            Assert.AreEqual(json, JsonDeGuardado.Serializar(vuelta));
            Assert.AreEqual(-1200.5, vuelta.Dinero, Tol);
            Assert.AreEqual(47.25, vuelta.DeudaTecnica, Tol);
        }

        // ------------------------------------------------- WorldState + ForresterModel (C2, ya existente)

        [Test]
        public void ForresterModel_avanza_un_dia_sobre_un_WorldState_real() {
            var w = WorldState.DesdeNivel(PerfilDePrueba());
            w.Set("Cobertura", 50);
            w.Set("Documentacion", 50);
            var r = new RuntimeState();

            var avanceDia = ForresterModel.AvanzarUnDia(w, r, new Coeficientes(), 3.0, 1.0, false);

            // V = 3 · 1 · VelocidadMod(1) · fComp(50)=1 · fMoral(60)=0.8 · fFatiga(10)=0.994 · fDeuda(0)=1
            const double v = 2.3856;
            Assert.AreEqual(v, avanceDia, Tol);
            Assert.AreEqual(v, w.Avance, Tol);
            Assert.AreEqual(0.8 * 0.8, w.DeudaTecnica, Tol);
            Assert.AreEqual(6.0, w.Cansancio, Tol);
            Assert.AreEqual(82.0, w.SaludJugador, Tol);
            Assert.AreEqual(60.0 + 0.6 - 0.02 * 6.0, w.MoralEquipo, Tol);
            Assert.AreEqual(50.0 - 0.5 * (v / 3.0), w.Cobertura, Tol);
            Assert.AreEqual(1.0, w.Dias, Tol);
            Assert.AreEqual(18000.0, w.Dinero, Tol, "ForresterModel no toca el dinero");
        }

        [Test]
        public void La_acotacion_del_WorldState_frena_al_modelo_igual_que_el_estado_falso() {
            var w = WorldState.DesdeNivel(PerfilDePrueba());
            w.Set("Cansancio", 99);
            w.Set("Cobertura", 0.1);
            var r = new RuntimeState { DiasSeguidosTrabajando = 1 };

            ForresterModel.AvanzarUnDia(w, r, new Coeficientes(), 30.0, 1.0, true);

            Assert.AreEqual(100.0, w.Cansancio, Tol);
            Assert.AreEqual(0.0, w.Cobertura, Tol);
        }

        // ---------------------------------------------------------------- RuntimeState

        [Test]
        public void RuntimeState_expone_solo_los_diez_campos_consultables() {
            var r = new RuntimeState { DiaActual = 7, Fase = 2, WipActual = 3, LimiteWip = 4, SobreCompromiso = 1.5 };
            double v;

            Assert.AreEqual(10, RuntimeState.Consultables.Count);
            foreach (var nombre in RuntimeState.Consultables)
                Assert.IsTrue(r.TryGet(nombre, out v), nombre + " deberia ser consultable");

            Assert.IsTrue(r.TryGet("diaActual", out v));
            Assert.AreEqual(7.0, v, Tol);
            Assert.IsTrue(r.TryGet("sobreCompromiso", out v));
            Assert.AreEqual(1.5, v, Tol);

            foreach (var interno in new[] { "sprintActual", "visitasZonaC", "minijuegosJugados", "ventana", "cambiosRechazados" })
                Assert.IsFalse(r.TryGet(interno, out v), interno + " es interno a proposito");
        }

        [Test]
        public void RuntimeState_cumple_el_puerto_de_contadores_en_solo_lectura() {
            var r = new RuntimeState { DiasSeguidosTrabajando = 2, SobreCompromiso = 4.5, WipActual = 6 };
            IContadoresDeSimulacion puerto = r;

            Assert.AreEqual(2, puerto.DiasSeguidosTrabajando);
            Assert.AreEqual(4.5, puerto.SobreCompromiso, Tol);
            Assert.AreEqual(6, puerto.WipActual);
        }

        [Test]
        public void DesdeNivel_copia_el_presupuesto_de_drama_en_vez_de_compartirlo() {
            var perfil = PerfilDePrueba();
            var r = RuntimeState.DesdeNivel(perfil);

            CollectionAssert.AreEqual(new[] { 1, 3, 2, 1 }, r.DramaRestante);
            r.DramaRestante[1] = 0;

            Assert.AreEqual(3, perfil.Director.PresupuestoDrama[1],
                            "gastar drama en una partida no puede vaciarlo para la siguiente");
            Assert.AreEqual(5, r.Artefactos.Count, "SRS, SAD, SDD, PTP y PMP empiezan a cero");
            Assert.AreEqual(1, r.Fase);
        }

        [Test]
        public void DramaDeLaFase_devuelve_la_casilla_de_la_fase_actual_y_cero_fuera_de_rango() {
            var r = RuntimeState.DesdeNivel(PerfilDePrueba());
            r.Fase = 2;
            Assert.AreEqual(3, r.DramaDeLaFase());
            r.Fase = 9;
            Assert.AreEqual(0, r.DramaDeLaFase());
        }

        // ---------------------------------------------------------------- LevelProfile

        [Test]
        public void LevelProfile_Clone_es_profundo_en_todo_lo_mutable() {
            var original = PerfilDePrueba();
            var copia = original.Clone();

            copia.Director.PesosPorTag["equipo"] = 99;
            copia.Director.PresupuestoDrama[0] = 99;
            copia.Coef.MultiplicarUno("kappa", 0.5);
            copia.Briefing[0] = "otra cosa";
            copia.ObjetivosActivos[0] = "OA-X";
            copia.Fase1.Arquitecturas[0].Id = "microservicios";
            copia.Fase1.Calidad.Atributos[0].TagAfectado = "otro";
            copia.Umbrales.Exito.AvanceMinimo = 99;

            Assert.AreEqual(1.5, original.Director.PesosPorTag["equipo"], Tol);
            Assert.AreEqual(1, original.Director.PresupuestoDrama[0]);
            Assert.AreEqual(0.4, original.Coef.Kappa, Tol);
            Assert.AreEqual("Veinte dias.", original.Briefing[0]);
            Assert.AreEqual("OA-DOC-01", original.ObjetivosActivos[0]);
            Assert.AreEqual("monolito", original.Fase1.Arquitecturas[0].Id);
            Assert.AreEqual("seguridad", original.Fase1.Calidad.Atributos[0].TagAfectado);
            Assert.AreEqual(0.0, original.Umbrales.Exito.AvanceMinimo, Tol);
        }

        [Test]
        public void Un_umbral_de_fallo_ausente_es_null_y_no_un_numero_centinela() {
            var p = new LevelProfile();
            Assert.IsNull(p.Umbrales.Fallo.Deuda);
            Assert.IsNull(p.Umbrales.Fallo.Moral);
            Assert.IsNull(p.Umbrales.Fallo.Dinero);
            Assert.AreEqual(100.0, p.Umbrales.Exito.DeudaMaxima, Tol);
        }

        [Test]
        public void El_factor_de_tag_por_fichas_sigue_la_tabla_del_reparto_de_calidad() {
            Assert.AreEqual(2.0, CalidadConfig.FactorDeTag(0), Tol, "sin invertir, el riesgo se duplica");
            Assert.AreEqual(1.3, CalidadConfig.FactorDeTag(1), Tol);
            Assert.AreEqual(1.0, CalidadConfig.FactorDeTag(2), Tol);
            Assert.AreEqual(0.6, CalidadConfig.FactorDeTag(3), Tol);
            Assert.AreEqual(0.6, CalidadConfig.FactorDeTag(8), Tol);
            Assert.AreEqual(2.0, CalidadConfig.FactorDeTag(-1), Tol, "un reparto invalido se trata como cero");
        }

        [Test]
        public void El_LevelProfile_sobrevive_a_un_viaje_por_json_sin_duplicar_listas() {
            var original = PerfilDePrueba();
            var json = JsonConvert.SerializeObject(original, JsonDeGuardado.Settings);
            var vuelta = JsonConvert.DeserializeObject<LevelProfile>(json, JsonDeGuardado.Settings);

            Assert.AreEqual(4, vuelta.Director.PresupuestoDrama.Length,
                            "ObjectCreationHandling.Replace: no se añaden a los valores por defecto");
            Assert.AreEqual(2, vuelta.Briefing.Length);
            Assert.AreEqual(1, vuelta.Fase1.Arquitecturas.Count);
            Assert.AreEqual(20, vuelta.DiasTotales);
            Assert.AreEqual(25.0, vuelta.VolatilidadReal, Tol);
            Assert.IsTrue(vuelta.Director.PesosPorTag.ContainsKey("equipo"));
            Assert.AreEqual(json, JsonConvert.SerializeObject(vuelta, JsonDeGuardado.Settings));
        }

        [Test]
        public void Los_diccionarios_siguen_ignorando_mayusculas_despues_de_pasar_por_json() {
            var original = PerfilDePrueba();
            var vuelta = JsonConvert.DeserializeObject<LevelProfile>(
                JsonConvert.SerializeObject(original, JsonDeGuardado.Settings), JsonDeGuardado.Settings);

            // Newtonsoft reconstruye los diccionarios con el comparador por defecto: sin el [OnDeserialized]
            // esto devolveria false y el peso del tag se quedaria en 1.0 sin avisar a nadie.
            Assert.IsTrue(vuelta.Director.PesosPorTag.ContainsKey("EQUIPO"),
                          "pesosPorTag debe seguir sin distinguir mayusculas tras recargar");

            var r = JsonConvert.DeserializeObject<RuntimeState>(
                JsonConvert.SerializeObject(RuntimeState.DesdeNivel(original), JsonDeGuardado.Settings),
                JsonDeGuardado.Settings);
            Assert.IsTrue(r.Artefactos.ContainsKey("srs"), "los artefactos tampoco");
        }

        [Test]
        public void El_RuntimeState_sobrevive_a_un_viaje_por_json() {
            var r = RuntimeState.DesdeNivel(PerfilDePrueba());
            r.DiaActual = 4;
            r.DiasSeguidosTrabajando = 2;
            r.Enfriamientos["EV-TEC-02"] = 3;
            r.SerieWip.Add(2);
            r.SerieWip.Add(5);

            var json = JsonConvert.SerializeObject(r, JsonDeGuardado.Settings);
            var vuelta = JsonConvert.DeserializeObject<RuntimeState>(json, JsonDeGuardado.Settings);

            Assert.AreEqual(4, vuelta.DiaActual);
            Assert.AreEqual(2, vuelta.DiasSeguidosTrabajando);
            Assert.AreEqual(3, vuelta.Enfriamientos["EV-TEC-02"]);
            CollectionAssert.AreEqual(new List<double> { 2, 5 }, vuelta.SerieWip);
            Assert.AreEqual(4, vuelta.DramaRestante.Length);
            StringAssert.Contains("\"EV-TEC-02\"", json, "las claves de diccionario no se camelizan");
        }
    }
}
