# Changelog

All notable changes to Chromatics are documented here.

## 4.0.162

- Tightened up PlayStation controller detection so plugging in a DualShock 4 no longer pops a "Failed to get info." error in some cases. Controllers that can't expose their serial number (older DS4s, or anything where Steam / DS4Windows is partially blocking access) are now identified by a stable hash of their device path instead, with a clear log message if the controller still can't be opened for lighting.

## 4.0.161

- Fixed an issue where connecting a DualShock 4 over USB could throw "Failed to get info." when Chromatics tried to read the controller's serial number. Older controllers without a readable serial descriptor now fall back to a stable identity derived from their device path, so mappings still persist correctly between restarts.

## 4.0.160

- PlayStation controllers now get a stable identity based on their serial number, so your saved mappings stay attached to the correct controller across app restarts — and if you have two of the same model, they no longer share settings.

## 4.0.159

- PlayStation controllers now respect the global brightness slider and the per-device brightness slider in the Mappings tab, just like every other device.
- Plug or pair a controller after Chromatics is already running and it's picked up automatically — no app restart needed. Same goes for unplug / unpair: the controller is removed cleanly from the device list when it disappears.
- The PlayStation provider is now marked as Beta in Settings and on the first-run wizard while we gather feedback from real hardware.

## 4.0.158

- Added PlayStation controller lighting support. The DualShock 4 lightbar, the DualSense lightbar, the five player-indicator LEDs, and the mic-mute LED can now all be mapped from the Mappings tab. Both USB and Bluetooth controllers are supported, and your controller's input still works normally in games while Chromatics drives the lights.
- Tip: if your controller is connected but Chromatics says it can't open it, another tool (like DS4Windows or reWASD) is probably holding it in exclusive mode. Disable exclusive mode in that tool, or close it before launching Chromatics.

## 4.0.157

- The first-run welcome screen now lets you pick your Chromatics language right away — and the screen re-translates itself instantly so you can see the change before continuing.
- Tank stance highlighting (Iron Will, Defiance, Royal Guard, Grit) and Summon Seraph now work correctly when the FFXIV client is set to Japanese, German or French.
- Updated the FFXIV memory reader (Sharlayan) with several stability and accuracy fixes — most notably, status effect slots no longer occasionally show the wrong status, and stack counts only display for statuses that actually stack.

## 4.0.156

- Fixed an issue where Chromatics could close itself with a crash dialog if a connected device (Hue, OpenRGB, etc.) had a brief network hiccup. These transient errors are now ignored.
- Saving your layer setup is more reliable: if antivirus, OneDrive, or Dropbox briefly locks the file, Chromatics will retry rather than failing the save.
- Quietened expected startup messages — if iCUE or OpenRGB isn't running, the app no longer treats it as a crash report.
- Updated dependency libraries to latest version

## 4.0.153

Initial public release of Chromatics 4.0
