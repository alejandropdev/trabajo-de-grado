using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Core.Tarjetas;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Una pila de tarjetas de decision: una frase, un personaje, dos o tres respuestas. Se elige deslizando la
    /// tarjeta, con los botones o con las teclas ←/→ (↑ para la del medio). Mientras se inclina, las barras enseñan
    /// lo que esa respuesta le haria al proyecto; al elegir, el motor lo aplica (GameSession.ResolverTarjeta) y pasa
    /// la siguiente. No hay un parrafo que leer: el «?» da una linea de apoyo y, si la tarjeta lo tiene, abre la pizarra.
    ///
    /// Sustituye a las pantallas de lectura sin cambiar lo que se evalua: la traza queda igual.
    /// </summary>
    public sealed class PantallaDeTarjetas : Pantalla {
        public GameSession Sesion;
        public List<TarjetaDef> Tarjetas = new List<TarjetaDef>();
        public string Etiqueta = "Decisión";
        /// <summary>Se llama al resolver CADA tarjeta, con la tarjeta y la opcion elegida (por si el flujo quiere reaccionar).</summary>
        public Action<TarjetaDef, OpcionTarjeta> AlElegir;
        /// <summary>
        /// Si quien la abre resuelve la eleccion por su cuenta (las ceremonias pasan por el reloj del dia), la pantalla solo
        /// la presenta: no llama a ResolverTarjeta y deja las barras enseñando lo elegido. False = la resuelve ella.
        /// </summary>
        public bool ResuelveOtro;
        public Action AlTerminar;

        private const float PausaTrasElegir = 0.45f;
        private const float AnchoDeTarjeta = 560f;
        private const float AltoDeTarjeta = 300f;

        private int _indice;
        private TarjetaDef _actual;
        private bool _terminada;
        private float _pausa;

        private TMP_Text _progreso;
        private RectTransform _tarjeta;
        private TMP_Text _personaje, _frase, _izquierda, _derecha, _ayuda;
        private DeslizableDeTarjeta _gesto;
        private RectTransform _botones;
        private BarrasDeConsecuencia _barras;
        private readonly List<Button> _botonesDeOpcion = new List<Button>();

        public override bool PuedeVolver { get { return false; } }

        private int UltimaOpcion { get { return _actual.Opciones.Count - 1; } }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.espacio, Tema.margen));

            var cabecera = Ui.Fila(marco);
            var etiqueta = Ui.Texto(cabecera, Etiqueta, EstiloTexto.Leyenda, Tema.violet);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Resorte(cabecera);
            _progreso = Ui.Texto(cabecera, "", EstiloTexto.Leyenda);

            // La mesa: aqui la tarjeta se mueve libre, sin que ningun layout la recoloque.
            var mesa = Ui.Nodo(marco, "Mesa");
            UiKit.Tamano(mesa, flexAncho: 1, flexAlto: 1);

            _izquierda = Ui.Texto(mesa, "", EstiloTexto.Encabezado, Tema.cyan, TextAlignmentOptions.Left);
            _derecha = Ui.Texto(mesa, "", EstiloTexto.Encabezado, Tema.cyan, TextAlignmentOptions.Right);
            Anclar((RectTransform)_izquierda.transform, 0f, 160f);
            Anclar((RectTransform)_derecha.transform, 1f, 160f);

            _tarjeta = Ui.Panel(mesa, "Tarjeta", Tema.surfaceRaised);
            _tarjeta.anchorMin = _tarjeta.anchorMax = _tarjeta.pivot = new Vector2(0.5f, 0.5f);
            _tarjeta.sizeDelta = new Vector2(AnchoDeTarjeta, AltoDeTarjeta);
            var contenido = Ui.Columna(_tarjeta, "Contenido", Tema.espacio, Tema.margen, TextAnchor.MiddleCenter);
            UiKit.Rellenar(contenido);
            _personaje = Ui.Texto(contenido, "", EstiloTexto.Etiqueta, Tema.cyan, TextAlignmentOptions.Center);
            _frase = Ui.Texto(contenido, "", EstiloTexto.Subtitulo, Tema.ink, TextAlignmentOptions.Center);

            _gesto = _tarjeta.gameObject.AddComponent<DeslizableDeTarjeta>();
            _gesto.AlInclinar = Inclinada;
            _gesto.AlSoltar = lado => Elegir(lado < 0 ? 0 : UltimaOpcion);

            _ayuda = Ui.Texto(marco, "", EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Center);
            _botones = Ui.Fila(marco, "Respuestas", Tema.espacio, alineacion: TextAnchor.MiddleCenter);
            _barras = new BarrasDeConsecuencia(Ui, marco);
        }

        private static void Anclar(RectTransform rt, float x, float ancho) {
            rt.anchorMin = rt.anchorMax = new Vector2(x, 0.5f);
            rt.pivot = new Vector2(x, 0.5f);
            rt.sizeDelta = new Vector2(ancho, 80f);
        }

        public override void AlMostrar() {
            if (_actual == null && !_terminada) Mostrar(0);
            base.AlMostrar();
        }

        private void Mostrar(int indice) {
            if (indice >= Tarjetas.Count) { Terminar(); return; }
            _indice = indice;
            _actual = Tarjetas[indice];
            _pausa = 0f;

            _progreso.text = $"{indice + 1} / {Tarjetas.Count}";
            _personaje.text = _actual.Personaje;
            _frase.text = _actual.Frase;
            _ayuda.text = "";
            _izquierda.text = _actual.Opciones[0].Etiqueta;
            _derecha.text = _actual.Opciones[UltimaOpcion].Etiqueta;
            _gesto.Volver();
            _gesto.Bloquear(false);

            UiKit.Vaciar(_botones);
            _botonesDeOpcion.Clear();
            for (var i = 0; i < _actual.Opciones.Count; i++) {
                var indiceDeOpcion = i;
                var opcion = _actual.Opciones[i];
                var boton = Ui.Boton(_botones, opcion.Etiqueta, () => Elegir(indiceDeOpcion), VarianteBoton.Secundario);
                boton.gameObject.AddComponent<PrevisualizaAlPasar>().Al = entra => Previsualizar(entra ? indiceDeOpcion : -1);
                _botonesDeOpcion.Add(boton);
            }
            if (!string.IsNullOrEmpty(_actual.Ayuda) || !string.IsNullOrEmpty(_actual.Termino))
                Ui.Boton(_botones, "?", Ayuda, VarianteBoton.Fantasma);

            _barras.Preparar(Sesion.W, StocksQueToca(_actual));
        }

        /// <summary>Los stocks que alguna respuesta mueve (maximo cuatro), en el orden en que aparecen.</summary>
        private static List<string> StocksQueToca(TarjetaDef tarjeta) {
            return tarjeta.Opciones.SelectMany(o => o.Efectos.Keys).Distinct(StringComparer.Ordinal).Take(4).ToList();
        }

        // ------------------------------------------------------------ gesto y teclas

        private void Inclinada(float inclinacion) {
            if (inclinacion < -0.15f) Previsualizar(0);
            else if (inclinacion > 0.15f) Previsualizar(UltimaOpcion);
            else Previsualizar(-1);
        }

        private void Previsualizar(int indiceDeOpcion) {
            if (_actual == null || _terminada) return;
            _barras.Previsualizar(indiceDeOpcion < 0
                ? null
                : ResolutorDeTarjeta.Previsualizar(Sesion.W, _actual, _actual.Opciones[indiceDeOpcion].Id));
        }

        private void Update() {
            if (_terminada || _actual == null) return;
            _barras.Animar(Time.unscaledDeltaTime);

            if (_pausa > 0f) {
                _pausa -= Time.unscaledDeltaTime;
                if (_pausa <= 0f) Mostrar(_indice + 1);
                return;
            }

            var teclado = Keyboard.current;
            if (teclado == null) return;
            if (teclado.leftArrowKey.wasPressedThisFrame) Elegir(0);
            else if (teclado.rightArrowKey.wasPressedThisFrame) Elegir(UltimaOpcion);
            else if (teclado.upArrowKey.wasPressedThisFrame && _actual.Opciones.Count >= 3) Elegir(1);
            else if (teclado.downArrowKey.wasPressedThisFrame && _actual.Opciones.Count >= 4) Elegir(2);
        }

        // ------------------------------------------------------------ elegir y ayuda

        private void Elegir(int indiceDeOpcion) {
            if (_terminada || _actual == null || _pausa > 0f) return;
            var opcion = _actual.Opciones[indiceDeOpcion];
            _gesto.Bloquear(true);

            if (ResuelveOtro) {
                Previsualizar(indiceDeOpcion);   // las barras se quedan enseñando lo elegido
            } else {
                Sesion.ResolverTarjeta(_actual, opcion.Id);
                _barras.Confirmar(Sesion.W);
            }
            AlElegir?.Invoke(_actual, opcion);

            foreach (var b in _botonesDeOpcion) b.interactable = false;
            _pausa = PausaTrasElegir;
        }

        private void Ayuda() {
            if (_actual == null) return;
            if (!string.IsNullOrEmpty(_actual.Termino)) {
                var entrada = Glosario.Buscar(App.Catalogo.Glosario, _actual.Termino);
                if (entrada != null) { PantallaDePizarra.AbrirConcepto(App, entrada); return; }
            }
            _ayuda.text = _ayuda.text == _actual.Ayuda ? "" : _actual.Ayuda;
        }

        private void Terminar() {
            if (_terminada) return;
            _terminada = true;
            AlTerminar?.Invoke();
        }

        /// <summary>Al pasar el raton por una respuesta, las barras enseñan su consecuencia (como al inclinar la tarjeta).</summary>
        private sealed class PrevisualizaAlPasar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
            public Action<bool> Al;
            public void OnPointerEnter(PointerEventData e) { Al?.Invoke(true); }
            public void OnPointerExit(PointerEventData e) { Al?.Invoke(false); }
        }
    }
}
