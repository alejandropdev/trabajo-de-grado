using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using Nexus.Core.Sesion;
using Nexus.Core.Tarjetas;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las ceremonias (2026-11-07), con el lenguaje de los indicadores y casi sin texto. Antes era un panel de parrafos
    /// (lo que no hay en la metodologia, para que sirve cada una, el acta entera…). Ahora:
    ///
    ///   Hoy      cuatro indicadores (hechas, minutos que costarian, asistencia, la proxima) y una tarjeta por
    ///            ceremonia: su icono, su nombre, cuanto dura y UNA linea. Pendiente: «Asistir». Hecha: un sello y lo
    ///            que se eligio; el acta entera, al pasar el raton.
    ///   Agenda   una linea de tiempo de los dias del nivel: hoy late, lo pasado lleva su marca, cada ceremonia su
    ///            color; las diarias, en una franja aparte. Al pasar por un dia, lo que hubo y si fuiste.
    ///   Actas    la asistencia en grande y las actas como tarjetas: lo que elegiste de titulo; el texto, al pasar.
    ///
    /// El marco es el del muro del backlog (PantallaDelMuro): translucido sobre la escena, el reloj espera mientras se mira.
    /// Movimiento segun emil-design: entradas cortas ease-out con escalonado tope 8, solo al abrir o cambiar de pestaña.
    /// </summary>
    public sealed partial class PantallaDelDia {
        private PantallaDelMuro _salaDeCeremonias;
        private string _ceremoniaAbierta;
        private bool _ayudaDeCeremonias, _graficaDeCeremonia, _todasLasActas;
        private int _pestanaPintada = -1, _pestanaAnteriorDeCeremonias;
        private readonly HashSet<string> _sellosVistos = new HashSet<string>();

        private static readonly string[] PestanasDeCeremonias = { "Hoy", "Agenda", "Actas" };

        /// <summary>Abre una pantalla de indicadores (el marco del muro) y para el reloj mientras se mira.</summary>
        private PantallaDelMuro AbrirPantallaDeIndicadores(string etiqueta, string titulo, Action<PantallaDelMuro> pintar, Func<string> clave,
                                                           Action<PantallaDelMuro> guardar) {
            Runner.Mirando = true;
            try {
                return App.Router.Apilar<PantallaDelMuro>(p => {
                    p.Etiqueta = etiqueta;
                    p.Titulo = titulo;
                    p.Pintar = pintar;
                    p.Clave = clave;
                    p.AlCerrar = () => {
                        guardar(null);
                        if (App != null && Runner != null) Runner.Mirando = false;
                    };
                    p.AlReabrir = m => {
                        guardar(m);
                        if (App != null && Runner != null) Runner.Mirando = true;
                    };
                });
            } catch {
                Runner.Mirando = false;
                throw;
            }
        }

        private void AbrirCeremonias(int pestana) {
            if (_panel != null || _muro != null || _salaDeCeremonias != null || _salaDeSuministros != null) return;
            _decidido = null;
            _pestanaDeCeremonias = Mathf.Clamp(pestana, 0, 2);
            _pestanaAnteriorDeCeremonias = _pestanaDeCeremonias;
            _pestanaPintada = -1;
            _ceremoniaAbierta = null;
            _ayudaDeCeremonias = _graficaDeCeremonia = _todasLasActas = false;
            _ultimoResultado = null;
            _salaDeCeremonias = AbrirPantallaDeIndicadores(S.Metodologia.Nombre, "Ceremonias", PintarCeremonias, ClaveDeCeremonias, m => _salaDeCeremonias = m);
        }

        private string ClaveDeCeremonias() {
            return S.MinutoDelDia + "|" + S.R.DiaActual + "|" + S.R.CeremoniasHechasHoy.Count + "|" + S.R.ActasDeCeremonias.Count + "|" +
                   AlertasSonando().Count + "|" + _pestanaDeCeremonias + "|" + _ceremoniaAbierta + "|" + _ayudaDeCeremonias + "|" +
                   _graficaDeCeremonia + "|" + _todasLasActas;
        }

        private void PintarCeremonias(PantallaDelMuro m) {
            // Se anima al abrir y al cambiar de pestaña; nunca al repintar despues de asistir.
            var animar = _pestanaPintada != _pestanaDeCeremonias;
            _pestanaPintada = _pestanaDeCeremonias;
            HerramientasDeCeremonias(m);

            var sonando = AlertasSonando().Count;
            if (sonando > 0) {
                var tira = Ui.Fila(m.Cuerpo, "Aviso sonando", Tema.espacio, 10, TextAnchor.MiddleLeft);
                Ui.Fondo(tira.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.warning, 0.14f), 12);
                Ui.Punto(tira, Tema.warning, 12);
                UiKit.Tamano(Ui.Texto(tira, sonando == 1 ? "Ha sonado un aviso: su tiempo corre en cuanto vuelvas." : $"Han sonado {sonando} avisos: su tiempo corre en cuanto vuelvas.",
                                      EstiloTexto.Pequeno, Tema.ink), flexAncho: 1);
                Ui.Boton(tira, "Volver a la jornada", CerrarPanel, VarianteBoton.Primario);
            }
            if (_ayudaDeCeremonias) {
                var ayuda = PiezasDelMuro.Panel(Ui, m.Cuerpo, "Lo que hay que saber", Tema.cyan, 14);
                if (Politica != null && !string.IsNullOrEmpty(Politica.LoQueNoHay)) Ui.Texto(ayuda, Politica.LoQueNoHay, EstiloTexto.Pequeno, Tema.ink);
                PantallaDeGlosario.Palabras(App, ayuda, "Palabras:", "ceremonia", "retrospectiva", Politica != null && Politica.SprintCerrado ? "sprint" : "wip");
            }

            switch (_pestanaDeCeremonias) {
                case 0: PintarHoyDeCeremonias(m, animar); break;
                case 1: PintarAgendaDeCeremonias(m, animar); break;
                default: PintarActasDeCeremonias(m, animar); break;
            }
        }

        // ==================================================================== arriba: pestañas

        /// <summary>Hoy | Agenda | Actas como un control segmentado, con una raya que se desliza bajo la elegida.</summary>
        private void HerramientasDeCeremonias(PantallaDelMuro m) {
            const float ancho = 150;
            var h = m.Herramientas;
            var control = Ui.Fila(h, "Pestañas", 0, 4, TextAnchor.MiddleCenter);
            Ui.Fondo(control.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.bg950, 0.7f), NexusTheme.RadioMd);
            Ui.Borde(control, Tema.lineStrong, NexusTheme.RadioMd, 1);
            for (var i = 0; i < PestanasDeCeremonias.Length; i++) {
                var indice = i;
                var activa = i == _pestanaDeCeremonias;
                var b = Ui.Boton(control, PestanasDeCeremonias[i], () => {
                    if (_pestanaDeCeremonias == indice) return;
                    _pestanaAnteriorDeCeremonias = _pestanaDeCeremonias;
                    _pestanaDeCeremonias = indice;
                    _ceremoniaAbierta = null;
                }, VarianteBoton.Fantasma);
                UiKit.Tamano(b, ancho);
                b.GetComponentInChildren<TMP_Text>().color = activa ? Tema.cyan : Tema.inkMuted;
            }
            var raya = Ui.Nodo(control, "Raya");
            UiKit.Tamano(raya).ignoreLayout = true;
            raya.anchorMin = raya.anchorMax = raya.pivot = new Vector2(0, 0);
            raya.sizeDelta = new Vector2(ancho - 24, 3);
            var img = raya.gameObject.AddComponent<Image>();
            img.color = Tema.cyan;
            img.raycastTarget = false;
            var desliza = raya.gameObject.AddComponent<RayaQueSeDesliza>();
            desliza.Desde = 4 + 12 + _pestanaAnteriorDeCeremonias * ancho;
            desliza.Hasta = 4 + 12 + _pestanaDeCeremonias * ancho;
            _pestanaAnteriorDeCeremonias = _pestanaDeCeremonias;

            Ui.Boton(h, _ayudaDeCeremonias ? "Ocultar" : "?", () => { _ayudaDeCeremonias = !_ayudaDeCeremonias; }, VarianteBoton.Fantasma);
        }

        /// <summary>La raya de la pestaña elegida: va de la anterior a la nueva (ease-in-out, ~200 ms).</summary>
        private sealed class RayaQueSeDesliza : MonoBehaviour {
            public float Desde, Hasta;
            private float _t;

            private void Update() {
                _t += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(_t / 0.2f);
                var e = p < 0.5f ? 4 * p * p * p : 1 - Mathf.Pow(-2 * p + 2, 3) / 2;
                ((RectTransform)transform).anchoredPosition = new Vector2(Mathf.Lerp(Desde, Hasta, e), 4);
                if (p >= 1) enabled = false;
            }
        }

        // ==================================================================== franja de indicadores

        private RectTransform FranjaDeIndicadores(PantallaDelMuro m) {
            var fila = Ui.Fila(m.Cuerpo, "Indicadores", Tema.espacio, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(fila, alto: 124, flexAncho: 1);
            return fila;
        }

        private RectTransform Indicador(RectTransform fila, string rotulo, Color color, int indice, bool animar) {
            var p = PiezasDelMuro.Panel(Ui, fila, rotulo, color, 14);
            UiKit.Tamano(p, ancho: 0, flexAncho: 1, flexAlto: 1);
            p.gameObject.AddComponent<PiezaViva>().Escala = 1.02f;
            if (animar) p.gameObject.AddComponent<EntradaDePieza>().Retraso = Mathf.Min(8, indice) * 0.05f;
            return p;
        }

        /// <summary>Asistencia: cuantas actas son de haber ido, y por dia.</summary>
        private void IndicadorDeAsistencia(RectTransform fila, int indice, bool animar) {
            var actas = S.R.ActasDeCeremonias;
            var asistidas = actas.Count(a => a.Asistio);
            var parte = actas.Count == 0 ? 0 : (double)asistidas / actas.Count;
            var color = actas.Count == 0 ? Tema.inkMuted : parte >= 0.7 ? Tema.success : parte >= 0.4 ? Tema.warning : Tema.danger;
            var p = Indicador(fila, "Asistencia", color, indice, animar);
            var c = Ui.Fila(p, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, c, parte * 100, "0", "%", color, 40, animar ? 0.15f : -1);
            var porDia = new List<double>();
            for (var d = 1; d <= Math.Max(1, S.R.DiaActual); d++) porDia.Add(actas.Count(a => a.Asistio && a.Dia == d));
            PiezasDelMuro.MiniGrafica(Ui, c, actas.Count == 0 ? null : porDia, color, 34, 10);
            Ui.Texto(p, actas.Count == 0 ? "aún no hay actas" : $"{asistidas} de {actas.Count}", EstiloTexto.Leyenda, Tema.inkMuted);
        }

        // ==================================================================== hoy

        private void PintarHoyDeCeremonias(PantallaDelMuro m, bool animar) {
            if (S.R.DiaActual == 0) { PiezasDelMuro.Vacio(Ui, m.Cuerpo, "Las ceremonias empiezan con el día 1.", 120); return; }
            var hoy = S.CeremoniasDeHoy();
            var plan = S.PlanDeHoy;
            var alEmpezar = plan == null ? new List<Ceremonia>() : plan.Ceremonias.Where(x => x.Opciones == null || x.Opciones.Count == 0).ToList();
            var proxima = S.AgendaDeCeremonias().FirstOrDefault(c => c.Dia > S.R.DiaActual);

            // ---- indicadores
            var fila = FranjaDeIndicadores(m);
            var hechas = hoy.Count(c => c.Hecha);
            var pHoy = Indicador(fila, "Hoy", Tema.cyan, 0, animar);
            var cHoy = Ui.Fila(pHoy, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, cHoy, hechas, "0", "", hechas == hoy.Count && hoy.Count > 0 ? Tema.success : Tema.cyan, 40, animar ? 0.1f : -1);
            Ui.Texto(cHoy, $"de {hoy.Count} hechas", EstiloTexto.Pequeno, Tema.inkMuted);
            PiezasDelMuro.Barra(Ui, pHoy, hoy.Count == 0 ? 0 : (float)hechas / hoy.Count, Tema.cyan, animar ? 0.1f : -1);

            var minutos = hoy.Where(c => !c.Hecha).Sum(c => c.DuracionMinutos);
            var pMin = Indicador(fila, "Minutos", Tema.warning, 1, animar);
            var cMin = Ui.Fila(pMin, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, cMin, minutos, "0", "", Tema.warning, 40, animar ? 0.15f : -1);
            Ui.Texto(cMin, "min si vas a todo", EstiloTexto.Pequeno, Tema.inkMuted);
            Ui.Texto(pMin, "Mirar es gratis; asistir, no.", EstiloTexto.Leyenda, Tema.inkFaint);

            IndicadorDeAsistencia(fila, 2, animar);

            var pProx = Indicador(fila, "La próxima", Tema.violet, 3, animar);
            if (proxima == null) Ui.Texto(pProx, "No quedan más", EstiloTexto.Encabezado, Tema.inkFaint);
            else {
                var dias = proxima.Dia - S.R.DiaActual;
                var cProx = Ui.Fila(pProx, "Cifra", 8, 0, TextAnchor.LowerLeft);
                PiezasDelMuro.Cifra(Ui, cProx, dias, "0", "", Tema.violet, 40, animar ? 0.2f : -1);
                Ui.Texto(cProx, dias == 1 ? "día" : "días", EstiloTexto.Pequeno, Tema.inkMuted);
                var nombre = Ui.Texto(pProx, proxima.Nombre, EstiloTexto.Pequeno, Tema.ink);
                nombre.maxVisibleLines = 1;
                nombre.overflowMode = TextOverflowModes.Ellipsis;
            }

            // ---- una tarjeta por ceremonia
            var mesa = Ui.Fila(m.Cuerpo, "Ceremonias", Tema.espacio * 1.5f, 0, TextAnchor.UpperCenter);
            UiKit.Tamano(mesa, flexAncho: 1);
            if (hoy.Count == 0 && alEmpezar.Count == 0) {
                var vacio = PiezasDelMuro.Vacio(Ui, mesa, proxima == null ? "Hoy no hay ceremonias." : $"Hoy no hay ceremonias. La próxima: {proxima.Nombre}, día {proxima.Dia}.", 140);
                UiKit.Tamano(vacio, ancho: 640);
                return;
            }
            var n = 0;
            foreach (var c in alEmpezar) TarjetaDeCeremoniaAlEmpezar(mesa, c, animar ? Mathf.Min(8, 4 + n++) * 0.05f : -1);
            foreach (var c in hoy) TarjetaDeCeremoniaDeHoy(mesa, c, animar ? Mathf.Min(8, 4 + n++) * 0.05f : -1);

            // ---- la que se esta llevando sin tarjeta: debajo, sus formas como cartas
            var abierta = hoy.FirstOrDefault(c => c.Id == _ceremoniaAbierta && !c.Hecha);
            if (abierta != null) ManoDeLaCeremonia(m, abierta);
        }

        /// <summary>Una ceremonia de hoy: icono, nombre, duracion y una linea. Hecha, con su sello; el acta, al pasar.</summary>
        private void TarjetaDeCeremoniaDeHoy(RectTransform mesa, CeremoniaDeHoy c, float retraso) {
            var color = PiezasDelMuro.ColorDeCeremonia(Tema, c.Id);
            var tarjeta = PiezasDelMuro.Panel(Ui, mesa, "", c.Hecha ? Tema.success : color, 20);
            UiKit.Tamano(tarjeta, 360, 300);
            if (retraso >= 0) tarjeta.gameObject.AddComponent<EntradaDePieza>().Retraso = retraso;
            if (c.Hecha) tarjeta.GetComponent<Image>().color = NexusTheme.Alfa(Color.Lerp(Tema.surfaceRaised, Tema.success, 0.06f), 0.94f);

            var cabecera = Ui.Fila(tarjeta, "Cabecera", 12, 0, TextAnchor.MiddleLeft);
            PiezasDelMuro.IconoDeCeremonia(Ui, cabecera, c.Id, 52, c.Hecha ? Tema.success : color);
            if (!c.Hecha) Ui.Chip(cabecera, $"{c.DuracionMinutos} min", NexusTheme.Alfa(color, 0.18f), color);
            var nombre = Ui.Texto(tarjeta, c.Nombre.ToUpperInvariant(), EstiloTexto.Encabezado, Tema.ink);
            nombre.font = Tema.FuenteDisplay;
            nombre.maxVisibleLines = 2;
            nombre.overflowMode = TextOverflowModes.Ellipsis;

            if (c.Hecha) {
                var acta = S.R.ActasDeCeremonias.LastOrDefault(a => a.Dia == S.R.DiaActual && a.CeremoniaId == c.Id);
                var clave = S.R.DiaActual + "|" + c.Id;
                PiezasDelMuro.Sello(Ui, tarjeta, "HECHA", Tema.success, !_sellosVistos.Contains(clave));
                _sellosVistos.Add(clave);
                // Lo que se eligio, corto; el acta entera, al pasar el raton (en el sitio de lo corto).
                var corto = Ui.Columna(tarjeta, "Corto", 6);
                Ui.Texto(corto, "LA LLEVASTE ASÍ", EstiloTexto.Leyenda, Tema.success);
                var opcion = Ui.Texto(corto, acta == null ? "" : acta.Opcion, EstiloTexto.Cuerpo, Tema.ink);
                opcion.maxVisibleLines = 3;
                opcion.overflowMode = TextOverflowModes.Ellipsis;
                UiKit.Tamano(Ui.Nodo(corto, "Hueco"), flexAlto: 1);
                if (acta != null && !string.IsNullOrEmpty(acta.Texto)) {
                    Ui.Texto(corto, "Pasa el ratón para leer el acta", EstiloTexto.Leyenda, Tema.inkFaint);
                    var largo = Ui.Texto(tarjeta, acta.Texto, EstiloTexto.Pequeno, Tema.ink);
                    largo.maxVisibleLines = 7;
                    largo.overflowMode = TextOverflowModes.Ellipsis;
                    largo.gameObject.SetActive(false);
                    var viva = tarjeta.gameObject.AddComponent<PiezaViva>();
                    viva.Escala = 1.02f;
                    viva.Revelar = largo.gameObject;
                    viva.Ocultar = corto.gameObject;
                }
                return;
            }

            var linea = !string.IsNullOrEmpty(c.Frase) ? c.Frase : PiezasDelMuro.PrimeraFrase(c.ParaQueSirve);
            var frase = Ui.Texto(tarjeta, linea, EstiloTexto.Cuerpo, Tema.inkMuted);
            frase.maxVisibleLines = 3;
            frase.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Tamano(Ui.Nodo(tarjeta, "Hueco"), flexAlto: 1);
            var porQueNo = S.PorQueNoSePuedeAsistir(c.Id);
            if (porQueNo != null) {
                var no = Ui.Texto(tarjeta, porQueNo, EstiloTexto.Pequeno, Tema.warning);
                no.maxVisibleLines = 2;
                no.overflowMode = TextOverflowModes.Ellipsis;
                return;
            }
            var tarjetaDef = TarjetaDeCeremoniaSiHay(c);
            var abierta = _ceremoniaAbierta == c.Id;
            var boton = Ui.Boton(tarjeta, tarjetaDef == null && abierta ? "Elige abajo" : "Asistir", () => {
                if (tarjetaDef != null) AsistirConTarjeta(c, tarjetaDef);
                else _ceremoniaAbierta = abierta ? null : c.Id;
            }, VarianteBoton.Primario);
            UiKit.Tamano(boton, flexAncho: 1);
            tarjeta.gameObject.AddComponent<PiezaViva>().Escala = 1.02f;
        }

        /// <summary>Las que se resuelven al empezar el dia (planificar, la retro): de hoy, pero sin nada que hacer aqui.</summary>
        private void TarjetaDeCeremoniaAlEmpezar(RectTransform mesa, Ceremonia c, float retraso) {
            var tarjeta = PiezasDelMuro.Panel(Ui, mesa, "", Tema.inkFaint, 20);
            UiKit.Tamano(tarjeta, 300, 300);
            tarjeta.gameObject.AddComponent<CanvasGroup>().alpha = 0.85f;
            if (retraso >= 0) {
                var e = tarjeta.gameObject.AddComponent<EntradaDePieza>();
                e.Retraso = retraso;
                e.AlfaFinal = 0.85f;
            }
            PiezasDelMuro.IconoDeCeremonia(Ui, tarjeta, c.Id, 52, Tema.inkMuted);
            var nombre = Ui.Texto(tarjeta, (c.Nombre ?? c.Id).ToUpperInvariant(), EstiloTexto.Encabezado, Tema.inkMuted);
            nombre.font = Tema.FuenteDisplay;
            var frase = Ui.Texto(tarjeta, PiezasDelMuro.PrimeraFrase(c.ParaQueSirve), EstiloTexto.Pequeno, Tema.inkFaint);
            frase.maxVisibleLines = 3;
            frase.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Tamano(Ui.Nodo(tarjeta, "Hueco"), flexAlto: 1);
            var alInicio = c.AjustaCoeficiente || string.Equals(c.Verbo, "V5", StringComparison.OrdinalIgnoreCase);
            Ui.Chip(tarjeta, alInicio ? "al empezar el día" : "pasa sola", NexusTheme.Alfa(Tema.inkFaint, 0.2f), Tema.inkMuted);
        }

        /// <summary>Sin tarjetas (textos completos): las formas de llevarla como una mano de cartas, debajo de la mesa.</summary>
        private void ManoDeLaCeremonia(PantallaDelMuro m, CeremoniaDeHoy c) {
            var color = PiezasDelMuro.ColorDeCeremonia(Tema, c.Id);
            var panel = PiezasDelMuro.Panel(Ui, m.Cuerpo, "¿Cómo la llevas? " + c.Nombre, color, 16);
            panel.gameObject.AddComponent<EntradaDePieza>();
            if (!string.IsNullOrEmpty(c.Muestra) && c.Muestra != VistasDeMonitoreo.Tablero) {
                Ui.Boton(panel, _graficaDeCeremonia ? "Ocultar la gráfica" : "Ver lo que hay sobre la mesa", () => { _graficaDeCeremonia = !_graficaDeCeremonia; }, VarianteBoton.Fantasma);
                if (_graficaDeCeremonia) PintarVistaDeMonitoreo(c.Muestra, panel);
            }
            var motivo = S.PorQueNoSePuedeAsistir(c.Id);
            var cartas = c.Opciones.Select(opcion => {
                var o = opcion;
                return new OpcionDeCarta {
                    Titulo = o.Texto,
                    Bloqueo = motivo,
                    AlElegir = () => {
                        _ceremoniaAbierta = null;
                        Actuar(() => S.AsistirACeremonia(c.Id, o.Id), $"Asististe a «{c.Nombre}».");
                    },
                };
            }).ToList();
            ManoDeCartas.Crear(Ui, Ui.Fila(panel, "Mano", 0, alineacion: TextAnchor.MiddleCenter), cartas, new ManoDeCartas.Ajustes { Sello = "ACORDADO" });
        }

        /// <summary>
        /// Asistir a una ceremonia como tarjeta: la escena para el reloj, se elige deslizando o con un boton y, al cerrar,
        /// el reloj cobra los minutos y aplica lo que esa forma de llevarla provoca (AsistirACeremonia), igual que antes.
        /// </summary>
        private void AsistirConTarjeta(CeremoniaDeHoy ceremonia, TarjetaDef tarjeta) {
            OpcionTarjeta elegida = null;
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<PantallaDeTarjetas>(p => {
                p.Sesion = S;
                p.Pizarra = true;   // las ceremonias, en la pizarra de post-its (2026-11-05)
                p.Etiqueta = "Ceremonia";
                p.Tarjetas = new List<TarjetaDef> { tarjeta };
                p.ResuelveOtro = true;
                p.AlElegir = (_, opcion) => elegida = opcion;
                p.AlTerminar = () => {
                    App.Router.Volver();
                    _escenaApilada = false;
                    Runner.EnEscena = false;
                    if (elegida != null)
                        Actuar(() => S.AsistirACeremonia(ceremonia.Id, elegida.Id), $"Asististe a «{ceremonia.Nombre}».");
                };
            });
        }

        /// <summary>La tarjeta de la ceremonia, salvo que el jugador haya pedido los textos completos (Opciones).</summary>
        private TarjetaDef TarjetaDeCeremoniaSiHay(CeremoniaDeHoy c) {
            return App.Preferencias.modoTarjetas ? TarjetaDeCeremonia.De(c) : null;
        }

        // ==================================================================== agenda

        /// <summary>
        /// La agenda como una linea de tiempo: un nodo por dia (lo pasado en verde, hoy latiendo), y debajo las
        /// ceremonias de ese dia como pastillas de su color. Las diarias van en una franja aparte, una sola vez.
        /// </summary>
        private void PintarAgendaDeCeremonias(PantallaDelMuro m, bool animar) {
            var agenda = S.AgendaDeCeremonias();
            if (agenda.Count == 0) { PiezasDelMuro.Vacio(Ui, m.Cuerpo, "Esta metodología no tiene ceremonias.", 120); return; }
            var dias = Math.Max(1, S.Perfil.DiasTotales);
            var diarias = agenda.GroupBy(c => c.Id).Where(g => g.Count() >= dias).Select(g => g.First()).ToList();
            var idsDiarias = new HashSet<string>(diarias.Select(c => c.Id));
            var resto = agenda.Where(c => !idsDiarias.Contains(c.Id)).ToList();
            var actas = S.R.ActasDeCeremonias;

            // ---- indicadores
            var fila = FranjaDeIndicadores(m);
            var pTotal = Indicador(fila, "En el nivel", Tema.cyan, 0, animar);
            var cTotal = Ui.Fila(pTotal, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, cTotal, agenda.Count, "0", "", Tema.cyan, 40, animar ? 0.1f : -1);
            Ui.Texto(cTotal, "ceremonias", EstiloTexto.Pequeno, Tema.inkMuted);
            var pasadas = agenda.Count(c => c.Cuando == "pasada");
            PiezasDelMuro.Barra(Ui, pTotal, (float)pasadas / agenda.Count, Tema.cyan, animar ? 0.1f : -1);
            var pQuedan = Indicador(fila, "Quedan", Tema.violet, 1, animar);
            var cQuedan = Ui.Fila(pQuedan, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, cQuedan, agenda.Count(c => c.Cuando != "pasada"), "0", "", Tema.violet, 40, animar ? 0.15f : -1);
            Ui.Texto(cQuedan, "contando hoy", EstiloTexto.Pequeno, Tema.inkMuted);
            IndicadorDeAsistencia(fila, 2, animar);

            // ---- las diarias, una vez
            if (diarias.Count > 0) {
                var franja = Ui.Fila(m.Cuerpo, "Diarias", 10, 12, TextAnchor.MiddleLeft);
                Ui.Fondo(franja.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.surfaceRaised, 0.94f), NexusTheme.RadioLg);
                Ui.Borde(franja, Tema.lineStrong, NexusTheme.RadioLg, 1);
                Ui.Texto(franja, "TODOS LOS DÍAS", EstiloTexto.Leyenda, Tema.inkMuted);
                foreach (var d in diarias) {
                    var color = PiezasDelMuro.ColorDeCeremonia(Tema, d.Id);
                    PiezasDelMuro.IconoDeCeremonia(Ui, franja, d.Id, 26, color);
                    Ui.Texto(franja, d.Nombre, EstiloTexto.Cuerpo, Tema.ink);
                }
                if (animar) franja.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.15f;
            }

            // ---- la linea de tiempo
            var linea = PiezasDelMuro.Panel(Ui, m.Cuerpo, "Día a día", Tema.cyan, 16);
            UiKit.Tamano(linea, flexAncho: 1, flexAlto: 1);
            if (animar) linea.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.2f;
            var columnas = Ui.Fila(linea, "Dias", 6, 0, TextAnchor.UpperCenter);
            UiKit.Tamano(columnas, flexAncho: 1, flexAlto: 1);
            for (var dia = 1; dia <= dias; dia++) {
                var esHoy = dia == S.R.DiaActual;
                var pasado = dia < S.R.DiaActual;
                var delDia = resto.Where(c => c.Dia == dia).ToList();
                var col = Ui.Columna(columnas, "Dia " + dia, 6, 4, TextAnchor.UpperCenter);
                UiKit.Tamano(col, ancho: 0, flexAncho: 1, flexAlto: 1);
                var fondoCol = col.gameObject.AddComponent<Image>();
                if (esHoy) Ui.Fondo(fondoCol, NexusTheme.Alfa(Tema.cyan, 0.08f), NexusTheme.RadioMd);
                else fondoCol.color = new Color(0, 0, 0, 0);

                // El nodo del dia.
                var nodo = Ui.Nodo(col, "Nodo");
                UiKit.Tamano(nodo, 34, 34);
                var circulo = nodo.gameObject.AddComponent<Image>();
                circulo.sprite = Ui.SpriteCircular();
                circulo.color = esHoy ? Tema.cyan : pasado ? NexusTheme.Alfa(Tema.success, 0.3f) : NexusTheme.Alfa(Tema.inkFaint, 0.3f);
                circulo.raycastTarget = false;
                var numero = Ui.Texto(nodo, dia.ToString(), EstiloTexto.Leyenda, esHoy ? Tema.onCyan : pasado ? Tema.success : Tema.inkMuted, TextAlignmentOptions.Center);
                UiKit.Tamano(numero).ignoreLayout = true;
                UiKit.Rellenar(numero.rectTransform);
                if (esHoy) {
                    nodo.gameObject.AddComponent<Latido>().Fuerza = 0.08f;
                    Ui.Texto(col, "HOY", EstiloTexto.Leyenda, Tema.cyan, TextAlignmentOptions.Center);
                }

                // Sus ceremonias: una pastilla de color cada una; al pasar, el detalle (y si fuiste).
                var pastillas = Ui.Columna(col, "Pastillas", 4, 0, TextAnchor.UpperCenter);
                var detalle = Ui.Columna(col, "Detalle", 4, 6, TextAnchor.UpperLeft);
                Ui.Fondo(detalle.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.bg950, 0.92f), NexusTheme.RadioSm);
                foreach (var c in delDia) {
                    var color = PiezasDelMuro.ColorDeCeremonia(Tema, c.Id);
                    var fue = actas.Any(a => a.Dia == dia && a.CeremoniaId == c.Id && a.Asistio);
                    var pastilla = Ui.Nodo(pastillas, "Pastilla");
                    UiKit.Tamano(pastilla, 30, 10);
                    var img = pastilla.gameObject.AddComponent<Image>();
                    Ui.Fondo(img, pasado && !fue ? NexusTheme.Alfa(color, 0.3f) : color, 5);
                    img.raycastTarget = false;
                    Ui.Texto(detalle, (pasado ? (fue ? "fuiste: " : "sin ti: ") : "") + c.Nombre, EstiloTexto.Leyenda, pasado && !fue ? Tema.inkFaint : color);
                }
                detalle.gameObject.SetActive(false);
                if (delDia.Count > 0) {
                    var viva = col.gameObject.AddComponent<PiezaViva>();
                    viva.Escala = 1.04f;
                    viva.Revelar = detalle.gameObject;
                    viva.Ocultar = pastillas.gameObject;
                }
                if (animar) col.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.25f + Mathf.Min(8, dia) * 0.03f;
            }

            // ---- la leyenda de colores
            var leyenda = Ui.Fila(m.Cuerpo, "Leyenda", 14, 0, TextAnchor.MiddleLeft);
            foreach (var g in resto.GroupBy(c => c.Id)) {
                var item = Ui.Fila(leyenda, "Item", 6, 0, TextAnchor.MiddleLeft);
                var pastilla = Ui.Nodo(item, "Pastilla");
                UiKit.Tamano(pastilla, 22, 8);
                Ui.Fondo(pastilla.gameObject.AddComponent<Image>(), PiezasDelMuro.ColorDeCeremonia(Tema, g.Key), 4);
                Ui.Texto(item, g.First().Nombre, EstiloTexto.Pequeno, Tema.inkMuted);
            }
            UiKit.Tamano(Ui.Nodo(leyenda, "Hueco"), flexAncho: 1);
            Ui.Texto(leyenda, "Pasa el ratón por un día para ver qué hubo", EstiloTexto.Leyenda, Tema.inkFaint);
        }

        // ==================================================================== actas

        /// <summary>Las actas como tarjetas: de que dia y que ceremonia, lo que se eligio de titulo y el texto al pasar.</summary>
        private void PintarActasDeCeremonias(PantallaDelMuro m, bool animar) {
            var actas = S.R.ActasDeCeremonias;
            var fila = FranjaDeIndicadores(m);
            IndicadorDeAsistencia(fila, 0, animar);
            var pTotal = Indicador(fila, "Actas", Tema.cyan, 1, animar);
            var cTotal = Ui.Fila(pTotal, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, cTotal, actas.Count, "0", "", Tema.cyan, 40, animar ? 0.1f : -1);
            Ui.Texto(cTotal, "escritas", EstiloTexto.Pequeno, Tema.inkMuted);
            var sinTi = actas.Count(a => !a.Asistio);
            var colorSinTi = sinTi > 0 ? Tema.warning : Tema.success;
            var pSinTi = Indicador(fila, "Sin ti", colorSinTi, 2, animar);
            var cSinTi = Ui.Fila(pSinTi, "Cifra", 8, 0, TextAnchor.LowerLeft);
            PiezasDelMuro.Cifra(Ui, cSinTi, sinTi, "0", "", colorSinTi, 40, animar ? 0.15f : -1);
            Ui.Texto(cSinTi, sinTi == 1 ? "ceremonia" : "ceremonias", EstiloTexto.Pequeno, Tema.inkMuted);

            if (actas.Count == 0) {
                PiezasDelMuro.Vacio(Ui, m.Cuerpo, "Todavía no hay actas: se escriben al asistir a una ceremonia o al cerrar el día.", 120);
                return;
            }
            RectTransform lista;
            var scroll = Ui.Desplazable(m.Cuerpo, out lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var rejilla = Ui.Nodo(lista, "Rejilla");
            var g = rejilla.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(440, 190);
            g.spacing = new Vector2(Tema.espacio, Tema.espacio);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            g.childAlignment = TextAnchor.UpperCenter;

            var todas = actas.AsEnumerable().Reverse().ToList();
            var mostrar = _todasLasActas ? todas : todas.Take(12).ToList();
            var n = 0;
            foreach (var a in mostrar) {
                var color = PiezasDelMuro.ColorDeCeremonia(Tema, a.CeremoniaId);
                var tarjeta = PiezasDelMuro.Panel(Ui, rejilla, $"Día {a.Dia} · {a.Nombre}", a.Asistio ? color : Tema.inkFaint, 16);
                if (!a.Asistio) tarjeta.gameObject.AddComponent<CanvasGroup>().alpha = 0.55f;
                if (animar) {
                    var e = tarjeta.gameObject.AddComponent<EntradaDePieza>();
                    e.Retraso = 0.15f + Mathf.Min(8, n++) * 0.05f;
                    e.AlfaFinal = a.Asistio ? 1f : 0.55f;
                }
                var corto = Ui.Columna(tarjeta, "Corto", 6);
                var titulo = Ui.Texto(corto, a.Asistio ? a.Opcion : "Sin ti", EstiloTexto.Encabezado, a.Asistio ? Tema.ink : Tema.inkMuted);
                titulo.maxVisibleLines = 3;
                titulo.overflowMode = TextOverflowModes.Ellipsis;
                if (!string.IsNullOrEmpty(a.Texto)) {
                    Ui.Texto(corto, "pasa el ratón para leerla", EstiloTexto.Leyenda, Tema.inkFaint);
                    var largo = Ui.Texto(tarjeta, a.Texto, EstiloTexto.Pequeno, Tema.ink);
                    largo.maxVisibleLines = 6;
                    largo.overflowMode = TextOverflowModes.Ellipsis;
                    largo.gameObject.SetActive(false);
                    var viva = tarjeta.gameObject.AddComponent<PiezaViva>();
                    viva.Escala = 1.02f;
                    viva.Revelar = largo.gameObject;
                    viva.Ocultar = corto.gameObject;
                }
            }
            if (todas.Count > 12)
                Ui.Boton(lista, _todasLasActas ? "Ver solo las últimas" : $"Ver las {todas.Count}", () => { _todasLasActas = !_todasLasActas; }, VarianteBoton.Fantasma);
        }
    }
}
