using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Narrativa;
using Nexus.Unity.Pantallas.Minijuegos;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las escenas de tiza de los pizarrones: un dibujo por idea, cada uno en su cuadro de 600×300. El pizarron lo
    /// agranda a pagina completa o lo encoge dentro de una columna. Los ids son los de EscenasDePizarra (Core), que
    /// es la lista que valida el contenido: una escena nueva se apunta alli y se dibuja aqui, en el archivo de su tema.
    /// </summary>
    public static partial class DibujosDeEscena {
        public const float Ancho = 600, Alto = 300;

        private static Dictionary<string, Action<Tizador>> _todas;

        private static Dictionary<string, Action<Tizador>> Todas {
            get {
                if (_todas != null) return _todas;
                var d = new Dictionary<string, Action<Tizador>>(StringComparer.Ordinal);
                Arquitectura(d);
                Calidad(d);
                Cascada(d);
                Despliegue(d);
                Documentos(d);
                Equipo(d);
                Juego(d);
                Kanban(d);
                Metricas(d);
                Oficio(d);
                Plataformas(d);
                Producto(d);
                Scrum(d);
                return _todas = d;
            }
        }

        public static bool Existe(string id) { return !string.IsNullOrEmpty(id) && Todas.ContainsKey(id); }

        public static void Dibujar(Tizador t, string id) {
            Action<Tizador> dibujo;
            if (id != null && Todas.TryGetValue(id, out dibujo)) dibujo(t);
            else t.Texto(Ancho / 2, Alto / 2, "?", 60, Tizador.Gris, true);
        }

        /// <summary>Lo que no cuadra entre la lista del contenido y lo que de verdad se sabe dibujar. Vacio = todo en orden.</summary>
        public static List<string> Desajustes() {
            var faltan = EscenasDePizarra.Todas.Where(id => !Todas.ContainsKey(id)).Select(id => "sin dibujo: " + id);
            var sobran = Todas.Keys.Where(id => !EscenasDePizarra.Existe(id)).Select(id => "sin apuntar en EscenasDePizarra: " + id);
            return faltan.Concat(sobran).ToList();
        }

        // ---------------------------------------------------------------- piezas que comparten varias escenas

        private static void Sobre(Tizador p, float x, float y, float w, float h, UnityEngine.Color? c = null) {
            p.Caja(x, y, w, h, null, c);
            p.Trazo(c, x, y, x + w / 2, y + h * 0.55f, x + w, y);
        }

        /// <summary>Un renglon de registro: una linea de texto con su marca al final.</summary>
        private static void Renglon(Tizador p, float x, float y, float w, string t, UnityEngine.Color? c = null) {
            p.Texto(x + 8, y, t, 16, c);
            p.Linea(x, y + 8, x + w, y + 8, Tizador.Gris, 1.5f);
        }
    }
}
