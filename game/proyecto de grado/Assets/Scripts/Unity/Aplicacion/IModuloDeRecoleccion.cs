using System;
using Nexus.Core.Fase1;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// El recorrido de la Fase 1, visto desde el juego: recibe la ENTRADA del contrato y, cuando el jugador
    /// termina, devuelve la SALIDA (docs/03-calidad/juego/contrato-recoleccion-3d.md). El juego no sabe como
    /// esta hecho: el mundo 3D vive en su propio ensamblado (Nexus.Mundo3D) y se registra en AppRoot al arrancar.
    ///
    /// Sin ningun modulo registrado, la Fase 1 sigue ofreciendo el recorrido simulado.
    /// </summary>
    public interface IModuloDeRecoleccion {
        /// <summary>
        /// Abre el recorrido. 'alTerminar' se llama UNA vez, con el juego ya de vuelta en la pantalla desde la
        /// que se abrio; quien llama es el que entrega el resultado al motor.
        /// </summary>
        void Empezar(AppRoot app, EntradaDeRecoleccion entrada, Action<ResultadoDeRecoleccion> alTerminar);
    }
}
