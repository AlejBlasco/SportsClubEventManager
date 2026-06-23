# Configuración de Base de Datos — Guía Funcional

**Versión:** 2026-06-23  
**Aplica a:** SportsClubEventManager

---

## ¿Qué es esta funcionalidad?

Esta funcionalidad establece la infraestructura de base de datos para la aplicación SportsClubEventManager. Permite que el sistema almacene y gestione de forma permanente la información sobre eventos deportivos, usuarios del club, y las registraciones de los participantes en cada evento.

A nivel técnico, se ha configurado Entity Framework Core con SQL Server para gestionar automáticamente la creación, actualización y eliminación de datos, garantizando la integridad y consistencia de la información almacenada.

**Beneficiarios:**
- **Desarrolladores:** Base de datos lista para usar en futuras funcionalidades
- **Administradores de sistema:** Despliegue simplificado con migraciones automáticas
- **Usuarios finales:** Fundamento técnico que permitirá funcionalidades de gestión de eventos en próximas versiones

---

## ¿Qué información se almacena?

La base de datos gestiona tres tipos principales de información:

### 1. Eventos Deportivos

Cada evento almacena:
- **Título** del evento (ej. "Torneo de Tenis de Mesa")
- **Descripción** detallada (opcional)
- **Fecha y hora** del evento
- **Ubicación** (ej. "Polideportivo Municipal")
- **Capacidad máxima** de participantes
- **Fecha de creación** y **última actualización** (auditoría automática)

### 2. Usuarios del Club

Cada usuario almacena:
- **Nombre completo**
- **Email** (único por usuario)
- **Género** (Masculino, Femenino, Otro)
- **Número de licencia** federativa (opcional)
- **Categoría de licencia** (opcional)
- **Fecha de creación** y **última actualización** (auditoría automática)

### 3. Registraciones a Eventos

Cada registración almacena:
- **Evento** al que se inscribe el usuario
- **Usuario** que se inscribe
- **Fecha de la registración**
- **Estado** (Registrado, Cancelado)
- **Fecha de creación** y **última actualización** (auditoría automática)

---

## ¿Cómo funciona?

### Inicio automático de la base de datos

Cuando la aplicación se ejecuta por primera vez:

1. La aplicación detecta que la base de datos no existe
2. Crea automáticamente la base de datos en SQL Server
3. Genera todas las tablas necesarias (Eventos, Usuarios, Registraciones)
4. Configura las relaciones entre tablas (ej. una registración pertenece a un evento y a un usuario)
5. Crea índices para mejorar la velocidad de búsquedas

Todo este proceso ocurre automáticamente, sin intervención manual.

### Protección de integridad de datos

El sistema implementa las siguientes reglas de protección:

**Regla 1: Emails únicos**
- No se permite registrar dos usuarios con el mismo email
- Garantiza identificación única de cada participante

**Regla 2: Eliminación en cascada de registraciones**
- Si se elimina un evento, todas las registraciones asociadas se eliminan automáticamente
- Evita registraciones huérfanas sin evento

**Regla 3: Protección de usuarios con registraciones**
- No se puede eliminar un usuario que tiene registraciones activas
- Garantiza integridad histórica de los datos

**Regla 4: Auditoría automática**
- Cada vez que se crea un registro, se guarda la fecha de creación
- Cada vez que se modifica un registro, se guarda la fecha de última modificación
- Permite rastrear cuándo se realizaron cambios en la información

---

## Requisitos del Sistema

### Para Desarrolladores

1. **SQL Server Express** debe estar instalado y ejecutándose
   - Instancia configurada: `NF-TRAVEL\SQLEXPRESS`
   - Autenticación: Windows Authentication (Integrated Security)

2. **Entity Framework Core Tools** debe estar instalado:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

3. **Docker** (opcional, solo para pruebas de integración):
   - Requerido únicamente para ejecutar los 24 tests de integración
   - No es necesario para ejecución normal de la aplicación

### Para Producción

1. **SQL Server** (versión 2019 o superior)
2. **Connection string** debe configurarse en Azure Key Vault o variables de entorno (no en archivos de configuración)
3. **Pipeline CI/CD** debe incluir paso de migración de base de datos antes del despliegue

---

## Limitaciones

- **Eliminación permanente:** Cuando se elimina un evento o usuario, los datos se borran permanentemente (no hay "papelera de reciclaje"). En futuras versiones se puede implementar eliminación lógica si se requiere.

- **Auto-migración en producción:** En entornos con múltiples instancias de la aplicación (ej. Azure App Service con escalado), la migración automática puede causar conflictos. Se recomienda desactivarla en producción y migrar como paso independiente en el pipeline.

- **Connection string en archivos locales:** Para desarrollo local, el connection string está en `appsettings.json`. Esto es seguro porque usa autenticación de Windows (sin contraseña), pero en producción debe estar en Azure Key Vault.

- **Validaciones de negocio futuras:** Actualmente, la base de datos garantiza integridad estructural (ej. campos requeridos, emails únicos), pero no implementa validaciones de negocio complejas (ej. "no permitir registración si el evento está lleno"). Estas validaciones se implementarán en historias futuras.

---

## Preguntas Frecuentes

**¿Qué sucede si la aplicación se inicia sin SQL Server ejecutándose?**

La aplicación mostrará un error de conexión y no iniciará. El error indicará claramente que no se puede conectar a SQL Server. Solución: Iniciar SQL Server Express y volver a ejecutar la aplicación.

---

**¿Puedo cambiar la estructura de la base de datos después de crearla?**

Sí. Si se modifican las entidades del dominio (ej. agregar un nuevo campo a Event), se debe generar una nueva migración con:
```bash
dotnet ef migrations add {NombreDeLaMigracion} --project Infrastructure --startup-project Web
```
La próxima vez que se inicie la aplicación, los cambios se aplicarán automáticamente.

---

**¿Se pueden perder datos si se ejecuta una migración?**

Las migraciones están diseñadas para ser seguras y reversibles. Sin embargo, si una migración incluye eliminación de columnas o tablas, los datos se perderán. Antes de aplicar migraciones en producción, se recomienda:
1. Hacer backup de la base de datos
2. Revisar el script SQL generado por la migración
3. Aplicar en entorno de prueba primero

---

**¿Por qué no se incluyen datos de ejemplo al crear la base de datos?**

Por decisión de diseño (Gate 1), no se incluyen datos de ejemplo en la migración inicial. Los datos de prueba se gestionarán en una historia futura. Esto permite que cada entorno (desarrollo, pruebas, producción) comience con una base de datos limpia y se pueble según sus necesidades específicas.

---

**¿Qué sucede si intento eliminar un usuario que tiene registraciones activas?**

El sistema rechazará la operación y mostrará un error indicando que el usuario no puede eliminarse porque tiene registraciones asociadas. Esto protege la integridad de los datos históricos. Para eliminar el usuario, primero se deben cancelar o eliminar sus registraciones.

---

**¿Se registra quién creó o modificó cada registro?**

Actualmente, el sistema solo registra **cuándo** se creó o modificó cada registro (fecha y hora en UTC), pero no **quién** lo hizo. La auditoría de usuario (`CreatedBy`, `UpdatedBy`) se implementará en una historia futura cuando se implemente autenticación de usuarios.

---

**¿Puedo ejecutar la aplicación sin Docker?**

Sí. Docker solo es necesario para ejecutar los tests de integración (24 tests que verifican el comportamiento con SQL Server real). La aplicación funciona perfectamente sin Docker. Los tests unitarios (60 tests) se ejecutan sin Docker utilizando una base de datos en memoria.

---

**¿Los datos se almacenan en la nube o localmente?**

En desarrollo, los datos se almacenan localmente en SQL Server Express (`NF-TRAVEL\SQLEXPRESS`). En producción, dependerá de la configuración del connection string — puede apuntar a Azure SQL Database (nube) o a un SQL Server on-premise.

---

## Funcionalidades Relacionadas

Esta configuración de base de datos es el fundamento técnico para las siguientes funcionalidades futuras:

- **Gestión de Eventos** (crear, listar, modificar, eliminar eventos deportivos)
- **Gestión de Usuarios** (registrar participantes del club)
- **Sistema de Registraciones** (inscripción de usuarios a eventos)
- **Notificaciones** (avisos cuando un usuario se registra o cancela)
- **Reportes** (estadísticas de participación, eventos más populares, etc.)

---

## Soporte Técnico

Para problemas relacionados con la base de datos, verificar:

1. **SQL Server está ejecutándose:**
   - Abrir "Servicios" de Windows (services.msc)
   - Verificar que "SQL Server (SQLEXPRESS)" está en estado "Running"

2. **Connection string es correcto:**
   - Revisar `appsettings.json` → `ConnectionStrings:DefaultConnection`
   - Verificar que el nombre del servidor coincide con la instancia instalada

3. **Logs de la aplicación:**
   - En modo Development, la aplicación registra todos los comandos SQL ejecutados
   - Revisar logs en consola para identificar errores de migración o queries

4. **Herramientas de inspección:**
   - **SQL Server Management Studio (SSMS):** Para inspeccionar tablas, datos, índices
   - **Azure Data Studio:** Alternativa multiplataforma a SSMS
   - **Entity Framework Core Tools:** Para generar scripts SQL de migraciones

---

**Fin de Guía Funcional**
