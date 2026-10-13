using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los sitios del propio juego (j-…): las barras del dia, el tablero, un aviso, el lanzamiento… Son los dibujos
    /// de las columnas de «Cómo lo verás en el juego», para que el jugador reconozca la pantalla de la que se habla.
    /// </summary>
    public static partial class DibujosDeEscena {
        private static void Juego(Dictionary<string, Action<Tizador>> d) {
            d["j-barras"] = p => {
                p.Caja(110, 30, 380, 240, null, Tizador.Gris);
                var nombres = new[] { "avance", "deuda", "cobertura", "cansancio", "moral" };
                var valores = new[] { 0.6f, 0.3f, 0.5f, 0.4f, 0.7f };
                for (var i = 0; i < 5; i++) { p.Texto(130, 68 + i * 44, nombres[i], 17, Tizador.Tiza); p.Medidor(250, 50 + i * 44, 220, valores[i], i == 0 ? Tizador.Cian : Tizador.Mostaza); }
            };
            d["j-horas-extra"] = p => {
                p.Esfera(170, 130, 70, Tizador.Tiza); p.Curva(330, 50, 290, 90, 330, 130, Tizador.Mostaza, false); p.Curva(330, 50, 352, 90, 330, 130, Tizador.Mostaza, false);
                p.Caja(390, 90, 170, 60, "Quedarte", Tizador.Mostaza, false, 24); p.Texto(475, 190, "una hora más", 20, Tizador.Mostaza, true);
                p.Texto(170, 250, "ya es de noche", 20, Tizador.Gris, true);
            };
            d["j-lanzamiento"] = p => {
                p.Bandera(120, 250, 190, Tizador.Mostaza); p.Caja(260, 50, 280, 190, null, Tizador.Tiza); p.Texto(400, 86, "Lanzamiento", 24, Tizador.Mostaza, true);
                p.Linea(280, 100, 520, 100, Tizador.Gris, 2); p.Texto(400, 150, "BIEN", 44, Tizador.Cian, true); p.Medidor(290, 190, 220, 0.8f, Tizador.Cian);
            };
            d["j-cliente-pide"] = p => {
                p.Figura(100, 110, Tizador.Mostaza, "cliente"); p.Bocadillo(140, 30, 250, 60, "«¿y si además…?»", Tizador.Mostaza, 22);
                p.Caja(150, 180, 110, 54, "Sí", Tizador.Cian, false, 22); p.Caja(280, 180, 110, 54, "No", Tizador.Roja, false, 22); p.Caja(410, 180, 150, 54, "Negociar", Tizador.Tiza, false, 22);
            };
            d["j-tablero"] = p => {
                p.Tablero(60, 30, 480, 240, Tizador.Tiza, "Por hacer", "En curso", "Hecho");
                p.Tarjeta(76, 70, 128, 44, Tizador.Cian); p.Tarjeta(76, 124, 128, 44, Tizador.Cian); p.Tarjeta(76, 178, 128, 44, Tizador.Cian);
                p.Tarjeta(236, 70, 128, 44, Tizador.Mostaza); p.Tarjeta(396, 70, 128, 44, Tizador.Tiza); p.Tarjeta(396, 124, 128, 44, Tizador.Tiza);
            };
            d["j-decision"] = p => {
                p.Caja(90, 30, 420, 240, null, Tizador.Tiza); p.Texto(300, 66, "Un aviso", 22, Tizador.Mostaza, true);
                for (var i = 0; i < 3; i++) p.Linea(120, 96 + i * 26, 480 - i * 60, 96 + i * 26, Tizador.Gris, 3);
                p.Caja(120, 190, 170, 54, "Rápido", Tizador.Roja, false, 20); p.Caja(310, 190, 170, 54, "Bien hecho", Tizador.Cian, false, 20);
            };
            d["j-incidencia"] = p => {
                p.Trazo(Tizador.Roja, 300, 30, 440, 250, 160, 250, 300, 30); p.Texto(300, 216, "!", 110, Tizador.Roja, true);
                p.Texto(300, 286, "algo se rompe", 20, Tizador.Roja, true);
            };
            d["j-oficina"] = p => {
                p.Linea(60, 220, 540, 220, Tizador.Tiza, 5); p.Navegador(110, 60, 200, 150, Tizador.Cian); p.Hoja(360, 90, 100, 124, Tizador.Tiza); p.Hoja(480, 120, 50, 94, Tizador.Gris);
                p.Texto(300, 268, "tu escritorio", 22, Tizador.Mostaza, true);
            };
            d["j-reto-pruebas"] = p => {
                p.Barras(80, 40, 300, 190, Tizador.Cian, 0.9f, 0.6f, 0.4f, 0.25f);
                p.Esfera(480, 110, 50, Tizador.Mostaza); p.Texto(480, 200, "horas a repartir", 20, Tizador.Mostaza, true); p.Texto(230, 268, "entre tipos de prueba", 20, Tizador.Tiza, true);
            };
            d["j-irse-a-casa"] = p => {
                p.Caja(330, 130, 170, 130, null, Tizador.Cian); p.Trazo(Tizador.Cian, 310, 130, 415, 50, 520, 130); p.Caja(395, 190, 44, 70, null, Tizador.Cian);
                p.Figura(160, 150, Tizador.Tiza); p.Flecha(200, 180, 300, 180, Tizador.Cian); p.Texto(160, 250, "a su hora", 22, Tizador.Tiza, true);
            };
            d["j-charla"] = p => {
                p.Figura(190, 130, Tizador.Cian, null); p.Figura(410, 130, Tizador.Mostaza, null);
                p.Bocadillo(210, 40, 80, 50, "···", Tizador.Cian, 28); p.Bocadillo(310, 40, 80, 50, "···", Tizador.Mostaza, 28);
                p.Linea(80, 200, 520, 200, Tizador.Gris, 3); p.Texto(300, 250, "en cada sala hay alguien", 22, Tizador.Tiza, true);
            };
            d["j-reunion"] = p => {
                p.Elipse(300, 170, 180, 50, Tizador.Tiza); p.Figura(170, 80, Tizador.Mostaza); p.Figura(300, 60, Tizador.Mostaza); p.Figura(430, 80, Tizador.Tiza);
                p.Esfera(520, 60, 30, Tizador.Roja); p.Texto(300, 268, "una reunión más", 22, Tizador.Roja, true);
            };
            d["j-pronostico"] = p => {
                p.Caja(100, 40, 400, 220, null, Tizador.Mostaza); p.Texto(300, 80, "Pronóstico del lanzamiento", 22, Tizador.Mostaza, true);
                p.Linea(120, 96, 480, 96, Tizador.Gris, 2); p.Texto(300, 160, "saldría REGULAR", 30, Tizador.Tiza, true); p.Medidor(150, 200, 300, 0.55f, Tizador.Mostaza);
            };
            d["j-apuntes"] = p => {
                p.Hoja(190, 30, 220, 240, Tizador.Tiza, "Apuntes"); p.Linea(190, 30, 190, 270, Tizador.Mostaza, 8);
                p.Si(450, 120, null, 16); p.Si(450, 180, null, 16);
            };
            d["j-fichas"] = p => {
                var nombres = new[] { "funcional", "usabilidad", "fiabilidad" }; var fichas = new[] { 2, 1, 1 };
                for (var i = 0; i < 3; i++) {
                    p.Texto(80, 86 + i * 80, nombres[i], 24, Tizador.Tiza);
                    for (var k = 0; k < 3; k++) { if (k < fichas[i]) p.Punto(330 + k * 70, 78 + i * 80, 24, Tizador.Mostaza); else p.Circulo(330 + k * 70, 78 + i * 80, 24, Tizador.Gris); }
                }
            };
            d["j-jurado"] = p => {
                p.Figura(170, 90, Tizador.Mostaza); p.Figura(300, 90, Tizador.Mostaza); p.Figura(430, 90, Tizador.Mostaza); p.Caja(90, 150, 420, 60, null, Tizador.Tiza);
                p.Lupa(290, 172, 14, Tizador.Mostaza); p.Texto(300, 258, "lo prueban ellos", 22, Tizador.Mostaza, true);
            };
            d["j-reto-detectar"] = p => {
                p.Caja(60, 60, 130, 60, "A", Tizador.Tiza, false, 22); p.Caja(260, 60, 130, 60, "B", Tizador.Tiza, false, 22); p.Caja(160, 190, 130, 60, "C", Tizador.Roja, false, 22);
                p.Flecha(195, 90, 255, 90); p.Flecha(310, 124, 250, 186, Tizador.Roja); p.Lupa(430, 170, 46, Tizador.Mostaza); p.Texto(470, 270, "¿dónde está el fallo?", 18, Tizador.Mostaza, true);
            };
            d["j-reto-ordenar"] = p => {
                for (var i = 0; i < 4; i++) { p.Tarjeta(150, 30 + i * 62, 240, 50, i == 0 ? Tizador.Cian : Tizador.Tiza); p.Texto(110, 64 + i * 62, (i + 1).ToString(), 26, Tizador.Mostaza, true); }
                p.Flecha(450, 220, 450, 60, Tizador.Mostaza); p.Texto(510, 150, "primero", 20, Tizador.Mostaza, true);
            };
            d["j-ticket"] = p => {
                p.Caja(150, 20, 300, 260, null, Tizador.Tiza); p.Caja(150, 20, 300, 44, "TICKET  #1042", Tizador.Mostaza, false, 20);
                var campos = new[] { "resumen", "descripción", "restricciones" };
                for (var i = 0; i < 3; i++) { p.Texto(170, 98 + i * 62, campos[i], 17, Tizador.Mostaza); p.Linea(170, 114 + i * 62, 430, 114 + i * 62, Tizador.Gris, 3); p.Linea(170, 130 + i * 62, 380, 130 + i * 62, Tizador.Gris, 3); }
            };
            d["j-eleccion"] = p => {
                var op = new[] { "A", "B", "C" };
                for (var i = 0; i < 3; i++) p.Caja(60 + i * 170, 50, 140, 120, op[i], i == 1 ? Tizador.Mostaza : Tizador.Gris, i != 1, 40);
                p.Mano(290, 150); p.Caja(150, 210, 300, 50, "porque…", Tizador.Cian, false, 22);
            };
            d["j-planificacion"] = p => {
                p.Tablero(40, 40, 200, 220, Tizador.Tiza, "backlog"); for (var i = 0; i < 4; i++) p.Tarjeta(56, 78 + i * 44, 168, 34, i < 2 ? Tizador.Cian : Tizador.Gris);
                p.Flecha(250, 110, 340, 110, Tizador.Cian); p.Caja(350, 60, 210, 130, null, Tizador.Cian); p.Texto(455, 100, "este sprint", 20, Tizador.Cian, true); p.Texto(455, 150, "¿cuánto cabe?", 22, Tizador.Mostaza, true);
            };
            d["j-retro"] = p => {
                p.Espejo(60, 40, 120, 170, Tizador.Tiza); p.Caja(240, 40, 310, 200, null, Tizador.Tiza); p.Linea(343, 40, 343, 240, Tizador.Gris, 2); p.Linea(446, 40, 446, 240, Tizador.Gris, 2);
                p.Carita(291, 80, 22, 1, Tizador.Cian); p.Carita(394, 80, 22, -1, Tizador.Roja); p.Estrella(497, 80, 22, Tizador.Mostaza);
                for (var k = 0; k < 3; k++) for (var i = 0; i < 2; i++) p.Linea(258 + k * 103, 140 + i * 40, 328 + k * 103, 140 + i * 40, Tizador.Gris, 3);
            };
            d["j-diagrama"] = p => {
                p.Figura(80, 110, Tizador.Mostaza); p.Flecha(120, 130, 190, 130); p.Caja(200, 100, 130, 60, null, Tizador.Cian); p.Flecha(335, 130, 400, 90); p.Flecha(335, 130, 400, 180);
                p.Caja(410, 60, 130, 56, null, Tizador.Cian); p.Cilindro(430, 160, 90, 70, Tizador.Tiza); p.Texto(300, 276, "el diagrama de tu propuesta", 20, Tizador.Tiza, true);
            };
            d["j-alerta"] = p => {
                p.Curva(230, 190, 230, 40, 300, 40, Tizador.Mostaza, false); p.Curva(300, 40, 370, 40, 370, 190, Tizador.Mostaza, false); p.Linea(200, 190, 400, 190, Tizador.Mostaza, 5);
                p.Circulo(300, 212, 14, Tizador.Mostaza); p.Curva(180, 80, 150, 120, 180, 160, Tizador.Roja, false); p.Curva(420, 80, 450, 120, 420, 160, Tizador.Roja, false);
                p.Texto(300, 270, "suena una alerta", 22, Tizador.Mostaza, true);
            };
            d["j-graficas"] = p => {
                p.Grafica(50, 40, 230, 180, Tizador.Cian, 1f, 0.8f, 0.7f, 0.45f, 0.3f, 0.1f); p.Barras(330, 40, 220, 180, Tizador.Mostaza, 0.5f, 0.7f, 0.6f);
                p.Texto(300, 268, "las gráficas del tablero", 22, Tizador.Tiza, true);
            };
            d["j-equipo"] = p => {
                var roles = new[] { "Interfaz", "Datos", "Pruebas" };
                for (var i = 0; i < 3; i++) { p.Caja(50 + i * 175, 50, 150, 200, null, Tizador.Tiza); p.Figura(125 + i * 175, 100, Tizador.Cian); p.Caja(70 + i * 175, 170, 110, 40, roles[i], Tizador.Mostaza, false, 18); }
            };
            d["j-lecciones"] = p => {
                p.Hoja(170, 20, 260, 260, Tizador.Tiza, "Lecciones"); p.Si(140, 100, null, 14); p.No(140, 160, null, 12); p.Si(140, 220, null, 14);
            };
        }
    }
}
