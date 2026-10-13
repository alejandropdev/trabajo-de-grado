using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>El curso de Cascada (ca-…): una obra que baja escalon a escalon, del plano al edificio entregado.</summary>
    public static partial class DibujosDeEscena {
        /// <summary>Una escalera que baja de izquierda a derecha: n escalones desde (x, y), cada uno de w×h.</summary>
        private static void Escalones(Tizador p, float x, float y, int n, float w, float h, Color? c = null) {
            var xy = new float[n * 4 + 2];
            xy[0] = x; xy[1] = y;
            for (var i = 0; i < n; i++) { xy[i * 4 + 2] = x + w * (i + 1); xy[i * 4 + 3] = y + h * i; xy[i * 4 + 4] = x + w * (i + 1); xy[i * 4 + 5] = y + h * (i + 1); }
            p.Trazo(c, xy);
        }

        private static void Balde(Tizador p, float x, float y, Color? c = null) {
            p.Trazo(c, x - 14, y - 22, x - 10, y, x + 10, y, x + 14, y - 22, x - 14, y - 22); p.Curva(x - 14, y - 22, x, y - 44, x + 14, y - 22, c, false);
        }

        private static void Carpeta(Tizador p, float x, float y, Color? c = null) {
            p.Trazo(c, x, y + 8, x, y + 40, x + 54, y + 40, x + 54, y + 8, x + 26, y + 8, x + 20, y, x, y, x, y + 8);
        }

        private static void Cascada(Dictionary<string, Action<Tizador>> d) {
            d["ca-que"] = p => {
                Escalones(p, 30, 50, 6, 86, 38, Tizador.Tiza);
                for (var i = 0; i < 6; i++) p.Curva(30 + 86 * (i + 1) - 8, 50 + 38 * i, 30 + 86 * (i + 1) + 6, 62 + 38 * i, 30 + 86 * (i + 1) + 4, 86 + 38 * i, Tizador.Cian, false);
                Balde(p, 76, 48, Tizador.Mostaza); Balde(p, 334, 162, Tizador.Mostaza); Balde(p, 506, 238, Tizador.Mostaza);
                p.Flecha(150, 40, 250, 86, Tizador.Mostaza); p.Texto(380, 60, "siempre hacia abajo,", 17, Tizador.Mostaza, true); p.Texto(380, 84, "nunca de vuelta", 17, Tizador.Mostaza, true);
            };
            d["ca-origen"] = p => {
                p.Figura(100, 110, Tizador.Tiza, "Winston Royce"); p.Texto(100, 196, "1970", 22, Tizador.Mostaza, true);
                p.Caja(230, 40, 220, 200, null, Tizador.Tiza); Escalones(p, 250, 70, 4, 44, 36, Tizador.Cian);
                p.Trazo(Tizador.Roja, 520, 60, 570, 140, 470, 140, 520, 60); p.Texto(520, 130, "!", 40, Tizador.Roja, true);
                p.Bocadillo(440, 170, 150, 60, "«así es arriesgado»", Tizador.Roja, 14); p.Texto(340, 270, "él mismo recomendaba volver atrás e iterar", 15, Tizador.Tiza, true);
            };
            d["ca-idea"] = p => {
                p.Caja(20, 50, 170, 190, null, Tizador.Cian); p.Caja(50, 80, 110, 130, null, Tizador.Cian, true); p.Linea(50, 130, 160, 130, Tizador.Cian, 2, true); p.Linea(105, 80, 105, 210, Tizador.Cian, 2, true);
                p.Texto(105, 266, "todo el plano, al inicio", 15, Tizador.Cian, true); p.Flecha(205, 145, 350, 145, Tizador.Tiza); p.Texto(278, 130, "meses", 15, Tizador.Gris, true);
                p.Edificio(370, 50, 110, 190, Tizador.Mostaza); p.Flecha(560, 220, 492, 220, Tizador.Tiza); p.Figura(540, 170, Tizador.Tiza); p.Texto(470, 266, "y solo entonces entra el cliente", 15, Tizador.Mostaza, true);
            };
            d["ca-fases"] = p => {
                Escalones(p, 10, 44, 7, 82, 32, Tizador.Tiza);
                var n = new[] { "requisitos", "análisis", "diseño", "construcción", "pruebas", "despliegue", "mantenimiento" };
                for (var i = 0; i < 7; i++) p.Texto(51 + i * 82, 66 + i * 32 + 32, n[i], 12, Tizador.Mostaza, true);
                p.Hoja(34, 2, 30, 36, Tizador.Cian); p.Lupa(128, 50, 11, Tizador.Cian); p.Caja(196, 62, 34, 40, null, Tizador.Cian, true);
                p.En(266, 100, 0.55f).Ladrillos(0, 0, Tizador.Cian); p.Termometro(378, 122, 46, Tizador.Cian); p.En(432, 170, 0.5f).Camion(0, 0, Tizador.Cian); p.En(528, 196, 0.8f).LlaveInglesa(0, 0, Tizador.Cian);
            };
            d["ca-requisitos"] = p => {
                p.Carita(70, 90, 26, 0, Tizador.Tiza); p.Bocadillo(104, 30, 150, 44, "«y además…»", Tizador.Tiza, 15); p.Texto(110, 150, "el cliente lo dicta todo", 14, Tizador.Tiza, true);
                p.Hoja(280, 20, 130, 250, Tizador.Mostaza, "SRS"); p.Curva(280, 270, 345, 296, 410, 270, Tizador.Mostaza, false);
                p.Figura(510, 90, Tizador.Cian, "Jefe de obra"); p.Curva(470, 110, 440, 130, 414, 120, Tizador.Cian);
                p.Curva(300, 250, 320, 236, 340, 250, Tizador.Tiza, false); p.Curva(360, 250, 376, 236, 392, 250, Tizador.Cian, false); p.Texto(510, 240, "firman los dos", 15, Tizador.Tiza, true);
            };
            d["ca-analisis"] = p => {
                p.Hoja(200, 20, 150, 240, Tizador.Tiza); p.Figura(90, 120, Tizador.Cian, "analista"); p.Lupa(210, 110, 30, Tizador.Mostaza);
                p.Texto(410, 70, "?", 34, Tizador.Mostaza, true); p.Texto(490, 76, "¿se puede hacer?", 15, Tizador.Mostaza, true);
                p.Trazo(Tizador.Roja, 410, 120, 430, 156, 390, 156, 410, 120); p.Texto(490, 150, "¿qué puede fallar?", 15, Tizador.Roja, true);
                p.Hoja(394, 190, 32, 40, Tizador.Cian); p.Texto(490, 220, "¿qué reglas hay?", 15, Tizador.Cian, true);
            };
            d["ca-diseno"] = p => {
                p.Caja(20, 50, 200, 180, null, Tizador.Cian); p.Caja(36, 70, 70, 60, null, Tizador.Cian, true); p.Caja(124, 70, 80, 60, null, Tizador.Cian, true); p.Caja(36, 150, 168, 60, null, Tizador.Cian, true);
                p.Texto(120, 260, "arquitectura: el plano general", 14, Tizador.Cian, true); p.Flecha(232, 140, 292, 140, Tizador.Tiza);
                for (var i = 0; i < 3; i++) {
                    p.Caja(310 + i * 94, 60 + i * 16, 86, 120, null, Tizador.Mostaza);
                    for (var k = 0; k < 4; k++) p.Linea(320 + i * 94, 84 + i * 16 + k * 22, 386 + i * 94, 84 + i * 16 + k * 22, Tizador.Mostaza, 2);
                }
                p.Texto(450, 260, "y el detalle de cada parte", 14, Tizador.Mostaza, true);
            };
            d["ca-implementacion"] = p => {
                p.Hoja(240, 10, 120, 100, Tizador.Mostaza, "receta"); p.Texto(300, 130, "se sigue al pie de la letra", 14, Tizador.Mostaza, true);
                for (var i = 0; i < 3; i++) {
                    p.Caja(30 + i * 190, 150, 160, 130, null, Tizador.Gris, true); p.Cocinero(80 + i * 190, 190, Tizador.Tiza); p.Olla(120 + i * 190, 210, 46, 38, Tizador.Cian);
                    p.Curva(80 + i * 190, 160, 180 + i * 100, 100, 250 + i * 40, 90, Tizador.Gris);
                }
            };
            d["ca-pruebas"] = p => {
                var n = new[] { "el cliente aprueba", "el plato completo", "dos ingredientes juntos", "un ingrediente" };
                var real = new[] { "aceptación", "sistema", "integración", "unitarias" };
                for (var i = 0; i < 4; i++) {
                    var w = 170 + i * 70; p.Caja(250 - w / 2, 20 + i * 62, w, 54, n[i], i == 0 ? Tizador.Mostaza : Tizador.Cian, false, 15);
                    p.Texto(250 + w / 2 + 20, 54 + i * 62, real[i], 15, Tizador.Gris);
                }
                p.Texto(300, 290, "todo junto y al final, de abajo hacia arriba", 14, Tizador.Tiza, true);
            };
            d["ca-despliegue"] = p => {
                p.Camion(40, 120, Tizador.Tiza); for (var i = 0; i < 3; i++) p.PlatoServido(60 + i * 26, 110 - i * 10, 14, Tizador.Cian);
                p.Flecha(180, 160, 330, 160, Tizador.Cian); p.Elipse(450, 170, 110, 30, Tizador.Tiza); p.Linea(450, 200, 450, 250, Tizador.Tiza, 4);
                for (var i = 0; i < 4; i++) p.PlatoServido(390 + i * 40, 160, 16, Tizador.Mostaza);
                p.Carita(450, 80, 28, 1, Tizador.Mostaza); p.Texto(300, 280, "el producto completo, de una sola vez", 16, Tizador.Tiza, true);
            };
            d["ca-mantenimiento"] = p => {
                p.Linea(300, 10, 300, 290, Tizador.Gris, 2, true); p.Linea(20, 150, 580, 150, Tizador.Gris, 2, true);
                p.Linea(60, 40, 240, 40, Tizador.Tiza, 4); p.Punto(150, 60, 4, Tizador.Cian); p.Punto(150, 78, 4, Tizador.Cian); p.Caja(130, 28, 40, 16, null, Tizador.Roja); p.Texto(150, 126, "correctivo: arreglar", 15, Tizador.Roja, true);
                p.Caja(420, 40, 50, 50, null, Tizador.Tiza); p.Punto(436, 65, 4); p.Punto(454, 65, 4); p.Curva(480, 60, 520, 40, 540, 70, Tizador.Mostaza); p.Texto(450, 126, "adaptativo: ajustarse", 15, Tizador.Mostaza, true);
                p.Caja(110, 180, 80, 60, null, Tizador.Tiza); p.Estrella(150, 210, 18, Tizador.Mostaza); p.Texto(150, 276, "perfectivo: mejorar", 15, Tizador.Cian, true);
                p.Caja(434, 190, 32, 56, null, Tizador.Roja); p.Linea(450, 190, 450, 176, Tizador.Roja, 5); p.Lupa(500, 200, 14, Tizador.Tiza); p.Texto(450, 276, "preventivo: evitar fallos", 15, Tizador.Tiza, true);
            };
            d["ca-documentacion"] = p => {
                Escalones(p, 40, 70, 4, 130, 50, Tizador.Tiza);
                for (var i = 0; i < 4; i++) {
                    p.Figura(100 + i * 130, 20 + i * 50, Tizador.Tiza);
                    if (i < 3) { Carpeta(p, 140 + i * 130, 30 + i * 50, Tizador.Mostaza); p.Flecha(196 + i * 130, 60 + i * 50, 214 + i * 130, 78 + i * 50, Tizador.Mostaza); }
                }
                p.Texto(170, 270, "cada fase entrega su documento a la siguiente", 15, Tizador.Mostaza, true);
            };
            d["ca-puertas"] = p => {
                p.Trazo(Tizador.Tiza, 30, 120, 240, 120, 240, 220, 570, 220);
                p.Caja(228, 20, 24, 200, null, Tizador.Mostaza); p.Linea(240, 20, 420, 20, Tizador.Mostaza, 4); p.Linea(420, 20, 420, 220, Tizador.Mostaza, 4);
                for (var x = 270f; x < 420; x += 30) p.Linea(x, 20, x, 220, Tizador.Mostaza, 2);
                p.Candado(330, 110, 18, Tizador.Roja); p.Figura(110, 40, Tizador.Tiza, "el cliente"); p.Hoja(160, 30, 44, 56, Tizador.Cian);
                p.Curva(166, 76, 180, 64, 198, 76, Tizador.Cian, false); p.Texto(500, 120, "firma, y se abre", 15, Tizador.Cian, true); p.Texto(500, 144, "la fase siguiente", 15, Tizador.Cian, true);
            };
            d["ca-roles"] = p => {
                Escalones(p, 20, 100, 5, 100, 36, Tizador.Tiza);
                var rol = new[] { "analista", "arquitecto", "programadores", "testers", "cliente" };
                for (var i = 0; i < 5; i++) { p.Figura(70 + i * 100, 52 + i * 36, i == 4 ? Tizador.Mostaza : Tizador.Tiza); p.Texto(70 + i * 100, 122 + i * 36, rol[i], 13, Tizador.Tiza, true); }
                p.Figura(450, 30, Tizador.Cian); p.Hoja(480, 20, 30, 40, Tizador.Cian); p.Texto(520, 90, "jefe de proyecto", 15, Tizador.Cian, true); p.Curva(430, 50, 300, 30, 200, 70, Tizador.Cian);
            };
            d["ca-planificacion"] = p => {
                p.Caja(70, 14, 130, 40, "Restaurante", Tizador.Mostaza, false, 15); p.Linea(135, 54, 135, 76, Tizador.Mostaza); p.Linea(50, 76, 220, 76, Tizador.Mostaza);
                var rama = new[] { "cocina", "salón", "carta" };
                for (var i = 0; i < 3; i++) { p.Linea(50 + i * 85, 76, 50 + i * 85, 96, Tizador.Mostaza); p.Caja(14 + i * 85, 96, 72, 34, rama[i], Tizador.Tiza, false, 13); }
                p.Texto(135, 164, "EDT: partir el trabajo", 14, Tizador.Mostaza, true);
                p.Ejes(290, 20, 280, 200, Tizador.Gris);
                var x = new[] { 300f, 350, 420, 470 }; var w = new[] { 60f, 80, 60, 80 };
                for (var i = 0; i < 4; i++) p.Bloque(x[i], 36 + i * 44, w[i], 28, i == 2 ? Tizador.Gris : Tizador.Cian);
                p.Trazo(Tizador.Roja, 300, 70, 360, 70, 360, 114, 430, 114, 470, 114, 470, 200, 550, 200); p.Texto(430, 250, "Gantt, y en rojo la ruta crítica", 14, Tizador.Roja, true);
            };
            d["ca-cambios"] = p => {
                p.Carita(60, 110, 26, 0, Tizador.Tiza); p.Bocadillo(20, 20, 200, 44, "«¡quiero pasta, no arroz!»", Tizador.Tiza, 13); p.Flecha(110, 120, 170, 120, Tizador.Mostaza);
                for (var i = 0; i < 4; i++) p.Hoja(190 + i * 8, 80 - i * 10, 80, 100, Tizador.Mostaza); p.Texto(240, 216, "solicitud", 15, Tizador.Mostaza, true);
                p.Flecha(310, 120, 360, 120, Tizador.Mostaza); p.Sello(410, 120, "EVAL.", Tizador.Tiza); p.Flecha(460, 120, 490, 120, Tizador.Mostaza);
                for (var i = 0; i < 3; i++) p.Carita(510 + i * 34, 110 + (i % 2) * 30, 15, -1, Tizador.Roja);
                p.Texto(540, 216, "comité", 15, Tizador.Roja, true); p.Texto(300, 270, "cambiar algo es un trámite formal: pedir, evaluar, aprobar", 14, Tizador.Tiza, true);
            };
            d["ca-costo"] = p => {
                p.Ejes(60, 20, 480, 210, Tizador.Gris); p.Trazo(Tizador.Roja, 60, 220, 180, 212, 300, 190, 400, 150, 480, 90, 540, 26);
                p.Texto(170, 180, "borrar una línea del plano", 14, Tizador.Cian, true); p.Moneda(170, 144, 12, Tizador.Cian);
                p.Texto(410, 60, "derribar una pared", 14, Tizador.Roja, true); for (var i = 0; i < 4; i++) p.Moneda(360 + i * 30, 26, 12, Tizador.Roja);
                p.Texto(300, 258, "cuanto más tarde se descubre un error, más cuesta", 15, Tizador.Tiza, true); p.Texto(300, 284, "inicio                                               final", 13, Tizador.Gris, true);
            };
            d["ca-v"] = p => {
                p.Trazo(Tizador.Tiza, 60, 30, 300, 270, 540, 30);
                var baja = new[] { "requisitos", "diseño", "detalle" }; var sube = new[] { "aceptación", "sistema", "unitarias" };
                for (var i = 0; i < 3; i++) {
                    var xl = 100 + i * 70; var y = 60 + i * 70; var xr = 500 - i * 70;
                    p.Punto(xl - 10, y, 6, Tizador.Cian); p.Punto(xr + 10, y, 6, Tizador.Mostaza); p.Linea(xl + 4, y, xr - 4, y, Tizador.Gris, 2, true);
                    var d = i < 2 ? 60 : -56;
                    p.Texto(xl + d, y - 10, baja[i], 14, Tizador.Cian, true); p.Texto(xr - d, y - 10, sube[i], 14, Tizador.Mostaza, true);
                }
                p.Texto(300, 296, "construir", 14, Tizador.Tiza, true); p.Texto(80, 280, "se define", 14, Tizador.Cian, true); p.Texto(520, 280, "se prueba", 14, Tizador.Mostaza, true);
            };
            d["ca-variantes"] = p => {
                Escalones(p, 20, 60, 3, 50, 40, Tizador.Tiza);
                for (var i = 0; i < 2; i++) p.Curva(100 + i * 50, 110 + i * 40, 70 + i * 50, 100 + i * 40, 80 + i * 50, 76 + i * 40, Tizador.Cian);
                p.Texto(100, 230, "con vuelta atrás", 14, Tizador.Cian, true);
                for (var i = 0; i < 3; i++) p.Bloque(230 + i * 36, 60 + i * 40, 70, 30, i == 1 ? Tizador.Mostaza : Tizador.Tiza);
                p.Texto(300, 230, "Sashimi:", 14, Tizador.Mostaza, true); p.Texto(300, 250, "fases que se solapan", 14, Tizador.Mostaza, true);
                for (var k = 0; k < 3; k++) Escalones(p, 420 + k * 54, 60 + k * 10, 3, 16, 30, Tizador.Tiza);
                p.Texto(500, 230, "incremental:", 14, Tizador.Tiza, true); p.Texto(500, 250, "varias pequeñas", 14, Tizador.Tiza, true);
            };
            d["ca-ventajas"] = p => {
                p.Figura(90, 110, Tizador.Cian, "Jefe de obra"); p.Carita(90, 50, 18, 1, Tizador.Cian);
                p.Calendario(200, 60, 90, 90, Tizador.Mostaza); p.Si(245, 120, null, 12); p.Texto(245, 184, "fechas claras", 15, Tizador.Mostaza, true);
                p.Caja(330, 60, 100, 90, null, Tizador.Tiza); p.Moneda(380, 100, 22, Tizador.Mostaza); p.Candado(414, 66, 10, Tizador.Tiza); p.Texto(380, 184, "presupuesto cerrado", 15, Tizador.Tiza, true);
                for (var i = 0; i < 3; i++) Carpeta(p, 470 + i * 14, 70 + i * 16, Tizador.Cian); p.Texto(520, 184, "todo documentado", 15, Tizador.Cian, true);
                p.Texto(300, 250, "fácil de entender y de controlar", 17, Tizador.Tiza, true);
            };
            d["ca-desventajas"] = p => {
                p.Carita(110, 120, 34, 0, Tizador.Tiza); p.Telarana(60, 40, 70, Tizador.Gris); p.Esfera(210, 70, 26, Tizador.Gris); p.Texto(130, 200, "meses esperando", 15, Tizador.Gris, true);
                p.Flecha(270, 130, 340, 130, Tizador.Tiza); p.PlatoServido(410, 150, 44, Tizador.Mostaza); p.Carita(520, 110, 30, -1, Tizador.Roja);
                p.Bocadillo(360, 20, 220, 44, "«esto no era lo que quería»", Tizador.Roja, 14); p.Texto(300, 260, "ve el producto muy tarde, y cambiarlo ya es difícil", 15, Tizador.Roja, true);
            };
            d["ca-cuando"] = p => {
                p.Balanza(300, 110, 320, -1, Tizador.Tiza);
                p.Curva(100, 100, 140, 66, 180, 100, Tizador.Cian, false); p.Linea(100, 100, 180, 100, Tizador.Cian);
                p.Texto(140, 44, "Cascada", 22, Tizador.Cian, true); p.Texto(140, 220, "un puente, un avión,", 15, Tizador.Cian, true); p.Texto(140, 242, "software médico", 15, Tizador.Cian, true);
                p.Texto(140, 274, "requisitos fijos y regulados", 14, Tizador.Tiza, true);
                p.Telefono(444, 22, 32, 50, Tizador.Mostaza); p.Texto(540, 50, "Ágil", 22, Tizador.Mostaza, true);
                p.Texto(460, 220, "una app de moda:", 15, Tizador.Mostaza, true); p.Texto(460, 242, "lo que se pide cambia", 15, Tizador.Mostaza, true);
            };
            d["ca-resumen"] = p => {
                var n = new[] { "Cascada", "Scrum", "Kanban" };
                for (var i = 0; i < 3; i++) { p.Texto(70, 60 + i * 90, n[i], 20, Tizador.Mostaza, true); p.Linea(140, 80 + i * 90, 570, 80 + i * 90, Tizador.Gris, 2); }
                p.PlatoServido(520, 56, 44, Tizador.Cian); p.Texto(330, 60, "una entrega grande, al final", 14, Tizador.Tiza, true);
                for (var i = 0; i < 4; i++) p.Bandeja(170 + i * 104, 164, 60, Tizador.Cian); p.Texto(355, 120, "entregas en ciclos", 14, Tizador.Tiza, true);
                for (var i = 0; i < 9; i++) p.Plato(170 + i * 46, 250, 16, Tizador.Cian); p.Texto(355, 214, "flujo continuo", 14, Tizador.Tiza, true);
            };
        }
    }
}
