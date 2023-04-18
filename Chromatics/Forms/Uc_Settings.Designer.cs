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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.gb_other = new System.Windows.Forms.GroupBox();
            this.gb_devicesettings = new System.Windows.Forms.GroupBox();
            this.lbl_devicebrightpercent = new MetroFramework.Controls.MetroLabel();
            this.lbl_devicebright = new MetroFramework.Controls.MetroLabel();
            this.trackbar_lighting = new MetroFramework.Controls.MetroTrackBar();
            this.gb_general = new System.Windows.Forms.GroupBox();
            this.btn_clearcache = new MetroFramework.Controls.MetroButton();
            this.btn_resetchromatics = new MetroFramework.Controls.MetroButton();
            this.chk_trayonstartup = new System.Windows.Forms.CheckBox();
            this.chk_minimizetray = new System.Windows.Forms.CheckBox();
            this.chk_winstart = new System.Windows.Forms.CheckBox();
            this.chk_localcache = new System.Windows.Forms.CheckBox();
            this.chk_updatecheck = new System.Windows.Forms.CheckBox();
            this.tlp_main.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.gb_devicesettings.SuspendLayout();
            this.gb_general.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlp_main
            // 
            this.tlp_main.ColumnCount = 2;
            this.tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45.83741F));
            this.tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 54.16259F));
            this.tlp_main.Controls.Add(this.tableLayoutPanel1, 1, 0);
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
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.gb_other, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.gb_devicesettings, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(470, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 81.70145F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18.29856F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(548, 623);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // gb_other
            // 
            this.gb_other.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gb_other.Location = new System.Drawing.Point(3, 3);
            this.gb_other.Name = "gb_other";
            this.gb_other.Size = new System.Drawing.Size(542, 502);
            this.gb_other.TabIndex = 0;
            this.gb_other.TabStop = false;
            this.gb_other.Text = "Settings";
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
            // Uc_Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tlp_main);
            this.Name = "Uc_Settings";
            this.Size = new System.Drawing.Size(1021, 629);
            this.Load += new System.EventHandler(this.OnLoad);
            this.tlp_main.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.gb_devicesettings.ResumeLayout(false);
            this.gb_devicesettings.PerformLayout();
            this.gb_general.ResumeLayout(false);
            this.gb_general.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlp_main;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
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
    }
}
