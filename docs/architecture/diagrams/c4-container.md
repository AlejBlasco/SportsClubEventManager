# C4 — Diagrama de Contenedores

Parte del catálogo de diagramas de la issue [#51](https://github.com/AlejBlasco/SportsClubEventManager/issues/51). Ver el índice completo en [`README.md`](README.md).

Nivel 2 del modelo [C4](https://c4model.com/): zoom dentro de la caja `SportsClubEventManager` del [Diagrama de Contexto](c4-context.md), mostrando las **unidades realmente desplegables** (contenedores en sentido C4 — procesos/servicios, no solo contenedores Docker) y los sistemas externos con los que cada una habla directamente.

> Esta es la vista C4 "oficial" que pide la issue #51. `docs/architecture/architecture.md` §2 tiene un diagrama relacionado pero con un propósito distinto: muestra las **capas de código** de Clean Architecture (Application, Domain, Infrastructure) dentro de `Api`/`Web`, útil para entender la organización interna del código — no la unidad de despliegue. Aquí, esas tres capas se consideran un único contenedor cada una (`Api`, `Web`), tal y como exige la notación C4 Container.

```mermaid
C4Container
    title Diagrama de Contenedores — SportsClubEventManager

    Person(usuario, "Socio / Administrador / Visitante", "Usuario de la aplicación, según su rol")

    System_Boundary(scem, "SportsClubEventManager") {
        Container(web, "Web", "Blazor Server, ASP.NET Core 10, Radzen.Blazor", "Interfaz de usuario server-rendered; llama a Api vía HttpClient tipado")
        Container(api, "Api", "ASP.NET Core 10 Web API", "Lógica de negocio (CQRS + MediatR + FluentValidation); expone REST y /metrics")
        ContainerDb(db, "SQL Server", "SQL Server 2022", "Persiste eventos, usuarios, inscripciones y auditoría")
    }

    System_Ext(google, "Google OAuth2", "Autenticación federada")
    System_Ext(n8n, "n8n", "Automatización de notificaciones por email (homelab)")
    System_Ext(prometheus, "Prometheus", "Recolecta métricas — stack 'monitoring' compartido del homelab")
    System_Ext(grafana, "Grafana", "Visualiza métricas, dashboard público — mismo stack 'monitoring'")

    Rel(usuario, web, "Usa", "HTTPS")
    Rel(web, api, "Llama", "HTTP + JWT (HttpClient tipado)")
    Rel(api, db, "Lee/escribe", "EF Core")
    Rel(api, google, "Autentica usuarios vía", "OAuth2")
    Rel(api, n8n, "Dispara webhooks de notificación", "HTTPS")
    Rel(api, prometheus, "Expone métricas", "GET /metrics")
    Rel(web, prometheus, "Expone métricas", "GET /metrics")
    Rel(prometheus, grafana, "Fuente de datos", "PromQL")

    UpdateLayoutConfig($c4ShapeInRow="4", $c4BoundaryInRow="1")
```

## Notas

- **`Web` nunca llama directamente a la base de datos** — todo pasa por `Api` vía HTTP, ver `docs/architecture/architecture.md` §2 y §11 (cadena de `DelegatingHandlers`).
- **`Api`/`Web` no tienen ningún contenedor Prometheus/Grafana propio en producción** — ambos son del stack `monitoring` compartido del homelab, no de este proyecto. `Api`/`Web` solo exponen `/metrics`; quién lo scrapea y visualiza es responsabilidad de infraestructura externa. Detalle completo en [`docs/observability/observability.md`](../../observability/observability.md).
- `SportsClubEventManager.Shared` (DTOs) no aparece como contenedor aparte — es una librería compartida entre `Api` y `Web`, no un proceso propio; a nivel C4 Container solo cuentan las unidades desplegables independientemente.
