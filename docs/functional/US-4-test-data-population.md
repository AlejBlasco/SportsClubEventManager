# Datos de Prueba para Desarrollo — Guía de Usuario
**Versión:** 2026-06-23  
**Aplicable a:** SportsClubEventManager (Entornos de Desarrollo y Staging)

---

## ¿Qué es esta funcionalidad?

Esta funcionalidad proporciona automáticamente datos de prueba realistas en la base de datos cuando se trabaja en entornos de desarrollo o staging. Permite a los desarrolladores y testers trabajar con ejemplos de eventos de tiro deportivo, usuarios y inscripciones sin tener que crear manualmente cada registro.

Los datos están basados en competiciones reales de tiro deportivo en España e incluyen modalidades auténticas como Pistola 9mm, Carabina BR50, Aire Comprimido, y otras competiciones típicas del calendario aragonés de precision shooting.

**Beneficiarios:**
- Desarrolladores que necesitan datos para construir y probar la interfaz de usuario
- Testers que necesitan verificar funcionalidades con escenarios realistas
- Diseñadores que necesitan ver cómo se visualizan los datos en la aplicación

---

## ¿Cómo se usa?

### Paso 1: Verificar el Entorno

Esta funcionalidad **solo está disponible en entornos de desarrollo y staging**. No está presente ni puede activarse en producción.

Para verificar tu entorno:
- Revisa la variable de entorno `ASPNETCORE_ENVIRONMENT`
- Debe estar configurada como `Development` o `Staging`

### Paso 2: Aplicar la Migración de Base de Datos

Los datos de prueba se insertan mediante una migración de base de datos específica.

**Comando a ejecutar (solo la primera vez):**

```bash
dotnet ef database update AddDevelopmentSeedData \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web
```

Este comando debe ejecutarse **una sola vez** por cada base de datos nueva.

### Paso 3: Iniciar la Aplicación

Después de aplicar la migración, inicia la aplicación normalmente:

```bash
dotnet run --project src/SportsClubEventManager.Web
```

### Paso 4: Explorar los Datos en la Interfaz

Navega a la sección de eventos de la aplicación. Verás:
- **10 eventos de tiro deportivo** con diferentes fechas y ubicaciones
- **Eventos pasados** (ya celebrados, útil para probar vistas históricas)
- **Eventos actuales y próximos** (con inscripciones activas)
- **Eventos futuros** (sin inscripciones aún)
- **Un evento completamente lleno** (para probar límites de capacidad)

---

## ¿Qué incluyen los datos de prueba?

### Eventos de Tiro Deportivo (10 eventos)

**Modalidades incluidas:**
- **Pistola 9mm** — Competiciones de pistola calibre 9mm
- **Carabina BR50** — Benchrest precision a 50 metros
- **Aire Comprimido** — Pistola y carabina de aire comprimido
- **Pistola Estándar** — Categoría olímpica de pistola
- **Pistola de Velocidad** — Tiro rápido con requisitos de precisión
- **Carabina 3x20** — Tres posiciones (tendido, de pie, rodilla) a 50m
- **Pistola 50m** — Disciplina olímpica de precisión
- **Trap y Foso Universal** — Tiro al plato

**Ubicaciones incluidas:**
- Zaragoza (CTZ)
- Teruel (CT Aguanaces)
- Huesca (RTAA)
- Madrid (RFEDETO)
- Barcelona (CT Vallès)
- Valencia (CTW)

**Distribución temporal:**
- 2 eventos pasados (enero-marzo 2026)
- 6 eventos actuales/próximos (junio-julio 2026)
- 2 eventos futuros (septiembre-diciembre 2026)

### Usuarios (6 usuarios)

Los datos incluyen 6 tiradores ficticios con nombres españoles realistas:

- **Carmen García López** — Categoría A, con licencia
- **Javier Martínez Ruiz** — Categoría S, con licencia
- **Ana Fernández Pérez** — Categoría A, con licencia
- **Miguel Sánchez Torres** — Sin licencia (representa un principiante)
- **Laura Rodríguez Gómez** — Categoría S, con licencia
- **Carlos Jiménez Moreno** — Categoría A, con licencia

Todos los emails utilizan el dominio `@example.com` para evitar cualquier confusión con usuarios reales.

### Inscripciones (29 inscripciones)

Las inscripciones representan diferentes escenarios de ocupación:

- **Eventos vacíos** — Sin inscripciones aún (útil para probar primeras inscripciones)
- **Eventos con pocas plazas ocupadas** — 13-20% de ocupación
- **Eventos medio llenos** — 33-58% de ocupación
- **Eventos casi llenos** — 85-90% de ocupación
- **Evento completamente lleno** — 100% de ocupación (no permite más inscripciones)
- **Inscripciones canceladas** — 3 inscripciones marcadas como canceladas

---

## Limitaciones

- **Solo para desarrollo:** Estos datos NO están disponibles en producción ni deben aplicarse allí.

- **Datos ficticios:** Aunque las modalidades y ubicaciones son reales, los usuarios y eventos específicos son completamente ficticios.

- **Fechas estáticas:** Los eventos tienen fechas fijas en 2026-2027. Con el tiempo, todos los eventos quedarán en el pasado.

- **Cantidad limitada:** Solo 10 eventos y 6 usuarios. Para probar escenarios con más datos, será necesario crearlos manualmente o ampliar los datos de seed.

- **No se puede deshacer fácilmente:** Una vez aplicada la migración, los datos quedan en la base de datos. Para eliminarlos, se debe resetear toda la base de datos.

---

## Preguntas Frecuentes

**¿Puedo modificar estos datos de prueba en la interfaz?**

Sí. Los datos se insertan en la base de datos como cualquier otro registro. Puedes editarlos, eliminarlos, o crear nuevos eventos e inscripciones. Sin embargo, si reseteas la base de datos, volverán a aparecer los datos originales.

**¿Qué pasa si aplico la migración dos veces?**

No pasa nada. La migración está diseñada para ser idempotente — aplicarla múltiples veces no duplicará los datos gracias al uso de GUIDs estables.

**¿Por qué no veo estos datos en mi aplicación?**

Posibles razones:
1. No has aplicado la migración `AddDevelopmentSeedData` aún
2. Tu entorno no está configurado como Development o Staging
3. Estás conectado a una base de datos diferente a la esperada

Verifica tu connection string en `appsettings.Development.json` y confirma que has ejecutado el comando de migración.

**¿Puedo usar estos datos para demos o presentaciones?**

Sí, ese es uno de los propósitos. Los datos son realistas y representan escenarios típicos del tiro deportivo español, lo que los hace apropiados para demostraciones.

**¿Necesito estos datos para que funcione la aplicación?**

No. La aplicación funciona perfectamente sin datos de seed, solo que empezarás con una base de datos vacía. Los datos de seed son una conveniencia para desarrollo, no un requisito funcional.

**¿Qué hago si quiero empezar de cero sin estos datos?**

Puedes resetear la base de datos completamente:

```bash
dotnet ef database drop --force \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web

dotnet ef database update \
  --project src/SportsClubEventManager.Infrastructure \
  --startup-project src/SportsClubEventManager.Web
```

Esto eliminará todos los datos (incluidos los de seed y cualquier dato manual que hayas creado) y aplicará solo las migraciones de esquema, dejándote con una base de datos vacía.

---

## Escenarios de Prueba Sugeridos

Con estos datos de prueba, puedes validar:

1. **Vista de Lista de Eventos** — Visualización de múltiples eventos con diferentes fechas y capacidades
2. **Filtrado por Fechas** — Separar eventos pasados, actuales y futuros
3. **Indicador de Capacidad** — Ver eventos con plazas disponibles vs eventos llenos
4. **Detalles de Evento** — Información completa de cada evento (ubicación, modalidad, descripción)
5. **Lista de Inscritos** — Ver quién está inscrito en cada evento
6. **Gestión de Cancelaciones** — Inscripciones canceladas no cuentan para la capacidad
7. **Restricción de Sobrecupo** — Evento lleno no permite más inscripciones
8. **Perfil de Usuario** — Ver datos de usuarios con y sin licencia
9. **Caracteres Españoles** — Verificar que nombres con acentos (García, Jiménez, Sánchez) se muestran correctamente

---

## Soporte Técnico

Si encuentras problemas con los datos de prueba:

1. **Verifica el entorno:** Confirma que `ASPNETCORE_ENVIRONMENT=Development`
2. **Revisa la migración:** Ejecuta `dotnet ef migrations list` y confirma que `AddDevelopmentSeedData` aparece
3. **Consulta los logs:** Busca mensajes de error durante la aplicación de migraciones
4. **Contacta al equipo de desarrollo** si el problema persiste

---

## Funcionalidades Relacionadas

- [US-3: Configuración de Base de Datos y Migraciones](./US-3-database-setup.md) — Prerequisito para datos de seed
- [Guía de Eventos](./eventos.md) — Cómo gestionar eventos en la aplicación (pendiente)
- [Guía de Inscripciones](./inscripciones.md) — Cómo inscribirse en eventos (pendiente)
