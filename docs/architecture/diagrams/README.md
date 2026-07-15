# Catálogo de diagramas — issue #51

Índice de los diagramas visuales que cubren la issue [#51 — "[Documentation] Create System Diagrams"](https://github.com/AlejBlasco/SportsClubEventManager/issues/51). Todos en formato [Mermaid](https://mermaid.js.org/) (texto plano, versionable, renderizado nativamente por GitHub), consistente con el resto de `docs/`.

No todos los diagramas viven físicamente en esta carpeta: dos de ellos (Registro, y el propio esquema de capas/dominio en `architecture.md`) ya estaban correctamente integrados con su contexto narrativo en otro documento, y moverlos habría roto esa narrativa sin aportar nada — este índice enlaza a ellos en vez de duplicarlos.

| Diagrama | AC de la issue #51 | Dónde vive | Notas |
|---|---|---|---|
| [C4 Context](c4-context.md) | C4 Context Diagram | Esta carpeta | Sistema + actores humanos + sistemas externos (Google, n8n, monitoring, Federación) |
| [C4 Container](c4-container.md) | C4 Container Diagram | Esta carpeta | `Api`/`Web`/`SQL Server` + Google/n8n/Prometheus/Grafana, como unidades de despliegue reales |
| [Diagrama ER](er-diagram.md) | ER diagram | Esta carpeta | Verificado línea a línea contra las 5 `IEntityTypeConfiguration<T>` reales |
| [Sequence — Registro](../architecture.md#9-flujo-end-to-end-inscribirse-a-un-evento) | Sequence diagram (registration) | `docs/architecture/architecture.md` §9 | Se queda in-situ: forma parte de la explicación de la arquitectura en capas |
| [Sequence — Cancelación](sequence-cancellation.md) | Sequence diagram (cancellation) | Esta carpeta | Cubre autoservicio y administrador (mismo *handler*, distinto flag) |
| [Sequence — CRUD eventos (admin)](sequence-event-crud.md) | Sequence diagram (admin event CRUD) | Esta carpeta | Create/Update/Delete en un único diagrama con bloques `alt` |
| [CI/CD pipeline](cicd-pipeline.md) | CI/CD pipeline flow diagram | Esta carpeta | Movido desde `docs/technical/issue-45-despliegue-automatizado-al-homelab.md` (única fuente de verdad) |

## Diagramas relacionados, fuera del alcance literal de la issue

No forman parte de los ACs de la #51, pero cubren terreno adyacente y ya existían antes de esta issue — se mencionan aquí para que quien busque un diagrama concreto no tenga que adivinar en qué documento está:

- [`docs/architecture/architecture.md`](../architecture.md) — vista de capas Clean Architecture (§2), grafo de referencias entre proyectos (§3), estructura de carpetas (§4), CQRS (§5), pipeline de MediatR (§6), modelo de dominio rico (§7), persistencia (§8), manejo de errores (§10), `DelegatingHandlers` (§11), tareas en segundo plano (§12), composition root (§13).
- [`docs/observability/observability.md`](../../observability/observability.md) — arquitectura de observabilidad en producción (Prometheus + Grafana compartidos del homelab) y el diagrama del incidente real de *provisioning*.
- Varios `docs/operations/*.md` y `docs/technical/*.md` tienen sus propios `flowchart` de lógica de negocio (decisiones/ramas), complementarios pero distintos en propósito a los `sequenceDiagram` de esta carpeta (interacción componente-a-componente en el tiempo).

## Mantenimiento

Estos diagramas reflejan el código real verificado en 2026-07-14 (no un diseño aspiracional — mismo criterio que el resto de `docs/`). Si cambia algo que afecte a uno de ellos (nueva entidad, nuevo contenedor desplegado, ruta de cancelación reescrita, etc.), actualiza el diagrama en el mismo cambio — es exactamente el riesgo de "los diagramas se quedan desactualizados" que la propia issue #51 señala como riesgo principal.
