using Windows.Devices.Lights;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    // Runtime descriptor for one Windows Dynamic Lighting (LampArray)
    // device. Captures the WinRT LampArray instance the DeviceWatcher
    // resolved from the device id, plus the OS-supplied identification
    // bits used for the Mapping tab label.
    //
    // The LampArray itself owns the live connection to the device and
    // is what the UpdateQueue calls SetColor / SetColorsForIndices on.
    // We hold a reference here so the provider's lifecycle can dispose
    // it when the device is removed.
    public sealed class DynamicLightingClientDefinition
    {
        public DynamicLightingClientDefinition(
            string deviceId,
            string displayName,
            LampArray lampArray)
        {
            DeviceId = deviceId ?? string.Empty;
            DisplayName = displayName ?? "Dynamic Lighting Device";
            LampArray = lampArray;
        }

        // OS device identifier (HID-style \\?\HID#... path). Stable across
        // reconnects of the same physical device; lets us match incoming
        // DeviceWatcher.Added events to existing entries during refresh.
        public string DeviceId { get; }

        // OEM-supplied display name from DeviceInformation.Name. Falls
        // back to a generic label when the OS doesn't give us one.
        public string DisplayName { get; }

        // The live WinRT object that fronts the device. The UpdateQueue
        // calls SetColor / SetColorsForIndices / SetSingleColor on this.
        // Disposed by the provider when the device is removed.
        public LampArray LampArray { get; }
    }
}
