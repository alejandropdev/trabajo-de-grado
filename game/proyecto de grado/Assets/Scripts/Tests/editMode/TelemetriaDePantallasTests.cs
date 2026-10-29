using System.Linq;
using Nexus.Core;
using NUnit.Framework;

namespace Nexus.Tests {
    /// <summary>La telemetria local de las pruebas con usuarios: tiempo por pantalla, pantallas saltadas y dia de abandono.</summary>
    public class TelemetriaDePantallasTests {
        [Test]
        public void Mide_el_tiempo_de_cada_pantalla_y_cuenta_las_que_se_pasan_sin_leer() {
            var t = new TelemetriaDePantallas(new AlmacenEnMemoria());
            t.EntrarA("PantallaDelDia", 0, 1);
            t.EntrarA("PantallaDeTarjetas", 10, 1);   // el dia duro 10 s
            t.EntrarA("PantallaDelDia", 11, 1);       // la tarjeta duro 1 s: saltada
            t.Terminar(31);

            var dia = t.Datos.Pantallas["PantallaDelDia"];
            Assert.AreEqual(2, dia.Visitas);
            Assert.AreEqual(30.0, dia.Segundos, 1e-9);   // 10 + 20
            Assert.AreEqual(0, dia.Saltadas);

            var tarjeta = t.Datos.Pantallas["PantallaDeTarjetas"];
            Assert.AreEqual(1, tarjeta.Saltadas);
            Assert.AreEqual(100.0, tarjeta.PorcentajeSaltadas, 1e-9);
        }

        [Test]
        public void Recuerda_hasta_que_dia_se_llego() {
            var t = new TelemetriaDePantallas(new AlmacenEnMemoria());
            t.EntrarA("A", 0, 2);
            t.EntrarA("B", 5, 4);
            t.EntrarA("C", 9, 3);   // volver a un dia anterior no lo baja
            Assert.AreEqual(4, t.Datos.DiaMasAltoAlcanzado);
        }

        [Test]
        public void Se_guarda_y_se_suma_entre_sesiones() {
            var almacen = new AlmacenEnMemoria();
            var primera = new TelemetriaDePantallas(almacen);
            primera.EntrarA("A", 0, 3);
            primera.Terminar(8);

            var segunda = new TelemetriaDePantallas(almacen);
            Assert.AreEqual(2, segunda.Datos.Sesiones);
            Assert.AreEqual(3, segunda.Datos.DiaMasAltoAlcanzado);
            Assert.AreEqual(8.0, segunda.Datos.Pantallas["A"].Segundos, 1e-9);
            Assert.AreEqual("A", segunda.PorTiempo().First().Key);
        }

        [Test]
        public void Un_archivo_roto_no_rompe_la_telemetria() {
            var almacen = new AlmacenEnMemoria();
            almacen.Guardar(TelemetriaDePantallas.Clave, "{ no es json");
            var t = new TelemetriaDePantallas(almacen);
            Assert.AreEqual(1, t.Datos.Sesiones);
        }
    }
}
