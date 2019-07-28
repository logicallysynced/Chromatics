using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.DeviceInterfaces
{
    public class Localization
    {
        private static KeyRegion _region;

        public static void SetKeyRegion(KeyRegion region)
        {
            _region = region;
        }
        
        public static string LocalizeKey(string key)
        {
            switch (key)
            {
                case "A":
                    if (_region == KeyRegion.AZERTY)
                    {
                        return "Q";
                    }
                    else
                    {
                        return key;
                    }

                case "Q":
                    if (_region == KeyRegion.AZERTY)
                    {
                        return "A";
                    }
                    else
                    {
                        return key;
                    }

                case "S":
                    if (_region == KeyRegion.AZERTY)
                    {
                        return "S";
                    }
                    else
                    {
                        return key;
                    }

                case "W":
                    if (_region == KeyRegion.AZERTY)
                    {
                        return "Z";
                    }
                    else
                    {
                        return key;
                    }

                case "Y":
                    if (_region == KeyRegion.QWERTZ)
                    {
                        return "Z";
                    }
                    else
                    {
                        return key;
                    }
                case "Z":
                    if (_region == KeyRegion.QWERTZ)
                    {
                        return "Y";
                    }
                    else if (_region == KeyRegion.AZERTY)
                    {
                        return "W";
                    }
                    else
                    {
                        return key;
                    }
                default:
                    return key;
            }
        }
    }
}
