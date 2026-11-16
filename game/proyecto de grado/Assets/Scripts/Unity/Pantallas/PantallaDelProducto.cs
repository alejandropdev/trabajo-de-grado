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
    /// Las entradas a «El producto» y a «Cómo está armado». La pantalla de la maqueta viva es ProductoVivo (desde 2026-11-11);
    /// la rejilla de modulos de tiza que vivia aqui se quito (2026-11-15) porque ninguna pantalla la abria.
    /// </summary>
    public static class PantallaDelProducto {
        private const float W = MarcoDePizarra.W;

        public static void Abrir(AppRoot app) {
            // «El producto» tiene desde 2026-11-11 su propia pantalla, con el lenguaje de los indicadores (ProductoVivo).
            ProductoVivo.Abrir(app);
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
    }
}
