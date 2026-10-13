using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Narrativa;
using Nexus.Core.Proyecto;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «El producto»: la maqueta viva de lo que se esta construyendo. Cada modulo se ve en la etapa en que esta (un
    /// boceto a lapiz, un mockup, un prototipo que responde, un MVP que funciona) y se puede tocar: en un prototipo
    /// el boton responde y no guarda nada; en un MVP lo que haces en un modulo aparece en los que dependen de el
    /// (registras una visita y sale en la lista). Debajo, lo que dice de su parte quien la va a usar.
    ///
    /// No cambia nada del proyecto: es para ver y para probar. El reloj del dia espera mientras esta abierta.
    /// </summary>
    public sealed class PantallaDelProducto : Pantalla {
        public override bool EsModal { get { return true; } }
        public override bool PuedeVolver { get { return true; } }

        private const float W = MarcoDePizarra.W;

        private Lamina _lamina;
        private bool _pausadoAntes;
        private int _registros;                 // cuantas veces se uso un modulo que funciona y tiene accion
        private string _elegido, _mensaje;
        private bool _acabaDePulsar;

        public static void Abrir(AppRoot app) {
            if (app.Sesion == null || app.Sesion.Perfil.Proyecto == null) return;
            app.Router.Apilar<PantallaDelProducto>();
        }

        /// <summary>
        /// «Cómo está armado»: el plano de una arquitectura con los modulos de este proyecto, en un pizarron. Sirve
        /// para la elegida (durante el nivel) y para comparar las opciones antes de elegir (en la Fase 1).
        /// </summary>
        public static void AbrirPlano(AppRoot app, string arquitecturaId, bool elegida) {
            var s = app.Sesion;
            if (s == null || s.Perfil.Proyecto == null || s.Perfil.Fase1 == null) return;
            var opcion = s.Perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == arquitecturaId);
            if (opcion == null) return;
            var ficha = s.Perfil.Proyecto;
            var plano = new PaginaDePizarra { Titulo = elegida ? "Cómo está armado tu sistema" : "Cómo quedaría armado" };
            var consecuencias = new PaginaDePizarra {
                Titulo = "Lo que eso significa cada día",
                Columnas = new List<ColumnaDePizarra> {
                    new ColumnaDePizarra { Dibujo = "servidor", Titulo = "Al desplegar", Texto = Recortar(DibujosDeArquitectura.AlDesplegar(opcion.Id)), Color = "mostaza" },
                    new ColumnaDePizarra { Dibujo = "equipo", Titulo = "En el día a día", Texto = Recortar(DibujosDeArquitectura.EnElDia(opcion.Id)), Color = "cian" }
                }
            };
            var paginas = new List<PaginaDePizarra> { plano, consecuencias };
            var dibujos = new Dictionary<PaginaDePizarra, Action<Tizador, float>>();
            dibujos[plano] = (t, y) => {
                var k = Mathf.Min((W - 120) / DibujosDeArquitectura.Ancho, (800 - (y + 20)) / DibujosDeArquitectura.Alto);
                DibujosDeArquitectura.Plano(t.En((W - DibujosDeArquitectura.Ancho * k) / 2, y + 20, k), ficha, opcion.Id, opcion.Nombre);
            };
            var concepto = Glosario.Buscar(app.Catalogo.Glosario, opcion.Id);
            app.Router.Apilar<PantallaDePizarra>(p => {
                p.Etiqueta = "ARQUITECTURA";
                p.Tema = opcion.Nombre + " · " + ficha.Nombre;
                p.Paginas = paginas;
                p.Dibujos = dibujos;
                if (concepto != null) foreach (var pagina in Glosario.PaginasDe(concepto)) paginas.Add(pagina);
            });
        }

        private static string Recortar(string s) { return s != null && s.Length > 170 ? s.Substring(0, 167) + "…" : s; }

        protected override void Construir() {
            if (App.Runner != null) { _pausadoAntes = App.Runner.Pausado; App.Runner.Pausado = true; }
            var marco = MarcoDePizarra.Construir(Ui, Raiz, "EL PRODUCTO", out _lamina);
            MarcoDePizarra.Boton(Ui, marco, "Cerrar", () => App.Router.Volver(), new Vector2(1, 0), new Vector2(-52, 44), 220, VarianteBoton.Secundario);
            var etapas = Glosario.Buscar(App.Catalogo.Glosario, "mvp");
            if (etapas != null)
                MarcoDePizarra.Boton(Ui, marco, "¿Qué es cada etapa?", () => PantallaDePizarra.AbrirConcepto(App, etapas), new Vector2(0, 0), new Vector2(52, 44), 300, VarianteBoton.Secundario);
            // El plano de la arquitectura elegida, con estos mismos modulos.
            if (!string.IsNullOrEmpty(App.Sesion.ArquitecturaElegida))
                MarcoDePizarra.Boton(Ui, marco, "Cómo está armado", () => AbrirPlano(App, App.Sesion.ArquitecturaElegida, true), new Vector2(0, 0), new Vector2(372, 44), 280);
            Pintar();
        }

        public override void Repintar() { if (_lamina != null) Pintar(); }

        private void Pintar() {
            MarcoDePizarra.Limpiar(_lamina);
            var s = App.Sesion;
            var ficha = s.Perfil.Proyecto;
            var modulos = s.Producto();
            var t = new Tizador(_lamina, Ui.FuenteTiza);

            t.Texto(60, 44, ("El producto · " + ficha.Nombre).ToUpperInvariant(), 22, Tizador.Gris);
            var general = MaquetaDelProducto.EtapaGeneral(modulos);
            t.Texto(W / 2, 92, "Hoy es un " + EtapasDelProducto.Nombre(general), 42, Tizador.Tiza, true);
            t.Linea(360, 112, W - 360, 112, null, 2);
            t.Texto(W / 2, 148, "Cada parte está donde la dejó el trabajo de estos días. Tócalas: las que ya funcionan, responden.", 22, Tizador.Gris, true);

            // La rejilla de modulos: 2 columnas hasta cuatro, 3 a partir de ahi.
            var n = modulos.Count;
            var cols = n <= 4 ? 2 : 3;
            var filas = Mathf.CeilToInt(n / (float)cols);
            const float y0 = 176, hueco = 18, altoTotal = 520;
            var ancho = (W - 120 - hueco * (cols - 1)) / cols;
            var alto = (altoTotal - hueco * (filas - 1)) / filas;
            var k = Mathf.Min((ancho - 30) / DibujosDelProducto.Ancho, (alto - 64) / DibujosDelProducto.Alto);
            for (var i = 0; i < n; i++) {
                var m = modulos[i];
                var x = 60 + (i % cols) * (ancho + hueco);
                var y = y0 + (i / cols) * (alto + hueco);
                var elegido = _elegido == m.Modulo.Id;
                t.Caja(x, y, ancho, alto, null, elegido ? Tizador.Mostaza : Tizador.Gris, !elegido);
                t.Texto(x + 16, y + 32, m.Modulo.Nombre, 24, elegido ? Tizador.Mostaza : Tizador.Tiza);
                t.Texto(x + ancho - 16 - 260, y + 32, $"{EtapasDelProducto.Nombre(m.Etapa)} · {m.Porcentaje:0} %", 20, ColorDe(m.Etapa), false, 260).alignment = TextAlignmentOptions.TopRight;
                var lapiz = t.En(x + (ancho - DibujosDelProducto.Ancho * k) / 2, y + 50, k);
                DibujosDelProducto.Modulo(lapiz, m.Modulo, m.Etapa, FilasDe(m), elegido && _acabaDePulsar);
                var modulo = m;
                _lamina.Zona(x, y, ancho, alto, () => Usar(modulo), "Modulo " + m.Modulo.Id);
            }

            // Abajo: lo que acaba de pasar, o lo que dice quien va a usar la parte elegida.
            var pie = y0 + altoTotal + 30;   // hasta tres renglones antes de los botones (842)
            var voz = s.VocesDelProducto().FirstOrDefault(v => v.ModuloId == _elegido) ?? s.VocesDelProducto().FirstOrDefault();
            if (!string.IsNullOrEmpty(_mensaje)) t.Texto(60, pie, _mensaje, 23, Tizador.Mostaza, false, W - 120);
            else if (voz != null) t.Texto(60, pie, $"{voz.Quien}: «{voz.Frase}»", 23, Tizador.Cian, false, W - 120);
        }

        private static Color ColorDe(string etapa) {
            switch (etapa) {
                case EtapasDelProducto.Boceto: return Tizador.Gris;
                case EtapasDelProducto.Mockup: return Tizador.Tiza;
                case EtapasDelProducto.Prototipo: return Tizador.Mostaza;
                default: return Tizador.Cian;
            }
        }

        private static bool Funciona(EstadoDelModulo m) { return m.Etapa == EtapasDelProducto.Mvp || m.Etapa == EtapasDelProducto.Desplegado; }

        /// <summary>Cuantos datos se ven ya en un modulo que funciona: los que el jugador ha ido metiendo en la maqueta.</summary>
        private int FilasDe(EstadoDelModulo m) { return Funciona(m) ? _registros : 0; }

        private void Usar(EstadoDelModulo m) {
            _elegido = m.Modulo.Id;
            _acabaDePulsar = false;
            var nombre = "«" + m.Modulo.Nombre + "»";
            if (m.Etapa == EtapasDelProducto.Boceto) _mensaje = $"{nombre} es todavía un boceto: {Minuscula(EtapasDelProducto.QueEs(m.Etapa))}";
            else if (m.Etapa == EtapasDelProducto.Mockup) _mensaje = $"{nombre} es un mockup: {Minuscula(EtapasDelProducto.QueEs(m.Etapa))}";
            else if (m.Etapa == EtapasDelProducto.Prototipo) {
                _acabaDePulsar = true;
                _mensaje = $"{nombre} es un prototipo: el botón responde, pero no guarda nada. Cierra y vuelve a abrir: no queda rastro.";
            } else if (string.IsNullOrEmpty(m.Modulo.Accion)) {
                var conAccion = App.Sesion.Producto().FirstOrDefault(x => Funciona(x) && !string.IsNullOrEmpty(x.Modulo.Accion));
                _mensaje = conAccion == null
                    ? $"{nombre} ya funciona, pero muestra lo que entra por otro módulo que todavía no está listo. Las partes se necesitan unas a otras."
                    : $"{nombre} muestra lo que pasa en el sistema. Prueba «{conAccion.Modulo.Accion}» en «{conAccion.Modulo.Nombre}» y vuelve a mirar aquí.";
            } else {
                _acabaDePulsar = true;
                _registros++;
                var seEnteran = App.Sesion.Producto().Where(x => x.Modulo.Id != m.Modulo.Id && Funciona(x)).Select(x => "«" + x.Modulo.Nombre + "»").ToList();
                var sinEnterarse = App.Sesion.Producto().Where(x => x.Modulo.Id != m.Modulo.Id && !Funciona(x)).Select(x => "«" + x.Modulo.Nombre + "»").ToList();
                _mensaje = $"«{m.Modulo.Accion}»: hecho, y guardado. " +
                           (seEnteran.Count > 0 ? "Mira " + string.Join(", ", seEnteran) + ": ya lo reflejan. " : "") +
                           (sinEnterarse.Count > 0 ? string.Join(", ", sinEnterarse) + " aún no, porque no están terminados." : "");
            }
            Pintar();
        }

        private static string Minuscula(string s) { return string.IsNullOrEmpty(s) ? "" : char.ToLowerInvariant(s[0]) + s.Substring(1); }

        private void OnDestroy() {
            if (App != null && App.Runner != null) App.Runner.Pausado = _pausadoAntes;
        }
    }
}
