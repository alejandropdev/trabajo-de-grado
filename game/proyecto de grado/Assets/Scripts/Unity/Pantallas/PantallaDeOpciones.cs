using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Unity.Tema;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las opciones (SRS CU-05). Se abre desde el menu de partidas, la pausa y el recorrido 3D. Lo continuo (volumen,
    /// musica, sensibilidad, velocidad) va en deslizadores y se aplica mientras se arrastra; se guarda al cerrar. Lo
    /// que es «una de tres» o «sí/no» va en botones que dicen su estado. En total, siete botones como mucho.
    /// </summary>
    public sealed class PantallaDeOpciones : PantallaModal {
        private readonly List<KeyValuePair<Button, Func<Preferencias, bool>>> _opciones =
            new List<KeyValuePair<Button, Func<Preferencias, bool>>>();
        private Preferencias _borrador;
        private bool _cambio;

        protected override float Ancho { get { return 680; } }

        protected override void Rellenar() {
            _borrador = App.Preferencias.Acotadas();
            Hoja.Titulo("Opciones");

            Hoja.Subtitulo("Dificultad de los retos", Tema.ink);
            Hoja.Nota("Cuánto reloj dan los minijuegos. Fácil da la mitad más; difícil, un tercio menos.");
            var fila = Hoja.Fila();
            Opcion(fila, "Fácil", p => p.dificultad = Preferencias.Facil, p => p.dificultad == Preferencias.Facil);
            Opcion(fila, "Normal", p => p.dificultad = Preferencias.Normal, p => p.dificultad == Preferencias.Normal);
            Opcion(fila, "Difícil", p => p.dificultad = Preferencias.Dificil, p => p.dificultad == Preferencias.Dificil);

            Hoja.Espacio();
            Hoja.Subtitulo("Sonido", Tema.ink);
            Ui.Deslizador(Hoja.Raiz, "Volumen general", (float)_borrador.volumenGeneral, 0, 1, 0.05f,
                          v => Ajustar(p => p.volumenGeneral = v), Porcentaje);
            Ui.Deslizador(Hoja.Raiz, "Música", (float)_borrador.volumenMusica, 0, 1, 0.05f,
                          v => Ajustar(p => p.volumenMusica = v), Porcentaje);

            Hoja.Espacio();
            Hoja.Subtitulo("Recorrido 3D", Tema.ink);
            Ui.Deslizador(Hoja.Raiz, "Sensibilidad del ratón", (float)_borrador.sensibilidadRaton,
                          (float)Preferencias.SensibilidadMinima, (float)Preferencias.SensibilidadMaxima, 0.1f,
                          v => Ajustar(p => p.sensibilidadRaton = v), v => "×" + v.ToString("0.0"));
            Alternar(Hoja.Fila(), p => p.invertirY ? "Eje vertical invertido: sí" : "Eje vertical invertido: no",
                     p => p.invertirY = !p.invertirY);

            Hoja.Espacio();
            Hoja.Subtitulo("Partida", Tema.ink);
            Ui.Deslizador(Hoja.Raiz, "Velocidad del día (el docente la ajusta a la clase)", (float)_borrador.velocidadDeSimulacion,
                          (float)Preferencias.VelocidadMinima, (float)Preferencias.VelocidadMaxima, 0.5f,
                          v => Ajustar(p => p.velocidadDeSimulacion = v), v => "×" + v.ToString("0.#"));
            fila = Hoja.Fila();
            Alternar(fila, p => p.pantallaCompleta ? "Pantalla completa" : "En ventana", p => p.pantallaCompleta = !p.pantallaCompleta);
            Alternar(fila, p => p.modoTarjetas ? "Textos cortos" : "Textos completos", p => p.modoTarjetas = !p.modoTarjetas);

            Hoja.Espacio();
            Ui.Boton(Hoja.Fila(), "Cerrar", () => App.Router.Volver(), VarianteBoton.Primario);
            Refrescar();
        }

        private static string Porcentaje(float v) { return Math.Round(v * 100) + " %"; }

        /// <summary>Un deslizador: se aplica ya (se oye y se nota), y se guarda al cerrar.</summary>
        private void Ajustar(Action<Preferencias> cambio) {
            cambio(_borrador);
            _cambio = true;
            App.PrevisualizarPreferencias(_borrador);
        }

        /// <summary>Uno de varios: se guarda en el momento.</summary>
        private void Opcion(UnityEngine.Transform fila, string texto, Action<Preferencias> aplicar, Func<Preferencias, bool> elegida) {
            var boton = Ui.Boton(fila, texto, () => {
                aplicar(_borrador);
                Guardar();
                Refrescar();
            }, VarianteBoton.Fantasma);
            _opciones.Add(new KeyValuePair<Button, Func<Preferencias, bool>>(boton, elegida));
        }

        /// <summary>Un sí/no en un solo boton que dice como esta; pulsarlo lo cambia.</summary>
        private void Alternar(UnityEngine.Transform fila, Func<Preferencias, string> rotulo, Action<Preferencias> cambiar) {
            Button boton = null;
            boton = Ui.Boton(fila, rotulo(_borrador), () => {
                cambiar(_borrador);
                Guardar();
                boton.GetComponentInChildren<TMPro.TMP_Text>().text = rotulo(_borrador);
            }, VarianteBoton.Secundario);
        }

        private void Guardar() {
            App.CambiarPreferencias(_borrador);
            _borrador = App.Preferencias.Acotadas();
            _cambio = false;
        }

        private void Refrescar() {
            foreach (var o in _opciones) Ui.Resaltar(o.Key, o.Value(_borrador));
        }

        private void OnDestroy() {
            if (_cambio && App != null) App.CambiarPreferencias(_borrador);
        }
    }
}
