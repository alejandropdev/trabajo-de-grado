using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Una columna en la que se «escribe» de arriba abajo: etiqueta, titulo, parrafos, botones. Casi todas las
    /// pantallas del juego son eso, y sin este envoltorio cada una repetiria las mismas cinco lineas de UiKit.
    /// </summary>
    public sealed class Hoja {
        private readonly UiKit _ui;
        public RectTransform Raiz { get; private set; }

        public Hoja(UiKit ui, RectTransform raiz) {
            _ui = ui;
            Raiz = raiz;
        }

        private NexusTheme Tema { get { return _ui.Tema; } }

        public void Vaciar() { UiKit.Vaciar(Raiz); }

        /// <summary>La micro-etiqueta sobre un titulo (caption en cyan, mayusculas por estilo).</summary>
        public TMP_Text Etiqueta(string texto, Color? color = null) {
            var t = _ui.Texto(Raiz, texto, EstiloTexto.Leyenda, color ?? Tema.cyan);
            t.fontStyle |= FontStyles.UpperCase;
            return t;
        }

        public TMP_Text Titulo(string texto) { return _ui.Texto(Raiz, texto, EstiloTexto.Titulo); }
        public TMP_Text Subtitulo(string texto, Color? color = null) { return _ui.Texto(Raiz, texto, EstiloTexto.Subtitulo, color); }
        public TMP_Text Parrafo(string texto, Color? color = null) { return _ui.Texto(Raiz, texto, EstiloTexto.Cuerpo, color); }
        public TMP_Text Nota(string texto, Color? color = null) { return _ui.Texto(Raiz, texto, EstiloTexto.Pequeno, color); }
        public TMP_Text Mono(string texto) { return _ui.Texto(Raiz, texto, EstiloTexto.Mono); }

        public void Espacio(float alto = -1) { _ui.Espaciador(Raiz, alto < 0 ? Tema.espacio : alto); }
        public void Separador() { _ui.Separador(Raiz); }

        public RectTransform Fila(float? espacio = null) { return _ui.Fila(Raiz, "Fila", espacio); }

        /// <summary>Un boton en su propia fila, precedido de un poco de aire: la accion con la que se sigue.</summary>
        public Button Accion(string texto, Action alPulsar, VarianteBoton variante = VarianteBoton.Primario) {
            Espacio();
            return _ui.Boton(Fila(), texto, alPulsar, variante);
        }

        public Button Opcion(string texto, string detalle, Action alPulsar) {
            return _ui.BotonDeOpcion(Raiz, texto, detalle, alPulsar);
        }

        public RectTransform Tarjeta(string titulo, Color? color = null) { return _ui.Tarjeta(Raiz, titulo, color); }
        public RectTransform Tarjeta(string titulo, Tono tono) { return _ui.Tarjeta(Raiz, titulo, tono); }

        /// <summary>Una linea de un personaje (DialogueBox). quien null o «narrador» = sin retrato.</summary>
        public TMP_Text Dialogo(string quien, string texto, Tono retrato = Tono.Cyan) {
            TMP_Text linea;
            _ui.Dialogo(Raiz, Textos.Hablante(quien), texto, out linea, retrato);
            return linea;
        }
    }

    /// <summary>Lo que varias pantallas necesitan decir igual: veredictos, cambios de estado, etiquetas.</summary>
    public static class Textos {
        public static string Veredicto(string veredicto) {
            switch (veredicto) {
                case "correcta": return "Correcta";
                case "aceptable": return "Aceptable";
                case "incorrecta": return "Incorrecta";
                default: return veredicto ?? "—";
            }
        }

        public static Color ColorDe(NexusTheme tema, string veredicto) {
            var tono = TonoDe(veredicto);
            return tono == Tono.Neutro ? tema.inkMuted : tema.ColorDe(tono);
        }

        /// <summary>Correcta = hecho (success), aceptable = aviso, incorrecta = danger.</summary>
        public static Tono TonoDe(string veredicto) {
            switch (veredicto) {
                case "correcta": return Tono.Exito;
                case "aceptable": return Tono.Aviso;
                case "incorrecta": return Tono.Peligro;
                default: return Tono.Neutro;
            }
        }

        /// <summary>"requisito_ambiguo" → "Requisito ambiguo". Los ids de etiqueta son para el motor, no para leer.</summary>
        public static string Humanizar(string id) {
            if (string.IsNullOrEmpty(id)) return "";
            var t = id.Replace('_', ' ').Replace('-', ' ');
            return char.ToUpperInvariant(t[0]) + t.Substring(1);
        }

        private static readonly Dictionary<string, string> NombresDeStock = new Dictionary<string, string> {
            { "Avance", "Avance" }, { "DeudaTecnica", "Deuda" }, { "MoralEquipo", "Moral" }, { "Cobertura", "Cobertura" },
            { "Documentacion", "Documentación" }, { "SatisfaccionCliente", "Cliente" }, { "Alcance", "Alcance" },
            { "Dias", "Días gastados" }, { "Cansancio", "Cansancio" }, { "Reputacion", "Reputación" },
            { "Competencia", "Competencia" }, { "SaludJugador", "Salud" }, { "Dinero", "Dinero" }
        };

        public static string NombreDeStock(string id) {
            string n;
            return NombresDeStock.TryGetValue(id, out n) ? n : id;
        }

        public static string Cambio(double v) {
            return v >= 0 ? "+" + v.ToString("0.#") : "−" + (-v).ToString("0.#");
        }

        public static string Previsualizar(IDictionary<string, double> cambios) {
            if (cambios == null || cambios.Count == 0) return "Sin cambios inmediatos.";
            var partes = cambios.Where(kv => Math.Abs(kv.Value) >= 0.05)
                                .Select(kv => NombreDeStock(kv.Key) + " " + Cambio(kv.Value)).ToList();
            return partes.Count == 0 ? "Sin cambios inmediatos." : string.Join(" · ", partes);
        }

        /// <summary>El nombre de quien habla, o null si es el narrador (el narrador no tiene nombre en pantalla).</summary>
        public static string Hablante(string quien) {
            if (string.IsNullOrEmpty(quien) || quien == "narrador") return null;
            return quien;
        }
    }
}
