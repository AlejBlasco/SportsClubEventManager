# Diagrama Entidad-Relación (ER)

Parte del catálogo de diagramas de la issue [#51](https://github.com/AlejBlasco/SportsClubEventManager/issues/51). Ver el índice completo en [`README.md`](README.md).

Modelo relacional final, **verificado directamente contra las 5 clases `IEntityTypeConfiguration<T>` de `src/SportsClubEventManager.Infrastructure/Persistence/Configurations/`** (no contra un diseño previo) el 2026-07-14. Complementa al `classDiagram` de dominio de `docs/architecture/architecture.md` §7: aquél muestra el modelo rico de dominio (comportamiento, invariantes); este muestra el esquema relacional real (columnas, claves, cardinalidad).

```mermaid
erDiagram
    USERS ||--o{ REGISTRATIONS : "UserId (Restrict)"
    EVENTS ||--o{ REGISTRATIONS : "EventId (Cascade)"
    USERS ||--o{ AUDITLOGS : "PerformedByUserId (NoAction)"
    EVENTS ||--o{ EVENTREMINDERNOTIFICATIONS : "EventId (Cascade)"

    USERS {
        guid Id PK
        string Name "max 200"
        string Gender "enum-as-string, max 50"
        string Email UK "max 256, único"
        string LicenseNumber "max 100, nullable"
        string LicenseCategory "max 50, nullable"
        string PasswordHash "max 500, nullable — null si solo usa OAuth2"
        string ExternalProviderId "max 256, nullable"
        string ProviderName "max 50, nullable"
        string RefreshToken "max 500, nullable"
        datetime RefreshTokenExpiryTime "nullable"
        bool IsActive "default true"
        datetime LastLoginAt "nullable"
        string Role "enum-as-string, max 50, default User"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }

    EVENTS {
        guid Id PK
        string Title "max 200"
        string Description "max 2000, nullable"
        datetime Date "indexado"
        string Location "max 500"
        int MaxCapacity
        bytes RowVersion "concurrencia optimista (IsRowVersion)"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }

    REGISTRATIONS {
        guid Id PK
        guid EventId FK
        guid UserId FK
        datetime RegistrationDate
        string Status "enum-as-string, max 50 (Registered/Cancelled)"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }

    AUDITLOGS {
        guid Id PK
        string Action "enum-as-string, max 50"
        guid PerformedByUserId FK "OnDelete: NoAction"
        guid TargetUserId "SIN FK — solo referencia histórica, ver nota"
        string TargetUserEmail "max 256, snapshot en el momento de la acción"
        datetime Timestamp "indexado"
        string Changes "nullable, JSON serializado"
        string IpAddress "max 45 (IPv6), nullable"
        string UserAgent "max 500, nullable"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }

    EVENTREMINDERNOTIFICATIONS {
        guid Id PK
        guid EventId FK "OnDelete: Cascade"
        int IntervalHours "parte de un índice único junto a EventId"
        datetime SentAtUtc
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }
```

## Notas — detalles que un ER "genérico" no capturaría

- **`AuditLogs.TargetUserId` no tiene restricción de clave foránea**, a propósito: `AuditLogConfiguration` solo declara la relación con `PerformedByUserId` (`OnDelete: NoAction`, para no perder el rastro de auditoría si se borra al administrador). `TargetUserId` es un `Guid` suelto — el usuario objetivo de la acción puede haber sido eliminado después (`DeleteUserCommand`), y por eso `TargetUserEmail` existe como *snapshot* de texto en el momento de la acción, no como algo recuperable vía `JOIN`.
- **Cardinalidad `Restrict` vs `Cascade` no es uniforme, y es intencional**: borrar un `Event` cancela en cascada sus `Registrations` y `EventReminderNotifications` (`OnDelete: Cascade` en ambas), pero borrar un `User` con inscripciones **está bloqueado a nivel de base de datos** (`OnDelete: Restrict` en `Registrations.UserId`) — coherente con que `DeleteUserCommandHandler` exige cancelar las inscripciones del usuario antes de poder borrarlo.
- **Tres índices únicos no evidentes en un ER básico**:
  - `Users.Email` — único global.
  - `(Users.ProviderName, Users.ExternalProviderId)` — único, pero con filtro (`HasFilter`) que solo aplica a filas donde ambos campos no son `NULL` — permite múltiples usuarios de login local (`ProviderName`/`ExternalProviderId` ambos `NULL`) sin violar la unicidad.
  - `(EventReminderNotifications.EventId, IntervalHours)` — único; barrera de base de datos contra reminders duplicados, redundante a propósito con la comprobación en `EventReminderBackgroundService`.
- **`Event.RowVersion`** es una columna de concurrencia optimista real de EF Core (`IsRowVersion()`), no un campo de negocio — se usa para detectar ediciones simultáneas de un evento por dos administradores (ver `UpdateEventCommandHandler`).
- `Event.CurrentRegistrations` e `Event.IsFull` (vistas en el `classDiagram` de dominio) **no son columnas** — son propiedades calculadas en memoria (`builder.Ignore(...)` en `EventConfiguration`), a partir de la colección `Registrations` ya cargada.
