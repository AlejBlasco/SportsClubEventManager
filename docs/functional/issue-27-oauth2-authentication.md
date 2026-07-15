# Autenticación Segura — Guía de Usuario
**Versión:** 2026-06-30  
**Aplicable a:** Sports Club Event Manager

---

## ¿Qué es esta función?

La autenticación segura permite acceder a tu cuenta del Gestor de Eventos del Club de Deportes de dos maneras simples y seguras: usando tu cuenta de Google o con un email y contraseña local. Una vez autenticado, tu sesión permanece activa mientras estés usando la aplicación, y se cierra automáticamente si permaneces inactivo durante 30 minutos. Tu información personal nunca se almacena de manera insegura — todas las contraseñas están encriptadas y tu sesión está protegida contra accesos no autorizados.

---

## Cómo usar

### Opción 1: Iniciar sesión con Google (Recomendado)

#### Paso 1: Accede a la página de Inicio de Sesión
Desde la página de inicio, haz click en **"Iniciar Sesión"** (esquina superior derecha si no estás autenticado).

#### Paso 2: Selecciona Google
En la página de Inicio de Sesión, haz click en el botón **"Iniciar sesión con Google"**.

#### Paso 3: Autentica en Google
Serás redirigido a Google. Sigue estos pasos:
- Si ya has iniciado sesión en Google → Google te pedirá permiso para compartir tu información
- Si no has iniciado sesión → Ingresa tu email y contraseña de Google, luego acepta permisos

#### Paso 4: Aprueba el acceso
Haz click en **"Continuar"** o **"Permitir"** para autorizar que el Gestor de Eventos acceda a tu nombre y correo.

#### Paso 5: Bienvenido
Serás redirigido automáticamente a la página de inicio. Tu nombre aparecerá en la esquina superior derecha. ¡Listo!

---

### Opción 2: Iniciar sesión con Email y Contraseña

#### Paso 1: Accede a la página de Inicio de Sesión
Desde la página de inicio, haz click en **"Iniciar Sesión"** (esquina superior derecha).

#### Paso 2: Completa el formulario
En la sección **"Iniciar sesión"** de la página:
- **Email:** Ingresa tu dirección de correo registrada
- **Contraseña:** Ingresa tu contraseña

#### Paso 3: Valida tu contraseña
Tu contraseña debe tener al menos:
- 8 caracteres de largo
- 1 letra mayúscula (A-Z)
- 1 letra minúscula (a-z)
- 1 número (0-9)
- 1 carácter especial (!@#$%^&*, etc)

Ejemplo de contraseña válida: `MiClub2026!`

#### Paso 4: Envía el formulario
Haz click en **"Iniciar sesión"**.

#### Paso 5: Espera a que se procese
Verás un indicador de carga mientras verificamos tus credenciales. Esto toma 1-2 segundos.

#### Paso 6: Accede a la aplicación
Si tus credenciales son correctas, serás redirigido a la página de inicio. Tu nombre aparecerá en la esquina superior derecha.

---

## Qué esperar

### Sesión Activa

Una vez autenticado:
- Tu nombre aparece en la esquina superior derecha
- Puedes ver un enlace **"Cerrar sesión"** junto a tu nombre
- Tienes acceso a todas las funcionalidades protegidas de la aplicación
- Tu sesión permanece activa mientras estés usando la aplicación

### Cierre Automático de Sesión

Tu sesión se cierra automáticamente después de:
- **30 minutos desde que iniciaste sesión** — actualmente no se renueva automáticamente
- **Hacer click en "Cerrar sesión"** — Cierre manual inmediato

Cuando tu sesión se cierra, serás redirigido a la página de Inicio de Sesión. Deberás volver a autenticarte.

### Recordar Sesión

Tu navegador **recordará que estás autenticado** incluso si cierras la pestaña, siempre y cuando no cierres el navegador completamente. Cuando regreses, simplemente actualiza la página — ya estarás autenticado (sin necesidad de ingresar credenciales nuevamente).

---

## Limitaciones

- **Una contraseña por cuenta:** No puedes tener múltiples contraseñas para la misma cuenta
- **Un dispositivo por sesión:** Cada dispositivo/navegador necesita iniciar sesión por separado. Iniciar sesión en tu laptop no te autentica en tu teléfono
- **Google es más simple:** Usar Google es más fácil que recordar una contraseña — se recomienda
- **No hay "Recordar contraseña" por email:** Si olvidas tu contraseña, contacta al administrador. Google gestiona tu contraseña de Google
- **Cambio de contraseña no automático:** Cambiar contraseña requiere contactar al administrador (futura funcionalidad)

---

## Preguntas Frecuentes

**¿Cómo inicio sesión si no tengo cuenta de Google?**  
Puedes usar email y contraseña local. El administrador creará una cuenta para ti, o pregunta cómo registrarse.

**¿Qué sucede si olvido mi contraseña?**  
Contacta al administrador del club. Se enviarán instrucciones para restablecerla (funcionalidad en desarrollo).

**¿Es seguro usar Google para iniciar sesión?**  
Sí, completamente. Google proporciona seguridad de nivel empresarial. Google NUNCA compartirá tu contraseña con nosotros — solo tu nombre y correo.

**¿Dónde se almacena mi contraseña?**  
Las contraseñas están **encriptadas** en la base de datos usando un algoritmo llamado BCrypt. Incluso los administradores no pueden ver tu contraseña en texto plano.

**¿Qué pasa si alguien accede a mi cuenta?**  
Cierra sesión en todas las partes (haz click en "Cerrar sesión"). Tu refresh token se revoca inmediatamente — cualquier dispositivo que intente usar tu sesión será desconectado.

**¿Puedo iniciar sesión en múltiples dispositivos simultáneamente?**  
Sí. Cada dispositivo necesita iniciar sesión por separado, pero puedes tener sesiones activas en tu laptop, teléfono y tablet al mismo tiempo.

**¿Cuánto tiempo dura mi sesión?**  
Tu sesión es válida durante 30 minutos desde que inicias sesión. Pasado ese tiempo deberás volver a iniciar sesión para seguir usando funciones que requieren autenticación (por ejemplo, tu perfil o tus inscripciones) — actualmente la renovación automática de sesión no está implementada.

**¿Qué es un "Refresh Token"?**  
Un mecanismo de seguridad pensado para renovar tu sesión sin que necesites volver a ingresar tu contraseña. La Api ya lo soporta, pero la aplicación Web todavía no lo utiliza automáticamente — por eso, hoy en día, tu sesión dura 30 minutos y luego debes iniciar sesión de nuevo.

**¿Mi sesión es segura?**  
Sí. Usamos encriptación de nivel bancario (HTTPS), protección contra ataques comunes (CSRF, XSS), y tokens que no pueden ser modificados o robados.

**¿Necesito una contraseña fuerte?**  
Sí, pero no es complicado. Necesitas: mayúsculas, minúsculas, números y un carácter especial. Ejemplo: `ClubEventos2026!` es válida.

**¿Qué pasa si veo un error "Sesión expirada"?**  
Tu sesión se cerró por inactividad o pasó 7 días desde tu último login. Simplemente vuelve a iniciar sesión — es normal y seguro.

---

**Última actualización:** 2026-06-30  
**Próxima revisión:** 2026-07-31
