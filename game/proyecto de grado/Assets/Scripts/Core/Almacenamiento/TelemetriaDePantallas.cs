using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Nexus.Core {
    /// <summary>Lo que se sabe de una pantalla: cuantas veces se abrio, cuanto se estuvo en ella y cuantas se cerro casi al instante.</summary>
    public sealed class MedidaDePantalla {
        public int Visitas;
        public double Segundos;
        /// <summary>Visitas que duraron menos de TelemetriaDePantallas.UmbralDeSaltada segundos: se pasaron sin leer.</summary>
        public int Saltadas;

        [JsonIgnore] public double SegundosPorVisita { get { return Visitas == 0 ? 0 : Segundos / Visitas; } }
        [JsonIgnore] public double PorcentajeSaltadas { get { return Visitas == 0 ? 0 : 100.0 * Saltadas / Visitas; } }
    }

    public sealed class DatosDeTelemetria {
        public Dictionary<string, MedidaDePantalla> Pantallas = new Dictionary<string, MedidaDePantalla>(StringComparer.Ordinal);
        /// <summary>El dia mas alto que se llego a jugar: donde la gente se queda.</summary>
        public int DiaMasAltoAlcanzado;
        public int Sesiones;
    }

    /// <summary>
    /// Telemetria LOCAL para las pruebas con usuarios (reduccion de texto del Nivel 0): cuanto tiempo se pasa en cada
    /// pantalla, que porcentaje se salta sin leer y hasta que dia se llega. No sale de la maquina: se guarda en un
    /// archivo del almacen, y quien conduce la prueba lo recoge. No registra decisiones ni nada del jugador.
    /// </summary>
    public sealed class TelemetriaDePantallas {
        public const string Clave = "telemetria.json";
        /// <summary>Una pantalla que se cierra antes de esto no se leyo.</summary>
        public const double UmbralDeSaltada = 2.0;

        private readonly IAlmacen _almacen;
        private string _actual;
        private double _desde;

        public DatosDeTelemetria Datos { get; private set; }

        public TelemetriaDePantallas(IAlmacen almacen) {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
            Datos = Cargar();
            Datos.Sesiones++;
        }

        /// <summary>Se abrio (o volvio a ser la de arriba) una pantalla. Cierra la medida de la anterior.</summary>
        public void EntrarA(string pantalla, double ahoraEnSegundos, int dia) {
            CerrarLaActual(ahoraEnSegundos);
            if (string.IsNullOrEmpty(pantalla)) return;
            _actual = pantalla;
            _desde = ahoraEnSegundos;
            if (dia > Datos.DiaMasAltoAlcanzado) Datos.DiaMasAltoAlcanzado = dia;
        }

        /// <summary>Cierra la pantalla en curso (al salir de la aplicacion) y guarda.</summary>
        public void Terminar(double ahoraEnSegundos) {
            CerrarLaActual(ahoraEnSegundos);
            Guardar();
        }

        public void Guardar() {
            _almacen.Guardar(Clave, JsonConvert.SerializeObject(Datos, Formatting.Indented));
        }

        /// <summary>Las pantallas donde mas se pasa el tiempo, de mayor a menor.</summary>
        public IEnumerable<KeyValuePair<string, MedidaDePantalla>> PorTiempo() {
            return Datos.Pantallas.OrderByDescending(p => p.Value.Segundos);
        }

        private void CerrarLaActual(double ahora) {
            if (_actual == null) return;
            var duracion = Math.Max(0, ahora - _desde);
            MedidaDePantalla medida;
            if (!Datos.Pantallas.TryGetValue(_actual, out medida)) Datos.Pantallas[_actual] = medida = new MedidaDePantalla();
            medida.Visitas++;
            medida.Segundos += duracion;
            if (duracion < UmbralDeSaltada) medida.Saltadas++;
            _actual = null;
        }

        private DatosDeTelemetria Cargar() {
            if (!_almacen.Existe(Clave)) return new DatosDeTelemetria();
            try {
                return JsonConvert.DeserializeObject<DatosDeTelemetria>(_almacen.Cargar(Clave)) ?? new DatosDeTelemetria();
            } catch (JsonException) {
                return new DatosDeTelemetria();
            }
        }
    }
}
