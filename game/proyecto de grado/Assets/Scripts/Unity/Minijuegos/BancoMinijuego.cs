using System.IO;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Minijuegos
{
    /// <summary>
    /// Banco de pruebas. Fabrica a mano el MinijuegoContext que en M8 dara el
    /// EventDirector, para poder jugar la escena hoy estando en M2.
    ///
    /// Uso: escena vacia -> GameObject vacio -> este componente ->
    /// arrastra PantallaDetectar.prefab al campo _prefabPantalla -> Play.
    /// </summary>
    public sealed class BancoMinijuego : MonoBehaviour
    {
        [SerializeField] private PantallaDetectarView _prefabPantalla;
        [SerializeField] private string _minijuegoId = "MJ-F2-02";
        [SerializeField, Range(0, 3)] private int _nivelAndamiaje = 3;
        [SerializeField] private string _quienEspera = "Javier";

        private void Start()
        {
            var ruta = Path.Combine(Application.streamingAssetsPath, "minijuegos", _minijuegoId + ".json");
            if (!File.Exists(ruta)) { Debug.LogError("No encuentro " + ruta); return; }

            MinijuegoDef def;
            try { def = CatalogoMinijuegos.Parsear(File.ReadAllText(ruta)); }
            catch (MinijuegoInvalidoException e) { Debug.LogError(e.Message); return; }

            var ctx = MinijuegoContext.Basico(_nivelAndamiaje);
            ctx.QuienEspera = _quienEspera;

            var pantalla = Instantiate(_prefabPantalla);
            pantalla.Iniciar(def, ctx, res =>
            {
                Debug.Log($"[{res.MinijuegoId}] resultado={res.Resultado} " +
                          $"veredicto={res.Rubrica.Veredicto} hallazgos={string.Join(",", res.Hallazgos)}");
                foreach (var kv in res.EfectosInmediatos) Debug.Log($"  efecto {kv.Key} {kv.Value:+0.#;-0.#}");
                foreach (var d in res.Detalle) Debug.Log("  " + d);
                // En M8: _effectApplier.Aplicar(res); _scheduler.Encolar(res.EfectosDiferidos); ...
            });
        }
    }
}
