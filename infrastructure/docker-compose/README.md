# Docker Compose

> Referencia de diseño: [`issue-46-infraestructura-como-codigo.md`](../../.claude/docs/sdlc/design/issue-46-infraestructura-como-codigo.md).

Esta carpeta contiene los dos ficheros Docker Compose reales del proyecto. Ambos declaran el mismo stack lógico (`sqlserver`, `api`, `web`, `prometheus` desde la issue #42) sobre una red interna nombrada `sportsclub-network` (`driver: bridge`), pero difieren en cómo obtienen las imágenes de `api`/`web`.

> El servicio `prometheus` (issue #42) scrapea `/metrics` de `api` y `web` según [`prometheus/prometheus.yml`](prometheus/prometheus.yml) y publica su UI únicamente en `${PROMETHEUS_BIND_ADDRESS:-127.0.0.1}:${PROMETHEUS_PORT:-9090}` — ver la sección de observabilidad del [`README`](../../README.md) del repositorio para el detalle completo, incluida la restricción de acceso en producción a la red interna del homelab/Tailscale VPN.

## `docker-compose.yml` — desarrollo local

- Construye las imágenes de `api`/`web` en local (`build:`), con `dockerfile: docker/Dockerfile.api` / `docker/Dockerfile.web` y `context: ../..` (la raíz del repositorio — este fichero vive dos niveles por debajo de ella).
- Es el fichero real detrás del `docker-compose.yml` de la raíz del repositorio, que desde esta issue es un *wrapper* de dos líneas con la clave `include:`:

  ```yaml
  include:
    - infrastructure/docker-compose/docker-compose.yml
  ```

  Gracias a `include:`, el directorio de proyecto de Compose (y por tanto la resolución por defecto de `.env`) sigue siendo la raíz del repositorio. Esto significa que **el flujo de desarrollo documentado en el README no cambia**: sigue siendo `cp .env.example .env` + `docker compose up --build`, ejecutado desde la raíz.
- Publica los puertos configurables (`API_PORT`, `WEB_PORT`, `SQL_PORT`) al host, para poder acceder a la aplicación y a SQL Server directamente durante el desarrollo.

## `docker-compose.prod.yml` — producción / homelab

- Usa imágenes ya publicadas en GHCR (`image: ghcr.io/alejblasco/sportsclubeventsmanager-{api,web}:${APP_VERSION:-latest}`) con `pull_policy: always`, en vez de `build:`. `APP_VERSION` permite fijar una versión concreta para hacer rollback sin reconstruir nada (ver [`../deploy/README.md`](../deploy/README.md)).
- Añade `restart: unless-stopped` a los tres servicios, propio de un despliegue persistente en el homelab.
- Es el stack que Portainer despliega realmente en el servidor del homelab, apuntando a este fichero mediante la configuración de su propio stack (fuera de este repositorio).

## Diferencias funcionales entre ambos ficheros

| | `docker-compose.yml` (dev) | `docker-compose.prod.yml` (producción) |
|---|---|---|
| Origen de la imagen | `build:` local (`context: ../..`) | `image:` publicada en GHCR + `pull_policy: always` |
| Reinicio | Por defecto de Docker (no declarado) | `restart: unless-stopped` |
| Versión desplegada | Siempre la última construida en local | `${APP_VERSION:-latest}` — permite fijar un tag concreto para rollback |
| Resto (`secrets:`, `healthcheck`, `depends_on`, `networks:`, `volumes:`) | Idéntico |

## Comandos de verificación

Validar sintácticamente ambos ficheros (sin arrancar nada):

```bash
# Desde la raíz del repositorio:
docker compose -f infrastructure/docker-compose/docker-compose.yml --project-directory . config
docker compose -f infrastructure/docker-compose/docker-compose.prod.yml config
```

El primer comando confirma, entre otras cosas, que `build.context` de `api`/`web` resuelve a la ruta absoluta de la raíz del repositorio (no a `infrastructure/docker-compose/`).

Levantar el stack de desarrollo completo (equivalente a ejecutar el fichero incluido directamente):

```bash
docker compose up --build
```

## Paso manual pendiente — actualizar la ruta del stack en Portainer

`docker-compose.prod.yml` se ha movido en esta issue desde `docker/docker-compose.prod.yml` a `infrastructure/docker-compose/docker-compose.prod.yml`. Este cambio de ruta **no se aplica solo** en Portainer: el propietario del homelab debe actualizar manualmente, en la configuración del stack de producción, la ruta del fichero Compose a la nueva ubicación, antes o inmediatamente después de fusionar esta PR.

Si no se actualiza antes del siguiente redeploy (manual o vía el webhook GitOps ya existente de `cd.yml`), Portainer dejará de encontrar el fichero en la ruta antigua y el redeploy fallará. Este paso es puramente operativo y no puede ejecutarse desde este repositorio ni desde un agente de desarrollo — ver `## Riesgos y Decisiones Abiertas`, punto 4, del [diseño de esta issue](../../.claude/docs/sdlc/design/issue-46-infraestructura-como-codigo.md).
