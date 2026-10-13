# C3 · Feedback de la beta 1.0: guía fiable, minijuegos que se ganan, consecuencias reales y relaciones

**Rama:** `feature/contenido`

**Qué incluye:**
- las 14 observaciones del documento *Feedback 1.0*;
- los arreglos pedidos aparte: la guía que se colgaba, Estimación de pruebas imposible y una negociación sin efecto;
- las relaciones con personajes en la Fase 2;
- la propuesta de una mecánica nueva para el tiempo muerto, solo como documento: [`propuesta-mecanica-tablero-vivo.md`](../../02-diseno/propuesta-mecanica-tablero-vivo.md).

---

## 1 · Las 14 observaciones, una a una

| # | Observación | Qué se hizo |
|---|---|---|
| 1 | Pausa para seguir luego desde el mismo perfil, no la de Unity | «Menú» ahora también existe en la Fase 1 y dentro de los minijuegos, no solo en el día. «Guardar y volver al menú» guarda la partida donde está. La Fase 1 a medio decidir viaja en el guardado (`RuntimeState.Fase1Borrador`). Al continuar no se repiten la entrevista ni las escenas de apertura (`RuntimeState.IntroVista`). En un minijuego, salir entrega lo que haya: un reto nunca queda a medias en el guardado. |
| 2 | «Ver el tema» y «Prueba del motor» son de admin | Solo se crean con `UNITY_EDITOR` o `DEVELOPMENT_BUILD`. |
| 2 | No se ve que el campo de nombre está escribiendo | El campo se enfoca solo al abrir la pantalla. TMP ignoraba `caretColor` sin `customCaretColor`: ahora el cursor es cian, ancho y parpadea. La indicación desaparece al enfocar y aparece un borde. Dice «Nombre del jugador». |
| 2 | Login sin lista de perfiles | **Trabajo futuro**, como sugiere el propio feedback. |
| 3 | «Diario de campo» no debería salir aún | En la pantalla de partidas solo aparece cuando el perfil ya encontró algo. |
| 4 | Respuestas de la entrevista que solo dicen «Correcta» | Los susurros de acierto de las preguntas 1, 3, 5, 7, 10, 12 y 14 dicen ahora el porqué. Los de fallo añaden una pista de la idea correcta sin decir la opción. |
| 5 | La conversación de Marisol sobre las 3 decisiones | GUIA-0.F1 reescrita: qué es cada decisión (metodología, calidad, arquitectura), con ejemplo y nombre técnico, y que el porqué se evalúa. Los guiones de Marisol pasan a tutear. |
| 6 | Explicar las métricas antes del recorrido | Nuevo panel «Cómo se mide tu proyecto» en el Encargo: cada métrica con qué la mueve y su «?», y lo que exige este nivel para que el lanzamiento salga bien. La guía lo señala (`F1-ENCARGO-2`). |
| 7 | Qué es un sprint, qué es cada ficha; releer a Marisol; ver que hay 8 razones | Hay un glosario nuevo (`narrativa/glosario.json`, 56 términos de toda la asignatura) con chips «?» en: <br>• metodologías, con «Palabras que vas a ver»; <br>• cada ficha de calidad; <br>• cada arquitectura; <br>• las barras del día. <br>«Lo que me dijo Marisol» permite releer todas las burbujas del nivel. Las razones se ven en dos columnas con «Elige 1 de N razones». |
| 8 | Móvil o web ofrecen las mismas razones | Cada arquitectura trae las suyas (`ArquitecturaOpcion.Razones`), y al cambiar de arquitectura se borra la razón elegida. El validador exige que cada razón evaluada esté entre las ofrecidas. |
| 9 | Feedback en «¿Qué hacemos primero?» | Pasa lo mismo en todos los backlogs: <br>• en el tutorial, cada tarjeta dice qué necesita y las dependencias rotas salen en rojo en vivo; <br>• responder al cliente mueve el tablero y el cliente contesta en un globo; <br>• al cerrar se enseña el orden óptimo. |
| 10, 11 | Más feedback y contexto en los minijuegos, sobre todo en el tutorial | Hay un modo acompañado (`LevelProfile.minijuegosAcompanados`, activo en N0): <br>• Detectar dice √/× con el porqué al marcar y explica cada etiqueta; <br>• Repartir dice cuánto atrapa cada pila. <br>La guía explica cada verbo paso a paso señalando su control real (estilo «círculo rojo»). El cierre de todos los minijuegos ahora tiene: <br>• título por verbo; <br>• la razón de la rúbrica; <br>• los defectos que se escaparon; <br>• una solución posible. <br>Las instrucciones de los 12 minijuegos siguen la estructura *qué es → qué haces → cómo ganas*. |
| 12 | Coleccionables: guardado automático y «revisar el diario» | Ya se guardaban al recogerlos. El modal ahora lo dice («√ Guardado en tu diario») y ofrece «Seguir» o «Revisar el diario». |
| 13 | La peor partida posible salió bien | Ver la sección 3. |
| 14 | No parece un tutorial | Los 57 pasos de la guía del N0 están reescritos en tú y para alguien que nunca vio IS. También hay: <br>• glosario; <br>• modo acompañado; <br>• conversaciones con preguntas de la asignatura. <br>N1 conserva solo 6 burbujas: es más libre. |

## 2 · La guía de Marisol ya no se cuelga

**Causas encontradas:**
- La burbuja no bloqueaba los clics, así que los disparadores se encolaban y salían tarde y en otra pantalla.
- Algunos pasos esperaban acciones ya hechas o imposibles:
  - `MAPA-2` pedía ir a la sala donde ya estabas;
  - `CIERRE-2` esperaba «Irme a casa» aunque eligieras quedarte;
  - `ALERTA-1` esperaba un aviso que ya había caducado.
- «¿Qué hago ahora?» no hacía nada en la segunda partida de un perfil.
- `MJ-2` explicaba Detectar en cualquier minijuego.

**Arreglo** (`GuiaView` y `Guia.cs`):

| Regla | Qué hace |
|---|---|
| Modal | Un paso sin acción pone un velo que bloquea la pantalla hasta «¡Entendido!». |
| Contexto | Los disparadores de pantalla son *contextuales*. Uno nuevo descarta lo que quedaba de la pantalla anterior; los de evento esperan su turno. |
| Acción ya hecha | `RegistrarComprobador`: si lo que pide el paso ya está hecho (estás en esa zona, el día ya empezó…), el paso no sale. |
| Acción imposible | `Cancelar`: la llaman la alerta caducada, «Quedarme» y el fin de la prórroga. |
| Visto al cerrar | Un paso se marca visto al cerrarlo, no al mostrarlo. |
| Releer | «¿Qué hago ahora?» relee la pantalla actual, vista o no. Si no tiene pasos propios, sale la ayuda genérica `ayuda.*` (nivel `*`). |
| Historial | «Lo que me dijo Marisol» relee todo lo dicho en el nivel. |
| Colocación | Se coloca la burbuja un frame después, con el layout ya recalculado. |

## 3 · Consecuencias reales: el lanzamiento exigente

`CalculoDeLanzamiento` (puro, en `Core/Evaluacion`) puntúa sobre 100:

| Factor | Peso |
|---|---|
| Alcance entregado | 30 |
| Errores que encontró el cliente | 20 |
| Riesgo | 20 |
| Satisfacción del cliente | 15 |
| Calidad de las decisiones | 15 |

**Niveles del resultado:** **Salió bien**, **Salió, pero con problemas** y **Salió mal**.

**Reglas duras:**
- Un factor en zona «mal» hunde el resultado a «mal».
- Para «bien» hace falta el puntaje y como mucho un factor «regular».

Los umbrales están en el JSON de cada nivel (bloque `lanzamiento`).

**Consecuencias de cada nivel:**
- Moral y reputación cambian según el resultado.
- El nivel solo se supera con «bien».
- Lecciones ofrece «Repetir el nivel» (vuelve a los flags del inicio del nivel, `SaveGame.FlagsAlEmpezarNivel`) o «Seguir de todos modos».
- Las escenas de cierre tienen variantes `lanzamiento-bien/con-problemas/mal` (CIN-0.2 y CIN-1.3).

**Incentivos arreglados:**
- En EV-TUT-01, la opción correcta ya no empeora el resultado.
- En EV-ALC-01 y EV-EQ-03, la correcta ya no queda por debajo de la aceptable.
- Los efectos `"Dias"` antes no hacían nada. Ahora se cobran en avance (`EffectApplier.CobrarDias`, 3 puntos por día) y la previsualización lo enseña.

**Calibración.** El robot de `ConsecuenciasRealesTests` juega el contenido real:

| Partida | N0 | N1 |
|---|---|---|
| Todo mal | **Mal** (38/100) | **Mal** (17–21/100) |
| Todo bien | **Bien** (85/100) | **Bien** (99–100/100) |
| La del tester (14/14, 9 errores, riesgo 38) | **Mal** | — |

## 4 · Minijuegos

**Estimación de pruebas.**
- Antes:
  - el paso de la UI era de 2 horas;
  - N1 (30 h) y F2-08 (40 h) no tenían ninguna combinación ganadora.
- Ahora:
  - `RepartirCfg.paso` está en los datos;
  - los presupuestos son 32 h y 42 h;
  - cada uno de los tres retos tiene **exactamente una** combinación ganadora (N0 6/6/0 · N1 10/12/10/0 · F2-08 10/16/6/10);
  - cada tipo trae una pista («suele haber 3 o 4») para razonarla;
  - el validador rechaza un reparto imposible (`RepartirEvaluador.RepartosGanadores`).

**Negociación del backlog.**
- `OrdenarCfg.tarjetaPedida` y `Respuesta.efectoEnBacklog`:
  - `forzar-arriba`;
  - `intercambiar`;
  - `ninguno`.
- Cada respuesta tiene su `replica`. La respuesta se aplica al tablero al elegirla y se deshace al cambiarla.
- «Obedecer», que es incorrecta, ya no puede dar «todos».
- En la práctica, la respuesta también aplica sus efectos.

**Detectar.**
- En el cierre se explican también las zonas que se escaparon.
- Las instrucciones son nuevas, con las tildes de MJ-F2-02 corregidas.

## 5 · Relaciones en la Fase 2

- Hay un catálogo nuevo, `narrativa/conversaciones.json`:
  - 6 personajes: Marta e Iván en N0; Óscar, Sarah, Javier y Voss en N1;
  - 31 conversaciones, cada una con una pregunta de la asignatura en tono de charla;
  - la respuesta del personaje es la explicación.
- Reglas:
  - Se habla en la sala de cada uno, una vez por persona y día.
  - La charla cuesta minutos de reloj.
  - La confianza desbloquea **ayudas puntuales**, una vez cada una:

    | Ayuda | Qué hace |
    |---|---|
    | revelar defectos | en Repartir |
    | mostrar dependencias | en Ordenar |
    | pista | en Detectar |
    | +45 s | de reloj en un reto |
    | bajar el cansancio | durante el día |

  - Las ayudas de reto se eligen en la receta, antes de jugar.
  - Al cerrar el nivel, la confianza se vuelca a `FLG_SARAH_VINCULO` y `FLG_JAVIER_CONFIANZA` (INV-6).
- Panel «Tu equipo» en el día.

## 6 · Pruebas

**Tests nuevos (EditMode):**

| Archivo | Qué comprueba |
|---|---|
| `JugabilidadDeContenidoTests` | Cada reparto y cada backlog se pueden ganar; ganador único en pruebas; la negociación mueve el tablero. |
| `ConsecuenciasRealesTests` | La peor y la mejor partida de N0 y N1; la del tester; un día perdido se cobra. |
| `GuiaYRelacionesTests` | Contexto de la guía; ayuda genérica en cada pantalla; tuteo; glosario completo; razones distintas por arquitectura; relaciones: reglas, ayudas, guardado e INV-6. |

**Cómo se ejecutan fuera de Unity:**
- Core y los tests EditMode no dependen del motor.
- Un `.csproj` que incluya `Assets/Scripts/Core/**` y `Assets/Scripts/Tests/editMode/**`, con Newtonsoft.Json, NUnit y `Microsoft.NET.Test.Sdk`, los compila y ejecuta con `dotnet test`.
- La variable `NEXUS_STREAMINGASSETS` apunta al contenido.

**Resultado (ronda 1):** 568 de 569 pasan. Tras la ronda 2: 580 de 581.

**El que falla ya fallaba antes de este trabajo:**
- Es `MinijuegoDetectarTests.Tocar_un_senuelo_manda_sobre_haber_acertado_todo`.
- Espera un `EV-EQ-07` diferido que `MJ-F2-02.json` ya no tiene desde el commit `353a72a`.
- Hay que decidir si se restaura el evento en la escena o se actualiza el test.

---

# Ronda 2 · segunda prueba de N0

## 7 · Minijuegos guiados (sustituye al modo «acompañado»)

**El fallo «hice todo bien y me dijo que mal».** Pinchar una pieza para verla la seleccionaba, y la selección se acumulaba. Una marca podía llevar a la vez el problema real y un señuelo:
- la corrección al momento decía «Bien visto»;
- el evaluador lo contaba como falso positivo.

**Arreglo:**
- En diagramas y secuencias, pinchar otra pieza cambia la selección. Solo el grafo de git selecciona varias, con «Limpiar la selección».
- Con andamiaje 3 solo se resaltan las piezas con problema, no los señuelos.

**Modo guiado** (`RecorridoGuiado`, en Core, y `GuiaDelMinijuego`, en Unity):
- Marisol lleva paso a paso por todo el reto y señala la pieza real. Cada paso dice qué hacer y qué hay que mirar.
- Lo que no toca no se hace, y el panel explica por qué.
- El reloj no corre.
- El cierre resume «la próxima vez lo harás tú: esto es lo que tienes que mirar».
- El recorrido se genera de los datos: zonas con su nueva `pista`, el mejor orden del backlog y el reparto ganador.
- `RecorridoGuiadoTests` comprueba que seguirlo da «todos» en las 12 escenas.

**Cuándo se activa:**
- en N0, siempre (`minijuegosGuiados`);
- en los demás niveles, la primera vez que el perfil juega cada minijuego, graduado o de práctica (`PlayerProfile.minijuegosJugados`);
- las repeticiones, libres.

## 8 · Backlog en dos fases

1. **Ordena.** Termina con «Listo: enseñárselo al cliente».
2. **Contesta.**
   - El tablero queda quieto.
   - Cada respuesta parte del orden de la fase 1, así que se puede cambiar de respuesta sin acumular efectos.
   - «Volver a ordenar» deshace la respuesta.
   - El globo dice qué cambió en el tablero.

**Dependencias.** Ya no hay flechas bajo las tarjetas. La tarjeta que va antes de tiempo lleva:
- borde rojo;
- «¡Ojo! Necesita «X» ANTES · está en el puesto N».

La necesitada dice quién la necesita. El puesto se ve grande.

## 9 · Pizarrones de conceptos y apuntes del proyecto

**Pizarrones.** Los «?» abren un pizarrón de varias páginas (`PantallaDePizarra`, mismo marco que la receta, `MarcoDePizarra`). Cada uno de los 56 términos tiene tres páginas:
1. ¿Qué es?, con viñetas dibujadas;
2. qué conlleva;
3. cómo se ve en el juego.

Los 20 iconos están en `IconosDePizarra`.

**Actualización: pizarrones ilustrados y cursos.** El glosario pasó a 83 términos y los pizarrones dejaron de tener un número fijo de páginas. Una página lleva ahora una de tres cosas:
- una escena de tiza a página completa (`dibujo`), hecha a medida para esa idea;
- de 2 a 4 columnas (`columnas`), cada una con su dibujo, su título y su texto;
- las viñetas de antes (`vinetas`).

Las escenas se apuntan en `EscenasDePizarra` (Core, es la lista que valida el contenido) y se dibujan en `DibujosDeEscena` (`Scripts/Unity/Pantallas/Pizarra/`, un archivo por tema, cada escena en un cuadro de 600×300). Las piezas reutilizables (figura, cocinero, edificio, navegador, balanza…) están en `Tizador.Piezas.cs`.

Lo que cambió para el jugador:
- las palabras del oficio de los dos niveles tienen su dibujo, y «Dónde lo verás» va en dos columnas (en el juego / en el oficio);
- cada medida del proyecto explica qué pasa si es alta, qué pasa si es baja, lo recomendable y por qué importa;
- los atributos de calidad, el backlog, el MVP (con boceto, mockup, prototipos, beta y producto completo), SRS, SAD, PTP y PMP, y la aplicación web y la app móvil se explican a fondo;
- Scrum, Kanban y Cascada tienen, además del pizarrón corto, un curso por capítulos (`narrativa/cursos/<id>.json`, botón «Aprender a fondo», `PantallaDeCurso`) de 31, 24 y 23 páginas;
- lo que antes se daba por sabido tiene entrada propia y su «?» donde aparece: ticket, cliente, interesados, roles del equipo, historia de usuario, columnas del tablero, revisión de código, burndown, flujo acumulado, curva S, velocidad, reunión diaria, planificación de sprint, diagramas de componentes y de secuencia, traza, log, defecto, dependencia, presupuesto e incidencia.

Las pruebas (`GuiaYRelacionesTests`) vigilan que no haya emojis, que cada escena ilustre una sola página, que ninguna quede sin usar, que los textos quepan y que los cursos estén enteros.

**Apuntes del proyecto.** Sustituyen a «Lo que me dijo Marisol». Es un pizarrón con lo esencial del nivel:
- el encargo y las pistas del recorrido;
- qué son las tres decisiones y qué elegiste;
- cómo se mide;
- cómo es un día;
- tu equipo.

## 10 · Equipo persistente y charlas diarias

**Equipo persistente.**
- Marta, Óscar, Sarah, Javier y Voss son `persistente`: su confianza se guarda en `SaveGame.Relaciones` al cerrar cada nivel.
- Al empezar el siguiente se hereda, y sus ayudas ya ganadas vuelven a estar disponibles.
- Repetir un nivel la restaura desde la foto `RelacionesAlEmpezarNivel`.

| Personajes | Umbrales de ayuda |
|---|---|
| Persistentes | 7 y 12 |
| Iván (solo N0) | 3 y 5 |

**Charlas diarias.** Si un personaje no tiene pregunta para hoy, ofrece una **charla del estado** (10 min, +1 confianza):
- comenta «su» barra con el valor real;
- por ejemplo, Javier: «Cobertura 18… el cliente va a encontrar unos 9 errores».

Se añadieron 8 preguntas. Un test comprueba que cada día hay con quien hablar en el pasillo (N0), el bullpen y la cafetería (N1).

## 11 · Que cada estadística se note en la Fase 2

- **Pronóstico del lanzamiento**, en vivo, en el día: «Si sigues así: con problemas (58/100) · lo que más te resta: …». Es la misma cuenta que el lanzamiento real (`PronosticoDeLanzamiento`). Un test comprueba que el último día coinciden.
- **Una línea bajo cada barra** con lo que provoca ahora:
  - cuánto frena al equipo;
  - cuántos errores llevará al cliente;
  - si ya está en zona de incidencias.
- **«Por qué avanzaste X puntos»** en el resumen del día: la velocidad base y lo que suma o resta cada barra.
- **Incidencias del estado.** Bloque `incidencias` en el JSON de cada nivel, como mucho una por día. Son deterministas y no gastan azar.

  | Condición | Incidencia |
  |---|---|
  | Deuda ≥ 50 | rompe algo |
  | Cobertura baja | el cliente encuentra un error |
  | Cansancio ≥ 60 | equipo agotado |
  | Moral ≤ 35 | roces |
  | Satisfacción ≤ 35 | reunión urgente |

  Se anuncian como «Consecuencia de tu deuda técnica (55): …». La guía de N0 lo explica la primera vez.
- **Calibración tras las incidencias:** la mejor partida sigue saliendo bien en N0 y N1, y la peor sale peor.

---

# Ronda 3 · fallos en Play

## 12 · El backlog no se podía jugar

**Síntoma:** sin guía, sin poder mover las HU ni negociar, y `MissingComponentException` en `PantallaOrdenar.SetAlfa`.

**Causa:** `GetComponent<CanvasGroup>() ?? AddComponent<CanvasGroup>()`. En el editor, el «null falso» de Unity no es `null` para `??`. La excepción cortaba la construcción del tablero antes de arrancar la guía, y la guía sin pasos lo bloqueaba todo.

**Arreglo:**
- El grupo se crea al construir y se comprueba con `== null`. Además, el panel del cliente no se puede pulsar en la fase 1.
- `PantallaDeMinijuego.Empezar` captura cualquier fallo al montar un reto: lo registra, lo da por omitido y deja volver a la jornada.
- Una guía que no llegó a arrancar ya no bloquea nada.

## 13 · Pantallas comprimidas y clics que no responden

- **Fase 2, columna derecha.** Pronóstico, proyecto y equipo van en su propio scroll, y el diario tiene alto fijo. Antes la columna los encogía y los textos se pisaban.
- **`UiKit.Vaciar`** apaga los hijos antes de destruirlos. Así, durante un frame, la lista vieja y la nueva ya no se suman, y la pantalla no «crece» ni salta.
- **Diálogos modales** (charlas, coleccionables, pausa). Su contenido va en un scroll con alto máximo, para que los botones nunca queden fuera de la pantalla.
- **Fase 1, razones.** Dos columnas que se reparten el ancho real, en lugar de celdas fijas de 760 px.
- **Panel «Marisol te guía».** El porqué tiene alto máximo y scroll, para no aplastar el tablero.
- **Cabeceras.** «Apuntes del proyecto» pasa a «Apuntes».

---

# Ronda 4 · el contexto del proyecto

**El problema.** Se podía jugar bien un nivel sin entender de qué era el proyecto:
- las cajas de los diagramas eran nombres sueltos;
- nada decía qué módulos tenía el sistema, quién era el cliente ni cuánto se llevaba hecho;
- un nivel solo se distinguía de otro por los días y los minijuegos.

Además, el contenido lo empeoraba:
- las prácticas de N0 hablaban de «concursantes» y un «jurado», es decir, de otro sistema distinto al del encargo (el registro de visitantes);
- el backlog de N1 tenía un «informe por sucursal» en un proyecto sin sucursales.

## 14 · El expediente del proyecto

**El bloque `proyecto`.** Va en cada `niveles/nivel-XX.json` (modelo `FichaDelProyecto`, en `Core/Proyecto`) y contiene:
- el sistema y el sector (cómo trabaja);
- el cliente (qué hace y qué le duele);
- los usuarios y la necesidad de cada uno;
- los módulos, con su peso y su avance inicial;
- el diagrama de contexto: actores y flujos;
- las palabras del oficio, con su significado y dónde se ven;
- los documentos que importan (SRS, SAD, PMP, PTP);
- lo que cambia respecto al nivel anterior;
- los temas de la asignatura.

`SchemaValidator` comprueba que los flujos y los usuarios apunten a piezas que existen.

**Cada minijuego** lleva `modulo` y `enElProyecto`.

## 15 · Dónde se ve

- **Fase 1, paso «El proyecto»** (antes, «El encargo»). Muestra, en este orden:
  - el cliente y su sector;
  - el sistema en una imagen: el diagrama de contexto dibujado con tiza (`ExpedienteView.DibujarContexto`);
  - las partes del sistema;
  - las palabras del oficio, cada una con su pizarrón;
  - qué tiene de distinto: comparado con el nivel anterior en días, equipo, alcance y presupuesto;
  - los documentos;
  - el ticket original.
- **«Comprueba que lo entendiste»** (`PreguntasDelProyecto`, determinista). Tres preguntas: una palabra del sector, la necesidad de un usuario y el módulo que la resuelve, y lo que le duele al cliente.
  - No restan: se explica el error y se reintenta, pero hay que contestarlas para seguir.
  - Cada acierto a la primera suma +1 de documentación.
  - Queda en la traza con origen `COMPRENSION`, que no cuenta en «cómo decidiste».
- **Minijuegos:**
  - la cabecera dice «PROYECTO · MÓDULO: X» y dónde estamos;
  - la receta empieza en «Dónde estamos en el proyecto», con el módulo del reto en mostaza;
  - en los diagramas, lo que hace cada caja se ve debajo de ella;
  - el modo guiado empieza situando el reto en el proyecto;
  - el cierre dice qué significa el resultado para el cliente.
- **Fase 2:**
  - la tarjeta «Las partes de …» muestra el avance por módulo (`AvanceDeModulos`: una lectura del avance global, sin simulación nueva);
  - el resumen del día dice qué se construyó («Lectura de matrículas 30 → 52 %»).
- **Apuntes:** empiezan por el expediente, con el diagrama incluido.

## 16 · Contenido

- **Expedientes** de N0 (registro de visitantes, recepción corporativa) y N1 (Cradle Lifts, gestión de instalaciones), con 7 y 9 palabras del oficio.
- **Prácticas de N0 reescritas** sobre el registro de visitas: formulario de visita, quién sigue dentro, aviso al anfitrión. Tienen la misma estructura y solución, así que el recorrido guiado y la calibración no cambian.
- **N1:** «informe mensual de uso de los patios».
- **Guía de N0:** cuatro burbujas nuevas (el sistema en una imagen, las palabras del oficio y las preguntas), y una en N1 sobre qué cambia.

**Pruebas:** `FichaDelProyectoTests` comprueba:
- que el expediente está completo y es coherente;
- que todo minijuego del nivel tiene un módulo que existe;
- que las prácticas ya no hablan de otro sistema;
- que las preguntas tienen una sola respuesta y son deterministas;
- que el avance por módulo es monótono y llega al 100 %;
- que el guiado empieza por el contexto.

**Resultado:** 586 de 587 (la que falla es la misma de antes).

---

# Ronda 5 · diagrama legible y scroll visible

## 17 · El diagrama del proyecto, ordenado

`DiagramaDeContexto` sustituye a la colocación anterior. Antes: módulos en rejilla, actores repartidos y flechas en diagonal con rótulos encima.

**Cómo se ordena ahora:**
- **Columnas de izquierda a derecha:** quien le pide algo al sistema, los módulos en el orden en que pasa el trabajo y quien recibe el resultado (también en cadena: aviso → correo → anfitrión).
- **Flechas en ángulo recto:** cada una tiene su propia entrada y salida en la caja y su carril en el hueco. Los carriles se ordenan para que no se crucen.
- **Saltos de columna:** si una flecha salta columnas, pasa por el pasillo libre entre cajas.
- **Rótulos:** cada flecha lleva un número, y debajo hay una leyenda («3 · Control de acceso → Barrera: abrir / no abrir»).
- **Recuadro del sistema:** abraza solo a los módulos.

**Comprobado fuera de Unity** con la geometría real de N0 y N1, renderizada a PNG: ninguna flecha atraviesa una caja ni se cruza con otra.

## 18 · Que se vea que se puede desplazar

Todas las áreas con scroll (`UiKit.Desplazable`, 23 usos) tienen ahora:
- **Barra de scroll visible**, que solo aparece cuando el contenido no cabe.
- **Aviso «▼ Hay más abajo» / «Hay más ►»** (`IndicadorDeScroll`):
  - late suave mientras quede contenido por ver;
  - al pulsarlo, avanza casi una pantalla;
  - se oculta al llegar al final;
  - en áreas de menos de 150 px solo sale la barra.

---

# Ronda 6 · la jornada, las pistas, el cliente, el producto y la Fase 3

Seis frentes de la prueba del 12 de octubre de 2026.

## 19 · Dos errores de la jornada

- **El reloj se quedaba parado tras una decisión.** La tarjeta de resultado (`_decidido`) solo se quitaba con «Volver a la jornada» y mantenía `Runner.EnEscena`. Ahora abrir cualquier panel (Equipo, Escritorio, Monitoreo, Tablero, Ceremonias, Mapa) la descarta. De paso: un panel tapado por otra pantalla ya no avisa de que «se cerró» (`PanelDelDia.AlReabrir`).
- **«Quedarme» cambiaba las barras a las 18:00.** No había doble aplicación: el día se simulaba al pulsar el botón y la prórroga era solo espera. Ahora `GameSession.Quedarse()` solo alarga el reloj, y `TerminarDia()` simula al acabar las horas extra (20:00 o «Irme ya»). El resumen del día muestra cada barra antes → después.
- **Recargar a las 18:00 saltaba el día.** `RuntimeState.JornadaSimulada` distingue «18:00 sin decidir» de «día ya cerrado».
- **El lanzamiento se repetía al recargar.** `NivelEnCurso` guarda el `LaunchResult`.

## 20 · Botones

- Receta del minijuego: un solo botón principal abajo a la derecha. En «Dónde estamos en el proyecto» es «Cómo se juega ►»; en la página siguiente, «¡A jugar!», con «◄ Dónde estamos» a su izquierda.
- Partidas: «◄ Cambiar de perfil» va en una fila de pie, abajo a la izquierda.

## 21 · Minijuegos: que se entienda cómo se juega

- **Backlog.** El modo guiado también empieza barajado (`OrdenarEvaluador.OrdenInicial`), enseña primero cómo se mueve una tarjeta y deja arrastrar la tarjeta señalada a su hueco, resaltado.
- **Diagramas.** Pistas de forma, siempre a la vista, que valen igual para defectos y señuelos: franjas de capa (`capa` en `elementos`), trazo de flecha según `tipo` (pide, lee, escribe), insignia «?» en la caja sin detalle, cada fila de la traza unida a su paso del diseño («= paso N» o «sin paso en el diseño»), fechas repetidas marcadas en el grafo de commits y leyenda fija.
- **«Pedir una pista (−15 s)».** Dos niveles: la pregunta que hay que hacerse y, después, la zona sospechosa.

## 22 · Fase 3: el despliegue

Código: `Core/Despliegue/` (`DespliegueDef`, `EvaluadorDeDespliegue`), `Core/Sesion/GameSession.Despliegue.cs`, `Unity/Pantallas/Despliegue/PantallaDeDespliegue.cs`. Contenido: bloque `despliegue` de cada nivel. Tests: `DespliegueTests`.

- **Cuándo.** Scrum: al cerrar cada sprint. Kanban: cada cinco días. Cascada: una sola vez, al final. Siempre el último día. Tras un despliegue intermedio se vuelve a la Fase 2; el último lleva al lanzamiento (Fase 4).
- **La tubería.** De «Código» a «Producción», con huecos donde se arrastran los tramos. Hay tramos trampa (subir directo, probar en producción, copiar a mano). Un hueco vacío no es un error: esa etapa no ocurre, y se cobra.
- **Enchufar el módulo.** Uno por módulo que sale. Sus cables van a las tomas de producción; hay tomas trampa (la base de pruebas, la clave a la vista). Un módulo que depende de otro que no sale no enciende.
- **Tiempo.** Una cuenta atrás para toda la fase (300 s). En cero se entrega lo que haya.
- **Resultado.** Tramo a tramo y módulo a módulo: qué estuvo bien, qué faltó y por qué.
- **Efecto.** Inmediato en satisfacción, deuda, moral y cansancio; una incidencia al día siguiente que cuesta avance; y dos acumuladores (`RiesgoDeDespliegue`, `DefectosDeDespliegue`) que leen por igual el pronóstico y el lanzamiento. Quien ya enseñó la mitad de los módulos antes del final llega con menos riesgo. Un despliegue no jugado es neutro, para no mover las partidas de calibración.

## 23 · La maqueta viva

`Core/Proyecto/Producto.cs`, `PantallaDelProducto`, `Pizarra/DibujosDelProducto.cs`. Cada módulo se dibuja en la etapa que le da su avance: boceto (menos de 25 %), mockup, prototipo (desde 50 %), MVP (desde 85 %) y en producción (desplegado). Se puede tocar: el prototipo responde y no guarda; en el MVP lo que se hace en un módulo aparece en los demás que ya funcionan. Se abre con «Ver el producto», en «Las partes del sistema». Al cerrar el día se anuncia lo que subió de etapa, y al cerrar un sprint o una etapa sale «La demo con el cliente», con lo que dice cada usuario de su parte (`VocesDelCliente`).

## 24 · Más negocio a la vista

- **La carpeta del cliente** (botón «Cliente» del riel, con contador de nuevos): papeles que llegan en días concretos, cada uno dibujado como lo que es (hoja de cuaderno, queja, correo, nota, hoja de datos) y con lo que le dice a quien construye. 5 en el nivel 0 y 8 en el 1 (`proyecto.papeles`).
- **Gente del cliente con quien hablar:** Rosa Villamil, la recepcionista (pasillo del nivel 0, desde el día 2), y Efraín Mosquera, el guardia de la garita (cafetería del nivel 1). Cuatro charlas cada uno, sobre lo que necesitan de verdad.

## 25 · Pruebas y lo que falta ver

670 de 670 fuera de Unity (antes 651). La capa de Unity compila. Los dibujos nuevos se revisaron con el visor fuera de Unity.

**Sin ver todavía en el editor:** la pantalla de despliegue (arrastre en dos ejes), la del producto, las pistas del diagrama y el arrastre del backlog guiado. La tubería no anima el agua: el resultado se cuenta tramo a tramo.

---

# Ronda 7 · duración de sprints, backlog propio, equipo, arquitectura y revisiones que salen bien

Nueve peticiones de la prueba del 12 de octubre de 2026 (segunda tanda). Todo lo nuevo es neutro si no se usa: los robots de calibración y las partidas ya equilibradas no cambian.

## 26 · El día no delata los avisos

Fuera el texto «Hoy va a llegar algún aviso» y el contador «Avisos · N de hoy»; el botón del riel solo cuenta lo que ya sonó y espera. «Esperar al siguiente aviso» y «Cerrar la jornada» se funden en **«Adelantar el reloj»** (`LevelRunner.AdelantarHastaQuePaseAlgo`): salta al siguiente aviso si lo hay y, si no, al cierre, sin decir cuál. Los anuncios de días futuros se mantienen.

## 27 · La carpeta del cliente, dibujada

«Quién es tu cliente» son tres cuadros con dibujo (quién es, qué le duele, cómo se trabaja ahí). «Lo que dicen hoy del producto» es una fila de fichas: la cara de cada usuario, la miniatura de su módulo tal como está hoy y una frase corta (`VozDelCliente.FraseCorta`, `Animo`). `DibujosDelProducto.Cliente` y `.Voces`.

## 28 · El equipo del proyecto

`GameSession.Equipo.cs`, panel «Equipo». Se ve siempre, con confianza cero incluida:

- Quién es cada uno (`descripcion` y `frase` en `equipo` de cada nivel), qué se le da mejor y peor, qué tarjeta lleva, cuáles terminó y su cansancio propio (`DesvioDeCansancio`, que estaba declarado y sin usar).
- Cuatro acciones, con su coste en minutos: **darle una tarjeta** (`AsignarTarjeta`), **hablar uno a uno** (15 min, una vez al día; dice cómo va y a quién le iría mejor su tarjeta), **respiro** (hoy no lleva tarjeta: baja su cansancio y el flujo del día) y **en pareja** (cobertura +2, deuda −1, sale algo menos de trabajo).
- Todo entra por el bono de flujo del día, que sigue acotado entre 0,80 y 1,15.
- El equipo se presenta también en «El proyecto» de la Fase 1.

## 29 · Cuánto dura cada sprint, etapa o revisión

`Core/Metodologia/OpcionesDeCalendario.cs`, `GameSession.AjustarCalendario`, paso «Metodología» de la Fase 1.

| | Opciones |
|---|---|
| Scrum | 4, 3, 2 o 1 sprint, según los días del nivel (nivel 1: de 5, de 7 o de 10 días; nivel 0: uno de 5 o dos de 3 y 2) |
| Kanban | Revisión del flujo y despliegue cada 3, 5 o 7 días (`Calendario.CadaCuantosDias`) |
| Cascada | Etapas equilibradas, más días para requisitos y diseño, o más para construir y probar |

Solo cambia el calendario: de él salen más o menos ceremonias, planificaciones y despliegues. La opción de serie deja todo como estaba. La elección viaja en el guardado (`NivelEnCurso.OpcionDeCalendario`).

## 30 · Product backlog y sprint backlog

- **Product backlog** (`GameSession.Backlog.cs`, paso «Backlog» de la Fase 1). Solo donde el proyecto empieza de cero (`proyecto.backlogInicial: "crear"`, nivel 0). Se eligen historias de entre las candidatas (`proyecto.historias`: pedidas, que nadie pidió, repetidas y tareas que no son historias) y se ordenan. Las tarjetas del tablero son esas historias en ese orden; los puntos de cada módulo se reparten entre ellas, así que la suma sigue siendo el alcance. Lo pedido que se dejó fuera entra igual, al final, como «Se descubrió tarde» y cuesta satisfacción; lo que nadie pidió sube el alcance. En el nivel 1 el paso enseña el backlog heredado y explica por qué ya existe.
- **Sprint backlog** (`ComprometerTarjetas`). Con dos o más sprints, la planificación deja de ser un número: se marcan las tarjetas que entran, con una barra de capacidad. Con uno solo queda como antes. `Comprometer(puntos)` se conserva para robots y pruebas.

## 31 · Pruebas que no se pueden saltar

Un depósito puede ser `critico` (con `porQueEsCritico`): un solo error escapado ahí pierde, por pocos que sean en total. Así las pruebas de extremo a extremo dejan de ser «lo primero a lo que se renuncia»: en `MJ-F2-08` y `MJ-OF-N1-PRUEBAS` cubren el camino que no puede fallar; en `MJ-OF-N0-PRUEBAS` siguen siendo opcionales, para que la respuesta no sea siempre la misma. El reparto prudente ya no cede lo crítico.

## 32 · Lo que se revisa no siempre está mal

- **Diagramas sin defectos** (`sinDefectos`): `MJ-F0-02` y `MJ-F1-10`. Se ganan con el botón «Está bien: lo apruebo», que está siempre para no delatar nada. Cualquier marca acusa a algo que estaba bien.
- **Reparto ya hecho** (`repartir.propuesta`): en `MJ-F2-09` Camila trae el plan de pruebas repartido y es correcto; lo bueno es entregarlo tal cual.
- **Documentos de Cascada** (`tablero/micro-doc.json`, `InspeccionarDocumento(doc, veredicto)`): se lee un extracto y se firma o se piden cambios. Hay extractos correctos y extractos con un problema para cada documento.
- **Decisiones del día**: `EV-OK-02` y `EV-OK-03` (nivel 1), donde lo correcto es dar por bueno el trabajo de un compañero después de mirarlo.

## 33 · La arquitectura, explicada y a la vista

- Glosario: «Arquitectura» pasa de 3 a 8 páginas, «Monolito modular» y «Microservicios» a 7 y «Parche sobre lo existente» a 5, con 17 escenas nuevas (`Escenas.Arquitectura.cs`).
- **El plano con los módulos del nivel** (`DibujosDeArquitectura.Plano`): un programa con paredes, servicios sueltos con su base, navegador y servidor, o lo nuevo pegado sobre lo viejo. Se abre desde cada opción en la Fase 1 («Ver cómo quedaría»), desde «Ver el producto» («Cómo está armado»), desde la columna del proyecto y desde el despliegue.
- La columna del proyecto dice qué arquitectura se eligió y qué le hace al día; el resumen del día lo recuerda cuando pesa.

## 34 · Pruebas y lo que falta

716 de 716 fuera de Unity (antes 670). Archivos nuevos: `EquipoDelProyectoTests`, `CalendarioElegidoTests`, `BacklogInicialTests`, `PruebasCriticasTests`, `RevisionesQueEstanBienTests`.

**Distinto de lo planeado:**
- El backlog se ordena con ▲▼, no arrastrando.
- La arquitectura no cambia la mecánica del despliegue (cuántos módulos se enchufan): lo explica en la entrada y en el plano.
- En el nivel 0 no hay decisión «está bien»: el tutorial tiene dos eventos fijos y una tercera los desplazaba.
- La receta de los repartos explica la pila crítica con texto, sin dibujo propio.

**Sin ver todavía en el editor:** todas las pantallas de esta ronda. Compilan y sus dibujos se revisaron con el visor fuera de Unity.
