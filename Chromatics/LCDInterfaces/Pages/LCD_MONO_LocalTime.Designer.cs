namespace Chromatics.LCDInterfaces
{
    partial class LCD_MONO_LocalTime
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
            this.lbl_lt_test = new System.Windows.Forms.Label();
            this.lbl_et_name = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbl_lt_test
            // 
            this.lbl_lt_test.BackColor = System.Drawing.Color.Transparent;
            this.lbl_lt_test.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_lt_test.ForeColor = System.Drawing.Color.White;
            this.lbl_lt_test.Location = new System.Drawing.Point(20, 0);
            this.lbl_lt_test.Name = "lbl_lt_test";
            this.lbl_lt_test.Size = new System.Drawing.Size(120, 29);
            this.lbl_lt_test.TabIndex = 0;
            this.lbl_lt_test.Text = "00:00 AM";
            this.lbl_lt_test.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbl_et_name
            // 
            this.lbl_et_name.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_et_name.ForeColor = System.Drawing.Color.White;
            this.lbl_et_name.Location = new System.Drawing.Point(37, 27);
            this.lbl_et_name.Name = "lbl_et_name";
            this.lbl_et_name.Size = new System.Drawing.Size(87, 17);
            this.lbl_et_name.TabIndex = 1;
            this.lbl_et_name.Text = "Local Time";
            this.lbl_et_name.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // LCD_MONO_LocalTime
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.lbl_et_name);
            this.Controls.Add(this.lbl_lt_test);
            this.Name = "LCD_MONO_LocalTime";
            this.Size = new System.Drawing.Size(160, 43);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lbl_lt_test;
        private System.Windows.Forms.Label lbl_et_name;
    }
}
