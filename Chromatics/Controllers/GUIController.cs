using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Chromatics.Datastore;
using Cyotek.Windows.Forms;
using Chromatics.LCDInterfaces;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Chromatics
{
    partial class Chromatics : ILogWrite
    {
        private readonly DataGridViewComboBoxColumn _dGmode = new DataGridViewComboBoxColumn();

        private readonly Dictionary<int, string> DeviceModes = new Dictionary<int, string>
        {
            //Keys
            {0, "Disabled"},
            {1, "Standby"},
            {2, "Default Color"},
            {3, "Highlight Colour"},
            {4, "Enmity Tracker"},
            {5, "Target HP"},
            {6, "Status Effects"},
            {7, "HP Tracker"},
            {8, "MP Tracker"},
            {9, "TP Tracker"},
            {10, "Castbar"},
            {11, "Chromatics Default"}
        };

        private readonly Dictionary<string, string[]> MappingPalette = new Dictionary<string, string[]>
        {
            //Keys
            {"ColorMapping_BaseColor", new[] {"Base Color", "1", "Black", "White"}},
            {"ColorMapping_HighlightColor", new[] {"Highlight Color", "1", "Black", "White"}},
            {"ColorMapping_HPFull", new[] {"HP Full", "2", "Black", "White"}},
            {"ColorMapping_HPEmpty", new[] {"HP Empty", "2", "Black", "White"}},
            {"ColorMapping_HPCritical", new[] {"HP Critical", "2", "Black", "White"}},
            {"ColorMapping_HPLoss", new[] {"HP Loss", "2", "Black", "White"}},
            {"ColorMapping_MPFull", new[] {"MP Full", "2", "Black", "White"}},
            {"ColorMapping_MPEmpty", new[] {"MP Empty", "2", "Black", "White"}},
            {"ColorMapping_TPFull", new[] {"TP Full", "2", "Black", "White"}},
            {"ColorMapping_TPEmpty", new[] {"TP Empty", "2", "Black", "White"}},
            {"ColorMapping_GPFull", new[] {"GP Full", "2", "Black", "White"}},
            {"ColorMapping_GPEmpty", new[] {"GP Empty", "2", "Black", "White"}},
            {"ColorMapping_CPFull", new[] {"CP Full", "2", "Black", "White"}},
            {"ColorMapping_CPEmpty", new[] {"CP Empty", "2", "Black", "White"}},
            {"ColorMapping_CastChargeFull", new[] {"Cast Charge", "3", "Black", "White"}},
            {"ColorMapping_CastChargeEmpty", new[] {"Cast Empty", "3", "Black", "White"}},
            {"ColorMapping_Emnity0", new[] { "Minimal Enmity", "4", "Black", "White"}},
            {"ColorMapping_Emnity1", new[] { "Low Enmity", "4", "Black", "White"}},
            {"ColorMapping_Emnity2", new[] { "Medium Enmity", "4", "Black", "White"}},
            {"ColorMapping_Emnity3", new[] { "High Enmity", "4", "Black", "White"}},
            {"ColorMapping_Emnity4", new[] { "Max Enmity", "4", "Black", "White"}},
            {"ColorMapping_NoEmnity", new[] { "No Enmity", "4", "Black", "White"}},
            {"ColorMapping_TargetHPClaimed", new[] {"Target HP - Claimed", "5", "Black", "White"}},
            {"ColorMapping_TargetHPIdle", new[] {"Target HP - Idle", "5", "Black", "White"}},
            {"ColorMapping_TargetHPEmpty", new[] {"Target HP - Empty", "5", "Black", "White"}},
            {"ColorMapping_TargetCasting", new[] {"Target Casting", "5", "Black", "White"}},
            {"ColorMapping_Bind", new[] {"Bind", "6", "Black", "White"}},
            {"ColorMapping_Petrification", new[] {"Petrification", "6", "Black", "White"}},
            {"ColorMapping_Slow", new[] {"Slow", "6", "Black", "White"}},
            {"ColorMapping_Stun", new[] {"Stun", "6", "Black", "White"}},
            {"ColorMapping_Silence", new[] {"Silence", "6", "Black", "White"}},
            {"ColorMapping_Poison", new[] {"Poison", "6", "Black", "White"}},
            {"ColorMapping_Pollen", new[] {"Pollen", "6", "Black", "White"}},
            {"ColorMapping_Pox", new[] {"Pox", "6", "Black", "White"}},
            {"ColorMapping_Paralysis", new[] {"Paralysis", "6", "Black", "White"}},
            {"ColorMapping_Leaden", new[] {"Leaden", "6", "Black", "White"}},
            {"ColorMapping_Incapacitation", new[] {"Incapacitation", "6", "Black", "White"}},
            {"ColorMapping_Dropsy", new[] {"Dropsy", "6", "Black", "White"}},
            {"ColorMapping_Amnesia", new[] {"Amnesia", "6", "Black", "White"}},
            {"ColorMapping_Bleed", new[] {"Bleed", "6", "Black", "White"}},
            {"ColorMapping_Misery", new[] {"Misery", "6", "Black", "White"}},
            {"ColorMapping_Sleep", new[] {"Sleep", "6", "Black", "White"}},
            {"ColorMapping_Daze", new[] {"Daze", "6", "Black", "White"}},
            {"ColorMapping_Heavy", new[] {"Heavy", "6", "Black", "White"}},
            {"ColorMapping_Infirmary", new[] {"Infirmary", "6", "Black", "White"}},
            {"ColorMapping_Burns", new[] {"Burns", "6", "Black", "White"}},
            {"ColorMapping_DeepFreeze", new[] {"Deep Freeze", "6", "Black", "White"}},
            {"ColorMapping_DamageDown", new[] {"Damage Down", "6", "Black", "White"}},
            {"ColorMapping_GCDHot", new[] {"GCD Countdown Hot", "7", "Black", "White"}},
            {"ColorMapping_GCDReady", new[] {"GCD Countdown Ready", "7", "Black", "White"}},
            {"ColorMapping_GCDEmpty", new[] {"GCD Countdown Empty", "7", "Black", "White"}},
            {"ColorMapping_HotbarProc", new[] {"Keybind Proc/Combo", "7", "Black", "White"}},
            {"ColorMapping_HotbarCD", new[] {"Keybind Cooldown", "7", "Black", "White"}},
            {"ColorMapping_HotbarReady", new[] {"Keybind Ready", "7", "Black", "White"}},
            {"ColorMapping_HotbarOutRange", new[] {"Keybind Out of Range", "7", "Black", "White"}},
            {"ColorMapping_HotbarNotAvailable", new[] {"Keybind Not Available", "7", "Black", "White"}}
        };
        

        private readonly Dictionary<int, string> PaletteCategories = new Dictionary<int, string>
        {
            //Keys
            {0, "All"},
            {1, "Chromatics"},
            {2, "Player Stats"},
            {3, "Abilities"},
            {4, "Enmity/Aggro"},
            {5, "Target/Enemy"},
            {6, "Status Effects"},
            {7, "Cooldowns/Keybinds" }
        };

        public void ResetDeviceDataGrid()
        {
            if (InvokeRequired)
            {
                ResetGridDelegate del = ResetDeviceDataGrid;
                Invoke(del);
            }
            else
            {
                SetupDeviceDataGrid();
            }
        }

        public void ResetMappingsDataGrid()
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

        private void InitColorMappingGrid()
        {
            foreach (var c in PaletteCategories)
                cb_palette_categories.Items.Add(c.Value);

            cb_palette_categories.SelectedIndex = 0;
            ToggleMappingControls(false);

            ResetMappingsDataGrid();
        }

        private void ToggleMappingControls(bool toggle)
        {
            if (toggle)
            {
                mapping_colorEditorManager.ColorEditor.Enabled = true;
                mapping_colorEditorManager.ColorGrid.Enabled = true;
                mapping_colorEditorManager.ColorWheel.Enabled = true;
                mapping_colorEditorManager.LightnessColorSlider.Enabled = true;
                mapping_colorEditorManager.ScreenColorPicker.Enabled = true;
                loadPaletteButton.Enabled = true;
            }
            else
            {
                mapping_colorEditorManager.ColorEditor.Enabled = false;
                mapping_colorEditorManager.ColorGrid.Enabled = false;
                mapping_colorEditorManager.ColorWheel.Enabled = false;
                mapping_colorEditorManager.LightnessColorSlider.Enabled = false;
                mapping_colorEditorManager.ScreenColorPicker.Enabled = false;
                loadPaletteButton.Enabled = false;
            }
        }

        private void cb_palette_categories_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MappingGridStartup)
            {
                var filter = cb_palette_categories.SelectedIndex.ToString();

                if (filter == "0")
                    foreach (DataGridViewRow row in dG_mappings.Rows)
                        row.Visible = true;
                else
                    foreach (DataGridViewRow row in dG_mappings.Rows)
                        if ((string) row.Cells[dG_mappings.Columns["mappings_col_cat"].Index].Value == filter)
                            row.Visible = true;
                        else
                            row.Visible = false;
            }
        }

        private void SetupMappingsDataGrid()
        {
            MappingGridStartup = false;
            dG_mappings.AllowUserToAddRows = true;
            dG_mappings.Rows.Clear();

            DrawMappingsDict();

            foreach (var palette in MappingPalette)
            {
                var _PaletteItem = (DataGridViewRow) dG_mappings.Rows[0].Clone();
                _PaletteItem.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value = palette.Key;
                _PaletteItem.Cells[dG_mappings.Columns["mappings_col_cat"].Index].Value = palette.Value[1];
                _PaletteItem.Cells[dG_mappings.Columns["mappings_col_type"].Index].Value = palette.Value[0];

                var _PaletteBtn = new DataGridViewTextBoxCell();
                _PaletteBtn.Style.BackColor = ColorTranslator.FromHtml(palette.Value[2]);
                _PaletteBtn.Style.SelectionBackColor = ColorTranslator.FromHtml(palette.Value[2]);

                _PaletteBtn.Value = "";
                _PaletteItem.Cells[dG_mappings.Columns["mappings_col_color"].Index] = _PaletteBtn;
                dG_mappings.Rows.Add(_PaletteItem);
                _PaletteBtn.ReadOnly = true;
            }

            dG_mappings.AllowUserToAddRows = false;
            MappingGridStartup = true;
        }

        private void DrawMappingsDict()
        {
            //PropertyInfo[] _FFXIVColorMappings = typeof(FFXIVColorMappings).GetProperties();

            //ColorMappings
            foreach (var p in typeof(FFXIVColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var _var = p.Name;
                var _color = (string) p.GetValue(ColorMappings);
                var _data = MappingPalette[_var];
                string[] data = {_data[0], _data[1], _color, _data[3]};
                MappingPalette[_var] = data;
            }
        }

        private void InitDeviceDataGrid()
        {
            //
        }

        private async void SetupDeviceDataGrid()
        {
            try
            {
                DeviceGridStartup = false;
                dG_devices.AllowUserToAddRows = true;
                dG_devices.Rows.Clear();


                if (RazerSDKCalled == 1)
                {
                    //Keyboard
                    var _RzDgKeyboard = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    //DataGridViewRow _RzDgKeyboard = new DataGridViewRow();
                    _RzDgKeyboard.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Keyboard";
                    _RzDgKeyboard.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Keyboard";
                    _RzDgKeyboard.Cells[dG_devices.Columns["col_status"].Index].Value = RazerDeviceKeyboard
                        ? "Enabled"
                        : "Disabled";
                    _RzDgKeyboard.Cells[dG_devices.Columns["col_state"].Index].Value = RazerDeviceKeyboard;
                    _RzDgKeyboard.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceKeyboard";

                    var _RzDgKeyboard_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _RzDgKeyboard_dgc.Items.Add(d.Value);

                    _RzDgKeyboard_dgc.Value = DeviceModes[11];
                    _RzDgKeyboard_dgc.DisplayStyleForCurrentCellOnly = true;
                    _RzDgKeyboard_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    _RzDgKeyboard.Cells[dG_devices.Columns["col_mode"].Index] = _RzDgKeyboard_dgc;

                    dG_devices.Rows.Add(_RzDgKeyboard);
                    _RzDgKeyboard_dgc.ReadOnly = true;

                    //Mouse
                    var _RzDgMouse = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    _RzDgMouse.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Mouse";
                    _RzDgMouse.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Mouse";
                    _RzDgMouse.Cells[dG_devices.Columns["col_status"].Index].Value = RazerDeviceMouse
                        ? "Enabled"
                        : "Disabled";
                    _RzDgMouse.Cells[dG_devices.Columns["col_state"].Index].Value = RazerDeviceMouse;
                    _RzDgMouse.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceMouse";

                    var _RzDgMouse_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _RzDgMouse_dgc.Items.Add(d.Value);

                    _RzDgMouse_dgc.Value = DeviceModes[11];
                    _RzDgMouse_dgc.DisplayStyleForCurrentCellOnly = true;
                    _RzDgMouse_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _RzDgMouse.Cells[dG_devices.Columns["col_mode"].Index] = _RzDgMouse_dgc;

                    dG_devices.Rows.Add(_RzDgMouse);
                    _RzDgMouse_dgc.ReadOnly = true;

                    //Headset
                    var _RzDgHeadset = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    _RzDgHeadset.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Headset";
                    _RzDgHeadset.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Headset";
                    _RzDgHeadset.Cells[dG_devices.Columns["col_status"].Index].Value = RazerDeviceHeadset
                        ? "Enabled"
                        : "Disabled";
                    _RzDgHeadset.Cells[dG_devices.Columns["col_state"].Index].Value = RazerDeviceHeadset;
                    _RzDgHeadset.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceHeadset";

                    var _RzDgHeadset_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _RzDgHeadset_dgc.Items.Add(d.Value);

                    _RzDgHeadset_dgc.Value = DeviceModes[11];
                    _RzDgHeadset_dgc.DisplayStyleForCurrentCellOnly = true;
                    _RzDgHeadset_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _RzDgHeadset.Cells[dG_devices.Columns["col_mode"].Index] = _RzDgHeadset_dgc;

                    dG_devices.Rows.Add(_RzDgHeadset);
                    _RzDgHeadset_dgc.ReadOnly = true;

                    //Mousepad
                    var _RzDgMousepad = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    _RzDgMousepad.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Mousepad";
                    _RzDgMousepad.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Mousepad";
                    _RzDgMousepad.Cells[dG_devices.Columns["col_status"].Index].Value = RazerDeviceMousepad
                        ? "Enabled"
                        : "Disabled";
                    _RzDgMousepad.Cells[dG_devices.Columns["col_state"].Index].Value = RazerDeviceMousepad;
                    _RzDgMousepad.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceMousepad";

                    var _RzDgMousepad_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _RzDgMousepad_dgc.Items.Add(d.Value);

                    _RzDgMousepad_dgc.Value = DeviceModes[11];
                    _RzDgMousepad_dgc.DisplayStyleForCurrentCellOnly = true;
                    _RzDgMousepad_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _RzDgMousepad.Cells[dG_devices.Columns["col_mode"].Index] = _RzDgMousepad_dgc;

                    dG_devices.Rows.Add(_RzDgMousepad);
                    _RzDgMousepad_dgc.ReadOnly = true;

                    //Keypad
                    var _RzDgKeypad = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    _RzDgKeypad.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Keypad";
                    _RzDgKeypad.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Keypad";
                    _RzDgKeypad.Cells[dG_devices.Columns["col_status"].Index].Value = RazerDeviceKeypad
                        ? "Enabled"
                        : "Disabled";
                    _RzDgKeypad.Cells[dG_devices.Columns["col_state"].Index].Value = RazerDeviceKeypad;
                    _RzDgKeypad.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceKeypad";

                    var _RzDgKeypad_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _RzDgKeypad_dgc.Items.Add(d.Value);

                    _RzDgKeypad_dgc.Value = DeviceModes[11];
                    _RzDgKeypad_dgc.DisplayStyleForCurrentCellOnly = true;
                    _RzDgKeypad_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _RzDgKeypad.Cells[dG_devices.Columns["col_mode"].Index] = _RzDgKeypad_dgc;

                    dG_devices.Rows.Add(_RzDgKeypad);
                    _RzDgKeypad_dgc.ReadOnly = true;

                    _razer.ResetRazerDevices(RazerDeviceKeyboard, RazerDeviceKeypad, RazerDeviceMouse, RazerDeviceMousepad, RazerDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                }

                if (CorsairSDKCalled == 1)
                {
                    //Keyboard
                    var _CorsairDgKeyboard = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    _CorsairDgKeyboard.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Corsair Keyboard";
                    _CorsairDgKeyboard.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Keyboard";
                    _CorsairDgKeyboard.Cells[dG_devices.Columns["col_status"].Index].Value = CorsairDeviceKeyboard
                        ? "Enabled"
                        : "Disabled";
                    _CorsairDgKeyboard.Cells[dG_devices.Columns["col_state"].Index].Value = CorsairDeviceKeyboard;
                    _CorsairDgKeyboard.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CorsairDeviceKeyboard";

                    var _CorsairDgKeyboard_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _CorsairDgKeyboard_dgc.Items.Add(d.Value);

                    _CorsairDgKeyboard_dgc.Value = DeviceModes[11];
                    _CorsairDgKeyboard_dgc.DisplayStyleForCurrentCellOnly = true;
                    _CorsairDgKeyboard_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _CorsairDgKeyboard.Cells[dG_devices.Columns["col_mode"].Index] = _CorsairDgKeyboard_dgc;

                    dG_devices.Rows.Add(_CorsairDgKeyboard);
                    _CorsairDgKeyboard_dgc.ReadOnly = true;

                    //Mouse
                    var _CorsairDgMouse = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    _CorsairDgMouse.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Corsair Mouse";
                    _CorsairDgMouse.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Mouse";
                    _CorsairDgMouse.Cells[dG_devices.Columns["col_status"].Index].Value = CorsairDeviceMouse
                        ? "Enabled"
                        : "Disabled";
                    _CorsairDgMouse.Cells[dG_devices.Columns["col_state"].Index].Value = CorsairDeviceMouse;
                    _CorsairDgMouse.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CorsairDeviceMouse";

                    var _CorsairDgMouse_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _CorsairDgMouse_dgc.Items.Add(d.Value);

                    _CorsairDgMouse_dgc.Value = DeviceModes[11];
                    _CorsairDgMouse_dgc.DisplayStyleForCurrentCellOnly = true;
                    _CorsairDgMouse_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _CorsairDgMouse.Cells[dG_devices.Columns["col_mode"].Index] = _CorsairDgMouse_dgc;

                    dG_devices.Rows.Add(_CorsairDgMouse);
                    _CorsairDgMouse_dgc.ReadOnly = true;

                    //Headset
                    var _CorsairDgHeadset = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    _CorsairDgHeadset.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Corsair Headset";
                    _CorsairDgHeadset.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Headset";
                    _CorsairDgHeadset.Cells[dG_devices.Columns["col_status"].Index].Value = CorsairDeviceHeadset
                        ? "Enabled"
                        : "Disabled";
                    _CorsairDgHeadset.Cells[dG_devices.Columns["col_state"].Index].Value = CorsairDeviceHeadset;
                    _CorsairDgHeadset.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CorsairDeviceHeadset";

                    var _CorsairDgHeadset_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _CorsairDgHeadset_dgc.Items.Add(d.Value);

                    _CorsairDgHeadset_dgc.Value = DeviceModes[11];
                    _CorsairDgHeadset_dgc.DisplayStyleForCurrentCellOnly = true;
                    _CorsairDgHeadset_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _CorsairDgHeadset.Cells[dG_devices.Columns["col_mode"].Index] = _CorsairDgHeadset_dgc;

                    dG_devices.Rows.Add(_CorsairDgHeadset);
                    _CorsairDgHeadset_dgc.ReadOnly = true;

                    //Mousepad
                    var _CorsairDgMousepad = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    _CorsairDgMousepad.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Corsair Mousepad";
                    _CorsairDgMousepad.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Mousepad";
                    _CorsairDgMousepad.Cells[dG_devices.Columns["col_status"].Index].Value = CorsairDeviceMousepad
                        ? "Enabled"
                        : "Disabled";
                    _CorsairDgMousepad.Cells[dG_devices.Columns["col_state"].Index].Value = CorsairDeviceMousepad;
                    _CorsairDgMousepad.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CorsairDeviceMousepad";

                    var _CorsairDgMousepad_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _CorsairDgMousepad_dgc.Items.Add(d.Value);

                    _CorsairDgMousepad_dgc.Value = DeviceModes[11];
                    _CorsairDgMousepad_dgc.DisplayStyleForCurrentCellOnly = true;
                    _CorsairDgMousepad_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _CorsairDgMousepad.Cells[dG_devices.Columns["col_mode"].Index] = _CorsairDgMousepad_dgc;

                    dG_devices.Rows.Add(_CorsairDgMousepad);
                    _CorsairDgMousepad_dgc.ReadOnly = true;

                    _corsair.ResetCorsairDevices(CorsairDeviceKeyboard, CorsairDeviceKeypad, CorsairDeviceMouse, CorsairDeviceMousepad, CorsairDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                }

                if (LogitechSDKCalled == 1)
                {
                    var _LogitechDgDevices = (DataGridViewRow)dG_devices.Rows[0].Clone();
                    _LogitechDgDevices.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Logitech Devices";
                    _LogitechDgDevices.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Multiple Devices";
                    _LogitechDgDevices.Cells[dG_devices.Columns["col_status"].Index].Value = LogitechDeviceKeyboard
                        ? "Enabled"
                        : "Disabled";
                    _LogitechDgDevices.Cells[dG_devices.Columns["col_state"].Index].Value = LogitechDeviceKeyboard;
                    _LogitechDgDevices.Cells[dG_devices.Columns["col_dattype"].Index].Value = "LogitechDevice";

                    var _LogitechDgDevices_dgc = new DataGridViewComboBoxCell();
                    foreach (var d in DeviceModes)
                        _LogitechDgDevices_dgc.Items.Add(d.Value);

                    _LogitechDgDevices_dgc.Value = DeviceModes[11];
                    _LogitechDgDevices_dgc.DisplayStyleForCurrentCellOnly = true;
                    _LogitechDgDevices_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    _LogitechDgDevices.Cells[dG_devices.Columns["col_mode"].Index] = _LogitechDgDevices_dgc;

                    dG_devices.Rows.Add(_LogitechDgDevices);
                    _LogitechDgDevices_dgc.ReadOnly = true;

                    _logitech.ResetLogitechDevices(LogitechDeviceKeyboard, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                }

                if (LifxSDKCalled == 1)
                    if (_lifx.LifxBulbs > 0)
                    {
                        foreach (var d in _lifx.LifxBulbsDat.ToList())
                        {
                            Thread.Sleep(1);

                            //LIFX Device
                            var state = await _lifx.GetLightStateAsync(d.Key);
                            var device = await _lifx.GetDeviceVersionAsync(d.Key);

                            _dGmode.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                            _dGmode.DisplayStyleForCurrentCellOnly = true;
                            var LState = _lifx.LifxStateMemory[d.Key.MacAddressName];

                            var _LIFXdGDevice = (DataGridViewRow)dG_devices.Rows[0].Clone();
                            _LIFXdGDevice.Cells[dG_devices.Columns["col_devicename"].Index].Value = state.Label + " (" +
                                                                                                    d.Key.MacAddressName +
                                                                                                    ")";
                            _LIFXdGDevice.Cells[dG_devices.Columns["col_devicetype"].Index].Value =
                                _lifx.LIFXproductids[device.Product] + " (Version " + device.Version + ")";
                            _LIFXdGDevice.Cells[dG_devices.Columns["col_status"].Index].Value = LState == 0
                                ? "Disabled"
                                : "Enabled";
                            _LIFXdGDevice.Cells[dG_devices.Columns["col_state"].Index].Value = LState == 0
                                ? false
                                : true;
                            _LIFXdGDevice.Cells[dG_devices.Columns["col_dattype"].Index].Value = "LIFX";
                            _LIFXdGDevice.Cells[dG_devices.Columns["col_ID"].Index].Value = d.Key.MacAddressName;

                            var _LIFXdGDevice_dgc = new DataGridViewComboBoxCell();

                            foreach (var x in DeviceModes.ToList())
                            {
                                if (d.Value != 0 && x.Key == 0) continue;
                                if (x.Key == 11) continue;
                                _LIFXdGDevice_dgc.Items.Add(x.Value);
                            }

                            _LIFXdGDevice_dgc.Value = d.Value == 11 ? DeviceModes[1] : DeviceModes[d.Value];
                            _LIFXdGDevice_dgc.DisplayStyleForCurrentCellOnly = true;
                            _LIFXdGDevice_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;

                            _LIFXdGDevice.Cells[dG_devices.Columns["col_mode"].Index] = _LIFXdGDevice_dgc;

                            dG_devices.Rows.Add(_LIFXdGDevice);
                            _LIFXdGDevice_dgc.ReadOnly = d.Value == 0 ? true : false;
                        }
                    }

                if (HueSDKCalled == 1)
                {
                    if (_hue.HueBulbs > 0)
                    {
                        foreach (var d in _hue.HueBulbsDat.ToList())
                        {
                            Thread.Sleep(1);

                            //HUE Device
                            var state = await _hue.GetLightStateAsync(d.Key);
                            var device = await _hue.GetDeviceVersionAsync(d.Key);

                            _dGmode.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                            _dGmode.DisplayStyleForCurrentCellOnly = true;
                            var LState = _hue.HueStateMemory[d.Key.UniqueId];

                            var _HUEdGDevice = (DataGridViewRow)dG_devices.Rows[0].Clone();
                            _HUEdGDevice.Cells[dG_devices.Columns["col_devicename"].Index].Value = d.Key.Name + " (" +
                                                                                                    d.Key.UniqueId +
                                                                                                    ")";
                            _HUEdGDevice.Cells[dG_devices.Columns["col_devicetype"].Index].Value =
                                d.Key.ModelId + " (Version " + device + ")";
                            _HUEdGDevice.Cells[dG_devices.Columns["col_status"].Index].Value = LState == 0
                                ? "Disabled"
                                : "Enabled";
                            _HUEdGDevice.Cells[dG_devices.Columns["col_state"].Index].Value = LState == 0
                                ? false
                                : true;
                            _HUEdGDevice.Cells[dG_devices.Columns["col_dattype"].Index].Value = "HUE";
                            _HUEdGDevice.Cells[dG_devices.Columns["col_ID"].Index].Value = d.Key.UniqueId;

                            var _HUEdGDevice_dgc = new DataGridViewComboBoxCell();

                            foreach (var x in DeviceModes.ToList())
                            {
                                if (d.Value != 0 && x.Key == 0) continue;
                                if (x.Key == 11) continue;
                                _HUEdGDevice_dgc.Items.Add(x.Value);
                            }

                            _HUEdGDevice_dgc.Value = d.Value == 11 ? DeviceModes[1] : DeviceModes[d.Value];
                            _HUEdGDevice_dgc.DisplayStyleForCurrentCellOnly = true;
                            _HUEdGDevice_dgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;

                            _HUEdGDevice.Cells[dG_devices.Columns["col_mode"].Index] = _HUEdGDevice_dgc;

                            dG_devices.Rows.Add(_HUEdGDevice);
                            _HUEdGDevice_dgc.ReadOnly = d.Value == 0 ? true : false;
                        }
                    }
                }

                DeviceGridStartup = true;
                dG_devices.AllowUserToAddRows = false;
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.ERROR, "EX: " + ex.Message);
            }
        }

        private void dG_devices_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (DeviceGridStartup)
            {
                var editedCell = dG_devices.Rows[e.RowIndex].Cells[e.ColumnIndex];
                var _dattype = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_dattype"].Index].Value;

                if (dG_devices.Columns[e.ColumnIndex].Name == "col_state")
                {
                    var _switch = (bool) editedCell.Value;
                    var _modeX = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_mode"].Index].Value;
                    var _key = DeviceModes.FirstOrDefault(x => x.Value == _modeX).Key;
                    var change = dG_devices.CurrentRow.Cells;

                    if (_dattype == "RazerDeviceKeyboard")
                    {
                        if (_switch)
                            RazerDeviceKeyboard = true;
                        else
                            RazerDeviceKeyboard = false;

                        change[dG_devices.Columns["col_status"].Index].Value = RazerDeviceKeyboard
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = RazerDeviceKeyboard;

                        if (RazerSDKCalled == 1)
                        {
                            _razer.ResetRazerDevices(RazerDeviceKeyboard, RazerDeviceKeypad, RazerDeviceMouse, RazerDeviceMousepad, RazerDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "RazerDeviceMouse")
                    {
                        if (_switch)
                            RazerDeviceMouse = true;
                        else
                            RazerDeviceMouse = false;

                        change[dG_devices.Columns["col_status"].Index].Value = RazerDeviceMouse ? "Enabled" : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = RazerDeviceMouse;

                        if (RazerSDKCalled == 1)
                        {
                            _razer.ResetRazerDevices(RazerDeviceKeyboard, RazerDeviceKeypad, RazerDeviceMouse, RazerDeviceMousepad, RazerDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "RazerDeviceHeadset")
                    {
                        if (_switch)
                            RazerDeviceHeadset = true;
                        else
                            RazerDeviceHeadset = false;

                        change[dG_devices.Columns["col_status"].Index].Value = RazerDeviceHeadset
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = RazerDeviceHeadset;

                        if (RazerSDKCalled == 1)
                        {
                            _razer.ResetRazerDevices(RazerDeviceKeyboard, RazerDeviceKeypad, RazerDeviceMouse, RazerDeviceMousepad, RazerDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "RazerDeviceMousepad")
                    {
                        if (_switch)
                            RazerDeviceMousepad = true;
                        else
                            RazerDeviceMousepad = false;

                        change[dG_devices.Columns["col_status"].Index].Value = RazerDeviceMousepad
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = RazerDeviceMousepad;

                        if (RazerSDKCalled == 1)
                        {
                            _razer.ResetRazerDevices(RazerDeviceKeyboard, RazerDeviceKeypad, RazerDeviceMouse, RazerDeviceMousepad, RazerDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "RazerDeviceKeypad")
                    {
                        if (_switch)
                            RazerDeviceKeypad = true;
                        else
                            RazerDeviceKeypad = false;

                        change[dG_devices.Columns["col_status"].Index].Value = RazerDeviceKeypad
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = RazerDeviceKeypad;

                        if (RazerSDKCalled == 1)
                        {
                            _razer.ResetRazerDevices(RazerDeviceKeyboard, RazerDeviceKeypad, RazerDeviceMouse, RazerDeviceMousepad, RazerDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "CorsairDeviceKeyboard")
                    {
                        if (_switch)
                            CorsairDeviceKeyboard = true;
                        else
                            CorsairDeviceKeyboard = false;

                        change[dG_devices.Columns["col_status"].Index].Value = CorsairDeviceKeyboard
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = CorsairDeviceKeyboard;

                        if (CorsairSDKCalled == 1)
                        {
                            _corsair.ResetCorsairDevices(CorsairDeviceKeyboard, CorsairDeviceKeypad, CorsairDeviceMouse, CorsairDeviceMousepad, CorsairDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "CorsairDeviceMouse")
                    {
                        if (_switch)
                            CorsairDeviceMouse = true;
                        else
                            CorsairDeviceMouse = false;

                        change[dG_devices.Columns["col_status"].Index].Value = CorsairDeviceMouse
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = CorsairDeviceMouse;

                        if (CorsairSDKCalled == 1)
                        {
                            _corsair.ResetCorsairDevices(CorsairDeviceKeyboard, CorsairDeviceKeypad, CorsairDeviceMouse, CorsairDeviceMousepad, CorsairDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "CorsairDeviceHeadset")
                    {
                        if (_switch)
                            CorsairDeviceHeadset = true;
                        else
                            CorsairDeviceHeadset = false;

                        change[dG_devices.Columns["col_status"].Index].Value = CorsairDeviceHeadset
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = CorsairDeviceHeadset;

                        if (CorsairSDKCalled == 1)
                        {
                            _corsair.ResetCorsairDevices(CorsairDeviceKeyboard, CorsairDeviceKeypad, CorsairDeviceMouse, CorsairDeviceMousepad, CorsairDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "CorsairDeviceMousepad")
                    {
                        if (_switch)
                            CorsairDeviceMousepad = true;
                        else
                            CorsairDeviceMousepad = false;

                        change[dG_devices.Columns["col_status"].Index].Value = CorsairDeviceMousepad
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = CorsairDeviceMousepad;

                        if (CorsairSDKCalled == 1)
                        {
                            _corsair.ResetCorsairDevices(CorsairDeviceKeyboard, CorsairDeviceKeypad, CorsairDeviceMouse, CorsairDeviceMousepad, CorsairDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "LogitechDevice")
                    {
                        if (_switch)
                            LogitechDeviceKeyboard = true;
                        else
                            LogitechDeviceKeyboard = false;

                        change[dG_devices.Columns["col_status"].Index].Value = LogitechDeviceKeyboard
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = LogitechDeviceKeyboard;

                        if (LogitechSDKCalled == 1)
                        {
                            _logitech.ResetLogitechDevices(LogitechDeviceKeyboard, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                        }
                    }
                    else if (_dattype == "LIFX")
                    {
                        var _id = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_ID"].Index].Value;
                        if (LifxSDKCalled == 1 && _id != null)
                        {
                            var _bulb = _lifx.LifxDevices[_id];

                            if (_switch)
                            {
                                _lifx.LifxBulbsDat[_bulb] = _lifx.LifxModeMemory[_id];
                                _lifx.LifxStateMemory[_bulb.MacAddressName] = 1;
                                WriteConsole(ConsoleTypes.LIFX, "Enabled LIFX Bulb " + _id);
                            }
                            else
                            {
                                _lifx.LifxModeMemory[_id] = _key;
                                _lifx.LifxStateMemory[_bulb.MacAddressName] = 0;
                                _lifx.LifxBulbsDat[_bulb] = 0;
                                var state = _lifx.LifxBulbsRestore[_bulb];
                                WriteConsole(ConsoleTypes.LIFX, "Disabled LIFX Bulb " + _id);
                                _lifx.SetColorAsync(_bulb, state.Hue, state.Saturation, state.Brightness, state.Kelvin,
                                    TimeSpan.FromMilliseconds(1000));
                                WriteConsole(ConsoleTypes.LIFX, "Restoring LIFX Bulb " + state.Label);
                            }

                            change[dG_devices.Columns["col_status"].Index].Value = _switch ? "Enabled" : "Disabled";
                        }
                    }
                    else if (_dattype == "HUE")
                    {
                        var _id = (string)dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_ID"].Index].Value;
                        if (HueSDKCalled == 1 && _id != null)
                        {
                            var _bulb = _hue.HueDevices[_id];

                            if (_switch)
                            {
                                _hue.HueBulbsDat[_bulb] = _hue.HueModeMemory[_id];
                                _hue.HueStateMemory[_bulb.UniqueId] = 1;
                                WriteConsole(ConsoleTypes.HUE, "Enabled HUE Bulb " + _id);
                            }
                            else
                            {
                                _hue.HueModeMemory[_id] = _key;
                                _hue.HueStateMemory[_bulb.UniqueId] = 0;
                                _hue.HueBulbsDat[_bulb] = 0;
                                var state = _hue.HueBulbsRestore[_bulb];
                                WriteConsole(ConsoleTypes.HUE, "Disabled HUE Bulb " + _id);

                                _hue.SetColorAsync(_bulb, state.Hue, state.Saturation, state.Brightness, state.ColorTemperature,
                                    TimeSpan.FromMilliseconds(1000));

                                WriteConsole(ConsoleTypes.HUE, "Restoring HUE Bulb " + _bulb.Name);
                            }

                            change[dG_devices.Columns["col_status"].Index].Value = _switch ? "Enabled" : "Disabled";
                        }
                    }
                }

                if (dG_devices.Columns[e.ColumnIndex].Name == "col_mode")
                {
                    var _mode = (string) editedCell.Value;
                    var _id = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_ID"].Index].Value;

                    if (LifxSDKCalled == 1 && _id != null && _dattype == "LIFX")
                    {
                        if (DeviceModes.ContainsValue(_mode))
                        {
                            var _key = DeviceModes.FirstOrDefault(x => x.Value == _mode).Key;
                            var _bulb = _lifx.LifxDevices[_id];
                            _lifx.LifxBulbsDat[_bulb] = _key;
                            _lifx.LifxModeMemory[_bulb.MacAddressName] = _key;
                            WriteConsole(ConsoleTypes.LIFX, "Updated Mode of LIFX Bulb " + _id + " to " + _key);
                        }
                    }

                    if (HueSDKCalled == 1 && _id != null && _dattype == "HUE")
                    {
                        if (DeviceModes.ContainsValue(_mode))
                        {
                            var _key = DeviceModes.FirstOrDefault(x => x.Value == _mode).Key;
                            var _bulb = _hue.HueDevices[_id];
                            _hue.HueBulbsDat[_bulb] = _key;
                            _hue.HueModeMemory[_bulb.UniqueId] = _key;
                            WriteConsole(ConsoleTypes.LIFX, "Updated Mode of HUE Bulb " + _id + " to " + _key);
                        }
                    }
                }

                SaveDevices();
                //ResetDeviceDataGrid();

                if (RazerSDKCalled == 1)
                {
                    _razer.ResetRazerDevices(RazerDeviceKeyboard, RazerDeviceKeypad, RazerDeviceMouse, RazerDeviceMousepad, RazerDeviceHeadset, System.Drawing.ColorTranslator.FromHtml(ColorMappings.ColorMapping_BaseColor));
                }
            }
        }

        private void CenterPictureBox(PictureBox picBox, Image picImage)
        {
            /*
            picBox.Image = picImage;
            picBox.Location = new Point((picBox.Parent.ClientSize.Width / 2) - (picImage.Width / 2),
                                        (picBox.Parent.ClientSize.Height / 2) - (picImage.Height / 2));
            picBox.Refresh();
            */
            var Xpos = picBox.Parent.Width / 2 - picImage.Width / 2;
            var ypos = picBox.Parent.Height / 2 - picImage.Height / 2;
            picBox.Location = new Point(Xpos, ypos);
        }

        private void dG_mappings_SelectionChanged(object sender, EventArgs e)
        {
            var _color = dG_mappings.CurrentRow.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor;
            ToggleMappingControls(true);
            mapping_colorEditorManager.Color = _color;
            previewPanel.BackColor = _color;
            //PaletteMappingCurrentSelect = dG_mappings.CurrentRow.Index;
        }

        private void dG_mappings_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            e.PaintParts &= ~DataGridViewPaintParts.Focus;
        }

        private void mapping_colorEditorManager_ColorChanged(object sender, EventArgs e)
        {
            var _PMCS = dG_mappings.CurrentRow;
            var _PCMSID = (string) _PMCS.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value;
            var _PCMSColor = _PMCS.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor;

            previewPanel.BackColor = mapping_colorEditorManager.Color;

            if (_PCMSColor != mapping_colorEditorManager.Color)
            {
                var _data = MappingPalette[_PCMSID];
                string[] data = {_data[0], _data[1], _data[2], ColorTranslator.ToHtml(_PCMSColor)};
                MappingPalette[_PCMSID] = data;

                foreach (var p in typeof(FFXIVColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (p.Name == _PCMSID)
                        p.SetValue(ColorMappings, ColorTranslator.ToHtml(mapping_colorEditorManager.Color));

                DrawMappingsDict();
                _PMCS.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor =
                    mapping_colorEditorManager.Color;
                _PMCS.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.SelectionBackColor =
                    mapping_colorEditorManager.Color;
                SaveColorMappings(0);
            }
        }

        private void btn_palette_undo_Click(object sender, EventArgs e)
        {
            var _PMCS = dG_mappings.CurrentRow;
            var _PCMSID = (string) _PMCS.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value;

            var _CM = new FFXIVColorMappings();
            var _restore = ColorTranslator.ToHtml(Color.Black);

            foreach (var p in typeof(FFXIVColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (p.Name == _PCMSID)
                    _restore = (string) p.GetValue(_CM);

            var restore = ColorTranslator.FromHtml(_restore);
            mapping_colorEditorManager.Color = restore;
        }

        private void loadPaletteButton_Click(object sender, EventArgs e)
        {
            using (FileDialog dialog = new OpenFileDialog
            {
                Filter = PaletteSerializer.DefaultOpenFilter,
                DefaultExt = "pal",
                Title = "Open Palette File"
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    try
                    {
                        IPaletteSerializer serializer;

                        serializer = PaletteSerializer.GetSerializer(dialog.FileName);
                        if (serializer != null)
                        {
                            ColorCollection palette;

                            if (!serializer.CanRead)
                                throw new InvalidOperationException("Serializer does not support reading palettes.");

                            using (var file = File.OpenRead(dialog.FileName))
                            {
                                palette = serializer.Deserialize(file);
                            }

                            if (palette != null)
                            {
                                // we can only display 96 colors in the color grid due to it's size, so if there's more, bin them
                                while (palette.Count > 96)
                                    palette.RemoveAt(palette.Count - 1);

                                // or if we have less, fill in the blanks
                                while (palette.Count < 96)
                                    palette.Add(Color.White);

                                colorGrid1.Colors = palette;
                            }
                        }
                        else
                        {
                            MessageBox.Show(
                                "Sorry, unable to open palette, the file format is not supported or is not recognized.",
                                "Load Palette", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format("Sorry, unable to open palette. {0}", ex.GetBaseException().Message),
                            "Load Palette", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
            }
        }

        private void btn_importChromatics_Click(object sender, EventArgs e)
        {
            ImportColorMappings();
        }

        private void btn_exportChromatics_Click(object sender, EventArgs e)
        {
            ExportColorMappings();
        }

        private void SetupTooltips()
        {
            var TT_btn_import = new ToolTip();
            TT_btn_import.SetToolTip(btn_importChromatics, "Import Chromatics Color Palette");

            var TT_btn_export = new ToolTip();
            TT_btn_export.SetToolTip(btn_exportChromatics, "Export Chromatics Color Palette");

            var TT_btn_loadpalette = new ToolTip();
            TT_btn_loadpalette.SetToolTip(loadPaletteButton, "Import External Color Swatches");

            var TT_btn_palette_undo = new ToolTip();
            TT_btn_palette_undo.SetToolTip(btn_palette_undo, "Restore to Default");
        }

        private void InitSettingsGUI()
        {
            chk_arxtoggle.Checked = ChromaticsSettings.ChromaticsSettings_ARXToggle;
            cb_arx_theme.SelectedIndex = ChromaticsSettings.ChromaticsSettings_ARXTheme;
            mi_arxenable.Checked = ChromaticsSettings.ChromaticsSettings_ARXToggle;
            chk_lccauto.Checked = ChromaticsSettings.ChromaticsSettings_LCCAuto;
            chk_memorycache.Checked = ChromaticsSettings.ChromaticsSettings_MemoryCache;
            chk_azertymode.Checked = ChromaticsSettings.ChromaticsSettings_AZERTYMode;

            chk_castanimatetoggle.Checked = ChromaticsSettings.ChromaticsSettings_CastAnimate;
            chk_castchargetoggle.Checked = ChromaticsSettings.ChromaticsSettings_CastToggle;
            chk_gcdcounttoggle.Checked = ChromaticsSettings.ChromaticsSettings_GCDCountdown;

            chk_keybindtoggle.Checked = ChromaticsSettings.ChromaticsSettings_KeybindToggle;
            chk_jobgaugetoggle.Checked = ChromaticsSettings.ChromaticsSettings_JobGaugeToggle;
            chk_highlighttoggle.Checked = ChromaticsSettings.ChromaticsSettings_KeyHighlights;
            chk_impactflashtog.Checked = ChromaticsSettings.ChromaticsSettings_ImpactToggle;
        }

        private void InitSettingsArxGUI()
        {
            if (cb_arx_mode.Items.Contains(ChromaticsSettings.ChromaticsSettings_ARXMode))
            {
                cb_arx_mode.SelectedItem = ChromaticsSettings.ChromaticsSettings_ARXMode;
            }
            else
            {
                var _ChromaticsSettings = new ChromaticsSettings();
                cb_arx_mode.SelectedItem = _ChromaticsSettings.ChromaticsSettings_ARXMode;
            }

            txt_arx_actip.Text = ChromaticsSettings.ChromaticsSettings_ARXACTIP;
        }

        private void chk_arxtoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_ARXToggle = chk_arxtoggle.Checked;
            SaveChromaticsSettings(1);

            if (chk_arxtoggle.Checked)
            {
                ArxToggle = true;
                cb_arx_theme.Enabled = true;
                cb_arx_mode.Enabled = true;

                if (ArxSDKCalled == 0)
                {
                    _arx = LogitechArxInterface.InitializeArxSDK();
                    if (_arx != null)
                    {
                        ArxSDK = true;
                        ArxSDKCalled = 1;
                        ArxState = 0;
                        
                        WriteConsole(ConsoleTypes.ARX, "ARX SDK Enabled");

                        LoadArxPlugins();

                        var theme = "light";
                        var themeid = cb_arx_theme.SelectedIndex;

                        switch (themeid)
                        {
                            case 0:
                                theme = "light";
                                break;
                            case 1:
                                theme = "dark";
                                break;
                            case 2:
                                theme = "grey";
                                break;
                            case 3:
                                theme = "black";
                                break;
                            case 4:
                                theme = "cycle";
                                break;
                        }

                        _arx.ArxUpdateTheme(theme);
                    }
                }
            }
            else
            {
                ArxToggle = false;
                cb_arx_theme.Enabled = false;
                cb_arx_mode.Enabled = false;

                if (plugs.Count > 0)
                {
                    foreach (var plugin in plugs)
                    {
                        if (cb_arx_mode.Items.Contains(plugin))
                        {
                            cb_arx_mode.Items.Remove(plugin);
                        }
                    }
                }

                if (ArxSDKCalled == 1)
                {
                    _arx.ShutdownArx();
                    ArxSDKCalled = 0;
                    WriteConsole(ConsoleTypes.ARX, "ARX SDK Disabled");
                }
            }
        }

        private void cb_arx_theme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ArxSDKCalled != 1) return;
            if (startup == false) return;
            var theme = "light";
            var themeid = cb_arx_theme.SelectedIndex;
            ChromaticsSettings.ChromaticsSettings_ARXTheme = cb_arx_theme.SelectedIndex;
            SaveChromaticsSettings(1);

            switch (themeid)
            {
                case 0:
                    theme = "light";
                    break;
                case 1:
                    theme = "dark";
                    break;
                case 2:
                    theme = "grey";
                    break;
                case 3:
                    theme = "black";
                    break;
                case 4:
                    theme = "cycle";
                    break;
            }

            _arx.ArxUpdateTheme(theme);
        }

        private void cb_arx_mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ArxSDKCalled != 1) return;
            if (startup == false) return;
            ChromaticsSettings.ChromaticsSettings_ARXMode = cb_arx_mode.SelectedItem.ToString();
            SaveChromaticsSettings(1);

            if (cb_arx_mode.SelectedIndex < 4)
            {
                ArxState = cb_arx_mode.SelectedIndex+1;

                switch (ArxState)
                {
                    case 1:
                        _arx.ArxSetIndex("playerhud.html");
                        break;
                    case 2:
                        _arx.ArxSetIndex("partylist.html");
                        break;
                    case 3:
                        _arx.ArxSetIndex("mapdata.html");
                        break;
                    case 4:
                        _arx.ArxSetIndex("act.html");

                        var changed = txt_arx_actip.Text;
                        if (changed.EndsWith("/"))
                        {
                            changed = changed.Substring(0, changed.Length - 1);
                        }
                        
                        _arx.ArxSendACTInfo(changed, 8085);
                        break;
                }
            }
            else
            {
                var getPlugin = cb_arx_mode.SelectedItem + ".html";
                ArxState = 100;
                _arx.ArxSetIndex(getPlugin);
            }
            
            WriteConsole(ConsoleTypes.ARX, "ARX Template Changed: " + cb_arx_mode.SelectedItem);
        }

        private void rtb_debug_TextChanged(object sender, EventArgs e)
        {
            rtb_debug.SelectionStart = rtb_debug.Text.Length;
            rtb_debug.ScrollToCaret();
        }

        private void showwindow_Click(object sender, EventArgs e)
        {
            if (!allowVisible)
            {
                allowVisible = true;
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        private void mi_updatecheck_Click(object sender, EventArgs e)
        {
            CheckUpdates(1);
        }

        private void mi_winstart_Click(object sender, EventArgs e)
        {
            if (mi_winstart.Checked)
            {
                // Add the value in the registry so that the application runs at startup
                rkApp.SetValue("Chromatics", Application.ExecutablePath.ToString());
            }
            else
            {
                // Remove the value from the registry so that the application doesn't start
                rkApp.DeleteValue("Chromatics", false);
            }

            chk_startupenable.Checked = mi_winstart.Checked;
        }

        private void enableeffects_Click(object sender, EventArgs e)
        {
            //Enable effects
            if (!mi_effectsenable.Checked)
            {
                if (RazerSDK)
                {
                    RazerSDKCalled = 0;
                }
                if (LogitechSDK)
                {
                    LogitechSDKCalled = 0;
                }
                if (CorsairSDK)
                {
                    CorsairSDKCalled = 0;
                }
                if (LifxSDK)
                {
                    LifxSDKCalled = 0;
                }
                if (HueSDK)
                {
                    HueSDKCalled = 0;
                }
            }
            else
            {
                if (RazerSDK)
                {
                    RazerSDKCalled = 1;
                }
                if (LogitechSDK)
                {
                    LogitechSDKCalled = 1;
                }
                if (CorsairSDK)
                {
                    CorsairSDKCalled = 1;
                }
                if (LifxSDK)
                {
                    LifxSDKCalled = 1;
                }
                if (HueSDK)
                {
                    HueSDKCalled = 1;
                }
            }

            ResetDeviceDataGrid();
        }

        private void enablearx_Click(object sender, EventArgs e)
        {
            if (mi_arxenable.Checked)
            {
                chk_arxtoggle.Checked = true;
            }
            else
            {
                chk_arxtoggle.Checked = false;
            }
        }

        private void notify_master_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!allowVisible)
            {
                allowVisible = true;
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        private void txt_arx_actip_TextChanged(object sender, EventArgs e)
        {
            if (startup)
            {
                var changed = txt_arx_actip.Text;
                if (changed.EndsWith("/"))
                {
                    changed = changed.Substring(0, changed.Length - 1);
                }

                txt_arx_actip.Text = changed;
                _arx.ArxSendACTInfo(changed, 8085);

                ChromaticsSettings.ChromaticsSettings_ARXACTIP = txt_arx_actip.Text;
                SaveChromaticsSettings(1);
            }
        }

        private void chk_lccenable_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            if (chk_lccenable.Checked)
            {
                ToggleLCCMode(true);
            }
            else
            {
                ToggleLCCMode(false);
            }
        }

        private void chk_lccauto_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_LCCAuto = chk_lccauto.Checked;
            SaveChromaticsSettings(1);
        }

        private void btn_lccrestore_Click(object sender, EventArgs e)
        {
            if (LogitechSDKCalled == 0)
                return;

            DialogResult lccrestore_check = MessageBox.Show("Are you sure you wish to restore LGS to its default settings. This should only be done as a last resort.", "Restore LGS Settings to Default", MessageBoxButtons.OKCancel);
            if (lccrestore_check == DialogResult.OK)
            {
                try
                {
                    while (Process.GetProcessesByName("ffxiv_dx11").Length > 0)
                    {
                        DialogResult lccrestore_warning = MessageBox.Show("You must close Final Fantasy XIV before using restore.", "Please close Final Fantasy XIV", MessageBoxButtons.RetryCancel);
                        if (lccrestore_warning == DialogResult.Cancel)
                        {
                            return;
                        }
                    }

                    if (File.Exists(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                    {
                        File.Delete(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll");
                    }

                    if (File.Exists(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled"))
                    {
                        File.Delete(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled");
                    }

                    var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    var path = enviroment + @"/LogitechLed.dll";

                    File.Copy(path, LgsInstall + @"\SDK\LED\x64\LogitechLed.dll", true);
                    WriteConsole(ConsoleTypes.LOGITECH, "LGS has been restored to its default settings.");

                    this.chk_lccenable.CheckedChanged -= new System.EventHandler(this.chk_lccenable_CheckedChanged);
                    chk_lccenable.Checked = false;
                    this.chk_lccenable.CheckedChanged += new System.EventHandler(this.chk_lccenable_CheckedChanged);
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.ERROR, "An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                    return;
                }

            }
            else if (lccrestore_check == DialogResult.Cancel)
            {
                return;
            }
        }

        private void ToggleLCCMode([Optional] bool force, [Optional] bool antilog)
        {
            if (LogitechSDKCalled == 0)
                return;

            var _force = false;

            if (!force)
                _force = true;


            if ((chk_lccenable.Checked || force) && !_force)
            {
                //Enable LCC
                
                if (File.Exists(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                {
                    try
                    {
                        //File.Copy(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll", LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", true);
                        //File.Delete(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll");
                        Microsoft.VisualBasic.FileIO.FileSystem.RenameFile(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll", "LogitechLed.dll.disabled");
                        
                    }
                    catch (Exception ex)
                    {
                        if (!antilog)
                            WriteConsole(ConsoleTypes.ERROR, "An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                        return;
                    }

                    if (!antilog)
                        WriteConsole(ConsoleTypes.LOGITECH, "Logitech Conflict Mode Enabled.");
                }
                else
                {
                    if (!antilog)
                        WriteConsole(ConsoleTypes.ERROR, "An Error occurred trying to enable Logitech Conflict Mode. Error: LGS SDK Library not found (A).");
                    return;
                }
            }
            else
            {
                //Disable LCC
                if (File.Exists(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled"))
                {
                    try
                    {
                        //File.Copy(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", LgsInstall + @"\SDK\LED\x64\LogitechLed.dll", true);
                        //File.Delete(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled");
                        Microsoft.VisualBasic.FileIO.FileSystem.RenameFile(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", "LogitechLed.dll");
                                                
                    }
                    catch (Exception ex)
                    {
                        if (!antilog)
                            WriteConsole(ConsoleTypes.ERROR, "An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                        return;
                    }

                    if (!antilog)
                        WriteConsole(ConsoleTypes.LOGITECH, "Logitech Conflict Mode Disabled.");
                }
                else
                {
                    if (!antilog)
                        WriteConsole(ConsoleTypes.ERROR, "An Error occurred trying to disable Logitech Conflict Mode. Error: LGS SDK Library not found (B).");
                    return;
                }
            }
        }

        private void chk_memorycache_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_MemoryCache = chk_memorycache.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_azertymode_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_AZERTYMode = chk_azertymode.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_startupenable_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_startupenable.Checked)
            {
                // Add the value in the registry so that the application runs at startup
                rkApp.SetValue("Chromatics", Application.ExecutablePath.ToString());
            }
            else
            {
                // Remove the value from the registry so that the application doesn't start
                rkApp.DeleteValue("Chromatics", false);
            }

            mi_winstart.Checked = chk_startupenable.Checked;
        }

        private void chk_castchargetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_CastToggle = chk_castchargetoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_castanimatetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_CastAnimate = chk_castanimatetoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_gcdcounttoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_GCDCountdown = chk_gcdcounttoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_highlighttoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_KeyHighlights = chk_highlighttoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_jobgaugetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_JobGaugeToggle = chk_jobgaugetoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_keybindtoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_KeybindToggle = chk_keybindtoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_impactflashtog_CheckedChanged(object sender, EventArgs e)
        {
            if (startup == false) return;

            ChromaticsSettings.ChromaticsSettings_ImpactToggle = chk_impactflashtog.Checked;
            SaveChromaticsSettings(1);
        }

        private delegate void ResetGridDelegate();

        private delegate void ResetMappingsDelegate();
    }
}