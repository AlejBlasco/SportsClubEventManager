# Modelo de Dominio — Guía Funcional
**Versión:** 2026-06-22  
**Aplicación:** Sports Club Event Manager

---

## ¿Qué es esta funcionalidad?

El modelo de dominio es la base técnica que permite que la aplicación Sports Club Event Manager gestione eventos deportivos e inscripciones de usuarios. Aunque esta funcionalidad no tiene una interfaz visible para el usuario final, establece las reglas fundamentales de cómo funcionan los eventos, las inscripciones y los usuarios en el sistema.

Esta actualización beneficia principalmente a los desarrolladores y futuros módulos del sistema, asegurando que todas las reglas de negocio estén claramente definidas y sean consistentes en toda la aplicación.

---

## Conceptos Principales

### Evento (Event)

Un evento representa una actividad deportiva organizada por el club. Cada evento tiene:

- **Título:** Nombre del evento (ejemplo: "Carrera Popular Primavera 2026")
- **Descripción:** Información adicional sobre el evento
- **Fecha y hora:** Cuándo se realizará el evento
- **Ubicación:** Dónde tendrá lugar
- **Capacidad máxima:** Número máximo de participantes permitidos

**Reglas automáticas:**
- La capacidad máxima debe ser al menos 1 persona
- Los eventos nuevos deben programarse para el futuro (no se pueden crear eventos en el pasado)
- El sistema calcula automáticamente cuántas inscripciones activas tiene cada evento
- El evento se marca como "completo" cuando se alcanza la capacidad máxima

### Usuario (User)

Representa a una persona que puede inscribirse en eventos. La información incluye:

- **Nombre:** Nombre completo del usuario
- **Email:** Dirección de correo electrónico (única en todo el sistema)
- **Género:** Masculino, Femenino u Otro
- **Número de licencia:** (Opcional) Número de licencia deportiva
- **Categoría de licencia:** (Opcional) Categoría de la licencia

**Reglas automáticas:**
- El email debe tener un formato válido (debe contener @ y un dominio)
- No pueden existir dos usuarios con el mismo email
- Los campos de licencia son opcionales para facilitar inscripciones rápidas

### Inscripción (Registration)

Vincula a un usuario con un evento. Cada inscripción tiene:

- **Fecha de inscripción:** Cuándo se realizó la inscripción
- **Estado:** Puede ser Registrado, Cancelado o En Lista de Espera

**Reglas automáticas:**
- Un usuario no puede inscribirse dos veces al mismo evento (evita duplicados)
- Las inscripciones canceladas permanecen en el sistema para mantener el historial
- Solo las inscripciones activas (no canceladas) cuentan para la capacidad del evento
- Un usuario puede cancelar su inscripción en cualquier momento

---

## Estados de Inscripción

El sistema maneja tres estados posibles para cada inscripción:

1. **Registrado (Registered):**
   - Estado normal de una inscripción activa
   - Cuenta para la capacidad del evento
   - El usuario está confirmado como participante

2. **Cancelado (Cancelled):**
   - El usuario canceló su participación
   - No cuenta para la capacidad del evento
   - Se conserva el historial de la cancelación
   - El usuario puede volver a inscribirse si el evento no está completo

3. **En Lista de Espera (Waitlisted):**
   - Funcionalidad futura (no implementada aún)
   - Permitirá gestionar usuarios en espera cuando un evento esté completo

---

## Protecciones Implementadas

El sistema incluye las siguientes protecciones automáticas:

### 1. Validación de Capacidad
- El sistema verifica automáticamente si un evento puede aceptar más inscripciones
- Previene exceder la capacidad máxima configurada
- Considera solo inscripciones activas (excluye las canceladas)

### 2. Prevención de Duplicados
- Un usuario no puede inscribirse múltiples veces al mismo evento
- Si un usuario cancela, puede volver a inscribirse si hay capacidad

### 3. Validación de Datos
- **Email:** Debe tener formato válido
- **Capacidad:** Debe ser al menos 1 persona
- **Fechas:** Los eventos nuevos deben ser futuros

### 4. Auditoría Básica
- Todas las entidades (eventos, usuarios, inscripciones) registran:
  - Fecha de creación
  - Fecha de última modificación
- Esta información ayuda a rastrear cambios en el sistema

---

## Limitaciones Actuales

Esta es la primera versión del modelo de dominio (MVP - Producto Mínimo Viable). Las siguientes limitaciones son conocidas y se resolverán en futuras actualizaciones:

1. **Sin autenticación de usuarios**
   - Aún no hay login ni gestión de contraseñas
   - Planificado para Sprint 2

2. **Sin roles o permisos**
   - Todos los usuarios tienen los mismos privilegios
   - Planificado para Sprint 2 (por ejemplo: administradores, organizadores, participantes)

3. **Sin gestión de lista de espera**
   - El estado "En Lista de Espera" está preparado pero no funcional aún
   - Se implementará en una historia futura

4. **Sin notificaciones**
   - El sistema no envía emails de confirmación o recordatorios
   - Funcionalidad futura

5. **Sin categorías de eventos**
   - Los eventos no se pueden clasificar por tipo (carreras, torneos, entrenamientos, etc.)
   - Funcionalidad futura

6. **Sin recurrencia de eventos**
   - No se pueden crear eventos repetitivos (semanales, mensuales, etc.)
   - Funcionalidad futura

---

## Preguntas Frecuentes

**¿Por qué no puedo ver esta funcionalidad en la aplicación?**

Esta historia implementa el "backend" (la lógica interna del sistema). La interfaz de usuario que permitirá a los usuarios interactuar con eventos e inscripciones se desarrollará en historias posteriores.

**¿Qué significa "Clean Architecture"?**

Es un patrón de diseño de software que separa claramente la lógica de negocio (reglas sobre eventos, usuarios e inscripciones) de la tecnología utilizada (bases de datos, interfaces web, etc.). Esto facilita el mantenimiento y la evolución futura del sistema.

**¿Por qué se conservan las inscripciones canceladas?**

Mantener el historial de cancelaciones permite:
- Análisis de tendencias (por ejemplo, qué eventos tienen más cancelaciones)
- Auditoría completa de todas las acciones
- Posibilidad de restaurar una inscripción si fue cancelada por error

**¿Cuándo estará disponible la interfaz de usuario?**

La interfaz web y la API REST se desarrollarán en los próximos sprints. Esta historia sienta las bases técnicas necesarias para esas funcionalidades.

**¿Qué pasa si intento inscribirme a un evento completo?**

El sistema rechazará la inscripción automáticamente. En futuras versiones, se ofrecerá la opción de unirse a una lista de espera.

**¿Puedo cambiar mi email después de registrarme?**

Técnicamente sí, pero dado que el email es único en el sistema, no podrás usar uno que ya esté en uso por otro usuario. La funcionalidad de edición de perfil se implementará en la capa de Application/API.

**¿Por qué el género solo tiene tres opciones?**

Esta fue una decisión de diseño aprobada con los stakeholders. Si en el futuro se requieren más opciones (por ejemplo, "Prefiero no decir"), el sistema está preparado para extenderse fácilmente.

**¿Qué significa "Guid" en el contexto técnico?**

Es el tipo de identificador único que el sistema usa internamente para cada evento, usuario e inscripción. Es un código alfanumérico que garantiza que cada registro sea único a nivel global (ejemplo: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`).

---

## Impacto para Usuarios Finales

**Impacto inmediato:** Ninguno visible (esta es una actualización de backend).

**Impacto futuro:**
- Base sólida para funcionalidades futuras
- Mejor calidad de datos (validaciones automáticas)
- Menor probabilidad de errores (reglas de negocio claras)
- Auditoría completa de acciones
- Sistema preparado para escalar

---

## Impacto para Administradores

**Actualmente:** No hay interfaz de administración en esta fase.

**Futuras capacidades habilitadas por esta actualización:**
- Gestión de eventos con capacidad controlada automáticamente
- Visualización de inscripciones activas vs. canceladas
- Informes basados en datos históricos
- Prevención de errores comunes (duplicados, emails inválidos, capacidades negativas)

---

## Glosario

- **Dominio:** El conjunto de reglas y conceptos del negocio (en este caso, eventos deportivos e inscripciones)
- **Entidad:** Un objeto con identidad única (Event, User, Registration)
- **Validación:** Verificación automática de que los datos cumplen las reglas
- **Auditoría:** Registro de cuándo se crearon o modificaron los datos
- **Capacidad:** Número máximo de participantes permitidos en un evento
- **Inscripción activa:** Inscripción no cancelada que cuenta para la capacidad del evento

---

## Funcionalidades Relacionadas

*Ninguna disponible aún — esta es la primera historia implementada del proyecto.*

**Próximas funcionalidades:**
- Infraestructura de base de datos (persistencia de datos)
- API REST para gestión de eventos e inscripciones
- Interfaz web para usuarios finales
- Autenticación y autorización

---

**Documento generado por:** Documentation Agent  
**Fecha:** 2026-06-22  
**Versión:** 1.0
