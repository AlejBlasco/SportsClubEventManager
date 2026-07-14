# Sequence Diagram — CRUD de eventos (administrador)

Parte del catálogo de diagramas de la issue [#51](https://github.com/AlejBlasco/SportsClubEventManager/issues/51). Ver el índice completo en [`README.md`](README.md).

Complementa al flowchart (lógica de decisión) de [`docs/operations/administracion-eventos.md`](../../operations/administracion-eventos.md) con la interacción real componente-a-componente en el tiempo, para las tres operaciones de escritura (`Create`/`Update`/`Delete`) que expone `AdminEventsController`.

```mermaid
sequenceDiagram
    actor A as Administrador
    participant EM as EventManagement.razor
    participant EFM as EventFormModal
    participant ES as EventManagementService (Web)
    participant AEC as AdminEventsController<br/>/api/admin/events
    participant DB as SQL Server
    participant Audit as AuditService
    participant N8n as n8n (webhook)

    A->>EM: Clic "Create New Event" / "Edit"
    EM->>EFM: Open() / Open(eventItem)
    A->>EFM: Rellena título, descripción, fecha,<br/>ubicación, aforo
    A->>EFM: Clic "Save"

    alt Crear evento
        EFM->>ES: CreateEventAsync(request)
        ES->>AEC: POST /api/admin/events
        AEC->>AEC: CreateEventCommandValidator<br/>(fecha futura, aforo 1-10.000)
        AEC->>DB: Events.Add(newEvent)
        AEC->>Audit: LogAsync(EventCreated, adminId, ...)
        AEC->>DB: SaveChangesAsync()
        AEC-->>ES: 201 Created (EventId)

    else Editar evento
        EFM->>ES: UpdateEventAsync(eventId, request)
        ES->>AEC: PUT /api/admin/events/{id}
        AEC->>DB: SELECT Event (Include Registrations, User)
        AEC->>AEC: UpdateEventCommandValidator<br/>(fecha futura; aforo &gt;= inscripciones activas)
        alt evento ya ocurrió
            AEC-->>ES: 400 Bad Request "Past events cannot be modified"
        else RowVersion no coincide
            AEC-->>ES: 409 Conflict "modified by another user"
        else válido
            AEC->>DB: actualiza Title/Description/Date/Location/MaxCapacity
            AEC->>Audit: LogAsync(EventUpdated, adminId, ... OldValues/NewValues)
            AEC->>DB: SaveChangesAsync() (con RowVersion original)
            AEC->>N8n: NotifyEventUpdatedAsync<br/>(a cada inscrito activo)
            AEC-->>ES: 200 OK (evento actualizado)
        end

    else Eliminar evento
        EFM->>ES: DeleteEventAsync(eventId)
        ES->>AEC: DELETE /api/admin/events/{id}
        AEC->>DB: SELECT Event (Include Registrations, User)
        alt evento ya ocurrió
            AEC-->>ES: 400 Bad Request "Past events cannot be deleted"
        else válido
            AEC->>DB: BEGIN TRANSACTION
            AEC->>DB: ExecuteUpdate: Registrations.Status = Cancelled<br/>(bulk, WHERE EventId AND Status != Cancelled)
            AEC->>Audit: LogAsync(EventDeleted, adminId, ...)
            AEC->>DB: Events.Remove(eventEntity)
            AEC->>DB: SaveChangesAsync() + COMMIT
            AEC->>N8n: NotifyEventCancelledAsync<br/>(a cada inscrito que estaba activo)
            AEC-->>ES: 200 OK (DeleteEventResponse: cancelledCount)
        end
    end

    ES-->>EM: OnEventSaved / OnEventDeleted
    EM->>EM: LoadEventsAsync() — recarga la tabla
    EM-->>A: "Event saved successfully." / resumen de borrado
```

## Notas

- **Solo `Update`/`Delete` tienen la regla "evento pasado es de solo lectura"** — `Create` no la necesita (un evento nuevo siempre se valida como fecha futura desde su propio validador).
- **Concurrencia optimista real solo en `Update`**: el `RowVersion` leído al abrir el formulario viaja de vuelta en la petición y se compara en `SaveChangesAsync`; si otro administrador editó el mismo evento mientras tanto, EF Core lanza `DbUpdateConcurrencyException` → `409 Conflict`.
- **Cancelación en bloque, no fila a fila**: `Delete` usa `ExecuteUpdateAsync` (un único `UPDATE` con `WHERE`) en vez de cargar y guardar cada `Registration` — requisito de rendimiento (NFR-2, SLA de 5 segundos con 500+ inscripciones), documentado en `DeleteEventCommandHandler`.
- **Las notificaciones a n8n solo se disparan tras un `COMMIT`/`SaveChangesAsync` exitoso**, nunca dentro del bloque que puede hacer *rollback* — una operación fallida nunca genera un email de "evento actualizado/cancelado" (issue #37).
- **Auditoría en las tres operaciones**, no solo en `Update`/`Delete` — `Create` también registra `AuditAction.EventCreated`.
