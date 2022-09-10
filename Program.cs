using System.Text.Json;
using System.Net;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using vineyard_deploy.Services;
using vineyard_deploy.Models;

var projects = JsonSerializer.Deserialize<List<ProjectInfo>>(File.ReadAllText("../vineyard_map/map.json"));

var Port = projects?.Where(x => x.Name == "Deploy").Single().Port ?? 0;

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
