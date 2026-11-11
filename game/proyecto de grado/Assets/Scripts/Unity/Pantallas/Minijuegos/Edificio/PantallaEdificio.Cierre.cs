using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    public sealed partial class PantallaEdificio {
        // ==================================================================== el cierre

        private Button _botonVerOleada;
        private TMP_Text _leccionTexto;

        /// <summary>
        /// El cierre: el mismo edificio, ya resuelto (en cada piso, lo que atrapo en su color y lo que se colo en rojo; el
        /// cliente con su animo), con globos encima: el veredicto, una linea por piso y la leccion del piso por el que se
        /// pasa, arriba a la izquierda; y «Ver la oleada», «¿Qué aprendiste?», reintentar (en practica) y volver, arriba a la
        /// derecha. Sin scroll.
        /// </summary>
        protected override bool PintarCierrePropio(RectTransform cuerpo) {
            AsegurarAnimador();
            if (_vuelos != null) { Destroy(_vuelos.gameObject); _vuelos = null; }
            _ola++;
            _tutorial = null; _globo = null; _contador = null; _soltar = null; _botonDePista = null; _botonSaltar = null; _lam = null; _leccionTexto = null;
            TutorialAbierto = false; _bloqueado = true; _soltando = false; _repitiendo = false;

            var clave = Resultado.Resultado;
            var bien = clave == ResultadosDeMinijuego.Todos;
            var tono = bien ? Tema.success : clave == ResultadosDeMinijuego.Parcial ? Tema.warning : Tema.danger;
            var calculo = RepartirEvaluador.Calcular(Cfg, _asignacion);
            var encontrados = calculo.Sum(r => r.Encontrados);
            var total = calculo.Sum(r => r.Encontrados + r.Escapan);
            var colados = total - encontrados;

            var area = Ui.Nodo(cuerpo, "Edificio");
            UiKit.Tamano(area, flexAncho: 1, flexAlto: 1);
            ConstruirEscena(area, true, EscenaResuelta);

            // ---- arriba a la izquierda: el veredicto, las cifras, lo que dice el reto y la leccion del piso
            var veredicto = Globo(_capa, "Veredicto", false, NexusTheme.Alfa(tono, 0.8f));
            Anclar(veredicto, new Vector2(0, 1), new Vector2(16, -14));
            veredicto.sizeDelta = new Vector2(700, 10);
            CrecerConElContenido(veredicto, false);
            Titular(veredicto, Guiado ? "RESULTADO · CON LA AYUDA DE MARISOL" : "RESULTADO", TituloDelResultado(clave, Def.Verbo), tono);
            var cifras = Ui.Fila(veredicto, "Cifras", Tema.espacio * 0.75f, alineacion: TextAnchor.MiddleLeft);
            Ui.Badge(cifras, $"{encontrados} de {total} frenados", colados == 0 ? Tono.Exito : Tono.Aviso);
            Ui.Badge(cifras, colados == 0 ? "ninguno llegó al cliente" : colados == 1 ? "llegó 1 al cliente" : $"llegaron {colados} al cliente", colados == 0 ? Tono.Exito : Tono.Peligro);
            foreach (var linea in Resultado.Detalle.Take(3)) {
                var t = Ui.Texto(veredicto, linea, EstiloTexto.Pequeno, Tema.ink);
                t.textWrappingMode = TextWrappingModes.Normal;
            }
            _leccionTexto = Ui.Texto(veredicto, "Pasa por un piso: qué frena, qué no ve y qué se coló.", EstiloTexto.Pequeno, Tema.cyan);
            _leccionTexto.textWrappingMode = TextWrappingModes.Normal;
            Entrar(veredicto, 0f);

            // ---- arriba a la derecha: los botones
            var acciones = Globo(_capa, "Acciones", true);
            Anclar(acciones, new Vector2(1, 1), new Vector2(-16, -14));
            CrecerConElContenido(acciones, true);
            _botonVerOleada = Ui.Boton(acciones, "Ver la oleada  ▶", () => { if (!_soltando && _lam != null) LanzarLaOleada(true); }, VarianteBoton.Fantasma);
            _botonSaltar = Ui.Boton(acciones, "Saltar  ▶▶", SaltarLaOleada, VarianteBoton.Fantasma);
            _botonSaltar.gameObject.SetActive(false);
            Ui.Boton(acciones, "¿Qué aprendiste?  ›", AbrirAprendizaje, VarianteBoton.Secundario);
            if (PuedeReintentar) Ui.Boton(acciones, "Intentarlo otra vez", ReintentarPractica, VarianteBoton.Secundario);
            var volver = Ui.Boton(acciones, "Volver a la jornada", VolverALaJornada, VarianteBoton.Primario);
            UiKit.Tamano(volver, alto: 64);
            Entrar(acciones, 0.25f);
            return true;
        }

        /// <summary>La escena en su estado final, sin animaciones: lo que se ve al cerrar el reto.</summary>
        private void EscenaResuelta() {
            Entrar(_lam.Raiz, 0f);
            var calculo = RepartirEvaluador.Calcular(Cfg, _asignacion);
            foreach (var pv in _vistas.Values) {
                pv.Resultado = calculo.First(r => r.DepositoId == pv.Dep.Id);
                pv.Horas.text = _asignacion[pv.Dep.Id] + " h";
                pv.Menos.gameObject.SetActive(false);
                pv.Mas.gameObject.SetActive(false);
                PintarPiso(pv);
            }
            _bolsa.gameObject.SetActive(false);
            var total = Mathf.Max(1, calculo.Sum(r => r.Encontrados + r.Escapan));
            var colados = calculo.Sum(r => r.Escapan);
            _animo = 1f - colados / (float)total;
            _alertas = Mathf.Min(colados, 5);
            PintarCliente();
        }

        /// <summary>Lo que no cabe en el cierre: la razon, lo que significa para el cliente y, guiado, lo que mirar la proxima vez.</summary>
        private void AbrirAprendizaje() {
            App.Router.Apilar<PantallaDeFicha>(p => {
                p.Titulo = "¿Qué aprendiste?";
                p.Pintar = h => {
                    var razon = Resultado != null && Resultado.Rubrica != null ? Resultado.Rubrica.Razon : null;
                    if (!string.IsNullOrEmpty(razon)) { h.Etiqueta("Lo que importa"); h.Parrafo(razon, Tema.ink); }
                    h.Etiqueta("Qué frena cada piso");
                    foreach (var d in TiposDePrueba.Pisos(Cfg).AsEnumerable().Reverse()) {
                        var t = TiposDePrueba.Para(d);
                        h.Nota("· " + t.Nombre + ": revisa " + t.Revisa + "; frena " + t.Incidencia + ". No ve " + t.NoVe + ".", Tema.cyan);
                    }
                    if (Guiado) {
                        h.Etiqueta("La próxima vez, mira esto");
                        foreach (var leccion in RecorridoGuiado.Lecciones(Guia.Pasos).Take(3)) h.Nota("· " + leccion);
                    }
                    var solucion = SolucionEnTexto().ToList();
                    if (solucion.Count > 0 && Resultado != null && Resultado.Resultado != ResultadosDeMinijuego.Todos && !Guiado) {
                        h.Etiqueta("Cómo se podía hacer");
                        foreach (var linea in solucion) h.Nota("· " + linea, Tema.cyan);
                    }
                    var cliente = QueSignificaParaElCliente();
                    if (cliente != null) { h.Etiqueta("Para el cliente"); h.Nota(cliente); }
                    h.Nota(Practica ? "Era práctica: lo que ganes va al proyecto, pero no cuenta para tu evaluación."
                                    : "Esto queda en tu evaluación. El resumen de todo, en Lecciones al cerrar el nivel.");
                };
            });
        }
    }
}
