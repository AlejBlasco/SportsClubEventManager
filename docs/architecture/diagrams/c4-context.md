# C4 — Diagrama de Contexto

Parte del catálogo de diagramas de la issue [#51](https://github.com/AlejBlasco/SportsClubEventManager/issues/51). Ver el índice completo en [`README.md`](README.md).

Nivel más alto del modelo [C4](https://c4model.com/): **SportsClubEventManager** como una única caja, sus actores humanos y los sistemas externos con los que se integra. No entra en el detalle de `Api`/`Web`/base de datos — eso es el [Diagrama de Contenedores](c4-container.md).

```mermaid
C4Context
    title Diagrama de Contexto del Sistema — SportsClubEventManager

    Person(socio, "Socio del club", "Consulta el calendario, se inscribe/cancela su inscripción a eventos, gestiona su propio perfil")
    Person(admin, "Administrador", "Gestiona eventos, usuarios e inscripciones; importa el calendario oficial desde CSV")
    Person(anon, "Visitante anónimo", "Consulta el calendario público de eventos, sin necesidad de iniciar sesión")

    System(scem, "SportsClubEventManager", "Gestión integral de eventos, inscripciones y socios de un club deportivo de tiro")

    System_Ext(google, "Google OAuth2", "Autenticación federada (login con cuenta de Google)")
    System_Ext(n8n, "n8n (homelab)", "Automatiza el envío de emails: confirmación, cambios y recordatorios de evento")
    System_Ext(monitoring, "Monitoring (homelab)", "Prometheus + Grafana, stack compartido del homelab; visualiza las métricas de la aplicación")
    System_Ext(federacion, "Federación de tiro deportivo", "Publica el calendario oficial de competiciones en un fichero CSV (proceso manual, sin integración en vivo)")

    Rel(socio, scem, "Usa", "HTTPS")
    Rel(admin, scem, "Administra", "HTTPS")
    Rel(anon, scem, "Consulta el calendario público", "HTTPS")
    Rel(scem, google, "Autentica usuarios vía", "OAuth2")
    Rel(scem, n8n, "Dispara webhooks de notificación hacia", "HTTPS")
    Rel(scem, monitoring, "Expone métricas de negocio/infraestructura, scrapeadas por", "HTTP /metrics")
    Rel(admin, federacion, "Descarga manualmente el CSV oficial de", "fuera de línea")
    Rel(federacion, scem, "El CSV descargado se sube e importa en", "carga manual, issue #35/#36")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

## Notas

- **La Federación no es una integración en vivo**: no hay ninguna API ni webhook con la federación — es un fichero CSV que el administrador descarga manualmente de la web de la federación y sube a través de [Importación masiva de eventos](../../operations/importacion-masiva-eventos.md). Se representa aquí como sistema externo porque es la fuente de datos real que origina todo el proyecto (ver la introducción del [README](../../../README.md)), aunque la interacción sea manual y offline.
- **`monitoring` (Prometheus + Grafana) no es propiedad de este proyecto**: es un stack compartido preexistente del homelab, reutilizado en vez de duplicado — ver [`docs/observability/observability.md`](../../observability/observability.md) para el porqué y el detalle completo.
- Los tres tipos de `Person` reflejan los tres niveles de acceso reales de la aplicación: anónimo (solo calendario público), `User` (autoservicio) y `Administrator` (gestión completa) — ver [`docs/technical/issue-28-autorizacion-basada-en-roles.md`](../../technical/issue-28-autorizacion-basada-en-roles.md).
