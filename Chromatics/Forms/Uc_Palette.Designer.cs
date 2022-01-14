using MetroFramework.Controls;

namespace Chromatics.Forms
{
    partial class Uc_Palette
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
            this.tlp_palette = new System.Windows.Forms.TableLayoutPanel();
            this.tlp_palette_inner = new System.Windows.Forms.TableLayoutPanel();
            this.palette_picker = new Cyotek.Windows.Forms.ScreenColorPicker();
            this.palette_preview = new System.Windows.Forms.Panel();
            this.palette_wheel = new Cyotek.Windows.Forms.ColorWheel();
            this.palette_grid = new Cyotek.Windows.Forms.ColorGrid();
            this.palette_editor = new Cyotek.Windows.Forms.ColorEditor();
            this.tlp_mappings = new System.Windows.Forms.TableLayoutPanel();
            this.dG_mappings = new System.Windows.Forms.DataGridView();
            this.mappings_col_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mappings_col_cat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mappings_col_type = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mappings_col_color = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tlp_controls = new System.Windows.Forms.TableLayoutPanel();
            this.btn_paletteexport = new System.Windows.Forms.Button();
            this.btn_paletteundo = new System.Windows.Forms.Button();
            this.btn_paletteimport = new System.Windows.Forms.Button();
            this.lbl_palettecat = new System.Windows.Forms.Label();
            this.cb_palette_categories = new MetroFramework.Controls.MetroComboBox();
            this.palette_colormanager = new Cyotek.Windows.Forms.ColorEditorManager();
            this.tlp_main.SuspendLayout();
            this.tlp_palette.SuspendLayout();
            this.tlp_palette_inner.SuspendLayout();
            this.tlp_mappings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dG_mappings)).BeginInit();
            this.tlp_controls.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlp_main
            // 
            this.tlp_main.ColumnCount = 2;
            this.tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.54481F));
            this.tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.45519F));
            this.tlp_main.Controls.Add(this.tlp_palette, 1, 0);
            this.tlp_main.Controls.Add(this.tlp_mappings, 0, 0);
            this.tlp_main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_main.Location = new System.Drawing.Point(0, 0);
            this.tlp_main.Name = "tlp_main";
            this.tlp_main.RowCount = 1;
            this.tlp_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_main.Size = new System.Drawing.Size(1261, 725);
            this.tlp_main.TabIndex = 0;
            // 
            // tlp_palette
            // 
            this.tlp_palette.ColumnCount = 2;
            this.tlp_palette.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 51.92308F));
            this.tlp_palette.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48.07692F));
            this.tlp_palette.Controls.Add(this.tlp_palette_inner, 1, 1);
            this.tlp_palette.Controls.Add(this.palette_wheel, 0, 0);
            this.tlp_palette.Controls.Add(this.palette_grid, 0, 1);
            this.tlp_palette.Controls.Add(this.palette_editor, 1, 0);
            this.tlp_palette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_palette.Location = new System.Drawing.Point(426, 3);
            this.tlp_palette.Name = "tlp_palette";
            this.tlp_palette.RowCount = 2;
            this.tlp_palette.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 61.19611F));
            this.tlp_palette.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38.80389F));
            this.tlp_palette.Size = new System.Drawing.Size(832, 719);
            this.tlp_palette.TabIndex = 0;
            // 
            // tlp_palette_inner
            // 
            this.tlp_palette_inner.ColumnCount = 1;
            this.tlp_palette_inner.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_palette_inner.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_palette_inner.Controls.Add(this.palette_picker, 0, 0);
            this.tlp_palette_inner.Controls.Add(this.palette_preview, 0, 1);
            this.tlp_palette_inner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_palette_inner.Location = new System.Drawing.Point(435, 443);
            this.tlp_palette_inner.Name = "tlp_palette_inner";
            this.tlp_palette_inner.RowCount = 2;
            this.tlp_palette_inner.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 74.35897F));
            this.tlp_palette_inner.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.64103F));
            this.tlp_palette_inner.Size = new System.Drawing.Size(394, 273);
            this.tlp_palette_inner.TabIndex = 3;
            // 
            // palette_picker
            // 
            this.palette_picker.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.palette_picker.Color = System.Drawing.Color.Empty;
            this.palette_picker.Image = global::Chromatics.Properties.Resources.eyedropper;
            this.palette_picker.Location = new System.Drawing.Point(3, 3);
            this.palette_picker.Name = "palette_picker";
            this.palette_picker.Size = new System.Drawing.Size(388, 196);
            // 
            // palette_preview
            // 
            this.palette_preview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.palette_preview.Location = new System.Drawing.Point(3, 205);
            this.palette_preview.Name = "palette_preview";
            this.palette_preview.Size = new System.Drawing.Size(388, 65);
            this.palette_preview.TabIndex = 1;
            // 
            // palette_wheel
            // 
            this.palette_wheel.Alpha = 1D;
            this.palette_wheel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.palette_wheel.Location = new System.Drawing.Point(3, 3);
            this.palette_wheel.Name = "palette_wheel";
            this.palette_wheel.Size = new System.Drawing.Size(426, 434);
            this.palette_wheel.TabIndex = 4;
            // 
            // palette_grid
            // 
            this.palette_grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.palette_grid.Location = new System.Drawing.Point(3, 443);
            this.palette_grid.Name = "palette_grid";
            this.palette_grid.Size = new System.Drawing.Size(426, 273);
            this.palette_grid.TabIndex = 5;
            // 
            // palette_editor
            // 
            this.palette_editor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.palette_editor.AutoSize = true;
            this.palette_editor.Location = new System.Drawing.Point(436, 5);
            this.palette_editor.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.palette_editor.Name = "palette_editor";
            this.palette_editor.Size = new System.Drawing.Size(392, 430);
            this.palette_editor.TabIndex = 6;
            // 
            // tlp_mappings
            // 
            this.tlp_mappings.ColumnCount = 1;
            this.tlp_mappings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_mappings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlp_mappings.Controls.Add(this.dG_mappings, 0, 0);
            this.tlp_mappings.Controls.Add(this.tlp_controls, 0, 1);
            this.tlp_mappings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_mappings.Location = new System.Drawing.Point(3, 3);
            this.tlp_mappings.Name = "tlp_mappings";
            this.tlp_mappings.RowCount = 2;
            this.tlp_mappings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 92.90681F));
            this.tlp_mappings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7.093185F));
            this.tlp_mappings.Size = new System.Drawing.Size(417, 719);
            this.tlp_mappings.TabIndex = 1;
            // 
            // dG_mappings
            // 
            this.dG_mappings.AllowUserToDeleteRows = false;
            this.dG_mappings.AllowUserToResizeColumns = false;
            this.dG_mappings.AllowUserToResizeRows = false;
            this.dG_mappings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dG_mappings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dG_mappings.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            this.dG_mappings.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
            this.dG_mappings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dG_mappings.ColumnHeadersVisible = false;
            this.dG_mappings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.mappings_col_id,
            this.mappings_col_cat,
            this.mappings_col_type,
            this.mappings_col_color});
            this.dG_mappings.Location = new System.Drawing.Point(3, 2);
            this.dG_mappings.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.dG_mappings.MultiSelect = false;
            this.dG_mappings.Name = "dG_mappings";
            this.dG_mappings.RowHeadersVisible = false;
            this.dG_mappings.RowHeadersWidth = 51;
            this.dG_mappings.RowTemplate.Height = 29;
            this.dG_mappings.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dG_mappings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dG_mappings.ShowCellErrors = false;
            this.dG_mappings.Size = new System.Drawing.Size(411, 664);
            this.dG_mappings.TabIndex = 1;
            this.dG_mappings.RowPrePaint += new System.Windows.Forms.DataGridViewRowPrePaintEventHandler(this.dG_mappings_RowPrePaint);
            this.dG_mappings.SelectionChanged += new System.EventHandler(this.dG_mappings_SelectionChanged);
            // 
            // mappings_col_id
            // 
            this.mappings_col_id.HeaderText = "ID";
            this.mappings_col_id.MinimumWidth = 6;
            this.mappings_col_id.Name = "mappings_col_id";
            this.mappings_col_id.ReadOnly = true;
            this.mappings_col_id.Visible = false;
            // 
            // mappings_col_cat
            // 
            this.mappings_col_cat.HeaderText = "Category";
            this.mappings_col_cat.MinimumWidth = 6;
            this.mappings_col_cat.Name = "mappings_col_cat";
            this.mappings_col_cat.ReadOnly = true;
            this.mappings_col_cat.Visible = false;
            // 
            // mappings_col_type
            // 
            this.mappings_col_type.HeaderText = "Type";
            this.mappings_col_type.MinimumWidth = 6;
            this.mappings_col_type.Name = "mappings_col_type";
            this.mappings_col_type.ReadOnly = true;
            // 
            // mappings_col_color
            // 
            this.mappings_col_color.HeaderText = "Color";
            this.mappings_col_color.MinimumWidth = 6;
            this.mappings_col_color.Name = "mappings_col_color";
            this.mappings_col_color.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.mappings_col_color.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // tlp_controls
            // 
            this.tlp_controls.ColumnCount = 5;
            this.tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19.81132F));
            this.tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 37.73584F));
            this.tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.15094F));
            this.tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.15094F));
            this.tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.15094F));
            this.tlp_controls.Controls.Add(this.btn_paletteexport, 3, 0);
            this.tlp_controls.Controls.Add(this.btn_paletteundo, 4, 0);
            this.tlp_controls.Controls.Add(this.btn_paletteimport, 2, 0);
            this.tlp_controls.Controls.Add(this.lbl_palettecat, 0, 0);
            this.tlp_controls.Controls.Add(this.cb_palette_categories, 1, 0);
            this.tlp_controls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlp_controls.Location = new System.Drawing.Point(3, 671);
            this.tlp_controls.Name = "tlp_controls";
            this.tlp_controls.RowCount = 1;
            this.tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlp_controls.Size = new System.Drawing.Size(411, 45);
            this.tlp_controls.TabIndex = 2;
            // 
            // btn_paletteexport
            // 
            this.btn_paletteexport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_paletteexport.AutoSize = true;
            this.btn_paletteexport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_paletteexport.Image = global::Chromatics.Properties.Resources.palette_save;
            this.btn_paletteexport.Location = new System.Drawing.Point(297, 3);
            this.btn_paletteexport.Name = "btn_paletteexport";
            this.btn_paletteexport.Size = new System.Drawing.Size(52, 39);
            this.btn_paletteexport.TabIndex = 0;
            this.btn_paletteexport.UseVisualStyleBackColor = true;
            this.btn_paletteexport.Click += new System.EventHandler(this.btn_paletteexport_Click);
            // 
            // btn_paletteundo
            // 
            this.btn_paletteundo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_paletteundo.AutoSize = true;
            this.btn_paletteundo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_paletteundo.Image = global::Chromatics.Properties.Resources.ret_arrow;
            this.btn_paletteundo.Location = new System.Drawing.Point(355, 3);
            this.btn_paletteundo.Name = "btn_paletteundo";
            this.btn_paletteundo.Size = new System.Drawing.Size(53, 39);
            this.btn_paletteundo.TabIndex = 2;
            this.btn_paletteundo.UseVisualStyleBackColor = true;
            this.btn_paletteundo.Click += new System.EventHandler(this.btn_paletteundo_Click);
            // 
            // btn_paletteimport
            // 
            this.btn_paletteimport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_paletteimport.AutoSize = true;
            this.btn_paletteimport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_paletteimport.Image = global::Chromatics.Properties.Resources.palette_load;
            this.btn_paletteimport.Location = new System.Drawing.Point(239, 3);
            this.btn_paletteimport.Name = "btn_paletteimport";
            this.btn_paletteimport.Size = new System.Drawing.Size(52, 39);
            this.btn_paletteimport.TabIndex = 1;
            this.btn_paletteimport.UseVisualStyleBackColor = true;
            this.btn_paletteimport.Click += new System.EventHandler(this.btn_paletteimport_Click);
            // 
            // lbl_palettecat
            // 
            this.lbl_palettecat.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_palettecat.AutoSize = true;
            this.lbl_palettecat.Location = new System.Drawing.Point(3, 0);
            this.lbl_palettecat.Name = "lbl_palettecat";
            this.lbl_palettecat.Size = new System.Drawing.Size(75, 45);
            this.lbl_palettecat.TabIndex = 4;
            this.lbl_palettecat.Text = "Category:";
            this.lbl_palettecat.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cb_palette_categories
            // 
            this.cb_palette_categories.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cb_palette_categories.FormattingEnabled = true;
            this.cb_palette_categories.ItemHeight = 24;
            this.cb_palette_categories.Location = new System.Drawing.Point(84, 3);
            this.cb_palette_categories.Name = "cb_palette_categories";
            this.cb_palette_categories.Size = new System.Drawing.Size(149, 30);
            this.cb_palette_categories.TabIndex = 3;
            this.cb_palette_categories.UseSelectable = true;
            this.cb_palette_categories.SelectedIndexChanged += new System.EventHandler(this.cb_palette_categories_SelectedIndexChanged);
            // 
            // palette_colormanager
            // 
            this.palette_colormanager.Color = System.Drawing.Color.Empty;
            this.palette_colormanager.ColorEditor = this.palette_editor;
            this.palette_colormanager.ColorGrid = this.palette_grid;
            this.palette_colormanager.ColorWheel = this.palette_wheel;
            this.palette_colormanager.ScreenColorPicker = this.palette_picker;
            // 
            // Uc_Palette
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tlp_main);
            this.Name = "Uc_Palette";
            this.Size = new System.Drawing.Size(1261, 725);
            this.Load += new System.EventHandler(this.OnLoad);
            this.tlp_main.ResumeLayout(false);
            this.tlp_palette.ResumeLayout(false);
            this.tlp_palette.PerformLayout();
            this.tlp_palette_inner.ResumeLayout(false);
            this.tlp_mappings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dG_mappings)).EndInit();
            this.tlp_controls.ResumeLayout(false);
            this.tlp_controls.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlp_main;
        private System.Windows.Forms.TableLayoutPanel tlp_palette;
        private System.Windows.Forms.TableLayoutPanel tlp_palette_inner;
        private System.Windows.Forms.TableLayoutPanel tlp_mappings;
        private System.Windows.Forms.Button btn_paletteundo;
        private System.Windows.Forms.Button btn_paletteimport;
        private System.Windows.Forms.Button btn_paletteexport;
        private MetroComboBox cb_palette_categories;
        private System.Windows.Forms.Label lbl_palettecat;
        private System.Windows.Forms.DataGridView dG_mappings;
        private System.Windows.Forms.DataGridViewTextBoxColumn mappings_col_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn mappings_col_cat;
        private System.Windows.Forms.DataGridViewTextBoxColumn mappings_col_type;
        private System.Windows.Forms.DataGridViewTextBoxColumn mappings_col_color;
        private Cyotek.Windows.Forms.ScreenColorPicker palette_picker;
        private Cyotek.Windows.Forms.ColorWheel palette_wheel;
        private Cyotek.Windows.Forms.ColorGrid palette_grid;
        private Cyotek.Windows.Forms.ColorEditor palette_editor;
        private Cyotek.Windows.Forms.ColorEditorManager palette_colormanager;
        private System.Windows.Forms.Panel palette_preview;
        private System.Windows.Forms.TableLayoutPanel tlp_controls;
    }
}
