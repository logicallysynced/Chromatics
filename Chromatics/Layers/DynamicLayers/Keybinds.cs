using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using Sanford.Multimedia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Chromatics.Helpers.MathHelper;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Layers
{
    public class KeybindsProcessor : LayerProcessor
    {
        private Dictionary<Led, ListLedGroup> _localgroups = new Dictionary<Led, ListLedGroup>();
        private SolidColorBrush keybind_cd_brush;
        private SolidColorBrush keybind_na_brush;
        private SolidColorBrush keybind_outranged_brush;
        private SolidColorBrush keybind_proc_brush;
        private SolidColorBrush keybind_ready_brush;
        private SolidColorBrush keybind_special_brush;
        private SolidColorBrush empty_brush;
        private HashSet<Sharlayan.Core.Enums.Action.Container> hotbarTypes = new HashSet<Sharlayan.Core.Enums.Action.Container>{
            Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_1,
            Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_2,
            Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_3,
            Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_4,
            Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_5,
            Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_6,
            Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_7,
            Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_8,
            Sharlayan.Core.Enums.Action.Container.CROSS_PETBAR
        };


        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;

            //Do not apply to devices other than Keyboards
            if (layer.deviceType != RGBDeviceType.Keyboard) return;

            //Keybinds Dynamic Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();

            //loop through all LED's and assign to device layer (Order of LEDs is not important for a highlight layer)
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);
            var ledArray = devices.SelectMany(d => d).Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

            var countKeys = ledArray.Count();

            //Check if layer has been updated or if layer is disabled or if currently in Preview mode    
            if (_init && (layer.requestUpdate || !layer.Enabled))
            {
                foreach (var layergroup in _localgroups)
                {
                    if (layergroup.Value != null)
                        layergroup.Value.Detach();
                }

                _localgroups.Clear();

                if (!layer.Enabled)
                    return;
            }
            
            //Process data from FFXIV
            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActions())
            {
                var getActions = _memoryHandler.Reader.GetActions();
                if (getActions.ActionContainers == null) return;

                var keybind_cd_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarCd.Color);
                var keybind_na_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarNotAvailable.Color);
                var keybind_outranged_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarOutRange.Color);
                var keybind_proc_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarProc.Color);
                var keybind_ready_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarReady.Color); //bleed layer
                var empty_color = ColorHelper.ColorToRGBColor(_colorPalette.KeybindDisabled.Color); //bleed layer

                var specialActionColors = new Dictionary<string, Color>() {
                    {"Map", ColorHelper.ColorToRGBColor(_colorPalette.KeybindMap.Color)},
                    {"Aether Currents", ColorHelper.ColorToRGBColor(_colorPalette.KeybindAetherCurrents.Color)},
                    {"Signs", ColorHelper.ColorToRGBColor(_colorPalette.KeybindSigns.Color)},
                    {"Waymarks", ColorHelper.ColorToRGBColor(_colorPalette.KeybindWaymarks.Color)},
                    {"Record Ready Check", ColorHelper.ColorToRGBColor(_colorPalette.KeybindRecordReadyCheck.Color)},
                    {"Ready Check", ColorHelper.ColorToRGBColor(_colorPalette.KeybindReadyCheck.Color)},
                    {"Countdown", ColorHelper.ColorToRGBColor(_colorPalette.KeybindCountdown.Color)},
                    {"Emotes", ColorHelper.ColorToRGBColor(_colorPalette.KeybindEmotes.Color)},
                    {"Linkshells", ColorHelper.ColorToRGBColor(_colorPalette.KeybindLinkshells.Color)},
                    {"Cross-world Linkshell", ColorHelper.ColorToRGBColor(_colorPalette.KeybindCrossWorldLS.Color)},
                    {"Contacts", ColorHelper.ColorToRGBColor(_colorPalette.KeybindContacts.Color)},
                    {"Sprint", ColorHelper.ColorToRGBColor(_colorPalette.KeybindSprint.Color)},
                    {"Teleport", ColorHelper.ColorToRGBColor(_colorPalette.KeybindTeleport.Color)},
                    {"Return", ColorHelper.ColorToRGBColor(_colorPalette.KeybindReturn.Color)},
                    {"Limit Break", ColorHelper.ColorToRGBColor(_colorPalette.KeybindLimitBreak.Color)},
                    {"Duty Action", ColorHelper.ColorToRGBColor(_colorPalette.KeybindDutyAction.Color)},
                    {"Repair", ColorHelper.ColorToRGBColor(_colorPalette.KeybindRepair.Color)},
                    {"Dig", ColorHelper.ColorToRGBColor(_colorPalette.KeybindDig.Color)},
                    {"Inventory", ColorHelper.ColorToRGBColor(_colorPalette.KeybindInventory.Color)}
                };

                if (keybind_cd_brush == null || keybind_cd_brush.Color != keybind_cd_color) keybind_cd_brush = new SolidColorBrush(keybind_cd_color);
                if (keybind_na_brush == null || keybind_na_brush.Color != keybind_na_color) keybind_na_brush = new SolidColorBrush(keybind_na_color);
                if (keybind_outranged_brush == null || keybind_outranged_brush.Color != keybind_outranged_color) keybind_outranged_brush = new SolidColorBrush(keybind_outranged_color);
                if (keybind_proc_brush == null || keybind_proc_brush.Color != keybind_proc_color) keybind_proc_brush = new SolidColorBrush(keybind_proc_color);
                if (keybind_ready_brush == null || keybind_ready_brush.Color != keybind_ready_color) keybind_ready_brush = new SolidColorBrush(keybind_ready_color);
                if (keybind_special_brush == null || keybind_special_brush.Color != empty_color) keybind_special_brush = new SolidColorBrush(empty_color);
                if (empty_brush == null || empty_brush.Color != empty_color) empty_brush = new SolidColorBrush(empty_color);

                if (layer.allowBleed)
                {
                    empty_brush.Color = Color.Transparent;
                    keybind_ready_brush.Color = Color.Transparent;
                }


                //Setup ListLedGroup for each keybind
                foreach (var led in ledArray)
                {
                    if (!_localgroups.ContainsKey(led))
                    {
                        var ledGroup = new ListLedGroup(surface, led)
                        {
                            ZIndex = layer.zindex,
                            Brush = empty_brush
                        };

                        _localgroups.Add(led, ledGroup);
                        ledGroup.Detach();
                        
                    }

                }

                foreach (var ledGroup in _localgroups)
                {

                    foreach (var hotbar in getActions.ActionContainers)
                    {
                        if (hotbarTypes.Contains(hotbar.ContainerType)) continue;


                        foreach (var action in hotbar.ActionItems)
                        {
                            if (action.ActionKey != LedKeyHelper.LedIdToHotbarKeyConverter(ledGroup.Key.Id)) continue;
                            if (!action.IsKeyBindAssigned || string.IsNullOrEmpty(action.Name) || string.IsNullOrEmpty(action.KeyBinds) || string.IsNullOrEmpty(action.ActionKey)) continue;

                            var modsactive = action.Modifiers.Count;
                            var modKey = Modifiers.Null;
                            var pushedKey = Modifiers.None;

                            //PRESSED
                            if (KeyController.IsCtrlPressed() && KeyController.IsAltPressed() && KeyController.IsShiftPressed())
                            {
                                pushedKey = Modifiers.CTRL_ALT_SHIFT;
                            }
                            else if (!KeyController.IsCtrlPressed() && KeyController.IsAltPressed() && KeyController.IsShiftPressed())
                            {
                                pushedKey = Modifiers.ALT_SHIFT;
                            }
                            else if (KeyController.IsCtrlPressed() && !KeyController.IsAltPressed() && KeyController.IsShiftPressed())
                            {
                                pushedKey = Modifiers.CTRL_SHIFT;
                            }
                            else if (KeyController.IsCtrlPressed() && KeyController.IsAltPressed() && !KeyController.IsShiftPressed())
                            {
                                pushedKey = Modifiers.CTRL_ALT;
                            }
                            else if (KeyController.IsCtrlPressed() && !KeyController.IsAltPressed() && !KeyController.IsShiftPressed())
                            {
                                pushedKey = Modifiers.CTRL;
                            }
                            else if (!KeyController.IsCtrlPressed() && KeyController.IsAltPressed() && !KeyController.IsShiftPressed())
                            {
                                pushedKey = Modifiers.ALT;
                            }
                            else if (!KeyController.IsCtrlPressed() && !KeyController.IsAltPressed() && KeyController.IsShiftPressed())
                            {
                                pushedKey = Modifiers.SHIFT;
                            }

                            if (modsactive > 0)
                            {
                                var _ctrl = false;
                                var _alt = false;
                                var _shift = false;

                                foreach (var modifier in action.Modifiers)
                                {
                                    switch (modifier)
                                    {
                                        case "Ctrl":
                                            _ctrl = true;
                                            break;
                                        case "Alt":
                                            _alt = true;
                                            break;
                                        case "Shift":
                                            _shift = true;
                                            break;
                                    }
                                }

                                //CTRL ALT SHIFT
                                if (_ctrl && _alt && _shift)
                                {
                                    modKey = Modifiers.CTRL_ALT_SHIFT;
                                }
                                //ALT SHIFT
                                else if (!_ctrl && _alt && _shift)
                                {
                                    modKey = Modifiers.ALT_SHIFT;
                                }
                                //CTRL SHIFT
                                else if (_ctrl && !_alt && _shift)
                                {
                                    modKey = Modifiers.CTRL_SHIFT;
                                }
                                //CTRL ALT
                                else if (_ctrl && _alt && !_shift)
                                {
                                    modKey = Modifiers.CTRL_ALT;
                                }
                                //CTRL
                                else if (_ctrl && !_alt && !_shift)
                                {
                                    modKey = Modifiers.CTRL;
                                }
                                //ALT
                                else if (!_ctrl && _alt && !_shift)
                                {
                                    modKey = Modifiers.ALT;
                                }
                                //SHIFT
                                else if (!_ctrl && !_alt && _shift)
                                {
                                    modKey = Modifiers.SHIFT;
                                }
                            }
                            else
                            {
                                modKey = Modifiers.None;
                            }

                            if (modKey != pushedKey && (modKey != Modifiers.None || pushedKey != Modifiers.None)) break;


                            if (modKey == pushedKey)
                            {

                                if (action.Category == 49 || action.Category == 51)
                                {
                                    if (!action.IsAvailable || !action.InRange || !action.ChargeReady || action.CoolDownPercent > 0)
                                    {
                                        ledGroup.Value.Brush = keybind_na_brush;
                                        continue;
                                    }

                                    if (specialActionColors.TryGetValue(action.Name, out Color actionColor))
                                    {
                                        keybind_special_brush.Color = actionColor;
                                        ledGroup.Value.Brush = keybind_special_brush;
                                    }

                                    continue;
                                }

                                if (action.IsAvailable && action.ChargeReady)
                                {
                                    if (action.InRange)
                                    {
                                        if (action.IsProcOrCombo)
                                        {
                                            ledGroup.Value.Brush = keybind_proc_brush;
                                        }
                                        else
                                        {
                                            if (action.CoolDownPercent > 0)
                                            {
                                                ledGroup.Value.Brush = keybind_cd_brush;

                                            }
                                            else
                                            {
                                                ledGroup.Value.Brush = keybind_ready_brush;

                                            }
                                        }
                                    }
                                    else
                                    {
                                        ledGroup.Value.Brush = keybind_outranged_brush;


                                    }
                                }
                                else
                                {
                                    ledGroup.Value.Brush = keybind_na_brush;

                                }


                            }

                        }
                    }
                }


                //Send layers to _layergroups Dictionary to be tracked outside this method

                var lg = _localgroups.Values.ToArray();

                if (_layergroups.ContainsKey(layer.layerID))
                {
                    _layergroups[layer.layerID] = lg;
                }
                else
                {
                    _layergroups.Add(layer.layerID, lg);
                }
            }

            //Apply lighting
            foreach (var layergroup in _localgroups)
            {
                layergroup.Value.Attach(surface);
            }
            
            _init = true;
            layer.requestUpdate = false;
        }

        private enum Modifiers
        {
            Null,
            None,
            CTRL,
            ALT,
            SHIFT,
            CTRL_ALT,
            CTRL_SHIFT,
            ALT_SHIFT,
            CTRL_ALT_SHIFT
        }

        
    }

    
}
