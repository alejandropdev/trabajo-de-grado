using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using Nexus.Core.Sesion;
using Nexus.Core.Tablero;
using Nexus.Core.Tarjetas;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Lo que hace que la metodologia se note en la Fase 2: el tablero del equipo, el monitoreo con el diagrama propio
    /// de cada una y el espacio de ceremonias. Todo sale de la politica de la metodologia (PoliticaDeTablero) y de sus
    /// ceremonias: aqui no hay ni un «si es Scrum…». Lo que una metodologia no tiene, no aparece, y el panel dice por que.
    ///
    ///   en el dia      el tablero en pequeño, solo para verlo fluir: que se esta haciendo, quien, cuanto lleva
    ///   Tablero        las columnas enteras, y actuar: asignar, priorizar, mover el limite de WIP, revisar un cambio
    ///   Monitoreo      burndown · flujo acumulado · curva S · documentos · backlog · pruebas, segun la metodologia
    ///   Ceremonias     las de hoy (se puede asistir y elegir como llevarlas), la agenda del nivel y las actas
    ///
    /// Mirar es gratis (el reloj espera); cada accion cuesta sus minutos y pasa por Runner.Actuar.
    /// </summary>
    public sealed partial class PantallaDelDia {
        private RectTransform _tableroDelDia;
        private string _claveDelTablero;
        private int _pestanaDeCeremonias;
        private string _ultimoResultado;   // lo que acaba de pasar al actuar, para decirlo arriba del panel

        private PoliticaDeTablero Politica { get { return S.Politica; } }

        private string NombreDeColumna(string id) {
            string porDefecto;
            switch (id) {
                case ColumnasDeBase.PorHacer: porDefecto = "Por hacer"; break;
                case ColumnasDeBase.Haciendo: porDefecto = "En curso"; break;
                case ColumnasDeBase.Revision: porDefecto = "Revisión"; break;
                default: porDefecto = "Hecho"; break;
            }
            return Politica == null ? porDefecto : Politica.NombreDe(id, porDefecto);
        }

        private static string NombreDeTipo(string tipo) {
            switch (tipo) {
                case "interfaz": return "interfaz";
                case "datos": return "datos";
                case "logica": return "lógica";
                case "pruebas": return "pruebas";
                default: return tipo ?? "";
            }
        }

        /// <summary>Hace una accion que cuesta minutos y recuerda lo que paso, para decirlo en el panel.</summary>
        private void Actuar(Func<ResultadoDeAvance> accion, string hecho) {
            try {
                if (!Runner.Actuar(accion)) { _ultimoResultado = "Ahora no se puede: el reloj está parado o hay algo abierto."; return; }
                _ultimoResultado = hecho;
                Anotar($"{S.HoraActual} · {hecho}");
            } catch (InvalidOperationException e) {
                _ultimoResultado = e.Message;
            }
        }

        private void DecirLoUltimo(Hoja h) {
            if (string.IsNullOrEmpty(_ultimoResultado)) return;
            Ui.Notificacion(h.Raiz, _ultimoResultado, Tono.Cyan);
        }

        // ==================================================================== el tablero, en el dia

        /// <summary>
        /// El tablero dentro de «El proyecto»: cuanto hay en cada columna y, una a una, las tarjetas que el equipo tiene
        /// entre manos, con quien la lleva y cuanto lleva. Solo se mira; para tocarlo, «Abrir el tablero».
        /// </summary>
        private void ConstruirTableroDelDia(Transform padre) {
            var bloque = Ui.Columna(padre, "Tablero del dia", Tema.Espacio(2));
            GuiaView.Registrar("dia.tablero", bloque);
            var cabecera = Ui.Fila(bloque, "Cabecera");
            UiKit.Tamano(Ui.Texto(cabecera, "EL TABLERO DEL EQUIPO", EstiloTexto.Leyenda, Tema.cyan), flexAncho: 1);
            Ui.Boton(cabecera, "Abrir el tablero", () => AbrirTablero(0), VarianteBoton.Fantasma);
            _tableroDelDia = Ui.Columna(bloque, "Tarjetas", Tema.Espacio(2));
        }

        private void PintarTableroDelDia() {
            if (_tableroDelDia == null) return;
            var t = S.Tablero;
            if (t == null) return;
            var enCurso = t.Tarjetas.Where(c => !c.EsBug && c.Empezada && !c.Terminada).OrderBy(c => c.Orden).ToList();
            var clave = string.Join("|", enCurso.Select(c => c.Id + ":" + (c.Trabajo / Math.Max(0.01, c.Puntos)).ToString("0.00") + ":" + c.Asignado)) +
                        "#" + t.Tarjetas.Count(c => c.Terminada) + "#" + t.Tarjetas.Count + "#" + (S.PrPendiente != null) + "#" + S.R.LimiteWip;
            if (clave == _claveDelTablero) return;
            _claveDelTablero = clave;
            UiKit.Vaciar(_tableroDelDia);

            var porHacer = t.Tarjetas.Count(c => !c.EsBug && !c.Empezada);
            var hechas = t.Tarjetas.Count(c => !c.EsBug && c.Terminada);
            var resumen = $"{NombreDeColumna(ColumnasDeBase.PorHacer)}: {porHacer}   ·   {NombreDeColumna(ColumnasDeBase.Haciendo)}: {enCurso.Count}" +
                          (Politica != null && Politica.LimiteDeWip ? $" de {S.R.LimiteWip}" : "") +
                          $"   ·   {NombreDeColumna(ColumnasDeBase.Hecho)}: {hechas}";
            Ui.Texto(_tableroDelDia, resumen, EstiloTexto.Pequeno, Tema.ink);

            if (enCurso.Count == 0)
                Ui.Texto(_tableroDelDia, S.R.DiaActual == 0 ? Solo("El equipo empieza a trabajar con el día 1.", "Empiezas a trabajar con el día 1.") : "Ahora mismo no hay nada a medias.",
                         EstiloTexto.Pequeno, Tema.inkMuted);
            foreach (var c in enCurso) {
                var fila = Ui.Fila(_tableroDelDia, "Tarjeta", Tema.Espacio(3));
                UiKit.Tamano(Ui.Texto(fila, c.Titulo, EstiloTexto.Pequeno, Tema.ink), ancho: 250);
                Ui.Barra(fila, (float)(c.Trabajo / Math.Max(0.01, c.Puntos)), c.AsignacionManual ? Tema.success : Tema.cyan);
                var quien = t.Miembro(c.Asignado);
                UiKit.Tamano(Ui.Texto(fila, quien == null ? "—" : quien.Nombre, EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Right), ancho: 90);
            }
            if (S.PrPendiente != null) {
                var n = Ui.Notificacion(_tableroDelDia, "Hay un cambio de código esperando tu revisión.", Tono.Aviso);
                Ui.Boton(n, "Revisar", AbrirRevision, VarianteBoton.Fantasma);
            }
        }

        // ==================================================================== el tablero, entero

        /// <summary>
        /// El tablero entero es ahora el muro del backlog (Dia/PantallaDelDia.Muro.cs, 2026-11-06): una pizarra a pantalla
        /// completa donde las fichas se arrastran. 'pestana' queda por compatibilidad con quien lo llamaba.
        /// </summary>
        private void AbrirTablero(int pestana) {
            AbrirMuro();
        }

        private static string ComoSeLeDa(MiembroDelEquipo m, string tipo) {
            var h = m.HabilidadPara(tipo);
            return h >= 1.2 ? "es su fuerte" : h <= 0.85 ? "le cuesta" : "se le da normal";
        }

        // ==================================================================== revisar un cambio de codigo

        /// <summary>Inspeccionar un documento de la etapa: se lee un trozo y se firma o se piden cambios.</summary>
        private void AbrirInspeccion(string documento) {
            var extracto = S.ExtractoDeHoy(documento);
            if (extracto == null) {
                Actuar(() => S.InspeccionarDocumento(documento), $"Inspeccionasteis el {documento}: documentación +3 y mejor calidad del documento.");
                return;
            }
            CerrarPanel();
            ResultadoDeRevision resultado = null;
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<PantallaDeFicha>(p => {
                p.Titulo = $"Inspección del {documento} · {extracto.Titulo}";
                p.AnchoDeLaFicha = 1100;
                p.AlCerrar = () => _escenaApilada = false;
                p.Pintar = h => {
                    PantallaDeGlosario.Palabras(App, h.Raiz, "¿Qué documento es?", documento.ToLowerInvariant());
                    h.Parrafo("Leed esta parte con calma. Si lo que dice se puede construir y comprobar tal cual, está bien: se firma. Si deja algo sin decir o se contradice, se piden cambios.");
                    var hoja = Ui.PanelColumna(h.Raiz, "Extracto", Tema.Espacio(4), Tema.Espacio(2), Tema.surfaceRaised);
                    for (var i = 0; i < extracto.Secciones.Count; i++) Ui.Texto(hoja, $"{i + 1}. {extracto.Secciones[i]}", EstiloTexto.Cuerpo, Tema.ink);
                    if (resultado == null) {
                        h.Nota($"Inspeccionarlo cuesta {GameSession.MinutosDeInspeccionar} min. No todo lo que se inspecciona está mal.");
                        var botones = h.Fila();
                        Ui.Boton(botones, "Firmarlo: está bien", () => Inspeccionar(documento, VeredictosDePr.Aprobar, r => { resultado = r; App.Router.Repintar(); }), VarianteBoton.Primario);
                        Ui.Boton(botones, "Pedir cambios: algo falla", () => Inspeccionar(documento, VeredictosDePr.PedirCambios, r => { resultado = r; App.Router.Repintar(); }));
                        return;
                    }
                    var t = h.Tarjeta(resultado.Titulo, resultado.Acierto ? Tono.Exito : Tono.Aviso);
                    Ui.Texto(t, resultado.Explicacion, EstiloTexto.Cuerpo, Tema.ink);
                    if (resultado.Cambios.Count > 0) Ui.Texto(t, "Lo que cambió: " + Textos.Previsualizar(resultado.Cambios), EstiloTexto.Pequeno, Tema.warning);
                };
            });
        }

        private void Inspeccionar(string documento, string veredicto, Action<ResultadoDeRevision> alDecidir) {
            try {
                ResultadoDeAvance avance;
                var r = S.InspeccionarDocumento(documento, veredicto, out avance);
                Runner.Notificar(avance);
                Anotar($"{S.HoraActual} · Inspección del {documento}: {r.Titulo.ToLowerInvariant()}.");
                alDecidir(r);
            } catch (InvalidOperationException e) {
                Anotar("No se pudo inspeccionar: " + e.Message);
            }
        }

        /// <summary>
        /// Revisar un cambio de codigo (2026-11-07): como en un IDE, con el diff animado y las dos decisiones como cartas
        /// (Revision/PantallaDeRevisionDeCodigo.cs). La escena para el reloj; decidir cobra sus minutos como siempre.
        /// </summary>
        private void AbrirRevision() {
            var pr = S.PrPendiente;
            if (pr == null) return;
            CerrarPanel();
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<PantallaDeRevisionDeCodigo>(p => {
                p.Pr = pr;
                p.TrabajasSolo = S.TrabajasSolo;
                p.Decidir = Revisar;
                p.AlCerrar = () => _escenaApilada = false;
            });
        }

        /// <summary>Hace la revision en el motor y la anota. Devuelve lo que salio, o null si no se pudo.</summary>
        private ResultadoDeRevision Revisar(string veredicto) {
            try {
                ResultadoDeAvance avance;
                var r = S.RevisarPr(veredicto, out avance);
                Runner.Notificar(avance);
                Anotar($"{S.HoraActual} · Revisión de código: {r.Titulo.ToLowerInvariant()}.");
                return r;
            } catch (InvalidOperationException e) {
                Anotar("No se pudo revisar: " + e.Message);
                return null;
            }
        }

        // ==================================================================== monitoreo: lo de la metodologia

        /// <summary>Las pestañas que la metodologia añade al monitoreo, en su orden: titulo y como se pinta.</summary>
        private List<KeyValuePair<string, Action<RectTransform>>> VistasDeLaMetodologia() {
            var vistas = new List<KeyValuePair<string, Action<RectTransform>>>();
            if (Politica == null) return vistas;
            foreach (var v in Politica.Vistas) {
                switch (v) {
                    case VistasDeMonitoreo.Burndown: vistas.Add(new KeyValuePair<string, Action<RectTransform>>("Burndown", PintarBurndown)); break;
                    case VistasDeMonitoreo.Cfd: vistas.Add(new KeyValuePair<string, Action<RectTransform>>("Flujo acumulado", PintarFlujoAcumulado)); break;
                    case VistasDeMonitoreo.CurvaS: vistas.Add(new KeyValuePair<string, Action<RectTransform>>("Curva S", PintarCurvaS)); break;
                    case VistasDeMonitoreo.Documentos: vistas.Add(new KeyValuePair<string, Action<RectTransform>>("Documentos", PintarDocumentos)); break;
                    case VistasDeMonitoreo.Pruebas: vistas.Add(new KeyValuePair<string, Action<RectTransform>>("Pruebas", PintarPruebas)); break;
                }
            }
            return vistas;
        }

        private void PintarVistaDeMonitoreo(string vista, RectTransform hoja) {
            switch (vista) {
                case VistasDeMonitoreo.Burndown: PintarBurndown(hoja); break;
                case VistasDeMonitoreo.Cfd: PintarFlujoAcumulado(hoja); break;
                case VistasDeMonitoreo.CurvaS: PintarCurvaS(hoja); break;
                case VistasDeMonitoreo.Documentos: PintarDocumentos(hoja); break;
            }
        }

        /// <summary>Scrum: lo que queda del sprint, dia a dia, contra la linea ideal.</summary>
        private void PintarBurndown(RectTransform hoja) {
            PantallaDeGlosario.Palabras(App, hoja, "Para leer esta gráfica:", "burndown", "velocidad", "sprint");
            var r = S.R;
            if (r.BurndownDelSprint.Count == 0) {
                Ui.Texto(hoja, "Todavía no hay sprint planificado.", EstiloTexto.Cuerpo);
                return;
            }
            // Los dias que el sprint dura en este nivel, no los que declara la metodologia (en un nivel de 5 dias, 5).
            var dias = S.DiasDeLaUnidadActual();
            var comprometido = r.BurndownDelSprint[0];
            Ui.Texto(hoja, $"SPRINT {r.SprintActual + 1} · LO QUE QUEDA POR TERMINAR", EstiloTexto.Leyenda, Tema.cyan);
            var g = new GraficoDeSeries(Ui, hoja, 1100, 340);
            g.Ejes("días del sprint", "puntos", 0, dias, Math.Max(1, comprometido));
            g.Linea(GraficasDelProyecto.BurndownIdeal(comprometido, dias), Tema.inkFaint, true, 2);
            g.Linea(r.BurndownDelSprint, Tema.cyan);
            GraficoDeSeries.Leyenda(Ui, hoja, new[] {
                new KeyValuePair<string, Color>("Lo que queda de verdad", Tema.cyan),
                new KeyValuePair<string, Color>("Ritmo ideal para llegar a cero", Tema.inkFaint)
            });
            var hechos = r.BurndownDelSprint.Count - 1;
            var ideal = comprometido * (1 - Math.Min(1.0, hechos / (double)dias));
            var real = r.BurndownDelSprint[r.BurndownDelSprint.Count - 1];
            Ui.Texto(hoja, hechos == 0 ? "El sprint acaba de empezar: la línea se mueve al cerrar cada día."
                         : real > ideal + 0.5 ? $"Vais {real - ideal:0.#} puntos por encima de la línea: a este ritmo no se termina lo comprometido. Solo baja lo TERMINADO, no lo «casi»."
                         : "Vais sobre la línea o por debajo: lo comprometido sale.", EstiloTexto.Cuerpo, real > ideal + 0.5 ? Tema.warning : Tema.ink);
            if (r.TerminadoPorIteracion.Count > 0)
                Ui.Texto(hoja, "Velocidad de los sprints cerrados (puntos terminados): " + string.Join(" · ", r.TerminadoPorIteracion.Select(v => v.ToString("0.#"))) +
                               ". Es lo que el equipo ha demostrado que puede prometer.", EstiloTexto.Pequeno);
        }

        /// <summary>Kanban: las tres bandas (por hacer, en curso, terminado) y lo que dicen del flujo.</summary>
        private void PintarFlujoAcumulado(RectTransform hoja) {
            PantallaDeGlosario.Palabras(App, hoja, "Para leer esta gráfica:", "flujo-acumulado", "wip");
            var r = S.R;
            if (r.SerieTerminado.Count < 2) {
                Ui.Texto(hoja, "El flujo se dibuja a partir del segundo día.", EstiloTexto.Cuerpo);
                return;
            }
            Ui.Texto(hoja, "DIAGRAMA DE FLUJO ACUMULADO", EstiloTexto.Leyenda, Tema.cyan);
            var g = new GraficoDeSeries(Ui, hoja, 1100, 340);
            g.Ejes("día", "puntos", 1, Math.Max(2, S.Perfil.DiasTotales), Math.Max(1, r.SerieAlcance.Max()) * 1.05);
            g.AreasApiladas(new IList<double>[] { r.SerieTerminado, r.SerieEnCurso, r.SeriePorHacer }, new[] { Tema.success, Tema.warning, Tema.inkFaint });
            g.MarcaVertical(r.DiaActual, "hoy", Tema.inkMuted);
            GraficoDeSeries.Leyenda(Ui, hoja, new[] {
                new KeyValuePair<string, Color>("Terminado", Tema.success), new KeyValuePair<string, Color>("En curso", Tema.warning),
                new KeyValuePair<string, Color>("Por hacer", Tema.inkFaint)
            });
            Ui.Texto(hoja, "Si la banda de «en curso» se ensancha, se empieza más de lo que se termina: ahí está el atasco. Lo sano es que sea fina y que «terminado» suba constante.",
                     EstiloTexto.Pequeno);
            var f = GraficasDelProyecto.Flujo(S.Tablero, r.DiaActual);
            var cifras = Ui.Fila(hoja, "Flujo", Tema.espacio);
            Ui.Cifra(cifras, f.TiempoDeCiclo.ToString("0.#") + " d", "Tiempo de ciclo", "de empezar una tarjeta a terminarla");
            Ui.Cifra(cifras, f.Rendimiento.ToString("0.##"), "Tarjetas por día", "en los últimos días");
            Ui.Cifra(cifras, f.EnCurso + " / " + S.R.LimiteWip, "En curso / límite", null, f.EnCurso > S.R.LimiteWip ? Tono.Aviso : Tono.Neutro);
            if (f.MasVieja != null)
                Ui.Texto(hoja, $"La tarjeta que más lleva a medias: «{f.MasVieja}», {f.DiasDeLaMasVieja} día(s).", EstiloTexto.Pequeno, f.DiasDeLaMasVieja >= 4 ? Tema.warning : Tema.ink);
        }

        /// <summary>Cascada: lo planificado, con forma de S, contra lo hecho. Y cuanto se va por detras.</summary>
        private void PintarCurvaS(RectTransform hoja) {
            PantallaDeGlosario.Palabras(App, hoja, "Para leer esta gráfica:", "curva-s", "avance", "alcance");
            var r = S.R;
            var dias = Math.Max(2, S.Perfil.DiasTotales);
            Ui.Texto(hoja, "CURVA S · LO PLANIFICADO CONTRA LO HECHO", EstiloTexto.Leyenda, Tema.cyan);
            var g = new GraficoDeSeries(Ui, hoja, 1100, 340);
            g.Ejes("día", "puntos", 1, dias + 1, Math.Max(1, S.W.Alcance) * 1.1);
            g.Linea(GraficasDelProyecto.CurvaSPlanificada(S.W.Alcance, dias), Tema.inkFaint, true, 2);
            if (r.SerieAvance.Count > 0) g.Linea(r.SerieAvance, Tema.cyan);
            g.MarcaVertical(Math.Max(1, r.DiaActual), "hoy", Tema.inkMuted);
            GraficoDeSeries.Leyenda(Ui, hoja, new[] {
                new KeyValuePair<string, Color>("Avance real", Tema.cyan), new KeyValuePair<string, Color>("El plan", Tema.inkFaint)
            });
            var desvio = GraficasDelProyecto.DesvioRespectoAlPlan(S.W.Avance, S.W.Alcance, Math.Max(1, r.DiaActual), dias);
            Ui.Texto(hoja, desvio > 0.5 ? $"Vais {desvio:0.#} puntos por detrás del plan. En Cascada esto se cuenta en el comité de seguimiento, con la curva delante."
                         : desvio < -0.5 ? $"Vais {-desvio:0.#} puntos por delante del plan." : "Vais con el plan.", EstiloTexto.Cuerpo, desvio > 0.5 ? Tema.warning : Tema.ink);
            Ui.Texto(hoja, "El plan arranca despacio a propósito: en las primeras etapas se escribe y se diseña, y casi nada «avanza» a la vista. Por eso un retraso al principio no se nota hasta el final.",
                     EstiloTexto.Pequeno);
        }

        /// <summary>Cascada: los documentos de cada etapa, lo completos que estan y su calidad. El de hoy se puede inspeccionar.</summary>
        private void PintarDocumentos(RectTransform hoja) {
            var hoy = S.DocumentosDeHoy();
            Ui.Texto(hoja, "Cada etapa deja un documento que se firma al cerrarla. Lo que no quede escrito aquí, alguien lo supondrá más adelante. " +
                           $"{Solo("Inspeccionar el de la etapa actual con el equipo", "Releer tú el de la etapa actual")} cuesta {GameSession.MinutosDeInspeccionar} min y se puede una vez al día.", EstiloTexto.Pequeno, Tema.ink);
            foreach (var etapa in S.Metodologia.Calendario.Etapas) {
                if (etapa.Documentos == null || etapa.Documentos.Count == 0) continue;
                foreach (var doc in etapa.Documentos) {
                    var d = doc;
                    Nexus.Core.Modelo.EstadoArtefacto estado;
                    S.R.Artefactos.TryGetValue(d, out estado);
                    var ficha = Ui.PanelColumna(hoja, "Documento " + d, Tema.Espacio(4), Tema.Espacio(1), Tema.surfaceRaised);
                    var cabecera = Ui.Fila(ficha, "Cabecera", Tema.Espacio(3));
                    var art = S.Perfil.Proyecto == null ? null : S.Perfil.Proyecto.Artefactos.FirstOrDefault(a => a.Id == d);
                    Ui.Texto(cabecera, d, EstiloTexto.Encabezado, Tema.ink);
                    Ui.Badge(cabecera, "etapa: " + etapa.Nombre, hoy.Contains(d) ? Tono.Cyan : Tono.Neutro);
                    Ui.Resorte(cabecera);
                    if (hoy.Contains(d)) {
                        var motivo = S.PorQueNoSePuedeInspeccionar(d);
                        var inspeccionar = Ui.Boton(cabecera, "Inspeccionarlo", () => AbrirInspeccion(d));
                        inspeccionar.interactable = motivo == null;
                    }
                    if (art != null && !string.IsNullOrEmpty(art.ParaQueAqui)) Ui.Texto(ficha, art.ParaQueAqui, EstiloTexto.Pequeno);
                    var completitud = estado == null ? 0 : estado.Completitud;
                    var calidad = estado == null ? 0 : estado.Calidad;
                    var linea = Ui.Fila(ficha, "Completitud", Tema.Espacio(3));
                    UiKit.Tamano(Ui.Texto(linea, "Completo", EstiloTexto.Pequeno, Tema.ink), ancho: 110);
                    Ui.Barra(linea, (float)(completitud / 100.0), completitud >= 99 ? Tema.success : Tema.cyan);
                    UiKit.Tamano(Ui.Texto(linea, $"{completitud:0} %", EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.Right), ancho: 70);
                    var linea2 = Ui.Fila(ficha, "Calidad", Tema.Espacio(3));
                    UiKit.Tamano(Ui.Texto(linea2, "Calidad", EstiloTexto.Pequeno, Tema.ink), ancho: 110);
                    Ui.Barra(linea2, (float)(calidad / 100.0), calidad >= 60 ? Tema.success : Tema.warning);
                    UiKit.Tamano(Ui.Texto(linea2, $"{calidad:0}", EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.Right), ancho: 70);
                }
            }
        }

        /// <summary>Hacer pruebas: una accion del jugador. Donde y cuando se puede, lo dice la metodologia.</summary>
        private void PintarPruebas(RectTransform hoja) {
            var w = S.W;
            var motivo = S.PorQueNoSePuedeProbar();
            Ui.Texto(hoja, $"Cobertura de pruebas: {w.Cobertura:0} %", EstiloTexto.Subtitulo, w.Cobertura <= 30 ? Tema.warning : Tema.ink);
            Ui.Barra(hoja, (float)w.Cobertura / 100f, w.Cobertura <= 30 ? Tema.warning : Tema.cyan);
            var defectos = Nexus.Core.Evaluacion.PronosticoDeLanzamiento.DefectosPorCobertura(w.Cobertura, S.EntregaIncremental);
            Ui.Texto(hoja, $"Con esta cobertura, unos {defectos:0} errores sin probar llegarían al cliente en el lanzamiento.", EstiloTexto.Cuerpo, Tema.ink);
            var efecto = S.EfectoDeProbar();
            var t = Ui.Tarjeta(hoja, "Una ronda de pruebas");
            Ui.Texto(t, $"Cuesta {GameSession.MinutosDeProbar} min de la jornada, una vez al día. Hoy daría: {Textos.Previsualizar(efecto)}. " +
                        "Cuanto menos probado está el sistema, más encuentra cada ronda.", EstiloTexto.Pequeno, Tema.ink);
            var probar = Ui.Boton(Ui.Fila(t), "Hacer una ronda de pruebas", () => Actuar(() => S.EjecutarPruebas(), Solo("El equipo hizo una ronda de pruebas: ", "Hiciste una ronda de pruebas: ") + Textos.Previsualizar(efecto) + "."), VarianteBoton.Primario);
            probar.interactable = motivo == null;
            if (motivo != null) Ui.Texto(t, motivo, EstiloTexto.Pequeno, Tema.warning);
            if (S.R.VecesQueSeProbo > 0) Ui.Texto(hoja, $"Rondas de pruebas hechas en este proyecto: {S.R.VecesQueSeProbo}.", EstiloTexto.Pequeno);
        }
    }
}
