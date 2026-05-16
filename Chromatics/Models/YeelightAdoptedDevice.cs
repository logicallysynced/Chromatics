using System;
using System.Collections.Generic;

namespace Chromatics.Models
{
    // Persisted (settings.json) record of a user-adopted Yeelight bulb /
    // strip / lamp. Identity is the bulb's stable hex Id from SSDP, which
    // survives IP changes; LastIp / Label / Model / Support are hints we
    // re-verify on every provider start (DHCP renewals are common, users
    // rename bulbs in the Yeelight app, firmware updates can change the
    // supported-method list).
    public class YeelightAdoptedDevice
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string LastIp { get; set; }
        public int LastPort { get; set; } = 55443;
        public string Model { get; set; }
        public string FirmwareVersion { get; set; }
        public List<string> Support { get; set; } = new();
    }
}
