using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Eventos;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// C10b · Catalogos. INV-5: validacion al cargar, o no se carga.
    ///
    /// La regla que se blinda aqui: un error de contenido tiene que doler al ARRANCAR, con el nombre
    /// del campo y el id del evento. Si no duele aqui, duele el dia 14 de una sesion de laboratorio
    /// de 90 minutos delante de veinte estudiantes, y entonces ya no hay nada que hacer.
    /// </summary>
    public class CatalogosTests {
        // ================================================================ JSON de referencia
        // Estos tres archivos son el formato real. Sirven de plantilla para el contenido de N0 y N1.

        private const string EventosJson = @"{
  ""version"": 1,
  ""eventos"": [
    {
      ""id"": ""EV-TEC-014"",
      ""nombre"": ""El servidor de integracion se cae"",
      ""tags"": [""tecnico"", ""devops""],
      ""fases"": [""desarrollo""],
      ""severidad"": 3,
      ""objetivoAprendizaje"": ""OA-DEVOPS-02"",
      ""precondiciones"": [""DeudaTecnica > 40"", ""diasDesde('EV-TEC-014') > 10""],
      ""pesoBase"": 10,
      ""modificadoresPeso"": [
        { ""variable"": ""DeudaTecnica"", ""curva"": ""lineal"",     ""factor"": 0.04 },
        { ""variable"": ""Cobertura"",    ""curva"": ""inversa"",    ""factor"": 0.02 },
        { ""variable"": ""Cansancio"",    ""curva"": ""cuadratica"", ""factor"": 0.03 }
      ],
      ""enfriamiento"": 8,
      ""maxOcurrencias"": 2,
      ""telegrafiado"": {
        ""diasAntes"": 2, ""canal"": ""log"",
        ""texto"": ""El build tardo 14 min. Ayer tardaba 6.""
      },
      ""opciones"": [
        {
          ""id"": ""A"",
          ""texto"": ""Parar la linea y arreglar el pipeline"",
          ""efectosInmediatos"": { ""Dias"": 2, ""DeudaTecnica"": -8, ""MoralEquipo"": 3 },
          ""efectosDiferidos"": [ { ""enDias"": 5, ""efectos"": { ""VelocidadMod"": ""+10%"" } } ],
          ""rubrica"": {
            ""veredicto"": ""correcta"", ""oa"": ""OA-DEVOPS-02"",
            ""razon"": ""Detener la linea ante un pipeline roto es la respuesta canonica.""
          }
        },
        {
          ""id"": ""B"",
          ""texto"": ""Compilar en local y seguir"",
          ""efectosInmediatos"": { ""DeudaTecnica"": 12 },
          ""efectosDiferidos"": [ { ""enDias"": 9, ""eventoForzado"": ""EV-TEC-021"" } ],
          ""rubrica"": {
            ""veredicto"": ""incorrecta"", ""oa"": ""OA-DEVOPS-02"",
            ""razon"": ""Perder el pipeline no retrasa el problema, lo esconde.""
          }
        }
      ]
    },
    {
      ""id"": ""EV-TEC-021"",
      ""nombre"": ""La integracion que nunca se hizo"",
      ""tags"": [""tecnico""],
      ""fases"": [""desarrollo"", ""lanzamiento""],
      ""severidad"": 4,
      ""objetivoAprendizaje"": ""OA-DEVOPS-02"",
      ""pesoBase"": 6,
      ""enfriamiento"": 10,
      ""maxOcurrencias"": 1,
      ""telegrafiado"": { ""diasAntes"": 1, ""canal"": ""chat"", ""texto"": ""Oscar: 'lleva dos dias sin integrar nadie'."" },
      ""opciones"": [
        {
          ""id"": ""A"", ""texto"": ""Parar un dia entero a integrar"",
          ""efectosInmediatos"": { ""Dias"": 1 },
          ""efectosDiferidos"": [ { ""enDias"": 3, ""efectos"": { ""DeudaTecnica"": -6 } } ],
          ""rubrica"": { ""veredicto"": ""correcta"", ""oa"": ""OA-DEVOPS-02"", ""razon"": ""Integrar pronto y a menudo."" }
        },
        {
          ""id"": ""B"", ""texto"": ""Integrar todo al final"",
          ""efectosInmediatos"": { ""DeudaTecnica"": 8 },
          ""efectosDiferidos"": [ { ""enDias"": 5, ""efectos"": { ""MoralEquipo"": -6 } } ],
          ""rubrica"": { ""veredicto"": ""incorrecta"", ""oa"": ""OA-DEVOPS-02"", ""razon"": ""El big bang de integracion nunca sale bien."" }
        }
      ]
    }
  ]
}";

        private const string NivelJson = @"{
  ""id"": ""nivel-01"",
  ""nombre"": ""Cradle Lifts"",
  ""briefing"": [
    ""Veinte dias habiles. Dos sprints."",
    ""Alguien hackeo la barrera de los patios."",
    ""No es un proyecto importante. Es una prueba.""
  ],
  ""diasTotales"": 20,
  ""presupuestoInicial"": 18000,
  ""alcanceInicial"": 34,
  ""equipoInicial"": 3,
  ""deudaHeredada"": 0,
  ""coberturaHeredada"": 40,
  ""documentacionHeredada"": 40,
  ""velocidadBase"": 3.0,
  ""volatilidadReal"": 25,
  ""objetivosActivos"": [""OA-DOC-01"", ""OA-DEVOPS-02""],
  ""metodologiasPermitidas"": [""scrum""],
  ""nivelAndamiaje"": 3,
  ""director"": {
    ""presupuestoDrama"": [1, 3, 2, 1],
    ""pesosPorTag"": { ""equipo"": 1.5, ""tecnico"": 0.8 },
    ""enfriamientoGlobal"": 2,
    ""maxEventosPorDia"": 1,
    ""ventanaTelegrafiado"": 2,
    ""multiplicadorSeveridad"": 1.0
  },
  ""umbrales"": {
    ""exito"": { ""avanceMinimo"": 28, ""coberturaMinima"": 30, ""deudaMaxima"": 60 },
    ""fallo"": { ""moral"": 10 }
  },
  ""fase1"": {
    ""calidad"": {
      ""fichas"": 8,
      ""textoPresion"": ""Ocho fichas. Lo que no cubras, te lo cobran."",
      ""atributos"": [
        { ""id"": ""seguridad"",   ""nombre"": ""Seguridad"",   ""tagAfectado"": ""seguridad"", ""textoSinInversion"": ""Nadie miro los accesos."" },
        { ""id"": ""fiabilidad"",  ""nombre"": ""Fiabilidad"",  ""tagAfectado"": ""tecnico"",   ""coeficienteAfectado"": ""alpha"", ""textoSinInversion"": ""Se cae los lunes."" },
        { ""id"": ""mantenibilidad"", ""nombre"": ""Mantenibilidad"", ""tagAfectado"": ""tecnico"", ""coeficienteAfectado"": ""kappa"", ""textoSinInversion"": ""Nadie lo entiende ya."" }
      ]
    },
    ""razonesDisponibles"": [
      { ""id"": ""es_un_dominio_pequeno"", ""texto"": ""El dominio es pequeno y conocido"" },
      { ""id"": ""el_equipo_ya_lo_conoce"", ""texto"": ""El equipo ya trabajo asi"" },
      { ""id"": ""es_lo_moderno"",          ""texto"": ""Es lo que se usa ahora"" }
    ],
    ""arquitecturas"": [
      {
        ""id"": ""monolito"", ""nombre"": ""Monolito modular"",
        ""efectos"": { ""Documentacion"": 5 },
        ""modificadoresModelo"": { ""alpha"": 0.9 },
        ""razonesValidas"": [""es_un_dominio_pequeno"", ""el_equipo_ya_lo_conoce""],
        ""razonesTrampa"": [""es_lo_moderno""],
        ""esLaAdecuada"": true,
        ""veredicto"": ""correcta"",
        ""razon"": ""Tres patios y una barrera no necesitan microservicios.""
      },
      {
        ""id"": ""microservicios"", ""nombre"": ""Microservicios"",
        ""efectos"": { ""DeudaTecnica"": 10, ""Dias"": 2 },
        ""modificadoresModelo"": { ""alpha"": 1.3 },
        ""razonesValidas"": [],
        ""razonesTrampa"": [""es_lo_moderno""],
        ""esLaAdecuada"": false,
        ""veredicto"": ""incorrecta"",
        ""razon"": ""Otro mas que confunde moda con criterio.""
      }
    ]
  }
}";

        private const string MetodologiaJson = @"{
  ""id"": ""scrum"",
  ""nombre"": ""Scrum"",
  ""familia"": ""agil"",
  ""resumen"": ""Iteraciones cortas, compromiso por sprint, inspeccion y adaptacion."",
  ""razonesValidas"": [""el_cliente_cambiara_de_opinion""],
  ""razonesTrampa"": [""es_lo_que_se_usa_ahora""],
  ""calendario"": { ""tipo"": ""iterativo"", ""etiquetaUnidad"": ""Sprint"", ""longitudIteracion"": 10, ""iteraciones"": 2 },
  ""ceremonias"": [
    { ""id"": ""daily"", ""nombre"": ""Daily"", ""cuando"": ""diario"", ""costeDias"": 0 },
    { ""id"": ""planning"", ""nombre"": ""Planning"", ""cuando"": ""inicioIteracion"", ""costeDias"": 0.5,
      ""verbo"": ""V5"", ""abreVentanaDeCambio"": true },
    { ""id"": ""retro"", ""nombre"": ""Retrospectiva"", ""cuando"": ""finIteracion"", ""costeDias"": 0.5,
      ""verbo"": ""V4"", ""ajustaCoeficiente"": true,
      ""acciones"": [
        { ""id"": ""documentar"", ""coeficiente"": ""kappa"", ""multiplicador"": 0.85,
          ""texto"": ""Documentar antes de cerrar tarea"",
          ""explicacion"": ""La cobertura y la documentacion dejan de diluirse tan rapido."" },
        { ""id"": ""refactor20"", ""coeficiente"": ""beta"", ""multiplicador"": 1.25,
          ""texto"": ""Reservar 20 % del sprint a refactor"",
          ""explicacion"": ""Cada esfuerzo de refactor quita mas deuda."" }
      ] }
  ],
  ""reglasDeCambio"": {
    ""costeMultiplicador"": 1.0,
    ""ventanas"": [""entreIteraciones""],
    ""textoEnVentana"": ""Entra en el sprint que viene."",
    ""penalizacionFueraDeVentana"": {
      ""permitido"": true, ""costeMultiplicador"": 2.0,
      ""efectosExtra"": { ""MoralEquipo"": -4 },
      ""texto"": ""Romper el sprint se paga.""
    }
  },
  ""modificadoresModelo"": { ""kappa"": 0.9 },
  ""modificadoresDirector"": { ""multiplicadorPesosPorTag"": { ""equipo"": 1.2 }, ""multiplicadorDrama"": 1.0 },
  ""tableroPrincipal"": ""burndown"",
  ""lanzamiento"": { ""factorRiesgo"": 0.9, ""entregaIncremental"": true, ""clienteYaVioElProducto"": true },
  ""rubricaCierre"": {
    ""practicas"": [
      { ""id"": ""ritmo"", ""descripcion"": ""Ritmo sostenible"", ""metrica"": ""diasConHorasExtra"",
        ""comparador"": ""<="", ""objetivo"": 4,
        ""razonSiCumple"": ""Mantuviste el ritmo."", ""razonSiFalla"": ""Quemaste al equipo para llegar."" },
      { ""id"": ""compromiso"", ""descripcion"": ""Compromiso realista"", ""metrica"": ""desviacionCompromisoPct"",
        ""comparador"": ""<="", ""objetivo"": 25,
        ""razonSiCumple"": ""Te comprometiste con lo que podias."", ""razonSiFalla"": ""Prometiste mas de lo que cabia."" }
    ]
  }
}";

        private static CatalogoEnMemoria FuenteValida() {
            return new CatalogoEnMemoria()
                .Con("eventos/eventos.json", EventosJson)
                .Con("niveles/nivel-01.json", NivelJson)
                .Con("metodologias/scrum.json", MetodologiaJson);
        }

        // ================================================================ carga

        [Test]
        public void Carga_un_catalogo_de_eventos_bien_formado() {
            var eventos = CatalogLoader.CargarEventos(EventosJson);

            Assert.AreEqual(2, eventos.Count);
            var ev = eventos[0];
            Assert.AreEqual("EV-TEC-014", ev.Id);
            CollectionAssert.AreEqual(new[] { "tecnico", "devops" }, ev.Tags);
            Assert.AreEqual(2, ev.Telegrafiado.DiasAntes);
            Assert.AreEqual(3, ev.ModificadoresPeso.Count);
            Assert.AreEqual("cuadratica", ev.ModificadoresPeso[2].Curva);
            Assert.AreEqual("incorrecta", ev.Opciones[1].Rubrica.Veredicto);
            Assert.AreEqual("EV-TEC-021", ev.Opciones[1].EfectosDiferidos[0].EventoForzado);
            Assert.AreEqual("+10%", ev.Opciones[0].EfectosDiferidos[0].Efectos["VelocidadMod"].ToString(),
                            "los porcentajes llegan como texto, no como numero");
        }

        [Test]
        public void Carga_un_perfil_de_nivel_bien_formado() {
            var p = CatalogLoader.CargarPerfil(NivelJson);

            Assert.AreEqual("nivel-01", p.Id);
            Assert.AreEqual(20, p.DiasTotales);
            Assert.AreEqual(3, p.Briefing.Length);
            Assert.AreEqual(25.0, p.VolatilidadReal, 1e-9);
            CollectionAssert.AreEqual(new[] { 1, 3, 2, 1 }, p.Director.PresupuestoDrama);
            Assert.AreEqual(1.5, p.Director.PesosPorTag["equipo"], 1e-9);
            Assert.AreEqual(2, p.Fase1.Arquitecturas.Count);
            Assert.IsTrue(p.Fase1.Arquitecturas[0].EsLaAdecuada);
            Assert.AreEqual(10.0, p.Umbrales.Fallo.Moral.Value, 1e-9);
            Assert.IsNull(p.Umbrales.Fallo.Dinero, "un umbral que el JSON no menciona es null, no cero");
            Assert.AreEqual(4, p.Coef.W.Length, "sin 'coef' en el JSON se quedan los valores por defecto");
        }

        [Test]
        public void Carga_una_metodologia_bien_formada_y_funciona() {
            var p = CatalogLoader.CargarMetodologia(MetodologiaJson);
            var reglas = new MethodologyRules(p, 20);

            Assert.AreEqual("scrum", p.Id);
            Assert.AreEqual(3, p.Ceremonias.Count);
            Assert.AreEqual(2, reglas.NumeroDeUnidades);
            Assert.IsTrue(reglas.PlanFor(10).Ceremonias.Any(c => c.Id == "retro"));
            Assert.IsTrue(reglas.EvaluarCambioDeAlcance(10).EnVentana);
            Assert.AreEqual(2.0, reglas.EvaluarCambioDeAlcance(5).CosteMultiplicador, 1e-9);
            Assert.AreEqual(0.85, p.Ceremonias[2].Acciones[0].Multiplicador, 1e-9);
        }

        [Test]
        public void CargarTodo_junta_las_tres_carpetas() {
            var catalogo = CatalogLoader.CargarTodo(FuenteValida());

            Assert.AreEqual(2, catalogo.Eventos.Count);
            Assert.AreEqual(1, catalogo.Niveles.Count);
            Assert.AreEqual(1, catalogo.Metodologias.Count);
            Assert.IsTrue(catalogo.Niveles.ContainsKey("nivel-01"));
            StringAssert.Contains("2 eventos", catalogo.ToString());
        }

        [Test]
        public void Los_diccionarios_del_json_siguen_sin_distinguir_mayusculas() {
            var p = CatalogLoader.CargarPerfil(NivelJson);
            Assert.IsTrue(p.Director.PesosPorTag.ContainsKey("EQUIPO"));

            var m = CatalogLoader.CargarMetodologia(MetodologiaJson);
            Assert.IsTrue(m.ModificadoresModelo.ContainsKey("KAPPA"));
        }

        [Test]
        public void Las_listas_no_se_duplican_al_cargar() {
            var p = CatalogLoader.CargarPerfil(NivelJson);
            Assert.AreEqual(4, p.Director.PresupuestoDrama.Length,
                            "ObjectCreationHandling.Replace: no se añaden a los valores por defecto");

            var m = CatalogLoader.CargarMetodologia(MetodologiaJson);
            Assert.AreEqual(1, m.ReglasDeCambio.Ventanas.Count,
                            "'ventanas' trae un valor por defecto en C#: sin Replace tendria dos");
            Assert.AreEqual("entreIteraciones", m.ReglasDeCambio.Ventanas[0]);
        }

        [Test]
        public void Un_json_roto_dice_donde_esta_el_error() {
            var ex = Assert.Throws<SchemaException>(
                () => CatalogLoader.CargarEventos(@"{ ""version"": 1, ""eventos"": [ { ""id"": "));
            StringAssert.Contains("eventos", ex.Message);
        }

        [Test]
        public void Una_lista_suelta_de_eventos_se_rechaza_explicando_la_forma() {
            var ex = Assert.Throws<SchemaException>(() => CatalogLoader.CargarEventos(@"[ { ""id"": ""EV-A"" } ]"));
            StringAssert.Contains("version", ex.Message);
        }

        [Test]
        public void Un_archivo_vacio_se_rechaza() {
            Assert.Throws<SchemaException>(() => CatalogLoader.CargarPerfil(""));
        }

        // ================================================================ INV-5 sobre eventos

        private static List<string> Errores(Action<EventDefinition> romper) {
            var eventos = CatalogLoader.CargarEventos(EventosJson);
            romper(eventos[0]);
            return SchemaValidator.ValidarEventos(eventos);
        }

        [Test]
        public void Un_evento_sin_telegrafiado_no_se_carga() {
            var e = Errores(ev => ev.Telegrafiado = null);
            Assert.IsTrue(e.Any(x => x.Contains("telegrafiado") && x.Contains("INV-3")), string.Join(" | ", e));
        }

        [Test]
        public void Un_telegrafiado_sin_texto_no_se_carga() {
            var e = Errores(ev => ev.Telegrafiado.Texto = "");
            Assert.IsTrue(e.Any(x => x.Contains("no dice nada")), string.Join(" | ", e));
        }

        [Test]
        public void Avisar_el_mismo_dia_no_es_avisar() {
            var e = Errores(ev => ev.Telegrafiado.DiasAntes = 0);
            Assert.IsTrue(e.Any(x => x.Contains("diasAntes")), string.Join(" | ", e));
        }

        [Test]
        public void Un_evento_con_una_sola_opcion_no_es_un_dilema() {
            var e = Errores(ev => ev.Opciones.RemoveAt(1));
            Assert.IsTrue(e.Any(x => x.Contains("dos opciones")), string.Join(" | ", e));
        }

        [Test]
        public void Una_opcion_sin_rubrica_no_se_carga() {
            var e = Errores(ev => ev.Opciones[1].Rubrica = null);
            Assert.IsTrue(e.Any(x => x.Contains("rubrica")), string.Join(" | ", e));
        }

        [Test]
        public void Una_rubrica_sin_razon_no_se_carga() {
            var e = Errores(ev => ev.Opciones[1].Rubrica.Razon = null);
            Assert.IsTrue(e.Any(x => x.Contains("porque")), string.Join(" | ", e));
        }

        [Test]
        public void Un_veredicto_inventado_no_se_carga() {
            var e = Errores(ev => ev.Opciones[1].Rubrica.Veredicto = "regular");
            Assert.IsTrue(e.Any(x => x.Contains("regular")), string.Join(" | ", e));
        }

        [Test]
        public void Una_cadena_que_apunta_a_un_evento_inexistente_no_se_carga() {
            var e = Errores(ev => ev.Opciones[1].EfectosDiferidos[0].EventoForzado = "EV-FANTASMA");
            Assert.IsTrue(e.Any(x => x.Contains("EV-FANTASMA")), string.Join(" | ", e));
        }

        [Test]
        public void Un_efecto_que_escribe_un_flag_narrativo_no_se_carga() {
            var e = Errores(ev => ev.Opciones[0].EfectosInmediatos["FLG_DEUDA_MORAL"] = 3);
            Assert.IsTrue(e.Any(x => x.Contains("INV-1")), string.Join(" | ", e));
        }

        [Test]
        public void Un_efecto_sobre_un_stock_que_no_existe_no_se_carga() {
            var e = Errores(ev => ev.Opciones[0].EfectosInmediatos["DeudaMoral"] = 3);
            Assert.IsTrue(e.Any(x => x.Contains("DeudaMoral")), string.Join(" | ", e));
        }

        [Test]
        public void Una_precondicion_con_una_variable_mal_escrita_no_se_carga() {
            var e = Errores(ev => ev.Precondiciones[0] = "DeudaTecnia > 40");
            Assert.IsTrue(e.Any(x => x.Contains("DeudaTecnia")), string.Join(" | ", e));
        }

        [Test]
        public void Una_precondicion_mal_escrita_se_caza_al_cargar_y_no_el_dia_14() {
            foreach (var mala in new[] { "DeudaTecnica = 40", "DeudaTecnica > 40 && Cobertura < 50",
                                         "(DeudaTecnica > 40", "inventada('x') > 1" }) {
                var e = Errores(ev => ev.Precondiciones[0] = mala);
                Assert.IsTrue(e.Count > 0, "deberia rechazar: " + mala);
            }
        }

        [Test]
        public void Una_precondicion_correcta_pasa_aunque_hoy_sea_falsa() {
            var e = Errores(ev => ev.Precondiciones[0] = "DeudaTecnica > 40 - riesgoLatente / 2");
            CollectionAssert.IsEmpty(e, "validar no es evaluar: solo se comprueba que los nombres existan");
        }

        [Test]
        public void Un_modificador_de_peso_con_una_curva_inventada_no_se_carga() {
            var e = Errores(ev => ev.ModificadoresPeso[0].Curva = "exponencial");
            Assert.IsTrue(e.Any(x => x.Contains("exponencial")), string.Join(" | ", e));
        }

        [Test]
        public void Ids_de_evento_repetidos_no_se_cargan() {
            var eventos = CatalogLoader.CargarEventos(EventosJson);
            eventos[1].Id = eventos[0].Id;
            Assert.IsTrue(SchemaValidator.ValidarEventos(eventos).Any(x => x.Contains("repetido")));
        }

        [Test]
        public void Un_evento_sin_ninguna_consecuencia_diferida_no_enseña_nada() {
            var e = Errores(ev => { foreach (var o in ev.Opciones) o.EfectosDiferidos.Clear(); });
            Assert.IsTrue(e.Any(x => x.Contains("prestamo")), string.Join(" | ", e));
        }

        [Test]
        public void La_severidad_va_de_cero_a_cinco_y_el_cero_vale() {
            CollectionAssert.IsEmpty(Errores(ev => ev.Severidad = 0), "las buenas noticias no consumen drama");
            Assert.IsTrue(Errores(ev => ev.Severidad = 6).Any(x => x.Contains("severidad")));
            Assert.IsTrue(Errores(ev => ev.Severidad = -1).Any(x => x.Contains("severidad")));
        }

        [Test]
        public void Una_fase_inventada_no_se_carga() {
            var e = Errores(ev => ev.Fases[0] = "precalentamiento");
            Assert.IsTrue(e.Any(x => x.Contains("precalentamiento")), string.Join(" | ", e));
        }

        // ================================================================ INV-5 sobre el nivel

        private static List<string> ErroresDeNivel(Action<LevelProfile> romper) {
            var p = CatalogLoader.CargarPerfil(NivelJson);
            romper(p);
            return SchemaValidator.ValidarNivel(p);
        }

        [Test]
        public void Un_nivel_de_menos_de_dos_dias_no_se_carga() {
            Assert.IsTrue(ErroresDeNivel(p => p.DiasTotales = 1).Any(x => x.Contains("diasTotales")));
        }

        [Test]
        public void Un_presupuesto_de_drama_que_no_tiene_cuatro_valores_no_se_carga() {
            Assert.IsTrue(ErroresDeNivel(p => p.Director.PresupuestoDrama = new[] { 1, 3 })
                          .Any(x => x.Contains("cuatro")));
        }

        [Test]
        public void Un_nivel_necesita_exactamente_una_arquitectura_adecuada() {
            Assert.IsTrue(ErroresDeNivel(p => p.Fase1.Arquitecturas[0].EsLaAdecuada = false)
                          .Any(x => x.Contains("ninguna arquitectura")));
            Assert.IsTrue(ErroresDeNivel(p => p.Fase1.Arquitecturas[1].EsLaAdecuada = true)
                          .Any(x => x.Contains("solo puede haber una")));
        }

        [Test]
        public void Un_nivel_sin_metodologias_permitidas_no_se_carga() {
            Assert.IsTrue(ErroresDeNivel(p => p.MetodologiasPermitidas = new string[0])
                          .Any(x => x.Contains("metodologiasPermitidas")));
        }

        [Test]
        public void Una_arquitectura_que_cita_una_razon_inexistente_no_se_carga() {
            Assert.IsTrue(ErroresDeNivel(p => p.Fase1.Arquitecturas[0].RazonesValidas[0] = "porque_si")
                          .Any(x => x.Contains("porque_si")));
        }

        [Test]
        public void Un_nivel_sin_briefing_no_se_carga() {
            Assert.IsTrue(ErroresDeNivel(p => p.Briefing = new string[0]).Any(x => x.Contains("briefing")));
        }

        [Test]
        public void Una_volatilidad_fuera_de_rango_no_se_carga() {
            Assert.IsTrue(ErroresDeNivel(p => p.VolatilidadReal = 140).Any(x => x.Contains("volatilidadReal")));
        }

        // ================================================================ INV-5 sobre la metodologia

        private static List<string> ErroresDeMetodologia(Action<MethodologyProfile> romper) {
            var p = CatalogLoader.CargarMetodologia(MetodologiaJson);
            romper(p);
            return SchemaValidator.ValidarMetodologia(p);
        }

        [Test]
        public void El_validador_delega_en_MethodologyRules_y_no_duplica_sus_reglas() {
            var e = ErroresDeMetodologia(p => p.Calendario.Tipo = "agil-ish");
            Assert.IsTrue(e.Any(x => x.Contains("agil-ish")), string.Join(" | ", e));

            var sinAcciones = ErroresDeMetodologia(p => p.Ceremonias[2].Acciones.Clear());
            Assert.IsTrue(sinAcciones.Any(x => x.Contains("retro")), string.Join(" | ", sinAcciones));
        }

        [Test]
        public void Una_practica_que_mide_una_metrica_inexistente_no_se_carga() {
            var e = ErroresDeMetodologia(p => p.RubricaCierre.Practicas[0].Metrica = "felicidad");
            Assert.IsTrue(e.Any(x => x.Contains("felicidad") && x.Contains("9 metricas")), string.Join(" | ", e));
        }

        [Test]
        public void Un_comparador_inventado_no_se_carga() {
            var e = ErroresDeMetodologia(p => p.RubricaCierre.Practicas[0].Comparador = "=~");
            Assert.IsTrue(e.Any(x => x.Contains("=~")), string.Join(" | ", e));
        }

        [Test]
        public void Una_razon_no_puede_ser_valida_y_trampa_a_la_vez() {
            var e = ErroresDeMetodologia(p => p.RazonesTrampa.Add("el_cliente_cambiara_de_opinion"));
            Assert.IsTrue(e.Any(x => x.Contains("a la vez")), string.Join(" | ", e));
        }

        [Test]
        public void Una_accion_de_retro_sin_explicacion_es_magia() {
            var e = ErroresDeMetodologia(p => p.Ceremonias[2].Acciones[0].Explicacion = null);
            Assert.IsTrue(e.Any(x => x.Contains("magia")), string.Join(" | ", e));
        }

        // ================================================================ el catalogo entero

        [Test]
        public void El_catalogo_valido_completo_carga_sin_un_solo_error() {
            CollectionAssert.IsEmpty(SchemaValidator.ValidarCatalogo(CatalogLoader.CargarTodo(FuenteValida())));
        }

        [Test]
        public void Un_nivel_que_permite_una_metodologia_que_no_existe_no_se_carga() {
            var fuente = new CatalogoEnMemoria()
                .Con("eventos/eventos.json", EventosJson)
                .Con("niveles/nivel-01.json", NivelJson.Replace(@"""scrum""", @"""xp"""))
                .Con("metodologias/scrum.json", MetodologiaJson);

            var ex = Assert.Throws<SchemaException>(() => CatalogLoader.CargarTodo(fuente));
            StringAssert.Contains("xp", ex.Message);
        }

        [Test]
        public void CargarTodo_reune_TODOS_los_errores_en_un_solo_mensaje() {
            var eventosRotos = EventosJson
                .Replace(@"""severidad"": 3", @"""severidad"": 9")
                .Replace(@"""maxOcurrencias"": 2", @"""maxOcurrencias"": 0");
            var nivelRoto = NivelJson.Replace(@"""diasTotales"": 20", @"""diasTotales"": 1");

            var fuente = new CatalogoEnMemoria()
                .Con("eventos/eventos.json", eventosRotos)
                .Con("niveles/nivel-01.json", nivelRoto)
                .Con("metodologias/scrum.json", MetodologiaJson);

            var ex = Assert.Throws<SchemaException>(() => CatalogLoader.CargarTodo(fuente));

            Assert.GreaterOrEqual(ex.Errores.Count, 3,
                                  "quien escribe contenido tiene que ver sus seis errores en el primer intento, " +
                                  "no arrancar el juego seis veces");
            StringAssert.Contains("severidad", ex.Message);
            StringAssert.Contains("maxOcurrencias", ex.Message);
            StringAssert.Contains("diasTotales", ex.Message);
        }

        [Test]
        public void Un_catalogo_sin_niveles_o_sin_metodologias_no_arranca() {
            var soloEventos = new CatalogoEnMemoria().Con("eventos/eventos.json", EventosJson);
            var ex = Assert.Throws<SchemaException>(() => CatalogLoader.CargarTodo(soloEventos));
            StringAssert.Contains("nivel", ex.Message);
        }

        // ================================================================ las fuentes

        [Test]
        public void CatalogoEnMemoria_lista_solo_los_hijos_directos_y_en_orden() {
            var fuente = new CatalogoEnMemoria()
                .Con("eventos/b.json", "{}").Con("eventos/a.json", "{}")
                .Con("eventos/sub/c.json", "{}").Con("niveles/n.json", "{}");

            CollectionAssert.AreEqual(new[] { "eventos/a.json", "eventos/b.json" },
                                      fuente.ListarCatalogos("eventos"));
            Assert.IsTrue(fuente.Existe("niveles/n.json"));
            Assert.IsFalse(fuente.Existe("niveles/z.json"));
            Assert.Throws<FileNotFoundException>(() => fuente.LeerCatalogo("niveles/z.json"));
        }

        [Test]
        public void CatalogoDeArchivos_lee_y_lista_en_orden_estable() {
            var carpeta = Path.Combine(Path.GetTempPath(), "nexus-catalogo-" + Guid.NewGuid().ToString("N"));
            try {
                Directory.CreateDirectory(Path.Combine(carpeta, "eventos"));
                File.WriteAllText(Path.Combine(carpeta, "eventos", "eventos.json"), EventosJson);
                Directory.CreateDirectory(Path.Combine(carpeta, "niveles"));
                File.WriteAllText(Path.Combine(carpeta, "niveles", "nivel-01.json"), NivelJson);
                Directory.CreateDirectory(Path.Combine(carpeta, "metodologias"));
                File.WriteAllText(Path.Combine(carpeta, "metodologias", "scrum.json"), MetodologiaJson);

                var fuente = new CatalogoDeArchivos(carpeta);
                var catalogo = CatalogLoader.CargarTodo(fuente);

                Assert.AreEqual(2, catalogo.Eventos.Count);
                Assert.AreEqual(1, catalogo.Niveles.Count);
                CollectionAssert.AreEqual(new[] { "eventos/eventos.json" }, fuente.ListarCatalogos("eventos"));
            } finally {
                if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            }
        }

        [Test]
        public void Un_catalogo_no_puede_salirse_de_su_carpeta() {
            var fuente = new CatalogoDeArchivos(Path.GetTempPath());
            Assert.Throws<ArgumentException>(() => fuente.LeerCatalogo("../secretos.json"));
            Assert.Throws<ArgumentException>(() => fuente.LeerCatalogo("C:/Windows/system.ini"));
        }
    }
}
