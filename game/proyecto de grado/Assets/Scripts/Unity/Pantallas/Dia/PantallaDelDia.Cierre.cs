using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Jornada;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El cierre de la jornada (2026-11-07). Antes: una etiqueta, el titulo, dos parrafos y dos botones pequeños, con
    /// dos tercios de la carta vacios. Ahora, sin scroll:
    ///   arriba    «Son las 17:00» y, a la derecha, la racha de dias quedandose (puntos; en rojo desde 2)
    ///   centro    la ventana de la oficina al atardecer (VentanaDelAtardecer), que cambia segun la carta que se mira
    ///   linea     la jornada de la mañana al cierre, y el tramo de horas extra hasta la hora limite, a rayas ambar
    ///   abajo     la decision como dos cartas: «Irme a casa» / «Quedarme hasta las 19:00»
    /// Lo que hacen las cartas es lo mismo que hacian los botones (y la guia del tutorial sigue esperando «irse»).
    /// </summary>
    public sealed partial class PantallaDelDia {
        private VentanaDelAtardecer _ventanaDelCierre;
        private int _diaDelCierre = -1;

        private void Cierre() {
            var j = S.Perfil.Jornada;
            var raiz = _fijoMomento;
            var primeraVez = _diaDelCierre != S.R.DiaActual;
            _diaDelCierre = S.R.DiaActual;

            // ---- cabecera: la hora y la racha
            var cabecera = Ui.Fila(raiz, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var titulos = Ui.Columna(cabecera, "Titulos", 2);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "CIERRE DE LA JORNADA", EstiloTexto.Leyenda, Tema.cyan).characterSpacing = 3;
            var titulo = Ui.Texto(titulos, $"Son las {S.HoraActual}", EstiloTexto.Titulo, Tema.ink);
            titulo.font = Tema.FuenteDisplay;
            RachaDeHorasExtra(cabecera, S.R.DiasSeguidosTrabajando);

            // ---- la ventana
            _ventanaDelCierre = VentanaDelAtardecer.Crear(Ui, raiz, S.MinutoDelDia, j.HoraCierre * RelojDeJornada.MinutosPorHora,
                                                          j.HoraLimite * RelojDeJornada.MinutosPorHora);
            if (primeraVez) _ventanaDelCierre.gameObject.AddComponent<EntradaDePieza>();

            // ---- la linea del dia
            LineaDeHorasExtra(raiz, j, primeraVez);

            // ---- la decision
            var limite = $"{j.HoraLimite:00}:00";
            var cartas = new List<OpcionDeCarta> {
                new OpcionDeCarta {
                    Titulo = "Irme a casa",
                    Detalle = Solo("Descansas, y el equipo también.", "Descansas."),
                    Pie = "Cansancio ↓ · mañana rindes mejor",
                    AlElegir = () => { Runner.Irse(); Anotar("Te fuiste a casa."); GuiaView.Hecho(App, "irse"); },
                },
                new OpcionDeCarta {
                    Titulo = "Quedarme hasta las " + limite,
                    Detalle = "Hoy avanzas un 25 % más. Sin avisos: tiempo libre para recorrer el mapa.",
                    Pie = "Cansancio, salud y deuda ↑ · se acumula",
                    // Quedarse hace imposible el «Irme a casa» que pudiera estar pidiendo la guia: se cancela, no se cuelga.
                    AlElegir = () => { Runner.Quedarse(); Anotar("Te quedaste haciendo horas extra."); GuiaView.Cancelar("irse"); },
                },
            };
            var mano = Ui.Fila(raiz, "Mano", 0, 0, TextAnchor.MiddleCenter);
            var ventana = _ventanaDelCierre;
            ManoDeCartas.Crear(Ui, mano, cartas, new ManoDeCartas.Ajustes {
                Sello = "DECIDIDO",
                AnchoDeCarta = 320,
                AlEnfocar = i => {
                    if (ventana == null) return;
                    ventana.Actual = i == 0 ? VentanaDelAtardecer.Modo.Casa : i == 1 ? VentanaDelAtardecer.Modo.Quedarse : VentanaDelAtardecer.Modo.Neutro;
                },
            });
        }

        // ==================================================================== se acabaron los dias

        private int _vecesFinDelDesarrollo;

        /// <summary>
        /// Se acabaron los dias (2026-11-07), con la misma estructura que el cierre y sin scroll: la cabecera con la
        /// tira de dias, una fila de cuatro cifras de como llega el proyecto, la escena de lo construido saliendo hacia
        /// produccion (o hacia el cliente) y una sola carta para seguir. Antes era un parrafo largo y un boton pequeño
        /// cuyo texto («Ir al despliegue (Fase 3) ►») no cabia.
        /// </summary>
        private void FinDelDesarrollo() {
            var falta = S.DesplieguePendiente;
            var w = S.W;
            var raiz = _fijoMomento;
            var primeraVez = _vecesFinDelDesarrollo++ == 0;
            var acento = falta ? Tema.cyan : Tema.violet;

            // ---- cabecera
            var cabecera = Ui.Fila(raiz, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var titulos = Ui.Columna(cabecera, "Titulos", 2);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, falta ? "FASE 3 · EL ÚLTIMO DESPLIEGUE" : "FASE 4 · EL LANZAMIENTO", EstiloTexto.Leyenda, acento).characterSpacing = 3;
            var titulo = Ui.Texto(titulos, "Se acabaron los días", EstiloTexto.Titulo, Tema.ink);
            titulo.font = Tema.FuenteDisplay;
            Ui.Texto(titulos, falta ? "Queda llevar a producción todo lo construido." : "Ahora el cliente ve lo que has hecho.",
                     EstiloTexto.Pequeno, Tema.inkMuted);
            DiasCumplidos(cabecera);

            // ---- como llega el proyecto, en cuatro cifras
            var cifras = Ui.Fila(raiz, "Cifras", Tema.espacio, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(cifras, flexAncho: 1);
            var tablero = S.R.Tablero;
            var hechas = tablero == null ? 0 : tablero.Tarjetas.Count(t => t.Terminada);
            var total = tablero == null ? 0 : tablero.Tarjetas.Count;
            var avance = w.Alcance > 0 ? 100 * w.Avance / w.Alcance : 0;
            var riesgo = S.R.SerieRiesgo.Count > 0 ? S.R.SerieRiesgo[S.R.SerieRiesgo.Count - 1] : (double?)null;
            var retraso = primeraVez ? 0.1f : -1f;
            Cifra(cifras, "Avance", avance, "0", "%", avance >= 80 ? Tema.success : avance >= 50 ? Tema.warning : Tema.danger, retraso);
            Cifra(cifras, "Tarjetas hechas", hechas, "0", total > 0 ? $"/{total}" : "", Tema.cyan, retraso < 0 ? -1 : retraso + 0.06f);
            Cifra(cifras, "Deuda técnica", w.DeudaTecnica, "0", "", w.DeudaTecnica >= 50 ? Tema.danger : w.DeudaTecnica >= 25 ? Tema.warning : Tema.success,
                  retraso < 0 ? -1 : retraso + 0.12f);
            if (riesgo.HasValue)
                Cifra(cifras, "Riesgo", riesgo.Value, "0", "", riesgo >= 50 ? Tema.danger : riesgo >= 25 ? Tema.warning : Tema.success,
                      retraso < 0 ? -1 : retraso + 0.18f);

            // ---- la escena
            var etapas = falta ? new[] { "Build", "Pruebas", "Staging", "Producción" } : new[] { "Entrega", "Demo", "Revisión", "El cliente" };
            var escena = EscenaDelDespliegue.Crear(Ui, raiz, hechas, !falta, etapas);
            if (primeraVez) escena.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.12f;

            // ---- una sola carta para seguir (el boton de antes no le cabia el texto)
            var mano = Ui.Fila(raiz, "Mano", 0, 0, TextAnchor.MiddleCenter);
            ManoDeCartas.Crear(Ui, mano, new List<OpcionDeCarta> {
                new OpcionDeCarta {
                    Titulo = falta ? "Ir al despliegue" : "Ir al lanzamiento",
                    Detalle = falta ? "El último despliegue, con el reloj en cuenta atrás." : "El riesgo que acumulaste decide si sale bien.",
                    Pie = falta ? "Cómo salga pesa en la evaluación" : "Después, el nivel se cierra y se evalúa",
                    AlElegir = () => FlujoDelNivel.Lanzar(App),
                },
            }, new ManoDeCartas.Ajustes {
                Sello = "EN MARCHA",
                ColorDelSello = acento,
                AnchoDeCarta = 380,
                AlEnfocar = i => { if (escena != null) escena.Listo = i == 0; },
            });
        }

        /// <summary>A la derecha de la cabecera: un punto por dia, todos cumplidos, y «N de N días».</summary>
        private void DiasCumplidos(Transform padre) {
            var dias = S.Perfil.DiasTotales;
            var columna = Ui.Columna(padre, "Dias", 4, 0, TextAnchor.MiddleRight);
            var puntos = Ui.Fila(columna, "Puntos", 5, 0, TextAnchor.MiddleRight);
            for (var i = 0; i < Mathf.Min(dias, 15); i++) Ui.Punto(puntos, NexusTheme.Alfa(Tema.cyan, 0.85f), 9);
            Ui.Texto(columna, $"{dias} de {dias} días", EstiloTexto.Pequeno, Tema.inkMuted, TMPro.TextAlignmentOptions.MidlineRight);
        }

        /// <summary>Un panel pequeño de los indicadores: el rotulo y una cifra grande que cuenta desde 0.</summary>
        private void Cifra(Transform padre, string rotulo, double valor, string formato, string sufijo, Color color, float retraso) {
            var panel = PiezasDelMuro.Panel(Ui, padre, rotulo, color, 12);
            UiKit.Tamano(panel, flexAncho: 1);
            PiezasDelMuro.Cifra(Ui, panel, valor, formato, sufijo, color, 34, retraso);
        }

        /// <summary>A la derecha de la cabecera: un punto por dia seguido quedandose (hasta 5) y una linea corta.</summary>
        private void RachaDeHorasExtra(Transform padre, int dias) {
            var peligro = EscenaDelCierre.EsRachaPeligrosa(dias);
            var color = peligro ? Tema.danger : dias > 0 ? Tema.warning : Tema.inkFaint;
            var columna = Ui.Columna(padre, "Racha", 4, 0, TextAnchor.MiddleRight);
            var puntos = Ui.Fila(columna, "Puntos", 6, 0, TextAnchor.MiddleRight);
            for (var i = 0; i < 5; i++) {
                var encendido = i < dias;
                var p = Ui.Punto(puntos, encendido ? color : NexusTheme.Alfa(Tema.inkFaint, 0.5f), 10);
                if (encendido && peligro && i == Mathf.Min(dias, 5) - 1) p.gameObject.AddComponent<Latido>().Fuerza = 0.15f;
            }
            Ui.Texto(columna, EscenaDelCierre.Racha(dias), EstiloTexto.Pequeno, peligro ? Tema.danger : Tema.inkMuted,
                     TMPro.TextAlignmentOptions.MidlineRight);
        }

        /// <summary>
        /// La jornada en una linea: de la hora de entrada al cierre, llena; del cierre a la hora limite, a rayas ambar
        /// (lo que costaria quedarse). Un punto cian marca ahora.
        /// </summary>
        private void LineaDeHorasExtra(Transform padre, JornadaConfig j, bool animar) {
            var fila = Ui.Fila(padre, "Linea del dia", 10, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(fila, alto: 22, flexAncho: 1);
            Ui.Texto(fila, $"{j.HoraInicio:00}:00", EstiloTexto.Pequeno, Tema.inkFaint);

            var jornada = Ui.Nodo(fila, "Jornada");
            UiKit.Tamano(jornada, alto: 6, flexAncho: Mathf.Max(1, j.HoraCierre - j.HoraInicio));
            var lleno = jornada.gameObject.AddComponent<Image>();
            Ui.Fondo(lleno, NexusTheme.Alfa(Tema.cyan, 0.35f), NexusTheme.RadioSm);
            lleno.raycastTarget = false;

            var ahora = Ui.Punto(fila, Tema.cyan, 12);
            ahora.gameObject.AddComponent<Latido>();
            Ui.Texto(fila, S.HoraActual, EstiloTexto.Pequeno, Tema.cyan);

            var extra = Ui.Fila(fila, "Horas extra", 5, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(extra, alto: 6, flexAncho: Mathf.Max(1, j.HoraLimite - j.HoraCierre));
            extra.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            extra.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;
            for (var i = 0; i < 10; i++) {
                var raya = Ui.Nodo(extra, "Raya");
                UiKit.Tamano(raya, alto: 4, flexAncho: 1);
                var img = raya.gameObject.AddComponent<Image>();
                img.color = NexusTheme.Alfa(Tema.warning, 0.75f);
                img.raycastTarget = false;
                if (animar) raya.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.25f + i * 0.04f;
            }
            Ui.Texto(fila, $"{j.HoraLimite:00}:00 · +25 % avance", EstiloTexto.Pequeno, Tema.warning);
        }
    }
}
