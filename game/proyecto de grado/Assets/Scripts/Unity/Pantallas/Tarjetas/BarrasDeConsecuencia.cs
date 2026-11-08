using System;
using System.Collections.Generic;
using Nexus.Core.Modelo;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las barras que enseñan lo que una decision le hace al proyecto SIN una sola frase: al inclinar la tarjeta, cada
    /// barra se mueve hacia donde iria y se tiñe de verde (mejora) o rojo (empeora); al elegir, se queda ahi.
    /// Solo salen las que la tarjeta toca, para no abrumar. Los stocks que no son de 0 a 100 (dinero, dias, alcance,
    /// avance) salen como una cifra con signo.
    /// </summary>
    public sealed class BarrasDeConsecuencia {
        private sealed class Fila {
            public string Stock;
            public BarraView Barra;
            public TMP_Text Cifra;
            public double Actual;
            public float Objetivo;
        }

        // Los que se miden de 0 a 100 y por tanto caben en una barra.
        private static readonly HashSet<string> EnBarra = new HashSet<string>(StringComparer.Ordinal) {
            "DeudaTecnica", "Cobertura", "Documentacion", "MoralEquipo", "Cansancio",
            "Competencia", "SatisfaccionCliente", "Reputacion", "SaludJugador"
        };

        // Cuando SUBIR es malo. El resto, subir es bueno. Dias y Dinero se leen al reves que el avance.
        private static readonly HashSet<string> SubirEsMalo = new HashSet<string>(StringComparer.Ordinal) {
            "DeudaTecnica", "Cansancio", "Dias"
        };

        private readonly UiKit _ui;
        private readonly NexusTheme _tema;
        private readonly RectTransform _contenedor;
        private readonly List<Fila> _filas = new List<Fila>();
        private Dictionary<string, double> _previsto = new Dictionary<string, double>();

        public BarrasDeConsecuencia(UiKit ui, Transform padre) {
            _ui = ui;
            _tema = ui.Tema;
            _contenedor = ui.Fila(padre, "Barras", _tema.espacio);
        }

        /// <summary>Las barras de estos stocks, con el valor que tienen ahora.</summary>
        public void Preparar(WorldState w, IEnumerable<string> stocks) {
            UiKit.Vaciar(_contenedor);
            _filas.Clear();
            _previsto = new Dictionary<string, double>();
            foreach (var stock in stocks) {
                var fila = new Fila { Stock = stock };
                double actual;
                w.TryGet(stock, out actual);
                fila.Actual = actual;

                var columna = _ui.Columna(_contenedor, "Barra " + stock, 2);
                UiKit.Tamano(columna, ancho: 0, flexAncho: 1);
                _ui.Texto(columna, Nombre(stock), EstiloTexto.Leyenda, _tema.inkMuted);
                if (EnBarra.Contains(stock)) {
                    fila.Barra = _ui.Barra(columna, (float)(actual / 100.0), _tema.cyan);
                    fila.Objetivo = (float)(actual / 100.0);
                } else {
                    fila.Cifra = _ui.Texto(columna, Formato(actual), EstiloTexto.Encabezado, _tema.ink);
                }
                _filas.Add(fila);
            }
        }

        /// <summary>Enseña la consecuencia de una respuesta (los deltas de EffectApplier.Previsualizar). null = ninguna.</summary>
        public void Previsualizar(Dictionary<string, double> deltas) {
            _previsto = deltas ?? new Dictionary<string, double>();
            foreach (var fila in _filas) {
                double delta;
                _previsto.TryGetValue(fila.Stock, out delta);
                var color = Mathf.Abs((float)delta) < 1e-6f ? _tema.cyan
                    : (delta > 0) == !SubirEsMalo.Contains(fila.Stock) ? _tema.success : _tema.danger;

                if (fila.Barra != null) {
                    fila.Objetivo = Mathf.Clamp01((float)((fila.Actual + delta) / 100.0));
                    fila.Barra.Color = color;
                } else if (fila.Cifra != null) {
                    fila.Cifra.text = Mathf.Abs((float)delta) < 1e-6f ? Formato(fila.Actual) : Formato(fila.Actual + delta) + "  (" + Signo(delta) + ")";
                    fila.Cifra.color = Mathf.Abs((float)delta) < 1e-6f ? _tema.ink : color;
                }
            }
        }

        /// <summary>Se llama cada fotograma: la barra se desliza hacia su objetivo en vez de saltar.</summary>
        public void Animar(float dt) {
            foreach (var fila in _filas) {
                if (fila.Barra == null) continue;
                fila.Barra.Valor = Mathf.MoveTowards(fila.Barra.Valor, fila.Objetivo, dt * 3f);
            }
        }

        /// <summary>Despues de elegir: lo previsto pasa a ser lo actual.</summary>
        public void Confirmar(WorldState w) {
            foreach (var fila in _filas) {
                double actual;
                w.TryGet(fila.Stock, out actual);
                fila.Actual = actual;
                if (fila.Barra != null) { fila.Objetivo = (float)(actual / 100.0); fila.Barra.Color = _tema.cyan; }
                if (fila.Cifra != null) { fila.Cifra.text = Formato(actual); fila.Cifra.color = _tema.ink; }
            }
            _previsto = new Dictionary<string, double>();
        }

        private static string Nombre(string stock) { return Textos.NombreDeStock(stock); }
        private static string Formato(double v) { return Math.Round(v, 1).ToString("0.#"); }
        private static string Signo(double d) { return (d > 0 ? "+" : "") + Math.Round(d, 1).ToString("0.#"); }
    }
}
