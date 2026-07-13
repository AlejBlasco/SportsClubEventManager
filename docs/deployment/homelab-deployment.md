# Despliegue automático al homelab

> Referencia operativa de cómo funciona y cómo se configura el despliegue continuo de
> `SportsClubEventManager` al homelab. Para el procedimiento de referencia día a día (camino feliz +
> rollback + fallbacks manuales) ver también
> [`infrastructure/deploy/DEPLOYMENT_RUNBOOK.md`](../../infrastructure/deploy/DEPLOYMENT_RUNBOOK.md);
> este documento es más extenso e incluye además cómo configurar todo desde cero y el histórico de
> problemas reales encontrados al ponerlo en marcha.

## 1. Arquitectura del despliegue

`SportsClubEventManager` se despliega mediante **Docker Compose + Portainer** sobre un homelab
personal — no hay Kubernetes. El acceso al homelab es **exclusivamente por Tailscale** (VPN mesh):
no existe ninguna ruta pública a Portainer ni, salvo que se configure explícitamente, a la propia
aplicación.

El pipeline se apoya en tres piezas versionadas en este repositorio:

| Pieza | Función |
|---|---|
| `.github/workflows/cd.yml` | Se dispara con cada `push` a `master` (y en cada PR contra `master`, aunque en ese caso solo corre la fase `validate`). Construye y valida las imágenes Docker (`api`, `web`), las publica en GHCR, dispara el redeploy en Portainer vía webhook, y comprueba que el despliegue queda sano antes de darlo por bueno. |
| `.github/workflows/rollback.yml` | Permite volver a una versión anterior ya desplegada con éxito, sin tocar la UI de Portainer. |
| `infrastructure/docker-compose/docker-compose.prod.yml` | Define el stack real que corre en Portainer (`sqlserver`, `api`, `web`). |

Como los runners de GitHub Actions (máquinas efímeras en la nube de GitHub) **no están dentro de la
red Tailscale del homelab** por defecto, todos los jobs que necesitan hablar con Portainer o con
`/health/*` se unen a la tailnet temporalmente, solo durante ese job, con la acción
`tailscale/github-action`.

## 2. Configuración inicial (solo la primera vez / al añadir un homelab nuevo)

### 2.1 GitHub Environment `homelab-production`

Los workflows leen los secretos/variables de un **Environment** de GitHub llamado
`homelab-production` (repo → **Settings → Environments**).

Secretos necesarios en ese Environment:

| Nombre | Qué es | De dónde sale |
|---|---|---|
| `PORTAINER_WEBHOOK_URL` | URL de un solo uso que dispara el redeploy del stack | Portainer → Stack → **Webhooks** (activar, con **"Re-pull image"**) |
| `PORTAINER_API_URL` | URL base de la instancia de Portainer (sin `/api` al final) | La tuya |
| `PORTAINER_API_KEY` | Token de la API de Portainer, usado por el rollback automático | Portainer → **My account → Access tokens** |
| `HOMELAB_WEB_URL` | URL (Tailscale) donde responde el servicio `web` | La tuya |
| `TS_AUTHKEY` | Auth key de Tailscale para que los runners de GitHub se unan a la tailnet (ver 2.2) | Tailscale admin console |

Variable (no secreta), a nivel de repositorio o del entorno `homelab-production`:

| Nombre | Qué es |
|---|---|
| `PORTAINER_STACK_NAME` | Nombre real del stack en Portainer. El valor por defecto que asumen los scripts si no se define es `sportsclubeventmanager-prod`; en el homelab actual el stack se llama **`sportsclub`**, así que esta variable es obligatoria en la práctica. |

`HOMELAB_WEB_URL` puede definirse como **variable** en vez de secreto si se quiere que aparezca como
enlace clicable en la pestaña **Environments** del repositorio (`cd.yml` la lee con
`vars.HOMELAB_WEB_URL || secrets.HOMELAB_WEB_URL`, así que funciona en cualquiera de las dos formas).

### 2.2 Acceso desde GitHub Actions a la tailnet (Tailscale)

1. En el admin console de Tailscale: **Settings → Keys → Generate auth key**.
   - Marcar **Reusable** (se usa en múltiples ejecuciones) y **Ephemeral** (el nodo se borra solo al
     desconectar el runner).
   - Etiqueta (`tag`): `tag:ci` (crearlo antes en **Access Controls** si no existe).
   - Expiración: la máxima permitida (90 días) — **hay que rotarla antes de que caduque**, o los 4
     jobs que dependen de Tailscale (`deploy`, `post-deploy-smoke-test` en `cd.yml`;
     `portainer-rollback`, `post-rollback-smoke-test` en `rollback.yml`) empezarán a fallar en el
     step "Connect to Tailscale".
   - Nota: `tailscale/github-action` marca el input `authkey` como deprecado a favor de un OAuth
     client (`https://tailscale.com/s/oauth-clients`). Sigue funcionando con auth key, pero es la
     vía recomendada a futuro si se quiere evitar la rotación manual cada 90 días.
2. Copiar la key generada y guardarla como el secret `TS_AUTHKEY` del Environment
   `homelab-production`.
3. Confirmar que la política de ACL de la tailnet permite que `tag:ci` alcance el host de Portainer
   y el de `web` — con la política por defecto (`{"src": ["*"], "dst": ["*"], "ip": ["*"]}`, permitir
   todo) no hace falta tocar nada más.

Con esto, cada job relevante incluye un step:

```yaml
- name: Connect to Tailscale
  uses: tailscale/github-action@v3
  with:
    authkey: ${{ secrets.TS_AUTHKEY }}
    tags: tag:ci
```

### 2.3 El stack en Portainer

El stack de producción debe existir en Portainer, apuntando al contenido de
`infrastructure/docker-compose/docker-compose.prod.yml`, con:

- Las variables de entorno del stack cargadas: `SA_PASSWORD`, `CONNECTION_STRING`,
  `JWT_SECRET_KEY`, `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `ADMIN_PASSWORD`, `API_PORT`,
  `WEB_PORT`, `ASPNETCORE_ENVIRONMENT`, `WEB_EXTERNAL_URL`. Estos 5 valores sensibles se pasan como
  **variables de entorno normales del stack**, no como Docker secrets — ver sección 6 para por qué.
- El **webhook de GitOps** activado con la opción **"Re-pull image"** — si no, el redeploy reinicia
  los contenedores pero puede no traer una imagen nueva con el mismo tag `latest`.
- `APP_VERSION` **sin** un valor fijo forzado, para que `${APP_VERSION:-latest}` decida (el
  `docker-compose.prod.yml` usa `pull_policy: always` en `api`/`web`, así que siempre vuelve a
  comprobar si hay una imagen más nueva para el tag configurado).

### 2.4 Protección de rama y aprobación de PRs

`master` exige (vía `.github/workflows/branch-protection.yml`): 1 aprobación de PR, checks de
estado en verde (`build-and-test`, `validate (api)`, `validate (web)`), e historial lineal (solo
*squash*/*rebase* merge, nunca *merge commit*). Si el repositorio tiene un único colaborador (el
propio propietario), **GitHub nunca cuenta la propia aprobación del autor de la PR**, así que con
`enforce_admins: true` nadie podría mergear nunca. La solución usada aquí: `enforce_admins: false`
en `branch-protection.yml`, que permite a un administrador usar el botón de GitHub **"Merge without
waiting for requirements to be met"** sin desactivar el resto de reglas para otros colaboradores.
Revisar esto (volver a `true`) en cuanto haya un segundo colaborador que pueda aprobar PRs.

## 3. Cómo funciona un despliegue normal (camino feliz)

1. Se mergea una PR (`squash` o `rebase`) contra `master`, o se lanza `cd.yml` manualmente
   (`workflow_dispatch`).
2. **`validate`** (matriz `api`/`web`, corre también en cada PR contra `master`):
   1. Construye la imagen localmente (`docker/build-push-action`, `push: false, load: true`).
   2. La escanea con **Trivy** dos veces: una en formato SARIF (todas las severidades, solo
      vulnerabilidades — sin escaneo de secretos, que ya cubre `gitleaks` en `ci.yml`) que se sube a
      la pestaña **Security** del repositorio; y otra en formato tabla, cuyo recuento de
      vulnerabilidades `CRITICAL` decide si el job falla (step "Fail on CRITICAL vulnerabilities").
   3. Compara el tamaño de la imagen contra `docker/image-size-baseline.json` (solo avisa, no
      bloquea).
   4. Ejecuta un **smoke test** (`.github/scripts/smoke-test.sh`) que levanta un SQL Server efímero
      y arranca el contenedor real contra él, con configuración de usar y tirar, para comprobar que
      la app arranca y `GET /health/live` responde `200`.
   - Si `validate` falla, el pipeline se detiene aquí: no se publica ni se despliega nada.
3. **`build-and-push`** (solo si el evento es `push`, no `pull_request`): publica `api` y `web` en
   GHCR con las etiquetas `latest`, `sha-<hash-corto>` y la versión de `Directory.Build.props`.
4. **`deploy`**: se conecta a la tailnet y llama al webhook de Portainer (`POST`). Portainer
   responde `204 No Content` en un disparo correcto (el pipeline acepta cualquier `2xx`, no solo
   `200`) y vuelve a hacer `pull` de las imágenes y recrea los contenedores `api`/`web`.
5. **`post-deploy-smoke-test`**: se conecta a la tailnet y comprueba, contra la URL real
   (`HOMELAB_WEB_URL`), `GET /health/live` y `GET /health/ready` (con reintentos, ~90s máx. cada
   uno). A diferencia del smoke test de `validate`, aquí ambos checks son **bloqueantes** — no hay
   arranque en frío de base de datos que justifique tratar `/health/ready` como informativo. Si
   falla, el job calcula el último tag bueno conocido (`find-last-good-tag.sh`) y publica en el
   resumen del job las instrucciones exactas de rollback.
6. **`tag-deployed-version`**: si todo fue bien, crea y empuja el tag ligero
   `deployed/homelab/<sha-corto>` sobre el commit desplegado. Este tag es la fuente de verdad de
   "qué se ha desplegado con éxito y cuándo", y es lo que consume `rollback.yml` como valores
   válidos de `version`.

En ningún punto de este flujo hace falta abrir la UI de Portainer ni ejecutar nada a mano.

## 4. Verificación manual (si se quiere comprobar sin esperar al pipeline)

```bash
# Desde una máquina conectada a la tailnet:
curl -s -o /dev/null -w '%{http_code}\n' https://<HOMELAB_WEB_URL>/health/live
curl -s -o /dev/null -w '%{http_code}\n' https://<HOMELAB_WEB_URL>/health/ready
```

Por SSH al host del homelab, para confirmar que los contenedores corren la imagen esperada y que el
`HEALTHCHECK` de Docker apunta a `/health/live` (no a `/`, que era el síntoma de estar corriendo una
imagen vieja pre-hardening):

```bash
docker inspect --format='{{.Config.Image}} | Health: {{.State.Health.Status}}' sportsclub-api-1 sportsclub-web-1
docker inspect --format='{{json .Config.Healthcheck.Test}}' sportsclub-api-1
```

## 5. Rollback

Ver [`DEPLOYMENT_RUNBOOK.md`, sección 2](../../infrastructure/deploy/DEPLOYMENT_RUNBOOK.md#2-rollback-automático-rollbackyml)
para el procedimiento completo. Resumen:

```bash
git fetch --tags
git tag -l 'deployed/homelab/*' --sort=-creatordate   # versiones ya desplegadas con éxito
gh workflow run rollback.yml -f version=<hash-corto>  # sin el prefijo "sha-"
```

`rollback.yml` valida que el tag exista, actualiza `APP_VERSION` en el stack de Portainer vía su API
(`infrastructure/deploy/portainer-rollback.sh` — sin reconstruir nada, la imagen ya existe en GHCR)
y vuelve a comprobar `/health/live` y `/health/ready`. Si algo de esto no está disponible (p. ej. la
API de Portainer inalcanzable), el runbook documenta también el fallback manual paso a paso desde la
UI de Portainer.

## 6. Docker secrets: por qué son variables de entorno planas y no ficheros montados

Se intentó migrar los 5 secretos de aplicación a Docker Compose "secrets" de fichero (que la app ya
sabe leer vía `AddDockerSecrets()`/`KeyPerFile`, ver `docs/technical/US-38-secrets-management.md`),
pero no funcionó en la práctica contra este Portainer concreto y se revirtió a variables de entorno
planas para desbloquear el despliegue. El detalle completo de qué se probó y por qué falló cada
intento está en el **"Known issue"** de
[`DEPLOYMENT_RUNBOOK.md`](../../infrastructure/deploy/DEPLOYMENT_RUNBOOK.md#known-issue-docker-secrets-file-based-secrets-nombre-file--no-llegan-a-la-app-en-este-portainer).
Queda pendiente de retomar esa investigación — ver
[`docs/technical/seguimiento-pendientes-cicd-homelab.md`](../technical/seguimiento-pendientes-cicd-homelab.md).

## 7. Problemas reales encontrados al poner esto en marcha (y su solución)

Esta sección es un caso de estudio de la depuración real hecha contra el homelab, útil si algo
similar vuelve a pasar:

1. **El pipeline de CD solo existía en `develop`, no en `master`.** Como `cd.yml` se dispara con
   push a `master`, todo el trabajo de CI/CD estuvo sin efecto hasta completar el primer merge real.
2. **Bloqueo de auto-aprobación en la protección de rama** — ver sección 2.4.
3. **El homelab solo es accesible por Tailscale** — resuelto uniendo el runner a la tailnet solo
   durante el job, ver sección 2.2.
4. **La imagen `:latest` en GHCR estaba desactualizada** (de antes de añadir
   `/health/live`/`/health/ready`), porque nunca se había completado un `push` a `master` real.
   Portainer marcaba el contenedor como "healthy" porque el `HEALTHCHECK` de esa imagen antigua
   comprobaba `/` en vez de `/health/live` — un falso positivo que solo se detectó inspeccionando el
   contenedor por SSH, no confiando en el estado que mostraba la UI de Portainer.
5. **Crash de Trivy en el step SARIF, sin mensaje de error.** Combinar `format: sarif` +
   `severity: CRITICAL` en el step de Trivy hacía morir el proceso justo al entrar en la fase
   `[dotnet-core] Detecting vulnerabilities...`, sin imprimir ninguna tabla de resultados ni traza —
   reproducido 3 veces en CI (api y web), incluso con `debug` logging activado, pero **no
   reproducible en local** con la misma versión de Trivy y las mismas flags contra una imagen
   equivalente. Se resolvió quitando el filtro de severidad del step que genera el SARIF (pasa a ser
   solo informativo, para la pestaña Security) y añadiendo un step nuevo,
   "Fail on CRITICAL vulnerabilities", que cuenta los `CRITICAL` reales a partir del informe en
   tabla (esa combinación nunca ha fallado).
6. **El smoke test de CI no pasaba `AdminUser__Password`.** La migración `SeedAdministratorUser`
   (añadida en US-28, cinco días antes de que existiera este smoke test) lanza una excepción en el
   arranque si `AdminUser:Password` no está configurado, y `.github/scripts/smoke-test.sh` nunca lo
   pasaba al contenedor `api` bajo prueba ni al contenedor `api` auxiliar del leg de `web`. Se
   corrigió generando un valor de usar y tirar y pasándolo a ambos contenedores.
7. **El webhook de Portainer responde `204`, no `200`.** El primer despliegue real a producción
   (disparado por el primer merge a `master`) se marcó como fallido en `cd.yml` aunque el redeploy
   había funcionado de verdad — confirmado por SSH antes de aplicar el fix: los contenedores se
   habían reiniciado con las imágenes nuevas y `/health/live`/`/health/ready` respondían `200`. El
   check solo aceptaba HTTP `200` como éxito; se corrigió para aceptar cualquier `2xx`.

**Conclusión práctica**: gran parte de la dificultad de este despliegue no fue el diseño del
pipeline en sí, sino detalles de la implementación concreta de Portainer y de los runners de GitHub
Actions que solo se revelan al desplegar contra una instancia real — de ahí el valor de tener acceso
SSH directo al host para verificar con `docker inspect` y `docker logs` en vez de depender solo de lo
que reporta la UI de Portainer o el estado en verde/rojo de un check de CI.

## 8. Pendientes conocidos

Ver [`docs/technical/seguimiento-pendientes-cicd-homelab.md`](../technical/seguimiento-pendientes-cicd-homelab.md).

## 9. Referencias

- [`infrastructure/deploy/DEPLOYMENT_RUNBOOK.md`](../../infrastructure/deploy/DEPLOYMENT_RUNBOOK.md) — procedimiento de referencia (camino feliz, rollback, fallbacks manuales).
- `.claude/docs/sdlc/design/issue-45-despliegue-automatizado-al-homelab.md` — diseño original de la issue #45 (incluye el Apéndice A con los pasos exactos para generar cada secreto/token).
- `docs/technical/issue-44-validacion-imagenes-docker-pipeline-cd.md` — diseño del job `validate` (Trivy, smoke test, baseline de tamaño).
- `docs/technical/issue-99-versionado-real-imagenes-y-releases.md` — versionado de imágenes y GitHub Releases (proceso `release: vX.Y.Z`, independiente de este pipeline de despliegue).
