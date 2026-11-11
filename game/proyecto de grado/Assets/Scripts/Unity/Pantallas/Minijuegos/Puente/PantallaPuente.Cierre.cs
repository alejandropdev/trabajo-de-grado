using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Puente;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    public sealed partial class PantallaPuente {
        // ==================================================================== el cierre

        /// <summary>
        /// El cierre: la misma escena, ya resuelta (los pilares hechos con su losa encendida, el que cayo en el suelo,
        /// el camion donde se quedo), con globos encima: el veredicto y una linea por pilar arriba a la izquierda, y
        /// «¿Qué aprendiste?», reintentar (en practica) y volver arriba a la derecha. Sin scroll.
        /// </summary>
        protected override bool PintarCierrePropio(RectTransform cuerpo) {
            if (_st == null) _st = new EstadoDelPuente(Def.Puente);
            AsegurarAnimador();
            if (_vuelos != null) { Destroy(_vuelos.gameObject); _vuelos = null; }
            _tutorial = null; _globo = null; _contador = null; _verificar = null; _panelVerificar = null; _botonDePista = null; _botonColocar = null;
            _lam = null; _resalte = null;
            TutorialAbierto = false; _bloqueado = true; _verificando = false;

            var clave = Resultado.Resultado;
            var bien = clave == ResultadosDeMinijuego.Todos;
            var tono = bien ? Tema.success : clave == ResultadosDeMinijuego.Parcial ? Tema.warning : Tema.danger;

            var area = Ui.Nodo(cuerpo, "Puente");
            UiKit.Tamano(area, flexAncho: 1, flexAlto: 1);
            ConstruirEscena(area, true, EscenaResuelta);

            // ---- arriba a la izquierda: el veredicto, las cifras y una linea por pilar
            var veredicto = Globo(_capa, "Veredicto", false, NexusTheme.Alfa(tono, 0.8f));
            Anclar(veredicto, new Vector2(0, 1), new Vector2(16, -14));
            veredicto.sizeDelta = new Vector2(700, 10);
            CrecerConElContenido(veredicto, false);
            Titular(veredicto, Guiado ? "RESULTADO · CON LA AYUDA DE MARISOL" : "RESULTADO", TituloDelResultado(clave, Def.Verbo), tono);
            var cifras = Ui.Fila(veredicto, "Cifras", Tema.espacio * 0.75f, alineacion: TextAnchor.MiddleLeft);
            var hechos = _st.Hecho.Count(h => h);
            Ui.Badge(cifras, hechos + " de " + _st.Cfg.Pilares.Count + " pilares", hechos == _st.Cfg.Pilares.Count ? Tono.Exito : Tono.Aviso);
            var perdidas = _st.VidasPerdidas;
            Ui.Badge(cifras, perdidas == 0 ? "ninguna vida perdida" : perdidas == 1 ? "1 vida perdida" : perdidas + " vidas perdidas", perdidas == 0 ? Tono.Exito : Tono.Peligro);
            foreach (var linea in Resultado.Detalle.Take(4)) {
                var t = Ui.Texto(veredicto, linea, EstiloTexto.Pequeno, Tema.ink);
                t.textWrappingMode = TMPro.TextWrappingModes.Normal;
            }
            Entrar(veredicto, 0f);

            // ---- arriba a la derecha: los botones
            var acciones = Globo(_capa, "Acciones", true);
            Anclar(acciones, new Vector2(1, 1), new Vector2(-16, -14));
            CrecerConElContenido(acciones, true);
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
            foreach (var pv in _pilares) {
                var i = pv.Indice;
                // Los corazones que se perdieron, ya partidos.
                var perdidos = _st.VidasIniciales - _st.Vidas[i];
                for (var k = 0; k < perdidos && k < pv.Corazones.Count; k++) {
                    var c = pv.Corazones[pv.Corazones.Count - 1 - k];
                    c.Perdido = true;
                    c.Izq.color = new Color(1, 1, 1, 0); c.Der.color = new Color(1, 1, 1, 0);
                }
                foreach (var b in pv.Bloques.Values) b.PuedeArrastrar = false;
                if (_st.Caido[i]) {
                    pv.Caido = true;
                    foreach (var b in pv.Bloques.Values) { var g = b.gameObject.AddComponent<CanvasGroup>(); g.alpha = 0; }
                    PintarLosaCaida(pv);
                } else if (_st.Hecho[i]) {
                    pv.Listo = true;
                    foreach (var b in pv.Bloques.Values) b.Poner(VistaDeBloque.Hecho, true);
                    PintarLosaEncendida(pv);
                } else {
                    var rev = _st.UltimaRevision[i];
                    if (rev != null && !rev.Correcto && rev.Orden.SequenceEqual(_st.Ordenes[i])) {
                        for (var j = 0; j < rev.Orden.Count; j++) {
                            var b = pv.Bloques[rev.Orden[j]];
                            b.Poner(rev.Estados[j] == EstadoDeBloque.Bien ? VistaDeBloque.Bien : rev.Estados[j] == EstadoDeBloque.Mal ? VistaDeBloque.Mal : VistaDeBloque.Apagado, true);
                            if (rev.Estados[j] == EstadoDeBloque.Mal) { b.DibujarGrietas(); b.Grietas.Avance = 1f; }
                        }
                    }
                }
            }
            _necesidadesVisibles = true;
            PintarNecesidades();
            // El camion, donde se quedo.
            if (_st.Completo) {
                _camion.anchoredPosition = new Vector2(_w - _cliffW + 14f, _camion.anchoredPosition.y);
                if (_textoDelCamion != null) _textoDelCamion.text = "¡GRACIAS!";
            } else if (_st.Perdido) {
                var idx = System.Array.FindIndex(_st.Caido, c => c);
                _camion.anchoredPosition = new Vector2(Mathf.Max(_camion.anchoredPosition.x, _cliffW + idx * _slabW - 150f), _camion.anchoredPosition.y);
                if (_textoDelCamion != null) _textoDelCamion.text = "¿Y AHORA?";
            }
        }

        private void PintarLosaEncendida(PilarVista pv) {
            pv.LosaFondo.color = Color.Lerp(new Color(0.05f, 0.1f, 0.12f), pv.Color, 0.42f);
            pv.LosaBorde.color = pv.Color;
            pv.LosaTexto.color = Color.white;
            pv.LosaLuz.color = NexusTheme.Alfa(pv.Color, 0.14f);
            pv.Lampara.color = Tema.success;
            pv.Sello.localScale = Vector3.one;
            pv.Sello.localEulerAngles = new Vector3(0, 0, -4f);
        }

        private void PintarLosaCaida(PilarVista pv) {
            var rojo = NexusTheme.Alfa(Color.Lerp(Tema.danger, Color.white, 0.3f), 0.95f);
            var w = _slabW - 6;
            pv.LosaFondo.color = new Color(0.2f, 0.06f, 0.07f, 0.97f);
            pv.LosaBorde.color = Tema.danger;
            pv.Lampara.color = Tema.danger;
            pv.LosaLuz.color = NexusTheme.Alfa(Tema.danger, 0.25f);
            pv.GrietasDeLaLosa.Limpiar();
            pv.GrietasDeLaLosa.Polilinea(new List<Vector2> { new Vector2(w * 0.42f, 0), new Vector2(w * 0.47f, AltoLosa * 0.3f), new Vector2(w * 0.4f, AltoLosa * 0.55f), new Vector2(w * 0.5f, AltoLosa * 0.8f), new Vector2(w * 0.46f, AltoLosa - 4) }, rojo, 3.4f);
            pv.Losa.anchoredPosition = new Vector2(pv.Losa.anchoredPosition.x, pv.Losa.anchoredPosition.y - 16f);
            pv.Losa.localEulerAngles = new Vector3(0, 0, -3.2f);
        }

        /// <summary>Lo que no cabe en el cierre: la razon, lo que significa para el cliente y, guiado, lo que mirar la proxima vez.</summary>
        private void AbrirAprendizaje() {
            App.Router.Apilar<PantallaDeFicha>(p => {
                p.Titulo = "¿Qué aprendiste?";
                p.Pintar = h => {
                    var razon = Resultado != null && Resultado.Rubrica != null ? Resultado.Rubrica.Razon : null;
                    if (!string.IsNullOrEmpty(razon)) { h.Etiqueta("Lo que importa"); h.Parrafo(razon, Tema.ink); }
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
