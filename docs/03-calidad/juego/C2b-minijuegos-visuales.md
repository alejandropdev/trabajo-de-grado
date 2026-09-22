# C2b · Minijuegos visuales y recetas

**Rama:** `feature/minijuegos-visuales`
**Qué incluye:** los minijuegos rehechos siguiendo los mockups aprobados (`minijuegos-mockups/*.png`):
cada lienzo se dibuja como lo que es, y cada minijuego trae su **receta**, una pizarra con dibujos al
estilo de Overcooked que explica qué hay que hacer y qué significa cada opción.

---

## Qué se hizo

### 1 · Las piezas de dibujo (`Unity/Tema/Graficos.cs`)

| Pieza                      | Qué es                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`DibujoUI`**             | Dibujo vectorial sobre la malla de uGUI: líneas continuas y discontinuas, flechas, curvas, rectángulos redondeados, círculos, elipses, el cilindro de base de datos y los iconos de pizarra (bicho, persona, ✓, ✗, reloj, mano). Usa coordenadas como las de un SVG, así que los lienzos se escriben igual que el prototipo. Con `Tiza = true` cada trazo se dibuja dos veces con un temblor mínimo, que da el aspecto de tiza sin texturas |
| **`Lamina`**               | Un `DibujoUI` de fondo con textos, pastillas y zonas pulsables colocados en esas mismas coordenadas                                                                                                                                                                                                                                                                                                                                         |
| `UiKit.SpriteRedondeado()` | Un rectángulo redondeado «9-slice» generado en memoria, para pastillas y tarjetas                                                                                                                                                                                                                                                                                                                                                           |
| `UiKit.FuenteTiza`         | La letra de pizarra (**Gochi Hand**, licencia OFL, en `Resources/Fuentes/` con su licencia al lado), creada en tiempo de ejecución. Si faltara, se usa la fuente normal                                                                                                                                                                                                                                                                     |

Las dos gráficas propias llevan `RequireComponent(CanvasRenderer)`: es la lección del bug del radar.

### 2 · Los lienzos, como en los mockups

| Minijuego                                           | Qué se ve                                                                                                                                                                                                                                                                     |
| --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Grafo de commits**                                | Un carril por rama con su color y el nombre arriba, un punto por commit, aristas padre → hijo (curvas si cambian de rama) y el «!» de los huérfanos. Se pincha la fila entera, y su diff sale coloreado en «Detalle»                                                          |
| **Diagrama** (MJ-F0-01, MJ-F1-07 y los de práctica) | Cajas por fila y columna: el externo en discontinuo, el componente continuo y la base de datos como cilindro. Las flechas llevan su texto en una pastilla que se coloca sola donde no pisa nada. Se pueden pinchar las cajas **y** las flechas                                |
| **Secuencia**                                       | A la izquierda, el diagrama diseñado igual que antes (participantes, líneas de vida, mensajes numerados). A la derecha, **lo que pasó de verdad como tabla**: #, hora, quién, a quién y qué pasó, una fila por momento, cada una pulsable                                     |
| **Backlog**                                         | Tarjetas con franja, número, valor y esfuerzo. Se **arrastran** o se suben y bajan con ▲▼. A la izquierda, un medidor de esfuerzo que se llena con lo que entra; la línea discontinua de capacidad separa lo que entra de lo que no                                           |
| **Horas de pruebas**                                | **Una pila por tipo, en fila, del mismo alto**: el alto de una pila es todo el presupuesto, así que una pila llena significa todas las horas en ese tipo. Se llenan de un solo azul muy claro, de dos en dos horas con −2 / +2. Lo sin repartir es su propia pila, en mostaza |

Estados, iguales en todos:

- **Seleccionado:** cian claro.
- **Marcado:** mostaza, con la chapita «!».
- **Ayuda del andamiaje 3:** las zonas candidatas, teñidas de mostaza.

### 3 · El cierre enseña la solución sobre el dibujo

Al entregar, el lienzo se vuelve a pintar a la izquierda, con la explicación a la derecha:

- **Detectar:** ✓ en cian lo que encontraste, ✗ en rojo lo que se te escapó y en gris los señuelos, que estaban bien.
- **Backlog:** tu orden, y **cada dependencia rota como una flecha roja** («necesita esta antes»).
- **Horas de pruebas:** los errores atrapados dentro de cada pila, y los que se escaparon cruzando hasta la columna del **cliente**.

### 4 · Las recetas (`PantallaDeReceta`)

Una pizarra con marco de madera, letra de tiza y la nota pegada en la esquina.

- **Sale sola al empezar** cada minijuego, con «¡NUEVO RETO!», quién espera y por qué, y el botón «¡A jugar!», que arranca el reloj.
- El botón **«Receta»** de la cabecera la abre en cualquier momento, con «Volver al reto». **Mientras está abierta, el reloj del reto no corre.**
- Contenido:
  - **Marcar** (grafo, diagrama, secuencia): «Cómo se juega» en tres dibujos (pincha lo raro → dile qué le pasa, con las etiquetas reales del reto → entrega a tiempo) y «lo que está bien no se marca». Además, **una viñeta por cada etiqueta de la paleta**, trampas incluidas, con un dibujo y una frase corta. **Los ejemplos son de otra tienda** (Pedidos, Tienda, Pagos, cajas A y B): enseñan qué significa cada etiqueta sin decir dónde está el error del reto.
  - **Backlog:** arrastrar, la línea, «hay cosas que van antes», contestar al cliente, y qué significa cada respuesta.
  - **Horas de pruebas:** dar horas, cada pila atrapa solo lo suyo, lo que se escapa llega al cliente, y el truco dibujado (todo en una pila frente a repartir según el riesgo).
- La tabla de «qué es» de cada etiqueta y sus dibujos está en `Recetas` (en `PantallaDeReceta.cs`). **Una etiqueta nueva en un JSON sale con su nombre aunque no tenga dibujo**, y para que tenga dibujo se añade una entrada ahí.

La práctica de oficina usa las mismas pantallas y la misma receta.

---

## Cómo probarlo

1. Abre Unity y deja que importe. La fuente `Resources/Fuentes/GochiHand-Regular.ttf` se importa sola como Font.
2. Juega el N0 hasta el reto del diagrama (día 3 o después), o haz la tarea de oficina «Revisar un diagrama» desde el primer día:
   - Sale la **receta** con «¡NUEVO RETO!». Comprueba que el reloj del reto no baja mientras está abierta. Pulsa «¡A jugar!».
   - Pincha una **caja** y una **flecha** (su pastilla): se ponen en cian claro. Márcalas con una etiqueta y pasan a mostaza con «!».
   - Pulsa **«Receta»** a mitad de partida y vuelve.
   - **Entrega:** el diagrama vuelve a salir con ✓, ✗ y los señuelos en gris.
3. En el N1:
   - **Backlog (MJ-F1-08):** arrastra una tarjeta hacia arriba y mira cómo se mueven la línea de capacidad y el medidor. Pon una tarjeta encima de la que necesita y entrega: sale la flecha roja.
   - **Horas de pruebas (MJ-F2-08):** llena una pila con todas las horas y mira que queda llena hasta arriba. Entrega y mira los errores que cruzan hasta el cliente.
   - **Secuencia (MJ-F2-07)** y **grafo (MJ-F2-02):** salen desde el día 6 o 7. Para verlos antes, juega varias partidas o usa una semilla de modo aula.

**Test Runner:** siguen siendo **522 en verde**. C2b solo toca la capa Unity; el Core y el contenido no cambian.

---

## Lo que tienes que saber

**1 · Compila contra tu Unity 6000.0.82f1, pero esta vez es muy visual y no lo he visto en Play.** El
riesgo está en los detalles de posición: textos que no caben, una pastilla que se sale. Todo está en
coordenadas fijas copiadas del mockup (1920×1080), así que corregir es cambiar un número. Mándame capturas
de lo que no quede fiel y lo ajusto.

**2 · El lienzo tiene tamaño fijo y va dentro de un scroll.** Si un reto tuviera muchas más piezas que los de
ahora, se desplaza en vez de apretarse.

**3 · El temblor de tiza es sencillo:** un segundo trazo desplazado medio píxel. Si quieres tiza «de verdad»
(con grano), el siguiente paso es una textura de tiza en el material, y se aplica a todas las recetas a la vez.

**4 · Las pantallas viejas del minijuego con prefabs** (`Unity/Minijuegos/PantallaDetectarView.cs` y
compañía) ya no las usa nada. No las borré: es tu decisión, como con la escena antigua del menú.

---

## Los comandos de git

Si todavía no commiteaste **C2a**, hazlo primero con los comandos de
[C2a-tutorial-y-correcciones.md](C2a-tutorial-y-correcciones.md). La carpeta `minijuegos-mockups/` la
pusiste en `.gitignore`, así que no entra en ninguno de los dos commits.

Después, con Unity abierto para que genere los `.meta` (de `Resources/`, de `Graficos.cs` y de
`PantallaDeReceta.cs`):

```bash
git checkout -b feature/minijuegos-visuales

git add "game/proyecto de grado/Assets/Scripts/Unity" \
        "game/proyecto de grado/Assets/Resources" \
        "game/proyecto de grado/Assets/Resources.meta" \
\
        docs/03-calidad/juego/C2b-minijuegos-visuales.md

git status

git commit -m "feat(minijuegos): lienzos visuales (grafo, diagrama, secuencia con tabla, backlog arrastrable, pilas de pruebas) y recetas en pizarra"
```
