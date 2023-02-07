using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Enums
{
    public enum KeyboardLocalization
    {
        [Description("QWERTY")]
        qwerty = 1,
        [Description("QWERTZ")]
        qwertz = 2,
        [Description("AZERTY")]
        azerty = 3,
    }
}
