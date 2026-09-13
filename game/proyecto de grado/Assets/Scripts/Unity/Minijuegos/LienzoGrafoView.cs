using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Grafo;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Minijuegos {
    /// <summary>
    /// Contrato del lienzo. PantallaDetectarView no sabe si debajo hay un grafo,
    /// un diagrama o un archivo de codigo: por eso el mismo componente sirve
    /// para las 10 escenas de "marcar sobre un lienzo".
    /// </summary>
    public interface ILienzo {
        void Construir(MinijuegoDef def, MinijuegoContext ctx, Action<string> alPinchar);
        void Refrescar(IReadOnlyList<string> seleccion, IReadOnlyDictionary<string, string> etiquetaPorCommit);
        void Resaltar(IEnumerable<string> commits);
        void Congelar(IEnumerable<string> commitsDelRecuadro);
    }

    /// <summary>
    /// Va en el GameObject "Lienzo" que montas en el editor. No crea paneles ni
    /// fondos: solo instancia una fila, un punto y una arista por commit dentro
    /// del contenedor que le des, con los prefabs que le arrastres.
    /// </summary>
    public sealed class LienzoGrafoView : MonoBehaviour, ILienzo {
        [Header("Contenedores (los montas tu en la escena)")]
        [SerializeField] private RectTransform _contenido;     // el Content del ScrollRect
        [SerializeField] private RectTransform _capaAristas;   // debajo de las filas
        [SerializeField] private RectTransform _capaPuntos;    // encima de las aristas

        [Header("Prefabs")]
        [SerializeField] private FilaCommitView _prefabFila;
        [SerializeField] private Image _prefabPunto;
        [SerializeField] private Image _prefabPuntoMerge;      // opcional: commits con dos padres
        [SerializeField] private Image _prefabArista;
        [SerializeField] private RectTransform _prefabRecuadro;

        [Header("Metrica")]
        [SerializeField] private float _altoFila = 30f;
        [SerializeField] private float _anchoCarril = 26f;
        [SerializeField] private float _margenIzquierdo = 16f;
        [SerializeField] private float _tamPunto = 11f;
        [SerializeField] private float _tamPuntoSeleccionado = 17f;

        [Header("Estilo")]
        [SerializeField] private PaletaNexus _paleta;
        [SerializeField] private string _formatoFecha = "dd MMM HH:mm";

        private MinijuegoDef _def;
        private GrafoLayout.Resultado _layout;
        private readonly Dictionary<string, FilaCommitView> _filas = new Dictionary<string, FilaCommitView>();
        private readonly Dictionary<string, Image> _puntos = new Dictionary<string, Image>();
        private readonly Dictionary<string, Color> _colorPorAutor = new Dictionary<string, Color>();
        private bool _congelado;

        public void Construir(MinijuegoDef def, MinijuegoContext ctx, Action<string> alPinchar) {
            _def = def;
            _layout = GrafoLayout.Calcular(def.Artefacto);
            _contenido.sizeDelta = new Vector2(_contenido.sizeDelta.x, _layout.Filas * _altoFila + 20f);

            AsignarColoresAutor(ctx);

            foreach (var nodo in _layout.Nodos) {
                var c = _def.Artefacto.Commits.First(x => x.Id == nodo.CommitId);

                var fila = Instantiate(_prefabFila, _contenido);
                fila.Rellenar(c, FechaCorta(c.Fecha), id => { if (!_congelado) alPinchar(id); });
                var rt = (RectTransform)fila.transform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -nodo.Fila * _altoFila);
                _filas[c.Id] = fila;

                Color colAutor;
                if (_colorPorAutor.TryGetValue(c.Autor, out colAutor))
                    fila.PintarAutor(colAutor);

                // Los commits con dos padres son fusiones: anillo hueco en vez de punto relleno.
                var esMerge = c.Padres != null && c.Padres.Count > 1;
                var prefab = (esMerge && _prefabPuntoMerge != null) ? _prefabPuntoMerge : _prefabPunto;

                var punto = Instantiate(prefab, _capaPuntos);
                punto.color = _paleta.Rama(ColorDeRama(c.Rama));
                var rtP = (RectTransform)punto.transform;
                rtP.anchoredPosition = Posicion(nodo);
                rtP.sizeDelta = new Vector2(_tamPunto, _tamPunto);
                _puntos[c.Id] = punto;
            }

            foreach (var a in _layout.Aristas) DibujarArista(a);
        }

        /// <summary>
        /// Reparte un color por autor, pero SOLO con andamiaje 3. Con niveles bajos
        /// el jugador tiene que leer las iniciales: si cada autor lleva su color, el
        /// tramo reescrito salta a la vista como una banda maciza y no queda nada que
        /// descubrir. El barajado usa la semilla de la partida y no UnityEngine.Random,
        /// para no romper el determinismo del arnes de simulacion.
        /// </summary>
        private void AsignarColoresAutor(MinijuegoContext ctx) {
            _colorPorAutor.Clear();
            if (ctx == null || ctx.NivelAndamiaje < 3) return;
            if (_paleta == null || _paleta.coloresAutor == null || _paleta.coloresAutor.Length == 0) return;

            var autores = _def.Artefacto.Commits
                .Select(c => c.Autor)
                .Distinct()
                .OrderBy(a => a, StringComparer.Ordinal)
                .ToList();

            var colores = _paleta.coloresAutor.ToList();
            var rnd = new System.Random(ctx.Semilla);
            for (int i = colores.Count - 1; i > 0; i--) {
                int j = rnd.Next(i + 1);
                var tmp = colores[i];
                colores[i] = colores[j];
                colores[j] = tmp;
            }

            for (int i = 0; i < autores.Count; i++)
                _colorPorAutor[autores[i]] = colores[i % colores.Count];
        }

        private void DibujarArista(GrafoLayout.Arista a) {
            var hijo = _layout.Por(a.HastaId);
            var padre = _layout.Por(a.DesdeId);
            if (hijo == null || padre == null) return;

            var pA = Posicion(hijo);
            var pB = Posicion(padre);
            var delta = pB - pA;

            var img = Instantiate(_prefabArista, _capaAristas);
            var rama = _def.Artefacto.Commits.First(c => c.Id == a.HastaId).Rama;
            img.color = _paleta.Rama(ColorDeRama(rama));
            var rt = (RectTransform)img.transform;
            rt.anchoredPosition = (pA + pB) / 2f;
            rt.sizeDelta = new Vector2(delta.magnitude, rt.sizeDelta.y);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        public void Refrescar(IReadOnlyList<string> seleccion,
                              IReadOnlyDictionary<string, string> etiquetaPorCommit) {
            var ramasVivas = new HashSet<string>(
                seleccion.Select(id => _def.Artefacto.Commits.First(c => c.Id == id).Rama));

            foreach (var kv in _puntos) {
                var c = _def.Artefacto.Commits.First(x => x.Id == kv.Key);
                bool viva = ramasVivas.Count == 0 || ramasVivas.Contains(c.Rama);
                var baseColor = _paleta.Rama(ColorDeRama(c.Rama));
                kv.Value.color = viva ? baseColor : baseColor * _paleta.atenuacionRamaInactiva;
                var rt = (RectTransform)kv.Value.transform;
                float t = seleccion.Contains(kv.Key) ? _tamPuntoSeleccionado : _tamPunto;
                rt.sizeDelta = new Vector2(t, t);
            }

            foreach (var kv in _filas) {
                if (etiquetaPorCommit.ContainsKey(kv.Key)) kv.Value.PintarFondo(_paleta.filaMarcada);
                else if (seleccion.Contains(kv.Key)) kv.Value.PintarFondo(_paleta.filaSeleccionada);
                else kv.Value.PintarFondo(_paleta.filaNormal);
            }
        }

        public void Resaltar(IEnumerable<string> commits) {
            foreach (var id in commits)
                if (_filas.ContainsKey(id)) _filas[id].PintarFondo(_paleta.filaResaltada);
        }

        public void Congelar(IEnumerable<string> commitsDelRecuadro) {
            _congelado = true;
            foreach (var f in _filas.Values) f.Bloquear();

            var nodos = commitsDelRecuadro.Select(id => _layout.Por(id)).Where(n => n != null).ToList();
            if (nodos.Count == 0 || _prefabRecuadro == null) return;

            float arriba = nodos.Min(n => n.Fila) * _altoFila;
            float abajo = (nodos.Max(n => n.Fila) + 1) * _altoFila;

            var marco = Instantiate(_prefabRecuadro, _capaAristas);
            marco.anchoredPosition = new Vector2(marco.anchoredPosition.x, -arriba);
            marco.sizeDelta = new Vector2(marco.sizeDelta.x, abajo - arriba);
        }

        private Vector2 Posicion(GrafoLayout.Nodo n) {
            return new Vector2(_margenIzquierdo + n.Carril * _anchoCarril,
                               -(n.Fila * _altoFila + _altoFila / 2f));
        }

        private string ColorDeRama(string ramaId) {
            var r = _def.Artefacto.Ramas.FirstOrDefault(x => x.Id == ramaId);
            return r?.Color ?? "gris";
        }

        private string FechaCorta(string iso) {
            DateTime d;
            if (!DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return iso;
            return d.ToString(_formatoFecha, CultureInfo.InvariantCulture).ToUpperInvariant();
        }
    }
}
