using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

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
        private int Paso { get { return Cfg.Presupuesto >= 20 ? 2 : 1; } }
        private int Usado { get { return _asignacion.Values.Sum(); } }

        private static readonly Color Relleno = new Color32(0xBF, 0xEF, 0xFF, 0xFF);
        private const float AltoPila = 540, AnchoPila = 170, Arriba = 150;

        protected override void ConstruirJuego(RectTransform cuerpo) {
            foreach (var d in Cfg.Depositos) _asignacion[d.Id] = 0;
            _lamina = NuevoLienzo(cuerpo, "Las horas de pruebas · una pila por tipo · se llena con las horas que le des", 1330, 820);

            var lado = Ui.Columna(cuerpo, "Tipos", Tema.espacio);
            UiKit.Tamano(lado, ancho: 460, flexAlto: 1);
            foreach (var d in Cfg.Depositos) {
                var t = Ui.Tarjeta(lado, d.Nombre);
                Ui.Texto(t, d.Descripcion ?? "", EstiloTexto.Pequeno, Tema.texto);
            }
            Ui.Resorte(lado);
            Ui.Boton(lado, "Entregar el reparto", Entregar, VarianteBoton.Primario);
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
                l.Texto(60, 16, 800, 34, $"{libres} de {Cfg.Presupuesto} {Cfg.Unidad} sin repartir", 22, libres > 0 ? Tema.amarillo : Tema.cian,
                        TextAlignmentOptions.MidlineLeft, null, true);
                l.Dibujo.Rect(60, Arriba, 110, AltoPila, Tema.amarillo, 2, null, true, 12);
                var hL = AltoPila * libres / Cfg.Presupuesto;
                if (hL > 0) l.Dibujo.Rect(64, Arriba + AltoPila - hL - 4, 102, hL, Tema.amarillo, 0, new Color(0.95f, 0.76f, 0.31f, 0.85f), false, 9);
                l.Texto(40, Arriba + AltoPila + 14, 150, 26, "Sin repartir", 17, Tema.amarillo, TextAlignmentOptions.Center);
            }

            for (var i = 0; i < n; i++) {
                var d = Cfg.Depositos[i];
                var h = _asignacion[d.Id];
                var x = x0 + i * sep;
                l.Texto(x - 40, Arriba - 76, AnchoPila + 80, 28, d.Nombre, 21, Tema.texto, TextAlignmentOptions.Center, null, true);
                l.Texto(x, Arriba - 44, AnchoPila, 26, $"{h} {Cfg.Unidad}", 19, Tema.cianClaro, TextAlignmentOptions.Center);
                l.Dibujo.Rect(x, Arriba, AnchoPila, AltoPila, Tema.cian, 3, new Color32(0x1A, 0x2D, 0x33, 0xFF), false, 14);
                var hh = AltoPila * h / Cfg.Presupuesto;
                if (hh > 0) {
                    l.Dibujo.Rect(x + 5, Arriba + AltoPila - hh - 5, AnchoPila - 10, hh, Relleno, 0, Relleno, false, 10);
                    for (var k = Paso; k < h; k += Paso) {
                        var y = Arriba + AltoPila - 5 - AltoPila * k / Cfg.Presupuesto;
                        l.Dibujo.Linea(x + 6, y, x + AnchoPila - 6, y, new Color32(0x9F, 0xDC, 0xEF, 0xFF), 1.5f);
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
                    l.Texto(x - 20, Arriba + AltoPila + 70, AnchoPila + 40, 24, $"{d.CostePorDefecto} {Cfg.Unidad} por error", 15, Tema.textoTenue, TextAlignmentOptions.Center);
                } else {
                    var r = resultado.First(x2 => x2.DepositoId == d.Id);
                    for (var k = 0; k < r.Encontrados; k++)
                        l.Dibujo.Bicho(x + 30 + (k % 4) * 38, Arriba + AltoPila - 26 - (k / 4) * 40, Tema.fondo);
                    l.Texto(x - 20, Arriba + AltoPila + 14, AnchoPila + 40, 24, $"atrapados {r.Encontrados}", 17, Tema.cianClaro, TextAlignmentOptions.Center);
                    if (r.Escapan > 0) {
                        l.Texto(x - 20, Arriba + AltoPila + 40, AnchoPila + 40, 24, $"se escapan {r.Escapan}", 17, Tema.rojo, TextAlignmentOptions.Center);
                        var by = Arriba + 60 + i * 130;
                        l.Dibujo.Curva(new Vector2(x + AnchoPila, Arriba + 30), new Vector2(x + AnchoPila + 120, Arriba - 20), new Vector2(1080, by), Tema.rojo, 2, false, true);
                        for (var k = 0; k < r.Escapan; k++) l.Dibujo.Bicho(1100 + (k % 3) * 40, by + (k / 3) * 40, Tema.rojo);
                    }
                }
            }
            if (resultado != null) {
                l.Dibujo.Rect(1060, Arriba - 40, 230, AltoPila + 80, Tema.rojo, 2, new Color(0.79f, 0.31f, 0.24f, 0.14f), false, 12);
                l.Texto(1060, Arriba - 30, 230, 30, "EL CLIENTE", 20, Tema.rojo, TextAlignmentOptions.Center, null, true);
            }
        }

        private void Sumar(string id, int delta) {
            var libres = Cfg.Presupuesto - Usado;
            _asignacion[id] = Mathf.Clamp(_asignacion[id] + Mathf.Min(delta, libres), 0, Cfg.Presupuesto);
            Repintar();
        }

        protected override ResultadoMinijuego Evaluar() {
            return RepartirEvaluador.Evaluar(Def, _asignacion);
        }

        protected override void PintarResultado(RectTransform zona) {
            var l = NuevoLienzo(zona, "Lo que atrapó cada pila · y lo que se escapó al cliente", 1330, 820);
            Pintar(l, RepartirEvaluador.Calcular(Cfg, _asignacion));
        }
    }
}
