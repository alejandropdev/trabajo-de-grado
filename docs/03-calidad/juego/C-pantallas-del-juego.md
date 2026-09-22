# C · Las pantallas del juego

**Rama:** `feature/pantallas-del-juego`
**Qué incluye:** todas las pantallas para jugar el N0 y el N1 de principio a fin con el ratón, sin
salir de Play. Son los perfiles, el menú de partidas, el prólogo con la entrevista, la Fase 1, el día
continuo con su mapa, los tres tipos de minijuego, el lanzamiento, el Dashboard de Lecciones y el
Diario de Campo. También incluye las piezas de motor y de contenido que esas pantallas necesitaban:
los verbos V2 y V3, los cinco minijuegos prioritarios y los coleccionables que se recogen de verdad.

> Con esta fase, **Play ya es el juego**: la prueba del motor de B2 sigue disponible desde el menú de
> perfiles, pero ya no es la primera pantalla.

---

## Qué se hizo

### 1 · El recorrido completo

```
Perfiles → Partidas → [nueva partida]
  N0 · antes del nivel   CIN-0.0 → ENT-0 (¿de dónde vienes?) → las 15 preguntas → ENT-0.FIN → CIN-0.1
  N0 · Fase 1            GUIA-0.F1 → encargo → metodología + motivo → fichas de calidad → arquitectura + motivo
  N0 · Fase 2            5 días continuos (TUT-0.1 … TUT-0.5, EV-TUT-01/02, MJ-F0-01)
  N0 · Fase 3            lanzamiento → Cerrar() → Dashboard de Lecciones → CIN-0.2 «El Aplauso»
  N1 · apertura          CIN-1.0 (mirar la pared o mirarle a él)
  N1 · Fase 1            CIN-1.1 (llega Voss) → las tres decisiones → CIN-1.1 «tras-<arquitectura>»
  N1 · Fase 2            20 días (CIN-1.2, INT-1, los 10 eventos, 5 minijuegos, retro y planning)
  N1 · Fase 3            CIN-1.3 → lanzamiento → CIN-1.4 «El Primer Glitch» → Cerrar() → Dashboard
  → «Fin de la versión jugable»
```

Ninguna pantalla sabe cuál va después: al terminar, le devuelve el control a **`FlujoDelNivel`**, que
es el único sitio donde está escrito el recorrido. Los guiones se buscan **por nivel y momento**
(`antesDelNivel`, `apertura`, `fase1`, `lanzamiento`, `cierre`, `despuesDelNivel`), así que un nivel
nuevo trae sus escenas sin tocar código. La única pieza fija es la entrevista: va después del guion
`ENT-0`.

### 2 · Las pantallas

| Pantalla                                                                                    | Qué hace                                                                                                                                                                                                                                                                                                               |
| ------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`PantallaDePerfiles`**                                                                    | Quién juega. Crear, elegir o borrar un perfil (se borran también sus partidas). Enseña cuántas partidas tiene, cuánto hay en su diario y si ya hizo la entrevista                                                                                                                                                      |
| **`PantallaDePartidas`**                                                                    | El menú del perfil. Partida nueva, con **modo aula** opcional (semilla fija: toda la clase juega la misma partida), y la lista de partidas guardadas para seguir o borrar. Las dañadas salen en rojo con su error, sin tumbar la lista                                                                                 |
| **`PantallaDeGuion`**                                                                       | Todas las escenas. Las líneas salen una a una (clic, Espacio o Enter) y las anteriores se quedan arriba, tenues. Tres voces: narrador (cursiva, sin nombre), personajes (con nombre) y `log` (monoespaciada). Si el guion acaba en elección, **no se puede seguir sin elegir**, y la elección se registra en la sesión |
| **`PantallaDeEntrevista`**                                                                  | Las 15 preguntas, más «No lo sé». Tras cada respuesta, **el susurro de HH a Marisol**, que es la única retroalimentación. Al terminar guarda el **pre-test en el perfil**, solo la primera vez                                                                                                                         |
| **`PantallaDeFase1`**                                                                       | Cuatro pasos: el encargo, la metodología y su motivo, el reparto de fichas y la arquitectura con su motivo. Los motivos de metodología salen **mezclados de todas las del nivel**; si solo salieran los de la elegida, la lista ya delataría cuál va con cuál. Ningún veredicto se enseña aquí                         |
| **`PantallaDelDia`**                                                                        | El día continuo, en tres columnas (ver abajo)                                                                                                                                                                                                                                                                          |
| **`PantallaDetectar`** (V1)                                                                 | Commits, diagramas de componentes y secuencias contra trazas: la misma pantalla para los tres lienzos                                                                                                                                                                                                                  |
| **`PantallaOrdenar`** (V3)                                                                  | El backlog: ▲▼, la línea de capacidad y la respuesta a Facilities                                                                                                                                                                                                                                                      |
| **`PantallaRepartir`** (V2)                                                                 | Las horas de pruebas entre cuatro tipos                                                                                                                                                                                                                                                                                |
| **`PantallaDeLanzamiento`**                                                                 | Lo que llegó al cliente: alcance entregado, defectos escapados y el riesgo con el que se lanzó                                                                                                                                                                                                                         |
| **`PantallaDeLecciones`**                                                                   | El Dashboard, con sus siete secciones (ver abajo)                                                                                                                                                                                                                                                                      |
| **`PantallaDelDiario`**                                                                     | El Diario de Campo, con cinco pestañas: cartas, pendrive, códigos, startups y advertencias. Lo no encontrado sale como hueco «???»                                                                                                                                                                                     |
| `PantallaDeColeccionable`, `PantallaDePausa`, `PantallaDeConfirmacion`, `PantallaDeMensaje` | Diálogos: lo que acabas de encontrar, la pausa (seguir, diario, guardar y salir), el «¿seguro?» antes de borrar y el final de la versión                                                                                                                                                                               |

### 3 · El día continuo

- **Izquierda:** el reloj y **el mapa**. Cada zona dice cuánto cuesta ir, si está cerrada y cuál es tu escritorio. Debajo, **«Aquí»**: la descripción de la zona, quién está, qué ofrece y, si hay algo, «Hay algo aquí: una carta de desarrollador» con el botón para mirarlo.
- **Centro, «Ahora»:** siempre responde a _¿qué pasa y qué hago?_ Muestra, según el momento, la retrospectiva, la planificación (comprometerse con − / +), la jornada con los anuncios del día, los avisos con su cuenta atrás, la decisión, lo que cambió al decidir, el cierre, las horas extra, el resumen del día o el paso al lanzamiento.
- **Derecha:** el proyecto (avance, deuda, moral, cobertura y cansancio), el riesgo latente del día y el diario del día, con lo más reciente arriba.

Las reglas del §3.3 y del §M8, aplicadas en la pantalla:

- **El reloj se para dentro de una escena** (decisión, minijuego, planificación, retro, una carta que lees) y en la pausa. **Nunca mientras decides si ir a atender un aviso.**
- **Los avisos solo se atienden desde tu escritorio.** Si estás lejos, el botón dice «Volver al escritorio y atender (N min)». El viaje cuesta esos minutos, y **el aviso puede caducar por el camino**.
- **Las decisiones no enseñan su veredicto durante el nivel.** Se ve lo que cambió en el momento, y el porqué se lee en el Dashboard. Así se decide por el mundo y no por la nota.
- Autoguardado al cerrar la Fase 1, **al final de cada día**, tras el lanzamiento, al cerrar el nivel y al salir desde la pausa.

### 4 · El Dashboard de Lecciones

1 · el resultado y los umbrales · 2 · **el riesgo latente y sus cuatro barras** (cansancio, deuda,
cobertura, documentación) · 3 · **el radar de competencias** por objetivo de aprendizaje · 4 · la
metodología: si era la adecuada, **si tu motivo era bueno, malo o una trampa** y sus prácticas contra
lo que pasó · 5 · la arquitectura con su veredicto y su porqué · 6 · **cada decisión** (Fase 1,
eventos, minijuegos, retro) con su veredicto, su razón y la nota de la metodología · 7 · **qué trajo
qué** (la cadena causal) y las escenas que no llegaste a ver.

### 5 · Lo que las pantallas necesitaban del motor y del contenido

| Pieza                                 | Qué es                                                                                                                                                                                                                                                                                   |
| ------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **V3 · `OrdenarEvaluador`**           | El backlog. Entran las tarjetas que caben de arriba abajo. Romper una dependencia es `falsoPositivo`; capturar al menos el 70 % del **mejor valor posible** (calculado por fuerza bruta, exacto) es `todos`, y menos, `parcial`. La respuesta al cliente suma sus propias consecuencias  |
| **V2 · `RepartirEvaluador`**          | Las horas. Cada tipo solo encuentra los defectos de su clase. Sobrecomprar un tipo mientras otro con defectos se queda a cero es `falsoPositivo`                                                                                                                                         |
| **V1 generalizado**                   | `Detectar` ya no es solo de commits: marca también **elementos y conexiones** de un diagrama                                                                                                                                                                                             |
| **5 minijuegos nuevos**               | `MJ-F0-01` (el diagrama del N0), `MJ-F1-07` (el diagrama de componentes), `MJ-F1-08` (el backlog), `MJ-F2-07` (la secuencia contra la traza) y `MJ-F2-08` (las horas de pruebas). Con `MJ-F2-02` (el grafo de commits) son los seis del índice, cada uno acotado a su nivel y a sus días |
| **Cadenas desde minijuegos**          | Fallar o dejar caducar la revisión del diagrama trae **EV-ALC-01** a los tres días. Negociar el backlog trae **EV-BUE-02** a los cuatro                                                                                                                                                  |
| **El omitido de cada escena**         | Si una alerta de minijuego caduca, se aplica **el `omitido` de su propia escena**, no uno genérico. Antes, no mirar el diagrama salía gratis                                                                                                                                             |
| **Coleccionables que se recogen**     | `ColeccionablesAqui()` y `RecogerColeccionable()`. Solo se recoge en su zona y una vez. Al cerrar el nivel se suman `FLG_CARTAS` y `FLG_CODIGOS`, y el perfil los guarda en el momento                                                                                                   |
| **La arquitectura deja flag**         | `flagsAlCerrar` en cada arquitectura: el monolito modular suma `FLG_VOSS_AFINIDAD`, y los microservicios se la restan                                                                                                                                                                    |
| **Días tranquilos**                   | `probabilidadDeDiaTranquiloMinijuegos` en el director del nivel (0,45 en N1 y 0 en N0, para que el tutorial enseñe su minijuego siempre)                                                                                                                                                 |
| **Textos de los motivos**             | `textosDeRazones` en cada metodología, para que la Fase 1 no enseñe ids                                                                                                                                                                                                                  |
| `GameSession.ZonaAbierta()`           | Para que el mapa pinte las puertas cerradas sin probar a entrar                                                                                                                                                                                                                          |
| `PlayerProfile.preTestHecho`          | El pre-test es **uno**, el de la primera vez                                                                                                                                                                                                                                             |
| `LevelRunner.Atender()` / `IrAZona()` | Atender y viajar también mueven el reloj, y en ese rato puede sonar o caducar otra alerta. Pasan por el runner para que la pantalla se entere                                                                                                                                            |

---

## Cómo probarlo

### En Unity

1. Abre el proyecto, espera a que compile y abre `Assets/Scenes/Nexus.unity`. Si no existe, usa **`Nexus > Crear escena principal`**.
2. **Play.** Sale **«¿Quién juega?»**. Escribe un nombre y pulsa **«Crear perfil»**.
3. En **«Hola, …»**, deja el nombre que propone y pulsa **«Empezar»**. El modo aula se puede probar después: con la misma semilla, dos partidas tienen que dar los mismos avisos el mismo día y a la misma hora.
4. **El prólogo del N0.** La sala del concurso, la entrevista con Marisol y la pregunta _¿de dónde viene usted?_. Elige una: decide cómo suena `INT-1` en el N1. Después vienen las 15 preguntas. Tras cada respuesta, **fíjate en el susurro**, que cambia según aciertes o falles. Al final, `ENT-0.FIN` y `CIN-0.1`.
5. **La Fase 1 del N0.** Marisol explica las tres decisiones. Lee el encargo, elige metodología **y motivo** (el botón no se activa sin los dos), reparte las 4 fichas (con una a cero sale en mostaza lo que va a fallar) y elige arquitectura con su motivo.
6. **El día 1.** La escena de la mañana (`TUT-0.1`) sale sola. Después:
   - Pulsa **Pasillo** en el mapa. El reloj avanza lo que cuesta el viaje. En «Aquí» sale lo que hay: **mira la advertencia** y guárdala en el diario.
   - Vuelve a **La sala del concurso**. Si hoy no queda nada, **«Cerrar la jornada»** → **«Irme a casa»** → el resumen.
7. **El día 2.** Llega el primer aviso (`EV-TUT-01`). Prueba **alejarte antes de atenderlo**: el botón pasa a «Volver al escritorio y atender (N min)». Decide, y verás **lo que cambió, pero no si estuvo bien**. También sale Marta (`TUT-0.2`) con su elección.
8. **El día 3 o después**, el ticket del **minijuego del diagrama** (`MJ-F0-01`). Pulsa «Empezar», pincha una pieza, márcala con una etiqueta y pulsa «Entregar». El cierre dice qué encontraste y qué no.
9. Al terminar el día 5: **«Ir al lanzamiento»** → el resultado → **«Cerrar el nivel»** → **el Dashboard**. Comprueba las siete secciones: tus decisiones con su veredicto y su porqué, y el motivo de la metodología juzgado.
10. **«Continuar»** → `CIN-0.2` «El Aplauso» → empieza el **N1**: `CIN-1.0`, la llegada de Voss y la Fase 1. Al confirmar la arquitectura, **Voss reacciona a la que elegiste**.
11. En el N1, con Scrum, el día 1 sale **la planificación**: pon más puntos de los que sugiere y mira cómo sube la deuda los días siguientes. A lo largo de los 20 días salen los otros cuatro minijuegos:
    - **El backlog** (V3): ordena con ▲▼ y contesta a Facilities. Si **negocias**, unos días después llega _«El cliente que entiende»_.
    - **El diagrama de componentes** (V1): si lo fallas **o lo dejas caducar**, a los tres días llega _«Ah, y también…»_.
    - **La secuencia contra la traza** (V1): dos columnas, lo diseñado y lo que pasó.
    - **Las horas de pruebas** (V2): reparte 40 horas.
12. **Menú → Guardar y volver al menú** a mitad de un día, y después **«Continuar»** esa partida: tiene que seguir **en el mismo día y a la misma hora** (INV-7).
13. **Diario** (arriba en el día, o desde el menú del perfil): las cinco pestañas con sus contadores. Lo encontrado sigue ahí en otra partida del mismo perfil.
14. Al acabar el N1 y su Dashboard sale **«Fin de la versión jugable»**.

Para ir más rápido: **×4** arriba, y en la jornada **«Esperar al siguiente aviso»**, que salta el reloj
hasta el siguiente aviso de hoy. No te ahorra ninguna consecuencia: el aviso suena igual y su ventana
de atención empieza igual.

### Test Runner

`EditMode > Run All`, todo en verde. En mi arnés son **511**: las 492 de B2 y 19 nuevas. Son las de
los verbos V2 y V3, la validación de escenas, los coleccionables, el flag de la arquitectura, los
minijuegos acotados por nivel, la probabilidad de día tranquilo y la puerta del mapa. La más
importante es la que ha cambiado: **`ContenidoRealTests` ya no se inventa el resultado de los
minijuegos**. El robot los juega con su escena y su evaluador reales (marca piezas al azar, ordena el
backlog y contesta, reparte las horas) y comprueba las cadenas **con su causa**. En 180 partidas de N1
jugó 855 minijuegos. Falló el diagrama 120 veces, y las 120 trajeron EV-ALC-01. Negoció 56 veces, y
las 56 trajeron EV-BUE-02. **Ninguna de las dos cadenas apareció nunca sin su causa.** Además, dejar
caducar el diagrama también trae EV-ALC-01.

---

## Lo que tienes que saber

**1 · Lo de siempre: compila contra tu Unity, pero no lo he visto.** Core, capa Unity y editor
compilan contra las DLL de 6000.0.82f1, uGUI, TextMeshPro e Input System sin un solo error. Lo que un
compilador no ve es el _layout_: si algo se monta o se sale, dímelo con una captura y lo ajusto.

**2 · Un bug de contenido que salió al jugar los minijuegos de verdad.** El `falsoPositivo` de
`MJ-F2-02` (el grafo de commits) encadenaba **`EV-EQ-07`, que no existe**. No había dado la cara porque
el robot fingía el resultado. El día que un jugador acusara a alguien sin pruebas, la partida habría
**lanzado una excepción**. Lo cambié por un coste diferido de moral (−3 a los tres días), que dice lo
mismo, y **añadí al validador la comprobación que faltaba**: toda cadena que salga de una escena de
minijuego tiene que apuntar a un evento del catálogo. Si algún día existe `EV-EQ-07`, se puede volver
a encadenar.

**3 · Otro bug, este del robot y no del juego.** Atender una alerta también mueve el reloj, y en ese
rato puede sonar otra. El robot no miraba lo que sonaba en ese tramo, así que algunos eventos
«caducaban» solos en las pruebas. El `LevelRunner` ya lo hacía bien para la pantalla. El robot ahora
procesa todos los tramos.

**4 · La Fase 1 es la versión simulada.** El plan decía «zonas A–F como lista con reloj de dos
horas», pero el contenido no tiene esas zonas. Hice las cuatro decisiones como pasos en orden, que es
lo que el recorrido del plano desbloquea. Si quieres el recorrido, hay que escribir primero las seis
zonas.

**5 · El pre-test se guarda solo la primera vez.** Una segunda partida del mismo perfil repite la
entrevista (es parte de la historia), pero ya no sobrescribe el resultado. `PlayerProfile` tiene un
campo nuevo, `preTestHecho`, que en los perfiles antiguos se lee como `false`.

**6 · Los motivos de la metodología salen mezclados de todas las metodologías del nivel.** Con los de
una sola, la lista le diría al jugador cuál corresponde a cuál.

**7 · Guardar a mitad de día sí funciona, pero no a mitad de decisión.** «Guardar y volver al menú»
guarda el día tal cual (hora, zona y alertas vivas). Una decisión o un minijuego abiertos no se
guardan abiertos: el autoguardado nunca cae dentro de una escena, y la pausa tampoco se abre desde
ella.

**8 · `MenuController`, `ProfileManager`, `PantallaPerfiles` y `FilaPerfilUI` siguen en el
proyecto.** Ya no los usa nada de la escena `Nexus`, pero la escena antigua `MenuInicial` sí. No los
borré, porque borrar es cosa tuya. Si quieres retirarlos, están los comandos al final, en un commit
aparte.

---

## Los comandos de git

Primero **abre Unity** y deja que importe: tiene que generar los `.meta` de la carpeta nueva
`Scripts/Unity/Pantallas/` (y de `Minijuegos/` dentro), de `Core/Minijuegos/ordenar/` y `repartir/`,
del test nuevo y de los JSON nuevos de `minijuegos/`. Sin los `.meta`, quien clone el repositorio
verá referencias perdidas.

```bash
git checkout -b feature/pantallas-del-juego

git add "game/proyecto de grado/Assets/Scripts" \
        "game/proyecto de grado/Assets/StreamingAssets" \
        docs/03-calidad/juego/C-pantallas-del-juego.md

git status
```

En el `git status`, comprueba que entran los `.meta` nuevos (`Pantallas.meta`, `ordenar.meta`,
`repartir.meta`, `MJ-F1-07.json.meta`…). Después:

```bash
git commit -m "feat(juego): N0 y N1 jugables de principio a fin — perfiles, prologo, Fase 1, dia continuo, minijuegos V1/V2/V3, lanzamiento, Dashboard de Lecciones y Diario de Campo"
```

**Opcional, en un commit aparte**, para retirar la escena antigua del menú y sus scripts. Antes,
quita `MenuInicial` de la lista de `File > Build Settings`:

```bash
git rm -r "game/proyecto de grado/Assets/Scenes/Menu" "game/proyecto de grado/Assets/Scenes/Menu.meta" \
          "game/proyecto de grado/Assets/Scripts/Unity/Menu_Inicial" "game/proyecto de grado/Assets/Scripts/Unity/Menu_Inicial.meta" \
          "game/proyecto de grado/Assets/Scripts/Unity/ProfileManager.cs" "game/proyecto de grado/Assets/Scripts/Unity/ProfileManager.cs.meta" \
          "game/proyecto de grado/Assets/Scripts/Unity/PantallaPerfiles.cs" "game/proyecto de grado/Assets/Scripts/Unity/PantallaPerfiles.cs.meta" \
          "game/proyecto de grado/Assets/Scripts/Unity/FilaPerfilUI.cs" "game/proyecto de grado/Assets/Scripts/Unity/FilaPerfilUI.cs.meta"
git add "game/proyecto de grado/ProjectSettings/EditorBuildSettings.asset"
git commit -m "chore(unity): retirar la escena MenuInicial y sus scripts, sustituidos por las pantallas nuevas"
```

Hazlo solo si no tienes nada tuyo en esa escena: después de este commit deja de existir.
