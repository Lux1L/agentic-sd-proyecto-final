---
name: add-entity
description: Agrega una nueva entidad de dominio a la plantilla de Vertical Slice Architecture — clase de entidad, catálogo de errores, eventos de dominio, configuración de EF Core, registro en el DbContext y migración. Úsala cuando el usuario pida agregar una entidad, un agregado, un modelo de dominio o una tabla.
argument-hint: <descripción de la entidad, p. ej. "Project con un nombre, un owner y una lista de todos">
---

# Agregar una entidad de dominio

Crea una nueva entidad y conéctala a través del único proyecto `Web.Api`, siguiendo el patrón de `TodoItem`. La entidad y sus tipos de dominio viven junto a su feature; solo la configuración de EF y el `DbSet` viven en `Database/`.

## Archivos a crear/modificar

1. **Entidad**: `src/Web.Api/Features/{Entity}/{Entity}.cs`

```csharp
using Web.Api.Common;

namespace Web.Api.Features.Projects;

public sealed class Project : Entity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

`sealed class`, hereda de `Entity` (de `Web.Api.Common`, que le da `DomainEvents` + `Raise(...)`), `Guid Id`, propiedades simples con setter, colecciones inicializadas con `= [];`.

2. **Catálogo de errores**: `src/Web.Api/Features/{Entity}/{Entity}Errors.cs`

```csharp
using Web.Api.Common;

namespace Web.Api.Features.Projects;

public static class ProjectErrors
{
    public static Error NotFound(Guid projectId) => Error.NotFound(
        "Projects.NotFound",
        $"The project with the Id = '{projectId}' was not found");
}
```

Los códigos siguen el formato `"{FeaturePlural}.{Reason}"`. Elige la factory según la semántica: `Error.NotFound` (404), `Error.Conflict` (409), `Error.Problem` (400), `Error.Failure` (500). `Error`/`ErrorType` están en `Web.Api.Common`.

3. **Eventos de dominio**: un record por archivo, `src/Web.Api/Features/{Entity}/{Entity}{PastTenseVerb}DomainEvent.cs`

```csharp
using Web.Api.Common;

namespace Web.Api.Features.Projects;

public sealed record ProjectCreatedDomainEvent(Guid ProjectId) : IDomainEvent;
```

Crea como mínimo el evento `Created`; agrega otros a medida que los commands los necesiten. Los eventos llevan ids, no entidades. `IDomainEvent` está en `Web.Api.Common`.

4. **Configuración de EF**: `src/Web.Api/Database/Configurations/{Entity}Configuration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Web.Api.Features.Projects;
using Web.Api.Features.Users;

namespace Web.Api.Database.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.OwnerId);
    }
}
```

Las relaciones se configuran al estilo shadow (`HasOne<User>().WithMany()`): las entidades guardan ids de claves foráneas, no propiedades de navegación. `ApplyConfigurationsFromAssembly` en `ApplicationDbContext` detecta las configuraciones automáticamente. Referenciar aquí la entidad de otra feature (p. ej. `User`) está bien: es una referencia a un tipo de dominio, no una dependencia de handler entre slices.

5. **Registro en el DbContext**: agrega `DbSet<{Entity}> {Plural}` en `src/Web.Api/Database/ApplicationDbContext.cs`. Este es el **único** lugar donde se declara un `DbSet`; en esta plantilla no existe `IApplicationDbContext`. Los handlers y los tests unitarios usan el `ApplicationDbContext` concreto, así que el nuevo set queda disponible en todas partes automáticamente.

6. **Migración**: desde la raíz del repo (proyecto único, así que es tanto `--project` como `--startup-project`):

```
dotnet ef migrations add Add_{Plural} --project src/Web.Api --startup-project src/Web.Api
```

Los nombres de migración siguen el formato `PascalCase_With_Underscores` (ver `Add_RefreshTokens`).

## Reglas

- La entidad es un tipo de dominio simple en su carpeta de feature: sin atributos de EF ni aspectos de persistencia en ella. Las claves, conversiones y relaciones viven exclusivamente en la configuración de `Database/Configurations/`.
- La entidad, los errores y los eventos de dominio van en `Features/{Entity}/` (namespace `Web.Api.Features.{Entity}`); la configuración de EF va en `Database/Configurations/`; el `DbSet`, en `ApplicationDbContext`.
- Los tipos de dominio se pueden compartir entre features (un slice de otra feature puede referenciar esta entidad), pero nunca se debe referenciar el `Command`/`Handler` de otra feature.
- Ejecuta `dotnet build` y `dotnet test` al terminar.
- Si el usuario también quiere casos de uso para la entidad, continúa con la skill `add-feature`.
