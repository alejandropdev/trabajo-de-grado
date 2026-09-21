# A′4 · `GameSession` con el día continuo

> **Fase A′, parada 4 de 4 — cierra el retrofit.**
> `NEXUS-DOCUMENTO-MAESTRO.md` §3.3 · §3.4 · §3.5 · §7.5 · §7.7 · M8.
> Estado: **implementado y probado**. 23 tests nuevos, todos en verde.
> Depende de A′1, A′2 y A′3, y toca `GameSession` (M9) entero.

---

## Qué se hizo

| Archivo | Qué |
|---|---|
| `Core/Servicios/SorteoDeMinuto.cs` | **nuevo** · el paso 7 del §7.5, hecho minuto |
| `Core/Sesion/GameSession.cs` | reescrito: `ComenzarDia`, `AvanzarReloj`, `IrAZona`, `AtenderAlerta`, `CerrarJornada`, `TerminarDia`, `Capturar`/`Restaurar` |
| `Core/Sesion/Dtos.cs` | `ResultadoDeAvance` |
| `Core/Eventos/EventDirector.cs` | el paso 7 sortea el minuto además del día |
| `Core/Modelo/RuntimeState.cs` | **se retira `Ventana`** — las cuatro ventanas fijas ya no existen |
| `Tests/editMode/GameSessionTests.cs` | reescrito por completo: 32 → 44 tests |
| `Tests/editMode/SorteoDeMinutoTests.cs` | 11 tests nuevos |

---

## Por qué esta es la parada más grande de todas

Las tres anteriores (A′1 reloj, A′2 alertas, A′3 mapa) fueron **aditivas**: piezas nuevas que nadie usaba todavía. Ésta es la que las conecta todas dentro de `GameSession`, y por eso es la única de las cuatro que **rompió tests existentes** — 21 de los 32 de `GameSessionTests` asumían el modelo viejo (`Decision`/`Minijuego` poblados nada más llamar a `ComenzarDia`). Los reescribí todos y añadí 12 más, específicos del día continuo.

---

## El bucle, antes y después

```
ANTES (4 ventanas fijas)                    AHORA (día continuo, §3.3)
─────────────────────────                   ──────────────────────────
ComenzarDia()                                ComenzarDia()
  → Decision ya poblada                        → agenda las alertas de hoy, a su hora
  → Minijuego ya poblado                        → Decision/Minijuego siguen vacíos
ResolverDecision(id)                         AvanzarReloj(min) / IrAZona(zona)
ResolverMinijuego(resultado)                   → el tiempo pasa; una alerta puede sonar o expirar
ElegirAccionRetro(id)                        AtenderAlerta(id)
TerminarDia(horasExtra)                        → SOLO desde tu escritorio; ahí se abre Decision/Minijuego
                                              ResolverDecision(id) / ResolverMinijuego(resultado)
                                              CerrarJornada()
                                                → solo si no queda nada pendiente
                                              TerminarDia(horasExtra)
                                                → solo si llegaste al cierre
```

**Cuatro llamadas nuevas: `AvanzarReloj`, `IrAZona`, `AtenderAlerta`, `CerrarJornada`.** Y `TerminarDia` gana una condición: **ahora exige que el reloj haya llegado al cierre**. Lanzar antes de las 18:00 lo rechaza con un mensaje explícito.

---

## Lo único que cambió del director (§7.5, literal)

> *«Lo único que cambió al pasar al día continuo es el paso 7: antes agendaba un **día**, ahora agenda un **(día, minuto)**. Los otros siete pasos son idénticos.»*

`SorteoDeMinuto.Elegir` hace exactamente eso: toma el mismo `DeterministicRng` que ya eligió el evento y saca de ahí el minuto. **Gasta exactamente una tirada** (hay un test que lo cuenta), así que el Modo Aula sigue siendo reproducible **al minuto** — dos estudiantes con la misma semilla no solo sufren las mismas crisis, las sufren a la misma hora.

Dos garantías que el sorteo impone y que un director ingenuo pasaría por alto:

- **El minuto siempre deja caber la ventana de atención entera.** Si una alerta pudiera sonar a las 17:00 con tres horas de plazo, tendría de hecho una hora, y el jugador sería castigado por la hora a la que el azar decidió llamarle. Todas las alertas valen lo mismo.
- **Cae en múltiplos de la granularidad** (15 min por defecto). `09:30` se lee mejor que `09:37`, y no le quita nada al azar: con 500 tiradas de prueba, todos los huecos disponibles salen.

---

## Las cuatro llamadas nuevas

### `AvanzarReloj(minutos)` — el corazón de todo

Es el único sitio, junto con `IrAZona` y `CerrarJornada`, donde el tiempo del día se mueve — y los tres pasan por aquí.

```
desde = reloj.Minuto
reloj.Avanzar(minutos)
SuenanEntre(desde, reloj.Minuto)   → las que empiezan a sonar en este tramo
Expirar(reloj.Minuto, zonaActual)  → las que se perdieron, YA con su consecuencia aplicada
```

★ **El reloj no se pausa mientras el jugador decide si atender una alerta.** El M8 lo advierte: si el mundo se congelara, la decisión no costaría nada. Solo se pausa **dentro** de la escena, una vez que `AtenderAlerta` ya cobró el desplazamiento y el tiempo de resolverla.

### `IrAZona(id)` — moverse cuesta, y las puertas se respetan

Cobra `CosteDeVisitar` (A′3) a través de `AvanzarReloj`, así que ir lejos puede hacer que una alerta suene o expire **por el camino**. Antes de moverte, comprueba la puerta:

```csharp
private bool PuertaAbierta(PuertaDeZona puerta) {
    if (puerta == null || puerta.SiempreAbierta) return true;
    if (!ConditionEvaluator.EvaluarTodas(puerta.Precondiciones, this)) return false;
    if (!string.IsNullOrEmpty(puerta.TrasBeat)) {
        var beat = _narrativa.PorId(puerta.TrasBeat);
        if (beat == null || !_narrativa.YaSalio(beat, R)) return false;
    }
    return true;
}
```

Reutiliza `ConditionEvaluator` (M2) y `NarrativeDirector.YaSalio` (M8) tal cual estaban. **Cero código nuevo en esos dos paquetes.**

### `AtenderAlerta(id)` — solo desde tu escritorio

```csharp
var ancla = Perfil.Mapa != null && !Perfil.Mapa.Vacio ? Perfil.Mapa.Ancla : null;
if (ancla != null && !string.Equals(R.ZonaActual, ancla.Id, ...))
    throw new InvalidOperationException("Las alertas solo se atienden desde tu escritorio.");
```

Si el nivel **no tiene mapa** (como el prólogo, o los fixtures de la mayoría de los tests de este archivo), `ancla` es `null` y la exigencia desaparece sola: todo ocurre en un escritorio implícito. Es la misma regla de A′3 — *«un nivel puede no tener mapa»* — funcionando aquí sin que nadie tuviera que tocarla.

### `CerrarJornada()` — el gate ya vivía en A′2

```csharp
if (!SePuedeCerrarLaJornada)
    throw new InvalidOperationException("Todavia queda algo pendiente hoy: ...");
```

`GameSession` no reimplementa la regla: delega en `ColaDeAlertas.SePuedeCerrarLaJornada` de A′2. Saltar al cierre **nunca se come una consecuencia**.

---

## La omisión: lo que pasa cuando nadie va

Cuando una alerta expira, `AplicarOmision` la reparte en dos caminos, y son deliberadamente distintos:

### Un minijuego omitido reutiliza `PuenteDelMotor.Omitido` (M7)

Ya existía. La única novedad es **quién lo llama**: antes lo llamaría la UI al notar que el reloj de la escena llegó a cero; ahora `GameSession` lo dispara solo, con `consecuencia = null` (el fallback genérico: *«No llegaste a mirarlo. Decidir no mirar también es decidir»*). La consecuencia **específica** de cada escena —con personajes con nombre, como *«Javier: esto lo arreglo yo esta noche»*— vive en el JSON de la escena, que solo la UI lee; el motor garantiza que la consecuencia mecánica ocurre, no elige la frase.

### Una decisión omitida es nueva, y no tiene bloque `consecuencias.omitido`

A diferencia de los minijuegos, un `EventDefinition` no trae una consecuencia declarada para «nadie decidió». La decisión de diseño: **no se aplica ningún efecto de ninguna opción** —nadie eligió, así que no hay opción que aplicar— pero sí queda constancia:

```
Origen: el evento    OpcionId: "omitido"    Veredicto: incorrecta
Razon: "No llegaste a tiempo. Alguien decidio por ti."
```

Y el evento se marca resuelto (`RegistrarDisparo`), así que su enfriamiento arranca igual que si hubieras contestado. **El mundo queda igual que haberla resuelto mal, nunca igual que si no hubiera pasado nada** — que es la frase exacta del M8.

---

## Lo que encontré al escribir los tests, y por qué importa

Las primeras versiones de cinco tests usaban `Assert.Ignore("esta semilla no agendó nada el día 1")` como salida cuando la semilla fija no producía una alerta el primer día. **Corrí la suite y los cinco se saltaron — cero verificación real.**

La causa es estructural, no mala suerte: `EventDirector.EventoDeHoy` solo devuelve algo si un **día anterior** ya lo agendó (§7.5), y el día 1 no tiene día anterior. Probar «solo el día 1» estaba condenado a fallar en silencio la mayoría de las veces.

La corrección: un helper `HastaUnDiaConAlerta` que juega días con normalidad **hasta que uno trae de verdad la alerta que el test necesita**, y solo entonces deja de avanzar. Los cinco tests pasaron a verificar algo real — incluidos los dos de la omisión, que son los que sostienen la frase del M8.

Una segunda cosa, ésta con impacto real: en `Con_mapa_las_alertas_solo_se_atienden_desde_el_escritorio` y en el test de restaurar una alerta viva, un `AvanzarReloj(600)` de golpe hacía que la alerta **sonara y expirara en la misma llamada** — porque `SuenanEntre` y `Expirar` corren dentro de la misma `AvanzarReloj`, y ambos operan sobre el mismo objeto `Alerta` por referencia. El test capturaba una alerta que, al mirarla un instante después, ya estaba expirada. La corrección: avanzar en pasos pequeños (15–30 min) y parar en cuanto algo suena, en vez de saltar el día entero de golpe.

---

## El puente al guardado: la versión difícil de INV-7

`Capturar()` ahora incluye `Alertas = _alertas.Copia()`. `Restaurar()` hace tres cosas nuevas, en este orden:

```
1 · _reloj.Restaurar(R.MinutoDelDia, R.JornadaProrrogada)   ← el minuto exacto en que se guardó
2 · _alertas.Restaurar(nivel.Alertas)                        ← con su estado: pendiente, atendida o expirada
3 · recuperar _eventoDeHoy / _minijuegoDeHoy de las alertas AUN PENDIENTES
```

El paso 3 es el que hace que `AtenderAlerta` siga funcionando tras recargar: sin recuperar a qué `EventDefinition` o `MinigameDefinition` apunta una alerta viva, atenderla tras restaurar fallaría al buscar el evento.

**Un límite que ya existía y no se cierra aquí:** si el jugador guarda con un panel de `Decision` o `Minijuego` ya **abierto** (después de `AtenderAlerta`, antes de resolver) y recarga, el panel no reaparece solo — hay que volver a `AtenderAlerta` la misma alerta, que sigue pendiente. Esa limitación **ya existía en el `GameSession` de M9**, antes del día continuo: los DTOs `Decision`/`Minijuego` nunca viajaron en el guardado. No es una regresión de A′4; se documenta aquí para que quede escrito en un solo sitio.

### El test que lo prueba de verdad

`Guardar_y_recargar_a_mitad_del_dia_con_una_alerta_viva_da_lo_mismo` es la versión dura de INV-7: no guarda **entre** dos días (como hacía el test de M9), sino **a mitad de uno**, con el reloj en cualquier minuto y alertas en cualquier estado. Compara una partida jugada de un tirón contra la misma partida cortada a los 150 minutos, serializada a JSON de verdad, restaurada y continuada — y exige que el minuto del reloj, el `WorldState` y el número de entradas de la traza coincidan exactamente.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `ComenzarDia()` | — | `DayBrief`; agenda las alertas de hoy, no las presenta |
| `AvanzarReloj(min)` | minutos a avanzar | `ResultadoDeAvance`: lo que sonó, lo que expiró (ya con su consecuencia aplicada) |
| `IrAZona(id)` | id de zona | ídem; **lanza** si la zona no existe, la puerta está cerrada, o el nivel no tiene mapa |
| `AtenderAlerta(id)` | id de alerta | ídem; **lanza** si no estás en tu escritorio, la alerta no sonó, o ya se resolvió |
| `CerrarJornada()` | — | `ResultadoDeAvance`; **lanza** si queda algo pendiente |
| `TerminarDia(horasExtra)` | — | nada; **lanza** si aún no son las 18:00 |
| `MinutoDelDia`, `HoraActual`, `ZonaActual`, `AlertasDeHoy`, `SePuedeCerrarLaJornada` | — | propiedades de solo lectura, para que la UI consulte en vivo |

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 444 en verde** (eran 421). `GameSessionTests` debe dar **44/44** y `SorteoDeMinutoTests` **11/11**.

### Prueba 2 — la manual: un día entero, con mapa, hora a hora

Crea `Assets/Scripts/Editor/PruebaA4.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Eventos;
using Nexus.Core.Jornada;
using Nexus.Core.Metodologia;
using Nexus.Core.Minijuegos;
using Nexus.Core.Modelo;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using UnityEditor;
using UnityEngine;

public static class PruebaA4 {
    [MenuItem("Nexus/Pruebas/A4 · Un dia entero con mapa")]
    public static void Correr() {
        var catalogo = Catalogo();
        var flags = new FlagStore(new Dictionary<string, double>(), catalogo.Flags);
        flags.Inicializar();

        var s = new GameSession(catalogo, "nivel-01", flags, 4417);
        s.ElegirMetodologia("scrum", "dominio_pequeno");
        s.RepartirCalidad(new Dictionary<string, int> { { "seguridad", 0 }, { "fiabilidad", 8 } });
        s.ElegirArquitectura("monolito", "dominio_pequeno");
        s.CerrarFase1();

        for (var dia = 1; dia <= 20; dia++) {
            var brief = s.ComenzarDia();
            var texto = $"===== DIA {dia} ({brief.EtiquetaUnidad}) =====\n";
            texto += $"  agendadas hoy: {string.Join(", ", s.AlertasDeHoy.Select(a => $"{a.Id}@{RelojDeJornada.Formatear(a.MinutoDeLaAlerta)}"))}\n";

            if (s.PendingPlanning != null) { s.Comprometer(s.PendingPlanning.CapacidadSugerida); texto += "  compromiso del sprint\n"; }

            var zonas = new[] { "bullpen", "cafeteria" };
            var zi = 0;

            for (var vueltas = 0; vueltas < 40 && !s.SePuedeCerrarLaJornada; vueltas++) {
                var antes = s.HoraActual;
                var avance = s.AvanzarReloj(45);

                foreach (var expirada in avance.AlertasQueExpiraron)
                    texto += $"  {s.HoraActual}  *** SE PERDIO {expirada.Id} ***\n";

                if (avance.AlertasQueSuenan.Count > 0 && s.ZonaActual != null) {
                    texto += $"  {antes} -> {s.HoraActual}  vuelves al escritorio a atender\n";
                    s.IrAZona(null);   // no aplica aqui; se atiende directo si no hay mapa configurado
                }

                foreach (var alerta in avance.AlertasQueSuenan) {
                    texto += $"  {s.HoraActual}  suena {alerta.Id} [{alerta.Canal}] \"{alerta.Texto}\"\n";
                    try {
                        s.AtenderAlerta(alerta.Id);
                        texto += $"  {s.HoraActual}  atendida\n";
                    } catch (System.InvalidOperationException) { /* no estabas en el escritorio */ }
                }

                if (s.Decision != null) {
                    var elegible = s.Decision.Opciones.First(o => !o.Bloqueada);
                    s.ResolverDecision(elegible.Id);
                    texto += $"  -> resuelta: \"{elegible.Texto}\"\n";
                }
                if (s.Minijuego != null) {
                    s.ResolverMinijuego(new ResultadoMinijuego {
                        MinijuegoId = s.Minijuego.MinijuegoId, Resultado = ResultadosDeMinijuego.Parcial,
                        Rubrica = new Rubrica { Veredicto = "aceptable", Oa = "OA-GIT-01", Razon = "Casi." },
                        EfectosInmediatos = { { "DeudaTecnica", 3f } }
                    });
                    texto += "  -> minijuego resuelto\n";
                }
            }

            s.CerrarJornada();
            texto += $"  {s.HoraActual}  jornada cerrada. ";
            var horasExtra = dia % 4 == 0;
            s.TerminarDia(horasExtra);
            texto += horasExtra ? "SE QUEDA\n" : "se va a casa\n";
            Debug.Log(texto);
        }

        Debug.Log("===== DASHBOARD =====\n" + s.Cerrar().Final);
    }

    private static Catalogo Catalogo() {
        // usa el mismo fixture que GameSessionTests: copia Ev(), Nivel(), Scrum(), Censo() desde ahi
        // o carga StreamingAssets si ya tienes el contenido de B1
        return null; // completar con tu catalogo de prueba
    }
}
```

**Aviso:** este script necesita el mismo fixture de catálogo que `GameSessionTests.cs` (los métodos `Ev`, `Nivel`, `Scrum`, `Censo`). Cópialos a una clase auxiliar, como hicimos en M9 — o, si ya tienes contenido real en `StreamingAssets` de B1, carga desde ahí con `CatalogLoader.CargarTodo`.

**Qué debes ver, y qué significa:**

- Cada día imprime **qué alertas se agendaron y a qué hora**, antes de que suene ninguna. Eso es lo que el director ya decidió al empezar el día — la partida ya está «escrita», solo falta que pase.
- Los `suena` y los `atendida` llegan **en el orden del reloj**, no en el orden en que el código los procesa.
- Si alguna vez ves `*** SE PERDIO ***`, es que el jugador (el script) no llegó a tiempo — y la traza de esa alerta dirá `incorrecta`.
- El último día imprime el `Final` del Dashboard: `completado` o `fallado`.

Cuando termines, **borra `PruebaA4.cs`**.

---

## La Fase A′ queda cerrada

| Parada | Qué | Tests |
|---|---|---|
| A′1 | El reloj de la jornada | 22 |
| A′2 | Alertas | 24 |
| A′3 | Mapa de zonas | 27 |
| A′4 | `GameSession` con el día continuo | 23 |
| | **total del retrofit** | **96** |

Sumado a los 348 de la Fase A: **444 tests**, y el motor ahora juega con el bucle que de verdad quieres construir — un día que corre solo, alertas que hay que ir a atender, un mapa que cuesta recorrer, y la misma disciplina de siempre: nada se dispara sin avisar, nada se escribe salvo por la puerta que toca, y recargar nunca cambia la partida.

## Lo que falta

- **No hay contenido real.** `StreamingAssets/NexusLite/` sigue vacío salvo `MJ-F2-02.json`. Los niveles de prueba de este documento son fixtures — el contenido real de N0 y N1, con sus zonas, sus eventos y sus beats, es **B1**.
- **No hay capa Unity.** Nadie llama a `AvanzarReloj` desde un `Update()`, no hay pantalla que muestre el reloj corriendo, no hay clic para ir a una zona. Eso es **B2** y la Fase C.
- **El panel abierto no sobrevive a un guardado**, como se explicó arriba. No es nuevo de A′4, pero sigue sin resolverse; si en algún momento se vuelve un problema real (un jugador que guarda todo el tiempo con un panel abierto), se puede cerrar persistiendo el `PendingDecision`/`PendingMinigame` actual en `NivelEnCurso`.
