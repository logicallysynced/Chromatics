using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Chromatics.DeviceInterfaces
{
    public class SteelSeriesInterface
    {
        public static SteelLib InitializeSteelSDK()
        {
            SteelLib steel = null;
            steel = new SteelLib();
            return steel;
        }
    }

    public class SteelSeriesSdkWrapper
    {
        private const string DEFAULT_CONTENT = "application/json";

        public const string GAME = "Final Fantasy XIV";
        private static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;

        public static Dictionary<string, EventBind> GameEvents = new Dictionary<string, EventBind>();

        public static string SendSDKPostResponse(string url, string json)
        {
            var result = "";
            using (var client = new WebClient())
            {
                client.Encoding = DEFAULT_ENCODING;
                client.Headers[HttpRequestHeader.ContentType] = DEFAULT_CONTENT;
                result = client.UploadString(url, "POST", json);
            }

            return result;
        }

        public static void SendSDKPost(string url, string json)
        {
            using (var client = new WebClient())
            {
                client.Encoding = DEFAULT_ENCODING;
                client.Headers[HttpRequestHeader.ContentType] = DEFAULT_CONTENT;
                client.UploadString(url, "POST", json);
            }
        }

        #region Enums

        public enum EventIcons
        {
            None = 0,
            Health = 1,
            Armor = 2,
            Ammo = 3,
            Money = 4,
            Explosion = 5,
            Kills = 6,
            Headshot = 7,
            Helmet = 8,
            Unknown = 9,
            Hunger = 10,
            Breath = 11,
            Compass = 12,
            Pickaxe = 13,
            Potion = 14,
            Clock = 15,
            Lightning = 16,
            Backpack = 17
        }

        public enum IconColors
        {
            Orange = 0,
            Gold = 1,
            Yellow = 2,
            Green = 3,
            Teal = 4,
            LightBlue = 5,
            Blue = 6,
            Purple = 7,
            Fuschia = 8,
            Pink = 9,
            Red = 10,
            Silver = 11
        }

        #endregion

        #region Models

        public class Color
        {
            [JsonProperty(PropertyName = "red")]
            public int red { get; set; }

            [JsonProperty(PropertyName = "green")]
            public int green { get; set; }

            [JsonProperty(PropertyName = "blue")]
            public int blue { get; set; }
        }

        public class Handler
        {
            [JsonProperty(PropertyName = "device-type")]
            public string device { get; set; }

            [JsonProperty(PropertyName = "zone")]
            public string zone { get; set; }

            [JsonProperty(PropertyName = "color")]
            public Color color { get; set; }

            [JsonProperty(PropertyName = "mode")]
            public string mode { get; set; }
        }

        public class EventBind
        {
            [JsonProperty(PropertyName = "device-type")]
            public string game { get; set; }

            [JsonProperty(PropertyName = "event")]
            public string name { get; set; }

            [JsonProperty(PropertyName = "min_value")]
            public int min_value { get; set; }

            [JsonProperty(PropertyName = "max_value")]
            public int max_value { get; set; }

            [JsonProperty(PropertyName = "icon_id")]
            public EventIcons icon_id { get; set; }

            [JsonProperty(PropertyName = "handlers")]
            public Handler handlers { get; set; }
        }


        public class Data
        {
            [JsonProperty(PropertyName = "value")]
            public int value { get; set; }
        }

        public class EventSend
        {
            [JsonProperty(PropertyName = "game")]
            public string game { get; set; }

            [JsonProperty(PropertyName = "event")]
            public string name { get; set; }

            [JsonProperty(PropertyName = "data")]
            public Data data { get; set; }
        }

        #endregion
    }

    public interface ISteelSdk
    {
    }

    public class SteelLib : ISteelSdk
    {
        private void RegisterEvents()
        {
            /*
            //All Keys
            SteelSeriesSdkWrapper.EventBind SetAllKeys = new SteelSeriesSdkWrapper.EventBind();

            SetAllKeys.game = SteelSeriesSdkWrapper.GAME;
            SetAllKeys.name = "SetAllKeys";
            SetAllKeys.handlers = new SteelSeriesSdkWrapper.Handler
            {
                device = "rgb-per-key-zones",
                mode = "color",
                zone = "all",
                color = new SteelSeriesSdkWrapper.Color { red = 0, green = 0, blue = 0 }
            };
            SetAllKeys.icon_id = SteelSeriesSdkWrapper.EventIcons.None;
            SetAllKeys.min_value = 0;
            SetAllKeys.max_value = 100;
            */
        }

        public void ApplyMapKeyLighting(string key, Color col)
        {
            //Single Key
            var guid = Guid.NewGuid().ToString();

            var SetSingleKey = new SteelSeriesSdkWrapper.EventBind
            {
                game = SteelSeriesSdkWrapper.GAME,
                name = guid,
                handlers = new SteelSeriesSdkWrapper.Handler
                {
                    device = "rgb-per-key-zones",
                    mode = "percent",
                    zone = "logo",
                    color = new SteelSeriesSdkWrapper.Color {red = 0, green = 0, blue = 0}
                },
                icon_id = SteelSeriesSdkWrapper.EventIcons.None,
                min_value = 0,
                max_value = 100
            };

            var json = new SteelSeriesSdkWrapper.EventSend
            {
                game = SteelSeriesSdkWrapper.GAME,
                name = guid,
                data = new SteelSeriesSdkWrapper.Data {value = 100}
            };

            SteelSeriesSdkWrapper.GameEvents.Add(guid, SetSingleKey);
            SteelSeriesSdkWrapper.SendSDKPost("", json.ToString());
        }
    }
}