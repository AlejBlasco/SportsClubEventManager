# Arquitectura del sistema

Documento de referencia para la sección [`f. Estructura del proyecto`](../../README.md#f-estructura-del-proyecto) del README. Describe la arquitectura de **SportsClubEventManager**, los patrones de diseño aplicados y por qué, con diagramas [Mermaid](https://mermaid.js.org/) que respaldan cada decisión sobre la base del código real del repositorio (no un diseño aspiracional).

## Índice

- [1. Principios arquitectónicos](#1-principios-arquitectónicos)
- [2. Vista de capas (Clean Architecture)](#2-vista-de-capas-clean-architecture)
- [3. Grafo de referencias entre proyectos](#3-grafo-de-referencias-entre-proyectos)
- [4. Estructura de carpetas](#4-estructura-de-carpetas)
- [5. CQRS: comandos y consultas](#5-cqrs-comandos-y-consultas)
- [6. Mediator y pipeline de comportamientos (MediatR)](#6-mediator-y-pipeline-de-comportamientos-mediatr)
- [7. Modelo de dominio](#7-modelo-de-dominio)
- [8. Persistencia: inversión de dependencias sobre EF Core](#8-persistencia-inversión-de-dependencias-sobre-ef-core)
- [9. Flujo end-to-end: inscribirse a un evento](#9-flujo-end-to-end-inscribirse-a-un-evento)
- [10. Manejo centralizado de errores](#10-manejo-centralizado-de-errores)
- [11. Comunicación Web → API: cadena de DelegatingHandlers](#11-comunicación-web--api-cadena-de-delegatinghandlers)
- [12. Tareas en segundo plano](#12-tareas-en-segundo-plano)
- [13. Composition root e inyección de dependencias](#13-composition-root-e-inyección-de-dependencias)
- [14. Resumen de patrones de diseño aplicados](#14-resumen-de-patrones-de-diseño-aplicados)

---

## 1. Principios arquitectónicos

El proyecto sigue **Clean Architecture** (Robert C. Martin): el dominio de negocio no depende de ningún detalle técnico (base de datos, framework web, proveedores externos), y son esos detalles los que dependen del dominio, nunca al revés. Esta inversión se conoce como la **regla de dependencia**: las flechas de dependencia de código fuente solo pueden apuntar hacia dentro, hacia las capas más abstractas.

```mermaid
flowchart TB
    subgraph Externo["Capas externas — detalles"]
        Api["SportsClubEventManager.Api<br/>(ASP.NET Core Web API)"]
        Web["SportsClubEventManager.Web<br/>(Blazor Server)"]
        Infra["SportsClubEventManager.Infrastructure<br/>(EF Core, OAuth2, CSV, Prometheus, n8n)"]
    end

    subgraph Aplicacion["Application — casos de uso"]
        App["SportsClubEventManager.Application<br/>(CQRS · MediatR · FluentValidation)"]
    end

    subgraph Centro["Domain — núcleo, sin dependencias externas"]
        Domain["SportsClubEventManager.Domain<br/>(Entidades · Enums · Reglas de negocio)"]
    end

    Api -.dependencia.-> App
    Web -.dependencia.-> Infra
    Infra -.dependencia.-> App
    App -.dependencia.-> Domain

    style Centro fill:#2f5233,color:#fff
    style Aplicacion fill:#3a6b8a,color:#fff
    style Externo fill:#6b6b6b,color:#fff
```

Ninguna clase de `Domain` referencia `Application`, `Infrastructure`, `Api` ni `Web`. `Application` no conoce Entity Framework Core ni ASP.NET Core: define **interfaces** (`IApplicationDbContext`, `IPasswordHasher`, `ITokenService`, `IWorkflowNotifier`...) que las capas externas implementan — es el **Principio de Inversión de Dependencias** (la "D" de SOLID) aplicado de forma sistemática en todo el proyecto.

Sobre esta base se apoyan varios patrones de diseño concretos, desarrollados en las secciones siguientes:

| Patrón | Dónde |
|---|---|
| CQRS (Command Query Responsibility Segregation) | `Application` — comandos y consultas separados por caso de uso |
| Mediator | `Application` — MediatR desacopla los controladores de los handlers |
| Pipeline / Chain of Responsibility | Comportamientos de MediatR, middlewares de ASP.NET Core, `DelegatingHandler` de `HttpClient` |
| Inversión de dependencias sobre persistencia | `IApplicationDbContext` (Application) / `AppDbContext` (Infrastructure) |
| Repository implícito vía `DbSet<T>` | `AppDbContext` actúa como Unit of Work sobre EF Core |
| Options pattern | Configuración fuertemente tipada (`JwtSettingsOptions`, `GoogleAuthOptions`, `MetricsOptions`, `N8nOptions`, `ApiSettingsOptions`...) |
| Composition root | Un método `AddXxx(this IServiceCollection)` por capa, invocado desde cada `Program.cs` |
| Hosted Service / Background worker | `ActiveEventsGaugeUpdater`, `EventReminderBackgroundService` |
| Optimistic concurrency | `Event.RowVersion` (control de concurrencia en inscripciones) |

## 2. Vista de capas (Clean Architecture)

Vista de componentes, ampliando el diagrama de la sección `a.` del README con el detalle de las dependencias externas de cada capa:

```mermaid
flowchart TB
    subgraph Cliente
        Browser["Navegador"]
    end

    subgraph Presentacion["Presentación"]
        WebApp["SportsClubEventManager.Web<br/>Blazor Server · Radzen.Blazor"]
        ApiApp["SportsClubEventManager.Api<br/>ASP.NET Core Web API · Swagger"]
    end

    subgraph Nucleo["Núcleo de aplicación"]
        App["SportsClubEventManager.Application<br/>CQRS · MediatR · FluentValidation"]
        Domain["SportsClubEventManager.Domain<br/>Entidades · Enums · Excepciones de dominio"]
    end

    subgraph Infra["Infraestructura"]
        InfraApp["SportsClubEventManager.Infrastructure<br/>EF Core · BCrypt · CsvHelper · Serilog"]
    end

    subgraph Externos["Sistemas externos"]
        DB[("SQL Server 2022")]
        Google["Google OAuth2"]
        N8n["n8n (homelab)"]
        Prom["Prometheus"]
    end

    Browser -->|HTTPS| WebApp
    WebApp -->|"HTTP + JWT<br/>(HttpClient tipado)"| ApiApp
    ApiApp --> App
    App --> Domain
    ApiApp --> InfraApp
    WebApp --> InfraApp
    InfraApp --> App
    InfraApp --> DB
    InfraApp <--> Google
    InfraApp -->|webhooks| N8n
    ApiApp -->|/metrics| Prom
    WebApp -->|/metrics| Prom
```

`SportsClubEventManager.Shared` (DTOs) no aparece en el diagrama por claridad: es referenciado transversalmente por `Api`, `Application` y `Web` para definir los contratos de datos que cruzan la frontera HTTP, sin acoplar `Web` a los tipos internos de `Application`/`Domain`.

> Este diagrama muestra las **capas de código** dentro de cada proceso. Para la vista C4 Container equivalente (unidades de despliegue reales — `Api`, `Web`, `SQL Server`, y los sistemas externos `Google`/`n8n`/`Prometheus`/`Grafana`, sin exponer las capas internas), ver [`docs/technical/diagrams/c4-container.md`](../technical/diagrams/c4-container.md).

## 3. Grafo de referencias entre proyectos

Dependencias reales declaradas en cada `.csproj` (`<ProjectReference>`), que materializan la regla de dependencia de la sección 1:

```mermaid
flowchart LR
    Domain["Domain"]
    Shared["Shared"]
    Application["Application"]
    Infrastructure["Infrastructure"]
    Api["Api"]
    Web["Web"]

    Application --> Domain
    Application --> Shared
    Infrastructure --> Application
    Infrastructure --> Domain
    Api --> Application
    Api --> Infrastructure
    Api --> Shared
    Web --> Infrastructure
    Web --> Shared
```

`Domain` no tiene ninguna flecha saliente: es el único proyecto sin `ProjectReference` a ningún otro, confirmando que es el núcleo de la arquitectura. `Web` no referencia `Application` directamente — solo consume la API vía HTTP (ver [sección 11](#11-comunicación-web--api-cadena-de-delegatinghandlers)), aunque comparte proceso con `Infrastructure` para health checks y logging.

## 4. Estructura de carpetas

```
/src
  /SportsClubEventManager.Domain
    /Common            → BaseEntity (Id, CreatedAt, UpdatedAt)
    /Entities           → Event, Registration, User, AuditLog, EventReminderNotification
    /Enums              → RegistrationStatus, Role, Gender, AuditAction
    /Exceptions         → DomainException, EntityNotFoundException, CapacityExceededException, DuplicateRegistrationException
  /SportsClubEventManager.Application
    /Common
      /Behaviors        → LoggingBehavior, ValidationBehavior (pipeline de MediatR)
      /Interfaces        → IApplicationDbContext, IPasswordHasher, ITokenService, IWorkflowNotifier, IApplicationMetrics...
      /Exceptions, /Models, /Validators, /Constants
    /Events             → Commands (CreateEvent, UpdateEvent, DeleteEvent, RegisterForEvent, CancelRegistration) + Queries (GetEvents, GetEventById, GetEventsAdmin)
    /Registrations      → Commands (CreateAdminRegistration, CancelRegistrationById) + Queries (GetUserRegistrations, GetRegistrationsAdmin)
    /Users              → Commands (UpdateProfile, ChangePassword, UpdateUserAsAdmin, UpdateUserRole, UpdateUserStatus, DeleteUser) + Queries (GetAllUsers, GetUserById, GetUserProfile)
    /Import             → Commands (ParseCsvFile, BulkCreateEvents)
    /Authentication     → Commands (Login, Logout, RefreshToken)
    /Authorization       → Policies
  /SportsClubEventManager.Infrastructure
    /Persistence         → AppDbContext, /Configurations (Fluent API de EF Core)
    /Migrations          → historial de migraciones EF Core
    /Authentication       → OAuth2, JWT, BCrypt
    /Import               → parser CSV
    /Metrics              → ActiveEventsGaugeUpdater (prometheus-net)
    /Notifications        → EventReminderBackgroundService, cliente HTTP de n8n
    /Logging              → configuración de Serilog
    /Services             → AuditService
    /Configuration         → Options fuertemente tipadas + carga de secretos
  /SportsClubEventManager.Shared
    /DTOs                 → contratos de datos entre Api y Web
  /SportsClubEventManager.Api
    /Controllers           → EventsController, RegistrationsController, UsersController, AuthenticationController, AdminEventsController, AdminRegistrationsController, AdminImportController
    /Middleware             → ExceptionHandlingMiddleware, CorrelationIdMiddleware, RequestUserLogContextMiddleware, UnauthorizedAccessLoggingMiddleware
    /Configuration           → registro de servicios de la Api
    /HealthChecks
  /SportsClubEventManager.Web
    /Components
      /Pages, /Admin, /Events, /Authentication, /Layout, /Shared
    /Services               → EventService, RegistrationService, UserManagementService... + DelegatingHandlers (AuthTokenHandler, CorrelationIdHandler, ApiCallLoggingHandler)
    /Configuration
/tests                       → un proyecto de test por capa (ver docs/development/overview.md)
```

Organización interna de `Application` **por feature** (vertical slices), no por tipo técnico — cada carpeta de caso de uso agrupa el comando/query junto a su handler y su validador:

```mermaid
graph TD
    App["Application"] --> Events
    App --> Registrations
    App --> Users
    App --> Import
    App --> Authentication
    App --> Common["Common<br/>(Behaviors, Interfaces, Exceptions)"]

    Events --> EC["Commands/<br/>RegisterForEvent/<br/>├─ Command<br/>├─ CommandHandler<br/>└─ CommandValidator"]
    Events --> EQ["Queries/<br/>GetEvents/<br/>├─ Query<br/>├─ QueryHandler<br/>└─ QueryValidator"]
```

Esta organización (**vertical slice architecture** dentro de la capa Application) favorece la cohesión: todo lo necesario para entender o modificar un caso de uso concreto vive en una sola carpeta, en vez de repartirse entre capas técnicas horizontales (`Controllers/`, `Services/`, `Repositories/`...).

## 5. CQRS: comandos y consultas

**CQRS** separa las operaciones que modifican estado (**Commands**) de las que solo leen datos (**Queries**), cada una con su propio modelo de entrada/salida. En este proyecto ambas viajan por el mismo `IMediator`, pero como tipos (`IRequest<TResponse>`) e intención completamente distintos:

```mermaid
flowchart LR
    subgraph Escritura["Comando (escritura)"]
        C["RegisterForEventCommand"] --> CH["RegisterForEventCommandHandler"]
        CH --> W1["Modifica el grafo de entidades"]
        W1 --> W2["context.SaveChangesAsync()"]
    end

    subgraph Lectura["Consulta (lectura)"]
        Q["GetEventsQuery"] --> QH["GetEventsQueryHandler"]
        QH --> R1["Proyección directa a DTO<br/>(sin tracking, sin SaveChanges)"]
    end

    Controller["Controller"] -->|"mediator.Send(command)"| C
    Controller -->|"mediator.Send(query)"| Q
```

Inventario de casos de uso existentes, agrupados por *feature*:

| Feature | Comandos (escritura) | Consultas (lectura) |
|---|---|---|
| Events | CreateEvent, UpdateEvent, DeleteEvent, RegisterForEvent, CancelRegistration | GetEvents, GetEventById, GetEventsAdmin |
| Registrations | CreateAdminRegistration, CancelRegistrationById | GetUserRegistrations, GetRegistrationsAdmin |
| Users | UpdateProfile, ChangePassword, UpdateUserAsAdmin, UpdateUserRole, UpdateUserStatus, DeleteUser | GetAllUsers, GetUserById, GetUserProfile |
| Import | ParseCsvFile, BulkCreateEvents | — |
| Authentication | Login, Logout, RefreshToken | — |

Cada comando/query es un `record` inmutable que implementa `IRequest<TResponse>`; cada handler implementa `IRequestHandler<TRequest, TResponse>` y es la **única** clase que conoce cómo resolver ese caso de uso concreto (principio de responsabilidad única aplicado a nivel de caso de uso, no de clase técnica).

## 6. Mediator y pipeline de comportamientos (MediatR)

Los controladores de la Api no invocan handlers directamente: publican la petición a través de `IMediator` (**patrón Mediator**), que la enruta al handler correspondiente atravesando antes una cadena de **comportamientos transversales** (**patrón Pipeline / Chain of Responsibility**), registrados en `Application/DependencyInjection.cs`:

```mermaid
sequenceDiagram
    participant Ctrl as Controller
    participant Med as IMediator
    participant Log as LoggingBehavior
    participant Val as ValidationBehavior
    participant H as Handler

    Ctrl->>Med: Send(command)
    Med->>Log: Handle(request, next)
    Log->>Log: LogInformation("Handling {Request}")
    Log->>Val: next()
    Val->>Val: Ejecuta todos los validadores registrados (FluentValidation)
    alt Validación falla
        Val-->>Log: throw ValidationException
        Log->>Log: LogWarning(errores)
        Log-->>Ctrl: propaga excepción
    else Validación correcta
        Val->>H: next()
        H->>H: Handle(request, ct)
        H-->>Val: TResponse
        Val-->>Log: TResponse
        Log->>Log: LogInformation("Handled in {ms}ms")
        Log-->>Ctrl: TResponse
    end
```

`LoggingBehavior` se registra **antes** que `ValidationBehavior` para ser la capa más externa del pipeline: así también queda registrado cuándo y por qué falla una validación, no solo los fallos del propio handler. Ningún comportamiento atrapa la excepción de forma silenciosa — todas se propagan hasta el [middleware de manejo de errores](#10-manejo-centralizado-de-errores) de la Api.

## 7. Modelo de dominio

Entidades del dominio, todas heredando de `BaseEntity` (identidad + auditoría de fechas), sin ninguna dependencia de EF Core ni de ningún framework:

```mermaid
classDiagram
    class BaseEntity {
        <<abstract>>
        +Guid Id
        +DateTime CreatedAt
        +DateTime? UpdatedAt
    }

    class Event {
        +string Title
        +string? Description
        +DateTime Date
        +string Location
        +int MaxCapacity
        +byte[]? RowVersion
        +int CurrentRegistrations
        +bool IsFull
        +CanAcceptRegistration() bool
        +ValidateFutureDate() void
    }

    class Registration {
        +Guid EventId
        +Guid UserId
        +DateTime RegistrationDate
        +RegistrationStatus Status
        +Cancel() void
        +IsActive() bool
    }

    class User {
        +string Name
        +Gender Gender
        +string Email
        +string? LicenseNumber
        +string? PasswordHash
        +string? ExternalProviderId
        +bool IsActive
        +Role Role
    }

    class AuditLog {
        +AuditAction Action
        +Guid PerformedByUserId
        +Guid TargetUserId
        +string TargetUserEmail
        +DateTime Timestamp
        +string? Changes
    }

    class EventReminderNotification {
        +Guid EventId
        +int IntervalHours
        +DateTime SentAtUtc
    }

    BaseEntity <|-- Event
    BaseEntity <|-- Registration
    BaseEntity <|-- User
    BaseEntity <|-- AuditLog
    BaseEntity <|-- EventReminderNotification

    Event "1" --> "*" Registration : Registrations
    User "1" --> "*" Registration : Registrations
    User "1" --> "*" AuditLog : PerformedByUser
    Event "1" --> "*" EventReminderNotification
```

El modelo aplica **encapsulación real** (no son simples *anemic models*): `Event.MaxCapacity` valida en su propio setter que sea mayor que cero (`ValidateCapacity`), lanzando `DomainException` si no lo es; `User.Email` valida su formato en el setter con una expresión regular compilada; `Event.IsFull`/`CanAcceptRegistration()` calculan invariantes de negocio (aforo) a partir de sus propias colecciones, en vez de delegar esa lógica a la capa de aplicación. `Event.RowVersion` habilita **concurrencia optimista** de EF Core para evitar condiciones de carrera cuando dos socios se inscriben simultáneamente en la última plaza disponible.

> Este `classDiagram` muestra el **modelo de dominio rico** (comportamiento, invariantes). Para el **esquema relacional** (columnas, tipos, claves foráneas y cardinalidad, verificado contra las 5 `IEntityTypeConfiguration<T>` reales), ver [`docs/technical/diagrams/er-diagram.md`](../technical/diagrams/er-diagram.md).

## 8. Persistencia: inversión de dependencias sobre EF Core

`Application` no conoce Entity Framework Core: define la interfaz `IApplicationDbContext` con únicamente lo que los casos de uso necesitan (los `DbSet<T>` y `SaveChangesAsync`). `Infrastructure.AppDbContext` es la única clase que implementa esa interfaz y la única que sabe que existe SQL Server detrás.

```mermaid
flowchart TB
    subgraph AppLayer["Application (interfaz)"]
        IDbContext["IApplicationDbContext<br/>+ DbSet&lt;Event&gt; Events<br/>+ DbSet&lt;Registration&gt; Registrations<br/>+ DbSet&lt;User&gt; Users<br/>+ DbSet&lt;AuditLog&gt; AuditLogs<br/>+ SaveChangesAsync()"]
        Handler["RegisterForEventCommandHandler<br/>(constructor: IApplicationDbContext context)"]
    end

    subgraph InfraLayer["Infrastructure (implementación)"]
        AppDbContext["AppDbContext : DbContext, IApplicationDbContext"]
        Configs["Configurations/<br/>(Fluent API — mapeo entidad ⇄ tabla)"]
        SqlServer[("SQL Server")]
    end

    Handler -->|depende de| IDbContext
    AppDbContext -.implementa.-> IDbContext
    AppDbContext --> Configs
    AppDbContext --> SqlServer
```

`AppDbContext` actúa de facto como un **Unit of Work**: agrupa todos los cambios pendientes en el `ChangeTracker` de EF Core y los confirma de forma atómica en una única llamada a `SaveChangesAsync`, sin que los handlers necesiten coordinar transacciones manualmente. No existe una capa `Repository` explícita por entidad — se ha optado deliberadamente por exponer `DbSet<T>` a través de la interfaz (patrón habitual en plantillas de Clean Architecture como la de Jason Taylor), evitando una capa de indirección adicional que aquí no aportaría valor: los `DbSet<T>` ya son en sí mismos una implementación del patrón Repository sobre EF Core, y el mapeo objeto-relacional (columnas, claves, índices únicos) se mantiene separado en `Persistence/Configurations/` mediante `IEntityTypeConfiguration<T>`, no en las entidades de dominio.

## 9. Flujo end-to-end: inscribirse a un evento

Ejemplo completo, de extremo a extremo, del caso de uso `RegisterForEvent` — desde el clic del socio en el navegador hasta la confirmación de negocio, atravesando todas las capas y patrones descritos arriba:

```mermaid
sequenceDiagram
    actor Socio as Socio (navegador)
    participant Comp as Componente Blazor
    participant Svc as EventService (Web)
    participant H1 as AuthTokenHandler
    participant H2 as CorrelationIdHandler
    participant H3 as ApiCallLoggingHandler
    participant Ctrl as EventsController (Api)
    participant Med as MediatR
    participant Val as ValidationBehavior
    participant Handler as RegisterForEventCommandHandler
    participant Db as AppDbContext / SQL Server
    participant Metrics as IApplicationMetrics (Prometheus)
    participant N8n as IWorkflowNotifier (n8n)

    Socio->>Comp: Clic en "Inscribirme"
    Comp->>Svc: RegisterAsync(eventId)
    Svc->>H1: HTTP POST /api/events/{id}/register
    H1->>H1: Añade header Authorization: Bearer {JWT}
    H1->>H2: next()
    H2->>H2: Añade header X-Correlation-Id
    H2->>H3: next()
    H3->>H3: Log de la petición saliente
    H3->>Ctrl: HTTP POST
    Ctrl->>Med: Send(RegisterForEventCommand)
    Med->>Val: pipeline (Logging → Validation)
    Val->>Handler: Handle(command, ct)
    Handler->>Db: Events.Include(Registrations).FirstOrDefault
    Db-->>Handler: Event + Registrations
    Handler->>Handler: Valida fecha futura, duplicados, aforo
    alt Aforo completo o ya inscrito
        Handler-->>Ctrl: throw CapacityExceededException / DuplicateRegistrationException
        Ctrl-->>Socio: 500 / ProblemDetails (ver sección 10)
    else Inscripción válida
        Handler->>Db: Registrations.Add(registration)
        Handler->>Db: SaveChangesAsync()
        Db-->>Handler: OK
        Handler->>Metrics: RecordRegistrationCreated("self-service")
        Handler->>N8n: NotifyRegistrationConfirmedAsync(payload)
        Handler-->>Ctrl: RegistrationCreatedDto
        Ctrl-->>Svc: 201 Created + DTO
        Svc-->>Comp: RegistrationCreatedDto
        Comp-->>Socio: Confirmación en pantalla
    end
```

Este flujo ilustra por qué las métricas y las notificaciones se disparan **después** de `SaveChangesAsync` y no antes: si la transacción falla (por ejemplo, un conflicto de concurrencia sobre `RowVersion`), ni el contador Prometheus ni el webhook de n8n deben contabilizar una inscripción que nunca llegó a persistirse.

## 10. Manejo centralizado de errores

Ningún controlador de la Api contiene bloques `try/catch`: todas las excepciones (de validación, de negocio o inesperadas) atraviesan sin capturar el pipeline de MediatR y llegan hasta `ExceptionHandlingMiddleware`, que las traduce a una respuesta `ProblemDetails` (RFC 7807), centralizando en un único punto la política de errores de toda la Api.

```mermaid
flowchart LR
    Handler["Command/Query Handler<br/>throw DomainException / ValidationException / ..."]
    Behaviors["Pipeline de MediatR<br/>(no captura, solo registra el log)"]
    MW["ExceptionHandlingMiddleware"]
    Resp["HTTP Response<br/>application/problem+json"]

    Handler -->|excepción sin capturar| Behaviors
    Behaviors -->|excepción sin capturar| MW
    MW -->|ValidationException| Resp400["400 Bad Request<br/>+ errores agrupados por campo"]
    MW -->|cualquier otra excepción| Resp500["500 Internal Server Error"]
```

Este es el mismo patrón (**Chain of Responsibility** vía middleware de ASP.NET Core) que ASP.NET Core aplica de forma nativa a toda la *request pipeline*: cada middleware decide si maneja la petición/excepción o la delega en el siguiente. `CorrelationIdMiddleware`, `RequestUserLogContextMiddleware` y `UnauthorizedAccessLoggingMiddleware` siguen la misma filosofía para enriquecer cada log con contexto (ID de correlación, usuario autenticado) sin acoplar esa responsabilidad a los controladores.

## 11. Comunicación Web → API: cadena de DelegatingHandlers

`SportsClubEventManager.Web` no accede a la base de datos ni a `Application` directamente: consume `SportsClubEventManager.Api` como cualquier cliente HTTP externo, a través de servicios tipados (`EventService`, `RegistrationService`, `UserManagementService`...) registrados con `IHttpClientFactory`. Cada `HttpClient` tipado encadena tres `DelegatingHandler` (**patrón Decorator / Chain of Responsibility** aplicado sobre `HttpClient`), en el mismo orden para todos los servicios:

```mermaid
flowchart LR
    Svc["EventService / RegistrationService / ...<br/>(HttpClient tipado)"]
    H1["AuthTokenHandler<br/>añade Authorization: Bearer {JWT}"]
    H2["CorrelationIdHandler<br/>añade X-Correlation-Id"]
    H3["ApiCallLoggingHandler<br/>log de la llamada saliente"]
    Api["SportsClubEventManager.Api"]

    Svc --> H1 --> H2 --> H3 --> Api
```

Cada servicio de `Web/Services` implementa una interfaz propia (`IEventService`, `IRegistrationService`...), lo que permite sustituirlos por dobles de prueba (`WireMock.Net`) en los tests de componentes Blazor con **bUnit**, sin necesidad de levantar la Api real.

## 12. Tareas en segundo plano

Dos `BackgroundService` (patrón **Hosted Service** de .NET) ejecutan trabajo periódico fuera del ciclo petición/respuesta HTTP, cada una con su propio `IServiceScope` por iteración para resolver dependencias con ciclo de vida `Scoped` (como `IApplicationDbContext`) de forma segura desde un servicio `Singleton`:

```mermaid
flowchart TB
    subgraph Timer1["PeriodicTimer cada 30s (configurable)"]
        Gauge["ActiveEventsGaugeUpdater"]
    end
    subgraph Timer2["PeriodicTimer cada 5 min (configurable)"]
        Reminder["EventReminderBackgroundService"]
    end

    Gauge -->|crea IServiceScope| DbG["IApplicationDbContext"]
    DbG -->|cuenta eventos futuros| Prom["sportsclubeventmanager_active_events<br/>(gauge de Prometheus)"]

    Reminder -->|crea IServiceScope| DbR["IApplicationDbContext"]
    DbR -->|eventos próximos a empezar| Check{"¿Recordatorio ya enviado?<br/>(EventReminderNotifications)"}
    Check -->|No| Send["IWorkflowNotifier → webhook n8n"]
    Check -->|Sí| Skip["Omitir (idempotencia)"]
    Send --> Log["Registrar en EventReminderNotifications<br/>(índice único EventId+IntervalHours)"]
```

El índice único `EventId`+`IntervalHours` sobre `EventReminderNotification` es lo que garantiza la idempotencia del recordatorio incluso ante un reinicio del contenedor a mitad de un ciclo de sondeo.

## 13. Composition root e inyección de dependencias

Cada capa expone su propio método de extensión `AddXxx(this IServiceCollection)` (`Application.DependencyInjection.AddApplication()`, `Infrastructure.DependencyInjection.AddInfrastructure()`, `Api.Configuration.ApiConfigurationExtensions`, `Web.Configuration.WebConfigurationExtensions`), responsable únicamente de registrar los servicios que esa capa aporta. El único punto donde todas se ensamblan es el **Composition Root**: el `Program.cs` de cada host.

```mermaid
flowchart TB
    subgraph ApiProgram["Api/Program.cs"]
        A1["AddApplication()"] --> A2["AddInfrastructure()"] --> A3["AddApiConfiguration()<br/>(JWT, Google OAuth2, Swagger, CORS)"]
    end

    subgraph WebProgram["Web/Program.cs"]
        W1["AddInfrastructure()"] --> W2["AddWebConfiguration()"] --> W3["AddHttpClient&lt;IEventService,...&gt;()<br/>+ AddHttpMessageHandler&lt;...&gt;() encadenados"]
    end

    A1 -.registra.-> MediatRReg["MediatR + FluentValidation<br/>+ LoggingBehavior + ValidationBehavior"]
    A2 -.registra.-> InfraReg["AppDbContext, IPasswordHasher,<br/>ITokenService, IWorkflowNotifier,<br/>BackgroundServices, Options tipadas"]
```

Ninguna capa interna (`Domain`, `Application`) conoce el contenedor de DI de ASP.NET Core más allá de estos métodos de extensión — es la única concesión pragmática a un framework concreto, y queda confinada a un único fichero por capa.

## 14. Resumen de patrones de diseño aplicados

| Patrón | Categoría (GoF / arquitectura) | Dónde se aplica | Por qué |
|---|---|---|---|
| Clean Architecture / regla de dependencia | Arquitectónico | Todo el `src/` | Aísla el dominio de negocio de los detalles técnicos (framework, BD, proveedores externos) |
| CQRS | Arquitectónico | `Application` (Commands/Queries por feature) | Separa intención de lectura y escritura; cada caso de uso es autocontenido |
| Mediator | Comportamiento | MediatR, `Api` → `Application` | Desacopla los controladores de los handlers concretos |
| Pipeline / Chain of Responsibility | Comportamiento | `LoggingBehavior`/`ValidationBehavior` (MediatR), middlewares (`Api`), `DelegatingHandler` (`Web`) | Aplica comportamiento transversal (logging, validación, autenticación) sin ensuciar cada caso de uso |
| Inversión de dependencias | Estructural (SOLID) | `IApplicationDbContext`, `IPasswordHasher`, `ITokenService`, `IWorkflowNotifier`, `IApplicationMetrics` | `Application` define contratos; `Infrastructure` los implementa |
| Repository / Unit of Work (implícito) | Estructural | `AppDbContext` vía `DbSet<T>` + `SaveChangesAsync` | Persistencia agrupada en una única transacción por caso de uso |
| Options pattern | Estructural | `JwtSettingsOptions`, `GoogleAuthOptions`, `MetricsOptions`, `N8nOptions`, `ApiSettingsOptions` | Configuración fuertemente tipada y validada al arranque (ver [`docs/development/installation.md`](../development/installation.md)) |
| Composition root | Estructural | Un `AddXxx()` por capa, invocado desde cada `Program.cs` | Un único punto de ensamblado por host, capas internas ignoran el contenedor DI |
| Hosted Service | Comportamiento | `ActiveEventsGaugeUpdater`, `EventReminderBackgroundService` | Trabajo periódico fuera del ciclo petición/respuesta |
| Optimistic concurrency | Estructural (persistencia) | `Event.RowVersion` | Evita condiciones de carrera al inscribirse en la última plaza disponible |
| Vertical slice | Organizativo | Carpetas por feature dentro de `Application` (`Events/`, `Users/`...) | Alta cohesión: todo lo relativo a un caso de uso vive junto |
