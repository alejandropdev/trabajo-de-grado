using System;
using Nexus.Core.Servicios;
using Nexus.Core.Tarjetas;

namespace Nexus.Core.Sesion {
    public sealed partial class GameSession {
        /// <summary>
        /// Resuelve una tarjeta: aplica los efectos, agenda los diferidos y escribe en la traza con el Origen, el Oa
        /// y el veredicto de la tarjeta — los mismos que tenia la pantalla que sustituye. Pasa por Registrar como
        /// los demas sitios que evaluan, asi la competencia y el Dashboard no saben que ya no hay un parrafo delante.
        /// La UI no aplica nada: elige, y el motor decide.
        /// </summary>
        public void ResolverTarjeta(TarjetaDef tarjeta, string opcionId) {
            if (tarjeta == null) throw new ArgumentNullException(nameof(tarjeta));
            var opcion = tarjeta.Opcion(opcionId);
            if (opcion == null) throw new InvalidOperationException($"'{opcionId}' no es una opcion de la tarjeta {tarjeta.Id}.");

            var antes = W.ToString();
            EffectApplier.Aplicar(W, opcion.Efectos);

            if (opcion.Diferidos != null)
                foreach (var diferido in opcion.Diferidos)
                    _scheduler.Encolar(R.DiaActual, diferido, tarjeta.Origen);

            Registrar(tarjeta.Origen, tarjeta.Titulo, opcion.Id, opcion.Etiqueta,
                      opcion.Veredicto, tarjeta.Oa, opcion.Razon, antes, opcion.NotaDeMetodologia);
        }
    }
}
