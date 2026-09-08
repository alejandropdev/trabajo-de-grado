namespace Nexus.Core {
    /// <summary>
    /// Contrato para leer y escribir datos en algún lado (disco, memoria, lo que sea).
    /// Nexus.Core solo conoce esta interfaz, nunca sabe CÓMO se guarda de verdad.
    /// </summary>
    public interface IAlmacen {
        void Guardar(string clave, string contenidoJson);
        string Cargar(string clave);
        bool Existe(string clave);
        void Borrar(string clave);
        System.Collections.Generic.List<string> ListarClaves(string prefijo);
    }
}
