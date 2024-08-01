using System;
using System.Linq;
using Q42.HueApi.Streaming.Models;
using RGB.NET.Core;
using RGBColor = Q42.HueApi.ColorConverters.RGBColor;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue;

public class HueUpdateQueue : UpdateQueue
{
    #region Properties & Fields

    private readonly StreamingLight _light;

    #endregion

    #region Constructors

    public HueUpdateQueue(IDeviceUpdateTrigger updateTrigger, string lightId, StreamingGroup group)
        : base(updateTrigger)
    {
        _light = group.First(l => l.Id == byte.Parse(lightId));
    }

    #endregion

    #region Methods

    protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
    {
        try
        {
            Color color = dataSet[0].color;

            _light.State.SetBrightness(1);
            _light.State.SetRGBColor(new RGBColor(color.R, color.G, color.B));

            return true;
        }
        catch (Exception ex)
        {
            HueRGBDeviceProvider.Instance.Throw(ex);
        }

        return false;
    }

    #endregion
}