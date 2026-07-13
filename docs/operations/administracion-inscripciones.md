# Administración de inscripciones

Exclusiva del rol `Administrator`: visión global de todas las inscripciones del club, inscripción manual de un socio y cancelación de cualquier inscripción, con exportación a CSV/PDF. Referenciado desde la sección [`e. Funcionalidades principales`](../../README.md#e-funcionalidades-principales) del README.

## Flujo

```mermaid
flowchart TD
    Start(["Administrador visita<br/>Admin/RegistrationManagement.razor"]) --> List["GET /api/admin/registrations<br/>(GetRegistrationsAdminQuery)<br/>paginado; filtrable por evento, usuario,<br/>estado y rango de fechas; ordenable"]
    List --> Action{"¿Acción?"}

    Action -->|Inscribir manualmente| Manual["Selecciona socio + evento"]
    Manual --> PostReg["POST /api/admin/registrations<br/>(CreateAdminRegistrationCommand)"]
    PostReg --> SameChecks["Mismas reglas que la autoinscripción:<br/>evento futuro, sin duplicados, con aforo"]
    SameChecks --> ManualResult{"¿Válido?"}
    ManualResult -->|No| E409["404 / 409<br/>(EntityNotFound / Duplicate / CapacityExceeded)"]
    ManualResult -->|Sí| ManualOk["201 Created"]
    ManualOk --> Audit1["AuditLog: RegistrationCreated"]

    Action -->|Cancelar inscripción| CancelAny["Selecciona cualquier inscripción<br/>(propia o de cualquier socio)"]
    CancelAny --> DeleteReg["DELETE /api/admin/registrations/{id}<br/>(CancelRegistrationByIdCommand,<br/>IsAdministrator = true)"]
    DeleteReg --> CancelOk["204 No Content"]
    CancelOk --> Audit2["AuditLog: RegistrationCancelled"]

    Action -->|Exportar| Export{"¿Formato?"}
    Export -->|CSV| Csv["Genera CSV en el navegador<br/>a partir de la página actual cargada<br/>(sin nueva llamada a la Api)"]
    Export -->|PDF| Pdf["Genera informe de texto<br/>con extensión .pdf, misma fuente de datos"]
    Csv --> Download["Descarga vía JS interop<br/>(downloadFileFromText)"]
    Pdf --> Download
```

## Explicación del flujo

`AdminRegistrationsController` (`[Authorize(Roles = "Administrator")]`, ruta `api/admin/registrations`) da a los administradores la vista que antes solo existía en la cabeza del secretario del club: todas las inscripciones de todos los socios a todos los eventos, filtrables por evento, por socio, por estado (`RegistrationStatus`) y por rango de fechas del evento, con paginación y ordenación configurables (`GetRegistrationsAdminQuery`).

**Inscripción manual** (`POST /api/admin/registrations`, `CreateAdminRegistrationCommand`) cubre el caso de un socio que sigue prefiriendo pedir la inscripción por teléfono o en persona: el administrador la da de alta en su nombre. El handler aplica exactamente las mismas reglas de negocio que la autoinscripción (evento no finalizado, sin duplicados, con aforo disponible — ver [`inscripcion-eventos.md`](inscripcion-eventos.md)), evitando dos implementaciones divergentes de la misma regla.

**Cancelación** (`DELETE /api/admin/registrations/{id}`, `CancelRegistrationByIdCommand` con `IsAdministrator = true`) reutiliza el mismo comando que la cancelación de autoservicio (`RegistrationsController.CancelMyRegistration`), pero con el flag `IsAdministrator` que omite la comprobación de propiedad — el administrador puede cancelar la inscripción de cualquier socio, no solo la propia.

**Exportación a CSV/PDF**: a diferencia del resto de operaciones descritas en este documento, la exportación **no llama a ningún endpoint nuevo de la Api**. `RegistrationManagement.razor.cs` construye el fichero directamente en el navegador (`ExportCsvAsync`/`ExportPdfAsync`) a partir de los datos **ya cargados en la página actual** (`_registrations.Items`) y lo descarga vía interoperabilidad JavaScript (`downloadFileFromText`). Esto implica que la exportación refleja exactamente lo que el administrador está viendo en pantalla — la página actual, con los filtros aplicados — y no un volcado completo de todas las inscripciones si hay más de una página de resultados; para exportar un conjunto distinto hay que ajustar antes los filtros o el tamaño de página.
