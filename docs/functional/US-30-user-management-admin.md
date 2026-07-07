# Guía de Usuario — Gestión de Usuarios en Administración

**Versión:** 2026-07-07  
**Aplicable a:** Sports Club Event Manager (Administración)

---

## ¿Qué es esta funcionalidad?

La Gestión de Usuarios es una herramienta que permite a los administradores del sistema ver, buscar, editar y gestionar todas las cuentas de usuario de la plataforma. Como administrador, puedes cambiar información de usuarios (nombre, email, etc.), activar o desactivar cuentas, asignar roles de acceso, y eliminar usuarios cuando sea necesario. Cada acción que realices queda registrada en un registro de auditoría para fines de cumplimiento y seguridad.

**Quién lo usa:** Administradores del sistema (usuarios con rol "Administrator")

---

## Cómo acceder

1. Inicia sesión en la plataforma con tu cuenta de administrador
2. En el menú principal, ve a **Administración** → **Gestión de Usuarios**
3. Verás una tabla con todos los usuarios del sistema

Si no ves esta opción, tu cuenta no tiene permisos de administrador. Contacta al administrador del sistema.

---

## Cómo usar

### Paso 1: Ver la lista de usuarios

Cuando accedes por primera vez, ves la primera página de usuarios (máximo 20 usuarios por página). La tabla muestra:
- **Nombre** del usuario
- **Email** de contacto
- **Rol** (Administrator, Member, o Guest)
- **Estado** (Activo o Inactivo)
- **Último acceso** (cuándo entró por última vez)

Si hay más de 20 usuarios, verás controles de paginación en la parte inferior.

### Paso 2: Buscar y filtrar usuarios

Hay varias formas de encontrar un usuario específico:

**Búsqueda rápida:**
- En el cuadro "Buscar usuario", escribe el nombre o email del usuario
- Los resultados se actualizan automáticamente
- La búsqueda no distingue mayúsculas/minúsculas

**Filtro por rol:**
- En el dropdown "Filtrar por rol", selecciona el rol que quieres ver (Administrator, Member, Guest, etc.)
- Solo aparecerán usuarios con ese rol

**Filtro por estado:**
- En el dropdown "Estado", selecciona "Activo" o "Inactivo"
- Solo aparecerán usuarios con ese estado

**Combinar filtros:**
- Puedes usar búsqueda + rol + estado simultáneamente
- Los usuarios que aparecen cumplen TODOS los criterios (no solo uno)

**Ordenar:**
- Haz clic en el encabezado de cualquier columna (Nombre, Email, Rol, Estado, Último Acceso) para ordenar por ese campo
- Haz clic nuevamente para invertir el orden (ascendente/descendente)

### Paso 3: Ver detalles de un usuario

1. En la tabla de usuarios, haz clic en la fila del usuario que quieres ver
2. Se abrirá un panel de detalles con información completa:
   - Nombre, email, género, número de licencia, categoría
   - Rol actual
   - Estado de cuenta (Activo/Inactivo)
   - Fecha de creación
   - Fecha de último acceso
   - Cantidad de eventos a los que está registrado

### Paso 4: Editar información de usuario

En el panel de detalles:

1. Haz clic en el botón **"Editar"**
2. Se habilitarán los campos para que puedas cambiar:
   - Nombre
   - Email
   - Género
   - Número de licencia
   - Categoría de licencia

**Importante:** Si cambias el email, debe ser una dirección de email válida y no puede estar ya en uso por otro usuario.

3. Haz clic en **"Guardar"** cuando termines
4. Verás un mensaje de confirmación: "Usuario actualizado exitosamente"

**¿Puedo editar mi propia cuenta?** Sí, puedes. Pero verás un aviso amarillo recordándote que estás modificando tu propia cuenta.

### Paso 5: Cambiar rol de un usuario

En el panel de detalles:

1. Busca la sección "Rol"
2. Haz clic en **"Cambiar rol"**
3. Se abrirá un dropdown con los roles disponibles: Administrator, Member, Guest
4. Selecciona el nuevo rol
5. Haz clic en **"Guardar"**

**Restricción importante:** No puedes remover el rol "Administrator" si es el único usuario con ese rol en el sistema. Recibirás un error: "No se puede remover el último Administrator del sistema". Esto es para proteger que la plataforma no quede bloqueada sin administrador.

**¿Puedo cambiar mi propio rol?** Sí, pero ten cuidado. Si te cambias a ti mismo a "Member" o "Guest", perderás acceso a esta página de Administración inmediatamente.

### Paso 6: Activar o desactivar una cuenta

En el panel de detalles:

1. Busca la sección "Estado"
2. Si la cuenta está **Activa**, verás el botón **"Desactivar cuenta"**
   - Los usuarios inactivos NO pueden iniciar sesión
   - Sus registraciones se mantienen en el sistema
3. Si la cuenta está **Inactiva**, verás el botón **"Activar cuenta"**
   - El usuario podrá iniciar sesión nuevamente

Haz clic en el botón correspondiente. Verás un mensaje de confirmación.

### Paso 7: Eliminar un usuario

⚠️ **Esta acción es irreversible. El usuario y toda su información se eliminarán permanentemente.**

En el panel de detalles:

1. Busca la sección "Zona de peligro" (generalmente al final)
2. Haz clic en **"Eliminar usuario"**
3. Se abrirá un diálogo de confirmación mostrando:
   - El nombre del usuario
   - El email del usuario
4. El diálogo te pedirá que escribas el nombre del usuario para confirmar (para evitar eliminaciones accidentales)
5. Una vez confirmado, haz clic en **"Confirmar eliminación"**

**¿Qué sucede cuando elimino un usuario?**
- Su cuenta se elimina del sistema
- Todos sus registros de eventos también se eliminan
- Su información no se puede recuperar
- Se genera una entrada en el registro de auditoría

**Restricción importante:** No puedes eliminar el único usuario Administrator en el sistema. Si intentas, verás un error.

**¿Puedo eliminar mi propia cuenta?** Sí, pero solo si hay otros Administrators en el sistema. Verás un diálogo de confirmación adicional advirtiéndote que se cerrará tu sesión inmediatamente.

---

## Qué esperar

### Después de realizar cambios

- Se mostrará un mensaje de confirmación verde ("Usuario actualizado", "Cuenta desactivada", etc.)
- El panel de detalles se actualizará para mostrar los cambios
- La tabla principal también se actualizará

### Paginación

- Si hay más de 20 usuarios, verás botones para ir a la página anterior/siguiente
- También puedes saltar a una página específica usando los números
- Los filtros que apliques se mantienen mientras navegas entre páginas

### Búsqueda lenta

- Si buscas en un sistema con miles de usuarios, el servidor puede tardar un segundo o dos en procesar
- Espera a que aparezcan los resultados antes de realizar otra acción

---

## Limitaciones

- **No hay edición masiva:** Solo puedes editar un usuario a la vez. Si necesitas cambiar múltiples usuarios (ej: desactivar 50), debes hacerlo uno por uno.

- **No hay recuperación de eliminar:** Una vez eliminado, un usuario no se puede recuperar. Asegúrate de que es lo que quieres hacer.

- **El email no se verifica:** Si cambias el email de un usuario a uno incorrecto, el usuario no recibirá confirmación. Pueden cambiar su email nuevamente usando el formulario de perfil.

- **No puedes bloquear el último Administrator:** El sistema te protege de quedarse sin administrador, pero esto significa que siempre debe haber al menos uno.

- **Historial limitado:** Solo puedes ver la información actual del usuario. Para ver un historial completo de cambios, consulta el Registro de Auditoría (disponible para administradores).

---

## Preguntas Frecuentes

**¿Cuál es la diferencia entre "Desactivar" y "Eliminar"?**

- **Desactivar:** La cuenta sigue existiendo pero el usuario no puede iniciar sesión. Puedes reactivarla después. Sus registraciones se mantienen en el sistema.
- **Eliminar:** La cuenta y toda su información se borran permanentemente. No se puede recuperar.

**¿Qué sucede si elimino un usuario que tiene registraciones en eventos?**

Sus registraciones también se eliminan. Si necesitas preservar el historial de registraciones, desactiva su cuenta en lugar de eliminarla.

**¿Puedo ver quién realizó cada cambio en un usuario?**

Sí. En el Registro de Auditoría (disponible en Administración), puedes ver:
- Quién (qué administrator) realizó cada cambio
- Cuándo se realizó
- Qué cambios se hicieron exactamente
- Desde qué dirección IP

**¿Qué pasa si cometo un error?**

- **Si cambiaste algo:** Simplemente edita nuevamente y corrige el valor.
- **Si eliminaste algo:** Desafortunadamente, no se puede recuperar. El sistema registra la eliminación en el Registro de Auditoría.
- **Si tu propia cuenta fue desactivada:** Contacta a otro Administrator para que te reactive.

**¿Cuál es el número máximo de usuarios que puedo ver en una página?**

El máximo es 100 usuarios por página. El valor por defecto es 20 para que las páginas carguen rápido. Puedes cambiar el tamaño de página en las opciones de paginación.

**¿Qué roles están disponibles?**

- **Administrator:** Acceso total a todo el sistema, incluyendo la Administración
- **Member:** Acceso a la mayoría de funciones (eventos, perfil)
- **Guest:** Acceso limitado (solo lectura de eventos públicos)

**¿Puedo cambiar mi propio rol a "Guest"?**

Sí, pero perderás acceso inmediatamente. Solo podrías recuperar acceso si otro Administrator te cambia de rol nuevamente.

**¿Se notifica al usuario cuando su cuenta es modificada?**

No, actualmente no se envían notificaciones. El usuario se enterará cuando intente acceder con los cambios (ej: si desactivas su cuenta, recibirá error al intentar iniciar sesión).

**¿Hay límite de usuarios que puedo crear/eliminar?**

No hay límite técnico. Sin embargo, cambios masivos pueden afectar el rendimiento del sistema.

---

## Funcionalidades relacionadas

- [Perfil de Usuario](./user-profile.md) — Los usuarios pueden editar su propia información de perfil
- [Control de Acceso Basado en Roles](./rbac.md) — Información técnica sobre cómo los roles controlan permisos
- [Registro de Auditoría](./audit-log.md) — Ver historial completo de cambios (disponible en Administración)

---

**Última actualización:** 2026-07-07  
**Versión de funcionalidad:** 1.0  
**Estado:** En producción (con pruebas manuales de seguridad pendientes)
