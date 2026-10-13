using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Proyecto {
    /// <summary>
    /// En que punto esta una parte del producto: de unas rayas en un papel a algo que el cliente usa de verdad. Es la
    /// misma escalera del glosario (boceto, mockup, prototipo, MVP), leida del avance del modulo.
    /// </summary>
    public static class EtapasDelProducto {
        public const string Boceto = "boceto";
        public const string Mockup = "mockup";
        public const string Prototipo = "prototipo";
        public const string Mvp = "mvp";
        public const string Desplegado = "desplegado";

        public static readonly string[] Todas = { Boceto, Mockup, Prototipo, Mvp, Desplegado };

        /// <summary>A partir de que porcentaje hecho un modulo deja de ser un mockup, un prototipo…</summary>
        public const double DesdeMockup = 25, DesdePrototipo = 50, DesdeMvp = 85;

        public static string De(double porcentaje, bool desplegado) {
            if (desplegado) return Desplegado;
            if (porcentaje >= DesdeMvp) return Mvp;
            if (porcentaje >= DesdePrototipo) return Prototipo;
            if (porcentaje >= DesdeMockup) return Mockup;
            return Boceto;
        }

        public static int Orden(string etapa) { return Math.Max(0, Array.IndexOf(Todas, etapa)); }

        public static string Nombre(string etapa) {
            switch (etapa) {
                case Mockup: return "mockup";
                case Prototipo: return "prototipo";
                case Mvp: return "MVP";
                case Desplegado: return "en producción";
                default: return "boceto";
            }
        }

        /// <summary>Que quiere decir estar en esa etapa, en una frase.</summary>
        public static string QueEs(string etapa) {
            switch (etapa) {
                case Mockup: return "Ya se ve cómo va a ser, pero es una imagen: no se puede usar.";
                case Prototipo: return "Responde al clic y pasa de una pantalla a otra, pero no guarda nada.";
                case Mvp: return "Funciona de verdad con lo esencial. Falta llevarlo a producción.";
                case Desplegado: return "Está en producción: el cliente lo usa.";
                default: return "Son rayas en un papel: la idea, sin nada construido.";
            }
        }
    }

    /// <summary>Un modulo del producto, tal y como esta hoy.</summary>
    public sealed class EstadoDelModulo {
        public ModuloDelProyecto Modulo;
        public double Porcentaje;
        public string Etapa;
    }

    /// <summary>
    /// El producto que se esta construyendo, modulo a modulo. No simula nada: lee el avance del proyecto (con
    /// AvanceDeModulos) y los modulos ya desplegados, y dice en que etapa esta cada parte.
    /// </summary>
    public static class MaquetaDelProducto {
        public static List<EstadoDelModulo> Calcular(FichaDelProyecto f, double avance, double alcance, IEnumerable<string> desplegados) {
            var resultado = new List<EstadoDelModulo>();
            if (f == null) return resultado;
            var ya = new HashSet<string>(desplegados ?? new string[0], StringComparer.Ordinal);
            foreach (var par in AvanceDeModulos.Calcular(f, avance, alcance))
                resultado.Add(new EstadoDelModulo {
                    Modulo = par.Key, Porcentaje = par.Value, Etapa = EtapasDelProducto.De(par.Value, ya.Contains(par.Key.Id))
                });
            return resultado;
        }

        /// <summary>La etapa del producto entero: la del modulo mas atrasado de los que ya se empezaron (o boceto si no hay ninguno).</summary>
        public static string EtapaGeneral(List<EstadoDelModulo> modulos) {
            if (modulos == null || modulos.Count == 0) return EtapasDelProducto.Boceto;
            var media = modulos.Average(m => m.Porcentaje);
            return EtapasDelProducto.De(media, modulos.All(m => m.Etapa == EtapasDelProducto.Desplegado));
        }

        /// <summary>Los modulos que subieron de etapa entre un avance y otro (lo que se anuncia al cerrar el dia).</summary>
        public static List<EstadoDelModulo> QueSubieron(FichaDelProyecto f, double avanceAntes, double avanceAhora, double alcance, IEnumerable<string> desplegados) {
            var antes = Calcular(f, avanceAntes, alcance, desplegados);
            var ahora = Calcular(f, avanceAhora, alcance, desplegados);
            return ahora.Where((m, i) => EtapasDelProducto.Orden(m.Etapa) > EtapasDelProducto.Orden(antes[i].Etapa)).ToList();
        }
    }

    /// <summary>Lo que dice quien va a usar el sistema cuando lo ve en el punto en que esta.</summary>
    public sealed class VozDelCliente {
        public string Quien;
        public string ModuloId;
        public string Etapa;
        public string Frase;
        /// <summary>Lo mismo en una linea, para ponerlo junto a un dibujo sin llenar la pizarra de texto.</summary>
        public string FraseCorta;
        /// <summary>Lo que esa persona necesita del sistema, tal cual lo dijo.</summary>
        public string Necesita;
        /// <summary>-1 descontento · 0 a la espera · 1 contento: la cara con que se le dibuja.</summary>
        public int Animo;
    }

    /// <summary>
    /// Los usuarios del cliente, mirando el producto. Cada uno habla de SU parte (el modulo que resuelve lo que
    /// necesita) y dice lo que cualquiera diria en esa etapa: un dibujo no le sirve, un prototipo casi, un MVP si.
    /// </summary>
    public static class VocesDelCliente {
        public static List<VozDelCliente> Para(FichaDelProyecto f, List<EstadoDelModulo> modulos, double satisfaccion) {
            var voces = new List<VozDelCliente>();
            if (f == null || modulos == null) return voces;
            foreach (var u in f.Usuarios) {
                var estado = modulos.FirstOrDefault(m => m.Modulo.Id == u.Modulo);
                if (estado == null) continue;
                voces.Add(new VozDelCliente {
                    Quien = u.Nombre, ModuloId = u.Modulo, Etapa = estado.Etapa, Frase = Frase(u, estado, satisfaccion),
                    FraseCorta = FraseCorta(estado.Etapa), Necesita = u.QueNecesita, Animo = Animo(estado.Etapa, satisfaccion)
                });
            }
            return voces;
        }

        public static string FraseCorta(string etapa) {
            switch (etapa) {
                case EtapasDelProducto.Mockup: return "Ya veo cómo va a quedar, pero no se puede usar.";
                case EtapasDelProducto.Prototipo: return "Responde, pero no guarda nada. Casi.";
                case EtapasDelProducto.Mvp: return "Esto ya me serviría. ¿Cuándo lo tengo?";
                case EtapasDelProducto.Desplegado: return "Ya lo uso todos los días.";
                default: return "Por ahora es un dibujo en un papel.";
            }
        }

        private static int Animo(string etapa, double satisfaccion) {
            if (satisfaccion <= 35) return -1;
            var orden = EtapasDelProducto.Orden(etapa);
            return orden >= 3 ? 1 : orden == 0 && satisfaccion < 50 ? -1 : 0;
        }

        private static string Frase(UsuarioDelProyecto u, EstadoDelModulo estado, double satisfaccion) {
            var necesita = EnMedioDeFrase(u.QueNecesita);
            string frase;
            switch (estado.Etapa) {
                case EtapasDelProducto.Mockup:
                    frase = $"Ya veo cómo va a quedar «{estado.Modulo.Nombre}», pero no se puede usar. Lo que necesito es {necesita}."; break;
                case EtapasDelProducto.Prototipo:
                    frase = $"Puedo hacer clic y pasar de pantalla, pero no guarda nada. Casi: lo que necesito es {necesita}."; break;
                case EtapasDelProducto.Mvp:
                    frase = $"Esto ya me serviría. Era justo lo que hacía falta: {necesita}. ¿Cuándo lo tengo en mi puesto?"; break;
                case EtapasDelProducto.Desplegado:
                    frase = $"Ya lo uso todos los días. Lo que pedí ({necesita}) funciona."; break;
                default:
                    frase = $"Por ahora es un dibujo en un papel. Lo que necesito es {necesita}."; break;
            }
            if (satisfaccion <= 35) frase += " Y la verdad, esperaba que nos preguntaran más.";
            else if (satisfaccion >= 75 && estado.Etapa != EtapasDelProducto.Boceto) frase += " Se nota que nos escucharon.";
            return frase;
        }

        /// <summary>«Registrar a un visitante en segundos, sin cuaderno.» → «registrar a un visitante en segundos, sin cuaderno».</summary>
        private static string EnMedioDeFrase(string texto) {
            if (string.IsNullOrEmpty(texto)) return "que funcione";
            var t = texto.Trim().TrimEnd('.');
            return char.ToLowerInvariant(t[0]) + t.Substring(1);
        }
    }

    /// <summary>
    /// Un papel que llega del cliente durante el desarrollo: una hoja del cuaderno de visitas, una queja, un correo,
    /// una hoja de turnos. Es el negocio contado con sus propios papeles, no con un resumen.
    /// </summary>
    public sealed class PapelDelCliente {
        public string Id;
        /// <summary>El dia en que llega (1 = el primero).</summary>
        public int Dia = 1;
        /// <summary>"cuaderno" | "queja" | "correo" | "hoja" | "nota": como se dibuja.</summary>
        public string Tipo = "nota";
        public string Titulo;
        /// <summary>Quien lo escribio o de donde sale.</summary>
        public string De;
        /// <summary>Lo que pone en el papel, renglon a renglon.</summary>
        public List<string> Lineas = new List<string>();
        /// <summary>Lo que ese papel le dice a quien construye el sistema.</summary>
        public string QueTeDice;
        /// <summary>El modulo del que habla (opcional).</summary>
        public string Modulo;
    }

    public static class TiposDePapel {
        public static readonly string[] Todos = { "cuaderno", "queja", "correo", "hoja", "nota" };
    }
}
