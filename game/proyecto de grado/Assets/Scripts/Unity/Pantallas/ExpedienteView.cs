using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Proyecto;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// Escala una lamina de tamaño fijo para que ocupe el ancho de su contenedor (y le da el alto que le corresponde).
    /// Asi un dibujo de tiza pensado a 1400 px se ve entero en una tarjeta de cualquier ancho.
    /// </summary>
    public sealed class EscalarAlAncho : MonoBehaviour {
        public RectTransform Contenido;
        public float AnchoBase = 1400, AltoBase = 520, EscalaMaxima = 1;
        private LayoutElement _le;
        private float _ultimo = -1;

        private void LateUpdate() {
            var rt = (RectTransform)transform;
            var ancho = rt.rect.width;
            if (ancho <= 1 || Contenido == null || Mathf.Abs(ancho - _ultimo) < 0.5f) return;
            _ultimo = ancho;
            var k = Mathf.Min(EscalaMaxima, ancho / AnchoBase);
            Contenido.localScale = new Vector3(k, k, 1);
            if (_le == null) _le = UiKit.Tamano(rt);
            _le.minHeight = _le.preferredHeight = AltoBase * k;
        }
    }

    /// <summary>
    /// El expediente del proyecto en pantalla (FichaDelProyecto): el diagrama de contexto dibujado con tiza, los
    /// pizarrones de las palabras del oficio, lo que cambia respecto al nivel anterior y las paginas para los
    /// Apuntes. Lo usan la Fase 1 (paso «El proyecto»), la receta de los minijuegos y los Apuntes.
    /// </summary>
    public static class ExpedienteView {
        // ==================================================================== el diagrama de contexto

        /// <summary>
        /// Dibuja el sistema en una imagen dentro del cuadro (x, y, w, h), ordenado de izquierda a derecha y con una
        /// leyenda numerada de las flechas (DiagramaDeContexto). 'resaltado' = el modulo del reto, en mostaza.
        /// </summary>
        public static void DibujarContexto(Tizador t, FichaDelProyecto f, float x, float y, float w, float h, string resaltado = null) {
            if (f == null || f.Modulos.Count == 0) return;
            new DiagramaDeContexto(f).Dibujar(t, x, y, w, h, resaltado);
        }

        /// <summary>Una pizarra con el diagrama de contexto, que se ajusta al ancho de donde se ponga (y su alto, al dibujo).</summary>
        public static void Pizarrita(UiKit ui, Transform padre, FichaDelProyecto f, string resaltado = null) {
            var diagrama = new DiagramaDeContexto(f);
            const float W = DiagramaDeContexto.Ancho + 40;
            var H = diagrama.Alto + 40;
            var caja = ui.Nodo(padre, "Diagrama de contexto");
            caja.gameObject.AddComponent<Image>().color = Tizador.Pizarra;
            UiKit.Tamano(caja, alto: H * 0.6f, flexAncho: 1);
            var lamina = new Lamina(ui, caja, W, H);
            lamina.Dibujo.Tiza = true;
            var rt = lamina.Raiz;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(W, H);
            var escala = caja.gameObject.AddComponent<EscalarAlAncho>();
            escala.Contenido = rt;
            escala.AnchoBase = W;
            escala.AltoBase = H;
            diagrama.Dibujar(new Tizador(lamina, ui.FuenteTiza), 20, 20, DiagramaDeContexto.Ancho, diagrama.Alto, resaltado);
        }

        // ==================================================================== palabras del oficio

        public static void AbrirTermino(AppRoot app, FichaDelProyecto f, TerminoDelSector termino) {
            if (termino == null) return;
            var paginas = new List<PaginaDePizarra> {
                new PaginaDePizarra { Titulo = "¿Qué es «" + termino.Termino + "»?", Texto = termino.Definicion },
                new PaginaDePizarra {
                    Titulo = "Dónde lo verás", EnElJuego = true,
                    Texto = (string.IsNullOrEmpty(termino.DondeLoVeras) ? "" : "En " + termino.DondeLoVeras + "\n") +
                            (f != null && f.Sector != null && !string.IsNullOrEmpty(f.Sector.Nombre)
                                ? $"Es una palabra del sector de {f.Sector.Nombre.ToLowerInvariant()}: quien trabaja ahí la usa a diario, y tu cliente también."
                                : "")
                }
            };
            app.Router.Apilar<PantallaDePizarra>(p => {
                p.Etiqueta = "EL OFICIO";
                p.Tema = f == null ? "" : "Palabras del proyecto · " + f.Nombre;
                p.Paginas = paginas;
            });
        }

        /// <summary>Un chip por palabra del oficio; cada uno abre su pizarron.</summary>
        public static RectTransform ChipsDeVocabulario(AppRoot app, Transform padre, FichaDelProyecto f) {
            var ui = app.Ui;
            var contenedor = ui.Columna(padre, "Vocabulario", 6);
            RectTransform fila = null;
            var enFila = 0;
            foreach (var t in f.Vocabulario) {
                if (fila == null || enFila >= 3) { fila = ui.Fila(contenedor, "Palabras", 6, alineacion: TextAnchor.UpperLeft); enFila = 0; }
                var termino = t;
                var b = ui.Boton(fila, termino.Termino + "  ?", () => AbrirTermino(app, f, termino), VarianteBoton.Secundario);
                UiKit.Tamano(b, ancho: 0, flexAncho: 1);
                enFila++;
            }
            if (fila != null) for (; enFila < 3; enFila++) UiKit.Tamano(ui.Nodo(fila, "Hueco"), ancho: 0, flexAncho: 1);
            return contenedor;
        }

        // ==================================================================== que cambia

        /// <summary>El nivel anterior a este en el catalogo (nivel-01 -> nivel-00). null si es el primero.</summary>
        public static LevelProfile NivelAnterior(AppRoot app, LevelProfile perfil) {
            if (perfil == null || string.IsNullOrEmpty(perfil.Id)) return null;
            var guion = perfil.Id.LastIndexOf('-');
            int numero;
            if (guion < 0 || !int.TryParse(perfil.Id.Substring(guion + 1), out numero) || numero <= 0) return null;
            var id = perfil.Id.Substring(0, guion + 1) + (numero - 1).ToString(new string('0', perfil.Id.Length - guion - 1));
            LevelProfile anterior;
            return app.Catalogo.Niveles.TryGetValue(id, out anterior) ? anterior : null;
        }

        /// <summary>Lo que cambia respecto al nivel anterior: las frases del expediente y los numeros comparados.</summary>
        public static List<string> QueCambia(AppRoot app, LevelProfile perfil) {
            var lineas = new List<string>();
            if (perfil.Proyecto != null) lineas.AddRange(perfil.Proyecto.LoNuevo);
            var antes = NivelAnterior(app, perfil);
            if (antes != null) {
                lineas.Add($"Frente a «{antes.Nombre}»: {perfil.DiasTotales} días (antes {antes.DiasTotales}) · equipo de {perfil.EquipoInicial} " +
                           $"(antes {antes.EquipoInicial}) · {perfil.AlcanceInicial:0} puntos de alcance (antes {antes.AlcanceInicial:0}) · " +
                           $"presupuesto {perfil.PresupuestoInicial:0} (antes {antes.PresupuestoInicial:0}).");
                if (antes.Proyecto != null && perfil.Proyecto != null)
                    lineas.Add($"Antes: {antes.Proyecto.Nombre}, para {antes.Proyecto.Cliente.Nombre}, con {antes.Proyecto.Modulos.Count} módulos. " +
                               $"Ahora: {perfil.Proyecto.Nombre}, para {perfil.Proyecto.Cliente.Nombre}, con {perfil.Proyecto.Modulos.Count} módulos.");
            }
            if (perfil.Proyecto != null && perfil.Proyecto.TemasDelNivel.Count > 0)
                lineas.Add("Lo que practicas aquí: " + string.Join(", ", perfil.Proyecto.TemasDelNivel) + ".");
            return lineas;
        }

        // ==================================================================== paginas para los apuntes

        /// <summary>
        /// Las paginas del expediente para un pizarron (Apuntes): el proyecto, el sistema en una imagen, sus partes y
        /// cuanto llevan, las palabras del oficio, que cambia y los documentos. 'dibujos' recibe el dibujo de la
        /// pagina del diagrama.
        /// </summary>
        public static List<PaginaDePizarra> Paginas(AppRoot app, LevelProfile perfil, WorldState w,
                                                    Dictionary<PaginaDePizarra, Action<Tizador, float>> dibujos,
                                                    Action<List<PaginaDePizarra>, string, string> anadir) {
            var paginas = new List<PaginaDePizarra>();
            var f = perfil.Proyecto;
            if (f == null) return paginas;

            anadir(paginas, "El proyecto: " + f.Nombre,
                   $"Cliente: {f.Cliente.Nombre}. {f.Cliente.QueHace}\nQué le duele: {f.Cliente.QueLeDuele}\n" +
                   $"Sector: {f.Sector.Nombre}. {f.Sector.ComoTrabaja}\nEn una frase: {f.EnUnaFrase}");

            var diagrama = new PaginaDePizarra {
                Titulo = "El sistema en una imagen",
                Texto = "Fuera, quién usa el sistema; dentro, sus partes. Las flechas dicen quién le pide qué a quién."
            };
            paginas.Add(diagrama);
            dibujos[diagrama] = (t, y) => DibujarContexto(t, f, 60, y + 20, MarcoDePizarra.W - 120, MarcoDePizarra.H - y - 140);

            var avance = w == null ? null : AvanceDeModulos.Calcular(f, w.Avance, w.Alcance);
            anadir(paginas, "Las partes del sistema",
                   string.Join("\n", f.Modulos.Select((m, i) => $"· {m.Nombre}" + (avance != null ? $" ({avance[i].Value:0} % hecho)" : "") + $": {m.QueHace}")));
            anadir(paginas, "Palabras del oficio", string.Join("\n", f.Vocabulario.Select(v => $"· {v.Termino}: {v.Definicion}")));
            anadir(paginas, "Qué tiene de distinto este proyecto", string.Join("\n", QueCambia(app, perfil).Select(l => "· " + l)));
            if (f.Artefactos.Count > 0)
                anadir(paginas, "Los documentos que importan aquí", string.Join("\n", f.Artefactos.Select(a => $"· {a.Id}: {a.ParaQueAqui}")));
            return paginas;
        }
    }
}
