using System;
using System.Collections.Generic;

namespace Nexus.Core.Minijuegos {
    /// <summary>
    /// Cuando se juega guiado un minijuego: la PRIMERA vez que el perfil se enfrenta a cada mecanica (detectar,
    /// ordenar, repartir), sea en una practica o en un reto. Tres recorridos por perfil, no uno por escena: quien ya
    /// sabe revisar un diagrama no necesita que Marisol le vuelva a llevar de la mano en el siguiente.
    /// </summary>
    public static class TutorialPorMecanica {
        public static bool DebeGuiarse(IEnumerable<string> mecanicasGuiadas, string verbo) {
            var canonico = Verbos.Normalizar(verbo);
            if (canonico == null) return false;   // un verbo desconocido no tiene recorrido que seguir
            if (mecanicasGuiadas == null) return true;
            foreach (var m in mecanicasGuiadas)
                if (Verbos.Normalizar(m) == canonico) return false;
            return true;
        }

        /// <summary>
        /// Los perfiles de antes anotaban cada ESCENA jugada. De ahi se saca que mecanicas ya conocen, para no
        /// volver a guiarles lo que ya hicieron. 'verboDe' dice de que verbo es una escena (null si ya no existe).
        /// </summary>
        public static List<string> Migrar(IEnumerable<string> minijuegosJugados, Func<string, string> verboDe) {
            var mecanicas = new List<string>();
            if (minijuegosJugados == null || verboDe == null) return mecanicas;
            foreach (var id in minijuegosJugados) {
                var verbo = Verbos.Normalizar(verboDe(id));
                if (verbo != null && !mecanicas.Contains(verbo)) mecanicas.Add(verbo);
            }
            return mecanicas;
        }
    }
}
