# M3 · C4 Cola Temporal

> Subsistema C4 de `nexus-motor-especificacion-tecnica.md` (§4.4.4).
> Estado: **implementado y probado**. 20 tests nuevos, todos en verde.
> Depende de M1 y M2.

---

## Qué se hizo

| Archivo | Qué contiene |
|---|---|
| `Core/Eventos/EfectosDiferidos.cs` | `EfectoDiferido`, `EfectoEnCola`, `TelegrafiadoPendiente` |
| `Core/Eventos/EffectScheduler.cs` | `EffectScheduler` — la agenda del tiempo |
| `Tests/editMode/ColaTemporalTests.cs` | 20 tests |

### Y además: se cerraron los cuatro `TODO` de `NivelEnCurso`

Esto **sí toca código existente**, y en M1 lo aplacé a propósito. Lo hago ahora porque los cuatro tipos ya existen (`WorldState` y `RuntimeState` de M1, `EfectoEnCola` y `TelegrafiadoPendiente` de M3) y es el momento natural: hacerlo de golpe es una edición coherente, y deja M9 libre para concentrarse solo en `GameSession`, que ya va a ser el módulo más grande.

| Archivo | Cambio | Líneas |
|---|---|---|
| `Core/Guardado/NivelEnCurso.cs` | `JToken W/R` → `WorldState/RuntimeState`; `JArray` → `List<EfectoEnCola>` / `List<TelegrafiadoPendiente>` | 4 campos |
| `Core/Guardado/SaveStore.cs` | `n.W.Type != JTokenType.Object` → `n.W == null` | 2 |
| `Tests/editMode/PersistenciaTests.cs` | la clase falsa `Stocks` se sustituye por el `WorldState` real | ~8 |

**El esquema JSON en disco no cambia** — el propio comentario de `NivelEnCurso` lo garantizaba, y hay un test que lo comprueba (`"deudaTecnica": 47.25` sigue apareciendo igual). `PersistenciaTests` sigue en **19/19**, los mismos de antes, pero ahora prueba los tipos de producción en vez de una clase falsa de cuatro campos. Es un test estrictamente mejor.

---

## Qué hace

Dos listas y **ninguna regla de juego**. La llenan `GameSession` y `EventDirector`; la vacía solo `GameSession`.

### La cola de efectos diferidos — el principio P4

Aquí vive *«el coste siempre es diferido»*. Lo que se decide el día 2 explota el día 11, y para entonces el jugador ya no se acuerda de por qué. Por eso existe la cadena causal de la Fase 4: para que se acuerde.

Y por eso la cola **viaja en el guardado**: sin ella, el jugador esquivaría consecuencias recargando la partida. Eso es exactamente lo que INV-7 prohíbe, y hay un test que lo comprueba jugando la misma consecuencia con y sin recarga.

### Los telegrafiados — INV-3

**El telegrafiado no es la alerta.** El telegrafiado llega uno a tres días antes por un canal diegético (*«El build tardó 14 min. Ayer tardaba 6.»*); la alerta es la citación del momento. El jugador que lee los logs ve venir el problema; el que no los lee, no. Esa diferencia es contenido pedagógico, no un detalle de UI.

### Tres efectos laterales que hay que conocer

| Llamada | Efecto lateral | Por qué |
|---|---|---|
| `Vencidos(día)` | **saca** los efectos de la cola | llamarlo dos veces el mismo día no los aplica dos veces |
| `AvisosDeHoy(día)` | marca `Emitido = true` | el aviso sale **una vez**; si se repitiera, el jugador dejaría de leerlo |
| `Encolar(...)` | **copia** el diccionario de efectos | el catálogo se carga una sola vez: compartirlo dejaría que una partida reescribiera el contenido de las siguientes |

### Tres decisiones que tomé donde la spec no llegaba

- **`Vencidos` recoge también lo atrasado** (`DiaObjetivo < día`). No debería pasar, porque `GameSession` llamará a esto cada mañana, pero si un día se saltara, un efecto perdido sería una consecuencia que el jugador esquiva sin enterarse — justo lo que la cola existe para impedir.
- **`AvisosDeHoy` también emite los avisos atrasados.** Mismo razonamiento, pero por INV-3: perder un aviso dispararía su evento a ciegas.
- **El día del aviso nunca baja de 1.** Un evento del día 2 telegrafiado con 3 días de antelación caería en el día −1, donde no lo vería nadie.

### Una colisión de nombres, resuelta sin tocar nada

Ya existía un `EfectoDiferido` en `Nexus.Core.Minijuegos` con `Dictionary<string,float>`. El de eventos usa `Dictionary<string,object>` porque un evento puede decir `"-15%"`, que un `float` no sabe representar.

Definí `Nexus.Core.Eventos.EfectoDiferido` aparte y **no puse el conversor aquí**: hacerlo obligaría a que `Core.Eventos` conociera `Core.Minijuegos`, que es justo la flecha que no debe existir. El puente lo pone M7, donde vive el director de minijuegos.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `Encolar(díaActual, diferido, origen)` | día + `EfectoDiferido` + quién lo programó | nada; lanza si vence en el pasado |
| `Vencidos(día)` | día absoluto | lista en orden de encolado, **y los quita** |
| `AgendarTelegrafiado(id, díaEvento, díasAntes, canal, texto)` | los 5 campos | nada |
| `AvisosDeHoy(día)` | día | avisos a emitir, **marcados como emitidos** |
| `EventosQueDisparanHoy(día)` | día | los de ese día; **no los retira** |
| `HayEventoAgendado(id)` | id | `bool` — para no agendar dos veces |
| `EventosAgendadosEn(día)` | día | cuántos hay — hace cumplir `maxEventosPorDia` |
| `OlvidarAgenda(id)` | id | retira el evento resuelto o cancelado |
| `Restaurar(cola, telegrafiados)` | las 2 listas del guardado | nada; **copia**, no comparte |
| `CopiaDeCola()` / `CopiaDeTelegrafiados()` | — | copia **profunda** para guardar |

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.

**Qué debes ver:** **143 en verde** (eran 123). Los nuevos están bajo `ColaTemporalTests`: **20/20**.

Mira también `PersistenciaTests`: debe seguir en **19/19**. Son los mismos tests de siempre, adaptados a los tipos nuevos. Si alguno se pusiera rojo, sería culpa mía y de este módulo.

### Prueba 2 — la manual: ver el tiempo funcionando

Crea `Assets/Scripts/Editor/PruebaM3.cs`:

```csharp
using System.Collections.Generic;
using Nexus.Core.Eventos;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using UnityEditor;
using UnityEngine;

public static class PruebaM3 {
    [MenuItem("Nexus/Pruebas/M3 · Veinte dias con consecuencias diferidas")]
    public static void Correr() {
        var s = new EffectScheduler();
        var w = new WorldState();
        w.Set("DeudaTecnica", 15);
        w.Set("Cobertura", 55);

        // Dia 5: el director elige un evento para el dia 9 y lo telegrafia 2 dias antes.
        s.AgendarTelegrafiado("EV-TEC-014", 9, 2, "log", "El build tardo 14 min. Ayer tardaba 6.");

        for (var dia = 1; dia <= 20; dia++) {
            var linea = $"DIA {dia,2}  ";

            foreach (var efecto in s.Vencidos(dia)) {
                EffectApplier.Aplicar(w, efecto.Efectos);
                linea += $"[SE COBRA lo de {efecto.Origen}] ";
                if (efecto.EventoForzado != null) linea += $"[encadena {efecto.EventoForzado}] ";
            }

            foreach (var aviso in s.AvisosDeHoy(dia))
                linea += $"[{aviso.Canal}] \"{aviso.Texto}\" ";

            foreach (var evento in s.EventosQueDisparanHoy(dia)) {
                linea += $">>> {evento.EventoId} <<<  el jugador elige 'compilar en local y seguir' ";
                s.OlvidarAgenda(evento.EventoId);
                EffectApplier.Aplicar(w, new Dictionary<string, object> { { "DeudaTecnica", 12L } });
                s.Encolar(dia, new EfectoDiferido {
                    EnDias = 9,
                    Efectos = new Dictionary<string, object> { { "VelocidadMod", "-25%" }, { "MoralEquipo", -6 } },
                    EventoForzado = "EV-TEC-021"
                }, "EV-TEC-014");
            }

            Debug.Log(linea + $"|  deuda {w.DeudaTecnica:F1}  vMod {w.VelocidadMod:F2}  moral {w.MoralEquipo:F1}");
        }
    }
}
```

Menú **Nexus > Pruebas > M3 · Veinte días con consecuencias diferidas**.

**Qué debes ver, y qué significa:**

- **Días 1–6:** nada. La deuda sigue en 15.0.
- **Día 7:** aparece `[log] "El build tardó 14 min. Ayer tardaba 6."` — el aviso. No pasa nada más. Este es el momento en que un jugador atento podría prepararse.
- **Día 8:** silencio otra vez. El aviso **no se repite**.
- **Día 9:** `>>> EV-TEC-014 <<<`. La deuda salta de 15.0 a 27.0. El jugador cree que ya pagó.
- **Días 10–17:** silencio. Aquí es donde se olvida.
- **Día 18:** `[SE COBRA lo de EV-TEC-014] [encadena EV-TEC-021]`. `vMod` cae de 1.00 a 0.75 y la moral baja 6. **Nueve días después de la decisión, y sin ninguna alarma en medio.**

Ese último salto es el juego entero en una línea de consola. Cuando en la Fase 4 el jugador vea la cadena causal, la frase será: *«El fallo ocurrió el día 18. La decisión que lo causó fue el día 9.»*

Cuando termines, **borra `PruebaM3.cs`**.

---

## Lo que falta

- **Nadie llena la cola todavía por su cuenta.** `EventDirector` (M5) es quien agendará los telegrafiados de verdad; hoy hay que llamar a `AgendarTelegrafiado` a mano.
- **`EfectoDiferido` de minijuegos sigue sin puente.** Lo pone M7.
- **`GameSession` es quien llamará a `Vencidos()` cada mañana.** Hasta M9, el bucle diario no existe.
