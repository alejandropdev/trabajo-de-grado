using System;
using System.Collections.Generic;

namespace Nexus.Core.Tablero {
    /// <summary>
    /// Mover un elemento de una lista a otro puesto, corriendo los de en medio: lo que hace soltar una tarjeta
    /// arrastrada en un muro (el product backlog de la Fase 1, el sprint backlog, el orden del tablero).
    /// </summary>
    public static class OrdenDeLista {
        /// <summary>
        /// Lleva el elemento de 'desde' a 'hasta' (indices de la lista, ya acotados). Devuelve si cambio algo.
        /// </summary>
        public static bool Mover<T>(IList<T> lista, int desde, int hasta) {
            if (lista == null) throw new ArgumentNullException(nameof(lista));
            if (desde < 0 || desde >= lista.Count) return false;
            hasta = Math.Max(0, Math.Min(lista.Count - 1, hasta));
            if (desde == hasta) return false;
            var elemento = lista[desde];
            lista.RemoveAt(desde);
            lista.Insert(hasta, elemento);
            return true;
        }

        /// <summary>
        /// Mete 'elemento' en el puesto 'puesto' (acotado), o lo mueve alli si ya estaba. Devuelve si cambio algo.
        /// </summary>
        public static bool Poner<T>(IList<T> lista, T elemento, int puesto) {
            if (lista == null) throw new ArgumentNullException(nameof(lista));
            var esta = lista.IndexOf(elemento);
            if (esta >= 0) return Mover(lista, esta, puesto);
            lista.Insert(Math.Max(0, Math.Min(lista.Count, puesto)), elemento);
            return true;
        }
    }
}
