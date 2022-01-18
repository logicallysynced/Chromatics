
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Fm_MainWindow));
            this.mT_TabManager = new MetroFramework.Controls.MetroTabControl();
            this.tP_console = new System.Windows.Forms.TabPage();
            this.tP_Effects = new System.Windows.Forms.TabPage();
            this.tP_mappings = new System.Windows.Forms.TabPage();
            this.tP_palette = new System.Windows.Forms.TabPage();
            this.mT_TabManager.SuspendLayout();
            this.SuspendLayout();
            // 
            // mT_TabManager
            // 
            this.mT_TabManager.Controls.Add(this.tP_console);
            this.mT_TabManager.Controls.Add(this.tP_mappings);
            this.mT_TabManager.Controls.Add(this.tP_palette);
            this.mT_TabManager.Controls.Add(this.tP_Effects);
            this.mT_TabManager.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mT_TabManager.Location = new System.Drawing.Point(20, 60);
            this.mT_TabManager.Name = "mT_TabManager";
            this.mT_TabManager.Padding = new System.Drawing.Point(6, 8);
            this.mT_TabManager.SelectedIndex = 0;
            this.mT_TabManager.Size = new System.Drawing.Size(1345, 726);
            this.mT_TabManager.TabIndex = 0;
            this.mT_TabManager.UseSelectable = true;
            // 
            // tP_console
            // 
            this.tP_console.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.tP_console.Location = new System.Drawing.Point(4, 38);
            this.tP_console.Name = "tP_console";
            this.tP_console.Size = new System.Drawing.Size(1337, 684);
            this.tP_console.TabIndex = 0;
            this.tP_console.Text = "Console";
            // 
            // tP_Effects
            // 
            this.tP_Effects.Location = new System.Drawing.Point(4, 38);
            this.tP_Effects.Name = "tP_Effects";
            this.tP_Effects.Size = new System.Drawing.Size(1337, 684);
            this.tP_Effects.TabIndex = 3;
            this.tP_Effects.Text = "Effects";
            // 
            // tP_mappings
            // 
            this.tP_mappings.Location = new System.Drawing.Point(4, 38);
            this.tP_mappings.Name = "tP_mappings";
            this.tP_mappings.Size = new System.Drawing.Size(1337, 684);
            this.tP_mappings.TabIndex = 1;
            this.tP_mappings.Text = "Mappings";
            // 
            // tP_palette
            // 
            this.tP_palette.Location = new System.Drawing.Point(4, 38);
            this.tP_palette.Name = "tP_palette";
            this.tP_palette.Size = new System.Drawing.Size(1337, 684);
            this.tP_palette.TabIndex = 2;
            this.tP_palette.Text = "Color Palette";
            // 
            // Fm_MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1385, 806);
            this.Controls.Add(this.mT_TabManager);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Fm_MainWindow";
            this.Text = "Chromatics";
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
    }
}