//Eorzea Time
var E_TIME = 20.5714285714;
var global = {
		utcTime: null,
		eorzeaTime: null
};
window.setInterval(updateClock, Math.floor(1000 * 60 /  E_TIME));
window.setInterval(GetTheme, 3000);

GetTheme();
		
function GetTheme() {
	var s_theme = $("#theme").html();
	
	if (s_theme === "cycle")
	{
		var d = new Date();
		d.setTime(global.eorzeaTime);
		var hours = d.getUTCHours();	
		
		if (hours >= 6 && hours < 18)
		{
			$('body').removeClass('light grey dark black').addClass('light');
		} else {
			$('body').removeClass('light grey dark black').addClass('dark');
		}
		
	} else if (s_theme === "light")
	{
		$('body').removeClass('light grey dark black').addClass('light');
	} else if (s_theme === "grey")
	{
		$('body').removeClass('light grey dark black').addClass('grey');
	} else if (s_theme === "dark")
	{
		$('body').removeClass('light grey dark black').addClass('dark');
	} else if (s_theme === "black")
	{
		$('body').removeClass('light grey dark black').addClass('black');
	}
}

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
				hours -= 12;
		hours = padLeft(hours);
		var minutes = d.getUTCMinutes();
		minutes = padLeft(minutes);
		eTime.innerHTML = hours + ":" + minutes + " " + ampm;
}

function padLeft(val){
		var str = "" + val;
		var pad = "00";
		return pad.substring(0, pad.length - str.length) + str;
}