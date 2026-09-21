# A′5 · Cadenas sin colisión y la alerta de las 08:00

**Rama:** `feature/cadenas-sin-colision`
**Código tocado:** `Core/Eventos/EventDirector.cs` (`ForzarEvento`) · `Core/Sesion/GameSession.cs` (`AvanzarReloj`)
**Tests nuevos:** 5 (2 en `DireccionDeEventosTests`, 3 en `GameSessionTests`)

> Es la única parada de esta fase que toca el motor, y solo porque era estrictamente necesario: con
> estos dos bugs, **el jugador podía esquivar consecuencias sin hacer nada**, que es justo lo que el
> diseño prohíbe.

---

## De dónde salen

Los encontró la parada anterior (`B1.3b`), al jugar N1 entero 180 veces con el contenido real: en 7 de
79 partidas en que el jugador parcheó `EV-TEC-02`, la deuda no volvió. Seguí las 7 una por una. Hay
dos causas y ninguna más:

| Bug                                           | Casos  | Reproducción        |
| --------------------------------------------- | ------ | ------------------- |
| **1 · Una cadena cae encima de otro evento**  | 4 de 7 | `scrum`, semilla 5  |
| **2 · Una alerta a las 08:00 no suena nunca** | 3 de 7 | `scrum`, semilla 16 |

Ninguno aparecía en los tests del motor porque usan catálogos de dos o tres eventos, donde casi nunca
coinciden dos en el mismo día ni el sorteo cae en el primer minuto. **Hizo falta contenido real,
jugado muchas veces, para que salieran.**

---

## Bug 1 · Las cadenas caían encima de otro evento

**Qué pasaba.** Solo se presenta un evento por día. Cuando el director elige uno al azar, comprueba
que el día esté libre. Pero `ForzarEvento`, que es lo que usan las cadenas, **no lo comprobaba**: si
el día ya estaba ocupado, agendaba el segundo igual, y ese día solo salía uno. El otro desaparecía en
silencio.

En la semilla 5: el día 10 el director agenda `EV-CAL-02` para el día 13. El día 11 la cadena fuerza
`EV-TEC-05`… también para el 13. El día 13 sale `EV-CAL-02`, y la deuda de haber parcheado no vuelve
nunca.

**El arreglo.** Si el día de la cadena está ocupado, se corre al primer día libre:

```csharp
var tope = Math.Max(1, _perfil.Director.MaxEventosPorDia);
while (_scheduler.EventosAgendadosEn(diaDelEvento) >= tope) {
    diaDelEvento++;
    diasAntes++;
}
```

Tres decisiones dentro de esas cuatro líneas:

- **Se mueve la cadena, no el otro evento.** El evento que ya estaba se había anunciado para ese día,
  y el jugador ya lo había visto. La cadena se está creando ahora: moverla no le miente a nadie.
- **El aviso sigue saliendo hoy.** Como `DiaDelAviso = díaDelEvento − díasAntes`, sumar uno a cada
  lado deja el aviso donde estaba. La consecuencia llega con **más** antelación, nunca con menos.
- **No gasta ninguna tirada del azar.** Correr el día es aritmética pura, así que INV-7 no cambia: la
  misma semilla sigue dando la misma partida, al mismo día y al mismo minuto.

## Bug 2 · La alerta de las 08:00 no sonaba (bug mío, de A′4)

**Qué pasaba.** `AvanzarReloj` avisa de las alertas que suenan en el intervalo _(antes, después]_. El
día empieza con el reloj en las 08:00, así que ningún intervalo contiene las 08:00: una alerta
sorteada justo en ese minuto **sonaba sin avisar a nadie** y expiraba como omitida. Y el sorteo sí
puede elegirlo, porque es el primer hueco válido.

**El arreglo.** El primer tramo del día que de verdad mueve el reloj incluye su propio arranque:

```csharp
var desdeExclusivo = desde == _reloj.MinutoDeInicio && avanzados > 0 ? desde - 1 : desde;
foreach (var alerta in _alertas.SuenanEntre(desdeExclusivo, _reloj.Minuto))
    if (alerta.EstaPendiente) resultado.AlertasQueSuenan.Add(alerta);
```

Dos detalles que no son obvios, y que tienen test propio:

- **`avanzados > 0`.** Si alguien llama a `AvanzarReloj(0)` a las 08:00, no se gasta el aviso: se
  guarda para el primer tramo que avance de verdad. Si no, sonaría en un tramo vacío y no volvería a
  sonar.
- **`EstaPendiente`.** Una interfaz puede enseñar la lista de alertas y dejar que el jugador atienda
  la de las 08:00 sin haber movido el reloj. `AtenderAlerta` cobra entonces su hora llamando a
  `AvanzarReloj` desde las 08:00, y sin el filtro **anunciaría como nueva una alerta ya atendida**.
  Este riesgo lo introducía mi propio arreglo, así que tiene su propio test.

---

## Entrada y salida

No cambia ningún contrato: `ForzarEvento` sigue recibiendo un id y agendando un evento telegrafiado, y
`AvanzarReloj` sigue devolviendo un `ResultadoDeAvance`. Lo único que cambia es que ahora hacen lo que
su documentación ya prometía.

---

## Cómo probarlo

**En Unity:** `Test Runner > EditMode > Run All`. Tiene que salir todo en verde, con **5 tests más**
que antes:

| Test                                                                       | Qué fija                                                                                      |
| -------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| `ForzarEvento_no_cae_encima_de_otro_evento_y_se_corre_al_primer_dia_libre` | día 11 ocupado → la cadena va al 12, con el aviso hoy (día 9), y el evento del 11 no se mueve |
| `ForzarEvento_salta_todos_los_dias_ocupados_seguidos`                      | días 11 y 12 ocupados → va al 13                                                              |
| `Una_alerta_a_la_hora_de_entrada_suena_en_el_primer_tramo_y_solo_una_vez`  | suena en el primer tramo y no en el segundo                                                   |
| `Avanzar_cero_minutos_a_las_ocho_no_gasta_el_aviso`                        | `AvanzarReloj(0)` no la gasta; el siguiente tramo sí la anuncia                               |
| `Una_alerta_de_las_ocho_atendida_sin_mover_el_reloj_no_vuelve_a_sonar`     | una ya atendida no se anuncia otra vez                                                        |

Para el bug 2 no dependo de que el sorteo caiga a las 08:00 por suerte: los tests usan una jornada de
08:00 a 11:00 con tres horas de ventana, y en ella **el único minuto donde cabe una alerta entera es
el de entrada**.

### Comprobé que los tests detectan de verdad el bug

Un test que pasa con y sin el arreglo no prueba nada. Por eso deshice los dos arreglos en una copia
y corrí los tests nuevos:

| Sin ningún arreglo                                                                                                             | Con el arreglo, pero sin el filtro `EstaPendiente`                                      |
| ------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------- |
| **4 de 5 fallan.** El quinto pasa, como era de esperar: sin el arreglo, a las 08:00 no sonaba nada, ni atendido ni sin atender | **El quinto falla.** Es el que protege contra el efecto secundario de mi propio arreglo |

### Resultado

|                                                                    | Antes             | Después                                                                                   |
| ------------------------------------------------------------------ | ----------------- | ----------------------------------------------------------------------------------------- |
| Suite del repositorio                                              | todo verde        | **440 / 440** (sin `MinijuegoDetectarTests`, que necesita la ruta de Unity, como siempre) |
| Contenido real de N1, 180 partidas: si parcheas, ¿vuelve la deuda? | **72 / 79**       | **86 / 86**                                                                               |
| Si dejas solo a Óscar, ¿se va?                                     | 33 / 33           | 35 / 35                                                                                   |
| Eventos que ve un jugador por partida                              | mín 1 · media 3.1 | **mín 2 · media 3.3**                                                                     |

La última fila también sube: hasta ahora, las alertas de las 08:00 se perdían aunque no fueran de
ninguna cadena.

---

## Lo que dejo anotado

**1 · `MaxEventosPorDia` mayor que 1 sigue sin funcionar del todo.** El director respeta el tope,
pero `GameSession` solo presenta **un** evento al día. Con un tope de 2, el segundo se perdería
exactamente igual que antes. Hoy no afecta a nada, porque N1 usa 1 y ningún nivel pide más. Si algún
nivel lo necesita, hay que arreglar `EventoDeHoy` para que devuelva la lista del día; es una parada
propia.

**2 · Un apunte de balance, no de motor: `EV-TEC-02` sale en las 180 partidas.** Es el único evento
elegible el día 3 (los demás piden día 4, 5 o 6), así que el director siempre lo elige primero. No
es un bug, pero hace que **el primer evento de N1 sea siempre el mismo**. Si prefieres variedad desde
el principio, basta con adelantar al día 3 la precondición de otro evento, por ejemplo `EV-INT-01`.
No lo cambio sin que lo decidas: es una decisión de diseño, no una corrección.

---

## Los comandos de git

Tres commits pequeños, uno por cosa:

```bash
git checkout -b feature/cadenas-sin-colision

git add "game/proyecto de grado/Assets/Scripts/Core/Eventos/EventDirector.cs" \
        "game/proyecto de grado/Assets/Scripts/Tests/editMode/DireccionDeEventosTests.cs"
git commit -m "fix(motor): una cadena ya no cae encima de otro evento y se pierde"

git add "game/proyecto de grado/Assets/Scripts/Core/Sesion/GameSession.cs" \
        "game/proyecto de grado/Assets/Scripts/Tests/editMode/GameSessionTests.cs"
git commit -m "fix(motor): la alerta sorteada a la hora de entrada ahora suena"

git add docs/03-calidad/motor/A5-cadenas-sin-colision.md
git commit -m "docs(motor): minidoc de la correccion de cadenas y alertas"
```
