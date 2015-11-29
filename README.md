# Chromatics

####About Chromatics

Chromatics is a plugin for Advance Combat Tracker (ACT) which connects Final Fantasy XIV (FFXIV) with Razer Chroma & Logitech RGB devices to create visual alerts using the devices LED's for various FFXIV & ACT functions.


####Features

* Set a device default color universally while ACT is running.
* Change color of devices when emnity is generated.
* Create alerts when Custom Triggers activate with adjustable rate & speed.
* Create alerts when Timers are triggered.
* Create alerts for recieving chat messages.
* Raid Mode for creating special effects when in Raiding instances.
* Vegas Mode for Gold Saucer.
* Device manager to enable/disable devices in ACT (partially implemented).


####Device Compatibility

**Razer Chroma**
* BlackWidow Chroma
* BlackWidow TE Chroma
* DeathAdder Chroma
* DeathStalker Chroma (Partial)
* Diamondback (Partial)
* Firefly
* Kraken 7.1 Chroma
* Mamba TE Chroma
* Orbweaver Chroma
* Tartarus Chroma

**Logitech RGB**
* G910 Orion Spark
* G710+
* G633 & G933
* G600
* G510/G510s
* G110
* G19/G19s
* G105
* G105 COD
* G300
* G303 Daedalus Apex
* G11
* G13
* G15

*Please note: Logitech RGB devices are untested at this current point in time*


####Prerequisites

* [Advance Combat Tracker](http://advancedcombattracker.com/) with FFXIV Plugin
* [Razer SDK](http://www.razerzone.com/au-en/synapse) (Automatically installed with Razer Synapse)
* .Net Framework 4.5 (Full)


####Installation

1. Download the [latest version](https://github.com/roxaskeyheart/Chromatics/releases) of Chromatics.
2. Unzip and copy the folder to your ACT installation directory. (C:\Program Files (x86)\Advanced Combat Tracker)
3. Turn on Chroma Apps in Razer Synapse (Synapse > Chroma Apps > Enable).
4. Open Advance Combat Tracker.
5. Open the Plugins tab.
6. Browse to your ACT installation directory and select the Chromatics.dll in the Chromatics folder.
7. Select Add/Enable Plugin.
8. A new Chromatics tab will appear, select this to customize settings.


####Known Issues

* Device selection only partially implemented.
* An issue in which plugin may cause ACT to crash if plugin is removed and re-added to ACT without restarting ACT in-between.
* Untested on Logitech RGB devices.
