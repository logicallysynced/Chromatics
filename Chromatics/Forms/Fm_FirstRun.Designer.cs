namespace Chromatics.Forms
{
    partial class Fm_FirstRun
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tlp_welcome1 = new System.Windows.Forms.TableLayoutPanel();
            this.rtb_welcome = new System.Windows.Forms.RichTextBox();
            this.tlp_devices = new System.Windows.Forms.TableLayoutPanel();
            this.mt_settings_wooting = new MetroFramework.Controls.MetroTile();
            this.mt_settings_asus = new MetroFramework.Controls.MetroTile();
            this.mt_settings_coolermaster = new MetroFramework.Controls.MetroTile();
            this.mt_settings_steelseries = new MetroFramework.Controls.MetroTile();
            this.mt_settings_corsair = new MetroFramework.Controls.MetroTile();
            this.mt_settings_razer = new MetroFramework.Controls.MetroTile();
            this.mt_settings_logitech = new MetroFramework.Controls.MetroTile();
            this.mt_settings_msi = new MetroFramework.Controls.MetroTile();
            this.mt_settings_novation = new MetroFramework.Controls.MetroTile();
            this.mt_settings_hue = new MetroFramework.Controls.MetroTile();
            this.mt_settings_openrgb = new MetroFramework.Controls.MetroTile();
            this.tlp_split = new System.Windows.Forms.TableLayoutPanel();
            this.btn_save = new MetroFramework.Controls.MetroTile();
            this.tlp_welcome1.SuspendLayout();
            this.tlp_devices.SuspendLayout();
            this.tlp_split.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlp_welcome1
            // 
            this.tlp_welcome1.ColumnCount = 1;
            this.tlp_welcome1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlp_welcome1.Controls.Add(this.rtb_welcome, 0, 0);
            this.tlp_welcome1.Controls.Add(this.tlp_devices, 0, 1);
            this.tlp_welcome1.Controls.Add(this.tlp_split, 0, 2);
            this.tlp_welcome1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_welcome1.Location = new System.Drawing.Point(20, 60);
            this.tlp_welcome1.Name = "tlp_welcome1";
            this.tlp_welcome1.RowCount = 3;
            this.tlp_welcome1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.46497F));
            this.tlp_welcome1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 88.53503F));
            this.tlp_welcome1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.tlp_welcome1.Size = new System.Drawing.Size(895, 628);
            this.tlp_welcome1.TabIndex = 0;
            // 
            // rtb_welcome
            // 
            this.rtb_welcome.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtb_welcome.BackColor = System.Drawing.Color.White;
            this.rtb_welcome.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtb_welcome.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.rtb_welcome.Location = new System.Drawing.Point(3, 3);
            this.rtb_welcome.Name = "rtb_welcome";
            this.rtb_welcome.ReadOnly = true;
            this.rtb_welcome.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtb_welcome.Size = new System.Drawing.Size(889, 58);
            this.rtb_welcome.TabIndex = 8;
            this.rtb_welcome.Text = "To get started, select the device providers you wish to use.\nYou can change these" +
    " at any time from the Settings menu.";
            // 
            // tlp_devices
            // 
            this.tlp_devices.ColumnCount = 4;
            this.tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.Controls.Add(this.mt_settings_wooting, 1, 1);
            this.tlp_devices.Controls.Add(this.mt_settings_asus, 0, 1);
            this.tlp_devices.Controls.Add(this.mt_settings_coolermaster, 4, 0);
            this.tlp_devices.Controls.Add(this.mt_settings_steelseries, 3, 0);
            this.tlp_devices.Controls.Add(this.mt_settings_corsair, 2, 0);
            this.tlp_devices.Controls.Add(this.mt_settings_razer, 0, 0);
            this.tlp_devices.Controls.Add(this.mt_settings_logitech, 1, 0);
            this.tlp_devices.Controls.Add(this.mt_settings_msi, 3, 1);
            this.tlp_devices.Controls.Add(this.mt_settings_novation, 0, 2);
            this.tlp_devices.Controls.Add(this.mt_settings_hue, 2, 2);
            this.tlp_devices.Controls.Add(this.mt_settings_openrgb, 1, 2);
            this.tlp_devices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_devices.Location = new System.Drawing.Point(3, 67);
            this.tlp_devices.Name = "tlp_devices";
            this.tlp_devices.RowCount = 5;
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.Size = new System.Drawing.Size(889, 492);
            this.tlp_devices.TabIndex = 2;
            this.tlp_devices.Resize += new System.EventHandler(this.OnResize);
            // 
            // mt_settings_wooting
            // 
            this.mt_settings_wooting.ActiveControl = null;
            this.mt_settings_wooting.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_wooting.AutoSize = true;
            this.mt_settings_wooting.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_wooting.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_wooting.Location = new System.Drawing.Point(449, 103);
            this.mt_settings_wooting.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_wooting.Name = "mt_settings_wooting";
            this.mt_settings_wooting.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_wooting.TabIndex = 2;
            this.mt_settings_wooting.Text = "Wooting";
            this.mt_settings_wooting.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_wooting.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_wooting.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_wooting.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_wooting.UseCustomBackColor = true;
            this.mt_settings_wooting.UseSelectable = true;
            this.mt_settings_wooting.UseTileImage = true;
            this.mt_settings_wooting.UseVisualStyleBackColor = false;
            // 
            // mt_settings_asus
            // 
            this.mt_settings_asus.ActiveControl = null;
            this.mt_settings_asus.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_asus.AutoSize = true;
            this.mt_settings_asus.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_asus.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_asus.Location = new System.Drawing.Point(227, 103);
            this.mt_settings_asus.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_asus.Name = "mt_settings_asus";
            this.mt_settings_asus.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_asus.TabIndex = 2;
            this.mt_settings_asus.Text = "ASUS";
            this.mt_settings_asus.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_asus.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_asus.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_asus.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_asus.UseCustomBackColor = true;
            this.mt_settings_asus.UseSelectable = true;
            this.mt_settings_asus.UseTileImage = true;
            this.mt_settings_asus.UseVisualStyleBackColor = false;
            // 
            // mt_settings_coolermaster
            // 
            this.mt_settings_coolermaster.ActiveControl = null;
            this.mt_settings_coolermaster.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_coolermaster.AutoSize = true;
            this.mt_settings_coolermaster.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_coolermaster.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_coolermaster.Location = new System.Drawing.Point(5, 103);
            this.mt_settings_coolermaster.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_coolermaster.Name = "mt_settings_coolermaster";
            this.mt_settings_coolermaster.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_coolermaster.TabIndex = 2;
            this.mt_settings_coolermaster.Text = "Coolermaster";
            this.mt_settings_coolermaster.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_coolermaster.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_coolermaster.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_coolermaster.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_coolermaster.UseCustomBackColor = true;
            this.mt_settings_coolermaster.UseSelectable = true;
            this.mt_settings_coolermaster.UseTileImage = true;
            this.mt_settings_coolermaster.UseVisualStyleBackColor = false;
            // 
            // mt_settings_steelseries
            // 
            this.mt_settings_steelseries.ActiveControl = null;
            this.mt_settings_steelseries.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_steelseries.AutoSize = true;
            this.mt_settings_steelseries.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_steelseries.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_steelseries.Location = new System.Drawing.Point(671, 5);
            this.mt_settings_steelseries.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_steelseries.Name = "mt_settings_steelseries";
            this.mt_settings_steelseries.Size = new System.Drawing.Size(213, 88);
            this.mt_settings_steelseries.TabIndex = 1;
            this.mt_settings_steelseries.Text = "SteelSeries";
            this.mt_settings_steelseries.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_steelseries.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_steelseries.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_steelseries.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_steelseries.UseCustomBackColor = true;
            this.mt_settings_steelseries.UseSelectable = true;
            this.mt_settings_steelseries.UseTileImage = true;
            this.mt_settings_steelseries.UseVisualStyleBackColor = false;
            // 
            // mt_settings_corsair
            // 
            this.mt_settings_corsair.ActiveControl = null;
            this.mt_settings_corsair.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_corsair.AutoSize = true;
            this.mt_settings_corsair.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_corsair.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_corsair.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.mt_settings_corsair.Location = new System.Drawing.Point(449, 5);
            this.mt_settings_corsair.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_corsair.Name = "mt_settings_corsair";
            this.mt_settings_corsair.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_corsair.TabIndex = 0;
            this.mt_settings_corsair.Text = "Corsair";
            this.mt_settings_corsair.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_corsair.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_corsair.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_corsair.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_corsair.UseCustomBackColor = true;
            this.mt_settings_corsair.UseSelectable = true;
            this.mt_settings_corsair.UseTileImage = true;
            this.mt_settings_corsair.UseVisualStyleBackColor = false;
            // 
            // mt_settings_razer
            // 
            this.mt_settings_razer.ActiveControl = null;
            this.mt_settings_razer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_razer.AutoSize = true;
            this.mt_settings_razer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_razer.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_razer.Location = new System.Drawing.Point(5, 5);
            this.mt_settings_razer.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_razer.Name = "mt_settings_razer";
            this.mt_settings_razer.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_razer.TabIndex = 2;
            this.mt_settings_razer.Text = "Razer";
            this.mt_settings_razer.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_razer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_razer.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_razer.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_razer.UseCustomBackColor = true;
            this.mt_settings_razer.UseSelectable = true;
            this.mt_settings_razer.UseTileImage = true;
            this.mt_settings_razer.UseVisualStyleBackColor = false;
            // 
            // mt_settings_logitech
            // 
            this.mt_settings_logitech.ActiveControl = null;
            this.mt_settings_logitech.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_logitech.AutoSize = true;
            this.mt_settings_logitech.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_logitech.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_logitech.Location = new System.Drawing.Point(227, 5);
            this.mt_settings_logitech.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_logitech.Name = "mt_settings_logitech";
            this.mt_settings_logitech.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_logitech.TabIndex = 2;
            this.mt_settings_logitech.Text = "Logitech";
            this.mt_settings_logitech.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_logitech.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_logitech.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_logitech.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_logitech.UseCustomBackColor = true;
            this.mt_settings_logitech.UseSelectable = true;
            this.mt_settings_logitech.UseTileImage = true;
            this.mt_settings_logitech.UseVisualStyleBackColor = false;
            // 
            // mt_settings_msi
            // 
            this.mt_settings_msi.ActiveControl = null;
            this.mt_settings_msi.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_msi.AutoSize = true;
            this.mt_settings_msi.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_msi.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_msi.Location = new System.Drawing.Point(671, 103);
            this.mt_settings_msi.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_msi.Name = "mt_settings_msi";
            this.mt_settings_msi.Size = new System.Drawing.Size(213, 88);
            this.mt_settings_msi.TabIndex = 3;
            this.mt_settings_msi.Text = "MSI";
            this.mt_settings_msi.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_msi.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_msi.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_msi.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_msi.UseCustomBackColor = true;
            this.mt_settings_msi.UseSelectable = true;
            this.mt_settings_msi.UseTileImage = true;
            this.mt_settings_msi.UseVisualStyleBackColor = false;
            // 
            // mt_settings_novation
            // 
            this.mt_settings_novation.ActiveControl = null;
            this.mt_settings_novation.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_novation.AutoSize = true;
            this.mt_settings_novation.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_novation.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_novation.Location = new System.Drawing.Point(5, 201);
            this.mt_settings_novation.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_novation.Name = "mt_settings_novation";
            this.mt_settings_novation.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_novation.TabIndex = 4;
            this.mt_settings_novation.Text = "Novation";
            this.mt_settings_novation.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_novation.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_novation.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_novation.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_novation.UseCustomBackColor = true;
            this.mt_settings_novation.UseSelectable = true;
            this.mt_settings_novation.UseTileImage = true;
            this.mt_settings_novation.UseVisualStyleBackColor = false;
            // 
            // mt_settings_hue
            // 
            this.mt_settings_hue.ActiveControl = null;
            this.mt_settings_hue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_hue.AutoSize = true;
            this.mt_settings_hue.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_hue.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_hue.Enabled = false;
            this.mt_settings_hue.Location = new System.Drawing.Point(449, 201);
            this.mt_settings_hue.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_hue.Name = "mt_settings_hue";
            this.mt_settings_hue.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_hue.TabIndex = 5;
            this.mt_settings_hue.Text = "Philips Hue";
            this.mt_settings_hue.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_hue.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_hue.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_hue.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_hue.UseCustomBackColor = true;
            this.mt_settings_hue.UseSelectable = true;
            this.mt_settings_hue.UseTileImage = true;
            this.mt_settings_hue.UseVisualStyleBackColor = false;
            this.mt_settings_hue.Visible = false;
            // 
            // mt_settings_openrgb
            // 
            this.mt_settings_openrgb.ActiveControl = null;
            this.mt_settings_openrgb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mt_settings_openrgb.AutoSize = true;
            this.mt_settings_openrgb.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.mt_settings_openrgb.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.mt_settings_openrgb.Location = new System.Drawing.Point(227, 201);
            this.mt_settings_openrgb.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_openrgb.Name = "mt_settings_openrgb";
            this.mt_settings_openrgb.Size = new System.Drawing.Size(212, 88);
            this.mt_settings_openrgb.TabIndex = 6;
            this.mt_settings_openrgb.Text = "OpenRGB";
            this.mt_settings_openrgb.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.mt_settings_openrgb.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.mt_settings_openrgb.TileImage = global::Chromatics.Properties.Resources.keyboard;
            this.mt_settings_openrgb.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.mt_settings_openrgb.UseCustomBackColor = true;
            this.mt_settings_openrgb.UseSelectable = true;
            this.mt_settings_openrgb.UseTileImage = true;
            this.mt_settings_openrgb.UseVisualStyleBackColor = false;
            // 
            // tlp_split
            // 
            this.tlp_split.ColumnCount = 2;
            this.tlp_split.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 74.80315F));
            this.tlp_split.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.19685F));
            this.tlp_split.Controls.Add(this.btn_save, 1, 0);
            this.tlp_split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_split.Location = new System.Drawing.Point(3, 565);
            this.tlp_split.Name = "tlp_split";
            this.tlp_split.RowCount = 1;
            this.tlp_split.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlp_split.Size = new System.Drawing.Size(889, 60);
            this.tlp_split.TabIndex = 9;
            // 
            // btn_save
            // 
            this.btn_save.ActiveControl = null;
            this.btn_save.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_save.AutoSize = true;
            this.btn_save.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btn_save.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.btn_save.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btn_save.Location = new System.Drawing.Point(670, 5);
            this.btn_save.Margin = new System.Windows.Forms.Padding(5);
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(214, 50);
            this.btn_save.TabIndex = 6;
            this.btn_save.Text = "Continue";
            this.btn_save.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btn_save.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btn_save.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btn_save.UseCustomBackColor = true;
            this.btn_save.UseSelectable = true;
            this.btn_save.UseVisualStyleBackColor = false;
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            // 
            // Fm_FirstRun
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(935, 708);
            this.ControlBox = false;
            this.Controls.Add(this.tlp_welcome1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Fm_FirstRun";
            this.Resizable = false;
            this.ShowInTaskbar = false;
            this.Style = MetroFramework.MetroColorStyle.Pink;
            this.Text = "Welcome to Chromatics";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.OnLoad);
            this.tlp_welcome1.ResumeLayout(false);
            this.tlp_devices.ResumeLayout(false);
            this.tlp_devices.PerformLayout();
            this.tlp_split.ResumeLayout(false);
            this.tlp_split.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel tlp_welcome1;
        private System.Windows.Forms.TableLayoutPanel tlp_devices;
        private MetroFramework.Controls.MetroTile mt_settings_wooting;
        private MetroFramework.Controls.MetroTile mt_settings_asus;
        private MetroFramework.Controls.MetroTile mt_settings_coolermaster;
        private MetroFramework.Controls.MetroTile mt_settings_steelseries;
        private MetroFramework.Controls.MetroTile mt_settings_corsair;
        private MetroFramework.Controls.MetroTile mt_settings_razer;
        private MetroFramework.Controls.MetroTile mt_settings_logitech;
        private MetroFramework.Controls.MetroTile mt_settings_msi;
        private MetroFramework.Controls.MetroTile mt_settings_novation;
        private MetroFramework.Controls.MetroTile mt_settings_hue;
        private MetroFramework.Controls.MetroTile mt_settings_openrgb;
        private System.Windows.Forms.RichTextBox rtb_welcome;
        private System.Windows.Forms.TableLayoutPanel tlp_split;
        private MetroFramework.Controls.MetroTile btn_save;
    }
}