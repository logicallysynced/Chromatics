namespace Chromatics.Forms
{
    partial class Uc_Settings
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tlp_main = new System.Windows.Forms.TableLayoutPanel();
            this.tlp_outer = new System.Windows.Forms.TableLayoutPanel();
            this.gb_other = new System.Windows.Forms.GroupBox();
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
            this.gb_devicesettings = new System.Windows.Forms.GroupBox();
            this.lbl_devicebrightpercent = new MetroFramework.Controls.MetroLabel();
            this.lbl_devicebright = new MetroFramework.Controls.MetroLabel();
            this.trackbar_lighting = new MetroFramework.Controls.MetroTrackBar();
            this.gb_general = new System.Windows.Forms.GroupBox();
            this.chk_updatecheck = new System.Windows.Forms.CheckBox();
            this.btn_clearcache = new MetroFramework.Controls.MetroButton();
            this.btn_resetchromatics = new MetroFramework.Controls.MetroButton();
            this.chk_trayonstartup = new System.Windows.Forms.CheckBox();
            this.chk_minimizetray = new System.Windows.Forms.CheckBox();
            this.chk_winstart = new System.Windows.Forms.CheckBox();
            this.chk_localcache = new System.Windows.Forms.CheckBox();
            this.tlp_main.SuspendLayout();
            this.tlp_outer.SuspendLayout();
            this.gb_other.SuspendLayout();
            this.tlp_devices.SuspendLayout();
            this.gb_devicesettings.SuspendLayout();
            this.gb_general.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlp_main
            // 
            this.tlp_main.ColumnCount = 2;
            this.tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45.83741F));
            this.tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54.16259F));
            this.tlp_main.Controls.Add(this.tlp_outer, 1, 0);
            this.tlp_main.Controls.Add(this.gb_general, 0, 0);
            this.tlp_main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_main.Location = new System.Drawing.Point(0, 0);
            this.tlp_main.Name = "tlp_main";
            this.tlp_main.RowCount = 1;
            this.tlp_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_main.Size = new System.Drawing.Size(1021, 629);
            this.tlp_main.TabIndex = 0;
            // 
            // tlp_outer
            // 
            this.tlp_outer.ColumnCount = 1;
            this.tlp_outer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_outer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_outer.Controls.Add(this.gb_other, 0, 0);
            this.tlp_outer.Controls.Add(this.gb_devicesettings, 0, 1);
            this.tlp_outer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_outer.Location = new System.Drawing.Point(470, 3);
            this.tlp_outer.Name = "tlp_outer";
            this.tlp_outer.RowCount = 2;
            this.tlp_outer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 81.70145F));
            this.tlp_outer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18.29856F));
            this.tlp_outer.Size = new System.Drawing.Size(548, 623);
            this.tlp_outer.TabIndex = 0;
            // 
            // gb_other
            // 
            this.gb_other.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gb_other.Controls.Add(this.tlp_devices);
            this.gb_other.Location = new System.Drawing.Point(3, 3);
            this.gb_other.Name = "gb_other";
            this.gb_other.Size = new System.Drawing.Size(542, 502);
            this.gb_other.TabIndex = 0;
            this.gb_other.TabStop = false;
            this.gb_other.Text = "Devices";
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
            this.tlp_devices.Location = new System.Drawing.Point(3, 23);
            this.tlp_devices.Name = "tlp_devices";
            this.tlp_devices.RowCount = 5;
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tlp_devices.Size = new System.Drawing.Size(536, 476);
            this.tlp_devices.TabIndex = 1;
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
            this.mt_settings_wooting.Location = new System.Drawing.Point(273, 100);
            this.mt_settings_wooting.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_wooting.Name = "mt_settings_wooting";
            this.mt_settings_wooting.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_wooting.Click += new System.EventHandler(this.mt_settings_wooting_Click);
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
            this.mt_settings_asus.Location = new System.Drawing.Point(139, 100);
            this.mt_settings_asus.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_asus.Name = "mt_settings_asus";
            this.mt_settings_asus.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_asus.Click += new System.EventHandler(this.mt_settings_asus_Click);
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
            this.mt_settings_coolermaster.Location = new System.Drawing.Point(5, 100);
            this.mt_settings_coolermaster.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_coolermaster.Name = "mt_settings_coolermaster";
            this.mt_settings_coolermaster.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_coolermaster.Click += new System.EventHandler(this.mt_settings_coolermaster_Click);
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
            this.mt_settings_steelseries.Location = new System.Drawing.Point(407, 5);
            this.mt_settings_steelseries.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_steelseries.Name = "mt_settings_steelseries";
            this.mt_settings_steelseries.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_steelseries.Click += new System.EventHandler(this.mt_settings_steelseries_Click);
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
            this.mt_settings_corsair.Location = new System.Drawing.Point(273, 5);
            this.mt_settings_corsair.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_corsair.Name = "mt_settings_corsair";
            this.mt_settings_corsair.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_corsair.Click += new System.EventHandler(this.mt_settings_corsair_Click);
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
            this.mt_settings_razer.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_razer.Click += new System.EventHandler(this.mt_settings_razer_Click);
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
            this.mt_settings_logitech.Location = new System.Drawing.Point(139, 5);
            this.mt_settings_logitech.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_logitech.Name = "mt_settings_logitech";
            this.mt_settings_logitech.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_logitech.Click += new System.EventHandler(this.mt_settings_logitech_Click);
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
            this.mt_settings_msi.Location = new System.Drawing.Point(407, 100);
            this.mt_settings_msi.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_msi.Name = "mt_settings_msi";
            this.mt_settings_msi.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_msi.Click += new System.EventHandler(this.mt_settings_msi_Click);
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
            this.mt_settings_novation.Location = new System.Drawing.Point(5, 195);
            this.mt_settings_novation.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_novation.Name = "mt_settings_novation";
            this.mt_settings_novation.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_novation.Click += new System.EventHandler(this.mt_settings_novation_Click);
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
            this.mt_settings_hue.Location = new System.Drawing.Point(273, 195);
            this.mt_settings_hue.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_hue.Name = "mt_settings_hue";
            this.mt_settings_hue.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_hue.Click += new System.EventHandler(this.mt_settings_hue_Click);
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
            this.mt_settings_openrgb.Enabled = false;
            this.mt_settings_openrgb.Location = new System.Drawing.Point(139, 195);
            this.mt_settings_openrgb.Margin = new System.Windows.Forms.Padding(5);
            this.mt_settings_openrgb.Name = "mt_settings_openrgb";
            this.mt_settings_openrgb.Size = new System.Drawing.Size(124, 85);
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
            this.mt_settings_openrgb.Click += new System.EventHandler(this.mt_settings_openRGB_Click);
            // 
            // gb_devicesettings
            // 
            this.gb_devicesettings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gb_devicesettings.Controls.Add(this.lbl_devicebrightpercent);
            this.gb_devicesettings.Controls.Add(this.lbl_devicebright);
            this.gb_devicesettings.Controls.Add(this.trackbar_lighting);
            this.gb_devicesettings.Location = new System.Drawing.Point(3, 511);
            this.gb_devicesettings.Name = "gb_devicesettings";
            this.gb_devicesettings.Size = new System.Drawing.Size(542, 109);
            this.gb_devicesettings.TabIndex = 1;
            this.gb_devicesettings.TabStop = false;
            this.gb_devicesettings.Text = "Global Device Controls";
            // 
            // lbl_devicebrightpercent
            // 
            this.lbl_devicebrightpercent.AutoSize = true;
            this.lbl_devicebrightpercent.Location = new System.Drawing.Point(269, 76);
            this.lbl_devicebrightpercent.Name = "lbl_devicebrightpercent";
            this.lbl_devicebrightpercent.Size = new System.Drawing.Size(42, 20);
            this.lbl_devicebrightpercent.TabIndex = 2;
            this.lbl_devicebrightpercent.Text = "100%";
            this.lbl_devicebrightpercent.UseCustomBackColor = true;
            // 
            // lbl_devicebright
            // 
            this.lbl_devicebright.AutoSize = true;
            this.lbl_devicebright.Location = new System.Drawing.Point(15, 43);
            this.lbl_devicebright.Name = "lbl_devicebright";
            this.lbl_devicebright.Size = new System.Drawing.Size(120, 20);
            this.lbl_devicebright.TabIndex = 1;
            this.lbl_devicebright.Text = "Device Brightness";
            this.lbl_devicebright.UseCustomBackColor = true;
            // 
            // trackbar_lighting
            // 
            this.trackbar_lighting.BackColor = System.Drawing.Color.Transparent;
            this.trackbar_lighting.LargeChange = 1;
            this.trackbar_lighting.Location = new System.Drawing.Point(143, 44);
            this.trackbar_lighting.MouseWheelBarPartitions = 5;
            this.trackbar_lighting.Name = "trackbar_lighting";
            this.trackbar_lighting.Size = new System.Drawing.Size(327, 29);
            this.trackbar_lighting.TabIndex = 0;
            this.trackbar_lighting.Text = "Device Brightness";
            this.trackbar_lighting.UseCustomBackColor = true;
            this.trackbar_lighting.Value = 100;
            this.trackbar_lighting.Scroll += new System.Windows.Forms.ScrollEventHandler(this.trackbar_lighting_Scroll);
            // 
            // gb_general
            // 
            this.gb_general.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gb_general.Controls.Add(this.chk_updatecheck);
            this.gb_general.Controls.Add(this.btn_clearcache);
            this.gb_general.Controls.Add(this.btn_resetchromatics);
            this.gb_general.Controls.Add(this.chk_trayonstartup);
            this.gb_general.Controls.Add(this.chk_minimizetray);
            this.gb_general.Controls.Add(this.chk_winstart);
            this.gb_general.Controls.Add(this.chk_localcache);
            this.gb_general.Location = new System.Drawing.Point(3, 3);
            this.gb_general.Name = "gb_general";
            this.gb_general.Size = new System.Drawing.Size(461, 623);
            this.gb_general.TabIndex = 1;
            this.gb_general.TabStop = false;
            this.gb_general.Text = "General";
            // 
            // chk_updatecheck
            // 
            this.chk_updatecheck.AutoSize = true;
            this.chk_updatecheck.Location = new System.Drawing.Point(24, 164);
            this.chk_updatecheck.Name = "chk_updatecheck";
            this.chk_updatecheck.Size = new System.Drawing.Size(152, 24);
            this.chk_updatecheck.TabIndex = 8;
            this.chk_updatecheck.Text = "Check for Updates";
            this.chk_updatecheck.UseVisualStyleBackColor = true;
            this.chk_updatecheck.CheckedChanged += new System.EventHandler(this.chk_updatecheck_CheckedChanged);
            // 
            // btn_clearcache
            // 
            this.btn_clearcache.Location = new System.Drawing.Point(196, 557);
            this.btn_clearcache.Name = "btn_clearcache";
            this.btn_clearcache.Size = new System.Drawing.Size(151, 43);
            this.btn_clearcache.TabIndex = 7;
            this.btn_clearcache.Text = "Clear Cache";
            this.btn_clearcache.UseSelectable = true;
            this.btn_clearcache.Click += new System.EventHandler(this.btn_clearcache_Click);
            // 
            // btn_resetchromatics
            // 
            this.btn_resetchromatics.Location = new System.Drawing.Point(24, 557);
            this.btn_resetchromatics.Name = "btn_resetchromatics";
            this.btn_resetchromatics.Size = new System.Drawing.Size(151, 43);
            this.btn_resetchromatics.TabIndex = 6;
            this.btn_resetchromatics.Text = "Reset Chromatics";
            this.btn_resetchromatics.UseSelectable = true;
            this.btn_resetchromatics.Click += new System.EventHandler(this.btn_resetchromatics_Click);
            // 
            // chk_trayonstartup
            // 
            this.chk_trayonstartup.AutoSize = true;
            this.chk_trayonstartup.Location = new System.Drawing.Point(24, 134);
            this.chk_trayonstartup.Name = "chk_trayonstartup";
            this.chk_trayonstartup.Size = new System.Drawing.Size(213, 24);
            this.chk_trayonstartup.TabIndex = 4;
            this.chk_trayonstartup.Text = "Minimise to Tray on Startup";
            this.chk_trayonstartup.UseVisualStyleBackColor = true;
            this.chk_trayonstartup.CheckedChanged += new System.EventHandler(this.chk_trayonstartup_CheckedChanged);
            // 
            // chk_minimizetray
            // 
            this.chk_minimizetray.AutoSize = true;
            this.chk_minimizetray.Location = new System.Drawing.Point(24, 104);
            this.chk_minimizetray.Name = "chk_minimizetray";
            this.chk_minimizetray.Size = new System.Drawing.Size(140, 24);
            this.chk_minimizetray.TabIndex = 3;
            this.chk_minimizetray.Text = "Minimise to Tray";
            this.chk_minimizetray.UseVisualStyleBackColor = true;
            this.chk_minimizetray.CheckedChanged += new System.EventHandler(this.chk_minimizetray_CheckedChanged);
            // 
            // chk_winstart
            // 
            this.chk_winstart.AutoSize = true;
            this.chk_winstart.Location = new System.Drawing.Point(24, 74);
            this.chk_winstart.Name = "chk_winstart";
            this.chk_winstart.Size = new System.Drawing.Size(177, 24);
            this.chk_winstart.TabIndex = 1;
            this.chk_winstart.Text = "Run on Windows Start";
            this.chk_winstart.UseVisualStyleBackColor = true;
            this.chk_winstart.CheckedChanged += new System.EventHandler(this.chk_winstart_CheckedChanged);
            // 
            // chk_localcache
            // 
            this.chk_localcache.AutoSize = true;
            this.chk_localcache.Location = new System.Drawing.Point(24, 44);
            this.chk_localcache.Name = "chk_localcache";
            this.chk_localcache.Size = new System.Drawing.Size(138, 24);
            this.chk_localcache.TabIndex = 0;
            this.chk_localcache.Text = "Use Local Cache";
            this.chk_localcache.UseVisualStyleBackColor = true;
            this.chk_localcache.CheckedChanged += new System.EventHandler(this.chk_localcache_CheckedChanged);
            // 
            // Uc_Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tlp_main);
            this.Name = "Uc_Settings";
            this.Size = new System.Drawing.Size(1021, 629);
            this.Load += new System.EventHandler(this.OnLoad);
            this.tlp_main.ResumeLayout(false);
            this.tlp_outer.ResumeLayout(false);
            this.gb_other.ResumeLayout(false);
            this.tlp_devices.ResumeLayout(false);
            this.tlp_devices.PerformLayout();
            this.gb_devicesettings.ResumeLayout(false);
            this.gb_devicesettings.PerformLayout();
            this.gb_general.ResumeLayout(false);
            this.gb_general.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlp_main;
        private System.Windows.Forms.TableLayoutPanel tlp_outer;
        private System.Windows.Forms.GroupBox gb_other;
        private System.Windows.Forms.GroupBox gb_general;
        private System.Windows.Forms.CheckBox chk_trayonstartup;
        private System.Windows.Forms.CheckBox chk_minimizetray;
        private System.Windows.Forms.CheckBox chk_winstart;
        private System.Windows.Forms.CheckBox chk_localcache;
        private MetroFramework.Controls.MetroButton btn_clearcache;
        private MetroFramework.Controls.MetroButton btn_resetchromatics;
        private System.Windows.Forms.GroupBox gb_devicesettings;
        private MetroFramework.Controls.MetroLabel lbl_devicebrightpercent;
        private MetroFramework.Controls.MetroLabel lbl_devicebright;
        private MetroFramework.Controls.MetroTrackBar trackbar_lighting;
        private System.Windows.Forms.CheckBox chk_updatecheck;
        private System.Windows.Forms.TableLayoutPanel tlp_devices;
        private MetroFramework.Controls.MetroTile mt_settings_wooting;
        private MetroFramework.Controls.MetroTile mt_settings_asus;
        private MetroFramework.Controls.MetroTile mt_settings_coolermaster;
        private MetroFramework.Controls.MetroTile mt_settings_steelseries;
        private MetroFramework.Controls.MetroTile mt_settings_corsair;
        private MetroFramework.Controls.MetroTile mt_settings_razer;
        private MetroFramework.Controls.MetroTile mt_settings_logitech;
        private MetroFramework.Controls.MetroTile mt_settings_hue;
        private MetroFramework.Controls.MetroTile mt_settings_msi;
        private MetroFramework.Controls.MetroTile mt_settings_novation;
        private MetroFramework.Controls.MetroTile mt_settings_openrgb;
    }
}
