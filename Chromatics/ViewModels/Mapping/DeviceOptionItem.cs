using RGB.NET.Core;
using System;

namespace Chromatics.ViewModels.Mapping
{
    public sealed class DeviceOptionItem
    {
        public Guid DeviceId { get; }
        public string Name { get; }
        public RGBDeviceType DeviceType { get; }

        public DeviceOptionItem(Guid deviceId, string name, RGBDeviceType deviceType)
        {
            DeviceId = deviceId;
            Name = name;
            DeviceType = deviceType;
        }

        public override string ToString() => Name;
    }
}
