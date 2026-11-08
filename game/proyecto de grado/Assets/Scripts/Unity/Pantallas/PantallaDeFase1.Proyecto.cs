using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Proyecto;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «El proyecto» de la Fase 1 rehecho (2026-11-08): tres partes, ninguna con scroll y casi sin texto.
    ///   0 · El cliente   su retrato, una escena animada de quien es / que le duele / como trabaja, las personas que lo
    ///                    usan (con su cara) y los datos del encargo en cifras (dias, equipo, alcance, novedades).
    ///   1 · El sistema   el MAPA VIVO (personas, partes y sistemas de fuera, con flechas por las que viaja la
    ///                    informacion) y, debajo, como se mide el proyecto: ocho medidores y lo que exige el lanzamiento.
    ///   2 · Comprueba    las tres preguntas como un juego: la ficha se arrastra hasta su respuesta.
    /// Lo que antes eran parrafos ahora es tooltip: se lee si se quiere saber mas.
    /// </summary>
    public sealed partial class PantallaDeFase1 {
        private const int PartesDeProyecto = 3;
        private static readonly string[] NombresDeParteDelProyecto = { "El cliente", "El sistema", "Comprueba" };

        private void EncargoEnPartes(FichaDelProyecto f) {
            _subPaso = Mathf.Clamp(_subPaso, 0, PartesDeProyecto - 1);
            if (_subPaso == 0) ProyectoCliente(f);
            else if (_subPaso == 1) ProyectoSistema(f);
            else ProyectoComprueba(f);
        }

        /// <summary>La cabecera comun: «El proyecto · 1 de 3», su titulo y las tres partes como chips para saltar.</summary>
        private void CabeceraDelProyecto(Hoja hoja, string titulo) {
            var fila = hoja.Fila();
            var titulos = Ui.Columna(fila, "Titulos", Tema.Espacio(1));
            UiKit.Tamano(titulos, flexAncho: 1);
            var etiqueta = Ui.Texto(titulos, "El proyecto · " + (_subPaso + 1) + " de " + PartesDeProyecto + " · " + NombresDeParteDelProyecto[_subPaso],
                                    EstiloTexto.Leyenda, Tema.cyan);
            etiqueta.fontStyle |= FontStyles.UpperCase;
            Ui.Texto(titulos, titulo, EstiloTexto.Titulo);
            var partes = Ui.Fila(fila, "Partes", Tema.Espacio(2), 0, TextAnchor.MiddleRight);
            partes.gameObject.AddComponent<ListaDeElementos>();
            for (var i = 0; i < PartesDeProyecto; i++) {
                var parte = i;
                var hecha = i == PartesDeProyecto - 1 && ComprobacionHecha;
                var boton = Ui.Boton(partes, (hecha ? "√ " : (i + 1) + " · ") + NombresDeParteDelProyecto[i], () => IrAParte(parte), VarianteBoton.Fantasma);
                Ui.Resaltar(boton, i == _subPaso);
            }
        }

        // ==================================================================== 0 · el cliente

        private void ProyectoCliente(FichaDelProyecto f) {
            var hoja = LienzoSinScroll("Proyecto · cliente");
            Tooltip().OcultarYa();
            CabeceraDelProyecto(hoja, f.Nombre);

            var cuerpo = Ui.Fila(hoja.Raiz, "Cuerpo", Tema.Espacio(5), 0, TextAnchor.UpperLeft);
            var leCuerpo = UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
            leCuerpo.preferredHeight = Tema.Px(340); leCuerpo.minHeight = Tema.Px(240);
            cuerpo.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            // ---- el cliente: su retrato, su nombre y una frase
            var cli = Ui.Columna(cuerpo, "Cliente", Tema.Espacio(2), 0, TextAnchor.UpperCenter);
            UiKit.Tamano(cli, ancho: Tema.Px(240), flexAlto: 1);
            PiezasDeProyecto.Cristal(Ui, cli, NexusTheme.Alfa(Tema.cyan, 0.6f));
            cli.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(14, 14, 12, 12);
            GuiaView.Registrar("fase1.cliente", cli);
            var rotulo = Ui.Texto(cli, "EL CLIENTE", EstiloTexto.Leyenda, Tema.cyan, TextAlignmentOptions.Center);
            UiKit.Tamano(rotulo, alto: Tema.Px(16));
            Ui.Ilustracion(cli, MaterialesNexus.IdDePersonaje(f.Cliente.Nombre), null);
            var nombre = Ui.Texto(cli, f.Cliente.Nombre, EstiloTexto.Encabezado, Tema.ink, TextAlignmentOptions.Center);
            nombre.fontStyle = FontStyles.Bold;
            TextoEnCaja.Ajustar(nombre, Tema.Px(20));
            UiKit.Tamano(nombre, alto: Tema.Px(30));
            var frase = Ui.Texto(cli, f.EnUnaFrase ?? "", EstiloTexto.Pequeno, Tema.inkMuted, TextAlignmentOptions.Top);
            TextoEnCaja.Ajustar(frase, Tema.Px(14));
            UiKit.Tamano(frase, alto: Tema.Px(72));

            // ---- la escena: quien es, que le duele, como trabaja
            var escena = Ui.Nodo(cuerpo, "Escena del cliente");
            UiKit.Tamano(escena, flexAncho: 1, flexAlto: 1);
            var animacion = EscenaDeMetodologia.Crear(Ui, escena, EscenasDelCliente.Fases(f), "El cliente en escenas");
            UiKit.Rellenar((RectTransform)animacion.transform);

            // ---- quien lo va a usar: caras y, al pasar el raton, lo que necesita
            var usan = Ui.Columna(cuerpo, "Usuarios", Tema.Espacio(2), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(usan, ancho: Tema.Px(250), flexAlto: 1);
            PiezasDeProyecto.Cristal(Ui, usan, NexusTheme.Alfa(Tema.warning, 0.5f));
            usan.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(12, 12, 12, 12);
            usan.gameObject.AddComponent<ListaDeElementos>();
            var rotuloUsan = Ui.Texto(usan, "QUIÉN LO VA A USAR", EstiloTexto.Leyenda, Tema.warning, TextAlignmentOptions.Center);
            UiKit.Tamano(rotuloUsan, alto: Tema.Px(16));
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

            // ---- el encargo en cifras
            var datos = Ui.Fila(hoja.Raiz, "Datos", Tema.Espacio(3), 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(datos, flexAncho: 1);
            datos.gameObject.AddComponent<ListaDeElementos>();
            var ancho = Tema.Px(140); var alto = Tema.Px(64);
            var porFormar = S.EquipoDesdeRecoleccion && !S.RecoleccionHecha;
            PiezasDeProyecto.Dato(Ui, datos, Perfil.DiasTotales.ToString(), "DÍAS", Tema.warning, Perfil.DiasTotales + " días",
                                  "Lo que dura el nivel: de aquí al lanzamiento.", Tooltip, ancho, alto);
            PiezasDeProyecto.Dato(Ui, datos, porFormar ? "?" : Perfil.EquipoInicial.ToString(), "EQUIPO", Tema.cyan, "Tu equipo",
                                  porFormar ? "Todavía no tienes equipo: lo formas tú en el recorrido. Cada hoja de vida que encuentres es una persona que se une; si no encuentras ninguna, lo sacas tú solo."
                                            : "Las personas con las que cuentas.", Tooltip, ancho, alto);
            PiezasDeProyecto.Dato(Ui, datos, Perfil.AlcanceInicial.ToString("0"), "ALCANCE", Tema.success, "Alcance",
                                  "Puntos de trabajo prometidos al cliente. Si prometes más, hay más que entregar.", Tooltip, ancho, alto);
            PiezasDeProyecto.Dato(Ui, datos, PiezasDeProyecto.Titular(f.Sector.Nombre, 24), "SECTOR", Tema.violet, f.Sector.Nombre, f.Sector.ComoTrabaja, Tooltip, ancho * 2f, alto);
            var novedades = ExpedienteView.QueCambia(App, Perfil);
            if (novedades.Count > 0)
                PiezasDeProyecto.Dato(Ui, datos, novedades.Count.ToString(), "NOVEDADES", Tema.warning, "Qué tiene de distinto este proyecto",
                                      string.Join("\n", novedades.Select(x => "· " + x)), Tooltip, ancho, alto);
            var pistas = S.PistasEncontradas();
            if (pistas.Count > 0)
                PiezasDeProyecto.Dato(Ui, datos, pistas.Count.ToString(), "PISTAS", Tema.cyan, "Lo que averiguaste en el recorrido",
                                      string.Join("\n", pistas.Select(x => "· " + x)), Tooltip, ancho, alto);

            // ---- para consultar: el ticket original, las palabras del oficio y los documentos
            var recursos = Ui.Fila(hoja.Raiz, "Consulta", Tema.Espacio(2), 0, TextAnchor.MiddleCenter);
            recursos.gameObject.AddComponent<ListaDeElementos>();
            GuiaView.Registrar("fase1.vocabulario", recursos);
            if (Perfil.Briefing != null && Perfil.Briefing.Length > 0) {
                Ui.Boton(recursos, "Leer el ticket", () => DialogosDelExpediente.Ticket(App, Perfil), VarianteBoton.Fantasma);
                PantallaDeGlosario.Chip(App, recursos, "ticket");
            }
            if (f.Vocabulario.Count > 0) Ui.Boton(recursos, "Palabras del oficio", () => DialogosDelExpediente.Vocabulario(App, f), VarianteBoton.Fantasma);
            if (f.Artefactos.Count > 0) Ui.Boton(recursos, "Documentos", () => DialogosDelExpediente.Documentos(App, f), VarianteBoton.Fantasma);
        }

        // ==================================================================== 1 · el sistema y como se mide

        private void ProyectoSistema(FichaDelProyecto f) {
            var hoja = LienzoSinScroll("Proyecto · sistema");
            Tooltip().OcultarYa();
            CabeceraDelProyecto(hoja, "Cómo funciona");

            var mapa = Ui.Nodo(hoja.Raiz, "Mapa del sistema");
            var le = UiKit.Tamano(mapa, flexAncho: 1, flexAlto: 1);
            le.preferredHeight = Tema.Px(330); le.minHeight = Tema.Px(200);
            GuiaView.Registrar("fase1.expediente", mapa);
            MapaVivoDelProyecto.Crear(Ui, mapa, f, Tooltip);

            // ---- como se mide: ocho medidores y lo que exige el lanzamiento
            var panel = Ui.Columna(hoja.Raiz, "Como se mide", Tema.Espacio(2), 0, TextAnchor.UpperLeft);
            UiKit.Tamano(panel, flexAncho: 1);
            PiezasDeProyecto.Cristal(Ui, panel, NexusTheme.Alfa(Tema.cyan, 0.4f), 0.5f);
            panel.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(14, 14, 8, 10);
            panel.gameObject.AddComponent<ListaDeElementos>();
            GuiaView.Registrar("fase1.metricas", panel);
            var cabecera = Ui.Fila(panel, "Cabecera", Tema.Espacio(2), 0, TextAnchor.MiddleLeft);
            var rotulo = Ui.Texto(cabecera, "CÓMO SE MIDE TU PROYECTO", EstiloTexto.Leyenda, Tema.cyan);
            UiKit.Tamano(rotulo, flexAncho: 1);
            Ui.Texto(cabecera, "Cada decisión mueve estos medidores. Pasa el ratón por uno.", EstiloTexto.Leyenda, Tema.inkMuted);

            var medidores = new[] {
                new[] { "AV", "Avance", "Sube con el trabajo de cada día. Las horas extra lo suben más, pero cobran después." },
                new[] { "AL", "Alcance", "Lo prometido. Si aceptas pedir más, crece y hay más que entregar." },
                new[] { "DT", "Deuda técnica", "Los atajos. Sube con prisas y horas extra, y trae errores." },
                new[] { "CB", "Cobertura", "Cuánto está probado. Si es baja, el cliente encuentra errores." },
                new[] { "CA", "Cansancio", "Sube al quedarte; baja al irte a casa. Si es alto, todo sale peor." },
                new[] { "MO", "Moral", "Las ganas del equipo. Baja con promesas imposibles y noches largas." },
                new[] { "SC", "Satisfacción", "Sube al escuchar, explicar y cumplir. Baja con las sorpresas." },
                new[] { "RI", "Riesgo", "Junta cansancio, deuda y falta de pruebas: el peligro de entregar." }
            };
            var colores = new[] { Tema.cyan, Tema.cyan, Tema.warning, Tema.success, Tema.warning, Tema.success, Tema.success, Tema.danger };
            var fila = Ui.Fila(panel, "Medidores", Tema.Espacio(2), 0, TextAnchor.MiddleCenter);
            UiKit.Tamano(fila, flexAncho: 1);
            for (var i = 0; i < medidores.Length; i++)
                PiezasDeProyecto.Dato(Ui, fila, medidores[i][0], medidores[i][1].ToUpperInvariant(), colores[i], medidores[i][1], medidores[i][2],
                                      Tooltip, Tema.Px(92), Tema.Px(56));

            var u = Perfil.Lanzamiento;
            if (u != null) {
                var metas = Ui.Fila(panel, "Para salir bien", Tema.Espacio(2), 0, TextAnchor.MiddleCenter);
                UiKit.Tamano(metas, flexAncho: 1);
                var etiqueta = Ui.Texto(metas, "PARA SALIR BIEN", EstiloTexto.Leyenda, Tema.success);
                UiKit.Tamano(etiqueta, ancho: Tema.Px(120));
                PiezasDeProyecto.Dato(Ui, metas, (u.EntregaBien * 100).ToString("0") + " %", "ENTREGADO", Tema.success, "Entregado",
                                      "Al menos este porcentaje del alcance, terminado.", Tooltip, Tema.Px(110), Tema.Px(52));
                PiezasDeProyecto.Dato(Ui, metas, "máx. " + u.DefectosBien, "ERRORES", Tema.success, "Errores",
                                      "Como mucho tantos errores se pueden escapar.", Tooltip, Tema.Px(110), Tema.Px(52));
                PiezasDeProyecto.Dato(Ui, metas, "bajo " + u.RiesgoBien.ToString("0"), "RIESGO", Tema.success, "Riesgo",
                                      "El riesgo latente tiene que quedar por debajo.", Tooltip, Tema.Px(110), Tema.Px(52));
                PiezasDeProyecto.Dato(Ui, metas, u.SatisfaccionBien.ToString("0") + "+", "CLIENTE", Tema.success, "Satisfacción del cliente",
                                      "El cliente tiene que acabar así de contento o más.", Tooltip, Tema.Px(110), Tema.Px(52));
                PiezasDeProyecto.Dato(Ui, metas, "OK", "DECISIONES", Tema.success, "Decisiones justificadas",
                                      "Lo elegido cuenta tanto como el motivo con el que lo elegiste.", Tooltip, Tema.Px(110), Tema.Px(52));
            }
        }

        // ==================================================================== 2 · comprueba, como juego

        private void ProyectoComprueba(FichaDelProyecto f) {
            var hoja = LienzoSinScroll("Proyecto · comprueba");
            Tooltip().OcultarYa();
            CabeceraDelProyecto(hoja, "¿Lo entendiste?");

            if (Preguntas.Count == 0) {
                Ui.Texto(hoja.Raiz, "Este proyecto no trae preguntas: puedes seguir.", EstiloTexto.Cuerpo, Tema.inkMuted);
                return;
            }

            // ---- el progreso: un punto por pregunta
            var hechas = Preguntas.Count(p => _comprobacion.ContainsKey(p.Id));
            var progreso = Ui.Fila(hoja.Raiz, "Progreso", Tema.Espacio(2), 0, TextAnchor.MiddleLeft);
            for (var i = 0; i < Preguntas.Count; i++) {
                var punto = Ui.Nodo(progreso, "Punto " + (i + 1));
                UiKit.Tamano(punto, Tema.Px(20), Tema.Px(20));
                var img = punto.gameObject.AddComponent<Image>();
                img.sprite = Ui.SpriteCircular(); img.raycastTarget = false;
                img.color = i < hechas ? Tema.success : i == hechas ? Tema.cyan : NexusTheme.Alfa(Tema.inkMuted, 0.4f);
                if (i == hechas && !ComprobacionHecha) {
                    var b = Ui.Borde(punto, Tema.cyan, Tema.Px(10) / Tema.escala, 2);
                    var latido = punto.gameObject.AddComponent<LatidoDeBorde>();
                    latido.Borde = b; latido.Color = Tema.cyan;
                }
            }
            Ui.Texto(progreso, ComprobacionHecha ? "¡Hecho!" : (hechas + 1) + " de " + Preguntas.Count, EstiloTexto.Subtitulo,
                     ComprobacionHecha ? Tema.success : Tema.cyan);

            var area = Ui.Nodo(hoja.Raiz, "Area del juego");
            var le = UiKit.Tamano(area, flexAncho: 1, flexAlto: 1);
            le.preferredHeight = Tema.Px(340); le.minHeight = Tema.Px(240);
            GuiaView.Registrar("fase1.comprobacion", area);

            if (ComprobacionHecha) { RepasoDeLaComprobacion(area); return; }

            var indice = Preguntas.FindIndex(p => !_comprobacion.ContainsKey(p.Id));
            var actual = Preguntas[indice];
            JuegoDeComprobacion.Crear(Ui, area, actual,
                opcion => ContestarJuego(actual, opcion),
                opcion => _opcionesFalladas.Contains(actual.Id + ":" + opcion),
                () => { _mantenerScroll = true; Repintar(); });
        }

        /// <summary>
        /// Guarda la respuesta y dice si vale. Acertar se guarda ya (y habilita «Siguiente» si era la ultima); fallar apaga
        /// esa casilla. No repinta: el juego anima el acierto y el fallo, y repinta cuando termina.
        /// </summary>
        private bool ContestarJuego(PreguntaDelProyecto p, int opcion) {
            var acierta = opcion == p.Correcta;
            if (acierta) { _comprobacion[p.Id] = opcion; _ultimaFallada = null; _ultimaAcertada = p.Id; }
            else { _falladas.Add(p.Id); _ultimaFallada = p.Id; _opcionesFalladas.Add(p.Id + ":" + opcion); }
            GuardarBorrador();
            PintarNavegacion();
            return acierta;
        }

        /// <summary>Las tres respuestas ya acertadas, como tres tarjetas con su sello.</summary>
        private void RepasoDeLaComprobacion(RectTransform area) {
            var fila = Ui.Fila(area, "Repaso", Tema.Espacio(4), 0, TextAnchor.MiddleCenter);
            UiKit.Rellenar(fila);
            fila.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            fila.gameObject.AddComponent<ListaDeElementos>();
            for (var i = 0; i < Preguntas.Count; i++) {
                var p = Preguntas[i];
                var tarjeta = Ui.Nodo(fila, "Repaso " + (i + 1));
                UiKit.Tamano(tarjeta, flexAncho: 1, flexAlto: 1);
                PiezasDeProyecto.Cristal(Ui, tarjeta, Tema.success);
                var tit = TextoEnCaja.Crear(Ui, tarjeta, p.Texto, EstiloTexto.Encabezado, Tema.ink, TextAlignmentOptions.Center, Tema.Px(18),
                                            new Vector2(0, 0.52f), Vector2.one, Tema.Px(14));
                tit.fontStyle = FontStyles.Bold;
                TextoEnCaja.Crear(Ui, tarjeta, "√ " + p.Opciones[p.Correcta], EstiloTexto.Cuerpo, Tema.success, TextAlignmentOptions.Center, Tema.Px(16),
                                  new Vector2(0, 0.08f), new Vector2(1, 0.50f), Tema.Px(14));
                Aparecer.En(tarjeta, 0.1f * i, false);
            }
        }
    }
}
