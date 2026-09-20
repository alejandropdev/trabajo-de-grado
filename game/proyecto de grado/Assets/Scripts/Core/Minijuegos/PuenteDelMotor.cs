using System;
using System.Collections.Generic;
using Nexus.Core.Evaluacion;
using Nexus.Core.Eventos;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using EfectoDelMotor = Nexus.Core.Eventos.EfectoDiferido;

namespace Nexus.Core.Minijuegos {
    /// <summary>
    /// C6 · El puente entre la escena y el motor.
    ///
    /// Los minijuegos se escribieron antes que el motor y hablan en Dictionary&lt;string,float&gt;;
    /// los eventos hablan en Dictionary&lt;string,object&gt;, porque un evento puede decir "-15%" y un
    /// float no sabe representar eso. Aqui se traduce, en un solo sitio.
    ///
    /// La flecha va en este sentido a proposito: Minijuegos conoce Eventos, y Eventos no conoce
    /// Minijuegos. Al reves seria un ciclo, y ademas no tendria sentido — el motor de eventos no
    /// necesita saber que existen los verbos.
    ///
    /// ★ Nota sobre INV-1: ResultadoMinijuego.Hallazgos NO se toca aqui. Los minijuegos no escriben
    /// FLG_*; devuelven hallazgos, y el canal narrativo (C7) decide al cerrar la fase que flag escribe
    /// cada uno. Es lo que mantiene testeable el arbol de los catorce finales.
    /// </summary>
    public static class PuenteDelMotor {
        /// <summary>Un efecto diferido de minijuego, en la forma que entiende EffectScheduler.</summary>
        public static EfectoDelMotor ATipoDelMotor(EfectoDiferido deLaEscena) {
            if (deLaEscena == null) return null;

            var convertido = new EfectoDelMotor {
                EnDias = deLaEscena.EnDias,
                EventoForzado = deLaEscena.EventoForzado
            };

            if (deLaEscena.Efectos != null)
                foreach (var kv in deLaEscena.Efectos) convertido.Efectos[kv.Key] = (double)kv.Value;

            return convertido;
        }

        /// <summary>
        /// Aplica el resultado: los efectos inmediatos al estado y los diferidos a la agenda.
        /// La escena NO aplica nada — devuelve el resultado y el motor lo aplica, que es lo que hace
        /// que la traza del jugador sea cierta (INV-2).
        /// </summary>
        public static void Aplicar(ResultadoMinijuego resultado, WorldState w, EffectScheduler scheduler, int dia) {
            if (resultado == null) throw new ArgumentNullException(nameof(resultado));
            if (w == null) throw new ArgumentNullException(nameof(w));

            EffectApplier.Aplicar(w, resultado.EfectosInmediatos);

            if (scheduler == null || resultado.EfectosDiferidos == null) return;
            var origen = string.IsNullOrEmpty(resultado.MinijuegoId) ? "MJ" : resultado.MinijuegoId;

            foreach (var diferido in resultado.EfectosDiferidos)
                scheduler.Encolar(dia, ATipoDelMotor(diferido), origen);
        }

        /// <summary>
        /// La entrada de traza de un minijuego. 'Origen' es el id del minijuego, que es lo que la
        /// cadena causal del post-mortem necesita para decir "el fallo del dia 18 empieza aqui".
        /// </summary>
        public static EntradaTraza ATraza(ResultadoMinijuego resultado, int dia, string estadoAntes, string estadoDespues) {
            if (resultado == null) throw new ArgumentNullException(nameof(resultado));

            var rubrica = resultado.Rubrica ?? new Rubrica();
            return new EntradaTraza {
                Dia = dia,
                Origen = string.IsNullOrEmpty(resultado.MinijuegoId) ? "MJ" : resultado.MinijuegoId,
                Titulo = resultado.TextoCierre,
                OpcionId = resultado.Resultado,
                OpcionTexto = Describir(resultado),
                Veredicto = rubrica.Veredicto,
                Oa = rubrica.Oa,
                Razon = rubrica.Razon,
                EstadoAntes = estadoAntes,
                EstadoDespues = estadoDespues
            };
        }

        /// <summary>
        /// Lo que se le enseña al jugador en la traza. Nunca dice "has fallado": dice lo que hizo.
        /// El fallo produce una escena con nombre, cara y fecha, no una puntuacion (§6.2, principio 1).
        /// </summary>
        private static string Describir(ResultadoMinijuego resultado) {
            switch (resultado.Resultado) {
                case ResultadosDeMinijuego.Todos: return "Marcaste todo lo que habia.";
                case ResultadosDeMinijuego.Parcial: return "Se te escapo algo.";
                case ResultadosDeMinijuego.FalsoPositivo: return "Marcaste algo que estaba bien.";
                case ResultadosDeMinijuego.Omitido: return "Se acabo el tiempo sin marcar nada.";
                default: return resultado.Resultado;
            }
        }

        /// <summary>
        /// El resultado de no hacer nada: se acabo el reloj. Lo construye la UI cuando expira la ventana
        /// de atencion, y lo trae de vuelta como cualquier otro resultado.
        ///
        /// Existe aqui y no en cada escena porque omitir tiene la misma forma para los seis verbos,
        /// y porque su rubrica es siempre la misma: incorrecta, pero explicando por que.
        /// </summary>
        public static ResultadoMinijuego Omitido(string minijuegoId, string objetivoAprendizaje,
                                                 Consecuencia consecuencia) {
            var resultado = new ResultadoMinijuego {
                MinijuegoId = minijuegoId,
                Resultado = ResultadosDeMinijuego.Omitido
            };

            if (consecuencia != null) {
                if (consecuencia.EfectosInmediatos != null)
                    foreach (var kv in consecuencia.EfectosInmediatos) resultado.EfectosInmediatos[kv.Key] = kv.Value;
                if (consecuencia.EfectosDiferidos != null)
                    resultado.EfectosDiferidos.AddRange(consecuencia.EfectosDiferidos);
                if (consecuencia.Hallazgos != null)
                    resultado.Hallazgos.AddRange(consecuencia.Hallazgos);
                if (consecuencia.Rubrica != null) resultado.Rubrica = consecuencia.Rubrica;
            }

            if (string.IsNullOrEmpty(resultado.Rubrica.Veredicto)) {
                resultado.Rubrica.Veredicto = Veredictos.Incorrecta;
                resultado.Rubrica.Oa = objetivoAprendizaje;
                resultado.Rubrica.Razon = "No llegaste a mirarlo. Decidir no mirar tambien es decidir.";
            }

            return resultado;
        }
    }
}
