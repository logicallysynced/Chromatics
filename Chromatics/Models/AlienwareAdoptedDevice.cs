namespace Chromatics.Models
{
    // Persisted (settings.json) record of a user-adopted Alienware device.
    // Identity is the VID/PID/DevicePath tuple. ApiVersion captures which
    // wire format the discovery probe decided on so we don't have to re-
    // probe the HID interface every launch.
    public class AlienwareAdoptedDevice
    {
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public string Manufacturer { get; set; }
        public string Product { get; set; }
        public string DevicePath { get; set; }

        // String representation of AlienwareApiVersion. Stored as string
        // rather than int so a future enum addition can't silently
        // collide with the persisted value.
        public string ApiVersion { get; set; }
        public int LightCount { get; set; }
        public int ReportLength { get; set; }
    }
}
