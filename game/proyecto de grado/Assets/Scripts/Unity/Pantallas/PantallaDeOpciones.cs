using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Unity.Tema;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las opciones del equipo (SRS CU-05): velocidad de la simulacion, volumen y modo de ventana. Se abre desde el
    /// menu de partidas y desde la pausa; cada cambio se aplica y se guarda en el momento.
    /// </summary>
    public sealed class PantallaDeOpciones : PantallaModal {
        private static readonly double[] Velocidades = { 0.5, 1, 2, 4 };
        private static readonly double[] Volumenes = { 0, 0.25, 0.5, 0.75, 1 };

        private readonly List<KeyValuePair<Button, Func<Preferencias, bool>>> _opciones =
            new List<KeyValuePair<Button, Func<Preferencias, bool>>>();

        protected override float Ancho { get { return 680; } }

        protected override void Rellenar() {
            Hoja.Titulo("Opciones");

            Hoja.Subtitulo("Velocidad de la simulación", Tema.ink);
            Hoja.Nota("Cuánto corre el reloj del día. El docente la usa para que el nivel quepa en la sesión de clase.");
            var fila = Hoja.Fila();
            foreach (var v in Velocidades) {
                var valor = v;
                Opcion(fila, "×" + valor.ToString("0.##"), p => p.velocidadDeSimulacion = valor,
                       p => Math.Abs(p.velocidadDeSimulacion - valor) < 0.001);
            }

            Hoja.Espacio();
            Hoja.Subtitulo("Volumen", Tema.ink);
            fila = Hoja.Fila();
            foreach (var v in Volumenes) {
                var valor = v;
                Opcion(fila, (valor * 100).ToString("0") + " %", p => p.volumenGeneral = valor,
                       p => Math.Abs(p.volumenGeneral - valor) < 0.001);
            }

            Hoja.Espacio();
            Hoja.Subtitulo("Pantalla", Tema.ink);
            fila = Hoja.Fila();
            Opcion(fila, "Pantalla completa", p => p.pantallaCompleta = true, p => p.pantallaCompleta);
            Opcion(fila, "Ventana", p => p.pantallaCompleta = false, p => !p.pantallaCompleta);

            Hoja.Espacio();
            Ui.Boton(Hoja.Fila(), "Cerrar", () => App.Router.Volver(), VarianteBoton.Primario);
            Refrescar();
        }

        private void Opcion(UnityEngine.Transform fila, string texto, Action<Preferencias> aplicar, Func<Preferencias, bool> elegida) {
            var boton = Ui.Boton(fila, texto, () => {
                var nuevas = App.Preferencias.Acotadas();
                aplicar(nuevas);
                App.CambiarPreferencias(nuevas);
                Refrescar();
            }, VarianteBoton.Fantasma);
            _opciones.Add(new KeyValuePair<Button, Func<Preferencias, bool>>(boton, elegida));
        }

        private void Refrescar() {
            foreach (var o in _opciones) Ui.Resaltar(o.Key, o.Value(App.Preferencias));
        }
    }
}
