using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// Dibuja en una Lamina con coordenadas «de prototipo» (las del mockup de pizarra) desplazadas y a escala.
    /// Asi cada viñeta se escribe en su propio cuadro de 330×190 y se coloca donde toque.
    /// </summary>
    public sealed partial class Tizador {
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
        /// <summary>Multiplicador del grosor de todos los trazos (1 = el de siempre). Lo fija Grueso.</summary>
        private float _peso = 1f;

        public Tizador(Lamina lamina, TMP_FontAsset fuente, float ox = 0, float oy = 0, float k = 1) {
            _l = lamina; _fuente = fuente; _ox = ox; _oy = oy; _k = k;
        }

        public Tizador En(float x, float y, float k = 1) { return new Tizador(_l, _fuente, _ox + x * _k, _oy + y * _k, _k * k) { _capa = _capa, _peso = _peso }; }

        /// <summary>Una copia con el trazo mas grueso (p veces): para pictogramas que se leen de lejos.</summary>
        public Tizador Grueso(float p) { return new Tizador(_l, _fuente, _ox, _oy, _k) { _capa = _capa, _peso = _peso * p }; }

        private float X(float x) { return _ox + x * _k; }
        private float Y(float y) { return _oy + y * _k; }
        private Vector2 P(float x, float y) { return new Vector2(X(x), Y(y)); }
        private DibujoUI D { get { return _l.Dibujo; } }

        public void Linea(float x1, float y1, float x2, float y2, Color? c = null, float g = 3, bool disc = false) {
            D.Linea(X(x1), Y(y1), X(x2), Y(y2), c ?? Tiza, g * _k * _peso, disc);
        }
        public void Flecha(float x1, float y1, float x2, float y2, Color? c = null, float g = 3) {
            Linea(x1, y1, x2, y2, c, g);
            D.Punta(P(x1, y1), P(x2, y2), c ?? Tiza, g * _k * _peso, 13 * _k);
        }
        public void Curva(float x1, float y1, float cx, float cy, float x2, float y2, Color? c = null, bool punta = true) {
            D.Curva(P(x1, y1), P(cx, cy), P(x2, y2), c ?? Tiza, 3 * _k * _peso, punta);
        }
        public void Caja(float x, float y, float w, float h, string t = null, Color? c = null, bool disc = false, float tam = 19) {
            D.Rect(X(x), Y(y), w * _k, h * _k, c ?? Tiza, 3 * _k * _peso, new Color(1, 1, 1, 0.04f), disc, 8 * _k);
            if (t != null) Texto(x + w / 2, y + h / 2 + 7, t, tam, c ?? Tiza, true);
        }
        public void Cilindro(float x, float y, float w, float h, Color? c = null) { D.Cilindro(X(x), Y(y), w * _k, h * _k, c ?? Tiza, 3 * _k * _peso); }
        public void Circulo(float x, float y, float r, Color? c = null, bool disc = false, float g = 3) { D.Circulo(X(x), Y(y), r * _k, c ?? Tiza, g * _k * _peso, null, disc); }
        /// <summary>Un circulo macizo con su numero, por ENCIMA de las lineas del dibujo (tapa la flecha que pasa debajo).</summary>
        public void Insignia(float x, float y, float r, string numero, Color fondo, Color colorTexto) {
            _l.Insignia(X(x), Y(y), r * _k, numero, fondo, colorTexto, Pizarra);
        }
        public void Nodo(float x, float y, Color c, string t = null) {
            D.Circulo(X(x), Y(y), 12 * _k, c, 3.5f * _k, Pizarra);
            if (t != null) Texto(x + 20, y + 7, t, 16, c);
        }
        public void Persona(float x, float y, Color? c = null, string t = null) {
            D.Persona(X(x), Y(y), c ?? Tiza, 3 * _k * _peso);
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

        /// <summary>
        /// Texto con la letra de tiza. (x, y) es la linea base, como en SVG; 'centrado' = text-anchor middle.
        /// Una etiqueta suelta (sin 'ancho') mide lo que mide su texto de verdad, no una estimacion por letras; y si cae
        /// encima de otra etiqueta ya escrita en la misma lamina, baja una linea (hasta tres) en vez de taparla.
        /// </summary>
        public TMP_Text Texto(float x, float y, string t, float tam, Color? c = null, bool centrado = false, float ancho = 0) {
            var w = (ancho > 0 ? ancho : Mathf.Max(60, t.Length * tam * 0.62f + 20)) * _k;
            var alto = tam * 1.5f * _k;
            var x0 = centrado ? X(x) - w / 2 : X(x);
            var y0 = Y(y) - tam * 1.05f * _k;
            var texto = _l.Texto(x0, y0, w, alto, t, tam * _k, c ?? Tiza,
                                 centrado ? TextAlignmentOptions.Top : TextAlignmentOptions.TopLeft, _fuente);
            texto.textWrappingMode = ancho > 0 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            if (ancho <= 0) {
                w = Mathf.Max(24 * _k, texto.GetPreferredValues(t ?? "").x + 8 * _k);
                x0 = centrado ? X(x) - w / 2 : X(x);
            }
            var sitio = _l.Reservar(new Rect(x0, y0, w, alto), alto * 0.9f);
            Lamina.Colocar((RectTransform)texto.transform, sitio.x, sitio.y, sitio.width, sitio.height);
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

        /// <summary>Que significa una etiqueta, en una linea. null si no hay texto para ella.</summary>
        public static string Significado(string etiqueta) {
            string que;
            return etiqueta != null && QueEs.TryGetValue(etiqueta, out que) ? que : null;
        }
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
        /// <summary>Las ayudas de compañeros que sirven para este reto (tipos). Solo se ofrecen antes de empezar.</summary>
        public List<string> Ayudas = new List<string>();
        /// <summary>Gasta una ayuda; true si se pudo.</summary>
        public Func<string, bool> AlUsarAyuda;

        public override bool EsModal { get { return true; } }
        public override bool PuedeVolver { get { return false; } }

        private const float W = 1612, H = 932;

        private Lamina _lamina;
        private bool _viendoProyecto;
        private TMPro.TMP_Text _textoPrincipal;
        private GameObject _volverAlProyecto;

        /// <summary>El expediente del nivel y el modulo de este reto, si los hay (para la pagina «Dónde estamos»).</summary>
        private Nexus.Core.Proyecto.FichaDelProyecto Proyecto {
            get { return App.Sesion != null && App.Sesion.Perfil != null ? App.Sesion.Perfil.Proyecto : null; }
        }

        private bool HayDondeEstamos { get { return Proyecto != null && Proyecto.Modulo(Def.Modulo) != null; } }

        protected override void Construir() {
            var marco = MarcoDePizarra.Construir(Ui, Raiz, Inicio ? "¡NUEVO RETO!" : "RECETA", out _lamina, true);
            // Al empezar un reto, lo primero es saber en qué parte del proyecto estamos (ronda 4); después, cómo se juega.
            _viendoProyecto = Inicio && HayDondeEstamos;
            // Un solo boton principal, siempre abajo a la derecha, que es por donde se avanza: en «Dónde estamos» lleva
            // a «Cómo se juega», y alli es «¡A jugar!». Volver atras queda a su izquierda, mas discreto.
            var principal = MarcoDePizarra.Boton(Ui, marco, "", Avanzar, new Vector2(1, 0), new Vector2(-52, 44), 260);
            _textoPrincipal = principal.GetComponentInChildren<TMPro.TMP_Text>();
            if (HayDondeEstamos)
                _volverAlProyecto = MarcoDePizarra.Boton(Ui, marco, "◄  Dónde estamos", CambiarPagina, new Vector2(1, 0), new Vector2(-330, 44), 260,
                                                         VarianteBoton.Secundario).gameObject;
            PintarPagina();

            // Las ayudas de los compañeros que confian en ti: se eligen aqui, antes de empezar.
            if (Inicio && Ayudas != null && Ayudas.Count > 0 && AlUsarAyuda != null) {
                var fila = Ui.Fila(marco, "Ayudas", 10);
                fila.anchorMin = fila.anchorMax = fila.pivot = new Vector2(0, 0);
                fila.anchoredPosition = new Vector2(52, 44);
                fila.sizeDelta = new Vector2(HayDondeEstamos ? 960 : 1240, 60);
                var titulo = Ui.Texto(fila, "Te ofrecen ayuda:", EstiloTexto.Cuerpo, Tema.mostazaClara);
                titulo.font = Ui.Tema.FuenteInterfazNegrita;
                foreach (var tipo in Ayudas) {
                    var ayuda = tipo;
                    Button b = null;
                    b = Ui.Boton(fila, "Usar: " + NombreCorto(ayuda), () => {
                        if (!AlUsarAyuda(ayuda)) return;
                        b.interactable = false;
                        b.GetComponentInChildren<TMPro.TMP_Text>().text = Nexus.Core.Narrativa.Glifos.Seguro("√ " + NombreCorto(ayuda));
                    });
                }
            }
        }

        private static string NombreCorto(string tipo) {
            switch (tipo) {
                case Nexus.Core.Relaciones.TiposDeAyuda.RevelarDefectos: return "cuántos errores hay";
                case Nexus.Core.Relaciones.TiposDeAyuda.MostrarDependencias: return "ver dependencias";
                case Nexus.Core.Relaciones.TiposDeAyuda.PistaDetectar: return "una pista";
                case Nexus.Core.Relaciones.TiposDeAyuda.MinutosExtra: return "+45 s de reloj";
                default: return tipo;
            }
        }

        private void Cerrar() {
            App.Router.Volver();
            AlCerrar?.Invoke();
        }

        private void Avanzar() {
            if (_viendoProyecto) CambiarPagina(); else Cerrar();
        }

        private void CambiarPagina() {
            _viendoProyecto = !_viendoProyecto;
            PintarPagina();
        }

        private void PintarPagina() {
            MarcoDePizarra.Limpiar(_lamina);
            // Rajdhani y trazo limpio (docs/08-arte): lo unico que se pinta en el DibujoUI es el diagrama de contexto, un plano.
            _lamina.Dibujo.Tiza = false;
            var t = new Tizador(_lamina, Ui.Tema.FuenteInterfaz);
            if (_viendoProyecto) PintarDondeEstamos(t); else Pintar(t);
            // Glifos.Seguro: el boton ya es neon, con la letra del design system, que no tiene «►».
            if (_textoPrincipal != null)
                _textoPrincipal.text = Nexus.Core.Narrativa.Glifos.Seguro(_viendoProyecto ? "Cómo se juega  ►" : Inicio ? "¡A jugar!" : "Volver al reto");
            if (_volverAlProyecto != null) _volverAlProyecto.SetActive(!_viendoProyecto);
        }

        /// <summary>El sistema del proyecto, con la parte de este reto en mostaza.</summary>
        private void PintarDondeEstamos(Tizador t) {
            var f = Proyecto;
            var modulo = f.Modulo(Def.Modulo);
            t.Texto(W / 2, 70, "DÓNDE ESTAMOS EN EL PROYECTO", 46, null, true);
            t.Forma(Nexus.Unity.Tema.LuzUI.Redondeado(W / 2 - 60, 84, 120, 4, 2), PanelDelPizarron.Acento);
            t.Texto(60, 140, $"{f.Nombre} · para {f.Cliente.Nombre}", 26, Tizador.Gris);
            if (!string.IsNullOrEmpty(Def.EnElProyecto)) t.Texto(60, 182, Def.EnElProyecto, 26, Tizador.Tiza, false, W - 120);
            // Los botones del marco ocupan de 842 hacia abajo: el diagrama y su pie acaban antes.
            ExpedienteView.DibujarContexto(t, f, 60, 250, W - 120, 530, Def.Modulo);
            t.Texto(W / 2, 820, $"EN MOSTAZA, LA PARTE DE ESTE RETO: «{modulo.Nombre.ToUpperInvariant()}»", 26, Tizador.Mostaza, true);
        }

        private void Pintar(Tizador t) {
            var paleta = Def.PaletaEtiquetas ?? new List<string>();
            var verbo = Verbos.Normalizar(Def.Verbo);

            t.Texto(W / 2, 70, (Def.Presentacion.Titulo ?? Def.Id).ToUpperInvariant(), 46, null, true);
            t.Forma(Nexus.Unity.Tema.LuzUI.Redondeado(W / 2 - 60, 84, 120, 4, 2), PanelDelPizarron.Acento);

            float y0 = 110;
            if (Inicio && !string.IsNullOrEmpty(Def.Presentacion.QuienEspera)) {
                var c = t.En(60, y0);
                DibujosDeEscena.RecetaPersona(c.En(0, -8, 0.7f), 30, 0, Tizador.Mostaza);
                c.Texto(70, 32, $"{Def.Presentacion.QuienEspera} espera · {Def.Presentacion.TextoPresion}", 24, Tizador.Mostaza);
                y0 += 70;
            }
            // El diagrama se juega con cartas de solucion (2026-11-09): su receta es otra.
            var conCartas = Nexus.Core.Minijuegos.Corregir.PuntosDelDiagrama.Aplica(Def);
            var una = paleta.Count <= 4;
            var kP = verbo == Verbos.Detectar && !una && !conCartas ? (Inicio ? 0.9f : 1f) : 1.18f;
            var pasos = t.En(70, y0 + 30, kP);
            var puente = Nexus.Core.Minijuegos.Puente.PuenteDelBacklog.Aplica(Def);
            var cual = verbo == Verbos.Ordenar ? (puente ? "puente" : "ordenar") : verbo == Verbos.Repartir ? "repartir" : conCartas ? "corregir" : "detectar";
            DibujosDeEscena.RecetaPasos(pasos, cual, paleta.ConvertAll(Textos.Humanizar));
            var yL = y0 + 30 + 200 * kP + 20;

            string pie;
            if (verbo == Verbos.Ordenar && puente) {
                pie = "ORDENA CADA PILAR DE ABAJO ARRIBA Y VERIFICA: DOS ERRORES EN UN MISMO PILAR Y EL PUENTE SE CAE.";
                t.Texto(60, yL, "EL TRUCO", 26, Tizador.Cian);
                t.Texto(60, yL + 50, "Para cada historia, pregúntate: ¿necesita algo de las otras? Si lo necesita, eso va más abajo.", 26);
                t.Texto(60, yL + 96, "Verificar sube por el pilar y se detiene en el primer fallo: lo de arriba queda sin revisar.", 26, Tizador.Mostaza);
                t.Texto(60, yL + 142, "Los pilares que salen bien se quedan hechos y ya no se tocan: solo repites el que falló.", 26);
            } else if (verbo == Verbos.Ordenar) {
                pie = "ORDENA LO QUE MÁS VALE ARRIBA, RESPETA LO QUE VA ANTES Y CONTESTA AL CLIENTE.";
                t.Texto(60, yL, "¿QUÉ SIGNIFICA CADA RESPUESTA?", 26, Tizador.Cian);
                var r = new[] {
                    new { n = "Sí a todo", q = "Prometes lo que no cabe: el equipo lo paga en horas y atajos.", c = Tizador.Roja },
                    new { n = "No", q = "Proteges al equipo, pero el cliente no elige qué se queda fuera.", c = Tizador.Mostaza },
                    new { n = "Negociar", q = "Le enseñas lo que cabe y le dejas elegir.", c = Tizador.Cian } };
                for (var i = 0; i < r.Length; i++) {
                    var k = t.En(60 + i * 505, yL + 30);
                    k.TarjetaDeChapa(0, 0, 480, 260);
                    DibujosDeEscena.RecetaBarra(k, 0, 0, 480, r[i].c);
                    k.Texto(24, 48, r[i].n, 30, r[i].c);
                    k.Texto(24, 90, r[i].q, 24, null, false, 430);
                }
            } else if (verbo == Verbos.Repartir) {
                pie = "REPARTE LAS HORAS ENTRE LAS PILAS: CADA UNA ATRAPA SOLO SUS ERRORES.";
                t.Texto(60, yL, "EL TRUCO", 26, Tizador.Cian);
                t.Texto(60, yL + 50, "No alcanza para todo. Mira cada tipo: ¿cuántos errores suele haber y cuánto cuesta atrapar cada uno?", 26);
                t.Texto(60, yL + 96, "Llena primero lo barato con muchos errores. Si no alcanza, deja fuera lo más caro y menos probable.", 26, Tizador.Mostaza);
                var tr = t.En(260, yL + 150);
                DibujosDeEscena.RecetaPilas(tr, new[] { 1f, 0f, 0f, 0f }, false);
                tr.Texto(190, 190, "todo en una pila = por las otras se escapa todo", 22, Tizador.Roja, true);
                var bu = t.En(900, yL + 150);
                DibujosDeEscena.RecetaPilas(bu, new[] { 0.5f, 0.35f, 0.25f, 0.4f }, true);
                bu.Texto(210, 190, "repartido según el riesgo = se escapa poco", 22, Tizador.Cian, true);
            } else if (conCartas) {
                pie = "ELIGE UN PUNTO, JUÉGALE LA CARTA QUE LO ARREGLA Y VERIFICA ANTES DE QUE ACABE EL RELOJ.";
                t.Texto(60, yL, "EL TRUCO", 26, Tizador.Cian);
                t.Texto(60, yL + 50, "Cada carta sirve una sola vez: si la pones en un punto, ya no está para otro.", 26);
                t.Texto(60, yL + 96, "Como mucho un punto está bien: para ese, «Está bien así». Arreglar lo que funciona también cuenta en contra.", 26, Tizador.Mostaza);
                var mano = t.En(300, yL + 160);
                DibujosDeEscena.RecetaMano(mano);
                mano.Texto(230, 190, "una carta jugada se queda hundida: no se repite", 22, Tizador.Mostaza, true);
                var bien = t.En(1000, yL + 160);
                DibujosDeEscena.RecetaBien(bien);
                bien.Texto(60, 190, "«Está bien así»: también una sola vez", 22, Tizador.Cian, true);
            } else {
                pie = "PINCHA LO QUE ESTÁ MAL, DILE QUÉ LE PASA Y ENTREGA ANTES DE QUE ACABE EL RELOJ.";
                t.Texto(60, yL, "¿CUÁNDO ES CADA COSA?  (ejemplos de otra tienda, no de tu reto)", 26, Tizador.Cian);
                var cols = paleta.Count > 4 ? 3 : Math.Max(1, paleta.Count);
                var w = 1500f / cols;
                // Dos filas de cuadros tienen que acabar antes de los botones del marco.
                var filas = Mathf.CeilToInt(paleta.Count / (float)cols);
                var hc = una ? 330f : Mathf.Min(210f, (Fondo - (yL + 20) - 15 * (filas - 1)) / filas);
                var esc = una ? 1f : 0.8f * hc / 210f;
                for (var i = 0; i < paleta.Count; i++) {
                    var id = paleta[i];
                    var cx = 60 + (i % cols) * w;
                    var cy = yL + 20 + (i / cols) * (hc + 15);
                    var cuadro = t.En(cx, cy);
                    cuadro.TarjetaDeChapa(0, 0, w - 30, hc);
                    cuadro.Texto(16, 30, Textos.Humanizar(id).ToUpperInvariant(), 22, Tizador.Mostaza);
                    DibujosDeEscena.RecetaVineta(t.En(cx + (w - 30 - 330 * esc) / 2, cy + (una ? 60 : 40), esc), id);
                    string que;
                    cuadro.Texto((w - 30) / 2, hc - 14, Recetas.QueEs.TryGetValue(id, out que) ? que : "", una ? 22 : 19, null, true);
                }
            }
            t.Texto(60, y0 + 10, "CÓMO SE JUEGA · " + pie, 22, Tizador.Cian);
        }

        /// <summary>Hasta donde se puede dibujar: debajo estan los botones del marco.</summary>
        private const float Fondo = 830;
    }
}
