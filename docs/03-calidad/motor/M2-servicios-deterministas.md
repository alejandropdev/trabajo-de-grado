# M2 · C10a Servicios deterministas

> Subsistema C10 de `nexus-motor-especificacion-tecnica.md` (§4.4.1, §4.4.5, §4.4.6, §4.10.4).
> Estado: **implementado y probado**. 33 tests nuevos, todos en verde.
> Depende de M1.

---

## Qué se hizo

Cuatro archivos nuevos en `Core/Servicios/` y un archivo de tests. **Cero ediciones a código existente.**

| Archivo | Clases | Invariante que blinda |
|---|---|---|
| `Core/Servicios/DeterministicRng.cs` | `DeterministicRng` | **INV-4** · nadie sortea por su cuenta · media **INV-7** |
| `Core/Servicios/EffectApplier.cs` | `EffectApplier` | **INV-1** · ningún evento escribe un flag · **INV-2** · una sola puerta de escritura |
| `Core/Servicios/ConditionEvaluator.cs` | `ConditionEvaluator`, `ExpresionInvalidaException` | un JSON de contenido no puede ejecutar código |
| `Core/Servicios/IStateContext.cs` | `IStateContext` | el puerto que desacopla al director del estado |
| `Tests/editMode/ServiciosDeterministasTests.cs` | 33 tests | — |

Con M2, **el `WorldState` de M1 ya se puede escribir**. Hasta ahora nadie tenía permiso.

---

## Qué hace cada uno

### `DeterministicRng` — el azar que se puede guardar

El problema: un generador no se serializa, pero la partida se guarda a mitad de nivel y al recargar **tiene** que seguir la misma secuencia. La solución de la especificación es contar las tiradas y quemarlas al restaurar:

```
guardar:   semilla 4417, consumos 37
restaurar: generador con semilla 4417, tirar 37 números a la basura
           → el número 38 es el que tocaba
```

Eso solo funciona si **todo sale por un único punto**. Por eso `Next(int)` no tiene camino propio: pasa por `SiguienteDouble()` como todo lo demás. Si tuviera el suyo, contar tiradas dejaría de reconstruir la secuencia, y el bug solo aparecería *después de guardar y recargar* — justo cuando nadie está mirando.

`RuletaPonderada` gasta **exactamente una tirada**, tenga la lista dos elementos o doscientos. Si el coste dependiera del tamaño del catálogo, añadir un evento cambiaría todas las partidas guardadas.

### ⚠ Desviación de la especificación que debes conocer

La spec dice `new Random(4417)`. **No uso `System.Random`.** Implementé SplitMix64 (Steele, Lea y Flood, 2014), quince líneas, período 2⁶⁴.

Por qué: `System.Random` no garantiza la misma secuencia entre runtimes — Microsoft ya cambió el algoritmo una vez. Y aquí el azar sostiene un **instrumento de la tesis**: *«dos personas con la misma semilla juegan exactamente el mismo escenario, y ahí se puede comparar quién decidió mejor»*. Un generador propio da esa garantía en Mono, en IL2CPP y en mi arnés de consola, con la misma semilla.

Beneficio inmediato: puedo verificar los módulos que vienen (selección de eventos, de minijuegos) en `dotnet test` antes de pasártelos, sabiendo que Unity dará exactamente los mismos números. Sin esto, mis comprobaciones previas no valdrían para nada que use azar.

Si prefieres ceñirte a la letra de la spec, se cambia en diez minutos y solo afecta a este archivo.

### `EffectApplier` — la única puerta de escritura

Que esto sea un cuello de botella no es burocracia: es lo que hace que `EstadoAntes` y `EstadoDespues` de la traza sean **ciertos**. Si cualquiera pudiera escribir un stock, la auditoría pedagógica que sostiene la tesis sería una lista de suposiciones.

Los tres formatos de valor:

| En el JSON | Qué hace |
|---|---|
| `"DeudaTecnica": 12` | delta absoluto → `nuevo = actual + 12` |
| `"Cobertura": "-15%"` | sobre el valor actual → `nuevo = actual × 0.85` |
| `"VelocidadMod": "-25%"` | lo mismo, y por eso los porcentajes sobre `VelocidadMod` **componen**: dos `"+10%"` seguidos dan **1.21**, no 1.20 |

`Previsualizar()` (el botón «Estimar impacto» de tu dashboard) tiene dos propiedades que importan:
- **Los deltas son honestos.** Se calculan aplicando sobre un clon, así que ya llevan dentro la acotación: si la cobertura está en 2 y el efecto es −15, el delta que se enseña es **−2**, no −15.
- **Solo enseña el efecto inmediato.** El diferido no se previsualiza nunca, y eso es diseño pedagógico, no un olvido.

### `ConditionEvaluator` — leer precondiciones sin ejecutar código

Descenso recursivo sobre esta gramática:

```
comparacion    := aditiva (('>'|'<'|'>='|'<='|'=='|'!=') aditiva)?
aditiva        := multiplicativa (('+'|'-') multiplicativa)*
multiplicativa := unaria (('*'|'/') unaria)*
unaria         := ('-'|'+') unaria | primaria
primaria       := numero | identificador ['(' cadena ')'] | '(' comparacion ')'
```

**Sin `eval` dinámico**, y eso no es purismo: los JSON de contenido son datos que alguien editará a mano, y un dato nunca debe poder ejecutar código arbitrario. Esta gramática puede leer números y comparar; no puede llamar a nada que no esté en `IStateContext`.

Tres decisiones con consecuencias:

- **Dividir por cero da 0, no excepción.** Una precondición mal escrita no debe poder tumbar el juego en el día 14 de una partida de laboratorio de 90 minutos.
- **Una variable desconocida SÍ lanza.** Es la asimetría deliberada con lo anterior: dividir por cero puede pasar con variables legítimas en tiempo de ejecución; un nombre mal escrito es un bug de contenido, y un bug de contenido se caza al cargar (INV-5), no se disimula durante toda la partida.
- **No hay `&&` ni `||`.** Cada condición va como un elemento de la lista `precondiciones`, y `EvaluarTodas` exige que se cumplan todas. Un JSON con `&&` da error en vez de pasar en silencio.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `new DeterministicRng(semilla)` | `int` | generador en la tirada 0 |
| `DeterministicRng.Restaurar(semilla, consumos)` | 2 `int` del guardado | generador exactamente donde estaba |
| `.NextDouble()` | — | `double` en `[0, 1)` |
| `.Next(max)` | `int > 0` | `int` en `[0, max)` |
| `.RuletaPonderada(pesos)` | lista de pesos | índice elegido, o **−1** si no hay ni un peso positivo |
| `EffectApplier.Aplicar(w, efectos, mult)` | `WorldState` + `Dictionary<string,object>` | nada; escribe el estado ya acotado |
| `EffectApplier.Previsualizar(w, efectos, mult)` | ídem | `Dictionary<string,double>` de **deltas**, sin tocar `w` |
| `EffectApplier.Validar(efectos, contexto)` | efectos + dónde están | nada, o excepción con el contexto dentro |
| `ConditionEvaluator.Evaluar(expr, ctx)` | texto + `IStateContext` | `bool`; expresión vacía = `true` |
| `ConditionEvaluator.EvaluarNumerico(expr, ctx)` | ídem | `double` |

---

## Un hallazgo sobre INV-4

Busqué quién sortea sin `DeterministicRng` y hay **un sitio**: [`Unity/Minijuegos/LienzoGrafoView.cs:113`](../../../game/proyecto%20de%20grado/Assets/Scripts/Unity/Minijuegos/LienzoGrafoView.cs#L113) usa `new System.Random(ctx.Semilla)` para barajar los colores de autor.

**Lo dejé como está**, y creo que es lo correcto: es un barajado puramente cosmético, solo activo con andamiaje 3, vive en la capa de UI y usa su propia instancia, así que no perturba la secuencia del motor. Su autor ya era consciente del tema (lo dice el comentario de la línea 98). Si en M7 quieres que todo el azar pase por el mismo sitio, se migra en cinco minutos.

---

## Cómo probarlo

### Prueba 1 — la automática

1. Unity → `Window > General > Test Runner` → **EditMode** → **Run All**.

**Qué debes ver:** **123 en verde** (eran 90). Los nuevos están bajo `ServiciosDeterministasTests`: deben ser **33/33**.

### Prueba 2 — la manual, para ver las tres invariantes con tus ojos

Crea `Assets/Scripts/Editor/PruebaM2.cs`:

```csharp
using System.Collections.Generic;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using UnityEditor;
using UnityEngine;

public static class PruebaM2 {
    [MenuItem("Nexus/Pruebas/M2 · Azar, efectos y precondiciones")]
    public static void Correr() {
        // --- 1. El azar se puede guardar a mitad y seguir igual ---
        var rng = new DeterministicRng(4417);
        var antes = "";
        for (var i = 0; i < 4; i++) antes += rng.NextDouble().ToString("F4") + "  ";
        var consumos = rng.Consumos;
        var siguiente = rng.NextDouble();

        var recargado = DeterministicRng.Restaurar(4417, consumos);
        Debug.Log($"AZAR  primeras 4: {antes}\n" +
                  $"      la 5a antes de guardar: {siguiente:F4}\n" +
                  $"      la 5a tras recargar:    {recargado.NextDouble():F4}   <- deben ser IGUALES");

        // --- 2. Los efectos: absoluto, porcentaje y composicion ---
        var w = new WorldState();
        w.Set("Cobertura", 50);
        w.Set("DeudaTecnica", 20);
        Debug.Log("EFECTOS  antes:  " + w);

        var opcionB = new Dictionary<string, object> {
            { "DeudaTecnica", 12L }, { "Cobertura", "-15%" }, { "VelocidadMod", "-25%" }
        };

        foreach (var kv in EffectApplier.Previsualizar(w, opcionB))
            Debug.Log($"EFECTOS  previsualizado  {kv.Key}: {kv.Value:+0.00;-0.00}");

        EffectApplier.Aplicar(w, opcionB);
        Debug.Log("EFECTOS  despues: " + w);

        // --- 3. INV-1: un evento no puede tocar un flag narrativo ---
        try {
            EffectApplier.Aplicar(w, new Dictionary<string, object> { { "FLG_DEUDA_MORAL", 3 } });
            Debug.LogError("INV-1 ROTA: deberia haber lanzado");
        } catch (System.InvalidOperationException ex) {
            Debug.Log("INV-1 OK  " + ex.Message);
        }

        // --- 4. Precondiciones ---
        var ctx = new CtxDemo();
        foreach (var expr in new[] {
            "DeudaTecnica > 40", "Cobertura / 0", "diasDesde('EV-NUNCA') > 10", "DeudaTecnica = 45"
        }) {
            try { Debug.Log($"COND  {expr,-32} -> {ConditionEvaluator.Evaluar(expr, ctx)}"); }
            catch (ExpresionInvalidaException ex) { Debug.Log($"COND  {expr,-32} -> RECHAZADA: {ex.Message}"); }
        }
    }

    private sealed class CtxDemo : IStateContext {
        public bool TryGetValue(string n, out double v) {
            v = n == "DeudaTecnica" ? 45 : (n == "Cobertura" ? 60 : 0);
            return n == "DeudaTecnica" || n == "Cobertura";
        }
        public double CallFunction(string n, string a) { return n == "diasDesde" ? 999 : 0; }
    }
}
```

Menú **Nexus > Pruebas > M2 · Azar, efectos y precondiciones**.

**Qué debes ver, y qué significa:**

- **AZAR** — la 5ª tirada antes de guardar y después de recargar son **el mismo número**. Eso es INV-7 funcionando: el jugador no puede esquivar una consecuencia recargando la partida.
- **EFECTOS previsualizado** — tres líneas con el impacto de cada stock. `Cobertura: -7.50` (el 15 % de 50), `DeudaTecnica: +12.00`, `VelocidadMod: -0.25`. Es lo que enseñará el botón «Estimar impacto».
- **EFECTOS antes/después** — compara las dos líneas: `Cobertura` pasa de 50.0 a 42.5, `DeudaTecnica` de 20.0 a 32.0, `VelocidadMod` de 1.00 a 0.75. Y nada más se movió.
- **INV-1 OK** — el mensaje explica por qué se rechaza. Prueba a quitar el `try/catch` y verás que el juego se niega en el acto.
- **COND** — cuatro líneas: `DeudaTecnica > 40` → `True`. `Cobertura / 0` → `False` (dio 0, **no** reventó). `diasDesde('EV-NUNCA') > 10` → `True` (999 días desde algo que nunca pasó). `DeudaTecnica = 45` → **RECHAZADA**, con un mensaje que te dice que uses `==`.

Cuando termines, **borra `PruebaM2.cs`**.

---

## Lo que falta para que esto se use de verdad

- **Nadie implementa `IStateContext` todavía.** Lo hará `GameSession` en M9; hasta entonces solo existen los dobles de los tests.
- **`EffectApplier` no encola nada.** Los efectos diferidos (`"enDias": 5`) necesitan `EffectScheduler`, que es **M3**, el siguiente.
- **`SchemaValidator` aún no traduce** las `InvalidOperationException` y `ExpresionInvalidaException` a errores de carga con archivo y campo. Eso es **M6**.
