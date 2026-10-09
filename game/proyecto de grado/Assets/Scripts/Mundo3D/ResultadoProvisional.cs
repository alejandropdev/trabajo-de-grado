using Nexus.Core.Fase1;

namespace Nexus.Mundo3D {
    /// <summary>
    /// ★ PROVISIONAL. El mundo 3D todavia no recoge nada: deja moverse y poco mas. Para que el nivel siga con
    /// lo que el diseño espera de un recorrido (pistas, recursos, personajes), al salir se entrega lo mismo que
    /// daba el boton «Normal» del recorrido simulado: determinista por semilla, y valido para el motor.
    ///
    /// Cuando el 3D sepa que zonas se pisaron y que se recogio, ESTA es la unica pieza que cambia: el resultado
    /// se arma con eso, y ni la pantalla de la Fase 1 ni el motor se enteran.
    /// </summary>
    public static class ResultadoProvisional {
        public static ResultadoDeRecoleccion Para(EntradaDeRecoleccion entrada) {
            return SimuladorDeRecoleccion.Simular(entrada, IntensidadesDeSimulacion.Normal);
        }
    }
}
