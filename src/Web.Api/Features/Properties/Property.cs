using Web.Api.Common;

namespace Web.Api.Features.Properties;

public sealed class Property : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public PropertyType Type { get; set; }
    public PropertyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
