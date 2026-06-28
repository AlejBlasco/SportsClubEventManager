# Diseño Técnico — Domain Model Foundation
**Story:** US-2  
**Working Branch:** features/us-2-domain-model-foundation  
**Fecha:** 2026-06-22  
**Estado:** Implementado

---

## Descripción General

Esta historia establece el modelo de dominio fundacional para la aplicación Sports Club Event Manager. Se ha implementado un conjunto completo de entidades de dominio puro siguiendo los principios de Clean Architecture, incluyendo las entidades principales Event (Evento), Registration (Inscripción) y User (Usuario), junto con sus enumeraciones, excepciones de dominio y clase base común.

El modelo de dominio es completamente independiente de cualquier framework de infraestructura, bases de datos o capas de presentación, garantizando máxima testabilidad y mantenibilidad. Todas las reglas de negocio están encapsuladas en las propias entidades del dominio, proporcionando una base sólida para el desarrollo futuro de las capas Application, Infrastructure y API.

---

## Arquitectura

### Componentes Implementados

El modelo de dominio se organiza en cuatro áreas principales:

**1. Common (Clases Comunes)**
- `BaseEntity`: Clase abstracta que proporciona identificador único (Guid) y campos de auditoría (CreatedAt, UpdatedAt) para todas las entidades del dominio

**2. Entities (Entidades)**
- `Event`: Representa un evento con título, descripción, fecha, ubicación y capacidad máxima
- `Registration`: Vincula un usuario con un evento, incluyendo fecha de inscripción y estado
- `User`: Representa un usuario con información básica: nombre, email, género y datos de licencia opcionales

**3. Enums (Enumeraciones)**
- `Gender`: Define el género del usuario (Male, Female, Other)
- `RegistrationStatus`: Define estados de inscripción (Registered, Cancelled, Waitlisted)

**4. Exceptions (Excepciones de Dominio)**
- `DomainException`: Excepción base para todas las violaciones de reglas de negocio
- `CapacityExceededException`: Lanzada cuando se excede la capacidad de un evento
- `DuplicateRegistrationException`: Lanzada al intentar una inscripción duplicada

### Flujo de Datos

En esta fase (solo dominio), el flujo de datos es interno a las entidades:

1. **Validación en Setters**: Las propiedades con reglas de negocio validan automáticamente al asignarse
   - `Event.MaxCapacity` valida que el valor sea > 0
   - `User.Email` valida el formato mediante expresión regular

2. **Propiedades Computadas**: Algunas propiedades se calculan dinámicamente
   - `Event.CurrentRegistrations` cuenta inscripciones activas (no canceladas)
   - `Event.IsFull` evalúa si se alcanzó la capacidad máxima

3. **Métodos de Negocio**: Encapsulan lógica de dominio compleja
   - `Event.CanAcceptRegistration()` verifica disponibilidad
   - `Event.ValidateFutureDate()` asegura que eventos nuevos sean futuros
   - `Registration.Cancel()` cancela una inscripción
   - `Registration.IsActive()` determina si una inscripción está activa

**Flujo futuro (Application/Infrastructure):**
```
API Layer → Application Services → Domain Entities → Repository (Infrastructure) → Database
                                   ↑ 
                          Validaciones y reglas de negocio
```

### Decisiones de Diseño

**1. Tipo de Clave Primaria: Guid (aprobado en Gate 1)**
- **Decisión:** Usar `Guid` en lugar de `int` para identificadores
- **Razones:**
  - Permite generación distribuida de IDs sin coordinación de base de datos
  - Facilita testing (crear entidades con IDs conocidos sin depender de auto-increment)
  - Desacople de la implementación de base de datos
- **Alternativa considerada:** `int` con auto-increment (descartada por limitaciones en escenarios distribuidos)

**2. CurrentRegistrations como Propiedad Computada (aprobado en Gate 1)**
- **Decisión:** Calcular en tiempo de ejecución contando la colección Registrations
- **Razones:**
  - Garantiza integridad de datos (siempre refleja el estado real)
  - Evita complejidad de mantener un campo persistido sincronizado
- **Alternativa considerada:** Campo persistido actualizado mediante eventos de dominio (diferido para optimización futura si es necesario)
- **Nota de rendimiento:** Requiere eagerly loading de la colección `Registrations` vía `.Include()` en la capa Application

**3. Email Globalmente Único (aprobado en Gate 1)**
- **Decisión:** El email es único a nivel de sistema
- **Razones:** Alineado con requisitos futuros de autenticación (Sprint 2)
- **Implementación:** Índice único en la capa Infrastructure

**4. Gender como Enum (aprobado en Gate 1)**
- **Decisión:** Enum con valores `Male`, `Female`, `Other`
- **Razones:**
  - Tipado fuerte en código
  - Rendimiento superior a lookups en base de datos para valores estáticos
- **Extensibilidad:** Puede migrarse a tabla de lookup si futuros requisitos lo demandan

**5. Cancelación con Flag de Estado (aprobado en Gate 1)**
- **Decisión:** Las inscripciones canceladas mantienen `Status = Cancelled` en lugar de eliminarse
- **Razones:**
  - Preserva trazabilidad de auditoría
  - Permite re-inscripción del mismo usuario
  - Facilita análisis histórico

**6. Fail-Fast Validation**
- **Decisión:** Validar inmediatamente en los setters de propiedades
- **Razones:**
  - Detecta errores lo antes posible
  - Imposibilita estados inválidos en entidades
- **Implementación:** Backing fields privados con validación en setter público

---

## Referencia de Entidades

### BaseEntity

Clase abstracta base para todas las entidades del dominio.

**Propiedades:**
- `Id` (Guid): Identificador único de la entidad
- `CreatedAt` (DateTime): Timestamp UTC de creación
- `UpdatedAt` (DateTime?): Timestamp UTC de última modificación (nullable)

---

### Event

Representa un evento en el sistema.

**Propiedades:**
- `Title` (string, requerido, max 200): Título del evento
- `Description` (string?, opcional): Descripción detallada
- `Date` (DateTime, requerido): Fecha y hora del evento
- `Location` (string, requerido, max 300): Ubicación donde se realiza
- `MaxCapacity` (int, requerido, > 0): Capacidad máxima de asistentes
- `Registrations` (ICollection&lt;Registration&gt;): Colección de inscripciones
- `CurrentRegistrations` (int, computada, solo lectura): Cuenta de inscripciones activas (excluye canceladas)
- `IsFull` (bool, computada, solo lectura): Indica si se alcanzó la capacidad máxima

**Métodos:**
- `CanAcceptRegistration()`: Devuelve `true` si el evento puede aceptar una nueva inscripción
- `ValidateFutureDate()`: Valida que la fecha del evento sea futura (para eventos nuevos)

**Reglas de Negocio:**
- `MaxCapacity` debe ser > 0 (validado en setter)
- Nuevos eventos deben tener fecha futura (validado en `ValidateFutureDate()`)
- `CurrentRegistrations` solo cuenta inscripciones con estado distinto de `Cancelled`

**Excepciones:**
- `DomainException`: Lanzada si MaxCapacity ≤ 0 o si la fecha es pasada

---

### User

Representa un usuario en el sistema (versión MVP mínima).

**Propiedades:**
- `Name` (string, requerido, max 100): Nombre del usuario
- `Gender` (Gender enum, requerido): Género (Male, Female, Other)
- `Email` (string, requerido, único, max 255): Dirección de email (validada)
- `LicenseNumber` (string?, opcional, max 50): Número de licencia deportiva
- `LicenseCategory` (string?, opcional, max 50): Categoría de licencia
- `Registrations` (ICollection&lt;Registration&gt;): Colección de inscripciones del usuario

**Reglas de Negocio:**
- Email debe tener formato válido: regex `^[^@\s]+@[^@\s]+\.[^@\s]+$`
- Email debe ser único globalmente (constraint en Infrastructure)
- Email es case-insensitive

**Excepciones:**
- `DomainException`: Lanzada si el email es null, vacío o formato inválido

**Nota de Seguridad:**
El regex de email es simplificado (validación de primer nivel). La capa Application debe implementar validación más estricta y/o enviar email de confirmación.

---

### Registration

Representa la inscripción de un usuario a un evento.

**Propiedades:**
- `EventId` (Guid, FK, requerido): Identificador del evento
- `UserId` (Guid, FK, requerido): Identificador del usuario
- `RegistrationDate` (DateTime, requerido): Fecha y hora de inscripción (default: DateTime.UtcNow)
- `Status` (RegistrationStatus enum, requerido): Estado de la inscripción (default: Registered)
- `Event` (Event, navegación): Evento asociado
- `User` (User, navegación): Usuario asociado

**Métodos:**
- `Cancel()`: Establece el estado a `Cancelled`
- `IsActive()`: Devuelve `true` si el estado no es `Cancelled`

**Reglas de Negocio:**
- Una combinación (EventId, UserId) solo puede tener una inscripción activa (constraint en Infrastructure mediante índice único filtrado)
- Las inscripciones canceladas preservan el historial (no se eliminan)

**Nota de Testabilidad:**
`RegistrationDate` usa `DateTime.UtcNow` directamente (decisión aceptada para fase de dominio). La capa Application debe introducir `ITimeProvider` para control de tiempo en tests.

---

### Gender (Enum)

```csharp
public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2
}
```

---

### RegistrationStatus (Enum)

```csharp
public enum RegistrationStatus
{
    Registered = 0,   // Inscripción activa
    Cancelled = 1,    // Inscripción cancelada
    Waitlisted = 2    // En lista de espera (funcionalidad futura)
}
```

---

### Excepciones de Dominio

**DomainException**
- Excepción base para todas las violaciones de reglas de negocio del dominio
- Hereda de `System.Exception`

**CapacityExceededException**
- Hereda de `DomainException`
- Lanzada cuando se intenta inscribir a un evento que alcanzó su capacidad máxima
- Mensaje por defecto: "The event has reached its maximum capacity."

**DuplicateRegistrationException**
- Hereda de `DomainException`
- Lanzada cuando un usuario intenta inscribirse dos veces al mismo evento
- Mensaje por defecto: "The user is already registered for this event."

---

## Configuración

### appsettings.json

**Ningún cambio requerido** en esta fase (solo dominio).

La futura capa Infrastructure requerirá:
- Connection string para base de datos
- Configuración de logging
- Configuración de Entity Framework Core

### Secretos / Azure Key Vault

**Ninguno** en esta fase. No hay dependencias de infraestructura.

---

## Cambios en Base de Datos

### Migración de Entity Framework Core

**Estado:** Pendiente (se creará en la historia de Infrastructure)

**Nombre sugerido:** `InitialDomainModel`

**Comando:**
```bash
dotnet ef migrations add InitialDomainModel --project src/SportsClubEventManager.Infrastructure
```

### Cambios de Esquema

**Tablas a crear:**

**1. Events**
- Id (uniqueidentifier, PK)
- Title (nvarchar(200), NOT NULL)
- Description (nvarchar(max), NULL)
- Date (datetime2, NOT NULL)
- Location (nvarchar(300), NOT NULL)
- MaxCapacity (int, NOT NULL, CHECK > 0)
- CreatedAt (datetime2, NOT NULL)
- UpdatedAt (datetime2, NULL)

**2. Users**
- Id (uniqueidentifier, PK)
- Name (nvarchar(100), NOT NULL)
- Gender (int, NOT NULL)
- Email (nvarchar(255), NOT NULL)
- LicenseNumber (nvarchar(50), NULL)
- LicenseCategory (nvarchar(50), NULL)
- CreatedAt (datetime2, NOT NULL)
- UpdatedAt (datetime2, NULL)

**Índices:**
- `IX_Users_Email` (UNIQUE) en Users.Email

**3. Registrations**
- Id (uniqueidentifier, PK)
- EventId (uniqueidentifier, FK → Events.Id, NOT NULL)
- UserId (uniqueidentifier, FK → Users.Id, NOT NULL)
- RegistrationDate (datetime2, NOT NULL)
- Status (int, NOT NULL)
- CreatedAt (datetime2, NOT NULL)
- UpdatedAt (datetime2, NULL)

**Índices:**
- `IX_Registrations_EventId` en Registrations.EventId
- `IX_Registrations_UserId` en Registrations.UserId
- `IX_Registrations_EventUser` (UNIQUE, FILTERED) en (EventId, UserId) WHERE Status != 1

**Foreign Keys:**
- Registrations.EventId → Events.Id (ON DELETE RESTRICT)
- Registrations.UserId → Users.Id (ON DELETE RESTRICT)

**Restricción de eliminación:** Las restricciones RESTRICT previenen la eliminación accidental de Events o Users que tengan Registrations asociadas.

---

## Dependencias Agregadas

**Ninguna**. El modelo de dominio es puro C# sin dependencias externas de NuGet.

**Futuras dependencias (capa Infrastructure):**
- `Microsoft.EntityFrameworkCore` (versión estable para .NET 10)
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`

**Futuras dependencias (proyectos de pruebas):**
- Ya están configuradas: `xunit`, `xunit.runner.visualstudio`, `NSubstitute`, `FluentAssertions`, `Bogus`

---

## Testing

### Cobertura de Pruebas Unitarias

**Resultados:**
- **Total de tests:** 86
- **Tests pasados:** 86 (100%)
- **Tests fallidos:** 0
- **Cobertura de línea:** 96.55% (supera el mínimo de 90%)
- **Cobertura de rama:** 100%

### Áreas Cubiertas por Tests

**BaseEntity (5 tests)**
- Inicialización de propiedades Id, CreatedAt, UpdatedAt
- Asignación de valores nullable

**Event (35 tests)**
- Validación de MaxCapacity (positivo, cero, negativo)
- Validación de fecha futura
- Cálculo de CurrentRegistrations (cero, con canceladas, mixtas)
- Propiedad IsFull (edge cases: exactamente en capacidad, excedida)
- Método CanAcceptRegistration

**User (33 tests)**
- Validación de email (formatos válidos e inválidos)
- Manejo de campos opcionales (LicenseNumber, LicenseCategory)
- Valores de enumeración Gender

**Registration (21 tests)**
- Método Cancel y cambios de estado
- Método IsActive
- Valor por defecto de RegistrationDate
- Construcción con diferentes valores de Status

**Exceptions (15 tests)**
- Constructores de DomainException, CapacityExceededException, DuplicateRegistrationException
- Jerarquía de herencia
- Lanzamiento y captura

### Escenarios de Pruebas de Integración

**No aplicables** en esta fase (solo dominio, sin infraestructura).

Las pruebas de integración futuras deben cubrir:
- Persistencia de entidades con Entity Framework Core
- Validación de constraints de base de datos (unique indexes, foreign keys)
- Consultas con `.Include()` para evitar N+1 queries
- Transacciones y rollback

---

## Limitaciones Conocidas

1. **Dependencia de DateTime.UtcNow**
   - `Registration.RegistrationDate` usa `DateTime.UtcNow` directamente
   - Dificulta tests determinísticos
   - **Mitigación planificada:** Introducir `ITimeProvider` en la capa Application

2. **Validación de Email Simplificada**
   - Regex básica, no cumple completamente RFC 5322
   - Puede aceptar emails inválidos en edge cases
   - **Mitigación planificada:** Validación más estricta en Application layer + email de confirmación

3. **Propiedades Mutables**
   - Todas las propiedades tienen setters públicos (requerido por EF Core)
   - Potencialmente permite bypass de validación
   - **Mitigación planificada:** Uso de `init` accessors donde sea posible en capa Infrastructure

4. **Sin Soft Delete**
   - No hay mecanismo de eliminación lógica (soft delete)
   - Las eliminaciones son permanentes
   - **Mitigación actual:** Foreign keys con ON DELETE RESTRICT previenen eliminaciones accidentales

5. **CurrentRegistrations sin Optimización**
   - La propiedad computada puede causar N+1 queries si no se usa `.Include()`
   - **Mitigación planificada:** Documentar en capa Application; considerar columna computada en DB si el rendimiento lo requiere

---

## Consideraciones de Seguridad

### Resumen del Security Review

**Estado de Gate 3:** ✅ APROBADO

**Hallazgos:**
- **0 Critical**
- **1 High:** DateTime.UtcNow afecta testabilidad (no es vulnerabilidad explotable)
- **2 Medium:** Email regex simplificado, posible predictibilidad de GUIDs secuenciales
- **1 Low:** PII en mensajes de excepción (email)

### Mitigaciones Aplicadas

**Email Validation:**
- Validación de primer nivel implementada en dominio
- Responsabilidad de validación estricta y verificación SMTP delegada a Application layer

**GUID Predictability:**
- Decisión de generación de IDs (random vs. sequential) diferida a Infrastructure
- Recomendación: usar `NEWID()` (random) en lugar de `NEWSEQUENTIALID()`
- **Crítico:** La capa Application DEBE implementar autorización adecuada independientemente del tipo de ID

**PII Logging:**
- Dominio no realiza logging directamente
- Application layer es responsable de sanitizar excepciones antes de loggear

**Secret Scanning:**
- ✅ Sin secretos detectados (0 dependencias externas)

**Dependency Scanning:**
- ✅ Sin dependencias vulnerables (0 paquetes NuGet)

### Recomendaciones para Fases Futuras

**Application Layer:**
1. Implementar `ITimeProvider` para abstracción de tiempo
2. Validación de email más estricta + confirmación por SMTP
3. Sanitización de PII en logs

**API Layer:**
1. Aplicar `[Authorize]` en todos los endpoints
2. Autorización a nivel de recurso (usuarios solo acceden a sus propias inscripciones)
3. Usar DTOs (nunca binding directo a entidades de dominio)
4. Rate limiting en endpoints públicos
5. Headers de seguridad (X-Content-Type-Options, CSP, etc.)

---

## Cumplimiento de Clean Architecture

✅ **Excelente cumplimiento**

- **Capa de Dominio pura:** Cero dependencias en Application, Infrastructure o Presentation
- **Entidades POCO:** Sin atributos de EF Core, JSON o cualquier framework
- **Reglas de negocio encapsuladas:** Toda la lógica en entidades del dominio
- **Excepciones de dominio:** Clara separación de concerns
- **Navegación bidireccional:** Correctamente definida (Event ↔ Registrations ↔ User)

**Principios SOLID:**
- ✅ Single Responsibility
- ✅ Open/Closed (extensibilidad vía BaseEntity)
- ✅ Liskov Substitution (correcta herencia)
- ⚠️ Dependency Inversion (DateTime.UtcNow viola esto temporalmente)

---

## Próximos Pasos

### Historia Siguiente: Infrastructure Layer

**Tareas:**
1. Crear DbContext para EF Core
2. Configurar relaciones de entidades vía Fluent API
3. Aplicar índices únicos y constraints
4. Generar migración inicial
5. Probar migración contra base de datos local
6. Implementar patrón Repository (opcional)

### Historia Siguiente: Application Layer

**Tareas:**
1. Crear Application Services (Commands y Queries)
2. Implementar DTOs para request/response
3. Agregar AutoMapper o Mapperly
4. Implementar `ITimeProvider` abstraction
5. Validación con FluentValidation
6. Implementar patrón Unit of Work

### Historia Siguiente: API Layer

**Tareas:**
1. Crear API controllers/endpoints
2. Configurar autenticación y autorización
3. Rate limiting middleware
4. Exception handling middleware
5. Swagger/OpenAPI documentation
6. CORS policy

---

## Referencias

- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)

---

**Documento generado por:** Documentation Agent  
**Fecha:** 2026-06-22  
**Versión:** 1.0
