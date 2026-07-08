# Guía de Usuario — Gestión Completa de Eventos en Administración

**Versión:** 2026-07-07  
**Aplicable a:** Sports Club Event Manager (Administración)

---

## ¿Qué es esta funcionalidad?

La Gestión de Eventos es una herramienta que permite a los administradores del sistema crear, editar y eliminar eventos deportivos en el calendario del club. Como administrador, puedes crear nuevos eventos con título, descripción, fecha, ubicación y capacidad máxima. También puedes editar los detalles de eventos existentes (cambiar fecha, capacidad, ubicación, etc.), ver una lista completa de todos los eventos (incluyendo pasados) con herramientas avanzadas de búsqueda y filtrado, y eliminar eventos que ya no se necesitan. Cuando eliminas un evento que tiene inscripciones, el sistema automáticamente cancela todas esas inscripciones de forma segura.

Cada acción que realices (crear, editar, eliminar un evento) queda registrada en un registro de auditoría para fines de seguridad y cumplimiento.

**Quién lo usa:** Administradores del sistema (usuarios con rol "Administrador")

---

## Cómo acceder

1. Inicia sesión en la plataforma con tu cuenta de administrador
2. En el menú principal, ve a **Administración** → **Gestión de Eventos**
3. Verás una tabla con todos los eventos del club

Si no ves esta opción, tu cuenta no tiene permisos de administrador. Contacta al administrador del sistema.

---

## Cómo usar

### Paso 1: Ver la lista de eventos

Cuando accedes a Gestión de Eventos, ves la primera página de eventos (máximo 20 eventos por página). La tabla muestra:
- **Título** del evento
- **Fecha y hora** del evento
- **Ubicación**
- **Inscripciones** (número actual / capacidad máxima, ej: 15/32)
- **Estado** (Próximo o Pasado)
- **Acciones** (botones Editar y Eliminar)

**Nota:** Los eventos aparecen en orden por fecha (más recientes primero). Puedes cambiar el orden haciendo clic en los encabezados de las columnas.

Si hay más de 20 eventos, verás controles de paginación (1, 2, 3, ..., Siguiente) en la parte inferior.

---

### Paso 2: Crear un nuevo evento

#### Para crear un evento:

1. Haz clic en el botón **"+ Crear Nuevo Evento"** en la esquina superior derecha
2. Se abrirá un formulario modal con los siguientes campos:
   - **Título** (obligatorio) — Nombre del evento, máximo 200 caracteres
   - **Descripción** (opcional) — Detalles sobre el evento, máximo 2000 caracteres
   - **Fecha y Hora** (obligatoria) — Debe ser una fecha y hora futura (no pasada)
   - **Ubicación** (obligatoria) — Dónde se realizará el evento, máximo 300 caracteres
   - **Capacidad Máxima** (obligatoria) — Número máximo de personas que pueden registrarse (mínimo 1, máximo 10,000)

3. Completa todos los campos requeridos con información válida
4. Haz clic en el botón **"Crear Evento"**
5. Si todo está correcto, el evento se crea y verás un mensaje de confirmación
6. El nuevo evento aparecerá en la tabla y estará disponible para que los usuarios se registren

#### Validaciones al crear:
- Título no puede estar vacío
- Fecha debe ser en el futuro (no hoy, ni pasada)
- Ubicación no puede estar vacía
- Capacidad debe ser un número positivo (> 0)

Si olvidas completar un campo o cometes un error, el sistema te lo indicará en rojo junto al campo incorrecto.

---

### Paso 3: Buscar y filtrar eventos

Hay varias formas de encontrar un evento específico:

#### Búsqueda rápida:
- En el cuadro de texto "Buscar evento", escribe el nombre o ubicación del evento
- Los resultados se actualizan automáticamente mientras escribes
- La búsqueda no distingue entre mayúsculas y minúsculas

#### Filtro por rango de fechas:
- En los campos "Desde" y "Hasta", selecciona un rango de fechas
- Solo aparecerán eventos cuya fecha caiga dentro de ese rango
- Puedes dejar uno o ambos campos vacíos para no filtrar por fecha

#### Filtro por estado:
- Usa el dropdown "Estado" para filtrar:
  - **Todos** — Muestra todos los eventos (pasados y futuros)
  - **Próximos** — Solo eventos cuya fecha es en el futuro
  - **Pasados** — Solo eventos cuya fecha ya pasó

#### Combinar filtros:
- Puedes usar búsqueda + rango de fechas + estado simultáneamente
- Los eventos que aparecen cumplen TODOS los criterios (no solo uno)

#### Ordenar:
- Haz clic en el encabezado de cualquier columna para ordenar por ese campo:
  - Título, Ubicación, Capacidad, Fecha de Creación
- Haz clic nuevamente en el mismo encabezado para invertir el orden (de A→Z o Z→A)

---

### Paso 4: Editar un evento existente

#### Para editar un evento:

1. En la tabla, encuentra el evento que deseas editar
2. Haz clic en el botón **"Editar"** (icono de lápiz) en la última columna
3. Se abrirá un formulario modal con los detalles actuales del evento
4. Modifica los campos que necesites cambiar:
   - Puedes cambiar el título, descripción, fecha, ubicación o capacidad
   - Los campos obligatorios son los mismos que al crear (Título, Fecha, Ubicación, Capacidad)

5. Haz clic en **"Actualizar Evento"** cuando termines
6. Verás un mensaje de confirmación: "Evento actualizado exitosamente"

#### Restricciones importantes:

**❌ No puedes editar eventos pasados:**
- Si intentas editar un evento cuya fecha ya pasó, el sistema te lo prohibirá
- Verás un botón "Editar" deshabilitado (gris) para eventos pasados
- Esto previene cambios retroactivos que podrían crear inconsistencias con las inscripciones

**❌ No puedes reducir la capacidad por debajo de inscripciones actuales:**
- Si el evento tiene 15 personas inscritas y tú intentas reducir la capacidad a 10, el sistema rechazará el cambio
- Verás un error: "La capacidad no puede ser menor que las inscripciones actuales (15). Por favor cancela algunas inscripciones primero."
- Debes primero cancelar algunas inscripciones antes de poder reducir la capacidad

#### Validaciones al editar:
- Mismas reglas que al crear: fecha futura, título no vacío, ubicación no vacía, capacidad válida
- Si hay conflicto de edición (otro admin editó el evento al mismo tiempo), verás el error: "El evento fue modificado por otro administrador. Por favor recarga la página e intenta de nuevo."

---

### Paso 5: Eliminar un evento

#### Para eliminar un evento:

1. En la tabla, encuentra el evento que deseas eliminar
2. Haz clic en el botón **"Eliminar"** (icono de papelera) en la última columna
3. Se abrirá una ventana de confirmación mostrando:
   - **Título** del evento a eliminar
   - **Fecha** del evento
   - **Ubicación**
   - **⚠️ Advertencia importante:** "Este evento tiene N inscripciones"
   - **Aviso:** "Eliminar este evento cancelará todas las N inscripciones. Esta acción no se puede deshacer."

4. Revisa cuidadosamente la información (especialmente el número de inscripciones que se cancelarán)
5. Si estás seguro, haz clic en **"Sí, Eliminar Evento"**
6. El evento se elimina del sistema y verás un mensaje: "Evento eliminado. N inscripciones fueron canceladas."

#### Restricciones importantes:

**❌ No puedes eliminar eventos pasados:**
- Si intentas eliminar un evento cuya fecha ya pasó, el sistema te lo prohibirá
- Verás un botón "Eliminar" deshabilitado (gris) para eventos pasados
- Esto protege la integridad histórica de los datos

#### Qué pasa cuando eliminas un evento con inscripciones:

- Todas las personas inscritas tienen su inscripción **cancelada automáticamente**
- Las inscripciones canceladas quedan registradas en el sistema (no se borran completamente)
- Los usuarios inscritos verán que su inscripción cambió de estado "Activo" a "Cancelado" cuando vuelvan a consultar
- La acción de eliminar queda registrada en el registro de auditoría del sistema

---

## Qué esperar después de cada acción

### Después de crear un evento:
- El evento aparece en la tabla de lista de eventos
- El evento está visible para todos los usuarios del club en el calendario público
- Los usuarios pueden comenzar a inscribirse inmediatamente
- La acción queda registrada en auditoría: "Evento Creado"

### Después de editar un evento:
- Los cambios aparecen inmediatamente en la tabla
- Si editaste la fecha, el evento podría moverse de posición en la tabla
- Los usuarios del club verán los cambios actualizados en el calendario público
- La acción queda registrada en auditoría: "Evento Actualizado" (con detalle de qué cambió)

### Después de eliminar un evento:
- El evento desaparece inmediatamente de la tabla
- El evento desaparece del calendario público
- Todas las inscripciones asociadas se cancelan automáticamente
- La acción queda registrada en auditoría: "Evento Eliminado" (con detalle de cuántas inscripciones se cancelaron)

---

## Limitaciones

- **Capacidad máxima:** No puedes crear eventos con capacidad mayor a 10,000 personas
- **Longitud de texto:** Títulos máximo 200 caracteres, descripción máximo 2,000 caracteres, ubicación máximo 300 caracteres
- **Eventos pasados:** No se pueden editar ni eliminar eventos cuya fecha ya pasó
- **Inscripciones canceladas no se reinstician:** Cuando cancelas un evento (eliminándolo), esas inscripciones se quedan como "Canceladas" permanentemente. Si recreas el mismo evento después, los usuarios deberán volver a inscribirse.
- **Paginación:** Se muestran máximo 20 eventos por página. Para más eventos, usa los controles de paginación en la parte inferior

---

## Preguntas Frecuentes

**¿Puedo editar un evento que ya comenzó?**  
No. Si la fecha del evento ya pasó, el sistema no te permitirá hacer cambios. Esto protege la consistencia de los datos históricos.

**¿Qué pasa si intento cambiar la capacidad a un número menor que las personas inscritas?**  
El sistema rechazará el cambio y te mostrará un error: "La capacidad no puede ser menor que las inscripciones actuales. Por favor cancela algunas inscripciones primero." Debes primero cancelar inscripciones (en la sección de Gestión de Inscripciones) antes de poder reducir la capacidad.

**¿Se notifica a los usuarios cuando elimino un evento?**  
Actualmente, cuando eliminas un evento, el sistema cancela automáticamente las inscripciones pero NO envía notificaciones por email a los usuarios. Esta funcionalidad de notificación está planeada para una versión futura.

**¿Puedo recuperar un evento que eliminé accidentalmente?**  
No. Una vez que eliminas un evento, no hay forma de recuperarlo desde la interfaz. Cada eliminación queda registrada en el registro de auditoría del sistema. Contacta al administrador del sistema si necesitas recuperar datos de eventos eliminados.

**¿Puedo cambiar la fecha de un evento a una fecha pasada?**  
No. El sistema solo permite fechas futuras en los eventos. Esto previene inconsistencias con las inscripciones y la lógica del calendario.

**¿Qué significa "Estado: Pasado"?**  
Un evento se marca como "Pasado" cuando su fecha y hora ya han ocurrido (comparado con la hora actual del servidor). Los eventos pasados se pueden ver pero no editar ni eliminar.

**¿Cuántos eventos puedo crear?**  
No hay límite técnico en el sistema. Puedes crear tantos eventos como sea necesario.

**¿Por qué mi botón "Editar" está gris?**  
El botón "Editar" se deshabilita automáticamente para eventos pasados. La fecha del evento ya ocurrió, así que no se permite hacer cambios retroactivos.

**¿Qué pasa si dos administradores intentan editar el mismo evento al mismo tiempo?**  
Solo el primero en guardar sus cambios tendrá éxito. El segundo verá el error: "El evento fue modificado por otro administrador. Por favor recarga la página e intenta de nuevo." El segundo admin deberá recargar la página para ver los cambios del primer admin y luego reintentar.

---

## Consejos y Buenas Prácticas

### ✓ Sé descriptivo en el título y descripción
- Usa títulos claros: "Torneo de Tenis Senior 2026" es mejor que "Torneo"
- Incluye detalles en la descripción: nivel requerido, equipo necesario, si hay cuota, etc.

### ✓ Revisa cuidadosamente antes de eliminar
- Especialmente si el evento tiene muchas inscripciones
- Lee la advertencia del modal de confirmación que muestra cuántas inscripciones se cancelarán

### ✓ Mantén actualizada la capacidad máxima
- Si esperas más o menos participantes, ajusta la capacidad anticipadamente
- Esto evita sorpresas cuando se alcanza el límite

### ✓ Usa los filtros para organizar
- Si tienes muchos eventos, usa filtros de fechas y búsqueda para encontrar rápidamente lo que necesitas

### ✓ Verifica la zona horaria
- Asegúrate de que la fecha y hora del evento son correctas en tu zona horaria local
- El sistema usa hora UTC internamente, pero la interfaz debe mostrar tu zona

---

## Información Técnica para Administradores

### Registro de Auditoría
Toda acción (crear, editar, eliminar evento) queda registrada con:
- Administrador que realizó la acción
- Tipo de acción (EventoCreado, EventoActualizado, EventoEliminado)
- Detalles: qué cambió, cuántas inscripciones se cancelaron, etc.
- Dirección IP del administrador
- Marca de tiempo exacta
- User-Agent del navegador

El equipo de IT puede acceder a este registro de auditoría para auditorías de seguridad y cumplimiento.

### Datos de Contacto para Soporte
Si encuentras problemas o tienes preguntas técnicas sobre la Gestión de Eventos, contacta a:
- **Equipo de IT:** [email/teléfono]
- **Administrador del Sistema:** [nombre/email]

---

**Fin de Guía de Usuario — Gestión de Eventos (US-31)**
