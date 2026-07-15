# Diseño Técnico — Gestión de Inscripciones (Administración)

**Issue:** #32  
**Rama de trabajo:** features/administration-implement-registration-management  
**Fecha:** 2026-07-07  
**Estado:** Implementado

---

## Descripción General

Este trabajo implementa un sistema completo de gestión de inscripciones a nivel de usuario y administrador. Los usuarios autenticados pueden ver sus inscripciones activas y cancelarlas (solo para eventos futuros). Los administradores tienen acceso completo: ver todas las inscripciones con filtros avanzados, crear inscripciones manuales para otros usuarios, cancelar cualquier inscripción, y exportar listados en CSV. Todas las acciones de administrador son registradas en la pista de auditoría.

> **Corrección post-implementación (2026-07-08):** `MyRegistrations.razor` (usuario) y, previsiblemente,
> `Admin/RegistrationManagement.razor` recibían 401 Unauthorized en todas sus llamadas porque el Web
> nunca reenviaba el JWT a los endpoints `[Authorize]` de `RegistrationsController` /
> `AdminRegistrationsController`. Ver
> [Reenvío del Token de la Web a la Api](../technical/issue-27-oauth2-authentication.md#reenvío-del-token-de-la-web-a-la-api-authtokenhandler)
> en el documento técnico de US-27 para el detalle completo de la causa raíz y el fix (`AuthTokenHandler`).

> **Corrección post-implementación (2026-07-08):** `MyRegistrations.razor` no declaraba
> `@rendermode InteractiveServer`. Sin ese directive, Blazor Server renderiza el componente en modo
> estático (SSR puro) y los manejadores `@onclick` nunca se conectan en el cliente, por lo que el
> botón "Cancel" no producía ninguna llamada ni error visible. Se añadió el rendermode (alineado con
> `EventDetails.razor`, `Events.razor` y `UserProfile.razor`, que ya lo tenían). Aprovechando el
> arreglo, se rediseñó la página con el lenguaje visual de marca (`page-header` con degradado,
> tarjeta, badges) usado en `UserProfile.razor`, y se añadió un `ConfirmationDialog` antes de
> cancelar, reutilizando el mismo componente y mensaje que ya usaba `EventDetails.razor` para
> cancelar un registro. Ver también la sección
> [Flujo de Datos — Usuario: Cancela Inscripción](#usuario--cancela-inscripción) actualizada más
> abajo.

---

## Arquitectura

### Componentes Involucrados

```
API Layer (REST)
├── RegistrationsController (usuario autenticado)
│   ├── GET /api/v1/registrations/me → GetUserRegistrationsQuery
│   └── DELETE /api/v1/registrations/{id} → CancelRegistrationByIdCommand
│
└── AdminRegistrationsController (administrador)
    ├── GET /api/admin/registrations → GetRegistrationsAdminQuery
    ├── POST /api/admin/registrations → CreateAdminRegistrationCommand
    └── DELETE /api/admin/registrations/{id} → CancelRegistrationByIdCommand

Application Layer (CQRS)
├── Queries
│   ├── GetUserRegistrationsQuery
│   │   └── GetUserRegistrationsQueryHandler (consulta EF Core)
│   └── GetRegistrationsAdminQuery
│       └── GetRegistrationsAdminQueryHandler (consulta paginada y filtrada)
│
└── Commands
    ├── CancelRegistrationByIdCommand
    │   └── CancelRegistrationByIdCommandHandler (autorización, auditoría)
    └── CreateAdminRegistrationCommand
        └── CreateAdminRegistrationCommandHandler (validaciones, auditoría)

Shared Layer (DTOs)
├── RegistrationListDto (respuesta de lectura)
├── CreateAdminRegistrationRequest (cuerpo POST)
└── GetAdminRegistrationsQueryParameters (parámetros GET)

Web Layer (UI Blazor)
├── MyRegistrations.razor / MyRegistrations.razor.cs (@rendermode InteractiveServer)
│   ├── RegistrationService (cliente HTTP)
│   └── Shared/ConfirmationDialog.razor (diálogo de confirmación antes de cancelar)
│
└── Admin/RegistrationManagement.razor / Admin/RegistrationManagement.razor.cs
    ├── AdminRegistrationManagementService (cliente HTTP)
    └── JS Interop: downloadFileFromText (exportar CSV)

Infraestructura
├── Auditoría: AuditAction (RegistrationCreated, RegistrationCancelled)
└── Excepciones: EntityNotFoundException, DomainException, etc.
```

### Flujo de Datos

#### Usuario — Consulta Inscripciones
1. Página `MyRegistrations` se carga → `OnInitializedAsync()`
2. Invoca `RegistrationService.GetMyRegistrationsAsync()`
3. HTTP GET `/api/v1/registrations/me` con autorización
4. `RegistrationsController.GetMyRegistrations()` extrae `userId` del claim de identidad
5. Envía `GetUserRegistrationsQuery(UserId, OnlyActive=true)`
6. Handler consulta registros donde `UserId == userId` y estado=Registered y `EventDate >= DateTime.UtcNow`
7. Proyecta a `RegistrationListDto` con `CanBeCancelledByUser = true` solo si evento es futuro
8. Devuelve lista; componente renderiza tabla

#### Usuario — Cancela Inscripción
1. Usuario hace clic en botón "Cancel" de la fila → `ShowCancelConfirmation(registration)` guarda
   la inscripción pendiente en `_pendingCancellation` y limpia mensajes previos
2. Se muestra `ConfirmationDialog` con título "Cancel Registration" y el mensaje "Are you sure you
   want to cancel your registration for this event?"
3. Si el usuario hace clic en "Cancel" del diálogo (o en el overlay) → `HideCancelConfirmation()`
   limpia `_pendingCancellation` sin llamar a la API
4. Si el usuario hace clic en "Confirm" → `HandleCancellationConfirmAsync()`:
   1. Invoca `RegistrationService.CancelMyRegistrationAsync(registrationId)`
   2. HTTP DELETE `/api/v1/registrations/{id}`
   3. Handler envía `CancelRegistrationByIdCommand` con `IsAdministrator=false`, `RequestingUserId=userId`
   4. Handler valida: registro existe, propietario==usuario, evento futuro
   5. Elimina registro de BD
   6. No registra en auditoría (acción de usuario, no administrador)
   7. Devuelve 204 NoContent
   8. Componente recarga lista con `LoadAsync()` y cierra el diálogo (`_pendingCancellation = null`)

**Nota:** Este flujo requiere que `MyRegistrations.razor` tenga `@rendermode InteractiveServer`;
sin interactividad del lado servidor, ni el botón "Cancel" ni el `ConfirmationDialog` responden a
clics (ver corrección post-implementación arriba).

#### Administrador — Consulta Inscripciones
1. Página `RegistrationManagement` se carga con `OnInitializedAsync()`
2. Invoca `AdminRegistrationManagementService.GetRegistrationsAsync()` con filtros iniciales
3. HTTP GET `/api/admin/registrations?pageNumber=1&pageSize=20&sortBy=RegistrationDate&sortOrder=desc`
4. Autorización: `[Authorize(Roles = "Administrator")]` en controlador
5. `AdminRegistrationsController.GetRegistrations()` envía `GetRegistrationsAdminQuery`
6. Handler aplica filtros (eventId, userId, status, fechas, texto de búsqueda)
7. Ordena por campo especificado (RegistrationDate, EventDate, UserName, Status)
8. Pagina resultados y proyecta a `RegistrationListDto`
9. Devuelve `PagedResult<RegistrationListDto>` con metadatos de paginación
10. Componente renderiza tabla y controles de navegación

#### Administrador — Crea Inscripción Manual
1. Al montar la página, `LoadManualRegistrationOptionsAsync()` precarga dos desplegables:
   `GetAllUsersAsync(isActiveFilter: true, ...)` y `GetAllEventsAsync(isUpcoming: true, ...)`
   (hasta 100 elementos cada uno, el máximo que admite el validador de ambas queries)
2. Admin selecciona un usuario (`_manualUserId`) y un evento (`_manualEventId`) en los `<select>`
3. Hace clic en botón "Create"
4. Invoca `AdminRegistrationManagementService.CreateRegistrationAsync(request)`
5. HTTP POST `/api/admin/registrations` con `CreateAdminRegistrationRequest`
6. Handler envía `CreateAdminRegistrationCommand` con `AdminUserId`, `UserId`, `EventId`
7. Handler valida: evento existe y futuro, usuario existe e activo, no inscripción duplicada, capacidad disponible
8. Crea nueva entidad `Registration` con estado=Registered
9. Registra en auditoría: `AuditAction.RegistrationCreated`, ipAddress, userAgent
10. Devuelve 201 Created con ID registrado
11. Componente limpia la selección de ambos desplegables y recarga lista

Los desplegables solo ofrecen usuarios activos y eventos futuros — exactamente las dos
precondiciones que el handler exige en el paso 7 — por lo que los errores "usuario inactivo" o
"evento pasado" ya no son alcanzables desde la UI, solo desde una llamada directa a la Api.

#### Administrador — Cancela Inscripción
1. Admin hace clic en botón "Cancel" en tabla
2. Invoca `AdminRegistrationManagementService.CancelRegistrationAsync(registrationId)`
3. HTTP DELETE `/api/admin/registrations/{id}`
4. Handler envía `CancelRegistrationByIdCommand` con `IsAdministrator=true`, `RequestingUserId=adminId`
5. Handler valida: registro existe
6. NO valida fecha de evento (admin puede cancelar registros pasados)
7. Elimina registro de BD
8. Registra en auditoría: `AuditAction.RegistrationCancelled`, detalles del evento/usuario, ipAddress, userAgent
9. Devuelve 204 NoContent
10. Componente recarga lista

#### Administrador — Exporta CSV
1. Admin pulsa el botón "Export CSV" en la cabecera de la página (`page-header-actions`,
   mismo patrón que "Import CSV" en `/admin/events`)
2. `ExportCsvAsync()` construye CSV en memoria
3. Encabezado: RegistrationId, EventTitle, EventDate, UserName, UserEmail, RegistrationDate, Status
4. Escapa valores CSV (comillas, comas)
5. Invoca JS Interop: `downloadFileFromText(fileName, content, "text/csv;charset=utf-8")`
6. JS crea Blob, descarga archivo: `registrations-20260707123456.csv`

> **Exportación a PDF eliminada.** Existió un botón "Export PDF"/`ExportPdfAsync()` que generaba
> texto plano con extensión `.pdf` (no un PDF real, sin cabecera `%PDF-` ni estructura de objetos
> PDF) — ningún lector de PDF podía abrirlo. Se retiró en vez de arreglarse con una librería de
> generación de PDF real (p. ej. QuestPDF/iText), para no añadir una dependencia nueva solo para
> este caso de uso. Solo queda la exportación a CSV.

### Decisiones de Diseño

| Decisión | Alternativa Rechazada | Justificación |
|---|---|---|
| **Dos endpoints API separados** (`/api/v1/registrations` vs `/api/admin/registrations`) | Un único endpoint con lógica condicional | Separación de responsabilidades, seguridad explícita, mejor evolución futura |
| **CanBeCancelledByUser en DTO** | Lógica de condición en componente | Fuente única de verdad (backend), coherencia con API GraphQL futura |
| **Eliminar registro en lugar de soft-delete** | Marcar como Cancelled | Simplifica conteos de capacidad, mantiene auditoría (comando registra cambio) |
| **Auditoría solo en acciones admin** | Auditar todas las acciones | Reduce ruido; usuarios ven sus propias acciones en UI |
| **Exportar en componente (CSV manual)** | Generar en servidor | Respuesta más rápida, menos carga servidor, CSV es trivial de construir en cliente |
| **Paginación de 20 registros por defecto** | Sin paginación o infinita | Balance: rendimiento UI vs cantidad de llamadas de navegación |

---

## Referencia de API

### GET /api/v1/registrations/me

**Descripción:** Obtiene las inscripciones activas del usuario autenticado.  
**Autenticación:** Requerida (JWT)  
**Autorización:** Usuario autenticado (sin restricción de rol)

**Respuesta 200 OK:**
```json
[
  {
    "registrationId": "550e8400-e29b-41d4-a716-446655440001",
    "eventId": "550e8400-e29b-41d4-a716-446655440010",
    "eventTitle": "Fútbol Amistoso",
    "eventDate": "2026-07-20T15:00:00Z",
    "eventLocation": "Cancha Central",
    "userId": "550e8400-e29b-41d4-a716-446655440100",
    "userName": "Juan Pérez",
    "userEmail": "juan@example.com",
    "registrationDate": "2026-07-07T10:30:00Z",
    "status": "Registered",
    "canBeCancelledByUser": true
  }
]
```

**Errores:**
| Código | Cuándo |
|--------|--------|
| 401 | No autenticado o JWT inválido |

---

### DELETE /api/v1/registrations/{id}

**Descripción:** Cancela una inscripción del usuario autenticado.  
**Autenticación:** Requerida (JWT)  
**Autorización:** Propietario de la inscripción

**Parámetros:**
- `id` (guid): ID de la inscripción

**Respuesta 204 No Content**

**Errores:**
| Código | Cuándo |
|--------|--------|
| 400 | Evento ya ocurrió (no se puede cancelar) |
| 401 | No autenticado |
| 403 | Usuario no es propietario de la inscripción |
| 404 | Inscripción no existe |

---

### GET /api/admin/registrations

**Descripción:** Obtiene inscripciones paginadas con filtros avanzados (solo administrador).  
**Autenticación:** Requerida (JWT)  
**Autorización:** Rol "Administrator"

**Parámetros Query:**
| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| `pageNumber` | int | 1 | Número de página (1-basado) |
| `pageSize` | int | 20 | Tamaño de página |
| `eventId` | guid? | null | Filtrar por evento |
| `userId` | guid? | null | Filtrar por usuario |
| `status` | enum? | null | Filtrar por estado (Registered, Cancelled) |
| `eventDateFrom` | datetime? | null | Filtrar eventos desde fecha |
| `eventDateTo` | datetime? | null | Filtrar eventos hasta fecha |
| `searchText` | string? | null | Búsqueda en título evento, nombre usuario, email |
| `sortBy` | string | RegistrationDate | Campo para ordenar |
| `sortOrder` | string | desc | Orden (asc, desc) |

**Campos de ordenamiento soportados:**
- RegistrationDate, EventDate, EventTitle, UserName, Status

**Respuesta 200 OK:**
```json
{
  "items": [
    {
      "registrationId": "550e8400-e29b-41d4-a716-446655440001",
      "eventId": "550e8400-e29b-41d4-a716-446655440010",
      "eventTitle": "Fútbol Amistoso",
      "eventDate": "2026-07-20T15:00:00Z",
      "eventLocation": "Cancha Central",
      "userId": "550e8400-e29b-41d4-a716-446655440100",
      "userName": "Juan Pérez",
      "userEmail": "juan@example.com",
      "registrationDate": "2026-07-07T10:30:00Z",
      "status": "Registered",
      "canBeCancelledByUser": true
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**Errores:**
| Código | Cuándo |
|--------|--------|
| 401 | No autenticado o JWT inválido |
| 403 | Usuario no tiene rol Administrator |

---

### POST /api/admin/registrations

**Descripción:** Crea una inscripción manualmente (solo administrador).  
**Autenticación:** Requerida (JWT)  
**Autorización:** Rol "Administrator"

**Cuerpo Solicitud:**
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440100",
  "eventId": "550e8400-e29b-41d4-a716-446655440010"
}
```

**Validaciones:**
- `userId` no vacío
- `eventId` no vacío
- Usuario existe e isActive=true
- Evento existe y eventDate >= now
- Usuario no tiene inscripción activa para ese evento
- Evento no ha alcanzado capacidad máxima

**Respuesta 201 Created:**
```
Location: /api/admin/registrations/{registrationId}
Body: "550e8400-e29b-41d4-a716-446655440002"
```

**Errores:**
| Código | Cuándo |
|--------|--------|
| 400 | Validación fallida (usuario inactivo, evento pasado, etc.) |
| 401 | No autenticado |
| 403 | Usuario no tiene rol Administrator |
| 404 | Usuario o evento no existe |
| 409 | Inscripción duplicada o evento lleno |

**Auditoría:**
- Acción: `RegistrationCreated`
- Se registran: ID admin, ID usuario, email usuario, detalles evento

---

### DELETE /api/admin/registrations/{id}

**Descripción:** Cancela cualquier inscripción (solo administrador).  
**Autenticación:** Requerida (JWT)  
**Autorización:** Rol "Administrator"

**Parámetros:**
- `id` (guid): ID de la inscripción

**Respuesta 204 No Content**

**Errores:**
| Código | Cuándo |
|--------|--------|
| 401 | No autenticado |
| 403 | Usuario no tiene rol Administrator |
| 404 | Inscripción no existe |
| 409 | Registro fue modificado/eliminado por otro proceso |

**Auditoría:**
- Acción: `RegistrationCancelled`
- Se registran: ID admin, ID usuario, email usuario, detalles evento, IP, User-Agent

---

## Cambios en la Configuración

### Enumeración AuditAction (extendida)

| Valor | Código | Descripción |
|-------|--------|-------------|
| RegistrationCreated | 9 | Administrador crea inscripción manual |
| RegistrationCancelled | 10 | Administrador cancela inscripción |

**Nota:** Se usa cuando `IsAdministrator=true` en comando.

---

## Cambios en BD / EF Core

### Tabla Registrations (sin cambios en esquema)
- Columna `Id` (PK, GUID)
- Columna `UserId` (FK)
- Columna `EventId` (FK)
- Columna `RegistrationDate` (DateTime)
- Columna `Status` (enum: Registered=0, Cancelled=1)

No se creó migración nueva; se aprovecha estructura existente de Sprint 1.

### Tabla AuditLogs (sin cambios en esquema)
- Auditoría existente almacena acciones registradas en handler

---

## Dependencias Agregadas

| Paquete | Versión | Propósito | Scope |
|---------|---------|----------|-------|
| Ninguno | - | Todo usa paquetes existentes | - |

(Se reutilizan: MediatR, FluentValidation, Entity Framework Core, System.Net.Http.Json)

---

## Pruebas

### Cobertura de Pruebas Unitarias

#### RegistrationService (Web)
- `GetMyRegistrationsAsync()`: mock HTTP 200
- `CancelMyRegistrationAsync()`: mock HTTP 204, 400, 404

#### AdminRegistrationManagementService (Web)
- `GetRegistrationsAsync()`: mock con varios filtros, paginación
- `CreateRegistrationAsync()`: mock HTTP 201
- `CancelRegistrationAsync()`: mock HTTP 204

#### Handlers CQRS (Application)
- `GetUserRegistrationsQueryHandler`: 
  - Filtra por UserId
  - OnlyActive=true retorna solo Registered y EventDate >= now
  - Ordena por EventDate
- `GetRegistrationsAdminQueryHandler`:
  - Aplica filtros individualmente
  - Ordena por campo especificado
  - Pagina correctamente
- `CancelRegistrationByIdCommandHandler`:
  - Usuario no puede cancelar registro ajeno → UnauthorizedAccessException
  - Usuario no puede cancelar evento pasado → DomainException
  - Admin puede cancelar cualquiera
  - Auditoría solo si IsAdministrator=true
- `CreateAdminRegistrationCommandHandler`:
  - Evento no existe → EntityNotFoundException
  - Usuario no existe → EntityNotFoundException
  - Usuario inactivo → InvalidOperationException
  - Inscripción duplicada → DuplicateRegistrationException
  - Capacidad excedida → CapacityExceededException
  - Auditoría registra con IP/UserAgent

#### Componentes Blazor (Web)
- `RegistrationManagement`: filtros, paginación, creación manual, exporta CSV
- `MyRegistrations`: sin tests bUnit dedicados a la fecha; el flujo de cancelación con
  confirmación reutiliza `Shared/ConfirmationDialog.razor`, que sí cuenta con cobertura propia
  (`ConfirmationDialogTests.cs`)

**Cobertura estimada:** 85%+

---

## Limitaciones Conocidas

1. **Cancela en tiempo real (no soft-delete):** Registros cancelados se eliminan de la BD. Si auditoría es requerida a nivel de registro cancellado, se necesitaría soft-delete con `DeletedAt` timestamp.

2. **Sin filtro por rango de fechas de inscripción:** Solo se puede filtrar por rango de fecha del evento, no por cuándo se inscribió el usuario. Agregable en futura versión.

3. **Búsqueda por texto no es full-text:** Búsqueda simple con `Contains()`. Para volúmenes altos, considerar Elasticsearch o Full-Text Search de SQL Server.

4. **Sin confirmación de diálogo modal para cancelación admin:** en `Admin/RegistrationManagement.razor` el admin hace clic en botón "Cancel" y se cancela inmediatamente, sin diálogo. Distinto del flujo de usuario en `MyRegistrations.razor`, que desde 2026-07-08 sí muestra un `ConfirmationDialog` antes de cancelar. Se recomienda extender el mismo patrón a la vista de administrador en una futura versión.

5. **Exportación solo de página actual:** CSV exporta solo los registros visibles en página actual. Para exportar todos con filtros, se requeriría parámetro `pageSize=MaxInt`.

---

## Consideraciones de Seguridad

### Autenticación
- ✅ Endpoints requieren JWT válido via `[Authorize]`
- ✅ ClaimTypes.NameIdentifier extraído y validado
- ✅ Fallback a 401 si userId claim ausente o inválido

### Autorización
- ✅ `/api/v1/registrations` abierto a usuarios autenticados (cualquier rol)
- ✅ `/api/admin/registrations` restringido a `[Authorize(Roles = "Administrator")]`
- ✅ Usuario regular no puede ver/modificar registros ajenos (validado en handler)
- ✅ Admin puede ver/modificar cualquier registro (por diseño)

### Validación
- ✅ FluentValidation en handlers de comando: IDs no vacíos
- ✅ Verificaciones de negocio en handler: evento futuro, usuario activo, capacidad
- ✅ Excepciones tipadas: EntityNotFoundException, DomainException, UnauthorizedAccessException
- ✅ HTTP status codes correctos (403 vs 404 vs 400)

### Auditoría
- ✅ Todas las acciones admin registradas: IP, User-Agent, detalles del cambio
- ✅ Acciones de usuario no registradas (privacidad)
- ✅ AuditAction enums claros: RegistrationCreated, RegistrationCancelled

### Datos Sensibles
- ✅ Email de usuario expuesto en DTO (necesario para admin, mitigado por autorización)
- ✅ IDs sensibles (userId, eventId) en URLs de admin, protegidos por rol
- ⚠️ IP y User-Agent almacenados en auditoría (RGPD: documentar en política privacidad)

---

## Integración con Sprint 1

- Reutiliza entidades `User`, `Event`, `Registration` existentes
- Reutiliza `RegistrationStatus` enum (Registered, Cancelled)
- Reutiliza `IApplicationDbContext` y `IAuditService` infraestructura
- Se agrega `AuditAction` enum (2 nuevos valores)
- Componentes Blazor siguen convención de App.razor, Program.cs inyecciones

---

## Métricas de Rendimiento

| Escenario | Query Esperado | Índices Necesarios |
|-----------|--------|---------|
| GetMyRegistrations (1 usuario, ~10 registros) | <50ms | IX_Registrations_UserId, IX_Registrations_EventId |
| GetRegistrationsAdmin (150 registros, página 1) | <200ms | IX_Registrations_Status, IX_Events_Date |
| Export 20 registros a CSV | <10ms (en cliente) | N/A |

Índices se asumen del Sprint 1 o deben verificarse.

---
