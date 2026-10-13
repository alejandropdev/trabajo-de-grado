using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Proyecto {
    /// <summary>
    /// El expediente del proyecto de un nivel: QUE se construye, para QUIEN, en que SECTOR y con que palabras.
    /// Espejo del bloque "proyecto" de niveles/nivel-XX.json. Es lo que hace que un nivel se distinga de otro por su
    /// contenido y no solo por los dias: lo enseña la Fase 1 (paso «El proyecto»), cada minijuego dice a que modulo
    /// pertenece, la Fase 2 muestra el avance por modulo y los Apuntes lo resumen.
    /// Solo datos: las reglas que lo usan estan en PreguntasDelProyecto y AvanceDeModulos.
    /// </summary>
    public sealed class FichaDelProyecto {
        /// <summary>El nombre del sistema que se construye («Registro de visitantes»).</summary>
        public string Nombre;
        public SectorDelProyecto Sector = new SectorDelProyecto();
        public ClienteDelProyecto Cliente = new ClienteDelProyecto();
        public List<UsuarioDelProyecto> Usuarios = new List<UsuarioDelProyecto>();
        /// <summary>«El sistema sirve para…», en una frase.</summary>
        public string EnUnaFrase;
        /// <summary>Las partes del sistema, en el orden en que se construyen.</summary>
        public List<ModuloDelProyecto> Modulos = new List<ModuloDelProyecto>();
        /// <summary>El diagrama de alto nivel: quien, desde fuera, habla con que modulo.</summary>
        public ContextoDelProyecto Contexto = new ContextoDelProyecto();
        /// <summary>Las palabras del oficio: lo que se dice en ese sector y aparece en los diagramas.</summary>
        public List<TerminoDelSector> Vocabulario = new List<TerminoDelSector>();
        /// <summary>Los documentos de la asignatura que pesan en ESTE proyecto, y por que.</summary>
        public List<ArtefactoDelProyecto> Artefactos = new List<ArtefactoDelProyecto>();
        /// <summary>Lo que cambia respecto al nivel anterior, en 2-4 frases.</summary>
        public List<string> LoNuevo = new List<string>();
        /// <summary>Los temas de la asignatura que se practican aqui.</summary>
        public List<string> TemasDelNivel = new List<string>();
        /// <summary>Los papeles del cliente que van llegando durante el desarrollo.</summary>
        public List<PapelDelCliente> Papeles = new List<PapelDelCliente>();

        /// <summary>
        /// "crear" = el proyecto empieza de cero y el product backlog lo arma el jugador con las Historias;
        /// "heredado" (o vacio) = ya viene hecho, porque el proyecto ya estaba en marcha.
        /// </summary>
        public string BacklogInicial;

        /// <summary>Las historias candidatas al product backlog: las que el cliente pidio y las que no deberian entrar.</summary>
        public List<HistoriaCandidata> Historias = new List<HistoriaCandidata>();

        public HistoriaCandidata Historia(string id) { return Historias.FirstOrDefault(h => h != null && h.Id == id); }

        /// <summary>Los papeles que ya han llegado ese dia, del mas nuevo al mas viejo.</summary>
        public List<PapelDelCliente> PapelesHasta(int dia) {
            return Papeles.Where(p => p != null && p.Dia <= dia).OrderByDescending(p => p.Dia).ToList();
        }

        public ModuloDelProyecto Modulo(string id) {
            return string.IsNullOrEmpty(id) ? null : Modulos.FirstOrDefault(m => m.Id == id);
        }

        /// <summary>El nombre de un actor o de un modulo del diagrama de contexto (null si no existe).</summary>
        public string NombreDePieza(string id) {
            var m = Modulo(id);
            if (m != null) return m.Nombre;
            var a = Contexto == null ? null : Contexto.Actores.FirstOrDefault(x => x.Id == id);
            return a == null ? null : a.Nombre;
        }

        /// <summary>La ficha no se modifica durante la partida: basta una copia superficial.</summary>
        public FichaDelProyecto Clone() { return (FichaDelProyecto)MemberwiseClone(); }
    }

    public static class BacklogsIniciales {
        public const string Crear = "crear";
        public const string Heredado = "heredado";
    }

    public static class TiposDeHistoria {
        /// <summary>El cliente la pidio: tiene que estar.</summary>
        public const string Pedida = "pedida";
        /// <summary>Suena bien, pero nadie la pidio: es trabajo de mas.</summary>
        public const string NadieLaPidio = "nadie";
        /// <summary>Es una tarea tecnica, no algo que alguien necesite: no es una historia.</summary>
        public const string NoEsHistoria = "no-historia";
        /// <summary>Dice lo mismo que otra con otras palabras.</summary>
        public const string Repetida = "repetida";

        public static readonly string[] Todos = { Pedida, NadieLaPidio, NoEsHistoria, Repetida };
    }

    /// <summary>
    /// Una historia que podria entrar al product backlog. «Como [quien], quiero [que], para [para que]»: asi se
    /// escribe lo que alguien necesita, no lo que hay que programar.
    /// </summary>
    public sealed class HistoriaCandidata {
        public string Id;
        /// <summary>El modulo al que pertenece (id de Modulos).</summary>
        public string Modulo;
        /// <summary>El nombre corto, el de la tarjeta.</summary>
        public string Titulo;
        public string Como, Quiero, Para;
        /// <summary>1 a 3: lo que vale para el cliente. Es lo que deberia decidir el orden.</summary>
        public int Valor = 2;
        public string Tipo = TiposDeHistoria.Pedida;
        /// <summary>Por que tenia que estar, o por que sobraba. Se dice al cerrar.</summary>
        public string Porque;

        public string Texto {
            get { return string.IsNullOrEmpty(Como) ? Titulo : $"Como {Como}, quiero {Quiero}, para {Para}."; }
        }
    }

    public sealed class SectorDelProyecto {
        public string Nombre;
        /// <summary>Como trabaja ese sector, en una o dos frases.</summary>
        public string ComoTrabaja;
    }

    public sealed class ClienteDelProyecto {
        public string Nombre;
        public string QueHace;
        public string QueLeDuele;
        /// <summary>Problemas que NO son los suyos, para la pregunta de comprobacion.</summary>
        public List<string> Distractores = new List<string>();
    }

    public sealed class UsuarioDelProyecto {
        public string Nombre;
        public string QueNecesita;
        /// <summary>El modulo que resuelve esa necesidad.</summary>
        public string Modulo;
    }

    public sealed class ModuloDelProyecto {
        public string Id;
        public string Nombre;
        public string QueHace;
        /// <summary>Cuanto trabajo es, en relacion a los demas (1 = normal).</summary>
        public double Peso = 1;
        /// <summary>0-100: lo que ya estaba hecho al llegar.</summary>
        public double AvanceInicial;

        // --- como se ve esta parte en «El producto» (la maqueta viva) ---
        /// <summary>"formulario" | "lista" | "aviso" | "lector" | "calendario" | "grafica": la pantalla con que se dibuja.</summary>
        public string Pantalla;
        /// <summary>Lo que se hace en ella («Registrar visita»). Vacio = solo se mira.</summary>
        public string Accion;
        /// <summary>Datos de ejemplo: los campos de un formulario, las filas de una lista, las lineas de un aviso.</summary>
        public List<string> Datos = new List<string>();
    }

    public sealed class ContextoDelProyecto {
        public List<ActorDelProyecto> Actores = new List<ActorDelProyecto>();
        public List<FlujoDelProyecto> Flujos = new List<FlujoDelProyecto>();
    }

    public sealed class ActorDelProyecto {
        public string Id;
        public string Nombre;
        /// <summary>"persona" | "sistema".</summary>
        public string Tipo = "persona";
    }

    public sealed class FlujoDelProyecto {
        public string Desde;
        public string Hasta;
        public string Texto;
    }

    public sealed class TerminoDelSector {
        public string Termino;
        public string Definicion;
        /// <summary>Donde aparece en el juego (que diagrama, que tarjeta).</summary>
        public string DondeLoVeras;
        /// <summary>La escena de tiza (EscenasDePizarra) que lo explica.</summary>
        public string Dibujo;
        /// <summary>La escena de tiza de «Dónde lo verás».</summary>
        public string DibujoDonde;
        /// <summary>El termino del glosario de la asignatura que lo amplia (opcional): sus paginas van a continuacion.</summary>
        public string Glosario;
    }

    public sealed class ArtefactoDelProyecto {
        /// <summary>"SRS", "SSD", "PMP", "PTP", "SAD"…</summary>
        public string Id;
        public string ParaQueAqui;
        /// <summary>El termino del glosario de la asignatura que lo explica (opcional).</summary>
        public string Glosario;
    }
}
