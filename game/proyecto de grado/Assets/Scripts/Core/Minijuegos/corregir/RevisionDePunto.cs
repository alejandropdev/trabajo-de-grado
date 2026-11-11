using System.Linq;

namespace Nexus.Core.Minijuegos.Corregir
{
    /// <summary>
    /// Lo que el cierre cuenta de UN punto, sin interfaz: si salio bien, que carta se puso y cual tocaba, un titular y
    /// una definicion de una linea (para el globo, que casi no lleva texto), y la explicacion completa para quien
    /// quiera leerla. Funcion pura: (definicion, punto, carta puesta) -> revision.
    /// </summary>
    public sealed class RevisionDePunto
    {
        public PuntoDelDiagrama Punto;
        public bool Acierta;
        /// <summary>La carta puesta (o «esta_bien»); null = se quedo sin decidir.</summary>
        public string CartaJugada;
        /// <summary>La que tocaba: la primera solucion de la zona, o «esta_bien» en un señuelo.</summary>
        public string CartaCorrecta;
        /// <summary>«Acoplamiento indebido», o «Estaba bien»: el nombre de lo que habia en ese punto.</summary>
        public string Titular;
        /// <summary>Una linea: que quiere decir ese defecto, o por que el señuelo no lo era.</summary>
        public string Definicion;
        public string ExplicacionCompleta;
        /// <summary>Por que la carta puesta no servia (null si acerto, no habia carta, o no lo dice).</summary>
        public string PorQueNo;

        public const int MaximoDeDefinicion = 110;

        public static RevisionDePunto Para(MinijuegoDef def, PuntoDelDiagrama p, string cartaJugada)
        {
            var r = new RevisionDePunto
            {
                Punto = p,
                CartaJugada = cartaJugada,
                Acierta = CorregirEvaluador.Acierta(def, p, cartaJugada),
                CartaCorrecta = p.EsReal ? p.Zona.Soluciones.FirstOrDefault() : PuntosDelDiagrama.EstaBien
            };
            if (!r.Acierta && cartaJugada != null && cartaJugada != PuntosDelDiagrama.EstaBien)
            {
                var c = PuntosDelDiagrama.Carta(def, cartaJugada);
                if (c != null && !string.IsNullOrEmpty(c.PorQueNo)) r.PorQueNo = c.PorQueNo;
            }

            if (p.EsReal)
            {
                r.Titular = Mayuscula(EtiquetasDeDefecto.Humanizar(p.Zona.Defecto));
                string que;
                r.Definicion = EtiquetasDeDefecto.QueEs.TryGetValue(p.Zona.Defecto ?? "", out que)
                    ? Mayuscula(que) + "."
                    : Corta(p.Zona.Explicacion);
                r.ExplicacionCompleta = p.Zona.Explicacion ?? "";
                if (!r.Acierta)
                {
                    r.ExplicacionCompleta += $"\n\nLo arreglaba «{PuntosDelDiagrama.NombreDeCarta(def, r.CartaCorrecta)}»." +
                        (r.PorQueNo != null ? $" «{PuntosDelDiagrama.NombreDeCarta(def, cartaJugada)}»: {r.PorQueNo}" : "");
                }
            }
            else
            {
                r.Titular = "Estaba bien";
                r.Definicion = Corta(p.Senuelo.RazonNoEsDefecto);
                r.ExplicacionCompleta = p.Senuelo.RazonNoEsDefecto ?? "";
                if (!r.Acierta) r.ExplicacionCompleta += "\n\nCambiar lo que funciona también cuenta en contra.";
            }
            return r;
        }

        /// <summary>La primera frase, recortada a MaximoDeDefinicion con puntos suspensivos.</summary>
        public static string Corta(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var t = texto.Trim();
            var punto = t.IndexOf(". ", System.StringComparison.Ordinal);
            if (punto > 8) t = t.Substring(0, punto + 1);
            if (t.Length > MaximoDeDefinicion) t = t.Substring(0, MaximoDeDefinicion - 1).TrimEnd() + "…";
            return t;
        }

        private static string Mayuscula(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
