# Propuesta de mecánica: «El Tablero Vivo»

> **Estado (2026-10-05):** implementada, con tres cambios respecto a lo que se propone abajo (ver «Cómo quedó», al final). El texto original se conserva como registro del diseño.
> **Estado original:** propuesta de diseño, sin implementar. Responde a la observación de la beta 1.0 de que la Fase 2 puede volverse aburrida cuando no pasa nada: el jugador se queda quieto viendo correr el reloj hasta el siguiente aviso.
> **Relacionado:** relaciones con personajes (`narrativa/conversaciones.json`, ya implementado), reloj de jornada (`RelojDeJornada`, `ColaDeAlertas`), modelo continuo (`ForresterModel`).

---

## 1. El problema

En la Fase 2 el reloj corre solo. Entre aviso y aviso el jugador puede:

- moverse por el mapa;
- hablar con alguien;
- hacer una tarea de oficina (una vez al día cada una).

Cuando ya hizo todo eso, solo le queda **esperar**. Esperar no enseña nada y rompe el ritmo. El botón «Esperar al siguiente aviso» lo admite: es saltarse el tiempo porque el tiempo no tiene contenido.

Pero ese tiempo muerto es justo lo que en un proyecto real **no** está muerto: el equipo trabaja, las tareas avanzan o se atascan, aparecen errores y alguien decide qué se hace después. Hoy todo eso lo simula `ForresterModel` en una sola tirada al final del día (`AvanzarUnDia`), invisible para el jugador.

**La idea:** sacar ese trabajo a la superficie y dárselo al jugador para que lo gestione en tiempo real.

## 2. La mecánica en una frase

Un tablero Kanban del equipo, siempre visible durante la jornada. Las tarjetas de trabajo y los errores van llegando solos. El jugador asigna cada tarjeta a un compañero según su habilidad y su cansancio, respeta un límite de trabajo en curso (WIP), y ve fluir el trabajo… o atascarse.

```
 POR HACER          HACIENDO (WIP 3)            REVISIÓN          HECHO
┌────────────┐     ┌────────────────────┐     ┌────────────┐     ┌──────┐
│ Formulario │     │ Guardar visitante  │     │ PR #12  ⚠  │     │ ✓ ✓  │
│ 3 pts  ★★  │     │ 🧑 Óscar  ▓▓▓░░  2h │     │ 3 líneas   │     │ ✓    │
├────────────┤     ├────────────────────┤     │ [Aprobar]  │     └──────┘
│ 🐞 Bug     │ ──► │ Aviso por correo   │     │ [Cambios]  │
│ barrera    │     │ 🧑 Sarah  ▓░░░░  4h │     └────────────┘
└────────────┘     ├────────────────────┤
                   │  (hueco libre)     │        Cansancio del equipo
                   └────────────────────┘        Óscar ▓▓▓▓░  Sarah ▓▓░░░
```

## 3. Objetivo pedagógico por tema

| Tema de la asignatura | Qué se aprende jugando |
|---|---|
| **Kanban** | El límite WIP de verdad: con más tarjetas en «haciendo», todas avanzan más lento (cambio de contexto) y el flujo cae. El atasco se ve, no se explica. |
| **Scrum** | Con Scrum, el tablero es el *sprint backlog*: solo entra lo comprometido en la planificación. Meter algo a mitad de sprint tiene un coste visible. |
| **Ciclo de vida (Cascada)** | Con Cascada las columnas se desbloquean por fases: no hay «Revisión» hasta la etapa de pruebas. El jugador siente por qué los errores tardíos cuestan más. |
| **git / revisión de código** | La columna «Revisión» trae micro-PR de 3 a 5 líneas: aprobar o pedir cambios en unos 30 segundos. Aprobar sin mirar mete deuda; pedir cambios a todo bloquea el flujo. |
| **Calidad / Testing** | Algunas tarjetas terminadas generan 🐞 errores al rato, más probables si se hicieron con el equipo cansado o se aprobaron sin revisar. |
| **Estimación** | Cada tarjeta muestra su estimación y su tiempo real al terminar. Al final del día, la diferencia alimenta `HistorialEstimaciones` (ya existe en `RuntimeState`). |
| **DevOps** | Un hito opcional: automatizar la columna «Despliegue». Cuesta tarjetas al principio y luego hace que «Hecho» sea inmediato. |
| **Gestión del equipo** | Asignar siempre a la misma persona la quema (sube su cansancio). Repartir y emparejar sube la competencia de todos. |

## 4. El bucle, minuto a minuto

1. **Llegan tarjetas** a «Por hacer», desde el backlog del día (salen del alcance pendiente) y desde los errores. El ritmo depende del nivel: unas 4 a 6 por jornada en N0 y 8 a 12 en N1.
2. **El jugador arrastra** una tarjeta a «Haciendo» y la suelta sobre un compañero. Cada compañero tiene:
   - una habilidad por tipo (frontend, datos, pruebas…);
   - un nivel de cansancio.
3. **La tarjeta avanza sola** con el reloj, a una velocidad que depende de:
   - la habilidad del compañero con ese tipo de tarjeta;
   - su cansancio;
   - el WIP: más tarjetas en curso, menos velocidad para todas.
4. **Al terminar, pasa a «Revisión».** Ahí puede pedir una micro-decisión de PR:
   - Aprobar rápido → la tarjeta avanza, con riesgo de error.
   - Pedir cambios → la tarjeta vuelve a «Haciendo» un rato y sale con menos riesgo.
5. **«Hecho»** suma avance al proyecto. Hoy ese avance lo pone `AvanzarUnDia` de golpe; con el tablero, sale de aquí.
6. **Los avisos siguen llegando** como ahora. Mientras se atiende uno, el tablero sigue corriendo: elegir entre apagar un fuego o vigilar el flujo es la tensión del día.

Durante el tiempo «vacío» siempre hay algo que mirar:
- una tarjeta atascada;
- un compañero que se está quemando;
- un PR esperando revisión.

Ninguna de esas acciones es obligatoria. Ignorarlas tiene consecuencias suaves y visibles: el flujo baja, sube el cansancio o se acumulan errores.

## 5. Integración con lo que ya existe

| Pieza actual | Cómo se integra |
|---|---|
| `RelojDeJornada` / `LevelRunner` | El tablero se actualiza en cada tramo `AlAvanzar` del runner. No necesita su propio reloj. |
| `ColaDeAlertas` | Los avisos no cambian. Un 🐞 grave puede convertirse en aviso con cuenta atrás. |
| `ForresterModel.AvanzarUnDia` | El avance diario pasa a ser la suma de lo que llegó a «Hecho». La deuda sube con los PR aprobados sin mirar; el cansancio, con el reparto desigual. `Velocidad` deja de ser un número y pasa a ser lo que se ve fluir. |
| `MethodologyRules` / `DayPlan` | La metodología configura el tablero: columnas, límite WIP, si se admiten tarjetas a mitad de sprint, qué columnas se desbloquean por etapa en Cascada. |
| Relaciones (Bloque F) | Un compañero con confianza alta trabaja más rápido contigo o acepta tarjetas de otro tipo. Una ayuda nueva: «Óscar revisa un PR por ti». |
| Evaluación (`DecisionTrace`) | Solo las decisiones con criterio entran en la traza: aprobar o pedir cambios, saltarse el WIP, sobrecargar a alguien. Con rúbrica `OA-KANBAN-01`, `OA-GIT-01` u `OA-CAL-01`. |
| Lanzamiento (`CalculoDeLanzamiento`) | No cambia: los errores escapados ya cuentan. El tablero solo hace visible de dónde salen. |

**Nuevo en Core:**
- `TableroDelEquipo`: estado puro y serializable dentro de `RuntimeState`, para que viaje en el guardado (INV-7).
- `MotorDelTablero`: avanza tarjetas por minutos, con reglas deterministas y el RNG de la sesión, de modo que la partida sea reproducible en modo aula.

**Nuevo en Unity:**
- `TableroView`, en la columna central del día, plegable cuando hay un aviso.

## 6. Variantes por metodología

- **Scrum.** Columnas: Por hacer → Haciendo → Revisión → Hecho.
  - Solo entran las tarjetas comprometidas en la planificación.
  - Meter una nueva a mitad de sprint pide confirmación y sube `SobreCompromiso`.
- **Kanban.** Mismas columnas, con límite WIP visible por columna.
  - Las tarjetas entran cuando hay hueco.
  - Superar el WIP se puede, pero la columna se pone en rojo y todo va más lento.
- **Cascada.** Columnas por etapa: Requisitos → Diseño → Construcción → Pruebas.
  - Solo la columna de la etapa actual está activa.
  - Los errores de requisitos aparecen como 🐞 en la etapa de pruebas y cuestan el triple.

## 7. Balance y riesgos

- **Carga cognitiva.** Es el riesgo principal: un tablero en tiempo real más avisos más mapa puede abrumar a quien nunca ha visto IS.
  - N0 empieza con 2 columnas y 2 compañeros, y la guía de Marisol lo introduce el día 2.
  - El tablero se pausa con el resto del juego.
  - Las tarjetas avanzan solas: no hace falta tocarlas para que el día funcione.
- **Ruido.** Si llegan demasiadas tarjetas, el tablero se convierte en un juego de clics. El ritmo de llegada se fija por nivel en el JSON y nunca supera una tarjeta cada 45 minutos de juego.
- **Competir con los minijuegos.** El tablero es gestión continua y ligera; los minijuegos son retos concentrados. Mientras hay un minijuego abierto, el tablero se congela.
- **Coste de implementación.** Es la pieza más grande propuesta hasta ahora. Las fases de abajo permiten parar en cualquiera con algo jugable.

## 8. Cómo se mediría si funciona

- Tiempo medio por jornada con el botón «Esperar al siguiente aviso»: debería bajar mucho.
- Diferencia de flujo (tarjetas hechas por día) entre quien respeta el WIP y quien no.
- Respuestas a las preguntas de Kanban y git del post-test, comparadas con la versión sin tablero.
- Encuesta corta tras la sesión: «¿Te aburriste esperando?» (Likert 1–5).

## 9. Plan por fases

| Fase | Qué se entrega | Se puede parar aquí |
|---|---|---|
| 1 | `TableroDelEquipo` + `MotorDelTablero` en Core, con tests EditMode (flujo, WIP, determinismo, guardado). | No, sin UI no es visible. |
| 2 | `TableroView` de solo lectura: el jugador ve fluir las tarjetas. | Sí: ya rellena el tiempo muerto con información. |
| 3 | Arrastrar tarjetas y asignar compañeros; WIP con efecto. | Sí: mecánica completa de Kanban. |
| 4 | Columna «Revisión» con micro-PR (aprobar o pedir cambios) y errores derivados. | Sí: añade git y calidad. |
| 5 | Variantes por metodología y ayudas de relaciones. | Versión completa. |

## 10. Alternativas descartadas

- **Un minijuego de relleno al azar** (reflejos, puzles) para el tiempo muerto. Rellena el tiempo pero no enseña nada de la asignatura y rompe la coherencia del mundo. Descartada.
- **Comprimir el día** (que el reloj vaya más rápido cuando no hay nada). Ya existe (×2, ×4 y «Esperar al siguiente aviso») y es justo el síntoma: el tiempo no tiene contenido. Quita el aburrimiento quitando el juego.
- **Más eventos por día.** Ya hay presupuesto de drama, y subirlo convierte cada día en una crisis. Los eventos deben seguir siendo raros para que pesen.

---

## Cómo quedó (implementación, 2026-10-05)

Código: `Core/Tablero/` (`TableroDelEquipo`, `GeneradorDeTarjetas`, `MotorDelTablero`, `GraficasDelProyecto`, `MicroPr`), `Core/Sesion/GameSession.Tablero.cs`, `Core/Metodologia/PoliticaDeTablero.cs` y, en Unity, `Pantallas/Dia/PantallaDelDia.Tablero.cs`. Tests: `TableroVivoTests`, `MetodologiasEnElTableroTests`.

Tres cosas se hicieron distinto de lo propuesto, y por qué:

1. **El tablero modula al motor, no lo sustituye (§4.5 y §5).** `W.Avance` sigue avanzando solo en `TerminarDia`, con `ForresterModel`. El tablero es una proyección: reparte ese avance en tarjetas y durante el día lo anticipa (`ProvisionalHoy`). Lo que el jugador hace con él entra como un `factorDeFlujo` acotado entre 0,80 y 1,15. Con el equipo en piloto automático el factor vale exactamente 1, así que las partidas ya calibradas no cambian (lo comprueba `En_piloto_automatico_el_factor_es_exactamente_uno…`).
2. **No usa el azar de la sesión (§5).** Generar tarjetas o elegir el cambio a revisar sale de un hash de `(semilla, id)`. Una sola tirada del `DeterministicRng` habría desplazado todos los eventos de todas las partidas.
3. **El ritmo de tarjetas es menor (§4.1 y §7).** El día dura entre 96 y 200 segundos reales y N0 tiene 12 puntos: salen 1–2 tarjetas terminadas por día, no 4–6. Por eso en el día el tablero solo se mira; asignar, priorizar y revisar se hacen con el panel abierto, que para el reloj, y cada acción cobra minutos.

Lo que cada metodología hace con el tablero es dato (`"tablero"` en `metodologias/*.json`):

| | Scrum | Kanban | Cascada |
|---|---|---|---|
| Qué se trabaja | Lo comprometido en la planificación (las primeras tarjetas del backlog) | Lo que cabe en el límite de trabajo en curso | Todo el alcance, por etapas |
| Acción propia | Ordenar el product backlog; meter algo a mitad de sprint (cuesta) | Subir o bajar el límite de WIP | Inspeccionar el documento de la etapa |
| Pruebas | Cuando se quiera, una ronda al día | Igual | Solo en la etapa de pruebas |
| Gráfica | Burndown del sprint y velocidad | Flujo acumulado, tiempo de ciclo | Curva S contra el plan; documentos |
| Ceremonias | Planificación, diaria, revisión, retrospectiva | Diaria ante el tablero, reposición, revisión del flujo | Arranque, comité de seguimiento, firma de etapa, lecciones aprendidas |

Las ceremonias con `opciones` se pueden jugar desde «Ceremonias». No asistir no castiga (el equipo la hace sin el jugador): se pierde lo que se habría ganado llevándola bien. Así los robots de calibración, que no asisten, dan lo mismo que antes.

Pendiente respecto a la propuesta: arrastrar tarjetas con el ratón (hoy se asigna con botones), bugs como tarjetas propias, la ayuda «Óscar revisa un PR por ti» y la medición del §8.

## Estado real (2026-10-12)

Respuesta corta a «¿ya se implementó el tablero vivo?»: sí, en versión reducida.

| De la propuesta | Estado |
|---|---|
| Tablero del equipo visible durante la jornada | Hecho |
| Asignar, priorizar y revisar | Hecho, con botones y con el panel abierto (el reloj espera) |
| Variantes por metodología y gráficas | Hecho |
| Efecto sobre el proyecto | Parcial: un factor entre 0,80 y 1,15 sobre el avance del día |
| Arrastrar tarjetas | No |
| Tarjetas que llegan solas en tiempo real | No (salen 1 o 2 terminadas por día) |
| Errores como tarjetas propias | No |
| Cansancio por persona | No (el cansancio es del equipo) |
| Hito de despliegue en el tablero | No en el tablero. El despliegue es ahora una fase propia, la Fase 3 (ver `docs/03-calidad/juego/C3-feedback-beta-1.md`, §22) |
