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

    public ProjectStatus Status { get; set; } = ProjectStatus.Dead;
}