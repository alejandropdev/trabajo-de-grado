using System;
using System.Collections.Generic;

namespace Nexus.Core.Narrativa {
    /// <summary>
    /// Los momentos del juego que pueden abrir un paso de la guia. Es una lista cerrada: un disparador mal
    /// escrito en el JSON no daria error, daria un paso que nunca sale. «dia.N» (dia.1, dia.2…) es el unico
    /// que lleva numero: sale al empezar ese dia, cuando ya ha terminado la escena de la mañana.
    /// </summary>
    public static class DisparadoresDeGuia {
        private static readonly string[] _fijos = {
            "fase1.encargo", "fase1.recoleccion", "fase1.metodologia", "fase1.calidad", "fase1.arquitectura", "fase1.resumen",
            "dia.antes", "mapa.zona", "alerta.suena", "alerta.lejos", "decision.abierta", "decision.hecha",
            "minijuego.presentacion", "minijuego.jugando", "minijuego.cierre",
            "oficina.disponible", "oficina.hecha", "coleccionable", "planificacion", "retro",
            "cierre", "prorroga", "resumen", "lanzamiento", "lecciones", "diario"
        };

        public static IReadOnlyList<string> Fijos { get { return _fijos; } }

        public static bool EsValido(string disparador) {
            if (string.IsNullOrEmpty(disparador)) return false;
            foreach (var d in _fijos) if (d == disparador) return true;
            int n;
            return disparador.StartsWith("dia.", StringComparison.Ordinal) &&
                   int.TryParse(disparador.Substring(4), out n) && n >= 1;
        }
    }

    /// <summary>Lo que el jugador tiene que hacer para que un paso se cierre solo («haz clic en el Pasillo»).</summary>
    public static class AccionesDeGuia {
        private static readonly string[] _tipos = { "ir-a-zona", "atender", "decidir", "recoger", "empezar-dia", "cerrar-jornada", "irse", "oficina" };

        public static bool EsValida(string accion) {
            if (string.IsNullOrEmpty(accion)) return true;   // sin accion: el paso se cierra con «Entendido»
            var tipo = accion.Split(':')[0];
            foreach (var t in _tipos) if (t == tipo) return true;
            return false;
        }

        /// <summary>'hecha' cumple lo que 'esperada' pide. «ir-a-zona» sin zona vale para cualquier zona.</summary>
        public static bool Cumple(string esperada, string hecha) {
            if (string.IsNullOrEmpty(esperada) || string.IsNullOrEmpty(hecha)) return false;
            if (esperada == hecha) return true;
            return esperada.IndexOf(':') < 0 && hecha.StartsWith(esperada + ":", StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Un paso de la guia del tutorial: una burbuja de Marisol que explica QUE hacer y POR QUE, en palabras
    /// para alguien de diez años. 'Mas' es la explicacion de verdad, para quien pulse «Quiero saber más».
    /// </summary>
    public sealed class PasoDeGuia {
        public string Id;
        public string Nivel;
        public string Disparador;
        public int Orden;
        public string Quien = "Marisol Andrade";
        public string Titulo;
        public string Texto;
        public string Mas;
        /// <summary>La pieza de la pantalla que se señala (la registran las pantallas: «mapa.pasillo», «dia.reloj»…).</summary>
        public string Resaltar;
        /// <summary>Si la tiene, el paso no se cierra con un boton: se cierra cuando el jugador lo hace.</summary>
        public string EsperaAccion;
        /// <summary>Si el reloj espera mientras se lee. Un paso que pide moverse no puede pararlo: no dejaria moverse.</summary>
        public bool PausaElReloj = true;
    }

    /// <summary>
    /// Decide que paso sale: el primero sin ver de ese disparador, en este nivel, por orden. Cada paso sale una
    /// vez por perfil (los vistos los guarda el perfil). Varios pasos con el mismo disparador salen seguidos,
    /// uno detras de otro: asi se explica algo largo en burbujas cortas.
    /// </summary>
    public sealed class GuiaDelTutorial {
        private readonly List<PasoDeGuia> _pasos = new List<PasoDeGuia>();
        private readonly ICollection<string> _vistos;

        public GuiaDelTutorial(IEnumerable<PasoDeGuia> pasos, string nivelId, ICollection<string> vistos) {
            _vistos = vistos ?? new List<string>();
            if (pasos != null)
                foreach (var p in pasos)
                    if (p != null && p.Nivel == nivelId) _pasos.Add(p);
            _pasos.Sort((a, b) => a.Orden != b.Orden ? a.Orden.CompareTo(b.Orden) : string.CompareOrdinal(a.Id, b.Id));
        }

        public bool Activa { get { return _pasos.Count > 0; } }

        /// <summary>El siguiente paso de ese momento, sin marcarlo como visto. null si no queda ninguno.</summary>
        public PasoDeGuia Siguiente(string disparador) {
            foreach (var p in _pasos)
                if (p.Disparador == disparador && !_vistos.Contains(p.Id)) return p;
            return null;
        }

        public void MarcarVisto(PasoDeGuia paso) {
            if (paso != null && !_vistos.Contains(paso.Id)) _vistos.Add(paso.Id);
        }

        public int Pendientes {
            get {
                var n = 0;
                foreach (var p in _pasos) if (!_vistos.Contains(p.Id)) n++;
                return n;
            }
        }
    }
}
