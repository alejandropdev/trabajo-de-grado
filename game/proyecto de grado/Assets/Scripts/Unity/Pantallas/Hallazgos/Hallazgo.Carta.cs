using System.Linq;
using Nexus.Core.Coleccion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    public static partial class Hallazgos {
        /// <summary>
        /// Una carta de desarrollador, como carta de coleccion: llega boca abajo, gira, y su cara se arma por partes
        /// con jerarquia (como el cierre del dia): la banda del palo con su glifo, numero y rareza; el titulo; el emblema
        /// propio de la carta, que se dibuja a mano; el texto; «Para pensar»; la fuente en letra pequeña.
        /// Las dificiles llevan un brillo de carta rara que la cruza de vez en cuando.
        /// </summary>
        public static void Carta(ContextoDeHallazgo x) {
            var col = x.Col;
            var tema = x.Tema;
            var color = ColorDePalo(tema, col.Palo);
            var cartas = x.Catalogo.Where(c => c.Serie == SeriesDeColeccionables.Carta).OrderBy(c => c.Id).ToList();
            var numero = cartas.FindIndex(c => c.Id == col.Id) + 1;
            var dificultad = col.Aparicion == null ? DificultadesDeColeccionable.Facil : col.Aparicion.Dificultad;
            var estrellas = dificultad == DificultadesDeColeccionable.Dificil ? 3 : dificultad == DificultadesDeColeccionable.Media ? 2 : 1;

            var carta = x.Nodo(x.Escenario, "Carta", 0, 0, 500, 700);
            // ---- el dorso
            var dorso = x.Caja(carta, "Dorso", 0, 0, 500, 700, Color.Lerp(tema.bg900, color, 0.18f), 22);
            x.Ui.Borde((RectTransform)dorso.transform, NexusTheme.Alfa(color, 0.8f), 22, 3);
            x.Caja(dorso.transform, "Marco", 0, 0, 440, 640, NexusTheme.Alfa(color, 0.08f), 16);
            DibujoUI d0;
            var glifoDorso = x.Lamina(dorso.transform, 0, 20, 60, 60, out d0);
            DibujosDeHallazgos.GlifoDePalo(glifoDorso, col.Palo, color);
            d0.transform.parent.localScale = Vector3.one * 2.6f;
            var marca = x.Texto(dorso.transform, "NEXUS", EstiloTexto.Leyenda, NexusTheme.Alfa(color, 0.9f), 0, -150, 300, 30);
            marca.characterSpacing = 18;

            // ---- la cara
            var frente = x.Caja(carta, "Frente", 0, 0, 500, 700, tema.surface, 22);
            x.Ui.Borde((RectTransform)frente.transform, color, 22, 2.5f);
            frente.gameObject.AddComponent<RectMask2D>();
            var cara = frente.transform;

            var cabecera = x.Caja(cara, "Banda", 0, 305, 500, 90, NexusTheme.Alfa(color, 0.2f));
            x.Caja(cabecera.transform, "Raya", 0, -44, 500, 3, color);
            DibujoUI dPalo;
            var glifo = x.Lamina(cabecera.transform, -200, 0, 60, 60, out dPalo);
            DibujosDeHallazgos.GlifoDePalo(glifo, col.Palo, color);
            x.Texto(cabecera.transform, NombreDePalo(col.Palo), EstiloTexto.Leyenda, color, -40, 14, 240, 26, TextAlignmentOptions.Left).characterSpacing = 4;
            x.Texto(cabecera.transform, $"Nº {numero:00}/{cartas.Count:00}", EstiloTexto.Pequeno, tema.inkMuted, -40, -14, 240, 24, TextAlignmentOptions.Left);
            DibujoUI dEstrellas;
            var rareza = x.Lamina(cabecera.transform, 180, 0, 110, 40, out dEstrellas);
            for (var i = 0; i < 3; i++) rareza.Estrella(22 + i * 34, 20, 13, i < estrellas ? Tizador.Mostaza : Tizador.Gris);
            var gCabecera = x.Grupo(cabecera);

            var titulo = x.Texto(cara, col.Titulo, EstiloTexto.Titulo, tema.ink, 0, 222, 450, 76);
            titulo.enableAutoSizing = true;
            titulo.fontSizeMin = 22;
            titulo.fontSizeMax = titulo.fontSize;
            var gTitulo = x.Grupo(titulo);

            var marcoEmblema = x.Circulo(cara, "Marco del emblema", 0, 58, 268, tema.bg900);
            x.Ui.Borde((RectTransform)marcoEmblema.transform, NexusTheme.Alfa(color, 0.6f), 134, 2);
            var halo = x.Circulo(cara, "Halo", 0, 58, 320, NexusTheme.Alfa(color, 0.0f));
            halo.sprite = PlanoIlustrado.Brillo();
            DibujoUI dEmblema;
            var emblema = x.Lamina(cara, 0, 58, 240, 220, out dEmblema);
            string clave;
            System.Action<Tizador, Color> dibujar;
            if (EmblemasDeColeccionables.DeCartas.TryGetValue(col.Id, out clave) && DibujosDeHallazgos.Emblemas.TryGetValue(clave, out dibujar))
                dibujar(emblema, color);
            else
                DibujosDeHallazgos.GlifoDePalo(emblema.En(60, 50, 2f), col.Palo, color);

            var texto = x.Texto(cara, col.Texto, EstiloTexto.Pequeno, tema.ink, 0, -126, 440, 118);
            texto.enableAutoSizing = true;
            texto.fontSizeMin = 13;
            texto.fontSizeMax = texto.fontSize;
            var gTexto = x.Grupo(texto);

            var pensar = x.Caja(cara, "Para pensar", 0, -240, 450, 96, NexusTheme.Alfa(color, 0.12f), 12);
            x.Ui.Borde((RectTransform)pensar.transform, NexusTheme.Alfa(color, 0.5f), 12, 1);
            x.Texto(pensar.transform, "PARA PENSAR", EstiloTexto.Leyenda, color, 0, 32, 400, 22).characterSpacing = 4;
            var pregunta = x.Texto(pensar.transform, col.PreguntaDeAplicacion ?? "", EstiloTexto.Pequeno, tema.ink, 0, -10, 420, 62);
            pregunta.fontStyle = FontStyles.Italic;
            pregunta.enableAutoSizing = true;
            pregunta.fontSizeMin = 12;
            pregunta.fontSizeMax = pregunta.fontSize;
            var gPensar = x.Grupo(pensar);

            var fuente = x.Texto(cara, col.Fuente ?? "", EstiloTexto.Pequeno, tema.inkFaint, 0, -318, 450, 34);
            fuente.fontSize *= 0.8f;
            fuente.overflowMode = TextOverflowModes.Ellipsis;
            var gFuente = x.Grupo(fuente);

            // ---- el brillo de carta rara
            Image brillo = null;
            if (estrellas == 3) {
                brillo = x.Caja(cara, "Brillo", -700, 0, 120, 1100, new Color(1, 1, 1, 0.1f));
                brillo.transform.localEulerAngles = new Vector3(0, 0, -22);
            }

            // ---- el ritmo
            var s = x.Sec;
            s.En(0f, 0.5f, p => {
                carta.anchoredPosition = new Vector2(0, Mathf.Lerp(-820, 0, ContextoDeHallazgo.Suave(p)));
                carta.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-8, 0, ContextoDeHallazgo.Suave(p)));
            });
            s.En(0.55f, 0.4f, p => {
                carta.localScale = new Vector3(Mathf.Max(0.02f, Mathf.Abs(Mathf.Cos(p * Mathf.PI))), 1, 1);
                dorso.gameObject.SetActive(p < 0.5f);
                frente.gameObject.SetActive(p >= 0.5f);
            });
            s.En(0.9f, 0.3f, p => gCabecera.alpha = p);
            s.En(1.0f, 0.35f, p => { gTitulo.alpha = p; titulo.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, ContextoDeHallazgo.Suave(p)); });
            s.En(1.2f, 1.5f, p => {
                dEmblema.Avance = p;
                halo.color = NexusTheme.Alfa(color, 0.25f * Mathf.Sin(p * Mathf.PI) + (p >= 1 ? 0.1f : 0));
            });
            s.En(2.3f, 0.35f, p => gTexto.alpha = p);
            s.En(2.6f, 0.35f, p => { gPensar.alpha = p; pensar.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1, ContextoDeHallazgo.Rebote(p)); });
            s.En(2.9f, 0.3f, p => gFuente.alpha = p);
            s.Siempre(t => {
                if (t < 3.2f) return;
                // Quieta, la carta flota un poco; la rara deja pasar su brillo cada tres segundos.
                carta.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 0.9f) * 1.2f);
                if (brillo != null) brillo.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-700, 700, Mathf.Repeat(t - 3.2f, 3f) / 1.2f), 0);
            });
        }

        public static Color ColorDePalo(NexusTheme tema, string palo) {
            switch (palo) {
                case PalosDeCarta.Leyes: return tema.cyan;
                case PalosDeCarta.Desastres: return tema.danger;
                case PalosDeCarta.Practicas: return tema.success;
                default: return tema.violet;
            }
        }

        public static string NombreDePalo(string palo) {
            switch (palo) {
                case PalosDeCarta.Leyes: return "LEYES";
                case PalosDeCarta.Desastres: return "DESASTRES";
                case PalosDeCarta.Practicas: return "PRÁCTICAS";
                default: return "OFICIO";
            }
        }
    }
}
