# C2a · Correcciones de la prueba, tutorial guiado, recolección 3D simulada y trabajo de oficina

**Rama:** `feature/tutorial-y-correcciones`
**Qué incluye:** todo lo que salió al probar la entrega C en Play, menos los minijuegos visuales, que van
en C2b. Son dos bugs, la entrevista sin patrones, Scrum y Cascada en el N0, la Fase 1 con vuelta atrás y
recolección 3D simulada, el trabajo de oficina y el N0 convertido en un tutorial guiado.

---

## Qué se hizo

### 1 · Los dos bugs

| Bug                                                                      | Causa                                                                                                                                                                                                                                                                               | Arreglo                                                                                                                                                                                                                                                                                                                                                          |
| ------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Al cerrar el N0, `MissingComponentException` en «Radar» y pantalla vacía | El radar es un `Graphic` propio, y un `Graphic` propio no trae `CanvasRenderer` como sí lo traen `Image` o TMP. En el editor, `GetComponent` devolvía un «falso null» que uGUI guardaba como si fuera el componente. Al destruir la pantalla, reventaba y cortaba el flujo a medias | `[RequireComponent(typeof(CanvasRenderer))]` en `GraficoRadar`, y `UiKit.Radar` lo añade antes. Además, **ningún paso del flujo puede volver a dejar la pantalla vacía**: si uno falla, sale «El juego no pudo seguir en este punto», con el error, **Reintentar** y **Volver al menú**. El router tampoco deja en la pila una pantalla que falló al construirse |
| Con el reloj parado se podía viajar                                      | El mapa solo miraba si la jornada corría, no si estabas dentro de una escena                                                                                                                                                                                                        | `LevelRunner.PuedeMoverse` = sin escena abierta, sin pausa y con la jornada corriendo. Lo usan el mapa, recoger coleccionables, atender y el trabajo de oficina, en la pantalla **y** dentro del runner. El mapa dice «Estás ocupado aquí: termina lo que tienes abierto»                                                                                        |

### 2 · La entrevista ya no se aprueba por la forma

Reescribí las 45 opciones. Los enunciados y las respuestas correctas no cambian (siguen repartidas
5/5/5), y el reparto de los susurros tampoco.

- Las tres opciones de cada pregunta **explican algo** y tienen una longitud parecida (la más larga, como mucho 1,16 veces la más corta).
- Las incorrectas son **ideas erróneas plausibles**, no absurdos: «la integración continua sirve para mantener el código con un mismo estilo», «el Scrum Master maximiza el valor porque dirige el proceso»…
- La correcta es la más larga (o empata) solo en 6 de 15 preguntas.

Esto lo vigila un test nuevo, `La_entrevista_no_se_puede_aprobar_por_la_forma_de_las_respuestas`.

### 3 · Metodologías del N0 y sus motivos

- El concurso ofrece **Scrum, Cascada y Kanban**. El robot juega el N0 30 veces con cada una, y el tutorial enseña sus dos decisiones en las 90 partidas.
- Al elegir una metodología salen **solo sus 8 motivos** (4 buenos y 4 trampa, mezclados). Cambiar de metodología borra el motivo elegido. «Es Scrum pero sin reuniones» ya solo sale al elegir Kanban, donde es la trampa que tiene que ser.

### 4 · La Fase 1: ir y volver, y la recolección 3D simulada

- Seis pasos: **Encargo → Recorrido → Metodología → Calidad → Arquitectura → Resumen**. «◂ Atrás» y «Siguiente ▸» abajo, y la franja de arriba también se puede pulsar. Lo completado lleva ✓.
- Nada se entrega al motor hasta **«Confirmar y empezar el nivel»**, en el resumen, que tiene «Cambiar» en cada línea.
- **Recorrido:** el plano con sus zonas y cuántas cosas hay en cada una (como «?»), y tres botones: **«Simular recolección 3D: rápida / normal / a fondo»**. Lo encontrado se aplica en el acto y se enseña:
  - las **pistas** aparecen en el encargo y en el paso de Arquitectura;
  - un **personaje** (Javier, en el N1) se une;
  - hay **moral**, **recursos**, una **carta** para el diario y algún **riesgo**.
- **El contrato para el subequipo 3D** (qué recibe y qué devuelve) está en [contrato-recoleccion-3d.md](contrato-recoleccion-3d.md).

### 5 · Trabajo de oficina

En la jornada, cuando no pasa nada, aparece la tarjeta **«Trabajo en tu escritorio»** con tres tareas por
nivel:

| Tarea                           | Minijuego (de práctica)                       | Mejora                   |
| ------------------------------- | --------------------------------------------- | ------------------------ |
| Revisar un diagrama             | V1, un diagrama pequeño con uno o dos errores | Documentación ↑, deuda ↓ |
| Ordenar el trabajo / el backlog | V3, cinco o seis tarjetas                     | Avance ↑, cliente ↑      |
| Probar lo que hay               | V2, 12 o 30 horas                             | Cobertura ↑              |

- Cuestan **45 minutos (N0) o 60 (N1)** del reloj, cobrados al empezar: mientras tanto pueden sonar o caducar avisos.
- Se hacen una vez al día cada una, y solo desde el escritorio.
- **No cuentan para la evaluación** (lo que elegiste): no entran en la traza ni en el radar, y no encadenan nada. La pantalla del minijuego lo dice arriba.
- Son **seis escenas nuevas** (`MJ-OF-N0-*` y `MJ-OF-N1-*`). No están en el índice del director, así que nunca llegan como aviso, y el validador comprueba que no se cuelen en él.
- Faltó una tarea de commits: el grafo de commits es la escena más compleja y la dejo para cuando esté el lienzo visual (C2b).

### 6 · El N0 como tutorial guiado

La **guía** es una burbuja de Marisol, siempre encima de todo. Dice qué hacer y por qué, y **señala con un
marco que parpadea** la pieza de la pantalla de la que habla.

- **47 pasos** en `narrativa/guia-tutorial.json`, con frases cortas y ejemplos de la vida diaria («la metodología es como una receta», «la arquitectura es como el plano de una casa»). La mayoría tiene **«Quiero saber más»** con la explicación de verdad.
- Cubre las tres metodologías explicadas fácil, las fichas de calidad, la arquitectura, el reloj, el mapa, el escritorio, los avisos (y por qué caducan), decidir sin ver la nota, los retos, el trabajo de oficina, los coleccionables, irse o quedarse, el resumen, el lanzamiento y cómo leer el Dashboard.
- Tiene dos tipos de paso:
  - los que se cierran con **«¡Entendido!»**, que **paran el reloj** mientras se leen (también el del minijuego);
  - los que **piden hacer algo** («Pulse el pasillo»), que no paran el reloj y **se cierran solos al hacerlo**.
- Cada paso sale **una vez por perfil**. **«¿Qué hago ahora?»** (arriba en el día, en la Fase 1, en los minijuegos y en el Dashboard) repite la última ayuda, y **«Volver a ver la guía»** en el menú del perfil la reinicia.
- La guía **solo existe en el N0**: en el N1 nadie explica nada, y así lo dice su último paso.

Las pantallas no saben qué pasos hay: avisan de lo que pasa (`GuiaView.Avisar("alerta.suena")`), de lo que
hizo el jugador (`GuiaView.Hecho("ir-a-zona:pasillo")`) y registran las piezas señalables. El contenido
decide qué se dice y cuándo, y el validador rechaza un disparador, una acción o una zona que no existan.

---

## Cómo probarlo

1. Unity → Play → crea un perfil **nuevo** (la guía sale una vez por perfil) → «Empezar».
2. **La entrevista:** fíjate en que ya no hay una opción obviamente más larga. Sigue saliendo el susurro.
3. **La Fase 1:** salen las burbujas de Marisol.
   - En **Recorrido**, pulsa «Normal». Mira lo encontrado y vuelve a **El encargo**: tiene la pista.
   - En **Metodología**, elige Scrum y mira sus motivos; cambia a Cascada y mira que cambian.
   - Ve a **Resumen**, pulsa «Cambiar» en algo, vuelve y **Confirma**.
4. **El día 1:** sigue las burbujas. Pide ir al pasillo: el reloj sigue corriendo y la burbuja se cierra sola al llegar.
   - Recoge la advertencia y vuelve al escritorio cuando te lo pida.
   - Haz una **tarea de oficina**: el reloj salta sus 45 minutos, el minijuego dice «práctica» y al volver sube la barra que toca.
5. **El día 2, con un aviso:** ábrelo y, **mientras la decisión está abierta, intenta pulsar el mapa: está desactivado** y lo dice.
6. **El minijuego del día 3:** mientras la burbuja explica cómo se juega, el reloj del reto no baja.
7. **El cierre del N0:** lanzamiento → Dashboard (con el radar) → «El Aplauso» → **empieza el N1**, sin excepciones.
8. **En el N1:** sin burbujas. En el Recorrido está **Javier**; si lo encuentras, al cerrar el nivel sube `FLG_JAVIER_CONFIANZA`.

**Test Runner:** `EditMode > Run All`. En mi arnés son **522 en verde** (11 nuevos): la forma de la
entrevista, el N0 con Cascada, la recolección (determinista, sus efectos una sola vez, rechaza lo que el 3D
no podía devolver, el flag de Javier solo al cerrar, viaja en el guardado), la oficina (tiempo, sin traza ni
radar, una vez al día, solo en el escritorio, guardado), el robot haciendo trabajo de oficina en los dos
niveles y la guía.

---

## Lo que tienes que saber

**1 · La recolección se aplica en cuanto termina, no al confirmar.** Las tres decisiones esperan al
resumen porque son decisiones; el recorrido es algo que pasó en el mundo. Por eso no se puede repetir.

**2 · El director del N0 no fija los días de los avisos.** El plan decía «alertas deterministas en días
concretos». Al final la guía se engancha a **lo que pasa** (el primer aviso, la primera decisión, el primer
reto), no al día, y así funciona con cualquier semilla sin tocar el director. El robot confirma que las dos
decisiones y el reto del tutorial salen en todas las partidas.

**3 · Una cosa que no hice:** atenuar el resto de botones mientras la guía pide una acción. La burbuja y
el marco dejan claro qué pulsar; atenuar exige saber qué botón es cada acción en cada pantalla, y lo dejo
para cuando lo veas jugar y me digas si hace falta.

**4 · Los JSON de los dos niveles salen reformateados** (las listas de números cortas, una por línea),
porque los reescribí con un script. El contenido es el mismo más las secciones nuevas.

**5 · Compila contra tu Unity, pero no lo he visto.** Mira sobre todo **dónde cae la burbuja** (abajo en
el centro, o arriba si lo señalado está abajo) y **el marco que parpadea**.

---

## Los comandos de git

Abre Unity y deja que importe: tiene que crear los `.meta` de `Core/Fase1/`, `Core/Oficina/`,
`Unity/Guia/`, `narrativa/guia-tutorial.json` y los `MJ-OF-*.json`.

```bash
git checkout -b feature/tutorial-y-correcciones

git add "game/proyecto de grado/Assets/Scripts" \
        "game/proyecto de grado/Assets/StreamingAssets" \
        docs/03-calidad/juego/C2a-tutorial-y-correcciones.md \
        docs/03-calidad/juego/contrato-recoleccion-3d.md

git status

git commit -m "feat(juego): tutorial guiado del N0, Fase 1 con vuelta atras y recoleccion 3D simulada, trabajo de oficina, entrevista sin patrones y fix del radar"
```

En el `git status`, comprueba que **no** entra nada de `docs/03-calidad/juego/minijuegos-mockups/`: eso es de C2b.
