using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Jornada;
using Nexus.Unity.Guia;
using Nexus.Unity.Juego;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Los paneles del dia: lo que antes estaba siempre en pantalla y ahora se abre desde el riel.
    ///   Mapa        donde estas, a donde puedes ir y lo que hay aqui (gente con quien hablar, cosas que encontrar)
    ///   Equipo      con quien te llevas bien y que ayudas tienes guardadas
    ///   Escritorio  el trabajo de practica
    ///   Avisos      lo que suena ahora, lo que hoy trae para leer, y la bitacora de lo que ha pasado
    ///   Monitoreo   el proyecto en detalle: medidores con su efecto, pronostico, partes del sistema y evolucion
    ///
    /// ★ VER es gratis: mientras hay un panel abierto el reloj espera (Runner.Mirando). ACTUAR desde el no: ir a una
    /// zona, hablar o empezar una tarea cobran sus minutos como siempre, y en ese rato puede sonar un aviso (el
    /// panel lo dice arriba). Lo que abre una escena encima del dia (atender, una tarea) cierra antes el panel.
    /// </summary>
    public sealed partial class PantallaDelDia {
        private PanelDelDia _panel;
        private int _pestanaDeAvisos, _pestanaDeMonitoreo;

        private void AbrirPanel(string etiqueta, string titulo, float ancho, Action<Hoja> pintar, Func<string> clave = null) {
            if (_panel != null) return;
            Runner.Mirando = true;
            try {
                _panel = App.Router.Apilar<PanelDelDia>(p => {
                    p.Etiqueta = etiqueta;
                    p.Titulo = titulo;
                    p.AnchoDelPanel = ancho;
                    p.Pintar = pintar;
                    p.Clave = clave;
                    p.AlCerrar = AlCerrarElPanel;
                });
            } catch {
                // Un panel que no se pudo pintar no puede dejar el reloj parado para siempre.
                Runner.Mirando = false;
                throw;
            }
        }

        private void AlCerrarElPanel() {
            _panel = null;
            if (App != null && Runner != null) Runner.Mirando = false;
        }

        /// <summary>Cierra el panel abierto, si es lo que esta encima. No hace nada si no hay ninguno.</summary>
        private void CerrarPanel() {
            if (_panel != null && App.Router.Actual == _panel) App.Router.Volver();
        }

        /// <summary>Arriba de un panel desde el que se actua: si ha sonado un aviso mientras tanto, que se sepa.</summary>
        private void AvisoDeAlertaEnPanel(Hoja h) {
            var sonando = AlertasSonando().Count;
            if (sonando == 0) return;
            var t = h.Tarjeta(sonando == 1 ? "Ha sonado un aviso" : $"Han sonado {sonando} avisos", Tono.Aviso);
            Ui.Texto(t, "Te espera en tu escritorio, y su tiempo corre en cuanto cierres este panel.", EstiloTexto.Pequeno, Tema.ink);
            Ui.Boton(Ui.Fila(t), "Volver a la jornada", CerrarPanel, VarianteBoton.Primario);
        }

        // ==================================================================== el mapa

        private void AbrirMapa() {
            AbrirPanel("Dónde estás y a dónde puedes ir", "El mapa", 1320, PintarPanelDelMapa,
                () => $"{S.ZonaActual}|{Runner.Estado}|{Runner.PuedeMoverse}|{S.ColeccionablesAqui().Count}|" +
                      $"{S.R.ConversacionesHechas.Count}|{AlertasSonando().Count}");
        }

        private void PintarPanelDelMapa(Hoja h) {
            var mapa = S.Perfil.Mapa;
            if (mapa == null || mapa.Vacio) {
                h.Nota("Este nivel no tiene mapa.");
                return;
            }
            AvisoDeAlertaEnPanel(h);
            h.Nota("Mientras miras el mapa, el reloj espera. Ir a otro lugar sí cuesta: los minutos que dice cada uno.");
            var puedeMoverse = Runner.PuedeMoverse;

            var columnas = Ui.Fila(h.Raiz, "Mapa", Tema.margen * 0.75f, alineacion: TextAnchor.UpperLeft);
            var zonas = Ui.Tarjeta(columnas, "A dónde puedes ir");
            UiKit.Tamano(zonas, ancho: 400, flexAncho: 1);
            GuiaView.Registrar("dia.mapa", zonas);
            foreach (var zona in mapa.Zonas) {
                var z = zona;
                var aqui = S.ZonaActual == z.Id;
                var abierta = S.ZonaAbierta(z.Id);
                var detalle = aqui ? "Estás aquí"
                            : !abierta ? "Cerrada por ahora"
                            : $"{mapa.CosteDeVisitar(S.ZonaActual, z.Id)} min";
                if (z.EsAncla) detalle += " · tu escritorio";
                var boton = Ui.BotonDeOpcion(zonas, z.Nombre, detalle, () => IrA(z.Id));
                GuiaView.Registrar("mapa." + z.Id, boton);
                boton.interactable = !aqui && abierta && puedeMoverse;
                if (aqui) Ui.Resaltar(boton, true);
            }
            if (!puedeMoverse && Runner.Estado != EstadoDelDia.DesarrolloTerminado)
                Ui.Texto(zonas, Runner.Pausado ? "El juego está en pausa: pulsa «Seguir», arriba, para poder moverte."
                              : Runner.EnEscena ? "Estás ocupado aquí: termina lo que tienes abierto antes de irte."
                              : "Solo te puedes mover mientras corre la jornada.", EstiloTexto.Pequeno, Tema.warning);

            var actual = mapa.PorId(S.ZonaActual);
            if (actual == null) return;
            var lugar = Ui.Tarjeta(columnas, "Aquí · " + actual.Nombre);
            UiKit.Tamano(lugar, ancho: 400, flexAncho: 1);
            GuiaView.Registrar("dia.aqui", lugar);
            if (!string.IsNullOrEmpty(actual.Descripcion)) Ui.Texto(lugar, actual.Descripcion, EstiloTexto.Pequeno, Tema.ink);
            if (actual.QuienEsta != null && actual.QuienEsta.Count > 0)
                Ui.Texto(lugar, "Aquí están: " + string.Join(", ", actual.QuienEsta), EstiloTexto.Pequeno);
            if (!string.IsNullOrEmpty(actual.QueDa)) Ui.Texto(lugar, actual.QueDa, EstiloTexto.Pequeno, Tema.cyan);

            foreach (var id in S.ColeccionablesAqui()) {
                var c = id;
                var col = App.Catalogo.Coleccionables.FirstOrDefault(x => x.Id == c);
                var que = col == null ? "algo" : PantallaDelDiario.NombreDeSerie(col.Serie, true);
                Ui.BotonDeOpcion(lugar, "Hay algo aquí: " + que, "Mirarlo", () => Recoger(c)).interactable = puedeMoverse;
            }

            foreach (var conv in S.ConversacionesAqui()) {
                var cv = conv;
                var p = App.Catalogo.Relaciones.PersonajePorId(cv.Personaje);
                var detalle = (cv.Pregunta == null ? "charla corta · " : "te quiere preguntar algo · ") +
                              $"{cv.Minutos} min · confianza {S.Confianza(cv.Personaje)}";
                var boton = Ui.BotonDeOpcion(lugar, "Hablar con " + (p?.Nombre ?? cv.Personaje), detalle, () => Hablar(cv));
                boton.interactable = puedeMoverse;
            }
        }

        // ==================================================================== el equipo

        private void AbrirEquipo() {
            AbrirPanel("Con quién te llevas bien y qué ayudas tienes", "Tu equipo", 900, PintarPanelDelEquipo,
                () => string.Join(",", S.R.Confianza.Select(kv => kv.Key + kv.Value)) + "|" +
                      string.Join(",", S.AyudasDisponibles().Select(kv => kv.Key + kv.Value)) + "|" + Runner.PuedeMoverse);
        }

        /// <summary>Las personas con las que ya hablaste, su confianza, y las ayudas que tienes guardadas.</summary>
        private void PintarPanelDelEquipo(Hoja h) {
            var lista = Ui.Columna(h.Raiz, "Personas", Tema.Espacio(1));
            GuiaView.Registrar("dia.equipo", lista);
            var rel = App.Catalogo.Relaciones;
            var conocidos = S.R.Confianza.Keys.ToList();
            if (conocidos.Count == 0)
                Ui.Texto(lista, "Todavía no has hablado con nadie. Cuando haya alguien en una sala, aparecerá «Hablar con…» en el mapa, en «Aquí».",
                         EstiloTexto.Pequeno, Tema.inkMuted);
            var enEsteNivel = new HashSet<string>(rel.Conversaciones.Where(c => c.Nivel == S.NivelId).Select(c => c.Personaje));
            foreach (var id in conocidos.OrderByDescending(x => enEsteNivel.Contains(x))) {
                var p = rel.PersonajePorId(id);
                var confianza = S.Confianza(id);
                var siguiente = p == null ? null : p.Ayudas.Where(a => a.Umbral > confianza).OrderBy(a => a.Umbral).FirstOrDefault();
                // RosterRow: el punto dice si ya diste todo con esa persona (confianza maxima) o sigue la relacion.
                Ui.FilaDeEquipo(lista, p?.Nombre ?? id, p?.Rol, siguiente == null ? EstadoDeEquipo.Hecho : EstadoDeEquipo.Activo);
                Ui.Texto(lista, siguiente == null ? $"confianza {confianza} · máxima" : $"confianza {confianza}/{siguiente.Umbral}", EstiloTexto.Leyenda, Tema.cyan);
                if (siguiente != null) Ui.Barra(lista, siguiente.Umbral > 0 ? (float)confianza / siguiente.Umbral : 1, Tema.cyan);
                // Quien sigue de un nivel a otro: lo que ganes con el no se pierde.
                var nota = p != null && p.Persistente
                    ? (enEsteNivel.Contains(id) ? "Sigue contigo en los próximos proyectos." : "No está en este proyecto, pero sigue contigo.")
                    : "Solo en este proyecto.";
                Ui.Texto(lista, nota, EstiloTexto.Leyenda, Tema.inkFaint);
            }
            var ayudas = S.AyudasDisponibles();
            if (ayudas.Count == 0) return;
            var guardadas = h.Tarjeta("Ayudas guardadas");
            foreach (var kv in ayudas) {
                var tipo = kv.Key;
                var fila = Ui.Columna(guardadas, espacio: 2);
                Ui.Texto(fila, $"{PantallaDeConversacion.TextoDeAyuda(tipo)}  ×{kv.Value}", EstiloTexto.Pequeno, Tema.ink);
                if (tipo == Nexus.Core.Relaciones.TiposDeAyuda.BajarCansancio) {
                    var usar = Ui.Boton(fila, "Usar ahora", () => {
                        var antes = S.W.Cansancio;
                        if (S.UsarAyuda(tipo)) Anotar($"{S.HoraActual} · El equipo descansa un rato: cansancio {Textos.Cambio(S.W.Cansancio - antes)}.");
                    }, VarianteBoton.Fantasma);
                    usar.interactable = Runner.PuedeMoverse;
                } else {
                    Ui.Texto(fila, "Se elige en la receta, antes de jugar el reto.", EstiloTexto.Pequeno, Tema.inkMuted);
                }
            }
        }

        // ==================================================================== el escritorio

        private void AbrirEscritorio() {
            AbrirPanel("Práctica · no cuenta para tu evaluación", "Trabajo en tu escritorio", 900, PintarPanelDelEscritorio,
                () => string.Join(",", S.TareasDeOficina.Select(t => S.PorQueNoSePuedeHacer(t.Id) ?? "ok")) + "|" +
                      Runner.PuedeMoverse + "|" + AlertasSonando().Count);
        }

        /// <summary>
        /// El trabajo de oficina: para que un rato sin avisos no sea solo esperar. Cada tarea es un minijuego de
        /// practica que cuesta tiempo del dia y mejora el stock de su tema. No cuenta para la evaluacion.
        /// </summary>
        private void PintarPanelDelEscritorio(Hoja h) {
            if (S.TareasDeOficina.Count == 0) {
                h.Nota("En este nivel no hay trabajo de escritorio.");
                return;
            }
            AvisoDeAlertaEnPanel(h);
            h.Parrafo("Mientras no suena nada, puedes adelantar trabajo. Cada tarea cuesta tiempo del día y mejora el proyecto. " +
                      "Es práctica: no cuenta para tu evaluación, y si no te sale puedes intentarla otra vez.");
            var lista = Ui.Columna(h.Raiz, "Tareas", Tema.Espacio(2));
            GuiaView.Registrar("dia.oficina", lista);
            foreach (var t in S.TareasDeOficina) {
                var tarea = t;
                var motivo = S.PorQueNoSePuedeHacer(tarea.Id);
                var detalle = $"{tarea.Minutos} min · mejora: {tarea.Mejora}" + (motivo == null ? "" : "   —   " + motivo);
                var boton = Ui.BotonDeOpcion(lista, tarea.Titulo, detalle, () => EmpezarTarea(tarea));
                boton.interactable = motivo == null && Runner.PuedeMoverse;
            }
        }

        // ==================================================================== avisos y bitacora

        private void AbrirAvisos(int pestana) {
            _pestanaDeAvisos = pestana;
            AbrirPanel("Lo que suena, lo que hoy trae y lo que ya pasó", "Avisos", 1000, PintarPanelDeAvisos,
                () => AlertasSonando().Count + "|" + _entradas.Count);
        }

        private void PintarPanelDeAvisos(Hoja h) {
            var pestanas = Ui.Pestanas(h.Raiz, new[] { "Hoy", "Lo que ha pasado" }, (i, hoja) => {
                if (i == 0) PintarAvisosDeHoy(new Hoja(Ui, hoja));
                else PintarBitacora(hoja);
            }, _pestanaDeAvisos);
            pestanas.AlCambiar = i => _pestanaDeAvisos = i;
        }

        private void PintarAvisosDeHoy(Hoja h) {
            var nada = true;

            var ancla = Ancla();
            var enElEscritorio = ancla == null || S.ZonaActual == ancla.Id;
            foreach (var alerta in AlertasSonando()) {
                nada = false;
                var a = alerta;
                var tarjeta = h.Tarjeta(a.Tipo == TiposDeAlerta.Minijuego ? "Esperando · ticket · " + a.Canal : "Esperando · " + a.Canal, Tono.Aviso);
                Ui.Texto(tarjeta, a.Texto, EstiloTexto.Cuerpo);
                Ui.Texto(tarjeta, $"Caduca a las {RelojDeJornada.Formatear(a.MinutoDeExpiracion)} · quedan {a.MinutosParaExpirar(S.MinutoDelDia)} min",
                         EstiloTexto.Pequeno, Tema.warning);
                var texto = enElEscritorio
                    ? "Atender"
                    : $"Volver al escritorio y atender ({S.Perfil.Mapa.CosteDeVisitar(S.ZonaActual, ancla.Id)} min)";
                Ui.Boton(Ui.Fila(tarjeta), texto, () => Atender(a), VarianteBoton.Primario);
            }

            var brief = S.BriefDeHoy;
            if (brief != null && brief.Incidencia != null) {
                nada = false;
                var inc = h.Tarjeta($"Hoy · consecuencia de tu {NombreDeEstadistica(brief.Incidencia.Estadistica)} ({brief.Incidencia.Valor:0})", Tono.Aviso);
                Ui.Texto(inc, brief.Incidencia.Titulo, EstiloTexto.Cuerpo).fontStyle = FontStyles.Bold;
                Ui.Texto(inc, brief.Incidencia.Texto, EstiloTexto.Pequeno, Tema.ink);
                var efectos = brief.Incidencia.Efectos.ToDictionary(kv => kv.Key, kv => Nexus.Core.Servicios.EffectApplier.ToDouble(kv.Value));
                Ui.Texto(inc, "Lo que costó: " + Textos.Previsualizar(efectos), EstiloTexto.Pequeno, Tema.warning);
            }
            if (brief != null && brief.Avisos.Count > 0) {
                nada = false;
                var anuncios = h.Tarjeta("Anuncios de hoy");
                Ui.Texto(anuncios, "Avisan de lo que llegará en los próximos días. Léelos: lo que hoy es un comentario suelto, mañana es un problema.",
                         EstiloTexto.Pequeno);
                foreach (var aviso in brief.Avisos) Ui.Notificacion(anuncios, aviso, Tono.Cyan);
            }
            if (nada) h.Nota(S.R.DiaActual == 0 ? "Aún no ha empezado el primer día." : "Hoy no hay nada que leer todavía. Cuando suene un aviso, aparecerá aquí y en «La jornada».");
        }

        private void PintarBitacora(RectTransform hoja) {
            if (_entradas.Count == 0) {
                Ui.Texto(hoja, "Todavía no ha pasado nada.", EstiloTexto.Pequeno);
                return;
            }
            Ui.Texto(hoja, "Lo más reciente, arriba.", EstiloTexto.Leyenda, Tema.inkFaint);
            foreach (var e in _entradas)
                Ui.Texto(hoja, e, EstiloTexto.Pequeno, e.StartsWith("—") ? Tema.cyan : Tema.ink);
        }

        // ==================================================================== monitoreo

        private void AbrirMonitoreo(int pestana) {
            _pestanaDeMonitoreo = pestana;
            _ultimoResultado = null;
            AbrirPanel((S.Perfil.Proyecto != null ? S.Perfil.Proyecto.Nombre : S.Perfil.Nombre) + " · " + S.Metodologia.Nombre, "Monitoreo del proyecto", 1320,
                       PintarPanelDeMonitoreo, () => S.MinutoDelDia + "|" + _ultimoResultado + "|" + S.R.PruebasHechasHoy + "|" + S.R.InspeccionHechaHoy);
        }

        private void PintarPanelDeMonitoreo(Hoja h) {
            h.Nota("Mirar es gratis: el reloj espera mientras este panel está abierto. Las acciones (probar, inspeccionar) cuestan minutos de la jornada.");
            DecirLoUltimo(h);
            // Lo comun a todo proyecto, y despues lo que es de ESTA metodologia: su diagrama, sus documentos, sus pruebas.
            var vistas = new List<KeyValuePair<string, Action<RectTransform>>> {
                new KeyValuePair<string, Action<RectTransform>>("El proyecto", PintarDetalleDelProyecto),
                new KeyValuePair<string, Action<RectTransform>>("Pronóstico", PintarDetalleDelPronostico),
                new KeyValuePair<string, Action<RectTransform>>("Las partes", PintarDetalleDeLasPartes)
            };
            var propias = VistasDeLaMetodologia();
            vistas.AddRange(propias);
            if (propias.Count == 0) vistas.Add(new KeyValuePair<string, Action<RectTransform>>("Evolución", PintarEvolucion));
            var pestanas = Ui.Pestanas(h.Raiz, vistas.Select(v => v.Key).ToList(), (i, hoja) => vistas[i].Value(hoja),
                                       Mathf.Clamp(_pestanaDeMonitoreo, 0, vistas.Count - 1));
            pestanas.AlCambiar = i => _pestanaDeMonitoreo = i;
        }

        /// <summary>Los cinco medidores con su «?» y, debajo de cada uno, lo que esta provocando ahora.</summary>
        private void PintarDetalleDelProyecto(RectTransform hoja) {
            var w = S.W;
            var valores = new[] {
                $"{w.Avance:0} / {w.Alcance:0} pts", $"{w.DeudaTecnica:0} / 100", $"{w.MoralEquipo:0} / 100", $"{w.Cobertura:0} %", $"{w.Cansancio:0} / 100"
            };
            var niveles = new[] {
                w.Alcance > 0 ? (float)(w.Avance / w.Alcance) : 0, (float)w.DeudaTecnica / 100f, (float)w.MoralEquipo / 100f,
                (float)w.Cobertura / 100f, (float)w.Cansancio / 100f
            };
            var colores = new[] { Tema.cyan, Tema.warning, Tema.cyan, Tema.cyan, Tema.warning };
            bool[] malos;
            var efectos = EfectosDeLosMedidores(out malos);
            for (var i = 0; i < MedidoresLargos.Length; i++) {
                var bloque = Ui.Columna(hoja, "Medidor", Tema.Espacio(1));
                var fila = Ui.Fila(bloque);
                Ui.Texto(fila, MedidoresLargos[i], EstiloTexto.Cuerpo, Tema.ink);
                PantallaDeGlosario.Chip(App, fila, MedidoresEnGlosario[i]);
                Ui.Resorte(fila);
                Ui.Texto(fila, valores[i], EstiloTexto.Cuerpo, Tema.ink, TextAlignmentOptions.Right);
                Ui.Barra(bloque, niveles[i], colores[i]);
                Ui.Texto(bloque, efectos[i], EstiloTexto.Pequeno, malos[i] ? Tema.danger : Tema.inkMuted);
            }
            Ui.Separador(hoja);
            var d = S.BriefDeHoy != null ? S.BriefDeHoy.Derivadas : null;
            var filaRiesgo = Ui.Fila(hoja);
            Ui.Texto(filaRiesgo, d == null ? "Riesgo latente: se calcula al empezar el día." : $"Riesgo latente al empezar el día: {d.RiesgoLatente:0} / 100",
                     EstiloTexto.Cuerpo, Tema.ink);
            PantallaDeGlosario.Chip(App, filaRiesgo, "riesgo");
            Ui.Texto(hoja, $"Satisfacción del cliente: {w.SatisfaccionCliente:0} / 100 · pesa 15 de 100 en el lanzamiento" +
                           (w.SatisfaccionCliente <= 35 ? $" · <color={NexusTheme.Html(Tema.danger)}>tan baja que pedirán explicaciones</color>" : ""),
                     EstiloTexto.Cuerpo, Tema.ink);
        }

        /// <summary>El pronostico factor a factor: que se mide en el lanzamiento, cuanto da hoy y por que importa.</summary>
        private void PintarDetalleDelPronostico(RectTransform hoja) {
            if (S.R.Fase != 2) {
                Ui.Texto(hoja, "El desarrollo ha terminado: ya no hay pronóstico, toca el lanzamiento.", EstiloTexto.Cuerpo);
                return;
            }
            var c = S.Pronostico();
            Ui.Texto(hoja, $"Si entregaras al ritmo de hoy: {Nexus.Core.Evaluacion.NivelesDeLanzamiento.Titulo(c.Nivel).ToLowerInvariant()} ({c.Puntaje:0} de 100)",
                     EstiloTexto.Subtitulo, ColorDelNivel(c.Nivel));
            Ui.Barra(hoja, (float)(c.Puntaje / 100.0), ColorDelNivel(c.Nivel));
            Ui.Texto(hoja, "Un solo factor en «mal» hunde el lanzamiento aunque los demás vayan bien.", EstiloTexto.Pequeno);
            foreach (var f in c.Factores) {
                var bloque = Ui.PanelColumna(hoja, "Factor", Tema.Espacio(3), Tema.Espacio(1), Tema.surfaceRaised);
                var fila = Ui.Fila(bloque);
                Ui.Texto(fila, f.Nombre, EstiloTexto.Cuerpo, Tema.ink).fontStyle = FontStyles.Bold;
                Ui.Badge(fila, f.Estado ?? "", f.Estado == "bien" ? Tono.Exito : f.Estado == "mal" ? Tono.Peligro : Tono.Aviso);
                Ui.Resorte(fila);
                Ui.Texto(fila, $"{f.Valor}   ·   {f.Puntos:0.#} de {f.Maximo:0} puntos", EstiloTexto.Pequeno, Tema.ink, TextAlignmentOptions.Right);
                if (!string.IsNullOrEmpty(f.Explicacion)) Ui.Texto(bloque, f.Explicacion, EstiloTexto.Pequeno);
            }
        }

        /// <summary>El avance repartido por las partes del sistema, con lo que hace cada una.</summary>
        private void PintarDetalleDeLasPartes(RectTransform hoja) {
            var ficha = S.Perfil.Proyecto;
            if (ficha == null || ficha.Modulos.Count == 0) {
                Ui.Texto(hoja, "Este proyecto no está descrito por partes.", EstiloTexto.Pequeno);
                return;
            }
            var porModulo = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(ficha, S.W.Avance, S.W.Alcance);
            foreach (var kv in porModulo) {
                var bloque = Ui.Columna(hoja, "Modulo", Tema.Espacio(1));
                var fila = Ui.Fila(bloque);
                Ui.Texto(fila, kv.Key.Nombre, EstiloTexto.Cuerpo, Tema.ink).fontStyle = FontStyles.Bold;
                Ui.Resorte(fila);
                Ui.Texto(fila, $"{kv.Value:0} %", EstiloTexto.Cuerpo, Tema.ink, TextAlignmentOptions.Right);
                Ui.Barra(bloque, (float)(kv.Value / 100.0), Tema.cyan);
                if (!string.IsNullOrEmpty(kv.Key.QueHace)) Ui.Texto(bloque, kv.Key.QueHace, EstiloTexto.Pequeno);
            }
        }

        /// <summary>
        /// Como ha ido el proyecto dia a dia: lo hecho contra lo que hay que hacer, y el riesgo acumulado. Las series
        /// las guarda el motor al empezar cada dia (RuntimeState.SerieAvance…), y hasta ahora no las dibujaba nadie.
        /// </summary>
        private void PintarEvolucion(RectTransform hoja) {
            var r = S.R;
            if (r.SerieAvance.Count < 2) {
                Ui.Texto(hoja, "Todavía no hay historia que dibujar: vuelve a partir del segundo día.", EstiloTexto.Cuerpo);
                return;
            }
            var dias = Math.Max(2, S.Perfil.DiasTotales);
            var techo = Math.Max(S.W.Alcance, r.SerieAlcance.Count > 0 ? r.SerieAlcance.Max() : 0) * 1.1;

            Ui.Texto(hoja, "LO HECHO CONTRA LO QUE HAY QUE HACER", EstiloTexto.Leyenda, Tema.cyan);
            var trabajo = new GraficoDeSeries(Ui, hoja, 1100, 320);
            trabajo.Ejes("día", "puntos", 1, dias, techo);
            trabajo.Linea(r.SerieAlcance, Tema.warning, true);
            trabajo.Linea(r.SerieAvance, Tema.cyan);
            trabajo.MarcaVertical(r.DiaActual, "hoy", Tema.inkMuted);
            GraficoDeSeries.Leyenda(Ui, hoja, new[] {
                new KeyValuePair<string, Color>("Avance (al empezar cada día)", Tema.cyan),
                new KeyValuePair<string, Color>("Alcance: lo que hay que entregar", Tema.warning)
            });

            if (r.SerieRiesgo.Count < 2) return;
            Ui.Texto(hoja, "EL RIESGO LATENTE", EstiloTexto.Leyenda, Tema.cyan);
            var riesgo = new GraficoDeSeries(Ui, hoja, 1100, 220);
            riesgo.Ejes("día", "riesgo", 1, dias, 100);
            riesgo.Linea(r.SerieRiesgo, Tema.danger);
            riesgo.MarcaVertical(r.DiaActual, "hoy", Tema.inkMuted);
            Ui.Texto(hoja, "Junta el cansancio, la deuda, lo que falta por probar y lo que falta por documentar. Con el riesgo alto, el " +
                           "lanzamiento sale mal aunque esté todo hecho.", EstiloTexto.Pequeno);
        }
    }
}
