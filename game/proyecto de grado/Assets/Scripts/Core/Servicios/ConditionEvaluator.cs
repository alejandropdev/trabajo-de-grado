using System;
using System.Collections.Generic;
using System.Globalization;

namespace Nexus.Core.Servicios {
    /// <summary>Una precondicion del catalogo que no se puede leer. La traduce SchemaValidator al cargar (INV-5).</summary>
    public sealed class ExpresionInvalidaException : Exception {
        public string Expresion { get; private set; }
        public int Posicion { get; private set; }

        public ExpresionInvalidaException(string mensaje, string expresion, int posicion)
            : base($"No se pudo leer la expresion \"{expresion}\": {mensaje} (posicion {posicion}).") {
            Expresion = expresion;
            Posicion = posicion;
        }
    }

    /// <summary>
    /// C10 · El lector de precondiciones (§4.4.6). Descenso recursivo sobre esta gramatica:
    ///
    ///   comparacion    := aditiva (('&gt;'|'&lt;'|'&gt;='|'&lt;='|'=='|'!=') aditiva)?
    ///   aditiva        := multiplicativa (('+'|'-') multiplicativa)*
    ///   multiplicativa := unaria (('*'|'/') unaria)*
    ///   unaria         := ('-'|'+') unaria | primaria
    ///   primaria       := numero | identificador ['(' cadena ')'] | '(' comparacion ')'
    ///
    /// SIN eval dinamico y sin dependencias, y eso no es purismo: los JSON de contenido son datos que
    /// alguien editara a mano, y un dato nunca debe poder ejecutar codigo arbitrario. Esta gramatica
    /// puede leer numeros y comparar; no puede llamar a nada que no este en IStateContext.
    /// </summary>
    public static class ConditionEvaluator {
        /// <summary>Tolerancia de igualdad. '==' sobre dobles calculados nunca debe compararse exacto.</summary>
        public const double Epsilon = 1e-9;

        /// <summary>Todas deben cumplirse. Una lista vacia o nula pasa: "sin precondiciones" no es "imposible".</summary>
        public static bool EvaluarTodas(IEnumerable<string> condiciones, IStateContext ctx) {
            if (condiciones == null) return true;
            foreach (var condicion in condiciones)
                if (!Evaluar(condicion, ctx)) return false;
            return true;
        }

        /// <summary>Una expresion sin comparacion se lee como "distinto de cero".</summary>
        public static bool Evaluar(string expresion, IStateContext ctx) {
            if (string.IsNullOrEmpty(expresion) || expresion.Trim().Length == 0) return true;
            return Math.Abs(EvaluarNumerico(expresion, ctx)) > Epsilon;
        }

        public static double EvaluarNumerico(string expresion, IStateContext ctx) {
            if (expresion == null) throw new ArgumentNullException(nameof(expresion));
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            return new Analizador(expresion, ctx).Analizar();
        }

        private sealed class Analizador {
            private readonly string _t;
            private readonly IStateContext _ctx;
            private int _i;

            public Analizador(string texto, IStateContext ctx) {
                _t = texto;
                _ctx = ctx;
                _i = 0;
            }

            public double Analizar() {
                var v = Comparacion();
                Espacios();
                if (_i < _t.Length) Error("sobra texto a partir de aqui");
                return v;
            }

            // --- gramatica ---

            private double Comparacion() {
                var izq = Aditiva();
                Espacios();
                var op = OperadorDeComparacion();
                if (op == null) return izq;

                var der = Aditiva();
                switch (op) {
                    case ">": return izq > der ? 1.0 : 0.0;
                    case "<": return izq < der ? 1.0 : 0.0;
                    case ">=": return izq >= der ? 1.0 : 0.0;
                    case "<=": return izq <= der ? 1.0 : 0.0;
                    case "==": return Math.Abs(izq - der) <= Epsilon ? 1.0 : 0.0;
                    default: return Math.Abs(izq - der) > Epsilon ? 1.0 : 0.0;   // "!="
                }
            }

            private double Aditiva() {
                var v = Multiplicativa();
                while (true) {
                    Espacios();
                    var c = Actual();
                    if (c == '+') { _i++; v += Multiplicativa(); } else if (c == '-') { _i++; v -= Multiplicativa(); } else return v;
                }
            }

            private double Multiplicativa() {
                var v = Unaria();
                while (true) {
                    Espacios();
                    var c = Actual();
                    if (c == '*') {
                        _i++;
                        v *= Unaria();
                    } else if (c == '/') {
                        _i++;
                        var divisor = Unaria();
                        // Decision deliberada: dividir por cero da 0, no excepcion. Una precondicion mal
                        // escrita no debe poder tumbar el juego en el dia 14 de una partida de laboratorio.
                        v = Math.Abs(divisor) <= Epsilon ? 0.0 : v / divisor;
                    } else {
                        return v;
                    }
                }
            }

            private double Unaria() {
                Espacios();
                var c = Actual();
                if (c == '-') { _i++; return -Unaria(); }
                if (c == '+') { _i++; return Unaria(); }
                return Primaria();
            }

            private double Primaria() {
                Espacios();
                if (_i >= _t.Length) { Error("se esperaba un numero, una variable o un parentesis"); return 0.0; }

                var c = Actual();

                if (c == '(') {
                    _i++;
                    var v = Comparacion();
                    Espacios();
                    if (Actual() != ')') Error("falta el parentesis de cierre");
                    _i++;
                    return v;
                }

                if (char.IsDigit(c) || c == '.') return Numero();
                if (char.IsLetter(c) || c == '_') return Identificador();

                Error($"caracter inesperado '{c}'");
                return 0.0;
            }

            private double Numero() {
                var inicio = _i;
                while (_i < _t.Length && (char.IsDigit(_t[_i]) || _t[_i] == '.')) _i++;
                var texto = _t.Substring(inicio, _i - inicio);

                double v;
                // Invariante siempre: si esto dependiera de la cultura del sistema operativo, "0.85"
                // se leeria como 85 en una maquina con coma decimal y el balanceo cambiaria de idioma.
                if (!double.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out v))
                    Error($"'{texto}' no es un numero valido");
                return v;
            }

            private double Identificador() {
                var inicio = _i;
                while (_i < _t.Length && (char.IsLetterOrDigit(_t[_i]) || _t[_i] == '_')) _i++;
                var nombre = _t.Substring(inicio, _i - inicio);

                Espacios();
                if (Actual() == '(') {
                    _i++;
                    var argumento = Cadena(nombre);
                    Espacios();
                    if (Actual() != ')') Error($"falta el parentesis de cierre de {nombre}(...)");
                    _i++;
                    return _ctx.CallFunction(nombre, argumento);
                }

                double v;
                if (_ctx.TryGetValue(nombre, out v)) return v;

                // A proposito NO devuelve 0: una variable mal escrita es un bug de contenido, y un bug de
                // contenido se caza al cargar (INV-5), no se disimula durante toda la partida.
                Error($"'{nombre}' no es una variable conocida del estado");
                return 0.0;
            }

            private string Cadena(string nombreDeLaFuncion) {
                Espacios();
                var comilla = Actual();
                if (comilla != '\'' && comilla != '"')
                    Error($"el argumento de {nombreDeLaFuncion}(...) debe ir entre comillas, " +
                          "por ejemplo diasDesde('EV-TEC-02')");

                _i++;
                var inicio = _i;
                while (_i < _t.Length && _t[_i] != comilla) _i++;
                if (_i >= _t.Length) Error("falta la comilla de cierre del argumento");

                var texto = _t.Substring(inicio, _i - inicio);
                _i++;
                return texto;
            }

            // --- utilidades del analizador ---

            private string OperadorDeComparacion() {
                if (_i >= _t.Length) return null;

                var c = _t[_i];
                var siguiente = _i + 1 < _t.Length ? _t[_i + 1] : '\0';

                if (c == '>') { _i++; if (siguiente == '=') { _i++; return ">="; } return ">"; }
                if (c == '<') { _i++; if (siguiente == '=') { _i++; return "<="; } return "<"; }
                if (c == '=') {
                    if (siguiente == '=') { _i += 2; return "=="; }
                    Error("usa '==' para comparar, no '='");
                }
                if (c == '!') {
                    if (siguiente == '=') { _i += 2; return "!="; }
                    Error("'!' solo es valido dentro de '!='");
                }
                return null;
            }

            private char Actual() {
                return _i < _t.Length ? _t[_i] : '\0';
            }

            private void Espacios() {
                while (_i < _t.Length && char.IsWhiteSpace(_t[_i])) _i++;
            }

            private void Error(string mensaje) {
                throw new ExpresionInvalidaException(mensaje, _t, _i);
            }
        }
    }
}
