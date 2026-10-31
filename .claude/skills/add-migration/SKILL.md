---
name: add-migration
description: Crea y aplica una migración de EF Core en la plantilla de Vertical Slice Architecture. Úsala cuando el usuario cambie el modelo (agregue o edite una entidad o su configuración) y necesite una migración, o pida actualizar el esquema de la base de datos.
argument-hint: <nombre de la migración o descripción del cambio en el modelo, p. ej. "Add_Projects">
---

# Agregar una migración de EF Core

Esta es una plantilla de proyecto único, así que `src/Web.Api` es **a la vez** el proyecto de migraciones y el proyecto de inicio (contiene el modelo, la cadena de conexión y la DI).

## Pasos

1. Termina primero el cambio en el modelo: la entidad (en `Features/{Entity}/`) + su `IEntityTypeConfiguration` (en `Database/Configurations/`) + un `DbSet` en `ApplicationDbContext` (ver `/add-entity`). **No existe `IApplicationDbContext`**: el `DbSet` en el `ApplicationDbContext` concreto es la única declaración. La migración lee el modelo, así que debe compilar.

2. Crea la migración desde la raíz del repo:

   ```
   dotnet ef migrations add {Migration_Name} --project src/Web.Api --startup-project src/Web.Api
   ```

   Los nombres siguen el formato `PascalCase_With_Underscores` (p. ej. `Add_Projects`, `Add_RefreshTokens`).

3. Revisa los archivos generados en `src/Web.Api/Database/Migrations/`: verifica el `Up`/`Down`, los nombres de tablas/columnas en snake_case y que no se haya eliminado nada inesperado.

4. Aplícala. En Development la app ejecuta `ApplyMigrations()` al iniciar, así que `dotnet run --project src/Web.Api` (con `docker compose up -d` corriendo) la aplica. Para aplicarla explícitamente:

   ```
   dotnet ef database update --project src/Web.Api --startup-project src/Web.Api
   ```

## Reglas

- Requiere la herramienta de EF: `dotnet tool install --global dotnet-ef` si falta `dotnet ef`.
- Nunca edites una migración que ya fue aplicada o commiteada; agrega una nueva.
- Para eliminar la última migración **no aplicada**: `dotnet ef migrations remove --project src/Web.Api --startup-project src/Web.Api`.
- Commitea la migración junto con el cambio en el modelo.
