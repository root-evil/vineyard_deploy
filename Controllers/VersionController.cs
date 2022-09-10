using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

using vineyard_deploy.Models;
using vineyard_deploy.Services;

namespace vineyard_deploy.Controllers;

[ApiController]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class VersionController : ControllerBase
{
    private readonly ILogger logger;
    private readonly VersionService versionService;

    public VersionController(ILogger<VersionController> logger, VersionService versionService)
    {
        this.logger = logger;
        this.versionService = versionService;
    }

    [HttpGet("/version")]
    public VersionInfo GetVersion()
    {
        return versionService.GetVersion();
    }
}
