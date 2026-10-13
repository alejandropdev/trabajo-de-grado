using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Despliegue {
    /// <summary>
    /// La Fase 3 de un nivel, en datos (bloque "despliegue" de niveles/nivel-XX.json): con que tramos se arma la
    /// tuberia, que tomas tiene el entorno y que cables trae cada modulo. Solo contenido; las reglas estan en
    /// EvaluadorDeDespliegue.
    /// </summary>
    public sealed class DespliegueDef {
        /// <summary>Segundos REALES para toda la fase (la tuberia y todos los modulos). En cero se entrega lo que haya.</summary>
        public int Segundos = 300;

        /// <summary>
        /// En un despliegue intermedio (al cerrar un sprint, en una revision del flujo) solo salen los modulos que
        /// llevan al menos este porcentaje hecho. En el ultimo salen todos los que tengan algo.
        /// </summary>
        public double UmbralDeModulo = 60;

        public TuberiaDef Tuberia = new TuberiaDef();

        /// <summary>Las tomas del entorno a las que se enchufan los cables: las buenas y las trampa.</summary>
        public List<TomaDef> Tomas = new List<TomaDef>();

        /// <summary>Los cables de cada modulo, por id de modulo. Un modulo sin entrada no necesita enchufar nada.</summary>
        public Dictionary<string, List<CableDef>> Modulos = new Dictionary<string, List<CableDef>>(StringComparer.Ordinal);

        public TramoDef Tramo(string id) { return Tuberia.Tramos.FirstOrDefault(t => t.Id == id); }
        public TomaDef Toma(string id) { return Tomas.FirstOrDefault(t => t.Id == id); }

        public List<CableDef> CablesDe(string modulo) {
            List<CableDef> cables;
            return modulo != null && Modulos != null && Modulos.TryGetValue(modulo, out cables) && cables != null ? cables : new List<CableDef>();
        }

        public DespliegueDef Clone() {
            var c = (DespliegueDef)MemberwiseClone();
            c.Tuberia = Tuberia == null ? new TuberiaDef() : Tuberia.Clone();
            c.Tomas = Tomas == null ? new List<TomaDef>() : Tomas.ToList();
            c.Modulos = new Dictionary<string, List<CableDef>>(StringComparer.Ordinal);
            if (Modulos != null) foreach (var kv in Modulos) c.Modulos[kv.Key] = kv.Value == null ? new List<CableDef>() : kv.Value.ToList();
            return c;
        }
    }

    /// <summary>La tuberia por la que el codigo viaja hasta produccion: sus huecos y los tramos con que se llenan.</summary>
    public sealed class TuberiaDef {
        /// <summary>Cuantos huecos tiene la tuberia. Puede haber mas tramos que huecos: elegir es parte del reto.</summary>
        public int Huecos = 6;
        public List<TramoDef> Tramos = new List<TramoDef>();

        public TuberiaDef Clone() { return new TuberiaDef { Huecos = Huecos, Tramos = Tramos == null ? new List<TramoDef>() : Tramos.ToList() }; }
    }

    /// <summary>Un tramo de tuberia: una etapa del despliegue (compilar, probar, aprobar…) o una trampa.</summary>
    public sealed class TramoDef {
        public string Id;
        public string Nombre;
        /// <summary>Que hace esa etapa, en una frase para quien no la conoce.</summary>
        public string QueHace;
        /// <summary>Si falta, el despliegue va a ciegas en algo: cuenta como «faltó».</summary>
        public bool Obligatorio;
        /// <summary>Un atajo que parece ahorrar tiempo y sale caro. Ponerlo cuenta como «mal».</summary>
        public bool Trampa;
        /// <summary>Los tramos que tienen que ir ANTES que este (si estan puestos).</summary>
        public List<string> DespuesDe = new List<string>();
        /// <summary>Atrapa errores antes de que lleguen al cliente: si falta, escapan defectos.</summary>
        public bool AtrapaErrores;
        /// <summary>Por que importa: lo que se dice en el resultado, tanto si esta como si falta.</summary>
        public string Porque;
    }

    /// <summary>Una toma del entorno: la base de datos de produccion, el correo real, la caja de secretos… o una trampa.</summary>
    public sealed class TomaDef {
        public string Id;
        public string Nombre;
        /// <summary>"produccion" o "pruebas": se rotula y se colorea distinto.</summary>
        public string Entorno = "produccion";
        public bool Trampa;
        /// <summary>Lo que pasa si se enchufa aqui algo que no debia (solo las trampa lo necesitan).</summary>
        public string Porque;
    }

    /// <summary>Un cable de un modulo: lo que necesita para funcionar y a que toma tiene que ir.</summary>
    public sealed class CableDef {
        public string Id;
        public string Nombre;
        /// <summary>La toma correcta. O "modulo:&lt;id&gt;" si lo que necesita es otro modulo ya desplegado.</summary>
        public string Toma;
        public string Porque;

        public const string PrefijoDeModulo = "modulo:";
        public bool EsDependencia { get { return Toma != null && Toma.StartsWith(PrefijoDeModulo, StringComparison.Ordinal); } }
        public string ModuloNecesario { get { return EsDependencia ? Toma.Substring(PrefijoDeModulo.Length) : null; } }
    }
}
