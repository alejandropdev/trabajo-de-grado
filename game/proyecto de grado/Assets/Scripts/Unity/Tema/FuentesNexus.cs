using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Tema {
    /// <summary>
    /// Las cuatro familias del design system, cada una con un trabajo fijo:
    ///   Display   (Orbitron)       marca: titulos de fase, encabezados de panel, cifras de StatTile
    ///   Interfaz  (Rajdhani)       toda la UI y el dialogo
    ///   Codigo    (JetBrains Mono) logs, codigo, consola
    ///   Documento (Spectral)       solo documentos del mundo (papel)
    ///
    /// Se crean en tiempo de ejecucion desde los TTF de Resources/Fuentes (licencia OFL), igual que la tiza de las
    /// recetas: no hace falta generar atlas a mano. Cada peso es su propio archivo y se cuelga de la tabla de pesos de
    /// TMP, asi que FontWeight.SemiBold o FontStyles.Bold usan la letra real y no una negrita falsa. Si falta un
    /// archivo, se usa la fuente por defecto de TMP: el juego se lee igual, solo pierde su tipografia.
    /// </summary>
    public static class FuentesNexus {
        private static TMP_FontAsset _display, _interfaz, _interfazNegrita, _codigo, _documento;
        private static bool _cargadas;

        public static TMP_FontAsset Display { get { Cargar(); return _display; } }
        public static TMP_FontAsset Interfaz { get { Cargar(); return _interfaz; } }
        /// <summary>Rajdhani Bold como fuente propia: para etiquetas y botones, donde todo el texto va a 700.</summary>
        public static TMP_FontAsset InterfazNegrita { get { Cargar(); return _interfazNegrita; } }
        public static TMP_FontAsset Codigo { get { Cargar(); return _codigo; } }
        public static TMP_FontAsset Documento { get { Cargar(); return _documento; } }

        private static void Cargar() {
            if (_cargadas) return;
            _cargadas = true;
            var reserva = TMP_Settings.defaultFontAsset;

            _interfaz = Crear("Rajdhani-Medium", reserva);
            Peso(_interfaz, 6, "Rajdhani-SemiBold");
            Peso(_interfaz, 7, "Rajdhani-Bold");

            _interfazNegrita = Crear("Rajdhani-Bold", reserva);

            // Las demas caen en Rajdhani antes que en la de TMP: un glifo que falte se ve de la misma familia de UI.
            var reservaUi = _interfaz != reserva ? _interfaz : reserva;

            _display = Crear("Orbitron-Bold", reservaUi);
            Peso(_display, 8, "Orbitron-ExtraBold");

            _codigo = Crear("JetBrainsMono-Regular", reservaUi);
            Peso(_codigo, 6, "JetBrainsMono-SemiBold");

            _documento = Crear("Spectral-Regular", reservaUi);
            Peso(_documento, 7, "Spectral-Bold");
            var cursiva = Crear("Spectral-Italic", null, false);
            if (cursiva != null && _documento != reserva) _documento.fontWeightTable[4].italicTypeface = cursiva;
        }

        private static TMP_FontAsset Crear(string archivo, TMP_FontAsset reserva, bool usarReserva = true) {
            var ttf = Resources.Load<Font>("Fuentes/" + archivo);
            if (ttf == null) {
                Debug.LogWarning("[Nexus] Falta la fuente Resources/Fuentes/" + archivo + ".ttf; se usa la de TMP.");
                return usarReserva ? reserva : null;
            }
            var fa = TMP_FontAsset.CreateFontAsset(ttf);
            if (fa == null) return usarReserva ? reserva : null;
            fa.name = archivo;
            if (reserva != null) fa.fallbackFontAssetTable = new List<TMP_FontAsset> { reserva };
            return fa;
        }

        private static void Peso(TMP_FontAsset baseFa, int indice, string archivo) {
            if (baseFa == null || baseFa == TMP_Settings.defaultFontAsset) return;
            var fa = Crear(archivo, TMP_Settings.defaultFontAsset, false);
            if (fa == null) return;
            baseFa.fontWeightTable[indice].regularTypeface = fa;
        }
    }
}
