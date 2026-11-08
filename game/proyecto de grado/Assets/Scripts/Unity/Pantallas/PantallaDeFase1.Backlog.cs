using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Proyecto;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using A = Nexus.Unity.Pantallas.ActoresDeEscena;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El product backlog de la Fase 1 en DOS partes (2026-11-08), con el lenguaje de cristal neon de las demas y sin scroll:
    ///   0 · Lo que pide el cliente   quien lo va a usar (caras; al pasar el raton, lo que necesita), una animacion generica
    ///                                de tres escenas (historias · el orden · lo que nadie pidio) y las cifras del paso.
    ///                                Las escenas NO enseñan las historias reales: eso es lo que se decide en la parte 2.
    ///   1 · Arma tu backlog          las candidatas por parte del sistema (fichas compactas) y, a la derecha, tu backlog
    ///                                numerado, con el anillo «N de M». Se arrastra o se pulsa.
    /// Si el proyecto ya estaba en marcha («heredado»), hay una sola parte: las partes del sistema con su avance.
    /// </summary>
    public sealed partial class PantallaDeFase1 {
        private const int PartesDeBacklog = 2;
        private RectTransform _capaDeArrastre;

        private int PartesDelBacklog { get { return S.SeCreaElBacklog ? PartesDeBacklog : 1; } }

        private RectTransform CapaDeArrastre() {
            if (_capaDeArrastre == null) _capaDeArrastre = PiezasDelMuro.CapaDeArrastre(Ui, Raiz);
            _capaDeArrastre.SetAsLastSibling();
            return _capaDeArrastre;
        }

        private Color ColorDeModulo(int i) {
            var paleta = new[] { Tema.cyan, Tema.violet, Tizador.Mostaza, Tema.success, Tizador.Roja, Tizador.Cian };
            return paleta[Mathf.Abs(i) % paleta.Length];
        }

        private void Backlog() {
            var f = Perfil.Proyecto;
            if (f == null) return;
            if (!S.SeCreaElBacklog) { BacklogHeredado(f); return; }
            _subPaso = Mathf.Clamp(_subPaso, 0, PartesDeBacklog - 1);
            if (_subPaso == 0) BacklogCliente(f); else BacklogArmar(f);
        }

        private void CabeceraDelBacklog(Hoja hoja, string titulo, string[] palabras, System.Action<RectTransform> derecha = null) {
            var fila = hoja.Fila();
            var textos = Ui.Columna(fila, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(textos, flexAncho: 1);
            var parte = S.SeCreaElBacklog ? " · " + (_subPaso + 1) + " de " + PartesDeBacklog : "";
            var etiqueta = Ui.Texto(textos, "Antes de construir · Backlog" + parte, EstiloTexto.Leyenda, Tema.cyan);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Texto(textos, titulo, EstiloTexto.Titulo);
            if (palabras != null && palabras.Length > 0) PantallaDeGlosario.Palabras(App, fila, "", palabras);
            if (derecha != null) derecha(fila);
        }

        // ==================================================================== 0 · lo que pide el cliente

        private void BacklogCliente(FichaDelProyecto f) {
            var hoja = LienzoSinScroll("Backlog · cliente");
            CabeceraDelBacklog(hoja, "Lo que pide el cliente", new[] { "backlog", "historia-de-usuario", "mvp", "alcance" });

            var cuerpo = Ui.Fila(hoja.Raiz, "Cuerpo", Tema.Espacio(3), 0, TextAnchor.UpperLeft);
            var leCuerpo = UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
            leCuerpo.preferredHeight = Tema.Px(320); leCuerpo.minHeight = Tema.Px(240);
            cuerpo.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            // ---- quien lo va a usar: caras y, al pasar el raton, lo que necesita
            var usan = Ui.Columna(cuerpo, "Usuarios", Tema.Espacio(2), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(usan, ancho: Tema.Px(260), flexAlto: 1);
            PiezasDeProyecto.Cristal(Ui, usan, NexusTheme.Alfa(Tema.warning, 0.5f));
            usan.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(12, 12, 12, 12);
            usan.gameObject.AddComponent<ListaDeElementos>();
            var rotulo = Ui.Texto(usan, "QUIÉN LO VA A USAR", EstiloTexto.Leyenda, Tema.warning, TextAlignmentOptions.Center);
            UiKit.Tamano(rotulo, alto: Tema.Px(16));
            foreach (var u in f.Usuarios.Take(5)) {
                var persona = u;
                var fila = Ui.Fila(usan, "Usuario " + u.Nombre, Tema.Espacio(2), 6, TextAnchor.MiddleLeft);
                UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1).minHeight = Tema.Px(48);
                var fondo = fila.gameObject.AddComponent<Image>();
                Ui.Fondo(fondo, NexusTheme.Alfa(Tema.bg900, 0.6f), NexusTheme.RadioMd);
                RostroDePersonaje.Crear(Ui, fila, MaterialesNexus.IdDePersonaje(u.Nombre), u.Nombre, Tema.Px(44), Tema.warning);
                var texto = Ui.Texto(fila, u.Nombre, EstiloTexto.Cuerpo, Tema.ink, TextAlignmentOptions.MidlineLeft);
                TextoEnCaja.Ajustar(texto, Tema.Px(16));
                UiKit.Tamano(texto, flexAncho: 1, alto: Tema.Px(40));
                fondo.raycastTarget = true;
                var r = fila.gameObject.AddComponent<RealceDeCalidad>();
                r.Fondo = fondo; r.Normal = NexusTheme.Alfa(Tema.bg900, 0.6f); r.Resaltado = NexusTheme.Alfa(Tema.warning, 0.22f); r.Escala = 1.03f;
                r.Entra = () => Tooltip().Mostrar(persona.Nombre, "Necesita: " + persona.QueNecesita, Tema.warning);
                r.Sale = () => Tooltip().Ocultar();
            }

            // ---- la escena: historias · el orden · lo que nadie pidio (genericas: no destapan las reales)
            var escena = Ui.Nodo(cuerpo, "Escena del backlog");
            UiKit.Tamano(escena, flexAncho: 1, flexAlto: 1);
            var animacion = EscenaDeMetodologia.Crear(Ui, escena, EscenasDelBacklog.Fases(), "El backlog en escenas");
            UiKit.Rellenar((RectTransform)animacion.transform);

            // ---- las cifras del paso
            var datos = Ui.Fila(hoja.Raiz, "Datos", Tema.Espacio(3), 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(datos, flexAncho: 1);
            datos.gameObject.AddComponent<ListaDeElementos>();
            var ancho = Tema.Px(170); var alto = Tema.Px(64);
            PiezasDeProyecto.Dato(Ui, datos, f.Historias.Count.ToString(), "CANDIDATAS", Tema.cyan, "Historias candidatas",
                                  "Lo que se podría construir. Unas las pidió el cliente; otras no deberían entrar.", Tooltip, ancho, alto);
            PiezasDeProyecto.Dato(Ui, datos, MinimoDeHistorias.ToString(), "MÍNIMO", Tema.success, "Mínimo para empezar",
                                  "Al menos una historia de cada parte del sistema: con menos, no hay sistema que construir.", Tooltip, ancho, alto);
            PiezasDeProyecto.Dato(Ui, datos, f.Modulos.Count.ToString(), "PARTES", Tema.violet, "Partes del sistema",
                                  string.Join("\n", f.Modulos.Select(m => "· " + m.Nombre)), Tooltip, ancho, alto);
        }

        // ==================================================================== 1 · arma tu backlog

        private void BacklogArmar(FichaDelProyecto f) {
            var hoja = LienzoSinScroll("Backlog · armar");
            var capa = CapaDeArrastre();
            var minimo = MinimoDeHistorias;
            // En el primer nivel (tutorial) se dice cuantas pidio el cliente: el anillo es «N de 7», con su pista.
            var pedidas = f.Historias.Count(x => x.Tipo == TiposDeHistoria.Pedida);
            var conPista = Perfil.Id == "nivel-00" && pedidas > 0;
            var meta = conPista ? pedidas : minimo;
            CabeceraDelBacklog(hoja, "Arma tu backlog", null, fila => {
                var lleno = conPista ? _backlog.Count == meta : _backlog.Count >= meta;
                var anillo = PiezasDeProyecto.Anillo(Ui, fila, Tema.Px(62), Mathf.Clamp01((float)_backlog.Count / meta),
                                                     lleno ? Tema.success : Tema.warning, _backlog.Count + "/" + meta);
                UiKit.Tamano((RectTransform)anillo, Tema.Px(62), Tema.Px(62));
                var texto = Ui.Texto(fila, conPista ? "el cliente pidió " + pedidas + " historias" : "mínimo " + minimo + ": una por parte",
                                     EstiloTexto.Leyenda, conPista ? Tema.cyan : Tema.inkMuted);
                UiKit.Tamano(texto, ancho: Tema.Px(150));
            });

            var cuerpo = Ui.Fila(hoja.Raiz, "Cuerpo", Tema.Espacio(3), 0, TextAnchor.UpperLeft);
            var leCuerpo = UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
            leCuerpo.preferredHeight = Tema.Px(430); leCuerpo.minHeight = Tema.Px(300);
            cuerpo.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            // ---- a la izquierda: las candidatas, una columna por parte del sistema
            var monton = Ui.Fila(cuerpo, "Candidatas", Tema.Espacio(2), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(monton, ancho: 0, flexAncho: 3, flexAlto: 1);
            monton.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            monton.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(10, 10, 10, 10);
            PiezasDeProyecto.Cristal(Ui, monton, NexusTheme.Alfa(Tema.inkMuted, 0.55f), 0.45f);
            GuiaView.Registrar("fase1.historias", monton);
            var destinoMonton = monton.gameObject.AddComponent<DestinoDeFicha>();
            destinoMonton.Ui = Ui; destinoMonton.Acento = Tema.inkMuted; destinoMonton.Radio = 16;
            destinoMonton.Motivo = (ficha, _) => null;
            destinoMonton.Coste = ficha => ficha.Origen == "backlog" ? "sacarla" : null;
            destinoMonton.AlRecibir = (ficha, _) => {
                if (ficha.Origen != "backlog") return;
                _backlog.Remove(ficha.Id);
                Repintar();
            };
            for (var i = 0; i < f.Modulos.Count; i++) {
                var m = f.Modulos[i];
                var color = ColorDeModulo(i);
                var propias = f.Historias.Where(x => x.Modulo == m.Id).ToList();
                if (propias.Count == 0) continue;
                var col = Ui.Columna(monton, "Parte " + m.Nombre, Tema.Espacio(1), 0, TextAnchor.UpperLeft);
                UiKit.Tamano(col, ancho: 0, flexAncho: 1, flexAlto: 1);
                var cab = Ui.Fila(col, "Cabecera", 6, 0, TextAnchor.MiddleLeft);
                UiKit.Tamano(cab, alto: Tema.Px(26), flexAncho: 1);
                Ui.Punto(cab, color, 10);
                var nombre = Ui.Texto(cab, m.Nombre, EstiloTexto.Encabezado, color, TextAlignmentOptions.MidlineLeft);
                nombre.fontStyle = FontStyles.Bold;
                TextoEnCaja.Ajustar(nombre, Tema.Px(16));
                UiKit.Tamano(nombre, flexAncho: 1, alto: Tema.Px(24));
                var yaEsta = propias.Count(x => _backlog.Contains(x.Id));
                foreach (var h in propias) {
                    if (_backlog.Contains(h.Id)) continue;
                    var ficha = FichaDeBacklog.Crear(Ui, col, capa, h, color, 0, "monton", Tooltip, false);
                    ConectarFicha(ficha, h.Id, capa);
                }
                if (yaEsta == propias.Count) PiezasDelMuro.Vacio(Ui, col, "ya están todas", Tema.Px(54));
            }

            // ---- a la derecha: tu backlog, numerado, con lo de arriba primero
            var carril = Ui.Columna(cuerpo, "Tu backlog", Tema.Espacio(1), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(carril, ancho: 0, flexAncho: 2, flexAlto: 1);
            PiezasDeProyecto.Cristal(Ui, carril, NexusTheme.Alfa(Tema.cyan, 0.75f), 0.55f);
            carril.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(10, 10, 10, 10);
            GuiaView.Registrar("fase1.mibacklog", carril);
            var cabeza = Ui.Fila(carril, "Cabecera", 6, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(cabeza, alto: Tema.Px(26), flexAncho: 1);
            var titulo = Ui.Texto(cabeza, "TU BACKLOG", EstiloTexto.Encabezado, Tema.cyan, TextAlignmentOptions.MidlineLeft);
            titulo.fontStyle = FontStyles.Bold;
            UiKit.Tamano(titulo, flexAncho: 1);
            var lista = Ui.Columna(carril, "Lista", Tema.Espacio(1), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(lista, flexAncho: 1, flexAlto: 1);
            var arriba = Ui.Fila(lista, "Arriba", 6, 0, TextAnchor.MiddleLeft);
            UiKit.Tamano(arriba, alto: Tema.Px(30), flexAncho: 1);
            PiezasDelMuro.Flecha(Ui, arriba, Tema.Px(22), Tema.Px(28), true, Tema.cyan);
            var pista = Ui.Texto(arriba, "arriba, lo que más le vale al cliente", EstiloTexto.Leyenda, Tema.cyan, TextAlignmentOptions.MidlineLeft);
            TextoEnCaja.Ajustar(pista, Tema.Px(12));
            UiKit.Tamano(pista, flexAncho: 1, alto: Tema.Px(28));
            for (var i = 0; i < _backlog.Count; i++) {
                var h = f.Historia(_backlog[i]);
                if (h == null) continue;
                var color = ColorDeModulo(f.Modulos.FindIndex(x => x.Id == h.Modulo));
                var ficha = FichaDeBacklog.Crear(Ui, lista, capa, h, color, i + 1, "backlog", Tooltip, true);
                ConectarFicha(ficha, h.Id, capa);
            }
            // los huecos que faltan: uno por cada parte del sistema que aun no tiene ninguna historia
            if (_backlog.Count < minimo)
                foreach (var m in f.Modulos.Where(m => !_backlog.Any(id => { var h = f.Historia(id); return h != null && h.Modulo == m.Id; })))
                    PiezasDelMuro.Vacio(Ui, lista, "falta una de " + m.Nombre, Tema.Px(36));
            var destino = carril.gameObject.AddComponent<DestinoDeFicha>();
            destino.Ui = Ui; destino.Lista = lista; destino.Acento = Tema.cyan; destino.Radio = 16;
            destino.Motivo = (ficha, _) => null;
            destino.AlRecibir = (ficha, puesto) => {
                if (Nexus.Core.Tablero.OrdenDeLista.Poner(_backlog, ficha.Id, Mathf.Max(0, puesto))) Repintar();
            };
        }

        /// <summary>Un clic la mete al final del backlog, o la saca; arrastrarla la pone en su puesto.</summary>
        private void ConectarFicha(FichaArrastrable ficha, string id, RectTransform capa) {
            ficha.AlPulsar = () => {
                if (!_backlog.Remove(id)) _backlog.Add(id);
                Repintar();
            };
            ficha.AlFallar = motivo => PiezasDelMuro.Aviso(Ui, capa, motivo, Tema.warning);
        }

        // ==================================================================== heredado

        private void BacklogHeredado(FichaDelProyecto f) {
            var hoja = LienzoSinScroll("Backlog · heredado");
            CabeceraDelBacklog(hoja, "El product backlog ya existe", new[] { "backlog", "historia-de-usuario", "mvp" });
            hoja.Parrafo("Alguien ya empezó. Durante el desarrollo lo ordenas en tu muro.");

            var partes = Ui.Fila(hoja.Raiz, "Partes", Tema.Espacio(3), 0, TextAnchor.UpperCenter);
            UiKit.Tamano(partes, flexAncho: 1, flexAlto: 1).preferredHeight = Tema.Px(240);
            partes.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            partes.gameObject.AddComponent<ListaDeElementos>();
            for (var i = 0; i < f.Modulos.Count; i++) {
                var m = f.Modulos[i];
                var color = ColorDeModulo(i);
                var tarjeta = Ui.Columna(partes, "Parte " + m.Nombre, Tema.Espacio(2), 0, TextAnchor.UpperCenter);
                UiKit.Tamano(tarjeta, ancho: 0, flexAncho: 1, flexAlto: 1);
                PiezasDeProyecto.Cristal(Ui, tarjeta, NexusTheme.Alfa(color, 0.7f));
                tarjeta.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(12, 12, 14, 12);
                var nombre = Ui.Texto(tarjeta, m.Nombre, EstiloTexto.Encabezado, color, TextAlignmentOptions.Center);
                nombre.fontStyle = FontStyles.Bold;
                TextoEnCaja.Ajustar(nombre, Tema.Px(20));
                UiKit.Tamano(nombre, flexAncho: 1, alto: Tema.Px(30));
                var lado = Tema.Px(96);
                var anillo = PiezasDeProyecto.Anillo(Ui, tarjeta, lado, (float)(m.AvanceInicial / 100.0), color,
                                                     m.AvanceInicial > 0 ? m.AvanceInicial.ToString("0") + "%" : "0%");
                var queHace = Ui.Texto(tarjeta, PiezasDeProyecto.PrimeraFrase(m.QueHace, 70), EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Top);
                TextoEnCaja.Ajustar(queHace, Tema.Px(14));
                UiKit.Tamano(queHace, flexAncho: 1, alto: Tema.Px(60));
                var estado = m.AvanceInicial > 0 ? "hecho" : "sin empezar";
                var leyenda = Ui.Texto(tarjeta, estado, EstiloTexto.Leyenda, color, TextAlignmentOptions.Center);
                UiKit.Tamano(leyenda, alto: Tema.Px(16));
            }
        }
    }
}
