using Chromatics.Core;
using Chromatics.Models;
using RGB.NET.Core;
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

        public static Color GetJobClassColor(Actor.Job job, PaletteColorModel colorPalette, bool highlight = false)
        {
            switch (job)
            {
                case Actor.Job.CUL:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobCULBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobCULHighlight.Color);
                case Actor.Job.GLD:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPLDBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPLDHighlight.Color);
                case Actor.Job.PGL:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMNKBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMNKHighlight.Color);
                case Actor.Job.MRD:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWARBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWARHighlight.Color);
                case Actor.Job.LNC:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRGBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRGHighlight.Color);
                case Actor.Job.ARC:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBRDBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBRDHighlight.Color);
                case Actor.Job.CNJ:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMHighlight.Color);
                case Actor.Job.THM:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMHighlight.Color);
                case Actor.Job.CPT:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobCPTBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobCPTHighlight.Color);
                case Actor.Job.BSM:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMHighlight.Color);
                case Actor.Job.ARM:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobARMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobARMHighlight.Color);
                case Actor.Job.GSM:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobGSMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobGSMHighlight.Color);
                case Actor.Job.LTW:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobLTWBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobLTWHighlight.Color);
                case Actor.Job.WVR:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWVRBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWVRHighlight.Color);
                case Actor.Job.ALC:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobALCBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobALCHighlight.Color);
                case Actor.Job.MIN:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMINBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMINHighlight.Color);
                case Actor.Job.BTN:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBTNBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBTNHighlight.Color);
                case Actor.Job.FSH:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobFSHBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobFSHHighlight.Color);
                case Actor.Job.PLD:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPLDBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPLDHighlight.Color);
                case Actor.Job.MNK:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMNKBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMNKHighlight.Color);
                case Actor.Job.WAR:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWARBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWARHighlight.Color);
                case Actor.Job.DRG:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRGBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRGHighlight.Color);
                case Actor.Job.BRD:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBRDBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBRDHighlight.Color);
                case Actor.Job.WHM:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMHighlight.Color);
                case Actor.Job.BLM:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMHighlight.Color);
                case Actor.Job.ACN:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMHighlight.Color);
                case Actor.Job.SMN:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSMNBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSMNHighlight.Color);
                case Actor.Job.SCH:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSCHBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSCHHighlight.Color);
                case Actor.Job.ROG:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobNINBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobNINHighlight.Color);
                case Actor.Job.NIN:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobNINBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobNINHighlight.Color);
                case Actor.Job.MCH:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMCHBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMCHHighlight.Color);
                case Actor.Job.DRK:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRKBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRKHighlight.Color);
                case Actor.Job.AST:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobASTBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobASTHighlight.Color);
                case Actor.Job.SAM:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSAMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSAMHighlight.Color);
                case Actor.Job.RDM:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobRDMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobRDMHighlight.Color);
                case Actor.Job.BLU:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLUBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLUHighlight.Color);
                case Actor.Job.GNB:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobGNBBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobGNBHighlight.Color);
                case Actor.Job.DNC:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDNCBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDNCHighlight.Color);
                case Actor.Job.RPR:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobRPRBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobRPRHighlight.Color);
                case Actor.Job.SGE:
                    if (highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSGEBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSGEHighlight.Color);
                default:
                    return ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
            }
        }
    }
}
