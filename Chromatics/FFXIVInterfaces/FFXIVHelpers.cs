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

        public static readonly Dictionary<int, int> ExperienceTable = new Dictionary<int, int>()
        {
            {0, 300},
            {1, 300},
            {2, 600},
            {3, 1100},
            {4, 1700},
            {5, 2300},
            {6, 4200},
            {7, 6000},
            {8, 7350},
            {9, 9930},
            {10, 11800},
            {11, 15600},
            {12, 19600},
            {13, 23700},
            {14, 26400},
            {15, 30500},
            {16, 35400},
            {17, 40500},
            {18, 45700},
            {19, 51000},
            {20, 56600},
            {21, 63900},
            {22, 71400},
            {23, 79100},
            {24, 87100},
            {25, 95200},
            {26, 109800},
            {27, 124800},
            {28, 140200},
            {29, 155900},
            {30, 162500},
            {31, 175900},
            {32, 189600},
            {33, 203500},
            {34, 217900},
            {35, 232320},
            {36, 249900},
            {37, 267800},
            {38, 286200},
            {39, 304900},
            {40, 324000},
            {41, 340200},
            {42, 356800},
            {43, 373700},
            {44, 390800},
            {45, 408200},
            {46, 437600},
            {47, 467500},
            {48, 498000},
            {49, 529000},
            {50, 864000},
            {51, 1058400},
            {52, 1267200},
            {53, 1555200},
            {54, 1872000},
            {55, 2217600},
            {56, 2592000},
            {57, 2995200},
            {58, 3427200},
            {59, 3888000},
            {60, 4470000},
            {61, 4873000},
            {62, 5316000},
            {63, 5809000},
            {64, 6364000},
            {65, 6995000},
            {66, 7722000},
            {67, 8575000},
            {68, 9593000},
            {69, 10826000},
            {70, 12449000},
            {71, 13881000},
            {72, 15556000},
            {73, 17498600},
            {74, 19750000},
            {75, 22330000},
            {76, 25340000},
            {77, 28650000},
            {78, 32750000},
            {79, 37650000},
            {80, 43300000},
        };
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
