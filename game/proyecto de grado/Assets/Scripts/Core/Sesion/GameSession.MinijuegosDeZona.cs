using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Jornada;
using Nexus.Core.Servicios;

namespace Nexus.Core.Sesion {
    /// <summary>Lo que dejo un minijuego de zona: si salio bien, lo que cambio y lo que se descubrio (rumores, pistas).</summary>
    public sealed class ResultadoDeMinijuegoDeZona {
        public string Tipo;
        public bool Exito;
        public Dictionary<string, double> Cambios = new Dictionary<string, double>(StringComparer.Ordinal);
        public List<string> Revelaciones = new List<string>();
    }

    /// <summary>
    /// Los minijuegos de zona (MinijuegosDeZona): una vez al dia por zona, cobran sus minutos como cualquier accion y
    /// dejan un efecto pequeño. La pantalla juega la mecanica (la taza, los logs…) y aqui llega solo si salio bien.
    /// </summary>
    public sealed partial class GameSession {
        /// <summary>El minijuego de la zona donde estas, o null si aqui no hay ninguno.</summary>
        public string MinijuegoDeZonaAqui() {
            var zona = Perfil.Mapa == null ? null : Perfil.Mapa.PorId(R.ZonaActual);
            return zona == null || !TiposDeMinijuegoDeZona.EsValido(zona.MinijuegoDeZona) ? null : zona.MinijuegoDeZona;
        }

        public bool MinijuegoDeZonaHechoHoy(string zonaId) {
            int dia;
            return R.MinijuegoDeZonaJugado.TryGetValue(zonaId ?? "", out dia) && dia == R.DiaActual;
        }

        public string PorQueNoSeJuegaElMinijuegoDeZona() {
            var tipo = MinijuegoDeZonaAqui();
            if (tipo == null) return "Aquí no hay nada que hacer.";
            if (MinijuegoDeZonaHechoHoy(R.ZonaActual)) return "Hoy ya lo hiciste. Vuelve mañana.";
            return PorQueNoSePuedeActuar(MinijuegosDeZona.Minutos(tipo));
        }

        /// <summary>
        /// Juega el minijuego de esta zona con el resultado de la mecanica ('exito'). Cobra sus minutos, aplica el
        /// efecto y lo marca como hecho hoy.
        /// </summary>
        public ResultadoDeAvance JugarMinijuegoDeZona(bool exito, out ResultadoDeMinijuegoDeZona resultado) {
            Exigir(PorQueNoSeJuegaElMinijuegoDeZona());
            var tipo = MinijuegoDeZonaAqui();
            R.MinijuegoDeZonaJugado[R.ZonaActual] = R.DiaActual;
            resultado = new ResultadoDeMinijuegoDeZona { Tipo = tipo, Exito = exito };
            switch (tipo) {
                case TiposDeMinijuegoDeZona.Cafe:
                    // Un cafe bien servido: el equipo descansa un poco. Mal servido, casi nada.
                    Aplicar(resultado, "Cansancio", exito ? -3.0 : -0.5);
                    break;
                case TiposDeMinijuegoDeZona.Logs:
                    // El error cazado a tiempo es un caso de prueba que ya no se escapa.
                    if (exito) Aplicar(resultado, "Cobertura", 2.0);
                    break;
                case TiposDeMinijuegoDeZona.Rumores:
                    resultado.Revelaciones.Add(Rumor());
                    resultado.Exito = true;
                    break;
                case TiposDeMinijuegoDeZona.Tablon:
                    resultado.Revelaciones.AddRange(PistasDelTablon());
                    resultado.Exito = true;
                    break;
            }
            return AvanzarReloj(MinijuegosDeZona.Minutos(tipo));
        }

        private void Aplicar(ResultadoDeMinijuegoDeZona r, string stock, double delta) {
            double antes, despues;
            W.TryGet(stock, out antes);
            EffectApplier.Aplicar(W, new Dictionary<string, object> { { stock, delta } });
            W.TryGet(stock, out despues);
            r.Cambios[stock] = Math.Round(despues - antes, 2);
        }

        /// <summary>Lo que se oye en el bullpen: a que hora y por donde llegara el proximo aviso de hoy, si llega alguno.</summary>
        private string Rumor() {
            var ahora = MinutoDelDia;
            var proxima = AlertasDeHoy.Where(a => a.EstaPendiente && !a.YaSono(ahora)).OrderBy(a => a.MinutoDeLaAlerta).FirstOrDefault();
            if (proxima == null) return "«Hoy está todo tranquilo… demasiado tranquilo.»";
            return $"«Dicen que hacia las {RelojDeJornada.Formatear(proxima.MinutoDeLaAlerta)} va a llegar algo por {proxima.Canal}.»";
        }

        /// <summary>Las notas del tablon: las pistas de hasta tres coleccionables del nivel que aun no han aparecido.</summary>
        private List<string> PistasDelTablon() {
            var pistas = new List<string>();
            if (Perfil.Mapa == null) return pistas;
            foreach (var zona in Perfil.Mapa.Zonas) {
                if (zona == null || zona.Coleccionables == null) continue;
                foreach (var id in zona.Coleccionables) {
                    if (pistas.Count >= 3) return pistas;
                    if (R.ColeccionablesRecogidos.Contains(id) || HaAparecido(id)) continue;
                    var col = Coleccionable(id);
                    if (col != null && col.Aparicion != null && !string.IsNullOrEmpty(col.Aparicion.Pista))
                        pistas.Add($"{zona.Nombre}: {col.Aparicion.Pista}");
                }
            }
            if (pistas.Count == 0) pistas.Add("«No queda nada por descubrir… de momento.»");
            return pistas;
        }
    }
}
