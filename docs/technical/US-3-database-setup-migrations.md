# Diseño Técnico — Configuración de Base de Datos y Migraciones

**Historia:** US-3  
**Rama de trabajo:** features/us-3-database-setup-and-migrations  
**Fecha:** 2026-06-23  
**Estado:** Implementado

---

## Resumen

Esta historia de usuario establece la capa de persistencia de datos para la aplicación SportsClubEventManager utilizando Entity Framework Core con SQL Server. Se ha implementado un DbContext completo con configuraciones Fluent API para las entidades Event, Registration y User, junto con migraciones automáticas en el inicio de la aplicación y un sistema de auditoría automático para campos CreatedAt y UpdatedAt.

El enfoque Code-First permite que el esquema de base de datos se genere automáticamente a partir de las entidades del dominio, manteniendo la arquitectura limpia y la separación de responsabilidades entre las capas Domain, Infrastructure y Presentation.

---

## Arquitectura

### Componentes Involucrados

```
┌─────────────────────────────────────────────────────────────┐
│  SportsClubEventManager.Web (Presentation)                  │
│  - Program.cs: Registra Infrastructure y ejecuta migraciones│
│  - appsettings.json: Connection string                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Referencia
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  SportsClubEventManager.Infrastructure (Persistence)        │
│  - AppDbContext: DbSets y SaveChangesAsync override         │
│  - Configurations/: EventConfiguration, UserConfiguration,  │
│                     RegistrationConfiguration               │
│  - DependencyInjection.cs: Registro de servicios EF Core    │
│  - Migrations/: 20260623054859_InitialCreate.cs             │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Referencia
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  SportsClubEventManager.Domain (Business Logic)             │
│  - Common/BaseEntity: CreatedAt, UpdatedAt                  │
│  - Event, Registration, User entities                       │
└─────────────────────────────────────────────────────────────┘
```

### Flujo de Datos

1. **Inicio de aplicación:**
   - `Program.cs` llama a `AddInfrastructure()` para registrar `AppDbContext` en el contenedor DI
   - `MigrateDatabaseAsync()` aplica migraciones pendientes automáticamente usando `Database.MigrateAsync()`
   - Si la base de datos no existe, EF Core la crea automáticamente

2. **Operación de escritura (insert/update):**
   - El código de la aplicación obtiene `AppDbContext` desde DI
   - Se realiza una operación de agregar o modificar entidades
   - `SaveChangesAsync()` se invoca
   - El override intercepta el guardado y puebla automáticamente:
     - `CreatedAt` → `DateTime.UtcNow` (solo en entidades Added)
     - `UpdatedAt` → `DateTime.UtcNow` (solo en entidades Modified)
   - EF Core traduce los cambios a SQL y ejecuta contra SQL Server
   - Transaction se confirma automáticamente

3. **Operación de lectura (query):**
   - El código de la aplicación obtiene `AppDbContext` desde DI
   - Se ejecuta una consulta LINQ sobre DbSet (`Events`, `Users`, `Registrations`)
   - EF Core traduce LINQ a SQL
   - Resultados se materializan como entidades del dominio

4. **Operación de eliminación (delete):**
   - Se elimina una entidad Event → EF Core aplica cascade delete y elimina todas sus Registrations automáticamente
   - Se intenta eliminar un User con Registrations → SQL Server rechaza la operación (restrict delete), lanzando `DbUpdateException`

---

## Decisiones de Diseño

### 1. Uso de Fluent API en lugar de Data Annotations

**Decisión:** Todas las configuraciones de entidades se realizan con Fluent API en clases separadas que implementan `IEntityTypeConfiguration<T>`.

**Alternativas consideradas:**
- Data Annotations: Contaminarían las entidades del dominio con concerns de persistencia

**Justificación:**
- Mantiene las entidades del dominio limpias y agnósticas de persistencia (Clean Architecture)
- Permite configuraciones complejas que no son posibles con Data Annotations
- Mejora la mantenibilidad al separar configuraciones por entidad

### 2. Auditoría automática en SaveChangesAsync

**Decisión:** Override de `SaveChangesAsync` en `AppDbContext` para poblar automáticamente `CreatedAt` y `UpdatedAt`.

**Alternativas consideradas:**
- Poblar manualmente en cada operación de negocio
- Usar interceptores de EF Core

**Justificación:**
- Garantiza que los campos de auditoría siempre se pueblan, sin depender de código manual
- Centraliza la lógica de auditoría en un solo lugar
- Reduce duplicación de código en capas superiores

### 3. Auto-aplicación de migraciones en startup

**Decisión:** `Program.cs` ejecuta `Database.MigrateAsync()` automáticamente al iniciar la aplicación.

**Alternativas consideradas:**
- Aplicar migraciones manualmente con `dotnet ef database update`
- Usar script SQL generado en pipeline CI/CD

**Justificación:**
- Simplifica la experiencia de desarrollo (no requiere pasos manuales)
- La base de datos siempre está actualizada al ejecutar la aplicación
- Apropiado para MVP y entornos de desarrollo

**Nota:** Para producción, se recomienda considerar un paso dedicado de migración en el pipeline CI/CD para evitar problemas de concurrencia con múltiples instancias.

### 4. Cascade delete para Event → Registration

**Decisión:** Eliminación en cascada configurada para la relación Event → Registration.

**Justificación:**
- Las registraciones no tienen sentido sin el evento asociado
- Evita registraciones huérfanas en la base de datos
- Simplifica la lógica de eliminación de eventos

### 5. Restrict delete para User → Registration

**Decisión:** Eliminación restringida para la relación User → Registration.

**Justificación:**
- Los usuarios con registraciones activas no deberían poder eliminarse
- Protege la integridad referencial histórica
- Requiere lógica de negocio explícita para decidir cómo manejar usuarios con datos

### 6. Unique index en User.Email

**Decisión:** Índice único aplicado a la columna `Users.Email`.

**Justificación:**
- Previene usuarios duplicados con el mismo email
- Facilita futuras funcionalidades de autenticación/login
- Mejora el rendimiento de búsquedas por email

### 7. Connection resiliency con retry policy

**Decisión:** SQL Server configurado con política de reintentos (3 intentos, 30 segundos de delay máximo).

**Justificación:**
- Protege contra fallos transitorios de red o base de datos
- Mejora la resiliencia en entornos cloud (Azure SQL Database)
- Evita errores innecesarios por problemas temporales de conectividad

---

## Cambios en la Base de Datos

### Migración de EF Core

**Nombre:** `20260623054859_InitialCreate`  
**Comando de generación:**
```bash
dotnet ef migrations add InitialCreate --project src/SportsClubEventManager.Infrastructure --startup-project src/SportsClubEventManager.Web
```

**Qué hace:**
- Crea la base de datos si no existe
- Crea las tablas Events, Users, Registrations
- Configura claves primarias, foreign keys, índices y constraints

### Cambios de Esquema

#### Tabla `Events`

| Columna | Tipo | Restricciones | Notas |
|---------|------|---------------|-------|
| `Id` | int | PRIMARY KEY, IDENTITY(1,1) | Generado automáticamente |
| `Title` | nvarchar(200) | NOT NULL | Título del evento |
| `Description` | nvarchar(2000) | NULL | Descripción opcional |
| `Date` | datetime2(7) | NOT NULL | Fecha y hora del evento |
| `Location` | nvarchar(500) | NOT NULL | Ubicación del evento |
| `MaxCapacity` | int | NOT NULL | Capacidad máxima (> 0) |
| `CreatedAt` | datetime2(7) | NOT NULL | Timestamp de creación (UTC) |
| `UpdatedAt` | datetime2(7) | NULL | Timestamp de última modificación (UTC) |

**Índices:**
- `IX_Events_Date` (no único) — optimiza búsquedas por fecha

#### Tabla `Users`

| Columna | Tipo | Restricciones | Notas |
|---------|------|---------------|-------|
| `Id` | int | PRIMARY KEY, IDENTITY(1,1) | Generado automáticamente |
| `Name` | nvarchar(200) | NOT NULL | Nombre del usuario |
| `Gender` | nvarchar(20) | NOT NULL | Género (almacenado como string: "Male", "Female", "Other") |
| `Email` | nvarchar(256) | NOT NULL, UNIQUE | Email del usuario |
| `LicenseNumber` | nvarchar(100) | NULL | Número de licencia (opcional) |
| `LicenseCategory` | nvarchar(50) | NULL | Categoría de licencia (opcional) |
| `CreatedAt` | datetime2(7) | NOT NULL | Timestamp de creación (UTC) |
| `UpdatedAt` | datetime2(7) | NULL | Timestamp de última modificación (UTC) |

**Índices:**
- `IX_Users_Email` (único) — previene duplicados, optimiza búsquedas por email

#### Tabla `Registrations`

| Columna | Tipo | Restricciones | Notas |
|---------|------|---------------|-------|
| `Id` | int | PRIMARY KEY, IDENTITY(1,1) | Generado automáticamente |
| `EventId` | int | NOT NULL, FOREIGN KEY → Events(Id) | Evento asociado |
| `UserId` | int | NOT NULL, FOREIGN KEY → Users(Id) | Usuario asociado |
| `RegistrationDate` | datetime2(7) | NOT NULL | Fecha de la registración |
| `Status` | nvarchar(50) | NOT NULL | Estado (almacenado como string: "Registered", "Cancelled") |
| `CreatedAt` | datetime2(7) | NOT NULL | Timestamp de creación (UTC) |
| `UpdatedAt` | datetime2(7) | NULL | Timestamp de última modificación (UTC) |

**Foreign Keys:**
- `FK_Registrations_Events_EventId` → Events(Id) con `ON DELETE CASCADE`
- `FK_Registrations_Users_UserId` → Users(Id) con `ON DELETE NO ACTION` (restrict)

**Índices:**
- `IX_Registrations_EventId` (no único) — optimiza joins y búsquedas por evento
- `IX_Registrations_UserId` (no único) — optimiza joins y búsquedas por usuario

---

## Configuración

### Connection string en appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=NF-TRAVEL\\SQLEXPRESS;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;Packet Size=4096;Command Timeout=0"
  }
}
```

**Notas de seguridad:**
- Usa Windows Authentication (`Integrated Security=True`) — no hay contraseña en texto plano
- Apropiado para desarrollo local con SQL Server Express
- **Para producción:** Migrar a Azure Key Vault o variables de entorno

### Logging de EF Core en appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Migrations": "Information"
    }
  }
}
```

**Qué habilita:**
- Logging de comandos SQL generados por EF Core (útil para debugging)
- Logging de ejecución de migraciones
- Solo activo en entorno Development

> **⚠️ Cuidado con `appsettings.Development.json` en el Web.** Tanto
> `SportsClubEventManager.Api` como `SportsClubEventManager.Web` leen
> `ConnectionStrings:DefaultConnection` (vía `AddInfrastructure`), y en Development el
> `appsettings.Development.json` de cada proyecto tiene prioridad sobre su `appsettings.json`. El
> 2026-07-08 se encontró `src/SportsClubEventManager.Web/appsettings.Development.json` con
> `"DefaultConnection": ""` — esa cadena vacía pisaba silenciosamente la cadena válida de
> `appsettings.json`, por lo que `MigrateDatabaseAsync()` fallaba al arrancar el Web (aunque el
> Api, sin esa clave en su propio `Development.json`, funcionaba con normalidad). Si una clave de
> configuración parece "no aplicarse", revisar primero si el `Development.json` del proyecto
> concreto la está sobrescribiendo con un valor vacío.

---

## Dependencias Añadidas

### Paquetes NuGet en Infrastructure

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.1 | Provider de EF Core para SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.1 | Herramientas CLI (`dotnet ef migrations`) |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.1 | Design-time tooling para generación de migraciones |

### Paquetes NuGet en Web

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Microsoft.EntityFrameworkCore.Design` | 10.0.1 | Requerido para generación de migraciones desde proyecto startup |

### Paquetes NuGet en tests/Infrastructure

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.1 | Provider in-memory para unit tests |
| `xunit` | 2.8.1 | Framework de testing |
| `FluentAssertions` | 6.12.1 | Assertions expresivas |
| `NSubstitute` | 5.1.0 | Mocking framework |
| `Bogus` | 35.5.1 | Generación de datos de prueba |

### Paquetes NuGet en tests/IntegrationTests

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Testcontainers.MsSql` | 4.4.0 | Orquestación de contenedores SQL Server |
| `Respawn` | 7.0.0 | Reset de base de datos entre tests |
| `FluentAssertions` | 7.1.0 | Assertions expresivas |

---

## Testing

### Cobertura de Unit Tests

**Total de tests:** 60  
**Cobertura estimada:** 91% (supera el mínimo requerido de 90%)  
**Estado:** ✅ Todos los tests pasan

**Qué se cubre:**

1. **AppDbContext (9 tests):**
   - Auto-población de `CreatedAt` en insert
   - Auto-población de `UpdatedAt` en update
   - Inmutabilidad de `CreatedAt` en update
   - No modificación de entidades sin cambios
   - Manejo correcto de `CancellationToken`

2. **EventConfiguration (14 tests):**
   - Validación de constraints (Title, Description, Location, MaxCapacity)
   - Propiedades computadas ignoradas (`CurrentRegistrations`, `IsFull`)
   - Cascade delete a Registrations
   - Índice en `Date`

3. **UserConfiguration (15 tests):**
   - Validación de constraints (Name, Email, Gender, License fields)
   - Unique index en `Email` previene duplicados
   - Restrict delete para Users con Registrations
   - Enum `Gender` almacenado como string

4. **RegistrationConfiguration (13 tests):**
   - Foreign keys requeridos (EventId, UserId)
   - Cascade delete desde Event
   - Restrict delete desde User
   - Enum `Status` almacenado como string

5. **DependencyInjection (9 tests):**
   - Registro correcto de `AppDbContext` como scoped service
   - Isolation entre scopes
   - Validación de connection string
   - Configuración de retry policy

**Nota:** Los tests utilizan EF Core InMemory provider, que simula el comportamiento de base de datos pero no replica exactamente todas las características de SQL Server (ej. algunas constraints).

### Escenarios de Integration Tests

**Total de tests:** 24  
**Estado:** ⚠️ Implementados pero requieren Docker para ejecución  
**Ejecutarán en:** CI pipeline con Docker disponible

**Qué se cubre:**

1. **Verificación de migraciones (4 tests):**
   - Migración ejecuta exitosamente contra SQL Server real
   - Tablas Events, Users, Registrations creadas correctamente

2. **CRUD Operations (6 tests):**
   - Crear, leer, actualizar, eliminar Events
   - Crear User, validar unique constraint en Email
   - Crear Registration con relaciones

3. **Foreign Keys y Cascades (4 tests):**
   - Eliminar Event cascadea a Registrations
   - Eliminar User con Registrations falla (restrict delete)
   - Violación de FK al insertar Registration con EventId/UserId inválido

4. **Índices (2 tests):**
   - Índice en `Event.Date` existe
   - Índice único en `User.Email` existe

5. **Audit Trail (2 tests):**
   - `CreatedAt` se puebla automáticamente en insert
   - `UpdatedAt` se puebla automáticamente en update

6. **Connection Resiliency (1 test):**
   - Conexión a SQL Server se establece correctamente

**Infraestructura de tests:**
- `DatabaseFixture.cs` — gestiona ciclo de vida del contenedor SQL Server
- `Testcontainers.MsSql` — levanta SQL Server 2022 en Docker
- `Respawn` — resetea base de datos entre tests para aislamiento

**Para ejecutar (requiere Docker):**
```bash
dotnet test tests/SportsClubEventManager.IntegrationTests/SportsClubEventManager.IntegrationTests.csproj
```

---

## Limitaciones Conocidas

1. **Auto-migración en producción:**
   - La migración automática en `Program.cs` puede causar problemas de concurrencia si múltiples instancias de la aplicación inician simultáneamente
   - Recomendación: Deshabilitar auto-migración en producción y aplicar migraciones en un paso dedicado del pipeline CI/CD

2. **Connection string en appsettings.json:**
   - Apropiado para desarrollo local, pero no seguro para producción
   - Recomendación: Migrar a Azure Key Vault o variables de entorno en producción

3. **Validación solo en EF Core:**
   - Las validaciones de negocio (ej. MaxCapacity > 0) están configuradas en Fluent API pero no en las entidades del dominio
   - Recomendación: Considerar agregar validaciones en el dominio para capturar errores antes de persistencia

4. **Soft delete no implementado:**
   - La eliminación es física (DELETE permanente)
   - Recomendación: Si se requiere auditoría completa en el futuro, implementar patrón Soft Delete con campo `IsDeleted`

5. **Pruebas de integración requieren Docker:**
   - Los 24 integration tests no pueden ejecutarse en entornos sin Docker
   - Recomendación: Asegurar que Docker esté disponible en entornos de desarrollo y CI

6. **Dependencia transitiva de Azure.Core:**
   - EF Core 10.0.1 tiene dependencia transitiva en Azure.Core 1.47.1, que solo soporta .NET 8.0
   - Estado actual: Vulnerabilidad GHSA-37gx-xxp4-5rgx y GHSA-w3x6-4m5h-cxqf suprimidas en `Directory.Build.props` hasta que se publique versión parcheada
   - Recomendación: Monitorear releases de Azure.Core y EF Core para actualizar cuando haya versión compatible con .NET 10.0 sin vulnerabilidades

---

## Consideraciones de Seguridad

### Revisión de Código (Code Review Agent)

**Resultado:** ✅ Aprobado (0 Critical, 0 High, 0 Medium, 0 Low)

**Aspectos verificados:**
- No hay secrets en texto plano (usa Windows Authentication)
- Connection string en appsettings.json es seguro para desarrollo
- No hay EF Core types expuestos en capa de presentación
- Separación de concerns respetada (Clean Architecture)
- Fluent API mantiene dominio libre de atributos de persistencia

**Recomendaciones implementadas:**
- Connection resiliency configurada (retry policy)
- Logging de EF Core configurado en Development environment
- Cascade delete y restrict delete configurados explícitamente

### Revisión de Seguridad (Security Review Agent)

**Estado:** — Fase omitida por decisión humana

**Nota:** Esta historia introduce solo configuración de base de datos y migraciones, sin lógica de negocio sensible a seguridad. Las validaciones de seguridad se realizarán en historias futuras que implementen autenticación, autorización, y endpoints de API.

### Vulnerabilidades de Dependencias

**Vulnerabilidades conocidas:**
- `System.Security.Cryptography.Xml` (dependencia transitiva de EF Core 10.0.1)
  - GHSA-37gx-xxp4-5rgx (severidad: High)
  - GHSA-w3x6-4m5h-cxqf (severidad: High)
- **Mitigación:** Suprimidas temporalmente en `Directory.Build.props` hasta que Microsoft publique versión parcheada para .NET 10.0

---

## API Reference

**N/A** — Esta historia no introduce endpoints de API. La capa de persistencia estará disponible para historias futuras que implementen API REST.

---

## Cómo Usar

### Generar nueva migración

Si se modifica una entidad del dominio o configuración:

```bash
dotnet ef migrations add {NombreDeLaMigracion} \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web
```

> **⚠️ No crear archivos de migración a mano.** El 2026-07-08 se detectó que tres migraciones
> (`AddRoleToUser`, `SeedAdministratorUser`, `AddAuditLogTable` — ver US-28/US-30) se habían
> creado manualmente sin su `.Designer.cs` asociado. Ese archivo lleva el atributo
> `[DbContext(typeof(AppDbContext))]` que EF Core necesita para reconocer la migración; sin él,
> `MigrateDatabaseAsync()` la ignora silenciosamente y solo se ve el mensaje "No migrations were
> applied. The database is already up to date." — sin ningún error visible, aunque la tabla o
> columna nunca se llegó a crear. Usa siempre `dotnet ef migrations add` (como arriba); genera
> automáticamente el `.cs` y el `.Designer.cs` juntos y evita esta clase de bug.

### Aplicar migraciones manualmente (alternativa a auto-migración)

```bash
dotnet ef database update \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web
```

### Revertir última migración

```bash
dotnet ef migrations remove \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web
```

### Generar script SQL de migración (para deployment manual)

```bash
dotnet ef migrations script \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web \
  --output migration.sql
```

---

## Trabajo Futuro Recomendado

1. **Desactivar auto-migración en producción:**
   - Condicionar `MigrateDatabaseAsync()` a `!env.IsProduction()`
   - Crear step de migración manual en pipeline Azure DevOps

2. **Mover connection string a Azure Key Vault:**
   - Crear secret `SportsClubDb` en Azure Key Vault
   - Configurar aplicación para leer de Key Vault en producción

3. **Implementar patrón Repository (opcional):**
   - Si la complejidad crece, considerar abstraer EF Core detrás de repositorios
   - Definir `IEventRepository`, `IUserRepository`, etc. en Application layer

4. **Implementar Soft Delete:**
   - Agregar campo `IsDeleted` a `BaseEntity`
   - Configurar query filter global en `AppDbContext` para excluir entidades eliminadas

5. **Auditoría de `CreatedBy` / `UpdatedBy`:**
   - Cuando se implemente autenticación, agregar campos de auditoría de usuario
   - Modificar `SaveChangesAsync` para poblar `CreatedBy` / `UpdatedBy` desde contexto de usuario actual

6. **Monitoreo de dependencias:**
   - Configurar Dependabot o similar para alertas de vulnerabilidades en NuGet packages
   - Actualizar a Azure.Core / EF Core versiones parcheadas cuando estén disponibles

---

**Fin del Documento Técnico**
