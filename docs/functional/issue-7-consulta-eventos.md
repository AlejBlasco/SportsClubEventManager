# Guía de Usuario — Consulta de Eventos
**Versión:** 2026-06-23  
**Se aplica a:** Sports Club Event Manager API

---

## ¿Qué es esta funcionalidad?

La API de Consulta de Eventos te permite obtener un listado de todos los eventos disponibles en el sistema de forma rápida y sencilla. Puedes ver información importante de cada evento como título, fecha, ubicación y cuántos espacios hay disponibles. Opcionalmente, puedes filtrar los eventos por un rango de fechas para encontrar exactamente lo que te interesa. Esta funcionalidad es el corazón del sistema de gestión de eventos y es lo primero que verás cuando accedas a la aplicación.

---

## Cómo usarlo

### Paso 1: Preparar la solicitud API

Si eres un desarrollador, consumidor de API o tester, necesitarás hacer una solicitud HTTP GET al siguiente endpoint:

```
GET https://localhost:5001/api/v1/events
```

Para usar esta API desde la línea de comandos, puedes usar `curl`:

```bash
curl -X GET "https://localhost:5001/api/v1/events" -H "Accept: application/json"
```

### Paso 2: Obtener todos los eventos (sin filtros)

Si quieres ver **todos los eventos** sin aplicar ningún filtro, simplemente llama al endpoint sin parámetros adicionales:

**Solicitud:**
```bash
curl -X GET "https://localhost:5001/api/v1/events" \
  -H "Accept: application/json"
```

**Respuesta esperada:**
```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "title": "Carabina de Aire - Entrenamiento",
    "date": "2026-01-15T09:00:00Z",
    "location": "Madrid - Real Federación",
    "maxCapacity": 20,
    "availableSlots": 18
  },
  {
    "id": "22222222-2222-2222-2222-222222222222",
    "title": "Carabina de Aire - Liga Local",
    "date": "2026-02-20T10:30:00Z",
    "location": "Barcelona - Club Tiro",
    "maxCapacity": 15,
    "availableSlots": 13
  }
]
```

Obtendrás una lista completa de todos los eventos, ordenados por fecha (los primeros son los más cercanos).

### Paso 3: Filtrar por fecha de inicio

Si solo quieres ver eventos que ocurran **a partir de una fecha específica**, usa el parámetro `startDate` en formato `YYYY-MM-DD`:

**Solicitud:**
```bash
curl -X GET "https://localhost:5001/api/v1/events?startDate=2026-07-01" \
  -H "Accept: application/json"
```

**Resultado:** Solo se devolverán eventos en o después del 1 de julio de 2026.

### Paso 4: Filtrar por fecha de fin

Si quieres ver eventos que ocurran **antes de una fecha específica**, usa el parámetro `endDate`:

**Solicitud:**
```bash
curl -X GET "https://localhost:5001/api/v1/events?endDate=2026-07-31" \
  -H "Accept: application/json"
```

**Resultado:** Solo se devolverán eventos en o antes del 31 de julio de 2026.

### Paso 5: Filtrar por rango de fechas (lo más común)

Para ver eventos en un **período específico**, combina `startDate` y `endDate`:

**Solicitud:**
```bash
curl -X GET "https://localhost:5001/api/v1/events?startDate=2026-07-01&endDate=2026-07-31" \
  -H "Accept: application/json"
```

**Respuesta esperada:**
```json
[
  {
    "id": "55555555-5555-5555-5555-555555555555",
    "title": "Aire Comprimido Carabina - Social",
    "date": "2026-07-12T10:30:00Z",
    "location": "Barcelona - CT Vallès",
    "maxCapacity": 7,
    "availableSlots": 1
  },
  {
    "id": "66666666-6666-6666-6666-666666666666",
    "title": "Pistola de Velocidad - Trofeo del Globo",
    "date": "2026-07-18T11:00:00Z",
    "location": "Valencia - CTW",
    "maxCapacity": 6,
    "availableSlots": 0
  }
]
```

---

## Qué esperar

### Información de cada evento

En la respuesta recibirás los siguientes datos para cada evento:

| Campo | Significado | Ejemplo |
|-------|-------------|---------|
| **id** | Identificador único del evento | `55555555-5555-5555-5555-555555555555` |
| **title** | Nombre completo del evento | "Aire Comprimido Carabina - Social" |
| **date** | Fecha y hora de inicio del evento | `2026-07-12T10:30:00Z` (formato ISO 8601) |
| **location** | Lugar donde se celebra el evento | "Barcelona - CT Vallès" |
| **maxCapacity** | Número máximo de participantes que permite el evento | `7` |
| **availableSlots** | Espacios disponibles para inscribirse *ahora mismo* | `1` (significa que solo hay 1 lugar disponible) |

### Orden de los eventos

Los eventos **siempre se devuelven en orden cronológico** (de más próximo a más lejano):
- El evento más cercano en el tiempo aparece primero
- El evento más lejano en el tiempo aparece al final

### Cálculo de espacios disponibles

El campo **`availableSlots`** se calcula dinámicamente:

```
Espacios disponibles = Capacidad máxima − Personas inscritas (activas)
```

**Ejemplo:**
- Evento con capacidad 20
- 18 personas inscritas
- `availableSlots = 20 − 18 = 2` espacios disponibles

**Nota importante:** Si `availableSlots = 0`, el evento está **completamente lleno**, pero sigue apareciendo en el listado (algunos eventos pueden permitir lista de espera).

---

## Limitaciones

- **Sin paginación (por ahora):** La API devuelve todos los eventos que coinciden con tu filtro. Si hay muchos eventos, la respuesta puede ser grande. En futuras versiones podrá limitarse esto.

- **Formato de fecha fijo:** Los filtros solo aceptan fechas en formato `YYYY-MM-DD` (ejemplo: `2026-07-15`). Cualquier otro formato será rechazado con un error.

- **Sin filtros adicionales (por ahora):** Por el momento solo puedes filtrar por fecha. En futuras versiones podrás filtrar también por categoría, tipo de evento, ubicación, etc.

- **Sin autenticación requerida:** En esta versión MVP, la API es pública. Cualquiera puede acceder a cualquier evento. En futuras versiones esto se restringirá por roles y permisos.

- **Solo lectura:** No puedes crear, editar ni eliminar eventos a través de este endpoint. Esto lo hacen los administradores del sistema a través de otras herramientas.

---

## Preguntas Frecuentes

**¿Qué pasa si no hay eventos que coincidan con mi filtro?**

Recibirás una respuesta HTTP 200 (éxito) con un array vacío `[]`. Esto es normal y significa simplemente que no hay eventos en ese período.

**¿Qué sucede si intento filtrar con fechas incorrectas?**

Obtendrás un error HTTP 400 (Solicitud Inválida) con un mensaje detallado indicando qué está mal. Por ejemplo:
- Si `startDate > endDate` (fecha de inicio después de fecha de fin)
- Si el formato de fecha no es `YYYY-MM-DD`

**¿Cuánto tiempo tarda la API en responder?**

Normalmente menos de 100 milisegundos. La respuesta es muy rápida incluso con cientos de eventos.

**¿Puedo usar solo `startDate` o solo `endDate`?**

Sí, completamente. Puedes usar:
- Solo `startDate` para ver eventos desde una fecha en adelante
- Solo `endDate` para ver eventos hasta una fecha específica
- Ambas para un rango
- Ninguna para todos los eventos

**¿Cómo sé si un evento está lleno?**

Si el campo `availableSlots` es `0`, el evento está lleno. Sin embargo, sigue apareciendo en el listado porque algunos eventos pueden tener lista de espera.

**¿A qué hora comienzan los eventos?**

La hora exacta está en el campo `date`. El formato es ISO 8601 en hora UTC (Hora Universal Coordinada). Por ejemplo: `2026-07-12T10:30:00Z` significa el 12 de julio de 2026 a las 10:30 AM UTC.

**¿Puedo acceder a esta API desde mi aplicación web (Blazor)?**

Sí. La API está configurada con CORS (Cross-Origin Resource Sharing) para permitir solicitudes desde aplicaciones web en localhost. Para producción, esto debe ser reconfigurado con los dominios reales.

**¿Es seguro compartir esta URL con otros?**

Sí, en la versión MVP es completamente seguro. La API es pública y no contiene datos sensibles. En futuras versiones con autenticación, necesitarás credenciales para acceder.

---

## Casos de Uso Comunes

### 1. Mostrar próximos eventos en el sitio web

```bash
# Obtener eventos desde hoy en adelante
curl "https://localhost:5001/api/v1/events?startDate=2026-06-23"
```

Utiliza esto para mostrar en tu página web un listado de "Próximos Eventos".

### 2. Buscar eventos en un mes específico

```bash
# Buscar todos los eventos en julio de 2026
curl "https://localhost:5001/api/v1/events?startDate=2026-07-01&endDate=2026-07-31"
```

Perfecto para un calendario mensual de eventos.

### 3. Buscar eventos para un fin de semana

```bash
# Buscar eventos entre el viernes 19 y el domingo 21 de julio
curl "https://localhost:5001/api/v1/events?startDate=2026-07-19&endDate=2026-07-21"
```

Útil para planificación de fin de semana.

### 4. Encontrar eventos cercanos

```bash
# Eventos en los próximos 30 días
curl "https://localhost:5001/api/v1/events?startDate=2026-06-23&endDate=2026-07-23"
```

---

## Documentación Interactiva (Swagger UI)

Si tienes la API ejecutándose localmente, puedes explorar de forma interactiva todos los endpoints en:

```
https://localhost:5001/swagger
```

Aquí encontrarás:
- Una interfaz gráfica para probar la API
- Documentación automática de parámetros
- Ejemplos de respuestas
- Códigos de error explicados

---

## Recursos Relacionados

- [Documentación Técnica](../technical/issue-7-event-listing-api.md) — Para desarrolladores
- API Documentation (Swagger) — `https://localhost:5001/swagger`
- Repositorio del Proyecto — https://github.com/AlejBlasco/SportsClubEventManager

---

## Soporte

Si encuentras problemas o tienes preguntas:

1. Verifica que la API esté ejecutándose (`dotnet run`)
2. Asegúrate de usar HTTPS (no HTTP)
3. Revisa que el Puerto sea 5001 (u otro configurado)
4. Comprueba que el formato de fechas sea `YYYY-MM-DD`
5. Consulta Swagger UI para ver ejemplos vivos

Para reportar bugs: https://github.com/AlejBlasco/SportsClubEventManager/issues

