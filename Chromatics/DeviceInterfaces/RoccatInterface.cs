using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.DeviceInterfaces
{
    class RoccatInterface
    {
        public static RoccatLib InitializeRoccatSDK()
        {
            RoccatLib roccat = null;
            roccat = new RoccatLib();

            var roccatstat = roccat.InitializeSDK();

            if (!roccatstat)
                return null;

            return roccat;
        }
    }

    public class RoccatSdkWrapper
    {
        //
    }

    public interface IRoccatSdk
    {
        bool InitializeSDK();
    }

    public class RoccatLib : IRoccatSdk
    {

        public bool InitializeSDK()
        {
            return true;
        }
    }
}
