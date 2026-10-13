using System;
using System.Collections.Generic;
using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «El producto»: cada modulo del sistema, dibujado en la etapa en que esta. La misma pantalla se ve como unas
    /// rayas a lapiz (boceto), como una imagen de colores (mockup), con sus rotulos y botones (prototipo) o con
    /// datos de verdad (MVP y en produccion). Tambien los papeles del cliente. Todo en un cuadro de 600×300, como
    /// las escenas, para poder ponerlo grande o en una rejilla.
    /// </summary>
    public static class DibujosDelProducto {
        public const float Ancho = 600, Alto = 300;

        /// <summary>
        /// Un modulo en su etapa. 'filas' = cuantos datos de ejemplo se ven ya (lo que el jugador ha ido haciendo en
        /// la maqueta); 'pulsado' = acaba de usarse (el boton responde).
        /// </summary>
        public static void Modulo(Tizador p, ModuloDelProyecto m, string etapa, int filas, bool pulsado) {
            var nivel = Math.Min(3, EtapasDelProducto.Orden(etapa));   // 0 boceto · 1 mockup · 2 prototipo · 3 funciona
            var datos = m.Datos ?? new List<string>();

            // El marco: un papel a lapiz, o la ventana del navegador.
            if (nivel == 0) {
                p.Caja(14, 10, 572, 280, null, Tizador.Gris, true);
                p.Linea(540, 34, 570, 12, Tizador.Mostaza, 5);   // el lapiz
            } else {
                p.Navegador(14, 10, 572, 280, nivel == 1 ? Tizador.Tiza : Tizador.Cian);
            }

            switch (m.Pantalla) {
                case "lista": Lista(p, nivel, datos, filas); break;
                case "aviso": Aviso(p, nivel, datos, filas); break;
                case "lector": Lector(p, nivel, datos, filas); break;
                case "calendario": Calendario(p, nivel, datos, filas); break;
                case "grafica": Grafica(p, nivel, datos, filas); break;
                default: Formulario(p, nivel, datos, filas); break;
            }

            // El boton de lo que se hace aqui.
            if (!string.IsNullOrEmpty(m.Accion)) {
                const float bx = 380, by = 236, bw = 190, bh = 42;
                if (nivel == 0) p.Caja(bx, by, bw, bh, null, Tizador.Gris, true);
                else if (nivel == 1) p.Bloque(bx, by, bw, bh, Tizador.Mostaza);
                else {
                    if (pulsado) p.Bloque(bx, by, bw, bh, Tizador.Mostaza);
                    p.Caja(bx, by, bw, bh, m.Accion, pulsado ? Tizador.Pizarra : Tizador.Mostaza, false, 17);
                }
            }
            if (etapa == EtapasDelProducto.Desplegado) p.Sello(108, 262, "EN USO", Tizador.Cian);
        }

        private static Color Tenue(Color c) { c.a = 0.55f; return c; }

        /// <summary>Un renglon: a lapiz, un bloque de color, o el texto de verdad.</summary>
        private static void Renglon(Tizador p, int nivel, float x, float y, float w, string texto, bool lleno) {
            if (nivel == 0) { p.Linea(x, y, x + w * 0.8f, y, Tizador.Gris, 2, true); return; }
            if (nivel == 1) { p.Bloque(x, y - 12, w * 0.7f, 16, Tenue(Tizador.Tiza)); return; }
            if (!lleno) { p.Linea(x, y, x + w, y, Tizador.Gris, 2, true); return; }
            p.Texto(x, y, texto, 17, Tizador.Tiza);
            p.Linea(x, y + 9, x + w, y + 9, Tizador.Gris, 1.5f);
        }

        private static void Formulario(Tizador p, int nivel, List<string> campos, int filas) {
            for (var i = 0; i < Math.Min(3, campos.Count); i++) {
                var y = 62 + i * 56;
                if (nivel == 0) { p.Linea(40, y + 8, 150, y + 8, Tizador.Gris, 2, true); p.Caja(40, y + 16, 300, 26, null, Tizador.Gris, true); continue; }
                if (nivel == 1) { p.Bloque(40, y, 120, 12, Tenue(Tizador.Tiza)); p.Bloque(40, y + 16, 300, 26, Tenue(Tizador.Cian)); continue; }
                p.Texto(40, y + 10, campos[i], 15, Tizador.Gris);
                p.Caja(40, y + 16, 300, 26, null, Tizador.Tiza);
                // Funcionando: los campos llevan lo que se escribio.
                if (nivel == 3 && filas > 0) p.Texto(50, y + 35, i == 0 ? "Ana R." : i == 1 ? "Carlos P." : "Sí", 15, Tizador.Cian);
            }
            if (nivel == 3 && filas > 0) { p.Si(420, 110, null, 20); p.Texto(470, 160, "guardado", 17, Tizador.Cian, true); }
        }

        private static void Lista(Tizador p, int nivel, List<string> datos, int filas) {
            var n = Math.Min(4, Math.Max(3, datos.Count));
            for (var i = 0; i < n; i++) {
                var y = 78 + i * 44;
                var lleno = nivel == 3 && i < Math.Min(filas, datos.Count);
                Renglon(p, nivel, 40, y, 500, i < datos.Count ? datos[i] : "", lleno);
            }
            if (nivel == 2) p.Texto(200, 256, "la lista está, pero vacía: aún no guarda", 14, Tizador.Gris, true);
            if (nivel == 3 && filas == 0) p.Texto(200, 256, "todavía no ha entrado nadie", 14, Tizador.Gris, true);
        }

        private static void Aviso(Tizador p, int nivel, List<string> datos, int filas) {
            // un sobre a la izquierda y el mensaje a la derecha
            if (nivel == 0) { p.Caja(50, 90, 130, 90, null, Tizador.Gris, true); p.Trazo(Tizador.Gris, 50, 90, 115, 140, 180, 90); }
            else if (nivel == 1) { p.Bloque(50, 90, 130, 90, Tenue(Tizador.Mostaza)); }
            else { p.Caja(50, 90, 130, 90, null, Tizador.Mostaza); p.Trazo(Tizador.Mostaza, 50, 90, 115, 140, 180, 90); }
            for (var i = 0; i < 2; i++) Renglon(p, nivel, 220, 110 + i * 46, 330, i < datos.Count ? datos[i] : "", nivel == 3 && filas > 0);
            if (nivel == 3 && filas > 0) p.Texto(115, 216, "enviado", 16, Tizador.Cian, true);
            if (nivel == 3 && filas == 0) p.Texto(200, 256, "esperando a que llegue una visita", 14, Tizador.Gris, true);
        }

        private static void Lector(Tizador p, int nivel, List<string> datos, int filas) {
            var c = nivel == 0 ? Tizador.Gris : nivel == 1 ? Tenue(Tizador.Tiza) : Tizador.Mostaza;
            p.Camara(50, 70, c);
            p.Caja(180, 60, 220, 70, nivel >= 2 ? (nivel == 3 && filas > 0 && datos.Count > 0 ? datos[(filas - 1) % datos.Count] : "··· ···") : null,
                   nivel == 0 ? Tizador.Gris : Tizador.Tiza, nivel == 0, 30);
            p.Linea(134, 92, 176, 92, c, 2, true);
            if (nivel == 3 && filas > 0 && datos.Count > 0) {
                p.Flecha(410, 95, 450, 95, Tizador.Cian);
                p.Texto(510, 102, "\"" + datos[(filas - 1) % datos.Count].Replace(" ", "") + "\"", 20, Tizador.Cian, true);
            }
            Renglon(p, nivel, 50, 190, 300, "matrículas leídas: " + (nivel == 3 ? filas.ToString() : "—"), nivel == 3);
        }

        private static void Calendario(Tizador p, int nivel, List<string> datos, int filas) {
            var c = nivel == 0 ? Tizador.Gris : Tizador.Tiza;
            p.Caja(40, 56, 320, 170, null, c, nivel == 0);
            for (var i = 1; i < 3; i++) p.Linea(40, 56 + i * 56.6f, 360, 56 + i * 56.6f, Tizador.Gris, 2, nivel == 0);
            for (var i = 0; i < 3; i++) {
                var y = 62 + i * 56.6f;
                if (nivel == 1) p.Bloque(50, y, 220 - i * 40, 40, Tenue(i == 1 ? Tizador.Mostaza : Tizador.Cian));
                if (nivel == 2) p.Texto(54, y + 30, i < datos.Count ? datos[i].Split(' ')[0] : "", 16, Tizador.Gris);
                if (nivel == 3 && i < datos.Count) {
                    var ocupado = i < filas;
                    if (ocupado) p.Bloque(50, y, 300, 40, Tenue(Tizador.Cian));
                    p.Texto(60, y + 28, ocupado ? datos[i] : datos[i].Split(' ')[0] + "  libre", 16, Tizador.Tiza);
                }
            }
        }

        private static void Grafica(Tizador p, int nivel, List<string> datos, int filas) {
            if (nivel == 0) { p.Ejes(60, 60, 300, 160, Tizador.Gris); p.Curva(70, 200, 200, 80, 350, 120, Tizador.Gris, false); return; }
            if (nivel == 1) { p.Barras(60, 60, 300, 160, Tenue(Tizador.Tiza), 0.5f, 0.7f, 0.4f, 0.8f); return; }
            if (nivel == 2) { p.Ejes(60, 60, 300, 160, Tizador.Gris); p.Texto(210, 150, "sin datos todavía", 16, Tizador.Gris, true); }
            else p.Barras(60, 60, 300, 160, Tizador.Cian, 0.35f, 0.6f, 0.5f, Mathf.Min(1f, 0.3f + filas * 0.15f));
            for (var i = 0; i < Math.Min(2, datos.Count); i++) p.Texto(400, 100 + i * 40, datos[i], 15, Tizador.Tiza);
        }

        // ==================================================================== el cliente, en dibujos

        private const float AnchoDePizarra = 1612;

        /// <summary>
        /// «Quién es tu cliente», a pagina completa: tres cuadros dibujados (quien es, que le duele, como trabaja su
        /// sector) con una frase cada uno, en vez de un parrafo. 'y' = hasta donde llego el texto de arriba.
        /// </summary>
        public static void Cliente(Tizador t, FichaDelProyecto f, float y) {
            if (f == null || f.Cliente == null) return;
            const float hueco = 24, alto = 470;
            var ancho = (AnchoDePizarra - 120 - hueco * 2) / 3;
            var y0 = y + 50;
            var titulos = new[] { "QUIÉN ES", "QUÉ LE DUELE HOY", "CÓMO SE TRABAJA AHÍ" };
            var colores = new[] { Tizador.Cian, Tizador.Roja, Tizador.Mostaza };
            var nombres = new[] { f.Cliente.Nombre, null, f.Sector != null ? f.Sector.Nombre : null };
            var textos = new[] { f.Cliente.QueHace, f.Cliente.QueLeDuele, f.Sector != null ? f.Sector.ComoTrabaja : "" };
            for (var i = 0; i < 3; i++) {
                var x = 60 + i * (ancho + hueco);
                t.Caja(x, y0, ancho, alto, null, Tizador.Gris, true);
                t.Texto(x + 24, y0 + 40, titulos[i], 24, colores[i]);
                var d = t.En(x + ancho / 2 - 110, y0 + 60);
                if (i == 0) {            // el edificio y su gente
                    d.Edificio(20, 10, 90, 130, Tizador.Cian); d.Persona(150, 70, Tizador.Tiza); d.Persona(195, 70, Tizador.Tiza);
                } else if (i == 1) {     // la fila que espera, y el reloj
                    d.Persona(20, 60, Tizador.Roja); d.Persona(65, 60, Tizador.Roja); d.Persona(110, 60, Tizador.Roja);
                    d.Reloj(180, 70); d.Carita(180, 20, 18, -1, Tizador.Roja);
                } else {                 // el mostrador: de pie y con prisa
                    d.Caja(10, 80, 200, 50, null, Tizador.Mostaza); d.Persona(110, 20, Tizador.Mostaza); d.Hoja(30, 88, 40, 34, Tizador.Tiza); d.RelojDeArena(175, 86, 36, Tizador.Mostaza);
                }
                var ty = y0 + 240;
                if (!string.IsNullOrEmpty(nombres[i])) { t.Texto(x + 24, ty, nombres[i], 24, Tizador.Tiza, false, ancho - 48); ty += nombres[i].Length > 34 ? 76 : 44; }
                t.Texto(x + 24, ty, textos[i] ?? "", 21, Tizador.Gris, false, ancho - 48);
            }
        }

        /// <summary>
        /// «Lo que dicen hoy del producto»: por cada usuario del cliente, su cara, su parte del sistema tal como esta
        /// hoy y una frase corta. Lo que necesita, debajo y en pequeño.
        /// </summary>
        public static void Voces(Tizador t, List<VozDelCliente> voces, List<EstadoDelModulo> modulos, float y) {
            if (voces == null || voces.Count == 0) return;
            const float hueco = 20, alto = 500;
            var n = Math.Min(4, voces.Count);
            var ancho = Math.Min(470, (AnchoDePizarra - 120 - hueco * (n - 1)) / n);
            var x0 = (AnchoDePizarra - (ancho * n + hueco * (n - 1))) / 2;
            var y0 = y + 44;
            for (var i = 0; i < n; i++) {
                var v = voces[i];
                var x = x0 + i * (ancho + hueco);
                var color = v.Animo > 0 ? Tizador.Cian : v.Animo < 0 ? Tizador.Roja : Tizador.Mostaza;
                t.Caja(x, y0, ancho, alto, null, Tizador.Gris, true);
                t.Carita(x + 44, y0 + 44, 24, v.Animo, color);
                t.Texto(x + 84, y0 + 52, v.Quien ?? "", 23, color, false, ancho - 100);
                var estado = modulos == null ? null : modulos.Find(m => m.Modulo.Id == v.ModuloId);
                var k = (ancho - 36) / Ancho;
                if (estado != null) {
                    Modulo(t.En(x + 18, y0 + 88, k), estado.Modulo, estado.Etapa, 2, false);
                    t.Texto(x + ancho / 2, y0 + 88 + Alto * k + 26, $"{estado.Modulo.Nombre} · {EtapasDelProducto.Nombre(estado.Etapa)}", 19, Tizador.Gris, true);
                }
                var ty = y0 + 88 + Alto * k + 70;
                t.Texto(x + 20, ty, "«" + v.FraseCorta + "»", 23, Tizador.Tiza, false, ancho - 40);
                if (!string.IsNullOrEmpty(v.Necesita)) t.Texto(x + 20, ty + 74, "Necesita: " + v.Necesita, 18, Tizador.Gris, false, ancho - 40);
            }
        }

        // ==================================================================== los papeles del cliente

        /// <summary>Un papel del cliente, dibujado como lo que es: una hoja de cuaderno, una queja, un correo, una hoja de datos o una nota.</summary>
        public static void Papel(Tizador p, PapelDelCliente papel) {
            var lineas = papel.Lineas ?? new List<string>();
            const float x = 24, w = 552;
            switch (papel.Tipo) {
                case "cuaderno":
                    p.Caja(x, 10, w, 280, null, Tizador.Tiza);
                    for (var i = 0; i < 7; i++) p.Circulo(x + 4, 34 + i * 38, 7, Tizador.Gris, false, 2);   // la espiral
                    p.Linea(x + 44, 10, x + 44, 290, Tizador.Roja, 2);                                       // el margen
                    for (var i = 0; i < Math.Min(5, lineas.Count); i++) {
                        p.Texto(x + 56, 62 + i * 50, lineas[i], 17, Tizador.Tiza);
                        p.Linea(x + 44, 72 + i * 50, x + w, 72 + i * 50, Tizador.Gris, 1.5f);
                    }
                    break;
                case "queja":
                    p.Caja(x, 10, w, 220, null, Tizador.Roja);
                    p.Trazo(Tizador.Roja, x + 60, 230, x + 40, 262, x + 110, 230);
                    for (var i = 0; i < Math.Min(4, lineas.Count); i++) p.Texto(x + 24, 56 + i * 46, lineas[i], 17, Tizador.Tiza);
                    p.Carita(x + w - 30, 264, 24, -1, Tizador.Roja);
                    break;
                case "correo":
                    p.Caja(x, 10, w, 280, null, Tizador.Cian);
                    p.Caja(x + 16, 24, 60, 40, null, Tizador.Mostaza); p.Trazo(Tizador.Mostaza, x + 16, 24, x + 46, 48, x + 76, 24);
                    p.Texto(x + 92, 52, "De: " + (papel.De ?? ""), 16, Tizador.Mostaza);
                    p.Linea(x + 16, 78, x + w - 16, 78, Tizador.Gris, 2);
                    for (var i = 0; i < Math.Min(4, lineas.Count); i++) p.Texto(x + 24, 116 + i * 46, lineas[i], 17, Tizador.Tiza);
                    break;
                case "hoja":
                    p.Caja(x, 10, w, 280, null, Tizador.Tiza);
                    p.Bloque(x, 10, w, 12, Tizador.Mostaza);
                    for (var i = 0; i < Math.Min(5, lineas.Count); i++) {
                        p.Texto(x + 24, 66 + i * 50, lineas[i], 18, Tizador.Tiza);
                        if (i < lineas.Count - 1) p.Linea(x + 16, 80 + i * 50, x + w - 16, 80 + i * 50, Tizador.Gris, 1.5f, true);
                    }
                    break;
                default:   // nota
                    p.Caja(x + 20, 20, w - 40, 250, null, Tizador.Mostaza);
                    p.Punto(x + w / 2, 20, 9, Tizador.Roja);
                    for (var i = 0; i < Math.Min(4, lineas.Count); i++) p.Texto(x + 44, 80 + i * 48, lineas[i], 17, Tizador.Tiza);
                    break;
            }
        }
    }
}
