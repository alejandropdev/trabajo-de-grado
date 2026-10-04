# Ilustraciones

Aquí va el arte pintado del juego. La interfaz lo busca por nombre y, mientras un archivo no exista, muestra
una silueta con el nombre en su lugar: se puede ir entregando poco a poco sin tocar código.

| Archivo | Dónde sale |
|---|---|
| `personajes/<nombre>.png` | Retratos: diálogos, la guía, quien espera un reto, la Fase 1. El nombre es el del personaje en minúsculas, sin tildes y con guiones (`marisol-andrade`, `voss`, `javier`, `oscar-rendon`, `facilities`). |
| `personajes/protagonista.png` | El protagonista pensando, a la izquierda de la Fase 1. |
| `escenas/<GUION>-<n>.png` | La viñeta `n` (desde 1) de la escena `<GUION>` (el id del guion, p. ej. `CIN-1.0-3`). Si no existe, la viñeta usa el retrato de quien habla. |

PNG con transparencia para personajes; escenas en 16:9 o 4:3. Importar como Sprite (Single).
