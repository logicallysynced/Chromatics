using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    /// <summary>
    /// Gathers Chromatics diagnostic files (console output, layer/palette/settings
    /// files, system info) into a zip the user can share with support. The zip
    /// is staged in a temp directory; callers are responsible for moving it to
    /// its final location and then calling <see cref="Cleanup"/> on the returned
    /// temp directory.
    /// </summary>
    public static class LogCollectionHelper
    {
        public sealed class CollectionResult
        {
            public string ZipPath { get; init; } = string.Empty;
            public string TempDirectory { get; init; } = string.Empty;
        }

        public static async Task<CollectionResult> CollectAsync(IEnumerable<string> consoleLines)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "chromatics-logs-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tempDir);

            var stagingDir = Path.Combine(tempDir, "staging");
            Directory.CreateDirectory(stagingDir);

            await File.WriteAllTextAsync(
                Path.Combine(stagingDir, "console.log"),
                string.Join(Environment.NewLine, consoleLines ?? Array.Empty<string>()),
                Encoding.UTF8);

            await File.WriteAllTextAsync(
                Path.Combine(stagingDir, "system-info.txt"),
                BuildSystemInfo(),
                Encoding.UTF8);

            CopyConfigFiles(stagingDir);

            var zipPath = Path.Combine(tempDir, $"chromatics-logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            ZipFile.CreateFromDirectory(stagingDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            Directory.Delete(stagingDir, recursive: true);

            return new CollectionResult { ZipPath = zipPath, TempDirectory = tempDir };
        }

        public static void Cleanup(string tempDirectory)
        {
            if (string.IsNullOrEmpty(tempDirectory) || !Directory.Exists(tempDirectory)) return;
            try { Directory.Delete(tempDirectory, recursive: true); } catch { }
        }

        private static string BuildSystemInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Chromatics diagnostic bundle ===");
            sb.AppendLine($"Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            sb.AppendLine($"Chromatics: {version}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($"Runtime: {Environment.Version} ({System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription})");
            sb.AppendLine($"Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            sb.AppendLine($"Machine: {Environment.MachineName}");
            sb.AppendLine($"User: {Environment.UserName}");
            sb.AppendLine();
            sb.AppendLine($"BaseDirectory: {AppContext.BaseDirectory}");
            sb.AppendLine($"ConfigDirectory: {FileOperationsHelper.GetConfigDirectory()}");
            sb.AppendLine($"InstallKind: {(IsInstalledBuild() ? "Installer (Velopack)" : "Portable")}");
            return sb.ToString();
        }

        private static void CopyConfigFiles(string destination)
        {
            var configDir = FileOperationsHelper.GetConfigDirectory();
            if (!Directory.Exists(configDir)) return;

            var configSubdir = Path.Combine(destination, "config");
            Directory.CreateDirectory(configSubdir);

            var files = Directory.EnumerateFiles(configDir, "*.chromatics3")
                .Concat(Directory.EnumerateFiles(configDir, "*.chromatics4"));

            foreach (var src in files)
            {
                var dst = Path.Combine(configSubdir, Path.GetFileName(src));
                try { File.Copy(src, dst, overwrite: true); } catch { }
            }
        }

        private static bool IsInstalledBuild()
        {
            var exeDir = AppContext.BaseDirectory;
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return exeDir.StartsWith(Path.Combine(localApp, "Chromatics"), StringComparison.OrdinalIgnoreCase);
        }
    }
}
