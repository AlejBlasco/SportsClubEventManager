# Roles de Usuario — Guía del Usuario

**Versión:** 2026-07-06  
**Aplicable a:** Sports Club Event Manager (Web y API)

---

## ¿Qué es esta funcionalidad?

El sistema de **Roles de Usuario** permite diferenciar entre usuarios normales y administradores dentro de Sports Club Event Manager. Los administradores tienen permisos adicionales para gestionar eventos, usuarios y configuración del sistema, mientras que los usuarios normales solo pueden gestionar sus propias actividades (inscripciones en eventos, consulta de información, etc.).

Esta funcionalidad garantiza que las acciones sensibles (como eliminar eventos o modificar cuentas de otros usuarios) solo puedan ser realizadas por personal autorizado, mejorando la seguridad y organización del sistema.

**Beneficios:**
- **Seguridad**: Las funciones administrativas están protegidas y solo accesibles para administradores
- **Organización**: Separación clara entre usuarios del club y personal administrativo
- **Auditoría**: Todos los intentos de acceso no autorizado quedan registrados en el sistema
- **Escalabilidad**: Base para futuros permisos más granulares (gestores de eventos, coordinadores, etc.)

---

## Roles Disponibles

### Usuario (User)

**Descripción:** Rol por defecto asignado a todos los miembros del club deportivo.

**Permisos:**
- ✅ Ver listado de eventos disponibles
- ✅ Inscribirse en eventos
- ✅ Cancelar sus propias inscripciones
- ✅ Ver detalles de eventos
- ✅ Actualizar su propio perfil
- ✅ Cerrar sesión
- ❌ **No puede:** Inscribir a otros usuarios
- ❌ **No puede:** Acceder al panel de administración
- ❌ **No puede:** Modificar o eliminar eventos
- ❌ **No puede:** Cambiar roles de otros usuarios

**¿Quién recibe este rol?**
- Todos los usuarios que se registran por primera vez (registro local o Google)
- Todos los usuarios existentes antes de la implementación de roles (asignación automática)

---

### Administrador (Administrator)

**Descripción:** Rol especial asignado al personal administrativo del club deportivo.

**Permisos:**
- ✅ **Todos los permisos de Usuario**, más:
- ✅ Inscribir a cualquier usuario en eventos (ej. inscripciones telefónicas)
- ✅ Cancelar inscripciones de cualquier usuario
- ✅ Acceder al panel de administración (`/admin`)
- ✅ Ver listado de usuarios (próximamente)
- ✅ Cambiar roles de usuarios (próximamente)
- ✅ Gestionar configuración del sistema (próximamente)

**¿Quién recibe este rol?**
- El usuario administrador inicial creado automáticamente (`admin@sportsclub.local`)
- Usuarios promovidos manualmente por otro administrador (requiere contacto con soporte técnico en la versión actual)

---

## Cómo Usar los Roles

### Para Usuarios Normales

#### 1. Registro e Inicio de Sesión

Al registrarse (ya sea con email/contraseña o con Google), automáticamente recibirás el rol de **Usuario**.

**Inicio de sesión:**
1. Accede a la aplicación web
2. Haz clic en **"Iniciar Sesión"**
3. Introduce tu email y contraseña, o usa **"Continuar con Google"**
4. Serás redirigido a la página principal

**Qué verás:**
- Tu nombre en la barra superior
- Menú de navegación estándar (Eventos, Mi Perfil, Cerrar Sesión)
- **No verás:** Enlace "Administración" en el menú

#### 2. Inscribirte en un Evento

1. Navega a la sección **Eventos**
2. Selecciona un evento de la lista
3. Haz clic en **"Inscribirme"**
4. Confirma tu inscripción

**Restricción importante:** Solo puedes inscribirte a ti mismo. Si necesitas inscribir a otra persona (ej. familiar, amigo), esa persona debe crear su propia cuenta o contactar con un administrador.

#### 3. Cancelar una Inscripción

1. Navega a **Mi Perfil** → **Mis Eventos**
2. Localiza el evento que deseas cancelar
3. Haz clic en **"Cancelar Inscripción"**
4. Confirma la cancelación

**Nota:** Solo puedes cancelar tus propias inscripciones. No verás opciones para cancelar inscripciones de otros usuarios.

#### 4. Intentar Acceder a Funciones de Administrador

Si intentas acceder a una URL de administración (ej. `/admin/users`) manualmente o mediante un enlace:

1. El sistema detectará que no tienes permisos de administrador
2. Serás redirigido a una **página de acceso denegado** (error 403)
3. Verás un mensaje: *"No tienes permiso para acceder a este recurso"*
4. Podrás regresar a la página principal con el botón **"Volver al Inicio"**

**Este comportamiento es normal y esperado** — protege funciones sensibles del sistema.

---

### Para Administradores

#### 1. Inicio de Sesión como Administrador

**Usuario administrador por defecto:**
- **Email:** `admin@sportsclub.local`
- **Contraseña:** Configurada por el equipo técnico (solicitar al soporte si no la conoces)

**Pasos:**
1. Accede a la aplicación web
2. Haz clic en **"Iniciar Sesión"**
3. Introduce el email y contraseña de administrador
4. Serás redirigido a la página principal

**Qué verás (diferente de usuarios normales):**
- Enlace **"Administración"** en el menú de navegación superior
- Indicador visual de rol de administrador (próximamente)

#### 2. Acceder al Panel de Administración

1. Haz clic en **"Administración"** en el menú superior
2. Accederás a `/admin/users` (gestión de usuarios)

**Estado actual:** En esta versión, el panel de administración muestra una página de **"Próximamente"** con las funcionalidades planificadas:
- Ver todos los usuarios registrados
- Asignar y modificar roles de usuarios
- Activar y desactivar cuentas de usuario
- Ver logs de actividad de usuarios

**Estas funcionalidades se implementarán en los próximos sprints.**

#### 3. Inscribir a Otro Usuario en un Evento

Como administrador, puedes inscribir a cualquier usuario en un evento (útil para inscripciones telefónicas o presenciales).

**Pasos:**
1. Navega a **Eventos**
2. Selecciona el evento deseado
3. Haz clic en **"Inscribir Usuario"** (opción solo visible para administradores)
4. Busca el usuario por email o nombre
5. Confirma la inscripción

**Nota:** El usuario inscrito recibirá una notificación por email (próximamente).

#### 4. Cancelar Inscripción de Otro Usuario

Como administrador, puedes cancelar inscripciones de cualquier usuario (útil para gestionar cancelaciones telefónicas o situaciones especiales).

**Pasos:**
1. Navega al evento correspondiente
2. Ve a la lista de inscritos
3. Localiza al usuario cuya inscripción deseas cancelar
4. Haz clic en **"Cancelar Inscripción"**
5. Confirma la acción

**Recomendación:** Informar al usuario afectado sobre la cancelación.

---

## Qué Esperar

### Como Usuario Normal

**Experiencia típica:**
- Navegación fluida entre eventos y tu perfil
- Inscripciones y cancelaciones instantáneas
- Mensajes claros si intentas acceder a funciones no permitidas
- Sin necesidad de contactar a soporte para operaciones habituales

**Mensaje de acceso denegado:**
Si ves la página de error 403 ("Acceso Denegado"), significa que intentaste acceder a una función de administrador. Esto **no es un error** — es el sistema protegiendo información sensible. Simplemente regresa a la página principal y continúa usando las funciones disponibles para usuarios normales.

### Como Administrador

**Experiencia típica:**
- Acceso completo a todas las funciones de usuarios normales
- Enlace adicional "Administración" en el menú
- Capacidad de realizar acciones en nombre de otros usuarios
- Logs automáticos de todas tus acciones administrativas

**Responsabilidad:**
Como administrador, tus acciones afectan a otros usuarios. El sistema registra todos los accesos administrativos para auditoría. Usa tus permisos de manera responsable.

---

## Limitaciones Actuales

### 1. No hay Auto-Gestión de Roles

**Limitación:** Los usuarios no pueden solicitar o cambiar su propio rol dentro de la aplicación.

**Workaround:** Si un miembro del personal del club necesita ser promovido a administrador:
1. Contactar con el administrador actual o soporte técnico
2. Proporcionar el email de la cuenta del club
3. El administrador realizará la promoción mediante base de datos (temporalmente) o esperar a la implementación de la UI de gestión de roles

**Próximamente:** Interface de gestión de usuarios en el panel de administración permitirá a administradores promover/degradar usuarios.

### 2. Solo Dos Roles

**Limitación:** Actualmente solo existen dos roles (Usuario y Administrador). No hay roles intermedios como:
- Coordinador de eventos (puede crear/modificar eventos pero no gestionar usuarios)
- Gestor de inscripciones (puede inscribir usuarios pero no modificar configuración)
- Auditor (puede ver logs pero no modificar datos)

**Próximamente:** Roles adicionales y permisos granulares se añadirán en futuros sprints según necesidades del club.

### 3. Un Solo Administrador Inicial

**Limitación:** El sistema crea solo un administrador (`admin@sportsclub.local`) durante la instalación. Administradores adicionales deben ser promovidos manualmente.

**Recomendación:** Promover a un segundo administrador de respaldo tan pronto como sea posible para evitar bloqueos si el administrador principal no está disponible.

### 4. Sin Cambio de Contraseña Self-Service

**Limitación:** El administrador inicial no puede cambiar su contraseña mediante la UI (debe ser cambiada mediante configuración del servidor).

**Recomendación de Seguridad:** Cambiar la contraseña del administrador inmediatamente después de la instalación en producción. Contactar con soporte técnico para asistencia.

**Próximamente:** Funcionalidad de "Cambiar Contraseña" disponible en el perfil de usuario.

---

## Preguntas Frecuentes

**¿Cómo sé qué rol tengo?**

Actualmente, no hay un indicador visual directo en la interfaz. Puedes saberlo de las siguientes maneras:
- Si ves el enlace **"Administración"** en el menú → eres Administrador
- Si **no** ves ese enlace → eres Usuario normal
- Próximamente: Badge visible en tu perfil mostrando tu rol

**¿Puedo tener múltiples roles?**

No. Cada usuario tiene exactamente un rol. En el futuro, se implementarán permisos adicionales que permitirán combinaciones más flexibles (ej. "Usuario con permiso de coordinación de eventos").

**¿Qué pasa si intento hacer algo sin permisos?**

El sistema mostrará una página de **"Acceso Denegado"** (error 403) con un mensaje explicativo. No se ejecutará la acción y el intento quedará registrado en los logs del sistema (sin penalización para el usuario, es solo para auditoría de seguridad).

**¿Puedo solicitar ser promovido a Administrador?**

Sí, contacta con el administrador actual del club o con soporte técnico. Explica por qué necesitas permisos de administrador y proporciona tu email de cuenta. La promoción se realizará tras aprobación.

**¿Cómo puedo promover a otro usuario a Administrador?**

**En esta versión:** Contacta con soporte técnico, quien realizará la promoción mediante base de datos.

**Próximamente (versión 2.x):** Como administrador, podrás:
1. Ir a **Administración** → **Usuarios**
2. Buscar el usuario deseado
3. Hacer clic en **"Promover a Administrador"**
4. Confirmar la acción
5. El usuario recibirá una notificación por email

**¿Los administradores pueden ver mi información personal?**

Sí, los administradores tienen acceso a la información de perfil de todos los usuarios (nombre, email, historial de inscripciones) para poder gestionar el club efectivamente. Sin embargo:
- Las contraseñas están cifradas y **nunca** son visibles para nadie (ni siquiera administradores)
- Los administradores no pueden modificar tu información personal sin tu consentimiento
- Todos los accesos administrativos quedan registrados para auditoría

**¿Qué pasa si olvido mi contraseña?**

**Si eres Usuario normal:**
1. Haz clic en **"¿Olvidaste tu contraseña?"** en la página de login (próximamente)
2. Introduce tu email
3. Recibirás un enlace de restablecimiento por email

**Si eres Administrador:**
1. Si tienes acceso a un segundo administrador, solicítale un restablecimiento
2. Si eres el único administrador, contacta con soporte técnico para restablecimiento seguro

**Si usaste Google OAuth2:**
No hay contraseña — tu cuenta está vinculada a Google. Simplemente haz clic en "Continuar con Google" en el login.

**¿Los usuarios nuevos por Google son automáticamente Administradores?**

No. Todos los usuarios que se registran mediante Google OAuth2 reciben automáticamente el rol de **Usuario** (no Administrador). Si un miembro del personal se registra con Google y necesita permisos de administrador, debe ser promovido manualmente por un administrador existente.

**¿Qué pasa si hay múltiples administradores y uno comete un error?**

Todos los cambios realizados por administradores quedan registrados en los logs del sistema con:
- Fecha y hora exacta de la acción
- Email del administrador que realizó la acción
- Descripción de la acción (ej. "Usuario X inscrito en evento Y")

Esto permite auditar acciones y, si es necesario, revertir cambios mediante soporte técnico.

**¿Los roles afectan a la API REST?**

Sí. Si estás desarrollando una aplicación que consume la API de Sports Club Event Manager, debes tener en cuenta que:
- Los endpoints de administración requieren un JWT con claim de rol "Administrator"
- Intentar acceder a endpoints de administración con rol "User" retornará error 403 Forbidden
- Consulta la documentación técnica de la API para más detalles sobre permisos por endpoint

---

## Funcionalidades Relacionadas

- **Autenticación con Google OAuth2**: Permite registro e inicio de sesión con cuenta de Google (todos los usuarios Google reciben rol User por defecto)
- **Gestión de Eventos**: Creación, modificación y eliminación de eventos (requiere permisos de administrador — próximamente)
- **Inscripciones en Eventos**: Sistema de registro para participar en actividades del club (disponible para todos los usuarios)
- **Auditoría de Seguridad**: Registro automático de intentos de acceso no autorizado (solo visible para administradores — próximamente)

---

## Soporte y Contacto

Si tienes preguntas sobre roles de usuario, permisos o necesitas asistencia para promover/degradar usuarios, contacta con:

**Soporte Técnico:**
- Email: soporte@sportsclub.local
- Teléfono: [pendiente de configuración]

**Administrador del Sistema:**
- Email: admin@sportsclub.local

**Documentación Adicional:**
- Documentación técnica para desarrolladores: `/docs/technical/issue-28-autorizacion-basada-en-roles.md`
- Guía de instalación: `/docs/deployment/installation-guide.md` (próximamente)

---

**Guía actualizada:** 2026-07-06  
**Versión del sistema:** 2.0 (con soporte de roles)  
**Próximas actualizaciones:** UI de gestión de roles (Sprint 3), cambio de contraseña self-service (Sprint 3), permisos granulares (Sprint 4)
