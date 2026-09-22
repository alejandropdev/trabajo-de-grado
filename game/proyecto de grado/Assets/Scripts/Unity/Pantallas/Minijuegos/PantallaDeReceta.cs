using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Dibuja en una Lamina con coordenadas «de prototipo» (las del mockup de pizarra) desplazadas y a escala.
    /// Asi cada viñeta se escribe en su propio cuadro de 330×190 y se coloca donde toque.
    /// </summary>
    public sealed class Tizador {
        public static readonly Color Tiza = new Color32(0xEE, 0xF3, 0xF1, 0xFF);
        public static readonly Color Cian = new Color32(0x8F, 0xEA, 0xFF, 0xFF);
        public static readonly Color Mostaza = new Color32(0xE3, 0xC2, 0x4F, 0xFF);
        public static readonly Color Roja = new Color32(0xE8, 0x82, 0x6F, 0xFF);
        public static readonly Color Gris = new Color32(0x6F, 0x8B, 0x92, 0xFF);
        public static readonly Color Pizarra = new Color32(0x17, 0x26, 0x2B, 0xFF);
        public static readonly Color Relleno = new Color32(0xBF, 0xEF, 0xFF, 0xFF);

        private readonly Lamina _l;
        private readonly TMP_FontAsset _fuente;
        private readonly float _ox, _oy, _k;

        public Tizador(Lamina lamina, TMP_FontAsset fuente, float ox = 0, float oy = 0, float k = 1) {
            _l = lamina; _fuente = fuente; _ox = ox; _oy = oy; _k = k;
        }

        public Tizador En(float x, float y, float k = 1) { return new Tizador(_l, _fuente, _ox + x * _k, _oy + y * _k, _k * k); }

        private float X(float x) { return _ox + x * _k; }
        private float Y(float y) { return _oy + y * _k; }
        private Vector2 P(float x, float y) { return new Vector2(X(x), Y(y)); }
        private DibujoUI D { get { return _l.Dibujo; } }

        public void Linea(float x1, float y1, float x2, float y2, Color? c = null, float g = 3, bool disc = false) {
            D.Linea(X(x1), Y(y1), X(x2), Y(y2), c ?? Tiza, g * _k, disc);
        }
        public void Flecha(float x1, float y1, float x2, float y2, Color? c = null, float g = 3) {
            Linea(x1, y1, x2, y2, c, g);
            D.Punta(P(x1, y1), P(x2, y2), c ?? Tiza, g * _k, 13 * _k);
        }
        public void Curva(float x1, float y1, float cx, float cy, float x2, float y2, Color? c = null, bool punta = true) {
            D.Curva(P(x1, y1), P(cx, cy), P(x2, y2), c ?? Tiza, 3 * _k, punta);
        }
        public void Caja(float x, float y, float w, float h, string t = null, Color? c = null, bool disc = false, float tam = 19) {
            D.Rect(X(x), Y(y), w * _k, h * _k, c ?? Tiza, 3 * _k, new Color(1, 1, 1, 0.04f), disc, 8 * _k);
            if (t != null) Texto(x + w / 2, y + h / 2 + 7, t, tam, c ?? Tiza, true);
        }
        public void Cilindro(float x, float y, float w, float h, Color? c = null) { D.Cilindro(X(x), Y(y), w * _k, h * _k, c ?? Tiza, 3 * _k); }
        public void Circulo(float x, float y, float r, Color? c = null, bool disc = false, float g = 3) { D.Circulo(X(x), Y(y), r * _k, c ?? Tiza, g * _k, null, disc); }
        public void Nodo(float x, float y, Color c, string t = null) {
            D.Circulo(X(x), Y(y), 12 * _k, c, 3.5f * _k, Pizarra);
            if (t != null) Texto(x + 20, y + 7, t, 16, c);
        }
        public void Persona(float x, float y, Color? c = null, string t = null) {
            D.Persona(X(x), Y(y), c ?? Tiza, 3 * _k);
            if (t != null) Texto(x, y + 56, t, 16, c ?? Tiza, true);
        }
        public void Cruz(float x, float y, Color? c = null, float r = 14) { D.Cruz(X(x), Y(y), c ?? Roja, r * _k); }
        public void Check(float x, float y, Color? c = null) { D.Check(X(x), Y(y), c ?? Cian); }
        public void Mano(float x, float y) { D.Mano(X(x), Y(y), Tiza); }
        public void Reloj(float x, float y) { D.Reloj(X(x), Y(y), Tiza); }
        public void Bicho(float x, float y, Color c) { D.Bicho(X(x), Y(y), c, 3 * _k); }
        public void Pila(float x, float y, float w, float h, float lleno) {
            D.Rect(X(x), Y(y), w * _k, h * _k, Tiza, 3 * _k, null, false, 8 * _k);
            if (lleno > 0) D.Rect(X(x + 5), Y(y + 5 + (h - 10) * (1 - lleno)), (w - 10) * _k, (h - 10) * lleno * _k, Relleno, 0, Relleno, false, 6 * _k);
        }

        /// <summary>Un chip (etiqueta con borde). Devuelve su ancho en coordenadas de prototipo.</summary>
        public float Chip(float x, float y, string t, bool on) {
            var w = t.Length * 10 + 26;
            D.Rect(X(x), Y(y), w * _k, 32 * _k, on ? Cian : Tiza, 2.5f * _k, on ? Cian : (Color?)null, false, 16 * _k);
            Texto(x + w / 2f, y + 23, t, 17, on ? Pizarra : Tiza, true);
            return w;
        }

        /// <summary>Texto con la letra de tiza. (x, y) es la linea base, como en SVG; 'centrado' = text-anchor middle.</summary>
        public TMP_Text Texto(float x, float y, string t, float tam, Color? c = null, bool centrado = false, float ancho = 0) {
            var w = (ancho > 0 ? ancho : Mathf.Max(60, t.Length * tam * 0.62f + 20)) * _k;
            var alto = tam * 1.5f * _k;
            var x0 = centrado ? X(x) - w / 2 : X(x);
            var texto = _l.Texto(x0, Y(y) - tam * 1.05f * _k, w, alto, t, tam * _k, c ?? Tiza,
                                 centrado ? TextAlignmentOptions.Top : TextAlignmentOptions.TopLeft, _fuente);
            texto.textWrappingMode = ancho > 0 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            return texto;
        }
    }

    /// <summary>
    /// Las viñetas de «¿cuándo es cada cosa?» y el texto corto de cada etiqueta. Los ejemplos son de OTRA tienda
    /// (Pedidos, Tienda, Pagos, cajas A y B): explican que significa cada etiqueta sin decir donde esta el error del
    /// reto de verdad. Cada viñeta se dibuja en un cuadro de 330×190.
    /// </summary>
    public static class Recetas {
        public static readonly Dictionary<string, string> QueEs = new Dictionary<string, string> {
            { "requisito_ambiguo", "Dice QUÉ hacer, pero no cómo decidir" }, { "requisito_incompleto", "Se olvidó de un caso" },
            { "acoplamiento_indebido", "Se mete en las tripas de otro" }, { "falla_de_seguridad", "Deja pasar a cualquiera" },
            { "dependencia_circular", "A necesita a B y B necesita a A" }, { "elemento_sin_especificar", "Nadie dice qué hace" },
            { "orden_incorrecto", "Pasó en otro orden" }, { "mensaje_duplicado", "Pasó dos veces" },
            { "mensaje_no_documentado", "Pasó y no estaba en el diseño" }, { "participante_equivocado", "Habló con quien no era" },
            { "historia_reescrita", "Commits copiados y cambiados de sitio" }, { "autoria_perdida", "El trabajo cambió de dueño" },
            { "rama_huerfana", "Un commit que no sale de nada" }, { "fusion_sin_revisar", "Se unió sin revisión" },
        };

        public static readonly Dictionary<string, Action<Tizador>> Dibujos = new Dictionary<string, Action<Tizador>> {
            { "requisito_ambiguo", p => {
                p.Caja(20, 70, 150, 64, "Pedidos");
                p.Curva(172, 70, 257, 0, 342, 70, Tizador.Mostaza, false); p.Curva(342, 70, 352, 115, 267, 115, Tizador.Mostaza, false);
                p.Linea(267, 115, 237, 135, Tizador.Mostaza); p.Linea(237, 135, 243, 115, Tizador.Mostaza); p.Curva(243, 115, 163, 115, 172, 70, Tizador.Mostaza, false);
                p.Texto(257, 62, "¿el más urgente?", 17, Tizador.Mostaza, true); p.Texto(257, 88, "¿urgente según quién?", 15, Tizador.Mostaza, true); } },
            { "requisito_incompleto", p => {
                p.Caja(20, 60, 150, 70, "Registrar"); p.Persona(230, 58, Tizador.Cian, "con cita"); p.Check(262, 62);
                p.Persona(300, 58, Tizador.Tiza, "sin cita"); p.Cruz(300, 40); p.Curva(170, 95, 200, 95, 215, 80, Tizador.Cian); } },
            { "acoplamiento_indebido", p => {
                p.Caja(10, 20, 110, 56, "A"); p.Caja(200, 20, 120, 56, "B"); p.Cilindro(215, 120, 90, 56);
                p.Flecha(120, 48, 198, 48); p.Curva(65, 78, 90, 160, 208, 150, Tizador.Roja); p.Texto(40, 150, "se salta a B", 16, Tizador.Roja); } },
            { "falla_de_seguridad", p => {
                p.Persona(40, 60, Tizador.Roja, "cualquiera"); p.Flecha(65, 80, 190, 80, Tizador.Roja); p.Caja(195, 45, 120, 70, "Puerta");
                p.Texto(255, 140, "abre sin preguntar", 17, Tizador.Roja, true); p.Curva(240, 32, 255, 5, 270, 32, Tizador.Roja, false); } },
            { "dependencia_circular", p => {
                p.Caja(30, 60, 100, 60, "A"); p.Caja(210, 60, 100, 60, "B");
                p.Curva(130, 70, 170, 30, 208, 70, Tizador.Mostaza); p.Curva(208, 112, 170, 152, 132, 112, Tizador.Mostaza); } },
            { "elemento_sin_especificar", p => { p.Caja(90, 40, 160, 90, null, Tizador.Tiza, true); p.Texto(170, 100, "¿¿¿ ???", 30, Tizador.Mostaza, true); } },
            { "orden_incorrecto", p => {
                p.Texto(10, 26, "Diseño", 17, Tizador.Cian); p.Texto(10, 56, "1  revisar", 19); p.Texto(10, 84, "2  entregar", 19);
                p.Texto(190, 26, "Pasó", 17, Tizador.Mostaza); p.Texto(190, 56, "1  entregar", 19, Tizador.Roja); p.Texto(190, 84, "2  revisar", 19);
                p.Curva(300, 60, 330, 72, 300, 86, Tizador.Roja); } },
            { "mensaje_duplicado", p => {
                p.Texto(20, 40, "10:01  guardar pedido", 20); p.Texto(20, 76, "10:01  guardar pedido", 20, Tizador.Roja);
                p.Caja(10, 54, 260, 32, null, Tizador.Roja); p.Texto(285, 76, "×2", 26, Tizador.Roja); } },
            { "mensaje_no_documentado", p => {
                p.Texto(10, 26, "Diseño", 17, Tizador.Cian); p.Texto(10, 58, "1  pedir", 19); p.Texto(10, 90, "—", 22, Tizador.Gris);
                p.Texto(190, 26, "Pasó", 17, Tizador.Mostaza); p.Texto(190, 58, "1  pedir", 19); p.Texto(190, 90, "2  reintentar", 19, Tizador.Mostaza); } },
            { "participante_equivocado", p => {
                p.Caja(10, 30, 90, 48, "Tienda", null, false, 16); p.Caja(130, 30, 90, 48, "Pagos", null, false, 16); p.Caja(240, 30, 90, 48, "Bodega", null, false, 16);
                p.Linea(55, 78, 55, 170, Tizador.Gris, 2, true); p.Linea(175, 78, 175, 170, Tizador.Gris, 2, true); p.Linea(285, 78, 285, 170, Tizador.Gris, 2, true);
                p.Flecha(55, 110, 280, 110, Tizador.Roja); p.Texto(170, 102, "cobrar()", 17, Tizador.Roja, true); } },
            { "historia_reescrita", p => {
                p.Linea(40, 20, 40, 170, Tizador.Cian); p.Nodo(40, 50, Tizador.Cian, "a"); p.Nodo(40, 100, Tizador.Cian, "b"); p.Nodo(40, 150, Tizador.Cian, "c");
                p.Linea(200, 50, 200, 170, Tizador.Mostaza); p.Nodo(200, 50, Tizador.Mostaza, "a'"); p.Nodo(200, 100, Tizador.Mostaza, "b'"); p.Nodo(200, 150, Tizador.Mostaza, "c'");
                p.Texto(95, 60, "=", 30, Tizador.Mostaza); } },
            { "autoria_perdida", p => {
                p.Nodo(40, 60, Tizador.Cian); p.Persona(110, 40, Tizador.Cian, "Ana"); p.Nodo(40, 140, Tizador.Mostaza); p.Persona(110, 120, Tizador.Roja, "Beto");
                p.Texto(170, 80, "mismo código", 17); p.Texto(170, 160, "firmado por otro", 18, Tizador.Roja); p.Flecha(40, 76, 40, 124, null, 2); } },
            { "rama_huerfana", p => {
                p.Linea(40, 20, 40, 170, Tizador.Cian); p.Nodo(40, 40, Tizador.Cian); p.Nodo(40, 90, Tizador.Cian); p.Nodo(40, 140, Tizador.Cian);
                p.Nodo(190, 90, Tizador.Mostaza); p.Circulo(190, 90, 28, Tizador.Mostaza, true, 2); } },
            { "fusion_sin_revisar", p => {
                p.Linea(40, 20, 40, 170, Tizador.Cian); p.Linea(140, 40, 140, 100); p.Curva(140, 100, 140, 130, 48, 140);
                p.Nodo(40, 140, Tizador.Mostaza, "merge"); p.Persona(250, 50, Tizador.Tiza, "revisor"); p.Cruz(250, 40); } },
        };
    }

    /// <summary>
    /// La receta: una pizarra, al estilo de las recetas de Overcooked, que explica el reto con dibujos y muy pocas
    /// palabras. Sale sola al empezar (con la etiqueta «¡NUEVO RETO!», quien espera y por que) y con el boton
    /// «Receta» en cualquier momento. Mientras esta abierta, el reloj del reto espera.
    /// </summary>
    public sealed class PantallaDeReceta : Pantalla {
        public MinijuegoDef Def;
        public bool Inicio;
        public Action AlCerrar;

        public override bool EsModal { get { return true; } }
        public override bool PuedeVolver { get { return false; } }

        private const float W = 1612, H = 932;

        protected override void Construir() {
            var velo = Raiz.gameObject.AddComponent<Image>();
            velo.color = new Color(0, 0, 0, 0.66f);

            var marco = Ui.Nodo(Raiz, "Marco");
            marco.anchorMin = marco.anchorMax = marco.pivot = new Vector2(0.5f, 0.5f);
            marco.sizeDelta = new Vector2(W + 28, H + 28);
            var madera = marco.gameObject.AddComponent<Image>();
            madera.sprite = Ui.SpriteRedondeado(); madera.type = Image.Type.Sliced;
            madera.color = new Color32(0x3A, 0x2E, 0x22, 0xFF);

            var tabla = Ui.Nodo(marco, "Pizarra");
            UiKit.Rellenar(tabla, 14);
            var fondo = tabla.gameObject.AddComponent<Image>();
            fondo.color = Tizador.Pizarra;

            var lamina = new Lamina(Ui, tabla, W, H);
            UiKit.Rellenar(lamina.Raiz);
            lamina.Dibujo.Tiza = true;
            var t = new Tizador(lamina, Ui.FuenteTiza);
            Pintar(t);

            // la nota pegada en la esquina
            var nota = Ui.Nodo(marco, "Nota");
            nota.anchorMin = nota.anchorMax = new Vector2(1, 1);
            nota.pivot = new Vector2(0.5f, 0.5f);
            nota.anchoredPosition = new Vector2(-70, -50);
            nota.sizeDelta = new Vector2(230, 56);
            nota.localRotation = Quaternion.Euler(0, 0, -9);
            nota.gameObject.AddComponent<Image>().color = new Color32(0xE7, 0xEE, 0xF0, 0xFF);
            var tn = Ui.Texto(nota, Inicio ? "¡NUEVO RETO!" : "RECETA", EstiloTexto.Subtitulo, new Color32(0x2E, 0x4A, 0x52, 0xFF), TextAlignmentOptions.Center);
            tn.font = Ui.FuenteTiza;
            UiKit.Rellenar((RectTransform)tn.transform);

            var boton = Ui.Boton(marco, Inicio ? "¡A jugar!" : "Volver al reto", Cerrar, VarianteBoton.Primario);
            var rb = (RectTransform)boton.transform;
            rb.anchorMin = rb.anchorMax = rb.pivot = new Vector2(1, 0);
            rb.anchoredPosition = new Vector2(-52, 44);
            rb.sizeDelta = new Vector2(220, 60);
        }

        private void Cerrar() {
            App.Router.Volver();
            AlCerrar?.Invoke();
        }

        private void Pintar(Tizador t) {
            var paleta = Def.PaletaEtiquetas ?? new List<string>();
            var verbo = Verbos.Normalizar(Def.Verbo);

            t.Texto(W / 2, 70, (Def.Presentacion.Titulo ?? Def.Id).ToUpperInvariant(), 46, null, true);
            t.Linea(420, 88, 1190, 88, null, 2);

            float y0 = 110;
            if (Inicio && !string.IsNullOrEmpty(Def.Presentacion.QuienEspera)) {
                var c = t.En(60, y0);
                c.Persona(30, 18, Tizador.Mostaza);
                c.Texto(70, 32, $"{Def.Presentacion.QuienEspera} espera · {Def.Presentacion.TextoPresion}", 24, Tizador.Mostaza);
                y0 += 70;
            }
            t.Texto(60, y0 + 10, "CÓMO SE JUEGA", 26, Tizador.Cian);

            var una = paleta.Count <= 4;
            var kP = verbo == Verbos.Detectar && !una && Inicio ? 1f : 1.18f;
            var pasos = t.En(70, y0 + 30, kP);
            if (verbo == Verbos.Ordenar) PasosOrdenar(pasos);
            else if (verbo == Verbos.Repartir) PasosRepartir(pasos);
            else PasosDetectar(pasos, paleta);
            var yL = y0 + 30 + 200 * kP + 20;

            string pie;
            if (verbo == Verbos.Ordenar) {
                pie = "ORDENA LO QUE MÁS VALE ARRIBA, RESPETA LO QUE VA ANTES Y CONTESTA AL CLIENTE.";
                t.Texto(60, yL, "¿QUÉ SIGNIFICA CADA RESPUESTA?", 26, Tizador.Cian);
                var r = new[] {
                    new { n = "Sí a todo", q = "Prometes lo que no cabe: el equipo lo paga en horas y atajos.", c = Tizador.Roja },
                    new { n = "No", q = "Proteges al equipo, pero el cliente no elige qué se queda fuera.", c = Tizador.Mostaza },
                    new { n = "Negociar", q = "Le enseñas lo que cabe y le dejas elegir.", c = Tizador.Cian } };
                for (var i = 0; i < r.Length; i++) {
                    var k = t.En(60 + i * 505, yL + 30);
                    k.Caja(0, 0, 480, 260, null, r[i].c, true);
                    k.Texto(24, 48, r[i].n, 30, r[i].c);
                    k.Texto(24, 90, r[i].q, 24, null, false, 430);
                }
            } else if (verbo == Verbos.Repartir) {
                pie = "REPARTE LAS HORAS ENTRE LAS PILAS: CADA UNA ATRAPA SOLO SUS ERRORES.";
                t.Texto(60, yL, "EL TRUCO", 26, Tizador.Cian);
                t.Texto(60, yL + 50, "No alcanza para todo. Mira la descripción de cada tipo: ¿dónde es más probable que haya errores?", 26);
                t.Texto(60, yL + 96, "Una pila vacía no atrapa nada, por barata que sea.", 26, Tizador.Mostaza);
                var tr = t.En(260, yL + 150);
                for (var i = 0; i < 4; i++) {
                    tr.Pila(i * 110, 0, 70, 150, i == 0 ? 1 : 0);
                    if (i > 0) { tr.Bicho(i * 110 + 35, 120, Tizador.Roja); tr.Flecha(i * 110 + 35, 100, i * 110 + 35, -10, Tizador.Roja, 2); }
                }
                tr.Texto(190, 190, "todo en una pila = por las otras se escapa todo", 22, Tizador.Roja, true);
                var bu = t.En(900, yL + 150);
                var niveles = new[] { 0.5f, 0.35f, 0.25f, 0.4f };
                for (var i = 0; i < 4; i++) { bu.Pila(i * 110, 0, 70, 150, niveles[i]); bu.Check(i * 110 + 35, -18); }
                bu.Texto(210, 190, "repartido según el riesgo = se escapa poco", 22, Tizador.Cian, true);
            } else {
                pie = "PINCHA LO QUE ESTÁ MAL, DILE QUÉ LE PASA Y ENTREGA ANTES DE QUE ACABE EL RELOJ.";
                t.Texto(60, yL, "¿CUÁNDO ES CADA COSA?  (ejemplos de otra tienda, no de tu reto)", 26, Tizador.Cian);
                var cols = paleta.Count > 4 ? 3 : Math.Max(1, paleta.Count);
                var w = 1500f / cols;
                var hc = una ? 330f : 210f;
                var esc = una ? 1f : 0.8f;
                for (var i = 0; i < paleta.Count; i++) {
                    var id = paleta[i];
                    var cx = 60 + (i % cols) * w;
                    var cy = yL + 20 + (i / cols) * (hc + 15);
                    var cuadro = t.En(cx, cy);
                    cuadro.Caja(0, 0, w - 30, hc, null, Tizador.Gris, true);
                    cuadro.Texto(16, 30, Textos.Humanizar(id).ToUpperInvariant(), 22, Tizador.Mostaza);
                    Action<Tizador> dibujo;
                    if (Recetas.Dibujos.TryGetValue(id, out dibujo))
                        dibujo(t.En(cx + (w - 30 - 330 * esc) / 2, cy + (una ? 60 : 40), esc));
                    string que;
                    cuadro.Texto((w - 30) / 2, hc - 14, Recetas.QueEs.TryGetValue(id, out que) ? que : "", una ? 22 : 19, null, true);
                }
            }
            t.Texto(700, 912, pie, 26, null, true);
        }

        private static void PasosDetectar(Tizador p, List<string> paleta) {
            var a = p.En(0, 0); a.Caja(0, 30, 150, 70, "pieza"); a.Mano(110, 70); a.Texto(75, 150, "1 · Pincha lo raro", 22, null, true);
            p.Curva(180, 60, 220, 30, 260, 60);
            var b = p.En(290, 0);
            for (var i = 0; i < Math.Min(3, paleta.Count); i++) b.Chip(-20, 20 + i * 42, Textos.Humanizar(paleta[i]), i == 1);
            b.Texto(70, 170, "2 · Dile qué le pasa", 22, null, true);
            p.Curva(520, 60, 550, 30, 580, 60);
            var c = p.En(600, 0); c.Reloj(60, 60); c.Caja(110, 40, 110, 42, "Entregar", Tizador.Cian); c.Texto(110, 150, "3 · Entrega a tiempo", 22, null, true);
            var d = p.En(920, 0); d.Caja(0, 30, 130, 70, "bien", Tizador.Cian); d.Check(105, 65);
            d.Texto(160, 72, "marcar", 20); d.Texto(160, 100, "= resta", 22, Tizador.Roja);
            d.Texto(110, 150, "¡Ojo! Lo que está bien no se marca", 19, Tizador.Mostaza, true);
        }

        private static void PasosOrdenar(Tizador p) {
            var a = p.En(0, 0);
            a.Caja(0, 10, 170, 36, "A"); a.Caja(0, 54, 170, 36, "B", Tizador.Cian); a.Caja(0, 98, 170, 36, "C"); a.Mano(150, 60);
            a.Texto(85, 170, "1 · Arrastra: arriba lo primero", 22, null, true);
            var b = p.En(300, 0);
            b.Caja(0, 10, 170, 36, "A"); b.Caja(0, 54, 170, 36, "B"); b.Linea(-10, 100, 180, 100, Tizador.Mostaza, 4, true); b.Caja(0, 112, 170, 36, "C", Tizador.Gris);
            b.Texto(85, 170, "2 · Lo de abajo no entra", 22, null, true);
            var c = p.En(600, 0);
            c.Caja(0, 10, 110, 40, "Base"); c.Caja(0, 100, 110, 40, "Techo"); c.Flecha(55, 52, 55, 96, Tizador.Mostaza);
            c.Texto(125, 82, "necesita", 18, Tizador.Mostaza); c.Texto(90, 170, "3 · Hay cosas que van antes", 22, null, true);
            var d = p.En(900, 0);
            d.Persona(40, 20, Tizador.Mostaza, "cliente"); d.Texto(100, 34, "«¡todo!»", 22, Tizador.Mostaza);
            d.Chip(90, 60, "Sí a todo", false); d.Chip(90, 100, "No", false); d.Chip(200, 100, "Negociar", true);
            d.Texto(150, 170, "4 · Contesta al cliente", 22, null, true);
        }

        private static void PasosRepartir(Tizador p) {
            var a = p.En(0, 0); a.Pila(30, 10, 60, 120, 0.42f); a.Texto(120, 60, "+2", 26, Tizador.Cian); a.Texto(120, 100, "−2", 26);
            a.Texto(70, 170, "1 · Dale horas a cada pila", 22, null, true);
            var b = p.En(360, 0); b.Pila(20, 10, 60, 120, 0); b.Bicho(50, 100, Tizador.Cian); b.Check(110, 100); b.Bicho(160, 100, Tizador.Roja); b.Cruz(160, 60, Tizador.Roja, 10);
            b.Texto(100, 170, "2 · Cada pila atrapa SOLO lo suyo", 22, null, true);
            var c = p.En(740, 0); c.Bicho(20, 70, Tizador.Roja); c.Flecha(40, 70, 140, 70, Tizador.Roja); c.Persona(175, 45, Tizador.Roja, "cliente");
            c.Texto(100, 170, "3 · Lo que se escapa, llega al cliente", 22, null, true);
            var d = p.En(1100, 0); d.Pila(40, 10, 60, 120, 1); d.Texto(70, 170, "Pila llena = TODAS las horas ahí", 19, Tizador.Mostaza, true);
        }
    }
}
