# Infraestructura como Código

> Referencia de diseño: [`issue-46-infraestructura-como-codigo.md`](../.claude/docs/sdlc/design/issue-46-infraestructura-como-codigo.md).

Esta carpeta es la ubicación única y versionada de toda la infraestructura como código (IaC) de `SportsClubEventManager`: cómo se orquesta el stack (Docker Compose) y cómo se automatiza su despliegue al homelab.

## Qué hay aquí

| Carpeta | Contenido |
|---|---|
| [`docker-compose/`](docker-compose/README.md) | Orquestación Docker Compose: stack de desarrollo local (`docker-compose.yml`) y stack de producción/homelab (`docker-compose.prod.yml`), ambos con la red interna `sportsclub-network`. |
| [`deploy/`](deploy/README.md) | Automatización de despliegue/rollback al homelab (issue #45, ya implementada): scripts de smoke test, cálculo del último tag correcto y rollback vía API de Portainer, más el runbook operativo completo. |

Los `Dockerfile.api`/`Dockerfile.web` (recetas de construcción de imagen) **no** viven aquí — permanecen en [`/docker/`](../docker/), ya que son una preocupación de *build* de aplicación, no de orquestación/despliegue de infraestructura.

## Desplegar desde cero (desarrollo local)

1. `cp .env.example .env` (en la raíz del repositorio) y rellenar las variables — ver la sección "c. Instalación y ejecución" del [`README`](../README.md) del repositorio para el detalle de cada variable.
2. `docker compose up --build` (desde la raíz del repositorio). El `docker-compose.yml` de la raíz es un fichero `include:` de dos líneas que resuelve exactamente el mismo stack que `infrastructure/docker-compose/docker-compose.yml` — el comando no ha cambiado respecto a versiones anteriores del repositorio.
3. La base de datos se crea y migra automáticamente al arrancar `api`/`web` (EF Core `MigrateDatabaseAsync()` en `Program.cs`) — no requiere ningún script de inicialización manual.

Ver [`docker-compose/README.md`](docker-compose/README.md) para el detalle de ambos ficheros Compose y sus comandos de verificación.

## Desplegar desde cero (producción / homelab)

El despliegue a producción se realiza vía Portainer, apuntando al stack `infrastructure/docker-compose/docker-compose.prod.yml`, y se automatiza completamente desde CI/CD (`.github/workflows/cd.yml` + `rollback.yml`). Ver:

- [`docker-compose/README.md`](docker-compose/README.md) — diferencias entre el stack de desarrollo y el de producción.
- [`deploy/README.md`](deploy/README.md) y el [runbook de despliegue](deploy/DEPLOYMENT_RUNBOOK.md) — flujo automático completo, rollback y *fallbacks* manuales.

## Qué NO cubre este IaC (alcance explícitamente fuera de esta issue)

- **Reverse proxy / ingress / DNS público**: gestionado a nivel de host (Nginx) y en Cloudflare, fuera de este repositorio — el homelab ya usa este mismo mecanismo hoy para exponer otras aplicaciones. Ver el runbook manual en el `## Apéndice A` del [diseño de esta issue](../.claude/docs/sdlc/design/issue-46-infraestructura-como-codigo.md#apéndice-a--runbook-manual-exponer-web-públicamente-ingressdns) para los pasos exactos que debe ejecutar el propietario del homelab.
- **Prometheus** (issue #42), **Grafana** (issue #43), **n8n** (issue #37): cada uno tiene su propia issue de seguimiento en este repositorio; no se implementan aquí.
- **Terraform**: no aplica — no hay ningún recurso cloud que aprovisionar; el despliegue real es un único nodo Docker/Portainer del homelab.
- **Backup/restauración del volumen `sqlserver_data`**: fuera de alcance de esta issue; seguimiento en la [issue #100](https://github.com/AlejBlasco/SportsClubEventManager/issues/100).
