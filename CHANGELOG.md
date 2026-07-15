# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `cd.yml`: new `tag-release-version` job automatically creates and pushes the `vX.Y.Z` git tag after a successful homelab deploy, when `Directory.Build.props`'s version isn't tagged yet and `CHANGELOG.md` documents it — triggering `release.yml` (GitHub Release creation) with no manual step, for the normal release flow. Falls back to a `::warning::` (never fails the job) if `CHANGELOG.md` isn't ready yet, since the deploy itself already succeeded by that point

### Fixed

- Google OAuth2 login no longer desyncs Web's and the Api's sessions: `GoogleCallback` used to set the `access_token`/`refresh_token` cookies on the Api's own origin and redirect the browser to Web's root, but those cookies were never visible to Web (different origin/port), leaving the user seemingly logged in with no actual session. Replaced with a one-time exchange code hand-off — the Api redirects to Web's new `/oauth-callback` page (`OAuthCallback.razor`) with a short-lived code minted by the new `IOAuthExchangeCodeStore`, which Web redeems server-to-server (`POST /api/authentication/oauth-exchange`) to build its own session, the same way local email/password login already does (#125)
- Login page silently swallowed `?error=oauth_failed`/`?error=token_missing` redirects from the Api; now shown to the user as a visible error message
- Local Docker Compose dev environment: `WebAppBaseUrl` was never set on the `api` service in `docker-compose.yml` (only in `docker-compose.prod.yml`), so the post-login redirect fell back to `https://localhost:7123` — a port not exposed by any container — instead of the actual Web container port
- `GET /account/logout` only cleared Web's own session cookie and never called `POST /api/authentication/logout`, so a user's `refresh_token` stayed valid in the database for its full 7-day lifetime after "logging out". Added `IAccountLogoutService`/`AccountLogoutService` (same typed-`HttpClient` pattern as the app's other Api services) so logout now revokes the `refresh_token` server-side too, best-effort (a failed revoke — e.g. an already-expired `access_token` — never blocks the local sign-out)
- `infrastructure/grafana/provisioning/datasources/datasources.yaml` provisioned our Prometheus datasource with `isDefault: true`, which it never needed (our dashboard/alert rules reference it by a fixed `uid`, not by "whichever is default") — verified against the real homelab Grafana (read-only, disposable container against `grafana.db`) that this silently flipped the shared, pre-existing `Prometheus` datasource from `is_default=1` to `0`, a side effect on infrastructure shared with other homelab apps. Fixed to `isDefault: false`; restoring the live Grafana's default is a separate manual action, not yet done, coordinated with the homelab owner (see `docs/observability/observability.md`, "Incidente 2")
- `docs/operations/importacion-masiva-eventos.md` documented the CSV import size limit as 10 MB; verified against `appsettings.json`/`appsettings.Development.json` (`ImportSettings:MaxFileSizeBytes: 5242880`) and `AdminImportController.cs` that the real, user-facing enforced limit is 5 MB — the 10 MB figures in code (`RequestFormLimits.MultipartBodyLengthLimit`, the Blazor client's `MaxBrowserReadSizeBytes`) are just a generous transport-level ceiling, not the authoritative limit

### Changed

- Extracted the `ClaimsPrincipal`-building logic (used after both local login and the new OAuth exchange) out of `Login.razor` into a shared `AuthenticationClaimsFactory`
- `docs/development/installation.md`: expanded the "Login con Google no funciona" troubleshooting with step-by-step instructions for creating a dev-only Google OAuth Client, and a table of the `invalid_client`/`redirect_uri_mismatch`/unreachable-callback symptoms and their causes
- `docs/development/installation.md`: corrected the WSL2 mount-propagation troubleshooting entry — the container that actually fails is `node-exporter` (explicit `rslave` propagation), not `cadvisor` (plain `ro` mount, unaffected)
- Moved `docs/technical/diagrams/` to `docs/architecture/diagrams/` — it's curated, living content (like `architecture.md`, which it already cross-references), not part of `docs/technical/`'s per-story design-doc history. Updated every cross-reference (`README.md`, `architecture.md`, two `docs/operations/*.md`, `issue-45-despliegue-automatizado-al-homelab.md`) and simplified the diagrams' own links to `architecture.md` now that they're siblings
- Renamed every `docs/functional/US-*.md` to `issue-*.md`, using the real GitHub issue number rather than the "US-N" story number — they only match 1:1 from issue #27 onward; earlier stories (US-2 through US-11) map to issues #4-#13 (e.g. `US-2-domain-model-foundation.md` → `issue-4-domain-model-foundation.md`, verified against GitHub, not assumed), matching the naming already used by `docs/technical/`'s and `docs/functional/`'s newer issue-*.md files
- Same rename for every `docs/technical/US-*.md`, same number mapping. Fixed every cross-reference this broke across `docs/`, `README.md` and `infrastructure/deploy/DEPLOYMENT_RUNBOOK.md` — including three links that were already broken before this rename (wrong slug guessed at authoring time, e.g. `US-27-oauth-authentication.md` instead of the real `US-27-oauth2-authentication.md`), found and fixed along the way
- Merged `infrastructure/deploy/DEPLOYMENT_RUNBOOK.md` into `docs/deployment/homelab-deployment.md` (now the single source of truth for the deploy/rollback runbook) and deleted the former — the happy path and rollback procedure were described in full in both, plus a third time (summarized) in `infrastructure/deploy/README.md`. `infrastructure/deploy/README.md` is now a minimal index (what's in this folder, what each script does) pointing at the docs/ runbook instead of restating it; `infrastructure/` keeps only scripts and minimal folder indexes, no narrative documentation. Fixed every reference to the deleted file across `docs/` and `infrastructure/README.md`
- Removed 6 "Funcionalidades/Características relacionadas" bullets across `docs/functional/*.md` that linked to files that never existed (wrong slugs, e.g. `us-7-event-registration.md`, `./user-profile.md`, `./rbac.md` — not typos introduced by any rename in this repo, just never-correct links from authoring time), plus 2 more explicitly marked "(pendiente)" pointing at guides never written (`eventos.md`, `inscripciones.md`) — including the `US-6-event-details-api.md` one flagged earlier as a known gap (issue #8 has no functional doc, still true, just no longer a dead link to it). Verified with a full repo-wide relative-link audit (all of `docs/`, `infrastructure/`, `README.md`, `CHANGELOG.md`) — no other broken links found
- `docs/development/installation.md`: reordered the `node-exporter`/WSL2 troubleshooting entry — the "skip the affected services" workaround had ended up sitting before the actual "Solución" fix with no label distinguishing the two, misleadingly readable as *the* fix. Now Causa → Solución (real fix) → Alternativa (workaround). Verified every package version in `docs/development/overview.md` (MediatR, FluentValidation, EF Core, CsvHelper, xUnit, Serilog, prometheus-net) against the real `.csproj` files — all accurate, no drift found

## [0.4.0] - 2026-07-14

### Added

- Grafana dashboard for `api`/`web` metrics, provisioned on the homelab's shared Prometheus/Grafana stack and published as a public, read-only, path-restricted dashboard (#43)
- Link to the live Grafana dashboard in the admin menu
- System diagrams (C4 Context, C4 Container, ER diagram, sequence diagrams for registration/cancellation/admin event CRUD, CI/CD pipeline flow), cataloged in `docs/technical/diagrams/` (#51)
- Observability documentation (`docs/observability/observability.md`) covering the production setup end-to-end, including a resolved Grafana provisioning incident
- Homelab deployment guide (`docs/deployment/homelab-deployment.md`)
- n8n workflows for member notifications (#37)

### Changed

- Manual registration in admin (`/admin/registrations`) now uses user/event dropdowns instead of free-text GUID inputs, only offering active users and upcoming events
- Registration Management: "Export CSV"/"Export PDF" export button moved to the page header
- CSV import: event description is no longer auto-composed from the Modality/Field/Category columns; left blank for the admin to fill in manually
- "View Members" now links to `/admin/users` (real user management) instead of the retired static `/users` page
- README: added an Observability & Metrics section with Prometheus/Grafana badges and a link to the live dashboard

### Removed

- PDF export in Registration Management: it generated plain text with a `.pdf` extension, which no PDF reader could actually open; removed instead of adding a PDF-generation dependency for a single export option. CSV export remains
- Static `/users` "Member Directory" placeholder page (`Users.razor`), superseded by the real `/admin/users` user management page, restricted to the `Administrator` role

### Fixed

- Login redirect now forces a full page reload, fixing unauthenticated users landing on the wrong page (URL changed but content didn't) after clicking a protected link from a static page
- Event description not shown when reopening the edit modal after saving (admin event management)
- Missing label on the role-edit button in the admin user details modal
- Grafana datasource name collision with a pre-existing manually-created datasource, which crashed the shared homelab Grafana on startup

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
