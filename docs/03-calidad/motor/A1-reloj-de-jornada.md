# A′1 · El reloj de la jornada

> **Fase A′, parada 1 de 4.** Va después de M9 y empieza el retrofit al día continuo.
> `NEXUS-DOCUMENTO-MAESTRO.md` §3.3 · §13.2 · M8.
> Estado: **implementado y probado**. 22 tests nuevos, todos en verde.

---

## Qué se hizo

| Archivo | Qué |
|---|---|
| `Core/Jornada/RelojDeJornada.cs` | **nuevo** · `RelojDeJornada` + `JornadaConfig` |
| `Core/Modelo/RuntimeState.cs` | edición: `MinutoDelDia`, `ZonaActual`, `JornadaProrrogada` + `minutoDelDia` como consultable |
| `Core/Modelo/LevelProfile.cs` | edición: el bloque `Jornada` |
| `Core/Datos/SchemaValidator.cs` | edición: `ValidarJornada` |
| `Tests/editMode/RelojDeJornadaTests.cs` | 22 tests |
| `Tests/editMode/NucleoDeEstadoTests.cs` | edición: un test que contaba consultables |

**La parada es aditiva a propósito.** `RuntimeState.Ventana` sigue ahí, marcada como en retirada: `GameSession` todavía la usa y migrarla es A′4. Así ninguno de los 348 tests anteriores corre riesgo, y si alguno se pusiera rojo sería culpa de esta parada y de nada más.

---

## Por qué existe esto

El motor se construyó sobre **cuatro ventanas fijas** (09:00 / 12:00 / 15:00 / 18:00). Ese modelo ya no es el del juego.

Y no es un capricho: **el canon ya lo había cambiado**. El §3.3 «El día continuo» dice, literalmente:

> *«El día de la Fase 2 **no tiene turnos**. El reloj corre solo. En cualquier instante el jugador está haciendo algo por elección propia — y en cualquier instante el trabajo puede reclamarlo.»*

Y el §13.2 marca las cuatro ventanas como **documentación obsoleta**. Lo que hace esta parada es poner el reloj que el canon ya pedía.

---

## Qué hace

### El reloj no sabe qué es el tiempo real

Esta es la decisión de diseño que sostiene todo lo demás. `RelojDeJornada` **solo cuenta minutos del juego** y los hace avanzar cuando alguien se lo pide. Quién se lo pide, y cada cuánto, es cosa de la capa de presentación, que leerá `SegundosRealesPorHora`.

Así el motor sigue siendo determinista y comprobable **sin abrir Unity** — que es exactamente lo que sostiene INV-7. Si el reloj tuviera corrutinas dentro, la mitad de los tests del proyecto dejarían de poder escribirse.

### El día

```
08:00 ─────────────────────────────► 18:00 ──► 20:00
      el jugador hace lo que quiere    │        (solo si se queda)
      y cada cosa cuesta minutos       │
                                       └─ ¿quedarse o irse?
                                          el unico momento fijo que sobrevivio
```

| Llamada | Qué hace |
|---|---|
| `Avanzar(minutos)` | hace correr el reloj y **devuelve los minutos que avanzó de verdad** |
| `SaltarHasta(minuto)` | salta sin pasar por el medio; es el botón «cerrar jornada» |
| `Prorrogar()` | quedarse a trabajar. Solo funciona **en** el cierre, y solo una vez |
| `Reiniciar()` / `Restaurar(minuto, prorrogada)` | el día siguiente · el guardado |

### El detalle que parece menor y no lo es

`Avanzar` devuelve **lo que avanzó de verdad**, no lo que se le pidió:

> Un desplazamiento de 30 minutos empezado a las 17:50 **no cuesta 30 minutos: cuesta 10.**

Quien llama necesita saberlo para cobrar solo el tiempo que existió. Sin esto, cruzar el campus al final del día saldría gratis o descuadraría el reloj.

### La jornada se configura por nivel

Vive en el `LevelProfile`, así que el prólogo puede tener días más cortos sin tocar código:

| Campo | Por defecto | Para qué |
|---|---|---|
| `horaInicio` | 8 | |
| `horaCierre` | 18 | la decisión más importante del juego |
| `horaLimite` | 20 | hasta dónde llega si se queda. Si `horaLimite == horaCierre`, **el nivel no ofrece horas extra** |
| `segundosRealesPorHora` | 20 | la escala. **El motor no la usa nunca**: la lee la UI |
| `ventanaDeAtencionMinutos` | 180 | tres horas para atender una alerta. Se usa en A′2 |

### Y el contenido ya puede preguntar la hora

`minutoDelDia` entra en los consultables de `RuntimeState`, así que una precondición puede decir:

```json
"precondiciones": ["minutoDelDia / 60 >= 15"]
```

*«este evento solo sale por la tarde».* Eran 10 consultables; ahora son 11.

---

## El problema aritmético que hay detrás, y cómo queda resuelto

Con el reloj en tiempo real: **12 h × 20 s = 4 min por día. × 20 días de N1 = 80 minutos solo de reloj**, y la sesión de laboratorio son 90.

Esta parada deja puestas las tres palancas para resolverlo, y ninguna necesita código nuevo:

1. **`segundosRealesPorHora` es un dato del nivel.** Bajarlo a 10 parte el día por la mitad. Y el `velocidadSimulacion` (0,5–2,0) de la configuración A1 ya existía en tu inventario de variables para que el docente ajuste en clase.
2. **`horaInicio` / `horaCierre` por nivel.** El prólogo puede ser de 09:00 a 13:00.
3. **`SaltarHasta`**, que es el botón «cerrar jornada». En A′2, cuando existan las alertas, **solo se habilitará si no queda ninguna pendiente ni ningún evento agendado para hoy** — así saltar nunca se come una consecuencia, y lo único que cuesta es perderte exploración y conversaciones.

El M8 del Documento Maestro avisa de que la escala del reloj y la ventana de atención *«son los dos números que definen si el día se siente tenso o agobiante, y **hay que jugarlos, no razonarlos**»*. Por eso están en datos y no en constantes.

---

## Cómo probarlo

### Prueba 1 — la automática

`Window > General > Test Runner` → **EditMode** → **Run All**.
**Debes ver 370 en verde** (eran 348). `RelojDeJornadaTests` debe dar **22/22**.

Y fíjate en que **`NucleoDeEstadoTests` sigue en verde**: cambié uno de sus tests, el que contaba los consultables. Ahora comprueba la lista en vez de un número, así que si alguien añade un consultable el test le obliga a declararlo — que es lo que convierte esa lista en un contrato con el contenido.

### Prueba 2 — la manual: ver pasar un día

Crea `Assets/Scripts/Editor/PruebaA1.cs`:

```csharp
using Nexus.Core.Jornada;
using UnityEditor;
using UnityEngine;

public static class PruebaA1 {
    [MenuItem("Nexus/Pruebas/A1 · Un dia con el reloj continuo")]
    public static void Correr() {
        var reloj = new RelojDeJornada(new JornadaConfig());
        var texto = "=== UN DIA NORMAL ===\n";

        // El jugador hace cosas, y cada cosa cuesta tiempo
        foreach (var (que, minutos) in new[] {
            ("mira el dashboard",          30),
            ("va al bullpen",              45),
            ("habla con Oscar",            60),
            ("baja al Data Hub",           50),
            ("habla con Javier",           60),
            ("sube a la cafeteria",        40),
            ("vuelve a su escritorio",     45),
            ("revisa el correo",           30),
            ("atiende una alerta",         90),
            ("se cruza el campus entero", 120),
            ("intenta una cosa mas",       60)
        }) {
            var antes = reloj.ToString();
            var reales = reloj.Avanzar(minutos);
            texto += $"  {antes} -> {reloj}  {que,-26} (pedidos {minutos,3} min, costaron {reales,3})";
            if (reales < minutos) texto += "   <- el dia se acabo a medias";
            texto += "\n";
            if (reloj.LlegoElCierre) break;
        }

        texto += $"\n  Son las {reloj}. ?Quedarse o irse?\n";
        Debug.Log(texto);

        // --- se queda ---
        var seQueda = new RelojDeJornada(new JornadaConfig());
        seQueda.SaltarHasta(seQueda.MinutoDeCierre);
        seQueda.Prorrogar();
        seQueda.Avanzar(75);
        Debug.Log($"SE QUEDA:  prorroga concedida -> ahora son las {seQueda}, " +
                  $"quedan {seQueda.MinutosRestantes} min");

        // --- se va ---
        var seVa = new RelojDeJornada(new JornadaConfig());
        seVa.SaltarHasta(seVa.MinutoDeCierre);
        Debug.Log($"SE VA:     son las {seVa}, terminada = {seVa.Terminada}");

        // --- el prologo, con jornada corta y sin horas extra ---
        var prologo = new RelojDeJornada(new JornadaConfig {
            HoraInicio = 9, HoraCierre = 13, HoraLimite = 13, VentanaDeAtencionMinutos = 90
        });
        prologo.Avanzar(4 * 60);
        Debug.Log($"PROLOGO:   de 09:00 a 13:00. Ahora {prologo}. " +
                  $"?Puede quedarse? {prologo.Prorrogar()}  <- este nivel no ofrece horas extra");
    }
}
```

Menú **Nexus > Pruebas > A1 · Un día con el reloj continuo**.

**Qué debes ver, y qué significa:**

- **Once líneas de un día**, con la hora subiendo de 08:00 a 18:00. Cada actividad mueve el reloj: atender una alerta cuesta hora y media, cruzarse el campus entero cuesta dos horas. Ese es el presupuesto de atención del §3.5, ahora literal.
- La **última línea** dice `17:30 -> 18:00  intenta una cosa mas (pedidos 60 min, costaron 30)` y marca **`<- el dia se acabo a medias`**. Ese descuadre entre lo pedido y lo cobrado es lo que `Avanzar` devuelve, y es lo que impide que cruzar el campus al final del día salga gratis.
- **SE QUEDA** → 19:15, con 45 min restantes.
- **SE VA** → 18:00, `terminada = True`.
- **PROLOGO** → `¿Puede quedarse? False`. Un nivel con `horaLimite == horaCierre` **no ofrece horas extra**, y eso se configura en el JSON del nivel, no en el código.

Cuando termines, **borra `PruebaA1.cs`**.

---

## Lo que falta

- **Nadie hace correr el reloj todavía.** `GameSession` sigue con las cuatro ventanas; migra en **A′4**.
- **No hay alertas.** `ventanaDeAtencionMinutos` está configurado y nadie lo usa: es **A′2**.
- **No hay zonas.** Los desplazamientos de la prueba manual son números inventados; el mapa real es **A′3**.
- **`RuntimeState.Ventana` sigue viva**, marcada como en retirada. Se va en A′4.
