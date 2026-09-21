using System;
using System.Collections.Generic;

namespace Nexus.Core.Eventos {
    /// <summary>Las cuatro fases de un nivel, como las nombra el catalogo.</summary>
    public static class FasesDelNivel {
        public const string Planificacion = "planificacion";
        public const string Desarrollo = "desarrollo";
        public const string Lanzamiento = "lanzamiento";
        public const string Evaluacion = "evaluacion";

        /// <summary>1..4 -> el nombre que usan los JSON. Fuera de rango devuelve "".</summary>
        public static string De(int fase) {
            switch (fase) {
                case 1: return Planificacion;
                case 2: return Desarrollo;
                case 3: return Lanzamiento;
                case 4: return Evaluacion;
                default: return "";
            }
        }
    }

    /// <summary>Como se convierte el valor de una variable en peso extra.</summary>
    public static class CurvasDePeso {
        /// <summary>factor · valor. Cuanta mas deuda, mas probable.</summary>
        public const string Lineal = "lineal";
        /// <summary>factor · (100 − valor). Cuanta MENOS cobertura, mas probable.</summary>
        public const string Inversa = "inversa";
        /// <summary>factor · valor² / 100. Casi nada hasta la mitad, y luego se dispara.</summary>
        public const string Cuadratica = "cuadratica";
    }

    /// <summary>
    /// C3 · Un evento del catalogo (§8.3.1). Espejo en C# de eventos/eventos.json.
    ///
    /// Las cinco reglas que todo evento cumple, y que el validador exige:
    /// 1 · va telegrafiado 1-3 dias antes por un canal diegetico;
    /// 2 · habla una persona con nombre, no "el sistema";
    /// 3 · no hay opcion obviamente correcta (por eso hacen falta al menos dos);
    /// 4 · cada opcion lleva su rubrica con su razon;
    /// 5 · al menos una opcion tiene coste diferido.
    /// </summary>
    public sealed class EventDefinition {
        public string Id;
        public string Nombre;

        /// <summary>Vocabulario cerrado: equipo, cliente, alcance, tecnico, devops, calidad, seguridad…</summary>
        public List<string> Tags = new List<string>();

        /// <summary>En que fases puede salir. Del vocabulario de FasesDelNivel.</summary>
        public List<string> Fases = new List<string>();

        /// <summary>1-5. Consume presupuesto de drama y escala los efectos.</summary>
        public int Severidad = 1;

        public string ObjetivoAprendizaje;

        /// <summary>Vacia = vale para todas las metodologias.</summary>
        public List<string> SoloMetodologias = new List<string>();

        /// <summary>
        /// Vacia = vale para todos los niveles. El catalogo de eventos es uno solo para todo el juego;
        /// esto es lo que impide que un evento escrito para la barrera de N1 salga en el concurso del N0.
        /// </summary>
        public List<string> SoloNiveles = new List<string>();

        public bool AplicaAlNivel(string nivelId) {
            if (SoloNiveles == null || SoloNiveles.Count == 0) return true;
            foreach (var n in SoloNiveles)
                if (string.Equals(n, nivelId, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Si es true, pasa por MethodologyRules.EvaluarCambioDeAlcance antes de presentarse.</summary>
        public bool EsCambioDeAlcance;

        /// <summary>
        /// Lo que tiene que ser cierto para que pueda salir. Es lo que hace que los riesgos tengan CAUSA:
        /// "DeudaTecnica &gt; 40" significa que el servidor de integracion no se cae por mala suerte.
        /// </summary>
        public List<string> Precondiciones = new List<string>();

        public double PesoBase = 10;

        /// <summary>Tus decisiones cambian tu perfil de riesgo. Ver CalcularPeso.</summary>
        public List<ModificadorPeso> ModificadoresPeso = new List<ModificadorPeso>();

        /// <summary>Dias que tienen que pasar antes de que pueda repetirse.</summary>
        public int Enfriamiento = 8;

        public int MaxOcurrencias = 1;

        /// <summary>OBLIGATORIO. Sin aviso previo no hay evento (INV-3).</summary>
        public Telegrafiado Telegrafiado;

        /// <summary>Minimo dos: si solo hay una, no es un dilema.</summary>
        public List<OpcionEvento> Opciones = new List<OpcionEvento>();

        public bool TieneTag(string tag) {
            if (Tags == null) return false;
            foreach (var t in Tags)
                if (string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public bool SaleEnFase(int fase) {
            if (Fases == null || Fases.Count == 0) return false;
            var nombre = FasesDelNivel.De(fase);
            foreach (var f in Fases)
                if (string.Equals(f, nombre, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }

    /// <summary>
    /// Como una variable del estado empuja la probabilidad de un evento (§4.4.3).
    /// Acumulacion MULTIPLICATIVA: peso *= (1 + contribucion).
    /// </summary>
    public sealed class ModificadorPeso {
        /// <summary>Nombre resoluble por IStateContext: un stock, un campo consultable, una derivada.</summary>
        public string Variable;

        /// <summary>lineal | inversa | cuadratica.</summary>
        public string Curva = CurvasDePeso.Lineal;

        public double Factor;
    }

    /// <summary>
    /// El aviso previo. El canal es diegetico siempre (principio P2): un log, un correo, un ticket,
    /// un comentario en el standup. Nunca un cartel que diga "atencion, evento".
    /// </summary>
    public sealed class Telegrafiado {
        /// <summary>Dias de antelacion. El motor fuerza un minimo de 1: avisar el mismo dia no es avisar.</summary>
        public int DiasAntes = 2;

        /// <summary>log | correo | chat | ticket | standup | postit.</summary>
        public string Canal = "log";

        public string Texto;
    }

    public sealed class OpcionEvento {
        public string Id;
        public string Texto;

        /// <summary>Si amplia el alcance, el veredicto de la metodologia decide cuanto cuesta.</summary>
        public bool AmpliaAlcance;

        /// <summary>Expresiones que deben cumplirse para que la opcion se pueda elegir. Si no, sale bloqueada y con motivo.</summary>
        public List<string> Requisitos = new List<string>();

        public Dictionary<string, object> EfectosInmediatos = new Dictionary<string, object>(StringComparer.Ordinal);

        public List<EfectoDiferido> EfectosDiferidos = new List<EfectoDiferido>();

        /// <summary>OBLIGATORIA: sin rubrica la decision no se puede evaluar y la Fase 4 se queda sin nada que decir.</summary>
        public RubricaOpcion Rubrica;

        /// <summary>Lo que ESTA metodologia opina de esta opcion. Sale en la traza.</summary>
        public string NotaDeMetodologia;
    }

    public sealed class RubricaOpcion {
        /// <summary>correcta | aceptable | incorrecta.</summary>
        public string Veredicto;

        public string Oa;

        /// <summary>El porque. Es lo que el jugador lee en el Dashboard de Lecciones.</summary>
        public string Razon;
    }

    /// <summary>
    /// Por que el director hizo lo que hizo, un dia. No es telemetria: es la herramienta con la que se
    /// depura un balanceo que "se siente raro", y la que permite explicar en la tesis por que una
    /// partida con semilla 4417 sale como sale.
    /// </summary>
    public sealed class DecisionDelDirector {
        public const string Agendado = "agendado";
        public const string Forzado = "forzado";
        public const string SinDrama = "sinDrama";
        public const string SinCandidatos = "sinCandidatos";
        public const string SinHueco = "sinHueco";
        public const string DiaLleno = "diaLleno";

        public int Dia;
        public string Resultado;
        public string EventoId;
        public int DiaDelEvento;
        public double Peso;
        public int Candidatos;
        public int DramaAntes;

        /// <summary>"EV-TEC-02: enfriamiento (faltan 3 dias)". Uno por evento descartado.</summary>
        public List<string> Descartados = new List<string>();

        public override string ToString() {
            var texto = $"dia {Dia}: {Resultado}";
            if (!string.IsNullOrEmpty(EventoId)) texto += $" {EventoId} para el dia {DiaDelEvento} (peso {Peso:F2})";
            return texto + $" · {Candidatos} candidatos, drama {DramaAntes}";
        }
    }
}
