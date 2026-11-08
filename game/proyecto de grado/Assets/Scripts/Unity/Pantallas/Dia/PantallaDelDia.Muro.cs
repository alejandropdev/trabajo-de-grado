using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Sesion;
using Nexus.Core.Tablero;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El muro del backlog, lo que abre «Backlog» en el HUD y «Detalle» en el carrusel. 2026-11-06: de un panel de tres
    /// pestañas de texto a fichas que se arrastran. 2026-11-07: con el lenguaje de la pantalla de indicadores (paneles
    /// translucidos, cifras grandes, barras, mini-graficas) y sin huecos muertos:
    ///
    ///   arriba        lo de la metodologia (sprint y su tubo, limite de WIP, etapas), «Revisar código» y «?»
    ///   indicadores   por hacer (y de que tipo), en curso contra su limite, hecho (con su tendencia), el ritmo (¿llega
    ///                 a tiempo?) y quien esta libre
    ///   filtro        ver solo un tipo de trabajo (gratis)
    ///   carriles      Product backlog | Sprint | En curso | Hecho (Scrum); Por hacer | En curso | Hecho (las demas).
    ///                 En curso enseña sus huecos libres (la siguiente entra sola), el backlog la raya de lo que cabe en
    ///                 el proximo sprint, por hacer marca «la siguiente»; abajo de cada uno, su reparto por tipos
    ///   banco         las caras: al coger una ficha, cada una dice si es su fuerte; al pasar por una cara se ven sus
    ///                 tarjetas, y al pasar por una ficha se enciende la cara de quien la lleva
    ///
    /// Se arrastra para ordenar (5 min, llegue donde llegue), para meter algo en el sprint y para dar una tarjeta. Un clic
    /// abre la hoja de la ficha con lo mismo en botones. Todo pasa por Actuar (cuesta sus minutos; mirar es gratis).
    /// </summary>
    public sealed partial class PantallaDelDia {
        private PantallaDelMuro _muro;
        private string _fichaAbierta, _filtroDelMuro;
        private bool _hechoAbierto, _ayudaDelMuro;

        // Lo que se enciende al pasar el raton: el aro de cada cara y las fichas de cada uno.
        private readonly Dictionary<string, Image> _arosDelBanco = new Dictionary<string, Image>();
        private readonly List<FichaDelMuro> _fichasDelMuro = new List<FichaDelMuro>();

        private sealed class FichaDelMuro {
            public string Miembro;
            public CanvasGroup Grupo;
            public float Alfa;
        }

        private const string CarrilBacklog = "backlog", CarrilSprint = "sprint", CarrilPorHacer = "porhacer",
                             CarrilEnCurso = "curso", CarrilHecho = "hecho";
        private const string FiltroErrores = "errores";

        private void AbrirMuro() {
            if (_panel != null || _muro != null || _salaDeCeremonias != null || _salaDeSuministros != null) return;
            _decidido = null;
            _fichaAbierta = null;
            _asignarTarjeta = _asignarA = null;
            _filtroDelMuro = null;
            _hechoAbierto = false;
            _ayudaDelMuro = false;
            _ultimoResultado = null;
            _muro = AbrirPantallaDeIndicadores(S.Metodologia.Nombre + (S.BriefDeHoy != null ? " · " + S.BriefDeHoy.EtiquetaUnidad : ""),
                                               Solo("Backlog del equipo", "Tu backlog"), PintarMuro, ClaveDelMuro, m => _muro = m);
        }

        private string ClaveDelMuro() {
            var t = S.Tablero;
            if (t == null) return "";
            return S.MinutoDelDia + "|" + S.R.LimiteWip + "|" + (S.PrPendiente != null) + "|" + AlertasSonando().Count + "|" +
                   _fichaAbierta + "|" + _asignarTarjeta + _asignarA + "|" + _filtroDelMuro + "|" + _hechoAbierto + "|" + _ayudaDelMuro + "|" + t.SprintActual + "|" +
                   string.Join(",", t.Tarjetas.Select(c => c.Id + c.Columna + c.Asignado + c.Orden + c.Sprint));
        }

        private void PintarMuro(PantallaDelMuro m) {
            var t = S.Tablero;
            if (t == null) { PiezasDelMuro.Vacio(Ui, m.Cuerpo, "El backlog aparece al empezar el desarrollo."); return; }
            _arosDelBanco.Clear();
            _fichasDelMuro.Clear();
            var animar = m.PrimeraVez;
            GuiaView.Registrar("dia.tablero", m.Cuerpo);
            HerramientasDelMuro(m);

            var sonando = AlertasSonando().Count;
            if (sonando > 0) {
                var tira = Ui.Fila(m.Cuerpo, "Aviso sonando", Tema.espacio, 10, TextAnchor.MiddleLeft);
                Ui.Fondo(tira.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.warning, 0.14f), 12);
                Ui.Punto(tira, Tema.warning, 12);
                UiKit.Tamano(Ui.Texto(tira, sonando == 1 ? "Ha sonado un aviso: su tiempo corre en cuanto vuelvas." : $"Han sonado {sonando} avisos: su tiempo corre en cuanto vuelvas.",
                                      EstiloTexto.Pequeno, Tema.ink), flexAncho: 1);
                Ui.Boton(tira, "Volver a la jornada", CerrarPanel, VarianteBoton.Primario);
            }
            if (_ayudaDelMuro) {
                var ayuda = Ui.Fila(m.Cuerpo, "Ayuda", Tema.espacio, 0, TextAnchor.MiddleLeft);
                PantallaDeGlosario.Palabras(App, ayuda, "Palabras del backlog:", "backlog", "columnas-del-tablero", "historia-de-usuario", "punto-de-historia",
                                            Politica != null && Politica.SprintCerrado ? "sprint" : "wip");
            }

            var conSprint = Politica != null && Politica.SprintCerrado;
            var sprintVivo = conSprint && t.SprintActual >= 0;
            var enCurso = t.Tarjetas.Where(c => (c.Columna == ColumnasDeBase.Haciendo || c.Columna == ColumnasDeBase.Revision) && !c.Terminada)
                           .OrderBy(c => c.Orden).ToList();
            var huecosEnCurso = Math.Max(0, CapacidadEnCurso() - enCurso.Count(c => !c.EsBug));

            IndicadoresDelMuro(m, enCurso, animar);
            FiltroDelMuro(m);

            var carriles = Ui.Fila(m.Cuerpo, "Carriles", Tema.espacio, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(carriles, flexAncho: 1, flexAlto: 1);
            var total = Math.Max(0.1, t.Tarjetas.Where(c => !c.EsBug).Sum(c => c.Puntos));
            var orden = 0;

            if (conSprint) {
                var fuera = t.Tarjetas.Where(c => !c.Terminada && !c.EsBug && (t.SprintActual < 0 || c.Sprint != t.SprintActual)).OrderBy(c => c.Orden).ToList();
                CarrilDeFichas(m, carriles, CarrilBacklog, "Product backlog", Tema.inkMuted, fuera, 1f, total, orden++, animar, 0,
                               sprintVivo ? "Lo de arriba entra primero en el próximo sprint." : "Ordénalo antes de planificar: lo de arriba entra primero.");
            }
            if (!conSprint || sprintVivo) {
                var porHacer = t.Tarjetas.Where(c => c.Columna == ColumnasDeBase.PorHacer && !c.Terminada &&
                                                     (!sprintVivo || c.Sprint == t.SprintActual || c.EsBug)).OrderBy(c => c.EsBug ? 0 : 1).ThenBy(c => c.Orden).ToList();
                CarrilDeFichas(m, carriles, sprintVivo ? CarrilSprint : CarrilPorHacer,
                               sprintVivo ? $"Sprint {t.SprintActual + 1}" : NombreDeColumna(ColumnasDeBase.PorHacer), Tema.cyan, porHacer, 1f, total, orden++, animar,
                               huecosEnCurso, sprintVivo ? "Arrastra aquí lo del backlog para meterlo (cuesta)." : "Arriba, lo que se empieza primero.");
            }
            CarrilDeFichas(m, carriles, CarrilEnCurso, NombreDeColumna(ColumnasDeBase.Haciendo), Tema.warning, enCurso, 1.2f, total, orden++, animar,
                           huecosEnCurso, "Suelta una ficha en una cara para dársela.");
            CarrilDeHechas(m, carriles, t.Tarjetas.Where(c => c.Terminada).OrderByDescending(c => c.DiaFin).ThenByDescending(c => c.Orden).ToList(), total, orden, animar);

            if (t.Miembros.Count >= 2) BancoDelEquipo(m, animar);
            if (!string.IsNullOrEmpty(_fichaAbierta)) HojaDeLaFicha(m, t.Tarjeta(_fichaAbierta));
            if (!string.IsNullOrEmpty(_asignarTarjeta)) ConfirmarAsignacion(m);
        }

        /// <summary>Cuantas tarjetas caben a la vez en curso: el limite de WIP (Kanban) o una por persona.</summary>
        private int CapacidadEnCurso() {
            // La misma cuenta que usa el motor: el limite de WIP, el que se subio al asignar, o una por persona.
            return MotorDelTablero.EnCurso(S.Tablero);
        }

        // ==================================================================== arriba

        private void HerramientasDelMuro(PantallaDelMuro m) {
            var t = S.Tablero;
            var h = m.Herramientas;
            var p = Politica;
            if (p != null && p.SprintCerrado && t.SprintActual >= 0) {
                var dias = S.DiasDeLaUnidadActual();
                var dia = Mathf.Clamp(S.R.DiaActual - S.R.DiaDeInicioDelSprint + 1, 1, dias);
                Ui.Texto(h, $"Sprint {t.SprintActual + 1}, día {dia} de {dias}", EstiloTexto.Encabezado, Tema.ink);
                var enSprint = t.Tarjetas.Where(c => !c.EsBug && c.Sprint == t.SprintActual).Sum(c => c.Puntos);
                var medidor = MedidorDeCapacidad.Crear(Ui, h, "muro.sprint", false, 240, 20);
                medidor.Fijar(enSprint, Math.Max(0.1, S.R.CompromisoActual));
                if (S.R.SobreCompromiso > 0.05)
                    Ui.Chip(h, $"sobrecompromiso {S.R.SobreCompromiso:0.#}: deuda cada día", NexusTheme.Alfa(Tema.warning, 0.2f), Tema.warning);
            } else if (p != null && p.LimiteDeWip) {
                Ui.Texto(h, "LÍMITE EN CURSO", EstiloTexto.Leyenda, Tema.inkMuted);
                var menos = Ui.Boton(h, "-", () => ActuarEnElMuro(null, () => S.AjustarLimiteWip(-1), "Bajaste el límite de trabajo en curso.", GameSession.MinutosDeAjustarWip), VarianteBoton.Secundario);
                menos.interactable = S.PorQueNoSePuedeAjustarElWip(-1) == null;
                var cifra = Ui.Texto(h, S.R.LimiteWip.ToString(), EstiloTexto.Hero, Tema.cyan, TextAlignmentOptions.Center);
                cifra.fontSize = 40;
                UiKit.Tamano(cifra, 50);
                var mas = Ui.Boton(h, "+", () => ActuarEnElMuro(null, () => S.AjustarLimiteWip(1), "Subiste el límite de trabajo en curso.", GameSession.MinutosDeAjustarWip), VarianteBoton.Secundario);
                mas.interactable = S.PorQueNoSePuedeAjustarElWip(1) == null;
                var factor = MotorDelTablero.FactorDelWip(t);
                if (factor > 1.001) Ui.Chip(h, $"el equipo rinde +{(factor - 1) * 100:0} %", NexusTheme.Alfa(Tema.success, 0.2f), Tema.success);
                else if (factor < 0.999) Ui.Chip(h, $"el equipo rinde -{(1 - factor) * 100:0} %", NexusTheme.Alfa(Tema.warning, 0.2f), Tema.warning);
            } else {
                var etapa = S.EtapaDeHoy();
                if (etapa != null) {
                    var pasada = true;
                    foreach (var e in S.Metodologia.Calendario.Etapas) {
                        var hoy = e.Id == etapa.Id;
                        if (hoy) pasada = false;
                        Ui.Chip(h, e.Nombre, hoy ? Tema.cyan : NexusTheme.Alfa(pasada ? Tema.success : Tema.inkFaint, 0.2f),
                                hoy ? Tema.onCyan : pasada ? Tema.success : Tema.inkMuted);
                    }
                }
            }
            if (S.PrPendiente != null) Ui.Boton(h, "Revisar código", AbrirRevision, VarianteBoton.Primario);
            Ui.Boton(h, _ayudaDelMuro ? "Ocultar palabras" : "?", () => { _ayudaDelMuro = !_ayudaDelMuro; }, VarianteBoton.Fantasma);
        }

        // ==================================================================== indicadores

        /// <summary>
        /// La franja de indicadores del backlog, como la de la pantalla de indicadores: cinco paneles con su cifra grande,
        /// su barra y lo que dicen. Llenan el muro de lo que importa aunque haya pocas tarjetas.
        /// </summary>
        private void IndicadoresDelMuro(PantallaDelMuro m, List<TarjetaDeTrabajo> enCurso, bool animar) {
            var t = S.Tablero;
            var fila = Ui.Fila(m.Cuerpo, "Indicadores", Tema.espacio, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(fila, alto: 132, flexAncho: 1);
            var tipos = new[] { "interfaz", "datos", "logica", "pruebas" };
            var retraso = 0f;
            Func<string, Color, RectTransform> panel = (rotulo, color) => {
                var p = PiezasDelMuro.Panel(Ui, fila, rotulo, color, 14);
                UiKit.Tamano(p, ancho: 0, flexAncho: 1, flexAlto: 1);
                p.gameObject.AddComponent<PiezaViva>().Escala = 1.02f;
                if (animar) p.gameObject.AddComponent<EntradaDePieza>().Retraso = retraso;
                retraso += 0.06f;
                return p;
            };
            float? cuenta = animar ? (float?)0.2f : null;

            // 1 · lo que falta: puntos sin empezar y de que tipo son
            var pendientes = t.Tarjetas.Where(c => !c.EsBug && !c.Empezada && !c.Terminada).ToList();
            var p1 = panel("Por hacer", Tema.cyan);
            var c1 = Ui.Fila(p1, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, c1, pendientes.Sum(c => c.Puntos), "0", "", Tema.cyan, 40, cuenta ?? -1);
            Ui.Texto(c1, $"pts · {pendientes.Count} tarjetas", EstiloTexto.Pequeno, Tema.inkMuted);
            PiezasDelMuro.Reparto(Ui, p1, tipos.Select(x => new KeyValuePair<Color, double>(PiezasDelMuro.ColorDeTipo(Tema, x, false),
                                                       pendientes.Where(c => c.Tipo == x).Sum(c => c.Puntos))).ToList());

            // 2 · en curso contra lo que cabe
            var capacidad = CapacidadEnCurso();
            var enMarcha = enCurso.Count(c => !c.EsBug);
            var p2 = panel(Politica != null && Politica.LimiteDeWip ? "En curso / límite" : "En curso / personas", Tema.warning);
            var c2 = Ui.Fila(p2, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, c2, enMarcha, "0", "", enMarcha > capacidad ? Tema.danger : Tema.warning, 40, cuenta ?? -1);
            Ui.Texto(c2, $"de {capacidad}", EstiloTexto.Encabezado, Tema.inkMuted);
            PiezasDelMuro.Barra(Ui, p2, capacidad == 0 ? 0 : Mathf.Clamp01((float)enMarcha / capacidad), enMarcha > capacidad ? Tema.danger : Tema.warning, cuenta ?? -1);
            Ui.Texto(p2, enMarcha < capacidad ? $"{capacidad - enMarcha} hueco(s): la siguiente entra sola" : enMarcha > capacidad ? "Demasiado a la vez" : "Lleno",
                     EstiloTexto.Leyenda, enMarcha > capacidad ? Tema.danger : Tema.inkMuted);

            // 3 · hecho, con su tendencia
            var hecho = t.PuntosDeAlcance <= 0 ? 0 : t.TrabajoDeAlcance / t.PuntosDeAlcance;
            var p3 = panel("Hecho", Tema.success);
            var c3 = Ui.Fila(p3, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, c3, hecho * 100, "0", "%", Tema.success, 40, cuenta ?? -1);
            PiezasDelMuro.MiniGrafica(Ui, c3, S.R.SerieAvance, Tema.success, 34, 10);
            PiezasDelMuro.Barra(Ui, p3, (float)hecho, Tema.success, cuenta ?? -1);

            // 4 · el ritmo: ¿llega a tiempo?
            var serie = S.R.SerieAvance;
            var diarios = new List<double>();
            for (var i = 1; i < serie.Count; i++) diarios.Add(Math.Max(0, serie[i] - serie[i - 1]));
            var ritmo = diarios.Count == 0 ? 0 : diarios.Skip(Math.Max(0, diarios.Count - 3)).Average();
            var falta = Math.Max(0, t.PuntosDeAlcance - t.TrabajoDeAlcance);
            var quedanDias = Math.Max(0, S.Perfil.DiasTotales - S.R.DiaActual + 1);
            var necesita = ritmo > 0.05 ? falta / ritmo : double.PositiveInfinity;
            var llega = necesita <= quedanDias + 0.01;
            var colorRitmo = ritmo <= 0.05 ? Tema.inkMuted : llega ? Tema.success : Tema.danger;
            var p4 = panel("Ritmo", colorRitmo);
            var c4 = Ui.Fila(p4, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, c4, ritmo, "0.#", "", colorRitmo, 40, cuenta ?? -1);
            Ui.Texto(c4, "pts / día", EstiloTexto.Pequeno, Tema.inkMuted);
            PiezasDelMuro.MiniGrafica(Ui, c4, diarios, colorRitmo, 34, 10);
            Ui.Texto(p4, ritmo <= 0.05 ? "Se mide al cerrar el primer día" :
                         llega ? $"Llega: ~{Math.Ceiling(necesita):0} días de {quedanDias}" : $"No llega: harían falta ~{Math.Ceiling(necesita):0} días, quedan {quedanDias}",
                     EstiloTexto.Leyenda, colorRitmo);

            // 5 · quien esta libre
            var p5 = panel("Equipo", Tema.violet);
            var libres = t.Miembros.Count(x => !x.DescansaHoy && !t.Tarjetas.Any(c => c.Asignado == x.Id && c.Empezada && !c.Terminada));
            var c5 = Ui.Fila(p5, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, c5, libres, "0", "", libres > 0 ? Tema.success : Tema.violet, 40, cuenta ?? -1);
            Ui.Texto(c5, libres == 1 ? "libre" : "libres", EstiloTexto.Pequeno, Tema.inkMuted);
            var puntos = Ui.Fila(p5, "Personas", 6, 0, TextAnchor.MiddleLeft);
            foreach (var x in t.Miembros) {
                var ocupado = t.Tarjetas.Any(c => c.Asignado == x.Id && c.Empezada && !c.Terminada);
                var punto = Ui.Punto(puntos, x.DescansaHoy ? Tema.inkFaint : ocupado ? Tema.cyan : Tema.success, 14);
                if (!x.DescansaHoy && ocupado) punto.gameObject.AddComponent<Latido>().Fuerza = 0.12f;
            }
        }

        /// <summary>Ver solo un tipo de trabajo: lo demas se apaga. Mirar es gratis.</summary>
        private void FiltroDelMuro(PantallaDelMuro m) {
            var fila = Ui.Fila(m.Cuerpo, "Filtro", 8, 0, TextAnchor.MiddleLeft);
            Ui.Texto(fila, "VER", EstiloTexto.Leyenda, Tema.inkMuted);
            Action<string, string, Color> opcion = (id, nombre, color) => {
                var activo = _filtroDelMuro == id;
                var sitio = Ui.Fila(fila, "Opcion", 6, 0, TextAnchor.MiddleLeft);
                if (id != null) Ui.Punto(sitio, color, 10);
                var boton = Ui.Boton(sitio, nombre, () => { _filtroDelMuro = activo ? null : id; }, VarianteBoton.Fantasma);
                Ui.Resaltar(boton, activo);
            };
            opcion(null, "Todo", Tema.ink);
            foreach (var tipo in new[] { "interfaz", "datos", "logica", "pruebas" })
                opcion(tipo, Capitalizar(PiezasDelMuro.NombreDeTipo(tipo)), PiezasDelMuro.ColorDeTipo(Tema, tipo, false));
            opcion(FiltroErrores, "Errores", Tema.danger);
            UiKit.Tamano(Ui.Nodo(fila, "Hueco"), flexAncho: 1);
            Ui.Texto(fila, "Arrastra para ordenar o dar · clic para ver", EstiloTexto.Leyenda, Tema.inkFaint);
        }

        private bool PasaElFiltro(TarjetaDeTrabajo c) {
            if (string.IsNullOrEmpty(_filtroDelMuro)) return true;
            if (_filtroDelMuro == FiltroErrores) return c.EsBug;
            return !c.EsBug && c.Tipo == _filtroDelMuro;
        }

        private static string Capitalizar(string s) {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        // ==================================================================== carriles

        private void CarrilDeFichas(PantallaDelMuro m, RectTransform padre, string id, string titulo, Color acento,
                                   List<TarjetaDeTrabajo> tarjetas, float ancho, double total, int indice, bool animar,
                                   int huecos, string pista) {
            RectTransform lista;
            DestinoDeFicha destino;
            var puntos = tarjetas.Where(c => !c.EsBug).Sum(c => c.Restante);
            var carril = PiezasDelMuro.Carril(Ui, padre, titulo, tarjetas.Count.ToString(), acento, out lista, out destino,
                                              puntos, (float)(puntos / total), animar ? 0.1f + indice * 0.08f : -1);
            UiKit.Tamano(carril, ancho: 0, flexAncho: ancho, flexAlto: 1);
            var t = S.Tablero;
            var ids = tarjetas.Select(c => c.Id).ToList();

            if (id == CarrilEnCurso && t.TrabajoAdeudado > 0.05) TarjetaDeRetrabajo(lista, t.TrabajoAdeudado);
            if (tarjetas.Count == 0 && id != CarrilEnCurso) PiezasDelMuro.Vacio(Ui, lista, pista, 90);

            // Scrum: hasta donde llega el proximo sprint, con lo que el equipo ha demostrado que hace.
            var corte = id == CarrilBacklog ? CapacidadDelProximoSprint() : -1;
            var acumulado = 0.0;
            var cortado = false;
            // Por hacer: las que entran solas en cuanto haya un hueco en curso.
            var siguientes = id == CarrilPorHacer || id == CarrilSprint ? huecos : 0;
            var n = 0;
            foreach (var c in tarjetas) {
                var tarjeta = c;
                acumulado += c.Restante;
                if (corte > 0 && !cortado && acumulado > corte + 0.05) {
                    cortado = true;
                    PiezasDelMuro.Corte(Ui, lista, $"hasta aquí cabe en el próximo sprint (~{corte:0} pts)", Tema.cyan);
                }
                var quien = string.IsNullOrEmpty(c.Asignado) ? null : t.Miembro(c.Asignado);
                var pareja = quien == null || string.IsNullOrEmpty(quien.ParejaHoy) ? null : t.Miembro(quien.ParejaHoy);
                var espera = t.TrabajoAdeudado > 0.05 && !c.Empezada && c.AsignacionManual && quien != null
                    ? "Asignada: empieza al terminar de rehacer" : null;
                var siguiente = !c.EsBug && !c.Empezada && siguientes > 0;
                if (siguiente) siguientes--;
                var datos = new DatosDeFicha {
                    Id = c.Id, Titulo = c.Titulo, Tipo = c.Tipo, Puntos = c.Puntos, EsBug = c.EsBug,
                    Avance = c.Empezada ? (float)(c.Trabajo / Math.Max(0.01, c.Puntos)) : -1,
                    Quien = quien == null ? null : quien.Nombre, Pareja = pareja == null ? null : pareja.Nombre,
                    Nueva = S.R.DiaActual > 1 && c.DiaEntrada == S.R.DiaActual, Tarde = c.DescubiertaTarde, Nota = espera,
                    Fija = id != CarrilEnCurso && c.Empezada, Siguiente = siguiente,
                    PorQueFija = "Ya está empezada: se ordena lo que aún no se ha tocado.",
                    Entrada = animar ? Mathf.Min(1.2f, 0.25f + indice * 0.08f + n * 0.05f) : -1
                };
                n++;
                var ficha = PiezasDelMuro.Ficha(Ui, lista, datos, m.Capa);
                ficha.Origen = id;
                ficha.AlPulsar = () => { _fichaAbierta = tarjeta.Id; };
                ficha.AlFallar = motivo => m.Avisar(motivo, Tema.warning);
                RegistrarFicha(ficha, c);
            }
            // En curso: los huecos libres, a trazos. Se ve de un vistazo cuanto cabe todavia.
            if (id == CarrilEnCurso)
                for (var i = 0; i < huecos; i++)
                    PiezasDelMuro.Vacio(Ui, lista, i == 0 ? "Hueco libre: la siguiente de la cola entra sola" : "Hueco libre", 64);

            PieDeCarril(carril, tarjetas);
            destino.Motivo = (f, puesto) => MotivoDelCarril(id, ids, f, puesto);
            destino.Coste = f => CosteDelCarril(id, f);
            destino.AlRecibir = (f, puesto) => SoltarEnCarril(id, ids, f, puesto);
        }

        /// <summary>
        /// Lo que entraria en el proximo sprint: lo que el equipo termino en el ultimo (su velocidad) o, si aun no hay
        /// ninguno cerrado, lo que se comprometio. -1 si no se sabe.
        /// </summary>
        private double CapacidadDelProximoSprint() {
            var r = S.R;
            if (r.TerminadoPorIteracion.Count > 0) return r.TerminadoPorIteracion[r.TerminadoPorIteracion.Count - 1];
            if (r.CompromisoActual > 0.05) return r.CompromisoActual;
            return -1;
        }

        /// <summary>Abajo del carril, su reparto por tipo de trabajo: aunque haya pocas fichas, el carril dice algo.</summary>
        private void PieDeCarril(RectTransform carril, List<TarjetaDeTrabajo> tarjetas) {
            var pie = Ui.Columna(carril, "Pie", 6);
            var tipos = new[] { "interfaz", "datos", "logica", "pruebas" };
            PiezasDelMuro.Reparto(Ui, pie, tipos.Select(x => new KeyValuePair<Color, double>(PiezasDelMuro.ColorDeTipo(Tema, x, false),
                                         tarjetas.Where(c => !c.EsBug && c.Tipo == x).Sum(c => c.Restante))).ToList(), 6);
            var leyenda = Ui.Fila(pie, "Leyenda", 10, 0, TextAnchor.MiddleLeft);
            foreach (var x in tipos) {
                var cuantos = tarjetas.Count(c => !c.EsBug && c.Tipo == x);
                if (cuantos == 0) continue;
                var item = Ui.Fila(leyenda, "Tipo", 4, 0, TextAnchor.MiddleLeft);
                Ui.Punto(item, PiezasDelMuro.ColorDeTipo(Tema, x, false), 8);
                Ui.Texto(item, $"{cuantos}", EstiloTexto.Leyenda, Tema.inkMuted);
            }
            var errores = tarjetas.Count(c => c.EsBug);
            if (errores > 0) {
                var item = Ui.Fila(leyenda, "Errores", 4, 0, TextAnchor.MiddleLeft);
                Ui.Punto(item, Tema.danger, 8);
                Ui.Texto(item, $"{errores} error{(errores == 1 ? "" : "es")}", EstiloTexto.Leyenda, Tema.danger);
            }
        }

        /// <summary>Lo que cada ficha enciende al pasar: la cara de quien la lleva. Y el filtro la apaga si no es de su tipo.</summary>
        private void RegistrarFicha(FichaArrastrable ficha, TarjetaDeTrabajo c) {
            var grupo = ficha.GetComponent<CanvasGroup>();
            if (grupo == null) grupo = ficha.gameObject.AddComponent<CanvasGroup>();
            var alfa = PasaElFiltro(c) ? 1f : 0.22f;
            var entrada = ficha.GetComponent<EntradaDePieza>();
            if (entrada != null) entrada.AlfaFinal = alfa;
            else grupo.alpha = alfa;
            _fichasDelMuro.Add(new FichaDelMuro { Miembro = c.Asignado, Grupo = grupo, Alfa = alfa });
            var pasar = ficha.GetComponent<AlPasar>();
            var quien = c.Asignado;
            if (pasar == null || string.IsNullOrEmpty(quien)) return;
            pasar.Entra += () => { Image aro; if (_arosDelBanco.TryGetValue(quien, out aro) && aro != null) aro.color = NexusTheme.Alfa(Tema.cyan, 0.85f); };
            pasar.Sale += () => { Image aro; if (_arosDelBanco.TryGetValue(quien, out aro) && aro != null && FichaArrastrable.EnVuelo == null) aro.color = new Color(1, 1, 1, 0); };
        }

        /// <summary>Por que no se puede soltar aqui (null = si). Soltar en el mismo sitio no es un error: no hace nada.</summary>
        private string MotivoDelCarril(string carril, List<string> ids, FichaArrastrable f, int puesto) {
            var c = S.Tablero.Tarjeta(f.Id);
            if (c == null) return "Esa tarjeta ya no está.";
            switch (carril) {
                case CarrilBacklog:
                    if (f.Origen == CarrilBacklog) return MotivoDeOrden(c, ids, puesto);
                    return f.Origen == CarrilSprint ? "Lo comprometido se queda en el sprint: se habló en la planificación."
                                                    : "Lo empezado no vuelve al backlog: se termina.";
                case CarrilSprint:
                    if (f.Origen == CarrilSprint) return MotivoDeOrden(c, ids, puesto);
                    if (f.Origen == CarrilBacklog) return S.PorQueNoSePuedeMeterAlSprint(f.Id);
                    return "Ya está en marcha: no hace falta meterla.";
                case CarrilPorHacer:
                    if (f.Origen == CarrilPorHacer) return MotivoDeOrden(c, ids, puesto);
                    return "Lo empezado no vuelve atrás: se termina.";
                case CarrilEnCurso:
                    if (f.Origen == CarrilEnCurso) return null;
                    return "El equipo la empieza cuando le toca: ponla la primera, o suéltala en la cara de alguien.";
                default:
                    return "Se termina trabajando, no arrastrando.";
            }
        }

        private string MotivoDeOrden(TarjetaDeTrabajo c, List<string> ids, int puesto) {
            if (c.EsBug) return "Un error se arregla cuando sale: no se ordena.";
            if (c.Empezada) return "Ya está empezada: se ordena lo que aún no se ha tocado.";
            if (puesto < 0) return null;
            var destino = PuestoEnLaFila(c, ids, puesto);
            if (S.FilaDeOrden(c).IndexOf(c) == destino) return null;
            return S.PorQueNoSePuedeMover(c.Id, destino);
        }

        /// <summary>
        /// El puesto en la fila de orden (lo no empezado de su grupo) que corresponde a soltar la ficha delante de la
        /// ficha numero 'puesto' del carril: se cuentan solo las de la fila (no los errores ni lo empezado).
        /// </summary>
        private int PuestoEnLaFila(TarjetaDeTrabajo c, List<string> ids, int puesto) {
            var fila = new HashSet<string>(S.FilaDeOrden(c).Select(x => x.Id));
            return ids.Where(id => id != c.Id).Take(Math.Max(0, puesto)).Count(id => fila.Contains(id));
        }

        private string CosteDelCarril(string carril, FichaArrastrable f) {
            if (carril == CarrilSprint && f.Origen == CarrilBacklog) return $"-{GameSession.MinutosDeMeterAlSprint} min, moral -2";
            if (carril == f.Origen && carril != CarrilEnCurso) return $"-{GameSession.MinutosDePriorizar} min";
            return null;
        }

        private void SoltarEnCarril(string carril, List<string> ids, FichaArrastrable f, int puesto) {
            var c = S.Tablero.Tarjeta(f.Id);
            if (c == null) return;
            if (carril == CarrilSprint && f.Origen == CarrilBacklog) {
                ActuarEnElMuro(f, () => S.MeterAlSprint(c.Id), $"Metiste «{c.Titulo}» a mitad de sprint: moral -2 y sobrecompromiso.", GameSession.MinutosDeMeterAlSprint);
                return;
            }
            if (carril != f.Origen || carril == CarrilEnCurso) return;
            var destino = PuestoEnLaFila(c, ids, puesto);
            if (S.FilaDeOrden(c).IndexOf(c) == destino) return;
            ActuarEnElMuro(f, () => S.MoverTarjetaA(c.Id, destino), $"«{c.Titulo}» pasa al puesto {destino + 1}.", GameSession.MinutosDePriorizar);
        }

        /// <summary>Actua (cobra sus minutos) y lo cuenta en el muro: la marca «-N min» donde cayo la ficha y la linea de abajo.</summary>
        private void ActuarEnElMuro(FichaArrastrable f, Func<ResultadoDeAvance> accion, string hecho, int minutos) {
            var antes = S.MinutoDelDia;
            Actuar(accion, hecho);
            if (_muro == null) return;
            if (S.MinutoDelDia != antes) {
                var donde = f != null ? f.DondeSeSolto : _muro.Capa.TransformPoint(Vector3.zero);
                _muro.Marcar(donde, $"-{minutos} min", Tema.cyan);
                _muro.Avisar(hecho, Tema.cyan);
            } else {
                _muro.Avisar(_ultimoResultado, Tema.warning);
            }
        }

        /// <summary>Hecho: estrecho, con la cifra grande, su barra del total y las ultimas, cada una en una linea.</summary>
        private void CarrilDeHechas(PantallaDelMuro m, RectTransform padre, List<TarjetaDeTrabajo> hechas, double total, int indice, bool animar) {
            RectTransform lista;
            DestinoDeFicha destino;
            var puntos = hechas.Where(c => !c.EsBug).Sum(c => c.Puntos);
            var carril = PiezasDelMuro.Carril(Ui, padre, NombreDeColumna(ColumnasDeBase.Hecho), hechas.Count.ToString(), Tema.success, out lista, out destino,
                                              puntos, (float)(puntos / total), animar ? 0.1f + indice * 0.08f : -1);
            UiKit.Tamano(carril, ancho: 290, flexAlto: 1);
            destino.Motivo = (f, puesto) => "Se termina trabajando, no arrastrando.";

            if (hechas.Count == 0) PiezasDelMuro.Vacio(Ui, lista, "Aquí se apila lo terminado.", 90);
            // Lo terminado por dia: una barrita por dia, como en los indicadores.
            if (hechas.Count > 0) {
                var porDia = new List<double>();
                for (var d = 1; d <= Math.Max(1, S.R.DiaActual); d++) porDia.Add(hechas.Count(c => c.DiaFin == d));
                var serie = Ui.Columna(lista, "Por dia", 4);
                Ui.Texto(serie, "TERMINADAS POR DÍA", EstiloTexto.Leyenda, Tema.inkFaint);
                PiezasDelMuro.MiniGrafica(Ui, serie, porDia, Tema.success, 36, 10);
            }
            var cuantas = _hechoAbierto ? hechas.Count : Math.Min(4, hechas.Count);
            var n = 0;
            foreach (var c in hechas.Take(cuantas)) {
                var fila = Ui.Fila(lista, "Hecha", 8, 8, TextAnchor.MiddleLeft);
                Ui.Fondo(fila.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.bg950, 0.6f), NexusTheme.RadioMd);
                var marca = new Lamina(Ui, fila, 18, 18, "Hecha");
                new Tizador(marca, Ui.FuenteTiza, 0, 0, 0.45f).Si(20, 22, c.EsBug ? Tema.danger : Tema.success, 12);
                var titulo = Ui.Texto(fila, c.Titulo, EstiloTexto.Pequeno, Tema.inkMuted);
                titulo.maxVisibleLines = 1;
                titulo.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Tamano(titulo, flexAncho: 1);
                if (c.DiaFin > 0) Ui.Texto(fila, "día " + c.DiaFin, EstiloTexto.Leyenda, Tema.inkFaint);
                if (animar) fila.gameObject.AddComponent<EntradaDePieza>().Retraso = Mathf.Min(1.2f, 0.3f + indice * 0.08f + n++ * 0.05f);
            }
            if (hechas.Count > 4)
                Ui.Boton(lista, _hechoAbierto ? "Ver menos" : $"Ver las {hechas.Count}", () => { _hechoAbierto = !_hechoAbierto; }, VarianteBoton.Fantasma);
        }

        // ==================================================================== el banco del equipo

        /// <summary>
        /// Las caras del equipo, abajo, cada una en su panel. Cuanto lleva (bolitas que laten), si esta libre, si descansa
        /// o va en pareja. Al coger una ficha, cada cara se rodea del color de lo bien que se le da ese trabajo; soltarla
        /// encima se la da. Al pasar el raton por una cara, sus tarjetas se quedan encendidas y el resto se apaga.
        /// </summary>
        private void BancoDelEquipo(PantallaDelMuro m, bool animar) {
            var t = S.Tablero;
            var fichas = new Dictionary<string, FichaDeMiembro>();
            foreach (var f in S.FichasDelEquipo()) fichas[f.Miembro.Id] = f;
            var banco = Ui.Fila(m.Cuerpo, "Banco", Tema.espacio, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(banco, alto: 124, flexAncho: 1);
            var indice = 0;
            foreach (var miembro in t.Miembros) {
                var mi = miembro;
                FichaDeMiembro ficha;
                fichas.TryGetValue(mi.Id, out ficha);
                var asiento = Ui.Fila(banco, "Asiento " + mi.Nombre, 12, 12, TextAnchor.MiddleLeft);
                UiKit.Tamano(asiento, ancho: 0, alto: 116, flexAncho: 1);
                var fondo = asiento.gameObject.AddComponent<Image>();
                Ui.Fondo(fondo, NexusTheme.Alfa(Tema.surfaceRaised, 0.94f), NexusTheme.RadioLg);
                Ui.Borde(asiento, Tema.lineStrong, NexusTheme.RadioLg, 1);
                if (animar) asiento.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.4f + indice * 0.06f;
                indice++;

                var cara = Ui.Nodo(asiento, "Cara");
                UiKit.Tamano(cara, 74, 74);
                var aro = Ui.Nodo(cara, "Aro");
                aro.anchorMin = aro.anchorMax = new Vector2(0.5f, 0.5f);
                aro.sizeDelta = new Vector2(86, 86);
                var imgAro = aro.gameObject.AddComponent<Image>();
                imgAro.sprite = Ui.SpriteCircular();
                imgAro.color = new Color(1, 1, 1, 0);
                imgAro.raycastTarget = false;
                _arosDelBanco[mi.Id] = imgAro;
                var dentro = Ui.Fila(cara, "Centro", 0, 0, TextAnchor.MiddleCenter);
                UiKit.Rellenar(dentro);
                Rostro(dentro, FotoDe(ficha, mi.Nombre), mi.Nombre, 68);

                var datos = Ui.Columna(asiento, "Datos", 4);
                UiKit.Tamano(datos, flexAncho: 1);
                var nombre = Ui.Texto(datos, mi.Nombre.Split(' ')[0].ToUpperInvariant(), EstiloTexto.Leyenda, Tema.ink);
                nombre.textWrappingMode = TextWrappingModes.NoWrap;
                var lleva = t.Tarjetas.Where(c => c.Asignado == mi.Id && c.Empezada && !c.Terminada).ToList();
                var estado = Ui.Columna(datos, "Estado", 4);
                if (mi.DescansaHoy) Ui.Texto(estado, "descansa hoy", EstiloTexto.Pequeno, Tema.inkFaint);
                else if (lleva.Count == 0) Ui.Texto(estado, "libre", EstiloTexto.Encabezado, Tema.success).gameObject.AddComponent<Latido>().Fuerza = 0.04f;
                else {
                    var bolitas = Ui.Fila(estado, "Lleva", 4, 0, TextAnchor.MiddleLeft);
                    for (var i = 0; i < Math.Min(4, lleva.Count); i++) Ui.Punto(bolitas, Tema.cyan, 9).gameObject.AddComponent<Latido>().Fuerza = 0.15f;
                    var trabajo = Ui.Texto(estado, lleva[0].Titulo, EstiloTexto.Leyenda, Tema.inkMuted);
                    trabajo.maxVisibleLines = 1;
                    trabajo.overflowMode = TextOverflowModes.Ellipsis;
                    PiezasDelMuro.Barra(Ui, estado, (float)(lleva[0].Trabajo / Math.Max(0.01, lleva[0].Puntos)), Tema.cyan, -1, 5).gameObject.AddComponent<BrilloQueRecorre>();
                }
                if (!string.IsNullOrEmpty(mi.ParejaHoy)) {
                    var otro = t.Miembro(mi.ParejaHoy);
                    Ui.Texto(estado, "en pareja" + (otro == null ? "" : " con " + otro.Nombre.Split(' ')[0]), EstiloTexto.Leyenda, Tema.violet);
                }
                var palabra = Ui.Texto(datos, "", EstiloTexto.Encabezado, Tema.ink);
                var gEstado = estado.gameObject.AddComponent<CanvasGroup>();
                palabra.gameObject.SetActive(false);

                // Pasar por la cara: se quedan encendidas sus tarjetas.
                var pasar = asiento.gameObject.AddComponent<AlPasar>();
                pasar.Entra += () => {
                    foreach (var f in _fichasDelMuro) if (f.Grupo != null) f.Grupo.alpha = f.Miembro == mi.Id ? 1f : Mathf.Min(f.Alfa, 0.25f);
                    imgAro.color = NexusTheme.Alfa(Tema.cyan, 0.6f);
                };
                pasar.Sale += () => {
                    if (FichaArrastrable.EnVuelo != null) return;
                    foreach (var f in _fichasDelMuro) if (f.Grupo != null) f.Grupo.alpha = f.Alfa;
                    imgAro.color = new Color(1, 1, 1, 0);
                };
                asiento.gameObject.AddComponent<PiezaViva>().Escala = 1.03f;

                var destino = asiento.gameObject.AddComponent<DestinoDeFicha>();
                destino.Ui = Ui;
                destino.Acento = Tema.cyan;
                destino.Radio = NexusTheme.RadioLg;
                destino.SitioDelChip = new Vector2(0, 4);
                destino.Motivo = (f, _) => S.PorQueNoSePuedeAsignar(f.Id, mi.Id);
                destino.Coste = f => $"-{GameSession.MinutosDeAsignar} min";
                destino.AlRecibir = (f, _) => {
                    var c = S.Tablero.Tarjeta(f.Id);
                    if (c == null) return;
                    IntentarAsignar(f, c, mi);
                };
                destino.AlEmpezarVuelo = f => {
                    var h = mi.HabilidadPara(f.Tipo);
                    var color = h >= 1.2 ? Tema.success : h <= 0.85 ? Tema.warning : Tema.cyan;
                    imgAro.color = NexusTheme.Alfa(color, 0.85f);
                    palabra.text = h >= 1.2 ? "SU FUERTE" : h <= 0.85 ? "LE CUESTA" : "NORMAL";
                    palabra.color = color;
                    palabra.gameObject.SetActive(true);
                    gEstado.alpha = 0;
                };
                destino.AlTerminarVuelo = f => {
                    imgAro.color = new Color(1, 1, 1, 0);
                    palabra.gameObject.SetActive(false);
                    gEstado.alpha = 1;
                };
            }
        }

        // ==================================================================== asignar (con aviso si no empezaria)

        /// <summary>La asignacion que espera a que el jugador diga si sube el limite: tarjeta y persona.</summary>
        private string _asignarTarjeta, _asignarA;

        /// <summary>
        /// Darle una tarjeta a alguien. Si con eso no empezaria (todo lo que cabe en curso ya esta lleno, o en Scrum no esta
        /// en el sprint), antes se pregunta: subir el limite y que empiece ya, o dejarla asignada esperando su turno.
        /// </summary>
        private void IntentarAsignar(FichaArrastrable f, TarjetaDeTrabajo c, MiembroDelEquipo mi) {
            if (S.PorQueNoEmpezariaAlAsignar(c.Id) != null && S.PorQueNoSePuedeAsignar(c.Id, mi.Id) == null) {
                _asignarTarjeta = c.Id;
                _asignarA = mi.Id;
                _fichaAbierta = null;
                return;
            }
            ActuarEnElMuro(f, () => S.AsignarTarjeta(c.Id, mi.Id), $"«{c.Titulo}» pasa a {mi.Nombre}: {ComoSeLeDa(mi, c.Tipo)}.", GameSession.MinutosDeAsignar);
        }

        /// <summary>El aviso: lo que pasa si se asigna sin mas, y lo que cuesta que empiece ya.</summary>
        private void ConfirmarAsignacion(PantallaDelMuro m) {
            var t = S.Tablero;
            var c = t.Tarjeta(_asignarTarjeta);
            var mi = t.Miembro(_asignarA);
            if (c == null || mi == null) { _asignarTarjeta = _asignarA = null; return; }
            var porQue = S.PorQueNoEmpezariaAlAsignar(c.Id);
            var limite = MotorDelTablero.EnCurso(t);
            var nuevo = S.LimiteEnCursoAlSubir();
            var fueraDelSprint = Politica != null && Politica.SprintCerrado && t.SprintActual >= 0 && c.Sprint != t.SprintActual;
            Action cerrar = () => { _asignarTarjeta = _asignarA = null; };

            var hoja = Dialogo(m, Tema.warning, 780, cerrar);
            var cabecera = Ui.Fila(hoja, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var signo = Ui.Texto(cabecera, "!", EstiloTexto.Hero, Tema.warning, TextAlignmentOptions.Center);
            signo.fontSize = 56;
            UiKit.Tamano(signo, 60, 70);
            signo.gameObject.AddComponent<Latido>().Fuerza = 0.06f;
            var titulos = Ui.Columna(cabecera, "Titulos", 4);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "ASIGNAR SUBIENDO EL LÍMITE EN CURSO", EstiloTexto.Leyenda, Tema.warning).characterSpacing = 3;
            Ui.Texto(titulos, $"¿Dar «{c.Titulo}» a {mi.Nombre.Split(' ')[0]} y que empiece ya?", EstiloTexto.Subtitulo, Tema.ink);

            Ui.Texto(hoja, porQue ?? "", EstiloTexto.Cuerpo, Tema.inkMuted);
            // Antes y despues del limite, en grande.
            var cambio = Ui.Fila(hoja, "Limite", Tema.espacio * 2, 12, TextAnchor.MiddleCenter);
            Ui.Fondo(cambio.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.bg950, 0.6f), NexusTheme.RadioMd);
            var antes = Ui.Columna(cambio, "Antes", 2, 0, TextAnchor.MiddleCenter);
            Ui.Texto(antes, "LÍMITE AHORA", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);
            PiezasDelMuro.Cifra(Ui, antes, limite, "0", "", Tema.ink, 46).alignment = TextAlignmentOptions.Center;
            PiezasDelMuro.Flecha(Ui, cambio, 90, 40, false, Tema.warning);
            var despues = Ui.Columna(cambio, "Despues", 2, 0, TextAnchor.MiddleCenter);
            Ui.Texto(despues, "QUEDARÍA EN", EstiloTexto.Leyenda, Tema.warning, TextAlignmentOptions.Center);
            PiezasDelMuro.Cifra(Ui, despues, nuevo, "0", "", Tema.warning, 46, 0.1f).alignment = TextAlignmentOptions.Center;

            var efectos = Ui.Columna(hoja, "Efectos", 6);
            Efecto(efectos, Tema.warning, "Más cosas a la vez: desde ahí el equipo rinde un poco menos.");
            if (fueraDelSprint) Efecto(efectos, Tema.danger, "Entra en el sprint en marcha: moral -2 y sobrecompromiso.");
            Efecto(efectos, Tema.cyan, $"Cuesta {GameSession.MinutosDeAsignar} min, como cualquier asignación.");

            var botones = RejillaDeBotones(hoja, 3, new Vector2(232, 84), "Botones");
            Ui.BotonDeOpcion(botones, $"Sí, subir a {nuevo}", "y que empiece ya", () => {
                cerrar();
                ActuarEnElMuro(null, () => S.AsignarSubiendoElLimite(c.Id, mi.Id),
                               $"Subiste el límite en curso a {nuevo}: «{c.Titulo}» empieza con {mi.Nombre}.", GameSession.MinutosDeAsignar);
            }).gameObject.AddComponent<Latido>().Fuerza = 0.015f;
            Ui.BotonDeOpcion(botones, "Solo asignarla", "esperará su turno", () => {
                cerrar();
                ActuarEnElMuro(null, () => S.AsignarTarjeta(c.Id, mi.Id), $"«{c.Titulo}» es de {mi.Nombre}: empezará cuando haya hueco.", GameSession.MinutosDeAsignar);
            });
            Ui.BotonDeOpcion(botones, "Cancelar", "no cambia nada", cerrar);
        }

        private void Efecto(RectTransform padre, Color color, string texto) {
            var fila = Ui.Fila(padre, "Efecto", 10, 0, TextAnchor.MiddleLeft);
            Ui.Punto(fila, color, 10);
            UiKit.Tamano(Ui.Texto(fila, texto, EstiloTexto.Pequeno, Tema.ink), flexAncho: 1);
        }

        /// <summary>Un dialogo encima del muro: velo que cierra al pulsar fuera, panel centrado que entra con un rebote.</summary>
        private RectTransform Dialogo(PantallaDelMuro m, Color color, float ancho, Action alCerrar) {
            var velo = Ui.Nodo(m.Cuerpo, "Velo");
            UiKit.Tamano(velo).ignoreLayout = true;
            UiKit.Rellenar(velo, -60);
            var imgVelo = velo.gameObject.AddComponent<Image>();
            imgVelo.color = NexusTheme.Alfa(Tema.bg950, 0.7f);
            var cerrar = velo.gameObject.AddComponent<Button>();
            cerrar.transition = Selectable.Transition.None;
            cerrar.onClick.AddListener(() => alCerrar());

            var hoja = Ui.Columna(velo, "Dialogo", Tema.espacio * 1.2f, 28, TextAnchor.UpperLeft);
            UiKit.Tamano(hoja).ignoreLayout = true;
            hoja.anchorMin = hoja.anchorMax = hoja.pivot = new Vector2(0.5f, 0.5f);
            hoja.sizeDelta = new Vector2(ancho, 0);
            hoja.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Ui.Fondo(hoja.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.surface, 0.98f), NexusTheme.RadioLg);
            Ui.Borde(hoja, color, NexusTheme.RadioLg, 2);
            Ui.Halo(hoja, NexusTheme.Alfa(color, 0.35f), NexusTheme.RadioLg);
            // Un clic dentro del dialogo no es un clic en el velo (que lo cierra).
            hoja.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            hoja.gameObject.AddComponent<EntradaDePieza>();
            return hoja;
        }

        /// <summary>Una rejilla de celdas iguales que mide lo que su contenido (para botones del mismo tamaño).</summary>
        private RectTransform RejillaDeBotones(RectTransform padre, int columnas, Vector2 celda, string nombre) {
            var rt = Ui.Nodo(padre, nombre);
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = celda;
            g.spacing = new Vector2(12, 12);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columnas;
            g.childAlignment = TextAnchor.UpperLeft;
            return rt;
        }


        // ==================================================================== la hoja de una ficha

        /// <summary>
        /// Un clic en una ficha: lo mismo que arrastrando, con botones (2026-11-07: mas grande; las acciones como
        /// tarjetas iguales en rejilla, cada una con lo que cuesta, y la gente con su cara y lo bien que se le da).
        /// </summary>
        private void HojaDeLaFicha(PantallaDelMuro m, TarjetaDeTrabajo c) {
            if (c == null) { _fichaAbierta = null; return; }
            var t = S.Tablero;
            var color = PiezasDelMuro.ColorDeTipo(Tema, c.Tipo, c.EsBug);
            var hoja = Dialogo(m, color, 980, () => _fichaAbierta = null);

            // ---- cabecera: icono, tipo, titulo y cerrar
            var cabecera = Ui.Fila(hoja, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            PiezasDelMuro.IconoDeTipo(Ui, cabecera, c.Tipo, c.EsBug, 64, color);
            var titulos = Ui.Columna(cabecera, "Titulos", 4);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, c.EsBug ? "ERROR" : PiezasDelMuro.NombreDeTipo(c.Tipo).ToUpperInvariant(), EstiloTexto.Leyenda, color).characterSpacing = 4;
            Ui.Texto(titulos, c.Titulo, EstiloTexto.Titulo, Tema.ink);
            Ui.Boton(cabecera, "Cerrar", () => _fichaAbierta = null, VarianteBoton.Fantasma);

            // ---- tres cifras como en los indicadores: puntos, avance, quien
            var cifras = Ui.Fila(hoja, "Cifras", Tema.espacio, 0, TextAnchor.UpperLeft);
            var pPuntos = PiezasDelMuro.Panel(Ui, cifras, "Puntos", color, 14);
            UiKit.Tamano(pPuntos, ancho: 0, flexAncho: 1);
            var filaPuntos = Ui.Fila(pPuntos, "Cifra", 10, 0, TextAnchor.MiddleLeft);
            PiezasDelMuro.Cifra(Ui, filaPuntos, c.Puntos, "0.#", "", color, 40, 0.1f);
            PiezasDelMuro.Puntos(Ui, filaPuntos, c.Puntos, color);
            var pAvance = PiezasDelMuro.Panel(Ui, cifras, "Hecho", Tema.success, 14);
            UiKit.Tamano(pAvance, ancho: 0, flexAncho: 1);
            var avance = c.Puntos > 0 ? c.Trabajo / c.Puntos : 0;
            PiezasDelMuro.Cifra(Ui, pAvance, avance * 100, "0", "%", Tema.success, 40, 0.15f);
            PiezasDelMuro.Barra(Ui, pAvance, (float)avance, Tema.success, 0.15f).gameObject.AddComponent<BrilloQueRecorre>();
            var pQuien = PiezasDelMuro.Panel(Ui, cifras, "La lleva", Tema.cyan, 14);
            UiKit.Tamano(pQuien, ancho: 0, flexAncho: 1);
            var lleva = t.Miembro(c.Asignado);
            Ui.Texto(pQuien, lleva == null ? "Nadie todavía" : lleva.Nombre, EstiloTexto.Encabezado, lleva == null ? Tema.inkFaint : Tema.ink);
            Ui.Texto(pQuien, lleva == null ? "Se la dará el reparto, o tú" : Capitalizar(ComoSeLeDa(lleva, c.Tipo)), EstiloTexto.Pequeno, Tema.inkMuted);

            // ---- acciones: tarjetas iguales, cada una con lo que cuesta
            var hayAcciones = false;
            var acciones = RejillaDeBotones(hoja, 4, new Vector2(218, 84), "Acciones");
            Action<string, string, string, Action> accion = (titulo, detalle, motivo, alPulsar) => {
                var b = Ui.BotonDeOpcion(acciones, titulo, motivo ?? detalle, alPulsar);
                b.interactable = motivo == null;
                hayAcciones = true;
            };
            if (!c.Empezada && !c.EsBug && !c.Terminada) {
                var minPriorizar = $"{GameSession.MinutosDePriorizar} min";
                accion("Ponerla la primera", minPriorizar, S.PorQueNoSePuedeMover(c.Id, 0),
                       () => ActuarEnElMuro(null, () => S.MoverTarjetaA(c.Id, 0), $"«{c.Titulo}» pasa a ser la primera.", GameSession.MinutosDePriorizar));
                accion("Subir un puesto", minPriorizar, S.PorQueNoSePuedePriorizar(c.Id, true),
                       () => ActuarEnElMuro(null, () => S.PriorizarTarjeta(c.Id, true), $"«{c.Titulo}» sube un puesto.", GameSession.MinutosDePriorizar));
                accion("Bajar un puesto", minPriorizar, S.PorQueNoSePuedePriorizar(c.Id, false),
                       () => ActuarEnElMuro(null, () => S.PriorizarTarjeta(c.Id, false), $"«{c.Titulo}» baja un puesto.", GameSession.MinutosDePriorizar));
            }
            if (Politica != null && Politica.SprintCerrado && t.SprintActual >= 0 && c.Sprint != t.SprintActual && !c.Terminada)
                accion("Meter en el sprint", $"{GameSession.MinutosDeMeterAlSprint} min, moral -2", S.PorQueNoSePuedeMeterAlSprint(c.Id),
                       () => ActuarEnElMuro(null, () => S.MeterAlSprint(c.Id), $"Metiste «{c.Titulo}» a mitad de sprint: moral -2 y sobrecompromiso.", GameSession.MinutosDeMeterAlSprint));
            if (!hayAcciones) acciones.gameObject.SetActive(false);

            // ---- darsela a alguien: la gente con su cara y lo bien que se le da
            if (t.Miembros.Count >= 2 && !c.Terminada) {
                var aviso = S.PorQueNoEmpezariaAlAsignar(c.Id);
                Ui.Texto(hoja, $"DÁRSELA A ALGUIEN ({GameSession.MinutosDeAsignar} MIN)", EstiloTexto.Leyenda, Tema.cyan).characterSpacing = 3;
                if (aviso != null) Efecto(hoja, Tema.warning, "Ahora no empezaría: al elegir a alguien te preguntaremos si subes el límite en curso.");
                var gente = RejillaDeBotones(hoja, 4, new Vector2(218, 92), "Gente");
                var fichas = new Dictionary<string, FichaDeMiembro>();
                foreach (var f in S.FichasDelEquipo()) fichas[f.Miembro.Id] = f;
                foreach (var mi in t.Miembros) {
                    var miembro = mi;
                    var h = mi.HabilidadPara(c.Tipo);
                    var colorH = h >= 1.2 ? Tema.success : h <= 0.85 ? Tema.warning : Tema.inkMuted;
                    var persona = Ui.Fila(gente, "Persona", 10, 10, TextAnchor.MiddleLeft);
                    var fondo = persona.gameObject.AddComponent<Image>();
                    var elegida = c.Asignado == mi.Id;
                    Ui.Fondo(fondo, elegida ? NexusTheme.Alfa(Tema.cyan, 0.18f) : NexusTheme.Alfa(Tema.bg950, 0.7f), NexusTheme.RadioMd);
                    Ui.Borde(persona, elegida ? Tema.cyan : NexusTheme.Alfa(colorH, 0.5f), NexusTheme.RadioMd, 1.5f);
                    FichaDeMiembro ficha;
                    fichas.TryGetValue(mi.Id, out ficha);
                    Rostro(persona, FotoDe(ficha, mi.Nombre), mi.Nombre, 52);
                    var datos = Ui.Columna(persona, "Datos", 2);
                    UiKit.Tamano(datos, flexAncho: 1);
                    Ui.Texto(datos, mi.Nombre.Split(' ')[0], EstiloTexto.Encabezado, Tema.ink).textWrappingMode = TextWrappingModes.NoWrap;
                    Ui.Texto(datos, elegida ? "la lleva ahora" : ComoSeLeDa(mi, c.Tipo), EstiloTexto.Pequeno, elegida ? Tema.cyan : colorH);
                    var motivo = S.PorQueNoSePuedeAsignar(c.Id, mi.Id);
                    var boton = persona.gameObject.AddComponent<Button>();
                    boton.transition = Selectable.Transition.None;
                    boton.interactable = motivo == null;
                    boton.onClick.AddListener(() => IntentarAsignar(null, c, miembro));
                    if (motivo == null) persona.gameObject.AddComponent<PiezaViva>().Escala = 1.04f;
                    else persona.gameObject.AddComponent<CanvasGroup>().alpha = 0.5f;
                }
            }
        }
    }
}
