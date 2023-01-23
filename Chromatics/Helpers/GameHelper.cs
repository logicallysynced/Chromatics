using Sharlayan.Core.Enums;
using Sharlayan.Models.ReadResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    public class GameHelper
    {
        public static bool IsCrafter(CurrentPlayerResult player)
        {
            switch (player.Entity.Job)
            {
                case Actor.Job.CPT:
                case Actor.Job.BSM:
                case Actor.Job.ARM:
                case Actor.Job.GSM:
                case Actor.Job.LTW:
                case Actor.Job.WVR:
                case Actor.Job.ALC:
                case Actor.Job.CUL:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsGatherer(CurrentPlayerResult player)
        {
            switch (player.Entity.Job)
            {
                case Actor.Job.FSH:
                case Actor.Job.MIN:
                case Actor.Job.BTN:
                    return true;
                default:
                    return false;
            }
        }
    }
}
