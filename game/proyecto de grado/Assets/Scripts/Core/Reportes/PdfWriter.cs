using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Nexus.Core.Reportes {
    /// <summary>Las cuatro fuentes estandar del PDF (no hace falta incrustarlas: todo visor las trae).</summary>
    public enum FuentePdf { Normal, Negrita, Cursiva, Mono }

    public readonly struct ColorPdf {
        public readonly double R, G, B;
        public ColorPdf(double r, double g, double b) { R = r; G = g; B = b; }

        public static readonly ColorPdf Negro = new ColorPdf(0, 0, 0);
        public static readonly ColorPdf GrisOscuro = new ColorPdf(0.25, 0.25, 0.25);
        public static readonly ColorPdf Gris = new ColorPdf(0.5, 0.5, 0.5);
        public static readonly ColorPdf GrisClaro = new ColorPdf(0.85, 0.85, 0.85);
    }

    /// <summary>
    /// Escritor de PDF minimo y determinista: paginas A4, texto, lineas y rectangulos. Sin dependencias y sin
    /// Unity, para que lo cubra «dotnet test» como el resto del motor. Usa Times y Courier con WinAnsiEncoding,
    /// que traen todos los visores y cubren las tildes y la «ñ» sin incrustar tipografias. Las coordenadas se dan
    /// desde la esquina SUPERIOR izquierda, en puntos (1/72 de pulgada), como las usa quien maqueta.
    /// El contenido no se comprime: asi el archivo se puede inspeccionar, y la misma entrada da los mismos bytes.
    /// </summary>
    public sealed class PdfWriter {
        public const double AnchoA4 = 595.28;
        public const double AltoA4 = 841.89;

        private readonly string _titulo, _autor, _asunto;
        private readonly DateTime _fecha;
        private readonly List<StringBuilder> _paginas = new List<StringBuilder>();
        private readonly List<List<string>> _textos = new List<List<string>>();

        public PdfWriter(string titulo, string autor, string asunto, DateTime fecha) {
            _titulo = titulo ?? "";
            _autor = autor ?? "";
            _asunto = asunto ?? "";
            _fecha = fecha;
        }

        public int Paginas { get { return _paginas.Count; } }

        /// <summary>Lo que se escribio en una pagina (0 = la primera), para comprobar el contenido sin leer el PDF.</summary>
        public IReadOnlyList<string> TextosDeLaPagina(int pagina) { return _textos[pagina]; }

        public void NuevaPagina() {
            _paginas.Add(new StringBuilder());
            _textos.Add(new List<string>());
        }

        /// <summary>'yBase' es la linea base del texto, medida desde arriba.</summary>
        public void Texto(double x, double yBase, string texto, FuentePdf fuente, double tamano, ColorPdf? color = null) {
            if (string.IsNullOrEmpty(texto)) return;
            var c = ActualOCrear();
            var col = color ?? ColorPdf.Negro;
            c.Append("BT ").Append(Color(col, false)).Append(" /F").Append(Indice(fuente)).Append(' ').Append(N(tamano))
             .Append(" Tf ").Append(N(x)).Append(' ').Append(N(AltoA4 - yBase)).Append(" Td (").Append(Escapar(texto)).Append(") Tj ET\n");
            _textos[_textos.Count - 1].Add(texto);
        }

        public void Linea(double x1, double y1, double x2, double y2, double grosor, ColorPdf? color = null) {
            ActualOCrear().Append(Color(color ?? ColorPdf.Negro, true)).Append(' ').Append(N(grosor)).Append(" w ")
                .Append(N(x1)).Append(' ').Append(N(AltoA4 - y1)).Append(" m ")
                .Append(N(x2)).Append(' ').Append(N(AltoA4 - y2)).Append(" l S\n");
        }

        public void Rectangulo(double x, double y, double ancho, double alto, ColorPdf? relleno, ColorPdf? borde, double grosor = 0.5) {
            if (relleno == null && borde == null) return;
            var c = ActualOCrear();
            if (relleno != null) c.Append(Color(relleno.Value, false)).Append(' ');
            if (borde != null) c.Append(Color(borde.Value, true)).Append(' ').Append(N(grosor)).Append(" w ");
            c.Append(N(x)).Append(' ').Append(N(AltoA4 - y - alto)).Append(' ').Append(N(ancho)).Append(' ').Append(N(alto)).Append(" re ")
             .Append(relleno != null && borde != null ? "B" : relleno != null ? "f" : "S").Append('\n');
        }

        // ================================================================== SALIDA

        public byte[] Generar() {
            if (_paginas.Count == 0) NuevaPagina();
            var n = _paginas.Count;
            var total = 7 + 2 * n;
            var offsets = new long[total + 1];
            var ms = new MemoryStream();

            Escribir(ms, "%PDF-1.4\n%");
            ms.WriteByte(0xE2); ms.WriteByte(0xE3); ms.WriteByte(0xCF); ms.WriteByte(0xD3);
            Escribir(ms, "\n");

            var kids = new StringBuilder();
            for (var i = 0; i < n; i++) kids.Append(8 + 2 * i).Append(" 0 R ");

            Objeto(ms, offsets, 1, "<< /Type /Catalog /Pages 2 0 R /Lang (es-CO) >>");
            Objeto(ms, offsets, 2, "<< /Type /Pages /Kids [" + kids.ToString().TrimEnd() + "] /Count " + n + " >>");
            Objeto(ms, offsets, 3, Fuente("Times-Roman"));
            Objeto(ms, offsets, 4, Fuente("Times-Bold"));
            Objeto(ms, offsets, 5, Fuente("Times-Italic"));
            Objeto(ms, offsets, 6, Fuente("Courier"));
            Objeto(ms, offsets, 7, "<< /Title (" + Escapar(_titulo) + ") /Author (" + Escapar(_autor) + ") /Subject (" + Escapar(_asunto) +
                                   ") /Creator (Nexus Protocol) /Producer (Nexus.Core.Reportes.PdfWriter) /CreationDate (D:" +
                                   _fecha.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ") >>");

            for (var i = 0; i < n; i++) {
                var pagina = 8 + 2 * i;
                var contenido = pagina + 1;
                Objeto(ms, offsets, pagina,
                    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + N(AnchoA4) + " " + N(AltoA4) + "] " +
                    "/Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R /F4 6 0 R >> >> /Contents " + contenido + " 0 R >>");
                var flujo = _paginas[i].ToString();
                Objeto(ms, offsets, contenido, "<< /Length " + flujo.Length + " >>\nstream\n" + flujo + "endstream");
            }

            var inicioXref = ms.Position;
            Escribir(ms, "xref\n0 " + (total + 1) + "\n0000000000 65535 f \n");
            for (var i = 1; i <= total; i++)
                Escribir(ms, offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
            Escribir(ms, "trailer\n<< /Size " + (total + 1) + " /Root 1 0 R /Info 7 0 R >>\nstartxref\n" +
                         inicioXref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
            return ms.ToArray();
        }

        private static string Fuente(string nombre) {
            return "<< /Type /Font /Subtype /Type1 /BaseFont /" + nombre + " /Encoding /WinAnsiEncoding >>";
        }

        private static void Objeto(MemoryStream ms, long[] offsets, int numero, string cuerpo) {
            offsets[numero] = ms.Position;
            Escribir(ms, numero + " 0 obj\n" + cuerpo + "\nendobj\n");
        }

        /// <summary>Todo lo que se escribe es ASCII (el texto sale escapado), asi que un char es un byte.</summary>
        private static void Escribir(MemoryStream ms, string s) {
            foreach (var ch in s) ms.WriteByte((byte)ch);
        }

        private StringBuilder ActualOCrear() {
            if (_paginas.Count == 0) NuevaPagina();
            return _paginas[_paginas.Count - 1];
        }

        private static int Indice(FuentePdf f) {
            switch (f) {
                case FuentePdf.Negrita: return 2;
                case FuentePdf.Cursiva: return 3;
                case FuentePdf.Mono: return 4;
                default: return 1;
            }
        }

        private static string N(double v) {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Color(ColorPdf c, bool trazo) {
            return N(c.R) + " " + N(c.G) + " " + N(c.B) + (trazo ? " RG" : " rg");
        }

        // ================================================================== TEXTO

        /// <summary>El caracter como byte de WinAnsi (Windows-1252). Lo que no esta, sale como «?».</summary>
        public static int Codificar(char c) {
            if (c >= 32 && c <= 126) return c;
            if (c >= 0xA0 && c <= 0xFF) return c;
            switch (c) {
                case '€': return 0x80;
                case '…': return 0x85;
                case '‘': return 0x91;
                case '’': return 0x92;
                case '“': return 0x93;
                case '”': return 0x94;
                case '•': return 0x95;
                case '–': return 0x96;
                case '—': return 0x97;
                case '\t': return 32;
                default: return '?';
            }
        }

        /// <summary>Cadena literal de PDF: parentesis y barra escapados, y todo lo no imprimible en octal.</summary>
        public static string Escapar(string texto) {
            var sb = new StringBuilder(texto.Length + 8);
            foreach (var ch in texto) {
                var b = Codificar(ch);
                if (b == '(' || b == ')' || b == '\\') sb.Append('\\').Append((char)b);
                else if (b < 32 || b > 126) sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                else sb.Append((char)b);
            }
            return sb.ToString();
        }

        private const string ConTilde = "ÀÁÂÃÄÅàáâãäåÈÉÊËèéêëÌÍÎÏìíîïÒÓÔÕÖòóôõöÙÚÛÜùúûüÝýÿÇçÑñ";
        private const string SinTilde = "AAAAAAaaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUUuuuuYyyCcNn";

        /// <summary>«canción» → «cancion». Sirve para el ancho de las letras acentuadas y para los nombres de archivo.</summary>
        public static string QuitarTildes(string texto) {
            if (string.IsNullOrEmpty(texto)) return texto ?? "";
            var sb = new StringBuilder(texto.Length);
            foreach (var ch in texto) {
                var i = ConTilde.IndexOf(ch);
                sb.Append(i >= 0 ? SinTilde[i] : ch);
            }
            return sb.ToString();
        }

        // Anchos AFM de Times, en milesimas de «em», para los caracteres 32..126. Courier mide 600 siempre.
        public static readonly int[] AnchoRoman = {
            250, 333, 408, 500, 500, 833, 778, 180, 333, 333, 500, 564, 250, 333, 250, 278,
            500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 278, 278, 564, 564, 564, 444,
            921, 722, 667, 667, 722, 611, 556, 722, 722, 333, 389, 722, 611, 889, 722, 722,
            556, 722, 667, 556, 611, 722, 722, 944, 722, 722, 611, 333, 278, 333, 469, 500,
            333, 444, 500, 444, 500, 444, 333, 500, 500, 278, 278, 500, 278, 778, 500, 500,
            500, 500, 333, 389, 278, 500, 500, 722, 500, 500, 444, 480, 200, 480, 541 };

        public static readonly int[] AnchoNegrita = {
            250, 333, 555, 500, 500, 1000, 833, 278, 333, 333, 500, 570, 250, 333, 250, 278,
            500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 333, 333, 570, 570, 570, 500,
            930, 722, 667, 722, 722, 667, 611, 778, 778, 389, 500, 778, 667, 944, 722, 778,
            611, 778, 722, 556, 667, 722, 722, 1000, 722, 722, 667, 333, 278, 333, 581, 500,
            333, 500, 556, 444, 556, 444, 333, 500, 556, 278, 333, 556, 278, 833, 556, 500,
            556, 556, 444, 389, 333, 556, 500, 722, 500, 500, 444, 394, 220, 394, 520 };

        public static readonly int[] AnchoCursiva = {
            250, 333, 420, 500, 500, 833, 778, 214, 333, 333, 500, 675, 250, 333, 250, 278,
            500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 333, 333, 675, 675, 675, 500,
            920, 611, 611, 667, 722, 611, 611, 722, 722, 333, 444, 667, 556, 833, 667, 722,
            611, 722, 611, 500, 556, 722, 611, 833, 611, 556, 556, 389, 278, 389, 422, 500,
            333, 500, 500, 444, 500, 444, 278, 500, 500, 278, 278, 444, 278, 722, 500, 500,
            500, 500, 389, 389, 278, 500, 444, 667, 444, 444, 389, 400, 275, 400, 541 };

        /// <summary>El ancho de un caracter, en milesimas de «em».</summary>
        private static int AnchoDe(char c, FuentePdf f) {
            if (f == FuentePdf.Mono) return 600;
            var tabla = f == FuentePdf.Negrita ? AnchoNegrita : f == FuentePdf.Cursiva ? AnchoCursiva : AnchoRoman;
            if (c >= 32 && c <= 126) return tabla[c - 32];
            var i = ConTilde.IndexOf(c);
            if (i >= 0) return tabla[SinTilde[i] - 32];
            switch (c) {
                case '¿': return tabla['?' - 32];
                case '¡': return tabla['!' - 32];
                case '«': case '»': return 500;
                case 'º': case 'ª': return 300;
                case '·': return 250;
                case '×': return 564;
                case '—': return 1000;
                case '–': return 500;
                case '…': return 1000;
                case '•': return 350;
                case '“': case '”': case '‘': case '’': return 400;
                default: return 500;
            }
        }

        public static double Ancho(string texto, FuentePdf fuente, double tamano) {
            if (string.IsNullOrEmpty(texto)) return 0;
            long suma = 0;
            foreach (var ch in texto) suma += AnchoDe(ch, fuente);
            return suma * tamano / 1000.0;
        }
    }
}
