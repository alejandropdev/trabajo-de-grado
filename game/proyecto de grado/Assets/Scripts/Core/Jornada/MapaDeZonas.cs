using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nexus.Core.Jornada {
    /// <summary>
    /// C11 · Lo que hace falta para que una zona esté abierta (§3.6).
    ///
    /// El mapa crece a medida que el jugador asciende de casta, y eso **hace visible la progresión
    /// sin una sola barra**: el día que te dejan entrar a la sala de arquitectura, lo notas porque
    /// aparece en el plano.
    ///
    /// Esta clase solo DECLARA la condición. Quien la evalúa es GameSession, que es quien tiene el
    /// estado y los beats: así Core.Jornada no necesita conocer ni el evaluador ni el canal narrativo.
    /// </summary>
    public sealed class PuertaDeZona {
        /// <summary>Expresiones evaluadas contra IStateContext. Todas deben cumplirse.</summary>
        public List<string> Precondiciones = new List<string>();

        /// <summary>Id de un beat que debe haber salido ya. La cocina se abre «tras CIN-2.3», por ejemplo.</summary>
        public string TrasBeat;

        public bool SiempreAbierta {
            get { return (Precondiciones == null || Precondiciones.Count == 0) && string.IsNullOrEmpty(TrasBeat); }
        }

        public PuertaDeZona Clone() {
            var c = (PuertaDeZona)MemberwiseClone();
            c.Precondiciones = Precondiciones == null ? null : new List<string>(Precondiciones);
            return c;
        }
    }

    /// <summary>
    /// C11 · Un sitio al que ir durante la jornada (§3.6).
    ///
    /// La regla de diseño: **cada zona da algo que no está en ninguna otra** — una persona, un
    /// artefacto, un rumor, un coleccionable. Si dos zonas dan lo mismo, una de las dos sobra.
    ///
    /// Y la que cambia el juego: *«Javier ya no es un turno, es un sitio al que ir.»* Deja de ser una
    /// parada obligatoria de las 09:00; ahora hay que bajar al Data Hub a buscarlo, y **la
    /// conversación entera se puede perder**. El jugador que no va, no se entera.
    /// </summary>
    public sealed class ZonaDeNivel {
        public string Id;
        public string Nombre;

        /// <summary>Lo que se ve al llegar. Una línea, para la pantalla.</summary>
        public string Descripcion;

        /// <summary>
        /// ★ **Tu escritorio es siempre el ancla:** es donde llegan las alertas y donde hay que volver
        /// a atenderlas. Todo nivel con mapa tiene exactamente una, y el validador lo exige.
        /// </summary>
        public bool EsAncla;

        /// <summary>Lo que cuesta la micro-escena de estar ahí, además del desplazamiento.</summary>
        public int MinutosDeVisita = 30;

        public PuertaDeZona Puerta = new PuertaDeZona();

        /// <summary>Quién se encuentra aquí. Puede no estar a todas horas.</summary>
        public List<string> QuienEsta = new List<string>();

        /// <summary>Ids de coleccionables escondidos aquí.</summary>
        public List<string> Coleccionables = new List<string>();

        /// <summary>«El equipo, la moral real, rumores de asignación». Para el plano y para la ficha.</summary>
        public string QueDa;

        public ZonaDeNivel Clone() {
            var c = (ZonaDeNivel)MemberwiseClone();
            c.Puerta = Puerta == null ? null : Puerta.Clone();
            c.QuienEsta = QuienEsta == null ? null : new List<string>(QuienEsta);
            c.Coleccionables = Coleccionables == null ? null : new List<string>(Coleccionables);
            return c;
        }
    }

    /// <summary>
    /// C11 · El mapa recorrible de un nivel (§3.6).
    ///
    /// **No confundir con las zonas A–F de la Fase 1.** Aquéllas son la recolección con reloj de dos
    /// horas y no cambian nunca; éste es un mapa **propio de cada nivel**, de 3 a 6 zonas, que se
    /// desbloquean por casta o por avance narrativo. El Documento Maestro marca esa distinción con
    /// énfasis, y confundirlas rompe las dos mecánicas a la vez.
    ///
    /// Un nivel puede no tener mapa: entonces no hay exploración y todo ocurre en el escritorio.
    /// Es lo que necesita un prólogo, y lo que permite que el motor funcione sin contenido todavía.
    /// </summary>
    public sealed class MapaDeZonas {
        /// <summary>Separador de las claves de coste: "escritorio>data-hub".</summary>
        public const char Separador = '>';

        public List<ZonaDeNivel> Zonas = new List<ZonaDeNivel>();

        /// <summary>Lo que cuesta ir de una zona a otra si nadie dijo otra cosa.</summary>
        public int CosteBaseDeViaje = 20;

        /// <summary>
        /// Los pares que NO cuestan lo de siempre: "escritorio&gt;data-hub": 45.
        /// Se busca en las dos direcciones, así que basta declarar cada par una vez.
        /// </summary>
        public Dictionary<string, int> Costes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public bool Vacio { get { return Zonas == null || Zonas.Count == 0; } }

        /// <summary>El escritorio. Null si el nivel no tiene mapa.</summary>
        public ZonaDeNivel Ancla {
            get {
                if (Zonas == null) return null;
                foreach (var z in Zonas)
                    if (z != null && z.EsAncla) return z;
                return null;
            }
        }

        public ZonaDeNivel PorId(string id) {
            if (Zonas == null || string.IsNullOrEmpty(id)) return null;
            foreach (var z in Zonas)
                if (z != null && string.Equals(z.Id, id, StringComparison.OrdinalIgnoreCase)) return z;
            return null;
        }

        /// <summary>
        /// Minutos de ir de una zona a otra. Quedarse donde estás es gratis; lo demás cuesta lo que
        /// diga el par, o el coste base.
        /// </summary>
        public int CosteEntre(string desde, string hasta) {
            if (string.Equals(desde, hasta, StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.IsNullOrEmpty(desde) || string.IsNullOrEmpty(hasta)) return CosteBaseDeViaje;

            int coste;
            if (Costes != null &&
                (Costes.TryGetValue(desde + Separador + hasta, out coste) ||
                 Costes.TryGetValue(hasta + Separador + desde, out coste)))
                return coste;

            return CosteBaseDeViaje;
        }

        /// <summary>Lo que cuesta ir Y estar: el desplazamiento más la micro-escena.</summary>
        public int CosteDeVisitar(string desde, string hasta) {
            var zona = PorId(hasta);
            return CosteEntre(desde, hasta) + (zona == null ? 0 : Math.Max(0, zona.MinutosDeVisita));
        }

        /// <summary>
        /// El recorrido MÁS BARATO que sale del ancla, visita todas las zonas y vuelve al ancla.
        /// Fuerza bruta sobre las permutaciones: con 3–6 zonas son como mucho 120 combinaciones.
        ///
        /// Existe para que la regla de diseño *«recorrerlas todas en un día es imposible»* (§3.6) deje
        /// de ser una intención y pase a ser un número que se puede mirar. **El motor no la impone**:
        /// es una propiedad de los costes que escriba el contenido, y esto es lo que permite
        /// comprobarlo al balancear. Si el resultado cabe en la jornada, el mapa está barato.
        /// </summary>
        public int MinutosParaRecorrerloTodo() {
            var ancla = Ancla;
            if (ancla == null) return 0;

            var otras = new List<ZonaDeNivel>();
            foreach (var z in Zonas)
                if (z != null && !z.EsAncla) otras.Add(z);

            if (otras.Count == 0) return 0;

            var mejor = int.MaxValue;
            Permutar(otras, 0, ancla.Id, 0, ref mejor);
            return mejor;
        }

        private void Permutar(List<ZonaDeNivel> pendientes, int desde, string zonaActual, int acumulado,
                              ref int mejor) {
            if (acumulado >= mejor) return;   // poda

            if (desde == pendientes.Count) {
                var total = acumulado + CosteEntre(zonaActual, Ancla.Id);
                if (total < mejor) mejor = total;
                return;
            }

            for (var i = desde; i < pendientes.Count; i++) {
                var tmp = pendientes[desde]; pendientes[desde] = pendientes[i]; pendientes[i] = tmp;

                var siguiente = pendientes[desde];
                Permutar(pendientes, desde + 1, siguiente.Id,
                         acumulado + CosteDeVisitar(zonaActual, siguiente.Id), ref mejor);

                tmp = pendientes[desde]; pendientes[desde] = pendientes[i]; pendientes[i] = tmp;
            }
        }

        public MapaDeZonas Clone() {
            var c = (MapaDeZonas)MemberwiseClone();
            c.Costes = Costes == null
                ? null
                : new Dictionary<string, int>(Costes, StringComparer.OrdinalIgnoreCase);

            if (Zonas != null) {
                c.Zonas = new List<ZonaDeNivel>(Zonas.Count);
                foreach (var z in Zonas) c.Zonas.Add(z == null ? null : z.Clone());
            }
            return c;
        }

        [OnDeserialized]
        internal void TrasCargar(StreamingContext _) {
            if (Costes == null || ReferenceEquals(Costes.Comparer, StringComparer.OrdinalIgnoreCase)) return;
            Costes = new Dictionary<string, int>(Costes, StringComparer.OrdinalIgnoreCase);
        }
    }
}
