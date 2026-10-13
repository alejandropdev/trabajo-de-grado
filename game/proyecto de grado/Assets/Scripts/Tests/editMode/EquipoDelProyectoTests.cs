using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Core.Tablero;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>
    /// El equipo del proyecto (ronda 6: «en ninguna parte puedo ver quién es mi equipo»). Cada compañero tiene quien
    /// es, lo que lleva y lo que termino, y hay cuatro cosas que hacer con el: darle una tarjeta, hablar uno a uno,
    /// darle un respiro y ponerlo en pareja. Sin usarlas, el dia es el de siempre.
    /// </summary>
    public class EquipoDelProyectoTests {
        private static Catalogo _catalogo;

        private static Catalogo Catalogo() {
            if (_catalogo != null) return _catalogo;
            var desdeEntorno = Environment.GetEnvironmentVariable("NEXUS_STREAMINGASSETS");
            var raiz = !string.IsNullOrEmpty(desdeEntorno)
                ? desdeEntorno
                : Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "Assets", "StreamingAssets"));
            return _catalogo = CatalogLoader.CargarTodo(new CatalogoDeArchivos(raiz));
        }

        private static GameSession EnElDia(string nivel) {
            var flags = new FlagStore(new Dictionary<string, double>(StringComparer.Ordinal), Catalogo().Flags);
            flags.Inicializar();
            var s = new GameSession(Catalogo(), nivel, flags, 7);
            s.ElegirMetodologia("kanban", Catalogo().Metodologias["kanban"].TextosDeRazones.Keys.First());
            s.RepartirCalidad(new Dictionary<string, int>());
            var arq = Catalogo().Niveles[nivel].Fase1.Arquitecturas[0];
            s.ElegirArquitectura(arq.Id, Catalogo().Niveles[nivel].Fase1.RazonesDe(arq)[0].Id);
            s.CerrarFase1();
            s.ComenzarDia();
            return s;
        }

        [Test]
        public void Cada_companero_del_nivel_dice_quien_es() {
            foreach (var nivel in Catalogo().Niveles.Values) {
                Assert.GreaterOrEqual(nivel.Equipo.Count, 2, nivel.Id);
                foreach (var m in nivel.Equipo) {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(m.Descripcion), $"{nivel.Id}/{m.Id}: sin descripción");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(m.Frase), $"{nivel.Id}/{m.Id}: sin frase");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(m.Rol), $"{nivel.Id}/{m.Id}: sin rol");
                }
            }
        }

        [Test]
        public void El_equipo_se_ve_desde_el_primer_dia_aunque_no_se_haya_hablado_con_nadie() {
            var s = EnElDia("nivel-01");
            var fichas = s.FichasDelEquipo();
            Assert.AreEqual(s.Perfil.Equipo.Count, fichas.Count);
            Assert.IsEmpty(s.R.Confianza, "no se ha hablado con nadie");
            foreach (var f in fichas) {
                Assert.IsFalse(string.IsNullOrEmpty(f.ComoVa), f.Miembro.Id);
                Assert.IsNotNull(f.Fuerte, f.Miembro.Id + ": cada uno tiene algo que se le da mejor");
                Assert.IsNotNull(s.DatosDeMiembro(f.Miembro.Id));
            }
        }

        [Test]
        public void Lo_que_lleva_y_lo_que_termino_cada_uno_sale_del_tablero() {
            var s = EnElDia("nivel-01");
            s.AvanzarReloj(240);
            var fichas = s.FichasDelEquipo();
            Assert.IsTrue(fichas.Any(f => f.Ahora.Count > 0), "a media jornada alguien lleva algo");
            var enCurso = s.Tablero.Tarjetas.Count(c => c.Empezada && !c.Terminada && c.Asignado != null);
            Assert.AreEqual(enCurso, fichas.Sum(f => f.Ahora.Count));
        }

        [Test]
        public void Hablar_uno_a_uno_cuesta_tiempo_sube_la_moral_y_solo_una_vez_al_dia() {
            var s = EnElDia("nivel-01");
            var id = s.EquipoDelProyecto()[0].Id;
            var minuto = s.MinutoDelDia;
            var moral = s.W.MoralEquipo;
            ResultadoDeAvance avance;
            var r = s.HablarUnoAUno(id, out avance);
            Assert.AreEqual(minuto + GameSession.MinutosDeUnoAUno, s.MinutoDelDia);
            Assert.Greater(s.W.MoralEquipo, moral);
            Assert.IsFalse(string.IsNullOrEmpty(r.LoQueDice));
            Assert.IsNotNull(s.PorQueNoSePuedeHablarUnoAUno(id), "una vez al día por persona");
            Assert.IsNull(s.PorQueNoSePuedeHablarUnoAUno(s.EquipoDelProyecto()[1].Id));
        }

        [Test]
        public void Un_respiro_baja_el_cansancio_y_ese_dia_se_avanza_menos() {
            var s = EnElDia("nivel-01");
            var m = s.EquipoDelProyecto()[0];
            var cansancio = s.W.Cansancio;
            s.DarRespiro(m.Id);
            Assert.IsTrue(m.DescansaHoy);
            Assert.Less(m.DesvioDeCansancio, 0);
            Assert.LessOrEqual(s.W.Cansancio, cansancio);
            s.AvanzarReloj(60);
            Assert.Less(MotorDelTablero.FactorDelDia(s.R.Tablero), 1.0);
            Assert.GreaterOrEqual(MotorDelTablero.FactorDelDia(s.R.Tablero), MotorDelTablero.FactorMinimo);
            Assert.IsNotNull(s.PorQueNoSePuedeDarRespiro(m.Id));
        }

        [Test]
        public void No_se_puede_mandar_a_descansar_a_todo_el_equipo() {
            var s = EnElDia("nivel-00");
            var equipo = s.EquipoDelProyecto();
            for (var i = 0; i < equipo.Count - 1; i++) s.DarRespiro(equipo[i].Id);
            Assert.IsNotNull(s.PorQueNoSePuedeDarRespiro(equipo[equipo.Count - 1].Id));
        }

        [Test]
        public void En_pareja_sube_la_cobertura_baja_la_deuda_y_sale_menos_trabajo() {
            var s = EnElDia("nivel-01");
            var equipo = s.EquipoDelProyecto();
            var cobertura = s.W.Cobertura;
            s.PonerEnPareja(equipo[0].Id, equipo[1].Id);
            Assert.Greater(s.W.Cobertura, cobertura);
            Assert.AreEqual(equipo[1].Id, equipo[0].ParejaHoy);
            Assert.AreEqual(equipo[0].Id, equipo[1].ParejaHoy);
            s.AvanzarReloj(60);
            Assert.Less(MotorDelTablero.FactorDelDia(s.R.Tablero), 1.0);
            Assert.IsNotNull(s.PorQueNoSePuedePonerEnPareja(equipo[0].Id, equipo[2].Id), "ya trabaja en pareja");
            Assert.IsNotNull(s.PorQueNoSePuedeDarRespiro(equipo[0].Id));
        }

        [Test]
        public void Lo_de_hoy_con_cada_uno_se_acaba_con_el_dia_y_viaja_en_el_guardado() {
            var s = EnElDia("nivel-01");
            var equipo = s.EquipoDelProyecto();
            s.DarRespiro(equipo[0].Id);
            s.PonerEnPareja(equipo[1].Id, equipo[2].Id);

            var copia = GameSession.Restaurar(Catalogo(), "nivel-01", s.Flags, 7, s.Capturar());
            Assert.IsTrue(copia.EquipoDelProyecto()[0].DescansaHoy, "el respiro viaja en el guardado");
            Assert.AreEqual(equipo[2].Id, copia.EquipoDelProyecto()[1].ParejaHoy);

            for (var v = 0; v < 60 && !s.SePuedeCerrarLaJornada; v++) {
                foreach (var a in s.AvanzarReloj(30).AlertasQueSuenan) s.AtenderAlerta(a.Id);
                if (s.Decision != null) s.ResolverDecision(s.Decision.Opciones.First(o => !o.Bloqueada).Id);
                if (s.Minijuego != null) s.ResolverMinijuego(Nexus.Core.Minijuegos.PuenteDelMotor.Omitido(s.Minijuego.MinijuegoId, s.Minijuego.ObjetivoAprendizaje, null));
            }
            s.CerrarJornada();
            s.TerminarDia(false);
            foreach (var m in s.EquipoDelProyecto()) {
                Assert.IsFalse(m.DescansaHoy, m.Id);
                Assert.IsNull(m.ParejaHoy, m.Id);
                Assert.IsFalse(m.HabloHoy, m.Id);
            }
        }
    }
}
