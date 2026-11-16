using System;
using System.Collections.Generic;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using L = Nexus.Unity.Tema.LuzUI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// (2026-11-15, direccion de arte) Las recetas de los minijuegos en el lenguaje nuevo: los pasos de cada reto y las 14
    /// viñetas de «¿cuándo es cada cosa?» (330×190, con ejemplos de OTRA tienda). Se dibujan sobre el panel de chapa, sin
    /// vitrina: la receta pone las piezas donde las necesita y a la escala que pida (Tizador.En).
    /// </summary>
    public static partial class DibujosDeEscena {
        public static void RecetaPersona(Tizador t, float x, float y, Color c) { Credencial(t, x, y, c); }
        public static void RecetaBarra(Tizador t, float x, float y, float w, Color c) { t.Forma(L.Redondeado(x, y, w, 4, 2), c); }

        // ================================================================ piezas de la receta

        /// <summary>Flecha curva de neon: una curva cuadratica con su punta en 'b'.</summary>
        private static void FlechaCurva(Tizador v, Vector2 a, Vector2 c, Vector2 b, Color col) {
            var curva = Tizador.Bezier(a, c, b, 22);
            v.Neon(curva, 1.2f, col);
            var d = (b - c).normalized; var n = new Vector2(-d.y, d.x);
            v.Forma(new List<Vector2> { b + d * 5, b - d * 8 + n * 6, b - d * 8 - n * 6 }, Color.Lerp(col, Color.white, 0.4f));
        }

        /// <summary>Pastilla de la interfaz (etiqueta con borde); 'on' la rellena. Devuelve su ancho.</summary>
        private static float Pastilla(Tizador v, float x, float y, string texto, bool on, Color? c = null) {
            var col = c ?? Nucleo;
            var w = texto.Length * 10 + 26;
            v.Forma(L.Redondeado(x - 1.5f, y - 1.5f, w + 3, 35, 17.5f), A(col, 0.95f));
            v.Forma(L.Redondeado(x, y, w, 32, 16), on ? col : Color.Lerp(Superficie, col, 0.12f));
            v.Rotulo(x + w / 2, y + 16, texto, 16, on ? H(0x03141A) : col, 0, true);
            return w;
        }

        /// <summary>Un nodo del grafo de commits: aro con halo y su etiqueta a la derecha.</summary>
        private static void NodoDeRama(Tizador v, float x, float y, Color c, string texto = null) {
            v.Halo(x, y, 20, A(c, 0.4f));
            v.Forma(L.Elipse(x, y, 10, 10, 20), SuperficieAlta, Superficie);
            Anillo(v, x, y, 10, 2.6f, c);
            if (texto != null) v.Rotulo(x + 20, y, texto, 16, c, -1);
        }

        /// <summary>Puntero de raton: lo que se toca con la mano en esta receta.</summary>
        private static void Puntero(Tizador v, float x, float y, float s = 1) {
            var p = new List<Vector2> { new Vector2(0, 0), new Vector2(0, 32), new Vector2(8, 24), new Vector2(16, 40), new Vector2(22, 37), new Vector2(14, 22), new Vector2(26, 22) };
            for (var i = 0; i < p.Count; i++) p[i] = new Vector2(x + p[i].x * s, y + p[i].y * s);
            v.Forma(p, Tinta, TintaSuave);
        }

        // ================================================================ las 14 viñetas

        public static bool RecetaVineta(Tizador p, string id) {
            var v = p;
            switch (id) {
                case "requisito_ambiguo":
                    Pieza(v, 10, 70, 130, 64, "Pedidos", Nucleo, 17);
                    v.Neon(new RutaDeCurva(142, 94).C(190, 10, 310, 10, 330, 80).C(340, 130, 230, 140, 172, 120).Puntos(), 1.2f, Aviso);
                    v.Rotulo(240, 152, "¿el más urgente?", 16, Aviso); v.Rotulo(240, 174, "¿urgente según quién?", 14, Aviso);
                    return true;
                case "requisito_incompleto":
                    Pieza(v, 20, 60, 150, 70, "Registrar", Nucleo, 17);
                    FlechaNeon(v, 176, 95, 214, 95, Nucleo);
                    Credencial(v, 240, 40, Nucleo, "con cita"); NeonCheck(v, 270, 34, 9);
                    Credencial(v, 300, 70, Tinta, "sin cita"); NeonX(v, 300, 60, 10);
                    return true;
                case "acoplamiento_indebido":
                    Pieza(v, 10, 20, 110, 56, "A", Nucleo, 18); Pieza(v, 200, 20, 120, 56, "B", Nucleo, 18);
                    Icono(v.En(226, 106, 0.62f), "base-de-datos", Tizador.Tiza);
                    FlechaNeon(v, 124, 48, 196, 48, Tinta);
                    FlechaCurva(v, new Vector2(65, 80), new Vector2(90, 168), new Vector2(214, 150), Coral);
                    v.Rotulo(70, 170, "se salta a B", 16, Coral, -1);
                    return true;
                case "falla_de_seguridad":
                    Credencial(v, 40, 40, Coral, "cualquiera");
                    FlechaNeon(v, 76, 80, 190, 80, Coral);
                    Pieza(v, 195, 45, 120, 70, "Puerta", Nucleo, 17);
                    v.Rotulo(255, 140, "abre sin preguntar", 17, Coral);
                    v.Neon(Tizador.Bezier(new Vector2(240, 34), new Vector2(255, 6), new Vector2(270, 34), 14), 1.2f, Coral);
                    return true;
                case "dependencia_circular":
                    Pieza(v, 30, 60, 100, 60, "A", Aviso, 20); Pieza(v, 210, 60, 100, 60, "B", Aviso, 20);
                    FlechaCurva(v, new Vector2(132, 70), new Vector2(170, 20), new Vector2(208, 70), Aviso);
                    FlechaCurva(v, new Vector2(208, 112), new Vector2(170, 162), new Vector2(132, 112), Aviso);
                    return true;
                case "elemento_sin_especificar":
                    Pieza(v, 90, 40, 160, 90, null, TintaSuave);
                    v.Rotulo(170, 85, "¿¿¿ ???", 30, Aviso, 0, false, true);
                    return true;
                case "orden_incorrecto":
                    v.Rotulo(10, 22, "Diseño", 17, Nucleo, -1, true); v.Rotulo(10, 52, "1  revisar", 19, Tinta, -1); v.Rotulo(10, 80, "2  entregar", 19, Tinta, -1);
                    v.Rotulo(190, 22, "Pasó", 17, Aviso, -1, true); v.Rotulo(190, 52, "1  entregar", 19, Coral, -1); v.Rotulo(190, 80, "2  revisar", 19, Tinta, -1);
                    FlechaCurva(v, new Vector2(296, 52), new Vector2(336, 66), new Vector2(298, 82), Coral);
                    return true;
                case "mensaje_duplicado":
                    v.Rotulo(20, 34, "10:01  guardar pedido", 20, Tinta, -1); v.Rotulo(20, 70, "10:01  guardar pedido", 20, Coral, -1);
                    Contorno(v, Caja(10, 50, 262, 32), 1.6f, Coral);
                    v.Rotulo(300, 66, "×2", 26, Coral);
                    return true;
                case "mensaje_no_documentado":
                    v.Rotulo(10, 22, "Diseño", 17, Nucleo, -1, true); v.Rotulo(10, 54, "1  pedir", 19, Tinta, -1); v.Rotulo(10, 86, "—", 22, TintaSuave, -1);
                    v.Rotulo(190, 22, "Pasó", 17, Aviso, -1, true); v.Rotulo(190, 54, "1  pedir", 19, Tinta, -1); v.Rotulo(190, 86, "2  reintentar", 19, Aviso, -1);
                    return true;
                case "participante_equivocado": {
                    var xs = new[] { 55f, 175f, 285f }; var nombres = new[] { "Tienda", "Pagos", "Bodega" };
                    for (var i = 0; i < 3; i++) {
                        Pieza(v, xs[i] - 45, 30, 90, 48, nombres[i], Nucleo, 15);
                        for (var y = 84f; y < 170; y += 12) v.Forma(Caja(xs[i] - 1, y, 2, 6), A(TintaSuave, 0.7f));
                    }
                    FlechaNeon(v, 55, 110, 280, 110, Coral); v.Rotulo(170, 98, "cobrar()", 17, Coral);
                    return true;
                }
                case "historia_reescrita":
                    v.Neon(Pts(40, 20, 40, 170), 1.2f, Nucleo); NodoDeRama(v, 40, 50, Nucleo, "a"); NodoDeRama(v, 40, 100, Nucleo, "b"); NodoDeRama(v, 40, 150, Nucleo, "c");
                    v.Neon(Pts(200, 50, 200, 170), 1.2f, Aviso); NodoDeRama(v, 200, 50, Aviso, "a'"); NodoDeRama(v, 200, 100, Aviso, "b'"); NodoDeRama(v, 200, 150, Aviso, "c'");
                    v.Rotulo(120, 60, "=", 30, Aviso, 0, false, true);
                    return true;
                case "autoria_perdida":
                    NodoDeRama(v, 40, 60, Nucleo); Credencial(v, 110, 28, Nucleo, "Ana");
                    NodoDeRama(v, 40, 140, Aviso); Credencial(v, 110, 108, Coral, "Beto");
                    FlechaNeon(v, 40, 76, 40, 124, Tinta);
                    v.Rotulo(170, 78, "mismo código", 17, Tinta, -1); v.Rotulo(170, 158, "firmado por otro", 18, Coral, -1);
                    return true;
                case "rama_huerfana":
                    v.Neon(Pts(40, 20, 40, 170), 1.2f, Nucleo); NodoDeRama(v, 40, 40, Nucleo); NodoDeRama(v, 40, 90, Nucleo); NodoDeRama(v, 40, 140, Nucleo);
                    NodoDeRama(v, 190, 90, Aviso); Anillo(v, 190, 90, 28, 2, A(Aviso, 0.7f));
                    return true;
                case "fusion_sin_revisar":
                    v.Neon(Pts(40, 20, 40, 170), 1.2f, Nucleo);
                    v.Neon(Pts(140, 40, 140, 100), 1.2f, Aviso); v.Neon(new RutaDeCurva(140, 100).Q(140, 132, 52, 140).Puntos(), 1.2f, Aviso);
                    NodoDeRama(v, 40, 140, Aviso, "merge");
                    Credencial(v, 250, 40, Tinta, "revisor"); NeonX(v, 250, 28, 12);
                    return true;
                default: return false;
            }
        }

        // ================================================================ los pasos de cada reto

        private static void PasoTexto(Tizador v, float x, float y, string t, float tam = 22, Color? c = null) { v.Rotulo(x, y, t, tam, c ?? Tinta); }

        /// <summary>Los pasos de la receta: "detectar", "corregir", "ordenar", "puente" o "repartir". 'chips' = las etiquetas de la paleta.</summary>
        public static void RecetaPasos(Tizador p, string cual, List<string> chips) {
            switch (cual) {
                case "corregir": PasosCorregirLuz(p); break;
                case "ordenar": PasosOrdenarLuz(p); break;
                case "puente": PasosPuenteLuz(p); break;
                case "repartir": PasosRepartirLuz(p); break;
                default: PasosDetectarLuz(p, chips ?? new List<string>()); break;
            }
        }

        private static void PasosDetectarLuz(Tizador p, List<string> chips) {
            var a = p.En(0, 0); Pieza(a, 0, 30, 150, 70, "pieza", Nucleo, 17); Puntero(a, 100, 60); PasoTexto(a, 75, 150, "1 · Pincha lo raro");
            FlechaCurva(p, new Vector2(180, 60), new Vector2(220, 20), new Vector2(260, 60), Tinta);
            var b = p.En(290, 0);
            for (var i = 0; i < Math.Min(3, chips.Count); i++) Pastilla(b, -20, 14 + i * 42, chips[i], i == 1);
            PasoTexto(b, 70, 170, "2 · Dile qué le pasa");
            FlechaCurva(p, new Vector2(520, 60), new Vector2(550, 20), new Vector2(580, 60), Tinta);
            var c = p.En(600, 0); Reloj(c, 60, 60, 28, 11, 55, Aviso); BotonUI(c, 110, 40, 110, 42, "Entregar", Nucleo, true); PasoTexto(c, 110, 150, "3 · Entrega a tiempo");
            var d = p.En(920, 0); Pieza(d, 0, 30, 130, 70, "bien", Nucleo, 17); NeonCheck(d, 108, 64, 14);
            d.Rotulo(160, 72, "marcar", 20, Tinta, -1); d.Rotulo(160, 100, "= resta", 22, Coral, -1);
            PasoTexto(d, 110, 150, "¡Ojo! Lo que está bien no se marca", 19, Aviso);
        }

        private static void PasosCorregirLuz(Tizador p) {
            var a = p.En(0, 0); Pieza(a, 0, 30, 150, 70, "pieza", Nucleo, 17);
            a.Halo(150, 30, 22, A(Aviso, 0.5f)); Anillo(a, 150, 30, 18, 2.4f, Aviso); a.Rotulo(150, 30, "#1", 16, Aviso, 0, false, true);
            Puntero(a, 100, 60); PasoTexto(a, 75, 150, "1 · Elige un punto");
            FlechaCurva(p, new Vector2(200, 60), new Vector2(240, 20), new Vector2(280, 60), Tinta);
            var b = p.En(300, 0);
            for (var i = 0; i < 3; i++) Pieza(b, i * 62, 10 + Math.Abs(i - 1) * 10, 56, 84, null, i == 1 ? Nucleo : TintaSuave);
            FlechaNeon(b, 90, 4, 90, -12, Nucleo);
            PasoTexto(b, 90, 150, "2 · Juégale la carta que lo arregla");
            FlechaCurva(p, new Vector2(560, 60), new Vector2(590, 20), new Vector2(620, 60), Tinta);
            var c = p.En(640, 0); Reloj(c, 60, 60, 28, 11, 55, Aviso); BotonUI(c, 110, 40, 120, 42, "Verificar", Nucleo, true); PasoTexto(c, 120, 150, "3 · Verifica a tiempo");
            var d = p.En(960, 0);
            d.Halo(40, 60, 30, A(Menta, 0.4f)); Anillo(d, 40, 60, 26, 2.6f, Menta); NeonCheck(d, 40, 62, 12);
            d.Halo(130, 60, 30, A(Coral, 0.4f)); Anillo(d, 130, 60, 26, 2.6f, Coral); NeonX(d, 130, 60, 12);
            PasoTexto(d, 90, 150, "Se revela punto por punto", 19, Aviso);
        }

        private static void PasosOrdenarLuz(Tizador p) {
            var a = p.En(0, 0);
            Pieza(a, 0, 10, 170, 36, "A", Tinta, 16); Pieza(a, 0, 54, 170, 36, "B", Nucleo, 16); Pieza(a, 0, 98, 170, 36, "C", Tinta, 16); Puntero(a, 140, 56);
            PasoTexto(a, 85, 170, "1 · Arrastra: arriba lo primero");
            var b = p.En(300, 0);
            Pieza(b, 0, 10, 170, 36, "A", Tinta, 16); Pieza(b, 0, 54, 170, 36, "B", Tinta, 16);
            for (var x = -10f; x < 180; x += 14) b.Forma(Caja(x, 99, 7, 3), Aviso);
            Pieza(b, 0, 112, 170, 36, "C", TintaSuave, 16);
            PasoTexto(b, 85, 170, "2 · Lo de abajo no entra");
            var c = p.En(600, 0);
            Pieza(c, 0, 10, 110, 40, "Base", Tinta, 16); Pieza(c, 0, 100, 110, 40, "Techo", Tinta, 16); FlechaNeon(c, 55, 52, 55, 96, Aviso);
            c.Rotulo(124, 76, "necesita", 18, Aviso, -1); PasoTexto(c, 90, 170, "3 · Hay cosas que van antes");
            var d = p.En(900, 0);
            Credencial(d, 40, 0, Aviso, "cliente"); d.Rotulo(100, 30, "«¡todo!»", 22, Aviso, -1);
            Pastilla(d, 90, 60, "Sí a todo", false); Pastilla(d, 90, 100, "No", false); Pastilla(d, 200, 100, "Negociar", true);
            PasoTexto(d, 150, 170, "4 · Contesta al cliente");
        }

        private static void PasosPuenteLuz(Tizador p) {
            var a = p.En(0, 0);
            Pieza(a, 0, 10, 170, 36, "Techo", Tinta, 16); Pieza(a, 0, 54, 170, 36, "Pared", Nucleo, 16); Pieza(a, 0, 98, 170, 36, "Base", Tinta, 16); FlechaNeon(a, 200, 130, 200, 20, Aviso);
            PasoTexto(a, 85, 170, "1 · Se construye de abajo arriba");
            var b = p.En(360, 0);
            Pieza(b, 0, 10, 110, 40, "Techo", Tinta, 16); Pieza(b, 0, 100, 110, 40, "Base", Tinta, 16); FlechaNeon(b, 55, 98, 55, 54, Aviso);
            b.Rotulo(125, 76, "necesita", 18, Aviso, -1); PasoTexto(b, 90, 170, "2 · Lo que necesita, más abajo");
            var c = p.En(720, 0);
            Pieza(c, 0, 10, 110, 40, "Base", TintaSuave, 16); Pieza(c, 0, 100, 110, 40, "Techo", Coral, 16); NeonX(c, 140, 120, 14);
            Pastilla(c, 120, 52, "1 vida menos", false, Coral);
            PasoTexto(c, 90, 170, "3 · Mal puesto: roja y pierdes una");
            var d = p.En(1080, 0);
            Pieza(d, 0, 10, 110, 40, "Techo", Menta, 16); Pieza(d, 0, 100, 110, 40, "Base", Menta, 16); NeonCheck(d, 156, 100, 14);
            PasoTexto(d, 90, 170, "4 · Todo verde: cruza el camión");
        }

        private static void PasosRepartirLuz(Tizador p) {
            var a = p.En(0, 0); Icono(a.En(-14, -4, 0.9f), "pila", Tizador.Tiza); a.Rotulo(124, 60, "+", 30, Nucleo, 0, false, true); a.Rotulo(124, 100, "−", 30, Tinta);
            PasoTexto(a, 70, 170, "1 · Dale horas a cada pila");
            var b = p.En(360, 0); Icono(b.En(-14, -4, 0.9f), "pila", Tizador.Tiza); Chispa(b, 90, 100, 8, 0.1f); NeonCheck(b, 130, 100, 12); Chispa(b, 170, 100, 8, 0.5f); NeonX(b, 170, 56, 10);
            PasoTexto(b, 100, 170, "2 · Cada pila atrapa SOLO lo suyo");
            var c = p.En(740, 0); Chispa(c, 22, 70, 9, 0.2f); FlechaNeon(c, 44, 70, 140, 70, Coral); Credencial(c, 175, 20, Coral, "cliente");
            PasoTexto(c, 100, 170, "3 · Lo que se escapa, llega al cliente");
            var d = p.En(1100, 0); Icono(d.En(14, -4, 0.9f), "pila", Tizador.Tiza); PasoTexto(d, 70, 170, "Pila llena = TODAS las horas ahí", 19, Aviso);
        }

        // ================================================================ los trucos del pie

        /// <summary>Cuatro pilas con sus niveles; 'bien' las pone en cian con su marca, 'mal' una llena y las otras vacias con chispas.</summary>
        public static void RecetaPilas(Tizador t, float[] niveles, bool bien) {
            for (var i = 0; i < niveles.Length; i++) {
                var ti = t.En(i * 110 - 20, -4, 1f);
                Icono(ti, "pila", bien ? Tizador.Cian : Tizador.Gris);
                if (bien) NeonCheck(t, i * 110 + 35, -18, 12);
                else if (i > 0) { Chispa(t, i * 110 + 35, 120, 8, i * 0.2f); FlechaNeon(t, i * 110 + 35, 100, i * 110 + 35, -10, Coral); }
            }
        }

        public static void RecetaMano(Tizador m) {
            for (var i = 0; i < 4; i++) {
                if (i == 1) { Pieza(m, i * 120, 24, 100, 140, "EN #1", TintaSuave, 16); } else Pieza(m, i * 120, 0, 100, 140, null, Tinta);
            }
        }

        public static void RecetaBien(Tizador b) { Pieza(b, 0, 0, 120, 140, null, Nucleo); NeonCheck(b, 60, 70, 26); }
    }
}
