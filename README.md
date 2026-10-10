# BMAD-Hospital

Sistema de turnos del **Hospital Municipal Las Acacias**, un caso **simulado** para la materia
Modelación y Diseño de Sistemas. Se construye con el método **BMAD**: un equipo de agentes de IA
(analista, producto, arquitectura, UX y desarrollo) trabaja junto al docente, documento por documento,
antes de escribir una línea de código.

## Por dónde empezar

1. **[Sesiones/](Sesiones/)**: el registro de cada sesión de trabajo, en orden. Ahí se ve cómo se tomó cada decisión.
2. **[docs/](docs/)**: los documentos del proyecto, en el orden en que se escribieron:

| Documento | Qué responde |
|---|---|
| [01-brief.md](docs/01-brief.md) | ¿Qué problema tiene el hospital y para quién es el sistema? |
| [02-prd.md](docs/02-prd.md) | ¿Qué tiene que hacer el sistema, exactamente? |
| [03-ux.md](docs/03-ux.md) | ¿Cómo se ven y cómo se usan las pantallas? |
| [04-arquitectura.md](docs/04-arquitectura.md) | ¿Cómo se construye? |
| [05-epicas-y-plan.md](docs/05-epicas-y-plan.md) | ¿En qué orden? |
| [stories/](docs/stories/) | Cada pieza de trabajo, lista para programar. |

## Tecnología

.NET 10 · Blazor · PostgreSQL · xUnit · Docker.

## Estado

Documentación del producto aprobada. Las stories del sprint 1 están escritas y revisadas; el código
empieza con la story 1.1. El código que hoy está en `src/` y `tests/` es un arranque provisorio que
esa story reemplaza.
