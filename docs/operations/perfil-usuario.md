# Gestión del perfil propio

Permite a cualquier socio consultar y actualizar sus propios datos, y cambiar su contraseña, sin intervención de un administrador. Referenciado desde la sección [`g. Funcionalidades principales`](../../README.md#g-funcionalidades-principales) del README.

## Flujo

```mermaid
flowchart TD
    Start(["Socio autenticado visita<br/>UserProfile.razor"]) --> Get["GET /api/users/{id}/profile<br/>(GetUserProfileQuery)"]
    Get --> Owner1{"¿id == usuario autenticado?"}
    Owner1 -->|No| E403a["403 Forbidden<br/>(solo se puede ver el propio perfil)"]
    Owner1 -->|Sí| Show["Se muestran: nombre, género,<br/>email, nº de licencia, categoría"]

    Show --> Choice{"¿Qué acción realiza el socio?"}

    Choice -->|Editar datos| Edit["Modifica nombre / género / email /<br/>licencia / categoría"]
    Edit --> Put["PUT /api/users/{id}/profile<br/>(UpdateProfileCommand)"]
    Put --> Owner2{"¿RequestingUserId == id?"}
    Owner2 -->|No| E403b["403 Forbidden"]
    Owner2 -->|Sí| Validate["Valida formato de email<br/>y unicidad (Domain: User.Email)"]
    Validate --> ValidOk{"¿Válido?"}
    ValidOk -->|No| E400a["400 Bad Request"]
    ValidOk -->|Sí| SaveProfile["Se guarda y se devuelve<br/>el perfil actualizado"]

    Choice -->|Cambiar contraseña| Pwd["Introduce contraseña actual,<br/>nueva contraseña y confirmación"]
    Pwd --> ConfirmCheck{"¿Nueva == Confirmación?"}
    ConfirmCheck -->|No| E400b["400 Bad Request<br/>(en el propio formulario, sin llamar a la Api)"]
    ConfirmCheck -->|Sí| PutPwd["PUT /api/users/{id}/password<br/>(ChangePasswordCommand)"]
    PutPwd --> CurrentCheck{"¿Contraseña actual correcta?<br/>(BCrypt.Verify)"}
    CurrentCheck -->|No| E401["401 Unauthorized"]
    CurrentCheck -->|Sí| Rehash["Se recalcula el hash BCrypt<br/>de la nueva contraseña"]
    Rehash --> NewTokens["Se emiten nuevos<br/>access_token / refresh_token"]
```

## Explicación del flujo

`UsersController` (`[Authorize]`) expone tres endpoints bajo `/api/users/{id}/...`, y los tres comparten la misma comprobación de propiedad: el `id` de la ruta debe coincidir con el `UserId` extraído del JWT del solicitante (`ClaimTypes.NameIdentifier`). Un socio nunca puede consultar ni modificar el perfil de otro socio por esta vía — esa capacidad queda reservada al rol `Administrator` (ver [`administracion-usuarios.md`](administracion-usuarios.md)).

- **`GET /api/users/{id}/profile`** (`GetUserProfileQuery`) devuelve los datos editables del perfil: nombre, género, email, número y categoría de licencia federativa.
- **`PUT /api/users/{id}/profile`** (`UpdateProfileCommand`) actualiza esos mismos campos. `User.Email`, al ser una propiedad de la entidad de dominio con validación en su propio *setter* (ver [modelo de dominio](../architecture/architecture.md#7-modelo-de-dominio)), rechaza cualquier formato de email inválido antes de llegar a persistirse, independientemente de si la validación de `FluentValidation` en `Application` ya lo había capturado antes.
- **`PUT /api/users/{id}/password`** (`ChangePasswordCommand`) exige la contraseña actual (verificada con `BCrypt.Net-Next` contra el hash almacenado) antes de aceptar la nueva; la comprobación de que "nueva contraseña" y "confirmación" coinciden se hace en el propio formulario Blazor antes de llamar a la Api, evitando una petición innecesaria si ya se sabe que fallará. Un cambio de contraseña exitoso invalida implícitamente la sesión anterior: se emite un nuevo `access_token`/`refresh_token`, igual que en un login normal.

Ninguno de estos tres flujos pasa por `AdminUser:Password` ni por ningún privilegio elevado — son operaciones de autoservicio, coherentes con el objetivo del proyecto de que el socio gestione sus propios datos sin depender del secretario del club.
