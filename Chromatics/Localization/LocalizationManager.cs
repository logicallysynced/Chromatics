using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Chromatics.Core;
using Chromatics.Enums;
using MetroFramework.Components;
using Newtonsoft.Json;

public static class LocalizationManager
{
    private static Dictionary<string, string> translations = new Dictionary<string, string>();
    private static Language currentLanguage;
    private static Dictionary<Control, string> originalTexts = new Dictionary<Control, string>();
    private static Dictionary<object, Dictionary<Control, string>> originalTooltips = new Dictionary<object, Dictionary<Control, string>>();
    private static Dictionary<DataGridView, Dictionary<(int, int), string>> originalDataGridViewTexts = new Dictionary<DataGridView, Dictionary<(int, int), string>>();

    public static string GetLocalizedText(string text)
    {
        var settings = AppSettings.GetSettings();
        var locale = settings.systemLanguage;

        if (currentLanguage != locale)
        {
            LoadTranslations(locale);
        }

        return translations.TryGetValue(text, out var localizedText) ? localizedText : text;
    }

    private static void LoadTranslations(Language locale)
    {
        translations.Clear();

        var environment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
        var localeFileName = GetLocaleFileName(locale);
        var filePath = Path.Combine(environment, "locale", localeFileName);

        if (File.Exists(filePath))
        {
            var jsonContent = File.ReadAllText(filePath);
            translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
            currentLanguage = locale;
        }
        else
        {
            Debug.WriteLine($"Translation file {localeFileName} not found. Falling back to original text.");
        }
    }

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
            _ => "en.json"
        };
    }

    public static void LocalizeForm(Form form)
    {
        var settings = AppSettings.GetSettings();
        var locale = settings.systemLanguage;

        if (currentLanguage != locale)
        {
            LoadTranslations(locale);
        }

        Debug.WriteLine($"Translating Form to {currentLanguage}");

        LocalizeControl(form);

        form.Invalidate();
        form.Refresh();
    }

    private static void LocalizeControl(Control control)
    {
        if (control.Name == "cb_language" || control.Name == "rtb_console") return;

        if (control is DataGridView dataGridView)
        {
            LocalizeDataGridView(dataGridView);
            return;
        }

        // Locate and handle ToolTip and MetroToolTip instances
        var controlType = control.GetType();
        var fields = controlType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var fieldValue = field.GetValue(control);
            if (fieldValue is ToolTip toolTip)
            {
                LocalizeTooltips(toolTip, control);
            }
            else if (fieldValue is MetroToolTip metroToolTip)
            {
                LocalizeTooltips(metroToolTip, control);
            }
        }

        if (!string.IsNullOrEmpty(control.Text))
        {
            if (!originalTexts.ContainsKey(control))
            {
                originalTexts[control] = control.Text;
            }

            control.Text = GetLocalizedText(originalTexts[control]);
        }

        foreach (Control childControl in control.Controls)
        {
            LocalizeControl(childControl);
        }
    }

    private static void LocalizeTooltips(object toolTipObj, Control control)
    {
        var toolTip = toolTipObj as ToolTip;
        var metroToolTip = toolTipObj as MetroToolTip;

        if (toolTip == null && metroToolTip == null)
            return;

        // Use appropriate dictionary based on toolTip or metroToolTip
        var currentToolTip = toolTip ?? (ToolTip)metroToolTip;
        if (currentToolTip == null)
            return;

        if (!originalTooltips.ContainsKey(currentToolTip))
        {
            originalTooltips[currentToolTip] = new Dictionary<Control, string>();
        }

        var controls = GetControls(control);
        foreach (var ctrl in controls)
        {
            var originalText = toolTip != null ? toolTip.GetToolTip(ctrl) : metroToolTip.GetToolTip(ctrl);
            if (!string.IsNullOrEmpty(originalText))
            {
                if (!originalTooltips[currentToolTip].ContainsKey(ctrl))
                {
                    originalTooltips[currentToolTip][ctrl] = originalText;
                }

                var localizedText = GetLocalizedText(originalTooltips[currentToolTip][ctrl]);
                if (toolTip != null)
                {
                    toolTip.SetToolTip(ctrl, localizedText);
                }
                else if (metroToolTip != null)
                {
                    metroToolTip.SetToolTip(ctrl, localizedText);
                }
            }
        }
    }

    private static void LocalizeDataGridView(DataGridView dataGridView)
    {
        if (!originalDataGridViewTexts.ContainsKey(dataGridView))
        {
            originalDataGridViewTexts[dataGridView] = new Dictionary<(int, int), string>();
        }

        foreach (DataGridViewColumn column in dataGridView.Columns)
        {
            if (column.Name == "mappings_col_type")
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    var cell = row.Cells[column.Index];
                    if (cell.Value is string cellValue)
                    {
                        var key = (row.Index, column.Index);
                        if (!originalDataGridViewTexts[dataGridView].ContainsKey(key))
                        {
                            originalDataGridViewTexts[dataGridView][key] = cellValue;
                        }

                        cell.Value = GetLocalizedText(originalDataGridViewTexts[dataGridView][key]);
                    }
                }
            }
        }

        dataGridView.CellMouseEnter -= DataGridView_CellMouseEnter;
        dataGridView.CellMouseEnter += DataGridView_CellMouseEnter;

        dataGridView.Refresh();
    }

    private static void DataGridView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
    {
        if (sender is DataGridView dataGridView)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var cell = dataGridView[e.ColumnIndex, e.RowIndex];
                if (cell.Value is string cellValue)
                {
                    var toolTip = dataGridView.FindForm()?.GetType()
                        .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(f => f.FieldType == typeof(ToolTip) || f.FieldType == typeof(MetroToolTip))
                        .Select(f => f.GetValue(dataGridView.FindForm()) as ToolTip)
                        .FirstOrDefault();

                    if (toolTip != null)
                    {
                        toolTip.SetToolTip(dataGridView, GetLocalizedText(cellValue));
                    }
                }
            }
        }
    }

    private static IEnumerable<Control> GetControls(Control control)
    {
        var controls = control.Controls.Cast<Control>();
        return controls.SelectMany(ctrl => GetControls(ctrl)).Concat(controls);
    }
}
