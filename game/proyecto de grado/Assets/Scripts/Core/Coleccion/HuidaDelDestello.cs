using System;

namespace Nexus.Core.Coleccion {
    /// <summary>
    /// Un coleccionable del mapa de la oficina ya no es un boton «Mirarlo»: es un destello que flota en su sala y que
    /// hay que atrapar con el raton. Esta clase es la regla del juego; la pantalla solo la dibuja.
    ///
    ///   - Cuando el cursor se le acerca, se escapa: un salto corto lejos del cursor, sin salirse de la sala.
    ///   - Se escapa tantas veces como dice lo dificil que fue hacerlo aparecer (facil 1, media 2, dificil 3).
    ///   - Despues se CANSA: deja de huir, flota mas despacio y brilla mas. Atraparlo nunca llega a frustrar; lo que
    ///     cuesta es que aparezca (AparicionDeColeccionable), no cogerlo.
    /// </summary>
    public sealed class HuidaDelDestello {
        /// <summary>Distancia (en pixeles del plano) a la que el cursor lo asusta.</summary>
        public const float RadioDeAlarma = 70f;

        /// <summary>Lo que se aleja en cada escape, en pixeles del plano.</summary>
        public const float SaltoDeHuida = 110f;

        public int Quedan { get; private set; }
        public int Total { get; private set; }
        public bool Cansado { get { return Quedan <= 0; } }

        /// <summary>A que velocidad flota (1 = normal). Cansado flota despacio: se deja coger.</summary>
        public float Ritmo { get { return Cansado ? 0.35f : 1f; } }

        public HuidaDelDestello(string dificultad) {
            Total = Quedan = Escapes(dificultad);
        }

        public static int Escapes(string dificultad) {
            switch (dificultad) {
                case DificultadesDeColeccionable.Dificil: return 3;
                case DificultadesDeColeccionable.Media: return 2;
                default: return 1;
            }
        }

        /// <summary>
        /// El cursor esta en (cx,cy) y el destello en (x,y). Si lo asusta y aun le quedan escapes, gasta uno y dice a
        /// donde salta. Si no, se queda donde esta y devuelve false.
        /// </summary>
        public bool Asustar(float x, float y, float cx, float cy,
                            float minX, float minY, float maxX, float maxY,
                            out float nx, out float ny) {
            nx = x; ny = y;
            if (Cansado) return false;
            var dx = x - cx; var dy = y - cy;
            if (dx * dx + dy * dy > RadioDeAlarma * RadioDeAlarma) return false;
            Quedan--;
            PuntoDeHuida(x, y, cx, cy, SaltoDeHuida, minX, minY, maxX, maxY, out nx, out ny);
            return true;
        }

        /// <summary>
        /// A donde saltar: de 16 direcciones, la que lo deja mas lejos del cursor una vez recortado a la sala. Asi, si
        /// esta acorralado contra una pared, se escurre a lo largo de ella en vez de chocar.
        /// </summary>
        public static void PuntoDeHuida(float x, float y, float cx, float cy, float salto,
                                        float minX, float minY, float maxX, float maxY,
                                        out float nx, out float ny) {
            nx = Clamp(x, minX, maxX); ny = Clamp(y, minY, maxY);
            var mejor = -1.0;
            for (var i = 0; i < 16; i++) {
                var ang = i * Math.PI / 8;
                var px = Clamp((float)(x + Math.Cos(ang) * salto), minX, maxX);
                var py = Clamp((float)(y + Math.Sin(ang) * salto), minY, maxY);
                var d = (px - cx) * (px - cx) + (py - cy) * (py - cy);
                if (d > mejor) { mejor = d; nx = px; ny = py; }
            }
        }

        private static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
    }
}
