using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Localization;
using Chromatics.Models;
using HidSharp;
using OpenRGB.NET;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Chromatics.Forms
{
    public partial class Uc_VirtualOtherController : VirtualDevice
    {
        public Uc_VirtualOtherController()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            if (!init)
            {
                InitializeDevice();
            }
        }

        public override void InitializeDevice()
        {

            //Get Keycap image
            var keycap_img = Properties.Resources.keycap_backglow;

            //Assign a keycap per cell based on selected device
            var selectedDevice = Uc_Mappings.GetActiveDevice();
            if (selectedDevice == null || selectedDevice.Value is not Guid selectedDeviceId)
            {
                return;
            }


            var device = RGBController.GetLiveDevices().FirstOrDefault(d => d.Key == selectedDeviceId).Value;
            if (device == null)
            {
                return;
            }

            if (device.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
            {
                return;
            }

            _deviceType = device.DeviceInfo.DeviceType;

            //var keycaps = Helpers.LedKeyHelper.GetAllKeysForDevice(device.DeviceInfo.DeviceType);

            var base_i = 0;
            var keycaps = new Dictionary<int, LedId>();

            foreach (var led in device)
            {
                if (!keycaps.ContainsKey(base_i))
                {
                    keycaps.Add(base_i, led.Id);
                }

                base_i++;
            }

            tlp_main.Controls.Clear();
            tlp_main.RowCount = 0;
            tlp_main.ColumnCount = 0;
            tlp_main.Padding = new Padding(2);
            tlp_main.Margin = new Padding(2);

            var columnlimit = 20;
            var currentrow = 0;
            var i = 0;

            if (keycaps != null)
            {
                foreach (var key in keycaps.Take(device.Count()))
                {
                    var width = _width;
                    var height = _height;

                    var key_text = key.Value.ToString();

                    string[] unwantedStrings = new string[]
                    {
                        "Unknown", "Cooler", "DRAM", "Fan", "GraphicsCard", "Headset",
                        "HeadsetStand", "Keypad", "Custom", "LedMatrix", "LedStripe",
                        "Mainboard", "Monitor", "Mouse", "Mousepad", "pad", "Speaker"
                    };

                    key_text = unwantedStrings.Aggregate(key_text, (current, unwanted) => current.Replace(unwanted, ""));


                    var keycap = new KeyButton
                    {
                        KeyName = key.Value.ToString(),
                        KeyType = key.Value,
                        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                        FlatStyle = FlatStyle.Flat,
                        Dock = DockStyle.Fill,
                        Text = key_text,
                        Padding = new Padding(2),
                        Margin = new Padding(2),
                        Width = width,
                        Height = height,
                        MaximumSize = new System.Drawing.Size(width, height),
                        AutoSize = false,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        Cursor = Cursors.Hand,
                        UseCompatibleTextRendering = true
                    };

                    keycap.FlatAppearance.BorderSize = 0;
                    keycap.Font = new Font(keycap.Font.FontFamily, keycap.Size.Height / 8, FontStyle.Bold);
                    keycap.ForeColor = System.Drawing.Color.White;
                    keycap.BackColor = System.Drawing.Color.DarkGray;

                    keycap.Click += new EventHandler(OnKeycapPressed);

                    //Draw Border over button
                    keycap.Invalidate();

                    //Cycle Through
                    _keybuttons.Add(keycap);

                    tlp_main.Controls.Add(keycap, i, currentrow);
                    tlp_main.ColumnCount++;

                    if (i == columnlimit)
                    {
                        tlp_main.RowCount++;
                        currentrow++;
                        i = 0;
                    }
                    else
                    {
                        i++;
                    }
                }
            }


            tlp_main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp_main.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            init = true;
        }
    }
}
