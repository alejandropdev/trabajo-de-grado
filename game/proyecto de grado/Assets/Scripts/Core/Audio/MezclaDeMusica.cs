using System;

namespace Nexus.Core.Audio {
    /// <summary>
    /// Que suena en cada momento del juego. Hay dos temas:
    ///   · tema 1 («nexus»): oscuro, crece despacio. Es la identidad del juego: menus, planificar, la jornada.
    ///   · tema 2 («accion»): brillante y de pulso constante. Es el de hacer algo contra el reloj: el recorrido 3D y la
    ///     Fase 3 (desplegar y lanzar).
    /// En los minijuegos no suena nada: ahi hace falta pensar.
    ///
    /// El nivel va de 0 a 1 y se multiplica por el volumen de musica de Opciones. Con «apremio» (al reloj le queda
    /// poco), el tema que suena sube un poco.
    /// </summary>
    public static class MezclaDeMusica {
        public const string Nexus = "nexus", Accion = "accion";
        public const float EnMenus = 1f, EnLaPartida = 0.3f, EnElRecorrido = 0.45f, EnElDespliegue = 0.35f, EnElLanzamiento = 0.5f;
        /// <summary>Lo que sube el tema cuando al reloj le queda poco.</summary>
        public const float SubidaPorApremio = 0.2f;

        public struct Mezcla {
            /// <summary>El tema que suena (Nexus o Accion), o null si no suena nada.</summary>
            public string Pista;
            public float Nivel;
        }

        /// <summary>Por el nombre del tipo de la pantalla de arriba (asi no depende de ningun ensamblado de Unity).</summary>
        public static Mezcla Para(string pantalla, bool apremio = false) {
            Mezcla m;
            switch (pantalla) {
                case "PantallaDePerfiles":
                case "PantallaDePartidas":
                case "PantallaDelDiario":
                case "PantallaDeMensaje":
                case "PantallaDeError":
                    m = new Mezcla { Pista = Nexus, Nivel = EnMenus };
                    break;
                case "PantallaDetectar":
                case "PantallaCorregirDiagrama":
                case "PantallaOrdenar":
                case "PantallaRepartir":
                    m = new Mezcla { Pista = null, Nivel = 0f };
                    break;
                case "PantallaDeRecoleccion3D":
                    m = new Mezcla { Pista = Accion, Nivel = EnElRecorrido };
                    break;
                case "PantallaDeDespliegue":
                    m = new Mezcla { Pista = Accion, Nivel = EnElDespliegue };
                    break;
                case "PantallaDeLanzamiento":
                    m = new Mezcla { Pista = Accion, Nivel = EnElLanzamiento };
                    break;
                default:
                    m = new Mezcla { Pista = Nexus, Nivel = EnLaPartida };
                    break;
            }
            if (apremio && m.Pista != null) m.Nivel = Math.Min(1f, m.Nivel + SubidaPorApremio);
            return m;
        }
    }
}
