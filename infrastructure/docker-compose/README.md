# Docker Compose

> Referencia de diseño: [`issue-46-infraestructura-como-codigo.md`](../../.claude/docs/sdlc/design/issue-46-infraestructura-como-codigo.md), [`issue-42-integracion-prometheus-metricas.md`](../../.claude/docs/sdlc/design/issue-42-integracion-prometheus-metricas.md), [`issue-43-dashboard-grafana-monitorizacion.md`](../../.claude/docs/sdlc/design/issue-43-dashboard-grafana-monitorizacion.md).

Esta carpeta contiene los dos ficheros Docker Compose reales del proyecto. Ambos declaran `sqlserver`, `api` y `web` sobre una red interna nombrada `sportsclub-network` (`driver: bridge`), pero difieren en cómo obtienen las imágenes de `api`/`web`. **Solo `docker-compose.yml` (desarrollo)** añade además `prometheus` (issue #42), `grafana`, `node-exporter` y `cadvisor` (issue #43) — `docker-compose.prod.yml` no declara ninguno de los cuatro: el homelab ya tiene su propio stack de Portainer independiente (`monitoring`) con los cuatro, y añadir una segunda copia en producción duplicaría métricas de host/contenedor y arriesgaría colisión de puertos con los servicios reales de `monitoring`.
>
> El `prometheus` de desarrollo scrapea `/metrics` de `api` y `web`, además de `node-exporter` y `cadvisor`, según [`prometheus/prometheus.yml`](prometheus/prometheus.yml), y publica su UI únicamente en `${PROMETHEUS_BIND_ADDRESS:-127.0.0.1}:${PROMETHEUS_PORT:-9090}`. En producción, ese mismo scrape lo hace el Prometheus ya existente del stack `monitoring`, mediante un runbook manual — ver la sección de observabilidad del [`README`](../../README.md) del repositorio y el `## Apéndice A` del [diseño de la issue #42](../../.claude/docs/sdlc/design/issue-42-integracion-prometheus-metricas.md).
>
> El `grafana` de desarrollo consume el *datasource* Prometheus y el dashboard versionado en [`../grafana/`](../grafana/), y publica su UI en `${GRAFANA_BIND_ADDRESS:-127.0.0.1}:${GRAFANA_PORT:-3000}` — acotado a la propia máquina, igual que `prometheus`. En producción, ese mismo dashboard se aplica sobre la Grafana ya existente del stack `monitoring` (*provisioning* por fichero) y se publica de forma acotada mediante Grafana Public Dashboards + una ruta dedicada del Cloudflare Tunnel del homelab — ver el `## Apéndice A` del [diseño de la issue #43](../../.claude/docs/sdlc/design/issue-43-dashboard-grafana-monitorizacion.md) y la sección "Observabilidad y métricas" del [`README`](../../README.md) raíz.

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
| Observabilidad (`prometheus`/`grafana`/`node-exporter`/`cadvisor`) | Los cuatro presentes (issues #42/#43) | **Ninguno de los cuatro** — el homelab ya los tiene en su stack `monitoring`, ver nota al principio de este documento |
| Resto (`sqlserver`/`api`/`web`: `healthcheck`, `depends_on`, `networks:`, `volumes:`) | Idéntico |

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

## Limitación conocida — `node-exporter` en WSL2 (desarrollo local)

Al ejecutar `docker compose up --build` en un host **Windows con Docker corriendo sobre WSL2**, el servicio `node-exporter` (issue #43) puede fallar al arrancar con:

```
Error response from daemon: path / is mounted on / but it is not a shared or slave mount
```

**Causa:** `node-exporter` monta `/:/host:ro,rslave` (ver `volumes:` del servicio en [`docker-compose.yml`](docker-compose.yml), el único fichero de este repositorio que lo declara — no existe en `docker-compose.prod.yml`) para poder inspeccionar el host. La propagación `rslave` exige explícitamente que el mount de origen ya sea `shared`/`slave` en el host, y WSL2 monta `/` como privada por defecto — no es un problema del stack en sí, sino del entorno WSL2. `cadvisor` monta `/:/rootfs:ro` en el mismo fichero, **sin** flag de propagación, por lo que no se ve afectado por este mismo error.

Al ser un problema del **host de desarrollo**, no del homelab de producción (servidor Linux real detrás de Portainer), no se corrige en ningún fichero de este repositorio. Se soluciona en la propia distro WSL2:

```bash
# Dentro de una terminal WSL2 (no PowerShell), una vez por arranque:
sudo mount --make-rshared /
```

Para que se aplique automáticamente en cada arranque de WSL2, añadir en `/etc/wsl.conf` de esa distro:

```ini
[boot]
command = "mount --make-rshared /"
```

y reiniciar WSL2 desde PowerShell con `wsl --shutdown`.

## Histórico — migración de ruta del stack en Portainer (issue #46, resuelto)

`docker-compose.prod.yml` se movió, en su día, de `docker/docker-compose.prod.yml` a `infrastructure/docker-compose/docker-compose.prod.yml`. Ese cambio de ruta no se aplicaba solo en Portainer — requería que el propietario del homelab actualizara manualmente, en la configuración del stack de producción, la ruta del fichero Compose a la nueva ubicación. **Ya se aplicó**: los despliegues automáticos vía `cd.yml` llevan funcionando con éxito desde entonces (9 tags `deployed/homelab/*` confirmados), lo que no sería posible si Portainer siguiera apuntando a la ruta antigua.
