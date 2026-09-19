using System;
using System.Diagnostics;

namespace Nexus.Core.Guardado {
    /// <summary>
    /// Guarda en los cinco puntos (§4.10.6) y cronometra cada guardado: el SRS exige menos de 500 ms.
    /// Core no puede usar Debug.Log, asi que el aviso sale por un callback que conecta la capa Unity.
    /// Se guarda al final del dia y no en cada decision: la jornada es atomica, y eso es pedagogia, no tecnica.
    /// </summary>
    public sealed class AutoGuardado {
        public const double UmbralPorDefectoMs = 500.0;

        public static class Motivos {
            public const string CierreFase1 = "cierre de la fase 1";
            public const string FinDeJornada = "fin de la jornada";
            public const string TrasLanzamiento = "tras el lanzamiento";
            public const string CierreDeNivel = "cierre del nivel";
            public const string PausaYSalida = "pausa y salida";
        }

        private readonly SaveStore _store;
        private readonly SaveGame _partida;
        private readonly Action<string> _aviso;
        private readonly double _umbralMs;

        public AutoGuardado(SaveStore store, SaveGame partida, Action<string> aviso = null, double umbralMs = UmbralPorDefectoMs) {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _partida = partida ?? throw new ArgumentNullException(nameof(partida));
            _aviso = aviso;
            _umbralMs = umbralMs;
        }

        public double UltimoMs { get; private set; }
        public int Guardados { get; private set; }
        public string UltimoMotivo { get; private set; }

        public SaveGame Partida {
            get { return _partida; }
        }

        public void Guardar(ISesionPersistible sesion, string motivo) {
            var reloj = Stopwatch.StartNew();
            _store.Sincronizar(_partida, sesion);
            reloj.Stop();

            UltimoMs = reloj.Elapsed.TotalMilliseconds;
            UltimoMotivo = motivo;
            Guardados++;

            if (UltimoMs > _umbralMs && _aviso != null)
                _aviso($"El autoguardado '{motivo}' tardó {UltimoMs:0} ms (límite {_umbralMs:0} ms).");
        }
    }
}
