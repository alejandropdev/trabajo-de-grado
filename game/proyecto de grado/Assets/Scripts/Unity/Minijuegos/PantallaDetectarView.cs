using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Detectar;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Minijuegos
{
    /// <summary>
    /// El componente "marcar sobre un lienzo" (10 de las 37 escenas).
    /// NO construye interfaz: todo lo visual esta montado en el prefab
    /// PantallaDetectar.prefab y llega por [SerializeField]. Este script
    /// solo rellena datos, escucha clics y devuelve un ResultadoMinijuego.
    /// </summary>
    public sealed class PantallaDetectarView : MonoBehaviour
    {
        [Header("Cabecera")]
        [SerializeField] private TMP_Text _titulo;
        [SerializeField] private TMP_Text _comoSeJuega;
        [SerializeField] private TMP_Text _reloj;
        [SerializeField] private TMP_Text _quienEspera;

        [Header("Lienzo")]
        [SerializeField] private MonoBehaviour _lienzoComponente;   // debe implementar ILienzo

        [Header("Paleta de etiquetas")]
        [SerializeField] private RectTransform _contenedorEtiquetas;
        [SerializeField] private EtiquetaBotonView _prefabEtiqueta;
        [SerializeField] private IconoEtiqueta[] _iconos;

        [Header("Comparacion")]
        [SerializeField] private TMP_Text _tituloComparacion;
        [SerializeField] private RectTransform _contenedorDiff;
        [SerializeField] private ColumnaDiffView _prefabColumnaDiff;
        [SerializeField] private string _textoSinSeleccion = "SELECCIONA DOS COMMITS PARA COMPARAR";

        [Header("Pie")]
        [SerializeField] private TMP_Text _estado;
        [SerializeField] private Button _botonEnviar;

        [Header("Cierre")]
        [SerializeField] private GameObject _panelCierre;
        [SerializeField] private TMP_Text _lineaCierre;
        [SerializeField] private Button _botonGuardar;

        [Header("Estilo")]
        [SerializeField] private PaletaNexus _paleta;
        [SerializeField] private int _segundosAviso = 15;

        [Serializable] public struct IconoEtiqueta { public string etiqueta; public Sprite icono; }

        private MinijuegoDef _def;
        private MinijuegoContext _ctx;
        private DetectarState _st;
        private ILienzo _lienzo;
        private Action<ResultadoMinijuego> _alCerrar;
        private readonly List<ColumnaDiffView> _columnas = new List<ColumnaDiffView>();
        private readonly List<EtiquetaBotonView> _etiquetas = new List<EtiquetaBotonView>();
        private float _transcurrido;
        private bool _terminado;

        public void Iniciar(MinijuegoDef def, MinijuegoContext ctx, Action<ResultadoMinijuego> alCerrar)
        {
            _def = def;
            _ctx = ctx;
            _alCerrar = alCerrar;
            _st = new DetectarState(def.Presentacion.SegundosReloj);
            _lienzo = _lienzoComponente as ILienzo;
            if (_lienzo == null)
                throw new InvalidOperationException("El componente asignado a _lienzoComponente no implementa ILienzo.");

            _titulo.text = def.Presentacion.Titulo;
            _comoSeJuega.text = def.Presentacion.ComoSeJuega;
            _quienEspera.text = def.Presentacion.TextoPresion
                                ?? ((ctx.QuienEspera ?? def.Presentacion.QuienEspera) + " esta esperando");

            _panelCierre.SetActive(false);
            _botonEnviar.onClick.AddListener(Enviar);

            _lienzo.Construir(def, ctx, AlPinchar);
            if (Andamiaje().ResaltarZonasCandidatas)
                _lienzo.Resaltar(def.Zonas.SelectMany(z => z.Commits).Distinct());

            ConstruirPaleta();
            RefrescarComparacion();
            RefrescarEstado();
        }

        private AndamiajeCfg Andamiaje()
        {
            AndamiajeCfg cfg;
            return _def.Andamiaje.TryGetValue(_ctx.NivelAndamiaje.ToString(), out cfg)
                ? cfg : new AndamiajeCfg();
        }

        private void ConstruirPaleta()
        {
            var cfg = Andamiaje();
            var visibles = (cfg.Etiquetas != null && cfg.Etiquetas.Count > 0)
                ? cfg.Etiquetas : _def.PaletaEtiquetas;

            foreach (var etiqueta in visibles)
            {
                var b = Instantiate(_prefabEtiqueta, _contenedorEtiquetas);
                b.Rellenar(etiqueta, IconoDe(etiqueta), Marcar);
                _etiquetas.Add(b);
            }
        }

        private Sprite IconoDe(string etiqueta)
        {
            foreach (var i in _iconos) if (i.etiqueta == etiqueta) return i.icono;
            return null;
        }

        private void AlPinchar(string commitId)
        {
            if (_terminado) return;
            _st.Alternar(commitId);
            _lienzo.Refrescar(_st.Seleccion, EtiquetaPorCommit());
            RefrescarComparacion();
            RefrescarEstado();
        }

        private void Marcar(string etiqueta)
        {
            if (_terminado) return;
            if (!_st.Marcar(etiqueta, _transcurrido)) return;   // no habia nada seleccionado
            _lienzo.Refrescar(_st.Seleccion, EtiquetaPorCommit());
            RefrescarComparacion();
            RefrescarEstado();
        }

        private IReadOnlyDictionary<string, string> EtiquetaPorCommit()
        {
            var d = new Dictionary<string, string>();
            foreach (var m in _st.Marcas)
                foreach (var c in m.Commits) d[c] = m.Etiqueta;
            return d;
        }

        private void RefrescarComparacion()
        {
            foreach (var c in _columnas) if (c != null) Destroy(c.gameObject);
            _columnas.Clear();

            if (_st.Seleccion.Count != 2)
            {
                _tituloComparacion.text = _textoSinSeleccion;
                return;
            }
            _tituloComparacion.text = "COMPARACION";

            foreach (var id in _st.Seleccion)
            {
                var c = _def.Artefacto.Commits.First(x => x.Id == id);
                Diff diff = null;
                if (!string.IsNullOrEmpty(c.Diff)) _def.Artefacto.Diffs.TryGetValue(c.Diff, out diff);

                var col = Instantiate(_prefabColumnaDiff, _contenedorDiff);
                // Se muestran los dos autores y ya. La conclusion la saca el jugador.
                col.Rellenar(diff != null ? diff.Titulo : c.Fecha + "  ·  " + c.Autor,
                             diff != null ? diff.Lineas : new List<LineaDiff>(),
                             _paleta);
                _columnas.Add(col);
            }
        }

        private void RefrescarEstado()
        {
            // Nunca dice cuantos defectos hay: la decision real es "sigo mirando o envio".
            var partes = new List<string> { _st.Marcas.Count + " tramos marcados" };
            if (_st.Seleccion.Count > 0) partes.Add(_st.Seleccion.Count + " commits seleccionados");
            if (Andamiaje().ContadorRestantes)
                partes.Add("quedan " + Math.Max(0, _def.Zonas.Count - _st.Marcas.Count));
            _estado.text = string.Join("   ·   ", partes);
        }

        private void Update()
        {
            if (_terminado || _st == null) return;
            _transcurrido += Time.deltaTime;
            _st.SegundosRestantes -= Time.deltaTime;

            int s = Mathf.Max(0, Mathf.CeilToInt(_st.SegundosRestantes));
            _reloj.text = string.Format("{0:00}:{1:00}", s / 60, s % 60);
            _reloj.color = s <= _segundosAviso ? _paleta.relojUrgente : _paleta.texto;

            if (_st.SegundosRestantes <= 0) Enviar();
        }

        private void Enviar()
        {
            if (_terminado) return;
            _terminado = true;
            _st.Enviado = true;
            _botonEnviar.interactable = false;
            foreach (var e in _etiquetas) e.Bloquear();

            var res = DetectarEvaluador.Evaluar(_def, _st);
            _lienzo.Congelar(_st.Marcas.SelectMany(m => m.Commits).Distinct());

            // Nunca "has fallado": una linea que no acusa a nadie.
            _lineaCierre.text = res.TextoCierre;
            _panelCierre.SetActive(true);
            _botonGuardar.onClick.RemoveAllListeners();
            _botonGuardar.onClick.AddListener(() =>
            {
                if (_alCerrar != null) _alCerrar(res);
                Destroy(gameObject);
            });
        }
    }
}
