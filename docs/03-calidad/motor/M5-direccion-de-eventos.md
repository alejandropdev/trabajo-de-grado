# M5 · C3 Dirección de Eventos

> Subsistema C3 de `nexus-motor-especificacion-tecnica.md` (§4.4.2, §4.4.3, §6.3).
> Estado: **implementado y probado**. 31 tests nuevos, todos en verde.
> Depende de M1, M2, M3 y M4.

---

## Qué se hizo

| Archivo | Clases |
|---|---|
| `Core/Eventos/EventDefinition.cs` | `EventDefinition`, `ModificadorPeso`, `Telegrafiado`, `OpcionEvento`, `RubricaOpcion`, `DecisionDelDirector` + vocabularios `FasesDelNivel` y `CurvasDePeso` |
| `Core/Eventos/EventDirector.cs` | `EventDirector` |
| `Tests/editMode/DireccionDeEventosTests.cs` | 31 tests |

**Cero ediciones a código existente.**

---

## La invariante que define el subsistema

**INV-3: el director elige y avisa; no dispara nunca.**

Todo lo que hace `TickSeleccion` es poner un aviso en la agenda para dentro de unos días. Quien presenta el evento es `GameSession`, el día que toca, y solo después de que el aviso haya salido por su canal diegético.

Por eso el jugador que lee los logs ve venir los problemas y el que no los lee, no. **Esa diferencia es contenido pedagógico**: aprender a leer las señales de un proyecto *es* el objetivo del juego.

La invariante está blindada por tres sitios:
- `EventoDeHoy` **lanza** si un evento va a presentarse sin que su aviso se haya emitido. Solo puede romperse por un error de orden dentro del motor — el contenido no tiene forma de provocarlo — así que se prefiere que estalle en los tests a que pase inadvertido en una sesión de laboratorio.
- `DiasDeAntelacion` fuerza un **mínimo de 1 día**: avisar el mismo día no es avisar.
- Un test simula 20 días completos y comprueba que **cada** evento presentado tenía su aviso emitido antes.

## El ciclo de vida completo (§6.3)

```
EnCatálogo ──┬─→ descartado por fase / precondición / enfriamiento / ocurrencias / metodología
             └─→ Candidato ─→ ruleta ponderada ─→ Elegido
                                                    │
                        ┌───────────────────────────┴─── día lleno ─→ vuelve al pozo, SIN gastar drama
                        ↓
                    Agendado (gasta drama) ─→ Avisado ─→ Disparado ─→ Resuelto ─→ EnEnfriamiento
```

El detalle de «**día lleno no consume drama**» está en la spec y es deliberado: que el calendario esté ocupado no es una decisión dramática, es una limitación de agenda. Hay un test que lo comprueba.

## La fórmula del peso

```
pesoEfectivo = pesoBase
             × Π(1 + contribución_i)      ← tus decisiones cambian tu perfil de riesgo
             × pesosPorTag[tag]           ← el enfoque del nivel, que tú configuraste sin saberlo
             × multiplicadorMetodología   ← y la etapa de hoy, en cascada
```

| Curva | Fórmula | Para qué sirve |
|---|---|---|
| `lineal` | `factor · valor` | cuanta más deuda, más probable |
| `inversa` | `factor · (100 − valor)` | cuanta **menos** cobertura, más probable |
| `cuadratica` | `factor · valor² / 100` | casi nada hasta la mitad, y luego se dispara |

### Una decisión que la spec dejaba abierta: los tags se multiplican

El §5.6 dice que el motor lite tomaba **el máximo** entre los tags y que el motor completo **multiplica**, marcándolo como «decisión a confirmar al implementar». **Elegí multiplicar.**

El motivo: un evento que es a la vez `tecnico` y `devops`, en un nivel donde el jugador descuidó ambos atributos de calidad, debe ser más probable que uno que solo toca uno de los dos. Con el máximo, descuidar dos cosas sale igual de caro que descuidar una, y eso rompe la lección.

Consecuencia práctica que conviene tener presente al escribir contenido: **los pesos por tag se componen rápido**. Un evento con dos tags a 1.5 y 2.0 sale con peso ×3.0. Si al balancear los niveles esto se descontrola, el ajuste va en `pesosPorTag` del perfil, no en la fórmula.

## Dos decisiones más donde la spec no llegaba

**1 · `TickSeleccion` no recibe un `WorldState`.**
La firma del §4.4.3 lo menciona, pero el director lee el estado **solo** a través de `IStateContext` — que es el puerto que existe exactamente para eso. Darle un `WorldState` sería abrirle una puerta que no debe tener, y el parámetro quedaría sin usar. Firma real: `TickSeleccion(RuntimeState r, IStateContext ctx, DayPlan plan)`.

**2 · `ForzarEvento` no consume drama.**
La spec dice que las cadenas siguen telegrafiadas pero no aclara el presupuesto. Las cadenas las escribe el contenido a propósito (las 7 del §A: *la deuda que vuelve*, *el bus factor*, *la autoría perdida*…), no el presupuesto de caos del nivel. Si consumieran drama, una cadena podría silenciar al director durante el resto de la fase.

**3 · El multiplicador de drama se aplica al leerlo, no al construir.**
`DramaRestante(r)` devuelve `floor(presupuestoDeLaFase × multiplicadorDrama)`. Cascada con multiplicador 0.8 sobre un presupuesto de 3 tiene **2** eventos: un nivel más plano, y más aburrido. Eso es intencionado — el aburrimiento de Cascada es parte de lo que enseña.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `new EventDirector(catálogo, perfil, reglas, rng, scheduler)` | los 5 | director listo; **lanza** si hay ids repetidos |
| `.TickSeleccion(r, ctx, plan)` | estado + puerto + plan del día | nada visible: **agenda** en el scheduler y escribe en `Log` |
| `.EventoDeHoy(r)` | estado | el `EventDefinition` de hoy o `null`; **lanza** si su aviso no salió |
| `.CalcularPeso(ev, ctx, plan)` | evento + puerto + plan | `double` ≥ 0 |
| `.ForzarEvento(id, r)` | id de la cadena | agenda sin gastar drama; **lanza** si el id no existe |
| `.RegistrarDisparo(ev, r)` | evento resuelto | cuenta la ocurrencia, arranca los dos enfriamientos, saca de la agenda |
| `.DramaRestante(r)` | estado | presupuesto de la fase, ya escalado por la metodología |
| `.Log` | — | `List<DecisionDelDirector>`: por qué hizo lo que hizo, un día por entrada |

El `Log` no es telemetría. Es la herramienta con la que se depura un balanceo que «se siente raro», y la que permite explicar en la tesis por qué una partida con semilla 4417 sale como sale.

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 205 en verde** (eran 174). `DireccionDeEventosTests` debe dar **31/31**.

### Prueba 2 — la manual: un nivel de 20 días, con el director decidiendo

Crea `Assets/Scripts/Editor/PruebaM5.cs`:

```csharp
using System.Collections.Generic;
using Nexus.Core.Eventos;
using Nexus.Core.Metodologia;
using Nexus.Core.Modelo;
using Nexus.Core.Servicios;
using UnityEditor;
using UnityEngine;

public static class PruebaM5 {
    private sealed class Ctx : IStateContext {
        public readonly WorldState W = new WorldState();
        public readonly RuntimeState R = new RuntimeState { Fase = 2 };
        public bool TryGetValue(string n, out double v) { return W.TryGet(n, out v) || R.TryGet(n, out v); }
        public double CallFunction(string n, string a) {
            if (n == "diasDesde") { int d; return R.Enfriamientos.TryGetValue(a, out d) ? R.DiaActual - d : 999; }
            if (n == "ocurrencias") { int x; return R.Ocurrencias.TryGetValue(a, out x) ? x : 0; }
            return 0;
        }
    }

    [MenuItem("Nexus/Pruebas/M5 · Un nivel de veinte dias con director")]
    public static void Correr() {
        var perfil = new LevelProfile { Id = "nivel-01", DiasTotales = 20 };
        perfil.Director.PresupuestoDrama = new[] { 1, 4, 2, 1 };
        perfil.Director.EnfriamientoGlobal = 3;
        perfil.Director.MaxEventosPorDia = 1;
        perfil.Director.PesosPorTag["equipo"] = 1.5;
        perfil.Director.PesosPorTag["tecnico"] = 0.8;

        var metodologia = new MethodologyProfile {
            Id = "kanban", Nombre = "Kanban", Familia = FamiliasDeMetodologia.Agil,
            Calendario = new Calendario { Tipo = TiposDeCalendario.Continuo, EtiquetaUnidad = "Flujo", LimiteWipInicial = 4 },
            TableroPrincipal = TablerosPrincipales.Cfd,
            RubricaCierre = new RubricaCierre { Practicas = { new PracticaEsperada {
                Id = "x", Metrica = "diasConHorasExtra", Comparador = "<=", Objetivo = 4 } } }
        };
        var reglas = new MethodologyRules(metodologia, 20);

        var ctx = new Ctx();
        ctx.R.DramaRestante = (int[])perfil.Director.PresupuestoDrama.Clone();
        ctx.W.Set("DeudaTecnica", 10);
        ctx.W.Set("Cobertura", 55);
        ctx.W.Set("Cansancio", 15);

        var scheduler = new EffectScheduler();
        var director = new EventDirector(Catalogo(), perfil, reglas, new DeterministicRng(4417), scheduler);
        var plan = reglas.PlanFor(1);

        for (var dia = 1; dia <= 20; dia++) {
            ctx.R.DiaActual = dia;
            var linea = $"DIA {dia,2}  ";

            foreach (var vencido in scheduler.Vencidos(dia)) {
                EffectApplier.Aplicar(ctx.W, vencido.Efectos);
                linea += $"[SE COBRA lo de {vencido.Origen}] ";
            }

            var evento = director.EventoDeHoy(ctx.R);
            if (evento != null) {
                var opcion = evento.Opciones[1];                       // el jugador siempre elige el atajo
                linea += $">>> {evento.Id} · elige \"{opcion.Texto}\" ";
                EffectApplier.Aplicar(ctx.W, opcion.EfectosInmediatos);
                foreach (var dif in opcion.EfectosDiferidos) scheduler.Encolar(dia, dif, evento.Id);
                director.RegistrarDisparo(evento, ctx.R);
            }

            director.TickSeleccion(ctx.R, ctx, plan);

            foreach (var aviso in scheduler.AvisosDeHoy(dia))
                linea += $"[{aviso.Canal}] \"{aviso.Texto}\" ";

            Debug.Log(linea + $"|  deuda {ctx.W.DeudaTecnica:F1}  cob {ctx.W.Cobertura:F1}  drama {director.DramaRestante(ctx.R)}");
        }

        var porQue = "===== POR QUE EL DIRECTOR HIZO LO QUE HIZO =====\n";
        foreach (var d in director.Log) porQue += d + "\n";
        Debug.Log(porQue);
    }

    private static List<EventDefinition> Catalogo() {
        return new List<EventDefinition> {
            Evento("EV-TEC-014", "El servidor de integracion se cae", "tecnico", "devops",
                   "El build tardo 14 min. Ayer tardaba 6.",
                   new ModificadorPeso { Variable = "DeudaTecnica", Curva = CurvasDePeso.Lineal, Factor = 0.04 }),
            Evento("EV-EQ-03", "Oscar se plantea irse", "equipo",  null,
                   "Oscar lleva tres dias sin hablar en el standup.",
                   new ModificadorPeso { Variable = "Cansancio", Curva = CurvasDePeso.Cuadratica, Factor = 0.03 }),
            Evento("EV-CAL-01", "Una regresion en produccion", "calidad", null,
                   "Nadie ha tocado las pruebas de regresion esta semana.",
                   new ModificadorPeso { Variable = "Cobertura", Curva = CurvasDePeso.Inversa, Factor = 0.02 }),
            Evento("EV-CLI-06", "La Ministra pregunta por el panel", "cliente", null,
                   "Correo sin asunto de la oficina de la Ministra.", null)
        };
    }

    private static EventDefinition Evento(string id, string nombre, string tag1, string tag2,
                                          string aviso, ModificadorPeso mod) {
        var ev = new EventDefinition {
            Id = id, Nombre = nombre,
            Tags = tag2 == null ? new List<string> { tag1 } : new List<string> { tag1, tag2 },
            Fases = new List<string> { FasesDelNivel.Desarrollo },
            PesoBase = 10, Enfriamiento = 6, MaxOcurrencias = 1, Severidad = 3,
            Telegrafiado = new Telegrafiado { DiasAntes = 2, Canal = "log", Texto = aviso },
            Opciones = {
                new OpcionEvento { Id = "A", Texto = "Parar la linea y arreglarlo",
                                   EfectosInmediatos = { { "Dias", 2 }, { "DeudaTecnica", -8 } },
                                   Rubrica = new RubricaOpcion { Veredicto = "correcta", Razon = "…" } },
                new OpcionEvento { Id = "B", Texto = "Seguir y ya lo vemos",
                                   EfectosInmediatos = { { "DeudaTecnica", 12L } },
                                   EfectosDiferidos = { new EfectoDiferido {
                                       EnDias = 6, Efectos = { { "VelocidadMod", "-15%" }, { "MoralEquipo", -5 } } } },
                                   Rubrica = new RubricaOpcion { Veredicto = "incorrecta", Razon = "…" } }
            }
        };
        if (mod != null) ev.ModificadoresPeso.Add(mod);
        return ev;
    }
}
```

Menú **Nexus > Pruebas > M5 · Un nivel de veinte días con director**.

**Qué debes ver, y qué significa:**

- **20 líneas de día** y, al final, **un bloque con el razonamiento del director**.
- Los avisos `[log] "..."` aparecen **siempre dos días antes** que su `>>> EV-... <<<`. Búscalos: nunca verás un `>>>` sin su `[log]` dos líneas más arriba. Eso es INV-3 a simple vista.
- La **deuda sube 12 cada vez** que el jugador elige el atajo, y **seis días después** aparece un `[SE COBRA lo de EV-...]`. El coste diferido, funcionando sobre el director real.
- **`drama` baja** de 4 a 3, 2, 1… y cuando llega a 0 el director deja de agendar. El nivel se calma solo.
- En el bloque final, líneas como `dia 4: sinCandidatos · 0 candidatos, drama 3` o `dia 9: agendado EV-CAL-01 para el dia 11 (peso 23.40) · 2 candidatos, drama 2`. **Ese `peso 23.40` es la cobertura baja empujando hacia arriba la probabilidad de una regresión.** El riesgo no es mala suerte: lo construyó el jugador.

Prueba a cambiar `ctx.W.Set("Cobertura", 55)` por `95` y vuelve a lanzarlo: verás que `EV-CAL-01` pierde peso y sale mucho menos. Esa es toda la tesis del motor en un experimento de diez segundos.

Cuando termines, **borra `PruebaM5.cs`**.

---

## Lo que falta

- **Nadie llama a `TickSeleccion` todavía.** Es `GameSession` (M9) quien lo hará en la ventana de las 09:00.
- **Las opciones no se presentan.** `PendingDecision` y `OpcionPresentada` — con sus previsualizaciones y sus bloqueos por `Requisitos` — son DTOs de M9.
- **`EsCambioDeAlcance` no se consulta.** M9 pasará esos eventos por `MethodologyRules.EvaluarCambioDeAlcance` antes de presentarlos.
- **El catálogo se construye a mano.** `CatalogLoader` y `SchemaValidator` llegan en **M6**, el siguiente.
