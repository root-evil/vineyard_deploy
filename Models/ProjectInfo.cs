using System.Text.Json.Serialization;

namespace vineyard_deploy.Models;

public enum ProjectStatus
{
    Alive,
    Dead,
}

public record ProjectInfo
{
    public string? Name { get; set; }
    public string? Repository { get; set; }
    public int Port { get; set; }
    public string? Path { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProjectStatus Status { get; set; } = ProjectStatus.Dead;
}