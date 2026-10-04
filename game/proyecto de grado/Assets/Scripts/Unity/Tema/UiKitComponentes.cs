using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Tema {
    public enum EstadoDeEquipo { Activo, Bloqueado, Inactivo, Hecho }
    public enum EstadoDePaso { Pendiente, Parcial, Hecho }
    public enum NivelDeLog { Info, Aviso, Error }

    /// <summary>Un renglon del ReviewBriefing: un glifo corto, su etiqueta fija y una o dos oraciones.</summary>
    public struct RenglonDeBriefing {
        public string Icono, Etiqueta, Texto;
        public RenglonDeBriefing(string icono, string etiqueta, string texto) { Icono = icono; Etiqueta = etiqueta; Texto = texto; }
    }

    /// <summary>
    /// Los componentes compuestos del design system que tienen equivalente en alguna pantalla del juego. Cada uno
    /// sigue las props y el README de su componente (DialogueBox, NotificationRow, StatTile…). Con el kit clasico
    /// caen en piezas sencillas, pero ninguna pantalla clasica los usa.
    /// </summary>
    public sealed partial class UiKit {
        // ================================================================ narrativa

        /// <summary>
        /// DialogueBox: retrato redondo con inicial, etiqueta del hablante sobre cyan solido y la linea en el estilo
        /// dialogo. Retrato violet para personajes narrativos fuera del flujo de trabajo, danger para una amenaza.
        /// 'hablante' null = el protagonista o el narrador: sin retrato ni etiqueta. 'continuar' muestra el ▼ que late.
        /// </summary>
        public RectTransform Dialogo(Transform padre, string hablante, string texto, out TMP_Text linea,
                                     Tono retrato = Tono.Cyan, bool continuar = false) {
            var rt = Nodo(padre, "Dialogo");
            Fondo(rt.gameObject.AddComponent<Image>(), Tema.surfaceRaised, NexusTheme.RadioLg);
            var fila = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configurar(fila, Tema.Espacio(4), Tema.Espacio(5), TextAnchor.UpperLeft);
            if (!string.IsNullOrEmpty(hablante)) Retrato(rt, Inicial(hablante), retrato);

            var cuerpo = Columna(rt, "Cuerpo", Tema.Espacio(2));
            Tamano(cuerpo, flexAncho: 1);
            if (!string.IsNullOrEmpty(hablante)) {
                var chip = ChipNexus(Fila(cuerpo, "Hablante"), hablante, Tema.cyan, Tema.onCyan);
                Estilizar(chip.GetComponentInChildren<TMP_Text>(), EstiloTexto.Etiqueta);
                chip.GetComponentInChildren<TMP_Text>().color = Tema.onCyan;
            }
            linea = Texto(cuerpo, texto, EstiloTexto.Dialogo);
            if (string.IsNullOrEmpty(hablante)) linea.fontStyle = FontStyles.Italic;
            if (continuar) {
                var pie = Fila(cuerpo, "Continua");
                Resorte(pie);
                var flecha = Texto(pie, "▼", EstiloTexto.Leyenda, Tema.cyan);
                flecha.gameObject.AddComponent<Parpadeo>();
            }
            Borde(rt, Tema.line, NexusTheme.RadioLg);
            return rt;
        }

        /// <summary>El retrato circular de un personaje: inicial en Orbitron dentro de un aro del tono.</summary>
        public RectTransform Retrato(Transform padre, string inicial, Tono tono = Tono.Cyan, float diametroWeb = 48) {
            var d = Tema.Px(diametroWeb);
            var color = tono == Tono.Neutro ? Tema.cyan : Tema.ColorDe(tono);
            var rt = Nodo(padre, "Retrato");
            var fondo = rt.gameObject.AddComponent<Image>();
            fondo.sprite = SpriteCircular();
            fondo.color = Tema.surfaceSunken;
            fondo.raycastTarget = false;
            Tamano(rt, d, d);
            var aro = Rellenar(Nodo(rt, "Aro")).gameObject.AddComponent<Image>();
            aro.sprite = Formas.Borde(d / 2f, Tema.Px(2));
            aro.color = color;
            aro.raycastTarget = false;
            var t = Texto(rt, inicial ?? "", EstiloTexto.Cuerpo, color, TextAlignmentOptions.Center);
            t.font = Tema.FuenteDisplay;
            t.fontSize = d * 0.38f;
            Rellenar((RectTransform)t.transform);
            return rt;
        }

        private static string Inicial(string nombre) {
            if (string.IsNullOrEmpty(nombre)) return "";
            var limpio = nombre.Replace("Dra. ", "").Replace("Dr. ", "").Trim();
            return limpio.Length > 0 ? char.ToUpperInvariant(limpio[0]).ToString() : "";
        }

        // ================================================================ HUD

        /// <summary>
        /// NotificationRow: punto de color, el evento en una oracion y la hora opcional. Danger = urgente y accionable
        /// ahora, Aviso = puede esperar, Cyan = informativo, Neutro = bitacora. Se apilan sin borde propio.
        /// </summary>
        public RectTransform Notificacion(Transform padre, string texto, Tono tono = Tono.Neutro, string hora = null) {
            var rt = Nodo(padre, "Notificacion");
            Fondo(rt.gameObject.AddComponent<Image>(), Tema.surfaceRaised, NexusTheme.RadioSm);
            var fila = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configurar(fila, Tema.Espacio(3), 0, TextAnchor.MiddleLeft);
            fila.padding = new RectOffset((int)Tema.Espacio(3), (int)Tema.Espacio(3), (int)Tema.Espacio(2), (int)Tema.Espacio(2));
            Punto(rt, tono == Tono.Neutro ? Tema.inkFaint : Tema.ColorDe(tono), 8);
            var t = Texto(rt, texto, EstiloTexto.Cuerpo);
            Tamano(t, flexAncho: 1);
            if (!string.IsNullOrEmpty(hora)) {
                var h = Texto(rt, hora, EstiloTexto.Leyenda, Tema.inkFaint);
                h.textWrappingMode = TextWrappingModes.NoWrap;
            }
            return rt;
        }

        /// <summary>
        /// StatTile: la cifra grande en Orbitron, su etiqueta y una nota opcional. El tono solo colorea la cifra: dejala
        /// neutra salvo que ESE numero este fuera de rango (Aviso/Peligro) o cierre una meta (Exito). Devuelve el texto
        /// de la cifra, para actualizarla.
        /// </summary>
        public TMP_Text Cifra(Transform padre, string valor, string etiqueta, string nota = null, Tono tono = Tono.Neutro) {
            var rt = Nodo(padre, "Cifra " + etiqueta);
            Fondo(rt.gameObject.AddComponent<Image>(), Tema.surfaceRaised, NexusTheme.RadioMd);
            var col = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(col, Tema.Px(2), Tema.Espacio(4), TextAnchor.UpperLeft);
            Tamano(rt, alto: null).minWidth = Tema.Px(92);
            var v = Texto(rt, valor, EstiloTexto.Cuerpo, tono == Tono.Neutro ? Tema.ink : Tema.ColorDe(tono));
            v.font = Tema.FuenteDisplay;
            v.fontSize = Tema.Px(22);
            v.textWrappingMode = TextWrappingModes.NoWrap;
            Texto(rt, etiqueta, EstiloTexto.Pequeno);
            if (!string.IsNullOrEmpty(nota)) Texto(rt, nota, EstiloTexto.Leyenda, Tema.inkFaint);
            Borde(rt, Tema.line, NexusTheme.RadioMd);
            return v;
        }

        /// <summary>
        /// TimerChip: el reloj de una escena contrarreloj. Cyan es tiempo normal; Aviso en los ultimos segundos; Peligro
        /// (con resplandor) solo si agotar el tiempo tiene consecuencia real.
        /// </summary>
        public TemporizadorView Temporizador(Transform padre, string valor, Tono tono = Tono.Cyan) {
            var rt = Nodo(padre, "Temporizador");
            Fondo(rt.gameObject.AddComponent<Image>(), Tema.surfaceRaised, NexusTheme.RadioSm);
            var fila = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configurar(fila, Tema.Espacio(1), 0, TextAnchor.MiddleCenter);
            fila.padding = new RectOffset((int)Tema.Espacio(3), (int)Tema.Espacio(3), (int)Tema.Espacio(1), (int)Tema.Espacio(1));
            var t = Texto(rt, valor, EstiloTexto.Cuerpo, Tema.cyan, TextAlignmentOptions.Center);
            t.font = Tema.FuenteInterfazNegrita;
            t.fontSize = Tema.Px(18);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var halo = Halo(rt, Tema.danger, NexusTheme.RadioSm, false);
            var borde = Borde(rt, Tema.line, NexusTheme.RadioSm);
            var vista = rt.gameObject.AddComponent<TemporizadorView>();
            vista.Configurar(Tema, t, borde, halo);
            vista.Mostrar(valor, tono);
            return vista;
        }

        /// <summary>
        /// RosterRow: avatar cuadrado con inicial, nombre y rol apilados, y un punto de estado que es la unica señal de
        /// color: Activo (success), Bloqueado (danger), Inactivo (sin actividad hoy), Hecho (cyan).
        /// </summary>
        public RectTransform FilaDeEquipo(Transform padre, string nombre, string rol, EstadoDeEquipo estado) {
            var rt = Fila(padre, "Equipo " + nombre, Tema.Espacio(3));
            var avatar = Nodo(rt, "Avatar");
            Fondo(avatar.gameObject.AddComponent<Image>(), Tema.surfaceSunken, NexusTheme.RadioMd);
            Tamano(avatar, Tema.Px(32), Tema.Px(32));
            var ini = Texto(avatar, Inicial(nombre), EstiloTexto.Cuerpo, Tema.ink, TextAlignmentOptions.Center);
            ini.font = Tema.FuenteDisplay;
            ini.fontSize = Tema.Px(13);
            Rellenar((RectTransform)ini.transform);
            Borde(avatar, Tema.lineStrong, NexusTheme.RadioMd);

            var id = Columna(rt, "Identidad", 0);
            Tamano(id, flexAncho: 1);
            Texto(id, nombre, EstiloTexto.Cuerpo);
            if (!string.IsNullOrEmpty(rol)) Texto(id, rol, EstiloTexto.Pequeno);

            Color color;
            switch (estado) {
                case EstadoDeEquipo.Activo: color = Tema.success; break;
                case EstadoDeEquipo.Bloqueado: color = Tema.danger; break;
                case EstadoDeEquipo.Hecho: color = Tema.cyan; break;
                default: color = Tema.inkFaint; break;
            }
            Punto(rt, color, 10);
            return rt;
        }

        /// <summary>
        /// StepList: pasos en el orden real del proceso (no se reordenan). Hecho cierra en success con el texto en
        /// ink-muted; Parcial lleva «~» en warning; Pendiente numera en neutro.
        /// </summary>
        public RectTransform Pasos(Transform padre, IList<KeyValuePair<string, EstadoDePaso>> pasos) {
            var lista = Columna(padre, "Pasos", Tema.Espacio(2));
            for (var i = 0; i < pasos.Count; i++) {
                var fila = Fila(lista, "Paso", Tema.Espacio(3));
                var estado = pasos[i].Value;
                var marca = Nodo(fila, "Marca");
                var fondo = marca.gameObject.AddComponent<Image>();
                fondo.sprite = SpriteCircular();
                fondo.raycastTarget = false;
                Tamano(marca, Tema.Px(22), Tema.Px(22));
                var aro = Rellenar(Nodo(marca, "Aro")).gameObject.AddComponent<Image>();
                aro.sprite = Formas.Borde(Tema.Px(11), Mathf.Max(1, Tema.Px(1)));
                aro.raycastTarget = false;
                string glifo; Color tinta;
                switch (estado) {
                    case EstadoDePaso.Hecho:
                        fondo.color = Tema.success; aro.color = Tema.success; glifo = "✓"; tinta = Tema.onSuccess; break;
                    case EstadoDePaso.Parcial:
                        fondo.color = Tema.warningSoft; aro.color = Tema.warning; glifo = "~"; tinta = Tema.warning; break;
                    default:
                        fondo.color = Color.clear; aro.color = Tema.lineStrong; glifo = (i + 1).ToString(); tinta = Tema.inkMuted; break;
                }
                var g = Texto(marca, glifo, EstiloTexto.Leyenda, tinta, TextAlignmentOptions.Center);
                g.font = Tema.FuenteInterfazNegrita;
                Rellenar((RectTransform)g.transform);
                var t = Texto(fila, pasos[i].Key, EstiloTexto.Cuerpo, estado == EstadoDePaso.Hecho ? Tema.inkMuted : Tema.ink);
                Tamano(t, flexAncho: 1);
            }
            return lista;
        }

        /// <summary>
        /// ReviewBriefing: el encabezado fijo de los minijuegos — «Lo que se ve», «Lo que haces», «Lo que cambia», «Cómo
        /// se cierra», en ese orden. Glifo en un aro cyan, etiqueta en cyan y el texto tenue.
        /// </summary>
        public RectTransform Briefing(Transform padre, IList<RenglonDeBriefing> renglones) {
            var lista = Columna(padre, "Briefing", Tema.Espacio(4));
            foreach (var r in renglones) {
                var fila = Fila(lista, "Renglon", Tema.Espacio(3), alineacion: TextAnchor.UpperLeft);
                var icono = Nodo(fila, "Icono");
                Tamano(icono, Tema.Px(28), Tema.Px(28));
                var aro = Rellenar(Nodo(icono, "Aro")).gameObject.AddComponent<Image>();
                aro.sprite = Formas.Borde(Tema.Px(14), Mathf.Max(1, Tema.Px(1)));
                aro.color = Tema.cyan;
                aro.raycastTarget = false;
                var g = Texto(icono, r.Icono ?? "", EstiloTexto.Leyenda, Tema.cyan, TextAlignmentOptions.Center);
                Rellenar((RectTransform)g.transform);
                var cuerpo = Columna(fila, "Cuerpo", Tema.Px(2));
                Tamano(cuerpo, flexAncho: 1);
                var e = Texto(cuerpo, r.Etiqueta, EstiloTexto.Leyenda, Tema.cyan);
                e.fontStyle = FontStyles.UpperCase;
                Texto(cuerpo, r.Texto, EstiloTexto.Cuerpo, Tema.inkMuted);
            }
            return lista;
        }

        /// <summary>
        /// LogPanel: el pozo de consola. Monoespaciado sobre surface-sunken; el nivel colorea solo la etiqueta y, en
        /// error, tambien el texto, para que el ojo encuentre primero lo que fallo.
        /// </summary>
        public RectTransform Registro(Transform padre, IEnumerable<KeyValuePair<NivelDeLog, string>> lineas) {
            var rt = Nodo(padre, "Registro");
            Fondo(rt.gameObject.AddComponent<Image>(), Tema.surfaceSunken, NexusTheme.RadioSm);
            var col = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configurar(col, 2, Tema.Espacio(3), TextAnchor.UpperLeft);
            foreach (var l in lineas) LineaDeRegistro(rt, l.Key, l.Value);
            Borde(rt, Tema.line, NexusTheme.RadioSm);
            return rt;
        }

        /// <summary>Una linea de LogPanel suelta, para registros que crecen.</summary>
        public TMP_Text LineaDeRegistro(Transform registro, NivelDeLog nivel, string texto) {
            string etiqueta; Color color;
            switch (nivel) {
                case NivelDeLog.Error: etiqueta = "[ERROR]"; color = Tema.danger; break;
                case NivelDeLog.Aviso: etiqueta = "[WARN]"; color = Tema.warning; break;
                default: etiqueta = "[INFO]"; color = Tema.cyan; break;
            }
            var cuerpo = nivel == NivelDeLog.Error ? $"<color={NexusTheme.Html(Tema.danger)}>{texto}</color>" : texto;
            var t = Texto(registro, $"<color={NexusTheme.Html(color)}>{etiqueta}</color> {cuerpo}", EstiloTexto.Mono, Tema.inkMuted);
            return t;
        }
    }

    /// <summary>El ▼ de DialogueBox: late entre 25 % y 100 % de opacidad cada 1,4 s. Es lo unico que se anima.</summary>
    public sealed class Parpadeo : MonoBehaviour {
        private Graphic _g;
        private void Awake() { _g = GetComponent<Graphic>(); }
        private void Update() {
            if (_g == null) return;
            var c = _g.color;
            c.a = Mathf.Lerp(0.25f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.4f));
            _g.color = c;
        }
    }

    /// <summary>La vista viva de un TimerChip: cambia el valor y el tono sin reconstruirlo.</summary>
    public sealed class TemporizadorView : MonoBehaviour {
        private NexusTheme _tema;
        private TMP_Text _texto;
        private Image _borde, _halo;

        internal void Configurar(NexusTheme tema, TMP_Text texto, Image borde, Image halo) {
            _tema = tema; _texto = texto; _borde = borde; _halo = halo;
        }

        public void Mostrar(string valor, Tono tono) {
            if (_texto == null) return;
            _texto.text = valor;
            var color = tono == Tono.Aviso ? _tema.warning : tono == Tono.Peligro ? _tema.danger : _tema.cyan;
            _texto.color = color;
            if (_borde != null) _borde.color = tono == Tono.Cyan || tono == Tono.Neutro ? _tema.line : color;
            if (_halo != null) _halo.gameObject.SetActive(tono == Tono.Peligro);
        }
    }
}
