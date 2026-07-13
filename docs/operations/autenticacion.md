# Autenticación (inicio y cierre de sesión)

Funcionalidad transversal: es el punto de entrada obligatorio para cualquier operación que no sea pública (consultar el calendario de eventos es la única excepción — ver [`calendario-eventos.md`](calendario-eventos.md)). Referenciado desde la sección [`e. Funcionalidades principales`](../../README.md#e-funcionalidades-principales) del README.

## Flujo

```mermaid
flowchart TD
    Start(["Usuario visita la aplicación"]) --> Login["Página de login<br/>(Login.razor)"]
    Login --> Choice{"¿Método de acceso?"}

    Choice -->|Email + contraseña| Local["Introduce email y contraseña"]
    Local --> LoginCmd["POST /api/authentication/login<br/>(LoginCommand)"]
    LoginCmd --> LocalCheck{"¿Credenciales válidas?"}
    LocalCheck -->|No| LocalError["401 Unauthorized<br/>mensaje de error en el formulario"]
    LocalError --> Login
    LocalCheck -->|Sí| Cookies

    Choice -->|Google| GoogleBtn["Clic en 'Iniciar sesión con Google'"]
    GoogleBtn --> GoogleRedirect["GET /api/authentication/google<br/>redirección a Google OAuth2"]
    GoogleRedirect --> GoogleConsent["Usuario autentica y autoriza en Google"]
    GoogleConsent --> GoogleCallback["GET /api/authentication/google/callback"]
    GoogleCallback --> GoogleCheck{"¿Autenticación de Google correcta?"}
    GoogleCheck -->|No| GoogleError["Redirección a /login?error=..."]
    GoogleError --> Login
    GoogleCheck -->|Sí| Cookies

    Cookies["Se emiten access_token (JWT, 30 min)<br/>y refresh_token (7 días)<br/>como cookies HttpOnly + Secure + SameSite=Strict"]
    Cookies --> Home["Redirección a la aplicación<br/>(rol User o Administrator determina el menú visible)"]

    Home --> Expired{"¿access_token expirado<br/>en una petición posterior?"}
    Expired -->|Sí| Refresh["POST /api/authentication/refresh<br/>(RefreshTokenCommand)"]
    Refresh --> RefreshCheck{"¿refresh_token válido?"}
    RefreshCheck -->|Sí| Cookies
    RefreshCheck -->|No| Login

    Home --> LogoutAction["Usuario pulsa 'Cerrar sesión'"]
    LogoutAction --> LogoutCmd["POST /api/authentication/logout<br/>(LogoutCommand)"]
    LogoutCmd --> Revoke["Se revoca el refresh_token en base de datos<br/>y se eliminan las cookies"]
    Revoke --> Login
```

## Explicación del flujo

La aplicación admite dos métodos de acceso, gestionados ambos por `AuthenticationController` (`SportsClubEventManager.Api`):

- **Login local (email + contraseña)**: el formulario envía las credenciales a `POST /api/authentication/login`, que despacha un `LoginCommand` vía MediatR. El handler compara el hash de la contraseña (`BCrypt.Net-Next`, factor de coste 12) y, si es válido, emite un JWT de acceso y un refresh token.
- **Login federado con Google OAuth2**: el botón "Iniciar sesión con Google" invoca `GET /api/authentication/google`, que lanza un `Challenge` contra el esquema de Google (`Microsoft.AspNetCore.Authentication.Google`). Tras el consentimiento del usuario en Google, este redirige a `GET /api/authentication/google/callback`, donde la Api recupera el `access_token`/`refresh_token` emitidos y continúa por el mismo camino que el login local.

En ambos casos, el resultado se materializa en dos **cookies `HttpOnly`, `Secure` y `SameSite=Strict`**: `access_token` (JWT, expira a los 30 minutos) y `refresh_token` (expira a los 7 días). El uso de cookies `HttpOnly` — en vez de almacenar el token en `localStorage` — evita que un script malicioso inyectado (XSS) pueda robar la sesión.

Cuando el `access_token` caduca durante el uso normal de la aplicación, `POST /api/authentication/refresh` (`RefreshTokenCommand`) emite un nuevo par de tokens sin que el usuario tenga que volver a introducir sus credenciales, siempre que el `refresh_token` siga siendo válido. Si también ha expirado o ha sido revocado, el usuario es redirigido de nuevo a la pantalla de login.

`POST /api/authentication/logout` (`LogoutCommand`, requiere estar autenticado) revoca el `refresh_token` almacenado en base de datos y limpia ambas cookies — un token robado después de un logout ya no puede usarse para obtener una nueva sesión, aunque el `access_token` JWT original técnicamente siga siendo válido hasta su expiración natural (máximo 30 minutos).
