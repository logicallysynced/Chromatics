# Changelog

All notable changes to Chromatics are documented here.

## 4.1.45

- Minor fixes to Logitech implementation.
- Some keyboards and headsets used to show up in Chromatics without any usable LEDs. They now get a default key grid filled in automatically, so layers can paint them just like any other device.

## 4.1.44

- **New:** QMK Raw HID keyboard support (Beta). Covers custom keyboards from NovelKeys, KBDFans, Drop, GMMK, Glorious, and any other brand running QMK firmware with Raw HID enabled. Enable it from Settings → Device Providers or pick it on the first-run device selector. Chromatics auto-detects compatible boards over USB and adopts them with no firmware flashing or extra software required. The provider drives per-key lighting through the OpenRGB-QMK plugin when the firmware has it installed; otherwise it controls the firmware's built-in RGB matrix base colour and effect mode via VIA. A pre-built key layout database covering 2650 QMK boards ships with Chromatics, so the Highlight and Keybind layers map to the correct physical keys without manual setup.
- Updated dependency libraries to latest version

## 4.1.38

- Added Auto-discovery for Hue bridges.
- Updated Hue light adoption. After pairing, Chromatics shows a picker so you can choose exactly which Hue lights it should control. Existing setups upgraded from earlier builds keep all their bulbs (one-time auto-adopt of whatever the bridge currently exposes); from then on you can deselect bulbs you don't want from the same dialog.
- Hue bulbs now restore their original colour and on/off state when Chromatics releases control.
- Disabling a device on the Mappings tab now sticks across restarts.
- Fixed an error that could appear in the log when disabling Hue (or any device provider) — "The device 'X' is not attached to this surface."


## 4.1.26

- **New:** PlayStation DS4 and DS5 controller lighting support (Beta). The DualShock 4 lightbar, the DualSense lightbar and the five player-indicator LEDs can all be mapped from the Mappings tab. Both USB and Bluetooth controllers are supported, and your controller's input still works normally in games while Chromatics drives the lights.
Tip: if your controller is connected but Chromatics says it can't open it, another tool (like Steam Input, DS4Windows, or reWASD) is probably holding it in exclusive mode. Close it or disable its exclusive mode before launching Chromatics.
- **New:** LIFX light support (Beta). Discovers LIFX bulbs, strips and tiles on your local network (uses Local LAN protocol — no LIFX cloud account required). When you enable LIFX in Settings, Chromatics shows a picker so you can choose which devices it should control. If no LIFX devices are found on the network, or you don't pick any, the LIFX toggle automatically switches itself back off so you don't end up with an enabled-but-empty provider. Multi-zone strips (Z, Beam, String, Neon) light up per-zone, and matrix devices (Tile, Candle Color) are mapped as a grid. When Chromatics releases control (you disable LIFX in Settings, or close the app), each device is restored to the colour and on/off state it was in before Chromatics took over.
- The Effect Layer enable checkbox on the Mappings tab is now a per-device master toggle for everything on the Effects tab. Turning it off for a device silences raid effects, duty-finder bell, damage flash, cutscene animation, vegas mode, the startup and title-screen animations, and any reactive-weather animation overlays just for that device. Base layer painting (static, job-class colours, weather colour, screen capture) and dynamic layers (HP, target, key bindings) keep running normally.
- Smoother Hue colour transitions. Fast-changing effects (Vegas mode, cutscenes, certain weather animations) used to look strobe-like on Hue bulbs that lack the Hue Play's hardware fade - Chromatics now asks the bridge to interpolate between colour frames so every supported bulb gets smoother motion on effects.
- Updated underlying dependencies for stability and bug fixes.

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
