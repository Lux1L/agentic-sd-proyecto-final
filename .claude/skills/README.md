# Skills de agente de Vertical Slice Architecture para Claude Code

Un paquete de skills que le enseña a Claude Code las convenciones de la plantilla de Vertical Slice Architecture, para que cada feature que construya se vea como si la hubieras escrito tú: un archivo por caso de uso, handlers propios de command/query (sin MediatR), manejo de errores basado en `Result`, acceso directo a `ApplicationDbContext`, endpoints con minimal API y cobertura completa de tests.

## Qué incluye

| Skill | Cómo invocarla | Qué hace |
|---|---|---|
| **add-feature** | `/add-feature archive a todo item` | Genera un vertical slice completo en un solo archivo: command/query anidado, handler, validator, endpoint, más tests unitarios, de validator y de integración. |
| **add-entity** | `/add-entity Project with a name and owner` | Agrega una entidad de dominio de punta a punta: entidad, catálogo de errores, eventos de dominio, configuración de EF, registro en el DbContext y migración. |
| **add-tests** | `/add-tests CreateTodo` | Completa los tests de handler, de validator y de integración que falten en casos de uso existentes. |
| **add-migration** | `/add-migration Add_Projects` | Crea y aplica una migración de EF Core (proyecto único: `src/Web.Api` es a la vez el proyecto de migraciones y el de inicio). |
| **add-background-job** | `/add-background-job delete expired refresh tokens hourly` | Agrega un `BackgroundService` programado que resuelve los servicios scoped de forma segura en cada ejecución. |
| **bootstrap** | `/bootstrap Acme.Billing` | Renombra la solución con el nombre de tu app y elimina la feature de ejemplo Todos (conserva Users + auth). |
| **vsa-review** | `/vsa-review` | Revisa los cambios pendientes contra las convenciones de la plantilla: slices de un solo archivo, aislamiento entre slices, manejo de errores, seguridad, cache y cobertura de tests. |

No es necesario invocarlas explícitamente: una vez instaladas, Claude Code elige la skill correcta automáticamente cuando dices algo como "agrega un endpoint para posponer un todo".

## Instalación

Las skills están en `.claude/skills/`. Si clonaste la plantilla, ya están activas: solo abre el repo en Claude Code.

Para usarlas en otro proyecto basado en esta plantilla, copia la carpeta:

```
your-project/
└── .claude/
    └── skills/
        ├── add-feature/
        ├── add-entity/
        ├── add-tests/
        ├── add-migration/
        ├── add-background-job/
        ├── bootstrap/
        └── vsa-review/
```

Funciona tanto con la variante estándar como con la variante Aspire de la plantilla.

## Pruébalo

```
/add-feature snooze a todo until a given date
```

Claude creará el slice de un solo archivo (el command, el validator, el handler con verificación de propiedad, evento de dominio e invalidación de cache, y el endpoint) junto con los tres tipos de tests, y luego compilará y ejecutará los tests.

## Personalización

Cada skill es un archivo Markdown simple (`SKILL.md`, más plantillas en `references/`). ¿Prefieres otra estructura de carpetas, records en todas partes u otro stack de testing? Edita las plantillas una vez y todas las features futuras seguirán ese cambio. Las skills son la versión ejecutable del documento de convenciones de tu equipo.
