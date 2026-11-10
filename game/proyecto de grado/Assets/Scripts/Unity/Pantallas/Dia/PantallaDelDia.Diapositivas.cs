using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Jornada;
using Nexus.Core.Tablero;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las diapositivas del carrusel del escritorio (Fase 2, 2026-11-06). Regla de todas: lo minimo escrito; el numero
    /// grande, el color y la forma dicen como va; el detalle aparece al pasar el raton (PiezaViva) o al pulsar.
    /// </summary>
    public sealed partial class PantallaDelDia {
        private void PintarDiapositiva(string id, RectTransform cuerpo) {
            switch (id) {
                case DiapositivasDelDia.Kpis: PintarKpis(cuerpo); break;
                case DiapositivasDelDia.Burndown:
                case DiapositivasDelDia.Flujo:
                case DiapositivasDelDia.CurvaS:
                case DiapositivasDelDia.Evolucion: PintarGraficaGrande(id, cuerpo); break;
                case DiapositivasDelDia.Documentos: PintarDocumentosVisuales(cuerpo); break;
                case DiapositivasDelDia.Tablero: PintarTableroGrande(cuerpo); break;
                case DiapositivasDelDia.Equipo: PintarEquipoEnCartas(cuerpo); break;
                case DiapositivasDelDia.Escritorio: PintarEscritorioHub(cuerpo); break;
                case DiapositivasDelDia.Cliente: PintarCliente(cuerpo); break;
            }
        }

        private string ClaveDeDiapositiva(string id) {
            var w = S.W;
            switch (id) {
                case DiapositivasDelDia.Kpis:
                    return $"{w.Avance:0}|{w.Alcance:0}|{w.DeudaTecnica:0}|{w.MoralEquipo:0}|{w.Cobertura:0}|{w.Cansancio:0}|{w.SatisfaccionCliente:0}|" +
                           $"{S.R.DiaActual}|{S.PorQueNoSePuedeProbar() == null}|{_avisoDeKpiHasta > Time.unscaledTime}";
                case DiapositivasDelDia.Tablero:
                    return S.Tablero == null ? "" : string.Join(",", S.Tablero.Tarjetas.Select(t => t.Id + t.Columna + t.Asignado + Math.Round(t.Restante, 1))) + "|" + S.R.LimiteWip;
                case DiapositivasDelDia.Equipo:
                    return string.Join(",", S.FichasDelEquipo().Select(f => f.Miembro.Id + Math.Round(f.Cansancio) + f.Ahora.Count + f.Miembro.HabloHoy)) + S.R.DiaActual;
                case DiapositivasDelDia.Escritorio:
                    return $"{S.TareasDeOficina.Count(t => S.PorQueNoSePuedeHacer(t.Id) == null)}|{S.CeremoniasDeHoy().Count(c => !c.Hecha)}|" +
                           $"{S.R.Suministros.Values.Sum()}|{S.R.PruebasHechasHoy}|{S.PrPendiente != null}|{Runner.PuedeMoverse}";
                case DiapositivasDelDia.Cliente:
                    return $"{S.PapelesDelCliente().Count}|{S.PapelesSinLeer()}|{w.SatisfaccionCliente:0}";
                case DiapositivasDelDia.Documentos:
                    return S.R.DiaActual + "|" + S.R.DocumentosInspeccionados + "|" + string.Join(",", S.R.Artefactos.Select(kv => kv.Key + Math.Round(kv.Value.Completitud)));
                default:
                    return $"{S.R.DiaActual}|{w.Avance:0}|{S.R.SerieAvance.Count}";
            }
        }

        // ==================================================================== indicadores

        /// <summary>Las tarjetas de los indicadores: numero grande, color de estado, mini-grafica; al pasar, su efecto.</summary>
        private void PintarKpis(RectTransform cuerpo) {
            var w = S.W;
            bool[] malos;
            var efectos = EfectosDeLosMedidores(out malos);
            var d = S.BriefDeHoy != null ? S.BriefDeHoy.Derivadas : null;
            var rejilla = Rejilla(cuerpo, 4, new Vector2(330, 196));
            GuiaView.Registrar("dia.proyecto", rejilla);

            TarjetaKpi(rejilla, "Avance", $"{w.Avance:0}<size=55%>/{w.Alcance:0}</size>", w.Alcance > 0 ? w.Avance / w.Alcance : 0, Tema.cyan,
                       S.R.SerieAvance, efectos[0], "avance");
            TarjetaKpi(rejilla, "Deuda técnica", $"{w.DeudaTecnica:0}", w.DeudaTecnica / 100, malos[1] ? Tema.danger : Tema.warning,
                       S.R.SerieDeuda, efectos[1], "deuda-tecnica");
            TarjetaKpi(rejilla, "Moral", $"{w.MoralEquipo:0}", w.MoralEquipo / 100, malos[2] ? Tema.danger : Tema.success,
                       S.R.SerieMoral, efectos[2], "moral");
            var cobertura = TarjetaKpi(rejilla, "Cobertura de pruebas", $"{w.Cobertura:0}<size=55%>%</size>", w.Cobertura / 100,
                                       malos[3] ? Tema.danger : Tema.cyan, S.R.SerieCobertura, efectos[3], "cobertura");
            // La ronda de pruebas: un «*» arriba a la derecha de su tarjeta. Al pulsarlo se hace, y la tarjeta lo dice.
            InsigniaDePruebas(cobertura);
            TarjetaKpi(rejilla, "Cansancio", $"{w.Cansancio:0}", w.Cansancio / 100, malos[4] ? Tema.danger : Tema.warning,
                       S.R.SerieCansancio, efectos[4], "cansancio");
            TarjetaDeRiesgo(rejilla, d == null ? (double?)null : d.RiesgoLatente);
            TarjetaKpi(rejilla, "Cliente", $"{w.SatisfaccionCliente:0}", w.SatisfaccionCliente / 100,
                       w.SatisfaccionCliente <= 35 ? Tema.danger : Tema.success, S.R.SerieSatisfaccion,
                       w.SatisfaccionCliente <= 35 ? "Tan baja que pedirán explicaciones." : "Sube al escuchar y cumplir; baja con sorpresas.", "satisfaccion");

            // El pronostico: un dial con el nivel del lanzamiento si se entregara hoy.
            if (S.R.Fase == 2) {
                var c = S.Pronostico();
                // Ordenado en dos columnas para que quepa en la tarjeta: a la izquierda el dial con su nivel; a la derecha
                // los tres factores que mas restan, como barras. Al pasar, una ficha con el peor y por que.
                // Una franja a lo ancho, debajo de las tarjetas (en una tarjeta pequeña no cabia): el dial con su nivel y los
                // cuatro factores lado a lado, cada uno con su barra y su valor. Al pasar el raton, el que mas resta y por que.
                rejilla.anchorMin = rejilla.anchorMax = new Vector2(0.5f, 0.6f);
                var franja = Ui.Fila(cuerpo, "Pronostico", Tema.margen, Tema.Espacio(4), TextAnchor.MiddleLeft);
                franja.anchorMin = new Vector2(0, 0);
                franja.anchorMax = new Vector2(1, 0);
                franja.pivot = new Vector2(0.5f, 0);
                franja.sizeDelta = new Vector2(-40, 150);
                franja.anchoredPosition = new Vector2(0, 4);
                var fondo = franja.gameObject.AddComponent<Image>();
                Ui.Fondo(fondo, Tema.surfaceRaised, NexusTheme.RadioLg);
                Ui.Borde(franja, ColorDelNivel(c.Nivel), NexusTheme.RadioLg, 1.5f);
                GuiaView.Registrar("dia.pronostico", franja);
                var izquierda = Ui.Columna(franja, "Nivel", 2, 0, TextAnchor.MiddleCenter);
                UiKit.Tamano(izquierda, ancho: 200);
                QuitarAltoMinimo(Ui.Texto(izquierda, "SI ENTREGARAS HOY", EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center));
                var dial = Ui.Dial(izquierda, (float)(c.Puntaje / 100.0), "", ColorDelNivel(c.Nivel), 80);
                dial.Formato = v => Mathf.RoundToInt(v * 100).ToString();
                QuitarAltoMinimo(Ui.Texto(izquierda, Nexus.Core.Evaluacion.NivelesDeLanzamiento.Titulo(c.Nivel), EstiloTexto.Encabezado, ColorDelNivel(c.Nivel), TextAlignmentOptions.Center));
                var factores = Ui.Fila(franja, "Factores", Tema.margen, alineacion: TextAnchor.MiddleLeft);
                UiKit.Tamano(factores, ancho: 0, flexAncho: 1);
                var ordenados = c.Factores.OrderBy(x => x.Maximo > 0 ? x.Puntos / x.Maximo : 1).ToList();
                foreach (var f in c.Factores.Take(4)) {
                    var parte = f.Maximo > 0 ? (float)(f.Puntos / f.Maximo) : 1f;
                    var color = parte >= 0.7f ? Tema.success : parte >= 0.4f ? Tema.warning : Tema.danger;
                    var col = Ui.Columna(factores, f.Nombre, 6, 0, TextAnchor.MiddleLeft);
                    UiKit.Tamano(col, ancho: 0, flexAncho: 1);
                    QuitarAltoMinimo(Ui.Texto(col, f.Nombre, EstiloTexto.Leyenda, Tema.inkMuted));
                    QuitarAltoMinimo(Ui.Texto(col, f.Valor, EstiloTexto.Encabezado, color));
                    UiKit.Tamano(Ui.Barra(col, parte, color), alto: 8);
                }
                var peor = ordenados.First();
                var viva = franja.gameObject.AddComponent<PiezaViva>();
                viva.Escala = 1.02f;
                var detalle = Ui.Columna(franja, "Detalle", 4, Tema.Espacio(3));
                var fondoDetalle = detalle.gameObject.AddComponent<Image>();
                Ui.Fondo(fondoDetalle, NexusTheme.Alfa(Tema.bg950, 0.85f), NexusTheme.RadioSm);
                UiKit.Tamano(detalle, ancho: 0, flexAncho: 1);
                Ui.Texto(detalle, "LO QUE MÁS RESTA · " + peor.Nombre.ToUpperInvariant(), EstiloTexto.Leyenda, Tema.warning);
                Ui.Texto(detalle, peor.Explicacion, EstiloTexto.Pequeno, Tema.ink);
                detalle.gameObject.SetActive(false);
                viva.Revelar = detalle.gameObject;
                viva.Ocultar = factores.gameObject;
            }
        }

        private string _avisoDeKpi;
        private float _avisoDeKpiHasta;

        /// <summary>El «*» de la ronda de pruebas, arriba a la derecha de la tarjeta de cobertura. Si ya se hizo, no esta.</summary>
        private void InsigniaDePruebas(RectTransform tarjeta) {
            if (_avisoDeKpiHasta > Time.unscaledTime && !string.IsNullOrEmpty(_avisoDeKpi)) {
                var aviso = Ui.Texto(tarjeta, _avisoDeKpi, EstiloTexto.Pequeno, Tema.success);
                aviso.fontStyle = FontStyles.Bold;
            }
            if (S.PorQueNoSePuedeProbar() != null) return;
            var efecto = S.EfectoDeProbar();
            var b = Ui.Nodo(tarjeta, "Probar");
            b.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            b.anchorMin = b.anchorMax = b.pivot = new Vector2(1, 1);
            b.anchoredPosition = new Vector2(-10, -10);
            b.sizeDelta = new Vector2(44, 44);
            var img = b.gameObject.AddComponent<Image>();
            img.sprite = Ui.SpriteCircular();
            img.color = Tema.cyan;
            var t = Ui.Texto(b, "*", EstiloTexto.Titulo, Tema.bg950, TextAlignmentOptions.Center);
            QuitarAltoMinimo(t);
            UiKit.Rellenar(t.rectTransform);
            // La pista, solo al pasar: que hace y cuanto cuesta.
            var pista = Ui.PanelColumna(b, "Pista", Tema.Espacio(3), 0, NexusTheme.Alfa(Tema.bg950, 0.95f));
            pista.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            pista.anchorMin = pista.anchorMax = new Vector2(1, 0);
            pista.pivot = new Vector2(1, 1);
            pista.anchoredPosition = new Vector2(0, -6);
            pista.sizeDelta = new Vector2(300, 64);
            Ui.Texto(pista, $"Ronda de pruebas · {Nexus.Core.Sesion.GameSession.MinutosDeProbar} min · {Textos.Previsualizar(efecto)}", EstiloTexto.Pequeno, Tema.ink);
            pista.gameObject.SetActive(false);
            var viva = b.gameObject.AddComponent<PiezaViva>();
            viva.Escala = 1.2f;
            viva.Revelar = pista.gameObject;
            viva.AlPulsar = () => {
                var antes = _ultimoResultado;
                Actuar(() => S.EjecutarPruebas(), Solo("El equipo hizo una ronda de pruebas: ", "Hiciste una ronda de pruebas: ") + Textos.Previsualizar(efecto) + ".");
                _avisoDeKpi = _ultimoResultado != antes && S.R.PruebasHechasHoy ? "Ronda hecha: " + Textos.Previsualizar(efecto) : _ultimoResultado;
                _avisoDeKpiHasta = Time.unscaledTime + 3.5f;
                _carrusel.Repintar();
            };
        }

        /// <summary>
        /// El riesgo como un semaforo de tres tramos (bajo, medio, alto) con su marca, y la palabra en grande. Antes del
        /// primer dia no hay medida: el semaforo vacio lo dice.
        /// </summary>
        private void TarjetaDeRiesgo(RectTransform padre, double? riesgo) {
            var t = Pieza(padre, "Riesgo");
            var nivel = riesgo == null ? "SIN MEDIR" : riesgo < 35 ? "BAJO" : riesgo < 60 ? "MEDIO" : "ALTO";
            var color = riesgo == null ? Tema.inkFaint : riesgo < 35 ? Tema.success : riesgo < 60 ? Tema.warning : Tema.danger;
            var palabra = Ui.Texto(t, nivel, EstiloTexto.Hero, color);
            palabra.fontSize = riesgo == null ? 30 : 44;
            var tramos = Ui.Fila(t, "Semaforo", 4);
            UiKit.Tamano(tramos, alto: 16);
            var colores = new[] { Tema.success, Tema.warning, Tema.danger };
            for (var i = 0; i < 3; i++) {
                var tramo = Ui.Nodo(tramos, "Tramo");
                var img = tramo.gameObject.AddComponent<Image>();
                var activo = riesgo != null && (i == 0 ? riesgo < 35 : i == 1 ? riesgo >= 35 && riesgo < 60 : riesgo >= 60);
                img.color = NexusTheme.Alfa(colores[i], activo ? 1f : 0.2f);
                img.raycastTarget = false;
                UiKit.Tamano(tramo, flexAncho: 1, alto: 16);
            }
            var mini = MiniGrafica(t, S.R.SerieRiesgo, color);
            Revelar(t, riesgo == null ? "Se mide al empezar cada día: junta cansancio, deuda y falta de pruebas."
                                      : $"{riesgo:0} de 100. Junta cansancio, deuda y falta de pruebas: el peligro de entregar.", mini);
            t.GetComponent<PiezaViva>().AlPulsar = () => {
                var e = Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, "riesgo");
                if (e != null) PantallaDePizarra.AbrirConcepto(App, e);
            };
        }

        private RectTransform TarjetaKpi(RectTransform padre, string nombre, string valor, double lleno, Color color,
                                         IList<double> serie, string efecto, string glosario) {
            var t = Pieza(padre, nombre);
            var numero = Ui.Texto(t, valor, EstiloTexto.Hero, color);
            numero.fontSize = 54;
            var barra = Ui.Barra(t, Mathf.Clamp01((float)lleno), color);
            var mini = MiniGrafica(t, serie, color);
            Revelar(t, efecto, mini);
            var pieza = t.GetComponent<PiezaViva>();
            pieza.AlPulsar = () => {
                var e = Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, glosario);
                if (e != null) PantallaDePizarra.AbrirConcepto(App, e);
            };
            return t;
        }

        /// <summary>Una tarjeta viva del panel: fondo, nombre arriba, y crece al pasar el raton.</summary>
        private RectTransform Pieza(RectTransform padre, string nombre) {
            var t = Ui.PanelColumna(padre, nombre, Tema.Espacio(4), Tema.Espacio(1), Tema.surfaceRaised);
            Ui.Borde(t, Tema.lineStrong, NexusTheme.RadioLg, 1);
            Ui.Texto(t, nombre.ToUpperInvariant(), EstiloTexto.Leyenda, Tema.inkMuted);
            t.gameObject.AddComponent<PiezaViva>();
            return t;
        }

        /// <summary>El texto que solo aparece al pasar el raton (en lugar de 'ocultar').</summary>
        private void Revelar(RectTransform pieza, string texto, GameObject ocultar) {
            // Una ficha con diseño (fondo, borde cyan a la izquierda), no un texto suelto: sale en el sitio de lo que oculta.
            var ficha = Ui.Fila(pieza, "Detalle", 0, alineacion: TextAnchor.UpperLeft);
            var fondo = ficha.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.85f), NexusTheme.RadioSm);
            fondo.raycastTarget = false;
            var franja = Ui.Nodo(ficha, "Franja");
            var img = franja.gameObject.AddComponent<Image>();
            img.color = Tema.cyan;
            img.raycastTarget = false;
            UiKit.Tamano(franja, ancho: 3);
            var t = Ui.Texto(ficha, texto, EstiloTexto.Pequeno, Tema.ink);
            t.margin = new Vector4(10, 6, 8, 6);
            UiKit.Tamano(t, flexAncho: 1);
            ficha.gameObject.SetActive(false);
            var viva = pieza.GetComponent<PiezaViva>();
            viva.Revelar = ficha.gameObject;
            viva.Ocultar = ocultar;
        }

        /// <summary>Las ultimas muestras de una serie como barritas (una por dia): la tendencia de un vistazo.</summary>
        private GameObject MiniGrafica(RectTransform padre, IList<double> serie, Color color) {
            var fila = Ui.Fila(padre, "Mini", 3, alineacion: TextAnchor.LowerLeft);
            UiKit.Tamano(fila, alto: 38);
            var muestras = serie == null ? new List<double>() : serie.Skip(Math.Max(0, serie.Count - 14)).ToList();
            var max = muestras.Count == 0 ? 1 : Math.Max(1, muestras.Max());
            for (var i = 0; i < muestras.Count; i++) {
                var b = Ui.Nodo(fila, "Barra");
                var img = b.gameObject.AddComponent<Image>();
                img.color = NexusTheme.Alfa(color, i == muestras.Count - 1 ? 1f : 0.45f);
                img.raycastTarget = false;
                UiKit.Tamano(b, 12, Mathf.Max(3, (float)(muestras[i] / max * 38)));
            }
            if (muestras.Count == 0) Ui.Texto(fila, "la tendencia sale al cerrar el día", EstiloTexto.Leyenda, Tema.inkFaint);
            return fila.gameObject;
        }

        /// <summary>Una rejilla centrada de celdas iguales.</summary>
        private RectTransform Rejilla(RectTransform cuerpo, int columnas, Vector2 celda) {
            var rt = Ui.Nodo(cuerpo, "Rejilla");
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = celda;
            g.spacing = new Vector2(Tema.espacio, Tema.espacio);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columnas;
            g.childAlignment = TextAnchor.MiddleCenter;
            var ajuste = rt.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.horizontalFit = ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        // ==================================================================== la grafica de la metodologia

        private void PintarGraficaGrande(string id, RectTransform cuerpo) {
            var col = Ui.Columna(cuerpo, "Grafica", Tema.espacio, 0, TextAnchor.UpperCenter);
            UiKit.Rellenar(col);
            var r = S.R;
            var ancho = 1380f;
            var alto = 430f;
            switch (id) {
                case DiapositivasDelDia.Burndown: {
                    if (r.BurndownDelSprint.Count == 0) { Vacio(col, "Todavía no hay sprint planificado."); return; }
                    var dias = S.DiasDeLaUnidadActual();
                    var comprometido = r.BurndownDelSprint[0];
                    var g = new GraficoDeSeries(Ui, col, ancho, alto);
                    g.Ejes("días del sprint", "puntos", 0, dias, Math.Max(1, comprometido));
                    g.Linea(GraficasDelProyecto.BurndownIdeal(comprometido, dias), Tema.inkFaint, true, 2);
                    g.Linea(r.BurndownDelSprint, Tema.cyan, false, 5);
                    var hechos = r.BurndownDelSprint.Count - 1;
                    var ideal = comprometido * (1 - Math.Min(1.0, hechos / (double)dias));
                    var real = r.BurndownDelSprint[r.BurndownDelSprint.Count - 1];
                    var cifras = Ui.Fila(col, "Cifras", Tema.espacio, alineacion: TextAnchor.MiddleCenter);
                    Ui.Cifra(cifras, $"{real:0.#}", "Quedan (pts)", null, real > ideal + 0.5 ? Tono.Aviso : Tono.Neutro);
                    Ui.Cifra(cifras, $"{ideal:0.#}", "Ideal hoy", null);
                    Ui.Cifra(cifras, $"Sprint {r.SprintActual + 1}", "En curso", null);
                    Leyenda(col, "Lo que queda", Tema.cyan, "Ritmo ideal", Tema.inkFaint);
                    break;
                }
                case DiapositivasDelDia.Flujo: {
                    if (r.SerieTerminado.Count < 2) { Vacio(col, "El flujo se dibuja a partir del segundo día."); return; }
                    var g = new GraficoDeSeries(Ui, col, ancho, alto);
                    g.Ejes("día", "puntos", 1, Math.Max(2, S.Perfil.DiasTotales), Math.Max(1, r.SerieAlcance.Max()) * 1.05);
                    g.AreasApiladas(new IList<double>[] { r.SerieTerminado, r.SerieEnCurso, r.SeriePorHacer }, new[] { Tema.success, Tema.warning, Tema.inkFaint });
                    g.MarcaVertical(r.DiaActual, "hoy", Tema.inkMuted);
                    var f = GraficasDelProyecto.Flujo(S.Tablero, r.DiaActual);
                    var cifras = Ui.Fila(col, "Cifras", Tema.espacio, alineacion: TextAnchor.MiddleCenter);
                    Ui.Cifra(cifras, f.TiempoDeCiclo.ToString("0.#") + " d", "Tiempo de ciclo", null);
                    Ui.Cifra(cifras, f.Rendimiento.ToString("0.##"), "Tarjetas / día", null);
                    Ui.Cifra(cifras, f.EnCurso + " / " + r.LimiteWip, "En curso / límite", null, f.EnCurso > r.LimiteWip ? Tono.Aviso : Tono.Neutro);
                    Leyenda(col, "Terminado", Tema.success, "En curso", Tema.warning, "Por hacer", Tema.inkFaint);
                    break;
                }
                case DiapositivasDelDia.CurvaS: {
                    var dias = Math.Max(2, S.Perfil.DiasTotales);
                    var g = new GraficoDeSeries(Ui, col, ancho, alto);
                    g.Ejes("día", "puntos", 1, dias + 1, Math.Max(1, S.W.Alcance) * 1.1);
                    g.Linea(GraficasDelProyecto.CurvaSPlanificada(S.W.Alcance, dias), Tema.inkFaint, true, 2);
                    if (r.SerieAvance.Count > 0) g.Linea(r.SerieAvance, Tema.cyan, false, 5);
                    g.MarcaVertical(Math.Max(1, r.DiaActual), "hoy", Tema.inkMuted);
                    var desvio = GraficasDelProyecto.DesvioRespectoAlPlan(S.W.Avance, S.W.Alcance, Math.Max(1, r.DiaActual), dias);
                    var cifras = Ui.Fila(col, "Cifras", Tema.espacio, alineacion: TextAnchor.MiddleCenter);
                    Ui.Cifra(cifras, desvio > 0.5 ? $"−{desvio:0.#}" : desvio < -0.5 ? $"+{-desvio:0.#}" : "0", "Respecto al plan (pts)", null,
                             desvio > 0.5 ? Tono.Aviso : Tono.Exito);
                    Ui.Cifra(cifras, $"{S.W.Avance:0}/{S.W.Alcance:0}", "Hecho", null);
                    Leyenda(col, "Avance real", Tema.cyan, "El plan", Tema.inkFaint);
                    break;
                }
                default: {
                    if (r.SerieAvance.Count < 2) { Vacio(col, "La evolución se dibuja a partir del segundo día."); return; }
                    var g = new GraficoDeSeries(Ui, col, ancho, alto);
                    g.Ejes("día", "puntos", 1, Math.Max(2, S.Perfil.DiasTotales), Math.Max(1, r.SerieAlcance.Max()) * 1.1);
                    g.Linea(r.SerieAlcance, Tema.inkFaint, true, 2);
                    g.Linea(r.SerieAvance, Tema.cyan, false, 5);
                    Leyenda(col, "Avance", Tema.cyan, "Alcance", Tema.inkFaint);
                    break;
                }
            }
        }

        private void Vacio(RectTransform padre, string texto) {
            var t = Ui.Texto(padre, texto, EstiloTexto.Subtitulo, Tema.inkMuted, TextAlignmentOptions.Center);
            UiKit.Tamano(t, flexAlto: 1);
        }

        private void Leyenda(RectTransform padre, params object[] pares) {
            var series = new List<KeyValuePair<string, Color>>();
            for (var i = 0; i + 1 < pares.Length; i += 2) series.Add(new KeyValuePair<string, Color>((string)pares[i], (Color)pares[i + 1]));
            GraficoDeSeries.Leyenda(Ui, padre, series);
        }

        // ==================================================================== documentos (Cascada)

        private void PintarDocumentosVisuales(RectTransform cuerpo) {
            var hoy = S.DocumentosDeHoy();
            var rejilla = Rejilla(cuerpo, 5, new Vector2(270, 300));
            foreach (var etapa in S.Metodologia.Calendario.Etapas) {
                if (etapa.Documentos == null) continue;
                foreach (var doc in etapa.Documentos) {
                    var d = doc;
                    Nexus.Core.Modelo.EstadoArtefacto estado;
                    S.R.Artefactos.TryGetValue(d, out estado);
                    var completitud = estado == null ? 0 : estado.Completitud;
                    var calidad = estado == null ? 0 : estado.Calidad;
                    var t = Pieza(rejilla, d);
                    Ui.Dial(t, (float)(completitud / 100.0), "completo", completitud >= 99 ? Tema.success : Tema.cyan, 120);
                    Ui.Texto(t, etapa.Nombre, EstiloTexto.Pequeno, hoy.Contains(d) ? Tema.cyan : Tema.inkMuted, TextAlignmentOptions.Center);
                    var barra = Ui.Barra(t, (float)(calidad / 100.0), calidad >= 60 ? Tema.success : Tema.warning);
                    if (hoy.Contains(d)) {
                        var motivo = S.PorQueNoSePuedeInspeccionar(d);
                        Revelar(t, motivo ?? $"Inspeccionarlo hoy ({Nexus.Core.Sesion.GameSession.MinutosDeInspeccionar} min): clic.", barra.gameObject);
                        if (motivo == null) t.GetComponent<PiezaViva>().AlPulsar = () => AbrirInspeccion(d);
                        Ui.Halo(t, NexusTheme.Alfa(Tema.cyan, 0.5f), NexusTheme.RadioLg);
                    }
                }
            }
        }

        // ==================================================================== tablero

        /// <summary>El tablero en grande: tres columnas de post-its con quien la lleva y cuanto falta.</summary>
        private void PintarTableroGrande(RectTransform cuerpo) {
            var t = S.Tablero;
            if (t == null) { Vacio(cuerpo, "Todavía no hay tablero."); return; }
            var col = Ui.Columna(cuerpo, "Tablero", Tema.espacio);
            UiKit.Rellenar(col);
            GuiaView.Registrar("dia.tablero", col);

            var cabecera = Ui.Fila(col, "Cabecera", Tema.espacio);
            if (Politica != null && Politica.LimiteDeWip) {
                // Kanban: el limite de trabajo en curso, a mano.
                Ui.Texto(cabecera, $"En curso: límite {S.R.LimiteWip}", EstiloTexto.Encabezado, Tema.ink);
                Ui.Boton(cabecera, "−", () => Actuar(() => S.AjustarLimiteWip(-1), "Bajaste el límite de trabajo en curso."), VarianteBoton.Secundario);
                Ui.Boton(cabecera, "+", () => Actuar(() => S.AjustarLimiteWip(+1), "Subiste el límite de trabajo en curso."), VarianteBoton.Secundario);
            } else if (t.SprintActual >= 0 && Politica != null && Politica.SprintCerrado) {
                Ui.Texto(cabecera, $"Sprint {t.SprintActual + 1}", EstiloTexto.Encabezado, Tema.ink);
            }
            Ui.Resorte(cabecera);
            if (S.PrPendiente != null) Ui.Boton(cabecera, "Revisar código", AbrirRevision, VarianteBoton.Primario);
            Ui.Boton(cabecera, "Detalle", () => AbrirTablero(0), VarianteBoton.Fantasma);

            var columnas = Ui.Fila(col, "Columnas", Tema.espacio, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(columnas, flexAncho: 1, flexAlto: 1);
            var grupos = new[] {
                new KeyValuePair<string, string[]>(ColumnasDeBase.PorHacer, new[] { ColumnasDeBase.PorHacer }),
                new KeyValuePair<string, string[]>(ColumnasDeBase.Haciendo, new[] { ColumnasDeBase.Haciendo, ColumnasDeBase.Revision }),
                new KeyValuePair<string, string[]>(ColumnasDeBase.Hecho, new[] { ColumnasDeBase.Hecho })
            };
            var colores = new[] { Tema.inkMuted, Tema.warning, Tema.success };
            for (var i = 0; i < grupos.Length; i++) {
                var g = grupos[i];
                var tarjetas = t.Tarjetas.Where(x => g.Value.Contains(x.Columna)).OrderBy(x => x.Orden).ToList();
                var panel = Ui.PanelColumna(columnas, "Columna", Tema.Espacio(3), Tema.Espacio(2), NexusTheme.Alfa(Tema.bg900, 0.9f));
                UiKit.Tamano(panel, ancho: 0, flexAncho: 1, flexAlto: 1);
                var linea = Ui.Nodo(panel, "Linea");
                linea.gameObject.AddComponent<Image>().color = colores[i];
                UiKit.Tamano(linea, alto: 3);
                var porDefecto = g.Key == ColumnasDeBase.PorHacer ? "Por hacer" : g.Key == ColumnasDeBase.Hecho ? "Hecho" : "Haciendo";
                var nombre = Politica != null ? Politica.NombreDe(g.Key, porDefecto) : porDefecto;
                Ui.Texto(panel, $"{nombre.ToUpperInvariant()} · {tarjetas.Count}", EstiloTexto.Leyenda, colores[i]);
                RectTransform lista;
                var scroll = Ui.Desplazable(panel, out lista);
                UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
                // Lo que hay que rehacer (un despliegue que fallo, un evento que quito avance) va primero: el avance
                // paga esto antes que cualquier tarjeta, y sin verlo parecia que el equipo no hacia nada.
                var debe = t.TrabajoAdeudado;
                if (g.Key == ColumnasDeBase.Haciendo && debe > 0.05) TarjetaDeRetrabajo(lista, debe);
                if (g.Key == ColumnasDeBase.Hecho) {
                    // Lo hecho, compacto y lo ultimo primero: con 17 tarjetas la columna era una lista sin fin.
                    const int maxHechas = 8;
                    foreach (var tarjeta in tarjetas.OrderByDescending(x => x.DiaFin).ThenByDescending(x => x.Orden).Take(maxHechas))
                        TarjetaHecha(lista, tarjeta);
                    if (tarjetas.Count > maxHechas)
                        Ui.Texto(lista, $"y {tarjetas.Count - maxHechas} más", EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Center);
                    continue;
                }
                foreach (var tarjeta in tarjetas.Take(14)) {
                    var espera = debe > 0.05 && !tarjeta.Empezada && !string.IsNullOrEmpty(tarjeta.Asignado) && tarjeta.AsignacionManual
                        ? "Asignada: empieza al terminar de rehacer" : null;
                    PostItDeTarjeta(lista, tarjeta, colores[i], espera);
                }
            }
        }

        /// <summary>
        /// Una tarjeta del tablero, sobria y legible (2026-11-06: el amarillo tapaba el texto): fondo oscuro, una franja de
        /// color a la izquierda (cyan trabajo, rojo error, verde hecho), el titulo claro y una linea con quien, cuanto y puntos.
        /// </summary>
        /// <summary>El trabajo que hay que rehacer, como una tarjeta en curso: naranja, con lo que falta.</summary>
        private void TarjetaDeRetrabajo(RectTransform padre, double debe) {
            var p = Ui.Fila(padre, "Retrabajo", 0, alineacion: TextAnchor.UpperLeft);
            Ui.Fondo(p.gameObject.AddComponent<Image>(), Tema.warningSoft, NexusTheme.RadioMd);
            Ui.Borde(p, NexusTheme.Alfa(Tema.warning, 0.7f), NexusTheme.RadioMd, 1.5f);
            var franja = Ui.Nodo(p, "Franja");
            var img = franja.gameObject.AddComponent<Image>();
            img.color = Tema.warning;
            img.raycastTarget = false;
            UiKit.Tamano(franja, ancho: 6);
            var contenido = Ui.Columna(p, "Contenido", 6, Tema.Espacio(3));
            UiKit.Tamano(contenido, flexAncho: 1);
            Ui.Texto(contenido, "Rehacer lo perdido", EstiloTexto.Cuerpo, Tema.ink);
            Ui.Texto(contenido, "Algo obligó a rehacer trabajo ya hecho. El avance del equipo va aquí antes que a lo nuevo.",
                     EstiloTexto.Pequeno, Tema.inkMuted);
            var pie = Ui.Fila(contenido, "Pie", 8);
            UiKit.Tamano(Ui.Nodo(pie, "Hueco"), flexAncho: 1);
            Ui.Chip(pie, $"faltan {debe:0.#} pts", Tema.bg950, Tema.warning);
        }

        /// <summary>Una tarjeta terminada, en una linea: punto verde, titulo y puntos.</summary>
        private void TarjetaHecha(RectTransform padre, TarjetaDeTrabajo t) {
            var p = Ui.Fila(padre, "Hecha", 8, Tema.Espacio(2), TextAnchor.MiddleLeft);
            Ui.Fondo(p.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.surfaceRaised, 0.6f), NexusTheme.RadioMd);
            Ui.Punto(p, t.EsBug ? Tema.danger : Tema.success, 10);
            var titulo = Ui.Texto(p, t.Titulo, EstiloTexto.Pequeno, Tema.inkMuted);
            titulo.maxVisibleLines = 1;
            titulo.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Tamano(titulo, flexAncho: 1);
            Ui.Texto(p, $"{t.Puntos:0.#} pts", EstiloTexto.Leyenda, Tema.inkFaint);
            var viva = p.gameObject.AddComponent<PiezaViva>();
            viva.Escala = 1.02f;
            viva.AlPulsar = () => AbrirTablero(0);
        }

        private void PostItDeTarjeta(RectTransform padre, TarjetaDeTrabajo t, Color color, string nota = null) {
            var franja = t.Terminada ? Tema.success : t.EsBug ? Tema.danger : Tema.cyan;
            var p = Ui.Fila(padre, "Tarjeta", 0, alineacion: TextAnchor.UpperLeft);
            var fondo = p.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, Tema.surfaceRaised, NexusTheme.RadioMd);
            var borde = Ui.Nodo(p, "Franja");
            var imgBorde = borde.gameObject.AddComponent<Image>();
            imgBorde.color = franja;
            imgBorde.raycastTarget = false;
            UiKit.Tamano(borde, ancho: 6);
            var contenido = Ui.Columna(p, "Contenido", 6, Tema.Espacio(3));
            UiKit.Tamano(contenido, flexAncho: 1);
            var titulo = Ui.Texto(contenido, t.Titulo, EstiloTexto.Cuerpo, Tema.ink);
            titulo.maxVisibleLines = 2;
            titulo.overflowMode = TextOverflowModes.Ellipsis;
            var fila = Ui.Fila(contenido, "Pie", 8);
            var miembro = string.IsNullOrEmpty(t.Asignado) || S.Tablero == null ? null : S.Tablero.Miembro(t.Asignado);
            Ui.Retrato(fila, miembro == null ? "?" : miembro.Nombre.Substring(0, 1), miembro == null ? Tono.Neutro : Tono.Cyan, 26);
            // Si quien la lleva trabaja hoy en pareja, la inicial de su pareja al lado.
            var pareja = miembro == null || string.IsNullOrEmpty(miembro.ParejaHoy) ? null : S.Tablero.Miembro(miembro.ParejaHoy);
            if (pareja != null) Ui.Retrato(fila, pareja.Nombre.Substring(0, 1), Tono.Violeta, 26);
            var hecho = t.Puntos > 0 ? Mathf.Clamp01((float)(1 - t.Restante / t.Puntos)) : (t.Terminada ? 1f : 0f);
            UiKit.Tamano(Ui.Barra(fila, hecho, franja), flexAncho: 1);
            Ui.Chip(fila, $"{t.Puntos:0.#} pts", Tema.bg950, Tema.inkMuted);
            if (!string.IsNullOrEmpty(nota)) Ui.Texto(contenido, nota, EstiloTexto.Leyenda, Tema.warning);
            var viva = p.gameObject.AddComponent<PiezaViva>();
            viva.Escala = 1.03f;
            viva.AlPulsar = () => AbrirTablero(0);
        }

        // ==================================================================== equipo

        /// <summary>El equipo como cartas: retrato, nombre, cansancio; «Descripción» le da la vuelta, «Interactuar» abre lo que puedes hacer con el.</summary>
        private void PintarEquipoEnCartas(RectTransform cuerpo) {
            var fichas = S.FichasDelEquipo();
            var fila = Ui.Fila(cuerpo, "Equipo", Tema.espacio * 1.5f, alineacion: TextAnchor.MiddleCenter);
            UiKit.Rellenar(fila);
            GuiaView.Registrar("dia.equipo", fila);
            if (S.TrabajasSolo || fichas.Count == 0) {
                var t = CartaDePersona(fila, "Tú", "Trabajas solo", "Sin equipo: cada acción la haces tú.", null);
                AccionDeCarta(t, "Qué puedo hacer", AbrirEquipo);
                return;
            }
            foreach (var f in fichas.Take(5)) {
                var ficha = f;
                var m = f.Miembro;
                var t = CartaDePersona(fila, m.Nombre, m.Rol, m.Descripcion, ficha);
                AccionDeCarta(t, "Interactuar", () => AbrirInteraccion(ficha));
            }
        }

        private RectTransform CartaDePersona(RectTransform padre, string nombre, string rol, string dorso, Nexus.Core.Sesion.FichaDeMiembro ficha) {
            var t = Ui.PanelColumna(padre, "Persona " + nombre, Tema.Espacio(4), Tema.Espacio(2), Tema.surfaceRaised);
            UiKit.Tamano(t, ancho: 300, alto: 600);
            Ui.Borde(t, Tema.lineStrong, NexusTheme.RadioLg, 1);
            var frente = Ui.Columna(t, "Frente", Tema.Espacio(2), 0, TextAnchor.UpperCenter);
            Rostro(frente, FotoDe(ficha, nombre), nombre, 150);
            Ui.Texto(frente, nombre, EstiloTexto.Subtitulo, Tema.ink, TextAlignmentOptions.Center);
            Ui.Texto(frente, rol, EstiloTexto.Pequeno, Tema.cyan, TextAlignmentOptions.Center);
            if (ficha != null) EstadoDeHoy(frente, ficha);
            if (ficha != null) BarrasDeAtributos(frente, ficha);
            // El reverso va en un visor que ocupa TODO el hueco entre el borde de arriba y los botones, y su ficha se encoge
            // (EscaladorAlCaber) lo justo para caber: antes, con una descripcion larga, empujaba «Frente» e «Interactuar»
            // fuera de la carta (2026-11-09).
            var visor = Ui.Nodo(t, "Visor del reverso");
            UiKit.Tamano(visor, flexAncho: 1, flexAlto: 100);
            var reverso = Ui.Columna(visor, "Reverso", Tema.Espacio(2));
            ReversoDePersona(reverso, nombre, rol, dorso, ficha);
            var escalador = visor.gameObject.AddComponent<EscaladorAlCaber>();
            escalador.Interior = reverso;
            visor.gameObject.SetActive(false);
            UiKit.Tamano(Ui.Nodo(t, "Hueco"), flexAlto: 1);
            // Las acciones, una debajo de otra y a todo el ancho: en fila, «Interactuar» se salia de la carta.
            var acciones = Ui.Columna(t, "Acciones", Tema.Espacio(2));
            // «Descripción» le da la vuelta a la carta (un giro en X), y otra vez la deja como estaba.
            var girar = Ui.Boton(acciones, "Descripción", null, VarianteBoton.Fantasma);
            UiKit.Tamano(girar, flexAncho: 1);
            var giro = t.gameObject.AddComponent<GiroDeCarta>();
            giro.Frente = frente.gameObject;
            giro.Reverso = visor.gameObject;
            girar.onClick.AddListener(giro.Girar);
            // La etiqueta dice a que cara se va: «Descripción» desde el frente, «Frente» desde el reverso.
            var etiqueta = girar.GetComponentInChildren<TMP_Text>();
            if (etiqueta != null) girar.onClick.AddListener(() => etiqueta.text = frente.gameObject.activeSelf ? "Frente" : "Descripción");
            t.gameObject.AddComponent<PiezaViva>().Escala = 1.04f;
            return t;
        }

        /// <summary>
        /// Lo que pasa hoy con esta persona, a la vista (2026-11-06: «parece que estoy adivinando»): en pareja (dos
        /// circulos enlazados y con quien), con un respiro, o ya hablaste con ella.
        /// </summary>
        private void EstadoDeHoy(RectTransform padre, Nexus.Core.Sesion.FichaDeMiembro ficha) {
            var m = ficha.Miembro;
            var pareja = string.IsNullOrEmpty(m.ParejaHoy) || S.Tablero == null ? null : S.Tablero.Miembro(m.ParejaHoy);
            if (pareja == null && !m.DescansaHoy && !m.HabloHoy) return;
            var fila = Ui.Fila(padre, "Hoy", 6, 0, TextAnchor.MiddleCenter);
            if (pareja != null) {
                IconoDePareja(fila, m.Nombre, pareja.Nombre);
                Ui.Chip(fila, "En pareja con " + pareja.Nombre.Split(' ')[0], Tema.violetSoft, Tema.violet);
            }
            if (m.DescansaHoy) Ui.Chip(fila, "Hoy descansa", Tema.successSoft, Tema.success);
            if (m.HabloHoy && pareja == null) Ui.Chip(fila, "Ya hablaste hoy", Tema.cyanSoft, Tema.cyan);
        }

        /// <summary>Dos circulos solapados con las iniciales: el icono de «trabajan en pareja».</summary>
        private void IconoDePareja(RectTransform padre, string uno, string otro) {
            var icono = Ui.Nodo(padre, "Pareja");
            UiKit.Tamano(icono, 46, 28);
            for (var i = 0; i < 2; i++) {
                var nombre = i == 0 ? uno : otro;
                var c = Ui.Nodo(icono, "Inicial");
                c.anchorMin = c.anchorMax = new Vector2(0, 0.5f);
                c.pivot = new Vector2(0, 0.5f);
                c.sizeDelta = new Vector2(28, 28);
                c.anchoredPosition = new Vector2(i * 18, 0);
                var img = c.gameObject.AddComponent<Image>();
                img.sprite = Ui.SpriteCircular();
                img.color = i == 0 ? Tema.cyan : Tema.violet;
                img.raycastTarget = false;
                var t = Ui.Texto(c, string.IsNullOrEmpty(nombre) ? "?" : nombre.Substring(0, 1), EstiloTexto.Leyenda, Tema.bg950, TextAlignmentOptions.Center);
                UiKit.Rellenar(t.rectTransform);
            }
        }

        /// <summary>
        /// El reverso de la carta de una persona, como una ficha: cabecera con su rol, la descripcion como cita con una
        /// raya de color, en que es fuerte y en que flojea (pastillas verde y roja con su nivel) y lo que lleva entre manos.
        /// </summary>
        private void ReversoDePersona(RectTransform reverso, string nombre, string rol, string descripcion, Nexus.Core.Sesion.FichaDeMiembro ficha) {
            var cab = Ui.Columna(reverso, "Cabecera", 2);
            Ui.Texto(cab, "PERFIL", EstiloTexto.Leyenda, Tema.cyan);
            Ui.Texto(cab, nombre, EstiloTexto.Subtitulo, Tema.ink);
            Ui.Texto(cab, rol, EstiloTexto.Pequeno, Tema.inkMuted);
            Ui.Separador(reverso);

            // La descripcion, como una cita con una raya de color a la izquierda.
            var cita = Ui.Fila(reverso, "Cita", Tema.Espacio(2), 0, TextAnchor.UpperLeft);
            var raya = Ui.Nodo(cita, "Raya");
            var imgRaya = raya.gameObject.AddComponent<Image>();
            imgRaya.color = Tema.cyan;
            imgRaya.raycastTarget = false;
            UiKit.Tamano(raya, ancho: 3, flexAlto: 1);
            var texto = Ui.Texto(cita, string.IsNullOrEmpty(descripcion) ? "Sin descripción." : descripcion, EstiloTexto.Pequeno, Tema.ink);
            texto.fontStyle = FontStyles.Italic;
            UiKit.Tamano(texto, flexAncho: 1);
            if (ficha == null) return;

            Rasgo(reverso, "FUERTE EN", ficha.Fuerte, ficha, Tema.success, Tema.successSoft);
            Rasgo(reverso, "FLOJO EN", ficha.Flojo, ficha, Tema.danger, Tema.dangerSoft);

            // Lo que lleva entre manos, en dos cifras.
            var cifras = Ui.Fila(reverso, "Cifras", Tema.Espacio(2), 0, TextAnchor.MiddleCenter);
            Cifra(cifras, ficha.Ahora.Count.ToString(), ficha.Ahora.Count == 1 ? "tarjeta ahora" : "tarjetas ahora", Tema.cyan);
            Cifra(cifras, ficha.Terminadas.Count.ToString(), ficha.Terminadas.Count == 1 ? "terminada" : "terminadas", Tema.success);
        }

        /// <summary>Un rasgo: su titulo pequeño y una pastilla con el tipo de trabajo y su nivel (0,5-1,5 a barra).</summary>
        private void Rasgo(RectTransform padre, string titulo, string tipo, Nexus.Core.Sesion.FichaDeMiembro ficha, Color color, Color fondo) {
            var bloque = Ui.Columna(padre, titulo, 4);
            Ui.Texto(bloque, titulo, EstiloTexto.Leyenda, Tema.inkMuted);
            var pastilla = Ui.PanelColumna(bloque, "Pastilla", Tema.Espacio(2), 4, fondo);
            Ui.Borde(pastilla, NexusTheme.Alfa(color, 0.6f), NexusTheme.RadioMd, 1);
            Ui.Texto(pastilla, string.IsNullOrEmpty(tipo) ? "Le da igual" : NombreCorto(tipo), EstiloTexto.Cuerpo, color);
            double nivel;
            if (!string.IsNullOrEmpty(tipo) && ficha.Miembro.Habilidades != null && ficha.Miembro.Habilidades.TryGetValue(tipo, out nivel))
                Ui.Barra(pastilla, Mathf.Clamp01((float)(nivel - 0.5)), color);
        }

        private void Cifra(RectTransform padre, string numero, string que, Color color) {
            var c = Ui.PanelColumna(padre, "Cifra " + que, Tema.Espacio(2), 0, Tema.bg900);
            UiKit.Tamano(c, flexAncho: 1);
            Ui.Texto(c, numero, EstiloTexto.Subtitulo, color, TextAlignmentOptions.Center);   // mas bajo: el reverso tiene que caber
            Ui.Texto(c, que, EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);
        }

        /// <summary>La foto de la persona: su personaje si lo tiene, si no por su nombre; «Tú» es la protagonista.</summary>
        private static string FotoDe(Nexus.Core.Sesion.FichaDeMiembro ficha, string nombre) {
            if (ficha == null) return "personajes/protagonista";
            return RostroDePersonaje.FotoDe(ficha.Miembro);   // el mismo de la Fase 1 (2026-11-08)
        }

        /// <summary>
        /// El ROSTRO de una persona: un circulo con la parte de arriba de su retrato (los retratos son de medio cuerpo, con
        /// la cara arriba al centro), con borde. Sin retrato, sus iniciales.
        /// </summary>
        private void Rostro(RectTransform padre, string id, string nombre, float lado) {
            // Dentro de una fila centrada: una columna estira a sus hijos a lo ancho, y el circulo salia ovalado.
            var fila = Ui.Fila(padre, "Rostro", 0, alineacion: TextAnchor.MiddleCenter);
            RostroDePersonaje.Crear(Ui, fila, id, nombre, lado);   // compartido con la Fase 1 (2026-11-08)
        }

        /// <summary>Las estadisticas como lineas de atributo (ficha de personaje): cada habilidad, y el cansancio.</summary>
        private void BarrasDeAtributos(RectTransform padre, Nexus.Core.Sesion.FichaDeMiembro ficha) {
            var lineas = Ui.Columna(padre, "Atributos", 4);
            foreach (var kv in ficha.Miembro.Habilidades.OrderBy(x => x.Key).Take(4)) {
                // Las habilidades van de ~0,5 a ~1,5 (1 = normal).
                var v = Mathf.Clamp01((float)((kv.Value - 0.5) / 1.0));
                Linea(lineas, NombreCorto(kv.Key), v, v >= 0.6f ? Tema.success : v >= 0.4f ? Tema.cyan : Tema.warning);
            }
            Linea(lineas, "Cansancio", (float)(ficha.Cansancio / 100), ficha.Cansancio >= 60 ? Tema.danger : Tema.warning);
        }

        private void Linea(RectTransform padre, string nombre, float valor, Color color) {
            var fila = Ui.Fila(padre, nombre, 6);
            UiKit.Tamano(Ui.Texto(fila, nombre, EstiloTexto.Leyenda, Tema.inkMuted), ancho: 90);
            UiKit.Tamano(Ui.Barra(fila, valor, color), flexAncho: 1);
        }

        private static string NombreCorto(string tipo) {
            if (string.IsNullOrEmpty(tipo)) return "";
            switch (tipo.ToLowerInvariant()) {
                case "logica": return "Lógica";
                case "interfaz": return "Interfaz";
                case "datos": return "Datos";
                case "pruebas": return "Pruebas";
            }
            var t = tipo.Replace('-', ' ').Replace('_', ' ');
            return char.ToUpperInvariant(t[0]) + t.Substring(1);
        }

        private void AccionDeCarta(RectTransform carta, string texto, Action alPulsar) {
            var acciones = carta.Find("Acciones");
            UiKit.Tamano(Ui.Boton(acciones, texto, alPulsar, VarianteBoton.Primario), flexAncho: 1);
        }

        /// <summary>Gira una carta sobre su eje vertical y cambia de cara a mitad de giro.</summary>
        private sealed class GiroDeCarta : MonoBehaviour {
            public GameObject Frente, Reverso;
            private float _t = 1f;
            private bool _cambiada;
            public void Girar() { _t = 0; _cambiada = false; }
            private void Update() {
                if (_t >= 1) return;
                _t = Mathf.Min(1, _t + Time.unscaledDeltaTime / 0.3f);
                var s = transform.localScale;
                transform.localScale = new Vector3(Mathf.Max(0.02f, Mathf.Abs(Mathf.Cos(_t * Mathf.PI))) * Mathf.Abs(s.y), s.y, 1);
                if (!_cambiada && _t >= 0.5f) {
                    _cambiada = true;
                    var frente = Frente.activeSelf;
                    Frente.SetActive(!frente);
                    Reverso.SetActive(frente);
                }
                if (_t >= 1) transform.localScale = new Vector3(s.y, s.y, 1);
            }
        }

        // ==================================================================== escritorio

        /// <summary>
        /// El escritorio como un tablero de mosaicos grandes: practicar, el plano, pruebas, suministros, ceremonias.
        /// Tres arriba y dos abajo, cada fila centrada (una rejilla dejaria los de abajo pegados a la izquierda).
        /// «Las partes» ya no es un mosaico (2026-11-07): el desglose por modulos sigue en el panel de Monitoreo.
        /// </summary>
        private void PintarEscritorioHub(RectTransform cuerpo) {
            var bloque = Ui.Columna(cuerpo, "Mosaicos", Tema.espacio, 0, TextAnchor.MiddleCenter);
            bloque.anchorMin = bloque.anchorMax = bloque.pivot = new Vector2(0.5f, 0.5f);
            var ajuste = bloque.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.horizontalFit = ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rejilla = Ui.Fila(bloque, "Arriba", Tema.espacio, 0, TextAnchor.MiddleCenter);
            var disponibles = S.TareasDeOficina.Count(t => S.PorQueNoSePuedeHacer(t.Id) == null);
            var oficina = Mosaico(rejilla, "Práctica", "Retos de práctica", disponibles > 0 ? disponibles.ToString() : null, AbrirEscritorio, disponibles > 0);
            GuiaView.Registrar("dia.oficina", oficina);

            var arq = S.Perfil.Fase1 == null ? null : S.Perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == S.ArquitecturaElegida);
            Mosaico(rejilla, "El plano", arq != null ? arq.Nombre : "Arquitectura", null,
                    () => { if (arq != null) PantallaDelProducto.AbrirPlano(App, arq.Id, true); }, false);

            var probar = S.PorQueNoSePuedeProbar();
            // La insignia dice cuantas rondas quedan hoy: «1» si se puede, nada si ya se hizo (antes salia un cuadrito).
            Mosaico(rejilla, "Pruebas", $"Cobertura {S.W.Cobertura:0}%", probar == null ? "1" : null, () => {
                if (probar == null) Actuar(() => S.EjecutarPruebas(), Solo("El equipo hizo una ronda de pruebas.", "Hiciste una ronda de pruebas."));
                else AbrirMonitoreo(0);
            }, probar == null);

            var abajo = Ui.Fila(bloque, "Abajo", Tema.espacio, 0, TextAnchor.MiddleCenter);
            var quedan = S.R.Suministros.Values.Sum();
            Mosaico(abajo, "Suministros", quedan > 0 ? "Del recorrido" : "No trajiste nada", quedan > 0 ? "×" + quedan : null,
                    () => { if (S.R.Suministros.Count > 0) AbrirSuministros(); }, false);

            var pendientes = S.CeremoniasDeHoy().Count(c => !c.Hecha);
            var ceremonias = Mosaico(abajo, "Ceremonias", S.Metodologia.Nombre, pendientes > 0 ? pendientes.ToString() : null, () => AbrirCeremonias(0), pendientes > 0);
            GuiaView.Registrar("dia.ceremonias", ceremonias);
        }

        private RectTransform Mosaico(RectTransform padre, string nombre, string sub, string insignia, Action alPulsar, bool brilla) {
            var t = Ui.PanelColumna(padre, nombre, Tema.Espacio(5), Tema.Espacio(1), Tema.surfaceRaised);
            UiKit.Tamano(t, ancho: 400, alto: 190);
            Ui.Borde(t, brilla ? Tema.cyan : Tema.lineStrong, NexusTheme.RadioLg, brilla ? 2 : 1);
            if (brilla) Ui.Halo(t, NexusTheme.Alfa(Tema.cyan, 0.45f), NexusTheme.RadioLg);
            Ui.Texto(t, nombre, EstiloTexto.Titulo, Tema.ink);
            Ui.Texto(t, sub, EstiloTexto.Pequeno, Tema.inkMuted);
            if (!string.IsNullOrEmpty(insignia)) {
                var b = Ui.Nodo(t, "Insignia");
                b.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                b.anchorMin = b.anchorMax = b.pivot = new Vector2(1, 1);
                b.anchoredPosition = new Vector2(-14, -14);
                b.sizeDelta = new Vector2(46, 46);
                var img = b.gameObject.AddComponent<Image>();
                img.sprite = Ui.SpriteCircular();
                img.color = Tema.cyan;
                img.raycastTarget = false;
                var texto = Ui.Texto(b, insignia, EstiloTexto.Encabezado, Tema.bg950, TextAlignmentOptions.Center);
                QuitarAltoMinimo(texto);
                UiKit.Rellenar(texto.rectTransform);
            }
            var viva = t.gameObject.AddComponent<PiezaViva>();
            viva.Escala = 1.07f;
            viva.AlPulsar = alPulsar;
            return t;
        }

        // ==================================================================== cliente

        private void PintarCliente(RectTransform cuerpo) {
            var ficha = S.Perfil.Proyecto;
            var fila = Ui.Fila(cuerpo, "Cliente", Tema.margen, alineacion: TextAnchor.UpperCenter);
            UiKit.Rellenar(fila);
            GuiaView.Registrar("dia.cliente", fila);

            var quien = Ui.PanelColumna(fila, "Quien", Tema.Espacio(5), Tema.Espacio(2), Tema.surfaceRaised);
            UiKit.Tamano(quien, ancho: 380);
            var nombre = ficha != null && ficha.Cliente != null ? ficha.Cliente.Nombre : "El cliente";
            Ui.Ilustracion(quien, MaterialesNexus.IdDePersonaje(nombre), null, ancho: 300, alto: 220);
            Ui.Texto(quien, nombre, EstiloTexto.Subtitulo, Tema.ink, TextAlignmentOptions.Center);
            var s = S.W.SatisfaccionCliente;
            Ui.Dial(quien, (float)(s / 100), "satisfacción", s <= 35 ? Tema.danger : s >= 65 ? Tema.success : Tema.warning, 130);

            var papeles = Ui.Columna(fila, "Papeles", Tema.espacio);
            UiKit.Tamano(papeles, ancho: 0, flexAncho: 1);
            var rejilla = Ui.Nodo(papeles, "Rejilla");
            var g = rejilla.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(300, 150);
            g.spacing = new Vector2(Tema.espacio, Tema.espacio);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            rejilla.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var p in S.PapelesDelCliente().OrderByDescending(x => x.Dia).Take(6)) {
                var nuevo = !S.R.PapelesLeidos.Contains(p.Id);
                var papel = Ui.PanelColumna(rejilla, "Papel", Tema.Espacio(3), 4, new Color32(0xF3, 0xEC, 0xD8, 0xFF));
                if (nuevo) Ui.Halo(papel, NexusTheme.Alfa(Tema.cyan, 0.7f), NexusTheme.RadioSm);
                Ui.Texto(papel, $"Día {p.Dia}" + (nuevo ? " · NUEVO" : ""), EstiloTexto.Leyenda, new Color32(0x6A, 0x5A, 0x3A, 0xFF));
                var t = Ui.Texto(papel, p.Titulo, EstiloTexto.Encabezado, new Color32(0x26, 0x26, 0x2A, 0xFF));
                t.maxVisibleLines = 3;
                t.overflowMode = TextOverflowModes.Ellipsis;
                var viva = papel.gameObject.AddComponent<PiezaViva>();
                viva.AlPulsar = AbrirCarpetaDelCliente;
            }
            foreach (var v in S.VocesDelProducto().Take(3))
                Ui.Texto(papeles, $"«{v.Frase}» — {v.Quien}", EstiloTexto.Dialogo, Tema.violet);
        }
    }
}
