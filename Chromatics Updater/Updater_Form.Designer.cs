namespace Chromatics_Updater
{
    partial class Updater_Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Updater_Form));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.prog_Download = new System.Windows.Forms.ProgressBar();
            this.lbl_data = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(2, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(219, 87);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // prog_Download
            // 
            this.prog_Download.Location = new System.Drawing.Point(12, 133);
            this.prog_Download.Name = "prog_Download";
            this.prog_Download.Size = new System.Drawing.Size(431, 23);
            this.prog_Download.TabIndex = 1;
            this.prog_Download.Click += new System.EventHandler(this.prog_Download_Click);
            // 
            // lbl_data
            // 
            this.lbl_data.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_data.Location = new System.Drawing.Point(12, 161);
            this.lbl_data.Name = "lbl_data";
            this.lbl_data.Size = new System.Drawing.Size(431, 32);
            this.lbl_data.TabIndex = 2;
            this.lbl_data.Text = "Preparing to Update Chromatics";
            this.lbl_data.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Updater_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(455, 216);
            this.Controls.Add(this.lbl_data);
            this.Controls.Add(this.prog_Download);
            this.Controls.Add(this.pictureBox1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Updater_Form";
            this.Text = "Updating Chromatics";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ProgressBar prog_Download;
        private System.Windows.Forms.Label lbl_data;
    }
}

