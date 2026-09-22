using System;
using System.Collections.Generic;
using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// La navegacion entre pantallas, como una pila. Sustituye al MenuController, que encendia y apagaba
    /// paneles con SetActive: con doce pantallas eso ya no escala, y ademas obliga a tenerlas todas montadas
    /// en la escena desde el principio.
    ///
    ///   IrA&lt;T&gt;()     vacia la pila y deja T sola         (menu → partida)
    ///   Apilar&lt;T&gt;()  pone T encima de la actual           (el dia → una decision)
    ///   Volver()       quita la de arriba y enseña la anterior
    ///   Repintar()     la de arriba vuelve a leer sus datos
    /// </summary>
    public sealed class ScreenRouter {
        private readonly AppRoot _app;
        private readonly RectTransform _capa;
        private readonly List<Pantalla> _pila = new List<Pantalla>();

        public event Action<Pantalla> AlCambiar;

        public ScreenRouter(AppRoot app, RectTransform capa) {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _capa = capa;
        }

        public Pantalla Actual { get { return _pila.Count == 0 ? null : _pila[_pila.Count - 1]; } }
        public int Profundidad { get { return _pila.Count; } }

        /// <summary>Cierra todo y abre T. 'configurar' se llama antes de construirla (para pasarle datos).</summary>
        public T IrA<T>(Action<T> configurar = null) where T : Pantalla {
            for (var i = _pila.Count - 1; i >= 0; i--) Destruir(_pila[i]);
            _pila.Clear();
            return Crear(configurar);
        }

        /// <summary>Abre T encima de la actual. Si T no es modal, la de debajo se oculta hasta que se vuelva.</summary>
        public T Apilar<T>(Action<T> configurar = null) where T : Pantalla {
            var debajo = Actual;
            var nueva = Crear(configurar, notificar: false);
            if (debajo != null && !nueva.EsModal) {
                debajo.AlOcultar();
                debajo.gameObject.SetActive(false);
            }
            AlCambiar?.Invoke(nueva);
            return nueva;
        }

        /// <summary>Cierra la de arriba. No hace nada si solo queda una: siempre tiene que haber una pantalla.</summary>
        public void Volver() {
            if (_pila.Count <= 1) return;
            var arriba = Actual;
            _pila.RemoveAt(_pila.Count - 1);
            Destruir(arriba);

            var ahora = Actual;
            if (!ahora.gameObject.activeSelf) ahora.gameObject.SetActive(true);
            ahora.AlMostrar();
            AlCambiar?.Invoke(ahora);
        }

        /// <summary>Vuelve hasta que la de arriba sea de tipo T. Si no hay ninguna T, no hace nada.</summary>
        public void VolverA<T>() where T : Pantalla {
            if (!_pila.Exists(p => p is T)) return;
            while (!(Actual is T)) Volver();
        }

        public void Repintar() {
            Actual?.Repintar();
        }

        private T Crear<T>(Action<T> configurar, bool notificar = true) where T : Pantalla {
            var rt = UiKit.Rellenar(_app.Ui.Nodo(_capa, typeof(T).Name));
            var pantalla = rt.gameObject.AddComponent<T>();
            try {
                configurar?.Invoke(pantalla);
                pantalla.Inicializar(_app);
            } catch {
                // Una pantalla que no se pudo construir no se queda a medias en la pila: se quita y se avisa arriba.
                UnityEngine.Object.Destroy(rt.gameObject);
                throw;
            }
            _pila.Add(pantalla);
            pantalla.AlMostrar();
            if (notificar) AlCambiar?.Invoke(pantalla);
            return pantalla;
        }

        private static void Destruir(Pantalla pantalla) {
            if (pantalla == null) return;
            pantalla.AlOcultar();
            UnityEngine.Object.Destroy(pantalla.gameObject);
        }
    }
}
