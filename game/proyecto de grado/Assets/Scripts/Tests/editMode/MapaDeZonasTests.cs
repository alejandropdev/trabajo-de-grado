using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nexus.Core.Datos;
using Nexus.Core.Guardado;
using Nexus.Core.Jornada;
using Nexus.Core.Modelo;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C11 · El mapa recorrible del nivel (§3.6).
    ///
    /// ⚠ No confundir con las zonas A–F de la Fase 1: aquéllas son la recolección con reloj de dos
    /// horas y no cambian nunca. Éste es un mapa PROPIO de cada nivel, que crece con la casta.
    ///
    /// La frase que ordena todo el módulo: *«Javier ya no es un turno, es un sitio al que ir.»*
    /// </summary>
    public class MapaDeZonasTests {
        /// <summary>El mapa de N1, que el §3.6 fija en tres zonas: escritorio, bullpen y cafetería.</summary>
        private static MapaDeZonas MapaDeN1() {
            return new MapaDeZonas {
                CosteBaseDeViaje = 20,
                Zonas = {
                    new ZonaDeNivel {
                        Id = "escritorio", Nombre = "Tu escritorio", EsAncla = true, MinutosDeVisita = 15,
                        QueDa = "Aqui llegan y se atienden las alertas. Correo y tickets."
                    },
                    new ZonaDeNivel {
                        Id = "bullpen", Nombre = "Bullpen", MinutosDeVisita = 30,
                        QueDa = "El equipo, la moral real, rumores de asignacion.",
                        QuienEsta = { "oscar" }, Coleccionables = { "COL-DEV-02" }
                    },
                    new ZonaDeNivel {
                        Id = "cafeteria", Nombre = "Cafeteria", MinutosDeVisita = 25,
                        QueDa = "Rumores, post-its, lo que nadie dice en el standup.",
                        Coleccionables = { "COL-POST-01" }
                    }
                },
                Costes = { { "escritorio>bullpen", 10 }, { "bullpen>cafeteria", 15 } }
            };
        }

        /// <summary>El mismo mapa mas las zonas de casta 2, para probar las puertas.</summary>
        private static MapaDeZonas MapaConPuertas() {
            var m = MapaDeN1();
            m.Zonas.Add(new ZonaDeNivel {
                Id = "data-hub", Nombre = "Data Hub -2", MinutosDeVisita = 40,
                QueDa = "Javier: la verdad tecnica y la social.",
                QuienEsta = { "javier" },
                Puerta = new PuertaDeZona { Precondiciones = { "diaActual >= 4" } }
            });
            m.Zonas.Add(new ZonaDeNivel {
                Id = "escondite", Nombre = "El escondite de Javier", MinutosDeVisita = 30,
                QueDa = "Las cintas, y Javier convertido en persona.",
                Puerta = new PuertaDeZona { TrasBeat = "CIN-2.3" }
            });
            m.Costes["escritorio>data-hub"] = 45;
            return m;
        }

        // ============================================================ el ancla

        [Test]
        public void Tu_escritorio_es_siempre_el_ancla() {
            var m = MapaDeN1();
            Assert.IsNotNull(m.Ancla);
            Assert.AreEqual("escritorio", m.Ancla.Id,
                            "es donde llegan las alertas y donde hay que volver a atenderlas");
        }

        [Test]
        public void Un_nivel_sin_mapa_es_valido_y_no_tiene_exploracion() {
            var m = new MapaDeZonas();
            Assert.IsTrue(m.Vacio);
            Assert.IsNull(m.Ancla);
            Assert.AreEqual(0, SchemaValidator.ValidarNivel(PerfilConMapa(m)).Count,
                            "un prologo puede no tener exploracion: todo ocurre en el escritorio");
        }

        // ============================================================ los costes

        [Test]
        public void Quedarse_donde_estas_es_gratis() {
            Assert.AreEqual(0, MapaDeN1().CosteEntre("bullpen", "bullpen"));
        }

        [Test]
        public void Un_par_declarado_cuesta_lo_que_dice_y_el_resto_el_coste_base() {
            var m = MapaDeN1();
            Assert.AreEqual(10, m.CosteEntre("escritorio", "bullpen"));
            Assert.AreEqual(15, m.CosteEntre("bullpen", "cafeteria"));
            Assert.AreEqual(20, m.CosteEntre("escritorio", "cafeteria"), "este par no esta declarado");
        }

        [Test]
        public void Los_costes_valen_en_las_dos_direcciones() {
            var m = MapaDeN1();
            Assert.AreEqual(10, m.CosteEntre("bullpen", "escritorio"),
                            "basta declarar cada par una vez");
            Assert.AreEqual(m.CosteEntre("bullpen", "cafeteria"), m.CosteEntre("cafeteria", "bullpen"));
        }

        [Test]
        public void Visitar_cuesta_el_viaje_mas_la_micro_escena() {
            var m = MapaDeN1();
            Assert.AreEqual(10 + 30, m.CosteDeVisitar("escritorio", "bullpen"));
            Assert.AreEqual(0 + 25, m.CosteDeVisitar("cafeteria", "cafeteria"),
                            "volver a mirar donde ya estas cuesta la escena, no el viaje");
        }

        [Test]
        public void Estar_lejos_es_un_riesgo_con_numero() {
            var m = MapaConPuertas();

            // Suena una alerta a las 11:00 con 3 h de ventana. ?Llegas desde el Data Hub?
            var desdeElDataHub = m.CosteEntre("data-hub", "escritorio");
            var desdeElBullpen = m.CosteEntre("bullpen", "escritorio");

            Assert.AreEqual(45, desdeElDataHub);
            Assert.AreEqual(10, desdeElBullpen);
            Assert.Greater(desdeElDataHub, desdeElBullpen,
                           "bajar a buscar a Javier tiene que costar de verdad, o no seria una decision");
        }

        [Test]
        public void El_coste_no_distingue_mayusculas_ni_al_buscar_la_zona() {
            var m = MapaDeN1();
            Assert.AreEqual(10, m.CosteEntre("ESCRITORIO", "Bullpen"));
            Assert.AreEqual("Bullpen", m.PorId("BULLPEN").Nombre);
        }

        [Test]
        public void Una_zona_que_no_existe_no_rompe_el_calculo() {
            var m = MapaDeN1();
            Assert.IsNull(m.PorId("catacumbas"));
            Assert.AreEqual(20, m.CosteEntre("escritorio", "catacumbas"), "cae al coste base");
            Assert.AreEqual(20, m.CosteDeVisitar("escritorio", "catacumbas"), "y no suma escena de una zona inexistente");
        }

        // ============================================================ las puertas

        [Test]
        public void Una_zona_sin_puerta_esta_siempre_abierta() {
            Assert.IsTrue(MapaDeN1().PorId("bullpen").Puerta.SiempreAbierta);
        }

        [Test]
        public void El_mapa_declara_las_puertas_pero_no_las_evalua() {
            var m = MapaConPuertas();

            var porEstado = m.PorId("data-hub").Puerta;
            Assert.IsFalse(porEstado.SiempreAbierta);
            CollectionAssert.AreEqual(new[] { "diaActual >= 4" }, porEstado.Precondiciones);

            var porNarrativa = m.PorId("escondite").Puerta;
            Assert.AreEqual("CIN-2.3", porNarrativa.TrasBeat,
                            "la cocina se abre tras CIN-2.3: el mapa crece con la historia, no solo con la casta");
        }

        // ============================================================ lo que da cada zona

        [Test]
        public void Cada_zona_dice_que_da_y_a_quien_esconde() {
            var m = MapaConPuertas();

            CollectionAssert.AreEqual(new[] { "javier" }, m.PorId("data-hub").QuienEsta);
            CollectionAssert.AreEqual(new[] { "COL-DEV-02" }, m.PorId("bullpen").Coleccionables);
            StringAssert.Contains("moral real", m.PorId("bullpen").QueDa);

            // Javier ya no es un turno, es un sitio al que ir: si no bajas, no te enteras.
            var zonasConJavier = m.Zonas.Where(z => z.QuienEsta.Contains("javier")).Select(z => z.Id);
            CollectionAssert.AreEqual(new[] { "data-hub" }, zonasConJavier);
        }

        // ============================================================ recorrerlo todo es imposible

        [Test]
        public void El_mapa_sabe_decir_lo_que_cuesta_recorrerlo_entero() {
            var m = MapaDeN1();

            // escritorio -> bullpen (10+30) -> cafeteria (15+25) -> escritorio (20) = 100
            Assert.AreEqual(100, m.MinutosParaRecorrerloTodo());
            Assert.AreEqual(0, new MapaDeZonas().MinutosParaRecorrerloTodo(), "un mapa vacio no cuesta nada");
        }

        [Test]
        public void Alejar_una_zona_encarece_el_recorrido_entero() {
            // ⚠ El motor NO impone que «recorrerlas todas sea imposible» (§3.6): eso es una propiedad
            // de los numeros que escriba el contenido. Lo que el motor da es la forma de VERLA al
            // balancear, y este test fija que la herramienta responde a los costes.
            var cerca = MapaConPuertas();
            var lejos = MapaConPuertas();
            lejos.CosteBaseDeViaje = 90;
            lejos.Costes["escritorio>data-hub"] = 150;

            Assert.AreEqual(210, cerca.MinutosParaRecorrerloTodo(), "con todo a mano: 3 h y media");
            Assert.AreEqual(420, lejos.MinutosParaRecorrerloTodo(),
                            "con el Data Hub donde el lore dice que esta: 7 h de una jornada de 10");
        }

        [Test]
        public void Un_atajo_que_nadie_usa_no_cambia_el_recorrido() {
            // Encarecer un tramo solo importa si la ruta optima pasaba por el. Es util saberlo al
            // balancear: subir un numero al azar puede no cambiar absolutamente nada.
            var m = MapaConPuertas();
            var antes = m.MinutosParaRecorrerloTodo();

            m.Costes["escritorio>data-hub"] = 300;

            Assert.AreEqual(antes, m.MinutosParaRecorrerloTodo(),
                            "la ruta mas barata nunca iba del escritorio al Data Hub directamente");
        }

        [Test]
        public void Cuanto_mas_lejos_estan_las_zonas_menos_caben_en_un_dia() {
            // El §3.5: «monitorear, recorrer y atender alertas compiten por los mismos minutos».
            //
            // Lo que se afirma aqui es la propiedad, no un numero concreto: encarecer los viajes
            // reduce cuantas zonas caben, y nunca al reves. Es lo unico que el motor garantiza;
            // decidir DONDE cae el corte es balanceo, y es cosa del contenido.
            const int atenderDosAlertas = 180;
            var caben = new List<int>();

            foreach (var costeDeViaje in new[] { 20, 60, 120, 200 }) {
                var m = MapaConPuertas();
                m.CosteBaseDeViaje = costeDeViaje;
                caben.Add(CuantasCabenHoy(m, atenderDosAlertas));
            }

            for (var i = 1; i < caben.Count; i++)
                Assert.LessOrEqual(caben[i], caben[i - 1],
                                   $"alejar las zonas nunca puede hacer que quepan MAS: {string.Join(" -> ", caben)}");

            Assert.AreEqual(4, caben[0], "con todo a mano caben las cuatro y no hay nada que priorizar");
            Assert.Less(caben[caben.Count - 1], 4,
                        "y con las zonas lejos de verdad hay que elegir a quien ver hoy");
            Assert.Greater(caben[caben.Count - 1], 0, "pero alguna si, o el mapa no serviria de nada");
        }

        /// <summary>Cuántas zonas se pueden visitar hoy reservando tiempo para el trabajo del día.</summary>
        private static int CuantasCabenHoy(MapaDeZonas m, int minutosDeTrabajo) {
            var reloj = new RelojDeJornada(new JornadaConfig());
            var zona = "escritorio";
            var visitadas = 0;

            foreach (var destino in new[] { "bullpen", "cafeteria", "data-hub", "escondite" }) {
                var ida = m.CosteDeVisitar(zona, destino);
                var vuelta = m.CosteEntre(destino, "escritorio");
                if (reloj.MinutosRestantes < ida + vuelta + minutosDeTrabajo) break;
                reloj.Avanzar(ida);
                zona = destino;
                visitadas++;
            }
            return visitadas;
        }

        // ============================================================ validacion

        private static LevelProfile PerfilConMapa(MapaDeZonas mapa) {
            var p = CatalogLoader.CargarPerfil(NivelJson);
            p.Mapa = mapa;
            return p;
        }

        private static List<string> Errores(Action<MapaDeZonas> romper) {
            var m = MapaDeN1();
            romper(m);
            return SchemaValidator.ValidarNivel(PerfilConMapa(m));
        }

        [Test]
        public void Un_mapa_sin_ancla_no_se_carga() {
            var e = Errores(m => m.PorId("escritorio").EsAncla = false);
            Assert.IsTrue(e.Any(x => x.Contains("ninguna zona es el ancla")), string.Join(" | ", e));
        }

        [Test]
        public void Un_mapa_con_dos_anclas_no_se_carga() {
            var e = Errores(m => m.PorId("bullpen").EsAncla = true);
            Assert.IsTrue(e.Any(x => x.Contains("solo puede haber una")), string.Join(" | ", e));
        }

        [Test]
        public void Una_zona_que_no_dice_que_da_no_se_carga() {
            var e = Errores(m => m.PorId("cafeteria").QueDa = null);
            Assert.IsTrue(e.Any(x => x.Contains("no dice que da")), string.Join(" | ", e));
        }

        [Test]
        public void Un_coste_que_menciona_una_zona_inexistente_no_se_carga() {
            var e = Errores(m => m.Costes["escritorio>catacumbas"] = 90);
            Assert.IsTrue(e.Any(x => x.Contains("catacumbas")), string.Join(" | ", e));
        }

        [Test]
        public void Una_clave_de_coste_mal_formada_no_se_carga() {
            var e = Errores(m => m.Costes["escritorio-bullpen"] = 30);
            Assert.IsTrue(e.Any(x => x.Contains("zonaA")), string.Join(" | ", e));
        }

        [Test]
        public void Una_puerta_con_una_precondicion_mal_escrita_no_se_carga() {
            var e = Errores(m => m.PorId("cafeteria").Puerta.Precondiciones.Add("diaActuall >= 4"));
            Assert.IsTrue(e.Any(x => x.Contains("diaActuall")), string.Join(" | ", e));
        }

        [Test]
        public void Ids_de_zona_repetidos_no_se_cargan() {
            var e = Errores(m => m.Zonas.Add(new ZonaDeNivel { Id = "bullpen", Nombre = "Otro", QueDa = "x" }));
            Assert.IsTrue(e.Any(x => x.Contains("repetida")), string.Join(" | ", e));
        }

        [Test]
        public void Un_mapa_de_mas_de_seis_zonas_avisa() {
            var e = Errores(m => {
                for (var i = 0; i < 5; i++)
                    m.Zonas.Add(new ZonaDeNivel { Id = "z" + i, Nombre = "Z" + i, QueDa = "algo" });
            });
            Assert.IsTrue(e.Any(x => x.Contains("de 3 a 6")), string.Join(" | ", e));
        }

        // ============================================================ datos

        [Test]
        public void El_mapa_sobrevive_a_un_viaje_por_json() {
            var original = MapaConPuertas();
            var json = JsonConvert.SerializeObject(original, JsonDeGuardado.Settings);
            var vuelta = JsonConvert.DeserializeObject<MapaDeZonas>(json, JsonDeGuardado.Settings);

            Assert.AreEqual(json, JsonConvert.SerializeObject(vuelta, JsonDeGuardado.Settings));
            Assert.AreEqual(5, vuelta.Zonas.Count);
            Assert.AreEqual("escritorio", vuelta.Ancla.Id);
            Assert.AreEqual(45, vuelta.CosteEntre("escritorio", "data-hub"));
            Assert.AreEqual("CIN-2.3", vuelta.PorId("escondite").Puerta.TrasBeat);
            Assert.AreEqual(45, vuelta.CosteEntre("ESCRITORIO", "DATA-HUB"),
                            "las claves de coste siguen sin distinguir mayusculas tras recargar");
        }

        [Test]
        public void El_mapa_se_clona_con_el_perfil_y_no_se_comparte() {
            var perfil = PerfilConMapa(MapaDeN1());
            var copia = perfil.Clone();

            copia.Mapa.PorId("bullpen").MinutosDeVisita = 999;
            copia.Mapa.Costes["escritorio>bullpen"] = 999;
            copia.Mapa.Zonas.Clear();

            Assert.AreEqual(30, perfil.Mapa.PorId("bullpen").MinutosDeVisita,
                            "el catalogo se carga una vez: una partida no puede reescribirlo");
            Assert.AreEqual(10, perfil.Mapa.CosteEntre("escritorio", "bullpen"));
            Assert.AreEqual(3, perfil.Mapa.Zonas.Count);
        }

        [Test]
        public void Las_zonas_visitadas_ya_viajan_en_el_estado() {
            var r = new RuntimeState { ZonaActual = "data-hub" };
            r.ZonasVisitadas["data-hub"] = 3;

            var json = JsonConvert.SerializeObject(r, JsonDeGuardado.Settings);
            var vuelta = JsonConvert.DeserializeObject<RuntimeState>(json, JsonDeGuardado.Settings);

            Assert.AreEqual("data-hub", vuelta.ZonaActual);
            Assert.AreEqual(3, vuelta.ZonasVisitadas["DATA-HUB"],
                            "y el contador de visitas es el que dispara CIN-2.3 a las tres bajadas");
        }

        private const string NivelJson = @"{
  ""id"": ""nivel-01"", ""nombre"": ""Cradle Lifts"",
  ""briefing"": [""Veinte dias.""],
  ""diasTotales"": 20, ""alcanceInicial"": 34, ""velocidadBase"": 3.0,
  ""volatilidadReal"": 25, ""nivelAndamiaje"": 3,
  ""metodologiasPermitidas"": [""scrum""],
  ""director"": { ""presupuestoDrama"": [1,3,2,1] },
  ""fase1"": {
    ""calidad"": { ""fichas"": 8, ""atributos"": [
      { ""id"": ""seguridad"", ""nombre"": ""Seguridad"", ""tagAfectado"": ""seguridad"" } ] },
    ""arquitecturas"": [
      { ""id"": ""monolito"", ""nombre"": ""Monolito"", ""esLaAdecuada"": true,
        ""veredicto"": ""correcta"", ""razon"": ""Tres patios no necesitan microservicios."" },
      { ""id"": ""micro"", ""nombre"": ""Microservicios"", ""esLaAdecuada"": false,
        ""veredicto"": ""incorrecta"", ""razon"": ""Confunde moda con criterio."" } ] } }";
    }
}
