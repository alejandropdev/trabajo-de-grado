#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Guardado;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Unity.Pantallas;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Aplicacion {
    /// <summary>
    /// (2026-11-15, direccion de arte) El pizarron tal como se ve en el juego: abre la PantallaDePizarra de verdad con
    /// una sola pagina de un termino del glosario, la captura y la cierra. La capa de la galeria se queda transparente.
    /// </summary>
    public sealed partial class CapturadorDeGaleria {
        /// <summary>Lo que hay que deshacer despues de capturar la pagina (cerrar la pantalla apilada).</summary>
        private Action _despues;

        /// <summary>El indice «Aprender a fondo» de un curso (scrum, kanban, cascada).</summary>
        private void IndiceDeCurso(RectTransform capa, string termino) {
            var fondo = capa.GetComponent<Image>();
            if (fondo != null) fondo.color = new Color(0, 0, 0, 0);
            var app = AppRoot.Instancia;
            var e = Glosario.Buscar(app.Catalogo.Glosario, termino);
            if (e == null) throw new InvalidOperationException("no hay termino " + termino);
            PantallaDeCurso.Abrir(app, e);
            _despues = () => app.Router.Volver();
        }

        /// <summary>Una partida del primer nivel con un perfil de capturas, si la galeria aun no tiene sesion (la de la Fase 1 hace lo mismo).</summary>
        private void AbrirSesionDeCaptura(AppRoot app) {
            if (app.Sesion != null) return;
            app.SeleccionarPerfil(new PlayerProfile { idPerfil = CapturadorDeFase1.IdDePerfil, nombreEstudiante = "Capturas", preTestHecho = true });
            var nivel = ProgresionDeNiveles.Primero(app.Catalogo);
            var partida = app.CrearPartida("capturas");
            partida.Partida.NivelActualId = nivel;
            partida.Partida.NivelAndamiaje = app.Catalogo.Niveles[nivel].NivelAndamiaje;
            app.AbrirPartida(partida);
            app.Sesion.R.IntroVista = true;
        }

        /// <summary>
        /// La carpeta del cliente y «Cómo está armado», con la sesion de verdad: que = 0 el cliente, 1 las voces, 2 un papel,
        /// 10+ el plano de la arquitectura n-10 de la Fase 1.
        /// </summary>
        private void PizarronDelCliente(RectTransform capa, int que) {
            var fondo = capa.GetComponent<Image>();
            if (fondo != null) fondo.color = new Color(0, 0, 0, 0);
            var app = AppRoot.Instancia;
            AbrirSesionDeCaptura(app);
            var s = app.Sesion;
            var ficha = s.Perfil.Proyecto;
            if (ficha == null) throw new InvalidOperationException("la sesion no tiene proyecto");
            if (que >= 10) {
                var opciones = s.Perfil.Fase1.Arquitecturas;
                PantallaDelProducto.AbrirPlano(app, opciones[Mathf.Clamp(que - 10, 0, opciones.Count - 1)].Id, true);
                _despues = () => app.Router.Volver();
                return;
            }
            var pagina = new PaginaDePizarra { Titulo = que == 0 ? "Quién es tu cliente" : que == 1 ? "Lo que dicen hoy del producto" : "Un papel del cliente" };
            var dibujos = new Dictionary<PaginaDePizarra, Action<Tizador, float>>();
            if (que == 0) dibujos[pagina] = (t, y) => DibujosDelProducto.Cliente(t, ficha, y);
            else if (que == 1) { var voces = s.VocesDelProducto(); var modulos = s.Producto(); dibujos[pagina] = (t, y) => DibujosDelProducto.Voces(t, voces, modulos, y); }
            else {
                var papeles = s.PapelesDelCliente();
                var papel = papeles[Mathf.Clamp(que - 2, 0, papeles.Count - 1)];
                pagina.Titulo = papel.Titulo;
                dibujos[pagina] = (t, y) => DibujosDelProducto.Papel(t.En((MarcoDePizarra.W - DibujosDelProducto.Ancho * 1.5f) / 2, y + 30, 1.5f), papel);
            }
            app.Router.Apilar<PantallaDePizarra>(pp => {
                pp.Etiqueta = "EL CLIENTE";
                pp.Tema = "La carpeta del cliente · " + ficha.Cliente.Nombre;
                pp.Paginas = new List<PaginaDePizarra> { pagina };
                pp.Dibujos = dibujos;
            });
            _despues = () => app.Router.Volver();
        }

        private void Pizarron(RectTransform capa, string termino, int pagina) {
            var fondo = capa.GetComponent<Image>();
            if (fondo != null) fondo.color = new Color(0, 0, 0, 0);
            var app = AppRoot.Instancia;
            var e = Glosario.Buscar(app.Catalogo.Glosario, termino);
            if (e == null) throw new InvalidOperationException("no hay termino " + termino);
            var paginas = Glosario.PaginasDe(e);
            var p = paginas[Mathf.Clamp(pagina, 0, paginas.Count - 1)];
            app.Router.Apilar<PantallaDePizarra>(s => {
                s.Etiqueta = "¿QUÉ ES?";
                s.Tema = e.Tema;
                s.Paginas = new List<PaginaDePizarra> { p };
                s.ConCurso = e.Curso != null && e.Curso.Count > 0 ? e : null;
            });
            _despues = () => app.Router.Volver();
        }
    }
}
#endif
