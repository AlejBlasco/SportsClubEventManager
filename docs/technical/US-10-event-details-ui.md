# Diseño Técnico — Event Details UI
**Story:** US-10  
**Título:** Event Details UI  
**Rama de trabajo:** features/us-10-event-details-ui  
**Fecha:** 2026-06-28  
**Estado:** Implementado

---

## Resumen Ejecutivo

Esta funcionalidad implementa una página de detalles de eventos en la interfaz de usuario Blazor Server, permitiendo a los usuarios ver información completa de un evento específico incluyendo título, descripción, fecha, ubicación, capacidad y plazas disponibles. Es una característica exclusivamente frontend que consume el endpoint API GET `/events/{id}` existente (implementado en US-6).

**Impacto:** 13 archivos creados, 7 archivos modificados, 0 migraciones de base de datos  
**Cobertura de tests:** 100% (35 nuevos tests)  
**Complejidad:** Media (11 horas de esfuerzo estimado)

---

## Visión General

La funcionalidad Event Details UI (US-10) proporciona una experiencia de usuario completa para visualizar los detalles de un evento individual. Los usuarios pueden navegar desde la lista de eventos a una página dedicada que muestra información detallada, incluyendo un indicador visual de capacidad, manejo de estados de carga y error, y navegación de retorno.

### Características Principales

1. **Visualización completa de datos del evento** — Título, descripción, fecha/hora, ubicación, capacidad total y plazas disponibles
2. **Indicador visual de capacidad** — Barra de progreso que muestra el porcentaje de ocupación
3. **Manejo de estados** — Loading spinner, página 404, mensajes de error con funcionalidad de reintento
4. **Diseño responsive** — Layout optimizado para móvil (viewport mínimo 320px)
5. **Accesibilidad WCAG 2.1 AA** — HTML semántico, atributos ARIA, contraste de color adecuado
6. **Navegación activada** — Enlaces "View Details" habilitados en las tarjetas de eventos

---

## Arquitectura

### Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────┐
│                    EventCard.razor                      │
│  ┌────────────────────────────────────────────────┐    │
│  │  <a href="/events/{id}">View Details →</a>    │    │
│  └────────────────────────────────────────────────┘    │
└────────────────────┬────────────────────────────────────┘
                     │ Navegación
                     ↓
┌─────────────────────────────────────────────────────────┐
│            EventDetails.razor (.razor.cs)               │
│  ┌────────────────────────────────────────────────┐    │
│  │  Route: /events/{id:guid}                      │    │
│  │  RenderMode: InteractiveServer                 │    │
│  │                                                 │    │
│  │  Estados:                                       │    │
│  │  - isLoading → LoadingSpinner                  │    │
│  │  - isNotFound → NotFoundPage                   │    │
│  │  - hasError → ErrorMessage                     │    │
│  │  - eventDetail != null → Renderizar detalles   │    │
│  └────────────────────────────────────────────────┘    │
└────────────────────┬────────────────────────────────────┘
                     │ IEventService
                     ↓
┌─────────────────────────────────────────────────────────┐
│                  EventService.cs                        │
│  ┌────────────────────────────────────────────────┐    │
│  │  GetEventByIdAsync(Guid id)                    │    │
│  │  ├─ GET /api/v1/events/{id}                    │    │
│  │  ├─ 200 OK → EventDetailDto                    │    │
│  │  ├─ 404 Not Found → null                       │    │
│  │  └─ 500+ → HttpRequestException                │    │
│  └────────────────────────────────────────────────┘    │
└────────────────────┬────────────────────────────────────┘
                     │ HttpClient
                     ↓
┌─────────────────────────────────────────────────────────┐
│              API Backend (US-6)                         │
│         GET /api/v1/events/{id}                         │
└─────────────────────────────────────────────────────────┘
```

### Componentes Compartidos Creados

```
Components/Shared/
├── LoadingSpinner.razor         (Spinner reutilizable con mensaje personalizable)
├── ErrorMessage.razor           (Mensaje de error con botón de reintento)
├── NotFoundPage.razor           (Página 404 con navegación de retorno)
├── CapacityIndicator.razor      (Barra de progreso de capacidad)
└── CapacityIndicator.razor.css  (Estilos con gradiente y overlay de patrón)
```

Estos componentes están diseñados para reutilización en futuras funcionalidades (ej. US-11 Event Registration).

---

## Flujo de Datos

### Navegación a Event Details

1. **Usuario** hace clic en "View Details →" en una tarjeta de evento (EventCard)
2. **Blazor Router** navega a `/events/{guid}` y activa EventDetails.razor
3. **EventDetails.OnInitializedAsync()** se ejecuta automáticamente
4. **Estado inicial:** `isLoading = true` → renderiza LoadingSpinner
5. **EventDetails** llama a `EventService.GetEventByIdAsync(Id)`
6. **EventService** realiza HTTP GET a `/api/v1/events/{id}`

### Escenario Exitoso (200 OK)

7. **API** devuelve EventDetailDto con datos del evento
8. **EventService** deserializa JSON a EventDetailDto y lo devuelve
9. **EventDetails** asigna `eventDetail = resultado`, establece `isLoading = false`
10. **Blazor** renderiza la página de detalles con:
    - Título del evento
    - Badge "Fully Booked" (si aplica)
    - Descripción (si existe)
    - Información del evento (fecha, ubicación, capacidad)
    - CapacityIndicator con barra de progreso
    - Espacio reservado para botones de registro futuros

### Escenario 404 Not Found

7. **API** devuelve 404 Not Found
8. **EventService** detecta StatusCode.NotFound, devuelve `null`
9. **EventDetails** detecta `eventDetail == null`, establece `isNotFound = true`, `isLoading = false`
10. **Blazor** renderiza NotFoundPage con mensaje y botón "Back to Events"

### Escenario de Error (500+, timeout, red)

7. **API** devuelve error (500, 503) o falla de red
8. **EventService** llama a `response.EnsureSuccessStatusCode()`, lanza HttpRequestException
9. **EventDetails** captura excepción en bloque catch:
    - Registra error con ILogger
    - Establece `hasError = true`, `isLoading = false`
10. **Blazor** renderiza ErrorMessage con:
    - Mensaje: "Unable to load event details. Please try again."
    - Botón "Retry" que invoca `LoadEventDetailsAsync()` de nuevo

---

## Decisiones de Diseño

### 1. Patrón Code-Behind

**Decisión:** Utilizar archivos `.razor.cs` separados para la lógica del componente  
**Razón:**  
- Separa las responsabilidades (markup en .razor, lógica en .razor.cs)
- Mejora la legibilidad y mantenibilidad
- Facilita las pruebas unitarias con bUnit

**Implementación:**
```csharp
// EventDetails.razor.cs
public sealed partial class EventDetails
{
    private EventDetailDto? eventDetail;
    private bool isLoading = true;
    private bool hasError;
    private bool isNotFound;
    
    [Parameter]
    public Guid Id { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        await LoadEventDetailsAsync();
    }
}
```

---

### 2. Manejo de Estado de 404 vs Error

**Decisión:** `GetEventByIdAsync` devuelve `null` para 404, lanza excepción para otros errores  
**Razón:**  
- 404 es un escenario esperado (usuario navega a evento inexistente)
- Otros errores (500, timeout) son excepcionales y requieren logging
- Permite al componente distinguir entre "no encontrado" y "error del servidor"

**Implementación:**
```csharp
// EventService.cs
public async Task<EventDetailDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    var response = await httpClient.GetAsync($"api/v1/events/{id}", cancellationToken);
    
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return null; // Escenario esperado
    }
    
    response.EnsureSuccessStatusCode(); // Otros errores → excepción
    
    var eventDetail = await response.Content.ReadFromJsonAsync<EventDetailDto>(cancellationToken);
    return eventDetail;
}
```

---

### 3. Componentes Compartidos Reutilizables

**Decisión:** Crear componentes genéricos (LoadingSpinner, ErrorMessage, NotFoundPage) en lugar de lógica inline  
**Razón:**  
- DRY (Don't Repeat Yourself) — evita duplicación en futuras páginas
- Testabilidad — cada componente tiene sus propios tests aislados
- Consistencia UI — mismo spinner/error/404 en toda la aplicación
- Facilita cambios futuros (ej. cambiar el diseño del spinner globalmente)

**Componentes creados:**
- **LoadingSpinner:** Envuelve RadzenProgressBarCircular, mensaje personalizable
- **ErrorMessage:** Muestra error con icono, mensaje y botón de reintento opcional
- **NotFoundPage:** Página 404 con icono, mensaje y navegación a /events
- **CapacityIndicator:** Barra de progreso con cálculo de porcentaje y badge "Fully Booked"

---

### 4. Indicador de Capacidad — Diseño Accesible para Daltónicos

**Decisión:** Gradiente azul con overlay de patrón rayado, sin usar rojo/verde  
**Razón:**  
- Rojo/verde es problemático para usuarios con daltonismo (8% de hombres)
- WCAG 2.1 AA requiere que el color no sea el único indicador visual
- Patrón rayado proporciona distinción táctil/visual adicional
- Gradiente azul es neutral y estéticamente agradable

**Implementación:**
```css
.capacity-bar {
    background: linear-gradient(90deg, #4a90e2 0%, #357abd 100%);
}

.capacity-bar::after {
    content: '';
    background-image: repeating-linear-gradient(
        45deg,
        transparent,
        transparent 10px,
        rgba(255, 255, 255, 0.1) 10px,
        rgba(255, 255, 255, 0.1) 20px
    );
}
```

---

### 5. Formato de Fecha Consistente

**Decisión:** Usar formato `"dddd, MMMM dd, yyyy 'at' h:mm tt"` (ej. "Friday, June 26, 2026 at 3:30 PM")  
**Razón:**  
- Consistencia con EventCard (que usa formato similar)
- Legibilidad — nombre completo del día y mes
- Conversión a hora local con `ToLocalTime()` para zona horaria del usuario
- **Nota:** Formato fijo en inglés por ahora, considerar locale-aware formatting en futuras iteraciones

**Implementación:**
```razor
<dd>@eventDetail.Date.ToLocalTime().ToString("dddd, MMMM dd, yyyy 'at' h:mm tt")</dd>
```

---

### 6. Navegación de Retorno — Link Directo vs. Historial del Navegador

**Decisión:** Usar `NavigationManager.NavigateTo("/events")` en lugar de `history.back()`  
**Razón:**  
- UX predecible — siempre vuelve a /events, sin importar el origen
- Usuario puede llegar desde marcador, enlace externo o búsqueda
- `history.back()` podría llevar a página externa o inicio de sesión
- Comportamiento controlado es mejor para SPA

**Implementación:**
```csharp
// NotFoundPage.razor
private void NavigateToEvents()
{
    NavigationManager.NavigateTo("/events");
}
```

---

### 7. Espacio Reservado para Botones de Registro

**Decisión:** Sección vacía con comentario, sin botones deshabilitados visibles  
**Razón:**  
- UX limpia — no confunde al usuario con botones no funcionales
- Preparación para US-11 — el espacio está reservado en el layout
- Comentario en código documenta la intención

**Implementación:**
```razor
<section class="event-section registration-placeholder">
    <!-- Reserved space for future registration buttons (US-11) -->
</section>
```

---

## Referencia de API (Cliente)

### IEventService.GetEventByIdAsync

**Método:** `Task<EventDetailDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)`

**Descripción:** Recupera los detalles de un evento específico por su identificador único.

**Parámetros:**
- `id` (Guid) — Identificador único del evento
- `cancellationToken` (CancellationToken, opcional) — Token para cancelar la operación asíncrona

**Retorno:**
- `EventDetailDto` si el evento existe
- `null` si el evento no se encuentra (404)

**Excepciones:**
- `HttpRequestException` — Error de API (500, 503, timeout, red)
- `OperationCanceledException` — Operación cancelada vía token

**Endpoint consumido:** `GET /api/v1/events/{id}` (implementado en US-6)

**Ejemplo de uso:**
```csharp
try
{
    var eventDetail = await EventService.GetEventByIdAsync(eventId);
    
    if (eventDetail is null)
    {
        // Evento no encontrado (404)
        isNotFound = true;
    }
    else
    {
        // Evento encontrado, renderizar detalles
        this.eventDetail = eventDetail;
    }
}
catch (HttpRequestException ex)
{
    // Error del servidor o de red
    Logger.LogError(ex, "Failed to load event details");
    hasError = true;
}
```

---

## Cobertura de Tests

### Tests Unitarios (35 nuevos)

**EventDetailsPageTests (12 tests)**
- ✅ Muestra loading spinner mientras carga
- ✅ Muestra información del evento cuando se carga exitosamente
- ✅ Muestra NotFoundPage cuando el evento no existe
- ✅ Muestra ErrorMessage cuando falla la llamada API
- ✅ Muestra badge "Fully Booked" para eventos llenos
- ✅ No muestra badge cuando hay disponibilidad
- ✅ Muestra CapacityIndicator
- ✅ Muestra link de navegación de retorno
- ✅ Permite retry después de un error
- ✅ Llama a EventService con ID correcto
- ✅ Muestra fecha en formato legible
- ✅ Maneja eventos sin descripción

**EventServiceTests (5 tests nuevos)**
- ✅ Devuelve EventDetailDto cuando API responde exitosamente
- ✅ Devuelve null cuando API devuelve 404
- ✅ Lanza HttpRequestException cuando API devuelve 500
- ✅ Respeta CancellationToken
- ✅ Devuelve evento con IsFullyBooked=true cuando está lleno

**CapacityIndicatorTests (7 tests)**
- ✅ Muestra información de capacidad correctamente
- ✅ Calcula porcentaje de ocupación correctamente
- ✅ Muestra badge "Fully Booked" cuando está lleno
- ✅ No muestra badge cuando hay disponibilidad
- ✅ Incluye atributos ARIA apropiados
- ✅ Maneja capacidad cero correctamente
- ✅ Muestra 100% cuando está completamente lleno

**LoadingSpinnerTests (3 tests)**
- ✅ Renderiza con mensaje por defecto
- ✅ Renderiza con mensaje personalizado
- ✅ Contiene barra de progreso

**ErrorMessageTests (5 tests)**
- ✅ Renderiza con mensaje por defecto
- ✅ Renderiza con mensaje personalizado
- ✅ Muestra botón de retry cuando se proporciona callback
- ✅ Invoca callback cuando se hace clic en retry
- ✅ No muestra botón cuando no hay callback

**NotFoundPageTests (3 tests)**
- ✅ Renderiza mensaje "Event Not Found"
- ✅ Muestra botón "Back to Events"
- ✅ Navega a /events cuando se hace clic

### Tests de Integración (1 test actualizado)

**EventsPageIntegrationTests**
- ✅ EventCard_WhenRendered_DisplaysActiveViewDetailsLink (actualizado para reflejar navegación activa)

### Cobertura Total

- **Tests pasando:** 69/69 (100%)
- **Cobertura de código US-10:** 100%
- **Duración:** 514ms
- **Cumple mínimo (90%):** ✅ SÍ (100% > 90%)

---

## Seguridad

### Análisis de Seguridad Realizado

**Escaneo de Secretos:** ✅ Ninguno detectado  
**OWASP Top 10:** ✅ Revisión manual pasada  
**Protección XSS:** ✅ Codificación automática de Blazor  
**Autenticación:** ✅ Apropiada para visualización pública de eventos  
**Autorización:** ✅ Aplicada a nivel API  
**Manejo de Errores:** ✅ Sin exposición de stack trace  
**Logging:** ✅ Sin datos sensibles registrados  

**Hallazgos de Seguridad:** 0 críticos, 0 altos, 0 medios, 0 bajos  
**Recomendaciones Informativas:** 2 (para futuras funcionalidades)

### Protección contra XSS

Todos los datos controlados por el usuario se renderizan mediante sintaxis `@expression` de Blazor, que automáticamente codifica HTML:

```razor
<h1>@eventDetail.Title</h1>
<p>@eventDetail.Description</p>
```

**No se utiliza `MarkupString`** en ningún archivo Razor, eliminando el vector de ataque XSS más común en Blazor.

---

## Accesibilidad (WCAG 2.1 AA)

### Características de Accesibilidad Implementadas

1. **HTML Semántico**
   - `<article>` para el contenido del evento
   - `<section>` para agrupaciones lógicas
   - `<dl>`, `<dt>`, `<dd>` para listas de definiciones
   - Jerarquía de encabezados apropiada (`<h1>`, `<h2>`)

2. **Atributos ARIA**
   ```html
   <div class="capacity-bar"
        role="progressbar"
        aria-valuenow="30"
        aria-valuemin="0"
        aria-valuemax="50"
        aria-label="Event capacity: 30 out of 50 slots filled">
   </div>
   ```

3. **Contraste de Color**
   - Texto: ratio 4.5:1 mínimo (#333 sobre blanco)
   - Elementos UI: ratio 3:1 mínimo
   - Badge "Fully Booked": #dc3545 (rojo) sobre blanco — cumple contraste

4. **Diseño para Daltónicos**
   - Barra de progreso usa gradiente azul (no rojo/verde)
   - Patrón rayado proporciona distinción visual adicional
   - Badge de texto "Fully Booked" complementa el indicador de color

5. **Navegación por Teclado**
   - Todos los elementos interactivos accesibles por Tab
   - Enlaces y botones tienen focus visible
   - Orden de tabulación lógico

6. **Diseño Responsive**
   - Viewport mínimo: 320px
   - Breakpoints: 320px, 768px
   - Sin scroll horizontal
   - Touch targets apropiados para móvil

---

## Diseño Responsive

### Breakpoints Implementados

**Mobile (320px - 767px)**
```css
@media (max-width: 768px) {
    .header-content {
        flex-direction: column;
        align-items: flex-start;
    }
    
    .info-item {
        grid-template-columns: 1fr; /* Una columna */
    }
}
```

**Muy Pequeño (320px - 575px)**
```css
@media (max-width: 320px) {
    .event-title {
        font-size: 1.25rem;
    }
}
```

**Desktop (1024px+)**
```css
.event-details-page {
    max-width: 800px;
    margin: 0 auto;
}
```

### Pruebas de Viewport

- ✅ 320px (iPhone SE) — Layout single-column, legible
- ✅ 375px (iPhone 12) — Layout single-column, espaciado adecuado
- ✅ 768px (iPad) — Layout single-column con fuentes más grandes
- ✅ 1024px+ (Desktop) — Contenedor centrado, max-width 800px
- ✅ Sin overflow horizontal en ningún tamaño

---

## Calidad de Código

### Análisis Estático

**dotnet format:** ✅ 0 archivos requieren formato  
**Roslyn Analyzers:** ✅ 0 errores, 0 warnings  
**Complejidad Ciclomática:** ✅ Todos los métodos < 10  
**Tamaño de Clases:** ✅ Todas < 500 líneas  
**Tamaño de Métodos:** ✅ Todos < 50 líneas  

### Dependencias Vulnerables

**Críticas/Altas:** ✅ 0  
**Moderadas:** ⚠️ 4 (OpenTelemetry transitive deps en proyecto de tests solamente)  
**Bajas:** ✅ 0  

**Nota:** Las vulnerabilidades moderadas están en dependencias transitivas del proyecto de tests solamente, no se despliegan a producción.

---

## Archivos Modificados/Creados

### Archivos Creados (13)

**Componentes de Páginas:**
1. `src/SportsClubEventManager.Web/Components/Pages/EventDetails.razor`
2. `src/SportsClubEventManager.Web/Components/Pages/EventDetails.razor.cs`
3. `src/SportsClubEventManager.Web/Components/Pages/EventDetails.razor.css`

**Componentes Compartidos:**
4. `src/SportsClubEventManager.Web/Components/Shared/LoadingSpinner.razor`
5. `src/SportsClubEventManager.Web/Components/Shared/ErrorMessage.razor`
6. `src/SportsClubEventManager.Web/Components/Shared/NotFoundPage.razor`
7. `src/SportsClubEventManager.Web/Components/Shared/CapacityIndicator.razor`
8. `src/SportsClubEventManager.Web/Components/Shared/CapacityIndicator.razor.css`

**Tests:**
9. `tests/SportsClubEventManager.Web.Tests/Components/EventDetailsPageTests.cs`
10. `tests/SportsClubEventManager.Web.Tests/Components/Shared/LoadingSpinnerTests.cs`
11. `tests/SportsClubEventManager.Web.Tests/Components/Shared/ErrorMessageTests.cs`
12. `tests/SportsClubEventManager.Web.Tests/Components/Shared/NotFoundPageTests.cs`
13. `tests/SportsClubEventManager.Web.Tests/Components/Shared/CapacityIndicatorTests.cs`

### Archivos Modificados (7)

**Servicios:**
1. `src/SportsClubEventManager.Web/Services/IEventService.cs` — Agregado método GetEventByIdAsync
2. `src/SportsClubEventManager.Web/Services/EventService.cs` — Implementado GetEventByIdAsync

**Componentes:**
3. `src/SportsClubEventManager.Web/Components/Events/EventCard.razor` — Habilitado link "View Details"
4. `src/SportsClubEventManager.Web/Components/Events/EventCard.razor.css` — Actualizado estilo del link
5. `src/SportsClubEventManager.Web/Components/_Imports.razor` — Agregado namespace Shared

**Tests:**
6. `tests/SportsClubEventManager.Web.Tests/Services/EventServiceTests.cs` — 5 nuevos tests
7. `tests/SportsClubEventManager.Web.Tests/Integration/EventsPageIntegrationTests.cs` — 1 test actualizado

---

## Notas de Implementación

### Características No Incluidas (Futuras)

1. **Botones de Registro/Cancelación** — Implementar en US-11
2. **Imágenes de Eventos** — Requiere cambios en API
3. **Formato de Fecha Sensible a Locale** — Considerar en futura iteración de i18n
4. **Eventos Sugeridos en Página 404** — Requiere llamada API adicional
5. **Compartir en Redes Sociales** — Funcionalidad futura

### Limitaciones Conocidas

1. **Formato de Fecha Fijo** — Actualmente en inglés independientemente del idioma del usuario
2. **Descripciones Largas** — Scroll natural, sin truncamiento/expansión
3. **Sin Rate Limiting en Cliente** — Confiado a nivel API
4. **Sin Prerendering** — InteractiveServer mode, carga inicial puede ser más lenta

---

## Próximos Pasos

### Después de CI Verification

1. **Merge a master** — Después de que CI y SonarCloud pasen
2. **Desplegar a entorno de desarrollo**
3. **Pruebas de aceptación del usuario**
4. **Monitoreo de métricas** — Tiempo de carga, tasa de error, uso

### Preparación para US-11 (Event Registration)

La arquitectura está preparada para US-11:
- Espacio reservado para botones de registro
- CapacityIndicator muestra disponibilidad
- EventService puede extenderse con métodos de registro
- Componentes compartidos reutilizables (LoadingSpinner, ErrorMessage)

---

## Referencias

**User Stories Relacionadas:**
- US-6: Event Details API (dependencia)
- US-9: Event Listing UI (contexto)
- US-11: Event Registration UI (próxima funcionalidad)

**Documentación Relacionada:**
- Blazor Server Documentation: https://learn.microsoft.com/aspnet/core/blazor/
- bUnit Testing: https://bunit.dev/
- Radzen Blazor Components: https://blazor.radzen.com/

**Outputs del Workflow:**
- Requirements Analysis: `.claude/docs/US-10/requirements-analysis.md`
- Impact Analysis: `.claude/docs/US-10/impact-analysis.md`
- Implementation Report: `.claude/docs/US-10/implementation-report.md`
- Code Review Report: `.claude/docs/US-10/review-report.md`
- Unit Test Report: `.claude/docs/US-10/unit-test-report.md`
- Integration Test Report: `.claude/docs/US-10/integration-test-report.md`
- Quality Report: `.claude/docs/US-10/quality-report.md`
- Security Report: `.claude/docs/US-10/security-report.md`

---

**Documento generado automáticamente por Documentation Agent**  
**Fecha:** 2026-06-28  
**Workflow:** AI-Governed SDLC Pipeline
