using Chromatics.Core;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Properties;
using MetroFramework.Components;
using MetroFramework.Controls;
using MetroFramework.Forms;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.CoolerMaster;
using RGB.NET.Devices.Corsair;
using RGB.NET.Devices.Logitech;
using RGB.NET.Devices.Msi;
using RGB.NET.Devices.Novation;
using RGB.NET.Devices.OpenRGB;
using RGB.NET.Devices.Razer;
using RGB.NET.Devices.SteelSeries;
using RGB.NET.Devices.Wooting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.Forms
{
    public partial class Fm_FirstRun : MetroForm
    {
        private MetroStyleManager metroStyleManager;
        private MetroToolTip tt_mappings;
        private readonly Color tilecol_enabled = Color.DeepSkyBlue;
        private readonly Color tilecol_disabled = Color.DarkGray;
        private int selected = 0;

        public Fm_FirstRun()
        {
            //Correct for DPI settings
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font(Font.Name, 8.25f * 100f / CreateGraphics().DpiY, Font.Style, Font.Unit, Font.GdiCharSet, Font.GdiVerticalFont);


            InitializeComponent();

            metroStyleManager = new MetroStyleManager(); 
            metroStyleManager.Owner = this;
            metroStyleManager.Theme = MetroFramework.MetroThemeStyle.Default;
            metroStyleManager.Style = MetroFramework.MetroColorStyle.Pink;

            this.Theme = metroStyleManager.Theme;
            this.Style = metroStyleManager.Style;

            foreach (var control in tlp_devices.Controls)
            {
                if (control.GetType() != typeof(MetroTile)) continue;
                var tile = (MetroTile)control;

                var previmg = tile.TileImage;
                var newimg = ScaleImage(previmg, tile.Width / 2, tile.Height / 2);

                tile.TileImage = newimg;
                tile.TileImageAlign = ContentAlignment.MiddleCenter;
                tile.Invalidate();
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            //Startup
            var settings = AppSettings.GetSettings();
            settings.deviceRazerEnabled = false;
            settings.deviceLogitechEnabled = false;
            settings.deviceCorsairEnabled = false;
            settings.deviceCoolermasterEnabled = false;
            settings.deviceSteelseriesEnabled = false;
            settings.deviceAsusEnabled = false;
            settings.deviceMsiEnabled = false;
            settings.deviceWootingEnabled = false;
            settings.deviceNovationEnabled = false;
            settings.deviceOpenRGBEnabled = false;
            settings.deviceHueEnabled = false;

            //Add tooltips
            tt_mappings = new MetroToolTip();

            mt_settings_razer.BackColor = settings.deviceRazerEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_logitech.BackColor = settings.deviceLogitechEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_corsair.BackColor = settings.deviceCorsairEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_coolermaster.BackColor = settings.deviceCoolermasterEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_steelseries.BackColor = settings.deviceSteelseriesEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_asus.BackColor = settings.deviceAsusEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_msi.BackColor = settings.deviceMsiEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_wooting.BackColor = settings.deviceWootingEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_novation.BackColor = settings.deviceNovationEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_openrgb.BackColor = settings.deviceOpenRGBEnabled ? tilecol_enabled : tilecol_disabled;
            mt_settings_hue.BackColor = settings.deviceHueEnabled ? tilecol_enabled : tilecol_disabled;           

            mt_settings_razer.Click += mt_settings_razer_Click;
            mt_settings_logitech.Click += mt_settings_logitech_Click;
            mt_settings_corsair.Click +=  mt_settings_corsair_Click;
            mt_settings_coolermaster.Click += mt_settings_coolermaster_Click;
            mt_settings_steelseries.Click += mt_settings_steelseries_Click;
            mt_settings_asus.Click += mt_settings_asus_Click;
            mt_settings_msi.Click += mt_settings_msi_Click;
            mt_settings_wooting.Click += mt_settings_wooting_Click;
            mt_settings_novation.Click += mt_settings_novation_Click;
            mt_settings_openrgb.Click += mt_settings_openRGB_Click;
            mt_settings_hue.Click += mt_settings_hue_Click;

            selected = 0;
            ValidateSelections();

            AppSettings.SaveSettings(settings);
        }

        private void ValidateSelections()
        {
            if (selected <= 0)
            {
                btn_save.BackColor = tilecol_disabled;
                //btn_save.Enabled = false;
                btn_save.Click -= btn_save_Click;
                tt_mappings.SetToolTip(btn_save, @"Please select at least one device provider.");
            }
            else if (selected > 0)
            {
                btn_save.BackColor = tilecol_enabled;
                //btn_save.Enabled = true;
                btn_save.Click += btn_save_Click;
                tt_mappings.SetToolTip(btn_save, @"");
            }
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            var settings = AppSettings.GetSettings();
            settings.firstrun = false;

            AppSettings.SaveSettings(settings);

            mt_settings_razer.Click -= mt_settings_razer_Click;
            mt_settings_logitech.Click -= mt_settings_logitech_Click;
            mt_settings_corsair.Click -=  mt_settings_corsair_Click;
            mt_settings_coolermaster.Click -= mt_settings_coolermaster_Click;
            mt_settings_steelseries.Click -= mt_settings_steelseries_Click;
            mt_settings_asus.Click -= mt_settings_asus_Click;
            mt_settings_msi.Click -= mt_settings_msi_Click;
            mt_settings_wooting.Click -= mt_settings_wooting_Click;
            mt_settings_novation.Click -= mt_settings_novation_Click;
            mt_settings_openrgb.Click -= mt_settings_openRGB_Click;
            mt_settings_hue.Click -= mt_settings_hue_Click;
            btn_save.Click -= btn_save_Click;

            Close();
            Dispose();
        }

        private void mt_settings_razer_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceRazerEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceRazerEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_logitech_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceLogitechEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceLogitechEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_corsair_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceCorsairEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceCorsairEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_steelseries_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceSteelseriesEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceSteelseriesEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_coolermaster_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceCoolermasterEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceCoolermasterEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_asus_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceAsusEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceAsusEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_wooting_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceWootingEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceWootingEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_msi_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceMsiEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceMsiEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_novation_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceNovationEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceNovationEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_hue_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceHueEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceHueEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void mt_settings_openRGB_Click(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var settings = AppSettings.GetSettings();
            var device = settings.deviceOpenRGBEnabled;

            if (device)
            {
                device = false;
                tile.BackColor = tilecol_disabled;
                selected--;
            }
            else
            {
                device = true;
                tile.BackColor = tilecol_enabled;
                selected++;
            }

            settings.deviceOpenRGBEnabled = device;
            AppSettings.SaveSettings(settings);
            ValidateSelections();
        }

        private void OnResize(object sender, EventArgs e)
        {
            foreach (var control in tlp_devices.Controls)
            {
                if (control.GetType() != typeof(MetroTile)) continue;
                var tile = (MetroTile)control;

                var previmg = tile.TileImage;
                var newimg = ScaleImage(previmg, tile.Width / 2, tile.Height / 2);

                tile.TileImage = newimg;
                tile.TileImageAlign = ContentAlignment.MiddleCenter;
                tile.Invalidate();
            }
        }

        private static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }
    }
}
