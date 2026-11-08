using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las opciones (SRS CU-05). Se abre desde el menu de partidas y la pausa. Lo continuo (volumen, musica, sensibilidad,
    /// velocidad) va en deslizadores y se aplica mientras se arrastra; se guarda al cerrar. Lo que es «una de tres»
    /// (la dificultad) va en baldosas y lo que es «si o no» en interruptores; esos se guardan en el momento.
    ///
    /// Rediseñada el 2026-11-07 con el lenguaje de la pausa: pantalla completa sobre el mismo velo oscuro (sin panel de
    /// metal ni scroll largo) y cuatro tarjetas de cristal con las esquinas cian, en dos columnas, cada una con su
    /// pictograma dibujado: Dificultad, Sonido, Recorrido 3D y Partida. Los botones a la vista son siete como mucho (aqui,
    /// cuatro: los tres interruptores y «Cerrar»; las baldosas y los deslizadores no son botones).
    /// </summary>
    public sealed class PantallaDeOpciones : Pantalla {
        private sealed class Baldosa {
            public Image Fondo, Borde;
            public DibujoUI Barras;
            public TMP_Text Rotulo, Detalle;
            public int Nivel;
            public Func<Preferencias, bool> Elegida;
        }

        private const float AlfaDelVelo = 0.95f;

        private readonly List<Baldosa> _baldosas = new List<Baldosa>();
        private Preferencias _borrador;
        private bool _cambio;

        public override bool EsModal { get { return true; } }

        protected override void Construir() {
            _borrador = App.Preferencias.Acotadas();

            // El mismo velo de la pausa: oscurece lo de debajo (que no se puede pulsar) lo bastante para leer.
            var velo = Raiz.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(Tema.bg950, AlfaDelVelo);

            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 1.2f));

            // ---- cabecera
            var cabecera = Ui.Fila(marco, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var titulos = Ui.Columna(cabecera, "Titulos", Tema.Espacio(1));
            Ui.Texto(titulos, "AJUSTES", EstiloTexto.Leyenda, Tema.cyan);
            Ui.Texto(titulos, "Opciones", EstiloTexto.Titulo);
            Ui.Resorte(cabecera);
            Ui.Texto(cabecera, "Esc para cerrar", EstiloTexto.Leyenda, Tema.inkFaint, TextAlignmentOptions.MidlineRight);

            // ---- las cuatro tarjetas, en dos columnas. Con scroll solo por si la pantalla fuera muy baja.
            RectTransform contenido;
            var scroll = Ui.Desplazable(marco, out contenido, "Secciones");
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var columnas = Ui.Fila(contenido, "Columnas", Tema.margen, 0, TextAnchor.UpperCenter);
            var izquierda = Ui.Columna(columnas, "Izquierda", Tema.margen);
            UiKit.Tamano(izquierda, flexAncho: 1);
            var derecha = Ui.Columna(columnas, "Derecha", Tema.margen);
            UiKit.Tamano(derecha, flexAncho: 1);

            // Dificultad: tres baldosas con su efecto escrito dentro (ya no hace falta la nota).
            var dificultad = Seccion(izquierda, "Dificultad de los retos", PictogramasDeOpciones.Tipo.Dificultad, 0f);
            var baldosas = Ui.Fila(dificultad, "Baldosas", Tema.espacio, 0, TextAnchor.MiddleCenter);
            CrearBaldosa(baldosas, "Fácil", "+50 % de reloj", 1, p => p.dificultad = Preferencias.Facil, p => p.dificultad == Preferencias.Facil);
            CrearBaldosa(baldosas, "Normal", "reloj base", 2, p => p.dificultad = Preferencias.Normal, p => p.dificultad == Preferencias.Normal);
            CrearBaldosa(baldosas, "Difícil", "-30 % de reloj", 3, p => p.dificultad = Preferencias.Dificil, p => p.dificultad == Preferencias.Dificil);

            var sonido = Seccion(izquierda, "Sonido", PictogramasDeOpciones.Tipo.Sonido, 0.14f);
            Ui.Deslizador(sonido, "Volumen general", (float)_borrador.volumenGeneral, 0, 1, 0.05f,
                          v => Ajustar(p => p.volumenGeneral = v), Porcentaje);
            Ui.Deslizador(sonido, "Música", (float)_borrador.volumenMusica, 0, 1, 0.05f,
                          v => Ajustar(p => p.volumenMusica = v), Porcentaje);

            var recorrido = Seccion(derecha, "Recorrido 3D", PictogramasDeOpciones.Tipo.Recorrido3D, 0.07f);
            Ui.Deslizador(recorrido, "Sensibilidad del ratón", (float)_borrador.sensibilidadRaton,
                          (float)Preferencias.SensibilidadMinima, (float)Preferencias.SensibilidadMaxima, 0.1f,
                          v => Ajustar(p => p.sensibilidadRaton = v), v => "×" + v.ToString("0.0"));
            Interruptor(recorrido, "Eje vertical invertido", null, p => p.invertirY, p => p.invertirY = !p.invertirY);

            var partida = Seccion(derecha, "Partida", PictogramasDeOpciones.Tipo.Partida, 0.21f);
            Ui.Deslizador(partida, "Velocidad del día", (float)_borrador.velocidadDeSimulacion,
                          (float)Preferencias.VelocidadMinima, (float)Preferencias.VelocidadMaxima, 0.5f,
                          v => Ajustar(p => p.velocidadDeSimulacion = v), v => "×" + v.ToString("0.#"));
            Ui.Texto(partida, "El docente la ajusta a la clase.", EstiloTexto.Leyenda, Tema.inkFaint);
            Interruptor(partida, "Pantalla completa", null, p => p.pantallaCompleta, p => p.pantallaCompleta = !p.pantallaCompleta);
            Interruptor(partida, "Textos cortos", "Frases breves en las tarjetas, en vez de párrafos", p => p.modoTarjetas,
                        p => p.modoTarjetas = !p.modoTarjetas);

            // ---- pie: «Cerrar», grande, abajo en el centro (como «Seguir jugando» en la pausa)
            var pie = Ui.Fila(marco, "Pie", 0, 0, TextAnchor.MiddleCenter);
            var cerrar = Ui.Boton(pie, "Cerrar", () => App.Router.Volver(), VarianteBoton.Primario);
            UiKit.Tamano(cerrar, ancho: 420, alto: 72);

            Refrescar();
        }

        private static string Porcentaje(float v) { return Math.Round(v * 100) + " %"; }

        // ==================================================================== piezas

        /// <summary>Una tarjeta de cristal con las esquinas cian y su titulo con pictograma. Entra con un fundido escalonado.</summary>
        private RectTransform Seccion(RectTransform columna, string titulo, PictogramasDeOpciones.Tipo tipo, float retraso) {
            var t = Ui.PanelColumna(columna, titulo, Tema.Espacio(5), Tema.Espacio(3), Tema.surface);
            Ui.Borde(t, Tema.line, NexusTheme.RadioLg, 1f);
            Ui.Esquinas(t);   // la firma visual: las cuatro esquinas en L

            var cab = Ui.Fila(t, "Cabecera", Tema.Espacio(3), 0, TextAnchor.MiddleLeft);
            var marcoIcono = Ui.Nodo(cab, "Icono");
            UiKit.Tamano(marcoIcono, ancho: 46, alto: 46);
            var rd = Ui.Nodo(marcoIcono, "Dibujo");
            rd.anchorMin = rd.anchorMax = rd.pivot = new Vector2(0.5f, 0.5f);
            rd.sizeDelta = new Vector2(PictogramasDeOpciones.Lado, PictogramasDeOpciones.Lado);
            rd.localScale = Vector3.one * 0.72f;   // 64 dibujados, 46 ocupados
            var dibujo = rd.gameObject.AddComponent<DibujoUI>();
            dibujo.raycastTarget = false;
            PictogramasDeOpciones.Dibujar(dibujo, tipo, Tema.cyan);
            Ui.Texto(cab, titulo, EstiloTexto.Subtitulo);

            AparicionDeTarjeta.Poner(t, retraso);
            return t;
        }

        /// <summary>Una de tres opciones: una baldosa con barras 1/2/3 y su efecto escrito. Se guarda en el momento.</summary>
        private void CrearBaldosa(RectTransform fila, string rotulo, string detalle, int nivel, Action<Preferencias> aplicar,
                             Func<Preferencias, bool> elegida) {
            var t = Ui.PanelColumna(fila, "Dificultad " + rotulo, Tema.Espacio(3), Tema.Espacio(1), Tema.surfaceRaised);
            UiKit.Tamano(t, flexAncho: 1, alto: 176);
            var grupo = t.GetComponent<VerticalLayoutGroup>();
            if (grupo != null) grupo.childAlignment = TextAnchor.MiddleCenter;
            var fondo = t.GetComponent<Image>();
            if (fondo == null) fondo = t.gameObject.AddComponent<Image>();
            var borde = Ui.Borde(t, Tema.lineStrong, NexusTheme.RadioLg, 1.5f);

            var marcoBarras = Ui.Nodo(t, "Barras");
            UiKit.Tamano(marcoBarras, ancho: PictogramasDeOpciones.Lado, alto: PictogramasDeOpciones.Lado);
            var barras = marcoBarras.gameObject.AddComponent<DibujoUI>();
            barras.raycastTarget = false;
            var et = Ui.Texto(t, rotulo, EstiloTexto.Etiqueta, Tema.inkMuted, TextAlignmentOptions.Center);
            var sub = Ui.Texto(t, detalle, EstiloTexto.Leyenda, Tema.inkFaint, TextAlignmentOptions.Center);

            var viva = t.gameObject.AddComponent<PiezaViva>();
            viva.Escala = 1.04f;
            viva.AlPulsar = () => {
                aplicar(_borrador);
                Guardar();
                Refrescar();
            };
            _baldosas.Add(new Baldosa { Fondo = fondo, Borde = borde, Barras = barras, Rotulo = et, Detalle = sub, Nivel = nivel, Elegida = elegida });
        }

        /// <summary>Un si/no: toda la fila es un boton, con una perilla que se desliza. Se guarda en el momento.</summary>
        private void Interruptor(RectTransform tarjeta, string rotulo, string detalle, Func<Preferencias, bool> estado,
                                 Action<Preferencias> cambiar) {
            var fila = Ui.Fila(tarjeta, "Interruptor " + rotulo, Tema.Espacio(3), 0, TextAnchor.MiddleLeft);
            // Un fondo invisible que da a la fila entera por zona de pulsacion.
            var zona = fila.gameObject.AddComponent<Image>();
            zona.color = new Color(0f, 0f, 0f, 0f);

            var textos = Ui.Columna(fila, "Textos", 2);
            UiKit.Tamano(textos, flexAncho: 1);
            Ui.Texto(textos, rotulo, EstiloTexto.Encabezado, Tema.ink).raycastTarget = false;
            if (!string.IsNullOrEmpty(detalle)) Ui.Texto(textos, detalle, EstiloTexto.Leyenda, Tema.inkMuted).raycastTarget = false;

            var pista = Ui.Nodo(fila, "Pista");
            UiKit.Tamano(pista, ancho: InterruptorVivo.Ancho, alto: InterruptorVivo.Alto);
            var imgPista = pista.gameObject.AddComponent<Image>();
            Ui.Fondo(imgPista, Tema.line, InterruptorVivo.Alto / 2f / Tema.escala);
            imgPista.raycastTarget = false;

            var perilla = Ui.Nodo(pista, "Perilla");
            perilla.anchorMin = perilla.anchorMax = perilla.pivot = new Vector2(0.5f, 0.5f);
            perilla.sizeDelta = new Vector2(InterruptorVivo.Perilla, InterruptorVivo.Perilla);
            var imgPerilla = perilla.gameObject.AddComponent<Image>();
            imgPerilla.sprite = Ui.SpriteCircular();
            imgPerilla.color = Tema.ink;
            imgPerilla.raycastTarget = false;

            var vivo = pista.gameObject.AddComponent<InterruptorVivo>();
            vivo.Pista = imgPista;
            vivo.PerillaRt = perilla;
            vivo.Apagado = Tema.line;
            vivo.Encendido = Tema.cyan;
            vivo.Poner(estado(_borrador), true);

            var boton = fila.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            boton.targetGraphic = zona;
            boton.onClick.AddListener(() => {
                cambiar(_borrador);
                Guardar();
                vivo.Poner(estado(_borrador), false);
            });
        }

        // ==================================================================== estado

        /// <summary>Un deslizador: se aplica ya (se oye y se nota), y se guarda al cerrar.</summary>
        private void Ajustar(Action<Preferencias> cambio) {
            cambio(_borrador);
            _cambio = true;
            App.PrevisualizarPreferencias(_borrador);
        }

        private void Guardar() {
            App.CambiarPreferencias(_borrador);
            _borrador = App.Preferencias.Acotadas();
            _cambio = false;
        }

        /// <summary>La baldosa elegida se ilumina en cian (borde, fondo y barras); las demas, apagadas.</summary>
        private void Refrescar() {
            foreach (var b in _baldosas) {
                var elegida = b.Elegida(_borrador);
                b.Fondo.color = elegida ? Tema.cyanSoft : Tema.surfaceRaised;
                b.Borde.color = elegida ? Tema.cyan : Tema.lineStrong;
                b.Rotulo.color = elegida ? Tema.ink : Tema.inkMuted;
                b.Detalle.color = elegida ? Tema.inkMuted : Tema.inkFaint;
                PictogramasDeOpciones.Barras(b.Barras, b.Nivel, elegida ? Tema.cyan : Tema.inkMuted, Tema.line);
            }
        }

        private void OnDestroy() {
            if (_cambio && App != null) App.CambiarPreferencias(_borrador);
        }
    }
}
