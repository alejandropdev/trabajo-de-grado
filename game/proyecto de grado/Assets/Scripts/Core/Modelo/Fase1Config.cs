using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nexus.Core.Modelo {
    /// <summary>
    /// C1 · Las tres decisiones fundacionales de la Fase 1 en datos (§4.2.4): que metodologia, como se
    /// reparte la calidad y que arquitectura. Las tres reconfiguran el motor entero antes del primer dia.
    /// </summary>
    public sealed class Fase1Config {
        public CalidadConfig Calidad = new CalidadConfig();
        public List<ArquitecturaOpcion> Arquitecturas = new List<ArquitecturaOpcion>();

        /// <summary>El menu de razones del verbo V4. Se comparte entre metodologia y arquitectura.</summary>
        public List<RazonOpcion> RazonesDisponibles = new List<RazonOpcion>();

        public Fase1Config Clone() {
            var c = new Fase1Config();
            c.Calidad = Calidad == null ? null : Calidad.Clone();
            if (Arquitecturas != null) {
                c.Arquitecturas = new List<ArquitecturaOpcion>(Arquitecturas.Count);
                foreach (var a in Arquitecturas) c.Arquitecturas.Add(a == null ? null : a.Clone());
            } else {
                c.Arquitecturas = null;
            }
            if (RazonesDisponibles != null) {
                c.RazonesDisponibles = new List<RazonOpcion>(RazonesDisponibles.Count);
                foreach (var r in RazonesDisponibles) c.RazonesDisponibles.Add(r == null ? null : r.Clone());
            } else {
                c.RazonesDisponibles = null;
            }
            return c;
        }
    }

    /// <summary>
    /// El reparto de fichas sobre los atributos de calidad (ISO/IEC 25010). El giro del §4.2.4:
    /// no invertir en un atributo SUBE el peso de su tag en el director. El jugador acaba de elegir
    /// que crisis va a sufrir el resto del nivel, y la pantalla no se lo dice.
    /// </summary>
    public sealed class CalidadConfig {
        public int Fichas = 8;
        public string TextoPresion;
        public List<AtributoCalidad> Atributos = new List<AtributoCalidad>();

        /// <summary>
        /// Cuanto multiplica el peso del tag de un atributo segun las fichas que recibio.
        /// Cero fichas duplica el riesgo; tres o mas casi lo apagan. Es una funcion pura para que
        /// GameSession.RepartirCalidad no tenga que llevar la tabla dentro.
        /// </summary>
        public static double FactorDeTag(int fichas) {
            if (fichas <= 0) return 2.0;
            if (fichas == 1) return 1.3;
            if (fichas >= 3) return 0.6;
            return 1.0;
        }

        public CalidadConfig Clone() {
            var c = (CalidadConfig)MemberwiseClone();
            if (Atributos != null) {
                c.Atributos = new List<AtributoCalidad>(Atributos.Count);
                foreach (var a in Atributos) c.Atributos.Add(a == null ? null : a.Clone());
            }
            return c;
        }
    }

    public sealed class AtributoCalidad {
        public string Id;
        public string Nombre;

        /// <summary>Tag del director cuyo peso sube si este atributo se queda sin fichas. Puede ser null.</summary>
        public string TagAfectado;

        /// <summary>Coeficiente del modelo que mejora al invertir aqui. Puede ser null.</summary>
        public string CoeficienteAfectado;

        /// <summary>Lo que se le cuenta al jugador en la Fase 4 si no invirtio y le costo caro.</summary>
        public string TextoSinInversion;

        public AtributoCalidad Clone() { return (AtributoCalidad)MemberwiseClone(); }
    }

    /// <summary>
    /// Una opcion de arquitectura. Importa tanto cual se elige como POR QUE: RazonesValidas y RazonesTrampa
    /// permiten detectar al que acierta por el motivo equivocado, que en la Fase 4 no puntua igual.
    /// </summary>
    public sealed class ArquitecturaOpcion {
        public string Id;
        public string Nombre;

        /// <summary>Efectos inmediatos sobre el WorldState. Valores double o texto "+10%" / "-25%".</summary>
        public Dictionary<string, object> Efectos = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>Multiplicadores sobre los coeficientes griegos: alpha, beta, gamma, iota, kappa...</summary>
        public Dictionary<string, double> ModificadoresModelo = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public List<string> RazonesValidas = new List<string>();
        public List<string> RazonesTrampa = new List<string>();

        /// <summary>La que el briefing sostiene. Todo nivel debe tener exactamente una, y el validador lo exige.</summary>
        public bool EsLaAdecuada;

        public string Veredicto;   // correcta | aceptable | incorrecta
        public string Razon;

        /// <summary>
        /// Flags que la eleccion suma AL CERRAR el nivel (INV-6): el canon dice que Voss aprueba el monolito
        /// y desprecia los microservicios en N1 ({"FLG_VOSS_AFINIDAD": 1}).
        /// </summary>
        public Dictionary<string, double> FlagsAlCerrar = new Dictionary<string, double>(StringComparer.Ordinal);

        public ArquitecturaOpcion Clone() {
            var c = (ArquitecturaOpcion)MemberwiseClone();
            c.Efectos = Efectos == null ? null : new Dictionary<string, object>(Efectos, StringComparer.Ordinal);
            c.ModificadoresModelo = ModificadoresModelo == null
                ? null
                : new Dictionary<string, double>(ModificadoresModelo, StringComparer.OrdinalIgnoreCase);
            c.RazonesValidas = RazonesValidas == null ? null : new List<string>(RazonesValidas);
            c.RazonesTrampa = RazonesTrampa == null ? null : new List<string>(RazonesTrampa);
            c.FlagsAlCerrar = FlagsAlCerrar == null ? null : new Dictionary<string, double>(FlagsAlCerrar, StringComparer.Ordinal);
            return c;
        }

        [OnDeserialized]
        internal void TrasCargar(StreamingContext _) {
            ModificadoresModelo = Diccionarios.SinMayusculas(ModificadoresModelo);
        }
    }

    public sealed class RazonOpcion {
        public string Id;
        public string Texto;

        public RazonOpcion Clone() { return (RazonOpcion)MemberwiseClone(); }
    }
}
