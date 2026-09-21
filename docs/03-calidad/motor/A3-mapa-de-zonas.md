# A′3 · Mapa de zonas

> **Fase A′, parada 3 de 4.** Depende de A′1 y A′2.
> `NEXUS-DOCUMENTO-MAESTRO.md` §3.5 · §3.6 · M8.
> Estado: **implementado y probado**. 27 tests nuevos, todos en verde.

---

## Qué se hizo

| Archivo | Qué |
|---|---|
| `Core/Jornada/MapaDeZonas.cs` | **nuevo** · `MapaDeZonas`, `ZonaDeNivel`, `PuertaDeZona` |
| `Core/Modelo/LevelProfile.cs` | edición: el bloque `Mapa` |
| `Core/Datos/SchemaValidator.cs` | edición: `ValidarMapa` |
| `Tests/editMode/MapaDeZonasTests.cs` | 27 tests |

`RuntimeState.ZonasVisitadas` y `ZonaActual` **ya existían** (de M1 y A′1), así que no hizo falta tocar el estado.

---

## La frase que ordena el módulo

> ### *«Javier ya no es un turno, es un sitio al que ir.»*

La regla se conserva intacta —*siempre dice una verdad técnica y una social*— pero deja de ser una parada obligatoria de las 09:00. Ahora hay que **bajar al Data Hub a buscarlo**, y la conversación entera se puede perder. **El jugador que no va, no se entera.**

Es el mismo argumento pedagógico de la decisión de las 18:00, aplicado al día entero.

## ⚠ No confundir con las zonas A–F

El Documento Maestro marca esta distinción con énfasis, y confundirlas rompe las dos mecánicas:

| | **Zonas A–F** (Fase 1) | **Mapa del nivel** (Fase 2) |
|---|---|---|
| Qué es | la recolección con reloj de 2 h | los sitios a los que ir cada día |
| Cambia | **nunca**, son siempre las mismas seis | **cada nivel** tiene el suyo, de 3 a 6 |
| Crece | no | **con la casta y con la historia** |

Esta parada implementa **el segundo**.

---

## Qué hace

### El ancla

> **Tu escritorio es siempre el ancla:** es donde llegan las alertas y donde hay que volver a atenderlas.

El validador exige **exactamente una**. Sin ancla, una alerta no se podría atender en ninguna parte.

### Los costes, en datos

```json
"mapa": {
  "costeBaseDeViaje": 20,
  "costes": { "escritorio>bullpen": 10, "escritorio>data-hub": 90 },
  "zonas": [ … ]
}
```

Solo se declaran los pares que **no** cuestan lo de siempre, y valen **en las dos direcciones**. `CosteDeVisitar` suma el viaje más la micro-escena de estar ahí.

Eso es lo que convierte *«estar lejos»* en un riesgo con número: si una alerta suena a las 11:00 con tres horas de ventana, volver desde el bullpen cuesta 10 minutos y desde el Data Hub cuesta 90.

### Las puertas

El mapa **declara** las condiciones pero **no las evalúa**:

```csharp
Puerta = { Precondiciones: ["diaActual >= 4"], TrasBeat: "CIN-2.3" }
```

Quien las evalúa es `GameSession`, que tiene el estado y los beats. Es deliberado: `ConditionEvaluator` vive en `Servicios`, y `Modelo` ya depende de `Jornada` — evaluarlas aquí crearía un ciclo entre paquetes. Así `Core.Jornada` sigue siendo una hoja.

Con eso, *«el mapa crece a medida que el jugador asciende, y eso hace visible la progresión sin una sola barra»*: la cocina se abre **tras `CIN-2.3`**, la sala de arquitectura **con casta 3**.

---

## Lo que aprendí escribiendo los tests, y que cambia cómo hay que balancear

Escribí un test llamado `Recorrer_todas_las_zonas_en_un_dia_es_imposible`, porque el §3.6 lo dice así de tajante. **Falló.** Con cinco zonas, tiempos de visita de 25–40 min y viajes de 20 min, el recorrido entero cuesta **210 minutos de una jornada de 600**. Caben todas, y sobran seis horas.

El test tenía razón en fallar, y el hallazgo es importante:

> **«Recorrerlas todas en un día es imposible» no es una propiedad del motor. Es una propiedad de los números que escriba el contenido.**

Así que en vez de trucar el fixture para que pasara, le di al motor la herramienta para hacerlo visible: **`MinutosParaRecorrerloTodo()`**, que calcula por fuerza bruta el recorrido más barato que sale del escritorio, visita todas las zonas y vuelve. Con 3–6 zonas son como mucho 120 permutaciones.

| Coste base de viaje | Recorrido entero | Del día |
|---|---|---|
| 20 min | 210 min | 35 % |
| 60 min | 330 min | 55 % |
| 90 min | 420 min | **70 %** |

**Esa tabla es la que hay que mirar al escribir `nivel-01.json` en B1.** Si el recorrido entero se lleva el 35 % del día, no hay nada que priorizar.

Y un segundo hallazgo, de los que ahorran tardes: **encarecer un tramo solo importa si la ruta óptima pasaba por él.** Subir `escritorio>data-hub` de 45 a 300 **no cambia nada**, porque el recorrido barato llega al Data Hub desde la cafetería. Hay un test que lo fija, para que nadie pierda una tarde subiendo un número que no hace nada.

El test que quedó afirma la propiedad real y no ajustable: **encarecer los viajes reduce cuántas zonas caben, y nunca al revés.** Dónde cae el corte es balanceo, y es cosa tuya.

---

## Entrada y salida

| | Entrada | Salida |
|---|---|---|
| `Ancla` | — | el escritorio, o `null` si el nivel no tiene mapa |
| `PorId(id)` | id de zona | la zona; no distingue mayúsculas |
| `CosteEntre(a, b)` | dos zonas | minutos. Quedarse donde estás es **gratis** |
| `CosteDeVisitar(a, b)` | ídem | viaje **+** micro-escena |
| `MinutosParaRecorrerloTodo()` | — | el recorrido más barato, ida y vuelta al ancla |
| `Vacio` | — | `true` si el nivel no tiene exploración |

**Un nivel puede no tener mapa.** Entonces no hay exploración y todo ocurre en el escritorio — es lo que necesita un prólogo, y lo que permite que todos los tests anteriores sigan funcionando sin declarar zonas.

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 421 en verde** (eran 394). `MapaDeZonasTests` debe dar **27/27**.

### Prueba 2 — la manual: el día que bajaste a ver a Javier

Crea `Assets/Scripts/Editor/PruebaA3.cs`:

```csharp
using Nexus.Core.Jornada;
using UnityEditor;
using UnityEngine;

public static class PruebaA3 {
    [MenuItem("Nexus/Pruebas/A3 · El mapa y lo que cuesta recorrerlo")]
    public static void Correr() {
        foreach (var costeBase in new[] { 20, 60, 90 }) {
            var m = Mapa(costeBase);
            var jornada = new JornadaConfig();
            var dia = (jornada.HoraCierre - jornada.HoraInicio) * 60;
            var tour = m.MinutosParaRecorrerloTodo();
            Debug.Log($"COSTE BASE {costeBase,3} min -> recorrerlo entero cuesta {tour} min " +
                      $"({100 * tour / dia} % de la jornada)");
        }

        // --- el dia concreto: suena una alerta y hay que volver ---
        var mapa = Mapa(60);
        var reloj = new RelojDeJornada(new JornadaConfig());
        var cola = new ColaDeAlertas();
        cola.Encolar(new Alerta {
            Id = "EV-TEC-014", MinutoDeLaAlerta = 11 * 60, MinutoDeExpiracion = 14 * 60,
            Canal = "chat", Texto = "Javier: 'el build lleva 14 min, ven a mirarlo'"
        });

        var zona = "escritorio";
        var texto = "\n=== EL DIA QUE BAJASTE A VER A JAVIER ===\n";

        foreach (var destino in new[] { "bullpen", "data-hub", "escritorio" }) {
            var coste = mapa.CosteDeVisitar(zona, destino);
            var z = mapa.PorId(destino);
            texto += $"  {reloj} -> vas a {z.Nombre,-22} ({mapa.CosteEntre(zona, destino),3} viaje + " +
                     $"{z.MinutosDeVisita,2} estancia = {coste,3} min)\n";
            if (!string.IsNullOrEmpty(z.QueDa)) texto += $"            \"{z.QueDa}\"\n";
            if (z.QuienEsta.Count > 0) texto += $"            esta: {string.Join(", ", z.QuienEsta)}\n";

            var desde = reloj.Minuto;
            reloj.Avanzar(coste);
            zona = destino;

            foreach (var a in cola.SuenanEntre(desde, reloj.Minuto))
                texto += $"  {RelojDeJornada.Formatear(a.MinutoDeLaAlerta)}    ALERTA \"{a.Texto}\" " +
                         $"-> hasta las {RelojDeJornada.Formatear(a.MinutoDeExpiracion)}\n";
            foreach (var a in cola.Expirar(reloj.Minuto, zona))
                texto += $"  {reloj}    *** SE PERDIO {a.Id} *** estabas en {a.ZonaDelJugador}\n";
        }

        foreach (var a in cola.Pendientes(reloj.Minuto))
            texto += $"  {reloj}    llegas a tiempo y atiendes {a.Id}\n";

        Debug.Log(texto + $"\n  Son las {reloj}. Quedan {reloj.MinutosRestantes} min de jornada.");
    }

    private static MapaDeZonas Mapa(int costeBase) {
        return new MapaDeZonas {
            CosteBaseDeViaje = costeBase,
            Zonas = {
                new ZonaDeNivel { Id = "escritorio", Nombre = "Tu escritorio", EsAncla = true,
                    MinutosDeVisita = 15, QueDa = "Aqui llegan y se atienden las alertas." },
                new ZonaDeNivel { Id = "bullpen", Nombre = "Bullpen", MinutosDeVisita = 30,
                    QueDa = "El equipo, la moral real, rumores de asignacion.",
                    QuienEsta = { "Oscar Rendon" }, Coleccionables = { "COL-DEV-02" } },
                new ZonaDeNivel { Id = "cafeteria", Nombre = "Cafeteria", MinutosDeVisita = 25,
                    QueDa = "Lo que nadie dice en el standup.", Coleccionables = { "COL-POST-01" } },
                new ZonaDeNivel { Id = "data-hub", Nombre = "Data Hub -2", MinutosDeVisita = 40,
                    QueDa = "Javier: una verdad tecnica y una social.", QuienEsta = { "Javier" } }
            },
            Costes = { { "escritorio>bullpen", 10 } }
        };
    }
}
```

Menú **Nexus > Pruebas > A3 · El mapa y lo que cuesta recorrerlo**.

**Qué debes ver, y qué significa:**

- **Tres líneas de coste**, con el porcentaje de la jornada que se lleva recorrerlo todo según lo lejos que estén las zonas. **Ésa es la tabla que hay que mirar al balancear N1.**
- **El día concreto:** vas al bullpen (barato, 10 min de viaje), bajas al Data Hub (caro, 60), y a las 11:00 suena la alerta mientras estás abajo. Vuelves, y el registro dice si llegaste o no.
- Cada zona imprime **qué da** y **quién está**. Compara: el bullpen da la moral real, el Data Hub da a Javier. **Ninguna zona repite lo de otra** — si repitiera, sobraría.

Prueba a cambiar el coste base a 90 y volver a lanzarlo: bajar a ver a Javier deja de compensar, y esa es exactamente la decisión que el mapa tiene que producir.

Cuando termines, **borra `PruebaA3.cs`**.

---

## Lo que falta

- **Nadie se mueve todavía.** `GameSession.IrAZona()` y el cobro del desplazamiento son **A′4**.
- **Las puertas no se evalúan.** El mapa las declara; `GameSession` las resolverá contra el estado y los beats en A′4.
- **Ningún nivel tiene mapa.** Los de N0 y N1 se escriben en **B1**, y ahí es donde la tabla de costes de arriba deja de ser teórica.
- **Los coleccionables de cada zona son ids sueltos.** El catálogo que los define llega en B1.6.
