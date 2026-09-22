using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// El escaparate del NexusTheme: todas las piezas de UiKit juntas. Sirve para ver de un vistazo si un
    /// color, una fuente o un sprite nuevo del tema se aplica bien, sin tener que llegar a la pantalla donde
    /// se usa. Va dentro de una lista con scroll, asi que puede crecer sin montarse encima de nada.
    /// </summary>
    public sealed class PantallaDelTema : Pantalla {
        private bool _estabaPausado;

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", relleno: Tema.margen));

            var cabecera = Ui.Fila(marco);
            Ui.Texto(cabecera, "El tema", EstiloTexto.Titulo);
            Ui.Resorte(cabecera);
            Ui.Boton(cabecera, "Volver", App.Router.Volver, VarianteBoton.Primario);

            Ui.Texto(marco, "Todo lo de esta pantalla sale de NexusTheme (Assets/Settings/NexusTheme.asset). Cambia un " +
                            "color, una fuente o arrastra un sprite a un campo del asset, y cambia aquí y en todas las " +
                            "pantallas, sin tocar código.", EstiloTexto.Pequeno);
            Ui.Separador(marco);

            var scroll = Ui.Desplazable(marco, out var lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            var textos = Ui.Tarjeta(lista, "Texto");
            Ui.Texto(textos, "Título", EstiloTexto.Titulo);
            Ui.Texto(textos, "Subtítulo", EstiloTexto.Subtitulo);
            Ui.Texto(textos, "Cuerpo: así se leen los briefings, los guiones y las rúbricas. Las frases largas se " +
                             "parten en varias líneas y la tarjeta crece con ellas.", EstiloTexto.Cuerpo);
            Ui.Texto(textos, "Pequeño: notas, etiquetas y detalles.", EstiloTexto.Pequeno);
            Ui.Texto(textos, "[BUILD] Compiling module facilities.access ... OK", EstiloTexto.Mono);

            var botones = Ui.Tarjeta(lista, "Botones");
            var fila = Ui.Fila(botones);
            Ui.Boton(fila, "Primario", null, VarianteBoton.Primario);
            Ui.Boton(fila, "Secundario", null);
            Ui.Boton(fila, "Peligro", null, VarianteBoton.Peligro);
            Ui.Boton(fila, "Fantasma", null, VarianteBoton.Fantasma);
            Ui.BotonDeOpcion(botones, "Una opción de decisión, con una frase lo bastante larga como para ocupar más de una línea y ver que el botón crece con ella.",
                             "Deuda +8 · Avance +1", null);

            var indicadores = Ui.Tarjeta(lista, "Barras y diales");
            Ui.Texto(indicadores, "Avance", EstiloTexto.Pequeno);
            Ui.Barra(indicadores, 0.72f);
            Ui.Texto(indicadores, "Deuda técnica (peligro: mostaza)", EstiloTexto.Pequeno);
            Ui.Barra(indicadores, 0.35f, Tema.mostaza);
            var diales = Ui.Fila(indicadores, espacio: Tema.margen * 2);
            Ui.Dial(diales, 0.64f, "Avance");
            Ui.Dial(diales, 0.41f, "Cobertura", Tema.cianClaro);
            Ui.Dial(diales, 0.28f, "Deuda", Tema.mostaza);

            var radar = Ui.Tarjeta(lista, "Radar de competencias (Dashboard de Lecciones)");
            var centro = Ui.Fila(radar, alineacion: TextAnchor.MiddleCenter);
            UiKit.Tamano(centro, alto: 440);
            Ui.Radar(centro, new[] { 0.8f, 0.55f, 0.35f, 0.7f, 0.5f, 0.62f },
                     new[] { "Requisitos", "Diseño", "Git", "Pruebas", "Estimación", "Procesos" }, 300);

            var paleta = Ui.Tarjeta(lista, "Paleta (Biblia §11.4.1)");
            var muestras = Ui.Fila(paleta);
            foreach (var (nombre, color) in new[] {
                         ("fondo", Tema.fondo), ("fondo 2", Tema.fondoSecundario), ("pared", Tema.pared),
                         ("hormigón", Tema.hormigon), ("cian", Tema.cian), ("cian claro", Tema.cianClaro),
                         ("mostaza", Tema.mostaza), ("naranja", Tema.naranja), ("rojo", Tema.rojo) }) {
                var muestra = Ui.PanelColumna(muestras, nombre, color: color);
                UiKit.Tamano(muestra, ancho: 110, alto: 70);
                Ui.Texto(muestra, nombre, EstiloTexto.Pequeno, Tema.texto);
            }
            Ui.Texto(paleta, "El mostaza es el color del peligro: no debe pasar del 12 % de ninguna pantalla.", EstiloTexto.Pequeno);
        }

        public override void AlMostrar() {
            // El dia de prueba no avanza mientras se mira el tema.
            _estabaPausado = App.Runner.Pausado;
            App.Runner.Pausado = true;
        }

        public override void AlOcultar() {
            App.Runner.Pausado = _estabaPausado;
        }
    }
}
