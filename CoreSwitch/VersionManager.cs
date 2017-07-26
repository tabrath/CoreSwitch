﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CoreSwitch
{
    public static class VersionManager
    {
        private static readonly string _home;
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
                _dotnet = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", "sdk");
            }
            else if (platform == OSPlatform.Linux)
            {
                _home = Environment.GetEnvironmentVariable("HOME");
                _dotnet = Path.Combine("opt", "dotnet", "sdk");
            }
            else if (platform == OSPlatform.OSX)
            {
                _home = Environment.GetEnvironmentVariable("HOME");
                _dotnet = Path.Combine("opt", "dotnet", "sdk"); // @todo: ensure this is correct
            }

            _success = true;
        }

        public static (IEnumerable<string>, bool) GetInstalledVersions()
        {
            if (!_success)
                return (null, false);

            return (new DirectoryInfo(_dotnet).EnumerateDirectories().Select(d => d.Name), true);
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
            if (!_success)
                return (null, false);

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
