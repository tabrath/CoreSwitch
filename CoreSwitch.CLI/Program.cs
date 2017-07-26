using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace CoreSwitch.CLI
{
    class Program
    {
        private const ConsoleColor ErrorColor = ConsoleColor.Red;
        private const ConsoleColor SuccessColor = ConsoleColor.Green;
        private const ConsoleColor SelectionColor = ConsoleColor.White;
        private const ConsoleColor LoggerColor = ConsoleColor.DarkGray;

        static void Main(string[] args)
        {
#if DEBUG
            var unlog = Logger.Default.Subscribe(line => ColorConsole.WriteLine(LoggerColor, $"[logger] {line}"));
            try
            {
#endif
                var (versions, versionsOk) = VersionManager.GetInstalledVersions();
                if (!versionsOk)
                {
                    ColorConsole.WriteLine(ErrorColor, "Error: Could not find any installed sdks.");
                    return;
                }

                var (version, isGlobal, versionOk) = VersionManager.GetSelectedVersion();
                if (!versionOk)
                {
                    ColorConsole.WriteLine(ErrorColor, "Error: Could not determine active sdk version.");
                    return;
                }

                if (args.Length == 0)
                {
                    PrintInstalledVersions(versions, version, isGlobal);
                }
                else
                {
                    var optionsArgs = args.Where(a => a.StartsWith("--")).ToArray();
                    var rawArgs = args.Except(optionsArgs).ToArray();
                    var options = Options.Parse(optionsArgs.Select(a => a.Substring(2).ToLower()).ToArray());

                    Logger.Default.Log($"options: {string.Join(", ", optionsArgs)}");
                    Logger.Default.Log($"args: {string.Join(", ", rawArgs)}");

                    if (rawArgs.Length == 0)
                    {
                        Console.WriteLine("Usage: version [options]");
                        Console.WriteLine("  --global    use global.json in user directory");
                        Console.WriteLine("  --force     force creation of global.json");
                        Console.WriteLine();
                        Console.WriteLine("Version must be in semantic version format (x.x.x-?) or 'latest'");
                        Console.WriteLine("Omitting --global will place global.json in current directory");
                        return;
                    }

                    var match = Regex.Match(rawArgs[0], @"^[0-9]\.[0-9]\.[0-9](\-[\S]+)?$");
                    var argVersion = match.Success
                        ? match.Value
                        : rawArgs[0].ToLower() == "latest"
                            ? versions.Last()
                            : null;

                    if (argVersion == null)
                    {
                        ColorConsole.WriteLine(ErrorColor, "Error: Not a valid version number");
                        return;
                    }

                    if (!options.Force && argVersion.Equals(version))
                    {
                        ColorConsole.WriteLine(ErrorColor, $"Error: Given version is equal to current version ({version})");
                        return;
                    }

                    if (!versions.Contains(argVersion))
                    {
                        ColorConsole.WriteLine(ErrorColor, $"Error: Given version does not match any installed versions ({version})");
                        return;
                    }

                    var (newVersion, filename, error) = VersionManager.SetVersion(argVersion, options.Global);
                    if (error != null)
                    {
                        ColorConsole.WriteLine(ErrorColor, $"Error: Could not set version: {error}");
                        return;
                    }

                    ColorConsole.WriteLine(SuccessColor, $"Successfully set new sdk version to: {newVersion} in {filename}");
                }
#if DEBUG
                if (Debugger.IsAttached)
                    Console.ReadKey();
            }
            finally
            {
                unlog();
            }
#endif
        }

        private class Options
        {
            public bool Global { get; set; } = false;
            public bool Force { get; set; } = false;

            public static Options Parse(params string[] args)
            {
                return new Options
                {
                    Global = args.Contains("global"),
                    Force = args.Contains("force")
                };
            }
        }

        private static void PrintInstalledVersions(string[] versions, string version, bool isGlobal)
        {
            Console.WriteLine("Installed versions:");
            foreach (var v in versions)
            {
                if (version == v)
                {
                    ColorConsole.WriteLine(SelectionColor, $" * {v}{(isGlobal?" (global)":"")}");
                }
                else
                {
                    Console.WriteLine($"   {v}");
                }
            }
        }
    }

    internal static class ColorConsole
    {
        public static void WriteLine(ConsoleColor color, params string[] lines)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            lines.ToList().ForEach(Console.WriteLine);
            Console.ForegroundColor = originalColor;
        }
    }
}