using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CoreSwitch
{
    public static class VersionManager
    {
        private static readonly Dictionary<OSPlatform, string> _home = new Dictionary<OSPlatform, string>
        {
            {OSPlatform.Windows, Environment.GetEnvironmentVariable("USERPROFILE")},
            {OSPlatform.Linux, Environment.GetEnvironmentVariable("HOME")},
            {OSPlatform.OSX, Environment.GetEnvironmentVariable("HOME")} // @todo: ensure this is correct
        };

        private static readonly Dictionary<OSPlatform, string> _dotnet = new Dictionary<OSPlatform, string>
        {
            {OSPlatform.Windows, Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", "sdk")},
            {OSPlatform.Linux, Path.Combine("opt", "dotnet", "sdk")},
            {OSPlatform.OSX, ""} // @todo: figure this out
        };

        private const string GlobalJsonFilename = "global.json";

        public static (IEnumerable<string>, bool) GetInstalledVersions()
        {
            var (platform, ok) = GetOSPlatform();
            if (!ok)
                return (null, false);

            return (new DirectoryInfo(_dotnet[platform.Value]).EnumerateDirectories().Select(d => d.Name), true);
        }

        private static (OSPlatform?, bool) GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return (OSPlatform.Windows, true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return (OSPlatform.Linux, true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return (OSPlatform.OSX, true);

            return (null, false);
        }

        public static (string, bool) GetSelectedVersion()
        {
            var (file, ok) = FindGlobalJson();
            if (!ok)
                return (null, false);

            var content = File.ReadAllText(file.FullName);
            if (string.IsNullOrEmpty(content))
                return (null, false);

            try
            {
                var config = JsonConvert.DeserializeObject<GlobalJson>(content);

                return (config.Sdk.Version, true);
            }
            catch
            {
                return (null, false);
            }
        }

        private static (FileInfo, bool) FindGlobalJson()
        {
            var (platform, ok) = GetOSPlatform();
            if (!ok)
                return (null, false);

            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (!directory.GetFiles(GlobalJsonFilename).Any())
            {
                directory = directory.Parent;
                if (directory == null)
                {
                    directory = new DirectoryInfo(_home[platform.Value]);
                    break;
                }
            }

            var file = directory?.GetFiles(GlobalJsonFilename).FirstOrDefault();

            return (file, file != null);
        }

        private class GlobalJson
        {
            public SdkConfig Sdk { get; set; }

            public class SdkConfig
            {
                public string Version { get; set; }
            }
        }
    }
}
