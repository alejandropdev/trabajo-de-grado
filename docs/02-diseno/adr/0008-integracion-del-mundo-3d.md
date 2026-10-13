# ADR-0008: Integración del mundo 3D de la recolección en el proyecto del juego

- Estado: propuesta
- Fecha: 09/10/2026
- Decide: equipo de desarrollo

## Contexto

La recolección de la Fase 1 (el recorrido por el plano, zonas A–F) la construye otro subequipo en un
proyecto de Unity aparte: Unity 2022.3, pipeline de render integrado, entrada antigua (`UnityEngine.Input`)
y Cinemachine 2.10. El juego está en Unity 6, URP 17 y el Input System nuevo en exclusiva. El juego ya
definía el contrato entre los dos (`EntradaDeRecoleccion` → `ResultadoDeRecoleccion`,
`docs/03-calidad/juego/contrato-recoleccion-3d.md`) y sustituía el 3D con un simulador.

Lo entregado por el subequipo es, hoy, una colección de assets (2,4 GB), el controlador de tercera persona
de un paquete de terceros (NThirdPerson) y un nivel (`malfarie/Levels/Level1.unity`) con sus zonas y
recolectables colocados. Todavía no informa al juego de lo que se recoge. Hay que tenerlo
todo en un solo proyecto, sin perder nada de ninguno de los dos, y poder comprobarlo.

## Alternativas consideradas

- **Dos proyectos y dos ejecutables.** No hay sesión, guardado ni interfaz compartidos; el contrato tendría
  que viajar por archivos. Descartada.
- **Migrar el juego al proyecto 3D.** Obliga a bajar de Unity 6 a 2022 y a rehacer URP y la entrada del
  juego, que es la parte grande y la que tiene pruebas. Descartada.
- **Cargar la escena 3D sustituyendo a la del juego** (`LoadSceneMode.Single`). Funciona porque `AppRoot`
  sobrevive, pero al volver hay que recargar la escena del juego y destruir su `AppRoot` duplicado.
  Descartada por frágil.
- **Migrar a Cinemachine 3.** Cambia la API y obliga a rehacer las cámaras de la escena del subequipo.
  Descartada: 2.10 funciona en Unity 6.
- **Referenciar el 3D directamente desde las pantallas del juego.** Ata `Nexus.Unity` a código de terceros.
  Descartada a favor de una interfaz.

## Decisión

El 3D se copia tal cual dentro del proyecto del juego, en `Assets/Mundo3D/Externo/`, y se enchufa a la
Fase 1 por la interfaz `IModuloDeRecoleccion`, que implementa un ensamblado propio (`Nexus.Mundo3D`) cargando
la escena del recorrido de forma aditiva.

En concreto:

- **Carpetas.** `Assets/Mundo3D/Externo/` es lo recibido, con sus `.meta` (los GUID no cambian).
  `Assets/Mundo3D/Escenas/Recoleccion3D.unity` es la escena que usa el juego: una copia preparada de
  `Externo/malfarie/Levels/Level1.unity` (el nivel del subequipo), que no se toca. El código propio va en `Assets/Scripts/Mundo3D/`.
- **Ensamblados.** `Nexus.Mundo3D → Nexus.Unity → Nexus.Core`. El código de terceros tiene el suyo
  (`NThirdPerson`). `Nexus.Core` no cambia: el contrato ya estaba ahí.
- **Enlace.** `Nexus.Mundo3D` se registra en `AppRoot.ModuloDeRecoleccion` antes de que cargue la primera
  escena. Sin módulo registrado, la Fase 1 sigue ofreciendo el recorrido simulado.
- **Escena aditiva.** `AppRoot` (sesión, pila de pantallas, borrador de la Fase 1) no se descarga nunca.
  Mientras dura el recorrido se apagan la cámara y el `AudioListener` del juego y se quita el fondo del lienzo.
- **Entrada.** El código de NThirdPerson se porta al Input System nuevo conservando los `KeyCode` que ya
  están guardados en la escena (`EntradaNueva`). La cámara lee la acción `Player/Look`.
- **Render.** Los materiales del pipeline integrado se convierten a URP con una herramienta de editor
  limitada a `Assets/Mundo3D`.
- **Resultado provisional.** Como el 3D aún no recoge, «Dejar de recolectar» entrega el resultado del
  simulador en intensidad «normal» (`ResultadoProvisional`).

- **Rendimiento.** El nivel recibido no se puede jugar tal cual (unos 14 millones de triángulos). La copia
  que usa el juego se optimiza con herramientas repetibles: modelos reducidos con Blender (archivos nuevos en
  `Assets/Mundo3D/Optimizado/`), una caja de colisión por mueble y un nivel de calidad propio («Mundo 3D»,
  con su perfil de URP) que solo está activo durante el recorrido. Ver `tools/integracion-3d/README.md`.

## Consecuencias

- Se gana un solo proyecto y un solo ejecutable; el resto del juego no cambia, y el contrato sigue siendo
  la única frontera entre el 3D y el motor.
- El día que el 3D sepa qué zonas se pisaron y qué se recogió, solo cambia `ResultadoProvisional`.
- Se modifican tres archivos de código de terceros (`NThirdPersonPlayer.cs`, `NKeyEvent.cs`, `NPause.cs`):
  si el subequipo actualiza el paquete, hay que volver a aplicar esos cambios.
- Los materiales convertidos a URP ya no son byte a byte los recibidos; el original queda en la carpeta
  `3D/` (fuera del repositorio) y la diferencia la lista `tools/integracion-3d/inventario.py`.
- Las escenas de demostración de los packs (algunas de HDRP o del pipeline integrado) se conservan, pero
  no se garantiza que se vean bien en URP. La iluminación horneada de la escena puede necesitar rehornearse.
- El repositorio crece unos 2,4 GB, casi todo por Git LFS.
- El subequipo de 3D debe trabajar a partir de ahora dentro de este proyecto (Unity 6, URP, Input System).
