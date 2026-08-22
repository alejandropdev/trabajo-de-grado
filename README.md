# Videojuego serio para la simulación de proyectos de software

Trabajo de grado — Ingeniería de Sistemas, Pontificia Universidad Javeriana, Bogotá D.C.

Simulador educativo en el que el estudiante asume el rol de responsable de un proyecto
de software y experimenta las consecuencias de sus decisiones técnicas y de gestión
sobre la Tríada de Hierro (tiempo, costo, calidad), la deuda técnica y el equipo.

## Estructura del repositorio

```
docs/       Documentación del trabajo de grado (PMP, SRS, GDD, arquitectura, QA)
game/       Proyecto de Unity 6
tools/      Scripts auxiliares y hojas de balanceo
builds/     Compilaciones locales (no versionado)
```

Dentro de `game/Assets/_Project/Scripts` el código está separado en cuatro capas con
dependencia unidireccional descendente:

```
UI     ──> App ──> Data ──> Domain
                              ^
                            Core
```

`Domain` es C# puro y no referencia `UnityEngine`; esa restricción está impuesta por su
assembly definition (`noEngineReferences: true`), de modo que el motor de simulación pueda
ejecutarse y balancearse en pruebas automatizadas sin abrir una escena.

## Requisitos

- Unity 6 (versión LTS fijada en `game/ProjectSettings/ProjectVersion.txt`)
- Visual Studio 2022 Community o JetBrains Rider
- Git 2.43 o superior y Git LFS

## Primera configuración

```bash
git clone <url-del-repositorio>
cd <repositorio>
git lfs install
git lfs pull
```

Configurar el mezclador de archivos YAML de Unity (evita conflictos irresolubles en
escenas y prefabs):

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver 'UnityYAMLMerge merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```

Después, abrir la carpeta `game/` desde Unity Hub.

## Flujo de trabajo

- `main` — únicamente versiones entregadas y etiquetadas.
- `develop` — rama de integración; toda funcionalidad se mezcla aquí.
- `feature/<modulo>-<descripcion>` — trabajo diario. Ejemplo: `feature/simulation-velocity-model`.
- `fix/<descripcion>` — corrección de defectos.
- `docs/<descripcion>` — cambios exclusivos de documentación.

Toda integración a `develop` se hace por *pull request* con revisión de al menos un
compañero. Ver `CONTRIBUTING.md`.

## Convención de commits

```
<tipo>(<módulo>): <descripción en imperativo>

feat(simulation): añadir modelo de acumulación de deuda técnica
fix(ui): corregir refresco del burndown al cambiar de fase
docs(gdd): incorporar sección de mecánicas de negociación
test(domain): cubrir la curva de incorporación del equipo
chore(repo): actualizar reglas de Git LFS
```

Tipos: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `art`, `balance`.

## Documentación

| Carpeta | Contenido |
| --- | --- |
| `docs/00-gestion` | Plan de administración del proyecto, cronograma, actas |
| `docs/01-requisitos` | SRS y matriz de trazabilidad |
| `docs/02-diseno` | GDD, arquitectura conceptual y mecánicas, ADR, diagramas |
| `docs/03-calidad` | Plan de control de calidad, casos de prueba, registro de defectos |
| `docs/04-pedagogia` | Fundamentación pedagógica y objetivos de aprendizaje |
| `docs/05-playtesting` | Protocolos, instrumentos y resultados de pruebas piloto |
| `docs/06-entregas` | Versiones entregadas a la asignatura |
