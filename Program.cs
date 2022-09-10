using System.Net;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using vineyard_deploy.Services;
using vineyard_deploy.Models;


var Port = int.Parse(Environment.GetEnvironmentVariable("VINEYARD_APP_PORT") ?? "0");

// get projects from vineyard_map/dev.json
// get projects from vineyard_map/prod.json

var projects = new ProjectInfo[]{
    new ProjectInfo
    {
        Name = "Backend",
        Port = 5000,
        Repository = "https://github.com/root-evil/vineyard_backend.git",
        Path = "vineyard_backend",
    }
}.AsEnumerable().Where(x => x.Name != "Deploy");

var version = new VersionInfo
{
    Name = "Deploy Backend Service",
    Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "undefined",
    Start = DateTime.UtcNow,
};

//----------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    serverOptions.Listen(IPAddress.Any, Port, listenOptions =>
    {
        listenOptions.UseConnectionLogging();
    });
});

builder.Services.AddSingleton(x => new VersionService(version));

builder.Services.AddSingleton(x => new GithubBackgroundService(projects, x.GetRequiredService<ILogger<GithubBackgroundService>>()));
builder.Services.AddSingleton<IHostedService>(p => p.GetRequiredService<GithubBackgroundService>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = version.Name,
            Version = version.VersionString,
            Description = $"Started: <b>{version.Start:u}</b>",
        });
        options.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Format = "$date-span", Example = new OpenApiString(TimeSpan.Zero.ToString()) });
        Directory.GetFiles(AppContext.BaseDirectory, "*.xml").ToList().ForEach(xmlFilePath => options.IncludeXmlComments(xmlFilePath));
    }
);

var app = builder.Build();

app.UseHttpLogging();
app.UseSwagger();
app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = string.Empty;
        options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{version.Name} {version.VersionString}");
    }
);

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
