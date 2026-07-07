# Gestión de Perfil de Usuario — Guía del Usuario
**Versión:** 2026-07-07  
**Aplica a:** SportsClubEventManager (Aplicación Web)  

---

## ¿Qué es esta funcionalidad?

La gestión de perfil te permite ver y actualizar tu información personal registrada en el sistema del club deportivo. Puedes modificar tu nombre, género, email (si usas usuario y contraseña), número de licencia y categoría de licencia. Si iniciaste sesión con tu cuenta de Google, algunos campos estarán protegidos porque son gestionados por Google.

Esta funcionalidad te ayuda a mantener tus datos actualizados para que el club pueda contactarte correctamente y para que tu información de licencia esté siempre al día.

---

## Cómo usarla

### Paso 1: Acceder a tu perfil

1. Inicia sesión en la aplicación con tu usuario y contraseña, o con tu cuenta de Google
2. En la barra superior de navegación, haz clic en tu nombre (esquina superior derecha)
3. Selecciona **"Mi Perfil"** en el menú desplegable

**Verás una página con todos tus datos personales:**
- Nombre
- Género
- Email
- Número de Licencia
- Categoría de Licencia
- Rol en el club (por ejemplo: "Miembro", "Administrador")
- Fecha de registro

---

### Paso 2: Editar tu información

**Para usuarios con cuenta local (usuario y contraseña):**

1. Modifica cualquiera de los campos editables:
   - **Nombre:** Tu nombre completo (mínimo 2 caracteres, máximo 100)
   - **Género:** Selecciona Masculino, Femenino u Otro
   - **Email:** Tu dirección de correo electrónico (debe ser única en el sistema)
   - **Número de Licencia:** Tu número de licencia deportiva (opcional)
   - **Categoría de Licencia:** La categoría de tu licencia, por ejemplo "B" (opcional)

2. Haz clic en **"Guardar"** para aplicar los cambios

3. Verás un mensaje de confirmación: **"Perfil actualizado correctamente"**

**Para usuarios con cuenta de Google:**

- Tu **email** aparecerá como solo lectura con la nota: *"Email gestionado por Google"*
- No podrás modificar tu email aquí porque está vinculado a tu cuenta de Google
- Los demás campos (Nombre, Género, Número de Licencia, Categoría) **sí puedes editarlos**

---

### Paso 3: Cambiar tu contraseña (solo cuentas locales)

Si iniciaste sesión con usuario y contraseña (no con Google), puedes cambiar tu contraseña:

1. En la página de perfil, haz clic en el botón **"Cambiar Contraseña"**

2. Se abrirá una ventana emergente con tres campos:
   - **Contraseña actual:** Escribe tu contraseña actual para confirmar tu identidad
   - **Nueva contraseña:** Escribe tu nueva contraseña (mínimo 8 caracteres)
   - **Confirmar nueva contraseña:** Repite la nueva contraseña

3. Haz clic en **"Cambiar Contraseña"**

4. Si todo es correcto, verás el mensaje: **"Contraseña cambiada correctamente"**

5. El sistema generará nuevos tokens de autenticación automáticamente (no necesitas volver a iniciar sesión)

**Nota importante:** Si iniciaste sesión con Google, el botón de cambiar contraseña no aparecerá, porque tu contraseña es gestionada por Google.

---

## Qué esperar

### Después de actualizar tu perfil

- Tus cambios se guardan inmediatamente en la base de datos
- El sistema actualiza la fecha de "Última actualización" de tu perfil
- Recibirás un mensaje de confirmación visual en pantalla
- Si modificaste tu email, este se aplicará de inmediato (asegúrate de escribirlo correctamente)
- Puedes seguir usando la aplicación normalmente

### Después de cambiar tu contraseña

- Tu contraseña anterior deja de ser válida
- Recibirás nuevos tokens de autenticación (esto ocurre automáticamente)
- Tus sesiones activas en otros dispositivos **seguirán funcionando** (no se cierran automáticamente)
- La próxima vez que inicies sesión, deberás usar tu nueva contraseña

---

## Validaciones y mensajes de error

### Errores comunes al editar el perfil

| Error | Causa | Solución |
|-------|-------|----------|
| "El nombre es obligatorio" | Dejaste el campo Nombre vacío | Escribe tu nombre completo |
| "El nombre debe tener al menos 2 caracteres" | Nombre demasiado corto | Escribe tu nombre completo |
| "El email ya está registrado" | Otro usuario usa ese email | Usa un email diferente o contacta al administrador |
| "Email gestionado por Google" | Intentas cambiar el email de una cuenta Google | Tu email solo puede cambiarse desde tu cuenta de Google |
| "Formato de email inválido" | El email no tiene formato correcto | Verifica que tenga @ y dominio válido (ejemplo: usuario@dominio.com) |

### Errores comunes al cambiar contraseña

| Error | Causa | Solución |
|-------|-------|----------|
| "La contraseña actual es incorrecta" | Escribiste mal tu contraseña actual | Verifica tu contraseña actual e inténtalo de nuevo |
| "La nueva contraseña debe tener al menos 8 caracteres" | Contraseña demasiado corta | Elige una contraseña de 8 caracteres o más |
| "Las contraseñas no coinciden" | Nueva contraseña y confirmación son diferentes | Escribe la misma contraseña en ambos campos |
| "Contraseña gestionada por Google" | Intentas cambiar contraseña con cuenta Google | Tu contraseña solo puede cambiarse desde Google |

---

## Limitaciones

- **Email sin verificación:** Cuando cambias tu email, el cambio se aplica de inmediato sin enviarte un correo de confirmación. **Importante:** Asegúrate de escribir correctamente tu nuevo email, porque si cometes un error, no podrás recibir correos del sistema.

- **Rol no editable:** Tu rol en el club (Miembro, Administrador, etc.) solo puede ser cambiado por un administrador del sistema. Si necesitas un cambio de rol, contacta al administrador.

- **Cambios simultáneos:** Si tienes el perfil abierto en dos pestañas del navegador y guardas cambios en ambas, el último cambio guardado sobrescribirá el anterior (last-write-wins). Para evitar perder información, edita tu perfil desde una sola pestaña.

- **Sesiones activas tras cambio de contraseña:** Si cambias tu contraseña, tus sesiones en otros dispositivos (móvil, tablet, otra computadora) NO se cerrarán automáticamente. Si sospechas que alguien accedió a tu cuenta, cambia tu contraseña y cierra sesión en todos los dispositivos manualmente.

---

## Preguntas Frecuentes

**¿Puedo cambiar mi rol de Miembro a Administrador?**  
No, los roles son gestionados por los administradores del club. Si necesitas un cambio de rol, contacta al administrador del sistema.

**¿Por qué no puedo cambiar mi email si uso Google?**  
Porque tu email está vinculado a tu cuenta de Google. Si necesitas usar un email diferente, deberás iniciar sesión con ese nuevo email de Google (o crear una cuenta local con usuario y contraseña).

**¿Qué pasa si escribo mal mi email al cambiarlo?**  
El cambio se aplicará inmediatamente, así que es muy importante que revises bien antes de guardar. Si cometes un error y pierdes acceso a tu cuenta, contacta al administrador del club para que corrija tu email.

**¿Puedo dejar vacíos Número de Licencia y Categoría?**  
Sí, ambos campos son opcionales. Sin embargo, es recomendable llenarlos si tienes una licencia deportiva, para que tu información esté completa en el sistema del club.

**¿Qué tan segura es mi contraseña?**  
Las contraseñas se almacenan de forma segura usando hashing BCrypt con factor de trabajo 12, un estándar robusto de la industria. Tu contraseña nunca se guarda en texto plano. Actualmente, la política requiere un mínimo de 8 caracteres. Se recomienda usar contraseñas largas y únicas.

**¿Cuántas veces puedo cambiar mi contraseña?**  
No hay límite de cambios, pero si ingresas mal la contraseña actual varias veces, el sistema registrará los intentos fallidos (esto ayuda a detectar intentos de acceso no autorizados).

**¿Qué es "Última actualización" que aparece en mi perfil?**  
Es la fecha y hora en que se modificó tu perfil por última vez (ya sea por ti o por un administrador). Si nunca has editado tu perfil, mostrará tu fecha de registro.

**¿Puedo cancelar los cambios después de hacer clic en Guardar?**  
No, los cambios se aplican de inmediato. Si deseas deshacer un cambio, deberás volver a editar el perfil y escribir los valores anteriores. **Tip:** Antes de guardar, revisa que todos los datos sean correctos.

---

## Funcionalidades Relacionadas

- [Autenticación OAuth2 con Google](./US-27-oauth-authentication.md) — Cómo iniciar sesión con tu cuenta de Google
- [Gestión de Roles y Permisos](./US-28-role-based-authorization.md) — Información sobre roles y permisos en el sistema
- (Próximamente) Verificación de Email — En una futura actualización, podrás verificar cambios de email con un código de confirmación

---

**Documento generado por:** Documentation Agent  
**Fecha:** 2026-07-07  
**Versión del sistema:** US-29 (Sprint 2)
