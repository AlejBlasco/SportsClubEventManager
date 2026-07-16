# Diseño Técnico — API de Registro de Eventos

**Story:** US-7  
**Rama de trabajo:** feature/US-7-event-registration-api  
**Fecha:** 2026-06-24  
**Estado:** Implementado

---

## Descripción General

US-7 implementa un endpoint POST que permite a los usuarios registrarse en eventos deportivos a través de la API REST. El endpoint maneja solicitudes concurrentes de forma segura mediante control de concurrencia optimista (RowVersion), valida todas las reglas de negocio (capacidad del evento, duplicados, eventos pasados) y retorna respuestas estructuradas según RFC 7807 (ProblemDetails).

La implementación sigue la arquitectura limpia del proyecto (CQRS + MediatR), con validación en dos niveles: FluentValidation para la estructura de la solicitud y lógica de dominio para las reglas de negocio. Un mecanismo de retry automático de EF Core maneja los conflictos de concurrencia cuando múltiples usuarios intentan registrarse simultáneamente en los últimos lugares disponibles.

> **Nota de vigencia (2026-07-15):** El diseño original de esta página (`userId` recibido en el
> cuerpo de la solicitud y validado con FluentValidation, ver sección
> [Referencia de API](#referencia-de-api) más abajo) quedó obsoleto tras US-28
> (autorización basada en roles, 2026-07-06): `EventsController.RegisterForEvent` ya no acepta
> ningún cuerpo de solicitud — el `UserId` se extrae exclusivamente del claim de identidad del JWT
> autenticado, y el registro de un usuario en nombre de otro (antes reservado a administradores por
> esta misma ruta) se realiza ahora mediante `POST /api/admin/registrations`
> (`AdminRegistrationsController`), documentado en
> [issue-32](issue-32-administracion-gestion-inscripciones.md). Como consecuencia, un `EventId` con
> formato de GUID inválido en la ruta ya no produce 400 Bad Request: la restricción de ruta
> `{id:guid}` hace que el endpoint ni siquiera coincida, devolviendo 404 Not Found. Esta página se
> conserva como registro histórico del diseño original de US-7; el comportamiento vigente está en
> [issue-32](issue-32-administracion-gestion-inscripciones.md) y
> [docs/operations/inscripcion-eventos.md](../operations/inscripcion-eventos.md).

---

## Arquitectura

### Componentes Involucrados

```
┌─────────────────────────────────────────────────────────────┐
│ API Layer (Controllers)                                       │
├─────────────────────────────────────────────────────────────┤
│ • EventsController.RegisterForEvent()                         │
│   - Maneja HTTP POST /api/v1/events/{id}/register            │
│   - Dispatch del comando a través de ISender                 │
│   - Mapeo de excepciones de dominio a códigos HTTP           │
│   - Retorna Location header en respuesta 201                 │
└────────────┬────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────┐
│ Application Layer (CQRS)                                      │
├─────────────────────────────────────────────────────────────┤
│ • RegisterForEventCommand                                     │
│   - Modelo de comando con EventId, UserId                    │
│   - Implementa IRequest<RegistrationCreatedDto>              │
│                                                               │
│ • RegisterForEventCommandHandler                              │
│   - Carga evento con registros incluidos                      │
│   - Valida existencia del evento (404)                        │
│   - Valida que el evento no sea en el pasado (400)            │
│   - Valida que el usuario no esté registrado (409)            │
│   - Valida capacidad disponible (409)                         │
│   - Crea registro y persiste con SaveChangesAsync             │
│   - Maneja DbUpdateConcurrencyException (409)                 │
│                                                               │
│ • RegisterForEventCommandValidator                            │
│   - FluentValidation para EventId (GUID válido)              │
│   - FluentValidation para UserId (GUID válido)               │
│                                                               │
│ • RegistrationCreatedDto                                      │
│   - Contiene ID de registro, evento, usuario, timestamp      │
│   - Incluye EventDetailDto con capacidad actual              │
└────────────┬────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────┐
│ Domain Layer (Entidades & Excepciones)                        │
├─────────────────────────────────────────────────────────────┤
│ • Event (Entity)                                              │
│   - Propiedad RowVersion para concurrencia optimista         │
│   - MaxCapacity: límite de registros                          │
│   - Registrations: colección de registros del evento          │
│                                                               │
│ • Registration (Entity)                                       │
│   - EventId, UserId, RegistrationDate, Status                │
│   - Navegación a Event                                        │
│                                                               │
│ • Excepciones tipificadas:                                    │
│   - EntityNotFoundException: evento no existe                 │
│   - DuplicateRegistrationException: usuario ya registrado    │
│   - CapacityExceededException: evento lleno                   │
│   - DomainException: otras validaciones de negocio            │
└────────────┬────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────┐
│ Infrastructure Layer (EF Core & Database)                     │
├─────────────────────────────────────────────────────────────┤
│ • AppDbContext                                                │
│   - DbSet<Event>, DbSet<Registration>                        │
│   - Event.RowVersion configurada como IsRowVersion()         │
│                                                               │
│ • Migration: 20260624120000_AddEventConcurrencyToken         │
│   - Agrega columna rowversion a tabla Events                 │
│                                                               │
│ • SQL Server Database                                         │
│   - Gestiona automáticamente RowVersion                       │
│   - Aislamiento de transacción: ReadCommitted (default)       │
└─────────────────────────────────────────────────────────────┘
```

### Flujo de Datos

**Flujo exitoso — Registro creado:**

1. Cliente HTTP: `POST /api/v1/events/{eventId}/register` con cuerpo `{ "userId": "guid" }`
2. EventsController recibe solicitud, valida ruta GUID
3. Crea RegisterForEventCommand(eventId, userId)
4. ISender.Send() envía a MediatR pipeline
5. ValidationBehavior ejecuta RegisterForEventCommandValidator
6. Validador verifica GUIDs válidos
7. RegisterForEventCommandHandler.Handle() ejecuta lógica:
   - Lee Event + Registrations desde BD (Include)
   - Verifica: existe, no está en pasado, usuario no registrado, hay capacidad
   - Crea Registration con status = Registered
   - context.Registrations.Add()
   - SaveChangesAsync() confirma transacción
   - BD verifica RowVersion no ha cambiado, actualiza registro
8. Handler retorna RegistrationCreatedDto con detalles completos del evento
9. Controller retorna 201 Created con Location header: `/api/v1/registrations/{registrationId}`

**Flujo de error — Evento no existe:**

1-6. Igual al anterior
7. Handler: FirstOrDefaultAsync retorna null
8. Lanza EntityNotFoundException
9. Controller captura, retorna 404 Not Found + ProblemDetails

**Flujo de error — Registro duplicado:**

1-7. Handler carga evento, encuentra registración existente
8. Lanza DuplicateRegistrationException
9. Controller captura, retorna 409 Conflict + ProblemDetails

**Flujo de error — Congestión concurrente:**

1. Dos usuarios simultáneamente: POST .../register para los últimos 2 lugares del evento
2-6. Ambos requests entran a la validación
7a. Primer request ejecuta Handle, crea registración, SaveChangesAsync() éxito
7b. Segundo request ejecuta Handle, event.RowVersion es diferente, SaveChangesAsync() lanza DbUpdateConcurrencyException
8b. Handler captura, lanza DomainException("Event capacity was reached...")
9b. Controller captura, retorna 409 Conflict

### Decisiones de Diseño

**1. Control de Concurrencia: Optimista con RowVersion**

- **Alternativa rechazada:** Bloqueo pesimista (SELECT FOR UPDATE)
  - Motivo: Menos escalable, requiere SQL directo, complejidad aumentada
- **Seleccionado:** RowVersion (timestamp)
  - Ventajas: Automático en SQL Server, no bloquea, escalable
  - Implementación: `builder.Property(e => e.RowVersion).IsRowVersion()`
  - Comportamiento: EF Core detecta cambios de RowVersion y lanza DbUpdateConcurrencyException

**2. Validación en Dos Niveles**

- **FluentValidation** (RegisterForEventCommandValidator)
  - Valida estructura: GUIDs válidos, campos presentes
  - Retorna 400 Bad Request si falla
  - Ejecutado en MediatR ValidationBehavior
  
- **Lógica de Dominio** (en handler)
  - Valida reglas de negocio: existe evento, no está en pasado, no duplicado, hay capacidad
  - Retorna 404, 400, o 409 según el error
  - Ejecutado dentro de transacción

**3. Enriquecimiento de Respuesta**

- **RegistrationCreatedDto incluye EventDetailDto completo**
  - Ventaja: Cliente no necesita hacer otra llamada GET para detalles
  - Contiene: título, descripción, ubicación, capacidad, registros actuales, lugares disponibles
  - Mantiene estado actualizado: capacidad recalculada después de la creación

**4. Re-registro después de cancelación permitido**

- **Decisión:** User puede registrarse de nuevo si previamente canceló
- **Implementación:** Validación filtra por `Status != Cancelled`
  - Solo cuenta registraciones activas (Registered, Waitlisted) para duplicados
  - Registraciones canceladas no impiden nuevos registros

**5. Sin autenticación en Sprint 1 (riesgo aceptado)**

- **Documentado:** Requirements Analysis, sección Risks
- **Motivo:** MVP rápido, ambiente interno confiable
- **Sprint 2:** Autenticación JWT, userId derivado de claims

---

## Referencia de API

### POST /api/v1/events/{id}/register

**Descripción:** Registra un usuario para un evento específico.

**Autenticación:** Ninguna (Sprint 1)  
**Autorización:** N/A

**Parámetros de ruta:**
- `id` (GUID, obligatorio): Identificador único del evento

**Cuerpo de solicitud:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Validaciones de solicitud:**
- `userId` debe ser GUID válido (no vacío, formato correcto)
- `userId` no puede ser nulo

**Respuesta 201 Created:**
```json
{
  "registrationId": "8c9e6f42-1234-5678-abcd-ef1234567890",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "registeredAt": "2026-06-24T14:30:00Z",
  "status": "Registered",
  "event": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Torneo de Básquetbol",
    "description": "Competencia regional de baloncesto",
    "date": "2026-07-15T19:00:00Z",
    "location": "Cancha Municipal Centro",
    "maxCapacity": 50,
    "currentRegistrations": 25,
    "availableSlots": 25,
    "isFullyBooked": false
  }
}
```

**Headers de respuesta:**
- `Location: /api/v1/registrations/{registrationId}` — URI del recurso creado

**Respuestas de error:**

| Status | Título | Detalle | Cuándo |
|--------|--------|---------|--------|
| 400 Bad Request | Invalid registration request | "Cannot register for events that have already occurred." | Evento con fecha en pasado |
| 400 Bad Request | Invalid registration request | Mensaje de validación FluentValidation | EventId o UserId inválidos |
| 404 Not Found | Event not found | "Event with identifier '{eventId}' does not exist." | Evento no existe |
| 409 Conflict | Duplicate registration | "User is already registered for this event with status 'Registered'." | Usuario ya registrado (activo) |
| 409 Conflict | Duplicate registration | "User is already registered for this event with status 'Waitlisted'." | Usuario en waitlist |
| 409 Conflict | Capacity exceeded | "Event has reached maximum capacity." | Evento lleno o concurrencia |
| 500 Internal Server Error | Unexpected error | Detalles del error | Error de aplicación |

**Códigos de error de validación (FluentValidation):**
- `InvalidEventId` — EventId está vacío
- `InvalidUserId` — UserId está vacío

---

## Cambios de Configuración

### Claves de appsettings.json

**Ninguna nueva.** La implementación no requiere nuevas claves de configuración.

- Conexión a SQL Server: ya configurada en US-3
- MediatR: ya registrado en dependency injection
- Validation: ya configurado en pipeline

### Secretos / Azure Key Vault

**Ninguno requerido.**

---

## Cambios de Base de Datos

### Migración EF Core

**Nombre:** `20260624120000_AddEventConcurrencyToken`

**Cambios:**
- Agrega columna `RowVersion` de tipo `rowversion` a tabla `Events`
- Configuración EF: `builder.Property(e => e.RowVersion).IsRowVersion()`
- Impacto de datos: Ninguno (SQL Server auto-gestiona la columna)

**Comando para aplicar:**
```bash
dotnet ef database update \
  --project src/SportsClubEventManager.Infrastructure/SportsClubEventManager.Infrastructure.csproj \
  --startup-project src/SportsClubEventManager.Api/SportsClubEventManager.Api.csproj
```

### Cambios de esquema

**Tabla Events:**
```sql
ALTER TABLE Events ADD [RowVersion] rowversion
```

**Tabla Registrations:** Sin cambios (ya existe desde US-3)

**Tabla Users:** No aplicable (Sprint 1 sin autenticación)

---

## Dependencias Agregadas

| Paquete | Versión | Propósito |
|---------|---------|----------|
| (ninguno) | - | Todas las dependencias necesarias ya están instaladas |

**Paquetes ya disponibles:**
- MediatR 12.x+ (command/query handling)
- FluentValidation 11.x+ (request validation)
- Microsoft.EntityFrameworkCore.SqlServer 10.x+ (database access)
- Microsoft.AspNetCore.Mvc (ASP.NET Core MVC)

---

## Testing

### Cobertura de Pruebas Unitarias

- **Total:** 76 tests
- **Pasadas:** 76 (100%)
- **Cobertura Application layer:** 98.11% (8.11pp por encima del mínimo 90%)
- **Tests de US-7:** 16 tests específicos
  - Handler tests: 10 (validación, duplicados, capacidad, concurrencia)
  - Validator tests: 6 (GUIDs válidos, casos vacíos)

**Brecha de cobertura:** 1.89% en handler (líneas 78-82: excepción de concurrencia)
- Motivo: Base de datos en memoria no soporta RowVersion
- Mitigación: Validado en integration tests con TestContainers (Docker requerido)

### Escenarios de Pruebas de Integración

- **Implementados:** 10 tests de integración (compilados, listos para ejecutar)
- **Estado:** Saltados en esta ejecución (Docker no disponible para TestContainers)
- **Escenarios cubiertos:**
  1. Registro exitoso retorna 201
  2. Evento no encontrado retorna 404
  3. Usuario ya registrado retorna 409
  4. Evento lleno retorna 409
  5. Evento en pasado retorna 400
  6. UserId inválido retorna 400
  7. Registración duplicada en waitlist retorna 409
  8. Re-registro después de cancelación exitoso
  9. Conflicto de concurrencia manejado
  10. Capacidad recalculada correctamente

---

## Limitaciones Conocidas

### Sprint 1

1. **Sin autenticación:** UserId confiado desde cuerpo de solicitud
   - Riesgo: Usuario puede registrar a otro usuario
   - Mitigación: Ambiente interno confiable
   - Sprint 2: JWT autenticación

2. **Sin rate limiting:** Usuario puede registrarse múltiples veces (antes de validación)
   - Mitigación: Validación de duplicados en BD
   - Sprint 2: Middleware de rate limiting

3. **Sin notificaciones:** Usuario no recibe confirmación por email
   - Sprint 2: Integración con servicio de emails

4. **Sin lista de espera:** No hay soporte para registros en espera cuando evento lleno
   - Sprint 3: Registro en waitlist con promoción automática

5. **Integration tests requieren Docker:**
   - Limitación: TestContainers necesita Docker daemon
   - Workaround: Ejecutar tests localmente con Docker Desktop o en CI/CD

---

## Consideraciones de Seguridad

### Control de Acceso

- ✅ Validación de entrada (FluentValidation + domain logic)
- ✅ Uso de EF Core parameterizado (sin SQL injection)
- ⚠️ Sin autenticación (aceptado para Sprint 1)
- ⚠️ Sin autorización por roles (Sprint 2)

### Manejo de Excepciones

- ⚠️ Mensajes de excepción expuestos en respuestas API
  - Riesgo: Si excepciones se enriquecen con detalles internos, se expondrían
  - Recomendación: Mapear excepciones a mensajes de usuario controlados (Sprint 2)

### Concurrencia

- ✅ RowVersion previene overbooking
- ✅ DbUpdateConcurrencyException capturada y mapeada a 409
- ✅ No hay deadlocks documentados (Registrations es tabla simple)

### Datos Sensibles

- ✅ No se loguean datos sensibles
- ✅ No se exponen en errores
- ✅ No se guardan en caché

---

## Patrones Arquitectónicos

### CQRS con MediatR

Toda la lógica de negocio se implementa como Command + Handler:

```
HTTP POST /api/v1/events/{id}/register
    ↓
EventsController.RegisterForEvent(id, request)
    ↓
ISender.Send(RegisterForEventCommand)
    ↓
MediatR Pipeline:
  1. ValidationBehavior → RegisterForEventCommandValidator
  2. (custom behaviors as needed)
    ↓
RegisterForEventCommandHandler.Handle()
    ↓
AppDbContext.SaveChangesAsync()
    ↓
return RegistrationCreatedDto
    ↓
Controller mapea a 201 Created + Location header
```

### Inyección de Dependencias

Registrado en `ApplicationDependencyInjection.cs`:

```csharp
// Commands
services.AddScoped<IRequestHandler<RegisterForEventCommand, RegistrationCreatedDto>, 
    RegisterForEventCommandHandler>();

// Validators
services.AddScoped<IValidator<RegisterForEventCommand>, 
    RegisterForEventCommandValidator>();
```

### Límites de Transacción

- **Inicio:** Cuando se llama `Handle()` en handler
- **Fin:** Cuando `SaveChangesAsync()` completa o lanza excepción
- **Scope:** Una solicitud = una transacción
- **Rollback:** Automático si cualquier validación falla

---

## Roadmap Sprint 2+

### Autenticación (Sprint 2 — Planificado)

```csharp
[Authorize]
[HttpPost("{id:guid}/register")]
public async Task<ActionResult<RegistrationCreatedDto>> RegisterForEvent(
    Guid id,
    CancellationToken cancellationToken)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // Validar que userId matches request, o es admin
}
```

### Notificaciones (Sprint 2)

```csharp
var notification = new RegistrationConfirmedNotification(registration.Id, userId, eventEntity.Title);
await _mediator.Publish(notification, cancellationToken);
```

### Rate Limiting (Sprint 2)

```csharp
[RateLimit("registration", maxRequests: 10, window: TimeSpan.FromMinutes(1))]
public async Task<ActionResult<RegistrationCreatedDto>> RegisterForEvent(...)
```

### Lista de Espera (Sprint 3)

```csharp
if (activeRegistrationsCount >= eventEntity.MaxCapacity)
{
    registration.Status = RegistrationStatus.Waitlisted;
    // Mantener en BD, no lanzar error
}
```

---

## Archivos Relevantes

### Creados

- `src/SportsClubEventManager.Application/Events/Commands/RegisterForEvent/RegisterForEventCommand.cs`
- `src/SportsClubEventManager.Application/Events/Commands/RegisterForEvent/RegisterForEventCommandHandler.cs`
- `src/SportsClubEventManager.Application/Events/Commands/RegisterForEvent/RegisterForEventCommandValidator.cs`
- `src/SportsClubEventManager.Shared/DTOs/RegistrationCreatedDto.cs`
- `src/SportsClubEventManager.Api/Models/RegisterForEventRequest.cs`
- `src/SportsClubEventManager.Infrastructure/Migrations/20260624120000_AddEventConcurrencyToken.cs`
- `src/SportsClubEventManager.Domain/Exceptions/EntityNotFoundException.cs`

### Modificados

- `src/SportsClubEventManager.Domain/Entities/Event.cs` (+ RowVersion)
- `src/SportsClubEventManager.Infrastructure/Persistence/Configurations/EventConfiguration.cs` (+ IsRowVersion)
- `src/SportsClubEventManager.Api/Controllers/EventsController.cs` (+ endpoint RegisterForEvent)

---

**Documento generado:** 2026-06-24  
**Agente:** Documentation Agent  
**Idioma:** Español (es)
