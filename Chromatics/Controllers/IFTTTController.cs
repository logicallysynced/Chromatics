using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromatics.Datastore;
using EasyHttp.Http;

namespace Chromatics.Controllers
{
    public static class IFTTTController
    {
        public static void FireIFTTTEvent(string eventName, string IFTTTUrl)
        {
            var http = new HttpClient();
            var iftttcode = IFTTTUrl.Replace("https://maker.ifttt.com/use/", "");
            http.Post(@"https://maker.ifttt.com/trigger/" + eventName + @"/with/key/" + iftttcode, @"", HttpContentTypes.TextPlain);
            Console.WriteLine(@"https://maker.ifttt.com/trigger/" + eventName + @"/with/key/" + iftttcode);
        }
    }
}
