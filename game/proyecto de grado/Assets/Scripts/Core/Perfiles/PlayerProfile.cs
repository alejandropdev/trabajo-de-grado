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
        public int[] resultadoPostTest = new int[15];
        public List<string> coleccionablesGlobales = new List<string>();
        public string notaNGplus = "";

        // No es un campo del inventario, pero lo necesitamos para la migración de esquema.
        public int version = 1;
    }
}
