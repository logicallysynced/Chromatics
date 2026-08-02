# Changelog

All notable changes to Chromatics are documented here.

## 4.3.33

- Fixed lighting stopping for the rest of the session when the Screen Capture base layer was in use and you returned to the title or character select screen.
- Yeelight, LIFX, Nanoleaf, and Alienware lights that are switched off or off the network at startup now log as device notices instead of errors.

## 4.3.32

- Improved the accuracy and speed of game-data reads on FFXIV patch 7.55, including inventory, chat log, job gauges, and player stats.

## 4.3.31

- Added support for FFXIV patch 7.55.
- Lighting no longer stops for the rest of the session when a single game-data read fails. Chromatics now retries, and reconnects to the game if the failures persist.

## 4.3.29

- Added a Black layer type, available as both a base layer and a dynamic layer.
- Layers saved for a disabled or disconnected device can now be copied to another device.
- New setting under Settings → General: lower the RGB refresh rate while FFXIV is not running. With the setting off, your configured refresh rate now applies in every state.
- Fixed the update prompt showing the newest version's heading twice in its changelog.

## 4.3.24

- **New:** Nanoleaf smart-light support (Beta). Covers the panel family (Shapes, Canvas, Elements, Lines, Aurora) and other controllers that speak the Nanoleaf OpenAPI. Enable it from Settings → Device Providers and pair each controller with a one-time button press; effects render across the panels at their real physical positions. Layer assignments stay with their panels if you add or remove panels from the wall later. Essentials bulbs and strips are not supported.
- Chromatics now closes faster when several smart-light brands are enabled: LIFX, Hue, and Nanoleaf devices restore their original state in parallel instead of one brand at a time.
- Fixed a startup crash on PCs where Windows App Control or Smart App Control blocks one of the bundled device libraries. Chromatics now starts with that provider disabled, logs which file was blocked, and explains how to allow it. This also covers the case where the core lighting library (RGB.NET) itself is blocked during settings load.
- Fixed a crash when opening the Hue, LIFX, or Yeelight device picker with the same bulb saved twice in settings.

## 4.3.14

- Added new dynamic layers for Focus Target HP and Focus Target Castbar.
- Added new effects for Status Inflicted and Casting Success.
- Added new status effects category and items to Palette tab.
- Fixed a bug which caused the Experience Tracker layer to not work with Reaper and Sage.
- Fixed the Keybinds layer misreading hotbar actions with high action ids.
- Updated Sharlayan to latest version
- Various performance improvements and bug fixes

## 4.2.73

- Added support for FFXIV 7.51
- Added an option to remap layers when keyboard type is changed.
- Fixed an issue where AZERTY keyboards weren't functioning properly.
- Fixed an issue which caused Chromatics to crash when changing layer modes.
- Fixed an issue which prevented non-bleed layers from re-applying if disabled and re-enabled, with the Enmity Tracker layer specifically going dark whenever the player wasn't on the target's hate table.
- Fixed an issue where the Enmity Tracker layer stayed dark while a target was engaged.

## 4.2.65

- **New:** EVision keyboard support (Beta). Covers 13 keyboards that share the Sonix VS11K28A firmware - Glorious GMMK TKL, Redragon K550 / K552 / K552-2 / K556, Tecware Phantom Elite, Womier K66 / K87, Mars Gaming MKMini, Skillkorp K5, DEXP Blaze, Warrior Kane TC235, and Gamepower Ogre RGB.
- **New:** Redragon mouse support (Beta). Covers 13 Redragon RGB mice that speak the OpenRGB HID protocol family - M711 Cobra, M715 Dagger, M716 Inquisitor, M602 Griffin, M801 Sniper, M808 Storm, M810 Taipan, M908 Impact, M987 Reaping, M719 Invader, M990 Legend, M709 Tiger, and M721-Pro Lonewolf 2.
- Updated internal Corsair, CoolerMaster, MSI & Wooting SDK libraries.
- Fixed an issue which prevented Corsair and OpenRGB RGB.NET device providers from starting.
- Fixed an error that could appear when Chromatics auto-checked for updates while minimized to the tray.
- Minor fixes and improvements.

## 4.2.50

- **New:** Windows Dynamic Lighting support. This is currently in beta.
- **New:** Yeelight device support. This is currently in beta.
- **New:** Alienware LightFX device support. This is currently in beta.
- **New:** Layers can now be copied between devices. A new copy icon next to the device brightness button on the **Mappings** tab opens a dialog that duplicates every layer from one device onto another.
- Hue, LIFX and Yeelight have been added to the first-run wizard, each with their own discovery flow.
- PlayStation, Hue and LIFX devices are no longer classed as beta.
- Multi-zone keyboards now show assignable keys for each zone instead of using the full keyboard grid intended for per-key LED layouts.
- Updated RGB.NET device providers for Razer, Logitech, SteelSeries, Corsair and OpenRGB.
- Chromatics is now officially code-signed for additional installer security.
- Increased the minimum Windows target to Windows 10 version 1809, build 17763.
- Fixed an issue that prevented Vegas Mode from starting in The Gold Saucer.
- Fixed an issue that could cause the title screen animation to play when loading between zones.
- Fixed an issue where console lines copied to the clipboard could be lost when Chromatics closed.
- Minor fixes and improvements.

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
