using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Corregir
{
    /// <summary>Lo que paso al intentar jugar una carta en un punto.</summary>
    public enum Jugada
    {
        /// <summary>La carta quedo puesta en el punto.</summary>
        Puesta,
        /// <summary>La carta ya esta en otro punto: no se puede repetir.</summary>
        EnUso,
        /// <summary>La carta no existe en la mano (o una pista la quemo).</summary>
        NoDisponible,
        /// <summary>No hay ningun punto elegido.</summary>
        SinPunto
    }

    /// <summary>
    /// El tablero del diagrama con cartas: que carta hay en cada punto. Las cartas son UNICAS: una carta en un punto
    /// no se puede jugar en otro hasta quitarla. «Está bien así» no se gasta. No se guarda en save.json: un reto no
    /// es reanudable a mitad.
    /// </summary>
    public sealed class CorregirState
    {
        public readonly Dictionary<string, string> Asignaciones = new Dictionary<string, string>();
        /// <summary>El segundo de la partida en que se puso cada carta (para la traza).</summary>
        public readonly Dictionary<string, float> Momentos = new Dictionary<string, float>();
        /// <summary>Cartas que una pista quemo: ya no se pueden jugar.</summary>
        public readonly HashSet<string> Descartadas = new HashSet<string>();
        public string PuntoElegido;

        private readonly HashSet<string> _mano;

        public CorregirState(IEnumerable<string> cartas)
        {
            _mano = new HashSet<string>(cartas ?? Enumerable.Empty<string>());
        }

        public string CartaEn(string punto)
        {
            string c;
            return punto != null && Asignaciones.TryGetValue(punto, out c) ? c : null;
        }

        /// <summary>El punto en el que esta la carta (null si esta en la mano, y siempre para «Está bien así»).</summary>
        public string PuntoDe(string carta)
        {
            if (carta == null || carta == PuntosDelDiagrama.EstaBien) return null;
            foreach (var kv in Asignaciones) if (kv.Value == carta) return kv.Key;
            return null;
        }

        public bool EnUso(string carta) { return PuntoDe(carta) != null; }

        /// <summary>
        /// Juega 'carta' en el punto elegido. Si el punto ya tenia otra, esa vuelve a la mano y se devuelve en
        /// 'devuelta'. Jugar en un punto la carta que ya tiene no cambia nada (y cuenta como puesta).
        /// </summary>
        public Jugada Jugar(string carta, float segundo, out string devuelta)
        {
            devuelta = null;
            if (string.IsNullOrEmpty(PuntoElegido)) return Jugada.SinPunto;
            var esEstaBien = carta == PuntosDelDiagrama.EstaBien;
            if (!esEstaBien && (!_mano.Contains(carta ?? "") || Descartadas.Contains(carta))) return Jugada.NoDisponible;
            var donde = PuntoDe(carta);
            if (donde != null && donde != PuntoElegido) return Jugada.EnUso;
            var anterior = CartaEn(PuntoElegido);
            if (anterior != null && anterior != carta && anterior != PuntosDelDiagrama.EstaBien) devuelta = anterior;
            Asignaciones[PuntoElegido] = carta;
            Momentos[PuntoElegido] = segundo;
            return Jugada.Puesta;
        }

        /// <summary>Quita la carta del punto y la devuelve (null si no tenia).</summary>
        public string Quitar(string punto)
        {
            var c = CartaEn(punto);
            if (c == null) return null;
            Asignaciones.Remove(punto);
            Momentos.Remove(punto);
            return c;
        }

        /// <summary>Quema una carta de la mano (pista). No se puede quemar una que ya esta jugada.</summary>
        public bool Descartar(string carta)
        {
            if (!_mano.Contains(carta ?? "") || EnUso(carta)) return false;
            return Descartadas.Add(carta);
        }

        public bool TodoDecidido(IEnumerable<PuntoDelDiagrama> puntos)
        {
            return puntos.All(p => Asignaciones.ContainsKey(p.Id));
        }
    }
}
