# M4 · C5 Reglas de Metodología

> Subsistema C5 de `nexus-motor-especificacion-tecnica.md` (§4.5).
> Estado: **implementado y probado**. 31 tests nuevos, todos en verde.
> Depende de M1.

---

## Qué se hizo

| Archivo | Clases |
|---|---|
| `Core/Metodologia/MethodologyProfile.cs` | `MethodologyProfile` + los 8 bloques (`Calendario`, `Etapa`, `Ceremonia`, `AccionRetro`, `ReglasDeCambio`, `PenalizacionFueraDeVentana`, `ModificadoresDirector`, `LanzamientoConfig`, `RubricaCierre`, `PracticaEsperada`) + 5 vocabularios cerrados |
| `Core/Metodologia/DayPlan.cs` | `DayPlan`, `VeredictoCambio` |
| `Core/Metodologia/MethodologyRules.cs` | `MethodologyRules`, `Tramo` |
| `Tests/editMode/ReglasDeMetodologiaTests.cs` | 31 tests |

**Cero ediciones a código existente.**

---

## La propiedad que importa

**Ni un solo `if (id == "scrum")` en ningún sitio.** Todo lo que diferencia a Scrum de Cascada está en los ocho bloques del JSON. Si algún día hace falta escribir aquí el nombre de una metodología, es que falta un campo en el perfil.

Eso no es un principio decorativo: hay un test, `Una_metodologia_nueva_entra_solo_con_un_json`, que se **inventa Espiral** (4 ciclos de 5 días, análisis de riesgo al inicio de cada ciclo, ventana entre iteraciones) sin tocar una línea de motor, y comprueba que trocea, agenda ceremonias y resuelve cambios de alcance correctamente. Si hubiera un solo `if` con un nombre dentro, ese test fallaría.

## Qué hacen los ocho bloques

| # | Bloque | Reescribe |
|---|---|---|
| 1 | `Calendario` | cómo se trocea el nivel: `iterativo` (Scrum, XP) · `secuencial` (Cascada, RUP) · `continuo` (Kanban) |
| 2 | `Ceremonias` | qué cae cada día y qué cuesta en días de proyecto |
| 3 | `ReglasDeCambio` | cuándo se admite un cambio de alcance y qué pasa si no |
| 4 | `ModificadoresModelo` | multiplica α β γ δ ι κ y `velocidadBase` |
| 5 | `ModificadoresDirector` | pesos por tag, presupuesto de drama, eventos bloqueados |
| 6 | `TableroPrincipal` | `burndown` · `curvaS` · `cfd` |
| 7 | `Lanzamiento` | factor de riesgo, entrega incremental, si el cliente ya vio el producto |
| 8 | `RubricaCierre` | con qué se te juzga en la Fase 4 |

### Las tres respuestas a la misma pregunta

Lo que mejor enseña el bloque es `EvaluarCambioDeAlcance`. El cliente quiere cambiar algo el **día 7**:

| Metodología | Respuesta | En datos |
|---|---|---|
| **Kanban** | *«Entra, pero algo tiene que salir del tablero.»* | `ventanas: [siempre]`, `consumeWip: true` |
| **Scrum** | *«Se puede, pero romper el sprint se paga.»* — permitido al **doble de coste** y −4 de moral. El día 10, en cambio, entra gratis | `ventanas: [entreIteraciones]`, penalización ×2 |
| **Cascada** | *«El documento está firmado. Fuera de un hito, no.»* — directamente **no permitido**. El día 6, que es hito, sí, y aun así cuesta el triple | `ventanas: [hito]`, `permitido: false` |

Ninguna de esas tres frases está en el código.

### `AccionRetro` — la mecánica más rara del juego

`{ coeficiente: "kappa", multiplicador: 0.85 }` se traduce en `Coeficientes.MultiplicarUno("kappa", 0.85)`, y a partir de ese día **la documentación se diluye más despacio. Para siempre, y sin que la pantalla lo presuma.** Es la única mecánica en la que el jugador modifica literalmente una constante de su propio proceso.

---

## Tres decisiones donde la spec no llegaba

**1 · El constructor valida el perfil, no solo `SchemaValidator`.**
La spec asigna esa validación a M6. La puse aquí *además* porque construir las reglas es lo que de verdad ejercita el perfil: al trocear el calendario se descubre que las etapas no tienen días, que la retro no ofrece acciones, que `kappa` está escrito `kapa`. Así M6 podrá limitarse a llamar a este constructor y traducir la excepción, en vez de duplicar las reglas. Una sola fuente de verdad.

Lo que rechaza, con el mensaje diciendo qué: familia o tablero fuera de vocabulario · calendario desconocido · secuencial sin etapas · iterativo sin iteraciones · ceremonia con `cuando` inventado · retro que ajusta coeficiente sin ofrecer acciones · acción con un coeficiente que no existe · ventana de cambio inventada · **rúbrica de cierre vacía** («sin ella la Fase 4 se queda muda»).

**2 · El último tramo se estira, y los que se pasan se recortan.**
La spec solo menciona lo primero. Añadí lo segundo: si un perfil declara 5 iteraciones de 10 días en un nivel de 20, no se crean unidades para días que no existen. Hay un test que comprueba la propiedad completa — *cada día del nivel pertenece a exactamente una unidad* — para las tres metodologías.

**3 · Una ceremonia con `abreVentanaDeCambio` manda sobre el calendario.**
Sin esto, ese campo del JSON no significaría nada. Ahora el planning de Scrum abre la ventana aunque las reglas del calendario estén vacías.

## Un tipo duplicado a propósito

`PracticaEsperada` (aquí) y `PracticaAEvaluar` (en `Core/Evaluacion/MethodologyReport.cs`) tienen los mismos siete campos. **No es un descuido.** El comentario que ya estaba en el código dice: *«Lo construye GameSession a partir del MethodologyProfile (C5), para que este paquete no dependa de Core.Method»*. Respeté ese diseño: `Core.Evaluacion` no conoce `Core.Metodologia`, y M9 escribirá las diez líneas de mapeo al cerrar el nivel.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `new MethodologyRules(perfil, díasTotales)` | perfil + días | reglas listas; **lanza** si el perfil no se sostiene |
| `.PlanFor(día)` | día | `DayPlan`: unidad, ceremonias de hoy, multiplicadores de etapa, efectos por día |
| `.EvaluarCambioDeAlcance(día)` | día | `VeredictoCambio`: permitido, en ventana, coste, consume WIP, efectos extra, texto |
| `.MultiplicadorTag(tag, plan)` | tag + plan | `double`, en cascada: perfil × etapa |
| `.EventoBloqueado(id, soloMetodologías)` | id + lista | `bool`; lista vacía = vale para todas |
| `.CeremoniaPorId(id)` | id | `Ceremonia` o `null` |
| `.NumeroDeUnidades`, `.MultiplicadorDrama`, `.Tramos` | — | el troceado del nivel |

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 174 en verde** (eran 143). `ReglasDeMetodologiaTests` debe dar **31/31**.

### Prueba 2 — la manual: las tres metodologías, lado a lado

Crea `Assets/Scripts/Editor/PruebaM4.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Metodologia;
using UnityEditor;
using UnityEngine;

public static class PruebaM4 {
    private const int Dias = 20;

    [MenuItem("Nexus/Pruebas/M4 · Veinte dias con tres metodologias")]
    public static void Correr() {
        foreach (var perfil in new[] { Scrum(), Cascada(), Kanban() }) {
            var r = new MethodologyRules(perfil, Dias);
            var texto = $"===== {perfil.Nombre.ToUpper()}  ({r.NumeroDeUnidades} unidades, tablero {perfil.TableroPrincipal}) =====\n";

            for (var dia = 1; dia <= Dias; dia++) {
                var plan = r.PlanFor(dia);
                var cambio = r.EvaluarCambioDeAlcance(dia);

                var ceremonias = plan.Ceremonias.Count == 0 ? "—" : string.Join(", ", plan.Ceremonias.Select(c => c.Id));
                var veredicto = !cambio.Permitido ? "CAMBIO: NO"
                              : cambio.EnVentana ? $"CAMBIO: si (x{cambio.CosteMultiplicador:0.#})"
                                                 : $"CAMBIO: fuera de ventana (x{cambio.CosteMultiplicador:0.#})";

                texto += $"dia {dia,2} | {plan.UnidadId,-13} {plan.DiaDentroDeUnidad,2}/u | {ceremonias,-24} | {veredicto}\n";
            }
            Debug.Log(texto);
        }
    }

    private static RubricaCierre Rubrica() {
        return new RubricaCierre { Practicas = { new PracticaEsperada {
            Id = "ritmo", Metrica = "diasConHorasExtra", Comparador = "<=", Objetivo = 4 } } };
    }

    private static MethodologyProfile Scrum() {
        return new MethodologyProfile {
            Id = "scrum", Nombre = "Scrum", Familia = FamiliasDeMetodologia.Agil,
            Calendario = new Calendario { Tipo = TiposDeCalendario.Iterativo, EtiquetaUnidad = "Sprint",
                                          LongitudIteracion = 10, Iteraciones = 2 },
            Ceremonias = {
                new Ceremonia { Id = "daily", Cuando = CuandoAplica.Diario },
                new Ceremonia { Id = "planning", Cuando = CuandoAplica.InicioIteracion, AbreVentanaDeCambio = true },
                new Ceremonia { Id = "retro", Cuando = CuandoAplica.FinIteracion, AjustaCoeficiente = true,
                                Acciones = { new AccionRetro { Id = "documentar", Coeficiente = "kappa", Multiplicador = 0.85 } } }
            },
            ReglasDeCambio = new ReglasDeCambio {
                Ventanas = new List<string> { VentanasDeCambio.EntreIteraciones },
                PenalizacionFueraDeVentana = new PenalizacionFueraDeVentana { Permitido = true, CosteMultiplicador = 2.0 }
            },
            TableroPrincipal = TablerosPrincipales.Burndown, RubricaCierre = Rubrica()
        };
    }

    private static MethodologyProfile Cascada() {
        return new MethodologyProfile {
            Id = "cascada", Nombre = "Cascada", Familia = FamiliasDeMetodologia.Tradicional,
            Calendario = new Calendario { Tipo = TiposDeCalendario.Secuencial, EtiquetaUnidad = "Etapa",
                Etapas = {
                    new Etapa { Id = "analisis", Dias = 6, MultiplicadorPesosPorTag = { { "alcance", 2.0 } } },
                    new Etapa { Id = "diseno", Dias = 5 },
                    new Etapa { Id = "construccion", Dias = 6 },
                    new Etapa { Id = "pruebas", Dias = 3, MultiplicadorPesosPorTag = { { "calidad", 2.5 } } }
                } },
            Ceremonias = { new Ceremonia { Id = "hito", Cuando = CuandoAplica.FinEtapa, CosteDias = 1.0 } },
            ReglasDeCambio = new ReglasDeCambio {
                CosteMultiplicador = 3.0, Ventanas = new List<string> { VentanasDeCambio.Hito },
                PenalizacionFueraDeVentana = new PenalizacionFueraDeVentana { Permitido = false }
            },
            TableroPrincipal = TablerosPrincipales.CurvaS, RubricaCierre = Rubrica()
        };
    }

    private static MethodologyProfile Kanban() {
        return new MethodologyProfile {
            Id = "kanban", Nombre = "Kanban", Familia = FamiliasDeMetodologia.Agil,
            Calendario = new Calendario { Tipo = TiposDeCalendario.Continuo, EtiquetaUnidad = "Flujo",
                                          LimiteWipInicial = 4 },
            Ceremonias = { new Ceremonia { Id = "flujo", Cuando = CuandoAplica.Cada, CadaNDias = 5 } },
            ReglasDeCambio = new ReglasDeCambio {
                Ventanas = new List<string> { VentanasDeCambio.Siempre }, ConsumeWip = true },
            TableroPrincipal = TablerosPrincipales.Cfd, RubricaCierre = Rubrica()
        };
    }
}
```

Menú **Nexus > Pruebas > M4 · Veinte días con tres metodologías**. Salen **tres bloques** en consola.

**Qué debes ver, y qué significa:**

- **Scrum** — dos unidades, `it1` (días 1–10) e `it2` (11–20). `daily` todos los días; `planning` los días 1 y 11; `retro` los días 10 y 20. La columna de cambio dice `si (x1)` los días **1, 10, 11 y 20** y `fuera de ventana (x2)` el resto. **Ese patrón es Scrum entero.**
- **Cascada** — cuatro unidades de distinta longitud: `analisis` 1–6, `diseno` 7–11, `construccion` 12–17, `pruebas` 18–20. El `hito` cae los días 6, 11, 17 y 20. La columna de cambio dice `CAMBIO: NO` en **16 de los 20 días**, y `si (x3)` solo en los cuatro hitos.
- **Kanban** — una sola unidad, `flujo`, los 20 días. `flujo` (revisión) cada 5 días. La columna de cambio dice `si (x1)` **siempre**.

Compara las tres columnas de la derecha. Esa es toda la diferencia pedagógica entre las tres metodologías, y sale de tres JSON.

Cuando termines, **borra `PruebaM4.cs`**.

---

## Lo que falta

- **`ModificadoresModelo` no se aplica todavía.** Lo hará `GameSession.ElegirMetodologia` (M9) con `Coeficientes.MultiplicarPor`.
- **`MultiplicadorTag` no lo consume nadie.** Es el `EventDirector` (**M5**, el siguiente) quien lo multiplicará en cascada sobre los `pesosPorTag` del nivel.
- **El mapeo `PracticaEsperada` → `PracticaAEvaluar`** lo escribe M9 al cerrar el nivel.
- **`cuandoSeLiberaWip`** está definido pero nunca aplica desde el calendario: lo disparará el flujo continuo de Kanban en M9.
