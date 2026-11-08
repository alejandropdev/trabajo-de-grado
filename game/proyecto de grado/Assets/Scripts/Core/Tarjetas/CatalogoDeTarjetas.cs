using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nexus.Core.Datos;
using Nexus.Core.Evaluacion;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Servicios;

namespace Nexus.Core.Tarjetas {
    /// <summary>
    /// Las tarjetas del juego, ya cargadas y validadas. Se leen de la carpeta "tarjetas/" (un archivo por momento)
    /// y NO pasan sin validar, igual que el resto de catalogos (INV-5): un error de contenido se ve al arrancar,
    /// no a mitad de una partida.
    /// </summary>
    public sealed class CatalogoDeTarjetas {
        public const string Carpeta = "tarjetas";

        public readonly List<TarjetaDef> Tarjetas = new List<TarjetaDef>();

        public TarjetaDef Buscar(string id) {
            foreach (var t in Tarjetas)
                if (string.Equals(t.Id, id, StringComparison.Ordinal)) return t;
            return null;
        }

        /// <summary>Las tarjetas de un momento, en su orden, ya filtradas por metodologia.</summary>
        public List<TarjetaDef> De(string momento, string metodologiaId = null) {
            return Tarjetas
                .Where(t => string.Equals(t.Momento, momento, StringComparison.Ordinal) && t.AplicaALa(metodologiaId))
                .OrderBy(t => t.Orden)
                .ToList();
        }

        /// <summary>Carga y valida todos los archivos de la carpeta. Si la carpeta no existe, el catalogo queda vacio.</summary>
        public static CatalogoDeTarjetas Cargar(ICatalogSource fuente) {
            var catalogo = new CatalogoDeTarjetas();
            var errores = new List<string>();

            foreach (var ruta in fuente.ListarCatalogos(Carpeta)) {
                ArchivoDeTarjetas archivo;
                try {
                    archivo = JsonConvert.DeserializeObject<ArchivoDeTarjetas>(fuente.LeerCatalogo(ruta));
                } catch (JsonException ex) {
                    errores.Add($"{ruta}: no se pudo leer el JSON. {ex.Message}");
                    continue;
                }
                if (archivo == null || archivo.Tarjetas == null) {
                    errores.Add($"{ruta}: se esperaba {{ \"version\": 1, \"tarjetas\": [ … ] }}.");
                    continue;
                }
                errores.AddRange(Validar(archivo.Tarjetas).Select(m => ruta + ": " + m));
                catalogo.Tarjetas.AddRange(archivo.Tarjetas);
            }

            errores.AddRange(RepetidasEntreArchivos(catalogo.Tarjetas));
            if (errores.Count > 0) throw new SchemaException("Las tarjetas", errores);
            return catalogo;
        }

        public static List<string> Validar(IList<TarjetaDef> tarjetas) {
            var e = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (var t in tarjetas) {
                if (t == null || string.IsNullOrEmpty(t.Id)) { e.Add("Hay una tarjeta sin 'id'."); continue; }
                var donde = $"Tarjeta {t.Id}";
                if (!ids.Add(t.Id)) e.Add($"{donde}: el id esta repetido.");

                if (string.IsNullOrEmpty(t.Personaje)) e.Add($"{donde}: falta 'personaje'. Habla alguien con nombre, no el sistema.");
                if (string.IsNullOrEmpty(t.Frase)) e.Add($"{donde}: falta 'frase'.");
                else if (Palabras(t.Frase) > LimitesDeTarjeta.PalabrasDeFrase)
                    e.Add($"{donde}: la frase tiene {Palabras(t.Frase)} palabras y el maximo es {LimitesDeTarjeta.PalabrasDeFrase}.");
                if (!string.IsNullOrEmpty(t.Ayuda) && Palabras(t.Ayuda) > LimitesDeTarjeta.PalabrasDeAyuda)
                    e.Add($"{donde}: la ayuda tiene {Palabras(t.Ayuda)} palabras y el maximo es {LimitesDeTarjeta.PalabrasDeAyuda}.");
                if (!string.IsNullOrEmpty(t.Pizarra) && !EscenasDePizarra.Existe(t.Pizarra))
                    e.Add($"{donde}: la pizarra '{t.Pizarra}' no existe en EscenasDePizarra.");
                if (string.IsNullOrEmpty(t.Momento)) e.Add($"{donde}: falta 'momento'.");
                if (string.IsNullOrEmpty(t.Origen)) e.Add($"{donde}: falta 'origen'. Sin el no se escribe en la traza.");
                if (string.IsNullOrEmpty(t.Titulo)) e.Add($"{donde}: falta 'titulo'. Es el nombre que sale en el Dashboard.");
                if (string.IsNullOrEmpty(t.Oa)) e.Add($"{donde}: falta 'oa'. Sin el la decision no mide ningun objetivo.");

                var n = t.Opciones == null ? 0 : t.Opciones.Count;
                if (n < LimitesDeTarjeta.MinimoDeOpciones || n > LimitesDeTarjeta.MaximoDeOpciones) {
                    e.Add($"{donde}: tiene {n} opciones y tiene que tener entre " +
                          $"{LimitesDeTarjeta.MinimoDeOpciones} y {LimitesDeTarjeta.MaximoDeOpciones}.");
                    if (n == 0) continue;
                }

                var opciones = new HashSet<string>(StringComparer.Ordinal);
                var hayCorrecta = false;
                foreach (var o in t.Opciones) {
                    if (o == null || string.IsNullOrEmpty(o.Id)) { e.Add($"{donde}: hay una opcion sin 'id'."); continue; }
                    var dondeOpcion = $"{donde} opcion {o.Id}";
                    if (!opciones.Add(o.Id)) e.Add($"{dondeOpcion}: el id esta repetido.");

                    if (string.IsNullOrEmpty(o.Etiqueta)) e.Add($"{dondeOpcion}: falta 'etiqueta'.");
                    else if (Palabras(o.Etiqueta) > LimitesDeTarjeta.PalabrasDeEtiqueta)
                        e.Add($"{dondeOpcion}: la etiqueta tiene {Palabras(o.Etiqueta)} palabras y el maximo es {LimitesDeTarjeta.PalabrasDeEtiqueta}.");

                    if (!Veredictos.EsValido(o.Veredicto)) e.Add($"{dondeOpcion}: el veredicto '{o.Veredicto}' no es correcta, aceptable ni incorrecta.");
                    if (o.Veredicto == Veredictos.Correcta) hayCorrecta = true;
                    if (string.IsNullOrEmpty(o.Razon)) e.Add($"{dondeOpcion}: falta 'razon'. Es lo que se lee despues, en el Dashboard.");

                    ValidarEfectos(o.Efectos, dondeOpcion, e);
                    if (o.Diferidos != null)
                        foreach (var d in o.Diferidos) {
                            if (d == null) continue;
                            if (d.EnDias < 0) e.Add($"{dondeOpcion}: un efecto diferido no puede vencer en el pasado.");
                            ValidarEfectos(d.Efectos, dondeOpcion + " (diferido)", e);
                        }
                }
                if (!hayCorrecta && n >= LimitesDeTarjeta.MinimoDeOpciones)
                    e.Add($"{donde}: ninguna opcion es 'correcta'. Una decision sin respuesta defendible no enseña nada.");
            }
            return e;
        }

        private static void ValidarEfectos(Dictionary<string, object> efectos, string donde, List<string> e) {
            if (efectos == null) return;
            foreach (var clave in efectos.Keys)
                if (!WorldState.EsStock(clave)) e.Add($"{donde}: '{clave}' no es un stock del WorldState.");
            try {
                EffectApplier.Validar(efectos, donde);
            } catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is FormatException) {
                e.Add(ex.Message);
            }
        }

        private static IEnumerable<string> RepetidasEntreArchivos(List<TarjetaDef> todas) {
            return todas.Where(t => t != null && !string.IsNullOrEmpty(t.Id))
                        .GroupBy(t => t.Id, StringComparer.Ordinal)
                        .Where(g => g.Count() > 1)
                        .Select(g => $"Tarjeta {g.Key}: esta en mas de un archivo.");
        }

        /// <summary>Cuenta palabras como lo hace la validacion; publico para que los tests usen la misma vara.</summary>
        public static int Palabras(string texto) {
            return string.IsNullOrWhiteSpace(texto)
                ? 0
                : texto.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
