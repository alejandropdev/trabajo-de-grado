using System;
using System.Collections.Generic;
using Nexus.Core.Modelo;
using Nexus.Core.Proyecto;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;

namespace Nexus.Unity.Pantallas {
    /// <summary>Un dialogo con titulo, lo que se le pinte dentro y «Cerrar». Para lo que se consulta y se suelta.</summary>
    public sealed class PantallaDeFicha : PantallaModal {
        public string Titulo;
        public Action<Hoja> Pintar;
        public float AnchoDeLaFicha = 900;
        public Action AlCerrar;
        private bool _cerrada;

        protected override float Ancho { get { return AnchoDeLaFicha; } }

        /// <summary>Se pinta en Repintar (que el router llama al abrirla): asi quien la abre puede volver a pintarla.</summary>
        protected override void Rellenar() { }

        public override void Repintar() {
            if (Hoja == null) return;
            Hoja.Vaciar();
            Hoja.Subtitulo(Titulo ?? "", Tema.ink);
            Pintar?.Invoke(Hoja);
            Hoja.Accion("Cerrar", () => App.Router.Volver(), VarianteBoton.Secundario);
        }

        public override void AlOcultar() {
            if (_cerrada) return;
            _cerrada = true;
            AlCerrar?.Invoke();
        }
    }

    /// <summary>
    /// Lo del expediente del proyecto que es de CONSULTA: el ticket original, las palabras del oficio, los
    /// documentos que importan, el encargo para releerlo al elegir arquitectura. Antes iba todo apilado en la
    /// primera pagina de la Fase 1; ahora esta a un boton, y la pagina se queda con lo que hay que leer si o si.
    /// </summary>
    public static class DialogosDelExpediente {
        private static void Abrir(AppRoot app, string titulo, Action<Hoja> pintar) {
            app.Router.Apilar<PantallaDeFicha>(p => { p.Titulo = titulo; p.Pintar = pintar; });
        }

        /// <summary>El ticket del cliente, con el aspecto de las pantallas de indicadores (ver TicketDelCliente).</summary>
        public static void Ticket(AppRoot app, LevelProfile perfil) {
            TicketDelCliente.Abrir(app, perfil);
        }

        public static void Vocabulario(AppRoot app, FichaDelProyecto ficha) {
            Abrir(app, "Palabras del oficio", h => {
                h.Nota("Las usa tu cliente y las verás en los diagramas y en las tarjetas. Pulsa cada una para ver qué es.");
                ExpedienteView.ChipsDeVocabulario(app, h.Raiz, ficha);
            });
        }

        public static void Documentos(AppRoot app, FichaDelProyecto ficha) {
            Abrir(app, "Los documentos que importan aquí", h => {
                var ui = app.Ui;
                foreach (var a in ficha.Artefactos) {
                    var fila = h.Fila(6);
                    var texto = ui.Texto(fila, $"<b>{a.Id}</b>: {a.ParaQueAqui}", EstiloTexto.Cuerpo, ui.Tema.ink);
                    UiKit.Tamano(texto, flexAncho: 1);
                    if (!string.IsNullOrEmpty(a.Glosario)) PantallaDeGlosario.Chip(app, fila, a.Glosario);
                }
            });
        }

        /// <summary>El encargo y lo averiguado en el recorrido, para releerlos antes de elegir la arquitectura.</summary>
        public static void Encargo(AppRoot app, LevelProfile perfil, IEnumerable<string> pistas) {
            TicketDelCliente.Abrir(app, perfil, pistas ?? new string[0], "El encargo, otra vez");
        }
    }
}
