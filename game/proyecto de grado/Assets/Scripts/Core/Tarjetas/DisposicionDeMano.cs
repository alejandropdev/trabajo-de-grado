using System;

namespace Nexus.Core.Tarjetas {
    /// <summary>
    /// Donde va cada carta de una mano de elecciones (ManoDeCartas). Solo geometria, para poder probarla sin Unity.
    ///
    ///   - Hasta 4 opciones: en ABANICO. Las cartas se reparten en fila, un poco giradas (las de los lados mas) y un
    ///     poco mas bajas cuanto mas lejos del centro, como una mano de naipes.
    ///   - Con una carta encima (raton o teclado), esa se endereza y sube, y sus vecinas se apartan: el abanico se abre.
    ///   - Mas de 4: en REJILLA, sin giro (siete razones en abanico no se leen).
    ///
    /// Coordenadas con el origen en el centro de la mano, Y hacia arriba (las de la UI de Unity).
    /// </summary>
    public static class DisposicionDeMano {
        public const int MaximoEnAbanico = 4;

        /// <summary>Lo que gira la carta de cada lado por cada puesto que se aleja del centro.</summary>
        public const float GiroPorPuesto = 4f;

        /// <summary>Lo que baja la carta por cada puesto (al cuadrado): el arco de la mano.</summary>
        public const float CaidaPorPuesto = 7f;

        /// <summary>Lo que se aparta cada vecina de la carta que se mira.</summary>
        public const float Apertura = 22f;

        /// <summary>Lo que sube la carta que se mira.</summary>
        public const float Subida = 18f;

        public struct Lugar {
            public float X, Y, Angulo;
        }

        public static bool EnRejilla(int n) { return n > MaximoEnAbanico; }

        /// <summary>Columnas de la rejilla: 3 por fila.</summary>
        public static int Columnas(int n) { return n <= 4 ? 2 : 3; }

        /// <summary>
        /// Los lugares de n cartas de 'ancho' x 'alto', separadas 'hueco'. 'encima' = indice de la carta que se mira
        /// (-1 ninguna). En rejilla la carta que se mira no se mueve (solo crece, cosa de quien la dibuja).
        /// </summary>
        public static Lugar[] Calcular(int n, float ancho, float alto, float hueco, int encima = -1) {
            if (n <= 0) return new Lugar[0];
            var lugares = new Lugar[n];
            if (EnRejilla(n)) {
                var cols = Columnas(n);
                var filas = (n + cols - 1) / cols;
                for (var i = 0; i < n; i++) {
                    int f = i / cols, c = i % cols;
                    // La ultima fila, si esta incompleta, se centra.
                    var enEstaFila = f == filas - 1 ? n - f * cols : cols;
                    lugares[i].X = (c - (enEstaFila - 1) / 2f) * (ancho + hueco);
                    lugares[i].Y = ((filas - 1) / 2f - f) * (alto + hueco);
                    lugares[i].Angulo = 0;
                }
                return lugares;
            }
            var medio = (n - 1) / 2f;
            for (var i = 0; i < n; i++) {
                var d = i - medio;
                lugares[i].X = d * (ancho + hueco);
                lugares[i].Y = -d * d * CaidaPorPuesto;
                lugares[i].Angulo = -d * GiroPorPuesto;   // en Unity, angulo positivo = antihorario: la derecha se inclina a la derecha
                if (encima < 0 || encima >= n) continue;
                if (i == encima) {
                    lugares[i].Angulo = 0;
                    lugares[i].Y += Subida;
                } else {
                    lugares[i].X += Math.Sign(i - encima) * Apertura;
                }
            }
            return lugares;
        }

        /// <summary>
        /// Por donde llega cada carta al repartir (2026-11-07). 'e' es lo que lleva de llegada, de 0 a 1 (ya suavizado):
        /// devuelve lo que hay que sumarle a su lugar (X, Y) y a su giro. Con e = 1 no suma nada: la carta esta en su
        /// sitio. La entrevista cambia de entrada en cada pregunta para que no se repita siempre igual.
        /// </summary>
        public static Lugar Desfase(EntradaDeMano entrada, int i, int n, float e) {
            var falta = 1 - (e < 0 ? 0 : e > 1 ? 1 : e);
            var d = i - (n - 1) / 2f;
            var lado = d < 0 || (d == 0 && i % 2 == 0) ? -1f : 1f;
            switch (entrada) {
                case EntradaDeMano.DesdeArriba:
                    return new Lugar { X = 0, Y = 300 * falta, Angulo = 0 };
                case EntradaDeMano.Girando:
                    return new Lugar { X = 0, Y = -120 * falta, Angulo = 180 * falta * (i % 2 == 0 ? 1 : -1) };
                case EntradaDeMano.DesdeLosLados:
                    return new Lugar { X = lado * 520 * falta, Y = 0, Angulo = -lado * 25 * falta };
                default:
                    return new Lugar { X = 0, Y = -260 * falta, Angulo = 0 };
            }
        }

        /// <summary>La entrada de la pregunta 'numero': rota entre las cuatro.</summary>
        public static EntradaDeMano EntradaPara(int numero) {
            return (EntradaDeMano)(((numero % 4) + 4) % 4);
        }

        /// <summary>Lo que ocupa la mano entera (para reservar sitio en el layout).</summary>
        public static void Tamano(int n, float ancho, float alto, float hueco, out float total, out float totalAlto) {
            if (n <= 0) { total = totalAlto = 0; return; }
            if (EnRejilla(n)) {
                var cols = Columnas(n);
                var filas = (n + cols - 1) / cols;
                total = cols * ancho + (cols - 1) * hueco;
                totalAlto = filas * alto + (filas - 1) * hueco;
                return;
            }
            // Margen para el giro, la subida y la apertura de las vecinas.
            total = n * ancho + (n - 1) * hueco + 2 * Apertura + 24;
            var medio = (n - 1) / 2f;
            totalAlto = alto + medio * medio * CaidaPorPuesto + Subida + 24;
        }
    }

    /// <summary>Como llegan las cartas al repartir. DesdeAbajo es la de siempre.</summary>
    public enum EntradaDeMano { DesdeAbajo, DesdeArriba, Girando, DesdeLosLados }

    /// <summary>
    /// Las teclas de una mano de elecciones: 1..9 o A..D eligen directamente. La A/B/C es la letra de las opciones de
    /// la entrevista; el numero, la de todas las demas. Devuelve -1 si la tecla no elige nada con n opciones.
    /// </summary>
    public static class TeclasDeEleccion {
        public static int Indice(string tecla, int n) {
            if (string.IsNullOrEmpty(tecla) || tecla.Length != 1 || n <= 0) return -1;
            var c = char.ToLowerInvariant(tecla[0]);
            int i;
            if (c >= '1' && c <= '9') i = c - '1';
            else if (c >= 'a' && c <= 'd') i = c - 'a';
            else return -1;
            return i < n ? i : -1;
        }

        /// <summary>Lo que se pinta en la carta: «1», «2»… o la letra si la mano usa letras.</summary>
        public static string Rotulo(int indice, bool conLetras) {
            if (indice < 0) return "";
            return conLetras ? ((char)('A' + indice)).ToString() : (indice + 1).ToString();
        }
    }
}
