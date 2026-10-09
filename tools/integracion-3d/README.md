# Integración del proyecto 3D

Scripts con los que se trajo el proyecto 3D de la recolección (carpeta `3D/`, fuera del repositorio) al
proyecto del juego, y con los que se comprueba que no se perdió nada. La decisión está en
`docs/02-diseno/adr/0008-integracion-del-mundo-3d.md`.

Todas las órdenes se lanzan desde la raíz del repositorio. `python -I` evita que Python cargue código de la
carpeta que examina.

| Archivo | Qué es |
|---|---|
| `inventario.py` | Lista cada archivo con su SHA-256 y su GUID; compara una carpeta contra esa lista |
| `referencias.py` | Busca GUID referenciados en escenas, prefabs y materiales que ningún `.meta` define |
| `copiar.py` | Copia `Assets/` del 3D al juego sin duplicar GUID |
| `manifiesto-origen.csv` | El inventario del 3D tal como se recibió |
| `referencias-origen.txt` | Las referencias que ya venían rotas en el 3D (la base de comparación) |
| `excepciones.txt` | Lo único que no se copió, y por qué |
| `origen-3d/` | `ProjectSettings/` y `Packages/` del proyecto 3D, de referencia |
| `informe-*.txt` | Salidas de la última comprobación |

## Repetir la comprobación

```bash
O="3D/Juego V1.1/Juego V1.1"
J="game/proyecto de grado"
T=tools/integracion-3d

# 1 · Está todo lo que llegó. Sale con error si falta un archivo o cambió un GUID.
python -I $T/inventario.py comparar $T/manifiesto-origen.csv "$J/Assets/Mundo3D/Externo" \
       --excepciones $T/excepciones.txt --informe $T/informe-inventario.txt

# 2 · No hay referencias rotas que no lo estuvieran ya en el 3D.
python -I $T/referencias.py "$J/Assets/Mundo3D/Externo" $T/informe-referencias.txt \
       --metas "$J/Assets" "$J/Library/PackageCache" --base $T/referencias-origen.txt

# 3 · Dentro de Unity (o con -batchmode -executeMethod Nexus.EditorTools.Mundo3D.Integracion3D.TodoEnLote):
#     menú Nexus > Mundo 3D > 3 · Verificar integración   → informe-unity.txt
```

En el paso 1, «idéntico» es byte a byte. «Cambiado» significa que el archivo sigue ahí con el mismo GUID y
otro contenido: los tres scripts portados al Input System nuevo, los materiales convertidos a URP y los
`.meta` que Unity 6 reescribe al importar. `informe-inventario.txt` los lista uno por uno.

## Rendimiento del recorrido

El nivel llegó con unos 14 millones de triángulos, 6.000 piezas de malla y 5.000 MeshColliders. El juego no
usa el nivel tal cual, sino una copia optimizada (`Assets/Mundo3D/Escenas/Recoleccion3D.unity`); el nivel del
equipo de 3D (`Externo/malfarie/Levels/Level1.unity`) y sus modelos no se tocan.

| Archivo | Qué es |
|---|---|
| `reducir_modelos.py` | Blender, en segundo plano: reduce cada modelo pesado a su presupuesto de triángulos. Escribe dos versiones en `Assets/Mundo3D/Optimizado/Modelos/`: `*_opt.fbx` (todo unido en una malla) y `*_partes.fbx` (pieza a pieza, para donde el nivel escondió o movió piezas) |
| `informe-reduccion.csv` | Modelo por modelo: triángulos antes y después, y qué modelo reducido lo sustituye. Unity lo lee |
| `informe-optimizacion.txt` | Lo que Unity no sustituyó en la escena, y por qué |
| `informe-rendimiento.txt` | El nivel original y la escena del juego, lado a lado: triángulos, piezas, colliders, memoria |

Cuando el equipo de 3D cambie el nivel, con el editor de Unity cerrado:

```bash
BLENDER="C:/Program Files/Blender Foundation/Blender 5.1/blender.exe"
UNITY="C:/Program Files/Unity/Hub/Editor/6000.0.82f1/Editor/Unity.exe"

# 1 · Modelos reducidos (solo hace los nuevos; --forzar los rehace todos)
"$BLENDER" -b --factory-startup -P tools/integracion-3d/reducir_modelos.py

# 2 · Rehacer la copia desde el nivel, optimizarla y medir
"$UNITY" -batchmode -projectPath "game/proyecto de grado" -logFile unity.log          -executeMethod Nexus.EditorTools.Mundo3D.Rendimiento3D.RehacerEnLote
```

Desde el editor son los menús `Nexus > Mundo 3D > 4…7` (el 5 rehace la copia desde el nivel y la optimiza).

Si un modelo reducido se ve mal, se le sube el presupuesto en `EXCEPCIONES` (arriba de `reducir_modelos.py`)
y se relanza con `--forzar --solo <nombre>`; después, el menú 5.

El rendimiento real se mide jugando: dentro del recorrido, **F3** enseña los FPS, y al salir queda una línea
en `rendimiento-3d.log` (carpeta de datos del juego: `%USERPROFILE%/AppData/LocalLow/<compañía>/<producto>/`).

## Volver a traer el 3D

Si el subequipo entrega una versión nueva: descomprimirla en `3D/`, regenerar el manifiesto
(`inventario.py generar`) y relanzar `copiar.py` (no sobrescribe lo que ya existe; los archivos que hayan
cambiado hay que revisarlos a mano). Después, los tres menús de `Nexus > Mundo 3D`.
