# 🏹 SportsClubEventManager

![Build](https://github.com/AlejBlasco/SportsClubEventManager/actions/workflows/ci.yml/badge.svg)
[![License: Academic-NC](https://img.shields.io/badge/License-Academic--NC-orange.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-10-blue?logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)
![OAuth2](https://img.shields.io/badge/Auth-OAuth2%20%2B%20JWT-4285F4?logo=google&logoColor=white)

> Trabajo de Fin de Máster — Plataforma de gestión de eventos para un club deportivo de tiro.

El **origen de este proyecto** es completamente práctico: nace de una necesidad real detectada en el club de tiro deportivo del que el autor es socio. Hasta ahora, la gestión de las inscripciones a las competiciones —cuyo calendario publica la federación en un fichero CSV— se realizaba **de forma totalmente manual**. Una semana antes de cada competición, cada socio interesado debía enviar un WhatsApp **al móvil personal del secretario del club** indicando sus datos, la competición y el horario en el que deseaba participar. El secretario revisaba uno a uno esos mensajes, introducía los datos a mano, comprobaba si quedaban plazas disponibles y respondía individualmente a cada socio, ya fuera para confirmar la inscripción o para avisar de que la competición estaba completa. Tras plantear este problema a la junta directiva del club, surgió la idea de explorar cómo se podría modernizar y digitalizar el proceso, dando origen al presente **prototipo**, desarrollado como Trabajo de Fin de Máster.

## Índice

- [a. Descripción general del proyecto](#a-descripción-general-del-proyecto)
- [b. Stack tecnológico utilizado](#b-stack-tecnológico-utilizado)
- [c. Instalación y ejecución](#c-instalación-y-ejecución)
- [d. Despliegue](#d-despliegue)
- [e. Estructura del proyecto](#e-estructura-del-proyecto)
- [f. Funcionalidades principales](#f-funcionalidades-principales)
- [g. Usuario y contraseña de prueba](#g-usuario-y-contraseña-de-prueba)
- [h. Proyectos personales empleados en su construcción](#h-proyectos-personales-empleados-en-su-construcción)

## a. Descripción general del proyecto

**SportsClubEventManager** es una aplicación web para la gestión integral de eventos de un club deportivo: publicación de un calendario de eventos, autoinscripción y cancelación por parte de los socios, y un panel de administración completo (eventos, usuarios, inscripciones e importación masiva vía CSV), todo ello protegido con autenticación OAuth2 + JWT y control de acceso basado en roles.

El proyecto evolucionó en varias iteraciones (MILESTONES + ISSUES) hasta convertirse en una aplicación completa:

1. **MVP sin autenticación**: modelo de dominio, persistencia y una API pública de solo lectura para consultar eventos.
2. **Autoinscripción**: los eventos pasan a poder aceptar inscripciones y cancelaciones con control de aforo.
3. **Interfaz Blazor**: calendario visual, listado, ficha de detalle y flujo de inscripción/cancelación para el usuario final.
4. **Seguridad y roles**: login con Google OAuth2 o email/contraseña, JWT, y dos roles (`User` / `Administrator`).
5. **Panel de administración**: gestión de usuarios, gestión de eventos (CRUD) y gestión de inscripciones, con registro de auditoría.
6. **Importación masiva**: carga de eventos desde CSV con previsualización, detección de duplicados y normalización automática de títulos.
7. **Telemetría**: métricas de negocio y de infraestructura expuestas en formato Prometheus y visualizadas en un dashboard de Grafana.
8. **Flujos de automatización**: notificaciones a los socios (confirmación de inscripción, actualización o cancelación de eventos, recordatorios) mediante flujos de n8n.
9. **Despliegue automático**: pipeline de CI/CD que construye, publica y despliega la aplicación de forma automática en cada cambio en `master`, con smoke test y rollback automatizados.

## b. Stack tecnológico utilizado

El detalle completo del stack tecnológico (plataforma, backend, frontend, persistencia, autenticación, observabilidad, automatización, contenedores, CI/CD y testing) está documentado en [`docs/development/overview.md`](docs/development/overview.md).

## c. Instalación y ejecución

La guía completa, paso a paso, para instalar y ejecutar la aplicación (vía Docker Compose o `dotnet run` local), ejecutar los tests y resolver los problemas más comunes, está documentada en [`docs/development/installation.md`](docs/development/installation.md).

## d. Despliegue

La aplicación se despliega de forma continua a un homelab personal (Docker Compose + Portainer, accesible por Tailscale) mediante un pipeline de CI/CD que construye, valida, publica y despliega automáticamente en cada nueva versión, con smoke test y rollback automatizados. La guía completa, paso a paso — configuración inicial, cómo publicar una nueva versión y troubleshooting — está documentada en [`docs/deployment/homelab-deployment.md`](docs/deployment/homelab-deployment.md).

## e. Estructura del proyecto

La aplicación sigue una arquitectura en capas (Clean Architecture), con separación estricta entre dominio, aplicación, infraestructura y presentación (API + Blazor), CQRS con MediatR y los patrones de diseño derivados de ambos. El detalle completo — vistas de capas, grafo de dependencias entre proyectos, árbol de carpetas, modelo de dominio y flujos end-to-end, todo respaldado con diagramas Mermaid — está documentado en [`docs/architecture/architecture.md`](docs/architecture/architecture.md).

## f. Funcionalidades principales

Cada funcionalidad está documentada en detalle en [`docs/operations/`](docs/operations/), con un diagrama de flujo Mermaid y su explicación.

**Para socios (rol `User`):**

- [Autenticación](docs/operations/autenticacion.md) — inicio de sesión con Google OAuth2 o email/contraseña, y cierre de sesión.
- [Consulta del calendario de eventos](docs/operations/calendario-eventos.md) — vista de calendario o listado, y ficha de detalle, accesible sin autenticación.
- [Inscripción y cancelación de inscripción a eventos](docs/operations/inscripcion-eventos.md) — con validación automática de aforo y duplicados.
- [Gestión del perfil propio](docs/operations/perfil-usuario.md) — edición de datos personales y cambio de contraseña.

**Para administradores (rol `Administrator`):**

- [Administración de usuarios](docs/operations/administracion-usuarios.md) — listado, edición, cambio de rol, activación/desactivación y borrado.
- [Administración de eventos](docs/operations/administracion-eventos.md) — CRUD completo de eventos.
- [Administración de inscripciones](docs/operations/administracion-inscripciones.md) — filtrado, inscripción manual, cancelación y exportación a CSV/PDF.
- [Importación masiva de eventos por CSV](docs/operations/importacion-masiva-eventos.md) — con previsualización, detección de duplicados y confirmación todo o nada.

## g. Usuario y contraseña de prueba

Al ejecutar el entorno en modo `Development` (Docker con `ASPNETCORE_ENVIRONMENT=Development`, o tras aplicar las migraciones de datos de prueba en local — ver [`c. Instalación y ejecución`](#c-instalación-y-ejecución)) se dispone de los siguientes usuarios:

| Rol | Email | Contraseña |
|---|---|---|
| Administrador | `admin@sportsclub.local` | La definida en la variable `ADMIN_PASSWORD` / secreto `AdminUser:Password` en el primer arranque |
| Socio | `carmen.garcia@example.com` | `Password1!` |
| Socio | `javier.martinez@example.com` | `Password1!` |
| Socio | `ana.fernandez@example.com` | `Password1!` |
| Socio | `miguel.sanchez@example.com` | `Password1!` |
| Socio | `laura.rodriguez@example.com` | `Password1!` |
| Socio | `carlos.jimenez@example.com` | `Password1!` |

> El acceso mediante **Google OAuth2** requiere registrar credenciales reales en [Google Cloud Console](https://console.cloud.google.com/apis/credentials); no existe un proveedor simulado para ese flujo.

### Gestión del administrador

- **Alta inicial**: la migración `SeedAdministratorUser` (a diferencia de `AddDevelopmentSeedData`/`SeedDevelopmentUserPasswords`, que solo se aplican en `Development`) se ejecuta en **cualquier entorno**, incluida producción, y crea `admin@sportsclub.local` leyendo su contraseña de `AdminUser:Password` (User Secrets en local, secreto de Docker `ADMIN_PASSWORD` en Compose). Es idempotente (`IF NOT EXISTS`): solo inserta el usuario la primera vez, por lo que cambiar `ADMIN_PASSWORD` en el `.env` **después** del primer arranque no modifica la contraseña de un administrador ya existente.
- **Cambiar la contraseña del administrador**: al no existir ninguna funcionalidad de "restablecer contraseña de otro usuario" (ni siquiera para administradores — ver [`administracion-usuarios.md`](docs/operations/administracion-usuarios.md), que solo permite editar datos, rol y estado, nunca la contraseña), la única vía es que el propio administrador inicie sesión y use el cambio de contraseña de autoservicio (`PUT /api/users/{id}/password`, ver [`perfil-usuario.md`](docs/operations/perfil-usuario.md)).
- **Añadir o quitar administradores**: cualquier administrador puede ascender a un socio existente al rol `Administrator` (o degradarlo de vuelta a `User`) desde [Administración de usuarios](docs/operations/administracion-usuarios.md) (`PUT /api/users/admin/{id}/role`). El sistema impide quedarse sin ningún administrador: tanto este cambio de rol como el borrado de un usuario se rechazan si el afectado es el último administrador restante.

## h. Proyectos personales empleados en su construcción

Este TFM se ha apoyado en varias herramientas y proyectos personales desarrollados previamente por el autor:

- 🏠 **Homelab casero** — infraestructura propia (Docker, Portainer) usada para el despliegue continuo de la aplicación.
- ⚙️ [**claude-sdlc-kit**](https://github.com/AlejBlasco/claude-sdlc-kit) — kit de agentes de IA que automatiza el ciclo de vida completo de desarrollo de software (análisis, diseño, implementación, testing, documentación y revisión), utilizado durante todo el proyecto (ver carpeta `.claude/`).
- 🔨 [**BlitzSliceForge**](https://github.com/AlejBlasco/BlitzSliceForge) — plantilla de generación de soluciones .NET en Clean Architecture, empleada como punto de partida de este repositorio.
