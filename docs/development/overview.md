# Stack tecnológico

Detalle del stack tecnológico utilizado en **SportsClubEventManager**, referenciado desde la sección [`b. Stack tecnológico utilizado`](../../README.md#b-stack-tecnológico-utilizado) del README.

## Plataforma

| Tecnología | Detalle |
|---|---|
| .NET | .NET 10 (SDK `10.0.100` fijado en `global.json`, `rollForward: latestFeature`) |
| Lenguaje | C#, `LangVersion` 13.0, `Nullable` habilitado, warnings tratados como errores (`TreatWarningsAsErrors`) |

## Backend / API

| Tecnología | Detalle |
|---|---|
| ASP.NET Core Web API | Expone la API REST consumida por `SportsClubEventManager.Web` |
| Arquitectura | CQRS con **MediatR** (`12.5.0`) para separar comandos y consultas |
| Validación | **FluentValidation** (`12.1.1`) en `Application`, integrada en la API vía `FluentValidation.AspNetCore` |
| Documentación de API | Swagger / OpenAPI (`Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore.SwaggerUI`), accesible en `/swagger` |

## Frontend

| Tecnología | Detalle |
|---|---|
| Blazor Server | Interfaz de usuario (`SportsClubEventManager.Web`), renderizado en servidor |
| Radzen.Blazor | Librería de componentes UI (calendario, tablas, formularios) |

## Persistencia

| Tecnología | Detalle |
|---|---|
| Entity Framework Core | `Microsoft.EntityFrameworkCore` / `.SqlServer` / `.Tools` / `.Design` (`10.0.9`), migraciones versionadas en `Infrastructure` |
| SQL Server | SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest` en Docker; LocalDB soportado en desarrollo local) |

## Autenticación y autorización

| Tecnología | Detalle |
|---|---|
| OAuth2 | Login con **Google** vía `Microsoft.AspNetCore.Authentication.Google` |
| Login local | Email + contraseña con **BCrypt.Net-Next** (factor de coste 12) |
| Tokens | **JWT** (`Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`) + cookies de sesión con expiración deslizante |
| Autorización | RBAC con dos roles (`User`, `Administrator`), políticas por claim de rol |

## Importación de datos

| Tecnología | Detalle |
|---|---|
| CsvHelper | `33.1.0` — carga masiva de eventos desde fichero CSV, con previsualización y detección de duplicados |

## Logging y observabilidad

| Tecnología | Detalle |
|---|---|
| Serilog | `Serilog.AspNetCore` + `Serilog.Enrichers.Environment`, logging estructurado en `Infrastructure` |
| Prometheus | `prometheus-net.AspNetCore` (Api, Web) y `prometheus-net` (Infrastructure) — métricas HTTP y de negocio expuestas en `/metrics` |
| Grafana | Dashboard "SportsClubEventManager - Overview" versionado como código (`infrastructure/grafana/`) |
| node-exporter / cAdvisor | Métricas de CPU/memoria/disco de host y contenedor, solo en el stack de desarrollo local |

## Automatización de flujos

| Tecnología | Detalle |
|---|---|
| n8n | Instancia ya existente del homelab (no desplegada por este repositorio); la Api invoca sus *webhooks* para notificaciones de negocio (confirmación de inscripción, actualización/cancelación de eventos, recordatorios) |

## Contenedores

| Tecnología | Detalle |
|---|---|
| Docker | Imágenes multi-stage (`docker/Dockerfile.api`, `docker/Dockerfile.web`) sobre `mcr.microsoft.com/dotnet/sdk:10.0` y `aspnet:10.0` |
| Docker Compose | Orquestación de `sqlserver` + `api` + `web` en producción; añade `prometheus` + `grafana` + `node-exporter` + `cadvisor` solo en desarrollo local (`infrastructure/docker-compose/`) |

## CI/CD

| Tecnología | Detalle |
|---|---|
| GitHub Actions | `ci.yml` (build + tests en cada PR), `cd.yml` (build, publicación de imágenes en GHCR y despliegue vía webhook de Portainer en `master`), `rollback.yml` (rollback automático), `release.yml` (publicación de GitHub Releases) |
| Registro de imágenes | GitHub Container Registry (GHCR) |
| Despliegue | Webhook de **Portainer** contra el homelab, con smoke test post-despliegue y rollback automatizado |

## Testing

| Capa | Frameworks |
|---|---|
| Backend (unitario) | **xUnit** (`2.9.3`), **FluentAssertions**, **NSubstitute**, **Bogus** (datos de prueba), `coverlet.collector` |
| Blazor | **bUnit**, **WireMock.Net** (mock de llamadas HTTP a la API) |
| Integración | **Testcontainers.MsSql**, **Respawn** (reseteo de base de datos), `Microsoft.AspNetCore.Mvc.Testing` |
