namespace Chromatics.LCDInterfaces
{
    partial class LCD_MONO_Boot
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
            this.pB_logo1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pB_logo1)).BeginInit();
            this.SuspendLayout();
            // 
            // pB_logo1
            // 
            this.pB_logo1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pB_logo1.Image = global::Chromatics.Properties.Resources.chromatics_white_sm;
            this.pB_logo1.InitialImage = global::Chromatics.Properties.Resources.chromatics_white_sm;
            this.pB_logo1.Location = new System.Drawing.Point(0, -2);
            this.pB_logo1.Name = "pB_logo1";
            this.pB_logo1.Size = new System.Drawing.Size(160, 43);
            this.pB_logo1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pB_logo1.TabIndex = 3;
            this.pB_logo1.TabStop = false;
            // 
            // LCD_MONO_Boot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.pB_logo1);
            this.Name = "LCD_MONO_Boot";
            this.Size = new System.Drawing.Size(160, 43);
            ((System.ComponentModel.ISupportInitialize)(this.pB_logo1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pB_logo1;
    }
}
