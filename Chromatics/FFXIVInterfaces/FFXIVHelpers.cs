using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chromatics.FFXIVInterfaces
{
    public class FFXIVHelpers
    {
        private static DateTime ToEorzeaTime(DateTime date)
        {
            const double EORZEA_MULTIPLIER = 3600D / 175D;

            // Calculate how many ticks have elapsed since 1/1/1970
            long epochTicks = date.ToUniversalTime().Ticks - (new DateTime(1970, 1, 1).Ticks);

            // Multiply those ticks by the Eorzea multipler (approx 20.5x)
            long eorzeaTicks = (long)Math.Round(epochTicks * EORZEA_MULTIPLIER);

            return new DateTime(eorzeaTicks);
        }

        public static DateTime FetchEorzeaTime()
        {
            return ToEorzeaTime(DateTime.Now);
        }

    }

    public static class ImageExtensions
    {
        public static byte[] ToByteArray(this Image image, ImageFormat format, bool reverse)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);

                if (reverse)
                {
                    var bytes = ms.ToArray();
                    var reversed = bytes.Reverse().ToArray();
                    return reversed;
                }

                return ms.ToArray();
            }
        }

        public static Image Convert(Bitmap oldbmp)
        {
            using (var ms = new MemoryStream())
            {
                oldbmp.Save(ms, ImageFormat.Gif);
                ms.Position = 0;
                return Image.FromStream(ms);
            }
        }
        
    }
}
