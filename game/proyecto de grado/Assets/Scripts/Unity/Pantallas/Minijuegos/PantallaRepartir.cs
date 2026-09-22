using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// V2 · Repartir. Un presupuesto que no alcanza para todo (las horas de pruebas) entre varios depositos.
    /// Cada tipo de prueba solo encuentra los defectos de su clase: lo que no se busca donde esta, escapa, y
    /// escapa exactamente del tipo en el que no se invirtio. Cuantos defectos hay de cada tipo no se ve; esa es
    /// la decision.
    /// </summary>
    public sealed class PantallaRepartir : PantallaDeMinijuego {
        private readonly Dictionary<string, int> _asignacion = new Dictionary<string, int>();
        private RectTransform _depositos;
        private TMP_Text _restante;

        private RepartirCfg Cfg { get { return Def.Repartir; } }
        private int Paso { get { return Cfg.Presupuesto >= 20 ? 2 : 1; } }
        private int Usado { get { return _asignacion.Values.Sum(); } }

        protected override void ConstruirJuego(RectTransform cuerpo) {
            foreach (var d in Cfg.Depositos) _asignacion[d.Id] = 0;

            var panel = Ui.PanelColumna(cuerpo, "Reparto", Tema.margen, Tema.espacio);
            UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            _restante = Ui.Texto(panel, "", EstiloTexto.Subtitulo);
            var scroll = Ui.Desplazable(panel, out _depositos);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            Ui.Boton(Ui.Fila(panel), "Entregar el reparto", Entregar, VarianteBoton.Primario);
            Repintar();
        }

        public override void Repintar() {
            if (_depositos == null) return;
            var libres = Cfg.Presupuesto - Usado;
            _restante.text = $"{libres} de {Cfg.Presupuesto} {Cfg.Unidad} sin repartir";
            _restante.color = libres == 0 ? Tema.cian : Tema.amarillo;

            UiKit.Vaciar(_depositos);
            foreach (var deposito in Cfg.Depositos) {
                var d = deposito;
                var tarjeta = Ui.PanelColumna(_depositos, d.Id, Tema.espacio, 4, Tema.pared);
                var fila = Ui.Fila(tarjeta);
                var textos = Ui.Columna(fila, espacio: 2);
                UiKit.Tamano(textos, flexAncho: 1);
                Ui.Texto(textos, d.Nombre, EstiloTexto.Cuerpo).fontStyle = FontStyles.Bold;
                if (!string.IsNullOrEmpty(d.Descripcion)) Ui.Texto(textos, d.Descripcion, EstiloTexto.Pequeno);

                var menos = Ui.Boton(fila, "−" + Paso, () => Sumar(d.Id, -Paso));
                UiKit.Tamano(menos, ancho: 80);
                menos.interactable = _asignacion[d.Id] > 0;
                UiKit.Tamano(Ui.Texto(fila, $"{_asignacion[d.Id]} {Cfg.Unidad}", EstiloTexto.Subtitulo,
                                      _asignacion[d.Id] > 0 ? Tema.cian : Tema.textoTenue, TextAlignmentOptions.Center), ancho: 160);
                var mas = Ui.Boton(fila, "+" + Paso, () => Sumar(d.Id, Paso));
                UiKit.Tamano(mas, ancho: 80);
                mas.interactable = libres > 0;
                Ui.Barra(tarjeta, Cfg.Presupuesto > 0 ? (float)_asignacion[d.Id] / Cfg.Presupuesto : 0);
            }
        }

        private void Sumar(string id, int delta) {
            var libres = Cfg.Presupuesto - Usado;
            var nuevo = Mathf.Clamp(_asignacion[id] + Mathf.Min(delta, libres), 0, Cfg.Presupuesto);
            _asignacion[id] = nuevo;
            Repintar();
        }

        protected override ResultadoMinijuego Evaluar() {
            return RepartirEvaluador.Evaluar(Def, _asignacion);
        }
    }
}
