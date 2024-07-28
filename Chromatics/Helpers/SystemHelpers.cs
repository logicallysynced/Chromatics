using Chromatics.Enums;
using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.Helpers
{
    public class SystemHelpers
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        public static bool IsDarkModeEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 17763) // Windows 10 version 1809
            {
                int isDark = 0;
                IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (hwnd != IntPtr.Zero)
                {
                    DwmGetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, out isDark, Marshal.SizeOf(typeof(int)));
                }
                return isDark == 1;
            }
            // Default to light mode for unsupported versions
            return false;
        }

    }

    public static class DarkModeManager
    {
        // Dictionary to store original colors of controls
        private static Dictionary<Control, (Color BackColor, Color ForeColor)> originalColors
            = new Dictionary<Control, (Color BackColor, Color ForeColor)>();

        public static void ToggleDarkMode(Form form, bool enableDarkMode)
        {
            if (enableDarkMode)
            {
                // Store original colors before applying dark mode
                StoreOriginalColors(form);
                ApplyTheme(form, true);
            }
            else
            {
                // Restore original colors when switching back
                RestoreOriginalColors(form);
            }

            // Invalidate the entire form to ensure all changes are applied
            form.Invalidate(true);
            form.Refresh();
        }

        private static void StoreOriginalColors(Control control)
        {
            if (!originalColors.ContainsKey(control))
            {
                // Store the current colors of the control
                originalColors[control] = (control.BackColor, control.ForeColor);
            }

            // Recursively store colors for all child controls, excluding specific types
            foreach (Control childControl in control.Controls)
            {
                if (!(childControl is MetroTile))
                {
                    StoreOriginalColors(childControl);
                }
            }
        }

        private static void RestoreOriginalColors(Control control)
        {
            if (originalColors.ContainsKey(control))
            {
                // Restore the stored colors
                var colors = originalColors[control];
                control.BackColor = colors.BackColor;
                control.ForeColor = colors.ForeColor;
                control.Invalidate(); // Ensure control is repainted
            }

            // Recursively restore colors for all child controls, excluding specific types
            foreach (Control childControl in control.Controls)
            {
                if (!(childControl is MetroTile))
                {
                    RestoreOriginalColors(childControl);

                    ApplyTheme(childControl, false);
                }
            }
        }

        private static void ApplyTheme(Control control, bool isDarkMode)
        {
            // Exclude specific control types from theming
            if (control is MetroTile)
            {
                return;
            }

            // Set colors based on dark mode
            if (isDarkMode)
            {
                control.BackColor = Color.FromArgb(45, 45, 48); // Dark background
                
                if (!(control is RichTextBox))
                {
                    control.ForeColor = Color.White; // Light text
                }

                control.Invalidate(); // Ensure control is repainted
            }


            // Recursively apply the theme to all child controls, excluding specific types
            foreach (Control childControl in control.Controls)
            {
                if (!(childControl is MetroTile))
                {
                    ApplyTheme(childControl, isDarkMode);
                }
            }

            // Special handling for specific control types (if needed)
            if (control is DataGridView grid)
            {
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = isDarkMode ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;
                grid.RowHeadersDefaultCellStyle.BackColor = isDarkMode ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
                grid.RowHeadersDefaultCellStyle.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;

                // Apply theme to each row and cell
                foreach (DataGridViewRow row in grid.Rows)
                {
                    row.DefaultCellStyle.BackColor = isDarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Window;
                    row.DefaultCellStyle.ForeColor = isDarkMode ? Color.White : SystemColors.ControlText;
                }
                grid.Refresh(); // Ensure DataGridView is fully repainted
                control.Invalidate();
            }


            if (control is RichTextBox richTextBox)
            {
                richTextBox.BackColor = isDarkMode ? Color.FromArgb(45, 45, 48) : originalColors[control].BackColor;

                int originalStart = richTextBox.SelectionStart;
                int originalLength = richTextBox.SelectionLength;

                // Iterate through each line in the RichTextBox
                int currentCharIndex = 0;
                foreach (var line in richTextBox.Lines)
                {
                    richTextBox.Select(currentCharIndex, line.Length);
                    Color selectionColor = richTextBox.SelectionColor;
                    Debug.WriteLine($"Color {selectionColor.Name} ({line})");

                    // Only change colors that are truly black or near-black
                    if (selectionColor.ToArgb().Equals(Color.Black.ToArgb()))
                    {
                        richTextBox.SelectionColor = Color.White;
                    }

                    if (selectionColor.ToArgb().Equals(Color.White.ToArgb()))
                    {
                        richTextBox.SelectionColor = Color.Black;
                    }

                    currentCharIndex += line.Length + 1; // +1 for the newline character
                }

                richTextBox.Select(originalStart, originalLength); // Restore original selection
                richTextBox.Invalidate(); // Ensure RichTextBox is repainted
            }
        }
    }
}
