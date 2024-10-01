using MetroFramework.Controls;

namespace Chromatics.Forms
{
    partial class Uc_Palette
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tlp_main = new System.Windows.Forms.TableLayoutPanel();
            tlp_palette = new System.Windows.Forms.TableLayoutPanel();
            tlp_palette_inner = new System.Windows.Forms.TableLayoutPanel();
            palette_picker = new Cyotek.Windows.Forms.ScreenColorPicker();
            palette_preview = new System.Windows.Forms.Panel();
            palette_wheel = new Cyotek.Windows.Forms.ColorWheel();
            palette_grid = new Cyotek.Windows.Forms.ColorGrid();
            palette_editor = new Cyotek.Windows.Forms.ColorEditor();
            tlp_mappings = new System.Windows.Forms.TableLayoutPanel();
            dG_mappings = new System.Windows.Forms.DataGridView();
            mappings_col_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            mappings_col_cat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            mappings_col_type = new System.Windows.Forms.DataGridViewTextBoxColumn();
            mappings_col_color = new System.Windows.Forms.DataGridViewTextBoxColumn();
            tlp_controls = new System.Windows.Forms.TableLayoutPanel();
            btn_paletteexport = new System.Windows.Forms.Button();
            btn_paletteundo = new System.Windows.Forms.Button();
            btn_paletteimport = new System.Windows.Forms.Button();
            lbl_palettecat = new System.Windows.Forms.Label();
            cb_palette_categories = new MetroComboBox();
            palette_colormanager = new Cyotek.Windows.Forms.ColorEditorManager();
            tlp_main.SuspendLayout();
            tlp_palette.SuspendLayout();
            tlp_palette_inner.SuspendLayout();
            tlp_mappings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dG_mappings).BeginInit();
            tlp_controls.SuspendLayout();
            SuspendLayout();
            // 
            // tlp_main
            // 
            tlp_main.ColumnCount = 2;
            tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.54481F));
            tlp_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.45519F));
            tlp_main.Controls.Add(tlp_palette, 1, 0);
            tlp_main.Controls.Add(tlp_mappings, 0, 0);
            tlp_main.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_main.Location = new System.Drawing.Point(0, 0);
            tlp_main.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_main.Name = "tlp_main";
            tlp_main.RowCount = 1;
            tlp_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            tlp_main.Size = new System.Drawing.Size(1576, 906);
            tlp_main.TabIndex = 0;
            // 
            // tlp_palette
            // 
            tlp_palette.ColumnCount = 2;
            tlp_palette.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 51.92308F));
            tlp_palette.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48.07692F));
            tlp_palette.Controls.Add(tlp_palette_inner, 1, 1);
            tlp_palette.Controls.Add(palette_wheel, 0, 0);
            tlp_palette.Controls.Add(palette_grid, 0, 1);
            tlp_palette.Controls.Add(palette_editor, 1, 0);
            tlp_palette.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_palette.Location = new System.Drawing.Point(532, 4);
            tlp_palette.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_palette.Name = "tlp_palette";
            tlp_palette.RowCount = 2;
            tlp_palette.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 61.19611F));
            tlp_palette.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38.80389F));
            tlp_palette.Size = new System.Drawing.Size(1040, 898);
            tlp_palette.TabIndex = 0;
            // 
            // tlp_palette_inner
            // 
            tlp_palette_inner.ColumnCount = 1;
            tlp_palette_inner.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_palette_inner.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            tlp_palette_inner.Controls.Add(palette_picker, 0, 0);
            tlp_palette_inner.Controls.Add(palette_preview, 0, 1);
            tlp_palette_inner.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_palette_inner.Location = new System.Drawing.Point(544, 553);
            tlp_palette_inner.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_palette_inner.Name = "tlp_palette_inner";
            tlp_palette_inner.RowCount = 2;
            tlp_palette_inner.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 74.35897F));
            tlp_palette_inner.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.64103F));
            tlp_palette_inner.Size = new System.Drawing.Size(492, 341);
            tlp_palette_inner.TabIndex = 3;
            // 
            // palette_picker
            // 
            palette_picker.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            palette_picker.Color = System.Drawing.Color.Empty;
            palette_picker.Image = global::Chromatics.Properties.Resources.eyedropper;
            palette_picker.Location = new System.Drawing.Point(4, 4);
            palette_picker.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            palette_picker.Name = "palette_picker";
            palette_picker.Size = new System.Drawing.Size(484, 245);
            // 
            // palette_preview
            // 
            palette_preview.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            palette_preview.Location = new System.Drawing.Point(4, 257);
            palette_preview.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            palette_preview.Name = "palette_preview";
            palette_preview.Size = new System.Drawing.Size(484, 80);
            palette_preview.TabIndex = 1;
            // 
            // palette_wheel
            // 
            palette_wheel.Alpha = 1D;
            palette_wheel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            palette_wheel.Location = new System.Drawing.Point(4, 4);
            palette_wheel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            palette_wheel.Name = "palette_wheel";
            palette_wheel.Size = new System.Drawing.Size(532, 541);
            palette_wheel.TabIndex = 4;
            // 
            // palette_grid
            // 
            palette_grid.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            palette_grid.Location = new System.Drawing.Point(4, 553);
            palette_grid.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            palette_grid.Name = "palette_grid";
            palette_grid.Padding = new System.Windows.Forms.Padding(6, 6, 6, 6);
            palette_grid.Size = new System.Drawing.Size(532, 341);
            palette_grid.TabIndex = 5;
            // 
            // palette_editor
            // 
            palette_editor.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            palette_editor.AutoSize = true;
            palette_editor.Location = new System.Drawing.Point(545, 6);
            palette_editor.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            palette_editor.Name = "palette_editor";
            palette_editor.Size = new System.Drawing.Size(490, 537);
            palette_editor.TabIndex = 6;
            // 
            // tlp_mappings
            // 
            tlp_mappings.ColumnCount = 1;
            tlp_mappings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_mappings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            tlp_mappings.Controls.Add(dG_mappings, 0, 0);
            tlp_mappings.Controls.Add(tlp_controls, 0, 1);
            tlp_mappings.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_mappings.Location = new System.Drawing.Point(4, 4);
            tlp_mappings.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_mappings.Name = "tlp_mappings";
            tlp_mappings.RowCount = 2;
            tlp_mappings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 92.90681F));
            tlp_mappings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7.093185F));
            tlp_mappings.Size = new System.Drawing.Size(520, 898);
            tlp_mappings.TabIndex = 1;
            // 
            // dG_mappings
            // 
            dG_mappings.AllowUserToDeleteRows = false;
            dG_mappings.AllowUserToResizeColumns = false;
            dG_mappings.AllowUserToResizeRows = false;
            dG_mappings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dG_mappings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dG_mappings.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            dG_mappings.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
            dG_mappings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dG_mappings.ColumnHeadersVisible = false;
            dG_mappings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { mappings_col_id, mappings_col_cat, mappings_col_type, mappings_col_color });
            dG_mappings.Location = new System.Drawing.Point(4, 2);
            dG_mappings.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            dG_mappings.MultiSelect = false;
            dG_mappings.Name = "dG_mappings";
            dG_mappings.RowHeadersVisible = false;
            dG_mappings.RowHeadersWidth = 51;
            dG_mappings.RowTemplate.Height = 29;
            dG_mappings.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            dG_mappings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dG_mappings.ShowCellErrors = false;
            dG_mappings.Size = new System.Drawing.Size(512, 830);
            dG_mappings.TabIndex = 1;
            dG_mappings.RowPrePaint += dG_mappings_RowPrePaint;
            dG_mappings.SelectionChanged += dG_mappings_SelectionChanged;
            // 
            // mappings_col_id
            // 
            mappings_col_id.HeaderText = "ID";
            mappings_col_id.MinimumWidth = 6;
            mappings_col_id.Name = "mappings_col_id";
            mappings_col_id.ReadOnly = true;
            mappings_col_id.Visible = false;
            // 
            // mappings_col_cat
            // 
            mappings_col_cat.HeaderText = "Category";
            mappings_col_cat.MinimumWidth = 6;
            mappings_col_cat.Name = "mappings_col_cat";
            mappings_col_cat.ReadOnly = true;
            mappings_col_cat.Visible = false;
            // 
            // mappings_col_type
            // 
            mappings_col_type.HeaderText = "Type";
            mappings_col_type.MinimumWidth = 6;
            mappings_col_type.Name = "mappings_col_type";
            mappings_col_type.ReadOnly = true;
            // 
            // mappings_col_color
            // 
            mappings_col_color.HeaderText = "Color";
            mappings_col_color.MinimumWidth = 6;
            mappings_col_color.Name = "mappings_col_color";
            mappings_col_color.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            mappings_col_color.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // tlp_controls
            // 
            tlp_controls.ColumnCount = 5;
            tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19.81132F));
            tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 37.73584F));
            tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.15094F));
            tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.15094F));
            tlp_controls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.15094F));
            tlp_controls.Controls.Add(btn_paletteexport, 3, 0);
            tlp_controls.Controls.Add(btn_paletteundo, 4, 0);
            tlp_controls.Controls.Add(btn_paletteimport, 2, 0);
            tlp_controls.Controls.Add(lbl_palettecat, 0, 0);
            tlp_controls.Controls.Add(cb_palette_categories, 1, 0);
            tlp_controls.Dock = System.Windows.Forms.DockStyle.Fill;
            tlp_controls.Location = new System.Drawing.Point(4, 838);
            tlp_controls.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            tlp_controls.Name = "tlp_controls";
            tlp_controls.RowCount = 1;
            tlp_controls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlp_controls.Size = new System.Drawing.Size(512, 56);
            tlp_controls.TabIndex = 2;
            // 
            // btn_paletteexport
            // 
            btn_paletteexport.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btn_paletteexport.AutoSize = true;
            btn_paletteexport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn_paletteexport.Image = global::Chromatics.Properties.Resources.palette_save;
            btn_paletteexport.Location = new System.Drawing.Point(370, 4);
            btn_paletteexport.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            btn_paletteexport.Name = "btn_paletteexport";
            btn_paletteexport.Size = new System.Drawing.Size(64, 48);
            btn_paletteexport.TabIndex = 0;
            btn_paletteexport.UseVisualStyleBackColor = true;
            btn_paletteexport.Click += btn_paletteexport_Click;
            // 
            // btn_paletteundo
            // 
            btn_paletteundo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btn_paletteundo.AutoSize = true;
            btn_paletteundo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn_paletteundo.Image = global::Chromatics.Properties.Resources.ret_arrow;
            btn_paletteundo.Location = new System.Drawing.Point(442, 4);
            btn_paletteundo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            btn_paletteundo.Name = "btn_paletteundo";
            btn_paletteundo.Size = new System.Drawing.Size(66, 48);
            btn_paletteundo.TabIndex = 2;
            btn_paletteundo.UseVisualStyleBackColor = true;
            btn_paletteundo.Click += btn_paletteundo_Click;
            // 
            // btn_paletteimport
            // 
            btn_paletteimport.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btn_paletteimport.AutoSize = true;
            btn_paletteimport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn_paletteimport.Image = global::Chromatics.Properties.Resources.palette_load;
            btn_paletteimport.Location = new System.Drawing.Point(298, 4);
            btn_paletteimport.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            btn_paletteimport.Name = "btn_paletteimport";
            btn_paletteimport.Size = new System.Drawing.Size(64, 48);
            btn_paletteimport.TabIndex = 1;
            btn_paletteimport.UseVisualStyleBackColor = true;
            btn_paletteimport.Click += btn_paletteimport_Click;
            // 
            // lbl_palettecat
            // 
            lbl_palettecat.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lbl_palettecat.AutoSize = true;
            lbl_palettecat.Location = new System.Drawing.Point(4, 0);
            lbl_palettecat.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbl_palettecat.Name = "lbl_palettecat";
            lbl_palettecat.Size = new System.Drawing.Size(93, 56);
            lbl_palettecat.TabIndex = 4;
            lbl_palettecat.Text = "Category:";
            lbl_palettecat.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cb_palette_categories
            // 
            cb_palette_categories.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            cb_palette_categories.FormattingEnabled = true;
            cb_palette_categories.ItemHeight = 23;
            cb_palette_categories.Location = new System.Drawing.Point(105, 4);
            cb_palette_categories.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            cb_palette_categories.Name = "cb_palette_categories";
            cb_palette_categories.Size = new System.Drawing.Size(185, 29);
            cb_palette_categories.TabIndex = 3;
            cb_palette_categories.UseSelectable = true;
            cb_palette_categories.SelectedIndexChanged += cb_palette_categories_SelectedIndexChanged;
            // 
            // palette_colormanager
            // 
            palette_colormanager.Color = System.Drawing.Color.Empty;
            palette_colormanager.ColorEditor = palette_editor;
            palette_colormanager.ColorGrid = palette_grid;
            palette_colormanager.ColorWheel = palette_wheel;
            palette_colormanager.ScreenColorPicker = palette_picker;
            palette_colormanager.ColorChanged += palette_colormanager_colorchanged;
            // 
            // Uc_Palette
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(tlp_main);
            Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            Name = "Uc_Palette";
            Size = new System.Drawing.Size(1576, 906);
            Load += OnLoad;
            Resize += OnResize;
            tlp_main.ResumeLayout(false);
            tlp_palette.ResumeLayout(false);
            tlp_palette.PerformLayout();
            tlp_palette_inner.ResumeLayout(false);
            tlp_mappings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dG_mappings).EndInit();
            tlp_controls.ResumeLayout(false);
            tlp_controls.PerformLayout();
            ResumeLayout(false);
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
