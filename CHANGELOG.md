# Changelog

All notable changes to Chromatics are documented here.

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
