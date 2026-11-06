using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Las 15 preguntas de la entrevista de admision: el pre-test. No afecta a la partida — pase lo que pase,
    /// entras al concurso — pero se guarda en el perfil, y al final del curso se compara con el post-test
    /// (ganancia de Hake). Por eso solo cuenta la primera vez que un perfil la hace.
    ///
    /// Tras cada respuesta, el hombre del traje verde se inclina y le susurra algo a Marisol. Ella no lo repite;
    /// tu si lo oyes. Es la unica retroalimentacion: la entrevista no dice «correcto» ni «incorrecto».
    /// </summary>
    public sealed class PantallaDeEntrevista : Pantalla {
        public Action AlTerminar;

        private PruebaDeAdmision _prueba;
        private List<PreguntaDeAdmision> _preguntas;
        private readonly Dictionary<int, string> _respuestas = new Dictionary<int, string>();
        private int _indice;
        private string _susurro;
        private Hoja _hoja;
        // La vista corta (frase + etiquetas) es la de siempre; la completa es el instrumento original, a peticion.
        private bool _completa;

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            _prueba = App.Catalogo.Admision;
            _preguntas = _prueba.Preguntas.OrderBy(p => p.Numero).ToList();
            // Quien cerro el juego a mitad vuelve a la pregunta donde iba, con sus respuestas.
            var guardada = App.PerfilActivo != null ? App.PerfilActivo.entrevistaEnCurso : null;
            if (EntrevistaEnCurso.HayUnaAMedias(guardada)) {
                foreach (var r in guardada.respuestas) _respuestas[r.Key] = r.Value;
                _indice = EntrevistaEnCurso.IndiceDeInicio(guardada, _preguntas.Count);
                string elegida;
                if (_respuestas.TryGetValue(_preguntas[_indice].Numero, out elegida))
                    _susurro = CorrectorDeAdmision.Susurro(_preguntas[_indice], elegida);
            }

            var marco = UiKit.Rellenar(Ui.Fila(Raiz, "Marco", Tema.margen * 1.5f, Tema.margen * 1.5f, TextAnchor.UpperLeft));

            var ficha = Ui.Tarjeta(marco, "La entrevista");
            UiKit.Tamano(ficha, ancho: 440);
            // Marisol a la izquierda y, a su lado, el ascensor: cada respuesta sube un piso (2026-11-05).
            var lado = Ui.Fila(ficha, "Retrato y ascensor", Tema.espacio, alineacion: TextAnchor.UpperLeft);
            var persona = Ui.Columna(lado, "Entrevistadora", Tema.Espacio(2));
            UiKit.Tamano(persona, ancho: 0, flexAncho: 1);
            var e = _prueba.Entrevistadora;
            if (e != null) {
                // Quien entrevista, con su retrato (o su silueta mientras no haya arte).
                Ui.Ilustracion(persona, MaterialesNexus.IdDePersonaje(e.Nombre), null, alto: 240);
                Ui.Texto(persona, e.Nombre, EstiloTexto.Encabezado);
                if (!string.IsNullOrEmpty(e.Rol)) Ui.Texto(persona, e.Rol, EstiloTexto.Pequeno, Tema.cyan);
            }
            Ui.Texto(persona, "Quince preguntas, un piso cada una. Si no sabes una, dilo: no se descuenta nada.", EstiloTexto.Pequeno);
            _ascensor = AscensorDeLaEntrevista.Crear(Ui, lado, _preguntas.Count, _respuestas.Count);

            var centro = Ui.Ventana(marco, "Pregunta");
            UiKit.Tamano(centro, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(centro, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            _hoja = new Hoja(Ui, contenido);
        }

        public override void Repintar() {
            if (_hoja == null) return;
            _hoja.Vaciar();
            _susurroEnPantalla = null;
            var p = _preguntas[_indice];
            string elegida;
            var respondida = _respuestas.TryGetValue(p.Numero, out elegida);
            // El ascensor sube un piso por respuesta, sea cual sea (no dice si acertaste).
            if (_ascensor != null) _ascensor.IrA(_respuestas.Count);

            _hoja.Etiqueta($"Piso {_indice + 1} · pregunta {_indice + 1} de {_preguntas.Count}");
            // Un respiro a mitad de camino: una linea de Marisol, neutra (ni nota ni pista).
            if (!respondida && (_indice == 5 || _indice == 10))
                _hoja.Nota(_indice == 5 ? "Marisol pasa una hoja: «Bien. Seguimos subiendo.»" : "Marisol sonríe a medias: «Ya casi arriba. Cinco más.»", Tema.violet);
            var corta = TieneVistaCorta(p) && !_completa;
            Ui.Texto(_hoja.Raiz, corta ? p.Frase : p.Enunciado, EstiloTexto.Encabezado);
            _hoja.Espacio();

            // Las opciones, como una mano de cartas en su orden de siempre (A, B, C: el instrumento no se baraja), y
            // «No lo sé» como una carta menor. El sello es el mismo para todas: ANOTADO.
            var cartas = new List<OpcionDeCarta>();
            var indiceElegida = -1;
            foreach (var opcion in p.Opciones) {
                var o = opcion;
                if (respondida && o.Id == elegida) indiceElegida = cartas.Count;
                cartas.Add(new OpcionDeCarta { Titulo = corta ? o.Etiqueta : o.Texto, AlElegir = () => Responder(p, o.Id) });
            }
            if (respondida && elegida == null) indiceElegida = cartas.Count;
            cartas.Add(new OpcionDeCarta { Titulo = "No lo sé", Detalle = "Prefiero no adivinar.", Discreta = true, AlElegir = () => Responder(p, null) });
            _hoja.Mano(cartas, new ManoDeCartas.Ajustes {
                Sello = "ANOTADO", ColorDelSello = Tema.violet, ConLetras = true, Elegida = respondida ? indiceElegida : -1, AnchoMaximo = 1100
            });

            if (!respondida) {
                if (TieneVistaCorta(p)) {
                    _hoja.Espacio(2);
                    Ui.Boton(_hoja.Fila(), corta ? "Ver pregunta completa" : "Ver versión corta", () => { _completa = !_completa; Repintar(); }, VarianteBoton.Fantasma);
                }
                return;
            }

            _hoja.Espacio();
            _hoja.Nota("El hombre del traje verde se inclina hacia Marisol:", Tema.inkMuted);
            // Un momento de historia, no un aviso: el susurro se escribe letra a letra (Enter o Espacio lo completa).
            _susurroEnPantalla = _hoja.Parrafo("«" + _susurro + "»", Tema.violet);
            _susurroEnPantalla.fontStyle = TMPro.FontStyles.Italic;
            _susurroEnPantalla.maxVisibleCharacters = 0;
            _letras = 0;
            var ultima = _indice == _preguntas.Count - 1;
            _hoja.Accion((ultima ? "Terminar la entrevista" : "Siguiente pregunta") + "  ↵", Siguiente);
        }

        private AscensorDeLaEntrevista _ascensor;
        private TMPro.TMP_Text _susurroEnPantalla;
        private float _letras;

        private void Update() {
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            var pulsa = teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame)
                        && App != null && App.Router.Actual == this;
            if (_susurroEnPantalla == null) return;
            var total = _susurroEnPantalla.text.Length;
            if (_letras < total) {
                _letras = pulsa ? total : _letras + Time.unscaledDeltaTime * 55f;
                _susurroEnPantalla.maxVisibleCharacters = Mathf.Min(total, (int)_letras);
                return;
            }
            if (pulsa) Siguiente();
        }

        private void Responder(PreguntaDeAdmision pregunta, string opcionId) {
            _respuestas[pregunta.Numero] = opcionId;   // null = «no lo sé», que cuenta como fallo
            _susurro = CorrectorDeAdmision.Susurro(pregunta, opcionId);
            var perfil = App.PerfilActivo;
            if (perfil != null && !perfil.preTestHecho) {
                perfil.entrevistaEnCurso = EntrevistaEnCurso.Anotar(perfil.entrevistaEnCurso, pregunta.Numero, opcionId,
                                                                    Math.Min(_indice + 1, _preguntas.Count - 1));
                App.GuardarPerfil();
            }
            Repintar();
        }

        private bool TieneVistaCorta(PreguntaDeAdmision p) {
            return App.Preferencias.modoTarjetas && !string.IsNullOrEmpty(p.Frase) && p.Opciones.All(o => !string.IsNullOrEmpty(o.Etiqueta));
        }

        private void Siguiente() {
            if (_indice < _preguntas.Count - 1) {
                _completa = false;
                _indice++;
                Repintar();
                return;
            }

            var perfil = App.PerfilActivo;
            if (perfil != null && !perfil.preTestHecho) {
                CorrectorDeAdmision.GuardarPreTest(perfil, CorrectorDeAdmision.Corregir(_prueba, _respuestas));
                perfil.preTestHecho = true;
                perfil.entrevistaEnCurso = null;
                App.GuardarPerfil();
            }
            AlTerminar?.Invoke();
        }
    }
}
