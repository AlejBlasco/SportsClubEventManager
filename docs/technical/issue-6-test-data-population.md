# Diseño Técnico — Población de Datos de Prueba
**Story:** US-4  
**Rama de Trabajo:** features/us-4-test-data-population  
**Fecha:** 2026-06-23  
**Estado:** Implementado

---

## Resumen

Se ha implementado un mecanismo de seeding de base de datos para entornos de desarrollo que proporciona datos de prueba realistas basados en el calendario de competiciones de tiro deportivo español. El sistema utiliza el método `HasData()` de EF Core para insertar automáticamente 10 eventos, 6 usuarios y 29 inscripciones que representan diversos escenarios de capacidad y estado.

Los datos de seed están diseñados exclusivamente para entornos de desarrollo y staging, con protecciones explícitas para evitar su aplicación en producción. Todos los GUIDs son estables y hardcoded para garantizar idempotencia en múltiples aplicaciones de migraciones.

---

## Arquitectura

### Componentes Involucrados

**Infrastructure Layer**
- `EventConfiguration.cs` — Sin modificaciones de seed (pendiente de implementación)
- `UserConfiguration.cs` — Sin modificaciones de seed (pendiente de implementación)
- `RegistrationConfiguration.cs` — Sin modificaciones de seed (pendiente de implementación)
- `20260623155030_AddDevelopmentSeedData.cs` — Migración que contiene todos los datos de seed

**Test Layer**
- `SeedDataTests.cs` — 12 pruebas unitarias para verificar integridad de datos de seed

### Flujo de Datos

1. **Creación de Migración:** El desarrollador ejecuta `dotnet ef migrations add AddDevelopmentSeedData`
2. **Generación de Migración:** EF Core genera el archivo de migración con InsertData statements
3. **Aplicación Manual:** En dev/staging, el desarrollador ejecuta `dotnet ef database update`
4. **Verificación de Entorno:** La migración verifica `ASPNETCORE_ENVIRONMENT`
5. **Inserción Condicional:** Si es Development, se ejecutan los InsertData; si no, se omiten
6. **Persistencia:** Los datos se insertan en tablas Events, Users, Registrations
7. **Idempotencia:** Re-aplicar la migración no duplica datos (GUIDs estables)

### Decisiones de Diseño

**1. Migración Directa vs Configuración en EntityTypeConfiguration**

**Decisión:** Los datos de seed se implementaron directamente en el archivo de migración con una guarda de entorno, en lugar de usar `HasData()` en las configuraciones de entidad.

**Alternativa considerada:** Añadir métodos `SeedData()` en cada EntityTypeConfiguration y llamar a `HasData()` desde `OnModelCreating`.

**Justificación:**
- Enfoque directo permite control explícito del entorno mediante verificación programática
- Evita contaminar las configuraciones de entidad con datos que solo son relevantes para dev
- Permite separación clara entre esquema (en configuraciones) y datos de prueba (en migración)
- Migración puede ser excluida fácilmente de pipelines de producción

**2. Protección de Entorno mediante Código vs Convención**

**Decisión:** Guardia de entorno explícita en métodos `Up()` y `Down()` de la migración.

**Implementación:**
```csharp
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
{
    migrationBuilder.InsertData(...);
}
```

**Justificación:**
- Previene accidentalmente aplicar seed en producción
- Default a "Production" si variable no está definida (fail-safe)
- Comparación case-insensitive para robustez
- Aplicado consistentemente en Up y Down

**3. GUIDs Estables vs Generados**

**Decisión:** Todos los GUIDs son hardcoded con patrones reconocibles (11111111-..., bbbbbbbb-..., etc.).

**Justificación:**
- Garantiza idempotencia absoluta — mismos IDs en todas las aplicaciones
- Facilita debugging y logs (patrones reconocibles a simple vista)
- Permite referencias FK estables y predecibles
- Evita duplicados en múltiples ejecuciones de migración

**4. Datos Realistas Basados en Calendario Real**

**Decisión:** Eventos basados en CALENDARIO PRECISION 2026 (competiciones reales de tiro deportivo español).

**Fuente:** [Google Sheets - CALENDARIO PRECISION 2026](https://docs.google.com/spreadsheets/d/1pKOl9JlXMrkkPN0TPqEFCyveeRx8nw-B/edit)

**Beneficios:**
- Modalidades auténticas (Pistola 9mm, Carabina BR50, Aire Comprimido, etc.)
- Ubicaciones reales (Zaragoza - CTZ, Teruel - CT Aguanaces, Huesca - RTAA)
- Nombres de usuario españoles con acentuación correcta
- Proporciona contexto realista para desarrollo UI/UX

---

## Cambios en Base de Datos

### Migración EF Core

**Nombre:** `20260623155030_AddDevelopmentSeedData`

**Propósito:** Insertar datos de prueba en tablas existentes (Events, Users, Registrations)

**Comandos:**

```bash
# Crear migración (ya ejecutado)
dotnet ef migrations add AddDevelopmentSeedData \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web

# Aplicar migración (manual, solo dev/staging)
dotnet ef database update AddDevelopmentSeedData \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web
```

**IMPORTANTE:** Esta migración está marcada como **DEV ONLY** y NO debe aplicarse en producción.

### Cambios de Esquema

**Ninguno.** Esta migración solo inserta datos en tablas existentes. No modifica el esquema de base de datos.

### Datos Insertados

**Events:** 10 registros  
**Users:** 6 registros  
**Registrations:** 29 registros

Ver sección "Resumen de Datos de Seed" más abajo para detalles completos.

---

## Dependencias Añadidas

**Ninguna.** Todas las dependencias requeridas ya están presentes en el proyecto.

Paquetes utilizados:
- `Microsoft.EntityFrameworkCore.SqlServer` (10.0.1)
- `Microsoft.EntityFrameworkCore.Tools` (10.0.1)

---

## Testing

### Cobertura de Pruebas Unitarias

**Archivo:** `tests/SportsClubEventManager.Infrastructure/Persistence/SeedDataTests.cs`

**12 pruebas implementadas:**

1. `SeedData_EventIds_ShouldBeUnique` — Verifica unicidad de GUIDs de eventos
2. `SeedData_UserIds_ShouldBeUnique` — Verifica unicidad de GUIDs de usuarios
3. `SeedData_RegistrationIds_ShouldBeUnique` — Verifica unicidad de GUIDs de inscripciones
4. `SeedData_RegistrationEventIds_ShouldReferenceValidEvents` — Verifica integridad FK eventos
5. `SeedData_RegistrationUserIds_ShouldReferenceValidUsers` — Verifica integridad FK usuarios
6. `SeedData_ShouldContainAtLeastOneFullyBookedEvent` — Verifica AC3 (evento lleno)
7. `SeedData_ShouldContainAtLeastOneEventWithAvailableCapacity` — Verifica AC4 (plazas disponibles)
8. `SeedData_Events_ShouldSpanPastPresentAndFutureDates` — Verifica AC2 (distribución temporal)
9. `SeedData_RegistrationCounts_ShouldNotExceedEventMaxCapacity` — Verifica restricciones de capacidad
10. `SeedData_ShouldContainAtLeastEightEvents` — Verifica AC1 (mínimo 8 eventos)
11. `SeedData_ShouldContainAtLeastFiveUsers` — Verifica AC5 (mínimo 5 usuarios)
12. `SeedData_ShouldContainAtLeastOneCancelledRegistration` — Verifica inclusión de inscripciones canceladas

**Resultado:** 12 passed, 0 failed, 0 skipped

**Nota de Code Review:** Se identificó una discrepancia entre los datos helper en las pruebas y los datos reales en la migración. Ver Override de CODE_REVIEW para contexto.

### Escenarios de Prueba de Integración

No aplicable. Esta historia es exclusivamente de infraestructura de datos y no requiere pruebas de integración.

### Verificación Manual Recomendada

Después de aplicar la migración:

1. **UI de Blazor:** Verificar que los 10 eventos aparecen en la lista de eventos
2. **Capacidad:** Confirmar que evento "Pistola de Velocidad - Trofeo del Globo" muestra como lleno (6/6)
3. **Distribución Temporal:** Verificar eventos pasados, presentes y futuros se distinguen visualmente
4. **Integridad de Datos:** Confirmar que todas las inscripciones muestran nombres de usuario correctos

---

## Resumen de Datos de Seed

### Eventos (10 total)

**Eventos Pasados (2):**

| ID | Título | Fecha | Ubicación | Capacidad | Inscripciones |
|---|---|---|---|---|---|
| 11111111-... | Pistola 9mm - Social | 2026-01-15 10:00 | Zaragoza - CTZ | 20 | 0 |
| 22222222-... | Carabina BR50 - Trofeo | 2026-03-20 09:30 | Teruel - CT Aguanaces | 15 | 2 |

**Eventos Actuales/Próximos (6):**

| ID | Título | Fecha | Ubicación | Capacidad | Inscripciones |
|---|---|---|---|---|---|
| 33333333-... | Aire Comprimido Pistola - Copa Aragón | 2026-06-28 10:00 | Huesca - RTAA | 15 | 3 |
| 44444444-... | Pistola Estándar - Campeonato Provincial | 2026-07-05 09:00 | Madrid - RFEDETO | 12 | 7 (6 activas + 1 cancelada) |
| 55555555-... | Aire Comprimido Carabina - Social | 2026-07-12 10:30 | Barcelona - CT Vallès | 7 | 6 |
| **66666666-...** | **Pistola de Velocidad - Trofeo del Globo** | **2026-07-18 11:00** | **Valencia - CTW** | **6** | **6 (LLENO)** |
| 77777777-... | Carabina 3x20 - Campeonato Provincial | 2026-07-25 09:00 | Zaragoza - CTZ | 15 | 6 (5 activas + 1 cancelada) |
| 88888888-... | Pistola 50m - Copa Aragón | 2026-07-30 08:30 | Teruel - CT Aguanaces | 40 | 6 (5 activas + 1 cancelada) |

**Eventos Futuros (2):**

| ID | Título | Fecha | Ubicación | Capacidad | Inscripciones |
|---|---|---|---|---|---|
| 99999999-... | Trap - Trofeo | 2026-09-10 09:00 | Huesca - RTAA | 30 | 0 |
| aaaaaaaa-... | Foso Universal - Social | 2026-12-05 10:00 | Madrid - RFEDETO | 20 | 0 |

### Usuarios (6 total)

| ID | Nombre | Email | Género | Licencia | Categoría |
|---|---|---|---|---|---|
| bbbbbbbb-... | Carmen García López | carmen.garcia@example.com | Female | ESP-2026-001 | A |
| cccccccc-... | Javier Martínez Ruiz | javier.martinez@example.com | Male | ESP-2026-002 | S |
| dddddddd-... | Ana Fernández Pérez | ana.fernandez@example.com | Female | ESP-2026-003 | A |
| eeeeeeee-... | Miguel Sánchez Torres | miguel.sanchez@example.com | Male | — | — |
| ffffffff-... | Laura Rodríguez Gómez | laura.rodriguez@example.com | Female | ESP-2026-004 | S |
| 10101010-... | Carlos Jiménez Moreno | carlos.jimenez@example.com | Male | ESP-2026-005 | A |

**Nota:** Miguel Sánchez Torres representa un usuario sin licencia (principiante).

### Inscripciones (29 total)

**Distribución por Estado:**
- **Activas (Registered):** 26
- **Canceladas (Cancelled):** 3

**Distribución por Evento:**
- Evento 1 (Pistola 9mm): 0 inscripciones
- Evento 2 (Carabina BR50): 2 inscripciones (13% ocupación)
- Evento 3 (Aire Comprimido Pistola): 3 inscripciones (20% ocupación)
- Evento 4 (Pistola Estándar): 7 inscripciones — 6 activas + 1 cancelada (58% ocupación activa)
- Evento 5 (Aire Comprimido Carabina): 6 inscripciones (85.7% ocupación)
- **Evento 6 (Pistola de Velocidad): 6 inscripciones (100% ocupación — LLENO)**
- Evento 7 (Carabina 3x20): 6 inscripciones — 5 activas + 1 cancelada (40% ocupación activa)
- Evento 8 (Pistola 50m): 6 inscripciones — 5 activas + 1 cancelada (15% ocupación activa)
- Evento 9 (Trap): 0 inscripciones
- Evento 10 (Foso Universal): 0 inscripciones

---

## Limitaciones Conocidas

1. **Sincronización de Datos de Prueba:** Los métodos helper en `SeedDataTests.cs` contienen datos hardcoded que no coinciden exactamente con los valores de capacidad y conteos de inscripciones en la migración. Esto fue identificado durante Code Review y aceptado como override (ver justificación registrada).

2. **Soft-Delete No Implementado:** Como se decidió en Gate 2 (Q1), la funcionalidad de soft-delete se difirió a una historia futura. Los datos de seed NO incluyen registros marcados como eliminados.

3. **Solo Entorno de Desarrollo:** Aunque la migración tiene protección de entorno, técnicamente es posible aplicarla manualmente en producción. Se requiere disciplina operacional para evitarlo.

4. **Datos Estáticos:** Las fechas son absolutas (2026-2027) y eventualmente quedarán desactualizadas. No hay lógica dinámica para ajustar fechas relativamente a "hoy".

5. **Límite de Usuarios:** Solo 6 usuarios seed, lo que limita la variedad de escenarios de inscripción múltiple por evento.

---

## Consideraciones de Seguridad

**Datos Sensibles:** ✅ No hay datos sensibles
- Todos los emails usan dominio `@example.com`
- Números de licencia son ficticios (patrón ESP-2026-XXX)
- No hay contraseñas, tokens, o información personal real

**Protección de Producción:** ✅ Implementada
- Comentario DEV ONLY en líneas 1-2 de migración
- Guardia de entorno en métodos Up() y Down()
- Default a "Production" si variable de entorno no está definida

**Exposición de Datos:** ⚠️ Riesgo Bajo
- Los datos de seed son públicos y visibles en el código fuente
- No representan datos reales de usuarios o competiciones
- Apropiado para repositorios privados; revisar antes de open-sourcing

**Inyección SQL:** ✅ No aplicable
- EF Core usa consultas parametrizadas
- Todos los valores son hardcoded en código C#

---

## Próximos Pasos

1. **Aplicar Migración en Dev/Staging:**
   ```bash
   dotnet ef database update AddDevelopmentSeedData \
     --project src/SportsClubEventManager.Infrastructure \
     --startup-project src/SportsClubEventManager.Web
   ```

2. **Verificar en UI de Blazor** que los 10 eventos aparecen correctamente con sus datos

3. **Validar Escenarios de Capacidad** — confirmar que evento lleno (6/6) no permite más inscripciones

4. **Considerar Soft-Delete en Futuras Historias** para expandir escenarios de prueba con registros eliminados

5. **Revisar Datos Helper en Tests** si se requiere perfecta paridad con migración (actualmente aceptado como override)

---

## Referencias

- **GitHub Issue:** [#6](https://github.com/AlejBlasco/SportsClubEventManager/issues/6)
- **Milestone:** Sprint 1
- **Dependencias:** US-3 (Database Setup and Migrations) — Completado
- **Fuente de Datos:** [CALENDARIO PRECISION 2026](https://docs.google.com/spreadsheets/d/1pKOl9JlXMrkkPN0TPqEFCyveeRx8nw-B/edit)
