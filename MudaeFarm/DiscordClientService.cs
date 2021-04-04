using System;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Logging;
using Disqord.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IDisqordLogger = Disqord.Logging.ILogger;

namespace MudaeFarm
{
    public interface IDiscordClientService : IHostedService
    {
        ValueTask<DiscordClient> GetClientAsync();
    }

    public class DiscordClientService : BackgroundService, IDiscordClientService
    {
        readonly IOptionsMonitor<GeneralOptions> _options;
        readonly ICredentialManager _credentials;
        readonly ILogger<DiscordClient> _logger;
        readonly IConfigurationRoot _configuration;
        readonly IServiceProvider _services;

        public DiscordClientService(IOptionsMonitor<GeneralOptions> options, ICredentialManager credentials, ILogger<DiscordClient> logger, IConfigurationRoot configuration, IServiceProvider services)
        {
            _options       = options;
            _credentials   = credentials;
            _logger        = logger;
            _configuration = configuration;
            _services      = services;
        }

        readonly TaskCompletionSource<DiscordClient> _source = new TaskCompletionSource<DiscordClient>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // ReSharper disable AccessToDisposedClosure
            await using var client = new DiscordClient(TokenType.User, _credentials.GetToken(), new DiscordClientConfiguration
            {
                Logger                = new LoggerAdaptor(_logger),
                MessageCache          = new DefaultMessageCache(20),
                DefaultRequestOptions = new RestRequestOptionsBuilder().WithCancellationToken(stoppingToken).Build()
            });

            client.Ready += async args =>
            {
                try
                {
                    _logger.LogWarning($"Logged in as {client.CurrentUser}.");

                    foreach (var provider in _configuration.Providers)
                    {
                        if (provider is DiscordConfigurationProvider discordProvider)
                            await discordProvider.InitializeAsync(_services, client, stoppingToken);
                    }

                    // at this point all option values are available
                    await client.SetPresenceAsync(_options.CurrentValue.FallbackStatus);

                    _logger.LogWarning(
                        "MudaeFarm is up and running! " +
                        "MudaeFarm will shut down when you close this window. " +
                        "Refer to https://github.com/chiyadev/MudaeFarm#configuration for the configuration guide. " +
                        "For additional logging information, restart MudaeFarm with a --verbose CLI argument.");

                    _source.TrySetResult(client);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not initialize Discord configuration providers.");
                    _source.TrySetException(e);
                }
            };

            await client.RunAsync(stoppingToken);
            // ReSharper enable AccessToDisposedClosure
        }

        public ValueTask<DiscordClient> GetClientAsync()
        {
            if (_source.Task.IsCompletedSuccessfully)
                return new ValueTask<DiscordClient>(_source.Task.Result);

            return new ValueTask<DiscordClient>(_source.Task);
        }

        sealed class LoggerAdaptor : IDisqordLogger
        {
            readonly ILogger<DiscordClient> _logger;

            public LoggerAdaptor(ILogger<DiscordClient> logger)
            {
                _logger = logger;
            }

            public event EventHandler<MessageLoggedEventArgs> MessageLogged;

            public void Log(object sender, MessageLoggedEventArgs e)
            {
                MessageLogged?.Invoke(sender, e);

                var level = e.Severity switch
                {
                    LogMessageSeverity.Trace       => LogLevel.Trace,
                    LogMessageSeverity.Debug       => LogLevel.Debug,
                    LogMessageSeverity.Information => LogLevel.Information,
                    LogMessageSeverity.Warning     => LogLevel.Warning,
                    LogMessageSeverity.Error       => LogLevel.Error,
                    LogMessageSeverity.Critical    => LogLevel.Critical,

                    _ => LogLevel.None
                };

                // downgrade unknown dispatch logs to debug level
                if (e.Source.Equals("Gateway", StringComparison.OrdinalIgnoreCase) && e.Message.Contains("Unknown dispatch", StringComparison.OrdinalIgnoreCase))
                    level = level < LogLevel.Debug ? level : LogLevel.Debug;

                _logger.Log(level, e.Exception, $"[{e.Source}] {e.Message}");

                // log a note about expired tokens: https://github.com/chiyadev/MudaeFarm/issues/153
                if (e.Message.Contains("AuthenticationFailed", StringComparison.OrdinalIgnoreCase))
                    _logger.Log(level, "Your Discord authentication token seems to have expired. Please try updating your token in \"profiles.json\" file in the folder \"%localappdata%\\MudaeFarm\".");
            }
        }
    }
}