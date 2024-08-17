using RGB.NET.Core;
using System.Diagnostics;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue;

public class HueDevice : AbstractRGBDevice<HueDeviceInfo>
{
    public HueDevice(HueDeviceInfo deviceInfo, HueUpdateQueue updateQueue) : base(deviceInfo, updateQueue)
    {
        InitializeLayout();
    }

    private void InitializeLayout()
    {
        Debug.WriteLine($"Hue ADDED: {DeviceInfo.Model}");
        // Models based on https://developers.meethue.com/develop/hue-api/supported-devices/#Supported-lights
        
        Led led = DeviceInfo.Model switch
        {
            // Hue bulb A19 (E27)
            "LCA001" => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),
            "LCA007" => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),
            "LCA0010" => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),
            "LCA0014" => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),
            "LCA0015" => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),
            "LCA0016" => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),
            // Hue Spot BR30 (quick Google search makes it seem like an older generation)
            "LCT002" => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),
            "LCT011" => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),
            // Hue Spot GU10
            "LCT003" => AddLed(LedId.Custom1, new Point(0, 0), new Size(50)),
            // Hue Go
            "LLC020" => AddLed(LedId.Custom1, new Point(0, 0), new Size(150)),
            // Hue LightStrips Plus
            "LCL001" => AddLed(LedId.LedStripe1, new Point(0, 0), new Size(2000, 14)),
            // Hue color candle	
            "LCT012" => AddLed(LedId.Custom1, new Point(0, 0), new Size(39)),
            _ => AddLed(LedId.Custom1, new Point(0, 0), new Size(50))
        };

        // Everything but the LED strip represents a light bulb of some sort and can be a circle
        if (led != null && DeviceInfo.Model != "LCL001")
            led.Shape = Shape.Circle;
    }
}