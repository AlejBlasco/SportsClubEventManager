# Despliegue automatizado al homelab — Documentación Funcional

## What this does

Cuando se publica una nueva versión del sitio web o del servicio de API, esa versión se despliega automáticamente en el servidor propio del equipo (el "homelab"). Hasta ahora, ese despliegue consistía en avisar al servidor de que había una versión nueva y confiar en que todo hubiera ido bien, sin ninguna comprobación posterior.

Con esta mejora, cada despliegue al homelab pasa por tres pasos adicionales, automáticos:

1. **Comprobación real de que la aplicación quedó funcionando.** Justo después del despliegue, el sistema consulta la aplicación ya desplegada (no una copia de prueba) y confirma que responde correctamente.
2. **Registro de qué versión quedó desplegada y cuándo.** Cada despliegue que pasa la comprobación anterior queda marcado de forma permanente como "esta es la versión que funcionaba en tal fecha".
3. **Posibilidad de deshacer un despliegue problemático con un único comando**, sin tener que entrar a ningún panel de administración a cambiar configuraciones a mano.

## Why it matters

Antes de esta mejora existían dos puntos débiles en el proceso:

- **Un despliegue roto podía pasar desapercibido.** El sistema avisaba al servidor de que había una versión nueva, pero no comprobaba después si esa versión realmente había arrancado bien. Un fallo solo se detectaba si alguien lo notaba usando la aplicación.
- **Recuperar una versión anterior era un proceso manual.** Si algo salía mal, había que entrar al panel de administración del servidor y cambiar configuraciones a mano para volver atrás, un proceso propenso a errores y que dependía de que la persona disponible supiera exactamente qué hacer.

Con esta mejora:

- **Los despliegues rotos se detectan solos, en cuestión de minutos**, en lugar de depender de que alguien note el problema por casualidad.
- **Recuperar la última versión que funcionaba es cuestión de un único comando**, no de un procedimiento manual en un panel de administración.
- **Queda un historial visible de qué versión estuvo desplegada y cuándo**, disponible para consulta sin tener que preguntar a nadie ni revisar registros dispersos.

## How it works (user perspective)

Desde la perspectiva del equipo, así se comporta el proceso en el día a día:

```mermaid
flowchart TD
    A[Se publica una nueva\nversión de la aplicación] --> B[El servidor del homelab\nse actualiza automáticamente]
    B --> C{¿La aplicación responde\ncorrectamente tras\nactualizarse?}
    C -- Sí --> D[Se registra esta versión\ncomo "funcionando correctamente"]
    D --> E[Fin: todo sigue normal,\nsin intervención de nadie]

    C -- No --> F[Se genera una alerta automática\ncon la versión anterior que sí funcionaba]
    F --> G{¿Alguien del equipo\nlanza la recuperación?}
    G -- Sí, con un comando --> H[El servidor vuelve automáticamente\na la última versión que funcionaba]
    H --> I{¿Esa versión anterior\nresponde correctamente?}
    I -- Sí --> J[Fin: servicio recuperado]
    I -- No --> K[Se requiere intervención manual\nsegún el procedimiento documentado]
```

En el caso habitual (la inmensa mayoría de despliegues), nadie del equipo tiene que hacer nada: la aplicación se actualiza, se comprueba sola, y queda registrada como correcta. Solo si algo falla entra en juego la alerta y, si se decide recuperar la versión anterior, un único comando se encarga de todo el proceso.

## Implicaciones de proceso

- **El historial de despliegues es ahora visible.** La pestaña "Environments" del repositorio de código muestra, para el entorno del homelab, cada despliegue realizado y si terminó en éxito o en fallo — no hace falta preguntar a nadie ni revisar manualmente si una versión concreta llegó a desplegarse bien.
- **Recuperar una versión anterior ya no requiere entrar al panel de administración del servidor.** Basta con ejecutar un comando indicando a qué versión anterior volver; el propio sistema se encarga de aplicar el cambio y de comprobar que la recuperación funcionó. El procedimiento manual en el panel de administración sigue documentado como alternativa, por si en algún momento el mecanismo automático no estuviera disponible.
- **Quedan pasos de configuración inicial pendientes, a cargo de la persona responsable del servidor del homelab.** Este cambio implementa el mecanismo completo, pero para que funcione de verdad hace falta que alguien con acceso al servidor complete, una única vez, la configuración de acceso entre el repositorio de código y el panel de administración del servidor (crear ciertas claves de acceso y cargarlas de forma segura). Hasta que eso se complete, el despliegue automático al homelab sigue funcionando exactamente igual que antes de esta mejora (sin la verificación ni la recuperación automática todavía activas).
- **Sin cambios para las personas usuarias finales de la plataforma.** Esta mejora no añade ninguna pantalla ni funcionalidad visible en el sitio web ni en la API; es un cambio interno del proceso de publicación y mantenimiento.

## Frequently Asked Questions

**¿Esto significa que un despliegue nunca más podrá romperse?**
No lo garantiza al cien por cien, pero cambia radicalmente cómo se detecta y se resuelve: antes, un despliegue roto podía pasar desapercibido; ahora se detecta en minutos y la recuperación es prácticamente inmediata.

**¿Quién decide cuándo recuperar una versión anterior?**
Cualquier persona del equipo de desarrollo con acceso al repositorio de código puede lanzar la recuperación, en cuanto vea la alerta de que un despliegue falló (o simplemente si decide que hace falta volver atrás por otro motivo).

**¿Cuánto tarda en detectarse un despliegue roto?**
El sistema comprueba la aplicación varias veces durante alrededor de un minuto y medio tras cada despliegue. Si en ese margen la aplicación no responde correctamente, se genera la alerta.

**¿Y si la recuperación automática tampoco funciona?**
Es un escenario contemplado, aunque poco frecuente: en ese caso el sistema no reintenta indefinidamente ni intenta cosas por su cuenta, sino que indica que hace falta seguir el procedimiento manual ya documentado, para evitar dejar el servicio en un estado peor.

**¿Cuándo estará esto completamente activo?**
En cuanto la persona responsable del servidor del homelab complete la configuración inicial de acceso pendiente (un paso único, no repetitivo). Mientras tanto, el despliegue sigue funcionando como hasta ahora, sin la verificación ni la recuperación automática todavía activas.
