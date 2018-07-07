using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromatics.LCDInterfaces;
using Sharlayan.Core;

namespace Chromatics.FFXIVInterfaces
{
    public class FFXIVUnsafeMethods
    {
        public void CallLcdData(ILogitechLcd sender, ActorItem _pI, ActorItem _tI)
        {
            sender.DrawLCDInfo(_pI, _tI);
        }
    }
}
