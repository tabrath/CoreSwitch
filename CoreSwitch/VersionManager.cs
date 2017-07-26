using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CoreSwitch
{
    public static class VersionManager
    {
        private static readonly string _home;
        private static readonly string _sdk;
        private static readonly string _dotnet;
        private static readonly bool _success;

        private const string GlobalJsonFilename = "global.json";

        static VersionManager()
        {
            var (platform, ok) = GetOSPlatform();
            if (!ok)
            {
                _success = false;
                return;
            }

            if (platform == OSPlatform.Windows)
            {
                _home = Environment.GetEnvironmentVariable("USERPROFILE");
                _sdk = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", "sdk");
                _dotnet = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", "dotnet.exe");
            }
            else if (platform == OSPlatform.Linux)
            {
                _home = Environment.GetEnvironmentVariable("HOME");
                _sdk = Path.DirectorySeparatorChar + Path.Combine("opt", "dotnet", "sdk");
                _dotnet = Path.DirectorySeparatorChar + Path.Combine("opt", "dotnet", "dotnet");
            }
            else if (platform == OSPlatform.OSX)
            {
                _home = Environment.GetEnvironmentVariable("HOME");
                _sdk = Path.DirectorySeparatorChar + Path.Combine("opt", "dotnet", "sdk"); // @todo: ensure this is correct
                _dotnet = Path.DirectorySeparatorChar + Path.Combine("opt", "dotnet", "dotnet");
            }

            _success = true;
        }

        public static (string[], bool) GetInstalledVersions()
        {
            if (!_success)
                return (null, false);

            return (new DirectoryInfo(_sdk).EnumerateDirectories().Select(d => d.Name).ToArray(), true);
        }

        private static (OSPlatform?, bool) GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return (OSPlatform.Windows, true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return (OSPlatform.Linux, true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return (OSPlatform.OSX, true);

            Logger.Default.Log($"{nameof(GetOSPlatform)}: Could not determine platform");

            return (null, false);
        }

        public static (string, bool) GetSelectedVersion()
        {
            /*var (file, ok) = FindGlobalJson();
            if (ok)
            {
                try
                {
                    var content = File.ReadAllText(file.FullName);
                    var config = JsonConvert.DeserializeObject<GlobalJson>(content);

                    return (config.Sdk.Version, true);
                }
                catch (Exception e)
                {
                    Logger.Default.Log($"{nameof(GetSelectedVersion)}: {e.Message}");
                }
            }*/

            return GetSelectedVersionFallback();
        }

        private static (string, bool) GetSelectedVersionFallback()
        {
            try
            {
                using (var dotnet = Process.Start(new ProcessStartInfo(_dotnet, "--version")
                {
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }))
                {
                    var version = dotnet.StandardOutput.ReadToEnd().TrimStart().TrimEnd();

                    Logger.Default.Log($"{nameof(GetSelectedVersionFallback)}: Got version '{version}' from 'dotnet --version'");

                    return (version, true);
                }
            }
            catch (Exception e)
            {
                Logger.Default.Log($"{nameof(GetSelectedVersionFallback)}: {e.Message}");
                return (null, false);
            }
        }

        private static (FileInfo, bool) FindGlobalJson()
        {
            if (!_success)
                return (null, false);

            try
            {
                var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (!directory.GetFiles(GlobalJsonFilename).Any())
                {
                    directory = directory.Parent;
                    if (directory == null)
                    {
                        directory = new DirectoryInfo(_home);
                        break;
                    }
                }

                var file = directory?.GetFiles(GlobalJsonFilename).FirstOrDefault();

                return (file, file != null);
            }
            catch (Exception e)
            {
                Logger.Default.Log($"{nameof(FindGlobalJson)}: {e.Message}");
                return (null, false);
            }
        }

        public static (string, string, string) SetVersion(string version, bool global = false)
        {
            var (file, ok) = FindGlobalJson();
            if (!ok)
                file = new FileInfo(Path.Combine(global ? _home : Directory.GetCurrentDirectory(), GlobalJsonFilename));

            try
            {
                using (var writer = file.CreateText())
                {
                    var config = GlobalJson.Create(version);
                    var serialized = JsonConvert.SerializeObject(config, Formatting.Indented);
                    writer.Write(serialized);
                }

                return (version, file.FullName, null);
            }
            catch (Exception e)
            {
                Logger.Default.Log($"{nameof(SetVersion)}: {e.Message}");
                return (null, null, e.Message);
            }
        }

        private class GlobalJson
        {
            [JsonProperty(PropertyName = "sdk")]
            public SdkConfig Sdk { get; set; }

            public class SdkConfig
            {
                [JsonProperty(PropertyName = "version")]
                public string Version { get; set; }
            }

            public static GlobalJson Create(string version)
            {
                return new GlobalJson
                {
                    Sdk = new SdkConfig
                    {
                        Version = version
                    }
                };
            }
        }
    }
}
