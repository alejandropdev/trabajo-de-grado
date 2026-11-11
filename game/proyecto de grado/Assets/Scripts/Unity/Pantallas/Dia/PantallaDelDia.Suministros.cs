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
    /// Los suministros (2026-11-07) como tarjetas con el dibujo de lo que son: la pizza, el cafe, la Guepardex. Antes era
    /// un bloque largo por cada uno (nombre, el texto entero, la regla en mayusculas cian y un boton amarillo a lo ancho).
    /// Ahora cada tarjeta lleva arriba su dibujo con su pequeño movimiento y cuantos quedan en grande; debajo, el nombre,
    /// UNA linea y la regla en dos etiquetas (para quien, cada cuanto); abajo, un solo boton. Darselo a alguien abre un
    /// dialogo con las caras del equipo, su cansancio y si le rebotaria. El marco es el de los indicadores.
    /// </summary>
    public sealed partial class PantallaDelDia {
        private PantallaDelMuro _salaDeSuministros;
        private string _suministroAbierto;
        private bool _suministrosPintados;

        private void AbrirSuministros() {
            if (_panel != null || _muro != null || _salaDeCeremonias != null || _salaDeSuministros != null) return;
            _decidido = null;
            _suministroAbierto = null;
            _ultimoResultado = null;
            _suministrosPintados = false;
            _salaDeSuministros = AbrirPantallaDeIndicadores("Del recorrido", "Suministros", PintarSuministros, ClaveDeSuministros, m => _salaDeSuministros = m);
        }

        private string ClaveDeSuministros() {
            return string.Join(",", S.R.Suministros.Select(kv => kv.Key + kv.Value)) + "|" + S.R.UsosDeSuministros.Count + "|" +
                   Runner.PuedeMoverse + "|" + _suministroAbierto + "|" + S.MinutoDelDia + "|" + AlertasSonando().Count;
        }

        private void PintarSuministros(PantallaDelMuro m) {
            var animar = !_suministrosPintados;
            _suministrosPintados = true;
            Ui.Chip(m.Herramientas, "usar uno cuesta minutos de la jornada", NexusTheme.Alfa(Tema.cyan, 0.15f), Tema.cyan);

            var fichas = S.FichasDeSuministros();
            var mesa = Ui.Fila(m.Cuerpo, "Suministros", Tema.espacio * 2, 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(mesa, flexAncho: 1, flexAlto: 1);
            if (fichas.Count == 0) {
                var vacio = PiezasDelMuro.Vacio(Ui, mesa, "No te queda ningún suministro. Se consiguen recorriendo el edificio en la Fase 1.", 140);
                UiKit.Tamano(vacio, ancho: 700);
                return;
            }
            for (var i = 0; i < fichas.Count; i++) TarjetaDeSuministro(mesa, fichas[i], animar ? 0.08f + Mathf.Min(8, i) * 0.06f : -1);
            if (!string.IsNullOrEmpty(_suministroAbierto)) {
                var abierta = fichas.FirstOrDefault(f => f.Objeto.Id == _suministroAbierto);
                if (abierta != null) DarSuministroA(m, abierta);
                else _suministroAbierto = null;
            }
        }

        /// <summary>Una tarjeta: el dibujo y cuantos quedan arriba; el nombre, una linea y la regla en etiquetas; un boton.</summary>
        private void TarjetaDeSuministro(RectTransform mesa, FichaDeSuministro f, float retraso) {
            var o = f.Objeto;
            var s = o.Suministro;
            var color = PiezasDelMuro.ColorDeSuministro(Tema, o.Id);
            var tarjeta = PiezasDelMuro.Panel(Ui, mesa, "", color, 18);
            UiKit.Tamano(tarjeta, 380, 540);
            var grupo = tarjeta.gameObject.AddComponent<CanvasGroup>();
            var alfa = f.PorQueNo == null ? 1f : 0.6f;
            grupo.alpha = alfa;
            if (FichaArrastrable.UltimaSoltada == o.Id) {
                tarjeta.gameObject.AddComponent<AsientoDeFicha>();
                FichaArrastrable.UltimaSoltada = null;
            } else if (retraso >= 0) {
                var e = tarjeta.gameObject.AddComponent<EntradaDePieza>();
                e.Retraso = retraso;
                e.AlfaFinal = alfa;
            }

            // ---- el dibujo, con su luz de fondo y cuantos quedan
            var cuadro = Ui.Nodo(tarjeta, "Cuadro");
            UiKit.Tamano(cuadro, alto: 230, flexAncho: 1);
            var fondo = cuadro.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg950, 0.75f), NexusTheme.RadioMd);
            fondo.raycastTarget = false;
            var luz = Ui.Nodo(cuadro, "Luz");
            luz.sizeDelta = new Vector2(320, 260);
            var imgLuz = luz.gameObject.AddComponent<Image>();
            imgLuz.sprite = PlanoIlustrado.Brillo();
            imgLuz.color = NexusTheme.Alfa(f.PorQueNo == null ? color : Tema.inkFaint, 0.28f);
            imgLuz.raycastTarget = false;
            var dibujo = ImagenDeSuministro(cuadro, o.Id, 200, f.PorQueNo != null, retraso >= 0);
            UiKit.Tamano(dibujo).ignoreLayout = true;
            dibujo.anchorMin = dibujo.anchorMax = dibujo.pivot = new Vector2(0.5f, 0.5f);
            dibujo.anchoredPosition = new Vector2(0, -6);
            var cuantos = Ui.Texto(cuadro, "×" + f.Cantidad, EstiloTexto.Hero, f.PorQueNo == null ? color : Tema.inkMuted, TextAlignmentOptions.TopRight);
            cuantos.fontSize = 42;
            UiKit.Tamano(cuantos).ignoreLayout = true;
            cuantos.rectTransform.anchorMin = cuantos.rectTransform.anchorMax = cuantos.rectTransform.pivot = new Vector2(1, 1);
            cuantos.rectTransform.anchoredPosition = new Vector2(-14, -8);
            cuantos.rectTransform.sizeDelta = new Vector2(120, 56);

            // ---- el nombre, una linea y la regla en etiquetas
            var nombre = Ui.Texto(tarjeta, o.Nombre.ToUpperInvariant(), EstiloTexto.Encabezado, Tema.ink);
            nombre.font = Tema.FuenteDisplay;
            var linea = Ui.Texto(tarjeta, PiezasDelMuro.PrimeraFrase(o.Texto), EstiloTexto.Cuerpo, Tema.inkMuted);
            linea.maxVisibleLines = 3;
            linea.overflowMode = TextOverflowModes.Ellipsis;
            var reglas = Ui.Fila(tarjeta, "Reglas", 6, 0, TextAnchor.MiddleLeft);
            var paraUno = s.Objetivo == Nexus.Core.Fase1.ObjetivosDeSuministro.Miembro;
            Ui.Chip(reglas, paraUno ? "una persona" : Solo("todo el equipo", "para ti"), NexusTheme.Alfa(color, 0.16f), color);
            var cada = s.EnfriamientoDias > 0 ? $"cada {s.EnfriamientoDias} días" : s.VecesPorDia == 1 ? "1 al día" : s.VecesPorDia > 1 ? $"{s.VecesPorDia} al día" : null;
            if (cada != null) Ui.Chip(reglas, cada, NexusTheme.Alfa(Tema.inkFaint, 0.2f), Tema.inkMuted);
            if (s.ReboteUsos > 0) {
                var rebota = Ui.Chip(reglas, "rebota si abusas", NexusTheme.Alfa(Tema.warning, 0.18f), Tema.warning);
                var detalle = Ui.Texto(tarjeta, $"Más de {s.ReboteUsos} en {s.ReboteVentanaDias} días a la misma persona: el bajón es peor que antes.", EstiloTexto.Pequeno, Tema.warning);
                detalle.gameObject.SetActive(false);
                var viva = rebota.gameObject.AddComponent<PiezaViva>();
                viva.Escala = 1.05f;
                viva.Revelar = detalle.gameObject;
            }
            UiKit.Tamano(Ui.Nodo(tarjeta, "Hueco"), flexAlto: 1);

            // ---- un boton, o por que ahora no
            if (f.PorQueNo != null) {
                var no = Ui.Texto(tarjeta, f.PorQueNo, EstiloTexto.Pequeno, Tema.warning);
                no.maxVisibleLines = 2;
                no.overflowMode = TextOverflowModes.Ellipsis;
                return;
            }
            var id = o.Id;
            var minutos = s.MinutosDeUso;
            var nombreDelObjeto = o.Nombre;
            var boton = paraUno
                ? Ui.Boton(tarjeta, "Dárselo a…", () => _suministroAbierto = id, VarianteBoton.Primario)
                : Ui.Boton(tarjeta, $"{Solo("Repartir", "Usarlo")} · {minutos} min", () => UsarSuministroEnLaSala(id, null, nombreDelObjeto, minutos, tarjeta), VarianteBoton.Primario);
            UiKit.Tamano(boton, flexAncho: 1);
            boton.interactable = Runner.PuedeMoverse;
            if (!Runner.PuedeMoverse) Ui.Texto(tarjeta, "Ahora no: termina lo que tienes abierto.", EstiloTexto.Leyenda, Tema.inkFaint);
            tarjeta.gameObject.AddComponent<PiezaViva>().Escala = 1.02f;
        }

        /// <summary>Darselo a alguien: las caras del equipo con su cansancio, y quien no deberia tomar mas.</summary>
        private void DarSuministroA(PantallaDelMuro m, FichaDeSuministro f) {
            var o = f.Objeto;
            var color = PiezasDelMuro.ColorDeSuministro(Tema, o.Id);
            var hoja = Dialogo(m, color, 920, () => _suministroAbierto = null);
            var cabecera = Ui.Fila(hoja, "Cabecera", Tema.espacio, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(ImagenDeSuministro(cabecera, o.Id, 90, false, false), 90, 90);
            var titulos = Ui.Columna(cabecera, "Titulos", 4);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, $"{o.Nombre.ToUpperInvariant()} · {o.Suministro.MinutosDeUso} MIN", EstiloTexto.Leyenda, color).characterSpacing = 3;
            Ui.Texto(titulos, "¿A quién se lo das?", EstiloTexto.Titulo, Tema.ink);
            Ui.Boton(cabecera, "Cerrar", () => _suministroAbierto = null, VarianteBoton.Salida);

            var gente = RejillaDeBotones(hoja, 3, new Vector2(280, 112), "Gente");
            foreach (var ficha in S.FichasDelEquipo()) {
                var mi = ficha.Miembro;
                var mid = mi.Id;
                var rebota = S.Rebotaria(o.Id, mid);
                var motivo = S.PorQueNoSePuedeUsarSuministro(o.Id, mid);
                var persona = Ui.Fila(gente, "Persona", 12, 12, TextAnchor.MiddleLeft);
                Ui.Fondo(persona.gameObject.AddComponent<Image>(), NexusTheme.Alfa(Tema.bg950, 0.7f), NexusTheme.RadioMd);
                Ui.Borde(persona, rebota ? NexusTheme.Alfa(Tema.danger, 0.7f) : Tema.lineStrong, NexusTheme.RadioMd, 1.5f);
                Rostro(persona, FotoDe(ficha, mi.Nombre), mi.Nombre, 56);
                var datos = Ui.Columna(persona, "Datos", 4);
                UiKit.Tamano(datos, flexAncho: 1);
                Ui.Texto(datos, mi.Nombre.Split(' ')[0], EstiloTexto.Encabezado, Tema.ink).textWrappingMode = TextWrappingModes.NoWrap;
                var colorCansancio = ficha.Cansancio >= 70 ? Tema.danger : ficha.Cansancio >= 45 ? Tema.warning : Tema.success;
                Ui.Texto(datos, $"cansancio {ficha.Cansancio:0}", EstiloTexto.Leyenda, Tema.inkMuted);
                PiezasDelMuro.Barra(Ui, datos, (float)(ficha.Cansancio / 100.0), colorCansancio, 0.1f, 6);
                if (rebota) Ui.Texto(datos, "le haría el efecto contrario", EstiloTexto.Leyenda, Tema.danger);
                var boton = persona.gameObject.AddComponent<Button>();
                boton.transition = Selectable.Transition.None;
                boton.interactable = motivo == null && Runner.PuedeMoverse;
                var nombre = o.Nombre;
                var minutos = o.Suministro.MinutosDeUso;
                var id = o.Id;
                var sitio = persona;
                boton.onClick.AddListener(() => {
                    _suministroAbierto = null;
                    UsarSuministroEnLaSala(id, mid, nombre, minutos, sitio);
                });
                if (boton.interactable) persona.gameObject.AddComponent<PiezaViva>().Escala = 1.04f;
                else persona.gameObject.AddComponent<CanvasGroup>().alpha = 0.5f;
            }
        }

        /// <summary>
        /// La imagen del suministro: la MISMA que usa el inventario del 3D (Resources/Ilustraciones/suministros/&lt;id&gt;.png,
        /// copia de Art/UI/Referencias/Consumibles), flotando despacio; apagada en gris si ahora no se puede usar. Si un
        /// suministro nuevo aun no tiene imagen, su dibujo de linea (PiezasDelMuro.IlustracionDeSuministro).
        /// </summary>
        private RectTransform ImagenDeSuministro(Transform padre, string id, float lado, bool apagado, bool animar) {
            var sprite = MaterialesNexus.Ilustracion("suministros/" + id);
            if (sprite == null) return PiezasDelMuro.IlustracionDeSuministro(Ui, padre, id, lado * 0.85f, apagado, animar);
            var rt = Ui.Nodo(padre, "Imagen");
            UiKit.Tamano(rt, lado, lado);
            rt.sizeDelta = new Vector2(lado, lado);
            var marco = Ui.Nodo(rt, "Flota");
            UiKit.Rellenar(marco);
            var img = marco.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = apagado ? new Color(0.45f, 0.47f, 0.5f, 0.8f) : Color.white;
            if (!apagado) marco.gameObject.AddComponent<FlotaEnBucle>();
            if (animar) rt.gameObject.AddComponent<EntradaDePieza>().Retraso = 0.15f;
            return rt;
        }

        /// <summary>Usar un suministro: cobra sus minutos y lo cuenta en la sala («-N min» y la linea de lo que paso).</summary>
        private void UsarSuministroEnLaSala(string id, string miembro, string nombre, int minutos, RectTransform desde) {
            ResultadoDeSuministro r = null;
            var antes = S.MinutoDelDia;
            Actuar(() => { r = S.UsarSuministro(id, miembro); return r.Avance; }, $"Usaste: {nombre}.");
            if (_salaDeSuministros == null) return;
            if (r == null || S.MinutoDelDia == antes) {
                _salaDeSuministros.Avisar(_ultimoResultado ?? "Ahora no se puede.", Tema.warning);
                return;
            }
            FichaArrastrable.UltimaSoltada = id;
            var donde = desde != null ? desde.TransformPoint(desde.rect.center) : _salaDeSuministros.Capa.TransformPoint(Vector3.zero);
            _salaDeSuministros.Marcar(donde, $"-{minutos} min", Tema.cyan);
            _salaDeSuministros.Avisar(r.Texto + (r.Cambios.Count > 0 ? "  (" + Textos.Previsualizar(r.Cambios) + ")" : ""), Tema.success);
        }
    }
}
