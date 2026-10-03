using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Narrativa;
using Nexus.Unity.Aplicacion;
using Nexus.Unity.Pantallas.Minijuegos;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// «Apuntes del proyecto»: lo que hay que saber de ESTE nivel, en un pizarron de varias paginas. Sustituye al
    /// historial de burbujas (feedback beta, ronda 2: el dia 3 solo enseñaba lo del ultimo minijuego). Siempre esta
    /// todo: el encargo, las tres decisiones (que son y que elegiste), como se mide el proyecto, como es un dia y el
    /// equipo. Sale de los datos del nivel y de la sesion, asi que vale para cualquier nivel.
    /// </summary>
    public static class ApuntesDelProyecto {
        private const int LineasPorPagina = 11;
        private const int Caracteres = 78;

        public static void Abrir(AppRoot app) {
            var s = app.Sesion;
            if (s == null) return;
            var perfil = s.Perfil;

            // 0 · el expediente del proyecto: que se construye, para quien, sus partes y sus palabras (ronda 4)
            var dibujos = new Dictionary<PaginaDePizarra, System.Action<Tizador, float>>();
            var paginas = ExpedienteView.Paginas(app, perfil, s.W, dibujos, Anadir);

            // 1 · el encargo (el ticket original)
            var encargo = string.Join("\n", (perfil.Briefing ?? new string[0]).Select(l => "· " + l));
            var pistas = s.PistasEncontradas();
            if (pistas.Count > 0) encargo += "\nLo que averiguaste en el recorrido:\n" + string.Join("\n", pistas.Select(p => "· " + p));
            Anadir(paginas, (perfil.Proyecto != null ? "El ticket original: " : "El encargo: ") + perfil.Nombre, encargo);

            // 2 · las tres decisiones: que son
            var explicacion = app.Catalogo.Guiones
                .Where(g => g != null && g.Momento == MomentosDeGuion.Fase1 && g.Nivel == perfil.Id && g.Variantes.ContainsKey(NarrativeBeat.VarianteDefecto))
                .SelectMany(g => g.Variantes[NarrativeBeat.VarianteDefecto]).Where(l => l.Quien == "Marisol Andrade").Select(l => l.Texto).ToList();
            if (explicacion.Count == 0)
                explicacion.Add("Todo proyecto toma tres decisiones antes de empezar: CÓMO trabaja el equipo (la metodología), " +
                                "QUÉ cuida más (las fichas de calidad) y QUÉ FORMA tiene lo que se construye (la arquitectura). " +
                                "En las tres cuenta el porqué.");
            Anadir(paginas, "Las tres decisiones", string.Join("\n", explicacion));

            // 3 · lo que elegiste
            Anadir(paginas, "Lo que elegiste, y por qué", LoElegido(app));

            // 4 · como se mide
            var u = perfil.Lanzamiento;
            var mide = "· Avance: puntos hechos de los prometidos (alcance). Las horas extra lo suben, y el cansancio lo cobra.\n" +
                       "· Deuda técnica: los atajos. Frena al equipo y trae errores; muy alta, rompe lo que funcionaba.\n" +
                       "· Cobertura: lo probado. Si es baja, el cliente encuentra los errores.\n" +
                       "· Cansancio y moral: cuánto rinde el equipo cada día.\n" +
                       "· Satisfacción del cliente: cuenta en el lanzamiento.\n" +
                       "· Riesgo latente: todo lo anterior junto; con riesgo alto, sale mal aunque esté terminado.";
            if (u != null)
                mide += $"\nPara que salga BIEN: entregar el {u.EntregaBien * 100:0} % o más, como mucho {u.DefectosBien} errores, " +
                        $"riesgo por debajo de {u.RiesgoBien:0}, satisfacción de {u.SatisfaccionBien:0} o más y decisiones bien justificadas.";
            Anadir(paginas, "Cómo se mide tu proyecto", mide);

            // 5 · como es un dia (las burbujas del tutorial sobre el dia, que valen para todos los niveles)
            var dia = app.Catalogo.Guia
                .Where(p => p.Nivel == "nivel-00" && (p.Disparador == "dia.antes" || p.Disparador == "cierre" || p.Disparador == "alerta.suena" || p.Disparador == "oficina.disponible"))
                .Where(p => string.IsNullOrEmpty(p.EsperaAccion))
                .OrderBy(p => p.Orden).Select(p => "· " + p.Texto).ToList();
            Anadir(paginas, "Cómo es un día", string.Join("\n", dia));

            // 6 · el equipo
            var rel = app.Catalogo.Relaciones;
            var equipo = s.R.Confianza.Count == 0
                ? "Todavía no has hablado con nadie. En cada sala hay gente: habla con ellos casi cada día y su confianza te dará ayudas."
                : string.Join("\n", s.R.Confianza.Select(kv => {
                    var p = rel.PersonajePorId(kv.Key);
                    var sig = p == null ? null : p.Ayudas.Where(a => a.Umbral > kv.Value).OrderBy(a => a.Umbral).FirstOrDefault();
                    return $"· {p?.Nombre ?? kv.Key}: confianza {kv.Value}" + (sig != null ? $" (su próxima ayuda, con {sig.Umbral})" : "") +
                           (p != null && p.Persistente ? " · sigue contigo en otros proyectos" : "");
                }));
            var ayudas = s.R.AyudasDisponibles.Where(kv => kv.Value > 0).ToList();
            if (ayudas.Count > 0) equipo += "\nAyudas guardadas:\n" + string.Join("\n", ayudas.Select(kv => $"· {PantallaDeConversacion.TextoDeAyuda(kv.Key)} ×{kv.Value}"));
            Anadir(paginas, "Tu equipo", equipo);

            app.Router.Apilar<PantallaDePizarra>(p => {
                p.Etiqueta = "APUNTES";
                p.Tema = "Apuntes del proyecto · " + perfil.Nombre;
                p.Paginas = paginas;
                p.Dibujos = dibujos;
            });
        }

        private static string LoElegido(AppRoot app) {
            var s = app.Sesion;
            var f1 = s.Perfil.Fase1;
            var b = s.R.Fase1Borrador;
            var metId = s.Fase1Cerrada ? s.Metodologia?.Id : b?.Metodologia;
            var metRazon = s.Fase1Cerrada ? s.RazonDeLaMetodologia : b?.RazonMetodologia;
            var arqId = s.Fase1Cerrada ? s.ArquitecturaElegida : b?.Arquitectura;
            var arqRazon = s.Fase1Cerrada ? s.RazonDeLaArquitectura : b?.RazonArquitectura;
            var fichas = s.Fase1Cerrada ? s.FichasDeCalidad.ToDictionary(kv => kv.Key, kv => kv.Value) : b?.Fichas;

            var lineas = new List<string>();
            if (!s.Fase1Cerrada) lineas.Add("(Todavía estás planeando: esto es lo que llevas elegido.)");
            var met = metId != null && app.Catalogo.Metodologias.ContainsKey(metId) ? app.Catalogo.Metodologias[metId] : null;
            lineas.Add(met == null ? "· Metodología: sin elegir." : $"· Metodología: {met.Nombre}" + (metRazon != null ? $", porque «{met.TextoDe(metRazon)}»." : "."));
            var reparto = fichas == null ? "" : string.Join(", ", f1.Calidad.Atributos.Where(a => fichas.ContainsKey(a.Id) && fichas[a.Id] > 0)
                                                                     .Select(a => $"{a.Nombre} {fichas[a.Id]}"));
            lineas.Add("· Calidad: " + (string.IsNullOrEmpty(reparto) ? "sin fichas repartidas." : reparto + "."));
            var sinFichas = fichas == null ? f1.Calidad.Atributos : f1.Calidad.Atributos.Where(a => !fichas.ContainsKey(a.Id) || fichas[a.Id] == 0).ToList();
            if (sinFichas.Count > 0) lineas.Add("  Sin fichas (es lo que puede fallar): " + string.Join(", ", sinFichas.Select(a => a.Nombre)) + ".");
            var arq = f1.Arquitecturas.FirstOrDefault(a => a.Id == arqId);
            var razon = f1.RazonesDisponibles.FirstOrDefault(r => r.Id == arqRazon);
            lineas.Add(arq == null ? "· Arquitectura: sin elegir." : $"· Arquitectura: {arq.Nombre}" + (razon != null ? $", porque «{razon.Texto}»." : "."));
            lineas.Add("Si salió mal alguna, lo verás en el lanzamiento y en Lecciones: cada una con su porqué.");
            return string.Join("\n", lineas);
        }

        /// <summary>Añade una seccion, partida en tantas paginas como haga falta para que se lea comoda.</summary>
        private static void Anadir(List<PaginaDePizarra> paginas, string titulo, string texto) {
            var lineas = new List<string>();
            foreach (var parrafo in (texto ?? "").Split('\n')) {
                var actual = "";
                foreach (var palabra in parrafo.Split(' ')) {
                    if (actual.Length + palabra.Length + 1 > Caracteres && actual.Length > 0) { lineas.Add(actual); actual = palabra; }
                    else actual = actual.Length == 0 ? palabra : actual + " " + palabra;
                }
                lineas.Add(actual);
            }
            for (var i = 0; i < lineas.Count; i += LineasPorPagina) {
                var trozo = lineas.Skip(i).Take(LineasPorPagina).ToList();
                paginas.Add(new PaginaDePizarra {
                    Titulo = i == 0 ? titulo : titulo + " (sigue)",
                    Texto = string.Join("\n", trozo)
                });
            }
        }
    }
}
