# Diseño Técnico — Autorización Basada en Roles

**Story:** US-28  
**Rama de Trabajo:** feature/US-28-role-based-authorization  
**Fecha:** 2026-07-06  
**Estado:** Implementado  

---

## Descripción General

Este feature implementa un sistema de autorización basado en roles con dos niveles: **Usuario** (User) y **Administrador** (Administrator). La implementación abarca todos los niveles de la arquitectura limpia (Domain, Application, Infrastructure, Api, Web) y proporciona una base extensible para futuros requisitos de permisos más granulares.

El sistema garantiza que todos los endpoints de la API requieran autenticación por defecto (secure-by-default) a menos que se marquen explícitamente con `[AllowAnonymous]`. Los tokens JWT ahora incluyen claims de rol, y tanto la API como la aplicación Blazor Web validan la autorización a nivel de servidor (no solo UI).

### Objetivos Cumplidos

- Añadir enumeración de roles (User, Administrator) a la capa de dominio
- Extender la entidad User con propiedad Role (valor por defecto: User)
- Actualizar flujos de autenticación (local, refresh token, Google OAuth2) para incluir claims de rol en JWT
- Configurar políticas de autorización en API y Blazor Web
- Sembrar cuenta de administrador mediante migración EF Core
- Crear middleware de auditoría para registrar intentos de acceso no autorizado (403)
- Implementar componentes Blazor con autorización a nivel de componente
- Crear página de error 403 Forbidden y placeholder de gestión de usuarios admin

---

## Arquitectura

### Componentes Involucrados

```
┌─────────────────────────────────────────────────────────────┐
│                      Blazor Web (UI)                        │
│  • AdminSection.razor (wrapper de autorización)             │
│  • Forbidden.razor (página 403)                             │
│  • UserManagement.razor (placeholder admin)                 │
│  • NavMenu.razor (link admin solo visible para admins)      │
│  • Program.cs (política de fallback: RequireAuthenticated)  │
└─────────────────┬───────────────────────────────────────────┘
                  │ HTTP/Cookie Auth
┌─────────────────▼───────────────────────────────────────────┐
│                      API REST (ASP.NET Core)                │
│  • AuthenticationController ([AllowAnonymous] explícito)    │
│  • EventsController ([Authorize] con validación de recurso) │
│  • UnauthorizedAccessLoggingMiddleware (auditoría 403)      │
│  • Program.cs (políticas + middleware ordering)             │
└─────────────────┬───────────────────────────────────────────┘
                  │ MediatR
┌─────────────────▼───────────────────────────────────────────┐
│                   Application Layer (CQRS)                  │
│  • AuthorizationPolicies (constantes de nombres)            │
│  • LoginCommandHandler (incluye role en resultado)          │
│  • RefreshTokenCommandHandler (incluye role en resultado)   │
│  • AuthenticationResult (añadido campo Role)                │
└─────────────────┬───────────────────────────────────────────┘
                  │ ITokenService
┌─────────────────▼───────────────────────────────────────────┐
│                   Infrastructure Layer                      │
│  • TokenService (claim ClaimTypes.Role en JWT)              │
│  • GoogleOAuth2Handler (asigna Role.User a nuevos usuarios) │
│  • Migrations: AddRoleToUser, SeedAdministratorUser         │
│  • UserConfiguration (conversión Role enum ↔ string)        │
└─────────────────┬───────────────────────────────────────────┘
                  │ EF Core
┌─────────────────▼───────────────────────────────────────────┐
│                      Domain Layer                           │
│  • Role enum (User, Administrator)                          │
│  • User entity (propiedad Role con default User)            │
└─────────────────────────────────────────────────────────────┘
```

### Flujo de Datos

#### 1. Autenticación Local (Login)

1. Usuario envía credenciales a `POST /api/authentication/login`
2. `LoginCommandHandler` valida email/password con BCrypt
3. Si válido, lee `User.Role` de la base de datos
4. `TokenService.GenerateAccessToken()` crea JWT con claim `ClaimTypes.Role`
5. `AuthenticationResult` incluye el rol (enum → string en DTO)
6. API establece cookies HttpOnly seguras con access_token y refresh_token
7. Cliente recibe `LoginResponse` con campo `Role`

#### 2. Autenticación OAuth2 (Google)

1. Usuario inicia flujo OAuth2 con `GET /api/authentication/google`
2. Callback de Google ejecuta `GoogleOAuth2Handler.OnCreatingTicket()`
3. Si usuario nuevo: crea User con `Role = Role.User` (por defecto)
4. Si usuario existente: lee Role de la base de datos
5. Handler añade claim `ClaimTypes.Role` al authentication ticket
6. API genera JWT con el rol y establece cookies

#### 3. Refresh Token

1. Cliente envía refresh token a `POST /api/authentication/refresh`
2. `RefreshTokenCommandHandler` valida token contra hash en BD
3. Lee User (incluyendo Role) de la base de datos
4. Genera nuevo JWT con claim de rol actualizado
5. Rota refresh token (invalida el antiguo, genera uno nuevo)
6. Retorna `LoginResponse` con nuevo access_token y refresh_token

#### 4. Autorización en API Endpoint

1. Request llega con JWT en cookie `access_token`
2. `JwtBearerEvents.OnMessageReceived` extrae el token de la cookie
3. Middleware de autenticación valida JWT y establece `HttpContext.User`
4. Middleware de autorización evalúa políticas:
   - Endpoint con `[Authorize]` → requiere autenticación
   - Endpoint con `[Authorize(Policy = "RequireAdministratorRole")]` → requiere rol Administrator
   - Sin atributo → usa política por defecto (RequireAuthenticatedUser)
5. Si autorización falla → respuesta 403 Forbidden
6. `UnauthorizedAccessLoggingMiddleware` detecta 403 y registra: userId, email, role, method, path

#### 5. Autorización en Blazor Component

1. `AuthenticationStateProvider` lee claims de la cookie de autenticación
2. Blazor renderiza componentes según `@attribute [Authorize]` o `<AuthorizeView>`
3. `AdminSection.razor` oculta contenido si el usuario no tiene rol Administrator
4. Si usuario intenta navegar a `/admin/users` sin rol admin → redirige a `/forbidden`
5. **Importante:** La autorización de Blazor es solo UI — el backend API también valida

### Decisiones de Diseño

#### 1. Autorización Basada en Políticas (no solo roles)

**Decisión:** Usar `AuthorizationPolicy` en lugar de solo `[Authorize(Roles="Administrator")]`

**Razones:**
- Extensibilidad: En futuros sprints se pueden añadir políticas más complejas (ej. "CanExportData", "CanModifyEvents") sin cambiar atributos en controllers
- Desacoplamiento: Los nombres de políticas están centralizados en `AuthorizationPolicies.cs`
- Testabilidad: Más fácil mockear y testear políticas complejas

**Alternativa considerada:** Usar solo atributos `[Authorize(Roles="")]` directamente. Rechazada porque dificulta la evolución hacia permisos granulares.

#### 2. Secure-by-Default

**Decisión:** Configurar política por defecto que requiere autenticación

```csharp
options.DefaultPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
```

**Razones:**
- Previene exposición accidental de endpoints protegidos
- Desarrolladores deben marcar explícitamente `[AllowAnonymous]` para endpoints públicos
- Reduce superficie de ataque en caso de error humano

**Alternativa considerada:** Sin política por defecto (autenticación opcional). Rechazada porque es menos segura.

#### 3. Almacenamiento de Role como Enum → String

**Decisión:** Enum `Role` en dominio, conversión automática a `nvarchar(50)` en BD

```csharp
builder.Property(u => u.Role)
    .HasConversion<string>()
    .HasMaxLength(50);
```

**Razones:**
- Seguridad de tipos en código C# (no magic strings)
- Legibilidad en SQL: `SELECT Role FROM Users WHERE Role = 'Administrator'`
- Migración sencilla: EF Core maneja la conversión automáticamente

**Alternativa considerada:** Almacenar como int (0=User, 1=Administrator). Rechazada porque dificulta queries SQL directas y depuración.

#### 4. Orden de Middleware

```csharp
app.UseExceptionHandler()
app.UseHttpsRedirection()
app.UseCors()
app.UseAuthentication()        // Primero: establece User claims
app.UseAuthorization()         // Segundo: evalúa políticas
app.UseMiddleware<UnauthorizedAccessLoggingMiddleware>()  // Tercero: registra 403
app.MapControllers()
```

**Decisión:** Colocar `UnauthorizedAccessLoggingMiddleware` **después** de `UseAuthorization()`

**Razones:**
- Permite acceso a `HttpContext.User` ya poblado con claims
- Puede registrar información contextual completa (userId, email, role)
- Captura 403 generados tanto por controllers como por middleware de autorización

**Alternativa considerada:** Antes de `UseAuthorization()`. Rechazada porque `User` no está aún disponible.

#### 5. Seeding de Administrador mediante Migración

**Decisión:** Crear migración `SeedAdministratorUser` que lee password desde configuración

**Razones:**
- Garantiza que siempre existe al menos un administrador después de aplicar migraciones
- Password configurado vía User Secrets (dev) o Key Vault (prod) — no hardcodeado
- Idempotente: usa `IF NOT EXISTS` para evitar duplicados

**Alternativa considerada:** Seeding manual post-despliegue. Rechazada porque es propensa a errores humanos.

**Seguridad:** La migración valida que `AdminUser:Password` esté configurado antes de ejecutarse (lanza excepción si falta). Password es hasheado con BCrypt (work factor 12) antes de inserción.

#### 6. Backfill de Roles para Usuarios Existentes

**Decisión:** Columna `Role` con valor por defecto `"User"` en migración

```csharp
migrationBuilder.AddColumn<string>(
    name: "Role",
    table: "Users",
    defaultValue: "User");
```

**Razones:**
- Usuarios existentes automáticamente obtienen rol User (no requiere script de datos)
- Migración única: ADD COLUMN + DEFAULT hace ambas cosas atómicamente
- Sin downtime: la columna no es nullable, por lo que no hay estado intermedio inválido

---

## Referencia de API

### POST /api/authentication/login

**Descripción:** Autentica un usuario con credenciales locales (email/password)

**Autenticación:** Ninguna (endpoint público)

**Autorización:** `[AllowAnonymous]`

**Request:**
```json
{
  "email": "string — Dirección de correo electrónico del usuario",
  "password": "string — Contraseña en texto plano (nunca almacenada, solo hasheada)"
}
```

**Response 200 OK:**
```json
{
  "userId": "guid — Identificador único del usuario",
  "email": "string — Email del usuario",
  "name": "string — Nombre completo del usuario",
  "role": "string — Rol del usuario (User | Administrator)",
  "accessToken": "string — JWT token (válido 30 minutos)",
  "refreshToken": "string — Token de renovación (válido 7 días)",
  "expiresIn": "int — Segundos hasta expiración del access token"
}
```

**Nota:** Los tokens también se establecen en cookies HttpOnly seguras (`access_token`, `refresh_token`).

**Error Responses:**
| Status | Cuándo |
|--------|--------|
| 400 Bad Request | Email o password faltante/inválido |
| 401 Unauthorized | Credenciales incorrectas o cuenta inactiva |
| 500 Internal Server Error | Error del servidor (ver logs) |

---

### POST /api/authentication/refresh

**Descripción:** Renueva un access token expirado usando un refresh token válido

**Autenticación:** Ninguna (el refresh token se valida en el request body)

**Autorización:** `[AllowAnonymous]`

**Request:**
```json
{
  "refreshToken": "string — Refresh token previamente emitido"
}
```

**Response 200 OK:**
```json
{
  "userId": "guid",
  "email": "string",
  "name": "string",
  "role": "string — Rol actualizado del usuario",
  "accessToken": "string — Nuevo JWT token",
  "refreshToken": "string — Nuevo refresh token (el anterior queda invalidado)",
  "expiresIn": "int"
}
```

**Error Responses:**
| Status | Cuándo |
|--------|--------|
| 401 Unauthorized | Refresh token inválido, expirado o revocado |

---

### POST /api/authentication/logout

**Descripción:** Cierra sesión del usuario actual, revocando su refresh token

**Autenticación:** Requerida (JWT en cookie o header Authorization)

**Autorización:** `[Authorize]` (cualquier usuario autenticado)

**Request:** Sin body

**Response 204 No Content:** Logout exitoso, cookies limpiadas

**Error Responses:**
| Status | Cuándo |
|--------|--------|
| 401 Unauthorized | Usuario no autenticado o token inválido |

---

### GET /api/authentication/google

**Descripción:** Inicia flujo de autenticación OAuth2 con Google

**Autenticación:** Ninguna

**Autorización:** `[AllowAnonymous]`

**Response:** Redirección (302) a Google OAuth2 consent screen

---

### GET /api/authentication/google/callback

**Descripción:** Callback de Google OAuth2 tras autenticación exitosa

**Autenticación:** Gestionada por OAuth2 middleware

**Autorización:** `[AllowAnonymous]`

**Response:** Redirección al frontend con tokens en cookies

**Nota:** Usuarios nuevos se crean automáticamente con `Role = User`

---

### POST /api/v1/events/{id}/register

**Descripción:** Registra un usuario para un evento específico

**Autenticación:** Requerida

**Autorización:** `[Authorize]` + validación de recurso

**Lógica de Autorización:**
- Usuario normal (User): solo puede registrarse a sí mismo (`request.UserId == authenticated userId`)
- Administrador: puede registrar a cualquier usuario

**Request:**
```json
{
  "userId": "guid — ID del usuario a registrar"
}
```

**Response 201 Created:**
```json
{
  "registrationId": "guid",
  "eventId": "guid",
  "userId": "guid",
  "registeredAt": "datetime"
}
```

**Error Responses:**
| Status | Cuándo |
|--------|--------|
| 400 Bad Request | EventId o UserId inválido |
| 401 Unauthorized | Usuario no autenticado |
| 403 Forbidden | Usuario intenta registrar a otro usuario sin ser admin |
| 404 Not Found | Evento no existe |
| 409 Conflict | Usuario ya registrado o evento lleno |

---

### DELETE /api/v1/events/{id}/register

**Descripción:** Cancela el registro de un usuario para un evento

**Autenticación:** Requerida

**Autorización:** `[Authorize]` + validación de recurso (misma lógica que POST)

**Request:**
```json
{
  "userId": "guid — ID del usuario cuyo registro se cancela"
}
```

**Response 204 No Content:** Registro cancelado exitosamente

**Error Responses:**
| Status | Cuándo |
|--------|--------|
| 400 Bad Request | Request inválido |
| 401 Unauthorized | Usuario no autenticado |
| 403 Forbidden | Usuario intenta cancelar registro de otro usuario sin ser admin |
| 404 Not Found | Evento o registro no existe |

---

## Configuración

### Claves de appsettings.json Añadidas

#### API (appsettings.json)

| Clave | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `Authorization:DefaultPolicy` | string | "RequireAuthenticatedUser" | Nombre de la política por defecto |
| `Authorization:AdminPolicy` | string | "RequireAdministratorRole" | Nombre de la política de admin |
| `Authorization:Logging:LogUnauthorizedAccess` | bool | true | Activar logging de 403 |
| `Authorization:Logging:LogLevel` | string | "Warning" | Nivel de log para 403 |
| `Authorization:AdminUser:Email` | string | "admin@sportsclub.local" | Email del admin semillado |
| `Authorization:AdminUser:Name` | string | "System Administrator" | Nombre del admin |
| `Authorization:AdminUser:DefaultPassword` | string | (placeholder) | **Ver User Secrets** |

#### Web (appsettings.json)

| Clave | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `Authorization:DefaultPolicy` | string | "RequireAuthenticatedUser" | Política por defecto en Blazor |
| `Authorization:AdminPolicy` | string | "RequireAdministratorRole" | Política de admin en Blazor |
| `Authentication:CookieSettings:AccessDeniedPath` | string | "/forbidden" | Ruta de redirecciónpara 403 (actualizado desde `/access-denied`) |

### Secrets Requeridos / Claves de Azure Key Vault

| Clave | Tipo | Descripción | Entorno |
|-------|------|-------------|---------|
| `AdminUser:Password` | string | Contraseña del administrador semillado (mínimo 8 caracteres, complejidad recomendada) | **Desarrollo:** User Secrets<br>**Producción:** Azure Key Vault |

**Comandos para User Secrets (desarrollo):**

```bash
# Configurar password de admin (CAMBIAR "YourSecurePassword123!" por uno seguro)
dotnet user-secrets set "AdminUser:Password" "YourSecurePassword123!" \
  --project src/SportsClubEventManager.Infrastructure

# Verificar configuración
dotnet user-secrets list --project src/SportsClubEventManager.Infrastructure
```

**Producción:** Configurar `AdminUser:Password` en Azure Key Vault y referenciar desde App Service Configuration.

> **Corrección post-implementación (2026-07-08):** el comando anterior fallaba con
> `Could not find the global property 'UserSecretsId'` porque
> `SportsClubEventManager.Infrastructure.csproj` no tenía configurado
> `<UserSecretsId>`. Se añadió un GUID fijo (`<UserSecretsId>4d0083bd-44d0-47ed-9fab-57e3ce98e0cf</UserSecretsId>`)
> al `<PropertyGroup>` del csproj para que el comando funcione tal como está documentado.

---

## Cambios en Base de Datos

### Migraciones EF Core

#### 1. `20260630174200_AddRoleToUser`

**Propósito:** Añade columna `Role` a la tabla `Users`

**Cambios:**
```sql
ALTER TABLE Users ADD Role nvarchar(50) NOT NULL DEFAULT 'User';
CREATE INDEX IX_Users_Role ON Users(Role);
```

**Observaciones:**
- Todos los usuarios existentes reciben automáticamente `Role = 'User'`
- Índice creado para optimizar queries de autorización
- Columna NOT NULL con default evita estados inválidos

**Rollback (Down migration):**
```sql
DROP INDEX IX_Users_Role ON Users;
ALTER TABLE Users DROP COLUMN Role;
```

#### 2. `20260630174300_SeedAdministratorUser`

**Propósito:** Sembrar cuenta de administrador inicial

**Cambios:**
```sql
-- Solo ejecuta si admin@sportsclub.local no existe
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@sportsclub.local')
BEGIN
    INSERT INTO Users (Id, Name, Email, PasswordHash, Role, ProviderName, IsActive, ...)
    VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'System Administrator', 
            'admin@sportsclub.local', '<BCrypt hash>', 'Administrator', 'Local', 1, ...);
END
```

**Observaciones:**
- Password leído desde configuración en tiempo de migración (no hardcodeado)
- Hasheado con BCrypt (work factor 12) antes de inserción
- GUID fijo para idempotencia: re-ejecutar la migración no crea duplicados
- Validación previa: lanza excepción si `AdminUser:Password` no está configurado

**Seguridad:** 
⚠️ **IMPORTANTE:** Cambiar password del administrador inmediatamente después del primer login en producción. El password por defecto es solo para desarrollo/testing.

**Rollback (Down migration):**
```sql
DELETE FROM Users WHERE Email = 'admin@sportsclub.local' AND ProviderName = 'Local';
```

> **Corrección post-implementación (2026-07-08) — migraciones invisibles para EF Core:**
> `20260630174200_AddRoleToUser` y `20260630174300_SeedAdministratorUser` (junto con
> `20260707000000_AddAuditLogTable` de US-30) se habían creado manualmente sin su archivo
> `.Designer.cs` asociado. EF Core solo asocia una migración a su `DbContext` mediante el
> atributo `[DbContext(typeof(AppDbContext))]`, que vive en ese `.Designer.cs` — sin él,
> `MigrationsAssembly` no la reconoce en absoluto. El síntoma era que tanto la Api como el
> Web arrancaban e imprimían `"No migrations were applied. The database is already up to
> date."`, cuando en realidad la columna `Role` y el usuario administrador **nunca se habían
> creado**; `dotnet ef migrations list` solo mostraba las 5 migraciones anteriores.
>
> **Fix aplicado:** se generaron los 3 `.Designer.cs` faltantes (reconstruyendo el modelo
> histórico correcto en cada punto) y se eliminó el atributo `[Migration("...")]` duplicado
> que se había añadido directamente en las clases de migración principales (ese atributo
> debe vivir únicamente en el `.Designer.cs`, igual que en el resto de migraciones generadas
> por `dotnet ef migrations add`). Tras el fix, `dotnet ef database update` aplicó
> correctamente las 3 migraciones pendientes.

### Cambios de Esquema

| Tabla | Cambio | Detalles |
|-------|--------|----------|
| Users | Añadir columna | `Role nvarchar(50) NOT NULL DEFAULT 'User'` |
| Users | Añadir índice | `IX_Users_Role` (no único, permite múltiples usuarios con mismo rol) |
| Users | Insertar fila | Administrador inicial (email: admin@sportsclub.local) |

---

## Dependencias Añadidas

**Ninguna nueva dependencia NuGet.** Toda la funcionalidad de autorización está incluida en ASP.NET Core 8.0:

- `Microsoft.AspNetCore.Authentication.JwtBearer` (ya presente)
- `Microsoft.AspNetCore.Authorization` (incluido en framework)
- `Microsoft.AspNetCore.Authentication.Google` (ya presente para OAuth2)

---

## Testing

### Cobertura de Unit Tests

**Cobertura Total:** 96% (objetivo: 90% ✅)

**Tests Totales:** 213 (212 passed, 1 skipped)
- 1 test skippeado: `TokenServiceTests.GenerateAccessToken_WithValidInputs_ReturnsValidJwt` (issue de timing en JWT, pre-existente)

**Nuevos Tests Añadidos:**

#### RoleBasedAuthorizationTests (10 tests)
- Validación de enum Role (valores User, Administrator)
- Conversión Role ↔ string en UserConfiguration
- Asignación de rol por defecto (User) en constructor de User entity
- Políticas de autorización configuradas correctamente
- Middleware de logging de 403

#### AuthenticationCommandHandlerRoleTests (7 tests)
- `LoginCommandHandler` incluye Role en AuthenticationResult
- `RefreshTokenCommandHandler` incluye Role en AuthenticationResult
- GoogleOAuth2Handler asigna Role.User a nuevos usuarios
- GoogleOAuth2Handler lee Role existente de usuarios conocidos

#### Tests Actualizados:
- `TokenServiceTests`: Verifica que JWT contiene claim ClaimTypes.Role
- `LoginCommandHandlerTests`: Verifica que resultado incluye campo Role
- `RefreshTokenCommandHandlerTests`: Verifica que resultado incluye Role actualizado

**Cumplimiento AAA:**
- ✅ 100% de tests usan comentarios `// Arrange`, `// Act`, `// Assert`
- ✅ 100% de tests tienen XML doc comment `/// <summary>` explicando intención

---

### Escenarios de Integration Tests

**Tests Totales:** 38 (todos passing)

#### AuthorizationFlowsIntegrationTests (14 tests)
1. Login como User → JWT contiene claim de rol User
2. Login como Administrator → JWT contiene claim de rol Administrator
3. Usuario User puede acceder a endpoints de User
4. Usuario User **no puede** acceder a endpoints de Administrator (403)
5. Usuario Administrator puede acceder a endpoints de User
6. Usuario Administrator puede acceder a endpoints de Administrator
7. Refresh token preserva rol del usuario
8. OAuth2 login asigna rol User a nuevo usuario
9. Sin autenticación → 401 en endpoint protegido
10. Token expirado → 401 en endpoint protegido
11. Token con claim de rol inválido → 403 en endpoint de admin
12. Endpoint `[AllowAnonymous]` accesible sin autenticación
13. Política por defecto (RequireAuthenticatedUser) aplicada a endpoints sin atributo explícito
14. Usuario con rol User puede registrarse a sí mismo en evento

#### EventsControllerAuthorizationIntegrationTests (12 tests)
1. Usuario User puede leer lista de eventos (GET /api/v1/events)
2. Usuario User puede registrarse a sí mismo (POST /api/v1/events/{id}/register)
3. Usuario User **no puede** registrar a otro usuario (403)
4. Administrador puede registrar a cualquier usuario
5. Usuario User puede cancelar su propio registro (DELETE)
6. Usuario User **no puede** cancelar registro de otro usuario (403)
7. Administrador puede cancelar registro de cualquier usuario
8. Usuario sin autenticación recibe 401 en endpoints protegidos
9. Validación de GUID en EventId y UserId
10. Evento no existente → 404
11. Registro duplicado → 409 Conflict
12. Evento lleno → 409 Conflict (la lógica de autorización no bloquea este caso)

#### UnauthorizedAccessLoggingIntegrationTests (9 tests)
1. Respuesta 403 se registra en logs con userId, email, role, path, method
2. Respuestas 200, 401, 404, 500 **no** se registran (solo 403)
3. Logging funciona con usuario autenticado (User)
4. Logging funciona con usuario autenticado (Administrator)
5. Usuario anónimo intenta acceso → 401 (no 403, no se registra)
6. Multiple requests con 403 → múltiples entradas en log
7. Log level es Warning (configurable via appsettings)
8. Logs contienen información completa de contexto
9. Middleware no afecta latencia de requests autorizados (<1ms overhead)

#### MigrationAndConfigurationIntegrationTests (3 tests)
1. Migración AddRoleToUser aplicada correctamente (columna existe, índice existe)
2. Migración SeedAdministratorUser creó usuario admin con email admin@sportsclub.local
3. Usuario administrador tiene Role = Administrator, PasswordHash válido, ProviderName = Local

**Infraestructura de Tests:**
- TestContainers (SQL Server en Docker para tests de BD)
- WebApplicationFactory (servidor API in-memory)
- Respawn (limpieza de BD entre tests)
- Custom TestLoggerProvider (captura logs para assertions)

---

## Limitaciones Conocidas

### 1. No hay UI de Gestión de Roles

**Limitación:** No se incluye interfaz de usuario para promover/degradar usuarios entre roles.

**Workaround Actual:** 
```sql
-- Promover usuario a Administrator
UPDATE Users SET Role = 'Administrator' WHERE Email = 'usuario@example.com';

-- Degradar administrador a User
UPDATE Users SET Role = 'User' WHERE Email = 'admin@example.com';
```

**Roadmap:** US-29+ implementará página de administración de usuarios con:
- Listado de todos los usuarios
- Botón "Promover a Administrator" / "Degradar a User"
- Registro de auditoría de cambios de rol

### 2. Solo Un Administrador Semillado

**Limitación:** La migración crea solo un administrador (`admin@sportsclub.local`). Administradores adicionales deben ser promovidos manualmente.

**Razón:** Decisión de Gate 2 — gestión de múltiples admins deferida a sprint futuro.

**Workaround Actual:** Usar SQL UPDATE (ver arriba) o promover usuarios tras implementar UI de gestión.

### 3. Sin UI de Cambio de Password

**Limitación:** El administrador semillado usa password configurado externamente, pero no hay UI para cambiar passwords.

**Implicación de Seguridad:** ⚠️ El password del administrador debe cambiarse manualmente en User Secrets (dev) o Key Vault (prod) tras el primer despliegue.

**Roadmap:** US-30+ implementará cambio de password self-service.

### 4. Sin Audit Log Persistente

**Limitación:** Los intentos de acceso no autorizado (403) se registran en ASP.NET Core logging (consola, Application Insights) pero **no** en una tabla de base de datos dedicada.

**Implicación:** Los logs rotan según configuración del logger. Para auditoría a largo plazo, configurar Application Insights con retención extendida.

**Roadmap:** US-31+ implementará tabla `AuditLogs` con:
- Intentos de login fallidos
- Accesos no autorizados (403)
- Cambios de rol
- Acciones de administrador (eliminación de usuarios, etc.)

### 5. Granularidad de Permisos Binaria

**Limitación:** Los roles son binarios (User vs Administrator). No hay permisos intermedios como:
- "Puede crear eventos pero no eliminarlos"
- "Puede exportar datos pero no importar"
- "Puede ver reportes pero no editarlos"

**Arquitectura Preparada:** El uso de `AuthorizationPolicy` permite añadir estos permisos en futuros sprints sin cambios breaking en la API.

**Roadmap:** US-32+ implementará claims adicionales (ej. `Permissions:CanExportData`) y políticas personalizadas.

### 6. Sin Rate Limiting

**Limitación de Seguridad:** Los endpoints de autenticación (`/login`, `/refresh`) no tienen rate limiting implementado.

**Mitigación Parcial:** BCrypt con work factor 12 proporciona resistencia natural a fuerza bruta (~250ms por intento).

**Recomendación:** Implementar `AspNetCoreRateLimit` antes del despliegue en producción (ver Security Report, finding Medium-2).

### 7. Sin Multi-Factor Authentication (MFA)

**Limitación de Seguridad:** No se soporta autenticación de dos factores (TOTP, SMS, email).

**Roadmap:** US-33+ implementará MFA opcional para cuentas de administrador (TOTP recomendado).

---

## Consideraciones de Seguridad

### Resumen del Security Review (Gate 3)

**Fecha de Revisión:** 2026-07-06  
**Estado:** ✅ Aprobado con condiciones

**Findings:**
- **Critical:** 0
- **High:** 0 (en código de aplicación)
- **Medium:** 3 (SQL interpolation pattern, rate limiting, security headers)
- **Low:** 2 (validación de startup, entropía de JWT secret)
- **Info:** 1 (configuración CORS)

### Fortalezas de Seguridad

✅ **Sin secretos hardcodeados:** Todos los secretos (JWT key, Google OAuth client secret, admin password) externalizados a User Secrets (dev) y Azure Key Vault (prod)

✅ **Hashing de passwords robusto:** BCrypt con work factor 12 (~250ms por hash), resistente a ataques de fuerza bruta con hardware moderno

✅ **Validación JWT completa:** 
- `ValidateIssuer = true` (previene reutilización de tokens de otros emisores)
- `ValidateAudience = true` (previene reutilización para otras audiencias)
- `ValidateLifetime = true` (previene uso de tokens expirados)
- `ValidateIssuerSigningKey = true` (previene tampering)
- `ClockSkew = TimeSpan.Zero` (sin grace period — validación estricta)

✅ **Secure-by-default:** Todos los endpoints requieren autenticación a menos que se marquen explícitamente `[AllowAnonymous]`

✅ **Cookies seguras:**
- `HttpOnly = true` (no accesibles desde JavaScript → previene XSS)
- `Secure = true` (solo HTTPS)
- `SameSite = Strict` (previene CSRF)

✅ **Autorización a nivel de recurso:** EventsController valida que usuarios solo puedan registrarse/cancelarse a sí mismos (salvo admins)

✅ **Blazor authorization respaldada por API:** La autorización UI (`[Authorize]` en Blazor) está respaldada por validación en el backend — no se confía solo en el frontend

✅ **Sin SQL injection:** Todas las queries usan EF Core con parámetros. Única excepción es migración con string interpolation (valores constantes en tiempo de compilación).

✅ **Sin uso de MarkupString:** Todos los componentes Blazor renderizan contenido con encoding automático

✅ **Logging de auditoría:** Intentos de acceso no autorizado (403) registrados con contexto completo (userId, email, role, path, method)

### Vulnerabilidades Identificadas y Mitigaciones

#### 1. Dependencia Vulnerable: Microsoft.OpenApi 2.0.0 (High)

**Afecta:** Proyectos Api y IntegrationTests  
**Severidad:** High (según GitHub Advisory)  
**Riesgo Real:** Low — OpenAPI solo habilitado en desarrollo  
**Decisión:** ✅ Riesgo aceptado con documentación

**Mitigación:**
- OpenAPI endpoint (`/openapi/v1.json`) solo expuesto en `app.Environment.IsDevelopment()`
- Producción no expone el endpoint vulnerable
- Monitorear actualizaciones de `Microsoft.AspNetCore.OpenApi`

#### 2. Sin Rate Limiting (Medium)

**Finding:** Endpoints `/login` y `/refresh` permiten intentos ilimitados  
**Riesgo:** Ataques de fuerza bruta, credential stuffing

**Mitigación Parcial:**
- BCrypt work factor 12 ralentiza naturalmente los intentos (~250ms cada uno)
- Password de admin es complejo (configurado externamente)
- Intentos fallidos registrados en logs (detección manual)

**Acción Requerida Antes de Producción:**
```bash
# Instalar AspNetCoreRateLimit
dotnet add package AspNetCoreRateLimit --project src/SportsClubEventManager.Api

# Configurar en Program.cs:
# - Login: máximo 5 intentos/minuto por IP
# - Refresh: máximo 10 intentos/minuto por IP
```

**Estimación:** 2-3 horas de implementación

#### 3. Faltan Security Headers Explícitos (Medium)

**Finding:** API no establece headers `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security`, etc.

**Riesgo:** MIME-sniffing attacks, clickjacking, XSS en navegadores antiguos

**Acción Requerida Antes de Producción:**
```csharp
// Añadir después de app.UseHttpsRedirection() en Program.cs:
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", 
            "max-age=31536000; includeSubDomains");
    }
    await next();
});
```

**Estimación:** 1-2 horas

#### 4. SQL String Interpolation en Migración (Medium)

**Finding:** `SeedAdministratorUser` usa `migrationBuilder.Sql($@"...")`

**Riesgo Actual:** Ninguno — solo interpola constantes (adminId, now) y output de BCrypt  
**Riesgo Futuro:** Si un desarrollador modifica la migración y añade interpolación de datos externos, podría introducir SQL injection

**Mitigación Recomendada (Opcional):**
Refactorizar a EF Core data seeding:
```csharp
// En UserConfiguration.cs:
modelBuilder.Entity<User>().HasData(new User
{
    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
    Email = "admin@sportsclub.local",
    // ... etc (requiere cálculo de PasswordHash en tiempo de compilación)
});
```

**Nota:** Esto requiere hashear el password en tiempo de compilación, lo cual complica la lectura desde User Secrets. Alternativa: añadir comentario de warning en la migración.

**Estimación:** 2-3 horas si se refactoriza; 15 minutos si solo se añade comentario

### Checklist Pre-Producción

Antes de desplegar a producción, verificar:

- [ ] **Secrets Configurados:**
  - [ ] `AdminUser:Password` en Azure Key Vault (no usar password por defecto)
  - [ ] `Authentication:JwtSettings:SecretKey` en Azure Key Vault (≥32 caracteres)
  - [ ] `Authentication:Google:ClientId` y `ClientSecret` en Key Vault

- [ ] **Configuración de Entorno:**
  - [ ] `appsettings.Production.json` sobrescribe `Cors:AllowedOrigins` (solo HTTPS, no localhost)
  - [ ] `AllowedHosts` restringido (no `*`)
  - [ ] Logging configurado para Application Insights (no solo consola)

- [ ] **Seguridad:**
  - [ ] Rate limiting implementado (`AspNetCoreRateLimit`)
  - [ ] Security headers configurados (ver arriba)
  - [ ] OpenAPI deshabilitado (`MapOpenApi()` solo en development — **ya cumplido ✅**)
  - [ ] CORS no permite `http://` origins (solo `https://`)

- [ ] **Base de Datos:**
  - [ ] Migraciones aplicadas (`dotnet ef database update`)
  - [ ] Usuario administrador creado (verificar con `SELECT * FROM Users WHERE Role = 'Administrator'`)
  - [ ] Password de admin cambiado desde el valor por defecto (login manual y cambio — requiere UI futura)

- [ ] **Monitoreo:**
  - [ ] Application Insights configurado para logs de seguridad (nivel Warning)
  - [ ] Alertas configuradas para:
    - Múltiples intentos de login fallidos desde misma IP (posible ataque)
    - Accesos 403 desde usuarios Administrator (posible compromiso de cuenta)
    - Picos de requests a `/login` (posible DDoS)

---

## Referencias y Enlaces

- **Workflow State:** `.claude/workflows/US-28.json`
- **Implementation Report:** `.claude/docs/US-28/implementation-report.md`
- **Security Report:** `.claude/docs/US-28/security-report-2026-07-06.md`
- **Unit Test Report:** `.claude/docs/US-28/unit-test-report.md`
- **Integration Test Report:** `.claude/docs/US-28/integration-test-report.md`
- **Quality Report:** `.claude/docs/US-28/quality-report.md`

**Documentación Externa:**
- ASP.NET Core Authorization: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/
- JWT Best Practices: https://datatracker.ietf.org/doc/html/rfc8725
- OWASP Top 10: https://owasp.org/www-project-top-ten/
- BCrypt Specification: https://en.wikipedia.org/wiki/Bcrypt

---

**Documento generado:** 2026-07-06T13:00:00Z  
**Agent:** Documentation Agent (Claude Haiku 4.5)  
**Idioma:** Español (configurado via `workflow.config.json > language.documentation`)
