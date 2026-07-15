# Diseño Técnico — Gestión de Usuarios (US-30)

**Historia:** US-30  
**Rama de trabajo:** feature/US-30-user-management  
**Fecha:** 2026-07-07  
**Estado:** Implementado  

---

## Resumen Ejecutivo

US-30 implementa capacidades completas de gestión de usuarios para administradores del sistema. Los administradores pueden visualizar una lista paginada de todos los usuarios con filtros avanzados (rol, estado, búsqueda), ver y editar detalles de usuarios, asignar roles, activar/desactivar cuentas, eliminar usuarios con eliminación en cascada de registros relacionados, y todas las acciones se registran en un registro de auditoría completo con dirección IP y User-Agent.

La implementación introduce dos nuevos patrones reutilizables para todo el sistema: **PagedResult<T>** (paginación genérica) y **AuditService** (registro de auditoría transaccional).

---

## Arquitectura

### Capas afectadas (Clean Architecture)

```
┌─────────────────────────────────────────────────────┐
│         Presentation Layer (Web)                    │
│  - UserManagement.razor (page)                      │
│  - UserDetailsModal.razor (modal)                   │
│  - UserManagementService (HTTP client)              │
└──────────────┬──────────────────────────────────────┘
               │ (MediatR commands/queries via HTTP)
┌──────────────▼──────────────────────────────────────┐
│         API Layer (Controllers)                     │
│  - UsersController (extended with 5 admin endpoints)│
└──────────────┬──────────────────────────────────────┘
               │ (IMediator.Send)
┌──────────────▼──────────────────────────────────────┐
│    Application Layer (CQRS)                         │
│  ├─ Queries:                                        │
│  │  - GetAllUsersQuery + QueryHandler               │
│  │  - GetUserByIdQuery + QueryHandler               │
│  ├─ Commands:                                       │
│  │  - UpdateUserAsAdminCommand + CommandHandler     │
│  │  - UpdateUserStatusCommand + CommandHandler      │
│  │  - UpdateUserRoleCommand + CommandHandler        │
│  │  - DeleteUserCommand + CommandHandler            │
│  └─ Validators (FluentValidation)                   │
└──────────────┬──────────────────────────────────────┘
               │ (DbContext + IAuditService)
┌──────────────▼──────────────────────────────────────┐
│   Infrastructure Layer                              │
│  - AppDbContext (User, AuditLog DbSets)            │
│  - AuditService (implementación IAuditService)      │
│  - AuditLogConfiguration (EF Core mapping)          │
│  - Migration: AddAuditLogTable                      │
└──────────────┬──────────────────────────────────────┘
               │
┌──────────────▼──────────────────────────────────────┐
│    Domain Layer                                     │
│  - User entity (existente, extendido via migrations)│
│  - AuditLog entity (nueva)                          │
│  - AuditAction enum (nueva)                         │
│  - Role enum (existente)                            │
└─────────────────────────────────────────────────────┘
```

### Entidades de Dominio

#### AuditLog (nueva)
```csharp
public class AuditLog : BaseEntity
{
    public AuditAction Action { get; set; }              // Tipo de acción
    public Guid PerformedByUserId { get; set; }          // Admin que ejecutó la acción
    public User PerformedByUser { get; set; }            // Navegación
    public Guid TargetUserId { get; set; }               // Usuario destino
    public string TargetUserEmail { get; set; }          // Email capturado en el momento
    public DateTime Timestamp { get; set; }              // Hora exacta
    public string? Changes { get; set; }                 // JSON con valores antiguos/nuevos
    public string? IpAddress { get; set; }               // Dirección IP del admin
    public string? UserAgent { get; set; }               // User-Agent del navegador
}
```

#### AuditAction (nuevo enum)
```csharp
public enum AuditAction
{
    UserUpdated = 0,        // Información de usuario actualizada
    UserDeactivated = 1,    // Cuenta desactivada
    UserActivated = 2,      // Cuenta activada
    RoleAssigned = 3,       // Rol asignado
    RoleRemoved = 4,        // Rol removido
    UserDeleted = 5         // Usuario eliminado
}
```

---

## Flujo de Datos

### 1. Listar Usuarios con Paginación y Filtros

```
GET /api/users/admin?pageNumber=1&pageSize=20&roleFilter=Administrator&isActiveFilter=true&searchText=john&sortBy=Name&sortOrder=asc

┌─────────────────────────────────────────────────────────┐
│ UsersController.GetAllUsers                              │
│ - Extrae parámetros de consulta                          │
│ - Crea GetAllUsersQuery                                  │
└──────┬──────────────────────────────────────────────────┘
       │ mediator.Send(query)
┌──────▼──────────────────────────────────────────────────┐
│ GetAllUsersQueryValidator                                │
│ - Valida: pageNumber > 0, pageSize 1-100                │
│ - Si la validación falla → FluentValidationException    │
└──────┬──────────────────────────────────────────────────┘
       │ si es válido
┌──────▼──────────────────────────────────────────────────┐
│ GetAllUsersQueryHandler                                  │
│ 1. Base: DbContext.Users.AsQueryable()                  │
│ 2. Aplicar filtro de rol: WHERE u.Role == roleFilter    │
│ 3. Aplicar filtro estado: WHERE u.IsActive == filter    │
│ 4. Aplicar búsqueda: WHERE u.Name.Contains() OR         │
│                           u.Email.Contains()            │
│ 5. Ordenar: OrderBy(sortBy, sortOrder)                  │
│ 6. Paginar: Skip((page-1)*size).Take(size)              │
│ 7. Proyectar: .Select(u => new UserListDto { ... })    │
│ 8. Ejecutar: ToListAsync()                              │
│ 9. Contar total: CountAsync()                           │
│ 10. Retornar: PagedResult<UserListDto>                  │
└──────┬──────────────────────────────────────────────────┘
       │
┌──────▼──────────────────────────────────────────────────┐
│ Response 200 OK                                          │
│ {                                                        │
│   "items": [ { id, name, email, role, isActive, ... }],│
│   "pageNumber": 1,                                       │
│   "pageSize": 20,                                        │
│   "totalCount": 143,                                     │
│   "totalPages": 8,                                       │
│   "hasNextPage": true,                                   │
│   "hasPreviousPage": false                               │
│ }                                                        │
└─────────────────────────────────────────────────────────┘
```

### 2. Editar Usuario (con Auditoría)

```
PUT /api/users/admin/{userId}
{
  "name": "John Updated",
  "email": "newemail@example.com"
}

┌─────────────────────────────────────────────────────────┐
│ UsersController.UpdateUser                               │
│ - Extrae ID del admin del claim ClaimTypes.NameIdentifier│
│ - Extrae IP y User-Agent del HttpContext                │
│ - Crea UpdateUserAsAdminCommand                          │
└──────┬──────────────────────────────────────────────────┘
       │ mediator.Send(command)
┌──────▼──────────────────────────────────────────────────┐
│ UpdateUserAsAdminCommandValidator                        │
│ - Valida formato email                                   │
│ - Valida campos requeridos                               │
└──────┬──────────────────────────────────────────────────┘
       │
┌──────▼──────────────────────────────────────────────────┐
│ UpdateUserAsAdminCommandHandler                          │
│ 1. Iniciar transacción DbContext                         │
│ 2. Recuperar user = DbContext.Users.Find(targetUserId)  │
│ 3. Si no existe → throw KeyNotFoundException             │
│ 4. Validar email único:                                  │
│    existingUser = DbContext.Users.Where(                │
│      u => u.Email == newEmail && u.Id != userId)       │
│    Si existe → throw InvalidOperationException           │
│ 5. Capturar cambios: oldEmail, newEmail, etc.           │
│ 6. Actualizar user.Name, user.Email, etc.               │
│ 7. Llamar auditService.LogAsync():                       │
│    - AuditAction = UserUpdated                           │
│    - Changes JSON con { from: old, to: new }            │
│    - IpAddress, UserAgent capturados                     │
│ 8. DbContext.SaveChangesAsync()                          │
│ 9. Si excepción → rollback transacción                   │
│ 10. Proyectar a UserDetailsDto                           │
│ 11. Retornar                                             │
└──────┬──────────────────────────────────────────────────┘
       │
┌──────▼──────────────────────────────────────────────────┐
│ Database transaction completes atomically:               │
│ - User table updated                                     │
│ - AuditLogs table tiene nueva entrada                    │
│ - Ambas operaciones se persisten o ambas se revierten    │
└──────┬──────────────────────────────────────────────────┘
       │
┌──────▼──────────────────────────────────────────────────┐
│ Response 200 OK                                          │
│ {                                                        │
│   "id": "...",                                           │
│   "name": "John Updated",                                │
│   "email": "newemail@example.com",                       │
│   "isActive": true,                                      │
│   ...                                                    │
│ }                                                        │
└─────────────────────────────────────────────────────────┘
```

### 3. Eliminar Usuario (con Protección del Último Admin)

```
DELETE /api/users/admin/{userId}

┌─────────────────────────────────────────────────────────┐
│ UsersController.DeleteUser                               │
│ - Extrae ID del admin                                    │
│ - Crea DeleteUserCommand                                 │
└──────┬──────────────────────────────────────────────────┘
       │
┌──────▼──────────────────────────────────────────────────┐
│ DeleteUserCommandHandler                                 │
│ 1. Iniciar transacción                                   │
│ 2. Recuperar user = DbContext.Users.Find(userId)        │
│ 3. Si no existe → throw KeyNotFoundException             │
│ 4. Validar último admin:                                 │
│    adminCount = DbContext.Users.Count(                  │
│      u => u.Role == Role.Administrator)                 │
│    Si user.Role == Administrator && adminCount == 1     │
│    → throw InvalidOperationException                     │
│      "Cannot remove the last Administrator"              │
│ 5. Cascada: Eliminar registraciones del usuario          │
│    registrations = DbContext.Registrations.Where(        │
│      r => r.UserId == userId)                           │
│    DbContext.Registrations.RemoveRange(registrations)    │
│ 6. Crear entrada auditoría (email capturado pre-delete) │
│    auditLog = new AuditLog {                             │
│      Action = AuditAction.UserDeleted,                   │
│      TargetUserId = userId,                              │
│      TargetUserEmail = user.Email,  // importante!      │
│      ...                                                  │
│    }                                                      │
│ 7. DbContext.AuditLogs.Add(auditLog)                     │
│ 8. DbContext.Users.Remove(user)                          │
│ 9. DbContext.SaveChangesAsync()                          │
│ 10. Commit transacción                                   │
└──────┬──────────────────────────────────────────────────┘
       │
┌──────▼──────────────────────────────────────────────────┐
│ Response 204 No Content                                  │
│ (Usuario y registraciones eliminados, auditoría grabada)│
└─────────────────────────────────────────────────────────┘
```

---

## Decisiones de Diseño y Alternativas

### 1. Eliminación Física vs Lógica

**Decisión (Gate 2):** Eliminación física (hard delete)

**Justificación:**
- Más simple: `DbContext.Users.Remove(user)` sin filtros globales
- GDPR right-to-erasure: cumple con requisitos de privacidad
- La auditoría preserva el email en `AuditLog.TargetUserEmail` antes de la eliminación
- Menos complejidad en consultas (sin filtros `WHERE NOT IsDeleted`)

**Alternativa rechazada (soft delete):**
- ✗ Requeriría `IsDeleted` flag en User entity
- ✗ Cada consulta debe incluir `WHERE NOT IsDeleted`
- ✗ Validación de email único se vuelve compleja
- ✗ Aumenta tamaño de BD indefinidamente

### 2. Cascada de Eliminación

**Decisión (Gate 2):** CASCADE DELETE de Registrations

**Lógica:**
```
WHEN User is deleted:
  → DELETE FROM Registrations WHERE UserId = @targetUserId
  → DELETE FROM Users WHERE Id = @targetUserId
  → INSERT INTO AuditLogs (Action = UserDeleted, TargetUserEmail = email_captured_before_delete)
```

**Justificación:**
- Un usuario deletreado no puede tener registraciones activas
- Mantiene integridad referencial
- Evita registraciones "huérfanas"
- Registrada en auditoría con identificación del usuario

### 3. Manejo de Concurrencia

**Decisión:** Last-write-wins (aceptar para MVP)

**Alternativas rechazadas:**
- ✗ Optimistic concurrency (RowVersion) — requiere migración adicional
- ✗ ETags en API — complejidad en cliente Blazor

**Justificación:**
- Baja probabilidad de ediciones simultáneas del mismo usuario
- Puede mejorarse en sprint futuro si hay conflictos reales en producción

### 4. Validación de Email al Cambiar

**Decisión:** No enviar email de verificación (override de admin confiable)

**Justificación:**
- Acción de administrador es acción privilegiada y confiable
- Simplifica flujo: cambio inmediato sin validación
- Usuario puede verificar por sí mismo si es incorrecto

---

## Patrón Paginación: PagedResult<T>

### Definición

```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int PageNumber { get; set; }           // 1-based
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

### Uso en Query Handler

```csharp
var users = await DbContext.Users
    .AsNoTracking()
    .Where(/* filters */)
    .OrderBy(/* sortBy */)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .Select(u => new UserListDto { /* mapping */ })
    .ToListAsync(cancellationToken);

var totalCount = await DbContext.Users
    .AsNoTracking()
    .Where(/* mismos filters */)
    .CountAsync(cancellationToken);

return new PagedResult<UserListDto>
{
    Items = users,
    PageNumber = pageNumber,
    PageSize = pageSize,
    TotalCount = totalCount
};
```

### Límites (Gate 2)

- Página mínima: 1
- Página máxima: sin límite (validar en cliente)
- PageSize mínimo: 1
- PageSize máximo: 100
- Validación en `GetAllUsersQueryValidator`

### Reutilización Futura

Patrón `PagedResult<T>` es genérico y será reutilizado en:
- Event list pagination (US-31)
- Registration list pagination (US-32)
- Audit log viewer (historia futura)

---

## Patrón Auditoría: AuditService + AuditLog

### Arquitectura

```
Command Handler
    ↓
Call IAuditService.LogAsync()
    ↓
AuditService
    ├─ Crear instancia AuditLog
    ├─ Serializar cambios a JSON
    ├─ Capturar IP y User-Agent
    └─ DbContext.AuditLogs.Add()
    ↓
DbContext.SaveChangesAsync()  ← Transacción única
    ├─ User entity updated/deleted
    ├─ Registrations cascade deleted (si aplica)
    └─ AuditLog entry persisted
```

### Interfaz IAuditService

```csharp
public interface IAuditService
{
    Task LogAsync(
        AuditAction action,
        Guid performedByUserId,
        Guid targetUserId,
        string? targetUserEmail = null,
        object? changes = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}
```

### Implementación

```csharp
public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly IJsonSerializer _serializer;

    public async Task LogAsync(
        AuditAction action,
        Guid performedByUserId,
        Guid targetUserId,
        string? targetUserEmail = null,
        object? changes = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            PerformedByUserId = performedByUserId,
            TargetUserId = targetUserId,
            TargetUserEmail = targetUserEmail ?? string.Empty,
            Timestamp = DateTime.UtcNow,
            Changes = changes != null ? _serializer.Serialize(changes) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        // SaveChangesAsync es responsabilidad del command handler
    }
}
```

### Garantías Transaccionales

```csharp
// En command handler:
try
{
    // Modificar entidad
    user.Email = request.Email;

    // Crear auditoría (antes de SaveChanges)
    await _auditService.LogAsync(
        AuditAction.UserUpdated,
        adminUserId,
        userId,
        changes: new { email = new { from = oldEmail, to = newEmail } },
        ipAddress: request.IpAddress,
        userAgent: request.UserAgent,
        cancellationToken);

    // Una sola llamada a SaveChanges = una sola transacción
    await _context.SaveChangesAsync(cancellationToken);

    // Si SaveChanges falla:
    // → Cambios en User entity se revierten
    // → AuditLog.Add() se revierte
    // → La operación completa falla atómicamente
}
catch (DbUpdateException ex)
{
    // La transacción se revierte automáticamente
    throw new InvalidOperationException("Update failed", ex);
}
```

---

## Referencia de API

### GET /api/users/admin

**Descripción:** Obtiene lista paginada, filtrada y ordenada de todos los usuarios (solo administrador)

**Autenticación:** Requerida (JWT Bearer)

**Autorización:** `Roles = "Administrator"`

**Parámetros de consulta:**
```
pageNumber=1              (int, ≥ 1, default=1)
pageSize=20               (int, 1-100, default=20)
roleFilter=Administrator  (Role enum, nullable)
isActiveFilter=true       (bool, nullable)
searchText=john           (string, nullable, búsqueda en name/email)
sortBy=Name               (string, default="Name")
sortOrder=asc             (string, "asc"/"desc")
```

**Respuesta 200:**
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "John Doe",
      "email": "john@example.com",
      "role": "Administrator",
      "isActive": true,
      "lastLoginAt": "2026-07-07T10:30:00Z",
      "createdAt": "2026-01-15T08:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 143,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**Errores:**
| Estado | Cuándo |
|--------|--------|
| 400 | Validación fallida (pageNumber < 1, pageSize > 100) |
| 401 | No autenticado |
| 403 | No es administrador |

---

### GET /api/users/admin/{id}

**Descripción:** Obtiene información detallada de un usuario específico (solo administrador)

**Autenticación:** Requerida

**Autorización:** `Roles = "Administrator"`

**Parámetro de ruta:**
```
id (Guid)  - ID del usuario a consultar
```

**Respuesta 200:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "John Doe",
  "email": "john@example.com",
  "gender": "Male",
  "licenseNumber": "DL123456",
  "licenseCategory": "A",
  "role": "Administrator",
  "isActive": true,
  "registrationCount": 5,
  "lastLoginAt": "2026-07-07T10:30:00Z",
  "createdAt": "2026-01-15T08:00:00Z"
}
```

**Errores:**
| Estado | Cuándo |
|--------|--------|
| 401 | No autenticado |
| 403 | No es administrador |
| 404 | Usuario no encontrado |

---

### PUT /api/users/admin/{id}

**Descripción:** Actualiza información de usuario (solo administrador)

**Autenticación:** Requerida

**Autorización:** `Roles = "Administrator"`

**Parámetro de ruta:**
```
id (Guid)  - ID del usuario a actualizar
```

**Cuerpo de solicitud:**
```json
{
  "name": "John Updated",
  "email": "newemail@example.com",
  "gender": "Male",
  "licenseNumber": "DL654321",
  "licenseCategory": "B"
}
```

**Respuesta 200:** UserDetailsDto actualizado (ver GET /api/users/admin/{id})

**Errores:**
| Estado | Cuándo |
|--------|--------|
| 400 | Email ya en uso / Formato email inválido |
| 401 | No autenticado |
| 403 | No es administrador |
| 404 | Usuario no encontrado |

**Auditoría:** Se registra AuditLog con Action = UserUpdated, Changes JSON captura cambios

---

### PUT /api/users/admin/{id}/status

**Descripción:** Activa o desactiva una cuenta de usuario

**Autenticación:** Requerida

**Autorización:** `Roles = "Administrator"`

**Parámetro de ruta:**
```
id (Guid)  - ID del usuario
```

**Cuerpo de solicitud:**
```json
{
  "isActive": false
}
```

**Respuesta 204:** Sin contenido

**Errores:**
| Estado | Cuándo |
|--------|--------|
| 401 | No autenticado |
| 403 | No es administrador |
| 404 | Usuario no encontrado |

**Auditoría:**
- Si `isActive=false`: AuditLog con Action = UserDeactivated
- Si `isActive=true`: AuditLog con Action = UserActivated

---

### PUT /api/users/admin/{id}/role

**Descripción:** Asigna o modifica el rol de un usuario

**Autenticación:** Requerida

**Autorización:** `Roles = "Administrator"`

**Parámetro de ruta:**
```
id (Guid)  - ID del usuario
```

**Cuerpo de solicitud:**
```json
{
  "role": "Administrator"
}
```

**Respuesta 204:** Sin contenido

**Errores:**
| Estado | Cuándo |
|--------|--------|
| 400 | Intento de remover último Administrator |
| 401 | No autenticado |
| 403 | No es administrador |
| 404 | Usuario no encontrado |

**Reglas de negocio críticas:**
- No se puede remover el rol Administrator si es el único usuario con ese rol
- Se genera excepción `InvalidOperationException` con mensaje: "Cannot remove the last Administrator from the system"

**Auditoría:**
- Si rol asignado: AuditLog con Action = RoleAssigned
- Si rol removido: AuditLog con Action = RoleRemoved

---

### DELETE /api/users/admin/{id}

**Descripción:** Elimina permanentemente una cuenta de usuario y sus registraciones asociadas (eliminación en cascada)

**Autenticación:** Requerida

**Autorización:** `Roles = "Administrator"`

**Parámetro de ruta:**
```
id (Guid)  - ID del usuario a eliminar
```

**Respuesta 204:** Sin contenido (usuario y sus registraciones eliminados)

**Errores:**
| Estado | Cuándo |
|--------|--------|
| 400 | Intento de eliminar último Administrator |
| 401 | No autenticado |
| 403 | No es administrador |
| 404 | Usuario no encontrado |

**Reglas de negocio críticas:**
- No se puede eliminar el único usuario Administrator
- Genera excepción `InvalidOperationException`: "Cannot delete the last Administrator from the system"
- Todas las Registrations del usuario se eliminan en cascada (CASCADE DELETE)
- El email del usuario se captura en AuditLog ANTES de la eliminación

**Auditoría:**
- AuditLog con Action = UserDeleted
- TargetUserEmail contiene el email en el momento de la eliminación (para preservar auditoría post-eliminación)
- Changes puede contener motivo de la eliminación (campo opcional)

---

## Cambios de Base de Datos

### Migración EF Core

**Nombre:** `20260707000000_AddAuditLogTable`

> **Corrección post-implementación (2026-07-08):** esta migración se creó manualmente sin su
> `.Designer.cs`, por lo que EF Core no la reconocía como migración de `AppDbContext` y nunca
> se llegó a aplicar (la Api/Web reportaban "database is already up to date" con la tabla
> `AuditLogs` inexistente). Ver la nota equivalente en
> `docs/technical/issue-28-autorizacion-basada-en-roles.md` para el detalle completo de la causa
> raíz y el fix (se generó el `.Designer.cs` faltante y se quitó el atributo `[Migration]`
> duplicado de la clase principal).

**Schema SQL generado:**
```sql
CREATE TABLE [AuditLogs] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_AuditLogs] PRIMARY KEY,
    [Action] NVARCHAR(50) NOT NULL,
    [PerformedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [TargetUserId] UNIQUEIDENTIFIER NOT NULL,
    [TargetUserEmail] NVARCHAR(256) NOT NULL,
    [Timestamp] DATETIME2 NOT NULL,
    [Changes] NVARCHAR(MAX) NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [UpdatedAt] DATETIME2 NOT NULL,
    CONSTRAINT [FK_AuditLogs_Users_PerformedByUserId] 
        FOREIGN KEY ([PerformedByUserId]) REFERENCES [Users] ([Id]) 
        ON DELETE NO ACTION,
    CONSTRAINT [FK_AuditLogs_Users_TargetUserId] 
        FOREIGN KEY ([TargetUserId]) REFERENCES [Users] ([Id]) 
        ON DELETE NO ACTION
);

CREATE INDEX [IX_AuditLogs_PerformedByUserId] ON [AuditLogs] ([PerformedByUserId]);
CREATE INDEX [IX_AuditLogs_TargetUserId] ON [AuditLogs] ([TargetUserId]);
CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs] ([Action]);
```

**Cambios en tablas existentes:**
- Ninguno. La tabla User ya existe con todas las columnas necesarias desde US-27/US-28.

**Índices añadidos:**
- `IX_AuditLogs_PerformedByUserId` — consultas de auditoría filtradas por administrador
- `IX_AuditLogs_TargetUserId` — auditoría filtrada por usuario destino
- `IX_AuditLogs_Timestamp` — ordenamiento temporal
- `IX_AuditLogs_Action` — filtrado por tipo de acción

**Restricciones de clave foránea:**
- `ON DELETE NO ACTION` en ambas FK — preserva integridad de auditoría incluso si User se elimina (nunca sucede en práctica, pero protege auditoría)

---

## Cambios de Configuración

**appsettings.json:**

No se requieren cambios. Los valores por defecto están codificados:
```
Pagination default pageSize = 20
Pagination max pageSize = 100
```

Si en el futuro se requiere externalización, puede añadirse:
```json
{
  "UserManagement": {
    "DefaultPageSize": 20,
    "MaxPageSize": 100
  }
}
```

---

## Dependencias de Paquetes

Ninguna nueva dependencia requerida. Todo ya está presente en el proyecto:

| Paquete | Versión | Uso |
|---------|---------|-----|
| MediatR | 12.4.1 | CQRS pattern |
| FluentValidation | 12.1.1 | Validación de commands/queries |
| Entity Framework Core | 10.0.1 | ORM y migraciones |
| JWT Bearer | 10.0.9 | Autenticación/autorización |

---

## Pruebas

### Cobertura Unitaria

**Objetivo:** 90%+  
**Resultado:** 90%+ (144/144 tests passed)

**Componentes cubiertos:**
- ✅ GetAllUsersQueryHandler — 26 tests (paginación, filtros, ordenamiento)
- ✅ GetAllUsersQueryValidator — 14 tests (validación de parámetros)
- ✅ GetUserByIdQueryHandler — 10 tests (recuperación y no-encontrado)
- ✅ UpdateUserAsAdminCommandHandler — 15 tests (actualización, email único, auditoría)
- ✅ UpdateUserStatusCommandHandler — 11 tests (activar/desactivar)
- ✅ UpdateUserRoleCommandHandler — 15 tests (**CRÍTICO:** Protección de último admin)
- ✅ DeleteUserCommandHandler — 15 tests (**CRÍTICO:** Eliminación cascada, último admin)

**Reglas de negocio críticas validadas:**
1. ✅ Protección de último Administrator (no se puede demoter/eliminar)
2. ✅ Cascada de eliminación de Registrations
3. ✅ Límites de paginación (default 20, max 100)
4. ✅ Integridad transaccional de auditoría
5. ✅ Unicidad de email
6. ✅ Captura de IP y User-Agent
7. ✅ Combinación de filtros (AND lógico)

### Pruebas de Integración

**Estado:** ⏭️ SKIPPED (decisión humana explícita por restricción de tiempo)

**Justificación:** Time constraint — testing y security review se realizarán manualmente fuera del flujo automatizado antes del despliegue a producción.

**Suite planeada (para ejecución manual):**
- AdminUserManagementIntegrationTests.cs con 9 escenarios:
  - Authorization enforcement (no-admin recibe 403)
  - CRUD operations end-to-end
  - Cascade deletion validation
  - Last-admin protection validation
  - Audit logging with real database
  - EF Core migration validation

**Nota:** Ciclo de integración fue abortado debido a problema de sincronización de snapshot de modelo EF (arreglable con `dotnet ef migrations add RegenerateSnapshot -f`). Código de prueba es válido y puede ser re-ejecutado una vez sincronizado el snapshot.

---

## Limitaciones Conocidas

1. **Concurrencia:** Last-write-wins (sin optimistic locking). Si dos admins editan el mismo usuario simultáneamente, los cambios del último prevalecen sin aviso.
   - **Mitigación futura:** Agregar RowVersion a User entity + HTTP 409 Conflict en handler

2. **Ediciones de Email sin Verificación:** Admin puede cambiar email de usuario sin verificación. Usuario no recibe confirmación.
   - **Mitigación:** Diseño intencional (admin es confiable); usuario puede verificar por sí mismo

3. **Cascade Delete Automático:** Todas las registraciones del usuario se eliminan junto con el usuario.
   - **Considera:** Para auditoría completa, la lista de registraciones eliminadas podría capturarse en Changes JSON (no implementado actualmente)

4. **Sin Operaciones en Lote:** No hay endpoints para activar/desactivar/eliminar múltiples usuarios de una vez.
   - **Recomendación:** Deferida a historia futura si se requiere

5. **Historial de Cambios Completo:** El JSON de Changes solo captura cambios recientes, no todo el historial de auditoría.
   - **Nota:** AuditLogs table preserva todas las entradas; solo se compactan por operación individual

---

## Consideraciones de Seguridad

### Autenticación y Autorización

✅ **Implementado:**
- Todos los endpoints requieren `[Authorize]`
- Endpoints admin requieren explícitamente `[Authorize(Roles = "Administrator")]`
- ID del admin se extrae de claims de JWT (ClaimTypes.NameIdentifier)
- IP y User-Agent se capturan desde HttpContext

**Nota:** Gate 3 (Security Review) fue saltado por decisión humana explícita. Antes del despliegue a producción, debe ejecutarse revisión manual de seguridad incluyendo:
- OWASP Top 10 (SQL injection via LINQ, XSS, CSRF, etc.)
- Dependency vulnerability check (dotnet audit)
- Authorization tests completos
- Input validation / output encoding

### Integridad de Auditoría

✅ **Implementado:**
- Email de usuario capturado EN EL MOMENTO de la acción (antes de eliminación)
- Auditoría transaccional (si AuditLog falla, el cambio se revierte)
- Foreign keys con ON DELETE NO ACTION (preserva auditoría)
- Índices en AuditLogs para queries eficientes

### Validaciones de Entrada

✅ **Implementado:**
- FluentValidation en todos los commands/queries
- Formato de email validado
- PageSize limitado a 1-100
- Campos requeridos validados

### Restricciones de Negocio

✅ **Implementado:**
- No se puede eliminar/demotar último Administrator
- Email debe ser único
- Self-modification permitido pero advertido en UI

---

## Ruta de Adopción Futura

Este story establece patrones reutilizables:

1. **PagedResult<T>** será usado por:
   - Event list pagination (US-31)
   - Registration list pagination (US-32)
   - Audit log viewer
   - Reportes

2. **AuditService + AuditLog** será usado por:
   - Event CRUD auditing (US-31)
   - Registration management auditing (US-32)
   - Compliance/forensics reports

3. **UpdateUserRoleCommand pattern** (validación de último admin) será base para:
   - Role hierarchy validation en contextos futuros
   - Permission escalation prevention

---

**Diseño técnico completado. Listo para revisión de seguridad manual e integración antes del despliegue a producción.**
