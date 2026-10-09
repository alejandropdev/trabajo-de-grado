using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Nexus.Mundo3D {
    /// <summary>
    /// Cuanto tarda cada fotograma del recorrido 3D, en la maquina de quien juega. Se ve en pantalla con F3 y, al
    /// salir del recorrido, queda una linea en un registro: asi un cambio de rendimiento se compara con numeros, y
    /// no con «va mejor» o «va peor».
    ///
    /// El registro esta en  Application.persistentDataPath/rendimiento-3d.log
    /// </summary>
    public sealed class MedidorDeRendimiento {
        /// <summary>Los primeros segundos no cuentan: la escena acaba de cargar y esta compilando shaders.</summary>
        private const float SegundosDeCalentamiento = 2f;

        private readonly List<float> _fotogramas = new List<float>(8192);
        private float _calentamiento, _ventana;
        private int _enLaVentana;

        /// <summary>Fotogramas por segundo del ultimo medio segundo: lo que se enseña en pantalla.</summary>
        public float FpsAhora { get; private set; }
        public float MsAhora { get { return FpsAhora > 0 ? 1000f / FpsAhora : 0; } }

        public static string RutaDelRegistro {
            get { return Path.Combine(Application.persistentDataPath, "rendimiento-3d.log"); }
        }

        /// <summary>Una vez por fotograma, con el tiempo real (no el escalado por Time.timeScale).</summary>
        public void Anotar(float segundos) {
            if (segundos <= 0) return;
            _ventana += segundos;
            _enLaVentana++;
            if (_ventana >= 0.5f) {
                FpsAhora = _enLaVentana / _ventana;
                _ventana = 0;
                _enLaVentana = 0;
            }
            if (_calentamiento < SegundosDeCalentamiento) { _calentamiento += segundos; return; }
            _fotogramas.Add(segundos);
        }

        /// <summary>Media, el 1 % de fotogramas mas lentos y el peor: la media sola esconde los tirones.</summary>
        public string Resumen() {
            if (_fotogramas.Count < 10) return "sin datos suficientes";
            var ordenados = new List<float>(_fotogramas);
            ordenados.Sort();
            float total = 0;
            foreach (var f in ordenados) total += f;
            var peores = Mathf.Max(1, ordenados.Count / 100);
            float lentos = 0;
            for (var i = ordenados.Count - peores; i < ordenados.Count; i++) lentos += ordenados[i];
            return string.Format(CultureInfo.InvariantCulture,
                "{0:0} s · media {1:0.0} FPS ({2:0.0} ms) · 1 % más lento {3:0.0} FPS · peor fotograma {4:0} ms · {5} fotogramas",
                total, ordenados.Count / total, 1000f * total / ordenados.Count, peores / lentos,
                1000f * ordenados[ordenados.Count - 1], ordenados.Count);
        }

        /// <summary>Añade una linea al registro. Que el disco falle no puede estropear la partida.</summary>
        public void Guardar(string escena) {
            if (_fotogramas.Count < 10) return;
            try {
                var linea = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss} | {1} | {2} | calidad «{3}» · {4}×{5} · vSync {6} | {7} · {8}{9}",
                    DateTime.Now, escena, Resumen(), QualitySettings.names[QualitySettings.GetQualityLevel()],
                    Screen.width, Screen.height, QualitySettings.vSyncCount, SystemInfo.graphicsDeviceName,
                    SystemInfo.processorType, Application.isEditor ? " · en el editor" : "");
                File.AppendAllText(RutaDelRegistro, linea + Environment.NewLine);
            } catch (Exception e) {
                Debug.LogWarning("[Nexus] No se pudo escribir el registro de rendimiento: " + e.Message);
            }
        }
    }
}
