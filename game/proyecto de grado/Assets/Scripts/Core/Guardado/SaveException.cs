using System;

namespace Nexus.Core.Guardado {
    /// <summary>
    /// El unico tipo que atrapa quien carga una partida. El mensaje es honesto y se puede
    /// enseñar tal cual: "(partida dañada)", "version futura", "es de otro nivel".
    /// </summary>
    public sealed class SaveException : Exception {
        public SaveException(string mensaje) : base(mensaje) { }

        public SaveException(string mensaje, Exception causa) : base(mensaje, causa) { }
    }
}
