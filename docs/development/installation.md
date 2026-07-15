# Instalación y ejecución

Guía paso a paso para instalar y ejecutar **SportsClubEventManager** en local, referenciada desde la sección [`c. Instalación y ejecución`](../../README.md#c-instalación-y-ejecución) del README.

Existen dos vías: **Docker Compose** (recomendada, todo el stack en contenedores) o **`dotnet run` local** (requiere SQL Server accesible). Sigue únicamente una de las dos opciones.

## Requisitos previos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) — la versión exacta (`10.0.100`) está fijada en `global.json`.
- [Docker](https://www.docker.com/) y Docker Compose — necesario para la Opción A, opcional para la Opción B.
- SQL Server / LocalDB instalado localmente — solo si se usa la Opción B sin Docker.
- Credenciales de [Google OAuth2](https://console.cloud.google.com/apis/credentials) — opcionales, solo necesarias si se quiere probar el login con Google (el login local con email/contraseña no las requiere).

## Opción A · Docker Compose (recomendada)

### Paso 1 — Clonar el repositorio

```bash
git clone https://github.com/AlejBlasco/SportsClubEventManager.git
cd SportsClubEventManager
```

### Paso 2 — Crear el fichero `.env`

Copiar la plantilla `.env.example` (no contiene ningún secreto real, solo la lista completa de variables esperadas, cada una documentada con un comentario):

```bash
cp .env.example .env
```

Abrir `.env` y rellenar, como mínimo, las siguientes variables:

```env
SA_PASSWORD=UnaContraseñaSegura123!
CONNECTION_STRING=Server=sqlserver,1433;Database=SportsClubEventManager;User Id=sa;Password=UnaContraseñaSegura123!;TrustServerCertificate=True;MultipleActiveResultSets=true
API_PORT=5240
WEB_PORT=5123
SQL_PORT=1433
PROMETHEUS_PORT=9090
GRAFANA_PORT=3000
GRAFANA_ADMIN_PASSWORD=<contraseña-fuerte-para-el-usuario-admin-de-grafana>
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET_KEY=<clave-base64-de-al-menos-32-caracteres>
ADMIN_PASSWORD=<contraseña para admin@sportsclub.local>
GOOGLE_CLIENT_ID=<opcional-si-se-usa-login-con-Google>
GOOGLE_CLIENT_SECRET=<opcional-si-se-usa-login-con-Google>
```

> `SA_PASSWORD` debe cumplir los requisitos de complejidad de SQL Server (mínimo 8 caracteres, mayúscula + minúscula + dígito + símbolo), y su valor debe mantenerse sincronizado con la contraseña embebida en `CONNECTION_STRING`.
>
> Para generar `JWT_SECRET_KEY` (mínimo 32 caracteres):
> ```bash
> openssl rand -base64 32
> ```
>
> Las variables relacionadas con n8n (`NOTIFICATIONS_N8N_*`) pueden dejarse vacías/por defecto (`NOTIFICATIONS_N8N_ENABLED=false`) — no son necesarias para levantar el entorno de desarrollo.

### Paso 3 — Levantar el stack completo

```bash
docker compose up --build
```

> El `docker-compose.yml` de la raíz es un fichero `include:` de dos líneas; el contenido real del stack vive en [`infrastructure/docker-compose/`](../../infrastructure/docker-compose/README.md), pero el comando anterior funciona sin cambios ejecutado desde la raíz del repositorio.

Este comando construye las imágenes de `api` y `web`, y levanta `sqlserver`, `api`, `web`, `prometheus`, `grafana`, `node-exporter` y `cadvisor`. La primera vez puede tardar varios minutos (build de las imágenes + arranque de SQL Server + aplicación automática de migraciones).

### Paso 4 — Acceder a la aplicación

| Servicio | URL |
|---|---|
| Web (Blazor) | http://localhost:5123 |
| API + Swagger | http://localhost:5240/swagger |
| Prometheus | http://localhost:9090 (o el puerto de `PROMETHEUS_PORT`) |
| Grafana | http://localhost:3000 (o el puerto de `GRAFANA_PORT`) — usuario `admin`, contraseña la definida en `GRAFANA_ADMIN_PASSWORD` |

En Prometheus, comprobar en **Status → Targets** que `api` y `web` aparecen como `UP`. En Grafana, el dashboard **"SportsClubEventManager - Overview"** ya aparece provisionado en la carpeta "SportsClubEventManager" sin ningún paso manual.

> Con `ASPNETCORE_ENVIRONMENT=Development` se aplican también las migraciones de datos de prueba (ver [`h. Usuario y contraseña de prueba`](../../README.md#h-usuario-y-contraseña-de-prueba)).
>
> **Validación de arranque**: ambos hosts (`Api` y `Web`) validan de forma agregada, al arrancar y antes de aceptar ninguna petición HTTP, que toda la configuración crítica esté presente y sea válida. Si falta o es inválida alguna variable obligatoria, el proceso **no arranca**: termina con una excepción que agrega en un único mensaje **todos** los errores de configuración detectados (no solo el primero). Revisar los logs del contenedor (`docker compose logs api` / `docker compose logs web`) para ver el detalle.

## Opción B · Ejecución local con `dotnet run`

Requiere una instancia de SQL Server / LocalDB accesible (puede ser el contenedor `sqlserver` de la Opción A levantado de forma aislada, o una instalación local).

### Paso 1 — Configurar los secretos de usuario

El `UserSecretsId` ya viene precommiteado en cada `.csproj`, no hace falta ejecutar `dotnet user-secrets init`:

```bash
dotnet user-secrets set "Authentication:JwtSettings:SecretKey" "<clave-de-al-menos-32-caracteres>" --project src/SportsClubEventManager.Api
dotnet user-secrets set "Authentication:Google:ClientId" "<google-client-id>" --project src/SportsClubEventManager.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "<google-client-secret>" --project src/SportsClubEventManager.Api
dotnet user-secrets set "AdminUser:Password" "<contraseña-admin>" --project src/SportsClubEventManager.Infrastructure
```

Un fichero de referencia con todos los secretos necesarios está disponible en [`.secrets-template.json`](../../.secrets-template.json). Para el inventario completo de secretos y el procedimiento de alta/rotación de cada uno, ver [`docs/technical/US-38-secrets-management.md`](../technical/US-38-secrets-management.md).

### Paso 2 — Aplicar las migraciones de base de datos

```bash
dotnet ef database update --project src/SportsClubEventManager.Infrastructure --startup-project src/SportsClubEventManager.Web
```

### Paso 3 — (Opcional) Aplicar datos de prueba

Solo en entorno `Development`:

```bash
dotnet ef database update AddDevelopmentSeedData --project src/SportsClubEventManager.Infrastructure --startup-project src/SportsClubEventManager.Web
dotnet ef database update SeedDevelopmentUserPasswords --project src/SportsClubEventManager.Infrastructure --startup-project src/SportsClubEventManager.Web
```

### Paso 4 — Arrancar la API y la aplicación Web

En dos terminales separadas:

```bash
dotnet run --project src/SportsClubEventManager.Api    # http://localhost:5240 · /swagger
dotnet run --project src/SportsClubEventManager.Web    # http://localhost:5123
```

## Ejecutar la batería de tests

```bash
dotnet test
```

No requiere Docker en marcha para los tests unitarios; los tests de integración (`SportsClubEventManager.IntegrationTests`) usan **Testcontainers** y sí requieren que el motor de Docker esté disponible en la máquina que ejecuta `dotnet test`, ya que levantan su propia instancia efímera de SQL Server.

## Troubleshooting

### `docker compose up --build` falla al arrancar `node-exporter` en Windows con WSL2

```
Error response from daemon: path / is mounted on / but it is not a shared or slave mount
```

**Causa:** `node-exporter` monta `/:/host:ro,rslave` — la propagación `rslave` exige explícitamente que el mount de origen ya sea `shared`/`slave` en el host, y WSL2 monta `/` como privada por defecto. No es un problema del stack, sino del entorno WSL2. `cadvisor` monta `/:/rootfs:ro` sin flag de propagación, por lo que **no** se ve afectado por este mismo error; si en algún momento sí lo estuviera, la solución es idéntica.

Si solo se necesita la aplicación en sí (sin `prometheus`/`grafana`/`node-exporter`/`cadvisor`), se puede evitar el problema arrancando únicamente los servicios necesarios:

```bash
docker compose up -d sqlserver api web
```

**Solución**, dentro de una terminal WSL2 (no PowerShell):

```bash
sudo mount --make-rshared /
```

Para que se aplique en cada arranque de WSL2, añadir en `/etc/wsl.conf` de esa distro:

```ini
[boot]
command = "mount --make-rshared /"
```

y reiniciar WSL2 desde PowerShell con `wsl --shutdown`.

### El contenedor `api` o `web` no arranca y el log muestra una excepción de configuración

Ambos hosts fallan rápido si falta o es inválida alguna variable obligatoria del `.env` (`JWT_SECRET_KEY`, `ADMIN_PASSWORD`, `CONNECTION_STRING`, etc.). El mensaje de la excepción agrega **todos** los errores detectados a la vez — revisar `docker compose logs api` (o `web`) y comparar el `.env` contra `.env.example`.

### `sqlserver` no llega a estado `healthy` y `api`/`web` se quedan esperando

`api` y `web` declaran `depends_on: sqlserver: condition: service_healthy`, por lo que no arrancan hasta que el healthcheck de `sqlserver` (`sqlcmd -Q "SELECT 1"`) responde correctamente. En máquinas con pocos recursos el arranque de SQL Server puede tardar más que el `start_period` (30s) — Docker seguirá reintentando (hasta 10 veces cada 15s); esperar o revisar `docker compose logs sqlserver` si el contenedor se marca como `unhealthy` de forma persistente.

### `SA_PASSWORD` rechazada por SQL Server

SQL Server exige una contraseña compleja (mínimo 8 caracteres, con mayúscula, minúscula, dígito y símbolo). Si `SA_PASSWORD` no la cumple, `sqlserver` arranca pero el healthcheck falla indefinidamente. Revisar el valor en `.env` y asegurarse de que coincide exactamente con la contraseña embebida en `CONNECTION_STRING`.

### Login con Google no funciona

El acceso mediante Google OAuth2 requiere registrar credenciales reales en [Google Cloud Console](https://console.cloud.google.com/apis/credentials) y rellenar `GOOGLE_CLIENT_ID`/`GOOGLE_CLIENT_SECRET` — no existe ningún proveedor simulado para este flujo. Si no se necesita probar este login, usar el login local con email/contraseña (ver [`h. Usuario y contraseña de prueba`](../../README.md#h-usuario-y-contraseña-de-prueba)).

Si sí se necesita probarlo, **crear un OAuth Client dedicado solo para desarrollo local** — nunca reutilizar el de producción, y nunca registrar `localhost` como redirect URI autorizado del cliente de producción:

1. En [Google Cloud Console → Credentials](https://console.cloud.google.com/apis/credentials), en el mismo proyecto que ya use el club (o uno propio), **Create Credentials → OAuth client ID → Web application**.
2. **Authorized JavaScript origins:** `http://localhost:5240` y `http://localhost:5123`.
3. **Authorized redirect URIs:** `http://localhost:5240/signin-google` (el `CallbackPath` que gestiona internamente el middleware de Google, no una ruta propia de la aplicación).
4. Si la pantalla de consentimiento OAuth está en modo "Testing", añadir la cuenta de Google que se vaya a usar como "Test user", o Google rechazará el login igualmente.
5. Copiar el `Client ID`/`Client Secret` resultantes a `GOOGLE_CLIENT_ID`/`GOOGLE_CLIENT_SECRET` en `.env`, y recrear el contenedor `api`: `docker compose up -d --force-recreate api`.

Errores esperados según el estado de la configuración:

| Síntoma | Causa |
|---|---|
| `Error 401: invalid_client — The OAuth client was not found` | `GOOGLE_CLIENT_ID` sigue siendo el placeholder de `.env.example`, o no corresponde a ningún cliente OAuth real |
| `redirect_uri_mismatch` | El `client_id` usado es válido pero no tiene `http://localhost:5240/signin-google` en sus "Authorized redirect URIs" (típicamente por reutilizar el cliente de producción, cuyos redirect URIs solo cubren el dominio público) |
| Tras el login, `https://localhost:7123/oauth-callback?...` → "No se pudo acceder a este sitio web" | Falta `WebAppBaseUrl` en el servicio `api` del compose — debe apuntar a `http://localhost:${WEB_PORT:-5123}` (ya corregido en `infrastructure/docker-compose/docker-compose.yml`; ver detalle en [`docs/technical/US-27-oauth2-authentication.md`](../technical/US-27-oauth2-authentication.md#configuración-real-en-docker-compose-local-2026-07-15)) |
