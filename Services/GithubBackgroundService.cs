using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.IO;
using System.Collections.Generic;

using vineyard_deploy.Models;

namespace vineyard_deploy.Services;

public class GithubBackgroundService : BackgroundService
{
    private readonly ILogger logger;
    private readonly IEnumerable<ProjectInfo> projects;
    private readonly Dictionary<ProjectInfo, Process> processes = new Dictionary<ProjectInfo, Process>();
    private DirectoryInfo workDirectory;

    public GithubBackgroundService( 
        IEnumerable<ProjectInfo> projects,
        ILogger<GithubBackgroundService> logger)
    {
        this.logger = logger;
        this.projects = projects;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        foreach(var (key, process) in processes)
        {
            process.Kill(true);
            logger.LogInformation($"Stopped : {key}");
        }
        Directory.Delete(this.workDirectory.FullName, true);
        logger.LogInformation($"Directory deleted : {this.workDirectory.FullName}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.workDirectory = Directory.CreateDirectory($"Workspace");
        logger.LogInformation($"Directory created : {this.workDirectory.FullName}");

        stoppingToken.Register(() => logger.LogDebug($"Service is stopping (stoppingToken)."));
        logger.LogInformation($"Service is starting");
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach(var project in projects)
                await ProcessProjectAsync(project, stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        logger.LogInformation($"Service is stopping");
    }
    public async Task ProcessProjectAsync(ProjectInfo project, CancellationToken stoppingToken)
    {
        try
        {
            if(project.Repository != null && project.Path != null)
            {
                await GetRepo(project.Repository);
                if(await UpdateRepo(project.Path) || (project.Status == ProjectStatus.Dead))
                    await StartProject(project);
            }
            logger.LogInformation($"Processed {project}");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Service ProcessProjectAsync. " + ex.ToString());
        }
    }

    private async Task StartProject(ProjectInfo project)
    {
        if(project.Path == null)
            return;

        var oldProcess = processes.GetValueOrDefault(project);
        if(oldProcess != null)
        {
            oldProcess.Kill(true);
            project.Status = ProjectStatus.Dead;
        }

        logger.LogInformation($"Start project {project}...");
        var process = new Process() {
            EnableRaisingEvents = true,
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"startup.sh",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.Combine(workDirectory.FullName, project.Path),
            }
        };
        process.StartInfo.Environment.Add("VINEYARD_APP_PORT", project.Port.ToString());
        process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[ {project.Name} ] {e.Data}");
            }
        });
        process.Exited += (sender, e) => {
            project.Status = ProjectStatus.Dead;
            logger.LogInformation($"Project stopped {project}...");
        };
        
        process.Start();
        process.BeginOutputReadLine();
        project.Status = ProjectStatus.Alive;
        processes[project] = process;
    }

    private async Task<bool> GetRepo(string path)
    {
        logger.LogInformation($"Cloning : {path}");
        var process = new Process() {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone {path}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDirectory.FullName,
            }
        };
        process.Start();
        await process.WaitForExitAsync();
        string result = await process.StandardError.ReadToEndAsync();
        logger.LogInformation($"Clone result : {result}");
        return !result.Contains("already exists and is not an empty directory.");
    }

    private async Task<bool> UpdateRepo(string path)
    {
        logger.LogInformation($"Pull : {path}");
        var process = new Process() {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"pull",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.Combine(workDirectory.FullName, path),
            }
        };
        process.Start();
        await process.WaitForExitAsync();
        string result = await process.StandardOutput.ReadToEndAsync();
        logger.LogInformation($"Pull result : {result}");
        return !result.Contains("Already up to date.");
    }
}