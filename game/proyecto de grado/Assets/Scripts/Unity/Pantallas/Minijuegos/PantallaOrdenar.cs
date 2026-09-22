using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// V3 · Ordenar. El backlog: tarjetas con valor y esfuerzo, una capacidad que no alcanza para todas y
    /// dependencias que no se ven hasta que se rompen. De arriba abajo entran las que caben; debajo de la
    /// linea se quedan fuera. Despues, la respuesta al cliente, que lo quiere todo: obedecer, rechazar o
    /// negociar. La respuesta tiene sus propias consecuencias, y negociar es la que abre otra historia.
    /// </summary>
    public sealed class PantallaOrdenar : PantallaDeMinijuego {
        private List<string> _orden;
        private string _respuesta;
        private RectTransform _lista, _respuestas;
        private TMP_Text _resumen;
        private Button _entregar;

        private OrdenarCfg Cfg { get { return Def.Ordenar; } }

        protected override void ConstruirJuego(RectTransform cuerpo) {
            _orden = Cfg.Tarjetas.Select(t => t.Id).ToList();

            var fila = Ui.Fila(cuerpo, "Tablero", Tema.margen * 0.75f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1);

            var backlog = Ui.PanelColumna(fila, "Backlog", Tema.margen, Tema.espacio);
            UiKit.Tamano(backlog, flexAncho: 1, flexAlto: 1);
            Ui.Texto(backlog, $"EL BACKLOG · CAPACIDAD DEL SPRINT: {Cfg.Capacidad} PUNTOS DE ESFUERZO", EstiloTexto.Pequeno, Tema.cian);
            _resumen = Ui.Texto(backlog, "", EstiloTexto.Pequeno, Tema.texto);
            var scroll = Ui.Desplazable(backlog, out _lista);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            var derecha = Ui.Columna(fila, "Cliente", Tema.espacio);
            UiKit.Tamano(derecha, ancho: 560, flexAlto: 1);
            var peticion = Ui.Tarjeta(derecha, "Lo que pide " + (Def.Presentacion.QuienEspera ?? "el cliente"), Tema.mostaza);
            Ui.Texto(peticion, Cfg.Peticion ?? "", EstiloTexto.Cuerpo);
            Ui.Texto(peticion, "¿Qué le contestas?", EstiloTexto.Pequeno, Tema.cianClaro);
            _respuestas = Ui.Columna(peticion, "Respuestas", 6);
            _entregar = Ui.Boton(Ui.Fila(derecha), "Entregar el orden y la respuesta", Entregar, VarianteBoton.Primario);
            Repintar();
        }

        public override void Repintar() {
            if (_orden == null) return;
            var porId = Cfg.Tarjetas.ToDictionary(t => t.Id);
            var entran = OrdenarEvaluador.Entran(Cfg, _orden);
            var esfuerzo = entran.Sum(id => porId[id].Esfuerzo);
            var valor = entran.Sum(id => porId[id].Valor);
            _resumen.text = $"Entran {entran.Count} de {_orden.Count} tarjetas · esfuerzo {esfuerzo} de {Cfg.Capacidad} · valor {valor}";

            UiKit.Vaciar(_lista);
            var lineaPintada = false;
            for (var i = 0; i < _orden.Count; i++) {
                var id = _orden[i];
                if (!lineaPintada && !entran.Contains(id)) {
                    lineaPintada = true;
                    var linea = Ui.Fila(_lista, "Linea");
                    Ui.Texto(linea, "— hasta aquí cabe en el sprint · lo de abajo se queda fuera —", EstiloTexto.Pequeno, Tema.mostaza,
                             TextAlignmentOptions.Center);
                }
                Tarjeta(porId[id], i, entran.Contains(id));
            }

            UiKit.Vaciar(_respuestas);
            foreach (var clave in new[] { "obedecer", "rechazar", "negociar" }) {
                Respuesta r;
                if (!Cfg.Respuestas.TryGetValue(clave, out r)) continue;
                var c = clave;
                var boton = Ui.BotonDeOpcion(_respuestas, r.Texto, Textos.Humanizar(c), () => { _respuesta = c; Repintar(); });
                Ui.Resaltar(boton, _respuesta == c);
            }
            _entregar.interactable = _respuesta != null;
        }

        private void Tarjeta(Tarjeta t, int indice, bool entra) {
            var panel = Ui.PanelColumna(_lista, t.Id, 8, 4, entra ? Tema.pared : Tema.fondo);
            var fila = Ui.Fila(panel);
            var textos = Ui.Columna(fila, espacio: 2);
            UiKit.Tamano(textos, flexAncho: 1);
            var titulo = Ui.Texto(textos, $"{indice + 1}.  {t.Titulo}", EstiloTexto.Cuerpo, entra ? Tema.texto : Tema.textoTenue);
            titulo.fontStyle = FontStyles.Bold;
            Ui.Texto(textos, $"valor {t.Valor} · esfuerzo {t.Esfuerzo}", EstiloTexto.Pequeno);

            var subir = Ui.Boton(fila, "▲", () => Mover(indice, -1));
            UiKit.Tamano(subir, ancho: 64);
            subir.interactable = indice > 0;
            var bajar = Ui.Boton(fila, "▼", () => Mover(indice, +1));
            UiKit.Tamano(bajar, ancho: 64);
            bajar.interactable = indice < _orden.Count - 1;
        }

        private void Mover(int indice, int delta) {
            var destino = indice + delta;
            if (destino < 0 || destino >= _orden.Count) return;
            var id = _orden[indice];
            _orden.RemoveAt(indice);
            _orden.Insert(destino, id);
            Repintar();
        }

        protected override ResultadoMinijuego Evaluar() {
            // Si el reloj acaba sin respuesta al cliente, el evaluador lo da por omitido: no contestar tambien es contestar.
            return OrdenarEvaluador.Evaluar(Def, _orden, _respuesta);
        }
    }
}
