using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Tablero;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Una persona del equipo como tarjeta (2026-11-08), hermana de las cartas de la Fase 2 pero con el aire de la
    /// Fase 1 (cristal oscuro, filo neon, sello): su rostro, nombre, rol y cuatro barras de habilidad. Al dejar el
    /// raton encima un momento se da la vuelta y enseña su perfil (quien es, en que es fuerte y floja, en que te ayuda).
    /// Si hay que elegir equipo, pulsarla la mete o la saca: se estampa «EN EL EQUIPO»; si no cabe, se sacude.
    /// </summary>
    public sealed class TarjetaDeMiembro : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        private NexusTheme _t;
        private RectTransform _cuerpo, _anverso, _reverso, _sello;
        private CanvasGroup _grupo;
        private Image _borde, _halo;
        private bool _dentro;
        private float _hover, _giro, _entrada, _tSello = 9, _sacudida = 9, _quieto;
        private Func<bool> _alPulsar;

        /// <summary>Esta en el equipo (lleva el sello).</summary>
        public bool Dentro { get; private set; }
        /// <summary>Se puede elegir: con false, pulsarla no hace nada (el equipo ya esta decidido).</summary>
        public bool Seleccionable { get; set; }

        private static readonly string[] Orden = { "interfaz", "datos", "logica", "pruebas" };
        private static readonly string[] Nombres = { "Interfaz", "Datos", "Lógica", "Pruebas" };

        /// <summary>
        /// 'm' null = la protagonista (trabajas sola). 'alPulsar' devuelve si se acepto el cambio (false = se sacude).
        /// </summary>
        public static TarjetaDeMiembro Crear(UiKit ui, Transform padre, MiembroDelEquipo m, string textoDeLaHoja, IList<string> ayudaEn,
                                             bool dentro, Func<bool> alPulsar, float retraso) {
            var raiz = ui.Nodo(padre, "Tarjeta " + (m != null ? m.Nombre : "Tú"));
            var c = raiz.gameObject.AddComponent<TarjetaDeMiembro>();
            c.Construir(ui, raiz, m, textoDeLaHoja, ayudaEn, dentro, alPulsar, retraso);
            return c;
        }

        private void Construir(UiKit ui, RectTransform raiz, MiembroDelEquipo m, string textoDeLaHoja, IList<string> ayudaEn,
                               bool dentro, Func<bool> alPulsar, float retraso) {
            _t = ui.Tema; _alPulsar = alPulsar; _entrada = -retraso; Dentro = dentro;
            var zona = raiz.gameObject.AddComponent<Image>();
            zona.color = Color.clear;

            _cuerpo = ui.Nodo(raiz, "Cuerpo");
            UiKit.Rellenar(_cuerpo);
            var fondo = _cuerpo.gameObject.AddComponent<Image>();
            ui.Fondo(fondo, NexusTheme.Alfa(_t.bg950, 0.72f), NexusTheme.RadioLg);
            fondo.raycastTarget = false;
            _halo = ui.Halo(_cuerpo, NexusTheme.Alfa(_t.cyan, 0), NexusTheme.RadioLg);
            _borde = ui.Borde(_cuerpo, _t.lineStrong, NexusTheme.RadioLg, 1.5f);
            _grupo = _cuerpo.gameObject.AddComponent<CanvasGroup>();
            _grupo.blocksRaycasts = false;

            var nombre = m != null ? m.Nombre : "Tú";
            // ---- anverso
            _anverso = ui.Nodo(_cuerpo, "Anverso");
            UiKit.Rellenar(_anverso);
            var hueco = ui.Nodo(_anverso, "Hueco del rostro");
            hueco.anchorMin = new Vector2(0, 0.60f); hueco.anchorMax = new Vector2(1, 0.96f);
            hueco.offsetMin = hueco.offsetMax = Vector2.zero;
            var rostro = RostroDePersonaje.Crear(ui, hueco, RostroDePersonaje.FotoDe(m), nombre, _t.Px(84));
            rostro.anchorMin = rostro.anchorMax = rostro.pivot = new Vector2(0.5f, 0.5f);
            rostro.anchoredPosition = Vector2.zero;
            rostro.GetComponent<LayoutElement>().ignoreLayout = true;
            var titulo = TextoEnCaja.Crear(ui, _anverso, nombre, EstiloTexto.Encabezado, _t.ink, TextAlignmentOptions.Center, _t.Px(18),
                                           new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.60f));
            titulo.fontStyle = FontStyles.Bold;
            TextoEnCaja.Crear(ui, _anverso, m != null ? m.Rol : "trabajas solo", EstiloTexto.Pequeno, _t.cyan, TextAlignmentOptions.Center,
                              _t.Px(13), new Vector2(0.04f, 0.40f), new Vector2(0.96f, 0.49f));
            if (m != null && m.Habilidades != null) {
                for (var i = 0; i < Orden.Length; i++) {
                    double v;
                    if (!m.Habilidades.TryGetValue(Orden[i], out v)) v = 1;
                    Barra(ui, _anverso, Nombres[i], Mathf.Clamp01((float)((v - 0.5) / 1.0)), 0.05f + (3 - i) * 0.085f);
                }
            } else {
                TextoEnCaja.Crear(ui, _anverso, "Sin hojas de vida: lo sacas tú, más despacio.", EstiloTexto.Pequeno, _t.warning,
                                  TextAlignmentOptions.Center, _t.Px(13), new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.36f));
            }

            // el sello «EN EL EQUIPO»
            _sello = ui.Nodo(_anverso, "Sello");
            _sello.anchorMin = _sello.anchorMax = new Vector2(0.5f, 0.62f);
            _sello.sizeDelta = new Vector2(_t.Px(112), _t.Px(22));
            _sello.localEulerAngles = new Vector3(0, 0, -6);
            var fs = _sello.gameObject.AddComponent<Image>();
            ui.Fondo(fs, NexusTheme.Alfa(_t.bg950, 0.92f), 4);
            fs.raycastTarget = false;
            ui.Borde(_sello, _t.cyan, 4, 2);
            TextoEnCaja.Crear(ui, _sello, "EN EL EQUIPO", EstiloTexto.Leyenda, _t.cyan, TextAlignmentOptions.Center, _t.Px(11),
                              Vector2.zero, Vector2.one, 2).fontStyle = FontStyles.Bold;
            _sello.localScale = dentro ? Vector3.one : Vector3.zero;

            // ---- reverso: el perfil
            _reverso = ui.Nodo(_cuerpo, "Reverso");
            UiKit.Rellenar(_reverso);
            TextoEnCaja.Crear(ui, _reverso, "PERFIL", EstiloTexto.Leyenda, _t.cyan, TextAlignmentOptions.Center, _t.Px(12),
                              new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f));
            var perfil = !string.IsNullOrEmpty(textoDeLaHoja) ? textoDeLaHoja : m != null ? (m.Descripcion ?? m.Frase ?? "") : "Trabajas tú solo.";
            var cita = TextoEnCaja.Crear(ui, _reverso, perfil, EstiloTexto.Pequeno, _t.ink, TextAlignmentOptions.Top, _t.Px(14),
                                         new Vector2(0.07f, 0.40f), new Vector2(0.93f, 0.87f));
            cita.fontStyle = FontStyles.Italic;
            if (m != null && m.Habilidades != null && m.Habilidades.Count > 0) {
                var fuerte = m.Habilidades.OrderByDescending(kv => kv.Value).First();
                var flojo = m.Habilidades.OrderBy(kv => kv.Value).First();
                TextoEnCaja.Crear(ui, _reverso, "Fuerte en " + Corto(fuerte.Key) + " · floja en " + Corto(flojo.Key), EstiloTexto.Pequeno,
                                  _t.warning, TextAlignmentOptions.Center, _t.Px(13), new Vector2(0.06f, 0.27f), new Vector2(0.94f, 0.39f));
            }
            var ayuda = ayudaEn != null && ayudaEn.Count > 0 ? "Te ayuda en el " + string.Join(" y el ", ayudaEn) + "."
                                                               : "Suma manos: con más gente, sale más trabajo al día.";
            TextoEnCaja.Crear(ui, _reverso, ayuda, EstiloTexto.Pequeno, ayudaEn != null && ayudaEn.Count > 0 ? _t.success : _t.inkMuted,
                              TextAlignmentOptions.Center, _t.Px(13), new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.26f));
            _reverso.gameObject.SetActive(false);
        }

        private static string Corto(string k) {
            for (var i = 0; i < Orden.Length; i++) if (string.Equals(Orden[i], k, StringComparison.OrdinalIgnoreCase)) return Nombres[i].ToLowerInvariant();
            return (k ?? "").Replace('_', ' ').Replace('-', ' ');
        }

        /// <summary>Una linea «Interfaz ▬▬▬▭» con la barra a escala (0,5 → vacia, 1,5 → llena).</summary>
        private void Barra(UiKit ui, RectTransform padre, string nombre, float valor, float y) {
            TextoEnCaja.Crear(ui, padre, nombre, EstiloTexto.Leyenda, _t.inkMuted, TextAlignmentOptions.MidlineLeft, _t.Px(11),
                              new Vector2(0.08f, y), new Vector2(0.40f, y + 0.075f));
            var pista = ui.Nodo(padre, "Barra " + nombre);
            pista.anchorMin = new Vector2(0.42f, y + 0.02f); pista.anchorMax = new Vector2(0.92f, y + 0.055f);
            pista.offsetMin = pista.offsetMax = Vector2.zero;
            var fp = pista.gameObject.AddComponent<Image>();
            ui.Fondo(fp, NexusTheme.Alfa(_t.ink, 0.10f), 3);
            fp.raycastTarget = false;
            var lleno = ui.Nodo(pista, "Lleno");
            lleno.anchorMin = Vector2.zero; lleno.anchorMax = new Vector2(Mathf.Max(0.04f, valor), 1);
            lleno.offsetMin = lleno.offsetMax = Vector2.zero;
            lleno.pivot = new Vector2(0, 0.5f);
            var fl = lleno.gameObject.AddComponent<Image>();
            ui.Fondo(fl, valor >= 0.6f ? _t.success : valor >= 0.4f ? _t.cyan : _t.warning, 3);
            fl.raycastTarget = false;
            Aparecer.En(lleno, 0.5f + y, false);
        }

        /// <summary>Cambia el sello (con su golpe al entrar).</summary>
        public void PonerDentro(bool dentro) {
            if (dentro && !Dentro) _tSello = 0;
            Dentro = dentro;
        }

        public void Sacudir() { _sacudida = 0; }

        public void OnPointerEnter(PointerEventData e) { _dentro = true; _quieto = 0; }
        public void OnPointerExit(PointerEventData e) { _dentro = false; }
        private void OnDisable() { _dentro = false; }

        public void OnPointerClick(PointerEventData e) {
            if (!Seleccionable || _entrada < 0.3f || _alPulsar == null) return;
            if (!_alPulsar()) Sacudir();
        }

        private void Update() { Paso(Mathf.Min(Time.unscaledDeltaTime, 0.1f)); }

        private void Paso(float dt) {
            _entrada += dt;
            _quieto += dt;
            var e = 1 - Mathf.Pow(1 - Mathf.Clamp01(_entrada / 0.45f), 3);
            _hover = Mathf.MoveTowards(_hover, _dentro ? 1 : 0, dt / 0.15f);
            // Se da la vuelta si el raton se queda encima un momento (pasar por encima no la voltea).
            _giro = Mathf.MoveTowards(_giro, _dentro && _quieto > 0.45f ? 1 : 0, dt / 0.28f);
            _tSello += dt; _sacudida += dt;

            var aplasta = Mathf.Max(0.02f, Mathf.Abs(Mathf.Cos(_giro * Mathf.PI)));
            var crece = (0.96f + 0.04f * e) * (1 + 0.04f * _hover);
            _cuerpo.localScale = new Vector3(crece * aplasta, crece, 1);
            var sacude = _sacudida < 0.35f ? Mathf.Sin(_sacudida * 60f) * 10f * (1 - _sacudida / 0.35f) : 0;
            _cuerpo.anchoredPosition = new Vector2(sacude, -(1 - e) * 40 + _hover * 8);
            var reverso = _giro >= 0.5f;
            if (_anverso.gameObject.activeSelf == reverso) { _anverso.gameObject.SetActive(!reverso); _reverso.gameObject.SetActive(reverso); }

            var apagada = Seleccionable && !Dentro;
            _grupo.alpha = e * (apagada && _hover < 0.5f ? 0.6f : 1f);
            _borde.color = _sacudida < 0.35f ? _t.warning : Dentro ? _t.cyan : Color.Lerp(_t.lineStrong, _t.cyan, _hover);
            _halo.color = NexusTheme.Alfa(_t.cyan, Mathf.Max(Dentro ? 0.22f : 0, _hover * 0.3f));
            var s = Dentro ? (_tSello < 0.4f ? ActoresDeEscena.Rebote(_tSello / 0.4f) : 1f) : 0f;
            _sello.localScale = new Vector3(s, s, 1);
        }
    }
}
