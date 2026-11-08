using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Tarjetas;
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
    ///
    /// Rediseño 2026-11-07 (sin ventana ni scroll; cada pregunta se ve distinta para no aburrir):
    ///   izquierda  la ficha de Marisol (retrato, nombre, una linea) y, debajo, el edificio en corte con el ascensor
    ///   arriba     la pregunta sin marco y las cartas, que llegan de una forma distinta en cada pregunta
    ///   abajo      la pizarra: el dibujo de tiza del tema de la pregunta, trazo a trazo; al responder, el susurro de HH
    /// Las preguntas, su orden, la correccion, el guardado y la reanudacion no cambian.
    /// </summary>
    public sealed class PantallaDeEntrevista : Pantalla {
        public Action AlTerminar;

        private PruebaDeAdmision _prueba;
        private List<PreguntaDeAdmision> _preguntas;
        private readonly Dictionary<int, string> _respuestas = new Dictionary<int, string>();
        private int _indice;
        private string _susurro;
        // La vista corta (frase + etiquetas) es la de siempre; la completa es el instrumento original, a peticion.
        private bool _completa;

        private RectTransform _pregunta;
        private TMPro.TMP_Text _lineaDeMarisol;
        private EdificioDeLaEntrevista _edificio;
        private PizarraDeLaPregunta _pizarra;
        private int _pizarraDe = -1;
        private TMPro.TMP_Text _susurroEnPantalla;
        private float _letras;

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

            // ---- izquierda: Marisol y el edificio
            var izquierda = Ui.Columna(marco, "Izquierda", Tema.espacio);
            UiKit.Tamano(izquierda, ancho: 380, flexAlto: 1);
            var ficha = PiezasDelMuro.Panel(Ui, izquierda, "La entrevista", Tema.violet, 14);
            var fila = Ui.Fila(ficha, "Marisol", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var e = _prueba.Entrevistadora;
            if (e != null) Ui.Ilustracion(fila, MaterialesNexus.IdDePersonaje(e.Nombre), null, ancho: 110, alto: 130);
            var datos = Ui.Columna(fila, "Datos", 4);
            UiKit.Tamano(datos, flexAncho: 1);
            if (e != null) Ui.Texto(datos, e.Nombre, EstiloTexto.Encabezado);
            _lineaDeMarisol = Ui.Texto(datos, "", EstiloTexto.Pequeno, Tema.inkMuted);
            _edificio = EdificioDeLaEntrevista.Crear(Ui, izquierda, _preguntas.Count, _respuestas.Count);

            // ---- derecha: la pregunta (sin marco) y la pizarra
            var derecha = Ui.Columna(marco, "Derecha", Tema.espacio);
            UiKit.Tamano(derecha, flexAncho: 1, flexAlto: 1);
            _pregunta = Ui.Columna(derecha, "Pregunta", Tema.espacio);
            UiKit.Tamano(_pregunta, flexAncho: 1);
            _pizarra = PizarraDeLaPregunta.Crear(Ui, derecha);
        }

        public override void Repintar() {
            if (_pregunta == null) return;
            UiKit.Vaciar(_pregunta);
            _susurroEnPantalla = null;
            var p = _preguntas[_indice];
            string elegida;
            var respondida = _respuestas.TryGetValue(p.Numero, out elegida);
            var nueva = _pizarraDe != _indice;
            // El ascensor sube un piso por respuesta, sea cual sea (no dice si acertaste).
            if (_edificio != null) _edificio.IrA(_respuestas.Count);

            // Marisol: la linea de siempre y, a mitad de camino, un respiro neutro (ni nota ni pista).
            var respiro = !respondida && (_indice == 5 || _indice == 10);
            _lineaDeMarisol.text = !respiro ? "Quince preguntas, un piso cada una. Si no sabes una, dilo: no se descuenta nada."
                                 : _indice == 5 ? "«Bien. Seguimos subiendo.»" : "«Ya casi arriba. Cinco más.»";
            _lineaDeMarisol.color = respiro ? Tema.violet : Tema.inkMuted;

            // ---- la pregunta, sin marco
            var cabecera = Ui.Fila(_pregunta, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var rotulo = Ui.Texto(cabecera, $"PISO {_indice + 1} · PREGUNTA {_indice + 1} DE {_preguntas.Count}", EstiloTexto.Leyenda, Tema.cyan);
            rotulo.characterSpacing = 3;
            UiKit.Tamano(rotulo, flexAncho: 1);
            var corta = TieneVistaCorta(p) && !_completa;
            if (!respondida && TieneVistaCorta(p))
                Ui.Boton(cabecera, corta ? "Ver pregunta completa" : "Ver versión corta", () => { _completa = !_completa; Repintar(); }, VarianteBoton.Fantasma);
            var enunciado = Ui.Texto(_pregunta, corta ? p.Frase : p.Enunciado, EstiloTexto.Titulo, Tema.ink);
            enunciado.fontSize = corta ? 40 : 30;
            if (nueva) enunciado.gameObject.AddComponent<EntradaDePieza>();

            // Las opciones, como una mano de cartas en su orden de siempre (A, B, C: el instrumento no se baraja), y
            // «No lo sé» como una carta menor. El sello es el mismo para todas: ANOTADO. Llegan de una forma distinta
            // en cada pregunta.
            var cartas = new List<OpcionDeCarta>();
            var indiceElegida = -1;
            foreach (var opcion in p.Opciones) {
                var o = opcion;
                if (respondida && o.Id == elegida) indiceElegida = cartas.Count;
                cartas.Add(new OpcionDeCarta { Titulo = corta ? o.Etiqueta : o.Texto, AlElegir = () => Responder(p, o.Id) });
            }
            if (respondida && elegida == null) indiceElegida = cartas.Count;
            cartas.Add(new OpcionDeCarta { Titulo = "No lo sé", Detalle = "Prefiero no adivinar.", Discreta = true, AlElegir = () => Responder(p, null) });
            var mano = Ui.Fila(_pregunta, "Mano", 0, 0, TextAnchor.MiddleCenter);
            ManoDeCartas.Crear(Ui, mano, cartas, new ManoDeCartas.Ajustes {
                Sello = "ANOTADO", ColorDelSello = Tema.violet, ConLetras = true, Elegida = respondida ? indiceElegida : -1,
                AnchoMaximo = 1100, Entrada = DisposicionDeMano.EntradaPara(p.Numero),
            });

            // ---- la pizarra: el dibujo del tema (una vez por pregunta) y, si ya se respondio, el susurro
            if (nueva) {
                _pizarraDe = _indice;
                _pizarra.Mostrar(p.Escena, p.Numero);
            }
            if (!respondida) return;
            var ultima = _indice == _preguntas.Count - 1;
            // Un momento de historia, no un aviso: el susurro se escribe letra a letra (Enter o Espacio lo completa).
            _susurroEnPantalla = _pizarra.Susurro(_susurro, (ultima ? "Terminar la entrevista" : "Siguiente pregunta") + "  ↵", Siguiente);
            _susurroEnPantalla.maxVisibleCharacters = 0;
            _letras = 0;
        }

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
