using Chromatics.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    public class DeviceHelper
    {

        public static Guid GenerateDeviceGuid(string deviceName)
        {
            // Use MD5 hash to get a 16-byte hash of the string
            using (var provider = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.Default.GetBytes(deviceName);
                byte[] hashBytes = provider.ComputeHash(inputBytes);
                // Generate a new Guid from the hash bytes
                return new Guid(hashBytes);
            }
        }

    }
}
