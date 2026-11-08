using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Sesion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Interactuar» con una persona del equipo (2026-11-06): en vez del panel de botones de texto, una escena limpia.
    /// A la izquierda, su carta en grande (foto y atributos) con un bocadillo para lo que dice; a la derecha, lo que
    /// puedes hacer con ella como una mano de cartas (hablar, darle una tarjeta, en pareja, un respiro). Darle una
    /// tarjeta o ponerla en pareja reparte una segunda mano para elegir cual o con quien.
    /// </summary>
    public sealed partial class PantallaDelDia {
        private RectTransform _capaInteraccion, _manoInteraccion;
        private TMP_Text _bocadillo;
        private FichaDeMiembro _conQuien;

        private void AbrirInteraccion(FichaDeMiembro ficha) {
            if (_capaInteraccion != null) Destroy(_capaInteraccion.gameObject);
            _conQuien = ficha;
            _capaInteraccion = Ui.Nodo(_escenario, "Interactuar");
            UiKit.Rellenar(_capaInteraccion);
            var velo = _capaInteraccion.gameObject.AddComponent<Image>();
            velo.color = NexusTheme.Alfa(Tema.bg950, 0.82f);
            _capaInteraccion.gameObject.AddComponent<EntradaDeTira>();
            // Salir siempre es facil (2026-11-06: no habia forma de irse): clic fuera de la carta y de la mano, Escape,
            // o el boton «Volver» arriba a la izquierda, a la vista.
            var salida = _capaInteraccion.gameObject.AddComponent<SalidaDeInteraccion>();
            salida.Cerrar = CerrarInteraccion;
            salida.Puede = () => !_bandejaAbierta;
            Runner.Mirando = true;

            var fila = Ui.Fila(_capaInteraccion, "Escena", Tema.margen, Tema.margen, TextAnchor.MiddleCenter);
            UiKit.Rellenar(fila);

            // ---- su carta, en grande, con el bocadillo encima
            var izquierda = Ui.Columna(fila, "Persona", Tema.espacio, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(izquierda, ancho: 400);
            var globo = Ui.PanelColumna(izquierda, "Bocadillo", Tema.Espacio(4), 0, Color.white);
            _bocadillo = Ui.Texto(globo, "¿Qué necesitas?", EstiloTexto.Dialogo, new Color32(0x26, 0x26, 0x2A, 0xFF), TextAlignmentOptions.Center);
            var carta = Ui.PanelColumna(izquierda, "Carta", Tema.Espacio(5), Tema.Espacio(2), Tema.surfaceRaised);
            Ui.Halo(carta, NexusTheme.Alfa(Tema.cyan, 0.45f), NexusTheme.RadioLg);
            Ui.Borde(carta, Tema.cyan, NexusTheme.RadioLg, 1.5f);
            var m = ficha.Miembro;
            Rostro(carta, FotoDe(ficha, m.Nombre), m.Nombre, 220);
            Ui.Texto(carta, m.Nombre, EstiloTexto.Titulo, Tema.ink, TextAlignmentOptions.Center);
            Ui.Texto(carta, m.Rol, EstiloTexto.Pequeno, Tema.cyan, TextAlignmentOptions.Center);
            BarrasDeAtributos(carta, ficha);

            // ---- lo que puedes hacer
            var derecha = Ui.Columna(fila, "Acciones", Tema.espacio, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(derecha, ancho: 0, flexAncho: 1);
            Ui.Texto(derecha, $"¿Qué haces con {m.Nombre}?", EstiloTexto.Subtitulo, Tema.ink, TextAlignmentOptions.Center);
            _manoInteraccion = Ui.Fila(derecha, "Mano", 0, alineacion: TextAnchor.MiddleCenter);
            Ui.Texto(derecha, "Haz clic fuera o pulsa Esc para volver al equipo", EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Center);
            ManoDeAcciones();

            // «Volver», arriba a la izquierda y fuera del layout: siempre en el mismo sitio, nada lo tapa.
            var volver = Ui.Boton(_capaInteraccion, "Volver (Esc)", CerrarInteraccion, VarianteBoton.Secundario);
            var rv = (RectTransform)volver.transform;
            var le = volver.GetComponent<LayoutElement>();
            if (le == null) le = volver.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            rv.anchorMin = rv.anchorMax = rv.pivot = new Vector2(0, 1);
            rv.sizeDelta = new Vector2(220, 52);
            rv.anchoredPosition = new Vector2(Tema.margen, -Tema.margen);
            volver.transform.SetAsLastSibling();
        }

        /// <summary>Cierra la escena de «Interactuar» con Escape o con un clic en el velo (fuera de la carta y la mano).</summary>
        private sealed class SalidaDeInteraccion : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler {
            public Action Cerrar;
            public Func<bool> Puede;
            public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e) {
                // Solo si el clic cayo en el velo mismo; un clic en un texto de la carta sube hasta aqui y no cuenta.
                if (e.pointerPressRaycast.gameObject == gameObject && (Puede == null || Puede())) Cerrar?.Invoke();
            }
            // Escape lo atiende PantallaDelDia.AlPulsarEscape (cierra esta capa y no abre la pausa en el mismo Escape).
        }

        private void CerrarInteraccion() {
            if (_capaInteraccion != null) Destroy(_capaInteraccion.gameObject);
            _capaInteraccion = null;
            if (_panel == null && !_bandejaAbierta) Runner.Mirando = false;
            _carrusel.Repintar();
        }

        private void ManoDeAcciones() {
            UiKit.Vaciar(_manoInteraccion);
            var id = _conQuien.Miembro.Id;
            var cartas = new List<OpcionDeCarta> {
                new OpcionDeCarta {
                    Titulo = "Hablar", Detalle = "Cómo va de verdad; sube un poco la moral.",
                    Pie = GameSession.MinutosDeUnoAUno + " min", Bloqueo = S.PorQueNoSePuedeHablarUnoAUno(id),
                    AlElegir = () => Hacer(() => { ResultadoDeAvance a; var r = S.HablarUnoAUno(id, out a); _dicho = r.LoQueDice; return a; },
                                           () => _dicho, $"Hablaste con {_conQuien.Miembro.Nombre}.")
                },
                new OpcionDeCarta {
                    Titulo = "Darle una tarjeta", Detalle = "Elige qué trabajo lleva.", Pie = GameSession.MinutosDeAsignar + " min",
                    AlElegir = ManoDeTarjetas
                },
                new OpcionDeCarta {
                    Titulo = "En pareja", Detalle = "Hoy trabaja con alguien: menos errores.", Pie = GameSession.MinutosDePareja + " min",
                    AlElegir = ManoDeCompaneros
                },
                new OpcionDeCarta {
                    Titulo = "Un respiro", Detalle = "Hoy descansa: baja su cansancio.", Pie = GameSession.MinutosDeRespiro + " min",
                    Bloqueo = S.PorQueNoSePuedeDarRespiro(id),
                    AlElegir = () => Hacer(() => S.DarRespiro(id), () => "Gracias. Lo necesitaba.", $"{_conQuien.Miembro.Nombre} tiene hoy un respiro.")
                },
            };
            ManoDeCartas.Crear(Ui, _manoInteraccion, cartas, new ManoDeCartas.Ajustes { Sello = "HECHO", AnchoMaximo = 1100, AnchoDeCarta = 240 });
        }

        private string _dicho;

        private void ManoDeTarjetas() {
            UiKit.Vaciar(_manoInteraccion);
            var id = _conQuien.Miembro.Id;
            var tarjetas = S.Tablero == null ? new List<Nexus.Core.Tablero.TarjetaDeTrabajo>()
                : S.Tablero.Tarjetas.Where(t => !t.Terminada && t.Asignado != id).OrderBy(t => t.Empezada ? 0 : 1).ThenBy(t => t.Orden).Take(4).ToList();
            if (tarjetas.Count == 0) { _bocadillo.text = "No hay nada que pueda coger ahora."; ManoDeAcciones(); return; }
            var cartas = tarjetas.Select(t => {
                var tarjeta = t;
                return new OpcionDeCarta {
                    Titulo = t.Titulo, Pie = $"{t.Puntos:0.#} pts", Bloqueo = S.PorQueNoSePuedeAsignar(t.Id, id),
                    AlElegir = () => Hacer(() => S.AsignarTarjeta(tarjeta.Id, id), () => $"Me pongo con «{tarjeta.Titulo}».",
                                           $"{_conQuien.Miembro.Nombre} lleva ahora «{tarjeta.Titulo}».")
                };
            }).ToList();
            ManoDeCartas.Crear(Ui, _manoInteraccion, cartas, new ManoDeCartas.Ajustes { Sello = "SUYA", AnchoMaximo = 1100, AnchoDeCarta = 240 });
        }

        private void ManoDeCompaneros() {
            UiKit.Vaciar(_manoInteraccion);
            var id = _conQuien.Miembro.Id;
            var otros = S.FichasDelEquipo().Where(f => f.Miembro.Id != id).Take(4).ToList();
            if (otros.Count == 0) { _bocadillo.text = "No hay nadie más en el equipo."; ManoDeAcciones(); return; }
            var cartas = otros.Select(f => {
                var otro = f.Miembro;
                return new OpcionDeCarta {
                    Titulo = otro.Nombre, Detalle = otro.Rol, Pie = GameSession.MinutosDePareja + " min",
                    Bloqueo = S.PorQueNoSePuedePonerEnPareja(id, otro.Id),
                    AlElegir = () => Hacer(() => S.PonerEnPareja(id, otro.Id), () => $"Hoy, con {otro.Nombre}.",
                                           $"{_conQuien.Miembro.Nombre} trabaja hoy en pareja con {otro.Nombre}.")
                };
            }).ToList();
            ManoDeCartas.Crear(Ui, _manoInteraccion, cartas, new ManoDeCartas.Ajustes { Sello = "JUNTOS", AnchoMaximo = 1100, AnchoDeCarta = 240 });
        }

        /// <summary>Hace la accion por el reloj (cobra sus minutos) y la persona contesta en el bocadillo.</summary>
        private void Hacer(Func<ResultadoDeAvance> accion, Func<string> loQueDice, string hecho) {
            try {
                if (!Runner.Actuar(accion)) { _bocadillo.text = "Ahora no: el reloj está parado o hay algo abierto."; ManoDeAcciones(); return; }
                Anotar($"{S.HoraActual} · {hecho}");
                _bocadillo.text = loQueDice() ?? "Hecho.";
            } catch (InvalidOperationException e) {
                _bocadillo.text = e.Message;
            }
            ManoDeAcciones();
        }
    }
}
