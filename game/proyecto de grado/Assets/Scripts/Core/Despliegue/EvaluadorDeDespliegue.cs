using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Proyecto;

namespace Nexus.Core.Despliegue {
    /// <summary>Lo que toca desplegar ahora: que dia es, si es el ultimo y que modulos salen.</summary>
    public sealed class PlanDeDespliegue {
        public int Dia;
        /// <summary>El ultimo del nivel: sale todo lo que haya, y despues viene la evaluacion.</summary>
        public bool EsFinal;
        public int Segundos;
        /// <summary>Los modulos que salen en este despliegue, en el orden en que se construyen.</summary>
        public List<string> Modulos = new List<string>();
        /// <summary>Los que ya estaban desplegados de antes (cuentan para las dependencias).</summary>
        public List<string> YaDesplegados = new List<string>();
        /// <summary>Los que todavia no llegan al minimo para salir: se dicen, para que se entienda por que no estan.</summary>
        public List<string> NoListos = new List<string>();
    }

    /// <summary>Lo que el jugador dejo puesto cuando entrego (o cuando se acabo el tiempo).</summary>
    public sealed class RespuestaDeDespliegue {
        /// <summary>Los huecos de la tuberia, en orden; null o vacio = hueco sin tramo.</summary>
        public List<string> Tuberia = new List<string>();
        /// <summary>Por modulo: cable → toma donde lo enchufo. Un cable que falta se quedo suelto.</summary>
        public Dictionary<string, Dictionary<string, string>> Enchufes = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        public double SegundosUsados;
        public bool SeAcaboElTiempo;
    }

    public static class EstadosDeDespliegue {
        public const string Bien = "bien";
        public const string Falto = "falto";
        public const string Mal = "mal";
    }

    /// <summary>Un renglon del resultado: un tramo de la tuberia o un modulo, como quedo y por que.</summary>
    public sealed class RenglonDeDespliegue {
        /// <summary>"tuberia" o "modulo".</summary>
        public string Que;
        public string Id;
        public string Nombre;
        /// <summary>bien | falto | mal.</summary>
        public string Estado;
        public string Texto;
    }

    /// <summary>
    /// El resultado de una Fase 3: renglon a renglon que estuvo bien y que falto, lo que le hace al proyecto hoy y lo
    /// que se lleva al lanzamiento. Se guarda en el historial del nivel (RuntimeState.Despliegues).
    /// </summary>
    public sealed class ResultadoDeDespliegue {
        public int Dia;
        public bool EsFinal;
        /// <summary>bien | regular | mal.</summary>
        public string Nivel;
        public string Titulo;
        public List<RenglonDeDespliegue> Renglones = new List<RenglonDeDespliegue>();
        public List<string> ModulosEncendidos = new List<string>();
        public List<string> ModulosCaidos = new List<string>();
        /// <summary>Lo que cambia hoy en las barras del proyecto.</summary>
        public Dictionary<string, object> Efectos = new Dictionary<string, object>(StringComparer.Ordinal);
        /// <summary>Puntos de avance que costara mañana arreglar lo que salio mal. 0 = nada.</summary>
        public double AvanceQueCostaraManana;
        /// <summary>Lo que este despliegue suma (o resta) al riesgo del lanzamiento.</summary>
        public double Riesgo;
        /// <summary>Errores que se escaparon por desplegar asi: se suman a los del lanzamiento.</summary>
        public int Defectos;
        /// <summary>Lo que noto el cliente, en una frase.</summary>
        public string ParaElCliente;
        public bool SeAcaboElTiempo;
    }

    /// <summary>
    /// Las reglas de la Fase 3. Dos partes:
    ///   · la tuberia — que etapas tiene el camino a produccion y en que orden. Un hueco vacio no da error: esa
    ///     etapa simplemente no ocurre, y lo que tenia que atrapar pasa de largo.
    ///   · los modulos — cada cable a su toma. Una toma trampa (la base de datos de pruebas, una clave a la vista)
    ///     enciende el modulo, pero mal; un cable suelto o una dependencia sin desplegar, no lo enciende.
    /// Es una funcion pura: no toca el estado. Quien aplica el resultado es GameSession.
    /// </summary>
    public static class EvaluadorDeDespliegue {
        // Lo que pesa cada cosa. Estan juntos para poder afinarlos jugando.
        public const double RiesgoPorTramoQueFalta = 6, RiesgoPorTrampa = 7, RiesgoPorOrden = 3, RiesgoPorModuloCaido = 5,
                            RiesgoPorTomaTrampa = 4, AlivioPorTodoBien = -8;

        /// <summary>Que modulos salen hoy. En el ultimo despliegue, todos los que tengan algo hecho; antes, los que llegan al umbral.</summary>
        public static PlanDeDespliegue Planear(DespliegueDef def, FichaDelProyecto ficha, double avance, double alcance,
                                               IEnumerable<string> yaDesplegados, int dia, bool esFinal) {
            if (def == null) throw new ArgumentNullException(nameof(def));
            var plan = new PlanDeDespliegue { Dia = dia, EsFinal = esFinal, Segundos = Math.Max(30, def.Segundos) };
            var ya = new HashSet<string>(yaDesplegados ?? new string[0], StringComparer.Ordinal);
            plan.YaDesplegados = ya.ToList();
            if (ficha == null) return plan;
            foreach (var par in AvanceDeModulos.Calcular(ficha, avance, alcance)) {
                var id = par.Key.Id;
                if (ya.Contains(id)) continue;
                var listo = esFinal ? par.Value > 0.5 : par.Value >= def.UmbralDeModulo;
                if (listo) plan.Modulos.Add(id); else plan.NoListos.Add(id);
            }
            return plan;
        }

        public static ResultadoDeDespliegue Evaluar(DespliegueDef def, FichaDelProyecto ficha, PlanDeDespliegue plan, RespuestaDeDespliegue r) {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            r = r ?? new RespuestaDeDespliegue();
            var res = new ResultadoDeDespliegue { Dia = plan.Dia, EsFinal = plan.EsFinal, SeAcaboElTiempo = r.SeAcaboElTiempo };

            // ------------------------------------------------------------ la tuberia
            var puestos = (r.Tuberia ?? new List<string>()).Where(id => !string.IsNullOrEmpty(id) && def.Tramo(id) != null).ToList();
            var faltan = 0; var trampas = 0; var desordenados = 0; var sinRed = 0;
            foreach (var tramo in def.Tuberia.Tramos) {
                var puesto = puestos.IndexOf(tramo.Id);
                if (tramo.Trampa) {
                    if (puesto < 0) continue;
                    trampas++;
                    res.Renglones.Add(Renglon("tuberia", tramo, EstadosDeDespliegue.Mal, "Lo pusiste, y es un atajo. " + tramo.Porque));
                    continue;
                }
                if (puesto < 0) {
                    if (!tramo.Obligatorio) continue;
                    faltan++;
                    if (tramo.AtrapaErrores) sinRed++;
                    res.Renglones.Add(Renglon("tuberia", tramo, EstadosDeDespliegue.Falto, "Faltó. " + tramo.Porque));
                    continue;
                }
                var antes = tramo.DespuesDe.Where(d => puestos.IndexOf(d) > puesto).Select(d => def.Tramo(d).Nombre).ToList();
                if (antes.Count > 0) {
                    desordenados++;
                    res.Renglones.Add(Renglon("tuberia", tramo, EstadosDeDespliegue.Mal,
                        $"Está, pero antes de {string.Join(" y ", antes.Select(n => "«" + n + "»"))}: así no sirve de lo que debía. " + tramo.Porque));
                } else {
                    res.Renglones.Add(Renglon("tuberia", tramo, EstadosDeDespliegue.Bien, tramo.Porque));
                }
            }

            // ------------------------------------------------------------ los modulos
            // Un modulo puede depender de otro: se resuelven hasta que no cambia nada (el orden de la ficha no basta).
            var encendidos = new HashSet<string>(plan.YaDesplegados, StringComparer.Ordinal);
            var tomasTrampa = 0;
            var estado = new Dictionary<string, RenglonDeDespliegue>(StringComparer.Ordinal);
            var pendientes = plan.Modulos.ToList();
            var cambio = true;
            while (cambio) {
                cambio = false;
                foreach (var modulo in pendientes.ToList()) {
                    var renglon = EvaluarModulo(def, ficha, modulo, r, encendidos, plan.Modulos, ref tomasTrampa, definitivo: false);
                    if (renglon == null) continue;   // espera a otro modulo que aun puede encender
                    estado[modulo] = renglon;
                    pendientes.Remove(modulo);
                    if (renglon.Estado != EstadosDeDespliegue.Falto) encendidos.Add(modulo);
                    cambio = true;
                }
            }
            foreach (var modulo in pendientes) estado[modulo] = EvaluarModulo(def, ficha, modulo, r, encendidos, plan.Modulos, ref tomasTrampa, definitivo: true);
            foreach (var modulo in plan.Modulos) {
                var renglon = estado[modulo];
                res.Renglones.Add(renglon);
                if (renglon.Estado == EstadosDeDespliegue.Falto) res.ModulosCaidos.Add(modulo); else res.ModulosEncendidos.Add(modulo);
            }

            // ------------------------------------------------------------ lo que cuesta
            var caidos = res.ModulosCaidos.Count; var arriba = res.ModulosEncendidos.Count;
            var malArriba = res.Renglones.Count(x => x.Que == "modulo" && x.Estado == EstadosDeDespliegue.Mal);
            var perfecto = faltan == 0 && trampas == 0 && desordenados == 0 && caidos == 0 && tomasTrampa == 0;

            res.Riesgo = faltan * RiesgoPorTramoQueFalta + trampas * RiesgoPorTrampa + desordenados * RiesgoPorOrden +
                         caidos * RiesgoPorModuloCaido + tomasTrampa * RiesgoPorTomaTrampa + (perfecto ? AlivioPorTodoBien : 0);
            // Sin las etapas que atrapan errores, lo que tenian que atrapar llega al cliente. Y una toma de pruebas en
            // produccion es un error seguro.
            res.Defectos = sinRed + tomasTrampa + (trampas > 0 ? 1 : 0);

            var satisfaccion = (arriba - malArriba) * 5.0 - malArriba * 2.0 - caidos * 6.0;
            if (plan.Modulos.Count == 0 && !perfecto) satisfaccion -= 2;
            Sumar(res.Efectos, "SatisfaccionCliente", satisfaccion);
            Sumar(res.Efectos, "DeudaTecnica", trampas * 4.0 + tomasTrampa * 2.0);
            Sumar(res.Efectos, "MoralEquipo", perfecto ? 4.0 : -(caidos * 2.0 + trampas * 1.5 + (faltan >= 2 ? 2.0 : 0)));
            Sumar(res.Efectos, "Cansancio", caidos * 3.0 + (r.SeAcaboElTiempo ? 2.0 : 0));
            res.AvanceQueCostaraManana = caidos * 1.5 + tomasTrampa * 1.0 + trampas * 1.0;

            var fallos = faltan + trampas + desordenados + caidos + tomasTrampa;
            res.Nivel = perfecto ? "bien" : (caidos * 2 + trampas * 2 + faltan + tomasTrampa >= 4 || (plan.Modulos.Count > 0 && arriba == 0)) ? "mal" : "regular";
            res.Titulo = perfecto ? "Despliegue limpio" : res.Nivel == "mal" ? "Despliegue accidentado" : $"Despliegue con {fallos} cosa(s) que corregir";
            res.ParaElCliente = ParaElCliente(ficha, res, malArriba);
            return res;
        }

        /// <summary>Un modulo: cada cable a su toma. null si depende de otro que todavia puede encender (se vuelve a mirar).</summary>
        private static RenglonDeDespliegue EvaluarModulo(DespliegueDef def, FichaDelProyecto ficha, string modulo, RespuestaDeDespliegue r,
                                                         HashSet<string> encendidos, List<string> salenHoy, ref int tomasTrampa, bool definitivo) {
            var m = ficha == null ? null : ficha.Modulo(modulo);
            var nombre = m == null ? modulo : m.Nombre;
            var cables = def.CablesDe(modulo);
            Dictionary<string, string> puestos;
            if (r.Enchufes == null || !r.Enchufes.TryGetValue(modulo, out puestos) || puestos == null) puestos = new Dictionary<string, string>();

            var sueltos = new List<string>(); var equivocados = new List<string>(); var trampas = new List<string>();
            foreach (var cable in cables) {
                if (cable.EsDependencia) {
                    var necesita = cable.ModuloNecesario;
                    if (encendidos.Contains(necesita)) continue;
                    // El otro modulo sale hoy y aun no se ha mirado: se espera una vuelta.
                    if (!definitivo && salenHoy.Contains(necesita) && necesita != modulo) return null;
                    var otro = ficha == null ? null : ficha.Modulo(necesita);
                    sueltos.Add($"necesita «{(otro == null ? necesita : otro.Nombre)}», que no está desplegado");
                    continue;
                }
                string toma;
                if (!puestos.TryGetValue(cable.Id, out toma) || string.IsNullOrEmpty(toma)) { sueltos.Add($"«{cable.Nombre}» se quedó sin enchufar"); continue; }
                if (toma == cable.Toma) continue;
                var donde = def.Toma(toma);
                if (donde != null && donde.Trampa) trampas.Add($"«{cable.Nombre}» en «{donde.Nombre}». {donde.Porque}");
                else equivocados.Add($"«{cable.Nombre}» no iba en «{(donde == null ? toma : donde.Nombre)}». {cable.Porque}");
            }

            var renglon = new RenglonDeDespliegue { Que = "modulo", Id = modulo, Nombre = nombre };
            if (sueltos.Count > 0 || equivocados.Count > 0) {
                renglon.Estado = EstadosDeDespliegue.Falto;
                renglon.Texto = "No encendió: " + string.Join("; ", sueltos.Concat(equivocados)) + ".";
                // Las trampas de un modulo que no enciende no llegan a hacer daño: no salio.
            } else if (trampas.Count > 0) {
                tomasTrampa += trampas.Count;
                renglon.Estado = EstadosDeDespliegue.Mal;
                renglon.Texto = "Encendió, pero mal enchufado: " + string.Join(" ", trampas);
            } else {
                renglon.Estado = EstadosDeDespliegue.Bien;
                renglon.Texto = cables.Count == 0 ? "Encendió. No necesitaba enchufar nada." : "Encendió con todo en su sitio.";
            }
            return renglon;
        }

        private static string ParaElCliente(FichaDelProyecto ficha, ResultadoDeDespliegue res, int malArriba) {
            Func<string, string> nombre = id => { var m = ficha == null ? null : ficha.Modulo(id); return "«" + (m == null ? id : m.Nombre) + "»"; };
            var cliente = ficha != null && ficha.Cliente != null && !string.IsNullOrEmpty(ficha.Cliente.Nombre) ? ficha.Cliente.Nombre : "El cliente";
            if (res.ModulosEncendidos.Count == 0 && res.ModulosCaidos.Count == 0) return cliente + " no recibió nada nuevo hoy: todavía no había ningún módulo listo para salir.";
            if (res.ModulosEncendidos.Count == 0) return cliente + " esperaba verlo funcionar y no encendió nada: " + string.Join(", ", res.ModulosCaidos.Select(nombre)) + ".";
            var texto = cliente + " ya puede usar " + string.Join(", ", res.ModulosEncendidos.Select(nombre)) + ".";
            if (res.ModulosCaidos.Count > 0) texto += " Se quedó sin " + string.Join(", ", res.ModulosCaidos.Select(nombre)) + ".";
            if (malArriba > 0) texto += " Algo de lo que salió está mal enchufado: lo notará.";
            return texto;
        }

        private static RenglonDeDespliegue Renglon(string que, TramoDef t, string estado, string texto) {
            return new RenglonDeDespliegue { Que = que, Id = t.Id, Nombre = t.Nombre, Estado = estado, Texto = (texto ?? "").Trim() };
        }

        private static void Sumar(Dictionary<string, object> efectos, string clave, double valor) {
            if (Math.Abs(valor) < 0.01) return;
            efectos[clave] = Math.Round(valor, 1);
        }

        /// <summary>Una tuberia que da «bien»: los obligatorios en un orden que respeta sus «despues de». Para las pruebas y el modo guiado.</summary>
        public static List<string> TuberiaCorrecta(DespliegueDef def) {
            var pendientes = def.Tuberia.Tramos.Where(t => t.Obligatorio && !t.Trampa).ToList();
            var orden = new List<string>();
            while (pendientes.Count > 0) {
                var siguiente = pendientes.FirstOrDefault(t => t.DespuesDe.All(d => orden.Contains(d) || pendientes.All(p => p.Id != d)));
                if (siguiente == null) siguiente = pendientes[0];   // un ciclo en los datos: el validador lo avisa
                orden.Add(siguiente.Id);
                pendientes.Remove(siguiente);
            }
            return orden;
        }

        /// <summary>Los enchufes que encienden bien todos los modulos del plan.</summary>
        public static Dictionary<string, Dictionary<string, string>> EnchufesCorrectos(DespliegueDef def, PlanDeDespliegue plan) {
            var todo = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            foreach (var modulo in plan.Modulos)
                todo[modulo] = def.CablesDe(modulo).Where(c => !c.EsDependencia).ToDictionary(c => c.Id, c => c.Toma, StringComparer.Ordinal);
            return todo;
        }
    }
}
