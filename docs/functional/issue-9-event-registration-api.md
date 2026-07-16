# Registrarse en Eventos — Guía de Usuario

**Versión:** 2026-06-24  
**Aplicable a:** SportsClubEventManager

> **Nota de vigencia (2026-07-15):** El "Paso 2" de esta guía (enviar `userId` en el cuerpo de la
> solicitud) quedó obsoleto tras añadirse autorización basada en JWT (US-28, 2026-07-06): el
> `userId` ya no se lee del cuerpo de la solicitud, se obtiene automáticamente del token de sesión
> de quien esté autenticado — solo puede registrarse a sí mismo por esta vía. Si desea registrar a
> otro usuario (solo administradores), vea
> [docs/operations/inscripcion-eventos.md](../operations/inscripcion-eventos.md) y
> [docs/operations/administracion-inscripciones.md](../operations/administracion-inscripciones.md)
> para el comportamiento vigente.

---

## ¿Qué es esta funcionalidad?

La funcionalidad de registro de eventos permite a los usuarios asegurar su lugar en un evento deportivo a través de la API. Cuando usted desea participar en un evento (como un torneo de básquetbol o partido amistoso), puede enviar una solicitud de registro que reserva inmediatamente su puesto. El sistema responde indicando si su registro fue exitoso, si el evento está lleno, o si ya estaba registrado.

Esta funcionalidad beneficia a:
- **Usuarios del club:** Pueden registrarse rápidamente en eventos sin formularios largos
- **Organizadores:** Saben exactamente cuántas personas participarán
- **Administradores:** Pueden ver quién está registrado y cuántos lugares quedan

---

## Cómo usar

### Paso 1: Seleccione un evento

En la lista de eventos disponibles, identifique el evento al que desea asistir.

**Información útil a revisar:**
- Nombre del evento (ej: "Torneo de Básquetbol")
- Fecha y hora
- Ubicación
- Cuántos lugares están disponibles aún

### Paso 2: Inicie el proceso de registro

Cuando esté listo para registrarse, busque el botón "Registrarse" o acceda al endpoint API:

**Endpoint:** `POST /api/v1/events/{idDelEvento}/register`

**Datos a enviar:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Reemplace:**
- `{idDelEvento}` — El ID único del evento (se obtiene de la lista de eventos)
- `"userId"` — Su ID de usuario (identificador único como usuario del sistema)

### Paso 3: Revise la confirmación

Si todo va bien, recibirá una confirmación con:

- **Número de confirmación:** Su ID de registro único
- **Detalles del evento:** Nombre, fecha, ubicación
- **Su posición:** Cuál es su número de registro (1er, 2do, etc.)
- **Lugares restantes:** Cuántos lugares quedan disponibles para otros usuarios

**Ejemplo de confirmación:**
```json
{
  "registrationId": "8c9e6f42-1234-5678-abcd-ef1234567890",
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "registeredAt": "2026-06-24T14:30:00Z",
  "status": "Confirmado",
  "event": {
    "title": "Torneo de Básquetbol Junio 2026",
    "date": "2026-07-15T19:00:00Z",
    "location": "Cancha Municipal Centro",
    "maxCapacity": 50,
    "currentRegistrations": 25,
    "availableSlots": 25,
    "isFullyBooked": false
  }
}
```

---

## Qué esperar

### Si el registro es exitoso

- ✅ Verá un mensaje de confirmación inmediato
- ✅ Recibirá un número de confirmación único
- ✅ Su lugar en el evento está **reservado**
- ✅ Si cancela después, puede registrarse nuevamente

### Si el evento está lleno

- ❌ Recibirá el mensaje: "El evento ha alcanzado su capacidad máxima"
- ❌ No puede registrarse en este momento
- ✅ Intente registrarse más tarde si alguien cancela su registro
- ✅ **Nota:** En futuras versiones habrá lista de espera

### Si ya estaba registrado

- ❌ Recibirá el mensaje: "Ya está registrado para este evento"
- ❌ No puede registrarse dos veces para el mismo evento
- ✅ Si desea cambiar su registro, cancele primero y luego regístrese de nuevo

### Si el evento ya pasó

- ❌ Recibirá el mensaje: "No puede registrarse en eventos que ya han ocurrido"
- ❌ La fecha/hora del evento debe ser en el futuro
- ✅ Busque otros eventos disponibles

---

## Limitaciones

- **No puede registrarse más de una vez:** Cada usuario solo puede tener un registro activo por evento. Si cancela, puede registrarse nuevamente.

- **El evento debe tener lugares disponibles:** Si alcanza la capacidad máxima, debe esperar a que alguien cancele (o registrarse en una lista de espera en versiones futuras).

- **El evento no puede estar en el pasado:** Solo puede registrarse si la fecha y hora del evento son en el futuro.

- **Se requiere ID de usuario válido:** Debe proporcionar un identificador único válido en formato GUID.

- **Sin confirmación por email aún:** Actualmente el sistema solo retorna confirmación en la API. Las notificaciones por email se implementarán en versiones futuras.

---

## Preguntas Frecuentes

**¿Qué hago si recibo un error "Evento no encontrado"?**

El ID del evento que envió no existe en el sistema. Verifique que:
1. Copió correctamente el ID del evento
2. El evento no fue eliminado
3. Usa la URL correcta: `/api/v1/events/{ID}/register`

**¿Puedo cambiar mis datos después de registrarme?**

Actualmente no puede editar su registro. Si necesita hacer cambios:
1. Cancele su registro actual
2. Registrese nuevamente con los datos correctos

Esta funcionalidad se añadirá en versiones futuras.

**¿Qué significa "Registrado" vs "En lista de espera"?**

- **Registrado:** Tiene un lugar confirmado en el evento
- **En lista de espera:** Está esperando en la lista por si alguien cancela

Actualmente solo se permite "Registrado". La funcionalidad de lista de espera se implementará en futuras versiones.

**¿Recibo confirmación por email?**

No en esta versión. El sistema retorna la confirmación directamente en la API. La integración de emails se planea para versiones futuras.

**¿Cuánto tiempo antes debo registrarme?**

Puede registrarse en cualquier momento antes de la hora del evento. Sin embargo, no hay límite mínimo. Recomendamos registrarse con anticipación para asegurar su lugar.

**¿Qué pasa si dos personas intentan registrarse al mismo tiempo?**

El sistema maneja esto automáticamente:
- La primera persona en completar el registro obtiene el lugar
- La segunda persona recibe el mensaje "El evento ha alcanzado su capacidad máxima"
- Todo esto ocurre en milisegundos — quien sea más rápido gana

---

## Funcionalidades relacionadas

- [Listar Eventos](./issue-7-consulta-eventos.md) — Ver todos los eventos disponibles

---

**Documento generado:** 2026-06-24  
**Versión API:** v1  
**Estado:** En producción (Sprint 1)
