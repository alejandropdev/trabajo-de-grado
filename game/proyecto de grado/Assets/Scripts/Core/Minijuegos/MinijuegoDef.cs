using System.Collections.Generic;

namespace Nexus.Core.Minijuegos
{
    /// <summary>
    /// Espejo en C# de minijuegos/MJ-*.json. Es contenido, no logica:
    /// aqui no va ni una regla de juego. Anadir una escena = anadir un JSON.
    /// Newtonsoft empareja nombres sin distinguir mayusculas, asi que
    /// "paletaEtiquetas" del JSON entra en PaletaEtiquetas sin atributos.
    /// </summary>
    public sealed class MinijuegoDef
    {
        public string Id;
        public string Verbo;                 // "V1_DETECTAR"
        public string Lienzo;                // "grafo_commits" | "diagrama" | "codigo" | "log"
        public string TemaPrincipal;
        public string ObjetivoAprendizaje;

        public Presentacion Presentacion = new Presentacion();
        public Artefacto Artefacto = new Artefacto();

        public List<Zona> Zonas = new List<Zona>();
        public List<Senuelo> Senuelos = new List<Senuelo>();
        public List<string> PaletaEtiquetas = new List<string>();

        /// <summary>Clave = nivelAndamiaje como texto ("0".."3").</summary>
        public Dictionary<string, AndamiajeCfg> Andamiaje = new Dictionary<string, AndamiajeCfg>();

        /// <summary>Clave = "todos" | "parcial" | "falsoPositivo" | "omitido".</summary>
        public Dictionary<string, Consecuencia> Consecuencias = new Dictionary<string, Consecuencia>();

        public Cierre Cierre = new Cierre();

        /// <summary>Solo verbo V3 (ordenar con restricciones): el backlog.</summary>
        public OrdenarCfg Ordenar;

        /// <summary>Solo verbo V2 (repartir un presupuesto escaso): las horas de pruebas.</summary>
        public RepartirCfg Repartir;
    }

    public sealed class Presentacion
    {
        public string Titulo;
        public string ComoSeJuega;
        public string QuienEspera;
        public string TextoPresion;
        public int SegundosReloj = 90;
    }

    public sealed class Artefacto
    {
        public List<Rama> Ramas = new List<Rama>();
        public List<Commit> Commits = new List<Commit>();
        public Dictionary<string, Diff> Diffs = new Dictionary<string, Diff>();

        /// <summary>
        /// Lienzos "diagrama" y "secuencia": piezas genericas que se pueden marcar (componentes, participantes,
        /// lineas de una traza). Las zonas y los señuelos las nombran por id, igual que a los commits.
        /// </summary>
        public List<Elemento> Elementos = new List<Elemento>();

        /// <summary>Flechas entre elementos (dependencias de un diagrama, mensajes de una secuencia). Tambien se marcan.</summary>
        public List<Conexion> Conexiones = new List<Conexion>();
    }

    public sealed class Elemento
    {
        public string Id;
        public string Texto;
        /// <summary>"componente", "baseDeDatos", "externo", "participante", "linea"…: solo cambia como se dibuja.</summary>
        public string Tipo;
        /// <summary>Lienzo "secuencia": "diagrama" (lo que se diseño) o "traza" (lo que paso de verdad).</summary>
        public string Grupo;
        public int Fila;
        public int Columna;
        public string Detalle;
    }

    public sealed class Conexion
    {
        public string Id;
        public string Desde;
        public string Hasta;
        public string Texto;
        /// <summary>El orden en el tiempo, en un diagrama de secuencia.</summary>
        public int Orden;
        public string Grupo;
    }

    /// <summary>El backlog del verbo V3: tarjetas, capacidad y la peticion del cliente.</summary>
    public sealed class OrdenarCfg
    {
        public List<Tarjeta> Tarjetas = new List<Tarjeta>();
        /// <summary>Los puntos que caben. Lo que quede por debajo de la linea no entra.</summary>
        public int Capacidad;
        /// <summary>Que parte del valor total tiene que caber para considerarlo un buen orden (0-1).</summary>
        public double UmbralDeValor = 0.7;
        /// <summary>Lo que pide el cliente, con su nombre: "La Ministra quiere todo para el dia 10".</summary>
        public string Peticion;
        /// <summary>obedecer | rechazar | negociar -> lo que pasa con cada respuesta.</summary>
        public Dictionary<string, Respuesta> Respuestas = new Dictionary<string, Respuesta>();
    }

    public sealed class Tarjeta
    {
        public string Id;
        public string Titulo;
        public int Valor;
        public int Esfuerzo;
        /// <summary>Tarjetas que tienen que ir ANTES. No se enseñan hasta que el jugador choca con ellas.</summary>
        public List<string> DependeDe = new List<string>();
    }

    public sealed class Respuesta
    {
        public string Texto;
        public Consecuencia Consecuencia = new Consecuencia();
    }

    /// <summary>Las horas del verbo V2: depositos con su coste y los defectos que hay escondidos.</summary>
    public sealed class RepartirCfg
    {
        public int Presupuesto;
        public string Unidad = "horas";
        public List<Deposito> Depositos = new List<Deposito>();
        /// <summary>Cuantos defectos escapan como maximo para considerar el reparto bueno.</summary>
        public int ToleranciaDeEscapes = 1;
    }

    public sealed class Deposito
    {
        public string Id;
        public string Nombre;
        public string Descripcion;
        /// <summary>Lo que cuesta encontrar UN defecto de este tipo.</summary>
        public int CostePorDefecto;
        /// <summary>Los defectos de este tipo que hay de verdad. El jugador no lo ve: lo descubre despues.</summary>
        public int DefectosOcultos;
    }

    public sealed class Rama
    {
        public string Id;
        public string Color;                 // nombre logico, lo resuelve la capa Unity
    }

    public sealed class Commit
    {
        public string Id;
        public string Rama;
        public string Fecha;                 // ISO 8601 sin zona: "2026-04-17T15:21"
        public string Autor;                 // iniciales
        public string Mensaje;
        public List<string> Padres = new List<string>();
        public string Diff;                  // clave en Artefacto.Diffs (puede ser null)
        public bool Huerfano;
    }

    public sealed class Diff
    {
        public string Titulo;
        public List<LineaDiff> Lineas = new List<LineaDiff>();
    }

    public sealed class LineaDiff
    {
        public string Tipo;                  // "ctx" | "add" | "del"
        public string Texto;
    }

    public sealed class Zona
    {
        public string Id;
        public string Tipo;                  // "commit" | "tramo"
        public List<string> Commits = new List<string>();
        public string Defecto;               // debe existir en PaletaEtiquetas
        public string Explicacion;
    }

    public sealed class Senuelo
    {
        public string Id;
        public List<string> Commits = new List<string>();
        public string RazonNoEsDefecto;
    }

    public sealed class AndamiajeCfg
    {
        public bool ResaltarZonasCandidatas;
        /// <summary>Vacia o null = se muestra la paleta completa.</summary>
        public List<string> Etiquetas = new List<string>();
        public bool ContadorRestantes;
        /// <summary>Etiquetas plausibles pero incorrectas para esta escena.</summary>
        public List<string> EtiquetasTrampa = new List<string>();
    }

    public sealed class Consecuencia
    {
        public Dictionary<string, float> EfectosInmediatos = new Dictionary<string, float>();
        public List<EfectoDiferido> EfectosDiferidos = new List<EfectoDiferido>();
        public List<string> Hallazgos = new List<string>();
        public Rubrica Rubrica = new Rubrica();
    }

    public sealed class EfectoDiferido
    {
        public int EnDias;
        public Dictionary<string, float> Efectos = new Dictionary<string, float>();
        public string EventoForzado;
    }

    public sealed class Rubrica
    {
        public string Veredicto;             // "correcta" | "aceptable" | "incorrecta"
        public string Oa;
        public string Razon;
    }

    public sealed class Cierre
    {
        public string Texto;
    }
}
