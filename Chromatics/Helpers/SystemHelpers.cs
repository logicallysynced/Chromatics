using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace Chromatics.Helpers
{
    public static class SystemHelpers
    {
        public static bool IsDarkModeEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 10) // Windows 10 and above
            {
                try
                {
                    // Registry path for the system's theme preference
                    const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                    const string valueName = "AppsUseLightTheme";

                    using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                    {
                        if (key != null)
                        {
                            object registryValueObject = key.GetValue(valueName);
                            if (registryValueObject != null)
                            {
                                int registryValue = (int)registryValueObject;
                                return registryValue == 0; // 0 indicates dark mode, 1 indicates light mode
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read dark mode setting: {ex.Message}");
                }
            }

            return false;
        }
    }
}
