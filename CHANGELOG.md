# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0] - 2026-07-13

### Added

- Health check endpoints (`/health/live`, `/health/ready`) for container orchestration and deployment readiness checks (#41)
- Prometheus metrics integration exposing business and infrastructure metrics, visualized in a Grafana dashboard (#42)
- Docker image validation in the CD pipeline: Trivy vulnerability scanning, image size baseline checks, and smoke tests before publishing (#44)
- Automated deployment to the homelab via Portainer webhook, with post-deploy smoke tests and automatic rollback (#45)
- Infrastructure as Code for the homelab Docker Compose stack (#46)
- n8n workflows for member notifications: registration confirmation, event updates/cancellations, and reminders (#37)

### Changed

- Environment-based configuration for application settings across Development/Production (US-39)
- Structured logging across API and Web (US-40)
- Secrets management for connection string, JWT key, Google OAuth credentials, and admin password (US-38)
- Real Docker image/GitHub Release versioning driven by `Directory.Build.props` `<Version>`, replacing the static placeholder (#99)
- Dependency updates (Dependabot: GitHub Actions multi-ecosystem group)

### Fixed

- Portainer webhook `204 No Content` response now treated as success in `cd.yml`
- Trivy SARIF scan no longer crashes when combined with `severity: CRITICAL`; the severity gate is now driven off the table report instead
- `AdminUser__Password` now passed through to the container at deploy time
- `extract-changelog-section.sh` no longer loses the escaped section-header pattern when handing it to `awk` via `-v` (gawk silently strips backslash sequences it doesn't recognize as C escapes, turning `\[X\.Y\.Z\]` into an unescaped character class that never matched); the pattern is now passed through `ENVIRON` instead, which is not re-escaped

## [0.2.0] - 2026-07-08

### Added

- Google OAuth2 and local email/password authentication issuing JWTs (US-27)
- Role-based authorization with `User` and `Administrator` roles (US-28)
- Self-service user profile management and password change (US-29)
- Admin user management: list, edit, change role, activate/deactivate, delete (US-30)
- Admin event management CRUD with capacity and date validation (US-31)
- Admin registration management with manual registration and CSV/PDF export (US-32)
- Bulk CSV event import with template, preview, and all-or-nothing confirmation (US-35)
- Automatic duplicate detection and title normalization during CSV import (US-36)
- Static and error pages (404, 500, maintenance, contact, FAQ, T&C, privacy, cookies)
- Audit log for administrative actions

### Changed

- Docker Compose configuration and CI pipeline improvements
- Admin navbar and admin pages UI refinements
- Dependency updates (Dependabot: GitHub Actions, NuGet packages)

### Fixed

- JWT no longer dropped when the Web app calls the API, fixing 401s on Profile and My Registrations
- Login and registration UI error handling
- User profile validation issues
- Event deletion and CSV import transactions now wrapped in the EF Core execution strategy
- Page redirection issues
- `EventDetails` test failures

### Security

- Upgraded `Microsoft.OpenApi` to fix advisory NU1903

## [0.1.0] - 2026-06-28

### Added

- Initial Clean Architecture solution scaffolding (Domain, Application, Infrastructure, Api, Web)
- Domain model foundation for events, users, and registrations (US-2)
- Database setup and EF Core migrations on SQL Server (US-3)
- Development seed data (US-4)
- Public event listing API with date-range filtering (US-5)
- Event self-registration and cancellation APIs (US-7, US-8)
- Blazor calendar/list view for browsing events with Radzen (US-9)
- Event details page with capacity indicator (US-10)
- Register/cancel UI flow (US-11)
- Docker Compose containerization for local development
- CI (build and test) and CD (build/push images, Portainer webhook) pipelines

[Unreleased]: https://github.com/AlejBlasco/SportsClubEventManager/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/AlejBlasco/SportsClubEventManager/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/AlejBlasco/SportsClubEventManager/releases/tag/v0.1.0
