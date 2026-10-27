using System;
using System.Collections.Generic;

namespace Nexus.Core.Coleccion {
    /// <summary>Las series que hoy tienen contenido implementado. Las demas estan diseñadas, no escritas.</summary>
    public static class SeriesDeColeccionables {
        public const string Carta = "carta";
        public const string Usb = "usb";
        public const string Codigo = "codigo";
        public const string Startup = "startup";
        public const string Advertencia = "advertencia";
        /// <summary>Notas adhesivas del edificio (los post-its del recorrido 3D): rumores y pistas, no lecciones.</summary>
        public const string Nota = "nota";

        private static readonly string[] _todas = { Carta, Usb, Codigo, Startup, Advertencia, Nota };
        public static IReadOnlyList<string> Todas { get { return _todas; } }

        public static bool EsValida(string serie) {
            foreach (var s in _todas)
                if (string.Equals(s, serie, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>Los cuatro palos de las cartas de desarrollador (§10.1).</summary>
    public static class PalosDeCarta {
        public const string Leyes = "leyes";
        public const string Desastres = "desastres";
        public const string Practicas = "practicas";
        public const string Oficio = "oficio";

        private static readonly string[] _todos = { Leyes, Desastres, Practicas, Oficio };
        public static IReadOnlyList<string> Todos { get { return _todos; } }

        public static bool EsValido(string palo) {
            foreach (var p in _todos)
                if (string.Equals(p, palo, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>Lo dificil que es hacer aparecer un coleccionable del mapa.</summary>
    public static class DificultadesDeColeccionable {
        /// <summary>Una condicion de hora o de dia: basta con volver mas tarde.</summary>
        public const string Facil = "facil";
        /// <summary>Dia y franja, o una accion sencilla (una ceremonia, una conversacion, una tarea).</summary>
        public const string Media = "media";
        /// <summary>Una combinacion, o algo que hay que sostener (quedarse de noche, revisar bien varios cambios).</summary>
        public const string Dificil = "dificil";

        private static readonly string[] _todas = { Facil, Media, Dificil };
        public static IReadOnlyList<string> Todas { get { return _todas; } }

        public static bool EsValida(string d) {
            foreach (var x in _todas)
                if (string.Equals(x, d, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>
    /// Cuando aparece un coleccionable del mapa. Sin esto, esta ahi desde que se entra en la zona. Con esto, solo
    /// cuando se cumplen TODAS sus precondiciones (la misma sintaxis que las de los eventos); mientras tanto, la zona
    /// enseña la pista.
    /// </summary>
    public sealed class AparicionDeColeccionable {
        public string Dificultad = DificultadesDeColeccionable.Facil;
        public List<string> Precondiciones = new List<string>();
        /// <summary>Lo que se ve en la zona mientras no aparece: mas clara cuanto mas facil.</summary>
        public string Pista;
    }

    /// <summary>
    /// Un objeto que el jugador encuentra en una zona del mapa. Un solo tipo para todas las series: los
    /// campos que usa cada una los exige el validador, no el compilador — asi una serie nueva es un
    /// valor nuevo en el JSON, no una clase nueva.
    ///
    /// ★ Nota de produccion del canon, que el validador hace cumplir: las cartas de leyes y desastres y
    /// las anecdotas de startups son hechos reales, y llevan SIEMPRE fuente. Ninguna frase se pone en
    /// boca de una persona real.
    /// </summary>
    public sealed class Coleccionable {
        public string Id;
        public string Serie;
        public string Titulo;

        /// <summary>El cuerpo: el reverso de una carta, lo que paso en una startup, la letra chica de un cartel.</summary>
        public string Texto;

        /// <summary>Donde se encuentra, dicho como lo veria el jugador ("pegado junto a la maquina de cafe").</summary>
        public string Ubicacion;

        // --- cartas ---
        public string Palo;
        public string PreguntaDeAplicacion;

        // --- cartas y startups ---
        public string Fuente;

        // --- startups ---
        public string Nicho;
        public string Causa;

        // --- codigos de terminal ---
        public string Comando;
        public string Revela;

        // --- fragmentos del USB ---
        public int Fragmento;

        /// <summary>Cuando aparece en su zona. null = desde que se entra.</summary>
        public AparicionDeColeccionable Aparicion;

        public bool TieneCondicion { get { return Aparicion != null && Aparicion.Precondiciones != null && Aparicion.Precondiciones.Count > 0; } }
    }
}
