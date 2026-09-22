using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Sesion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El Dashboard de Lecciones: lo que el nivel enseño, leido del DebriefReport. Es el unico sitio donde se
    /// ven los veredictos y sus porques — durante el nivel no se enseñan, para que se decida por el mundo y no
    /// por la nota. Siete secciones:
    ///   1 · el resultado          2 · el riesgo latente (sus 4 barras)     3 · las competencias (radar)
    ///   4 · la metodologia        5 · la arquitectura                       6 · cada decision, con su porque
    ///   7 · la cadena causal: que decision trajo que consecuencia
    /// </summary>
    public sealed class PantallaDeLecciones : Pantalla {
        public DebriefReport Reporte;
        public Action AlSeguir;

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            var r = Reporte;
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 0.75f));
            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 0);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "DASHBOARD DE LECCIONES · " + (r.NivelNombre ?? r.NivelId).ToUpperInvariant(), EstiloTexto.Pequeno, Tema.cian);
            Ui.Texto(titulos, r.CumpleUmbralesDeExito ? "Nivel superado" : "Nivel no superado", EstiloTexto.Titulo);
            Ui.Boton(cabecera, "Continuar", () => AlSeguir?.Invoke(), VarianteBoton.Primario);

            RectTransform contenido;
            var scroll = Ui.Desplazable(marco, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            var fila1 = Ui.Fila(contenido, "Fila 1", Tema.margen * 0.75f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(Resultado(fila1, r), flexAncho: 1);
            UiKit.Tamano(Riesgo(fila1), flexAncho: 1);
            UiKit.Tamano(Competencias(fila1, r), flexAncho: 1);

            var fila2 = Ui.Fila(contenido, "Fila 2", Tema.margen * 0.75f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(Metodologia(fila2, r), flexAncho: 1);
            UiKit.Tamano(Arquitectura(fila2, r), flexAncho: 1);

            Decisiones(contenido, r);
            Cadena(contenido, r);
        }

        // 1
        private RectTransform Resultado(Transform padre, DebriefReport r) {
            var t = Ui.Tarjeta(padre, "1 · El resultado");
            var l = r.Lanzamiento;
            if (l != null) {
                Ui.Texto(t, l.Exito ? "El lanzamiento salió bien." : "El lanzamiento falló.", EstiloTexto.Cuerpo, l.Exito ? Tema.cian : Tema.rojo);
                Ui.Texto(t, $"Entregado {l.AlcanceEntregado:0} de {l.AlcanceComprometido:0} puntos · {l.DefectosEscapados} defectos llegaron al cliente.",
                         EstiloTexto.Pequeno, Tema.texto);
            }
            if (r.CumpleUmbralesDeExito) Ui.Texto(t, "Cumple todos los umbrales de éxito del nivel.", EstiloTexto.Pequeno, Tema.texto);
            foreach (var u in r.UmbralesFallados) Ui.Texto(t, "· " + u, EstiloTexto.Pequeno, Tema.amarillo);
            return t;
        }

        // 2
        private RectTransform Riesgo(Transform padre) {
            var t = Ui.Tarjeta(padre, "2 · El riesgo latente");
            var brief = App.Sesion != null ? App.Sesion.BriefDeHoy : null;
            var d = brief != null ? brief.Derivadas : null;
            if (d == null) {
                Ui.Texto(t, "Sin datos del último día.", EstiloTexto.Pequeno);
                return t;
            }
            Ui.Texto(t, $"Al empezar el último día: {d.RiesgoLatente:0} / 100", EstiloTexto.Cuerpo);
            Ui.Texto(t, "De dónde venía:", EstiloTexto.Pequeno);
            var partes = new[] {
                new KeyValuePair<string, double>("Cansancio", d.RiesgoPorCansancio),
                new KeyValuePair<string, double>("Deuda técnica", d.RiesgoPorDeuda),
                new KeyValuePair<string, double>("Poca cobertura de pruebas", d.RiesgoPorCobertura),
                new KeyValuePair<string, double>("Poca documentación", d.RiesgoPorDocumentacion)
            };
            var maximo = Math.Max(1.0, partes.Max(p => p.Value));
            foreach (var p in partes) {
                var fila = Ui.Fila(t);
                Ui.Texto(fila, p.Key, EstiloTexto.Pequeno, Tema.texto);
                Ui.Resorte(fila);
                Ui.Texto(fila, p.Value.ToString("0.#"), EstiloTexto.Pequeno, Tema.texto);
                Ui.Barra(t, (float)(p.Value / maximo), p.Value >= maximo - 0.001 ? Tema.mostaza : Tema.cian);
            }
            return t;
        }

        // 3
        private RectTransform Competencias(Transform padre, DebriefReport r) {
            var t = Ui.Tarjeta(padre, "3 · Tus competencias");
            var oas = r.Competencia == null ? new List<string>() : r.Competencia.PorObjetivo.Keys.OrderBy(k => k).ToList();
            if (oas.Count == 0) {
                Ui.Texto(t, "Todavía no hay decisiones evaluadas.", EstiloTexto.Pequeno);
                return t;
            }
            if (oas.Count >= 3) {
                var centro = Ui.Fila(t, alineacion: TextAnchor.MiddleCenter);
                Ui.Radar(centro, oas.Select(o => (float)r.Competencia.PuntuacionDe(o)).ToArray(),
                         oas.Select(o => o.Replace("OA-", "")).ToArray(), 260);
                Ui.Espaciador(t, 36);
            }
            foreach (var oa in oas) {
                var c = r.Competencia.PorObjetivo[oa];
                Ui.Texto(t, $"{oa}: {c.Correctas} bien · {c.Aceptables} regular · {c.Incorrectas} mal", EstiloTexto.Pequeno, Tema.texto);
            }
            return t;
        }

        // 4
        private RectTransform Metodologia(Transform padre, DebriefReport r) {
            var t = Ui.Tarjeta(padre, "4 · La metodología");
            var m = r.Metodologia;
            if (m == null) return t;
            Ui.Texto(t, m.MetodologiaNombre, EstiloTexto.Subtitulo, Tema.texto);
            Ui.Texto(t, m.EraAdecuada
                ? "Era una metodología adecuada para este proyecto."
                : "No era la metodología adecuada para este proyecto.", EstiloTexto.Cuerpo, m.EraAdecuada ? Tema.cian : Tema.amarillo);
            var metodologia = App.Catalogo.Metodologias.ContainsKey(m.MetodologiaId ?? "") ? App.Catalogo.Metodologias[m.MetodologiaId] : null;
            var motivo = metodologia != null ? metodologia.TextoDe(m.RazonElegida) : m.RazonElegida;
            Ui.Texto(t, $"Tu motivo: «{motivo}»", EstiloTexto.Pequeno, Tema.texto);
            Ui.Texto(t, m.RazonValida ? "Es un buen motivo para elegirla."
                      : m.RazonTrampa ? "Es un motivo trampa: se puede acertar por la razón equivocada, y aquí se distingue."
                      : "No es de los motivos que justifican esta metodología.", EstiloTexto.Pequeno,
                     m.RazonValida ? Tema.cian : m.RazonTrampa ? Tema.rojo : Tema.amarillo);
            if (m.Practicas.Count > 0) {
                Ui.Texto(t, "Sus prácticas, contra lo que pasó:", EstiloTexto.Pequeno);
                foreach (var p in m.Practicas.Where(x => x.Evaluable))
                    Ui.Texto(t, (p.Cumple ? "✓ " : "✗ ") + p.Descripcion + (string.IsNullOrEmpty(p.Razon) ? "" : " — " + p.Razon),
                             EstiloTexto.Pequeno, p.Cumple ? Tema.texto : Tema.amarillo);
            }
            return t;
        }

        // 5
        private RectTransform Arquitectura(Transform padre, DebriefReport r) {
            var t = Ui.Tarjeta(padre, "5 · La arquitectura");
            var perfil = App.Catalogo.Niveles.ContainsKey(r.NivelId) ? App.Catalogo.Niveles[r.NivelId] : null;
            var opcion = perfil == null ? null : perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == r.ArquitecturaElegida);
            Ui.Texto(t, opcion != null ? opcion.Nombre : r.ArquitecturaElegida, EstiloTexto.Subtitulo, Tema.texto);
            Chip(t, r.ArquitecturaVeredicto);
            if (!string.IsNullOrEmpty(r.ArquitecturaRazon)) Ui.Texto(t, r.ArquitecturaRazon, EstiloTexto.Cuerpo);
            return t;
        }

        // 6
        private void Decisiones(Transform padre, DebriefReport r) {
            var t = Ui.Tarjeta(padre, "6 · Cada decisión, y por qué");
            if (r.Traza == null || r.Traza.Entradas.Count == 0) {
                Ui.Texto(t, "No hay decisiones registradas.", EstiloTexto.Pequeno);
                return;
            }
            foreach (var e in r.Traza.Entradas) {
                var bloque = Ui.PanelColumna(t, "Decision", Tema.espacio, 4, Tema.pared);
                var cabecera = Ui.Fila(bloque);
                var titulo = Ui.Texto(cabecera, (e.Dia > 0 ? $"Día {e.Dia} · " : "") + e.Titulo, EstiloTexto.Cuerpo);
                titulo.fontStyle = FontStyles.Bold;
                UiKit.Tamano(titulo, flexAncho: 1);
                Chip(cabecera, e.Veredicto);
                if (!string.IsNullOrEmpty(e.OpcionTexto) && e.OpcionTexto != e.OpcionId)
                    Ui.Texto(bloque, "Elegiste: " + e.OpcionTexto, EstiloTexto.Pequeno, Tema.texto);
                if (!string.IsNullOrEmpty(e.Razon)) Ui.Texto(bloque, e.Razon, EstiloTexto.Pequeno);
                if (!string.IsNullOrEmpty(e.NotaDeMetodologia)) Ui.Texto(bloque, e.NotaDeMetodologia, EstiloTexto.Pequeno, Tema.cianClaro);
            }
        }

        // 7
        private void Cadena(Transform padre, DebriefReport r) {
            var t = Ui.Tarjeta(padre, "7 · Qué trajo qué");
            if (r.CadenaCausal == null || r.CadenaCausal.Count == 0)
                Ui.Texto(t, "Ninguna decisión de este nivel desencadenó otra.", EstiloTexto.Pequeno);
            else
                foreach (var eslabon in r.CadenaCausal) Ui.Texto(t, "· " + eslabon, EstiloTexto.Cuerpo);
            if (r.BeatsPerdidos != null && r.BeatsPerdidos.Count > 0)
                Ui.Texto(t, "Escenas que no llegaste a ver: " + string.Join(", ", r.BeatsPerdidos), EstiloTexto.Pequeno, Tema.amarillo);
        }

        private void Chip(Transform padre, string veredicto) {
            if (string.IsNullOrEmpty(veredicto)) return;
            var fila = padre.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>() != null ? padre : Ui.Fila(padre);
            Ui.Chip(fila, Textos.Veredicto(veredicto), Textos.ColorDe(Tema, veredicto));
        }
    }
}
