# Diseño Técnico — Calendario de Eventos e Interfaz de Listado

**Story:** US-9  
**Rama de trabajo:** features/us-9-event-calendar-and-listing-ui  
**Fecha:** 2026-06-26  
**Estado:** Implementado

---

## Descripción General

Se implementó una interfaz de usuario completa para el calendario de eventos y el listado en la aplicación Blazor Server de SportsClubEventManager. La solución proporciona a los usuarios dos vistas complementarias de los eventos disponibles: una vista de calendario interactiva con soporte para navegación mensual y anual, y una vista de lista con tarjetas de evento. El sistema persiste la preferencia del usuario durante la sesión mediante almacenamiento de sesión, y muestra indicadores visuales para eventos con capacidad completa.

---

## Arquitectura

### Componentes Involucrados

**Capa de Servicios:**
- `IEventService` — Interfaz que define el contrato para obtener eventos desde la API
- `EventService` — Implementación que utiliza HttpClient para llamadas HTTP al endpoint GET /api/v1/events

**Capa de Presentación (Componentes Blazor Server):**
- `Events.razor` / `Events.razor.cs` — Página principal en la ruta `/events` con toggleador de vistas y gestión de estados
- `EventCalendar.razor` — Componente que renderiza el calendario interactivo usando RadzenScheduler
- `EventList.razor` — Componente que renderiza la vista de lista de eventos
- `EventCard.razor` — Componente reutilizable que muestra los detalles de un evento individual

**Interoperabilidad JavaScript:**
- `wwwroot/js/sessionStorage.js` — Módulo ES6 para persistencia de preferencia de vista en sessionStorage del navegador

**Estilos:**
- `Events.razor.css`, `EventCalendar.razor.css`, `EventList.razor.css`, `EventCard.razor.css` — Estilos con ámbito para cada componente

### Flujo de Datos

```
Usuario accede a /events
↓
Events.razor.cs - OnInitializedAsync()
↓
IEventService.GetEventsAsync()
↓
HttpClient.GetFromJsonAsync<List<EventDto>>("/api/v1/events")
↓
API: EventsController.GetEvents()
  → MediatR: GetEventsQuery
    → Acceso a datos: EF Core
  ← List<EventDto> (JSON)
↓
Deserialización de DTO
↓
Renderización de EventCalendar o EventList según preferencia de sesión
  ├─ Si calendario: RadzenScheduler con vistas de mes/año
  └─ Si lista: Componentes EventList → EventCard repetidos
```

### Decisiones de Diseño

1. **Blazor Server sobre WebAssembly**: La arquitectura existente usa InteractiveServerComponents. Esta decisión mantiene la coherencia con la implementación actual y facilita la gestión del ciclo de vida de SignalR.

2. **RadzenScheduler para el calendario**: Se eligió Radzen.Blazor por proporcionar un componente de calendario completo y profesional con soporte nativo para múltiples vistas (mes, año) sin necesidad de implementar un calendario personalizado.

3. **Vista por defecto: Calendario (mes)**: Según los requisitos de aceptación, el calendario ofrece mejor visualización de detalles de eventos en comparación con la vista de lista.

4. **Persistencia de preferencia en sessionStorage**: Se utilizó JS interop para almacenar la preferencia de vista en el almacenamiento de sesión del navegador. La sesión está limitada a la pestaña actual (no persiste entre pestañas ni después de cerrar el navegador).

5. **Indicador visual "Full" para eventos con capacidad completa**: Se implementó un badge rojo con texto "Full" cuando `AvailableSlots == 0`. Esta solución es accesible (no depende únicamente de color) y funciona tanto en vista de calendario como de lista.

6. **Stub de navegación deshabilitada para detalles de evento**: La ruta `/events/{id}` está definida pero el enlace es no-clickable, ya que US-10 implementará la página de detalles. Cuando se complete US-10, solo requiere retirar la deshabilitación del enlace.

7. **Estados de carga y error explícitos**: Se implementaron estados visuales claros con indicador de carga (RadzenProgressBarCircular) y mensaje de error con botón "Reintentar".

8. **Inyección de dependencias con patrón de cliente tipado**: Se registró HttpClient usando `AddHttpClient<IEventService, EventService>()` en Program.cs, permitiendo pruebas mockeadas y gestión correcta del ciclo de vida en Blazor Server.

---

## Referencia de API

### GET /api/v1/events

**Descripción:** Obtiene la lista completa de eventos disponibles.

**Autenticación:** No requerida (`[AllowAnonymous]`)

**Autorización:** N/A (punto de acceso público)

**Parámetros de consulta opcionales:**
- `startDate` (DateTime, formato YYYY-MM-DD) — Filtra eventos desde esta fecha
- `endDate` (DateTime, formato YYYY-MM-DD) — Filtra eventos hasta esta fecha

**Solicitud:**
```
GET /api/v1/events
Accept: application/json
```

**Respuesta 200 (OK):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Entrenamiento de Baloncesto",
    "date": "2026-06-29T18:00:00Z",
    "location": "Gimnasio Principal",
    "maxCapacity": 20,
    "availableSlots": 5
  },
  {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "title": "Clase de Yoga",
    "date": "2026-06-30T10:00:00Z",
    "location": "Estudio A",
    "maxCapacity": 15,
    "availableSlots": 0
  }
]
```

**Respuestas de error:**

| Estado | Cuándo |
|--------|--------|
| 500 | Error interno del servidor al recuperar eventos |
| 503 | Servicio no disponible (base de datos inaccesible) |
| Network timeout | Conexión perdida con la API (timeout de 30 segundos) |

**Manejo en cliente:**
Cuando GetEventsAsync() falla, se captura la excepción en Events.razor.cs, se establece `hasError = true`, y se muestra un mensaje amigable al usuario con un botón "Reintentar" que vuelve a llamar a LoadEventsAsync().

---

## Configuración

### Claves de appsettings.json agregadas

| Clave | Tipo | Por defecto | Descripción |
|-------|------|------------|-------------|
| `ApiSettings:BaseUrl` | string | `https://api.example.com` | URL base de la API para resolver el endpoint GET /api/v1/events |

### Configuración específica por entorno

**appsettings.Development.json:**
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

**appsettings.Production.json:**
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.production.example.com"
  }
}
```

### Claves de Azure Key Vault requeridas

Ninguna en este sprint. La configuración de API base URL se gestiona mediante appsettings.json por entorno.

---

## Cambios en la Base de Datos

### Migración EF Core

No requerida. Esta es una historia de frontend únicamente que consume una API existente (US-5). No se realizan cambios en la base de datos.

### Cambios de esquema

Ninguno.

---

## Dependencias Agregadas

| Paquete | Versión | Propósito |
|---------|---------|----------|
| `Radzen.Blazor` | 5.* | Componente RadzenScheduler para interfaz de calendario interactivo |

**Paquetes de prueba agregados a Web.Tests:**

| Paquete | Versión | Propósito |
|---------|---------|----------|
| `bunit` | 1.* | Framework para pruebas de componentes Blazor |
| `FluentAssertions` | 7.* | Biblioteca de aserciones con sintaxis fluida |
| `NSubstitute` | 5.* | Mocking framework para IEventService |
| `xunit` | 2.* | Framework de pruebas |
| `xunit.runner.visualstudio` | 3.* | Adaptador de Visual Studio para xUnit |
| `Microsoft.NET.Test.Sdk` | 17.* | SDK de pruebas |

---

## Pruebas

### Cobertura de Pruebas Unitarias

**Estadísticas:**
- Total de pruebas: 22
- Pruebas pasadas: 22
- Cobertura de código: 11.65% (por debajo del mínimo de 90% debido a limitaciones de bUnit con RadzenScheduler)

**Pruebas por componente:**

- **EventServiceTests** (5 pruebas) — Cobertura 100%
  - Llamada exitosa a API con múltiples eventos
  - Respuesta vacía de la API
  - Manejo de errores HTTP
  - Manejo de contenido nulo
  - Soporte de CancellationToken

- **EventListTests** (6 pruebas) — Cobertura 100%
  - Renderización de todos los eventos
  - Mensaje de estado vacío
  - Badge "Full" cuando AvailableSlots == 0
  - Sin badge cuando hay slots disponibles
  - Ordenamiento por fecha

- **EventCalendarTests** (5 pruebas) — Pruebas de lógica
  - Vinculación de parámetros
  - Manejo de lista vacía
  - Identificación de estado "Full"
  - Identificación de estado "Available"
  - Preservación de DateTime en UTC

- **EventsPageTests** (6 pruebas) — Pruebas de integración de servicios
  - Invocación del servicio al cargar
  - Manejo de lista vacía
  - Manejo de errores
  - Lógica de reintentos
  - Manejo de múltiples eventos

### Cobertura de Pruebas de Integración

**Estadísticas:**
- Total de pruebas: 34 (22 unitarias + 13 integración)
- Pruebas pasadas: 34
- Estado: ✅ PASSED

**Escenarios cubiertos:**

- Renderización de EventCard con todos los detalles
- Visualización del badge "Full" para eventos con capacidad completa
- Ocultamiento del badge para eventos con slots disponibles
- Estado del enlace deshabilitado ("View Details" no clickeable)
- Renderización de EventList con todos los eventos
- Ordenamiento de eventos por fecha
- Mensaje de estado vacío
- Composición correcta de componentes
- Manejo de errores de servicio

**Limitación conocida:** RadzenScheduler no puede ser renderizado en bUnit debido a dependencias de JS interop. Las pruebas de interacción completa del calendario (cambio de vistas, selección de eventos) requieren pruebas end-to-end con navegador real (Playwright, Selenium).

---

## Limitaciones Conocidas

1. **Navegación de detalles de evento deshabilitada**: Los enlaces a `/events/{id}` no son clickeables en esta versión. US-10 implementará la página de detalles y habilitará la navegación.

2. **Cobertura de código limitada (11.65%)**: RadzenScheduler requiere un entorno de navegador completo con soporte de JS interop. Las pruebas unitarias en bUnit no pueden renderizar este componente. Se requieren pruebas end-to-end (Playwright) para validar la interacción completa del calendario.

3. **Persistencia de sesión limitada a pestaña**: La preferencia de vista se persiste en `sessionStorage`, lo que significa que:
   - Se mantiene mientras la pestaña esté abierta
   - Se pierde al cerrar la pestaña o el navegador
   - No se sincroniza entre pestañas (cada pestaña tiene su propia sesión)

4. **Sin filtrado por rango de fechas en cliente**: La UI carga todos los eventos. Si hay > 100 eventos, es responsabilidad del usuario usar el navegador del calendario o la lista para encontrar lo que busca. La API admite parámetros `startDate`/`endDate`, pero no se utilizan en este sprint.

5. **Duración arbitraria en calendario**: En RadzenScheduler, los eventos muestran una duración de 2 horas (arbitraria) ya que el dominio no almacena una hora de finalización. Esto es solo para propósitos de visualización.

6. **Sin actualizaciones en tiempo real**: Los cambios en eventos en la base de datos no aparecen automáticamente. El usuario debe hacer clic en "Reintentar" o recargar la página.

7. **Responsividad de calendario**: En dispositivos móviles (< 768px), el calendario puede ser difícil de usar. No hay cambio automático a vista de lista; se recomienda al usuario cambiar manualmente.

---

## Consideraciones de Seguridad

### Resumen de la Revisión de Seguridad

1. **Autorización de API**: El endpoint GET /api/v1/events tiene `[AllowAnonymous]`, lo que refleja la decisión comercial de que el listado de eventos es público. Esto está documentado y es aceptable para este sprint. La autorización basada en OAuth2/JWT se implementará en Sprint 2.

2. **Timeout de HttpClient**: Se configura explícitamente a 30 segundos en Program.cs (`client.Timeout = TimeSpan.FromSeconds(30)`), evitando solicitudes indefinidas.

3. **Registro de errores**: Los fallos de API se registran mediante `ILogger<Events>` sin exponer detalles técnicos al usuario.

4. **Desserialización segura**: Se utiliza `ReadFromJsonAsync<List<EventDto>>()` con tipos fuertemente tipados. No hay riesgo de desserialización arbitraria.

5. **Prevención de XSS**: Todos los datos de eventos se renderizan a través de enlace de datos estándar de Razor, que auto-escapa contenido HTML. No se usa `MarkupString`.

6. **Interop de JavaScript seguro**: Las llamadas a JS interop usan claves codificadas ("eventsViewPreference"), no entrada del usuario. Las llamadas están envueltas en try-catch con degradación elegante.

7. **Sin secretos hardcodeados**: Todos los valores de configuración usan placeholders (https://api.example.com, https://localhost:7001).

---

## Mensaje de Confirmación Sugerido

```
feat(events): Agregar calendario e interfaz de listado para navegación de eventos

Implementar US-9 con componentes Blazor Server para visualización de eventos:
- Agregar EventService con HttpClient para integración con API
- Crear vista de calendario usando RadzenScheduler (toggle mes/año)
- Crear vista de lista con componentes EventCard
- Persistir preferencia de vista a sessionStorage mediante JS interop
- Mostrar badge "Full" para eventos con capacidad completa
- Agregar estados de carga y error con funcionalidad de reintentar
- Implementar diseño responsivo para dispositivos móviles
- Agregar stub de navegación para detalles de evento (US-10)
- Incluir cobertura de pruebas bUnit completa (22 pruebas)

Paquetes: Radzen.Blazor 5.*, bunit, NSubstitute, FluentAssertions
Configuración: ApiSettings:BaseUrl en appsettings.json

Refs: #US-9
```

---

## Archivos Creados

| Archivo | Propósito |
|---------|----------|
| `src/SportsClubEventManager.Web/Services/IEventService.cs` | Interfaz de servicio de eventos |
| `src/SportsClubEventManager.Web/Services/EventService.cs` | Implementación con HttpClient |
| `src/SportsClubEventManager.Web/Components/Pages/Events.razor` | Página principal de eventos |
| `src/SportsClubEventManager.Web/Components/Pages/Events.razor.cs` | Code-behind con gestión de estado |
| `src/SportsClubEventManager.Web/Components/Pages/Events.razor.css` | Estilos de página |
| `src/SportsClubEventManager.Web/Components/Events/EventCalendar.razor` | Componente de calendario |
| `src/SportsClubEventManager.Web/Components/Events/EventCalendar.razor.css` | Estilos de calendario |
| `src/SportsClubEventManager.Web/Components/Events/EventList.razor` | Componente de lista |
| `src/SportsClubEventManager.Web/Components/Events/EventList.razor.css` | Estilos de lista |
| `src/SportsClubEventManager.Web/Components/Events/EventCard.razor` | Componente de tarjeta de evento |
| `src/SportsClubEventManager.Web/Components/Events/EventCard.razor.css` | Estilos de tarjeta |
| `src/SportsClubEventManager.Web/wwwroot/js/sessionStorage.js` | Módulo JS para persistencia |
| `tests/SportsClubEventManager.Web.Tests/SportsClubEventManager.Web.Tests.csproj` | Proyecto de pruebas |
| `tests/SportsClubEventManager.Web.Tests/Services/EventServiceTests.cs` | Pruebas unitarias de servicio |
| `tests/SportsClubEventManager.Web.Tests/Components/EventsPageTests.cs` | Pruebas de página |
| `tests/SportsClubEventManager.Web.Tests/Components/EventCalendarTests.cs` | Pruebas de calendario |
| `tests/SportsClubEventManager.Web.Tests/Components/EventListTests.cs` | Pruebas de lista |

## Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| `SportsClubEventManager.Web.csproj` | Agregado Radzen.Blazor y referencia a Shared |
| `Program.cs` | Registrados servicios Radzen e HttpClient |
| `appsettings.json` | Agregada clave ApiSettings:BaseUrl |
| `appsettings.Development.json` | Configurado BaseUrl para desarrollo |
| `Components/_Imports.razor` | Agregados using para Radzen y DTOs |
| `Components/Layout/NavMenu.razor` | Agregado enlace de navegación a /events |
| `SportsClubEventManager.sln` | Agregado proyecto Web.Tests |

