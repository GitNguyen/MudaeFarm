using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public interface ICredentialManager
    {
        string SelectedProfile { get; set; }

        string GetToken();
    }

    public class CredentialManager : ICredentialManager
    {
        static readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MudaeFarm", "profiles.json");

        readonly ILogger<CredentialManager> _logger;

        public CredentialManager(ILogger<CredentialManager> logger)
        {
            _logger = logger;
        }

        public string SelectedProfile { get; set; } = "default";

        public string GetToken()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? "");

            // profile-token mapping
            var profiles = new Dictionary<string, string>();

            try
            {
                profiles = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_path));
            }
            catch (IOException) { }

            var token = null as string;

            while (profiles.Count != 0 && !profiles.TryGetValue(SelectedProfile, out token))
            {
                Console.Write($"\n\nProfiles (Light Support):\n{string.Join('\n', profiles.Keys.Select(k => $"  - {k}"))}\n\nSelect profile: ");

                SelectedProfile = Console.ReadLine() ?? "";
            }

            _logger.LogInformation($"Selected profile '{SelectedProfile}'.");

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine(@"
MudaeFarm requires your user token in order to proceed.

A user token is a long piece of text that is synonymous to your Discord password.
How to find your token: https://github.com/chiyadev/MudaeFarm/blob/master/User%20tokens.md

What happens when you enter your token:
  - MudaeFarm will save this token to the disk UNENCRYPTED. (see %localappdata%\MudaeFarm)
  - MudaeFarm will authenticate to Discord using this token, ACTING ON BEHALF OF YOU.

MudaeFarm makes no guarantee regarding your account's privacy nor safety.
If you are concerned, you may inspect MudaeFarm's complete source code at: https://github.com/chiyadev/MudaeFarm

MudaeFarm is licensed under the MIT License. The authors of MudaeFarm shall not be held liable for any claim, damage or liability.
You can read the license terms at: https://github.com/chiyadev/MudaeFarm/blob/master/LICENSE
".Trim());

                Console.Write("Enter token: ");

                profiles[SelectedProfile] = Console.ReadLine();

                File.WriteAllText(_path, JsonConvert.SerializeObject(profiles, Formatting.Indented));
            }

            var selected = SelectedProfile;

            for (var i = 0; i < profiles.Count; i++)
            {
                profiles.TryGetValue(selected, out var value);

                // aliased profiles
                if (value != null && profiles.ContainsKey(value))
                {
                    selected = value;
                    continue;
                }

                return value;
            }

            throw new StackOverflowException("Detected circular references in aliased profiles.");
        }
    }
}