using Chromatics.Core;
using Chromatics.Enums;
using MetroFramework.Components;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.Forms
{
    public partial class Uc_Effects : UserControl
    {
        private MetroToolTip tt_mappings;

        public Uc_Effects()
        {
            InitializeComponent();


        }

        private void OnLoad(object sender, EventArgs e)
        {
            //Create tooltop manager
            tt_mappings = new MetroToolTip
            {
                ToolTipIcon = ToolTipIcon.Info,
                IsBalloon = true,
                ShowAlways = true
            };

            tt_mappings.SetToolTip(mt_effect_dfbell, LocalizationManager.GetLocalizedText("Flash devices when Duty Finder pops."));
            tt_mappings.SetToolTip(mt_effect_damageflash, LocalizationManager.GetLocalizedText("Flash devices when damage is taken."));
            tt_mappings.SetToolTip(mt_effect_startupanimation, LocalizationManager.GetLocalizedText("Displays rainbow animation on all devices when Chromatics starts but not connected to FFXIV."));
            tt_mappings.SetToolTip(mt_effect_reactiveweather, LocalizationManager.GetLocalizedText("Animated base layers for Reactive Weather."));
            tt_mappings.SetToolTip(mt_effect_titlescreen, LocalizationManager.GetLocalizedText("Animation on devices when on title and character selection screen."));
            tt_mappings.SetToolTip(mt_effect_cutscenes, LocalizationManager.GetLocalizedText("Animation on devices when cutscenes play."));
            tt_mappings.SetToolTip(mt_effect_vegasmode, LocalizationManager.GetLocalizedText("Color cycle devices when in the Gold Saucer."));
            tt_mappings.SetToolTip(mt_effect_raideffects, LocalizationManager.GetLocalizedText("Animated raid zone effects instead of reactive weather. Uses Reactive Weather layers."));

            //Load Effect Settings
            if (RGBController.LoadEffectsSettings())
            {
                Logger.WriteConsole(LoggerTypes.System, $"Loaded effects from effects.chromatics3");

            }
            else
            {
                Logger.WriteConsole(LoggerTypes.System, @"No effects file found. Creating default effects..");
                RGBController.SaveEffectsSettings();
            }

            var effects = RGBController.GetEffectsSettings();
            foreach (var control in tlp_main.Controls)
            {
                if (control.GetType() != typeof(MetroTile)) continue;
                var tile = (MetroTile)control;

                foreach (var p in effects.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (p.Name == tile.Name.Replace("mt_", ""))
                    {
                        var mapping = (bool)p.GetValue(effects);

                        if (mapping)
                        {
                            tile.BackColor = Color.DeepSkyBlue;
                        }
                        else
                        {
                            tile.BackColor = Color.DarkRed;
                        }

                    }

                }
            }
        }

        private void OnResize(object sender, EventArgs e)
        {
            // Check if the form is minimized
            if (Fm_MainWindow.GetForm().WindowState == FormWindowState.Minimized)
            {
                return;
            }

            foreach (var control in tlp_main.Controls)
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

        private void OnClick(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(MetroTile)) return;
            var tile = (MetroTile)sender;

            var effects = RGBController.GetEffectsSettings();

            foreach (var p in effects.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.Name == tile.Name.Replace("mt_", ""))
                {
                    var mapping = (bool)p.GetValue(effects);

                    if (mapping)
                    {
                        p.SetValue(effects, false);
                        tile.BackColor = Color.DarkRed;
                    }
                    else
                    {
                        p.SetValue(effects, true);
                        tile.BackColor = Color.DeepSkyBlue;
                    }
                                        
                }

            }

            RGBController.SaveEffectsSettings();
        }

        private static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image), "Image cannot be null.");
            }

            if (image.Width <= 0 || image.Height <= 0)
            {
                throw new ArgumentException("Image dimensions must be greater than zero.");
            }

            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            if (newWidth <= 0 || newHeight <= 0)
            {
                throw new ArgumentException("Scaled image dimensions must be greater than zero.");
            }

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }

    }
}
