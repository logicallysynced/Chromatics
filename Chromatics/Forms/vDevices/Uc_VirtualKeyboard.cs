using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Localization;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Chromatics.Forms
{
    public partial class Uc_VirtualKeyboard : VirtualDevice
    {
        IRGBDevice _device;

        public Uc_VirtualKeyboard(IRGBDevice deviceId)
        {
            _device = deviceId;

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

            //Assign a keycap per cell
            var settings = AppSettings.GetSettings();

            var keycaps = KeyLocalization.GetLocalizedKeys(settings.keyboardLayout);
            tlp_main.Controls.Clear();
            tlp_main.RowCount = 0;
            tlp_main.ColumnCount = 0;
            tlp_main.Padding = new Padding(0);
            tlp_main.Margin = new Padding(0);

            TableLayoutPanel currentTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            currentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            currentTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            if (keycaps != null)
            {
                foreach (var key in keycaps)
                {
                    var width = Convert.ToInt16(key.width) + 5;
                    var height = Convert.ToInt16(key.height) + 5;

                    if (height >= _height)
                    {
                        height = _height;
                    }

                    var keycap = new KeyButton
                    {
                        KeyName = key.visualName,
                        KeyType = key.LedType,
                        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                        FlatStyle = FlatStyle.Flat,
                        Dock = DockStyle.Fill,
                        Text = key.visualName,
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

                    if (key.margin_left > 7)
                    {
                        var margin_width = Convert.ToInt16(key.margin_left);

                        var panel = new Panel
                        {
                            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                            Dock = DockStyle.Fill,
                            Padding = new Padding(0),
                            Margin = new Padding(0),
                            Height = (_height - 5),
                            Width = margin_width,
                            MaximumSize = new System.Drawing.Size(margin_width, (_height - 5)),
                            AutoSize = false,
                            AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        };

                        currentTable.Controls.Add(panel, currentTable.ColumnCount, tlp_main.RowCount);
                        currentTable.ColumnCount++;
                    }

                    currentTable.Controls.Add(keycap, currentTable.ColumnCount, tlp_main.RowCount);
                    currentTable.ColumnCount++;

                    if (key.line_break == true)
                    {
                        tlp_main.Controls.Add(currentTable, 0, tlp_main.RowCount);
                        tlp_main.RowCount++;

                        currentTable = new TableLayoutPanel
                        {
                            Dock = DockStyle.Top,
                            AutoSize = false,
                            Padding = new Padding(0),
                            Margin = new Padding(0)

                        };

                        currentTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                        currentTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    }

                    _keybuttons.Add(keycap);
                }
            }


            tlp_main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp_main.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            init = true;

        }
    }
}