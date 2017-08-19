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
        public static SteelLib InitializeSteelSdk()
        {
            SteelLib steel = null;
            steel = new SteelLib();
            return steel;
        }
    }

    public class SteelSeriesSdkWrapper
    {
        private const string DefaultContent = "application/json";

        public const string Game = "Final Fantasy XIV";
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public static Dictionary<string, EventBind> GameEvents = new Dictionary<string, EventBind>();

        public static string SendSdkPostResponse(string url, string json)
        {
            var result = "";
            using (var client = new WebClient())
            {
                client.Encoding = DefaultEncoding;
                client.Headers[HttpRequestHeader.ContentType] = DefaultContent;
                result = client.UploadString(url, "POST", json);
            }

            return result;
        }

        public static void SendSdkPost(string url, string json)
        {
            using (var client = new WebClient())
            {
                client.Encoding = DefaultEncoding;
                client.Headers[HttpRequestHeader.ContentType] = DefaultContent;
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
            public int Red { get; set; }

            [JsonProperty(PropertyName = "green")]
            public int Green { get; set; }

            [JsonProperty(PropertyName = "blue")]
            public int Blue { get; set; }
        }

        public class Handler
        {
            [JsonProperty(PropertyName = "device-type")]
            public string Device { get; set; }

            [JsonProperty(PropertyName = "zone")]
            public string Zone { get; set; }

            [JsonProperty(PropertyName = "color")]
            public Color Color { get; set; }

            [JsonProperty(PropertyName = "mode")]
            public string Mode { get; set; }
        }

        public class EventBind
        {
            [JsonProperty(PropertyName = "device-type")]
            public string Game { get; set; }

            [JsonProperty(PropertyName = "event")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "min_value")]
            public int MinValue { get; set; }

            [JsonProperty(PropertyName = "max_value")]
            public int MaxValue { get; set; }

            [JsonProperty(PropertyName = "icon_id")]
            public EventIcons IconId { get; set; }

            [JsonProperty(PropertyName = "handlers")]
            public Handler Handlers { get; set; }
        }


        public class Data
        {
            [JsonProperty(PropertyName = "value")]
            public int Value { get; set; }
        }

        public class EventSend
        {
            [JsonProperty(PropertyName = "game")]
            public string Game { get; set; }

            [JsonProperty(PropertyName = "event")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "data")]
            public Data Data { get; set; }
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

            var setSingleKey = new SteelSeriesSdkWrapper.EventBind
            {
                Game = SteelSeriesSdkWrapper.Game,
                Name = guid,
                Handlers = new SteelSeriesSdkWrapper.Handler
                {
                    Device = "rgb-per-key-zones",
                    Mode = "percent",
                    Zone = "logo",
                    Color = new SteelSeriesSdkWrapper.Color {Red = 0, Green = 0, Blue = 0}
                },
                IconId = SteelSeriesSdkWrapper.EventIcons.None,
                MinValue = 0,
                MaxValue = 100
            };

            var json = new SteelSeriesSdkWrapper.EventSend
            {
                Game = SteelSeriesSdkWrapper.Game,
                Name = guid,
                Data = new SteelSeriesSdkWrapper.Data {Value = 100}
            };

            SteelSeriesSdkWrapper.GameEvents.Add(guid, setSingleKey);
            SteelSeriesSdkWrapper.SendSdkPost("", json.ToString());
        }
    }
}