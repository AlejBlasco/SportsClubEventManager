# Inscripción y cancelación de inscripción a eventos

El caso de uso principal del proyecto: sustituye el WhatsApp manual al secretario del club por una inscripción con validación automática de aforo y duplicados. Referenciado desde la sección [`g. Funcionalidades principales`](../../README.md#g-funcionalidades-principales) del README.

## Flujo

```mermaid
flowchart TD
    Detail(["Ficha de detalle del evento<br/>(EventDetails.razor)"]) --> Click["Socio pulsa 'Inscribirme'"]
    Click --> Post["POST /api/v1/events/{id}/register<br/>(RegisterForEventCommand)"]

    Post --> LoadEvent["Se carga el evento con sus<br/>inscripciones actuales"]
    LoadEvent --> ExistsCheck{"¿Evento existe?"}
    ExistsCheck -->|No| E404["404 Not Found"]
    ExistsCheck -->|Sí| DateCheck{"¿Fecha del evento<br/>ya ha pasado?"}
    DateCheck -->|Sí| E400["400 Bad Request"]
    DateCheck -->|No| DupCheck{"¿Ya existe una inscripción<br/>activa del mismo socio?"}
    DupCheck -->|Sí| E409a["409 Conflict<br/>(DuplicateRegistrationException)"]
    DupCheck -->|No| CapCheck{"¿Aforo completo?<br/>(inscripciones activas ≥ MaxCapacity)"}
    CapCheck -->|Sí| E409b["409 Conflict<br/>(CapacityExceededException)"]
    CapCheck -->|No| Save["Se crea la Registration<br/>y se confirma con SaveChangesAsync"]

    Save --> Concurrency{"¿Conflicto de concurrencia?<br/>(dos inscripciones simultáneas<br/>a la última plaza)"}
    Concurrency -->|Sí| E409c["409 Conflict<br/>'Please try again'"]
    Concurrency -->|No| Success["201 Created"]

    Success --> Metric["Métrica Prometheus:<br/>sportsclubeventmanager_event_registrations_total"]
    Success --> Notify["Webhook n8n:<br/>email de confirmación al socio"]
    Success --> MyRegs["La inscripción aparece en<br/>'Mis inscripciones' (MyRegistrations.razor)"]

    MyRegs --> Cancel["Socio pulsa 'Cancelar'<br/>sobre una inscripción propia"]
    Cancel --> Delete["DELETE /api/v1/events/{id}/register<br/>o DELETE /api/v1/registrations/{id}"]
    Delete --> CancelHandler["Se elimina físicamente la Registration<br/>(hard delete, Registrations.Remove)"]
    CancelHandler --> CancelSuccess["204 No Content"]
```

> Para la interacción completa componente-a-componente (incluida la variante de cancelación por administrador), ver el [sequence diagram de cancelación](../architecture/diagrams/sequence-cancellation.md).

## Explicación del flujo

`EventsController.RegisterForEvent` (`[Authorize]`, `POST /api/v1/events/{id}/register`) despacha `RegisterForEventCommand` con el `EventId` de la ruta y el `UserId` extraído del JWT del socio autenticado — un usuario nunca puede inscribir a otro usuario por esta vía (esa capacidad es exclusiva de administradores, ver [`administracion-inscripciones.md`](administracion-inscripciones.md)).

`RegisterForEventCommandHandler` aplica, en orden, las tres reglas de negocio que antes verificaba manualmente el secretario del club por WhatsApp:

1. **El evento no puede haber pasado ya** (`Event.ValidateFutureDate`, vía comparación directa de `Event.Date` con `DateTime.UtcNow`).
2. **El socio no puede tener ya una inscripción activa** para el mismo evento (`DuplicateRegistrationException` si existe una `Registration` con `Status != Cancelled`).
3. **El evento no puede estar completo** (`CapacityExceededException` si las inscripciones activas ya igualan `MaxCapacity`).

Si las tres se cumplen, se crea la `Registration` y se confirma con `SaveChangesAsync`. `Event.RowVersion` proporciona **concurrencia optimista** de EF Core: si dos socios intentan inscribirse simultáneamente en la última plaza disponible, uno de los dos recibe un `DbUpdateConcurrencyException`, que el handler traduce en un `409 Conflict` pidiendo reintentar — nunca se permite que el aforo se sobrepase por una condición de carrera.

Solo **después** de que `SaveChangesAsync` confirme la escritura se disparan los efectos secundarios: el contador Prometheus `sportsclubeventmanager_event_registrations_total{source="self-service"}` y la notificación por email vía el webhook de n8n `registration-confirmed` (ver la [sección 9 del documento de arquitectura](../architecture/architecture.md#9-flujo-end-to-end-inscribirse-a-un-evento) para el diagrama de secuencia completo, capa por capa).

La cancelación (`DELETE /api/v1/events/{id}/register` desde la ficha del evento, vía `CancelRegistrationCommandHandler`; o `DELETE /api/v1/registrations/{id}` desde "Mis inscripciones", vía `CancelRegistrationByIdCommandHandler`) **elimina físicamente** la fila (`Registrations.Remove(registration)` en ambos handlers, explícitamente comentado como "hard delete" en el código) — no hay histórico de bajas conservado en base de datos. El método `Registration.Cancel()` del dominio (que sí cambiaría `Status` a `Cancelled` sin borrar) existe pero no lo invoca ningún *handler* actual; es código muerto. La única traza que queda de una cancelación por parte de un socio es el contador Prometheus `sportsclubeventmanager_registration_cancellations_total{source="self-service"}` — a diferencia de la cancelación por administrador, que sí queda registrada en el panel de auditoría (ver [`administracion-inscripciones.md`](administracion-inscripciones.md)).
