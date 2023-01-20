using Chromatics.Enums;
using Chromatics.Localization;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Chromatics.Forms
{
    public partial class Uc_VirtualMouse : VirtualDevice
    {
        public Uc_VirtualMouse()
        {
            InitializeComponent();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            //Get Keycap image
            var keycap_img = Properties.Resources.keycap_backglow;


            //Assign a keycap per cell
            var keycaps = Helpers.LedKeyHelper.GetAllKeysForDevice(RGBDeviceType.Mouse);
            tlp_main.Controls.Clear();
            tlp_main.RowCount = 0;
            tlp_main.ColumnCount = 0;
            tlp_main.Padding = new Padding(0);
            tlp_main.Margin = new Padding(0);

            var columnlimit = 20;
            var currentrow = 0;
            var i = 0;

            foreach (var key in keycaps)
            {
                var width = _width;
                var height = _height;

                var keycap = new KeyButton
                {
                    KeyName = key.Value.ToString(),
                    KeyType = key.Value,
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    FlatStyle = FlatStyle.Flat,
                    Dock = DockStyle.Fill,
                    Text = key.Value.ToString().Replace("Mouse", ""),
                    Padding = new Padding(0),
                    Margin = new Padding(0),
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

            tlp_main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp_main.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            init = true;
        }

    }
}
