using System;
using System.Collections.Generic;

namespace Nexus.Core.Coleccion {
    /// <summary>
    /// Una nota adhesiva leida como una charla: quien la firma (si la firma) y sus frases, una por mensaje. Quita las
    /// comillas «» de fuera y la firma final «– Nombre.».
    /// </summary>
    public static class LecturaDeNota {
        public sealed class Lectura {
            /// <summary>Quien firma, o null si no firma nadie.</summary>
            public string Autor;
            public List<string> Frases = new List<string>();
        }

        public static Lectura Partir(string texto) {
            var r = new Lectura();
            var t = (texto ?? "").Trim();
            if (t.StartsWith("«", StringComparison.Ordinal)) t = t.Substring(1);
            if (t.EndsWith("»", StringComparison.Ordinal)) t = t.Substring(0, t.Length - 1);
            t = t.Trim();

            // La firma: lo que va tras el ultimo guion largo o medio, si es corto (un nombre, no una frase).
            var guion = Math.Max(t.LastIndexOf(" – ", StringComparison.Ordinal), t.LastIndexOf(" — ", StringComparison.Ordinal));
            if (guion > 0) {
                var firma = t.Substring(guion + 3).Trim().TrimEnd('.', ' ');
                if (firma.Length > 0 && firma.Length <= 30) {
                    r.Autor = firma;
                    t = t.Substring(0, guion).Trim();
                }
            }

            // Las frases: se corta tras . ! ? seguidos de espacio. Los puntos suspensivos y las abreviaturas cortas no
            // cortan (en las notas no hay); una frase que queda muy corta se pega a la anterior.
            var actual = "";
            for (var i = 0; i < t.Length; i++) {
                actual += t[i];
                var fin = (t[i] == '.' || t[i] == '!' || t[i] == '?') && (i + 1 == t.Length || t[i + 1] == ' ');
                if (fin && actual.Trim().Length > 0) {
                    Anadir(r.Frases, actual.Trim());
                    actual = "";
                }
            }
            if (actual.Trim().Length > 0) Anadir(r.Frases, actual.Trim());
            return r;
        }

        private static void Anadir(List<string> frases, string frase) {
            if (frases.Count > 0 && frase.Length < 12) frases[frases.Count - 1] += " " + frase;
            else frases.Add(frase);
        }
    }
}
