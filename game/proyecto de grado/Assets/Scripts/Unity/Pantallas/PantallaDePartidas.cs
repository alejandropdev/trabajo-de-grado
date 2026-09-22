using System;
using Nexus.Core.Guardado;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El menu de un perfil: empezar una partida, seguir una guardada, abrir el diario. Una partida es un
    /// recorrido por los niveles con su propia semilla; el modo aula fija la semilla para que toda la clase
    /// juegue exactamente la misma partida.
    /// </summary>
    public sealed class PantallaDePartidas : Pantalla {
        private RectTransform _lista;
        private TMP_InputField _nombre;
        private TMP_InputField _semilla;
        private Button _botonAula;
        private GameObject _filaSemilla;
        private TMP_Text _aviso;
        private bool _modoAula;

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 1.5f));

            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 2);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "NEXUS PROTOCOL", EstiloTexto.Pequeno, Tema.cian);
            Ui.Texto(titulos, "Hola, " + App.PerfilActivo.nombreEstudiante, EstiloTexto.Titulo);
            Ui.Boton(cabecera, "Diario de campo", () => App.Router.Apilar<PantallaDelDiario>());
            Ui.Boton(cabecera, "Cambiar de perfil", () => App.Router.IrA<PantallaDePerfiles>(), VarianteBoton.Fantasma);

            var cuerpo = Ui.Fila(marco, "Cuerpo", Tema.margen, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);

            var guardadas = Ui.PanelColumna(cuerpo, "Partidas", Tema.margen, Tema.espacio);
            UiKit.Tamano(guardadas, flexAncho: 1, flexAlto: 1);
            Ui.Texto(guardadas, "TUS PARTIDAS", EstiloTexto.Pequeno, Tema.cian);
            var scroll = Ui.Desplazable(guardadas, out _lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            ConstruirNueva(cuerpo);
        }

        private void ConstruirNueva(Transform padre) {
            var nueva = Ui.Tarjeta(padre, "Partida nueva");
            UiKit.Tamano(nueva, ancho: 600);
            Ui.Texto(nueva, "Empieza en el Nivel 0: la entrevista de admisión y el concurso. Después, Cradle Lifts.", EstiloTexto.Pequeno);
            var n = App.Partidas.Listar(App.PerfilActivo.idPerfil).Count + 1;
            _nombre = Ui.CampoDeTexto(nueva, "Nombre de la partida", "Partida " + n, 540);
            _nombre.characterLimit = 40;

            Ui.Espaciador(nueva, 4);
            _botonAula = Ui.BotonDeOpcion(nueva, "Modo aula: no",
                "Con el modo aula, toda la clase escribe la misma semilla y juega exactamente la misma partida: los mismos eventos, el mismo día, a la misma hora.",
                AlternarAula);
            var fila = Ui.Fila(nueva);
            _filaSemilla = fila.gameObject;
            Ui.Texto(fila, "Semilla", EstiloTexto.Cuerpo);
            _semilla = Ui.CampoDeTexto(fila, "p. ej. 2026", "", 220);
            _semilla.contentType = TMP_InputField.ContentType.IntegerNumber;
            _filaSemilla.SetActive(false);

            _aviso = Ui.Texto(nueva, "", EstiloTexto.Pequeno, Tema.mostaza);
            Ui.Boton(Ui.Fila(nueva), "Empezar", Empezar, VarianteBoton.Primario);
        }

        private void AlternarAula() {
            _modoAula = !_modoAula;
            _filaSemilla.SetActive(_modoAula);
            _botonAula.GetComponentInChildren<TMP_Text>().text = _modoAula ? "Modo aula: sí" : "Modo aula: no";
            Ui.Resaltar(_botonAula, _modoAula);
        }

        public override void Repintar() {
            UiKit.Vaciar(_lista);
            var partidas = App.Partidas.Listar(App.PerfilActivo.idPerfil);
            if (partidas.Count == 0) {
                Ui.Texto(_lista, "Todavía no tienes partidas. Empieza una a la derecha.", EstiloTexto.Cuerpo, Tema.textoTenue);
                return;
            }
            foreach (var r in partidas) Fila(r);
        }

        private void Fila(ResumenDePartida r) {
            var fila = Ui.PanelColumna(_lista, "Partida", Tema.espacio, 6, Tema.pared);
            var linea = Ui.Fila(fila);
            var datos = Ui.Columna(linea, espacio: 2);
            UiKit.Tamano(datos, flexAncho: 1);
            Ui.Texto(datos, r.Nombre, EstiloTexto.Subtitulo, r.Danada ? Tema.rojo : Tema.texto);
            if (r.Danada) {
                Ui.Texto(datos, "No se puede abrir: " + r.Error, EstiloTexto.Pequeno, Tema.rojo);
            } else {
                var nivel = r.NivelActualId != null && App.Catalogo.Niveles.ContainsKey(r.NivelActualId)
                    ? App.Catalogo.Niveles[r.NivelActualId].Nombre : r.NivelActualId;
                Ui.Texto(datos, $"{nivel} · {(r.EntreNiveles ? "al empezar el nivel" : "nivel en curso")} · guardada el " +
                                PantallaDePerfiles.Fecha(r.FechaUltimoGuardado), EstiloTexto.Pequeno);
                var id = r.Id;
                Ui.Boton(linea, "Continuar", () => Abrir(id), VarianteBoton.Primario);
            }
            var borrar = r;
            Ui.Boton(linea, "Borrar", () => App.Router.Apilar<PantallaDeConfirmacion>(c => {
                c.Titulo = $"¿Borrar «{borrar.Nombre}»?";
                c.Texto = "La partida desaparece. Lo que hayas encontrado para el diario se queda en tu perfil.";
                c.TextoSi = "Borrar";
                c.AlConfirmar = () => { App.Partidas.Borrar(App.PerfilActivo.idPerfil, borrar.Id); Repintar(); };
            }), VarianteBoton.Fantasma);
        }

        private void Empezar() {
            int? semilla = null;
            if (_modoAula) {
                int s;
                if (!int.TryParse(_semilla.text, out s) || s <= 0) { _aviso.text = "El modo aula necesita una semilla: un número mayor que 0."; return; }
                semilla = s;
            }
            var nombre = string.IsNullOrWhiteSpace(_nombre.text) ? "Partida" : _nombre.text.Trim();
            var partida = App.CrearPartida(nombre, semilla, _modoAula);
            App.AbrirPartida(partida);
            FlujoDelNivel.Continuar(App);
        }

        private void Abrir(string partidaId) {
            try {
                var partida = App.Partidas.Cargar(App.PerfilActivo.idPerfil, partidaId);
                App.AbrirPartida(partida);
                FlujoDelNivel.Continuar(App);
            } catch (Exception ex) {
                Debug.LogException(ex);
                _aviso.text = "No se pudo abrir la partida: " + ex.Message;
            }
        }
    }
}
