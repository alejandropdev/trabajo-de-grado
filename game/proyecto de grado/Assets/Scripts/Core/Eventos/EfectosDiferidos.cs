using System;
using System.Collections.Generic;

namespace Nexus.Core.Eventos {
    /// <summary>
    /// C4 · Un efecto que el contenido programa para dentro de N dias (§4.4.4). Espejo en C# del bloque
    /// "efectosDiferidos" de un JSON de evento o de minijuego.
    ///
    /// Aqui vive el principio P4 del diseño: el coste siempre es diferido. Lo que se decide el dia 2
    /// explota el dia 11, y para entonces el jugador ya no se acuerda de por que. Por eso la cadena causal
    /// de la Fase 4 existe: para que se acuerde.
    ///
    /// Nota: hay otro EfectoDiferido en Nexus.Core.Minijuegos con Dictionary&lt;string,float&gt;. Este usa
    /// object porque un evento puede decir "-15%", que un float no sabe representar. El puente entre los dos
    /// lo pone M7, donde vive el director de minijuegos; poner la conversion aqui obligaria a que
    /// Core.Eventos conociera Core.Minijuegos, que es justo la flecha que no debe existir.
    /// </summary>
    public sealed class EfectoDiferido {
        /// <summary>Dias desde hoy. 0 significa "al final de esta misma jornada".</summary>
        public int EnDias;

        /// <summary>Valores double o texto "+10%" / "-25%", igual que los efectos inmediatos.</summary>
        public Dictionary<string, object> Efectos = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>Id del evento que se fuerza ese dia. Es como se encadenan las 7 cadenas del §A.</summary>
        public string EventoForzado;
    }

    /// <summary>
    /// C4 · Un efecto ya agendado, con fecha absoluta. Viaja en el guardado: SIN ESTO el jugador esquiva
    /// consecuencias recargando la partida, que es exactamente lo que INV-7 prohibe.
    /// </summary>
    public sealed class EfectoEnCola {
        /// <summary>Dia absoluto del nivel en que vence.</summary>
        public int DiaObjetivo;

        /// <summary>Quien lo programo: EV-xxx | MJ-xxx | FASE1 | RETRO | LANZAMIENTO. Sale en la traza.</summary>
        public string Origen;

        public Dictionary<string, object> Efectos = new Dictionary<string, object>(StringComparer.Ordinal);

        public string EventoForzado;

        public EfectoEnCola Clone() {
            var c = (EfectoEnCola)MemberwiseClone();
            c.Efectos = Efectos == null ? null : new Dictionary<string, object>(Efectos, StringComparer.Ordinal);
            return c;
        }
    }

    /// <summary>
    /// C4 · El aviso que precede a un evento (§4.4.4). INV-3: el director agenda y avisa, nunca dispara
    /// a bocajarro.
    ///
    /// El telegrafiado NO es la alerta. El telegrafiado llega uno a tres dias antes por un canal diegetico
    /// ("el build tardo 14 min, ayer tardaba 6"); la alerta es la citacion del momento. El jugador que lee
    /// los logs ve venir el problema; el que no los lee, no. Esa diferencia es contenido pedagogico.
    /// </summary>
    public sealed class TelegrafiadoPendiente {
        public string EventoId;

        /// <summary>Dia absoluto en que el evento se presentara.</summary>
        public int DiaDelEvento;

        /// <summary>
        /// Minuto del dia en que sonara la alerta, del mismo DeterministicRng que eligio el evento
        /// (§7.5 paso 7). -1 = el director no eligio hora y la decide quien construya la alerta.
        ///
        /// Es LO UNICO que cambio al pasar al dia continuo: antes se agendaba un dia, ahora un
        /// (dia, minuto). La reproducibilidad del Modo Aula se mantiene al minuto.
        /// </summary>
        public int MinutoDelEvento = -1;

        /// <summary>Dia absoluto en que sale el aviso.</summary>
        public int DiaDelAviso;

        /// <summary>log | correo | chat | ticket | standup | postit. Nunca un HUD narrativo (principio P2).</summary>
        public string Canal;

        public string Texto;

        /// <summary>Una vez emitido no vuelve a salir: si se repitiera, el jugador dejaria de leerlo.</summary>
        public bool Emitido;

        public TelegrafiadoPendiente Clone() {
            return (TelegrafiadoPendiente)MemberwiseClone();
        }
    }
}
