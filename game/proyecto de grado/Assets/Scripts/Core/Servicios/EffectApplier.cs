using System;
using System.Collections.Generic;
using System.Globalization;
using Nexus.Core.Modelo;

namespace Nexus.Core.Servicios {
    /// <summary>
    /// C10 · El UNICO camino de escritura al WorldState (INV-2). La unica excepcion deliberada en todo
    /// el motor es ForresterModel.AvanzarUnDia, que es el motor del tiempo.
    ///
    /// Que esto sea un cuello de botella no es burocracia: es lo que hace que EstadoAntes y EstadoDespues
    /// de la traza sean ciertos. Si cualquiera pudiera escribir un stock, la auditoria pedagogica que
    /// sostiene la tesis seria una lista de suposiciones.
    ///
    /// Los tres formatos de valor que entiende un efecto (§4.4.5):
    ///
    ///   "DeudaTecnica": 12       -> delta absoluto:   nuevo = actual + 12
    ///   "Cobertura": "-15%"      -> sobre el actual:  nuevo = actual * (1 - 0.15)
    ///   "VelocidadMod": "-25%"   -> lo mismo, y por eso los porcentajes sobre VelocidadMod COMPONEN:
    ///                               dos "+10%" seguidos dan 1.21, no 1.20
    /// </summary>
    public static class EffectApplier {
        public const string PrefijoFlagProhibido = "FLG_";

        private const NumberStyles Estilos = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        /// <summary>
        /// Aplica los efectos inmediatos. 'mult' escala el efecto entero (severidad del evento,
        /// multiplicador de la metodologia); sobre un porcentaje escala el porcentaje.
        /// </summary>
        public static void Aplicar(WorldState w, Dictionary<string, object> efectos, double mult = 1.0) {
            if (w == null) throw new ArgumentNullException(nameof(w));
            if (efectos == null) return;

            foreach (var kv in efectos) {
                ValidarClave(kv.Key, null);

                double actual;
                w.TryGet(kv.Key, out actual);

                double valor;
                if (EsPorcentaje(kv.Value, kv.Key, null, out valor))
                    w.Set(kv.Key, actual * (1.0 + valor * mult));
                else
                    w.Set(kv.Key, actual + valor * mult);
            }
        }

        /// <summary>
        /// Comodidad para los contratos que ya existen con Dictionary&lt;string,float&gt;
        /// (ResultadoMinijuego.EfectosInmediatos). Un float no puede llevar un "-15%", asi que
        /// aqui todos los valores son deltas absolutos.
        /// </summary>
        public static void Aplicar(WorldState w, Dictionary<string, float> efectos, double mult = 1.0) {
            if (efectos == null) return;

            var convertidos = new Dictionary<string, object>(efectos.Count, StringComparer.Ordinal);
            foreach (var kv in efectos) convertidos[kv.Key] = (double)kv.Value;
            Aplicar(w, convertidos, mult);
        }

        /// <summary>
        /// Comprueba un bloque de efectos sin aplicarlo. Lo llama SchemaValidator al cargar el catalogo,
        /// que traduce estas excepciones a SchemaException con el archivo y el campo (INV-5).
        /// 'contexto' es lo que se antepone al mensaje: "EV-TEC-014 opcion B", por ejemplo.
        /// </summary>
        public static void Validar(Dictionary<string, object> efectos, string contexto) {
            if (efectos == null) return;

            foreach (var kv in efectos) {
                ValidarClave(kv.Key, contexto);
                double ignorado;
                EsPorcentaje(kv.Value, kv.Key, contexto, out ignorado);   // lanza si el valor es ilegible
            }
        }

        /// <summary>
        /// Lo que CAMBIARIA cada stock, sin cambiar nada. Es lo que pinta el boton "Estimar impacto"
        /// del panel de decision.
        ///
        /// Dos propiedades que importan:
        /// · Los deltas son HONESTOS: se calculan aplicando sobre un clon, asi que ya llevan dentro la
        ///   acotacion. Si la cobertura esta en 2 y el efecto es -15, el delta que se enseña es -2.
        /// · Solo enseña el efecto INMEDIATO. El diferido no se previsualiza nunca, y eso es diseño
        ///   pedagogico, no un olvido: el jugador decide con informacion incompleta a proposito.
        /// </summary>
        public static Dictionary<string, double> Previsualizar(WorldState w, Dictionary<string, object> efectos,
                                                               double mult = 1.0) {
            if (w == null) throw new ArgumentNullException(nameof(w));

            var deltas = new Dictionary<string, double>(StringComparer.Ordinal);
            if (efectos == null) return deltas;

            var copia = w.Clone();
            Aplicar(copia, efectos, mult);

            foreach (var clave in efectos.Keys) {
                double antes, despues;
                w.TryGet(clave, out antes);
                copia.TryGet(clave, out despues);
                deltas[clave] = despues - antes;
            }
            return deltas;
        }

        /// <summary>
        /// Lee un valor de efecto como numero. Newtonsoft entrega los enteros del JSON como long y los
        /// decimales como double, asi que ambos tienen que pasar por aqui sin sorpresas.
        /// </summary>
        public static double ToDouble(object v) {
            if (v == null) throw new InvalidOperationException("Un efecto no puede tener valor nulo.");

            if (v is double) return (double)v;
            if (v is float) return (float)v;
            if (v is int) return (int)v;
            if (v is long) return (long)v;
            if (v is short) return (short)v;
            if (v is byte) return (byte)v;
            if (v is decimal) return (double)(decimal)v;

            var texto = v as string;
            if (texto != null) {
                double n;
                if (double.TryParse(texto.Trim(), Estilos, CultureInfo.InvariantCulture, out n)) return n;
                throw new InvalidOperationException($"'{texto}' no es un numero ni un porcentaje valido.");
            }

            var convertible = v as IConvertible;
            if (convertible != null) {
                try {
                    return convertible.ToDouble(CultureInfo.InvariantCulture);
                } catch (Exception) {
                    // cae al mensaje de abajo, que dice el tipo
                }
            }

            throw new InvalidOperationException(
                $"No se puede leer '{v}' ({v.GetType().Name}) como valor de efecto.");
        }

        // --- interno ---

        private static void ValidarClave(string clave, string contexto) {
            if (string.IsNullOrEmpty(clave) || clave.Trim().Length == 0)
                throw new InvalidOperationException($"{Donde(contexto)}hay un efecto sin nombre de stock.");

            // INV-1, comprobado en runtime ademas de al cargar. Es la regla que mantiene testeable
            // el arbol de los 14 finales: si un evento pudiera mover un FLG_*, el final dejaria de
            // depender solo de decisiones explicitas del jugador.
            if (clave.TrimStart().StartsWith(PrefijoFlagProhibido, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"{Donde(contexto)}un efecto intenta escribir '{clave}'. Ningun evento, minijuego ni " +
                    $"decision puede tocar un {PrefijoFlagProhibido}*: los flags narrativos solo se escriben " +
                    "al cerrar el nivel (INV-1).");

            if (!WorldState.EsStock(clave))
                throw new InvalidOperationException(
                    $"{Donde(contexto)}'{clave}' no es un stock del WorldState. " +
                    $"Validos: {string.Join(", ", WorldState.Nombres)}.");
        }

        /// <summary>true si el valor era un porcentaje ("-15%"); en ese caso devuelve la fraccion (-0.15).</summary>
        private static bool EsPorcentaje(object v, string clave, string contexto, out double valor) {
            var texto = v as string;
            if (texto == null) {
                try {
                    valor = ToDouble(v);
                } catch (InvalidOperationException ex) {
                    throw new InvalidOperationException($"{Donde(contexto)}en '{clave}': {ex.Message}", ex);
                }
                return false;
            }

            var limpio = texto.Trim();
            var esPorcentaje = limpio.EndsWith("%", StringComparison.Ordinal);
            var numero = esPorcentaje ? limpio.Substring(0, limpio.Length - 1).Trim() : limpio;

            double n;
            if (!double.TryParse(numero, Estilos, CultureInfo.InvariantCulture, out n))
                throw new InvalidOperationException(
                    $"{Donde(contexto)}en '{clave}': '{texto}' no es un numero ni un porcentaje valido. " +
                    "Formatos aceptados: 12, -8.5, \"+10%\", \"-25%\".");

            valor = esPorcentaje ? n / 100.0 : n;
            return esPorcentaje;
        }

        private static string Donde(string contexto) {
            return string.IsNullOrEmpty(contexto) ? "" : contexto + ": ";
        }
    }
}
