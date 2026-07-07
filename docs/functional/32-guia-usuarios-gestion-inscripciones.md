# Guía de Usuario — Gestión de Inscripciones
**Versión:** 2026-07-07  
**Aplica a:** Sports Club Event Manager (v1.2+)

---

## ¿Qué es esta funcionalidad?

La Gestión de Inscripciones permite a los usuarios ver y cancelar sus propias inscripciones a eventos, y a los administradores del sistema gestionar todas las inscripciones: verlas, filtrarlas, crear inscripciones manuales, cancelarlas, y exportar reportes. Esta funcionalidad es esencial para que los usuarios gestionen su participación en eventos y que los administradores controlen la asistencia y capacidad de eventos.

---

## Cómo Usarlo

### Parte 1: Usuarios — Ver y Cancelar Mis Inscripciones

#### Paso 1: Acceder a Mis Inscripciones

1. Inicia sesión en el portal
2. En el menú superior o lateral, busca la opción **"Mis Inscripciones"** o **"My Registrations"**
3. Haz clic para ir a la página

**Resultado esperado:** Ves una tabla con todos tus eventos próximos en los que estás inscrito.

![Paso 1: Accede a Mis Inscripciones](./images/32-usuario-paso1.png)

#### Paso 2: Revisa tu Lista de Inscripciones

En la tabla verás las siguientes columnas:

| Columna | Qué significa |
|---------|-------|
| **Event** | Nombre del evento |
| **Date** | Cuándo es el evento (fecha y hora) |
| **Location** | Dónde se llevará a cabo |
| **Registered On** | Cuándo te inscribiste |
| **Status** | Verde si estás inscrito, gris si fue cancelada |
| **Actions** | Botones para cancelar |

**Nota:** Solo ves inscripciones a eventos futuros. Eventos pasados no aparecen aquí.

#### Paso 3: Cancela una Inscripción (si es necesario)

1. Encuentra el evento que deseas cancelar en la tabla
2. En la columna **Actions** (última columna), verás un botón **"Cancel"**
3. Haz clic en el botón **"Cancel"**

**Resultado:** El botón mostrará "Cancelling..." mientras se procesa. Si todo funciona:
- Aparecerá un mensaje verde: **"Registration cancelled successfully."**
- El evento desaparecerá de tu lista
- Tu lugar se libera para otro usuario

**Si hay error:**
- Aparecerá un mensaje rojo explicando qué pasó (ej: "Registration no longer available")
- El evento seguirá en tu lista

**Restricciones:**
- ❌ No puedes cancelar inscripciones a eventos que ya ocurrieron
- ❌ No puedes cancelar inscripciones de otros usuarios
- ❌ Si no hay eventos futuros, verás el mensaje: "You do not have active registrations"

![Paso 3: Cancela Inscripción](./images/32-usuario-paso3.png)

---

### Parte 2: Administradores — Gestionar Todas las Inscripciones

#### Paso 1: Acceder a Gestión de Inscripciones

1. Inicia sesión **como Administrador**
2. En el menú, busca **"Registration Management"** o en la sección de Administración
3. Haz clic para ir a la página

**Resultado esperado:** Ves un panel completo con filtros, tabla de registros, y opciones de exportación.

![Paso 1: Accede a Gestión de Inscripciones](./images/32-admin-paso1.png)

#### Paso 2: Visualiza Todas las Inscripciones

La tabla muestra todas las inscripciones del sistema (usuario y evento de ambas). Columnas:

| Columna | Información |
|---------|--------|
| **Event** | Nombre del evento |
| **Event Date** | Fecha y hora del evento |
| **User** | Nombre del usuario inscrito |
| **Email** | Email del usuario |
| **Registered On** | Cuándo se inscribió |
| **Status** | Registered (verde) o Cancelled (gris) |
| **Actions** | Botón para cancelar |

**Paginación:** Por defecto ves 20 inscripciones por página. Usa los botones de navegación (Previous/Next/Página N) en la parte inferior.

#### Paso 3: Filtra Inscripciones

Para encontrar inscripciones específicas, usa la sección **Filtros** en la parte superior:

**Filtro 1: Búsqueda por Texto**
- Escribe en el campo "Search"
- Busca en: título del evento, nombre del usuario, o email
- Ejemplo: "Juan" te mostrará todas las inscripciones de usuarios con "Juan" en el nombre
- Haz clic en botón **"Apply"** para filtrar

**Filtro 2: Estado**
- Dropdown "Status": Selecciona **"All"**, **"Registered"**, o **"Cancelled"**
- Ejemplo: "Registered" para ver solo inscripciones activas

**Filtro 3: Rango de Fechas del Evento**
- Campo "Event From": Pon la fecha mínima (ej: 2026-07-01)
- Campo "Event To": Pon la fecha máxima (ej: 2026-08-31)
- Solo verás inscripciones a eventos en ese rango

**Filtro 4: Ordenamiento**
- Dropdown "Sort By": Elige por qué campo ordenar:
  - Registration Date (por defecto)
  - Event Date
  - Event (título)
  - User (nombre)
  - Status
- Dropdown "Order": Selecciona **"Descending"** (más nuevo primero) o **"Ascending"** (más antiguo primero)

**Aplicar Filtros:**
1. Completa los filtros que desees
2. Haz clic en el botón azul **"Apply"** (con icono de embudo)
3. La tabla se actualiza automáticamente
4. El contador en la parte inferior muestra cuántas inscripciones coinciden

![Paso 3: Usa Filtros](./images/32-admin-paso3.png)

#### Paso 4: Crea una Inscripción Manual

A veces necesitas inscribir a un usuario manualmente (ej: inscripción por teléfono).

1. Busca la sección **"Manual Registration"** en la página (debajo de Sort/Order)
2. Ingresa dos valores:
   - **User ID**: El GUID único del usuario (ej: `550e8400-e29b-41d4-a716-446655440100`)
   - **Event ID**: El GUID único del evento (ej: `550e8400-e29b-41d4-a716-446655440010`)
3. Haz clic en el botón verde **"Create"**

**Resultado:** Si es exitosa:
- Mensaje verde: "Registration created successfully."
- Los campos se limpian
- La tabla se actualiza y ves la nueva inscripción
- Se registra en auditoría quién y cuándo la creó

**Errores posibles:**
- ❌ "Manual registration requires a valid user GUID" — El User ID no es un GUID válido
- ❌ "User is already registered for this event" — El usuario ya está inscrito
- ❌ "Event has reached maximum capacity" — No hay lugar disponible
- ❌ "Cannot register users for events that have already occurred" — El evento ya pasó
- ❌ "User not found" — El User ID no existe en el sistema
- ❌ "Event not found" — El Event ID no existe en el sistema

**Dónde encontrar GUIDs:**
- User ID: En página de gestión de usuarios, o contacta a soporte
- Event ID: En página de gestión de eventos

![Paso 4: Crea Inscripción Manual](./images/32-admin-paso4.png)

#### Paso 5: Cancela una Inscripción

Como administrador, puedes cancelar cualquier inscripción (incluso eventos ya ocurridos).

1. Encuentra la inscripción en la tabla
2. En la columna **Actions**, haz clic en el botón rojo **"Cancel"**
3. La inscripción se cancela inmediatamente

**Resultado:** 
- Mensaje verde: "Registration cancelled successfully."
- La tabla se recarga
- Se registra en auditoría: quién canceló, cuándo, para quién, en qué evento

**Nota:** A diferencia de los usuarios, como admin:
- ✅ Puedes cancelar eventos que ya ocurrieron
- ✅ Se registra cada cancelación en auditoría

![Paso 5: Cancela Inscripción Admin](./images/32-admin-paso5.png)

#### Paso 6: Exporta Listado a CSV

Para descargar un reporte de inscripciones en formato CSV (hojas de cálculo):

1. Aplica los filtros que necesites (visto en Paso 3)
2. Busca la sección **Exportación** en la parte inferior derecha
3. Haz clic en botón **"Export CSV"** (con icono de CSV)

**Resultado:**
- Se descarga un archivo: `registrations-20260707123456.csv`
- Contiene: Registration ID, Event Title, Event Date, User Name, Email, Registration Date, Status
- Abre el archivo en Excel, Google Sheets, LibreOffice, etc.

**Nota:** Exporta solo la página actual. Para exportar todos los resultados con tus filtros:
1. Cambia el filtro de paginación a una página con "todos" visible
2. O exporta página por página

#### Paso 7: Exporta Listado a PDF

Para descargar un reporte de inscripciones en formato PDF:

1. Aplica los filtros que necesites
2. Busca la sección **Exportación** en la parte inferior derecha
3. Haz clic en botón **"Export PDF"** (con icono de PDF)

**Resultado:**
- Se descarga un archivo: `registrations-20260707123456.pdf`
- Contiene un reporte con encabezado, timestamp, y detalles de cada inscripción
- Abre el archivo en Adobe Reader, navegador, o lector PDF

![Paso 7: Exporta](./images/32-admin-paso7.png)

---

## Qué Esperar Después

### Para Usuarios:
- ✅ Al cancelar, tu lugar en el evento se libera inmediatamente
- ✅ Tu email permanece en el sistema para otros propósitos
- ✅ No ves inscripciones canceladas en "Mis Inscripciones"
- ✅ No recibe automáticamente email de confirmación (pero administrador puede ver que cancelaste)

### Para Administradores:
- ✅ Todas las acciones quedan registradas en auditoría con IP y hora
- ✅ Puedes ver historial de cambios (si accedes a auditoría directamente)
- ✅ Los reportes CSV/PDF se crean en cliente (privacidad, sin almacenar en servidor)
- ✅ Cambios se aplican inmediatamente a BD

---

## Limitaciones

### Usuarios:
- ❌ No puedes ver inscripciones a eventos pasados
- ❌ No puedes ver inscripciones de otros usuarios
- ❌ No puedes crear o modificar inscripciones manualmente (solo administrador)
- ❌ No puedes cancelar inscripciones a eventos que ya ocurrieron

### Administradores:
- ❌ Exportar PDF es solo texto plano (no PDF con formato/tablas)
- ❌ Exportar solo incluye la página actual de resultados
- ❌ No hay búsqueda de texto completo (simple Contains)
- ❌ No hay confirmación de diálogo antes de cancelar (se cancela al instante)

---

## Preguntas Frecuentes

**¿Qué pasa si cancelo una inscripción?**  
Tu inscripción se elimina, tu lugar se libera para otros usuarios, y aparecerá un mensaje de confirmación. Los administradores verán en auditoría que cancelaste.

**¿Puedo cancelar inscripciones de otros usuarios?**  
No, a menos que seas administrador. Los usuarios solo ven y pueden cancelar sus propias inscripciones.

**¿Cuántas inscripciones puedo tener activas?**  
Sin límite (depende de la configuración del evento y su capacidad máxima).

**¿Cómo sé si un evento está lleno?**  
Si intentas crear una inscripción manual y ves "Event has reached maximum capacity", está lleno.

**¿Puedo recuperar una inscripción cancelada?**  
No, la cancelación es permanente. Tendrías que crear una nueva inscripción (o contactar al administrador).

**¿Se registra en auditoría cuando cancelo una inscripción como usuario?**  
No. Solo se registra cuando un administrador cancela. Tus cancellaciones las ves solo tú en la UI.

**¿Puedo exportar TODAS las inscripciones, no solo la página actual?**  
Actualmente no. Puedes cambiar el tamaño de página antes de exportar, o exportar página por página.

**¿Dónde encuentro el GUID de un usuario o evento?**  
En las páginas de gestión de usuarios y eventos respectivamente. Si usas la UI, normalmente verás un botón "Copy ID" o similar.

**¿Qué significa el color de la insignia de estado?**  
- Verde = Registered (inscripción activa)
- Gris = Cancelled (inscripción cancelada)

**¿Puedo filtrar por ciudad o ubicación?**  
No en esta versión. Puedes filtrar por rango de fechas del evento.

**¿Cuántos registros puedo ver por página?**  
Por defecto, 20. Puedes cambiar este número si modificas parámetros avanzados.

---

## Ayuda Adicional

- Para reportar problemas: Contacta al equipo de soporte
- Para solicitar cambios: Abre un issue en [GitHub Issues](https://github.com/AlejBlasco/SportsClubEventManager/issues)
- Para documentación técnica: Ver `/docs/technical/32-administracion-gestion-inscripciones.md`

---
