# A′2 · Alertas

> **Fase A′, parada 2 de 4.** Depende de A′1.
> `NEXUS-DOCUMENTO-MAESTRO.md` §3.3 · §7.5 · §7.7 · M8.
> Estado: **implementado y probado**. 24 tests nuevos, todos en verde.

---

## Qué se hizo

| Archivo | Qué |
|---|---|
| `Core/Jornada/Alerta.cs` | **nuevo** · `Alerta`, `TiposDeAlerta`, `EstadosDeAlerta` |
| `Core/Jornada/ColaDeAlertas.cs` | **nuevo** · las alertas de hoy y su estado |
| `Core/Eventos/EfectosDiferidos.cs` | edición: `TelegrafiadoPendiente.MinutoDelEvento` |
| `Core/Eventos/EffectScheduler.cs` | edición: `AgendarTelegrafiado` acepta el minuto |
| `Core/Guardado/NivelEnCurso.cs` | edición: el bloque `Alertas` |
| `Core/Guardado/SaveStore.cs` | edición: validar que existe |
| `Tests/editMode/AlertasTests.cs` | 24 tests |

**Las dos ediciones de código existente son compatibles hacia atrás.** `AgendarTelegrafiado` gana un parámetro **opcional** al final, así que las llamadas de M5 y los tests de M3 siguen compilando sin tocarlos. Por eso los 361 tests anteriores siguen en verde.

---

## La distinción que define esta parada

> ## **La alerta NO es el telegrafiado.**

El §7.7 avisa de que confundirlos **destruye el argumento pedagógico** del juego. Son dos cosas:

| | **Telegrafiado** | **Alerta** |
|---|---|---|
| Cuándo | **días antes** (`telegrafiado.diasAntes`) | el día que toca, a una hora concreta |
| Qué dice | *«El build tardó 14 min. Ayer tardaba 6.»* | *«Te necesitan en tu escritorio.»* |
| Para qué | convierte el azar en **gestión de riesgo** | es la citación del momento |
| Quién lo lleva | C4, la cola temporal | C11, esta parada |

**Un evento bien telegrafiado hace días también llega como alerta el día que toca.** Hay un test que recorre el ciclo entero: el director agenda EV-TEC-014 para el día 9 a las 14:00 avisando 2 días antes; el día 7 sale el aviso y no hay ninguna alerta; el día 9 nace la alerta a las 14:00.

---

## Qué hace

### La ventana de atención es el nervio del día

```
        ⚡ 11:00  «Te necesitan en tu escritorio»       [chat de Javier]
           │
           │  VENTANA DE ATENCION: 3 h  ────────────────► 14:00
           │  el jugador PUEDE seguir con lo suyo.
           │  Volver cuesta el tiempo de desplazamiento
           │  desde donde este: estar lejos es un riesgo real
           │
           ├─ ATENDIDA ──► la decision, o el minijuego
           │
           └─ EXPIRADA ──► resultado 'omitido'
                           la rubrica lo lee como 'incorrecta'
```

### La regla que más importa

> **Expirar no es lo mismo que no haber pasado nada.**

El M8 lo pone como criterio literal de «terminado»: *«una alerta ignorada hasta que expira produce el resultado `omitido` con su escena, y **deja el mundo igual que haberla resuelto mal — no igual que si no hubiera pasado nada**»*.

Aquí se marca el estado; quien aplica la consecuencia es `GameSession` en A′4. Pero el estado ya distingue los tres casos, y el guardado los lleva.

### Y queda constancia de dónde estabas

`Alerta.ZonaDelJugador` se anota tanto al atender como al expirar. No es decoración: el §8 lo pide explícitamente para la traza —

> *«Dejar expirar una alerta → qué estaba haciendo el jugador en ese momento: **evidencia directa de gestión de la atención**»*

— y es lo que permitirá que el Dashboard diga *«la alerta de las 15:00 expiró mientras estabas en la cafetería»* en vez de un número.

### El botón «cerrar jornada», bien cerrado

`SePuedeCerrarLaJornada(minuto)` solo devuelve `true` cuando **no queda nada**: ni pendiente ahora, ni por sonar después.

Así saltar al cierre **nunca se come una consecuencia**, y lo único que cuesta es perderse la exploración y las conversaciones de lo que quedaba de tarde. Ésa era la tercera palanca que prometí en A′1 para el problema de los 80 minutos.

### Un detalle de intervalo que evita un bug feo

`SuenanEntre(desde, hasta)` usa el intervalo **abierto por la izquierda y cerrado por la derecha**: `(desde, hasta]`. Sin eso, avanzar el reloj dos veces seguidas anunciaría dos veces la misma alerta. Hay un test solo para esto.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `Encolar(alerta)` | una `Alerta` | nada; **lanza** si expira antes de sonar o no tiene id |
| `SuenanEntre(desde, hasta)` | el tramo que acaba de avanzar el reloj | las que suenan **una sola vez** |
| `Expirar(minuto, zona)` | ahora + dónde está el jugador | las que se perdieron, ya marcadas |
| `Pendientes(minuto)` | ahora | las que ya sonaron y siguen vivas |
| `Atender(id, minuto, zona)` | ídem | la alerta; **lanza** si ya se resolvió o aún no sonó |
| `SePuedeCerrarLaJornada(minuto)` | ahora | `bool` — el gate del botón |
| `VaciarDelDia()` | — | las alertas de ayer no se heredan |
| `Copia()` / `Restaurar(lista)` | — / la lista del guardado | copia **profunda** |

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 394 en verde** (eran 370). `AlertasTests` debe dar **24/24**.

El que resume el módulo se llama `Un_dia_con_dos_alertas_una_atendida_y_otra_perdida`: hace correr un día entero de media hora en media hora y comprueba la bitácora línea a línea, incluida `18:00 EXPIRA MJ-F2-02 (estabas en cafeteria)`.

### Prueba 2 — la manual: el día en que estabas lejos

Crea `Assets/Scripts/Editor/PruebaA2.cs`:

```csharp
using System.Collections.Generic;
using Nexus.Core.Jornada;
using UnityEditor;
using UnityEngine;

public static class PruebaA2 {
    [MenuItem("Nexus/Pruebas/A2 · El dia en que estabas lejos")]
    public static void Correr() {
        Simular("EL JUGADOR SE QUEDA CERCA", volverAlEscritorio: true);
        Simular("EL JUGADOR SE VA AL DATA HUB", volverAlEscritorio: false);
    }

    private static void Simular(string titulo, bool volverAlEscritorio) {
        var config = new JornadaConfig();
        var reloj = new RelojDeJornada(config);
        var cola = new ColaDeAlertas();

        // el director agendo dos cosas para hoy
        cola.Encolar(Alerta("EV-TEC-014", 11 * 60, config, TiposDeAlerta.Decision,
                            "Javier: 'el build lleva 14 min, ven a mirarlo'"));
        cola.Encolar(Alerta("MJ-F2-02", 15 * 60, config, TiposDeAlerta.Minijuego,
                            "Ticket escalado: auditoria del repositorio"));

        var zona = "escritorio";
        var texto = $"=== {titulo} ===\n";

        while (!reloj.Terminada) {
            var desde = reloj.Minuto;
            reloj.Avanzar(30);

            foreach (var a in cola.SuenanEntre(desde, reloj.Minuto))
                texto += $"  {reloj}  ALERTA [{a.Canal}] \"{a.Texto}\"  " +
                         $"-> tienes hasta las {RelojDeJornada.Formatear(a.MinutoDeExpiracion)}\n";

            foreach (var a in cola.Expirar(reloj.Minuto, zona))
                texto += $"  {reloj}  *** SE PERDIO {a.Id} *** estabas en {a.ZonaDelJugador}\n";

            // a las 10:00 el jugador baja al Data Hub a buscar a Javier
            if (reloj.Minuto == 10 * 60) { zona = "data-hub"; texto += $"  {reloj}  bajas al Data Hub -2\n"; }

            // vuelve (o no) cuando suena la primera alerta
            if (reloj.Minuto == 12 * 60 && volverAlEscritorio) {
                zona = "escritorio";
                texto += $"  {reloj}  subes al escritorio (2 pisos, 60 min)\n";
                reloj.Avanzar(60);
            }

            foreach (var a in cola.Pendientes(reloj.Minuto))
                if (zona == "escritorio" && a.EstaPendiente) {
                    cola.Atender(a.Id, reloj.Minuto, zona);
                    texto += $"  {reloj}  ATIENDES {a.Id}\n";
                }

            if (cola.SePuedeCerrarLaJornada(reloj.Minuto) && reloj.Minuto >= 16 * 60) {
                texto += $"  {reloj}  no queda nada pendiente -> puedes cerrar la jornada\n";
                reloj.SaltarHasta(reloj.MinutoDeCierre);
            }
        }

        texto += "\n  RESUMEN:\n";
        foreach (var a in cola.Alertas) texto += $"    {a}\n";
        Debug.Log(texto);
    }

    private static Alerta Alerta(string id, int minuto, JornadaConfig c, string tipo, string mensaje) {
        return new Alerta {
            Id = id, Tipo = tipo, MinutoDeLaAlerta = minuto,
            MinutoDeExpiracion = minuto + c.VentanaDeAtencionMinutos,
            Canal = tipo == TiposDeAlerta.Minijuego ? "ticket" : "chat", Texto = mensaje
        };
    }
}
```

Menú **Nexus > Pruebas > A2 · El día en que estabas lejos**. Salen **dos bloques**, la misma semilla de alertas con dos comportamientos distintos.

**Qué debes ver, y qué significa:**

- En los dos, a las **11:00** suena `EV-TEC-014` con su texto diegético y el plazo hasta las **14:00**. Nunca aparece «evento aleatorio»: aparece Javier diciendo algo.
- **El que vuelve** sube al escritorio a las 12:00, le cuesta una hora de desplazamiento, y llega a tiempo: `ATIENDES EV-TEC-014`.
- **El que se queda en el Data Hub** ve `*** SE PERDIO EV-TEC-014 *** estabas en data-hub` a las 14:00. **Y eso queda escrito.**
- Al final, el resumen con las dos alertas y su estado. Compara los dos bloques: **la misma jornada, la misma semilla, dos historias distintas** — y la diferencia es sólo dónde estaba el jugador.

Prueba a cambiar `VentanaDeAtencionMinutos` a 60 en el `JornadaConfig`: con una hora de ventana, volver desde el Data Hub ya no llega. Ése es el número que el M8 dice que *«hay que jugarlo, no razonarlo»*.

Cuando termines, **borra `PruebaA2.cs`**.

---

## Lo que falta

- **Nadie crea alertas todavía.** El director agenda `(día, minuto)` pero `GameSession` aún no convierte los telegrafiados de hoy en alertas: es **A′4**.
- **Expirar todavía no cobra nada.** El resultado `omitido` con su escena lo aplica `GameSession`, también en A′4.
- **Las zonas son cadenas sueltas.** `"data-hub"` es hoy un texto sin mapa detrás; el mapa real, con sus costes de desplazamiento, es **A′3**.
- **El director no elige el minuto.** `MinutoDelEvento` existe y vale −1: quien lo sortee será `EventDirector` en A′4, con el mismo `DeterministicRng`, para que el Modo Aula siga siendo reproducible **al minuto**.
