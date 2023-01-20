using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Enums
{
    public enum LoggerTypes
    {
        [DefaultValue(typeof(Color), "Black")]
        Default = 0,
        [DefaultValue(typeof(Color), "Black")]
        System = 1,
        [DefaultValue(typeof(Color), "OrangeRed")]
        Devices = 2,
        [DefaultValue(typeof(Color), "DarkCyan")]
        FFXIV = 3,
        [DefaultValue(typeof(Color), "Red")]
        Error = 4
    }
}
