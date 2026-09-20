# M1 · C1 Núcleo de Estado

> Subsistema C1 de `nexus-motor-especificacion-tecnica.md` (§4.2, §4.3.3).
> Estado: **implementado y probado**. 24 tests nuevos, todos en verde.

---

## Qué se hizo

Cinco archivos nuevos en `game/proyecto de grado/Assets/Scripts/Core/Modelo/` y un archivo de tests.
**Cero ediciones a código existente**: M1 es puramente aditivo, así que si algún test de los 66 anteriores se pusiera rojo, sería un problema de tu entorno y no de este módulo.

| Archivo | Clases | Para qué |
|---|---|---|
| `Core/Modelo/WorldState.cs` | `WorldState` | Los 13 stocks del proyecto + `VelocidadMod` |
| `Core/Modelo/RuntimeState.cs` | `RuntimeState`, `Estimacion`, `EstadoArtefacto` | El estado de ejecución del nivel |
| `Core/Modelo/LevelProfile.cs` | `LevelProfile`, `DirectorConfig`, `Umbrales`, `UmbralExito`, `UmbralFallo` | El carácter de un nivel, en datos |
| `Core/Modelo/Fase1Config.cs` | `Fase1Config`, `CalidadConfig`, `AtributoCalidad`, `ArquitecturaOpcion`, `RazonOpcion` | Las tres decisiones fundacionales |
| `Core/Modelo/Diccionarios.cs` | `Diccionarios` (interno) | Arregla una trampa de Newtonsoft (ver abajo) |
| `Tests/editMode/NucleoDeEstadoTests.cs` | 24 tests | La red de seguridad |

---

## Qué hace

### `WorldState` — «cómo va el proyecto»

Los 13 stocks con sus rangos. Se **reinicia en cada nivel**: lo que sobrevive entre niveles son los `FLG_*`, que llegan en M8.

| Stock | Inicial | Regla |
|---|---|---|
| `Dias`, `Alcance`, `Avance` | 0 / perfil / 0 | nunca bajan de 0, **sin techo** |
| `Dinero` | perfil | **puede ser negativo** — estar en números rojos es un estado válido |
| `DeudaTecnica`, `Cobertura`, `Documentacion` | perfil | 0–100 |
| `MoralEquipo`, `Cansancio`, `Competencia` | 60 / 10 / 50 | 0–100 |
| `SatisfaccionCliente`, `Reputacion` | 50 / 50 | 0–100 |
| `SaludJugador` | 80 | 0–100 |
| `VelocidadMod` | 1.0 | suelo **0.1**, nunca 0 |

El acceso es **por nombre** (`TryGet("DeudaTecnica", out v)`), no por propiedad tipada. Es deliberado: permite que un JSON de contenido diga `"DeudaTecnica": 12` sin obligar a un `switch` en cada consumidor. El precio es que un nombre mal escrito no lo caza el compilador, así que `Set()` lanza nombrando el stock inválido **y listando los válidos**.

### `RuntimeState` — «cómo vas tú jugándolo»

Reloj, estado del director, comportamiento, registro pedagógico y las series de los tableros.

Dos decisiones que conviene que conozcas:

- **`TryGet` expone solo 10 campos**, no todos. Es a propósito (§4.3.3): abrir el objeto entero convertiría cualquier detalle de implementación en contrato de contenido, y luego no se podría cambiar sin romper JSONs. Los consultables son `diaActual`, `fase`, `wipActual`, `limiteWip`, `sobreCompromiso`, `diasConHorasExtra`, `diasSeguidosTrabajando`, `cambiosAceptados`, `vecesQueSeFueACasa`, `accionesRetroElegidas`.
- **La cola de diferidos y los telegrafiados NO viven aquí.** El §5.7 los lista bajo `RuntimeState`, pero el §4.4.4 dice que los posee `EffectScheduler`, y `NivelEnCurso` ya los guarda por separado. Tener dos dueños de la agenda del tiempo sería la peor fuente de bugs posible, así que mandan el §4.4.4 y el código existente. Llegan en M3.

`IContadoresDeSimulacion` está implementado **de forma explícita**, para que el contrato «ForresterModel nunca escribe contadores» sea literal y no solo un comentario: por la referencia de clase esos campos se escriben, pero por el puerto solo se leen.

### `LevelProfile` — el carácter del nivel, en datos

Los 24 parámetros del §4.2.3 en 4 grupos. Aquí no hay ni una regla de juego: cambiar estos números cambia por completo cómo se siente un nivel **sin recompilar**. Es la regla de balanceo del §10.4.

**`Clone()` es profundo**, y eso resuelve un bug real: el reparto de fichas de la Fase 1 reescribe `Director.PesosPorTag`, pero el catálogo se carga **una sola vez** al arrancar. Sin el clon, jugar el mismo nivel dos veces en la misma sesión arrastraría los pesos de la partida anterior. `GameSession` (M9) trabajará siempre sobre un clon.

Un detalle de la especificación que hubo que resolver: el §4.2.1 dice que `Cobertura` empieza «según perfil», pero los 24 parámetros del §4.2.3 solo listan `deudaHeredada` y `documentacionHeredada`. Añadí **`CoberturaHeredada`** (por defecto 0) para que la tabla de stocks sea honrada. Es el parámetro 25.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `WorldState.DesdeNivel(perfil)` | un `LevelProfile` | estado inicial del nivel, ya acotado |
| `WorldState.TryGet(nombre, out v)` | nombre del stock (sin distinguir mayúsculas) | `true` + valor · `false` + 0 si no existe |
| `WorldState.Set(nombre, valor)` | nombre + `double` | nada; **acota antes de asignar**; lanza si el nombre no existe o el valor no es finito |
| `WorldState.ToString()` | — | línea de auditoría para `EstadoAntes`/`EstadoDespues` de la traza, **siempre con punto decimal** |
| `RuntimeState.DesdeNivel(perfil)` | un `LevelProfile` | fase 1, drama copiado, 5 artefactos a cero |
| `LevelProfile.Clone()` | — | copia profunda, independiente |
| `CalidadConfig.FactorDeTag(fichas)` | nº de fichas (0..n) | 0→**2.0** · 1→1.3 · 2→1.0 · ≥3→0.6 |

---

## Dos cosas que encontré y arreglé

**1 · Newtonsoft se come el comparador de los diccionarios.**
Un `Dictionary<string,double>(StringComparer.OrdinalIgnoreCase)` se reconstruye al deserializar con el comparador **por defecto**. No da error: da una búsqueda fallida silenciosa. Un JSON con `"pesosPorTag": {"Equipo": 1.5}` dejaría de encontrarse al buscar `"equipo"`, y el peso del evento se quedaría en 1.0 sin que nadie se entere — justo la clase de bug que INV-7 persigue. Resuelto con `[OnDeserialized]` en `DirectorConfig`, `RuntimeState` y `ArquitecturaOpcion`, y con un test que lo vigila.

**2 · Un valor no finito envenenaría el estado en silencio.**
`Math.Max(0, Math.Min(100, NaN))` devuelve `NaN`. Si un efecto de contenido divide por cero, ese `NaN` entraría en un stock y contaminaría todos los cálculos posteriores sin lanzar nada. `Set()` ahora rechaza `NaN` e infinitos en el acto, con un mensaje que dice de dónde suelen venir.

---

## Cómo probarlo

### Prueba 1 — la automática (es la que cuenta)

1. Abre `game/proyecto de grado/` en Unity **6000.0.82f1**.
2. Espera a que termine de compilar (abajo a la derecha, sin la ruedecita).
3. `Window > General > Test Runner`.
4. Pestaña **EditMode** → botón **Run All**.

**Qué debes ver:** **90 tests, todos en verde.** Eran 66; los 24 nuevos están bajo `Nexus.Tests > NucleoDeEstadoTests`.

Si quieres ver solo los nuevos, despliega `NucleoDeEstadoTests` y pulsa **Run Selected**: deben salir **24/24**.

### Prueba 2 — la manual, para ver el estado con tus ojos

Esta es opcional, pero es la que te deja *ver* que el motor se mueve. Crea un archivo temporal `Assets/Scripts/Editor/PruebaM1.cs`:

```csharp
using Nexus.Core.Modelo;
using Nexus.Core.Simulacion;
using UnityEditor;
using UnityEngine;

public static class PruebaM1 {
    [MenuItem("Nexus/Pruebas/M1 · Diez dias de trabajo")]
    public static void DiezDias() {
        var perfil = new LevelProfile {
            Id = "nivel-01", DiasTotales = 20,
            PresupuestoInicial = 18000, AlcanceInicial = 34,
            CoberturaHeredada = 40, DocumentacionHeredada = 40, VelocidadBase = 3.0
        };

        var w = WorldState.DesdeNivel(perfil);
        var r = RuntimeState.DesdeNivel(perfil);
        Debug.Log("DIA 0  " + w);

        for (var dia = 1; dia <= 10; dia++) {
            var horasExtra = dia <= 5;                       // cinco noches seguidas, luego a casa
            r.DiasSeguidosTrabajando = horasExtra ? dia : 0;
            ForresterModel.AvanzarUnDia(w, r, perfil.Coef, perfil.VelocidadBase, 1.0, horasExtra);
            Debug.Log("DIA " + dia + "  " + w);
        }
    }
}
```

Menú **Nexus > Pruebas > M1 · Diez días de trabajo**, y mira la consola.

**Qué debes ver, y qué significa:**

- **Diez líneas**, una por día, todas con **punto decimal** (nunca coma) — eso es la cultura invariante funcionando.
- `Avance` sube más rápido los 5 primeros días (el +25 % del overtime es el señuelo) y **`DeudaTecnica` sube casi 4 veces más rápido** en esos mismos días: 0.8 × 3.0 con horas extra contra 0.8 × 0.8 sin ellas.
- `SaludJugador` cae **cada vez más rápido** entre el día 1 y el 5 (−1.5, −2.3, −3.1, −3.9, −4.7): es la penalización no lineal. Del día 6 al 10 se recupera a +2.0 fijo. **Tres noches seguidas cuestan mucho más que el triple de una.**
- `Cobertura` y `Documentacion` **bajan solas**, sin que nadie las toque: se diluyen al crecer el producto.
- `Dinero` se queda clavado en 18000.0 — el modelo de Forrester no toca el dinero, eso lo hacen los eventos.
- Ningún valor se sale de 0–100 en los stocks acotados.

Cuando termines, **borra `PruebaM1.cs`**: es un andamio, no forma parte del módulo.

---

## Lo que NO hace todavía (y por qué)

- **`NivelEnCurso.W` y `.R` siguen siendo `JToken`.** El plan original cerraba ese `TODO(C1)` aquí, pero tiparlos obliga a tocar `SaveStore.ValidarNivel` y `PersistenciaTests`, y entonces un test en rojo no te diría si el fallo es del módulo nuevo o de mi edición. Se cierra en **M9**, que es donde de verdad hace falta y donde `PersistenciaTests` se convierte en el test real de INV-7 — su propio comentario ya dice «llegará cuando exista GameSession».
- **Nadie escribe el `WorldState` todavía.** `EffectApplier`, que será el único camino de escritura (INV-2), llega en **M2**.
- **`Fase1Config` está definido pero no se consume.** Lo usará `GameSession.RepartirCalidad` y `ElegirArquitectura` en M9.
