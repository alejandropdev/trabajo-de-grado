# Ilustraciones

Aquí va el arte pintado del juego. La interfaz lo busca por nombre y, mientras un archivo no exista, muestra
una silueta con el nombre en su lugar: se puede ir entregando poco a poco sin tocar código.

| Archivo | Dónde sale |
|---|---|
| `personajes/<nombre>.png` | Retratos: diálogos, la guía, quien espera un reto, la Fase 1. El nombre es el del personaje en minúsculas, sin tildes y con guiones (`marisol-andrade`, `voss`, `javier`, `oscar-rendon`, `facilities`). |
| `personajes/protagonista.png` | El protagonista pensando, a la izquierda de la Fase 1. |
| `escenas/<GUION>-<n>.png` | La viñeta `n` (desde 1) de la escena `<GUION>` (el id del guion, p. ej. `CIN-1.0-3`). Si no existe, la viñeta usa el retrato de quien habla. |

PNG con transparencia para personajes; escenas en 16:9 o 4:3. Importar como Sprite (Single).

## Arte provisional generado (octubre 2026)

Todo lo que hay ahora en `personajes/` y `escenas/` se generó con IA (OpenAI `gpt-image-1.5` para retratos
con transparencia, `gpt-image-2` para viñetas) usando como referencia el arte conceptual del *Documento de
Diseño y Validación de los Elementos Visuales* (`knowledge-base/ilustraciones/`). Es **provisional**: sustituir
por el arte final dejando el mismo nombre de archivo (el `.meta` conserva la configuración de importación).

- **Retratos** (768×768, fondo transparente): `protagonista`, `hh`, `marisol-andrade`, `marta-oyola`,
  `ivan-cardenas`, `oscar-rendon` (+ `oscar`, para «Óscar» en los minijuegos), `sarah-lindqvist`, `javier`,
  `voss` (+ `helena-voss`), `facilities` (la Supervisora de Turno) y `megafonia` (un altavoz).
  Marisol e Iván no tienen hoja conceptual: se inventaron a partir de su descripción en el JSON.
- **Viñetas** (1152×768) solo para líneas del narrador que dicen lo mismo en todas las variantes del guion:
  el id `escenas/<GUION>-<n>` no distingue variante, así que una viñeta común taparía, en otra variante,
  el retrato de quien habla. Sin viñeta a propósito: `CIN-0.2-3/4/5`, `CIN-1.1-1`, `CIN-1.3-2/3`.
