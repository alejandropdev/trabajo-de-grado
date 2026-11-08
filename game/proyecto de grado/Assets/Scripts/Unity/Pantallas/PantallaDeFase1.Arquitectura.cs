using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La Arquitectura de la Fase 1 en DOS partes (2026-11-08), como las dos primeras de la Metodologia y sin scroll:
    ///   0 · Elegir     las formas como cartas (cada una con su dibujo) y, debajo, una animacion de 5 s por forma:
    ///                  una peticion que cruza el sistema. Al elegir, pasa sola a la parte 1.
    ///   1 · ¿Por qué?  la carta elegida (al pasar el raton se voltea: «Cambiar arquitectura»), las razones de ESA
    ///                  forma en dos columnas y, debajo, la animacion detallada con los modulos de este proyecto:
    ///                  cómo es · una petición · al desplegar · el día a día.
    /// </summary>
    public sealed partial class PantallaDeFase1 {
        private const int PartesDeArquitectura = 2;

        private void Arquitectura() {
            var fase1 = Perfil.Fase1;
            var elegida = fase1.Arquitecturas.FirstOrDefault(a => a.Id == _arquitectura);
            if (elegida == null) _subPaso = 0;
            if (_subPaso == 0) ArquitecturaElegir(fase1.Arquitecturas);
            else ArquitecturaRazon(elegida);
        }

        private void CabeceraDeArquitectura(Hoja hoja, string titulo, System.Action<RectTransform> derecha) {
            var fila = hoja.Fila();
            var textos = Ui.Columna(fila, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(textos, flexAncho: 1);
            var etiqueta = Ui.Texto(textos, "Decisión 3 de 3 · Arquitectura · " + (_subPaso + 1) + " de " + PartesDeArquitectura,
                                    EstiloTexto.Leyenda, Tema.cyan);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Texto(textos, titulo, EstiloTexto.Titulo);
            if (derecha != null) derecha(fila);
        }

        private void ArquitecturaElegir(IList<ArquitecturaOpcion> opciones) {
            var hoja = LienzoSinScroll("Arquitectura · elegir");
            CabeceraDeArquitectura(hoja, "¿Qué forma va a tener lo que construyes?", fila =>
                Ui.Boton(fila, "Releer el encargo", () => DialogosDelExpediente.Encargo(App, Perfil, S.PistasEncontradas()), VarianteBoton.Fantasma));

            var rotulo = hoja.Fila();
            Ui.Texto(rotulo, "Arquitecturas", EstiloTexto.Subtitulo);
            UiKit.Tamano(Ui.Texto(rotulo, "La respuesta está en el encargo. Pasa el ratón por una para verla abajo; púlsala para elegirla.",
                                  EstiloTexto.Pequeno), flexAncho: 1);

            var filaCartas = Ui.Fila(hoja.Raiz, "Arquitecturas", Tema.Espacio(6), 0, TextAnchor.MiddleCenter);
            filaCartas.gameObject.AddComponent<Nexus.Unity.Aplicacion.ListaDeElementos>();
            GuiaView.Registrar("fase1.arquitecturas", filaCartas);

            var escena = Ui.Nodo(hoja.Raiz, "Explicacion general");
            var le = UiKit.Tamano(escena, flexAncho: 1, flexAlto: 1);
            le.minHeight = Tema.Px(150); le.preferredHeight = Tema.Px(200);
            var ficha = Perfil.Proyecto;
            var animacion = EscenaDeMetodologia.Crear(Ui, escena, opciones.Select(a => DibujosDeArquitecturaAnimados.ResumenAnimado(a, ficha)).ToList(),
                                                      "Las arquitecturas", DibujosDeArquitecturaAnimados.Ancho, DibujosDeArquitecturaAnimados.Alto);
            UiKit.Rellenar((RectTransform)animacion.transform);

            var cartas = new List<CartaDeMetodologia>();
            for (var i = 0; i < opciones.Count; i++) {
                var arq = opciones[i];
                var indice = i;
                CartaDeMetodologia carta = null;
                carta = CartaDeMetodologia.Crear(Ui, filaCartas, arq.Nombre, p => DibujosDeArquitecturaAnimados.Icono(p, arq, ficha), false, () => {
                    if (_arquitectura != arq.Id) _razonArquitectura = null;   // otra forma, otras razones
                    _arquitectura = arq.Id;
                    foreach (var c in cartas) { c.Elegida = false; c.Apagada = c != carta; }
                    carta.Sellar();
                    GuardarBorrador();
                    PintarNavegacion();
                    AvanzarDespues(0.8f, 1);
                }, 0.07f * i, "Cambiar arquitectura");
                UiKit.Tamano(carta, ancho: Tema.Px(220), alto: Tema.Px(150));
                carta.AlEnfocar = encima => { if (encima) animacion.Saltar(indice); };
                if (_arquitectura == arq.Id) carta.Elegida = true;
                else if (_arquitectura != null) carta.Apagada = true;
                cartas.Add(carta);
            }
        }

        private void ArquitecturaRazon(ArquitecturaOpcion elegida) {
            var hoja = LienzoSinScroll("Arquitectura · por que");
            var queEs = Nexus.Core.Narrativa.Glosario.Buscar(App.Catalogo.Glosario, elegida.Id);
            var id = elegida.Id;
            CabeceraDeArquitectura(hoja, "¿Por qué " + elegida.Nombre + "?", fila => {
                if (queEs != null) Ui.Boton(fila, "¿Qué es?", () => PantallaDePizarra.AbrirConcepto(App, queEs), VarianteBoton.Fantasma);
                // Como quedaria ESTE sistema con esa forma: el plano grande, con los modulos del proyecto.
                if (Perfil.Proyecto != null) Ui.Boton(fila, "Ver el plano", () => PantallaDelProducto.AbrirPlano(App, id, false), VarianteBoton.Fantasma);
            });

            var medio = Ui.Fila(hoja.Raiz, "Eleccion y razones", Tema.Espacio(6), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(medio, flexAncho: 1);
            var ficha = Perfil.Proyecto;
            var carta = CartaDeMetodologia.Crear(Ui, medio, elegida.Nombre, p => DibujosDeArquitecturaAnimados.Icono(p, elegida, ficha), true, () => {
                _arquitectura = null; _razonArquitectura = null;
                IrAParte(0);
            }, 0, "Cambiar arquitectura");
            carta.Elegida = true;
            UiKit.Tamano(carta, ancho: Tema.Px(210), alto: Tema.Px(205));

            var derecha = Ui.Columna(medio, "Razones", Tema.Espacio(2));
            UiKit.Tamano(derecha, flexAncho: 1);
            // Cada forma trae SUS razones (feedback #8): elegir app o web no puede ofrecer los mismos argumentos.
            var razones = Perfil.Fase1.RazonesDe(elegida).Select(r => new KeyValuePair<string, string>(r.Id, r.Texto)).ToList();
            RejillaDeRazones(derecha, razones, () => _razonArquitectura, r => {
                _razonArquitectura = r;
                GuardarBorrador();
                PintarNavegacion();
            });

            var escena = Ui.Nodo(hoja.Raiz, "Explicacion detallada");
            var le = UiKit.Tamano(escena, flexAncho: 1, flexAlto: 1);
            le.minHeight = Tema.Px(150); le.preferredHeight = Tema.Px(190);
            var animacion = EscenaDeMetodologia.Crear(Ui, escena, DibujosDeArquitecturaAnimados.Detalle(elegida, ficha), "La arquitectura en detalle",
                                                      DibujosDeArquitecturaAnimados.Ancho, DibujosDeArquitecturaAnimados.Alto);
            UiKit.Rellenar((RectTransform)animacion.transform);
        }
    }
}
