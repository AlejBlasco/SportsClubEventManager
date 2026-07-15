# Despliegue automatizado al homelab

> Referencia de diseño: [`issue-45-despliegue-automatizado-al-homelab.md`](../../.claude/docs/sdlc/design/issue-45-despliegue-automatizado-al-homelab.md).

Esta carpeta contiene los **scripts** de la automatización de despliegue y rollback al homelab (issue #45, ya implementada y en producción — no es un placeholder). El procedimiento operativo completo — flujo automático, rollback, *fallbacks* manuales y troubleshooting — vive en [`docs/deployment/homelab-deployment.md`](../../docs/deployment/homelab-deployment.md), única fuente de verdad; esta carpeta no tiene su propia copia de esa documentación.

## Contenido

| Fichero | Uso |
|---|---|
| `smoke-test.sh` | Sondea `/health/live` y `/health/ready` contra la URL pública del homelab tras un despliegue o un rollback, para confirmar que la aplicación desplegada está realmente sana (no solo que la llamada a Portainer devolvió 200). Usado por `.github/workflows/cd.yml` (job `post-deploy-smoke-test`) y por `rollback.yml` (job `post-rollback-smoke-test`). |
| `find-last-good-tag.sh` | Busca, vía la API de *Deployments* de GitHub, el último tag `sha-<hash>` desplegado con éxito en el entorno `homelab-production` antes de un commit dado. Usado para calcular y publicar la guía de rollback cuando falla el smoke test post-despliegue. |
| `portainer-rollback.sh` | Ejecuta el rollback llamando directamente a la API de Portainer (`PUT /api/stacks/{id}`): fija `APP_VERSION` al tag solicitado y fuerza un re-pull de la imagen ya existente en GHCR, sin reconstruir nada. Usado por `rollback.yml`. |

## Relación con `infrastructure/docker-compose/`

Estos scripts operan sobre el stack de producción desplegado en Portainer a partir de [`../docker-compose/docker-compose.prod.yml`](../docker-compose/docker-compose.prod.yml) — no lo reemplazan ni lo duplican. `docker-compose.prod.yml` define *qué* se despliega (servicios, imágenes, secretos, red); esta carpeta define *cómo* se dispara, verifica y revierte ese despliegue de forma automática desde CI/CD.
