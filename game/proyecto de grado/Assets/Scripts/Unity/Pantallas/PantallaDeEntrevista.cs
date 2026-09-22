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

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            _prueba = App.Catalogo.Admision;
            _preguntas = _prueba.Preguntas.OrderBy(p => p.Numero).ToList();

            var marco = UiKit.Rellenar(Ui.Fila(Raiz, "Marco", Tema.margen * 1.5f, Tema.margen * 1.5f, TextAnchor.UpperLeft));

            var ficha = Ui.Tarjeta(marco, "La entrevista");
            UiKit.Tamano(ficha, ancho: 440);
            var e = _prueba.Entrevistadora;
            if (e != null) {
                Ui.Texto(ficha, e.Nombre, EstiloTexto.Subtitulo, Tema.texto);
                if (!string.IsNullOrEmpty(e.Rol)) Ui.Texto(ficha, e.Rol, EstiloTexto.Pequeno, Tema.cianClaro);
                if (!string.IsNullOrEmpty(e.Descripcion)) Ui.Texto(ficha, e.Descripcion, EstiloTexto.Pequeno);
            }
            Ui.Separador(ficha);
            Ui.Texto(ficha, "Quince preguntas. Si no sabes una, dilo: no se descuenta nada. La entrevista no cambia nada de la partida; " +
                            "sirve para saber de dónde partes.", EstiloTexto.Pequeno);

            var centro = Ui.PanelColumna(marco, "Pregunta", Tema.margen * 1.25f, Tema.espacio);
            UiKit.Tamano(centro, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(centro, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            _hoja = new Hoja(Ui, contenido);
        }

        public override void Repintar() {
            if (_hoja == null) return;
            _hoja.Vaciar();
            var p = _preguntas[_indice];
            _hoja.Etiqueta($"Pregunta {_indice + 1} de {_preguntas.Count}");
            _hoja.Subtitulo(p.Enunciado, Tema.texto);
            _hoja.Espacio();

            string elegida;
            var respondida = _respuestas.TryGetValue(p.Numero, out elegida);
            foreach (var opcion in p.Opciones) {
                var o = opcion;
                var boton = _hoja.Opcion($"{o.Id.ToUpperInvariant()}.  {o.Texto}", null, () => Responder(p, o.Id));
                if (respondida) {
                    boton.interactable = false;
                    if (o.Id == elegida) Ui.Resaltar(boton, true);
                }
            }
            if (!respondida) {
                _hoja.Espacio(4);
                Ui.Boton(_hoja.Fila(), "No lo sé", () => Responder(p, null), VarianteBoton.Fantasma);
                return;
            }

            _hoja.Espacio();
            _hoja.Nota("El hombre del traje verde se inclina hacia Marisol:", Tema.textoTenue);
            var susurro = _hoja.Parrafo("«" + _susurro + "»", Tema.mostazaClara);
            susurro.fontStyle = TMPro.FontStyles.Italic;
            var ultima = _indice == _preguntas.Count - 1;
            _hoja.Accion(ultima ? "Terminar la entrevista" : "Siguiente pregunta", Siguiente);
        }

        private void Responder(PreguntaDeAdmision pregunta, string opcionId) {
            _respuestas[pregunta.Numero] = opcionId;   // null = «no lo sé», que cuenta como fallo
            _susurro = CorrectorDeAdmision.Susurro(pregunta, opcionId);
            Repintar();
        }

        private void Siguiente() {
            if (_indice < _preguntas.Count - 1) {
                _indice++;
                Repintar();
                return;
            }

            var perfil = App.PerfilActivo;
            if (perfil != null && !perfil.preTestHecho) {
                CorrectorDeAdmision.GuardarPreTest(perfil, CorrectorDeAdmision.Corregir(_prueba, _respuestas));
                perfil.preTestHecho = true;
                App.GuardarPerfil();
            }
            AlTerminar?.Invoke();
        }
    }
}
