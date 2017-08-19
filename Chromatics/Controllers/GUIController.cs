using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Chromatics.Datastore;
using Chromatics.LCDInterfaces;
using Cyotek.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace Chromatics
{
    partial class Chromatics : ILogWrite
    {
        private readonly DataGridViewComboBoxColumn _dGmode = new DataGridViewComboBoxColumn();

        private readonly Dictionary<DeviceModeTypes, string> _deviceModes = new Dictionary<DeviceModeTypes, string>
        {
            //Keys
            {DeviceModeTypes.Disabled, "Disabled"},
            {DeviceModeTypes.Standby, "Standby"},
            {DeviceModeTypes.DefaultColor, "Default Color"},
            {DeviceModeTypes.HighlightColor, "Highlight Colour"},
            {DeviceModeTypes.EnmityTracker, "Enmity Tracker"},
            {DeviceModeTypes.TargetHp, "Target HP"},
            {DeviceModeTypes.StatusEffects, "Status Effects"},
            {DeviceModeTypes.HpTracker, "HP Tracker"},
            {DeviceModeTypes.MpTracker, "MP Tracker"},
            {DeviceModeTypes.TpTracker, "TP Tracker"},
            {DeviceModeTypes.Castbar, "Castbar"},
            {DeviceModeTypes.DutyFinder, "Duty Finder Bell"},
            {DeviceModeTypes.ChromaticsDefault, "Chromatics Default"}
        };

        private readonly Dictionary<string, string[]> _mappingPalette = new Dictionary<string, string[]>
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
            {"ColorMapping_Emnity0", new[] {"Minimal Enmity", "4", "Black", "White"}},
            {"ColorMapping_Emnity1", new[] {"Low Enmity", "4", "Black", "White"}},
            {"ColorMapping_Emnity2", new[] {"Medium Enmity", "4", "Black", "White"}},
            {"ColorMapping_Emnity3", new[] {"High Enmity", "4", "Black", "White"}},
            {"ColorMapping_Emnity4", new[] {"Max Enmity", "4", "Black", "White"}},
            {"ColorMapping_NoEmnity", new[] {"No Enmity", "4", "Black", "White"}},
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
            {"ColorMapping_HotbarNotAvailable", new[] {"Keybind Not Available", "7", "Black", "White"}},
            {"ColorMapping_DutyFinderBell", new[] {"Duty Finder Bell", "8", "Black", "White"}}
        };


        private readonly Dictionary<int, string> _paletteCategories = new Dictionary<int, string>
        {
            //Keys
            {0, "All"},
            {1, "Chromatics"},
            {2, "Player Stats"},
            {3, "Abilities"},
            {4, "Enmity/Aggro"},
            {5, "Target/Enemy"},
            {6, "Status Effects"},
            {7, "Cooldowns/Keybinds"},
            {8, "Notifications"}
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
            foreach (var c in _paletteCategories)
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

            foreach (var palette in _mappingPalette)
            {
                var paletteItem = (DataGridViewRow) dG_mappings.Rows[0].Clone();
                paletteItem.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value = palette.Key;
                paletteItem.Cells[dG_mappings.Columns["mappings_col_cat"].Index].Value = palette.Value[1];
                paletteItem.Cells[dG_mappings.Columns["mappings_col_type"].Index].Value = palette.Value[0];

                var paletteBtn = new DataGridViewTextBoxCell();
                paletteBtn.Style.BackColor = ColorTranslator.FromHtml(palette.Value[2]);
                paletteBtn.Style.SelectionBackColor = ColorTranslator.FromHtml(palette.Value[2]);

                paletteBtn.Value = "";
                paletteItem.Cells[dG_mappings.Columns["mappings_col_color"].Index] = paletteBtn;
                dG_mappings.Rows.Add(paletteItem);
                paletteBtn.ReadOnly = true;
            }

            dG_mappings.AllowUserToAddRows = false;
            MappingGridStartup = true;
        }

        private void DrawMappingsDict()
        {
            //PropertyInfo[] _FFXIVColorMappings = typeof(FFXIVColorMappings).GetProperties();

            //ColorMappings
            foreach (var p in typeof(FfxivColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var var = p.Name;
                var color = (string) p.GetValue(ColorMappings);
                var _data = _mappingPalette[var];
                string[] data = {_data[0], _data[1], color, _data[3]};
                _mappingPalette[var] = data;
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


                if (RazerSdkCalled == 1)
                {
                    //Keyboard
                    var rzDgKeyboard = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    //DataGridViewRow _RzDgKeyboard = new DataGridViewRow();
                    rzDgKeyboard.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Keyboard";
                    rzDgKeyboard.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Keyboard";
                    rzDgKeyboard.Cells[dG_devices.Columns["col_status"].Index].Value = _razerDeviceKeyboard
                        ? "Enabled"
                        : "Disabled";
                    rzDgKeyboard.Cells[dG_devices.Columns["col_state"].Index].Value = _razerDeviceKeyboard;
                    rzDgKeyboard.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceKeyboard";

                    var rzDgKeyboardDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        rzDgKeyboardDgc.Items.Add(d.Value);

                    rzDgKeyboardDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    rzDgKeyboardDgc.DisplayStyleForCurrentCellOnly = true;
                    rzDgKeyboardDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
                    rzDgKeyboard.Cells[dG_devices.Columns["col_mode"].Index] = rzDgKeyboardDgc;

                    dG_devices.Rows.Add(rzDgKeyboard);
                    rzDgKeyboardDgc.ReadOnly = true;

                    //Mouse
                    var rzDgMouse = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    rzDgMouse.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Mouse";
                    rzDgMouse.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Mouse";
                    rzDgMouse.Cells[dG_devices.Columns["col_status"].Index].Value = _razerDeviceMouse
                        ? "Enabled"
                        : "Disabled";
                    rzDgMouse.Cells[dG_devices.Columns["col_state"].Index].Value = _razerDeviceMouse;
                    rzDgMouse.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceMouse";

                    var rzDgMouseDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        rzDgMouseDgc.Items.Add(d.Value);

                    rzDgMouseDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    rzDgMouseDgc.DisplayStyleForCurrentCellOnly = true;
                    rzDgMouseDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    rzDgMouse.Cells[dG_devices.Columns["col_mode"].Index] = rzDgMouseDgc;

                    dG_devices.Rows.Add(rzDgMouse);
                    rzDgMouseDgc.ReadOnly = true;

                    //Headset
                    var rzDgHeadset = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    rzDgHeadset.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Headset";
                    rzDgHeadset.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Headset";
                    rzDgHeadset.Cells[dG_devices.Columns["col_status"].Index].Value = _razerDeviceHeadset
                        ? "Enabled"
                        : "Disabled";
                    rzDgHeadset.Cells[dG_devices.Columns["col_state"].Index].Value = _razerDeviceHeadset;
                    rzDgHeadset.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceHeadset";

                    var rzDgHeadsetDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        rzDgHeadsetDgc.Items.Add(d.Value);

                    rzDgHeadsetDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    rzDgHeadsetDgc.DisplayStyleForCurrentCellOnly = true;
                    rzDgHeadsetDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    rzDgHeadset.Cells[dG_devices.Columns["col_mode"].Index] = rzDgHeadsetDgc;

                    dG_devices.Rows.Add(rzDgHeadset);
                    rzDgHeadsetDgc.ReadOnly = true;

                    //Mousepad
                    var rzDgMousepad = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    rzDgMousepad.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Mousepad";
                    rzDgMousepad.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Mousepad";
                    rzDgMousepad.Cells[dG_devices.Columns["col_status"].Index].Value = _razerDeviceMousepad
                        ? "Enabled"
                        : "Disabled";
                    rzDgMousepad.Cells[dG_devices.Columns["col_state"].Index].Value = _razerDeviceMousepad;
                    rzDgMousepad.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceMousepad";

                    var rzDgMousepadDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        rzDgMousepadDgc.Items.Add(d.Value);

                    rzDgMousepadDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    rzDgMousepadDgc.DisplayStyleForCurrentCellOnly = true;
                    rzDgMousepadDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    rzDgMousepad.Cells[dG_devices.Columns["col_mode"].Index] = rzDgMousepadDgc;

                    dG_devices.Rows.Add(rzDgMousepad);
                    rzDgMousepadDgc.ReadOnly = true;

                    //Keypad
                    var rzDgKeypad = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    rzDgKeypad.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Razer Keypad";
                    rzDgKeypad.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Keypad";
                    rzDgKeypad.Cells[dG_devices.Columns["col_status"].Index].Value = _razerDeviceKeypad
                        ? "Enabled"
                        : "Disabled";
                    rzDgKeypad.Cells[dG_devices.Columns["col_state"].Index].Value = _razerDeviceKeypad;
                    rzDgKeypad.Cells[dG_devices.Columns["col_dattype"].Index].Value = "RazerDeviceKeypad";

                    var rzDgKeypadDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        rzDgKeypadDgc.Items.Add(d.Value);

                    rzDgKeypadDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    rzDgKeypadDgc.DisplayStyleForCurrentCellOnly = true;
                    rzDgKeypadDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    rzDgKeypad.Cells[dG_devices.Columns["col_mode"].Index] = rzDgKeypadDgc;

                    dG_devices.Rows.Add(rzDgKeypad);
                    rzDgKeypadDgc.ReadOnly = true;

                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                }

                if (CorsairSdkCalled == 1)
                {
                    //Keyboard
                    var corsairDgKeyboard = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    corsairDgKeyboard.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Corsair Keyboard";
                    corsairDgKeyboard.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Keyboard";
                    corsairDgKeyboard.Cells[dG_devices.Columns["col_status"].Index].Value = _corsairDeviceKeyboard
                        ? "Enabled"
                        : "Disabled";
                    corsairDgKeyboard.Cells[dG_devices.Columns["col_state"].Index].Value = _corsairDeviceKeyboard;
                    corsairDgKeyboard.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CorsairDeviceKeyboard";

                    var corsairDgKeyboardDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        corsairDgKeyboardDgc.Items.Add(d.Value);

                    corsairDgKeyboardDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    corsairDgKeyboardDgc.DisplayStyleForCurrentCellOnly = true;
                    corsairDgKeyboardDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    corsairDgKeyboard.Cells[dG_devices.Columns["col_mode"].Index] = corsairDgKeyboardDgc;

                    dG_devices.Rows.Add(corsairDgKeyboard);
                    corsairDgKeyboardDgc.ReadOnly = true;

                    //Mouse
                    var corsairDgMouse = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    corsairDgMouse.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Corsair Mouse";
                    corsairDgMouse.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Mouse";
                    corsairDgMouse.Cells[dG_devices.Columns["col_status"].Index].Value = _corsairDeviceMouse
                        ? "Enabled"
                        : "Disabled";
                    corsairDgMouse.Cells[dG_devices.Columns["col_state"].Index].Value = _corsairDeviceMouse;
                    corsairDgMouse.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CorsairDeviceMouse";

                    var corsairDgMouseDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        corsairDgMouseDgc.Items.Add(d.Value);

                    corsairDgMouseDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    corsairDgMouseDgc.DisplayStyleForCurrentCellOnly = true;
                    corsairDgMouseDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    corsairDgMouse.Cells[dG_devices.Columns["col_mode"].Index] = corsairDgMouseDgc;

                    dG_devices.Rows.Add(corsairDgMouse);
                    corsairDgMouseDgc.ReadOnly = true;

                    //Headset
                    var corsairDgHeadset = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    corsairDgHeadset.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Corsair Headset";
                    corsairDgHeadset.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Headset";
                    corsairDgHeadset.Cells[dG_devices.Columns["col_status"].Index].Value = _corsairDeviceHeadset
                        ? "Enabled"
                        : "Disabled";
                    corsairDgHeadset.Cells[dG_devices.Columns["col_state"].Index].Value = _corsairDeviceHeadset;
                    corsairDgHeadset.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CorsairDeviceHeadset";

                    var corsairDgHeadsetDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        corsairDgHeadsetDgc.Items.Add(d.Value);

                    corsairDgHeadsetDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    corsairDgHeadsetDgc.DisplayStyleForCurrentCellOnly = true;
                    corsairDgHeadsetDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    corsairDgHeadset.Cells[dG_devices.Columns["col_mode"].Index] = corsairDgHeadsetDgc;

                    dG_devices.Rows.Add(corsairDgHeadset);
                    corsairDgHeadsetDgc.ReadOnly = true;

                    //Mousepad
                    var corsairDgMousepad = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    corsairDgMousepad.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Corsair Mousepad";
                    corsairDgMousepad.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Mousepad";
                    corsairDgMousepad.Cells[dG_devices.Columns["col_status"].Index].Value = _corsairDeviceMousepad
                        ? "Enabled"
                        : "Disabled";
                    corsairDgMousepad.Cells[dG_devices.Columns["col_state"].Index].Value = _corsairDeviceMousepad;
                    corsairDgMousepad.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CorsairDeviceMousepad";

                    var corsairDgMousepadDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        corsairDgMousepadDgc.Items.Add(d.Value);

                    corsairDgMousepadDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    corsairDgMousepadDgc.DisplayStyleForCurrentCellOnly = true;
                    corsairDgMousepadDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    corsairDgMousepad.Cells[dG_devices.Columns["col_mode"].Index] = corsairDgMousepadDgc;

                    dG_devices.Rows.Add(corsairDgMousepad);
                    corsairDgMousepadDgc.ReadOnly = true;

                    _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                        _corsairDeviceMousepad, _corsairDeviceHeadset,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                }

                if (LogitechSdkCalled == 1)
                {
                    var logitechDgDevices = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    logitechDgDevices.Cells[dG_devices.Columns["col_devicename"].Index].Value = "Logitech Devices";
                    logitechDgDevices.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Multiple Devices";
                    logitechDgDevices.Cells[dG_devices.Columns["col_status"].Index].Value = _logitechDeviceKeyboard
                        ? "Enabled"
                        : "Disabled";
                    logitechDgDevices.Cells[dG_devices.Columns["col_state"].Index].Value = _logitechDeviceKeyboard;
                    logitechDgDevices.Cells[dG_devices.Columns["col_dattype"].Index].Value = "LogitechDevice";

                    var logitechDgDevicesDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        logitechDgDevicesDgc.Items.Add(d.Value);

                    logitechDgDevicesDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    logitechDgDevicesDgc.DisplayStyleForCurrentCellOnly = true;
                    logitechDgDevicesDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    logitechDgDevices.Cells[dG_devices.Columns["col_mode"].Index] = logitechDgDevicesDgc;

                    dG_devices.Rows.Add(logitechDgDevices);
                    logitechDgDevicesDgc.ReadOnly = true;

                    _logitech.ResetLogitechDevices(_logitechDeviceKeyboard,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                }

                if (CoolermasterSdkCalled == 1)
                {
                    var coolermasterDgDevices = (DataGridViewRow) dG_devices.Rows[0].Clone();
                    coolermasterDgDevices.Cells[dG_devices.Columns["col_devicename"].Index].Value =
                        "Coolermaster Devices";
                    coolermasterDgDevices.Cells[dG_devices.Columns["col_devicetype"].Index].Value = "Multiple Devices";
                    coolermasterDgDevices.Cells[dG_devices.Columns["col_status"].Index].Value =
                        _coolermasterDeviceKeyboard
                            ? "Enabled"
                            : "Disabled";
                    coolermasterDgDevices.Cells[dG_devices.Columns["col_state"].Index].Value =
                        _coolermasterDeviceKeyboard;
                    coolermasterDgDevices.Cells[dG_devices.Columns["col_dattype"].Index].Value = "CoolermasterDevice";

                    var coolermasterDgDevicesDgc = new DataGridViewComboBoxCell();
                    foreach (var d in _deviceModes)
                        coolermasterDgDevicesDgc.Items.Add(d.Value);

                    coolermasterDgDevicesDgc.Value = _deviceModes[DeviceModeTypes.ChromaticsDefault];
                    coolermasterDgDevicesDgc.DisplayStyleForCurrentCellOnly = true;
                    coolermasterDgDevicesDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;

                    coolermasterDgDevices.Cells[dG_devices.Columns["col_mode"].Index] = coolermasterDgDevicesDgc;

                    dG_devices.Rows.Add(coolermasterDgDevices);
                    coolermasterDgDevicesDgc.ReadOnly = true;

                    _coolermaster.ResetCoolermasterDevices(_coolermasterDeviceKeyboard, _coolermasterDeviceMouse,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                }

                if (LifxSdkCalled == 1)
                    if (_lifx.LifxBulbs > 0)
                        foreach (var d in _lifx.LifxBulbsDat.ToList())
                        {
                            Thread.Sleep(1);

                            //LIFX Device
                            var state = await _lifx.GetLightStateAsync(d.Key);
                            var device = await _lifx.GetDeviceVersionAsync(d.Key);

                            _dGmode.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                            _dGmode.DisplayStyleForCurrentCellOnly = true;
                            var lState = _lifx.LifxStateMemory[d.Key.MacAddressName];

                            var lifxdGDevice = (DataGridViewRow) dG_devices.Rows[0].Clone();
                            lifxdGDevice.Cells[dG_devices.Columns["col_devicename"].Index].Value = state.Label + " (" +
                                                                                                    d.Key
                                                                                                        .MacAddressName +
                                                                                                    ")";
                            lifxdGDevice.Cells[dG_devices.Columns["col_devicetype"].Index].Value =
                                _lifx.LifXproductids[device.Product] + " (Version " + device.Version + ")";
                            lifxdGDevice.Cells[dG_devices.Columns["col_status"].Index].Value = lState == 0
                                ? "Disabled"
                                : "Enabled";
                            lifxdGDevice.Cells[dG_devices.Columns["col_state"].Index].Value = lState == 0
                                ? false
                                : true;
                            lifxdGDevice.Cells[dG_devices.Columns["col_dattype"].Index].Value = "LIFX";
                            lifxdGDevice.Cells[dG_devices.Columns["col_ID"].Index].Value = d.Key.MacAddressName;

                            var lifxdGDeviceDgc = new DataGridViewComboBoxCell();

                            foreach (var x in _deviceModes.ToList())
                            {
                                if (d.Value != 0 && x.Key == DeviceModeTypes.Disabled) continue;
                                if (x.Key == DeviceModeTypes.ChromaticsDefault) continue;
                                lifxdGDeviceDgc.Items.Add(x.Value);
                            }

                            lifxdGDeviceDgc.Value = d.Value == DeviceModeTypes.ChromaticsDefault
                                ? _deviceModes[DeviceModeTypes.Standby]
                                : _deviceModes[d.Value];
                            lifxdGDeviceDgc.DisplayStyleForCurrentCellOnly = true;
                            lifxdGDeviceDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;

                            lifxdGDevice.Cells[dG_devices.Columns["col_mode"].Index] = lifxdGDeviceDgc;

                            //Check for duplicates

                            /*
                            var duplicate = false;

                            if (DeviceGridStartup)
                            {
                                foreach (DataGridViewRow row in dG_devices.Rows)
                                {
                                    if (row.Cells[dG_devices.Columns["col_ID"].Index].Value.ToString().Contains(d.Key.MacAddressName))
                                    {
                                        duplicate = true;
                                        break;
                                    }
                                    
                                }
                            }

                            if (duplicate) continue;
                            */

                            dG_devices.Rows.Add(lifxdGDevice);
                            lifxdGDeviceDgc.ReadOnly = d.Value == 0 ? true : false;
                        }

                if (HueSdkCalled == 1)
                    if (_hue.HueBulbs > 0)
                        foreach (var d in _hue.HueBulbsDat.ToList())
                        {
                            Thread.Sleep(1);

                            //HUE Device
                            var state = await _hue.GetLightStateAsync(d.Key);
                            var device = await _hue.GetDeviceVersionAsync(d.Key);

                            _dGmode.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                            _dGmode.DisplayStyleForCurrentCellOnly = true;
                            var lState = _hue.HueStateMemory[d.Key.UniqueId];

                            var huedGDevice = (DataGridViewRow) dG_devices.Rows[0].Clone();
                            huedGDevice.Cells[dG_devices.Columns["col_devicename"].Index].Value = d.Key.Name + " (" +
                                                                                                   d.Key.UniqueId +
                                                                                                   ")";
                            huedGDevice.Cells[dG_devices.Columns["col_devicetype"].Index].Value =
                                d.Key.ModelId + " (Version " + device + ")";
                            huedGDevice.Cells[dG_devices.Columns["col_status"].Index].Value = lState == 0
                                ? "Disabled"
                                : "Enabled";
                            huedGDevice.Cells[dG_devices.Columns["col_state"].Index].Value = lState == 0
                                ? false
                                : true;
                            huedGDevice.Cells[dG_devices.Columns["col_dattype"].Index].Value = "HUE";
                            huedGDevice.Cells[dG_devices.Columns["col_ID"].Index].Value = d.Key.UniqueId;

                            var huedGDeviceDgc = new DataGridViewComboBoxCell();

                            foreach (var x in _deviceModes.ToList())
                            {
                                if (d.Value != 0 && x.Key == DeviceModeTypes.Disabled) continue;
                                if (x.Key == DeviceModeTypes.ChromaticsDefault) continue;
                                huedGDeviceDgc.Items.Add(x.Value);
                            }

                            huedGDeviceDgc.Value = d.Value == DeviceModeTypes.ChromaticsDefault
                                ? _deviceModes[DeviceModeTypes.Standby]
                                : _deviceModes[d.Value];
                            huedGDeviceDgc.DisplayStyleForCurrentCellOnly = true;
                            huedGDeviceDgc.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;

                            huedGDevice.Cells[dG_devices.Columns["col_mode"].Index] = huedGDeviceDgc;

                            //Check for duplicates
                            /*
                            var duplicate = false;

                            if (DeviceGridStartup)
                            {
                                foreach (DataGridViewRow row in dG_devices.Rows)
                                {
                                    if (row.Cells[dG_devices.Columns["col_ID"].Index].Value.ToString().Contains(d.Key.UniqueId))
                                    {
                                        duplicate = true;
                                        break;
                                    }

                                }
                            }

                            if (duplicate) continue;
                            */
                            dG_devices.Rows.Add(huedGDevice);
                            huedGDeviceDgc.ReadOnly = d.Value == 0 ? true : false;
                        }

                DeviceGridStartup = true;
                dG_devices.AllowUserToAddRows = false;
            }
            catch (Exception ex)
            {
                WriteConsole(ConsoleTypes.Error, "EX: " + ex.Message);
                WriteConsole(ConsoleTypes.Error, "EX: " + ex.StackTrace);
            }
        }

        private void dG_devices_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (DeviceGridStartup)
            {
                var editedCell = dG_devices.Rows[e.RowIndex].Cells[e.ColumnIndex];
                var dattype = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_dattype"].Index]
                    .Value;

                if (dG_devices.Columns[e.ColumnIndex].Name == "col_state")
                {
                    var _switch = (bool) editedCell.Value;
                    var modeX = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_mode"].Index].Value;
                    var key = _deviceModes.FirstOrDefault(x => x.Value == modeX).Key;
                    var change = dG_devices.CurrentRow.Cells;

                    if (dattype == "RazerDeviceKeyboard")
                    {
                        if (_switch)
                            _razerDeviceKeyboard = true;
                        else
                            _razerDeviceKeyboard = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _razerDeviceKeyboard
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _razerDeviceKeyboard;

                        if (RazerSdkCalled == 1)
                            _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                                _razerDeviceMousepad, _razerDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "RazerDeviceMouse")
                    {
                        if (_switch)
                            _razerDeviceMouse = true;
                        else
                            _razerDeviceMouse = false;

                        change[dG_devices.Columns["col_status"].Index].Value =
                            _razerDeviceMouse ? "Enabled" : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _razerDeviceMouse;

                        if (RazerSdkCalled == 1)
                            _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                                _razerDeviceMousepad, _razerDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "RazerDeviceHeadset")
                    {
                        if (_switch)
                            _razerDeviceHeadset = true;
                        else
                            _razerDeviceHeadset = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _razerDeviceHeadset
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _razerDeviceHeadset;

                        if (RazerSdkCalled == 1)
                            _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                                _razerDeviceMousepad, _razerDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "RazerDeviceMousepad")
                    {
                        if (_switch)
                            _razerDeviceMousepad = true;
                        else
                            _razerDeviceMousepad = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _razerDeviceMousepad
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _razerDeviceMousepad;

                        if (RazerSdkCalled == 1)
                            _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                                _razerDeviceMousepad, _razerDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "RazerDeviceKeypad")
                    {
                        if (_switch)
                            _razerDeviceKeypad = true;
                        else
                            _razerDeviceKeypad = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _razerDeviceKeypad
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _razerDeviceKeypad;

                        if (RazerSdkCalled == 1)
                            _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                                _razerDeviceMousepad, _razerDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "CorsairDeviceKeyboard")
                    {
                        if (_switch)
                            _corsairDeviceKeyboard = true;
                        else
                            _corsairDeviceKeyboard = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _corsairDeviceKeyboard
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _corsairDeviceKeyboard;

                        if (CorsairSdkCalled == 1)
                            _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                                _corsairDeviceMousepad, _corsairDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "CorsairDeviceMouse")
                    {
                        if (_switch)
                            _corsairDeviceMouse = true;
                        else
                            _corsairDeviceMouse = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _corsairDeviceMouse
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _corsairDeviceMouse;

                        if (CorsairSdkCalled == 1)
                            _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                                _corsairDeviceMousepad, _corsairDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "CorsairDeviceHeadset")
                    {
                        if (_switch)
                            _corsairDeviceHeadset = true;
                        else
                            _corsairDeviceHeadset = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _corsairDeviceHeadset
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _corsairDeviceHeadset;

                        if (CorsairSdkCalled == 1)
                            _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                                _corsairDeviceMousepad, _corsairDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "CorsairDeviceMousepad")
                    {
                        if (_switch)
                            _corsairDeviceMousepad = true;
                        else
                            _corsairDeviceMousepad = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _corsairDeviceMousepad
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _corsairDeviceMousepad;

                        if (CorsairSdkCalled == 1)
                            _corsair.ResetCorsairDevices(_corsairDeviceKeyboard, _corsairDeviceKeypad, _corsairDeviceMouse,
                                _corsairDeviceMousepad, _corsairDeviceHeadset,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "LogitechDevice")
                    {
                        if (_switch)
                            _logitechDeviceKeyboard = true;
                        else
                            _logitechDeviceKeyboard = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _logitechDeviceKeyboard
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _logitechDeviceKeyboard;

                        if (LogitechSdkCalled == 1)
                            _logitech.ResetLogitechDevices(_logitechDeviceKeyboard,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "CoolermasterDevice")
                    {
                        if (_switch)
                            _coolermasterDeviceKeyboard = true;
                        else
                            _coolermasterDeviceKeyboard = false;

                        change[dG_devices.Columns["col_status"].Index].Value = _coolermasterDeviceKeyboard
                            ? "Enabled"
                            : "Disabled";
                        change[dG_devices.Columns["col_state"].Index].Value = _coolermasterDeviceKeyboard;

                        if (CoolermasterSdkCalled == 1)
                            _coolermaster.ResetCoolermasterDevices(_coolermasterDeviceKeyboard, _coolermasterDeviceMouse,
                                ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
                    }
                    else if (dattype == "LIFX")
                    {
                        var id = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_ID"].Index].Value;
                        if (LifxSdkCalled == 1 && id != null)
                        {
                            var bulb = _lifx.LifxDevices[id];

                            if (_switch)
                            {
                                _lifx.LifxBulbsDat[bulb] = _lifx.LifxModeMemory[id];
                                _lifx.LifxStateMemory[bulb.MacAddressName] = 1;
                                WriteConsole(ConsoleTypes.Lifx, "Enabled LIFX Bulb " + id);
                            }
                            else
                            {
                                _lifx.LifxModeMemory[id] = key;
                                _lifx.LifxStateMemory[bulb.MacAddressName] = 0;
                                _lifx.LifxBulbsDat[bulb] = 0;
                                var state = _lifx.LifxBulbsRestore[bulb];
                                WriteConsole(ConsoleTypes.Lifx, "Disabled LIFX Bulb " + id);
                                _lifx.SetColorAsync(bulb, state.Hue, state.Saturation, state.Brightness, state.Kelvin,
                                    TimeSpan.FromMilliseconds(1000));
                                WriteConsole(ConsoleTypes.Lifx, "Restoring LIFX Bulb " + state.Label);
                            }

                            change[dG_devices.Columns["col_status"].Index].Value = _switch ? "Enabled" : "Disabled";
                        }
                    }
                    else if (dattype == "HUE")
                    {
                        var id = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_ID"].Index].Value;
                        if (HueSdkCalled == 1 && id != null)
                        {
                            var bulb = _hue.HueDevices[id];

                            if (_switch)
                            {
                                _hue.HueBulbsDat[bulb] = _hue.HueModeMemory[id];
                                _hue.HueStateMemory[bulb.UniqueId] = 1;
                                WriteConsole(ConsoleTypes.Hue, "Enabled HUE Bulb " + id);
                            }
                            else
                            {
                                _hue.HueModeMemory[id] = key;
                                _hue.HueStateMemory[bulb.UniqueId] = 0;
                                _hue.HueBulbsDat[bulb] = 0;
                                var state = _hue.HueBulbsRestore[bulb];
                                WriteConsole(ConsoleTypes.Hue, "Disabled HUE Bulb " + id);

                                _hue.SetColorAsync(bulb, state.Hue, state.Saturation, state.Brightness,
                                    state.ColorTemperature,
                                    TimeSpan.FromMilliseconds(1000));

                                WriteConsole(ConsoleTypes.Hue, "Restoring HUE Bulb " + bulb.Name);
                            }

                            change[dG_devices.Columns["col_status"].Index].Value = _switch ? "Enabled" : "Disabled";
                        }
                    }
                }

                if (dG_devices.Columns[e.ColumnIndex].Name == "col_mode")
                {
                    var mode = (string) editedCell.Value;
                    var id = (string) dG_devices.Rows[e.RowIndex].Cells[dG_devices.Columns["col_ID"].Index].Value;

                    if (LifxSdkCalled == 1 && id != null && dattype == "LIFX")
                        if (_deviceModes.ContainsValue(mode))
                        {
                            var key = _deviceModes.FirstOrDefault(x => x.Value == mode).Key;
                            var bulb = _lifx.LifxDevices[id];
                            _lifx.LifxBulbsDat[bulb] = key;
                            _lifx.LifxModeMemory[bulb.MacAddressName] = key;
                            WriteConsole(ConsoleTypes.Lifx, "Updated Mode of LIFX Bulb " + id + " to " + key);
                        }

                    if (HueSdkCalled == 1 && id != null && dattype == "HUE")
                        if (_deviceModes.ContainsValue(mode))
                        {
                            var key = _deviceModes.FirstOrDefault(x => x.Value == mode).Key;
                            var bulb = _hue.HueDevices[id];
                            _hue.HueBulbsDat[bulb] = key;
                            _hue.HueModeMemory[bulb.UniqueId] = key;
                            WriteConsole(ConsoleTypes.Lifx, "Updated Mode of HUE Bulb " + id + " to " + key);
                        }
                }

                SaveDevices();
                //ResetDeviceDataGrid();

                if (RazerSdkCalled == 1)
                    _razer.ResetRazerDevices(_razerDeviceKeyboard, _razerDeviceKeypad, _razerDeviceMouse,
                        _razerDeviceMousepad, _razerDeviceHeadset,
                        ColorTranslator.FromHtml(ColorMappings.ColorMappingBaseColor));
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
            var xpos = picBox.Parent.Width / 2 - picImage.Width / 2;
            var ypos = picBox.Parent.Height / 2 - picImage.Height / 2;
            picBox.Location = new Point(xpos, ypos);
        }

        private void dG_mappings_SelectionChanged(object sender, EventArgs e)
        {
            var color = dG_mappings.CurrentRow.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor;
            ToggleMappingControls(true);
            mapping_colorEditorManager.Color = color;
            previewPanel.BackColor = color;
            //PaletteMappingCurrentSelect = dG_mappings.CurrentRow.Index;
        }

        private void dG_mappings_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            e.PaintParts &= ~DataGridViewPaintParts.Focus;
        }

        private void mapping_colorEditorManager_ColorChanged(object sender, EventArgs e)
        {
            var pmcs = dG_mappings.CurrentRow;
            var pcmsid = (string) pmcs.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value;
            var pcmsColor = pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor;

            previewPanel.BackColor = mapping_colorEditorManager.Color;

            if (pcmsColor != mapping_colorEditorManager.Color)
            {
                var _data = _mappingPalette[pcmsid];
                string[] data = {_data[0], _data[1], _data[2], ColorTranslator.ToHtml(pcmsColor)};
                _mappingPalette[pcmsid] = data;

                foreach (var p in typeof(FfxivColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (p.Name == pcmsid)
                        p.SetValue(ColorMappings, ColorTranslator.ToHtml(mapping_colorEditorManager.Color));

                DrawMappingsDict();
                pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.BackColor =
                    mapping_colorEditorManager.Color;
                pmcs.Cells[dG_mappings.Columns["mappings_col_color"].Index].Style.SelectionBackColor =
                    mapping_colorEditorManager.Color;

                Setbase = false;
                SaveColorMappings(0);
            }
        }

        private void btn_palette_undo_Click(object sender, EventArgs e)
        {
            var pmcs = dG_mappings.CurrentRow;
            var pcmsid = (string) pmcs.Cells[dG_mappings.Columns["mappings_col_id"].Index].Value;

            var cm = new FfxivColorMappings();
            var _restore = ColorTranslator.ToHtml(Color.Black);

            foreach (var p in typeof(FfxivColorMappings).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (p.Name == pcmsid)
                    _restore = (string) p.GetValue(cm);

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
            var ttBtnImport = new ToolTip();
            ttBtnImport.SetToolTip(btn_importChromatics, "Import Chromatics Color Palette");

            var ttBtnExport = new ToolTip();
            ttBtnExport.SetToolTip(btn_exportChromatics, "Export Chromatics Color Palette");

            var ttBtnLoadpalette = new ToolTip();
            ttBtnLoadpalette.SetToolTip(loadPaletteButton, "Import External Color Swatches");

            var ttBtnPaletteUndo = new ToolTip();
            ttBtnPaletteUndo.SetToolTip(btn_palette_undo, "Restore to Default");
        }

        private void InitSettingsGui()
        {
            chk_arxtoggle.Checked = ChromaticsSettings.ChromaticsSettingsArxToggle;
            cb_arx_theme.SelectedIndex = ChromaticsSettings.ChromaticsSettingsArxTheme;
            mi_arxenable.Checked = ChromaticsSettings.ChromaticsSettingsArxToggle;
            chk_lccauto.Checked = ChromaticsSettings.ChromaticsSettingsLccAuto;
            chk_memorycache.Checked = ChromaticsSettings.ChromaticsSettingsMemoryCache;
            chk_azertymode.Checked = ChromaticsSettings.ChromaticsSettingsAzertyMode;

            chk_castanimatetoggle.Checked = ChromaticsSettings.ChromaticsSettingsCastAnimate;
            chk_castchargetoggle.Checked = ChromaticsSettings.ChromaticsSettingsCastToggle;
            chk_gcdcounttoggle.Checked = ChromaticsSettings.ChromaticsSettingsGcdCountdown;

            chk_keybindtoggle.Checked = ChromaticsSettings.ChromaticsSettingsKeybindToggle;
            chk_jobgaugetoggle.Checked = ChromaticsSettings.ChromaticsSettingsJobGaugeToggle;
            chk_highlighttoggle.Checked = ChromaticsSettings.ChromaticsSettingsKeyHighlights;
            chk_impactflashtog.Checked = ChromaticsSettings.ChromaticsSettingsImpactToggle;
            chk_dfbelltoggle.Checked = ChromaticsSettings.ChromaticsSettingsDfBellToggle;
        }

        private void InitSettingsArxGui()
        {
            if (cb_arx_mode.Items.Contains(ChromaticsSettings.ChromaticsSettingsArxMode))
            {
                cb_arx_mode.SelectedItem = ChromaticsSettings.ChromaticsSettingsArxMode;
            }
            else
            {
                var chromaticsSettings = new ChromaticsSettings();
                cb_arx_mode.SelectedItem = chromaticsSettings.ChromaticsSettingsArxMode;
            }

            txt_arx_actip.Text = ChromaticsSettings.ChromaticsSettingsArxactip;
        }

        private void chk_arxtoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsArxToggle = chk_arxtoggle.Checked;
            SaveChromaticsSettings(1);

            if (chk_arxtoggle.Checked)
            {
                ArxToggle = true;
                cb_arx_theme.Enabled = true;
                cb_arx_mode.Enabled = true;

                if (ArxSdkCalled == 0)
                {
                    _arx = LogitechArxInterface.InitializeArxSdk();
                    if (_arx != null)
                    {
                        ArxSdk = true;
                        ArxSdkCalled = 1;
                        ArxState = 0;

                        WriteConsole(ConsoleTypes.Arx, "ARX SDK Enabled");

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

                if (Plugs.Count > 0)
                    foreach (var plugin in Plugs)
                        if (cb_arx_mode.Items.Contains(plugin))
                            cb_arx_mode.Items.Remove(plugin);

                if (ArxSdkCalled == 1)
                {
                    _arx.ShutdownArx();
                    ArxSdkCalled = 0;
                    WriteConsole(ConsoleTypes.Arx, "ARX SDK Disabled");
                }
            }
        }

        private void cb_arx_theme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ArxSdkCalled != 1) return;
            if (Startup == false) return;
            var theme = "light";
            var themeid = cb_arx_theme.SelectedIndex;
            ChromaticsSettings.ChromaticsSettingsArxTheme = cb_arx_theme.SelectedIndex;
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
            if (ArxSdkCalled != 1) return;
            if (Startup == false) return;
            ChromaticsSettings.ChromaticsSettingsArxMode = cb_arx_mode.SelectedItem.ToString();
            SaveChromaticsSettings(1);

            if (cb_arx_mode.SelectedIndex < 4)
            {
                ArxState = cb_arx_mode.SelectedIndex + 1;

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
                            changed = changed.Substring(0, changed.Length - 1);

                        _arx.ArxSendActInfo(changed, 8085);
                        break;
                }
            }
            else
            {
                var getPlugin = cb_arx_mode.SelectedItem + ".html";
                ArxState = 100;
                _arx.ArxSetIndex(getPlugin);
            }

            WriteConsole(ConsoleTypes.Arx, "ARX Template Changed: " + cb_arx_mode.SelectedItem);
        }

        private void rtb_debug_TextChanged(object sender, EventArgs e)
        {
            rtb_debug.SelectionStart = rtb_debug.Text.Length;
            rtb_debug.ScrollToCaret();
        }

        private void showwindow_Click(object sender, EventArgs e)
        {
            if (!_allowVisible)
            {
                _allowVisible = true;
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
                _rkApp.SetValue("Chromatics", Application.ExecutablePath);
            else
                _rkApp.DeleteValue("Chromatics", false);

            chk_startupenable.Checked = mi_winstart.Checked;
        }

        private void enableeffects_Click(object sender, EventArgs e)
        {
            //Enable effects
            if (!mi_effectsenable.Checked)
            {
                if (RazerSdk)
                    RazerSdkCalled = 0;
                if (LogitechSdk)
                    LogitechSdkCalled = 0;
                if (CorsairSdk)
                    CorsairSdkCalled = 0;
                if (LifxSdk)
                    LifxSdkCalled = 0;
                if (HueSdk)
                    HueSdkCalled = 0;
            }
            else
            {
                if (RazerSdk)
                    RazerSdkCalled = 1;
                if (LogitechSdk)
                    LogitechSdkCalled = 1;
                if (CorsairSdk)
                    CorsairSdkCalled = 1;
                if (LifxSdk)
                    LifxSdkCalled = 1;
                if (HueSdk)
                    HueSdkCalled = 1;
            }

            ResetDeviceDataGrid();
        }

        private void enablearx_Click(object sender, EventArgs e)
        {
            if (mi_arxenable.Checked)
                chk_arxtoggle.Checked = true;
            else
                chk_arxtoggle.Checked = false;
        }

        private void notify_master_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!_allowVisible)
            {
                _allowVisible = true;
                Show();
                WindowState = FormWindowState.Normal;
            }
        }

        private void txt_arx_actip_TextChanged(object sender, EventArgs e)
        {
            if (Startup)
            {
                var changed = txt_arx_actip.Text;
                if (changed.EndsWith("/"))
                    changed = changed.Substring(0, changed.Length - 1);

                txt_arx_actip.Text = changed;
                _arx.ArxSendActInfo(changed, 8085);

                ChromaticsSettings.ChromaticsSettingsArxactip = txt_arx_actip.Text;
                SaveChromaticsSettings(1);
            }
        }

        private void chk_lccenable_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            if (chk_lccenable.Checked)
                ToggleLccMode(true);
            else
                ToggleLccMode(false);
        }

        private void chk_lccauto_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsLccAuto = chk_lccauto.Checked;
            SaveChromaticsSettings(1);
        }

        private void btn_lccrestore_Click(object sender, EventArgs e)
        {
            if (LogitechSdkCalled == 0)
                return;

            var lccrestoreCheck =
                MessageBox.Show(
                    "Are you sure you wish to restore LGS to its default settings? This should only be done as a last resort.",
                    "Restore LGS Settings to Default", MessageBoxButtons.OKCancel);
            if (lccrestoreCheck == DialogResult.OK)
                try
                {
                    while (Process.GetProcessesByName("ffxiv_dx11").Length > 0)
                    {
                        var lccrestoreWarning =
                            MessageBox.Show("You must close Final Fantasy XIV before using restore.",
                                "Please close Final Fantasy XIV", MessageBoxButtons.RetryCancel);
                        if (lccrestoreWarning == DialogResult.Cancel)
                            return;
                    }

                    if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                        File.Delete(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll");

                    if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled"))
                        File.Delete(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled");

                    var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    var path = enviroment + @"/LogitechLed.dll";

                    File.Copy(path, _lgsInstall + @"\SDK\LED\x64\LogitechLed.dll", true);
                    WriteConsole(ConsoleTypes.Logitech, "LGS has been restored to its default settings.");

                    chk_lccenable.CheckedChanged -= chk_lccenable_CheckedChanged;
                    chk_lccenable.Checked = false;
                    chk_lccenable.CheckedChanged += chk_lccenable_CheckedChanged;
                }
                catch (Exception ex)
                {
                    WriteConsole(ConsoleTypes.Error,
                        "An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                }
            else if (lccrestoreCheck == DialogResult.Cancel)
                return;
        }

        private void ToggleLccMode([Optional] bool force, [Optional] bool antilog)
        {
            if (LogitechSdkCalled == 0)
                return;

            var _force = false;

            if (!force)
                _force = true;


            if ((chk_lccenable.Checked || force) && !_force)
            {
                //Enable LCC

                if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll"))
                {
                    try
                    {
                        //File.Copy(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll", LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", true);
                        //File.Delete(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll");
                        FileSystem.RenameFile(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll", "LogitechLed.dll.disabled");
                    }
                    catch (Exception ex)
                    {
                        if (!antilog)
                            WriteConsole(ConsoleTypes.Error,
                                "An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                        return;
                    }

                    if (!antilog)
                        WriteConsole(ConsoleTypes.Logitech, "Logitech Conflict Mode Enabled.");
                }
                else
                {
                    if (!antilog)
                        WriteConsole(ConsoleTypes.Error,
                            "An Error occurred trying to enable Logitech Conflict Mode. Error: LGS SDK Library not found (A).");
                }
            }
            else
            {
                //Disable LCC
                if (File.Exists(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled"))
                {
                    try
                    {
                        //File.Copy(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", LgsInstall + @"\SDK\LED\x64\LogitechLed.dll", true);
                        //File.Delete(LgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled");
                        FileSystem.RenameFile(_lgsInstall + @"\SDK\LED\x64\LogitechLed.dll.disabled", "LogitechLed.dll");
                    }
                    catch (Exception ex)
                    {
                        if (!antilog)
                            WriteConsole(ConsoleTypes.Error,
                                "An Error occurred trying to enable Logitech Conflict Mode. Error: " + ex.Message);
                        return;
                    }

                    if (!antilog)
                        WriteConsole(ConsoleTypes.Logitech, "Logitech Conflict Mode Disabled.");
                }
                else
                {
                    if (!antilog)
                        WriteConsole(ConsoleTypes.Error,
                            "An Error occurred trying to disable Logitech Conflict Mode. Error: LGS SDK Library not found (B).");
                }
            }
        }

        private void chk_memorycache_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsMemoryCache = chk_memorycache.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_azertymode_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsAzertyMode = chk_azertymode.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_startupenable_CheckedChanged(object sender, EventArgs e)
        {
            if (chk_startupenable.Checked)
                _rkApp.SetValue("Chromatics", Application.ExecutablePath);
            else
                _rkApp.DeleteValue("Chromatics", false);

            mi_winstart.Checked = chk_startupenable.Checked;
        }

        private void chk_castchargetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsCastToggle = chk_castchargetoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_castanimatetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsCastAnimate = chk_castanimatetoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_gcdcounttoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsGcdCountdown = chk_gcdcounttoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_highlighttoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsKeyHighlights = chk_highlighttoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_jobgaugetoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsJobGaugeToggle = chk_jobgaugetoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_keybindtoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsKeybindToggle = chk_keybindtoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_impactflashtog_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsImpactToggle = chk_impactflashtog.Checked;
            SaveChromaticsSettings(1);
        }

        private void chk_dfbelltoggle_CheckedChanged(object sender, EventArgs e)
        {
            if (Startup == false) return;

            ChromaticsSettings.ChromaticsSettingsDfBellToggle = chk_dfbelltoggle.Checked;
            SaveChromaticsSettings(1);
        }

        private delegate void ResetGridDelegate();

        private delegate void ResetMappingsDelegate();
    }
}