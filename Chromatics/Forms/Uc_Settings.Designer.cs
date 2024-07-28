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
            tlp_main = new System.Windows.Forms.TableLayoutPanel();
            tlp_outer = new System.Windows.Forms.TableLayoutPanel();
            gb_other = new System.Windows.Forms.GroupBox();
            tlp_devices = new System.Windows.Forms.TableLayoutPanel();
            mt_settings_wooting = new MetroFramework.Controls.MetroTile();
            mt_settings_asus = new MetroFramework.Controls.MetroTile();
            mt_settings_coolermaster = new MetroFramework.Controls.MetroTile();
            mt_settings_steelseries = new MetroFramework.Controls.MetroTile();
            mt_settings_corsair = new MetroFramework.Controls.MetroTile();
            mt_settings_razer = new MetroFramework.Controls.MetroTile();
            mt_settings_logitech = new MetroFramework.Controls.MetroTile();
            mt_settings_msi = new MetroFramework.Controls.MetroTile();
            mt_settings_novation = new MetroFramework.Controls.MetroTile();
            mt_settings_hue = new MetroFramework.Controls.MetroTile();
            mt_settings_openrgb = new MetroFramework.Controls.MetroTile();
            gb_devicesettings = new System.Windows.Forms.GroupBox();
            lbl_devicebrightpercent = new MetroFramework.Controls.MetroLabel();
            lbl_devicebright = new MetroFramework.Controls.MetroLabel();
            trackbar_lighting = new MetroFramework.Controls.MetroTrackBar();
            gb_general = new System.Windows.Forms.GroupBox();
            lbl_language = new MetroFramework.Controls.MetroLabel();
            lbl_theme = new MetroFramework.Controls.MetroLabel();
            cb_language = new MetroFramework.Controls.MetroComboBox();
            cb_theme = new MetroFramework.Controls.MetroComboBox();
            chk_updatecheck = new MetroFramework.Controls.MetroCheckBox();
            btn_clearcache = new MetroFramework.Controls.MetroButton();
            btn_resetchromatics = new MetroFramework.Controls.MetroButton();
            chk_trayonstartup = new MetroFramework.Controls.MetroCheckBox();
            chk_minimizetray = new MetroFramework.Controls.MetroCheckBox();
            chk_winstart = new MetroFramework.Controls.MetroCheckBox();
            chk_localcache = new MetroFramework.Controls.MetroCheckBox();
            tlp_main.SuspendLayout();
            tlp_outer.SuspendLayout();
            gb_other.SuspendLayout();
            tlp_devices.SuspendLayout();
            gb_devicesettings.SuspendLayout();
            gb_general.SuspendLayout();
            SuspendLayout();
            // 
            // tlp_main
            // 
            tlp_main.ColumnCount = 2;
            tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45.83741F));
            tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54.16259F));
            tlp_main.Controls.Add(tlp_outer, 1, 0);
            tlp_main.Controls.Add(gb_general, 0, 0);
            tlp_main.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_main.Location = new System.Drawing.Point(0, 0);
            tlp_main.Margin = new System.Windows.Forms.Padding(4);
            tlp_main.Name = "tlp_main";
            tlp_main.RowCount = 1;
            tlp_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            tlp_main.Size = new System.Drawing.Size(1276, 786);
            tlp_main.TabIndex = 0;
            // 
            // tlp_outer
            // 
            tlp_outer.ColumnCount = 1;
            tlp_outer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_outer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            tlp_outer.Controls.Add(gb_other, 0, 0);
            tlp_outer.Controls.Add(gb_devicesettings, 0, 1);
            tlp_outer.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_outer.Location = new System.Drawing.Point(588, 4);
            tlp_outer.Margin = new System.Windows.Forms.Padding(4);
            tlp_outer.Name = "tlp_outer";
            tlp_outer.RowCount = 2;
            tlp_outer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 81.70145F));
            tlp_outer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18.29856F));
            tlp_outer.Size = new System.Drawing.Size(684, 778);
            tlp_outer.TabIndex = 0;
            // 
            // gb_other
            // 
            gb_other.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gb_other.Controls.Add(tlp_devices);
            gb_other.Location = new System.Drawing.Point(4, 4);
            gb_other.Margin = new System.Windows.Forms.Padding(4);
            gb_other.Name = "gb_other";
            gb_other.Padding = new System.Windows.Forms.Padding(4);
            gb_other.Size = new System.Drawing.Size(676, 627);
            gb_other.TabIndex = 0;
            gb_other.TabStop = false;
            gb_other.Text = "Devices";
            // 
            // tlp_devices
            // 
            tlp_devices.ColumnCount = 4;
            tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.Controls.Add(mt_settings_wooting, 1, 1);
            tlp_devices.Controls.Add(mt_settings_asus, 0, 1);
            tlp_devices.Controls.Add(mt_settings_coolermaster, 4, 0);
            tlp_devices.Controls.Add(mt_settings_steelseries, 3, 0);
            tlp_devices.Controls.Add(mt_settings_corsair, 2, 0);
            tlp_devices.Controls.Add(mt_settings_razer, 0, 0);
            tlp_devices.Controls.Add(mt_settings_logitech, 1, 0);
            tlp_devices.Controls.Add(mt_settings_msi, 3, 1);
            tlp_devices.Controls.Add(mt_settings_novation, 0, 2);
            tlp_devices.Controls.Add(mt_settings_hue, 2, 2);
            tlp_devices.Controls.Add(mt_settings_openrgb, 1, 2);
            tlp_devices.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_devices.Location = new System.Drawing.Point(4, 28);
            tlp_devices.Margin = new System.Windows.Forms.Padding(4);
            tlp_devices.Name = "tlp_devices";
            tlp_devices.RowCount = 5;
            tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlp_devices.Size = new System.Drawing.Size(668, 595);
            tlp_devices.TabIndex = 1;
            tlp_devices.Resize += OnResize;
            // 
            // mt_settings_wooting
            // 
            mt_settings_wooting.ActiveControl = null;
            mt_settings_wooting.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_wooting.AutoSize = true;
            mt_settings_wooting.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_wooting.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_wooting.Location = new System.Drawing.Point(340, 125);
            mt_settings_wooting.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_wooting.Name = "mt_settings_wooting";
            mt_settings_wooting.Size = new System.Drawing.Size(155, 107);
            mt_settings_wooting.TabIndex = 2;
            mt_settings_wooting.Text = "Wooting";
            mt_settings_wooting.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_wooting.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_wooting.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_wooting.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_wooting.UseCustomBackColor = true;
            mt_settings_wooting.UseSelectable = true;
            mt_settings_wooting.UseTileImage = true;
            mt_settings_wooting.UseVisualStyleBackColor = false;
            mt_settings_wooting.Click += mt_settings_wooting_Click;
            // 
            // mt_settings_asus
            // 
            mt_settings_asus.ActiveControl = null;
            mt_settings_asus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_asus.AutoSize = true;
            mt_settings_asus.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_asus.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_asus.Location = new System.Drawing.Point(173, 125);
            mt_settings_asus.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_asus.Name = "mt_settings_asus";
            mt_settings_asus.Size = new System.Drawing.Size(155, 107);
            mt_settings_asus.TabIndex = 2;
            mt_settings_asus.Text = "ASUS";
            mt_settings_asus.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_asus.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_asus.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_asus.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_asus.UseCustomBackColor = true;
            mt_settings_asus.UseSelectable = true;
            mt_settings_asus.UseTileImage = true;
            mt_settings_asus.UseVisualStyleBackColor = false;
            mt_settings_asus.Click += mt_settings_asus_Click;
            // 
            // mt_settings_coolermaster
            // 
            mt_settings_coolermaster.ActiveControl = null;
            mt_settings_coolermaster.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_coolermaster.AutoSize = true;
            mt_settings_coolermaster.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_coolermaster.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_coolermaster.Location = new System.Drawing.Point(6, 125);
            mt_settings_coolermaster.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_coolermaster.Name = "mt_settings_coolermaster";
            mt_settings_coolermaster.Size = new System.Drawing.Size(155, 107);
            mt_settings_coolermaster.TabIndex = 2;
            mt_settings_coolermaster.Text = "Coolermaster";
            mt_settings_coolermaster.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_coolermaster.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_coolermaster.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_coolermaster.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_coolermaster.UseCustomBackColor = true;
            mt_settings_coolermaster.UseSelectable = true;
            mt_settings_coolermaster.UseTileImage = true;
            mt_settings_coolermaster.UseVisualStyleBackColor = false;
            mt_settings_coolermaster.Click += mt_settings_coolermaster_Click;
            // 
            // mt_settings_steelseries
            // 
            mt_settings_steelseries.ActiveControl = null;
            mt_settings_steelseries.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_steelseries.AutoSize = true;
            mt_settings_steelseries.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_steelseries.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_steelseries.Location = new System.Drawing.Point(507, 6);
            mt_settings_steelseries.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_steelseries.Name = "mt_settings_steelseries";
            mt_settings_steelseries.Size = new System.Drawing.Size(155, 107);
            mt_settings_steelseries.TabIndex = 1;
            mt_settings_steelseries.Text = "SteelSeries";
            mt_settings_steelseries.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_steelseries.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_steelseries.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_steelseries.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_steelseries.UseCustomBackColor = true;
            mt_settings_steelseries.UseSelectable = true;
            mt_settings_steelseries.UseTileImage = true;
            mt_settings_steelseries.UseVisualStyleBackColor = false;
            mt_settings_steelseries.Click += mt_settings_steelseries_Click;
            // 
            // mt_settings_corsair
            // 
            mt_settings_corsair.ActiveControl = null;
            mt_settings_corsair.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_corsair.AutoSize = true;
            mt_settings_corsair.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_corsair.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_corsair.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            mt_settings_corsair.Location = new System.Drawing.Point(340, 6);
            mt_settings_corsair.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_corsair.Name = "mt_settings_corsair";
            mt_settings_corsair.Size = new System.Drawing.Size(155, 107);
            mt_settings_corsair.TabIndex = 0;
            mt_settings_corsair.Text = "Corsair";
            mt_settings_corsair.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_corsair.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_corsair.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_corsair.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_corsair.UseCustomBackColor = true;
            mt_settings_corsair.UseSelectable = true;
            mt_settings_corsair.UseTileImage = true;
            mt_settings_corsair.UseVisualStyleBackColor = false;
            mt_settings_corsair.Click += mt_settings_corsair_Click;
            // 
            // mt_settings_razer
            // 
            mt_settings_razer.ActiveControl = null;
            mt_settings_razer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_razer.AutoSize = true;
            mt_settings_razer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_razer.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_razer.Location = new System.Drawing.Point(6, 6);
            mt_settings_razer.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_razer.Name = "mt_settings_razer";
            mt_settings_razer.Size = new System.Drawing.Size(155, 107);
            mt_settings_razer.TabIndex = 2;
            mt_settings_razer.Text = "Razer";
            mt_settings_razer.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_razer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_razer.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_razer.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_razer.UseCustomBackColor = true;
            mt_settings_razer.UseSelectable = true;
            mt_settings_razer.UseTileImage = true;
            mt_settings_razer.UseVisualStyleBackColor = false;
            mt_settings_razer.Click += mt_settings_razer_Click;
            // 
            // mt_settings_logitech
            // 
            mt_settings_logitech.ActiveControl = null;
            mt_settings_logitech.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_logitech.AutoSize = true;
            mt_settings_logitech.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_logitech.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_logitech.Location = new System.Drawing.Point(173, 6);
            mt_settings_logitech.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_logitech.Name = "mt_settings_logitech";
            mt_settings_logitech.Size = new System.Drawing.Size(155, 107);
            mt_settings_logitech.TabIndex = 2;
            mt_settings_logitech.Text = "Logitech";
            mt_settings_logitech.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_logitech.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_logitech.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_logitech.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_logitech.UseCustomBackColor = true;
            mt_settings_logitech.UseSelectable = true;
            mt_settings_logitech.UseTileImage = true;
            mt_settings_logitech.UseVisualStyleBackColor = false;
            mt_settings_logitech.Click += mt_settings_logitech_Click;
            // 
            // mt_settings_msi
            // 
            mt_settings_msi.ActiveControl = null;
            mt_settings_msi.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_msi.AutoSize = true;
            mt_settings_msi.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_msi.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_msi.Location = new System.Drawing.Point(507, 125);
            mt_settings_msi.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_msi.Name = "mt_settings_msi";
            mt_settings_msi.Size = new System.Drawing.Size(155, 107);
            mt_settings_msi.TabIndex = 3;
            mt_settings_msi.Text = "MSI";
            mt_settings_msi.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_msi.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_msi.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_msi.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_msi.UseCustomBackColor = true;
            mt_settings_msi.UseSelectable = true;
            mt_settings_msi.UseTileImage = true;
            mt_settings_msi.UseVisualStyleBackColor = false;
            mt_settings_msi.Click += mt_settings_msi_Click;
            // 
            // mt_settings_novation
            // 
            mt_settings_novation.ActiveControl = null;
            mt_settings_novation.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_novation.AutoSize = true;
            mt_settings_novation.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_novation.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_novation.Location = new System.Drawing.Point(6, 244);
            mt_settings_novation.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_novation.Name = "mt_settings_novation";
            mt_settings_novation.Size = new System.Drawing.Size(155, 107);
            mt_settings_novation.TabIndex = 4;
            mt_settings_novation.Text = "Novation";
            mt_settings_novation.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_novation.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_novation.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_novation.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_novation.UseCustomBackColor = true;
            mt_settings_novation.UseSelectable = true;
            mt_settings_novation.UseTileImage = true;
            mt_settings_novation.UseVisualStyleBackColor = false;
            mt_settings_novation.Click += mt_settings_novation_Click;
            // 
            // mt_settings_hue
            // 
            mt_settings_hue.ActiveControl = null;
            mt_settings_hue.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_hue.AutoSize = true;
            mt_settings_hue.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_hue.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_hue.Enabled = false;
            mt_settings_hue.Location = new System.Drawing.Point(340, 244);
            mt_settings_hue.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_hue.Name = "mt_settings_hue";
            mt_settings_hue.Size = new System.Drawing.Size(155, 107);
            mt_settings_hue.TabIndex = 5;
            mt_settings_hue.Text = "Philips Hue";
            mt_settings_hue.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_hue.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_hue.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_hue.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_hue.UseCustomBackColor = true;
            mt_settings_hue.UseSelectable = true;
            mt_settings_hue.UseTileImage = true;
            mt_settings_hue.UseVisualStyleBackColor = false;
            mt_settings_hue.Visible = false;
            mt_settings_hue.Click += mt_settings_hue_Click;
            // 
            // mt_settings_openrgb
            // 
            mt_settings_openrgb.ActiveControl = null;
            mt_settings_openrgb.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            mt_settings_openrgb.AutoSize = true;
            mt_settings_openrgb.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            mt_settings_openrgb.BackColor = System.Drawing.Color.DeepSkyBlue;
            mt_settings_openrgb.Location = new System.Drawing.Point(173, 244);
            mt_settings_openrgb.Margin = new System.Windows.Forms.Padding(6);
            mt_settings_openrgb.Name = "mt_settings_openrgb";
            mt_settings_openrgb.Size = new System.Drawing.Size(155, 107);
            mt_settings_openrgb.TabIndex = 6;
            mt_settings_openrgb.Text = "OpenRGB";
            mt_settings_openrgb.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            mt_settings_openrgb.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            mt_settings_openrgb.TileImage = global::Chromatics.Properties.Resources.keyboard;
            mt_settings_openrgb.TileImageAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mt_settings_openrgb.UseCustomBackColor = true;
            mt_settings_openrgb.UseSelectable = true;
            mt_settings_openrgb.UseTileImage = true;
            mt_settings_openrgb.UseVisualStyleBackColor = false;
            mt_settings_openrgb.Click += mt_settings_openRGB_Click;
            // 
            // gb_devicesettings
            // 
            gb_devicesettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gb_devicesettings.Controls.Add(lbl_devicebrightpercent);
            gb_devicesettings.Controls.Add(lbl_devicebright);
            gb_devicesettings.Controls.Add(trackbar_lighting);
            gb_devicesettings.Location = new System.Drawing.Point(4, 639);
            gb_devicesettings.Margin = new System.Windows.Forms.Padding(4);
            gb_devicesettings.Name = "gb_devicesettings";
            gb_devicesettings.Padding = new System.Windows.Forms.Padding(4);
            gb_devicesettings.Size = new System.Drawing.Size(676, 135);
            gb_devicesettings.TabIndex = 1;
            gb_devicesettings.TabStop = false;
            gb_devicesettings.Text = "Global Device Controls";
            // 
            // lbl_devicebrightpercent
            // 
            lbl_devicebrightpercent.AutoSize = true;
            lbl_devicebrightpercent.Location = new System.Drawing.Point(336, 95);
            lbl_devicebrightpercent.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_devicebrightpercent.Name = "lbl_devicebrightpercent";
            lbl_devicebrightpercent.Size = new System.Drawing.Size(39, 19);
            lbl_devicebrightpercent.TabIndex = 2;
            lbl_devicebrightpercent.Text = "100%";
            lbl_devicebrightpercent.UseCustomBackColor = true;
            // 
            // lbl_devicebright
            // 
            lbl_devicebright.AutoSize = true;
            lbl_devicebright.Location = new System.Drawing.Point(19, 54);
            lbl_devicebright.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_devicebright.Name = "lbl_devicebright";
            lbl_devicebright.Size = new System.Drawing.Size(110, 19);
            lbl_devicebright.TabIndex = 1;
            lbl_devicebright.Text = "Device Brightness";
            lbl_devicebright.UseCustomBackColor = true;
            // 
            // trackbar_lighting
            // 
            trackbar_lighting.BackColor = System.Drawing.Color.Transparent;
            trackbar_lighting.LargeChange = 1;
            trackbar_lighting.Location = new System.Drawing.Point(179, 55);
            trackbar_lighting.Margin = new System.Windows.Forms.Padding(4);
            trackbar_lighting.MouseWheelBarPartitions = 5;
            trackbar_lighting.Name = "trackbar_lighting";
            trackbar_lighting.Size = new System.Drawing.Size(409, 36);
            trackbar_lighting.TabIndex = 0;
            trackbar_lighting.Text = "Device Brightness";
            trackbar_lighting.UseCustomBackColor = true;
            trackbar_lighting.Value = 100;
            trackbar_lighting.Scroll += trackbar_lighting_Scroll;
            // 
            // gb_general
            // 
            gb_general.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            gb_general.Controls.Add(lbl_language);
            gb_general.Controls.Add(lbl_theme);
            gb_general.Controls.Add(cb_language);
            gb_general.Controls.Add(cb_theme);
            gb_general.Controls.Add(chk_updatecheck);
            gb_general.Controls.Add(btn_clearcache);
            gb_general.Controls.Add(btn_resetchromatics);
            gb_general.Controls.Add(chk_trayonstartup);
            gb_general.Controls.Add(chk_minimizetray);
            gb_general.Controls.Add(chk_winstart);
            gb_general.Controls.Add(chk_localcache);
            gb_general.Location = new System.Drawing.Point(4, 4);
            gb_general.Margin = new System.Windows.Forms.Padding(4);
            gb_general.Name = "gb_general";
            gb_general.Padding = new System.Windows.Forms.Padding(4);
            gb_general.Size = new System.Drawing.Size(576, 778);
            gb_general.TabIndex = 1;
            gb_general.TabStop = false;
            gb_general.Text = "General";
            // 
            // lbl_language
            // 
            lbl_language.AutoSize = true;
            lbl_language.Location = new System.Drawing.Point(245, 592);
            lbl_language.Name = "lbl_language";
            lbl_language.Size = new System.Drawing.Size(89, 25);
            lbl_language.TabIndex = 12;
            lbl_language.Text = "Language";
            // 
            // lbl_theme
            // 
            lbl_theme.AutoSize = true;
            lbl_theme.Location = new System.Drawing.Point(30, 592);
            lbl_theme.Name = "lbl_theme";
            lbl_theme.Size = new System.Drawing.Size(65, 25);
            lbl_theme.TabIndex = 11;
            lbl_theme.Text = "Theme";
            // 
            // cb_language
            // 
            cb_language.FormattingEnabled = true;
            cb_language.Location = new System.Drawing.Point(245, 630);
            cb_language.Name = "cb_language";
            cb_language.Size = new System.Drawing.Size(182, 33);
            cb_language.TabIndex = 10;
            cb_language.SelectedIndexChanged += cb_language_SelectedIndexChanged;
            // 
            // cb_theme
            // 
            cb_theme.FormattingEnabled = true;
            cb_theme.Location = new System.Drawing.Point(30, 630);
            cb_theme.Name = "cb_theme";
            cb_theme.Size = new System.Drawing.Size(182, 33);
            cb_theme.TabIndex = 9;
            cb_theme.SelectedIndexChanged += cb_theme_SelectedIndexChanged;
            // 
            // chk_updatecheck
            // 
            chk_updatecheck.AutoSize = true;
            chk_updatecheck.Location = new System.Drawing.Point(30, 205);
            chk_updatecheck.Margin = new System.Windows.Forms.Padding(4);
            chk_updatecheck.Name = "chk_updatecheck";
            chk_updatecheck.Size = new System.Drawing.Size(184, 29);
            chk_updatecheck.TabIndex = 8;
            chk_updatecheck.Text = "Check for Updates";
            chk_updatecheck.UseVisualStyleBackColor = true;
            chk_updatecheck.CheckedChanged += chk_updatecheck_CheckedChanged;
            // 
            // btn_clearcache
            // 
            btn_clearcache.Location = new System.Drawing.Point(245, 696);
            btn_clearcache.Margin = new System.Windows.Forms.Padding(4);
            btn_clearcache.Name = "btn_clearcache";
            btn_clearcache.Size = new System.Drawing.Size(189, 54);
            btn_clearcache.TabIndex = 7;
            btn_clearcache.Text = "Clear Cache";
            btn_clearcache.UseSelectable = true;
            btn_clearcache.Click += btn_clearcache_Click;
            // 
            // btn_resetchromatics
            // 
            btn_resetchromatics.Location = new System.Drawing.Point(30, 696);
            btn_resetchromatics.Margin = new System.Windows.Forms.Padding(4);
            btn_resetchromatics.Name = "btn_resetchromatics";
            btn_resetchromatics.Size = new System.Drawing.Size(189, 54);
            btn_resetchromatics.TabIndex = 6;
            btn_resetchromatics.Text = "Reset Chromatics";
            btn_resetchromatics.UseSelectable = true;
            btn_resetchromatics.Click += btn_resetchromatics_Click;
            // 
            // chk_trayonstartup
            // 
            chk_trayonstartup.AutoSize = true;
            chk_trayonstartup.Location = new System.Drawing.Point(30, 168);
            chk_trayonstartup.Margin = new System.Windows.Forms.Padding(4);
            chk_trayonstartup.Name = "chk_trayonstartup";
            chk_trayonstartup.Size = new System.Drawing.Size(255, 29);
            chk_trayonstartup.TabIndex = 4;
            chk_trayonstartup.Text = "Minimise to Tray on Startup";
            chk_trayonstartup.UseVisualStyleBackColor = true;
            chk_trayonstartup.CheckedChanged += chk_trayonstartup_CheckedChanged;
            // 
            // chk_minimizetray
            // 
            chk_minimizetray.AutoSize = true;
            chk_minimizetray.Location = new System.Drawing.Point(30, 130);
            chk_minimizetray.Margin = new System.Windows.Forms.Padding(4);
            chk_minimizetray.Name = "chk_minimizetray";
            chk_minimizetray.Size = new System.Drawing.Size(167, 29);
            chk_minimizetray.TabIndex = 3;
            chk_minimizetray.Text = "Minimise to Tray";
            chk_minimizetray.UseVisualStyleBackColor = true;
            chk_minimizetray.CheckedChanged += chk_minimizetray_CheckedChanged;
            // 
            // chk_winstart
            // 
            chk_winstart.AutoSize = true;
            chk_winstart.Location = new System.Drawing.Point(30, 92);
            chk_winstart.Margin = new System.Windows.Forms.Padding(4);
            chk_winstart.Name = "chk_winstart";
            chk_winstart.Size = new System.Drawing.Size(215, 29);
            chk_winstart.TabIndex = 1;
            chk_winstart.Text = "Run on Windows Start";
            chk_winstart.UseVisualStyleBackColor = true;
            chk_winstart.CheckedChanged += chk_winstart_CheckedChanged;
            // 
            // chk_localcache
            // 
            chk_localcache.AutoSize = true;
            chk_localcache.Location = new System.Drawing.Point(30, 55);
            chk_localcache.Margin = new System.Windows.Forms.Padding(4);
            chk_localcache.Name = "chk_localcache";
            chk_localcache.Size = new System.Drawing.Size(164, 29);
            chk_localcache.TabIndex = 0;
            chk_localcache.Text = "Use Local Cache";
            chk_localcache.UseVisualStyleBackColor = true;
            chk_localcache.CheckedChanged += chk_localcache_CheckedChanged;
            // 
            // Uc_Settings
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(tlp_main);
            Margin = new System.Windows.Forms.Padding(4);
            Name = "Uc_Settings";
            Size = new System.Drawing.Size(1276, 786);
            Load += OnLoad;
            tlp_main.ResumeLayout(false);
            tlp_outer.ResumeLayout(false);
            gb_other.ResumeLayout(false);
            tlp_devices.ResumeLayout(false);
            tlp_devices.PerformLayout();
            gb_devicesettings.ResumeLayout(false);
            gb_devicesettings.PerformLayout();
            gb_general.ResumeLayout(false);
            gb_general.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlp_main;
        private System.Windows.Forms.TableLayoutPanel tlp_outer;
        private System.Windows.Forms.GroupBox gb_other;
        private System.Windows.Forms.GroupBox gb_general;
        private MetroFramework.Controls.MetroCheckBox chk_trayonstartup;
        private MetroFramework.Controls.MetroCheckBox chk_minimizetray;
        private MetroFramework.Controls.MetroCheckBox chk_winstart;
        private MetroFramework.Controls.MetroCheckBox chk_localcache;
        private MetroFramework.Controls.MetroButton btn_clearcache;
        private MetroFramework.Controls.MetroButton btn_resetchromatics;
        private System.Windows.Forms.GroupBox gb_devicesettings;
        private MetroFramework.Controls.MetroLabel lbl_devicebrightpercent;
        private MetroFramework.Controls.MetroLabel lbl_devicebright;
        private MetroFramework.Controls.MetroTrackBar trackbar_lighting;
        private MetroFramework.Controls.MetroCheckBox chk_updatecheck;
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
        private MetroFramework.Controls.MetroComboBox cb_theme;
        private MetroFramework.Controls.MetroLabel lbl_language;
        private MetroFramework.Controls.MetroLabel lbl_theme;
        private MetroFramework.Controls.MetroComboBox cb_language;

    }
}
