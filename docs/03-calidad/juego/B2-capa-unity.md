# B2 · La capa Unity

**Rama:** `feature/capa-unity`
**Qué incluye:** todo lo que hace falta para que el motor corra dentro de Unity. Es la aplicación
(`AppRoot`, `ScreenRouter`), el aspecto (`NexusTheme`, `UiKit`), el día continuo en tiempo real
(`LevelRunner`, `RelojView`), la escena y dos pantallas: la de error y la de diagnóstico.

> Esta fase no construye todavía las pantallas del juego (eso es C). Construye el sitio donde viven,
> y una pantalla de diagnóstico para comprobar en Play que todo el conducto funciona, del catálogo
> al reloj.

---

## Qué se hizo

### 1 · La aplicación

| Pieza                 | Qué hace                                                                                                                                                                                                                                                                                        |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`AppRoot`**         | El único objeto de la escena. Al arrancar **carga y valida todo el catálogo**, crea el lienzo (1920×1080, escalado), el `EventSystem` y el router, y abre la primera pantalla. Después es el dueño de lo que dura más que una pantalla: perfil activo, partida, sesión del motor y autoguardado |
| **`ScreenRouter`**    | La navegación, como una pila: `IrA<T>()` (vacía y abre), `Apilar<T>()` (encima; modal o no), `Volver()`, `VolverA<T>()` y `Repintar()`. Sustituye al `MenuController` y sus `SetActive`                                                                                                         |
| **`Pantalla`**        | La clase base. Una pantalla **no vive en ninguna escena ni prefab**: el router la crea en tiempo de ejecución, así que añadir una pantalla es añadir una clase                                                                                                                                  |
| **`RutasDeGuardado`** | `persistentDataPath/NexusProtocol/`, la raíz de la especificación §8.3.7. Las claves dentro las siguen decidiendo `ProfileStore` y `SaveStore`                                                                                                                                                  |
| **`PantallaDeError`** | INV-5 hecho pantalla. Si el contenido no valida, el juego **no arranca a medias**: lista cada error del validador, que nombra archivo y campo, y ofrece **Recargar** tras corregir el JSON                                                                                                      |

`AppRoot` ya sabe hacer el ciclo entero de una partida con las APIs del Core que existían:

- Seleccionar perfil, crear partida en el primer nivel y abrirla (empieza o rehidrata).
- Guardar con `AutoGuardado` en los cinco puntos del §5.7.
- Pasar al siguiente nivel al cerrar uno, y guardar al salir de la aplicación a mitad de día. INV-7 garantiza que recargar da lo mismo.

### 2 · El aspecto

| Pieza                               | Qué hace                                                                                                                                                                                                                                                                  |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`NexusTheme`** (ScriptableObject) | La paleta de la Biblia §11.4.1, «Búnker Corporativo v2», además de fuentes, tamaños de texto, **sprites** y medidas. Hoy los sprites están vacíos y se pintan rectángulos de color; **el día que arrastres un sprite a un campo, cambia en todas las pantallas a la vez** |
| **`UiKit`**                         | La fábrica de piezas: paneles, columnas, filas, listas con scroll, textos (5 estilos), botones (Primario, Secundario, Peligro y Fantasma), **barras, diales y radar**. Ninguna pantalla crea un `Image` a mano                                                            |
| **`GraficoRadar`**                  | El radar de competencias del Dashboard, dibujado con la malla de uGUI, sin texturas                                                                                                                                                                                       |

La regla de color más fácil de romper sin querer está escrita en el propio tema: **el mostaza es el
peligro y no pasa del 12 % del encuadre**. Solo lo usan el botón «Peligro», la hora en la última hora
antes del cierre y las alertas pendientes.

### 3 · El día continuo en tiempo real

| Pieza                            | Qué hace                                                                                                                                                                                                                              |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`RelojEnTiempoReal`** (Core)   | Convierte segundos de pantalla en minutos de juego y **acumula la fracción**: a 60 fotogramas por segundo, redondear cada fotograma dejaría el reloj parado. Velocidad de ×0,25 a ×4 para que el docente comprima una sesión, y pausa |
| **`LevelRunner`**                | Máquina de estados del día: `Corriendo → EnElCierre → (Irse · Quedarse → Prórroga) → DiaTerminado`, o bien `DesarrolloTerminado`. Avisa con eventos (alerta que suena o que expira, cierre, fin de día, fin del desarrollo)           |
| **`RelojView`**                  | El reloj en pantalla: día y unidad, la hora, la barra de la jornada, el estado y las alertas que esperan                                                                                                                              |
| **`ProgresionDeNiveles`** (Core) | Qué nivel viene después de otro                                                                                                                                                                                                       |

Tres formas de detener el reloj, y **no son lo mismo**:

- **`EnEscena`**: hay una decisión o un minijuego abiertos. La alerta ya se atendió, así que congelar el mundo aquí no regala nada.
- **`Pausado`**: el menú de pausa.
- **Nunca** mientras el jugador _decide si ir_ a atender una alerta. Si el mundo se parara, esa decisión no costaría nada (§M8).

### 4 · La escena

Menú **`Nexus > Crear escena principal`**. Crea el tema (`Assets/Settings/NexusTheme.asset`) si no
existe, y la escena `Assets/Scenes/Nexus.unity`, que solo tiene una cámara y un `AppRoot` con el tema
asignado. Además la pone **primera en Build Settings**. Todo lo demás lo crea `AppRoot` al arrancar,
así que la escena no hay que volver a tocarla. Con el mismo menú, `Nexus > Abrir la carpeta de
guardado` te enseña dónde están los perfiles y las partidas.

---

## Cómo probarlo

### En Unity

1. Abre el proyecto y espera a que compile.
2. Menú **`Nexus > Crear escena principal`**.
3. **Play.** Sale la **prueba del motor**: un nivel real (el N0) jugado de principio a fin, sin guardar
   nada. Tiene cuatro zonas:
   - **Arriba, la franja «Un día es así»**: `1 · Mañana → 2 · Jornada → 3 · Aviso → 4 · Decisión → 5 · Cierre → 6 · Resumen`.
     Se ilumina el momento en que estás.
   - **Izquierda**: el reloj, el ritmo (Pausa, ×1, ×2, ×4) y **el proyecto** (avance, deuda, moral y
     cobertura), que cambia con cada decisión.
   - **Centro, la tarjeta «Ahora»**: siempre dice qué está pasando y qué puedes hacer, con el botón para
     hacerlo.
   - **Derecha, «Lo que ha pasado»**: el diario, con lo más reciente arriba.
4. Recorre un día siguiendo la tarjeta central:
   - **«Empezar el día 1»** → la escena de la mañana (`TUT-0.1`, Marisol), línea a línea con
     «Siguiente». El reloj espera mientras dura.
   - **La jornada corre.** Si hoy va a llegar un aviso, puedes esperar o pulsar **«Adelantar hasta el
     aviso»**. El día 1 no hay ninguno (los eventos se anuncian con antelación), así que pulsa **«Cerrar
     la jornada»**.
   - **Cierre**: «Irme a casa» o «Quedarme». Si te quedas, vienen dos horas extra sin avisos, con
     «Terminar la jornada ya».
   - **Resumen del día**: cuánto cambió cada indicador, y **«Empezar el día 2»**.
   - El **día 2** llega el primer aviso (`EV-TUT-01`). **«Atender el aviso»** → la decisión, con lo que
     cambia cada opción debajo → **lo que significa** tu elección (correcta, aceptable o incorrecta, y
     por qué) → **«Seguir con la jornada»**.
   - El día 2 también trae la escena de **Marta**, que termina con una elección (aliarte o no).
5. Al acabar el día 5: **«Lanzar y cerrar el nivel»** → el resultado del lanzamiento, los umbrales,
   si la metodología era la adecuada y tus decisiones tal como las leerá el Dashboard.
6. **«Ver el tema»** (arriba a la derecha) abre el escaparate de `UiKit`: textos, botones, barras,
   diales, radar y paleta. El día de prueba se pausa mientras lo miras.

### Para ver fallar la pantalla de error

Cambia `"diasTotales": 5` por `"diasTotales": 1` en `niveles/nivel-00.json` y pulsa Play. Tiene que
salir _«El contenido del juego tiene errores»_ con el mensaje exacto del validador. Deshaz el cambio,
pulsa **«Recargar el contenido»** y vuelve el diagnóstico, sin salir de Play.

### Test Runner

`EditMode > Run All`, todo en verde. En mi arnés son **492**: las 482 de antes y 10 nuevas, del reloj
en tiempo real, la progresión de niveles y el guardado del perfil.

---

## Lo que tienes que saber

**1 · No puedo ejecutar Unity, así que monté dos arneses que compilan contra tu instalación real.**
Compilan el Core, la capa Unity y los scripts de editor contra las DLL de **Unity 6000.0.82f1** y las
de uGUI, TextMeshPro e Input System del propio proyecto. Todo compila sin un solo aviso de API
obsoleta. **Lo que un compilador no puede decirme es cómo se ve**: el _layout_, los colores y el ritmo
del reloj los tienes que mirar tú en Play, con la lista de arriba. La capa Unity no tiene tests
automáticos porque el ensamblado de tests es `noEngineReferences`. Por eso toda la lógica que se podía
separar (el reloj en tiempo real, la progresión) está en el Core, con sus pruebas.

**2 · Añadí dos referencias al `Nexus.Unity.asmdef`: `UnityEngine.UI` y `Unity.InputSystem`.** Saqué
los GUID de los `.meta` de los paquetes instalados, no de memoria:

- `UnityEngine.UI` ya se usaba (`PantallaPerfiles` usa `Button`), pero llegaba de forma implícita a través de TextMeshPro. Ahora está declarada.
- `Unity.InputSystem` hace falta porque el proyecto está en **modo «solo Input System nuevo»** (`activeInputHandler: 1`). Con ese modo, el `StandaloneInputModule` clásico lanza excepciones, y leer la tecla Escape exige su API. `AppRoot` crea el mismo `InputSystemUIInputModule` que ya usa tu escena `MenuInicial`.

**3 · Tres bugs que encontré revisando y que ningún compilador ve:**

- En uGUI, un panel con una columna _estirada_ dentro mide **0 de alto** dentro de una fila: el reloj habría sido invisible. `UiKit.PanelColumna` lo resuelve.
- En una columna sin ancho forzado, un texto mide lo que ocupa en **una sola línea**: los briefings y las rúbricas se habrían salido de la pantalla. Las columnas de `UiKit` fuerzan el ancho completo y las filas no.
- Una `Image` sin sprite **ignora el modo rellenado**, así que un dial sin sprite no se dibujaría nunca. `UiKit` genera un círculo en memoria cuando el tema no trae sprite.

**4 · La fuente.** La de TextMeshPro por defecto (LiberationSans) es estática, pero su fuente de
reserva es **dinámica**, así que «—», «…», las comillas curvas y «¿» se generan solos. Cambié un «●»
del reloj por «•», que seguro está en la fuente.

**5 · `ProfileStore.Guardar` pasa a ser público.** Era privado, y sin eso nadie podía guardar el
pre-test de la entrevista ni los coleccionables en el perfil. Tiene su prueba de ida y vuelta.

**6 · `MenuController` y `ProfileManager` siguen en el proyecto.** El plan decía retirarlos aquí,
pero la escena `MenuInicial` los usa: si los borrara ahora, esa escena quedaría con _scripts_ rotos.
Se retiran en C, cuando las pantallas nuevas de perfiles y menú la sustituyan.

**7 · Los datos de prueba antiguos no se pierden, pero no se ven.** La raíz de guardado pasa a ser
`persistentDataPath/NexusProtocol/`, como pide la especificación. Los perfiles que creaste con la
escena `MenuInicial` están en `persistentDataPath/perfiles/` y el juego nuevo no los lee. Si los
quieres, basta con moverlos dentro de `NexusProtocol/`.

---

## Los comandos de git

Primero abre Unity y ejecuta **`Nexus > Crear escena principal`**: genera la escena, el tema y los
`.meta` de las carpetas nuevas, y cambia Build Settings.

```bash
git checkout -b feature/capa-unity

git add "game/proyecto de grado/Assets/Scripts" \
        "game/proyecto de grado/Assets/Scenes/Nexus.unity" \
        "game/proyecto de grado/Assets/Scenes/Nexus.unity.meta" \
        "game/proyecto de grado/Assets/Settings" \
        "game/proyecto de grado/ProjectSettings/EditorBuildSettings.asset" \
        docs/03-calidad/juego/B2-capa-unity.md

git commit -m "feat(unity): la capa Unity — AppRoot, router de pantallas, tema, UiKit y el dia continuo en tiempo real"
```

Antes de commitear, un `git status` rápido. Si ves cambios en `ProjectSettings/` que no sean
`EditorBuildSettings.asset`, suele ser Unity actualizando algo por su cuenta: mejor dejarlos fuera de
este commit.
