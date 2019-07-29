using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;
using Sharlayan;
using Sharlayan.Core;
using Sharlayan.Core.Enums;
using Sharlayan.Models;
using Chromatics.Controllers;

namespace Chromatics
{
    partial class Chromatics
    {
        public void ImplementJobGauges(List<StatusItem> statEffects, Color baseColor, Sharlayan.Models.ReadResults.ActionResult hotbars)
        { 
            if (ChromaticsSettings.ChromaticsSettingsJobGaugeToggle)
            {


                switch (_playerInfo.Job)
                {
                    case Actor.Job.WAR:
                        var burstwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARBeastGauge);
                        var maxwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARBeastGaugeMax);
                        var negwarcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARNegative);
                        var defiancecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARDefiance);
                        var nondefiancecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWARNonDefiance);
                        var wrath = Cooldowns.Wrath;
                        var polWrath = (wrath - 0) * (50 - 0) / (100 - 0) + 0;
                        
                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(wrath, 0, 100,
                                    JobLightbar_Collection.Length, 0);

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? burstwarcol : negwarcol, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(wrath, 0, 100,
                                    JobFunction_Collection.Length, 0);

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? burstwarcol : negwarcol, false);
                            }
                        }
                        

                        if (statEffects.Find(i => i.StatusName == "Defiance") != null)
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", defiancecol, false);
                            GlobalApplyMapKeyLighting("NumAdd", defiancecol, false);
                            GlobalApplyMapKeyLighting("NumEnter", defiancecol, false);
                            GlobalApplyMapKeyLighting("Num0", defiancecol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", defiancecol, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", nondefiancecol, false);
                            GlobalApplyMapKeyLighting("NumAdd", nondefiancecol, false);
                            GlobalApplyMapKeyLighting("NumEnter", nondefiancecol, false);
                            GlobalApplyMapKeyLighting("Num0", nondefiancecol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", nondefiancecol, false);
                        }

                        if (wrath > 0)
                        {
                            if (polWrath >= 50)
                            {
                                //Flash
                                ToggleGlobalFlash3(true);
                                GlobalFlash3(maxwarcol, 150);
                            }
                            else if (polWrath < 50 && polWrath > 40)
                            {
                                GlobalApplyMapKeyLighting("NumLock", burstwarcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", burstwarcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", burstwarcol, false);

                                GlobalApplyMapKeyLighting("Num7", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num8", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num9", burstwarcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstwarcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstwarcol, false);

                                ToggleGlobalFlash3(false);
                            }
                            else if (polWrath <= 40 && polWrath > 30)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num7", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num8", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num9", burstwarcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstwarcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstwarcol, false);

                                ToggleGlobalFlash3(false);
                            }
                            else if (polWrath <= 30 && polWrath > 20)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstwarcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstwarcol, false);

                                ToggleGlobalFlash3(false);
                            }
                            else if (polWrath <= 20 && polWrath > 10)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num4", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num5", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num6", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstwarcol, false);

                                ToggleGlobalFlash3(false);
                            }
                            else if (polWrath <= 10 && polWrath > 0)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num4", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num5", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num6", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstwarcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstwarcol, false);

                                ToggleGlobalFlash3(false);
                            }
                            else if (polWrath == 0)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num4", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num5", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num6", negwarcol, false);

                                GlobalApplyMapKeyLighting("Num1", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num2", negwarcol, false);
                                GlobalApplyMapKeyLighting("Num3", negwarcol, false);

                                ToggleGlobalFlash3(false);
                            }
                        }
                        else
                        {
                            ToggleGlobalFlash3(false);

                            GlobalApplyMapKeyLighting("NumLock", negwarcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negwarcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negwarcol, false);

                            GlobalApplyMapKeyLighting("Num7", negwarcol, false);
                            GlobalApplyMapKeyLighting("Num8", negwarcol, false);
                            GlobalApplyMapKeyLighting("Num9", negwarcol, false);

                            GlobalApplyMapKeyLighting("Num4", negwarcol, false);
                            GlobalApplyMapKeyLighting("Num5", negwarcol, false);
                            GlobalApplyMapKeyLighting("Num6", negwarcol, false);

                            GlobalApplyMapKeyLighting("Num1", negwarcol, false);
                            GlobalApplyMapKeyLighting("Num2", negwarcol, false);
                            GlobalApplyMapKeyLighting("Num3", negwarcol, false);
                        }

                        break;
                    case Actor.Job.PLD:
                        //Paladin
                        
                        var negpldcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDNegative);
                        var oathcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDOathGauge);
                        var swordcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDSwordOath);
                        var ironwillcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobPLDIronWill);

                        var oathgauge = Cooldowns.OathGauge;
                        
                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(oathgauge, 0, 100,
                                    JobLightbar_Collection.Length, 0);

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? oathcol : negpldcol, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(oathgauge, 0, 100,
                                    JobFunction_Collection.Length, 0);

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? oathcol : negpldcol, false);
                            }
                        }
                        
                        if (statEffects.ToList().Find(i => i.StatusName == "Iron Will") != null)
                        {
                            GlobalApplyMapKeyLighting("NumLock", ironwillcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", ironwillcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", ironwillcol, false);
                            GlobalApplyMapKeyLighting("Num0", ironwillcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", ironwillcol, false);
                            GlobalApplyMapKeyLighting("NumSubtract", ironwillcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", ironwillcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", ironwillcol, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumLock", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negpldcol, false);
                            GlobalApplyMapKeyLighting("Num0", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumSubtract", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", negpldcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", negpldcol, false);
                        }

                        if (statEffects.Find(i => i.StatusName == "Sword Oath") != null)
                        {
                            var oathstacks = (int)statEffects.Find(x => x.StatusName == "Sword Oath").Stacks;
                            switch (oathstacks)
                            {
                                case 3:
                                    GlobalApplyMapKeyLighting("Num7", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num8", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num9", swordcol, false);

                                    GlobalApplyMapKeyLighting("Num4", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num5", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num6", swordcol, false);

                                    GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num3", swordcol, false);
                                    break;
                                case 2:
                                    GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                    GlobalApplyMapKeyLighting("Num4", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num5", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num6", swordcol, false);

                                    GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num3", swordcol, false);
                                    break;
                                case 1:
                                    GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                    GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                    GlobalApplyMapKeyLighting("Num1", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num2", swordcol, false);
                                    GlobalApplyMapKeyLighting("Num3", swordcol, false);
                                    break;
                                default:
                                    GlobalApplyMapKeyLighting("Num7", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num8", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num9", negpldcol, false);

                                    GlobalApplyMapKeyLighting("Num4", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num5", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num6", negpldcol, false);

                                    GlobalApplyMapKeyLighting("Num1", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num2", negpldcol, false);
                                    GlobalApplyMapKeyLighting("Num3", negpldcol, false);
                                    break;
                            }

                            
                        }

                        break;
                    case Actor.Job.MNK:
                        var greased = Cooldowns.GreasedLightningStacks;
                        var greaseRemaining = Cooldowns.GreasedLightningTimeRemaining;
                        var burstmnkcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMNKGreased);
                        var burstmnkempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMNKNegative);


                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate = ((int)greaseRemaining - 0) * (JobLightbar_Collection.Length - 0) / (16 - 0) + 0;
                            

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? burstmnkcol : burstmnkempty, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate = ((int)greaseRemaining - 0) * (JobFunction_Collection.Length - 0) / (16 - 0) + 0;

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? burstmnkcol : burstmnkempty, false);
                            }
                        }

                        if (greased > 0)
                        {
                            if (greaseRemaining > 0 && greaseRemaining <= 5)
                            {
                                ToggleGlobalFlash3(true);
                                GlobalFlash3(burstmnkcol, 150);
                            }
                            else
                            {
                                ToggleGlobalFlash3(false);

                                switch (greased)
                                {
                                    case 3:
                                        GlobalApplyMapKeyLighting("Num9", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num6", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num3", burstmnkcol, false);

                                        GlobalApplyMapKeyLighting("Num8", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num5", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num2", burstmnkcol, false);

                                        GlobalApplyMapKeyLighting("Num7", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num4", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num1", burstmnkcol, false);
                                        break;
                                    case 2:
                                        GlobalApplyMapKeyLighting("Num9", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num6", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num3", burstmnkempty, false);

                                        GlobalApplyMapKeyLighting("Num8", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num5", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num2", burstmnkcol, false);

                                        GlobalApplyMapKeyLighting("Num7", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num4", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num1", burstmnkcol, false);
                                        break;
                                    case 1:
                                        GlobalApplyMapKeyLighting("Num9", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num6", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num3", burstmnkempty, false);

                                        GlobalApplyMapKeyLighting("Num8", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num5", burstmnkempty, false);
                                        GlobalApplyMapKeyLighting("Num2", burstmnkempty, false);

                                        GlobalApplyMapKeyLighting("Num7", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num4", burstmnkcol, false);
                                        GlobalApplyMapKeyLighting("Num1", burstmnkcol, false);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            ToggleGlobalFlash3(false);

                            GlobalApplyMapKeyLighting("Num9", burstmnkempty, false);
                            GlobalApplyMapKeyLighting("Num6", burstmnkempty, false);
                            GlobalApplyMapKeyLighting("Num3", burstmnkempty, false);

                            GlobalApplyMapKeyLighting("Num8", burstmnkempty, false);
                            GlobalApplyMapKeyLighting("Num5", burstmnkempty, false);
                            GlobalApplyMapKeyLighting("Num2", burstmnkempty, false);

                            GlobalApplyMapKeyLighting("Num7", burstmnkempty, false);
                            GlobalApplyMapKeyLighting("Num4", burstmnkempty, false);
                            GlobalApplyMapKeyLighting("Num1", burstmnkempty, false);
                        }

                        break;
                    case Actor.Job.DRG:

                        var burstdrgcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGBloodDragon);
                        var burstdrgeyecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGBloodDragon);
                        var negdrgcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGNegative);
                        var bloodremain = Cooldowns.BloodOfTheDragonTimeRemaining;
                        var polBlood = (bloodremain - 0) * (50 - 0) / (30 - 0) + 0;

                        if (Cooldowns.LifeOfTheDragon)
                        {
                            burstdrgeyecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGLifeOfTheDragon);
                            GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGLifeOfTheDragon), false);
                            GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGLifeOfTheDragon), false);
                            GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGLifeOfTheDragon), false);
                            GlobalApplyMapKeyLighting("NumDecimal", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGLifeOfTheDragon), false);
                            GlobalApplyMapKeyLighting("Num0", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGLifeOfTheDragon), false);
                        }
                        else
                        {
                            switch (Cooldowns.DragonGauge)
                            {
                                case 2:
                                    burstdrgeyecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge2);
                                    GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge2), false);
                                    GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge2), false);
                                    GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge2), false);
                                    GlobalApplyMapKeyLighting("NumDecimal", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge2), false);
                                    GlobalApplyMapKeyLighting("Num0", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge2), false);
                                    break;
                                case 1:
                                    burstdrgeyecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge1);
                                    GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge1), false);
                                    GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge1), false);
                                    GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge1), false);
                                    GlobalApplyMapKeyLighting("NumDecimal", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge1), false);
                                    GlobalApplyMapKeyLighting("Num0", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGDragonGauge1), false);
                                    break;
                                default:
                                    burstdrgeyecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRGBloodDragon);
                                    GlobalApplyMapKeyLighting("NumSubtract", negdrgcol, false);
                                    GlobalApplyMapKeyLighting("NumAdd", negdrgcol, false);
                                    GlobalApplyMapKeyLighting("NumEnter", negdrgcol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negdrgcol, false);
                                    GlobalApplyMapKeyLighting("Num0", negdrgcol, false);
                                    break;
                            }
                        }
                        
                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate = (bloodremain - 0) * (JobLightbar_Collection.Length - 0) / (30 - 0) + 0;

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? burstdrgeyecol : negdrgcol, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate = (bloodremain - 0) * (JobFunction_Collection.Length - 0) / (30 - 0) + 0;

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? burstdrgeyecol : negdrgcol, false);
                            }
                        }
                        

                        if (bloodremain > 0)
                        {
                            if (polBlood > 50)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num7", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num8", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num9", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                            }
                            else if (polBlood <= 50 && polBlood > 40)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num7", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num8", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num9", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                            }
                            else if (polBlood <= 40 && polBlood > 30)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", negdrgcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrgcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrgcol, false);

                                GlobalApplyMapKeyLighting("Num7", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num8", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num9", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                            }
                            else if (polBlood <= 30 && polBlood > 20)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", negdrgcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrgcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrgcol, false);

                                GlobalApplyMapKeyLighting("Num7", negdrgcol, false);
                                GlobalApplyMapKeyLighting("Num8", negdrgcol, false);
                                GlobalApplyMapKeyLighting("Num9", negdrgcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstdrgcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                            }
                            else if (polBlood <= 20 && polBlood > 10)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", negdrgcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrgcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrgcol, false);

                                GlobalApplyMapKeyLighting("Num7", negdrgcol, false);
                                GlobalApplyMapKeyLighting("Num8", negdrgcol, false);
                                GlobalApplyMapKeyLighting("Num9", negdrgcol, false);

                                GlobalApplyMapKeyLighting("Num4", negdrgcol, false);
                                GlobalApplyMapKeyLighting("Num5", negdrgcol, false);
                                GlobalApplyMapKeyLighting("Num6", negdrgcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstdrgcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstdrgcol, false);
                            }
                            else if (polBlood <= 10 && polBlood > 0)
                            {
                                //Flash
                                ToggleGlobalFlash3(true);
                                GlobalFlash3(burstdrgcol, 150);
                            }
                        }
                        else
                        {
                            ToggleGlobalFlash3(false);

                            GlobalApplyMapKeyLighting("NumLock", negdrgcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negdrgcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negdrgcol, false);

                            GlobalApplyMapKeyLighting("Num7", negdrgcol, false);
                            GlobalApplyMapKeyLighting("Num8", negdrgcol, false);
                            GlobalApplyMapKeyLighting("Num9", negdrgcol, false);

                            GlobalApplyMapKeyLighting("Num4", negdrgcol, false);
                            GlobalApplyMapKeyLighting("Num5", negdrgcol, false);
                            GlobalApplyMapKeyLighting("Num6", negdrgcol, false);

                            GlobalApplyMapKeyLighting("Num1", negdrgcol, false);
                            GlobalApplyMapKeyLighting("Num2", negdrgcol, false);
                            GlobalApplyMapKeyLighting("Num3", negdrgcol, false);
                        }
                        break;
                    case Actor.Job.BRD:
                        //Bard Songs
                        var burstcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);
                        var negcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);
                        var repcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDRepertoire);

                        GlobalApplyMapKeyLighting("Num0", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                        GlobalApplyMapKeyLighting("NumDecimal", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);

                        //Console.WriteLine(@"Song: " + Cooldowns.Song.ToString());
                        
                        if (Cooldowns.Song != Cooldowns.BardSongs.None)
                        {
                            var songremain = Cooldowns.SongTimeRemaining;

                            switch (Cooldowns.Song)
                            {
                                case Cooldowns.BardSongs.ArmysPaeon:
                                    burstcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDArmys);
                                    negcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);

                                    GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    break;
                                case Cooldowns.BardSongs.MagesBallad:
                                    burstcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDBallad);
                                    negcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);

                                    GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    break;
                                case Cooldowns.BardSongs.WanderersMinuet:
                                    burstcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDMinuet);
                                    negcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative);

                                    //Console.WriteLine(Cooldowns.RepertoireStacks);

                                    switch (Cooldowns.RepertoireStacks)
                                    {
                                        case 1:
                                            GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                            GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                            GlobalApplyMapKeyLighting("NumEnter", repcol, false);
                                            break;
                                        case 2:
                                            GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                            GlobalApplyMapKeyLighting("NumAdd", repcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", repcol, false);
                                            break;
                                        case 3:
                                            GlobalApplyMapKeyLighting("NumSubtract", repcol, false);
                                            GlobalApplyMapKeyLighting("NumAdd", repcol, false);
                                            GlobalApplyMapKeyLighting("NumEnter", repcol, false);
                                            break;
                                        default:
                                            GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                            GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                            GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                            break;
                                    }

                                    break;
                                default:
                                    GlobalApplyMapKeyLighting("NumSubtract", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    GlobalApplyMapKeyLighting("NumAdd", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    GlobalApplyMapKeyLighting("NumEnter", ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBRDNegative), false);
                                    break;
                            }

                            //Lightbar
                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                var JobLightbar_Collection = DeviceEffects.LightbarZones;
                                var JobLightbar_Interpolate = ((int)songremain - 0) * (JobLightbar_Collection.Length - 0) / (30 - 0) + 0;

                                for (int i = 0; i < JobLightbar_Collection.Length; i++)
                                {
                                    GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                        JobLightbar_Interpolate > i ? burstcol : negcol, false, false);
                                }
                            }

                            //FKeys
                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                var JobFunction_Collection = DeviceEffects.Functions;
                                var JobFunction_Interpolate = ((int)songremain - 0) * (JobFunction_Collection.Length - 0) / (30 - 0) + 0;

                                for (int i = 0; i < JobFunction_Collection.Length; i++)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        JobFunction_Interpolate > i ? burstcol : negcol, false);
                                }
                            }

                            var songInt = ((int)songremain - 0) * (100 - 0) / (30 - 0) + 0;
                            if (songInt <= 100 && songInt > 80)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", burstcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", burstcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", burstcol, false);

                                GlobalApplyMapKeyLighting("Num7", burstcol, false);
                                GlobalApplyMapKeyLighting("Num8", burstcol, false);
                                GlobalApplyMapKeyLighting("Num9", burstcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstcol, false);
                            }
                            else if (songInt <= 80 && songInt > 60)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", negcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negcol, false);

                                GlobalApplyMapKeyLighting("Num7", burstcol, false);
                                GlobalApplyMapKeyLighting("Num8", burstcol, false);
                                GlobalApplyMapKeyLighting("Num9", burstcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstcol, false);
                            }
                            else if (songInt <= 60 && songInt > 40)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", negcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negcol, false);

                                GlobalApplyMapKeyLighting("Num7", negcol, false);
                                GlobalApplyMapKeyLighting("Num8", negcol, false);
                                GlobalApplyMapKeyLighting("Num9", negcol, false);

                                GlobalApplyMapKeyLighting("Num4", burstcol, false);
                                GlobalApplyMapKeyLighting("Num5", burstcol, false);
                                GlobalApplyMapKeyLighting("Num6", burstcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstcol, false);
                            }
                            else if (songInt <= 40 && songInt > 20)
                            {
                                ToggleGlobalFlash3(false);

                                GlobalApplyMapKeyLighting("NumLock", negcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negcol, false);

                                GlobalApplyMapKeyLighting("Num7", negcol, false);
                                GlobalApplyMapKeyLighting("Num8", negcol, false);
                                GlobalApplyMapKeyLighting("Num9", negcol, false);

                                GlobalApplyMapKeyLighting("Num4", negcol, false);
                                GlobalApplyMapKeyLighting("Num5", negcol, false);
                                GlobalApplyMapKeyLighting("Num6", negcol, false);

                                GlobalApplyMapKeyLighting("Num1", burstcol, false);
                                GlobalApplyMapKeyLighting("Num2", burstcol, false);
                                GlobalApplyMapKeyLighting("Num3", burstcol, false);
                            }
                            else if (songInt <= 20 && songInt > 0)
                            {
                                //Flash
                                ToggleGlobalFlash3(true);
                                GlobalFlash3(burstcol, 150);
                            }
                        }
                        else
                        {
                            ToggleGlobalFlash3(false);

                            GlobalApplyMapKeyLighting("NumLock", negcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negcol, false);

                            GlobalApplyMapKeyLighting("Num7", negcol, false);
                            GlobalApplyMapKeyLighting("Num8", negcol, false);
                            GlobalApplyMapKeyLighting("Num9", negcol, false);

                            GlobalApplyMapKeyLighting("Num4", negcol, false);
                            GlobalApplyMapKeyLighting("Num5", negcol, false);
                            GlobalApplyMapKeyLighting("Num6", negcol, false);

                            GlobalApplyMapKeyLighting("Num1", negcol, false);
                            GlobalApplyMapKeyLighting("Num2", negcol, false);
                            GlobalApplyMapKeyLighting("Num3", negcol, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                foreach (var f in DeviceEffects.LightbarZones)
                                {
                                    GlobalApplyMapLightbarLighting(f, negcol, false);
                                }
                            }

                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                foreach (var f in DeviceEffects.Functions)
                                {
                                    GlobalApplyMapKeyLighting(f, negcol, false);
                                }
                            }
                        }
                        break;
                    case Actor.Job.WHM:
                        //White Mage
                        var negwhmcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMNegative);
                        var flowercol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMFlowerPetal);
                        var cureproc = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMFreecure);
                        var bloodlilycol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMBloodLily);

                        var petalcount = Cooldowns.FlowerPetals;
                        var flowercharge = Cooldowns.FlowerCharge;
                        //Console.WriteLine(Cooldowns.FlowerCharge * 100);


                        if (statEffects.Find(i => i.StatusName == "Freecure") != null)
                        {
                            GlobalApplyMapKeyLighting("NumLock", cureproc, false);
                            GlobalApplyMapKeyLighting("NumDivide", cureproc, false);
                            GlobalApplyMapKeyLighting("NumMultiply", cureproc, false);
                            GlobalApplyMapKeyLighting("NumSubtract", cureproc, false);
                            GlobalApplyMapKeyLighting("NumAdd", cureproc, false);
                            GlobalApplyMapKeyLighting("NumEnter", cureproc, false);
                            GlobalApplyMapKeyLighting("Num0", cureproc, false);
                            GlobalApplyMapKeyLighting("NumDecimal", cureproc, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumLock", negwhmcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negwhmcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negwhmcol, false);
                            GlobalApplyMapKeyLighting("NumSubtract", negwhmcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", negwhmcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", negwhmcol, false);
                            GlobalApplyMapKeyLighting("Num0", negwhmcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", negwhmcol, false);
                        }

                        //Lightbar
                        var flowerchargecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobWHMFlowerCharge);
                        if (Cooldowns.BloodFlowerReady)
                            flowerchargecol = bloodlilycol;

                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int((int)flowercharge, 0, 116,
                                    JobLightbar_Collection.Length, 0);

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? flowerchargecol : negwhmcol, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int((int)flowercharge, 0, 116,
                                    JobFunction_Collection.Length, 0);

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? flowerchargecol : negwhmcol, false);
                            }
                        }

                        switch (petalcount)
                        {
                            case 3:
                                GlobalApplyMapKeyLighting("Num9", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num6", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num3", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);

                                GlobalApplyMapKeyLighting("Num8", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num5", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num2", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);

                                GlobalApplyMapKeyLighting("Num7", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num4", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num1", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                break;
                            case 2:
                                GlobalApplyMapKeyLighting("Num9", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num6", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num3", negwhmcol, false);

                                GlobalApplyMapKeyLighting("Num8", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num5", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num2", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);

                                GlobalApplyMapKeyLighting("Num7", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num4", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num1", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                break;
                            case 1:
                                GlobalApplyMapKeyLighting("Num9", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num6", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num3", negwhmcol, false);

                                GlobalApplyMapKeyLighting("Num8", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num5", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num2", negwhmcol, false);

                                GlobalApplyMapKeyLighting("Num7", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num4", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                GlobalApplyMapKeyLighting("Num1", Cooldowns.BloodFlowerReady ? bloodlilycol : flowercol, false);
                                break;
                            default:
                                GlobalApplyMapKeyLighting("Num9", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num6", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num3", negwhmcol, false);

                                GlobalApplyMapKeyLighting("Num8", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num5", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num2", negwhmcol, false);

                                GlobalApplyMapKeyLighting("Num7", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num4", negwhmcol, false);
                                GlobalApplyMapKeyLighting("Num1", negwhmcol, false);
                                break;
                        }


                        break;
                    case Actor.Job.BLM:
                        //Black Mage

                        var negblmcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMNegative);
                        var firecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMAstralFire);
                        var icecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMUmbralIce);
                        var heartcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMUmbralHeart);
                        var enochcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMEnochianCountdown);
                        var enochchargecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMEnochianCharge);

                        var firestacks = Cooldowns.AstralFire;
                        var icestacks = Cooldowns.UmbralIce;
                        var heartstacks = Cooldowns.UmbralHearts;

                        if (Cooldowns.PolyglotActive)
                        {
                            enochchargecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMPolyglot);
                        }
                        else
                        {
                            enochchargecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobBLMEnochianCharge);
                        }
                        
                        switch (heartstacks)
                        {
                            case 1:
                                GlobalApplyMapKeyLighting("NumSubtract", negblmcol, false);
                                GlobalApplyMapKeyLighting("NumAdd", negblmcol, false);
                                GlobalApplyMapKeyLighting("NumEnter", heartcol, false);
                                break;
                            case 2:
                                GlobalApplyMapKeyLighting("NumSubtract", negblmcol, false);
                                GlobalApplyMapKeyLighting("NumAdd", heartcol, false);
                                GlobalApplyMapKeyLighting("NumEnter", heartcol, false);
                                break;
                            case 3:
                                GlobalApplyMapKeyLighting("NumSubtract", heartcol, false);
                                GlobalApplyMapKeyLighting("NumAdd", heartcol, false);
                                GlobalApplyMapKeyLighting("NumEnter", heartcol, false);
                                break;
                            default:
                                GlobalApplyMapKeyLighting("NumSubtract", negblmcol, false);
                                GlobalApplyMapKeyLighting("NumAdd", negblmcol, false);
                                GlobalApplyMapKeyLighting("NumEnter", negblmcol, false);
                                break;
                        }

                        if (firestacks > 0)
                        {
                            switch (firestacks)
                            {
                                case 1:
                                    GlobalApplyMapKeyLighting("NumLock", firecol, false);
                                    GlobalApplyMapKeyLighting("NumDivide", negblmcol, false);
                                    GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                    break;
                                case 2:
                                    GlobalApplyMapKeyLighting("NumLock", firecol, false);
                                    GlobalApplyMapKeyLighting("NumDivide", firecol, false);
                                    GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                    break;
                                case 3:
                                    GlobalApplyMapKeyLighting("NumLock", firecol, false);
                                    GlobalApplyMapKeyLighting("NumDivide", firecol, false);
                                    GlobalApplyMapKeyLighting("NumMultiply", firecol, false);
                                    break;
                            }
                        }
                        else if (icestacks > 0)
                        {
                            switch (icestacks)
                            {
                                case 1:
                                    GlobalApplyMapKeyLighting("NumLock", icecol, false);
                                    GlobalApplyMapKeyLighting("NumDivide", negblmcol, false);
                                    GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                    break;
                                case 2:
                                    GlobalApplyMapKeyLighting("NumLock", icecol, false);
                                    GlobalApplyMapKeyLighting("NumDivide", icecol, false);
                                    GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                                    break;
                                case 3:
                                    GlobalApplyMapKeyLighting("NumLock", icecol, false);
                                    GlobalApplyMapKeyLighting("NumDivide", icecol, false);
                                    GlobalApplyMapKeyLighting("NumMultiply", icecol, false);
                                    break;
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumLock", negblmcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negblmcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negblmcol, false);
                        }
                        
                        if (Cooldowns.EnochianActive)
                        {
                            var enochtime = (Cooldowns.EnochianTimeRemaining - 0) * (40 - 0) / (15 - 0) + 0;

                            //Lightbar
                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                var JobLightbar_Collection = DeviceEffects.LightbarZones;
                                var JobLightbar_Interpolate = ((int)Cooldowns.EnochianCharge - 0) * (JobLightbar_Collection.Length - 0) / (116 - 0) + 0;

                                

                                for (int i = 0; i < JobLightbar_Collection.Length; i++)
                                {
                                    GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                        JobLightbar_Interpolate > i ? enochchargecol : negblmcol, false, false);
                                }
                            }

                            //FKeys
                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                var JobFunction_Collection = DeviceEffects.Functions;
                                var JobFunction_Interpolate = ((int)Cooldowns.EnochianCharge - 0) * (JobFunction_Collection.Length - 0) / (116 - 0) + 0;

                                for (int i = 0; i < JobFunction_Collection.Length; i++)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        JobFunction_Interpolate > i ? enochchargecol : negblmcol, false);
                                }
                            }

                            if (enochtime <= 40 && enochtime > 30)
                            {
                                GlobalApplyMapKeyLighting("Num7", enochcol, false);
                                GlobalApplyMapKeyLighting("Num8", enochcol, false);
                                GlobalApplyMapKeyLighting("Num9", enochcol, false);
                                GlobalApplyMapKeyLighting("Num4", enochcol, false);
                                GlobalApplyMapKeyLighting("Num5", enochcol, false);
                                GlobalApplyMapKeyLighting("Num6", enochcol, false);
                                GlobalApplyMapKeyLighting("Num1", enochcol, false);
                                GlobalApplyMapKeyLighting("Num2", enochcol, false);
                                GlobalApplyMapKeyLighting("Num3", enochcol, false);
                                GlobalApplyMapKeyLighting("Num0", enochcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", enochcol, false);
                            }
                            else if (enochtime <= 30 && enochtime > 20)
                            {
                                GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num4", enochcol, false);
                                GlobalApplyMapKeyLighting("Num5", enochcol, false);
                                GlobalApplyMapKeyLighting("Num6", enochcol, false);
                                GlobalApplyMapKeyLighting("Num1", enochcol, false);
                                GlobalApplyMapKeyLighting("Num2", enochcol, false);
                                GlobalApplyMapKeyLighting("Num3", enochcol, false);
                                GlobalApplyMapKeyLighting("Num0", enochcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", enochcol, false);
                            }
                            else if (enochtime <= 20 && enochtime > 10)
                            {
                                GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num4", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num5", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num6", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num1", enochcol, false);
                                GlobalApplyMapKeyLighting("Num2", enochcol, false);
                                GlobalApplyMapKeyLighting("Num3", enochcol, false);
                                GlobalApplyMapKeyLighting("Num0", enochcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", enochcol, false);
                            }
                            else if (enochtime <= 10 && enochtime > 0)
                            {
                                GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num4", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num5", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num6", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num1", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num2", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num3", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num0", enochcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", enochcol, false);
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num4", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num5", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num6", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num1", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num2", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num3", negblmcol, false);
                                GlobalApplyMapKeyLighting("Num0", negblmcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", negblmcol, false);
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("Num7", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num8", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num9", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num4", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num5", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num6", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num1", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num2", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num3", negblmcol, false);
                            GlobalApplyMapKeyLighting("Num0", negblmcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", negblmcol, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                foreach (var f in DeviceEffects.LightbarZones)
                                {
                                    GlobalApplyMapLightbarLighting(f, negblmcol, false);
                                }
                            }

                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                foreach (var f in DeviceEffects.Functions)
                                {
                                    GlobalApplyMapKeyLighting(f, negblmcol, false);
                                }
                            }
                        }

                        break;
                    case Actor.Job.SMN:
                        var aetherflowsmn = Cooldowns.AetherflowCount;

                        var burstsmncol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSMNAetherflow);
                        var burstsmnempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSMNNegative);


                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate = ((int)Cooldowns.BloodOfTheDragonTimeRemaining - 0) * (JobLightbar_Collection.Length - 0) / (15 - 0) + 0;

                            

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? burstsmncol : burstsmnempty, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate = ((int)Cooldowns.BloodOfTheDragonTimeRemaining - 0) * (JobFunction_Collection.Length - 0) / (15 - 0) + 0;
                            
                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? burstsmncol : burstsmnempty, false);
                            }
                        }

                        if (aetherflowsmn > 0)
                        {
                            switch (aetherflowsmn)
                            {
                                case 2:
                                    GlobalApplyMapKeyLighting("Num9", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num6", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num3", burstsmncol, false);

                                    GlobalApplyMapKeyLighting("Num8", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num5", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num2", burstsmncol, false);

                                    GlobalApplyMapKeyLighting("Num7", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num4", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num1", burstsmncol, false);
                                    break;
                                case 1:
                                    GlobalApplyMapKeyLighting("Num9", burstsmnempty, false);
                                    GlobalApplyMapKeyLighting("Num6", burstsmnempty, false);
                                    GlobalApplyMapKeyLighting("Num3", burstsmnempty, false);

                                    GlobalApplyMapKeyLighting("Num8", burstsmnempty, false);
                                    GlobalApplyMapKeyLighting("Num5", burstsmnempty, false);
                                    GlobalApplyMapKeyLighting("Num2", burstsmnempty, false);

                                    GlobalApplyMapKeyLighting("Num7", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num4", burstsmncol, false);
                                    GlobalApplyMapKeyLighting("Num1", burstsmncol, false);
                                    break;
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("Num9", burstsmnempty, false);
                            GlobalApplyMapKeyLighting("Num6", burstsmnempty, false);
                            GlobalApplyMapKeyLighting("Num3", burstsmnempty, false);

                            GlobalApplyMapKeyLighting("Num8", burstsmnempty, false);
                            GlobalApplyMapKeyLighting("Num5", burstsmnempty, false);
                            GlobalApplyMapKeyLighting("Num2", burstsmnempty, false);

                            GlobalApplyMapKeyLighting("Num7", burstsmnempty, false);
                            GlobalApplyMapKeyLighting("Num4", burstsmnempty, false);
                            GlobalApplyMapKeyLighting("Num1", burstsmnempty, false);
                        }

                        break;
                    case Actor.Job.SCH:

                        var aetherflowsch = Cooldowns.AetherflowCount;

                        var burstschcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSCHAetherflow);
                        var burstschempty = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSCHNegative);
                        

                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(Cooldowns.OathGauge, 0, 100,
                                    JobLightbar_Collection.Length, 0);

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? burstschcol : burstschempty, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(Cooldowns.OathGauge, 0, 100,
                                    JobFunction_Collection.Length, 0);

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? burstschcol : burstschempty, false);
                            }
                        }

                        if (aetherflowsch > 0)
                        {
                            switch (aetherflowsch)
                            {
                                case 3:
                                    GlobalApplyMapKeyLighting("Num9", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num6", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num3", burstschcol, false);

                                    GlobalApplyMapKeyLighting("Num8", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num5", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num2", burstschcol, false);

                                    GlobalApplyMapKeyLighting("Num7", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num4", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num1", burstschcol, false);

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", burstschcol, false, false);
                                    }
                                    break;
                                case 2:
                                    GlobalApplyMapKeyLighting("Num9", burstschempty, false);
                                    GlobalApplyMapKeyLighting("Num6", burstschempty, false);
                                    GlobalApplyMapKeyLighting("Num3", burstschempty, false);

                                    GlobalApplyMapKeyLighting("Num8", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num5", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num2", burstschcol, false);

                                    GlobalApplyMapKeyLighting("Num7", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num4", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num1", burstschcol, false);

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", burstschcol, false, false);
                                    }
                                    break;
                                case 1:
                                    GlobalApplyMapKeyLighting("Num9", burstschempty, false);
                                    GlobalApplyMapKeyLighting("Num6", burstschempty, false);
                                    GlobalApplyMapKeyLighting("Num3", burstschempty, false);

                                    GlobalApplyMapKeyLighting("Num8", burstschempty, false);
                                    GlobalApplyMapKeyLighting("Num5", burstschempty, false);
                                    GlobalApplyMapKeyLighting("Num2", burstschempty, false);

                                    GlobalApplyMapKeyLighting("Num7", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num4", burstschcol, false);
                                    GlobalApplyMapKeyLighting("Num1", burstschcol, false);

                                    if (_LightbarMode == LightbarMode.JobGauge)
                                    {
                                        GlobalApplyMapLightbarLighting("Lightbar19", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar18", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar17", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar16", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar15", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar14", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar13", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar12", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar11", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar10", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar9", burstschempty, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar8", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar7", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar6", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar5", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar4", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar3", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar2", burstschcol, false, false);
                                        GlobalApplyMapLightbarLighting("Lightbar1", burstschcol, false, false);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("Num9", burstschempty, false);
                            GlobalApplyMapKeyLighting("Num6", burstschempty, false);
                            GlobalApplyMapKeyLighting("Num3", burstschempty, false);

                            GlobalApplyMapKeyLighting("Num8", burstschempty, false);
                            GlobalApplyMapKeyLighting("Num5", burstschempty, false);
                            GlobalApplyMapKeyLighting("Num2", burstschempty, false);

                            GlobalApplyMapKeyLighting("Num7", burstschempty, false);
                            GlobalApplyMapKeyLighting("Num4", burstschempty, false);
                            GlobalApplyMapKeyLighting("Num1", burstschempty, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                GlobalApplyMapLightbarLighting("Lightbar19", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar18", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar17", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar16", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar15", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar14", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar13", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar12", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar11", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar10", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar9", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar8", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar7", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar6", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar5", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar4", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar3", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar2", burstschempty, false, false);
                                GlobalApplyMapLightbarLighting("Lightbar1", burstschempty, false, false);
                            }
                        }

                        break;
                    case Actor.Job.NIN:
                        //Ninja
                        var negnincol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobNINNegative);
                        var hutoncol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobNINHuton);

                        var hutonremain = Cooldowns.HutonTimeRemaining;
                        var polHuton = (hutonremain - 0) * (50 - 0) / (1.0 - 0) + 0;

                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate = ((int)hutonremain - 0) * (JobLightbar_Collection.Length - 0) / (1.0 - 0) + 0;
                            
                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? hutoncol : negnincol, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate = ((int)hutonremain - 0) * (JobFunction_Collection.Length - 0) / (1.0 - 0) + 0;

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? hutoncol : negnincol, false);
                            }
                        }

                        if (polHuton <= 50 && polHuton > 40)
                        {
                            ToggleGlobalFlash3(false);

                            GlobalApplyMapKeyLighting("NumLock", hutoncol, false);
                            GlobalApplyMapKeyLighting("NumDivide", hutoncol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", hutoncol, false);

                            GlobalApplyMapKeyLighting("Num7", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num8", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num9", hutoncol, false);

                            GlobalApplyMapKeyLighting("Num4", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num5", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num6", hutoncol, false);

                            GlobalApplyMapKeyLighting("Num1", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num2", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num3", hutoncol, false);
                        }
                        else if (polHuton <= 40 && polHuton > 30)
                        {
                            ToggleGlobalFlash3(false);

                            GlobalApplyMapKeyLighting("NumLock", negnincol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negnincol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negnincol, false);

                            GlobalApplyMapKeyLighting("Num7", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num8", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num9", hutoncol, false);

                            GlobalApplyMapKeyLighting("Num4", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num5", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num6", hutoncol, false);

                            GlobalApplyMapKeyLighting("Num1", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num2", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num3", hutoncol, false);
                        }
                        else if (polHuton <= 30 && polHuton > 20)
                        {
                            ToggleGlobalFlash3(false);

                            GlobalApplyMapKeyLighting("NumLock", negnincol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negnincol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negnincol, false);

                            GlobalApplyMapKeyLighting("Num7", negnincol, false);
                            GlobalApplyMapKeyLighting("Num8", negnincol, false);
                            GlobalApplyMapKeyLighting("Num9", negnincol, false);

                            GlobalApplyMapKeyLighting("Num4", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num5", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num6", hutoncol, false);

                            GlobalApplyMapKeyLighting("Num1", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num2", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num3", hutoncol, false);
                        }
                        else if (polHuton <= 20 && polHuton > 10)
                        {
                            ToggleGlobalFlash3(false);

                            GlobalApplyMapKeyLighting("NumLock", negnincol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negnincol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negnincol, false);

                            GlobalApplyMapKeyLighting("Num7", negnincol, false);
                            GlobalApplyMapKeyLighting("Num8", negnincol, false);
                            GlobalApplyMapKeyLighting("Num9", negnincol, false);

                            GlobalApplyMapKeyLighting("Num4", negnincol, false);
                            GlobalApplyMapKeyLighting("Num5", negnincol, false);
                            GlobalApplyMapKeyLighting("Num6", negnincol, false);

                            GlobalApplyMapKeyLighting("Num1", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num2", hutoncol, false);
                            GlobalApplyMapKeyLighting("Num3", hutoncol, false);
                        }
                        else if (polHuton <= 10 && polHuton > 0)
                        {
                            //Flash
                            ToggleGlobalFlash3(true);
                            GlobalFlash3(hutoncol, 150);
                        }
                        else
                        {
                            ToggleGlobalFlash3(false);
                        }

                        break;
                    case Actor.Job.DRK:
                        var negdrkcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRKNegative);
                        var bloodcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRKBloodGauge);
                        var gritcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobDRKGrit);

                        var bloodgauge = Cooldowns.BloodGauge;
                        var bloodPol = (bloodgauge - 0) * (50 - 0) / (100 - 0) + 0;
                        
                        GlobalApplyMapKeyLighting("NumSubtract", negdrkcol, false);
                        GlobalApplyMapKeyLighting("NumAdd", negdrkcol, false);
                        GlobalApplyMapKeyLighting("NumEnter", negdrkcol, false);

                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(bloodgauge, 0, 100,
                                    JobLightbar_Collection.Length, 0);

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                if (statEffects.Find(t => t.StatusName == "Grit") != null)
                                {
                                    GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                        JobLightbar_Interpolate > i ? gritcol : negdrkcol, false, false);
                                }
                                else
                                {
                                    GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                        JobLightbar_Interpolate > i ? bloodcol : negdrkcol, false, false);
                                }
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(bloodgauge, 0, 100,
                                    JobFunction_Collection.Length, 0);

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                if (statEffects.Find(t => t.StatusName == "Grit") != null)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        JobFunction_Interpolate > i ? gritcol : negdrkcol, false);
                                }
                                else
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        JobFunction_Interpolate > i ? bloodcol : negdrkcol, false, false);
                                }
                            }
                        }


                        if (bloodPol <= 50 && bloodPol > 40)
                        {
                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                            {
                                GlobalApplyMapKeyLighting("NumLock", gritcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", gritcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", gritcol, false);

                                GlobalApplyMapKeyLighting("Num7", gritcol, false);
                                GlobalApplyMapKeyLighting("Num8", gritcol, false);
                                GlobalApplyMapKeyLighting("Num9", gritcol, false);

                                GlobalApplyMapKeyLighting("Num4", gritcol, false);
                                GlobalApplyMapKeyLighting("Num5", gritcol, false);
                                GlobalApplyMapKeyLighting("Num6", gritcol, false);

                                GlobalApplyMapKeyLighting("Num1", gritcol, false);
                                GlobalApplyMapKeyLighting("Num2", gritcol, false);
                                GlobalApplyMapKeyLighting("Num3", gritcol, false);

                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("NumLock", bloodcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", bloodcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num7", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num8", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num9", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num4", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num5", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num6", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);
                            }
                        }
                        else if (bloodPol <= 40 && bloodPol > 30)
                        {
                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num7", gritcol, false);
                                GlobalApplyMapKeyLighting("Num8", gritcol, false);
                                GlobalApplyMapKeyLighting("Num9", gritcol, false);

                                GlobalApplyMapKeyLighting("Num4", gritcol, false);
                                GlobalApplyMapKeyLighting("Num5", gritcol, false);
                                GlobalApplyMapKeyLighting("Num6", gritcol, false);

                                GlobalApplyMapKeyLighting("Num1", gritcol, false);
                                GlobalApplyMapKeyLighting("Num2", gritcol, false);
                                GlobalApplyMapKeyLighting("Num3", gritcol, false);

                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num7", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num8", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num9", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num4", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num5", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num6", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);
                            }
                        }
                        if (bloodPol <= 30 && bloodPol > 20)
                        {
                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num4", gritcol, false);
                                GlobalApplyMapKeyLighting("Num5", gritcol, false);
                                GlobalApplyMapKeyLighting("Num6", gritcol, false);

                                GlobalApplyMapKeyLighting("Num1", gritcol, false);
                                GlobalApplyMapKeyLighting("Num2", gritcol, false);
                                GlobalApplyMapKeyLighting("Num3", gritcol, false);

                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num4", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num5", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num6", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);
                            }
                        }
                        else if (bloodPol <= 20 && bloodPol > 10)
                        {
                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num1", gritcol, false);
                                GlobalApplyMapKeyLighting("Num2", gritcol, false);
                                GlobalApplyMapKeyLighting("Num3", gritcol, false);

                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num1", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num2", bloodcol, false);
                                GlobalApplyMapKeyLighting("Num3", bloodcol, false);

                                GlobalApplyMapKeyLighting("Num0", bloodcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", bloodcol, false);
                            }
                        }
                        else if (bloodPol <= 10 && bloodPol > 0)
                        {
                            if (statEffects.Find(i => i.StatusName == "Grit") != null)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num1", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num2", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num3", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num0", gritcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", gritcol, false);
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num1", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num2", negdrkcol, false);
                                GlobalApplyMapKeyLighting("Num3", negdrkcol, false);

                                GlobalApplyMapKeyLighting("Num0", negdrkcol, false);
                                GlobalApplyMapKeyLighting("NumDecimal", negdrkcol, false);
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumLock", negdrkcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negdrkcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negdrkcol, false);

                            GlobalApplyMapKeyLighting("Num7", negdrkcol, false);
                            GlobalApplyMapKeyLighting("Num8", negdrkcol, false);
                            GlobalApplyMapKeyLighting("Num9", negdrkcol, false);

                            GlobalApplyMapKeyLighting("Num4", negdrkcol, false);
                            GlobalApplyMapKeyLighting("Num5", negdrkcol, false);
                            GlobalApplyMapKeyLighting("Num6", negdrkcol, false);

                            GlobalApplyMapKeyLighting("Num1", negdrkcol, false);
                            GlobalApplyMapKeyLighting("Num2", negdrkcol, false);
                            GlobalApplyMapKeyLighting("Num3", negdrkcol, false);

                            GlobalApplyMapKeyLighting("Num0", negdrkcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", negdrkcol, false);
                        }
                        break;
                    case Actor.Job.AST:
                        var burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTNegative);
                        

                        if (Cooldowns.CurrentCard != Cooldowns.CardTypes.None)
                        {
                            var tR = 30;

                            switch (Cooldowns.CurrentCard)
                            {
                                case Cooldowns.CardTypes.Arrow:
                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTArrow);

                                    if (statEffects.Find(i => i.StatusName == "Expanded Royal Road") != null)
                                    {
                                        tR = 60;
                                    }

                                    break;
                                case Cooldowns.CardTypes.Balance:
                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTBalance);
                                    break;
                                case Cooldowns.CardTypes.Bole:
                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTBole);
                                    break;
                                case Cooldowns.CardTypes.Ewer:
                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTEwer);
                                    break;
                                case Cooldowns.CardTypes.Spear:
                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTSpear);

                                    if (statEffects.Find(i => i.StatusName == "Expanded Royal Road") != null)
                                    {
                                        tR = 60;
                                    }

                                    break;
                                case Cooldowns.CardTypes.Spire:
                                    burstastcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTSpire);
                                    break;
                            }

                            if (Cooldowns.CurrentCard != _currentCard)
                            {
                                if (Cooldowns.CurrentCard != Cooldowns.CardTypes.None)
                                    GlobalRipple1(burstastcol, 80, baseColor);

                                _currentCard = Cooldowns.CurrentCard;
                            }

                            GlobalApplyMapKeyLighting("NumLock", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumSubtract", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num7", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num8", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num9", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num4", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num5", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num6", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num1", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num2", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num3", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num0", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", burstastcol, false);
                            

                            //Lightbar
                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                var JobLightbar_Collection = DeviceEffects.LightbarZones;
                                var JobLightbar_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int((int)Cooldowns.CurrentCardRemainingTime, 0, tR,
                                        JobLightbar_Collection.Length, 0);

                                for (int i = 0; i < JobLightbar_Collection.Length; i++)
                                {
                                    GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                        JobLightbar_Interpolate > i ? burstastcol : ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTNegative), false, false);
                                }
                            }

                            //FKeys
                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                var JobFunction_Collection = DeviceEffects.Functions;
                                var JobFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int((int)Cooldowns.CurrentCardRemainingTime, 0, tR,
                                        JobFunction_Collection.Length, 0);

                                for (int i = 0; i < JobFunction_Collection.Length; i++)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        JobFunction_Interpolate > i ? burstastcol : ColorTranslator.FromHtml(ColorMappings.ColorMappingJobASTNegative), false);
                                }
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumLock", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumSubtract", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num7", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num8", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num9", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumAdd", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num4", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num5", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num6", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumEnter", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num1", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num2", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num3", burstastcol, false);
                            GlobalApplyMapKeyLighting("Num0", burstastcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", burstastcol, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                foreach (var f in DeviceEffects.LightbarZones)
                                {
                                    GlobalApplyMapLightbarLighting(f, burstastcol, false);
                                }
                            }

                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                foreach (var f in DeviceEffects.Functions)
                                {
                                    GlobalApplyMapKeyLighting(f, burstastcol, false);
                                }
                            }
                        }
                        break;
                    case Actor.Job.MCH:
                        //Machinist
                        var ammo = Cooldowns.Battery;

                        var ammoburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMCHAmmo);
                        var negmchburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMCHNegative);

                        var heatnormal = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMCHHeatGauge);
                        var heatover = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobMCHOverheat);

                        switch (ammo)
                        {
                            case 1:
                                GlobalApplyMapKeyLighting("NumSubtract", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumAdd", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumEnter", ammoburst, false);
                                break;
                            case 2:
                                GlobalApplyMapKeyLighting("NumSubtract", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumAdd", ammoburst, false);
                                GlobalApplyMapKeyLighting("NumEnter", ammoburst, false);
                                break;
                            case 3:
                                GlobalApplyMapKeyLighting("NumSubtract", ammoburst, false);
                                GlobalApplyMapKeyLighting("NumAdd", ammoburst, false);
                                GlobalApplyMapKeyLighting("NumEnter", ammoburst, false);
                                break;
                            default:
                                GlobalApplyMapKeyLighting("NumSubtract", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumAdd", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumEnter", negmchburst, false);
                                break;
                        }


                        var mchgb = Cooldowns.HeatGauge;

                        if (Cooldowns.HyperchargeActive)
                        {
                            //Overheating
                            GlobalApplyMapKeyLighting("NumLock", heatover, false);
                            GlobalApplyMapKeyLighting("NumDivide", heatover, false);
                            GlobalApplyMapKeyLighting("NumMultiply", heatover, false);
                            GlobalApplyMapKeyLighting("Num7", heatover, false);
                            GlobalApplyMapKeyLighting("Num8", heatover, false);
                            GlobalApplyMapKeyLighting("Num9", heatover, false);
                            GlobalApplyMapKeyLighting("Num4", heatover, false);
                            GlobalApplyMapKeyLighting("Num5", heatover, false);
                            GlobalApplyMapKeyLighting("Num6", heatover, false);
                            GlobalApplyMapKeyLighting("Num1", heatover, false);
                            GlobalApplyMapKeyLighting("Num2", heatover, false);
                            GlobalApplyMapKeyLighting("Num3", heatover, false);
                            GlobalApplyMapKeyLighting("Num0", heatover, false);
                            GlobalApplyMapKeyLighting("NumDecimal", heatover, false);

                            //Lightbar
                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                var JobLightbar_Collection = DeviceEffects.LightbarZones;
                                var JobLightbar_Interpolate = ((int)mchgb - 0) * (JobLightbar_Collection.Length - 0) / (1.0 - 0) + 0;

                                

                                for (int i = 0; i < JobLightbar_Collection.Length; i++)
                                {
                                    GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                        JobLightbar_Interpolate > i ? heatover : negmchburst, false, false);
                                }
                            }

                            //FKeys
                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                var JobFunction_Collection = DeviceEffects.Functions;
                                var JobFunction_Interpolate = ((int)mchgb - 0) * (JobFunction_Collection.Length - 0) / (1.0 - 0) + 0;

                                for (int i = 0; i < JobFunction_Collection.Length; i++)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        JobFunction_Interpolate > i ? heatover : negmchburst, false);
                                }
                            }
                        }
                        else
                        {
                            //Normal
                            var polGB = (mchgb - 0) * (50 - 0) / (100 - 0) + 0;

                            //Lightbar
                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                var JobLightbar_Collection = DeviceEffects.LightbarZones;
                                var JobLightbar_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(mchgb, 0, 100,
                                        JobLightbar_Collection.Length, 0);

                                for (int i = 0; i < JobLightbar_Collection.Length; i++)
                                {
                                    GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                        JobLightbar_Interpolate > i ? heatnormal : negmchburst, false, false);
                                }
                            }

                            //FKeys
                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                var JobFunction_Collection = DeviceEffects.Functions;
                                var JobFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(mchgb, 0, 100,
                                        JobFunction_Collection.Length, 0);

                                for (int i = 0; i < JobFunction_Collection.Length; i++)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        JobFunction_Interpolate > i ? heatnormal : negmchburst, false);
                                }
                            }

                            if (polGB <= 50 && polGB > 40)
                            {
                                GlobalApplyMapKeyLighting("NumLock", heatnormal, false);
                                GlobalApplyMapKeyLighting("NumDivide", heatnormal, false);
                                GlobalApplyMapKeyLighting("NumMultiply", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num7", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num8", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num9", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num4", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num5", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num6", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num1", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num2", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num3", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);
                            }
                            else if (polGB <= 40 && polGB > 30)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num7", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num8", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num9", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num4", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num5", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num6", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num1", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num2", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num3", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);
                            }
                            else if (polGB <= 30 && polGB > 20)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num4", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num5", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num6", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num1", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num2", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num3", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);
                                
                            }
                            else if (polGB <= 20 && polGB > 10)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num4", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num5", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num6", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num1", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num2", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num3", heatnormal, false);
                                GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);
                            }
                            else if (polGB <= 10 && polGB > 0)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num4", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num5", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num6", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num1", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num2", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num3", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num0", heatnormal, false);
                                GlobalApplyMapKeyLighting("NumDecimal", heatnormal, false);
                            }
                            else if (polGB == 0)
                            {
                                GlobalApplyMapKeyLighting("NumLock", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumDivide", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumMultiply", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num7", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num8", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num9", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num4", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num5", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num6", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num1", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num2", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num3", negmchburst, false);
                                GlobalApplyMapKeyLighting("Num0", negmchburst, false);
                                GlobalApplyMapKeyLighting("NumDecimal", negmchburst, false);
                                
                            }
                        }

                        break;
                    case Actor.Job.SAM:
                        //Samurai
                        var negsamcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMNegative);
                        var setsucol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMSetsu); //Top
                        var getsucol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMGetsu); //Left
                        var kacol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMKa); //Right
                        var kenkicol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobSAMKenki);

                        var sen = Cooldowns.SenGauge;
                        var kenkicharge = Cooldowns.KenkiCharge;
                        var PolKenki = (kenkicharge - 0) * (40 - 0) / (100 - 0) + 0;

                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_Collection = DeviceEffects.LightbarZones;
                            var JobLightbar_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(kenkicharge, 0, 100,
                                    JobLightbar_Collection.Length, 0);

                            for (int i = 0; i < JobLightbar_Collection.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                    JobLightbar_Interpolate > i ? kenkicol : negsamcol, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_Collection = DeviceEffects.Functions;
                            var JobFunction_Interpolate =
                                Helpers.FFXIVInterpolation.Interpolate_Int(kenkicharge, 0, 100,
                                    JobFunction_Collection.Length, 0);

                            for (int i = 0; i < JobFunction_Collection.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                    JobFunction_Interpolate > i ? kenkicol : negsamcol, false);
                            }
                        }

                        switch (sen)
                        {
                            case 1:
                                GlobalApplyMapKeyLighting("Num8", setsucol, false);
                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num3", negsamcol, false);
                                break;
                            case 2:
                                GlobalApplyMapKeyLighting("Num8", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num1", getsucol, false);
                                GlobalApplyMapKeyLighting("Num3", negsamcol, false);
                                break;
                            case 3:
                                GlobalApplyMapKeyLighting("Num8", setsucol, false);
                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num3", getsucol, false);
                                break;
                            case 4:
                                GlobalApplyMapKeyLighting("Num8", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num3", kacol, false);
                                break;
                            case 5:
                                GlobalApplyMapKeyLighting("Num8", setsucol, false);
                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num3", kacol, false);
                                break;
                            case 6:
                                GlobalApplyMapKeyLighting("Num8", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num1", getsucol, false);
                                GlobalApplyMapKeyLighting("Num3", kacol, false);
                                break;
                            case 7:
                                GlobalApplyMapKeyLighting("Num8", setsucol, false);
                                GlobalApplyMapKeyLighting("Num1", getsucol, false);
                                GlobalApplyMapKeyLighting("Num3", kacol, false);
                                break;
                            default:
                                GlobalApplyMapKeyLighting("Num8", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num1", negsamcol, false);
                                GlobalApplyMapKeyLighting("Num3", negsamcol, false);
                                break;
                        }

                        if (PolKenki <= 40 && PolKenki > 30)
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", kenkicol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", kenkicol, false);
                            GlobalApplyMapKeyLighting("NumDivide", kenkicol, false);
                            GlobalApplyMapKeyLighting("NumLock", kenkicol, false);
                        }
                        else if (PolKenki <= 30 && PolKenki > 20)
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", kenkicol, false);
                            GlobalApplyMapKeyLighting("NumDivide", kenkicol, false);
                            GlobalApplyMapKeyLighting("NumLock", kenkicol, false);
                        }
                        else if (PolKenki <= 20 && PolKenki > 10)
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", kenkicol, false);
                            GlobalApplyMapKeyLighting("NumLock", kenkicol, false);
                        }
                        else if (PolKenki <= 10 && PolKenki > 0)
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumLock", kenkicol, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumSubtract", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumDivide", negsamcol, false);
                            GlobalApplyMapKeyLighting("NumLock", negsamcol, false);
                        }

                        GlobalApplyMapKeyLighting("Num2", negsamcol, false);
                        GlobalApplyMapKeyLighting("Num4", negsamcol, false);
                        GlobalApplyMapKeyLighting("Num5", negsamcol, false);
                        GlobalApplyMapKeyLighting("Num6", negsamcol, false);
                        GlobalApplyMapKeyLighting("Num7", negsamcol, false);
                        GlobalApplyMapKeyLighting("Num9", negsamcol, false);
                        GlobalApplyMapKeyLighting("NumAdd", negsamcol, false);
                        GlobalApplyMapKeyLighting("NumEnter", negsamcol, false);
                        GlobalApplyMapKeyLighting("Num0", negsamcol, false);
                        GlobalApplyMapKeyLighting("NumDecimal", negsamcol, false);

                        break;
                    case Actor.Job.RDM:
                        var blackmana = Cooldowns.BlackMana;
                        var whitemana = Cooldowns.WhiteMana;
                        var polBlack = (blackmana - 0) * (40 - 0) / (100 - 0) + 0;
                        var polWhite = (whitemana - 0) * (40 - 0) / (100 - 0) + 0;
                        
                        var blackburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMBlackMana);
                        var whiteburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMWhiteMana);
                        var negburst = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobRDMNegative);

                        //Lightbar
                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            var JobLightbar_CollectionA = DeviceEffects.LightbarZonesR.ToList();
                            JobLightbar_CollectionA.Reverse();
                            var JobLightbar_InterpolateA =
                                Helpers.FFXIVInterpolation.Interpolate_Int(blackmana, 0, 100,
                                    JobLightbar_CollectionA.Count, 0);

                            for (int i = 0; i < JobLightbar_CollectionA.Count; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_CollectionA[i],
                                    JobLightbar_InterpolateA > i ? blackburst : negburst, false, false);
                            }

                            var JobLightbar_CollectionB = DeviceEffects.LightbarZonesL;
                            var JobLightbar_InterpolateB =
                                Helpers.FFXIVInterpolation.Interpolate_Int(whitemana, 0, 100,
                                    JobLightbar_CollectionB.Length, 0);

                            for (int i = 0; i < JobLightbar_CollectionB.Length; i++)
                            {
                                GlobalApplyMapLightbarLighting(JobLightbar_CollectionB[i],
                                    JobLightbar_InterpolateB > i ? whiteburst : negburst, false, false);
                            }
                        }

                        //FKeys
                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            var JobFunction_CollectionA = DeviceEffects.FunctionR.ToList();
                            JobFunction_CollectionA.Reverse();
                            var JobFunction_InterpolateA =
                                Helpers.FFXIVInterpolation.Interpolate_Int(blackmana, 0, 100,
                                    JobFunction_CollectionA.Count, 0);

                            for (int i = 0; i < JobFunction_CollectionA.Count; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_CollectionA[i],
                                    JobFunction_InterpolateA > i ? blackburst : negburst, false);
                            }

                            var JobFunction_CollectionB = DeviceEffects.FunctionL;
                            var JobFunction_InterpolateB =
                                Helpers.FFXIVInterpolation.Interpolate_Int(whitemana, 0, 100,
                                    JobFunction_CollectionB.Length, 0);

                            for (int i = 0; i < JobFunction_CollectionB.Length; i++)
                            {
                                GlobalApplyMapKeyLighting(JobFunction_CollectionB[i],
                                    JobFunction_InterpolateB > i ? whiteburst : negburst, false);
                            }
                        }

                        GlobalApplyMapKeyLighting("NumDivide", Color.Black, false);
                        GlobalApplyMapKeyLighting("Num8", Color.Black, false);
                        GlobalApplyMapKeyLighting("Num5", Color.Black, false);
                        GlobalApplyMapKeyLighting("Num2", Color.Black, false);

                        //Black
                        if (polBlack <= 40 && polBlack > 30)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", blackburst, false);
                            GlobalApplyMapKeyLighting("Num9", blackburst, false);
                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                            GlobalApplyMapKeyLighting("Num3", blackburst, false);
                        }
                        else if (polBlack <= 30 && polBlack > 20)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                            GlobalApplyMapKeyLighting("Num9", blackburst, false);
                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                            GlobalApplyMapKeyLighting("Num3", blackburst, false);
                        }
                        else if (polBlack <= 20 && polBlack > 10)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                            GlobalApplyMapKeyLighting("Num6", blackburst, false);
                            GlobalApplyMapKeyLighting("Num3", blackburst, false);
                           
                        }
                        else if (polBlack <= 10 && polBlack > 0)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                            GlobalApplyMapKeyLighting("Num6", negburst, false);
                            GlobalApplyMapKeyLighting("Num3", blackburst, false);
                            
                        }
                        else if (polBlack == 0)
                        {
                            GlobalApplyMapKeyLighting("NumMultiply", negburst, false);
                            GlobalApplyMapKeyLighting("Num9", negburst, false);
                            GlobalApplyMapKeyLighting("Num6", negburst, false);
                            GlobalApplyMapKeyLighting("Num3", negburst, false);
                            
                        }


                        //White
                        if (polWhite <= 40 && polWhite > 30)
                        {
                            GlobalApplyMapKeyLighting("NumLock", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                            
                        }
                        else if (polWhite <= 30 && polWhite > 20)
                        {
                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                            GlobalApplyMapKeyLighting("Num7", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                            
                        }
                        else if (polWhite <= 20 && polWhite > 10)
                        {
                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                            GlobalApplyMapKeyLighting("Num4", whiteburst, false);
                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                            
                        }
                        else if (polWhite <= 10 && polWhite > 0)
                        {
                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                            GlobalApplyMapKeyLighting("Num4", negburst, false);
                            GlobalApplyMapKeyLighting("Num1", whiteburst, false);
                            
                        }
                        else if (polWhite == 0)
                        {
                            GlobalApplyMapKeyLighting("NumLock", negburst, false);
                            GlobalApplyMapKeyLighting("Num7", negburst, false);
                            GlobalApplyMapKeyLighting("Num4", negburst, false);
                            GlobalApplyMapKeyLighting("Num1", negburst, false);
                            
                        }

                        break;
                    case Actor.Job.DNC:

                        if (hotbars != null)
                        { 
                            if (statEffects.Find(i => i.StatusName == "Technical Finish") != null)
                            {
                                GlobalApplyMapKeyLighting("NumLock",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCTechnicalFinish), false);
                                GlobalApplyMapKeyLighting("NumDivide",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCTechnicalFinish), false);
                                GlobalApplyMapKeyLighting("NumMultiply",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCTechnicalFinish), false);
                                GlobalApplyMapKeyLighting("NumSubtract",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCTechnicalFinish), false);
                            }
                            else if (statEffects.Find(i => i.StatusName == "Standard Finish") != null)
                            {
                                GlobalApplyMapKeyLighting("NumLock",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCStandardFinish), false);
                                GlobalApplyMapKeyLighting("NumDivide",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCStandardFinish), false);
                                GlobalApplyMapKeyLighting("NumMultiply",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCStandardFinish), false);
                                GlobalApplyMapKeyLighting("NumSubtract",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCStandardFinish), false);
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("NumLock",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative), false);
                                GlobalApplyMapKeyLighting("NumDivide",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative), false);
                                GlobalApplyMapKeyLighting("NumMultiply",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative), false);
                                GlobalApplyMapKeyLighting("NumSubtract",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative), false);
                            }

                            if (statEffects.Find(i => i.StatusName == "Standard Step") != null)
                            {

                                foreach (var hotbar in hotbars.ActionContainers)
                                {
                                    if (hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_1 ||
                                        hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_2 ||
                                        hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_3 ||
                                        hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_4 ||
                                        hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_5 ||
                                        hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_6 ||
                                        hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_7 ||
                                        hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_HOTBAR_8 ||
                                        hotbar.ContainerType == Sharlayan.Core.Enums.Action.Container.CROSS_PETBAR)
                                        continue;

                                    foreach (var action in hotbar.ActionItems)
                                    {
                                        if (string.IsNullOrEmpty(action.Name)) continue;
                                        if (action.Name == "Entrechat" || action.Name == "Pirouette" ||
                                            action.Name == "Emboite" ||
                                            action.Name == "Jete" || action.Name == "Double Standard Finish")
                                        {
                                            if (action.IsAvailable || !_playerInfo.IsCasting)
                                            {
                                                switch (action.Name)
                                                {
                                                    case "Entrechat":
                                                        //Blue
                                                        if (action.IsProcOrCombo)
                                                        {
                                                            GlobalApplyMapKeyLighting("Num7",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num8",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num9",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num6",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num5",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num4",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num3",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num2",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num1",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("Num0",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("NumAdd",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("NumEnter",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("NumDecimal",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);

                                                            if (_FKeyMode == FKeyMode.JobGauge)
                                                            {
                                                                var JobFunction_Collection = DeviceEffects.Functions.ToList();

                                                                for (int i = 0; i < JobFunction_Collection.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCEntrechat), false);
                                                                }
                                                            }

                                                            if (_LightbarMode == LightbarMode.JobGauge)
                                                            {
                                                                var Lightbar_Collection = DeviceEffects.LightbarZones.ToList();

                                                                for (int i = 0; i < Lightbar_Collection.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(Lightbar_Collection[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCEntrechat), false);
                                                                }
                                                            }
                                                        }

                                                        break;
                                                    case "Pirouette":
                                                        //Yellow
                                                        if (action.IsProcOrCombo)
                                                        {
                                                            GlobalApplyMapKeyLighting("Num7",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num8",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num9",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num6",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num5",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num4",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num3",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num2",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num1",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num0",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("NumAdd",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("NumEnter",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("NumDecimal",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);

                                                            if (_FKeyMode == FKeyMode.JobGauge)
                                                            {
                                                                var JobFunction_Collection = DeviceEffects.Functions.ToList();

                                                                for (int i = 0; i < JobFunction_Collection.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCPirouette), false);
                                                                }
                                                            }

                                                            if (_LightbarMode == LightbarMode.JobGauge)
                                                            {
                                                                var Lightbar_Collection = DeviceEffects.LightbarZones.ToList();

                                                                for (int i = 0; i < Lightbar_Collection.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(Lightbar_Collection[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCPirouette), false);
                                                                }
                                                            }
                                                        }

                                                        break;
                                                    case "Emboite":
                                                        //Red

                                                        if (action.IsProcOrCombo)
                                                        {
                                                            GlobalApplyMapKeyLighting("Num7",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num8",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num9",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num6",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num5",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num4",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num3",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num2",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num1",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num0",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("NumAdd",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("NumEnter",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("NumDecimal",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);

                                                            if (_FKeyMode == FKeyMode.JobGauge)
                                                            {
                                                                var JobFunction_Collection = DeviceEffects.Functions.ToList();

                                                                for (int i = 0; i < JobFunction_Collection.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCEmboite), false);
                                                                }
                                                            }

                                                            if (_LightbarMode == LightbarMode.JobGauge)
                                                            {
                                                                var Lightbar_Collection = DeviceEffects.LightbarZones.ToList();

                                                                for (int i = 0; i < Lightbar_Collection.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(Lightbar_Collection[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCEmboite), false);
                                                                }
                                                            }
                                                        }

                                                        break;
                                                    case "Jete":
                                                        //Green

                                                        if (action.IsProcOrCombo)
                                                        {
                                                            GlobalApplyMapKeyLighting("Num7",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num8",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num9",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num6",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num5",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num4",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num3",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num2",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num1",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num0",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("NumAdd",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("NumEnter",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("NumDecimal",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);

                                                            if (_FKeyMode == FKeyMode.JobGauge)
                                                            {
                                                                var JobFunction_Collection = DeviceEffects.Functions.ToList();

                                                                for (int i = 0; i < JobFunction_Collection.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCJete), false);
                                                                }
                                                            }

                                                            if (_LightbarMode == LightbarMode.JobGauge)
                                                            {
                                                                var Lightbar_Collection = DeviceEffects.LightbarZones.ToList();

                                                                for (int i = 0; i < Lightbar_Collection.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(Lightbar_Collection[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCJete), false);
                                                                }
                                                            }
                                                        }

                                                        break;
                                                    case "Double Standard Finish":
                                                        //Green
                                                        if (action.IsProcOrCombo)
                                                        {
                                                            GlobalApplyMapKeyLighting("Num7",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num8",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num9",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEmboite), false);
                                                            GlobalApplyMapKeyLighting("Num6",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num5",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num4",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCJete), false);
                                                            GlobalApplyMapKeyLighting("Num3",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num2",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num1",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCPirouette), false);
                                                            GlobalApplyMapKeyLighting("Num0",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("NumAdd",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("NumEnter",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);
                                                            GlobalApplyMapKeyLighting("NumDecimal",
                                                                ColorTranslator.FromHtml(ColorMappings
                                                                    .ColorMappingJobDNCEntrechat), false);

                                                            if (_FKeyMode == FKeyMode.JobGauge)
                                                            {
                                                                var JobFunction_CollectionA = DeviceEffects.Function1.ToList();
                                                                var JobFunction_CollectionB = DeviceEffects.Function2.ToList();
                                                                var JobFunction_CollectionC = DeviceEffects.Function3.ToList();

                                                                for (int i = 0; i < JobFunction_CollectionA.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(JobFunction_CollectionA[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCEmboite), false);
                                                                }

                                                                for (int i = 0; i < JobFunction_CollectionB.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(JobFunction_CollectionB[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCJete), false);
                                                                }

                                                                for (int i = 0; i < JobFunction_CollectionC.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(JobFunction_CollectionC[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCPirouette), false);
                                                                }
                                                            }

                                                            if (_LightbarMode == LightbarMode.JobGauge)
                                                            {
                                                                var Lightbar_CollectionA = DeviceEffects.LightbarZonesL.ToList();
                                                                var Lightbar_CollectionB = DeviceEffects.LightbarZonesL.ToList();

                                                                for (int i = 0; i < Lightbar_CollectionA.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(Lightbar_CollectionA[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCEmboite), false);
                                                                }

                                                                for (int i = 0; i < Lightbar_CollectionB.Count; i++)
                                                                {
                                                                    GlobalApplyMapKeyLighting(Lightbar_CollectionB[i],
                                                                        ColorTranslator.FromHtml(ColorMappings
                                                                            .ColorMappingJobDNCJete), false);
                                                                }
                                                            }
                                                        }

                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                GlobalApplyMapKeyLighting("Num7",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num8",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num9",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num6",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num5",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num4",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num3",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num2",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num1",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("Num0",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("NumAdd",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("NumEnter",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);
                                GlobalApplyMapKeyLighting("NumDecimal",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobDNCNegative),
                                    false);

                                if (_FKeyMode == FKeyMode.JobGauge)
                                {
                                    var JobFunction_Collection = DeviceEffects.Functions.ToList();

                                    for (int i = 0; i < JobFunction_Collection.Count; i++)
                                    {
                                        GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                            ColorTranslator.FromHtml(ColorMappings
                                                .ColorMappingJobDNCNegative), false);
                                    }
                                }

                                if (_LightbarMode == LightbarMode.JobGauge)
                                {
                                    var Lightbar_Collection = DeviceEffects.LightbarZones.ToList();

                                    for (int i = 0; i < Lightbar_Collection.Count; i++)
                                    {
                                        GlobalApplyMapKeyLighting(Lightbar_Collection[i],
                                            ColorTranslator.FromHtml(ColorMappings
                                                .ColorMappingJobDNCNegative), false);
                                    }
                                }
                            }
                        }

                        break;
                    case Actor.Job.GNB:
                        if (statEffects.Find(i => i.StatusName == "Royal Guard") != null)
                        {
                            GlobalApplyMapKeyLighting("NumLock",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard), false);
                            GlobalApplyMapKeyLighting("NumDivide",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard), false);
                            GlobalApplyMapKeyLighting("NumMultiply",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard), false);
                            GlobalApplyMapKeyLighting("NumSubtract",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard), false);
                            GlobalApplyMapKeyLighting("Num7",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobGNBRoyalGuard),
                                    false);
                            GlobalApplyMapKeyLighting("Num8",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("Num9",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("Num6",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("Num5",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("Num4",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("Num3",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("Num2",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("Num1",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("Num0",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("NumAdd",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("NumEnter",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);
                            GlobalApplyMapKeyLighting("NumDecimal",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBRoyalGuard),
                                false);

                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                var JobFunction_Collection = DeviceEffects.Functions.ToList();

                                for (int i = 0; i < JobFunction_Collection.Count; i++)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        ColorTranslator.FromHtml(ColorMappings
                                            .ColorMappingJobGNBRoyalGuard), false);
                                }
                            }

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                var Lightbar_Collection = DeviceEffects.LightbarZones.ToList();

                                for (int i = 0; i < Lightbar_Collection.Count; i++)
                                {
                                    GlobalApplyMapKeyLighting(Lightbar_Collection[i],
                                        ColorTranslator.FromHtml(ColorMappings
                                            .ColorMappingJobGNBRoyalGuard), false);
                                }
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumLock",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative), false);
                            GlobalApplyMapKeyLighting("NumDivide",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative), false);
                            GlobalApplyMapKeyLighting("NumMultiply",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative), false);
                            GlobalApplyMapKeyLighting("NumSubtract",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative), false);
                            GlobalApplyMapKeyLighting("Num7",
                                    ColorTranslator.FromHtml(ColorMappings
                                        .ColorMappingJobGNBNegative),
                                    false);
                            GlobalApplyMapKeyLighting("Num8",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("Num9",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("Num6",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("Num5",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("Num4",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("Num3",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("Num2",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("Num1",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("Num0",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("NumAdd",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("NumEnter",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);
                            GlobalApplyMapKeyLighting("NumDecimal",
                                ColorTranslator.FromHtml(ColorMappings
                                    .ColorMappingJobGNBNegative),
                                false);

                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                var JobFunction_Collection = DeviceEffects.Functions.ToList();

                                for (int i = 0; i < JobFunction_Collection.Count; i++)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        ColorTranslator.FromHtml(ColorMappings
                                            .ColorMappingJobGNBNegative), false);
                                }
                            }

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                var Lightbar_Collection = DeviceEffects.LightbarZones.ToList();

                                for (int i = 0; i < Lightbar_Collection.Count; i++)
                                {
                                    GlobalApplyMapKeyLighting(Lightbar_Collection[i],
                                        ColorTranslator.FromHtml(ColorMappings
                                            .ColorMappingJobGNBNegative), false);
                                }
                            }
                        }
                        break;
                    case Actor.Job.ALC:
                    case Actor.Job.ARM:
                    case Actor.Job.BSM:
                    case Actor.Job.CPT:
                    case Actor.Job.CUL:
                    case Actor.Job.GSM:
                    case Actor.Job.LTW:
                    case Actor.Job.WVR:
                        //Crafter
                        var negcraftercol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobCrafterNegative);
                        var innerquietcol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobCrafterInnerquiet);
                        var collectablecol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobCrafterCollectable);
                        var craftercol = ColorTranslator.FromHtml(ColorMappings.ColorMappingJobCrafterCrafter);
                        
                        if (statEffects.Find(i => i.StatusName == "Collectable Synthesis") != null)
                        {
                            GlobalApplyMapKeyLighting("NumLock", collectablecol, false);
                            GlobalApplyMapKeyLighting("NumDivide", collectablecol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", collectablecol, false);
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("NumLock", craftercol, false);
                            GlobalApplyMapKeyLighting("NumDivide", craftercol, false);
                            GlobalApplyMapKeyLighting("NumMultiply", craftercol, false);
                        }
                        
                        if (statEffects.Find(i => i.StatusName == "Inner Quiet") != null)
                        {
                            var IQStacks = statEffects.Find(i => i.StatusName == "Inner Quiet").Stacks;

                            //Lightbar
                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                var JobLightbar_Collection = DeviceEffects.LightbarZones;
                                var JobLightbar_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(IQStacks, 0, 12,
                                        JobLightbar_Collection.Length, 0);

                                for (int i = 0; i < JobLightbar_Collection.Length; i++)
                                {
                                    GlobalApplyMapLightbarLighting(JobLightbar_Collection[i],
                                        JobLightbar_Interpolate > i ? innerquietcol : negcraftercol, false, false);
                                }
                            }

                            //FKeys
                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                var JobFunction_Collection = DeviceEffects.Functions;
                                var JobFunction_Interpolate =
                                    Helpers.FFXIVInterpolation.Interpolate_Int(IQStacks, 0, 12,
                                        JobFunction_Collection.Length, 0);

                                for (int i = 0; i < JobFunction_Collection.Length; i++)
                                {
                                    GlobalApplyMapKeyLighting(JobFunction_Collection[i],
                                        JobFunction_Interpolate > i ? innerquietcol : negcraftercol, false);
                                }
                            }

                            switch (IQStacks)
                            {
                                case 1:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 2:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 3:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 4:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 5:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 6:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", innerquietcol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 7:
                                    GlobalApplyMapKeyLighting("Num7", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 8:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 9:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", innerquietcol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 10:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                case 11:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", innerquietcol, false);
                                    break;
                                case 12:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num2", innerquietcol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);
                                    break;
                                default:
                                    GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                                    GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("Num0", negcraftercol, false);
                                    GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                                    break;
                            }
                        }
                        else
                        {
                            GlobalApplyMapKeyLighting("Num7", negcraftercol, false);
                            GlobalApplyMapKeyLighting("Num8", negcraftercol, false);
                            GlobalApplyMapKeyLighting("Num9", negcraftercol, false);

                            GlobalApplyMapKeyLighting("Num4", negcraftercol, false);
                            GlobalApplyMapKeyLighting("Num5", negcraftercol, false);
                            GlobalApplyMapKeyLighting("Num6", negcraftercol, false);

                            GlobalApplyMapKeyLighting("Num1", negcraftercol, false);
                            GlobalApplyMapKeyLighting("Num2", negcraftercol, false);
                            GlobalApplyMapKeyLighting("Num3", negcraftercol, false);
                            GlobalApplyMapKeyLighting("Num0", innerquietcol, false);
                            GlobalApplyMapKeyLighting("NumDecimal", negcraftercol, false);

                            if (_LightbarMode == LightbarMode.JobGauge)
                            {
                                foreach (var f in DeviceEffects.LightbarZones)
                                {
                                    GlobalApplyMapLightbarLighting(f, negcraftercol, false);
                                }
                            }

                            if (_FKeyMode == FKeyMode.JobGauge)
                            {
                                foreach (var f in DeviceEffects.Functions)
                                {
                                    GlobalApplyMapKeyLighting(f, negcraftercol, false);
                                }
                            }
                        }


                        GlobalApplyMapKeyLighting("NumEnter", negcraftercol, false);
                        GlobalApplyMapKeyLighting("NumAdd", negcraftercol, false);
                        GlobalApplyMapKeyLighting("NumSubtract", negcraftercol, false);

                        break;

                    case Actor.Job.FSH:
                    case Actor.Job.BTN:
                    case Actor.Job.MIN:
                        //Gatherer
                        GlobalApplyMapKeyLighting("NumLock", baseColor, false);
                        GlobalApplyMapKeyLighting("NumDivide", baseColor, false);
                        GlobalApplyMapKeyLighting("NumMultiply", baseColor, false);
                        GlobalApplyMapKeyLighting("NumSubtract", baseColor, false);
                        GlobalApplyMapKeyLighting("Num7", baseColor, false);
                        GlobalApplyMapKeyLighting("Num8", baseColor, false);
                        GlobalApplyMapKeyLighting("Num9", baseColor, false);
                        GlobalApplyMapKeyLighting("NumAdd", baseColor, false);
                        GlobalApplyMapKeyLighting("Num4", baseColor, false);
                        GlobalApplyMapKeyLighting("Num5", baseColor, false);
                        GlobalApplyMapKeyLighting("Num6", baseColor, false);
                        GlobalApplyMapKeyLighting("NumEnter", baseColor, false);
                        GlobalApplyMapKeyLighting("Num1", baseColor, false);
                        GlobalApplyMapKeyLighting("Num2", baseColor, false);
                        GlobalApplyMapKeyLighting("Num3", baseColor, false);
                        GlobalApplyMapKeyLighting("Num0", baseColor, false);
                        GlobalApplyMapKeyLighting("NumDecimal", baseColor, false);

                        if (_LightbarMode == LightbarMode.JobGauge)
                        {
                            foreach (var f in DeviceEffects.LightbarZones)
                            {
                                GlobalApplyMapLightbarLighting(f, baseColor, false);
                            }
                        }

                        if (_FKeyMode == FKeyMode.JobGauge)
                        {
                            foreach (var f in DeviceEffects.Functions)
                            {
                                GlobalApplyMapKeyLighting(f, baseColor, false);
                            }
                        }

                        break;

                }
            }
            else
            {
                ToggleGlobalFlash3(false);

                if (!ChromaticsSettings.ChromaticsSettingsKeybindToggle)
                {
                    GlobalApplyMapKeyLighting("NumLock", baseColor, false);
                    GlobalApplyMapKeyLighting("NumDivide", baseColor, false);
                    GlobalApplyMapKeyLighting("NumMultiply", baseColor, false);
                    GlobalApplyMapKeyLighting("NumSubtract", baseColor, false);
                    GlobalApplyMapKeyLighting("Num7", baseColor, false);
                    GlobalApplyMapKeyLighting("Num8", baseColor, false);
                    GlobalApplyMapKeyLighting("Num9", baseColor, false);
                    GlobalApplyMapKeyLighting("NumAdd", baseColor, false);
                    GlobalApplyMapKeyLighting("Num4", baseColor, false);
                    GlobalApplyMapKeyLighting("Num5", baseColor, false);
                    GlobalApplyMapKeyLighting("Num6", baseColor, false);
                    GlobalApplyMapKeyLighting("NumEnter", baseColor, false);
                    GlobalApplyMapKeyLighting("Num1", baseColor, false);
                    GlobalApplyMapKeyLighting("Num2", baseColor, false);
                    GlobalApplyMapKeyLighting("Num3", baseColor, false);
                    GlobalApplyMapKeyLighting("Num0", baseColor, false);
                    GlobalApplyMapKeyLighting("NumDecimal", baseColor, false);
                }

                if (_LightbarMode == LightbarMode.JobGauge)
                {
                    foreach (var f in DeviceEffects.LightbarZones)
                    {
                        GlobalApplyMapLightbarLighting(f, baseColor, false);
                    }
                }

                if (_FKeyMode == FKeyMode.JobGauge)
                {
                    foreach (var f in DeviceEffects.Functions)
                    {
                        GlobalApplyMapKeyLighting(f, baseColor, false);
                    }
                }
            }
        }
    }
}
