using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Fase1;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Guia;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La Fase 1, antes del primer dia:
    ///   1 · El encargo (el briefing, y lo que se averiguo en el recorrido)
    ///   2 · La recoleccion 3D por el plano (zonas A–F). La construye otro subequipo; aqui se simula
    ///   3 · La metodologia y su motivo      4 · El reparto de fichas de calidad
    ///   5 · La arquitectura y su motivo     6 · El resumen, con «Confirmar y empezar»
    ///
    /// Se puede ir y volver entre pasos todas las veces que se quiera: las tres decisiones se guardan AQUI y
    /// solo se entregan al motor al confirmar el resumen (una llamada por decision, como siempre). La
    /// recoleccion es la excepcion: pasa en el mundo, y se aplica en cuanto termina.
    ///
    /// Ningun veredicto se enseña aqui: se leen en el Dashboard de Lecciones.
    /// </summary>
    public sealed class PantallaDeFase1 : Pantalla {
        private enum Paso { Encargo, Recoleccion, Metodologia, Calidad, Arquitectura, Resumen }

        private GameSession S { get { return App.Sesion; } }
        private LevelProfile Perfil { get { return S.Perfil; } }
        private bool HayRecoleccion { get { return S.Recoleccion != null; } }

        private Paso _paso = Paso.Encargo;
        private bool _guionInicialPendiente = true;
        private Hoja _hoja;
        private RectTransform _navegacion;
        private readonly Dictionary<Paso, Button> _chips = new Dictionary<Paso, Button>();
        private ScrollRect _scroll;

        private string _metodologia, _razonMetodologia, _arquitectura, _razonArquitectura;
        private readonly Dictionary<string, int> _fichas = new Dictionary<string, int>();

        public override bool PuedeVolver { get { return false; } }

        protected override void Construir() {
            var marco = UiKit.Rellenar(Ui.Columna(Raiz, "Marco", Tema.margen, Tema.margen * 0.75f));

            var cabecera = Ui.Fila(marco);
            var titulos = Ui.Columna(cabecera, espacio: 2);
            UiKit.Tamano(titulos, flexAncho: 1);
            Ui.Texto(titulos, "FASE 1 · PLANIFICACIÓN", EstiloTexto.Pequeno, Tema.cian);
            Ui.Texto(titulos, Perfil.Nombre, EstiloTexto.Titulo);
            GuiaView.BotonDeAyuda(App, cabecera);

            var franja = Ui.Fila(marco, "Pasos", 6);
            GuiaView.Registrar("fase1.pasos", franja);
            foreach (var paso in Pasos()) {
                var p = paso;
                var chip = Ui.Boton(franja, NombreDe(p), () => IrA(p), VarianteBoton.Secundario);
                UiKit.Tamano(chip, flexAncho: 1);
                _chips[p] = chip;
            }

            var panel = Ui.PanelColumna(marco, "Contenido", Tema.margen * 1.25f, Tema.espacio);
            UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            _scroll = Ui.Desplazable(panel, out contenido);
            UiKit.Tamano(_scroll, flexAncho: 1, flexAlto: 1);
            _hoja = new Hoja(Ui, contenido);
            GuiaView.Registrar("fase1.contenido", panel);
            _navegacion = Ui.Fila(panel, "Navegacion");
            GuiaView.Registrar("fase1.navegacion", _navegacion);

            foreach (var atributo in Perfil.Fase1.Calidad.Atributos) _fichas[atributo.Id] = 0;
        }

        private IEnumerable<Paso> Pasos() {
            yield return Paso.Encargo;
            if (HayRecoleccion) yield return Paso.Recoleccion;
            yield return Paso.Metodologia;
            yield return Paso.Calidad;
            yield return Paso.Arquitectura;
            yield return Paso.Resumen;
        }

        private static string NombreDe(Paso p) {
            switch (p) {
                case Paso.Encargo: return "El encargo";
                case Paso.Recoleccion: return "Recorrido";
                case Paso.Metodologia: return "Metodología";
                case Paso.Calidad: return "Calidad";
                case Paso.Arquitectura: return "Arquitectura";
                default: return "Resumen";
            }
        }

        private void Update() {
            // Los guiones de la Fase 1 (la guia de Marisol, la llegada de Voss) se apilan encima en cuanto la
            // pantalla existe. No se puede hacer en Construir: el router todavia no la tiene en su pila.
            if (!_guionInicialPendiente) return;
            _guionInicialPendiente = false;
            ReproducirGuionesDeFase1(NarrativeBeat.VarianteDefecto, () => GuiaView.Avisar(App, "fase1.encargo"));
        }

        private void ReproducirGuionesDeFase1(string variante, System.Action alTerminar) {
            var guiones = FlujoDelNivel.Guiones(App, S.NivelId, MomentosDeGuion.Fase1)
                .Where(g => g.Variantes.ContainsKey(variante)).ToList();
            ReproducirEnOrden(guiones, 0, variante, alTerminar);
        }

        private void ReproducirEnOrden(List<Guion> guiones, int i, string variante, System.Action alTerminar) {
            if (i >= guiones.Count) { alTerminar?.Invoke(); return; }
            FlujoDelNivel.Reproducir(App, guiones[i], variante, true, () => ReproducirEnOrden(guiones, i + 1, variante, alTerminar));
        }

        // ==================================================================== pintar

        public override void Repintar() {
            if (_hoja == null) return;
            foreach (var kv in _chips) {
                var hecho = Completo(kv.Key) && kv.Key != _paso;
                Ui.Resaltar(kv.Value, kv.Key == _paso, hecho ? Tema.hormigon : Tema.pared);
                kv.Value.GetComponentInChildren<TMP_Text>().text = (hecho ? "√ " : "") + NombreDe(kv.Key);
            }
            _hoja.Vaciar();
            switch (_paso) {
                case Paso.Encargo: Encargo(); break;
                case Paso.Recoleccion: Recoleccion(); break;
                case Paso.Metodologia: Metodologia(); break;
                case Paso.Calidad: Calidad(); break;
                case Paso.Arquitectura: Arquitectura(); break;
                case Paso.Resumen: Resumen(); break;
            }
            PintarNavegacion();
            _scroll.verticalNormalizedPosition = 1;
        }

        private bool Completo(Paso p) {
            switch (p) {
                case Paso.Recoleccion: return S.RecoleccionHecha;
                case Paso.Metodologia: return _metodologia != null && _razonMetodologia != null;
                case Paso.Calidad: return true;
                case Paso.Arquitectura: return _arquitectura != null && _razonArquitectura != null;
                case Paso.Resumen: return false;
                default: return true;
            }
        }

        private void PintarNavegacion() {
            UiKit.Vaciar(_navegacion);
            var pasos = Pasos().ToList();
            var i = pasos.IndexOf(_paso);
            if (i > 0) Ui.Boton(_navegacion, "◄  Atrás", () => IrA(pasos[i - 1]), VarianteBoton.Fantasma);
            Ui.Resorte(_navegacion);
            if (i < pasos.Count - 1) {
                var siguiente = Ui.Boton(_navegacion, "Siguiente  ►", () => IrA(pasos[i + 1]), VarianteBoton.Primario);
                siguiente.interactable = Completo(_paso);
            }
        }

        // ==================================================================== 1 · el encargo

        private void Encargo() {
            _hoja.Etiqueta("El encargo");
            _hoja.Titulo(Perfil.Nombre);
            if (Perfil.Briefing != null) foreach (var linea in Perfil.Briefing) _hoja.Parrafo(linea);
            PintarPistas();
            _hoja.Espacio();
            _hoja.Nota($"{Perfil.DiasTotales} días · equipo de {Perfil.EquipoInicial} · alcance de {Perfil.AlcanceInicial:0} puntos");
            _hoja.Nota("Léelo con calma: las tres decisiones que vienen se juzgan contra lo que dice aquí.");
        }

        private void PintarPistas() {
            var pistas = S.PistasEncontradas();
            if (pistas.Count == 0) return;
            var t = _hoja.Tarjeta("Lo que averiguaste en el recorrido", Tema.mostaza);
            foreach (var p in pistas) Ui.Texto(t, "· " + p, EstiloTexto.Cuerpo);
        }

        // ==================================================================== 2 · la recoleccion 3D (simulada)

        private void Recoleccion() {
            var cfg = S.Recoleccion;
            _hoja.Etiqueta("El recorrido por el plano");
            _hoja.Titulo("Recolección");
            if (!string.IsNullOrEmpty(cfg.Texto)) _hoja.Parrafo(cfg.Texto);

            if (!S.RecoleccionHecha) {
                var aviso = _hoja.Tarjeta("Esta parte es en 3D", Tema.mostaza);
                Ui.Texto(aviso, "El recorrido en 3D lo está construyendo otro equipo. Mientras tanto, se simula: elige cómo de a fondo " +
                                "quieres recorrerlo. Cuanto más miras, más encuentras… y más tiempo gastas, y más cosas puedes tocar que no debías.",
                         EstiloTexto.Pequeno, Tema.texto);
            }

            var zonas = _hoja.Tarjeta($"El plano · {cfg.MinutosDisponibles} minutos");
            var visitadas = S.R.RecoleccionHecha ? new HashSet<string>(S.HallazgosRecogidos().Select(h => h.Zona)) : new HashSet<string>();
            foreach (var z in cfg.Zonas) {
                var cuantos = cfg.Hallazgos.Count(h => h.Zona == z.Id);
                Ui.Texto(zonas, $"<b>{z.Id} · {z.Nombre}</b>  ({z.MinutosDeVisita} min)  —  {z.Descripcion}  " +
                                (S.RecoleccionHecha ? "" : new string('?', cuantos)), EstiloTexto.Pequeno,
                         visitadas.Contains(z.Id) ? Tema.cianClaro : Tema.texto);
            }

            if (!S.RecoleccionHecha) {
                var fila = _hoja.Fila();
                GuiaView.Registrar("fase1.simular", fila);
                Ui.Boton(fila, "Simular recolección 3D: rápida", () => Simular(IntensidadesDeSimulacion.Rapida));
                Ui.Boton(fila, "Normal", () => Simular(IntensidadesDeSimulacion.Normal), VarianteBoton.Primario);
                Ui.Boton(fila, "A fondo", () => Simular(IntensidadesDeSimulacion.AFondo));
                return;
            }

            var resultado = _hoja.Tarjeta($"Lo que encontraste · {S.R.MinutosDeRecoleccion} minutos", Tema.cian);
            var hallazgos = S.HallazgosRecogidos();
            if (hallazgos.Count == 0) Ui.Texto(resultado, "Nada. A veces el recorrido no da nada.", EstiloTexto.Cuerpo);
            foreach (var h in hallazgos) Ui.Texto(resultado, DescribirHallazgo(h), EstiloTexto.Cuerpo,
                                                   h.Tipo == TiposDeHallazgo.Riesgo ? Tema.amarillo : Tema.texto);
        }

        private string DescribirHallazgo(Hallazgo h) {
            string que;
            switch (h.Tipo) {
                case TiposDeHallazgo.Pista: que = "Pista"; break;
                case TiposDeHallazgo.Personaje: que = "Se une"; break;
                case TiposDeHallazgo.Moral: que = "Para el equipo"; break;
                case TiposDeHallazgo.Recurso: que = "Recurso"; break;
                case TiposDeHallazgo.Coleccionable: que = "Para tu diario"; break;
                default: que = "Tropiezo"; break;
            }
            var texto = h.Tipo == TiposDeHallazgo.Coleccionable
                ? (App.Catalogo.Coleccionables.FirstOrDefault(c => c.Id == h.Id)?.Titulo ?? h.Id)
                : (h.Texto ?? h.Nombre);
            var efectos = h.Efectos != null && h.Efectos.Count > 0 ? "   (" + Textos.Previsualizar(h.Efectos) + ")" : "";
            return $"<b>{que}:</b> {texto}{efectos}";
        }

        private void Simular(string intensidad) {
            var entrada = S.EntradaDeRecoleccion(App.PerfilActivo != null ? App.PerfilActivo.coleccionablesGlobales : null);
            S.AplicarRecoleccion(SimuladorDeRecoleccion.Simular(entrada, intensidad));
            foreach (var h in S.HallazgosRecogidos())
                if (h.Tipo == TiposDeHallazgo.Coleccionable) App.AnotarColeccionable(h.Id);
            Repintar();
        }

        // ==================================================================== 3 · metodologia

        private void Metodologia() {
            _hoja.Etiqueta("Decisión 1 de 3");
            _hoja.Titulo("¿Cómo va a trabajar el equipo?");
            _hoja.Parrafo("Elige una metodología y después el motivo. El motivo cuenta tanto como la elección: cada metodología tiene " +
                          "buenos motivos para elegirla y motivos que suenan bien pero no lo son.");

            var disponibles = S.MetodologiasDisponibles();
            var fila = _hoja.Fila();
            GuiaView.Registrar("fase1.metodologias", fila);
            foreach (var m in disponibles) {
                var met = m;
                var tarjeta = Ui.BotonDeOpcion(fila, met.Nombre, met.Resumen, () => {
                    if (_metodologia != met.Id) _razonMetodologia = null;   // otra metodologia, otros motivos
                    _metodologia = met.Id;
                    Repintar();
                });
                UiKit.Tamano(tarjeta, ancho: disponibles.Count > 2 ? 440 : 600);
                Ui.Resaltar(tarjeta, _metodologia == met.Id);
            }

            var elegida = disponibles.FirstOrDefault(m => m.Id == _metodologia);
            if (elegida == null) {
                _hoja.Nota("Elige una para ver sus motivos.");
                return;
            }
            _hoja.Espacio();
            _hoja.Subtitulo($"¿Por qué {elegida.Nombre}?");
            foreach (var motivo in elegida.TextosDeRazones.OrderBy(x => x.Value, System.StringComparer.Ordinal)) {
                var id = motivo.Key;
                var boton = _hoja.Opcion(motivo.Value, null, () => { _razonMetodologia = id; Repintar(); });
                Ui.Resaltar(boton, _razonMetodologia == id);
            }
        }

        // ==================================================================== 4 · calidad

        private void Calidad() {
            var calidad = Perfil.Fase1.Calidad;
            var usadas = _fichas.Values.Sum();
            _hoja.Etiqueta("Decisión 2 de 3");
            _hoja.Titulo("¿A qué le dedicas tu cuidado?");
            if (!string.IsNullOrEmpty(calidad.TextoPresion)) _hoja.Parrafo(calidad.TextoPresion);
            _hoja.Subtitulo($"Fichas: {calidad.Fichas - usadas} de {calidad.Fichas} sin repartir",
                            usadas == calidad.Fichas ? Tema.cian : Tema.amarillo);

            foreach (var atributo in calidad.Atributos) {
                var a = atributo;
                var fichas = _fichas[a.Id];
                var tarjeta = Ui.PanelColumna(_hoja.Raiz, a.Nombre, Tema.espacio, 4, Tema.pared);
                var linea = Ui.Fila(tarjeta);
                var nombre = Ui.Texto(linea, a.Nombre, EstiloTexto.Cuerpo);
                nombre.fontStyle = FontStyles.Bold;
                UiKit.Tamano(nombre, flexAncho: 1);
                var menos = Ui.Boton(linea, "−", () => { _fichas[a.Id]--; Repintar(); });
                UiKit.Tamano(menos, ancho: 64);
                menos.interactable = fichas > 0;
                UiKit.Tamano(Ui.Texto(linea, new string('●', fichas) + new string('○', Mathf.Max(0, calidad.Fichas / 2 - fichas)),
                                      EstiloTexto.Subtitulo, fichas > 0 ? Tema.cian : Tema.textoTenue, TextAlignmentOptions.Center), ancho: 180);
                var mas = Ui.Boton(linea, "+", () => { _fichas[a.Id]++; Repintar(); });
                UiKit.Tamano(mas, ancho: 64);
                mas.interactable = usadas < calidad.Fichas;
                if (fichas == 0 && !string.IsNullOrEmpty(a.TextoSinInversion))
                    Ui.Texto(tarjeta, "Sin fichas: " + a.TextoSinInversion, EstiloTexto.Pequeno, Tema.mostazaClara);
            }
            if (usadas < calidad.Fichas) _hoja.Nota("Puedes dejar fichas sin usar, pero no se guardan para después.");
        }

        // ==================================================================== 5 · arquitectura

        private void Arquitectura() {
            var fase1 = Perfil.Fase1;
            _hoja.Etiqueta("Decisión 3 de 3");
            _hoja.Titulo("¿Qué forma va a tener lo que construyes?");
            _hoja.Parrafo("Vuelve a leer el encargo antes de elegir: la respuesta está ahí.");
            var recordatorio = _hoja.Tarjeta("El encargo, otra vez");
            if (Perfil.Briefing != null) foreach (var linea in Perfil.Briefing) Ui.Texto(recordatorio, "· " + linea, EstiloTexto.Pequeno);
            foreach (var p in S.PistasEncontradas()) Ui.Texto(recordatorio, "· " + p, EstiloTexto.Pequeno, Tema.mostazaClara);

            _hoja.Espacio();
            var fila = _hoja.Fila();
            foreach (var a in fase1.Arquitecturas) {
                var arq = a;
                var boton = Ui.BotonDeOpcion(fila, arq.Nombre, null, () => { _arquitectura = arq.Id; Repintar(); });
                UiKit.Tamano(boton, ancho: 420);
                Ui.Resaltar(boton, _arquitectura == arq.Id);
            }

            _hoja.Espacio();
            _hoja.Subtitulo("¿Por qué?");
            foreach (var razon in fase1.RazonesDisponibles) {
                var id = razon.Id;
                var boton = _hoja.Opcion(razon.Texto, null, () => { _razonArquitectura = id; Repintar(); });
                Ui.Resaltar(boton, _razonArquitectura == id);
            }
        }

        // ==================================================================== 6 · resumen

        private void Resumen() {
            _hoja.Etiqueta("Antes de empezar");
            _hoja.Titulo("Tu plan");
            _hoja.Parrafo("Revísalo. Puedes cambiar cualquier cosa; cuando confirmes, ya no hay vuelta atrás: empieza el día 1.");

            var met = S.MetodologiasDisponibles().FirstOrDefault(m => m.Id == _metodologia);
            Linea("Metodología", met == null ? null : $"{met.Nombre} — porque «{met.TextoDe(_razonMetodologia)}»", Paso.Metodologia,
                  _razonMetodologia != null);
            var reparto = string.Join(" · ", Perfil.Fase1.Calidad.Atributos.Where(a => _fichas[a.Id] > 0).Select(a => $"{a.Nombre} {_fichas[a.Id]}"));
            Linea("Calidad", string.IsNullOrEmpty(reparto) ? "sin fichas repartidas" : reparto, Paso.Calidad, true);
            var arq = Perfil.Fase1.Arquitecturas.FirstOrDefault(a => a.Id == _arquitectura);
            var razon = Perfil.Fase1.RazonesDisponibles.FirstOrDefault(r => r.Id == _razonArquitectura);
            Linea("Arquitectura", arq == null ? null : $"{arq.Nombre} — porque «{razon?.Texto}»", Paso.Arquitectura, razon != null);
            if (HayRecoleccion)
                Linea("Recorrido", S.RecoleccionHecha ? $"{S.HallazgosRecogidos().Count} hallazgos en {S.R.MinutosDeRecoleccion} min" : null,
                      Paso.Recoleccion, S.RecoleccionHecha);

            var listo = Pasos().Where(p => p != Paso.Resumen).All(Completo);
            var confirmar = _hoja.Accion("Confirmar y empezar el nivel", Confirmar);
            GuiaView.Registrar("fase1.confirmar", confirmar);
            confirmar.interactable = listo;
            if (!listo) _hoja.Nota("Falta algo por decidir: lo que no tiene √ arriba.", Tema.amarillo);
        }

        private void Linea(string que, string valor, Paso paso, bool completo) {
            var tarjeta = Ui.PanelColumna(_hoja.Raiz, que, Tema.espacio, 4, Tema.pared);
            var fila = Ui.Fila(tarjeta);
            var textos = Ui.Columna(fila, espacio: 2);
            UiKit.Tamano(textos, flexAncho: 1);
            Ui.Texto(textos, que.ToUpperInvariant(), EstiloTexto.Pequeno, Tema.cian);
            Ui.Texto(textos, valor ?? "Sin decidir", EstiloTexto.Cuerpo, completo ? Tema.texto : Tema.amarillo);
            Ui.Boton(fila, "Cambiar", () => IrA(paso), VarianteBoton.Fantasma);
        }

        private void Confirmar() {
            S.ElegirMetodologia(_metodologia, _razonMetodologia);
            S.RepartirCalidad(_fichas.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value));
            S.ElegirArquitectura(_arquitectura, _razonArquitectura);
            // La historia reacciona a la arquitectura (Voss, en N1) con la variante «tras-<id>» del guion de la fase.
            ReproducirGuionesDeFase1("tras-" + _arquitectura, () => {
                S.CerrarFase1();
                FlujoDelNivel.TrasLaFase1(App);
            });
        }

        private void IrA(Paso paso) {
            _paso = paso;
            Repintar();
            GuiaView.Avisar(App, "fase1." + paso.ToString().ToLowerInvariant());
        }
    }
}
