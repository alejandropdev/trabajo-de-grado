using Nexus.Unity.Tema;
using UnityEngine;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// Una pantalla del juego. Se construye UNA vez, con UiKit, cuando el router la crea; despues solo se
    /// repinta. No se monta en ninguna escena ni en ningun prefab: el ScreenRouter la instancia en tiempo de
    /// ejecucion, asi que añadir una pantalla es añadir una clase.
    ///
    /// Ciclo de vida:  Configurar (opcional, antes de construir) → Construir → AlMostrar
    ///                 → Repintar* → AlOcultar (si otra se apila encima) → AlMostrar → … → destruida.
    /// </summary>
    public abstract class Pantalla : MonoBehaviour {
        protected AppRoot App { get; private set; }
        protected UiKit Ui { get { return App.Ui; } }
        protected NexusTheme Tema { get { return App.Ui.Tema; } }
        protected RectTransform Raiz { get; private set; }

        /// <summary>
        /// Si es modal, la pantalla de debajo sigue visible (un dialogo sobre el dia continuo). Si no, la
        /// de debajo se oculta mientras esta este encima.
        /// </summary>
        public virtual bool EsModal { get { return false; } }

        /// <summary>Si Escape (o el boton «volver») puede cerrarla. Una decision a medias, por ejemplo, no.</summary>
        public virtual bool PuedeVolver { get { return true; } }

        internal void Inicializar(AppRoot app) {
            App = app;
            Raiz = (RectTransform)transform;
            Construir();
        }

        /// <summary>Crea la interfaz. Se llama una sola vez.</summary>
        protected abstract void Construir();

        /// <summary>Cada vez que la pantalla pasa a ser la de arriba del todo.</summary>
        public virtual void AlMostrar() { Repintar(); }

        /// <summary>Cuando otra pantalla (no modal) se pone encima.</summary>
        public virtual void AlOcultar() { }

        /// <summary>Vuelve a leer los datos y actualiza lo que se ve. No reconstruye.</summary>
        public virtual void Repintar() { }
    }
}
