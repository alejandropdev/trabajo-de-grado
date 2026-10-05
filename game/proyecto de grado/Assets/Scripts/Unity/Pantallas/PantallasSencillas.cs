using System;
using System.Collections.Generic;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Base de los dialogos: oscurece lo de debajo (que sigue viendose, y no se puede pulsar) y pone un panel
    /// centrado. La pantalla de debajo no se oculta: un modal es una pregunta sobre ella, no otro sitio.
    /// </summary>
    public abstract class PantallaModal : Pantalla {
        public override bool EsModal { get { return true; } }

        protected Hoja Hoja { get; private set; }

        protected virtual float Ancho { get { return 760; } }

        protected override void Construir() {
            var velo = Raiz.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(Tema.bg950, 0.72f);   // raycastTarget: bloquea los clics de la pantalla de debajo

            // Un modal flota sobre el HUD: surface-raised y las esquinas del Panel.
            var panel = Ui.Ventana(Raiz, "Dialogo");
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            var ajuste = panel.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            panel.sizeDelta = new Vector2(Ancho, 0);

            // ★ El contenido va en un scroll de alto acotado: un dialogo largo (una charla con su respuesta y sus
            // ayudas) crecia mas que la pantalla y dejaba sus botones fuera, sin poder pulsarlos.
            RectTransform contenido;
            var scroll = Ui.Desplazable(panel, out contenido, "Contenido del dialogo");
            _alto = UiKit.Tamano(scroll, flexAncho: 1, alto: 0);
            _contenido = contenido;
            Hoja = new Hoja(Ui, contenido);
            Rellenar();
        }

        private LayoutElement _alto;
        private RectTransform _contenido;

        /// <summary>El dialogo mide lo que su contenido, hasta casi el alto de la pantalla; lo demas, con scroll.</summary>
        private void LateUpdate() {
            if (_alto == null || _contenido == null) return;
            var maximo = Mathf.Max(200, Raiz.rect.height - 160);
            var alto = Mathf.Min(maximo, LayoutUtility.GetPreferredHeight(_contenido));
            if (Mathf.Abs(_alto.preferredHeight - alto) > 0.5f) _alto.minHeight = _alto.preferredHeight = alto;
        }

        protected abstract void Rellenar();
    }

    /// <summary>«¿Seguro?» antes de lo que no se deshace: borrar un perfil o una partida.</summary>
    public sealed class PantallaDeConfirmacion : PantallaModal {
        public string Titulo = "¿Seguro?";
        public string Texto;
        public string TextoSi = "Sí";
        public Action AlConfirmar;

        protected override void Rellenar() {
            Hoja.Subtitulo(Titulo, Tema.ink);
            if (!string.IsNullOrEmpty(Texto)) Hoja.Parrafo(Texto);
            Hoja.Espacio();
            var fila = Hoja.Fila();
            Ui.Boton(fila, TextoSi, () => { App.Router.Volver(); AlConfirmar?.Invoke(); }, VarianteBoton.Peligro);
            Ui.Boton(fila, "Cancelar", () => App.Router.Volver(), VarianteBoton.Fantasma);
        }
    }

    /// <summary>Un mensaje a pantalla completa con una sola salida: el final de la version jugable, un error de partida.</summary>
    public sealed class PantallaDeMensaje : Pantalla {
        public string Etiqueta;
        public string Titulo;
        public List<string> Parrafos = new List<string>();
        public string TextoBoton = "Volver al menú";
        public Action AlPulsar;
        public string TextoSecundario;
        public Action AlSecundario;

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            var centro = Ui.Columna(Raiz, "Centro", Tema.espacio, Tema.margen);
            centro.anchorMin = new Vector2(0.2f, 0.15f);
            centro.anchorMax = new Vector2(0.8f, 0.85f);
            centro.offsetMin = centro.offsetMax = Vector2.zero;
            ((VerticalLayoutGroup)centro.GetComponent<VerticalLayoutGroup>()).childAlignment = TextAnchor.MiddleLeft;

            var hoja = new Hoja(Ui, centro);
            if (!string.IsNullOrEmpty(Etiqueta)) hoja.Etiqueta(Etiqueta);
            hoja.Titulo(Titulo);
            foreach (var p in Parrafos) hoja.Parrafo(p);
            var boton = hoja.Accion(TextoBoton, () => (AlPulsar ?? App.MostrarInicio)());
            if (!string.IsNullOrEmpty(TextoSecundario))
                Ui.Boton(boton.transform.parent, TextoSecundario, () => AlSecundario?.Invoke(), VarianteBoton.Fantasma);
        }
    }

    /// <summary>
    /// La pausa, en cualquier momento de la partida (el dia, la Fase 1, un minijuego). Mientras esta abierta el
    /// reloj no corre, y desde aqui se puede leer el diario o guardar y salir: la partida se guarda tal cual y se
    /// retoma en el mismo sitio y con el mismo perfil (INV-7 garantiza que se recupera igual).
    /// </summary>
    public sealed class PantallaDePausa : PantallaModal {
        /// <summary>Lo que la pantalla de debajo tiene que guardar antes de salir (el borrador de la Fase 1).</summary>
        public Action AntesDeSalir;
        /// <summary>Dentro de un minijuego: salir lo deja sin hacer (cuenta como no entregado).</summary>
        public string Aviso;

        protected override float Ancho { get { return 600; } }

        public static bool Abierta { get; private set; }

        protected override void Rellenar() {
            Abierta = true;
            if (App.Runner != null) App.Runner.Pausado = true;
            var s = App.Sesion;
            Hoja.Titulo("Pausa");
            if (s != null)
                Hoja.Nota(s.Fase1Cerrada
                    ? $"{s.Perfil.Nombre} · día {s.R.DiaActual} · {s.HoraActual}. El reloj está parado."
                    : $"{s.Perfil.Nombre} · planificando (Fase 1). Lo que lleves elegido se guarda.");
            if (!string.IsNullOrEmpty(Aviso)) Hoja.Nota(Aviso, Tema.warning);
            Hoja.Espacio();
            var col = Ui.Columna(Hoja.Raiz, espacio: Tema.espacio);
            Ui.Boton(col, "Seguir jugando", Seguir, VarianteBoton.Primario);
            Ui.Boton(col, "Diario de campo", () => App.Router.Apilar<PantallaDelDiario>());
            Ui.Boton(col, "Guardar y volver al menú", Salir);
            Hoja.Nota("Al volver, elige tu perfil y pulsa «Continuar» en esta partida: sigues justo donde lo dejaste.");
        }

        private void Seguir() {
            if (App.Runner != null) App.Runner.Pausado = false;
            App.Router.Volver();
        }

        private void Salir() {
            AntesDeSalir?.Invoke();
            App.CerrarPartida();
            App.MostrarInicio();
        }

        private void OnDestroy() {
            Abierta = false;
            if (App != null && App.Runner != null) App.Runner.Pausado = false;
        }
    }

    /// <summary>
    /// El «?» que hay junto a cada termino de la asignatura. Abre su pizarron (PantallaDePizarra): varias paginas
    /// con dibujos de tiza que explican que es, que conlleva y donde se ve en Nexus.
    /// </summary>
    public static class PantallaDeGlosario {
        /// <summary>Un «?» pequeño que abre el pizarron de 'id'. No pinta nada si el glosario no lo tiene.</summary>
        public static Button Chip(AppRoot app, Transform padre, string id) {
            var e = Nexus.Core.Narrativa.Glosario.Buscar(app.Catalogo.Glosario, id);
            if (e == null) return null;
            var b = app.Ui.Boton(padre, "?", () => PantallaDePizarra.AbrirConcepto(app, e), VarianteBoton.Fantasma);
            UiKit.Tamano(b, ancho: 44);
            return b;
        }
    }

    /// <summary>
    /// Hablar con alguien de la sala: unas lineas, su pregunta y tus respuestas. Al contestar, el personaje responde
    /// (y esa respuesta es la explicacion de por que estaba bien o mal), y se ve como cambio su confianza.
    /// </summary>
    public sealed class PantallaDeConversacion : PantallaModal {
        public Nexus.Core.Relaciones.Conversacion Conversacion;
        /// <summary>Contesta y devuelve lo que paso. Lo hace la sesion (cobra los minutos).</summary>
        public Func<string, Nexus.Core.Relaciones.ResultadoDeConversacion> AlContestar;
        public Action<Nexus.Core.Relaciones.ResultadoDeConversacion> AlCerrar;

        private Nexus.Core.Relaciones.ResultadoDeConversacion _resultado;

        protected override float Ancho { get { return 900; } }

        protected override void Rellenar() {
            var c = Conversacion;
            var personaje = App.Catalogo.Relaciones.PersonajePorId(c.Personaje);
            Hoja.Etiqueta((personaje?.Rol ?? "") + $" · {c.Minutos} min de charla");
            Hoja.Titulo(personaje?.Nombre ?? c.Personaje);
            foreach (var l in c.Lineas) Hoja.Dialogo(l.Quien, l.Texto);

            if (c.Pregunta == null || c.Pregunta.Opciones.Count == 0) {
                Hoja.Accion("Seguir", () => Contestar(null));
                return;
            }
            Hoja.Espacio();
            Ui.Texto(Hoja.Raiz, c.Pregunta.Texto, EstiloTexto.Encabezado, Tema.ink);
            foreach (var o in c.Pregunta.Opciones) {
                var opcion = o;
                Hoja.Opcion(opcion.Texto, null, () => Contestar(opcion.Id));
            }
        }

        private void Contestar(string opcionId) {
            if (_resultado != null) return;
            _resultado = AlContestar?.Invoke(opcionId);
            if (_resultado == null) { Cerrar(); return; }

            Hoja.Vaciar();
            var personaje = App.Catalogo.Relaciones.PersonajePorId(Conversacion.Personaje);
            Hoja.Titulo(personaje?.Nombre ?? Conversacion.Personaje);
            if (!string.IsNullOrEmpty(_resultado.Respuesta)) Hoja.Dialogo(personaje?.Nombre ?? Conversacion.Personaje, _resultado.Respuesta);
            var cambio = _resultado.CambioDeConfianza;
            var fila = Hoja.Fila();
            Ui.Badge(fila, cambio > 0 ? $"Confianza +{cambio}" : cambio < 0 ? $"Confianza {cambio}" : "La confianza no cambia",
                     cambio > 0 ? Tono.Exito : cambio < 0 ? Tono.Peligro : Tono.Neutro);
            Ui.Texto(fila, cambio == 0 ? $"sigue en {_resultado.ConfianzaTotal}" : $"ahora {_resultado.ConfianzaTotal}", EstiloTexto.Pequeno);
            foreach (var a in _resultado.AyudasNuevas) {
                var t = Hoja.Tarjeta("¡Te ofrece una ayuda!", Tono.Cyan);
                Ui.Texto(t, "«" + a.Texto + "»", EstiloTexto.Dialogo);
                Ui.Texto(t, TextoDeAyuda(a.Tipo) + (a.Usos > 1 ? $" · {a.Usos} usos" : ""), EstiloTexto.Pequeno);
            }
            Hoja.Accion("Seguir", Cerrar);
        }

        private void Cerrar() {
            App.Router.Volver();
            AlCerrar?.Invoke(_resultado);
        }

        /// <summary>Que hace cada ayuda, en palabras del jugador.</summary>
        public static string TextoDeAyuda(string tipo) {
            switch (tipo) {
                case Nexus.Core.Relaciones.TiposDeAyuda.RevelarDefectos: return "En un reparto de horas de pruebas: ves cuántos errores hay de verdad en cada tipo.";
                case Nexus.Core.Relaciones.TiposDeAyuda.MostrarDependencias: return "En un backlog: ves qué tarjeta necesita cuál mientras ordenas.";
                case Nexus.Core.Relaciones.TiposDeAyuda.PistaDetectar: return "En una revisión: una pieza con un problema de verdad aparece señalada.";
                case Nexus.Core.Relaciones.TiposDeAyuda.MinutosExtra: return "En cualquier reto: 45 segundos más de reloj.";
                case Nexus.Core.Relaciones.TiposDeAyuda.BajarCansancio: return "Cuando quieras durante el día: el equipo descansa y baja el cansancio.";
                default: return tipo;
            }
        }
    }
}
