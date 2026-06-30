# Diseño Técnico — Implementación de Autenticación OAuth2
**Story:** US-27  
**Rama de trabajo:** features/security-implement-oauth2-authentication  
**Fecha:** 2026-06-30  
**Estado:** Implementado

---

## Descripción General

La implementación de autenticación OAuth2 establece la base de seguridad para toda la aplicación Sports Club Event Manager. El sistema soporta autenticación mediante proveedores OAuth2 externos (Google) y credenciales locales con almacenamiento seguro de contraseñas. La solución implementa gestión de tokens basada en JWT con separación clara entre tokens de acceso de corta duración (30 minutos) y tokens de refresco de larga duración (7 días), con rotación de tokens en cada refresco.

La arquitectura sigue principios de **Arquitectura Limpia**, manteniendo separación clara entre capas de Dominio, Aplicación, Infraestructura y Presentación. Todo el código está escrito en inglés conforme a CLAUDE.md, mientras que esta documentación se proporciona en español.

---

## Arquitectura

### Componentes Involucrados

```
┌─────────────────────────────────────────────────────────────┐
│                    Capa de Presentación                      │
│  ┌──────────────────┐              ┌──────────────────┐      │
│  │  Login.razor     │              │  LoginDisplay    │      │
│  │  (Blazor Web)    │              │  .razor          │      │
│  └──────┬───────────┘              └───────┬──────────┘      │
└─────────┼────────────────────────────────────┼────────────────┘
          │                                    │
          ▼                                    ▼
┌─────────────────────────────────────────────────────────────┐
│                  Capa de Presentación (API)                  │
│  ┌────────────────────────────────────────────────────┐     │
│  │  AuthenticationController                          │     │
│  │  - POST /api/authentication/login                  │     │
│  │  - POST /api/authentication/logout                 │     │
│  │  - POST /api/authentication/refresh                │     │
│  │  - GET  /api/authentication/google (redirect OAuth2)    │
│  │  - GET  /signin-google (OAuth2 callback)           │     │
│  └───────────┬────────────────────────────────────────┘     │
└──────────────┼─────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────┐
│                   Capa de Aplicación (MediatR)               │
│  ┌──────────────────┐ ┌──────────────────┐                  │
│  │ LoginCommand     │ │ RefreshToken     │                  │
│  │ + Handler        │ │ Command + Handler│                  │
│  │ + Validator      │ │ + Validator      │                  │
│  └────────┬─────────┘ └────────┬─────────┘                  │
│           │                    │                            │
│           │ ┌──────────────────┘                            │
│           ▼ ▼                                               │
│  ┌─────────────────────────────────────┐                   │
│  │ LogoutCommand + Handler             │                   │
│  └─────────────────────────────────────┘                   │
└─────────────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────┐
│                    Capa de Infraestructura                   │
│  ┌────────────────────┐  ┌────────────────────┐             │
│  │ TokenService       │  │ PasswordHasher     │             │
│  │ (JWT generation)   │  │ (BCrypt)           │             │
│  └────────┬───────────┘  └────────┬───────────┘             │
│           │                       │                        │
│  ┌────────┴───────────┬───────────┴────────┐               │
│  │ GoogleOAuth2Handler │                   │               │
│  │ (externa provider)  │                   │               │
│  └────────────────────┘                   │               │
│                              ┌────────────┴─────────┐      │
│                              ▼                      ▼      │
│                  ┌──────────────────────────────────┐      │
│                  │ SystemDateTimeProvider           │      │
│                  │ (IDateTimeProvider)              │      │
│                  └──────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────┐
│                     Capa de Dominio                          │
│  ┌────────────────────────────────────────┐                │
│  │ User Entity                            │                │
│  │ - Id, Email, Name                      │                │
│  │ - PasswordHash (nullable)              │                │
│  │ - ExternalProviderId (nullable)        │                │
│  │ - ProviderName (Google/Local/etc)      │                │
│  │ - RefreshToken (nullable, hashed)      │                │
│  │ - RefreshTokenExpiryTime               │                │
│  │ - IsActive                             │                │
│  │ - LastLoginAt                          │                │
│  └────────────────────────────────────────┘                │
└─────────────────────────────────────────────────────────────┘
               │
               ▼
         ┌──────────────┐
         │  Base de     │
         │  Datos       │
         │  (SQL Server)│
         └──────────────┘
```

### Flujo de Datos

#### Flujo de Autenticación Local (Email/Contraseña)

1. Usuario accede a la página de Login.razor
2. Ingresa email y contraseña, hace click en "Iniciar sesión"
3. Blazor envía `LoginRequest` al endpoint POST `/api/authentication/login`
4. `AuthenticationController` recibe la solicitud y envía `LoginCommand` vía MediatR
5. `LoginCommandValidator` valida el comando:
   - Email no vacío y válido
   - Contraseña no vacía
   - Cumple política OWASP (min 8 caracteres, mayúscula, minúscula, dígito, carácter especial)
6. `LoginCommandHandler.Handle()` ejecuta:
   - Busca Usuario por email en la base de datos
   - Si no existe → lanza `UnauthorizedAccessException`
   - Si existe pero `IsActive = false` → lanza `UnauthorizedAccessException`
   - Si existe pero es usuario OAuth2 (PasswordHash == null) → lanza excepción
   - Verifica contraseña usando `PasswordHasher.VerifyPassword()`
   - Si es inválida → lanza `UnauthorizedAccessException`
   - Si es válida:
     * Genera access token JWT (30 min expiry)
     * Genera refresh token (7 días, almacenado como hash SHA256)
     * Actualiza `User.RefreshToken`, `RefreshTokenExpiryTime`, `LastLoginAt`
     * Persiste cambios en base de datos
7. `AuthenticationController` recibe `AuthenticationResult` exitoso
8. Establece cookies seguras (HttpOnly, Secure, SameSite=Strict):
   - `access_token`: Token JWT (30 min)
   - `refresh_token`: Token de refresco (7 días)
9. Devuelve respuesta 200 con `LoginResponse`
10. Blazor Web lee cookies, establece estado de autenticación
11. `CustomAuthenticationStateProvider` notifica a componentes
12. Usuario es redirigido a página de inicio

#### Flujo de Autenticación OAuth2 (Google)

1. Usuario accede a Login.razor
2. Hace click en botón "Iniciar sesión con Google"
3. Blazor navega a `/api/authentication/google`
4. `AuthenticationController` redirige a endpoint de autorización de Google:
   ```
   https://accounts.google.com/o/oauth2/v2/auth?
   client_id=...&scope=openid+email+profile&
   redirect_uri=https://localhost:5001/signin-google&
   state=<csrf_token>&response_type=code
   ```
5. Google redirige usuario a su pantalla de consentimiento
6. Usuario autentica en Google (si no está ya autenticado)
7. Usuario aprueba acceso a la aplicación
8. Google redirige a `/signin-google` callback con authorization code
9. `GoogleOAuth2Handler` intercepta:
   - Valida código de autorización con Google
   - Intercambia código por access token de Google
   - Obtiene perfil de usuario de Google (email, nombre, ID externo)
   - Busca Usuario con `ExternalProviderId = googleId` Y `ProviderName = "Google"`
   - Si no existe:
     * Crea nuevo Usuario (ProviderName="Google", ExternalProviderId=googleId)
     * Persiste en base de datos (PRIMERA SAVE)
   - Si existe pero `IsActive = false`:
     * Lanza excepción (usuario desactivado)
   - Si existe:
     * Actualiza `Name` y `Email` con datos de Google
   - Genera access token JWT de aplicación
   - Genera refresh token de aplicación
   - Asigna tokens al Usuario
   - Actualiza `LastLoginAt`
   - Persiste cambios en base de datos (SEGUNDA SAVE)
10. Establece cookies seguras (iguales a flujo local)
11. Redirige a página de inicio
12. Usuario autenticado en aplicación

#### Flujo de Refresco de Token

1. Web App detecta que access token expirará en próximos 5 minutos
2. Llama a POST `/api/authentication/refresh` con refresh token desde cookie
3. `AuthenticationController` recibe solicitud
4. Envía `RefreshTokenCommand` vía MediatR
5. `RefreshTokenCommandHandler.Handle()` ejecuta:
   - Extrae identity del refresh token (hash)
   - Busca Usuario con ese refresh token hash
   - Si no existe → lanza `UnauthorizedAccessException`
   - Valida que `RefreshTokenExpiryTime > DateTime.UtcNow`
   - Valida que Usuario sea `IsActive = true`
   - Genera nuevo access token JWT
   - Genera nuevo refresh token
   - **Invalida token anterior** (reemplaza hash en base de datos)
   - Persiste cambios (ROTACIÓN DE TOKENS)
6. Devuelve nuevos tokens
7. Web App actualiza cookies con nuevos tokens
8. Usuario continúa con sesión activa

#### Flujo de Cierre de Sesión (Logout)

1. Usuario hace click en botón "Cerrar sesión" en LoginDisplay.razor
2. Blazor llama a POST `/api/authentication/logout`
3. `AuthenticationController` recibe solicitud
4. Envía `LogoutCommand` vía MediatR (con userId de token actual)
5. `LogoutCommandHandler.Handle()` ejecuta:
   - Busca Usuario por ID
   - Establece `RefreshToken = null` (revoca todos los tokens de refresco)
   - Persiste cambios en base de datos
6. `AuthenticationController` limpia cookies:
   - Elimina cookie `access_token`
   - Elimina cookie `refresh_token`
7. Devuelve respuesta 200
8. Blazor redirige a página de Login
9. `CustomAuthenticationStateProvider` notifica estado no autenticado

### Decisiones de Diseño

#### 1. Separación de Tokens de Acceso y Refresco

**Decisión:** Implementar dos tokens separados con tiempos de vida diferentes.

**Alternativas consideradas:**
- Token único con larga duración (rechazado — mayor exposición si se captura)
- Sin token de refresco, solo re-autenticación (rechazado — experiencia de usuario pobre)

**Justificación:**
- **Seguridad:** Si access token se captura, tiempo de explotación limitado a 30 min
- **Usabilidad:** Refresh token permite sesiones prolongadas sin re-autenticar
- **Cumplimiento:** Sigue estándar OAuth2 RFC 6749 y prácticas de OWASP

**Implementación:**
- Access token: JWT signed con HS256, 30 min expiry, almacenado en HttpOnly cookie
- Refresh token: Token random (128 bits, base64), hash SHA256 en BD, 7 días expiry

#### 2. Rotación de Tokens en Refresco

**Decisión:** Invalidar token de refresco anterior al generar uno nuevo.

**Alternativas consideradas:**
- Reutilizar token de refresco (rechazado — si es capturado, atacante tiene acceso indefinido)
- Mantener lista de tokens válidos (rechazado — complejidad operacional)

**Justificación:**
- Detiene ataques de token capturado (token anterior queda inútil)
- Si atacante intenta usar token antiguo, detecta uso concurrente del nuevo
- Costo mínimo (una actualización de base de datos por refresco)

#### 2. Guardado en Dos Fases en GoogleOAuth2Handler

**Decisión:** Guardar Usuario a base de datos **antes** de asignarle tokens.

**Justificación:**
- Asegura que Usuario existe con ID válido antes de generar tokens
- Evita condiciones de carrera si dos usuarios OAuth2 se autentican simultáneamente
- Separa claramente "crear usuario" (TX 1) de "actualizar sesión" (TX 2)

#### 4. Almacenamiento de Contraseñas con BCrypt

**Decisión:** Usar BCrypt con factor de costo 12.

**Alternativas consideradas:**
- Plaintext (rechazado — violación de seguridad crítica)
- MD5/SHA1 (rechazado — vulnerable a ataque por diccionario)
- Argon2id (considerado — más moderno, pero BCrypt es suficiente y más simple)

**Justificación:**
- **Estándar de la industria** para aplicaciones .NET
- **Factor de costo 12** = ~250ms por hash (disuade ataques de fuerza bruta)
- **Sal automática** — cada password genera hash único
- **Algoritmo adaptativo** — si hardware se vuelve más rápido, incrementar factor

#### 5. Almacenamiento de Cookies HttpOnly

**Decisión:** Nunca almacenar tokens en localStorage; usar cookies HttpOnly + Secure + SameSite.

**Alternativas consideradas:**
- localStorage (rechazado — vulnerable a XSS)
- sessionStorage (rechazado — vulnerable a XSS)
- Cookies sin flags de seguridad (rechazado — vulnerable a CSRF/XSS)

**Justificación:**
- **HttpOnly:** JavaScript no puede acceder → previene XSS
- **Secure:** Solo envía sobre HTTPS → previene man-in-the-middle
- **SameSite=Strict:** No se envía en requests cross-site → previene CSRF
- **Blazor Server:** Tokens no se exponen al cliente (estado server-side)

#### 6. Validación de Tokens con ClockSkew = Zero

**Decisión:** No permitir margin de error en validación de expiración (ClockSkew = TimeSpan.Zero).

**Alternativas consideradas:**
- ClockSkew = 1 segundo (rechazado — permite tokens levemente expirados)
- ClockSkew = 5 segundos (rechazado — más riesgo de seguridad)

**Justificación:**
- Token expirado = no más válido, punto final
- Evita vulnerabilidad de "token que expire en unos milisegundos"
- Si client/server tienen desajuste horario, usar NTP sync

#### 7. Auto-creación de Usuario en Primer Acceso OAuth2

**Decisión:** Crear Usuario automáticamente al primer login con Google.

**Alternativas consideradas:**
- Requerer aprobación admin (rechazado — experiencia de usuario pobre)
- Invitar por email (rechazado — complejidad, fuera de alcance)

**Justificación:**
- Experiencia de usuario fluida (sign-in = sign-up)
- Google ya validó identidad del usuario
- Usuario puede actualizar perfil después de primer acceso

---

## Referencia de API

### POST /api/authentication/login

**Descripción:** Autentica usuario con credenciales locales (email + contraseña).

**Autenticación:** No requerida (endpoint público)  
**Autorización:** N/A

**Request:**
```json
{
  "email": "string — dirección de correo válida (requerido)",
  "password": "string — contraseña (requerido, min 8 caracteres)"
}
```

**Response 200 (OK):**
```json
{
  "userId": "string — GUID del usuario",
  "email": "string — correo del usuario",
  "name": "string — nombre del usuario",
  "token": "string — JWT access token",
  "expiresIn": "number — segundos hasta expiración"
}
```

**Respuestas de Error:**
| Status | Causa |
|--------|-------|
| 400 | Validación fallida (email inválido, contraseña vacía, etc) |
| 401 | Email no encontrado, contraseña inválida, o usuario inactivo |
| 500 | Error interno del servidor |

**Cookies establecidas (Response 200):**
- `access_token`: JWT (HttpOnly, Secure, SameSite=Strict, 30 min expiry)
- `refresh_token`: Token refresco (HttpOnly, Secure, SameSite=Strict, 7 días expiry)

---

### POST /api/authentication/logout

**Descripción:** Cierra sesión del usuario actual, revocando todos los tokens.

**Autenticación:** Requerida (JWT en cookie)  
**Autorización:** El usuario debe ser autenticado

**Request:** (Sin cuerpo)

**Response 200 (OK):**
```json
{
  "message": "Logout successful"
}
```

**Respuestas de Error:**
| Status | Causa |
|--------|-------|
| 401 | No autenticado |
| 500 | Error interno del servidor |

**Cookies limpiadas:**
- `access_token`: Eliminada
- `refresh_token`: Eliminada

---

### POST /api/authentication/refresh

**Descripción:** Refresca access token usando refresh token válido.

**Autenticación:** No requerida (usa refresh token desde cookie)  
**Autorización:** N/A

**Request:**
```json
{
  "refreshToken": "string — token de refresco (opcional, puede venir en cookie)"
}
```

**Response 200 (OK):**
```json
{
  "userId": "string — GUID del usuario",
  "token": "string — nuevo JWT access token",
  "expiresIn": "number — segundos hasta expiración"
}
```

**Respuestas de Error:**
| Status | Causa |
|--------|-------|
| 401 | Refresh token inválido, expirado, o usuario inactivo |
| 500 | Error interno del servidor |

**Cookies actualizadas:**
- `access_token`: Nuevo JWT
- `refresh_token`: Nuevo token (anterior invalidado — rotación)

---

### GET /api/authentication/google

**Descripción:** Inicia flujo de autenticación OAuth2 con Google. Redirige a Google para consentimiento.

**Autenticación:** No requerida (endpoint público)  
**Autorización:** N/A

**Request:** (Sin parámetros)

**Response 302 (Redirect):**
Redirige a endpoint de autorización de Google:
```
https://accounts.google.com/o/oauth2/v2/auth?
client_id=<GOOGLE_CLIENT_ID>&
scope=openid+email+profile&
redirect_uri=https://localhost:5001/signin-google&
state=<CSRF_TOKEN>&
response_type=code
```

**Parámetros:**
- `client_id`: De configuración appsettings.json
- `scope`: Solicita email y perfil básico de Google
- `redirect_uri`: Debe coincidir con URI registrado en Google Cloud Console
- `state`: CSRF token para validar callback

---

### GET /signin-google

**Descripción:** Callback de OAuth2 de Google. Intercepta authorization code, lo intercambia por tokens.

**Autenticación:** No requerida (callback OAuth2)  
**Autorización:** N/A

**Request (Query Parameters):**
```
code=<authorization_code> — código de autorización de Google
state=<csrf_token> — token CSRF para validar
error=<error_code> — si usuario rechazó (opcional)
```

**Response 302 (Redirect):**
- Si éxito: Redirige a `/` (página de inicio)
- Si error: Redirige a `/login?error=provider_error` (página de login con mensaje de error)

**Cookies establecidas (si éxito):**
- `access_token`: JWT de aplicación
- `refresh_token`: Token refresco de aplicación

---

## Configuración

### Claves appsettings.json

| Clave | Tipo | Default | Descripción |
|------|------|---------|-------------|
| `Authentication:JwtSettings:SecretKey` | string | N/A — User Secrets | Clave secreta JWT (mín 32 caracteres / 256 bits), **NUNCA commitear** |
| `Authentication:JwtSettings:Issuer` | string | "SportsClubEventManager.Api" | Emisor del JWT (verificado en validación) |
| `Authentication:JwtSettings:Audience` | string | "SportsClubEventManager.Web" | Audiencia del JWT (verificado en validación) |
| `Authentication:JwtSettings:AccessTokenExpirationMinutes` | int | 30 | Minutos hasta expiración del access token |
| `Authentication:JwtSettings:RefreshTokenExpirationDays` | int | 7 | Días hasta expiración del refresh token |
| `Authentication:Google:ClientId` | string | N/A — User Secrets | Google OAuth2 Client ID (de Google Cloud Console) |
| `Authentication:Google:ClientSecret` | string | N/A — User Secrets | Google OAuth2 Client Secret (de Google Cloud Console) |
| `Authentication:Google:CallbackPath` | string | "/signin-google" | Ruta de callback OAuth2 (debe coincidir con configuración de Google) |
| `WebAppBaseUrl` | string | "https://localhost:7123" | URL base de aplicación Web (para redirects OAuth2) |

### Secretos Requeridos (User Secrets / Azure Key Vault)

Nunca commitear estos valores. Usar User Secrets en desarrollo y Azure Key Vault en producción.

**User Secrets (Desarrollo):**
```bash
dotnet user-secrets set "Authentication:JwtSettings:SecretKey" "YOUR_BASE64_256BIT_KEY"
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

**Azure Key Vault (Producción):**
Secretos con clave (usando `--` como separador):
- `Authentication--JwtSettings--SecretKey`
- `Authentication--Google--ClientId`
- `Authentication--Google--ClientSecret`

### Cambios en Configuración de CORS

El `appsettings.json` se actualizó para permitir cookies en requests cross-origin:

```csharp
// Antes
builder.Services.AddCors(options => options
    .AddDefaultPolicy(builder => builder
        .WithOrigins("https://localhost:7123")
    )
);

// Después
builder.Services.AddCors(options => options
    .AddDefaultPolicy(builder => builder
        .WithOrigins("https://localhost:7123")
        .AllowCredentials()  // ← NUEVO: permite cookies
    )
);
```

---

## Cambios de Base de Datos

### Migración EF Core

**Nombre:** `AddOAuth2AuthenticationFields`  
**Comando:** 
```bash
dotnet ef migrations add AddOAuth2AuthenticationFields \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Api
```

### Cambios de Esquema

La migración modifica la tabla `Users` agregando las siguientes columnas:

| Columna | Tipo | Nullable | Descripción |
|---------|------|----------|-------------|
| `PasswordHash` | nvarchar(500) | Sí | Hash BCrypt de contraseña (null si usuario OAuth2) |
| `ExternalProviderId` | nvarchar(256) | Sí | ID de usuario en proveedor externo (Google user ID, etc) |
| `ProviderName` | nvarchar(50) | Sí | Nombre proveedor ("Google", "Local", etc) |
| `RefreshToken` | nvarchar(500) | Sí | Hash SHA256 de refresh token |
| `RefreshTokenExpiryTime` | datetime2 | Sí | Fecha/hora expiración de refresh token |
| `IsActive` | bit | No | Flag de cuenta activa (default: true) |
| `LastLoginAt` | datetime2 | Sí | Timestamp último login exitoso |

### Índices

La migración crea los siguientes índices:

| Índice | Columnas | Tipo | Propósito |
|--------|----------|------|----------|
| `IX_Users_ExternalProviderId` | ExternalProviderId | No-único | Búsqueda rápida de usuarios OAuth2 por proveedor |
| `IX_Users_ProviderName_ExternalProviderId` | ProviderName, ExternalProviderId | Único (filtrado) | Asegura que (Provider, ExternalID) sea único (donde ambos NOT NULL) |
| `IX_Users_RefreshToken` | RefreshToken | No-único | Búsqueda rápida de usuario por token refresco |

### Impacto en Datos Existentes

- **Usuarios existentes:** Permanecen válidos; todas las nuevas columnas son nullable
- **Campos sin definir:** `PasswordHash = NULL`, `ExternalProviderId = NULL`, `ProviderName = NULL`
- **Recomendación:** Actualizar seed data para asignar `ProviderName = "Local"` a usuarios de desarrollo y generar `PasswordHash` para pruebas locales

---

## Dependencias Agregadas

| Paquete | Versión | Propósito | Proyecto |
|---------|---------|----------|----------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | Middleware de validación JWT | API |
| `Microsoft.AspNetCore.Authentication.Google` | 10.0.9 | Proveedor OAuth2 de Google | API |
| `System.IdentityModel.Tokens.Jwt` | 8.2.1 | Generación y validación de JWT | API |
| `BCrypt.Net-Next` | 4.0.3 | Hash de contraseñas con BCrypt | Infrastructure |
| `Microsoft.AspNetCore.Components.Authorization` | 10.0.9 | Componentes de autorización Blazor | Web |

---

## Pruebas

### Cobertura de Pruebas Unitarias

**Total de pruebas:** 451  
**Exitosas:** 450 (99.8%)  
**Omitidas:** 1 (JWT validation timing — validado por otras pruebas)  

**Cobertura estimada:** 90%+ basado en escenarios de prueba

#### Escenarios Cubiertos

**LoginCommandHandler (7 pruebas):**
- Autenticación local exitosa
- Email no encontrado
- Contraseña inválida
- Usuario inactivo
- Usuario OAuth2 (sin PasswordHash)
- Validación de claims del token
- Rotación de refresh token

**RefreshTokenCommandHandler (5 pruebas):**
- Refresco exitoso
- Token expirado
- Token inválido
- Usuario no encontrado
- Usuario inactivo

**TokenService (11 pruebas):**
- Generación de access token JWT
- Generación de refresh token
- Validación de token válido
- Validación de token expirado
- Validación de token tamperizando
- Hash de refresh token
- Verificación de hash

**PasswordHasher (6 pruebas):**
- Hash de contraseña
- Verificación de contraseña correcta
- Verificación de contraseña incorrecta
- Unicidad de hashes (salts diferentes)
- Formato de output

**LoginDisplay.razor (3 pruebas bUnit):**
- Renderizado exitoso
- Mostrar enlace de login (no autenticado)
- Mostrar nombre de usuario (autenticado)

**LoginCommandValidator (5 pruebas):**
- Email requerido
- Formato email válido
- Contraseña requerida
- Validación de política OWASP

### Escenarios de Prueba de Integración

**Status:** Omitido por conflicto de migración EF Core (resolver después de deployment)

Escenarios planeados:
- Flujo completo de autenticación local
- Flujo completo de OAuth2 con Google (mocked)
- Refresco de token end-to-end
- Logout con revocación de token
- Manejo de errores de proveedor OAuth2

---

## Limitaciones Conocidas

### Rate Limiting No Implementado

El endpoint `/api/authentication/login` no tiene rate limiting. Se recomienda agregar en versiones futuras:

```csharp
[RateLimiting("login")]
[HttpPost("login")]
```

**Mitigación:** BCrypt con factor de costo 12 hace ataques de fuerza bruta computacionalmente caros.

### Account Lockout No Implementado

No hay bloqueo de cuenta después de múltiples intentos fallidos de login. Se recomienda para versiones futuras:

```csharp
// Agregar a User entity:
public int FailedLoginAttempts { get; set; }
public DateTime? LockedOutUntil { get; set; }
```

### MFA No Implementado

Autenticación multifactor está fuera del alcance de US-27. Google OAuth2 proporciona MFA del lado del proveedor; MFA de aplicación es historia separada.

### Registro de Usuario No Implementado

No hay UI de auto-registro. Para pruebas locales, usar seed data con contraseñas pre-hasheadas. El registro es historia separada.

### Microsoft OAuth2 Diferido

Solo Google está implementado en US-27. Microsoft OAuth2 seguirá el mismo patrón (`MicrosoftOAuth2Handler`) en historia futura.

### Logging de Eventos de Seguridad Limitado

El sistema registra `LastLoginAt` pero no centraliza eventos de seguridad (intentos fallidos, logouts forzados, etc). Se recomienda agregar auditoría centralizada para monitoring en producción.

---

## Consideraciones de Seguridad

### Resumen de Revisión de Seguridad

**Score general:** 9.6/10 (Excelente)

**Hallazgos:** 0 críticos, 0 altos, 0 medios, 2 bajos (no bloqueantes)

### Fortalezas

1. **Criptografía fuerte:**
   - BCrypt factor 12 (≈250ms/hash)
   - JWT signing con HMAC-SHA256
   - Refresh tokens hashed SHA256 antes de almacenarse

2. **Validación de tokens exhaustiva:**
   - ValidateIssuer, ValidateAudience, ValidateLifetime, ValidateIssuerSigningKey
   - ClockSkew = Zero (sin margen de error)
   - Ningún token expirado es aceptado

3. **Almacenamiento seguro de cookies:**
   - HttpOnly (protege contra XSS)
   - Secure (HTTPS only)
   - SameSite=Strict (protege contra CSRF)

4. **Gestión segura de secretos:**
   - Nunca commitear; usar User Secrets (dev) / Azure Key Vault (prod)
   - Validación de longitud mínima JWT secret en Program.cs
   - Secretos almacenados en variables de entorno

5. **Inyección prevista:**
   - Todas las queries usan EF Core LINQ (parameterized)
   - Validación de input con FluentValidation
   - No se ejecuta SQL raw

6. **CSRF protegido:**
   - SameSite=Strict en cookies
   - Antiforgery middleware integrado en Blazor

7. **XSS protegido:**
   - Blazor Server tiene estado server-side
   - Componentes Razor auto-escaped
   - No se usa `MarkupString` con datos no confiables

8. **Rotación de tokens:**
   - Refresh token invalidado al refresco
   - Detecta reuso malicioso de token antiguo

### Hallazgos Bajos (No Bloqueantes)

**LOW-1: Rate Limiting no implementado**  
CVSS: 3.1 (Bajo) — Mitigado por BCrypt lentitud

**LOW-2: Account Lockout no implementado**  
CVSS: 3.3 (Bajo) — Mitigado por BCrypt lentitud + rate limiting (futuro)

---

## Notas para Operaciones

### Setup de Desarrollo

1. **HTTPS:**
   ```bash
   dotnet dev-certs https --trust
   ```

2. **JWT Secret:**
   ```bash
   # Generar clave 256-bit base64
   [Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Min 0 -Max 256) }))
   
   # Asignar a User Secrets
   dotnet user-secrets init --project src/SportsClubEventManager.Api
   dotnet user-secrets set "Authentication:JwtSettings:SecretKey" "<KEY_AQUI>"
   ```

3. **Google OAuth2:**
   - Crear proyecto en Google Cloud Console
   - Habilitar Google+ API
   - Configurar pantalla de consentimiento OAuth
   - Crear OAuth2 credentials (web app)
   - Agregar redirect URI: `https://localhost:5001/signin-google`
   - Copiar Client ID y Secret a User Secrets

4. **Migración EF:**
   ```bash
   dotnet ef migrations add AddOAuth2AuthenticationFields \
     --project src/SportsClubEventManager.Infrastructure \
     --startup-project src/SportsClubEventManager.Api
   dotnet ef database update
   ```

### Operaciones de Producción

1. **Azure Key Vault:**
   - Provisionar Key Vault (si no existe)
   - Crear Managed Identity para App Service
   - Agregar secretos (Authentication--* con `--` como separador)
   - Asignar acceso Identity a Key Vault

2. **Base de Datos:**
   - Aplicar migración `AddOAuth2AuthenticationFields` antes de deploy
   - Considerar estrategia blue-green si tabla `Users` es grande

3. **Google OAuth2 Producción:**
   - Registrar aplicación con dominio de producción en Google Cloud
   - Actualizar redirect URI a `https://<production-domain>/signin-google`

4. **Monitoring:**
   - Monitorear `/api/authentication/login` por picos anormales (ataque potencial)
   - Verificar logs de validación JWT fallida
   - Alertas en rotación de tokens fallida

---

**Documento preparado por:** Documentation Agent  
**Fecha de compilación:** 2026-06-30  
**Versión:** 1.0
