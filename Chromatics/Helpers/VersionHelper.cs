using System.Reflection;

namespace Chromatics.Helpers
{
    public static class VersionHelper
    {
        /// <summary>
        /// The version as users see it, in the window title and the console tab.
        /// Stops at the patch because the assembly version's fourth segment is a
        /// legacy .NET slot that publish.py strips before a release advertises
        /// itself, so showing it would name a version nobody can download.
        /// </summary>
        public static string DisplayVersion
        {
            get
            {
                var version = typeof(VersionHelper).Assembly.GetName().Version;
                if (version == null) return "(unknown version)";

                var betaSuffix = UpdateService.IsBetaChannel() ? " [BETA]" : string.Empty;
                return $"{version.Major}.{version.Minor}.{version.Build}{betaSuffix}";
            }
        }
    }
}
