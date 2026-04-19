using Chromatics.Enums;
using Chromatics.Layers;
using Chromatics.Localization;
using Chromatics.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Chromatics.ViewModels.Mapping
{
    // Holds precomputed canvas geometry for one device's keys. For keyboards,
    // the row layout is lifted from the old Uc_VirtualKeyboard TableLayoutPanel
    // flow (per-key width/height, margin_left as inline spacer, line_break as
    // row break). Position is absolute so views just bind X/Y onto a Canvas.
    public sealed partial class VirtualDeviceViewModel : ObservableObject
    {
        // Keyboard sizing — matches old Uc_VirtualKeyboard TableLayoutPanel.
        private const double RowHeightCap = 35;
        private const double CellPad = 5;
        private const double DefaultMarginLeft = 7;

        // Non-keyboard sizing. Wider keycaps so labels like "Mouse1" fit,
        // and we wrap rows at NonKbColumnCap to prevent tall strips (e.g.
        // strip devices with 60+ LEDs) running horizontally off-screen.
        private const double NonKbKeyWidth = 64;
        private const double NonKbKeyHeight = 38;
        private const double NonKbGap = 8;
        private const int NonKbColumnCap = 10;

        // Minimum canvas size for non-keyboard devices. The grid-computed
        // bounding box hugs the default key layout tightly, which gave users
        // almost no room to drag keys around. Pad the canvas to a generous
        // drawing area so arbitrary shapes (rings, crosses, L-shapes) are
        // possible without keys falling off the right edge.
        private const double NonKbMinCanvasWidth = 960;
        private const double NonKbMinCanvasHeight = 520;

        public Guid DeviceId { get; }
        public string DeviceName { get; }
        public RGBDeviceType DeviceType { get; }
        public ObservableCollection<KeycapViewModel> Keycaps { get; }

        [ObservableProperty] private double _width;
        [ObservableProperty] private double _height;

        // True only for non-keyboard devices whose keys can be dragged by the
        // user. Flipped by MappingViewModel when the lock toggle changes.
        [ObservableProperty] private bool _isDraggable;

        public bool SupportsDragReposition => DeviceType != RGBDeviceType.Keyboard;

        private Action<LedId> _pickKeyCallback;

        public void SetPickKeyCallback(Action<LedId> callback) => _pickKeyCallback = callback;
        public void InvokePickKey(LedId ledId) => _pickKeyCallback?.Invoke(ledId);

        // Restores every keycap to its grid-computed default position, clears
        // the persisted overrides for this device, and saves. Called from the
        // view after the user has confirmed via dialog.
        public void ResetOverrides()
        {
            foreach (var keycap in Keycaps)
            {
                keycap.X = keycap.DefaultX;
                keycap.Y = keycap.DefaultY;
            }
            MappingLayers.ClearDeviceLayoutOverrides(DeviceId);
            System.Threading.Tasks.Task.Run(() => MappingLayers.SaveMappings());
        }

        public VirtualDeviceViewModel(Guid deviceId, string deviceName, RGBDeviceType deviceType,
                                      ObservableCollection<KeycapViewModel> keycaps,
                                      double width, double height)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            DeviceType = deviceType;
            Keycaps = keycaps;
            _width = width;
            _height = height;
        }

        // Keyboards: always render the full printed layout. The old Uc_VirtualKeyboard
        // had a broken Where(...) guard that never filtered, so the UI showed every
        // localized key regardless of which LEDs RGB.NET actually exposed. We match
        // that intentionally — "preserve keyboard shape" trumps LED-subset accuracy.
        public static VirtualDeviceViewModel BuildForKeyboard(Guid deviceId, string deviceName,
                                                              KeyboardLocalization layout,
                                                              ISet<LedId> availableLeds = null)
        {
            var keys = KeyLocalization.GetLocalizedKeys(layout);
            return Build(deviceId, deviceName, RGBDeviceType.Keyboard, keys, availableLeds: null);
        }

        public static VirtualDeviceViewModel BuildFromKeys(Guid deviceId, string deviceName,
                                                           RGBDeviceType deviceType,
                                                           IEnumerable<KeyboardKey> keys,
                                                           ISet<LedId> availableLeds = null)
        {
            return Build(deviceId, deviceName, deviceType, keys, availableLeds);
        }

        private static VirtualDeviceViewModel Build(Guid deviceId, string deviceName,
                                                    RGBDeviceType deviceType,
                                                    IEnumerable<KeyboardKey> keys,
                                                    ISet<LedId> availableLeds)
        {
            var overrides = MappingLayers.GetDeviceLayoutOverrides(deviceId);

            if (deviceType == RGBDeviceType.Keyboard)
                return BuildKeyboardLayout(deviceId, deviceName, deviceType, keys, availableLeds, overrides);

            return BuildNonKeyboardLayout(deviceId, deviceName, deviceType, keys, availableLeds, overrides);
        }

        private static VirtualDeviceViewModel BuildKeyboardLayout(Guid deviceId, string deviceName,
                                                                  RGBDeviceType deviceType,
                                                                  IEnumerable<KeyboardKey> keys,
                                                                  ISet<LedId> availableLeds,
                                                                  IReadOnlyDictionary<LedId, DeviceKeyPosition> overrides)
        {
            var keycaps = new ObservableCollection<KeycapViewModel>();

            double cursorX = 0;
            double cursorY = 0;
            double rowHeight = 0;
            double maxX = 0;

            foreach (var key in keys)
            {
                if (availableLeds != null && !availableLeds.Contains(key.LedType))
                {
                    if (key.line_break == true)
                    {
                        cursorY += rowHeight;
                        cursorX = 0;
                        rowHeight = 0;
                    }
                    continue;
                }

                double marginLeft = key.margin_left ?? DefaultMarginLeft;
                if (marginLeft > DefaultMarginLeft)
                    cursorX += marginLeft;

                double width  = (key.width  ?? 30) + CellPad;
                double height = Math.Min((key.height ?? 30) + CellPad, RowHeightCap);

                // Keyboard keys don't support user-drag yet — overrides are
                // honoured only on non-keyboard layouts where they make sense.
                keycaps.Add(new KeycapViewModel(
                    key.visualName ?? string.Empty,
                    key.LedType,
                    cursorX, cursorY,
                    width, height));

                cursorX += width;
                if (cursorX > maxX) maxX = cursorX;
                if (height > rowHeight) rowHeight = height;

                if (key.line_break == true)
                {
                    cursorY += rowHeight;
                    cursorX = 0;
                    rowHeight = 0;
                }
            }

            double totalHeight = cursorY + rowHeight;
            return new VirtualDeviceViewModel(deviceId, deviceName, deviceType, keycaps, maxX, totalHeight);
        }

        private static VirtualDeviceViewModel BuildNonKeyboardLayout(Guid deviceId, string deviceName,
                                                                     RGBDeviceType deviceType,
                                                                     IEnumerable<KeyboardKey> keys,
                                                                     ISet<LedId> availableLeds,
                                                                     IReadOnlyDictionary<LedId, DeviceKeyPosition> overrides)
        {
            var keycaps = new ObservableCollection<KeycapViewModel>();

            int col = 0;
            int row = 0;
            double maxX = 0;
            double maxY = 0;

            foreach (var key in keys)
            {
                if (availableLeds != null && !availableLeds.Contains(key.LedType))
                    continue;

                double gridX = col * (NonKbKeyWidth + NonKbGap);
                double gridY = row * (NonKbKeyHeight + NonKbGap);
                double x = gridX;
                double y = gridY;

                // User-repositioned keys: the persisted position wins over the
                // grid-computed default so dragged layouts survive reopens.
                // The grid position is kept as DefaultX/Y so Reset can restore
                // it — otherwise Reset would collapse to "no-op" once a layout
                // file exists on disk.
                if (overrides != null && overrides.TryGetValue(key.LedType, out var pos))
                {
                    x = pos.X;
                    y = pos.Y;
                }

                keycaps.Add(new KeycapViewModel(
                    key.visualName ?? key.LedType.ToString(),
                    key.LedType,
                    x, y,
                    gridX, gridY,
                    NonKbKeyWidth, NonKbKeyHeight));

                if (x + NonKbKeyWidth > maxX) maxX = x + NonKbKeyWidth;
                if (y + NonKbKeyHeight > maxY) maxY = y + NonKbKeyHeight;

                col++;
                if (col >= NonKbColumnCap)
                {
                    col = 0;
                    row++;
                }
            }

            double canvasWidth  = Math.Max(maxX, NonKbMinCanvasWidth);
            double canvasHeight = Math.Max(maxY, NonKbMinCanvasHeight);
            return new VirtualDeviceViewModel(deviceId, deviceName, deviceType, keycaps, canvasWidth, canvasHeight);
        }
    }
}
