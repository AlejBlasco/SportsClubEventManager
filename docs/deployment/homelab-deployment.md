# Despliegue automático al homelab

Guía paso a paso para configurar y ejecutar el despliegue continuo de **SportsClubEventManager** al homelab. Para el procedimiento operativo día a día (camino feliz + rollback + fallbacks manuales desde la UI de Portainer) ver también [`infrastructure/deploy/DEPLOYMENT_RUNBOOK.md`](../../infrastructure/deploy/DEPLOYMENT_RUNBOOK.md).

El despliegue se apoya en **Docker Compose + Portainer** sobre un homelab personal (no hay Kubernetes), accesible **exclusivamente por Tailscale** — no hay ninguna ruta pública a Portainer. Existen dos flujos: la **configuración inicial** (solo la primera vez que se conecta un homelab) y **hacer un despliegue** (cada vez que se publica una nueva versión). Sigue el que corresponda.

## Requisitos previos

- Acceso de administrador al repositorio de GitHub (para configurar Environments y secretos).
- Acceso al panel de administración de [Tailscale](https://login.tailscale.com/admin) de la tailnet del homelab.
- Acceso a la instancia de Portainer del homelab, con permisos para crear stacks, webhooks y tokens de API.
- [`gh` CLI](https://cli.github.com/) autenticado (`gh auth login`) — se usa en varios pasos para crear PRs, tags y consultar runs.

## Configuración inicial (solo la primera vez)

### Paso 1 — Crear el GitHub Environment `homelab-production`

En el repositorio: **Settings → Environments → New environment**, nombre `homelab-production`.

Cargar estos secretos en ese Environment:

| Secreto | Qué es | De dónde sale |
|---|---|---|
| `PORTAINER_WEBHOOK_URL` | URL de un solo uso que dispara el redeploy del stack | Portainer → Stack → **Webhooks** (activar, con **"Re-pull image"**) |
| `PORTAINER_API_URL` | URL base de la instancia de Portainer (sin `/api` al final) | La tuya |
| `PORTAINER_API_KEY` | Token de la API de Portainer, usado por el rollback automático | Portainer → **My account → Access tokens** |
| `HOMELAB_WEB_URL` | URL (Tailscale) donde responde el servicio `web` | La tuya |
| `TS_AUTHKEY` | Auth key de Tailscale para que los runners de GitHub se unan a la tailnet (Paso 2) | Tailscale admin console |

Y esta variable (no secreta), a nivel de repositorio o del propio entorno `homelab-production`:

| Variable | Qué es |
|---|---|
| `PORTAINER_STACK_NAME` | Nombre real del stack en Portainer. Por defecto los scripts asumen `sportsclubeventmanager-prod`; si el stack se llama distinto (en el homelab actual se llama `sportsclub`), esta variable es obligatoria. |

> `HOMELAB_WEB_URL` puede definirse como **variable** en vez de secreto si se quiere que aparezca como enlace clicable en la pestaña **Environments** del repositorio (`cd.yml` la lee con `vars.HOMELAB_WEB_URL || secrets.HOMELAB_WEB_URL`, funciona igual en ambos casos).

### Paso 2 — Dar acceso a GitHub Actions a la tailnet

Los runners de GitHub Actions (`ubuntu-latest`) no están en la tailnet del homelab por defecto, así que se unen a ella temporalmente en cada job que lo necesita, vía `tailscale/github-action`.

1. En el admin console de Tailscale: **Settings → Keys → Generate auth key**.
   - Marcar **Reusable** y **Ephemeral**.
   - Etiqueta (`tag`): `tag:ci` (crearla antes en **Access Controls** si no existe).
   - Expiración: la máxima permitida (90 días) — **hay que rotarla antes de que caduque**, o los jobs `deploy`/`post-deploy-smoke-test` (`cd.yml`) y `portainer-rollback`/`post-rollback-smoke-test` (`rollback.yml`) empezarán a fallar en el step "Connect to Tailscale".
2. Copiar la key generada como el secret `TS_AUTHKEY` del Environment `homelab-production` (Paso 1).
3. Confirmar que la ACL de la tailnet permite que `tag:ci` alcance el host de Portainer y el de `web` (con la política por defecto, permitir todo, no hace falta tocar nada más).

### Paso 3 — Configurar el stack en Portainer

Crear el stack de producción en Portainer apuntando al contenido de [`infrastructure/docker-compose/docker-compose.prod.yml`](../../infrastructure/docker-compose/docker-compose.prod.yml), con:

- Las variables de entorno del stack: `SA_PASSWORD`, `CONNECTION_STRING`, `JWT_SECRET_KEY`, `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `ADMIN_PASSWORD`, `API_PORT`, `WEB_PORT`, `ASPNETCORE_ENVIRONMENT`, `WEB_EXTERNAL_URL`. Estos 5 valores sensibles van como **variables de entorno normales del stack**, no como Docker secrets — ver [Troubleshooting](#docker-secrets-de-fichero-secrets-nombre-file--no-llegan-a-la-app-en-portainer).
- El **webhook de GitOps** activado con **"Re-pull image"** — si no, el redeploy reinicia los contenedores pero puede no traer una imagen nueva con el mismo tag `latest`.
- `APP_VERSION` **sin** un valor fijo forzado, para que `${APP_VERSION:-latest}` decida (el compose usa `pull_policy: always`).

### Paso 4 — Confirmar la protección de la rama `master`

`.github/workflows/branch-protection.yml` configura sobre `master`: 1 aprobación de PR, checks en verde (`build-and-test`, `validate (api)`, `validate (web)`) e historial lineal (solo *squash*/*rebase* merge).

> Si eres el único colaborador del repositorio, GitHub nunca cuenta tu propia aprobación como autor de la PR — con `enforce_admins: true` no podrías mergear nunca ninguna PR. Por eso `enforce_admins` está en `false`: como admin puedes usar el botón **"Merge without waiting for requirements to be met"** sin desactivar el resto de reglas para otros colaboradores. Revísalo (vuelve a `true`) en cuanto haya un segundo colaborador que pueda aprobar PRs.

Con esto, la configuración inicial está completa. Los siguientes pasos son los que se repiten **en cada despliegue**.

## Cómo hacer un despliegue (release)

Cada despliegue publica una nueva versión SemVer (`vX.Y.Z`) desde `develop` hacia `master`, y termina con una GitHub Release. El pipeline construye, publica y despliega solo — tu parte es preparar la versión y mergear.

### Paso 1 — Crear la rama de release desde `develop`

```powershell
git checkout develop
git pull
git checkout -b release/vX.Y.Z
```

### Paso 2 — Bump de versión y cierre del CHANGELOG

En [`Directory.Build.props`](../../Directory.Build.props), sube `<Version>`:

```xml
<Version>X.Y.Z</Version>
```

En [`CHANGELOG.md`](../../CHANGELOG.md), mueve el contenido de `## [Unreleased]` a una nueva sección con la fecha de hoy, dejando `## [Unreleased]` vacío:

```markdown
## [Unreleased]

## [X.Y.Z] - AAAA-MM-DD

### Added
- ...

### Changed
- ...

### Fixed
- ...
```

> `release.yml` (Paso 5) fallará si esta sección no existe o está vacía — ver [Troubleshooting](#releaseyml-falla-con-section--xyz--was-found-but-has-no-real-content).

### Paso 3 — Commit, push y abrir la PR contra `master`

```powershell
git add Directory.Build.props CHANGELOG.md
git commit -m "release: vX.Y.Z"
git push -u origin release/vX.Y.Z

gh pr create --base master --head release/vX.Y.Z --title "release: vX.Y.Z"
```

### Paso 4 — Esperar los checks y mergear

```powershell
gh pr checks release/vX.Y.Z --watch
```

Cuando `build-and-test`, `validate (api)` y `validate (web)` estén en verde:

```powershell
gh pr merge release/vX.Y.Z --squash --delete-branch
```

(`master` exige historial lineal — siempre *squash* o *rebase*, nunca *merge commit*.)

### Paso 5 — El despliegue ocurre automáticamente — no hace falta nada más

El `push` a `master` dispara `cd.yml` completo, sin intervención manual:

1. **`validate`** — reconstruye y escanea (Trivy) las imágenes `api`/`web`, y las somete a un smoke test local.
2. **`build-and-push`** — publica ambas imágenes en GHCR con las etiquetas `latest`, `sha-<hash-corto>` y `X.Y.Z`.
3. **`deploy`** — llama al webhook de Portainer, que vuelve a hacer `pull` y recrea los contenedores.
4. **`post-deploy-smoke-test`** — comprueba `GET /health/live` y `GET /health/ready` contra la URL real del homelab.
5. **`tag-deployed-version`** — crea el tag `deployed/homelab/<sha-corto>`, fuente de verdad de qué versión está desplegada.

Sigue el progreso con:

```powershell
gh run watch --exit-status
```

Si `post-deploy-smoke-test` falla, el propio job deja en el resumen del run el comando exacto de rollback (ver [Rollback](#rollback)).

### Paso 6 — Crear el tag SemVer y publicar la GitHub Release

Solo después de confirmar que el Paso 5 terminó en verde:

```powershell
git checkout master
git pull
git tag vX.Y.Z
git push origin vX.Y.Z
```

Esto dispara `release.yml`, que valida que `Directory.Build.props` coincide con el tag, extrae la sección `## [X.Y.Z]` de `CHANGELOG.md` y publica la GitHub Release con esas notas — no reconstruye ni redespliega nada (ya se hizo en el Paso 5).

```powershell
gh release view vX.Y.Z
```

### Paso 7 — Sincronizar `develop` con `master`

```powershell
git checkout develop
git pull
git merge master
git push
```

## Verificación manual

Para comprobar el estado del despliegue sin depender de la UI de Portainer:

```bash
# Desde una máquina conectada a la tailnet:
curl -s -o /dev/null -w '%{http_code}\n' https://<HOMELAB_WEB_URL>/health/live
curl -s -o /dev/null -w '%{http_code}\n' https://<HOMELAB_WEB_URL>/health/ready
```

Por SSH al host del homelab, para confirmar que los contenedores corren la imagen esperada y que el `HEALTHCHECK` de Docker apunta a `/health/live` (no a `/`, que fue el síntoma de una imagen vieja — ver [Troubleshooting](#la-imagen-latest-en-ghcr-estaba-desactualizada)):

```bash
docker inspect --format='{{.Config.Image}} | Health: {{.State.Health.Status}}' sportsclub-api-1 sportsclub-web-1
docker inspect --format='{{json .Config.Healthcheck.Test}}' sportsclub-api-1
```

## Rollback

Ver [`DEPLOYMENT_RUNBOOK.md`, sección 2](../../infrastructure/deploy/DEPLOYMENT_RUNBOOK.md#2-rollback-automático-rollbackyml) para el procedimiento completo. Resumen:

```bash
git fetch --tags
git tag -l 'deployed/homelab/*' --sort=-creatordate   # versiones ya desplegadas con éxito
gh workflow run rollback.yml -f version=<hash-corto>  # sin el prefijo "sha-"
```

`rollback.yml` valida que el tag exista, actualiza `APP_VERSION` en el stack de Portainer vía su API (sin reconstruir nada, la imagen ya existe en GHCR) y vuelve a comprobar `/health/live`/`/health/ready`. Si la API de Portainer no está disponible, el runbook documenta el fallback manual paso a paso desde la UI.

## Troubleshooting

### El pipeline de CD solo corría en `develop`, nunca en `master`

**Causa:** `cd.yml` se dispara con `push` a `master`. Si todo el trabajo de CI/CD se desarrolló y probó en `develop` sin haber completado nunca un merge real a `master`, el pipeline nunca llegó a ejecutarse de verdad contra el homelab.

**Solución:** completar el primer merge real de `develop` a `master` (o lanzar `cd.yml` manualmente con `workflow_dispatch`) para validar el flujo de punta a punta al menos una vez.

### `validate (api)` / `validate (web)` se quedan en "Expected — Waiting for status to be reported" para siempre

**Causa:** el job `validate` usa `strategy.matrix.include` con tres claves (`service`, `dockerfile`, `image`). Sin un `name:` explícito en el job, GitHub Actions nombra el check con **todas** las claves del matrix, no solo `service` — el check real se reporta como `validate (api, docker/Dockerfile.api, ghcr.io/...)`, que nunca coincide con el string exacto `validate (api)` que exige `branch-protection.yml` (comprobable con `gh api repos/<owner>/<repo>/branches/master/protection/required_status_checks`). El check requerido queda pendiente indefinidamente aunque el job real termine en verde.

**Solución:** añadir `name: validate (${{ matrix.service }})` al job en `cd.yml`, forzando el nombre del check a depender solo de `matrix.service`.

### `release.yml` falla con "Section '## [X.Y.Z]' ... has no real content (empty section)"

**Causa:** aunque el `CHANGELOG.md` tenga contenido real bajo `## [X.Y.Z]`, `extract-changelog-section.sh` pasaba el patrón regex ya escapado (`^## \[X\.Y\.Z\]`) a `awk` vía `-v pattern="..."`. `awk -v` reprocesa las secuencias de escape del valor asignado, y como `\[`, `\.`, `\]` no son escapes válidos de C, gawk las trata como "escape sequence treated as plain" (visible como warnings en el log) y elimina las barras invertidas. El patrón que `awk` usa de verdad acaba siendo `^## [X.Y.Z]`, donde `[X.Y.Z]` deja de ser literal y pasa a ser una **clase de caracteres** que nunca hace match con la cabecera real, así que la sección sale vacía.

**Solución:** pasar el patrón vía variable de entorno en vez de `-v` (`PATTERN="$HEADER_PATTERN" awk '... ENVIRON["PATTERN"] ...'`) — los valores de entorno no sufren ese reprocesado de escapes.

### La imagen `:latest` en GHCR estaba desactualizada

**Causa:** antes de completar el primer `push` a `master` real, la imagen `:latest` en GHCR era anterior a la introducción de `/health/live`/`/health/ready`. Portainer marcaba el contenedor como "healthy" porque el `HEALTHCHECK` de esa imagen antigua comprobaba `/` en vez de `/health/live` — un falso positivo que la UI de Portainer no revela.

**Solución:** no confiar solo en el estado de Portainer; verificar por SSH con `docker inspect` (ver [Verificación manual](#verificación-manual)) qué imagen y qué healthcheck corre realmente el contenedor.

### Trivy falla en el step SARIF sin mensaje de error

**Causa:** combinar `format: sarif` con `severity: CRITICAL` en el mismo step de Trivy hacía morir el proceso sin traza, reproducido de forma consistente en CI pero no en local con las mismas flags.

**Solución:** el step SARIF ya no filtra por severidad (queda como informativo, para la pestaña Security); el gate real de severidad vive en un step aparte, "Fail on CRITICAL vulnerabilities", que cuenta los `CRITICAL` a partir del informe en formato tabla.

### El smoke test de CI falla porque falta `AdminUser__Password`

**Causa:** la migración `SeedAdministratorUser` lanza una excepción en el arranque si `AdminUser:Password` no está configurado, y `.github/scripts/smoke-test.sh` no lo pasaba al contenedor bajo prueba.

**Solución:** el smoke test genera un valor de usar y tirar y lo pasa como `AdminUser__Password` a los contenedores que arranca.

### El webhook de Portainer se marca como fallido aunque el redeploy funcionó

**Causa:** el webhook de Portainer responde `204 No Content` en un disparo correcto, no `200`. El step que llamaba al webhook solo aceptaba `200` como éxito.

**Solución:** aceptar cualquier código `2xx` como éxito.

### Docker secrets de fichero (`secrets: <nombre>: file: ...`) no llegan a la app en Portainer

**Causa (no resuelta):** se intentó migrar los 5 secretos de aplicación a Docker Compose "secrets" de fichero (que la app ya sabe leer vía `AddDockerSecrets()`/`KeyPerFile`), pero contra este Portainer concreto (Business Edition, motor de compose vendorizado) el mount y el contenido del fichero eran correctos (confirmado con `docker inspect`/`docker exec`), y aun así la Api seguía arrancando con el connection string de fallback de `appsettings.json`, como si `AddDockerSecrets()` no incorporara `/run/secrets` a la configuración final.

**Solución (workaround actual):** los 5 secretos se pasan como variables de entorno normales del stack, no como Docker secrets. El detalle completo de qué se probó está en el ["Known issue" de `DEPLOYMENT_RUNBOOK.md`](../../infrastructure/deploy/DEPLOYMENT_RUNBOOK.md#known-issue-docker-secrets-file-based-secrets-nombre-file--no-llegan-a-la-app-en-este-portainer). Pendiente de retomar — ver [`docs/technical/seguimiento-pendientes-cicd-homelab.md`](../technical/seguimiento-pendientes-cicd-homelab.md).

## Referencias

- [`infrastructure/deploy/DEPLOYMENT_RUNBOOK.md`](../../infrastructure/deploy/DEPLOYMENT_RUNBOOK.md) — procedimiento de referencia (camino feliz, rollback, fallbacks manuales).
- [`CHANGELOG.md`](../../CHANGELOG.md) — historial real de versiones publicadas (Keep a Changelog / SemVer).
- [`.github/workflows/release.yml`](../../.github/workflows/release.yml) — publica la GitHub Release al empujar el tag `vX.Y.Z`.
- `.claude/docs/sdlc/design/issue-45-despliegue-automatizado-al-homelab.md` — diseño original de la issue #45 (incluye el Apéndice A con los pasos exactos para generar cada secreto/token).
- `docs/technical/issue-44-validacion-imagenes-docker-pipeline-cd.md` — diseño del job `validate` (Trivy, smoke test, baseline de tamaño).
- `docs/technical/issue-99-versionado-real-imagenes-y-releases.md` — versionado de imágenes y GitHub Releases (proceso `release: vX.Y.Z`).
