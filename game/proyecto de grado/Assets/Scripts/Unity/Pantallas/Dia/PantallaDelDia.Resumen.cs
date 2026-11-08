using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Sesion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El cierre del dia (2026-11-06): una carta fija, sin desplazarse, que se lee de un vistazo.
    ///   arriba   — «Día N de T, terminado» y la tira de dias (hechos, hoy, los que quedan)
    ///   1        — ¿Cómo cambió el proyecto hoy? cinco medidores: valor que cuenta, cambio en color, barra que crece
    ///   2        — ¿Por qué avanzaste X puntos? la velocidad base, cada factor como barra a izquierda o derecha, el total
    ///   abajo    — lo que cambio de etapa (o lo que se construyo) y, a la derecha, las notas cortas y «Empezar el día»
    /// Las piezas entran escalonadas y las barras crecen desde como estaba el dia por la mañana.
    /// </summary>
    public sealed partial class PantallaDelDia {
        private static readonly string[] NombresDeMedidores = { "Avance", "Deuda técnica", "Moral", "Cobertura", "Cansancio" };
        private static readonly bool[] SubirEsBueno = { true, false, true, true, false };

        private void Resumen() {
            var w = S.W;
            var raiz = _fijoMomento;
            var retraso = 0f;

            // ---- cabecera: titulo y tira de dias
            var cabecera = Ui.Fila(raiz, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            var titulos = Ui.Columna(cabecera, "Titulos", 2);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "FIN DEL DÍA", EstiloTexto.Leyenda, Tema.cyan);
            Ui.Texto(titulos, $"Día {S.R.DiaActual} de {S.Perfil.DiasTotales}, terminado", EstiloTexto.Titulo, Tema.ink);
            TiraDeDias(cabecera);

            // ---- 1. como cambio el proyecto
            var caja1 = Caja(raiz, S.JornadaProrrogada ? "¿Cómo cambió el proyecto hoy? (con horas extra)" : "¿Cómo cambió el proyecto hoy?",
                             Tema.cyan, 1f, retraso += 0.08f);
            var medidores = Ui.Fila(caja1, "Medidores", Tema.espacio, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(medidores, flexAncho: 1, flexAlto: 1);
            var ahora = new[] { w.Avance, w.DeudaTecnica, w.MoralEquipo, w.Cobertura, w.Cansancio };
            for (var i = 0; i < ahora.Length; i++)
                Medidor(medidores, i, _tengoElInicioDelDia ? _alEmpezarElDia[i] : ahora[i], ahora[i], w.Alcance, 0.25f + i * 0.08f);

            // ---- 2. por que avanzaste lo que avanzaste
            var avanzado = _tengoElInicioDelDia ? w.Avance - _alEmpezarElDia[0] : 0;
            var caja2 = Caja(raiz, $"¿Por qué avanzaste {avanzado:0.#} puntos?", Tema.violet, 1f, retraso += 0.08f);
            PorQueAvanzaste(caja2, avanzado);

            // ---- abajo: la etapa (o lo construido) y la columna de cierre
            var abajo = Ui.Fila(raiz, "Abajo", Tema.espacio, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(abajo, flexAncho: 1, flexAlto: 1.1f);
            LoQueSeConstruyo(abajo, retraso += 0.08f);
            ColumnaDeCierre(abajo, retraso + 0.1f);
        }

        // ==================================================================== piezas

        /// <summary>Una caja de la carta: fondo elevado, borde del color del tema y una marca a la izquierda del titulo.</summary>
        private RectTransform Caja(RectTransform padre, string titulo, Color acento, float flexAlto, float retraso) {
            var caja = Ui.PanelColumna(padre, "Caja " + titulo, Tema.Espacio(4), Tema.Espacio(3), Tema.surfaceRaised);
            UiKit.Tamano(caja, flexAncho: 1, flexAlto: flexAlto);
            Ui.Borde(caja, NexusTheme.Alfa(acento, 0.45f), NexusTheme.RadioLg, 1);
            var cab = Ui.Fila(caja, "Titulo", 10, 0, TextAnchor.MiddleLeft);
            var marca = Ui.Nodo(cab, "Marca");
            var img = marca.gameObject.AddComponent<Image>();
            img.color = acento;
            img.raycastTarget = false;
            UiKit.Tamano(marca, 5, 24);
            Ui.Texto(cab, titulo, EstiloTexto.Subtitulo, Tema.ink);
            caja.gameObject.AddComponent<EntradaDeResumen>().Retraso = retraso;
            return caja;
        }

        /// <summary>Un punto por dia: los hechos en cian, hoy en dorado (late), los que quedan apagados.</summary>
        private void TiraDeDias(RectTransform padre) {
            var total = Mathf.Clamp(S.Perfil.DiasTotales, 1, 30);
            var columna = Ui.Columna(padre, "Dias", 6, 0, TextAnchor.MiddleRight);
            var tira = Ui.Fila(columna, "Tira", 6, 0, TextAnchor.MiddleRight);
            var despliegues = new HashSet<int>(S.DiasDeDespliegue());
            for (var d = 1; d <= total; d++) {
                var color = d < S.R.DiaActual ? Tema.cyan : d == S.R.DiaActual ? PlanoIlustrado.ColorDelDestello : Tema.lineStrong;
                var punto = Ui.Punto(tira, color, d == S.R.DiaActual ? 16 : 11);
                if (d == S.R.DiaActual) punto.gameObject.AddComponent<LatidoDeResumen>();
                else if (despliegues.Contains(d) && d > S.R.DiaActual) punto.color = NexusTheme.Alfa(Tema.warning, 0.8f);
            }
            var quedan = Math.Max(0, S.Perfil.DiasTotales - S.R.DiaActual);
            Ui.Texto(columna, quedan == 0 ? "Era el último día" : quedan == 1 ? "Queda 1 día" : $"Quedan {quedan} días",
                     EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Right);
        }

        /// <summary>
        /// Un medidor del dia: su nombre, el valor (que cuenta desde el de la mañana), el cambio en una pastilla verde o
        /// roja segun si es bueno o malo que suba, y una barra que crece desde la marca de como estaba.
        /// </summary>
        private void Medidor(RectTransform padre, int i, double antes, double ahora, double alcance, float retraso) {
            var esAvance = i == 0;
            var escala = esAvance ? Math.Max(1, alcance) : 100.0;
            var cambio = ahora - antes;
            var hayCambio = _tengoElInicioDelDia && Math.Abs(cambio) >= 0.05;
            var bueno = cambio > 0 == SubirEsBueno[i];
            var color = !hayCambio ? Tema.cyan : bueno ? Tema.success : Tema.danger;

            var tile = Ui.PanelColumna(padre, NombresDeMedidores[i], Tema.Espacio(3), 8, Tema.bg900);
            UiKit.Tamano(tile, flexAncho: 1);
            Ui.Texto(tile, NombresDeMedidores[i].ToUpperInvariant(), EstiloTexto.Leyenda, Tema.inkMuted);
            var linea = Ui.Fila(tile, "Valor", 8, 0, TextAnchor.MiddleLeft);
            var valor = Ui.Texto(linea, "", EstiloTexto.Titulo, Tema.ink);
            valor.textWrappingMode = TextWrappingModes.NoWrap;
            var contar = valor.gameObject.AddComponent<CuentaDeResumen>();
            contar.Desde = (float)(antes / escala * 100);
            contar.Hasta = (float)(ahora / escala * 100);
            contar.Sufijo = esAvance || i == 3 ? " %" : "";
            contar.Retraso = retraso;
            if (_tengoElInicioDelDia) {
                var texto = !hayCambio ? "igual" : (cambio > 0 ? "+" : "-") + Math.Abs(cambio).ToString("0.#") + (esAvance ? " pts" : "");
                var chip = Ui.Chip(linea, texto, !hayCambio ? Tema.surfaceRaised : bueno ? Tema.successSoft : Tema.dangerSoft,
                                   !hayCambio ? Tema.inkMuted : color);
                chip.gameObject.AddComponent<EntradaDeResumen>().Retraso = retraso + 0.6f;
            }
            var barra = Ui.Barra(tile, (float)(antes / escala), color);
            var crecer = barra.gameObject.AddComponent<CrecimientoDeBarra>();
            crecer.Barra = barra;
            crecer.Desde = (float)(antes / escala);
            crecer.Hasta = (float)(ahora / escala);
            crecer.Retraso = retraso;
            if (hayCambio) {
                // La marca de como estaba esta mañana: se ve cuanto se movio.
                var marca = Ui.Nodo(barra.transform, "Esta mañana");
                marca.anchorMin = new Vector2(crecer.Desde, -0.4f);
                marca.anchorMax = new Vector2(crecer.Desde, 1.4f);
                marca.sizeDelta = new Vector2(2, 0);
                marca.anchoredPosition = Vector2.zero;
                var img = marca.gameObject.AddComponent<Image>();
                img.color = NexusTheme.Alfa(Tema.ink, 0.7f);
                img.raycastTarget = false;
                var le = marca.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;
            }
        }

        /// <summary>
        /// La velocidad base, cada factor como una barra que tira a la izquierda (resta) o a la derecha (suma), y el total.
        /// Es ForresterModel a la vista: la deuda, el cansancio y la moral SI cuentan cada dia.
        /// </summary>
        private void PorQueAvanzaste(RectTransform caja, double avanzado) {
            var fila = Ui.Fila(caja, "Cuenta", Tema.espacio, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1);
            if (!_tengoElInicioDelDia) {
                Ui.Texto(fila, "Este día se cargó de una partida guardada: la cuenta de hoy no está.", EstiloTexto.Pequeno, Tema.inkMuted,
                         TextAlignmentOptions.Center);
                return;
            }
            Pastilla(fila, "BASE", $"{S.Perfil.VelocidadBase:0.#} pts", Tema.cyan, 0.3f);
            var factores = new[] {
                new KeyValuePair<string, double>("Moral", Nexus.Core.Simulacion.ForresterModel.FMoral(_alEmpezarElDia[2])),
                new KeyValuePair<string, double>("Cansancio", Nexus.Core.Simulacion.ForresterModel.FFatiga(_alEmpezarElDia[4])),
                new KeyValuePair<string, double>("Deuda técnica", Nexus.Core.Simulacion.ForresterModel.FDeuda(_alEmpezarElDia[1])),
                new KeyValuePair<string, double>("Competencia", Nexus.Core.Simulacion.ForresterModel.FComp(S.W.Competencia)),
            };
            for (var i = 0; i < factores.Length; i++) {
                var pct = (factores[i].Value - 1) * 100;
                var color = pct < -5 ? Tema.danger : pct > 0.5 ? Tema.success : Tema.inkMuted;
                var col = Ui.Columna(fila, factores[i].Key, 6, 0, TextAnchor.MiddleCenter);
                UiKit.Tamano(col, flexAncho: 1);
                Ui.Texto(col, factores[i].Key.ToUpperInvariant(), EstiloTexto.Leyenda, Tema.inkMuted, TextAlignmentOptions.Center);
                var cifra = Ui.Texto(col, (pct >= 0 ? "+" : "-") + Math.Abs(pct).ToString("0") + " %", EstiloTexto.Subtitulo, color,
                                     TextAlignmentOptions.Center);
                cifra.gameObject.AddComponent<EntradaDeResumen>().Retraso = 0.45f + i * 0.1f;
                BarraDivergente(col, Mathf.Clamp((float)pct / 40f, -1, 1), color, 0.45f + i * 0.1f);
            }
            var igual = Ui.Texto(fila, "=", EstiloTexto.Titulo, Tema.inkMuted, TextAlignmentOptions.Center);
            UiKit.Tamano(igual, ancho: 24);
            Pastilla(fila, S.JornadaProrrogada ? "HOY (+25 % EXTRA)" : "HOY", $"{avanzado:0.#} pts", PlanoIlustrado.ColorDelDestello, 0.95f);
        }

        /// <summary>Una cifra destacada en una pastilla: titulo pequeño arriba, el numero grande.</summary>
        private void Pastilla(RectTransform padre, string titulo, string cifra, Color color, float retraso) {
            var p = Ui.PanelColumna(padre, "Pastilla " + titulo, Tema.Espacio(3), 2, NexusTheme.Alfa(color, 0.12f));
            UiKit.Tamano(p, ancho: 170);
            Ui.Borde(p, NexusTheme.Alfa(color, 0.6f), NexusTheme.RadioMd, 1.5f);
            Ui.Texto(p, titulo, EstiloTexto.Leyenda, color, TextAlignmentOptions.Center);
            var t = Ui.Texto(p, cifra, EstiloTexto.Titulo, Tema.ink, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            p.gameObject.AddComponent<EntradaDeResumen>().Retraso = retraso;
        }

        /// <summary>Una barra con el cero en el centro: crece a la derecha si suma, a la izquierda si resta.</summary>
        private void BarraDivergente(RectTransform padre, float valor, Color color, float retraso) {
            var fondo = Ui.Nodo(padre, "Divergente");
            UiKit.Tamano(fondo, alto: 10, flexAncho: 1);
            Ui.Fondo(fondo.gameObject.AddComponent<Image>(), Tema.bg950, 3);
            var relleno = Ui.Nodo(fondo, "Relleno");
            var img = relleno.gameObject.AddComponent<Image>();
            Ui.Fondo(img, color, 3);
            img.raycastTarget = false;
            relleno.anchorMin = relleno.anchorMax = new Vector2(0.5f, 0);
            relleno.offsetMin = relleno.offsetMax = Vector2.zero;
            var centro = Ui.Nodo(fondo, "Cero");
            centro.anchorMin = new Vector2(0.5f, -0.5f);
            centro.anchorMax = new Vector2(0.5f, 1.5f);
            centro.sizeDelta = new Vector2(2, 0);
            centro.anchoredPosition = Vector2.zero;
            var imgCentro = centro.gameObject.AddComponent<Image>();
            imgCentro.color = Tema.inkMuted;
            imgCentro.raycastTarget = false;
            var crecer = fondo.gameObject.AddComponent<CrecimientoDivergente>();
            crecer.Relleno = relleno;
            crecer.Valor = valor;
            crecer.Retraso = retraso;
        }

        /// <summary>
        /// Abajo a la izquierda: lo que cambio de etapa (con una voz de quien lo usara), o, si nada cambio de etapa,
        /// las partes que avanzaron como barras. En dia de demo, lo que dijeron.
        /// </summary>
        private void LoQueSeConstruyo(RectTransform padre, float retraso) {
            var f = S.Perfil.Proyecto;
            var w = S.W;
            var antes = _tengoElInicioDelDia ? _alEmpezarElDia[0] : w.Avance;
            var subieron = f == null ? new List<Nexus.Core.Proyecto.EstadoDelModulo>()
                : Nexus.Core.Proyecto.MaquetaDelProducto.QueSubieron(f, antes, w.Avance, w.Alcance, S.R.ModulosDesplegados);
            var plan = S.PlanDeHoy;
            var voces = S.VocesDelProducto();
            var hayDemo = plan != null && plan.EsUltimoDiaDeUnidad && voces.Count > 0;

            var titulo = subieron.Count > 0 ? "El producto cambió de etapa" : hayDemo ? "La demo con el cliente" : "Hoy se construyó";
            var caja = Caja(padre, titulo, subieron.Count > 0 ? Tema.success : Tema.cyan, 1, retraso);
            UiKit.Tamano(caja, flexAlto: 1);

            var maxFilas = hayDemo ? 2 : 3;
            if (subieron.Count > 0) {
                foreach (var m in subieron.Take(maxFilas)) {
                    var fila = Ui.Fila(caja, "Etapa", 10, 0, TextAnchor.MiddleLeft);
                    Ui.Punto(fila, Tema.success, 10);
                    var nombre = Ui.Texto(fila, m.Modulo.Nombre, EstiloTexto.Cuerpo, Tema.ink);
                    nombre.maxVisibleLines = 1;
                    nombre.overflowMode = TextOverflowModes.Ellipsis;
                    UiKit.Tamano(nombre, flexAncho: 1);
                    Ui.Chip(fila, Nexus.Core.Proyecto.EtapasDelProducto.Nombre(m.Etapa), Tema.successSoft, Tema.success);
                    fila.gameObject.AddComponent<EntradaDeResumen>().Retraso = retraso + 0.2f;
                }
                if (subieron.Count > maxFilas) Ui.Texto(caja, $"y {subieron.Count - maxFilas} más", EstiloTexto.Pequeno, Tema.inkMuted);
            } else if (f != null && f.Modulos.Count > 0 && !hayDemo) {
                var deAntes = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(f, antes, w.Alcance);
                var deAhora = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(f, w.Avance, w.Alcance);
                var cambios = Enumerable.Range(0, deAhora.Count).Where(i => deAhora[i].Value - deAntes[i].Value >= 0.5)
                    .OrderByDescending(i => deAhora[i].Value - deAntes[i].Value).Take(maxFilas).ToList();
                if (cambios.Count == 0)
                    Ui.Texto(caja, "Hoy no avanzó ninguna parte del sistema.", EstiloTexto.Cuerpo, Tema.inkMuted);
                foreach (var i in cambios) {
                    var fila = Ui.Fila(caja, "Parte", 10, 0, TextAnchor.MiddleLeft);
                    var nombre = Ui.Texto(fila, deAhora[i].Key.Nombre, EstiloTexto.Pequeno, Tema.ink);
                    nombre.maxVisibleLines = 1;
                    nombre.overflowMode = TextOverflowModes.Ellipsis;
                    UiKit.Tamano(nombre, ancho: 220);
                    var desde = (float)(deAntes[i].Value / 100);
                    var barra = Ui.Barra(fila, desde, Tema.cyan);
                    UiKit.Tamano(barra, flexAncho: 1);
                    var crecer = barra.gameObject.AddComponent<CrecimientoDeBarra>();
                    crecer.Barra = barra;
                    crecer.Desde = desde;
                    crecer.Hasta = (float)(deAhora[i].Value / 100);
                    crecer.Retraso = retraso + 0.3f;
                    Ui.Chip(fila, $"+{deAhora[i].Value - deAntes[i].Value:0} %", Tema.cyanSoft, Tema.cyan);
                }
            }

            // Una voz (o dos, si fue la demo): lo que dicen quienes van a usarlo.
            var queDicen = hayDemo ? voces.Take(2).ToList()
                : voces.Where(v => subieron.Any(m => m.Modulo.Id == v.ModuloId)).Take(1).ToList();
            foreach (var v in queDicen) {
                var cita = Ui.Texto(caja, $"{v.Quien}: «{v.Frase}»", EstiloTexto.Dialogo, Tema.cyan);
                cita.maxVisibleLines = 2;
                cita.overflowMode = TextOverflowModes.Ellipsis;
                cita.gameObject.AddComponent<EntradaDeResumen>().Retraso = retraso + 0.5f;
            }
            if (subieron.Count > 0 || hayDemo) {
                UiKit.Tamano(Ui.Nodo(caja, "Hueco"), flexAlto: 1);
                Ui.Boton(Ui.Fila(caja), "Ver el producto", () => PantallaDelProducto.Abrir(App), VarianteBoton.Fantasma);
            }
        }

        /// <summary>Abajo a la derecha: las notas cortas (decisiones, guardado, despliegue, arquitectura) y el boton de seguir.</summary>
        private void ColumnaDeCierre(RectTransform padre, float retraso) {
            var col = Ui.Columna(padre, "Cierre", 10, 0, TextAnchor.UpperLeft);
            UiKit.Tamano(col, ancho: 380, flexAlto: 1);
            col.gameObject.AddComponent<EntradaDeResumen>().Retraso = retraso;

            if (_tengoElInicioDelDia)
                Nota(col, Tema.violet, _decisionesDeHoy == 0 ? "No tomaste ninguna decisión" :
                                       _decisionesDeHoy == 1 ? "Tomaste 1 decisión" : $"Tomaste {_decisionesDeHoy} decisiones");
            Nota(col, Tema.success, "La partida se ha guardado");
            var proximo = S.DiasDeDespliegue().Where(d => d > S.R.DiaActual).Select(d => (int?)d).FirstOrDefault();
            if (proximo.HasValue && !S.DesplieguePendiente) Nota(col, Tema.warning, $"Próximo despliegue: al cerrar el día {proximo.Value}");
            // Cuando la arquitectura elegida pesa sobre el modelo (o venia con un coste), se dice: no es mala suerte.
            var arq = S.Perfil.Fase1 == null ? null : S.Perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == S.ArquitecturaElegida);
            if (arq != null && (!arq.EsLaAdecuada || (arq.ModificadoresModelo != null && arq.ModificadoresModelo.Count > 0))) {
                var t = Nota(col, Tema.warning, $"Por tu arquitectura ({arq.Nombre}): {DibujosDeArquitectura.EnElDia(arq.Id)}");
                t.maxVisibleLines = 3;
                t.overflowMode = TextOverflowModes.Ellipsis;
            }
            UiKit.Tamano(Ui.Nodo(col, "Hueco"), flexAlto: 1);

            // Dia de despliegue: antes de seguir construyendo, lo que esta listo sale a produccion (Fase 3).
            if (S.DesplieguePendiente) {
                var aviso = Ui.PanelColumna(col, "Despliegue", Tema.Espacio(3), 4, Tema.warningSoft);
                Ui.Borde(aviso, Tema.warning, NexusTheme.RadioMd, 1.5f);
                Ui.Texto(aviso, "HOY TOCA DESPLEGAR", EstiloTexto.Leyenda, Tema.warning);
                Ui.Texto(aviso, "Lo que está listo llega al cliente, con el reloj en cuenta atrás.", EstiloTexto.Pequeno, Tema.ink);
                var ir = Ui.Boton(col, "Ir al despliegue (Fase 3)", () => FlujoDelNivel.Desplegar(App, () => App.Router.IrA<PantallaDelDia>()),
                                  VarianteBoton.Primario);
                UiKit.Tamano(ir, alto: 60, flexAncho: 1);
                return;
            }
            var seguir = Ui.Boton(col, $"Empezar el día {S.R.DiaActual + 1}", EmpezarDia, VarianteBoton.Primario);
            UiKit.Tamano(seguir, alto: 60, flexAncho: 1);
            seguir.gameObject.AddComponent<LatidoDeResumen>().Suave = true;
        }

        /// <summary>Una nota de una linea con un punto de color delante.</summary>
        private TMP_Text Nota(RectTransform padre, Color color, string texto) {
            var fila = Ui.Fila(padre, "Nota", 10, 0, TextAnchor.UpperLeft);
            var punto = Ui.Punto(fila, color, 9);
            var lp = punto.gameObject.GetComponent<LayoutElement>();
            if (lp != null) lp.minHeight = lp.preferredHeight = 22;
            punto.preserveAspect = true;
            var t = Ui.Texto(fila, texto, EstiloTexto.Pequeno, Tema.ink);
            UiKit.Tamano(t, flexAncho: 1);
            return t;
        }

        // ==================================================================== animaciones

        /// <summary>Aparece tras un retraso: se funde y crece un poco (la escala no la toca el layout).</summary>
        private sealed class EntradaDeResumen : MonoBehaviour {
            public float Retraso;
            private float _t;
            private CanvasGroup _grupo;
            private void Awake() {
                _grupo = gameObject.AddComponent<CanvasGroup>();
                _grupo.alpha = 0;
            }
            private void Update() {
                _t += Time.unscaledDeltaTime;
                var r = Mathf.Clamp01((_t - Retraso) / 0.35f);
                var e = 1 - Mathf.Pow(1 - r, 3);
                _grupo.alpha = e;
                transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
                if (r >= 1) enabled = false;
            }
        }

        /// <summary>Una barra que va de como estaba a como esta.</summary>
        private sealed class CrecimientoDeBarra : MonoBehaviour {
            public BarraView Barra;
            public float Desde, Hasta, Retraso;
            private float _t;
            private void Update() {
                _t += Time.unscaledDeltaTime;
                var r = Mathf.Clamp01((_t - Retraso) / 0.8f);
                var e = 1 - Mathf.Pow(1 - r, 3);
                if (Barra != null) Barra.Valor = Mathf.Lerp(Desde, Hasta, e);
                if (r >= 1) enabled = false;
            }
        }

        /// <summary>Un numero que cuenta de un valor a otro.</summary>
        private sealed class CuentaDeResumen : MonoBehaviour {
            public float Desde, Hasta, Retraso;
            public string Sufijo = "";
            private float _t;
            private TMP_Text _texto;
            private void Awake() { _texto = GetComponent<TMP_Text>(); }
            private void Update() {
                _t += Time.unscaledDeltaTime;
                var r = Mathf.Clamp01((_t - Retraso) / 0.8f);
                var e = 1 - Mathf.Pow(1 - r, 3);
                _texto.text = Mathf.Lerp(Desde, Hasta, e).ToString("0") + Sufijo;
                if (r >= 1) enabled = false;
            }
        }

        /// <summary>El relleno de una barra divergente, desde el centro.</summary>
        private sealed class CrecimientoDivergente : MonoBehaviour {
            public RectTransform Relleno;
            public float Valor, Retraso;
            private float _t;
            private void Update() {
                _t += Time.unscaledDeltaTime;
                var r = Mathf.Clamp01((_t - Retraso) / 0.7f);
                var e = 1 - Mathf.Pow(1 - r, 3);
                var x = 0.5f + 0.5f * Valor * e;
                Relleno.anchorMin = new Vector2(Mathf.Min(0.5f, x), 0);
                Relleno.anchorMax = new Vector2(Mathf.Max(0.5f, x), 1);
                Relleno.offsetMin = Relleno.offsetMax = Vector2.zero;
                if (r >= 1) enabled = false;
            }
        }

        /// <summary>Late: el dia de hoy en la tira, o (suave) el boton de seguir.</summary>
        private sealed class LatidoDeResumen : MonoBehaviour {
            public bool Suave;
            private void Update() {
                var a = Suave ? 0.025f : 0.18f;
                transform.localScale = Vector3.one * (1 + a * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (Suave ? 3f : 4f))));
            }
        }
    }
}
