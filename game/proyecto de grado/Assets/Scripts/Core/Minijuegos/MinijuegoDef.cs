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
