using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Core.Reportes {
    public enum AlineacionPdf { Izquierda, Centro }

    /// <summary>
    /// El maquetador: lleva el cursor vertical, parte el texto en lineas segun el ancho real de las letras,
    /// salta de pagina cuando no cabe y numera arriba a la derecha (APA 7). Pone tablas con solo tres filetes
    /// horizontales y figuras con su leyenda encima, como pide APA. Parrafos con el cuerpo en Times 12 pt.
    /// </summary>
    public sealed class Documento {
        public const double Margen = 72;
        public const double AnchoUtil = PdfWriter.AnchoA4 - 2 * Margen;
        public const double Cuerpo = 12;
        /// <summary>APA pide doble espacio; con tablas y figuras de por medio, 1,5 hace el informe legible sin duplicar las paginas.</summary>
        public const double Interlineado = 1.5;

        private const double Superior = 72;
        private const double LimiteInferior = PdfWriter.AltoA4 - 72;

        private readonly PdfWriter _pdf;
        private double _y;
        private int _tabla, _figura;

        public Documento(PdfWriter pdf) {
            _pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));
            NuevaPagina();
        }

        public PdfWriter Pdf { get { return _pdf; } }
        public double Y { get { return _y; } }
        public int Tablas { get { return _tabla; } }
        public int Figuras { get { return _figura; } }

        public void NuevaPagina() {
            _pdf.NuevaPagina();
            _y = Superior;
            var numero = _pdf.Paginas.ToString();
            _pdf.Texto(PdfWriter.AnchoA4 - Margen - PdfWriter.Ancho(numero, FuentePdf.Normal, Cuerpo), 50, numero, FuentePdf.Normal, Cuerpo);
        }

        public void Espacio(double puntos) { _y += puntos; }

        /// <summary>Si lo que viene no cabe en lo que queda de pagina, pasa a la siguiente.</summary>
        public void AsegurarEspacio(double alto) {
            if (_y + alto > LimiteInferior) NuevaPagina();
        }

        // ================================================================== TEXTO

        public void Parrafo(string texto, FuentePdf fuente = FuentePdf.Normal, double tamano = Cuerpo, double sangriaPrimera = 0,
                            double sangria = 0, double interlineado = Interlineado, ColorPdf? color = null, double despues = 0,
                            AlineacionPdf alineacion = AlineacionPdf.Izquierda) {
            var ancho = AnchoUtil - sangria;
            var lineas = Ajustar(texto, fuente, tamano, ancho, ancho - sangriaPrimera);
            var alto = tamano * interlineado;
            for (var i = 0; i < lineas.Count; i++) {
                AsegurarEspacio(alto);
                var x = Margen + sangria + (i == 0 ? sangriaPrimera : 0);
                if (alineacion == AlineacionPdf.Centro) x = Margen + (AnchoUtil - PdfWriter.Ancho(lineas[i], fuente, tamano)) / 2;
                _pdf.Texto(x, _y + tamano, lineas[i], fuente, tamano, color);
                _y += alto;
            }
            _y += despues;
        }

        /// <summary>Titulo APA: nivel 1 centrado y en negrita; nivel 2 a la izquierda y en negrita.</summary>
        public void Titulo(int nivel, string texto) {
            AsegurarEspacio(Cuerpo * Interlineado * 3);
            _y += nivel == 1 ? 8 : 4;
            Parrafo(texto, FuentePdf.Negrita, Cuerpo, alineacion: nivel == 1 ? AlineacionPdf.Centro : AlineacionPdf.Izquierda);
        }

        public void Vineta(string texto, double tamano = Cuerpo) {
            AsegurarEspacio(tamano * Interlineado);
            _pdf.Texto(Margen + 18, _y + tamano, "•", FuentePdf.Normal, tamano);
            Parrafo(texto, FuentePdf.Normal, tamano, sangria: 36);
        }

        // ================================================================== TABLAS Y FIGURAS

        /// <summary>
        /// Tabla APA: «Tabla n» en negrita, su titulo en cursiva debajo, solo tres filetes (arriba, bajo la cabecera y
        /// al pie) y la cabecera repetida si salta de pagina. 'pesos' reparte el ancho entre las columnas.
        /// </summary>
        public void Tabla(string titulo, string[] cabecera, IList<string[]> filas, double[] pesos, string nota = null) {
            if (cabecera == null || pesos == null || pesos.Length != cabecera.Length)
                throw new ArgumentException("La tabla necesita un peso por columna.");
            _tabla++;
            const double tam = 10, linea = 12, padH = 4, padV = 3;

            var anchos = new double[pesos.Length];
            double suma = 0;
            foreach (var p in pesos) suma += p;
            for (var i = 0; i < pesos.Length; i++) anchos[i] = AnchoUtil * pesos[i] / suma;

            var cab = Envolver(cabecera, anchos, FuentePdf.Negrita, tam, padH);
            var altoCab = AltoDe(cab, linea, padV);
            var primera = filas.Count > 0 ? Envolver(filas[0], anchos, FuentePdf.Normal, tam, padH) : null;

            AsegurarEspacio(2 * Cuerpo * 1.2 + 12 + altoCab + (primera == null ? 0 : AltoDe(primera, linea, padV)));
            _y += 6;
            Parrafo("Tabla " + _tabla, FuentePdf.Negrita, Cuerpo, interlineado: 1.2);
            Parrafo(titulo, FuentePdf.Cursiva, Cuerpo, interlineado: 1.2, despues: 6);

            DibujarCabecera(cab, anchos, altoCab, tam, linea, padH, padV);
            foreach (var fila in filas) {
                var celdas = Envolver(fila, anchos, FuentePdf.Normal, tam, padH);
                var alto = AltoDe(celdas, linea, padV);
                if (_y + alto > LimiteInferior) {
                    _pdf.Linea(Margen, _y, Margen + AnchoUtil, _y, 0.75);
                    NuevaPagina();
                    DibujarCabecera(cab, anchos, altoCab, tam, linea, padH, padV);
                }
                DibujarFila(celdas, anchos, FuentePdf.Normal, tam, linea, padH, padV);
                _y += alto;
            }
            _pdf.Linea(Margen, _y, Margen + AnchoUtil, _y, 0.75);
            _y += 4;
            if (!string.IsNullOrEmpty(nota)) Parrafo("Nota. " + nota, FuentePdf.Normal, 10, interlineado: 1.2);
            _y += 10;
        }

        private void DibujarCabecera(List<string>[] cab, double[] anchos, double alto, double tam, double linea, double padH, double padV) {
            _pdf.Linea(Margen, _y, Margen + AnchoUtil, _y, 0.75);
            DibujarFila(cab, anchos, FuentePdf.Negrita, tam, linea, padH, padV);
            _y += alto;
            _pdf.Linea(Margen, _y, Margen + AnchoUtil, _y, 0.5);
        }

        private void DibujarFila(List<string>[] celdas, double[] anchos, FuentePdf fuente, double tam, double linea, double padH, double padV) {
            var x = Margen;
            for (var c = 0; c < celdas.Length; c++) {
                for (var j = 0; j < celdas[c].Count; j++)
                    _pdf.Texto(x + padH, _y + padV + j * linea + tam, celdas[c][j], fuente, tam);
                x += anchos[c];
            }
        }

        private static List<string>[] Envolver(string[] fila, double[] anchos, FuentePdf fuente, double tam, double padH) {
            var celdas = new List<string>[anchos.Length];
            for (var c = 0; c < anchos.Length; c++) {
                var texto = c < fila.Length ? fila[c] : "";
                var ancho = anchos[c] - 2 * padH;
                celdas[c] = Ajustar(texto, fuente, tam, ancho, ancho);
            }
            return celdas;
        }

        private static double AltoDe(List<string>[] celdas, double linea, double padV) {
            var mayor = 1;
            foreach (var c in celdas) if (c.Count > mayor) mayor = c.Count;
            return mayor * linea + 2 * padV;
        }

        /// <summary>Figura APA de barras horizontales: «Figura n», su titulo en cursiva y una barra por dato (de 0 a 'maximo').</summary>
        public void FiguraDeBarras(string titulo, IList<KeyValuePair<string, double>> barras, double maximo, string nota = null) {
            _figura++;
            const double fila = 20, etiqueta = 170, valor = 44;
            AsegurarEspacio(2 * Cuerpo * 1.2 + 12 + Math.Min(barras.Count, 3) * fila);
            _y += 6;
            Parrafo("Figura " + _figura, FuentePdf.Negrita, Cuerpo, interlineado: 1.2);
            Parrafo(titulo, FuentePdf.Cursiva, Cuerpo, interlineado: 1.2, despues: 6);

            var x0 = Margen + etiqueta + 8;
            var ancho = AnchoUtil - etiqueta - 8 - valor;
            foreach (var b in barras) {
                AsegurarEspacio(fila);
                var texto = Recortar(b.Key, FuentePdf.Normal, 10, etiqueta);
                _pdf.Texto(Margen, _y + 13, texto, FuentePdf.Normal, 10);
                var fraccion = maximo <= 0 ? 0 : Math.Max(0, Math.Min(1, b.Value / maximo));
                _pdf.Rectangulo(x0, _y + 4, ancho, 12, null, ColorPdf.Gris, 0.5);
                if (fraccion > 0) _pdf.Rectangulo(x0, _y + 4, ancho * fraccion, 12, ColorPdf.GrisOscuro, null);
                _pdf.Texto(x0 + ancho + 6, _y + 13, b.Value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture), FuentePdf.Normal, 10);
                _y += fila;
            }
            _y += 4;
            if (!string.IsNullOrEmpty(nota)) Parrafo("Nota. " + nota, FuentePdf.Normal, 10, interlineado: 1.2);
            _y += 10;
        }

        // ================================================================== AJUSTE DE LINEAS

        private static string Recortar(string texto, FuentePdf fuente, double tam, double ancho) {
            if (PdfWriter.Ancho(texto, fuente, tam) <= ancho) return texto;
            var sb = new StringBuilder();
            foreach (var ch in texto) {
                if (PdfWriter.Ancho(sb.ToString() + ch + "…", fuente, tam) > ancho) break;
                sb.Append(ch);
            }
            return sb.Append('…').ToString();
        }

        /// <summary>Parte el texto en lineas que caben en 'ancho' (la primera, en 'anchoPrimera'). Respeta los saltos de linea.</summary>
        public static List<string> Ajustar(string texto, FuentePdf fuente, double tam, double ancho, double anchoPrimera) {
            var lineas = new List<string>();
            var limite = anchoPrimera;
            void Emitir(string s) { lineas.Add(s); limite = ancho; }

            foreach (var parrafo in (texto ?? "").Replace("\r", "").Split('\n')) {
                var actual = "";
                foreach (var cruda in parrafo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                    var palabra = cruda;
                    while (PdfWriter.Ancho(palabra, fuente, tam) > limite && palabra.Length > 1) {
                        // Una palabra mas ancha que la linea (una ruta, un identificador): se corta donde llegue.
                        if (actual.Length > 0) { Emitir(actual); actual = ""; }
                        var k = 1;
                        while (k < palabra.Length - 1 && PdfWriter.Ancho(palabra.Substring(0, k + 1), fuente, tam) <= limite) k++;
                        Emitir(palabra.Substring(0, k));
                        palabra = palabra.Substring(k);
                    }
                    var prueba = actual.Length == 0 ? palabra : actual + " " + palabra;
                    if (PdfWriter.Ancho(prueba, fuente, tam) <= limite) actual = prueba;
                    else if (actual.Length == 0) actual = palabra;
                    else { Emitir(actual); actual = palabra; }
                }
                Emitir(actual);
            }
            return lineas;
        }
    }
}
