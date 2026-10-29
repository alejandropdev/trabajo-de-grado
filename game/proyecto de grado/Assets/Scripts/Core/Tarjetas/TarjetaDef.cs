using System;
using System.Collections.Generic;
using Nexus.Core.Eventos;

namespace Nexus.Core.Tarjetas {
    /// <summary>
    /// Lo que cabe en una tarjeta. Son los limites de la reduccion de texto del Nivel 0: una tarjeta se lee en
    /// segundos o no es una tarjeta. Los valida CatalogoDeTarjetas al cargar, asi un autor no puede colar un parrafo.
    /// </summary>
    public static class LimitesDeTarjeta {
        public const int PalabrasDeFrase = 12;
        public const int PalabrasDeEtiqueta = 3;
        public const int PalabrasDeAyuda = 18;
        public const int MinimoDeOpciones = 2;
        public const int MaximoDeOpciones = 4;   // la retrospectiva de Scrum ofrece cuatro mejoras
    }

    /// <summary>
    /// Una decision rapida: un personaje dice una frase y el jugador elige entre dos o tres respuestas.
    /// Es el reemplazo de las pantallas de lectura. Lo que enseña no se lee: se ve en las barras al elegir.
    ///
    /// Una tarjeta evalua EXACTAMENTE igual que la pantalla que sustituye: escribe en la traza con el mismo
    /// Origen, Oa y veredicto (GameSession.ResolverTarjeta). Por eso el Dashboard de Lecciones no cambia.
    /// </summary>
    public sealed class TarjetaDef {
        public string Id;

        /// <summary>Quien habla (un personaje con nombre, no "el sistema"). Pone cara a la tarjeta.</summary>
        public string Personaje;

        /// <summary>Una sola frase. Maximo LimitesDeTarjeta.PalabrasDeFrase palabras.</summary>
        public string Frase;

        /// <summary>El «?»: una linea de apoyo, opcional. Lo largo vive en las pizarras, que no se tocan.</summary>
        public string Ayuda;

        /// <summary>Escena de pizarra que abre el «?». Vacio = el «?» solo muestra la linea.</summary>
        public string Pizarra;

        /// <summary>Id de una entrada del glosario: el «?» abre su pizarron. Vacio = el «?» solo muestra la linea de ayuda.</summary>
        public string Termino;

        /// <summary>De donde sale: "admision", "fase1", "ceremonia", "dia"… La UI decide donde la muestra.</summary>
        public string Momento;

        /// <summary>Orden dentro del momento.</summary>
        public int Orden;

        /// <summary>Lo que escribe en la traza: FASE1, RETRO, ADMISION, EV-TUT-01… Es el mismo que usaba la pantalla antigua.</summary>
        public string Origen;

        /// <summary>El titulo con el que sale en el Dashboard de Lecciones.</summary>
        public string Titulo;

        /// <summary>Objetivo de aprendizaje que mide esta decision.</summary>
        public string Oa;

        /// <summary>Vacia = vale para todas las metodologias.</summary>
        public List<string> SoloMetodologias = new List<string>();

        public List<OpcionTarjeta> Opciones = new List<OpcionTarjeta>();

        public OpcionTarjeta Opcion(string id) {
            if (Opciones == null) return null;
            foreach (var o in Opciones)
                if (string.Equals(o.Id, id, StringComparison.Ordinal)) return o;
            return null;
        }

        public bool AplicaALa(string metodologiaId) {
            if (SoloMetodologias == null || SoloMetodologias.Count == 0) return true;
            foreach (var m in SoloMetodologias)
                if (string.Equals(m, metodologiaId, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    public sealed class OpcionTarjeta {
        public string Id;

        /// <summary>Lo que se ve en el boton o en el borde de la tarjeta. Maximo LimitesDeTarjeta.PalabrasDeEtiqueta palabras.</summary>
        public string Etiqueta;

        /// <summary>Nombre de un icono de la UI. Opcional: sin el, la etiqueta sola.</summary>
        public string Icono;

        /// <summary>correcta | aceptable | incorrecta. Va a la traza, no a la pantalla.</summary>
        public string Veredicto;

        /// <summary>El porque. Es lo que el jugador lee en el Dashboard, despues de jugar; nunca durante la tarjeta.</summary>
        public string Razon;

        /// <summary>Mismo formato que los efectos de un evento: stock -> delta o "+10%".</summary>
        public Dictionary<string, object> Efectos = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>Consecuencias que llegan despues. No se previsualizan nunca: se decide con informacion incompleta, a proposito.</summary>
        public List<EfectoDiferido> Diferidos = new List<EfectoDiferido>();

        /// <summary>Lo que ESTA metodologia opina de la opcion. Sale en la traza.</summary>
        public string NotaDeMetodologia;
    }

    /// <summary>La forma de un archivo de tarjetas: { "version": 1, "tarjetas": [ … ] }.</summary>
    public sealed class ArchivoDeTarjetas {
        public int Version = 1;
        public List<TarjetaDef> Tarjetas = new List<TarjetaDef>();
    }
}
