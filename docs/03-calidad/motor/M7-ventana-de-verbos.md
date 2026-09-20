# M7 · C6 Ventana de Verbos

> Subsistema C6 de `nexus-motor-especificacion-tecnica.md` (§4.6, §6.1, §7.5).
> Estado: **implementado y probado**. 31 tests nuevos, todos en verde.
> Depende de M1, M2, M3, M5 y M6.

---

## Qué se hizo

| Archivo | Clases |
|---|---|
| `Core/Minijuegos/MinigameDefinition.cs` | `MinigameDefinition`, `PendingMinigame`, `Verbos`, `ResultadosDeMinijuego`, `DecisionDeMinijuego` |
| `Core/Minijuegos/MinigameDirector.cs` | `MinigameDirector` |
| `Core/Minijuegos/PuenteDelMotor.cs` | el puente entre la escena y el motor |
| `Tests/editMode/VentanaDeVerbosTests.cs` | 31 tests |

**Ediciones a código existente: dos, y estaban anunciadas.** El minidoc de M6 decía que `CargarMinijuegos` llegaría aquí. `CatalogLoader` gana `Catalogo.Minijuegos` y la carga del índice; `SchemaValidator` gana `ValidarMinijuegos`. Sus 46 tests siguen pasando: `ValidarCatalogo` recibió un parámetro **opcional**, así que ninguna llamada anterior cambió.

---

## La decisión de diseño que define el módulo

La especificación proponía un `MinigameDefinition` con un `Dictionary<string,object> Parametros` para el bloque de cada verbo, **y ella misma marcaba el problema**: renuncia al tipado, y sugería seis subclases con deserialización polimórfica.

Antes de escribir nada miré el contenido que ya existe. `MJ-F2-02.json` tiene `id`, `verbo`, `lienzo`, `objetivoAprendizaje`, `presentacion`, `artefacto`, `zonas`, `senuelos`, `paletaEtiquetas`, `andamiaje`, `consecuencias`, `cierre` — y **no tiene** `pesoBase`, `enfriamiento` ni `precondiciones`, que es justo lo que el director necesita para elegir.

Eso apunta a la respuesta, y es la propia invariante del §4.6:

> **La mecánica vive en la UI; el motor solo sabe cuál toca.**

Así que son **dos vistas del mismo minijuego**, no una:

| | Qué contiene | Quién la lee | Dónde vive |
|---|---|---|---|
| **`MinigameDefinition`** (nueva) | id, verbo, **archivo**, OA, presión diegética, reloj, peso, enfriamiento, precondiciones, fases | el **motor**, para elegir | `minijuegos/indice.json` |
| **`MinijuegoDef`** (ya existía) | zonas, señuelos, commits, diffs, andamiaje, consecuencias | la **escena**, para jugar | `minijuegos/MJ-*.json` |

El campo `archivo` es el puente. Y el problema del tipado desaparece solo: cada verbo ya tiene su clase tipada en la capa que lo juega, y el motor se queda con diez campos que valen igual para los seis.

**El riesgo de tener dos vistas es que se contradigan**, así que el validador las cruza: comprueba que el `archivo` existe, parsea como escena, y que su `id`, `verbo` y `objetivoAprendizaje` coinciden con lo que el índice dice de él. Un desajuste no daría la cara hasta que alguien abriera esa escena concreta; ahora salta al arrancar.

### Los verbos: dos convenciones reconciliadas

La spec §4.6 escribe `detectar`. El contenido en disco escribe `V1_DETECTAR`. `Verbos.Normalizar` acepta las dos y canoniza a la del contenido, que es la que está escrita en el JSON que ya existe.

---

## La regla de ritmo es pedagogía, no una limitación

> **Máximo uno al día · nunca dos días seguidos · el 45 % de los días elegibles no sale ninguno.**

En doce días deben salir tres o cuatro. Si saliera uno cada día dejaría de ser una interrupción y pasaría a ser la mecánica principal, **y el juego no va de eso: va de gestionar**. Hay tres tests sobre esto, incluido uno estadístico con 3.000 tiradas que comprueba el 45 % con tolerancia del 3 %.

### Disciplina de azar

El orden del algoritmo del §7.5 no es decorativo: decide **cuándo se gasta una tirada**.

- La regla de ritmo **no gasta azar**.
- El filtrado **no gasta azar**.
- La tirada del 45 % se hace **después** de saber que había candidatos.

Consecuencia práctica: **añadir un minijuego al índice no descoloca las partidas ya guardadas**, y un nivel donde ningún minijuego aplica consume exactamente cero azar. Hay dos tests que lo vigilan contando `rng.Consumos`.

---

## El puente al motor

Los minijuegos se escribieron antes que el motor y hablan en `Dictionary<string,float>`; los eventos hablan en `Dictionary<string,object>`, porque un evento puede decir `"-15%"`. `PuenteDelMotor` traduce, en un solo sitio.

La flecha va en ese sentido a propósito: **Minijuegos conoce Eventos, y Eventos no conoce Minijuegos.** Al revés sería un ciclo, y además el motor de eventos no necesita saber que existen los verbos.

**Sobre INV-1:** `ResultadoMinijuego.Hallazgos` **no se toca**. Los minijuegos no escriben `FLG_*`; devuelven hallazgos, y el canal narrativo (C7, M8) decidirá al cerrar la fase qué flag escribe cada uno. Es lo que mantiene testeable el árbol de los catorce finales, y hay un test que lo comprueba.

**`Omitido`** vive aquí y no en cada escena porque omitir tiene la misma forma para los seis verbos. Su rúbrica por defecto es *«No llegaste a mirarlo. Decidir no mirar también es decidir.»* Si la escena trae su propia consecuencia de `omitido` en el JSON, manda esa — y entonces el cierre es *«Javier lo revisó solo, a las once de la noche»*, que es el principio 1 del §6.2: **el fallo produce una escena con nombre, cara y fecha, no una puntuación.**

### Un coste de ergonomía que conviene conocer

Hay dos tipos llamados `EfectoDiferido`: `Nexus.Core.Minijuegos` (float) y `Nexus.Core.Eventos` (object). Cualquier archivo que use los dos namespaces a la vez tiene que desambiguar con un alias:

```csharp
using EfectoDeEscena = Nexus.Core.Minijuegos.EfectoDiferido;
```

Es el precio de no haber tocado el contrato de minijuegos que ya existía. Lo paga quien escribe el puente y los tests, una línea, y a cambio el contrato de las escenas no se movió.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `new MinigameDirector(índice, perfil, rng)` | los 3 | director; **lanza** si hay ids repetidos |
| `.MinijuegoDeHoy(r, ctx)` | estado + puerto | `PendingMinigame` o `null`; marca que hoy ya hubo uno |
| `.RegistrarJugado(id, r)` | id | cuenta la ocurrencia y arranca el enfriamiento propio |
| `.Log` | — | por qué: `propuesto` · `ritmo` · `sinCandidatos` · `diaTranquilo` |
| `PuenteDelMotor.Aplicar(resultado, w, scheduler, día)` | el resultado de la escena | efectos al estado, diferidos a la agenda |
| `PuenteDelMotor.ATraza(resultado, día, antes, después)` | ídem | `EntradaTraza` lista para `DecisionTrace` |
| `PuenteDelMotor.Omitido(id, oa, consecuencia)` | id + la consecuencia del JSON | `ResultadoMinijuego` con veredicto `incorrecta` explicado |

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 282 en verde** (eran 251). `VentanaDeVerbosTests` debe dar **31/31**, y `CatalogosTests` seguir en **46/46**.

### Prueba 2 — la manual: el ritmo de veinte días

Crea `Assets/Scripts/Editor/PruebaM7.cs`:

```csharp
using System.Collections.Generic;
using Nexus.Core.Eventos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using UnityEditor;
using UnityEngine;
using EfectoDeEscena = Nexus.Core.Minijuegos.EfectoDiferido;

public static class PruebaM7 {
    private sealed class Ctx : IStateContext {
        public readonly WorldState W = new WorldState();
        public readonly RuntimeState R = new RuntimeState { Fase = 2 };
        public bool TryGetValue(string n, out double v) { return W.TryGet(n, out v) || R.TryGet(n, out v); }
        public double CallFunction(string n, string a) { return n == "diasDesde" ? 999 : 0; }
    }

    [MenuItem("Nexus/Pruebas/M7 · El ritmo de la ventana de las 15:00")]
    public static void Correr() {
        var perfil = new LevelProfile {
            Id = "nivel-01", DiasTotales = 20, NivelAndamiaje = 3,
            ObjetivosActivos = new[] { "OA-GIT-01", "OA-DIS-01" }
        };

        var indice = new List<MinigameDefinition> {
            Mj("MJ-F2-02", "OA-GIT-01", "Javier necesita el grafo antes de las 10:00"),
            Mj("MJ-F2-05", "OA-DIS-01", "La revision de diseño empieza en dos horas"),
            Mj("MJ-F2-09", "OA-GIT-01", "El release se corta a las 18:00"),
            Mj("MJ-F2-11", "OA-OTRO",   "Nadie espera esto (no aplica a este nivel)")
        };

        var ctx = new Ctx();
        var scheduler = new EffectScheduler();
        var director = new MinigameDirector(indice, perfil, new DeterministicRng(4417));
        ctx.W.Set("DeudaTecnica", 15);

        for (var dia = 1; dia <= 20; dia++) {
            ctx.R.DiaActual = dia;
            var linea = $"DIA {dia,2}  ";

            foreach (var vencido in scheduler.Vencidos(dia)) {
                EffectApplier.Aplicar(ctx.W, vencido.Efectos);
                linea += $"[SE COBRA lo de {vencido.Origen}] ";
            }

            var pendiente = director.MinijuegoDeHoy(ctx.R, ctx);
            if (pendiente != null) {
                linea += $">>> {pendiente.MinijuegoId} ({pendiente.Verbo}, {pendiente.Segundos}s) " +
                         $"\"{pendiente.PresionDiegetica}\" ";

                // el jugador lo juega a medias
                var resultado = new ResultadoMinijuego {
                    MinijuegoId = pendiente.MinijuegoId,
                    Resultado = ResultadosDeMinijuego.Parcial,
                    Rubrica = new Rubrica { Veredicto = "aceptable", Oa = pendiente.ObjetivoAprendizaje,
                                            Razon = "Viste el rebase, no el huerfano." },
                    EfectosInmediatos = { { "DeudaTecnica", 5f } },
                    EfectosDiferidos = { new EfectoDeEscena { EnDias = 4, Efectos = { { "Cobertura", -3f } } } },
                    Hallazgos = { "vio_el_rebase" }
                };

                PuenteDelMotor.Aplicar(resultado, ctx.W, scheduler, dia);
                director.RegistrarJugado(pendiente.MinijuegoId, ctx.R);
                linea += "-> parcial ";
            }

            Debug.Log(linea + $"|  deuda {ctx.W.DeudaTecnica:F1}  cob {ctx.W.Cobertura:F1}");
        }

        var porQue = "===== POR QUE EL DIRECTOR HIZO LO QUE HIZO =====\n";
        foreach (var d in director.Log) porQue += d + "\n";
        Debug.Log(porQue);
    }

    private static MinigameDefinition Mj(string id, string oa, string presion) {
        return new MinigameDefinition {
            Id = id, Verbo = Verbos.Detectar, Archivo = "minijuegos/" + id + ".json",
            ObjetivoAprendizaje = oa, PresionDiegetica = presion,
            Reloj = 90, PesoBase = 10, Enfriamiento = 6, MaxOcurrencias = 1
        };
    }
}
```

Menú **Nexus > Pruebas > M7 · El ritmo de la ventana de las 15:00**.

**Qué debes ver, y qué significa:**

- **Tres o cuatro `>>>` en veinte días**, y **nunca dos días seguidos**. Ese es el ritmo de diseño funcionando.
- **`MJ-F2-11` no sale nunca.** Su OA no está entre los objetivos del nivel: un minijuego de Git en un nivel que no enseña Git sería ruido, por bien hecho que esté.
- Cada `>>>` trae **quién espera** — *«Javier necesita el grafo antes de las 10:00»*. La presión es diegética; el reloj existe porque alguien espera, no porque sí.
- **Cuatro días después de cada minijuego** aparece un `[SE COBRA lo de MJ-...]` y la cobertura baja. El coste diferido, ahora también desde la ventana de verbos.
- En el bloque final: líneas `ritmo` (el día siguiente a uno que salió), `diaTranquilo` (había candidatos y el 45 % decidió que no) y `sinCandidatos`. **Las tres razones son distintas y el log las distingue** — eso es lo que permite depurar un balanceo que «se siente raro».

Cambia `ObjetivosActivos` para incluir `"OA-OTRO"` y vuelve a lanzarlo: `MJ-F2-11` empieza a salir.

Cuando termines, **borra `PruebaM7.cs`**.

---

## Lo que falta

- **`minijuegos/indice.json` no existe en disco todavía.** Se escribe en B1, junto al contenido de N0 y N1. Hoy solo está `MJ-F2-02.json`, la escena.
- **Los otros cinco verbos** (repartir, ordenar, elegir, predecir, trazar) están en el vocabulario y el motor los soporta, pero **sus escenas no se construyen** — está fuera del alcance acordado. N0 y N1 se cubren con V1 más el panel de decisión.
- **Nadie llama a `MinijuegoDeHoy` todavía.** Es `GameSession` (M9) quien lo hará en la ventana de las 15:00.
