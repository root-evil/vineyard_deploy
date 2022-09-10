using vineyard_deploy.Models;

namespace vineyard_deploy.Services;

public class VersionService
{
    private readonly VersionInfo version;
    public VersionService(VersionInfo version)
    {
        this.version = version;
    }

    public VersionInfo GetVersion()
    {
        return version;
    }
}