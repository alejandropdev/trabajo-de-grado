using System;
using System.Collections.Generic;
using System.Linq;
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
            var titulos = Ui.Columna(cabecera, espacio: Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "NEXUS PROTOCOL", EstiloTexto.Hero);   // el logotipo: una sola vez por pantalla
            Ui.Texto(titulos, "Hola, " + App.PerfilActivo.nombreEstudiante, EstiloTexto.Titulo);
            // Lo que no es jugar (opciones, diario, guia, borrar) espera en un solo menu: la pantalla es para elegir partida.
            // Es una red de nodos (como la pausa): un nodo por cosa, unido al nucleo «Más».
            Ui.Boton(cabecera, "Más", () => App.Router.Apilar<PantallaDeRedDeMenu>(m => {
                m.Titulo = "Más";
                m.Opciones = OpcionesDeLaCabecera();
            }), VarianteBoton.Secundario);

            var cuerpo = Ui.Fila(marco, "Cuerpo", Tema.margen, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);

            var guardadas = Ui.Tarjeta(cuerpo, "Tus partidas");
            UiKit.Tamano(guardadas, flexAncho: 1, flexAlto: 1);
            var scroll = Ui.Desplazable(guardadas, out _lista);
            _lista.gameObject.AddComponent<ListaDeElementos>();   // una fila por partida: contenido, no botones de la pantalla
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            ConstruirNueva(cuerpo);

            // Volver atras va abajo a la izquierda, donde se busca en cualquier juego.
            var pie = Ui.Fila(marco, "Pie");
            Ui.Boton(pie, "◄  Cambiar de perfil", () => App.Router.IrA<PantallaDePerfiles>(), VarianteBoton.Fantasma);
        }

        private List<OpcionDeMenu> OpcionesDeLaCabecera() {
            var perfil = App.PerfilActivo;
            var lista = new List<OpcionDeMenu> { new OpcionDeMenu("Opciones", () => App.Router.Apilar<PantallaDeOpciones>()) };
            // El diario aparece cuando ya hay algo que leer en el: antes de la primera partida solo confunde.
            if (perfil.coleccionablesGlobales != null && perfil.coleccionablesGlobales.Count > 0)
                lista.Add(new OpcionDeMenu("Diario de campo", () => App.Router.Apilar<PantallaDelDiario>()));
            if (perfil.guiaVista != null && perfil.guiaVista.Count > 0)
                lista.Add(new OpcionDeMenu("Volver a ver la guía", () => {
                    // La guia sale una vez por perfil; esto la reinicia para la proxima partida del tutorial.
                    perfil.guiaVista.Clear();
                    App.GuardarPerfil();
                    App.Router.IrA<PantallaDePartidas>();
                }));
            if (perfil.recetasVistas != null && perfil.recetasVistas.Count > 0)
                lista.Add(new OpcionDeMenu("Volver a ver las recetas de los retos", () => {
                    perfil.recetasVistas.Clear();
                    App.GuardarPerfil();
                }));
            var partidas = App.Partidas.Listar(perfil.idPerfil);
            if (partidas.Count > 0)
                lista.Add(new OpcionDeMenu("Borrar una partida…", () => App.Router.Apilar<PantallaDeMenu>(m => {
                    m.Titulo = "¿Cuál borras?";
                    m.Opciones = partidas.Select(r => new OpcionDeMenu(r.Nombre, () => PedirBorrar(r), VarianteBoton.Peligro)).ToList();
                })));
            return lista;
        }

        private void PedirBorrar(ResumenDePartida borrar) {
            App.Router.Apilar<PantallaDeConfirmacion>(c => {
                c.Titulo = $"¿Borrar «{borrar.Nombre}»?";
                c.Texto = "La partida desaparece. Lo que hayas encontrado para el diario se queda en tu perfil.";
                c.TextoSi = "Borrar";
                c.AlConfirmar = () => { App.Partidas.Borrar(App.PerfilActivo.idPerfil, borrar.Id); Repintar(); };
            });
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

            _aviso = Ui.Texto(nueva, "", EstiloTexto.Pequeno, Tema.warning);
            Ui.Boton(Ui.Fila(nueva), "Empezar", Empezar, VarianteBoton.Primario);
        }

        private void AlternarAula() {
            _modoAula = !_modoAula;
            _filaSemilla.SetActive(_modoAula);
            _botonAula.GetComponentInChildren<TMP_Text>().text = _modoAula ? "Modo aula: sí" : "Modo aula: no";
            Ui.Resaltar(_botonAula, _modoAula, Tono.Violeta);   // el Modo Aula es violet en el design system
        }

        public override void Repintar() {
            UiKit.Vaciar(_lista);
            var partidas = App.Partidas.Listar(App.PerfilActivo.idPerfil);
            if (partidas.Count == 0) {
                Ui.Texto(_lista, "Todavía no tienes partidas. Empieza una a la derecha.", EstiloTexto.Cuerpo, Tema.inkMuted);
                return;
            }
            foreach (var r in partidas) Fila(r);
        }

        private void Fila(ResumenDePartida r) {
            var fila = Ui.PanelColumna(_lista, "Partida", Tema.Espacio(4), 6, Tema.surfaceRaised);
            var linea = Ui.Fila(fila);
            var datos = Ui.Columna(linea, espacio: 2);
            UiKit.Tamano(datos, flexAncho: 1);
            Ui.Texto(datos, r.Nombre, EstiloTexto.Encabezado, r.Danada ? Tema.danger : Tema.ink);
            if (r.Danada) {
                Ui.Texto(datos, "No se puede abrir: " + r.Error, EstiloTexto.Pequeno, Tema.danger);
            } else {
                // Donde va: terminada, o «Fase N · Nivel M» con su dia, y una tira con un punto por nivel (2026-11-06).
                var orden = Nexus.Core.Sesion.ProgresionDeNiveles.EnOrden(App.Catalogo);
                var completada = orden.Count > 0 && orden.All(n => r.NivelesCompletados.Contains(n));
                var indice = r.NivelActualId == null ? -1 : orden.IndexOf(r.NivelActualId);
                var nivel = r.NivelActualId != null && App.Catalogo.Niveles.ContainsKey(r.NivelActualId)
                    ? App.Catalogo.Niveles[r.NivelActualId].Nombre : r.NivelActualId;

                var estado = Ui.Fila(datos, "Estado", 8, 0, TextAnchor.MiddleLeft);
                if (completada) {
                    Ui.Chip(estado, "COMPLETADA", Tema.successSoft, Tema.success);
                } else {
                    var fase = r.EntreNiveles || r.Fase == 0 ? "Por empezar" : $"Fase {r.Fase}";
                    Ui.Chip(estado, $"{fase}, Nivel {Math.Max(0, indice)}", Tema.cyanSoft, Tema.cyan);
                }
                for (var i = 0; i < orden.Count; i++) {
                    var hecho = r.NivelesCompletados.Contains(orden[i]);
                    var actual = !completada && i == indice;
                    var color = hecho ? Tema.success : actual ? Tema.cyan : Tema.lineStrong;
                    var punto = Ui.PanelColumna(estado, "Nivel " + i, 4, 0, NexusTheme.Alfa(color, hecho || actual ? 0.2f : 0.08f));
                    Ui.Borde(punto, color, NexusTheme.RadioMd, actual ? 2 : 1);
                    Ui.Texto(punto, "N" + i, EstiloTexto.Leyenda, hecho || actual ? color : Tema.inkFaint, TextAlignmentOptions.Center);
                }

                string donde;
                if (completada) donde = $"{orden.Count} de {orden.Count} niveles terminados";
                else if (r.EntreNiveles || r.Fase == 0) donde = $"{nivel} · al empezar el nivel";
                else if (r.Fase == 1) donde = $"{nivel} · Fase 1: la preparación";
                else if (r.Fase == 2) donde = $"{nivel} · Fase 2: el desarrollo, día {Math.Max(1, r.Dia)}";
                else donde = $"{nivel} · Fase 4: lanzado, falta cerrar el nivel";
                Ui.Texto(datos, $"{donde} · guardada el {PantallaDePerfiles.Fecha(r.FechaUltimoGuardado)}", EstiloTexto.Pequeno);
                var id = r.Id;
                Ui.Boton(linea, completada ? "Abrir" : "Continuar", () => Abrir(id), completada ? VarianteBoton.Secundario : VarianteBoton.Primario);
            }
            // Una partida dañada no se puede continuar: su unico boton es borrarla. Las demas se borran desde «Más».
            if (r.Danada) {
                var borrar = r;
                Ui.Boton(linea, "Borrar", () => PedirBorrar(borrar), VarianteBoton.Fantasma);
            }
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
