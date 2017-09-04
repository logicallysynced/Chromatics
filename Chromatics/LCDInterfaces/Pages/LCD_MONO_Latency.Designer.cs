namespace Chromatics.LCDInterfaces
{
    partial class LCD_MONO_Latency
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
            this.lbl_latency_ping = new System.Windows.Forms.Label();
            this.lbl_latency_name = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbl_latency_ping
            // 
            this.lbl_latency_ping.BackColor = System.Drawing.Color.Transparent;
            this.lbl_latency_ping.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_latency_ping.ForeColor = System.Drawing.Color.White;
            this.lbl_latency_ping.Location = new System.Drawing.Point(37, 0);
            this.lbl_latency_ping.Name = "lbl_latency_ping";
            this.lbl_latency_ping.Size = new System.Drawing.Size(89, 29);
            this.lbl_latency_ping.TabIndex = 0;
            this.lbl_latency_ping.Text = "0ms";
            this.lbl_latency_ping.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbl_latency_name
            // 
            this.lbl_latency_name.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_latency_name.ForeColor = System.Drawing.Color.White;
            this.lbl_latency_name.Location = new System.Drawing.Point(0, 27);
            this.lbl_latency_name.Name = "lbl_latency_name";
            this.lbl_latency_name.Size = new System.Drawing.Size(160, 16);
            this.lbl_latency_name.TabIndex = 1;
            this.lbl_latency_name.Text = "Latency";
            this.lbl_latency_name.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // LCD_MONO_Latency
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.lbl_latency_name);
            this.Controls.Add(this.lbl_latency_ping);
            this.Name = "LCD_MONO_Latency";
            this.Size = new System.Drawing.Size(160, 43);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lbl_latency_ping;
        private System.Windows.Forms.Label lbl_latency_name;
    }
}
