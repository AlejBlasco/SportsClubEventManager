# Diseño Técnico — API de Cancelación de Registros
**Historia:** US-8  
**Rama de trabajo:** features/us-8-registration-cancellation-api  
**Fecha:** 2026-06-25  
**Estado:** Implementado

---

## Resumen

Se ha implementado un nuevo endpoint DELETE en la API de eventos para permitir que los usuarios cancelen sus registros a eventos. Esta característica es complementaria al endpoint de registro (US-7) y completa el ciclo de vida de la gestión de registros del MVP.

La implementación utiliza una estrategia de eliminación dura (hard delete), removiendo completamente el registro de la base de datos. El diseño sigue patrones limpios de arquitectura y replica exactamente los mecanismos de manejo de concurrencia y validación del endpoint de registro existente.

> **Nota de vigencia (2026-07-15):** El diseño original de esta página (`userId` recibido en el
> cuerpo de la solicitud y validado con FluentValidation) quedó obsoleto tras US-28 (autorización
> basada en roles, 2026-07-06): `EventsController.CancelRegistration` ya no acepta ningún cuerpo de
> solicitud — el `UserId` se extrae exclusivamente del claim de identidad del JWT autenticado. La
> cancelación de la inscripción de otro usuario (capacidad de administrador) se realiza ahora
> mediante `DELETE /api/admin/registrations/{id}` (`AdminRegistrationsController`), documentado en
> [issue-32](issue-32-administracion-gestion-inscripciones.md). Como consecuencia, un `EventId` con
> formato de GUID inválido en la ruta ya no produce 400 Bad Request: la restricción de ruta
> `{id:guid}` hace que el endpoint ni siquiera coincida, devolviendo 404 Not Found. Esta página se
> conserva como registro histórico del diseño original de US-8; el comportamiento vigente está en
> [issue-32](issue-32-administracion-gestion-inscripciones.md) y
> [docs/operations/administracion-inscripciones.md](../operations/administracion-inscripciones.md).

---

## Arquitectura

### Componentes Involucrados

**Capas de Clean Architecture:**

| Capa | Componentes | Responsabilidad |
|------|------------|-----------------|
| **Domain** | Entidades: `Event`, `Registration` | Sin cambios — se utilizan las entidades existentes |
| **Application** | `CancelRegistrationCommand`, `CancelRegistrationCommandHandler`, `CancelRegistrationCommandValidator` | Orquestación de casos de uso y lógica de validación |
| **Infrastructure** | `IApplicationDbContext` | Acceso a datos a través de Entity Framework Core |
| **API** | `EventsController`, `CancelRegistrationRequest` | Presentación HTTP y validación de DTOs |

### Flujo de Datos

```
Cliente HTTP
    ↓
DELETE /api/v1/events/{eventId}/register
    ↓
EventsController.CancelRegistration()
    ├─ Deserializa CancelRegistrationRequest
    └─ Envía CancelRegistrationCommand vía MediatR
        ↓
    CancelRegistrationCommandValidator
        ├─ Valida EventId (debe ser un GUID válido)
        └─ Valida UserId (debe ser un GUID válido)
        ↓
    CancelRegistrationCommandHandler.Handle()
        ├─ Consulta la base de datos: Event con Registrations
        ├─ Valida que el evento existe
        ├─ Valida que la fecha del evento no ha pasado
        ├─ Busca el registro activo del usuario
        ├─ Valida que existe un registro activo
        ├─ Elimina el registro (hard delete)
        ├─ Guarda cambios en la BD con control de concurrencia
        └─ Maneja conflictos de concurrencia
        ↓
    EventsController
        ├─ Captura excepciones y mapea a HTTP 404, 400, 409
        └─ Retorna 204 No Content si tiene éxito
        ↓
    Cliente recibe respuesta HTTP
```

### Decisiones de Diseño

#### 1. Estrategia de Eliminación: Hard Delete

**Decisión:** Utilizar eliminación dura (removimiento completo del registro de la BD).

**Justificación:**
- Simplifica la lógica de consultas: no es necesario filtrar `Status != Cancelled` en todas partes
- El MVP no requiere historial de auditoría de cancelaciones
- Confirmado por Gate 1 (decisión de usuario)
- La re-registro es permitida después de cancelación (sin restricciones)

**Alternativa considerada:** Soft delete (actualizar `Status = Cancelled`)
- Pros: Proporciona historial de auditoría, permite análisis futuro
- Cons: Requiere filtrado adicional en consultas, más complejo
- Deferred para futuras iteraciones si la auditoría es necesaria

#### 2. Código de Respuesta: 204 No Content

**Decisión:** Retornar HTTP 204 No Content en cancelaciones exitosas.

**Justificación:**
- Conforme a semántica REST para operaciones DELETE
- Indica éxito sin necesidad de enviar cuerpo de respuesta
- Reduce tamaño de payload
- Consistente con convenciones de API REST

#### 3. Control de Concurrencia: Optimista

**Decisión:** Utilizar tokens de concurrencia optimista (`RowVersion` en `Event`).

**Justificación:**
- Ya configurado en la base de datos (desde US-3)
- Evita bloqueos pessimistas que reducirían el rendimiento
- Detecta conflictos cuando múltiples operaciones modifican simultáneamente
- Mapea a HTTP 409 Conflict con mensaje de reintentar

**Cómo funciona:**
```
Entity Framework Core rastrea RowVersion
         ↓
Al llamar SaveChangesAsync()
         ↓
Compara RowVersion actual vs. esperado
         ↓
Si no coincide → DbUpdateConcurrencyException
         ↓
Capturado y remapeado a DomainException
         ↓
Controlador mapea a HTTP 409
```

#### 4. Validación de Fecha: Eventos Futuros Solo

**Decisión:** No permitir cancelación de registros para eventos que ya han ocurrido.

**Implementación:**
```csharp
if (eventEntity.Date < DateTime.UtcNow)
{
    throw new DomainException("Cannot cancel registrations for events that have already occurred.");
}
```

**Justificación:**
- Previene corrupción de datos históricos
- Refleja la lógica del negocio (eventos pasados son inmutables)
- Confirmado por Gate 1

#### 5. Re-registro Permitido

**Decisión:** Permitir que un usuario se registre nuevamente después de cancelar.

**Justificación:**
- No hay restricción en el modelo de dominio
- El usuario puede cambiar de opinión
- Confirmado por Gate 1

---

## Referencia de API

### DELETE /api/v1/events/{id}/register

**Descripción:** Cancela el registro de un usuario para un evento específico.

**Autenticación:** Requerida (pendiente para Sprint 2)  
**Autorización:** Pendiente para Sprint 2

**Ruta de solicitud:**
```
DELETE /api/v1/events/{id}/register
Host: https://api.sportsclub.local
Content-Type: application/json
```

**Parámetros:**
| Parámetro | Ubicación | Tipo | Requerido | Descripción |
|-----------|----------|------|----------|------------|
| `id` | Ruta | UUID (GUID) | Sí | ID único del evento |
| `UserId` | Cuerpo | UUID (GUID) | Sí | ID único del usuario cancelando |

**Cuerpo de solicitud:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Respuestas exitosas:**

**200 204 No Content:**
```
HTTP/1.1 204 No Content
Content-Length: 0
```

El registro ha sido cancelado exitosamente. No hay cuerpo de respuesta.

**Respuestas de error:**

**400 Bad Request — Solicitud inválida:**
```json
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://api.sportsclub.local/problems/invalid-request",
  "title": "Invalid cancellation request",
  "status": 400,
  "detail": "Cannot cancel registrations for events that have already occurred.",
  "instance": "/api/v1/events/550e8400-e29b-41d4-a716-446655440000/register"
}
```

**Escenarios:**
- El evento tiene una fecha en el pasado
- El ID del evento no es un GUID válido
- El ID del usuario no es un GUID válido o está vacío

**404 Not Found — Recurso no encontrado:**
```json
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://api.sportsclub.local/problems/not-found",
  "title": "Event or registration not found",
  "status": 404,
  "detail": "Event with identifier '550e8400-e29b-41d4-a716-446655440000' does not exist.",
  "instance": "/api/v1/events/550e8400-e29b-41d4-a716-446655440000/register"
}
```

**Escenarios:**
- El evento no existe
- El usuario no tiene un registro activo para el evento
- El registro ya fue cancelado (no existe)

**409 Conflict — Conflicto de concurrencia:**
```json
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://api.sportsclub.local/problems/concurrency-conflict",
  "title": "Concurrency conflict",
  "status": 409,
  "detail": "The registration was modified or deleted by another process. Please try again.",
  "instance": "/api/v1/events/550e8400-e29b-41d4-a716-446655440000/register"
}
```

**Escenario:**
- Otra operación simultánea modificó la fila (RowVersion cambió)

**500 Internal Server Error:**
```json
HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json

{
  "type": "https://api.sportsclub.local/problems/server-error",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred while processing your request.",
  "instance": "/api/v1/events/550e8400-e29b-41d4-a716-446655440000/register"
}
```

---

## Cambios de Base de Datos

### Migraciones de EF Core
**Requerido:** No

La base de datos ya posee todos los esquemas necesarios. La estrategia de hard delete utiliza el método `DbSet<Registration>.Remove()` estándar sin requerir cambios de esquema.

### Cambios de esquema
**Requerido:** No

Se utilizan las tablas y columnas existentes:
- Tabla `Registrations` — se eliminarán filas
- Tabla `Events` — sin cambios de esquema (solo lectura con `.Include()`)
- Columna `Events.RowVersion` — ya configurada para concurrencia optimista

---

## Dependencias Añadidas

| Paquete | Versión | Propósito | Cambio |
|---------|---------|----------|--------|
| MediatR | (existente) | Patrón de comandos | Sin cambios |
| FluentValidation | (existente) | Validación de entrada | Sin cambios |
| Microsoft.EntityFrameworkCore | (existente) | Acceso a datos | Sin cambios |
| Microsoft.AspNetCore.Mvc | (existente) | Framework web | Sin cambios |

**Ningún paquete NuGet nuevo fue añadido.**

---

## Pruebas

### Cobertura de Pruebas Unitarias

**Archivos de prueba creados:**
1. `CancelRegistrationCommandHandlerTests.cs` — 10 casos de prueba
2. `CancelRegistrationCommandValidatorTests.cs` — 7 casos de prueba

**Cobertura de aplicación:** 97.61% (exceeds 90% minimum)

**Escenarios cubiertos:**

| Categoría | Casos de prueba |
|-----------|-----------------|
| Cancelación exitosa | Verifica eliminación del registro, aislamiento de múltiples registros |
| Evento no encontrado | Verifica que se lanza `EntityNotFoundException` |
| Evento pasado | Verifica que se lanza `DomainException` para fechas pasadas |
| Registro no existe | Verifica 404 para registros activos no encontrados |
| Registro ya cancelado | Verifica 404 para intentos de doble cancelación |
| Conflicto de concurrencia | Verifica manejo de `DbUpdateConcurrencyException` |
| Validación: EventId | Verifica rechazo cuando EventId está vacío |
| Validación: UserId | Verifica rechazo cuando UserId está vacío |
| Validación: múltiples campos | Verifica errores cuando ambos campos están vacíos |

### Pruebas de Integración

**Archivo creado:**  
`EventCancellationIntegrationTests.cs` — 10 casos de prueba

**Infraestructura:**
- Framework: xUnit
- Base de datos: TestContainers + SQL Server 2022
- Host web: WebApplicationFactory (in-process)
- Limpieza de BD: Respawn (estado limpio entre pruebas)

**Estado de ejecución:**
- Las pruebas no pudieron ejecutarse en el ambiente local (Docker no disponible)
- Verificación estática: Cumple con todos los estándares de código
- Ejecución esperada: CI/CD pipeline (Docker disponible)

**Escenarios cubiertos:**

| Grupo | Escenarios |
|------|----------|
| Cancelación exitosa | 204 No Content, eliminación de BD, aislamiento de registros |
| Recursos no encontrados | Evento no existe (404), registro no existe (404), ya cancelado (404) |
| Solicitudes inválidas | UserId vacío (400), GUID inválido (400), evento pasado (400) |
| Flujo de re-registro | Verificación que re-registro es posible después de cancelación |

---

## Configuración

### Claves de appsettings.json
**Añadidas:** Ninguna

La implementación no requiere nuevas claves de configuración. El comportamiento está completamente determinado por la lógica de comandos.

### Secretos requeridos / Claves de Azure Key Vault
**Requeridas:** Ninguno

---

## Patrones Arquitectónicos

### Patrón CQRS (Command Query Responsibility Segregation)

**Implementación:**
- **Command:** `CancelRegistrationCommand` contiene datos de entrada
- **Handler:** `CancelRegistrationCommandHandler` ejecuta la lógica de cancelación
- **Dispatcher:** MediatR envía el comando al manejador apropiado
- **Retorno:** `Unit` (void) — operación que no retorna datos

**Flujo:**
```csharp
var command = new CancelRegistrationCommand
{
    EventId = id,
    UserId = request.UserId
};

await sender.Send(command, cancellationToken);  // MediatR dispatcher
// CancelRegistrationCommandHandler.Handle() se ejecuta aquí
return NoContent();  // 204 si tiene éxito
```

### Patrón de Validación de Fluent

**Validador:** `CancelRegistrationCommandValidator`

```csharp
public class CancelRegistrationCommandValidator : AbstractValidator<CancelRegistrationCommand>
{
    public CancelRegistrationCommandValidator()
    {
        RuleFor(c => c.EventId).NotEmpty().WithMessage("EventId is required");
        RuleFor(c => c.UserId).NotEmpty().WithMessage("UserId is required");
    }
}
```

**Ejecución:** MediatR ejecuta automáticamente el validador antes de enviar al manejador.

### Manejo de Excepciones

**Excepciones de dominio:**
```csharp
// En CancelRegistrationCommandHandler:
if (eventEntity is null)
    throw new EntityNotFoundException("Event with identifier...");  // → HTTP 404

if (eventEntity.Date < DateTime.UtcNow)
    throw new DomainException("Cannot cancel registrations...");   // → HTTP 400

if (registration is null)
    throw new EntityNotFoundException("No active registration...");  // → HTTP 404
```

**Mapeo en controlador:**
```csharp
try
{
    await sender.Send(command, cancellationToken);
    return NoContent();  // 204
}
catch (EntityNotFoundException ex)
{
    return NotFound(new ProblemDetails { ... });  // 404
}
catch (DomainException ex)
{
    return BadRequest(new ProblemDetails { ... });  // 400
}
```

---

## Limitaciones Conocidas

1. **Sin Autenticación (MVP):** El UserId se acepta del cuerpo de solicitud sin verificación. Cualquier usuario puede cancelar registros de cualquier otro usuario. Será abordado en Sprint 2 con OAuth/JWT.

2. **Sin Historial de Auditoría:** Hard delete no deja registro de quién canceló y cuándo. Soft delete será considerado en futuras iteraciones.

3. **Sin Rate Limiting:** No hay limitación de tasa para prevenir abuso (cancelaciones masivas). Será implementado junto con autenticación.

4. **Sin Notificaciones:** No se envían emails o notificaciones push cuando se cancela un registro. Fuera de alcance.

5. **Sin Lista de Espera:** Cuando se libera una plaza, se asigna a nuevos registros en orden FIFO. Futuro: gestión de lista de espera.

6. **Sin Período de Gracia:** Los usuarios pueden cancelar hasta la fecha exacta del evento. No hay período de anticipación mínimo configurado. Gate 1 confirmó que esto es aceptable.

---

## Consideraciones de Seguridad

### Vulnerabilidades Identificadas y Estado

**CRÍTICA: IDOR (Insecure Direct Object Reference)**
- **Descripción:** Falta de autorización — cualquier usuario puede cancelar registros de cualquier otro
- **Estado:** OVERRIDDEN — autorización diferida a Sprint 2 (decision de usuario en Gate 3)
- **Mitigación Futura:** Se implementará autenticación y verificación de que el usuario autenticado coincide con el UserId

**MEDIA: Falta de Autenticación Sistemática**
- **Descripción:** No hay atributo `[Authorize]` en el controlador
- **Estado:** OVERRIDDEN — autenticación diferida a Sprint 2
- **Mitigación Futura:** Configurar JWT/OAuth y aplicar `[Authorize]` a operaciones sensibles

**MEDIA: CORS sin Credenciales**
- **Descripción:** Política CORS no configurada explícitamente para credenciales
- **Estado:** Será abordado cuando se implemente autenticación
- **Mitigación:** Agregar `.AllowCredentials()` a configuración CORS

**BAJA: Falta de Logging de Seguridad**
- **Descripción:** No se registran intentos de cancelación para auditoría
- **Estado:** Low priority (futuro)
- **Mitigación Sugerida:** Inyectar `ILogger` en handler y registrar intentos/éxitos/fallos

### Análisis de Inyección

**SQL Injection:** ✅ SEGURO
- Se utiliza Entity Framework Core con LINQ (consultas parametrizadas)
- Ninguna SQL cruda (`ExecuteSqlRaw`, `FromSqlRaw`)

**Command Injection:** ✅ NO APLICABLE
- No hay ejecución de procesos o comandos del sistema

**LDAP Injection:** ✅ NO APLICABLE
- No hay integraciones de directorio

### Validación de Entrada

✅ **EventId (GUID):** Validado en ruta con restricción `:guid`
✅ **UserId (GUID):** Validado en validador FluentValidation
✅ **Deserialization:** ASP.NET Core utiliza configuración de seguridad por defecto

---

## Notas de Despliegue

**Sin cambios de esquema:** No es necesario ejecutar migraciones

**Sin configuración nueva:** No hay nuevas variables de entorno o secretos

**Sin cambios críticos:** El endpoint es una adición, no modifica existentes

**Compatibilidad:** El cliente debe enviar solicitudes DELETE con cuerpo JSON (no todos los clientes lo soportan de forma nativa)

---

## Referencias de Implementación

**Patrón base:** `RegisterForEventCommandHandler` (US-7)

**Cambios principales versus patrón base:**
- Eliminación de registro en lugar de creación
- Validación de fecha (solo eventos futuros)
- Búsqueda de registro activo dentro de la colección cargada
- Respuesta 204 No Content en lugar de 201 Created

---

**Fin de Diseño Técnico**
