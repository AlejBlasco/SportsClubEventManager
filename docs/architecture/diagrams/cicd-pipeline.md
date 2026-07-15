# CI/CD — Diagrama de flujo del pipeline

Parte del catálogo de diagramas de la issue [#51](https://github.com/AlejBlasco/SportsClubEventManager/issues/51). Ver el índice completo en [`README.md`](README.md).

**Movido** desde `docs/technical/issue-45-despliegue-automatizado-al-homelab.md` (única fuente de verdad; ese documento ahora enlaza aquí en vez de tener una copia propia, para no arriesgar que las dos versiones diverjan). Cubre el pipeline completo: `push`/`workflow_dispatch` → construcción/validación → despliegue → verificación post-despliegue → *rollback* automático si falla.

```mermaid
flowchart TD
    Push["push(master) | workflow_dispatch"] --> Validate["Job validate (issue #44)"]
    Validate --> BuildPush["Job build-and-push\ntags: latest, sha-&lt;sha&gt;, &lt;version&gt;"]
    BuildPush --> Deploy["Job deploy\nwebhook GitOps de Portainer"]

    Deploy -->|needs: deploy| Smoke["Job post-deploy-smoke-test\nenvironment: homelab-production"]

    subgraph Smoke_detail["post-deploy-smoke-test"]
        direction TB
        S1["smoke-test.sh\nGET /health/live (bloqueante)"]
        S2["smoke-test.sh\nGET /health/ready (bloqueante)"]
        S1 --> S2
    end
    Smoke -.-> Smoke_detail

    Smoke -->|éxito| Tag["Job tag-deployed-version\nneeds: post-deploy-smoke-test, if: success()"]
    Tag --> GitTag["git tag deployed/homelab/&lt;sha-corto&gt;\ngit push origin &lt;tag&gt;"]
    GitTag --> TagRelease["Job tag-release-version\nneeds: tag-deployed-version, if: success()"]

    subgraph TagRelease_detail["tag-release-version (issue #99, 2026-07-15)"]
        direction TB
        TR1{"git rev-parse vX.Y.Z\n¿ya existe?"}
        TR1 -->|Sí| TR2["No hace nada"]
        TR1 -->|No| TR3["extract-changelog-section.sh\nX.Y.Z CHANGELOG.md"]
        TR3 -->|"con contenido"| TR4["git tag vX.Y.Z\ngit push origin vX.Y.Z"]
        TR3 -->|"ausente o vacía"| TR5["::warning::\n(no falla el job)"]
    end
    TagRelease -.-> TagRelease_detail
    TR4 -.dispara.-> ReleaseYml["release.yml\n(workflow independiente)"]

    Smoke -->|fallo| Report["find-last-good-tag.sh\n+ ::error:: en $GITHUB_STEP_SUMMARY\nexit 1"]
    Report -.sugiere.-> RollbackTrigger["gh workflow run rollback.yml -f version=&lt;hash&gt;"]

    RollbackTrigger --> RB1["Job validate-version\ngit ls-remote tag deployed/homelab/&lt;version&gt;"]
    RB1 -->|needs| RB2["Job portainer-rollback\nenvironment: homelab-production\nportainer-rollback.sh"]
    RB2 -->|needs| RB3["Job post-rollback-smoke-test\nreutiliza smoke-test.sh"]

    RB2 -.PUT /api/stacks/id\nAPP_VERSION=sha-&lt;hash&gt;\npullImage=true.-> Portainer[("Portainer API\nBusiness Edition")]
```

## Puntos clave

- **`deploy` no verificaba nada antes de esta issue** (#45): llamaba al webhook de Portainer y el pipeline terminaba ahí. `post-deploy-smoke-test` y `tag-deployed-version` son los dos jobs que añaden verificación real y trazabilidad de qué versión está desplegada en cada momento.
- **El tag `deployed/homelab/<sha-corto>` es la fuente de verdad** de qué se desplegó con éxito por última vez — `rollback.yml` lo usa para validar que la versión a la que se quiere volver existió de verdad.
- **`tag-release-version` (issue #99, 2026-07-15) cierra el último paso manual del release**: si `Directory.Build.props` trae una versión sin tag `vX.Y.Z` todavía y `CHANGELOG.md` ya la documenta (lo normal, si se siguió el runbook de release), crea y empuja ese tag automáticamente — disparando `release.yml` sin intervención humana. Si `CHANGELOG.md` no está listo, solo deja un `::warning::` y sigue sin crear el tag; nunca hace fallar el job, porque el despliegue al homelab (jobs anteriores) ya tuvo éxito en ese punto.
- **El rollback no reconstruye ninguna imagen**: `portainer-rollback.sh` solo actualiza `APP_VERSION` en el stack de Portainer vía su API (`pullImage=true` para asegurar que se tira de la imagen ya publicada en GHCR con ese tag), y vuelve a correr el mismo smoke test.
- Procedimiento operativo completo (comandos exactos, troubleshooting) en [`docs/deployment/homelab-deployment.md`](../../deployment/homelab-deployment.md), única fuente de verdad (fusionado con el antiguo `infrastructure/deploy/DEPLOYMENT_RUNBOOK.md` el 2026-07-15).
