# Guía de Usuario — Cancelación de Registros
**Versión:** 2026-06-25  
**Se aplica a:** Sports Club Event Manager — API REST

---

## ¿Qué es esta característica?

La cancelación de registros permite que los usuarios cancelen su inscripción a eventos cuando no puedan asistir. Cuando cancelas tu registro, tu plaza se libera inmediatamente y queda disponible para otros usuarios que deseen inscribirse. Esta característica es especialmente útil si tus planes cambian en el último momento.

---

## Cómo usar la característica

### Paso 1: Identifica el evento y tu ID de usuario

Necesitarás:
- **ID del evento:** El identificador único del evento del que deseas cancelar tu inscripción
- **Tu ID de usuario:** Tu identificador único en el sistema

### Paso 2: Envía una solicitud de cancelación

Utiliza una solicitud DELETE HTTP dirigida al endpoint de cancelación:

```bash
DELETE https://api.sportsclub.local/api/v1/events/{ID_EVENTO}/register
Content-Type: application/json

{
  "userId": "{TU_ID_USUARIO}"
}
```

**Ejemplo real:**
```bash
DELETE https://api.sportsclub.local/api/v1/events/550e8400-e29b-41d4-a716-446655440000/register
Content-Type: application/json

{
  "userId": "660e8400-e29b-41d4-a716-446655440000"
}
```

### Paso 3: Verifica la respuesta

**Si tienes éxito:**
- Recibirás una respuesta `204 No Content`
- Tu registro ha sido cancelado
- Tu plaza queda disponible para otros

**Si hay un problema:**
- Consulta la sección "Escenarios de error" más abajo para ver qué significa el código de error

---

## Qué esperar

Después de cancelar tu registro:

✅ **Tu inscripción se elimina permanentemente**  
La cancelación es definitiva. Tu nombre será removido del listado de inscritos.

✅ **Tu plaza se libera inmediatamente**  
Otros usuarios pueden ahora inscribirse en la plaza que ocupabas.

✅ **Puedes volver a inscribirte**  
Si cambias de opinión, puedes inscribirte nuevamente para el mismo evento.

✅ **No recibirás recordatorios del evento**  
Como ya no estás inscrito, no tendrás acceso a actualizaciones del evento.

---

## Limitaciones

- **Solo eventos futuros:** No puedes cancelar registros para eventos que ya han ocurrido
- **Sin reembolsos:** Esta API no maneja reembolsos (fuera de alcance)
- **Sin confirmación por email:** Por ahora, la cancelación ocurre instantáneamente sin confirmación por email
- **Cancelación permanente:** Una vez cancelado, el registro no puede ser recuperado. Deberás volver a inscribirte si deseas asistir
- **Sin autenticación actual:** Por favor, mantén seguro tu ID de usuario. En futuras versiones, se requerirá autenticación

---

## Preguntas Frecuentes

**¿Qué pasa si intento cancelar una inscripción que no existe?**

Recibirás un error `404 Not Found`. Verifica que:
- El ID del evento es correcto
- El ID de usuario es correcto
- Ya estás inscrito en ese evento

**¿Qué pasa si el evento ya ha comenzado?**

Recibirás un error `400 Bad Request` con el mensaje:
> "Cannot cancel registrations for events that have already occurred."

Solo puedes cancelar inscritos para eventos que aún no han comenzado.

**¿Puedo inscribirme de nuevo después de cancelar?**

Sí. Después de cancelar tu registro, puedes hacer una nueva solicitud de inscripción y tu plaza será restaurada.

**¿Cuánto tiempo tarda la cancelación?**

La cancelación ocurre instantáneamente. Una vez recibida la respuesta exitosa, tu plaza está disponible para otros.

**¿Qué es el error 409 Conflict?**

Este error raro significa que otra operación modificó la información del evento exactamente al mismo tiempo que tu cancelación. Intenta de nuevo; generalmente solo ocurre bajo mucha concurrencia.

**¿Se puede hacer más de una cancelación por evento?**

No. Cada usuario puede tener un solo registro por evento. La segunda cancelación resultará en `404 Not Found` porque ya no tendrás un registro activo.

**¿Dónde puedo ver mi lista de inscritos?**

Consulta el endpoint `GET /api/v1/events/{id}` para ver los detalles del evento, incluyendo el número de plazas disponibles y participantes.

---

**Versión del documento:** 2026-06-25  
**Próxima revisión esperada:** 2026-07-15 (después de Sprint 2 cuando se agregue autenticación)
