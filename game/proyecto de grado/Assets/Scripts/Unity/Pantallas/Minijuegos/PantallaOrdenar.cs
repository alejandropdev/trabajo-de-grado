using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Ordenar;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>Hace arrastrable una tarjeta del backlog: la sigue al puntero y, al soltarla, dice a que altura cayo.</summary>
    public sealed class TarjetaArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        public Action<float> AlSoltar;     // la y (hacia abajo, en coordenadas de la lamina) donde quedo su centro
        private RectTransform _rt;
        private Vector2 _inicio;

        public void OnBeginDrag(PointerEventData e) {
            _rt = (RectTransform)transform;
            _inicio = _rt.anchoredPosition;
            _rt.SetAsLastSibling();
            _rt.localRotation = Quaternion.Euler(0, 0, 1.2f);
        }

        public void OnDrag(PointerEventData e) {
            var padre = (RectTransform)_rt.parent;
            Vector2 local, anterior;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position, e.pressEventCamera, out local);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, e.position - e.delta, e.pressEventCamera, out anterior);
            _rt.anchoredPosition += new Vector2(0, (local - anterior).y);
        }

        public void OnEndDrag(PointerEventData e) {
            _rt.localRotation = Quaternion.identity;
            var y = -_rt.anchoredPosition.y + _rt.sizeDelta.y / 2;
            AlSoltar?.Invoke(y);
        }
    }

    /// <summary>
    /// V3 · Ordenar. El backlog como un tablero: tarjetas que se arrastran (o se suben y bajan con ▲▼), un medidor
    /// de esfuerzo a la izquierda que se llena con lo que entra, y la linea de capacidad: lo de abajo se queda fuera.
    /// Las dependencias no se ven hasta que se rompen: al entregar, cada una rota se dibuja como una flecha roja.
    /// A la derecha, lo que pide el cliente y las tres respuestas.
    /// </summary>
    public sealed class PantallaOrdenar : PantallaDeMinijuego {
        private List<string> _orden;
        private string _respuesta;
        private Lamina _lamina;
        private RectTransform _respuestas;
        private TMP_Text _resumen;
        private Button _entregar;

        private OrdenarCfg Cfg { get { return Def.Ordenar; } }
        private const float Fila = 70, Y0 = 20, Izq = 130, Ancho = 1180;

        protected override void ConstruirJuego(RectTransform cuerpo) {
            _orden = Cfg.Tarjetas.Select(t => t.Id).ToList();
            _lamina = NuevoLienzo(cuerpo, "El backlog · arrastra las tarjetas · arriba lo primero", Izq + Ancho + 20, Y0 + (Cfg.Tarjetas.Count + 1) * Fila + 40);

            var lado = Ui.Columna(cuerpo, "Cliente", Tema.espacio);
            UiKit.Tamano(lado, ancho: 460, flexAlto: 1);
            var peticion = Ui.PanelColumna(lado, "Peticion", Tema.margen * 0.75f, Tema.espacio);
            peticion.gameObject.AddComponent<Outline>().effectColor = Tema.mostaza;
            Ui.Texto(peticion, ("Lo que pide " + (Def.Presentacion.QuienEspera ?? "el cliente")).ToUpperInvariant(), EstiloTexto.Pequeno, Tema.mostaza);
            Ui.Texto(peticion, "«" + (Cfg.Peticion ?? "") + "»", EstiloTexto.Cuerpo);
            var r = Ui.Tarjeta(lado, "¿Qué le contestas?");
            UiKit.Tamano(r, flexAlto: 1);
            _respuestas = Ui.Columna(r, "Respuestas", 8);
            var sprint = Ui.Tarjeta(lado, "En el sprint");
            _resumen = Ui.Texto(sprint, "", EstiloTexto.Cuerpo);
            Ui.Texto(sprint, "Algunas tarjetas necesitan otra antes. Si las pones al revés, lo verás al entregar.", EstiloTexto.Pequeno);
            _entregar = Ui.Boton(lado, "Entregar el orden y la respuesta", Entregar, VarianteBoton.Primario);
            Repintar();
        }

        public override void Repintar() {
            if (_orden == null || Resultado != null) return;
            PintarBacklog(_lamina, false);

            var entran = OrdenarEvaluador.Entran(Cfg, _orden);
            var porId = Cfg.Tarjetas.ToDictionary(t => t.Id);
            _resumen.text = $"Esfuerzo {entran.Sum(id => porId[id].Esfuerzo)} de {Cfg.Capacidad} · valor {entran.Sum(id => porId[id].Valor)}";

            UiKit.Vaciar(_respuestas);
            foreach (var clave in new[] { "obedecer", "rechazar", "negociar" }) {
                Respuesta resp;
                if (!Cfg.Respuestas.TryGetValue(clave, out resp)) continue;
                var c = clave;
                var boton = Ui.BotonDeOpcion(_respuestas, resp.Texto, Textos.Humanizar(c), () => { _respuesta = c; Repintar(); });
                Ui.Resaltar(boton, _respuesta == c);
            }
            _entregar.interactable = _respuesta != null;
        }

        private float YDe(int indice, int linea) { return Y0 + indice * Fila + (indice >= linea ? 44 : 0); }

        private void PintarBacklog(Lamina l, bool resultado) {
            for (var i = l.Raiz.childCount - 1; i >= 0; i--)
                if (l.Raiz.GetChild(i).gameObject != l.Dibujo.gameObject) Destroy(l.Raiz.GetChild(i).gameObject);
            l.Dibujo.Limpiar();

            var porId = Cfg.Tarjetas.ToDictionary(t => t.Id);
            var entran = OrdenarEvaluador.Entran(Cfg, _orden);
            var linea = _orden.FindIndex(id => !entran.Contains(id));
            if (linea < 0) linea = _orden.Count;

            // el medidor de esfuerzo: la columna entera es TODO el backlog; se llena con lo que entra
            var total = Cfg.Tarjetas.Sum(t => t.Esfuerzo);
            var alto = _orden.Count * Fila + 44;
            var escala = alto / total;
            l.Dibujo.Rect(40, Y0, 44, alto, Tema.hormigon, 0, Tema.hormigon, false, 8);
            var yb = Y0;
            foreach (var id in _orden.Where(entran.Contains)) {
                var h = porId[id].Esfuerzo * escala;
                l.Dibujo.Rect(40, yb, 44, h - 3, Tema.cian, 0, Tema.cian, false, 6);
                yb += h;
            }
            var yCap = Y0 + Cfg.Capacidad * escala;
            l.Dibujo.Linea(24, yCap, 100, yCap, Tema.mostaza, 4, true);
            l.Texto(20, yCap + 6, 90, 24, $"{entran.Sum(id => porId[id].Esfuerzo)}/{Cfg.Capacidad}", 16, Tema.mostazaClara, TextAlignmentOptions.Center);

            // la linea de capacidad entre las tarjetas
            var yLinea = YDe(linea, linea) - 30;
            l.Dibujo.Linea(Izq, yLinea, Izq + Ancho, yLinea, Tema.mostaza, 4, true);
            l.Texto(Izq, yLinea + 4, 700, 22, "CAPACIDAD DEL SPRINT · lo de abajo se queda fuera", 16, Tema.mostazaClara);

            var rotas = resultado ? Rotas() : new List<KeyValuePair<string, string>>();
            for (var i = 0; i < _orden.Count; i++) {
                var t = porId[_orden[i]];
                var dentro = i < linea;
                var y = YDe(i, linea);
                var tarjeta = Ui.Nodo(l.Raiz, "Tarjeta " + t.Id);
                Lamina.Colocar(tarjeta, Izq, y, Ancho, Fila - 10);
                var img = tarjeta.gameObject.AddComponent<Image>();
                img.sprite = Ui.SpriteRedondeado(); img.type = Image.Type.Sliced;
                img.color = dentro ? (Color)Tema.pared : new Color32(0x1F, 0x31, 0x37, 0xFF);
                var franja = Ui.Nodo(tarjeta, "Franja");
                Lamina.Colocar(franja, 0, 0, 6, Fila - 10);
                franja.gameObject.AddComponent<Image>().color = dentro ? Tema.cian : Tema.hormigon;

                var tenue = dentro ? 1f : 0.55f;
                var fila = new Lamina(Ui, tarjeta, Ancho, Fila - 10, "Contenido");
                UiKit.Rellenar(fila.Raiz);
                fila.Texto(18, 0, 30, Fila - 10, "≡", 22, Tema.textoTenue, TextAlignmentOptions.Center);
                fila.Texto(56, 0, 40, Fila - 10, (i + 1).ToString(), 20, Tema.textoTenue, TextAlignmentOptions.Center, null, true);
                fila.Texto(104, 0, 640, Fila - 10, t.Titulo, 20, WithAlpha(Tema.texto, tenue), TextAlignmentOptions.MidlineLeft, null, true);
                fila.Pastilla(820, (Fila - 10) / 2, "valor " + t.Valor, 16, WithAlpha(Tema.cian, tenue), Tema.textoSobreCian);
                fila.Pastilla(940, (Fila - 10) / 2, "esfuerzo " + t.Esfuerzo, 16, Tema.hormigon, Tema.texto);
                if (!resultado) {
                    var indice = i;
                    var sube = Ui.Boton(tarjeta, "▲", () => Mover(indice, indice - 1));
                    Lamina.Colocar((RectTransform)sube.transform, Ancho - 124, 8, 52, Fila - 26);
                    sube.interactable = i > 0;
                    var baja = Ui.Boton(tarjeta, "▼", () => Mover(indice, indice + 1));
                    Lamina.Colocar((RectTransform)baja.transform, Ancho - 64, 8, 52, Fila - 26);
                    baja.interactable = i < _orden.Count - 1;
                    tarjeta.gameObject.AddComponent<TarjetaArrastrable>().AlSoltar = yCentro => Soltar(indice, yCentro, linea);
                } else if (!dentro) {
                    fila.Texto(Ancho - 200, 0, 180, Fila - 10, "no entró", 16, Tema.textoTenue, TextAlignmentOptions.MidlineRight);
                }
            }

            // al entregar: cada dependencia rota, una flecha roja de la tarjeta a la que necesitaba
            foreach (var par in rotas) {
                var i1 = _orden.IndexOf(par.Key); var i2 = _orden.IndexOf(par.Value);
                var ya = YDe(i1, linea) + (Fila - 10) / 2; var yb2 = YDe(i2, linea) + (Fila - 10) / 2;
                var x = Izq + Ancho - 150;
                l.Dibujo.Curva(new Vector2(x, ya), new Vector2(x + 90, (ya + yb2) / 2), new Vector2(x, yb2), Tema.rojo, 4);
                l.Texto(x + 60, (ya + yb2) / 2 - 12, 160, 24, "necesita esta antes", 15, Tema.rojo);
            }
        }

        private static Color WithAlpha(Color c, float a) { c.a *= a; return c; }

        /// <summary>Las dependencias rotas entre las tarjetas que entran: (la que va antes de tiempo, la que necesitaba).</summary>
        private List<KeyValuePair<string, string>> Rotas() {
            var lista = new List<KeyValuePair<string, string>>();
            var entran = OrdenarEvaluador.Entran(Cfg, _orden);
            foreach (var t in Cfg.Tarjetas) {
                if (!entran.Contains(t.Id)) continue;
                foreach (var dep in t.DependeDe)
                    if (!entran.Contains(dep) || _orden.IndexOf(dep) > _orden.IndexOf(t.Id)) lista.Add(new KeyValuePair<string, string>(t.Id, dep));
            }
            return lista;
        }

        private void Soltar(int desde, float yCentro, int linea) {
            var hasta = 0;
            for (var i = 0; i < _orden.Count; i++)
                if (i != desde && yCentro > YDe(i, linea) + Fila / 2) hasta++;
            Mover(desde, Mathf.Clamp(hasta, 0, _orden.Count - 1));
        }

        private void Mover(int desde, int hasta) {
            if (hasta < 0 || hasta >= _orden.Count) { Repintar(); return; }
            var id = _orden[desde];
            _orden.RemoveAt(desde);
            _orden.Insert(hasta, id);
            Repintar();
        }

        protected override ResultadoMinijuego Evaluar() {
            // Sin respuesta al cliente, el evaluador lo da por omitido: no contestar tambien es contestar.
            return OrdenarEvaluador.Evaluar(Def, _orden, _respuesta);
        }

        protected override void PintarResultado(RectTransform zona) {
            var l = NuevoLienzo(zona, "Tu orden · en rojo, lo que necesitaba otra tarjeta antes", Izq + Ancho + 20, Y0 + (Cfg.Tarjetas.Count + 1) * Fila + 40);
            PintarBacklog(l, true);
        }
    }
}
