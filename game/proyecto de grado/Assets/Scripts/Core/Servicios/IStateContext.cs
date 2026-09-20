namespace Nexus.Core.Servicios {
    /// <summary>
    /// C10 · El puerto que desacopla al director del estado (§4.4.1). Es lo que ve una precondicion
    /// escrita en un JSON de contenido: unas variables y unas funciones, nada mas.
    ///
    /// Lo implementa GameSession (M9), que reparte cada nombre entre WorldState, RuntimeState y lo suyo
    /// propio. Vive aqui, junto a su unico consumidor (ConditionEvaluator), y no en Core.Eventos como
    /// dice el mapa de paquetes: un puerto pertenece a quien lo llama, no a quien lo rellena.
    /// </summary>
    public interface IStateContext {
        /// <summary>
        /// Resuelve una variable por nombre: un stock del WorldState, uno de los diez campos consultables
        /// del RuntimeState, o una de las derivadas que expone GameSession (diasTotales, volatilidadReal,
        /// riesgoLatente, retrasoRelativo). Devuelve false si el nombre no existe.
        /// </summary>
        bool TryGetValue(string nombre, out double valor);

        /// <summary>
        /// Resuelve una llamada del tipo diasDesde('EV-TEC-02') u ocurrencias('EV-TEC-02').
        ///
        /// Las dos tienen un valor por defecto que importa: diasDesde de un evento que nunca ocurrio
        /// devuelve 999 (y no 0), para que "diasDesde('X') > 10" sea cierto la primera vez;
        /// ocurrencias de un evento que nunca ocurrio devuelve 0. Con los defectos al reves, la mitad
        /// de las precondiciones del catalogo se comportarian al contrario de lo que su autor cree.
        /// </summary>
        double CallFunction(string nombre, string argumento);
    }
}
