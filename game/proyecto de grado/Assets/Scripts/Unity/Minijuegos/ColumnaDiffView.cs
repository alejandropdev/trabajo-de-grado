using System.Collections.Generic;
using System.Text;
using Nexus.Core.Minijuegos;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Minijuegos
{
    /// <summary>Prefab "ColumnaDiff". Usa una fuente monoespaciada en el cuerpo.</summary>
    public sealed class ColumnaDiffView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titulo;
        [SerializeField] private TMP_Text _cuerpo;

        public void Rellenar(string titulo, IEnumerable<LineaDiff> lineas, PaletaNexus paleta)
        {
            _titulo.text = titulo;
            var sb = new StringBuilder();
            foreach (var l in lineas)
            {
                Color c; string signo;
                switch (l.Tipo)
                {
                    case "add": c = paleta.diffAnadido; signo = "+ "; break;
                    case "del": c = paleta.diffBorrado; signo = "- "; break;
                    default: c = paleta.diffContexto; signo = "  "; break;
                }
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append('>')
                  .Append(signo).Append(l.Texto).Append("</color>\n");
            }
            _cuerpo.text = sb.ToString();
        }
    }
}
