using System.Collections.Generic;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Minijuegos;
using Nexus.Core.Relaciones;
using Nexus.Core.Tablero;

namespace Nexus.Tests {
    public class AndamiajeYRelojTests {
        private static MiembroDelEquipo Miembro(string id, string nombre, string habilidad, double valor) {
            return new MiembroDelEquipo {
                Id = id, Nombre = nombre,
                Habilidades = new Dictionary<string, double> { { habilidad, valor } }
            };
        }

        private static Personaje Sarah() {
            return new Personaje {
                Id = "sarah", Nombre = "Sarah",
                Ayudas = new List<AyudaDePersonaje> {
                    new AyudaDePersonaje { Umbral = 7, Tipo = TiposDeAyuda.MostrarDependencias },
                    new AyudaDePersonaje { Umbral = 2, Tipo = TiposDeAyuda.MinutosExtra }
                }
            };
        }

        [Test]
        public void Sin_equipo_ni_confianza_el_reto_se_queda_en_la_base() {
            var r = AndamiajeDelReto.Efectivo(2, Verbos.Ordenar, null, null, null);
            Assert.AreEqual(2, r.Nivel);
            Assert.IsFalse(r.Subio);
        }

        [Test]
        public void Un_companero_fuerte_en_logica_sube_el_ordenar_a_tres_y_se_sabe_quien() {
            var equipo = new[] { Miembro("ana", "Ana", "logica", 1.35), Miembro("leo", "Leo", "logica", 0.9) };
            var r = AndamiajeDelReto.Efectivo(2, "ordenar", equipo, null, null);
            Assert.AreEqual(3, r.Nivel);
            Assert.AreEqual("Ana", r.Quien);
            Assert.AreEqual("equipo", r.Via);
        }

        [Test]
        public void El_companero_solo_ayuda_en_lo_suyo() {
            var equipo = new[] { Miembro("ana", "Ana", "logica", 1.35) };
            Assert.AreEqual(2, AndamiajeDelReto.Efectivo(2, Verbos.Detectar, equipo, null, null).Nivel);
        }

        [Test]
        public void Trabajando_solo_no_hay_quien_ayude() {
            var tu = Miembro(Nexus.Core.Fase1.CatalogoDeObjetos3D.IdDeTuSolo, "Tú", "logica", 2.0);
            Assert.AreEqual(2, AndamiajeDelReto.Efectivo(2, Verbos.Ordenar, new[] { tu }, null, null).Nivel);
        }

        [Test]
        public void La_confianza_con_quien_sabe_del_reto_tambien_lo_sube() {
            var personajes = new[] { Sarah() };
            Assert.AreEqual(2, AndamiajeDelReto.Efectivo(2, Verbos.Ordenar, null, personajes, id => 6).Nivel, "a 6 aun no");
            var r = AndamiajeDelReto.Efectivo(2, Verbos.Ordenar, null, personajes, id => 7);
            Assert.AreEqual(3, r.Nivel);
            Assert.AreEqual("Sarah", r.Quien);
            Assert.AreEqual("confianza", r.Via);
        }

        [Test]
        public void Los_minutos_extra_no_cuentan_como_saber_del_reto() {
            // Sarah ofrece +45 s desde confianza 2, pero eso no la hace experta en detectar.
            Assert.AreEqual(2, AndamiajeDelReto.Efectivo(2, Verbos.Detectar, null, new[] { Sarah() }, id => 20).Nivel);
        }

        [Test]
        public void Nunca_pasa_de_tres() {
            var equipo = new[] { Miembro("ana", "Ana", "logica", 1.35) };
            Assert.AreEqual(3, AndamiajeDelReto.Efectivo(3, Verbos.Ordenar, equipo, null, null).Nivel);
            Assert.IsFalse(AndamiajeDelReto.Efectivo(3, Verbos.Ordenar, equipo, null, null).Subio);
        }

        [Test]
        public void Al_formar_el_equipo_se_dice_en_que_ayuda_cada_uno() {
            var ana = new MiembroDelEquipo {
                Id = "ana", Nombre = "Ana",
                Habilidades = new Dictionary<string, double> { { "pruebas", 1.4 }, { "logica", 1.2 }, { "datos", 0.9 } }
            };
            CollectionAssert.AreEqual(new[] { "revisar diagramas y cambios", "ordenar el backlog" }, AndamiajeDelReto.EnQueAyuda(ana));
            Assert.IsEmpty(AndamiajeDelReto.EnQueAyuda(Miembro("leo", "Leo", "datos", 1.0)));
        }

        [Test]
        public void El_reloj_escala_con_la_dificultad_y_no_baja_de_treinta() {
            Assert.AreEqual(135f, RelojDelReto.Segundos(90, Preferencias.Facil));
            Assert.AreEqual(90f, RelojDelReto.Segundos(90, Preferencias.Normal));
            Assert.AreEqual(63f, RelojDelReto.Segundos(90, Preferencias.Dificil));
            Assert.AreEqual(90f, RelojDelReto.Segundos(90, "inventada"));
            Assert.AreEqual(RelojDelReto.Minimo, RelojDelReto.Segundos(20, Preferencias.Dificil));
        }

        [Test]
        public void Las_preferencias_nuevas_tienen_valores_sensatos_y_se_acotan() {
            var p = new Preferencias();
            Assert.AreEqual(Preferencias.Normal, p.dificultad);
            Assert.IsFalse(p.invertirY);
            var raras = new Preferencias { dificultad = "imposible", volumenMusica = 4, sensibilidadRaton = 0 }.Acotadas();
            Assert.AreEqual(Preferencias.Normal, raras.dificultad);
            Assert.AreEqual(1.0, raras.volumenMusica);
            Assert.AreEqual(Preferencias.SensibilidadMinima, raras.sensibilidadRaton);
        }

        [Test]
        public void Las_preferencias_viejas_sin_los_campos_nuevos_cargan_con_los_de_por_defecto() {
            var almacen = new AlmacenEnMemoria();
            almacen.Guardar(PreferenciasStore.Clave, "{\"velocidadDeSimulacion\":2,\"volumenGeneral\":0.5}");
            var p = new PreferenciasStore(almacen).Cargar();
            Assert.AreEqual(Preferencias.Normal, p.dificultad);
            Assert.AreEqual(0.7, p.volumenMusica);
            Assert.AreEqual(1.0, p.sensibilidadRaton);
        }
    }
}
