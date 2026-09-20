# M8 · C7 Canal Narrativo

> Subsistema C7 de `nexus-motor-especificacion-tecnica.md` (§4.7, §4.9.4, §8.3.5, §8.3.6).
> Estado: **implementado y probado**. 34 tests nuevos, todos en verde.
> Depende de M1, M2 y M6.

---

## Qué se hizo

| Archivo | Clases |
|---|---|
| `Core/Narrativa/FlagStore.cs` | `FlagStore`, `DefinicionDeFlag`, `EjesDeFlag` |
| `Core/Narrativa/NarrativeBeat.cs` | `NarrativeBeat`, `VentanaDeBeat`, `BeatDeHoy`, `DecisionNarrativa`, `PrioridadDeBeat` |
| `Core/Narrativa/NarrativeDirector.cs` | `NarrativeDirector`, `PuenteDeFlags` |
| `Tests/editMode/CanalNarrativoTests.cs` | 34 tests |

**Ediciones a código existente: dos**, las mismas de M7 y por el mismo motivo. `CatalogLoader` gana `CargarNarrativa` y `CargarFlags`; `SchemaValidator` gana `ValidarNarrativa` y `ValidarFlags`. Sus tests siguen en verde.

---

## Las dos invariantes

### 1 · `FLG_DEUDA_MORAL` no baja nunca

No es una regla de balanceo. Es **la tesis del juego**: la deuda técnica se paga refactorizando, la deuda moral no se paga — solo se declara o se oculta.

Por eso está blindada por tres sitios: `Sumar()` ignora cualquier delta negativo, `Set()` tampoco la deja bajar, y el **validador exige** que el censo la declare `noBaja` (si no, el catálogo no carga, con el mensaje *«es la tesis del juego, no un parámetro de balanceo»*).

La única puerta que se salta las reglas es **el constructor**, y es deliberado: restaurar una partida guardada tiene que poder reponer cualquier valor, incluido uno al que hoy ya no se podría llegar sumando. La spec ya lo marcaba: *«por constructor A PROPÓSITO»*.

**Un detalle de implementación que importa:** el diccionario que recibe el constructor es el **respaldo**, y se escribe a través de él — no se copia. Así `SaveGame.Flags` está siempre al día sin un paso de sincronización que alguien pueda olvidar.

### 2 · El director solo puede RETRASAR, ADELANTAR y COLOREAR

Nunca **CREAR** un beat que no esté en el catálogo, ni **CANCELAR** uno que sí esté. Por eso el árbol de los catorce finales es auditable: la trama está escrita en `narrativa.json` y el motor solo decide cuándo suena cada pieza.

Y es **determinista al 100 %**: aquí no entra el azar. Dos partidas con las mismas decisiones cuentan la misma historia aunque tengan semillas distintas — *el azar decide qué crisis te toca, no quién eres*.

---

## Los tres puntos que la spec dejaba abiertos (§4.7.3)

La especificación marcaba tres `⚠ PENDIENTE`. Esto es lo que decidí y por qué:

**(a) Cómo se evalúan las expresiones de coloreo.**
Con `ConditionEvaluator`, como la propia spec recomendaba. Gana además que el validador las comprueba al cargar: **una condición de coloreo mal escrita no daría error, daría una escena que siempre suena igual** — y eso no se nota jugando, solo leyendo el JSON con lupa. Ahora salta al arrancar.

**(b) Qué ocurre si un beat `obligatorio` no cumple precondiciones al llegar `diaMax`.**
Aquí hice una separación semántica que creo que resuelve el problema:

> **La ventana es ritmo. Las precondiciones son coherencia.**

- Para un **obligatorio**, la ventana es una *preferencia*: si el día llega y las precondiciones aún no se cumplen, el beat **espera**. Mejor tarde que no contar la escena. Sale marcado como `Retrasado`.
- Las **precondiciones no se saltan jamás**, ni para un obligatorio. Disparar *«El Cuarto»* a quien nunca se fue a casa no sería narrativa, sería un fallo.
- Y si al cerrar el nivel un obligatorio nunca llegó a salir, `ObligatoriosQueNoSalieron()` **deja constancia en el log**. Un beat obligatorio inalcanzable es un bug de contenido, y sin esto no daría la cara nunca.

**(c) Si los beats pueden encadenarse.**
**No.** Un beat no puede disparar otro. Mantiene el canal determinista y testeable, y las cadenas narrativas ya se expresan con precondiciones sobre flags.

## Dos decisiones más

**Como máximo un beat al día.** Un beat es una cinemática de 60–90 s; encadenar dos el mismo día convertiría la narrativa en una interrupción del juego en vez de en parte de él. Los que quedan fuera siguen disponibles mañana. Orden: obligatorios antes que opcionales, y dentro de cada grupo el de ventana más temprana.

**Los beats emitidos viajan en `RuntimeState.Ocurrencias`** con el prefijo `CIN:`. No hizo falta añadir un campo al guardado: `Ocurrencias` ya viaja, y así un beat no se repite tras recargar.

## El puente entre los dos canales (§4.9.4)

> **INV-6: ocurre UNA vez, dentro de `Cerrar()`, y en ningún otro sitio.**

Es el momento en que el proyecto se convierte en biografía: los stocks del nivel, que se van a tirar, dejan su huella en los flags de la partida, que no se tiran nunca.

| Snapshot (`Set`) | Contador (`Sumar`) |
|---|---|
| `FLG_DEUDA_TECNICA`, `FLG_MORAL_EQUIPO`, `FLG_SALUD`, `FLG_CALIDAD_ACUM`, `FLG_REPUTACION` | `FLG_HORAS_EXTRA`, `FLG_VIDA_EXTERNA` |

**Desviación de la letra del §4.9.4:** la spec escribe los siete con `=`. Pero dos son contadores acumulativos — el Dashboard del §10.3 enseña *«horas extra acumuladas: 412»*, y eso son ocho niveles sumando, no el último sobrescribiendo a los siete anteriores. Esos dos usan `Sumar()`. Hay un test que lo fija.

---

## Las siete deudas documentales

El propio `nexus-inventario-variables.md` §D se detecta estos siete problemas. **Ninguno afecta al código de M8 ni al contenido de N0/N1** — todos viven en los flags de N4–N8. Los listo para que decidas cuándo escribamos ese contenido, y señalo lo que yo haría:

| # | Problema | Lo que yo haría |
|---|---|---|
| 1 | `FLG_VOSS` y `FLG_VOSS_AFINIDAD` son el mismo flag con dos nombres (ídem Sato y Okafor) | Unificar a `FLG_VOSS_AFINIDAD`. Es un renombrado, sin discusión |
| 2 | `FLG_CALIDAD_ACUM` solapa con la derivada `CalidadEntregada` | Mantenerlo como **snapshot al cerrar** (es lo que implementé). La derivada es "hoy", el flag es "cómo terminó ese nivel" |
| 3 | **Siete flags se escriben y nunca se leen**: `FLG_VIO_PANTALLA_DORADA`, `FLG_ZERO_INVESTIGA`, `FLG_INTENTO_OFICIAL`, `FLG_LEYO_NDA`, `FLG_VIO_COMMIT_SOSPECHOSO`, `FLG_ORIGEN`, `FLG_PERFIL_TECNICO` | **Tu decisión.** Dos de ellos (`FLG_ORIGEN`, `FLG_PERFIL_TECNICO`) se escriben en N0 y la Biblia dice que colorean diálogos más adelante: esos yo les daría lector. Los otros cinco, o les das lector o los retiras |
| 4 | `FLG_OSCAR_PROTEGIDO` **se lee y nunca se escribe** (decide si Óscar aparece en la lista de 340 despidos del N6) | **Tu decisión, y es la más narrativa de las siete.** Hay que elegir qué acción del jugador lo activa. Óscar es el ancla emocional del juego |
| 5 | **`FLG_SALUD` tiene tres umbrales contradictorios** para el mismo final F11: `<20` (§2.2), `≤10` (§3.4), `≤5` (§7.4) | **Tu decisión.** Yo iría a `≤10`: con `≤5` el burnout casi nunca se alcanza y el final sobra; con `<20` se dispara por accidente y deja de ser una advertencia |
| 6 | La convención de nombres del anexo §10.2 no la usa ningún flag real | Retirar el anexo. Los nombres actuales son consistentes entre sí |
| 7 | Los snapshots `_N1` (`FLG_DEUDA_TECNICA_N1`, `FLG_HORAS_EXTRA_N1`) no tienen semántica definida | **Tu decisión.** O son una serie indexada por nivel (y entonces conviene un solo flag con sufijo sistemático) o sobran, porque `FLG_DEUDA_TECNICA` ya guarda el último |

Las cuatro que marco como tuyas (#3, #4, #5, #7) son decisiones de diseño narrativo, no técnicas. **No corre prisa**: se resuelven cuando escribamos el contenido de N4–N8, que está fuera del alcance acordado. Si prefieres fijarlas ahora, dímelo y las llevo al `flags.json` de B1.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `new FlagStore(respaldo, censo)` | el dict de `SaveGame.Flags` + el censo | store que **escribe a través** del respaldo |
| `.Get/.Set/.Sumar/.Tiene` | nombre `FLG_*` | valor; **lanza** si no empieza por `FLG_` o no está en el censo |
| `.Inicializar()` | — | pone los iniciales que falten, sin pisar los que hay |
| `new NarrativeDirector(catálogo)` | los beats | director; **lanza** si hay ids repetidos |
| `.BeatDeHoy(r, ctx)` | estado + puerto | **un** `BeatDeHoy` (id, nombre, variante, retrasado) o `null` |
| `.ObligatoriosQueNoSalieron(r)` | estado | los ids que nunca pudieron salir, y los anota en el log |
| `PuenteDeFlags.VolcarAlCerrar(w, r, flags)` | los dos estados + los flags | 5 snapshots + 2 contadores |

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 316 en verde** (eran 282). `CanalNarrativoTests` debe dar **34/34**.

### Prueba 2 — la manual: la deuda que no se paga

Crea `Assets/Scripts/Editor/PruebaM8.cs`:

```csharp
using System.Collections.Generic;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Servicios;
using UnityEditor;
using UnityEngine;

public static class PruebaM8 {
    private sealed class Ctx : IStateContext {
        public readonly WorldState W = new WorldState();
        public readonly RuntimeState R = new RuntimeState { Fase = 2 };
        public bool TryGetValue(string n, out double v) { return W.TryGet(n, out v) || R.TryGet(n, out v); }
        public double CallFunction(string n, string a) { return 0; }
    }

    [MenuItem("Nexus/Pruebas/M8 · La deuda que no se paga")]
    public static void Correr() {
        // --- 1. Las dos deudas, lado a lado ---
        var respaldo = new Dictionary<string, double>();
        var flags = new FlagStore(respaldo, Censo());
        var w = new WorldState();

        Debug.Log("--- 1. LAS DOS DEUDAS ---");
        w.Set("DeudaTecnica", 40);
        flags.Sumar(FlagStore.DeudaMoral, 6);
        Debug.Log($"tras los atajos:   tecnica {w.DeudaTecnica:F0}   moral {flags.Get(FlagStore.DeudaMoral):F0}");

        w.Set("DeudaTecnica", w.DeudaTecnica - 25);      // refactorizas
        flags.Sumar(FlagStore.DeudaMoral, -25);          // ...y pides perdon
        Debug.Log($"tras refactorizar: tecnica {w.DeudaTecnica:F0}   moral {flags.Get(FlagStore.DeudaMoral):F0}" +
                  "   <- la moral NO se movio");

        // --- 2. El canal narrativo a lo largo de 20 dias ---
        Debug.Log("--- 2. VEINTE DIAS DE NARRATIVA ---");
        var ctx = new Ctx();
        var director = new NarrativeDirector(Beats());

        for (var dia = 1; dia <= 20; dia++) {
            ctx.R.DiaActual = dia;
            if (dia == 6) ctx.R.VecesQueSeFueACasa = 3;      // el jugador se fue a casa tres noches
            if (dia == 12) ctx.W.Set("MoralEquipo", 30);     // y el equipo se hunde

            var beat = director.BeatDeHoy(ctx.R, ctx);
            if (beat != null)
                Debug.Log($"DIA {dia,2}  >>> {beat.BeatId} \"{beat.Nombre}\" en tono {beat.Variante}" +
                          (beat.Retrasado ? "  (RETRASADO)" : ""));
        }

        foreach (var perdido in director.ObligatoriosQueNoSalieron(ctx.R))
            Debug.LogWarning($"Obligatorio que nunca salio: {perdido}");

        // --- 3. El volcado al cerrar ---
        Debug.Log("--- 3. EL VOLCADO AL CERRAR (INV-6) ---");
        ctx.W.Set("DeudaTecnica", 47);
        ctx.W.Set("Cobertura", 50);
        ctx.W.Set("Documentacion", 30);
        ctx.R.DiasConHorasExtra = 12;
        ctx.R.VecesQueSeFueACasa = 2;

        PuenteDeFlags.VolcarAlCerrar(ctx.W, ctx.R, flags);
        foreach (var kv in flags.Todos) Debug.Log($"   {kv.Key} = {kv.Value:F0}");

        // y un segundo nivel, para ver que acumula
        ctx.W.Set("DeudaTecnica", 8);
        ctx.R.DiasConHorasExtra = 9;
        PuenteDeFlags.VolcarAlCerrar(ctx.W, ctx.R, flags);
        Debug.Log($"tras el segundo nivel:  DEUDA_TECNICA = {flags.Get("FLG_DEUDA_TECNICA"):F0} (snapshot)  " +
                  $"HORAS_EXTRA = {flags.Get("FLG_HORAS_EXTRA"):F0} (acumulado)");
    }

    private static List<DefinicionDeFlag> Censo() {
        return new List<DefinicionDeFlag> {
            F("FLG_DEUDA_MORAL", EjesDeFlag.Integridad, 0, 0, 20, noBaja: true),
            F("FLG_DEUDA_TECNICA", EjesDeFlag.Competencia, 0, 0, 100),
            F("FLG_MORAL_EQUIPO", EjesDeFlag.Relaciones, 60, 0, 100),
            F("FLG_SALUD", EjesDeFlag.Persona, 80, 0, 100),
            F("FLG_VIDA_EXTERNA", EjesDeFlag.Persona, 0, 0, 10),
            F("FLG_CALIDAD_ACUM", EjesDeFlag.Competencia, 0, 0, 100),
            F("FLG_REPUTACION", EjesDeFlag.Estado, 50, 0, 100),
            new DefinicionDeFlag { Id = "FLG_HORAS_EXTRA", Eje = EjesDeFlag.Estado, Inicial = 0,
                                   SinTecho = true, Descripcion = "Contador acumulado." }
        };
    }

    private static DefinicionDeFlag F(string id, string eje, double ini, double min, double max, bool noBaja = false) {
        return new DefinicionDeFlag { Id = id, Eje = eje, Inicial = ini, Min = min, Max = max,
                                      NoBaja = noBaja, Descripcion = id };
    }

    private static List<NarrativeBeat> Beats() {
        var cafetera = new NarrativeBeat {
            Id = "CIN-2.3", Nombre = "La Cafetera", Prioridad = PrioridadDeBeat.Opcional,
            Ventana = new VentanaDeBeat { DiaMin = 8, DiaMax = 16 },
            Precondiciones = { "vecesQueSeFueACasa >= 3" },
            Coloreo = { { "MoralEquipo < 40", "variante_fria" }, { "default", "variante_base" } }
        };
        var glitch = new NarrativeBeat {
            Id = "CIN-1.4", Nombre = "El Primer Glitch", Prioridad = PrioridadDeBeat.Obligatorio,
            Ventana = new VentanaDeBeat { DiaMin = 18, DiaMax = 20 }
        };
        var cuarto = new NarrativeBeat {
            Id = "INT-1", Nombre = "El Cuarto", Prioridad = PrioridadDeBeat.Obligatorio,
            Ventana = new VentanaDeBeat { DiaMin = 3, DiaMax = 5 },
            Precondiciones = { "vecesQueSeFueACasa >= 3" }
        };
        return new List<NarrativeBeat> { cafetera, glitch, cuarto };
    }
}
```

Menú **Nexus > Pruebas > M8 · La deuda que no se paga**.

**Qué debes ver, y qué significa:**

1. **Las dos deudas.** Refactorizas y la técnica baja de 40 a 15. Pides perdón y **la moral se queda en 6**. Esa línea es el juego entero.
2. **Los veinte días.** `INT-1` («El Cuarto») es obligatorio con ventana 3–5, pero su precondición no se cumple hasta el día 6 → **sale el día 6 marcado `(RETRASADO)`**. Esa es la decisión (b): la ventana es ritmo, las precondiciones son coherencia. `CIN-2.3` («La Cafetera») sale el día 8 en `variante_base`; si mueves la caída de moral al día 10, sale en `variante_fria`. Y `CIN-1.4` cae el día 18, sin precondiciones.
3. **El volcado.** Tras el primer nivel, `FLG_HORAS_EXTRA = 12`. Tras el segundo, **`FLG_DEUDA_TECNICA` vale 8** (snapshot del último) pero **`FLG_HORAS_EXTRA` vale 21** (acumulado). Esa diferencia es la desviación del §4.9.4 que expliqué arriba.

Prueba a quitar `if (dia == 6) ctx.R.VecesQueSeFueACasa = 3;`: `INT-1` no sale nunca y aparece el `LogWarning` de obligatorio perdido. Así es como se caza una trama con un agujero.

Cuando termines, **borra `PruebaM8.cs`**.

---

## Lo que falta

- **`narrativa/narrativa.json` y `flags.json` no existen en disco.** Se escriben en B1, con los beats de N0 y N1 (`CIN-0.0` «El Ascensor», `CIN-1.0` «El Té», `CIN-1.2` «El Standup de los Fantasmas», `CIN-1.4` «El Primer Glitch», `INT-1` «El Cuarto») y el censo de flags.
- **Nadie llama a `BeatDeHoy` ni a `VolcarAlCerrar`.** Lo hará `GameSession` en M9 — y el volcado **solo** desde `Cerrar()`, que es lo que hace cierto INV-6.
- **Los hallazgos de los minijuegos siguen sin traducirse a flags.** El puente `Hallazgos → FLG_*` lo escribe M9 con la tabla del contenido.

---

**Con M8 cierra la Fase A menos una pieza.** Quedan los nueve subsistemas montados y sin nadie que los una: eso es **M9 · `GameSession`**, el hub del que cuelgan todos.
