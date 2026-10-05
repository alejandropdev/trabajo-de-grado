using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Tablero {
    /// <summary>Las columnas del tablero. La metodologia puede añadir y renombrar las suyas (fase 7); estas son las de base.</summary>
    public static class ColumnasDeBase {
        public const string PorHacer = "porHacer";
        public const string Haciendo = "haciendo";
        public const string Revision = "revision";
        public const string Hecho = "hecho";
    }

    /// <summary>
    /// Una pieza de trabajo del equipo: lo que antes era solo «unos puntos de Alcance». Se genera de los modulos del
    /// proyecto y suma exactamente el alcance (GeneradorDeTarjetas), y el avance del dia la va llenando.
    /// </summary>
    public sealed class TarjetaDeTrabajo {
        public string Id;
        public string Titulo;
        /// <summary>El modulo del proyecto al que pertenece (id de FichaDelProyecto.Modulos). Vacio en un bug o un cambio pedido.</summary>
        public string Modulo;
        /// <summary>El tipo de trabajo, para saber a quien se le da mejor: «interfaz», «datos», «logica», «pruebas».</summary>
        public string Tipo;
        public double Puntos;
        /// <summary>Lo hecho, de 0 a Puntos.</summary>
        public double Trabajo;
        public string Columna = ColumnasDeBase.PorHacer;
        /// <summary>El miembro que la lleva (id). null = nadie todavia.</summary>
        public string Asignado;
        /// <summary>Si la asigno el jugador. Las que no, las reparte el piloto automatico.</summary>
        public bool AsignacionManual;
        public bool EsBug;
        /// <summary>El orden de construccion: lo que va antes, se empieza antes. El jugador lo podra cambiar al priorizar.</summary>
        public int Orden;
        public int DiaEntrada;
        /// <summary>-1 mientras no se ha empezado / terminado.</summary>
        public int DiaInicio = -1;
        public int DiaFin = -1;
        /// <summary>Scrum: el sprint al que se comprometio (indice de la unidad). -1 = sigue en el product backlog.</summary>
        public int Sprint = -1;

        public bool Terminada { get { return Trabajo >= Puntos - MotorDelTablero.Epsilon; } }
        public bool Empezada { get { return Trabajo > MotorDelTablero.Epsilon; } }
        public double Restante { get { return Math.Max(0, Puntos - Trabajo); } }

        public TarjetaDeTrabajo Clone() { return (TarjetaDeTrabajo)MemberwiseClone(); }
    }

    /// <summary>
    /// Un compañero del equipo. No es un personaje de las conversaciones (Marta, Voss…): esos son gente del edificio.
    /// Esto es quien programa y prueba contigo, con lo que se le da mejor.
    /// </summary>
    public sealed class MiembroDelEquipo {
        public string Id;
        public string Nombre;
        public string Rol;
        /// <summary>El personaje de las conversaciones con el que se corresponde, si lo hay (para una ayuda futura).</summary>
        public string Personaje;
        /// <summary>Tipo de trabajo -> lo bien que se le da (1 = normal; 0,7 flojo; 1,3 su fuerte). Lo que no aparece vale 1.</summary>
        public Dictionary<string, double> Habilidades = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Lo que su cansancio se desvia del del equipo. La suma de todos es 0: asignarle siempre al mismo lo quema,
        /// pero no sube el cansancio global a escondidas.
        /// </summary>
        public double DesvioDeCansancio;

        public double HabilidadPara(string tipo) {
            if (string.IsNullOrEmpty(tipo) || Habilidades == null) return 1.0;
            // A mano y sin distinguir mayusculas: al volver de un guardado el diccionario ya no trae su comparador.
            foreach (var kv in Habilidades)
                if (string.Equals(kv.Key, tipo, StringComparison.OrdinalIgnoreCase)) return kv.Value;
            return 1.0;
        }
    }

    /// <summary>
    /// El tablero del equipo: las tarjetas, quienes las llevan y lo acumulado del dia. Es una PROYECCION con estado del
    /// avance del motor, no otro motor: W.Avance sigue siendo la unica verdad y solo avanza al cerrar el dia
    /// (ForresterModel.AvanzarUnDia). El tablero reparte ese avance en tarjetas, y durante el dia lo anticipa
    /// (ProvisionalHoy) para que se vea fluir el trabajo.
    ///
    /// Invariante (MotorDelTablero.Conciliar): suma del Trabajo de las tarjetas de alcance − TrabajoAdeudado =
    /// W.Avance + ProvisionalHoy, acotado a lo que hay que hacer.
    /// </summary>
    public sealed class TableroDelEquipo {
        public List<TarjetaDeTrabajo> Tarjetas = new List<TarjetaDeTrabajo>();
        public List<MiembroDelEquipo> Miembros = new List<MiembroDelEquipo>();

        /// <summary>Lo que el equipo va haciendo hoy, antes de que cierre el dia y se sume al avance de verdad.</summary>
        public double ProvisionalHoy;

        /// <summary>
        /// Trabajo que un efecto quito (un evento que obliga a rehacer) cuando no habia nada a medias de donde quitarlo:
        /// lo ya terminado no se deshace, asi que se debe, y lo siguiente que se avance lo paga primero.
        /// </summary>
        public double TrabajoAdeudado;

        /// <summary>Para el factor de flujo del dia: la suma de (factor × minutos) y los minutos contados.</summary>
        public double SumaFactorMinutos;
        public int MinutosDeHoy;

        /// <summary>Cuantas tarjetas se han generado, para dar ids que no se repiten aunque se quiten tarjetas.</summary>
        public int Generadas;

        /// <summary>
        /// Cuantas tarjetas se llevan a la vez. 0 = tantas como compañeros (una cada uno). En Kanban es el limite de
        /// WIP, y el jugador lo mueve; LimiteNeutro es con el que se empezo, y moverlo de ahi tiene efecto.
        /// </summary>
        public int LimiteEnCurso;
        public int LimiteNeutro;

        /// <summary>Scrum: el sprint en curso. Lo comprometido en el se trabaja antes que el resto del backlog.</summary>
        public int SprintActual = -1;

        /// <summary>Lo que una ceremonia bien (o mal) llevada le suma hoy al ritmo del equipo. Se pone a 0 cada dia.</summary>
        public double BonoDeFlujoHoy;

        public double PuntosDeAlcance { get { return Tarjetas.Where(t => !t.EsBug).Sum(t => t.Puntos); } }
        public double TrabajoDeAlcance { get { return Tarjetas.Where(t => !t.EsBug).Sum(t => t.Trabajo); } }

        public IEnumerable<TarjetaDeTrabajo> EnColumna(string columna) {
            return Tarjetas.Where(t => t.Columna == columna).OrderBy(t => t.Orden);
        }

        public TarjetaDeTrabajo Tarjeta(string id) {
            return Tarjetas.FirstOrDefault(t => t.Id == id);
        }

        public MiembroDelEquipo Miembro(string id) {
            return Miembros.FirstOrDefault(m => m.Id == id);
        }

        public TableroDelEquipo Clone() {
            var c = new TableroDelEquipo {
                ProvisionalHoy = ProvisionalHoy, TrabajoAdeudado = TrabajoAdeudado,
                SumaFactorMinutos = SumaFactorMinutos, MinutosDeHoy = MinutosDeHoy, Generadas = Generadas,
                LimiteEnCurso = LimiteEnCurso, LimiteNeutro = LimiteNeutro, SprintActual = SprintActual, BonoDeFlujoHoy = BonoDeFlujoHoy
            };
            c.Tarjetas = Tarjetas.Select(t => t.Clone()).ToList();
            c.Miembros = Miembros.Select(m => new MiembroDelEquipo {
                Id = m.Id, Nombre = m.Nombre, Rol = m.Rol, Personaje = m.Personaje, DesvioDeCansancio = m.DesvioDeCansancio,
                Habilidades = new Dictionary<string, double>(m.Habilidades, StringComparer.OrdinalIgnoreCase)
            }).ToList();
            return c;
        }
    }
}
