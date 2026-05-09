# Changelog

All notable changes to Chromatics are documented here.

## 4.1.22

- **New:** PlayStation DS4 and DS5 controller lighting support (Beta). The DualShock 4 lightbar, the DualSense lightbar and the five player-indicator LEDs can all be mapped from the Mappings tab. Both USB and Bluetooth controllers are supported, and your controller's input still works normally in games while Chromatics drives the lights.
- Tip: if your controller is connected but Chromatics says it can't open it, another tool (like Steam Input, DS4Windows, or reWASD) is probably holding it in exclusive mode. Close it or disable its exclusive mode before launching Chromatics.
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
