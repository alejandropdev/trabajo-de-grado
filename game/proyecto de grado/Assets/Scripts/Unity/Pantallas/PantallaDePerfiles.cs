using System;
using System.Globalization;
using Nexus.Core;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La primera pantalla: quien juega. Un perfil es un estudiante — su pre-test, su coleccion y todas sus
    /// partidas cuelgan de el. Sustituye a la PantallaPerfiles de la escena antigua.
    /// </summary>
    public sealed class PantallaDePerfiles : Pantalla {
        private RectTransform _lista;
        private TMP_InputField _nombre;
        private TMP_Text _aviso;

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 1.5f));

            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 2);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "NEXUS PROTOCOL", EstiloTexto.Pequeno, Tema.cian);
            Ui.Texto(titulos, "¿Quién juega?", EstiloTexto.Titulo);
            Ui.Texto(titulos, "Cada estudiante tiene su perfil: ahí se guardan sus partidas, su entrevista de admisión y todo lo que encuentre.",
                     EstiloTexto.Pequeno);
            Ui.Boton(cabecera, "Prueba del motor", () => App.Router.Apilar<PantallaDeDiagnostico>(), VarianteBoton.Fantasma);
            Ui.Boton(cabecera, "Ver el tema", () => App.Router.Apilar<PantallaDelTema>(), VarianteBoton.Fantasma);

            var cuerpo = Ui.Fila(marco, "Cuerpo", Tema.margen, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);

            var perfiles = Ui.PanelColumna(cuerpo, "Perfiles", Tema.margen, Tema.espacio);
            UiKit.Tamano(perfiles, flexAncho: 1, flexAlto: 1);
            Ui.Texto(perfiles, "PERFILES", EstiloTexto.Pequeno, Tema.cian);
            var scroll = Ui.Desplazable(perfiles, out _lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            var nuevo = Ui.Tarjeta(cuerpo, "Perfil nuevo");
            UiKit.Tamano(nuevo, ancho: 560);
            Ui.Texto(nuevo, "Tu nombre, como quieras que aparezca en el juego.", EstiloTexto.Pequeno);
            _nombre = Ui.CampoDeTexto(nuevo, "Nombre del estudiante", "", 500);
            _nombre.characterLimit = 40;
            _nombre.onSubmit.AddListener(_ => Crear());
            _aviso = Ui.Texto(nuevo, "", EstiloTexto.Pequeno, Tema.mostaza);
            Ui.Boton(Ui.Fila(nuevo), "Crear perfil", Crear, VarianteBoton.Primario);
        }

        public override void Repintar() {
            UiKit.Vaciar(_lista);
            var perfiles = App.Perfiles.ListarPerfiles();
            if (perfiles.Count == 0) {
                Ui.Texto(_lista, "Todavía no hay ningún perfil. Crea el tuyo a la derecha.", EstiloTexto.Cuerpo, Tema.textoTenue);
                return;
            }
            foreach (var perfil in perfiles) Fila(perfil);
        }

        private void Fila(PlayerProfile perfil) {
            var fila = Ui.PanelColumna(_lista, "Perfil", Tema.espacio, 6, Tema.pared);
            var linea = Ui.Fila(fila);
            var datos = Ui.Columna(linea, espacio: 2);
            UiKit.Tamano(datos, flexAncho: 1);
            Ui.Texto(datos, perfil.nombreEstudiante, EstiloTexto.Subtitulo, Tema.texto);
            var partidas = App.Partidas.Listar(perfil.idPerfil).Count;
            var coleccion = perfil.coleccionablesGlobales == null ? 0 : perfil.coleccionablesGlobales.Count;
            Ui.Texto(datos, $"Creado el {Fecha(perfil.fechaCreacion)} · {partidas} {(partidas == 1 ? "partida" : "partidas")} · " +
                            $"{coleccion} en el diario · {(perfil.preTestHecho ? "entrevista hecha" : "sin entrevista")}",
                     EstiloTexto.Pequeno);

            var p = perfil;
            Ui.Boton(linea, "Jugar", () => { App.SeleccionarPerfil(p); App.Router.IrA<PantallaDePartidas>(); }, VarianteBoton.Primario);
            Ui.Boton(linea, "Borrar", () => App.Router.Apilar<PantallaDeConfirmacion>(c => {
                c.Titulo = $"¿Borrar a {p.nombreEstudiante}?";
                c.Texto = "Se borran el perfil y TODAS sus partidas. No se puede deshacer.";
                c.TextoSi = "Borrar";
                c.AlConfirmar = () => {
                    App.Partidas.BorrarTodasDelPerfil(p.idPerfil);
                    App.Perfiles.Borrar(p.idPerfil);
                    Repintar();
                };
            }), VarianteBoton.Fantasma);
        }

        private void Crear() {
            var nombre = (_nombre.text ?? "").Trim();
            if (nombre.Length == 0) { _aviso.text = "Escribe un nombre."; return; }
            var perfil = App.Perfiles.CrearPerfil(nombre);
            App.SeleccionarPerfil(perfil);
            App.Router.IrA<PantallaDePartidas>();
        }

        internal static string Fecha(string iso) {
            DateTime fecha;
            if (string.IsNullOrEmpty(iso) ||
                !DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out fecha)) return "—";
            return fecha.ToLocalTime().ToString("d MMM yyyy, HH:mm", new CultureInfo("es-CO"));
        }
    }
}
