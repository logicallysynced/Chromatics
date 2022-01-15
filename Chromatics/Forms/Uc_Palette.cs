using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Helpers;
using Chromatics.Models;
using MetroFramework.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Chromatics.Enums.Palette;

namespace Chromatics.Forms
{
    public partial class Uc_Palette : UserControl
    {
        private bool MappingGridStartup;
        private MetroToolTip tt_mappings;
        private delegate void ResetMappingsDelegate();

        public Uc_Palette()
        {
            InitializeComponent();
            cb_palette_categories.DrawMode = DrawMode.OwnerDrawFixed;
            cb_palette_categories.ItemHeight = tlp_controls.Height;
            cb_palette_categories.Margin = new Padding(0, tlp_controls.Height / 4, 3, 0);
        }

        private void OnLoad(object sender, EventArgs e)
        {
            //Create tooltop manager
            tt_mappings = new MetroToolTip
            {
                ToolTipIcon = ToolTipIcon.Info,
                IsBalloon = true,
                ShowAlways = true
            };

            tt_mappings.SetToolTip(btn_paletteexport, @"Export Chromatics color palette to file");
            tt_mappings.SetToolTip(btn_paletteimport, @"Import Chromatics color palette from file");
            tt_mappings.SetToolTip(btn_paletteundo, @"Restore selected color mapping");
            tt_mappings.SetToolTip(cb_palette_categories, @"Filter color mappings");

            //Load Color Mappings
            if (RGBController.LoadColorPalette())
            {
                Logger.WriteConsole(LoggerTypes.System, $"Loaded palette from palette.chromatics3");

            }
            else
            {
                Logger.WriteConsole(LoggerTypes.System, @"No palette file found. Creating default color palette..");
                RGBController.SaveColorPalette();
            }

            InitColorMappingGrid();

            
        }

        private void ToggleMappingControls(bool toggle)
        {
            if (toggle)
            {
                palette_colormanager.ColorEditor.Enabled = true;
                palette_colormanager.ColorGrid.Enabled = true;
                palette_colormanager.ColorWheel.Enabled = true;
                palette_colormanager.ScreenColorPicker.Enabled = true;
            }
            else
            {
                palette_colormanager.ColorEditor.Enabled = false;
                palette_colormanager.ColorGrid.Enabled = false;
                palette_colormanager.ColorWheel.Enabled = false;
                palette_colormanager.ScreenColorPicker.Enabled = false;
            }
        }

        private void InitColorMappingGrid()
        {
            //Enumerate Palette Types to category selection
            for (int i = 0; i <= Palette.TypeCount; i++)
            {
                var name = EnumExtensions.GetAttribute<DisplayAttribute>((Palette.PaletteTypes)i).Name;
                var item = new ComboboxItem { Value = (Palette.PaletteTypes)i, Text = name };
                cb_palette_categories.Items.Add(item);
            }

            cb_palette_categories.SelectedIndex = 0;
            ToggleMappingControls(false);

            ResetMappingsDataGrid();
        }

        private void ResetMappingsDataGrid()
        {
            if (InvokeRequired)
            {
                ResetMappingsDelegate del = ResetMappingsDataGrid;
                Invoke(del);
            }
            else
            {
                SetupMappingsDataGrid();
            }
        }

        private void SetupMappingsDataGrid()
        {
            MappingGridStartup = false;
            dG_mappings.AllowUserToAddRows = true;
            dG_mappings.Rows.Clear();

            var i = 0;
            var active = RGBController.GetActivePalette();
            var palette = active.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            DataGridViewRow[] dgV = new DataGridViewRow[palette.Length];

            foreach (var p in palette)
            {
                var mapping = (ColorMapping)p.GetValue(active);
                var paletteItem = (DataGridViewRow)dG_mappings.Rows[0].Clone();
                paletteItem.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value = p.Name;
                paletteItem.Cells[dG_mappings.Columns["mappings_col_cat"].Index].Value = mapping.Type;
                paletteItem.Cells[dG_mappings.Columns["mappings_col_type"].Index].Value = mapping.Name;

                var paletteBtn = new DataGridViewTextBoxCell();
                paletteBtn.Style.BackColor = mapping.Color;
                paletteBtn.Style.SelectionBackColor = mapping.Color;

                paletteBtn.Value = "";

                paletteItem.Cells[dG_mappings.Columns["mappings_col_color"].Index] = paletteBtn;

                //dG_mappings.Rows.Add(paletteItem);
                dgV[i] = paletteItem;
                i++;
                paletteBtn.ReadOnly = true;
            }

            dG_mappings.Rows.AddRange(dgV);

            dG_mappings.AllowUserToAddRows = false;
            MappingGridStartup = true;
            
        }

        private void btn_paletteimport_Click(object sender, EventArgs e)
        {
            RGBController.ImportColorPalette();
            ResetMappingsDataGrid();
        }

        private void btn_paletteexport_Click(object sender, EventArgs e)
        {
            RGBController.ExportColorPalette();
        }

        private void btn_paletteundo_Click(object sender, EventArgs e)
        {
            var pmcs = dG_mappings.CurrentRow;
            var pcmsid = (string)pmcs.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value;

            var cm = new PaletteColorModel();
            var _restore = Color.Black;

            foreach (var p in typeof(PaletteColorModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.Name == pcmsid)
                {
                    var mapping = (ColorMapping)p.GetValue(cm);
                    _restore = mapping.Color;
                }
            }
                               
            palette_colormanager.Color = _restore;
        }

        private void cb_palette_categories_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MappingGridStartup)
            {
                var filter = (ComboboxItem)cb_palette_categories.SelectedItem;

                if ((PaletteTypes)filter.Value == PaletteTypes.All)
                    foreach (DataGridViewRow row in dG_mappings.Rows)
                        row.Visible = true;
                else
                    foreach (DataGridViewRow row in dG_mappings.Rows)
                        if ((PaletteTypes)row.Cells[dG_mappings.Columns["mappings_col_cat"].Index].Value == (PaletteTypes)filter.Value)
                            row.Visible = true;
                        else
                            row.Visible = false;
            }
        }

        private void dG_mappings_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            e.PaintParts &= ~DataGridViewPaintParts.Focus;
        }

        private void dG_mappings_SelectionChanged(object sender, EventArgs e)
        {
            var color = dG_mappings.CurrentRow.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor;
            ToggleMappingControls(true);
            palette_colormanager.Color = color;
            palette_preview.BackColor = color;
        }

        private void palette_colormanager_colorchanged(object sender, EventArgs e)
        {
            var pmcs = dG_mappings.CurrentRow;
            var pcmsid = (string)pmcs.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value;
            var pcmsColor = pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor;
            var col = palette_colormanager.Color;

            palette_preview.BackColor = col;

            if (pcmsColor != palette_colormanager.Color)
            {
                var active = RGBController.GetActivePalette();
                foreach (var p in typeof(PaletteColorModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (p.Name == pcmsid)
                    {
                        var mapping = (ColorMapping)p.GetValue(active);
                        var new_mapping = new ColorMapping(mapping.Name, mapping.Type, col); 
                        p.SetValue(active, new_mapping);
                    }
                        
                }
                   
                pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor = col;
                pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.SelectionBackColor = col;

                //SetKeysbase = false;
                //SetMousebase = false;
                //SetPadbase = false;
                RGBController.SaveColorPalette();
            }
        }

        private void OnResize(object sender, EventArgs e)
        {
            cb_palette_categories.Margin = new Padding(0, tlp_controls.Height / 4, 3, 0);
        }
    }
}
