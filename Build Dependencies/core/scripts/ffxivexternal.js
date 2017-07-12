var FFXIV_ETime;
var FFXIV_HPPercent;
var FFXIV_MPPercent;
var FFXIV_TPPercent;
var FFXIV_HPCurrent;
var FFXIV_MPCurrent;
var FFXIV_TPCurrent;
var FFXIV_HPMax;
var FFXIV_MPMax;
var FFXIV_TPMax;
var FFXIV_HudType;
var FFXIV_Location;
var FFXIV_HudMode;
var FFXIV_TargetHPPercent;
var FFXIV_TargetHPCurrent;
var FFXIV_TargetHPMax;
var FFXIV_TargetName;
var FFXIV_TargetEngaged;

var FFXIV_PlayerPosX;
var FFXIV_PlayerPosY;
var FFXIV_PlayerPosZ;
var FFXIV_ActionStatus;
var FFXIV_CastPercent;
var FFXIV_CastProgress;
var FFXIV_CastTime;
var FFXIV_CastingToggle;
var FFXIV_HitboxRadius;
var FFXIV_PlayerClaimed;
var FFXIV_PlayerJob;
var FFXIV_MapID;
var FFXIV_MapIndex;
var FFXIV_MapTerritory;
var FFXIV_PlayerName = "Debug";
var FFXIV_TargetType;

updateStats();
window.setInterval(updateStats, 300);
console.warn("Startup");
//var i = 0;
//$("#debug1").text(i);

function updateStats() {
	
	FFXIV_HPPercent = document.querySelector("#hp_percent").textContent;
	FFXIV_MPPercent = document.querySelector("#mp_percent").textContent;
	FFXIV_TPPercent = document.querySelector("#tp_percent").textContent;
	FFXIV_HPCurrent = document.querySelector("#hp_current").textContent;
	FFXIV_MPCurrent = document.querySelector("#mp_current").textContent;
	FFXIV_TPCurrent = document.querySelector("#tp_current").textContent;
	FFXIV_HPMax = document.querySelector("#hp_max").textContent;
	FFXIV_MPMax = document.querySelector("#mp_max").textContent;
	FFXIV_TPMax = "1000";
	FFXIV_HudType = document.querySelector("#hud_type").textContent;
	FFXIV_Location = document.querySelector("#current_location").textContent;
	FFXIV_HudMode = document.querySelector("#hud_mode").textContent;
	FFXIV_TargetHPPercent = document.querySelector("#target_hppercent").textContent;
	FFXIV_TargetHPCurrent = document.querySelector("#target_hpcurrent").textContent;
	FFXIV_TargetHPMax = document.querySelector("#target_hpmax").textContent;
	FFXIV_TargetName = document.querySelector("#target_name").textContent;
	FFXIV_TargetEngaged = document.querySelector("#target_engaged").textContent;
	FFXIV_PlayerPosX = document.querySelector("#playerposX").textContent;
	FFXIV_PlayerPosY = document.querySelector("#playerposY").textContent;
	FFXIV_PlayerPosZ = document.querySelector("#playerposZ").textContent;
	FFXIV_ActionStatus = document.querySelector("#actionstatus").textContent;
	FFXIV_CastPercent = document.querySelector("#castperc").textContent;
	FFXIV_CastProgress = document.querySelector("#castprogress").textContent;
	FFXIV_CastTime = document.querySelector("#casttime").textContent;
	FFXIV_CastingToggle = document.querySelector("#castingtoggle").textContent;
	FFXIV_HitboxRadius = document.querySelector("#hitboxrad").textContent;
	FFXIV_PlayerClaimed = document.querySelector("#playerclaimed").textContent;
	FFXIV_PlayerJob = document.querySelector("#playerjob").textContent;
	FFXIV_MapID = document.querySelector("#mapid").textContent;
	FFXIV_MapIndex = document.querySelector("#mapindex").textContent;
	FFXIV_MapTerritory = document.querySelector("#mapterritory").textContent;
	FFXIV_PlayerName = $("#playername").text();
	FFXIV_TargetType = document.querySelector("#targettype").textContent;
	
	//document.querySelector("#debug1").textContent = "Increment: " + i;
	//$("#debug1").text("Increment: " + i);
	//i++;
	/*
	var FFXIV = {
  	HPPercent:FFXIV_HPPercent,
		MPPercent:FFXIV_MPPercent,
		TPPercent:FFXIV_TPPercent,
		HPCurrent:FFXIV_HPCurrent,
		MPCurrent:FFXIV_MPCurrent,
		TPCurrent:FFXIV_TPCurrent,
		HPMax:FFXIV_HPMax,
		MPMax:FFXIV_MPMax,
		TPMax:FFXIV_TPMax,
		HudType:FFXIV_HudType,
		Location:FFXIV_Location,
		HudMode:FFXIV_HudMode,
		TargetHPPercent:FFXIV_TargetHPPercent,
		TargetHPCurrent:FFXIV_TargetHPCurrent,
		TargetHPMax:FFXIV_TargetHPMax,
		TargetName:FFXIV_TargetName,
		TargetEngaged:FFXIV_TargetEngaged,
		PlayerPosX:FFXIV_PlayerPosX,
		PlayerPosY:FFXIV_PlayerPosY,
		PlayerPosZ:FFXIV_PlayerPosZ,
		ActionStatus:FFXIV_ActionStatus,
		CastPercent:FFXIV_CastPercent,
		CastProgress:FFXIV_CastProgress,
		CastTime:FFXIV_CastTime,
		CastingToggle:FFXIV_CastingToggle,
		HitboxRadius:FFXIV_HitboxRadius,
		PlayerClaimed:FFXIV_PlayerClaimed,
		PlayerJob:FFXIV_PlayerJob,
		MapID:FFXIV_MapID,
		MapIndex:FFXIV_MapIndex,
		MapTerritory:FFXIV_MapTerritory,
		PlayerName:FFXIV_PlayerName,
		TargetType:FFXIV_TargetType
	};
	*/
	/*
	FFXIV_HPPercent = document.querySelector("#hp_percent").textContent;
	FFXIV_MPPercent = document.querySelector("#mp_percent").textContent;
	FFXIV_TPPercent = document.querySelector("#tp_percent").textContent;
	FFXIV_HPCurrent = document.querySelector("#hp_current").textContent;
	FFXIV_MPCurrent = document.querySelector("#mp_current").textContent;
	FFXIV_TPCurrent = document.querySelector("#tp_current").textContent;
	FFXIV_HPMax = document.querySelector("#hp_max").textContent;
	FFXIV_MPMax = document.querySelector("#mp_max").textContent;
	FFXIV_TPMax = "1000";
	FFXIV_HudType = document.querySelector("#hud_type").textContent;
	FFXIV_Location = document.querySelector("#current_location").textContent;
	FFXIV_HudMode = document.querySelector("#hud_mode").textContent;
	FFXIV_TargetHPPercent = document.querySelector("#target_hppercent").textContent;
	FFXIV_TargetHPCurrent = document.querySelector("#target_hpcurrent").textContent;
	FFXIV_TargetHPMax = document.querySelector("#target_hpmax").textContent;
	FFXIV_TargetName = document.querySelector("#target_name").textContent;
	FFXIV_TargetEngaged = document.querySelector("#target_engaged").textContent;
	FFXIV_PlayerPosX = document.querySelector("#playerposX").textContent;
	FFXIV_PlayerPosY = document.querySelector("#playerposY").textContent;
	FFXIV_PlayerPosZ = document.querySelector("#playerposZ").textContent;
	FFXIV_ActionStatus = document.querySelector("#actionstatus").textContent;
	FFXIV_CastPercent = document.querySelector("#castperc").textContent;
	FFXIV_CastProgress = document.querySelector("#castprogress").textContent;
	FFXIV_CastTime = document.querySelector("#casttime").textContent;
	FFXIV_CastingToggle = document.querySelector("#castingtoggle").textContent;
	FFXIV_HitboxRadius = document.querySelector("#hitboxrad").textContent;
	FFXIV_PlayerClaimed = document.querySelector("#playerclaimed").textContent;
	FFXIV_PlayerJob = document.querySelector("#playerjob").textContent;
	FFXIV_MapID = document.querySelector("#mapid").textContent;
	FFXIV_MapIndex = document.querySelector("#mapindex").textContent;
	FFXIV_MapTerritory = document.querySelector("#mapterritory").textContent;
	FFXIV_PlayerName = document.querySelector("#playername").textContent;
	FFXIV_TargetType = document.querySelector("#targettype").textContent;
	*/
}

/*
var E_TIME = 20.5714285714;
var global = {
		utcTime: null,
		eorzeaTime: null
};
window.setInterval(updateClock, Math.floor(1000 * 60 /  E_TIME));

function updateClock() {
		global.utcTime = new Date().getTime();
		var eo_timestamp = Math.floor(global.utcTime * E_TIME);
		global.eorzeaTime = new Date();
		global.eorzeaTime.setTime(eo_timestamp);
		showTime();
}

function showTime() {
		var d = new Date();
		d.setTime(global.eorzeaTime);
		var eTime = document.getElementById('e-time');
		var hours = d.getUTCHours();
		var ampm = hours > 11 ? "PM" : "AM";
		if(hours > 12)
		{
				hours -= 12;
		}
		hours = padLeft(hours);
		var minutes = d.getUTCMinutes();
		minutes = padLeft(minutes);
		FFXIV_ETime = hours + ":" + minutes + " " + ampm;
}

function padLeft(val){
		var str = "" + val;
		var pad = "00";
		return pad.substring(0, pad.length - str.length) + str;
}
*/
