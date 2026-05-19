using System.Collections.Generic;
using RGBNetCore = global::RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // Runtime descriptor for an adopted QMK keyboard. Captures the
    // identifying metadata from the firmware handshake plus the
    // resolved LED layout (per-key semantic LedIds when a VIA keymap
    // was fetchable, Custom1..N otherwise) so the device and update
    // queue don't have to re-probe on every Load.
    public sealed class QmkRawHidClientDefinition
    {
        public QmkRawHidClientDefinition(
            int vendorId, int productId,
            string manufacturer, string product,
            string firmwareDeviceName,
            int ledCount,
            QmkRawHidProtocolMode protocol,
            byte matrixColumns, byte matrixRows,
            string viaKeymapKey,
            IReadOnlyList<QmkLedLayoutEntry> layout,
            int inputReportByteLength,
            int outputReportByteLength)
        {
            VendorId = vendorId;
            ProductId = productId;
            Manufacturer = manufacturer ?? string.Empty;
            Product = product ?? string.Empty;
            FirmwareDeviceName = firmwareDeviceName ?? string.Empty;
            LedCount = ledCount;
            Protocol = protocol;
            MatrixColumns = matrixColumns;
            MatrixRows = matrixRows;
            ViaKeymapKey = viaKeymapKey ?? string.Empty;
            Layout = layout ?? System.Array.Empty<QmkLedLayoutEntry>();
            InputReportByteLength = inputReportByteLength;
            OutputReportByteLength = outputReportByteLength;
        }

        public int VendorId { get; }
        public int ProductId { get; }
        public string Manufacturer { get; }
        public string Product { get; }
        public string FirmwareDeviceName { get; }
        public int LedCount { get; }
        public QmkRawHidProtocolMode Protocol { get; }
        public byte MatrixColumns { get; }
        public byte MatrixRows { get; }
        public string ViaKeymapKey { get; }

        // Report sizes from HidP_GetCaps (Windows). These include the leading
        // report-id byte that HidStream prepends/strips, so the actual data
        // payload is OutputReportByteLength - 1 / InputReportByteLength - 1.
        // VIA firmware typically reports 33 / 33 (RAW_EPSIZE=32). OpenRGB-QMK
        // firmware reports 65 / 65 (RAW_EPSIZE=64). Earlier builds of this
        // provider hardcoded 32-byte buffers and timed out against OpenRGB-QMK
        // boards because the firmware's 64-byte reply never fit.
        public int InputReportByteLength { get; }
        public int OutputReportByteLength { get; }

        // One entry per RGB LED reported by the firmware, ordered by LED
        // index. For OpenRgbQmk mode this is populated from
        // Cmd_GetLedInfo + the optional VIA keymap merge. For ViaOnly
        // mode the list contains a single entry (LedId.Custom1) used
        // for the single representative-colour LED.
        public IReadOnlyList<QmkLedLayoutEntry> Layout { get; }

        // Stable identity for matching against persisted SettingsModel
        // entries. Manufacturer/Product strings vary in case across firmware
        // builds (e.g. "NovelKeys" vs "Novelkeys"); compare case-insensitive.
        public string Identity => $"{VendorId:X4}:{ProductId:X4}:{Manufacturer}:{Product}";
    }

    // One physical LED's placement on the host-side keyboard layout.
    // MatrixCol/MatrixRow are the firmware's LED-matrix coordinates;
    // PreferredLedId is the semantic RGB.NET id when we matched against a
    // VIA keymap (e.g. LedId.Keyboard_A), or LedId.Custom1 + index when
    // we fell back to Custom1..N. PreferredLedId is consumed by
    // QmkRawHidDevice.InitializeLayout.
    public readonly struct QmkLedLayoutEntry
    {
        public readonly int FirmwareIndex;
        public readonly byte MatrixCol;
        public readonly byte MatrixRow;
        public readonly RGBNetCore.LedId PreferredLedId;
        public readonly RGBNetCore.Point Location;
        public readonly RGBNetCore.Size Size;

        public QmkLedLayoutEntry(int firmwareIndex, byte matrixCol, byte matrixRow, RGBNetCore.LedId preferredLedId, RGBNetCore.Point location, RGBNetCore.Size size)
        {
            FirmwareIndex = firmwareIndex;
            MatrixCol = matrixCol;
            MatrixRow = matrixRow;
            PreferredLedId = preferredLedId;
            Location = location;
            Size = size;
        }
    }
}
