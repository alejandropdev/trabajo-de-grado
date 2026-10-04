using System.Collections.Generic;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// El escaparate del design system: todos los tokens y todas las piezas de UiKit juntas. Sirve para ver de un
    /// vistazo si un color, una fuente o una medida del tema se aplica bien, sin tener que llegar a la pantalla donde
    /// se usa. Va dentro de una lista con scroll, asi que puede crecer sin montarse encima de nada.
    /// </summary>
    public sealed class PantallaDelTema : Pantalla {
        private bool _estabaPausado;

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", relleno: Tema.margen));

            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 0);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "Design system · tema Núcleo", EstiloTexto.Leyenda, Tema.cyan).fontStyle |= FontStyles.UpperCase;
            Ui.Texto(titulos, "El tema", EstiloTexto.Titulo);
            Ui.Boton(cabecera, "Volver", App.Router.Volver, VarianteBoton.Primario);

            Ui.Texto(marco, "Todo lo de esta pantalla sale de NexusTheme (Assets/Settings/NexusTheme.asset) y de las fuentes de " +
                            "Resources/Fuentes. Cambia un token del asset y cambia aquí y en todas las pantallas, sin tocar código. " +
                            "La receta de los minijuegos y las pizarras de tiza no usan nada de esto: se quedan como estaban.",
                     EstiloTexto.Pequeno);
            Ui.Separador(marco);

            var scroll = Ui.Desplazable(marco, out var lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            Colores(lista);
            Tipografia(lista);
            Botones(lista);
            Indicadores(lista);
            Narrativa(lista);
            Hud(lista);
        }

        private void Colores(Transform lista) {
            var t = Ui.Tarjeta(lista, "Color");
            Muestras(t, new[] {
                ("bg-950", Tema.bg950), ("bg-900", Tema.bg900), ("surface", Tema.surface), ("surface-raised", Tema.surfaceRaised),
                ("surface-sunken", Tema.surfaceSunken), ("line", Tema.line), ("line-strong", Tema.lineStrong) });
            Muestras(t, new[] {
                ("ink", Tema.ink), ("ink-muted", Tema.inkMuted), ("ink-faint", Tema.inkFaint),
                ("cyan", Tema.cyan), ("cyan-soft", Tema.cyanSoft), ("violet", Tema.violet), ("violet-soft", Tema.violetSoft) });
            Muestras(t, new[] {
                ("success", Tema.success), ("success-soft", Tema.successSoft), ("warning", Tema.warning), ("warning-soft", Tema.warningSoft),
                ("danger", Tema.danger), ("danger-soft", Tema.dangerSoft), ("paper-bg", Tema.paperBg) });
            Ui.Texto(t, "Cyan es la marca (selección, progreso, foco). Violet, solo momentos narrativos. Danger = urgente, " +
                        "warning = aviso o deuda, success = hecho. Si todo es urgente, nada lo es.", EstiloTexto.Pequeno);
        }

        private void Muestras(Transform padre, (string nombre, Color color)[] muestras) {
            var fila = Ui.Fila(padre, espacio: Tema.Espacio(2));
            foreach (var (nombre, color) in muestras) {
                var col = Ui.Columna(fila, nombre, 4);
                var muestra = Ui.Panel(col, nombre, color);
                UiKit.Tamano(muestra, ancho: 150, alto: 56);
                Ui.Texto(col, nombre, EstiloTexto.Leyenda);
            }
        }

        private void Tipografia(Transform lista) {
            var t = Ui.Tarjeta(lista, "Tipografía");
            Ui.Texto(t, "NEXUS PROTOCOL", EstiloTexto.Hero);
            Ui.Texto(t, "Fase 1 · phase", EstiloTexto.Titulo);
            Ui.Texto(t, "Dashboard de proyecto · panel-title", EstiloTexto.Subtitulo);
            Ui.Texto(t, "Selección de arquitectura · heading", EstiloTexto.Encabezado);
            Ui.Texto(t, "Body: así se leen los briefings, los guiones y las rúbricas. Las frases largas se parten en varias " +
                        "líneas y la tarjeta crece con ellas.", EstiloTexto.Cuerpo);
            Ui.Texto(t, "Un nuevo día comienza en la torre. Tu futuro está a punto de escribirse. · dialogue", EstiloTexto.Dialogo);
            Ui.Texto(t, "Etiqueta de botón · label", EstiloTexto.Etiqueta);
            Ui.Texto(t, "Texto secundario y notas, en ink-muted.", EstiloTexto.Pequeno);
            Ui.Texto(t, "Día 12 · 09:45 · caption", EstiloTexto.Leyenda);
            Ui.Texto(t, "[INFO] Build started... · code", EstiloTexto.Mono);
            Ui.Texto(t, "root@n1-cradle:~$ · code-label", EstiloTexto.CodigoEtiqueta);
        }

        private void Botones(Transform lista) {
            var t = Ui.Tarjeta(lista, "Botones y selección");
            var fila = Ui.Fila(t);
            Ui.Boton(fila, "Primario", null, VarianteBoton.Primario);
            Ui.Boton(fila, "Secundario", null);
            Ui.Boton(fila, "Peligro", null, VarianteBoton.Peligro);
            Ui.Boton(fila, "Fantasma", null, VarianteBoton.Fantasma);
            var menu = Ui.Columna(t, "Menu", Tema.Espacio(2));
            UiKit.Tamano(menu, ancho: 420);
            Ui.Resaltar(Ui.Boton(menu, "►  Opción de menú activa", null, VarianteBoton.Menu), true);
            Ui.Boton(menu, "►  Opción de menú", null, VarianteBoton.Menu);

            var tarjetas = Ui.Fila(t, "Tarjetas", Tema.espacio, alineacion: TextAnchor.UpperLeft);
            var a = Ui.BotonDeOpcion(tarjetas, "Scrum", "Iteraciones cortas y revisión con el cliente.", null);
            Ui.Resaltar(a, true);
            Ui.BotonDeOpcion(tarjetas, "Kanban", "Flujo continuo con límites de trabajo en curso.", null);
            Ui.BotonDeOpcion(tarjetas, "Cascada", "Fases cerradas, una tras otra.", null);
            foreach (Transform c in tarjetas) UiKit.Tamano(c, ancho: 0, flexAncho: 1);

            var etiquetas = Ui.Columna(t, "Etiquetas", Tema.Espacio(2));
            UiKit.Tamano(etiquetas, ancho: 520);
            Ui.Resaltar(Ui.OpcionDeEtiqueta(etiquetas, "Fuga de estado global", Tono.Peligro, null), true, Tono.Peligro);
            Ui.OpcionDeEtiqueta(etiquetas, "Acoplamiento excesivo", Tono.Aviso, null);
            Ui.OpcionDeEtiqueta(etiquetas, "Observación neutra", Tono.Cyan, null);

            Ui.CampoDeTexto(t, "Campo de texto", "");
        }

        private void Indicadores(Transform lista) {
            var t = Ui.Tarjeta(lista, "Indicadores");
            var badges = Ui.Fila(t, espacio: Tema.Espacio(2));
            Ui.Badge(badges, "Lead dev");
            Ui.Badge(badges, "Info", Tono.Cyan);
            Ui.Badge(badges, "Hecho", Tono.Exito);
            Ui.Badge(badges, "Aviso", Tono.Aviso);
            Ui.Badge(badges, "Urgente", Tono.Peligro, true);

            Ui.Texto(t, "Avance", EstiloTexto.Pequeno);
            Ui.Barra(t, 0.72f);
            Ui.Texto(t, "Deuda técnica", EstiloTexto.Pequeno);
            Ui.Barra(t, 0.35f, Tema.warning);

            var diales = Ui.Fila(t, espacio: Tema.margen * 2);
            Ui.Dial(diales, 0.64f, "Tiempo");
            Ui.Dial(diales, 0.41f, "Costo", Tema.warning);
            Ui.Dial(diales, 0.22f, "Calidad", Tema.danger);
            Ui.Radar(diales, new[] { 0.8f, 0.55f, 0.35f, 0.7f, 0.5f, 0.62f },
                     new[] { "Requisitos", "Diseño", "Git", "Pruebas", "Estimación", "Procesos" }, 240);

            var cifras = Ui.Fila(t, espacio: Tema.Espacio(3));
            Ui.Cifra(cifras, "16h", "Unitarias", "23 %");
            Ui.Cifra(cifras, "920 pts", "Backlog");
            Ui.Cifra(cifras, "4", "Errores al cliente", null, Tono.Peligro);
            Ui.Temporizador(cifras, "01:30");
            Ui.Temporizador(cifras, "00:12", Tono.Aviso);
            Ui.Temporizador(cifras, "00:04", Tono.Peligro);
        }

        private void Narrativa(Transform lista) {
            var t = Ui.Tarjeta(lista, "Narrativa");
            TMP_Text linea;
            Ui.Dialogo(t, "Marisol Andrade", "Un nuevo día comienza. Mira el panel del centro: siempre dice qué puedes hacer ahora.",
                       out linea, Tono.Cyan, true);
            Ui.Dialogo(t, "Dra. Voss", "Las métricas no mienten. Cada número es una vida en este lugar.", out linea, Tono.Violeta);
            Ui.Briefing(t, new List<RenglonDeBriefing> {
                new RenglonDeBriefing("•", "Lo que se ve", "Un diagrama con dos piezas que no deberían hablarse."),
                new RenglonDeBriefing("→", "Lo que haces", "Marcas cada pieza con el nombre de su problema."),
                new RenglonDeBriefing("!", "Lo que cambia", "Lo que se escape lo notará el cliente."),
                new RenglonDeBriefing("√", "Cómo se cierra", "Entregas antes de que acabe el reloj.")
            });
        }

        private void Hud(Transform lista) {
            var fila = Ui.Fila(lista, "HUD", Tema.Espacio(6), alineacion: TextAnchor.UpperLeft);
            var t = Ui.Tarjeta(fila, "Notificaciones");
            UiKit.Tamano(t, ancho: 0, flexAncho: 1);
            Ui.Notificacion(t, "Stage bloqueado", Tono.Peligro, "Hace 2 min");
            Ui.Notificacion(t, "Ley de Brooks activada", Tono.Aviso, "Hace 9 min");
            Ui.Notificacion(t, "Nómina aprobada", Tono.Cyan, "09:12");
            Ui.Notificacion(t, "Javier llegó a la oficina", Tono.Neutro, "08:55");

            var e = Ui.Tarjeta(fila, "Equipo y pasos");
            UiKit.Tamano(e, ancho: 0, flexAncho: 1);
            Ui.FilaDeEquipo(e, "Javier", "Lead dev", EstadoDeEquipo.Activo);
            Ui.FilaDeEquipo(e, "Sarah Lindqvist", "Arquitecta", EstadoDeEquipo.Bloqueado);
            Ui.FilaDeEquipo(e, "Óscar Rendón", "QA", EstadoDeEquipo.Inactivo);
            Ui.Pasos(e, new List<KeyValuePair<string, EstadoDePaso>> {
                new KeyValuePair<string, EstadoDePaso>("Requerimientos funcionales", EstadoDePaso.Hecho),
                new KeyValuePair<string, EstadoDePaso>("Arquitectura de datos", EstadoDePaso.Parcial),
                new KeyValuePair<string, EstadoDePaso>("Diseño de interfaz", EstadoDePaso.Pendiente)
            });

            var alerta = Ui.Tarjeta(fila, "Deuda técnica alta", Tono.Peligro);
            UiKit.Tamano(alerta, ancho: 0, flexAncho: 1);
            Ui.Registro(alerta, new[] {
                new KeyValuePair<NivelDeLog, string>(NivelDeLog.Info, "Build started..."),
                new KeyValuePair<NivelDeLog, string>(NivelDeLog.Aviso, "Dockerfile: imagen base sin versión"),
                new KeyValuePair<NivelDeLog, string>(NivelDeLog.Error, "Healthcheck failed: /api/pagos")
            });
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
