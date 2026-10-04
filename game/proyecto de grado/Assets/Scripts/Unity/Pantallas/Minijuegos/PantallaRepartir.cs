using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
using Nexus.Core.Relaciones;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// V2 · Repartir. Una pila por tipo de prueba, todas en fila y todas del mismo alto: el alto de una pila es TODO el
    /// presupuesto, asi que una pila llena significa que todas las horas estan en ese tipo. Se llenan (de un solo
    /// azul muy claro) y se vacian de dos en dos horas con −2 / +2. Las horas sin repartir forman su propia pila, en
    /// mostaza.
    ///
    /// Al entregar: dentro de cada pila, los errores que atrapo; los que se escaparon cruzan hasta el cliente.
    /// Cada tipo solo encuentra los errores de su clase, y cuantos hay de cada uno no se ve: esa es la decision.
    /// </summary>
    public sealed class PantallaRepartir : PantallaDeMinijuego {
        private readonly Dictionary<string, int> _asignacion = new Dictionary<string, int>();
        private Lamina _lamina;

        private RepartirCfg Cfg { get { return Def.Repartir; } }
        private int Paso { get { return Math.Max(1, Cfg.Paso); } }
        private int Usado { get { return _asignacion.Values.Sum(); } }

        /// <summary>Se ve cuantos errores hay de verdad: ayuda de un compañero.</summary>
        private bool Revelado { get { return Pendiente != null && Pendiente.TieneAyuda(TiposDeAyuda.RevelarDefectos); } }

        /// <summary>Lo que atrapa cada pila se ve mientras juegas: modo guiado, o si un compañero te lo chivo.</summary>
        private bool VerAtrapados { get { return Guiado || Revelado; } }

        private readonly Dictionary<string, RectTransform> _mas = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, RectTransform> _tipos = new Dictionary<string, RectTransform>();
        private Button _entregar;

        private Color Relleno { get { return NexusTheme.Alfa(Tema.papelCyan, 0.85f); } }
        private const float AltoPila = 540, AnchoPila = 170, Arriba = 150;

        protected override void ConstruirJuego(RectTransform cuerpo) {
            foreach (var d in Cfg.Depositos) _asignacion[d.Id] = 0;
            _lamina = NuevoLienzo(cuerpo, "Las horas de pruebas · una pila por tipo · súbelas y bájalas con + y −", 1330, 820);
            GuiaView.Registrar("mj.repartir.pilas", _lamina.Raiz);

            RectTransform pie;
            var lado = ColumnaLateral(cuerpo, "Tipos", 420, out pie);
            GuiaView.Registrar("mj.repartir.tipos", lado);
            foreach (var d in Cfg.Depositos) {
                var t = Ui.Tarjeta(lado, $"{d.Nombre} · {d.CostePorDefecto} {Cfg.Unidad} por error");
                _tipos[d.Id] = t;
                Ui.Texto(t, d.Descripcion ?? "", EstiloTexto.Pequeno, Tema.ink);
                if (Revelado)
                    Ui.Texto(t, $"Te lo chivaron: aquí hay {d.DefectosOcultos} error(es).", EstiloTexto.Pequeno, Tema.warning);
                else if (Andamiaje >= 2 && !string.IsNullOrEmpty(d.Pista))
                    Ui.Texto(t, "Pista: " + d.Pista, EstiloTexto.Pequeno, Tema.cyan);
            }
            Ui.Texto(lado, $"Ganas si se escapan como mucho {Cfg.ToleranciaDeEscapes} error(es).", EstiloTexto.Pequeno, Tema.warning);
            _entregar = Ui.Boton(pie, "Entregar el reparto", Entregar, VarianteBoton.Primario);
            GuiaView.Registrar("mj.entregar", _entregar);
            Repintar();
        }

        public override void Repintar() {
            if (_lamina == null || Resultado != null) return;
            Pintar(_lamina, null);
        }

        private void Pintar(Lamina l, List<RepartirEvaluador.Reparto> resultado) {
            for (var i = l.Raiz.childCount - 1; i >= 0; i--)
                if (l.Raiz.GetChild(i).gameObject != l.Dibujo.gameObject) Destroy(l.Raiz.GetChild(i).gameObject);
            l.Dibujo.Limpiar();

            var libres = Cfg.Presupuesto - Usado;
            var n = Cfg.Depositos.Count;
            var sep = 250f;
            var x0 = resultado == null ? 250f : 80f;

            if (resultado == null) {
                l.Texto(60, 16, 800, 34, $"{libres} de {Cfg.Presupuesto} {Cfg.Unidad} sin repartir", 22, libres > 0 ? Tema.papelAviso : Tema.papelExito,
                        TextAlignmentOptions.MidlineLeft, null, true);
                l.Dibujo.Rect(60, Arriba, 110, AltoPila, Tema.papelAviso, 2, null, true, 12);
                var hL = AltoPila * libres / Cfg.Presupuesto;
                if (hL > 0) l.Dibujo.Rect(64, Arriba + AltoPila - hL - 4, 102, hL, Tema.papelAviso, 0, NexusTheme.Alfa(Tema.papelAviso, 0.85f), false, 9);
                l.Texto(40, Arriba + AltoPila + 14, 150, 26, "Sin repartir", 17, Tema.papelAviso, TextAlignmentOptions.Center);
            }

            for (var i = 0; i < n; i++) {
                var d = Cfg.Depositos[i];
                var h = _asignacion[d.Id];
                var x = x0 + i * sep;
                l.Texto(x - 40, Arriba - 76, AnchoPila + 80, 28, d.Nombre, 21, Tema.paperInk, TextAlignmentOptions.Center, null, true);
                l.Texto(x, Arriba - 44, AnchoPila, 26, $"{h} {Cfg.Unidad}", 19, Tema.papelCyan, TextAlignmentOptions.Center);
                l.Dibujo.Rect(x, Arriba, AnchoPila, AltoPila, Tema.papelCyan, 3, Tema.papelCaja, false, 14);
                var hh = AltoPila * h / Cfg.Presupuesto;
                if (hh > 0) {
                    l.Dibujo.Rect(x + 5, Arriba + AltoPila - hh - 5, AnchoPila - 10, hh, Relleno, 0, Relleno, false, 10);
                    for (var k = Paso; k < h; k += Paso) {
                        var y = Arriba + AltoPila - 5 - AltoPila * k / Cfg.Presupuesto;
                        l.Dibujo.Linea(x + 6, y, x + AnchoPila - 6, y, NexusTheme.Alfa(Color.white, 0.35f), 1.5f);
                    }
                }

                if (resultado == null) {
                    var id = d.Id;
                    var menos = Ui.Boton(l.Raiz, "−" + Paso, () => Sumar(id, -Paso));
                    Lamina.Colocar((RectTransform)menos.transform, x + 6, Arriba + AltoPila + 16, 70, 46);
                    menos.interactable = h > 0;
                    var mas = Ui.Boton(l.Raiz, "+" + Paso, () => Sumar(id, Paso));
                    Lamina.Colocar((RectTransform)mas.transform, x + AnchoPila - 76, Arriba + AltoPila + 16, 70, 46);
                    mas.interactable = libres > 0;
                    if (i == 0) GuiaView.Registrar("mj.repartir.mas", mas);
                    _mas[d.Id] = (RectTransform)mas.transform;
                    l.Texto(x - 20, Arriba + AltoPila + 70, AnchoPila + 40, 24, $"{d.CostePorDefecto} {Cfg.Unidad} por error", 15, Tema.paperMuted, TextAlignmentOptions.Center);
                    if (VerAtrapados) {
                        // Guiado: cuantos atrapa ya esta pila, y si ya no queda nada que atrapar en ella.
                        var atrapa = Math.Min(d.DefectosOcultos, h / d.CostePorDefecto);
                        var lleno = atrapa >= d.DefectosOcultos;
                        var texto = lleno ? (h > d.CostePorDefecto * d.DefectosOcultos ? "ya no queda nada: sobran horas" : "atrapa todo lo que hay")
                                          : $"atraparía {atrapa}";
                        l.Texto(x - 20, Arriba + AltoPila + 96, AnchoPila + 40, 24, texto, 15, lleno ? Tema.papelExito : Tema.papelAviso, TextAlignmentOptions.Center);
                    }
                } else {
                    var r = resultado.First(x2 => x2.DepositoId == d.Id);
                    for (var k = 0; k < r.Encontrados; k++)
                        l.Dibujo.Bicho(x + 30 + (k % 4) * 38, Arriba + AltoPila - 26 - (k / 4) * 40, Tema.paperInk);
                    l.Texto(x - 20, Arriba + AltoPila + 14, AnchoPila + 40, 24, $"atrapados {r.Encontrados}", 17, Tema.papelExito, TextAlignmentOptions.Center);
                    if (r.Escapan > 0) {
                        l.Texto(x - 20, Arriba + AltoPila + 40, AnchoPila + 40, 24, $"se escapan {r.Escapan}", 17, Tema.papelPeligro, TextAlignmentOptions.Center);
                        var by = Arriba + 60 + i * 130;
                        l.Dibujo.Curva(new Vector2(x + AnchoPila, Arriba + 30), new Vector2(x + AnchoPila + 120, Arriba - 20), new Vector2(1080, by), Tema.papelPeligro, 2, false, true);
                        for (var k = 0; k < r.Escapan; k++) l.Dibujo.Bicho(1100 + (k % 3) * 40, by + (k / 3) * 40, Tema.papelPeligro);
                    }
                }
            }
            if (resultado != null) {
                l.Dibujo.Rect(1060, Arriba - 40, 230, AltoPila + 80, Tema.papelPeligro, 2, NexusTheme.Alfa(Tema.papelPeligro, 0.16f), false, 12);
                l.Texto(1060, Arriba - 30, 230, 30, "EL CLIENTE", 20, Tema.papelPeligro, TextAlignmentOptions.Center, null, true);
            }
        }

        private void Sumar(string id, int delta) {
            var libres = Cfg.Presupuesto - Usado;
            if (Guiado) {
                // Guiado: solo la pila del paso, y solo hasta las horas que dice Marisol.
                var p = Guia.Actual;
                if (!Guia.Permite(AccionGuiada.Asignar, id) || p == null) { Guia.Rechazar(); return; }
                _asignacion[id] = Mathf.Clamp(_asignacion[id] + Mathf.Min(delta, libres), 0, p.Valor);
                if (_asignacion[id] == p.Valor) Guia.Hecho(AccionGuiada.Asignar, id);
                Repintar();
                return;
            }
            _asignacion[id] = Mathf.Clamp(_asignacion[id] + Mathf.Min(delta, libres), 0, Cfg.Presupuesto);
            Repintar();
        }

        protected override RectTransform PiezaGuiada(PasoGuiado paso) {
            RectTransform rt;
            switch (paso.Accion) {
                case AccionGuiada.Asignar: return _mas.TryGetValue(paso.Objetivo ?? "", out rt) ? rt : null;
                case AccionGuiada.Entregar: return _entregar != null ? (RectTransform)_entregar.transform : null;
                default:
                    if (!string.IsNullOrEmpty(paso.Objetivo) && _tipos.TryGetValue(paso.Objetivo, out rt)) return rt;
                    return _lamina != null ? _lamina.Raiz : null;
            }
        }

        protected override ResultadoMinijuego Evaluar() {
            return RepartirEvaluador.Evaluar(Def, _asignacion);
        }

        protected override void PintarResultado(RectTransform zona) {
            var l = NuevoLienzo(zona, "Lo que atrapó cada pila · y lo que se escapó al cliente", 1330, 820);
            Pintar(l, RepartirEvaluador.Calcular(Cfg, _asignacion));
        }

        protected override IEnumerable<string> SolucionEnTexto() {
            var ganador = RepartirEvaluador.RepartosGanadores(Cfg).OrderBy(g => g.Values.Sum()).FirstOrDefault();
            if (ganador == null) yield break;
            yield return "Un reparto que ganaba: " + string.Join(" · ", Cfg.Depositos.Select(d => $"{d.Nombre} {ganador[d.Id]} {Cfg.Unidad}")) + ".";
            yield return "Había " + string.Join(", ", Cfg.Depositos.Select(d => $"{d.DefectosOcultos} en {d.Nombre.ToLowerInvariant()}")) + ".";
        }
    }
}
