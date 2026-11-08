using System;

namespace Nexus.Core.Tarjetas {
    /// <summary>
    /// Donde va cada nodo de la pausa en red (2026-11-07). Solo geometria, para poder probarla sin Unity.
    ///
    /// Los nodos se reparten en una ELIPSE alrededor del nucleo, a pasos iguales y en sentido horario. Con un numero
    /// impar el primero queda arriba del todo; con uno par la red arranca media vuelta de paso antes, para que NINGUN nodo
    /// caiga en la vertical de abajo: ahi va «Seguir jugando».
    ///
    /// Coordenadas con el origen en el centro de la red, Y hacia arriba (las de la UI de Unity).
    /// </summary>
    public static class DisposicionRadial {
        public struct Punto {
            public float X, Y;
        }

        /// <summary>Angulo (grados, 0 = derecha, antihorario) del primer nodo.</summary>
        public static float AnguloInicial(int n) {
            if (n <= 0) return 90f;
            return n % 2 == 1 ? 90f : 90f - 180f / n;
        }

        /// <summary>Los puntos de n nodos en una elipse de semiejes rx (horizontal) y ry (vertical).</summary>
        public static Punto[] Calcular(int n, float rx, float ry) {
            if (n <= 0) return new Punto[0];
            var puntos = new Punto[n];
            var inicio = AnguloInicial(n);
            var paso = 360f / n;
            for (var i = 0; i < n; i++) {
                var angulo = (inicio - i * paso) * (float)Math.PI / 180f;   // restar = sentido horario
                puntos[i].X = rx * (float)Math.Cos(angulo);
                puntos[i].Y = ry * (float)Math.Sin(angulo);
            }
            return puntos;
        }
    }
}
