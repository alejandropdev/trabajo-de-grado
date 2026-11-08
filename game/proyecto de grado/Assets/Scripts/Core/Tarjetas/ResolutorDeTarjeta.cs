using System;
using System.Collections.Generic;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;

namespace Nexus.Core.Tarjetas {
    /// <summary>
    /// Lo que la tarjeta enseña mientras se arrastra: cuanto cambiaria cada barra con cada respuesta. Solo el efecto
    /// INMEDIATO — el diferido no se previsualiza nunca (EffectApplier.Previsualizar lo dice: es diseño pedagogico).
    /// No escribe en nada: se puede llamar cada fotograma.
    /// </summary>
    public static class ResolutorDeTarjeta {
        public static Dictionary<string, double> Previsualizar(WorldState w, TarjetaDef tarjeta, string opcionId) {
            if (tarjeta == null) throw new ArgumentNullException(nameof(tarjeta));
            var opcion = tarjeta.Opcion(opcionId);
            if (opcion == null) throw new InvalidOperationException($"'{opcionId}' no es una opcion de la tarjeta {tarjeta.Id}.");
            return EffectApplier.Previsualizar(w, opcion.Efectos);
        }
    }
}
