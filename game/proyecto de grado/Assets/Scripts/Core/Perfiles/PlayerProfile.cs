using System;
using System.Collections.Generic;

namespace Nexus.Core {
    [Serializable]
    public class PlayerProfile {
        public string idPerfil;
        public string nombreEstudiante;
        public string fechaCreacion;      // formato ISO 8601, ej. "2026-09-01T10:30:00"
        public int partidasCompletadas;
        public float[] perfilDeCompetencia = new float[12];
        public int[] resultadoPreTest = new int[15];
        // Si la entrevista del N0 ya se hizo con este perfil. El pre-test es UNO: el de la primera vez, antes de
        // haber jugado. Con el pre-test hecho, la entrevista ya no se repite en otra partida.
        public bool preTestHecho;
        // La entrevista empezada y sin terminar (null si no hay). Se borra al terminarla.
        public Nexus.Core.Evaluacion.EntrevistaEnCurso entrevistaEnCurso;
        // Las mecanicas cuya receta ya se le abrio sola a este perfil. Despues, la receta queda tras el boton «?».
        public List<string> recetasVistas = new List<string>();
        // Los pasos de la guia del tutorial que este perfil ya vio: la guia no se repite en otra partida.
        public List<string> guiaVista = new List<string>();
        // Los minijuegos que este perfil ya terminó alguna vez.
        public List<string> minijuegosJugados = new List<string>();
        // Las mecánicas (verbos) que Marisol ya le guió de principio a fin: la primera vez de cada una se juega
        // guiada, y solo esa. null = perfil de antes de que existiera: se deduce de minijuegosJugados al abrirlo.
        public List<string> mecanicasGuiadas;
        public int[] resultadoPostTest = new int[15];
        public List<string> coleccionablesGlobales = new List<string>();
        public string notaNGplus = "";

        // No es un campo del inventario, pero lo necesitamos para la migración de esquema.
        public int version = 1;
    }
}
