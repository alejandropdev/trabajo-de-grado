using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Detectar
{
    /// <summary>
    /// Estado vivo de la escena. NO se guarda en save.json: un minijuego dura
    /// 60-120 s y no es reanudable a mitad. Si el jugador cierra el juego aqui,
    /// al cargar vuelve al momento anterior a la alerta.
    /// </summary>
    public sealed class DetectarState
    {
        public readonly List<Marca> Marcas = new List<Marca>();
        public readonly List<string> Seleccion = new List<string>();   // commits pinchados aun sin etiquetar
        public float SegundosRestantes;
        public bool Enviado;

        public DetectarState(int segundosReloj)
        {
            SegundosRestantes = segundosReloj;
        }

        public void Alternar(string commitId)
        {
            if (Seleccion.Contains(commitId)) Seleccion.Remove(commitId);
            else Seleccion.Add(commitId);
        }

        /// <summary>Convierte la seleccion actual en una marca con etiqueta.</summary>
        public bool Marcar(string etiqueta, float segundoDeLaPartida)
        {
            if (Seleccion.Count == 0) return false;
            Marcas.Add(new Marca
            {
                Commits = Seleccion.ToList(),
                Etiqueta = etiqueta,
                Segundo = segundoDeLaPartida
            });
            Seleccion.Clear();
            return true;
        }

        public void Desmarcar(int indice)
        {
            if (indice >= 0 && indice < Marcas.Count) Marcas.RemoveAt(indice);
        }

        public bool CommitMarcado(string commitId)
        {
            return Marcas.Any(m => m.Commits.Contains(commitId));
        }
    }

    public sealed class Marca
    {
        public List<string> Commits = new List<string>();
        public string Etiqueta;
        public float Segundo;
    }
}
