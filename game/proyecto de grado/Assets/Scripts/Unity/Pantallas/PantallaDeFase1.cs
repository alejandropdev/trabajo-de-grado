using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// La Fase 1: las tres decisiones fundacionales, en orden, antes del primer dia.
    ///   1 · El encargo (el briefing del nivel)
    ///   2 · La metodologia, y POR QUE — el porque cuenta tanto como la eleccion, y hay porques trampa
    ///   3 · El reparto de las fichas de calidad — lo que se deja a cero es lo que va a fallar
    ///   4 · La arquitectura, y por que
    ///
    /// Ninguna enseña su veredicto aqui: se leen en el Dashboard de Lecciones al cerrar el nivel. Lo unico que
    /// reacciona en el momento es la historia (Voss, tras elegir arquitectura en N1). Cada decision se confirma
    /// una vez y no se deshace, igual que en el motor.
    ///
    /// La Fase 1 del diseño completo es un recorrido por el plano (zonas A–F con reloj de dos horas); en esta
    /// version esta simulada como estos cuatro pasos, que son las decisiones que el recorrido desbloquea.
    /// </summary>
    public sealed class PantallaDeFase1 : Pantalla {
        private enum Paso { Encargo, Metodologia, Calidad, Arquitectura }

        private GameSession S { get { return App.Sesion; } }
        private LevelProfile Perfil { get { return S.Perfil; } }

        private Paso _paso = Paso.Encargo;
        private bool _guionInicialPendiente = true;
        private Hoja _hoja;
        private readonly List<Image> _chips = new List<Image>();
        private readonly List<TMP_Text> _textosChip = new List<TMP_Text>();
        private UnityEngine.UI.ScrollRect _scroll;

        // lo elegido en el paso actual, antes de confirmarlo
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

            var franja = Ui.Fila(marco, "Pasos", 6);
            foreach (var nombre in new[] { "1 · El encargo", "2 · Metodología", "3 · Calidad", "4 · Arquitectura" }) {
                var chip = Ui.PanelColumna(franja, nombre, 8, 0, Tema.pared);
                UiKit.Tamano(chip, flexAncho: 1);
                _chips.Add(chip.GetComponent<Image>());
                _textosChip.Add(Ui.Texto(chip, nombre, EstiloTexto.Pequeno, Tema.texto, TextAlignmentOptions.Center));
            }

            var panel = Ui.PanelColumna(marco, "Contenido", Tema.margen * 1.25f, Tema.espacio);
            UiKit.Tamano(panel, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            _scroll = Ui.Desplazable(panel, out contenido);
            UiKit.Tamano(_scroll, flexAncho: 1, flexAlto: 1);
            _hoja = new Hoja(Ui, contenido);

            foreach (var atributo in Perfil.Fase1.Calidad.Atributos) _fichas[atributo.Id] = 0;
        }

        private void Update() {
            // Los guiones de la Fase 1 (la guia de Marisol, la llegada de Voss) se apilan encima en cuanto la
            // pantalla existe. No se puede hacer en Construir: el router todavia no la tiene en su pila.
            if (!_guionInicialPendiente) return;
            _guionInicialPendiente = false;
            ReproducirGuionesDeFase1(NarrativeBeat.VarianteDefecto, null);
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

        public override void Repintar() {
            if (_hoja == null) return;
            for (var i = 0; i < _chips.Count; i++) {
                var actual = i == (int)_paso;
                _chips[i].color = actual ? Tema.cian : i < (int)_paso ? Tema.hormigon : Tema.pared;
                _textosChip[i].color = actual ? Tema.textoSobreCian : Tema.textoTenue;
            }
            _hoja.Vaciar();
            switch (_paso) {
                case Paso.Encargo: Encargo(); break;
                case Paso.Metodologia: Metodologia(); break;
                case Paso.Calidad: Calidad(); break;
                case Paso.Arquitectura: Arquitectura(); break;
            }
            _scroll.verticalNormalizedPosition = 1;
        }

        // ==================================================================== 1 · el encargo

        private void Encargo() {
            _hoja.Etiqueta("El encargo");
            _hoja.Titulo(Perfil.Nombre);
            if (Perfil.Briefing != null) foreach (var linea in Perfil.Briefing) _hoja.Parrafo(linea);
            _hoja.Espacio();
            _hoja.Nota($"{Perfil.DiasTotales} días · equipo de {Perfil.EquipoInicial} · alcance de {Perfil.AlcanceInicial:0} puntos");
            _hoja.Nota("Léelo con calma: las tres decisiones que vienen se juzgan contra lo que dice aquí.");
            _hoja.Accion("Planificar", () => IrA(Paso.Metodologia));
        }

        // ==================================================================== 2 · metodologia

        private void Metodologia() {
            _hoja.Etiqueta("Decisión 1 de 3");
            _hoja.Titulo("¿Cómo va a trabajar el equipo?");
            _hoja.Parrafo("Elige una metodología y después el motivo. El motivo cuenta tanto como la elección: hay motivos buenos para la " +
                          "metodología equivocada, y motivos equivocados para la buena.");

            var disponibles = S.MetodologiasDisponibles();
            var fila = _hoja.Fila();
            foreach (var m in disponibles) {
                var met = m;
                var tarjeta = Ui.BotonDeOpcion(fila, met.Nombre, met.Resumen, () => { _metodologia = met.Id; Repintar(); });
                UiKit.Tamano(tarjeta, ancho: 520);
                Ui.Resaltar(tarjeta, _metodologia == met.Id);
            }

            _hoja.Espacio();
            _hoja.Subtitulo("¿Por qué?");
            // Los motivos de TODAS las metodologias del nivel, mezclados: si solo salieran los de la elegida,
            // la lista ya diria cual es el motivo «de» cada una.
            var motivos = new List<KeyValuePair<string, string>>();
            foreach (var m in disponibles)
                foreach (var kv in m.TextosDeRazones)
                    if (!motivos.Any(x => x.Key == kv.Key)) motivos.Add(kv);
            foreach (var motivo in motivos.OrderBy(x => x.Value, System.StringComparer.Ordinal)) {
                var id = motivo.Key;
                var boton = _hoja.Opcion(motivo.Value, null, () => { _razonMetodologia = id; Repintar(); });
                Ui.Resaltar(boton, _razonMetodologia == id);
            }

            var confirmar = _hoja.Accion("Confirmar metodología", () => {
                S.ElegirMetodologia(_metodologia, _razonMetodologia);
                IrA(Paso.Calidad);
            });
            confirmar.interactable = _metodologia != null && _razonMetodologia != null;
            _hoja.Nota("Una vez confirmada, no se cambia: reescribe el calendario, las ceremonias y cómo se te va a evaluar.");
        }

        // ==================================================================== 3 · calidad

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
                UiKit.Tamano(Ui.Texto(linea, new string('●', fichas) + new string('○', Mathf.Max(0, 4 - fichas)),
                                      EstiloTexto.Subtitulo, fichas > 0 ? Tema.cian : Tema.textoTenue, TextAlignmentOptions.Center), ancho: 150);
                var mas = Ui.Boton(linea, "+", () => { _fichas[a.Id]++; Repintar(); });
                UiKit.Tamano(mas, ancho: 64);
                mas.interactable = usadas < calidad.Fichas;
                if (fichas == 0 && !string.IsNullOrEmpty(a.TextoSinInversion))
                    Ui.Texto(tarjeta, "Sin fichas: " + a.TextoSinInversion, EstiloTexto.Pequeno, Tema.mostazaClara);
            }

            _hoja.Accion("Confirmar el reparto", () => {
                S.RepartirCalidad(_fichas.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value));
                IrA(Paso.Arquitectura);
            });
            if (usadas < calidad.Fichas) _hoja.Nota("Puedes dejar fichas sin usar, pero no se guardan para después.");
        }

        // ==================================================================== 4 · arquitectura

        private void Arquitectura() {
            var fase1 = Perfil.Fase1;
            _hoja.Etiqueta("Decisión 3 de 3");
            _hoja.Titulo("¿Qué forma va a tener lo que construyes?");
            _hoja.Parrafo("Vuelve a leer el encargo antes de elegir: la respuesta está ahí.");
            var recordatorio = _hoja.Tarjeta("El encargo, otra vez");
            if (Perfil.Briefing != null) foreach (var linea in Perfil.Briefing) Ui.Texto(recordatorio, "· " + linea, EstiloTexto.Pequeno);

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

            var confirmar = _hoja.Accion("Confirmar y empezar el nivel", Cerrar);
            confirmar.interactable = _arquitectura != null && _razonArquitectura != null;
        }

        private void Cerrar() {
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
        }
    }
}
