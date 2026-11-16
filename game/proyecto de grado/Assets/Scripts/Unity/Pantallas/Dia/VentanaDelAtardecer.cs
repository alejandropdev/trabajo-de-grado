using System.Collections.Generic;
using Nexus.Core.Jornada;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La ventana de las cinco, arriba del cierre de la jornada. Rediseñada desde cero el 2026-11-15 con la direccion de
    /// arte (docs/08-arte): POR DENTRO el bunker bioluminiscente (muro verdeazulado, el arco de costilla con su filo de luz,
    /// la guirnalda de bombillas, la taza esmaltada, el reloj de bisel cromado y el abrigo colgado); POR FUERA el futuro
    /// que Nexus prometio en 1957 (atardecer, torres en capsula, la torre Nexus con su platillo y su atomo de neon, el
    /// monorriel y las ventanas de las oficinas que se encienden por grupos: la ciudad se va a casa).
    ///
    /// La decision la cambia (Modo, al mirar una carta):
    ///   Casa      el sol se hunde, cae la noche (luna, estrellas), la guirnalda se apaga y el abrigo se mece
    ///   Quedarse  sigue la tarde, la guirnalda se enciende del todo, la taza humea y el reloj marca el arco hasta la hora limite
    /// Los cambios entran en ~220 ms con ease-out y vuelven algo mas rapido. Lienzo de 760×250 que se escala para caber.
    /// Capa, Grupo, Imagen, Colocado y Hex los usan tambien EscenaDelDespliegue y EdificioDeLaEntrevista.
    /// </summary>
    public sealed class VentanaDelAtardecer : MonoBehaviour {
        public enum Modo { Neutro, Casa, Quedarse }

        private const float Ancho = 760, Alto = 250;
        // La ventana aerodinamica (lados rectos y arco arriba), en coordenadas del lienzo.
        private const float VX0 = 36, VX1 = 556, VTecho = 14, VArranque = 72, VSuelo = 214;
        private const float VW = VX1 - VX0, VH = VSuelo - VTecho;
        private const float RelojX = 664, RelojY = 78, RelojR = 48;
        private const float SolX = 150;     // en coordenadas del cristal
        private const int TorreX = 352;     // la torre Nexus, en coordenadas del cristal

        // Paleta de la escena (STYLE.md §2): fuera, el atardecer de 1957; dentro, el bunker.
        private static readonly Color Lejos = Hex(0x9A4458), Lejos2 = Hex(0x7A3450), Medio = Hex(0x3C1A34),
            Cerca = Hex(0x1E0E22), Atardecer = Hex(0xF08A3C), Sol = Hex(0xF7D07A), Bombilla = Hex(0xF2C14E);
        private static readonly Color MuroArriba = Hex(0x0A2A33), MuroAbajo = Hex(0x05161C), Costilla = Hex(0x123F4A),
            Costilla2 = Hex(0x0B2A32), Niebla = Hex(0x2A8C98);

        public Modo Actual = Modo.Neutro;

        private RectTransform _lienzo, _sol, _segundero, _abrigo;
        private CanvasGroup _noche, _veloNoche, _arco, _vapor;
        private RawImage _halo;
        private readonly List<RawImage> _bombillas = new List<RawImage>();
        private readonly List<Image> _ventanas = new List<Image>();
        private int[] _orden;
        private float _casa, _quedarse, _t;
        private float _solTarde, _solNoche;
        private Color _luzEncendida, _luzApagada;

        /// <summary>
        /// Crea la ventana dentro de 'padre' (que la maqueta: flexible a lo ancho y a lo alto). minutoDelDia es la hora que
        /// marca el reloj; limite, en minutos del dia, cierra el arco de las horas extra y hunde el sol.
        /// </summary>
        public static VentanaDelAtardecer Crear(UiKit ui, Transform padre, int minutoDelDia, int cierre, int limite) {
            var hueco = ui.Nodo(padre, "Ventana del atardecer");
            var le = UiKit.Tamano(hueco, alto: 150, flexAncho: 1, flexAlto: 1);
            le.preferredHeight = Alto;
            var v = hueco.gameObject.AddComponent<VentanaDelAtardecer>();
            v.Construir(ui, minutoDelDia, cierre, limite);
            return v;
        }

        private void Construir(UiKit ui, int minuto, int cierre, int limite) {
            var tema = ui.Tema;
            _lienzo = ui.Nodo(transform, "Lienzo");
            _lienzo.anchorMin = _lienzo.anchorMax = _lienzo.pivot = new Vector2(0.5f, 0.5f);
            _lienzo.sizeDelta = new Vector2(Ancho, Alto);
            _luzEncendida = NexusTheme.Alfa(Hex(0xFFD98A), 0.95f);
            _luzApagada = NexusTheme.Alfa(Hex(0x24122A), 0.9f);

            Fuera(ui, minuto, cierre, limite);
            Dentro(ui, tema);
            Reloj(ui, tema, minuto, limite);
            Abrigo(ui);
            Ajustar();
        }

        // ==================================================================== fuera: el futuro de 1957

        private void Fuera(UiKit ui, int minuto, int cierre, int limite) {
            var cristal = ui.Nodo(_lienzo, "Cristal");
            Lamina.Colocar(cristal, VX0, VTecho, VW, VH);
            cristal.gameObject.AddComponent<RectMask2D>();

            LuzUI.Cielo(ui, cristal, 0, 0, VW, VH, LuzUI.Parada(0, Hex(0x2A1440)), LuzUI.Parada(0.42f, Hex(0x6A2A50)),
                        LuzUI.Parada(0.66f, Hex(0xC2504A)), LuzUI.Parada(0.82f, Atardecer), LuzUI.Parada(1, Sol));

            // La noche, encima del cielo de tarde, con su luna y sus estrellas: se funde al elegir «Casa».
            var noche = ui.Nodo(cristal, "Noche");
            UiKit.Rellenar(noche);
            LuzUI.Cielo(ui, noche, 0, 0, VW, VH, LuzUI.Parada(0, Hex(0x060818)), LuzUI.Parada(0.6f, Hex(0x121636)), LuzUI.Parada(1, Hex(0x2A2448)));
            LuzUI.Brillo(ui, noche, 96, 40, 60, 60, NexusTheme.Alfa(Hex(0xDCE6FF), 0.35f));
            LuzUI.Forma(ui, noche, LuzUI.Elipse(96, 40, 13, 13, 32), Hex(0xF2F0E2), Hex(0xBFC4D6), new Vector2(84, 28), new Vector2(108, 52), "Luna");
            var estrellas = new[] { P(170, 20), P(236, 46), P(318, 16), P(402, 38), P(470, 12), P(30, 70), P(212, 84), P(360, 72), P(498, 60), P(140, 98) };
            foreach (var e in estrellas) LuzUI.Brillo(ui, noche, e.x, e.y, 4, 4, NexusTheme.Alfa(Color.white, 0.9f));
            _noche = Grupo(noche, 0);

            // El sol: halo amplio aditivo y el disco con su degradado; mas tarde, mas hundido.
            _sol = ui.Nodo(cristal, "Sol");
            _sol.anchorMin = _sol.anchorMax = new Vector2(0, 1);
            _sol.pivot = new Vector2(0.5f, 0.5f);
            _sol.sizeDelta = new Vector2(60, 60);
            _halo = LuzUI.Brillo(ui, _sol, 30, 30, 150, 110, NexusTheme.Alfa(Hex(0xF6A04E), 0.6f));
            LuzUI.Forma(ui, _sol, LuzUI.Elipse(30, 30, 26, 26, 40), Hex(0xFFF0C4), Atardecer, new Vector2(30, 4), new Vector2(30, 56), "Disco");
            _solTarde = Mathf.Lerp(150, 166, EscenaDelCierre.FraccionDeTarde(minuto, cierre, limite));
            _solNoche = 236;
            _sol.anchoredPosition = new Vector2(SolX, -_solTarde);

            // Capa lejana: torres en capsula (remate redondo), aclaradas por la bruma.
            float[,] torres = { { 4, 34, 92 }, { 46, 26, 70 }, { 236, 30, 104 }, { 274, 22, 76 }, { 430, 36, 96 }, { 476, 26, 120 } };
            for (var i = 0; i < torres.GetLength(0); i++) {
                float x = torres[i, 0], w = torres[i, 1], alto = torres[i, 2];
                LuzUI.Forma(ui, cristal, LuzUI.Redondeado(x, VH - alto, w, alto + 20, w / 2, 6), Lejos, Lejos2, new Vector2(0, VH - alto), new Vector2(0, VH), "Torre");
            }

            // La torre Nexus: fuste, platillo y la aguja con su atomo de neon (el futuro prometido).
            const float TX = TorreX;
            LuzUI.Forma(ui, cristal, new RutaDeCurva(TX - 12, VH).L(TX - 9, 64).C(TX - 7, 50, TX + 7, 50, TX + 9, 64).L(TX + 12, VH).Puntos(),
                        Hex(0x6A2C4A), Medio, new Vector2(TX - 12, 0), new Vector2(TX + 12, 0), "Torre Nexus");
            LuzUI.Forma(ui, cristal, LuzUI.Elipse(TX, 66, 44, 7, 40), Hex(0x7A3456), Hex(0x2E1430), new Vector2(0, 59), new Vector2(0, 73), "Platillo");
            LuzUI.Forma(ui, cristal, LuzUI.Elipse(TX, 64, 36, 2.5f, 32), NexusTheme.Alfa(Sol, 0.6f), "Luz del platillo");
            var aguja = Capa(ui, cristal, "Aguja y atomo");
            aguja.Linea(TX, 52, TX, 30, Medio, 2.5f);
            for (var i = 0; i < 3; i++) {
                var orbita = new List<Vector2>();
                for (var j = 0; j <= 32; j++) {
                    var a = j * Mathf.PI * 2 / 32;
                    var p = Quaternion.Euler(0, 0, i * 60) * new Vector3(Mathf.Cos(a) * 13, Mathf.Sin(a) * 4.5f, 0);
                    orbita.Add(P(TX + p.x, 24 + p.y));
                }
                aguja.Polilinea(orbita, NexusTheme.Alfa(Bombilla, 0.95f), 1.6f);
            }
            LuzUI.Brillo(ui, cristal, TX, 24, 30, 30, NexusTheme.Alfa(Bombilla, 0.6f));
            LuzUI.Niebla(ui, cristal, 0, 70, VW, VH - 70, Atardecer, 0f, 0.32f);

            // El monorriel cruzando en curva, con sus pilares y un vagon.
            var via = new RutaDeCurva(-10, 140).C(140, 120, 360, 124, 530, 132).Puntos();
            LuzUI.Forma(ui, cristal, LuzUI.Tubo(via, 6, 6), Medio, "Via");
            for (var i = 1; i < 5; i++) {
                var p = via[i * via.Count / 5];
                LuzUI.Forma(ui, cristal, new List<Vector2> { P(p.x - 2.5f, p.y), P(p.x + 2.5f, p.y), P(p.x + 6, VH), P(p.x - 6, VH) }, Medio, Cerca, new Vector2(0, p.y), new Vector2(0, VH), "Pilar");
            }
            LuzUI.Forma(ui, cristal, LuzUI.Redondeado(180, 112, 62, 10, 5, 4), Hex(0xE8DCD6), Hex(0x8A6A72), new Vector2(0, 112), new Vector2(0, 122), "Vagon");
            LuzUI.Forma(ui, cristal, LuzUI.Redondeado(186, 114, 50, 3, 1.5f, 2), NexusTheme.Alfa(Hex(0x5FD8F5), 0.9f), "Ventanilla");

            // Primer plano de fuera: tejados en boomerang y parabolas, casi negros, con sus oficinas.
            var tejados = new List<List<Vector2>> {
                new RutaDeCurva(0, VH).L(0, 158).Q(40, 146, 96, 160).L(110, 150).L(110, VH).Puntos(),
                new RutaDeCurva(124, VH).L(124, 170).Q(170, 120, 216, 170).L(216, VH).Puntos(),
                new RutaDeCurva(392, VH).L(392, 156).L(470, 142).Q(450, 160, 470, 174).L(520, 168).L(520, VH).Puntos()
            };
            foreach (var c in tejados) LuzUI.Forma(ui, cristal, c, Hex(0x2A1430), Cerca, new Vector2(0, 140), new Vector2(0, VH), "Tejado");

            // De noche la ciudad entera se apaga bajo un velo azul (las ventanas encendidas quedan encima).
            var velo = ui.Nodo(cristal, "Velo de noche");
            UiKit.Rellenar(velo);
            LuzUI.Cielo(ui, velo, 0, 0, VW, VH, LuzUI.Parada(0, NexusTheme.Alfa(Hex(0x0A0E24), 0.2f)), LuzUI.Parada(1, NexusTheme.Alfa(Hex(0x0A0E24), 0.78f)));
            _veloNoche = Grupo(velo, 0);

            // Las oficinas: ventanas en grupos de dos o tres por planta, como una oficina, no confeti.
            var luces = ui.Nodo(cristal, "Luces");
            UiKit.Rellenar(luces);
            int[,] grupos = { { 10, 168, 3 }, { 54, 176, 2 }, { 18, 186, 2 }, { 140, 178, 2 }, { 176, 188, 2 }, { 150, 196, 3 },
                              { 404, 166, 3 }, { 480, 182, 2 }, { 410, 184, 2 }, { 440, 196, 3 },
                              { TorreX - 6, 82, 2 }, { TorreX - 6, 100, 2 }, { TorreX - 6, 118, 2 } };
            for (var g = 0; g < grupos.GetLength(0); g++)
                for (var k = 0; k < grupos[g, 2]; k++)
                    _ventanas.Add(Imagen(Colocado(ui, luces, "Ventana", grupos[g, 0] + k * 8, grupos[g, 1], 5, 4), null, _luzApagada));
            _orden = EscenaDelCierre.OrdenDeEncendido(_ventanas.Count);
        }

        // ==================================================================== dentro: el bunker

        private void Dentro(UiKit ui, NexusTheme tema) {
            // El muro: todo el lienzo menos el hueco aerodinamico de la ventana (un solo poligono, abierto por abajo).
            var muro = new List<Vector2> { P(0, Alto), P(0, 0), P(Ancho, 0), P(Ancho, Alto), P(VX1, Alto), P(VX1, VArranque) };
            muro.AddRange(Semiarco(VX1, VX0, VArranque, VTecho));
            muro.Add(P(VX0, VArranque)); muro.Add(P(VX0, Alto));
            LuzUI.Forma(ui, _lienzo, muro, MuroArriba, MuroAbajo, new Vector2(0, 0), new Vector2(0, Alto), "Muro");
            // La luz del bunker baña el muro alrededor de la ventana.
            LuzUI.Brillo(ui, _lienzo, 296, 120, 360, 170, NexusTheme.Alfa(Niebla, 0.16f));

            // La costilla que enmarca la ventana: banda en arco con un leve bamboleo y su filo de luz por dentro.
            var marco = new List<Vector2> { P(VX1 + 14, VSuelo) };
            marco.AddRange(Semiarco(VX1 + 14, VX0 - 14, VArranque, VTecho - 10));
            marco.Add(P(VX0 - 14, VSuelo));
            marco.Add(P(VX0, VSuelo));
            marco.AddRange(Semiarco(VX0, VX1, VArranque, VTecho));
            marco.Add(P(VX1, VSuelo));
            LuzUI.Forma(ui, _lienzo, marco, Costilla, Costilla2, new Vector2(0, 0), new Vector2(0, VSuelo), "Costilla");
            var filo = Capa(ui, _lienzo, "Filo de luz");
            var borde = new List<Vector2> { P(VX0 + 1, VSuelo) };
            borde.AddRange(Semiarco(VX0 + 1, VX1 - 1, VArranque, VTecho + 1));
            borde.Add(P(VX1 - 1, VSuelo));
            filo.Polilinea(borde, NexusTheme.Alfa(tema.cyan, 0.55f), 2);
            filo.Linea(VX0 + VW / 2, VTecho + 1, VX0 + VW / 2, VSuelo, Costilla2, 6);

            // El alfeizar.
            LuzUI.Forma(ui, _lienzo, LuzUI.Redondeado(VX0 - 22, VSuelo - 2, VW + 44, 16, 4, 4), Hex(0x1A4E58), Costilla2, new Vector2(0, VSuelo - 2), new Vector2(0, VSuelo + 14), "Alfeizar");
            var canto = Capa(ui, _lienzo, "Canto del alfeizar");
            canto.Linea(VX0 - 18, VSuelo - 1, VX1 + 18, VSuelo - 1, NexusTheme.Alfa(Niebla, 0.8f), 1.5f);

            // La guirnalda de bombillas por delante de la ventana: la luz calida de dentro.
            var cable = new RutaDeCurva(0, 6).Q(150, 58, 300, 8).Q(450, 58, 600, 10).Q(680, 34, 760, 6).Puntos();
            canto.Polilinea(cable, Hex(0x03090C), 2.5f);
            for (var i = 3; i < cable.Count - 2; i += 5) {
                var p = cable[i];
                _bombillas.Add(LuzUI.Brillo(ui, _lienzo, p.x, p.y + 6, 26, 26, NexusTheme.Alfa(Bombilla, 0.5f)));
                LuzUI.Forma(ui, _lienzo, LuzUI.Elipse(p.x, p.y + 6, 3, 4, 12), Hex(0xFFF1C4), "Bombilla");
            }

            // La taza esmaltada en el alfeizar, con su vapor.
            const float TazaX = 470, TazaY = VSuelo - 26;
            LuzUI.Forma(ui, _lienzo, LuzUI.Tubo(new RutaDeCurva(TazaX + 26, TazaY + 6).C(TazaX + 38, TazaY + 6, TazaX + 38, TazaY + 20, TazaX + 26, TazaY + 20).Puntos(), 4, 4), Hex(0xCFD8DA), "Asa");
            LuzUI.Forma(ui, _lienzo, new RutaDeCurva(TazaX, TazaY).L(TazaX + 28, TazaY).L(TazaX + 26, TazaY + 24).Q(TazaX + 14, TazaY + 28, TazaX + 2, TazaY + 24).Puntos(),
                        Hex(0xEEF2EE), Hex(0x7E9298), new Vector2(TazaX, 0), new Vector2(TazaX + 28, 0), "Taza");
            LuzUI.Forma(ui, _lienzo, LuzUI.Elipse(TazaX + 14, TazaY + 1, 14, 2.5f, 20), Hex(0x1A4E58), "Borde de la taza");
            var vapor = Colocado(ui, _lienzo, "Vapor", TazaX, TazaY - 34, 28, 30);
            for (var i = 0; i < 3; i++) {
                var voluta = Colocado(ui, vapor, "Voluta", 2 + i * 8, 18, 12, 12);
                LuzUI.Brillo(ui, voluta, 6, 6, 9, 9, NexusTheme.Alfa(tema.ink, 0.55f));
                var sube = voluta.gameObject.AddComponent<SubeEnBucle>();
                sube.Fase = i / 3f; sube.Altura = 22; sube.Periodo = 2.2f;
            }
            _vapor = Grupo(vapor, 0.3f);
        }

        // ==================================================================== el reloj y el abrigo

        private void Reloj(UiKit ui, NexusTheme tema, int minuto, int limite) {
            LuzUI.Forma(ui, _lienzo, LuzUI.Elipse(RelojX, RelojY + 3, RelojR + 6, RelojR + 6, 48), NexusTheme.Alfa(Color.black, 0.45f), "Sombra del reloj");
            LuzUI.Forma(ui, _lienzo, LuzUI.Elipse(RelojX, RelojY, RelojR + 5, RelojR + 5, 48), Hex(0xEEF4F5), Hex(0x3E4C54), new Vector2(0, RelojY - RelojR), new Vector2(0, RelojY + RelojR), "Bisel");
            LuzUI.Forma(ui, _lienzo, LuzUI.Elipse(RelojX, RelojY, RelojR - 2, RelojR - 2, 48), Hex(0x0F2630), Hex(0x05161C), new Vector2(0, RelojY - RelojR), new Vector2(0, RelojY + RelojR), "Esfera");
            var horas = Capa(ui, _lienzo, "Horas");
            for (var h = 0; h < 12; h++) {
                var a = h * 30 * Mathf.Deg2Rad;
                var grande = h % 3 == 0;
                var largo = grande ? 8f : 4f;
                horas.Linea(RelojX + Mathf.Sin(a) * (RelojR - 8), RelojY - Mathf.Cos(a) * (RelojR - 8),
                            RelojX + Mathf.Sin(a) * (RelojR - 8 - largo), RelojY - Mathf.Cos(a) * (RelojR - 8 - largo),
                            grande ? tema.ink : tema.inkFaint, grande ? 2.5f : 1.5f);
            }
            // El arco de las horas extra, de ahora a la hora limite (en la esfera de las horas).
            var arco = Capa(ui, _lienzo, "Arco de horas extra");
            var desde = EscenaDelCierre.AnguloDeHora(minuto);
            var hasta = desde + Mathf.Max(0, limite - minuto) / 720f * 360f;
            if (hasta > desde) {
                var puntos = new List<Vector2>();
                for (var k = 0; k <= 16; k++) {
                    var a = Mathf.Lerp(desde, hasta, k / 16f) * Mathf.Deg2Rad;
                    puntos.Add(P(RelojX + Mathf.Sin(a) * (RelojR + 10), RelojY - Mathf.Cos(a) * (RelojR + 10)));
                }
                arco.Polilinea(puntos, tema.warning, 3.5f);
                var fin = puntos[puntos.Count - 1];
                arco.Circulo(fin.x, fin.y, 3.5f, Color.clear, 0, tema.warning);
            }
            _arco = Grupo(arco.rectTransform, 0);
            // Lo que informa brilla: un halo tenue sobre la esfera y el segundero cian.
            LuzUI.Brillo(ui, _lienzo, RelojX, RelojY, RelojR, RelojR, NexusTheme.Alfa(tema.cyan, 0.14f));
            Aguja(ui, EscenaDelCierre.AnguloDeHora(minuto), 26, 4, tema.ink);
            Aguja(ui, EscenaDelCierre.AnguloDeMinuto(minuto), 36, 2.5f, tema.ink);
            _segundero = Aguja(ui, 0, 38, 1.5f, tema.cyan);
            LuzUI.Forma(ui, _lienzo, LuzUI.Elipse(RelojX, RelojY, 4, 4, 16), Hex(0xEEF4F5), Hex(0x6A7A82), new Vector2(0, RelojY - 4), new Vector2(0, RelojY + 4), "Eje");
        }

        private void Abrigo(UiKit ui) {
            var gancho = Capa(ui, _lienzo, "Gancho");
            gancho.Linea(RelojX, 140, RelojX, 150, Hex(0x8A9AA0), 2.5f);
            _abrigo = ui.Nodo(_lienzo, "Abrigo");
            _abrigo.anchorMin = _abrigo.anchorMax = new Vector2(0, 1);
            _abrigo.pivot = new Vector2(0.5f, 1f);
            _abrigo.sizeDelta = new Vector2(70, 90);
            _abrigo.anchoredPosition = new Vector2(RelojX, -148);
            // Tela con caida: hombros redondeados, bajo acampanado; el cuello, un pliegue y la cinta mostaza del canon.
            var tela = new RutaDeCurva(35, 0).C(48, 4, 58, 14, 60, 30).C(62, 52, 66, 74, 62, 88).Q(36, 92, 8, 88)
                .C(4, 74, 8, 52, 10, 30).C(12, 14, 22, 4, 35, 0).Puntos();
            LuzUI.Forma(ui, _abrigo, tela, Hex(0x3E5158), Hex(0x1C2A30), new Vector2(0, 0), new Vector2(0, 90), "Tela");
            LuzUI.Forma(ui, _abrigo, new RutaDeCurva(24, 6).L(35, 26).L(46, 6).Q(35, 12, 24, 6).Puntos(), Hex(0x2A3A40), "Cuello");
            var pliegues = Capa(ui, _abrigo, "Pliegues");
            pliegues.Polilinea(new RutaDeCurva(35, 26).Q(33, 58, 36, 88).Puntos(), NexusTheme.Alfa(Color.black, 0.35f), 1.5f);
            pliegues.Polilinea(new RutaDeCurva(10, 32).Q(7, 60, 9, 86).Puntos(), NexusTheme.Alfa(Niebla, 0.7f), 1.5f);
            LuzUI.Forma(ui, _abrigo, LuzUI.Redondeado(42, 40, 12, 3, 1.5f, 2), Hex(0xC9A227), "Cinta");
        }

        // ==================================================================== cada fotograma

        private void Update() {
            var dt = Time.unscaledDeltaTime;
            _t += dt;
            Ajustar();

            // Hacia el modo que se mira: entra con ease-out (~220 ms); volver a la tarde va algo mas rapido.
            _casa = Acercar(_casa, Actual == Modo.Casa ? 1 : 0, dt);
            _quedarse = Acercar(_quedarse, Actual == Modo.Quedarse ? 1 : 0, dt);

            _noche.alpha = _casa;
            _veloNoche.alpha = _casa;
            _arco.alpha = _quedarse;
            _vapor.alpha = (0.3f + 0.7f * _quedarse) * (1 - _casa);
            _sol.anchoredPosition = new Vector2(SolX, -(Mathf.Lerp(_solTarde, _solNoche, _casa) - 10 * _quedarse));
            _halo.color = NexusTheme.Alfa(_halo.color, 0.6f * (1 - _casa) + 0.15f * _quedarse);

            // La guirnalda: tenue de tarde, apagada al irse, encendida del todo al quedarse; una bombilla tiembla de vez en cuando.
            var luz = 0.45f * (1 - _casa) + 0.5f * _quedarse;
            for (var i = 0; i < _bombillas.Count; i++) {
                var tiembla = i == 2 && Mathf.Repeat(_t, 5.3f) < 0.18f ? 0.4f : 1f;
                _bombillas[i].color = NexusTheme.Alfa(_bombillas[i].color, luz * tiembla);
            }

            // El abrigo se mece cuando te vas (alguien lo va a coger).
            _abrigo.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_t * 3.2f) * 6f * _casa);

            // El segundero salta cada segundo (un tic, no un barrido).
            _segundero.localRotation = Quaternion.Euler(0, 0, -Mathf.Floor(_t) * 6f);

            // La ciudad se va encendiendo en su orden salteado, en bucle.
            var encendidas = EscenaDelCierre.VentanasEncendidas(_t, _ventanas.Count);
            for (var i = 0; i < _orden.Length; i++)
                _ventanas[_orden[i]].color = i < encendidas ? _luzEncendida : _luzApagada;
        }

        private static float Acercar(float v, float objetivo, float dt) {
            // Exponencial: rapida al principio y aterriza suave. k=14 llega al 95 % en ~215 ms; k=20, en ~150 ms.
            var k = objetivo > v ? 14f : 20f;
            return Mathf.Lerp(v, objetivo, 1 - Mathf.Exp(-k * dt));
        }

        /// <summary>El lienzo se escala para caber en el hueco, sin deformarse (nunca mas de un 15 % de su tamaño).</summary>
        private void Ajustar() {
            var r = ((RectTransform)transform).rect;
            if (r.width < 1 || r.height < 1) return;
            var s = Mathf.Min(r.width / Ancho, r.height / Alto, 1.15f);
            _lienzo.localScale = new Vector3(s, s, 1);
        }

        // ==================================================================== piezas

        /// <summary>El arco de la ventana de un lado al otro (de xDesde a xHasta), con el leve bamboleo de las costillas.</summary>
        private static List<Vector2> Semiarco(float xDesde, float xHasta, float yArranque, float yTecho) {
            var p = new List<Vector2>();
            float cx = (xDesde + xHasta) / 2, rx = Mathf.Abs(xHasta - xDesde) / 2, ry = yArranque - yTecho;
            var haciaLaIzquierda = xDesde > xHasta;
            for (var i = 0; i <= 32; i++) {
                var t = i / 32f;
                var a = Mathf.PI * (haciaLaIzquierda ? t : 1 - t);
                var bamboleo = 1 + 0.012f * Mathf.Sin(t * 9.3f);
                p.Add(P(cx + Mathf.Cos(a) * rx, yArranque - Mathf.Sin(a) * ry * bamboleo));
            }
            return p;
        }

        private RectTransform Aguja(UiKit ui, float angulo, float largo, float grosor, Color color) {
            var rt = ui.Nodo(_lienzo, "Aguja");
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(grosor, largo);
            rt.anchoredPosition = new Vector2(RelojX, -RelojY);
            rt.localRotation = Quaternion.Euler(0, 0, -angulo);
            Imagen(rt, null, color);
            return rt;
        }

        internal static DibujoUI Capa(UiKit ui, Transform padre, string nombre) {
            var rt = ui.Nodo(padre, nombre);
            UiKit.Rellenar(rt);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var d = rt.gameObject.AddComponent<DibujoUI>();
            d.raycastTarget = false;
            return d;
        }

        internal static CanvasGroup Grupo(RectTransform rt, float alfa) {
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = alfa;
            g.blocksRaycasts = false;
            g.interactable = false;
            return g;
        }

        internal static Image Imagen(RectTransform rt, Sprite sprite, Color color) {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        internal static RectTransform Colocado(UiKit ui, Transform padre, string nombre, float x, float y, float w, float h) {
            var rt = ui.Nodo(padre, nombre);
            Lamina.Colocar(rt, x, y, w, h);
            return rt;
        }

        private static Vector2 P(float x, float y) { return new Vector2(x, y); }
        internal static Color Hex(int rgb) { return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1); }
    }
}
