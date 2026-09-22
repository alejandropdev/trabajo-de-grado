using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Guardado;
using Nexus.Core.Jornada;
using Nexus.Core.Minijuegos;
using Nexus.Core.Sesion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Juego;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El dia continuo (§3.3). Tres columnas:
    ///   izquierda — el reloj y el MAPA: donde estas, a donde puedes ir y cuanto cuesta, y lo que hay aqui
    ///   centro    — «Ahora»: lo unico que hay que mirar para saber que hacer (un aviso, una decision, el cierre…)
    ///   derecha   — el proyecto (sus stocks) y el diario del dia: lo que ya ha pasado, lo mas reciente arriba
    ///
    /// El reloj corre solo. Se para DENTRO de una escena (decision, minijuego, planificacion, retro, una carta
    /// que lees) y en la pausa; nunca mientras decides si ir a atender un aviso — si el mundo se congelara ahi,
    /// esa decision no costaria nada. Los avisos solo se atienden desde tu escritorio: si estas lejos, volver
    /// cuesta minutos, y por el camino el aviso puede caducar.
    /// </summary>
    public sealed class PantallaDelDia : Pantalla {
        private enum Paso {
            AntesDeEmpezar, Retro, Planificacion, Jornada, Aviso, Decision, Decidido,
            Cierre, Prorroga, Resumen, FinDelDesarrollo
        }

        private GameSession S { get { return App.Sesion; } }
        private LevelRunner Runner { get { return App.Runner; } }

        // interfaz
        private Hoja _ahora;
        private RectTransform _mapa, _aqui, _diario;
        private TMP_Text _titulo, _etiqueta, _riesgo;
        private Button _pausa;
        private readonly TMP_Text[] _valores = new TMP_Text[5];
        private readonly BarraView[] _barras = new BarraView[5];
        private readonly List<KeyValuePair<Alerta, TMP_Text>> _cuentasAtras = new List<KeyValuePair<Alerta, TMP_Text>>();
        private string _claveAhora, _claveMapa;

        // lo que la pantalla sabe y la sesion no
        private bool _escenaApilada;
        private EntradaTraza _decidido;
        private string _cambiosDeLaDecision;
        private double _compromiso;
        private int _diaDelCompromiso = -1;
        private double[] _alEmpezarElDia = new double[5];
        private int _decisionesDeHoy;
        private bool _tengoElInicioDelDia;   // false al recuperar una partida: no se sabe como empezo ese dia
        private readonly List<string> _entradas = new List<string>();

        public override bool PuedeVolver { get { return false; } }

        // ==================================================================== construir

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen * 0.75f, Tema.margen * 0.75f));
            ConstruirCabecera(marco);

            var cuerpo = Ui.Fila(marco, "Cuerpo", Tema.margen * 0.75f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(cuerpo, flexAncho: 1, flexAlto: 1);
            ConstruirIzquierda(cuerpo);

            var centro = Ui.PanelColumna(cuerpo, "Ahora", Tema.margen, Tema.espacio);
            UiKit.Tamano(centro, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(centro, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            _ahora = new Hoja(Ui, contenido);

            ConstruirDerecha(cuerpo);

            Runner.Usar(S);
            if (S.R.DiaActual > 0) Runner.ReanudarDia();
            Runner.EnEscena = false;
            Runner.Pausado = false;
            Runner.AlSonarAlerta += AlSonar;
            Runner.AlExpirarAlerta += AlExpirar;
            Runner.AlLlegarElCierre += AlCierre;
            Runner.AlTerminarElDia += AlFinDelDia;

            Anotar(S.R.DiaActual == 0
                ? $"Empieza «{S.Perfil.Nombre}» con {S.Metodologia.Nombre}."
                : $"Partida recuperada: día {S.R.DiaActual}, {S.HoraActual}.");
        }

        private void ConstruirCabecera(Transform padre) {
            var cabecera = Ui.Fila(padre, "Cabecera");
            var titulos = Ui.Columna(cabecera, espacio: 0);
            UiKit.Tamano(titulos, flexAncho: 1);
            _etiqueta = Ui.Texto(titulos, "", EstiloTexto.Pequeno, Tema.cian);
            _titulo = Ui.Texto(titulos, "", EstiloTexto.Subtitulo, Tema.texto);

            _pausa = Ui.Boton(cabecera, "Pausa", () => Runner.Pausado = !Runner.Pausado);
            UiKit.Tamano(_pausa, ancho: 130);
            foreach (var v in new[] { 1, 2, 4 }) {
                var velocidad = v;
                UiKit.Tamano(Ui.Boton(cabecera, "×" + v, () => Runner.Velocidad = velocidad, VarianteBoton.Fantasma), ancho: 64);
            }
            Ui.Boton(cabecera, "Diario", AbrirDiario);
            Ui.Boton(cabecera, "Menú", () => App.Router.Apilar<PantallaDePausa>());
        }

        private void ConstruirIzquierda(Transform padre) {
            var izquierda = Ui.Columna(padre, "Izquierda", Tema.espacio);
            UiKit.Tamano(izquierda, ancho: 400, flexAlto: 1);
            RelojView.Crear(Ui, izquierda, Runner);

            RectTransform contenido;
            var scroll = Ui.Desplazable(izquierda, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var mapa = Ui.Tarjeta(contenido, "El mapa");
            _mapa = Ui.Columna(mapa, "Zonas", 6);
            var aqui = Ui.Tarjeta(contenido, "Aquí");
            _aqui = Ui.Columna(aqui, "Zona actual", 6);
        }

        private void ConstruirDerecha(Transform padre) {
            var derecha = Ui.Columna(padre, "Derecha", Tema.espacio);
            UiKit.Tamano(derecha, ancho: 420, flexAlto: 1);

            var proyecto = Ui.Tarjeta(derecha, "El proyecto");
            var nombres = new[] { "Avance", "Deuda técnica", "Moral del equipo", "Cobertura de pruebas", "Cansancio" };
            var colores = new[] { Tema.cian, Tema.mostaza, Tema.cianClaro, Tema.cian, Tema.naranja };
            for (var i = 0; i < nombres.Length; i++) {
                var fila = Ui.Fila(proyecto);
                Ui.Texto(fila, nombres[i], EstiloTexto.Pequeno, Tema.texto);
                Ui.Resorte(fila);
                _valores[i] = Ui.Texto(fila, "", EstiloTexto.Pequeno, Tema.texto, TextAlignmentOptions.Right);
                _barras[i] = Ui.Barra(proyecto, 0, colores[i]);
            }
            _riesgo = Ui.Texto(proyecto, "", EstiloTexto.Pequeno);

            var diario = Ui.PanelColumna(derecha, "Diario del dia", Tema.margen * 0.75f, Tema.espacio);
            UiKit.Tamano(diario, flexAncho: 1, flexAlto: 1);
            Ui.Texto(diario, "LO QUE HA PASADO", EstiloTexto.Pequeno, Tema.cian);
            var scroll = Ui.Desplazable(diario, out _diario);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            PintarDiario();
        }

        private void OnDestroy() {
            if (App == null || Runner == null) return;
            Runner.AlSonarAlerta -= AlSonar;
            Runner.AlExpirarAlerta -= AlExpirar;
            Runner.AlLlegarElCierre -= AlCierre;
            Runner.AlTerminarElDia -= AlFinDelDia;
        }

        // ==================================================================== que pasa ahora

        private Paso PasoActual() {
            if (_decidido != null) return Paso.Decidido;
            if (S.Decision != null) return Paso.Decision;
            if (S.PendingRetro != null && S.PendingRetro.Acciones.Count > 0) return Paso.Retro;
            if (S.PendingPlanning != null) return Paso.Planificacion;
            if (Runner.Estado == EstadoDelDia.Corriendo && AlertasSonando().Count > 0) return Paso.Aviso;

            switch (Runner.Estado) {
                case EstadoDelDia.Parado: return Paso.AntesDeEmpezar;
                case EstadoDelDia.EnElCierre: return Paso.Cierre;
                case EstadoDelDia.Prorroga: return Paso.Prorroga;
                case EstadoDelDia.DiaTerminado: return Paso.Resumen;
                case EstadoDelDia.DesarrolloTerminado: return Paso.FinDelDesarrollo;
                default: return Paso.Jornada;
            }
        }

        private void Update() {
            if (S == null || S.NivelTerminado) return;

            // ★ El reloj se para dentro de una escena, y solo ahi.
            Runner.EnEscena = _escenaApilada || _decidido != null || S.Decision != null || S.PendingPlanning != null ||
                              (S.PendingRetro != null && S.PendingRetro.Acciones.Count > 0);

            var paso = PasoActual();
            var clave = $"{paso}|{S.R.DiaActual}|{S.ZonaActual}|{string.Join(",", AlertasSonando().Select(a => a.Id))}|" +
                        $"{S.SePuedeCerrarLaJornada}|{QuedaAlgunAviso()}|{_compromiso}";
            if (clave != _claveAhora) {
                _claveAhora = clave;
                PintarAhora(paso);
            }

            var claveMapa = $"{S.R.DiaActual}|{S.ZonaActual}|{Runner.Estado}|{S.ColeccionablesAqui().Count}";
            if (claveMapa != _claveMapa) {
                _claveMapa = claveMapa;
                PintarMapa();
            }

            foreach (var kv in _cuentasAtras)
                kv.Value.text = kv.Key.EstaPendiente
                    ? $"Caduca a las {RelojDeJornada.Formatear(kv.Key.MinutoDeExpiracion)} · quedan {kv.Key.MinutosParaExpirar(S.MinutoDelDia)} min"
                    : "";

            PintarCabecera();
            PintarProyecto();
        }

        private void PintarCabecera() {
            var brief = S.BriefDeHoy;
            _etiqueta.text = S.Perfil.Nombre.ToUpperInvariant() + " · " + S.Metodologia.Nombre.ToUpperInvariant();
            _titulo.text = S.R.DiaActual == 0
                ? "Antes del día 1"
                : $"Día {S.R.DiaActual} de {S.Perfil.DiasTotales}" + (brief != null ? " · " + brief.EtiquetaUnidad : "");
            _pausa.GetComponentInChildren<TMP_Text>().text = Runner.Pausado ? "Seguir" : "Pausa";
        }

        private void PintarProyecto() {
            var w = S.W;
            _valores[0].text = $"{w.Avance:0} / {w.Alcance:0} pts";
            _barras[0].Valor = w.Alcance > 0 ? (float)(w.Avance / w.Alcance) : 0;
            _valores[1].text = $"{w.DeudaTecnica:0} / 100";
            _barras[1].Valor = (float)w.DeudaTecnica / 100f;
            _valores[2].text = $"{w.MoralEquipo:0} / 100";
            _barras[2].Valor = (float)w.MoralEquipo / 100f;
            _valores[3].text = $"{w.Cobertura:0} %";
            _barras[3].Valor = (float)w.Cobertura / 100f;
            _valores[4].text = $"{w.Cansancio:0} / 100";
            _barras[4].Valor = (float)w.Cansancio / 100f;
            var d = S.BriefDeHoy != null ? S.BriefDeHoy.Derivadas : null;
            _riesgo.text = d == null ? "" : $"Riesgo latente al empezar el día: {d.RiesgoLatente:0} / 100";
        }

        private void PintarAhora(Paso paso) {
            _cuentasAtras.Clear();
            _ahora.Vaciar();
            switch (paso) {
                case Paso.AntesDeEmpezar: AntesDeEmpezar(); break;
                case Paso.Retro: Retro(); break;
                case Paso.Planificacion: Planificacion(); break;
                case Paso.Jornada: Jornada(); break;
                case Paso.Aviso: Aviso(); break;
                case Paso.Decision: Decision(); break;
                case Paso.Decidido: Decidido(); break;
                case Paso.Cierre: Cierre(); break;
                case Paso.Prorroga: Prorroga(); break;
                case Paso.Resumen: Resumen(); break;
                case Paso.FinDelDesarrollo: FinDelDesarrollo(); break;
            }
        }

        // ---------------------------------------------------------------- cada paso

        private void AntesDeEmpezar() {
            var j = S.Perfil.Jornada;
            _ahora.Etiqueta("Fase 2 · el desarrollo");
            _ahora.Titulo($"{S.Perfil.DiasTotales} días por delante");
            _ahora.Parrafo($"Cada día empieza a las {j.HoraInicio:00}:00 y la jornada acaba a las {j.HoraCierre:00}:00. El reloj corre solo: " +
                           "arriba puedes pausarlo o acelerarlo.");
            _ahora.Parrafo("En cualquier momento puede llegar un aviso. Tienes unas horas para atenderlo, y solo se atiende desde tu " +
                           "escritorio. Si lo dejas caducar, alguien decide por ti, y casi nunca bien.");
            _ahora.Parrafo("Mientras tanto, el mapa de la izquierda es tuyo: cada zona tiene algo que no está en ninguna otra. Pero ir " +
                           "cuesta tiempo, y estar lejos cuando suena un aviso también.");
            _ahora.Accion("Empezar el día 1", EmpezarDia);
        }

        private void Retro() {
            var retro = S.PendingRetro;
            _ahora.Etiqueta("Ceremonia · retrospectiva");
            _ahora.Titulo("¿Qué cambiamos del proceso?");
            if (!string.IsNullOrEmpty(retro.Texto)) _ahora.Parrafo(retro.Texto);
            _ahora.Nota("Lo que elijas cambia cómo trabaja el equipo desde hoy. El reloj espera.");
            foreach (var accion in retro.Acciones) {
                var a = accion;
                _ahora.Opcion(a.Texto, null, () => {
                    S.ElegirAccionRetro(a.Id);
                    Anotar($"Retrospectiva: «{a.Texto}»");
                });
            }
        }

        private void Planificacion() {
            var plan = S.PendingPlanning;
            if (_diaDelCompromiso != S.R.DiaActual) {
                _diaDelCompromiso = S.R.DiaActual;
                _compromiso = Math.Round(plan.CapacidadSugerida);
            }
            _ahora.Etiqueta("Ceremonia · planificación de " + plan.Unidad);
            _ahora.Titulo(string.IsNullOrEmpty(plan.Texto) ? "¿Con cuánto te comprometes?" : plan.Texto);
            _ahora.Parrafo($"El equipo calcula que puede con unos {plan.CapacidadSugerida:0.#} puntos. Quedan {plan.PuntosPendientes:0} puntos por hacer.");
            _ahora.Nota("Prometer más de lo que cabe no da error: genera sobrecompromiso, y el sobrecompromiso genera deuda técnica cada día hasta que se cierra.");

            var fila = _ahora.Fila();
            UiKit.Tamano(Ui.Boton(fila, "−", () => _compromiso = Math.Max(0, _compromiso - 1)), ancho: 64);
            Ui.Texto(fila, $"{_compromiso:0} puntos", EstiloTexto.Subtitulo,
                     _compromiso > plan.CapacidadSugerida + 0.5 ? Tema.amarillo : Tema.cian, TextAlignmentOptions.Center);
            UiKit.Tamano(Ui.Boton(fila, "+", () => _compromiso += 1), ancho: 64);

            _ahora.Accion($"Comprometerse a {_compromiso:0} puntos", () => {
                S.Comprometer(_compromiso);
                Anotar($"Planificación: el equipo se compromete a {_compromiso:0} puntos.");
            });
        }

        private void Jornada() {
            var porSonar = QuedaAlgunAviso();
            _ahora.Etiqueta("La jornada corre");
            _ahora.Titulo(S.HoraActual.StartsWith("0") ? "Buenos días" : "Trabajando");
            _ahora.Parrafo(porSonar
                ? "Hoy va a llegar algún aviso, y no sabes cuándo. Mientras tanto puedes recorrer el mapa, o esperar en tu escritorio."
                : "Hoy ya no queda ningún aviso por llegar. Puedes explorar, o dar la jornada por terminada.");

            var brief = S.BriefDeHoy;
            if (brief != null && brief.Avisos.Count > 0) {
                var anuncios = _ahora.Tarjeta("Anuncios de hoy");
                Ui.Texto(anuncios, "Avisan de lo que llegará en los próximos días. Léelos: lo que hoy es un comentario suelto, mañana es un problema.",
                         EstiloTexto.Pequeno);
                foreach (var aviso in brief.Avisos) Ui.Texto(anuncios, aviso, EstiloTexto.Cuerpo);
            }

            _ahora.Espacio();
            var botones = _ahora.Fila();
            if (porSonar) Ui.Boton(botones, "Esperar al siguiente aviso", () => Runner.AdelantarHastaElSiguienteAviso());
            var cerrar = Ui.Boton(botones, "Cerrar la jornada", () => Runner.CerrarJornada(),
                                  porSonar ? VarianteBoton.Secundario : VarianteBoton.Primario);
            cerrar.interactable = S.SePuedeCerrarLaJornada;
            if (!S.SePuedeCerrarLaJornada)
                _ahora.Nota("«Cerrar la jornada» salta al final del día, y solo se puede cuando no queda nada pendiente: saltar nunca te ahorra una consecuencia.");
        }

        private void Aviso() {
            _ahora.Etiqueta("Aviso", Tema.mostaza);
            var alertas = AlertasSonando();
            _ahora.Titulo(alertas.Count == 1 ? "Ha llegado un aviso" : $"Tienes {alertas.Count} avisos");
            _ahora.Nota("El reloj NO se para mientras decides si ir: esperar también cuesta.");

            var ancla = Ancla();
            var enElEscritorio = ancla == null || S.ZonaActual == ancla.Id;
            foreach (var alerta in alertas) {
                var a = alerta;
                var tarjeta = _ahora.Tarjeta(a.Tipo == TiposDeAlerta.Minijuego ? "Ticket · " + a.Canal : a.Canal, Tema.mostaza);
                Ui.Texto(tarjeta, a.Texto, EstiloTexto.Cuerpo);
                _cuentasAtras.Add(new KeyValuePair<Alerta, TMP_Text>(a, Ui.Texto(tarjeta, "", EstiloTexto.Pequeno, Tema.amarillo)));
                var texto = enElEscritorio
                    ? "Atender"
                    : $"Volver al escritorio y atender ({S.Perfil.Mapa.CosteDeVisitar(S.ZonaActual, ancla.Id)} min)";
                Ui.Boton(Ui.Fila(tarjeta), texto, () => Atender(a), VarianteBoton.Primario);
            }
        }

        private void Decision() {
            var d = S.Decision;
            _ahora.Etiqueta("Decisión");
            _ahora.Titulo(d.Titulo);
            if (!string.IsNullOrEmpty(d.TextoAviso)) _ahora.Parrafo(d.TextoAviso);
            _ahora.Nota("El reloj está parado mientras decides. Debajo de cada opción, lo que cambia en el momento; lo que llega días después no se ve.");
            foreach (var opcion in d.Opciones) {
                var o = opcion;
                var detalle = o.Bloqueada ? "No disponible: " + o.MotivoBloqueo : Textos.Previsualizar(o.Previsualizacion);
                if (!string.IsNullOrEmpty(o.NotaDeMetodologia) && !o.Bloqueada) detalle += "   ·   " + o.NotaDeMetodologia;
                var boton = _ahora.Opcion(o.Texto, detalle, () => Resolver(o.Id));
                boton.interactable = !o.Bloqueada;
            }
        }

        private void Decidido() {
            _ahora.Etiqueta("Decidido");
            _ahora.Titulo(_decidido.Titulo);
            _ahora.Parrafo("Elegiste: " + _decidido.OpcionTexto);
            _ahora.Parrafo("En el momento: " + _cambiosDeLaDecision);
            _ahora.Nota("Si la decisión tiene consecuencias, llegarán en los próximos días. Si estuvo bien o mal, y por qué, lo leerás en el Dashboard de Lecciones al cerrar el nivel.");
            _ahora.Accion("Volver a la jornada", () => { _decidido = null; });
        }

        private void Cierre() {
            var j = S.Perfil.Jornada;
            _ahora.Etiqueta("Cierre de la jornada");
            _ahora.Titulo($"Son las {S.HoraActual}");
            _ahora.Parrafo("Irte a casa: descansas, y el equipo también. Quedarte hasta las " + $"{j.HoraLimite:00}:00" +
                           ": hoy avanzas un 25 % más, pero lo pagas en cansancio, salud y deuda técnica, y se acumula.");
            _ahora.Parrafo("Si te quedas, hasta esa hora no llegan avisos: es tiempo libre para recorrer el mapa.");
            var botones = _ahora.Fila();
            Ui.Boton(botones, "Irme a casa", () => { Runner.Irse(); Anotar("Te fuiste a casa."); }, VarianteBoton.Primario);
            Ui.Boton(botones, "Quedarme", () => { Runner.Quedarse(); Anotar("Te quedaste haciendo horas extra."); }, VarianteBoton.Peligro);
        }

        private void Prorroga() {
            _ahora.Etiqueta("Horas extra");
            _ahora.Titulo("Te has quedado");
            _ahora.Parrafo("El día ya contó con las horas extra. Hasta la hora límite no llegan avisos: recorre el mapa, habla con quien quede y busca lo que no has encontrado.");
            _ahora.Accion("Irme ya", () => Runner.TerminarLaProrroga());
        }

        private void Resumen() {
            var w = S.W;
            _ahora.Etiqueta("Fin del día");
            _ahora.Titulo($"Día {S.R.DiaActual} de {S.Perfil.DiasTotales}, terminado");
            if (_tengoElInicioDelDia) _ahora.Parrafo($"Hoy: avance {Textos.Cambio(w.Avance - _alEmpezarElDia[0])} · deuda {Textos.Cambio(w.DeudaTecnica - _alEmpezarElDia[1])} · " +
                           $"moral {Textos.Cambio(w.MoralEquipo - _alEmpezarElDia[2])} · cobertura {Textos.Cambio(w.Cobertura - _alEmpezarElDia[3])} · " +
                           $"cansancio {Textos.Cambio(w.Cansancio - _alEmpezarElDia[4])}.");
            if (_tengoElInicioDelDia) _ahora.Parrafo(_decisionesDeHoy == 0 ? "Hoy no tomaste ninguna decisión." :
                           _decisionesDeHoy == 1 ? "Hoy tomaste 1 decisión." : $"Hoy tomaste {_decisionesDeHoy} decisiones.");
            _ahora.Nota("La partida se ha guardado.");
            _ahora.Accion($"Empezar el día {S.R.DiaActual + 1}", EmpezarDia);
        }

        private void FinDelDesarrollo() {
            _ahora.Etiqueta("Fase 3 · el lanzamiento");
            _ahora.Titulo("Se acabaron los días");
            _ahora.Parrafo("El desarrollo ha terminado. Ahora el cliente ve lo que has hecho, y el riesgo que acumulaste durante todos " +
                           "estos días decide si sale bien. Después, el nivel se cierra y se evalúa.");
            _ahora.Accion("Ir al lanzamiento", () => FlujoDelNivel.Lanzar(App));
        }

        // ==================================================================== el mapa

        private void PintarMapa() {
            UiKit.Vaciar(_mapa);
            UiKit.Vaciar(_aqui);
            var mapa = S.Perfil.Mapa;
            if (mapa == null || mapa.Vacio) {
                Ui.Texto(_mapa, "Este nivel no tiene mapa.", EstiloTexto.Pequeno);
                return;
            }
            var puedeMoverse = Runner.Estado == EstadoDelDia.Corriendo || Runner.Estado == EstadoDelDia.Prorroga;

            foreach (var zona in mapa.Zonas) {
                var z = zona;
                var aqui = S.ZonaActual == z.Id;
                var abierta = S.ZonaAbierta(z.Id);
                var detalle = aqui ? "Estás aquí"
                            : !abierta ? "Cerrada por ahora"
                            : $"{mapa.CosteDeVisitar(S.ZonaActual, z.Id)} min";
                if (z.EsAncla) detalle += " · tu escritorio";
                var boton = Ui.BotonDeOpcion(_mapa, z.Nombre, detalle, () => Runner.IrAZona(z.Id));
                boton.interactable = !aqui && abierta && puedeMoverse;
                if (aqui) Ui.Resaltar(boton, true);
            }
            if (!puedeMoverse && Runner.Estado != EstadoDelDia.DesarrolloTerminado)
                Ui.Texto(_mapa, "Solo te puedes mover mientras corre la jornada.", EstiloTexto.Pequeno);

            var actual = mapa.PorId(S.ZonaActual);
            if (actual == null) return;
            Ui.Texto(_aqui, actual.Nombre, EstiloTexto.Cuerpo).fontStyle = FontStyles.Bold;
            if (!string.IsNullOrEmpty(actual.Descripcion)) Ui.Texto(_aqui, actual.Descripcion, EstiloTexto.Pequeno, Tema.texto);
            if (actual.QuienEsta != null && actual.QuienEsta.Count > 0)
                Ui.Texto(_aqui, "Aquí están: " + string.Join(", ", actual.QuienEsta), EstiloTexto.Pequeno);
            if (!string.IsNullOrEmpty(actual.QueDa)) Ui.Texto(_aqui, actual.QueDa, EstiloTexto.Pequeno, Tema.cianClaro);

            foreach (var id in S.ColeccionablesAqui()) {
                var c = id;
                var col = App.Catalogo.Coleccionables.FirstOrDefault(x => x.Id == c);
                var que = col == null ? "algo" : PantallaDelDiario.NombreDeSerie(col.Serie, true);
                Ui.BotonDeOpcion(_aqui, "Hay algo aquí: " + que, "Mirarlo", () => Recoger(c));
            }
        }

        // ==================================================================== acciones

        private void EmpezarDia() {
            var w = S.W;
            _alEmpezarElDia = new[] { w.Avance, w.DeudaTecnica, w.MoralEquipo, w.Cobertura, w.Cansancio };
            _decisionesDeHoy = 0;
            _tengoElInicioDelDia = true;

            var brief = Runner.EmpezarDia();
            if (brief == null) return;
            Anotar($"— Día {brief.Dia} · {brief.EtiquetaUnidad} —");
            if (brief.Ceremonias.Count > 0) Anotar("Ceremonias: " + string.Join(", ", brief.Ceremonias));
            foreach (var origen in brief.EfectosQueVencieronHoy.Distinct())
                Anotar("Hoy se cobra algo de antes: " + NombreDeOrigen(origen) + ".");

            if (S.Beat != null) {
                var guion = App.Catalogo.Guiones.FirstOrDefault(g => g.Id == S.Beat.BeatId);
                if (guion != null) {
                    Anotar($"Escena: «{guion.Titulo}»");
                    _escenaApilada = true;
                    Runner.EnEscena = true;
                    FlujoDelNivel.Reproducir(App, guion, S.Beat.Variante, true, () => _escenaApilada = false);
                }
            }
        }

        private void Atender(Alerta alerta) {
            var ancla = Ancla();
            if (ancla != null && S.ZonaActual != ancla.Id) Runner.IrAZona(ancla.Id);
            if (!alerta.EstaPendiente) {
                Anotar("Llegaste tarde: el aviso caducó por el camino.");
                return;
            }
            if (!Runner.Atender(alerta.Id)) return;
            if (S.Minijuego != null) AbrirMinijuego(S.Minijuego);
        }

        private void AbrirMinijuego(PendingMinigame pendiente) {
            MinijuegoDef def;
            try {
                def = Nexus.Core.Datos.CatalogoMinijuegos.Parsear(new Nexus.Core.Datos.CatalogoDeArchivos(RutasDeGuardado.Contenido).LeerCatalogo(pendiente.Archivo));
            } catch (Exception ex) {
                // El catalogo se valido al arrancar (INV-5), asi que esto no deberia pasar; si pasa, la partida sigue.
                Debug.LogException(ex);
                S.ResolverMinijuego(PuenteDelMotor.Omitido(pendiente.MinijuegoId, pendiente.ObjetivoAprendizaje, null));
                Anotar("No se pudo abrir el minijuego " + pendiente.MinijuegoId + ".");
                return;
            }

            _escenaApilada = true;
            Runner.EnEscena = true;
            Action<ResultadoMinijuego> alTerminar = resultado => {
                S.ResolverMinijuego(resultado);
                App.Router.Volver();
                _escenaApilada = false;
                Anotar($"{S.HoraActual} · {def.Presentacion.Titulo}: {PantallaDeMinijuego.TituloDelResultado(resultado.Resultado).ToLowerInvariant()}.");
            };
            switch (Verbos.Normalizar(def.Verbo)) {
                case Verbos.Ordenar:
                    App.Router.Apilar<PantallaOrdenar>(p => { p.Def = def; p.Pendiente = pendiente; p.AlTerminar = alTerminar; });
                    break;
                case Verbos.Repartir:
                    App.Router.Apilar<PantallaRepartir>(p => { p.Def = def; p.Pendiente = pendiente; p.AlTerminar = alTerminar; });
                    break;
                default:
                    App.Router.Apilar<PantallaDetectar>(p => { p.Def = def; p.Pendiente = pendiente; p.AlTerminar = alTerminar; });
                    break;
            }
        }

        private void Resolver(string opcionId) {
            var antes = S.W.Clone();
            S.ResolverDecision(opcionId);
            _decidido = S.Traza.Entradas.LastOrDefault();
            _decisionesDeHoy++;

            var cambios = new Dictionary<string, double>();
            foreach (var nombre in Nexus.Core.Modelo.WorldState.Nombres) {
                double a, d;
                if (antes.TryGet(nombre, out a) && S.W.TryGet(nombre, out d) && Math.Abs(d - a) >= 0.05 && nombre != "VelocidadMod")
                    cambios[nombre] = d - a;
            }
            _cambiosDeLaDecision = Textos.Previsualizar(cambios);
            if (_decidido != null) Anotar($"{S.HoraActual} · Decidiste en «{_decidido.Titulo}».");
        }

        private void Recoger(string id) {
            S.RecogerColeccionable(id);
            App.AnotarColeccionable(id);
            var col = App.Catalogo.Coleccionables.FirstOrDefault(x => x.Id == id);
            if (col == null) return;
            Anotar($"Encontraste {PantallaDelDiario.NombreDeSerie(col.Serie, true)}: «{col.Titulo}».");
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<PantallaDeColeccionable>(p => {
                p.Coleccionable = col;
                p.AlCerrar = () => _escenaApilada = false;
            });
        }

        private void AbrirDiario() {
            Runner.Pausado = true;
            App.Router.Apilar<PantallaDelDiario>(p => p.AlCerrar = () => Runner.Pausado = false);
        }

        // ==================================================================== lo que avisa el runner

        private void AlSonar(Alerta alerta) {
            Anotar($"{RelojDeJornada.Formatear(alerta.MinutoDeLaAlerta)} · Aviso: {alerta.Texto}");
        }

        private void AlExpirar(Alerta alerta) {
            Anotar($"{RelojDeJornada.Formatear(alerta.MinutoDeExpiracion)} · Un aviso caducó sin que nadie lo atendiera. Alguien decidió por ti.");
        }

        private void AlCierre() {
            Anotar($"{S.HoraActual} · Cierre de la jornada.");
        }

        private void AlFinDelDia() {
            Anotar($"Fin del día {S.R.DiaActual}.");
            App.Guardar(AutoGuardado.Motivos.FinDeJornada);
        }

        // ==================================================================== ayudas

        private List<Alerta> AlertasSonando() {
            var ahora = S.MinutoDelDia;
            return S.AlertasDeHoy.Where(a => a.EstaPendiente && a.YaSono(ahora)).ToList();
        }

        private bool QuedaAlgunAviso() {
            if (Runner.Estado != EstadoDelDia.Corriendo) return false;
            var ahora = S.MinutoDelDia;
            return S.AlertasDeHoy.Any(a => a.EstaPendiente && a.MinutoDeLaAlerta > ahora);
        }

        private ZonaDeNivel Ancla() {
            var mapa = S.Perfil.Mapa;
            return mapa != null && !mapa.Vacio ? mapa.Ancla : null;
        }

        private string NombreDeOrigen(string origen) {
            var ev = App.Catalogo.Eventos.FirstOrDefault(e => e.Id == origen);
            if (ev != null) return "«" + ev.Nombre + "»";
            return origen;
        }

        private void Anotar(string linea) {
            _entradas.Insert(0, linea);
            if (_entradas.Count > 60) _entradas.RemoveAt(_entradas.Count - 1);
            PintarDiario();
        }

        private void PintarDiario() {
            if (_diario == null) return;
            UiKit.Vaciar(_diario);
            foreach (var e in _entradas)
                Ui.Texto(_diario, e, EstiloTexto.Pequeno, e.StartsWith("—") ? Tema.cian : Tema.texto);
        }
    }
}
