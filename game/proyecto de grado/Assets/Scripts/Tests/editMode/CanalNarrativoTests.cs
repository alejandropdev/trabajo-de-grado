using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Servicios;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C7 · Canal narrativo. Dos invariantes:
    /// · El director solo puede RETRASAR, ADELANTAR y COLOREAR; nunca CREAR ni CANCELAR.
    /// · FLG_DEUDA_MORAL no baja nunca. No es balanceo: es la tesis del juego.
    /// </summary>
    public class CanalNarrativoTests {
        private const double Tol = 1e-9;

        private sealed class Ctx : IStateContext {
            public readonly WorldState W = new WorldState();
            public readonly RuntimeState R = new RuntimeState { Fase = 2, DiaActual = 1 };

            public bool TryGetValue(string nombre, out double valor) {
                return W.TryGet(nombre, out valor) || R.TryGet(nombre, out valor);
            }

            public double CallFunction(string nombre, string argumento) { return 0; }
        }

        // ================================================================ censo de prueba

        private static List<DefinicionDeFlag> Censo() {
            return new List<DefinicionDeFlag> {
                new DefinicionDeFlag { Id = "FLG_DEUDA_MORAL", Eje = EjesDeFlag.Integridad, Inicial = 0,
                                       Min = 0, Max = 20, NoBaja = true,
                                       Descripcion = "NO BAJA NUNCA. Ni con buenas acciones." },
                new DefinicionDeFlag { Id = "FLG_DEUDA_TECNICA", Eje = EjesDeFlag.Competencia, Inicial = 0,
                                       Min = 0, Max = 100, Descripcion = "Snapshot al cerrar cada nivel." },
                new DefinicionDeFlag { Id = "FLG_INTEGRIDAD", Eje = EjesDeFlag.Integridad, Inicial = 0,
                                       Min = -10, Max = 10, Descripcion = "De -10 a +10." },
                new DefinicionDeFlag { Id = "FLG_SALUD", Eje = EjesDeFlag.Persona, Inicial = 80,
                                       Min = 0, Max = 100, Descripcion = "Abre el final F11." },
                new DefinicionDeFlag { Id = "FLG_VIDA_EXTERNA", Eje = EjesDeFlag.Persona, Inicial = 0,
                                       Min = 0, Max = 10, Descripcion = "Cuantas noches te fuiste a casa." },
                new DefinicionDeFlag { Id = "FLG_HORAS_EXTRA", Eje = EjesDeFlag.Estado, Inicial = 0,
                                       SinTecho = true, Descripcion = "Contador acumulado de toda la partida." },
                new DefinicionDeFlag { Id = "FLG_MORAL_EQUIPO", Eje = EjesDeFlag.Relaciones, Inicial = 60,
                                       Min = 0, Max = 100, Descripcion = "Snapshot al cerrar." },
                new DefinicionDeFlag { Id = "FLG_CALIDAD_ACUM", Eje = EjesDeFlag.Competencia, Inicial = 0,
                                       Min = 0, Max = 100, Descripcion = "Media de cobertura y documentacion." },
                new DefinicionDeFlag { Id = "FLG_REPUTACION", Eje = EjesDeFlag.Estado, Inicial = 50,
                                       Min = 0, Max = 100, Descripcion = "Snapshot al cerrar." }
            };
        }

        private static FlagStore Store(Dictionary<string, double> respaldo = null, bool conCenso = true) {
            return new FlagStore(respaldo ?? new Dictionary<string, double>(StringComparer.Ordinal),
                                 conCenso ? Censo() : null);
        }

        // ================================================================ FlagStore

        [Test]
        public void Todo_flag_empieza_por_FLG() {
            var flags = Store(conCenso: false);
            Assert.Throws<ArgumentException>(() => flags.Set("DEUDA_MORAL", 3));
            Assert.Throws<ArgumentException>(() => flags.Get("salud"));
            Assert.Throws<ArgumentException>(() => flags.Sumar("", 1));
            Assert.DoesNotThrow(() => flags.Set("FLG_LO_QUE_SEA", 3));
        }

        [Test]
        public void La_deuda_moral_no_baja_nunca() {
            var flags = Store();

            flags.Sumar(FlagStore.DeudaMoral, 5);
            Assert.AreEqual(5.0, flags.Get(FlagStore.DeudaMoral), Tol);

            flags.Sumar(FlagStore.DeudaMoral, -3);
            Assert.AreEqual(5.0, flags.Get(FlagStore.DeudaMoral), Tol,
                            "la deuda tecnica se paga refactorizando; la deuda moral no se paga");

            flags.Sumar(FlagStore.DeudaMoral, 2);
            Assert.AreEqual(7.0, flags.Get(FlagStore.DeudaMoral), Tol, "pero seguir subiendo si");
        }

        [Test]
        public void La_deuda_moral_tampoco_baja_por_la_puerta_de_atras() {
            var flags = Store();
            flags.Sumar(FlagStore.DeudaMoral, 8);

            flags.Set(FlagStore.DeudaMoral, 2);
            Assert.AreEqual(8.0, flags.Get(FlagStore.DeudaMoral), Tol, "Set tampoco la baja");

            flags.Set(FlagStore.DeudaMoral, 12);
            Assert.AreEqual(12.0, flags.Get(FlagStore.DeudaMoral), Tol);
        }

        [Test]
        public void Los_flags_se_acotan_segun_su_censo() {
            var flags = Store();

            flags.Set("FLG_DEUDA_TECNICA", 250);
            Assert.AreEqual(100.0, flags.Get("FLG_DEUDA_TECNICA"), Tol);

            flags.Set("FLG_INTEGRIDAD", -40);
            Assert.AreEqual(-10.0, flags.Get("FLG_INTEGRIDAD"), Tol, "este eje va de -10 a +10");

            flags.Set("FLG_INTEGRIDAD", 40);
            Assert.AreEqual(10.0, flags.Get("FLG_INTEGRIDAD"), Tol);

            flags.Set(FlagStore.DeudaMoral, 99);
            Assert.AreEqual(20.0, flags.Get(FlagStore.DeudaMoral), Tol);
        }

        [Test]
        public void Los_contadores_solo_tienen_suelo() {
            var flags = Store();

            flags.Sumar("FLG_HORAS_EXTRA", 412);
            Assert.AreEqual(412.0, flags.Get("FLG_HORAS_EXTRA"), Tol, "un contador no topa en 100");

            flags.Set("FLG_HORAS_EXTRA", -5);
            Assert.AreEqual(0.0, flags.Get("FLG_HORAS_EXTRA"), Tol);
        }

        [Test]
        public void Un_flag_que_nunca_se_escribio_vale_su_inicial() {
            var flags = Store();
            Assert.AreEqual(80.0, flags.Get("FLG_SALUD"), Tol);
            Assert.IsFalse(flags.Tiene("FLG_SALUD"), "valer 80 no es lo mismo que estar escrito");

            var sinCenso = Store(conCenso: false);
            Assert.AreEqual(0.0, sinCenso.Get("FLG_LO_QUE_SEA"), Tol);
        }

        [Test]
        public void El_respaldo_se_escribe_a_traves_y_no_se_copia() {
            var respaldo = new Dictionary<string, double>(StringComparer.Ordinal);
            var flags = Store(respaldo);

            flags.Set("FLG_DEUDA_TECNICA", 47);
            flags.Sumar("FLG_HORAS_EXTRA", 12);

            Assert.AreEqual(47.0, respaldo["FLG_DEUDA_TECNICA"], Tol,
                            "SaveGame.Flags esta al dia sin un paso de sincronizacion que se pueda olvidar");
            Assert.AreEqual(12.0, respaldo["FLG_HORAS_EXTRA"], Tol);
        }

        [Test]
        public void El_constructor_es_la_unica_puerta_que_se_salta_las_reglas() {
            // Restaurar una partida tiene que poder reponer cualquier valor, incluso uno al que hoy
            // no se podria llegar sumando.
            var guardado = new Dictionary<string, double>(StringComparer.Ordinal) {
                { FlagStore.DeudaMoral, 14 }, { "FLG_HORAS_EXTRA", 412 }
            };
            var flags = new FlagStore(guardado, Censo());

            Assert.AreEqual(14.0, flags.Get(FlagStore.DeudaMoral), Tol);
            Assert.AreEqual(412.0, flags.Get("FLG_HORAS_EXTRA"), Tol);

            flags.Sumar(FlagStore.DeudaMoral, -10);
            Assert.AreEqual(14.0, flags.Get(FlagStore.DeudaMoral), Tol, "pero desde dentro sigue sin bajar");
        }

        [Test]
        public void Un_flag_fuera_del_censo_se_rechaza_cuando_hay_censo() {
            var flags = Store();
            var ex = Assert.Throws<ArgumentException>(() => flags.Set("FLG_INVENTADO", 1));
            StringAssert.Contains("flags.json", ex.Message);
        }

        [Test]
        public void Sin_censo_no_hay_acotacion_pero_las_otras_reglas_siguen() {
            var flags = Store(conCenso: false);

            flags.Set("FLG_CUALQUIERA", 9999);
            Assert.AreEqual(9999.0, flags.Get("FLG_CUALQUIERA"), Tol);

            flags.Sumar(FlagStore.DeudaMoral, 5);
            flags.Sumar(FlagStore.DeudaMoral, -5);
            Assert.AreEqual(5.0, flags.Get(FlagStore.DeudaMoral), Tol, "la regla dura no depende del censo");
        }

        [Test]
        public void Inicializar_pone_los_iniciales_que_falten_sin_pisar_los_que_hay() {
            var respaldo = new Dictionary<string, double>(StringComparer.Ordinal) { { "FLG_SALUD", 40 } };
            var flags = Store(respaldo);

            flags.Inicializar();

            Assert.AreEqual(40.0, flags.Get("FLG_SALUD"), Tol, "no pisa lo que ya estaba");
            Assert.AreEqual(60.0, flags.Get("FLG_MORAL_EQUIPO"), Tol);
            Assert.AreEqual(Censo().Count, respaldo.Count);
        }

        [Test]
        public void Un_valor_no_finito_se_rechaza() {
            var flags = Store();
            Assert.Throws<InvalidOperationException>(() => flags.Set("FLG_SALUD", double.NaN));
        }

        [Test]
        public void Copia_es_independiente_de_la_partida() {
            var flags = Store();
            flags.Set("FLG_SALUD", 62);

            var copia = flags.Copia();
            copia["FLG_SALUD"] = 0;

            Assert.AreEqual(62.0, flags.Get("FLG_SALUD"), Tol);
        }

        // ================================================================ NarrativeDirector

        private static NarrativeBeat Beat(string id, int min, int max, string prioridad = PrioridadDeBeat.Opcional) {
            return new NarrativeBeat {
                Id = id, Nombre = "Escena " + id, Prioridad = prioridad,
                Ventana = new VentanaDeBeat { DiaMin = min, DiaMax = max }
            };
        }

        [Test]
        public void Un_beat_sale_una_sola_vez() {
            var ctx = new Ctx();
            var director = new NarrativeDirector(new List<NarrativeBeat> { Beat("CIN-1.2", 1, 20) });

            Assert.IsNotNull(director.BeatDeHoy(ctx.R, ctx));
            ctx.R.DiaActual = 2;
            Assert.IsNull(director.BeatDeHoy(ctx.R, ctx));
        }

        [Test]
        public void Un_beat_no_sale_antes_de_su_ventana() {
            var ctx = new Ctx();
            var director = new NarrativeDirector(new List<NarrativeBeat> { Beat("CIN-2.3", 8, 16) });

            for (var dia = 1; dia <= 7; dia++) {
                ctx.R.DiaActual = dia;
                Assert.IsNull(director.BeatDeHoy(ctx.R, ctx), "dia " + dia);
            }

            ctx.R.DiaActual = 8;
            Assert.IsNotNull(director.BeatDeHoy(ctx.R, ctx));
        }

        [Test]
        public void Un_opcional_se_pierde_si_se_le_pasa_la_ventana() {
            var ctx = new Ctx { R = { DiaActual = 17 } };
            var director = new NarrativeDirector(new List<NarrativeBeat> { Beat("CIN-2.3", 8, 16) });

            Assert.IsNull(director.BeatDeHoy(ctx.R, ctx));
            Assert.AreEqual(DecisionNarrativa.FueraDeVentana, director.Log[0].Resultado);
        }

        [Test]
        public void Un_obligatorio_espera_a_que_se_cumplan_sus_precondiciones() {
            var ctx = new Ctx();
            var beat = Beat("INT-1", 8, 12, PrioridadDeBeat.Obligatorio);
            beat.Precondiciones.Add("vecesQueSeFueACasa >= 3");
            var director = new NarrativeDirector(new List<NarrativeBeat> { beat });

            ctx.R.DiaActual = 10;
            Assert.IsNull(director.BeatDeHoy(ctx.R, ctx), "dentro de ventana, pero aun no toca");

            ctx.R.DiaActual = 18;                      // se paso la ventana
            ctx.R.VecesQueSeFueACasa = 3;              // y ahora si se cumple
            var salida = director.BeatDeHoy(ctx.R, ctx);

            Assert.IsNotNull(salida, "la ventana es ritmo, no un limite: mejor tarde que no contar la escena");
            Assert.IsTrue(salida.Retrasado);
        }

        [Test]
        public void Las_precondiciones_no_se_saltan_ni_para_un_obligatorio() {
            var ctx = new Ctx { R = { DiaActual = 30 } };
            var beat = Beat("INT-1", 8, 12, PrioridadDeBeat.Obligatorio);
            beat.Precondiciones.Add("vecesQueSeFueACasa >= 3");
            var director = new NarrativeDirector(new List<NarrativeBeat> { beat });

            Assert.IsNull(director.BeatDeHoy(ctx.R, ctx),
                          "disparar 'El Cuarto' a quien nunca se fue a casa no seria narrativa, seria un fallo");

            var perdidos = director.ObligatoriosQueNoSalieron(ctx.R);
            CollectionAssert.AreEqual(new[] { "INT-1" }, perdidos);
            Assert.IsTrue(director.Log.Any(l => l.Resultado == DecisionNarrativa.Perdido));
        }

        [Test]
        public void Como_maximo_un_beat_al_dia_y_los_obligatorios_van_primero() {
            var ctx = new Ctx();
            var director = new NarrativeDirector(new List<NarrativeBeat> {
                Beat("CIN-A", 1, 20),
                Beat("CIN-B", 1, 20, PrioridadDeBeat.Obligatorio),
                Beat("CIN-C", 1, 20)
            });

            Assert.AreEqual("CIN-B", director.BeatDeHoy(ctx.R, ctx).BeatId, "el obligatorio manda");

            ctx.R.DiaActual = 2;
            Assert.AreEqual("CIN-A", director.BeatDeHoy(ctx.R, ctx).BeatId, "y los demas esperan a mañana");

            ctx.R.DiaActual = 3;
            Assert.AreEqual("CIN-C", director.BeatDeHoy(ctx.R, ctx).BeatId);

            ctx.R.DiaActual = 4;
            Assert.IsNull(director.BeatDeHoy(ctx.R, ctx));
        }

        /// <summary>Emite el beat sobre un estado limpio, para poder probar varios coloreos del mismo beat.</summary>
        private static string VarianteCon(NarrativeBeat beat, double moral, double deuda) {
            var ctx = new Ctx();
            ctx.W.Set("MoralEquipo", moral);
            ctx.W.Set("DeudaTecnica", deuda);
            return new NarrativeDirector(new List<NarrativeBeat> { beat }).BeatDeHoy(ctx.R, ctx).Variante;
        }

        [Test]
        public void El_coloreo_elige_la_primera_expresion_cierta() {
            var beat = Beat("CIN-2.3", 1, 20);
            beat.Coloreo["MoralEquipo < 40"] = "variante_fria";
            beat.Coloreo["DeudaTecnica > 60"] = "variante_reproche";
            beat.Coloreo["default"] = "variante_base";

            Assert.AreEqual("variante_base", VarianteCon(beat, moral: 70, deuda: 10));
            Assert.AreEqual("variante_reproche", VarianteCon(beat, moral: 70, deuda: 80));
            Assert.AreEqual("variante_fria", VarianteCon(beat, moral: 20, deuda: 10));

            // Las dos ciertas a la vez: manda la primera del JSON, y por eso se conserva el orden.
            Assert.AreEqual("variante_fria", VarianteCon(beat, moral: 20, deuda: 80));
        }

        [Test]
        public void Sin_coloreo_sale_la_variante_por_defecto() {
            var ctx = new Ctx();
            var salida = new NarrativeDirector(new List<NarrativeBeat> { Beat("CIN-A", 1, 20) }).BeatDeHoy(ctx.R, ctx);
            Assert.AreEqual(NarrativeBeat.VarianteDefecto, salida.Variante, "una escena sin variante no se podria pintar");
        }

        [Test]
        public void El_canal_narrativo_es_determinista_y_no_toca_el_azar() {
            // Dos partidas con las mismas decisiones cuentan la misma historia. El azar decide que
            // crisis te toca, no quien eres.
            var catalogo = new List<NarrativeBeat> { Beat("CIN-A", 1, 20), Beat("CIN-B", 3, 20) };

            var primera = new List<string>();
            var segunda = new List<string>();

            foreach (var salida in new[] { primera, segunda }) {
                var ctx = new Ctx();
                var director = new NarrativeDirector(catalogo);
                for (var dia = 1; dia <= 10; dia++) {
                    ctx.R.DiaActual = dia;
                    var beat = director.BeatDeHoy(ctx.R, ctx);
                    if (beat != null) salida.Add($"d{dia}:{beat.BeatId}:{beat.Variante}");
                }
            }

            CollectionAssert.AreEqual(primera, segunda);
        }

        [Test]
        public void El_director_solo_cuenta_lo_que_esta_en_el_catalogo() {
            var ctx = new Ctx();
            var director = new NarrativeDirector(new List<NarrativeBeat> { Beat("CIN-A", 1, 20) });

            Assert.IsNull(director.PorId("CIN-INVENTADO"), "no puede CREAR lo que no existe");
            Assert.AreEqual("CIN-A", director.BeatDeHoy(ctx.R, ctx).BeatId);
            Assert.AreEqual(1, director.Catalogo.Count);
        }

        [Test]
        public void Los_beats_emitidos_viajan_en_el_estado_y_sobreviven_al_guardado() {
            var ctx = new Ctx();
            var director = new NarrativeDirector(new List<NarrativeBeat> { Beat("CIN-A", 1, 20) });
            director.BeatDeHoy(ctx.R, ctx);

            Assert.AreEqual(1, ctx.R.Ocurrencias[NarrativeDirector.PrefijoOcurrencia + "CIN-A"],
                            "no hace falta un campo nuevo en el guardado: Ocurrencias ya viaja");

            // un director nuevo sobre el mismo estado no lo repite
            var traRecargar = new NarrativeDirector(new List<NarrativeBeat> { Beat("CIN-A", 1, 20) });
            ctx.R.DiaActual = 5;
            Assert.IsNull(traRecargar.BeatDeHoy(ctx.R, ctx));
        }

        [Test]
        public void Un_catalogo_narrativo_con_ids_repetidos_lanza() {
            var ex = Assert.Throws<InvalidOperationException>(
                () => new NarrativeDirector(new List<NarrativeBeat> { Beat("CIN-A", 1, 5), Beat("CIN-A", 6, 9) }));
            StringAssert.Contains("CIN-A", ex.Message);
        }

        // ================================================================ el puente al cerrar

        [Test]
        public void El_volcado_al_cerrar_hace_cinco_snapshots_y_dos_contadores() {
            var w = new WorldState();
            w.Set("DeudaTecnica", 47.4);
            w.Set("MoralEquipo", 38.6);
            w.Set("SaludJugador", 62.2);
            w.Set("Cobertura", 50);
            w.Set("Documentacion", 30);
            w.Set("Reputacion", 55.5);

            var r = new RuntimeState { DiasConHorasExtra = 12, VecesQueSeFueACasa = 2 };
            var flags = Store();

            PuenteDeFlags.VolcarAlCerrar(w, r, flags);

            Assert.AreEqual(47.0, flags.Get("FLG_DEUDA_TECNICA"), Tol);
            Assert.AreEqual(39.0, flags.Get("FLG_MORAL_EQUIPO"), Tol);
            Assert.AreEqual(62.0, flags.Get("FLG_SALUD"), Tol);
            Assert.AreEqual(40.0, flags.Get("FLG_CALIDAD_ACUM"), Tol, "media de cobertura y documentacion");
            Assert.AreEqual(56.0, flags.Get("FLG_REPUTACION"), Tol);
            Assert.AreEqual(12.0, flags.Get("FLG_HORAS_EXTRA"), Tol);
            Assert.AreEqual(10.0, flags.Get("FLG_VIDA_EXTERNA"), Tol, "2 noches x 5");
        }

        [Test]
        public void Las_horas_extra_se_acumulan_entre_niveles_y_los_snapshots_no() {
            var flags = Store();
            var w = new WorldState();

            w.Set("DeudaTecnica", 20);
            PuenteDeFlags.VolcarAlCerrar(w, new RuntimeState { DiasConHorasExtra = 12 }, flags);

            w.Set("DeudaTecnica", 5);
            PuenteDeFlags.VolcarAlCerrar(w, new RuntimeState { DiasConHorasExtra = 9 }, flags);

            Assert.AreEqual(5.0, flags.Get("FLG_DEUDA_TECNICA"), Tol, "el snapshot es del ultimo nivel");
            Assert.AreEqual(21.0, flags.Get("FLG_HORAS_EXTRA"), Tol,
                            "el Dashboard dice 'horas extra acumuladas: 412', y eso son ocho niveles sumando");
        }

        // ================================================================ catalogos

        private const string NarrativaJson = @"{
  ""version"": 1,
  ""beats"": [
    { ""id"": ""CIN-1.4"", ""nombre"": ""El Primer Glitch"",
      ""ventana"": { ""diaMin"": 18, ""diaMax"": 20 },
      ""prioridad"": ""obligatorio"",
      ""coloreo"": { ""default"": ""variante_base"" } },
    { ""id"": ""CIN-2.3"", ""nombre"": ""La Cafetera"",
      ""ventana"": { ""diaMin"": 8, ""diaMax"": 16 },
      ""precondiciones"": [""vecesQueSeFueACasa >= 3""],
      ""prioridad"": ""opcional"",
      ""coloreo"": {
        ""MoralEquipo < 40"": ""variante_fria"",
        ""DeudaTecnica > 60"": ""variante_reproche"",
        ""default"": ""variante_base""
      } }
  ]
}";

        private const string FlagsJson = @"{
  ""version"": 1,
  ""flags"": [
    { ""id"": ""FLG_DEUDA_MORAL"", ""eje"": ""integridad"", ""inicial"": 0, ""min"": 0, ""max"": 20,
      ""noBaja"": true, ""descripcion"": ""NO BAJA NUNCA. Ni con buenas acciones. Es la tesis del juego."" },
    { ""id"": ""FLG_DEUDA_TECNICA"", ""eje"": ""competencia"", ""inicial"": 0, ""min"": 0, ""max"": 100,
      ""descripcion"": ""Snapshot de la deuda al cerrar cada nivel."" },
    { ""id"": ""FLG_EVIDENCIA"", ""eje"": ""poder"", ""inicial"": 0, ""sinTecho"": true,
      ""descripcion"": ""Piezas del expediente. Sin techo."" },
    { ""id"": ""FLG_SALUD"", ""eje"": ""persona"", ""inicial"": 80, ""min"": 0, ""max"": 100,
      ""descripcion"": ""Abre el final F11 (Burnout)."" }
  ]
}";

        [Test]
        public void Carga_narrativa_y_flags_desde_json() {
            var beats = CatalogLoader.CargarNarrativa(NarrativaJson);
            Assert.AreEqual(2, beats.Count);
            Assert.AreEqual(8, beats[1].Ventana.DiaMin);
            Assert.AreEqual(3, beats[1].Coloreo.Count);
            Assert.AreEqual("obligatorio", beats[0].Prioridad);

            var flags = CatalogLoader.CargarFlags(FlagsJson);
            Assert.AreEqual(4, flags.Count);
            Assert.IsTrue(flags[0].NoBaja);
            Assert.IsTrue(flags[2].SinTecho);

            // y el store funciona con el censo recien cargado
            var store = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), flags);
            store.Sumar("FLG_EVIDENCIA", 15);
            Assert.AreEqual(15.0, store.Get("FLG_EVIDENCIA"), Tol);
        }

        [Test]
        public void Una_expresion_de_coloreo_mal_escrita_no_se_carga() {
            var roto = NarrativaJson.Replace(@"""MoralEquipo < 40""", @"""MoralEquipoo < 40""");
            var ex = Assert.Throws<SchemaException>(() => CatalogLoader.CargarNarrativa(roto));
            StringAssert.Contains("MoralEquipoo", ex.Message);
        }

        [Test]
        public void Una_prioridad_o_una_ventana_imposibles_no_se_cargan() {
            var prioridad = NarrativaJson.Replace(@"""obligatorio""", @"""importante""");
            Assert.Throws<SchemaException>(() => CatalogLoader.CargarNarrativa(prioridad));

            var ventana = NarrativaJson.Replace(@"""diaMin"": 8, ""diaMax"": 16", @"""diaMin"": 16, ""diaMax"": 8");
            Assert.Throws<SchemaException>(() => CatalogLoader.CargarNarrativa(ventana));
        }

        [Test]
        public void El_censo_tiene_que_declarar_que_la_deuda_moral_no_baja() {
            var roto = FlagsJson.Replace(@"""noBaja"": true,", "");
            var ex = Assert.Throws<SchemaException>(() => CatalogLoader.CargarFlags(roto));
            StringAssert.Contains("tesis del juego", ex.Message);
        }

        [Test]
        public void Un_censo_sin_deuda_moral_no_se_carga() {
            var sinDeudaMoral = @"{ ""version"": 1, ""flags"": [
                { ""id"": ""FLG_SALUD"", ""eje"": ""persona"", ""inicial"": 80, ""min"": 0, ""max"": 100,
                  ""descripcion"": ""x"" } ] }";
            var ex = Assert.Throws<SchemaException>(() => CatalogLoader.CargarFlags(sinDeudaMoral));
            StringAssert.Contains("FLG_DEUDA_MORAL", ex.Message);
        }

        [Test]
        public void Un_flag_con_un_eje_inventado_o_sin_descripcion_no_se_carga() {
            var eje = FlagsJson.Replace(@"""eje"": ""poder""", @"""eje"": ""karma""");
            Assert.Throws<SchemaException>(() => CatalogLoader.CargarFlags(eje));

            var sinDescripcion = FlagsJson.Replace(
                @"""descripcion"": ""Piezas del expediente. Sin techo.""", @"""descripcion"": """"");
            var ex = Assert.Throws<SchemaException>(() => CatalogLoader.CargarFlags(sinDescripcion));
            StringAssert.Contains("documenta", ex.Message);
        }

        [Test]
        public void Un_flag_que_dice_sinTecho_y_declara_maximo_no_se_carga() {
            var roto = FlagsJson.Replace(@"""sinTecho"": true", @"""sinTecho"": true, ""max"": 15");
            Assert.Throws<SchemaException>(() => CatalogLoader.CargarFlags(roto));
        }
    }
}
