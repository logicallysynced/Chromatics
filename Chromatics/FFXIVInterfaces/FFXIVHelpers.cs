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

        public static bool IsCompanyAction(string action)
        {
            switch(action)
            {
                case "The Heat Of Battle":
                    return true;
                case "Meat And Mead":
                    return true;
                case "Earth And Water":
                    return true;
                case "Helping Hand":
                    return true;
                case "A Man's Best Friend":
                    return true;
                case "Mark Up":
                    return true;
                case "Seal Sweetener":
                    return true;
                case "Jackpot":
                    return true;
                case "Brave New World":
                    return true;
                case "Live Of The Land":
                    return true;
                case "What You See":
                    return true;
                case "Eat From The Hand":
                    return true;
                case "In Control":
                    return true;
                case "That Which Binds Us":
                    return true;
                case "Proper Care":
                    return true;
                case "Back On Your Feet":
                    return true;
                case "Reduced Rates":
                    return true;
                default:
                    return false;
            }
        }

        public static readonly Dictionary<int, int> ExperienceTable = new Dictionary<int, int>()
        {
            {0, 300},
            {1, 600},
            {2, 1100},
            {3, 1700},
            {4, 2300},
            {5, 4200},
            {6, 6000},
            {7, 7350},
            {8, 9930},
            {9, 11800},
            {10, 15600},
            {11, 19600},
            {12, 23700},
            {13, 26400},
            {14, 30500},
            {15, 35400},
            {16, 40500},
            {17, 45700},
            {18, 51000},
            {19, 56600},
            {20, 63900},
            {21, 71400},
            {22, 79100},
            {23, 87100},
            {24, 95200},
            {25, 109800},
            {26, 124800},
            {27, 140200},
            {28, 155900},
            {29, 162500},
            {30, 175900},
            {31, 189600},
            {32, 203500},
            {33, 217900},
            {34, 232320},
            {35, 249900},
            {36, 267800},
            {37, 286200},
            {38, 304900},
            {39, 324000},
            {40, 340200},
            {41, 356800},
            {42, 373700},
            {43, 390800},
            {44, 408200},
            {45, 437600},
            {46, 467500},
            {47, 498000},
            {48, 529000},
            {49, 864000},
            {50, 1058400},
            {51, 1267200},
            {52, 1555200},
            {53, 1872000},
            {54, 2217600},
            {55, 2592000},
            {56, 2995200},
            {57, 3427200},
            {58, 3888000},
            {59, 4470000},
            {60, 4873000},
            {61, 5316000},
            {62, 5809000},
            {63, 6364000},
            {64, 6995000},
            {65, 7722000},
            {66, 8575000},
            {67, 9593000},
            {68, 10826000},
            {69, 12449000},
            {70, 13881000},
            {71, 15556000},
            {72, 17498600},
            {73, 19750000},
            {74, 22330000},
            {75, 25340000},
            {76, 28650000},
            {77, 32750000},
            {78, 37650000},
            {79, 43300000},
            {80, 0 }
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
