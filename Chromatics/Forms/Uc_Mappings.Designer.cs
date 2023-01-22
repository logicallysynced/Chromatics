
using System.Drawing;

namespace Chromatics.Forms
{
    partial class Uc_Mappings
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
            this.tlp_base = new System.Windows.Forms.TableLayoutPanel();
            this.tlp_mid = new System.Windows.Forms.TableLayoutPanel();
            this.pn_right = new System.Windows.Forms.Panel();
            this.flp_layers = new System.Windows.Forms.FlowLayoutPanel();
            this.tlp_frame = new System.Windows.Forms.TableLayoutPanel();
            this.tlp_controls = new System.Windows.Forms.TableLayoutPanel();
            this.btn_clearselection = new MetroFramework.Controls.MetroButton();
            this.btn_reverseselection = new MetroFramework.Controls.MetroButton();
            this.btn_undoselection = new MetroFramework.Controls.MetroButton();
            this.tlp_layercontrols = new System.Windows.Forms.TableLayoutPanel();
            this.cb_changemode = new MetroFramework.Controls.MetroComboBox();
            this.btn_togglebleed = new MetroFramework.Controls.MetroButton();
            this.pn_top = new System.Windows.Forms.Panel();
            this.tlp_top = new System.Windows.Forms.TableLayoutPanel();
            this.btn_export = new MetroFramework.Controls.MetroButton();
            this.btn_import = new MetroFramework.Controls.MetroButton();
            this.cb_addlayer = new MetroFramework.Controls.MetroComboBox();
            this.btn_addlayer = new MetroFramework.Controls.MetroButton();
            this.cb_deviceselect = new MetroFramework.Controls.MetroComboBox();
            this.btn_preview = new MetroFramework.Controls.MetroButton();
            this.pn_bottom = new System.Windows.Forms.Panel();
            this.tlp_base.SuspendLayout();
            this.tlp_mid.SuspendLayout();
            this.pn_right.SuspendLayout();
            this.tlp_frame.SuspendLayout();
            this.tlp_controls.SuspendLayout();
            this.tlp_layercontrols.SuspendLayout();
            this.pn_top.SuspendLayout();
            this.tlp_top.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlp_base
            // 
            this.tlp_base.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tlp_base.AutoSize = true;
            this.tlp_base.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlp_base.BackColor = System.Drawing.SystemColors.Control;
            this.tlp_base.ColumnCount = 1;
            this.tlp_base.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_base.Controls.Add(this.tlp_mid, 0, 1);
            this.tlp_base.Controls.Add(this.pn_top, 0, 0);
            this.tlp_base.Controls.Add(this.pn_bottom, 0, 2);
            this.tlp_base.Location = new System.Drawing.Point(0, 0);
            this.tlp_base.Name = "tlp_base";
            this.tlp_base.RowCount = 3;
            this.tlp_base.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlp_base.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90F));
            this.tlp_base.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tlp_base.Size = new System.Drawing.Size(1870, 1040);
            this.tlp_base.TabIndex = 0;
            // 
            // tlp_mid
            // 
            this.tlp_mid.ColumnCount = 2;
            this.tlp_mid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tlp_mid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tlp_mid.Controls.Add(this.pn_right, 1, 0);
            this.tlp_mid.Controls.Add(this.tlp_frame, 0, 0);
            this.tlp_mid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_mid.Location = new System.Drawing.Point(3, 55);
            this.tlp_mid.Name = "tlp_mid";
            this.tlp_mid.RowCount = 1;
            this.tlp_mid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_mid.Size = new System.Drawing.Size(1864, 930);
            this.tlp_mid.TabIndex = 0;
            // 
            // pn_right
            // 
            this.pn_right.Controls.Add(this.flp_layers);
            this.pn_right.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pn_right.Location = new System.Drawing.Point(1307, 3);
            this.pn_right.Name = "pn_right";
            this.pn_right.Size = new System.Drawing.Size(554, 924);
            this.pn_right.TabIndex = 1;
            // 
            // flp_layers
            // 
            this.flp_layers.AllowDrop = true;
            this.flp_layers.AutoScroll = true;
            this.flp_layers.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.flp_layers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flp_layers.Location = new System.Drawing.Point(0, 0);
            this.flp_layers.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.flp_layers.Name = "flp_layers";
            this.flp_layers.Size = new System.Drawing.Size(554, 924);
            this.flp_layers.TabIndex = 3;
            // 
            // tlp_frame
            // 
            this.tlp_frame.ColumnCount = 2;
            this.tlp_frame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 93.37442F));
            this.tlp_frame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.625578F));
            this.tlp_frame.Controls.Add(this.tlp_controls, 1, 0);
            this.tlp_frame.Controls.Add(this.tlp_layercontrols, 0, 1);
            this.tlp_frame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_frame.Location = new System.Drawing.Point(3, 3);
            this.tlp_frame.Name = "tlp_frame";
            this.tlp_frame.RowCount = 3;
            this.tlp_frame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 37.5603F));
            this.tlp_frame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.117445F));
            this.tlp_frame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 56.32226F));
            this.tlp_frame.Size = new System.Drawing.Size(1298, 924);
            this.tlp_frame.TabIndex = 2;
            // 
            // tlp_controls
            // 
            this.tlp_controls.ColumnCount = 1;
            this.tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_controls.Controls.Add(this.btn_clearselection, 0, 0);
            this.tlp_controls.Controls.Add(this.btn_reverseselection, 0, 1);
            this.tlp_controls.Controls.Add(this.btn_undoselection, 0, 2);
            this.tlp_controls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_controls.Location = new System.Drawing.Point(1215, 3);
            this.tlp_controls.Name = "tlp_controls";
            this.tlp_controls.RowCount = 4;
            this.tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tlp_controls.Size = new System.Drawing.Size(80, 341);
            this.tlp_controls.TabIndex = 0;
            // 
            // btn_clearselection
            // 
            this.btn_clearselection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_clearselection.AutoSize = true;
            this.btn_clearselection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btn_clearselection.Enabled = false;
            this.btn_clearselection.Location = new System.Drawing.Point(3, 3);
            this.btn_clearselection.Name = "btn_clearselection";
            this.btn_clearselection.Size = new System.Drawing.Size(74, 30);
            this.btn_clearselection.TabIndex = 0;
            this.btn_clearselection.Text = "Clear";
            this.btn_clearselection.UseSelectable = true;
            this.btn_clearselection.Click += new System.EventHandler(this.btn_clearselection_Click);
            // 
            // btn_reverseselection
            // 
            this.btn_reverseselection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_reverseselection.AutoSize = true;
            this.btn_reverseselection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btn_reverseselection.Enabled = false;
            this.btn_reverseselection.Location = new System.Drawing.Point(3, 88);
            this.btn_reverseselection.Name = "btn_reverseselection";
            this.btn_reverseselection.Size = new System.Drawing.Size(74, 30);
            this.btn_reverseselection.TabIndex = 1;
            this.btn_reverseselection.Text = "Reverse";
            this.btn_reverseselection.UseSelectable = true;
            this.btn_reverseselection.Click += new System.EventHandler(this.btn_reverseselection_Click);
            // 
            // btn_undoselection
            // 
            this.btn_undoselection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_undoselection.AutoSize = true;
            this.btn_undoselection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btn_undoselection.Location = new System.Drawing.Point(3, 173);
            this.btn_undoselection.Name = "btn_undoselection";
            this.btn_undoselection.Size = new System.Drawing.Size(74, 30);
            this.btn_undoselection.TabIndex = 2;
            this.btn_undoselection.Text = "Undo";
            this.btn_undoselection.UseSelectable = true;
            this.btn_undoselection.Click += new System.EventHandler(this.btn_undoselection_Click);
            // 
            // tlp_layercontrols
            // 
            this.tlp_layercontrols.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tlp_layercontrols.ColumnCount = 4;
            this.tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tlp_layercontrols.Controls.Add(this.cb_changemode, 3, 0);
            this.tlp_layercontrols.Controls.Add(this.btn_togglebleed, 2, 0);
            this.tlp_layercontrols.Location = new System.Drawing.Point(3, 350);
            this.tlp_layercontrols.Name = "tlp_layercontrols";
            this.tlp_layercontrols.RowCount = 1;
            this.tlp_layercontrols.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_layercontrols.Size = new System.Drawing.Size(1206, 50);
            this.tlp_layercontrols.TabIndex = 1;
            // 
            // cb_changemode
            // 
            this.cb_changemode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cb_changemode.FormattingEnabled = true;
            this.cb_changemode.ItemHeight = 24;
            this.cb_changemode.Location = new System.Drawing.Point(906, 10);
            this.cb_changemode.Name = "cb_changemode";
            this.cb_changemode.Size = new System.Drawing.Size(297, 30);
            this.cb_changemode.TabIndex = 6;
            this.cb_changemode.UseSelectable = true;
            this.cb_changemode.SelectedIndexChanged += new System.EventHandler(this.cb_changemode_SelectedIndexChanged);
            // 
            // btn_togglebleed
            // 
            this.btn_togglebleed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_togglebleed.AutoSize = true;
            this.btn_togglebleed.Location = new System.Drawing.Point(763, 3);
            this.btn_togglebleed.Margin = new System.Windows.Forms.Padding(3, 3, 20, 3);
            this.btn_togglebleed.Name = "btn_togglebleed";
            this.btn_togglebleed.Size = new System.Drawing.Size(120, 44);
            this.btn_togglebleed.TabIndex = 4;
            this.btn_togglebleed.Text = "Bleed Disabled";
            this.btn_togglebleed.UseCustomBackColor = true;
            this.btn_togglebleed.UseCustomForeColor = true;
            this.btn_togglebleed.UseSelectable = true;
            this.btn_togglebleed.Click += new System.EventHandler(this.btn_togglebleed_Click);
            // 
            // pn_top
            // 
            this.pn_top.Controls.Add(this.tlp_top);
            this.pn_top.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pn_top.Location = new System.Drawing.Point(3, 3);
            this.pn_top.Name = "pn_top";
            this.pn_top.Size = new System.Drawing.Size(1864, 46);
            this.pn_top.TabIndex = 1;
            // 
            // tlp_top
            // 
            this.tlp_top.ColumnCount = 6;
            this.tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58.79828F));
            this.tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.349785F));
            this.tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.38412F));
            this.tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.403433F));
            this.tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13F));
            this.tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_top.Controls.Add(this.btn_export, 0, 0);
            this.tlp_top.Controls.Add(this.btn_import, 0, 0);
            this.tlp_top.Controls.Add(this.cb_addlayer, 4, 0);
            this.tlp_top.Controls.Add(this.btn_addlayer, 5, 0);
            this.tlp_top.Controls.Add(this.cb_deviceselect, 0, 0);
            this.tlp_top.Controls.Add(this.btn_preview, 3, 0);
            this.tlp_top.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_top.Location = new System.Drawing.Point(0, 0);
            this.tlp_top.Name = "tlp_top";
            this.tlp_top.RowCount = 1;
            this.tlp_top.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_top.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_top.Size = new System.Drawing.Size(1864, 46);
            this.tlp_top.TabIndex = 0;
            // 
            // btn_export
            // 
            this.btn_export.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_export.AutoSize = true;
            this.btn_export.Location = new System.Drawing.Point(1236, 3);
            this.btn_export.Margin = new System.Windows.Forms.Padding(3, 3, 20, 3);
            this.btn_export.Name = "btn_export";
            this.btn_export.Size = new System.Drawing.Size(96, 40);
            this.btn_export.TabIndex = 5;
            this.btn_export.Text = "Export";
            this.btn_export.UseCustomBackColor = true;
            this.btn_export.UseCustomForeColor = true;
            this.btn_export.UseSelectable = true;
            this.btn_export.Click += new System.EventHandler(this.btn_export_Click);
            // 
            // btn_import
            // 
            this.btn_import.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_import.AutoSize = true;
            this.btn_import.Location = new System.Drawing.Point(1117, 3);
            this.btn_import.Margin = new System.Windows.Forms.Padding(3, 3, 20, 3);
            this.btn_import.Name = "btn_import";
            this.btn_import.Size = new System.Drawing.Size(96, 40);
            this.btn_import.TabIndex = 4;
            this.btn_import.Text = "Import";
            this.btn_import.UseCustomBackColor = true;
            this.btn_import.UseCustomForeColor = true;
            this.btn_import.UseSelectable = true;
            this.btn_import.Click += new System.EventHandler(this.btn_import_Click);
            // 
            // cb_addlayer
            // 
            this.cb_addlayer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cb_addlayer.FormattingEnabled = true;
            this.cb_addlayer.ItemHeight = 24;
            this.cb_addlayer.Location = new System.Drawing.Point(1493, 13);
            this.cb_addlayer.Name = "cb_addlayer";
            this.cb_addlayer.Size = new System.Drawing.Size(236, 30);
            this.cb_addlayer.TabIndex = 0;
            this.cb_addlayer.UseSelectable = true;
            this.cb_addlayer.SelectedIndexChanged += new System.EventHandler(this.cb_addlayer_SelectedIndexChanged);
            // 
            // btn_addlayer
            // 
            this.btn_addlayer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_addlayer.Location = new System.Drawing.Point(1735, 3);
            this.btn_addlayer.Name = "btn_addlayer";
            this.btn_addlayer.Size = new System.Drawing.Size(94, 40);
            this.btn_addlayer.TabIndex = 1;
            this.btn_addlayer.Text = "Add Layer";
            this.btn_addlayer.UseCustomBackColor = true;
            this.btn_addlayer.UseCustomForeColor = true;
            this.btn_addlayer.UseSelectable = true;
            this.btn_addlayer.Click += new System.EventHandler(this.btn_addlayer_Click);
            // 
            // cb_deviceselect
            // 
            this.cb_deviceselect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cb_deviceselect.FormattingEnabled = true;
            this.cb_deviceselect.ItemHeight = 24;
            this.cb_deviceselect.Location = new System.Drawing.Point(3, 13);
            this.cb_deviceselect.Name = "cb_deviceselect";
            this.cb_deviceselect.Size = new System.Drawing.Size(236, 30);
            this.cb_deviceselect.TabIndex = 2;
            this.cb_deviceselect.UseSelectable = true;
            this.cb_deviceselect.SelectedIndexChanged += new System.EventHandler(this.cb_deviceselect_SelectedIndexChanged);
            // 
            // btn_preview
            // 
            this.btn_preview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_preview.AutoSize = true;
            this.btn_preview.Location = new System.Drawing.Point(1373, 3);
            this.btn_preview.Margin = new System.Windows.Forms.Padding(3, 3, 20, 3);
            this.btn_preview.Name = "btn_preview";
            this.btn_preview.Size = new System.Drawing.Size(97, 40);
            this.btn_preview.TabIndex = 3;
            this.btn_preview.Text = "Preview";
            this.btn_preview.UseCustomBackColor = true;
            this.btn_preview.UseCustomForeColor = true;
            this.btn_preview.UseSelectable = true;
            this.btn_preview.Click += new System.EventHandler(this.btn_preview_Click);
            // 
            // pn_bottom
            // 
            this.pn_bottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pn_bottom.Location = new System.Drawing.Point(3, 991);
            this.pn_bottom.Name = "pn_bottom";
            this.pn_bottom.Size = new System.Drawing.Size(1864, 46);
            this.pn_bottom.TabIndex = 2;
            // 
            // Uc_Mappings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.tlp_base);
            this.Name = "Uc_Mappings";
            this.Size = new System.Drawing.Size(2065, 1163);
            this.Load += new System.EventHandler(this.OnLoad);
            this.Resize += new System.EventHandler(this.OnResize);
            this.tlp_base.ResumeLayout(false);
            this.tlp_mid.ResumeLayout(false);
            this.pn_right.ResumeLayout(false);
            this.tlp_frame.ResumeLayout(false);
            this.tlp_controls.ResumeLayout(false);
            this.tlp_controls.PerformLayout();
            this.tlp_layercontrols.ResumeLayout(false);
            this.tlp_layercontrols.PerformLayout();
            this.pn_top.ResumeLayout(false);
            this.tlp_top.ResumeLayout(false);
            this.tlp_top.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlp_base;
        private System.Windows.Forms.TableLayoutPanel tlp_mid;
        private System.Windows.Forms.Panel pn_right;
        private System.Windows.Forms.Panel pn_top;
        private System.Windows.Forms.Panel pn_bottom;
        private System.Windows.Forms.FlowLayoutPanel flp_layers;
        private System.Windows.Forms.TableLayoutPanel tlp_top;
        private MetroFramework.Controls.MetroComboBox cb_addlayer;
        private MetroFramework.Controls.MetroButton btn_addlayer;
        private MetroFramework.Controls.MetroComboBox cb_deviceselect;
        private System.Windows.Forms.TableLayoutPanel tlp_frame;
        private System.Windows.Forms.TableLayoutPanel tlp_controls;
        private MetroFramework.Controls.MetroButton btn_clearselection;
        private MetroFramework.Controls.MetroButton btn_reverseselection;
        private MetroFramework.Controls.MetroButton btn_preview;
        private MetroFramework.Controls.MetroButton btn_undoselection;
        private MetroFramework.Controls.MetroButton btn_export;
        private MetroFramework.Controls.MetroButton btn_import;
        private System.Windows.Forms.TableLayoutPanel tlp_layercontrols;
        private MetroFramework.Controls.MetroComboBox cb_changemode;
        private MetroFramework.Controls.MetroButton btn_togglebleed;
    }
}
