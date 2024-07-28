using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Chromatics.Core;
using Chromatics.Enums;
using MetroFramework.Components;
using Newtonsoft.Json;

namespace Chromatics.Localization
{
    public static class LocalizationManager
    {
        // Dictionary to store translations for the current locale
        private static Dictionary<string, string> translations;

        // Method to get localized text
        public static string GetLocalizedText(string text)
        {
            var settings = AppSettings.GetSettings();
            var locale = settings.systemLanguage; // Returns Language.English, Language.Japanese, etc.

            // Ensure the translations are loaded for the current locale
            LoadTranslations(locale);

            // Return the translated text if available; otherwise, return the original text
            return translations.TryGetValue(text, out var localizedText) ? localizedText : text;
        }

        // Method to load translations from the JSON file based on the locale
        private static void LoadTranslations(Language locale)
        {
            // Avoid reloading if already loaded
            if (translations != null && translations.Count > 0)
            {
                return;
            }

            var environment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var localeFileName = GetLocaleFileName(locale);
            var filePath = Path.Combine(environment, "locale", localeFileName);

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Load the JSON file content and deserialize it into the dictionary
                var jsonContent = File.ReadAllText(filePath);
                translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
            }
            else
            {
                // If the file does not exist, fallback to English (or any default locale)
                translations = new Dictionary<string, string>();
            }
        }

        // Method to get the JSON file name based on the locale
        private static string GetLocaleFileName(Language locale)
        {
            return locale switch
            {
                Language.English => "en.json",
                Language.Japanese => "ja.json",
                Language.French => "fr.json",
                Language.German => "de.json",
                Language.Spanish => "es.json",
                Language.Korean => "ko.json",
                Language.Chinese => "zh_CN.json",

                // Add other cases for different languages
                _ => "en.json" // Default to English if the locale is not recognized
            };
        }

        // Method to localize all controls on a form
        public static void LocalizeForm(Form form)
        {
            // Localize the controls
            LocalizeControl(form);

            // Localize the tooltips
            LocalizeTooltips(form);
        }

        // Method to recursively localize controls
        private static void LocalizeControl(Control control)
        {
            // Localize the Text property if available
            if (!string.IsNullOrEmpty(control.Text))
            {
                control.Text = GetLocalizedText(control.Text);
            }

            // Recursively localize child controls
            foreach (Control childControl in control.Controls)
            {
                LocalizeControl(childControl);
            }
        }

        // Method to localize tooltips associated with controls on a form
        private static void LocalizeTooltips(Form form)
        {
            foreach (var field in form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(ToolTip))
                {
                    var toolTip = (ToolTip)field.GetValue(form);
                    foreach (Control control in form.Controls)
                    {
                        string originalText = toolTip.GetToolTip(control);
                        if (!string.IsNullOrEmpty(originalText))
                        {
                            toolTip.SetToolTip(control, GetLocalizedText(originalText));
                        }
                    }
                }
            }
        }
    }
}
