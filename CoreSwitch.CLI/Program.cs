using System;
using System.Diagnostics;

namespace CoreSwitch.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var (versions, versionsOk) = VersionManager.GetInstalledVersions();
            if (!versionsOk)
            {
                Console.WriteLine("Could not find any installed sdks.");
                return;
            }

            foreach (var v in versions)
            {
                Console.WriteLine($"v{v}");
            }

            var (version, versionOk) = VersionManager.GetSelectedVersion();
            if (!versionOk)
            {
                Console.WriteLine("Could not determine active sdk version.");
                return;
            }

            Console.WriteLine($"Selected version: {version}");

            if (Debugger.IsAttached)
                Console.ReadKey();
        }
    }
}