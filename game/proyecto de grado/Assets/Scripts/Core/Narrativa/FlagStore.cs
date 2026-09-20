using System;
using System.Collections.Generic;
using System.Globalization;

namespace Nexus.Core.Narrativa {
    /// <summary>Los cinco ejes del §4.7.2, mas el cajon de contadores y estado.</summary>
    public static class EjesDeFlag {
        public const string Competencia = "competencia";
        public const string Integridad = "integridad";
        public const string Relaciones = "relaciones";
        public const string Poder = "poder";
        public const string Persona = "persona";
        public const string Estado = "estado";

        private static readonly string[] _todos = { Competencia, Integridad, Relaciones, Poder, Persona, Estado };
        public static IReadOnlyList<string> Todos { get { return _todos; } }

        public static bool EsValido(string eje) {
            foreach (var e in _todos)
                if (string.Equals(e, eje, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }

    /// <summary>
    /// C7 · Un flag del censo (§8.3.6). Espejo en C# de flags.json.
    ///
    /// El catalogo existe **aunque el codigo no lo necesitaria**, y la especificacion explica por que:
    /// "el catalogo documenta, y la lista completa en un archivo se revisa; repartida por el codigo, no".
    /// Los cuarenta y cinco flags del juego en una pagina se auditan de un vistazo; escondidos en
    /// cuarenta y cinco sitios distintos, no.
    /// </summary>
    public sealed class DefinicionDeFlag {
        public string Id;
        public string Eje;
        public double Inicial;

        /// <summary>Suelo. Si falta y no es 'sinTecho', el flag no se acota por abajo.</summary>
        public double? Min;

        /// <summary>Techo. Si falta, no se acota por arriba.</summary>
        public double? Max;

        /// <summary>Atajo para los contadores: suelo 0 y sin techo (FLG_HORAS_EXTRA, FLG_EVIDENCIA…).</summary>
        public bool SinTecho;

        /// <summary>Monotono creciente. Hoy solo FLG_DEUDA_MORAL, y el codigo lo impone ademas del catalogo.</summary>
        public bool NoBaja;

        public string Descripcion;
    }

    /// <summary>
    /// C7 · Los flags de la partida (§4.7.1). Lo que los proyectos olvidan y las decisiones no.
    ///
    /// Vive en la PARTIDA, no en el nivel: el WorldState se tira al cerrar cada nivel, esto no.
    /// El diccionario que recibe el constructor es el RESPALDO y se escribe a traves de el, no se copia:
    /// asi SaveGame.Flags esta siempre al dia sin un paso de sincronizacion que alguien pueda olvidar.
    ///
    /// Tres reglas dentro de la clase:
    ///
    /// 1 · Todo flag empieza por FLG_, o ArgumentException. Sin esto, un efecto de evento mal escrito
    ///     acabaria creando un flag fantasma que nadie lee.
    ///
    /// 2 · ★ FLG_DEUDA_MORAL NO BAJA NUNCA. Sumar() ignora cualquier delta negativo sobre el, y Set()
    ///     tampoco lo deja bajar. No es una regla de balanceo: es la tesis del juego. La deuda tecnica
    ///     se paga refactorizando; la deuda moral no se paga, solo se declara o se oculta.
    ///
    /// 3 · Acotacion por catalogo: los flags de eje van de 0 a 100 o en su rango propio, y los
    ///     contadores solo tienen suelo.
    ///
    /// La unica puerta que se salta las reglas es el constructor, y es a proposito: restaurar una
    /// partida guardada tiene que poder reponer cualquier valor, incluido uno que hoy ya no se
    /// podria alcanzar sumando.
    /// </summary>
    public sealed class FlagStore {
        public const string Prefijo = "FLG_";

        /// <summary>El flag que sostiene la tesis. Se nombra aqui ademas de en el catalogo a proposito.</summary>
        public const string DeudaMoral = "FLG_DEUDA_MORAL";

        private readonly Dictionary<string, double> _flags;
        private readonly Dictionary<string, DefinicionDeFlag> _definiciones =
            new Dictionary<string, DefinicionDeFlag>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 'respaldo' se guarda por referencia: es el diccionario de SaveGame.Flags.
        /// 'censo' puede ser null en tests; sin el no hay acotacion, pero las reglas 1 y 2 siguen vigentes.
        /// </summary>
        public FlagStore(Dictionary<string, double> respaldo, IEnumerable<DefinicionDeFlag> censo = null) {
            _flags = respaldo ?? new Dictionary<string, double>(StringComparer.Ordinal);

            if (censo == null) return;
            foreach (var def in censo) {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                _definiciones[def.Id] = def;
            }
        }

        public bool HayCenso { get { return _definiciones.Count > 0; } }

        public IEnumerable<KeyValuePair<string, double>> Todos { get { return _flags; } }

        public bool Tiene(string nombre) {
            return nombre != null && _flags.ContainsKey(nombre);
        }

        /// <summary>Un flag que nunca se escribio vale su inicial del censo, o 0 si no hay censo.</summary>
        public double Get(string nombre) {
            Comprobar(nombre);

            double valor;
            if (_flags.TryGetValue(nombre, out valor)) return valor;

            DefinicionDeFlag def;
            return _definiciones.TryGetValue(nombre, out def) ? def.Inicial : 0.0;
        }

        public void Set(string nombre, double valor) {
            Comprobar(nombre);

            if (double.IsNaN(valor) || double.IsInfinity(valor))
                throw new InvalidOperationException(
                    $"Se intento escribir '{nombre}' con un valor no finito " +
                    $"({valor.ToString(CultureInfo.InvariantCulture)}).");

            if (NoBaja(nombre)) {
                var actual = Get(nombre);
                if (valor < actual) return;   // la deuda moral no se paga
            }

            _flags[nombre] = Acotar(nombre, valor);
        }

        /// <summary>
        /// Suma sobre el valor actual. Sobre FLG_DEUDA_MORAL, un delta negativo se ignora en silencio:
        /// no es un error del que llama, es que esa operacion no existe en este juego.
        /// </summary>
        public void Sumar(string nombre, double delta) {
            Comprobar(nombre);
            if (delta < 0 && NoBaja(nombre)) return;
            Set(nombre, Get(nombre) + delta);
        }

        /// <summary>Escribe los valores iniciales del censo que todavia no existan. Se llama al crear la partida.</summary>
        public void Inicializar() {
            foreach (var kv in _definiciones)
                if (!_flags.ContainsKey(kv.Key)) _flags[kv.Key] = Acotar(kv.Key, kv.Value.Inicial);
        }

        /// <summary>Una copia suelta, para el DebriefReport. Tocarla no altera la partida.</summary>
        public Dictionary<string, double> Copia() {
            return new Dictionary<string, double>(_flags, StringComparer.Ordinal);
        }

        // ------------------------------------------------------------------ interno

        private void Comprobar(string nombre) {
            if (string.IsNullOrEmpty(nombre) || !nombre.StartsWith(Prefijo, StringComparison.Ordinal))
                throw new ArgumentException(
                    $"'{nombre}' no es un flag narrativo: todos empiezan por {Prefijo}.", nameof(nombre));

            if (_definiciones.Count > 0 && !_definiciones.ContainsKey(nombre))
                throw new ArgumentException(
                    $"'{nombre}' no esta en el censo de flags. Añadelo a flags.json o corrige el nombre.",
                    nameof(nombre));
        }

        private bool NoBaja(string nombre) {
            if (string.Equals(nombre, DeudaMoral, StringComparison.Ordinal)) return true;

            DefinicionDeFlag def;
            return _definiciones.TryGetValue(nombre, out def) && def.NoBaja;
        }

        private double Acotar(string nombre, double valor) {
            DefinicionDeFlag def;
            if (!_definiciones.TryGetValue(nombre, out def)) return valor;

            if (def.SinTecho) return valor < 0 ? 0 : valor;
            if (def.Min.HasValue && valor < def.Min.Value) return def.Min.Value;
            if (def.Max.HasValue && valor > def.Max.Value) return def.Max.Value;
            return valor;
        }
    }
}
