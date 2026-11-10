using System.Linq;
using Nexus.Core.Minijuegos.Corregir;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    public sealed partial class PantallaDeDespliegue {
        // ==================================================================== 0 · que se despliega hoy

        /// <summary>
        /// La introduccion, sin scroll ni parrafos (2026-11-09): tres tarjetas que entran una tras otra (lo que sale hoy, tu
        /// arquitectura y como se juega) y, abajo, el boton de empezar con las palabras del oficio a mano.
        /// </summary>
        private void PintarIntro() {
            _paso = Paso.Intro;
            PonerAntetitulo("antes de empezar");
            PonerIntentos();
            Decir(_plan.EsFinal
                ? "Se acabó el desarrollo. Lo construido tiene que llegar al cliente: eso es desplegar."
                : "Cerraste una etapa. Lo que está listo llega hoy al cliente: eso es desplegar.");

            var fila = Ui.Fila(_escena, "Tarjetas", Tema.espacio * 2, 0, TextAnchor.UpperCenter);
            Anclar(fila, new Vector2(0.5f, 0.5f), new Vector2(0, -30));
            Crecer(fila, true);

            // ---- 1 · lo que sale hoy
            var salen = Tarjeta(fila, "HOY SALEN", Tema.cyan);
            if (_plan.Modulos.Count == 0)
                Ui.Texto(salen, "Ninguno llega todavía al mínimo. Aun así, hay que dejar listo el camino a producción.", EstiloTexto.Cuerpo, Tema.ink);
            foreach (var m in _plan.Modulos) Ui.Chip(salen, NombreDe(m), Tema.cyanSoft, Tema.cyan);
            foreach (var m in _plan.YaDesplegados) Ui.Chip(salen, NombreDe(m) + " · ya en producción", Tema.successSoft, Tema.success);
            foreach (var m in _plan.NoListos) Ui.Chip(salen, NombreDe(m) + " · aún no", Tema.surfaceRaised, Tema.inkMuted);

            // ---- 2 · tu arquitectura
            var arq = App.Sesion.Perfil.Fase1 == null ? null : App.Sesion.Perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == App.Sesion.ArquitecturaElegida);
            var tuya = Tarjeta(fila, "TU ARQUITECTURA", Tema.violet);
            if (arq == null) {
                Ui.Texto(tuya, "Sin arquitectura elegida.", EstiloTexto.Cuerpo, Tema.inkMuted);
            } else {
                Ui.Texto(tuya, arq.Nombre, EstiloTexto.Subtitulo, Tema.ink);
                Ui.Texto(tuya, RevisionDePunto.Corta(DibujosDeArquitectura.AlDesplegar(arq.Id)), EstiloTexto.Cuerpo, Tema.inkMuted);
                Ui.Boton(tuya, "Ver el plano", () => PantallaDelProducto.AbrirPlano(App, arq.Id, true), VarianteBoton.Fantasma);
            }

            // ---- 3 · como se juega
            var como = Tarjeta(fila, "CÓMO SE JUEGA", Tema.warning);
            PasoDeJuego(como, "integracion", "1 · Arma la tubería", "Del código al cliente, sin atajos.");
            PasoDeJuego(como, "vigilancia", "2 · Enciende los cuartos", "A oscuras, con una linterna.");
            Ui.Chip(como, "Tienes " + Reloj(_total), Tema.warningSoft, Tema.warning);

            for (var i = 0; i < fila.childCount; i++) Aparecer((RectTransform)fila.GetChild(i), 0.15f + i * 0.16f);

            // ---- abajo: las palabras del oficio y empezar
            var pie = Globo(_escena, "Empezar", true);
            Anclar(pie, new Vector2(0.5f, 0), new Vector2(0, 28));
            Crecer(pie, true);
            PantallaDeGlosario.Palabras(App, pie, "", "despliegue", "pipeline", "entorno", "secretos");
            var empezar = Ui.Boton(pie, "Empezar  ►", EmpezarLaTuberia, VarianteBoton.Primario);
            UiKit.Tamano(empezar, alto: 64);
            Aparecer(pie, 0.7f);
            GuiaView.Avisar(App, "despliegue.intro");
        }

        /// <summary>Una tarjeta de cristal de la introduccion: ancho fijo, un rotulo de color y lo que se le meta.</summary>
        private RectTransform Tarjeta(RectTransform padre, string titulo, Color color) {
            var t = Globo(padre, "Tarjeta " + titulo, false, NexusTheme.Alfa(color, 0.7f), Tema.espacio * 1.4f);
            UiKit.Tamano(t, ancho: 500);
            Ui.Texto(t, titulo, EstiloTexto.Leyenda, color);
            return t;
        }

        /// <summary>Un paso de «como se juega»: un dibujo a la izquierda y su titulo con una linea debajo.</summary>
        private void PasoDeJuego(RectTransform padre, string icono, string titulo, string linea) {
            var fila = Ui.Fila(padre, "Paso", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var nodo = Ui.Nodo(fila, "Icono");
            UiKit.Tamano(nodo, 64, 64);
            nodo.gameObject.AddComponent<CanvasRenderer>();
            var dibujo = nodo.gameObject.AddComponent<DibujoUI>();
            dibujo.raycastTarget = false;
            DibujosDeEtapas.Dibujar(dibujo, icono, 0, 0, 64, Tema.ink, Tema.cyan);
            var textos = Ui.Columna(fila, "Texto", 2);
            UiKit.Tamano(textos, flexAncho: 1);
            Ui.Texto(textos, titulo, EstiloTexto.Encabezado, Tema.ink);
            Ui.Texto(textos, linea, EstiloTexto.Pequeno, Tema.inkMuted);
        }

        private void EmpezarLaTuberia() {
            if (_transicionando || _paso != Paso.Intro) return;
            _paso = Paso.Tuberia;
            LimpiarPaso();
            Transicion(PintarTuberia);
        }
    }
}
