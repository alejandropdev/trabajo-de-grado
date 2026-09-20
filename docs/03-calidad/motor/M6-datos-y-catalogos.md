# M6 · C10b Datos y catálogos

> Subsistema C10 de `nexus-motor-especificacion-tecnica.md` (§S.5, §4.11, §8.3).
> Estado: **implementado y probado**. 46 tests nuevos, todos en verde.
> Depende de M1, M2, M4 y M5.

---

## Qué se hizo

| Archivo | Clases |
|---|---|
| `Core/Datos/ICatalogSource.cs` | `ICatalogSource`, `CatalogoEnMemoria`, `CatalogoDeArchivos` |
| `Core/Datos/SchemaValidator.cs` | `SchemaValidator`, `SchemaException` |
| `Core/Datos/CatalogLoader.cs` | `CatalogLoader`, `Catalogo` |
| `Core/Servicios/VariablesDeSesion.cs` | el vocabulario de `IStateContext` |
| `Tests/editMode/CatalogosTests.cs` | 46 tests, con los **tres JSON de referencia completos** |

**Cero ediciones a código existente.**

---

## Qué hace

### `ICatalogSource` — se cierra la última asimetría del diseño

El §S.5 lo decía con todas las letras: el guardado tenía su puerto (`IAlmacen`) desde el principio, pero el contenido se leía con una llamada directa a `Application.streamingAssetsPath`, **y eso ataba el motor a Unity**. Era la única asimetría que quedaba.

Ahora los tests cargan catálogos de memoria y el arnés de consola los carga de disco, sin que `Nexus.Core` sepa que existe un `StreamingAssets`. El adaptador de Unity es una línea, y llega en la Fase B.

`CatalogoDeArchivos` vive en `Nexus.Core` y no en la capa Unity, y eso es correcto: `noEngineReferences` prohíbe `UnityEngine`, **no `System.IO`**. Lo único que Unity aporta es la ruta base.

Un detalle que parece menor y no lo es: `ListarCatalogos` devuelve la lista **ordenada**. El orden del catálogo de eventos es el orden de la ruleta, así que sin un orden estable la misma semilla dejaría de dar la misma partida según cómo el sistema de archivos devolviera los ficheros.

### `SchemaValidator` — INV-5 hecho real

> **Validación al cargar, o no se carga.**

La regla de oro: un error de contenido tiene que doler **al arrancar**, con el nombre del campo y el id del evento. Si no duele ahí, duele el día 14 de una sesión de laboratorio de 90 minutos, delante de veinte estudiantes, y entonces ya no hay nada que hacer.

**Lo mejor del módulo: también valida las expresiones.** Una precondición que dice `"DeudaTecnia > 40"` (con la errata) se caza al cargar, porque el evaluador se ejecuta contra un `ContextoDeValidacion` que conoce todos los nombres legítimos y ninguno más.

Eso cierra el círculo que dejé abierto en M2: **por eso `ConditionEvaluator` puede permitirse lanzar ante una variable desconocida.** Cuando el juego está corriendo, ya se sabe que no las hay.

Y distingue validar de evaluar: `"DeudaTecnica > 40 - riesgoLatente / 2"` pasa la validación aunque hoy sea falsa. Solo se comprueba que los nombres existan y que la gramática se sostenga.

### Lo que rechaza

| Eventos | id único · nombre · OA · severidad 0–5 · fase del vocabulario · peso > 0 · **telegrafiado con texto y canal** · `diasAntes ≥ 1` · **mínimo dos opciones** · **toda opción con rúbrica Y razón** · veredicto del vocabulario · efectos válidos · **cadenas que apuntan a un evento existente** · **alguna opción con coste diferido** · precondiciones y modificadores de peso parseables |
| Nivel | id · nombre · `diasTotales ≥ 2` · **`presupuestoDrama` con exactamente cuatro valores** · ≥1 metodología permitida · `volatilidadReal` 0–100 · `nivelAndamiaje` 0–3 · briefing no vacío · **exactamente una arquitectura con `esLaAdecuada`** · ≥2 arquitecturas · razones citadas que existen · coeficientes válidos |
| Metodología | todo lo que valida `MethodologyRules` · métricas de las 9 · comparadores válidos · **una razón no puede ser válida y trampa a la vez** · acciones de retro con explicación · efectos válidos en ceremonias y etapas |
| Catálogo | el nivel permite metodologías que existen · **cada metodología sostiene ese nivel con SUS días** |

### Tres decisiones que conviene conocer

**1 · `CargarTodo` parsea primero y valida después, de una vez.**
Si validara archivo a archivo, un autor de contenido con seis errores repartidos tendría que arrancar el juego **seis veces** para verlos. Así los ve todos en el primer intento. Hay un test que lo comprueba con tres errores en dos archivos distintos.

**2 · No duplica las reglas de la metodología: delega en `MethodologyRules`.**
Como prometí en M4. Construir las reglas es lo que de verdad ejercita el perfil — al trocear el calendario se descubre que una etapa no tiene días o que `kappa` está escrito `kapa`. Una sola fuente de verdad.

**3 · Severidad 0 se admite.**
La tabla del §4.11 dice 0–5 y el §8.3.1 dice 1–5. Elegí 0–5: la familia `BUE` del catálogo son **buenas noticias**, y una buena noticia no debería consumir presupuesto de drama.

### Un detalle de carpetas

El §8.4 dice `perfiles/nivel-01.json`. El repo ya tenía creada `Assets/StreamingAssets/niveles/`, así que uso **`niveles/`**. Las cuatro carpetas son `eventos/`, `niveles/`, `metodologias/` y `minijuegos/`, y están declaradas como constantes en `CatalogLoader`.

---

## Los tres JSON de referencia

Están completos y comentados dentro de `Tests/editMode/CatalogosTests.cs`, y **son la plantilla del contenido de N0 y N1** que escribiremos en B1. Los tres se cargan, se validan y se ejecutan de verdad en los tests: el perfil de Scrum se trocea en dos sprints, resuelve cambios de alcance y aplica el `kappa 0.85` de la retro.

Si quieres verlos, están al principio de ese archivo, en `EventosJson`, `NivelJson` y `MetodologiaJson`.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `CatalogLoader.CargarEventos(json)` | texto | `List<EventDefinition>` validada; **lanza `SchemaException`** |
| `CatalogLoader.CargarPerfil(json)` | texto | `LevelProfile` validado |
| `CatalogLoader.CargarMetodologia(json)` | texto | `MethodologyProfile` validado |
| `CatalogLoader.CargarTodo(fuente)` | un `ICatalogSource` | `Catalogo` entero; **todos los errores en un solo mensaje** |
| `SchemaValidator.Validar*` | el objeto | `List<string>` de errores; vacía = válido |
| `CatalogoEnMemoria().Con(ruta, json)` | pares | fuente para tests |
| `new CatalogoDeArchivos(raíz)` | carpeta | fuente de disco; **no deja salirse de la raíz** |

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 251 en verde** (eran 205). `CatalogosTests` debe dar **46/46**.

### Prueba 2 — la manual: romper un JSON a propósito

Esta es la prueba que importa de este módulo. Crea `Assets/Scripts/Editor/PruebaM6.cs`:

```csharp
using System.IO;
using Nexus.Core.Datos;
using UnityEditor;
using UnityEngine;

public static class PruebaM6 {
    private const string Eventos = @"{ ""version"": 1, ""eventos"": [ {
        ""id"": ""EV-TEC-014"", ""nombre"": ""El servidor de integracion se cae"",
        ""tags"": [""tecnico""], ""fases"": [""desarrollo""], ""severidad"": 3,
        ""objetivoAprendizaje"": ""OA-DEVOPS-02"", ""pesoBase"": 10,
        ""precondiciones"": [""DeudaTecnica > 40""],
        ""telegrafiado"": { ""diasAntes"": 2, ""canal"": ""log"", ""texto"": ""El build tardo 14 min."" },
        ""opciones"": [
          { ""id"": ""A"", ""texto"": ""Parar la linea"",
            ""efectosInmediatos"": { ""Dias"": 2 },
            ""efectosDiferidos"": [ { ""enDias"": 5, ""efectos"": { ""VelocidadMod"": ""+10%"" } } ],
            ""rubrica"": { ""veredicto"": ""correcta"", ""razon"": ""Es la respuesta canonica."" } },
          { ""id"": ""B"", ""texto"": ""Seguir"",
            ""efectosInmediatos"": { ""DeudaTecnica"": 12 },
            ""efectosDiferidos"": [ { ""enDias"": 9, ""efectos"": { ""MoralEquipo"": -5 } } ],
            ""rubrica"": { ""veredicto"": ""incorrecta"", ""razon"": ""No lo retrasa, lo esconde."" } } ] } ] }";

    private const string Nivel = @"{
        ""id"": ""nivel-01"", ""nombre"": ""Cradle Lifts"",
        ""briefing"": [""Veinte dias. No es un proyecto importante.""],
        ""diasTotales"": 20, ""alcanceInicial"": 34, ""velocidadBase"": 3.0,
        ""volatilidadReal"": 25, ""nivelAndamiaje"": 3,
        ""metodologiasPermitidas"": [""kanban""],
        ""director"": { ""presupuestoDrama"": [1,3,2,1], ""pesosPorTag"": { ""tecnico"": 1.5 } },
        ""fase1"": {
          ""calidad"": { ""fichas"": 8, ""atributos"": [
            { ""id"": ""seguridad"", ""nombre"": ""Seguridad"", ""tagAfectado"": ""tecnico"" } ] },
          ""arquitecturas"": [
            { ""id"": ""monolito"", ""nombre"": ""Monolito"", ""esLaAdecuada"": true,
              ""veredicto"": ""correcta"", ""razon"": ""Tres patios no necesitan microservicios."" },
            { ""id"": ""micro"", ""nombre"": ""Microservicios"", ""esLaAdecuada"": false,
              ""veredicto"": ""incorrecta"", ""razon"": ""Confunde moda con criterio."" } ] } }";

    private const string Metodologia = @"{
        ""id"": ""kanban"", ""nombre"": ""Kanban"", ""familia"": ""agil"",
        ""calendario"": { ""tipo"": ""continuo"", ""etiquetaUnidad"": ""Flujo"", ""limiteWipInicial"": 4 },
        ""tableroPrincipal"": ""cfd"",
        ""rubricaCierre"": { ""practicas"": [ {
          ""id"": ""wip"", ""descripcion"": ""Respetar el limite de WIP"", ""metrica"": ""vecesExcedioWip"",
          ""comparador"": ""<="", ""objetivo"": 2,
          ""razonSiCumple"": ""Respetaste el tablero."", ""razonSiFalla"": ""Metiste mas de lo que cabia."" } ] } }";

    [MenuItem("Nexus/Pruebas/M6 · Cargar un catalogo y romperlo")]
    public static void Correr() {
        Debug.Log("--- 1. CATALOGO BUENO ---");
        Debug.Log(CatalogLoader.CargarTodo(Fuente(Eventos, Nivel, Metodologia)).ToString());

        Probar("2. UNA PRECONDICION CON UNA ERRATA",
               Eventos.Replace("DeudaTecnica > 40", "DeudaTecnia > 40"), Nivel, Metodologia);

        Probar("3. UN EFECTO QUE TOCA UN FLAG NARRATIVO (INV-1)",
               Eventos.Replace(@"""DeudaTecnica"": 12", @"""FLG_DEUDA_MORAL"": 3"), Nivel, Metodologia);

        Probar("4. UN EVENTO SIN AVISO PREVIO (INV-3)",
               Eventos.Replace(@"""telegrafiado"": { ""diasAntes"": 2, ""canal"": ""log"", ""texto"": ""El build tardo 14 min."" },", ""),
               Nivel, Metodologia);

        Probar("5. TRES ERRORES A LA VEZ",
               Eventos.Replace(@"""severidad"": 3", @"""severidad"": 9"),
               Nivel.Replace(@"""diasTotales"": 20", @"""diasTotales"": 1")
                     .Replace(@"[1,3,2,1]", @"[1,3]"),
               Metodologia);
    }

    private static CatalogoEnMemoria Fuente(string ev, string niv, string met) {
        return new CatalogoEnMemoria()
            .Con("eventos/eventos.json", ev)
            .Con("niveles/nivel-01.json", niv)
            .Con("metodologias/kanban.json", met);
    }

    private static void Probar(string titulo, string ev, string niv, string met) {
        try {
            CatalogLoader.CargarTodo(Fuente(ev, niv, met));
            Debug.LogError($"--- {titulo} ---\nDEBERIA HABER FALLADO Y NO FALLO");
        } catch (SchemaException ex) {
            Debug.Log($"--- {titulo} ---\n{ex.Message}");
        }
    }
}
```

Menú **Nexus > Pruebas > M6 · Cargar un catálogo y romperlo**. Salen **cinco bloques**.

**Qué debes ver, y qué significa:**

1. `1 eventos, 1 niveles, 1 metodologias` — el catálogo bueno entra sin ruido.
2. **La errata.** `DeudaTecnia` (falta la `c`) se caza **al cargar**, diciendo que no es una variable conocida. Esa errata, sin este módulo, habría hecho que el evento nunca saliera — y nadie se habría enterado nunca.
3. **INV-1.** El mensaje explica que ningún evento puede tocar un `FLG_*` y que los flags narrativos solo se escriben al cerrar el nivel.
4. **INV-3.** *«falta 'telegrafiado'. Ningún evento puede caer sin aviso previo.»*
5. **Los tres errores juntos**, en un solo mensaje, con viñetas. Ese es el bloque que de verdad importa: así es como se corrige contenido sin volverse loco.

Prueba a romper otras cosas: pon `"veredicto": "regular"`, quita una arquitectura, marca las dos como `esLaAdecuada`, pon `"metrica": "felicidad"`. Cada una tiene su mensaje.

Cuando termines, **borra `PruebaM6.cs`**.

---

## Lo que falta

- **`CargarMinijuegos` y `CargarNarrativa`** no existen todavía porque `MinigameDefinition` (M7) y `NarrativeBeat` (M8) no existen. Ya hay `CatalogoMinijuegos` para el verbo V1, escrito antes; M7 lo integrará.
- **El adaptador `CatalogoDeStreamingAssets`** es la capa Unity: una línea, en la Fase B.
- **Nadie llama a `CargarTodo` todavía.** Lo hará `AppRoot` al arrancar.
- **No hay contenido real en `StreamingAssets`.** Los JSON de N0 y N1 se escriben en B1, usando como plantilla los tres de `CatalogosTests`.
