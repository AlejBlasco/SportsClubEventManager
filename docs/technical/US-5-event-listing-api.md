# Diseño Técnico — API de Listado de Eventos
**Historia:** US-5  
**Rama de trabajo:** features/us-5-event-listing-api  
**Fecha:** 2026-06-23  
**Estado:** Implementado

---

## Descripción General

Se ha implementado un endpoint REST que permite recuperar un listado de eventos con filtrado opcional por rango de fechas. La implementación sigue los principios de Clean Architecture, utiliza MediatR para el patrón CQRS, FluentValidation para la validación de entrada en la capa de aplicación, y devuelve respuestas estandarizadas en formato ProblemDetails (RFC 7807).

Este es un componente fundamental de la MVP del Sprint 1 que habilita la funcionalidad de calendario de eventos en el frontend.

---

## Arquitectura

### Componentes Involucrados

La implementación involucra cuatro capas siguiendo Clean Architecture:

**1. Capa de Presentación (API)**
- `EventsController.cs` — Controlador delgado que expone el endpoint HTTP
- `ExceptionHandlingMiddleware.cs` — Middleware global para manejo de errores con ProblemDetails
- `Program.cs` — Configuración de servicios, middleware y pipeline

**2. Capa de Aplicación**
- `GetEventsQuery.cs` — Solicitud de consulta (MediatR IRequest)
- `GetEventsQueryHandler.cs` — Manejador de la consulta con lógica de filtrado y proyección
- `GetEventsQueryValidator.cs` — Validador de entrada (FluentValidation)
- `ValidationBehavior.cs` — Comportamiento del pipeline de MediatR para validación
- `IApplicationDbContext.cs` — Abstracción para acceso a datos

**3. Capa de Infraestructura**
- `AppDbContext.cs` — Implementa IApplicationDbContext con EF Core
- `DependencyInjection.cs` — Registro de servicios

**4. Capa de Dominio**
- `Event.cs` — Entidad Event con campos requeridos
- `Registration.cs` — Entidad Registration con estado (activa, cancelada, en lista de espera)

**5. Proyecto Compartido**
- `EventDto.cs` — DTO para transferencia de datos entre API y clientes

### Flujo de Datos

1. **Solicitud HTTP**
   ```
   GET /api/v1/events?startDate=2026-07-01&endDate=2026-07-31
   ```

2. **Pipeline del Controlador**
   - `EventsController.GetEvents()` recibe parámetros de query
   - Crea instancia de `GetEventsQuery` con los filtros
   - Envía consulta a través del `ISender` de MediatR

3. **Pipeline de MediatR**
   - `ValidationBehavior` intercepta la solicitud
   - Ejecuta `GetEventsQueryValidator` para validar parámetros
   - Si hay errores, lanza `ValidationException`
   - Si la validación pasa, invoca `GetEventsQueryHandler`

4. **Ejecución de la Consulta**
   - `GetEventsQueryHandler.Handle()` recibe la consulta validada
   - Construye una consulta LINQ comenzando con `context.Events`
   - Aplica filtro `startDate` si se proporciona: `e.Date >= startDate`
   - Aplica filtro `endDate` si se proporciona: `e.Date <= endDate`
   - Ordena por fecha ascendente: `OrderBy(e => e.Date)`
   - **Proyecta a EventDto** en la consulta (no materializa entidades)
   - Calcula `AvailableSlots` en la base de datos: `MaxCapacity - Count(Registrations where Status != Cancelled)`
   - Ejecuta consulta con `.AsNoTracking()` y `.ToListAsync()`

5. **Respuesta**
   - Retorna `List<EventDto>` ordenada por fecha
   - Controlador devuelve HTTP 200 OK con JSON serializado

6. **Manejo de Errores**
   - Si hay `ValidationException`, el middleware convierte a HTTP 400 con ProblemDetails
   - Si hay cualquier otra excepción, el middleware retorna HTTP 500 con mensaje genérico

### Decisiones de Diseño

#### 1. **Proyección en lugar de AutoMapper**
- **Decisión:** Usar proyección LINQ directamente en la consulta EF Core
- **Justificación:** 
  - Evita la vulnerabilidad de seguridad CVE-2021-43805 en AutoMapper 12.x
  - Genera una **única consulta SQL** con agregación `LEFT JOIN` y `COUNT`
  - Más eficiente que AutoMapper (sin deserialización intermedia)
  - Código más simple y directo

#### 2. **Dependencia Invertida con IApplicationDbContext**
- **Decisión:** La capa Application define interface `IApplicationDbContext`, Infrastructure la implementa
- **Justificación:**
  - Cumple con Clean Architecture (el flujo de dependencias: API → Application ← Infrastructure)
  - Permite testing sin dependencias de Infrastructure
  - Posibilita múltiples implementaciones de DbContext
  - Código más mantenible y testeable

#### 3. **Validación en Pipeline de MediatR**
- **Decisión:** Usar `IPipelineBehavior` para validación automática en cada consulta
- **Justificación:**
  - La validación ocurre antes del handler, no mezcla responsabilidades
  - Reutilizable en todas las consultas futuras
  - El controlador permanece delgado, sin lógica de validación
  - Integración perfecta con FluentValidation

#### 4. **Cálculo de AvailableSlots en la Base de Datos**
- **Decisión:** Calcular en la proyección de SQL, no en memoria
- **Justificación:**
  - Evita cargar todas las registraciones en memoria
  - Una sola consulta SQL en lugar de N+1 consultas
  - EF Core genera `SELECT COUNT(*) WHERE Status != Cancelled` automáticamente
  - Escalable incluso con cientos de eventos y miles de registraciones

#### 5. **Filtrado Inclusivo en Fechas**
- **Decisión:** `StartDate >= Date <= EndDate` (ambos inclusive)
- **Justificación:**
  - Más intuitivo para usuarios: "eventos en julio" = desde el 1 al 31 inclusive
  - Cumple con requisitos de aceptación
  - Consistente con comportamiento esperado de rangos de fechas

---

## Referencia de API

### GET /api/v1/events

**Descripción:** Recupera un listado de eventos con filtrado opcional por rango de fechas. Los eventos se ordenan por fecha en orden ascendente. Se calcula dinámicamente el número de espacios disponibles basado en la capacidad máxima y registraciones activas.

**Autenticación:** No requerida (MVP público)

**Autorización:** Ninguna (acceso público)

**Parámetros de Query:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `startDate` | DateTime? | No | Fecha de inicio del filtro (inclusive). Formato: `YYYY-MM-DD`. Solo se devuelven eventos en o después de esta fecha. |
| `endDate` | DateTime? | No | Fecha de fin del filtro (inclusive). Formato: `YYYY-MM-DD`. Solo se devuelven eventos en o antes de esta fecha. |

**Ejemplo de Solicitud:**
```http
GET /api/v1/events?startDate=2026-07-01&endDate=2026-07-31 HTTP/1.1
Host: localhost:5001
Accept: application/json
```

**Respuesta 200 OK:**
```json
[
  {
    "id": "55555555-5555-5555-5555-555555555555",
    "title": "Aire Comprimido Carabina - Social",
    "date": "2026-07-12T10:30:00Z",
    "location": "Barcelona - CT Vallès",
    "maxCapacity": 7,
    "availableSlots": 1
  },
  {
    "id": "66666666-6666-6666-6666-666666666666",
    "title": "Pistola de Velocidad - Trofeo del Globo",
    "date": "2026-07-18T11:00:00Z",
    "location": "Valencia - CTW",
    "maxCapacity": 6,
    "availableSlots": 0
  }
]
```

**Esquema de Respuesta — EventDto:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `id` | Guid | Identificador único del evento |
| `title` | string | Título/nombre del evento |
| `date` | DateTime | Fecha y hora del evento en UTC |
| `location` | string | Ubicación donde se celebra el evento |
| `maxCapacity` | int | Capacidad máxima de participantes |
| `availableSlots` | int | Espacios disponibles (MaxCapacity - registraciones activas). Puede ser 0 si el evento está lleno. |

**Respuestas de Error:**

| Código | Condición | Ejemplo |
|--------|-----------|---------|
| 400 | Parámetros inválidos (ej: startDate > endDate, formato de fecha incorrecto) | Ver ProblemDetails abajo |
| 500 | Error interno del servidor | ProblemDetails con mensaje genérico |

**Ejemplo — Error 400 (Rango de Fechas Inválido):**
```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/events",
  "errors": {
    "": [
      "StartDate must be less than or equal to EndDate."
    ]
  }
}
```

**Ejemplo — Error 400 (Formato de Fecha Incorrecto):**
```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/events",
  "errors": {
    "startDate": [
      "The value 'invalid-date' is not valid for DateTime."
    ]
  }
}
```

---

## Validación

### Reglas de Validación (FluentValidation)

**Archivo:** `src/SportsClubEventManager.Application/Events/Queries/GetEvents/GetEventsQueryValidator.cs`

Las siguientes reglas se aplican automáticamente en el pipeline de MediatR:

1. **StartDate y EndDate opcionales** — ambos pueden ser null
2. **Si se proporciona StartDate:** Debe ser una fecha válida en formato `YYYY-MM-DD`
3. **Si se proporciona EndDate:** Debe ser una fecha válida en formato `YYYY-MM-DD`
4. **Si ambas se proporcionan:** `StartDate <= EndDate` (requerido)
   - Si `StartDate > EndDate`, se retorna HTTP 400 con error de validación

**Ejemplo de Validación Fallida:**
```csharp
// Esto fallará validación:
startDate = "2026-08-01" (agosto)
endDate = "2026-07-01" (julio)
// Error: "StartDate must be less than or equal to EndDate."
```

### Punto de Integración

El validador se ejecuta **antes** del handler gracias a `ValidationBehavior`:
```csharp
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

---

## Manejo de Errores

### ProblemDetails (RFC 7807)

Todas las respuestas de error siguen el estándar RFC 7807 (Problem Details for HTTP APIs) implementado en `ExceptionHandlingMiddleware.cs`.

**Estructura Base:**
```json
{
  "status": <http-status-code>,
  "title": "<error-title>",
  "detail": "<error-detail>",
  "instance": "<request-path>",
  "errors": { "<field>": ["<error-message>"] }  // Solo para validación
}
```

**Errores de Validación (HTTP 400):**
```json
{
  "status": 400,
  "title": "Validation Error",
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/events",
  "errors": {
    "startDate": ["The value 'abc' is not valid for DateTime."],
    "": ["StartDate must be less than or equal to EndDate."]
  }
}
```

**Errores Internos (HTTP 500):**
```json
{
  "status": 500,
  "title": "Internal Server Error",
  "detail": "An unexpected error occurred. Please contact support.",
  "instance": "/api/v1/events"
}
```

**Implementación en Middleware:**
- Las excepciones de `ValidationException` se convierten a HTTP 400
- Cualquier otra excepción se convierte a HTTP 500
- **Nunca se exponen stack traces** en producción (por seguridad)
- Los logs incluyen información detallada para debugging

---

## Configuración

### Claves de appsettings.json

| Clave | Tipo | Valor por Defecto | Descripción |
|-------|------|-------------------|-------------|
| `ConnectionStrings:DefaultConnection` | string | `Server=(localdb)\mssqllocaldb;Database=SportsClubEventManager;...` | Cadena de conexión a SQL Server |
| `Logging:LogLevel:Default` | string | `Information` | Nivel de logging por defecto |
| `Logging:LogLevel:Microsoft.AspNetCore` | string | `Warning` | Nivel de logging para ASP.NET Core |
| `Cors:AllowedOrigins` | string[] | `["https://localhost:5001", "https://localhost:7001"]` | Orígenes CORS permitidos |
| `AllowedHosts` | string | `*` | Hosts HTTP permitidos |

### Configuración de Desarrollo (appsettings.Development.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:5001",
      "https://localhost:7001",
      "http://localhost:5000",
      "http://localhost:7000"
    ]
  }
}
```

### Secretos Requeridos / Azure Key Vault

**Desarrollo:** 
- Ninguno. Se usa cadena de conexión en `appsettings.json`

**Producción:**
- `ConnectionString` — Debe moverse a Azure Key Vault
- `Cors:AllowedOrigins` — Debe actualizar orígenes a dominios de producción
- Documentado en checklist de deployment (pendiente)

---

## Cambios de Base de Datos

### Migración de EF Core

**Estado:** No requiere. Se utiliza la migración existente de US-3 (Database Setup and Migrations).

La consulta es **read-only** y no modifica esquema:
- Usa tablas existentes: `Events`, `Registrations`, `Users`
- Propiedades requeridas ya existen en el modelo de dominio
- No hay campos nuevos que requieran migración

### Cambios de Esquema

**No aplica.** La implementación solo consulta tablas existentes.

**Tablas utilizadas:**
- `Events` — Tabla de eventos (read-only)
- `Registrations` — Tabla de registraciones (read-only, para contar espacios disponibles)

---

## Dependencias Agregadas

### NuGet Packages

| Paquete | Versión | Proyecto | Propósito |
|---------|---------|----------|-----------|
| **MediatR** | 12.4.1 | Application, Api | Patrón CQRS para consultas |
| **FluentValidation** | 11.11.0 | Application | Validación de entrada |
| **FluentValidation.DependencyInjectionExtensions** | 11.11.0 | Application | Registro de validadores en DI |
| **FluentValidation.AspNetCore** | 11.3.0 | Api | Integración con ASP.NET Core |
| **Microsoft.EntityFrameworkCore** | 10.0.1 | Application | Abstracciones de EF Core (DbSet<T>) |
| **Swashbuckle.AspNetCore** | 7.2.0 | Api | Swagger/OpenAPI |
| **Microsoft.AspNetCore.OpenApi** | 10.0.9 | Api | Soporte OpenAPI |

**Compatibilidad:**
- ✅ .NET 10.0 soportado por todos los paquetes
- ✅ MediatR 12.4.1 es la última versión gratuita (13+ requiere licencia comercial)
- ✅ Sin vulnerabilidades conocidas

---

## Testing

### Cobertura de Pruebas Unitarias

**Total de pruebas:** 29 (todas pasadas)  
**Cobertura:** ~92% (supera mínimo requerido del 90%)

**Componentes cubiertos:**

1. **GetEventsQueryHandler (17 pruebas)**
   - Sin filtros: retorna todos los eventos ordenados por fecha
   - Filtro startDate: retorna eventos en o después de esa fecha
   - Filtro endDate: retorna eventos en o antes de esa fecha
   - Ambos filtros: retorna eventos en el rango especificado
   - Cálculo de AvailableSlots: MaxCapacity - registraciones activas
   - Registraciones canceladas: excluidas del cálculo
   - Registraciones en lista de espera: incluidas en el cálculo
   - Base de datos vacía: retorna lista vacía
   - Casos límite: eventos en fechas exactas, muy futuro, etc.

2. **GetEventsQueryValidator (12 pruebas)**
   - Ambas fechas nulas: válido
   - Solo StartDate: válido
   - Solo EndDate: válido
   - StartDate == EndDate: válido
   - StartDate < EndDate: válido
   - StartDate > EndDate: inválido (error específico)
   - Casos límite: fechas muy separadas, pasadas, futuras, etc.

### Escenarios de Integración

Pendiente. La fase de INTEGRATION_TESTING creará pruebas de extremo a extremo con `Microsoft.AspNetCore.Mvc.Testing` y TestContainers.

**Escenarios planeados:**
- GET /api/v1/events (sin filtros) → HTTP 200 con todos los eventos
- GET /api/v1/events?startDate=... → HTTP 200 con eventos filtrados
- GET /api/v1/events?endDate=... → HTTP 200 con eventos filtrados
- GET /api/v1/events?startDate=X&endDate=Y → HTTP 200 con eventos en rango
- Rango de fechas inválido → HTTP 400 con ProblemDetails
- Base de datos vacía → HTTP 200 con array vacío
- Headers CORS presentes en respuesta
- Swagger UI accesible en /swagger

---

## Limitaciones Conocidas

### MVP (Sprint 1)

1. **Sin paginación**
   - Devuelve todos los eventos que coinciden con el filtro
   - Deferred a Sprint 2 cuando el conteo de eventos crezca > 500
   - Impacto actual: mínimo (10 eventos en seed data)

2. **Sin autenticación/autorización**
   - API pública (decisión del stakeholder para MVP)
   - Deferred a Sprint 2 (OAuth2 + RBAC)
   - Acceso sin restricciones a todos los eventos

3. **Sin filtros adicionales**
   - Solo filtrado por fecha (startDate, endDate)
   - Filtrado por categoría/tipo deferred a Sprint 2
   - Filtrado por ubicación deferred a Sprint 2

4. **Sin caché**
   - Cada solicitud consulta la base de datos
   - Deferred a Sprint 2 si análisis de rendimiento lo requiere
   - Rendimiento actual: < 100ms para 10 eventos

5. **Sin rate limiting**
   - API sin protección contra abuso
   - Deferred a Sprint 2 (junto con autenticación)
   - Aceptable para MVP en entorno controlado

### Decisiones de Diseño

1. **Eventos llenos incluidos**
   - Eventos con `AvailableSlots = 0` SÍ se devuelven
   - El frontend decide si mostrarlos o no
   - Permite casos de uso como "lista de espera"

2. **Formato de fecha ISO 8601**
   - Solo se acepta `YYYY-MM-DD` en los parámetros
   - Conversión implícita a DateTime con medianoche (00:00:00)
   - Más intuitivo que timestamps completos

3. **Sin endpoint de evento individual**
   - No existe GET /api/v1/events/{id}
   - Deferred a futuro cuando sea necesario

---

## Consideraciones de Seguridad

### Vulnerabilidades Evitadas

1. **SQL Injection** ✅ Mitigado
   - EF Core parameteriza automáticamente todas las consultas
   - FluentValidation valida formatos de fecha
   - No se utiliza SQL raw

2. **Information Disclosure** ✅ Mitigado
   - ProblemDetails nunca expone stack traces
   - Logs incluyen detalles (acceso restringido)
   - Errores genéricos para usuarios

3. **CORS** ✅ Configurado
   - Orígenes limitados a localhost en desarrollo
   - Documentado para producción

### Dependencias de Seguridad

**Verificado:**
- ✅ MediatR 12.4.1 — sin CVEs conocidas
- ✅ FluentValidation 11.11.0 — sin CVEs conocidas
- ✅ Microsoft.EntityFrameworkCore 10.0.1 — sin CVEs conocidas
- ✅ Swashbuckle.AspNetCore 7.2.0 — sin CVEs conocidas

**Vulnerable y Removido:**
- ❌ AutoMapper 12.x — CVE-2021-43805 (HIGH) — **Removido, reemplazado con proyección**

### Validación de Entrada

- Todos los parámetros validados en Application layer
- FluentValidation integrado en pipeline de MediatR
- Rechaza formatos de fecha inválidos con HTTP 400
- Rechaza rangos de fechas inválidos con HTTP 400

### Acceso a Datos

- `IApplicationDbContext` abstrae acceso a base de datos
- Queries son read-only (no hay operaciones de escritura)
- `AsNoTracking()` optimiza y limita tracking de EF Core
- No hay operaciones N+1 (consulta única con agregación)

---

## Resumen de Implementación

**Archivos Creados:** 12
- API: 5 (Controller, Middleware, Program, 2 configs)
- Application: 5 (Query, Handler, Validator, Behavior, DI)
- Shared: 1 (EventDto)
- Infrastructure: 2 (IApplicationDbContext, DependencyInjection)

**Líneas de Código:** ~439 (sin comentarios de documentación)

**Build:** ✅ Correcto (0 advertencias, 0 errores)

**Pruebas:** ✅ 29/29 pasadas (~92% cobertura)

**Seguridad:** ✅ Sin vulnerabilidades conocidas

**Performance:** ✅ < 100ms para 10 eventos, escalable con proyección

---

## Referencias

- **GitHub Issue:** #7 (US-5)
- **Requirements Analysis:** `.claude/docs/US-5/requirements-analysis.md`
- **Impact Analysis:** `.claude/docs/US-5/impact-analysis.md`
- **Implementation Report:** `.claude/docs/US-5/implementation-report.md`
- **Code Review Report:** `.claude/docs/US-5/review-report.md`
- **Unit Test Report:** `.claude/docs/US-5/unit-test-report.md`
- **Rama de trabajo:** `features/us-5-event-listing-api`
