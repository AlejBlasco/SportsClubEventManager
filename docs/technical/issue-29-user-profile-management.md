# Diseño Técnico — Gestión de Perfil de Usuario
**Story:** US-29  
**Rama de trabajo:** feature/US-29-user-profile-management  
**Fecha:** 2026-07-07  
**Estado:** Implementado  

---

## Resumen

Esta funcionalidad permite a los usuarios autenticados visualizar y actualizar su información personal (Nombre, Género, Email, Número de Licencia, Categoría de Licencia) y cambiar su contraseña (solo para autenticación local). Se implementa siguiendo la arquitectura Clean Architecture del proyecto, con capas claramente separadas y autorización reforzada en dos niveles (controlador + handler).

La implementación distingue entre usuarios con autenticación local (credenciales en base de datos) y usuarios OAuth (Google), aplicando restricciones específicas: los usuarios OAuth no pueden modificar su email (gestionado por el proveedor) ni cambiar contraseña. Todos los campos son validados tanto en el cliente (Blazor) como en el servidor (FluentValidation) para garantizar la integridad de los datos.

**Requisitos previos:** US-27 (OAuth2 Authentication) y US-28 (Role-based Authorization) completados.

> **Correcciones post-implementación (2026-07-08):** aunque este documento describe `UserProfile.razor`
> como la única página en `/profile`, en el código llegó a coexistir un `Profile.razor` obsoleto (una
> versión temprana de solo lectura) mapeado a la misma ruta `@page "/profile"`, lo que provocaba un
> `AmbiguousMatchException` al navegar a "My Profile". Se eliminó `Profile.razor` / `Profile.razor.cs`,
> dejando `UserProfile.razor` como implementación única. Además, `UserProfileService` recibía 401
> Unauthorized en todas sus llamadas porque el Web nunca reenviaba el JWT a la Api — ver
> [Reenvío del Token de la Web a la Api](../technical/issue-27-oauth2-authentication.md#reenvío-del-token-de-la-web-a-la-api-authtokenhandler)
> en el documento técnico de US-27 para el detalle completo de la causa raíz y el fix (`AuthTokenHandler`).

---

## Arquitectura

### Componentes Involucrados

```
┌─────────────────┐
│  User Browser   │
│   (Blazor UI)   │
└────────┬────────┘
         │ HTTPS
         ▼
┌─────────────────────────────────────────┐
│  Web Layer (SportsClubEventManager.Web) │
│  • UserProfile.razor                     │
│  • ChangePasswordModal.razor             │
│  • UserProfileService (HTTP Client)      │
└────────┬────────────────────────────────┘
         │ HTTP (Internal)
         ▼
┌───────────────────────────────────────────┐
│  API Layer (SportsClubEventManager.Api)   │
│  • UsersController                         │
│    - GET /api/users/{id}/profile          │
│    - PUT /api/users/{id}/profile          │
│    - PUT /api/users/{id}/password         │
└────────┬──────────────────────────────────┘
         │ MediatR
         ▼
┌──────────────────────────────────────────────────┐
│  Application Layer (Application)                  │
│  • GetUserProfileQuery + Handler                  │
│  • UpdateProfileCommand + Handler + Validator     │
│  • ChangePasswordCommand + Handler + Validator    │
└────────┬─────────────────────────────────────────┘
         │ IApplicationDbContext
         ▼
┌──────────────────────────────────────────────────┐
│  Infrastructure Layer (Infrastructure)            │
│  • ApplicationDbContext (EF Core)                 │
│  • PasswordHasher (BCrypt)                        │
│  • TokenService (JWT generation)                  │
└────────┬─────────────────────────────────────────┘
         │
         ▼
┌───────────────┐
│   Database    │
│  (Users table)│
└───────────────┘
```

**Responsabilidades por capa:**

- **Shared Layer:** DTOs para transferencia de datos entre Web y API
- **Web Layer:** Interfaz de usuario (Blazor Server), validación cliente, gestión de estado del formulario
- **API Layer:** Endpoints REST, autorización a nivel de controlador, mapeo de excepciones a códigos HTTP
- **Application Layer:** Lógica de negocio, validación servidor, autorización a nivel de handler, orquestación CQRS
- **Infrastructure Layer:** Acceso a datos (EF Core), hashing de contraseñas, generación de tokens
- **Domain Layer:** Entidad `User`, enums (`Gender`, `Role`)

---

### Flujo de Datos

#### Caso 1: Visualizar Perfil

1. Usuario autenticado navega a `/profile`
2. Blazor Server renderiza `UserProfile.razor`
3. Componente invoca `IUserProfileService.GetProfileAsync(userId)`
4. HTTP GET → `/api/users/{userId}/profile`
5. `UsersController.GetProfile` valida que `RequestingUserId` (del JWT) == `userId` (ruta)
6. Si autorización OK → envía `GetUserProfileQuery` vía MediatR
7. `GetUserProfileQueryHandler` consulta `User` en base de datos
8. Retorna `UserProfileDto` con flag `IsOAuthUser` calculado
9. Blazor renderiza formulario con campos editables/read-only según tipo de autenticación

#### Caso 2: Actualizar Perfil

1. Usuario modifica campos editables y hace clic en "Guardar"
2. `EditForm.OnValidSubmit` invoca `HandleProfileSubmitAsync`
3. Blazor valida con `DataAnnotationsValidator`
4. HTTP PUT → `/api/users/{userId}/profile` con `UpdateProfileRequest` en body
5. `UsersController.UpdateProfile` valida autorización
6. Crea `UpdateProfileCommand` con `RequestingUserId` y campos del request
7. MediatR envía comando a `UpdateProfileCommandHandler`
8. Handler valida:
   - Autorización: `RequestingUserId` == `UserId` (doble check)
   - Usuario OAuth no puede cambiar email
   - Email único (si cambió)
   - Enum `Gender` válido
9. Actualiza entidad `User` y persiste con `SaveChangesAsync`
10. Retorna `UserProfileDto` actualizado
11. Blazor muestra mensaje de confirmación

#### Caso 3: Cambiar Contraseña (Local Auth)

1. Usuario hace clic en "Cambiar Contraseña" (botón oculto si OAuth)
2. Se abre `ChangePasswordModal.razor`
3. Usuario ingresa: contraseña actual, nueva contraseña, confirmación
4. Validación cliente: mínimo 8 caracteres, coincidencia confirmación
5. HTTP PUT → `/api/users/{userId}/password` con `ChangePasswordRequest`
6. `UsersController.ChangePassword` valida autorización y coincidencia de contraseñas
7. MediatR envía `ChangePasswordCommand` a `ChangePasswordCommandHandler`
8. Handler valida:
   - Autorización doble check
   - Usuario no es OAuth (excepción si lo es)
   - Contraseña actual correcta (vía `IPasswordHasher.VerifyPassword`)
9. Hashea nueva contraseña con BCrypt (work factor 12)
10. Genera nuevos access token + refresh token
11. Hashea refresh token y lo almacena con expiración
12. Retorna `AuthenticationResult` con nuevos tokens
13. Modal muestra éxito, cierra, y actualiza tokens en cliente

---

### Decisiones de Diseño

| Decisión | Alternativas Consideradas | Justificación |
|----------|---------------------------|---------------|
| **Autorización en dos capas** (controller + handler) | Solo en controller, solo en handler | Defensa en profundidad. Controller valida early, handler garantiza seguridad incluso si comando se invoca desde otro contexto |
| **DTOs inmutables** (records con `init`) | Clases mutables con setters | Previene manipulación post-deserialización. Seguridad mejorada |
| **Validación email con FluentValidation + regex custom** | Solo `.EmailAddress()` de FluentValidation | FluentValidation `.EmailAddress()` es permisivo. Regex custom valida formato + dominio con punto |
| **Password policy: solo mín. 8 chars** | Complejidad (mayúsculas, números, símbolos) | Decisión Gate 2: mantener política actual. BCrypt mitiga fuerza bruta |
| **Email sin verificación** | Flujo de verificación con token | Decisión Gate 2: MVP sin verificación. Backlog para Sprint 3+ |
| **Sin bloqueo optimista (RowVersion)** | Añadir campo RowVersion | Decisión Gate 2: bajo riesgo en MVP. Backlog para mejora futura |
| **Tokens nuevos al cambiar contraseña** | Invalidar sesiones existentes | Decisión Gate 2: mantener sesiones activas. UX menos disruptiva |

---

## Referencia de API

### GET /api/users/{id}/profile

**Descripción:** Obtiene el perfil del usuario especificado.

**Autenticación:** Requerida (JWT Bearer token)

**Autorización:** El usuario solo puede obtener su propio perfil (`RequestingUserId` debe coincidir con `id` en ruta)

**Parámetros de ruta:**
- `id` (Guid): Identificador único del usuario

**Respuesta 200 OK:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Alex Blasco",
  "gender": "Male",
  "email": "alejblasco@gmail.com",
  "licenseNumber": "AB123456",
  "licenseCategory": "B",
  "role": "Member",
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2026-07-06T14:22:00Z",
  "isOAuthUser": false,
  "providerName": null
}
```

**Respuestas de error:**

| Código | Descripción |
|--------|-------------|
| 401 Unauthorized | Token JWT ausente o inválido |
| 403 Forbidden | Usuario intenta acceder a perfil de otro usuario |
| 404 Not Found | Usuario no encontrado en base de datos |

---

### PUT /api/users/{id}/profile

**Descripción:** Actualiza el perfil del usuario especificado.

**Autenticación:** Requerida (JWT Bearer token)

**Autorización:** El usuario solo puede actualizar su propio perfil

**Parámetros de ruta:**
- `id` (Guid): Identificador único del usuario

**Cuerpo del request:**
```json
{
  "name": "Alex Blasco",
  "gender": "Male",
  "email": "alejblasco@gmail.com",
  "licenseNumber": "AB123456",
  "licenseCategory": "B"
}
```

**Validaciones:**
- `name`: requerido, 2-100 caracteres, patrón `^[a-zA-Z\s\-']+$`
- `gender`: requerido, valores válidos: `"Male"`, `"Female"`, `"Other"` (case-insensitive)
- `email`: requerido, formato RFC 5322, máx. 256 chars, único en sistema
- `licenseNumber`: opcional, máx. 50 caracteres
- `licenseCategory`: opcional, máx. 50 caracteres

**Restricciones OAuth:**
- Si el usuario es OAuth (`ProviderName != null`), no puede cambiar `email`. El endpoint retorna 400 Bad Request con mensaje: `"Email is managed by {ProviderName} and cannot be changed here."`

**Respuesta 200 OK:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Alex Blasco",
  "gender": "Male",
  "email": "alejblasco@gmail.com",
  "licenseNumber": "AB123456",
  "licenseCategory": "B",
  "role": "Member",
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2026-07-06T16:45:00Z",
  "isOAuthUser": false,
  "providerName": null
}
```

**Respuestas de error:**

| Código | Descripción |
|--------|-------------|
| 400 Bad Request | Validación fallida (email duplicado, OAuth intenta cambiar email, formato inválido) |
| 401 Unauthorized | Token JWT ausente o inválido |
| 403 Forbidden | Usuario intenta modificar perfil de otro usuario |
| 404 Not Found | Usuario no encontrado |

---

### PUT /api/users/{id}/password

**Descripción:** Cambia la contraseña del usuario especificado (solo autenticación local).

**Autenticación:** Requerida (JWT Bearer token)

**Autorización:** El usuario solo puede cambiar su propia contraseña

**Parámetros de ruta:**
- `id` (Guid): Identificador único del usuario

**Cuerpo del request:**
```json
{
  "currentPassword": "oldPassword123",
  "newPassword": "newSecurePass456",
  "confirmNewPassword": "newSecurePass456"
}
```

**Validaciones:**
- `currentPassword`: requerido, no vacío
- `newPassword`: requerido, mínimo 8 caracteres
- `confirmNewPassword`: debe coincidir con `newPassword` (validado en controller)

**Restricciones OAuth:**
- Si el usuario es OAuth, el endpoint retorna 400 Bad Request con mensaje: `"Password is managed by {ProviderName} and cannot be changed here."`

**Respuesta 200 OK:**
```json
{
  "message": "Password changed successfully.",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "expiresIn": 1800
}
```

**Campos de respuesta:**
- `accessToken`: Nuevo JWT para autenticación (válido por 30 minutos, configurable)
- `refreshToken`: Nuevo token de renovación (válido por 7 días, configurable)
- `expiresIn`: Tiempo de expiración del access token en segundos

**Respuestas de error:**

| Código | Descripción |
|--------|-------------|
| 400 Bad Request | Contraseñas no coinciden, OAuth intenta cambiar contraseña, validación fallida |
| 401 Unauthorized | Token JWT inválido, contraseña actual incorrecta |
| 403 Forbidden | Usuario intenta cambiar contraseña de otro usuario |
| 404 Not Found | Usuario no encontrado |

---

## Configuración

### Claves de appsettings.json

No se añadieron nuevas claves de configuración. Se utilizan las existentes:

| Clave | Tipo | Valor | Descripción |
|-------|------|-------|-------------|
| `ApiSettings:BaseUrl` | string | `https://localhost:5001` | URL base de la API para HTTP client en Web layer |
| `Authentication:JwtSettings:AccessTokenExpirationMinutes` | int | `30` | Expiración de access token (usado al generar nuevos tokens tras cambio de contraseña) |
| `Authentication:JwtSettings:RefreshTokenExpirationDays` | int | `7` | Expiración de refresh token |

### Secretos / Azure Key Vault

No se requieren nuevos secretos. Se utilizan los existentes del sistema de autenticación:
- `Authentication:JwtSettings:Secret` (clave de firma de JWT)
- ConnectionString a base de datos

---

## Cambios en Base de Datos

### Migración EF Core

**No se requiere migración.** Todos los campos utilizados ya existen en la entidad `User`:
- `Name` (string)
- `Gender` (Gender enum)
- `Email` (string, índice único)
- `LicenseNumber` (string?, nullable)
- `LicenseCategory` (string?, nullable)
- `PasswordHash` (string?, nullable)
- `ExternalProviderId` (string?, nullable — para OAuth)
- `ProviderName` (string?, nullable — para OAuth)
- `RefreshToken` (string?, nullable)
- `RefreshTokenExpiryTime` (DateTime?, nullable)
- `Role` (Role enum)
- `CreatedAt` (DateTime, heredado de BaseEntity)
- `UpdatedAt` (DateTime?, heredado de BaseEntity)

### Cambios en Esquema

**Ninguno.** Se reutilizan campos existentes de US-27 y US-28.

---

## Dependencias Añadidas

**Ninguna dependencia nueva.** Se utilizan paquetes ya referenciados:

| Proyecto | Paquetes Utilizados | Propósito |
|----------|---------------------|-----------|
| Application | `FluentValidation`, `MediatR` | Validación de comandos, patrón CQRS |
| Infrastructure | `BCrypt.Net-Next` (existente) | Hashing de contraseñas |
| Web | `System.Net.Http.Json` (built-in) | HTTP client para llamadas a API |

---

## Testing

### Cobertura de Tests Unitarios

**94% de cobertura** (objetivo: 90%)

**68 tests unitarios** creados en 5 archivos:

1. **UpdateProfileCommandHandlerTests.cs** (396 líneas)
   - Actualización exitosa (usuario local y OAuth)
   - Fallo de autorización (RequestingUserId != UserId)
   - Usuario no encontrado
   - Violación de unicidad de email
   - Usuario OAuth intenta cambiar email (bloqueado)
   - Validación de enum Gender inválido

2. **UpdateProfileCommandValidatorTests.cs** (355 líneas)
   - Validaciones de `Name` (mínimo, máximo, patrón)
   - Validaciones de `Email` (formato RFC 5322, unicidad)
   - Validaciones de `Gender` (enum válido)
   - Validaciones de `LicenseNumber` / `LicenseCategory` (longitud máxima)

3. **ChangePasswordCommandHandlerTests.cs** (471 líneas)
   - Cambio exitoso con generación de nuevos tokens
   - Fallo de autorización
   - Usuario no encontrado
   - Contraseña actual incorrecta
   - Usuario OAuth intenta cambiar contraseña (bloqueado)
   - Generación y hasheo de refresh token
   - Verificación de claims en nuevo access token

4. **ChangePasswordCommandValidatorTests.cs** (183 líneas)
   - Validaciones de contraseña actual (requerida)
   - Validaciones de nueva contraseña (mínimo 8 caracteres)

5. **GetUserProfileQueryHandlerTests.cs** (225 líneas)
   - Obtención exitosa de perfil
   - Usuario no encontrado
   - Flag `IsOAuthUser` calculado correctamente
   - Fallback de `UpdatedAt` a `CreatedAt` si null

**Escenarios de alta prioridad cubiertos:**
- ✅ Autorización (dos capas)
- ✅ Protección de usuarios OAuth
- ✅ Unicidad de email
- ✅ Verificación de contraseña actual
- ✅ Manejo de errores (KeyNotFoundException, UnauthorizedAccessException, InvalidOperationException)

**Convenciones aplicadas:**
- ✅ Estructura Arrange / Act / Assert en todos los tests
- ✅ XML doc comments en inglés en todos los métodos de test
- ✅ NSubstitute para mocks (IApplicationDbContext, IPasswordHasher, ITokenService)
- ✅ FluentAssertions para aserciones legibles

### Escenarios de Tests de Integración

**Status:** Fase saltada explícitamente (jump to QUALITY_REVIEW).

**Escenarios recomendados para implementación futura:**
- End-to-end flow: GET → PUT → GET con verificación de persistencia
- Validación de JWT real (no mock)
- Email uniqueness enforcement a nivel de base de datos (constraint check)
- Concurrent updates con base de datos real
- OAuth flow completo con TestContainers

---

## Limitaciones Conocidas

1. **Email sin verificación**
   - Los cambios de email se aplican inmediatamente sin confirmación
   - Riesgo: usuario puede perder acceso si escribe mal el nuevo email
   - Mitigación: validación de formato estricta, unicidad validada
   - Backlog: US-XX para implementar flujo de verificación con token (Sprint 3+)

2. **Sin control de concurrencia optimista**
   - Actualizaciones simultáneas resultan en last-write-wins
   - Riesgo: cambios de una sesión pueden ser sobrescritos por otra
   - Probabilidad: baja (usuarios típicamente tienen una sola sesión activa)
   - Backlog: Añadir campo `RowVersion` a entidad User

3. **Sin rate limiting**
   - Endpoints de perfil no tienen límite de requests por minuto
   - Riesgo: abuso o DoS por usuario comprometido
   - Mitigación: autenticación requerida (reduce superficie de ataque)
   - Acción: Añadir ASP.NET Core rate limiting antes de producción

4. **Password policy débil**
   - Solo se requiere mínimo 8 caracteres, sin complejidad
   - Decisión: mantener política actual (Gate 2 decision)
   - Mitigación: BCrypt work factor 12 mitiga fuerza bruta
   - Mayoría de usuarios usa OAuth (no tienen password local)

5. **Advertencia de cambios no guardados**
   - No implementada debido a limitación de API `NavigationManager.LocationChanging` en versión actual de .NET
   - Puede añadirse en futuro sprint con JavaScript interop

---

## Consideraciones de Seguridad

### Resultados de Security Review

**Status:** ✅ PASSED (Gate 3 aprobado)

**Resumen:**
- 0 vulnerabilidades críticas
- 0 vulnerabilidades altas
- 3 vulnerabilidades medias (todas aceptadas para MVP)
- 1 vulnerabilidad baja
- 2 informativas

### Fortalezas de Seguridad

✅ **Autorización robusta (dos capas)**
- Controller valida JWT y `RequestingUserId` == `id` en ruta
- Handler valida `RequestingUserId` == `UserId` en comando
- Logs de intentos no autorizados (nivel Warning)

✅ **Criptografía correcta**
- Passwords hasheados con BCrypt (work factor 12)
- Refresh tokens hasheados antes de almacenar en BD
- No se loguean passwords ni tokens (solo UserIds)

✅ **Protección contra inyección SQL**
- Solo EF Core con consultas parametrizadas (LINQ)
- No hay uso de `FromSqlRaw` o `ExecuteSqlInterpolated`

✅ **Seguridad Blazor**
- Sin uso de `MarkupString` (riesgo XSS)
- Auto-escape de Blazor habilitado
- Sin JavaScript interop con input no sanitizado

✅ **Validación de entrada**
- FluentValidation en servidor (todos los comandos)
- Blazor DataAnnotationsValidator en cliente
- Validación de email con regex custom (más estricta que default)

✅ **Secretos**
- No se detectaron secretos hardcodeados en código
- Sin credentials en logs
- Sin PII en URLs (todo en request body)

### Hallazgos de Seguridad (Aceptados)

**MEDIUM-1: Email sin verificación**
- Riesgo: cambio no verificado puede causar pérdida de acceso
- Status: Aceptado en Gate 2, backlog para Sprint 3+
- Mitigación: unicidad validada, usuarios OAuth protegidos

**MEDIUM-2: Sin protección contra actualizaciones concurrentes**
- Riesgo: last-write-wins en actualizaciones simultáneas
- Status: Aceptado (baja probabilidad en MVP)
- Mitigación: sesión única por usuario típicamente

**MEDIUM-3: Sin rate limiting**
- Riesgo: abuso por cuenta comprometida
- Status: Recomendado antes de producción
- Mitigación: autenticación requerida, logs de auditoría

**LOW-1: Password policy débil**
- Riesgo: usuarios pueden elegir contraseñas débiles
- Status: Aceptado por diseño (Gate 2 decision)
- Mitigación: BCrypt, mayoría usa OAuth

### Cumplimiento OWASP Top 10

- ✅ **A01 — Broken Access Control:** Autorización en dos capas, sin IDOR
- ✅ **A02 — Cryptographic Failures:** BCrypt, tokens hasheados, sin MD5/SHA1
- ✅ **A03 — Injection:** EF Core parametrizado, sin SQL injection
- ⚠️ **A04 — Insecure Design:** Email sin verificación, sin rate limiting (aceptado)
- ✅ **A05 — Security Misconfiguration:** No debug endpoints, errores sanitizados
- ✅ **A06 — Vulnerable Components:** 4 Moderate en dependencias de test (no producción)
- ✅ **A07 — Authentication Failures:** JWT correcto, password actual requerido
- ✅ **A08 — Software and Data Integrity:** DTOs inmutables, no unsafe deserialization
- ✅ **A09 — Logging and Monitoring:** No PII en logs, eventos de seguridad logueados
- ✅ **A10 — SSRF:** No aplica (no hay requests basados en input de usuario)

---

## Archivos Creados/Modificados

### Archivos Creados (19)

**Shared Layer:**
- `SportsClubEventManager.Shared/DTOs/UserProfileDto.cs`
- `SportsClubEventManager.Shared/DTOs/UpdateProfileRequest.cs`
- `SportsClubEventManager.Shared/DTOs/ChangePasswordRequest.cs`
- `SportsClubEventManager.Shared/DTOs/ChangePasswordResponse.cs`

**Application Layer:**
- `SportsClubEventManager.Application/Users/Queries/GetUserProfile/GetUserProfileQuery.cs`
- `SportsClubEventManager.Application/Users/Queries/GetUserProfile/GetUserProfileQueryHandler.cs`
- `SportsClubEventManager.Application/Users/Commands/UpdateProfile/UpdateProfileCommand.cs`
- `SportsClubEventManager.Application/Users/Commands/UpdateProfile/UpdateProfileCommandHandler.cs`
- `SportsClubEventManager.Application/Users/Commands/UpdateProfile/UpdateProfileCommandValidator.cs`
- `SportsClubEventManager.Application/Users/Commands/ChangePassword/ChangePasswordCommand.cs`
- `SportsClubEventManager.Application/Users/Commands/ChangePassword/ChangePasswordCommandHandler.cs`
- `SportsClubEventManager.Application/Users/Commands/ChangePassword/ChangePasswordCommandValidator.cs`

**API Layer:**
- `SportsClubEventManager.Api/Controllers/UsersController.cs`

**Web Layer:**
- `SportsClubEventManager.Web/Services/IUserProfileService.cs`
- `SportsClubEventManager.Web/Services/UserProfileService.cs`
- `SportsClubEventManager.Web/Components/Pages/UserProfile.razor`
- `SportsClubEventManager.Web/Components/Shared/ChangePasswordModal.razor`

**Tests:**
- `SportsClubEventManager.Application.Tests/Users/Commands/UpdateProfile/UpdateProfileCommandHandlerTests.cs`
- `SportsClubEventManager.Application.Tests/Users/Commands/UpdateProfile/UpdateProfileCommandValidatorTests.cs`
- `SportsClubEventManager.Application.Tests/Users/Commands/ChangePassword/ChangePasswordCommandHandlerTests.cs`
- `SportsClubEventManager.Application.Tests/Users/Commands/ChangePassword/ChangePasswordCommandValidatorTests.cs`
- `SportsClubEventManager.Application.Tests/Users/Queries/GetUserProfile/GetUserProfileQueryHandlerTests.cs`

### Archivos Modificados (2)

- `SportsClubEventManager.Web/Program.cs` — Añadida inyección de dependencia para `IUserProfileService`
- (La interfaz `IUserRepository` mencionada en Implementation Report no fue encontrada en el commit real — posiblemente implementada inline en el DbContext)

**Total:** 23 archivos, 2,880 líneas añadidas

---

## Próximos Pasos

1. **Commit y push** de la rama `feature/US-29-user-profile-management`
2. **Esperar CI/CD** pipeline (build, tests, análisis)
3. **Ejecutar** `@orchestrator ci-passed` una vez que CI complete exitosamente
4. **Crear Pull Request** hacia `master` (se recomienda tras CI_VERIFICATION)
5. **Code review humano** (opcional, ya pasó Code Review Agent)
6. **Merge** a master tras aprobación

### Backlog de Mejoras Futuras

- **Alta prioridad (antes de producción):**
  - Añadir rate limiting a endpoints de perfil (MEDIUM-3)

- **Media prioridad (Sprint 3+):**
  - Implementar flujo de verificación de email (MEDIUM-1)
  - Añadir control de concurrencia optimista con RowVersion (MEDIUM-2)

- **Baja prioridad (backlog):**
  - Fortalecer password policy con requisitos de complejidad (LOW-1)
  - Añadir advertencia de cambios no guardados con JavaScript interop
  - Upgrade de paquetes OpenTelemetry en Web.Tests (4 vulnerabilidades Moderate)

---

**Documento generado por:** Documentation Agent  
**Fecha:** 2026-07-07  
**Basado en:** Requirements Analysis, Impact Analysis, Implementation Report, Code Review, Quality Review, Security Review de US-29
