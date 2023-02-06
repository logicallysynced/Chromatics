
namespace Chromatics.Forms
{
    partial class Fm_MainWindow
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Fm_MainWindow));
            this.mT_TabManager = new MetroFramework.Controls.MetroTabControl();
            this.tP_console = new System.Windows.Forms.TabPage();
            this.tP_mappings = new System.Windows.Forms.TabPage();
            this.tP_palette = new System.Windows.Forms.TabPage();
            this.tP_Effects = new System.Windows.Forms.TabPage();
            this.tP_Settings = new System.Windows.Forms.TabPage();
            this.notifyIcon_main = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip_main = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btn_help = new MetroFramework.Controls.MetroButton();
            this.mT_TabManager.SuspendLayout();
            this.SuspendLayout();
            // 
            // mT_TabManager
            // 
            this.mT_TabManager.Controls.Add(this.tP_console);
            this.mT_TabManager.Controls.Add(this.tP_mappings);
            this.mT_TabManager.Controls.Add(this.tP_palette);
            this.mT_TabManager.Controls.Add(this.tP_Effects);
            this.mT_TabManager.Controls.Add(this.tP_Settings);
            this.mT_TabManager.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mT_TabManager.Location = new System.Drawing.Point(20, 80);
            this.mT_TabManager.Name = "mT_TabManager";
            this.mT_TabManager.Padding = new System.Drawing.Point(6, 8);
            this.mT_TabManager.SelectedIndex = 0;
            this.mT_TabManager.Size = new System.Drawing.Size(1345, 706);
            this.mT_TabManager.TabIndex = 0;
            this.mT_TabManager.UseSelectable = true;
            // 
            // tP_console
            // 
            this.tP_console.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.tP_console.Location = new System.Drawing.Point(4, 38);
            this.tP_console.Name = "tP_console";
            this.tP_console.Size = new System.Drawing.Size(1337, 664);
            this.tP_console.TabIndex = 0;
            this.tP_console.Text = "Console";
            // 
            // tP_mappings
            // 
            this.tP_mappings.Location = new System.Drawing.Point(4, 39);
            this.tP_mappings.Name = "tP_mappings";
            this.tP_mappings.Size = new System.Drawing.Size(1337, 663);
            this.tP_mappings.TabIndex = 1;
            this.tP_mappings.Text = "Mappings";
            // 
            // tP_palette
            // 
            this.tP_palette.Location = new System.Drawing.Point(4, 39);
            this.tP_palette.Name = "tP_palette";
            this.tP_palette.Size = new System.Drawing.Size(1337, 663);
            this.tP_palette.TabIndex = 2;
            this.tP_palette.Text = "Color Palette";
            // 
            // tP_Effects
            // 
            this.tP_Effects.Location = new System.Drawing.Point(4, 39);
            this.tP_Effects.Name = "tP_Effects";
            this.tP_Effects.Size = new System.Drawing.Size(1337, 663);
            this.tP_Effects.TabIndex = 3;
            this.tP_Effects.Text = "Effects";
            // 
            // tP_Settings
            // 
            this.tP_Settings.Location = new System.Drawing.Point(4, 39);
            this.tP_Settings.Name = "tP_Settings";
            this.tP_Settings.Size = new System.Drawing.Size(1337, 663);
            this.tP_Settings.TabIndex = 4;
            this.tP_Settings.Text = "Settings";
            // 
            // notifyIcon_main
            // 
            this.notifyIcon_main.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon_main.Icon")));
            this.notifyIcon_main.Text = "Chromatics";
            this.notifyIcon_main.Visible = true;
            this.notifyIcon_main.DoubleClick += new System.EventHandler(this.OnNotifyIconDoubleClick);
            // 
            // contextMenuStrip_main
            // 
            this.contextMenuStrip_main.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip_main.Name = "contextMenuStrip_main";
            this.contextMenuStrip_main.Size = new System.Drawing.Size(61, 4);
            // 
            // btn_help
            // 
            this.btn_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_help.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_help.Location = new System.Drawing.Point(1243, 82);
            this.btn_help.Name = "btn_help";
            this.btn_help.Size = new System.Drawing.Size(118, 29);
            this.btn_help.TabIndex = 1;
            this.btn_help.Text = "Documentation";
            this.btn_help.UseSelectable = true;
            this.btn_help.UseVisualStyleBackColor = false;
            this.btn_help.Click += new System.EventHandler(this.btn_help_Click);
            // 
            // Fm_MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackImage = ((System.Drawing.Image)(resources.GetObject("$this.BackImage")));
            this.BackImagePadding = new System.Windows.Forms.Padding(15, 10, 0, 0);
            this.BackMaxSize = 200;
            this.ClientSize = new System.Drawing.Size(1385, 806);
            this.Controls.Add(this.btn_help);
            this.Controls.Add(this.mT_TabManager);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Fm_MainWindow";
            this.Padding = new System.Windows.Forms.Padding(20, 80, 20, 20);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            this.Load += new System.EventHandler(this.OnLoad);
            this.Resize += new System.EventHandler(this.OnResize);
            this.mT_TabManager.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MetroFramework.Controls.MetroTabControl mT_TabManager;
        private System.Windows.Forms.TabPage tP_console;
        private System.Windows.Forms.TabPage tP_mappings;
        private System.Windows.Forms.TabPage tP_palette;
        private System.Windows.Forms.TabPage tP_Effects;
        private System.Windows.Forms.TabPage tP_Settings;
        private System.Windows.Forms.NotifyIcon notifyIcon_main;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip_main;
        private MetroFramework.Controls.MetroButton btn_help;
    }
}