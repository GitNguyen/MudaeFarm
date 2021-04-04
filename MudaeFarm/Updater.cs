using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using FileMode = System.IO.FileMode;

namespace MudaeFarm
{
    public class Updater : BackgroundService
    {
        public static readonly Version CurrentVersion = typeof(Program).Assembly.GetName().Version;

        readonly IDiscordClientService _discord;
        readonly GitHubClient _github;
        readonly HttpClient _http;
        readonly IOptionsMonitor<GeneralOptions> _options;
        readonly ILogger<Updater> _logger;

        public Updater(IDiscordClientService discord, GitHubClient github, HttpClient http, IOptionsMonitor<GeneralOptions> options, ILogger<Updater> logger)
        {
            _discord = discord;
            _github  = github;
            _http    = http;
            _options = options;
            _logger  = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // this is a bit hacky; we will wait for discord to get fully initialized and then read auto-update setting
            await _discord.GetClientAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_options.CurrentValue.AutoUpdate)
                    await CheckAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        public async Task CheckAsync(CancellationToken cancellationToken = default)
        {
            Release release;
            Version version;

            try
            {
                release = await _github.Repository.Release.GetLatest("chiyadev", "MudaeFarm");

                if (!Version.TryParse(release.Name.TrimStart('v'), out version))
                    return;

                if (CurrentVersion.CompareTo(version) >= 0)
                    return;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Could not check for updates.");
                return;
            }

            try
            {
                var asset = release.Assets.FirstOrDefault(a => a.Name.Contains(".zip"));

                if (asset == null)
                    return;

                _logger.LogInformation($"Downloading update v{version.ToString(3)}...");

                var zipPath = Path.GetTempFileName();
                var dirPath = Path.ChangeExtension(zipPath, "2");

                await using (var source = await _http.GetStreamAsync(asset.BrowserDownloadUrl))
                await using (var destination = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                    await source.CopyToAsync(destination, cancellationToken);

                ZipFile.ExtractToDirectory(zipPath, dirPath);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName        = new DirectoryInfo(dirPath).EnumerateFiles("*.exe").First().FullName,
                        UseShellExecute = false,
                        ArgumentList =
                        {
                            "--kill", Process.GetCurrentProcess().Id.ToString(),
                            "--update", AppDomain.CurrentDomain.BaseDirectory
                        }
                    }
                };

                // updater process will kill us
                process.Start();
                process.WaitForExit();

                _logger.LogWarning("Could not install an update because the updater exited unexpectedly.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Could not install an update.");
            }
        }

        public static Task KillProcessAsync(int pid, CancellationToken cancellationToken = default)
        {
            var process = Process.GetProcessById(pid);

            process.Kill();

            return Task.Run(process.WaitForExit, cancellationToken);
        }

        public static async Task InstallUpdateAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Installation directory '{path}' does not exist.");
                return;
            }

            Console.WriteLine($"Installing update v{CurrentVersion.ToString(3)} to '{path}'...");

            // delete all existing files except logs
            foreach (var file in Directory.EnumerateFiles(path))
            {
                for (var i = 0; i < 10; i++)
                {
                    try
                    {
                        if (!file.StartsWith("log", StringComparison.OrdinalIgnoreCase))
                            File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Could not delete '{file}'. {e}");

                        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                    }
                }
            }

            foreach (var file in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory))
            {
                try
                {
                    File.Copy(file, Path.Combine(path, Path.GetFileName(file)), true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not copy '{file}'. {e}");

                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName        = new DirectoryInfo(path).EnumerateFiles("*.exe").First().FullName,
                UseShellExecute = false
            });

            Environment.Exit(0);
        }
    }
}