
using Chromatics.Core;

namespace Chromatics.Forms
{
    partial class Uc_Console
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
                Logger.OnConsoleLogged -= new OnConsoleLoggedEventHandler(OnConsoleLogged);
                rtb_console.TextChanged -= rtb_console_TextChanged;
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
            this.rtb_console = new System.Windows.Forms.RichTextBox();
            this.tlp_console = new System.Windows.Forms.TableLayoutPanel();
            this.tlp_console.SuspendLayout();
            this.SuspendLayout();
            // 
            // rtb_console
            // 
            this.rtb_console.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtb_console.Font = new System.Drawing.Font("Segoe UI", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.rtb_console.Location = new System.Drawing.Point(3, 38);
            this.rtb_console.Name = "rtb_console";
            this.rtb_console.ReadOnly = true;
            this.rtb_console.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtb_console.Size = new System.Drawing.Size(1198, 673);
            this.rtb_console.TabIndex = 0;
            this.rtb_console.Text = "";
            // 
            // tlp_console
            // 
            this.tlp_console.ColumnCount = 1;
            this.tlp_console.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_console.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_console.Controls.Add(this.rtb_console, 0, 1);
            this.tlp_console.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_console.Location = new System.Drawing.Point(0, 0);
            this.tlp_console.Name = "tlp_console";
            this.tlp_console.RowCount = 2;
            this.tlp_console.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlp_console.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 95F));
            this.tlp_console.Size = new System.Drawing.Size(1204, 714);
            this.tlp_console.TabIndex = 1;
            // 
            // Uc_Console
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tlp_console);
            this.Name = "Uc_Console";
            this.Size = new System.Drawing.Size(1204, 714);
            this.tlp_console.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtb_console;
        private System.Windows.Forms.TableLayoutPanel tlp_console;
    }
}
