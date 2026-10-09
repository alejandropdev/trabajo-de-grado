using System;
using Nexus.Core.Fase1;
using Nexus.Unity.Aplicacion;
using UnityEngine;

namespace Nexus.Mundo3D {
    /// <summary>
    /// El recorrido 3D de la Fase 1, enchufado al juego. Se registra solo, antes de que cargue la primera escena;
    /// el juego solo conoce la interfaz, asi que quitar este ensamblado de la build lo devuelve al recorrido simulado.
    /// </summary>
    public sealed class ModuloDeRecoleccion3D : IModuloDeRecoleccion {
        /// <summary>La escena del recorrido. Tiene que estar en Build Settings (Nexus > Mundo 3D > Preparar escena de recolección).</summary>
        public const string Escena = "Recoleccion3D";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar() {
            AppRoot.ModuloDeRecoleccion = new ModuloDeRecoleccion3D();
        }

        public void Empezar(AppRoot app, EntradaDeRecoleccion entrada, Action<ResultadoDeRecoleccion> alTerminar) {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (entrada == null) throw new ArgumentNullException(nameof(entrada));
            app.Router.Apilar<PantallaDeRecoleccion3D>(p => {
                p.Entrada = entrada;
                p.AlTerminar = alTerminar;
            });
        }
    }
}
