# Guía de Usuario — Calendario de Eventos

**Versión:** 2026-06-26  
**Aplicable a:** SportsClubEventManager

---

## ¿Qué es esta función?

La página de Eventos te permite explorar todos los eventos disponibles en el club de dos formas: una vista de calendario interactivo para ver qué eventos ocurren en cada fecha, y una vista de lista para examinar todos los eventos en orden. Puedes cambiar entre vistas según prefieras, y el sistema recordará tu preferencia durante tu sesión. La función es especialmente útil para identificar rápidamente qué eventos tienen capacidad disponible y cuáles están completos.

---

## Cómo usar esta función

### Paso 1: Acceder a la página de Eventos

1. Haz clic en el enlace **Eventos** en el menú de navegación (esquina superior izquierda)
2. Serás redirigido a `/events` donde se cargará la lista de eventos disponibles

La primera vez que accedes, verás el **calendario** como vista por defecto. Se mostrará un indicador de carga mientras se obtienen los datos del servidor.

![Paso 1: Acceso a la página de Eventos](./images/US-9-step1-access-events.png)

### Paso 2: Explorar la Vista de Calendario

Una vez cargado, verás el calendario del mes actual con los eventos marcados en sus fechas correspondientes:

- **Navegación de meses**: Usa los botones de flecha para ir a meses anteriores o posteriores
- **Vista anual**: Haz clic en el selector de año para cambiar a una vista anual (opcional, depende de tu preferencia)
- **Eventos en el calendario**: Cada evento aparece en su fecha. Si el evento está completo, verá un badge rojo con la etiqueta **"Full"**

Los eventos se muestran con su nombre completo (ejemplo: "Entrenamiento de Baloncesto"). Si el evento está completo, el nombre incluye automáticamente " - Full" para una identificación rápida.

![Paso 2: Vista de Calendario](./images/US-9-step2-calendar-view.png)

### Paso 3: Ver Detalles de un Evento en el Calendario

1. Haz clic sobre un evento en el calendario
2. El sistema está preparado para mostrarte los detalles completos del evento
3. **Nota:** Los detalles de eventos estarán disponibles próximamente en una actualización futura

Por ahora, puedes ver el nombre del evento en el calendario e identificar si está completo por el badge rojo.

### Paso 4: Cambiar a la Vista de Lista

Para ver todos los eventos en formato de lista:

1. Localiza los botones **"Calendar"** y **"List"** en la esquina superior derecha de la página
2. Haz clic en el botón **"List"** (estará resaltado cuando sea la vista activa)
3. La página cambiará instantáneamente a la vista de lista sin necesidad de recargar datos

La vista de lista muestra cada evento en una tarjeta individual con toda la información disponible.

![Paso 4: Cambiar a Vista de Lista](./images/US-9-step4-switch-list-view.png)

### Paso 5: Explorar la Vista de Lista

En la vista de lista, cada evento se muestra en una tarjeta que contiene:

- **Título del evento**: Nombre del evento (ejemplo: "Entrenamiento de Baloncesto")
- **Fecha y hora**: Cuándo ocurre el evento (ejemplo: "29/06/2026 18:00")
- **Ubicación**: Dónde se lleva a cabo (ejemplo: "Gimnasio Principal")
- **Capacidad**: Cuántos lugares hay disponibles
  - Ejemplo: "5 slots disponibles de 20"
  - Ejemplo: "0 slots disponibles de 15" → mostrará badge rojo **"Full"**
- **Badge "Full" (si corresponde)**: Un distintivo rojo que indica que el evento está completo

Los eventos se ordenan cronológicamente desde la fecha más próxima a la más lejana.

![Paso 5: Vista de Lista](./images/US-9-step5-list-view.png)

### Paso 6: Identificar Eventos Completos

Para saber rápidamente qué eventos están completos:

**En vista de calendario:**
- Busca eventos con badge rojo **"Full"** o con " - Full" en el nombre

**En vista de lista:**
- Busca tarjetas de eventos con el badge rojo **"Full"** en la esquina superior derecha
- El campo de capacidad mostrará "0 slots disponibles"

Los eventos completos se destacan visualmente para que no pierdas tiempo intentando registrarte en ellos.

### Paso 7: Navegar entre Vistas

El botón toggleador de vistas (Calendario/Lista) está siempre visible en la parte superior de la página:

- El botón activo está **resaltado** (color más oscuro)
- El botón inactivo está **atenuado** (color más claro)
- Haz clic en el que desees usar
- Tu preferencia se guardará automáticamente durante tu sesión

Cuando cierres la pestaña y la abras nuevamente, el sistema recordará qué vista usabas.

![Paso 7: Toggleador de Vistas](./images/US-9-step7-view-toggle.png)

---

## Qué esperar

### Carga de la página

Cuando accedes a la página de Eventos por primera vez, ves un indicador de carga (un círculo giratorio) con el mensaje "Cargando eventos...". Esto puede tardar 1-2 segundos mientras se obtienen los datos del servidor.

### Vista completada

Una vez cargada, verás:
- El calendario del mes actual (por defecto) con todos los eventos del club
- Los botones de alternancia de vistas en la parte superior
- Cada evento claramente identificado con su nombre, fecha y estado de capacidad

### Sin eventos disponibles

Si no hay eventos disponibles (situación rara), verás el mensaje: **"No hay eventos disponibles en este momento"**

### Error al cargar

Si hay un problema al obtener los eventos (ejemplo: conexión lenta o servidor inaccesible):

1. Verás un mensaje rojo: **"Unable to load events. Please try again." (No se pueden cargar los eventos. Por favor, inténtalo de nuevo.)**
2. Aparecerá un botón **"Retry"** (Reintentar)
3. Haz clic en "Retry" para intentar descargar los eventos nuevamente

---

## Limitaciones

- **Detalles de evento no disponibles**: Por ahora, hacer clic en un evento no abre una página de detalles. Esta función estará disponible próximamente.

- **Sin búsqueda ni filtrado**: No puedes buscar eventos por nombre ni filtrar por fecha. Debes usar el calendario para navegar o revisar manualmente la lista.

- **Vista de preferencia local**: Tu preferencia entre vista de calendario y lista se guarda solo en tu navegador, para tu sesión actual. Si usas otro navegador o abres una pestaña nueva, la preferencia vuelve al valor por defecto (calendario).

- **Calendarios en móviles**: En pantallas pequeñas (teléfono), el calendario puede ser un poco pequeño. Puedes cambiar a la vista de lista para una mejor experiencia móvil.

- **Sin actualizaciones automáticas**: Si un evento se llena mientras estás viendo la página, no lo verás cambiar automáticamente. Necesitarás hacer clic en "Reintentar" para actualizar la lista.

---

## Preguntas Frecuentes

**¿Cómo sé si un evento está completo?**

Busca el badge rojo **"Full"** en la tarjeta del evento. En la vista de calendario, también se mostrará " - Full" al final del nombre del evento. En la vista de lista, verás "0 slots disponibles".

**¿Dónde puedo registrarme para un evento?**

El registro en eventos estará disponible próximamente. Por ahora, la página de Eventos solo permite explorar y ver qué eventos hay disponibles.

**¿Mi preferencia de vista se guarda?**

Sí, tu preferencia entre vista de calendario y lista se mantiene durante tu sesión actual. Si cierras la pestaña o el navegador, la preferencia se pierde y vuelve al valor por defecto (calendario) en la siguiente visita.

**¿Qué hago si veo un error?**

Si ves un mensaje de error, intenta lo siguiente:
1. Haz clic en el botón **"Retry"** (Reintentar)
2. Si persiste, verifica tu conexión a Internet
3. Si el problema continúa, la API del servidor puede estar inaccesible. Vuelve más tarde.

**¿Puedo ver eventos que ya pasaron?**

La página muestra eventos disponibles en el club. Normalmente estos son eventos futuros, pero depende de cómo el club los configure en el sistema.

**¿Funciona en mi teléfono?**

Sí, la página es completamente responsiva y funciona en dispositivos móviles. Si el calendario es pequeño en tu teléfono, cambiar a la vista de lista puede ser más cómodo.

**¿Qué significa "slots disponibles"?**

Un "slot" es un lugar o plaza en el evento. Si un evento tiene 20 lugares de capacidad máxima y 5 slots disponibles, significa que hay 15 personas ya registradas y quedan 5 lugares disponibles para nuevas personas. Cuando los slots disponibles llegan a 0, el evento está completo.

---

## Funciones relacionadas

- **Detalles de Evento** (próximamente) — Ver información completa de un evento específico
- **Registro en Eventos** (próximamente) — Inscribirse en un evento disponible
- **Mis Eventos** (próximamente) — Ver eventos en los que estoy registrado

