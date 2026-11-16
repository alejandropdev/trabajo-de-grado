using System;
using System.Collections.Generic;
using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) «El producto» y lo que cuenta el cliente, en el lenguaje nuevo: cada modulo en su
    /// etapa (un boceto a tinta sobre papel, una maqueta de colores, un prototipo con sus rotulos, la pantalla funcionando),
    /// el cliente con su torre y sus usuarios con su credencial, y los papeles que traen. DibujosDelProducto
    /// (Pizarra/DibujosDelProducto.cs) sigue siendo la fachada; aqui esta el dibujo.
    /// </summary>
    public static partial class DibujosDeEscena {
        private static readonly Color Tinta0 = H(0x1E2A2A);
        private const float Ancho0 = 600, Alto0 = 300;

        // ================================================================ el modulo en su etapa

        /// <summary>Un renglon: tinta sobre papel (0), un bloque de color (1), una linea a trazos (2) o el texto de verdad (3).</summary>
        private static void RenglonDeProducto(Tizador v, int nivel, float x, float y, float w, string texto, bool lleno) {
            if (nivel == 0) { v.Forma(L.Redondeado(x, y - 1, w * 0.8f, 2.4f, 1.2f), A(Tinta0, 0.55f)); return; }
            if (nivel == 1) { v.Forma(L.Redondeado(x, y - 12, w * 0.7f, 16, 5), A(TintaSuave, 0.35f)); return; }
            if (!lleno) { for (var xx = x; xx < x + w; xx += 12) v.Forma(Caja(xx, y, 6, 1.8f), A(TintaSuave, 0.4f)); return; }
            v.Rotulo(x, y - 4, texto, 16, Tinta, -1);
            v.Forma(Caja(x, y + 9, w, 1.2f), A(Borde, 0.9f));
        }

        public static void ProductoModulo(Tizador p, ModuloDelProyecto m, string etapa, int filas, bool pulsado) {
            var nivel = Math.Min(3, EtapasDelProducto.Orden(etapa));   // 0 boceto · 1 mockup · 2 prototipo · 3 funciona
            var datos = m.Datos ?? new List<string>();
            var v = p.Vitrina(Ancho0, Alto0);

            // El marco: un papel con tinta, o la ventana del navegador.
            if (nivel == 0) {
                v.Forma(L.Redondeado(20, 16, 572, 280, 4), A(Silueta, 0.5f));
                v.Forma(L.Redondeado(14, 10, 572, 280, 4), H(0xEDE7D6), H(0xCFC7B0));
                v.Forma(Tizador.Rotar(L.Redondeado(536, 18, 70, 8, 3), 540, 22, -34), Aviso, H(0xB0922E));
            } else Ventana(v, 14, 10, 572, 280, null, nivel == 1 ? TintaSuave : Nucleo);

            switch (m.Pantalla) {
                case "lista": ProdLista(v, nivel, datos, filas); break;
                case "aviso": ProdAviso(v, nivel, datos, filas); break;
                case "lector": ProdLector(v, nivel, datos, filas); break;
                case "calendario": ProdCalendario(v, nivel, datos, filas); break;
                case "grafica": ProdGrafica(v, nivel, datos, filas); break;
                default: ProdFormulario(v, nivel, datos, filas); break;
            }

            // El boton de lo que se hace aqui.
            if (!string.IsNullOrEmpty(m.Accion)) {
                const float bx = 380, by = 236, bw = 190, bh = 42;
                if (nivel == 0) Contorno(v, Caja(bx, by, bw, bh), 1.6f, A(Tinta0, 0.6f));
                else if (nivel == 1) v.Forma(L.Redondeado(bx, by, bw, bh, bh / 2), A(Aviso, 0.5f));
                else BotonUI(v, bx, by, bw, bh, m.Accion, Aviso, pulsado);
            }
            if (etapa == EtapasDelProducto.Desplegado) SelloTexto(v, 108, 262, "EN USO", Nucleo, 96);
        }

        private static void ProdFormulario(Tizador v, int nivel, List<string> campos, int filas) {
            for (var i = 0; i < Math.Min(3, campos.Count); i++) {
                var y = 62 + i * 56;
                if (nivel == 0) { v.Forma(Caja(40, y + 8, 110, 2), A(Tinta0, 0.5f)); Contorno(v, Caja(40, y + 16, 300, 26), 1.4f, A(Tinta0, 0.55f)); continue; }
                if (nivel == 1) { v.Forma(L.Redondeado(40, y, 120, 12, 5), A(TintaSuave, 0.35f)); v.Forma(L.Redondeado(40, y + 16, 300, 26, 7), A(Nucleo, 0.25f)); continue; }
                v.Rotulo(40, y + 8, campos[i], 15, TintaSuave, -1);
                v.Forma(L.Redondeado(40, y + 16, 300, 26, 7), Pozo);
                Contorno(v, L.Redondeado(40, y + 16, 300, 26, 7), 1.2f, A(Borde, 0.9f));
                if (nivel == 3 && filas > 0) v.Rotulo(52, y + 29, i == 0 ? "Ana R." : i == 1 ? "Carlos P." : "Sí", 15, Nucleo, -1);
            }
            if (nivel == 3 && filas > 0) { NeonCheck(v, 420, 110, 20); v.Rotulo(470, 160, "guardado", 17, Menta); }
        }

        private static void ProdLista(Tizador v, int nivel, List<string> datos, int filas) {
            var n = Math.Min(4, Math.Max(3, datos.Count));
            for (var i = 0; i < n; i++) {
                var lleno = nivel == 3 && i < Math.Min(filas, datos.Count);
                RenglonDeProducto(v, nivel, 40, 78 + i * 44, 500, i < datos.Count ? datos[i] : "", lleno);
            }
            if (nivel == 2) v.Rotulo(300, 256, "la lista está, pero vacía: aún no guarda", 14, TintaSuave);
            if (nivel == 3 && filas == 0) v.Rotulo(300, 256, "todavía no ha entrado nadie", 14, TintaSuave);
        }

        private static void ProdAviso(Tizador v, int nivel, List<string> datos, int filas) {
            // un sobre a la izquierda y el mensaje a la derecha
            var c = nivel == 0 ? A(Tinta0, 0.6f) : nivel == 1 ? A(Aviso, 0.5f) : Aviso;
            if (nivel == 1) v.Forma(L.Redondeado(50, 90, 130, 90, 6), c);
            else { Contorno(v, Caja(50, 90, 130, 90), 1.8f, c); Contorno(v, Pts(50, 90, 115, 140, 180, 90), 1.8f, c); }
            for (var i = 0; i < 2; i++) RenglonDeProducto(v, nivel, 220, 110 + i * 46, 330, i < datos.Count ? datos[i] : "", nivel == 3 && filas > 0);
            if (nivel == 3 && filas > 0) v.Rotulo(115, 216, "enviado", 16, Menta);
            if (nivel == 3 && filas == 0) v.Rotulo(300, 256, "esperando a que llegue una visita", 14, TintaSuave);
        }

        private static void ProdLector(Tizador v, int nivel, List<string> datos, int filas) {
            var c = nivel == 0 ? A(Tinta0, 0.6f) : nivel == 1 ? A(TintaSuave, 0.6f) : Aviso;
            CamaraVigilancia(v, 56, 70, c);
            var placa = nivel >= 2 ? (nivel == 3 && filas > 0 && datos.Count > 0 ? datos[(filas - 1) % datos.Count] : "··· ···") : null;
            if (nivel == 0) Contorno(v, Caja(180, 60, 220, 70), 1.6f, A(Tinta0, 0.6f));
            else v.Forma(L.Redondeado(180, 60, 220, 70, 6), nivel == 1 ? A(TintaSuave, 0.3f) : H(0xEDE7D6));
            if (placa != null) v.Rotulo(290, 95, placa, 28, Tinta0, 0, false, true);
            if (nivel == 3 && filas > 0 && datos.Count > 0) {
                FlechaNeon(v, 410, 95, 450, 95, Nucleo);
                v.Rotulo(510, 95, "\"" + datos[(filas - 1) % datos.Count].Replace(" ", "") + "\"", 20, Nucleo, 0, false, true);
            }
            RenglonDeProducto(v, nivel, 50, 190, 300, "matrículas leídas: " + (nivel == 3 ? filas.ToString() : "—"), nivel == 3);
        }

        private static void ProdCalendario(Tizador v, int nivel, List<string> datos, int filas) {
            if (nivel == 0) Contorno(v, Caja(40, 56, 320, 170), 1.6f, A(Tinta0, 0.55f));
            else PanelUI(v, 40, 56, 320, 170);
            for (var i = 1; i < 3; i++) v.Forma(Caja(44, 56 + i * 56.6f, 312, 1.2f), nivel == 0 ? A(Tinta0, 0.4f) : A(Borde, 0.9f));
            for (var i = 0; i < 3; i++) {
                var y = 62 + i * 56.6f;
                if (nivel == 1) v.Forma(L.Redondeado(50, y, 220 - i * 40, 40, 6), A(i == 1 ? Aviso : Nucleo, 0.4f));
                if (nivel == 2) v.Rotulo(54, y + 22, i < datos.Count ? datos[i].Split(' ')[0] : "", 16, TintaSuave, -1);
                if (nivel == 3 && i < datos.Count) {
                    var ocupado = i < filas;
                    if (ocupado) v.Forma(L.Redondeado(50, y, 300, 40, 6), A(Nucleo, 0.35f));
                    v.Rotulo(60, y + 22, ocupado ? datos[i] : datos[i].Split(' ')[0] + "  libre", 16, ocupado ? Tinta : TintaSuave, -1);
                }
            }
        }

        private static void ProdGrafica(Tizador v, int nivel, List<string> datos, int filas) {
            if (nivel == 0) {
                v.Forma(Caja(60, 60, 2, 160), A(Tinta0, 0.55f)); v.Forma(Caja(60, 218, 300, 2), A(Tinta0, 0.55f));
                v.Neon(Tizador.Bezier(new Vector2(70, 200), new Vector2(200, 80), new Vector2(350, 120), 20), 0.9f, A(Tinta0, 0.7f));
                return;
            }
            if (nivel == 1) {
                var altos = new[] { 0.5f, 0.7f, 0.4f, 0.8f };
                for (var i = 0; i < 4; i++) v.Forma(L.Redondeado(70 + i * 72, 220 - 150 * altos[i], 50, 150 * altos[i], 6), A(TintaSuave, 0.4f));
                return;
            }
            PanelUI(v, 50, 54, 320, 176);
            if (nivel == 2) v.Rotulo(210, 150, "sin datos todavía", 16, TintaSuave);
            else {
                var altos = new[] { 0.35f, 0.6f, 0.5f, Mathf.Min(1f, 0.3f + filas * 0.15f) };
                for (var i = 0; i < 4; i++) v.Forma(L.Redondeado(70 + i * 72, 220 - 150 * altos[i], 50, 150 * altos[i], 6), Color.Lerp(Nucleo, Color.white, 0.2f), A(Nucleo, 0.6f));
            }
            for (var i = 0; i < Math.Min(2, datos.Count); i++) v.Rotulo(400, 100 + i * 40, datos[i], 15, Tinta, -1);
        }

        // ================================================================ el cliente, en dibujos

        /// <summary>«Quién es tu cliente», a página completa: tres tarjetas de chapa (quién es, qué le duele, cómo se trabaja ahí).</summary>
        public static void ProductoCliente(Tizador t, FichaDelProyecto f, float y) {
            if (f == null || f.Cliente == null) return;
            const float hueco = 24, alto = 470, W = 1612;
            var ancho = (W - 120 - hueco * 2) / 3;
            var y0 = y + 50;
            var titulos = new[] { "QUIÉN ES", "QUÉ LE DUELE HOY", "CÓMO SE TRABAJA AHÍ" };
            var colores = new[] { Nucleo, Coral, Aviso };
            var nombres = new[] { f.Cliente.Nombre, null, f.Sector != null ? f.Sector.Nombre : null };
            var textos = new[] { f.Cliente.QueHace, f.Cliente.QueLeDuele, f.Sector != null ? f.Sector.ComoTrabaja : "" };
            for (var i = 0; i < 3; i++) {
                var x = 60 + i * (ancho + hueco);
                t.TarjetaDeChapa(x, y0, ancho, alto);
                t.Rotulo(x + 24, y0 + 32, titulos[i], 18, colores[i], -1, true);
                var d = t.En(x + ancho / 2 - 110, y0 + 56);
                if (i == 0) {            // la torre del cliente y su gente
                    TorreDelCliente(d, 70, 150, 130, 0.8f, colores[0]); Credencial(d, 150, 66, Tinta); Credencial(d, 196, 66, Tinta);
                } else if (i == 1) {     // la fila que espera, y el reloj
                    Credencial(d, 30, 64, Coral); Credencial(d, 76, 64, Coral); Credencial(d, 122, 64, Coral); Reloj(d, 188, 70, 26, 11, 50, Coral);
                } else {                 // el mostrador: de pie y con prisa
                    Escritorio(d, 6, 214, 134, Aviso); Credencial(d, 110, 24, Aviso); Papel(d, 24, 96, 40, 34); RelojDeArena(d, 176, 86, 44, Aviso, 0.3f);
                }
                var ty = y0 + 240;
                if (!string.IsNullOrEmpty(nombres[i])) { t.Rotulo(x + 24, ty + 8, nombres[i], 24, Tinta, -1, false, false, ancho - 48); ty += nombres[i].Length > 34 ? 76 : 44; }
                t.Texto(x + 24, ty, textos[i] ?? "", 21, TintaSuave, false, ancho - 48);
            }
        }

        /// <summary>«Lo que dicen hoy del producto»: por cada usuario, su credencial, su parte del sistema tal como está hoy y su frase.</summary>
        public static void ProductoVoces(Tizador t, List<VozDelCliente> voces, List<EstadoDelModulo> modulos, float y) {
            if (voces == null || voces.Count == 0) return;
            const float hueco = 20, alto = 500, W = 1612;
            var n = Math.Min(4, voces.Count);
            var ancho = Math.Min(470, (W - 120 - hueco * (n - 1)) / n);
            var x0 = (W - (ancho * n + hueco * (n - 1))) / 2;
            var y0 = y + 44;
            for (var i = 0; i < n; i++) {
                var voz = voces[i];
                var x = x0 + i * (ancho + hueco);
                var color = voz.Animo > 0 ? Nucleo : voz.Animo < 0 ? Coral : Aviso;
                t.TarjetaDeChapa(x, y0, ancho, alto);
                Credencial(t, x + 44, y0 + 52, color);
                t.Rotulo(x + 84, y0 + 36, voz.Quien ?? "", 20, color, -1, false, false, ancho - 100);
                var estado = modulos == null ? null : modulos.Find(m => m.Modulo.Id == voz.ModuloId);
                var k = (ancho - 36) / Ancho0;
                if (estado != null) {
                    ProductoModulo(t.En(x + 18, y0 + 118, k), estado.Modulo, estado.Etapa, 2, false);
                    t.Rotulo(x + ancho / 2, y0 + 118 + Alto0 * k + 20, $"{estado.Modulo.Nombre} · {EtapasDelProducto.Nombre(estado.Etapa)}", 17, TintaSuave);
                }
                var ty = y0 + 118 + Alto0 * k + 50;
                t.Texto(x + 20, ty, "«" + voz.FraseCorta + "»", 23, Tinta, false, ancho - 40);
                if (!string.IsNullOrEmpty(voz.Necesita)) t.Texto(x + 20, ty + 74, "Necesita: " + voz.Necesita, 18, TintaSuave, false, ancho - 40);
            }
        }

        // ================================================================ los papeles del cliente

        /// <summary>Un papel del cliente, dibujado como lo que es: una hoja de cuaderno, una queja, un correo, una hoja de datos o una nota.</summary>
        public static void ProductoPapel(Tizador p, PapelDelCliente papel) {
            var v = p.Vitrina(Ancho0, Alto0);
            var lineas = papel.Lineas ?? new List<string>();
            const float x = 24, w = 552;
            switch (papel.Tipo) {
                case "cuaderno":
                    v.Forma(L.Redondeado(x + 6, 16, w, 280, 4), A(Silueta, 0.5f)); v.Forma(L.Redondeado(x, 10, w, 280, 4), H(0xEDE7D6), H(0xCFC7B0));
                    for (var i = 0; i < 7; i++) { v.Forma(L.Elipse(x + 6, 34 + i * 38, 6, 6, 12), H(0x2B3335)); v.Forma(L.Redondeado(x + 2, 32 + i * 38, 14, 3, 1.5f), Cromo); }
                    v.Forma(Caja(x + 44, 10, 2, 280), A(Coral, 0.8f));
                    for (var i = 0; i < Math.Min(5, lineas.Count); i++) {
                        v.Rotulo(x + 56, 62 + i * 50, lineas[i], 17, H(0x1E4A5A), -1);
                        v.Forma(Caja(x + 46, 72 + i * 50, w - 46, 1.2f), A(Tinta0, 0.25f));
                    }
                    break;
                case "queja":
                    v.Forma(L.Redondeado(x + 6, 16, w, 220, 4), A(Silueta, 0.5f)); v.Forma(L.Redondeado(x, 10, w, 220, 4), H(0xEDE7D6), H(0xCFC7B0));
                    Contorno(v, Caja(x + 6, 16, w - 12, 208), 2.4f, A(Coral, 0.85f));
                    v.Forma(Pts(x + 60, 230, x + 40, 262, x + 110, 230), H(0xCFC7B0));
                    for (var i = 0; i < Math.Min(4, lineas.Count); i++) v.Rotulo(x + 24, 56 + i * 46, lineas[i], 17, Tinta0, -1);
                    Sello(v, x + w - 40, 258, 26, Coral);
                    break;
                case "correo":
                    PanelUI(v, x, 10, w, 280);
                    v.Forma(L.Redondeado(x + 16, 24, 60, 40, 5), A(Aviso, 0.9f)); Contorno(v, Pts(x + 16, 24, x + 46, 48, x + 76, 24), 1.8f, H(0x241C02));
                    v.Rotulo(x + 92, 44, "De: " + (papel.De ?? ""), 16, Aviso, -1);
                    v.Forma(Caja(x + 16, 78, w - 32, 1.2f), A(Borde, 0.9f));
                    for (var i = 0; i < Math.Min(4, lineas.Count); i++) v.Rotulo(x + 24, 112 + i * 46, lineas[i], 17, Tinta, -1);
                    break;
                case "hoja":
                    v.Forma(L.Redondeado(x + 6, 16, w, 280, 4), A(Silueta, 0.5f)); v.Forma(L.Redondeado(x, 10, w, 280, 4), H(0xEDE7D6), H(0xCFC7B0));
                    v.Forma(L.Redondeado(x, 10, w, 14, 4), Aviso);
                    for (var i = 0; i < Math.Min(5, lineas.Count); i++) {
                        v.Rotulo(x + 24, 62 + i * 50, lineas[i], 18, Tinta0, -1);
                        if (i < lineas.Count - 1) for (var xx = x + 16; xx < x + w - 16; xx += 12) v.Forma(Caja(xx, 80 + i * 50, 6, 1.2f), A(Tinta0, 0.3f));
                    }
                    break;
                default:   // nota
                    v.Forma(L.Redondeado(x + 26, 26, w - 40, 250, 3), A(Silueta, 0.4f)); v.Forma(L.Redondeado(x + 20, 20, w - 40, 250, 3), H(0xF2D56A), H(0xD9B84A));
                    v.Halo(x + w / 2, 22, 14, A(Coral, 0.5f)); v.Forma(L.Elipse(x + w / 2, 22, 8, 8, 14), Color.Lerp(Coral, Color.white, 0.3f), H(0x8A2E1E));
                    for (var i = 0; i < Math.Min(4, lineas.Count); i++) v.Rotulo(x + 44, 80 + i * 48, lineas[i], 17, H(0x3A2A0E), -1);
                    break;
            }
        }
    }
}
