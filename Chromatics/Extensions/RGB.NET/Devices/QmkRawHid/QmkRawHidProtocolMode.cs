namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // Which protocol the UpdateQueue should target on a given device. Set
    // once at handshake (see QmkRawHidDiscovery) and stable for the
    // lifetime of the device — we don't downgrade mid-session.
    public enum QmkRawHidProtocolMode
    {
        ViaOnly,
        OpenRgbQmk,
    }
}
