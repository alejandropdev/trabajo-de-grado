using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Detectar;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    /// <summary>
    /// V1 · Detectar. Un artefacto (un grafo de commits, un diagrama de componentes, un diagrama de secuencia
    /// contra una traza real) en el que hay defectos entre señuelos. Se pinchan piezas y se les pone nombre;
    /// marcar lo que esta bien tambien cuesta. Es la misma pantalla para los tres lienzos: lo que cambia es
    /// que piezas trae el artefacto (commits, elementos, conexiones).
    ///
    /// El andamiaje del nivel decide la ayuda: con 3 se resaltan las zonas candidatas y la paleta solo trae
    /// las etiquetas que hacen falta; con 0 no hay ayuda y la paleta trae etiquetas trampa.
    /// </summary>
    public sealed class PantallaDetectar : PantallaDeMinijuego {
        private DetectarState _estado;
        private AndamiajeCfg _andamiaje;
        private readonly Dictionary<string, Button> _piezas = new Dictionary<string, Button>();
        private readonly Dictionary<string, string> _nombres = new Dictionary<string, string>();
        private HashSet<string> _candidatas = new HashSet<string>();
        private RectTransform _marcas, _detalle;
        private TMP_Text _seleccion, _aviso;

        protected override void ConstruirJuego(RectTransform cuerpo) {
            _estado = new DetectarState(Def.Presentacion.SegundosReloj);
            AndamiajeCfg cfg;
            _andamiaje = Def.Andamiaje != null && Def.Andamiaje.TryGetValue(Pendiente.NivelAndamiaje.ToString(), out cfg) ? cfg : null;
            if (_andamiaje != null && _andamiaje.ResaltarZonasCandidatas)
                _candidatas = new HashSet<string>(Def.Zonas.SelectMany(z => z.Commits).Concat(Def.Senuelos.SelectMany(s => s.Commits)));

            var fila = Ui.Fila(cuerpo, "Tablero", Tema.margen * 0.75f, alineacion: TextAnchor.UpperLeft);
            UiKit.Tamano(fila, flexAncho: 1, flexAlto: 1);

            var lienzo = Ui.PanelColumna(fila, "Lienzo", Tema.margen, Tema.espacio);
            UiKit.Tamano(lienzo, flexAncho: 1, flexAlto: 1);
            RectTransform contenido;
            var scroll = Ui.Desplazable(lienzo, out contenido);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);
            PintarArtefacto(contenido);

            ConstruirPanelDeMarcas(fila);
            Repintar();
        }

        // ==================================================================== el artefacto

        private void PintarArtefacto(RectTransform padre) {
            foreach (var e in Def.Artefacto.Elementos) _nombres[e.Id] = e.Texto;
            foreach (var c in Def.Artefacto.Commits) _nombres[c.Id] = c.Id + " · " + c.Mensaje;

            if (Def.Artefacto.Commits.Count > 0) PintarCommits(padre);

            var grupos = Def.Artefacto.Elementos.Select(e => e.Grupo).Concat(Def.Artefacto.Conexiones.Select(c => c.Grupo))
                            .Distinct().ToList();
            if (grupos.Count > 1) {
                // Dos versiones del mismo artefacto (lo diseñado contra lo que paso): una al lado de la otra.
                var columnas = Ui.Fila(padre, "Grupos", Tema.margen, alineacion: TextAnchor.UpperLeft);
                foreach (var g in grupos) {
                    var col = Ui.Columna(columnas, g ?? "grupo", Tema.espacio);
                    UiKit.Tamano(col, flexAncho: 1);
                    Ui.Texto(col, NombreDeGrupo(g), EstiloTexto.Subtitulo);
                    PintarElementos(col, Def.Artefacto.Elementos.Where(e => e.Grupo == g).ToList());
                    PintarConexiones(col, Def.Artefacto.Conexiones.Where(c => c.Grupo == g).ToList());
                }
            } else {
                PintarElementos(padre, Def.Artefacto.Elementos);
                PintarConexiones(padre, Def.Artefacto.Conexiones);
            }
        }

        private static string NombreDeGrupo(string grupo) {
            switch (grupo) {
                case "diagrama": return "Lo que se diseñó";
                case "traza": case "realidad": return "Lo que pasó de verdad";
                default: return Textos.Humanizar(grupo);
            }
        }

        private void PintarCommits(Transform padre) {
            Ui.Texto(padre, "HISTÓRICO DE COMMITS", EstiloTexto.Pequeno, Tema.cian);
            foreach (var c in Def.Artefacto.Commits) {
                var commit = c;
                var detalle = $"rama {c.Rama} · {c.Autor} · {c.Fecha?.Replace('T', ' ')}" + (c.Huerfano ? " · sin padre" : "");
                Pieza(padre, commit.Id, $"{commit.Id}  {commit.Mensaje}", detalle, () => MostrarCommit(commit));
            }
        }

        private void PintarElementos(Transform padre, List<Elemento> elementos) {
            if (elementos.Count == 0) return;
            Ui.Texto(padre, "PIEZAS", EstiloTexto.Pequeno, Tema.cian);
            foreach (var filaDeElementos in elementos.GroupBy(e => e.Fila).OrderBy(g => g.Key)) {
                var fila = Ui.Fila(padre, "Fila", Tema.espacio, alineacion: TextAnchor.UpperLeft);
                foreach (var e in filaDeElementos.OrderBy(x => x.Columna)) {
                    var el = e;
                    var boton = Pieza(fila, el.Id, el.Texto, Textos.Humanizar(el.Tipo), () => MostrarTexto(el.Texto, el.Detalle));
                    UiKit.Tamano(boton, ancho: 250);
                }
            }
        }

        private void PintarConexiones(Transform padre, List<Conexion> conexiones) {
            if (conexiones.Count == 0) return;
            Ui.Texto(padre, conexiones.Any(c => c.Orden > 0) ? "MENSAJES, EN ORDEN" : "CONEXIONES", EstiloTexto.Pequeno, Tema.cian);
            foreach (var c in conexiones.OrderBy(x => x.Orden)) {
                var con = c;
                var prefijo = con.Orden > 0 ? con.Orden + ".  " : "";
                var titulo = $"{prefijo}{Nombre(con.Desde)}  →  {Nombre(con.Hasta)}";
                Pieza(padre, con.Id, titulo, con.Texto, () => MostrarTexto(titulo, con.Texto));
            }
        }

        private string Nombre(string id) {
            string n;
            return id != null && _nombres.TryGetValue(id, out n) ? n : id;
        }

        private Button Pieza(Transform padre, string id, string titulo, string detalle, System.Action alMirar) {
            if (!_nombres.ContainsKey(id)) _nombres[id] = titulo;
            var boton = Ui.BotonDeOpcion(padre, titulo, detalle, () => {
                _estado.Alternar(id);
                alMirar?.Invoke();
                Repintar();
            });
            _piezas[id] = boton;
            return boton;
        }

        // ==================================================================== las marcas

        private void ConstruirPanelDeMarcas(Transform padre) {
            var panel = Ui.Columna(padre, "Marcas", Tema.espacio);
            UiKit.Tamano(panel, ancho: 480, flexAlto: 1);

            var marcar = Ui.Tarjeta(panel, "Marcar");
            _seleccion = Ui.Texto(marcar, "", EstiloTexto.Pequeno, Tema.texto);
            var etiquetas = _andamiaje != null && _andamiaje.Etiquetas != null && _andamiaje.Etiquetas.Count > 0
                ? _andamiaje.Etiquetas : Def.PaletaEtiquetas;
            foreach (var etiqueta in etiquetas) {
                var e = etiqueta;
                Ui.Boton(marcar, "Es: " + Textos.Humanizar(e), () => Marcar(e));
            }
            _aviso = Ui.Texto(marcar, "", EstiloTexto.Pequeno, Tema.amarillo);

            var lista = Ui.PanelColumna(panel, "Lista", Tema.margen * 0.75f, Tema.espacio);
            UiKit.Tamano(lista, flexAlto: 1);
            Ui.Texto(lista, "LO QUE HAS MARCADO", EstiloTexto.Pequeno, Tema.cian);
            var scroll = Ui.Desplazable(lista, out _marcas);
            UiKit.Tamano(scroll, flexAncho: 1, flexAlto: 1);

            var detalle = Ui.Tarjeta(panel, "Detalle");
            _detalle = Ui.Columna(detalle, "Contenido", 4);
            Ui.Texto(_detalle, "Pincha una pieza para verla de cerca.", EstiloTexto.Pequeno);

            Ui.Boton(Ui.Fila(panel), "Entregar", Entregar, VarianteBoton.Primario);
        }

        private void Marcar(string etiqueta) {
            if (_estado.Seleccion.Count == 0) {
                _aviso.text = "Primero pincha en el tablero las piezas que tienen el problema.";
                return;
            }
            _estado.Marcar(etiqueta, Segundo);
            _aviso.text = "";
            Repintar();
        }

        public override void Repintar() {
            if (_estado == null) return;
            var marcadas = new HashSet<string>(_estado.Marcas.SelectMany(m => m.Commits));
            foreach (var kv in _piezas) {
                var elegida = _estado.Seleccion.Contains(kv.Key);
                Color normal = marcadas.Contains(kv.Key) ? Tema.mostazaEnvejecida
                             : _candidatas.Contains(kv.Key) ? Color.Lerp(Tema.pared, Tema.mostaza, 0.3f)
                             : Tema.pared;
                Ui.Resaltar(kv.Value, elegida, normal);
            }

            _seleccion.text = _estado.Seleccion.Count == 0
                ? "Pincha una o varias piezas del tablero y dile qué problema tienen."
                : "Seleccionado: " + string.Join(", ", _estado.Seleccion.Select(Nombre));

            UiKit.Vaciar(_marcas);
            if (_andamiaje != null && _andamiaje.ContadorRestantes)
                Ui.Texto(_marcas, $"Hay {Def.Zonas.Count} problemas en el tablero. Llevas {_estado.Marcas.Count} marcas.", EstiloTexto.Pequeno);
            for (var i = 0; i < _estado.Marcas.Count; i++) {
                var indice = i;
                var m = _estado.Marcas[i];
                var fila = Ui.PanelColumna(_marcas, "Marca", 8, 4, Tema.pared);
                Ui.Texto(fila, Textos.Humanizar(m.Etiqueta), EstiloTexto.Cuerpo, Tema.mostazaClara);
                Ui.Texto(fila, string.Join(", ", m.Commits.Select(Nombre)), EstiloTexto.Pequeno);
                Ui.Boton(Ui.Fila(fila), "Quitar", () => { _estado.Desmarcar(indice); Repintar(); }, VarianteBoton.Fantasma);
            }
        }

        private void MostrarTexto(string titulo, string texto) {
            UiKit.Vaciar(_detalle);
            Ui.Texto(_detalle, titulo, EstiloTexto.Cuerpo).fontStyle = FontStyles.Bold;
            if (!string.IsNullOrEmpty(texto)) Ui.Texto(_detalle, texto, EstiloTexto.Pequeno, Tema.texto);
        }

        private void MostrarCommit(Commit c) {
            UiKit.Vaciar(_detalle);
            Ui.Texto(_detalle, c.Id + "  " + c.Mensaje, EstiloTexto.Cuerpo).fontStyle = FontStyles.Bold;
            Ui.Texto(_detalle, $"{c.Autor} · {c.Fecha?.Replace('T', ' ')} · padres: " +
                               (c.Padres.Count == 0 ? "ninguno" : string.Join(", ", c.Padres)), EstiloTexto.Pequeno);
            Diff diff;
            if (string.IsNullOrEmpty(c.Diff) || !Def.Artefacto.Diffs.TryGetValue(c.Diff, out diff)) return;
            if (!string.IsNullOrEmpty(diff.Titulo)) Ui.Texto(_detalle, diff.Titulo, EstiloTexto.Pequeno, Tema.cianClaro);
            foreach (var linea in diff.Lineas.Take(18)) {
                var t = Ui.Texto(_detalle, (linea.Tipo == "add" ? "+ " : linea.Tipo == "del" ? "- " : "  ") + linea.Texto, EstiloTexto.Mono);
                t.fontSize = Tema.tamPequeno;
                t.color = linea.Tipo == "add" ? Tema.cian : linea.Tipo == "del" ? Tema.rojo : Tema.textoTenue;
            }
        }

        protected override ResultadoMinijuego Evaluar() {
            return DetectarEvaluador.Evaluar(Def, _estado);
        }
    }
}
