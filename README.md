**LOOKING FOR C# DEVELOPERS TO HELP MAINTAIN THIS - Please contact me if you're keen to get involved!**

![Chromatics Logo](http://thejourneynetwork.net/chromatics/chromatics_black_md.png)

[![Github All Releases](https://img.shields.io/github/downloads/roxaskeyheart/Chromatics/total.svg)](https://github.com/roxaskeyheart/Chromatics/releases)
[![Github Latest Releases](https://img.shields.io/github/downloads/roxaskeyheart/Chromatics/latest/total.svg)](https://github.com/roxaskeyheart/Chromatics/releases/latest)
[![Latest Release](https://img.shields.io/github/release/roxaskeyheart/Chromatics.svg)](https://github.com/roxaskeyheart/Chromatics/releases/latest)
[![Discord](https://img.shields.io/discord/334196655131721741.svg)](https://discord.gg/sK47yFE)

[Join Support Discord](https://discord.gg/sK47yFE)


***Important:** This is a new branch for Chromatics 3.x series. It is a complete rebuild of Chromatics from the ground up in .NET 6. It will utilise the new version of Sharlayan for async FFXIV calls and RGB.NET to standardise RGB device SDK's (as opposed to manually managing them as Chromatics 2.x did.*
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
Chromatics is compatible with a wide range of RGB devices, supported by the library RGB.NET. Any devices supported by RGB.NET should be supported by Chromatics.

<br><br>
### Changes from Chromatics 2.x ###

**Device Compatibility**
<br>
Chromatics 2.x (and 1.x) was originally designed to work only with Razer RGB devices using a dedicated library. Over time we added vendor devices, but each of these required their own seperate library to work with their vendor devices. After many years this has made the codebase very difficult to maintain and update due to managing upwards of 8 different libraries, to the point where it became impossible for me to keep them all up to date.
<br><br>
In Chromatics 3.x we are implementing a single library, [RGB.NET](https://github.com/DarthAffe/RGB.NET) that is designed to work with multiple vendors in a unified way. This will make continued development and management much easier, as well as far more memory/CPU efficient. The trade off to this is Chromatics' device compatibility will be limited to what vendors & devices RGB.NET supports. If your device is not currently supported, please get in touch with the developers of RGB.NET to have it implemented.
<br><br>
**Features**
<br>
As this is a rebuild of Chromatics from the ground up, there will be some features that may be in Chromatics 2.x which are not yet implemented in 3.x. In addition some features that are in 2.x won't be ported across at all. While some of the features added (such as Google Cast, Logitech ARX, API access, etc.) were cool, they were unrelated to the core functionality of Chromatics (RGB peripheral lighting) and started to bloat the app in terms of codebase and performance. If there is significant demand for a specific feature however, we will consider implementing it again in Chromatics 3. Please submit a feature request on Discord to let us know your favourite feature - but we won't promise anything.
<br><br>
**FFXIV Integration/New Updates**
<br>
Chromatics 3.x will still rely on Sharlayan for reading FFXIV memory. The main reason for this is we don't currently have the knowledge to implement our own memory scanner. This means, at least for the time being, compatibility with new major versions of FFXIV will be delayed as Sharlayan has become unmanaged by its original developers. We are considering other options at this time, such as FFXIVClientStructs and Dalamud, but for the time being these libraries don't meet our core requirements.
<br><br><br>
### Developers ### 
If you wish to build Chromatics yourself, you can download the active branch and open in Visual Studio 2022. Please pull all nuget packages and also link any additional libraries from Build Dependencies before building. If you need any further assistance, please contact us on Discord.
<br><br><br>
### Open Source Libraries ### 
* [RGB.NET](https://github.com/DarthAffe/RGB.NET) - used for RGB device integration
* [Sharlayan](https://github.com/FFXIVAPP/sharlayan) - used for FFXIV memory reading
* [MetroModernUI](https://github.com/dennismagno/metroframework-modern-ui) - used as a user interface base in winforms
* [Cyotek ColorPicker](https://github.com/cyotek/Cyotek.Windows.Forms.ColorPicker) - user interface components for choosing colors
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - used for everything JSON
* [Aurora](https://github.com/antonpup/Aurora) - borrowed some code base, not actually a dependency
* [Artemis](https://github.com/Artemis-RGB/Artemis) - borrowed some code base and RGB.NET profiles, not actually a dependency
* [FFXIVWeather](https://github.com/karashiiro/FFXIVWeather) - For calculating current weather.
