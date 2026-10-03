using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Evaluacion;
using Nexus.Core.Guardado;
using Nexus.Core.Jornada;
using Nexus.Core.Minijuegos;
using Nexus.Core.Sesion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
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
        private RectTransform _mapa, _aqui, _diario, _equipo;
        private TMP_Text _titulo, _etiqueta, _riesgo, _satisfaccion;
        private string _claveEquipo;
        private Button _pausa;
        private readonly TMP_Text[] _valores = new TMP_Text[5];
        private readonly TMP_Text[] _efectos = new TMP_Text[5];
        private TMP_Text _pronosticoTitulo, _pronosticoDetalle;
        private BarraView _pronosticoBarra;
        /// <summary>Por modulo del proyecto: el texto del porcentaje y su barra.</summary>
        private readonly List<KeyValuePair<TMP_Text, BarraView>> _modulos = new List<KeyValuePair<TMP_Text, BarraView>>();
        private float _siguientePronostico;
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
            GuiaView.Registrar("dia.ahora", centro);

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
            GuiaView.RegistrarComprobador(AccionYaHecha);
            if (S.R.DiaActual == 0) GuiaView.Avisar(App, "dia.antes");
        }

        /// <summary>
        /// Para la guia: lo que una burbuja pide, ¿ya esta hecho (o ya no se puede hacer)? Si lo esta, la burbuja
        /// no sale; si saliera, esperaria para siempre algo que no va a pasar.
        /// </summary>
        private bool AccionYaHecha(string accion) {
            if (S == null || string.IsNullOrEmpty(accion)) return false;
            if (accion.StartsWith("ir-a-zona:", StringComparison.Ordinal))
                return S.ZonaActual == accion.Substring("ir-a-zona:".Length);
            switch (accion) {
                case "empezar-dia": return S.R.DiaActual > 0 && Runner.Estado != EstadoDelDia.Parado && Runner.Estado != EstadoDelDia.DiaTerminado;
                case "irse": return Runner.Estado == EstadoDelDia.Prorroga || Runner.Estado == EstadoDelDia.DiaTerminado;
                case "atender": return AlertasSonando().Count == 0;
                default: return false;
            }
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
            GuiaView.Registrar("dia.velocidad", _pausa);
            GuiaView.Registrar("dia.diario", Ui.Boton(cabecera, "Diario", AbrirDiario));
            GuiaView.BotonDeAyuda(App, cabecera);
            Ui.Boton(cabecera, "Menú", () => App.Router.Apilar<PantallaDePausa>());
        }

        private void ConstruirIzquierda(Transform padre) {
            var izquierda = Ui.Columna(padre, "Izquierda", Tema.espacio);
            UiKit.Tamano(izquierda, ancho: 400, flexAlto: 1);
            GuiaView.Registrar("dia.reloj", RelojView.Crear(Ui, izquierda, Runner));

            RectTransform contenido;
            var scroll = Ui.Desplazable(izquierda, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            var mapa = Ui.Tarjeta(contenido, "El mapa");
            GuiaView.Registrar("dia.mapa", mapa);
            _mapa = Ui.Columna(mapa, "Zonas", 6);
            var aqui = Ui.Tarjeta(contenido, "Aquí");
            GuiaView.Registrar("dia.aqui", aqui);
            _aqui = Ui.Columna(aqui, "Zona actual", 6);
        }

        private void ConstruirDerecha(Transform padre) {
            var derecha = Ui.Columna(padre, "Derecha", Tema.espacio);
            UiKit.Tamano(derecha, ancho: 420, flexAlto: 1);

            // ★ Las tarjetas van en su propio scroll: sin el, cuando no cabian, la columna las encogia por debajo de
            // su alto y los textos se pisaban. Si no caben, se desplazan; el diario, debajo, con alto fijo.
            RectTransform tarjetas;
            var scrollTarjetas = Ui.Desplazable(derecha, out tarjetas, "Tarjetas");
            UiKit.Tamano(scrollTarjetas, flexAncho: 1, flexAlto: 1);

            // Lo primero: como saldria el lanzamiento si se entregara al ritmo de hoy. Es lo que da peso a cada barra.
            var pronostico = Ui.Tarjeta(tarjetas, "Pronóstico del lanzamiento", Tema.mostaza);
            GuiaView.Registrar("dia.pronostico", pronostico);
            _pronosticoTitulo = Ui.Texto(pronostico, "", EstiloTexto.Cuerpo, Tema.texto);
            _pronosticoBarra = Ui.Barra(pronostico, 0, Tema.cian);
            _pronosticoDetalle = Ui.Texto(pronostico, "", EstiloTexto.Pequeno, Tema.texto);

            var proyecto = Ui.Tarjeta(tarjetas, "El proyecto");
            GuiaView.Registrar("dia.proyecto", proyecto);
            var nombres = new[] { "Avance", "Deuda técnica", "Moral del equipo", "Cobertura de pruebas", "Cansancio" };
            var glosario = new[] { "avance", "deuda-tecnica", "moral", "cobertura", "cansancio" };
            var colores = new[] { Tema.cian, Tema.mostaza, Tema.cianClaro, Tema.cian, Tema.naranja };
            for (var i = 0; i < nombres.Length; i++) {
                var fila = Ui.Fila(proyecto);
                Ui.Texto(fila, nombres[i], EstiloTexto.Pequeno, Tema.texto);
                PantallaDeGlosario.Chip(App, fila, glosario[i]);
                Ui.Resorte(fila);
                _valores[i] = Ui.Texto(fila, "", EstiloTexto.Pequeno, Tema.texto, TextAlignmentOptions.Right);
                _barras[i] = Ui.Barra(proyecto, 0, colores[i]);
                _efectos[i] = Ui.Texto(proyecto, "", EstiloTexto.Pequeno, Tema.textoTenue);
                _efectos[i].fontSize = Tema.tamPequeno - 2;
            }
            var filaRiesgo = Ui.Fila(proyecto);
            _riesgo = Ui.Texto(filaRiesgo, "", EstiloTexto.Pequeno);
            UiKit.Tamano(_riesgo, flexAncho: 1);
            PantallaDeGlosario.Chip(App, filaRiesgo, "riesgo");
            _satisfaccion = Ui.Texto(proyecto, "", EstiloTexto.Pequeno);

            // El avance, parte por parte del sistema: que se construye de verdad cada dia (ronda 4).
            var ficha = S.Perfil.Proyecto;
            if (ficha != null && ficha.Modulos.Count > 0) {
                var modulos = Ui.Tarjeta(tarjetas, "Las partes de " + ficha.Nombre);
                GuiaView.Registrar("dia.modulos", modulos);
                foreach (var m in ficha.Modulos) {
                    var fila = Ui.Fila(modulos);
                    var nombre = Ui.Texto(fila, m.Nombre, EstiloTexto.Pequeno, Tema.texto);
                    UiKit.Tamano(nombre, flexAncho: 1);
                    var pct = Ui.Texto(fila, "", EstiloTexto.Pequeno, Tema.texto, TextAlignmentOptions.Right);
                    UiKit.Tamano(pct, ancho: 70);
                    _modulos.Add(new KeyValuePair<TMP_Text, BarraView>(pct, Ui.Barra(modulos, 0, Tema.cianClaro)));
                }
            }

            // Con quien te llevas bien y que ayudas tienes guardadas.
            var equipo = Ui.Tarjeta(tarjetas, "Tu equipo");
            GuiaView.Registrar("dia.equipo", equipo);
            _equipo = Ui.Columna(equipo, "Personas", 4);

            var diario = Ui.PanelColumna(derecha, "Diario del dia", Tema.margen * 0.75f, Tema.espacio);
            UiKit.Tamano(diario, flexAncho: 1, alto: 240);
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
                              (S.PendingRetro != null && S.PendingRetro.Acciones.Count > 0) || GuiaView.PausaActiva;

            var paso = PasoActual();
            var clave = $"{paso}|{S.R.DiaActual}|{S.ZonaActual}|{string.Join(",", AlertasSonando().Select(a => a.Id))}|" +
                        $"{S.SePuedeCerrarLaJornada}|{QuedaAlgunAviso()}|{_compromiso}|{Runner.PuedeMoverse}|" +
                        string.Join(",", S.TareasDeOficina.Select(t => S.PorQueNoSePuedeHacer(t.Id) == null ? "1" : "0"));
            if (clave != _claveAhora) {
                _claveAhora = clave;
                PintarAhora(paso);
            }

            var claveMapa = $"{S.R.DiaActual}|{S.ZonaActual}|{Runner.Estado}|{Runner.PuedeMoverse}|{S.ColeccionablesAqui().Count}|" +
                            S.R.ConversacionesHechas.Count;
            if (claveMapa != _claveMapa) {
                _claveMapa = claveMapa;
                PintarMapa();
            }

            var claveEquipo = string.Join(",", S.R.Confianza.Select(kv => kv.Key + kv.Value)) + "|" +
                              string.Join(",", S.AyudasDisponibles().Select(kv => kv.Key + kv.Value)) + "|" + Runner.PuedeMoverse;
            if (claveEquipo != _claveEquipo) {
                _claveEquipo = claveEquipo;
                PintarEquipo();
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
            _satisfaccion.text = $"Satisfacción del cliente: {w.SatisfaccionCliente:0} / 100 · pesa 15 de 100 en el lanzamiento" +
                                 (w.SatisfaccionCliente <= 35 ? " · <color=#E0705A>tan baja que pedirán explicaciones</color>" : "");
            PintarEfectos();
            if (_modulos.Count > 0) {
                var porModulo = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(S.Perfil.Proyecto, w.Avance, w.Alcance);
                for (var i = 0; i < _modulos.Count && i < porModulo.Count; i++) {
                    _modulos[i].Key.text = $"{porModulo[i].Value:0} %";
                    _modulos[i].Value.Valor = (float)(porModulo[i].Value / 100.0);
                }
            }
            if (Time.unscaledTime >= _siguientePronostico) {
                _siguientePronostico = Time.unscaledTime + 1f;
                PintarPronostico();
            }
        }

        /// <summary>
        /// Debajo de cada barra, lo que esa barra esta provocando AHORA, con las mismas formulas que usa el motor:
        /// sin esto, solo el avance parecia importar (feedback beta, ronda 2).
        /// </summary>
        private void PintarEfectos() {
            var w = S.W;
            var inc = S.EntregaIncremental;
            var faltan = Math.Max(0, w.Alcance - w.Avance);
            _efectos[0].text = faltan <= 0.05 ? "Todo lo prometido está hecho." : $"Faltan {faltan:0.#} puntos por hacer.";

            var lentoDeuda = (1 - Nexus.Core.Simulacion.ForresterModel.FDeuda(w.DeudaTecnica)) * 100;
            var errDeuda = Nexus.Core.Evaluacion.PronosticoDeLanzamiento.DefectosPorDeuda(w.DeudaTecnica, inc);
            _efectos[1].text = Efecto($"El equipo va un {lentoDeuda:0} % más lento · +{errDeuda:0.#} errores llegarán al cliente",
                                      w.DeudaTecnica >= 50, w.DeudaTecnica >= 50 ? " · ya rompe cosas" : "");

            var rinde = Nexus.Core.Simulacion.ForresterModel.FMoral(w.MoralEquipo) * 100;
            _efectos[2].text = Efecto($"El equipo rinde al {rinde:0} % de lo que podría", w.MoralEquipo <= 35, w.MoralEquipo <= 35 ? " · hay roces" : "");

            var errCob = Nexus.Core.Evaluacion.PronosticoDeLanzamiento.DefectosPorCobertura(w.Cobertura, inc);
            _efectos[3].text = Efecto($"~{errCob:0} errores sin probar llegarían al cliente", w.Cobertura <= 30, "");

            var lentoFatiga = (1 - Nexus.Core.Simulacion.ForresterModel.FFatiga(w.Cansancio)) * 100;
            _efectos[4].text = Efecto($"El equipo va un {lentoFatiga:0} % más lento por cansancio", w.Cansancio >= 60,
                                      w.Cansancio >= 60 ? " · se equivoca más" : "");
        }

        private string Efecto(string texto, bool malo, string extra) {
            return malo ? $"<color=#E0705A>{texto}{extra}</color>" : texto;
        }

        private void PintarPronostico() {
            if (S.R.Fase != 2) return;
            var c = S.Pronostico();
            var color = c.Nivel == Nexus.Core.Evaluacion.NivelesDeLanzamiento.Bien ? "#7FD8E8"
                      : c.Nivel == Nexus.Core.Evaluacion.NivelesDeLanzamiento.Mal ? "#E0705A" : "#E8C25A";
            _pronosticoTitulo.text = $"Si sigues así: <color={color}><b>{Nexus.Core.Evaluacion.NivelesDeLanzamiento.Titulo(c.Nivel).ToLowerInvariant()}</b></color> ({c.Puntaje:0}/100)";
            _pronosticoBarra.Valor = (float)(c.Puntaje / 100.0);
            var peor = c.Factores.OrderBy(f => f.Maximo > 0 ? f.Puntos / f.Maximo : 1).First();
            var malos = c.Factores.Where(f => f.Estado != "bien").Select(f => f.Nombre.ToLowerInvariant()).ToList();
            _pronosticoDetalle.text = $"Lo que más te resta: {peor.Nombre.ToLowerInvariant()} ({peor.Valor})." +
                                      (malos.Count > 1 ? " También flojea: " + string.Join(", ", malos.Where(m => m != peor.Nombre.ToLowerInvariant())) + "." : "");
        }

        /// <summary>Las personas con las que ya hablaste, su confianza, y las ayudas que tienes guardadas.</summary>
        private void PintarEquipo() {
            UiKit.Vaciar(_equipo);
            var rel = App.Catalogo.Relaciones;
            var conocidos = S.R.Confianza.Keys.ToList();
            if (conocidos.Count == 0)
                Ui.Texto(_equipo, "Todavía no has hablado con nadie. Cuando haya alguien en una sala, aparecerá «Hablar con…» en «Aquí».",
                         EstiloTexto.Pequeno, Tema.textoTenue);
            var enEsteNivel = new HashSet<string>(rel.Conversaciones.Where(c => c.Nivel == S.NivelId).Select(c => c.Personaje));
            foreach (var id in conocidos.OrderByDescending(x => enEsteNivel.Contains(x))) {
                var p = rel.PersonajePorId(id);
                var confianza = S.Confianza(id);
                var siguiente = p == null ? null : p.Ayudas.Where(a => a.Umbral > confianza).OrderBy(a => a.Umbral).FirstOrDefault();
                var fila = Ui.Fila(_equipo);
                Ui.Texto(fila, p?.Nombre ?? id, EstiloTexto.Pequeno, Tema.texto);
                Ui.Resorte(fila);
                Ui.Texto(fila, siguiente == null ? $"confianza {confianza} · máxima" : $"confianza {confianza}/{siguiente.Umbral}", EstiloTexto.Pequeno, Tema.cianClaro);
                if (siguiente != null) Ui.Barra(_equipo, siguiente.Umbral > 0 ? (float)confianza / siguiente.Umbral : 1, Tema.cian);
                // Quien sigue de un nivel a otro: lo que ganes con el no se pierde.
                var nota = p != null && p.Persistente
                    ? (enEsteNivel.Contains(id) ? "Sigue contigo en los próximos proyectos." : "No está en este proyecto, pero sigue contigo.")
                    : "Solo en este proyecto.";
                Ui.Texto(_equipo, nota, EstiloTexto.Pequeno, Tema.textoTenue).fontSize = Tema.tamPequeno - 2;
            }
            var ayudas = S.AyudasDisponibles();
            if (ayudas.Count == 0) return;
            Ui.Texto(_equipo, "AYUDAS GUARDADAS", EstiloTexto.Pequeno, Tema.mostaza);
            foreach (var kv in ayudas) {
                var tipo = kv.Key;
                var fila = Ui.Columna(_equipo, espacio: 2);
                Ui.Texto(fila, $"{PantallaDeConversacion.TextoDeAyuda(tipo)}  ×{kv.Value}", EstiloTexto.Pequeno, Tema.texto);
                if (tipo == Nexus.Core.Relaciones.TiposDeAyuda.BajarCansancio) {
                    var usar = Ui.Boton(fila, "Usar ahora", () => {
                        var antes = S.W.Cansancio;
                        if (S.UsarAyuda(tipo)) Anotar($"{S.HoraActual} · El equipo descansa un rato: cansancio {Textos.Cambio(S.W.Cansancio - antes)}.");
                        _claveEquipo = null;
                    }, VarianteBoton.Fantasma);
                    usar.interactable = Runner.PuedeMoverse;
                } else {
                    Ui.Texto(fila, "Se elige en la receta, antes de jugar el reto.", EstiloTexto.Pequeno, Tema.textoTenue);
                }
            }
        }

        /// <summary>Hablar con alguien de la sala: la escena para el reloj, y la charla lo cobra al contestar.</summary>
        private void Hablar(Nexus.Core.Relaciones.Conversacion c) {
            _escenaApilada = true;
            Runner.EnEscena = true;
            App.Router.Apilar<PantallaDeConversacion>(p => {
                p.Conversacion = c;
                p.AlContestar = opcion => Runner.Hablar(c.Id, opcion);
                p.AlCerrar = r => {
                    _escenaApilada = false;
                    if (r == null) return;
                    var nombre = App.Catalogo.Relaciones.PersonajePorId(r.Personaje)?.Nombre ?? r.Personaje;
                    Anotar($"{S.HoraActual} · Hablaste con {nombre}: confianza {Textos.Cambio(r.CambioDeConfianza)}.");
                    GuiaView.Hecho(App, "hablar");
                    GuiaView.Avisar(App, "conversacion");
                    if (r.AyudasNuevas.Count > 0) GuiaView.Avisar(App, "ayuda.disponible");
                };
            });
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
            switch (paso) {
                case Paso.Retro: GuiaView.Avisar(App, "retro"); break;
                case Paso.Planificacion: GuiaView.Avisar(App, "planificacion"); break;
                case Paso.Decision: GuiaView.Avisar(App, "decision.abierta"); break;
                case Paso.Cierre: GuiaView.Avisar(App, "cierre"); break;
                case Paso.Prorroga: GuiaView.Avisar(App, "prorroga"); break;
                case Paso.Resumen: GuiaView.Avisar(App, "resumen"); break;
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
            _ahora.Titulo("¿Con cuánto te comprometes?");
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
            if (brief != null && brief.Incidencia != null) {
                var inc = _ahora.Tarjeta($"Hoy · consecuencia de tu {NombreDeEstadistica(brief.Incidencia.Estadistica)} ({brief.Incidencia.Valor:0})", Tema.rojo);
                Ui.Texto(inc, brief.Incidencia.Titulo, EstiloTexto.Cuerpo).fontStyle = FontStyles.Bold;
                Ui.Texto(inc, brief.Incidencia.Texto, EstiloTexto.Pequeno, Tema.texto);
                var efectos = brief.Incidencia.Efectos.ToDictionary(kv => kv.Key, kv => Nexus.Core.Servicios.EffectApplier.ToDouble(kv.Value));
                Ui.Texto(inc, "Lo que costó: " + Textos.Previsualizar(efectos), EstiloTexto.Pequeno, Tema.amarillo);
            }
            if (brief != null && brief.Avisos.Count > 0) {
                var anuncios = _ahora.Tarjeta("Anuncios de hoy");
                Ui.Texto(anuncios, "Avisan de lo que llegará en los próximos días. Léelos: lo que hoy es un comentario suelto, mañana es un problema.",
                         EstiloTexto.Pequeno);
                foreach (var aviso in brief.Avisos) Ui.Texto(anuncios, aviso, EstiloTexto.Cuerpo);
            }

            PintarOficina();

            _ahora.Espacio();
            var botones = _ahora.Fila();
            if (porSonar) Ui.Boton(botones, "Esperar al siguiente aviso", () => Runner.AdelantarHastaElSiguienteAviso());
            var cerrar = Ui.Boton(botones, "Cerrar la jornada", () => { Runner.CerrarJornada(); GuiaView.Hecho(App, "cerrar-jornada"); },
                                  porSonar ? VarianteBoton.Secundario : VarianteBoton.Primario);
            cerrar.interactable = S.SePuedeCerrarLaJornada;
            if (!S.SePuedeCerrarLaJornada)
                _ahora.Nota("«Cerrar la jornada» salta al final del día, y solo se puede cuando no queda nada pendiente: saltar nunca te ahorra una consecuencia.");
        }

        /// <summary>
        /// El trabajo de oficina: para que un rato sin avisos no sea solo esperar. Cada tarea es un minijuego de
        /// practica que cuesta tiempo del dia y mejora el stock de su tema. No cuenta para la evaluacion.
        /// </summary>
        private void PintarOficina() {
            if (S.TareasDeOficina.Count == 0) return;
            var tarjeta = _ahora.Tarjeta("Trabajo en tu escritorio");
            GuiaView.Registrar("dia.oficina", tarjeta);
            Ui.Texto(tarjeta, "Mientras no suena nada, puedes adelantar trabajo. Cada tarea cuesta tiempo del día y mejora el proyecto. " +
                              "Es práctica: no cuenta para tu evaluación.", EstiloTexto.Pequeno);
            foreach (var t in S.TareasDeOficina) {
                var tarea = t;
                var motivo = S.PorQueNoSePuedeHacer(tarea.Id);
                var detalle = $"{tarea.Minutos} min · mejora: {tarea.Mejora}" + (motivo == null ? "" : "   —   " + motivo);
                var boton = Ui.BotonDeOpcion(tarjeta, tarea.Titulo, detalle, () => EmpezarTarea(tarea));
                boton.interactable = motivo == null && Runner.PuedeMoverse;
            }
            GuiaView.Avisar(App, "oficina.disponible");
        }

        private void EmpezarTarea(Nexus.Core.Oficina.TareaDeOficina tarea) {
            if (!Runner.EmpezarTarea(tarea.Id)) return;
            Anotar($"{S.HoraActual} · Trabajo: «{tarea.Titulo}».");
            var pendiente = new PendingMinigame {
                MinijuegoId = tarea.Minijuego, Archivo = tarea.Archivo, NivelAndamiaje = S.Perfil.NivelAndamiaje
            };
            AbrirEscenaDeMinijuego(pendiente, true, resultado => {
                var cambios = S.ResolverTarea(resultado);
                Anotar($"Trabajo terminado: {Textos.Previsualizar(cambios)}.");
                GuiaView.Hecho(App, "oficina");
                GuiaView.Avisar(App, "oficina.hecha");
            });
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
            Ui.Boton(botones, "Irme a casa", () => { Runner.Irse(); Anotar("Te fuiste a casa."); GuiaView.Hecho(App, "irse"); }, VarianteBoton.Primario);
            // Quedarse hace imposible el «Irme a casa» que pudiera estar pidiendo la guia: se cancela, no se cuelga.
            Ui.Boton(botones, "Quedarme", () => { Runner.Quedarse(); Anotar("Te quedaste haciendo horas extra."); GuiaView.Cancelar("irse"); }, VarianteBoton.Peligro);
        }

        private void Prorroga() {
            _ahora.Etiqueta("Horas extra");
            _ahora.Titulo("Te has quedado");
            _ahora.Parrafo("El día ya contó con las horas extra. Hasta la hora límite no llegan avisos: recorre el mapa, habla con quien quede y busca lo que no has encontrado.");
            _ahora.Accion("Irme ya", () => { Runner.TerminarLaProrroga(); GuiaView.Hecho(App, "irse"); });
        }

        private void Resumen() {
            var w = S.W;
            _ahora.Etiqueta("Fin del día");
            _ahora.Titulo($"Día {S.R.DiaActual} de {S.Perfil.DiasTotales}, terminado");
            if (_tengoElInicioDelDia) _ahora.Parrafo($"Hoy: avance {Textos.Cambio(w.Avance - _alEmpezarElDia[0])} · deuda {Textos.Cambio(w.DeudaTecnica - _alEmpezarElDia[1])} · " +
                           $"moral {Textos.Cambio(w.MoralEquipo - _alEmpezarElDia[2])} · cobertura {Textos.Cambio(w.Cobertura - _alEmpezarElDia[3])} · " +
                           $"cansancio {Textos.Cambio(w.Cansancio - _alEmpezarElDia[4])}.");
            if (_tengoElInicioDelDia) PintarPorQueAvanzaste(w.Avance - _alEmpezarElDia[0]);
            if (_tengoElInicioDelDia) PintarQueSeConstruyo(_alEmpezarElDia[0], w.Avance, w.Alcance);
            if (_tengoElInicioDelDia) _ahora.Parrafo(_decisionesDeHoy == 0 ? "Hoy no tomaste ninguna decisión." :
                           _decisionesDeHoy == 1 ? "Hoy tomaste 1 decisión." : $"Hoy tomaste {_decisionesDeHoy} decisiones.");
            _ahora.Nota("La partida se ha guardado.");
            _ahora.Accion($"Empezar el día {S.R.DiaActual + 1}", EmpezarDia);
        }

        /// <summary>Que partes del sistema avanzaron hoy, y cuanto: el avance en puntos traducido a lo que se construye.</summary>
        private void PintarQueSeConstruyo(double avanceAntes, double avanceAhora, double alcance) {
            var f = S.Perfil.Proyecto;
            if (f == null || f.Modulos.Count == 0) return;
            var antes = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(f, avanceAntes, alcance);
            var ahora = Nexus.Core.Proyecto.AvanceDeModulos.Calcular(f, avanceAhora, alcance);
            var cambios = Enumerable.Range(0, ahora.Count).Where(i => ahora[i].Value - antes[i].Value >= 0.5)
                .Select(i => $"{ahora[i].Key.Nombre} ({antes[i].Value:0} → {ahora[i].Value:0} %)").ToList();
            _ahora.Parrafo(cambios.Count == 0 ? "Hoy no avanzó ninguna parte del sistema."
                                              : "Hoy se construyó: " + string.Join(" · ", cambios) + ".", Tema.cianClaro);
        }

        /// <summary>
        /// Por que el equipo avanzo lo que avanzo hoy: la velocidad base por cada barra que la frena o la empuja.
        /// Es ForresterModel en palabras: deja ver que la deuda, el cansancio y la moral SI cuentan cada dia.
        /// </summary>
        private void PintarPorQueAvanzaste(double avanzado) {
            var t = _ahora.Tarjeta("Por qué avanzaste " + avanzado.ToString("0.#") + " puntos", Tema.cian);
            var m = Nexus.Core.Simulacion.ForresterModel.FMoral(_alEmpezarElDia[2]);
            var f = Nexus.Core.Simulacion.ForresterModel.FFatiga(_alEmpezarElDia[4]);
            var d = Nexus.Core.Simulacion.ForresterModel.FDeuda(_alEmpezarElDia[1]);
            var c = Nexus.Core.Simulacion.ForresterModel.FComp(S.W.Competencia);
            Ui.Texto(t, $"Un equipo en perfectas condiciones haría unos {S.Perfil.VelocidadBase:0.#} puntos al día. Hoy:", EstiloTexto.Pequeno, Tema.texto);
            Factor(t, "Moral del equipo", _alEmpezarElDia[2], m);
            Factor(t, "Cansancio", _alEmpezarElDia[4], f);
            Factor(t, "Deuda técnica", _alEmpezarElDia[1], d);
            Factor(t, "Competencia del equipo", S.W.Competencia, c);
            Ui.Texto(t, "Si te quedaste, las horas extra sumaron un 25 %, pero subieron el cansancio para mañana.", EstiloTexto.Pequeno, Tema.textoTenue);
        }

        private void Factor(Transform padre, string nombre, double valor, double mult) {
            var pct = (mult - 1) * 100;
            var color = pct < -5 ? Tema.rojo : pct > 0.5 ? Tema.cian : Tema.texto;
            Ui.Texto(padre, $"· {nombre} ({valor:0}): {(pct >= 0 ? "+" : "")}{pct:0} %", EstiloTexto.Pequeno, color);
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
            var puedeMoverse = Runner.PuedeMoverse;

            foreach (var zona in mapa.Zonas) {
                var z = zona;
                var aqui = S.ZonaActual == z.Id;
                var abierta = S.ZonaAbierta(z.Id);
                var detalle = aqui ? "Estás aquí"
                            : !abierta ? "Cerrada por ahora"
                            : $"{mapa.CosteDeVisitar(S.ZonaActual, z.Id)} min";
                if (z.EsAncla) detalle += " · tu escritorio";
                var boton = Ui.BotonDeOpcion(_mapa, z.Nombre, detalle, () => IrA(z.Id));
                GuiaView.Registrar("mapa." + z.Id, boton);
                boton.interactable = !aqui && abierta && puedeMoverse;
                if (aqui) Ui.Resaltar(boton, true);
            }
            if (!puedeMoverse && Runner.Estado != EstadoDelDia.DesarrolloTerminado)
                Ui.Texto(_mapa, Runner.EnEscena || Runner.Pausado
                    ? "Estás ocupado aquí: termina lo que tienes abierto antes de irte."
                    : "Solo te puedes mover mientras corre la jornada.", EstiloTexto.Pequeno, Tema.amarillo);

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
                Ui.BotonDeOpcion(_aqui, "Hay algo aquí: " + que, "Mirarlo", () => Recoger(c)).interactable = puedeMoverse;
            }

            foreach (var conv in S.ConversacionesAqui()) {
                var cv = conv;
                var p = App.Catalogo.Relaciones.PersonajePorId(cv.Personaje);
                var detalle = (cv.Pregunta == null ? "charla corta · " : "te quiere preguntar algo · ") +
                              $"{cv.Minutos} min · confianza {S.Confianza(cv.Personaje)}";
                var boton = Ui.BotonDeOpcion(_aqui, "Hablar con " + (p?.Nombre ?? cv.Personaje), detalle, () => Hablar(cv));
                boton.interactable = puedeMoverse;
            }
        }

        // ==================================================================== acciones

        private void IrA(string zonaId) {
            Runner.IrAZona(zonaId);
            if (S.ZonaActual != zonaId) return;
            GuiaView.Hecho(App, "ir-a-zona:" + zonaId);
            GuiaView.Avisar(App, "mapa.zona");
        }

        private void EmpezarDia() {
            var w = S.W;
            _alEmpezarElDia = new[] { w.Avance, w.DeudaTecnica, w.MoralEquipo, w.Cobertura, w.Cansancio };
            _decisionesDeHoy = 0;
            _tengoElInicioDelDia = true;

            var brief = Runner.EmpezarDia();
            if (brief == null) return;
            GuiaView.Hecho(App, "empezar-dia");
            var dia = brief.Dia;
            Anotar($"— Día {brief.Dia} · {brief.EtiquetaUnidad} —");
            if (brief.Ceremonias.Count > 0) Anotar("Ceremonias: " + string.Join(", ", brief.Ceremonias));
            if (brief.Incidencia != null)
            {
                Anotar($"Consecuencia de tu {NombreDeEstadistica(brief.Incidencia.Estadistica)} ({brief.Incidencia.Valor:0}): {brief.Incidencia.Titulo}.");
                GuiaView.Avisar(App, "incidencia");
            }
            foreach (var origen in brief.EfectosQueVencieronHoy.Distinct())
                Anotar("Hoy se cobra algo de antes: " + NombreDeOrigen(origen) + ".");

            if (S.Beat != null) {
                var guion = App.Catalogo.Guiones.FirstOrDefault(g => g.Id == S.Beat.BeatId);
                if (guion != null) {
                    Anotar($"Escena: «{guion.Titulo}»");
                    _escenaApilada = true;
                    Runner.EnEscena = true;
                    FlujoDelNivel.Reproducir(App, guion, S.Beat.Variante, true, () => {
                        _escenaApilada = false;
                        GuiaView.Avisar(App, "dia." + dia);
                    });
                    return;
                }
            }
            GuiaView.Avisar(App, "dia." + dia);
        }

        private void Atender(Alerta alerta) {
            var ancla = Ancla();
            if (ancla != null && S.ZonaActual != ancla.Id) Runner.IrAZona(ancla.Id);
            if (!alerta.EstaPendiente) {
                Anotar("Llegaste tarde: el aviso caducó por el camino.");
                if (AlertasSonando().Count == 0) GuiaView.Cancelar("atender");
                return;
            }
            if (!Runner.Atender(alerta.Id)) return;
            GuiaView.Hecho(App, "atender");
            if (S.Minijuego != null) AbrirMinijuego(S.Minijuego);
        }

        private void AbrirMinijuego(PendingMinigame pendiente) {
            AbrirEscenaDeMinijuego(pendiente, false, resultado => {
                S.ResolverMinijuego(resultado);
                Anotar($"{S.HoraActual} · {PantallaDeMinijuego.TituloDelResultado(resultado.Resultado, pendiente.Verbo)}.");
            });
        }

        /// <summary>Abre la escena de un minijuego encima del dia, con el reloj parado. 'practica' = trabajo de oficina.</summary>
        private void AbrirEscenaDeMinijuego(PendingMinigame pendiente, bool practica, Action<ResultadoMinijuego> alResolver) {
            MinijuegoDef def;
            try {
                def = Nexus.Core.Datos.CatalogoMinijuegos.Parsear(new Nexus.Core.Datos.CatalogoDeArchivos(RutasDeGuardado.Contenido).LeerCatalogo(pendiente.Archivo));
            } catch (Exception ex) {
                // El catalogo se valido al arrancar (INV-5), asi que esto no deberia pasar; si pasa, la partida sigue.
                Debug.LogException(ex);
                alResolver(PuenteDelMotor.Omitido(pendiente.MinijuegoId, pendiente.ObjetivoAprendizaje, null));
                Anotar("No se pudo abrir el minijuego " + pendiente.MinijuegoId + ".");
                return;
            }

            // Guiado: en el tutorial siempre; en los demas niveles, la primera vez que este perfil juega este reto
            // (graduado o de practica). Las siguientes, libre: es donde se demuestra lo aprendido.
            pendiente.Guiado = pendiente.Guiado || S.Perfil.MinijuegosGuiados || !App.YaJugoElMinijuego(pendiente.MinijuegoId);

            _escenaApilada = true;
            Runner.EnEscena = true;
            Action<ResultadoMinijuego> alTerminar = resultado => {
                alResolver(resultado);
                App.AnotarMinijuegoJugado(pendiente.MinijuegoId);
                App.Router.Volver();
                _escenaApilada = false;
            };
            switch (Verbos.Normalizar(def.Verbo)) {
                case Verbos.Ordenar:
                    App.Router.Apilar<PantallaOrdenar>(p => { p.Def = def; p.Pendiente = pendiente; p.Practica = practica; p.AlTerminar = alTerminar; });
                    break;
                case Verbos.Repartir:
                    App.Router.Apilar<PantallaRepartir>(p => { p.Def = def; p.Pendiente = pendiente; p.Practica = practica; p.AlTerminar = alTerminar; });
                    break;
                default:
                    App.Router.Apilar<PantallaDetectar>(p => { p.Def = def; p.Pendiente = pendiente; p.Practica = practica; p.AlTerminar = alTerminar; });
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
            GuiaView.Hecho(App, "decidir");
            GuiaView.Avisar(App, "decision.hecha");
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
            GuiaView.Hecho(App, "recoger");
            GuiaView.Avisar(App, "coleccionable");
        }

        private void AbrirDiario() {
            Runner.Pausado = true;
            App.Router.Apilar<PantallaDelDiario>(p => p.AlCerrar = () => Runner.Pausado = false);
        }

        // ==================================================================== lo que avisa el runner

        private void AlSonar(Alerta alerta) {
            Anotar($"{RelojDeJornada.Formatear(alerta.MinutoDeLaAlerta)} · Aviso: {alerta.Texto}");
            // Primero «estás lejos» (que para el reloj), y despues «pulsa Atender», que espera a que se haga.
            var ancla = Ancla();
            if (ancla != null && S.ZonaActual != ancla.Id) GuiaView.Avisar(App, "alerta.lejos");
            GuiaView.Avisar(App, "alerta.suena");
        }

        private void AlExpirar(Alerta alerta) {
            Anotar($"{RelojDeJornada.Formatear(alerta.MinutoDeExpiracion)} · Un aviso caducó sin que nadie lo atendiera. Alguien decidió por ti.");
            // La guia podia estar esperando «Atender»: ya no hay nada que atender, asi que no se queda colgada.
            if (AlertasSonando().Count == 0) GuiaView.Cancelar("atender");
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

        private static string NombreDeEstadistica(string stock) {
            switch (stock) {
                case "DeudaTecnica": return "deuda técnica";
                case "Cobertura": return "cobertura de pruebas";
                case "Cansancio": return "cansancio";
                case "MoralEquipo": return "moral del equipo";
                case "SatisfaccionCliente": return "satisfacción del cliente";
                default: return stock;
            }
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
