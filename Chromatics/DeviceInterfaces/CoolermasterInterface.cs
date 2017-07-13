using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.DeviceInterfaces
{
    class CoolermasterInterface
    {
        public static CoolermasterLib InitializeCoolermasterSDK()
        {
            CoolermasterLib coolermaster = null;
            coolermaster = new CoolermasterLib();

            var coolermasterstat = coolermaster.InitializeSDK();

            if (!coolermasterstat)
                return null;

            return coolermaster;
        }
    }

    public class CoolermasterSdkWrapper
    {
        //
    }

    public interface ICoolermasterSdk
    {
        bool InitializeSDK();
    }

    public class CoolermasterLib : ICoolermasterSdk
    {

        public bool InitializeSDK()
        {
            return true;
        }
    }
}
