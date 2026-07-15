# Sequence Diagram — Cancelación de inscripción

Parte del catálogo de diagramas de la issue [#51](https://github.com/AlejBlasco/SportsClubEventManager/issues/51). Ver el índice completo en [`README.md`](README.md).

Complementa al flowchart (lógica de decisión) de [`docs/operations/inscripcion-eventos.md`](../../operations/inscripcion-eventos.md) con la interacción real componente-a-componente en el tiempo. Cubre las **dos** variantes que existen en el código — autoservicio y administrador — porque ambas pasan por el mismo `CancelRegistrationByIdCommandHandler`, solo cambia el flag `IsAdministrator`.

```mermaid
sequenceDiagram
    actor U as Socio
    participant MR as MyRegistrations.razor
    participant CD as ConfirmationDialog
    participant RS as RegistrationService (Web)
    participant RC as RegistrationsController<br/>DELETE /api/v1/registrations/{id}
    actor A as Administrador
    participant ARC as AdminRegistrationsController<br/>DELETE /api/admin/registrations/{id}
    participant H as CancelRegistrationByIdCommandHandler
    participant DB as SQL Server
    participant Audit as AuditService

    U->>MR: Clic "Cancel" en una fila
    MR->>CD: ShowCancelConfirmation(registration)
    CD-->>U: "¿Seguro que quieres cancelar?"
    U->>CD: Confirma
    CD->>MR: HandleCancellationConfirmAsync()
    MR->>RS: CancelMyRegistrationAsync(registrationId)
    RS->>RC: HTTP DELETE /api/v1/registrations/{id}<br/>(JWT del socio)
    RC->>H: Send(CancelRegistrationByIdCommand<br/>{ RequestingUserId, IsAdministrator: false })

    Note over A,ARC: Variante administrador (misma issue, admin puede cancelar la de cualquiera)
    A->>ARC: HTTP DELETE /api/admin/registrations/{id}
    ARC->>H: Send(CancelRegistrationByIdCommand<br/>{ RequestingUserId: adminId, IsAdministrator: true })

    H->>DB: SELECT Registration (Include Event, User)
    DB-->>H: registration

    alt registro no existe
        H-->>RC: EntityNotFoundException
        RC-->>RS: 404 Not Found
    else no es autoservicio y no es el dueño
        H-->>RC: UnauthorizedAccessException
        RC-->>RS: 403 Forbidden
    else no es admin y el evento ya ocurrió
        H-->>RC: DomainException
        RC-->>RS: 400 Bad Request
    else válido
        H->>DB: Registrations.Remove(registration)
        opt IsAdministrator == true
            H->>Audit: LogAsync(RegistrationCancelled, adminId, targetUserId, targetEmail, ipAddress, userAgent)
        end
        H->>DB: SaveChangesAsync()
        alt DbUpdateConcurrencyException
            H-->>RC: DomainException("modified or deleted by another process")
            RC-->>RS: 400 Bad Request
        else éxito
            H-->>RC: (void)
            RC-->>RS: 204 No Content
            RS-->>MR: success = true
            MR-->>U: "Registration cancelled successfully."<br/>recarga la lista
        end
    end
```

## Notas

- **Sin auditoría en autoservicio**: `AuditService.LogAsync` solo se invoca cuando `IsAdministrator == true` — un socio cancelando su propia inscripción no genera entrada de auditoría (ver [`administracion-inscripciones.md`](../../operations/administracion-inscripciones.md)).
- **Eliminación física, no *soft-delete***: `Registrations.Remove(registration)` borra la fila; no hay una columna `IsDeleted`. La decisión de diseño y su justificación están en `docs/technical/US-32-administracion-gestion-inscripciones.md`, tabla "Decisiones de Diseño".
- **Un administrador puede cancelar inscripciones de eventos ya ocurridos**; un socio no — es la única rama del `alt` que depende de `IsAdministrator`, aparte del registro de auditoría.
- **Concurrencia**: aunque `Registration` no tiene `RowVersion` propio, `SaveChangesAsync` puede lanzar `DbUpdateConcurrencyException` si la fila fue borrada por otro proceso entre el `SELECT` y el `DELETE` (por ejemplo, si el evento se elimina — cascada — justo mientras el socio cancela su inscripción).
