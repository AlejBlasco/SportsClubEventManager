# Despliegue automático al homelab

Guía operativa completa — única fuente de verdad — para configurar, ejecutar, verificar y revertir el despliegue continuo de **SportsClubEventManager** al homelab. Antes vivía repartida entre este documento y `infrastructure/deploy/DEPLOYMENT_RUNBOOK.md`; se fusionaron en uno solo (2026-07-15) para eliminar la duplicación entre ambos — `infrastructure/deploy/` conserva únicamente los scripts (`smoke-test.sh`, `find-last-good-tag.sh`, `portainer-rollback.sh`) y un índice mínimo que apunta aquí.

El despliegue se apoya en **Docker Compose + Portainer** sobre un homelab personal (no hay Kubernetes), accesible **exclusivamente por Tailscale** — no hay ninguna ruta pública a Portainer. Existen tres flujos: la **configuración inicial** (solo la primera vez que se conecta un homelab), **hacer un despliegue** (cada vez que se publica una nueva versión) y **rollback** (si algo sale mal). Sigue el que corresponda.

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

1. **`validate`** — reconstruye y escanea (Trivy) las imágenes `api`/`web`, y las somete a un smoke test local. Si falla, el pipeline se detiene aquí — no se publica ni despliega nada.
2. **`build-and-push`** — publica ambas imágenes en GHCR con las etiquetas `latest`, `sha-<hash-corto>` y `X.Y.Z`.
3. **`deploy`** — llama al **webhook de GitOps de Portainer** (`secrets.PORTAINER_WEBHOOK_URL`), que hace que Portainer vuelva a hacer `pull` de las imágenes (`pull_policy: always`) y recree los contenedores `api`/`web`.
4. **`post-deploy-smoke-test`** — espera a que el despliegue esté realmente sano contra la URL pública real (`secrets.HOMELAB_WEB_URL`, entorno `homelab-production`): `GET /health/live` y `GET /health/ready` (bloqueantes, hasta 6 intentos cada 15s, ~90s máx. cada uno — `/health/ready` valida `Api` de forma transitiva, ver `ApiAvailabilityHealthCheck`, issue #41). Si cualquiera de los dos falla, el job ejecuta `find-last-good-tag.sh`, escribe el tag `sha-*` correcto anterior y las instrucciones de rollback en el resumen del job (`$GITHUB_STEP_SUMMARY`) con `::error::`, y falla — lo que marca el `Deployment` de `homelab-production` como `failure`, visible en la pestaña **Environments** del repositorio.
5. **`tag-deployed-version`** — si el smoke test pasa, crea y empuja el tag ligero `deployed/homelab/<sha-corto>` sobre el commit desplegado, usando el `GITHUB_TOKEN` de la propia ejecución. Este tag es la fuente de verdad de "qué se ha desplegado con éxito y cuándo", y es lo que consume el rollback automático como valores válidos de `version`.
6. **`tag-release-version`** — si `Directory.Build.props` tiene una versión que todavía no tiene su tag `vX.Y.Z` (el caso normal tras seguir el Paso 2), crea y empuja ese tag automáticamente, lo que dispara `release.yml` sin ninguna acción manual — ver [Paso 6](#paso-6-automático—el-tag-semver-y-la-github-release) más abajo.

En ningún punto de este flujo se necesita abrir la UI de Portainer ni ejecutar nada a mano — es el comportamiento esperado una vez cargados los secretos/variables del [Paso 1](#paso-1--crear-el-github-environment-homelab-production).

Sigue el progreso con:

```powershell
gh run watch --exit-status
```

Si `post-deploy-smoke-test` falla, el propio job deja en el resumen del run el comando exacto de rollback (ver [Rollback](#rollback)).

### Paso 6 (automático) — el tag SemVer y la GitHub Release

Con el Paso 2 ya hecho (bump de `<Version>` + cierre de `## [Unreleased]` a `## [X.Y.Z] - fecha` en `CHANGELOG.md`, dentro de la propia PR de release), el job `tag-release-version` del Paso 5 crea y empuja el tag `vX.Y.Z` automáticamente en cuanto el despliegue al homelab termina con éxito — no hace falta ningún comando manual en el caso normal.

Eso dispara `release.yml`, que valida que `Directory.Build.props` coincide con el tag, extrae la sección `## [X.Y.Z]` de `CHANGELOG.md` y publica la GitHub Release con esas notas — no reconstruye ni redespliega nada (ya se hizo en el Paso 5).

```powershell
gh release view vX.Y.Z
```

**Solo hace falta un tag manual** si `tag-release-version` avisó (`::warning::` en el resumen del run, sin hacer fallar el job — el despliegue en sí ya tuvo éxito) de que `CHANGELOG.md` todavía no documenta la versión — por ejemplo, si `<Version>` se subió a `master` en una PR distinta a la del cierre del CHANGELOG. En ese caso, completa `CHANGELOG.md` y crea el tag a mano:

```powershell
git checkout master
git pull
git tag vX.Y.Z
git push origin vX.Y.Z
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

### Rollback automático (`rollback.yml`)

Si un despliegue queda en mal estado (o simplemente se quiere volver a una versión anterior), el rollback es automático, sin tocar la UI de Portainer:

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
3. **`post-rollback-smoke-test`**: reutiliza `infrastructure/deploy/smoke-test.sh` (los mismos checks de `/health/live` y `/health/ready` que el despliegue normal). Si falla, el job falla con una alerta explícita — **no hay reintento automático adicional ni "rollback del rollback"**; en ese caso hay que seguir el [procedimiento manual paso a paso](#fallback-manual-rollback-paso-a-paso-en-portainer) de más abajo.

### Fallback manual: "Pull and redeploy" desde la UI de Portainer

Si el **webhook** de Portainer no está disponible (secreto no cargado, API/host inalcanzable, etc.), el despliegue normal puede hacerse a mano:

1. Entrar en Portainer → seleccionar el **Environment** del nodo del homelab.
2. Ir a **Stacks** → abrir el stack de producción (por defecto `sportsclubeventmanager-prod`; si el nombre real difiere, `rollback.yml` lo lee de la variable `PORTAINER_STACK_NAME` — ver [Paso 1](#paso-1--crear-el-github-environment-homelab-production)).
3. Pulsar **"Pull and redeploy"** (o **"Update the stack"** con la opción **"Re-pull image"** activada, según la versión de Portainer). Esto vuelve a hacer `pull` de la imagen `:${APP_VERSION:-latest}` configurada actualmente y recrea los contenedores `api`/`web`.
4. Verificar manualmente `GET https://<HOMELAB_WEB_URL>/health/live` y `GET https://<HOMELAB_WEB_URL>/health/ready` para confirmar que el despliegue quedó sano (los mismos checks que hace `smoke-test.sh`).

### Fallback manual: rollback paso a paso en Portainer

Si la **API** de Portainer no está disponible (p. ej. porque no es alcanzable desde runners de GitHub-hosted), el rollback puede hacerse a mano con el mismo resultado final que `rollback.yml`:

1. Elegir a qué versión volver a partir de los tags `deployed/homelab/*` (ver comando `git tag -l` de más arriba), o ejecutando `infrastructure/deploy/find-last-good-tag.sh <sha-actual>` para obtener automáticamente el último tag correcto anterior.
2. En Portainer → **Stacks** → abrir el stack de producción → sección de **variables de entorno del stack**.
3. Fijar (o editar) la variable `APP_VERSION` al valor `sha-<hash-corto>` elegido en el paso 1.
4. Pulsar **"Update the stack"** con la opción **"Re-pull image"** activada — la imagen con ese tag ya existe en GHCR, así que no hace falta reconstruir nada.
5. Verificar manualmente `/health/live` y `/health/ready` como en el punto 4 del fallback anterior.
6. Una vez confirmado que el rollback fue efectivo, documentar el incidente y, si procede, dejar `APP_VERSION` fijada a ese valor hasta que se publique una corrección — el siguiente `push` a `master` que pase el smoke test volverá a mover el despliegue hacia adelante de forma automática.

## Troubleshooting

### El pipeline de CD solo corría en `develop`, nunca en `master`

**Causa:** `cd.yml` se dispara con `push` a `master`. Si todo el trabajo de CI/CD se desarrolló y probó en `develop` sin haber completado nunca un merge real a `master`, el pipeline nunca llegó a ejecutarse de verdad contra el homelab.

**Solución:** completar el primer merge real de `develop` a `master` (o lanzar `cd.yml` manualmente con `workflow_dispatch`) para validar el flujo de punta a punta al menos una vez.

### `validate (api)` / `validate (web)` se quedan en "Expected — Waiting for status to be reported" para siempre

**Causa:** el job `validate` usa `strategy.matrix.include` con tres claves (`service`, `dockerfile`, `image`). Sin un `name:` explícito en el job, GitHub Actions nombra el check con **todas** las claves del matrix, no solo `service` — el check real se reporta como `validate (api, docker/Dockerfile.api, ghcr.io/...)`, que nunca coincide con el string exacto `validate (api)` que exige `branch-protection.yml` (comprobable con `gh api repos/<owner>/<repo>/branches/master/protection/required_status_checks`). El check requerido queda pendiente indefinidamente aunque el job real termine en verde.

**Solución:** añadir `name: validate (${{ matrix.service }})` al job en `cd.yml`, forzando el nombre del check a depender solo de `matrix.service`.

### `tag-release-version` no crea el tag `vX.Y.Z` y solo deja un `::warning::`

**Causa:** `<Version>` en `Directory.Build.props` cambió (respecto al último tag `vX.Y.Z` existente), pero `CHANGELOG.md` todavía no tiene una sección `## [X.Y.Z]` con contenido real — típicamente porque el bump de versión llegó a `master` en una PR distinta a la que cierra `## [Unreleased]`, rompiendo el orden esperado del Paso 2. El job no falla (el despliegue al homelab ya tuvo éxito y no debe verse como roto), solo omite la creación del tag.

**Solución:** completar `CHANGELOG.md` (mover `## [Unreleased]` a `## [X.Y.Z] - fecha` con contenido real) y crear el tag manualmente — ver el bloque de comandos al final del [Paso 6](#paso-6-automático—el-tag-semver-y-la-github-release).

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

**Causa (no resuelta):** se intentó migrar los 5 secretos de aplicación a Docker Compose "secrets" file-based (que la app ya sabe leer vía `AddDockerSecrets()`/`KeyPerFile` en `/run/secrets`, ver [`docs/technical/issue-38-secrets-management.md`](../technical/issue-38-secrets-management.md)), pero **no funcionó en la práctica contra este Portainer** y se revirtió a variables de entorno planas para desbloquear el despliegue:

- `secrets: <nombre>: environment: VAR` (secret alimentado desde una variable de entorno) se comprobó que **no se monta en absoluto**: el motor de compose embebido en Portainer (Business Edition 2.39.2, `docker/cli` vendorizado, distinto del `docker compose` del sistema) acepta la sintaxis sin error pero `docker inspect` mostraba `.Mounts: []` en el contenedor `api`.
- `secrets: <nombre>: file: <ruta>` con una ruta solo visible dentro del contenedor `portainer` (p. ej. `/data/...`) falla con un error explícito del daemon (`invalid mount config for type "bind": bind source path does not exist`), porque los bind mounts los resuelve el daemon de Docker contra el filesystem del **host real**, no contra la vista del contenedor `portainer`.
- `secrets: <nombre>: file: <ruta-real-del-host>` (p. ej. `/home/adminlab/sportsclub-secrets/...`) sí monta correctamente — confirmado con `docker inspect <contenedor> --format '{{json .Mounts}}'` mostrando los 5 binds, y confirmado leyendo el contenido del fichero desde dentro del contenedor (`docker exec ... cat /run/secrets/...`), con el valor correcto. **Aun así, la Api seguía arrancando con el connection string de fallback de `appsettings.json` (LocalDB)**, como si `AddDockerSecrets()` no estuviera incorporando `/run/secrets` a la configuración final. No se llegó a la causa raíz (requeriría depurar el proceso .NET en marcha, no solo el filesystem del contenedor).

**Solución (workaround actual):** los 5 secretos se pasan como variables de entorno normales del stack, no como Docker secrets. Pendiente de retomar en una tarea aparte, sin bloquear despliegues mientras tanto: por qué `KeyPerFile` no sobrescribe `appsettings.json` en este entorno concreto, a pesar de que el mount y el contenido del fichero son correctos y hay tests unitarios (`SecretsConfigurationExtensionsTests.cs`) que cubren la lógica de mapeo de claves.

### Editar `stack.env`/`docker-compose.yml` directamente en el volumen de Portainer no es duradero

**Causa:** Portainer guarda la configuración real de un stack (incluidas sus variables de entorno) en su propia base de datos interna (`portainer.db`, BoltDB, dentro del volumen `adminlab_portainer_data`), no en los ficheros planos `docker-compose.yml`/`stack.env` que también deja en `/data/compose/<id>/v2/` dentro de ese mismo volumen. Esos ficheros son solo una proyección: **Portainer los regenera a partir de `portainer.db` cada vez que él mismo ejecuta un deploy** (incluido el redeploy disparado por el webhook que usa `deploy` en `cd.yml`). Editar `stack.env`/`docker-compose.yml` a mano (por ejemplo con un contenedor temporal montando el volumen, sin necesitar sudo en el host) aplica el cambio si además se ejecuta `docker compose up -d` manualmente contra esos ficheros — pero en cuanto Portainer vuelve a desplegar el stack por su cuenta, sobrescribe esos ficheros con el contenido de `portainer.db`, revirtiendo el cambio sin ningún aviso.

**Solución:** cualquier cambio en las variables de entorno o en el compose de un stack de Portainer tiene que aplicarse a través del propio Portainer — UI (**Stacks → \<stack\> → Editor**, tanto el YAML como la pestaña de **Environment variables**) o su API (`PUT /api/stacks/{id}`) — nunca editando los ficheros del volumen directamente. Verificar que quedó aplicado de verdad comprobando la fecha de modificación de `portainer.db` (no solo la de `stack.env`).

### Un subdominio de dos niveles (`api.sportsclub.ablasco.com`) da "TLS alert, unrecognized name" al exponerlo por Cloudflare Tunnel

**Causa:** el certificado "Universal SSL" que Cloudflare emite automáticamente para una zona solo cubre el dominio raíz y un nivel de wildcard (`ablasco.com` + `*.ablasco.com`). Un hostname con dos niveles de subdominio (`api.sportsclub.ablasco.com`) no encaja en ese wildcard — Cloudflare necesita el add-on de pago **Advanced Certificate Manager** para cubrirlo, y sin él el borde de Cloudflare rechaza el handshake TLS del cliente antes incluso de llegar al origen (mismo síntoma, por cierto, que sufre Let's Encrypt/npm con un wildcard de un solo nivel si se intenta cubrir el mismo hostname ahí).

**Solución:** usar un hostname de un solo nivel bajo el dominio raíz (p. ej. `sportsclub-api.ablasco.com` en vez de `api.sportsclub.ablasco.com`), igual que el patrón ya usado por `sportsclub.ablasco.com`.

### Exponer un nuevo servicio por Cloudflare Tunnel apuntando a npm en vez de al contenedor da un bucle de redirección

**Causa:** para los hostnames públicos de esta app, el **Service URL** del túnel de Cloudflare (Zero Trust → Networks → Tunnels → Public Hostname) apunta **directamente al puerto del contenedor** (p. ej. `http://192.168.1.100:5123` para `web`) — npm (nginx-proxy-manager) no está en ese camino. npm solo sirve para acceso por LAN/Tailscale a través de su propio puerto 443 con su propio certificado Let's Encrypt. Si el Service URL de una entrada nueva del túnel apunta en cambio al puerto 80 de npm, la petición sí llega a npm, pero por HTTP plano y sin las cabeceras que npm necesita para no forzar su propio redirect a HTTPS (`force-ssl.conf`) — el resultado es un `301` a la misma URL una y otra vez.

**Solución:** el Service URL de cada entrada del túnel debe apuntar directo al puerto del contenedor correspondiente (`http://192.168.1.100:<puerto-del-servicio>`), nunca a npm.

### Un cambio en `docker-compose.prod.yml` del repo no llega solo a Portainer, aunque se despliegue

**Causa:** el webhook de Portainer que dispara `deploy` en `cd.yml` solo hace `pull` de la imagen nueva y recrea los contenedores — **nunca sincroniza el texto del `docker-compose.yml` que Portainer tiene guardado** con el `infrastructure/docker-compose/docker-compose.prod.yml` del repositorio. Son dos copias completamente independientes: el código de la imagen se actualiza solo con cada deploy, pero cualquier línea nueva de `environment:` (una variable de entorno nueva, por ejemplo) solo llega al contenedor en marcha si alguien la pega también a mano en Portainer (**Stacks → \<stack\> → Editor**). Asumir que "ya vendrá con el PR" deja el contenedor corriendo con el código nuevo pero sin la variable que ese código espera — mismo síntoma que si la variable no existiera, pero más difícil de detectar porque el deploy en sí sale verde.

**Solución:** cuando un PR añade o renombra una variable de entorno consumida por `api`/`web`, la línea correspondiente del compose tiene que pegarse en Portainer **por separado**, en el mismo momento (antes o después, da igual) que se hace el deploy del código — nunca asumir que llega sola. Verificar siempre con `docker exec <contenedor> printenv` tras el deploy, no solo que el contenedor esté `healthy`.

## Referencias

- [`infrastructure/deploy/README.md`](../../infrastructure/deploy/README.md) — índice de los scripts (`smoke-test.sh`, `find-last-good-tag.sh`, `portainer-rollback.sh`) que este documento describe en uso.
- [`CHANGELOG.md`](../../CHANGELOG.md) — historial real de versiones publicadas (Keep a Changelog / SemVer).
- [`.github/workflows/release.yml`](../../.github/workflows/release.yml) — publica la GitHub Release al empujar el tag `vX.Y.Z`.
- `.claude/docs/sdlc/design/issue-45-despliegue-automatizado-al-homelab.md` — diseño original de la issue #45 (incluye el Apéndice A con los pasos exactos para generar cada secreto/token).
- `docs/technical/issue-44-validacion-imagenes-docker-pipeline-cd.md` — diseño del job `validate` (Trivy, smoke test, baseline de tamaño).
- `docs/technical/issue-99-versionado-real-imagenes-y-releases.md` — versionado de imágenes y GitHub Releases (proceso `release: vX.Y.Z`).
