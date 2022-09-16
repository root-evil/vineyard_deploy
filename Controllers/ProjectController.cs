using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

using vineyard_deploy.Models;
using vineyard_deploy.Services;

namespace vineyard_deploy.Controllers;

[ApiController]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class ProjectController : ControllerBase
{
    private readonly ILogger logger;
    private readonly VersionService versionService;
    private readonly GithubBackgroundService githubBackgroundService;

    public ProjectController(ILogger<ProjectController> logger, VersionService versionService, GithubBackgroundService githubBackgroundService)
    {
        this.logger = logger;
        this.versionService = versionService;
        this.githubBackgroundService = githubBackgroundService;
    }

    [HttpGet("/api/v1/projects")]
    public IEnumerable<ProjectInfo> GetProjects()
    {
        return githubBackgroundService.GetProjects();
    }

    [HttpGet("/api/v1/project/{name}")]
    public ProjectInfo GetProject([FromRoute] string name)
    {
        return githubBackgroundService.GetProject(name);
    }

    [Produces(MediaTypeNames.Text.Plain)]
    [HttpGet("/api/v1/logs/{name}")]
    public async Task<string> GetLogs([FromRoute] string name)
    {
        return await githubBackgroundService.GetLog(name);
    }
}
