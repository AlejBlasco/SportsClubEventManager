# Runbook de despliegue al homelab

> Referencia de diseño: [`issue-45-despliegue-automatizado-al-homelab.md`](../../.claude/docs/sdlc/design/issue-45-despliegue-automatizado-al-homelab.md).

Este documento describe cómo se despliega `SportsClubEventManager` al homelab en condiciones normales (flujo automático), y qué hacer si algo falla y hace falta intervenir a mano.

## 1. Flujo automático (camino feliz)

1. Un `push` a `master` (o un `workflow_dispatch` manual) dispara `.github/workflows/cd.yml`.
2. `validate` (issue #44) construye ambas imágenes localmente, las escanea con Trivy y ejecuta un smoke test contra un SQL Server efímero. Si falla, el pipeline se detiene aquí y no se publica ni despliega nada.
3. `build-and-push` publica `api` y `web` en GHCR con las etiquetas `latest`, `sha-<hash-corto>` y la versión de `Directory.Build.props`.
4. `deploy` llama al **webhook de GitOps de Portainer** (`secrets.PORTAINER_WEBHOOK_URL`), que hace que Portainer vuelva a hacer `pull` de las imágenes (`pull_policy: always`) y recree los contenedores `api`/`web` del stack `docker/docker-compose.prod.yml`.
5. `post-deploy-smoke-test` espera a que el despliegue esté realmente sano contra la URL pública real (`secrets.HOMELAB_WEB_URL`, entorno `homelab-production`):
   - `GET /health/live` (bloqueante, hasta 6 intentos cada 15s, ~90s máx.).
   - `GET /health/ready` (mismo patrón de reintentos) — esto también valida `Api` de forma transitiva, ver `ApiAvailabilityHealthCheck` (issue #41).
   - Si cualquiera de los dos falla, el job ejecuta `find-last-good-tag.sh`, escribe el tag `sha-*` correcto anterior y las instrucciones de rollback en el resumen del job (`$GITHUB_STEP_SUMMARY`) con `::error::`, y falla — lo que marca el `Deployment` de `homelab-production` como `failure`, visible en la pestaña **Environments** del repositorio.
6. Si el smoke test pasa, `tag-deployed-version` crea y empuja el tag ligero `deployed/homelab/<sha-corto>` sobre el commit desplegado, usando el `GITHUB_TOKEN` de la propia ejecución. Este tag es la fuente de verdad de "qué se ha desplegado con éxito y cuándo", y es lo que consume `rollback.yml` como valores válidos de `version`.

En ningún punto de este flujo se necesita abrir la UI de Portainer ni ejecutar nada a mano — es el comportamiento esperado una vez cargados los cuatro secretos/variables descritos en el `## Apéndice A` del diseño.

## 2. Rollback automático (`rollback.yml`)

Si un despliegue queda en mal estado (o simplemente se quiere volver a una versión anterior), el rollback también es automático, sin tocar la UI de Portainer:

```bash
# Ver qué versiones se han desplegado con éxito (más reciente primero):
git fetch --tags
git tag -l 'deployed/homelab/*' --sort=-creatordate

# Lanzar el rollback a una de esas versiones (usar solo el hash corto,
# SIN el prefijo "sha-" — el workflow lo añade internamente):
gh workflow run rollback.yml -f version=abc1234
```

También puede lanzarse desde la pestaña **Actions → Rollback Homelab Deployment → Run workflow** de GitHub, indicando el mismo valor en el campo `version`.

`rollback.yml` ejecuta, en orden:

1. **`validate-version`**: comprueba que existe el tag `deployed/homelab/<version>`. Si no existe, falla con un mensaje explícito en vez de intentar desplegar un commit que nunca llegó a desplegarse con éxito.
2. **`portainer-rollback`**: llama a `infrastructure/deploy/portainer-rollback.sh "sha-<version>"`, que se autentica contra la API de Portainer (`X-API-Key`), localiza el stack de producción por nombre y hace `PUT /api/stacks/{id}` fijando la variable de entorno de stack `APP_VERSION=sha-<version>` y forzando `pullImage: true`. La imagen con ese tag ya existe en GHCR — no se reconstruye nada.
3. **`post-rollback-smoke-test`**: reutiliza `infrastructure/deploy/smoke-test.sh` (los mismos checks de `/health/live` y `/health/ready` que el despliegue normal). Si falla, el job falla con una alerta explícita — **no hay reintento automático adicional ni "rollback del rollback"**; en ese caso hay que seguir el procedimiento manual de la sección 4.

## 3. Fallback manual: "Pull and redeploy" desde la UI de Portainer

Si el webhook de Portainer no está disponible (secreto no cargado, API/host inalcanzable, etc.), el despliegue normal puede hacerse a mano:

1. Entrar en Portainer → seleccionar el **Environment** del nodo del homelab.
2. Ir a **Stacks** → abrir el stack de producción (por defecto `sportsclubeventmanager-prod`, ver `PORTAINER_STACK_NAME` en `portainer-rollback.sh` si el nombre real difiere).
3. Pulsar **"Pull and redeploy"** (o **"Update the stack"** con la opción **"Re-pull image"** activada, según la versión de Portainer). Esto vuelve a hacer `pull` de la imagen `:${APP_VERSION:-latest}` configurada actualmente y recrea los contenedores `api`/`web`.
4. Verificar manualmente `GET https://<HOMELAB_WEB_URL>/health/live` y `GET https://<HOMELAB_WEB_URL>/health/ready` para confirmar que el despliegue quedó sano (los mismos checks que hace `smoke-test.sh`).

## 4. Fallback manual: rollback paso a paso en Portainer

Si `rollback.yml` no está disponible (p. ej. porque la API completa de Portainer no es alcanzable desde runners de GitHub-hosted, ver riesgo residual en el diseño), el rollback puede hacerse a mano con el mismo resultado final:

1. Elegir a qué versión volver a partir de los tags `deployed/homelab/*` (ver comando `git tag -l` de la sección 2), o ejecutando `infrastructure/deploy/find-last-good-tag.sh <sha-actual>` para obtener automáticamente el último tag correcto anterior.
2. En Portainer → **Stacks** → abrir el stack de producción → sección de **variables de entorno del stack**.
3. Fijar (o editar) la variable `APP_VERSION` al valor `sha-<hash-corto>` elegido en el paso 1.
4. Pulsar **"Update the stack"** con la opción **"Re-pull image"** activada — la imagen con ese tag ya existe en GHCR, así que no hace falta reconstruir nada.
5. Verificar manualmente `/health/live` y `/health/ready` como en la sección 3, punto 4.
6. Una vez confirmado que el rollback fue efectivo, documentar el incidente y, si procede, dejar `APP_VERSION` fijada a ese valor hasta que se publique una corrección — el siguiente `push` a `master` que pase el smoke test volverá a mover el despliegue hacia adelante de forma automática.

## 5. Prerrequisitos operativos (fuera del alcance de este repositorio)

Antes de que el flujo automático de las secciones 1 y 2 funcione de verdad, alguien con acceso al homelab y a la configuración del repositorio de GitHub debe:

- Crear la **GitHub Environment** `homelab-production`.
- Cargar en ese entorno los cuatro secretos/variables: `PORTAINER_WEBHOOK_URL`, `HOMELAB_WEB_URL`, `PORTAINER_API_URL`, `PORTAINER_API_KEY`.
- Confirmar que el stack `docker/docker-compose.prod.yml` en Portainer tiene el **webhook de GitOps** activado con **"Re-pull image"**, y que existe la variable de entorno de stack `APP_VERSION` sin un valor por defecto forzado (para que `${APP_VERSION:-latest}` decida).
- Confirmar que la API de Portainer (`PORTAINER_API_URL`) es alcanzable desde runners GitHub-hosted (`ubuntu-latest`), no solo la ruta del webhook.

Los pasos exactos para generar cada secreto/token están documentados en el `## Apéndice A` del diseño de esta issue. Ninguno de estos pasos se realiza desde este repositorio ni desde el pipeline de agentes de desarrollo — son responsabilidad del propietario del homelab.
