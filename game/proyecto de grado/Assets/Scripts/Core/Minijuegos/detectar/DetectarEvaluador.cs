using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Minijuegos.Detectar
{
    /// <summary>
    /// El corazon del componente "marcar sobre un lienzo".
    /// Funcion pura: (definicion, marcas) -> resultado. Sin Unity, sin reloj,
    /// sin aleatoriedad. Es lo que se testea con dotnet test y lo que hace
    /// defendible la evaluacion pedagogica: la rubrica sale de un JSON, no de un modelo.
    /// </summary>
    public static class DetectarEvaluador
    {
        public const string TODOS = "todos";
        public const string PARCIAL = "parcial";
        public const string FALSO_POSITIVO = "falsoPositivo";
        public const string OMITIDO = "omitido";

        public static ResultadoMinijuego Evaluar(MinijuegoDef def, DetectarState st)
        {
            var traza = new List<MarcaRegistrada>();
            var zonasAcertadas = new HashSet<string>();
            bool hayFalsoPositivo = false;

            foreach (var marca in st.Marcas)
            {
                // Las zonas pueden solaparse (x04 esta dentro del tramo Z1 y es tambien Z2).
                // Primero se busca la que coincide con la etiqueta que puso el jugador;
                // solo si ninguna coincide se toma cualquiera que toque, para poder
                // distinguir "marco el sitio correcto con la etiqueta equivocada".
                var zona = def.Zonas.FirstOrDefault(z => Toca(marca.Commits, z.Commits) && marca.Etiqueta == z.Defecto)
                           ?? def.Zonas.FirstOrDefault(z => Toca(marca.Commits, z.Commits));
                var senuelo = def.Senuelos.FirstOrDefault(s => Toca(marca.Commits, s.Commits));

                bool etiquetaOk = zona != null && marca.Etiqueta == zona.Defecto;
                if (etiquetaOk) zonasAcertadas.Add(zona.Id);
                if (senuelo != null) hayFalsoPositivo = true;

                traza.Add(new MarcaRegistrada
                {
                    Commits = marca.Commits.ToList(),
                    Etiqueta = marca.Etiqueta,
                    ZonaAcertada = etiquetaOk ? zona.Id : null,
                    SenueloTocado = senuelo?.Id,
                    SegundoDeLaPartida = (int)marca.Segundo
                });
            }

            string clave;
            if (st.Marcas.Count == 0) clave = OMITIDO;
            else if (hayFalsoPositivo) clave = FALSO_POSITIVO;          // manda sobre acertar
            else if (zonasAcertadas.Count == def.Zonas.Count) clave = TODOS;
            else clave = PARCIAL;

            return Construir(def, clave, traza, zonasAcertadas);
        }

        /// <summary>Una marca cubre una zona si comparten al menos un commit.</summary>
        private static bool Toca(List<string> commitsMarca, List<string> commitsZona)
        {
            return commitsMarca.Any(commitsZona.Contains);
        }

        private static ResultadoMinijuego Construir(
            MinijuegoDef def, string clave, List<MarcaRegistrada> traza, HashSet<string> acertadas)
        {
            Consecuencia c;
            if (!def.Consecuencias.TryGetValue(clave, out c)) c = new Consecuencia();

            var res = new ResultadoMinijuego
            {
                MinijuegoId = def.Id,
                Resultado = clave,
                Rubrica = c.Rubrica,
                EfectosInmediatos = new Dictionary<string, float>(c.EfectosInmediatos),
                EfectosDiferidos = c.EfectosDiferidos.ToList(),
                Hallazgos = c.Hallazgos.ToList(),
                Traza = traza,
                TextoCierre = def.Cierre?.Texto
            };

            // El detalle explica lo que habia, no puntua. Se muestra al cerrar.
            foreach (var z in def.Zonas)
                if (acertadas.Contains(z.Id))
                    res.Detalle.Add(z.Explicacion);

            foreach (var m in traza)
                if (m.SenueloTocado != null)
                {
                    var s = def.Senuelos.First(x => x.Id == m.SenueloTocado);
                    res.Detalle.Add(s.RazonNoEsDefecto);
                }

            return res;
        }
    }
}
