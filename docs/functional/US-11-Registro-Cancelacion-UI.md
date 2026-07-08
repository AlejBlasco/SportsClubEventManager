# US-11: Interfaz de Registro y Cancelación de Eventos

**Fecha:** 28 de junio de 2026 (última actualización: 8 de julio de 2026)  
**Versión:** 1.1  
**Estado:** Implementado

---

## Descripción

Esta funcionalidad permite a los usuarios registrarse y cancelar su registro para eventos a través de la interfaz web.

> **Actualización (2026-07-08):** El formulario de registro ya no solicita nombre y correo
> electrónico como en el MVP original. Ahora que la aplicación cuenta con autenticación real
> (OAuth2 / JWT, ver US-27), el registro usa siempre la identidad de la cuenta autenticada: el
> servidor extrae al usuario del JWT y el formulario solo muestra su nombre y correo (de solo
> lectura, prellenados) a modo de confirmación. Ver sección
> [Formulario de Registro](#2-formulario-de-registro) más abajo.

---

## Funcionalidades Implementadas

### 1. Registro para Eventos

Los usuarios pueden registrarse para eventos con plazas disponibles:

1. **Visualización del Botón de Registro:**
   - El botón "Registrarse para el Evento" aparece cuando:
     - El evento tiene plazas disponibles
     - El usuario no está ya registrado
   - El botón está deshabilitado cuando el evento está completo
   - Si el usuario no ha iniciado sesión, se le redirige a `/login`

2. **Formulario de Registro:**
   - Al hacer clic en "Registrarse", aparece un formulario modal
   - **Nombre** y **Correo electrónico** aparecen prellenados automáticamente con los datos de
     la cuenta autenticada, y no son editables desde este formulario
   - Un texto informativo indica: "You are registering with the name and email from your
     account. To change them, update your profile." con enlace directo a `/profile`
   - No es necesario escribir ni validar estos campos manualmente

3. **Confirmación y Resultado:**
   - Mensaje de éxito: "¡Registro exitoso! Ahora estás registrado para este evento."
   - En caso de error: Mensaje descriptivo con posibilidad de reintentar
   - Los detalles del evento se actualizan automáticamente (plazas disponibles)

### 2. Cancelación de Registro

Los usuarios registrados pueden cancelar su participación:

1. **Visualización del Botón de Cancelación:**
   - El botón "Cancelar Registro" reemplaza al de registro cuando el usuario ya está inscrito

2. **Diálogo de Confirmación:**
   - Al hacer clic en "Cancelar Registro", aparece un diálogo de confirmación
   - Mensaje: "¿Estás seguro de que deseas cancelar tu registro para este evento?"
   - Opciones: Cancelar / Confirmar

3. **Confirmación y Resultado:**
   - Mensaje de éxito: "Registro cancelado correctamente."
   - En caso de error: Mensaje descriptivo
   - Los detalles del evento se actualizan (plazas disponibles aumentan)

### 3. Persistencia de Estado

- El estado de registro se consulta al servidor (`GetMyRegistrationsAsync`) en cada carga de la
  página de detalles del evento, no se guarda en el navegador
- Si el usuario recarga la página, el estado de registro se recalcula desde la base de datos
- El estado persiste entre sesiones y dispositivos porque está asociado a la cuenta autenticada,
  no al navegador

---

## Casos de Uso

### Caso de Uso 1: Registro Exitoso

**Actor:** Usuario  
**Precondiciones:** El evento tiene plazas disponibles, el usuario no está registrado

**Flujo:**
1. El usuario navega a la página de detalles del evento (con sesión iniciada)
2. Ve el botón "Registrarse para el Evento" habilitado
3. Hace clic en el botón
4. Aparece el formulario de registro con su nombre y correo ya prellenados desde su cuenta
   (por ejemplo, "María García" / "maria.garcia@example.com"), mostrados de solo lectura
5. Hace clic en "Registrar"
6. Ve el mensaje "¡Registro exitoso!"
7. El botón cambia a "Cancelar Registro"
8. Las plazas disponibles se reducen en 1

**Postcondiciones:** El usuario está registrado, puede cancelar su registro

### Caso de Uso 2: Intento de Registro en Evento Completo

**Actor:** Usuario  
**Precondiciones:** El evento está completo (0 plazas disponibles)

**Flujo:**
1. El usuario navega a la página de detalles del evento
2. Ve el botón "Evento Completo" deshabilitado
3. La insignia "Completo" está visible
4. No puede proceder con el registro

**Postcondiciones:** No se produce ningún registro

### Caso de Uso 3: Cancelación de Registro

**Actor:** Usuario  
**Precondiciones:** El usuario está registrado para el evento

**Flujo:**
1. El usuario navega a la página de detalles del evento
2. Ve el mensaje "Estás registrado para este evento"
3. Ve el botón "Cancelar Registro"
4. Hace clic en el botón
5. Aparece el diálogo de confirmación
6. Hace clic en "Confirmar"
7. Ve el mensaje "Registro cancelado correctamente."
8. El botón cambia a "Registrarse para el Evento"
9. Las plazas disponibles aumentan en 1

**Postcondiciones:** El registro está cancelado, puede volver a registrarse

---

## Mensajes del Sistema

### Mensajes de Éxito

- **Registro exitoso:** "¡Registro exitoso! Ahora estás registrado para este evento."
- **Cancelación exitosa:** "Registro cancelado correctamente."

### Mensajes de Error

- **Evento completo:** "El evento está completo. No se aceptan más registros."
- **Error de red:** "Se produjo un error inesperado. Por favor, inténtalo de nuevo más tarde."
- **Registro duplicado:** "El registro falló. Es posible que el evento esté completo o que ya estés registrado. Por favor, actualiza e inténtalo de nuevo."

### Mensajes de Validación

Los campos de nombre y correo ya no son editables por el usuario (se prellenan desde la cuenta
autenticada), por lo que estos mensajes no deberían aparecer en uso normal. Se conservan como
salvaguarda en el modelo del formulario:

- **Nombre requerido:** "El nombre es obligatorio"
- **Nombre muy corto:** "El nombre debe tener entre 2 y 100 caracteres"
- **Correo requerido:** "El correo electrónico es obligatorio"
- **Correo inválido:** "Por favor, introduce una dirección de correo electrónico válida"

---

## Limitaciones Conocidas

### Sin Notificaciones por Correo

El sistema no envía confirmaciones por correo electrónico.

**Solución futura:** Sistema de notificaciones en el Sprint 3

### Sin Lista de Espera

Si un evento está completo, el usuario no puede unirse a una lista de espera.

**Solución futura:** Sprint 3 o posterior

---

## Accesibilidad

✅ **Navegación por teclado:** Todos los elementos interactivos accesibles  
✅ **Lectores de pantalla:** Etiquetas apropiadas en todos los campos  
✅ **Mensajes de error:** Asociados con los campos correspondientes  
✅ **Estados de carga:** Comunicados a tecnologías de asistencia  
✅ **Contraste de color:** Cumple con las directrices de accesibilidad  

---

## Compatibilidad de Navegadores

✅ Chrome (última versión)  
✅ Firefox (última versión)  
✅ Edge (última versión)  
✅ Safari (última versión)  

Diseño responsive compatible con dispositivos móviles y de escritorio.

---

## Soporte

Para problemas o preguntas:
- Consultar la documentación técnica en `docs/technical/`
- Reportar errores en el sistema de seguimiento de issues
- Contactar al equipo de desarrollo

---

## Historial de Cambios

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0 | 28/06/2026 | Implementación inicial (Sprint 1 MVP) |
| 1.1 | 08/07/2026 | El formulario de registro ya no pide nombre/correo: los prellena de solo lectura desde la cuenta autenticada (OAuth2/JWT). Se elimina la sección de limitaciones sobre autenticación temporal y se corrige la descripción de persistencia de estado (se consulta al servidor, no localStorage). |
