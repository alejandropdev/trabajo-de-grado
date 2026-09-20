using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nexus.Core.Guardado;
using Nexus.Core.Metodologia;
using Nexus.Core.Simulacion;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C5 · Reglas de metodologia. La propiedad que se blinda aqui no es un calculo: es que el motor
    /// NO sepa que existe Scrum. Todo lo que diferencia a una metodologia de otra vive en su JSON,
    /// y el test Una_metodologia_nueva_entra_solo_con_un_json lo demuestra inventandose una.
    /// </summary>
    public class ReglasDeMetodologiaTests {
        private const double Tol = 1e-9;
        private const int Dias = 20;

        // ------------------------------------------------------------------ los tres perfiles

        private static RubricaCierre RubricaMinima() {
            return new RubricaCierre {
                Practicas = {
                    new PracticaEsperada {
                        Id = "ritmo", Descripcion = "Ritmo sostenible", Metrica = "diasConHorasExtra",
                        Comparador = "<=", Objetivo = 4,
                        RazonSiCumple = "Mantuviste el ritmo.", RazonSiFalla = "Quemaste al equipo."
                    }
                }
            };
        }

        private static MethodologyProfile Scrum() {
            return new MethodologyProfile {
                Id = "scrum", Nombre = "Scrum", Familia = FamiliasDeMetodologia.Agil,
                RazonesValidas = { "el_cliente_cambiara_de_opinion" },
                RazonesTrampa = { "es_lo_que_se_usa_ahora" },
                Calendario = new Calendario {
                    Tipo = TiposDeCalendario.Iterativo, EtiquetaUnidad = "Sprint",
                    LongitudIteracion = 10, Iteraciones = 2
                },
                Ceremonias = {
                    new Ceremonia { Id = "daily", Nombre = "Daily", Cuando = CuandoAplica.Diario, CosteDias = 0 },
                    new Ceremonia { Id = "planning", Nombre = "Planning", Cuando = CuandoAplica.InicioIteracion,
                                    CosteDias = 0.5, Verbo = "V5", AbreVentanaDeCambio = true },
                    new Ceremonia { Id = "review", Nombre = "Review", Cuando = CuandoAplica.FinIteracion,
                                    CosteDias = 0.5, RevelaProductoAlCliente = true },
                    new Ceremonia { Id = "retro", Nombre = "Retrospectiva", Cuando = CuandoAplica.FinIteracion,
                                    CosteDias = 0.5, Verbo = "V4", AjustaCoeficiente = true,
                                    Acciones = {
                                        new AccionRetro { Id = "documentar", Coeficiente = "kappa", Multiplicador = 0.85,
                                                          Texto = "Documentar antes de cerrar tarea" },
                                        new AccionRetro { Id = "refactor20", Coeficiente = "beta", Multiplicador = 1.25,
                                                          Texto = "Reservar 20 % del sprint a refactor" }
                                    } }
                },
                ReglasDeCambio = new ReglasDeCambio {
                    CosteMultiplicador = 1.0,
                    Ventanas = new List<string> { VentanasDeCambio.EntreIteraciones },
                    TextoEnVentana = "Entra en el sprint que viene.",
                    PenalizacionFueraDeVentana = new PenalizacionFueraDeVentana {
                        Permitido = true, CosteMultiplicador = 2.0,
                        EfectosExtra = { { "MoralEquipo", -4 } },
                        Texto = "Romper el sprint se paga."
                    }
                },
                ModificadoresModelo = { { "kappa", 0.9 } },
                ModificadoresDirector = new ModificadoresDirector {
                    MultiplicadorPesosPorTag = { { "equipo", 1.2 } }, MultiplicadorDrama = 1.0
                },
                TableroPrincipal = TablerosPrincipales.Burndown,
                RubricaCierre = RubricaMinima()
            };
        }

        private static MethodologyProfile Cascada() {
            return new MethodologyProfile {
                Id = "cascada", Nombre = "Cascada", Familia = FamiliasDeMetodologia.Tradicional,
                Calendario = new Calendario {
                    Tipo = TiposDeCalendario.Secuencial, EtiquetaUnidad = "Etapa",
                    Etapas = {
                        new Etapa { Id = "analisis", Nombre = "Analisis", Dias = 6,
                                    MultiplicadorPesosPorTag = { { "alcance", 2.0 } }, Texto = "Cerrar requisitos." },
                        new Etapa { Id = "diseno", Nombre = "Diseño", Dias = 5,
                                    MultiplicadorPesosPorTag = { { "tecnico", 1.5 } } },
                        new Etapa { Id = "construccion", Nombre = "Construccion", Dias = 6,
                                    EfectosPorDia = { { "Documentacion", -0.2 } } },
                        new Etapa { Id = "pruebas", Nombre = "Pruebas", Dias = 3,
                                    MultiplicadorPesosPorTag = { { "calidad", 2.5 } } }
                    }
                },
                Ceremonias = {
                    new Ceremonia { Id = "hito", Nombre = "Hito de aprobacion", Cuando = CuandoAplica.FinEtapa,
                                    CosteDias = 1.0 }
                },
                ReglasDeCambio = new ReglasDeCambio {
                    CosteMultiplicador = 3.0,
                    Ventanas = new List<string> { VentanasDeCambio.Hito },
                    PenalizacionFueraDeVentana = new PenalizacionFueraDeVentana {
                        Permitido = false, Texto = "El documento esta firmado. Fuera de un hito no se toca."
                    }
                },
                ModificadoresDirector = new ModificadoresDirector { MultiplicadorDrama = 0.8 },
                TableroPrincipal = TablerosPrincipales.CurvaS,
                RubricaCierre = RubricaMinima()
            };
        }

        private static MethodologyProfile Kanban() {
            return new MethodologyProfile {
                Id = "kanban", Nombre = "Kanban", Familia = FamiliasDeMetodologia.Agil,
                Calendario = new Calendario {
                    Tipo = TiposDeCalendario.Continuo, EtiquetaUnidad = "Flujo", LimiteWipInicial = 4
                },
                Ceremonias = {
                    new Ceremonia { Id = "reposicion", Nombre = "Reposicion",
                                    Cuando = CuandoAplica.CuandoSeLiberaWip, MuestraLeadTime = true },
                    new Ceremonia { Id = "flujo", Nombre = "Revision de flujo", Cuando = CuandoAplica.Cada,
                                    CadaNDias = 5, CosteDias = 0.25 }
                },
                ReglasDeCambio = new ReglasDeCambio {
                    CosteMultiplicador = 1.0,
                    Ventanas = new List<string> { VentanasDeCambio.Siempre },
                    ConsumeWip = true,
                    TextoEnVentana = "Entra, pero algo tiene que salir del tablero."
                },
                TableroPrincipal = TablerosPrincipales.Cfd,
                RubricaCierre = RubricaMinima()
            };
        }

        // ------------------------------------------------------------------ troceado del calendario

        [Test]
        public void Scrum_trocea_el_nivel_en_iteraciones() {
            var r = new MethodologyRules(Scrum(), Dias);

            Assert.AreEqual(2, r.NumeroDeUnidades);
            Assert.AreEqual(1, r.Tramos[0].DiaInicio);
            Assert.AreEqual(10, r.Tramos[0].DiaFin);
            Assert.AreEqual(11, r.Tramos[1].DiaInicio);
            Assert.AreEqual(20, r.Tramos[1].DiaFin);
            Assert.AreEqual("Sprint", r.PlanFor(4).EtiquetaUnidad);
        }

        [Test]
        public void Cascada_trocea_el_nivel_en_etapas() {
            var r = new MethodologyRules(Cascada(), Dias);

            Assert.AreEqual(4, r.NumeroDeUnidades);
            CollectionAssert.AreEqual(new[] { "analisis", "diseno", "construccion", "pruebas" },
                                      r.Tramos.Select(t => t.Id).ToArray());
            Assert.AreEqual(6, r.Tramos[0].DiaFin);
            Assert.AreEqual(7, r.Tramos[1].DiaInicio);
            Assert.AreEqual(18, r.Tramos[3].DiaInicio);
            Assert.AreEqual(20, r.Tramos[3].DiaFin);
        }

        [Test]
        public void Kanban_es_un_solo_tramo_de_flujo_continuo() {
            var r = new MethodologyRules(Kanban(), Dias);

            Assert.AreEqual(1, r.NumeroDeUnidades);
            Assert.AreEqual(1, r.Tramos[0].DiaInicio);
            Assert.AreEqual(20, r.Tramos[0].DiaFin);
            Assert.AreEqual(4, r.Profile.Calendario.LimiteWipInicial);
        }

        [Test]
        public void El_ultimo_tramo_se_estira_hasta_el_final_del_nivel() {
            var perfil = Scrum();
            perfil.Calendario.LongitudIteracion = 7;   // 2 x 7 = 14 < 20
            var r = new MethodologyRules(perfil, Dias);

            Assert.AreEqual(2, r.NumeroDeUnidades);
            Assert.AreEqual(20, r.Tramos[1].DiaFin, "ningun dia del nivel puede quedarse sin unidad");
        }

        [Test]
        public void Los_tramos_que_se_pasan_del_nivel_se_recortan() {
            var perfil = Scrum();
            perfil.Calendario.Iteraciones = 5;   // 5 x 10 = 50 > 20
            var r = new MethodologyRules(perfil, Dias);

            Assert.AreEqual(2, r.NumeroDeUnidades, "no se crean unidades para dias que no existen");
            Assert.AreEqual(20, r.Tramos[1].DiaFin);
        }

        [Test]
        public void Cada_dia_del_nivel_pertenece_a_exactamente_una_unidad() {
            foreach (var perfil in new[] { Scrum(), Cascada(), Kanban() }) {
                var r = new MethodologyRules(perfil, Dias);
                for (var dia = 1; dia <= Dias; dia++) {
                    var coincidencias = r.Tramos.Count(t => t.Contiene(dia));
                    Assert.AreEqual(1, coincidencias, $"{perfil.Id}, dia {dia}");
                }
            }
        }

        // ------------------------------------------------------------------ ceremonias

        [Test]
        public void El_daily_cae_todos_los_dias_y_la_retro_solo_al_cerrar_el_sprint() {
            var r = new MethodologyRules(Scrum(), Dias);

            for (var dia = 1; dia <= Dias; dia++)
                Assert.IsTrue(r.PlanFor(dia).Ceremonias.Any(c => c.Id == "daily"), "dia " + dia);

            var conRetro = Enumerable.Range(1, Dias).Where(d => r.PlanFor(d).Ceremonias.Any(c => c.Id == "retro"));
            CollectionAssert.AreEqual(new[] { 10, 20 }, conRetro.ToArray());

            var conPlanning = Enumerable.Range(1, Dias).Where(d => r.PlanFor(d).Ceremonias.Any(c => c.Id == "planning"));
            CollectionAssert.AreEqual(new[] { 1, 11 }, conPlanning.ToArray());
        }

        [Test]
        public void El_hito_de_cascada_cae_al_cerrar_cada_etapa() {
            var r = new MethodologyRules(Cascada(), Dias);
            var conHito = Enumerable.Range(1, Dias).Where(d => r.PlanFor(d).Ceremonias.Any(c => c.Id == "hito"));
            CollectionAssert.AreEqual(new[] { 6, 11, 17, 20 }, conHito.ToArray());
        }

        [Test]
        public void Una_ceremonia_cada_N_dias_cae_cuando_toca() {
            var r = new MethodologyRules(Kanban(), Dias);
            var conFlujo = Enumerable.Range(1, Dias).Where(d => r.PlanFor(d).Ceremonias.Any(c => c.Id == "flujo"));
            CollectionAssert.AreEqual(new[] { 5, 10, 15, 20 }, conFlujo.ToArray());
        }

        [Test]
        public void La_reposicion_de_wip_no_la_dispara_el_calendario() {
            var r = new MethodologyRules(Kanban(), Dias);
            for (var dia = 1; dia <= Dias; dia++)
                Assert.IsFalse(r.PlanFor(dia).Ceremonias.Any(c => c.Id == "reposicion"),
                               "la reposicion la dispara el flujo, no la fecha");
        }

        // ------------------------------------------------------------------ el enfoque cambia dentro del nivel

        [Test]
        public void Las_etapas_de_cascada_cambian_el_enfoque_dentro_del_nivel() {
            var r = new MethodologyRules(Cascada(), Dias);

            Assert.AreEqual(2.0, r.MultiplicadorTag("alcance", r.PlanFor(3)), Tol, "en Analisis pesa el alcance");
            Assert.AreEqual(1.0, r.MultiplicadorTag("alcance", r.PlanFor(19)), Tol, "en Pruebas ya no");
            Assert.AreEqual(2.5, r.MultiplicadorTag("calidad", r.PlanFor(19)), Tol, "en Pruebas pesa la calidad");
            Assert.AreEqual(1.5, r.MultiplicadorTag("tecnico", r.PlanFor(8)), Tol, "en Diseño pesa lo tecnico");
        }

        [Test]
        public void El_multiplicador_de_tag_se_aplica_en_cascada_perfil_por_etapa() {
            var perfil = Cascada();
            perfil.ModificadoresDirector.MultiplicadorPesosPorTag["alcance"] = 0.5;
            var r = new MethodologyRules(perfil, Dias);

            Assert.AreEqual(0.5 * 2.0, r.MultiplicadorTag("alcance", r.PlanFor(3)), Tol);
            Assert.AreEqual(0.5, r.MultiplicadorTag("alcance", r.PlanFor(19)), Tol);
            Assert.AreEqual(1.0, r.MultiplicadorTag("inexistente", r.PlanFor(3)), Tol);
        }

        [Test]
        public void Los_efectos_por_dia_de_una_etapa_llegan_al_plan() {
            var r = new MethodologyRules(Cascada(), Dias);
            Assert.IsTrue(r.PlanFor(14).EfectosDeEtapa.ContainsKey("Documentacion"), "dia 14 esta en Construccion");
            CollectionAssert.IsEmpty(r.PlanFor(3).EfectosDeEtapa, "Analisis no cobra nada por dia");
        }

        // ------------------------------------------------------------------ cambios de alcance

        [Test]
        public void Cada_metodologia_trata_el_cambio_de_alcance_a_su_manera() {
            var kanban = new MethodologyRules(Kanban(), Dias).EvaluarCambioDeAlcance(7);
            Assert.IsTrue(kanban.Permitido);
            Assert.IsTrue(kanban.EnVentana, "en Kanban la ventana esta siempre abierta");
            Assert.IsTrue(kanban.ConsumeWip, "pero algo tiene que salir del tablero");

            var scrumDentro = new MethodologyRules(Scrum(), Dias).EvaluarCambioDeAlcance(7);
            Assert.IsTrue(scrumDentro.Permitido, "romper el sprint se puede…");
            Assert.IsFalse(scrumDentro.EnVentana);
            Assert.AreEqual(2.0, scrumDentro.CosteMultiplicador, Tol, "…pero al doble de coste");
            Assert.IsTrue(scrumDentro.EfectosExtra.ContainsKey("MoralEquipo"));

            var scrumEntre = new MethodologyRules(Scrum(), Dias).EvaluarCambioDeAlcance(10);
            Assert.IsTrue(scrumEntre.EnVentana, "al cerrar el sprint, si");
            Assert.AreEqual(1.0, scrumEntre.CosteMultiplicador, Tol);

            var cascadaDentro = new MethodologyRules(Cascada(), Dias).EvaluarCambioDeAlcance(8);
            Assert.IsFalse(cascadaDentro.Permitido, "en Cascada, fuera de un hito, directamente no");

            var cascadaHito = new MethodologyRules(Cascada(), Dias).EvaluarCambioDeAlcance(6);
            Assert.IsTrue(cascadaHito.Permitido);
            Assert.IsTrue(cascadaHito.EnVentana);
            Assert.AreEqual(3.0, cascadaHito.CosteMultiplicador, Tol, "y aun asi cuesta el triple");
        }

        [Test]
        public void Una_ceremonia_que_abre_ventana_manda_sobre_el_calendario() {
            var perfil = Scrum();
            perfil.ReglasDeCambio.Ventanas = new List<string>();   // el calendario no abre ninguna ventana
            var r = new MethodologyRules(perfil, Dias);

            Assert.IsTrue(r.EvaluarCambioDeAlcance(11).EnVentana, "el planning del dia 11 la abre");
            Assert.IsFalse(r.EvaluarCambioDeAlcance(7).EnVentana);
        }

        // ------------------------------------------------------------------ director

        [Test]
        public void Una_metodologia_puede_bloquear_eventos_que_con_ella_no_tienen_sentido() {
            var perfil = Kanban();
            perfil.ModificadoresDirector.EventosBloqueados.Add("EV-ALC-SPRINT");
            var r = new MethodologyRules(perfil, Dias);

            Assert.IsTrue(r.EventoBloqueado("EV-ALC-SPRINT", null));
            Assert.IsFalse(r.EventoBloqueado("EV-TEC-02", null));
        }

        [Test]
        public void Un_evento_que_solo_existe_para_otras_metodologias_se_bloquea() {
            var r = new MethodologyRules(Kanban(), Dias);

            Assert.IsTrue(r.EventoBloqueado("EV-X", new List<string> { "scrum", "xp" }));
            Assert.IsFalse(r.EventoBloqueado("EV-X", new List<string> { "kanban" }));
            Assert.IsFalse(r.EventoBloqueado("EV-X", new List<string>()), "lista vacia = vale para todas");
        }

        [Test]
        public void El_multiplicador_de_drama_sale_del_perfil() {
            Assert.AreEqual(1.0, new MethodologyRules(Scrum(), Dias).MultiplicadorDrama, Tol);
            Assert.AreEqual(0.8, new MethodologyRules(Cascada(), Dias).MultiplicadorDrama, Tol,
                            "Cascada hace el nivel menos convulso, y mas aburrido");
        }

        [Test]
        public void CeremoniaPorId_encuentra_la_ceremonia() {
            var r = new MethodologyRules(Scrum(), Dias);
            Assert.AreEqual("Retrospectiva", r.CeremoniaPorId("retro").Nombre);
            Assert.IsNull(r.CeremoniaPorId("no-existe"));
        }

        // ------------------------------------------------------------------ la retrospectiva toca el modelo

        [Test]
        public void Una_accion_de_retro_modifica_el_coeficiente_que_dice() {
            var r = new MethodologyRules(Scrum(), Dias);
            var retro = r.CeremoniaPorId("retro");
            var coef = new Coeficientes();

            var documentar = retro.Acciones.First(a => a.Id == "documentar");
            coef.MultiplicarUno(documentar.Coeficiente, documentar.Multiplicador);

            Assert.AreEqual(0.4 * 0.85, coef.Kappa, Tol, "a partir de hoy la documentacion se diluye mas despacio");
            Assert.AreEqual(0.5, coef.Iota, Tol, "y nada mas se movio");
        }

        // ------------------------------------------------------------------ la propiedad que importa

        [Test]
        public void Una_metodologia_nueva_entra_solo_con_un_json() {
            // Espiral no existe en el codigo. Si el motor tuviera un solo if con el nombre de una
            // metodologia dentro, esto no funcionaria.
            var espiral = new MethodologyProfile {
                Id = "espiral", Nombre = "Espiral", Familia = FamiliasDeMetodologia.Tradicional,
                Calendario = new Calendario {
                    Tipo = TiposDeCalendario.Iterativo, EtiquetaUnidad = "Ciclo",
                    LongitudIteracion = 5, Iteraciones = 4
                },
                Ceremonias = {
                    new Ceremonia { Id = "analisis-riesgo", Nombre = "Analisis de riesgo",
                                    Cuando = CuandoAplica.InicioIteracion, CosteDias = 1.0,
                                    AjustaCoeficiente = true,
                                    Acciones = { new AccionRetro { Id = "prototipo", Coeficiente = "alpha",
                                                                   Multiplicador = 0.7, Texto = "Prototipar lo dudoso" } } }
                },
                ReglasDeCambio = new ReglasDeCambio {
                    Ventanas = new List<string> { VentanasDeCambio.EntreIteraciones }, CosteMultiplicador = 1.5
                },
                TableroPrincipal = TablerosPrincipales.CurvaS,
                RubricaCierre = RubricaMinima()
            };

            var r = new MethodologyRules(espiral, Dias);

            Assert.AreEqual(4, r.NumeroDeUnidades);
            Assert.AreEqual("Ciclo", r.PlanFor(7).EtiquetaUnidad);
            Assert.IsTrue(r.PlanFor(6).Ceremonias.Any(c => c.Id == "analisis-riesgo"));
            Assert.IsTrue(r.EvaluarCambioDeAlcance(5).EnVentana);
            Assert.IsFalse(r.EvaluarCambioDeAlcance(7).EnVentana);
        }

        // ------------------------------------------------------------------ validacion

        [Test]
        public void Un_calendario_desconocido_se_rechaza_nombrandolo() {
            var perfil = Scrum();
            perfil.Calendario.Tipo = "agil-ish";
            var ex = Assert.Throws<InvalidOperationException>(() => new MethodologyRules(perfil, Dias));
            StringAssert.Contains("agil-ish", ex.Message);
            StringAssert.Contains("iterativo", ex.Message);
        }

        [Test]
        public void Un_calendario_secuencial_sin_etapas_se_rechaza() {
            var perfil = Cascada();
            perfil.Calendario.Etapas.Clear();
            Assert.Throws<InvalidOperationException>(() => new MethodologyRules(perfil, Dias));
        }

        [Test]
        public void Un_iterativo_sin_iteraciones_se_rechaza() {
            var perfil = Scrum();
            perfil.Calendario.Iteraciones = 0;
            Assert.Throws<InvalidOperationException>(() => new MethodologyRules(perfil, Dias));
        }

        [Test]
        public void Una_retrospectiva_sin_acciones_se_rechaza() {
            var perfil = Scrum();
            perfil.Ceremonias.First(c => c.Id == "retro").Acciones.Clear();
            var ex = Assert.Throws<InvalidOperationException>(() => new MethodologyRules(perfil, Dias));
            StringAssert.Contains("retro", ex.Message);
        }

        [Test]
        public void Un_coeficiente_inventado_en_una_accion_se_rechaza() {
            var perfil = Scrum();
            perfil.Ceremonias.First(c => c.Id == "retro").Acciones[0].Coeficiente = "omega";
            var ex = Assert.Throws<InvalidOperationException>(() => new MethodologyRules(perfil, Dias));
            StringAssert.Contains("omega", ex.Message);
        }

        [Test]
        public void Sin_rubrica_de_cierre_no_hay_con_que_juzgar() {
            var perfil = Scrum();
            perfil.RubricaCierre.Practicas.Clear();
            var ex = Assert.Throws<InvalidOperationException>(() => new MethodologyRules(perfil, Dias));
            StringAssert.Contains("rubricaCierre", ex.Message);
        }

        [Test]
        public void Una_familia_o_un_tablero_desconocidos_se_rechazan() {
            var conFamilia = Scrum();
            conFamilia.Familia = "hibrida";
            Assert.Throws<InvalidOperationException>(() => new MethodologyRules(conFamilia, Dias));

            var conTablero = Scrum();
            conTablero.TableroPrincipal = "gantt";
            Assert.Throws<InvalidOperationException>(() => new MethodologyRules(conTablero, Dias));
        }

        [Test]
        public void Una_ventana_de_cambio_inventada_se_rechaza() {
            var perfil = Scrum();
            perfil.ReglasDeCambio.Ventanas = new List<string> { "cuando_me_apetezca" };
            var ex = Assert.Throws<InvalidOperationException>(() => new MethodologyRules(perfil, Dias));
            StringAssert.Contains("cuando_me_apetezca", ex.Message);
        }

        // ------------------------------------------------------------------ datos

        [Test]
        public void Clone_es_profundo() {
            var original = Scrum();
            var copia = original.Clone();

            copia.Calendario.Iteraciones = 99;
            copia.Ceremonias[0].Id = "otro";
            copia.Ceremonias.First(c => c.Id == "retro").Acciones[0].Multiplicador = 99;
            copia.ModificadoresModelo["kappa"] = 99;
            copia.ReglasDeCambio.Ventanas.Clear();
            copia.RubricaCierre.Practicas[0].Objetivo = 99;
            copia.RazonesValidas.Clear();

            Assert.AreEqual(2, original.Calendario.Iteraciones);
            Assert.AreEqual("daily", original.Ceremonias[0].Id);
            Assert.AreEqual(0.85, original.Ceremonias.First(c => c.Id == "retro").Acciones[0].Multiplicador, Tol);
            Assert.AreEqual(0.9, original.ModificadoresModelo["kappa"], Tol);
            Assert.AreEqual(1, original.ReglasDeCambio.Ventanas.Count);
            Assert.AreEqual(4.0, original.RubricaCierre.Practicas[0].Objetivo, Tol);
            Assert.AreEqual(1, original.RazonesValidas.Count);
        }

        [Test]
        public void El_perfil_sobrevive_a_un_viaje_por_json() {
            var original = Scrum();
            var json = JsonConvert.SerializeObject(original, JsonDeGuardado.Settings);
            var vuelta = JsonConvert.DeserializeObject<MethodologyProfile>(json, JsonDeGuardado.Settings);

            Assert.AreEqual(json, JsonConvert.SerializeObject(vuelta, JsonDeGuardado.Settings));
            Assert.AreEqual(4, vuelta.Ceremonias.Count);
            Assert.AreEqual(1, vuelta.ReglasDeCambio.Ventanas.Count, "ObjectCreationHandling.Replace: no se duplican");
            Assert.IsTrue(vuelta.ModificadoresModelo.ContainsKey("KAPPA"), "y el diccionario sigue sin distinguir mayusculas");

            // y sigue funcionando igual tras el viaje
            var r = new MethodologyRules(vuelta, Dias);
            Assert.AreEqual(2, r.NumeroDeUnidades);
            Assert.IsTrue(r.EvaluarCambioDeAlcance(10).EnVentana);
        }
    }
}
