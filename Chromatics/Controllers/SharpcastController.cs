using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models.Media;

namespace Chromatics.Controllers
{
    public static class SharpcastController
    {
        private static IEnumerable<IReceiver> _chromecasts = new List<IReceiver>();
        private static Dictionary<string, IReceiver> Chromecasts = new Dictionary<string, IReceiver>();
        private static string _selectedDevice;

        private static Sender _sender;
        
        public static async Task InitSharpcastAsync()
        {
            Console.WriteLine(@"Scanning for Chromecast Devices..");
            _chromecasts = await new DeviceLocator().FindReceiversAsync();
        }

        public static Dictionary<string, IReceiver> ReturnActiveChromecasts()
        {
            foreach (var casts in _chromecasts)
            {
                if (!Chromecasts.ContainsKey(casts.Id))
                    Chromecasts.Add(casts.Id, casts);
            }

            return Chromecasts;
        }

        public static void EndSharpcaster()
        {
            Chromecasts.Clear();
        }

        public static void SetActiveDevice(string devId)
        {
            _selectedDevice = devId;
        }

        public static async void CastMedia(string filename)
        {
            _sender = new Sender();
            var path = @"https://chromaticsffxiv.com/chromatics2/cast/" + filename;

            // Connect to the Chromecast
            if (Chromecasts.ContainsKey(_selectedDevice))
            {
                await _sender.ConnectAsync(Chromecasts[_selectedDevice]);
                // Launch the default media receiver application
                var mediaChannel = _sender.GetChannel<IMediaChannel>();
                await _sender.LaunchAsync(mediaChannel);
                // Load and play Big Buck Bunny video
                var mediaStatus = await mediaChannel.LoadAsync(
                    new MediaInformation() { ContentId = path });

                await mediaChannel.PlayAsync();
            }
        }
    }
}
