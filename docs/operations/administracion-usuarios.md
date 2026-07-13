# Administración de usuarios

Exclusiva del rol `Administrator`: listado, edición, cambio de rol, activación/desactivación y borrado de cualquier socio del club. Referenciado desde la sección [`e. Funcionalidades principales`](../../README.md#e-funcionalidades-principales) del README.

## Flujo

```mermaid
flowchart TD
    Start(["Administrador visita<br/>Admin/UserManagement.razor"]) --> List["GET /api/users/admin<br/>(GetAllUsersQuery)<br/>paginado, filtrable por rol/estado,<br/>buscable por nombre/email, ordenable"]
    List --> Select["Selecciona un usuario"]
    Select --> Action{"¿Acción?"}

    Action -->|Editar datos| Edit["PUT /api/users/admin/{id}<br/>(UpdateUserAsAdminCommand)"]
    Edit --> EditOk["Se actualizan nombre, email,<br/>género, licencia"]
    EditOk --> Audit1["AuditLog: UserUpdated"]

    Action -->|Cambiar rol| Role["PUT /api/users/admin/{id}/role<br/>(UpdateUserRoleCommand)"]
    Role --> LastAdminRole{"¿Se retira el rol Administrator<br/>y es el último administrador?"}
    LastAdminRole -->|Sí| E400a["400 Bad Request<br/>'Cannot remove the Administrator<br/>role from the last administrator'"]
    LastAdminRole -->|No| RoleOk["Rol actualizado"]
    RoleOk --> Audit2["AuditLog: RoleAssigned / RoleRemoved"]

    Action -->|Activar / desactivar| Status["PUT /api/users/admin/{id}/status<br/>(UpdateUserStatusCommand)"]
    Status --> StatusOk["IsActive actualizado<br/>(usuario inactivo no puede iniciar sesión)"]
    StatusOk --> Audit3["AuditLog: UserActivated / UserDeactivated"]

    Action -->|Eliminar| Delete["DELETE /api/users/admin/{id}<br/>(DeleteUserCommand)"]
    Delete --> LastAdminDel{"¿Es Administrator<br/>y es el último administrador?"}
    LastAdminDel -->|Sí| E400b["400 Bad Request<br/>'Cannot delete the last<br/>administrator in the system'"]
    LastAdminDel -->|No| DeleteOk["Usuario y sus inscripciones<br/>eliminados en cascada"]
    DeleteOk --> Audit4["AuditLog: UserDeleted"]
```

## Explicación del flujo

`UsersController` expone un segundo grupo de endpoints bajo `/api/users/admin/...`, todos protegidos con `[Authorize(Roles = "Administrator")]` — un socio con rol `User` recibe `403 Forbidden` si intenta invocarlos.

El listado (`GET /api/users/admin`, `GetAllUsersQuery`) admite paginación, filtro por rol (`roleFilter`) y estado (`isActiveFilter`), búsqueda de texto libre por nombre o email (`searchText`), y ordenación configurable (`sortBy`/`sortOrder`) — necesario desde el momento en que el club tiene más socios de los que caben en una sola pantalla.

Dos operaciones incorporan una salvaguarda explícita para no dejar el sistema sin administradores: tanto `UpdateUserRoleCommandHandler` (al retirar el rol `Administrator` de un usuario) como `DeleteUserCommandHandler` (al eliminarlo) cuentan cuántos administradores quedarían tras la operación y la rechazan con `InvalidOperationException` (→ `400 Bad Request`) si el resultado fuera cero. Sin esta comprobación, un error de un único administrador podría dejar el club sin nadie capaz de gestionar la aplicación.

`DeleteUser` es un borrado físico, no lógico: elimina el usuario y, en cascada, todas sus `Registration` asociadas — a diferencia de `UpdateUserStatus`, que solo desactiva la cuenta (`IsActive = false`) sin perder su histórico, impidiéndole iniciar sesión pero conservando sus inscripciones pasadas.

Las cuatro operaciones de escritura (`UpdateUserAsAdmin`, `UpdateUserRole`, `UpdateUserStatus`, `DeleteUser`) registran una entrada en `AuditLog` a través de `IAuditService`, capturando qué administrador realizó el cambio, sobre qué usuario, cuándo, y desde qué IP/User-Agent — la traza de auditoría que la gestión manual por WhatsApp nunca tuvo.
