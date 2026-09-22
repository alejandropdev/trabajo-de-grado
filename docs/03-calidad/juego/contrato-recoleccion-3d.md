# Contrato de la recolección 3D (Fase 1)

**Para:** el subequipo que construye el recorrido 3D de la Fase 1 (zonas A–F).
**Qué es:** lo que el módulo 3D recibe del juego al empezar y lo que tiene que devolver al terminar.
Mientras el 3D no existe, el juego lo sustituye con un simulador que devuelve exactamente lo mismo, así
que cambiar uno por otro no toca nada más.

---

## El reparto de trabajo

| Quién | Hace |
|---|---|
| **El juego (motor)** | Decide QUÉ hay en cada zona: el contenido está en el JSON del nivel. Aplica los efectos de lo que se recogió y lo valida |
| **El módulo 3D** | Enseña el plano, deja moverse con un reloj de `minutosDisponibles` y dice **qué zonas se visitaron y qué hallazgos se recogieron** |

El 3D **no calcula efectos** (dinero, moral…): solo devuelve ids. Así el contenido cambia sin tocar el 3D, y
el 3D no puede darle al jugador nada que el diseño no haya puesto.

---

## ENTRADA · `EntradaDeRecoleccion`

Se obtiene con `GameSession.EntradaDeRecoleccion(coleccionablesDelPerfil)`. Es un documento autocontenido y
se puede serializar a JSON tal cual.

```json
{
  "nivelId": "nivel-01",
  "semilla": 12345,
  "nivelAndamiaje": 2,
  "minutosDisponibles": 120,
  "texto": "Tienes dos horas para recorrer el plano del encargo (zonas A a F). No da tiempo a verlo todo: elige.",
  "zonas": [
    { "id": "A", "nombre": "La garita de entrada", "descripcion": "…", "minutosDeVisita": 20 }
  ],
  "hallazgos": [
    { "id": "PISTA-N1-01", "tipo": "pista",         "zona": "A", "texto": "El guardia cuenta que el mando genérico abre la barrera desde la calle." },
    { "id": "REC-N1-01",   "tipo": "recurso",       "zona": "D", "nombre": "Adelanto de presupuesto", "efectos": { "Dinero": 1500 } },
    { "id": "PER-N1-01",   "tipo": "personaje",     "zona": "E", "nombre": "Javier", "efectos": { "VelocidadMod": 0.05, "MoralEquipo": 3 },
                           "flagAlCerrar": "FLG_JAVIER_CONFIANZA", "valorDelFlag": 1 },
    { "id": "MOR-N1-01",   "tipo": "moral",         "zona": "B", "efectos": { "MoralEquipo": 4, "Cansancio": -3 } },
    { "id": "COL-DEV-06",  "tipo": "coleccionable", "zona": "F" },
    { "id": "RIE-N1-01",   "tipo": "riesgo",        "zona": "C", "efectos": { "Cansancio": 4, "SatisfaccionCliente": -2 } }
  ],
  "coleccionablesYaEnElPerfil": ["COL-ADV-01"],
  "estadoInicial": { "Dinero": 18000, "MoralEquipo": 60, "Cansancio": 10 }
}
```

| Campo | Para qué le sirve al 3D |
|---|---|
| `semilla` | Si el 3D tiene algo aleatorio (dónde aparece un personaje), que use esta semilla: la misma partida tiene que dar el mismo recorrido (modo aula) |
| `nivelAndamiaje` | 3 = tutorial: se pueden señalar los hallazgos. 0 = sin ninguna ayuda |
| `minutosDisponibles` | El reloj del recorrido, en minutos de juego |
| `zonas[].minutosDeVisita` | Lo que cuesta del reloj entrar en esa zona |
| `hallazgos[]` | Qué se puede encontrar y dónde. `efectos` es informativo (para enseñarlo en un HUD, si se quiere): el motor lo aplica, no el 3D |
| `coleccionablesYaEnElPerfil` | Lo que el estudiante ya tiene en su diario. El 3D puede enseñarlo como «ya visto» |
| `estadoInicial` | Los stocks del proyecto al empezar, por si el HUD los enseña |

### Los tipos de hallazgo

| Tipo | Qué es | Efecto |
|---|---|---|
| `pista` | Algo que el encargo no decía. Ayuda a decidir la metodología y la arquitectura | Se añade al encargo en pantalla |
| `recurso` | Dinero, documentación… | Suma a esos stocks |
| `personaje` | Alguien que se une o ayuda (trabajadores, aliados) | Velocidad y moral. Además, puede dejar un flag de historia al cerrar el nivel |
| `moral` | Lo que cuida al equipo | Moral arriba, cansancio abajo |
| `coleccionable` | Una pieza del Diario de Campo | Va al diario del estudiante |
| `riesgo` | Algo que se toca y no se debía | Resta |

Los stocks posibles en `efectos` son los del proyecto: `Dias, Dinero, Alcance, Avance, DeudaTecnica,
Cobertura, Documentacion, MoralEquipo, Cansancio, Competencia, SatisfaccionCliente, Reputacion,
SaludJugador, VelocidadMod`.

---

## SALIDA · `ResultadoDeRecoleccion`

```json
{
  "nivelId": "nivel-01",
  "minutosUsados": 95,
  "zonasVisitadas": ["A", "B", "D"],
  "hallazgos": ["PISTA-N1-01", "MOR-N1-01", "REC-N1-01"],
  "abandonoAntes": false
}
```

Se entrega con `GameSession.AplicarRecoleccion(resultado)`, **una sola vez**, antes de cerrar la Fase 1.
El motor **rechaza** (lanza `InvalidOperationException` con el motivo) un resultado que:
- sea de otro nivel;
- use más minutos de los disponibles, o menos de cero;
- nombre una zona que no existe;
- nombre un hallazgo que no existe, o lo repita;
- traiga un hallazgo de una zona que no está en `zonasVisitadas`.

Qué hace el motor con un resultado válido:
- Suma los `efectos` de cada hallazgo al proyecto.
- Pasa los coleccionables al diario.
- Guarda las pistas, que salen en el encargo.
- Los personajes con `flagAlCerrar` escriben su flag **al cerrar el nivel**, nunca antes (INV-6).
- Todo viaja en el guardado (INV-7).

---

## Cómo se simula hoy

`SimuladorDeRecoleccion.Simular(entrada, intensidad)`, con `"rapida"`, `"normal"` o `"a-fondo"`. Usa el 40 %,
el 70 % o el 100 % del tiempo, recoge cada hallazgo de las zonas visitadas con probabilidad 0,5, 0,8 o 1, y
los riesgos con 0,2, 0,35 o 0,6. Es determinista: misma semilla e intensidad, mismo resultado. En pantalla
son los tres botones «Simular recolección 3D» del paso «Recorrido» de la Fase 1.

## Dónde está el contenido

`StreamingAssets/niveles/nivel-XX.json` → `fase1.recoleccion` (`minutosDisponibles`, `texto`, `zonas`,
`hallazgos`). El validador del catálogo comprueba al arrancar el juego que cada hallazgo esté en una zona
que existe, que sus efectos sean stocks reales, que los coleccionables estén en el catálogo y que los
flags estén en el censo. Si algo no cuadra, el juego no arranca y dice qué es.
