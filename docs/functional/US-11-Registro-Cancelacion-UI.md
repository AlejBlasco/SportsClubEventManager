# US-11: Interfaz de Registro y Cancelación de Eventos

**Fecha:** 28 de junio de 2026  
**Versión:** 1.0  
**Estado:** Implementado

---

## Descripción

Esta funcionalidad permite a los usuarios registrarse y cancelar su registro para eventos a través de la interfaz web. Es parte del MVP (Sprint 1) y utiliza un sistema temporal de identificación por nombre y correo electrónico hasta que se implemente OAuth2 en el Sprint 2.

---

## Funcionalidades Implementadas

### 1. Registro para Eventos

Los usuarios pueden registrarse para eventos con plazas disponibles:

1. **Visualización del Botón de Registro:**
   - El botón "Registrarse para el Evento" aparece cuando:
     - El evento tiene plazas disponibles
     - El usuario no está ya registrado
   - El botón está deshabilitado cuando el evento está completo

2. **Formulario de Registro:**
   - Al hacer clic en "Registrarse", aparece un formulario modal
   - Campos requeridos:
     - **Nombre:** 2-100 caracteres
     - **Correo electrónico:** Formato válido, máximo 200 caracteres
   - Validación en tiempo real

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

- El estado de registro se guarda en el navegador (localStorage)
- Si el usuario recarga la página, se mantiene el estado de registro
- El estado persiste entre sesiones del navegador

---

## Casos de Uso

### Caso de Uso 1: Registro Exitoso

**Actor:** Usuario  
**Precondiciones:** El evento tiene plazas disponibles, el usuario no está registrado

**Flujo:**
1. El usuario navega a la página de detalles del evento
2. Ve el botón "Registrarse para el Evento" habilitado
3. Hace clic en el botón
4. Aparece el formulario de registro
5. Introduce su nombre: "María García"
6. Introduce su correo: "maria.garcia@example.com"
7. Hace clic en "Registrar"
8. Ve el mensaje "¡Registro exitoso!"
9. El botón cambia a "Cancelar Registro"
10. Las plazas disponibles se reducen en 1

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

- **Nombre requerido:** "El nombre es obligatorio"
- **Nombre muy corto:** "El nombre debe tener entre 2 y 100 caracteres"
- **Correo requerido:** "El correo electrónico es obligatorio"
- **Correo inválido:** "Por favor, introduce una dirección de correo electrónico válida"

---

## Limitaciones Conocidas (MVP)

### Autenticación Temporal

**Situación actual:** El sistema solicita nombre y correo electrónico sin autenticación real.

**Implicaciones:**
- El mismo usuario puede registrarse múltiples veces con diferentes correos
- No hay verificación de identidad
- El estado solo se guarda en el navegador local

**Solución futura:** OAuth2 en el Sprint 2 (junio-julio 2026)

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
