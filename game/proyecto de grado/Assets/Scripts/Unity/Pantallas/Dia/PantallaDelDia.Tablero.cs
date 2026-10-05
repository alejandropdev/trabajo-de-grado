using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using Nexus.Core.Sesion;
using Nexus.Core.Tablero;
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
        private int _pestanaDeCeremonias, _pestanaDelTablero;
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
                Ui.Texto(_tableroDelDia, S.R.DiaActual == 0 ? "El equipo empieza a trabajar con el día 1." : "Ahora mismo no hay nada a medias.",
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

        private void AbrirTablero(int pestana) {
            _pestanaDelTablero = pestana;
            _ultimoResultado = null;
            AbrirPanel(S.Metodologia.Nombre + " · " + (S.BriefDeHoy != null ? S.BriefDeHoy.EtiquetaUnidad : ""), "El tablero del equipo", 1560,
                       PintarPanelDelTablero, ClaveDelPanelDelTablero);
        }

        private string ClaveDelPanelDelTablero() {
            var t = S.Tablero;
            if (t == null) return "";
            return S.MinutoDelDia + "|" + S.R.LimiteWip + "|" + (S.PrPendiente != null) + "|" + _ultimoResultado + "|" +
                   string.Join(",", t.Tarjetas.Select(c => c.Id + c.Columna + c.Asignado + c.Orden + c.Sprint)) + "|" + AlertasSonando().Count;
        }

        private void PintarPanelDelTablero(Hoja h) {
            var t = S.Tablero;
            if (t == null) { h.Nota("El tablero aparece al empezar el desarrollo."); return; }
            AvisoDeAlertaEnPanel(h);
            DecirLoUltimo(h);
            var titulos = new List<string> { "Tablero", "El equipo" };
            var conBacklog = Politica != null && Politica.Tiene(VistasDeMonitoreo.Backlog);
            if (conBacklog) titulos.Add("Product backlog");
            var pestanas = Ui.Pestanas(h.Raiz, titulos, (i, hoja) => {
                if (i == 0) PintarColumnasDelTablero(hoja);
                else if (i == 1) PintarEquipoDelTablero(hoja);
                else PintarBacklog(hoja);
            }, Mathf.Clamp(_pestanaDelTablero, 0, titulos.Count - 1));
            pestanas.AlCambiar = i => _pestanaDelTablero = i;
        }

        private void PintarColumnasDelTablero(RectTransform hoja) {
            var t = S.Tablero;
            var p = Politica;
            if (p != null && !string.IsNullOrEmpty(p.LoQueNoHay)) Ui.Texto(hoja, p.LoQueNoHay, EstiloTexto.Pequeno, Tema.inkMuted);

            // Lo propio de cada metodologia, arriba del tablero.
            if (p != null && p.LimiteDeWip) {
                var wip = Ui.Fila(hoja, "WIP", Tema.Espacio(3));
                var factor = MotorDelTablero.FactorDelWip(t);
                Ui.Texto(wip, $"Límite de trabajo en curso: {S.R.LimiteWip}", EstiloTexto.Cuerpo, Tema.ink).fontStyle = FontStyles.Bold;
                var menos = Ui.Boton(wip, "− 1", () => Actuar(() => S.AjustarLimiteWip(-1), "Bajaste el límite de trabajo en curso."));
                menos.interactable = S.PorQueNoSePuedeAjustarElWip(-1) == null;
                var mas = Ui.Boton(wip, "+ 1", () => Actuar(() => S.AjustarLimiteWip(1), "Subiste el límite de trabajo en curso."));
                mas.interactable = S.PorQueNoSePuedeAjustarElWip(1) == null;
                Ui.Texto(wip, factor > 1.001 ? "El equipo está más enfocado: rinde un poco más."
                            : factor < 0.999 ? $"Demasiadas cosas a la vez (o gente parada): el equipo rinde un {(1 - factor) * 100:0} % menos."
                            : $"Moverlo cuesta {GameSession.MinutosDeAjustarWip} min. Menos cosas a medias suele ser más cosas terminadas.",
                         EstiloTexto.Pequeno, factor < 0.999 ? Tema.warning : factor > 1.001 ? Tema.success : Tema.inkMuted);
            }
            var etapa = S.EtapaDeHoy();
            if (etapa != null) {
                var etapas = Ui.Fila(hoja, "Etapas", Tema.Espacio(2));
                var pasada = true;
                foreach (var e in S.Metodologia.Calendario.Etapas) {
                    var hoy = e.Id == etapa.Id;
                    if (hoy) pasada = false;
                    Ui.Badge(etapas, (pasada ? "√ " : "") + e.Nombre + (hoy ? " · hoy" : ""), hoy ? Tono.Cyan : pasada ? Tono.Exito : Tono.Neutro, hoy);
                }
            }
            if (p != null && p.SprintCerrado && t.SprintActual >= 0)
                Ui.Texto(hoja, $"Sprint {t.SprintActual + 1}: comprometidos {S.R.CompromisoActual:0.#} puntos · quedan {S.RestanteDelSprint():0.#}" +
                               (S.R.SobreCompromiso > 0.05 ? $" · <color={NexusTheme.Html(Tema.warning)}>sobrecompromiso de {S.R.SobreCompromiso:0.#}: genera deuda cada día</color>" : ""),
                         EstiloTexto.Cuerpo, Tema.ink);
            if (S.PrPendiente != null) {
                var n = Ui.Notificacion(hoja, $"Un cambio espera revisión: «{S.PrPendiente.Titulo}» ({GameSession.MinutosDeRevisarPr} min).", Tono.Aviso);
                Ui.Boton(n, "Revisar", AbrirRevision);
            }

            var columnas = Ui.Fila(hoja, "Columnas", Tema.espacio, alineacion: TextAnchor.UpperLeft);
            foreach (var id in new[] { ColumnasDeBase.PorHacer, ColumnasDeBase.Haciendo, ColumnasDeBase.Hecho }) {
                var tarjetas = TarjetasDeLaColumna(id);
                var titulo = NombreDeColumna(id) + " · " + tarjetas.Count +
                             (id == ColumnasDeBase.Haciendo && p != null && p.LimiteDeWip ? " de " + S.R.LimiteWip : "");
                var columna = Ui.Tarjeta(columnas, titulo, id == ColumnasDeBase.Hecho ? Tono.Exito : Tono.Neutro);
                UiKit.Tamano(columna, ancho: 300, flexAncho: id == ColumnasDeBase.Haciendo ? 1.6f : 1);
                if (tarjetas.Count == 0) Ui.Texto(columna, "Nada aquí.", EstiloTexto.Pequeno, Tema.inkMuted);
                // Lo hecho se resume: lo que importa de esa columna es que crece.
                foreach (var c in id == ColumnasDeBase.Hecho ? tarjetas.OrderByDescending(x => x.DiaFin).Take(6) : tarjetas.Take(8))
                    PintarTarjeta(columna, c);
                var resto = tarjetas.Count - (id == ColumnasDeBase.Hecho ? 6 : 8);
                if (resto > 0) Ui.Texto(columna, $"… y {resto} más.", EstiloTexto.Pequeno, Tema.inkMuted);
            }
        }

        /// <summary>Las tarjetas de una columna. Con sprint, «por hacer» es solo lo comprometido: el resto esta en el product backlog.</summary>
        private List<TarjetaDeTrabajo> TarjetasDeLaColumna(string columna) {
            var t = S.Tablero;
            var lista = t.Tarjetas.Where(c => !c.EsBug && (c.Columna == columna || (columna == ColumnasDeBase.Haciendo && c.Columna == ColumnasDeBase.Revision)));
            if (columna == ColumnasDeBase.PorHacer && Politica != null && Politica.SprintCerrado && t.SprintActual >= 0)
                lista = lista.Where(c => c.Sprint == t.SprintActual);
            return lista.OrderBy(c => c.Orden).ToList();
        }

        private void PintarTarjeta(Transform columna, TarjetaDeTrabajo c) {
            var t = S.Tablero;
            var ficha = Ui.PanelColumna(columna, "Tarjeta " + c.Id, Tema.Espacio(3), Tema.Espacio(1), Tema.surfaceRaised);
            Ui.Texto(ficha, c.Titulo, EstiloTexto.Cuerpo, Tema.ink).fontStyle = FontStyles.Bold;
            Ui.Texto(ficha, $"{c.Puntos:0.#} pts · {NombreDeTipo(c.Tipo)}" + (c.Terminada && c.DiaFin >= 0 ? $" · terminada el día {c.DiaFin}" : ""),
                     EstiloTexto.Leyenda, Tema.inkFaint);
            if (c.Terminada) return;

            if (!c.Empezada) {
                var orden = Ui.Fila(ficha, "Orden", Tema.Espacio(2));
                var sube = Ui.Boton(orden, "▲ Antes", () => Actuar(() => S.PriorizarTarjeta(c.Id, true), $"«{c.Titulo}» sube en el orden."), VarianteBoton.Fantasma);
                sube.interactable = S.PorQueNoSePuedePriorizar(c.Id, true) == null;
                var baja = Ui.Boton(orden, "▼ Después", () => Actuar(() => S.PriorizarTarjeta(c.Id, false), $"«{c.Titulo}» baja en el orden."), VarianteBoton.Fantasma);
                baja.interactable = S.PorQueNoSePuedePriorizar(c.Id, false) == null;
                return;
            }

            Ui.Barra(ficha, (float)(c.Trabajo / Math.Max(0.01, c.Puntos)), c.AsignacionManual ? Tema.success : Tema.cyan);
            var lleva = t.Miembro(c.Asignado);
            Ui.Texto(ficha, lleva == null ? "Nadie la lleva." : $"La lleva {lleva.Nombre}" + (c.AsignacionManual ? " (se la diste tú): " + ComoSeLeDa(lleva, c.Tipo) : " (reparto automático)"),
                     EstiloTexto.Pequeno, Tema.ink);
            var quienes = Ui.Fila(ficha, "Asignar", Tema.Espacio(2));
            Ui.Texto(quienes, "Dársela a:", EstiloTexto.Leyenda, Tema.inkFaint);
            foreach (var m in t.Miembros) {
                var miembro = m;
                var boton = Ui.Boton(quienes, miembro.Nombre, () => Actuar(() => S.AsignarTarjeta(c.Id, miembro.Id),
                                     $"«{c.Titulo}» pasa a {miembro.Nombre}: {ComoSeLeDa(miembro, c.Tipo)}."), VarianteBoton.Fantasma);
                boton.interactable = S.PorQueNoSePuedeAsignar(c.Id, miembro.Id) == null;
                Ui.Resaltar(boton, c.Asignado == miembro.Id && c.AsignacionManual);
            }
        }

        private static string ComoSeLeDa(MiembroDelEquipo m, string tipo) {
            var h = m.HabilidadPara(tipo);
            return h >= 1.2 ? "es su fuerte" : h <= 0.85 ? "le cuesta" : "se le da normal";
        }

        /// <summary>Quien es quien: a que se le da bien cada uno. Es con lo que se decide a quien darle cada tarjeta.</summary>
        private void PintarEquipoDelTablero(RectTransform hoja) {
            var t = S.Tablero;
            Ui.Texto(hoja, $"Darle una tarjeta a quien se le da bien ese tipo de trabajo la hace rendir más; a quien le cuesta, menos. Asignar cuesta {GameSession.MinutosDeAsignar} min. " +
                           "Si no tocas nada, el equipo se reparte solo, por turnos y sin mirar a quién se le da mejor.", EstiloTexto.Pequeno, Tema.ink);
            var fila = Ui.Fila(hoja, "Equipo", Tema.espacio, alineacion: TextAnchor.UpperLeft);
            foreach (var m in t.Miembros) {
                var ficha = Ui.Tarjeta(fila, m.Nombre);
                UiKit.Tamano(ficha, ancho: 260, flexAncho: 1);
                if (!string.IsNullOrEmpty(m.Rol)) Ui.Texto(ficha, m.Rol, EstiloTexto.Pequeno, Tema.inkMuted);
                foreach (var tipo in new[] { "interfaz", "datos", "logica", "pruebas" }) {
                    var h = m.HabilidadPara(tipo);
                    var linea = Ui.Fila(ficha, "Habilidad", Tema.Espacio(2));
                    UiKit.Tamano(Ui.Texto(linea, NombreDeTipo(tipo), EstiloTexto.Pequeno, Tema.ink), ancho: 90);
                    Ui.Barra(linea, Mathf.InverseLerp(0.5f, 1.5f, (float)h), h >= 1.2 ? Tema.success : h <= 0.85 ? Tema.warning : Tema.cyan);
                }
                var lleva = t.Tarjetas.Where(c => c.Asignado == m.Id && c.Empezada && !c.Terminada).Select(c => c.Titulo).ToList();
                Ui.Texto(ficha, lleva.Count == 0 ? "Ahora no lleva ninguna tarjeta." : "Lleva: " + string.Join(" · ", lleva), EstiloTexto.Pequeno, Tema.ink);
            }
        }

        /// <summary>Scrum: lo que no esta en el sprint. Su orden decide que entra en el siguiente; meterlo en este cuesta.</summary>
        private void PintarBacklog(RectTransform hoja) {
            var t = S.Tablero;
            Ui.Texto(hoja, "Lo que está arriba entra primero en el próximo sprint: ordénalo ANTES de planificar. Meter algo en el sprint que ya está en marcha se puede, " +
                           "pero el equipo se había comprometido con otra cosa: cuesta moral y genera sobrecompromiso.", EstiloTexto.Pequeno, Tema.ink);
            var fuera = t.Tarjetas.Where(c => !c.EsBug && !c.Terminada && (t.SprintActual < 0 || c.Sprint != t.SprintActual)).OrderBy(c => c.Orden).ToList();
            if (fuera.Count == 0) { Ui.Texto(hoja, "No queda nada fuera del sprint.", EstiloTexto.Cuerpo); return; }
            for (var i = 0; i < fuera.Count; i++) {
                var c = fuera[i];
                var fila = Ui.PanelColumna(hoja, "Historia", Tema.Espacio(3), Tema.Espacio(1), Tema.surfaceRaised);
                var linea = Ui.Fila(fila, "Linea", Tema.Espacio(3));
                UiKit.Tamano(Ui.Texto(linea, $"{i + 1}. {c.Titulo}  ·  {c.Puntos:0.#} pts · {NombreDeTipo(c.Tipo)}", EstiloTexto.Cuerpo, Tema.ink), flexAncho: 1);
                var sube = Ui.Boton(linea, "▲", () => Actuar(() => S.PriorizarTarjeta(c.Id, true), $"«{c.Titulo}» sube en el backlog."), VarianteBoton.Fantasma);
                sube.interactable = S.PorQueNoSePuedePriorizar(c.Id, true) == null;
                var baja = Ui.Boton(linea, "▼", () => Actuar(() => S.PriorizarTarjeta(c.Id, false), $"«{c.Titulo}» baja en el backlog."), VarianteBoton.Fantasma);
                baja.interactable = S.PorQueNoSePuedePriorizar(c.Id, false) == null;
                var meter = Ui.Boton(linea, "Meter en este sprint", () => Actuar(() => S.MeterAlSprint(c.Id), $"Metiste «{c.Titulo}» a mitad de sprint: moral −2 y sobrecompromiso."));
                meter.interactable = S.PorQueNoSePuedeMeterAlSprint(c.Id) == null;
            }
        }

        // ==================================================================== revisar un cambio de codigo

        private void AbrirRevision() {
            var pr = S.PrPendiente;
            if (pr == null) return;
            CerrarPanel();
            ResultadoDeRevision resultado = null;
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<PantallaDeFicha>(p => {
                p.Titulo = "Revisión de código · " + pr.Titulo;
                p.AnchoDeLaFicha = 1100;
                p.AlCerrar = () => _escenaApilada = false;
                p.Pintar = h => PintarRevision(h, pr, () => resultado, r => { resultado = r; App.Router.Repintar(); });
            });
        }

        private void PintarRevision(Hoja h, MicroPr pr, Func<ResultadoDeRevision> leer, Action<ResultadoDeRevision> alDecidir) {
            if (!string.IsNullOrEmpty(pr.Descripcion)) h.Parrafo("Lo que dice quien lo escribió: «" + pr.Descripcion + "»");
            var codigo = Ui.PanelColumna(h.Raiz, "Diff", Tema.Espacio(4), 2, Tema.bg950);
            foreach (var linea in pr.Lineas) {
                var texto = Ui.Texto(codigo, (linea.Tipo == "add" ? "+ " : linea.Tipo == "del" ? "− " : "  ") + linea.Texto, EstiloTexto.Mono);
                texto.color = linea.Tipo == "add" ? Tema.success : linea.Tipo == "del" ? Tema.danger : Tema.inkMuted;
            }
            var r = leer();
            if (r == null) {
                h.Nota($"En verde, lo que añade; en rojo, lo que quita. Revisarlo cuesta {GameSession.MinutosDeRevisarPr} min. Si lo cierras sin decidir, el equipo se lo revisa entre ellos.");
                var botones = h.Fila();
                Ui.Boton(botones, "Aprobar: está bien", () => Revisar(VeredictosDePr.Aprobar, alDecidir), VarianteBoton.Primario);
                Ui.Boton(botones, "Pedir cambios: algo falla", () => Revisar(VeredictosDePr.PedirCambios, alDecidir));
                return;
            }
            var t = h.Tarjeta(r.Titulo, r.Acierto ? Tono.Exito : Tono.Aviso);
            Ui.Texto(t, r.Explicacion, EstiloTexto.Cuerpo, Tema.ink);
            if (r.Cambios.Count > 0) Ui.Texto(t, "Lo que cambió: " + Textos.Previsualizar(r.Cambios), EstiloTexto.Pequeno, Tema.warning);
        }

        private void Revisar(string veredicto, Action<ResultadoDeRevision> alDecidir) {
            try {
                ResultadoDeAvance avance;
                var r = S.RevisarPr(veredicto, out avance);
                Runner.Notificar(avance);
                Anotar($"{S.HoraActual} · Revisión de código: {r.Titulo.ToLowerInvariant()}.");
                alDecidir(r);
            } catch (InvalidOperationException e) {
                Anotar("No se pudo revisar: " + e.Message);
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
                           $"Inspeccionar el de la etapa actual con el equipo cuesta {GameSession.MinutosDeInspeccionar} min y se puede una vez al día.", EstiloTexto.Pequeno, Tema.ink);
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
                        var inspeccionar = Ui.Boton(cabecera, "Inspeccionarlo", () => Actuar(() => S.InspeccionarDocumento(d), $"Inspeccionasteis el {d}: documentación +3 y mejor calidad del documento."));
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
            var probar = Ui.Boton(Ui.Fila(t), "Hacer una ronda de pruebas", () => Actuar(() => S.EjecutarPruebas(), "El equipo hizo una ronda de pruebas: " + Textos.Previsualizar(efecto) + "."), VarianteBoton.Primario);
            probar.interactable = motivo == null;
            if (motivo != null) Ui.Texto(t, motivo, EstiloTexto.Pequeno, Tema.warning);
            if (S.R.VecesQueSeProbo > 0) Ui.Texto(hoja, $"Rondas de pruebas hechas en este proyecto: {S.R.VecesQueSeProbo}.", EstiloTexto.Pequeno);
        }

        // ==================================================================== ceremonias

        private void AbrirCeremonias(int pestana) {
            _pestanaDeCeremonias = pestana;
            _ultimoResultado = null;
            AbrirPanel(S.Metodologia.Nombre, "Ceremonias", 1320, PintarPanelDeCeremonias,
                       () => S.R.CeremoniasHechasHoy.Count + "|" + S.R.ActasDeCeremonias.Count + "|" + S.R.DiaActual + "|" + _ultimoResultado + "|" + AlertasSonando().Count);
        }

        private void PintarPanelDeCeremonias(Hoja h) {
            AvisoDeAlertaEnPanel(h);
            if (Politica != null && !string.IsNullOrEmpty(Politica.LoQueNoHay)) h.Nota(Politica.LoQueNoHay);
            var pestanas = Ui.Pestanas(h.Raiz, new[] { "Hoy", "Agenda del proyecto", "Actas" }, (i, hoja) => {
                if (i == 0) PintarCeremoniasDeHoy(hoja);
                else if (i == 1) PintarAgenda(hoja);
                else PintarActas(hoja);
            }, _pestanaDeCeremonias);
            pestanas.AlCambiar = i => _pestanaDeCeremonias = i;
        }

        private void PintarCeremoniasDeHoy(RectTransform hoja) {
            if (S.R.DiaActual == 0) { Ui.Texto(hoja, "Las ceremonias empiezan con el día 1.", EstiloTexto.Cuerpo); return; }
            var plan = S.PlanDeHoy;
            var hoy = S.CeremoniasDeHoy();
            // Las que se resuelven al empezar el dia (planificar, la retrospectiva) no se juegan aqui, pero son de hoy.
            if (plan != null)
                foreach (var c in plan.Ceremonias.Where(x => x.Opciones == null || x.Opciones.Count == 0)) {
                    var t = Ui.Tarjeta(hoja, c.Nombre ?? c.Id);
                    if (!string.IsNullOrEmpty(c.ParaQueSirve)) Ui.Texto(t, c.ParaQueSirve, EstiloTexto.Cuerpo, Tema.ink);
                    Ui.Texto(t, c.AjustaCoeficiente || string.Equals(c.Verbo, "V5", StringComparison.OrdinalIgnoreCase)
                                    ? "Se hace al empezar el día, en «La jornada»." : "Hoy toca; pasa sola, sin nada que decidir.", EstiloTexto.Pequeno);
                }
            if (hoy.Count == 0 && (plan == null || plan.Ceremonias.Count == 0)) {
                Ui.Texto(hoja, "Hoy no hay ninguna ceremonia.", EstiloTexto.Cuerpo);
                return;
            }
            foreach (var ceremonia in hoy) {
                var c = ceremonia;
                var t = Ui.Tarjeta(hoja, c.Nombre + (c.Hecha ? " · hecha" : $" · {c.DuracionMinutos} min"), c.Hecha ? Tono.Exito : Tono.Cyan);
                if (!string.IsNullOrEmpty(c.ParaQueSirve)) Ui.Texto(t, c.ParaQueSirve, EstiloTexto.Cuerpo, Tema.ink);
                if (c.Hecha) {
                    var acta = S.R.ActasDeCeremonias.LastOrDefault(a => a.Dia == S.R.DiaActual && a.CeremoniaId == c.Id);
                    if (acta != null) {
                        Ui.Texto(t, "La llevaste así: " + acta.Opcion, EstiloTexto.Pequeno, Tema.cyan);
                        Ui.Texto(t, acta.Texto, EstiloTexto.Cuerpo, Tema.ink);
                    }
                    continue;
                }
                if (!string.IsNullOrEmpty(c.Texto)) Ui.Texto(t, c.Texto, EstiloTexto.Pequeno).fontStyle = FontStyles.Italic;
                if (!string.IsNullOrEmpty(c.Muestra) && c.Muestra != VistasDeMonitoreo.Tablero) {
                    var delante = Ui.PanelColumna(t, "Delante", Tema.Espacio(3), Tema.Espacio(2), Tema.surfaceSunken);
                    Ui.Texto(delante, "LO QUE HAY SOBRE LA MESA", EstiloTexto.Leyenda, Tema.inkFaint);
                    PintarVistaDeMonitoreo(c.Muestra, delante);
                }
                var motivo = S.PorQueNoSePuedeAsistir(c.Id);
                Ui.Texto(t, motivo ?? "¿Cómo la llevas? Si no asistes, el equipo la hace sin ti: no pasa nada malo, pero tampoco lo que ganarías llevándola bien.",
                         EstiloTexto.Pequeno, motivo == null ? Tema.inkMuted : Tema.warning);
                foreach (var opcion in c.Opciones) {
                    var o = opcion;
                    var boton = Ui.BotonDeOpcion(t, o.Texto, null, () => Actuar(() => S.AsistirACeremonia(c.Id, o.Id), $"Asististe a «{c.Nombre}»."));
                    boton.interactable = motivo == null;
                }
            }
        }

        private void PintarAgenda(RectTransform hoja) {
            var agenda = S.AgendaDeCeremonias();
            if (agenda.Count == 0) { Ui.Texto(hoja, "Esta metodología no tiene ceremonias.", EstiloTexto.Cuerpo); return; }
            Ui.Texto(hoja, "Lo que marca el ritmo del proyecto con esta metodología, día a día.", EstiloTexto.Pequeno, Tema.ink);
            // Las diarias se dicen una vez; el resto, con su dia.
            var diarias = agenda.GroupBy(c => c.Id).Where(g => g.Count() >= S.Perfil.DiasTotales).Select(g => g.First().Nombre).ToList();
            if (diarias.Count > 0) Ui.Notificacion(hoja, "Todos los días: " + string.Join(" · ", diarias), Tono.Cyan);
            foreach (var dia in agenda.Where(c => !diarias.Contains(c.Nombre)).GroupBy(c => c.Dia).OrderBy(g => g.Key)) {
                var cuando = dia.First().Cuando;
                Ui.Notificacion(hoja, string.Join(" · ", dia.Select(c => c.Nombre)), cuando == "hoy" ? Tono.Aviso : cuando == "pasada" ? Tono.Neutro : Tono.Cyan,
                                cuando == "hoy" ? $"día {dia.Key} · hoy" : "día " + dia.Key);
            }
        }

        private void PintarActas(RectTransform hoja) {
            var actas = S.R.ActasDeCeremonias;
            if (actas.Count == 0) { Ui.Texto(hoja, "Todavía no hay actas: se escriben al asistir a una ceremonia o al cerrar el día.", EstiloTexto.Cuerpo); return; }
            var asistidas = actas.Count(a => a.Asistio);
            Ui.Texto(hoja, $"Has asistido a {asistidas} de {actas.Count} ceremonias.", EstiloTexto.Cuerpo, Tema.ink);
            foreach (var a in actas.AsEnumerable().Reverse().Where(x => x.Asistio).Take(20)) {
                var t = Ui.PanelColumna(hoja, "Acta", Tema.Espacio(3), Tema.Espacio(1), Tema.surfaceRaised);
                Ui.Texto(t, $"Día {a.Dia} · {a.Nombre}", EstiloTexto.Leyenda, Tema.cyan);
                Ui.Texto(t, a.Opcion, EstiloTexto.Pequeno, Tema.ink).fontStyle = FontStyles.Bold;
                Ui.Texto(t, a.Texto, EstiloTexto.Pequeno, Tema.ink);
            }
        }
    }
}
