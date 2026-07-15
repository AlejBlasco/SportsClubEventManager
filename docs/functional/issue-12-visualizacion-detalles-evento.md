# Guía del Usuario — Visualización de Detalles de Evento

**Funcionalidad:** Event Details UI  
**Versión:** 1.0  
**Fecha:** 2026-06-28  
**Estado:** Disponible

---

## ¿Qué es esta funcionalidad?

La página de Detalles de Evento te permite ver información completa sobre un evento específico del club deportivo, incluyendo la fecha, ubicación, capacidad y plazas disponibles. Puedes acceder a esta página desde la lista de eventos para tomar decisiones informadas sobre tu participación.

---

## ¿Cómo acceder a los detalles de un evento?

### Desde la Lista de Eventos

1. Navega a la página de eventos (`/events`)
2. Encuentra el evento que te interesa en la lista o calendario
3. Haz clic en el enlace **"View Details →"** en la tarjeta del evento
4. Serás redirigido a la página de detalles del evento

**Ejemplo:**

```
┌─────────────────────────────────────┐
│  Torneo de Fútbol                   │
│  Jul 15, 2026 2:30 PM              │
│  Location: Estadio Principal        │
│  Available Slots: 20 / 50          │
│                                     │
│  [View Details →]  ← Haz clic aquí │
└─────────────────────────────────────┘
```

---

## ¿Qué información puedo ver?

### Información del Evento

La página de detalles muestra:

1. **Título del Evento** — Nombre completo del evento
2. **Descripción** — Detalles sobre el evento, actividades, requisitos
3. **Fecha y Hora** — Cuándo se lleva a cabo el evento (formato: "Viernes, 15 de junio de 2026 a las 2:30 PM")
4. **Ubicación** — Dónde se realiza el evento
5. **Capacidad Total** — Número máximo de participantes
6. **Plazas Disponibles** — Cuántos lugares quedan libres

### Indicador de Capacidad

Una **barra de progreso visual** muestra el nivel de ocupación del evento:

- **Barra azul** — Porcentaje de plazas ocupadas
- **Patrón rayado** — Indicador visual adicional (accesible para daltónicos)
- **Texto "Fully Booked"** — Aparece cuando el evento está lleno

**Ejemplo:**

```
Capacity
20 / 50

[████████████░░░░░░░░░░░░░] 40%
```

### Badge "Fully Booked"

Cuando un evento no tiene plazas disponibles, verás un **badge rojo "Fully Booked"** junto al título del evento y debajo del indicador de capacidad.

```
┌────────────────────────────────────────┐
│  Torneo de Fútbol  [Fully Booked]     │
└────────────────────────────────────────┘
```

---

## Navegación

### Volver a la Lista de Eventos

En la esquina superior de la página de detalles, encontrarás un enlace **"← Back to Events"** que te devuelve a la lista principal de eventos.

**Ruta de navegación:**

```
Lista de Eventos → Detalles del Evento → [Back to Events] → Lista de Eventos
```

---

## Estados de la Página

### Cargando

Mientras se recuperan los datos del evento, verás un **spinner de carga** con el mensaje "Loading event details...". Esto suele tardar menos de 2 segundos.

```
  ⏳ Loading event details...
```

### Evento No Encontrado (404)

Si navegas a un evento que no existe o ha sido eliminado, verás:

```
┌────────────────────────────────────┐
│        🔍                          │
│   Event Not Found                  │
│                                    │
│   The event you are looking for    │
│   does not exist or has been       │
│   removed.                         │
│                                    │
│   [Back to Events]                 │
└────────────────────────────────────┘
```

### Error al Cargar

Si hay un problema con la conexión o el servidor, verás un **mensaje de error** con un botón **"Retry"**:

```
┌────────────────────────────────────┐
│        ⚠️                          │
│   Unable to load event details.    │
│   Please try again.                │
│                                    │
│   [Retry]                          │
└────────────────────────────────────┘
```

Haz clic en **"Retry"** para intentar cargar los datos de nuevo.

---

## Diseño Responsive (Móvil y Tableta)

La página de detalles de evento está optimizada para todos los dispositivos:

### Móvil (Smartphones)

- Layout de **una sola columna**
- Fuentes optimizadas para pantallas pequeñas
- Touch targets apropiados para dedos
- Sin scroll horizontal

**Viewport mínimo:** 320px (iPhone SE)

### Tableta (iPad)

- Layout similar a móvil con espaciado mayor
- Fuentes más grandes para mejor legibilidad

### Desktop

- Contenedor centrado con **ancho máximo de 800px**
- Espaciado generoso para lectura cómoda

---

## Accesibilidad

Esta página cumple con los estándares **WCAG 2.1 AA** de accesibilidad:

### Para Usuarios con Daltonismo

- El indicador de capacidad usa **gradiente azul**, no rojo/verde
- Patrón rayado proporciona distinción visual adicional
- Badge de texto complementa los indicadores de color

### Para Lectores de Pantalla

- Atributos ARIA en la barra de progreso
- HTML semántico para navegación clara
- Etiquetas descriptivas en todos los elementos

### Navegación por Teclado

- Todos los enlaces y botones accesibles con **Tab**
- Orden de tabulación lógico
- Indicador de foco visible

---

## Preguntas Frecuentes (FAQ)

### ¿Puedo registrarme para un evento desde esta página?

**No todavía.** La funcionalidad de registro de eventos (US-11) se implementará próximamente. Por ahora, puedes ver los detalles y plazas disponibles.

### ¿Qué significa "Fully Booked"?

Significa que el evento ha alcanzado su capacidad máxima y **no hay plazas disponibles**. No puedes registrarte para un evento que está lleno.

### ¿Por qué veo "Event Not Found"?

Esto puede ocurrir si:
- El ID del evento en la URL es incorrecto
- El evento ha sido eliminado
- Navegaste a través de un enlace antiguo o marcador

**Solución:** Haz clic en "Back to Events" y selecciona un evento de la lista actual.

### ¿Qué hago si veo un error al cargar?

1. Haz clic en el botón **"Retry"** para intentar de nuevo
2. Verifica tu conexión a internet
3. Actualiza la página (F5)
4. Si el problema persiste, contacta al soporte técnico

### ¿Puedo ver eventos pasados?

Sí, la página de detalles muestra información de cualquier evento, pasado o futuro. La fecha y hora te indicarán cuándo se realizó o realizará el evento.

### ¿Por qué la página de detalles tarda en cargar?

La página realiza una llamada al servidor para obtener los datos más recientes del evento. Esto normalmente tarda menos de 2 segundos. Si tarda más, puede haber:
- Conexión a internet lenta
- Problemas con el servidor
- Alto tráfico en la aplicación

---

## Capturas de Pantalla

### Vista Desktop — Evento con Disponibilidad

```
┌─────────────────────────────────────────────────────┐
│  ← Back to Events                                   │
│                                                     │
│  Torneo de Fútbol                                  │
│                                                     │
│  Description                                        │
│  ───────────────────────────────────────────────   │
│  Torneo amistoso de fútbol entre equipos del club. │
│  Se requiere calzado deportivo y ropa cómoda.      │
│                                                     │
│  Event Information                                  │
│  ───────────────────────────────────────────────   │
│  Date & Time: Friday, July 15, 2026 at 2:30 PM    │
│  Location: Estadio Principal                        │
│  Total Capacity: 50 participants                    │
│  Available Slots: 20                                │
│                                                     │
│  Capacity                                           │
│  ───────────────────────────────────────────────   │
│  20 / 50                                            │
│  [██████████████████░░░░░░░░░░] 60%               │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Vista Móvil — Evento Lleno

```
┌──────────────────────┐
│ ← Back to Events     │
│                      │
│ Clase de Yoga        │
│ [Fully Booked]       │
│                      │
│ Description          │
│ ──────────────────   │
│ Clase de yoga para   │
│ todos los niveles.   │
│                      │
│ Event Information    │
│ ──────────────────   │
│ Date & Time          │
│ Jun 20, 2026 6:00 PM │
│                      │
│ Location             │
│ Sala de Fitness      │
│                      │
│ Total Capacity       │
│ 30 participants      │
│                      │
│ Available Slots      │
│ 0                    │
│                      │
│ Capacity             │
│ ──────────────────   │
│ 0 / 30               │
│ [████████████████]   │
│ Fully Booked         │
│                      │
└──────────────────────┘
```

---

## Próximas Funcionalidades

### En Desarrollo (US-11: Event Registration)

Próximamente podrás:
- **Registrarte** para eventos con plazas disponibles
- **Cancelar** tu registro si cambias de planes
- Ver el **estado de tu registro** en la página de detalles

---

## Soporte Técnico

Si tienes problemas con la visualización de detalles de eventos:

1. **Verifica** que estás usando un navegador actualizado (Chrome, Firefox, Edge, Safari)
2. **Limpia** la caché del navegador si ves información desactualizada
3. **Intenta** en modo incógnito para descartar problemas de cookies
4. **Contacta** al soporte técnico si el problema persiste

---

**Última actualización:** 2026-06-28  
**Versión:** 1.0  
**Funcionalidad:** US-10 Event Details UI
