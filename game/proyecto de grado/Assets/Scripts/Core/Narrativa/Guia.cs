using System;
using System.Collections.Generic;

namespace Nexus.Core.Narrativa {
    /// <summary>
    /// Los momentos del juego que pueden abrir un paso de la guia. Es una lista cerrada: un disparador mal
    /// escrito en el JSON no daria error, daria un paso que nunca sale. «dia.N» (dia.1, dia.2…) es el unico
    /// que lleva numero: sale al empezar ese dia, cuando ya ha terminado la escena de la mañana.
    ///
    /// Hay dos clases de disparador:
    ///   · CONTEXTUALES: dicen en que pantalla (o en que momento de la pantalla) esta el jugador. Cuando llega
    ///     uno nuevo, lo que la guia tuviera pendiente de la pantalla anterior ya no sirve y se descarta: asi la
    ///     guia nunca explica la Calidad cuando el jugador ya esta en la Arquitectura.
    ///   · DE EVENTO: algo que pasa (suena un aviso, encuentras algo). Esos esperan su turno en la cola.
    ///
    /// «ayuda.&lt;pantalla&gt;» no se dispara nunca solo: es lo que responde «¿Qué hago ahora?» cuando la pantalla no
    /// tiene pasos propios.
    /// </summary>
    public static class DisparadoresDeGuia {
        private static readonly string[] _contextuales = {
            "fase1.encargo", "fase1.recoleccion", "fase1.metodologia", "fase1.calidad", "fase1.arquitectura", "fase1.resumen",
            "dia.antes", "decision.abierta",
            "minijuego.presentacion", "minijuego.jugando.detectar", "minijuego.jugando.ordenar", "minijuego.jugando.repartir",
            "minijuego.cierre", "planificacion", "retro", "cierre", "prorroga", "resumen", "lanzamiento", "lecciones", "diario"
        };

        private static readonly string[] _deEvento = {
            "mapa.zona", "alerta.suena", "alerta.lejos", "decision.hecha", "oficina.disponible", "oficina.hecha",
            "coleccionable", "conversacion", "ayuda.disponible", "incidencia"
        };

        private static readonly string[] _ayudas = { "ayuda.fase1", "ayuda.dia", "ayuda.minijuego", "ayuda.lecciones" };

        public static IReadOnlyList<string> Fijos {
            get {
                var todos = new List<string>(_contextuales);
                todos.AddRange(_deEvento);
                todos.AddRange(_ayudas);
                return todos;
            }
        }

        public static bool EsValido(string disparador) {
            if (string.IsNullOrEmpty(disparador)) return false;
            if (Array.IndexOf(_contextuales, disparador) >= 0 || Array.IndexOf(_deEvento, disparador) >= 0 ||
                Array.IndexOf(_ayudas, disparador) >= 0) return true;
            return EsDia(disparador);
        }

        /// <summary>«dia.3»: sale al empezar ese dia. Cuenta como contextual: el dia anterior ya paso.</summary>
        public static bool EsDia(string disparador) {
            int n;
            return disparador != null && disparador.StartsWith("dia.", StringComparison.Ordinal) &&
                   int.TryParse(disparador.Substring(4), out n) && n >= 1;
        }

        public static bool EsContextual(string disparador) {
            return disparador != null && (Array.IndexOf(_contextuales, disparador) >= 0 || EsDia(disparador));
        }

        /// <summary>La ayuda generica que responde «¿Qué hago ahora?» en el contexto dado.</summary>
        public static string AyudaDe(string contexto) {
            if (string.IsNullOrEmpty(contexto)) return null;
            if (contexto.StartsWith("fase1.", StringComparison.Ordinal)) return "ayuda.fase1";
            if (contexto.StartsWith("minijuego.", StringComparison.Ordinal)) return "ayuda.minijuego";
            if (contexto == "lecciones" || contexto == "lanzamiento") return "ayuda.lecciones";
            return "ayuda.dia";
        }
    }

    /// <summary>Lo que el jugador tiene que hacer para que un paso se cierre solo («haz clic en el Pasillo»).</summary>
    public static class AccionesDeGuia {
        private static readonly string[] _tipos = { "ir-a-zona", "atender", "decidir", "recoger", "empezar-dia", "cerrar-jornada", "irse", "oficina", "hablar" };

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
    /// para alguien que nunca ha visto ingenieria de software. 'Mas' es la explicacion de verdad, para quien
    /// pulse «Quiero saber más».
    /// </summary>
    public sealed class PasoDeGuia {
        /// <summary>Nivel comodin: el paso vale en todos los niveles (las ayudas genericas).</summary>
        public const string TodosLosNiveles = "*";

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

        /// <summary>La misma burbuja para releerla: sin accion que esperar (ya paso), se cierra con «Entendido».</summary>
        public PasoDeGuia ParaReleer() {
            var c = (PasoDeGuia)MemberwiseClone();
            c.EsperaAccion = null;
            c.PausaElReloj = true;
            return c;
        }
    }

    /// <summary>
    /// Decide que paso sale: el primero sin ver de ese disparador, en este nivel, por orden. Cada paso sale una
    /// vez por perfil (los vistos los guarda el perfil, y se marcan al CERRAR la burbuja, no al abrirla: si el
    /// jugador sale a mitad, lo que no leyo vuelve a salir). Varios pasos con el mismo disparador salen
    /// seguidos, uno detras de otro: asi se explica algo largo en burbujas cortas.
    /// </summary>
    public sealed class GuiaDelTutorial {
        private readonly List<PasoDeGuia> _pasos = new List<PasoDeGuia>();
        private readonly ICollection<string> _vistos;

        public GuiaDelTutorial(IEnumerable<PasoDeGuia> pasos, string nivelId, ICollection<string> vistos) {
            _vistos = vistos ?? new List<string>();
            if (pasos != null)
                foreach (var p in pasos)
                    if (p != null && (p.Nivel == nivelId || p.Nivel == PasoDeGuia.TodosLosNiveles)) _pasos.Add(p);
            _pasos.Sort((a, b) => a.Orden != b.Orden ? a.Orden.CompareTo(b.Orden) : string.CompareOrdinal(a.Id, b.Id));
        }

        /// <summary>Hay guia en este nivel: algun paso que no sea solo la ayuda generica.</summary>
        public bool Activa { get { return _pasos.Count > 0; } }

        /// <summary>El siguiente paso de ese momento, sin marcarlo como visto. null si no queda ninguno.</summary>
        public PasoDeGuia Siguiente(string disparador) {
            foreach (var p in _pasos)
                if (p.Disparador == disparador && !_vistos.Contains(p.Id)) return p;
            return null;
        }

        /// <summary>Todos los pasos de ese momento, vistos o no, en orden: para «¿Qué hago ahora?».</summary>
        public List<PasoDeGuia> Pasos(string disparador) {
            var lista = new List<PasoDeGuia>();
            foreach (var p in _pasos)
                if (p.Disparador == disparador) lista.Add(p);
            return lista;
        }

        public void MarcarVisto(PasoDeGuia paso) {
            if (paso != null && !_vistos.Contains(paso.Id)) _vistos.Add(paso.Id);
        }

        public bool Visto(PasoDeGuia paso) { return paso != null && _vistos.Contains(paso.Id); }

        public int Pendientes {
            get {
                var n = 0;
                foreach (var p in _pasos) if (!_vistos.Contains(p.Id)) n++;
                return n;
            }
        }
    }
}
