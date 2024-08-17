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
        private readonly HashSet<Sharlayan.Core.Enums.Action.Container> hotbarTypes = new HashSet<Sharlayan.Core.Enums.Action.Container>{
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
            if (layer.deviceType != RGBDeviceType.Keyboard) return;

            var _colorPalette = RGBController.GetActivePalette();
            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray = GetLedArray(layer);

            if (_init && (layer.requestUpdate || !layer.Enabled))
            {
                DetachAndClearLocalGroups();
                if (!layer.Enabled)
                    return;
            }

            var _memoryHandler = GameController.GetGameData();

            if (_memoryHandler?.Reader != null && _memoryHandler.Reader.CanGetActions())
            {
                var getActions = _memoryHandler.Reader.GetActions();
                if (getActions.ActionContainers == null) return;

                UpdateBrushColors(layer, _colorPalette);

                // Define special action colors
                var specialActionColors = new Dictionary<string, Color>
                {
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

                InitializeLedGroups(ledArray, layer.zindex);

                foreach (var ledGroup in _localgroups)
                {
                    foreach (var hotbar in getActions.ActionContainers)
                    {
                        if (hotbarTypes.Contains(hotbar.ContainerType)) continue;

                        foreach (var action in hotbar.ActionItems)
                        {
                            ProcessActionItem(action, ledGroup, specialActionColors);
                        }
                    }
                }

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

            AttachGroupsToSurface();
            _init = true;
            layer.requestUpdate = false;
        }

        private void DetachAndClearLocalGroups()
        {
            foreach (var layergroup in _localgroups.Values)
            {
                layergroup?.Detach();
            }

            _localgroups.Clear();
        }

        private void UpdateBrushColors(IMappingLayer layer, PaletteColorModel _colorPalette)
        {
            var keybind_cd_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarCd.Color);
            var keybind_na_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarNotAvailable.Color);
            var keybind_outranged_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarOutRange.Color);
            var keybind_proc_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarProc.Color);
            var keybind_ready_color = ColorHelper.ColorToRGBColor(_colorPalette.HotbarReady.Color);
            var empty_color = ColorHelper.ColorToRGBColor(_colorPalette.KeybindDisabled.Color);

            keybind_cd_brush = GetOrCreateBrush(keybind_cd_brush, keybind_cd_color);
            keybind_na_brush = GetOrCreateBrush(keybind_na_brush, keybind_na_color);
            keybind_outranged_brush = GetOrCreateBrush(keybind_outranged_brush, keybind_outranged_color);
            keybind_proc_brush = GetOrCreateBrush(keybind_proc_brush, keybind_proc_color);
            keybind_ready_brush = GetOrCreateBrush(keybind_ready_brush, keybind_ready_color);
            keybind_special_brush = GetOrCreateBrush(keybind_special_brush, empty_color);
            empty_brush = GetOrCreateBrush(empty_brush, empty_color);

            if (layer.allowBleed)
            {
                empty_brush.Color = Color.Transparent;
                keybind_ready_brush.Color = Color.Transparent;
            }
        }

        private SolidColorBrush GetOrCreateBrush(SolidColorBrush brush, Color color)
        {
            if (brush == null || brush.Color != color)
            {
                brush = new SolidColorBrush(color);
            }
            return brush;
        }

        private void InitializeLedGroups(Led[] ledArray, int zindex)
        {
            foreach (var led in ledArray)
            {
                if (!_localgroups.ContainsKey(led))
                {
                    var ledGroup = new ListLedGroup(surface, led)
                    {
                        ZIndex = zindex,
                        Brush = empty_brush
                    };
                    _localgroups.Add(led, ledGroup);
                    ledGroup.Detach();
                }
            }
        }

        private void ProcessActionItem(Sharlayan.Core.ActionItem action, KeyValuePair<Led, ListLedGroup> ledGroup, Dictionary<string, Color> specialActionColors)
        {
            if (action.ActionKey != LedKeyHelper.LedIdToHotbarKeyConverter(ledGroup.Key.Id)) return;
            if (!action.IsKeyBindAssigned || string.IsNullOrEmpty(action.Name) || string.IsNullOrEmpty(action.KeyBinds) || string.IsNullOrEmpty(action.ActionKey)) return;

            var modKey = GetModifiersFromAction(action);
            var pushedKey = GetPushedModifiers();

            if (modKey != pushedKey && (modKey != Modifiers.None || pushedKey != Modifiers.None)) return;

            UpdateLedGroupBrush(action, ledGroup.Value, specialActionColors);
        }

        private Modifiers GetModifiersFromAction(Sharlayan.Core.ActionItem action)
        {
            if (action.Modifiers.Count == 0) return Modifiers.None;

            var _ctrl = action.Modifiers.Contains("Ctrl");
            var _alt = action.Modifiers.Contains("Alt");
            var _shift = action.Modifiers.Contains("Shift");

            if (_ctrl && _alt && _shift) return Modifiers.CTRL_ALT_SHIFT;
            if (_alt && _shift) return Modifiers.ALT_SHIFT;
            if (_ctrl && _shift) return Modifiers.CTRL_SHIFT;
            if (_ctrl && _alt) return Modifiers.CTRL_ALT;
            if (_ctrl) return Modifiers.CTRL;
            if (_alt) return Modifiers.ALT;
            if (_shift) return Modifiers.SHIFT;

            return Modifiers.None;
        }

        private Modifiers GetPushedModifiers()
        {
            if (KeyController.IsCtrlPressed() && KeyController.IsAltPressed() && KeyController.IsShiftPressed())
                return Modifiers.CTRL_ALT_SHIFT;
            if (!KeyController.IsCtrlPressed() && KeyController.IsAltPressed() && KeyController.IsShiftPressed())
                return Modifiers.ALT_SHIFT;
            if (KeyController.IsCtrlPressed() && !KeyController.IsAltPressed() && KeyController.IsShiftPressed())
                return Modifiers.CTRL_SHIFT;
            if (KeyController.IsCtrlPressed() && KeyController.IsAltPressed() && !KeyController.IsShiftPressed())
                return Modifiers.CTRL_ALT;
            if (KeyController.IsCtrlPressed() && !KeyController.IsAltPressed() && !KeyController.IsShiftPressed())
                return Modifiers.CTRL;
            if (!KeyController.IsCtrlPressed() && KeyController.IsAltPressed() && !KeyController.IsShiftPressed())
                return Modifiers.ALT;
            if (!KeyController.IsCtrlPressed() && !KeyController.IsAltPressed() && KeyController.IsShiftPressed())
                return Modifiers.SHIFT;

            return Modifiers.None;
        }

        private void UpdateLedGroupBrush(Sharlayan.Core.ActionItem action, ListLedGroup ledGroup, Dictionary<string, Color> specialActionColors)
        {
            if (action.Category == 49 || action.Category == 51)
            {
                if (!action.IsAvailable || !action.InRange || !action.ChargeReady || action.CoolDownPercent > 0)
                {
                    ledGroup.Brush = keybind_na_brush;
                    return;
                }

                if (specialActionColors.TryGetValue(action.Name, out Color actionColor))
                {
                    keybind_special_brush.Color = actionColor;
                    ledGroup.Brush = keybind_special_brush;
                }
                return;
            }

            if (action.IsAvailable && action.ChargeReady)
            {
                if (action.InRange)
                {
                    ledGroup.Brush = action.IsProcOrCombo ? keybind_proc_brush : (action.CoolDownPercent > 0 ? keybind_cd_brush : keybind_ready_brush);
                }
                else
                {
                    ledGroup.Brush = keybind_outranged_brush;
                }
            }
            else
            {
                ledGroup.Brush = keybind_na_brush;
            }
        }

        private void AttachGroupsToSurface()
        {
            foreach (var layergroup in _localgroups)
            {
                layergroup.Value.Attach(surface);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DetachAndClearLocalGroups();
            }

            base.Dispose(disposing);
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
