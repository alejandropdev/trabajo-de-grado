using System;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Despliegue;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// La Fase 3 rediseñada (2026-11-09): la tuberia se PRUEBA y la luz se detiene en el primer fallo (RevisorDeTuberia);
    /// los modulos se juegan en cuartos a oscuras, con dos tomas por cable (la correcta y un señuelo); y las pruebas
    /// fallidas de la tuberia cuestan riesgo.
    /// </summary>
    public class TuberiaYCuartosTests {
        private static Catalogo _catalogo;

        private static Catalogo Cat() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static DespliegueDef Def(string nivel) { return Cat().Niveles[nivel].Despliegue; }
        private static System.Collections.Generic.IEnumerable<string> Niveles() {
            return Cat().Niveles.Values.Where(n => n.Despliegue != null).Select(n => n.Id);
        }

        /// <summary>La tuberia correcta, rellena con huecos vacios hasta el numero de huecos.</summary>
        private static System.Collections.Generic.List<string> Huecos(DespliegueDef def) {
            var h = EvaluadorDeDespliegue.TuberiaCorrecta(def);
            while (h.Count < def.Tuberia.Huecos) h.Add(null);
            return h;
        }

        // ================================================================ el revisor de la tuberia

        [Test]
        public void Una_tuberia_correcta_deja_pasar_la_luz_hasta_el_cliente() {
            foreach (var nivel in Niveles()) {
                var def = Def(nivel);
                var f = RevisorDeTuberia.Revisar(def, Huecos(def));
                Assert.IsTrue(f.Completa, nivel);
                Assert.AreEqual(-1, f.PrimerFallo, nivel);
                Assert.IsEmpty(f.Faltan, nivel);
                Assert.IsTrue(f.Secciones.All(s => s.Estado == EstadoDeSeccion.Bien || s.Estado == EstadoDeSeccion.Vacia), nivel);
            }
        }

        [Test]
        public void La_luz_se_detiene_en_el_primer_fallo_y_lo_que_hay_despues_queda_apagado_aunque_este_bien() {
            var def = Def("nivel-01");
            var h = Huecos(def);
            // Se cambia la 3.ª etapa por un atajo: las siguientes siguen siendo correctas, pero ya no se miran.
            var trampa = def.Tuberia.Tramos.First(t => t.Trampa).Id;
            h[2] = trampa;
            var f = RevisorDeTuberia.Revisar(def, h);
            Assert.IsFalse(f.Completa);
            Assert.AreEqual(2, f.PrimerFallo);
            Assert.AreEqual(EstadoDeSeccion.Bien, f.Secciones[0].Estado);
            Assert.AreEqual(EstadoDeSeccion.Bien, f.Secciones[1].Estado);
            Assert.AreEqual(EstadoDeSeccion.Mal, f.Secciones[2].Estado);
            StringAssert.Contains("atajo", f.Secciones[2].Motivo);
            for (var i = 3; i < f.Secciones.Count; i++)
                Assert.AreEqual(EstadoDeSeccion.SinRevisar, f.Secciones[i].Estado, "hueco " + (i + 1) + ": despues del fallo, apagado");
        }

        [Test]
        public void Una_etapa_antes_de_la_que_necesita_rompe_el_flujo_en_la_primera_de_las_dos() {
            var def = Def("nivel-01");
            var h = Huecos(def);
            // «Compilar» es la primera: se intercambia con la segunda. La segunda (ahora primera) necesita «Compilar» antes.
            var a = h[0]; h[0] = h[1]; h[1] = a;
            var f = RevisorDeTuberia.Revisar(def, h);
            Assert.AreEqual(0, f.PrimerFallo);
            StringAssert.Contains("antes de", f.Secciones[0].Motivo);
            Assert.IsTrue(f.Secciones.Skip(1).All(s => s.Estado == EstadoDeSeccion.SinRevisar));
        }

        [Test]
        public void Un_hueco_vacio_falla_mientras_falten_obligatorias() {
            var def = Def("nivel-01");
            var h = Huecos(def);
            var fuera = h[3];
            h[3] = null;   // un hueco vacio en medio, con una obligatoria sin poner en ningun sitio
            var f = RevisorDeTuberia.Revisar(def, h);
            Assert.AreEqual(3, f.PrimerFallo);
            CollectionAssert.Contains(f.Faltan, fuera);
            StringAssert.Contains("falta", f.Secciones[3].Motivo.ToLowerInvariant());
        }

        [Test]
        public void Con_todas_las_obligatorias_puestas_un_hueco_sin_llenar_se_atraviesa_apagado() {
            // Nivel 0: 6 huecos y 5 obligatorias: el sexto se puede dejar vacio.
            var def = Def("nivel-00");
            var h = EvaluadorDeDespliegue.TuberiaCorrecta(def);
            Assert.Less(h.Count, def.Tuberia.Huecos, "hay mas huecos que obligatorias");
            while (h.Count < def.Tuberia.Huecos) h.Add(null);
            var f = RevisorDeTuberia.Revisar(def, h);
            Assert.IsTrue(f.Completa);
            Assert.AreEqual(EstadoDeSeccion.Vacia, f.Secciones.Last().Estado);
        }

        [Test]
        public void Una_dependencia_obligatoria_que_no_esta_en_la_tuberia_tambien_rompe_el_flujo() {
            var def = Def("nivel-01");
            var h = Huecos(def);
            var sinCompilar = h.Select(x => x == "compilar" ? "directo" : x).ToList();
            var f = RevisorDeTuberia.Revisar(def, sinCompilar);
            Assert.IsFalse(f.Completa);
            Assert.AreEqual(h.IndexOf("compilar"), f.PrimerFallo);
        }

        [Test]
        public void Toda_tuberia_que_el_revisor_da_por_completa_es_un_bien_para_el_evaluador() {
            foreach (var nivel in Niveles()) {
                var def = Def(nivel);
                var huecos = Huecos(def);
                Assert.IsTrue(RevisorDeTuberia.Revisar(def, huecos).Completa, nivel);
                var plan = new PlanDeDespliegue { Dia = 1, EsFinal = true };
                var res = EvaluadorDeDespliegue.Evaluar(def, null, plan, new RespuestaDeDespliegue { Tuberia = huecos });
                Assert.IsTrue(res.Renglones.Where(r => r.Que == "tuberia").All(r => r.Estado == EstadosDeDespliegue.Bien), nivel);
            }
        }

        // ================================================================ los cuartos a oscuras

        [Test]
        public void Cada_cable_ofrece_dos_tomas_la_correcta_y_un_senuelo_distinto() {
            foreach (var nivel in Niveles()) {
                var def = Def(nivel);
                foreach (var kv in def.Modulos)
                    foreach (var cable in kv.Value) {
                        var o = def.OpcionesDeCable(kv.Key, cable);
                        if (cable.EsDependencia) { Assert.IsEmpty(o, nivel + "/" + kv.Key + "/" + cable.Id); continue; }
                        Assert.AreEqual(2, o.Count, nivel + "/" + kv.Key + "/" + cable.Id);
                        CollectionAssert.Contains(o, cable.Toma);
                        Assert.AreNotEqual(o[0], o[1]);
                        Assert.IsNotNull(def.Toma(o[0])); Assert.IsNotNull(def.Toma(o[1]));
                        Assert.IsTrue(def.Toma(o.First(t => t != cable.Toma)).Trampa, nivel + "/" + cable.Id + ": el señuelo es una toma trampa");
                    }
            }
        }

        [Test]
        public void El_orden_de_las_tomas_es_estable_y_la_correcta_no_siempre_cae_del_mismo_lado() {
            var def = Def("nivel-01");
            var todas = def.Modulos.SelectMany(kv => kv.Value.Where(c => !c.EsDependencia).Select(c => new { modulo = kv.Key, cable = c })).ToList();
            var lados = todas.Select(x => def.OpcionesDeCable(x.modulo, x.cable)[0] == x.cable.Toma).ToList();
            CollectionAssert.AreEqual(lados, todas.Select(x => def.OpcionesDeCable(x.modulo, x.cable)[0] == x.cable.Toma).ToList());
            Assert.IsTrue(lados.Contains(true) && lados.Contains(false), "la buena sale a veces a la izquierda y a veces a la derecha");
        }

        [Test]
        public void Sin_alternativa_el_senuelo_sale_de_la_primera_toma_trampa_distinta_de_la_correcta() {
            var def = Def("nivel-01");
            var cable = new CableDef { Id = "x", Nombre = "x", Toma = "bd-produccion", Alternativa = null, Porque = "x" };
            var o = def.OpcionesDeCable("acceso", cable);
            Assert.AreEqual(2, o.Count);
            var senuelo = o.First(t => t != "bd-produccion");
            Assert.IsTrue(def.Toma(senuelo).Trampa);
            cable.Alternativa = "no-existe";
            Assert.AreEqual(senuelo, def.OpcionesDeCable("acceso", cable).First(t => t != "bd-produccion"), "una alternativa que no existe se ignora");
        }

        [Test]
        public void El_contenido_real_trae_un_senuelo_por_cada_cable() {
            // CargarTodo ya ejecuta el validador de esquema: si una alternativa no existiera, el catalogo no cargaria.
            foreach (var nivel in Niveles()) {
                var def = Def(nivel);
                foreach (var kv in def.Modulos)
                    foreach (var c in kv.Value.Where(x => !x.EsDependencia))
                        Assert.IsFalse(string.IsNullOrEmpty(c.Alternativa), nivel + "/" + kv.Key + "/" + c.Id + ": cada cable trae su señuelo");
            }
        }

        // ================================================================ la dificultad de las tomas (ronda 2)

        private static string EntornoDe(DespliegueDef def, string toma) { var t = def.Toma(toma); return t == null ? "produccion" : t.Entorno; }

        [Test]
        public void El_primer_cuarto_de_cada_nivel_sigue_siendo_pruebas_contra_produccion() {
            foreach (var nivel in Niveles()) {
                var def = Def(nivel);
                var primero = def.Modulos.First().Value.First(c => !c.EsDependencia);
                Assert.AreEqual("produccion", EntornoDe(def, primero.Toma), nivel);
                Assert.AreEqual("pruebas", EntornoDe(def, primero.Alternativa), nivel + ": el primer cuarto es el fácil");
            }
        }

        [Test]
        public void En_cada_nivel_hay_un_cable_donde_lo_correcto_es_pruebas() {
            foreach (var nivel in Niveles()) {
                var def = Def(nivel);
                var cables = def.Modulos.SelectMany(kv => kv.Value).Where(c => !c.EsDependencia).ToList();
                var deEnsayo = cables.Where(c => EntornoDe(def, c.Toma) == "pruebas").ToList();
                Assert.IsNotEmpty(deEnsayo, nivel + ": elegir siempre producción no puede bastar");
                foreach (var c in deEnsayo) {
                    Assert.IsFalse(def.Toma(c.Toma).Trampa, nivel + ": la toma de ensayo correcta no es una trampa");
                    Assert.AreEqual("produccion", EntornoDe(def, c.Alternativa), nivel + ": su señuelo dice PRODUCCIÓN");
                }
            }
        }

        [Test]
        public void Fuera_del_primer_cuarto_la_mayoria_de_senuelos_parecen_tan_reales_como_la_correcta() {
            foreach (var nivel in Niveles()) {
                var def = Def(nivel);
                var cables = def.Modulos.SelectMany(kv => kv.Value).Where(c => !c.EsDependencia).Skip(1).ToList();
                var parecidos = cables.Count(c => EntornoDe(def, c.Alternativa) == EntornoDe(def, c.Toma));
                Assert.GreaterOrEqual(parecidos * 2, cables.Count, nivel + ": al menos la mitad de los señuelos comparten rótulo con la correcta");
            }
        }

        [Test]
        public void El_cable_de_ensayo_se_evalua_como_bien_y_su_senuelo_como_mal() {
            foreach (var nivel in Niveles()) {
                var def = Def(nivel);
                var kv = def.Modulos.First(m => m.Value.Any(c => c.Id == "ensayo"));
                var cable = kv.Value.First(c => c.Id == "ensayo");
                Assert.AreEqual("entorno-de-ensayo", cable.Toma, nivel);
                Assert.IsTrue(def.OpcionesDeCable(kv.Key, cable).SequenceEqual(def.OpcionesDeCable(kv.Key, cable)), nivel + ": orden estable");
                Assert.IsTrue(def.Toma(cable.Alternativa).Trampa, nivel + ": producción directa es una trampa");
            }
        }

        // ================================================================ las pruebas fallidas cuestan

        [Test]
        public void Cada_prueba_fallida_de_la_tuberia_suma_riesgo_con_un_tope_y_no_cambia_el_nivel() {
            var def = Def("nivel-01");
            var plan = new PlanDeDespliegue { Dia = 1, EsFinal = true };
            Func<int, double> riesgo = intentos => {
                var r = new RespuestaDeDespliegue { Tuberia = Huecos(def), IntentosFallidosDeTuberia = intentos };
                return EvaluadorDeDespliegue.Evaluar(def, null, plan, r).Riesgo;
            };
            var base0 = riesgo(0);
            Assert.AreEqual(base0 + 2, riesgo(1), 1e-9);
            Assert.AreEqual(base0 + 6, riesgo(3), 1e-9);
            Assert.AreEqual(base0 + EvaluadorDeDespliegue.TopeDeRiesgoPorIntentos, riesgo(40), 1e-9, "tope");
            var res = EvaluadorDeDespliegue.Evaluar(def, null, plan, new RespuestaDeDespliegue { Tuberia = Huecos(def), IntentosFallidosDeTuberia = 4 });
            Assert.AreEqual(4, res.IntentosFallidos);
            Assert.AreEqual("bien", res.Nivel, "el nivel sale de como quedo, no de los tropiezos");
        }
    }
}
