# M9 · `GameSession` — el hub

> `nexus-motor-especificacion-tecnica.md` §4.9, §5.3, §5.7, §7.6.
> Estado: **implementado y probado**. 32 tests nuevos, todos en verde.
> Depende de **todos** los módulos anteriores. **Cierra la Fase A.**

---

## Qué se hizo

| Archivo | Clases |
|---|---|
| `Core/Sesion/Dtos.cs` | `DayBrief`, `PlanningRequest`, `RetroRequest`, `OpcionPresentada`, `PendingDecision`, `LaunchResult`, `DebriefReport` |
| `Core/Sesion/GameSession.cs` | `GameSession` |
| `Core/Sesion/FabricaDeSesion.cs` | `FabricaDeSesion` |
| `Tests/editMode/GameSessionTests.cs` | 32 tests |

**Cero ediciones a código existente.** Es la primera vez desde M6 que un módulo no toca nada de lo anterior, y no es casualidad: todo lo que `GameSession` necesitaba ya estaba declarado.

---

## Lo que demuestra este módulo

`GameSession` implementa **cuatro puertos que otros paquetes declararon antes de que esta clase existiera**:

| Puerto | Lo declaró | En qué módulo |
|---|---|---|
| `IStateContext` | los servicios, para que el director no toque el estado | M2 |
| `ISesionPersistible` | la persistencia, que ya estaba escrita en `origin/main` | antes de empezar |
| `IFabricaDeSesion<T>` | ídem | antes de empezar |
| `IMetricasDelNivel` | la evaluación pedagógica, ídem | antes de empezar |

**Ninguno de esos paquetes conoce `GameSession`.** Por eso se pudieron escribir y probar meses antes de que existiera el hub. La topología es **estrella, no malla**: si aparece una flecha entre dos subsistemas que no pasa por aquí, está mal — con la única excepción del `EventDirector`, que sí habla con el evaluador de condiciones, el azar, la agenda y las reglas de metodología.

---

## El día, en el orden exacto del §5.3

```
ComenzarDia()
 1 · R.DiaActual++ · PlanFor(día)
 2 · Vencidos(día)       ← lo que se cobra hoy de decisiones viejas, y las cadenas que encadena
 3 · EfectosDeEtapa      ← lo que la etapa cobra cada día, pase lo que pase
 4 · Ceremonias          ← cuestan días de proyecto, y abren planning o retro
 5 · EventoDeHoy() o TickSeleccion()
 6 · MinijuegoDeHoy()
 7 · AvisosDeHoy()       ← los telegrafiados de eventos FUTUROS
 8 · BeatDeHoy()
 9 · Calcular() → DayBrief
```

**El orden no es decorativo.** Los diferidos vencen **antes** de que el director elija, porque lo que se cobra hoy cambia lo que hoy es probable: si la deuda sube a 45 esta mañana, el evento que necesita `DeudaTecnica > 40` pasa a ser candidato esta misma mañana.

Y `TerminarDia(horasExtra)` sigue siendo **el único punto del motor donde el tiempo avanza**.

## El orden de rehidratación del §7.6, que no es negociable

```
1 · DeterministicRng.Restaurar(semilla, consumos)   ← quemar las tiradas gastadas
2 · EffectScheduler.Restaurar(cola, telegrafiados)
3 · W, R, Coef, Traza, Competencia + los privados
4 · Metodología, Reglas, EventDirector, MinigameDirector   ← dependen de 1 y 2
5 · PlanFor(díaActual)
```

★ **Lo que NO se hace:** volver a llamar a `ElegirMetodologia`, `RepartirCalidad` ni `ElegirArquitectura`. Sus efectos ya están dentro de `W` y de `Coef`.

La **única excepción** son los pesos por tag del reparto de calidad, y por un motivo concreto: viven en el `LevelProfile`, que se recarga limpio del catálogo en cada arranque. Hay un test para cada mitad de esa frase.

---

## El bug que INV-7 cazó, y por eso la invariante existe

Escribí `ReaplicarPesosDeCalidad()` para el punto anterior, y de paso metí dentro esto:

```csharp
if (fichas > 0 && !string.IsNullOrEmpty(atributo.CoeficienteAfectado))
    Coef.MultiplicarUno(atributo.CoeficienteAfectado, Math.Pow(0.93, fichas));
```

Parecía razonable: invertir en fiabilidad también mejora `alpha`. **Pero `Coef` viaja en el guardado**, así que ese descuento ya estaba dentro al restaurar — y volver a aplicarlo lo regalaba **otra vez, en cada recarga**.

El test de INV-7 lo cazó al instante: la misma partida daba `DeudaTecnica=13.7` de un tirón y `11.2` guardando y recargando. Un jugador que recargara cinco veces acabaría con la mitad de deuda que uno que no lo hiciera. **Sin dar error nunca**, que es lo que lo hace peligroso.

La corrección: `ReaplicarPesosDeCalidad()` toca **solo** los pesos por tag; el descuento de coeficiente se aplica una vez, en `RepartirCalidad()`.

> Eso es exactamente lo que la especificación avisaba en el §7.6: *«reaplicarlos daría cobertura gratis en cada recarga sin dar error»*. Lo avisaba, lo escribí igual, y el test lo cazó. Por eso está el test.

## Y un test que no probaba nada

`Semillas_distintas_producen_partidas_distintas` falló, pero **el fallo era del test**. Mis ocho eventos de prueba eran clones con **efectos idénticos**: daba igual cuál saliera, el estado final era el mismo. El azar sí estaba decidiendo; el fixture no podía notarlo.

Lo arreglé dando a cada evento un coste distinto. Lo dejo escrito porque es una trampa fácil de repetir al escribir el contenido real: **si dos eventos cuestan lo mismo, que salga uno u otro no enseña nada.**

---

## INV-6: el puente ocurre una vez

El volcado de stocks a `FLG_*` ocurre **dentro de `Cerrar()` y en ningún otro sitio**. Hay un test que juega veinte días completos y comprueba que los flags **siguen intactos**, y que solo cambian al cerrar.

Es el momento en que el proyecto se convierte en biografía.

---

## Entrada y salida

| Fase | Llamada | Qué devuelve |
|---|---|---|
| **1** | `MetodologiasDisponibles()` | las que el nivel permite |
| | `ElegirMetodologia(id, razónId)` | reescribe 8 cosas del motor; anota el veredicto de la razón |
| | `RepartirCalidad(fichas)` | ★ no invertir **sube** el peso de ese tag en el director |
| | `ElegirArquitectura(id, razónId)` | aplica efectos y modificadores |
| | `CerrarFase1()` | arranca el bucle diario; anota `coberturaAlCerrarDiseno` |
| **2** | `ComenzarDia()` | `DayBrief`; deja puestos `Decision`, `Minijuego`, `Beat`, `PendingPlanning`, `PendingRetro` |
| | `Comprometer(puntos)` | fija el compromiso y el **sobrecompromiso** |
| | `ResolverDecision(opciónId)` | aplica, encola diferidos, escribe traza y rúbrica |
| | `ResolverMinijuego(resultado)` | ídem, por el puente de M7 |
| | `ElegirAccionRetro(id)` | ★ cambia un coeficiente del modelo, para siempre |
| | `TerminarDia(horasExtra)` | ★ el único sitio donde avanza el tiempo |
| **3/4** | `EjecutarLanzamiento()` | `LaunchResult` |
| | `Cerrar()` | `DebriefReport` + **el puente a flags** |
| — | `Capturar()` / `Restaurar(...)` | el guardado |

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 348 en verde** (eran 316). `GameSessionTests` debe dar **32/32**.

El que importa se llama `Diez_dias_seguidos_dan_lo_mismo_que_cinco_guardar_recargar_y_cinco`. Hace el viaje completo: juega 5 días, captura, **serializa a JSON**, deserializa, restaura y juega 5 más — y compara el `DebriefReport` entero contra el de una partida jugada del tirón.

### Prueba 2 — la manual: un nivel entero, de principio a fin

Crea `Assets/Scripts/Editor/PruebaM9.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Datos;
using Nexus.Core.Minijuegos;
using Nexus.Core.Narrativa;
using Nexus.Core.Sesion;
using UnityEditor;
using UnityEngine;

public static class PruebaM9 {
    [MenuItem("Nexus/Pruebas/M9 · Un nivel entero")]
    public static void Correr() {
        // Usa el catalogo de CatalogosTests como plantilla. Si ya tienes contenido en
        // StreamingAssets/NexusLite, cambia esto por: CatalogLoader.CargarTodo(new CatalogoDeArchivos(ruta))
        var catalogo = CatalogoDeEjemplo.Construir();
        var flags = new FlagStore(new Dictionary<string, double>(), catalogo.Flags);
        flags.Inicializar();

        var s = new GameSession(catalogo, "nivel-01", flags, 4417);

        Debug.Log("=== FASE 1 ===");
        foreach (var m in s.MetodologiasDisponibles()) Debug.Log("  disponible: " + m.Nombre);
        s.ElegirMetodologia("scrum", "dominio_pequeno");
        s.RepartirCalidad(new Dictionary<string, int> { { "seguridad", 0 }, { "fiabilidad", 8 } });
        Debug.Log($"  peso del tag 'seguridad' tras no invertir: " +
                  $"{s.Perfil.Director.PesosPorTag["seguridad"]:F1}  <- acabas de elegir que crisis sufrir");
        s.ElegirArquitectura("monolito", "dominio_pequeno");
        s.CerrarFase1();

        Debug.Log("=== FASE 2 ===");
        for (var dia = 1; dia <= s.Perfil.DiasTotales; dia++) {
            var brief = s.ComenzarDia();
            var linea = $"DIA {brief.Dia,2} [{brief.EtiquetaUnidad}]  ";

            foreach (var origen in brief.EfectosQueVencieronHoy) linea += $"[SE COBRA {origen}] ";
            foreach (var aviso in brief.Avisos) linea += aviso + " ";
            if (brief.Ceremonias.Count > 0) linea += "(" + string.Join(", ", brief.Ceremonias) + ") ";

            if (s.PendingPlanning != null) {
                var capacidad = s.PendingPlanning.CapacidadSugerida;
                s.Comprometer(capacidad + 4);                       // el jugador promete de mas
                linea += $"COMPROMISO {capacidad + 4:F0} (cabian {capacidad:F0}) ";
            }

            if (s.Decision != null) {
                var opcion = s.Decision.Opciones.First(o => !o.Bloqueada);
                linea += $">>> {s.Decision.Titulo}: \"{opcion.Texto}\" ";
                s.ResolverDecision(opcion.Id);
            }

            if (s.Minijuego != null) {
                linea += $"[15:00 {s.Minijuego.MinijuegoId}] ";
                s.ResolverMinijuego(new ResultadoMinijuego {
                    MinijuegoId = s.Minijuego.MinijuegoId, Resultado = ResultadosDeMinijuego.Parcial,
                    Rubrica = new Rubrica { Veredicto = "aceptable", Oa = s.Minijuego.ObjetivoAprendizaje,
                                            Razon = "Se te escapo algo." },
                    EfectosInmediatos = { { "DeudaTecnica", 3f } }
                });
            }

            if (s.PendingRetro != null) {
                var accion = s.PendingRetro.Acciones[0];
                linea += $"[RETRO: {accion.Texto}] ";
                s.ElegirAccionRetro(accion.Id);
            }

            if (s.Beat != null) linea += $"[CINE {s.Beat.BeatId} · {s.Beat.Variante}] ";

            var horasExtra = dia % 3 == 0;
            s.TerminarDia(horasExtra);
            Debug.Log(linea + (horasExtra ? "| SE QUEDA " : "| a casa  ") +
                      $"| av {s.W.Avance:F0}/{s.W.Alcance:F0} deuda {s.W.DeudaTecnica:F0} " +
                      $"moral {s.W.MoralEquipo:F0} salud {s.W.SaludJugador:F0}");
        }

        Debug.Log("=== FASE 3 ===");
        var lanzamiento = s.EjecutarLanzamiento();
        Debug.Log($"  {(lanzamiento.Exito ? "SALIO BIEN" : "SALIO MAL")} · riesgo {lanzamiento.RiesgoDeLanzamiento:F1} · " +
                  $"entregado {lanzamiento.AlcanceEntregado:F0}/{lanzamiento.AlcanceComprometido:F0} · " +
                  $"{lanzamiento.DefectosEscapados} defectos escapados");

        Debug.Log("=== FASE 4 · DASHBOARD DE LECCIONES ===");
        var d = s.Cerrar();
        Debug.Log($"1 · El release: {(d.Lanzamiento.Exito ? "exito" : "fallo")}");
        Debug.Log($"2 · Como termino: {d.Final}" +
                  (d.UmbralesFallados.Count == 0 ? "" : "\n     " + string.Join("\n     ", d.UmbralesFallados)));
        Debug.Log($"3 · Tu metodologia: {d.Metodologia.MetodologiaNombre} · " +
                  $"razon valida: {d.Metodologia.RazonValida} · era adecuada: {d.Metodologia.EraAdecuada} " +
                  $"(volatilidad real {d.Metodologia.VolatilidadReal:F0})");
        foreach (var p in d.Metodologia.Practicas)
            Debug.Log($"     {(p.Cumple ? "OK " : "NO ")} {p.Descripcion}: {p.Valor:F1} {p.Comparador} {p.Objetivo:F0} — {p.Razon}");
        Debug.Log("4 · Competencia: " + string.Join(" · ",
            d.Competencia.PorObjetivo.Select(kv => $"{kv.Key} {kv.Value.Puntuacion:F0}%")));
        Debug.Log("5 · Cadena causal:\n     " + string.Join("\n     ", d.CadenaCausal));
        Debug.Log($"6 · Decisiones: {d.Traza.Entradas.Count} en la traza");
        Debug.Log("7 · Lo que se lleva la partida:\n     " + string.Join("\n     ",
            d.Flags.Select(kv => $"{kv.Key} = {kv.Value:F0}")));
    }
}
```

Te hace falta un `CatalogoDeEjemplo.Construir()`. **Lo más rápido es copiar el fixture de los tests**: abre `Tests/editMode/GameSessionTests.cs`, copia los métodos `Nivel()`, `Scrum()`, `Kanban()`, `Ev()`, `Censo()` y `Catalogo()` a una clase pública `CatalogoDeEjemplo` en `Assets/Scripts/Editor/`. Es temporal: en B1 esto lo sustituye el contenido real de N0 y N1 leído de `StreamingAssets`.

**Qué debes ver, y qué significa:**

- **Fase 1** — la línea del peso del tag `seguridad` pasando a **2.0** por no invertir. Nada en la pantalla se lo dice al jugador.
- **Los veinte días** — busca el patrón: un `[log] "..."` y, **dos días después**, su `>>>`. Y varios días más tarde, un `[SE COBRA ...]`. Las tres piezas de P4, en una sola columna.
- **La retro del día 10 y del 20**, cambiando `kappa`. A partir de ahí la documentación se diluye más despacio, y no lo notarás salvo que compares dos partidas.
- **`COMPROMISO 34 (cabían 30)`** — y a partir de ese día la deuda sube más rápido, todos los días, hasta que el sprint cierre.
- **El Dashboard con sus siete secciones.** Fíjate en la 3: *«era adecuada: False»* porque la volatilidad real del nivel es 25 y Scrum es ágil. **Eligió bien por buen motivo, y aun así no encajaba.** Eso es exactamente lo que el juego quiere enseñar: no hay modelo mejor, hay modelo adecuado.

Cuando termines, **borra `PruebaM9.cs`** y la clase de ejemplo.

---

## La Fase A queda cerrada

| Módulo | Subsistema | Tests |
|---|---|---|
| M1 | C1 Núcleo de Estado | 24 |
| M2 | C10a Servicios deterministas | 33 |
| M3 | C4 Cola Temporal | 20 |
| M4 | C5 Reglas de Metodología | 31 |
| M5 | C3 Dirección de Eventos | 31 |
| M6 | C10b Datos y catálogos | 46 |
| M7 | C6 Ventana de Verbos | 31 |
| M8 | C7 Canal Narrativo | 34 |
| M9 | Core.Session · `GameSession` | 32 |
| — | los que ya estaban (C2, C8, C9, V1) | 66 |
| | **total** | **348** |

**Las siete invariantes, todas con test:**

| | | Dónde |
|---|---|---|
| INV-1 | ningún evento escribe un `FLG_*` | M2 (runtime) + M6 (al cargar) |
| INV-2 | solo `EffectApplier` escribe el `WorldState` | M2 |
| INV-3 | el director agenda, nunca dispara | M5 (`EventoDeHoy` lanza) + M6 (exige telegrafiado) |
| INV-4 | nadie sortea sin `DeterministicRng` | M2 |
| INV-5 | validación al cargar, o no se carga | M6 |
| INV-6 | el puente a flags ocurre en un solo sitio | M9 |
| INV-7 | recargar no puede cambiar la partida | M3 (parcial) + **M9 (completo)** |

## Lo que falta para jugar

- **No hay contenido real.** `StreamingAssets/NexusLite/` sigue vacío salvo `MJ-F2-02.json`. Eso es **B1**.
- **No hay capa Unity.** `AppRoot`, `ScreenRouter`, `LevelRunner`, `NexusTheme`, `UiKit`. Eso es **B2**.
- **`Hallazgos → FLG_*`** sigue sin puente: hace falta la tabla del contenido, que se escribe en B1.
