# Guía de contribución

## Antes de empezar a trabajar

1. Actualizar `develop`: `git checkout develop && git pull`.
2. Crear la rama: `git checkout -b feature/<modulo>-<descripcion>`.
3. Verificar que la tarea existe en el tablero y está asignada.

## Reglas que evitan conflictos en Unity

Los conflictos de merge en archivos de Unity son el riesgo R-08 del plan de proyecto.
Estas reglas lo mitigan:

1. **Una escena, una persona.** Avisar en el canal del equipo antes de editar una escena
   o un prefab compartido. No trabajar dos personas a la vez sobre el mismo `.unity`.
2. **Preferir prefabs a escenas.** Construir la interfaz en prefabs y componerlos en la
   escena; los conflictos sobre prefabs pequeños se resuelven, los de escenas grandes no.
3. **Nunca borrar ni renombrar un `.meta` a mano.** Hacer los movimientos de archivos
   desde el propio editor de Unity.
4. **Modo de serialización en texto forzado.** Debe estar activo en Project Settings →
   Editor → Asset Serialization → Force Text.
5. **Contenido en ScriptableObjects, no en código.** Un evento o un ajuste de balance
   nuevos son un asset nuevo, no una modificación de un archivo compartido.

## Antes de abrir un pull request

- [ ] El proyecto compila sin advertencias nuevas.
- [ ] Las pruebas de EditMode pasan (Window → General → Test Runner).
- [ ] Si se tocó el motor de simulación, hay una prueba que cubre el cambio.
- [ ] Si se tocó una mecánica, el GDD y el documento de arquitectura y mecánicas quedaron
      actualizados en la misma rama.
- [ ] No se subieron binarios fuera de Git LFS ni carpetas `Library/` o `Temp/`.
- [ ] Los archivos `.meta` de todo asset nuevo están incluidos en el commit.

## Revisión

Cada pull request requiere la aprobación de al menos un integrante distinto del autor.
El revisor verifica la lista anterior y la coherencia con la arquitectura documentada en
`docs/02-diseno/`.
