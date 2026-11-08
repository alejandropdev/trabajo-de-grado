using System;

namespace Nexus.Core.Jornada {
    /// <summary>
    /// Las cuentas de la ventana del atardecer que acompaña al cierre de la jornada (2026-11-07): donde va el sol, que
    /// marca el reloj, en que orden se encienden las ventanas de la ciudad y que se dice de la racha de horas extra.
    /// Viven aqui, sin Unity, para poder probarlas; la escena solo las dibuja.
    /// </summary>
    public static class EscenaDelCierre {
        /// <summary>A partir de cuantos dias seguidos quedandose la racha se avisa en rojo.</summary>
        public const int RachaPeligrosa = 2;

        /// <summary>
        /// Cuanto de la tarde ha pasado entre 'desde' y 'hasta' (en minutos del dia), de 0 a 1. Fuera del tramo se
        /// queda en el borde; un tramo vacio vale 1 (ya paso).
        /// </summary>
        public static float FraccionDeTarde(int minuto, int desde, int hasta) {
            if (hasta <= desde) return 1f;
            var f = (minuto - desde) / (float)(hasta - desde);
            return f < 0 ? 0 : f > 1 ? 1 : f;
        }

        /// <summary>El angulo de la aguja de las horas, en grados en el sentido del reloj desde las 12.</summary>
        public static float AnguloDeHora(int minutoDelDia) {
            var m = ((minutoDelDia % 720) + 720) % 720;
            return m / 720f * 360f;
        }

        /// <summary>El angulo del minutero, en grados en el sentido del reloj desde las 12.</summary>
        public static float AnguloDeMinuto(int minutoDelDia) {
            var m = ((minutoDelDia % 60) + 60) % 60;
            return m / 60f * 360f;
        }

        /// <summary>
        /// El orden en que se encienden 'total' ventanas: una permutacion fija (la misma semilla, el mismo orden) para que
        /// la ciudad no se encienda de izquierda a derecha como una barra de progreso.
        /// </summary>
        public static int[] OrdenDeEncendido(int total, int semilla = 17) {
            if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
            var orden = new int[total];
            for (var i = 0; i < total; i++) orden[i] = i;
            var r = new Random(semilla);
            for (var i = total - 1; i > 0; i--) {
                var j = r.Next(i + 1);
                var t = orden[i]; orden[i] = orden[j]; orden[j] = t;
            }
            return orden;
        }

        /// <summary>
        /// Cuantas ventanas hay encendidas a los 'segundos' de abrir: se van encendiendo durante 'periodo' segundos, se
        /// quedan asi 'pausa' segundos y vuelven a empezar, poco a poco, en bucle.
        /// </summary>
        public static int VentanasEncendidas(float segundos, int total, float periodo = 9f, float pausa = 3f) {
            if (total <= 0 || periodo <= 0) return 0;
            var ciclo = periodo + Math.Max(0f, pausa);
            var t = segundos % ciclo;
            if (t < 0) t += ciclo;
            if (t >= periodo) return total;
            return Math.Min(total, (int)(t / periodo * (total + 1)));
        }

        /// <summary>La linea sobre la racha de dias quedandose (RuntimeState.DiasSeguidosTrabajando).</summary>
        public static string Racha(int diasSeguidos) {
            if (diasSeguidos <= 0) return "Sin horas extra seguidas";
            if (diasSeguidos == 1) return "Ayer te quedaste";
            return $"Llevas {diasSeguidos} días seguidos quedándote";
        }

        public static bool EsRachaPeligrosa(int diasSeguidos) { return diasSeguidos >= RachaPeligrosa; }

        // ==================================================================== se acabaron los dias (2026-11-07)

        /// <summary>
        /// Por donde va el paquete en la tuberia del despliegue, de 0 a 1: recorre durante 'periodo' segundos, se queda
        /// 'pausa' segundos en el final (llego) y vuelve a salir.
        /// </summary>
        public static float Recorrido(float segundos, float periodo = 3.2f, float pausa = 0.8f) {
            if (periodo <= 0) return 1f;
            var ciclo = periodo + Math.Max(0f, pausa);
            var t = segundos % ciclo;
            if (t < 0) t += ciclo;
            return t >= periodo ? 1f : t / periodo;
        }

        /// <summary>La ultima etapa (0..etapas-1) por la que ya paso el paquete en 'recorrido'.</summary>
        public static int EtapaAlcanzada(float recorrido, int etapas) {
            if (etapas <= 1) return 0;
            var r = recorrido < 0 ? 0 : recorrido > 1 ? 1 : recorrido;
            return Math.Min(etapas - 1, (int)Math.Floor(r * (etapas - 1) + 1e-4));
        }

        /// <summary>
        /// Lo construido apilado como cajas: cuantas van en cada fila, de abajo arriba, en piramide desde 'base' (como
        /// mucho 'base' filas). Lo que no cabe no se dibuja: la pila dice «mucho», no cuenta una a una.
        /// </summary>
        public static int[] FilasDeLaPila(int cajas, int base_ = 5) {
            if (cajas <= 0 || base_ <= 0) return new int[0];
            var filas = new System.Collections.Generic.List<int>();
            var quedan = cajas;
            for (var ancho = base_; ancho > 0 && quedan > 0; ancho--) {
                var aqui = Math.Min(ancho, quedan);
                filas.Add(aqui);
                quedan -= aqui;
            }
            return filas.ToArray();
        }
    }
}
