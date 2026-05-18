using System;
using System.Runtime.InteropServices;

namespace Chromatics.Helpers
{
    // Forces Windows to materialise pending clipboard data into the system
    // clipboard so it survives the source app exiting.
    //
    // Avalonia's IClipboard.SetTextAsync on Windows hands the data over via
    // OLE's delayed-rendering path: the app stays the clipboard provider
    // and Windows asks for the bytes back when another process pastes.
    // When the provider process exits, the registration is gone and any
    // pasted content goes empty. OleFlushClipboard() walks the delayed
    // formats and serialises them straight into the system clipboard,
    // disconnecting the provider so subsequent pastes work without the
    // app being alive.
    //
    // Call this after every explicit clipboard write Chromatics performs
    // (Copy All button, crash-dialog event-id copy), and at process
    // shutdown to cover Ctrl+C inside Avalonia TextBoxes (whose default
    // copy command goes through the same delayed-rendering path).
    public static class ClipboardHelper
    {
        [DllImport("ole32.dll")]
        private static extern int OleFlushClipboard();

        // Best-effort. Returns true on S_OK; logs and swallows on any
        // failure (clipboard locked by another process, no OLE on this
        // session, etc.) — we never want a clipboard flush to crash
        // shutdown or a UI handler.
        public static bool FlushOleClipboard()
        {
            try
            {
                return OleFlushClipboard() == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
