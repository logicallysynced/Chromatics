
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
            tlp_base = new System.Windows.Forms.TableLayoutPanel();
            tlp_mid = new System.Windows.Forms.TableLayoutPanel();
            pn_right = new System.Windows.Forms.Panel();
            flp_layers = new System.Windows.Forms.FlowLayoutPanel();
            tlp_frame = new System.Windows.Forms.TableLayoutPanel();
            tlp_controls = new System.Windows.Forms.TableLayoutPanel();
            btn_clearselection = new MetroFramework.Controls.MetroButton();
            btn_reverseselection = new MetroFramework.Controls.MetroButton();
            btn_undoselection = new MetroFramework.Controls.MetroButton();
            tlp_layercontrols = new System.Windows.Forms.TableLayoutPanel();
            cb_changemode = new MetroFramework.Controls.MetroComboBox();
            btn_togglebleed = new MetroFramework.Controls.MetroButton();
            rtb_layerhelper = new System.Windows.Forms.RichTextBox();
            pn_top = new System.Windows.Forms.Panel();
            tlp_top = new System.Windows.Forms.TableLayoutPanel();
            btn_export = new MetroFramework.Controls.MetroButton();
            btn_import = new MetroFramework.Controls.MetroButton();
            cb_addlayer = new MetroFramework.Controls.MetroComboBox();
            btn_addlayer = new MetroFramework.Controls.MetroButton();
            cb_deviceselect = new MetroFramework.Controls.MetroComboBox();
            btn_preview = new MetroFramework.Controls.MetroButton();
            pn_bottom = new System.Windows.Forms.Panel();
            tlp_footer = new System.Windows.Forms.TableLayoutPanel();
            rtb_layerhelp = new System.Windows.Forms.RichTextBox();
            tlp_base.SuspendLayout();
            tlp_mid.SuspendLayout();
            pn_right.SuspendLayout();
            tlp_frame.SuspendLayout();
            tlp_controls.SuspendLayout();
            tlp_layercontrols.SuspendLayout();
            pn_top.SuspendLayout();
            tlp_top.SuspendLayout();
            pn_bottom.SuspendLayout();
            tlp_footer.SuspendLayout();
            SuspendLayout();
            // 
            // tlp_base
            // 
            tlp_base.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tlp_base.AutoSize = true;
            tlp_base.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            tlp_base.BackColor = SystemColors.Control;
            tlp_base.ColumnCount = 1;
            tlp_base.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_base.Controls.Add(tlp_mid, 0, 1);
            tlp_base.Controls.Add(pn_top, 0, 0);
            tlp_base.Controls.Add(pn_bottom, 0, 2);
            tlp_base.Location = new Point(0, 0);
            tlp_base.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_base.Name = "tlp_base";
            tlp_base.RowCount = 3;
            tlp_base.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            tlp_base.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90F));
            tlp_base.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            tlp_base.Size = new Size(2338, 1300);
            tlp_base.TabIndex = 0;
            // 
            // tlp_mid
            // 
            tlp_mid.ColumnCount = 2;
            tlp_mid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            tlp_mid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            tlp_mid.Controls.Add(pn_right, 1, 0);
            tlp_mid.Controls.Add(tlp_frame, 0, 0);
            tlp_mid.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_mid.Location = new Point(4, 69);
            tlp_mid.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_mid.Name = "tlp_mid";
            tlp_mid.RowCount = 1;
            tlp_mid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_mid.Size = new Size(2330, 1162);
            tlp_mid.TabIndex = 0;
            // 
            // pn_right
            // 
            pn_right.Controls.Add(flp_layers);
            pn_right.Dock = System.Windows.Forms.DockStyle.Fill;
            pn_right.Location = new Point(1635, 4);
            pn_right.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            pn_right.Name = "pn_right";
            pn_right.Size = new Size(691, 1154);
            pn_right.TabIndex = 1;
            // 
            // flp_layers
            // 
            flp_layers.AllowDrop = true;
            flp_layers.AutoScroll = true;
            flp_layers.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            flp_layers.Dock = System.Windows.Forms.DockStyle.Fill;
            flp_layers.Location = new Point(0, 0);
            flp_layers.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            flp_layers.Name = "flp_layers";
            flp_layers.Size = new Size(691, 1154);
            flp_layers.TabIndex = 3;
            // 
            // tlp_frame
            // 
            tlp_frame.ColumnCount = 2;
            tlp_frame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 93.37442F));
            tlp_frame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.625578F));
            tlp_frame.Controls.Add(tlp_controls, 1, 0);
            tlp_frame.Controls.Add(tlp_layercontrols, 0, 1);
            tlp_frame.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_frame.Location = new Point(4, 4);
            tlp_frame.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_frame.Name = "tlp_frame";
            tlp_frame.RowCount = 3;
            tlp_frame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            tlp_frame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8F));
            tlp_frame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 52F));
            tlp_frame.Size = new Size(1623, 1154);
            tlp_frame.TabIndex = 2;
            // 
            // tlp_controls
            // 
            tlp_controls.ColumnCount = 1;
            tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            tlp_controls.Controls.Add(btn_clearselection, 0, 0);
            tlp_controls.Controls.Add(btn_reverseselection, 0, 1);
            tlp_controls.Controls.Add(btn_undoselection, 0, 2);
            tlp_controls.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_controls.Location = new Point(1519, 4);
            tlp_controls.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_controls.Name = "tlp_controls";
            tlp_controls.RowCount = 4;
            tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlp_controls.Size = new Size(100, 453);
            tlp_controls.TabIndex = 0;
            // 
            // btn_clearselection
            // 
            btn_clearselection.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btn_clearselection.AutoSize = true;
            btn_clearselection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            btn_clearselection.Enabled = false;
            btn_clearselection.Location = new Point(4, 4);
            btn_clearselection.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            btn_clearselection.Name = "btn_clearselection";
            btn_clearselection.Size = new Size(92, 35);
            btn_clearselection.TabIndex = 0;
            btn_clearselection.Text = "Clear";
            btn_clearselection.UseSelectable = true;
            btn_clearselection.Click += btn_clearselection_Click;
            // 
            // btn_reverseselection
            // 
            btn_reverseselection.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btn_reverseselection.AutoSize = true;
            btn_reverseselection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            btn_reverseselection.Enabled = false;
            btn_reverseselection.Location = new Point(4, 117);
            btn_reverseselection.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            btn_reverseselection.Name = "btn_reverseselection";
            btn_reverseselection.Size = new Size(92, 35);
            btn_reverseselection.TabIndex = 1;
            btn_reverseselection.Text = "Reverse";
            btn_reverseselection.UseSelectable = true;
            btn_reverseselection.Click += btn_reverseselection_Click;
            // 
            // btn_undoselection
            // 
            btn_undoselection.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btn_undoselection.AutoSize = true;
            btn_undoselection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            btn_undoselection.Location = new Point(4, 230);
            btn_undoselection.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            btn_undoselection.Name = "btn_undoselection";
            btn_undoselection.Size = new Size(92, 35);
            btn_undoselection.TabIndex = 2;
            btn_undoselection.Text = "Undo";
            btn_undoselection.UseSelectable = true;
            btn_undoselection.Click += btn_undoselection_Click;
            // 
            // tlp_layercontrols
            // 
            tlp_layercontrols.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tlp_layercontrols.ColumnCount = 5;
            tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 1F));
            tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 4F));
            tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            tlp_layercontrols.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlp_layercontrols.Controls.Add(cb_changemode, 4, 0);
            tlp_layercontrols.Controls.Add(btn_togglebleed, 3, 0);
            tlp_layercontrols.Controls.Add(rtb_layerhelper, 1, 0);
            tlp_layercontrols.Font = new Font("Segoe UI", 10.2F);
            tlp_layercontrols.Location = new Point(4, 465);
            tlp_layercontrols.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_layercontrols.Name = "tlp_layercontrols";
            tlp_layercontrols.RowCount = 1;
            tlp_layercontrols.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_layercontrols.Size = new Size(1507, 84);
            tlp_layercontrols.TabIndex = 1;
            // 
            // cb_changemode
            // 
            cb_changemode.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            cb_changemode.FormattingEnabled = true;
            cb_changemode.ItemHeight = 23;
            cb_changemode.Location = new Point(1133, 27);
            cb_changemode.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            cb_changemode.Name = "cb_changemode";
            cb_changemode.Size = new Size(370, 29);
            cb_changemode.TabIndex = 6;
            cb_changemode.UseSelectable = true;
            cb_changemode.SelectedIndexChanged += cb_changemode_SelectedIndexChanged;
            // 
            // btn_togglebleed
            // 
            btn_togglebleed.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btn_togglebleed.AutoSize = true;
            btn_togglebleed.Location = new Point(908, 4);
            btn_togglebleed.Margin = new System.Windows.Forms.Padding(4, 4, 25, 4);
            btn_togglebleed.Name = "btn_togglebleed";
            btn_togglebleed.Size = new Size(196, 76);
            btn_togglebleed.TabIndex = 4;
            btn_togglebleed.Text = "Bleed Disabled";
            btn_togglebleed.UseCustomBackColor = true;
            btn_togglebleed.UseCustomForeColor = true;
            btn_togglebleed.UseSelectable = true;
            btn_togglebleed.Click += btn_togglebleed_Click;
            // 
            // rtb_layerhelper
            // 
            rtb_layerhelper.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rtb_layerhelper.BackColor = SystemColors.Control;
            rtb_layerhelper.BorderStyle = System.Windows.Forms.BorderStyle.None;
            rtb_layerhelper.Location = new Point(19, 4);
            rtb_layerhelper.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            rtb_layerhelper.Name = "rtb_layerhelper";
            rtb_layerhelper.ReadOnly = true;
            rtb_layerhelper.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            rtb_layerhelper.Size = new Size(820, 76);
            rtb_layerhelper.TabIndex = 7;
            rtb_layerhelper.Text = "";
            rtb_layerhelper.TextChanged += rtb_layerhelper_TextChanged;
            // 
            // pn_top
            // 
            pn_top.Controls.Add(tlp_top);
            pn_top.Dock = System.Windows.Forms.DockStyle.Fill;
            pn_top.Location = new Point(4, 4);
            pn_top.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            pn_top.Name = "pn_top";
            pn_top.Size = new Size(2330, 57);
            pn_top.TabIndex = 1;
            // 
            // tlp_top
            // 
            tlp_top.ColumnCount = 6;
            tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58.79828F));
            tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.349785F));
            tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.38412F));
            tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.403433F));
            tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13F));
            tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7F));
            tlp_top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            tlp_top.Controls.Add(btn_export, 0, 0);
            tlp_top.Controls.Add(btn_import, 0, 0);
            tlp_top.Controls.Add(cb_addlayer, 4, 0);
            tlp_top.Controls.Add(btn_addlayer, 5, 0);
            tlp_top.Controls.Add(cb_deviceselect, 0, 0);
            tlp_top.Controls.Add(btn_preview, 3, 0);
            tlp_top.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_top.Location = new Point(0, 0);
            tlp_top.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_top.Name = "tlp_top";
            tlp_top.RowCount = 1;
            tlp_top.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_top.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            tlp_top.Size = new Size(2330, 57);
            tlp_top.TabIndex = 0;
            // 
            // btn_export
            // 
            btn_export.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btn_export.AutoSize = true;
            btn_export.Location = new Point(1545, 4);
            btn_export.Margin = new System.Windows.Forms.Padding(4, 4, 25, 4);
            btn_export.Name = "btn_export";
            btn_export.Size = new Size(119, 49);
            btn_export.TabIndex = 5;
            btn_export.Text = "Export";
            btn_export.UseCustomBackColor = true;
            btn_export.UseCustomForeColor = true;
            btn_export.UseSelectable = true;
            btn_export.Click += btn_export_Click;
            // 
            // btn_import
            // 
            btn_import.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btn_import.AutoSize = true;
            btn_import.Location = new Point(1396, 4);
            btn_import.Margin = new System.Windows.Forms.Padding(4, 4, 25, 4);
            btn_import.Name = "btn_import";
            btn_import.Size = new Size(120, 49);
            btn_import.TabIndex = 4;
            btn_import.Text = "Import";
            btn_import.UseCustomBackColor = true;
            btn_import.UseCustomForeColor = true;
            btn_import.UseSelectable = true;
            btn_import.Click += btn_import_Click;
            // 
            // cb_addlayer
            // 
            cb_addlayer.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            cb_addlayer.FormattingEnabled = true;
            cb_addlayer.ItemHeight = 23;
            cb_addlayer.Location = new Point(1865, 23);
            cb_addlayer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            cb_addlayer.Name = "cb_addlayer";
            cb_addlayer.Size = new Size(294, 29);
            cb_addlayer.TabIndex = 0;
            cb_addlayer.UseSelectable = true;
            cb_addlayer.SelectedIndexChanged += cb_addlayer_SelectedIndexChanged;
            // 
            // btn_addlayer
            // 
            btn_addlayer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btn_addlayer.Location = new Point(2168, 4);
            btn_addlayer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            btn_addlayer.Name = "btn_addlayer";
            btn_addlayer.Size = new Size(118, 49);
            btn_addlayer.TabIndex = 1;
            btn_addlayer.Text = "Add Layer";
            btn_addlayer.UseCustomBackColor = true;
            btn_addlayer.UseCustomForeColor = true;
            btn_addlayer.UseSelectable = true;
            btn_addlayer.Click += btn_addlayer_Click;
            // 
            // cb_deviceselect
            // 
            cb_deviceselect.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            cb_deviceselect.FormattingEnabled = true;
            cb_deviceselect.ItemHeight = 23;
            cb_deviceselect.Location = new Point(4, 23);
            cb_deviceselect.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            cb_deviceselect.Name = "cb_deviceselect";
            cb_deviceselect.Size = new Size(488, 29);
            cb_deviceselect.TabIndex = 2;
            cb_deviceselect.UseSelectable = true;
            cb_deviceselect.SelectedIndexChanged += cb_deviceselect_SelectedIndexChanged;
            // 
            // btn_preview
            // 
            btn_preview.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btn_preview.AutoSize = true;
            btn_preview.Location = new Point(1715, 4);
            btn_preview.Margin = new System.Windows.Forms.Padding(4, 4, 25, 4);
            btn_preview.Name = "btn_preview";
            btn_preview.Size = new Size(121, 49);
            btn_preview.TabIndex = 3;
            btn_preview.Text = "Preview";
            btn_preview.UseCustomBackColor = true;
            btn_preview.UseCustomForeColor = true;
            btn_preview.UseSelectable = true;
            btn_preview.Click += btn_preview_Click;
            // 
            // pn_bottom
            // 
            pn_bottom.Controls.Add(tlp_footer);
            pn_bottom.Dock = System.Windows.Forms.DockStyle.Fill;
            pn_bottom.Location = new Point(4, 1239);
            pn_bottom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            pn_bottom.Name = "pn_bottom";
            pn_bottom.Size = new Size(2330, 57);
            pn_bottom.TabIndex = 2;
            // 
            // tlp_footer
            // 
            tlp_footer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tlp_footer.ColumnCount = 2;
            tlp_footer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            tlp_footer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            tlp_footer.Controls.Add(rtb_layerhelp, 1, 0);
            tlp_footer.Location = new Point(-4, 4);
            tlp_footer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_footer.Name = "tlp_footer";
            tlp_footer.RowCount = 1;
            tlp_footer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_footer.Size = new Size(2330, 53);
            tlp_footer.TabIndex = 3;
            // 
            // rtb_layerhelp
            // 
            rtb_layerhelp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rtb_layerhelp.BackColor = SystemColors.Control;
            rtb_layerhelp.BorderStyle = System.Windows.Forms.BorderStyle.None;
            rtb_layerhelp.Location = new Point(1635, 4);
            rtb_layerhelp.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            rtb_layerhelp.Name = "rtb_layerhelp";
            rtb_layerhelp.ReadOnly = true;
            rtb_layerhelp.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            rtb_layerhelp.Size = new Size(691, 45);
            rtb_layerhelp.TabIndex = 8;
            rtb_layerhelp.Text = "Hold Shift to copy layers";
            // 
            // Uc_Mappings
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            Controls.Add(tlp_base);
            Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            Name = "Uc_Mappings";
            Size = new Size(2581, 1454);
            Load += OnLoad;
            Resize += OnResize;
            tlp_base.ResumeLayout(false);
            tlp_mid.ResumeLayout(false);
            pn_right.ResumeLayout(false);
            tlp_frame.ResumeLayout(false);
            tlp_controls.ResumeLayout(false);
            tlp_controls.PerformLayout();
            tlp_layercontrols.ResumeLayout(false);
            tlp_layercontrols.PerformLayout();
            pn_top.ResumeLayout(false);
            tlp_top.ResumeLayout(false);
            tlp_top.PerformLayout();
            pn_bottom.ResumeLayout(false);
            tlp_footer.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.RichTextBox rtb_layerhelper;
        private System.Windows.Forms.TableLayoutPanel tlp_footer;
        private System.Windows.Forms.RichTextBox rtb_layerhelp;
    }
}
