**LOOKING FOR C# DEVELOPERS TO HELP MAINTAIN THIS - Please contact me if you're keen to get involved!**

![Chromatics Logo](http://thejourneynetwork.net/chromatics/chromatics_black_md.png)

[![Github All Releases](https://img.shields.io/github/downloads/roxaskeyheart/Chromatics/total.svg)](https://github.com/roxaskeyheart/Chromatics/releases)
[![Github Latest Releases](https://img.shields.io/github/downloads/roxaskeyheart/Chromatics/latest/total.svg)](https://github.com/roxaskeyheart/Chromatics/releases/latest)
[![Latest Release](https://img.shields.io/github/release/roxaskeyheart/Chromatics.svg)](https://github.com/roxaskeyheart/Chromatics/releases/latest)
[![Discord](https://img.shields.io/discord/334196655131721741.svg)](https://discord.gg/sK47yFE)

[Join Support Discord](https://discord.gg/sK47yFE)
<br>
[Documentation](https://docs.chromaticsffxiv.com/chromatics-3)
<br>
<br>
***Important:** This is the Chromatics 4.x series. It is a complete rebuild of Chromatics from the ground up in .NET 10. It will utilise the new version of Sharlayan for async FFXIV calls and RGB.NET to standardise RGB device SDK's (as opposed to manually managing them as Chromatics 2.x did.*

> ### ⚠️ Upgrading from Chromatics 3?
> Chromatics 4 stores your settings in a new location on your computer, and **your previous settings will not be carried across automatically**. You can bring your layers and colour palette across manually using the steps below.
>
> **To bring your layer mappings across:**
> 1. Find your old `layers.chromatics3` file. It will be in the same folder you had Chromatics 3 installed in.
> 2. Open Chromatics 4 and go to the **Mapping** tab.
> 3. Click **Import** and select your `layers.chromatics3` file.
>
> **To bring your colour palette across:**
> 1. Find your old `palette.chromatics3` file. It will be in the same folder you had Chromatics 3 installed in.
> 2. Open Chromatics 4 and go to the **Palette** tab.
> 3. Click **Import** and select your `palette.chromatics3` file.
>
> Your effects settings will need to be configured again from scratch. We apologise for the inconvenience — the rebuild was significant enough that a fully automatic migration was not possible.
<br><br>
Chromatics is a third-party add-on for Final Fantasy XIV which creates lighting effects on your RGB devices. There are many different scenes and effects available including:
* HP/MP/GP/CP
* Keybinds - lights your keys depending on your mapped keybind status
* Castbar progress
* Target HP/Target Castbar progress
* Job Gauges
* Enmity Tracker
* Battle Stance
* Reactive Weather - displays static & animation weather effects
* Duty Finder Bell - flash your device when your DF pops
* Damage Flash - flash your device when you take damage
* Gold Saucer Vegas Mode
* Title screen & cutscene animations
<br>
<img src="https://chromaticsffxiv.com/img/Chromatics4_MappingScreen.png" alt="Chromatics Palettes">
<br>
<br>
Chromatics is compatible with a wide range of RGB devices, supported by the library RGB.NET. Any devices supported by RGB.NET should be supported by Chromatics.

<br><br><br>
### Developers ### 
If you wish to build Chromatics yourself, you can download the active branch and open in Visual Studio 2022. Please pull all nuget packages and also link any additional libraries from Build Dependencies before building. If you need any further assistance, please contact us on Discord.
<br><br><br>
### Open Source Libraries ### 
* [RGB.NET](https://github.com/DarthAffe/RGB.NET) - used for RGB device integration
* [Sharlayan](https://github.com/FFXIVAPP/sharlayan) - used for FFXIV memory reading
* [Artemis](https://github.com/Artemis-RGB/Artemis) - borrowed some code base and RGB.NET profiles, not actually a dependency
* [FFXIVWeather](https://github.com/karashiiro/FFXIVWeather) - For calculating current weather.
* [Sentry](https://sentry.io/) - error monitoring, logs, metrics, tracing, and profiling
<br><br><br>
### Privacy / Telemetry ###
Chromatics uses [Sentry](https://sentry.io/) to collect anonymous crash reports, error logs, performance traces, and profiling samples. This data helps us identify and fix bugs that affect real users.

**What we collect:**
* Unhandled exception stack traces and the immediately preceding log lines
* The Chromatics version, your operating system version, and the .NET runtime version
* Performance traces (a 20% sample of update cycles) and profiling data
* Anonymous session counts used to compute crash-free release health
* Optional free-text comments only when you explicitly type into the crash dialog

**What we do NOT collect:**
* Your name, email, or any other personally identifying information — the crash dialog asks only for free-text comments
* Your character name, server, free company, or any FFXIV account information
* Your IP address (Sentry strips this; `SendDefaultPii` is disabled)
* The contents of your screen, keystrokes, or any input
* File paths or settings outside of what is directly relevant to a crash
* Your bridge keys, light IDs, or any device credentials

**Opting out:** Crash reporting is enabled by default but can be turned off at any time from **Settings → Advanced → Send anonymous crash reports**. When disabled, the Sentry SDK is paused and no network traffic is generated.

Beta and stable channel reports are tagged separately so beta-channel issues do not affect the stable crash-free rate.
<br><br><br>
### Disclaimer ###
Chromatics is not in anyway affiliated with Square Enix or FINAL FANTASY. All rights to their respected owners.

© 2010-2026 SQUARE ENIX CO., LTD. All Rights Reserved. A REALM REBORN is a registered trademark or trademark of Square Enix Co., Ltd. FINAL FANTASY, SQUARE ENIX and the SQUARE ENIX logo are registered trademarks or trademarks of Square Enix Holdings Co., Ltd.
