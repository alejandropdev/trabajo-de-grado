# B1 · El contenido completo de N0 y N1

**Rama:** `feature/contenido`
**Qué incluye:** todo lo que quedaba de la fase B1 en una sola entrega: el Nivel 0 entero, la
entrevista, la agenda narrativa y los guiones de N0 y N1, los coleccionables y su colocación, y las
seis piezas pequeñas de motor que ese contenido necesitaba para poder existir.

> Hasta B1.3b el trabajo iba en paradas pequeñas. Esta entrega junta el resto de la fase, como
> pediste. Las paradas anteriores (censo de flags, metodologías, perfil de N1, eventos de N1 y el
> arreglo de las cadenas) ya están commiteadas y tienen su propio minidoc.

---

## El resultado, en una línea

**El catálogo de N0 y N1 carga completo y se juega entero.** Son 12 eventos, 2 niveles, 3
metodologías, 7 cinemáticas del director, 17 guiones, 15 preguntas, 46 flags y 25 coleccionables.
Lo validan **482 pruebas en verde**, entre ellas 240 partidas completas jugadas con el contenido real.

---

## 1 · Seis piezas de motor

El contenido pedía cosas que el motor no sabía hacer. Las añadí pequeñas, cada una con sus pruebas,
y ninguna cambia un contrato que ya existiera.

| Pieza                                      | Qué resuelve                                                                                                                                                                                                                                                              | Dónde                                                       |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| **`soloNiveles`** en eventos y cinemáticas | Los catálogos son globales: sin esto, `CIN-1.0` (día 1) saldría también el día 1 del concurso, y los eventos de la barrera de N1 aparecerían en N0. Sigue el mismo patrón que `SoloMetodologias`. Vacío = de todos los niveles                                            | `EventDefinition`, `NarrativeBeat`, filtro en `GameSession` |
| **Prueba de admisión**                     | Esquema de las 15 preguntas, el corrector (1 por acierto, 0 por fallo o sin responder), el susurro de HH, el porcentaje, **la ganancia de Hake** y el guardado en `PlayerProfile.resultadoPreTest`. `GananciaDeHake` no existía, aunque yo había dicho que sí             | `Core/Evaluacion/PruebaDeAdmision.cs`                       |
| **Coleccionables**                         | Un solo tipo para las cinco series implementadas (carta, USB, código, startup, advertencia). El validador exige los campos de cada serie: **una carta o una startup sin fuente no carga**                                                                                 | `Core/Coleccion/Coleccionable.cs`                           |
| **Guiones**                                | Hasta ahora una cinemática tenía id, ventana y variante, pero ningún sitio donde escribir lo que se dice. Un guion lleva sus líneas por variante, el momento en que se reproduce y, si acaba en una elección, sus opciones                                                | `Core/Narrativa/Guion.cs`                                   |
| **Elecciones de escena → flags**           | Aceptar a Marta, fijarse en la pared dorada o abrir el log de build tienen que dejar un flag escrito. `RegistrarEleccion` lo anota (una sola vez, viaja en el guardado) y **`Cerrar()` lo escribe**. INV-6 no cambia: sigue siendo el único sitio donde se escriben flags | `GameSession`, `RuntimeState.EleccionesNarrativas`          |
| **Flags legibles en condiciones**          | Seis flags del censo existen solo para colorear diálogos y nadie los podía leer. Una condición ya puede decir `FLG_ORIGEN == 1`. **Leer** un flag no rompe INV-1, que prohíbe **escribirlos**. El validador comprueba que cada flag leído esté en el censo                | `GameSession.TryGetValue`, `SchemaValidator`                |

Y el cargador conoce tres archivos nuevos, todos opcionales como la narrativa:
`narrativa/guiones.json`, `prueba-de-admision.json` y `coleccionables.json`.

### Lo que el validador comprueba ahora, con todo cargado

- Cada cinemática que el director puede emitir tiene guion, y ese guion tiene escrita **cada
  variante que el coloreo puede elegir**. Una escena en blanco no carga.
- Cada opción de guion escribe un flag que existe en el censo.
- Cada coleccionable que una zona dice tener existe, y está en **un solo sitio** de todo el juego.
- La entrevista tiene exactamente 15 preguntas, y cada respuesta correcta es una de sus opciones.
- Todo `soloNiveles` apunta a un nivel que existe.

---

## 2 · El contenido

### Nivel 0 · «El Concurso del Ascensor»

| Momento           | Escena                | Qué pasa                                                                   |
| ----------------- | --------------------- | -------------------------------------------------------------------------- |
| antes del nivel   | `CIN-0.0` El Ascensor | El ascensor de vidrio, la megafonía, el reglamento                         |
| antes del nivel   | `ENT-0` La entrevista | Marisol pregunta el origen (→ `FLG_ORIGEN`) y las 15 preguntas; HH susurra |
| antes del nivel   | `ENT-0.FIN`           | «Su puesto es el siete»                                                    |
| antes del nivel   | `CIN-0.1` El Problema | Enseña a leer un ticket: prioridad, restricción, comentarios               |
| Fase 1            | `GUIA-0.F1`           | Marisol explica las tres decisiones                                        |
| día 1             | `TUT-0.1`             | El reloj corre; las alertas se atienden desde tu puesto                    |
| día 2             | `TUT-0.2`             | Marta te ofrece alianza (→ `FLG_MARTA_ALIADA`) y explica los avisos        |
| día 3             | `TUT-0.3`             | Se abre el auditorio; mira las paredes                                     |
| día 4             | `TUT-0.4`             | Marta: «quedarse es un préstamo»                                           |
| día 5             | `TUT-0.5`             | La entrega; la barra se congela seis segundos                              |
| después del nivel | `CIN-0.2` El Aplauso  | El badge gris, el holograma, el USB dos segundos                           |

- **El encargo del concurso** (ticket NEX-4417): un registro de visitantes para la recepción de N1.
  La restricción del ticket (un solo equipo, nadie instala nada) es la que sostiene la arquitectura
  correcta, una aplicación web sencilla.
- **5 días** con jornada de 09:00 a 17:00 y reloj más rápido que en N1. Solo se ofrecen **Scrum y
  Kanban**. El motor recorta el sprint a los 5 días, así que el tutorial enseña igual todas las
  ceremonias: planificación el día 1, reunión diaria, y revisión más retrospectiva el día 5.
- **2 eventos de tutorial**: `EV-TUT-01` (el requisito que nadie escribió) y `EV-TUT-02` (la demo
  de mañana), ambos con coste diferido en la opción cómoda.
- **4 zonas**: la sala del concurso (ancla), el pasillo (Marta), la terminal (`whoami`) y el
  auditorio (desde el día 3).

### Nivel 1 · lo que faltaba

- **Cinemáticas del director**: `CIN-1.2` (el standup de Óscar, día 6, que además abre el sótano)
  e `INT-1` (el cuarto, cuando te has ido a casa tres veces).
- **Guiones de flujo**: `CIN-1.0` El Té (→ `FLG_VIO_PANTALLA_DORADA`), `CIN-1.1` La Arquitectura
  (una variante por cada arquitectura), `CIN-1.3` El Deploy Tonto y `CIN-1.4` El Primer Glitch (→
  `FLG_ZERO_NOTADO_N1`).
- **`INT-1` cambia según tu origen**: si vienes del pueblo llamas a tu madre; si vienes de la
  universidad, a un compañero que sigue buscando trabajo. Es el primer flag de color de diálogo que
  tiene un lector, como pedía el canon.
- **Los 10 eventos de N1** quedan atados a N1 con `soloNiveles`.

### La entrevista

Las 15 preguntas, las opciones y los susurros son los que aprobaste, con **dos cambios**. El
primero es de presentación: en el documento, **12 de las 15 respuestas correctas eran la B**. Un
alumno que contestara siempre B sacaría un 80 % sin saber nada, y el pre-test mediría su estrategia
en vez de lo que sabe. Reordené las opciones para que queden 5 A, 5 B y 5 C, sin cambiar el texto
de ninguna, y una prueba vigila que no vuelva a pasar. El segundo es el paréntesis que quité de la
pregunta 15 (lo explico en la sección 3).

### Los coleccionables

Los que aprobaste para implementar: **8 cartas**, **el fragmento 1 del USB**, **2 startups**
(Webvan y Pets.com) y **las 12 advertencias**. Además van los dos códigos de terminal que el canon
ya situaba en N0 y N1 (`whoami` y `ls -la /var/log/legacy`). Son 25 en total, y **cada uno está en
una zona concreta del mapa**; una prueba comprueba que ninguno se quede sin sitio.

---

## 3 · Lo que decidí o cambié, por transparencia

**Sobre el documento aprobado:**

| Cambio                                                                                                                          | Por qué                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| ------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Revisé las fuentes y cambié dos que no pude verificar**: la cita del caso de Harvard sobre Webvan y el libro del _bus factor_ | La nota de producción del canon exige fuente verificable. Webvan y Pets.com citan ahora el hecho documentado (quiebra en julio de 2001, liquidación en noviembre de 2000), sin inventar el título de un artículo. El _bus factor_ cita un artículo académico que trata exactamente ese concepto (Cosentino, Cánovas Izquierdo y Cabot, SANER 2015). **Te recomiendo revisar las 10 fuentes contra el original antes de publicar**: son hechos reales y es tu tesis la que los firma |
| El Burnout ya no dice «síndrome reconocido por la OMS»                                                                          | La OMS lo clasifica como **fenómeno ocupacional, no como enfermedad**. Lo escribí como lo dice la CIE-11                                                                                                                                                                                                                                                                                                                                                                            |
| La pregunta 15 perdió el paréntesis «(Ley de Goodhart…)»                                                                        | Dentro de la opción correcta le regalaba la respuesta al alumno                                                                                                                                                                                                                                                                                                                                                                                                                     |
| La megafonía dice «**Cinco días**. Un problema real. Un ganador.»                                                               | El canon dice «Cuarenta y ocho horas», pero pediste 5 días                                                                                                                                                                                                                                                                                                                                                                                                                          |
| **Marisol coordina también el concurso** y es la voz que guía el tutorial                                                       | Hacía falta una voz diegética para las guías. Ella ya estaba en escena, y así el final F9 pesa más: la persona que te acompañó cinco días no aprende tu nombre                                                                                                                                                                                                                                                                                                                      |
| Dos advertencias (`COL-ADV-01` y `COL-ADV-11`) están en N0, no en N1                                                            | El tutorial tiene que enseñar que en las paredes hay cosas que recoger                                                                                                                                                                                                                                                                                                                                                                                                              |
| La carta canon `COL-DEV-01` «El Mítico Hombre-Mes», que el canon pone en N0, **no está implementada**                           | No estaba entre las 8 que aprobaste. Si la quieres, es una entrada en el JSON                                                                                                                                                                                                                                                                                                                                                                                                       |
| Cambié «coches» por «carros» y «cómo vais» por «cómo van»                                                                       | El juego es para estudiantes de la Javeriana, y el propio HH dice «carro» en su frase canon                                                                                                                                                                                                                                                                                                                                                                                         |

**Sobre el contenido nuevo:**

- **El enunciado del concurso lo concreté yo.** El canon solo dice que llega como ticket P1 y que
  enseña _alcance, requisito, restricción y criterio de aceptación_. El registro de visitantes cumple
  las cuatro cosas y no se pisa con Cradle Lifts. Es fácil de cambiar si tienes otra idea.
- **N0 tiene 3 de drama, no 2.** Kanban multiplica el drama por 0,9 y el motor redondea hacia abajo:
  con 2, el tutorial enseñaba **una sola decisión** a quien eligiera Kanban. Lo encontraron las
  pruebas: `EV-TUT-02` salía en 30 de 60 partidas. Ahora sale en 60 de 60 con las dos metodologías.
- **La línea extra de Sarah en `INT-1`**, la que el canon condiciona a haber visto la pared dorada,
  no está: el coloreo elige una sola variante, y ya la usa el origen. Se puede combinar más adelante.

---

## 4 · Cómo probarlo

**En Unity:** abre el proyecto (se generan los `.meta` de los archivos nuevos) y lanza
`Test Runner > EditMode > Run All`. Tiene que salir todo en verde.

Los tests nuevos van en tres grupos:

| Archivo                               | Qué comprueba                                                                                                                                                                                                                                                    |
| ------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AdmisionColeccionYGuionesTests` (25) | El corrector y la ganancia de Hake, y que el validador rechace lo que rompería una partida o una medición: carta sin fuente, startup sin causa, fragmento 9 del USB, coleccionable en dos sitios, escena sin guion, variante sin escribir, flag fuera del censo… |
| `GameSessionTests` (+7)               | Un evento o una cinemática de otro nivel no sale nunca; una elección de escena se escribe **al cerrar y no antes**, no se deshace y **sobrevive a guardar y recargar**; una condición puede leer un flag                                                         |
| `ContenidoRealTests` (10)             | **El contenido de verdad, jugado entero**: 60 partidas de N0 y 180 de N1                                                                                                                                                                                         |

`ContenidoRealTests` es la que vigila el contenido a partir de ahora. Si mañana cambias un JSON y
rompes una cadena, una cinemática o una puerta, **sale en rojo**. Comprueba:

- N0: cada día trae su guía, en orden (`TUT-0.1` el día 1 … `TUT-0.5` el día 5); ningún evento de
  N1 se cuela; **las dos decisiones del tutorial salen en todas las partidas**.
- N1: las cadenas se respetan y **se cierran siempre** (la deuda volvió 86 de 86 veces; Óscar se fue
  35 de 35); `CIN-1.2` sale el día 6; el sótano no se abre antes; la planificación de Scrum cae los
  días 1 y 11; `INT-1` cambia según tu origen.
- La entrevista: acertarlo todo da 100 %, y **ninguna letra concentra las respuestas correctas**.
- Todos los coleccionables están en algún sitio.

Fuera de Unity, estas pruebas leen el contenido de la ruta que diga la variable de entorno
`NEXUS_STREAMINGASSETS`.

---

## 5 · Lo que queda para C (no es de esta fase, pero lo dejo escrito)

| Pendiente                                                                                                                                          | Por qué es de C                                                                             |
| -------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| **Recoger un coleccionable**: marcarlo en `PlayerProfile.coleccionablesGlobales` y volcar `FLG_CARTAS` y `FLG_CODIGOS` al cerrar                   | Es la mecánica de la pantalla de coleccionables (C7). El contenido y su validación ya están |
| **`FLG_VOSS_AFINIDAD` según la arquitectura elegida** (canon: monolito +1, microservicios −1)                                                      | Es una regla del nivel sobre la decisión de Fase 1; va con la pantalla de Fase 1 (C3)       |
| Los minijuegos de N1 (`minijuegos/indice.json`)                                                                                                    | C5. `EV-ALC-01` y `EV-BUE-02` dependen de ellos                                             |
| N0 solo enseña **tiempo y calidad**; dinero y deuda se ven bloqueados                                                                              | Es del dashboard (C4); el motor no tiene nada que decir                                     |
| **Orden de las escenas de flujo**: `antesDelNivel` por `orden`; `CIN-1.4` (cierre) **antes** de `Cerrar()`, para que su elección llegue al volcado | Lo hace el `ScreenRouter` (B2/C2). Está escrito aquí para que no se olvide                  |

---

## Los comandos de git

Una sola entrega, un solo commit. Abre Unity antes, para que genere los `.meta` de las carpetas y
archivos nuevos (`Coleccion/`, `narrativa/` y los JSON y scripts nuevos).

```bash
git checkout -b feature/contenido

git add "game/proyecto de grado/Assets/Scripts/Core" \
        "game/proyecto de grado/Assets/Scripts/Tests/editMode" \
        "game/proyecto de grado/Assets/StreamingAssets" \
        docs/03-calidad/juego/B1-contenido-completo.md

git commit -m "feat(contenido): N0 y N1 completos — entrevista, guiones, coleccionables y el motor que los sostiene"
```
