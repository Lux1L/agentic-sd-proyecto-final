---
name: bootstrap
description: Convierte esta plantilla en un proyecto nuevo — renombra la solución y (opcionalmente) el namespace raíz con el nombre de la app del usuario, y elimina la feature de ejemplo Todos para empezar desde cero. Úsala justo después de clonar, cuando el usuario quiera iniciar su propio proyecto a partir de la plantilla.
argument-hint: <nombre del nuevo proyecto/solución, p. ej. "Acme.Billing">
---

# Crear un proyecto nuevo a partir de la plantilla

Dos tareas: **(1)** renombrar la plantilla con el nombre del usuario y **(2)** eliminar la feature de ejemplo **Todos** (conservar Users + auth, ya que la mayoría de apps los necesitan). Confirma primero el nuevo nombre con el usuario.

## 1. Renombrar

La plantilla viene como la solución `VerticalSlice` con un único proyecto `Web.Api` (namespace raíz `Web.Api`); no hay proyectos Domain/Application/Infrastructure/SharedKernel.

- Renombra `VerticalSlice.slnx` → `{Name}.slnx`.
- Si el usuario quiere su propio namespace raíz (p. ej. `Acme.Billing` en lugar de `Web.Api`), renombra el namespace `Web.Api` en `src/Web.Api` y en los proyectos de tests: cada `namespace Web.Api.*`, cada `using Web.Api.*`, las entradas `<RootNamespace>`/`InternalsVisibleTo` y el hook de clase parcial `Program`/`Web.Api` que usan los tests de integración. Si le parece bien quedarse con el namespace `Web.Api`, omite este paso.
- Reemplaza los identificadores fijos de la plantilla: el nombre de la base de datos y `Jwt:Issuer`/`Audience` en `appsettings*.json`, `POSTGRES_DB`/los nombres de servicio + `Host=` en `docker-compose.yml`, el título del `README.md` y cualquier `UserSecretsId` si quiere uno nuevo.

## 2. Eliminar el ejemplo Todos

Elimina el slice de Todos en todas partes:

- `src/Web.Api/Features/Todos/` (la carpeta completa: entidad, errores, eventos de dominio, claves de cache y cada archivo de slice).
- `src/Web.Api/Database/Configurations/TodoItemConfiguration.cs`.
- Quita el `DbSet` `TodoItems` de `ApplicationDbContext` (no existe `IApplicationDbContext` ni `TestDbContext` que actualizar; el contexto concreto es la única declaración).
- `tests/Web.Api.UnitTests/Todos/` y `tests/IntegrationTests/Todos/`.
- Elimina la tabla con una migración (`/add-migration Remove_Todos`) o, para una base de datos totalmente nueva, borra `src/Web.Api/Database/Migrations/`, ejecuta `dotnet ef migrations add Initial --project src/Web.Api --startup-project src/Web.Api` y deja que se recree.

## Reglas

- Conserva toda la estructura de proyecto único y cada aspecto transversal (`Common`, `Database`, `Authentication`, `Authorization`, `Notifications`, endpoints, rate limiting, cabeceras de seguridad, observabilidad), además de `Features/Users/` y sus slices de auth; solo se elimina la *feature de ejemplo*.
- Compila y ejecuta `dotnet test` después de cada etapa; los ArchitectureTests y los tests de integración revelan cualquier referencia colgante.
- Deja el paquete `.claude/skills/` en su lugar para que las convenciones acompañen al nuevo proyecto.
