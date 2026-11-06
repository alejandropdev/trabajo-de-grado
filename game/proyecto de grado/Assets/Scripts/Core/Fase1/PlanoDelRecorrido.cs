using System;

namespace Nexus.Core.Fase1 {
    /// <summary>
    /// El plano ilustrado del recorrido 3D (StreamingAssets/mapa-3d.json) y la cuenta que pone al jugador encima.
    ///
    /// El dibujo NO es una vista cenital: es el edificio visto desde arriba y de frente, asi que el suelo, que en el
    /// mundo es un rectangulo, en la imagen es un trapecio (mas estrecho arriba). Escalar X y Z a lo ancho y alto de
    /// la imagen pondria el punto en otra sala; lo que se usa es una homografia: la transformacion de perspectiva que
    /// lleva las 4 esquinas del suelo a sus 4 esquinas en el dibujo, y todo lo de dentro con ellas.
    ///
    /// El orden de las esquinas dice tambien el giro del dibujo respecto del mundo (en el de Nexus, «arriba» en el
    /// dibujo es −X del mundo): por eso no hay un campo de rotacion aparte.
    /// </summary>
    public sealed class PlanoDelRecorrido {
        /// <summary>Ruta en Resources de la ilustracion ("Mapas/Mapa3D").</summary>
        public string Imagen;

        /// <summary>Tamano en pixeles del dibujo con el que se midieron las esquinas.</summary>
        public float AnchoImagen, AltoImagen;

        /// <summary>El rectangulo de suelo andable, en metros del mundo.</summary>
        public LimitesDelMundo Mundo = new LimitesDelMundo();

        /// <summary>
        /// Las esquinas del suelo en el dibujo, en pixeles con el origen ARRIBA a la izquierda, en este orden:
        /// (minX,minZ), (maxX,minZ), (maxX,maxZ), (minX,maxZ).
        /// </summary>
        public float[][] Esquinas;

        public sealed class LimitesDelMundo {
            public float MinX, MaxX, MinZ, MaxZ;
        }

        private double[] _h;

        public bool EsValido {
            get {
                if (Esquinas == null || Esquinas.Length != 4 || AnchoImagen <= 0 || AltoImagen <= 0 || Mundo == null) return false;
                if (Mundo.MaxX - Mundo.MinX <= 0 || Mundo.MaxZ - Mundo.MinZ <= 0) return false;
                foreach (var e in Esquinas) if (e == null || e.Length != 2) return false;
                return Homografia() != null;
            }
        }

        /// <summary>
        /// Donde cae en el dibujo un punto del mundo, en fracciones de la imagen (0..1, origen arriba a la izquierda).
        /// Fuera del suelo tambien responde (fuera del trapecio): quien pinta decide si lo recorta.
        /// </summary>
        public bool Proyectar(float x, float z, out float u, out float v) {
            u = v = 0;
            var h = Homografia();
            if (h == null) return false;
            var s = (x - Mundo.MinX) / (double)(Mundo.MaxX - Mundo.MinX);
            var t = (z - Mundo.MinZ) / (double)(Mundo.MaxZ - Mundo.MinZ);
            var w = h[6] * s + h[7] * t + 1;
            if (Math.Abs(w) < 1e-9) return false;
            u = (float)((h[0] * s + h[1] * t + h[2]) / w / AnchoImagen);
            v = (float)((h[3] * s + h[4] * t + h[5]) / w / AltoImagen);
            return true;
        }

        /// <summary>
        /// Hacia donde mira en el dibujo alguien que en el mundo mira hacia (dirX, dirZ): angulo en grados, 0 = hacia
        /// la derecha de la imagen y creciendo en el sentido de las agujas del reloj (y de la imagen hacia abajo).
        /// Se mide proyectando un paso corto, asi respeta la perspectiva y el giro del dibujo.
        /// </summary>
        public bool Rumbo(float x, float z, float dirX, float dirZ, out float grados) {
            grados = 0;
            var largo = Math.Sqrt(dirX * dirX + dirZ * dirZ);
            if (largo < 1e-6) return false;
            const double paso = 0.5;
            float u0, v0, u1, v1;
            if (!Proyectar(x, z, out u0, out v0)) return false;
            if (!Proyectar((float)(x + dirX / largo * paso), (float)(z + dirZ / largo * paso), out u1, out v1)) return false;
            var du = (u1 - u0) * AnchoImagen;
            var dv = (v1 - v0) * AltoImagen;
            if (Math.Abs(du) < 1e-9 && Math.Abs(dv) < 1e-9) return false;
            grados = (float)(Math.Atan2(dv, du) * 180.0 / Math.PI);
            return true;
        }

        /// <summary>Si cambian las esquinas o los limites despues de usarlo, hay que olvidarse de la cuenta hecha.</summary>
        public void Recalcular() { _h = null; }

        /// <summary>
        /// La homografia del cuadrado unidad (s,t) a los pixeles del dibujo: 8 incognitas, 2 ecuaciones por esquina.
        /// Null si las esquinas no la permiten (tres alineadas, repetidas…).
        /// </summary>
        private double[] Homografia() {
            if (_h != null) return _h;
            if (Esquinas == null || Esquinas.Length != 4) return null;
            double[][] src = { new[] { 0.0, 0.0 }, new[] { 1.0, 0.0 }, new[] { 1.0, 1.0 }, new[] { 0.0, 1.0 } };
            var a = new double[8, 9];
            for (var i = 0; i < 4; i++) {
                if (Esquinas[i] == null || Esquinas[i].Length != 2) return null;
                double s = src[i][0], t = src[i][1], x = Esquinas[i][0], y = Esquinas[i][1];
                var f = 2 * i;
                a[f, 0] = s; a[f, 1] = t; a[f, 2] = 1; a[f, 6] = -s * x; a[f, 7] = -t * x; a[f, 8] = x;
                a[f + 1, 3] = s; a[f + 1, 4] = t; a[f + 1, 5] = 1; a[f + 1, 6] = -s * y; a[f + 1, 7] = -t * y; a[f + 1, 8] = y;
            }
            _h = Resolver(a);
            return _h;
        }

        /// <summary>Gauss con pivoteo parcial sobre la matriz aumentada 8x9.</summary>
        private static double[] Resolver(double[,] a) {
            const int n = 8;
            for (var c = 0; c < n; c++) {
                var piv = c;
                for (var f = c + 1; f < n; f++) if (Math.Abs(a[f, c]) > Math.Abs(a[piv, c])) piv = f;
                if (Math.Abs(a[piv, c]) < 1e-12) return null;
                if (piv != c)
                    for (var k = 0; k <= n; k++) { var tmp = a[c, k]; a[c, k] = a[piv, k]; a[piv, k] = tmp; }
                for (var f = 0; f < n; f++) {
                    if (f == c) continue;
                    var m = a[f, c] / a[c, c];
                    if (m == 0) continue;
                    for (var k = c; k <= n; k++) a[f, k] -= m * a[c, k];
                }
            }
            var r = new double[n];
            for (var i = 0; i < n; i++) r[i] = a[i, n] / a[i, i];
            return r;
        }
    }
}
