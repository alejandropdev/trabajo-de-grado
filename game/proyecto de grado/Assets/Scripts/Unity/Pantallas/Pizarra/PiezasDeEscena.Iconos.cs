using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) Los iconos de las tarjetas de columnas y de las viñetas del pizarron, en su cuadro
    /// de 160×130, con el lenguaje de las escenas: objetos del mundo (credencial, reloj, servidor, candado…) y señales de
    /// neon. El error es una chispa; la persona, su credencial. Se pintan encima de la tarjeta, no dentro de una vitrina.
    /// </summary>
    public static partial class DibujosDeEscena {
        /// <summary>El color del contenido (los de tiza) traducido a la paleta de la direccion de arte.</summary>
        private static Color Traducir(Color c) {
            if (c == Tizador.Cian) return Nucleo;
            if (c == Tizador.Mostaza) return Aviso;
            if (c == Tizador.Roja) return Coral;
            if (c == Tizador.Gris) return TintaSuave;
            return Tinta;
        }

        /// <summary>Dibuja el icono con el lenguaje nuevo. false si no lo conoce (entonces se pinta el de tiza).</summary>
        public static bool Icono(Tizador v, string icono, Color tiza) {
            var c = Traducir(tiza);
            switch (icono) {
                case "check":
                    v.Halo(80, 65, 56, A(Menta, 0.3f));
                    v.Neon(Pts(52, 66, 72, 86, 110, 42), 3, tiza == Tizador.Tiza ? Menta : c);
                    return true;
                case "cruz":
                    v.Halo(80, 65, 56, A(Coral, 0.3f));
                    v.Neon(Pts(56, 41, 104, 89), 3, Coral);
                    v.Neon(Pts(104, 41, 56, 89), 3, Coral);
                    return true;
                case "bicho":
                    Chispa(v, 80, 64, 30, 0.1f); Chispa(v, 120, 36, 10, 0.5f); Chispa(v, 40, 98, 8, 0.8f);
                    return true;
                case "reloj":
                    Reloj(v, 80, 64, 48, 10, 10, c == Tinta ? Nucleo : c);
                    return true;
                case "persona":
                    Credencial(v, 80, 46, c == Tinta ? Nucleo : c);
                    return true;
                case "equipo":
                    Credencial(v, 40, 58, c == Tinta ? Nucleo : c); Credencial(v, 120, 58, c == Tinta ? Nucleo : c); Credencial(v, 80, 46, c == Tinta ? Aviso : c);
                    return true;
                case "mano":
                    // Una palanca: la decision la toma alguien con la mano.
                    v.Forma(L.Redondeado(40, 96, 80, 22, 8), H(0x46555A), H(0x1A2428));
                    v.Cable(Pts(80, 100, 112, 34), 7, 6, Cromo);
                    v.Halo(114, 30, 26, A(c == Tinta ? Coral : c, 0.4f));
                    v.Forma(L.Elipse(114, 30, 13, 13, 20), Color.Lerp(c == Tinta ? Coral : c, Color.white, 0.3f), c == Tinta ? Coral : c);
                    return true;
                case "pila": {
                    // Tubo de nivel: cristal con la energia que queda dentro.
                    var col = c == Tinta ? Nucleo : c;
                    v.Forma(L.Redondeado(56, 6, 48, 10, 4), Cromo, CromoOscuro);
                    v.Forma(L.Redondeado(60, 16, 40, 104, 14), H(0x0B1E23), H(0x05141A));
                    v.Forma(L.Redondeado(64, 58, 32, 58, 12), Color.Lerp(col, Color.white, 0.3f), col);
                    v.Halo(80, 74, 40, A(col, 0.35f));
                    v.Forma(L.Redondeado(66, 20, 6, 92, 3), A(Color.white, 0.18f));
                    v.Forma(L.Redondeado(56, 118, 48, 10, 4), Cromo, CromoOscuro);
                    return true;
                }
                case "barra-subiendo":
                case "barra-bajando": {
                    var sube = icono == "barra-subiendo";
                    var col = c != Tinta ? c : sube ? Menta : Coral;   // el color de la columna manda (riesgo que sube = mostaza)
                    for (var i = 0; i < 4; i++) {
                        var h = 25 * (sube ? i + 1 : 4 - i);
                        v.Forma(L.Redondeado(22 + i * 32, 118 - h, 24, h, 5), Color.Lerp(col, Color.white, 0.25f), A(col, 0.6f));
                    }
                    v.Halo(sube ? 130 : 30, sube ? 18 : 18, 26, A(col, 0.35f));
                    return true;
                }
                case "flecha":
                    FlechaNeon(v, 22, 65, 140, 65, c == Tinta ? Nucleo : c);
                    return true;
                case "documento":
                    v.Forma(L.Redondeado(50, 12, 66, 108, 4), A(Silueta, 0.5f));
                    v.Forma(L.Redondeado(46, 8, 66, 108, 4), H(0xEDE7D6), H(0xCFC7B0));
                    v.Forma(Caja(56, 22, 36, 4), A(c == Tinta ? H(0x1E2A2A) : c, 0.9f));
                    for (var i = 0; i < 5; i++) v.Forma(Caja(56, 38 + i * 14, i % 2 == 0 ? 46 : 36, 3), A(H(0x1E2A2A), 0.45f));
                    return true;
                case "servidor":
                    v.Forma(L.Redondeado(46, 8, 68, 114, 6), H(0x2B3335), H(0x101416));
                    for (var i = 0; i < 4; i++) {
                        var y = 18 + i * 26;
                        v.Forma(L.Redondeado(52, y, 56, 20, 3), H(0x1A2124));
                        for (var k = 0; k < 3; k++) v.Forma(Caja(58, y + 5 + k * 4, 26, 1.6f), A(TintaSuave, 0.4f));
                        var lc = i == 2 ? Aviso : Menta;
                        var luz = v.Halo(98, y + 10, 8, A(lc, 0.6f));
                        if (i == 1 || i == 3) v.Parpadeo(luz, 0.2f, i * 0.4f);
                        v.Forma(L.Elipse(98, y + 10, 3, 3, 10), Color.Lerp(lc, Color.white, 0.4f));
                    }
                    return true;
                case "candado": {
                    var col = c == Tinta ? Aviso : c;
                    v.Forma(L.Arco(80, 58, 24, 30, 7, 180, 360, 24), Cromo, CromoOscuro);
                    v.Forma(L.Redondeado(48, 54, 64, 56, 8), Color.Lerp(col, Color.white, 0.15f), Color.Lerp(col, Silueta, 0.45f));
                    v.Forma(L.Elipse(80, 76, 6, 6, 14), H(0x1A120C));
                    v.Forma(Pts(77, 78, 83, 78, 85, 96, 75, 96), H(0x1A120C));
                    return true;
                }
                case "tablero":
                    v.Forma(L.Redondeado(6, 10, 148, 110, 8), SuperficieAlta, Superficie);
                    v.Forma(Caja(55, 18, 1.2f, 94), A(Borde, 0.8f)); v.Forma(Caja(105, 18, 1.2f, 94), A(Borde, 0.8f));
                    TarjetaUI(v, 12, 24, 38, 22, Nucleo); TarjetaUI(v, 12, 52, 38, 22, Nucleo);
                    TarjetaUI(v, 61, 24, 38, 22, Aviso); TarjetaUI(v, 111, 24, 38, 22, Menta);
                    return true;
                case "tarjeta":
                    TarjetaUI(v, 24, 38, 112, 54, c == Tinta ? Nucleo : c);
                    return true;
                case "caja":
                    CajaDeCarga(v, 24, 40, 112, 56, H(0x3F7F88));
                    return true;
                case "base-de-datos":
                    for (var i = 2; i >= 0; i--) {
                        var y = 40 + i * 28;
                        v.Forma(L.Redondeado(36, y - 12, 88, 28, 5), H(0x2B4F57), H(0x11262C));
                        v.Forma(L.Elipse(80, y - 12, 44, 10, 24), H(0x3FA3B0), H(0x1E6470));
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}
