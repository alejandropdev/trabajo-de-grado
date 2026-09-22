using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Guardado;
using Nexus.Core.Narrativa;
using Nexus.Unity.Aplicacion;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El orden de las pantallas de un nivel. Ninguna pantalla sabe cual va despues de ella: al terminar, le
    /// devuelve el control a esto, que es el unico sitio donde esta escrito el recorrido:
    ///
    ///   antes del nivel   CIN-0.0 → ENT-0 → [las 15 preguntas] → ENT-0.FIN → CIN-0.1        (guiones 'antesDelNivel')
    ///   apertura          CIN-1.0                                                             (guiones 'apertura')
    ///   Fase 1            briefing, metodologia, calidad, arquitectura  (+ guiones 'fase1')
    ///   Fase 2            el dia continuo, dia tras dia                  (+ guiones 'dia', que trae el director narrativo)
    ///   Fase 3            guiones 'lanzamiento' → el lanzamiento → guiones 'cierre' → Cerrar() → Dashboard de Lecciones
    ///   despues           guiones 'despuesDelNivel' (CIN-0.2) → el nivel siguiente, o el final de la version
    ///
    /// Los guiones se buscan por nivel y momento, asi que un nivel nuevo trae sus escenas sin tocar este archivo.
    /// La unica pieza fija es la entrevista, que va despues del guion ENT-0 si el catalogo tiene prueba de admision.
    /// </summary>
    public static class FlujoDelNivel {
        public const string GuionDeLaEntrevista = "ENT-0";

        /// <summary>Retoma una partida recien abierta donde se quedo.</summary>
        public static void Continuar(AppRoot app) {
            var s = app.Sesion;
            if (s == null) { FinDeLaVersion(app); return; }
            if (!s.Fase1Cerrada) { EmpezarNivel(app); return; }
            if (s.R.Fase == 2) { app.Router.IrA<PantallaDelDia>(); return; }
            Lanzar(app);
        }

        public static void EmpezarNivel(AppRoot app) {
            var nivel = app.Sesion.NivelId;
            var pasos = new List<Action<Action>>();

            foreach (var guion in Guiones(app, nivel, MomentosDeGuion.AntesDelNivel)) {
                var g = guion;
                pasos.Add(siguiente => Reproducir(app, g, NarrativeBeat.VarianteDefecto, false, siguiente));
                if (g.Id == GuionDeLaEntrevista && app.Catalogo.Admision != null)
                    pasos.Add(siguiente => app.Router.IrA<PantallaDeEntrevista>(p => p.AlTerminar = siguiente));
            }
            foreach (var guion in Guiones(app, nivel, MomentosDeGuion.Apertura)) {
                var g = guion;
                pasos.Add(siguiente => Reproducir(app, g, NarrativeBeat.VarianteDefecto, false, siguiente));
            }
            pasos.Add(_ => app.Router.IrA<PantallaDeFase1>());
            Ejecutar(app, pasos);
        }

        /// <summary>La Fase 1 ya esta cerrada: primer autoguardado (§5.7) y al dia 1.</summary>
        public static void TrasLaFase1(AppRoot app) {
            app.Guardar(AutoGuardado.Motivos.CierreFase1);
            app.Router.IrA<PantallaDelDia>();
        }

        /// <summary>Se acabaron los dias: el lanzamiento, el cierre, las lecciones y el nivel siguiente.</summary>
        public static void Lanzar(AppRoot app) {
            var s = app.Sesion;
            var nivel = s.NivelId;
            var pasos = new List<Action<Action>>();

            foreach (var guion in Guiones(app, nivel, MomentosDeGuion.Lanzamiento)) {
                var g = guion;
                pasos.Add(siguiente => Reproducir(app, g, NarrativeBeat.VarianteDefecto, false, siguiente));
            }
            pasos.Add(siguiente => {
                if (s.Lanzamiento == null) s.EjecutarLanzamiento();
                app.Guardar(AutoGuardado.Motivos.TrasLanzamiento);
                app.Router.IrA<PantallaDeLanzamiento>(p => p.AlSeguir = siguiente);
            });
            // El cierre va ANTES de Cerrar(): sus elecciones (abrir el log de CIN-1.4) escriben flags, y los
            // flags solo se escriben en Cerrar (INV-6).
            foreach (var guion in Guiones(app, nivel, MomentosDeGuion.Cierre)) {
                var g = guion;
                pasos.Add(siguiente => Reproducir(app, g, NarrativeBeat.VarianteDefecto, false, siguiente));
            }
            pasos.Add(siguiente => {
                var reporte = s.Cerrar();
                app.UltimoCierre = reporte;
                app.Guardar(AutoGuardado.Motivos.CierreDeNivel);
                app.Router.IrA<PantallaDeLecciones>(p => { p.Reporte = reporte; p.AlSeguir = siguiente; });
            });
            foreach (var guion in Guiones(app, nivel, MomentosDeGuion.DespuesDelNivel)) {
                var g = guion;
                pasos.Add(siguiente => Reproducir(app, g, NarrativeBeat.VarianteDefecto, false, siguiente));
            }
            pasos.Add(_ => {
                var siguienteNivel = app.PasarAlSiguienteNivel();
                if (siguienteNivel == null) FinDeLaVersion(app);
                else EmpezarNivel(app);
            });
            Ejecutar(app, pasos);
        }

        public static void FinDeLaVersion(AppRoot app) {
            app.CerrarPartida();
            app.Router.IrA<PantallaDeMensaje>(p => {
                p.Etiqueta = "Fin de la versión jugable";
                p.Titulo = "Hasta aquí llega esta versión de NEXUS";
                p.Parrafos.Add("Terminaste los niveles que existen por ahora: el concurso y Cradle Lifts. La partida queda guardada, y lo que encontraste está en tu diario de campo.");
                p.Parrafos.Add("Lo que decidiste en cada nivel ya dejó su huella en la historia: cuando existan los niveles siguientes, se acordarán.");
            });
        }

        // ==================================================================== guiones

        public static List<Guion> Guiones(AppRoot app, string nivel, string momento) {
            return app.Catalogo.Guiones
                .Where(g => g != null && g.Nivel == nivel && g.Momento == momento)
                .OrderBy(g => g.Orden)
                .ToList();
        }

        /// <summary>
        /// Reproduce un guion. 'apilar' = encima de la pantalla actual (una escena a mitad de dia), o sustituyendola
        /// (el prologo, que no tiene nada debajo). Si el guion no tiene esa variante, se usa la de por defecto, y
        /// si tampoco, se sigue sin escena: un guion que falta no puede bloquear la partida.
        /// </summary>
        public static void Reproducir(AppRoot app, Guion guion, string variante, bool apilar, Action alTerminar) {
            List<LineaDeGuion> lineas;
            if (guion == null ||
                !(guion.Variantes.TryGetValue(variante ?? NarrativeBeat.VarianteDefecto, out lineas) ||
                  guion.Variantes.TryGetValue(NarrativeBeat.VarianteDefecto, out lineas)) ||
                lineas == null || lineas.Count == 0) {
                alTerminar?.Invoke();
                return;
            }

            Action<PantallaDeGuion> configurar = p => {
                p.Guion = guion;
                p.Lineas = lineas;
                p.AlElegir = opcion => {
                    var s = app.Sesion;
                    // Despues de Cerrar ya no se registra nada: los flags ya se escribieron (INV-6).
                    if (s != null && !s.NivelTerminado && !s.R.EleccionesNarrativas.ContainsKey(guion.Id))
                        s.RegistrarEleccion(guion.Id, opcion.Id);
                };
                p.AlTerminar = () => {
                    if (apilar) app.Router.Volver();
                    alTerminar?.Invoke();
                };
            };
            if (apilar) app.Router.Apilar(configurar);
            else app.Router.IrA(configurar);
        }

        /// <summary>
        /// Corre una lista de pasos en orden: cada uno recibe como argumento como seguir con el siguiente.
        /// ★ Si un paso falla, nunca se queda la pantalla vacia: se dice que fallo, con la opcion de reintentar
        /// ese mismo paso o volver al menu (la partida ya esta guardada en el ultimo punto de autoguardado).
        /// </summary>
        private static void Ejecutar(AppRoot app, List<Action<Action>> pasos, int desde = 0) {
            if (desde >= pasos.Count) return;
            try {
                pasos[desde](() => Ejecutar(app, pasos, desde + 1));
            } catch (Exception ex) {
                Debug.LogException(ex);
                MostrarFallo(app, ex, () => Ejecutar(app, pasos, desde));
            }
        }

        public static void MostrarFallo(AppRoot app, Exception ex, Action reintentar) {
            app.Router.IrA<PantallaDeMensaje>(p => {
                p.Etiqueta = "Algo ha fallado";
                p.Titulo = "El juego no pudo seguir en este punto";
                p.Parrafos.Add(ex.GetType().Name + ": " + ex.Message);
                p.Parrafos.Add("Tu partida está guardada en el último punto de autoguardado. Puedes reintentar este paso, o volver al menú y continuarla después. " +
                               "Si se repite, copia el error de la consola de Unity.");
                if (reintentar != null) {
                    p.TextoBoton = "Reintentar";
                    p.AlPulsar = reintentar;
                    p.TextoSecundario = "Volver al menú";
                    p.AlSecundario = () => { app.CerrarPartida(); app.MostrarInicio(); };
                } else {
                    p.AlPulsar = () => { app.CerrarPartida(); app.MostrarInicio(); };
                }
            });
        }
    }
}
