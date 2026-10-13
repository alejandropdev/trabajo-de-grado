using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Despliegue;
using Nexus.Core.Eventos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Core.Proyecto;
using Nexus.Core.Servicios;

namespace Nexus.Core.Sesion {
    /// <summary>
    /// La Fase 3: desplegar. Cuando toca lo dice la metodologia (al cerrar cada sprint, en cada revision del flujo, o
    /// una sola vez al final), y siempre el ultimo dia. Despues de un despliegue intermedio se vuelve al desarrollo;
    /// el ultimo lleva a la evaluacion.
    ///
    /// ★ Lo que sale de aqui pesa: mueve las barras hoy, puede costar avance mañana y se suma al riesgo y a los
    /// errores del lanzamiento. Un despliegue que nunca se llega a jugar (el motor corrido sin pantalla, en las
    /// pruebas) no cuenta ni a favor ni en contra.
    /// </summary>
    public sealed partial class GameSession {
        public const string OrigenDeDespliegue = "DESPLIEGUE";

        /// <summary>El resultado del ultimo despliegue resuelto en esta sesion (para la pantalla). No viaja en el guardado.</summary>
        public ResultadoDeDespliegue UltimoDespliegue { get; private set; }

        /// <summary>Si este nivel tiene Fase 3 (bloque "despliegue").</summary>
        public bool HayDespliegue { get { return Perfil != null && Perfil.Despliegue != null && Perfil.Despliegue.Tuberia.Tramos.Count > 0; } }

        /// <summary>Si hoy, con el dia ya cerrado, toca desplegar y todavia no se ha hecho.</summary>
        public bool DesplieguePendiente {
            get {
                if (!HayDespliegue || !Fase1Cerrada || NivelTerminado || Lanzamiento != null) return false;
                if (R.DiaActual <= 0 || !JornadaSimulada) return false;
                if (R.Despliegues.Any(d => d.Dia == R.DiaActual)) return false;
                return EsDiaDeDespliegue(R.DiaActual);
            }
        }

        /// <summary>Si ese dia cierra con un despliegue, segun la metodologia. El ultimo dia del nivel, siempre.</summary>
        public bool EsDiaDeDespliegue(int dia) {
            if (!HayDespliegue || dia <= 0) return false;
            if (dia >= Perfil.DiasTotales) return dia == Perfil.DiasTotales;
            return Reglas != null && Reglas.EsDiaDeDespliegue(dia);
        }

        /// <summary>Los dias del nivel que cierran con un despliegue (para enseñar el calendario).</summary>
        public List<int> DiasDeDespliegue() {
            return Enumerable.Range(1, Perfil.DiasTotales).Where(EsDiaDeDespliegue).ToList();
        }

        /// <summary>Que se despliega hoy. No cambia nada: se puede pedir las veces que haga falta.</summary>
        public PlanDeDespliegue PlanDelDespliegue() {
            if (!HayDespliegue) throw new InvalidOperationException("Este nivel no tiene Fase 3.");
            return EvaluadorDeDespliegue.Planear(Perfil.Despliegue, Perfil.Proyecto, W.Avance, W.Alcance, R.ModulosDesplegados,
                                                 R.DiaActual, R.DiaActual >= Perfil.DiasTotales);
        }

        /// <summary>Cierra la Fase 3 de hoy con lo que el jugador dejo puesto. Aplica sus efectos y lo apunta en el historial.</summary>
        public ResultadoDeDespliegue ResolverDespliegue(RespuestaDeDespliegue respuesta) {
            if (!DesplieguePendiente) throw new InvalidOperationException("Hoy no hay ningun despliegue pendiente.");

            var plan = PlanDelDespliegue();
            var res = EvaluadorDeDespliegue.Evaluar(Perfil.Despliegue, Perfil.Proyecto, plan, respuesta);
            var antes = W.ToString();

            EffectApplier.Aplicar(W, res.Efectos);
            // Lo que salio mal hay que arreglarlo mañana, y eso es avance que no se hace. El ultimo dia no hay mañana:
            // ahi lo que pesa es el riesgo y los errores, que van directos al lanzamiento.
            if (res.AvanceQueCostaraManana > 0 && !plan.EsFinal)
                _scheduler.Encolar(R.DiaActual, new EfectoDiferido {
                    EnDias = 1,
                    Efectos = new Dictionary<string, object>(StringComparer.Ordinal) { { "Avance", -Math.Round(res.AvanceQueCostaraManana, 1) } }
                }, OrigenDeDespliegue);

            foreach (var modulo in res.ModulosEncendidos) if (!R.ModulosDesplegados.Contains(modulo)) R.ModulosDesplegados.Add(modulo);
            R.RiesgoDeDespliegue = Math.Max(-15, Math.Min(40, R.RiesgoDeDespliegue + res.Riesgo));
            R.DefectosDeDespliegue = Math.Max(0, R.DefectosDeDespliegue + res.Defectos);
            R.Despliegues.Add(new RegistroDeDespliegue {
                Dia = res.Dia, EsFinal = res.EsFinal, Nivel = res.Nivel, Titulo = res.Titulo,
                Encendidos = res.ModulosEncendidos.ToList(), Caidos = res.ModulosCaidos.ToList(),
                Riesgo = Math.Round(res.Riesgo, 1), Defectos = res.Defectos
            });

            Registrar(OrigenDeDespliegue, "Despliegue del día " + res.Dia, res.Nivel, res.Titulo,
                      res.Nivel == "bien" ? Veredictos.Correcta : res.Nivel == "regular" ? Veredictos.Aceptable : Veredictos.Incorrecta,
                      "OA-DEVOPS-01",
                      string.Join(" ", res.Renglones.Where(x => x.Estado != EstadosDeDespliegue.Bien).Select(x => $"{x.Nombre}: {x.Texto}").Take(4).DefaultIfEmpty("Todo en su sitio.")),
                      antes);

            UltimoDespliegue = res;
            return res;
        }

        /// <summary>
        /// Lo que los despliegues del nivel le suman al riesgo del lanzamiento. Quien fue entregando por partes y el
        /// cliente ya lo vio funcionar llega al final con menos incertidumbre (clienteYaVioElProducto).
        /// </summary>
        public double RiesgoPorDespliegues() {
            var riesgo = R.RiesgoDeDespliegue;
            var total = Perfil.Proyecto == null ? 0 : Perfil.Proyecto.Modulos.Count;
            if (total > 0) {
                var vistosAntes = R.Despliegues.Where(d => !d.EsFinal).SelectMany(d => d.Encendidos).Distinct().Count();
                if (vistosAntes * 2 >= total) riesgo -= 5;
            }
            return riesgo;
        }

        public int DefectosPorDespliegues() { return R.DefectosDeDespliegue; }

        // ==================================================================== el producto y el cliente

        /// <summary>El producto, modulo a modulo, en la etapa en que esta hoy (boceto, mockup, prototipo, MVP, en produccion).</summary>
        public List<EstadoDelModulo> Producto() {
            return MaquetaDelProducto.Calcular(Perfil.Proyecto, W.Avance, W.Alcance, R.ModulosDesplegados);
        }

        /// <summary>Lo que dicen hoy del producto quienes lo van a usar.</summary>
        public List<VozDelCliente> VocesDelProducto() {
            return VocesDelCliente.Para(Perfil.Proyecto, Producto(), W.SatisfaccionCliente);
        }

        /// <summary>Los papeles del cliente que ya han llegado, del mas nuevo al mas viejo.</summary>
        public List<PapelDelCliente> PapelesDelCliente() {
            return Perfil.Proyecto == null ? new List<PapelDelCliente>() : Perfil.Proyecto.PapelesHasta(Math.Max(1, R.DiaActual));
        }

        public int PapelesSinLeer() { return PapelesDelCliente().Count(p => !R.PapelesLeidos.Contains(p.Id)); }

        public void MarcarPapelesLeidos() {
            foreach (var p in PapelesDelCliente()) if (!R.PapelesLeidos.Contains(p.Id)) R.PapelesLeidos.Add(p.Id);
        }
    }
}
