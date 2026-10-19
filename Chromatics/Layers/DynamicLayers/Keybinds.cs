using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Chromatics.Helpers.MathHelper;
using Color = RGB.NET.Core.Color;

namespace Chromatics.Layers
{
    public class KeybindsProcessor : LayerProcessor
    {
        private static KeybindsProcessor _instance;
        private Dictionary<Led, ListLedGroup> _localgroups = new Dictionary<Led, ListLedGroup>();
        private readonly Dictionary<string, Color> _specialActionColors = new Dictionary<string, Color>(19);
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

        // Private constructor to prevent direct instantiation
        private KeybindsProcessor() { }

        // Singleton instance access
        public static KeybindsProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new KeybindsProcessor();
                }
                return _instance;
            }
        }

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

                // Repopulate the reusable special-action color map for this tick
                _specialActionColors.Clear();
                _specialActionColors["Map"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindMap.Color);
                _specialActionColors["Aether Currents"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindAetherCurrents.Color);
                _specialActionColors["Signs"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindSigns.Color);
                _specialActionColors["Waymarks"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindWaymarks.Color);
                _specialActionColors["Record Ready Check"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindRecordReadyCheck.Color);
                _specialActionColors["Ready Check"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindReadyCheck.Color);
                _specialActionColors["Countdown"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindCountdown.Color);
                _specialActionColors["Emotes"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindEmotes.Color);
                _specialActionColors["Linkshells"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindLinkshells.Color);
                _specialActionColors["Cross-world Linkshell"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindCrossWorldLS.Color);
                _specialActionColors["Contacts"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindContacts.Color);
                _specialActionColors["Sprint"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindSprint.Color);
                _specialActionColors["Teleport"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindTeleport.Color);
                _specialActionColors["Return"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindReturn.Color);
                _specialActionColors["Limit Break"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindLimitBreak.Color);
                _specialActionColors["Duty Action"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindDutyAction.Color);
                _specialActionColors["Repair"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindRepair.Color);
                _specialActionColors["Dig"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindDig.Color);
                _specialActionColors["Inventory"] = ColorHelper.ColorToRGBColor(_colorPalette.KeybindInventory.Color);
                var specialActionColors = _specialActionColors;

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
                    _layergroups[layer.layerID] = lg;
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

            // The game writes these in its own language, so they go through the
            // parser rather than being compared against the English spellings.
            var (_ctrl, _alt, _shift) = Helpers.HotbarModifierParser.Parse(
                action.Modifiers, out var unrecognised);

            if (unrecognised.Count > 0) ReportUnrecognisedModifiers(unrecognised);
            WarnIfModifiersCannotBeRead();

            if (_ctrl && _alt && _shift) return Modifiers.CTRL_ALT_SHIFT;
            if (_alt && _shift) return Modifiers.ALT_SHIFT;
            if (_ctrl && _shift) return Modifiers.CTRL_SHIFT;
            if (_ctrl && _alt) return Modifiers.CTRL_ALT;
            if (_ctrl) return Modifiers.CTRL;
            if (_alt) return Modifiers.ALT;
            if (_shift) return Modifiers.SHIFT;

            return Modifiers.None;
        }

        // One line per distinct spelling, not per tick. A name we cannot place
        // is a one-entry fix in HotbarModifierParser, so the console needs to
        // name it rather than leave the keybind quietly dark.
        private static readonly HashSet<string> _reportedModifiers = new(StringComparer.OrdinalIgnoreCase);

        private static void ReportUnrecognisedModifiers(IReadOnlyList<string> names)
        {
            foreach (var name in names)
            {
                bool isNew;
                lock (_reportedModifiers)
                    isNew = _reportedModifiers.Add(name);

                if (isNew)
                    Logger.WriteConsole(LoggerTypes.Devices,
                        $"Hotbar keybind modifier '{name}' was not recognised, so keybinds using it will not light up. Please report this along with your game's language.",
                        forwardToSentry: false);
            }
        }

        private static bool _hookWarningShown;

        private static void WarnIfModifiersCannotBeRead()
        {
            if (_hookWarningShown) return;
            if (!KeyController.IsHookBlind()) return;

            // Windows says a modifier is held right now and the hook never told
            // us, so the comparison below can only fail. Checked this way rather
            // than "has the hook ever fired", which is also true of anyone who
            // has not pressed a key yet and warned every healthy user.
            _hookWarningShown = true;
            Logger.WriteConsole(LoggerTypes.Devices,
                "Chromatics cannot see modifier keys being held, so hotbar keybinds that use Ctrl, Alt or Shift will not light up. This usually means the game is running with administrator rights and Chromatics is not.",
                forwardToSentry: false);
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
            if (action.Category == 49 || action.Category == 51 || action.Category == 56)
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
            _instance = null;
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
