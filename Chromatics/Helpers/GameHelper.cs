using Chromatics.Core;
using Chromatics.Models;
using FFXIVWeather.Models;
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
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobCULBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobCULHighlight.Color);
                case Actor.Job.GLD:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPLDBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPLDHighlight.Color);
                case Actor.Job.PGL:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMNKBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMNKHighlight.Color);
                case Actor.Job.MRD:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWARBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWARHighlight.Color);
                case Actor.Job.LNC:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRGBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRGHighlight.Color);
                case Actor.Job.ARC:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBRDBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBRDHighlight.Color);
                case Actor.Job.CNJ:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMHighlight.Color);
                case Actor.Job.THM:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMHighlight.Color);
                case Actor.Job.CPT:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobCPTBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobCPTHighlight.Color);
                case Actor.Job.BSM:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMHighlight.Color);
                case Actor.Job.ARM:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobARMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobARMHighlight.Color);
                case Actor.Job.GSM:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobGSMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobGSMHighlight.Color);
                case Actor.Job.LTW:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobLTWBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobLTWHighlight.Color);
                case Actor.Job.WVR:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWVRBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWVRHighlight.Color);
                case Actor.Job.ALC:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobALCBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobALCHighlight.Color);
                case Actor.Job.MIN:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMINBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMINHighlight.Color);
                case Actor.Job.BTN:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBTNBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBTNHighlight.Color);
                case Actor.Job.FSH:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobFSHBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobFSHHighlight.Color);
                case Actor.Job.PLD:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPLDBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPLDHighlight.Color);
                case Actor.Job.MNK:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMNKBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMNKHighlight.Color);
                case Actor.Job.WAR:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWARBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWARHighlight.Color);
                case Actor.Job.DRG:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRGBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRGHighlight.Color);
                case Actor.Job.BRD:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBRDBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBRDHighlight.Color);
                case Actor.Job.WHM:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMHighlight.Color);
                case Actor.Job.BLM:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLMHighlight.Color);
                case Actor.Job.ACN:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobWHMHighlight.Color);
                case Actor.Job.SMN:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSMNBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSMNHighlight.Color);
                case Actor.Job.SCH:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSCHBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSCHHighlight.Color);
                case Actor.Job.ROG:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobNINBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobNINHighlight.Color);
                case Actor.Job.NIN:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobNINBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobNINHighlight.Color);
                case Actor.Job.MCH:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMCHBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobMCHHighlight.Color);
                case Actor.Job.DRK:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRKBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDRKHighlight.Color);
                case Actor.Job.AST:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobASTBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobASTHighlight.Color);
                case Actor.Job.SAM:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSAMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSAMHighlight.Color);
                case Actor.Job.RDM:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobRDMBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobRDMHighlight.Color);
                case Actor.Job.BLU:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLUBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobBLUHighlight.Color);
                case Actor.Job.GNB:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobGNBBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobGNBHighlight.Color);
                case Actor.Job.DNC:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDNCBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobDNCHighlight.Color);
                case Actor.Job.RPR:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobRPRBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobRPRHighlight.Color);
                case Actor.Job.SGE:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSGEBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobSGEHighlight.Color);
                case Actor.Job.PCT:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPCTBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobPCTHighlight.Color);
                case Actor.Job.VPR:
                    if (!highlight)
                        return ColorHelper.ColorToRGBColor(colorPalette.JobVPRBase.Color);
                    else
                        return ColorHelper.ColorToRGBColor(colorPalette.JobVPRHighlight.Color);
                default:
                    return ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
            }
        }

        public static string GetJobClassDynamicLayerDescriptions(Actor.Job jobClass, string layer)
        {
            if (layer == "A")
            {
                switch (jobClass)
                {
                    case Actor.Job.CPT:
                    case Actor.Job.BSM:
                    case Actor.Job.ARM:
                    case Actor.Job.GSM:
                    case Actor.Job.LTW:
                    case Actor.Job.WVR:
                    case Actor.Job.ALC:
                    case Actor.Job.CUL:
                        return @"Crafters' currently not implemented.";
                    case Actor.Job.PLD:
                        return @"Shows Paladin's Oath Gauge.";
                    case Actor.Job.MNK:
                        return @"Shows Monk's Chakra stacks.";
                    case Actor.Job.WAR:
                        return @"Shows Warrior's Beast Gauge.";
                    case Actor.Job.DRG:
                        return @"Shows Dragoon's Blood Dragon Gauge.";
                    case Actor.Job.BRD:
                        return @"Shows currently playing Bard songs.";
                    case Actor.Job.WHM:
                        return @"Shows White Mage's flower charge time.";
                    case Actor.Job.BLM:
                        return @"Shows Black Mage's Astral & Umbral stacks";
                    case Actor.Job.SMN:
                        return @"Shows Summoner's Attunement timer for each Egi.";
                    case Actor.Job.SCH:
                        return @"Shows Scholar's Faerie Gauge.";
                    case Actor.Job.NIN:
                        return @"Shows Ninja's Huton Timer.";
                    case Actor.Job.MCH:
                        return @"Shows Machinst's Heat and Overheat Gauge.";
                    case Actor.Job.DRK:
                        return @"Shows Dark Knight's Grit and Blood Gauge.";
                    case Actor.Job.AST:
                        return @"Shows Astrologian's drawn card.";
                    case Actor.Job.SAM:
                        return @"Shows Samurai's Kenki Guage.";
                    case Actor.Job.RDM:
                        return @"Shows Red Mage's White Mana Gauge.";
                    case Actor.Job.BLU:
                        return @"Blue Mage not implemented.";
                    case Actor.Job.GNB:
                        return @"Shows Gunbreaker's Royal Gaurd.";
                    case Actor.Job.DNC:
                        return @"Shows Dancer's Fourfold Feathers.";
                    case Actor.Job.RPR:
                        return @"Shows Reaper's Soul Gauge.";
                    case Actor.Job.SGE:
                        return @"Shows Sage Addersgall Recharge Gauge.";
                    case Actor.Job.VPR:
                        return @"Shows Viper's Vipersight Gauge.";
                    case Actor.Job.PCT:
                        return @"Shows Pictomancer's Palette Gauge.";
                }
            }
            else if (layer == "B")
            {
                switch (jobClass)
                {
                    case Actor.Job.CPT:
                    case Actor.Job.BSM:
                    case Actor.Job.ARM:
                    case Actor.Job.GSM:
                    case Actor.Job.LTW:
                    case Actor.Job.WVR:
                    case Actor.Job.ALC:
                    case Actor.Job.CUL:
                        return @"Crafters' currently not implemented.";
                    case Actor.Job.PLD:
                        return @"Paladin second gauge not implemented.";
                    case Actor.Job.MNK:
                        return @"Monk second gauge not implemented.";
                    case Actor.Job.WAR:
                        return @"Shows Warrior's Defiance status.";
                    case Actor.Job.DRG:
                        return @"Shows Dragoon's Dragon Gaze stacks.";
                    case Actor.Job.BRD:
                        return @"Shows Bard's Soul Voice Gauge.";
                    case Actor.Job.WHM:
                        return @"Shows White Mage's Flower count.";
                    case Actor.Job.BLM:
                        return @"Shows Black Mage's Enochian timer.";
                    case Actor.Job.SMN:
                        return @"Shows Summoner's Summon timer.";
                    case Actor.Job.SCH:
                        return @"Shows Scholar's Aetherflow stacks.";
                    case Actor.Job.NIN:
                        return @"Shows Ninja's Ninki Gauge.";
                    case Actor.Job.MCH:
                        return @"Shows Machinst's Battery Gauge.";
                    case Actor.Job.DRK:
                        return @"Shows Dark Knight's Darkside timer.";
                    case Actor.Job.AST:
                        return @"Astrologian second gauge not implemented.";
                    case Actor.Job.SAM:
                        return @"Shows Samurai's Meditation Guage.";
                    case Actor.Job.RDM:
                        return @"Shows Red Mage's Black Mana Gauge.";
                    case Actor.Job.BLU:
                        return @"Blue Mage not implemented.";
                    case Actor.Job.GNB:
                        return @"Shows Gunbreaker's Charge count.";
                    case Actor.Job.DNC:
                        return @"Shows Dancer's Espirit Gauge.";
                    case Actor.Job.RPR:
                        return @"Shows Reaper's Shroud Gauge.";
                    case Actor.Job.SGE:
                        return @"Shows Sage's Addersting Gauge.";
                    case Actor.Job.VPR:
                        return @"Shows Viper's Serpent Offerings Gauge.";
                    case Actor.Job.PCT:
                        return @"Shows Pictomancer's Canvas.";
                }
            }

            return @"";
        }

        public static string GetZoneNameById(uint id, string language = "en")
        {
            var data = FileOperationsHelper.GetWeatherDataLoaded();

            if (data == null) return null;

            var terriTypes = data.TerriTypes;
            var terriType = terriTypes.FirstOrDefault(tt => tt.Id == id);
            if (terriType == null)
            {
                return null; // or throw an exception, or return a default value
            }

            return language.ToLower() switch
            {
                "en" => terriType.NameEn,
                "de" => terriType.NameDe,
                "fr" => terriType.NameFr,
                "ja" => terriType.NameJa,
                "zh" => terriType.NameZh,
                _ => null // or throw an exception, or return a default value
            };
        }
    }
}
