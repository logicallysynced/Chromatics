using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics
{
    public enum ConsoleTypes
    {
        SYSTEM,
        FFXIV,
        RAZER,
        CORSAIR,
        LOGITECH,
        LIFX,
        HUE,
        ARX,
        STEEL,
        COOLERMASTER,
        ROCCAT,
        ERROR
    };
    
    public enum DeviceModeTypes
    {
        DISABLED,
        STANDBY,
        DEFAULT_COLOR,
        HIGHLIGHT_COLOR,
        ENMITY_TRACKER,
        TARGET_HP,
        STATUS_EFFECTS,
        HP_TRACKER,
        MP_TRACKER,
        TP_TRACKER,
        CASTBAR,
        CHROMATICS_DEFAULT,
        DUTY_FINDER,
        UNKNOWN
    }
}
